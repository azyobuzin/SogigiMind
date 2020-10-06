using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using BiDaFlow.Fluent;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SogigiMind.Data;
using SogigiMind.DataAccess;
using SogigiMind.Logics;
using SogigiMind.Options;
using SogigiMind.Services;

namespace SogigiMind.BackgroundServices
{
    /// <summary>
    /// <see cref="IThumbnailQueueConsumer"/> のコンシューマーとして、サムネイル作成をディスパッチします。
    /// </summary>
    public class ThumbnailBackgroundService : BackgroundService
    {
        private readonly IThumbnailQueueConsumer _queueConsumer;
        private readonly ISystemClock _clock;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger _logger;
        private readonly int _workerCount;
        private readonly Dictionary<string, List<TaskCompletionSource<IReadOnlyList<ThumbnailInfo>>>> _workingItems = new Dictionary<string, List<TaskCompletionSource<IReadOnlyList<ThumbnailInfo>>>>();

        public ThumbnailBackgroundService(
            IOptionsMonitor<ThumbnailOptions> options,
            IThumbnailQueueConsumer queueConsumer,
            ISystemClock? clock,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<ThumbnailBackgroundService>? logger)
        {
            this._queueConsumer = queueConsumer ?? throw new ArgumentNullException(nameof(queueConsumer));
            this._clock = clock ?? new SystemClock();
            this._serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            this._logger = (ILogger?)logger ?? NullLogger.Instance;
            this._workerCount = options.CurrentValue.WorkerCount;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var registration = stoppingToken.Register(() =>
                {
                    this._queueConsumer.Stop();

                    int itemCount;
                    lock (this._workingItems)
                        itemCount = this._workingItems.Count;

                    if (itemCount > 0)
                        this._logger.LogInformation("Waiting for {Count} items to be canceled", itemCount);
                });

                var maxDegreeOfParallelism = this._workerCount > 0 ? this._workerCount : DataflowBlockOptions.Unbounded;
                var workBlock = this.CreateDataflowBlock(maxDegreeOfParallelism, stoppingToken);

                this._logger.LogInformation("ThumbnailBackgroundService is started (WorkerCount = {WorkerCount})", maxDegreeOfParallelism);

                while (await this._queueConsumer.DequeueAsync().ConfigureAwait(false) is { } queueItem)
                {
                    var normalizedUrl = UrlNormalizer.NormalizeUrl(queueItem.Url);

                    lock (this._workingItems)
                    {
                        if (this._workingItems.TryGetValue(normalizedUrl, out var tasks))
                        {
                            // 現在サムネイル作成中なので、完了したら通知してもらう
                            if (queueItem.CompletionSource != null)
                                tasks.Add(queueItem.CompletionSource);

                            continue;
                        }
                        else
                        {
                            // サムネイル作成キューに積む
                            tasks = new List<TaskCompletionSource<IReadOnlyList<ThumbnailInfo>>>();
                            if (queueItem.CompletionSource != null)
                                tasks.Add(queueItem.CompletionSource);

                            this._workingItems.Add(normalizedUrl, tasks);
                        }
                    }

                    var sendTask = workBlock.SendAsync(normalizedUrl);
                    await Task.WhenAny(sendTask, workBlock.Completion).ConfigureAwait(false);

                    if (!sendTask.IsCompletedSuccessfully ||
#pragma warning disable VSTHRD103 // 非同期メソッドの場合に非同期メソッドを呼び出す
                        sendTask.Result == false)
#pragma warning restore VSTHRD103
                    {
                        // これ以上入力できない状態にある。おそらく例外が発生しているので
                        // ループ後の await で例外がハンドルされる。
                        break;
                    }
                }

                workBlock.Complete();
                await workBlock.Completion.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (!(ex is OperationCanceledException && stoppingToken.IsCancellationRequested))
                    this._logger.LogError(ex, "Error in ThumbnailBackgroundService");
            }

            // 待っているタスクをすべてキャンセル状態にする
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

            this._logger.LogInformation("ThumbnailBackgroundService is stopped");
        }


