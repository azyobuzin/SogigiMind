using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using SogigiMind.Data;
using SogigiMind.Logics;

namespace SogigiMind.DataAccess
{
    /// <summary>
    /// 競合時の処理を一切考慮していないナイーブな EF による <see cref="IRemoteImageDao"/> 実装
    /// </summary>
    public class EfRemoteImageDao : IRemoteImageDao
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ISystemClock _clock;

        public EfRemoteImageDao(ApplicationDbContext dbContext, ISystemClock clock)
        {
            this._dbContext = dbContext;
            this._clock = clock;
        }

        public async Task UpdateAsync(string url, bool? isSensitive, bool? isPublic)
        {
            UrlNormalizer.AssertNormalized(url);

            var remoteImageData = await this._dbContext.RemoteImages.SingleOrDefaultAsync(x => x.Url == url).ConfigureAwait(false);
            var now = this._clock.UtcNow.UtcDateTime;

            if (remoteImageData == null)
            {
                remoteImageData = new RemoteImageData()
                {
                    Url = url,
                    IsSensitive = isSensitive,
                    IsPublic = isPublic,
                    InsertedAt = now,
                    UpdatedAt = now,
                };
                this._dbContext.Add(remoteImageData);
            }
            else
            {
                var changed = false;

                if (isSensitive != null && remoteImageData.IsSensitive != isSensitive.Value)
                {
                    remoteImageData.IsSensitive = isSensitive.Value;
                    changed = true;
                }

                if (isPublic != null && (remoteImageData.IsPublic == null || (!remoteImageData.IsPublic.Value && isPublic.Value)))
                {
                    remoteImageData.IsPublic = isPublic.Value;
                    changed = true;
                }

                if (changed) remoteImageData.UpdatedAt = now;
            }

            await this._dbContext.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
