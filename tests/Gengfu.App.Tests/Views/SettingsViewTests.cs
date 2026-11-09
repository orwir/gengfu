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
        session.Settings.SoundFile = @"C:\Sounds\jungle.mp3";
        try
        {
            var offsets = wpf.Invoke(() =>
            {
                var view = new SettingsView(session, () => { }, () => { });
                WpfApp.Lay(view, scale);
                var speaker = (FrameworkElement)((Panel)view.TestButton.Content).Children[0];
                var note = (FrameworkElement)((Panel)view.FileName.Parent).Children[0];
                return new[]
                {
                    WpfApp.Offsets(view.TestButton, speaker, view.TestText, scale),
                    WpfApp.Offsets(view.FileBox, note, view.FileName, scale),
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
}
