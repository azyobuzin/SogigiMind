using System.ComponentModel.DataAnnotations;

namespace SogigiMind.Models
{
    public class PersonalSensitivity
    {
#pragma warning disable CS8618 // Null 非許容フィールドは初期化されていません。null 許容として宣言することを検討してください。

        [Required]
        public string User { get; set; }

        [Required]
        public string Url { get; set; }

        public bool Sensitive { get; set; }

#pragma warning restore CS8618
    }
}
