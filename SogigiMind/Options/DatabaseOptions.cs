using System.ComponentModel.DataAnnotations;

namespace SogigiMind.Options
{
    public class DatabaseOptions
    {
        [Required]
        public string? ConnectionString { get; set; }

        [Required]
        public string? Database { get; set; }
    }
}
