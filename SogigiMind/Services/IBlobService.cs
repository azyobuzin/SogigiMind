using System;
using System.IO;
using System.Threading.Tasks;
using SogigiMind.Infrastructures;

namespace SogigiMind.Services
{
    public interface IBlobService
    {
        Task<UploadedBlobInfo> GetBlobInfoAsync(long blobId);

        Task<Stream> OpenReadAsync(long blobId);

        Task<UploadResult> UploadAsync(UploadingBlobInfo blobInfo, Stream contentStream);
    }

    public class UploadedBlobInfo
    {
        public long BlobId { get; }

        public long ContentLength { get; }

        public string? ContentType { get; }

        public string? Etag { get; }

        public DateTimeOffset? LastModified { get; }

        public UploadedBlobInfo(long blobId, long contentLength, string? contentType, string? etag, DateTimeOffset? lastModified)
        {
            this.BlobId = blobId;
            this.ContentLength = contentLength;
            this.ContentType = contentType;
            this.Etag = etag;
            this.LastModified = lastModified;
        }
    }

    public class UploadingBlobInfo
    {
        public string? ContentType { get; }

        public string? Etag { get; }

        public DateTimeOffset? LastModified { get; }

        public UploadingBlobInfo(string? contentType, string? etag, DateTimeOffset? lastModified)
        {
            this.ContentType = contentType;
            this.Etag = etag;
            this.LastModified = lastModified;
        }
    }

    public class UploadResult
    {
        public long BlobId { get; }

        public long ContentLength { get; }

        public UploadResult(long blobId, long contentLength)
        {
            this.BlobId = blobId;
            this.ContentLength = contentLength;
        }
    }
}
