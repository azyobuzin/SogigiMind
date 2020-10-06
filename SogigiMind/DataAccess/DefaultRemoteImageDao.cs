using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using SogigiMind.Data;
using SogigiMind.Logics;

namespace SogigiMind.DataAccess
{
    public class DefaultRemoteImageDao : IRemoteImageDao
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ISystemClock _clock;

        public DefaultRemoteImageDao(ApplicationDbContext dbContext, ISystemClock clock)
        {
            this._dbContext = dbContext;
            this._clock = clock;
        }

        public async Task UpdateAsync(string url, bool markAsKnown, bool? isSensitive, bool? isPublic)
        {
            UrlNormalizer.AssertNormalized(url);
            var now = this._clock.UtcNow.UtcDateTime;

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
                await this._dbContext.SaveChangesAsync().ConfigureAwait(false);
                // TODO: handle unique constraint violation
            }
            else
            {
                await UpdateCoreAsync().ConfigureAwait(false);
            }

            async ValueTask UpdateCoreAsync()
            {
                while (true)
                {
                    var changed = false;
                    if (markAsKnown && !remoteImageData.IsKnown)
                    {
                        remoteImageData.IsKnown = true;
                        changed = true;
                    }
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
                    if (changed) remoteImageData.UpdatedAt = this._clock.UtcNow.UtcDateTime;

                    this._dbContext.Update(remoteImageData);

                    try
                    {
                        await this._dbContext.SaveChangesAsync().ConfigureAwait(false);
                        break;
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        var entry = ex.Entries.SingleOrDefault();
                        if (!(entry.Entity is RemoteImageData))
                            throw new InvalidOperationException($"Entity is not a RemoteImageData. It is {(entry.Entity is { } e ? e.GetType().ToString() : "null")}.", ex);

                        // Reset 
                        entry.State = EntityState.Detached;

                        // DB の値を使ってもう一度
                        remoteImageData = (RemoteImageData)(await entry.GetDatabaseValuesAsync().ConfigureAwait(false))!.ToObject();
                    }
                }
            }
        }
    }
}
