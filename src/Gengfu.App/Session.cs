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

/// <summary>What Undo puts back: the events as they were, and the one removed, or none after Back to default.</summary>
internal sealed record EventsUndo(IReadOnlyList<MatchEvent> Events, MatchEvent? Removed);

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

    // The events' calls, by what they play.
    private readonly Dictionary<CallSound, Task<CallClip>> _clips = [];
    private ConnectionStatus _seen;
    private int _touched;
    private TimeSpan? _readAt;
    private bool _readRefused;
    private string? _settingsSeen;
    private (string, string?) _secretsSeen;
    private TimeSpan _saveAt;
    private string? _settingsWritten;
    private (string, string?) _secretsWritten;

    // The listeners the last whisper reached nobody among, as the server said, and the connection's
    // state when it went out. The connection replaces its state whenever it changes, so while that
    // state stands, the connection is the same one.
    private (WhisperTarget Target, ConnectionState On)? _unheard;

    private bool _usingKey;

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
        Connection = new TeamSpeakConnection(identity, time);
        Language = AppLanguage.Resolve(Settings.Language, windows);
        Clock = new MatchClock(time);
        Schedule = new CallSchedule(Clock, Settings.Events, Settings.Lead);
        Caller = new Caller(Schedule, () => CanCall, RenderAsync, WhisperAsync);
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

    /// <summary>Who hears the calls; null until a group or a channel is chosen.</summary>
    public WhisperTarget? Target => Settings.Listeners switch
    {
        ListenerKind.Group when Settings.Group is { } group => new WhisperTarget.ServerGroup(group.Id),
        ListenerKind.Channels when Settings.Channels.Count > 0 => new WhisperTarget.Channels([.. Settings.Channels.Select(channel => channel.Id)]),
        _ => null,
    };

    /// <summary>Whether a call can go out now: connected, with someone to hear it.</summary>
    public bool CanCall => Target is not null && Connection.State.Status == ConnectionStatus.Connected;

    /// <summary>
    /// Whether the server said the last call reached nobody. It holds until a whisper gets through,
    /// or the connection or the listeners change: it was about them.
    /// </summary>
    public bool NobodyHeard =>
        _unheard is { } unheard && unheard.Target == Target && ReferenceEquals(unheard.On, Connection.State);

    /// <summary>The privilege key typed in Settings. It is never saved: once used, a key is worthless.</summary>
    public string PrivilegeKey { get; private set; } = "";

    /// <summary>
    /// How the server took the last key used. It stands until another key is typed or Connect is
    /// pressed: the connection's own reconnects leave the bot's group as it was.
    /// </summary>
    public KeyResult? KeyAnswer { get; private set; }

    /// <summary>Whether Use would send the typed key: connected, a key typed, and none on its way.</summary>
    public bool CanUseKey => CanUseKeyFor(Connection.State.Status, PrivilegeKey, _usingKey);

    /// <summary>Whether Connect would start a new connection: none is up, or a field changed since this one began.</summary>
    public bool OffersConnect => OffersConnectFor(Connection.State.Status, ConnectedWith, Login.Of(Settings, Password));

    public StatusLine Status => StatusLine.Of(Connection.State, ConnectedWith?.Server, Settings, Language, Clock.Time.Now, NobodyHeard);

    public StatusLine SettingsStatus => StatusLine.InSettings(Connection.State, ConnectedWith?.Server, Settings, Clock.Time.Now);

    /// <summary>
    /// The events before the last Remove or Back to default, while Settings offers to undo it; null
    /// once the events change again or Settings closes.
    /// </summary>
    public EventsUndo? Undo { get; private set; }

    /// <summary>
    /// Makes the calls due now, keeps the server's lists fresh, keeps the settings file in step both
    /// ways, and looks for updates when one is due; the window calls it on every tick.
    /// </summary>
    public void Tick()
    {
        Caller.Tick();
        // Forgotten once it no longer holds, so the first listeners chosen again don't bring it back.
        if (_unheard is not null && !NobodyHeard)
        {
            _unheard = null;
        }
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
        KeyAnswer = null;
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

    /// <summary>Takes the privilege key as typed; the answer to the key before it no longer stands.</summary>
    public void TypeKey(string key)
    {
        if (key != PrivilegeKey)
        {
            PrivilegeKey = key;
            KeyAnswer = null;
        }
    }

    /// <summary>Uses the typed key on the server Gengfu is on; one the server took is spent, so it is cleared.</summary>
    public async Task UseKeyAsync()
    {
        if (!CanUseKey)
        {
            return;
        }
        _usingKey = true;
        try
        {
            KeyUsed(await Connection.UsePrivilegeKeyAsync(PrivilegeKey.Trim()));
        }
        finally
        {
            _usingKey = false;
        }
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

    /// <summary>Renames the event; an empty name, or the jungle's own, makes it the jungle.</summary>
    public void RenameEvent(int index, string name) =>
        EditEvent(index, matchEvent => matchEvent with
        {
            Name = string.IsNullOrWhiteSpace(name) || name == Copy.EventName(MatchEvent.Jungle, Language) ? null : name,
        });

    public void SetSpawns(int index, IReadOnlyList<TimeSpan> spawns) => EditEvent(index, matchEvent => matchEvent with { Spawns = spawns });

    /// <summary>Takes the typed words. The default words are kept as "the default", so they follow the name, the lead and the language.</summary>
    public void SetWords(int index, string words) =>
        EditEvent(index, matchEvent => matchEvent with { Text = words == Copy.DefaultWords(matchEvent, Settings.Lead, Language) ? null : words });

    /// <summary>Gives the event a sound file of its own; null gives it its words back.</summary>
    public void SetSoundFile(int index, string? file)
    {
        EditEvent(index, matchEvent => matchEvent with { SoundFile = file });
        SoundChanged();
    }

    /// <summary>Puts a new event at the end, named for the user to rename, with no spawn times yet.</summary>
    public void AddEvent()
    {
        var was = Settings.Events;
        ChangeEvents([.. was, new MatchEvent { Name = Strings.ResourceManager.GetString(nameof(Strings.NewEvent), Language), Spawns = [] }], [.. was, null]);
        SoundChanged();
    }

    /// <summary>Removes the event at once; Undo puts it back.</summary>
    public void RemoveEvent(int index)
    {
        var was = Settings.Events;
        var left = was.Where((_, i) => i != index).ToList();
        ChangeEvents(left, left);
        Undo = new EventsUndo(was, was[index]);
    }

    /// <summary>Puts the built-in GvG preset back, the jungle alone, at once; Undo puts the events back.</summary>
    public void ResetEvents()
    {
        var was = Settings.Events;
        ChangeEvents([MatchEvent.Jungle]);
        Undo = new EventsUndo(was, null);
        SoundChanged();
    }

    /// <summary>Puts back the events the last Remove or Back to default took.</summary>
    public void UndoEvents()
    {
        if (Undo is { } undo)
        {
            ChangeEvents(undo.Events);
            SoundChanged();
        }
    }

    /// <summary>Settings has closed, and with it the offer to undo.</summary>
    public void ForgetUndo() => Undo = null;

    /// <summary>Whispers the event's call once, now, or the first event's; false when it couldn't go out, there is no event, or nobody heard it.</summary>
    public Task<bool> TestAsync(MatchEvent? matchEvent = null) =>
        (matchEvent ?? Settings.Events.FirstOrDefault()) is { } tested ? Caller.TestAsync(tested) : Task.FromResult(false);

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

    /// <summary>Renders the events' calls again, after words, the voice or a file changed, or a voice was added to Windows.</summary>
    public void SoundChanged()
    {
        _clips.Clear();
        foreach (var sound in Settings.Events.Select(matchEvent => SoundOf(Settings, matchEvent, Language)).Distinct())
        {
            _clips[sound] = _audio.RenderAsync(sound, Language);
        }
    }

    /// <summary>The event's call as last rendered, with why it isn't what it was given when it isn't; null until a render ends.</summary>
    public CallClip? ClipOf(MatchEvent matchEvent) =>
        _clips.GetValueOrDefault(SoundOf(Settings, matchEvent, Language)) is { IsCompletedSuccessfully: true } clip ? clip.Result : null;

    /// <summary>The words a call of the event says: its own, or its name and the lead in the app's language.</summary>
    public string WordsOf(MatchEvent matchEvent) => WordsOf(Settings, matchEvent, Language);

    /// <summary>What listeners hear from a call of <paramref name="matchEvent"/>, in words, for the line under the clock.</summary>
    public string HeardFor(MatchEvent matchEvent) => HeardAs(Settings, matchEvent, Language, ClipOf(matchEvent));

    /// <summary>Writes what changed at once, so the file opened in an editor holds it.</summary>
    public void SaveNow() => Save(flush: true);

    public async ValueTask DisposeAsync()
    {
        _watch.Dispose();
        Save(flush: true);
        await Connection.DisposeAsync();
    }

    /// <summary>
    /// Takes in how a whisper to <paramref name="target"/> went, sent while the connection stood at
    /// <paramref name="on"/>, and says whether anyone may have heard it.
    /// </summary>
    internal bool Whispered(WhisperTarget target, ConnectionState on, WhisperResult result)
    {
        _unheard = result switch
        {
            WhisperResult.ReachedNobody => (target, on),
            WhisperResult.Sent => null,
            _ => _unheard,
        };
        return result == WhisperResult.Sent;
    }

    /// <summary>Takes in how the server took the key; one it took is spent, so it is cleared.</summary>
    internal void KeyUsed(KeyResult answer)
    {
        KeyAnswer = answer;
        if (answer.Outcome == KeyOutcome.Taken)
        {
            PrivilegeKey = "";
        }
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

    // A pasted key often brings spaces along; they are no key.
    internal static bool CanUseKeyFor(ConnectionStatus status, string key, bool sending) =>
        !sending && key.Trim().Length > 0 && status == ConnectionStatus.Connected;

    internal static string WordsOf(AppSettings settings, MatchEvent matchEvent, CultureInfo language) =>
        matchEvent.Text ?? Copy.DefaultWords(matchEvent, settings.Lead, language);

    /// <summary>
    /// What a call of <paramref name="matchEvent"/> plays: its sound file, which gives way to its
    /// words when it can't play; or its words, in the chosen voice or as the gong.
    /// </summary>
    internal static CallSound SoundOf(AppSettings settings, MatchEvent matchEvent, CultureInfo language)
    {
        var words = settings.Voice == AppSettings.Gong
            ? (CallSound)new CallSound.Gong()
            : new CallSound.Speech(WordsOf(settings, matchEvent, language), settings.Voice);
        return matchEvent.SoundFile is { } file ? new CallSound.AudioFile(file, words) : words;
    }

    /// <summary>The words, the file's name, or the gong: whichever the call plays, chosen or not.</summary>
    internal static string HeardAs(AppSettings settings, MatchEvent matchEvent, CultureInfo language, CallClip? clip)
    {
        var sound = SoundOf(settings, matchEvent, language);
        if (sound is CallSound.AudioFile { Otherwise: { } otherwise } && clip?.Problem is CallProblem.FileMissing or CallProblem.FileUnreadable)
        {
            sound = otherwise;
        }
        return clip?.Problem is CallProblem.NoVoice or CallProblem.NoText or CallProblem.SpeechFailed ? Strings.Gong
            : sound switch
            {
                CallSound.AudioFile file => Path.GetFileName(file.Path),
                CallSound.Speech speech => speech.Text,
                _ => Strings.Gong,
            };
    }

    // A call goes out to the listeners chosen now, on the connection as it stands now.
    private async Task<bool> WhisperAsync(IReadOnlyList<byte[]> frames)
    {
        if (Target is not { } target)
        {
            return false;
        }
        var on = Connection.State;
        return Whispered(target, on, await Connection.WhisperAsync(frames, target));
    }

    // A call asks for its sound as it is now.
    private Task<CallClip> RenderAsync(MatchEvent matchEvent)
    {
        var sound = SoundOf(Settings, matchEvent, Language);
        return _clips[sound] = _audio.RenderAsync(sound, Language);
    }

    private void EditEvent(int index, Func<MatchEvent, MatchEvent> edit)
    {
        var was = Settings.Events;
        ChangeEvents([.. was.Select((matchEvent, i) => i == index ? edit(matchEvent) : matchEvent)], was);
    }

    // Every change of the events comes here, and the offer to undo goes with each.
    private void ChangeEvents(IReadOnlyList<MatchEvent> events, IReadOnlyList<MatchEvent?>? was = null)
    {
        Settings.Events = events;
        Schedule.ChangeEvents(events, was);
        Undo = null;
    }

    private void Apply(AppSettings settings)
    {
        var themeChanged = settings.Theme != Settings.Theme;
        Settings = settings;
        Undo = null;
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
