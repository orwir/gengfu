using System.Globalization;
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
    Kicked,
    Banned,

    /// <summary>Any other refusal; <see cref="ConnectionState.Detail"/> holds the server's own words.</summary>
    Refused,
}

/// <summary>The connection at one moment. It is replaced whole, so a reader on any thread sees one state.</summary>
public sealed record ConnectionState(ConnectionStatus Status, ConnectionProblem Problem = ConnectionProblem.None, string? Detail = null);

/// <summary>A regular server group, as the server lists it.</summary>
public sealed record ServerGroupInfo(ulong Id, string Name);

/// <summary>A channel, listed in the server's tree order; <paramref name="Depth"/> counts its parents.</summary>
public sealed record ChannelInfo(ulong Id, string Name, int Depth);

/// <summary>
/// Gengfu's seat on a TeamSpeak server. It connects as a normal client and keeps the connection up:
/// an attempt the server doesn't answer gives up after <see cref="AttemptTimeout"/>, a failure is
/// retried after 1 and 2 s and then every 5 s, so a server that comes back is reached within about
/// 10 s. It stops only when disconnected, kicked, banned or refused the password. When the server
/// asks for a stronger identity it raises the identity's level and retries at once. TSLib runs the
/// connection on its own thread, and the events fire there.
/// </summary>
public sealed class TeamSpeakConnection : IAsyncDisposable
{
    private static readonly TimeSpan[] RetryDelays =
        [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5)];

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
    private volatile ConnectionState _state = new(ConnectionStatus.Disconnected);

    /// <param name="identity">The bot's identity as <see cref="Identity"/> gave it; null or unreadable makes a new one.</param>
    public TeamSpeakConnection(string? identity)
        : this(identity, new TsAddress())
    {
    }

    internal TeamSpeakConnection(string? identity, TsAddress address)
    {
        _address = address;
        _identity = identity is not null && IdentityData.FromTsIdentity(identity).GetOk(out var kept)
            ? kept
            : IdentityData.GenerateNewIdentity();
        Identity = _identity.ToTsIdentity();
        _client = new TsFullClient(_scheduler);
        _client.OnDisconnected += (_, e) => _ended?.TrySetResult(e);
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

    /// <summary>Whispers a call, frame by frame. Returns false, having sent nothing, when not connected.</summary>
    public async Task<bool> WhisperAsync(IReadOnlyList<byte[]> frames, WhisperTarget target)
    {
        if (_state.Status != ConnectionStatus.Connected)
        {
            return false;
        }
        await _voice.PlayAsync(frames, frame => target.Send(_client, frame));
        return true;
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

    /// <summary>The wait before the next attempt, after this many failures in a row.</summary>
    internal static TimeSpan RetryDelay(int failures) => RetryDelays[Math.Clamp(failures, 1, RetryDelays.Length) - 1];

    /// <summary>Whether to try again after this problem: not after a kick, a ban or a wrong password.</summary>
    internal static bool Retries(ConnectionProblem problem) =>
        problem is not (ConnectionProblem.WrongPassword or ConnectionProblem.Kicked or ConnectionProblem.Banned);

    /// <summary>Why a connection ended or an attempt failed: the server's error when it sent one, else the reason.</summary>
    internal static (ConnectionProblem Problem, string? Detail) Classify(Reason reason, CommandError? error)
    {
        // TSLib reports its own failures, such as a closed connection, as custom errors.
        if (error is not null && error.Id is not (TsErrorCode.ok or TsErrorCode.custom_error))
        {
            return error.Id switch
            {
                TsErrorCode.server_invalid_password => (ConnectionProblem.WrongPassword, null),
                TsErrorCode.connect_failed_banned or TsErrorCode.ban_flooding => (ConnectionProblem.Banned, null),
                TsErrorCode.server_maxclients_reached => (ConnectionProblem.ServerFull, null),
                _ => (ConnectionProblem.Refused, error.Message),
            };
        }
        return reason switch
        {
            Reason.KickedFromServer => (ConnectionProblem.Kicked, null),
            Reason.Banned => (ConnectionProblem.Banned, null),
            Reason.ServerShutdown or Reason.ServerStopped => (ConnectionProblem.ServerShutdown, null),
            _ => (ConnectionProblem.NoAnswer, null),
        };
    }

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
                state = new ConnectionState(ConnectionStatus.Connecting, problem, detail);
                Report(state);
                await Task.Delay(RetryDelay(failures), stop);
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
                await RaiseSecurityAsync(level, stop);
                continue;
            }
            var (problem, detail) = Classify(end.ExitReason, error);
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
