using System.Reflection;

namespace SogigiMind.Logics
{
    public static class AppVersion
    {
        private static string? s_informationalVersion;

        public static string InformationalVersion => s_informationalVersion ??=
            typeof(AppVersion).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion
                ?? "(no version)";
    }
}
