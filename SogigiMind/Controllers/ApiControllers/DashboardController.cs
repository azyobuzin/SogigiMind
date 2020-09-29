using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace SogigiMind.Controllers.ApiControllers
{
    [ApiController, Route("api/dashboard")]
    public class DashboardController
    {
        /// <response code="403">password is incorrect.</response>
        [HttpPost("login")]
        public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
        {
            throw new NotImplementedException();
        }

#pragma warning disable CS8618 // Null 非許容フィールドは初期化されていません。null 許容として宣言することを検討してください。

        public class LoginRequest
        {
            [Required]
            public string Password { get; set; }
        }

        public class LoginResponse
        {
            [Required]
            public string Token { get; set; }
        }
    }
}
