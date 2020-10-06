using System;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using SogigiMind.Data;
using SogigiMind.Infrastructures;
using SogigiMind.Services;

namespace SogigiMind.Repositories
{
    public class AccessTokenRepository
    {
        private readonly IDbConnectionProvider<ApplicationDbContext> _dbConnectionProvider;
        private readonly ISystemClock _clock;

        public AccessTokenRepository(IDbConnectionProvider<ApplicationDbContext> dbConnectionProvider, ISystemClock? clock)
        {
            this._dbConnectionProvider = dbConnectionProvider ?? throw new ArgumentNullException(nameof(dbConnectionProvider));
            this._clock = clock ?? new SystemClock();
        }

        public async Task<ClaimsIdentity?> GetIdentityByTokenAsync(string token, UnitOfDbConnection unitOfDbConnection)
        {
            var dbContext = this._dbConnectionProvider.GetConnection(unitOfDbConnection);
            var tokenHash = ComputeTokenHash(token);

            // Waiting for EFCore 5.0
            // https://github.com/dotnet/efcore/issues/10582
            var whereClause = dbContext.Database.IsInMemory()
                ? (Expression<Func<AccessTokenData, bool>>)(x => x.TokenHash.SequenceEqual(tokenHash))
                : (x => x.TokenHash == tokenHash);

            var accessTokenData = await dbContext
                .AccessTokens.AsNoTracking()
                .Include(x => x.Claims)
                .SingleOrDefaultAsync(whereClause)
                .ConfigureAwait(false);

            if (accessTokenData == null) return null;

            return new ClaimsIdentity(
                accessTokenData.Claims.Select(x => new Claim(x.ClaimType, x.ClaimValue)),
                "AccessToken");
        }

        public Task InsertIdenityAsync(string token, ClaimsIdentity identity, UnitOfDbConnection unitOfDbConnection)
        {
            var dbContext = this._dbConnectionProvider.GetConnection(unitOfDbConnection);

            var accessTokenData = new AccessTokenData()
            {
                TokenHash = ComputeTokenHash(token),
                InsertedAt = this._clock.UtcNow.UtcDateTime,
            };
            dbContext.Add(accessTokenData);

            dbContext.AddRange(
                identity.Claims.Where(x => x != null)
                    .Select(x => new AccessTokenClaimData()
                    {
                        AccessToken = accessTokenData,
                        ClaimType = x.Type,
                        ClaimValue = x.Value
                    }));

            return dbContext.SaveChangesAsync();
        }

        private static byte[] ComputeTokenHash(string token)
        {
            using var hash = SHA256.Create();
            return hash.ComputeHash(Encoding.UTF8.GetBytes(token));
        }
    }
}
