namespace Gengfu.Core.Tests;

public class CallScheduleTests
{
    private static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(100);

    private readonly FakeTimeSource _time = new();
    private readonly MatchClock _clock;
    private readonly CallSchedule _schedule;

    public CallScheduleTests()
    {
        _clock = new MatchClock(_time);
        _schedule = new CallSchedule(_clock, CallSchedule.GvgSpawns, CallSchedule.DefaultLead);
    }

    private Call First => _schedule.Calls[0];

    [Fact]
    public void The_GvG_preset_calls_each_jungle_spawn_30_s_ahead()
    {
        Assert.Equal(
            new[] { "25:00", "20:00", "15:00", "10:00", "05:00" },
            _schedule.Calls.Select(call => MatchClock.Format(call.Spawn)));
        Assert.Equal(
            new[] { "25:30", "20:30", "15:30", "10:30", "05:30" },
            _schedule.Calls.Select(call => MatchClock.Format(call.At)));
    }

    [Fact]
    public void A_whole_match_makes_each_call_once_when_the_clock_shows_its_time()
    {
        // 97 ms never lands on a call time, so each call is seen a little after the clock reaches it.
        var tick = TimeSpan.FromMilliseconds(97);
        var made = new List<(Call Call, TimeSpan GameTime)>();

        _clock.Start();
        while (_clock.State == ClockState.Running)
        {
            made.AddRange(_schedule.TakeDue().Select(call => (call, _clock.GameTime)));
            _time.Advance(tick);
        }
        made.AddRange(_schedule.TakeDue().Select(call => (call, _clock.GameTime)));

        Assert.Equal(_schedule.Calls, made.Select(m => m.Call));
        Assert.All(made, m => Assert.InRange(m.Call.At - m.GameTime, TimeSpan.Zero, tick));
    }

    [Fact]
    public void A_call_fires_once_when_the_clock_reaches_its_time()
    {
        _clock.Start();

        RunTo(First.At + TimeSpan.FromTicks(1));
        Assert.Empty(_schedule.TakeDue());

        _time.Advance(TimeSpan.FromTicks(1));
        Assert.Equal(new[] { First }, _schedule.TakeDue());

        _time.Advance(Tick);
        Assert.Empty(_schedule.TakeDue());
    }

    [Fact]
    public void A_correction_moves_every_call_still_ahead()
    {
        _clock.Start();
        RunTo(TimeSpan.FromMinutes(26));

        // The game shows 26:10, so every call is due 10 s later than the clock had it.
        _clock.Move(TimeSpan.FromSeconds(10));
        var corrected = _time.Now;
        var firedAfter = new List<TimeSpan>();
        while (_clock.State == ClockState.Running)
        {
            if (_schedule.TakeDue().Count > 0)
            {
                firedAfter.Add(_time.Now - corrected);
            }
            _time.Advance(Tick);
        }

        Assert.Equal(new[] { 40, 340, 640, 940, 1240 }.Select(s => TimeSpan.FromSeconds(s)), firedAfter);
    }

    [Fact]
    public void A_jump_back_re_arms_a_call()
    {
        _clock.Start();
        RunTo(First.At);
        Assert.Single(_schedule.TakeDue());

        // Back above the call's time while it is still calling: it is the next call again at once.
        _time.Advance(TimeSpan.FromSeconds(1));
        _clock.Move(TimeSpan.FromSeconds(10));
        Assert.Empty(_schedule.TakeDue());
        Assert.Equal(CallState.Next, _schedule.StateOf(First));

        RunTo(First.At);
        Assert.Equal(new[] { First }, _schedule.TakeDue());
    }

    [Fact]
    public void A_jump_past_a_call_fires_it_at_once_while_its_spawn_is_ahead()
    {
        _clock.Start();
        RunTo(TimeSpan.FromMinutes(25, 35));
        Assert.Empty(_schedule.TakeDue());

        _clock.Move(TimeSpan.FromSeconds(-15));
        Assert.Equal(new[] { First }, _schedule.TakeDue());

        // Calling is timed from the late call itself, not from the time it was due.
        _time.Advance(CallSchedule.CallingFor - TimeSpan.FromTicks(1));
        _schedule.TakeDue();
        Assert.Equal(CallState.Calling, _schedule.StateOf(First));
    }

