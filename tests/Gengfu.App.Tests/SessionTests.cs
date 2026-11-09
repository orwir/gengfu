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
    public void The_call_says_the_default_for_its_lead_until_the_user_types_their_own()
    {
        Assert.Equal("Jungle in thirty seconds", _session.CallText);

        _session.ChangeLead(TimeSpan.FromSeconds(45));
        Assert.Equal("Jungle in forty-five seconds", _session.CallText);
        Assert.Equal(TimeSpan.FromSeconds(45), _session.Schedule.Lead);

        // Typing the default back keeps it the default.
        _session.SetCallText("Jungle in forty-five seconds");
        Assert.Null(_session.Settings.CallText);

        _session.SetCallText("Jungle, go go go");
        _session.ChangeLead(TimeSpan.FromSeconds(15));
        Assert.Equal("Jungle, go go go", _session.CallText);
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
    public void A_call_plays_the_sound_file_or_the_gong_when_chosen_and_speaks_the_text_otherwise()
    {
        var settings = new AppSettings { Voice = "zira" };
        Assert.Equal(new CallSound.Speech("Jungle in thirty seconds", "zira"), Session.SoundOf(settings, MatchEvent.Jungle, English));

        settings.Voice = AppSettings.Gong;
        Assert.Equal(new CallSound.Gong(), Session.SoundOf(settings, MatchEvent.Jungle, English));

        settings.SoundFile = @"C:\Sounds\jungle.mp3";
        Assert.Equal(new CallSound.AudioFile(@"C:\Sounds\jungle.mp3"), Session.SoundOf(settings, MatchEvent.Jungle, English));
        Assert.Equal(new CallSound.AudioFile(@"C:\Sounds\jungle.mp3"), Session.SoundOf(settings, null, English));
    }

    [Fact]
    public void An_event_s_own_text_plays_in_the_chosen_voice_whatever_else_Settings_sets()
    {
        var boss = new MatchEvent { Name = "Boss", Spawns = [TimeSpan.FromMinutes(12)], Text = "Boss in thirty seconds" };
        var settings = new AppSettings { Voice = "zira", SoundFile = @"C:\Sounds\jungle.mp3" };

        Assert.Equal(new CallSound.Speech("Boss in thirty seconds", "zira"), Session.SoundOf(settings, boss, English));

        settings.Voice = AppSettings.Gong;
        Assert.Equal(new CallSound.Gong(), Session.SoundOf(settings, boss, English));
    }

    [Fact]
    public void The_line_under_the_clock_names_what_listeners_hear()
    {
        using var english = new UiCulture("en-US");
        var played = new CallClip([[1]]);
        var settings = new AppSettings();
        var boss = new MatchEvent { Name = "Boss", Spawns = [TimeSpan.FromMinutes(12)], Text = "Boss in thirty seconds" };

        Assert.Equal("Jungle in thirty seconds", Session.HeardAs(settings, MatchEvent.Jungle, English, played));
        Assert.Equal("Boss in thirty seconds", Session.HeardAs(settings, boss, English, played));
        Assert.Equal("the gong", Session.HeardAs(settings, boss, English, new CallClip([[1]], CallProblem.NoVoice)));
        settings.Voice = AppSettings.Gong;
        Assert.Equal("the gong", Session.HeardAs(settings, MatchEvent.Jungle, English, played));

        // A file chosen after the gong plays the file, so the line names the file.
        settings.SoundFile = @"C:\Sounds\jungle.mp3";
        Assert.Equal("jungle.mp3", Session.HeardAs(settings, MatchEvent.Jungle, English, played));
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
