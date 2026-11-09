using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Interop;
using Gengfu.App.Speech;
using Gengfu.Core;
using Velopack;

namespace Gengfu.App;

internal static class Program
{
    /// <summary>
    /// Velopack goes first: when Setup or an update runs Gengfu to finish its work, it does so and
    /// exits here, and an update downloaded earlier is applied before the window opens. Only the
    /// first Gengfu on the user's own folder applies one, since Velopack's updater closes every copy
    /// it finds running; a second start on the same folder brings the first one's window forward.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        var home = Home.Of(Environment.GetEnvironmentVariable(Home.Variable));
        using var single = new SingleInstance(home.Lock);
        VelopackApp.Build().SetAutoApplyOnStartup(AppliesUpdate(home, single.IsFirst)).Run();
        if (!single.IsFirst)
        {
            single.ShowRunning();
            return;
        }
        var app = new App();
        app.InitializeComponent();
        var session = new Session(CultureInfo.CurrentUICulture, new SystemTimeSource(), new CallAudio(), home.Folder, Install(home, Updates.Installed));
        var window = new MainWindow(session);
        single.Mark(new WindowInteropHelper(window).EnsureHandle());
        app.Run(window);
    }

    /// <summary>Whether this Gengfu applies an update downloaded earlier as it starts.</summary>
    internal static bool AppliesUpdate(Home home, bool first) => first && !home.Isolated;

    /// <summary>Velopack's manager for the installed copy; null for a copy that leaves the install alone.</summary>
    internal static UpdateManager? Install(Home home, Func<UpdateManager?> installed) => home.Isolated ? null : installed();
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

    /// <summary>
    /// One Gengfu per folder in a Windows session. The user's own folder takes Local\Gengfu, the name
    /// every version holds, so copies of any version on it exclude each other.
    /// </summary>
    public string Lock => Isolated
        ? @"Local\Gengfu-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Folder.ToUpperInvariant())))[..16]
        : @"Local\Gengfu";

    public static Home Of(string? variable) =>
        new(string.IsNullOrWhiteSpace(variable) ? Own : Path.TrimEndingDirectorySeparator(Path.GetFullPath(variable)));
}
