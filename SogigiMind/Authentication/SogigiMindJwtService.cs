using System;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace SogigiMind.Authentication
{
    public class SogigiMindJwtService
    {
        public const string MastodonLikeAuthenticationVerifyCredentialsUrlClaim = "urn:sogigimind:mastodon_like_authentication/verify_credentials_url";
        public const string MastodonLikeAuthenticationAuthorizationHeaderClaim = "urn:sogigimind:mastodon_like_authentication/authorization_header";
        public const string ActivityPubSignatureJwtClaim = "urn:sogigimind:ap_sig_authentication/jwt";

        private readonly IOptionsMonitor<SogigiMindJwtOptions> _options;

        public SogigiMindJwtService(IOptionsMonitor<SogigiMindJwtOptions> options)
        {
            this._options = options ?? throw new ArgumentNullException(nameof(options));
        }

        private SogigiMindJwtOptions Options => this._options.CurrentValue ?? throw new InvalidOperationException("options is null.");

        /// <summary>
        /// Decodes the JWT and create a <see cref="ClaimsIdentity"/>.
        /// </summary>
        public ClaimsIdentity? ReadToken(string token)
        {
            var handler = new JsonWebTokenHandler();
            var decoded = handler.ReadJsonWebToken(token);

            var isEndUser = decoded.Subject?.StartsWith("acct:", StringComparison.Ordinal) == true;
            if (isEndUser)
            {
                if (!decoded.Audiences.Contains(this.Options.Audience))
                    return null;

                if (decoded.Alg == "none")
                {
                    // Mastodon-like API authentication
                    var identity = new GenericIdentity(decoded.Subject, "MastodonLike");
                    if (!decoded.TryGetClaim(MastodonLikeAuthenticationVerifyCredentialsUrlClaim, out var verifyCredentialsClaim))
                        return null;
                    if (!decoded.TryGetClaim(MastodonLikeAuthenticationAuthorizationHeaderClaim, out var authorizationHeaderClaim))
                        return null;
                    identity.AddClaim(verifyCredentialsClaim);
                    identity.AddClaim(authorizationHeaderClaim);
                    return identity;
                }
                else
                {
                    // ActivityPub signature
                    var identity = new GenericIdentity(decoded.Subject, "ActivityPubSignature");
                    identity.AddClaim(new Claim(ActivityPubSignatureJwtClaim, token));
                    return identity;
                }
            }
            else
            {
                // 通常の JWT のフロー
                var validationParameters = this.CreateValidationParameters();
                var result = handler.ValidateToken(token, validationParameters);
                return result.IsValid ? result.ClaimsIdentity : null;
            }
        }

        private TokenValidationParameters CreateValidationParameters()
        {
            var options = this.Options;

            var validationParameters = new TokenValidationParameters()
            {
                RequireExpirationTime = false,
                ValidIssuer = options.Issuer,
                ValidAudience = options.Audience,
            };

            if (options.IssuerSigningKey is { } k)
                validationParameters.IssuerSigningKey = new SymmetricSecurityKey(k);

            if (options.IssuerSigningKeys is { } ks)
                validationParameters.IssuerSigningKeys = ks.Select(x => new SymmetricSecurityKey(x));

            return validationParameters;
        }
    }
}
