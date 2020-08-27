using System;
using System.ComponentModel.DataAnnotations;

namespace SogigiMind.Models
{
    public class FetchStatus
    {
#pragma warning disable CS8618 // Null 非許容フィールドは初期化されていません。null 許容として宣言することを検討してください。

        /// <summary>
        /// 正規化された URL
        /// </summary>
        [Required]
        public string Url { get; set; }

        public FetchStatusKind Status { get; set; }

        public string? ContentType { get; set; }

        public string? ContentHash { get; set; }

        public DateTimeOffset LastAttempt { get; set; }

        /// <summary>
        /// 投稿者が設定したセンシティビティ
        /// </summary>
        public bool? Sensitive { get; set; }

        /// <summary>
        /// 学習データとして利用できるデータか
        /// </summary>
        public bool? CanUseToTrain { get; set; }

#pragma warning restore CS8618
    }

    public enum FetchStatusKind
    {
        Success,

        /// <summary>
        /// 接続先がエラーを返した。
        /// </summary>
        RemoteError,

        /// <summary>
        /// ダウンロードしたデータの処理に失敗した。
        /// </summary>
        InternalError,
    }
}
