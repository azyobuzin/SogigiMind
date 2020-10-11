#nullable disable

using System;
using System.Collections.Generic;

namespace SogigiMind.Data
{
    public class EndUserData
    {
        public long Id { get; set; }

        public string Acct { get; set; }

        public DateTime InsertedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public List<EstimationLogData> EstimationLogs { get; set; }
    }
}
