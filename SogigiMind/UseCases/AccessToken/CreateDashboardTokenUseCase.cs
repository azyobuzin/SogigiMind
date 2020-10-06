using System;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SogigiMind.Authentication;
using SogigiMind.Infrastructures;
using SogigiMind.Logics;
using SogigiMind.Options;
using SogigiMind.Repositories;

namespace SogigiMind.UseCases.AccessToken
{
    public class CreateDashboardTokenUseCase
    {
        private readonly IOptionsMonitor<DashboardLoginOptions> _options;
        private readonly AccessTokenRepository _accessTokenRepository;
        private readonly ISystemClock _clock;
        private readonly ILogger _logger;

        public CreateDashboardTokenUseCase(
            IOptionsMonitor<DashboardLoginOptions> options,
            AccessTokenRepository accessTokenRepository,
            ISystemClock? clock,
            ILogger<CreateDashboardTokenUseCase>? logger)
        {
            this._options = options ?? throw new ArgumentNullException(nameof(options));
            this._accessTokenRepository = accessTokenRepository ?? throw new ArgumentNullException(nameof(accessTokenRepository));
            this._clock = clock ?? new SystemClock();
            this._logger = (ILogger?)logger ?? NullLogger.Instance;
        }

        public async Task<CreateDashboardTokenOutput?> ExecuteAsync(string? password)
        {
            var options = this._options.CurrentValue;

            var passwordCorrect = (password ?? "") == (options.Password ?? "");
            if (!passwordCorrect)
            {
                this._logger.LogInformation("Dashboard login failed (password: {Password})", password);
                return null;
            }

            var identity = new GenericIdentity("Dashboard User");
            identity.AddClaim(new Claim(identity.RoleClaimType, SogigiMindRoles.Dashboard));

            DateTimeOffset? expiration = null;
            if (options.TokenExpiration > TimeSpan.Zero)
            {
                expiration = this._clock.UtcNow + options.TokenExpiration;
                identity.AddClaim(new Claim(ClaimTypes.Expiration, expiration.Value.ToString("O")));
            }

            var token = AccessTokenGenerator.Generate("dashboard");

            var unitOfDbConnection = new UnitOfDbConnection();
            await using (unitOfDbConnection.ConfigureAwait(false))
                await this._accessTokenRepository.InsertIdenityAsync(token, identity, unitOfDbConnection).ConfigureAwait(false);

            this._logger.LogInformation("Dashboard login success");

            return new CreateDashboardTokenOutput(token, expiration);
        }
    }

    public class CreateDashboardTokenOutput
    {
        public string Token { get; }
        public DateTimeOffset? Expiration { get; }

        public CreateDashboardTokenOutput(string token, DateTimeOffset? expiration)
        {
            this.Token = token;
            this.Expiration = expiration;
        }
    }
}
