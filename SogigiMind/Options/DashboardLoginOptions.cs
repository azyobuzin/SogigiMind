using System;

#pragma warning disable CS8618 // Null 非許容フィールドは初期化されていません。null 許容として宣言することを検討してください。

namespace SogigiMind.Options
{
    public class DashboardLoginOptions
    {
        public string Password { get; set; }

        public TimeSpan TokenExpiration { get; set; } = TimeSpan.FromDays(1);
    }
}
