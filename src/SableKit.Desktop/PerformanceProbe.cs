using System.Diagnostics;
using System.Text.Json;
using System.Windows.Threading;

namespace SableKit.Desktop;

internal static class PerformanceProbe
{
    public static async Task Run(TrayWindow tray, string output)
    {
        Directory.CreateDirectory(output);
        using var process = Process.GetCurrentProcess();
        await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        var startup = (DateTime.Now - process.StartTime).TotalMilliseconds;
        await Task.Delay(5000);
        var visible = await Sample(process);
        tray.Hide(); await Task.Delay(5000);
        var hidden = await Sample(process);
        var latency = new List<double>(); var stages = new List<Dictionary<string,double>>();
        for (var i = 0; i < 5; i++)
        {
            tray.OpenTimings = new(); var timer = Stopwatch.StartNew(); tray.Open();
            await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            latency.Add(timer.Elapsed.TotalMilliseconds); stages.Add(tray.OpenTimings); tray.OpenTimings=null; tray.Hide();
        }
        File.WriteAllText(Path.Combine(output, "performance.json"), JsonSerializer.Serialize(new { StartupToUiIdleMilliseconds = startup, WarmOpenToUiIdleMilliseconds = latency, WarmOpenStageMilliseconds = stages, Visible = visible, Hidden = hidden, RenderTier = RenderCapability.Tier >> 16, NativeBackdropApiAccepted = NativeWindow.BackdropAccepted, RemoteSession = SystemParameters.IsRemoteSession, OpaqueRequested = App.DisableBackdrop, OS = Environment.OSVersion.ToString(), Runtime = Environment.Version.ToString(), Note = "One local Release run, five-second CPU samples; UI-idle timings are not display-present timestamps. No main or tool window created." }, new JsonSerializerOptions { WriteIndented = true }));
    }
    private static async Task<object> Sample(Process process)
    {
        process.Refresh(); var start = process.TotalProcessorTime; var timer = Stopwatch.StartNew();
        await Task.Delay(5000); process.Refresh();
        return new { CpuCorePercent = (process.TotalProcessorTime - start).TotalMilliseconds / timer.Elapsed.TotalMilliseconds * 100, WorkingSetMiB = process.WorkingSet64 / 1048576.0, PrivateMiB = process.PrivateMemorySize64 / 1048576.0 };
    }
}
