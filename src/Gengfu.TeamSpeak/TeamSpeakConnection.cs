using System.Diagnostics;
using System.Globalization;
using Gengfu.Core;
using TSLib;
using TSLib.Full;
using TSLib.Full.Book;
using TSLib.Helper;
using TSLib.Messages;
using TSLib.Scheduler;

namespace Gengfu.TeamSpeak;

/// <summary>Whether Gengfu is on the server. Connecting covers every attempt and the wait between two.</summary>
public enum ConnectionStatus
{
    Disconnected,
    Connecting,
    Connected,
}

/// <summary>Why the last attempt failed or the connection ended; the App puts it into words.</summary>
public enum ConnectionProblem
{
    None,

    /// <summary>The address is malformed, or nothing resolves it.</summary>
    AddressNotFound,

    /// <summary>The server didn't answer, or stopped answering; an unexpected failure counts too, its error in <see cref="ConnectionState.Detail"/>.</summary>
    NoAnswer,

    ServerShutdown,
    ServerFull,
    WrongPassword,

    /// <summary>A person kicked the bot.</summary>
    Kicked,

    /// <summary>The server itself kicked the bot, as an idle limit does.</summary>
    KickedByServer,

    Banned,

    /// <summary>The server's flood protection refuses every client from this address for a while.</summary>
    FloodProtection,

    /// <summary>The server asks for a stronger identity; <see cref="ConnectionState.Detail"/> holds the level it asks for.</summary>
    IdentityTooWeak,

    /// <summary>Any other refusal; <see cref="ConnectionState.Detail"/> holds the server's own words.</summary>
    Refused,
}

/// <summary>
/// The connection at one moment. It is replaced whole, so a reader on any thread sees one state, and
/// only when it changes: while a connection lasts, its Connected state stands, and the App takes a new
/// one for a new connection. <paramref name="RetryAt"/> is when the next attempt starts, on the time
/// source the connection was given; null while an attempt runs, and once it has stopped trying.
/// </summary>
public sealed record ConnectionState(
    ConnectionStatus Status, ConnectionProblem Problem = ConnectionProblem.None, string? Detail = null, TimeSpan? RetryAt = null);

/// <summary>A regular server group, as the server lists it.</summary>
public sealed record ServerGroupInfo(ulong Id, string Name);

/// <summary>A channel, listed in the server's tree order; <paramref name="Depth"/> counts its parents.</summary>
public sealed record ChannelInfo(ulong Id, string Name, int Depth);

/// <summary>How a whisper went, once it has played.</summary>
public enum WhisperResult
{
    /// <summary>Gengfu wasn't connected, so nothing went out.</summary>
    NotSent,

    /// <summary>It went out, and the server said nothing against it, as it does while anyone can hear it.</summary>
    Sent,

    /// <summary>
    /// It went out, and the server said it reached nobody: nobody in the group or channels is online,
    /// the group or channel is gone, or every listener needs more whisper power than the bot has.
    /// </summary>
    ReachedNobody,
}

/// <summary>How the server took a privilege key.</summary>
public enum KeyOutcome
{
    /// <summary>The server didn't answer: Gengfu wasn't connected, or the connection ended first.</summary>
    NoAnswer,

    /// <summary>The server put the bot in the key's group; <see cref="KeyResult.Detail"/> names it when the server says which.</summary>
    Taken,

    /// <summary>The server doesn't know the key: made up, mistyped, or used already, since the server deletes a used key.</summary>
    Unknown,

    /// <summary>The bot's groups may not use privilege keys; the key itself is left as it was.</summary>
    NotAllowed,

    /// <summary>Any other refusal; <see cref="KeyResult.Detail"/> holds the server's own words.</summary>
    Refused,
}

/// <summary>How the server took a privilege key, with the group it gave or its own words.</summary>
public sealed record KeyResult(KeyOutcome Outcome, string? Detail = null);

