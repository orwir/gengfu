using Gengfu.Core.Settings;

namespace Gengfu.Core;

/// <summary>How the calls list shows a call.</summary>
public enum CallState
{
    Later,
    Next,
    Calling,
    Called,

    /// <summary>
    /// It never went out: it came due while Gengfu couldn't whisper (see <see cref="CallSchedule.MarkNotSent"/>),
    /// or a correction took the clock past its spawn first.
    /// </summary>
    NotSent,
}

/// <summary>A call: the event and the spawn it announces, and the game time it fires at.</summary>
/// <param name="Index">Its place in <see cref="CallSchedule.Calls"/>.</param>
public sealed record Call(int Index, MatchEvent Event, TimeSpan Spawn, TimeSpan At);

/// <summary>
/// Knows when each call fires: at its spawn plus the lead time, in game time, so a correction to the
/// clock moves every call still ahead. Every spawn of every event gets a call, soonest first. A call
/// fires once while the clock is between its time and its spawn: late if a correction lands the
/// clock there, since a late call still helps, but never after the spawn, when the mob is already
/// farmed. A correction back above its time re-arms it. A call whose spawn passed without it shows
/// as not sent: nobody heard it.
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
    private Fired?[] _fired;
    private bool[] _notSent;

    /// <summary>
    /// The lead must be longer than the app's tick: a call can fire only while the clock is between
    /// its time and its spawn. Validate it where it is read from settings.
    /// </summary>
    public CallSchedule(MatchClock clock, IReadOnlyList<MatchEvent> events, TimeSpan lead)
    {
        _clock = clock;
        Lead = lead;
        Calls = CallsOf(events, lead);
        _fired = new Fired?[Calls.Count];
        _notSent = new bool[Calls.Count];
    }

    public TimeSpan Lead { get; private set; }

    public IReadOnlyList<Call> Calls { get; private set; }

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
            if (idle || gameTime > ArmedAbove(call))
            {
                _fired[call.Index] = null;
                _notSent[call.Index] = false;
            }
            else if (_fired[call.Index] is null && gameTime > call.Spawn)
            {
                _fired[call.Index] = new Fired(now, gameTime);
                due.Add(call);
            }
        }
        return due;
    }

    /// <summary>
    /// Marks a call that came due but couldn't go out; it shows as not sent until it re-arms. The
    /// events may have changed while it was going out, so it is found among the calls as they are.
    /// </summary>
    public void MarkNotSent(Call call)
    {
        var index = call.Index < Calls.Count && SameCall(Calls[call.Index], call)
            ? call.Index
            : Calls.FirstOrDefault(now => SameCall(now, call))?.Index;
        if (index is { } at && _fired[at] is not null)
        {
            _notSent[at] = true;
        }
    }

    /// <summary>Moves every call to its spawn plus the new lead. A call already made stays made.</summary>
    public void ChangeLead(TimeSpan lead)
    {
        Lead = lead;
        Calls = [.. Calls.Select(call => call with { At = call.Spawn + lead })];
    }

    /// <summary>
    /// Takes new events, as when the settings file changes mid-match. A call the new events still
    /// make, by an event of the same name at the same spawn, keeps how it stood: made stays made.
    /// </summary>
    public void ChangeEvents(IReadOnlyList<MatchEvent> events)
    {
        var (calls, fired, notSent) = (Calls, _fired, _notSent);
        Calls = CallsOf(events, Lead);
        _fired = new Fired?[Calls.Count];
        _notSent = new bool[Calls.Count];
        foreach (var call in Calls)
        {
            if (calls.FirstOrDefault(was => SameCall(was, call)) is { } was)
            {
                _fired[call.Index] = fired[was.Index];
                _notSent[call.Index] = notSent[was.Index];
            }
        }
    }

    public CallState StateOf(Call call)
    {
        if (_clock.State == ClockState.Idle)
        {
            return CallState.Later;
        }
        if (_notSent[call.Index])
        {
            return CallState.NotSent;
        }
        // A made call stays made until it re-arms, wherever a new lead puts its time.
        if (_fired[call.Index] is { } fired)
        {
            return _clock.Time.Now - fired.Instant < CallingFor ? CallState.Calling : CallState.Called;
        }
        var gameTime = _clock.GameTime;
        if (gameTime <= call.Spawn)
        {
            return CallState.NotSent;
        }
        var behindAnother = Calls.Any(other => _fired[other.Index] is null && other.At < gameTime && other.At > call.At);
        return behindAnother ? CallState.Later : CallState.Next;
    }

    // Across a change of events, a call is the one of an event of the same name at the same spawn.
    private static bool SameCall(Call one, Call other) => one.Spawn == other.Spawn && one.Event.Name == other.Event.Name;

    // Soonest first as the clock counts down; at the same spawn, the events keep their order.
    private static IReadOnlyList<Call> CallsOf(IReadOnlyList<MatchEvent> events, TimeSpan lead) =>
    [
        .. events
            .SelectMany(matchEvent => matchEvent.Spawns.Select(spawn => (Event: matchEvent, Spawn: spawn)))
            .OrderByDescending(call => call.Spawn)
            .Select((call, index) => new Call(index, call.Event, call.Spawn, call.Spawn + lead)),
    ];

    // A made call re-arms once the clock is back above its time and above the time it fired at, so
    // a lead shortened after the call went out doesn't make it again.
    private TimeSpan ArmedAbove(Call call) =>
        _fired[call.Index] is { } fired && fired.GameTime > call.At ? fired.GameTime : call.At;

    private readonly record struct Fired(TimeSpan Instant, TimeSpan GameTime);
}
