using System.Globalization;
using System.IO;
using Gengfu.App.Speech;
using Gengfu.Core;
using Gengfu.Core.Settings;
using Gengfu.TeamSpeak;

namespace Gengfu.App;

/// <summary>Strings put together from parts: counts, times, voices, events, and an event's default words.</summary>
internal static class Copy
{
    /// <summary>An event's default words for a lead Settings offers: its name and the lead, such as "Boss in thirty seconds".</summary>
    public static string DefaultWords(MatchEvent matchEvent, TimeSpan lead, CultureInfo language) => DefaultWords([matchEvent], lead, language);

    /// <summary>
    /// The default words of events called together, at one spawn time: their names, each once, then
    /// the lead, such as "Jungle and Boss in thirty seconds" or "Jungle, Boss and Supply in thirty seconds".
    /// </summary>
    public static string DefaultWords(IReadOnlyList<MatchEvent> events, TimeSpan lead, CultureInfo language)
    {
        var format = Strings.ResourceManager.GetString(string.Create(CultureInfo.InvariantCulture, $"CallText{lead.TotalSeconds:0}"), language)
            ?? throw new ArgumentOutOfRangeException(nameof(lead), lead, "Settings offers no such lead.");
        var names = events.Select(matchEvent => EventName(matchEvent, language)).Distinct().ToList();
        var named = names.Count < 2
            ? string.Concat(names)
            : string.Format(language, Strings.ResourceManager.GetString(nameof(Strings.NamesAnd), language)!, string.Join(", ", names[..^1]), names[^1]);
        return string.Format(language, format, named);
    }

    /// <summary>"3 channels", "21 канал": the count with the noun in the language's plural form.</summary>
    public static string ChannelCount(int count, CultureInfo language)
    {
        var form = language.TwoLetterISOLanguageName == "ru" ? RussianForm(count) : count == 1 ? "One" : "Many";
        return string.Format(language, Strings.ResourceManager.GetString("ChannelCount" + form, language)!, count);
    }

    /// <summary>An event as the calls list names it: its own name, or the jungle's in the language, the app's by default.</summary>
    public static string EventName(MatchEvent matchEvent, CultureInfo? language = null) =>
        matchEvent.Name ?? Strings.ResourceManager.GetString(nameof(Strings.Jungle), language)!;

    /// <summary>
    /// What a closed event says under its name: what is wrong with it, if anything, else its spawn
    /// times and its own sound file.
    /// </summary>
    /// <param name="problem">Why its call isn't what it was given, from its last render.</param>
    /// <param name="unreadable">Whether spawn times typed for it couldn't be read.</param>
    public static (string Text, bool Wrong) EventSummary(MatchEvent matchEvent, CallProblem problem, bool unreadable) =>
        unreadable ? (Strings.SpawnsUnreadableShort, true)
        : problem switch
        {
            CallProblem.NoText => (Strings.NoTextShort, true),
            CallProblem.SpeechFailed => (Strings.SpeechFailedShort, true),
            CallProblem.FileUnreadable => (Format(Strings.FileUnreadableShort, FileName(matchEvent) ?? ""), true),
            CallProblem.FileMissing => (Format(Strings.FileMissingShort, FileName(matchEvent) ?? ""), true),
            _ when matchEvent.Spawns.Count == 0 => (Strings.NoSpawnTimes, false),
            _ => (string.Join(" · ", matchEvent.Spawns.Select(MatchClock.Format).Append(FileName(matchEvent)).OfType<string>()), false),
        };

    /// <summary>What the bar under an event's sound file says when it can't play: why, and what its calls play instead.</summary>
    /// <param name="says">The words its calls say instead; null when they play the gong.</param>
    public static string FileHint(CallProblem problem, string file, string? says) => (problem, says) switch
    {
        (CallProblem.FileMissing, null) => Format(Strings.FileMissing, file),
        (CallProblem.FileMissing, _) => Format(Strings.FileMissingSays, file, says),
        (_, null) => Strings.FileUnreadable,
        _ => Format(Strings.FileUnreadableSays, says),
    };

    /// <summary>What the bar at the top of Settings says about the settings file.</summary>
    public static string FileError(SettingsError error) => error switch
    {
        { Line: null } => Strings.SettingsFileUnreadable,
        { Setting: null } => Format(Strings.SettingsFileErrorOnLine, error.Line),
        _ => Format(Strings.SettingsFileError, error.Line, error.Setting),
    };

    /// <summary>What the bar under the privilege key says of how the server took it.</summary>
    public static string KeyAnswer(KeyResult answer) => answer switch
    {
        { Outcome: KeyOutcome.Taken, Detail: { } group } => Format(Strings.KeyTaken, group),
        { Outcome: KeyOutcome.Taken } => Strings.KeyTakenNoGroup,
        { Outcome: KeyOutcome.Unknown } => Strings.KeyUnknown,
        { Outcome: KeyOutcome.NotAllowed } => Strings.KeyNotAllowed,
        { Outcome: KeyOutcome.Refused } => Format(Strings.KeyRefused, answer.Detail ?? ""),
        _ => Strings.KeyNoAnswer,
    };

    /// <summary>The line under the version in Settings › General; none for a copy that doesn't update.</summary>
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

    private static string? FileName(MatchEvent matchEvent) => matchEvent.SoundFile is { } file ? Path.GetFileName(file) : null;

    // 1, 21, 31 канал; 2–4, 22–24 канала; 5–20, 25–30 каналов.
    private static string RussianForm(int count) =>
        (count % 10, count % 100) switch
        {
            (1, not 11) => "One",
            (2 or 3 or 4, not (12 or 13 or 14)) => "Few",
            _ => "Many",
        };
}
