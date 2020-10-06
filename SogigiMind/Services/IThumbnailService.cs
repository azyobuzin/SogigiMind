using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SogigiMind.Services
{
    public interface IThumbnailService
    {
        Task<IReadOnlyList<ThumbnailInfo>> CreateThumbnailAsync(string url, CancellationToken cancellationToken = default);
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
