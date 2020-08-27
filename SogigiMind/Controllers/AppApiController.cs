using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SogigiMind.Services;

namespace SogigiMind.Controllers
{
    [ApiController, Authorize(Roles = "App")]
    public class AppApiController : ControllerBase
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

            Task.Run(() =>
            {
                foreach (var url in request.Urls)
                {
                    thumbnailService.GetOrCreateThumbnailAsync(url, request.Sensitive, request.CanUseToTrain)
                        .ContinueWith(
                            (t, state) =>
                            {
                                if (t.Exception is { } ex)
                                {
                                    this._logger.LogError(ex, "Failed to prefetch {Url}.", state);
                                }
                            },
                            url,
                            CancellationToken.None,
                            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                            TaskScheduler.Default
                        );
                }
            });
        }

        /// <summary>
        /// 指定された URL のサムネイルを作成します。
        /// エラーが発生した場合は <paramref name="url"/> へリダイレクトします。
        /// </summary>
        [HttpGet, HttpHead, HttpPost]
        public async Task<IActionResult> Thumbnail(string url, [FromServices] ThumbnailService thumbnailService)
        {
            try
            {
                var result = await thumbnailService.GetOrCreateThumbnailAsync(url, null, null);

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
        public void RecordPersonalSensitivity([FromBody] RecordPersonalSensitivityRequest request)
        {
            // TODO
        }

        /// <summary>
        /// ユーザーにとってセンシティブな画像かを取得します。
        /// </summary>
        /// <returns>戻り値は <see cref="GetPersonalSensitivityRequest.Items"/> と同じ要素数、順番です。</returns>
        [HttpPost]
        public ActionResult<IEnumerable<GetPersonalSensitivityResponseItem>> GetPersonalSensitivity([FromBody] GetPersonalSensitivityRequest request)
        {
            throw new NotImplementedException();
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

        public class GetPersonalSensitivityResponseItem
        {
            public string Url { get; set; }

            /// <summary>
            /// 機械学習によって推論されたセンシティビティ 0～1。ただし推論に失敗した場合は <see langword="null"/>。
            /// </summary>
            public float? Sensitivity { get; set; }

            /// <summary>
            /// ユーザーが設定したセンシティビティ。ユーザーが設定していない場合は <see langword="null"/>。
            /// </summary>
            public bool? PersonalSensitivity { get; set; }
        }
    }
}
