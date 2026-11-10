using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Gengfu.App.Speech;
using Gengfu.App.Views;
using Gengfu.Core.Settings;
using Gengfu.TeamSpeak;

namespace Gengfu.App.Tests.Views;

[Collection(WpfAppCollection.Name)]
public sealed class SettingsViewTests(WpfApp wpf)
{
    [Fact]
    public async Task Theme_shows_the_saved_choice_applies_a_new_one_at_once_and_tells_the_window()
    {
        var folder = Path.Combine(Path.GetTempPath(), "gengfu-tests", Guid.NewGuid().ToString("N"));
        var session = new Session(
            CultureInfo.GetCultureInfo("en-US"),
            new FakeTimeSource(),
            new CallAudio(() => [], (_, _) => throw new InvalidOperationException(), (_, _) => throw new InvalidOperationException()),
            folder);
        session.Settings.Theme = Theme.Light;
        try
        {
            var (shown, chosen, mode, told) = wpf.Invoke(() =>
            {
                try
                {
                    var told = 0;
                    var view = new SettingsView(session, () => { }, () => told++);
                    var shown = (Theme?)((ComboBoxItem)view.ThemeBox.SelectedItem).Tag;
                    view.ThemeBox.SelectedItem = view.ThemeBox.Items.Cast<ComboBoxItem>().Single(item => (Theme?)item.Tag == Theme.Dark);
                    // The window hears of it after the selection's event, once the dispatcher gets there.
                    Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
                    return (shown, session.Settings.Theme, Application.Current.ThemeMode, told);
                }
                finally
                {
                    AppTheme.Apply(null);
                }
            });

            Assert.Equal(Theme.Light, shown);
            Assert.Equal(Theme.Dark, chosen);
            Assert.Equal(ThemeMode.Dark, mode);
            Assert.Equal(1, told);
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

    [Fact]
    public async Task Under_Test_call_it_says_when_the_server_said_nobody_heard_the_last_call_and_why()
    {
        var folder = Path.Combine(Path.GetTempPath(), "gengfu-tests", Guid.NewGuid().ToString("N"));
        var session = new Session(
            CultureInfo.GetCultureInfo("en-US"),
            new FakeTimeSource(),
            new CallAudio(() => [], (_, _) => throw new InvalidOperationException(), (_, _) => throw new InvalidOperationException()),
            folder);
        session.Settings.Group = new Named(1625, "GvG");
        try
        {
            var (before, group, channels, heard) = wpf.Invoke(() =>
            {
                var view = new SettingsView(session, () => { }, () => { });
                var before = view.NobodyHeardBar.Visibility;
                session.Whispered(session.Target!, session.Connection.State, WhisperResult.ReachedNobody);
                view.Refresh();
                var group = (view.NobodyHeardBar.Visibility, view.NobodyHeardText.Text == Strings.NobodyHeardGroup);
                session.Settings.Listeners = ListenerKind.Channels;
                session.Settings.Channels = [new Named(3978, "Push 2")];
                session.Whispered(session.Target!, session.Connection.State, WhisperResult.ReachedNobody);
                view.Refresh();
                var channels = (view.NobodyHeardBar.Visibility, view.NobodyHeardText.Text == Strings.NobodyHeardChannels);
                session.Whispered(session.Target!, session.Connection.State, WhisperResult.Sent);
                view.Refresh();
                return (before, group, channels, view.NobodyHeardBar.Visibility);
            });

            Assert.Equal(Visibility.Collapsed, before);
            Assert.Equal((Visibility.Visible, true), group);
            Assert.Equal((Visibility.Visible, true), channels);
            Assert.Equal(Visibility.Collapsed, heard);
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

    [Fact]
    public async Task A_privilege_key_waits_behind_its_link_and_the_bar_under_it_says_how_the_server_took_it()
    {
        var folder = Path.Combine(Path.GetTempPath(), "gengfu-tests", Guid.NewGuid().ToString("N"));
        var session = new Session(
            CultureInfo.GetCultureInfo("en-US"),
            new FakeTimeSource(),
            new CallAudio(() => [], (_, _) => throw new InvalidOperationException(), (_, _) => throw new InvalidOperationException()),
            folder);
        try
        {
            var (tabOrder, closed, opened, offline, taken, typedAgain, unknown) = wpf.Invoke(() =>
            {
                var view = new SettingsView(session, () => { }, () => { });
                // Tab follows the tree: from the field to Use.
                var tabOrder = Controls(view.KeyPanel).Where(control => control.Focusable).Select(control => control.Name).ToList();
                var closed = (view.KeyLink.Visibility, view.KeyPanel.Visibility);
                view.KeyLink.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                var opened = (view.KeyLink.Visibility, view.KeyPanel.Visibility, view.KeyBar.Visibility);
                view.KeyBox.Text = "first";
                view.Refresh();
                // Use waits for a connection.
                var offline = (session.PrivilegeKey, view.UseKeyButton.IsEnabled);
                session.KeyUsed(new KeyResult(KeyOutcome.Taken, "Callers"));
                view.Refresh();
                var taken = (view.KeyBar.Visibility, view.KeyBar.Style == view.FindResource("SuccessBar"), view.KeyText.Text, view.KeyBox.Text);
                view.KeyBox.Text = "second";
                view.Refresh();
                var typedAgain = view.KeyBar.Visibility;
                session.KeyUsed(new KeyResult(KeyOutcome.Unknown));
                view.Refresh();
                var unknown = (view.KeyBar.Style == view.FindResource("ErrorBar"), view.KeyText.Text, view.KeyBox.Text);
                return (tabOrder, closed, opened, offline, taken, typedAgain, unknown);
            });

            Assert.Equal(["KeyBox", "UseKeyButton"], tabOrder);
            Assert.Equal((Visibility.Visible, Visibility.Collapsed), closed);
            Assert.Equal((Visibility.Collapsed, Visibility.Visible, Visibility.Collapsed), opened);
            Assert.Equal(("first", false), offline);
            Assert.Equal((Visibility.Visible, true, "Key used — Gengfu is in Callers now.", ""), taken);
            Assert.Equal(Visibility.Collapsed, typedAgain);
            Assert.Equal((true, Strings.KeyUnknown, "second"), unknown);
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

    [Fact]
    public async Task Settings_made_again_opens_the_key_s_field_while_a_key_is_typed_or_its_answer_stands()
    {
        var folder = Path.Combine(Path.GetTempPath(), "gengfu-tests", Guid.NewGuid().ToString("N"));
        var session = new Session(
            CultureInfo.GetCultureInfo("en-US"),
            new FakeTimeSource(),
            new CallAudio(() => [], (_, _) => throw new InvalidOperationException(), (_, _) => throw new InvalidOperationException()),
            folder);
        try
        {
            var (typed, answered) = wpf.Invoke(() =>
            {
                session.TypeKey("first");
                var typed = new SettingsView(session, () => { }, () => { });
                session.KeyUsed(new KeyResult(KeyOutcome.Taken, "Callers"));
                var answered = new SettingsView(session, () => { }, () => { });
                return ((typed.KeyPanel.Visibility, typed.KeyBox.Text), (answered.KeyPanel.Visibility, answered.KeyBar.Visibility));
            });

            Assert.Equal((Visibility.Visible, "first"), typed);
            Assert.Equal((Visibility.Visible, Visibility.Visible), answered);
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

    // The controls under an element in tree order, which is the order Tab takes.
    private static IEnumerable<Control> Controls(DependencyObject parent) =>
        LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>()
            .SelectMany(child => child is Control control ? [control] : Controls(child));

    [Fact]
    public Task Add_event_opens_a_new_one_at_the_end_and_Remove_and_Back_to_default_offer_Undo_until_the_events_change() =>
        OnView((session, view) =>
        {
            Assert.Equal(Visibility.Collapsed, view.BackToDefaultLink.Visibility);
            Click(view.AddEventButton);
            var added = Rows(view)[^1];
            Assert.Equal((2, true, "New event"), (Rows(view).Count, added.IsOpen, added.NameBox.Text));
            Assert.False(Rows(view)[0].IsOpen);
            Assert.Equal(Visibility.Visible, view.BackToDefaultLink.Visibility);

            // Removed at once, the jungle comes back open with Undo, where it was.
            Rows(view)[0].Row.IsExpanded = true;
            Click(Rows(view)[0].RemoveButton);
            Assert.Equal(new[] { "New event" }, Rows(view).Select(row => row.Title.Text));
            Assert.Equal((Visibility.Visible, "“Jungle” removed."), (view.UndoBar.Visibility, view.UndoText.Text));
            Click(view.UndoButton);
            Assert.Equal(new[] { ("Jungle", true), ("New event", true) }, Rows(view).Select(row => (row.Title.Text, row.IsOpen)));
            Assert.Equal(Visibility.Collapsed, view.UndoBar.Visibility);

            Click(view.BackToDefaultLink);
            Assert.Equal((Visibility.Visible, "Back to default: the jungle alone."), (view.UndoBar.Visibility, view.UndoText.Text));
            Assert.Equal((1, Visibility.Collapsed), (Rows(view).Count, view.BackToDefaultLink.Visibility));
            Assert.True(session.Settings.EventsAreDefault);

            // An edit takes the offer away.
            Rows(view)[0].SpawnsBox.Text = "25";
            Rows(view)[0].ApplySpawns();
            view.Refresh();
            Assert.Equal(Visibility.Collapsed, view.UndoBar.Visibility);
        });

    [Fact]
    public Task The_caution_that_nobody_heard_the_last_call_shows_under_the_Test_call_pressed_last() =>
        OnView((session, view) =>
        {
            session.Settings.Group = new Named(1625, "GvG");
            Click(view.AddEventButton);
            var boss = Rows(view)[1];
            session.Whispered(session.Target!, session.Connection.State, WhisperResult.ReachedNobody);
            view.Refresh();
            Assert.Equal((Visibility.Visible, Visibility.Collapsed), (view.NobodyHeardBar.Visibility, boss.NobodyHeardBar.Visibility));

            Click(boss.TestButton);
            view.Refresh();
            Assert.Equal((Visibility.Collapsed, Visibility.Visible), (view.NobodyHeardBar.Visibility, boss.NobodyHeardBar.Visibility));

            Click(view.TestButton);
            view.Refresh();
            Assert.Equal((Visibility.Visible, Visibility.Collapsed), (view.NobodyHeardBar.Visibility, boss.NobodyHeardBar.Visibility));
        });

    [Fact]
    public Task While_a_Test_call_plays_the_one_pressed_reads_Calling_and_Calls_own_when_no_row_s_was() =>
        OnView((_, view) =>
        {
            Click(view.AddEventButton);
            var (jungle, boss) = (Rows(view)[0], Rows(view)[1]);

            Assert.True(SettingsView.ReadsCalling(testing: true, pressed: null, button: null));
            Assert.False(SettingsView.ReadsCalling(testing: true, pressed: null, button: boss));
            Assert.False(SettingsView.ReadsCalling(testing: true, pressed: boss, button: null));
            Assert.True(SettingsView.ReadsCalling(testing: true, pressed: boss, button: boss));
            Assert.False(SettingsView.ReadsCalling(testing: true, pressed: boss, button: jungle));
            Assert.False(SettingsView.ReadsCalling(testing: false, pressed: boss, button: boss));
        });

    [Fact]
    public Task With_every_event_removed_Settings_says_no_calls_go_out() =>
        OnView((_, view) =>
        {
            Assert.Equal(Visibility.Collapsed, view.NoEventsCard.Visibility);

            Click(Rows(view)[0].RemoveButton);

            Assert.Empty(Rows(view));
            Assert.Equal(Visibility.Visible, view.NoEventsCard.Visibility);
        });

    [Fact]
    public Task A_new_lead_rewrites_the_events_default_words_and_leaves_their_own() =>
        OnView((session, view) =>
        {
            Click(view.AddEventButton);
            Rows(view)[1].WordsBox.Text = "Boss, go go go";

            view.LeadBox.SelectedItem = view.LeadBox.Items.Cast<ComboBoxItem>().Single(item => (TimeSpan)item.Tag == TimeSpan.FromSeconds(45));

            Assert.Equal(new[] { "Jungle in forty-five seconds", "Boss, go go go" }, Rows(view).Select(row => row.WordsBox.Text));
            Assert.Equal("Boss, go go go", session.Settings.Events[1].Text);
        });

    [Theory]
    [MemberData(nameof(WpfApp.Scales), MemberType = typeof(WpfApp))]
    public async Task Test_call_and_the_sound_file_keep_their_icon_and_label_on_one_centre(double scale)
    {
        var folder = Path.Combine(Path.GetTempPath(), "gengfu-tests", Guid.NewGuid().ToString("N"));
        var session = new Session(
            CultureInfo.GetCultureInfo("en-US"),
            new FakeTimeSource(),
            new CallAudio(() => [], (_, _) => throw new InvalidOperationException(), (_, _) => throw new InvalidOperationException()),
            folder);
        session.SetSoundFile(0, @"C:\Sounds\jungle.mp3");
        try
        {
            var offsets = wpf.Invoke(() =>
            {
                var view = new SettingsView(session, () => { }, () => { });
                var row = Rows(view)[0];
                row.Row.IsExpanded = true;
                WpfApp.Lay(view, scale);
                FrameworkElement Icon(Button button) => (FrameworkElement)((Panel)button.Content).Children[0];
                var note = (FrameworkElement)((Panel)row.FileName.Parent).Children[0];
                var plus = (Panel)view.AddEventButton.Content;
                return new[]
                {
                    WpfApp.Offsets(view.TestButton, Icon(view.TestButton), view.TestText, scale),
                    WpfApp.Offsets(row.TestButton, Icon(row.TestButton), row.TestText, scale),
                    WpfApp.Offsets(row.FileBox, note, row.FileName, scale),
                    WpfApp.Offsets(view.AddEventButton, (FrameworkElement)plus.Children[0], (TextBlock)plus.Children[1], scale),
                };
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
    public async Task Each_bar_s_icon_and_the_connection_dot_centre_on_the_capitals_of_their_words_first_line(double scale)
    {
        var folder = Path.Combine(Path.GetTempPath(), "gengfu-tests", Guid.NewGuid().ToString("N"));
        // A settings file with an error, for its bar; with no voice at all, the no-voice hint shows.
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "settings.json"), "{\n  \"server\": \"ts.example.com\"\n  \"lead\": 30\n}");
        var session = new Session(
            CultureInfo.GetCultureInfo("en-US"),
            new FakeTimeSource(),
            new CallAudio(() => [], (_, _) => throw new InvalidOperationException(), (_, _) => throw new InvalidOperationException()),
            folder);
        session.Settings.Group = new Named(1625, "GvG");
        // A second event with nothing to say, for its bar, and a third removed, for Undo's.
        session.AddEvent();
        session.AddEvent();
        session.SetWords(1, "");
        session.RemoveEvent(2);
        session.SoundChanged();
        try
        {
            // The hints follow the calls' sounds, which render off the UI thread.
            Assert.True(SpinWait.SpinUntil(() => session.Settings.Events.All(e => session.ClipOf(e) is not null), TimeSpan.FromSeconds(10)));
            var offsets = wpf.Invoke(() =>
            {
                var view = new SettingsView(session, () => { }, () => { });
                session.KeyUsed(new KeyResult(KeyOutcome.Unknown));
                session.Whispered(session.Target!, session.Connection.State, WhisperResult.ReachedNobody);
                var row = Rows(view)[1];
                row.Row.IsExpanded = true;
                row.SpawnsBox.Text = "61";
                row.ApplySpawns();
                view.Refresh();
                WpfApp.Lay(view, scale);
                Assert.All(
                    [view.FileBar, view.KeyBar, view.SoundHint, view.NobodyHeardBar, view.UndoBar, row.SpawnsBar, row.SoundBar],
                    bar => Assert.Equal(Visibility.Visible, bar.Visibility));
                return new[]
                {
                    WpfApp.Capitals(Icon(view.FileBar), view.FileErrorText, scale),
                    WpfApp.Capitals(Icon(view.KeyBar), view.KeyText, scale),
                    WpfApp.Capitals(Icon(view.SoundHint), view.HintText, scale),
                    WpfApp.Capitals(Icon(view.NobodyHeardBar), view.NobodyHeardText, scale),
                    WpfApp.Capitals((FrameworkElement)((Panel)view.UndoText.Parent).Children[0], view.UndoText, scale),
                    WpfApp.Capitals(Icon(row.SpawnsBar), row.SpawnsBarText, scale),
                    WpfApp.Capitals(Icon(row.SoundBar), row.SoundBarText, scale),
                    WpfApp.Capitals(row.SummaryMark, row.Summary, scale),
                    WpfApp.Capitals(view.ConnectionDot, view.ConnectionText, scale),
                };
            });

            Assert.All(offsets, offset => Assert.InRange(offset, -0.5, 0.5));
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

    // A bar's icon leads the panel it holds.
    private static FrameworkElement Icon(Border bar) => (FrameworkElement)((Panel)bar.Child).Children[0];

    private static List<EventRow> Rows(SettingsView view) => [.. view.EventRows.Children.Cast<EventRow>()];

    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

    // A view in English, on a session in a folder of its own.
    private async Task OnView(Action<Session, SettingsView> check)
    {
        var folder = Path.Combine(Path.GetTempPath(), "gengfu-tests", Guid.NewGuid().ToString("N"));
        var session = new Session(
            CultureInfo.GetCultureInfo("en-US"),
            new FakeTimeSource(),
            new CallAudio(() => [], (_, _) => throw new InvalidOperationException(), (_, _) => throw new InvalidOperationException()),
            folder);
        try
        {
            wpf.Invoke(() =>
            {
                using var english = new UiCulture("en-US");
                check(session, new SettingsView(session, () => { }, () => { }));
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
