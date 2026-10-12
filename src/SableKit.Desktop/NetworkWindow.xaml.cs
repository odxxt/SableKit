namespace SableKit.Desktop;

public partial class NetworkWindow : Window
{
    private CancellationTokenSource? cancellation;
    public NetworkWindow(bool inspectClipboard = true)
    {
        InitializeComponent(); SourceInitialized += (_, _) => NativeWindow.Apply(this, true);
        foreach (var (name, port) in new[] { ("SSH · 22", 22), ("HTTPS · 443", 443), ("RDP · 3389", 3389), ("SMB · 445", 445) })
        {
            var b = Ui.Button(name, () => { if (!NetworkTarget.TryParse(Target.Text, out var target, out var error)) { Summary.Text = error; return; } Target.Text = new NetworkTarget(target!.Host, port).Display; Check(this, new RoutedEventArgs()); }); b.FontSize = 11; b.Padding = new Thickness(8, 5, 8, 5); Ports.Children.Add(b);
        }
        if (inspectClipboard) LoadClipboard(false);
        Closed += (_, _) => cancellation?.Cancel();
        PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) cancellation?.Cancel(); if (e.Key == Key.Enter && cancellation is null) Check(this, e); };
    }
    private void LoadClipboard(bool explicitPaste)
    {
        try { var value = AppState.ReadClipboard(); if (explicitPaste || NetworkTarget.TryParse(value, out _, out _)) Target.Text = value.Trim(); }
        catch (System.Runtime.InteropServices.COMException) { Summary.Text = "Clipboard is busy. Try Paste again."; }
    }
    private void PasteTarget(object sender, RoutedEventArgs e) => LoadClipboard(true);
    private async void Check(object sender, RoutedEventArgs e) => await Run(false);
    private async void Dns(object sender, RoutedEventArgs e) => await Run(true);
    public async Task Run(bool dns)
    {
        if (cancellation is not null) return;
        if (!NetworkTarget.TryParse(Target.Text, out var target, out var error)) { Summary.Text = error; return; }
        using var run = new CancellationTokenSource(); cancellation = run;
        CheckButton.IsEnabled = DnsButton.IsEnabled = Ports.IsEnabled = false; StopButton.Visibility = Visibility.Visible; Summary.Text = "Checking…"; Elapsed.Text = "";
        try
        {
            var result = dns ? await NetworkChecks.DnsAsync(target!.Host, run.Token) : await NetworkChecks.RunAsync(target!, run.Token);
            Summary.Text = result.Summary; Summary.SetResourceReference(TextBlock.ForegroundProperty, result.Success ? "Good" : "Bad"); Results.Text = result.Detail; Elapsed.Text = $"{result.Milliseconds} ms"; CopyButton.IsEnabled = true;
        }
        catch (OperationCanceledException) { Summary.Text = "Cancelled"; }
        finally { cancellation = null; CheckButton.IsEnabled = DnsButton.IsEnabled = Ports.IsEnabled = true; StopButton.Visibility = Visibility.Collapsed; }
    }
    private void Stop(object sender, RoutedEventArgs e) => cancellation?.Cancel();
    private async void CopyResult(object sender, RoutedEventArgs e) { try { await AppState.CopyAsync($"{Summary.Text}\n{Results.Text}\n{Elapsed.Text}"); Summary.Text = "Result copied"; } catch (System.Runtime.InteropServices.COMException) { Summary.Text = "Clipboard is busy. Try again."; } }
}