        private ITargetBlock<string> CreateDataflowBlock(int maxDegreeOfParallelism, CancellationToken cancellationToken)
        {
            var blockOptions = new ExecutionDataflowBlockOptions()
            {
                BoundedCapacity = maxDegreeOfParallelism,
                CancellationToken = cancellationToken,
                EnsureOrdered = false,
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
            };

            var checkLatestFetchAttemptBlock = new TransformBlock<string, string?>(
                async url =>
                {
                    return await this.NeedsToCreateThumbnailAsync(url).ConfigureAwait(false)
                        ? url : null;
                },
                blockOptions);

            var createThumbnailBlock = new ActionBlock<string?>(
                url => this.CreateThumbnailAsync(url!, cancellationToken),
                blockOptions);

            var nullBlock = new ActionBlock<string?>(
                _ => { },
                new ExecutionDataflowBlockOptions()
                {
                    SingleProducerConstrained = true,
                });

            // checkLatestFetchAttempt で null になったものは別のブロックに流すことで
            // BoundedCapacity をすぐに回復させる。
            checkLatestFetchAttemptBlock.LinkTo(nullBlock, x => x == null);

            return checkLatestFetchAttemptBlock.ToTargetBlock(createThumbnailBlock);
        }

        private async Task CreateThumbnailAsync(string url, CancellationToken cancellationToken)
        {
            UrlNormalizer.AssertNormalized(url);

            IReadOnlyList<ThumbnailInfo>? thumbnails = null;
            Exception? exception = null;

            try
            {
                using var scope = this._serviceScopeFactory.CreateScope();
                var thumbnailService = scope.ServiceProvider.GetRequiredService<IThumbnailCreationService>();
                thumbnails = await thumbnailService.CreateThumbnailAsync(url, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            this.SetTaskResult(url, thumbnails, exception, cancellationToken);
        }

        private async Task<bool> NeedsToCreateThumbnailAsync(string url)
        {
            UrlNormalizer.AssertNormalized(url);

            try
            {
                using var scope = this._serviceScopeFactory.CreateScope();
                var fetchAttemptDao = scope.ServiceProvider.GetRequiredService<IFetchAttemptDao>();
                var fetchAttemptInfo = await fetchAttemptDao.GetLatestFetchAttemptAsync(url).ConfigureAwait(false);

                // すでにサムネイルが作成されているなら、その結果を返す
                if (fetchAttemptInfo?.Status == FetchAttemptStatus.Success)
                {
                    this.SetTaskResult(url, fetchAttemptInfo.Thumbnails, null, CancellationToken.None);
                    return false;
                }

                // InternalError を起こしていたら、改善する可能性は低いので、 10 分リトライ禁止
                if (fetchAttemptInfo?.Status == FetchAttemptStatus.InternalError &&
                    fetchAttemptInfo.InsertedAt.AddMinutes(10) <= this._clock.UtcNow)
                {
                    this.SetTaskResult(url, Array.Empty<ThumbnailInfo>(), null, CancellationToken.None);
                    return false;
                }
            }
            catch (Exception ex)
            {
                this.SetTaskResult(url, null, ex, CancellationToken.None);
                return false;
            }

            // サムネイルが作成されていない場合は、 createThumbnailBlock に処理を継続する。
            // SetTaskResult は createThumbnailBlock で呼び出される。
            return true;
        }

        private void SetTaskResult(string url, IReadOnlyList<ThumbnailInfo>? thumbnails, Exception? exception, CancellationToken cancellationToken)
        {
            // 完了を通知して _workingItems から削除する
            List<TaskCompletionSource<IReadOnlyList<ThumbnailInfo>>>? tasks;
            lock (this._workingItems)
                this._workingItems.Remove(url, out tasks);

            if (tasks != null)
            {
                foreach (var tcs in tasks)
                {
                    if (exception == null)
                        tcs.TrySetResult(thumbnails ?? throw new ArgumentNullException(nameof(thumbnails)));
                    else if (cancellationToken.IsCancellationRequested)
                        tcs.TrySetCanceled(cancellationToken);
                    else
                        tcs.TrySetException(exception);
                }
            }
        }
    }
}
