using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using SogigiMind.Data;
using SogigiMind.Logics;

namespace SogigiMind.DataAccess
{
    public class EfFetchAttemptDao : IFetchAttemptDao
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ISystemClock _clock;

        public EfFetchAttemptDao(ApplicationDbContext dbContext, ISystemClock? clock)
        {
            this._dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            this._clock = clock ?? new SystemClock();
        }

        public async Task<FetchAttemptInfo?> GetLatestFetchAttemptAsync(string url)
        {
            UrlNormalizer.AssertNormalized(url);

            var fetchAttemptData = await this._dbContext.FetchAttempts
                .AsNoTracking()
                .Include(x => x.Thumbnails)
                .ThenInclude(x => x.Blob.ContentLength)
                .Where(x => x.RemoteImage.Url == url)
                .OrderByDescending(x => x.InsertedAt)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);

            if (fetchAttemptData == null) return null;

            return new FetchAttemptInfo(
                url,
                fetchAttemptData.Status,
                fetchAttemptData.ContentLength,
                fetchAttemptData.ContentType,
                new DateTimeOffset(fetchAttemptData.StartTime, TimeSpan.Zero),
                new DateTimeOffset(fetchAttemptData.InsertedAt, TimeSpan.Zero),
                fetchAttemptData.Thumbnails
                    .Select(x => new ThumbnailInfo(x.BlobId, x.ContentType, x.Blob.ContentLength, x.Width, x.Height, x.IsAnimation))
                    .ToArray()
            );
        }

        public async Task InsertFetchAttemptAsync(string url, FetchAttemptStatus status, long? contentLength, string? contentType, DateTimeOffset startTime, IEnumerable<ThumbnailInfo> thumbnails)
        {
            UrlNormalizer.AssertNormalized(url);

            var remoteImageData = await this._dbContext.RemoteImages
                .SingleOrDefaultAsync(x => x.Url == url)
                .ConfigureAwait(false);

            if (remoteImageData == null)
                throw new ArgumentException($"The specified URL '{url}' is not inserted to RemoteImages.");

            var now = this._clock.UtcNow.UtcDateTime;

            var fetchAttemptData = new FetchAttemptData()
            {
                RemoteImage = remoteImageData,
                Status = status,
                ContentLength = contentLength,
                ContentType = contentType,
                StartTime = startTime.UtcDateTime,
                InsertedAt = now,
            };
            this._dbContext.Add(fetchAttemptData);

            foreach (var thumbnail in thumbnails)
            {
                var thumbnailData = new ThumbnailData()
                {
                    FetchAttempt = fetchAttemptData,
                    BlobId = thumbnail.BlobId,
                    ContentType = thumbnail.ContentType,
                    Width = thumbnail.Width,
                    Height = thumbnail.Height,
                    IsAnimation = thumbnail.IsAnimation,
                };
                this._dbContext.Add(thumbnailData);
            }

            await this._dbContext.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
