using System.Globalization;
using Gengfu.App.Speech;
using Gengfu.Core;
using Gengfu.Core.Settings;

namespace Gengfu.App;

/// <summary>Strings put together from parts: counts, times, voices, events, and the call's default text.</summary>
internal static class Copy
{
    /// <summary>The call's default text for a lead Settings offers, such as "Jungle in thirty seconds".</summary>
    public static string CallText(TimeSpan lead, CultureInfo language) =>
        Strings.ResourceManager.GetString(string.Create(CultureInfo.InvariantCulture, $"CallText{lead.TotalSeconds:0}"), language)
        ?? throw new ArgumentOutOfRangeException(nameof(lead), lead, "Settings offers no such lead.");

    /// <summary>"3 channels", "21 канал": the count with the noun in the language's plural form.</summary>
    public static string ChannelCount(int count, CultureInfo language)
    {
        var form = language.TwoLetterISOLanguageName == "ru" ? RussianForm(count) : count == 1 ? "One" : "Many";
        return string.Format(language, Strings.ResourceManager.GetString("ChannelCount" + form, language)!, count);
    }

    /// <summary>An event as the calls list names it: its own name, or the jungle's in the app's language.</summary>
    public static string EventName(MatchEvent matchEvent) => matchEvent.Name ?? Strings.Jungle;

    /// <summary>What the bar at the top of Settings says about the settings file.</summary>
    public static string FileError(SettingsError error) => error switch
    {
        { Line: null } => Strings.SettingsFileUnreadable,
        { Setting: null } => Format(Strings.SettingsFileErrorOnLine, error.Line),
        _ => Format(Strings.SettingsFileError, error.Line, error.Setting),
    };

    /// <summary>The line under the version in Settings › General; none for a copy Setup didn't install.</summary>
    public static string? UpdateLine(UpdateState state) => state switch
    {
        UpdateState.Checking => Strings.UpdateChecking,
        UpdateState.UpToDate => Strings.UpToDate,
        UpdateState.Downloading downloading => Format(Strings.UpdateDownloading, downloading.Version),
        UpdateState.Ready ready => Format(Strings.UpdateInstalled, ready.Version),
        UpdateState.Failed => Strings.UpdateCheckFailed,
        _ => null,
    };

    /// <summary>A lead as Settings offers it, such as "30 seconds".</summary>
    public static string Lead(TimeSpan lead) => Format(Strings.SecondsCount, lead.TotalSeconds);

    /// <summary>A voice as the list shows it: its name, then its language in that language's own words.</summary>
    public static string VoiceLabel(Voice voice)
    {
        string language;
        try
        {
            language = CultureInfo.GetCultureInfo(voice.Language).NativeName;
        }
        catch (CultureNotFoundException)
        {
            language = voice.Language;
        }
        return voice.Name + " — " + language;
    }

    /// <summary>Time left until a call, such as "2:13", rounded up like the clock.</summary>
    public static string Span(TimeSpan span)
    {
        var (minutes, seconds) = MatchClock.Shown(span < TimeSpan.Zero ? TimeSpan.Zero : span);
        return string.Create(CultureInfo.InvariantCulture, $"{minutes}:{seconds:00}");
    }

    public static string Format(string format, params object[] args) => string.Format(CultureInfo.CurrentCulture, format, args);

    // 1, 21, 31 канал; 2–4, 22–24 канала; 5–20, 25–30 каналов.
    private static string RussianForm(int count) =>
        (count % 10, count % 100) switch
        {
            (1, not 11) => "One",
            (2 or 3 or 4, not (12 or 13 or 14)) => "Few",
            _ => "Many",
        };
}
