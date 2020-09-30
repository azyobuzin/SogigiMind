#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SogigiMind.Data
{
    public class EstimationLogData
    {
        public long Id { get; set; }

        public long UserId { get; set; }

        public EndUserData User { get; set; }

        public long RemoteImageId { get; set; }

        public RemoteImageData RemoteImage { get; set; }

        [Required, Column(TypeName = "jsonb")]
        public string Result { get; set; }

        public DateTime InsertedAt { get; set; }
    }
}
