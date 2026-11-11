using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Gengfu.App.Tests;

/// <summary>
/// Gengfu's own App, with its styles and the Fluent theme, on one STA thread for every test in its
/// collection: WPF allows one Application in a process, and a view lives on the thread that made it.
/// </summary>
public sealed class WpfApp : IDisposable
{
    private readonly Thread _thread;
    private readonly Dispatcher _dispatcher;

    public WpfApp()
    {
        using var ready = new ManualResetEventSlim();
        Dispatcher? dispatcher = null;
        _thread = new Thread(() =>
        {
            var app = new App();
            app.InitializeComponent();
            // Tests open windows of their own and close them; the App lasts until Dispose ends it.
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            dispatcher = Dispatcher.CurrentDispatcher;
            ready.Set();
            Dispatcher.Run();
        })
        { IsBackground = true };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        ready.Wait();
        _dispatcher = dispatcher!;
        // Only Program opens Gengfu's window, on the folder it names; the App opens none itself,
        // so a test never runs a Gengfu on the user's own settings. Every view test fails if it does.
        if (_dispatcher.Invoke(() => Application.Current.Windows.Count, DispatcherPriority.ApplicationIdle) != 0)
        {
            Dispose();
            throw new InvalidOperationException("Gengfu's App opened a window by itself.");
        }
    }

    /// <summary>Display scales Windows offers, 100% to 200%.</summary>
    public static TheoryData<double> Scales => new() { 1.0, 1.25, 1.5, 1.75, 2.0 };

    /// <summary>Runs on the App's thread, where views can be made and laid out.</summary>
    public T Invoke<T>(Func<T> work) => _dispatcher.Invoke(work);

    /// <summary>
    /// Shows a view in a window of its own, as Gengfu's window holds it, so it can take keyboard focus.
    /// The window is off screen, never activated and out of the taskbar, so a test run leaves the
    /// user's own windows alone. Close it when done.
    /// </summary>
    public static Window Show(UIElement view)
    {
        var window = new Window
        {
            Content = view,
            Width = 380,
            Height = 616,
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false,
            ShowActivated = false,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = -20000,
            Top = -20000,
        };
        window.Show();
        Settle();
        return window;
    }

    /// <summary>
    /// What has keyboard focus in the window shown. Windows may take activation from that window,
    /// which is never the foreground one, and WPF then drops keyboard focus but remembers where it
    /// was: this puts it back there first, as activating the window would.
    /// </summary>
    public static IInputElement? Focused()
    {
        if (Keyboard.FocusedElement is null)
        {
            var window = Application.Current.Windows.OfType<Window>().Single();
            Keyboard.Focus(FocusManager.GetFocusedElement(window) ?? window);
        }
        return Keyboard.FocusedElement;
    }

    /// <summary>
    /// Presses and releases a key where focus is, through WPF's input as a typed key goes, so WPF's
    /// own handling of it runs too: Tab moves focus, Space presses a button.
    /// </summary>
    public static void Press(Key key)
    {
        var focused = Focused() ?? throw new InvalidOperationException($"Nothing has keyboard focus to take {key}.");
        var source = PresentationSource.FromVisual((Visual)focused);
        foreach (var stroke in new[] { Keyboard.PreviewKeyDownEvent, Keyboard.PreviewKeyUpEvent })
        {
            InputManager.Current.ProcessInput(new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, key) { RoutedEvent = stroke });
        }
        Settle();
    }

    /// <summary>
    /// Holds a key down for the App's thread alone until disposed, as Windows reports it to WPF, such
    /// as Shift for Shift+Tab. The thread's own key state changes; the user's keyboard doesn't.
    /// </summary>
    public static IDisposable Hold(Key key)
    {
        var before = new byte[256];
        GetKeyboardState(before);
        var held = (byte[])before.Clone();
        held[KeyInterop.VirtualKeyFromKey(key)] |= 0x80;
        SetKeyboardState(held);
        return new Released(before);
    }

    /// <summary>Types text where focus is, as a keyboard's characters arrive.</summary>
    public static void Type(string text)
    {
        var focused = Focused() ?? throw new InvalidOperationException($"Nothing has keyboard focus to take “{text}”.");
        TextCompositionManager.StartComposition(new TextComposition(InputManager.Current, focused, text));
        Settle();
    }

    /// <summary>Lets the App's thread finish what is queued, such as moving focus off a control turned off.</summary>
    public static void Settle() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);

    public void Dispose()
    {
        _dispatcher.Invoke(() => Application.Current.Shutdown());
        _dispatcher.InvokeShutdown();
        _thread.Join();
    }

    /// <summary>
    /// Lays a view out as Gengfu's window does at a display scale: its size and font, rounded to
    /// whole device pixels. A detached root's scale reaches only what is already under it, so the
    /// view is laid out once to build its tree, then scaled and laid out again.
    /// </summary>
    public static void Lay(Control view, double scale)
    {
        view.FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI");
        view.FontSize = 14;
        view.UseLayoutRounding = true;
        Arrange(view);
        VisualTreeHelper.SetRootDpi(view, new DpiScale(scale, scale));
        Invalidate(view);
        Arrange(view);
    }

    /// <summary>
    /// In device pixels at the scale laid out, below being positive: how far an icon's centre sits
    /// from the centre of what holds it, and how far its label's capitals sit from the icon's centre.
    /// </summary>
    public static (double Icon, double Capitals) Offsets(FrameworkElement holder, FrameworkElement icon, TextBlock label, double scale)
    {
        var centre = icon.TransformToAncestor(holder).Transform(new Point(0, icon.ActualHeight / 2)).Y;
        return ((centre - (holder.ActualHeight / 2)) * scale, Capitals(icon, label, scale));
    }

    /// <summary>
    /// In device pixels at the scale laid out, below being positive: how far the capitals of a text's
    /// first line sit from a mark's centre. The capitals, not the line, so a descender doesn't count.
    /// The first line's baseline is the font's own: a taller line height adds its room under the
    /// baseline, and wrapping only adds lines below.
    /// </summary>
    public static double Capitals(FrameworkElement mark, TextBlock text, double scale)
    {
        Assert.Equal(scale, VisualTreeHelper.GetDpi(text).DpiScaleY);
        var typeface = new Typeface(text.FontFamily, text.FontStyle, text.FontWeight, text.FontStretch);
        Assert.True(typeface.TryGetGlyphTypeface(out var glyphs));
        var line = new FormattedText(text.Text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, text.FontSize, Brushes.Black, scale);
        var capitals = line.Baseline - (glyphs.CapsHeight * text.FontSize / 2);
        var centre = mark.ActualHeight / 2;
        return (text.TransformToVisual(mark).Transform(new Point(0, capitals)).Y - centre) * scale;
    }

    private static void Arrange(Control view)
    {
        view.Measure(new Size(380, 584));
        view.Arrange(new Rect(0, 0, 380, 584));
        view.UpdateLayout();
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetKeyboardState(byte[] state);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetKeyboardState(byte[] state);

    private static void Invalidate(DependencyObject element)
    {
        if (element is UIElement laid)
        {
            laid.InvalidateMeasure();
            laid.InvalidateArrange();
        }

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
        {
            Invalidate(VisualTreeHelper.GetChild(element, i));
        }
    }

    private sealed class Released(byte[] before) : IDisposable
    {
        public void Dispose() => SetKeyboardState(before);
    }
}

[CollectionDefinition(Name)]
public sealed class WpfAppCollection : ICollectionFixture<WpfApp>
{
    public const string Name = "Gengfu's App";
}
