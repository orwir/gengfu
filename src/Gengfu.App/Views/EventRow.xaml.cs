using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using Gengfu.App.Speech;
using Gengfu.Core.Settings;
using Microsoft.Win32;

namespace Gengfu.App.Views;

/// <summary>
/// One event in Settings: closed, its name and spawn times, or what is wrong with it; open, its name,
/// spawn times, and words or sound file, with its own Test call. Every edit applies at once, but
/// typed spawn times wait for Enter or leaving the field, since half of one is no time at all.
/// </summary>
internal partial class EventRow : UserControl
{
    private readonly Session _session;
    private readonly Action _typed;
    private readonly Action<int> _removed;
    private readonly Action<EventRow> _tested;

    // Set while the row writes a field, so only the user's own changes count.
    private bool _showing;

    // Typed spawn times Gengfu couldn't read: the part it couldn't.
    private string? _unreadable;

    /// <param name="typed">Told of every keystroke in a field whose words the call says, to render once typing pauses.</param>
    /// <param name="removed">Told the event at this index is to go.</param>
    /// <param name="tested">Told this row's Test call was pressed.</param>
    public EventRow(Session session, int index, bool open, Action typed, Action<int> removed, Action<EventRow> tested)
    {
        InitializeComponent();
        _session = session;
        Index = index;
        _typed = typed;
        _removed = removed;
        _tested = tested;
        Row.IsExpanded = open;
        var matchEvent = Event;
        _showing = true;
        NameBox.Text = Copy.EventName(matchEvent);
        SpawnsBox.Text = MatchEvent.WriteSpawns(matchEvent.Spawns);
        WordsBox.Text = session.WordsOf(matchEvent);
        _showing = false;
        ShowPlaceholders();
        AutomationProperties.SetName(RemoveButton, Copy.Format(Strings.RemoveEventNamed, Copy.EventName(matchEvent)));
        Refresh(testing: false, nobodyHeard: false);
    }

    public int Index { get; }

    public bool IsOpen => Row.IsExpanded;

    private MatchEvent Event => _session.Settings.Events[Index];

    /// <summary>Shows the event as it is now; Settings calls this on every tick.</summary>
    /// <param name="testing">Whether this row's Test call is playing.</param>
    /// <param name="nobodyHeard">Whether to say, under this row's Test call, that the server said nobody heard the last call.</param>
    public void Refresh(bool testing, bool nobodyHeard)
    {
        var matchEvent = Event;
        var name = Copy.EventName(matchEvent);
        var problem = _session.ClipOf(matchEvent)?.Problem ?? CallProblem.None;
        var (summary, wrong) = Copy.EventSummary(matchEvent, problem, _unreadable is not null);
        Title.Text = name;
        Summary.Text = summary;
        Summary.SetResourceReference(TextBlock.ForegroundProperty, wrong ? "SystemFillColorCautionBrush" : "TextFillColorSecondaryBrush");
        SummaryMark.Visibility = wrong ? Visibility.Visible : Visibility.Collapsed;
        AutomationProperties.SetName(Row, name + ", " + summary);

        SpawnsBar.Visibility = _unreadable is null ? Visibility.Collapsed : Visibility.Visible;
        SpawnsBarText.Text = _unreadable is null ? "" : Copy.Format(Strings.SpawnsUnreadable, _unreadable);

        var file = matchEvent.SoundFile;
        WordsPanel.Visibility = file is null ? Visibility.Visible : Visibility.Collapsed;
        FilePanel.Visibility = file is null ? Visibility.Collapsed : Visibility.Visible;
        FileName.Text = file is null ? "" : Path.GetFileName(file);
        FileBox.ToolTip = file;
        SoundSwitch.Content = file is null ? Strings.UseSoundFile : Strings.UseText;
        // A file that can't play gives way to the words, unless the gong is the voice.
        var says = _session.Settings.Voice == AppSettings.Gong ? null : _session.WordsOf(matchEvent);
        var hint = problem switch
        {
            CallProblem.NoText => Strings.NoText,
            CallProblem.SpeechFailed => Strings.SpeechFailed,
            CallProblem.FileMissing or CallProblem.FileUnreadable when file is not null => Copy.FileHint(problem, Path.GetFileName(file), says),
            _ => null,
        };
        SoundBar.Visibility = hint is null ? Visibility.Collapsed : Visibility.Visible;
        SoundBarText.Text = hint ?? "";

        TestButton.IsEnabled = _session.CanCall && !_session.Caller.Testing;
        TestText.Text = testing ? Strings.TestCalling : Strings.TestCall;
        AutomationProperties.SetName(TestButton, testing ? Strings.TestCalling : Copy.Format(Strings.TestCallFor, name));
        NobodyHeardBar.Visibility = nobodyHeard ? Visibility.Visible : Visibility.Collapsed;
        NobodyHeardText.Text = _session.Settings.Listeners == ListenerKind.Group ? Strings.NobodyHeardGroup : Strings.NobodyHeardChannels;
    }

