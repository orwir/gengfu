using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using Heijden.Dns.Portable;
using TSLib;
using TSLib.Full;
using TSLib.Helper;
using TSLib.Messages;
using ServerGroup = TSLib.Full.Book.ServerGroup;

namespace Gengfu.TeamSpeak.Tests;

public sealed class TeamSpeakConnectionTests
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

    // It never moves, so a wait's next attempt is due at exactly now plus the wait.
    private readonly FakeTimeSource _time = new();

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 5)]
    [InlineData(50, 5)]
    public void Retries_wait_1_and_2_s_then_5_s_each(int failures, int seconds)
    {
        Assert.Equal(TimeSpan.FromSeconds(seconds), TeamSpeakConnection.RetryDelay(ConnectionProblem.NoAnswer, failures));
    }

    [Theory]
    [InlineData(ConnectionProblem.ServerFull, 30)]
    [InlineData(ConnectionProblem.Refused, 30)]
    [InlineData(ConnectionProblem.FloodProtection, 300)]
    public void A_refusal_waits_30_s_and_flood_protection_5_minutes(ConnectionProblem problem, int seconds)
    {
        Assert.Equal(TimeSpan.FromSeconds(seconds), TeamSpeakConnection.RetryDelay(problem, 1));
        Assert.Equal(TimeSpan.FromSeconds(seconds), TeamSpeakConnection.RetryDelay(problem, 9));
    }

    [Theory]
    [InlineData(ConnectionProblem.WrongPassword, false)]
    [InlineData(ConnectionProblem.Kicked, false)]
    [InlineData(ConnectionProblem.Banned, false)]
    [InlineData(ConnectionProblem.IdentityTooWeak, false)]
    [InlineData(ConnectionProblem.AddressNotFound, true)]
    [InlineData(ConnectionProblem.NoAnswer, true)]
    [InlineData(ConnectionProblem.ServerShutdown, true)]
    [InlineData(ConnectionProblem.ServerFull, true)]
    [InlineData(ConnectionProblem.KickedByServer, true)]
    [InlineData(ConnectionProblem.FloodProtection, true)]
    [InlineData(ConnectionProblem.Refused, true)]
    public void It_gives_up_only_after_a_persons_kick_a_ban_a_wrong_password_or_a_level_too_high(ConnectionProblem problem, bool retries)
    {
        Assert.Equal(retries, TeamSpeakConnection.Retries(problem));
    }

    [Theory]
    [InlineData(Reason.Timeout, ConnectionProblem.NoAnswer)]
    [InlineData(Reason.SocketError, ConnectionProblem.NoAnswer)]
    [InlineData(Reason.LeftServer, ConnectionProblem.NoAnswer)]
    [InlineData(Reason.ServerShutdown, ConnectionProblem.ServerShutdown)]
    [InlineData(Reason.ServerStopped, ConnectionProblem.ServerShutdown)]
    [InlineData(Reason.KickedFromServer, ConnectionProblem.Kicked)]
    [InlineData(Reason.Banned, ConnectionProblem.Banned)]
    public void Without_a_server_error_the_reason_tells_why_it_ended(Reason reason, ConnectionProblem problem)
    {
        Assert.Equal((problem, null), TeamSpeakConnection.Classify(reason, null));
        Assert.Equal((problem, null), TeamSpeakConnection.Classify(reason, CommandError.ConnectionClosed));
    }

    [Fact]
    public void A_kick_by_the_server_itself_is_told_from_a_persons()
    {
        Assert.Equal((ConnectionProblem.KickedByServer, null), TeamSpeakConnection.Classify(Reason.KickedFromServer, null, ClientId.Null));
        Assert.Equal((ConnectionProblem.Kicked, null), TeamSpeakConnection.Classify(Reason.KickedFromServer, null, new ClientId(61)));
        Assert.Equal((ConnectionProblem.Kicked, null), TeamSpeakConnection.Classify(Reason.KickedFromServer, null));
    }

    [Theory]
    [InlineData(TsErrorCode.server_invalid_password, ConnectionProblem.WrongPassword)]
    [InlineData(TsErrorCode.connect_failed_banned, ConnectionProblem.Banned)]
    [InlineData(TsErrorCode.ban_flooding, ConnectionProblem.FloodProtection)]
    [InlineData(TsErrorCode.server_maxclients_reached, ConnectionProblem.ServerFull)]
    public void A_server_refusal_is_told_by_its_error(TsErrorCode id, ConnectionProblem problem)
    {
        Assert.Equal((problem, null), TeamSpeakConnection.Classify(Reason.LeftServer, new CommandError { Id = id }));
    }

    [Theory]
    [InlineData("flood prevention, please try again later", ConnectionProblem.FloodProtection)]
    [InlineData("You are banned temporarily. Reason: \"flood prevention\"\nTry again in 00:02:00", ConnectionProblem.Banned)]
    [InlineData("You are banned permanently. Reason: \"flooding\"", ConnectionProblem.Banned)]
    public void Flood_protection_refuses_as_a_ban_does_and_only_its_words_tell_it_apart(string words, ConnectionProblem problem)
    {
        var error = new CommandError { Id = TsErrorCode.connect_failed_banned, ExtraMessage = words };

        Assert.Equal((problem, null), TeamSpeakConnection.Classify(Reason.LeftServer, error));
    }

    [Fact]
    public void Any_other_refusal_keeps_the_servers_own_words()
    {
        var error = new CommandError { Id = TsErrorCode.client_too_many_clones_connected, Message = "too many clones already connected" };

        Assert.Equal(
            (ConnectionProblem.Refused, "too many clones already connected"),
            TeamSpeakConnection.Classify(Reason.LeftServer, error));
    }

    [Fact]
    public void A_request_for_a_stronger_identity_names_the_level_wanted()
    {
        Assert.Equal(23, TeamSpeakConnection.RequiredSecurityLevel(
            new CommandError { Id = TsErrorCode.client_could_not_validate_identity, ExtraMessage = "23" }));
        Assert.Null(TeamSpeakConnection.RequiredSecurityLevel(
            new CommandError { Id = TsErrorCode.server_invalid_password, ExtraMessage = "23" }));
        Assert.Null(TeamSpeakConnection.RequiredSecurityLevel(null));
    }

    [Fact]
    public void Channels_come_in_the_servers_tree_order()
    {
        // Lobby holds Squad A (which holds Push) and then Squad B; AFK follows Lobby.
        (ulong Id, string Name, ulong Parent, ulong Above)[] channels =
        [
            (4, "Squad B", 1, 3),
            (2, "AFK", 0, 1),
            (5, "Push", 3, 0),
            (1, "Lobby", 0, 0),
            (3, "Squad A", 1, 0),
        ];

        Assert.Equal(
            [new ChannelInfo(1, "Lobby", 0), new(3, "Squad A", 1), new(5, "Push", 2), new(4, "Squad B", 1), new(2, "AFK", 0)],
            TeamSpeakConnection.InTreeOrder(channels));
    }

    [Fact]
    public void Only_regular_groups_are_listed_in_the_servers_order_with_ids_from_the_books_keys()
    {
        // TSLib's book leaves each group's own Id at 0, so none is set here either.
        var groups = new Dictionary<ServerGroupId, ServerGroup>
        {
            [new ServerGroupId(2)] = new() { Name = "Admin Server Query", GroupType = GroupType.Query, SortId = 0 },
            [new ServerGroupId(3)] = new() { Name = "Template Admin", GroupType = GroupType.Template, SortId = 0 },
            [new ServerGroupId(9)] = new() { Name = "Normal", GroupType = GroupType.Regular, SortId = 20 },
            [new ServerGroupId(12)] = new() { Name = "GvG", GroupType = GroupType.Regular, SortId = 10 },
            [new ServerGroupId(7)] = new() { Name = "Guest", GroupType = GroupType.Regular, SortId = 20 },
        };

        Assert.Equal(
            [new ServerGroupInfo(12, "GvG"), new(7, "Guest"), new(9, "Normal")],
            TeamSpeakConnection.RegularGroups(groups));
    }

    [Fact]
    public async Task A_whisper_while_not_connected_sends_nothing()
    {
        await using var connection = new TeamSpeakConnection(null, _time);

        Assert.Equal(WhisperResult.NotSent, await connection.WhisperAsync([[1, 2, 3]], new WhisperTarget.ServerGroup(12)));
    }

    [Fact]
    public async Task Each_whisper_says_whether_the_server_said_it_reached_nobody()
    {
        // TS3 and TS6 answer a whisper nobody can hear with error 1804, and say nothing while anyone hears it.
        using var server = new LocalTsServer("") { RefusesWhispers = true };
        await using var connection = new TeamSpeakConnection(null, _time);
        var states = Watch(connection);
        await connection.ConnectAsync(server.Address, "Gengfu", "");
        Assert.Equal(new ConnectionState(ConnectionStatus.Connecting), await NextAsync(states));
        Assert.Equal(new ConnectionState(ConnectionStatus.Connected), await NextAsync(states));
        var up = connection.State;
        // A tenth of a second: shorter than the server may take to answer, so the whisper waits for it.
        IReadOnlyList<byte[]> frames = [.. Enumerable.Repeat(new byte[] { 1, 2, 3 }, 5)];
        var squad = new WhisperTarget.ServerGroup(12);

        Assert.Equal(WhisperResult.ReachedNobody, await connection.WhisperAsync(frames, squad).WaitAsync(Patience));

        server.RefusesWhispers = false;
        Assert.Equal(WhisperResult.Sent, await connection.WhisperAsync(frames, squad).WaitAsync(Patience));
        // The App tells one connection from the next by its state, so whispers leave it as it stands.
        Assert.Same(up, connection.State);
    }

    [Fact]
    public async Task A_raised_identity_keeps_its_key_and_reaches_the_level_asked_for()
    {
        var weak = IdentityData.GenerateNewIdentity(0);
        await using var connection = new TeamSpeakConnection(weak.ToTsIdentity(), _time);
        var changes = 0;
        connection.IdentityChanged += (_, _) => changes++;

        await connection.RaiseSecurityAsync(6, CancellationToken.None);

        var raised = IdentityData.FromTsIdentity(connection.Identity).Unwrap();
        Assert.Equal(1, changes);
        Assert.Equal(weak.PublicKeyString, raised.PublicKeyString);
        Assert.True(raised.GetSecurityLevel() >= 6);
    }

    [Fact]
    public async Task It_keeps_the_identity_it_is_given_and_makes_one_when_there_is_none()
    {
        await using var made = new TeamSpeakConnection(null, _time);
        await using var kept = new TeamSpeakConnection(made.Identity, _time);
        await using var remade = new TeamSpeakConnection("not an identity", _time);

        Assert.True(IdentityData.FromTsIdentity(made.Identity).Ok);
        Assert.Equal(made.Identity, kept.Identity);
        Assert.True(IdentityData.FromTsIdentity(remade.Identity).Ok);
        Assert.NotEqual(made.Identity, remade.Identity);
    }

    [Fact]
    public async Task An_address_nothing_resolves_is_reported_while_it_keeps_trying()
    {
        await using var connection = new TeamSpeakConnection(null, _time);
        var states = Watch(connection);

        await connection.ConnectAsync("teal example.test", "Gengfu", "");

        Assert.Equal(new ConnectionState(ConnectionStatus.Connecting), await NextAsync(states));
        Assert.Equal(new ConnectionState(ConnectionStatus.Connecting, ConnectionProblem.AddressNotFound, null, In(1)), await NextAsync(states));
    }

    [Fact]
    public async Task A_server_that_does_not_answer_is_reported_and_tried_again_until_disconnect()
    {
        // Nothing listens on the port, so Windows refuses the first attempt at once; the second
        // finds a socket there that never answers, and is still waiting when Disconnect comes.
        // While Gengfu waits, the state names when the next attempt comes; while it runs, none.
        var port = ClosedUdpPort();
        await using var connection = new TeamSpeakConnection(null, _time);
        var states = Watch(connection);

        await connection.ConnectAsync($"127.0.0.1:{port}", "Gengfu", "");
        Assert.Equal(new ConnectionState(ConnectionStatus.Connecting), await NextAsync(states));
        Assert.Equal(new ConnectionState(ConnectionStatus.Connecting, ConnectionProblem.NoAnswer, null, In(1)), await NextAsync(states));

        using var server = new UdpClient(new IPEndPoint(IPAddress.Loopback, port));
        var retry = await server.ReceiveAsync().WaitAsync(Patience);
        Assert.NotEmpty(retry.Buffer);
        Assert.Equal(new ConnectionState(ConnectionStatus.Connecting, ConnectionProblem.NoAnswer), await NextAsync(states));

        await connection.DisconnectAsync().WaitAsync(Patience);
        Assert.Equal(new ConnectionState(ConnectionStatus.Disconnected), connection.State);
    }

    [Fact]
    public async Task An_attempt_the_server_never_answers_is_given_up_and_made_afresh()
    {
        // TSLib alone resends the first attempt's packet until its own 10 s timeout. Each attempt
        // opens its own socket, so a fresh one arrives from a new port: here 1 s + 1 s later.
        using var server = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        var port = ((IPEndPoint)server.Client.LocalEndPoint!).Port;
        await using var connection = new TeamSpeakConnection(null, _time) { AttemptTimeout = TimeSpan.FromSeconds(1) };

        await connection.ConnectAsync($"127.0.0.1:{port}", "Gengfu", "");
        var first = (await server.ReceiveAsync().WaitAsync(Patience)).RemoteEndPoint;
        await FreshAttemptAsync().WaitAsync(TimeSpan.FromSeconds(5));

        // Whether the fresh attempt still runs or has given up in turn, the reason is the same.
        Assert.Equal((ConnectionStatus.Connecting, ConnectionProblem.NoAnswer), (connection.State.Status, connection.State.Problem));

        async Task FreshAttemptAsync()
        {
            UdpReceiveResult packet;
            do
            {
                packet = await server.ReceiveAsync();
            }
            while (packet.RemoteEndPoint.Equals(first));
        }
    }

    [Theory]
    [InlineData(@"error id=1028 msg=invalid\sserver\spassword", ConnectionStatus.Disconnected, ConnectionProblem.WrongPassword, 0)]
    [InlineData(@"error id=3329 msg=connection\sfailed,\syou\sare\sbanned extra_msg=You\sare\sbanned\stemporarily.\sReason:\s""probe""\nTry\sagain\sin\s00:02:00", ConnectionStatus.Disconnected, ConnectionProblem.Banned, 0)]
    [InlineData(@"error id=3329 msg=connection\sfailed,\syou\sare\sbanned extra_msg=flood\sprevention,\splease\stry\sagain\slater", ConnectionStatus.Connecting, ConnectionProblem.FloodProtection, 300)]
    [InlineData(@"error id=1027 msg=server\smaxclient\sreached", ConnectionStatus.Connecting, ConnectionProblem.ServerFull, 30)]
    public async Task The_servers_refusal_of_an_attempt_decides_what_follows(string refusal, ConnectionStatus status, ConnectionProblem problem, int retryIn)
    {
        using var server = new LocalTsServer(refusal);
        await using var connection = new TeamSpeakConnection(null, _time);
        var states = Watch(connection);

        await connection.ConnectAsync(server.Address, "Gengfu", "");

        Assert.Equal(new ConnectionState(ConnectionStatus.Connecting), await NextAsync(states));
        Assert.Equal(new ConnectionState(status, problem, null, retryIn == 0 ? null : In(retryIn)), await NextAsync(states));
    }

    [Fact]
    public async Task A_server_that_asks_for_a_stronger_identity_gets_one_on_the_next_attempt()
    {
        using var server = new LocalTsServer(
            @"error id=519 msg=could\snot\svalidate\sclient\sidentity extra_msg=16",
            @"error id=1028 msg=invalid\sserver\spassword");
        await using var connection = new TeamSpeakConnection(IdentityData.GenerateNewIdentity(0).ToTsIdentity(), _time);
        var states = Watch(connection);

        await connection.ConnectAsync(server.Address, "Gengfu", "");

        // Only a second attempt is refused for its password.
        Assert.Equal(new ConnectionState(ConnectionStatus.Connecting), await NextAsync(states));
        Assert.Equal(new ConnectionState(ConnectionStatus.Connecting, ConnectionProblem.IdentityTooWeak, "16"), await NextAsync(states));
        Assert.Equal(new ConnectionState(ConnectionStatus.Disconnected, ConnectionProblem.WrongPassword), await NextAsync(states));
        Assert.True(IdentityData.FromTsIdentity(connection.Identity).Unwrap().GetSecurityLevel() >= 16);
    }

    [Fact]
    public async Task A_server_that_asks_for_more_than_level_32_is_left_with_the_identity_as_it_was()
    {
        using var server = new LocalTsServer(@"error id=519 msg=could\snot\svalidate\sclient\sidentity extra_msg=33");
        var identity = IdentityData.GenerateNewIdentity(0).ToTsIdentity();
        await using var connection = new TeamSpeakConnection(identity, _time);
        var states = Watch(connection);

        await connection.ConnectAsync(server.Address, "Gengfu", "");

        Assert.Equal(new ConnectionState(ConnectionStatus.Connecting), await NextAsync(states));
        Assert.Equal(new ConnectionState(ConnectionStatus.Disconnected, ConnectionProblem.IdentityTooWeak, "33"), await NextAsync(states));
        Assert.Equal(identity, connection.Identity);
    }

    [Fact]
    public async Task A_server_that_refuses_the_level_the_identity_has_is_not_asked_again()
    {
        using var server = new LocalTsServer(@"error id=519 msg=could\snot\svalidate\sclient\sidentity extra_msg=8");
        await using var connection = new TeamSpeakConnection(IdentityData.GenerateNewIdentity(8).ToTsIdentity(), _time);
        var states = Watch(connection);

        await connection.ConnectAsync(server.Address, "Gengfu", "");

        Assert.Equal(new ConnectionState(ConnectionStatus.Connecting), await NextAsync(states));
        Assert.Equal(new ConnectionState(ConnectionStatus.Disconnected, ConnectionProblem.IdentityTooWeak, "8"), await NextAsync(states));
        Assert.Equal(1, server.Attempts);
    }

    [Fact]
    public async Task A_server_that_refuses_is_not_tried_again_at_once()
    {
        using var server = new LocalTsServer(@"error id=1027 msg=server\smaxclient\sreached");
        await using var connection = new TeamSpeakConnection(null, _time);
        var states = Watch(connection);

        await connection.ConnectAsync(server.Address, "Gengfu", "");
        Assert.Equal(new ConnectionState(ConnectionStatus.Connecting), await NextAsync(states));
        Assert.Equal(new ConnectionState(ConnectionStatus.Connecting, ConnectionProblem.ServerFull, null, In(30)), await NextAsync(states));

        // The quickest retry comes 1 s after a failure; a refusal's comes 30 s after.
        await Task.Delay(TimeSpan.FromSeconds(2));
        Assert.Equal(1, server.Attempts);
    }

    [Fact]
    public async Task After_a_kick_by_the_server_itself_it_comes_back()
    {
        // TS3 and TS6 both kick an idle client in the name of client 0, "server".
        using var server = new LocalTsServer(
            $@"notifyclientleftview cfid=1 ctid=0 reasonid=5 invokerid=0 invokername=server reasonmsg=idle\stime\sexceeded clid={LocalTsServer.BotId}",
            "");
        await using var connection = new TeamSpeakConnection(null, _time);
        var states = Watch(connection);

        await connection.ConnectAsync(server.Address, "Gengfu", "");

        Assert.Equal(new ConnectionState(ConnectionStatus.Connecting), await NextAsync(states));
        Assert.Equal(new ConnectionState(ConnectionStatus.Connected), await NextAsync(states));
        Assert.Equal(new ConnectionState(ConnectionStatus.Connecting, ConnectionProblem.KickedByServer, null, In(1)), await NextAsync(states));
        Assert.Equal(new ConnectionState(ConnectionStatus.Connecting, ConnectionProblem.KickedByServer), await NextAsync(states));
        Assert.Equal(new ConnectionState(ConnectionStatus.Connected), await NextAsync(states));
    }

    [Fact]
    public async Task After_a_kick_by_a_person_it_stays_off()
    {
        using var server = new LocalTsServer(
            $@"notifyclientleftview cfid=1 ctid=0 reasonid=5 invokerid=61 invokername=Admin invokeruid=admin reasonmsg=enough clid={LocalTsServer.BotId}");
        await using var connection = new TeamSpeakConnection(null, _time);
        var states = Watch(connection);

        await connection.ConnectAsync(server.Address, "Gengfu", "");

        Assert.Equal(new ConnectionState(ConnectionStatus.Connecting), await NextAsync(states));
        Assert.Equal(new ConnectionState(ConnectionStatus.Connected), await NextAsync(states));
        Assert.Equal(new ConnectionState(ConnectionStatus.Disconnected, ConnectionProblem.Kicked), await NextAsync(states));
    }

    [Fact]
    public async Task Overlapping_connects_and_disconnects_end_disconnected_with_no_attempt_left()
    {
        using var server = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        var address = $"127.0.0.1:{((IPEndPoint)server.Client.LocalEndPoint!).Port}";
        await using var connection = new TeamSpeakConnection(null, _time) { AttemptTimeout = TimeSpan.FromSeconds(1) };
        await connection.ConnectAsync(address, "Gengfu", "");
        await server.ReceiveAsync().WaitAsync(Patience);

        await Task.WhenAll(
            connection.DisconnectAsync(),
            connection.ConnectAsync(address, "Gengfu", ""),
            connection.DisconnectAsync()).WaitAsync(Patience);

        // Packets sent before the last disconnect may still wait in the buffer; after it, none come.
        while (server.Available > 0)
        {
            IPEndPoint? from = null;
            server.Receive(ref from);
        }
        await Task.Delay(TimeSpan.FromSeconds(3));
        Assert.Equal(0, server.Available);
        Assert.Equal(new ConnectionState(ConnectionStatus.Disconnected), connection.State);
    }

    [Fact]
    public async Task An_attempt_that_fails_unexpectedly_is_reported_and_tried_again()
    {
        using var dns = new LocalDns();
        var lookups = Channel.CreateUnbounded<string>();
        var address = new TsAddress(
            () => new Resolver(dns.EndPoint),
            (host, _) =>
            {
                lookups.Writer.TryWrite(host);
                return Task.FromException<IPAddress[]>(new InvalidOperationException("lookup broke"));
            });
        await using var connection = new TeamSpeakConnection(null, _time, address);
        var states = Watch(connection);

        await connection.ConnectAsync("teal.example.test", "Gengfu", "");

        Assert.Equal(new ConnectionState(ConnectionStatus.Connecting), await NextAsync(states));
        Assert.Equal(
            new ConnectionState(ConnectionStatus.Connecting, ConnectionProblem.NoAnswer, "lookup broke", In(1)),
            await NextAsync(states));
        await lookups.Reader.ReadAsync().AsTask().WaitAsync(Patience);
        await lookups.Reader.ReadAsync().AsTask().WaitAsync(Patience);
    }

    [Fact]
    public async Task Connect_and_disconnect_return_at_once_while_a_DNS_server_never_answers()
    {
        // Each name the DNS server leaves unanswered holds a lookup for 3 s.
        using var dns = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        var address = new TsAddress(
            () => new Resolver((IPEndPoint)dns.Client.LocalEndPoint!),
            (_, _) => Task.FromException<IPAddress[]>(new SocketException((int)SocketError.HostNotFound)));
        await using var connection = new TeamSpeakConnection(null, _time, address);
        var watch = Stopwatch.StartNew();

        await connection.ConnectAsync("teal.example.test", "Gengfu", "").WaitAsync(Patience);
        await dns.ReceiveAsync().WaitAsync(Patience);
        await connection.DisconnectAsync().WaitAsync(Patience);

        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(1), $"Connect and disconnect took {watch.Elapsed}.");
        Assert.Equal(new ConnectionState(ConnectionStatus.Disconnected), connection.State);
    }

    // When a wait that starts now ends, on the connection's clock.
    private TimeSpan In(int seconds) => _time.Now + TimeSpan.FromSeconds(seconds);

    private static ChannelReader<ConnectionState> Watch(TeamSpeakConnection connection)
    {
        var states = Channel.CreateUnbounded<ConnectionState>();
        connection.StateChanged += (_, state) => states.Writer.TryWrite(state);
        return states.Reader;
    }

    private static async Task<ConnectionState> NextAsync(ChannelReader<ConnectionState> states) =>
        await states.ReadAsync().AsTask().WaitAsync(Patience);

    private static int ClosedUdpPort()
    {
        using var socket = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)socket.Client.LocalEndPoint!).Port;
    }
}
