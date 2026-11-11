using System.Globalization;

namespace Gengfu.Core.Tests;

public class SyncInputTests
{
    private readonly FakeTimeSource _time = new();
    private readonly MatchClock _clock;

    public SyncInputTests() => _clock = new MatchClock(_time);

    [Theory]
    [InlineData("22:02", "20", "20:02")]
    [InlineData("22:02", "0", "00:02")]
    [InlineData("22:02", "22", "22:02")]
    [InlineData("34:30", "35", "35:00")]
    public void Typed_minutes_keep_the_running_seconds(string shown, string typed, string after)
    {
        RunTo(shown);
        var input = new SyncInput(_clock, ClockField.Minutes);

        input.Keystroke();
        _time.Advance(TimeSpan.FromSeconds(0.3));
        input.Enter(typed);

        Assert.Equal(after, _clock.Display);
    }

    [Theory]
    [InlineData("22:02", "58", "21:58")]
    [InlineData("21:57", "03", "22:03")]
    [InlineData("22:30", "10", "22:10")]
    [InlineData("22:30", "50", "22:50")]
    [InlineData("22:02", "02", "22:02")]
    public void Typed_seconds_land_on_the_nearest_minute(string shown, string typed, string gameShowed)
    {
        RunTo(shown);
        var input = new SyncInput(_clock, ClockField.Seconds);
        var typedAt = _time.Now;

        input.Keystroke();
        _time.Advance(TimeSpan.FromSeconds(0.3));
        input.Enter(typed);

        Assert.Equal(Time(gameShowed) - SyncInput.TypingLag, _clock.GameTimeAt(typedAt));
    }

    [Fact]
    public void Typed_seconds_are_timed_from_the_first_keystroke()
    {
        RunTo("22:02");
        var input = new SyncInput(_clock, ClockField.Seconds);
        var firstKeystroke = _time.Now;

        input.Keystroke();
        _time.Advance(TimeSpan.FromSeconds(0.4));
        input.Keystroke();
        _time.Advance(TimeSpan.FromSeconds(0.5));
        input.Enter("58");

        Assert.Equal(Time("21:58") - SyncInput.TypingLag, _clock.GameTimeAt(firstKeystroke));
        Assert.Equal(Time("21:58") - SyncInput.TypingLag - TimeSpan.FromSeconds(0.9), _clock.GameTime);
    }

    [Theory]
    [InlineData(1.2, false)]
    [InlineData(1.3, true)]
    public void After_a_pause_Enter_times_the_typed_seconds(double pause, bool fromEnter)
    {
        RunTo("21:44");
        var input = new SyncInput(_clock, ClockField.Seconds);
        var firstKeystroke = _time.Now;

        input.Keystroke();
        _time.Advance(TimeSpan.FromSeconds(pause));
        input.Enter("40");

        if (fromEnter)
        {
            Assert.Equal(Time("21:40") - SyncInput.EnterLag, _clock.GameTime);
        }
        else
        {
            Assert.Equal(Time("21:40") - SyncInput.TypingLag, _clock.GameTimeAt(firstKeystroke));
        }
    }

    [Theory]
    [InlineData(0.3)]
    [InlineData(2.0)]
    [InlineData(5.0)]
    public void Leaving_the_field_times_the_value_from_the_first_keystroke_however_long_after(double pause)
    {
        RunTo("21:44");
        var input = new SyncInput(_clock, ClockField.Seconds);
        var firstKeystroke = _time.Now;

        input.Keystroke();
        _time.Advance(TimeSpan.FromSeconds(pause));
        input.Leave("40");

        Assert.Equal(Time("21:40") - SyncInput.TypingLag, _clock.GameTimeAt(firstKeystroke));
    }

    [Theory]
    [InlineData("10:00", 0.6, false, "08:59")]
    [InlineData("10:01", 5.0, false, "08:56")]
    [InlineData("10:00", 0.6, true, "08:59")]
    [InlineData("10:01", 2.0, true, "08:59")]
    public void Typed_minutes_count_from_the_clock_at_the_first_keystroke(string shown, double pause, bool byEnter, string after)
    {
        RunTo(shown);
        var input = new SyncInput(_clock, ClockField.Minutes);

        input.Keystroke();
        _time.Advance(TimeSpan.FromSeconds(pause));
        Assert.StartsWith("09:", _clock.Display);
        if (byEnter)
        {
            input.Enter("9");
        }
        else
        {
            input.Leave("9");
        }

        Assert.Equal(after, _clock.Display);
    }

