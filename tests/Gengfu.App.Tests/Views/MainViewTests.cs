using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Gengfu.App.Speech;
using Gengfu.App.Views;
using Gengfu.Core;
using Gengfu.Core.Settings;
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
    public Task While_a_call_is_next_the_line_under_the_clock_asks_to_match_the_in_game_timer() =>
        OnView((session, _, view) =>
        {
            session.Clock.Start();
            view.Refresh();
            Assert.Equal(("Match the in-game timer", ""), Line(view));

            using (new UiCulture("ru-RU"))
            {
                view.Refresh();
                Assert.Equal(("Подгоните под таймер в игре", ""), Line(view));
            }
        });

    [Fact]
    public Task The_line_under_the_clock_says_to_press_Start_what_a_call_says_and_when_calls_and_match_are_over() =>
        OnView((session, time, view) =>
        {
            Assert.Equal(("Press Start when the match begins", ""), Line(view));

            session.Settings.Voice = AppSettings.Gong;
            session.Clock.Start();
            // 25:29, in the first call's window. The schedule fires it here, not the Caller, which
            // with no server marks it not sent.
            time.Advance(TimeSpan.FromSeconds(571));
            Assert.Single(session.Schedule.TakeDue());
            view.Refresh();
            Assert.Equal(("Calling now:", "the gong"), Line(view));

            time.Advance(CallSchedule.CallingFor);
            view.Refresh();
            Assert.Equal(("Match the in-game timer", ""), Line(view));

            time.Advance(TimeSpan.FromMinutes(21));
            view.Refresh();
            Assert.Equal(("No more calls this match", ""), Line(view));

            time.Advance(TimeSpan.FromMinutes(5));
            view.Refresh();
            Assert.Equal(("Match over", ""), Line(view));
        });

    [Theory]
    [MemberData(nameof(WpfApp.Scales), MemberType = typeof(WpfApp))]
    public async Task Start_Stop_and_Test_call_keep_their_icon_and_label_on_the_buttons_centre(double scale)
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
                WpfApp.Lay(view, scale);
                var speaker = (FrameworkElement)((Panel)view.TestButton.Content).Children[0];
                var found = new List<(double Icon, double Capitals)>
                {
                    WpfApp.Offsets(view.StartStop, view.StartIcon, view.StartStopText, scale),
                    WpfApp.Offsets(view.TestButton, speaker, view.TestText, scale),
                };
                session.Clock.Start();
                view.Refresh();
                view.UpdateLayout();
                found.Add(WpfApp.Offsets(view.StartStop, view.StopIcon, view.StartStopText, scale));
                return found;
            });

            Assert.All(offsets, offset =>
            {
                Assert.InRange(offset.Icon, -0.5, 0.5);
                Assert.InRange(offset.Capitals, -0.5, 0.5);
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

    // The line under the clock: its words, and the value after them in the accent.
    private static (string Text, string Value) Line(MainView view) => (view.LineText.Text, view.LineValue.Text);

    // A view in English, on a session in a folder of its own and a clock that moves only when the
    // check moves it. The view reads its strings in its thread's language, Windows' own unless set.
    private async Task OnView(Action<Session, FakeTimeSource, MainView> check)
    {
        var time = new FakeTimeSource();
        var folder = Path.Combine(Path.GetTempPath(), "gengfu-tests", Guid.NewGuid().ToString("N"));
        var session = new Session(
            CultureInfo.GetCultureInfo("en-US"),
            time,
            new CallAudio(() => [], (_, _) => throw new InvalidOperationException(), (_, _) => throw new InvalidOperationException()),
            folder);
        try
        {
            wpf.Invoke(() =>
            {
                using var english = new UiCulture("en-US");
                check(session, time, new MainView(session, () => { }));
                return true;
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
