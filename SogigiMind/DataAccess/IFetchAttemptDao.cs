using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SogigiMind.Data;

namespace SogigiMind.DataAccess
{
    public interface IFetchAttemptDao
    {
        Task<FetchAttemptInfo?> GetLatestFetchAttemptAsync(string url);

        Task InsertFetchAttemptAsync(string url, FetchAttemptStatus status, long? contentLength, string? contentType, DateTimeOffset startTime, IEnumerable<ThumbnailInfo> thumbnails);
    }

    public class FetchAttemptInfo
    {
        public string Url { get; }

        public FetchAttemptStatus Status { get; }

        public long? ContentLength { get; }

        public string? ContentType { get; }

        public DateTimeOffset StartTime { get; }

        public DateTimeOffset InsertedAt { get; }

        public IReadOnlyList<ThumbnailInfo> Thumbnails { get; }

        public FetchAttemptInfo(string url, FetchAttemptStatus status, long? contentLength, string? contentType, DateTimeOffset startTime, DateTimeOffset insertedAt, IReadOnlyList<ThumbnailInfo> thumbnails)
        {
            this.Url = url;
            this.Status = status;
            this.ContentLength = contentLength;
            this.ContentType = contentType;
            this.InsertedAt = insertedAt;
            this.Thumbnails = thumbnails;
        }
    }

    public class ThumbnailInfo
    {
        public long BlobId { get; }

        public string ContentType { get; }

        public long ContentLength { get; }

        public int Width { get; }

        public int Height { get; }

        public bool IsAnimation { get; }

        public ThumbnailInfo(long blobId, string contentType, long contentLength, int width, int height, bool isAnimation)
        {
            this.BlobId = blobId;
            this.ContentType = contentType;
            this.ContentLength = contentLength;
            this.Width = width;
            this.Height = height;
            this.IsAnimation = isAnimation;
        }
    }
}
