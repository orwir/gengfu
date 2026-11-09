namespace Gengfu.TeamSpeak.Tests;

public sealed class WhisperTargetTests
{
    [Fact]
    public void The_same_channels_are_the_same_listeners()
    {
        WhisperTarget pushes = new WhisperTarget.Channels([3977, 3978]);
        WhisperTarget again = new WhisperTarget.Channels([3977, 3978]);

        Assert.True(pushes == again);
        Assert.Equal(pushes.GetHashCode(), again.GetHashCode());
        Assert.NotEqual(pushes, new WhisperTarget.Channels([3977]));
        Assert.NotEqual(new WhisperTarget.Channels([12]), (WhisperTarget)new WhisperTarget.ServerGroup(12));
    }
}
