namespace Gengfu.Core;

/// <summary>How the calls list shows a call.</summary>
public enum CallState
{
    Later,
    Next,
    Calling,
    Called,
}

/// <summary>A call: the spawn it announces and the game time it fires at.</summary>
/// <param name="Index">Its place in <see cref="CallSchedule.Calls"/>, the same as its spawn's in the list the schedule was built from.</param>
public sealed record Call(int Index, TimeSpan Spawn, TimeSpan At);

/// <summary>
/// Knows when each call fires: at its spawn plus the lead time, in game time, so a correction to the
/// clock moves every call still ahead. A call fires once while the clock is between its time and its
/// spawn: late if a correction lands the clock there, since a late call still helps, but never after
/// the spawn, when the mob is already farmed. A correction back above its time re-arms it. A call
/// whose spawn passed without it shows with the called ones.
/// </summary>
public sealed class CallSchedule
{
    /// <summary>The GvG jungle spawns. The one at 30:00 gets no call: everyone leaves base then.</summary>
    public static IReadOnlyList<TimeSpan> GvgSpawns { get; } =
    [
        TimeSpan.FromMinutes(25),
        TimeSpan.FromMinutes(20),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromMinutes(10),
        TimeSpan.FromMinutes(5),
    ];

    public static TimeSpan DefaultLead { get; } = TimeSpan.FromSeconds(30);

    /// <summary>How long a call shows as calling after it fires.</summary>
    public static TimeSpan CallingFor { get; } = TimeSpan.FromSeconds(3);

    private readonly MatchClock _clock;
    private readonly TimeSpan?[] _firedAt;

    /// <summary>
    /// The lead must be longer than the app's tick: a call can fire only while the clock is between
    /// its time and its spawn. Validate it where it is read from settings.
    /// </summary>
    public CallSchedule(MatchClock clock, IEnumerable<TimeSpan> spawns, TimeSpan lead)
    {
        _clock = clock;
        Calls = [.. spawns.Select((spawn, index) => new Call(index, spawn, spawn + lead))];
        _firedAt = new TimeSpan?[Calls.Count];
    }

    public IReadOnlyList<Call> Calls { get; }

    /// <summary>
    /// The calls to make now, each returned once. Calls fire only here, so the app calls this on every
    /// tick, before it reads any state.
    /// </summary>
    public IReadOnlyList<Call> TakeDue()
    {
        var now = _clock.Time.Now;
        var idle = _clock.State == ClockState.Idle;
        var gameTime = _clock.GameTime;
        List<Call> due = [];
        foreach (var call in Calls)
        {
            if (idle || gameTime > call.At)
            {
                _firedAt[call.Index] = null;
            }
            else if (_firedAt[call.Index] is null && gameTime > call.Spawn)
            {
                _firedAt[call.Index] = now;
                due.Add(call);
            }
        }
        return due;
    }

    public CallState StateOf(Call call)
    {
        if (_clock.State == ClockState.Idle)
        {
            return CallState.Later;
        }
        if (_firedAt[call.Index] is { } firedAt && _clock.Time.Now - firedAt < CallingFor)
        {
            return CallState.Calling;
        }
        var gameTime = _clock.GameTime;
        if (gameTime <= call.At)
        {
            return CallState.Called;
        }
        var behindAnother = Calls.Any(other => other.At < gameTime && other.At > call.At);
        return behindAnother ? CallState.Later : CallState.Next;
    }
}
