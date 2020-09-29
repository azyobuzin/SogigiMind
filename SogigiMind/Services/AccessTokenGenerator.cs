using System;

namespace SogigiMind.Services
{
    public static class AccessTokenGenerator
    {
        public static string Generate(string prefix)
        {
            return prefix + "." + Guid.NewGuid().ToString("N");
        }
    }
}
