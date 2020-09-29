using System;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using SogigiMind.Authentication;

namespace SogigiMind.Services
{
    public class DashboardLoginService
    {
        private readonly IOptionsMonitor<DashboardLoginOptions> _options;
        private readonly AccessTokenRepository _accessTokenRepository;
        private readonly ISystemClock _clock;

        public DashboardLoginService(
            IOptionsMonitor<DashboardLoginOptions> options,
            AccessTokenRepository accessTokenRepository,
            ISystemClock? clock)
        {
            this._options = options ?? throw new ArgumentNullException(nameof(options));
            this._accessTokenRepository = accessTokenRepository ?? throw new ArgumentNullException(nameof(accessTokenRepository));
            this._clock = clock ?? new SystemClock();
        }

        public async Task<AccessTokenResult?> ChallengeAsync(string password)
        {
            var options = this._options.CurrentValue;

            var passwordCorrect = (password ?? "") == (options.Password ?? "");
            if (!passwordCorrect) return null;

            var identity = new GenericIdentity("Dashboard User");
            identity.AddClaim(new Claim(identity.RoleClaimType, SogigiMindRoles.Dashboard));

            var expiration = this._clock.UtcNow + options.TokenExpiration;
            var token = AccessTokenGenerator.Generate("dashboard");

            await this._accessTokenRepository.InsertIdenityAsync(token, expiration, identity).ConfigureAwait(false);

            return new AccessTokenResult(token, expiration);
        }
    }

#pragma warning disable CS8618 // Null 非許容フィールドは初期化されていません。null 許容として宣言することを検討してください。

    public class DashboardLoginOptions
    {
        public string Password { get; set; }

        public TimeSpan TokenExpiration { get; set; }
    }
}
