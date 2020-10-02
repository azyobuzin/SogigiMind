namespace SogigiMind.Authentication
{
    public static class SogigiMindClaimTypes
    {
        public const string Prefix = "urn:sogigimind:claim:";

        /// <summary>
        /// エンドユーザーのユーザー名。一般的に <c>foo@domain</c> の形式。
        /// </summary>
        public const string Acct = Prefix + "acct";

        /// <summary>
        /// 値が <c>true</c> ならば、管理画面で操作することができる。
        /// </summary>
        public const string VisibleInDashboard = Prefix + "visible_in_dashboard";

        /// <summary>
        /// 管理者が設定したトークンの説明
        /// </summary>
        public const string Description = Prefix + "description";

        /// <summary>
        /// 指定したドメインのユーザーとして認証することができる。
        /// </summary>
        public const string AllowedDomain = Prefix + "allowed_domain";
    }
}
