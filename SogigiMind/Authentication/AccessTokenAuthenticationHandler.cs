using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using SogigiMind.Services;

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
            var identity = await this._accessTokenService.GetIdentityByTokenAsync(token).ConfigureAwait(false);

            return identity == null
                ? AuthenticateResult.NoResult()
                : AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), this.Scheme.Name));
        }
    }
}
