using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace SableKit.Desktop;

internal static class NativeWindow
{
    internal static bool BackdropAccepted { get; private set; }
    [StructLayout(LayoutKind.Sequential)] private struct Margins { public int Left, Right, Top, Bottom; }
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
    [DllImport("dwmapi.dll")] private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins margins);
    [DllImport("user32.dll", SetLastError = true)] internal static extern bool RegisterHotKey(IntPtr handle, int id, uint modifiers, uint key);
    [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr handle, int id);

    public static void Apply(Window window, bool compact)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)) return;
        var handle = new WindowInteropHelper(window).Handle;
        var dark = ThemeManager.IsDark ? 1 : 0; DwmSetWindowAttribute(handle, 20, ref dark, 4);
        var corners = 2; DwmSetWindowAttribute(handle, 33, ref corners, 4);
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621) && !SystemParameters.HighContrast && !App.DisableBackdrop)
        {
            var backdrop = compact ? 3 : 2;
            if (DwmSetWindowAttribute(handle, 38, ref backdrop, 4) == 0)
            {
                BackdropAccepted = true;
                var margins = new Margins { Left = -1, Right = -1, Top = -1, Bottom = -1 };
                DwmExtendFrameIntoClientArea(handle, ref margins);
                if (PresentationSource.FromVisual(window) is HwndSource source) source.CompositionTarget.BackgroundColor = Colors.Transparent;
                window.Background = Brushes.Transparent;
            }
        }
    }
}
