using System.Text.Json;

namespace SableKit.Desktop;

// Position persistence and title double-click reset follow SableLatch; see SABLELATCH-NOTICE.txt.
internal sealed class PanelPlacement(Window window, string path)
{
    internal void Restore()
    {
        var area = SystemParameters.WorkArea;
        double? left = null, top = null;
        try
        {
            if (File.Exists(path) && JsonSerializer.Deserialize<double[]>(File.ReadAllText(path)) is { Length: 2 } p && p.All(double.IsFinite))
            { left = p[0]; top = p[1]; }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { }
        window.Left = Math.Clamp(left ?? area.Right - window.Width - 18, area.Left, Math.Max(area.Left, area.Right - window.Width));
        window.Top = Math.Clamp(top ?? area.Bottom - window.Height - 18, area.Top, Math.Max(area.Top, area.Bottom - window.Height));
    }
    internal void Save() { Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, JsonSerializer.Serialize(new[] { window.Left, window.Top })); }
    internal void Reset() { var area = SystemParameters.WorkArea; window.Left = Math.Max(area.Left, area.Right - window.Width - 18); window.Top = Math.Max(area.Top, area.Bottom - window.Height - 18); Save(); }
}
