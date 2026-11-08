using Gengfu.App.Views;
using Gengfu.Core;
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

    [Fact]
    public void The_update_notice_shows_a_ready_version_only_while_the_clock_is_stopped()
    {
        var ready = new UpdateState.Ready("0.1.1");

        Assert.True(MainView.ShowsUpdate(ready, ClockState.Idle));
        Assert.True(MainView.ShowsUpdate(ready, ClockState.Over));
        Assert.False(MainView.ShowsUpdate(ready, ClockState.Running));
        Assert.False(MainView.ShowsUpdate(new UpdateState.Downloading("0.1.1"), ClockState.Idle));
        Assert.False(MainView.ShowsUpdate(new UpdateState.UpToDate(), ClockState.Idle));
    }
}
