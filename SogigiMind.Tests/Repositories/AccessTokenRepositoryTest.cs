using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using ChainingAssertion;
using Microsoft.EntityFrameworkCore;
using SogigiMind.Authentication;
using SogigiMind.Infrastructures;
using SogigiMind.TestInfrastructures;
using Xunit;

namespace SogigiMind.Repositories
{
    public class AccessTokenRepositoryTest
    {
        [Fact]
        public async Task TestInsertAndGet()
        {
            var clock = new ConstantClock(new DateTimeOffset(2020, 10, 1, 0, 0, 0, TimeSpan.Zero));
            var dbConnectionProvider = new InMemoryDbContextProvider();
            var repository = new AccessTokenRepository(dbConnectionProvider, clock);
            await using var unitOfDbConnection = new UnitOfDbConnection();
            var dbContext = dbConnectionProvider.GetConnection(unitOfDbConnection);

            var token = "test.ecdcf6a6f05a4456b9b079fe99b8e7e5";
            var name = "Name";
            var claims = new Dictionary<string, string>()
            {
                { ClaimTypes.Expiration, clock.UtcNow.AddDays(1).ToString("O") },
                { SogigiMindClaimTypes.Description, "Description" },
            };

            var identity = new GenericIdentity(name);
            identity.AddClaims(claims.Select(x => new Claim(x.Key, x.Value)));

            await repository.InsertIdenityAsync(token, identity, unitOfDbConnection);

            var insertedData = await dbContext.AccessTokens.SingleAsync();
            insertedData.InsertedAt.Is(clock.UtcNow.UtcDateTime);

            var restoredIdentity = await repository.GetIdentityByTokenAsync(token, unitOfDbConnection);
            restoredIdentity.IsNotNull();
            restoredIdentity!.Name.Is(name);

            foreach (var kvp in claims)
            {
                restoredIdentity.FindAll(kvp.Key).Single().Value.Is(kvp.Value);
            }
        }
    }
}
