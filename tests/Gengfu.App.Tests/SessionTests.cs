using System.Globalization;
using Gengfu.App.Speech;
using Gengfu.Core;
using Gengfu.Core.Settings;
using Gengfu.TeamSpeak;

namespace Gengfu.App.Tests;

// The connection's TSLib thread keeps the test host alive until the connection is disposed.
public sealed class SessionTests : IAsyncLifetime
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

    private readonly FakeTimeSource _time = new();
    private readonly Session _session;

    // No voices and no files: every call plays the gong, so nothing here depends on the machine.
    public SessionTests() =>
        _session = new Session(
            English,
            _time,
            new CallAudio(() => [], (_, _) => throw new InvalidOperationException(), (_, _) => throw new InvalidOperationException()),
            new TeamSpeakConnection(null));

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => _session.DisposeAsync().AsTask();

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
        Assert.Equal(new CallSound.Speech("Jungle in thirty seconds", "zira"), Session.SoundOf(settings, English));

        settings.Voice = AppSettings.Gong;
        Assert.Equal(new CallSound.Gong(), Session.SoundOf(settings, English));

        settings.SoundFile = @"C:\Sounds\jungle.mp3";
        Assert.Equal(new CallSound.AudioFile(@"C:\Sounds\jungle.mp3"), Session.SoundOf(settings, English));
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
}
