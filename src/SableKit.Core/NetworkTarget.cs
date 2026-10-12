using System.Net;
using System.Text.RegularExpressions;

namespace SableKit.Core;

public sealed record NetworkTarget(string Host, int? Port)
{
    public string Display => Port is null ? Host : Host.Contains(':') ? $"[{Host}]:{Port}" : $"{Host}:{Port}";

    public static bool TryParse(string? input, out NetworkTarget? target, out string error)
    {
        target = null; error = "Enter an IP address or hostname, optionally followed by a port.";
        var value = input?.Trim() ?? "";
        if (value.Length is 0 or > 300 || value.Any(char.IsWhiteSpace)) return false;
        string host; string? portText = null;
        if (value.StartsWith('['))
        {
            var close = value.IndexOf(']');
            if (close < 0) { error = "Close the IPv6 address with ], then add :port if needed."; return false; }
            host = value[1..close];
            if (!IPAddress.TryParse(host, out var ipv6) || ipv6.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6) return false;
            if (close + 1 < value.Length)
            {
                if (value[close + 1] != ':') return false;
                portText = value[(close + 2)..];
            }
        }
        else if (value.Count(c => c == ':') > 1) host = value;
        else
        {
            var split = value.IndexOf(':');
            host = split < 0 ? value : value[..split];
            if (split >= 0) portText = value[(split + 1)..];
        }
        var isIp = IPAddress.TryParse(host, out _);
        // Reject ambiguous numeric formats accepted by legacy IPv4 parsing.
        if (isIp && !host.Contains(':') && (!Regex.IsMatch(host, @"^\d{1,3}(\.\d{1,3}){3}$") || host.Split('.').Any(p => p.Length > 1 && p[0] == '0'))) return false;
        if (!isIp && (host.Contains(':') || host.All(c => char.IsDigit(c) || c == '.') || !IsHostname(host))) return false;
        int? port = null;
        if (portText is not null)
        {
            if (!Regex.IsMatch(portText, @"^\d{1,5}$") || !int.TryParse(portText, out var p) || p is < 1 or > 65535)
            { error = "Port must be a number from 1 to 65535. Correct the port before running."; return false; }
            port = p;
        }
        target = new(host, port); error = ""; return true;
    }

    private static bool IsHostname(string value) => value.Length <= 253 && value.TrimEnd('.').Split('.').All(label => label.Length is > 0 and <= 63 && Regex.IsMatch(label, @"^[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?$"));
}
