using System;
using System.Diagnostics;

namespace SogigiMind.Logics
{
    public static class UrlNormalizer
    {
        public static string NormalizeUrl(string url)
        {
            if (url == null) throw new ArgumentNullException(nameof(url));
            if (url.Length == 0) throw new ArgumentException("url is empty.");
            return new Uri(url).AbsoluteUri;
        }

        internal static void AssertNormalized(string url) => Debug.Assert(url == NormalizeUrl(url));
    }
}
