using System.Globalization;
using System.Windows;
using System.Windows.Controls;
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
            new App().InitializeComponent();
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
    /// from the centre of what holds it, and how far its label's capitals sit from the icon's
    /// centre. The capitals, not the line, so a descender doesn't count.
    /// </summary>
    public static (double Icon, double Capitals) Offsets(FrameworkElement holder, FrameworkElement icon, TextBlock label, double scale)
    {
        Assert.Equal(scale, VisualTreeHelper.GetDpi(label).DpiScaleY);
        var typeface = new Typeface(label.FontFamily, label.FontStyle, label.FontWeight, label.FontStretch);
        Assert.True(typeface.TryGetGlyphTypeface(out var glyphs));
        var line = new FormattedText(label.Text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, label.FontSize, Brushes.Black, scale);
        var capitals = label.TransformToAncestor(holder).Transform(new Point(0, line.Baseline - (glyphs.CapsHeight * label.FontSize / 2))).Y;
        var centre = icon.TransformToAncestor(holder).Transform(new Point(0, icon.ActualHeight / 2)).Y;
        return ((centre - (holder.ActualHeight / 2)) * scale, (capitals - centre) * scale);
    }

    private static void Arrange(Control view)
    {
        view.Measure(new Size(380, 584));
        view.Arrange(new Rect(0, 0, 380, 584));
        view.UpdateLayout();
    }

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
}

[CollectionDefinition(Name)]
public sealed class WpfAppCollection : ICollectionFixture<WpfApp>
{
    public const string Name = "Gengfu's App";
}
