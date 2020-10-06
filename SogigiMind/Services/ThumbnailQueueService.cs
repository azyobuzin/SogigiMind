using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using SogigiMind.DataAccess;
using SogigiMind.Services;

namespace SogigiMind.Services
{
    /// <summary>
    /// サムネイル作成のキューを提供します。
    /// </summary>
    public class ThumbnailQueueService : IThumbnailQueueProducer, IThumbnailQueueConsumer
    {
        private readonly Channel<ThumbnailQueueItem> _channel;

        public ThumbnailQueueService()
        {
            this._channel = Channel.CreateUnbounded<ThumbnailQueueItem>();
        }

        public void Enqueue(ThumbnailQueueItem queueItem)
        {
            if (queueItem == null) throw new ArgumentNullException(nameof(queueItem));
            var written = this._channel.Writer.TryWrite(queueItem);
            if (!written) throw new InvalidOperationException("Could not write to the channel.");
        }

        public async Task<ThumbnailQueueItem?> DequeueAsync(CancellationToken cancellationToken = default)
        {
            var reader = this._channel.Reader;

            if (!await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                return null;

            return await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        }

        public void Stop()
        {
            this._channel.Writer.TryComplete();
        }
    }

    public class ThumbnailQueueItem
    {
        public string Url { get; }
        public TaskCompletionSource<IReadOnlyList<ThumbnailInfo>>? CompletionSource { get; }

        public ThumbnailQueueItem(string url, TaskCompletionSource<IReadOnlyList<ThumbnailInfo>>? completionSource)
        {
            this.Url = url ?? throw new ArgumentNullException(nameof(url));
            this.CompletionSource = completionSource;
        }
    }

    public interface IThumbnailQueueProducer
    {
        void Enqueue(ThumbnailQueueItem queueItem);
    }

    public interface IThumbnailQueueConsumer
    {
        Task<ThumbnailQueueItem?> DequeueAsync(CancellationToken cancellationToken = default);

        void Stop();
    }

    public static class ThumbnailQueueProducerExtensions
    {
        public static Task<IReadOnlyList<ThumbnailInfo>> GetOrCreateThumbnailAsync(this IThumbnailQueueProducer producer, string url)
        {
            var tcs = new TaskCompletionSource<IReadOnlyList<ThumbnailInfo>>();
            producer.Enqueue(new ThumbnailQueueItem(url, tcs));
            return tcs.Task;
        }

        public static void PostCreateThumbnail(IThumbnailQueueProducer producer, string url)
        {
            producer.Enqueue(new ThumbnailQueueItem(url, null));
        }
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class SogigiMindServiceCollectionExtensions
    {
        public static IServiceCollection AddThumbnailQueueService(this IServiceCollection services)
        {
            var service = new ThumbnailQueueService();
            services.AddSingleton<IThumbnailQueueProducer>(service);
            services.AddSingleton<IThumbnailQueueConsumer>(service);
            return services;
        }
    }
}
