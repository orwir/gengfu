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
    private readonly Dispatcher _dispatcher;

    public WpfApp()
    {
        using var ready = new ManualResetEventSlim();
        Dispatcher? dispatcher = null;
        var thread = new Thread(() =>
        {
            new App().InitializeComponent();
            dispatcher = Dispatcher.CurrentDispatcher;
            ready.Set();
            Dispatcher.Run();
        })
        { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        ready.Wait();
        _dispatcher = dispatcher!;
    }

    /// <summary>Runs on the App's thread, where views can be made and laid out.</summary>
    public T Invoke<T>(Func<T> work) => _dispatcher.Invoke(work);

    public void Dispose()
    {
        _dispatcher.Invoke(() => Application.Current.Shutdown());
        _dispatcher.InvokeShutdown();
    }

    /// <summary>Lays a view out at the window's size and in the window's font, as Gengfu shows it.</summary>
    public static void Lay(Control view)
    {
        view.FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI");
        view.FontSize = 14;
        view.Measure(new Size(380, 584));
        view.Arrange(new Rect(0, 0, 380, 584));
        view.UpdateLayout();
    }

    /// <summary>
    /// How far an icon's centre and its label's capitals sit from the centre of what holds them,
    /// below being positive. The capitals, not the line, so a descender doesn't count.
    /// </summary>
    public static (double Icon, double Label) Offsets(FrameworkElement holder, FrameworkElement icon, TextBlock label)
    {
        var typeface = new Typeface(label.FontFamily, label.FontStyle, label.FontWeight, label.FontStretch);
        Assert.True(typeface.TryGetGlyphTypeface(out var glyphs));
        var line = new FormattedText(label.Text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, label.FontSize, Brushes.Black, 1.0);
        var capitals = line.Baseline - (glyphs.CapsHeight * label.FontSize / 2);
        var middle = holder.ActualHeight / 2;
        return (
            icon.TransformToAncestor(holder).Transform(new Point(0, icon.ActualHeight / 2)).Y - middle,
            label.TransformToAncestor(holder).Transform(new Point(0, capitals)).Y - middle);
    }
}

[CollectionDefinition(Name)]
public sealed class WpfAppCollection : ICollectionFixture<WpfApp>
{
    public const string Name = "Gengfu's App";
}
