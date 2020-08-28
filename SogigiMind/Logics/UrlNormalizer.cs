using System;
using System.Diagnostics;

namespace SogigiMind.Logics
{
    public static class UrlNormalizer
    {
        public static string NormalizeUrl(string url) => new Uri(url).AbsoluteUri;

        internal static void AssertNormalized(string url) => Debug.Assert(url == NormalizeUrl(url));
    }
}
