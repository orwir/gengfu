using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Gengfu.App.Views;

namespace Gengfu.App;

/// <summary>
/// Gengfu's one window: the main view, or Settings in its place with the way back in the title bar.
/// It drives the session from a 50 ms tick and redraws whichever view shows.
/// </summary>
public partial class MainWindow : Window
{
    private const int DwmWindowCornerPreference = 33;
    private const int DwmRound = 2;

    private readonly Session _session = new(CultureInfo.CurrentUICulture);
    private MainView _main;
    private SettingsView? _settings;

    public MainWindow()
    {
        InitializeComponent();
        AppLanguage.Apply(_session.Language);
        _main = new MainView(_session, ShowSettings);
        ShowMain();
        // The digits change on whole game seconds; a short tick shows each change within 50 ms.
        _ = new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Normal, (_, _) => Tick(), Dispatcher);
        // A voice added in Windows' settings meanwhile is picked up when the user comes back.
        Activated += (_, _) => _session.SoundChanged();
        _session.SettingsReplaced += (_, _) => RemakeViews();
        _ = _session.ConnectSavedAsync();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        // Windows 11 rounds and outlines a window that draws its own title bar only when asked;
        // Windows 10 knows no such attribute and keeps square corners.
        var round = DwmRound;
        _ = DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, DwmWindowCornerPreference, ref round, sizeof(int));
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        // Esc leaves Settings, unless a list or a field inside took it first.
        if (e.Key == Key.Escape && Page.Content is SettingsView)
        {
            e.Handled = true;
            ShowMain();
        }
        base.OnKeyDown(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        // Leaving the server takes up to 2 s; until the connection is gone, its thread keeps the process alive.
        Task.Run(() => _session.DisposeAsync().AsTask()).Wait(TimeSpan.FromSeconds(3));
    }

    private void Tick()
    {
        _session.Tick();
        if (Page.Content is SettingsView settings)
        {
            settings.Refresh();
        }
        else
        {
            _main.Refresh();
        }
    }

    private void ShowMain()
    {
        var fromSettings = Page.Content is SettingsView;
        Page.Content = _main;
        _main.Refresh();
        ShowTitleBar();
        if (fromSettings)
        {
            // The view takes focus once it is back in the window, after layout.
            _ = Dispatcher.InvokeAsync(_main.FocusSettings, DispatcherPriority.Loaded);
        }
    }

    private void ShowSettings()
    {
        _settings ??= new SettingsView(_session, OnLanguageChosen);
        Page.Content = _settings;
        ShowTitleBar();
        BackButton.Focus();
    }

    // The strings and the settings are read when a view is made, so a new language, or settings
    // taken from the file, take new views. Whichever view showed stays, Settings where it was scrolled.
    private void RemakeViews()
    {
        var inSettings = Page.Content is SettingsView;
        var offset = _settings?.ScrollOffset ?? 0;
        _main = new MainView(_session, ShowSettings);
        _settings = inSettings ? new SettingsView(_session, OnLanguageChosen) : null;
        Page.Content = inSettings ? _settings : _main;
        ShowTitleBar();
        if (_settings is { } settings)
        {
            _ = Dispatcher.InvokeAsync(() => settings.ScrollOffset = offset, DispatcherPriority.Loaded);
        }
    }

    // Focus goes back to Language, where the user just chose one.
    private void OnLanguageChosen()
    {
        RemakeViews();
        _ = Dispatcher.InvokeAsync(() => _settings?.FocusLanguage(), DispatcherPriority.Loaded);
    }

    private void ShowTitleBar()
    {
        var inSettings = Page.Content is SettingsView;
        BackButton.Visibility = inSettings ? Visibility.Visible : Visibility.Collapsed;
        AppIcon.Visibility = inSettings ? Visibility.Collapsed : Visibility.Visible;
        Caption.Text = inSettings ? Strings.Settings : "Gengfu";
        AutomationProperties.SetName(BackButton, Strings.Back);
        AutomationProperties.SetName(MinimizeButton, Strings.Minimize);
        AutomationProperties.SetName(CloseButton, Strings.Close);
        MinimizeButton.ToolTip = Strings.Minimize;
        CloseButton.ToolTip = Strings.Close;
    }

    private void OnBack(object sender, RoutedEventArgs e) => ShowMain();

    private void OnMinimize(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnClose(object sender, RoutedEventArgs e) => Close();

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
}
