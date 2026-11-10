using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;
using Gengfu.App.Controls;
using Gengfu.Core;

namespace Gengfu.App.Views;

/// <summary>
/// The main view: the status line, the clock and the line under it, the calls list, the update notice,
/// and Start/Stop, Test call and Settings. It shows the session as it is on each tick.
/// </summary>
internal partial class MainView : UserControl
{
    private const double RowHeight = 36;

    private readonly Session _session;
    private readonly Action _openSettings;
    private readonly ClockDigits _digits;
    private readonly Style _startStyle;
    private StatusTone? _tone;
    private int _followed = -1;

    // The rows show these calls; the schedule makes new ones whenever the events or the lead change.
    private IReadOnlyList<Call> _shown = [];
    private CallRow[] _rows = [];

    public MainView(Session session, Action openSettings)
    {
        InitializeComponent();
        _session = session;
        _openSettings = openSettings;
        _digits = new ClockDigits(session.Clock);
        ClockHost.Child = _digits;
        _startStyle = (Style)FindResource("AccentButtonStyle");
        Refresh();
    }

    /// <summary>Puts keyboard focus where the way back from Settings lands.</summary>
    public void FocusSettings() => SettingsButton.Focus();

    /// <summary>Shows the session as it is now; the window calls this on every tick.</summary>
    public void Refresh()
    {
        _digits.Refresh();
        ShowStartStop();
        ShowStatus();
        ShowLine();
        ShowCalls();
        FollowCalls();
        TestButton.IsEnabled = _session.CanTest;
        TestText.Text = _session.Caller.Testing ? Strings.TestCalling : Strings.TestCall;
        UpdateNotice.Visibility = ShowsUpdate(_session.Updates.State, _session.Clock.State) ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>Whether the update notice shows: a version is ready, and the clock is stopped, so a match is never interrupted.</summary>
    internal static bool ShowsUpdate(UpdateState update, ClockState clock) => update is UpdateState.Ready && clock != ClockState.Running;

    /// <summary>The call the list keeps in view: the one calling, else the next, else the last once all are behind.</summary>
    internal static int InFocus(IReadOnlyList<CallState> states)
    {
        var all = states.ToList();
        return all.IndexOf(CallState.Calling) is >= 0 and var calling ? calling
            : all.IndexOf(CallState.Next) is >= 0 and var next ? next
            : all.Any(state => state is CallState.Called or CallState.NotSent) ? all.Count - 1
            : 0;
    }

    /// <summary>The brush a tone paints its dot with, and the words too unless they stay plain.</summary>
    internal static string ToneBrush(StatusTone tone) => tone switch
    {
        StatusTone.Success => "SystemFillColorSuccessBrush",
        StatusTone.Caution or StatusTone.Retrying => "SystemFillColorCautionBrush",
        StatusTone.Pending => "TextFillColorSecondaryBrush",
        _ => "SystemFillColorCriticalBrush",
    };

    private void ShowStartStop()
    {
        var running = _session.Clock.State == ClockState.Running;
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

    private void ShowStatus()
    {
        var status = _session.Status;
        StatusText.Text = status.Text;
        StatusLink.Visibility = status.LinksToSettings ? Visibility.Visible : Visibility.Collapsed;
        if (_tone == status.Tone)
        {
            return;
        }
        _tone = status.Tone;
        var brush = ToneBrush(status.Tone);
        StatusDot.SetResourceReference(Shape.StrokeProperty, brush);
        if (status.Tone is StatusTone.Pending or StatusTone.Retrying)
        {
            StatusDot.Fill = Brushes.Transparent;
        }
        else
        {
            StatusDot.SetResourceReference(Shape.FillProperty, brush);
        }
        StatusText.SetResourceReference(TextBlock.ForegroundProperty,
            status.Tone is StatusTone.Success or StatusTone.Pending or StatusTone.Off ? "TextFillColorSecondaryBrush" : brush);
    }

    private void ShowLine()
    {
        var schedule = _session.Schedule;
        var state = _session.Clock.State;
        if (state == ClockState.Idle)
        {
            ShowLine(Strings.PressStart, "");
        }
        else if (state == ClockState.Over)
        {
            ShowLine(Strings.MatchOver, "");
        }
        else if (Caller.Joined(schedule.Calls.Where(call => schedule.StateOf(call) == CallState.Calling)).FirstOrDefault() is { } calling)
        {
            ShowLine(Strings.CallingNow, _session.HeardFor([.. calling.Select(call => call.Event)]));
        }
        else if (schedule.Calls.Any(call => schedule.StateOf(call) == CallState.Next))
        {
            // The list shows the next call; the line asks for what keeps every call on time.
            ShowLine(Strings.MatchTimer, "");
        }
        else
        {
            ShowLine(Strings.NoMoreCalls, "");
        }
    }

    private void ShowLine(string text, string value)
    {
        LineText.Text = text;
        LineValue.Text = value;
    }

    private void ShowCalls()
    {
        var schedule = _session.Schedule;
        CallsAhead.Text = Copy.Format(Strings.CallsAhead, schedule.Lead.TotalSeconds);
        if (!ReferenceEquals(_shown, schedule.Calls))
        {
            _shown = schedule.Calls;
            CallRows.Children.Clear();
            _rows = [.. _shown.Select(_ => new CallRow(CallRows))];
            _followed = -1;
        }
        NoCalls.Visibility = _shown.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        NoCallsText.Text = _session.Settings.Events.Count == 0 ? Strings.NoEventsCalls : Strings.NoSpawnsCalls;
        var gameTime = _session.Clock.GameTime;
        foreach (var call in schedule.Calls)
        {
            _rows[call.Index].Show(call, schedule.StateOf(call), gameTime);
        }
    }

    // When the calls don't fit, the list keeps the call in focus second from the top, the one before
    // it still in view. It moves only when that call changes, so a scroll by hand stays meanwhile.
    private void FollowCalls()
    {
        var schedule = _session.Schedule;
        var focus = InFocus([.. schedule.Calls.Select(schedule.StateOf)]);
        if (focus == _followed || !CallsScroller.IsLoaded)
        {
            return;
        }
        _followed = focus;
        CallsScroller.ScrollToVerticalOffset(Math.Max(0, focus - 1) * RowHeight);
    }

    private void OnStartStop(object sender, RoutedEventArgs e)
    {
        if (_session.Clock.State == ClockState.Running)
        {
            _session.Clock.Stop();
        }
        else
        {
            _session.Clock.Start();
        }
        Refresh();
    }

    private async void OnTest(object sender, RoutedEventArgs e) => await _session.TestAsync();

    // Velopack's updater takes over once Gengfu has closed the usual way, off the server and saved.
    private void OnRestart(object sender, RoutedEventArgs e)
    {
        if (_session.Updates.ApplyOnClose())
        {
            Window.GetWindow(this)?.Close();
        }
    }

    private void OnSettings(object sender, RoutedEventArgs e) => _openSettings();

    /// <summary>One row of the calls list: the call's time, the event and its spawn, and how the call stands.</summary>
    private sealed class CallRow
    {
        private const string Check = "M3.5,8.5 L6.5,11.5 L12.5,4.5";
        private const string Speaker = "M2.5,6 L5,6 L8.5,3 L8.5,13 L5,10 L2.5,10 Z M11,5.5 A3.5,3.5 0 0 1 11,10.5";
        private const string Cross = "M4.5,4.5 L11.5,11.5 M11.5,4.5 L4.5,11.5";

        private readonly Rectangle _tint = new() { Opacity = 0.08, Visibility = Visibility.Collapsed };
        private readonly TextBlock _time = new() { Width = 44, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center };
        private readonly TextBlock _name = new() { TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
        private readonly Run _event = new();
        private readonly Run _spawn = new() { FontSize = 12 };
        private readonly Label _mark = new() { Style = (Style)Application.Current.FindResource("CallMark") };
        private readonly Path _icon = new() { Style = (Style)Application.Current.FindResource("Icon"), StrokeThickness = 1.4 };
        private readonly TextBlock _status = new() { FontSize = 12, LineHeight = 16, Margin = new Thickness(6, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        private CallState? _shown;

        public CallRow(Panel host)
        {
            _tint.SetResourceReference(Shape.FillProperty, "AccentFillColorDefaultBrush");
            Typography.SetNumeralAlignment(_time, FontNumeralAlignment.Tabular);
            _name.Inlines.Add(_event);
            _name.Inlines.Add(_spawn);
            _spawn.SetResourceReference(TextElement.ForegroundProperty, "TextFillColorSecondaryBrush");
            _mark.Content = _icon;
            var status = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            status.Children.Add(_mark);
            status.Children.Add(_status);
            var columns = new DockPanel { Margin = new Thickness(16, 0, 16, 0) };
            DockPanel.SetDock(_time, Dock.Left);
            DockPanel.SetDock(status, Dock.Right);
            columns.Children.Add(_time);
            columns.Children.Add(status);
            _name.Margin = new Thickness(12, 0, 0, 0);
            columns.Children.Add(_name);
            var row = new Grid { Height = RowHeight };
            row.Children.Add(_tint);
            row.Children.Add(columns);
            host.Children.Add(row);
        }

        public void Show(Call call, CallState state, TimeSpan gameTime)
        {
            _time.Text = MatchClock.Format(call.At);
            _event.Text = Copy.EventName(call.Event);
            _spawn.Text = " · " + Copy.Format(Strings.Spawns, MatchClock.Format(call.Spawn));
            _status.Text = state switch
            {
                CallState.Next => Copy.Format(Strings.In, Copy.Span(gameTime - call.At)),
                CallState.Calling => Strings.Calling,
                CallState.NotSent => Strings.NotSent,
                _ => "",
            };
            if (_shown == state)
            {
                return;
            }
            _shown = state;
            var hot = state is CallState.Next or CallState.Calling;
            var past = state is CallState.Called or CallState.NotSent;
            _tint.Visibility = hot ? Visibility.Visible : Visibility.Collapsed;
            Paint(_time, hot ? "AccentTextFillColorPrimaryBrush" : past ? "TextFillColorSecondaryBrush" : "TextFillColorPrimaryBrush");
            Paint(_name, past ? "TextFillColorSecondaryBrush" : "TextFillColorPrimaryBrush");
            var statusBrush = state switch
            {
                CallState.NotSent => "SystemFillColorCriticalBrush",
                CallState.Called => "TextFillColorSecondaryBrush",
                _ => "AccentTextFillColorPrimaryBrush",
            };
            Paint(_status, statusBrush);
            _mark.SetResourceReference(Control.ForegroundProperty, statusBrush);
            _status.FontWeight = hot ? FontWeights.SemiBold : FontWeights.Normal;
            _icon.Data = Geometry.Parse(state switch
            {
                CallState.Called => Check,
                CallState.Calling => Speaker,
                CallState.NotSent => Cross,
                _ => "M0,0",
            });
            _mark.Visibility = state is CallState.Called or CallState.Calling or CallState.NotSent ? Visibility.Visible : Visibility.Collapsed;
            // A made call shows only its check, so the check carries the words for a screen reader.
            AutomationProperties.SetName(_mark, state == CallState.Called ? Strings.Called : "");
        }

        private static void Paint(TextBlock text, string brush) => text.SetResourceReference(TextBlock.ForegroundProperty, brush);
    }
}
