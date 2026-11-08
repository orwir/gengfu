using System.Globalization;
using System.IO;
using Gengfu.App.Speech;
using Gengfu.Core;
using Gengfu.Core.Settings;
using Gengfu.TeamSpeak;

namespace Gengfu.App;

/// <summary>What Gengfu connects with; Connect is offered again once the fields say something else.</summary>
internal sealed record Login(string Server, string Nickname, string Password)
{
    public static Login Of(AppSettings settings, string password) =>
        new(
            settings.Server.Trim(),
            string.IsNullOrWhiteSpace(settings.Nickname) ? AppSettings.DefaultNickname : settings.Nickname.Trim(),
            password);
}

/// <summary>
/// One running Gengfu: what Settings holds, the match clock and its calls, the call's sound, and the
/// seat on TeamSpeak. The window drives it from its tick, on the UI thread.
/// </summary>
internal sealed class Session : IAsyncDisposable
{
    // The server sends its groups a moment after the connection is up.
    private static readonly TimeSpan ListsAfter = TimeSpan.FromSeconds(1);

    private readonly CultureInfo _windows;
    private readonly CallAudio _audio;
    private Task<CallClip> _clip;
    private ConnectionStatus _seen;

    /// <param name="windows">Windows' display language, for "Same as Windows".</param>
    public Session(CultureInfo windows)
        : this(windows, new SystemTimeSource(), new CallAudio(), new TeamSpeakConnection(null))
    {
    }

    internal Session(CultureInfo windows, ITimeSource time, CallAudio audio, TeamSpeakConnection connection)
    {
        _windows = windows;
        _audio = audio;
        Connection = connection;
        Language = AppLanguage.Resolve(Settings.Language, windows);
        Clock = new MatchClock(time);
        Schedule = new CallSchedule(Clock, CallSchedule.GvgSpawns, Settings.Lead);
        Caller = new Caller(
            Schedule,
            () => CanCall,
            RenderAsync,
            frames => Target is { } target ? Connection.WhisperAsync(frames, target) : Task.FromResult(false));
        _clip = RenderAsync();
    }

    public AppSettings Settings { get; } = new();

    /// <summary>The server password, kept out of <see cref="Settings"/>.</summary>
    public string Password { get; set; } = "";

    /// <summary>The language the strings show in and a voice is picked by.</summary>
    public CultureInfo Language { get; private set; }

    public MatchClock Clock { get; }

    public CallSchedule Schedule { get; }

    public Caller Caller { get; }

    public TeamSpeakConnection Connection { get; }

    /// <summary>What the current connection was started with; null after Disconnect or before any Connect.</summary>
    public Login? ConnectedWith { get; private set; }

    /// <summary>The server's groups, as of the last look; refreshed once connected and when a list opens.</summary>
    public IReadOnlyList<ServerGroupInfo> Groups { get; private set; } = [];

    /// <summary>The server's channels in its tree order, as of the last look.</summary>
    public IReadOnlyList<ChannelInfo> Channels { get; private set; } = [];

    /// <summary>The call as last rendered, with why it plays the gong when it does; null until the first render ends.</summary>
    public CallClip? Clip => _clip.IsCompletedSuccessfully ? _clip.Result : null;

    /// <summary>Who hears the calls; null until a group or a channel is chosen.</summary>
    public WhisperTarget? Target => Settings.Listeners switch
    {
        ListenerKind.Group when Settings.Group is { } group => new WhisperTarget.ServerGroup(group.Id),
        ListenerKind.Channels when Settings.Channels.Count > 0 => new WhisperTarget.Channels([.. Settings.Channels.Select(channel => channel.Id)]),
        _ => null,
    };

    /// <summary>Whether a call can go out now: connected, with someone to hear it.</summary>
    public bool CanCall => Target is not null && Connection.State.Status == ConnectionStatus.Connected;

    /// <summary>Whether Connect would start a new connection: none is up, or a field changed since this one began.</summary>
    public bool OffersConnect => OffersConnectFor(Connection.State.Status, ConnectedWith, Login.Of(Settings, Password));

    public StatusLine Status => StatusLine.Of(Connection.State, ConnectedWith?.Server, Settings, Language);

    public StatusLine SettingsStatus => StatusLine.InSettings(Connection.State, ConnectedWith?.Server, Settings);

    /// <summary>What a call says: the user's text, or the default for the language and lead.</summary>
    public string CallText => TextOf(Settings, Language);

    /// <summary>What listeners hear, in words, for the line under the clock.</summary>
    public string Heard => HeardAs(Settings, Language, Clip);

    /// <summary>Makes the calls due now and keeps the server's lists fresh; the window calls it on every tick.</summary>
    public void Tick()
    {
        Caller.Tick();
        var status = Connection.State.Status;
        if (status == ConnectionStatus.Connected && _seen != ConnectionStatus.Connected)
        {
            _ = RefreshListsLaterAsync();
        }
        _seen = status;
    }