    [Fact]
    public void A_jump_past_a_spawn_drops_its_call()
    {
        _clock.Start();
        RunTo(TimeSpan.FromMinutes(25, 35));
        Assert.Empty(_schedule.TakeDue());

        _clock.Move(TimeSpan.FromSeconds(-40));

        Assert.Empty(_schedule.TakeDue());
        Assert.Equal(CallState.Called, _schedule.StateOf(First));
    }

    [Fact]
    public void A_dropped_call_fires_once_a_correction_brings_its_spawn_back_ahead()
    {
        _clock.Start();
        RunTo(TimeSpan.FromMinutes(26, 20));
        Assert.Empty(_schedule.TakeDue());

        // An overshoot to 24:20 passes the spawn; the fix to 25:20 is back inside the 30 s before it.
        _clock.Move(TimeSpan.FromMinutes(-2));
        Assert.Empty(_schedule.TakeDue());
        _clock.Move(TimeSpan.FromMinutes(1));

        Assert.Equal(new[] { First }, _schedule.TakeDue());
    }

    [Fact]
    public void States_follow_the_match()
    {
        _schedule.TakeDue();
        Assert.Equal(new[] { CallState.Later, CallState.Later, CallState.Later, CallState.Later, CallState.Later }, States());

        _clock.Start();
        _schedule.TakeDue();
        Assert.Equal(new[] { CallState.Next, CallState.Later, CallState.Later, CallState.Later, CallState.Later }, States());

        RunTo(First.At);
        _schedule.TakeDue();
        Assert.Equal(new[] { CallState.Calling, CallState.Next, CallState.Later, CallState.Later, CallState.Later }, States());

        _time.Advance(CallSchedule.CallingFor - TimeSpan.FromTicks(1));
        _schedule.TakeDue();
        Assert.Equal(CallState.Calling, _schedule.StateOf(First));

        _time.Advance(TimeSpan.FromTicks(1));
        _schedule.TakeDue();
        Assert.Equal(new[] { CallState.Called, CallState.Next, CallState.Later, CallState.Later, CallState.Later }, States());

        _time.Advance(MatchClock.MatchLength);
        _schedule.TakeDue();
        Assert.Equal(ClockState.Over, _clock.State);
        Assert.All(States(), state => Assert.Equal(CallState.Called, state));
    }

    [Fact]
    public void Stop_silences_every_call_and_Start_re_arms_them()
    {
        _clock.Start();
        RunTo(First.At);
        Assert.Single(_schedule.TakeDue());

        _clock.Stop();
        Assert.Empty(_schedule.TakeDue());
        Assert.All(States(), state => Assert.Equal(CallState.Later, state));

        _time.Advance(TimeSpan.FromMinutes(30));
        Assert.Empty(_schedule.TakeDue());

        _clock.Start();
        RunTo(First.At);
        Assert.Equal(new[] { First }, _schedule.TakeDue());
    }

    [Fact]
    public void Nothing_fires_while_the_clock_is_idle()
    {
        // A call timed above 35:00 is already reached by the idle clock's 35:00.
        var schedule = new CallSchedule(_clock, [TimeSpan.FromMinutes(34, 50)], CallSchedule.DefaultLead);

        Assert.Empty(schedule.TakeDue());

        _clock.Start();
        Assert.Equal(schedule.Calls, schedule.TakeDue());
    }

    [Fact]
    public void The_next_call_is_the_soonest_whatever_the_list_order()
    {
        var schedule = new CallSchedule(_clock, CallSchedule.GvgSpawns.Reverse(), CallSchedule.DefaultLead);

        _clock.Start();
        schedule.TakeDue();

        var next = Assert.Single(schedule.Calls, call => schedule.StateOf(call) == CallState.Next);
        Assert.Equal(TimeSpan.FromMinutes(25), next.Spawn);
        Assert.Equal(schedule.Calls.Count - 1, next.Index);
    }

    private IEnumerable<CallState> States() => _schedule.Calls.Select(_schedule.StateOf);

    // Lets the running clock count down, uncorrected, until it shows the given game time.
    private void RunTo(TimeSpan gameTime) => _time.Advance(_clock.GameTime - gameTime);
}
