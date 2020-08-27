using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SogigiMind.Infrastructures;
using SogigiMind.Models;
using SogigiMind.Options;

namespace SogigiMind.Services
{
    public class ThumbnailService
    {
        private readonly IMongoDatabase _mongoDatabase;
        private readonly IOptionsMonitor<ThumbnailOptions> _options;
        private readonly ILogger _logger;
        private readonly Dictionary<string, Task<ThumbnailResult?>> _tasks = new Dictionary<string, Task<ThumbnailResult?>>();
        private readonly HttpClient _httpClient;
        private Task? _initializeIndexesTask;

        private const string ThumbnailPrefix = "thumbnail/";
        private static readonly TimeSpan s_ffmpegTimeout = TimeSpan.FromSeconds(10);

        public ThumbnailService(IMongoDatabase mongoDatabase, IOptionsMonitor<ThumbnailOptions> options, ILogger<ThumbnailService>? logger)
        {
            this._mongoDatabase = mongoDatabase ?? throw new ArgumentNullException(nameof(mongoDatabase));
            this._options = options ?? throw new ArgumentNullException(nameof(options));
            this._logger = (ILogger?)logger ?? NullLogger.Instance;

            this._httpClient = new HttpClient(new HttpClientHandler() { AllowAutoRedirect = true })
            {
                Timeout = TimeSpan.FromSeconds(600),
                DefaultRequestHeaders =
                {
                    UserAgent =
                    {
                        new ProductInfoHeaderValue("SogigiMind", typeof(ThumbnailService).Assembly.GetName().Version?.ToString() ?? "0.0.0.0")
                    }
                }
            };
        }

        public async Task<ThumbnailResult?> GetOrCreateThumbnailAsync(string url, bool? sensitive, bool? canUseToTrain)
        {
            url = new Uri(url).AbsoluteUri; // Normalize

            // 同じ URL に対して、このサーバーですでに処理を開始しているなら、それを待機する
            Task<ThumbnailResult?>? task;
            lock (this._tasks)
            {
                if (!this._tasks.TryGetValue(url, out task))
                {
                    task = Task.Run(() => this.GetOrCreateThumbnailCoreAsync(url));
                    this._tasks[url] = task;
                }
            }

            var result = await task.ConfigureAwait(false);

            if (sensitive != null || canUseToTrain == true)
            {
                try
                {
                    var updates = new List<UpdateDefinition<FetchStatus>>(2);

                    if (sensitive != null)
                        updates.Add(Builders<FetchStatus>.Update.Set(x => x.Sensitive, sensitive.Value));

                    if (canUseToTrain == true)
                        updates.Add(Builders<FetchStatus>.Update.Set(x => x.CanUseToTrain, true));

                    await this.GetFetchStatusCollection()
                        .UpdateOneAsync(x => x.Url == url, Builders<FetchStatus>.Update.Combine(updates))
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, "Failed to write Sensitive and CanUseToTrain. ({Url})", url);
                }
            }

            return result;
        }

        private IMongoCollection<FetchStatus> GetFetchStatusCollection()
            => this._mongoDatabase.GetCollection<FetchStatus>(nameof(FetchStatus));

