using System.Globalization;
using Gengfu.Core.Settings;
using Gengfu.TeamSpeak;

namespace Gengfu.App.Tests;

public sealed class StatusLineTests : IDisposable
{
    private const string Server = "ts.example.com";
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en");
    private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru");
    private static readonly ConnectionState Connected = new(ConnectionStatus.Connected);

    private readonly UiCulture _culture = new("en-US");
    private readonly AppSettings _settings = new() { Server = Server };

    public void Dispose() => _culture.Dispose();

    [Fact]
    public void Connected_it_names_the_group_that_hears_the_calls()
    {
        _settings.Group = new Named(1625, "GvG");

        Assert.Equal(
            new StatusLine(StatusTone.Success, "Connected · whispering to the GvG group", false),
            StatusLine.Of(Connected, Server, _settings, English));
    }

    [Fact]
    public void Connected_it_names_one_channel_and_counts_several()
    {
        _settings.Listeners = ListenerKind.Channels;

        _settings.Channels = [new Named(3978, "Push 2")];
        Assert.Equal(
            new StatusLine(StatusTone.Success, "Connected · whispering to Push 2", false),
            StatusLine.Of(Connected, Server, _settings, English));

        _settings.Channels = [new Named(3977, "Push 1"), new Named(3978, "Push 2"), new Named(3979, "Push 3")];
        Assert.Equal("Connected · whispering to 3 channels", StatusLine.Of(Connected, Server, _settings, English).Text);
    }

    [Fact]
    public void In_Russian_the_count_takes_its_case()
    {
        using var russian = new UiCulture("ru-RU");
        _settings.Listeners = ListenerKind.Channels;
        _settings.Channels = [new Named(1, "Пуш 1"), new Named(2, "Пуш 2"), new Named(3, "Пуш 3")];

        Assert.Equal("Подключено · шёпот в 3 канала", StatusLine.Of(Connected, Server, _settings, Russian).Text);

        _settings.Channels = [new Named(2, "Пуш 2")];
        Assert.Equal("Подключено · шёпот в канал «Пуш 2»", StatusLine.Of(Connected, Server, _settings, Russian).Text);
    }

    [Fact]
    public void Connected_with_nobody_chosen_it_sends_the_user_to_Settings()
    {
        var expected = new StatusLine(StatusTone.Caution, "Connected · choose who hears the calls", true);

        Assert.Equal(expected, StatusLine.Of(Connected, Server, _settings, English));

        // A group chosen earlier doesn't count while the channels are what's chosen.
        _settings.Group = new Named(1625, "GvG");
        _settings.Listeners = ListenerKind.Channels;
        Assert.Equal(expected, StatusLine.Of(Connected, Server, _settings, English));
    }

    [Fact]
    public void While_connecting_it_names_the_server_and_why_it_tries_again()
    {
        Assert.Equal(
            new StatusLine(StatusTone.Pending, "Connecting to ts.example.com…", false),
            StatusLine.Of(new ConnectionState(ConnectionStatus.Connecting), Server, _settings, English));

        Assert.Equal(
            new[]
            {
                "No answer from ts.example.com — trying again",
                "Can’t find ts.example.com — trying again",
                "The server shut down — trying again",
                "The server is full — trying again",
                "The server refused: nickname is already in use — trying again",
            },
            new[]
            {
                ConnectionProblem.NoAnswer,
                ConnectionProblem.AddressNotFound,
                ConnectionProblem.ServerShutdown,
                ConnectionProblem.ServerFull,
                ConnectionProblem.Refused,
            }.Select(problem =>
            {
                var line = StatusLine.Of(new ConnectionState(ConnectionStatus.Connecting, problem, "nickname is already in use"), Server, _settings, English);
                Assert.Equal(StatusTone.Retrying, line.Tone);
                Assert.False(line.LinksToSettings);
                return line.Text;
            }));
    }

    [Fact]
    public void Stopped_for_good_it_says_why_and_sends_the_user_to_Settings()
    {
        Assert.Equal(
            new[]
            {
                "Not connected — wrong server password",
                "Not connected — kicked from the server",
                "Not connected — banned from the server",
                "Not connected",
            },
            new[] { ConnectionProblem.WrongPassword, ConnectionProblem.Kicked, ConnectionProblem.Banned, ConnectionProblem.None }
                .Select(problem =>
                {
                    var line = StatusLine.Of(new ConnectionState(ConnectionStatus.Disconnected, problem), Server, _settings, English);
                    Assert.Equal(StatusTone.Critical, line.Tone);
                    Assert.True(line.LinksToSettings);
                    return line.Text;
                }));
    }

    [Fact]
    public void Before_any_server_is_entered_TeamSpeak_is_not_set_up()
    {
        var disconnected = new ConnectionState(ConnectionStatus.Disconnected);
        var blank = new AppSettings { Server = "  " };

        Assert.Equal(new StatusLine(StatusTone.Critical, "TeamSpeak isn’t set up", true), StatusLine.Of(disconnected, null, blank, English));
        Assert.Equal("Not connected", StatusLine.Of(disconnected, null, _settings, English).Text);
    }

    [Fact]
    public void Beside_Connect_the_words_are_shorter_and_plain_when_nothing_went_wrong()
    {
        Assert.Equal(
            new[]
            {
                (StatusTone.Success, "Connected"),
                (StatusTone.Pending, "Connecting…"),
                (StatusTone.Retrying, "No answer from ts.example.com — trying again"),
                (StatusTone.Critical, "Wrong server password"),
                (StatusTone.Critical, "Kicked from the server"),
                (StatusTone.Critical, "Banned from the server"),
                (StatusTone.Off, "Not connected"),
            },
            new[]
            {
                Connected,
                new ConnectionState(ConnectionStatus.Connecting),
                new ConnectionState(ConnectionStatus.Connecting, ConnectionProblem.NoAnswer),
                new ConnectionState(ConnectionStatus.Disconnected, ConnectionProblem.WrongPassword),
                new ConnectionState(ConnectionStatus.Disconnected, ConnectionProblem.Kicked),
                new ConnectionState(ConnectionStatus.Disconnected, ConnectionProblem.Banned),
                new ConnectionState(ConnectionStatus.Disconnected),
            }.Select(state => StatusLine.InSettings(state, Server, _settings)).Select(line => (line.Tone, line.Text)));
    }
}
