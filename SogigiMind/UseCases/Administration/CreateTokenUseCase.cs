using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SogigiMind.Authentication;
using SogigiMind.DataAccess;
using SogigiMind.Infrastructures;
using SogigiMind.Logics;

namespace SogigiMind.UseCases.Administration
{
    public class CreateTokenUseCase
    {
        private readonly IAccessTokenDao _accessTokenDao;
        private readonly ILogger _logger;

        public CreateTokenUseCase(
            IAccessTokenDao accessTokenDao,
            ILogger<CreateTokenUseCase>? logger)
        {
            this._accessTokenDao = accessTokenDao ?? throw new ArgumentNullException(nameof(accessTokenDao));
            this._logger = (ILogger?)logger ?? NullLogger.Instance;
        }

        private static readonly ImmutableArray<string> s_allowedRoles = ImmutableArray.Create(SogigiMindRoles.AppServer, SogigiMindRoles.TrainingWorker);

        /// <exception cref="CreateTokenInvalidRolesException"><paramref name="roles"/> is invalid.</exception>
        public async Task<CreateTokenOutput> ExecuteAsync(IReadOnlyList<string>? roles, IReadOnlyList<string>? domains, string? description)
        {
            roles ??= Array.Empty<string>();

            if (roles.Any(x => !s_allowedRoles.Contains(x)))
                throw new CreateTokenInvalidRolesException("roles contains an unknown role.");

            var rolesStr = string.Join(",", roles.Select(x => x.Substring(SogigiMindRoles.Prefix.Length)));
            var displayName = rolesStr;

            if (!string.IsNullOrEmpty(description))
                displayName += " (" + description + ")";

            var identity = new GenericIdentity(displayName.ToString());
            identity.AddClaims(roles.Select(x => new Claim(identity.RoleClaimType, x)));
            identity.AddClaim(new Claim(SogigiMindClaimTypes.VisibleInDashboard, "true"));

            if (domains != null)
                identity.AddClaims(domains.Select(x => new Claim(SogigiMindClaimTypes.AllowedDomain, x)));

            if (description != null)
                identity.AddClaim(new Claim(SogigiMindClaimTypes.Description, description));

            var token = AccessTokenGenerator.Generate("t");

            await this._accessTokenDao.InsertIdenityAsync(token, identity).ConfigureAwait(false);

            this._logger.LogInformation("Created token for {Roles}", rolesStr);

            return new CreateTokenOutput(token, null);
        }
    }

    public class CreateTokenOutput
    {
        public string Token { get; }
        public DateTimeOffset? Expiration { get; }

        public CreateTokenOutput(string token, DateTimeOffset? expiration)
        {
            this.Token = token;
            this.Expiration = expiration;
        }
    }

    public class CreateTokenInvalidRolesException : ArgumentException
    {
        public CreateTokenInvalidRolesException(string message) : base(message) { }
    }
}
