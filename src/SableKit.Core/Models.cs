using System.Text.Json.Serialization;

namespace SableKit.Core;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ActionKind { Password, Application, Network }
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PanelBehavior { StayOpen, CloseImmediately, CloseAfterCompletion }
public enum ExecutionState { Idle, Running, Completed, Failed, Cancelled, Unavailable }

public sealed class ActionDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "New action";
    public string Description { get; set; } = "";
    public string Aliases { get; set; } = "";
    public ActionKind Kind { get; set; }
    public bool Enabled { get; set; } = true;
    public int? PasswordLengthOverride { get; set; }
    public string Executable { get; set; } = "";
    public List<string> Arguments { get; set; } = [];
    public string Target { get; set; } = "";
    public PanelBehavior PanelBehavior { get; set; }
    public string Icon => Kind switch { ActionKind.Password => "key", ActionKind.Network => "network", _ => "terminal" };
    public string ToolName => Kind == ActionKind.Network ? "SableNet" : "SableKit";
}

public sealed class Profile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Default";
    public bool GridView { get; set; }
    public List<string> ActionIds { get; set; } = [];
    public override string ToString() => Name;
}

public sealed class Configuration
{
    public int SchemaVersion { get; set; } = 1;
    public int PasswordLength { get; set; } = 12;
    public string Theme { get; set; } = "Dark";
    public bool ReducedMotion { get; set; }
    public string ActiveProfileId { get; set; } = "default";
    public List<ActionDefinition> Actions { get; set; } = [];
    public List<Profile> Profiles { get; set; } = [];
    [JsonIgnore] public Profile ActiveProfile => Profiles.FirstOrDefault(p => p.Id == ActiveProfileId) ?? Profiles[0];

    public static Configuration CreateDefault() => new()
    {
        Actions = [
            new() { Id = "password", Name = "Generate password", Description = "12 characters · copy to clipboard", Aliases = "random secure password", Kind = ActionKind.Password },
            new() { Id = "network", Name = "Check clipboard address", Description = "Ping or test a TCP port", Aliases = "ping ip tcp connection", Kind = ActionKind.Network },
            new() { Id = "terminal", Name = "Open PowerShell", Description = "Start a Windows PowerShell session", Aliases = "shell console terminal", Kind = ActionKind.Application, Executable = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe") }
        ],
        Profiles = [
            new() { Id = "default", Name = "Default", ActionIds = ["password", "network", "terminal"] },
            new() { Id = "lab", Name = "Lab", ActionIds = ["network", "terminal"] }
        ]
    };

    public void Validate()
    {
        if (SchemaVersion != 1) throw new InvalidDataException("This settings version is not supported. Use the SableKit version that created it.");
        if (PasswordLength is < 8 or > 128) throw new InvalidDataException("Default password length must be 8–128.");
        if (Actions is null || Profiles is null || Profiles.Count == 0) throw new InvalidDataException("At least one profile is required.");
        if (Actions.Any(a => a is null) || Profiles.Any(p => p is null)) throw new InvalidDataException("Invalid configuration entries.");
        if (Actions.Select(a => a.Id).Distinct().Count() != Actions.Count || Profiles.Select(p => p.Id).Distinct().Count() != Profiles.Count) throw new InvalidDataException("Duplicate configuration IDs.");
        foreach (var action in Actions)
        {
            if (string.IsNullOrWhiteSpace(action.Id) || string.IsNullOrWhiteSpace(action.Name) || !Enum.IsDefined(action.Kind) || !Enum.IsDefined(action.PanelBehavior)) throw new InvalidDataException("Invalid action definition.");
            if (action.PasswordLengthOverride is < 8 or > 128) throw new InvalidDataException("Password length must be 8–128.");
            if (action.Arguments is null || action.Arguments.Any(a => a is null)) throw new InvalidDataException("Invalid application arguments.");
        }
        foreach (var profile in Profiles)
            if (string.IsNullOrWhiteSpace(profile.Id) || string.IsNullOrWhiteSpace(profile.Name) || profile.ActionIds is null || profile.ActionIds.Distinct().Count() != profile.ActionIds.Count || profile.ActionIds.Any(id => !Actions.Any(a => a.Id == id))) throw new InvalidDataException("A profile contains invalid or missing actions.");
        if (Theme is not ("Dark" or "Light" or "System")) Theme = "Dark";
        if (!Profiles.Any(p => p.Id == ActiveProfileId)) ActiveProfileId = Profiles[0].Id;
    }
}

public static class ActionSettings
{
    public static int PasswordLength(Configuration config, ActionDefinition action) => action.PasswordLengthOverride ?? config.PasswordLength;
    public static bool Matches(ActionDefinition action, string query) => $"{action.Name} {action.Description} {action.Aliases} {action.ToolName}".Contains(query.Trim(), StringComparison.OrdinalIgnoreCase);
}
