using System.Diagnostics.CodeAnalysis;
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
/// One running Gengfu: what Settings holds, kept in the settings file, the match clock and its calls,
/// the calls' sound, the seat on TeamSpeak, and its updates. The window drives it from its tick, on
/// the UI thread.
/// </summary>
internal sealed class Session : IAsyncDisposable
{
    // The server sends its groups a moment after the connection is up.
    private static readonly TimeSpan ListsAfter = TimeSpan.FromSeconds(1);

    // Typing writes the files once it pauses, not on every keystroke.
    private static readonly TimeSpan SaveAfter = TimeSpan.FromMilliseconds(500);

    // An editor saving the file may touch it several times in a row; it is read once that settles.
    private static readonly TimeSpan ReadAfter = TimeSpan.FromMilliseconds(250);

    // A file Windows won't let Gengfu read or write, perhaps held by another program, is tried again now and then.
    private static readonly TimeSpan TryAgainAfter = TimeSpan.FromSeconds(2);

    private static readonly SettingsError CantRead = new(null, null);

    private readonly CultureInfo _windows;
    private readonly CallAudio _audio;
    private readonly SettingsStore _store;
    private readonly Secrets _secrets;
    private readonly IDisposable _watch;
    private readonly bool _secretsUnread;

    // The clips of the events' own texts, by text.
    private readonly Dictionary<string, Task<CallClip>> _clips = [];
    private Task<CallClip> _clip;
    private ConnectionStatus _seen;
    private int _touched;
    private TimeSpan? _readAt;
    private bool _readRefused;
    private string? _settingsSeen;
    private (string, string?) _secretsSeen;
    private TimeSpan _saveAt;
    private string? _settingsWritten;
    private (string, string?) _secretsWritten;

    /// <param name="windows">Windows' display language, for "Same as Windows".</param>
    /// <param name="folder">Where the settings file and the secrets live: <see cref="Home"/> names it.</param>
    /// <param name="install">Velopack's manager when this copy keeps the install up to date; null for any other.</param>
    internal Session(CultureInfo windows, ITimeSource time, CallAudio audio, string folder, Velopack.UpdateManager? install = null)
    {
        Updates = new Updates(time, install);
        _windows = windows;
        _audio = audio;
        _store = new SettingsStore(Path.Combine(folder, "settings.json"));
        _secrets = new Secrets(Path.Combine(folder, "secrets.bin"));
        switch (_store.Load())
        {
            case SettingsLoad.Loaded loaded:
                Settings = loaded.Settings;
                _settingsWritten = SettingsStore.Serialize(Settings);
                break;
            case SettingsLoad.Broken broken:
                FileError = broken.Error;
                break;
            case SettingsLoad.Unreadable:
                FileError = CantRead;
                _readRefused = true;
                break;
        }
        // Unread, the secrets file still holds the identity the bot's server groups belong to: this
        // run makes do with a new one and never writes over the file.
        var kept = _secrets.Load();
        _secretsUnread = kept is null;
        string? identity;
        (Password, identity) = kept ?? ("", null);
        _secretsWritten = (Password, identity);
        Connection = new TeamSpeakConnection(identity);
        Language = AppLanguage.Resolve(Settings.Language, windows);
        Clock = new MatchClock(time);
        Schedule = new CallSchedule(Clock, Settings.Events, Settings.Lead);
        Caller = new Caller(
            Schedule,
            () => CanCall,
            RenderAsync,
            frames => Target is { } target ? Connection.WhisperAsync(frames, target) : Task.FromResult(false));
        SoundChanged();
        _watch = _store.Watch(() => Interlocked.Exchange(ref _touched, 1));
    }

    public AppSettings Settings { get; private set; } = new();

    /// <summary>The server password, kept out of <see cref="Settings"/>.</summary>
    public string Password { get; set; } = "";

    public string SettingsFile => _store.FilePath;

    /// <summary>What is wrong with the settings file; while it is set, Gengfu never writes over the file.</summary>
    public SettingsError? FileError { get; private set; }

    /// <summary>Raised when an edit of the settings file replaces <see cref="Settings"/>, so the views show them afresh.</summary>
    public event EventHandler? SettingsReplaced;

    /// <summary>The language the strings show in and a voice is picked by.</summary>
    public CultureInfo Language { get; private set; }

    public MatchClock Clock { get; }

