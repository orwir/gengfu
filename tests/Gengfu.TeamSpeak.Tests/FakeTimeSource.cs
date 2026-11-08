using Gengfu.Core;

namespace Gengfu.TeamSpeak.Tests;

/// <summary>A time source that moves only when a test, or a fake sleep, advances it.</summary>
internal sealed class FakeTimeSource : ITimeSource
{
    // Away from zero, so nothing can lean on time starting at the origin.
    public TimeSpan Now { get; private set; } = TimeSpan.FromHours(5);

    public void Advance(TimeSpan by) => Now += by;
}
