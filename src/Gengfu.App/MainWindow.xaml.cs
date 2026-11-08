using System.Windows;
using System.Windows.Threading;
using Gengfu.App.Controls;
using Gengfu.Core;

namespace Gengfu.App;

public partial class MainWindow : Window
{
    private readonly MatchClock _clock = new(new SystemTimeSource());
    private readonly ClockDigits _digits;
    private readonly Style _startStyle;

    public MainWindow()
    {
        InitializeComponent();
        _digits = new ClockDigits(_clock);
        ClockHost.Child = _digits;
        _startStyle = (Style)FindResource("AccentButtonStyle");
        Refresh();
        // The digits change on whole game seconds; a short tick shows each change within 50 ms.
        _ = new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Normal, (_, _) => Refresh(), Dispatcher);
    }

    private void Refresh()
    {
        _digits.Refresh();
        var running = _clock.State == ClockState.Running;
        if (running)
        {
            StartStop.ClearValue(StyleProperty);
        }
        else
        {
            StartStop.Style = _startStyle;
        }
        StartStop.Padding = running ? new Thickness(12, 0, 12, 0) : new Thickness(12, 0, 16, 0);
        StartIcon.Visibility = running ? Visibility.Collapsed : Visibility.Visible;
        StopIcon.Visibility = running ? Visibility.Visible : Visibility.Collapsed;
        StartStopText.Text = running ? Strings.Stop : Strings.Start;
    }

    private void OnStartStop(object sender, RoutedEventArgs e)
    {
        if (_clock.State == ClockState.Running)
        {
            _clock.Stop();
        }
        else
        {
            _clock.Start();
        }
        Refresh();
    }
}
