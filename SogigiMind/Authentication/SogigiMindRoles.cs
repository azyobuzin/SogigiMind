using System;

namespace SogigiMind.Authentication
{
    public static class SogigiMindRoles
    {
        /// <summary>
        /// Mastodon や Pleroma のようなアプリのサーバー
        /// </summary>
        public const string AppServer = "AppServer";

        /// <summary>
        /// 学習を行うワーカー
        /// </summary>
        public const string TrainingWorker = "TrainingWorker";

        /// <summary>
        /// 管理画面へアクセスできる
        /// </summary>
        public const string Dashboard = "Dashboard";

        /// <summary>
        /// アプリを利用するユーザー
        /// </summary>
        [Obsolete]
        public const string EndUser = "EndUser";
    }
}
