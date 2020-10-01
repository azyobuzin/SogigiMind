using Microsoft.AspNetCore.Http;
using SogigiMind.Authentication;

namespace SogigiMind.Infrastructures
{
    public static class HttpContextExtensions
    {
        public static string? GetAcct(this HttpContext httpContext)
        {
            return httpContext.User?.FindFirst(SogigiMindClaimTypes.AccountName)?.Value;
        }
    }
}
