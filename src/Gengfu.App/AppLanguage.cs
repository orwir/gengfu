using System.Globalization;

namespace Gengfu.App;

/// <summary>
/// The language Gengfu shows its strings in and picks a voice by: English or Russian. "Same as
/// Windows" means Russian on a Russian Windows and English on any other.
/// </summary>
internal static class AppLanguage
{
    public static CultureInfo English { get; } = CultureInfo.GetCultureInfo("en");

    public static CultureInfo Russian { get; } = CultureInfo.GetCultureInfo("ru");

    /// <param name="choice">"en", "ru", or null for Windows' own.</param>
    /// <param name="windows">Windows' display language.</param>
    public static CultureInfo Resolve(string? choice, CultureInfo windows) =>
        (choice ?? windows.TwoLetterISOLanguageName) == "ru" ? Russian : English;

    /// <summary>Shows the strings read from now on in this language.</summary>
    public static void Apply(CultureInfo language)
    {
        CultureInfo.CurrentUICulture = language;
        CultureInfo.DefaultThreadCurrentUICulture = language;
        Strings.Culture = language;
    }
}
