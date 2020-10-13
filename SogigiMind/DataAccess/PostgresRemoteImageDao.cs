using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using SogigiMind.Data;
using SogigiMind.Logics;

namespace SogigiMind.DataAccess
{
    public class PostgresRemoteImageDao : IRemoteImageDao
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ISystemClock _clock;

        public PostgresRemoteImageDao(ApplicationDbContext dbContext, ISystemClock clock)
        {
            this._dbContext = dbContext;
            this._clock = clock;
        }

        public async Task UpdateAsync(string url, bool? isSensitive, bool? isPublic)
        {
            UrlNormalizer.AssertNormalized(url);

            //更新が必要かだけを確認するので、トランザクションは必要ない
            var remoteImageData = await this._dbContext.RemoteImages
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Url == url)
                .ConfigureAwait(false);

            var changed = remoteImageData == null ||
                (isSensitive != null && remoteImageData.IsSensitive != isSensitive.Value) ||
                (isPublic != null && (remoteImageData.IsPublic == null || (!remoteImageData.IsPublic.Value && isPublic.Value)));

            if (!changed) return;

            var now = this._clock.UtcNow.UtcDateTime;

            await this._dbContext.Database.ExecuteSqlRawAsync(
                @"
INSERT INTO remote_images (url, is_sensitive, is_public, inserted_at, updated_at)
VALUES ({0}, {1}, {2}, {3}, {3})
ON CONFLICT (url) DO UPDATE
SET is_sensitive = COALESCE({1}, is_sensitive),
    is_public = COALESCE({2} OR is_sensitive, {2}, is_sensitive),
    updated_at = {3}
",
                url, isSensitive, isPublic, now
            ).ConfigureAwait(false);
        }
    }
}