    // The clock's seconds are 43 s from the game's 26:05, and 45 s from its 26:55: the nearest minute
    // to the clock would be a minute off either way.
    [Theory]
    [InlineData("34:48", "05", "26:05")]
    [InlineData("34:10", "55", "26:55")]
    public void Seconds_typed_right_after_minutes_land_in_the_minute_typed(string shown, string typed, string gameShowed)
    {
        RunTo(shown);
        var minutes = TypeMinutes("26");
        var input = new SyncInput(_clock, ClockField.Seconds, minutes);
        var typedAt = _time.Now;

        input.Keystroke();
        _time.Advance(TimeSpan.FromSeconds(0.3));
        input.Enter(typed);

        Assert.Equal(Time(gameShowed) - SyncInput.TypingLag, _clock.GameTimeAt(typedAt));
    }

    // 58 typed this long after the first keystroke of 26. From 1.5 s on, the seconds and the wait
    // round to a minute: the game has crossed into 25 since it showed the minutes.
    [Theory]
    [InlineData(1.0, "26:58")]
    [InlineData(1.4, "26:58")]
    [InlineData(1.6, "25:58")]
    [InlineData(3.0, "25:58")]
    public void Seconds_land_in_the_minute_below_once_the_game_has_crossed_into_it(double later, string gameShowed)
    {
        RunTo("34:20");
        var minutesAt = _time.Now;
        var minutes = TypeMinutes("26");
        _time.Advance(minutesAt + TimeSpan.FromSeconds(later) - _time.Now);
        var input = new SyncInput(_clock, ClockField.Seconds, minutes);
        var typedAt = _time.Now;

        input.Keystroke();
        _time.Advance(TimeSpan.FromSeconds(0.3));
        input.Enter("58");

        Assert.Equal(Time(gameShowed) - SyncInput.TypingLag, _clock.GameTimeAt(typedAt));
    }

    // 26 typed, then seconds typed ahead and Enter as the game reaches them, this long after the
    // minutes' first keystroke: 00 when 26 was read at 26:05, 59 at 26:04, and 55 at 26:00, since
    // Enter follows the game reaching the seconds by EnterLag, not TypingLag. 59.7 s on, 59 means the
    // game has crossed two minutes since 26 was read at 26:00.
    [Theory]
    [InlineData("00", 4.0, "26:00")]
    [InlineData("59", 4.0, "25:59")]
    [InlineData("55", 4.0, "25:55")]
    [InlineData("59", 59.7, "24:59")]
    public void Seconds_typed_ahead_after_minutes_land_by_the_Enter(string typed, double later, string gameShowed)
    {
        RunTo("34:50");
        var minutesAt = _time.Now;
        var minutes = TypeMinutes("26");
        var input = new SyncInput(_clock, ClockField.Seconds, minutes);

        input.Keystroke();
        _time.Advance(minutesAt + TimeSpan.FromSeconds(later) - _time.Now);
        input.Enter(typed);

        Assert.Equal(Time(gameShowed) - SyncInput.EnterLag, _clock.GameTime);
    }

    // The game shows 26:05 as 26 is typed, with the clock at 34:48. A minute on, the clock's 25:48 is
    // still 43 s from the game, and seconds no longer right after the minutes land on the nearest
    // minute to it.
    [Theory]
    [InlineData(59.0, "25:05")]
    [InlineData(60.0, "26:05")]
    public void Seconds_are_right_after_the_minutes_for_a_minute_from_their_first_keystroke(double later, string landed)
    {
        RunTo("34:48");
        var minutesAt = _time.Now;
        var minutes = TypeMinutes("26");
        _time.Advance(minutesAt + TimeSpan.FromSeconds(later) - _time.Now);
        var input = new SyncInput(_clock, ClockField.Seconds, minutes);
        var typedAt = _time.Now;

        input.Keystroke();
        _time.Advance(TimeSpan.FromSeconds(0.3));
        input.Enter("05");

        Assert.Equal(Time(landed) - SyncInput.TypingLag, _clock.GameTimeAt(typedAt));
    }

    // The clock, started early, reads 34:30 while the game still shows 35:00, its first second: 35
    // typed, then 58 1.2 s later. The game never shows 35:58, so they land on its nearest time, 34:58.
    [Fact]
    public void Seconds_right_after_35_land_on_a_time_the_game_can_show()
    {
        RunTo("34:30");
        var minutesAt = _time.Now;
        var minutes = TypeMinutes("35");
        _time.Advance(minutesAt + TimeSpan.FromSeconds(1.2) - _time.Now);
        var input = new SyncInput(_clock, ClockField.Seconds, minutes);
        var typedAt = _time.Now;

        input.Keystroke();
        _time.Advance(TimeSpan.FromSeconds(0.3));
        input.Enter("58");

        Assert.Equal(Time("34:58") - SyncInput.TypingLag, _clock.GameTimeAt(typedAt));
    }

