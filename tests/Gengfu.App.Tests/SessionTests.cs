using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using Gengfu.App.Speech;
using Gengfu.Core;
using Gengfu.Core.Settings;
using Gengfu.TeamSpeak;

namespace Gengfu.App.Tests;

// The connection's TSLib thread keeps the test host alive until the connection is disposed. The
// settings file and the secrets are real files, in a folder of the test's own.
public sealed class SessionTests : IAsyncLifetime
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

    private readonly FakeTimeSource _time = new();
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "gengfu-tests", Guid.NewGuid().ToString("N"));
    private Session _session;

    public SessionTests() => _session = NewSession();

    private string SettingsFile => Path.Combine(_folder, "settings.json");

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _session.DisposeAsync();
        Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public void An_event_says_its_name_and_the_lead_until_the_user_types_its_own_words()
    {
        Assert.Equal("Jungle in thirty seconds", _session.WordsOf(_session.Settings.Events[0]));

        _session.ChangeLead(TimeSpan.FromSeconds(45));
        Assert.Equal("Jungle in forty-five seconds", _session.WordsOf(_session.Settings.Events[0]));
        Assert.Equal(TimeSpan.FromSeconds(45), _session.Schedule.Lead);

        // Typing the default back keeps it the default.
        _session.SetWords(0, "Jungle in forty-five seconds");
        Assert.Null(_session.Settings.Events[0].Text);

        _session.SetWords(0, "Jungle, go go go");
        _session.ChangeLead(TimeSpan.FromSeconds(15));
        Assert.Equal("Jungle, go go go", _session.WordsOf(_session.Settings.Events[0]));
    }

    [Fact]
    public void A_named_event_without_words_of_its_own_says_its_own_name_never_the_jungle_s()
    {
        _session.AddEvent();
        var added = _session.Settings.Events[1];
        Assert.Equal(("New event", 0), (added.Name, added.Spawns.Count));
        Assert.Equal("New event in thirty seconds", _session.WordsOf(added));

        _session.RenameEvent(1, "Boss");
        Assert.Equal("Boss in thirty seconds", _session.WordsOf(_session.Settings.Events[1]));
        Assert.Equal("Jungle in thirty seconds", _session.WordsOf(_session.Settings.Events[0]));

        // An empty name, or the jungle's own, makes it the jungle.
        _session.RenameEvent(1, " ");
        Assert.Null(_session.Settings.Events[1].Name);
        _session.RenameEvent(1, "Boss");
        _session.RenameEvent(1, "Jungle");
        Assert.Null(_session.Settings.Events[1].Name);
    }

    [Fact]
    public void An_event_renamed_or_given_other_times_mid_match_keeps_its_calls_so_none_goes_out_twice()
    {
        _session.Clock.Start();
        // 25:29, in the first call's window.
        _time.Advance(TimeSpan.FromSeconds(571));
        Assert.Single(_session.Schedule.TakeDue());

        _session.RenameEvent(0, "Forest");
        _session.SetSpawns(0, [TimeSpan.FromMinutes(25), TimeSpan.FromMinutes(17)]);
        _session.SetWords(0, "Forest, go go go");

        Assert.Empty(_session.Schedule.TakeDue());
        Assert.Equal(CallState.Calling, _session.Schedule.StateOf(_session.Schedule.Calls[0]));
        Assert.Equal(new[] { "25:00", "17:00" }, _session.Schedule.Calls.Select(call => MatchClock.Format(call.Spawn)));
    }

    [Fact]
    public void A_Test_call_goes_out_only_when_a_call_can_none_is_playing_and_there_is_an_event_to_call()
    {
        Assert.True(Session.CanTestFor(canCall: true, testing: false, events: 1));
        Assert.False(Session.CanTestFor(canCall: true, testing: false, events: 0));
        Assert.False(Session.CanTestFor(canCall: false, testing: false, events: 1));
        Assert.False(Session.CanTestFor(canCall: true, testing: true, events: 1));
    }

    [Fact]
    public void Calls_Test_call_plays_the_first_event_s_call_and_an_event_s_own_Test_call_its_call()
    {
        var boss = new MatchEvent { Name = "Boss", Spawns = [] };

        Assert.Same(MatchEvent.Jungle, Session.TestedEvent([MatchEvent.Jungle, boss], null));
        Assert.Same(boss, Session.TestedEvent([MatchEvent.Jungle, boss], boss));
        Assert.Null(Session.TestedEvent([], null));
    }

    [Fact]
    public void Remove_and_Back_to_default_act_at_once_and_Undo_puts_the_events_back_until_they_change_again()
    {
        _session.AddEvent();
        _session.RenameEvent(1, "Boss");
        var (jungle, boss) = (_session.Settings.Events[0], _session.Settings.Events[1]);
        Assert.Null(_session.Undo);

        _session.RemoveEvent(0);
        Assert.Equal(new[] { boss }, _session.Settings.Events);
        Assert.Same(jungle, _session.Undo?.Removed);
        _session.UndoEvents();
        Assert.Equal(new[] { jungle, boss }, _session.Settings.Events);
        Assert.Null(_session.Undo);

        _session.ResetEvents();
        Assert.True(_session.Settings.EventsAreDefault);
        Assert.Equal(2, _session.Undo?.Events.Count);
        Assert.Null(_session.Undo?.Removed);
        _session.UndoEvents();
        Assert.Equal(new[] { jungle, boss }, _session.Settings.Events);

        // Another change takes the offer away, and so does leaving Settings.
        _session.RemoveEvent(1);
        _session.SetSpawns(0, [TimeSpan.FromMinutes(25)]);
        Assert.Null(_session.Undo);
        _session.ResetEvents();
        _session.ForgetUndo();
        _session.UndoEvents();
        Assert.True(_session.Settings.EventsAreDefault);
    }

    [Fact]
    public async Task Events_edited_in_Settings_are_saved_and_come_back_on_the_next_start()
    {
        _session.AddEvent();
        _session.RenameEvent(1, "Босс");
        _session.SetSpawns(1, [TimeSpan.FromMinutes(25), TimeSpan.FromMinutes(15)]);
        _session.SetSoundFile(1, @"C:\Звуки\босс.mp3");
        _session.SetWords(0, "Лес, вперёд");
        Settle();

        await RestartAsync();

        Assert.Equal(
            new (string?, string, string?, string?)[] { (null, "25:00, 20:00, 15:00, 10:00, 05:00", "Лес, вперёд", null), ("Босс", "25:00, 15:00", null, @"C:\Звуки\босс.mp3") },
            _session.Settings.Events.Select(e => (e.Name, MatchEvent.WriteSpawns(e.Spawns), e.Text, e.SoundFile)));
        Assert.Equal(7, _session.Schedule.Calls.Count);
    }

    [Fact]
    public async Task An_event_s_file_that_isn_t_on_this_PC_gives_way_to_its_words_and_its_call_says_why()
    {
        await _session.DisposeAsync();
        _session = new Session(
            English,
            _time,
            new CallAudio(() => [new Voice("zira", "Microsoft Zira", "en-US")], (_, _) => Speech.Sound.Wav(Speech.Sound.Tone(300, 0.5, 16000), 16000), (_, _) => throw new InvalidOperationException()),
            _folder);

        _session.SetSoundFile(0, Path.Combine(_folder, "jungle.mp3"));
        var jungle = _session.Settings.Events[0];
        Assert.True(SpinWait.SpinUntil(() => _session.ClipOf(jungle) is not null, TimeSpan.FromSeconds(10)));

        Assert.Equal(CallProblem.FileMissing, _session.ClipOf(jungle)!.Problem);
        Assert.Equal("Jungle in thirty seconds", _session.HeardFor(jungle));
    }

    [Fact]
    public void The_chosen_group_or_channels_hear_the_calls()
    {
        Assert.Null(_session.Target);

        _session.Settings.Group = new Named(1625, "GvG");
        Assert.Equal(new WhisperTarget.ServerGroup(1625), _session.Target);

        _session.Settings.Listeners = ListenerKind.Channels;
        Assert.Null(_session.Target);

        _session.Settings.Channels = [new Named(3977, "Push 1"), new Named(3978, "Push 2")];
        var channels = Assert.IsType<WhisperTarget.Channels>(_session.Target);
        Assert.Equal(new ulong[] { 3977, 3978 }, channels.Ids);
    }

    [Fact]
    public void A_call_due_while_Gengfu_is_not_on_the_server_shows_as_not_sent()
    {
        _session.Settings.Group = new Named(1625, "GvG");
        _session.Clock.Start();
        _time.Advance(MatchClock.MatchLength - _session.Schedule.Calls[0].At);

        _session.Tick();

        Assert.False(_session.CanCall);
        Assert.Equal(CallState.NotSent, _session.Schedule.StateOf(_session.Schedule.Calls[0]));
    }

    [Fact]
    public void A_call_the_server_says_reached_nobody_was_not_heard_until_a_whisper_gets_through()
    {
        _session.Settings.Group = new Named(1625, "GvG");
        var gvg = _session.Target!;
        var on = _session.Connection.State;

        Assert.False(_session.Whispered(gvg, on, WhisperResult.ReachedNobody));
        _session.Tick();
        Assert.True(_session.NobodyHeard);

        // A whisper that couldn't go out says nothing of who hears.
        Assert.False(_session.Whispered(gvg, on, WhisperResult.NotSent));
        Assert.True(_session.NobodyHeard);

        Assert.True(_session.Whispered(gvg, on, WhisperResult.Sent));
        Assert.False(_session.NobodyHeard);
    }

    [Fact]
    public void Other_listeners_start_afresh_even_when_the_first_are_chosen_again()
    {
        _session.Settings.Listeners = ListenerKind.Channels;
        _session.Settings.Channels = [new Named(3977, "Push 1"), new Named(3978, "Push 2")];
        _session.Whispered(_session.Target!, _session.Connection.State, WhisperResult.ReachedNobody);

        // The same channels, chosen anew, are the ones nobody heard.
        _session.Settings.Channels = [new Named(3977, "Push 1"), new Named(3978, "Push 2")];
        _session.Tick();
        Assert.True(_session.NobodyHeard);

        _session.Settings.Channels = [new Named(3978, "Push 2")];
        Assert.False(_session.NobodyHeard);
        _session.Tick();

        _session.Settings.Channels = [new Named(3977, "Push 1"), new Named(3978, "Push 2")];
        Assert.False(_session.NobodyHeard);
    }

    [Fact]
    public async Task A_new_connection_starts_afresh()
    {
        _session.Settings.Group = new Named(1625, "GvG");
        var on = _session.Connection.State;
        _session.Whispered(_session.Target!, on, WhisperResult.ReachedNobody);

        // Nothing answers on the discard port; the connection keeps trying until the test ends.
        _session.Settings.Server = "127.0.0.1:9";
        await _session.ConnectAsync();
        for (var tries = 0; tries < 100 && ReferenceEquals(_session.Connection.State, on); tries++)
        {
            await Task.Delay(20);
        }

        Assert.False(_session.NobodyHeard);
    }

    [Fact]
    public async Task A_privilege_key_waits_for_a_connection_and_is_never_saved()
    {
        _session.TypeKey(" v2Rk+Qc8XyT1aZ9mBf3LwN0pHs7uJd5EgKoYiC4 ");

        Assert.False(_session.CanUseKey);
        await _session.UseKeyAsync();
        Assert.Null(_session.KeyAnswer);

        _session.SaveNow();
        await _session.DisposeAsync();
        _session = NewSession();
        Assert.Equal("", _session.PrivilegeKey);
        Assert.DoesNotContain("v2Rk", await File.ReadAllTextAsync(SettingsFile), StringComparison.Ordinal);
    }

    [Fact]
    public void A_key_the_server_took_is_spent_and_one_it_didn_t_take_stays_until_another_is_typed()
    {
        _session.TypeKey("first");

        _session.KeyUsed(new KeyResult(KeyOutcome.Taken, "Callers"));
        Assert.Equal("", _session.PrivilegeKey);
        Assert.Equal(new KeyResult(KeyOutcome.Taken, "Callers"), _session.KeyAnswer);

        _session.TypeKey("second");
        Assert.Null(_session.KeyAnswer);
        _session.KeyUsed(new KeyResult(KeyOutcome.Unknown));
        _session.Tick();
        Assert.Equal("second", _session.PrivilegeKey);
        Assert.Equal(new KeyResult(KeyOutcome.Unknown), _session.KeyAnswer);

        // The same text again is no new key.
        _session.TypeKey("second");
        Assert.Equal(new KeyResult(KeyOutcome.Unknown), _session.KeyAnswer);
        _session.TypeKey("second2");
        Assert.Null(_session.KeyAnswer);
    }

    [Fact]
    public void Use_sends_a_key_only_while_connected_with_one_typed_and_none_on_its_way()
    {
        Assert.True(Session.CanUseKeyFor(ConnectionStatus.Connected, " v2Rk+Qc8 ", sending: false));
        Assert.False(Session.CanUseKeyFor(ConnectionStatus.Connected, " \t", sending: false));
        Assert.False(Session.CanUseKeyFor(ConnectionStatus.Connected, "v2Rk+Qc8", sending: true));
        Assert.False(Session.CanUseKeyFor(ConnectionStatus.Connecting, "v2Rk+Qc8", sending: false));
        Assert.False(Session.CanUseKeyFor(ConnectionStatus.Disconnected, "v2Rk+Qc8", sending: false));
    }

    [Fact]
    public async Task A_key_s_answer_stands_through_the_connection_s_own_retries_until_Connect_is_pressed()
    {
        _session.KeyUsed(new KeyResult(KeyOutcome.Taken, "Callers"));
        await _session.DisconnectAsync();
        Assert.Equal(new KeyResult(KeyOutcome.Taken, "Callers"), _session.KeyAnswer);

        // Nothing answers on the discard port; the connection keeps trying until the test ends.
        _session.Settings.Server = "127.0.0.1:9";
        await _session.ConnectAsync();
        Assert.Null(_session.KeyAnswer);
        _session.KeyUsed(new KeyResult(KeyOutcome.NoAnswer));
        var on = _session.Connection.State;
        for (var tries = 0; tries < 100 && ReferenceEquals(_session.Connection.State, on); tries++)
        {
            await Task.Delay(20);
        }
        _session.Tick();
        Assert.NotSame(on, _session.Connection.State);
        Assert.Equal(new KeyResult(KeyOutcome.NoAnswer), _session.KeyAnswer);
    }

    [Fact]
    public void A_call_speaks_its_event_s_words_in_the_voice_or_plays_its_own_file_with_the_words_behind_it()
    {
        var settings = new AppSettings { Voice = "zira" };
        var boss = new MatchEvent { Name = "Boss", Spawns = [TimeSpan.FromMinutes(12)] };
        var withFile = boss with { SoundFile = @"C:\Sounds\boss.mp3", Text = "Boss, go go go" };

        Assert.Equal(new CallSound.Speech("Jungle in thirty seconds", "zira"), Session.SoundOf(settings, MatchEvent.Jungle, English));
        Assert.Equal(new CallSound.Speech("Boss in thirty seconds", "zira"), Session.SoundOf(settings, boss, English));
        Assert.Equal(
            new CallSound.AudioFile(@"C:\Sounds\boss.mp3", new CallSound.Speech("Boss, go go go", "zira")),
            Session.SoundOf(settings, withFile, English));

        settings.Voice = AppSettings.Gong;
        Assert.Equal(new CallSound.Gong(), Session.SoundOf(settings, boss, English));
        Assert.Equal(new CallSound.AudioFile(@"C:\Sounds\boss.mp3", new CallSound.Gong()), Session.SoundOf(settings, withFile, English));
    }

    [Fact]
    public void The_line_under_the_clock_names_what_listeners_hear()
    {
        using var english = new UiCulture("en-US");
        var played = new CallClip([[1]]);
        var settings = new AppSettings();
        var boss = new MatchEvent { Name = "Boss", Spawns = [TimeSpan.FromMinutes(12)], Text = "Boss, go go go" };
        var withFile = boss with { SoundFile = @"C:\Sounds\boss.mp3" };

        Assert.Equal("Jungle in thirty seconds", Session.HeardAs(settings, MatchEvent.Jungle, English, played));
        Assert.Equal("Boss, go go go", Session.HeardAs(settings, boss, English, played));
        Assert.Equal("the gong", Session.HeardAs(settings, boss, English, new CallClip([[1]], CallProblem.NoVoice)));
        Assert.Equal("boss.mp3", Session.HeardAs(settings, withFile, English, played));
        // A file that can't play gives way to the words.
        Assert.Equal("Boss, go go go", Session.HeardAs(settings, withFile, English, new CallClip([[1]], CallProblem.FileMissing)));

        settings.Voice = AppSettings.Gong;
        Assert.Equal("the gong", Session.HeardAs(settings, MatchEvent.Jungle, English, played));
        Assert.Equal("boss.mp3", Session.HeardAs(settings, withFile, English, played));
        Assert.Equal("the gong", Session.HeardAs(settings, withFile, English, new CallClip([[1]], CallProblem.FileUnreadable)));
    }

    [Fact]
    public void The_group_list_keeps_the_chosen_group_until_the_server_s_groups_are_in()
    {
        var chosen = new Named(1625, "ГВГ");
        IReadOnlyList<ServerGroupInfo> none = [];
        IReadOnlyList<ServerGroupInfo> server = [new(6, "Normal"), new(1625, "ГВГ")];

        Assert.Equal(new[] { chosen }, Session.GroupChoices(connected: false, server, chosen));
        Assert.Equal(new[] { chosen }, Session.GroupChoices(connected: true, none, chosen));
        Assert.Equal(new[] { new Named(6, "Normal"), chosen }, Session.GroupChoices(connected: true, server, chosen));
        Assert.Empty(Session.GroupChoices(connected: true, none, null));
    }

    [Fact]
    public async Task The_group_and_channels_chosen_on_one_server_are_cleared_on_connecting_to_another()
    {
        // Nothing answers on the discard port; the connection keeps trying until the test ends.
        _session.Settings.Server = "127.0.0.1:9";
        await _session.ConnectAsync();
        _session.ChooseGroup(new Named(1625, "ГВГ"));
        _session.ChooseChannels([new Named(3978, "Пуш 2")]);

        await _session.ConnectAsync();
        Assert.Equal(new Named(1625, "ГВГ"), _session.Settings.Group);
        Assert.Single(_session.Settings.Channels);

        _session.Settings.Server = "127.0.0.2:9";
        await _session.ConnectAsync();
        Assert.Null(_session.Settings.Group);
        Assert.Empty(_session.Settings.Channels);
        Assert.Null(_session.Target);
    }

    [Fact]
    public void Connect_is_offered_again_once_a_field_changes()
    {
        var login = new Login("ts.example.com", "Gengfu", "");

        Assert.True(Session.OffersConnectFor(ConnectionStatus.Disconnected, login, login));
        Assert.False(Session.OffersConnectFor(ConnectionStatus.Connected, login, login));
        Assert.False(Session.OffersConnectFor(ConnectionStatus.Connecting, login, login));
        Assert.True(Session.OffersConnectFor(ConnectionStatus.Connected, login, login with { Password = "gvgnight" }));
        Assert.True(Session.OffersConnectFor(ConnectionStatus.Connected, null, login));
    }

    [Fact]
    public void A_login_trims_the_fields_and_falls_back_to_the_bot_s_own_name()
    {
        var settings = new AppSettings { Server = "  ts.example.com ", Nickname = "   " };

        Assert.Equal(new Login("ts.example.com", "Gengfu", " secret "), Login.Of(settings, " secret "));
    }

    [Fact]
    public async Task Settings_are_written_once_typing_pauses_and_come_back_on_the_next_start()
    {
        _session.Settings.Server = "ts.example.com";
        _session.Tick();
        _time.Advance(TimeSpan.FromMilliseconds(499));
        _session.Tick();
        Assert.False(File.Exists(SettingsFile));

        _time.Advance(TimeSpan.FromMilliseconds(1));
        _session.Tick();
        Assert.Contains("\"server\": \"ts.example.com\"", File.ReadAllText(SettingsFile), StringComparison.Ordinal);

        _session.Settings.Nickname = "Gengfu 2";
        await RestartAsync();
        Assert.Equal(("ts.example.com", "Gengfu 2"), (_session.Settings.Server, _session.Settings.Nickname));
    }

    [Fact]
    public async Task The_password_and_the_identity_come_back_on_the_next_start_and_never_reach_the_settings_file()
    {
        _session.Password = "gvgnight";
        var identity = _session.Connection.Identity;
        Settle();

        Assert.DoesNotContain("gvgnight", File.ReadAllText(SettingsFile), StringComparison.Ordinal);
        Assert.DoesNotContain(identity, File.ReadAllText(SettingsFile), StringComparison.Ordinal);
        var secrets = File.ReadAllBytes(Path.Combine(_folder, "secrets.bin"));
        Assert.Equal(-1, secrets.AsSpan().IndexOf("gvgnight"u8));
        Assert.Equal(-1, secrets.AsSpan().IndexOf(Encoding.UTF8.GetBytes(identity)));

        await RestartAsync();
        Assert.Equal("gvgnight", _session.Password);
        Assert.Equal(identity, _session.Connection.Identity);
    }

    [Fact]
    public async Task A_secrets_file_Windows_won_t_let_Gengfu_read_is_never_written_over()
    {
        _session.Password = "gvgnight";
        Settle();
        var secretsFile = Path.Combine(_folder, "secrets.bin");
        var kept = File.ReadAllBytes(secretsFile);

        FileStream? held = null;
        await RestartAsync(() => held = new FileStream(secretsFile, FileMode.Open, FileAccess.Read, FileShare.None));
        held!.Dispose();
        Assert.Equal("", _session.Password);
        _session.Password = "another";
        Settle();

        await RestartAsync();
        Assert.Equal(kept, File.ReadAllBytes(secretsFile));
        Assert.Equal("gvgnight", _session.Password);
    }

    [Fact]
    public async Task A_hand_edited_file_is_written_again_only_when_a_setting_changes_in_the_app()
    {
        const string HandWritten = "// The guild's preset\n{ \"server\": \"ts.example.com\" }";
        await RestartAsync(() => File.WriteAllText(SettingsFile, HandWritten));
        Settle();
        Assert.Equal(HandWritten, File.ReadAllText(SettingsFile));

        _session.Settings.Nickname = "Gengfu 2";
        Settle();
        Assert.Contains("\"nickname\": \"Gengfu 2\"", File.ReadAllText(SettingsFile), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_broken_file_leaves_the_defaults_and_is_never_written_over()
    {
        const string Broken = "{\n  \"server\": \"ts.example.com\"\n  \"lead\": 30\n}";
        await RestartAsync(() => File.WriteAllText(SettingsFile, Broken));

        Assert.Equal(new SettingsError(3, null), _session.FileError);
        Assert.Equal("", _session.Settings.Server);

        _session.Settings.Server = "ts.example.com";
        Settle();
        _session.SaveNow();
        await _session.DisposeAsync();
        Assert.Equal(Broken, File.ReadAllText(SettingsFile));
        _session = NewSession();
    }

    [Fact]
    public void An_edit_of_the_file_replaces_the_settings_and_one_with_an_error_shows_until_it_is_fixed()
    {
        Settle();
        var replaced = 0;
        _session.SettingsReplaced += (_, _) => replaced++;
        var edited = """{ "server": "ts.example.com", "lead": 45, "events": [{ "spawns": ["25:00"] }, { "name": "Boss", "spawns": ["12:00"] }] }""";

        File.WriteAllText(SettingsFile, edited);
        _session.Reload();
        Assert.Equal(1, replaced);
        Assert.Equal("ts.example.com", _session.Settings.Server);
        Assert.Equal(new[] { "25:45", "12:45" }, _session.Schedule.Calls.Select(call => MatchClock.Format(call.At)));

        File.WriteAllText(SettingsFile, "{ \"server\": }");
        _session.Reload();
        Assert.Equal(new SettingsError(1, "server"), _session.FileError);
        Assert.Equal("ts.example.com", _session.Settings.Server);

        File.WriteAllText(SettingsFile, edited);
        _session.Reload();
        Assert.Null(_session.FileError);
        Assert.Equal(2, replaced);

        // Gengfu's own save comes back unchanged and replaces nothing.
        _session.Settings.Nickname = "Gengfu 2";
        Settle();
        _session.Reload();
        Assert.Equal(2, replaced);
    }

    [Fact]
    public async Task An_edit_made_in_another_program_is_read_once_it_settles()
    {
        Settle();

        await File.WriteAllTextAsync(SettingsFile, "{ \"nickname\": \"Gengfu 2\" }");
        for (var tries = 0; tries < 100 && _session.Settings.Nickname != "Gengfu 2"; tries++)
        {
            await Task.Delay(50);
            _time.Advance(TimeSpan.FromMilliseconds(100));
            _session.Tick();
        }

        Assert.Equal("Gengfu 2", _session.Settings.Nickname);
    }

    [Fact]
    public void A_deleted_file_is_written_again_with_the_settings_Gengfu_holds()
    {
        _session.Settings.Server = "ts.example.com";
        Settle();

        File.Delete(SettingsFile);
        _session.Reload();
        Settle();

        Assert.Contains("\"server\": \"ts.example.com\"", File.ReadAllText(SettingsFile), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_file_Windows_wouldn_t_let_Gengfu_read_is_tried_again()
    {
        FileStream? held = null;
        await RestartAsync(() =>
        {
            File.WriteAllText(SettingsFile, "{ \"nickname\": \"Gengfu 2\" }");
            held = new FileStream(SettingsFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        });
        Assert.Equal(new SettingsError(null, null), _session.FileError);

        held!.Dispose();
        _session.Tick();
        _time.Advance(TimeSpan.FromSeconds(2));
        _session.Tick();

        Assert.Null(_session.FileError);
        Assert.Equal("Gengfu 2", _session.Settings.Nickname);
    }

    [Fact]
    public void A_file_Windows_won_t_let_Gengfu_read_mid_run_is_read_again_and_saving_resumes()
    {
        Settle();
        using (new FileStream(SettingsFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            _session.Reload();
            Assert.Equal(new SettingsError(null, null), _session.FileError);
        }

        _session.Tick();
        _time.Advance(TimeSpan.FromSeconds(2));
        _session.Tick();
        Assert.Null(_session.FileError);

        _session.Settings.Nickname = "Gengfu 2";
        Settle();
        Assert.Contains("\"nickname\": \"Gengfu 2\"", File.ReadAllText(SettingsFile), StringComparison.Ordinal);
    }

    [Fact]
    public void An_error_found_in_the_file_stands_while_Windows_won_t_let_Gengfu_read_it()
    {
        Settle();
        File.WriteAllText(SettingsFile, "{ \"server\": }");
        _session.Reload();

        using (new FileStream(SettingsFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            _session.Reload();
        }
        _session.Reload();

        Assert.Equal(new SettingsError(1, "server"), _session.FileError);
    }

    [Fact]
    public async Task A_fix_saved_while_Windows_won_t_let_Gengfu_read_the_file_is_read_once_it_can()
    {
        Settle();
        File.WriteAllText(SettingsFile, "{ \"server\": }");
        _session.Reload();

        // Another program reads the file as the fix is saved, sharing it with writers but not readers;
        // letting go of it changes nothing on disk, so no edit is seen after.
        using (new FileStream(SettingsFile, FileMode.Open, FileAccess.Read, FileShare.Write))
        {
            await File.WriteAllTextAsync(SettingsFile, "{ \"server\": \"ts.example.com\" }");
            for (var tick = 0; tick < 20; tick++)
            {
                await Task.Delay(50);
                _time.Advance(TimeSpan.FromMilliseconds(300));
                _session.Tick();
            }
            Assert.Equal(new SettingsError(1, "server"), _session.FileError);
        }

        _session.Tick();
        _time.Advance(TimeSpan.FromSeconds(2));
        _session.Tick();

        Assert.Null(_session.FileError);
        Assert.Equal("ts.example.com", _session.Settings.Server);
    }

    [Fact]
    public void A_write_Windows_refuses_is_tried_again_now_and_then_not_on_every_tick()
    {
        Settle();
        var temp = SettingsFile + ".tmp";
        File.SetAttributes(SettingsFile, FileAttributes.ReadOnly);
        try
        {
            _session.Settings.Nickname = "Gengfu 2";
            Settle();
            Assert.True(File.Exists(temp));

            File.Delete(temp);
            _time.Advance(TimeSpan.FromMilliseconds(100));
            _session.Tick();
            Assert.False(File.Exists(temp));

            _time.Advance(TimeSpan.FromSeconds(2));
            _session.Tick();
            Assert.True(File.Exists(temp));
        }
        finally
        {
            File.SetAttributes(SettingsFile, FileAttributes.Normal);
        }
    }

    [Fact]
    public async Task Gengfu_connects_at_start_only_when_a_server_is_saved()
    {
        await _session.ConnectSavedAsync();
        Assert.Null(_session.ConnectedWith);

        // Nothing answers on the discard port; the connection keeps trying until the test ends.
        _session.Settings.Server = "127.0.0.1:9";
        await _session.ConnectSavedAsync();
        Assert.Equal("127.0.0.1:9", _session.ConnectedWith?.Server);
    }

    [Fact]
    public async Task An_installed_Gengfu_looks_for_updates_as_it_ticks()
    {
        using var feed = new LocalFeed();
        feed.Publish("0.1.0", "0.1.1");
        await RestartAsync(install: feed.Manager("0.1.0"));

        for (var tries = 0; tries < 200 && _session.Updates.State is not UpdateState.Ready; tries++)
        {
            _session.Tick();
            await Task.Delay(25);
        }

        Assert.Equal(new UpdateState.Ready("0.1.1"), _session.Updates.State);
    }

    // No voices and no files: every call plays the gong, so nothing here depends on the machine.
    private Session NewSession(Velopack.UpdateManager? install = null) =>
        new(
            English,
            _time,
            new CallAudio(() => [], (_, _) => throw new InvalidOperationException(), (_, _) => throw new InvalidOperationException()),
            _folder,
            install);

    // Lets whatever changed stand still long enough to be written.
    private void Settle()
    {
        _session.Tick();
        _time.Advance(TimeSpan.FromSeconds(1));
        _session.Tick();
    }

    // Closes Gengfu, does what happens while it is closed, and starts it again on the same folder.
    private async Task RestartAsync(Action? meanwhile = null, Velopack.UpdateManager? install = null)
    {
        await _session.DisposeAsync();
        meanwhile?.Invoke();
        _session = NewSession(install);
    }
}

// The theme is the whole app's, set on Gengfu's App, so these run on its thread.
[Collection(WpfAppCollection.Name)]
public sealed class SessionThemeTests(WpfApp wpf)
{
    [Fact]
    public async Task A_theme_chosen_in_Settings_or_edited_into_the_file_applies_at_once()
    {
        var folder = Path.Combine(Path.GetTempPath(), "gengfu-tests", Guid.NewGuid().ToString("N"));
        var file = Path.Combine(folder, "settings.json");
        var session = new Session(
            CultureInfo.GetCultureInfo("en-US"),
            new FakeTimeSource(),
            new CallAudio(() => [], (_, _) => throw new InvalidOperationException(), (_, _) => throw new InvalidOperationException()),
            folder);
        try
        {
            var modes = wpf.Invoke(() =>
            {
                try
                {
                    session.ChangeTheme(Theme.Dark);
                    var chosen = Application.Current.ThemeMode;
                    File.WriteAllText(file, "{ \"theme\": \"light\" }");
                    session.Reload();
                    var edited = Application.Current.ThemeMode;
                    File.WriteAllText(file, "{ \"theme\": null }");
                    session.Reload();
                    return new[] { chosen, edited, Application.Current.ThemeMode };
                }
                finally
                {
                    AppTheme.Apply(null);
                }
            });

            Assert.Equal([ThemeMode.Dark, ThemeMode.Light, ThemeMode.System], modes);
            Assert.Null(session.Settings.Theme);
        }
        finally
        {
            await session.DisposeAsync();
            Directory.Delete(folder, recursive: true);
        }
    }
}
