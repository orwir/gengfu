using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Gengfu.App.Speech;
using Gengfu.App.Views;
using Gengfu.Core.Settings;

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
