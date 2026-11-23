using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Input;

namespace Gengfu.App.Tests;

[Collection(WpfAppCollection.Name)]
public sealed class WpfAppTests(WpfApp wpf)
{
    private const int NameOfObject = 2;

    // The PC's real keys and mouse buttons reach only the desktop taking input, which is the one a
    // test run starts on whenever someone is at the PC: on a desktop of its own the App never meets them.
    [Fact]
    public void The_App_s_thread_is_on_a_desktop_of_its_own_not_the_one_the_test_run_started_on()
    {
        var started = DesktopOfThisThread();
        var app = wpf.Invoke(DesktopOfThisThread);

        Assert.NotEqual(started, app);
    }

    // On its own desktop the window keeps keyboard focus, so a focus lost in a view test is a real
    // fault in the test: it fails where it happens, not quietly put back.
    [Fact]
    public void A_key_pressed_or_text_typed_once_keyboard_focus_is_lost_fails_the_test()
    {
        var lost = wpf.Invoke(() =>
        {
            var box = new TextBox();
            var window = WpfApp.Show(box);
            try
            {
                box.Focus();
                Keyboard.ClearFocus();
                return (Pressed: Record.Exception(() => WpfApp.Press(Key.Tab)), Typed: Record.Exception(() => WpfApp.Type("x")));
            }
            finally
            {
                window.Close();
            }
        });

        Assert.StartsWith("Nothing has keyboard focus", Assert.IsType<InvalidOperationException>(lost.Pressed).Message);
        Assert.StartsWith("Nothing has keyboard focus", Assert.IsType<InvalidOperationException>(lost.Typed).Message);
    }

    private static string DesktopOfThisThread()
    {
        var desktop = GetThreadDesktop(GetCurrentThreadId());
        var name = new char[256];
        if (!GetUserObjectInformation(desktop, NameOfObject, name, name.Length * sizeof(char), out _))
        {
            throw new Win32Exception();
        }
        return new string(name).TrimEnd('\0');
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetThreadDesktop(uint thread);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetUserObjectInformation(IntPtr handle, int index, [Out] char[] info, int length, out int needed);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();
}
