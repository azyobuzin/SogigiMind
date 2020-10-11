using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace SogigiMind.Controllers.ApiControllers
{
    [ApiController, Route("api/thumbnail")]
    public class ThumbnailController : ControllerBase
    {
        /// <summary>
        /// Creates a thumbnail of the specified URL.
        /// </summary>
        /// <response code="200">Returns the thumbnail image.</response>
        /// <response code="302">Failed to create a thumbnail or the created thumbnail is bigger than the original.</response>
        [HttpHead, HttpGet]
        public IActionResult Thumbnail([Required] string url)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a thumbnail of the specified URL.
        /// </summary>
        /// <param name="url">URL string encoded with <see cref="Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(byte[])"/>.</param>
        /// <param name="sig">
        /// Signature string encoded with <see cref="Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(byte[])"/>,
        /// which is a HMAC-SHA1 computed with Base64 encoded <paramref name="url"/> parameter and the private key.
        /// This parameter is required if the URL is not prefetched.
        /// </param>
        /// <response code="200">Returns the thumbnail image.</response>
        /// <response code="302">Failed to create a thumbnail or the created thumbnail is bigger than the original.</response>
        /// <response code="403"><paramref name="sig"/> is invalid.</response>
        [HttpHead, HttpGet, Route("pleroma/{sig}/{url}/{filename?}")]
        public IActionResult ThumbnailPleromaStyle([Required] string url, [Required] string sig)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 指定された URL のサムネイルの作成を予約します。
        /// </summary>
        [HttpPost("prefetch")]
        public IActionResult Prefetch([FromBody, Required] IEnumerable<PrefetchRequestItem>? request)
        {
            throw new NotImplementedException();
        }

#pragma warning disable CS8618 // Null 非許容フィールドは初期化されていません。null 許容として宣言することを検討してください。

        public class PrefetchRequestItem
        {
            [Required]
            public string Url { get; set; }

            /// <summary>
            /// Sensitivity specified by the sender.
            /// </summary>
            public bool? IsSensitive { get; set; }

            /// <summary>
            /// Whether the URL is attached to a public post.
            /// </summary>
            public bool? IsPublic { get; set; }

            /// <summary>
            /// この画像を受信したアカウント
            /// </summary>
            /// <remarks>
            /// 実際にプリフェッチを行うべきかの判断に使用する。
            /// 直近にそのユーザーについてのセンシティブ推定を行っているなら、画像をプリフェッチし、推定も先に行っておく（今後実装する）。
            /// </remarks>
            public IReadOnlyList<string?>? Receivers { get; set; }
        }
    }
}
