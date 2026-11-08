namespace Gengfu.Core.Tests;

public class SystemTimeSourceTests
{
    [Fact]
    public void Now_moves_with_real_time()
    {
        var source = new SystemTimeSource();
        var before = source.Now;

        Thread.Sleep(20);

        Assert.True(source.Now > before);
    }
}
