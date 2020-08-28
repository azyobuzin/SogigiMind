using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace SogigiMind.Models
{
#pragma warning disable CS8618 // Null 非許容フィールドは初期化されていません。null 許容として宣言することを検討してください。

    public class FetchStatus
    {
        [BsonId]
        public ObjectId Id { get; set; }

        /// <summary>
        /// 正規化された URL
        /// </summary>
        [Required]
        public string Url { get; set; }

        [BsonRepresentation(BsonType.String)]
        public FetchStatusKind Status { get; set; }

        public string? ContentType { get; set; }

        public string? ContentHash { get; set; }

        public ThumbnailInfo? ThumbnailInfo { get; set; }

        public DateTime? LastAttempt { get; set; }

        /// <summary>
        /// 投稿者が設定したセンシティビティ
        /// </summary>
        public bool? Sensitive { get; set; }

        /// <summary>
        /// 学習データとして利用できるデータか
        /// </summary>
        public bool? CanUseToTrain { get; set; }

        public static async Task CreateIndexesAsync(IMongoCollection<FetchStatus> collection)
        {
            await collection.Indexes
                .CreateOneAsync(new CreateIndexModel<FetchStatus>(
                    Builders<FetchStatus>.IndexKeys.Ascending(x => x.Url),
                    new CreateIndexOptions() { Unique = true }))
                .ConfigureAwait(false);

            await collection.Indexes
                .CreateOneAsync(new CreateIndexModel<FetchStatus>(
                    Builders<FetchStatus>.IndexKeys.Descending(x => x.LastAttempt)))
                .ConfigureAwait(false);
        }
    }

    public enum FetchStatusKind
    {
        NotYet,

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

    public class ThumbnailInfo
    {
        [Required]
        public string ContentType { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public bool IsAnimation { get; set; }
    }
}
