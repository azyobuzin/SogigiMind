#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SogigiMind.Data
{
    public class EndUserData
    {
        public long Id { get; set; }

        public string Acct { get; set; }

        [Required, Column(TypeName = "jsonb")]
        public string Settings { get; set; }

        public DateTime InsertedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public List<EstimationLogData> EstimationLogs { get; set; }
    }
}
