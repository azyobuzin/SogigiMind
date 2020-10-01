using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using SogigiMind.Data;
using SogigiMind.Logics;

namespace SogigiMind.UseCases.Sensitivity
{
    public class SetSensitivityUseCase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ISystemClock _clock;

        public SetSensitivityUseCase(ApplicationDbContext dbContext, ISystemClock? clock)
        {
            this._dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            this._clock = clock ?? new SystemClock();
        }

        public async Task ExecuteAsync(string acct, string url, bool isSensitive)
        {
            url = UrlNormalizer.NormalizeUrl(url);
            var now = this._clock.UtcNow.UtcDateTime;

            // TODO: 共通化とリトライ処理
            var endUserData = await this._dbContext.EndUsers
                .SingleOrDefaultAsync(x => x.Acct == acct)
                .ConfigureAwait(false);

            if (endUserData == null)
            {
                endUserData = new EndUserData()
                {
                    Acct = acct,
                    Settings = "{}",
                    InsertedAt = now,
                    UpdatedAt = now,
                };
                this._dbContext.Add(endUserData);
            }

            var remoteImageData = await this._dbContext.RemoteImages
                .SingleOrDefaultAsync(x => x.Url == url)
                .ConfigureAwait(false);

            if (remoteImageData == null)
            {
                remoteImageData = new RemoteImageData()
                {
                    Url = url,
                    InsertedAt = now,
                    UpdatedAt = now,
                };
                this._dbContext.Add(remoteImageData);
            }

            await this._dbContext.SaveChangesAsync().ConfigureAwait(false);

            var personalSensitivityData = await this._dbContext.PersonalSensitivities
                .SingleOrDefaultAsync(x => x.UserId == endUserData.Id && x.RemoteImageId == remoteImageData.Id)
                .ConfigureAwait(false);

            if (personalSensitivityData == null)
            {
                personalSensitivityData = new PersonalSensitivityData()
                {
                    User = endUserData,
                    RemoteImage = remoteImageData,
                    IsSensitive = isSensitive,
                    InsertedAt = now,
                    UpdatedAt = now,
                };
                this._dbContext.Add(personalSensitivityData);
            }
            else if (personalSensitivityData.IsSensitive != isSensitive)
            {
                personalSensitivityData.IsSensitive = isSensitive;
                personalSensitivityData.UpdatedAt = now;
            }

            await this._dbContext.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
