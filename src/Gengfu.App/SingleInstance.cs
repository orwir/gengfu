using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Gengfu.App;

/// <summary>
/// One Gengfu per Windows session, held by a named mutex. A second would be a second bot on the
/// server, and with an update downloaded, Velopack's updater would close the first to apply it.
/// </summary>
internal sealed partial class SingleInstance : IDisposable
{
    private const int Restore = 9;

    private readonly Mutex _mutex;

    public SingleInstance(string name)
    {
        _mutex = new Mutex(initiallyOwned: true, name, out var first);
        IsFirst = first;
    }

    /// <summary>No other Gengfu holds the name: this one runs, and a later one gives way to it.</summary>
    public bool IsFirst { get; }

    /// <summary>Brings the running Gengfu's window forward, restored if it was minimized.</summary>
    public static void ShowRunning()
    {
        using var self = Process.GetCurrentProcess();
        foreach (var other in Process.GetProcessesByName(self.ProcessName))
        {
            using (other)
            {
                if (other.Id != self.Id && other.MainWindowHandle != IntPtr.Zero)
                {
                    _ = ShowWindow(other.MainWindowHandle, Restore);
                    _ = SetForegroundWindow(other.MainWindowHandle);
                }
            }
        }
    }

    public void Dispose()
    {
        if (IsFirst)
        {
            _mutex.ReleaseMutex();
        }
        _mutex.Dispose();
    }

    [LibraryImport("user32.dll")]
    private static partial int ShowWindow(IntPtr window, int command);

    [LibraryImport("user32.dll")]
    private static partial int SetForegroundWindow(IntPtr window);
}
