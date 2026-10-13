using System.Diagnostics;
using System.Text;

namespace SableKit.Desktop;

internal static class NetworkCommand
{
    internal static async Task<CheckResult> Run(NetworkTarget target, CancellationToken token, IProgress<string>? output = null, bool dns = false)
    {
        if (!NetworkTarget.TryParse(target.Display, out _, out var error)) throw new InvalidOperationException(error);
        token.ThrowIfCancellationRequested();
        var clock = Stopwatch.StartNew();
        var system = Environment.GetFolderPath(Environment.SpecialFolder.System);
        var info = new ProcessStartInfo { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        if (dns || target.Port is null)
        {
            info.FileName = Path.Combine(system, dns ? "nslookup.exe" : "ping.exe");
            if (!dns) foreach (var arg in new[] { "-n", "4", "-w", "1000" }) info.ArgumentList.Add(arg);
            info.ArgumentList.Add(target.Host);
        }
        else
        {
            info.FileName = Path.Combine(system, "WindowsPowerShell", "v1.0", "powershell.exe");
            // Fixed script; validated user values cross the process boundary only as environment data.
            const string script = "$ProgressPreference='SilentlyContinue'; $ErrorActionPreference='Stop'; [Console]::OutputEncoding=[Text.Encoding]::UTF8; try { $r=Test-NetConnection -ComputerName $env:SABLEKIT_HOST -Port ([int]$env:SABLEKIT_PORT) -WarningAction Continue; $r | Out-String -Width 120 | Write-Output; if(-not $r.TcpTestSucceeded){exit 1} } catch { Write-Output $_; exit 1 }";
            foreach (var arg in new[] { "-NoLogo", "-NoProfile", "-NonInteractive", "-EncodedCommand", Convert.ToBase64String(Encoding.Unicode.GetBytes(script)) }) info.ArgumentList.Add(arg);
            info.Environment["SABLEKIT_HOST"] = target.Host;
            info.Environment["SABLEKIT_PORT"] = target.Port.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            info.StandardOutputEncoding = Encoding.UTF8;
        }
        using var process = Process.Start(info) ?? throw new IOException("Windows could not start the network command.");
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(token); limit.CancelAfter(TimeSpan.FromSeconds(45));
        using var registration = limit.Token.Register(() => { try { if (!process.HasExited) process.Kill(true); } catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception) { } });
        var text = new StringBuilder(); var gate = new object();
        async Task Read(StreamReader stream)
        {
            while (await stream.ReadLineAsync() is { } line)
            {
                lock (gate) { if (text.Length < 65536) { text.AppendLine(line); output?.Report(text.ToString()); } }
            }
        }
        await Task.WhenAll(Read(process.StandardOutput), Read(process.StandardError), process.WaitForExitAsync());
        token.ThrowIfCancellationRequested();
        if (limit.IsCancellationRequested) return new(false, "Check timed out", text + "\nCommand stopped after 45 seconds.", clock.ElapsedMilliseconds);
        var success = process.ExitCode == 0;
        return new(success, success ? "Check complete" : "Check failed", text.ToString().TrimEnd(), clock.ElapsedMilliseconds);
    }
}
