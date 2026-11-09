using System.Globalization;

namespace Gengfu.Core;

/// <summary>The clock fields the user can type into.</summary>
public enum ClockField
{
    Minutes,
    Seconds,
}

/// <summary>
/// One typed correction to a clock field, from its first keystroke to Enter or leaving the field.
/// Typed minutes keep the running seconds. Typed seconds land on the nearest minute and are timed
/// from the first keystroke, or from Enter after a pause, so the user can type ahead and press
/// Enter as the game reaches the number; leaving the field never counts as typing ahead. A value
/// the game can't show is ignored.
/// </summary>
public sealed class SyncInput(MatchClock clock, ClockField field)
{
    /// <summary>
    /// The game time at the first keystroke is the typed seconds less this: the game rounds up, so
    /// the value it showed was half a second in on average, and reading it takes about 0.6 s more.
    /// </summary>
    public static TimeSpan TypingLag { get; } = TimeSpan.FromSeconds(1.1);

    /// <summary>The game time at a typed-ahead Enter is the typed seconds less this reaction time.</summary>
    public static TimeSpan EnterLag { get; } = TimeSpan.FromSeconds(0.2);

    /// <summary>A longer wait between the last keystroke and Enter means the user typed ahead.</summary>
    public static TimeSpan EnterPause { get; } = TimeSpan.FromSeconds(1.2);

    private TimeSpan? _firstKeystroke;
    private TimeSpan _lastKeystroke;

    /// <summary>Whether a keystroke waits to be applied; Enter or leaving the field spends it.</summary>
    public bool Typed => _firstKeystroke is not null;

    /// <summary>Records that the user changed the field's text, now.</summary>
    public void Keystroke()
    {
        var now = clock.Time.Now;
        _firstKeystroke ??= now;
        _lastKeystroke = now;
    }

    public void Enter(string text) => Apply(text, typedAhead: clock.Time.Now - _lastKeystroke > EnterPause);

    // Leaving often comes well after typing what the game showed: a Tab, a click, a switch back to
    // the game. So it never reads as typing ahead, however long the pause.
    public void Leave(string text) => Apply(text, typedAhead: false);

    // Applies once: a typed-ahead Enter followed by leaving the field must not be re-timed.
    private void Apply(string text, bool typedAhead)
    {
        if (_firstKeystroke is not { } firstKeystroke)
        {
            return;
        }
        _firstKeystroke = null;
        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
        {
            return;
        }
        if (field == ClockField.Minutes)
        {
            // A minute holds for 60 s, so nobody types one ahead: typed minutes describe the clock as
            // typing began, even when its own minute ticks over before they apply.
            if (value <= MatchClock.MatchLength.TotalMinutes)
            {
                clock.Move(TimeSpan.FromMinutes(value - MatchClock.Shown(clock.GameTimeAt(firstKeystroke)).Minutes));
            }
            return;
        }
        if (value >= 60)
        {
            return;
        }
        // The moment the seconds describe: the game reaching them, or showing them as typing began.
        var instant = typedAhead ? clock.Time.Now : firstKeystroke;
        if (NearestMinute(clock.GameTimeAt(instant), value, typedAhead ? EnterLag : TypingLag) is { } gameTime)
        {
            clock.Anchor(instant, gameTime);
        }
    }

    // The clock is off by seconds, not minutes, so the typed seconds belong to the minute that
    // lands nearest to it: 58 typed at 22:02 means 21:58. Only a time the game can show counts;
    // the lag may then take the match past 00:00, which ends it.
    private static TimeSpan? NearestMinute(TimeSpan current, int seconds, TimeSpan lag)
    {
        var shownMinute = MatchClock.Shown(current).Minutes;
        TimeSpan? nearest = null;
        for (var minute = shownMinute - 1; minute <= shownMinute + 1; minute++)
        {
            var reading = TimeSpan.FromMinutes(minute) + TimeSpan.FromSeconds(seconds);
            if (reading < TimeSpan.Zero || reading > MatchClock.MatchLength)
            {
                continue;
            }
            var candidate = reading - lag;
            if (nearest is null || (candidate - current).Duration() < (nearest.Value - current).Duration())
            {
                nearest = candidate;
            }
        }
        return nearest;
    }
}
