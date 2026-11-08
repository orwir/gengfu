using Gengfu.App.Views;
using static Gengfu.Core.CallState;

namespace Gengfu.App.Tests.Views;

public sealed class MainViewTests
{
    [Fact]
    public void The_calls_list_keeps_the_call_calling_or_next_in_view_and_the_last_once_all_are_behind()
    {
        Assert.Equal(0, MainView.InFocus([Later, Later, Later]));
        Assert.Equal(2, MainView.InFocus([Called, NotSent, Next, Later]));
        Assert.Equal(1, MainView.InFocus([Called, Calling, Next, Later]));
        Assert.Equal(3, MainView.InFocus([Called, Called, NotSent, Called]));
    }
}
