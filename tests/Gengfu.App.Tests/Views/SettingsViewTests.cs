using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Gengfu.App.Speech;
using Gengfu.App.Views;

namespace Gengfu.App.Tests.Views;

[Collection(WpfAppCollection.Name)]
public sealed class SettingsViewTests(WpfApp wpf)
{
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
                var view = new SettingsView(session, () => { });
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
