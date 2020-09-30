#nullable disable

using System;
using System.ComponentModel.DataAnnotations;

namespace SogigiMind.Data
{
    public class BlobData
    {
        public long Id { get; set; }

        [Required]
        public byte[] Content { get; set; }

        public long ContentLength { get; set; }

        public string ContentType { get; set; }

        public string Etag { get; set; }

        public DateTime? LastModified { get; set; }
    }
}
