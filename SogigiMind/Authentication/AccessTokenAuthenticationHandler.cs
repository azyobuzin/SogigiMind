using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using SogigiMind.Infrastructures;
using SogigiMind.Repositories;

namespace SogigiMind.Authentication
{
    public class AccessTokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string DefaultAuthenticationScheme = "SogigiMindAccessToken";

        private readonly AccessTokenRepository _accessTokenService;

        public AccessTokenAuthenticationHandler(
            AccessTokenRepository accessTokenService,
            IOptionsMonitor<AuthenticationSchemeOptions> authenticationSchemeOptions,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(authenticationSchemeOptions, logger, encoder, clock)
        {
            this._accessTokenService = accessTokenService;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var authHeader = (string?)this.Request.Headers[HeaderNames.Authorization];

            if (authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) != true)
                return AuthenticateResult.NoResult();

            var token = authHeader.Substring("Bearer ".Length).Trim();

            ClaimsIdentity? identity;
            var unitOfDbConnection = new UnitOfDbConnection();
            await using (unitOfDbConnection.ConfigureAwait(false))
                identity = await this._accessTokenService.GetIdentityByTokenAsync(token, unitOfDbConnection).ConfigureAwait(false);

            if (identity == null) return AuthenticateResult.Fail("Invalid token");

            // 有効期限の検証
            var expirationClaim = identity.Claims
                .Where(x => x != null && x.Type == ClaimTypes.Expiration)
                .Select(x => (DateTimeOffset?)DateTimeOffset.Parse(x.Value))
                .OrderBy(x => x!.Value)
                .FirstOrDefault();
            if (expirationClaim < this.Clock.UtcNow)
                return AuthenticateResult.Fail("Ticket expired");

            // AllowedDomain によるユーザーなりすましの許可
            foreach (var acctHeader in this.Request.Headers["x-sogigimind-acct"])
            {
                // foo@domain の形式であることを確認
                var i = acctHeader.IndexOf('@');
                if (i < 0) continue;

                var domain = acctHeader.Substring(i + 1).TrimEnd();
                if (domain.Length == 0) continue;

                var isAllowed = identity.Claims
                    .Any(x => x != null &&
                        x.Type == SogigiMindClaimTypes.AllowedDomain &&
                        x.Value == domain);
                if (isAllowed)
                    identity.AddClaim(new Claim(SogigiMindClaimTypes.Acct, acctHeader.Trim()));
            }

            return identity == null
                ? AuthenticateResult.NoResult()
                : AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), this.Scheme.Name));
        }
    }
}
