#nullable disable

using System;
using System.ComponentModel.DataAnnotations;

namespace SogigiMind.Data
{
    public class RemoteImageData
    {
        public long Id { get; set; }

        [Required]
        public string Url { get; set; }

        public bool IsKnown { get; set; }

        public bool? IsSensitive { get; set; }

        public bool? IsPublic { get; set; }

        public DateTime InsertedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