    public CallSchedule Schedule { get; }

    public Caller Caller { get; }

    public TeamSpeakConnection Connection { get; }

    public Updates Updates { get; }

    /// <summary>What the current connection was started with; null after Disconnect or before any Connect.</summary>
    public Login? ConnectedWith { get; private set; }

    /// <summary>The server's groups, as of the last look; refreshed once connected and when a list opens.</summary>
    public IReadOnlyList<ServerGroupInfo> Groups { get; private set; } = [];

    /// <summary>The server's channels in its tree order, as of the last look.</summary>
    public IReadOnlyList<ChannelInfo> Channels { get; private set; } = [];

    /// <summary>The call as Settings sets it, last rendered, with why it plays the gong when it does; null until the first render ends.</summary>
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

    /// <summary>
    /// Makes the calls due now, keeps the server's lists fresh, keeps the settings file in step both
    /// ways, and looks for updates when one is due; the window calls it on every tick.
    /// </summary>
    public void Tick()
    {
        Caller.Tick();
        _ = Updates.Tick();
        var status = Connection.State.Status;
        if (status == ConnectionStatus.Connected && _seen != ConnectionStatus.Connected)
        {
            _ = RefreshListsLaterAsync();
        }
        _seen = status;
        ReadIfEdited();
        Save(flush: false);
    }

    /// <summary>Connects with the saved server, if there is one; the window calls it at start.</summary>
    public Task ConnectSavedAsync() => string.IsNullOrWhiteSpace(Settings.Server) ? Task.CompletedTask : ConnectAsync();

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

    /// <param name="choice">Light, dark, or null for Windows' own.</param>
    public void ChangeTheme(Theme? choice)
    {
        Settings.Theme = choice;
        AppTheme.Apply(choice);
    }

    /// <summary>Renders the calls again, after a text, the voice or the file changed, or a voice was added to Windows.</summary>
    [MemberNotNull(nameof(_clip))]
    public void SoundChanged()
    {
        _clip = Render(null);
        _clips.Clear();
        foreach (var matchEvent in Settings.Events.Where(matchEvent => matchEvent.Text is not null).DistinctBy(matchEvent => matchEvent.Text))
        {
            _clips[matchEvent.Text!] = Render(matchEvent);
        }
    }

    /// <summary>What listeners hear from a call of <paramref name="matchEvent"/>, in words, for the line under the clock.</summary>
    public string HeardFor(MatchEvent matchEvent)
    {
        var clip = matchEvent.Text is { } text ? _clips.GetValueOrDefault(text) : _clip;
        return HeardAs(Settings, matchEvent, Language, clip is { IsCompletedSuccessfully: true } ? clip.Result : null);
    }

    /// <summary>Writes what changed at once, so the file opened in an editor holds it.</summary>
    public void SaveNow() => Save(flush: true);

    public async ValueTask DisposeAsync()
    {
        _watch.Dispose();
        Save(flush: true);
        await Connection.DisposeAsync();
    }

    /// <summary>Takes in the settings file as it is on disk now, after an edit.</summary>
    internal void Reload()
    {
        var load = _store.Load();
        _readRefused = load is SettingsLoad.Unreadable;
        switch (load)
        {
            case SettingsLoad.Loaded loaded:
                FileError = null;
                Apply(loaded.Settings);
                break;
            case SettingsLoad.Broken broken:
                FileError = broken.Error;
                break;
            case SettingsLoad.Missing:
                // Deleted: the next save writes what Gengfu holds, so the file is there to open again.
                FileError = null;
                _settingsWritten = null;
                break;
            case SettingsLoad.Unreadable:
                // An error already found stays shown until the file reads again.
                FileError ??= CantRead;
                break;
            case SettingsLoad.Unchanged when FileError == CantRead:
                // Readable again, and as Gengfu last knew it: the file read cleanly then.
                FileError = null;
                break;
        }
    }

    /// <summary>The group drop-down's list: the server's groups once they are in, until then the group already chosen.</summary>
    internal static IReadOnlyList<Named> GroupChoices(bool connected, IReadOnlyList<ServerGroupInfo> groups, Named? chosen) =>
        connected && groups.Count > 0 ? [.. groups.Select(group => new Named(group.Id, group.Name))]
        : chosen is not null ? [chosen]
        : [];

