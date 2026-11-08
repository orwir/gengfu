namespace Gengfu.Core.Settings;

/// <summary>Who hears the calls: everyone in a server group, in whatever channel they are, or everyone in the chosen channels.</summary>
public enum ListenerKind
{
    Group,
    Channels,
}

/// <summary>A server group or a channel: its id, and its name as the server gave it.</summary>
public sealed record Named(ulong Id, string Name);

/// <summary>What the user sets in Settings. The server password and the bot's identity are kept apart from it.</summary>
public sealed class AppSettings
{
    /// <summary>The <see cref="Voice"/> that plays the gong in place of any voice.</summary>
    public const string Gong = "gong";

    public const string DefaultNickname = "Gengfu";

    /// <summary>The leads Settings offers; each is far longer than a tick, as <see cref="CallSchedule"/> needs.</summary>
    public static IReadOnlyList<TimeSpan> Leads { get; } =
        [.. new[] { 15, 20, 30, 45, 60 }.Select(seconds => TimeSpan.FromSeconds(seconds))];

    public string Server { get; set; } = "";

    public string Nickname { get; set; } = DefaultNickname;

    public ListenerKind Listeners { get; set; }

    public Named? Group { get; set; }

    /// <summary>The chosen channels, in the server's tree order.</summary>
    public IReadOnlyList<Named> Channels { get; set; } = [];

    public TimeSpan Lead { get; set; } = CallSchedule.DefaultLead;

    /// <summary>What a call says; null says the default for the app's language and the lead.</summary>
    public string? CallText { get; set; }

    /// <summary>A Windows voice's id, or <see cref="Gong"/>; null speaks in the first voice of the app's language.</summary>
    public string? Voice { get; set; }

    /// <summary>A sound file played in place of the text; null speaks the text.</summary>
    public string? SoundFile { get; set; }

    /// <summary>"en" or "ru"; null follows Windows.</summary>
    public string? Language { get; set; }
}
