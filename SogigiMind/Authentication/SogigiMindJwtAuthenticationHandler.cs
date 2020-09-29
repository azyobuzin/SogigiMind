using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace SogigiMind.Authentication
{
    public class SogigiMindJwtAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly SogigiMindJwtService _jwtService;

        public SogigiMindJwtAuthenticationHandler(
            SogigiMindJwtService jwtService,
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
            this._jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var authHeader = (string?)this.Context.Request.Headers[HeaderNames.Authorization];

            if (authHeader == null || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(AuthenticateResult.NoResult());

            var token = authHeader.Substring("Bearer ".Length).Trim();

            throw new NotImplementedException();
        }
    }
}
