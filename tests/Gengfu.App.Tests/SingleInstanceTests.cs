using System.Windows.Interop;

namespace Gengfu.App.Tests;

[Collection(WpfAppCollection.Name)]
public sealed class SingleInstanceTests(WpfApp wpf)
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

    [Fact]
    public void A_later_start_finds_only_the_window_of_the_Gengfu_on_its_own_name()
    {
        var name = @"Local\Gengfu-tests-" + Guid.NewGuid().ToString("N");

        // A bare, hidden window: closing a WPF Window would shut the shared App down.
        var (same, other) = wpf.Invoke(() =>
        {
            using var window = new HwndSource(new HwndSourceParameters("Gengfu test") { WindowStyle = 0 });
            using var first = new SingleInstance(name);
            first.Mark(window.Handle);
            using var later = new SingleInstance(name);
            using var elsewhere = new SingleInstance(name + "-elsewhere");
            return (later.IsMarked(window.Handle), elsewhere.IsMarked(window.Handle));
        });

        Assert.True(same);
        Assert.False(other);
    }
}
