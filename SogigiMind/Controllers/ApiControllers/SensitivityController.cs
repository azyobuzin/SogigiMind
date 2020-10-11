using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SogigiMind.Infrastructures;
using SogigiMind.UseCases.Sensitivity;

namespace SogigiMind.Controllers.ApiControllers
{
    [ApiController, Route("api/sensitivity")]
    public class SensitivityController : ControllerBase
    {
        /// <summary>
        /// ユーザーにとってセンシティブな画像かを記録します。
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> PostSensitivity(
            [FromBody, Required] PostSensitivityRequest request,
            [FromServices] SetSensitivityUseCase useCase)
        {
            await useCase.ExecuteAsync(request.Acct, request.Url, request.IsSensitive).ConfigureAwait(false);
            return this.Ok();
        }

        /// <summary>
        /// ユーザーにとってセンシティブな画像かを取得します。
        /// </summary>
        /// <returns>戻り値は <paramref name="request"/> と同じ要素数、順番です。</returns>
        [HttpPost("estimate")]
        public async Task<ActionResult<IReadOnlyList<EstimateSensitivityResponseItem>>> EstimateSensitivity(
            [FromBody, Required] EstimateSensitivityRequest request,
            [FromServices] EstimateSensitivityUseCase useCase)
        {
            var inputs = request.Items?.Select(x => new EstimateSensitivityInputItem(x.Url, x.IsSensitive, x.IsPublic)).ToArray();
            var outputs = await useCase.ExecuteAsync(request.Acct, inputs).ConfigureAwait(false);
            return outputs.Select(x => new EstimateSensitivityResponseItem()
            {
                Url = x.Url,
                EstimatedSensitivity = x.EstimatedSensitivity,
                PersonalSensitivity = x.PersonalSensitivity,
            }).ToArray();
        }

        /// <summary>
        /// ユーザーの情報を削除します。
        /// </summary>
        [HttpPost("clear")]
        public async Task<IActionResult> ClearPersonalSensitivities(
            [FromBody, Required] ClearPersonalSensitivitiesRequest request,
            [FromServices] DeletePersonalSensitivitiesUseCase useCase)
        {
            await useCase.ExecuteAsync(request.Acct).ConfigureAwait(false);
            return this.Ok();
        }

#pragma warning disable CS8618 // Null 非許容フィールドは初期化されていません。null 許容として宣言することを検討してください。

        public class PostSensitivityRequest
        {
            [Required]
            public string Acct { get; set; }

            [Required]
            public string Url { get; set; }

            [Required]
            public bool IsSensitive { get; set; }
        }

        public class EstimateSensitivityRequest
        {
            [Required]
            public string Acct { get; set; }

            [Required]
            public IReadOnlyList<EstimateSensitivityRequestItem> Items { get; set; }
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

        public class ClearPersonalSensitivitiesRequest
        {
            [Required]
            public string Acct { get; set; }
        }
    }
}
