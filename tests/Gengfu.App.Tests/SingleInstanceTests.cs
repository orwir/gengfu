namespace Gengfu.App.Tests;

public sealed class SingleInstanceTests
{
    [Fact]
    public void A_second_Gengfu_gives_way_to_the_first_until_the_first_has_closed()
    {
        var name = @"Local\Gengfu-tests-" + Guid.NewGuid().ToString("N");

        using (var first = new SingleInstance(name))
        {
            Assert.True(first.IsFirst);
            using var second = new SingleInstance(name);
            Assert.False(second.IsFirst);
        }

        using var next = new SingleInstance(name);
        Assert.True(next.IsFirst);
    }
}
