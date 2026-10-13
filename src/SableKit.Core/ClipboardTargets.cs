using System.Net;
using System.Text.RegularExpressions;

namespace SableKit.Core;

public sealed record DetectedTarget(NetworkTarget Target, string PortText = "", string? Error = null)
{
    public string Display => Error is null ? Target.Display : $"{Target.Host} : {PortText}";
    public override string ToString() => Display;
}

public static class ClipboardTargets
{
    private static readonly TimeSpan Budget = TimeSpan.FromMilliseconds(100);
    private static readonly Regex Candidates = new(@"https?://[^\s<>""']+|\[[0-9a-fA-F:%]+\](?::[^\s,;)]+)?|(?<![\w@])(?:\d{1,3}(?:\.\d{1,3}){3}|(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?\.)+[a-zA-Z][a-zA-Z0-9-]*|localhost)(?::[^\s,;)]+)?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, Budget);
    private static readonly HashSet<int> CommonPorts = [21, 22, 25, 53, 80, 110, 135, 139, 143, 389, 443, 445, 465, 587, 636, 993, 995, 1433, 1521, 3306, 3389, 5432, 5900, 5985, 5986, 8000, 8080, 8443];

    public static IReadOnlyList<DetectedTarget> Extract(string? input)
    {
        if (string.IsNullOrWhiteSpace(input) || input.Length > 16384) return [];
        var text = input.Trim();
        if (NetworkTarget.TryParse(text, out var exact, out _) && LooksLikeAddress(exact!.Host))
            return [new(exact, exact.Port?.ToString() ?? "")];
        try
        {
            var matches = Candidates.Matches(text).Cast<Match>().Take(32).ToArray();
            var found = new List<DetectedTarget>();
            for (var i = 0; i < matches.Length; i++)
            {
                var m = matches[i]; var value = m.Value.TrimEnd('.', ',', ';', ')');
                if (value.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    // Extract only the authority. URL paths, query values and credentials are not command inputs.
                    var authority = value[(value.IndexOf("://", StringComparison.Ordinal) + 3)..].Split('/', '?', '#')[0];
                    if (authority.Contains('@')) authority = authority[(authority.LastIndexOf('@') + 1)..];
                    value = authority;
                }
                if (!NetworkTarget.TryParse(value, out var target, out var error))
                {
                    var colon = value.LastIndexOf(':');
                    if (colon > 0 && NetworkTarget.TryParse(value[..colon], out var hostOnly, out _) && hostOnly!.Port is null)
                        found.Add(new(hostOnly, value[(colon + 1)..], error));
                    continue;
                }
                if (target!.Port is null)
                {
                    var end = i + 1 < matches.Length ? matches[i + 1].Index : text.Length;
                    var tail = text.Substring(m.Index + m.Length, Math.Min(96, end - m.Index - m.Length)).Split('\n', ';')[0];
                    var labeled = Regex.Match(tail, @"\b(?:port|tcp)\s*(?:is\s*|[:=]\s*)?(\d{1,10})\b", RegexOptions.IgnoreCase, Budget);
                    int? inferred = null;
                    if (labeled.Success)
                    {
                        if (int.TryParse(labeled.Groups[1].Value, out var port) && port is > 0 and <= 65535) inferred = port;
                    }
                    else
                    {
                        var ports = Regex.Matches(tail, @"\b\d{1,5}\b", RegexOptions.None, Budget).Cast<Match>()
                            .Where(n => !Regex.IsMatch(tail[..n.Index], @"(?:ticket|case|id)\s*[#:=]?\s*$", RegexOptions.IgnoreCase, Budget))
                            .Select(n => int.Parse(n.Value)).Where(CommonPorts.Contains).Distinct().ToArray();
                        if (ports.Length == 1) inferred = ports[0];
                    }
                    target = target with { Port = inferred };
                }
                found.Add(new(target, target.Port?.ToString() ?? ""));
            }
            return found.DistinctBy(t => (t.Target.Host.ToLowerInvariant(), t.PortText)).ToArray();
        }
        catch (RegexMatchTimeoutException) { return []; }
    }

    private static bool LooksLikeAddress(string host) => IPAddress.TryParse(host, out _) || host.Contains('.') || host.Contains('-') || host.Any(char.IsDigit) || host.Equals("localhost", StringComparison.OrdinalIgnoreCase);
}
