#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SogigiMind.Data
{
    public class AccessTokenData
    {
        public long Id { get; set; }

        [Required]
        public byte[] TokenHash { get; set; }

        public DateTime? Expiration { get; set; }

        public DateTime InsertedAt { get; set; }

        public List<AccessTokenClaimData> Claims { get; set; }
    }
}
