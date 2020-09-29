using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SogigiMind.Authentication;

namespace SogigiMind.Controllers.ApiControllers
{
    [ApiController, Route("api/thumbnail")]
    public class ThumbnailController : ControllerBase
    {
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
        [HttpHead, HttpGet]
        public IActionResult Thumbnail([Required] string url, string? sig)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 指定された URL のサムネイルの作成を予約します。
        /// </summary>
        [HttpPost("prefetch"), Authorize(Roles = SogigiMindRoles.AppServer)]
        public IActionResult Prefetch([FromBody] IEnumerable<PrefetchRequestItem>? request)
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
            /// The names of accounts mentioning the URL.
            /// </summary>
            public IReadOnlyList<string?>? SourceAccounts { get; set; }
        }
    }
}
