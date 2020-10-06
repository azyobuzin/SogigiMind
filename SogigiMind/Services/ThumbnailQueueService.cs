using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using SogigiMind.Services;

namespace SogigiMind.Services
{
    public class ThumbnailQueueService : IThumbnailService, IThumbnailQueue
    {
        private readonly Channel<ThumbnailQueueItem> _channel;

        public ThumbnailQueueService()
        {
            this._channel = Channel.CreateUnbounded<ThumbnailQueueItem>();
        }

        public Task<IReadOnlyList<ThumbnailInfo>> CreateThumbnailAsync(string url, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<IReadOnlyList<ThumbnailInfo>>();
            var written = this._channel.Writer.TryWrite(new ThumbnailQueueItem(url, tcs));
            return written ? tcs.Task : throw new InvalidOperationException("Could not write to the channel.");
        }

        public void Enqueue(string url)
        {
            var written = this._channel.Writer.TryWrite(new ThumbnailQueueItem(url, null));
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

    public interface IThumbnailQueue
    {
        void Enqueue(string url);

        Task<ThumbnailQueueItem?> DequeueAsync(CancellationToken cancellationToken = default);

        void Stop();
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class SogigiMindServiceCollectionExtensions
    {
        public static IServiceCollection AddThumbnailQueueService(this IServiceCollection services)
        {
            var service = new ThumbnailQueueService();
            services.AddSingleton<IThumbnailService>(service);
            services.AddSingleton<IThumbnailQueue>(service);
            return services;
        }
    }
}
