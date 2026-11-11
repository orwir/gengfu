using System.Text.Json.Serialization;

namespace Gengfu.Core.Settings;

/// <summary>Who hears the calls: everyone in a server group, in whatever channel they are, or everyone in the chosen channels.</summary>
public enum ListenerKind
{
    Group,
    Channels,
}

/// <summary>How Gengfu looks when it doesn't look as Windows does.</summary>
public enum Theme
{
    Light,
    Dark,
}

/// <summary>A server group or a channel: its id, and its name as the server gave it.</summary>
public sealed record Named(ulong Id, string Name);

/// <summary>
/// Something in the match whose every spawn gets a call. Without a name it is the jungle, named in
/// the app's language. Its calls say its words in the voice Settings sets, by default its name and
/// the lead, or play a sound file of its own in their place.
/// </summary>
public sealed record MatchEvent
{
    /// <summary>The built-in GvG preset's one event.</summary>
    public static MatchEvent Jungle { get; } = new() { Spawns = CallSchedule.GvgSpawns };

    [JsonConverter(typeof(NotBlankConverter))]
    public string? Name { get; init; }

    /// <summary>Game times as the game shows them, from 35:00 down to 00:00.</summary>
    [JsonConverter(typeof(SpawnsConverter))]
    public required IReadOnlyList<TimeSpan> Spawns { get; init; }

    /// <summary>What its calls say; null says its name and the lead. An empty one says nothing, and the calls play the gong.</summary>
    public string? Text { get; init; }

    /// <summary>A sound file its calls play in place of its words; null speaks the words.</summary>
    [JsonConverter(typeof(NotBlankConverter))]
    public string? SoundFile { get; init; }

    /// <summary>
    /// Spawn times as typed in Settings: each a game time such as 25:00 or 5:00, or whole minutes
    /// such as 25, apart by commas or spaces, in any order. They come back soonest first, each once;
    /// when a part isn't a game time, none come back, and that part does.
    /// </summary>
    public static (IReadOnlyList<TimeSpan> Spawns, string? Unreadable) ReadSpawns(string text)
    {
        List<TimeSpan> spawns = [];
        foreach (var part in text.Split([',', ';', ' ', '\t'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (SpawnsConverter.Parse(part.Contains(':', StringComparison.Ordinal) ? part : part + ":00") is not { } spawn)
            {
                return ([], part);
            }
            spawns.Add(spawn);
        }
        return ([.. spawns.Distinct().OrderDescending()], null);
    }

    /// <summary>Spawn times as Settings shows them, such as "25:00, 15:00".</summary>
    public static string WriteSpawns(IEnumerable<TimeSpan> spawns) => string.Join(", ", spawns.Select(MatchClock.Format));
}

/// <summary>
/// What the user sets, in Settings or in the settings file. The server password and the bot's
/// identity are kept apart from it.
/// </summary>
public sealed class AppSettings : IJsonOnDeserialized
{
    /// <summary>The <see cref="Voice"/> that plays the gong in place of any voice.</summary>
    public const string Gong = "gong";

    public const string DefaultNickname = "Gengfu";

    /// <summary>The leads Settings offers; each is far longer than a tick, as <see cref="CallSchedule"/> needs.</summary>
    public static IReadOnlyList<TimeSpan> Leads { get; } =
        [.. new[] { 15, 20, 30, 45, 60 }.Select(seconds => TimeSpan.FromSeconds(seconds))];

    public string Server { get; set; } = "";

    public string Nickname { get; set; } = DefaultNickname;

    [JsonConverter(typeof(ListenersConverter))]
    public ListenerKind Listeners { get; set; }

    public Named? Group { get; set; }

    /// <summary>The chosen channels, in the server's tree order.</summary>
    public IReadOnlyList<Named> Channels { get; set; } = [];

    /// <summary>The server <see cref="Group"/> and <see cref="Channels"/> were chosen on: their ids mean nothing on another.</summary>
    public string? ListenersServer { get; set; }

    [JsonConverter(typeof(LeadConverter))]
    public TimeSpan Lead { get; set; } = CallSchedule.DefaultLead;

    /// <summary>A Windows voice's id, or <see cref="Gong"/>; null speaks in the first voice of the app's language.</summary>
    public string? Voice { get; set; }

    /// <summary>"en" or "ru"; null follows Windows.</summary>
    [JsonConverter(typeof(LanguageConverter))]
    public string? Language { get; set; }

    /// <summary>Light or dark; null follows Windows.</summary>
    [JsonConverter(typeof(ThemeConverter))]
    public Theme? Theme { get; set; }

    /// <summary>The events whose spawns get calls; the built-in GvG preset is the jungle alone.</summary>
    public IReadOnlyList<MatchEvent> Events { get; set; } = [MatchEvent.Jungle];

    /// <summary>
    /// Whether the events are the built-in GvG preset's: the jungle alone at its spawn times. How it
    /// sounds doesn't count: its words and sound file stay through Back to default, as Call ahead and
    /// Voice do, so an old file's text and sound file don't make the events read as changed.
    /// </summary>
    [JsonIgnore]
    public bool EventsAreDefault => Events is [{ Name: null } jungle] && jungle.Spawns.SequenceEqual(CallSchedule.GvgSpawns);

    // Before 0.3 a call's text and sound file were set once for every event. A file that still has
    // them opens, with them moved into its events, and its next save writes them there.
    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    private string? CallText { get; set; }

    [JsonInclude]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    private string? SoundFile { get; set; }

    // The text was the jungle's words, and the file played for every event without words of its own.
    // The reader lets a null event in whatever the type says; it stays, for the file's own check,
    // which names where it is.
    void IJsonOnDeserialized.OnDeserialized()
    {
        if (CallText is null && SoundFile is null)
        {
            return;
        }
        var file = string.IsNullOrWhiteSpace(SoundFile) ? null : SoundFile;
        Events =
        [
            .. Events.Select(matchEvent => matchEvent is { Text: null } ? matchEvent with
            {
                Text = matchEvent.Name is null ? CallText : null,
                SoundFile = matchEvent.SoundFile ?? file,
            } : matchEvent!),
        ];
        (CallText, SoundFile) = (null, null);
    }
}
