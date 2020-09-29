using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SogigiMind.Authentication;

namespace SogigiMind.Controllers.ApiControllers
{
    [ApiController, Route("api/admin"), Authorize(Roles = SogigiMindRoles.Dashboard)]
    public class AdministrationController
    {
        /// <summary>
        /// 指定したロールを持つアクセストークンを発行します。
        /// </summary>
        [HttpPost("token")]
        public ActionResult<TokenResponse> CreateToken([FromBody] TokenRequest request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 指定したユーザーの情報を削除します。
        /// </summary>
        [HttpPost("delete_user")]
        public IActionResult DeleteUser([Required] string user)
        {
            throw new NotImplementedException();
        }

#pragma warning disable CS8618 // Null 非許容フィールドは初期化されていません。null 許容として宣言することを検討してください。

        public class TokenRequest
        {
            [Required]
            public IReadOnlyList<string> Roles { get; set; }
        }

        public class TokenResponse
        {
            [Required]
            public string Token { get; set; }
        }
    }
}
