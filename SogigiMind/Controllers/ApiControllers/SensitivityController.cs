using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SogigiMind.Authentication;
using SogigiMind.Infrastructures;
using SogigiMind.UseCases.Sensitivity;

namespace SogigiMind.Controllers.ApiControllers
{
    [ApiController, Route("api/sensitivity"), Authorize(Policy = EndUserAuthorizationPolicy.PolicyName)]
    public class SensitivityController : ControllerBase
    {
        /// <summary>
        /// ユーザーにとってセンシティブな画像かを記録します。
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> PostSensitivity([FromBody] PostSensitivityRequest request, [FromServices] SetSensitivityUseCase useCase)
        {
            // TODO: acct の取得を共通化して、引数から取れるようにしたい
            var acct = this.HttpContext.GetAcct();
            if (string.IsNullOrEmpty(acct)) return this.Forbid();

            await useCase.ExecuteAsync(acct, request.Url, request.IsSensitive).ConfigureAwait(false);
            return this.Ok();
        }

        /// <summary>
        /// ユーザーにとってセンシティブな画像かを取得します。
        /// </summary>
        /// <returns>戻り値は <paramref name="request"/> と同じ要素数、順番です。</returns>
        [HttpPost("estimate")]
        public async Task<ActionResult<IReadOnlyList<EstimateSensitivityResponseItem>>> EstimateSensitivity(
            [FromBody] IEnumerable<EstimateSensitivityRequestItem>? request,
            [FromServices] EstimateSensitivityUseCase useCase)
        {
            var acct = this.HttpContext.GetAcct();
            if (string.IsNullOrEmpty(acct)) return this.Forbid();

            var inputs = request?.Select(x => new EstimateSensitivityInputItem(x.Url, x.IsSensitive, x.IsPublic)).ToArray();
            var outputs = await useCase.ExecuteAsync(acct, inputs).ConfigureAwait(false);
            return outputs.Select(x => new EstimateSensitivityResponseItem()
            {
                Url = x.Url,
                EstimatedSensitivity = x.EstimatedSensitivity,
                PersonalSensitivity = x.PersonalSensitivity,
            }).ToArray();
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
        public async Task<IActionResult> Clear([FromServices] DeletePersonalSensitivitiesUseCase useCase)
        {
            var acct = this.HttpContext.GetAcct();
            if (string.IsNullOrEmpty(acct)) return this.Forbid();

            await useCase.ExecuteAsync(acct).ConfigureAwait(false);
            return this.Ok();
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