    /// <summary>Shows the words afresh while they are the default, which follows the name, the lead and the language.</summary>
    public void ShowWords()
    {
        if (Event.Text is null)
        {
            _showing = true;
            WordsBox.Text = _session.WordsOf(Event);
            _showing = false;
        }
    }

    /// <summary>Puts focus in the name with it selected, so typing replaces it, as for a new event.</summary>
    public void FocusName()
    {
        NameBox.Focus();
        NameBox.SelectAll();
    }

    /// <summary>Puts focus on the header, where the row opens and closes.</summary>
    public void FocusHeader() => (Row.Template.FindName("Header", Row) as UIElement)?.Focus();

    private void ShowPlaceholders()
    {
        NamePlaceholder.Visibility = NameBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        SpawnsPlaceholder.Visibility = SpawnsBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnNameChanged(object sender, TextChangedEventArgs e)
    {
        ShowPlaceholders();
        if (_showing)
        {
            return;
        }
        _session.RenameEvent(Index, NameBox.Text);
        AutomationProperties.SetName(RemoveButton, Copy.Format(Strings.RemoveEventNamed, Copy.EventName(Event)));
        ShowWords();
        _typed();
    }

    private void OnSpawnsChanged(object sender, TextChangedEventArgs e) => ShowPlaceholders();

    // Enter applies the typed times, as leaving the field does.
    private void OnSpawnsKey(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            ApplySpawns();
        }
    }

    private void OnSpawnsLeft(object sender, KeyboardFocusChangedEventArgs e) => ApplySpawns();

    /// <summary>
    /// Applies the typed spawn times, as Enter or leaving the field does. Times Gengfu reads go to the
    /// event and come back written as the game shows them; ones it can't read change nothing, and
    /// the bar says which part.
    /// </summary>
    internal void ApplySpawns()
    {
        var (spawns, unreadable) = MatchEvent.ReadSpawns(SpawnsBox.Text);
        _unreadable = unreadable;
        if (unreadable is null)
        {
            if (!spawns.SequenceEqual(Event.Spawns))
            {
                _session.SetSpawns(Index, spawns);
            }
            _showing = true;
            SpawnsBox.Text = MatchEvent.WriteSpawns(spawns);
            SpawnsBox.CaretIndex = SpawnsBox.Text.Length;
            _showing = false;
        }
    }

    private void OnWordsChanged(object sender, TextChangedEventArgs e)
    {
        if (!_showing)
        {
            _session.SetWords(Index, WordsBox.Text);
            _typed();
        }
    }

    private void OnRemove(object sender, RoutedEventArgs e) => _removed(Index);

    private void OnTest(object sender, RoutedEventArgs e)
    {
        _tested(this);
        _ = _session.TestAsync(Event);
    }

    private void OnSoundSwitch(object sender, RoutedEventArgs e)
    {
        if (Event.SoundFile is null)
        {
            ChooseFile();
        }
        else
        {
            _session.SetSoundFile(Index, null);
            ShowWords();
        }
    }

    private void OnChooseFile(object sender, RoutedEventArgs e) => ChooseFile();

    private void ChooseFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = Strings.ChooseSoundFile,
            Filter = $"{Strings.SoundFiles}|*.mp3;*.wav;*.wma;*.m4a;*.aac;*.flac|{Strings.AllFiles}|*.*",
        };
        if (Event.SoundFile is { } current)
        {
            dialog.InitialDirectory = Path.GetDirectoryName(current);
        }
        if (dialog.ShowDialog(Window.GetWindow(this)) == true)
        {
            _session.SetSoundFile(Index, dialog.FileName);
        }
    }
}
