#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SogigiMind.Data
{
    public class RemoteImageData
    {
        public long Id { get; set; }

        [Required]
        public string Url { get; set; }

        /// <summary>
        /// prefetch または署名付きサムネイルリクエストが飛んできたことがあるかどうか
        /// </summary>
        public bool IsKnown { get; set; }

        public bool? IsSensitive { get; set; }

        public bool? IsPublic { get; set; }

        public DateTime InsertedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public List<FetchAttemptData> FetchAttempts { get; set; }

        public List<EstimationLogData> EstimationLogs { get; set; }
    }
}
