using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Gengfu.App.Speech;
using Gengfu.App.Views;
using Gengfu.Core;
using Gengfu.Core.Settings;

namespace Gengfu.App.Tests.Views;

[Collection(WpfAppCollection.Name)]
public sealed class EventRowTests(WpfApp wpf)
{
    [Fact]
    public Task Closed_an_event_shows_its_name_and_spawn_times_and_open_its_fields_in_Tab_s_order() =>
        OnRow((_, row) =>
        {
            Assert.Equal(("Jungle", "25:00 · 20:00 · 15:00 · 10:00 · 05:00", Visibility.Collapsed), (row.Title.Text, row.Summary.Text, row.SummaryMark.Visibility));
            Assert.False(row.IsOpen);

            // Tab follows the tree: the name, the bin, the times, the words or the file's Change, Test call, then the switch.
            var tabOrder = Controls((DependencyObject)row.Row.Content).Where(control => control.Focusable).Select(control => control.Name);
            Assert.Equal(["NameBox", "RemoveButton", "SpawnsBox", "WordsBox", "ChangeFile", "TestButton", "SoundSwitch"], tabOrder);
            Assert.Equal(("Jungle", "25:00, 20:00, 15:00, 10:00, 05:00", "Jungle in thirty seconds"), (row.NameBox.Text, row.SpawnsBox.Text, row.WordsBox.Text));
        });

    [Fact]
    public Task Typed_spawn_times_apply_written_as_the_game_shows_them_and_ones_Gengfu_can_t_read_change_nothing() =>
        OnRow((session, row) =>
        {
            // Typing alone changes nothing: half a time is no time.
            row.SpawnsBox.Text = "15 25";
            Assert.Equal(CallSchedule.GvgSpawns, session.Settings.Events[0].Spawns);

            row.ApplySpawns();
            Assert.Equal([TimeSpan.FromMinutes(25), TimeSpan.FromMinutes(15)], session.Settings.Events[0].Spawns);
            Assert.Equal("25:00, 15:00", row.SpawnsBox.Text);

            row.SpawnsBox.Text = "25, 61:00";
            row.ApplySpawns();
            row.Refresh(testing: false, nobodyHeard: false);
            Assert.Equal([TimeSpan.FromMinutes(25), TimeSpan.FromMinutes(15)], session.Settings.Events[0].Spawns);
            Assert.Equal("25, 61:00", row.SpawnsBox.Text);
            Assert.Equal((Visibility.Visible, "Can’t read “61:00”: write times as 25:00 or 25, up to 35:00. The calls keep the times from before."),
                (row.SpawnsBar.Visibility, row.SpawnsBarText.Text));
            Assert.Equal(("Can’t read the spawn times", Visibility.Visible), (row.Summary.Text, row.SummaryMark.Visibility));

            row.SpawnsBox.Text = "";
            row.ApplySpawns();
            row.Refresh(testing: false, nobodyHeard: false);
            Assert.Empty(session.Settings.Events[0].Spawns);
            Assert.Equal((Visibility.Collapsed, "No spawn times yet"), (row.SpawnsBar.Visibility, row.Summary.Text));
        });

    [Fact]
    public Task A_renamed_event_s_default_words_follow_its_name_and_its_own_words_stay() =>
        OnRow((session, row) =>
        {
            row.NameBox.Text = "Boss";
            row.Refresh(testing: false, nobodyHeard: false);
            Assert.Equal(("Boss", "Boss", "Boss in thirty seconds"), (session.Settings.Events[0].Name, row.Title.Text, row.WordsBox.Text));

            // An empty name is the jungle, as the field's placeholder says.
            row.NameBox.Text = "";
            row.Refresh(testing: false, nobodyHeard: false);
            Assert.Equal((null, "Jungle", Visibility.Visible), (session.Settings.Events[0].Name, row.Title.Text, row.NamePlaceholder.Visibility));
            Assert.Equal("Jungle in thirty seconds", row.WordsBox.Text);

            row.WordsBox.Text = "Forest, go go go";
            row.NameBox.Text = "Forest";
            Assert.Equal(("Forest, go go go", "Forest, go go go"), (session.Settings.Events[0].Text, row.WordsBox.Text));
        });

    [Fact]
    public Task An_event_with_a_file_of_its_own_shows_the_file_in_place_of_its_words_and_gives_them_back() =>
        OnRow((session, row) =>
        {
            session.SetSoundFile(0, @"C:\Sounds\jungle.mp3");
            row.Refresh(testing: false, nobodyHeard: false);
            Assert.Equal((Visibility.Collapsed, Visibility.Visible, "jungle.mp3", Strings.UseText),
                (row.WordsPanel.Visibility, row.FilePanel.Visibility, row.FileName.Text, row.SoundSwitch.Content));
            Assert.Equal("25:00 · 20:00 · 15:00 · 10:00 · 05:00 · jungle.mp3", row.Summary.Text);

            row.SoundSwitch.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            row.Refresh(testing: false, nobodyHeard: false);
            Assert.Null(session.Settings.Events[0].SoundFile);
            Assert.Equal((Visibility.Visible, Visibility.Collapsed, Strings.UseSoundFile), (row.WordsPanel.Visibility, row.FilePanel.Visibility, row.SoundSwitch.Content));
        });

    [Fact]
    public Task The_rows_own_Test_call_reads_Calling_while_its_call_plays_and_says_under_it_when_nobody_heard() =>
        OnRow((_, row) =>
        {
            row.Refresh(testing: true, nobodyHeard: true);

            Assert.Equal((Strings.TestCalling, Visibility.Visible), (row.TestText.Text, row.NobodyHeardBar.Visibility));
            Assert.Equal(Strings.NobodyHeardGroup, row.NobodyHeardText.Text);
        });

    // The controls under an element in tree order, which is the order Tab takes.
    private static IEnumerable<Control> Controls(DependencyObject parent) =>
        LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>()
            .SelectMany(child => child is Control control ? [control] : Controls(child));

    // The jungle's row, open, in English, on a session in a folder of its own.
    private async Task OnRow(Action<Session, EventRow> check)
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
                check(session, new EventRow(session, 0, open: false, () => { }, _ => { }, _ => { }));
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
