using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SogigiMind.Authentication;
using SogigiMind.UseCases.Administration;

namespace SogigiMind.Controllers.ApiControllers
{
    [ApiController, Route("api/admin"), Authorize(Roles = SogigiMindRoles.Dashboard)]
    public class AdministrationController : ControllerBase
    {
        /// <summary>
        /// 指定したロールを持つアクセストークンを発行します。
        /// </summary>
        [HttpPost("token")]
        public async Task<ActionResult<TokenResponse>> CreateToken([FromBody] TokenRequest request, [FromServices] CreateTokenUseCase useCase)
        {
            try
            {
                var result = await useCase.ExecuteAsync(request.Roles, request.Domains, request.Description).ConfigureAwait(false);
                return new TokenResponse() { Token = result.Token, Expiration = result.Expiration };
            }
            catch (CreateTokenInvalidRolesException ex)
            {
                // TODO: ProblemDetailsFactory を使う
                // https://github.com/dotnet/aspnetcore/blob/v3.1.8/src/Mvc/Mvc.Core/src/Infrastructure/ProblemDetailsClientErrorFactory.cs
                return this.BadRequest(ex.Message);
            }
        }

#pragma warning disable CS8618 // Null 非許容フィールドは初期化されていません。null 許容として宣言することを検討してください。

        public class TokenRequest
        {
            /// <summary>
            /// 発行するトークンに割り当てるロール
            /// </summary>
            /// <remarks>
            /// 有効な値は <see cref="SogigiMindRoles.AppServer"/> または <see cref="SogigiMindRoles.TrainingWorker"/>。
            /// </remarks>
            public IReadOnlyList<string>? Roles { get; set; }

            /// <summary>
            /// 発行したトークンを使って、このプロパティに指定したドメインのユーザーとして操作を行うことができる。
            /// </summary>
            public IReadOnlyList<string>? Domains { get; set; }

            public string? Description { get; set; }
        }

        public class TokenResponse
        {
            [Required]
            public string Token { get; set; }

            public DateTimeOffset? Expiration { get; set; }
        }
    }
}
