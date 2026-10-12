using Microsoft.Win32;

namespace SableKit.Desktop;

internal static class ThemeManager
{
    private static bool initialized;
    private static bool previousHighContrast;
    public static bool IsDark { get; private set; } = true;
    public static void Apply(string preference)
    {
        var oldDark = IsDark;
        IsDark = preference != "Light";
        if (preference == "System")
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            IsDark = key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        if (initialized && oldDark == IsDark && previousHighContrast == SystemParameters.HighContrast) return;
        initialized = true; previousHighContrast = SystemParameters.HighContrast;
        var values = IsDark
            ? new[] { "#18181B", "#EE18181B", "#232326", "#333337", "#434348", "#F4F4F5", "#ADADB6", "#F4F4F5", "#19191C", "#A1D5B0", "#FFB1AB" }
            : new[] { "#F6F6F7", "#F2F6F6F7", "#EBEBEF", "#DFDFE5", "#C7C7CE", "#202024", "#62626B", "#242429", "#FFFFFF", "#25643D", "#A02826" };
        string[] keys = ["Surface", "PanelSurface", "Inset", "Hover", "Border", "Text", "Muted", "Accent", "AccentText", "Good", "Bad"];
        for (var i = 0; i < keys.Length; i++) Application.Current.Resources[keys[i]] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(values[i]));
        if (SystemParameters.HighContrast)
        {
            foreach (var key in new[] { "Surface", "PanelSurface", "Inset" }) Application.Current.Resources[key] = SystemColors.WindowBrush;
            foreach (var key in new[] { "Text", "Muted", "Border", "Accent", "Good", "Bad" }) Application.Current.Resources[key] = SystemColors.WindowTextBrush;
            Application.Current.Resources["AccentText"] = SystemColors.WindowBrush;
            Application.Current.Resources["Hover"] = SystemColors.HighlightBrush;
        }
    }
}
