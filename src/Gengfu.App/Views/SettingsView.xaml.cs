using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Gengfu.App.Speech;
using Gengfu.Core;
using Gengfu.Core.Settings;
using Gengfu.TeamSpeak;
using Microsoft.Win32;

namespace Gengfu.App.Views;

/// <summary>
/// Settings: the TeamSpeak server, who hears the calls, what a call plays, the language and the
/// theme. Every change applies at once and goes into the settings file; the server password stays
/// out of it.
/// </summary>
internal partial class SettingsView : UserControl
{
    // A typed text is rendered once the typing pauses, not on every keystroke.
    private static readonly TimeSpan TypingPause = TimeSpan.FromMilliseconds(600);

    private readonly Session _session;
    private readonly Action _languageChanged;
    private readonly Action _themeChanged;
    private readonly DispatcherTimer _typing;

    // Set while the view writes a control, so only the user's own changes count.
    private bool _showing;
    private StatusTone? _tone;
    private CallProblem? _problem;
    private KeyResult? _keyShown;

    // Whether the privilege key's field is open in place of its link.
    private bool _keyOpen;

    public SettingsView(Session session, Action languageChanged, Action themeChanged)
    {
        InitializeComponent();
        _session = session;
        _languageChanged = languageChanged;
        _themeChanged = themeChanged;
        _typing = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher) { Interval = TypingPause };
        _typing.Tick += (_, _) =>
        {
            _typing.Stop();
            session.SoundChanged();
        };

