using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SpotWhy.Services;

public class HotkeyService
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int HOTKEY_ID = 9000;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_NOREPEAT = 0x4000;
    private const uint VK_SPACE = 0x20;
    private const int WM_HOTKEY = 0x0312;

    private readonly Window _window;
    private HwndSource? _source;

    public event Action? HotkeyPressed;

    public HotkeyService(Window window)
    {
        _window = window;
    }

    public void Register()
    {
        var helper = new WindowInteropHelper(_window);
        _source = HwndSource.FromHwnd(helper.Handle);
        _source?.AddHook(WndProc);

        RegisterHotKey(helper.Handle, HOTKEY_ID, MOD_CONTROL | MOD_NOREPEAT, VK_SPACE);
    }

    public void Unregister()
    {
        var helper = new WindowInteropHelper(_window);
        UnregisterHotKey(helper.Handle, HOTKEY_ID);
        _source?.RemoveHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && (int)wParam == HOTKEY_ID)
        {
            HotkeyPressed?.Invoke();
            handled = true;
        }
        return IntPtr.Zero;
    }
}
