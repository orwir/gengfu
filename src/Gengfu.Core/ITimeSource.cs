namespace Gengfu.Core;

/// <summary>
/// Monotonic time: never jumps when the wall clock is set or synced, so the match clock only moves
/// when the user moves it. Tests replace it with a fake they advance by hand.
/// </summary>
public interface ITimeSource
{
    /// <summary>Time since an arbitrary fixed origin.</summary>
    TimeSpan Now { get; }
}
