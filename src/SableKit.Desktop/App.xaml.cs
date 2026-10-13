using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Interop;
using Forms = System.Windows.Forms;

namespace SableKit.Desktop;

public partial class App : Application
{
    private Forms.NotifyIcon? trayIcon;
    private HwndSource? hotkey;
    private Mutex? instance;
    private EventWaitHandle? showEvent;
    private RegisteredWaitHandle? wait;
    private TrayWindow? tray;
    private MainWindow? main;
    private NetworkWindow? network;
    public AppState State { get; private set; } = null!;
    public static bool Exiting { get; private set; }
    internal static bool DisableBackdrop { get; private set; }
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DisableBackdrop = e.Args.Contains("--opaque");
        if (e.Args.Contains("--software-rendering")) System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
        var verification = e.Args.Length == 2 && e.Args[0] == "--verify";
        var measure = e.Args.Length >= 2 && e.Args[0] == "--measure";
        var dataFolder = verification || measure ? Path.Combine(Path.GetFullPath(e.Args[1]), "test-data") : Path.Combine(AppContext.BaseDirectory, "SableKit-data");
        if (!verification && !measure)
        {
            var suffix = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(dataFolder)))[..16];
            instance = new Mutex(true, "Local\\SableKit-" + suffix, out var created);
            showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "Local\\SableKit-show-" + suffix);
            if (!created) { showEvent.Set(); Shutdown(); return; }
            wait = ThreadPool.RegisterWaitForSingleObject(showEvent, (_, _) => Dispatcher.BeginInvoke(OpenTray), null, Timeout.Infinite, false);
        }
        try
        {
            var store = new ConfigurationStore(Path.Combine(dataFolder, "settings.json"));
            var config = verification ? Configuration.CreateDefault() : store.Load(); store.Save(config);
            State = new AppState(store, config); ThemeManager.Apply(config.Theme);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            if (verification) { Directory.CreateDirectory(e.Args[1]); File.WriteAllText(Path.Combine(e.Args[1], "failure.txt"), ex.ToString()); }
            else MessageBox.Show($"SableKit could not open its settings. Your existing file has been preserved.\n\n{ex.Message}\n\nExtract SableKit to a writable folder and try again.", "SableKit", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1); return;
        }
        if (verification)
        {
            try { await Verification.Run(this, e.Args[1]); Shutdown(0); }
            catch (Exception ex) { File.WriteAllText(Path.Combine(e.Args[1], "failure.txt"), ex.ToString()); Shutdown(1); }
            return;
        }
        CreateTray(); RegisterShortcut(); OpenTray();
        if (measure)
        {
            await PerformanceProbe.Run(tray!, e.Args[1]); Quit();
        }
    }
    public void OpenTray() { tray ??= new TrayWindow(State); tray.Open(); }
    public void OpenMain(string page = "Actions")
    {
        if (main is null) { main = new MainWindow(State); main.Closed += (_, _) => main = null; }
        main.Navigate(page); main.Show(); if (main.WindowState == WindowState.Minimized) main.WindowState = WindowState.Normal; main.Activate();
    }
    public void OpenNetwork()
    {
        if (network is null) { network = new NetworkWindow(); network.Closed += (_, _) => network = null; }
        network.Show(); if (network.WindowState == WindowState.Minimized) network.WindowState = WindowState.Normal; network.Activate();
    }
    private void CreateTray()
    {
        var menu = new Forms.ContextMenuStrip(); menu.Items.Add("Quick panel", null, (_, _) => OpenTray()); menu.Items.Add("Open SableKit", null, (_, _) => OpenMain()); menu.Items.Add(new Forms.ToolStripSeparator()); menu.Items.Add("Quit", null, (_, _) => Quit());
        trayIcon = new Forms.NotifyIcon { Icon = CreateIcon(), Text = "SableKit · Ctrl+Alt+K", Visible = true, ContextMenuStrip = menu };
        trayIcon.MouseClick += (_, e) => { if (e.Button == Forms.MouseButtons.Left) { if (tray?.IsVisible == true && !tray.Pinned) tray.Hide(); else OpenTray(); } };
    }
    private static System.Drawing.Icon CreateIcon()
    {
        using var bitmap = new System.Drawing.Bitmap(32, 32); using var graphics = System.Drawing.Graphics.FromImage(bitmap);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; graphics.Clear(System.Drawing.Color.Transparent);
        using var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(244, 244, 245));
        graphics.FillPolygon(brush, [new(19, 2), new(5, 19), new(14, 19), new(12, 30), new(28, 12), new(18, 12)]);
        var handle = bitmap.GetHicon(); try { return (System.Drawing.Icon)System.Drawing.Icon.FromHandle(handle).Clone(); } finally { DestroyIcon(handle); }
    }
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr handle);
    private void RegisterShortcut()
    {
        hotkey = new HwndSource(new HwndSourceParameters("SableKit hotkeys") { Width = 0, Height = 0, WindowStyle = 0, ParentWindow = new IntPtr(-3) });
        hotkey.AddHook((IntPtr hwnd, int message, IntPtr w, IntPtr l, ref bool handled) => { if (message == 0x0312 && w.ToInt32() == 1) { OpenTray(); handled = true; } return IntPtr.Zero; });
        if (!NativeWindow.RegisterHotKey(hotkey.Handle, 1, 0x4000 | 0x2 | 0x1, 0x4B)) State.SetResult("Ctrl+Alt+K is already in use", "Use the tray icon to open SableKit. Custom global bindings are planned.", true);
    }
    private void Quit() { State.CancelAll(); Exiting = true; Shutdown(); }
    protected override void OnExit(ExitEventArgs e)
    {
        Exiting = true; State?.CancelAll();
        if (hotkey is not null) { NativeWindow.UnregisterHotKey(hotkey.Handle, 1); hotkey.Dispose(); }
        if (trayIcon is not null) { trayIcon.Visible = false; trayIcon.Icon?.Dispose(); trayIcon.ContextMenuStrip?.Dispose(); trayIcon.Dispose(); }
        wait?.Unregister(null); showEvent?.Dispose(); instance?.Dispose(); base.OnExit(e);
    }
}
