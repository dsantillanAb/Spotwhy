using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SpotWhy.Services;

public static class AcrylicService
{
    [DllImport("user32.dll")]
    private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    public static void Apply(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;

        var accent = new AccentPolicy
        {
            AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLUR,
            GradientColor = unchecked((int)0x58080808)
        };

        var data = new WindowCompositionAttributeData
        {
            Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
            Size = Marshal.SizeOf(accent),
            Data = Marshal.AllocHGlobal(Marshal.SizeOf(accent))
        };

        Marshal.StructureToPtr(accent, data.Data, false);
        SetWindowCompositionAttribute(hwnd, ref data);
        Marshal.FreeHGlobal(data.Data);
    }

    private enum AccentState
    {
        ACCENT_DISABLED = 0,
        ACCENT_ENABLE_GRADIENT = 1,
        ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
        ACCENT_ENABLE_BLURBEHIND = 3,
        ACCENT_ENABLE_ACRYLICBLUR = 4
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AccentPolicy
    {
        public AccentState AccentState;
        public int AccentFlags;
        public int GradientColor;
        public int AnimationId;
    }

    private enum WindowCompositionAttribute
    {
        WCA_ACCENT_POLICY = 19
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowCompositionAttributeData
    {
        public WindowCompositionAttribute Attribute;
        public int Size;
        public IntPtr Data;
    }
}
