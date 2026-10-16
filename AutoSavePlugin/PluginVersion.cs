using System.Reflection;

namespace AutoSavePlugin;

/// <summary>Version of the plug-in, set at build time.</summary>
/// <remarks>
/// GitHub Actions passes the release tag as <c>-p:Version=2.0.0</c>; local builds use the
/// default from the .csproj. The same version ends up in the DLL file properties.
/// </remarks>
public static class PluginVersion
{
    public static string Current { get; } = Read();

    private static string Read()
    {
        var version = typeof(PluginVersion).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";

        // The SDK appends "+<commit hash>" when built from a git repository
        var plus = version.IndexOf('+');
        return plus >= 0 ? version[..plus] : version;
    }
}
