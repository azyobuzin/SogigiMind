#nullable disable

using System.ComponentModel.DataAnnotations;

namespace SogigiMind.Data
{
    public class AccessTokenClaimData
    {
        public long Id { get; set; }

        public long AccessTokenId { get; set; }

        public AccessTokenData AccessToken { get; set; }

        [Required]
        public string ClaimType { get; set; }

        [Required]
        public string ClaimValue { get; set; }
    }
}
