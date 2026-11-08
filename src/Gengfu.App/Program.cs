using Velopack;

namespace Gengfu.App;

internal static class Program
{
    /// <summary>
    /// Velopack goes first: when Setup or an update runs Gengfu to finish its work, it does so and
    /// exits here, and an update downloaded earlier is applied before the window opens.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        VelopackApp.Build().Run();
        App.Main();
    }
}
