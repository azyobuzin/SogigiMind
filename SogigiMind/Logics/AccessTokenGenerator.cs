using System;

namespace SogigiMind.Logics
{
    public static class AccessTokenGenerator
    {
        public static string Generate(string prefix)
        {
            return prefix + "." + Guid.NewGuid().ToString("N");
        }
    }
}
