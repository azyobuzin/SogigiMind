using System.Collections.Generic;
using System.Threading.Tasks;
using SogigiMind.Infrastructures;

namespace SogigiMind.Services
{
    public interface IThumbnailService
    {
        Task<IReadOnlyList<ThumbnailInfo>> GetOrCreateThumbnailAsync(string url, UnitOfDbConnection unitOfDbConnection);
    }

    public class ThumbnailInfo
    {
        public long BlobId { get; }

        public string ContentType { get; }

        public long ContentLength { get; }

        public ThumbnailInfo(long blobId, string contentType, long contentLength)
        {
            this.BlobId = blobId;
            this.ContentType = contentType;
            this.ContentLength = contentLength;
        }
    }
}
