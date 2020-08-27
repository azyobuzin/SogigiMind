namespace SogigiMind.Options
{
    public class TokenOptions
    {
        /// <summary>
        /// アプリサーバーが使用するトークン。 <see langword="null"/> または空文字列を指定すると、トークンなしで App ロールが与えられます。
        /// </summary>
        public string? App { get; set; }

        /// <summary>
        /// 学習ワーカーが使用するトークン。 <see langword="null"/> または空文字列を指定すると、トークンなしで Worker ロールが与えられます。
        /// </summary>
        public string? Worker { get; set; }
    }
}
