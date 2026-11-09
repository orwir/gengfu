using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Gengfu.App;

/// <summary>
/// One Gengfu per folder in a Windows session, held by a named mutex. A second on the same folder
/// would be a second bot on the server, and on the user's own folder, Velopack's updater would close
/// the first to apply a downloaded update.
/// </summary>
internal sealed partial class SingleInstance : IDisposable
{
    private const int Restore = 9;

    // A window property naming the lock its Gengfu holds, so a later start finds that Gengfu's
    // window among every process named Gengfu, which may run on other folders.
    private const string Property = "Gengfu.SingleInstance";

    private readonly Mutex _mutex;
    private readonly IntPtr _token;

    public SingleInstance(string name)
    {
        _mutex = new Mutex(initiallyOwned: true, name, out var first);
        IsFirst = first;
        _token = (IntPtr)BitConverter.ToInt64(SHA256.HashData(Encoding.UTF8.GetBytes(name)));
    }

    /// <summary>No other Gengfu holds the name: this one runs, and a later one gives way to it.</summary>
    public bool IsFirst { get; }

    /// <summary>Marks the first Gengfu's window as the one a later start on the same name brings forward.</summary>
    public void Mark(IntPtr window) => _ = SetProp(window, Property, _token);

    /// <summary>Whether the window is the one the Gengfu holding this name marked.</summary>
    public bool IsMarked(IntPtr window) => GetProp(window, Property) == _token;

    /// <summary>Brings the window of the Gengfu holding this name forward, restored if it was minimized.</summary>
    public void ShowRunning()
    {
        using var self = Process.GetCurrentProcess();
        foreach (var other in Process.GetProcessesByName(self.ProcessName))
        {
            using (other)
            {
                var window = other.MainWindowHandle;
                if (other.Id != self.Id && window != IntPtr.Zero && IsMarked(window))
                {
                    _ = ShowWindow(window, Restore);
                    _ = SetForegroundWindow(window);
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

    [LibraryImport("user32.dll", EntryPoint = "SetPropW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int SetProp(IntPtr window, string name, IntPtr data);

    [LibraryImport("user32.dll", EntryPoint = "GetPropW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr GetProp(IntPtr window, string name);
}
