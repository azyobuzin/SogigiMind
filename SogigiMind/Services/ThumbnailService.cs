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
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
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
        private bool _ffmpegInitialized;

        private const string ThumbnailPrefix = "thumbnail/";
        private const string TrainPrefix = "train/";

        /// <summary>学習データとして使用する画像サイズ</summary>
        private static readonly Size s_trainImageSize = new Size(128, 128);

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

        public Task<ThumbnailResult?> GetOrCreateThumbnailAsync(string url, bool? sensitive, bool? canUseToTrain)
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

            return task;
        }

        private async Task<ThumbnailResult?> GetOrCreateThumbnailCoreAsync(string url)
        {
            var collection = this._mongoDatabase.GetCollection<FetchStatus>(nameof(FetchStatus));
            string? downloadPath = null;
            string contentType;

            try
            {
                // すでにあるかを確認
                var fetchStatus = await collection.Find(x => x.Url == url).FirstOrDefaultAsync().ConfigureAwait(false);

                if (fetchStatus?.ContentHash != null)
                {
                    try
                    {
                        var gridFs = new GridFSBucket(this._mongoDatabase);
                        var fileName = ThumbnailPrefix + fetchStatus.ContentHash;
                        using var stream = await gridFs.OpenDownloadStreamByNameAsync(fileName).ConfigureAwait(false);
                        using var ms = new MemoryStream(checked((int)stream.FileInfo.Length));
                        await stream.CopyToAsync(ms).ConfigureAwait(false);
                        return new ThumbnailResult(stream.FileInfo.Metadata.GetValue("contentType").AsString, ms.ToArray());
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

                    // MaxFileSize までダウンロードする。超えた分は切り捨ててデコーダがうまくやってくれるかに任せる。
                    // （faststart じゃない mp4 は死ぬんだよな……）
                    await this.CopyStreamAsync(srcStream, dstStream).ConfigureAwait(false);
                }
                catch (Exception ex) 
                {
                    var isRemoteErrr = ex is OperationCanceledException || ex is HttpRequestException || ex is IOException;
                    this._logger.LogWarning(ex, "Failed to fetch {Url}.", url);
                    await UpdateStatusAsync(isRemoteErrr ? FetchStatusKind.RemoteError : FetchStatusKind.InternalError).ConfigureAwait(false);
                    return null;
                }

                var hashTask = Task.Run(() => ComputeFileHashAsync(downloadPath));

                try
                {
                    // 画像として処理できるか試す
                    var result = await this.TryCreateThumbnailAsync(downloadPath, url).ConfigureAwait(false);

                    // ImageSharp で処理できなかったら FFmpeg に入力してみる
                    result ??= await this.TryCreateThumbnailForVideoAsync(downloadPath, url, contentType).ConfigureAwait(false);

                }catch (Exception ex)
                {
                    // TODO: ???
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Failed to create thumbnail. ({Url})", url);

                // エラーを起こしたら InternalError 扱い
                if (!(ex is MongoConnectionException))
                {
                    try
                    {
                        await UpdateStatusAsync(FetchStatusKind.InternalError).ConfigureAwait(false);
                    }
                    catch (Exception ex2)
                    {
                        this._logger.LogError(ex2, "Failed to write InternalError status. ({Url})", url);
                    }
                }

                return null;
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

            Task UpdateStatusAsync(FetchStatusKind status)
            {
                return collection.UpdateOneAsync(
                    x => x.Url == url,
                    Builders<FetchStatus>.Update
                        .Set(x => x.Url, url)
                        .Set(x => x.Status, status)
                        .Set(x => x.LastAttempt, DateTime.Now),
                    new UpdateOptions() { IsUpsert = true }
                );
            }
        }

        private static bool IsAcceptableContentType(string contentType)
        {
            contentType = contentType.Trim();
            return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                || contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)
                || string.Equals(contentType, "application/octet-stream", StringComparison.OrdinalIgnoreCase);
        }

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

            var imageFormat = await Image.DetectFormatAsync(imageStream).ConfigureAwait(false);
            if (imageFormat == null) return null; // ImageSharp でデコードできる形式ではない

            imageStream.Position = 0;
            using var srcImage = await Image.LoadAsync<Rgb24>(imagePath).ConfigureAwait(false);

            // Exif の回転を適用
            srcImage.Mutate(x => x.AutoOrient());

            // メタデータのクリーニング
            srcImage.Metadata.ExifProfile = null;
            srcImage.Metadata.IptcProfile = null;
            srcImage.Metadata.GetGifMetadata().Comments.Clear();

            var isAnimation = srcImage.Frames.Count > 1;
            var thumbnailFormat = isAnimation ? imageFormat : JpegFormat.Instance;
            var thumbnailContentType = thumbnailFormat.DefaultMimeType;

            byte[] thumbnail;
            var maxLongSide = this._options.CurrentValue.ThumbnailLongSide;

            var needResize = srcImage.Width > maxLongSide || srcImage.Height > maxLongSide;
            if (needResize)
            {
                var thumbnailSize = srcImage.Width >= srcImage.Height
                    ? new Size(maxLongSide, Math.Max(1, (int)Math.Round(srcImage.Height * ((double)maxLongSide / srcImage.Width))))
                    : new Size(Math.Max(1, (int)Math.Round(srcImage.Width * ((double)maxLongSide / srcImage.Height))), srcImage.Height);
                using var thumbnailImage = srcImage.Clone(x => x.Resize(new ResizeOptions()
                {
                    Mode = ResizeMode.Stretch,
                    Size = thumbnailSize,
                }));
                thumbnail = ImageToByteArray(thumbnailImage, thumbnailFormat);
            }
            else
            {
                thumbnail = ImageToByteArray(srcImage, thumbnailFormat);
            }

            // もし作成したサムネイルのほうが大きくなってしまったら、元の画像を使う
            if (thumbnail.Length > imageStream.Length)
            {
                this._logger.LogInformation("The created thumbnail is bigger than the original. ({Url})", url);

                imageStream.Position = 0;
                var pos = 0;
                while (true)
                {
                    var count = await imageStream.ReadAsync(thumbnail, pos, thumbnail.Length - pos);
                    if (count == 0) break;
                    pos += count;
                }

                Debug.Assert(pos == imageStream.Length);
                Array.Resize(ref thumbnail, pos);

                thumbnailContentType = imageFormat.DefaultMimeType;
            }

            byte[]? trainImage = null;

            try
            {
                // アニメーションを削除する
                while (srcImage.Frames.Count > 1)
                    srcImage.Frames.RemoveFrame(srcImage.Frames.Count - 1);

                srcImage.Mutate(x => x.Resize(new ResizeOptions()
                {
                    Mode = ResizeMode.Crop,
                    Size = s_trainImageSize,
                }));

                srcImage.Metadata.GetPngMetadata().InterlaceMethod = PngInterlaceMode.None;

                trainImage = ImageToByteArray(srcImage, PngFormat.Instance);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Failed to create train image. ({Url})", url);
            }

            return new InternalThumbnailResult(imageFormat.DefaultMimeType, thumbnailContentType, thumbnail, trainImage);

            byte[] ImageToByteArray(Image<Rgb24> image, IImageFormat format)
            {
                using var ms = new MemoryStream();
                image.Save(ms, format);
                return ms.ToArray();
            }
        }

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

                return new InternalThumbnailResult(contentType, thumbnailResult.ThumbnailContentType, thumbnailResult.Thumbnail, thumbnailResult.TrainImage);
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

        private class InternalThumbnailResult
        {
            public string SourceContentType { get; }
            public string ThumbnailContentType { get; }
            public byte[] Thumbnail { get; }
            public byte[]? TrainImage { get; }

            public InternalThumbnailResult(string sourceContentType, string thumbnailContentType, byte[] thumbnail, byte[]? trainImage)
            {
                this.SourceContentType = sourceContentType;
                this.ThumbnailContentType = thumbnailContentType;
                this.Thumbnail = thumbnail;
                this.TrainImage = trainImage;
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
