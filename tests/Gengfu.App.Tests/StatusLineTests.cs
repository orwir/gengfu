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

    // The time on the clock the connection's next attempt is set on.
    private static readonly TimeSpan Now = TimeSpan.FromHours(5);

    private readonly UiCulture _culture = new("en-US");
    private readonly AppSettings _settings = new() { Server = Server };

    public void Dispose() => _culture.Dispose();

    [Fact]
    public void Connected_it_names_the_group_that_hears_the_calls()
    {
        _settings.Group = new Named(1625, "GvG");

        Assert.Equal(
            new StatusLine(StatusTone.Success, "Connected · whispering to the GvG group", false),
            StatusLine.Of(Connected, Server, _settings, English, Now));
    }

    [Fact]
    public void Connected_it_names_one_channel_and_counts_several()
    {
        _settings.Listeners = ListenerKind.Channels;

        _settings.Channels = [new Named(3978, "Push 2")];
        Assert.Equal(
            new StatusLine(StatusTone.Success, "Connected · whispering to Push 2", false),
            StatusLine.Of(Connected, Server, _settings, English, Now));

        _settings.Channels = [new Named(3977, "Push 1"), new Named(3978, "Push 2"), new Named(3979, "Push 3")];
        Assert.Equal("Connected · whispering to 3 channels", StatusLine.Of(Connected, Server, _settings, English, Now).Text);
    }

    [Fact]
    public void In_Russian_the_count_takes_its_case()
    {
        using var russian = new UiCulture("ru-RU");
        _settings.Listeners = ListenerKind.Channels;
        _settings.Channels = [new Named(1, "Пуш 1"), new Named(2, "Пуш 2"), new Named(3, "Пуш 3")];

        Assert.Equal("Подключено · шёпот в 3 канала", StatusLine.Of(Connected, Server, _settings, Russian, Now).Text);

        _settings.Channels = [new Named(2, "Пуш 2")];
        Assert.Equal("Подключено · шёпот в канал «Пуш 2»", StatusLine.Of(Connected, Server, _settings, Russian, Now).Text);
    }

    [Fact]
    public void Connected_after_a_call_nobody_heard_it_says_so_with_no_link_to_Settings()
    {
        _settings.Group = new Named(1625, "GvG");

        Assert.Equal(
            new StatusLine(StatusTone.Caution, "Connected · nobody heard the last call", false),
            StatusLine.Of(Connected, Server, _settings, English, Now, nobodyHeard: true));
        // Until the connection is up, its own line comes first.
        Assert.Equal(
            "Connecting to ts.example.com…",
            StatusLine.Of(new ConnectionState(ConnectionStatus.Connecting), Server, _settings, English, Now, nobodyHeard: true).Text);

        using var russian = new UiCulture("ru-RU");
        Assert.Equal(
            "Подключено · последнее объявление никто не услышал",
            StatusLine.Of(Connected, Server, _settings, Russian, Now, nobodyHeard: true).Text);
    }

    [Fact]
    public void Connected_with_nobody_chosen_it_sends_the_user_to_Settings()
    {
        var expected = new StatusLine(StatusTone.Caution, "Connected · choose who hears the calls", true);

        Assert.Equal(expected, StatusLine.Of(Connected, Server, _settings, English, Now));

        // A group chosen earlier doesn't count while the channels are what's chosen.
        _settings.Group = new Named(1625, "GvG");
        _settings.Listeners = ListenerKind.Channels;
        Assert.Equal(expected, StatusLine.Of(Connected, Server, _settings, English, Now));
    }

    [Fact]
    public void While_connecting_it_names_the_server_and_why_it_tries_again()
    {
        Assert.Equal(
            new StatusLine(StatusTone.Pending, "Connecting to ts.example.com…", false),
            StatusLine.Of(new ConnectionState(ConnectionStatus.Connecting), Server, _settings, English, Now));

        Assert.Equal(
            new[]
            {
                "No answer from ts.example.com — trying again in 0:25",
                "Can’t find ts.example.com — trying again in 0:25",
                "The server shut down — trying again in 0:25",
                "The server kicked the bot — trying again in 0:25",
                "The server is full — trying again in 0:25",
                "The server is limiting connections — trying again in 0:25",
                "The server refused: nickname is already in use — trying again in 0:25",
            },
            new[]
            {
                ConnectionProblem.NoAnswer,
                ConnectionProblem.AddressNotFound,
                ConnectionProblem.ServerShutdown,
                ConnectionProblem.KickedByServer,
                ConnectionProblem.ServerFull,
                ConnectionProblem.FloodProtection,
                ConnectionProblem.Refused,
            }.Select(problem =>
            {
                var waiting = new ConnectionState(ConnectionStatus.Connecting, problem, "nickname is already in use", In(25));
                var line = StatusLine.Of(waiting, Server, _settings, English, Now);
                Assert.Equal(StatusTone.Retrying, line.Tone);
                Assert.False(line.LinksToSettings);
                return line.Text;
            }));
    }

    [Fact]
    public void Waiting_to_try_again_it_counts_down_to_the_next_attempt_as_the_calls_list_does()
    {
        string Full(TimeSpan? retryAt) =>
            StatusLine.Of(new ConnectionState(ConnectionStatus.Connecting, ConnectionProblem.ServerFull, null, retryAt), Server, _settings, English, Now).Text;

        Assert.Equal("The server is full — trying again in 4:12", Full(In(252)));
        // Rounded up like the clock, so it reads 0:01 until the attempt starts.
        Assert.Equal("The server is full — trying again in 0:05", Full(In(4.2)));
        Assert.Equal("The server is full — trying again in 0:01", Full(In(0.1)));
        // While the attempt runs, and in the moment before it starts, it says it tries.
        Assert.Equal("The server is full — trying again…", Full(null));
        Assert.Equal("The server is full — trying again…", Full(Now));
        Assert.Equal("The server is full — trying again…", Full(In(-1)));
    }

    [Fact]
    public void Raising_the_identity_it_names_the_level_with_the_plain_connecting_dot()
    {
        var raising = new ConnectionState(ConnectionStatus.Connecting, ConnectionProblem.IdentityTooWeak, "28");
        var expected = new StatusLine(StatusTone.Pending, "Raising the bot’s security level to 28 — can take minutes", false);

        Assert.Equal(expected, StatusLine.Of(raising, Server, _settings, English, Now));
        Assert.Equal(expected, StatusLine.InSettings(raising, Server, _settings, Now));
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
                "Not connected — server wants security level 40",
                "Not connected",
            },
            new[]
            {
                new ConnectionState(ConnectionStatus.Disconnected, ConnectionProblem.WrongPassword),
                new ConnectionState(ConnectionStatus.Disconnected, ConnectionProblem.Kicked),
                new ConnectionState(ConnectionStatus.Disconnected, ConnectionProblem.Banned),
                new ConnectionState(ConnectionStatus.Disconnected, ConnectionProblem.IdentityTooWeak, "40"),
                new ConnectionState(ConnectionStatus.Disconnected),
            }.Select(state =>
            {
                var line = StatusLine.Of(state, Server, _settings, English, Now);
                Assert.Equal(StatusTone.Critical, line.Tone);
                Assert.True(line.LinksToSettings);
                return line.Text;
            }));
    }

    [Fact]
    public void In_Russian_a_strict_servers_lines_have_their_own_words()
    {
        using var russian = new UiCulture("ru-RU");

        Assert.Equal(
            new[]
            {
                "Повышаю уровень безопасности до 28 — займёт минуты",
                "ts.example.com не отвечает — повторю через 0:05",
                "Не нахожу ts.example.com — повторю через 0:02",
                "Сервер выгнал бота — повторю через 0:01",
                "Сервер ограничил подключения — повторю через 4:12",
                "Сервер отказал: too many clones already connected — повторю через 0:25",
                "Сервер заполнен — пробую снова…",
                "Не подключено — требуется уровень 40",
            },
            new[]
            {
                new ConnectionState(ConnectionStatus.Connecting, ConnectionProblem.IdentityTooWeak, "28"),
                new ConnectionState(ConnectionStatus.Connecting, ConnectionProblem.NoAnswer, null, In(5)),
                new ConnectionState(ConnectionStatus.Connecting, ConnectionProblem.AddressNotFound, null, In(2)),
                new ConnectionState(ConnectionStatus.Connecting, ConnectionProblem.KickedByServer, null, In(1)),
                new ConnectionState(ConnectionStatus.Connecting, ConnectionProblem.FloodProtection, null, In(252)),
                new ConnectionState(ConnectionStatus.Connecting, ConnectionProblem.Refused, "too many clones already connected", In(25)),
                new ConnectionState(ConnectionStatus.Connecting, ConnectionProblem.ServerFull),
                new ConnectionState(ConnectionStatus.Disconnected, ConnectionProblem.IdentityTooWeak, "40"),
            }.Select(state => StatusLine.Of(state, Server, _settings, Russian, Now).Text));
        Assert.Equal(
            "Сервер требует уровень безопасности 40",
            StatusLine.InSettings(new ConnectionState(ConnectionStatus.Disconnected, ConnectionProblem.IdentityTooWeak, "40"), Server, _settings, Now).Text);
    }

    [Fact]
    public void Before_any_server_is_entered_TeamSpeak_is_not_set_up()
    {
        var disconnected = new ConnectionState(ConnectionStatus.Disconnected);
        var blank = new AppSettings { Server = "  " };

        Assert.Equal(new StatusLine(StatusTone.Critical, "TeamSpeak isn’t set up", true), StatusLine.Of(disconnected, null, blank, English, Now));
        Assert.Equal("Not connected", StatusLine.Of(disconnected, null, _settings, English, Now).Text);
    }

    [Fact]
    public void Beside_Connect_the_words_are_shorter_and_plain_when_nothing_went_wrong()
    {
        Assert.Equal(
            new[]
            {
                (StatusTone.Success, "Connected"),
                (StatusTone.Pending, "Connecting…"),
                (StatusTone.Retrying, "No answer from ts.example.com — trying again in 0:05"),
                (StatusTone.Critical, "Wrong server password"),
                (StatusTone.Critical, "Kicked from the server"),
                (StatusTone.Critical, "Banned from the server"),
                (StatusTone.Critical, "Server wants security level 40"),
                (StatusTone.Off, "Not connected"),
            },
            new[]
            {
                Connected,
                new ConnectionState(ConnectionStatus.Connecting),
                new ConnectionState(ConnectionStatus.Connecting, ConnectionProblem.NoAnswer, null, In(5)),
                new ConnectionState(ConnectionStatus.Disconnected, ConnectionProblem.WrongPassword),
                new ConnectionState(ConnectionStatus.Disconnected, ConnectionProblem.Kicked),
                new ConnectionState(ConnectionStatus.Disconnected, ConnectionProblem.Banned),
                new ConnectionState(ConnectionStatus.Disconnected, ConnectionProblem.IdentityTooWeak, "40"),
                new ConnectionState(ConnectionStatus.Disconnected),
            }.Select(state => StatusLine.InSettings(state, Server, _settings, Now)).Select(line => (line.Tone, line.Text)));
    }

    // When a wait that starts now ends.
    private static TimeSpan In(double seconds) => Now + TimeSpan.FromSeconds(seconds);
}
