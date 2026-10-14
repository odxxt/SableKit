using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SableKit.Desktop;

internal static class Verification
{
    public static async Task Run(App app, string output)
    {
        Directory.CreateDirectory(output);
        if (File.Exists(Path.Combine(output, "failure.txt"))) File.Delete(Path.Combine(output, "failure.txt"));
        var checks = new List<string>();
        void Check(bool condition, string name) { if (!condition) throw new InvalidOperationException(name); checks.Add(name); }
        var originalClipboard = Clipboard.GetDataObject();
        var state = app.State;
        TrayWindow? tray = null; MainWindow? main = null; NetworkWindow? net = null;
        try
        {
            Clipboard.SetText("127.0.0.1:443");
            var watch = Stopwatch.StartNew(); tray = new TrayWindow(state) { VerificationMode = true }; tray.Open(false); await Settle();
            var firstPanelMs = watch.Elapsed.TotalMilliseconds;
            Check(tray.Width == 336 && tray.Height == 404, "Compact dimensions 336x404 DIP");
            Check(tray.Suggestion.Visibility == Visibility.Visible, "Clipboard TCP suggestion detected");
            tray.PinButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Check(tray.Pinned && tray.Topmost, "Pin toggles retained panel state");
            Check(tray.PanelTitle.Text == "SableKit" && tray.PinButton.BorderThickness == new Thickness(0), "Pin uses fill without a label or border");
            Capture(tray, output, "tray-list.png");
            tray.TogglePin(null, null); await Settle(); Capture(tray, output, "tray-unpinned.png");
            Check(!tray.Pinned && !tray.PanelTitle.Text.Contains("Pinned"), "Unpin clears the retained state"); tray.TogglePin(null, null);
            Check(!Descendants<TextBlock>(tray).Any(t=>t.Text is "Open app" or "Passwords"), "Tray omits Open app and the password tool shortcut");
            var dpi = VisualTreeHelper.GetDpi(tray);
            var clippedIcons = Descendants<Icon>(tray).Where(i => i.IsVisible && (Math.Abs(i.RenderSize.Width - i.Width) > 1 / dpi.DpiScaleX || Math.Abs(i.RenderSize.Height - i.Height) > 1 / dpi.DpiScaleY)).Select(i => $"{i.Kind}: {i.Width}x{i.Height} -> {i.RenderSize}").ToArray();
            Check(clippedIcons.Length == 0, "Small icon layout matches requested dimensions within pixel rounding: " + string.Join(", ", clippedIcons));
            tray.SearchButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); tray.SearchBox.Text = "random"; Check(tray.ActionsList.Items.Count == 1, "Search resolves aliases");
            await Settle(); Capture(tray, output, "tray-search.png");
            Check(Equals(tray.SearchButton.Background, tray.FindResource("Hover")), "Search remains visibly active while editing");
            tray.SearchBox.Text = "no-such-action"; Check(tray.EmptyText.Visibility == Visibility.Visible, "Search empty state");
            tray.SearchButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            tray.ToggleView(null, null); await Settle(); Capture(tray, output, "tray-grid.png"); Check(state.Config.ActiveProfile.GridView, "Grid preference persisted");
            var actionsScroll = Descendants<ScrollViewer>(tray).Single(s => s.Content is StackPanel panel && panel.Children.Contains(tray.ActionsList));
            Check(actionsScroll.ScrollableHeight < 1, "Default grid tiles fit without clipping or scrolling");
            tray.ToggleView(null, null);
            tray.CycleProfile(1); Check(state.Config.ActiveProfileId == "lab" && tray.ActionsList.Items.Count == 2, "Profile switch changes pinned references");
            tray.CycleProfile(-1);
            var gate=new ProfileWheelGate();
            Check(gate.Step(-120,1000)==1 && gate.Step(-120,1010)==0 && gate.Step(-120,1200)==0 && gate.Step(-120,1420)==1, "Profile wheel burst holds at one profile during its settling interval");
            var precise=new ProfileWheelGate();
            Check(precise.Step(-30,1000)==0 && precise.Step(-30,1020)==0 && precise.Step(-60,1040)==1, "Profile wheel dead zone accumulates small deltas");
            if (!ClipboardReader.OpenClipboard(new System.Windows.Interop.WindowInteropHelper(tray).Handle)) throw new InvalidOperationException("Could not hold clipboard for contention test");
            var busyClock=Stopwatch.StartNew();
            try
            {
                try { await AppState.ReadClipboardAsync(); throw new InvalidOperationException("Busy clipboard was read"); }
                catch(System.Runtime.InteropServices.COMException) { Check(busyClock.ElapsedMilliseconds<750, "Busy clipboard read returns promptly without blocking the panel"); }
            }
            finally { ClipboardReader.CloseClipboard(); }
            var password = state.Items.First(i => i.Definition.Kind == ActionKind.Password);
            tray.Activate(); ((Button)tray.ActionsList.Items[0]).Focus();
            var copy = state.ExecuteAsync(password); await Task.Delay(80);
            Check(password.Copied && Clipboard.GetText().Length == 12, "Real clipboard write precedes Copied state"); Capture(tray, output, "tray-copied.png");
            Check(Keyboard.FocusedElement is Button { Tag: ActionItem focused } && focused.Definition.Id == password.Definition.Id, "Running and Copied retain action keyboard focus");
            await copy; Check(password.State == ExecutionState.Idle, "Copied restores idle state");
            Check(Keyboard.FocusedElement is Button { Tag: ActionItem restored } && restored.Definition.Id == password.Definition.Id, "Idle restoration retains action keyboard focus");
            Check(tray.Width == 336 && tray.Height == 404, "Feedback preserves panel dimensions");
            using var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var accept = listener.AcceptTcpClientAsync();
            var check = await NetworkChecks.RunAsync(new NetworkTarget("127.0.0.1", port), CancellationToken.None); using var client = await accept;
            Check(check.Success, "Real local TCP connectivity");
            net = new NetworkWindow(false) { VerificationMode = true }; net.Target.Text = $"127.0.0.1:{port}"; net.Show();
            var accept2 = listener.AcceptTcpClientAsync(); await net.Run(false); using var client2 = await accept2.WaitAsync(TimeSpan.FromSeconds(5));
            Check(net.Results.Text.Contains("TcpTestSucceeded") && net.Results.Text.Contains("True"), "Network window displays native Test-NetConnection output");
            Clipboard.SetText("192.0.2.10"); Check(net.Target.Text == "127.0.0.1" && net.Port.Text == port.ToString(), "Later clipboard changes do not replace visible target");
            Capture(net, output, "network.png");
            net.Target.Text = "127.0.0.1:99999"; await net.Run(false); Check(net.Summary.Text.Contains("65535"), "Invalid explicit port blocks execution");
            var rdp = net.Ports.Children.Cast<Button>().Single(b => b.Content?.ToString() == "RDP");
            net.Target.Text = "127.0.0.1"; rdp.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(net.Port.Text == "3389" && net.Results.Text.Length == 0, "Port preset populates without executing"); Capture(net, output, "network-preset.png");
            Clipboard.SetText("https://example.com/path"); net.LoadClipboard(); await Settle();
            Check(net.Target.Text == "example.com" && net.Port.Text.Length == 0, "Clipboard URL fills host without an implicit port");
            net.SetCandidates(ClipboardTargets.Extract("10.0.0.1 and 10.0.0.2 port 443"));
            Check(net.Candidates.Visibility == Visibility.Visible && net.Candidates.SelectedIndex == -1, "Multiple targets require a choice"); Capture(net, output, "network-picker.png");
            net.Candidates.SelectedIndex=1;
            Check(net.Target.Text=="10.0.0.2" && net.Port.Text=="443", "Choosing a detected address fills its associated port");
            var ping = await NetworkCommand.Run(new NetworkTarget("127.0.0.1", null), CancellationToken.None);
            Check(ping.Success && ping.Detail.Contains("127.0.0.1"), "Native ping output is returned");
            using (var stop = new CancellationTokenSource(150))
            {
                try { await NetworkCommand.Run(new NetworkTarget("127.0.0.1", null), stop.Token); throw new InvalidOperationException("Native cancellation ignored"); }
                catch (OperationCanceledException) { Check(true, "Native command cancellation stops execution"); }
            }
            var quick = new ActionItem(new ActionDefinition { Name = "Ping clipboard", Kind = ActionKind.Network });
            await state.ExecuteAsync(quick, "127.0.0.1");
            Check(tray.ResultRegion.Visibility == Visibility.Visible && tray.ResultOutput.Text == state.Result && tray.ActionsHeading.Text == "Pinned actions", "Quick output stays in panel without replacing the heading");
            Capture(tray, output, "tray-result.png");
            net.SetTarget(state.LastNetworkTarget!, state.Result);
            Check(net.Results.Text == state.Result && net.Target.Text == "127.0.0.1", "Opening the tool transfers the same output and target"); Check(net.Candidates.Visibility==Visibility.Collapsed, "Explicit result handoff clears old target choices");
            Capture(net, output, "network-ping.png");
            foreach(var field in new[] { net.Results, tray.ResultOutput })
            {
                field.UpdateLayout(); var scroll=Descendants<ScrollViewer>(field).Single();
                var bar=Descendants<System.Windows.Controls.Primitives.ScrollBar>(field).Single(b=>b.Orientation==Orientation.Horizontal);
                Check(bar.ActualWidth>200 && Math.Abs(bar.ActualHeight-10)<=1, $"Native output horizontal scrollbar spans the output pane ({bar.ActualWidth:0.#}x{bar.ActualHeight:0.#})");
                scroll.ScrollToRightEnd(); await Settle();
                Check(scroll.HorizontalOffset>0, "Long native output scrolls horizontally"); scroll.ScrollToLeftEnd();
            }
            tray.Left = 60; tray.Top = 70; tray.SavePosition(); tray.Hide(); tray.Open();
            Check(tray.Left == 60 && tray.Top == 70, "Reopening preserves moved position");
            tray.ToggleFromTray(); Check(!tray.IsVisible, "Tray click hides a pinned panel");
            tray.ToggleFromTray(); Check(tray.IsVisible && tray.Left == 60, "Next tray click reopens in place");
            tray.TogglePin(null, null); tray.VerificationMode = false; tray.HandleDeactivated(true); tray.ToggleFromTray();
            Check(!tray.IsVisible, "Tray focus-loss does not reopen the panel on the same click");
            tray.VerificationMode = true; tray.Open(); tray.ResetPosition();
            Check(Math.Abs(tray.Left - (SystemParameters.WorkArea.Right - tray.Width - 18)) < 1 && Math.Abs(tray.Top - (SystemParameters.WorkArea.Bottom - tray.Height - 18)) < 1, "Title reset returns to bottom right");
            tray.CycleProfile(1); tray.CycleProfile(-1); Check(tray.ProfileName.Text == "Default", "Profile carousel cycles both directions");
            main = new MainWindow(state); main.Show(); await Settle();
            var workArea = NativeWindow.WorkArea(main);
            Check(workArea.Contains(new Rect(main.Left, main.Top, main.ActualWidth, main.ActualHeight)), "Main window opens inside the monitor work area");
            Check(main.CloseButton.IsVisible && main.MinimizeButton.IsVisible && main.MaximizeButton.IsVisible, "Main window exposes caption controls");
            main.MinimizeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Settle();
            Check(main.WindowState == WindowState.Minimized, "Caption minimize works");
            main.WindowState = WindowState.Normal; main.Activate();
            main.MaximizeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Settle(); Check(main.WindowState == WindowState.Maximized, "Caption maximize works");
            main.MaximizeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Settle(); Check(main.WindowState == WindowState.Normal, "Caption restore works");
            Capture(main, output, "main-actions.png");
            Check(main.Navigation.Orientation==Orientation.Horizontal && main.Navigation.Children.Count==5, "Main navigation uses five horizontal tabs");
            var library=Descendants<WrapPanel>(main).Single(p=>p.Name=="ActionLibrary");
            Check(library.Children.Count==3 && Descendants<ActionEditorWindow>(main).Count()==0, "Actions open as a grid without an embedded editor");
            ((Button)library.Children[0]).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Settle();
            var editor=main.Editors.Single(); editor.DiscardDecision=()=>false;
            var nameField=Descendants<TextBox>(editor).Single(t=>System.Windows.Automation.AutomationProperties.GetName(t)=="Name");
            nameField.Text="Edited draft"; await Settle();
            Check(editor.HasDraft && state.Config.Actions[0].Name=="Generate password", "Separate settings window retains an unsaved draft");
            Check(Descendants<TextBlock>(editor.PreviewButton).Any(t=>t.Text=="Edited draft"), "Button preview updates from the current draft");
            var previousResult=state.Result; editor.PreviewButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(state.Result==previousResult, "Clicking the preview never executes an action");
            var previewPosition=editor.PreviewButton.TranslatePoint(new Point(),editor);
            editor.FormScroll.ScrollToBottom(); await Settle();
            Check(Math.Abs(previewPosition.Y-editor.PreviewButton.TranslatePoint(new Point(),editor).Y)<1 && editor.PreviewButton.IsVisible, "Button preview stays fixed while settings scroll");
            Capture(editor,output,"action-editor-scrolled.png"); editor.FormScroll.ScrollToTop(); await Settle(); Capture(editor,output,"action-editor.png");
            var librarySearch=Descendants<TextBox>(main).Single(t=>System.Windows.Automation.AutomationProperties.GetName(t)=="Search action library");
            librarySearch.Text="ping"; Check(library.Children.Count==1 && editor.HasDraft && nameField.Text=="Edited draft", "Library search preserves a separate action draft"); librarySearch.Text="";
            editor.Close(); Check(editor.IsVisible, "Cancelled discard keeps the action settings window open");
            main.Close(); Check(main.IsVisible && editor.IsVisible, "Closing the main window protects owned action drafts");
            nameField.Text="";
            Descendants<Button>(editor).Single(b=>b.Content is string label && label=="Duplicate").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(editor.StatusText.Text.Contains("name"), "Duplicate reports invalid draft instead of throwing");
            nameField.Text="Generate password";
            Descendants<Button>(editor).Single(b=>b.Content is string label && label=="Duplicate").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(editor.HasDraft && !state.Config.Actions.Any(a=>a.Id==editor.EditingActionId), "Duplicate remains an unsaved protected draft");
            main.Navigate("Preferences");
            Check(main.PageTitle.Text=="Preferences" && editor.HasDraft, "Top tab navigation leaves a separate draft intact");
            editor.DiscardDecision=()=>true; editor.Close();
            main.Navigate("Actions"); main.AddButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Settle();
            var created=main.Editors.Single(); created.DiscardDecision=()=>true;
            Check(created.PreviewButton.IsVisible && created.HasDraft, "New action opens with a sticky preview");
            Capture(created,output,"action-new.png");
            created.FitToWorkArea(new Rect(0,0,853,480)); await Settle();
            Check(created.PreviewButton.IsVisible && created.FormScroll.ViewportHeight>50, "New action preview and fields fit a small work area"); Capture(created,output,"action-editor-small.png");
            created.Close();
            main.Navigate("Preferences"); await Settle();
            var themeChoice=Descendants<ComboBox>(main).Single();
            main.Activate(); themeChoice.Focus(); await Settle();
            var choiceToggle=(System.Windows.Controls.Primitives.ToggleButton)themeChoice.Template.FindName("Toggle",themeChoice);
            Check(themeChoice.IsKeyboardFocusWithin && choiceToggle.Background==Application.Current.FindResource("Hover"), "Closed choices show keyboard focus with a fill change and no outline");
            var defaults = Descendants<TextBox>(main).Single(t => System.Windows.Automation.AutomationProperties.GetName(t) == "Default length");
            defaults.Text = "20"; main.DiscardDecision = () => false; main.Navigate("Tools"); Check(main.PageTitle.Text == "Preferences" && main.HasDraft, "Preferences edits are protected on navigation");
            defaults.Text = "12"; Descendants<Button>(main).Single(b => b.Content is string label && label == "Save preferences").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Check(!main.HasDraft, "Saving preferences clears the unsaved state");
            main.DiscardDecision = () => true;
            main.Navigate("Preferences"); await Settle();
            var pageScroll=Descendants<ScrollViewer>(main).First(s=>s.Content is StackPanel);
            main.Height=440; await Settle(); pageScroll.ScrollToTop(); await Settle();
            SmoothScrolling.Wheel(pageScroll,-120); await Task.Delay(220);
            Check(pageScroll.VerticalOffset>0 && pageScroll.VerticalOffset<=30, "Wheel scroll moves a short eased distance");
            var startOffset=pageScroll.VerticalOffset; for(var i=0;i<20;i++) SmoothScrolling.Wheel(pageScroll,-120); await Task.Delay(220);
            Check(pageScroll.VerticalOffset-startOffset<=91, "Rapid wheel input cannot build an excessive scroll jump");
            main.FitToWorkArea(workArea);
            main.Navigate("Profiles"); await Settle(); Capture(main, output, "main-profiles.png");
            main.Navigate("Preferences"); await Settle(); Capture(main, output, "main-preferences.png");
            main.Navigate("Actions"); main.Width = 720; main.Height = 480; await Settle(); Capture(main, output, "main-minimum.png");
            var narrowGrid=Descendants<WrapPanel>(main).Single(w=>w.Name=="ActionLibrary");
            var narrowTiles=narrowGrid.Children.Cast<Button>().ToArray();
            var firstPosition=narrowTiles[0].TranslatePoint(new Point(),narrowGrid); var secondPosition=narrowTiles[1].TranslatePoint(new Point(),narrowGrid);
            Check(Math.Abs(firstPosition.Y-secondPosition.Y)<1 && secondPosition.X>firstPosition.X && narrowTiles[0].ActualWidth*2>narrowGrid.ActualWidth*.85, "Minimum-width action grid uses two full columns without wasted row space");
            foreach (var area in new[] { new Rect(0, 0, 853, 480), new Rect(-1024, 0, 1024, 700), new Rect(0, 0, 1920, 1040) })
            {
                main.FitToWorkArea(area); await Settle();
                Check(area.Contains(new Rect(main.Left, main.Top, main.ActualWidth, main.ActualHeight)), $"Main window fits logical work area {area.Width}x{area.Height}");
                if (area.Width == 853) Capture(main, output, "main-scaled-screen.png");
            }
            main.FitToWorkArea(workArea); main.Width = 720; main.Height = 480; await Settle();
            ThemeManager.Apply("Light"); await Settle(); Capture(main, output, "main-light.png"); Capture(tray, output, "tray-light.png"); ThemeManager.Apply("Dark");
            foreach(var window in new Window[]{tray,net,main})
            {
                var bitmap=Render(window); var pixel=new byte[4]; bitmap.CopyPixels(new Int32Rect(0,0,1,1),pixel,4,0);
                Check(window.AllowsTransparency && System.Windows.Shell.WindowChrome.GetWindowChrome(window) is null && pixel[3]==0, window.Title+" has transparent rounded corners without system chrome");
            }
            main.CloseButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Check(!main.IsVisible, "Caption close works"); main = null; net.Close(); net = null; tray.Hide();
            var timings = new List<double>(); for (var i = 0; i < 10; i++) { watch.Restart(); tray.Open(false); await Settle(); timings.Add(watch.Elapsed.TotalMilliseconds); tray.Hide(); }
            using var process = Process.GetCurrentProcess(); var cpuStart = process.TotalProcessorTime; var idleClock = Stopwatch.StartNew(); await Task.Delay(3000); process.Refresh();
            var measurement = new { FirstPanelMilliseconds = firstPanelMs, WarmPanelMilliseconds = timings, WorkingSetBytes = process.WorkingSet64, PrivateBytes = process.PrivateMemorySize64, IdleCpuCorePercent = (process.TotalProcessorTime - cpuStart).TotalMilliseconds / idleClock.Elapsed.TotalMilliseconds * 100, Caveat = "Debug/verification process after rendering multiple windows; not a clean-start or release idle baseline. RenderTargetBitmap captures WPF client content, not DWM acrylic." };
            File.WriteAllText(Path.Combine(output, "verification.json"), JsonSerializer.Serialize(new { Checks = checks, Measurements = measurement, OS = Environment.OSVersion.ToString(), Runtime = Environment.Version.ToString() }, new JsonSerializerOptions { WriteIndented = true }));
        }
        finally
        {
            if (originalClipboard is not null) Clipboard.SetDataObject(originalClipboard, true); else Clipboard.Clear();
            if (main is not null) { main.DiscardDecision = () => true; foreach(var editor in main.Editors) editor.DiscardDecision=()=>true; main.Close(); } if (net is not null) net.Close(); tray?.Hide();
        }
    }
    private static async Task Settle() { await Application.Current.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle); await Task.Delay(50); }
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) yield return match;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }
    private static RenderTargetBitmap Render(Window window)
    {
        window.UpdateLayout(); var content=(FrameworkElement)window.Content;
        var bitmap=new RenderTargetBitmap((int)content.ActualWidth,(int)content.ActualHeight,96,96,PixelFormats.Pbgra32); bitmap.Render(content); return bitmap;
    }
    private static void Capture(Window window, string folder, string name)
    {
        var bitmap=Render(window);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var stream = File.Create(Path.Combine(folder, name)); encoder.Save(stream);
    }
}
