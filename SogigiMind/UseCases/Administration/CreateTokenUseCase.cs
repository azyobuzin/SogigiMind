﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SogigiMind.Authentication;
using SogigiMind.Infrastructures;
using SogigiMind.Logics;
using SogigiMind.Repositories;

namespace SogigiMind.UseCases.Administration
{
    public class CreateTokenUseCase
    {
        private readonly AccessTokenRepository _accessTokenRepository;
        private readonly ILogger _logger;

        public CreateTokenUseCase(
            AccessTokenRepository accessTokenRepository,
            ILogger<CreateTokenUseCase>? logger)
        {
            this._accessTokenRepository = accessTokenRepository ?? throw new ArgumentNullException(nameof(accessTokenRepository));
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

            var unitOfDbConnection = new UnitOfDbConnection();
            await using (unitOfDbConnection.ConfigureAwait(false))
                await this._accessTokenRepository.InsertIdenityAsync(token, identity, unitOfDbConnection).ConfigureAwait(false);

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
