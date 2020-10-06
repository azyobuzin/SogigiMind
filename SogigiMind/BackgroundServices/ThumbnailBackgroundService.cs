using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SogigiMind.Logics;
using SogigiMind.Options;
using SogigiMind.Services;

namespace SogigiMind.BackgroundServices
{
    public class ThumbnailBackgroundService<TThumbnailServiceImpl> : BackgroundService
        where TThumbnailServiceImpl : notnull, IThumbnailService
    {
        private readonly IThumbnailQueue _queueService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger _logger;
        private readonly int _workerCount;
        private readonly Dictionary<string, List<TaskCompletionSource<IReadOnlyList<ThumbnailInfo>>>> _workingItems = new Dictionary<string, List<TaskCompletionSource<IReadOnlyList<ThumbnailInfo>>>>();
        private static readonly ObjectFactory s_thumbnailServiceFactory = ActivatorUtilities.CreateFactory(typeof(TThumbnailServiceImpl), Array.Empty<Type>());

        public ThumbnailBackgroundService(
            IOptionsMonitor<ThumbnailOptions> options,
            IThumbnailQueue queueService,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<ThumbnailBackgroundService<TThumbnailServiceImpl>>? logger)
        {
            this._queueService = queueService ?? throw new ArgumentNullException(nameof(queueService));
            this._serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            this._logger = (ILogger?)logger ?? NullLogger.Instance;
            this._workerCount = options.CurrentValue.WorkerCount;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var registration = stoppingToken.Register(this._queueService.Stop);

                var workBlock = new ActionBlock<string>(
                    url => this.CreateThumbnailAsync(url, stoppingToken),
                    new ExecutionDataflowBlockOptions()
                    {
                        CancellationToken = stoppingToken,
                        EnsureOrdered = false,
                        MaxDegreeOfParallelism = this._workerCount > 0 ? this._workerCount : DataflowBlockOptions.Unbounded,
                    });

                while (await this._queueService.DequeueAsync().ConfigureAwait(false) is { } queueItem)
                {
                    var normalizedUrl = UrlNormalizer.NormalizeUrl(queueItem.Url);
                    var enqueue = false;

                    lock (this._workingItems)
                    {
                        if (this._workingItems.TryGetValue(normalizedUrl, out var tasks))
                        {
                            // 現在サムネイル作成中なので、完了したら通知してもらう
                            if (queueItem.CompletionSource != null)
                                tasks.Add(queueItem.CompletionSource);
                        }
                        else
                        {
                            // キューに積む
                            tasks = new List<TaskCompletionSource<IReadOnlyList<ThumbnailInfo>>>();
                            if (queueItem.CompletionSource != null)
                                tasks.Add(queueItem.CompletionSource);

                            this._workingItems.Add(normalizedUrl, tasks);

                            enqueue = true;
                        }
                    }

                    if (enqueue) workBlock.Post(normalizedUrl);
                }

                workBlock.Complete();
                await workBlock.Completion.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error in " + this.GetType().Name);
            }
            finally
            {
                // 待っているタスクをすべてキャンセルする
                var returningToken = stoppingToken.IsCancellationRequested ? stoppingToken : default;
                lock (this._workingItems)
                {
                    foreach (var xs in this._workingItems.Values)
                    {
                        if (xs != null)
                        {
                            foreach (var tcs in xs)
                                tcs.TrySetCanceled(returningToken);
                        }
                    }

                    this._workingItems.Clear();
                }
            }
        }

        private async Task CreateThumbnailAsync(string url, CancellationToken cancellationToken)
        {
            UrlNormalizer.AssertNormalized(url);

            IReadOnlyList<ThumbnailInfo>? thumbnails = null;
            Exception? exception = null;

            try
            {
                using (var scope = this._serviceScopeFactory.CreateScope())
                {
                    var thumbnailService = (IThumbnailService)s_thumbnailServiceFactory(scope.ServiceProvider, Array.Empty<object>());
                    thumbnails = await thumbnailService.CreateThumbnailAsync(url, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            // 完了を通知して _workingItems から削除する
            List<TaskCompletionSource<IReadOnlyList<ThumbnailInfo>>>? tasks;
            lock (this._workingItems)
                this._workingItems.Remove(url, out tasks);

            if (tasks != null)
            {
                foreach (var tcs in tasks)
                {
                    if (exception == null)
                        tcs.TrySetResult(thumbnails!);
                    else if (cancellationToken.IsCancellationRequested)
                        tcs.TrySetCanceled(cancellationToken);
                    else
                        tcs.TrySetException(exception);
                }
            }
        }
    }
}
