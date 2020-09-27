#nullable disable

using System;
using System.ComponentModel.DataAnnotations;

namespace SogigiMind.Data
{
    public class PersonalSensitivityData
    {
        [Required]
        public string UserId { get; set; }

        public EndUserData User { get; set; }

        public long RemoteImageId { get; set; }

        public RemoteImageData RemoteImage { get; set; }

        public bool IsSensitive { get; set; }

        public DateTime InsertedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
