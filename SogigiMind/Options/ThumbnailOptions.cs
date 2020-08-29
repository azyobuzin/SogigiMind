using System;
using System.ComponentModel.DataAnnotations;

namespace SogigiMind.Options
{
    public class ThumbnailOptions
    {
        /// <summary>指定するとそのプロキシを、指定しないとシステムのプロキシを使用します。</summary>
        public string? Proxy { get; set; }

        /// <summary>このサイズを超えるデータはダウンロードしない</summary>
        [Range(1, int.MaxValue)]
        public int DownloadSizeLimit { get; set; } = 50 * 1024 * 1024;

        /// <summary>長辺がこのピクセル数を超えないようにリサイズする</summary>
        [Range(1, int.MaxValue)]
        public int ThumbnailLongSide { get; set; } = 800;

        public string? FFmpegPath { get; set; } = "ffmpeg";
    }
}