        private async Task<ThumbnailResult?> GetOrCreateThumbnailCoreAsync(string url)
        {
            string? downloadPath = null;

            try
            {
                await this.InitializeIndexesAsync().ConfigureAwait(false);

                var collection = this.GetFetchStatusCollection();
                var gridFs = new GridFSBucket(this._mongoDatabase);
                string contentType;

                // すでにあるかを確認
                var fetchStatus = await collection.Find(x => x.Url == url).FirstOrDefaultAsync().ConfigureAwait(false);

                if (fetchStatus?.ContentHash is { } contentHash && fetchStatus.ThumbnailInfo is { ContentType: var thumbnailContentType })
                {
                    try
                    {
                        var bytes = await gridFs.DownloadAsBytesByNameAsync(ThumbnailPathInGridFs(contentHash)).ConfigureAwait(false);
                        return new ThumbnailResult(thumbnailContentType, bytes);
                    }
                    catch (GridFSFileNotFoundException) { }
                }

                // InternalError を起こしていたら、改善する可能性は低いので、 10 分リトライ禁止
                if (fetchStatus?.LastAttempt.AddMinutes(10) <= DateTimeOffset.Now) return null;

                this._logger.LogInformation("Creating thumbnail for {Url}.", url);

                try
                {
                    using var res = await this._httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

                    if (!res.IsSuccessStatusCode)
                    {
                        this._logger.LogWarning("Got response with {StatusCode} from {Url}.", (int)res.StatusCode, url);
                        await UpdateStatusAsync(FetchStatusKind.RemoteError).ConfigureAwait(false);
                        return null;
                    }

                    // レスポンスを読むまでもなく、画像でも動画でもなさそうなデータなら、読まない
                    if (res.Content == null || res.Content.Headers.ContentLength == 0
                        || !IsAcceptableContentType(contentType = res.Content.Headers.ContentType.MediaType))
                    {
                        await UpdateStatusAsync(FetchStatusKind.InternalError).ConfigureAwait(false);
                        return null;
                    }

                    using var srcStream = await res.Content.ReadAsStreamAsync().ConfigureAwait(false);

                    downloadPath = Path.GetTempFileName();
                    using var dstStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);

                    await this.CopyStreamAsync(srcStream, dstStream).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    var isRemoteError = ex is OperationCanceledException || ex is HttpRequestException || ex is IOException;
                    this._logger.LogWarning(ex, "Failed to fetch {Url}.", url);
                    await UpdateStatusAsync(isRemoteError ? FetchStatusKind.RemoteError : FetchStatusKind.InternalError).ConfigureAwait(false);
                    return null;
                }

                InternalThumbnailResult? internalResult;

                try
                {
                    var hashTask = Task.Run(() => ComputeFileHashAsync(downloadPath)).TouchException();

                    // 画像として処理できるか試す
                    internalResult = await this.TryCreateThumbnailAsync(downloadPath, url).ConfigureAwait(false);

                    // ImageSharp で処理できなかったら FFmpeg に入力してみる
                    internalResult ??= await this.TryCreateThumbnailForVideoAsync(downloadPath, url, contentType).ConfigureAwait(false);

                    contentHash = await hashTask.ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, "Failed to create thumbnail. ({Url})", url);

                    // エラーを起こしたら InternalError 扱い
                    await UpdateStatusAsync(FetchStatusKind.InternalError).ConfigureAwait(false);

                    return null;
                }

                var result = internalResult != null
                    ? new ThumbnailResult(internalResult.ThumbnailContentType, internalResult.Thumbnail)
                    : null;

