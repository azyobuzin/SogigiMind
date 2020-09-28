using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SogigiMind.Authentication;

namespace SogigiMind.Controllers.ApiControllers
{
    [ApiController, Route("api/sensitivity"), Authorize(Roles = Roles.EndUser)]
    public class SensitivityController : ControllerBase
    {
        /// <summary>
        /// ユーザーにとってセンシティブな画像かを記録します。
        /// </summary>
        [HttpPost]
        public IActionResult PostSensitivity([FromBody] PostSensitivityRequest request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// ユーザーにとってセンシティブな画像かを取得します。
        /// </summary>
        /// <returns>戻り値は <paramref name="request"/> と同じ要素数、順番です。</returns>
        [HttpPost("estimate")]
        public ActionResult<IEnumerable<EstimateSensitivityResponseItem>> EstimateSensitivity([FromBody] IEnumerable<EstimateSensitivityRequestItem> request)
        {
            throw new NotImplementedException();
        }

        [HttpGet("settings")]
        public IActionResult GetSettings()
        {
            throw new NotImplementedException();
        }

        [HttpPost("settings")]
        public IActionResult PostSettings()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// ユーザーの情報を削除します。
        /// </summary>
        [HttpPost("clear")]
        public IActionResult Clear()
        {
            throw new NotImplementedException();
        }

#pragma warning disable CS8618 // Null 非許容フィールドは初期化されていません。null 許容として宣言することを検討してください。

        public class PostSensitivityRequest
        {
            [Required]
            public string Url { get; set; }

            [Required]
            public bool IsSensitive { get; set; }
        }

        public class EstimateSensitivityRequestItem
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

        public class EstimateSensitivityResponseItem
        {
            [Required]
            public string Url { get; set; }

            /// <summary>
            /// 機械学習によって推論されたセンシティビティ 0～1。ただし推論に失敗した場合は <see langword="null"/>。
            /// </summary>
            public float? EstimatedSensitivity { get; set; }

            /// <summary>
            /// ユーザーが設定したセンシティビティ。ユーザーが設定していない場合は <see langword="null"/>。
            /// </summary>
            public bool? PersonalSensitivity { get; set; }
        }
    }
}
