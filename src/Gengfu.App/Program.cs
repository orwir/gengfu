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
        using var single = new SingleInstance(@"Local\Gengfu");
        VelopackApp.Build().SetAutoApplyOnStartup(single.IsFirst).Run();
        if (!single.IsFirst)
        {
            SingleInstance.ShowRunning();
            return;
        }
        App.Main();
    }
}
