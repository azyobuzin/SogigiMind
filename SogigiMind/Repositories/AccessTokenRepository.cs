using System;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using SogigiMind.Data;

namespace SogigiMind.Repositories
{
    public class AccessTokenRepository
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ISystemClock _clock;

        public AccessTokenRepository(ApplicationDbContext dbContext, ISystemClock? clock)
        {
            this._dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            this._clock = clock ?? new SystemClock();
        }

        public async Task<ClaimsIdentity?> GetIdentityByTokenAsync(string token)
        {
            var tokenHash = ComputeTokenHash(token);
            var accessTokenData = await this._dbContext
                .AccessTokens.AsNoTracking()
                .Include(x => x.Claims)
                .SingleOrDefaultAsync(x => x.TokenHash == tokenHash)
                .ConfigureAwait(false);

            if (accessTokenData == null) return null;

            return new ClaimsIdentity(
                accessTokenData.Claims.Select(x => new Claim(x.ClaimType, x.ClaimValue)),
                "AccessToken");
        }

        public Task InsertIdenityAsync(string token, ClaimsIdentity identity)
        {
            var accessTokenData = new AccessTokenData()
            {
                TokenHash = ComputeTokenHash(token),
                InsertedAt = this._clock.UtcNow.UtcDateTime,
            };
            this._dbContext.Add(accessTokenData);

            this._dbContext.AddRange(
                identity.Claims.Where(x => x != null)
                    .Select(x => new AccessTokenClaimData()
                    {
                        AccessToken = accessTokenData,
                        ClaimType = x.Type,
                        ClaimValue = x.Value
                    }));

            return this._dbContext.SaveChangesAsync();
        }

        private static byte[] ComputeTokenHash(string token)
        {
            using var hash = SHA256.Create();
            return hash.ComputeHash(Encoding.UTF8.GetBytes(token));
        }
    }
}
