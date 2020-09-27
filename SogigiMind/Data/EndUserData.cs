#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SogigiMind.Data
{
    public class EndUserData
    {
        public string Id { get; set; }

        [Required, Column(TypeName = "jsonb")]
        public string Settings { get; set; }

        public DateTime InsertedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
