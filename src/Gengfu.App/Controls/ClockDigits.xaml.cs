using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Gengfu.Core;

namespace Gengfu.App.Controls;

/// <summary>
/// The clock's minutes and seconds. They read like plain digits, become fields on a click or Tab,
/// and the arrows above and below nudge the clock by a minute or a second. Typed values go through
/// <see cref="SyncInput"/>. Everything is disabled unless the clock runs.
/// </summary>
public partial class ClockDigits : UserControl
{
    private readonly MatchClock _clock;
    private readonly Field _minutes;
    private readonly Field _seconds;

    // Set while the clock writes a field, so only the user's own edits count as keystrokes.
    private bool _showing;

    // The field Enter or Esc left last; Tab from the digits as a whole goes on from it.
    private Field? _left;

    public ClockDigits(MatchClock clock)
    {
        InitializeComponent();
        _clock = clock;
        _minutes = new Field(MinutesBox, ClockField.Minutes, TimeSpan.FromMinutes(1));
        _seconds = new Field(SecondsBox, ClockField.Seconds, TimeSpan.FromSeconds(1));
        MinuteMore.Click += (_, _) => Nudge(_minutes, 1);
        MinuteLess.Click += (_, _) => Nudge(_minutes, -1);
        SecondMore.Click += (_, _) => Nudge(_seconds, 1);
        SecondLess.Click += (_, _) => Nudge(_seconds, -1);
        foreach (var box in new[] { MinutesBox, SecondsBox })
        {
            box.GotKeyboardFocus += OnFocused;
            box.LostKeyboardFocus += OnUnfocused;
            box.PreviewMouseLeftButtonDown += OnPressed;
            box.PreviewTextInput += OnTextInput;
            box.TextChanged += OnTextChanged;
            box.PreviewKeyDown += OnKeyDown;
        }
        KeyDown += OnParkedKeyDown;
    }

    /// <summary>Shows the clock as it is now; the window calls this on every tick.</summary>
    public void Refresh()
    {
        IsEnabled = _clock.State == ClockState.Running;
        var (minutes, seconds) = MatchClock.Shown(_clock.GameTime);
        Show(_minutes, minutes);
        Show(_seconds, seconds);
    }

    // Until the user types, a field shows the running clock, kept selected while it has focus.
    private void Show(Field field, int value)
    {
        var text = value.ToString("00", CultureInfo.InvariantCulture);
        if (field.Input?.Typed == true || field.Box.Text == text)
        {
            return;
        }
        _showing = true;
        field.Box.Text = text;
        _showing = false;
        if (field.Box.IsKeyboardFocused)
        {
            field.Box.SelectAll();
        }
    }

    // Drops anything typed in the field, since the value it was typed against has moved.
    private void Nudge(Field field, int direction)
    {
        _clock.Move(direction * field.Step);
        if (field.Input is not null)
        {
            field.Input = new SyncInput(_clock, field.Kind);
        }
        Refresh();
    }

    private void OnFocused(object sender, KeyboardFocusChangedEventArgs e)
    {
        var field = FieldOf(sender);
        field.Input = new SyncInput(_clock, field.Kind);
        field.Box.SelectAll();
    }

    private void OnUnfocused(object sender, KeyboardFocusChangedEventArgs e)
    {
        var field = FieldOf(sender);
        field.Input?.Leave(field.Box.Text);
        field.Input = null;
        Refresh();
    }

    // A click selects the whole value, so typing replaces it, instead of placing the caret.
    private void OnPressed(object sender, MouseButtonEventArgs e)
    {
        var box = (TextBox)sender;
        if (!box.IsKeyboardFocused)
        {
            box.Focus();
            e.Handled = true;
        }
    }

    private void OnTextInput(object sender, TextCompositionEventArgs e) =>
        e.Handled = !e.Text.All(char.IsAsciiDigit);

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_showing)
        {
            FieldOf(sender).Input?.Keystroke();
        }
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        var field = FieldOf(sender);
        switch (e.Key)
        {
            case Key.Enter:
                field.Input?.Enter(field.Box.Text);
                LeaveField(field);
                break;
            case Key.Escape:
                field.Input = null;
                LeaveField(field);
                break;
            case Key.Up:
                Nudge(field, 1);
                break;
            case Key.Down:
                Nudge(field, -1);
                break;
            default:
                return;
        }
        e.Handled = true;
    }

    // Enter and Esc park focus on the digits as a whole, so they read as plain text again, as the
    // design has it; the window would hand focus straight back to the field.
    private void LeaveField(Field field)
    {
        _left = field;
        Focus();
    }

    // WPF's Tab from the digits as a whole starts at their first field, the minutes, so seconds
    // typed next would land there. Tab goes on from the field left instead, as from that field.
    private void OnParkedKeyDown(object sender, KeyEventArgs e)
    {
        var modifiers = e.KeyboardDevice.Modifiers;
        if (e.Key == Key.Tab && IsKeyboardFocused && _left is { } left && (modifiers & ~ModifierKeys.Shift) == ModifierKeys.None)
        {
            var direction = modifiers == ModifierKeys.Shift ? FocusNavigationDirection.Previous : FocusNavigationDirection.Next;
            e.Handled = left.Box.MoveFocus(new TraversalRequest(direction));
        }
    }

    private Field FieldOf(object sender) => sender == MinutesBox ? _minutes : _seconds;

    private sealed class Field(TextBox box, ClockField kind, TimeSpan step)
    {
        public TextBox Box { get; } = box;

        public ClockField Kind { get; } = kind;

        public TimeSpan Step { get; } = step;

        /// <summary>The edit in progress while the field has focus.</summary>
        public SyncInput? Input { get; set; }
    }
}