/// <summary>
/// Gengfu's seat on a TeamSpeak server. It connects as a normal client and keeps the connection up:
/// an attempt the server doesn't answer gives up after <see cref="AttemptTimeout"/>, a failure is
/// retried after 1 and 2 s and then every 5 s, so a server that comes back is reached within about
/// 10 s. After a refusal it waits 30 s, and 5 minutes while the server's flood protection holds. When
/// the server asks for a stronger identity it raises the identity's level and retries at once. It
/// stops when disconnected, kicked by a person, banned or refused the password, or asked for a level
/// above <see cref="MaxSecurityLevel"/> or one the identity already has. TSLib runs the connection on
/// its own thread, and the events fire there.
/// </summary>
public sealed class TeamSpeakConnection : IAsyncDisposable
{
    /// <summary>
    /// The highest identity level Gengfu raises to. Each level doubles the work: 32 takes about 17
    /// minutes on one core of a fast PC, 33 half an hour, and TSLib's raise can't be cancelled.
    /// </summary>
    internal const int MaxSecurityLevel = 32;

    private static readonly TimeSpan[] RetryDelays =
        [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5)];

    private static readonly TimeSpan RefusedDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan FloodDelay = TimeSpan.FromMinutes(5);

    // TS3 and TS6 say a whisper reaches nobody within a quarter of a second of its first frame, and a
    // far server adds its round trip. A shorter whisper waits out the rest before it says how it went.
    private static readonly TimeSpan NoTargetsWithin = TimeSpan.FromSeconds(1);

    private readonly ITimeSource _time;
    private readonly TsAddress _address;
    private readonly DedicatedTaskScheduler _scheduler = new(Id.Null);
    private readonly TsFullClient _client;
    private readonly VoiceSender _voice = new();
    private readonly SemaphoreSlim _switching = new(1, 1);
    private IdentityData _identity;
    private Task<IdentityData>? _raising;
    private CancellationTokenSource? _run;
    private Task _running = Task.CompletedTask;
    private TaskCompletionSource<DisconnectEventArgs>? _ended;
    private ClientId? _kicker;
    private volatile ConnectionState _state = new(ConnectionStatus.Disconnected);

    // How often the server has said a whisper reached nobody: TSLib's thread counts, a whisper compares.
    private long _noTargets;

    // The server group the server last put the bot in; read and written on TSLib's thread.
    private ServerGroupId? _joined;

    /// <param name="identity">The bot's identity as <see cref="Identity"/> gave it; null or unreadable makes a new one.</param>
    /// <param name="time">The clock its states' <see cref="ConnectionState.RetryAt"/> is on: the App's own.</param>
    public TeamSpeakConnection(string? identity, ITimeSource time)
        : this(identity, time, new TsAddress())
    {
    }

    internal TeamSpeakConnection(string? identity, ITimeSource time, TsAddress address)
    {
        _time = time;
        _address = address;
        _identity = identity is not null && IdentityData.FromTsIdentity(identity).GetOk(out var kept)
            ? kept
            : IdentityData.GenerateNewIdentity();
        Identity = _identity.ToTsIdentity();
        _client = new TsFullClient(_scheduler);
        _client.OnDisconnected += (_, e) => _ended?.TrySetResult(e);
        // TSLib keeps only why the bot left; who kicked it comes with the same notice, which reaches
        // this event before TSLib ends the connection for it.
        _client.OnClientLeftView += (_, leaves) =>
        {
            foreach (var leave in leaves.Where(leave => leave.ClientId == _client.ClientId))
            {
                _kicker = leave.InvokerId;
            }
        };
        // While a whisper reaches nobody the server says so about three times a second, and while
        // anyone hears it, never. The error names no whisper, so it counts for the one on air.
        _client.OnErrorEvent += (_, error) =>
        {
            if (error.Id == TsErrorCode.whisper_no_targets)
            {
                Interlocked.Increment(ref _noTargets);
            }
        };
        // The server names the group a privilege key gave the bot just before it answers the key.
        _client.OnEachClientServerGroupAdded += (_, added) =>
        {
            if (added.ClientId == _client.ClientId)
            {
                _joined = added.ServerGroupId;
            }
        };
    }

    /// <summary>
    /// The bot's identity, in the official client's export format. Keep it: a server group given to
    /// the bot belongs to its identity. It changes when a server asks for a stronger one.
    /// </summary>
    public string Identity { get; private set; }

    public event EventHandler? IdentityChanged;

    public ConnectionState State => _state;

    public event EventHandler<ConnectionState>? StateChanged;

    /// <summary>
    /// How long an attempt waits for the server. TSLib would resend its opening packet until its own
    /// 10 s timeout, but an attempt made during an outage never succeeds once the link is back,
    /// likely because that packet carries the time the attempt began; only a fresh one gets
    /// through. A healthy handshake takes under a second.
    /// </summary>
    internal TimeSpan AttemptTimeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>Starts connecting, in place of any earlier connection, and keeps at it.</summary>
    public Task ConnectAsync(string address, string nickname, string password) =>
        SwitchAsync(() =>
        {
            var run = new CancellationTokenSource();
            _run = run;
            _running = KeepConnectedAsync(address, nickname, password, run.Token);
        });

    /// <summary>Leaves the server and stops trying.</summary>
    public Task DisconnectAsync() =>
        SwitchAsync(() => Report(new ConnectionState(ConnectionStatus.Disconnected)));

    /// <summary>The server's regular groups in its own order. It sends them a moment after the connection is up.</summary>
    public Task<IReadOnlyList<ServerGroupInfo>> ListServerGroupsAsync() =>
        _scheduler.Invoke(() => RegularGroups(_client.Book.ServerGroups));

    /// <summary>The server's channels in its tree order.</summary>
    public Task<IReadOnlyList<ChannelInfo>> ListChannelsAsync() =>
        _scheduler.Invoke(() => InTreeOrder(
            _client.Book.Channels.Select(channel =>
                (channel.Key.Value, channel.Value.Name, channel.Value.Parent.Value, channel.Value.Order.Value))));

    /// <summary>
    /// Whispers a call, frame by frame, and once it has played says how it went: whether the server
    /// said it reached nobody. Sends nothing when not connected.
    /// </summary>
    public async Task<WhisperResult> WhisperAsync(IReadOnlyList<byte[]> frames, WhisperTarget target)
    {
        if (_state.Status != ConnectionStatus.Connected)
        {
            return WhisperResult.NotSent;
        }
        // Counted from the whisper's own first frame: one queued behind another isn't on air yet.
        var onAir = false;
        var since = 0L;
        var before = 0L;
        await _voice.PlayAsync(frames, frame =>
        {
            if (!onAir)
            {
                (onAir, since, before) = (true, Stopwatch.GetTimestamp(), Interlocked.Read(ref _noTargets));
            }
            target.Send(_client, frame);
        });
        var left = NoTargetsWithin - Stopwatch.GetElapsedTime(since);
        if (left > TimeSpan.Zero)
        {
            await Task.Delay(left);
        }
        return Interlocked.Read(ref _noTargets) == before ? WhisperResult.Sent : WhisperResult.ReachedNobody;
    }

    /// <summary>
    /// Uses a privilege key on the server Gengfu is on, and says how the server took it. The group it
    /// gives the bot comes at once and stays with the bot's identity. Sends nothing when not connected.
    /// </summary>
    public async Task<KeyResult> UsePrivilegeKeyAsync(string key)
    {
        if (_state.Status != ConnectionStatus.Connected)
        {
            return new KeyResult(KeyOutcome.NoAnswer);
        }
        return await _scheduler.InvokeAsync(async () =>
        {
            _joined = null;
            var used = await _client.PrivilegeKeyUse(key);
            var group = _joined is { } id && _client.Book.ServerGroups.TryGetValue(id, out var joined) ? joined.Name : null;
            return ReadKey(used.Ok ? null : used.Error, group);
        });
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await DisconnectAsync().WaitAsync(TimeSpan.FromSeconds(2));
        }
        catch (TimeoutException)
        {
            // A server that stopped answering never confirms the leave; TSLib would wait 10 s for it.
        }
        finally
        {
            // TSLib's scheduler thread is a foreground thread: left running, it keeps the app alive.
            _voice.Dispose();
            _client.Dispose();
            _scheduler.Dispose();
        }
    }

    /// <summary>The wait before the next attempt, after this problem and this many failures in a row.</summary>
    internal static TimeSpan RetryDelay(ConnectionProblem problem, int failures) =>
        problem switch
        {
            // Each attempt a server answers earns flood points for this address, and its flood
            // protection lifts only once nobody tries for a while: on TS6, about 4 minutes.
            ConnectionProblem.FloodProtection => FloodDelay,
            ConnectionProblem.ServerFull or ConnectionProblem.Refused => RefusedDelay,
            _ => RetryDelays[Math.Clamp(failures, 1, RetryDelays.Length) - 1],
        };

    /// <summary>Whether to try again after this problem: not after a person's kick, a ban, a wrong password or an identity a raise can't help.</summary>
    internal static bool Retries(ConnectionProblem problem) =>
        problem is not (ConnectionProblem.WrongPassword or ConnectionProblem.Kicked or ConnectionProblem.Banned
            or ConnectionProblem.IdentityTooWeak);

    /// <summary>
    /// Why a connection ended or an attempt failed: the server's error when it sent one, else the
    /// reason. <paramref name="kicker"/> is who kicked the bot; the server itself is client 0.
    /// </summary>
    internal static (ConnectionProblem Problem, string? Detail) Classify(Reason reason, CommandError? error, ClientId? kicker = null)
    {
        // TSLib reports its own failures, such as a closed connection, as custom errors.
        if (error is not null && error.Id is not (TsErrorCode.ok or TsErrorCode.custom_error))
        {
            return error.Id switch
            {
                TsErrorCode.server_invalid_password => (ConnectionProblem.WrongPassword, null),
                // Flood protection refuses as a ban does, and only its words tell it apart. A ban's
                // words start "You are banned", so no ban reason can pass for them.
                TsErrorCode.connect_failed_banned when error.ExtraMessage?.StartsWith("flood prevention", StringComparison.OrdinalIgnoreCase) == true
                    => (ConnectionProblem.FloodProtection, null),
                TsErrorCode.ban_flooding => (ConnectionProblem.FloodProtection, null),
                TsErrorCode.connect_failed_banned => (ConnectionProblem.Banned, null),
                TsErrorCode.server_maxclients_reached => (ConnectionProblem.ServerFull, null),
                _ => (ConnectionProblem.Refused, error.Message),
            };
        }
        return reason switch
        {
            Reason.KickedFromServer when kicker == ClientId.Null => (ConnectionProblem.KickedByServer, null),
            Reason.KickedFromServer => (ConnectionProblem.Kicked, null),
            Reason.Banned => (ConnectionProblem.Banned, null),
            Reason.ServerShutdown or Reason.ServerStopped => (ConnectionProblem.ServerShutdown, null),
            _ => (ConnectionProblem.NoAnswer, null),
        };
    }

    /// <summary>
    /// How the server took a privilege key, from its error, none when it took it; TS3 and TS6 answer
    /// alike. <paramref name="group"/> is the group the server put the bot in, when it said.
    /// </summary>
    internal static KeyResult ReadKey(CommandError? error, string? group) =>
        error switch
        {
            null => new KeyResult(KeyOutcome.Taken, group),
            { Id: TsErrorCode.privilege_key_invalid } => new KeyResult(KeyOutcome.Unknown),
            // The server checks b_virtualserver_token_use before it looks the key up. The permission
            // the error names doesn't count: TSLib may not have the server's permission names yet.
            { Id: TsErrorCode.permissions_client_insufficient } => new KeyResult(KeyOutcome.NotAllowed),
            // TSLib's own failures: there was no connection, or none left before the server answered.
            { Id: TsErrorCode.custom_error } => new KeyResult(KeyOutcome.NoAnswer),
            { } other => new KeyResult(KeyOutcome.Refused, other.Message),
        };

    /// <summary>The identity level a refusal asks for; the server puts it in the error's extra message.</summary>
    internal static int? RequiredSecurityLevel(CommandError? error) =>
        error?.Id == TsErrorCode.client_could_not_validate_identity
        && int.TryParse(error.ExtraMessage, NumberStyles.None, CultureInfo.InvariantCulture, out var level)
            ? level
            : null;

    /// <summary>The regular groups in the server's sort order. The book keys groups by id but leaves each group's own Id at 0.</summary>
    internal static IReadOnlyList<ServerGroupInfo> RegularGroups(IEnumerable<KeyValuePair<ServerGroupId, ServerGroup>> groups) =>
    [
        .. groups
            .Where(group => group.Value.GroupType == GroupType.Regular)
            .OrderBy(group => group.Value.SortId)
            .ThenBy(group => group.Key.Value)
            .Select(group => new ServerGroupInfo(group.Key.Value, group.Value.Name)),
    ];

    /// <summary>Orders channels the way the server shows them: each names the one above it, 0 for the first.</summary>
    internal static IReadOnlyList<ChannelInfo> InTreeOrder(IEnumerable<(ulong Id, string Name, ulong Parent, ulong Above)> channels)
    {
        var children = channels.ToLookup(channel => channel.Parent);
        var ordered = new List<ChannelInfo>();
        AddChildren(0, 0);
        return ordered;

        void AddChildren(ulong parent, int depth)
        {
            var siblings = children[parent].ToList();
            var above = 0UL;
            while (siblings.Count > 0)
            {
                // A chain the server hasn't finished sending keeps the rest in the order they came.
                var next = Math.Max(siblings.FindIndex(channel => channel.Above == above), 0);
                var channel = siblings[next];
                siblings.RemoveAt(next);
                ordered.Add(new ChannelInfo(channel.Id, channel.Name, depth));
                AddChildren(channel.Id, depth + 1);
                above = channel.Id;
            }
        }
    }

    // Stops the current run, then does the switch, on TSLib's thread like everything that touches
    // the client but the voice frames. One switch at a time: overlapping calls would otherwise stop
    // the same run twice, or leave a new one running that nothing can stop.
    private Task SwitchAsync(Action then) =>
        _scheduler.InvokeAsync(async () =>
        {
            await _switching.WaitAsync();
            try
            {
                await StopAsync();
                then();
            }
            finally
            {
                _switching.Release();
            }
        });

    private async Task StopAsync()
    {
        if (_run is not { } run)
        {
            return;
        }
        try
        {
            await run.CancelAsync();
            await _client.Disconnect();
            await _running;
        }
        finally
        {
            run.Dispose();
            _run = null;
        }
    }

    private async Task KeepConnectedAsync(string address, string nickname, string password, CancellationToken stop)
    {
        var state = new ConnectionState(ConnectionStatus.Connecting);
        var failures = 0;
        try
        {
            while (true)
            {
                // While it runs, an attempt names no next one.
                state = state with { RetryAt = null };
                Report(state);
                (ConnectionProblem Problem, string? Detail, bool WasUp) outcome;
                try
                {
                    outcome = await ConnectOnceAsync(address, nickname, password, stop);
                }
                catch (Exception ex) when (!stop.IsCancellationRequested)
                {
                    // An exception out of an attempt (from the address lookup, TSLib or a
                    // StateChanged handler) is a failed attempt too: ending the loop would stop
                    // all retries for good. Leave first, or the next attempt's Connect would end
                    // this connection and take that end for its own.
                    await _client.Disconnect();
                    outcome = (ConnectionProblem.NoAnswer, ex.Message, false);
                }
                var (problem, detail, wasUp) = outcome;
                if (!Retries(problem))
                {
                    Report(new ConnectionState(ConnectionStatus.Disconnected, problem, detail));
                    return;
                }
                failures = wasUp ? 1 : failures + 1;
                var delay = RetryDelay(problem, failures);
                state = new ConnectionState(ConnectionStatus.Connecting, problem, detail, _time.Now + delay);
                Report(state);
                await Task.Delay(delay, stop);
            }
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested)
        {
        }
    }

    // Connects and waits for the connection to end; says why it ended, and whether it was up.
    private async Task<(ConnectionProblem Problem, string? Detail, bool WasUp)> ConnectOnceAsync(
        string address, string nickname, string password, CancellationToken stop)
    {
        if (await _address.ResolveAsync(address, stop) is not { } endPoint)
        {
            return (ConnectionProblem.AddressNotFound, null, false);
        }
        while (true)
        {
            // TSLib raises OnDisconnected for a failed attempt too, before Connect returns.
            var ended = new TaskCompletionSource<DisconnectEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
            _ended = ended;
            _kicker = null;
            var connecting = _client.Connect(new ConnectionDataFull(
                endPoint.ToString(),
                _identity,
                username: nickname,
                serverPassword: password.Length == 0 ? (Password?)null : Password.FromPlain(password)));
            if (await Task.WhenAny(connecting, Task.Delay(AttemptTimeout, stop)) != connecting)
            {
                stop.ThrowIfCancellationRequested();
                await _client.Disconnect();
            }
            var result = await connecting;
            if (result.Ok)
            {
                Report(new ConnectionState(ConnectionStatus.Connected));
            }
            var end = await ended.Task;
            stop.ThrowIfCancellationRequested();
            var error = result.Ok ? end.Error : result.Error;
            if (!result.Ok && RequiredSecurityLevel(error) is { } level)
            {
                var asked = level.ToString(CultureInfo.InvariantCulture);
                // Above the ceiling a raise takes too long. At a level the identity already has it
                // changes nothing, and asking again at once, over and over, would flood the server.
                if (level > MaxSecurityLevel || _identity.GetSecurityLevel() >= level)
                {
                    return (ConnectionProblem.IdentityTooWeak, asked, false);
                }
                Report(new ConnectionState(ConnectionStatus.Connecting, ConnectionProblem.IdentityTooWeak, asked));
                await RaiseSecurityAsync(level, stop);
                continue;
            }
            var (problem, detail) = Classify(end.ExitReason, error, _kicker);
            return (problem, detail, result.Ok);
        }
    }

    /// <summary>
    /// Raises the identity's level on a copy, off TSLib's thread: each level doubles the work, and a
    /// high one takes minutes. TSLib can't cancel it, so a stop only stops the wait; the raise runs
    /// on, and the next attempt that needs one waits for it instead of starting another.
    /// </summary>
    internal async Task RaiseSecurityAsync(int level, CancellationToken stop)
    {
        if (_raising is null)
        {
            var copy = new IdentityData(_identity.PrivateKey, _identity.PublicKey, _identity.ValidKeyOffset, _identity.LastCheckedKeyOffset);
            _raising = Task.Run(() =>
            {
                copy.ImproveSecurity(level);
                return copy;
            });
        }
        var raised = await _raising.WaitAsync(stop);
        _raising = null;
        _identity = raised;
        Identity = raised.ToTsIdentity();
        IdentityChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Report(ConnectionState state)
    {
        if (state == _state)
        {
            return;
        }
        _state = state;
        StateChanged?.Invoke(this, state);
    }
}
