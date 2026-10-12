using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace SableKit.Core;

public sealed record CheckResult(bool Success, string Summary, string Detail, long Milliseconds);

public static class NetworkChecks
{
    public static async Task<CheckResult> RunAsync(NetworkTarget target, CancellationToken cancellationToken)
    {
        var clock = Stopwatch.StartNew();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            if (target.Port is int port)
            {
                using var client = new TcpClient();
                await client.ConnectAsync(target.Host, port, timeout.Token);
                return new(true, "TCP connection accepted", $"{target.Display}\nThe port is reachable. This does not verify application-service health.", clock.ElapsedMilliseconds);
            }
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(target.Host, TimeSpan.FromSeconds(4), cancellationToken: timeout.Token);
            return new(reply.Status == IPStatus.Success, reply.Status == IPStatus.Success ? "Host replied" : $"Ping: {reply.Status}", $"{target.Host}\nAddress: {reply.Address}\nRound trip: {reply.RoundtripTime} ms\nICMP may be blocked even when the host is available.", clock.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { return new(false, "Check timed out", $"No result from {target.Display} within 5 seconds.", clock.ElapsedMilliseconds); }
        catch (Exception ex) when (ex is SocketException or PingException or ArgumentException)
        { return new(false, "Connection failed", ex.Message, clock.ElapsedMilliseconds); }
    }

    public static async Task<CheckResult> DnsAsync(string host, CancellationToken token)
    {
        var clock = Stopwatch.StartNew();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token); timeout.CancelAfter(5000);
        try
        {
            var result = await Dns.GetHostEntryAsync(host, timeout.Token);
            return new(true, result.HostName, string.Join(Environment.NewLine, result.AddressList.Select(a => a.ToString())), clock.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) { return new(false, "DNS timed out", "No DNS result within 5 seconds.", clock.ElapsedMilliseconds); }
        catch (SocketException ex) { return new(false, "DNS lookup failed", ex.Message, clock.ElapsedMilliseconds); }
    }
}