        _showing = true;
        ServerBox.Text = session.Settings.Server;
        PasswordBox.Password = session.Password;
        NicknameBox.Text = session.Settings.Nickname;
        KeyBox.Text = session.PrivilegeKey;
        // A remade view opens the field again when a key is typed or an answer stands.
        _keyOpen = session.PrivilegeKey.Length > 0 || session.KeyAnswer is not null;
        GroupRadio.IsChecked = session.Settings.Listeners == ListenerKind.Group;
        ChannelsRadio.IsChecked = session.Settings.Listeners == ListenerKind.Channels;
        LeadBox.ItemsSource = AppSettings.Leads.Select(lead => new ComboBoxItem { Content = Copy.Lead(lead), Tag = lead }).ToList();
        LeadBox.SelectedItem = LeadBox.Items.Cast<ComboBoxItem>().First(item => (TimeSpan)item.Tag == session.Settings.Lead);
        SayBox.Text = session.CallText;
        LanguageBox.ItemsSource = new[]
        {
            new ComboBoxItem { Content = Strings.SameAsWindows, Tag = null },
            new ComboBoxItem { Content = "English", Tag = "en" },
            new ComboBoxItem { Content = "Русский", Tag = "ru" },
        };
        LanguageBox.SelectedItem = LanguageBox.Items.Cast<ComboBoxItem>().First(item => (string?)item.Tag == session.Settings.Language);
        ThemeBox.ItemsSource = new[]
        {
            new ComboBoxItem { Content = Strings.SameAsWindows, Tag = null },
            new ComboBoxItem { Content = Strings.Light, Tag = Theme.Light },
            new ComboBoxItem { Content = Strings.Dark, Tag = Theme.Dark },
        };
        ThemeBox.SelectedItem = ThemeBox.Items.Cast<ComboBoxItem>().First(item => (Theme?)item.Tag == session.Settings.Theme);
        VersionText.Text = "Gengfu " + AppVersion.Current;
        _showing = false;
        ShowPlaceholders();
        _ = ShowVoicesAsync();
        Refresh();
    }

    public double ScrollOffset
    {
        get => Scroller.VerticalOffset;
        set => Scroller.ScrollToVerticalOffset(value);
    }

    public void FocusLanguage() => LanguageBox.Focus();

    public void FocusTheme() => ThemeBox.Focus();

    /// <summary>Shows the session as it is now; the window calls this on every tick.</summary>
    public void Refresh()
    {
        _showing = true;
        ShowFileError();
        ShowConnection();
        ShowKey();
        ShowListeners();
        ShowSound();
        ShowNobodyHeard();
        TestButton.IsEnabled = _session.CanCall && !_session.Caller.Testing;
        TestText.Text = _session.Caller.Testing ? Strings.TestCalling : Strings.TestCall;
        var updateLine = Copy.UpdateLine(_session.Updates.State);
        UpdateText.Text = updateLine ?? "";
        UpdateText.Visibility = updateLine is null ? Visibility.Collapsed : Visibility.Visible;
        _showing = false;
    }

    private void ShowFileError()
    {
        var error = _session.FileError;
        FileBar.Visibility = error is null ? Visibility.Collapsed : Visibility.Visible;
        FileErrorText.Text = error is null ? "" : Copy.FileError(error);
    }

    private void ShowConnection()
    {
        var offersConnect = _session.OffersConnect;
        ConnectButton.Content = offersConnect ? Strings.Connect : Strings.Disconnect;
        ConnectButton.IsEnabled = !offersConnect || !string.IsNullOrWhiteSpace(_session.Settings.Server);
        var status = _session.SettingsStatus;
        ConnectionText.Text = status.Text;
        if (_tone == status.Tone)
        {
            return;
        }
        _tone = status.Tone;
        var brush = MainView.ToneBrush(status.Tone);
        ConnectionDot.SetResourceReference(Shape.StrokeProperty, brush);
        if (status.Tone is StatusTone.Pending or StatusTone.Retrying)
        {
            ConnectionDot.Fill = Brushes.Transparent;
        }
        else
        {
            ConnectionDot.SetResourceReference(Shape.FillProperty, brush);
        }
        ConnectionText.SetResourceReference(TextBlock.ForegroundProperty,
            status.Tone is StatusTone.Critical or StatusTone.Retrying ? brush : "TextFillColorSecondaryBrush");
    }

    private void ShowKey()
    {
        KeyLink.Visibility = _keyOpen ? Visibility.Collapsed : Visibility.Visible;
        KeyPanel.Visibility = _keyOpen ? Visibility.Visible : Visibility.Collapsed;
        // A key the server took is spent, and the session empties it.
        if (KeyBox.Text != _session.PrivilegeKey)
        {
            KeyBox.Text = _session.PrivilegeKey;
        }
        UseKeyButton.IsEnabled = _session.CanUseKey;
        var answer = _session.KeyAnswer;
        KeyBar.Visibility = answer is null ? Visibility.Collapsed : Visibility.Visible;
        if (answer is null || answer == _keyShown)
        {
            return;
        }
        _keyShown = answer;
        var taken = answer.Outcome == KeyOutcome.Taken;
        KeyBar.Style = (Style)FindResource(taken ? "SuccessBar" : "ErrorBar");
        KeyDisc.SetResourceReference(Shape.FillProperty, taken ? "SystemFillColorSuccessBrush" : "SystemFillColorCriticalBrush");
        // A check for a key the server took, a cross for one it didn't.
        KeyMark.Data = Geometry.Parse(taken ? "M5,8.25 L7,10.25 L11,6" : "M5.75,5.75 L10.25,10.25 M10.25,5.75 L5.75,10.25");
        KeyText.Text = Copy.KeyAnswer(answer);
    }

    private void ShowListeners()
    {
        var settings = _session.Settings;
        var connected = _session.Connection.State.Status == ConnectionStatus.Connected;
        var byGroup = settings.Listeners == ListenerKind.Group;
        GroupBox.IsEnabled = byGroup && connected;
        ChannelsButton.IsEnabled = !byGroup && connected;
        WhoNote.Text = byGroup ? Strings.WhoNoteGroup : Strings.WhoNoteChannels;
        ChannelsText.Text = settings.Channels switch
        {
            [] => Strings.PickChannels,
            [var channel] => channel.Name,
            var channels => Copy.ChannelCount(channels.Count, _session.Language),
        };

        var groups = Session.GroupChoices(connected, _session.Groups, settings.Group);
        if (!groups.SequenceEqual(GroupBox.Items.Cast<ComboBoxItem>().Select(item => (Named)item.Tag)))
        {
            GroupBox.ItemsSource = groups.Select(group => new ComboBoxItem { Content = group.Name, Tag = group }).ToList();
        }
        GroupBox.SelectedItem = GroupBox.Items.Cast<ComboBoxItem>().FirstOrDefault(item => ((Named)item.Tag).Id == settings.Group?.Id);
        GroupPlaceholder.Text = connected ? Strings.ChooseGroup : Strings.ConnectFirst;
        GroupPlaceholder.Visibility = GroupBox.SelectedItem is null ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ShowSound()
    {
        var file = _session.Settings.SoundFile;
        SpeechPanel.Visibility = file is null ? Visibility.Visible : Visibility.Collapsed;
        FilePanel.Visibility = file is null ? Visibility.Collapsed : Visibility.Visible;
        FileName.Text = file is null ? "" : System.IO.Path.GetFileName(file);
        FileBox.ToolTip = file;
        SoundSwitch.Content = file is null ? Strings.UseSoundFile : Strings.UseText;
        // A hint about the other kind of sound is left over from before a switch: the render runs behind.
        var problem = _session.Clip?.Problem ?? CallProblem.None;
        ShowHint(problem == CallProblem.FileUnreadable == (file is not null) ? problem : CallProblem.None);
    }

    private void ShowNobodyHeard()
    {
        NobodyHeardBar.Visibility = _session.NobodyHeard ? Visibility.Visible : Visibility.Collapsed;
        NobodyHeardText.Text = _session.Settings.Listeners == ListenerKind.Group ? Strings.NobodyHeardGroup : Strings.NobodyHeardChannels;
    }

    private void ShowHint(CallProblem problem)
    {
        if (_problem == problem)
        {
            return;
        }
        _problem = problem;
        SoundHint.Visibility = problem == CallProblem.None ? Visibility.Collapsed : Visibility.Visible;
        var noVoice = problem == CallProblem.NoVoice;
        SoundHint.Style = (Style)FindResource(noVoice ? "InfoBar" : "CautionBar");
        HintText.Text = problem switch
        {
            CallProblem.NoVoice => Strings.NoVoice,
            CallProblem.NoText => Strings.NoText,
            CallProblem.SpeechFailed => Strings.SpeechFailed,
            _ => Strings.FileUnreadable,
        };
        HintLink.Visibility = noVoice ? Visibility.Visible : Visibility.Collapsed;
        HintDisc.SetResourceReference(Shape.FillProperty, noVoice ? "AccentFillColorDefaultBrush" : "SystemFillColorCautionBrush");
        HintStem.SetResourceReference(Shape.StrokeProperty, "TextOnAccentFillColorPrimaryBrush");
        HintDot.SetResourceReference(Shape.FillProperty, "TextOnAccentFillColorPrimaryBrush");
        // An "i" for information, a "!" for a call that plays the gong.
        HintStem.Data = Geometry.Parse(noVoice ? "M8,7.25 L8,11.25" : "M8,4.5 L8,8.75");
        Canvas.SetTop(HintDot, noVoice ? 3.95 : 10.15);
    }

    private void ShowPlaceholders()
    {
        ServerPlaceholder.Visibility = ServerBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        PasswordPlaceholder.Visibility = PasswordBox.Password.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        KeyPlaceholder.Visibility = KeyBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    // Windows lists its voices off the UI thread; a voice added meanwhile shows the next time the list opens.
    private async Task ShowVoicesAsync()
    {
        var installed = await Task.Run(Voices.Installed);
        var settings = _session.Settings;
        var language = _session.Language;
        var chosen = settings.Voice == AppSettings.Gong
            ? AppSettings.Gong
            : Voices.Pick(installed, settings.Voice, language)?.Id ?? AppSettings.Gong;
        List<ComboBoxItem> items = [new() { Content = Strings.GongNoVoice, Tag = AppSettings.Gong }];
        items.AddRange(Voices.ForList(installed, language).Select(voice => new ComboBoxItem { Content = Copy.VoiceLabel(voice), Tag = voice.Id }));
        _showing = true;
        VoiceBox.ItemsSource = items;
        VoiceBox.SelectedItem = items.First(item => (string)item.Tag == chosen);
        _showing = false;
    }

    private void OnServerChanged(object sender, TextChangedEventArgs e)
    {
        ShowPlaceholders();
        if (!_showing)
        {
            _session.Settings.Server = ServerBox.Text;
        }
    }

    private void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        ShowPlaceholders();
        if (!_showing)
        {
            _session.Password = PasswordBox.Password;
        }
    }

    private void OnNicknameChanged(object sender, TextChangedEventArgs e)
    {
        if (!_showing)
        {
            _session.Settings.Nickname = NicknameBox.Text;
        }
    }

    // Enter in a TeamSpeak field connects, as Connect would.
    private void OnLoginKey(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _session.OffersConnect && ConnectButton.IsEnabled)
        {
            e.Handled = true;
            _ = _session.ConnectAsync();
        }
    }

    private async void OnConnect(object sender, RoutedEventArgs e)
    {
        if (_session.OffersConnect)
        {
            await _session.ConnectAsync();
        }
        else
        {
            await _session.DisconnectAsync();
        }
    }

    // The field opens in place of the link, with focus in it.
    private void OnKeyLink(object sender, RoutedEventArgs e)
    {
        _keyOpen = true;
        Refresh();
        KeyBox.Focus();
    }

    private void OnKeyChanged(object sender, TextChangedEventArgs e)
    {
        ShowPlaceholders();
        if (!_showing)
        {
            _session.TypeKey(KeyBox.Text);
        }
    }

    // Enter in the key's field uses it, as Use would.
    private void OnKeyKey(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _session.CanUseKey)
        {
            e.Handled = true;
            _ = UseKeyAsync();
        }
    }

    private async void OnUseKey(object sender, RoutedEventArgs e) => await UseKeyAsync();

    // Use is off while the key is on its way, which takes focus from it; focus goes to the field, which
    // keeps a key the server didn't take.
    private async Task UseKeyAsync()
    {
        var fromButton = UseKeyButton.IsKeyboardFocused;
        await _session.UseKeyAsync();
        Refresh();
        if (fromButton)
        {
            KeyBox.Focus();
        }
    }

    private void OnListenersChecked(object sender, RoutedEventArgs e)
    {
        if (!_showing)
        {
            _session.Settings.Listeners = ChannelsRadio.IsChecked == true ? ListenerKind.Channels : ListenerKind.Group;
        }
    }

    private async void OnGroupsOpened(object sender, EventArgs e) => await _session.RefreshListsAsync();

    private void OnGroupChosen(object sender, SelectionChangedEventArgs e)
    {
        if (!_showing && GroupBox.SelectedItem is ComboBoxItem { Tag: Named group })
        {
            _session.ChooseGroup(group);
        }
    }

    // The channels open in a list with a box to tick for each, in the server's tree order.
    private async void OnChannelsOpen(object sender, RoutedEventArgs e)
    {
        await _session.RefreshListsAsync();
        var chosen = _session.Settings.Channels.Select(channel => channel.Id).ToHashSet();
        ChannelList.Children.Clear();
        foreach (var channel in _session.Channels)
        {
            var box = new CheckBox
            {
                Content = channel.Name,
                Tag = channel,
                IsChecked = chosen.Contains(channel.Id),
                Margin = new Thickness(8 + (20 * channel.Depth), 0, 8, 0),
            };
            box.Checked += OnChannelTicked;
            box.Unchecked += OnChannelTicked;
            ChannelList.Children.Add(box);
        }
        // As wide as the card, its right edge on the button's.
        var width = ((FrameworkElement)ChannelsButton.Parent).ActualWidth;
        ((FrameworkElement)ChannelsPopup.Child).Width = width;
        ChannelsPopup.HorizontalOffset = ChannelsButton.ActualWidth - width;
        ChannelsPopup.IsOpen = true;
        // While the list is open, a click on the button only closes it.
        ChannelsButton.IsHitTestVisible = false;
        ChannelList.Children.OfType<CheckBox>().FirstOrDefault()?.Focus();
    }

    private void OnChannelTicked(object sender, RoutedEventArgs e) =>
        _session.ChooseChannels(
        [
            .. ChannelList.Children.OfType<CheckBox>()
                .Where(box => box.IsChecked == true)
                .Select(box => (ChannelInfo)box.Tag)
                .Select(channel => new Named(channel.Id, channel.Name)),
        ]);

    private void OnChannelsKey(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Escape or Key.Enter)
        {
            e.Handled = true;
            ChannelsPopup.IsOpen = false;
        }
    }

    private void OnChannelsClosed(object? sender, EventArgs e)
    {
        ChannelsButton.IsHitTestVisible = true;
        if (ChannelList.IsKeyboardFocusWithin)
        {
            ChannelsButton.Focus();
        }
    }

    private void OnLeadChosen(object sender, SelectionChangedEventArgs e)
    {
        if (_showing || LeadBox.SelectedItem is not ComboBoxItem { Tag: TimeSpan lead })
        {
            return;
        }
        _session.ChangeLead(lead);
        if (_session.Settings.CallText is null)
        {
            _showing = true;
            SayBox.Text = _session.CallText;
            _showing = false;
        }
    }

    private void OnSayChanged(object sender, TextChangedEventArgs e)
    {
        if (_showing)
        {
            return;
        }
        _session.SetCallText(SayBox.Text);
        _typing.Stop();
        _typing.Start();
    }

    private async void OnVoicesOpened(object sender, EventArgs e) => await ShowVoicesAsync();

    private void OnVoiceChosen(object sender, SelectionChangedEventArgs e)
    {
        if (!_showing && VoiceBox.SelectedItem is ComboBoxItem { Tag: string voice })
        {
            _session.Settings.Voice = voice;
            _session.SoundChanged();
        }
    }

    private void OnSoundSwitch(object sender, RoutedEventArgs e)
    {
        if (_session.Settings.SoundFile is null)
        {
            ChooseFile();
        }
        else
        {
            _session.Settings.SoundFile = null;
            _session.SoundChanged();
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
        if (_session.Settings.SoundFile is { } current)
        {
            dialog.InitialDirectory = System.IO.Path.GetDirectoryName(current);
        }
        if (dialog.ShowDialog(Window.GetWindow(this)) == true)
        {
            _session.Settings.SoundFile = dialog.FileName;
            _session.SoundChanged();
        }
    }

    private async void OnTest(object sender, RoutedEventArgs e) => await _session.Caller.TestAsync();

    // In the app Windows opens .json files with, or in Notepad when there is none.
    private void OnOpenSettingsFile(object sender, RoutedEventArgs e)
    {
        _session.SaveNow();
        try
        {
            Process.Start(new ProcessStartInfo(_session.SettingsFile) { UseShellExecute = true });
        }
        catch (Win32Exception)
        {
            Process.Start(new ProcessStartInfo("notepad.exe") { ArgumentList = { _session.SettingsFile } });
        }
    }

    private void OnOpenLanguageRegion(object sender, RoutedEventArgs e) =>
        Process.Start(new ProcessStartInfo("ms-settings:regionlanguage") { UseShellExecute = true });

    private void OnLanguageChosen(object sender, SelectionChangedEventArgs e)
    {
        if (_showing || LanguageBox.SelectedItem is not ComboBoxItem item)
        {
            return;
        }
        _session.ChangeLanguage((string?)item.Tag);
        // Views read their strings when they are made, so the window makes them again, after this event.
        _ = Dispatcher.InvokeAsync(_languageChanged);
    }

    // A new theme gives every control its template afresh, which scrolls each list back to its top,
    // so the window makes the views again, as for a language, after this event.
    private void OnThemeChosen(object sender, SelectionChangedEventArgs e)
    {
        if (!_showing && ThemeBox.SelectedItem is ComboBoxItem item)
        {
            _session.ChangeTheme((Theme?)item.Tag);
            _ = Dispatcher.InvokeAsync(_themeChanged);
        }
    }
}
