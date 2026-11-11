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
/// the calls' sound, the seat on TeamSpeak, its updates and its log. The window drives it from its
/// tick, on the UI thread. Only the connection's states come in on TSLib's thread, one at a time, for
/// the log: <see cref="ConnectionReported"/> reads the login and the settings there, which the UI
/// thread replaces whole and never changes in place.
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
    private readonly Log _log;
    private readonly SettingsStore _store;
    private readonly Secrets _secrets;
    private readonly IDisposable _watch;
    private readonly bool _secretsUnread;

    // The parts of the calls, by what they play: each event's call, and the words of events called together.
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

    // What the last connection line said: a retry of the same problem adds none, so a server down
    // for an hour is one line, not one every 5 s. The connection reports one state at a time.
    private (string Server, ConnectionStatus, ConnectionProblem, string?) _connectionSaid;

    private bool _saveRefused;

    /// <param name="windows">Windows' display language, for "Same as Windows".</param>
    /// <param name="folder">Where the settings file and the secrets live: <see cref="Home"/> names it.</param>
    /// <param name="log">Gengfu's log; this run's first line is the start.</param>
    /// <param name="install">Velopack's manager when this copy keeps the install up to date; null for any other.</param>
    internal Session(CultureInfo windows, ITimeSource time, CallAudio audio, string folder, Log log, Velopack.UpdateManager? install = null)
    {
        _windows = windows;
        _audio = audio;
        _log = log;
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
        bool lost;
        (Password, identity, lost) = kept ?? ("", null, false);
        _secretsWritten = (Password, identity);
        Language = AppLanguage.Resolve(Settings.Language, windows);
        _log.Write(StartLine(Language, updates: install is not null));
        FileErrorSaid(was: null);
        if (_secretsUnread)
        {
            _log.Write("Windows won't let Gengfu read secrets.bin: this run the bot has a new identity, and the file stays as it is");
        }
        else if (lost)
        {
            _log.Write("secrets.bin doesn't open for this Windows user, as when another user or PC saved it: the bot has a new identity and no server password");
        }
        Updates = new Updates(time, install, log.Write);
        Clock = new MatchClock(time);
        Clock.Changed += (_, change) => _log.Write(ClockLine(change));
        Connection = new TeamSpeakConnection(identity, time);
        Connection.StateChanged += (_, state) => ConnectionReported(state);
        Schedule = new CallSchedule(Clock, Settings.Events, Settings.Lead);
        Caller = new Caller(Schedule, log.Write, () => WhyNoCall, RenderAsync, WhisperAsync);
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
    public WhisperTarget? Target => TargetOf(Settings);

    /// <summary>Whether a call can go out now: connected, with someone to hear it.</summary>
    public bool CanCall => WhyNoCall is null;

    /// <summary>Why no call can go out now, in the log's words; null when one can.</summary>
    internal string? WhyNoCall => WhyNoCallFor(Connection.State.Status, Target);

    /// <summary>Whether a Test call can go out now: a call can, none is playing, and there is an event to call.</summary>
    public bool CanTest => CanTestFor(CanCall, Caller.Testing, Settings.Events.Count);

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

    /// <summary>Takes the event's spawn times; the calls it now shares a spawn time with are rendered together.</summary>
    public void SetSpawns(int index, IReadOnlyList<TimeSpan> spawns)
    {
        EditEvent(index, matchEvent => matchEvent with { Spawns = spawns });
        SoundChanged();
    }

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
        SoundChanged();
    }

    /// <summary>
    /// Puts the built-in GvG preset back at once, the jungle alone at its spawn times, keeping how the
    /// jungle sounds: its words and sound file. Undo puts the events back.
    /// </summary>
    public void ResetEvents()
    {
        var was = Settings.Events;
        var jungle = was.FirstOrDefault(matchEvent => matchEvent.Name is null);
        ChangeEvents([MatchEvent.Jungle with { Text = jungle?.Text, SoundFile = jungle?.SoundFile }]);
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
        TestedEvent(Settings.Events, matchEvent) is { } tested ? Caller.TestAsync(tested) : Task.FromResult(false);

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
        Settings.Voice = VoiceAfterSwitch(Settings.Voice, _audio.InstalledVoices(), Language);
        SoundChanged();
    }

    /// <param name="choice">Light, dark, or null for Windows' own.</param>
    public void ChangeTheme(Theme? choice)
    {
        Settings.Theme = choice;
        AppTheme.Apply(choice);
    }

    /// <summary>
    /// Renders the calls again, each event's and those of events at one spawn time, after words, the
    /// voice, a file or spawn times changed, or a voice was added to Windows.
    /// </summary>
    public void SoundChanged()
    {
        _clips.Clear();
        var sounds = Settings.Events
            .Select(matchEvent => SoundOf(Settings, matchEvent, Language))
            .Concat(Caller.Joined(Schedule.Calls).SelectMany(calls => SoundsOf(Settings, [.. calls.Select(call => call.Event)], Language)));
        foreach (var sound in sounds.Distinct())
        {
            _clips[sound] = _audio.RenderAsync(sound, Language);
        }
    }

    /// <summary>The event's call as last rendered, with why it isn't what it was given when it isn't; null until a render ends.</summary>
    public CallClip? ClipOf(MatchEvent matchEvent) => ClipFor(SoundOf(Settings, matchEvent, Language));

    /// <summary>The words a call of the event says: its own, or its name and the lead in the app's language.</summary>
    public string WordsOf(MatchEvent matchEvent) => WordsOf(Settings, matchEvent, Language);

    /// <summary>What listeners hear from one call of <paramref name="events"/>, at one spawn time, in words, for the line under the clock.</summary>
    public string HeardFor(IReadOnlyList<MatchEvent> events) => HeardAs(Settings, events, Language, ClipFor);

    public async ValueTask DisposeAsync()
    {
        _watch.Dispose();
        Save(flush: true);
        await Connection.DisposeAsync();
        _log.Write("Gengfu closed");
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
        _log.Write(KeyLine(answer));
        if (answer.Outcome == KeyOutcome.Taken)
        {
            PrivilegeKey = "";
        }
    }

    /// <summary>Takes in a state the connection reports, and logs it when it says something new.</summary>
    internal void ConnectionReported(ConnectionState state)
    {
        // The login's password stays here: a record prints every field.
        var (server, nickname, _) = ConnectedWith ?? Login.Of(Settings, "");
        var said = (server, state.Status, state.Problem, state.Detail);
        if (said != _connectionSaid)
        {
            _connectionSaid = said;
            _log.Write(ConnectionLine(state, server, nickname, Settings, Clock.Time.Now));
        }
    }

    /// <summary>Takes in the settings file as it is on disk now, after an edit.</summary>
    internal void Reload()
    {
        var was = FileError;
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
        FileErrorSaid(was);
    }

    /// <summary>
    /// The frames of one call of <paramref name="events"/>, at one spawn time, its parts as they sound
    /// now, a sound two share once, and what they play. All are asked for before any is awaited, so
    /// the clips are written only on the caller's thread.
    /// </summary>
    internal async Task<CallRender> RenderAsync(IReadOnlyList<MatchEvent> events)
    {
        var language = Language;
        var sounds = SoundsOf(Settings, events, language);
        var parts = sounds.Select(sound => _clips[sound] = _audio.RenderAsync(sound, language)).ToList();
        var clips = await Task.WhenAll(parts);
        return new CallRender(CallAudio.Join([.. clips.Select(clip => clip.Frames)]), PlayedAs(sounds, clips, language));
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

    internal static bool CanTestFor(bool canCall, bool testing, int events) => canCall && !testing && events > 0;

    internal static WhisperTarget? TargetOf(AppSettings settings) => settings.Listeners switch
    {
        ListenerKind.Group when settings.Group is { } group => new WhisperTarget.ServerGroup(group.Id),
        ListenerKind.Channels when settings.Channels.Count > 0 => new WhisperTarget.Channels([.. settings.Channels.Select(channel => channel.Id)]),
        _ => null,
    };

    internal static string? WhyNoCallFor(ConnectionStatus status, WhisperTarget? target) =>
        status != ConnectionStatus.Connected ? "not connected"
        : target is null ? "nobody chosen to hear the calls"
        : null;

    /// <summary>How a whisper to <paramref name="whom"/> went, in the log's words.</summary>
    internal static string HowItWent(WhisperResult result, string whom) => result switch
    {
        WhisperResult.Sent => "sent to " + whom,
        WhisperResult.ReachedNobody => "nobody heard it: the server said it reached nobody in " + whom,
        _ => "not sent: not connected",
    };

    /// <summary>
    /// The voice to keep on a switch to <paramref name="language"/>. The events' words follow the
    /// language, so a chosen voice that can't speak it gives way to the language's own. The gong
    /// stays, and so does a voice that isn't installed, which speaks nothing anyway.
    /// </summary>
    internal static string? VoiceAfterSwitch(string? chosen, IReadOnlyList<Voice> installed, CultureInfo language) =>
        installed.FirstOrDefault(voice => voice.Id == chosen) is { } voice && !Voices.Speaks(voice, language) ? null : chosen;

    // The Test call of Calls and of the main window plays the first event's call; an event's own, its call.
    internal static MatchEvent? TestedEvent(IReadOnlyList<MatchEvent> events, MatchEvent? pressed) => pressed ?? events.FirstOrDefault();

    internal static string WordsOf(AppSettings settings, MatchEvent matchEvent, CultureInfo language) =>
        matchEvent.Text ?? Copy.DefaultWords(matchEvent, settings.Lead, language);

    /// <summary>
    /// What a call of <paramref name="matchEvent"/> plays: its sound file, which gives way to its
    /// words when it can't play; or its words, in the chosen voice or as the gong.
    /// </summary>
    internal static CallSound SoundOf(AppSettings settings, MatchEvent matchEvent, CultureInfo language)
    {
        var words = Spoken(settings, WordsOf(settings, matchEvent, language));
        return matchEvent.SoundFile is { } file ? new CallSound.AudioFile(file, words) : words;
    }

    /// <summary>
    /// What one call of <paramref name="events"/>, at one spawn time, plays, part by part. Those that
    /// say their name and the lead say them in one sentence, "Jungle and Boss in thirty seconds", in
    /// the place of the first of them; the rest play their own words or sound file, in their order.
    /// A part two of them share plays once.
    /// </summary>
    internal static IReadOnlyList<CallSound> SoundsOf(AppSettings settings, IReadOnlyList<MatchEvent> events, CultureInfo language)
    {
        var named = events.Where(SaysItsName).ToList();
        return
        [
            .. events
                .Select(matchEvent => SaysItsName(matchEvent)
                    ? Spoken(settings, Copy.DefaultWords(named, settings.Lead, language))
                    : SoundOf(settings, matchEvent, language))
                .Distinct(),
        ];
    }

    /// <summary>
    /// What listeners hear from one call of <paramref name="events"/>, part by part: the words, a
    /// file's name, or the gong, each once, as the call plays a sound its parts share once.
    /// </summary>
    /// <param name="clipOf">A part as last rendered; null until a render ends.</param>
    internal static string HeardAs(AppSettings settings, IReadOnlyList<MatchEvent> events, CultureInfo language, Func<CallSound, CallClip?> clipOf) =>
        string.Join(" · ", SoundsOf(settings, events, language).Select(sound => HeardAs(sound, clipOf(sound))).Distinct());

    /// <summary>
    /// What one call played, part by part, in the log: the words, a file's name, or the gong, and
    /// why a part isn't what it was given when it isn't.
    /// </summary>
    internal static string PlayedAs(IReadOnlyList<CallSound> sounds, IReadOnlyList<CallClip> clips, CultureInfo language) =>
        string.Join(" · ", sounds.Zip(clips, (sound, clip) => PlayedAs(sound, clip, language)).Distinct());

    /// <summary>A run's first line in the log: what places the rest, the version first.</summary>
    internal static string StartLine(CultureInfo language, bool updates)
    {
        var offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"Gengfu {AppVersion.Current} started · Windows {Environment.OSVersion.Version} · {language.EnglishName} · UTC{(offset < TimeSpan.Zero ? "-" : "+")}{offset:hh\\:mm}{(updates ? "" : " · updates off")}");
    }

    /// <summary>The connection's state in the log: the status line's words, with what the line leaves out.</summary>
    /// <param name="now">The time on the clock the connection's next attempt is set on.</param>
    internal static string ConnectionLine(ConnectionState state, string server, string nickname, AppSettings settings, TimeSpan now)
    {
        var again = state.RetryAt is { } at && at > now ? "trying again in " + Copy.Span(at - now) : "trying again…";
        return state switch
        {
            { Status: ConnectionStatus.Connected } => TargetOf(settings) is { } target ? "Connected · whispering to " + Whom(target, settings) : "Connected · choose who hears the calls",
            { Status: ConnectionStatus.Connecting, Problem: ConnectionProblem.None } => $"Connecting to {server} as {nickname}…",
            { Status: ConnectionStatus.Connecting, Problem: ConnectionProblem.IdentityTooWeak } => $"Raising the bot's security level to {state.Detail} — can take minutes",
            { Status: ConnectionStatus.Connecting, Problem: ConnectionProblem.AddressNotFound } => $"Can't find {server} — {again}",
            { Status: ConnectionStatus.Connecting, Problem: ConnectionProblem.ServerShutdown } => "The server shut down — " + again,
            { Status: ConnectionStatus.Connecting, Problem: ConnectionProblem.KickedByServer } => "The server kicked the bot — " + again,
            { Status: ConnectionStatus.Connecting, Problem: ConnectionProblem.ServerFull } => "The server is full — " + again,
            { Status: ConnectionStatus.Connecting, Problem: ConnectionProblem.FloodProtection } => "The server is limiting connections — " + again,
            { Status: ConnectionStatus.Connecting, Problem: ConnectionProblem.Refused } => $"The server refused: {state.Detail} — {again}",
            // The status line leaves out the error an attempt failed on.
            { Status: ConnectionStatus.Connecting, Detail: { } error } => $"No answer from {server} ({error}) — {again}",
            { Status: ConnectionStatus.Connecting } => $"No answer from {server} — {again}",
            { Problem: ConnectionProblem.WrongPassword } => "Not connected — wrong server password",
            { Problem: ConnectionProblem.Kicked } => "Not connected — kicked from the server",
            { Problem: ConnectionProblem.Banned } => "Not connected — banned from the server",
            { Problem: ConnectionProblem.IdentityTooWeak } => $"Not connected — server wants security level {state.Detail}",
            _ => "Not connected",
        };
    }

    /// <summary>How the server took a privilege key, in the log; never the key.</summary>
    internal static string KeyLine(KeyResult answer) => answer switch
    {
        { Outcome: KeyOutcome.Taken, Detail: { } group } => $"Privilege key used: the bot is in {group} now",
        { Outcome: KeyOutcome.Taken } => "Privilege key used: the server gave the bot the key's group",
        { Outcome: KeyOutcome.Unknown } => "Privilege key refused: the server doesn't know it, as when it's used already or mistyped",
        { Outcome: KeyOutcome.NotAllowed } => "Privilege key refused: the server doesn't let the bot use privilege keys",
        { Outcome: KeyOutcome.Refused } => "Privilege key refused: " + answer.Detail,
        _ => "Privilege key: the connection dropped before the server answered",
    };

    /// <summary>A Start, a Stop or a correction of the clock, in the log, its times as the game shows them.</summary>
    internal static string ClockLine(ClockChange change) => change.Kind switch
    {
        ClockChangeKind.Started => "Clock started at " + MatchClock.Format(change.To),
        ClockChangeKind.Stopped => "Clock stopped at " + MatchClock.Format(change.From),
        _ => $"Clock set to {MatchClock.Format(change.To)}, from {MatchClock.Format(change.From)}",
    };

    /// <summary>The settings file's error in the log, as Settings' bar says it; with none, that the file reads cleanly again.</summary>
    internal static string FileErrorLine(SettingsError? error) => error switch
    {
        null => "settings.json reads without an error again",
        { Line: { } line, Setting: { } setting } => $"settings.json has an error on line {line} ({setting}). Until it's fixed, Gengfu works with the settings it has and saves nothing",
        { Line: { } line } => $"settings.json has an error on line {line}. Until it's fixed, Gengfu works with the settings it has and saves nothing",
        _ => "Windows won't let Gengfu read settings.json. Until it can, Gengfu works with the settings it has and saves nothing",
    };

    /// <summary>The words, the file's name, or the gong: whichever the part plays, chosen or not.</summary>
    private static string HeardAs(CallSound sound, CallClip? clip)
    {
        if (sound is CallSound.AudioFile { Otherwise: { } otherwise } && clip?.Problem is CallProblem.FileMissing or CallProblem.FileUnreadable)
        {
            sound = clip.Instead == CallProblem.None ? otherwise : new CallSound.Gong();
        }
        return clip?.Problem is CallProblem.NoVoice or CallProblem.NoText or CallProblem.SpeechFailed ? Strings.Gong
            : sound switch
            {
                CallSound.AudioFile file => Path.GetFileName(file.Path),
                CallSound.Speech speech => speech.Text,
                _ => Strings.Gong,
            };
    }

    private static string PlayedAs(CallSound sound, CallClip clip, CultureInfo language) => sound switch
    {
        CallSound.Speech speech when clip.Problem == CallProblem.None => Quoted(speech.Text),
        CallSound.Speech speech => "the gong, since " + Unspoken(clip.Problem, speech.Text, clip.Detail, language),
        CallSound.AudioFile file when clip.Problem == CallProblem.None => Path.GetFileName(file.Path),
        CallSound.AudioFile file => InPlaceOf(file, clip, language),
        _ => "the gong",
    };

    // What played in place of a file that couldn't: its words, or the gong when they couldn't either.
    private static string InPlaceOf(CallSound.AudioFile file, CallClip clip, CultureInfo language)
    {
        var name = Path.GetFileName(file.Path);
        var why = clip.Problem == CallProblem.FileMissing ? name + " isn't on this PC" : $"Windows can't play {name}: {clip.Detail?.TrimEnd('.')}";
        return file.Otherwise switch
        {
            CallSound.Speech words when clip.Instead == CallProblem.None => $"{Quoted(words.Text)}, since {why}",
            CallSound.Speech words => $"the gong, since {why}, and {Unspoken(clip.Instead, words.Text, null, language)}",
            _ => "the gong, since " + why,
        };
    }

    // Why words played the gong in their place.
    private static string Unspoken(CallProblem problem, string words, string? detail, CultureInfo language) => problem switch
    {
        CallProblem.NoVoice => "no installed voice speaks " + language.EnglishName,
        CallProblem.NoText => "there are no words to say",
        _ => $"Windows couldn't speak {Quoted(words)}" + (detail is null ? "" : ": " + detail.TrimEnd('.')),
    };

    private static string Quoted(string words) => $"“{words.Trim()}”";

    // The listeners a target made from these settings reaches, in the log's words. It only names
    // them: whether a call goes out is the target's to say.
    private static string Whom(WhisperTarget target, AppSettings settings) =>
        target is WhisperTarget.ServerGroup
            ? $"the {settings.Group?.Name} group"
            : string.Join(", ", settings.Channels.Select(channel => channel.Name));

    private static bool SaysItsName(MatchEvent matchEvent) => matchEvent.Text is null && matchEvent.SoundFile is null;

    // Words in the chosen voice, or the gong chosen in its place.
    private static CallSound Spoken(AppSettings settings, string words) =>
        settings.Voice == AppSettings.Gong ? new CallSound.Gong() : new CallSound.Speech(words, settings.Voice);

    // A call goes out to the listeners chosen now, on the connection as it stands now.
    private async Task<CallWhisper> WhisperAsync(IReadOnlyList<byte[]> frames)
    {
        if (Target is not { } target)
        {
            return new CallWhisper(false, "not sent: nobody chosen to hear the calls");
        }
        var (on, whom) = (Connection.State, Whom(target, Settings));
        var result = await Connection.WhisperAsync(frames, target);
        return new CallWhisper(Whispered(target, on, result), HowItWent(result, whom));
    }

    private CallClip? ClipFor(CallSound sound) =>
        _clips.GetValueOrDefault(sound) is { IsCompletedSuccessfully: true } clip ? clip.Result : null;

    private void EditEvent(int index, Func<MatchEvent, MatchEvent> edit)
    {
        var was = Settings.Events;
        ChangeEvents([.. was.Select((matchEvent, i) => i == index ? edit(matchEvent) : matchEvent)], was);
    }

    // Every edit of the events in the app comes here, and the offer to undo goes with each.
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
        var (refused, saved) = (false, false);
        // Never over a file Gengfu couldn't read: the edit in it would be lost.
        if (FileError is null && settings != _settingsWritten)
        {
            if (_store.Save(Settings))
            {
                _settingsWritten = settings;
                saved = true;
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
                saved = true;
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
        // A line when Windows starts refusing, and one when a save gets through again.
        if (refused ? !_saveRefused : _saveRefused && saved)
        {
            _saveRefused = refused;
            _log.Write(refused ? "Windows won't let Gengfu save its settings; it tries again every 2 s" : "Gengfu saves its settings again");
        }
    }

    // A line whenever the settings file's error changes: found, another found, or gone.
    private void FileErrorSaid(SettingsError? was)
    {
        if (FileError != was)
        {
            _log.Write(FileErrorLine(FileError));
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
