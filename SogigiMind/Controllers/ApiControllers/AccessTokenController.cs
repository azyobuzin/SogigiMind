using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SogigiMind.Services;

namespace SogigiMind.Controllers.ApiControllers
{
    [ApiController, Route("api/access_token")]
    [SuppressMessage("Style", "VSTHRD200:非同期メソッドに \"Async\" サフィックスを使用する")]
    public class AccessTokenController : ControllerBase
    {
        /// <summary>
        /// 管理画面のログイン
        /// </summary>
        /// <response code="200">Success</response>
        /// <response code="403"><c>password</c> is incorrect.</response>
        [HttpPost("dashboard")]
        public async Task<ActionResult<AccessTokenResponse>> Dashboard([FromBody] DashboardTokenRequest request, [FromServices] DashboardLoginService service)
        {
            var result = await service.ChallengeAsync(request.Password).ConfigureAwait(false);

            if (result == null) return this.Forbid();

            return new AccessTokenResponse()
            {
                Token = result.Token,
                Expiration = result.Expiration
            };
        }

        /// <summary>
        /// Mastodon のアクセストークンを利用して、アプリユーザーのためのトークンを発行します。
        /// </summary>
        /// <param name="serviceProvider">https://host/api/v1/accounts/verify_credentials</param>
        /// <param name="authorizationHeader">Bearer token</param>
        /// <response code="200">Success</response>
        /// <response code="403">ユーザーの検証に失敗</response>
        [HttpPost("end_user_echo")]
        public ActionResult<AccessTokenResponse> EndUserEcho(
            [FromHeader(Name = "x-auth-service-provider"), Required] string serviceProvider,
            [FromHeader(Name = "x-verify-credentials-authorization"), Required] string authorizationHeader)
        {
            throw new NotImplementedException();
        }

#pragma warning disable CS8618 // Null 非許容フィールドは初期化されていません。null 許容として宣言することを検討してください。

        public class DashboardTokenRequest
        {
            [Required]
            public string Password { get; set; }
        }

        public class AccessTokenResponse
        {
            [Required]
            public string Token { get; set; }

            public DateTimeOffset? Expiration { get; set; }
        }
    }
}
