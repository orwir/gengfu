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
    [InlineData(2.0, false)]
    [InlineData(3.5, true)]
    public void Leaving_the_field_applies_and_takes_a_longer_pause_to_count_as_typing_ahead(double pause, bool fromLeaving)
    {
        RunTo("21:44");
        var input = new SyncInput(_clock, ClockField.Seconds);
        var firstKeystroke = _time.Now;

        input.Keystroke();
        _time.Advance(TimeSpan.FromSeconds(pause));
        input.Leave("40");

        if (fromLeaving)
        {
            Assert.Equal(Time("21:40") - SyncInput.EnterLag, _clock.GameTime);
        }
        else
        {
            Assert.Equal(Time("21:40") - SyncInput.TypingLag, _clock.GameTimeAt(firstKeystroke));
        }
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
