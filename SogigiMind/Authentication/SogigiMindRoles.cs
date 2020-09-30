namespace SogigiMind.Authentication
{
    public static class SogigiMindRoles
    {
        public const string Prefix = "urn:sogigimind:role:";

        /// <summary>
        /// Mastodon や Pleroma のようなアプリのサーバー
        /// </summary>
        public const string AppServer = Prefix + "app_server";

        /// <summary>
        /// 学習を行うワーカー
        /// </summary>
        public const string TrainingWorker = Prefix + "training_worker";

        /// <summary>
        /// 管理画面へアクセスできる
        /// </summary>
        public const string Dashboard = Prefix + "dashboard";
    }
}
