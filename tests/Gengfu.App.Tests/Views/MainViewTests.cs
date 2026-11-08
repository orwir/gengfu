using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Gengfu.App.Speech;
using Gengfu.App.Views;
using Gengfu.Core;
using static Gengfu.Core.CallState;

namespace Gengfu.App.Tests.Views;

[Collection(WpfAppCollection.Name)]
public sealed class MainViewTests(WpfApp wpf)
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

    [Fact]
    public async Task Start_Stop_and_Test_call_keep_their_icon_and_label_on_the_buttons_centre()
    {
        var folder = Path.Combine(Path.GetTempPath(), "gengfu-tests", Guid.NewGuid().ToString("N"));
        var session = new Session(
            CultureInfo.GetCultureInfo("en-US"),
            new FakeTimeSource(),
            new CallAudio(() => [], (_, _) => throw new InvalidOperationException(), (_, _) => throw new InvalidOperationException()),
            folder);
        try
        {
            var offsets = wpf.Invoke(() =>
            {
                var view = new MainView(session, () => { });
                WpfApp.Lay(view);
                var speaker = (FrameworkElement)((Panel)view.TestButton.Content).Children[0];
                var found = new List<(double Icon, double Label)>
                {
                    WpfApp.Offsets(view.StartStop, view.StartIcon, view.StartStopText),
                    WpfApp.Offsets(view.TestButton, speaker, view.TestText),
                };
                session.Clock.Start();
                view.Refresh();
                view.UpdateLayout();
                found.Add(WpfApp.Offsets(view.StartStop, view.StopIcon, view.StartStopText));
                return found;
            });

            Assert.All(offsets, offset =>
            {
                Assert.InRange(offset.Icon, -0.5, 0.5);
                Assert.InRange(offset.Label, -0.5, 0.5);
            });
        }
        finally
        {
            await session.DisposeAsync();
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
    }
}
