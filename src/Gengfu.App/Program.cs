using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Gengfu.App.Speech;
using Gengfu.Core;
using Velopack;

namespace Gengfu.App;

internal static class Program
{
    /// <summary>
    /// Velopack goes first: when Setup or an update runs Gengfu to finish its work, it does so and
    /// exits here, and an update downloaded earlier is applied before the window opens. Only the
    /// first Gengfu applies one, since Velopack's updater closes every copy it finds running; a
    /// second start brings the first one's window forward instead.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        var home = Home.Of(Environment.GetEnvironmentVariable(Home.Variable));
        using var single = new SingleInstance(home.Lock);
        VelopackApp.Build().SetAutoApplyOnStartup(single.IsFirst && !home.Isolated).Run();
        if (!single.IsFirst)
        {
            SingleInstance.ShowRunning();
            return;
        }
        var app = new App();
        app.InitializeComponent();
        var session = new Session(CultureInfo.CurrentUICulture, new SystemTimeSource(), new CallAudio(), home.Folder, Install(home));
        app.Run(new MainWindow(session));
    }

    /// <summary>Velopack's manager for the installed copy; null for a copy that leaves the install alone.</summary>
    internal static UpdateManager? Install(Home home) => home.Isolated ? null : Updates.Installed();
}

/// <summary>
/// Where Gengfu keeps its settings and secrets: %AppData%\Gengfu, or the folder GENGFU_HOME names.
/// A copy on any other folder, as tests and checks run it, holds a lock of its own and never
/// updates, so it can't stand in for the user's Gengfu, close it or change its install.
/// </summary>
internal sealed record Home(string Folder)
{
    public const string Variable = "GENGFU_HOME";

    private static string Own => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Gengfu");

    public bool Isolated => !string.Equals(Folder, Own, StringComparison.OrdinalIgnoreCase);

    /// <summary>One Gengfu per folder in a Windows session; the user's own keeps the name it always had.</summary>
    public string Lock => Isolated
        ? @"Local\Gengfu-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Folder.ToUpperInvariant())))[..16]
        : @"Local\Gengfu";

    public static Home Of(string? variable) =>
        new(string.IsNullOrWhiteSpace(variable) ? Own : Path.TrimEndingDirectorySeparator(Path.GetFullPath(variable)));
}
