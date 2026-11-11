using System.Windows.Controls;
using System.Windows.Input;
using Gengfu.App.Controls;
using Gengfu.Core;

namespace Gengfu.App.Tests.Controls;

[Collection(WpfAppCollection.Name)]
public sealed class ClockDigitsTests(WpfApp wpf)
{
    [Fact]
    public void Tab_after_Enter_in_the_minutes_goes_on_to_the_seconds_so_typed_seconds_land_there() =>
        OnDigits((time, clock, digits, _) =>
        {
            // The game shows 26:05; the clock, started late, reads 34:20.
            time.Advance(TimeSpan.FromSeconds(40));
            digits.Refresh();

            digits.MinutesBox.Focus();
            WpfApp.Type("26");
            WpfApp.Press(Key.Enter);
            // The digits read as plain text again, focus on the clock as a whole.
            Assert.Same(digits, WpfApp.Focused());
            WpfApp.Press(Key.Tab);
            WpfApp.Type("05");
            WpfApp.Press(Key.Enter);

            // Timed from the first keystroke, less the time it takes to read the game's timer. Typed
            // into the minutes, 05 would have put the clock at 05:20.
            Assert.Equal("26:04", clock.Display);
        });

    [Fact]
    public void Tab_after_Esc_in_the_seconds_goes_on_past_the_clock_with_nothing_applied() =>
        OnDigits((_, clock, digits, after) =>
        {
            digits.SecondsBox.Focus();
            WpfApp.Type("3");
            WpfApp.Press(Key.Escape);
            WpfApp.Press(Key.Tab);

            Assert.Same(after, WpfApp.Focused());
            Assert.Equal("35:00", clock.Display);
        });

    [Fact]
    public void Shift_Tab_after_Enter_in_the_seconds_goes_back_to_the_minutes() =>
        OnDigits((_, _, digits, _) =>
        {
            digits.SecondsBox.Focus();
            WpfApp.Press(Key.Enter);
            using (WpfApp.Hold(Key.LeftShift))
            {
                WpfApp.Press(Key.Tab);
            }

            Assert.Same(digits.MinutesBox, WpfApp.Focused());
        });

    // The digits of a running clock in a window, with the stop that follows them, as Stop follows
    // them in the main window while the clock runs.
    private void OnDigits(Action<FakeTimeSource, MatchClock, ClockDigits, Button> check) =>
        wpf.Invoke(() =>
        {
            var time = new FakeTimeSource();
            var clock = new MatchClock(time);
            clock.Start();
            var digits = new ClockDigits(clock);
            var after = new Button { Content = "After" };
            var window = WpfApp.Show(new StackPanel { Children = { digits, after } });
            try
            {
                digits.Refresh();
                check(time, clock, digits, after);
            }
            finally
            {
                window.Close();
            }
            return true;
        });
}
