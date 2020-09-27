#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SogigiMind.Data
{
    public class BlobData
    {
        public long Id { get; set; }

        [Required]
        public byte[] Content { get; set; }

        public string Etag { get; set; }

        public DateTime? LastModified { get; set; }

        [Column(TypeName = "jsonb")]
        public string Metadata { get; set; }
    }
}