                try
                {
                    var operation = Builders<FetchStatus>.Update
                        .SetOnInsert(x => x.Url, url)
                        .Set(x => x.Status, internalResult != null ? FetchStatusKind.Success : FetchStatusKind.InternalError)
                        .Set(x => x.ContentType, internalResult?.SourceContentType ?? contentType)
                        .Set(x => x.ContentHash, contentHash)
                        .Set(x => x.LastAttempt, DateTimeOffset.Now);

                    if (internalResult != null)
                    {
                        await gridFs.UploadFromBytesAsync(
                            ThumbnailPathInGridFs(contentHash),
                            internalResult.Thumbnail,
                            new GridFSUploadOptions()
                            {
                                Metadata = new BsonDocument()
                                {
                                    { "url", url },
                                    {"contentType", internalResult.ThumbnailContentType },
                                }
                            }
                        ).ConfigureAwait(false);

                        // GridFS にアップロードできたら ThumbnailInfo を埋める
                        operation = operation.Set(x => x.ThumbnailInfo, new ThumbnailInfo()
                        {
                            ContentType = internalResult.ThumbnailContentType,
                            Width = internalResult.ThumbnailSize.Width,
                            Height = internalResult.ThumbnailSize.Height,
                            IsAnimation = internalResult.IsAnimation,
                        });
                    }

                    await collection.UpdateOneAsync(x => x.Url == url, operation, new UpdateOptions() { IsUpsert = true }).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, "Failed to write thumbnail to DB. ({Url})", url);
                }

                return result;

                async Task UpdateStatusAsync(FetchStatusKind status)
                {
                    try
                    {
                        await collection.UpdateOneAsync(
                            x => x.Url == url,
                            Builders<FetchStatus>.Update
                                .SetOnInsert(x => x.Url, url)
                                .Set(x => x.Status, status)
                                .Set(x => x.LastAttempt, DateTime.Now),
                            new UpdateOptions() { IsUpsert = true }
                        );
                    }
                    catch (Exception ex)
                    {
                        this._logger.LogError(ex, "Failed to write error status. ({Url})", url);
                    }
                }
            }
            finally
            {
                // 一時ファイルを削除
                if (downloadPath != null)
                {
                    try
                    {
                        File.Delete(downloadPath);
                    }
                    catch (Exception ex)
                    {
                        this._logger.LogWarning(ex, "Failed to delete temporary file at " + nameof(GetOrCreateThumbnailCoreAsync) + ".");
                    }
                }

                lock (this._tasks)
                    this._tasks.Remove(url);
            }
        }

        private static bool IsAcceptableContentType(string contentType)
        {
            contentType = contentType.Trim();
            return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                || contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)
                || string.Equals(contentType, "application/octet-stream", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 最大 <see cref="ThumbnailOptions.DownloadSizeLimit"/> バイトのデータをコピーします。
        /// </summary>
        private Task CopyStreamAsync(Stream srcStream, Stream dstStream)
        {
            var pipe = new Pipe(new PipeOptions(useSynchronizationContext: false));
            ReadSource(pipe.Writer);
            return pipe.Reader.CopyToAsync(dstStream);

            async void ReadSource(PipeWriter writer)
            {
                try
                {
                    var bytesLeft = this._options.CurrentValue.DownloadSizeLimit;

                    while (bytesLeft > 0)
                    {
                        var mem = writer.GetMemory();
                        if (mem.Length > bytesLeft)
                            mem.Slice(0, bytesLeft);

                        var count = await srcStream.ReadAsync(mem).ConfigureAwait(false);
                        if (count == 0) break;

                        bytesLeft -= count;
                        writer.Advance(count);

                        var result = await writer.FlushAsync().ConfigureAwait(false);
                        if (result.IsCanceled || result.IsCompleted) return;
                    }

                    writer.Complete();
                }
                catch (Exception ex)
                {
                    writer.Complete(ex);
                }
            }
        }

        /// <summary>
        /// 画像ファイルからサムネイルを作成する。
        /// </summary>
        private async Task<InternalThumbnailResult?> TryCreateThumbnailAsync(string imagePath, string url)
        {
            using var imageStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read, 16 * 1024, FileOptions.Asynchronous);

            var srcFormat = await Image.DetectFormatAsync(imageStream).ConfigureAwait(false);
            if (srcFormat == null) return null; // ImageSharp でデコードできる形式ではない

            imageStream.Position = 0;

            bool isAnimation;
            IImageFormat thumbnailFormat;
            byte[] thumbnailBytes;
            Size srcSize, thumbnailSize;

            using (var image = await Image.LoadAsync<Rgb24>(imagePath).ConfigureAwait(false))
            {
                // Exif の回転を適用
                image.Mutate(x => x.AutoOrient());
                srcSize = image.Size();

                // メタデータのクリーニング
                image.Metadata.ExifProfile = null;
                image.Metadata.IptcProfile = null;
                image.Metadata.GetGifMetadata()?.Comments?.Clear();

                isAnimation = image.Frames.Count > 1;
                thumbnailFormat = isAnimation ? (IImageFormat)GifFormat.Instance : JpegFormat.Instance;

                // 大きかったらリサイズ
                var maxLongSide = this._options.CurrentValue.ThumbnailLongSide;
                if (srcSize.Width > maxLongSide || srcSize.Height > maxLongSide)
                {
                    thumbnailSize = srcSize.Width >= srcSize.Height
                        ? new Size(maxLongSide, Math.Max(1, (int)Math.Round(srcSize.Height * ((double)maxLongSide / srcSize.Width))))
                        : new Size(Math.Max(1, (int)Math.Round(srcSize.Width * ((double)maxLongSide / srcSize.Height))), srcSize.Height);
                    image.Mutate(x => x.Resize(new ResizeOptions()
                    {
                        Mode = ResizeMode.Stretch,
                        Size = thumbnailSize,
                    }));
                }
                else
                {
                    thumbnailSize = srcSize;
                }

                using (var ms = new MemoryStream())
                {
                    image.Save(ms, thumbnailFormat);
                    thumbnailBytes = ms.ToArray();
                }
            }

            // もし作成したサムネイルのほうが大きくなってしまったら、元の画像を使う
            if (thumbnailBytes.Length > imageStream.Length)
            {
                this._logger.LogInformation("The created thumbnail is bigger than the original. ({Url})", url);

                imageStream.Position = 0;
                var pos = 0;
                while (true)
                {
                    var count = await imageStream.ReadAsync(thumbnailBytes, pos, thumbnailBytes.Length - pos);
                    if (count == 0) break;
                    pos += count;
                }

                Debug.Assert(pos == imageStream.Length);
                Array.Resize(ref thumbnailBytes, pos);

                thumbnailFormat = srcFormat;
                thumbnailSize = srcSize;
            }

            return new InternalThumbnailResult(srcFormat.DefaultMimeType, thumbnailFormat.DefaultMimeType, thumbnailSize, isAnimation, thumbnailBytes);
        }

        /// <summary>
        /// FFmpeg で 1 フレーム目を切り出してサムネイルを作成する。
        /// </summary>
        private async Task<InternalThumbnailResult?> TryCreateThumbnailForVideoAsync(string videoPath, string url, string contentType)
        {
            var ffmpegPath = this._options.CurrentValue.FFmpegPath;
            if (string.IsNullOrEmpty(ffmpegPath)) ffmpegPath = "ffmpeg";

            var imagePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png");

            try
            {
                var cts = new CancellationTokenSource(s_ffmpegTimeout);
                BufferedCommandResult commandResult;

                try
                {
                    commandResult = await Cli.Wrap(ffmpegPath)
                        .WithArguments(new[]
                        {
                            "-hide_banner",
                            "-loglevel", "warning",
                            "-y",
                            "-i", videoPath,
                            "-vframes", "1",
                            imagePath,
                        })
                        .WithStandardInputPipe(PipeSource.Null)
                        .WithValidation(CommandResultValidation.None) // ExitCode は後で検証するので例外にしない
                        .ExecuteBufferedAsync(cts.Token)
                        .Task.ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cts.IsCancellationRequested)
                {
                    this._logger.LogWarning("FFmpeg was killed at timeout. ({Url})", url);
                    return null;
                }

                if (commandResult.ExitCode != 0)
                {
                    this._logger.LogWarning("FFmpeg exited with error. ({Url}) {Message}", url, commandResult.StandardError);
                    return null;
                }

                if (!File.Exists(imagePath))
                {
                    this._logger.LogWarning("The output file is not found. ({Url})", url);
                    return null;
                }

                var thumbnailResult = await this.TryCreateThumbnailAsync(imagePath, url).ConfigureAwait(false);
                if (thumbnailResult == null) return null;

                Debug.Assert(thumbnailResult.SourceContentType == "image/png");
                thumbnailResult.SourceContentType = contentType;

                return thumbnailResult;
            }
            finally
            {
                try
                {
                    File.Delete(imagePath);
                }
                catch (Exception ex)
                {
                    this._logger.LogWarning(ex, "Failed to delete temporary file at " + nameof(TryCreateThumbnailForVideoAsync) + ".");
                }
            }
        }

        private static string ComputeFileHashAsync(string path)
        {
            using var hash = SHA256.Create();
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var bytes = hash.ComputeHash(stream);
            return string.Concat(bytes.Select(x => x.ToString("x2", CultureInfo.InvariantCulture)));
        }

        private static string ThumbnailPathInGridFs(string contentHash) => ThumbnailPrefix + contentHash;

        private Task InitializeIndexesAsync()
        {
            // 複数回実行してしまっても問題ないので、雑に判定
            if (this._initializeIndexesTask is { } t)
                return t;

            t = Task.Run(() => FetchStatus.CreateIndexesAsync(this.GetFetchStatusCollection()));

            this._initializeIndexesTask = t;
            return t;
        }

        private class InternalThumbnailResult
        {
            public string SourceContentType { get; set; }
            public string ThumbnailContentType { get; set; }
            public Size ThumbnailSize { get; set; }
            public bool IsAnimation { get; set; }
            public byte[] Thumbnail { get; set; }

            public InternalThumbnailResult(
                string sourceContentType,
                string thumbnailContentType,
                Size thumbnailSize,
                bool isAnimation,
                byte[] thumbnail)
            {
                this.SourceContentType = sourceContentType;
                this.ThumbnailContentType = thumbnailContentType;
                this.ThumbnailSize = thumbnailSize;
                this.IsAnimation = isAnimation;
                this.Thumbnail = thumbnail;
            }
        }
    }

    public class ThumbnailResult
    {
        public string ContentType { get; }
        public byte[] Content { get; }

        public ThumbnailResult(string contentType, byte[] content)
        {
            this.ContentType = contentType ?? throw new ArgumentNullException(nameof(content));
            this.Content = content ?? throw new ArgumentNullException(nameof(content));
        }
    }
}
