using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SogigiMind.Data;

namespace SogigiMind.Services
{
    public class DefaultBlobService : IBlobService
    {
        private readonly ApplicationDbContext _dbContext;

        public DefaultBlobService(ApplicationDbContext dbContext)
        {
            this._dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<UploadedBlobInfo> GetBlobInfoAsync(long blobId)
        {
            return await this._dbContext.Blobs
                .AsNoTracking()
                .Where(x => x.Id == blobId)
                .Select(x => new UploadedBlobInfo(x.Id, x.ContentLength, x.ContentType, x.Etag, x.LastModified))
                .SingleOrDefaultAsync()
                .ConfigureAwait(false)
                ?? throw new ArgumentException($"blobId {blobId} is not found.");
        }

        public async Task<Stream> OpenReadAsync(long blobId)
        {
            var bytes = await this._dbContext.Blobs
                .AsNoTracking()
                .Where(x => x.Id == blobId)
                .Select(x => x.Content)
                .SingleOrDefaultAsync()
                .ConfigureAwait(false)
                ?? throw new ArgumentException($"blobId {blobId} is not found.");
            return new MemoryStream(bytes, 0, bytes.Length, false, true);
        }

        public async Task<UploadResult> UploadAsync(UploadingBlobInfo blobInfo, Stream contentStream)
        {
            var contentBytes = await StreamToByteArrayAsync(contentStream).ConfigureAwait(false);

            var blobData = new BlobData()
            {
                Content = contentBytes,
                ContentLength = contentBytes.Length,
                ContentType = blobInfo.ContentType,
                Etag = blobInfo.Etag,
                LastModified = blobInfo.LastModified?.UtcDateTime,
            };

            this._dbContext.Add(blobData);
            await this._dbContext.SaveChangesAsync().ConfigureAwait(false);

            return new UploadResult(blobData.Id, contentBytes.Length);
        }

        private static async ValueTask<byte[]> StreamToByteArrayAsync(Stream stream)
        {
            if (stream is MemoryStream ms && ms.TryGetBuffer(out var innerBuf))
                return ((ReadOnlySpan<byte>)innerBuf).Slice(checked((int)ms.Position)).ToArray();

            ms = new MemoryStream();
            await stream.CopyToAsync(ms).ConfigureAwait(false);
            return ms.ToArray();
        }
    }
}
