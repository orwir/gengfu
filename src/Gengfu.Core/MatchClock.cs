using System.Globalization;

namespace Gengfu.Core;

/// <summary>Idle before Start and after Stop, running while it counts down, over at 00:00.</summary>
public enum ClockState
{
    Idle,
    Running,
    Over,
}

/// <summary>
/// The match clock, mirroring the in-game GvG timer from 35:00 down to 00:00. It keeps one anchor,
/// "at instant t the game showed r", so the game time now is r − (now − t). Start and every
/// correction only replace the anchor, which carries everything timed in game time along with it.
/// </summary>
public sealed class MatchClock(ITimeSource time)
{
    public static TimeSpan MatchLength { get; } = TimeSpan.FromMinutes(35);

    private bool _started;
    private TimeSpan _anchorInstant;
    private TimeSpan _anchorGameTime;

    public ITimeSource Time { get; } = time;

    public ClockState State => StateAt(Time.Now);

    /// <summary>The game time now; 35:00 while idle.</summary>
    public TimeSpan GameTime => GameTimeAt(Time.Now);

    /// <summary>The game time as the game shows it, such as "34:59".</summary>
    public string Display => Format(GameTime);

    public void Start()
    {
        _started = true;
        SetAnchor(Time.Now, MatchLength);
    }

    public void Stop() => _started = false;

    /// <summary>Shifts the game time; a positive shift leaves more of the match to play.</summary>
    public void Move(TimeSpan by)
    {
        var now = Time.Now;
        if (StateAt(now) == ClockState.Running)
        {
            SetAnchor(now, GameTimeAt(now) + by);
        }
    }

    /// <summary>Takes "at <paramref name="instant"/> the game showed <paramref name="gameTime"/>" as the anchor.</summary>
    public void Anchor(TimeSpan instant, TimeSpan gameTime)
    {
        if (State == ClockState.Running)
        {
            SetAnchor(instant, gameTime);
        }
    }

    /// <summary>
    /// Formats a game time the way the game shows it: whole seconds rounded up, so 35:00 holds for
    /// the first second and 00:00 appears only at the very end.
    /// </summary>
    public static string Format(TimeSpan gameTime)
    {
        var seconds = (gameTime.Ticks + TimeSpan.TicksPerSecond - 1) / TimeSpan.TicksPerSecond;
        return string.Create(CultureInfo.InvariantCulture, $"{seconds / 60:00}:{seconds % 60:00}");
    }

    private ClockState StateAt(TimeSpan now) =>
        !_started ? ClockState.Idle
        : GameTimeAt(now) > TimeSpan.Zero ? ClockState.Running
        : ClockState.Over;

    private TimeSpan GameTimeAt(TimeSpan now)
    {
        if (!_started)
        {
            return MatchLength;
        }
        var left = _anchorGameTime - (now - _anchorInstant);
        return left > TimeSpan.Zero ? left : TimeSpan.Zero;
    }

    // The game timer never reads above 35:00 or below 00:00, so a correction past either end stops there.
    private void SetAnchor(TimeSpan instant, TimeSpan gameTime)
    {
        _anchorInstant = instant;
        _anchorGameTime = TimeSpan.FromTicks(Math.Clamp(gameTime.Ticks, 0, MatchLength.Ticks));
    }
}
