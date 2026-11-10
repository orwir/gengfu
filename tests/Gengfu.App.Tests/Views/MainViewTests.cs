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

    [Fact]
    public Task The_calls_list_follows_the_events_as_Settings_edits_them_mid_match_too() =>
        OnView((session, time, view) =>
        {
            session.Clock.Start();
            time.Advance(TimeSpan.FromSeconds(571));
            Assert.Single(session.Schedule.TakeDue());
            view.Refresh();
            Assert.Equal(5, view.CallRows.Children.Count);

            session.AddEvent();
            session.RenameEvent(1, "Boss");
            session.SetSpawns(1, [TimeSpan.FromMinutes(22)]);
            view.Refresh();

            Assert.Equal(6, view.CallRows.Children.Count);
            Assert.Equal(Visibility.Collapsed, view.NoCalls.Visibility);
            // The call made before the edit stays made.
            Assert.Equal(CallState.Calling, session.Schedule.StateOf(session.Schedule.Calls[0]));
        });

    [Fact]
    public Task With_no_calls_the_list_says_why() =>
        OnView((session, _, view) =>
        {
            Assert.Equal(Visibility.Collapsed, view.NoCalls.Visibility);

            session.SetSpawns(0, []);
            view.Refresh();
            Assert.Equal((Visibility.Visible, "No spawn times, so no calls."), (view.NoCalls.Visibility, view.NoCallsText.Text));
            Assert.Empty(view.CallRows.Children);

            session.RemoveEvent(0);
            view.Refresh();
            Assert.Equal((Visibility.Visible, "No events, so no calls."), (view.NoCalls.Visibility, view.NoCallsText.Text));
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

    [Theory]
    [MemberData(nameof(WpfApp.Scales), MemberType = typeof(WpfApp))]
    public Task Each_mark_beside_words_centres_on_the_capitals_of_their_first_line(double scale) =>
        OnView((session, time, view) =>
        {
            // A call not sent and the next one calling, at 25:29 and a call later.
            session.Clock.Start();
            time.Advance(TimeSpan.FromSeconds(571));
            var notSent = Assert.Single(session.Schedule.TakeDue());
            session.Schedule.MarkNotSent(notSent);
            time.Advance(notSent.At - session.Schedule.Calls[notSent.Index + 1].At);
            var calling = Assert.Single(session.Schedule.TakeDue());
            view.Refresh();
            WpfApp.Lay(view, scale);
            List<double> offsets = [Mark(view, notSent, scale), Mark(view, calling, scale), WpfApp.Capitals(view.StatusDot, view.StatusText, scale)];

            // The update notice, with the clock stopped; its words take two lines.
            session.Clock.Stop();
            view.UpdateNotice.Visibility = Visibility.Visible;
            view.UpdateLayout();
            Assert.True(view.UpdateText.ActualHeight >= 2 * view.UpdateText.LineHeight);
            offsets.Add(WpfApp.Capitals((FrameworkElement)((Panel)view.UpdateText.Parent).Children[0], view.UpdateText, scale));

            Assert.All(offsets, offset => Assert.InRange(offset, -0.5, 0.5));
        });

    // A row in the calls list holds its columns; the last but one holds the call's mark and its words.
    private static double Mark(MainView view, Call call, double scale)
    {
        var columns = (Panel)((Panel)view.CallRows.Children[call.Index]).Children[1];
        var status = (Panel)columns.Children[1];
        return WpfApp.Capitals((FrameworkElement)((Label)status.Children[0]).Content, (TextBlock)status.Children[1], scale);
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
