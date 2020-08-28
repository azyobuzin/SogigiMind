using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SogigiMind.Infrastructures;
using SogigiMind.Services;

namespace SogigiMind.Controllers
{
    [Authorize(Roles = "App")]
    [SuppressMessage("Style", "VSTHRD200:非同期メソッドに \"Async\" サフィックスを使用する", Justification = "Controller の Action")]
    public class AppApiController : Controller
    {
        private readonly ILogger _logger;

        public AppApiController(ILogger<AppApiController>? logger)
        {
            this._logger = (ILogger?)logger ?? NullLogger.Instance;
        }

        /// <summary>
        /// 指定された URL のサムネイルの作成を予約します。
        /// </summary>
        [HttpPost]
        public void Prefetch([FromBody] PrefetchRequest request, [FromServices] ThumbnailService thumbnailService)
        {
            if (request?.Urls == null) return;

            _ = Task.Run(() =>
            {
                foreach (var url in request.Urls)
                {
                    thumbnailService.GetOrCreateThumbnailAsync(url, request.Sensitive, request.CanUseToTrain)
                        .Catch(ex => this._logger.LogError(ex, "Failed to prefetch {Url}.", url));
                }
            });
        }

        /// <summary>
        /// 指定された URL のサムネイルを作成します。
        /// エラーが発生した場合は <paramref name="url"/> へリダイレクトします。
        /// </summary>
        [HttpGet, HttpHead, HttpPost]
        public async Task<IActionResult> Thumbnail([Required] string url, [FromServices] ThumbnailService thumbnailService)
        {
            try
            {
                var result = await thumbnailService.GetOrCreateThumbnailAsync(url, null, null).ConfigureAwait(false);

                if (result != null)
                    return this.File(result.Content, result.ContentType);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Failed to get thumbnail. ({Url})", url);
            }

            return this.Redirect(url);
        }

        /// <summary>
        /// ユーザーにとってセンシティブな画像かを記録します。
        /// </summary>
        [HttpPost]
        public Task RecordPersonalSensitivity([FromBody] RecordPersonalSensitivityRequest request, [FromServices] PersonalSensitivityService service)
        {
            return service.RecordPersonalSensitivityAsync(request.User, request.Url, request.Sensitive);
        }

        /// <summary>
        /// ユーザーにとってセンシティブな画像かを取得します。
        /// </summary>
        /// <returns>戻り値は <see cref="GetPersonalSensitivityRequest.Items"/> と同じ要素数、順番です。</returns>
        [HttpPost]
        public async Task<IEnumerable<PersonalSensitivityEstimationResult>> GetPersonalSensitivity(
            [FromBody] GetPersonalSensitivityRequest request,
            [FromServices] PersonalSensitivityService service)
        {
            if (request?.Items == null) return Enumerable.Empty<PersonalSensitivityEstimationResult>();

            return await service.EstimatePersonalSensitivityAsync(
                request.User,
                request.Items.Select(x => new PersonalSensitivityEstimationInput(x.Url, x.SensitiveByDefault, x.CanUseToTrain))
            ).ConfigureAwait(false);
        }

#pragma warning disable CS8618 // Null 非許容フィールドは初期化されていません。null 許容として宣言することを検討してください。

        public class PrefetchRequest
        {
            public IReadOnlyList<string>? Urls { get; set; }

            /// <summary>
            /// 投稿者が設定したセンシティビティ
            /// </summary>
            public bool? Sensitive { get; set; }

            /// <summary>
            /// 学習データとして利用できるデータか
            /// </summary>
            public bool? CanUseToTrain { get; set; }
        }

        public class RecordPersonalSensitivityRequest
        {
            [Required]
            public string User { get; set; }

            [Required]
            public string Url { get; set; }

            [Required]
            public bool Sensitive { get; set; }
        }

        public class GetPersonalSensitivityRequest
        {
            [Required]
            public string User { get; set; }

            public IReadOnlyList<GetPersonalSensitivityRequestItem>? Items { get; set; }
        }

        public class GetPersonalSensitivityRequestItem
        {
            [Required]
            public string Url { get; set; }

            /// <summary>
            /// 投稿者が設定したセンシティビティ
            /// </summary>
            [Required]
            public bool SensitiveByDefault { get; set; }

            /// <summary>
            /// 学習データとして利用できるデータか
            /// </summary>
            [Required]
            public bool? CanUseToTrain { get; set; }
        }
    }
}
