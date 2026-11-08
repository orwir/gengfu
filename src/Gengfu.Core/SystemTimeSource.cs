using System.Diagnostics;

namespace Gengfu.Core;

/// <summary>The real monotonic time, from the high-resolution performance counter.</summary>
public sealed class SystemTimeSource : ITimeSource
{
    private readonly long _origin = Stopwatch.GetTimestamp();

    public TimeSpan Now => Stopwatch.GetElapsedTime(_origin);
}
