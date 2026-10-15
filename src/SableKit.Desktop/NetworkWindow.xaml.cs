namespace SableKit.Desktop;

public partial class NetworkWindow : Window
{
    private CancellationTokenSource? cancellation;
    private bool loading, pinned;
    private int clipboardRequest;
    private readonly PanelPlacement placement;
    public NetworkWindow(bool inspectClipboard = true)
    {
        InitializeComponent();
        placement = new PanelPlacement(this, Path.Combine(((App)Application.Current).State.DataDirectory, "sablenet-window.json"));
        SourceInitialized += (_, _) => { NativeWindow.Apply(this, true); placement.Restore(); };
        foreach (var (name, port) in new[] { ("SSH",22), ("HTTP",80), ("HTTPS",443), ("RDP",3389), ("SMB",445) })
        {
            var b = Ui.Button(name, () => Port.Text = port.ToString(), ""); b.FontSize=11; b.Tag=port;b.Margin=new Thickness(3,0,3,0);b.Padding=new Thickness(5,3,5,3); b.MinHeight=28; b.ToolTip=$"Use TCP port {port}"; Ports.Children.Add(b);
        }
        Results.TextChanged+=(_,_)=>EmptyOutput.Visibility=Results.Text.Length==0?Visibility.Visible:Visibility.Collapsed;
        UpdateTargetControls();Closed += (_, _) => cancellation?.Cancel();
        PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) { if(cancellation is not null) cancellation.Cancel(); else Close(); e.Handled=true; } };
        Deactivated += (_, _) => { if (!pinned && cancellation is null && !Candidates.IsDropDownOpen && !VerificationMode) Hide(); };
        if (inspectClipboard) LoadClipboard();
    }
    internal bool VerificationMode { get; set; }
    internal async void LoadClipboard()
    {
        var request=++clipboardRequest;
        try { var text=await AppState.ReadClipboardAsync(); if(request==clipboardRequest && cancellation is null) SetCandidates(ClipboardTargets.Extract(text)); }
        catch (System.Runtime.InteropServices.COMException) { Summary.Text="Clipboard is busy. Try Paste again."; }
    }
    internal void SetCandidates(IReadOnlyList<DetectedTarget> values)
    {
        clipboardRequest++;
        Results.Clear(); CopyButton.IsEnabled=false; Elapsed.Text="";
        Candidates.ItemsSource=values;
        Candidates.Visibility=values.Count>1 ? Visibility.Visible : Visibility.Collapsed;
        if(values.Count==1) SetTarget(values[0]);
        else if(values.Count>1) { loading=true; Target.Text=""; Port.Text=""; loading=false; Candidates.SelectedIndex=-1; Summary.Text="Choose a detected address"; }
        else Summary.Text="Copy an address or enter one below";
        UpdateTargetControls();
    }
    internal void SetTarget(DetectedTarget value, string? output = null, bool preserveChoices = false)
    {
        clipboardRequest++;
        if(!preserveChoices) { Candidates.ItemsSource=null; Candidates.Visibility=Visibility.Collapsed; }
        loading=true; Target.Text=value.Target.Host; Port.Text=value.PortText; loading=false;
        Results.Text=output ?? ""; CopyButton.IsEnabled=!string.IsNullOrEmpty(output);
        Summary.Text=value.Error ?? (output is null ? "Ready" : "Quick-action output"); Elapsed.Text="";UpdateTargetControls();
    }
    private void ChooseCandidate(object sender, SelectionChangedEventArgs e) { if(Candidates.SelectedItem is DetectedTarget value) SetTarget(value, preserveChoices:true); }
    private void InputChanged(object sender, TextChangedEventArgs e) { if(loading || Results is null || cancellation is not null) return; clipboardRequest++; Results.Clear(); CopyButton.IsEnabled=false; Summary.Text="Ready"; Elapsed.Text="";UpdateTargetControls(); }
    private void UpdateTargetControls()
    {
        if(CheckButton is null || Port is null)return;
        CheckButton.Content=string.IsNullOrWhiteSpace(Port.Text)?"Ping address":"Test TCP connection";
        foreach(Button button in Ports.Children)button.SetResourceReference(BackgroundProperty,button.Tag?.ToString()==Port.Text.Trim()?"Hover":"Surface");
    }
    private void PasteTarget(object sender, RoutedEventArgs e) => LoadClipboard();
    private async void Check(object sender, RoutedEventArgs e) => await Run(false);
    private async void Dns(object sender, RoutedEventArgs e) => await Run(true);
    public async Task Run(bool dns)
    {
        if(cancellation is not null) return;
        var input=Target.Text.Trim();
        // Address accepts a pasted explicit endpoint; a separately entered port takes precedence.
        if(!NetworkTarget.TryParse(input,out var host,out var error)) { Summary.Text=error; return; }
        var full=string.IsNullOrWhiteSpace(Port.Text) ? host!.Display : new NetworkTarget(host!.Host, null).Display;
        if(!string.IsNullOrWhiteSpace(Port.Text)) full=host.Host.Contains(':') ? $"[{host.Host}]:{Port.Text.Trim()}" : $"{host.Host}:{Port.Text.Trim()}";
        if(!NetworkTarget.TryParse(full,out var target,out error)) { Summary.Text=error; return; }
        loading=true; Target.Text=target!.Host; Port.Text=target.Port?.ToString() ?? ""; loading=false;
        using var run=new CancellationTokenSource(); cancellation=run;
        CheckButton.Visibility=Visibility.Collapsed; StopButton.Visibility=Visibility.Visible;
        Target.IsEnabled=Port.IsEnabled=Ports.IsEnabled=DnsButton.IsEnabled=PasteButton.IsEnabled=Candidates.IsEnabled=false;
        Summary.Text=dns ? "nslookup" : target.Port is null ? "ping" : "Test-NetConnection"; Results.Clear(); Elapsed.Text="";
        try
        {
            var result=await NetworkCommand.Run(target,run.Token,new Progress<string>(text => { Results.Text=text; Results.ScrollToEnd(); CopyButton.IsEnabled=text.Length>0; }),dns);
            Results.Text=result.Detail; Summary.Text=result.Summary; Elapsed.Text=$"{result.Milliseconds/1000.0:0.0}s"; CopyButton.IsEnabled=Results.Text.Length>0;
        }
        catch(OperationCanceledException) { Summary.Text="Stopped"; }
        catch(Exception ex) when(ex is IOException or System.ComponentModel.Win32Exception or InvalidOperationException) { Summary.Text="Unable to run check"; Results.Text=ex.Message; }
        finally { cancellation=null;UpdateTargetControls(); CheckButton.Visibility=Visibility.Visible; StopButton.Visibility=Visibility.Collapsed; Target.IsEnabled=Port.IsEnabled=Ports.IsEnabled=DnsButton.IsEnabled=PasteButton.IsEnabled=Candidates.IsEnabled=true; }
    }
    private void Stop(object sender,RoutedEventArgs e)=>cancellation?.Cancel();
    private async void CopyResult(object sender,RoutedEventArgs e) { try { await AppState.CopyAsync(Results.Text); } catch(System.Runtime.InteropServices.COMException) { Summary.Text="Clipboard is busy"; } }
    private void TogglePin(object sender,RoutedEventArgs e) { pinned=!pinned; Topmost=pinned; if(pinned) PinButton.SetResourceReference(BackgroundProperty,"Hover"); else PinButton.Background=Brushes.Transparent; PinButton.ToolTip=pinned?"Unpin":"Pin · keep open"; }
    private void CloseTool(object sender,RoutedEventArgs e)=>Close();
    private void DragHeader(object sender,MouseButtonEventArgs e)
    {
        if(TrayWindow.FindParent<Button>(e.OriginalSource as DependencyObject) is not null) return;
        try { if(e.ClickCount==2 && TrayWindow.FindParent<TextBlock>(e.OriginalSource as DependencyObject)==BrandTitle) placement.Reset(); else { DragMove(); placement.Save(); } }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or InvalidOperationException) { Summary.Text="Position could not be saved"; }
    }
}
