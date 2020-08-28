using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using SogigiMind.Models;

namespace SogigiMind.Repositories
{
    public class PersonalSensitivityRepository : IPersonalSensitivityRepository
    {
        private readonly IMongoCollection<PersonalSensitivity> _collection;
        private readonly ILogger _logger;
        private readonly IndexInitializer _indexInitializer;

        public PersonalSensitivityRepository(IMongoDatabase mongoDatabase, ILogger<PersonalSensitivityRepository>? logger)
        {
            this._collection = (mongoDatabase ?? throw new ArgumentNullException(nameof(mongoDatabase)))
                .GetCollection<PersonalSensitivity>(nameof(PersonalSensitivity));
            this._logger = (ILogger?)logger ?? NullLogger.Instance;
            this._indexInitializer = new IndexInitializer(
                () => PersonalSensitivity.CreateIndexesAsync(this._collection),
                logger);
        }

        public async Task<bool?> GetSensitivityAsync(string user, string url)
        {
            await this._indexInitializer.CreateIndexesAsync().ConfigureAwait(false);

            return (await this._collection.Find(x => x.User == user && x.Url == url)
                .SingleOrDefaultAsync().ConfigureAwait(false))?.Sensitive;
        }

        public async Task UpdateSensitivityAsync(string user, string url, bool sensitive, DateTimeOffset updatedAt)
        {
            await this._indexInitializer.CreateIndexesAsync().ConfigureAwait(false);

            await this._collection.UpdateOneAsync(
                x => x.User == user && x.Url == url,
                Builders<PersonalSensitivity>.Update
                    .SetOnInsert(x => x.User, user)
                    .SetOnInsert(x => x.Url, url)
                    .Set(x => x.Sensitive, sensitive)
                    .Set(x => x.UpdatedAt, updatedAt.UtcDateTime),
                new UpdateOptions() { IsUpsert = true }
            ).ConfigureAwait(false);
        }
    }
}
