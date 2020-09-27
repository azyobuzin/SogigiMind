#nullable disable

using System.ComponentModel.DataAnnotations;

namespace SogigiMind.Data
{
    public class ThumbnailData
    {
        public long Id { get; set; }

        public long FetchAttemptId { get; set; }

        public FetchAttemptData FetchAttempt { get; set; }

        public long BlobId { get; set; }

        public BlobData Blob { get; set; }

        [Required]
        public string ContentType { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public bool IsAnimation { get; set; }
    }
}
