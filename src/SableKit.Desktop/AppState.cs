using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SableKit.Desktop;

public sealed class ActionItem(ActionDefinition definition) : INotifyPropertyChanged
{
    public ActionDefinition Definition { get; } = definition;
    public string Name => Definition.Name;
    public string Description => Definition.Description;
    public string Icon => Definition.Icon;
    public string ToolName => Definition.ToolName;
    public ExecutionState State { get; private set; } = definition.Enabled ? ExecutionState.Idle : ExecutionState.Unavailable;
    public string Feedback { get; private set; } = "";
    public bool Running => State == ExecutionState.Running;
    public bool Copied => State == ExecutionState.Completed && Feedback == "Copied";
    public bool Available => Definition.Enabled && !Running;
    public event PropertyChangedEventHandler? PropertyChanged;
    public void SetState(ExecutionState state, string feedback)
    {
        State = state; Feedback = feedback;
        foreach (var property in new[] { nameof(State), nameof(Feedback), nameof(Running), nameof(Copied), nameof(Available) }) PropertyChanged?.Invoke(this, new(property));
    }
}

public sealed class AppState(ConfigurationStore store, Configuration configuration)
{
    public Configuration Config { get; } = configuration;
    public List<ActionItem> Items { get; private set; } = configuration.Actions.Select(a => new ActionItem(a)).ToList();
    public string Status { get; private set; } = "Ready when you are.";
    public string Result { get; private set; } = "";
    public bool LastFailed { get; private set; }
    public event Action? Changed;
    public event Action? ResultChanged;
    public event Action? HideRequested;
    private readonly Dictionary<string, CancellationTokenSource> active = [];
    public bool IsRunning => active.Count > 0;

    public void Save()
    {
        store.Save(Config);
        Items = Config.Actions.Select(a => Items.FirstOrDefault(i => ReferenceEquals(i.Definition, a)) ?? new ActionItem(a)).ToList();
        Changed?.Invoke();
    }
    public void SetResult(string summary, string detail = "", bool failed = false)
    { Status = summary; Result = detail; LastFailed = failed; ResultChanged?.Invoke(); }

    public static string ReadClipboard() => Clipboard.ContainsText() ? Clipboard.GetText() : "";
    public static async Task CopyAsync(string value)
    {
        for (var attempt = 0; ; attempt++)
        {
            try { Clipboard.SetText(value); return; }
            catch (COMException) when (attempt < 3) { await Task.Delay(60); }
        }
    }

    public async Task ExecuteAsync(ActionItem item, string? explicitTarget = null)
    {
        if (!item.Available || active.ContainsKey(item.Definition.Id)) return;
        var action = item.Definition;
        using var cancellation = new CancellationTokenSource(); active.Add(action.Id, cancellation);
        item.SetState(ExecutionState.Running, "Running…");
        SetResult($"Running {action.Name}…");
        if (action.PanelBehavior == PanelBehavior.CloseImmediately) HideRequested?.Invoke();
        try
        {
            string feedback;
            switch (action.Kind)
            {
                case ActionKind.Password:
                    var length = ActionSettings.PasswordLength(Config, action);
                    await CopyAsync(Passwords.Generate(length));
                    feedback = "Copied"; SetResult("Password copied", $"{length} random characters copied to your clipboard."); break;
                case ActionKind.Application:
                    if (!File.Exists(action.Executable)) throw new FileNotFoundException("The application is missing. Choose its executable in the action editor.");
                    var start = new ProcessStartInfo(action.Executable) { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(action.Executable)! };
                    foreach (var argument in action.Arguments) start.ArgumentList.Add(argument);
                    using (Process.Start(start) ?? throw new InvalidOperationException("Windows did not start the application.")) { }
                    feedback = "Opened"; SetResult("Application launched", action.Name); break;
                case ActionKind.Network:
                    var input = explicitTarget ?? (string.IsNullOrWhiteSpace(action.Target) ? ReadClipboard() : action.Target);
                    if (!NetworkTarget.TryParse(input, out var target, out var error)) throw new InvalidOperationException(error);
                    var result = await NetworkChecks.RunAsync(target!, cancellation.Token);
                    SetResult(result.Summary, $"{result.Detail}\nElapsed: {result.Milliseconds} ms", !result.Success);
                    if (!result.Success) { item.SetState(ExecutionState.Failed, result.Summary); return; }
                    feedback = "Completed"; break;
                default: throw new InvalidOperationException("This operation is unavailable.");
            }
            item.SetState(ExecutionState.Completed, feedback);
            if (action.PanelBehavior == PanelBehavior.CloseAfterCompletion) HideRequested?.Invoke();
            await Task.Delay(1800, cancellation.Token);
            if (item.State == ExecutionState.Completed) item.SetState(ExecutionState.Idle, "");
        }
        catch (OperationCanceledException) { item.SetState(ExecutionState.Cancelled, "Cancelled"); SetResult("Action cancelled"); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception or COMException or ArgumentException)
        { item.SetState(ExecutionState.Failed, "Failed"); SetResult("Action failed", ex.Message, true); }
        finally { active.Remove(action.Id); }
    }
    public void CancelAll() { foreach (var source in active.Values.ToArray()) source.Cancel(); }
}
