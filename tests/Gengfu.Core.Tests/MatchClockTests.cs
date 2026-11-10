namespace Gengfu.Core.Tests;

public class MatchClockTests
{
    private static readonly TimeSpan OneTick = TimeSpan.FromTicks(1);

    private readonly FakeTimeSource _time = new();
    private readonly MatchClock _clock;

    public MatchClockTests() => _clock = new MatchClock(_time);

    [Fact]
    public void Before_Start_it_is_idle_at_35_00()
    {
        _time.Advance(TimeSpan.FromMinutes(3));

        Assert.Equal(ClockState.Idle, _clock.State);
        Assert.Equal("35:00", _clock.Display);
    }

    [Fact]
    public void Start_shows_35_00_for_the_first_second()
    {
        _clock.Start();
        Assert.Equal("35:00", _clock.Display);
        Assert.Equal(ClockState.Running, _clock.State);

        _time.Advance(TimeSpan.FromSeconds(1) - OneTick);
        Assert.Equal("35:00", _clock.Display);

        _time.Advance(OneTick);
        Assert.Equal("34:59", _clock.Display);
    }

    [Theory]
    [InlineData(2100.0, "35:00")]
    [InlineData(1530.0, "25:30")]
    [InlineData(1529.5, "25:30")]
    [InlineData(1529.0, "25:29")]
    [InlineData(300.0, "05:00")]
    [InlineData(0.001, "00:01")]
    [InlineData(0.0, "00:00")]
    public void Format_rounds_up_to_whole_seconds(double seconds, string shown)
    {
        Assert.Equal(shown, MatchClock.Format(TimeSpan.FromSeconds(seconds)));
    }

    [Fact]
    public void It_shows_00_00_only_at_the_end_and_stays_there()
    {
        _clock.Start();

        _time.Advance(MatchClock.MatchLength - OneTick);
        Assert.Equal("00:01", _clock.Display);
        Assert.Equal(ClockState.Running, _clock.State);

        _time.Advance(OneTick);
        Assert.Equal("00:00", _clock.Display);
        Assert.Equal(ClockState.Over, _clock.State);

        _time.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal(TimeSpan.Zero, _clock.GameTime);
    }

    [Fact]
    public void Move_shifts_the_game_time_and_the_clock_counts_on_from_there()
    {
        _clock.Start();
        _time.Advance(TimeSpan.FromMinutes(10));

        _clock.Move(TimeSpan.FromSeconds(3));
        Assert.Equal(TimeSpan.FromMinutes(25, 3), _clock.GameTime);

        _clock.Move(TimeSpan.FromMinutes(-1));
        Assert.Equal(TimeSpan.FromMinutes(24, 3), _clock.GameTime);

        _time.Advance(TimeSpan.FromSeconds(4));
        Assert.Equal("23:59", _clock.Display);
    }

    [Fact]
    public void Anchor_counts_from_the_instant_the_game_showed_the_time()
    {
        _clock.Start();
        _time.Advance(TimeSpan.FromMinutes(13));
        var firstKeystroke = _time.Now;
        _time.Advance(TimeSpan.FromSeconds(2));

        _clock.Anchor(firstKeystroke, TimeSpan.FromMinutes(22));

        Assert.Equal(TimeSpan.FromMinutes(21, 58), _clock.GameTime);
    }

    [Fact]
    public void A_correction_stops_at_35_00_and_at_00_00()
    {
        _clock.Start();
        _time.Advance(TimeSpan.FromSeconds(30));

        _clock.Move(TimeSpan.FromMinutes(1));
        Assert.Equal(MatchClock.MatchLength, _clock.GameTime);
        _time.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal("34:59", _clock.Display);

        _clock.Move(TimeSpan.FromMinutes(-40));
        Assert.Equal(ClockState.Over, _clock.State);
        Assert.Equal("00:00", _clock.Display);
    }

    [Fact]
    public void Corrections_wait_for_Start_and_end_with_the_match()
    {
        _clock.Move(TimeSpan.FromMinutes(-5));
        _clock.Anchor(_time.Now, TimeSpan.FromMinutes(20));
        Assert.Equal(ClockState.Idle, _clock.State);
        Assert.Equal(MatchClock.MatchLength, _clock.GameTime);

        _clock.Start();
        _time.Advance(MatchClock.MatchLength);
        _clock.Move(TimeSpan.FromSeconds(10));
        _clock.Anchor(_time.Now, TimeSpan.FromMinutes(1));
        Assert.Equal(ClockState.Over, _clock.State);
        Assert.Equal(TimeSpan.Zero, _clock.GameTime);
    }

    [Fact]
    public void Stop_returns_to_35_00_until_the_next_Start()
    {
        _clock.Start();
        _time.Advance(TimeSpan.FromMinutes(7));

        _clock.Stop();
        _time.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal(ClockState.Idle, _clock.State);
        Assert.Equal("35:00", _clock.Display);

        _clock.Start();
        _time.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal("34:59", _clock.Display);
    }

    [Fact]
    public void Start_Stop_and_each_correction_say_what_they_did_to_the_game_time()
    {
        List<ClockChange> changes = [];
        _clock.Changed += (_, change) => changes.Add(change);
        _clock.Stop();
        _clock.Move(TimeSpan.FromSeconds(5));

        _clock.Start();
        _time.Advance(TimeSpan.FromMinutes(10));
        _clock.Move(TimeSpan.FromSeconds(3));
        var firstKeystroke = _time.Now;
        _time.Advance(TimeSpan.FromSeconds(2));
        _clock.Anchor(firstKeystroke, TimeSpan.FromMinutes(22));
        _clock.Stop();
        _clock.Stop();

        Assert.Equal(
            [
                new ClockChange(ClockChangeKind.Started, MatchClock.MatchLength, MatchClock.MatchLength),
                new ClockChange(ClockChangeKind.Corrected, TimeSpan.FromMinutes(25), TimeSpan.FromMinutes(25, 3)),
                new ClockChange(ClockChangeKind.Corrected, TimeSpan.FromMinutes(25, 1), TimeSpan.FromMinutes(21, 58)),
                new ClockChange(ClockChangeKind.Stopped, TimeSpan.FromMinutes(21, 58), MatchClock.MatchLength),
            ],
            changes);
    }

    [Fact]
    public void Corrections_after_the_end_change_nothing_and_say_nothing()
    {
        _clock.Start();
        _time.Advance(MatchClock.MatchLength);
        List<ClockChange> changes = [];
        _clock.Changed += (_, change) => changes.Add(change);

        _clock.Move(TimeSpan.FromSeconds(10));
        _clock.Anchor(_time.Now, TimeSpan.FromMinutes(1));

        Assert.Empty(changes);
    }

    [Fact]
    public void Start_after_the_end_begins_a_new_match()
    {
        _clock.Start();
        _time.Advance(TimeSpan.FromMinutes(40));

        _clock.Start();

        Assert.Equal(ClockState.Running, _clock.State);
        Assert.Equal("35:00", _clock.Display);
    }
}