    public Task ConnectAsync()
    {
        var login = Login.Of(Settings, Password);
        // Ids belong to one server: on another, the same ids name other groups or nothing.
        if (Settings.ListenersServer is { } chosenOn && !string.Equals(chosenOn, login.Server, StringComparison.OrdinalIgnoreCase))
        {
            Settings.Group = null;
            Settings.Channels = [];
            Settings.ListenersServer = null;
        }
        ConnectedWith = login;
        ForgetLists();
        // The new connection's lists are read even when no tick sees it connecting.
        _seen = ConnectionStatus.Disconnected;
        return Connection.ConnectAsync(login.Server, login.Nickname, login.Password);
    }

    public Task DisconnectAsync()
    {
        ConnectedWith = null;
        ForgetLists();
        return Connection.DisconnectAsync();
    }

    /// <summary>Reads the server's groups and channels again, and brings the chosen ones' names up to date.</summary>
    public async Task RefreshListsAsync()
    {
        if (Connection.State.Status != ConnectionStatus.Connected)
        {
            return;
        }
        Groups = await Connection.ListServerGroupsAsync();
        Channels = await Connection.ListChannelsAsync();
        if (Settings.Group is { } chosen && Groups.FirstOrDefault(group => group.Id == chosen.Id) is { } fresh)
        {
            Settings.Group = new Named(fresh.Id, fresh.Name);
        }
        Settings.Channels =
        [
            .. Settings.Channels.Select(channel =>
                Channels.FirstOrDefault(known => known.Id == channel.Id) is { } known ? new Named(known.Id, known.Name) : channel),
        ];
    }

    /// <summary>Takes a group from the server Gengfu is connected to.</summary>
    public void ChooseGroup(Named group)
    {
        Settings.Group = group;
        Settings.ListenersServer = ConnectedWith?.Server;
    }

    /// <summary>Takes the channels ticked on the server Gengfu is connected to, in its tree order.</summary>
    public void ChooseChannels(IReadOnlyList<Named> channels)
    {
        Settings.Channels = channels;
        Settings.ListenersServer = ConnectedWith?.Server;
    }

    /// <summary>Takes the typed text. The default text is kept as "the default", so it follows the lead and the language.</summary>
    public void SetCallText(string text) =>
        Settings.CallText = text == Copy.CallText(Settings.Lead, Language) ? null : text;

    public void ChangeLead(TimeSpan lead)
    {
        Settings.Lead = lead;
        Schedule.ChangeLead(lead);
        SoundChanged();
    }

    /// <param name="choice">"en", "ru", or null for Windows' own.</param>
    public void ChangeLanguage(string? choice)
    {
        Settings.Language = choice;
        Language = AppLanguage.Resolve(choice, _windows);
        AppLanguage.Apply(Language);
        SoundChanged();
    }

    /// <summary>Renders the call again, after its text, voice or file changed, or a voice was added to Windows.</summary>
    public void SoundChanged() => _clip = RenderAsync();

    public ValueTask DisposeAsync() => Connection.DisposeAsync();

    /// <summary>The group drop-down's list: the server's groups once they are in, until then the group already chosen.</summary>
    internal static IReadOnlyList<Named> GroupChoices(bool connected, IReadOnlyList<ServerGroupInfo> groups, Named? chosen) =>
        connected && groups.Count > 0 ? [.. groups.Select(group => new Named(group.Id, group.Name))]
        : chosen is not null ? [chosen]
        : [];

    internal static bool OffersConnectFor(ConnectionStatus status, Login? connectedWith, Login fields) =>
        status == ConnectionStatus.Disconnected || connectedWith != fields;

    internal static string TextOf(AppSettings settings, CultureInfo language) =>
        settings.CallText ?? Copy.CallText(settings.Lead, language);

    internal static CallSound SoundOf(AppSettings settings, CultureInfo language) =>
        settings.SoundFile is { } file ? new CallSound.AudioFile(file)
        : settings.Voice == AppSettings.Gong ? new CallSound.Gong()
        : new CallSound.Speech(TextOf(settings, language), settings.Voice);

    /// <summary>The text, the file's name, or the gong whenever the call plays it, chosen or not.</summary>
    internal static string HeardAs(AppSettings settings, CultureInfo language, CallClip? clip) =>
        clip is { Problem: not CallProblem.None } ? Strings.Gong
        : SoundOf(settings, language) switch
        {
            CallSound.AudioFile file => Path.GetFileName(file.Path),
            CallSound.Speech speech => speech.Text,
            _ => Strings.Gong,
        };

    private Task<CallClip> RenderAsync() => _clip = _audio.RenderAsync(SoundOf(Settings, Language), Language);

    // The lists belong to one server: a new connection starts without them.
    private void ForgetLists()
    {
        Groups = [];
        Channels = [];
    }

    private async Task RefreshListsLaterAsync()
    {
        await Task.Delay(ListsAfter);
        await RefreshListsAsync();
    }
}