    // 26 typed with the clock at 34:48; then an arrow takes it to 26:49, or Stop and Start restart the
    // match and it runs to 34:50. Either has the last word: the seconds land on the nearest minute.
    [Theory]
    [InlineData("arrow", "05", "27:05")]
    [InlineData("restart", "50", "34:50")]
    public void Seconds_after_anything_else_moved_the_clock_land_on_the_nearest_minute(string between, string typed, string landed)
    {
        RunTo("34:48");
        var minutes = TypeMinutes("26");
        if (between == "arrow")
        {
            _clock.Move(TimeSpan.FromSeconds(1));
        }
        else
        {
            _clock.Stop();
            _clock.Start();
            _time.Advance(TimeSpan.FromSeconds(10));
        }
        var input = new SyncInput(_clock, ClockField.Seconds, minutes);
        var typedAt = _time.Now;

        input.Keystroke();
        _time.Advance(TimeSpan.FromSeconds(0.3));
        input.Enter(typed);

        Assert.Equal(Time(landed) - SyncInput.TypingLag, _clock.GameTimeAt(typedAt));
    }

    [Theory]
    [InlineData(ClockField.Minutes, "36")]
    [InlineData(ClockField.Minutes, "99")]
    [InlineData(ClockField.Minutes, "")]
    [InlineData(ClockField.Seconds, "60")]
    [InlineData(ClockField.Seconds, "99")]
    [InlineData(ClockField.Seconds, "")]
    [InlineData(ClockField.Seconds, "-5")]
    [InlineData(ClockField.Seconds, " 5")]
    [InlineData(ClockField.Seconds, "5a")]
    public void A_value_the_game_cannot_show_is_ignored(ClockField field, string typed)
    {
        RunTo("22:02");
        var before = _clock.GameTime;
        var input = new SyncInput(_clock, field);

        input.Keystroke();
        input.Enter(typed);

        Assert.Equal(before, _clock.GameTime);
        Assert.Null(input.Minutes);
    }

    [Fact]
    public void Without_a_keystroke_there_is_nothing_to_apply()
    {
        RunTo("22:02");
        var before = _clock.GameTime;
        var input = new SyncInput(_clock, ClockField.Seconds);

        input.Enter("58");
        input.Leave("58");

        Assert.False(input.Typed);
        Assert.Equal(before, _clock.GameTime);
    }

    [Fact]
    public void An_input_applies_once()
    {
        RunTo("21:44");
        var input = new SyncInput(_clock, ClockField.Seconds);

        input.Keystroke();
        _time.Advance(TimeSpan.FromSeconds(2));
        input.Enter("40");
        input.Leave("40");

        Assert.False(input.Typed);
        Assert.Equal(Time("21:40") - SyncInput.EnterLag, _clock.GameTime);
    }

    [Theory]
    [InlineData("00:02", "58", "00:58")]
    [InlineData("34:58", "02", "34:02")]
    [InlineData("34:58", "01", "34:01")]
    [InlineData("35:00", "00", "35:00")]
    [InlineData("35:00", "58", "34:58")]
    public void Typed_seconds_land_on_a_time_the_game_can_show(string shown, string typed, string gameShowed)
    {
        RunTo(shown);
        var input = new SyncInput(_clock, ClockField.Seconds);
        var typedAt = _time.Now;

        input.Keystroke();
        input.Enter(typed);

        Assert.Equal(ClockState.Running, _clock.State);
        Assert.Equal(Time(gameShowed) - SyncInput.TypingLag, _clock.GameTimeAt(typedAt));
    }

    [Theory]
    [InlineData("00:02", "01")]
    [InlineData("00:10", "00")]
    public void Typed_seconds_at_the_very_end_end_the_match(string shown, string typed)
    {
        RunTo(shown);
        var input = new SyncInput(_clock, ClockField.Seconds);

        input.Keystroke();
        input.Enter(typed);

        Assert.Equal(ClockState.Over, _clock.State);
        Assert.Equal("00:00", _clock.Display);
    }

    // Types minutes as the minutes field takes them, Enter 0.3 s after the first keystroke.
    private TypedMinutes TypeMinutes(string typed)
    {
        var input = new SyncInput(_clock, ClockField.Minutes);
        input.Keystroke();
        _time.Advance(TimeSpan.FromSeconds(0.3));
        input.Enter(typed);
        var minutes = input.Minutes;
        Assert.NotNull(minutes);
        return minutes;
    }

    // Starts the clock and runs it to half a second before it would show the next lower value.
    private void RunTo(string shown)
    {
        _clock.Start();
        _time.Advance(MatchClock.MatchLength - Time(shown) + TimeSpan.FromSeconds(0.5));
        Assert.Equal(shown, _clock.Display);
    }

    private static TimeSpan Time(string shown) =>
        TimeSpan.ParseExact(shown, @"mm\:ss", CultureInfo.InvariantCulture);
}
