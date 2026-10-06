using System.Reflection;

namespace KBot.CLI;

internal static class ApplicationVersion
{
    public static string Value => typeof(ApplicationVersion).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "DEV";
}
