using Gengfu.App.Speech;

namespace Gengfu.App.Tests.Speech;

public sealed class GongTests
{
    [Fact]
    public void The_gong_is_struck_twice_0_55_s_apart_and_rings_out_within_2_4_s()
    {
        var gong = Gong.Render();

        Assert.Equal(115200, gong.Length);
        // Its level in 10 ms steps jumps only where the second strike lands.
        var steps = Enumerable.Range(0, gong.Length / 480).Select(step => Sound.Rms(gong[(step * 480)..((step + 1) * 480)])).ToArray();
        Assert.Equal(new[] { 55 }, Enumerable.Range(1, steps.Length - 1).Where(step => steps[step] > 1.5 * steps[step - 1]));
        Assert.True(steps[0] > 0.1);
        Assert.True(Sound.Peak(gong[^480..]) < 0.001);
    }

    [Fact]
    public void Every_gong_is_the_same()
    {
        Assert.Equal(Gong.Render(), Gong.Render());
    }
}
