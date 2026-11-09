using System.Windows;
using Gengfu.Core.Settings;

namespace Gengfu.App;

/// <summary>
/// How Gengfu looks: light, dark, or as Windows does, following Windows when it switches. Every
/// colour is a Fluent brush taken by DynamicResource, so whatever shows repaints in a new theme.
/// </summary>
internal static class AppTheme
{
    /// <param name="choice">Light, dark, or null for Windows' own.</param>
    public static ThemeMode ModeOf(Theme? choice) => choice switch
    {
        Theme.Light => ThemeMode.Light,
        Theme.Dark => ThemeMode.Dark,
        _ => ThemeMode.System,
    };

    /// <summary>Shows the whole app in this theme from now on.</summary>
    public static void Apply(Theme? choice)
    {
        var mode = ModeOf(choice);
        // Any mode set, even the one in use, loads the Fluent theme afresh and repaints everything.
        if (Application.Current.ThemeMode != mode)
        {
            Application.Current.ThemeMode = mode;
        }
    }
}
