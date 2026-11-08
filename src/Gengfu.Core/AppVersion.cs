using System.Reflection;

namespace Gengfu.Core;

/// <summary>
/// The running version, shown in Settings › General. It is written once, as &lt;Version&gt; in
/// Directory.Build.props, and the build stamps it into every assembly.
/// </summary>
public static class AppVersion
{
    public static string Current { get; } = Read(typeof(AppVersion).Assembly);

    public static string Read(Assembly assembly)
    {
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return informational is null ? "0.0.0" : informational.Split('+')[0];
    }

    /// <summary>A test build, such as 0.1.0-rc.1: it takes test builds as updates, which a release never does.</summary>
    public static bool IsPrerelease(string version) => version.Contains('-');
}
