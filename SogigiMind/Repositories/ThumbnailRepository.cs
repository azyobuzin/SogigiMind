using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace SogigiMind.Repositories
{
    public class ThumbnailRepository : IThumbnailRepository
    {
        private readonly GridFSBucket _gridFs;

        public ThumbnailRepository(IMongoDatabase mongoDatabase)
        {
            this._gridFs = new GridFSBucket(mongoDatabase ?? throw new ArgumentNullException(nameof(mongoDatabase)));
        }

        public async Task<byte[]?> DownloadAsBytesAsync(string key)
        {
            try
            {
                return await this._gridFs.DownloadAsBytesByNameAsync(PathInGridFs(key)).ConfigureAwait(false);
            }
            catch (GridFSFileNotFoundException)
            {
                return null;
            }
        }

        public Task UploadAsync(string key, byte[] content, string url, string contentType)
        {
            return this._gridFs.UploadFromBytesAsync(
                PathInGridFs(key),
                content,
                new GridFSUploadOptions()
                {
                    Metadata = new BsonDocument()
                    {
                        { "url", url },
                        {"contentType", contentType },
                    }
                });
        }

        private static string PathInGridFs(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            return "thumbnail/" + key;
        }
    }
}