    internal static bool OffersConnectFor(ConnectionStatus status, Login? connectedWith, Login fields) =>
        status == ConnectionStatus.Disconnected || connectedWith != fields;

    internal static string TextOf(AppSettings settings, CultureInfo language) =>
        settings.CallText ?? Copy.CallText(settings.Lead, language);

    /// <summary>
    /// What a call of <paramref name="matchEvent"/> plays: its own text in the chosen voice, else what
    /// Settings sets. Null asks for Settings' own, which Test call plays.
    /// </summary>
    internal static CallSound SoundOf(AppSettings settings, MatchEvent? matchEvent, CultureInfo language) =>
        matchEvent?.Text is { } text ? Spoken(settings, text)
        : settings.SoundFile is { } file ? new CallSound.AudioFile(file)
        : Spoken(settings, TextOf(settings, language));

    /// <summary>The text, the file's name, or the gong whenever the call plays it, chosen or not.</summary>
    internal static string HeardAs(AppSettings settings, MatchEvent? matchEvent, CultureInfo language, CallClip? clip) =>
        clip is { Problem: not CallProblem.None } ? Strings.Gong
        : SoundOf(settings, matchEvent, language) switch
        {
            CallSound.AudioFile file => Path.GetFileName(file.Path),
            CallSound.Speech speech => speech.Text,
            _ => Strings.Gong,
        };

    private static CallSound Spoken(AppSettings settings, string text) =>
        settings.Voice == AppSettings.Gong ? new CallSound.Gong() : new CallSound.Speech(text, settings.Voice);

    private Task<CallClip> Render(MatchEvent? matchEvent) => _audio.RenderAsync(SoundOf(Settings, matchEvent, Language), Language);

    // A call asks for its sound as it is now: its event's own, or Settings' for the rest and Test call.
    private Task<CallClip> RenderAsync(MatchEvent? matchEvent)
    {
        if (matchEvent?.Text is not { } text)
        {
            return _clip = Render(null);
        }
        return _clips[text] = Render(matchEvent);
    }

    private void Apply(AppSettings settings)
    {
        var themeChanged = settings.Theme != Settings.Theme;
        Settings = settings;
        _settingsWritten = SettingsStore.Serialize(settings);
        // The language and the theme are the whole app's, so each is set only when the file changes it.
        if (AppLanguage.Resolve(settings.Language, _windows) is var language && language != Language)
        {
            Language = language;
            AppLanguage.Apply(language);
        }
        if (themeChanged)
        {
            AppTheme.Apply(settings.Theme);
        }
        Schedule.ChangeLead(settings.Lead);
        Schedule.ChangeEvents(settings.Events);
        SoundChanged();
        SettingsReplaced?.Invoke(this, EventArgs.Empty);
    }

    // The file is read again once an edit has settled, and after a read Windows refused, now and
    // then, whatever error shows: the refused read may have been of a fix.
    private void ReadIfEdited()
    {
        var now = Clock.Time.Now;
        if (Interlocked.Exchange(ref _touched, 0) == 1)
        {
            _readAt = now + ReadAfter;
        }
        else if (_readRefused && _readAt is null)
        {
            _readAt = now + TryAgainAfter;
        }
        if (_readAt <= now)
        {
            _readAt = null;
            Reload();
        }
    }

    // Writes the settings and the secrets once they have stood still a moment; a flush writes them now.
    private void Save(bool flush)
    {
        var now = Clock.Time.Now;
        var settings = SettingsStore.Serialize(Settings);
        (string, string?) secrets = (Password, Connection.Identity);
        if (settings != _settingsSeen || secrets != _secretsSeen)
        {
            (_settingsSeen, _secretsSeen, _saveAt) = (settings, secrets, now + SaveAfter);
        }
        if (!flush && now < _saveAt)
        {
            return;
        }
        var refused = false;
        // Never over a file Gengfu couldn't read: the edit in it would be lost.
        if (FileError is null && settings != _settingsWritten)
        {
            if (_store.Save(Settings))
            {
                _settingsWritten = settings;
            }
            else
            {
                refused = true;
            }
        }
        if (!_secretsUnread && secrets != _secretsWritten)
        {
            if (_secrets.Save(Password, Connection.Identity))
            {
                _secretsWritten = secrets;
            }
            else
            {
                refused = true;
            }
        }
        if (refused)
        {
            _saveAt = now + TryAgainAfter;
        }
    }

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
