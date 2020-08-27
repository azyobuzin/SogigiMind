using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using SogigiMind.Options;

namespace SogigiMind.Infrastructures
{
    public class TokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string DefaultAuthenticationScheme = "Token";

        private readonly IOptionsMonitor<TokenOptions>? _tokenOptions;

        public TokenAuthenticationHandler(
            IOptionsMonitor<TokenOptions>? tokenOptions,
            IOptionsMonitor<AuthenticationSchemeOptions> authenticationSchemeOptions,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(authenticationSchemeOptions, logger, encoder, clock)
        {
            this._tokenOptions = tokenOptions;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var authHeader = this.Request.Headers[HeaderNames.Authorization].FirstOrDefault();
            string? token = null;

            if (authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
                token = authHeader.Substring("Bearer ".Length).Trim();

            var tokenOptions = this._tokenOptions?.CurrentValue;

            // ユーザー名を適当に決める
            var identity = "Anonymous";
            if (tokenOptions != null && !string.IsNullOrEmpty(token))
            {
                if (token == tokenOptions.App) identity = "App";
                else if (token == tokenOptions.Worker) identity = "Worker";
            }

            // tokenOptions とトークンを比較して、有効なロールを設定
            var roles = new List<string>();
            if (string.IsNullOrEmpty(tokenOptions?.App) || tokenOptions.App == token)
                roles.Add("App");
            if (string.IsNullOrEmpty(tokenOptions?.Worker) || tokenOptions.Worker == token)
                roles.Add("Worker");

            var principal = new GenericPrincipal(new GenericIdentity(identity), roles.ToArray());
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, this.Scheme.Name)));
        }
    }
}
