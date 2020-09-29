using System;

namespace SogigiMind.Services
{
    public class AccessTokenResult
    {
        public string Token { get; }
        public DateTimeOffset? Expiration { get; }

        public AccessTokenResult(string token, DateTimeOffset? expiration)
        {
            this.Token = token;
            this.Expiration = expiration;
        }
    }
}
