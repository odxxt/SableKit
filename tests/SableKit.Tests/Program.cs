using SableKit.Core;
using System.Net;
using System.Net.Sockets;

var checks = 0;
void Check(bool value, string name) { if (!value) throw new Exception("FAIL: " + name); checks++; Console.WriteLine("PASS: " + name); }
foreach (var (input, host, port) in new (string, string, int?)[] { ("10.0.0.25", "10.0.0.25", null), ("10.0.0.25:443", "10.0.0.25", 443), ("[2001:db8::25]:443", "2001:db8::25", 443), ("2001:db8::25", "2001:db8::25", null), ("example.com:65535", "example.com", 65535), ("localhost", "localhost", null) })
    Check(NetworkTarget.TryParse(input, out var value, out _) && value!.Host == host && value.Port == port, "Parse " + input);
foreach (var input in new[] { "10.0.0.1:", "10.0.0.1:0", "10.0.0.1:65536", "10.0.0.1:abc", "[::1]:-1", "example.com:443;calc", "host & whoami", "999.2.3.4", "127.1", "012.0.0.1", "1.2.3.4 5.6.7.8", "[::1", "[example.com]:443" })
    Check(!NetworkTarget.TryParse(input, out _, out _), "Reject " + input);
foreach (var (input, host, port) in new (string, string, int?)[] {
    ("https://example.com/path?a=443", "example.com", null),
    ("https://example.com:8443/path", "example.com", 8443),
    ("10.0.0.25 RDP 3389", "10.0.0.25", 3389),
    ("example.com service port 12345", "example.com", 12345),
    ("10.0.0.25 service 443", "10.0.0.25", 443),
    ("10.0.0.25 port 99999", "10.0.0.25", null),
    ("10.0.0.25 ticket 443", "10.0.0.25", null),
    ("[2001:db8::25]:443", "2001:db8::25", 443),
    ("server01", "server01", null) })
{
    var detected = ClipboardTargets.Extract(input);
    Check(detected.Count == 1 && detected[0].Target.Host == host && detected[0].Target.Port == port && detected[0].Error is null, "Extract " + input);
}
Check(ClipboardTargets.Extract("10.0.0.1:99999").Single().Error is not null, "Explicit invalid port requires correction");
Check(ClipboardTargets.Extract("10.0.0.1 port 22 and 10.0.0.2 port 443").Count == 2, "Multiple targets retained");
Check(ClipboardTargets.Extract("10.0.0.1 10.0.0.1").Count == 1, "Duplicate targets collapsed");
Check(ClipboardTargets.Extract("hello world").Count == 0, "Ordinary prose is not an address");
Check(ClipboardTargets.Extract(new string('a', 20000)).Count == 0, "Oversized clipboard inspection is bounded");
var config = Configuration.CreateDefault(); config.Validate(); var password = config.Actions[0];
Check(ActionSettings.PasswordLength(config, password) == 12, "Default password length");
config.PasswordLength = 20; Check(ActionSettings.PasswordLength(config, password) == 20, "Inheritance follows default");
password.PasswordLengthOverride = 12; Check(ActionSettings.PasswordLength(config, password) == 12, "Override survives default change");
password.PasswordLengthOverride = null; Check(ActionSettings.PasswordLength(config, password) == 20, "Reset restores inheritance");
Check(ActionSettings.Matches(password, "random"), "Aliases searchable");
Check(Passwords.Generate(12).Length == 12 && Passwords.Generate(8).Length == 8, "Configured password lengths");
Check(Passwords.Generate(128).Length == 128, "Maximum password length");
var root = Path.Combine(Path.GetTempPath(), "SableKit-tests-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
try
{
    var store = new ConfigurationStore(Path.Combine(root, "settings.json")); store.Save(config); config.ActiveProfileId = "lab"; config.ActiveProfile.GridView = true; store.Save(config);
    var loaded = store.Load(); Check(loaded.ActiveProfileId == "lab" && loaded.ActiveProfile.GridView, "Profile and view round trip"); Check(File.Exists(store.Path + ".bak"), "Atomic replacement retains backup");
    Check(loaded.Profiles[1].ActionIds.All(id => loaded.Actions.Any(a => a.Id == id)), "Profiles reference shared library");
    File.WriteAllText(store.Path, "broken-json"); try { store.Load(); throw new Exception("Corrupt file accepted"); } catch (System.Text.Json.JsonException) { Check(File.ReadAllText(store.Path) == "broken-json", "Corrupt configuration preserved"); }
}
finally { Directory.Delete(root, true); }
using var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); var portNumber = ((IPEndPoint)listener.LocalEndpoint).Port;
var accept = listener.AcceptTcpClientAsync(); var result = await NetworkChecks.RunAsync(new("127.0.0.1", portNumber), CancellationToken.None); using var accepted = await accept;
Check(result.Success && result.Detail.Contains("does not verify"), "TCP result distinguishes service health");
using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
try { await NetworkChecks.RunAsync(new("127.0.0.1", portNumber), cancelled.Token); throw new Exception("Cancellation ignored"); } catch (OperationCanceledException) { Check(true, "Network cancellation propagates"); }
Console.WriteLine($"{checks} checks passed.");
