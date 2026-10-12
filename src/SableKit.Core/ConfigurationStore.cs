using System.Text.Json;

namespace SableKit.Core;

public sealed class ConfigurationStore(string path)
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    public string Path { get; } = System.IO.Path.GetFullPath(path);

    public Configuration Load()
    {
        if (!File.Exists(Path)) return Configuration.CreateDefault();
        var configuration = JsonSerializer.Deserialize<Configuration>(File.ReadAllText(Path), Options) ?? throw new InvalidDataException("Settings are empty.");
        configuration.Validate();
        return configuration;
    }

    public void Save(Configuration configuration)
    {
        configuration.Validate();
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        var temporary = Path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(configuration, Options));
        if (File.Exists(Path)) File.Replace(temporary, Path, Path + ".bak");
        else File.Move(temporary, Path);
    }
}
