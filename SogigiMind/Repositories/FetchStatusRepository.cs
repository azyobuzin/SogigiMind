using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using SogigiMind.Logics;
using SogigiMind.Models;

namespace SogigiMind.Repositories
{
    public class FetchStatusRepository : IFetchStatusRepository
    {
        private readonly IMongoCollection<FetchStatus> _collection;
        private readonly ILogger _logger;
        private readonly IndexInitializer _indexInitializer;

        public FetchStatusRepository(IMongoDatabase mongoDatabase, ILogger<FetchStatusRepository>? logger)
        {
            this._collection = (mongoDatabase ?? throw new ArgumentNullException(nameof(mongoDatabase)))
                .GetCollection<FetchStatus>(nameof(FetchStatus));
            this._logger = (ILogger?)logger ?? NullLogger.Instance;
            this._indexInitializer = new IndexInitializer(
                () => FetchStatus.CreateIndexesAsync(this._collection),
                logger);
        }

        public Task<FetchStatus> FindByUrlAsync(string url)
        {
            UrlNormalizer.AssertNormalized(url);
            return this._collection.Find(x => x.Url == url).SingleOrDefaultAsync();
        }

        public Task UpdateLeaningOptionsAsync(string url, bool? sensitive, bool? canUseToTrain)
        {
            UrlNormalizer.AssertNormalized(url);

            if (sensitive == null && canUseToTrain.GetValueOrDefault() == false)
                return Task.CompletedTask;

            var operation = Builders<FetchStatus>.Update
                .SetOnInsert(x => x.Url, url)
                .SetOnInsert(x => x.Status, FetchStatusKind.NotYet);

            if (sensitive != null)
                operation = operation.Set(x => x.Sensitive, sensitive.Value);

            if (canUseToTrain == true)
                operation = operation.Set(x => x.CanUseToTrain, true);

            return this._collection.UpdateOneAsync(
                x => x.Url == url, operation,
                new UpdateOptions() { IsUpsert = true });
        }

        public Task UpdateStatusAsync(string url, FetchStatusKind status, DateTimeOffset attemptedAt)
        {
            UrlNormalizer.AssertNormalized(url);

            return this._collection.UpdateOneAsync(
                x => x.Url == url,
                Builders<FetchStatus>.Update
                    .SetOnInsert(x => x.Url, url)
                    .Set(x => x.Status, status)
                    .Set(x => x.LastAttempt, attemptedAt.UtcDateTime),
                new UpdateOptions() { IsUpsert = true }
            );
        }

        public Task UpdateThumbnailInfoAsync(
            string url, FetchStatusKind status,
            string contentType, string contentHash,
            ThumbnailInfo? thumbnailInfo, DateTimeOffset attemptedAt)
        {
            UrlNormalizer.AssertNormalized(url);

            return this._collection.UpdateOneAsync(
                x => x.Url == url,
                Builders<FetchStatus>.Update
                    .SetOnInsert(x => x.Url, url)
                    .Set(x => x.Status, status)
                    .Set(x => x.ContentType, contentType)
                    .Set(x => x.ContentHash, contentHash)
                    .Set(x => x.ThumbnailInfo, thumbnailInfo)
                    .Set(x => x.LastAttempt, attemptedAt.UtcDateTime),
                new UpdateOptions() { IsUpsert = true }
            );
        }
    }
}
