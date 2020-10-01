using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using SogigiMind.Data;
using SogigiMind.Logics;

namespace SogigiMind.Repositories
{
    public class RemoteImageRepository
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ISystemClock _clock;

        public RemoteImageRepository(ApplicationDbContext dbContext, ISystemClock clock)
        {
            this._dbContext = dbContext;
            this._clock = clock;
        }

        public Task UpdateAsync(string url, bool markAsKnown, bool? isSensitive, bool? isPublic)
        {
            UrlNormalizer.AssertNormalized(url);
            var now = this._clock.UtcNow.UtcDateTime;

            return this._dbContext.Database.IsNpgsql()
                ? this.UpdatePostgresAsync(url, markAsKnown, isSensitive, isPublic, now)
                : this.UpdateNaitveAsync(url, markAsKnown, isSensitive, isPublic, now);
        }

        private Task UpdatePostgresAsync(string url, bool markAsKnown, bool? isSensitive, bool? isPublic, DateTime now)
        {
            return this._dbContext.Database.ExecuteSqlInterpolatedAsync(
                $@"
INSERT INTO remote_images (url, is_known, is_sensitive, is_public, inserted_at, updated_at)
VALUES ({url}, {markAsKnown}, {isSensitive}, {isPublic}, {now}, {now})
ON CONFLICT (url) DO UPDATE SET
    is_known = {markAsKnown} OR is_known,
    is_sensitive = COALESCE({isSensitive}, is_sensitive),
    is_public = CASE
        WHEN is_public IS NULL THEN {isPublic}
        WHEN {isPublic} IS NOT NULL THEN {isPublic} OR is_public
        ELSE is_public
    END,
    updated_at = {now}
");
        }

        private async Task UpdateNaitveAsync(string url, bool markAsKnown, bool? isSensitive, bool? isPublic, DateTime now)
        {
            var remoteImageData = await this._dbContext.RemoteImages.SingleOrDefaultAsync(x => x.Url == url).ConfigureAwait(false);

            if (remoteImageData == null)
            {
                remoteImageData = new RemoteImageData()
                {
                    Url = url,
                    IsKnown = markAsKnown,
                    IsSensitive = isSensitive,
                    IsPublic = isPublic,
                    InsertedAt = now,
                    UpdatedAt = now,
                };
                this._dbContext.Add(remoteImageData);
            }
            else
            {
                if (markAsKnown) remoteImageData.IsKnown = true;
                if (isSensitive != null) remoteImageData.IsSensitive = isSensitive.Value;
                if (isPublic != null) remoteImageData.IsPublic |= isPublic.Value;
                remoteImageData.UpdatedAt = now;
            }

            await this._dbContext.SaveChangesAsync().ConfigureAwait(false);
            // TODO: handle unique vaiolation error or DbUpdateConcurrencyException
        }
    }
}
