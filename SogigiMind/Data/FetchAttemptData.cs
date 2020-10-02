#nullable disable

using System;
using System.Collections.Generic;

namespace SogigiMind.Data
{
    public class FetchAttemptData
    {
        public long Id { get; set; }

        public long RemoteImageId { get; set; }

        public RemoteImageData RemoteImage { get; set; }

        public FetchAttemptStatus Status { get; set; }

        public long? ContentLength { get; set; }

        public string ContentType { get; set; }

        /// <summary>
        /// 取得処理を開始した時刻
        /// </summary>
        public DateTime StartTime { get; set; }

        public DateTime InsertedAt { get; set; }

        public List<ThumbnailData> Thumbnails { get; set; }
    }

    public enum FetchAttemptStatus
    {
        Success = 1,
        RemoteError,
        InternalError,
    }
}
