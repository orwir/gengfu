using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Heijden.Dns.Portable;
using Heijden.DNS;

namespace Gengfu.TeamSpeak;

/// <summary>
/// Turns the server address the user typed into the endpoint to connect to, the way the official
/// client does (ADR-0003): an IP literal as is; else the host's <c>_ts3._udp</c> SRV record; else
/// TSDNS, found through <c>_tsdns._tcp</c> SRV records on the host and its parent domains, with
/// <c>$PORT</c> standing for the typed port; else the system's DNS. A typed port always wins.
/// </summary>
public sealed class TsAddress
{
    public const int DefaultPort = 9987;

    // TSLib gives TSDNS one second, which is too short for some hosts.
    private static readonly TimeSpan TsDnsTimeout = TimeSpan.FromSeconds(3);

    private readonly Func<Resolver> _dns;
    private readonly Func<string, CancellationToken, Task<IPAddress[]>> _lookUpHost;

    /// <summary>
    /// Asks the DNS servers the system has at each lookup, so an app that runs for days follows a
    /// change of network, and one started before the network is up finds it once it is.
    /// </summary>
    public TsAddress()
        : this(() => new Resolver(), Dns.GetHostAddressesAsync)
    {
    }

    /// <param name="dns">Makes the resolver that looks up SRV records, once per lookup.</param>
    /// <param name="lookUpHost">Where host names are looked up.</param>
    internal TsAddress(Func<Resolver> dns, Func<string, CancellationToken, Task<IPAddress[]>> lookUpHost)
    {
        _dns = dns;
        _lookUpHost = lookUpHost;
    }

    /// <returns>Where to connect, or null when the address is malformed or nothing resolves it.</returns>
    public async Task<IPEndPoint?> ResolveAsync(string address, CancellationToken cancel = default)
    {
        address = address.Trim();
        if (IPEndPoint.TryParse(address, out var literal))
        {
            return literal.Port == 0 ? new IPEndPoint(literal.Address, DefaultPort) : literal;
        }
        if (!TrySplit(address, out var host, out var typedPort))
        {
            return null;
        }

        var dns = _dns();
        if (await SrvAsync(dns, "_ts3._udp." + host, cancel) is { } voice && await HostAsync(voice.Target, cancel) is { } voiceIp)
        {
            return new IPEndPoint(voiceIp, typedPort ?? voice.Port);
        }

        var portForTsDns = (typedPort ?? DefaultPort).ToString(CultureInfo.InvariantCulture);
        foreach (var domain in ThisAndParents(host))
        {
            if (await SrvAsync(dns, "_tsdns._tcp." + domain, cancel) is { } tsdns
                && await AskTsDnsAsync(tsdns, host, cancel) is { } answer
                && IPEndPoint.TryParse(answer.Replace("$PORT", portForTsDns, StringComparison.Ordinal), out var found))
            {
                return new IPEndPoint(found.Address, typedPort ?? (found.Port == 0 ? DefaultPort : found.Port));
            }
        }

        return await HostAsync(host, cancel) is { } ip ? new IPEndPoint(ip, typedPort ?? DefaultPort) : null;
    }

    private static bool TrySplit(string address, out string host, out int? port)
    {
        var colon = address.LastIndexOf(':');
        host = (colon < 0 ? address : address[..colon]).TrimEnd('.');
        port = null;
        if (colon >= 0)
        {
            if (!int.TryParse(address[(colon + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out var typed)
                || typed is < 1 or > ushort.MaxValue)
            {
                return false;
            }
            port = typed;
        }
        return Uri.CheckHostName(host) == UriHostNameType.Dns;
    }

    // teal.example.com → teal.example.com, example.com
    private static IEnumerable<string> ThisAndParents(string host)
    {
        var labels = host.Split('.');
        for (var i = 0; i < labels.Length - 1; i++)
        {
            yield return string.Join('.', labels[i..]);
        }
    }

    // Heijden's Query blocks its caller for the whole lookup, up to 3 s for each DNS server that
    // stays silent, so it runs on the thread pool and a cancel stops the wait. A reply its parser
    // can't read counts as no record.
    private static async Task<(string Target, int Port)?> SrvAsync(Resolver dns, string name, CancellationToken cancel)
    {
        Response response;
        try
        {
            response = await Task.Run(() => dns.Query(name, QType.SRV, QClass.IN), cancel).WaitAsync(cancel);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
        var best = response.RecordsSRV.OrderBy(record => record.PRIORITY).FirstOrDefault();
        return best is null ? null : (best.TARGET.TrimEnd('.'), best.PORT);
    }

    private async Task<IPAddress?> HostAsync(string host, CancellationToken cancel)
    {
        try
        {
            var addresses = await _lookUpHost(host, cancel);
            return addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork) ?? addresses.FirstOrDefault();
        }
        catch (SocketException)
        {
            return null;
        }
    }

    // A TSDNS server reads a host name and answers "ip:port", "ip:$PORT", or "404" when it has none.
    private async Task<string?> AskTsDnsAsync((string Target, int Port) server, string host, CancellationToken cancel)
    {
        if (await HostAsync(server.Target, cancel) is not { } ip)
        {
            return null;
        }
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancel);
        timeout.CancelAfter(TsDnsTimeout);
        try
        {
            using var tcp = new TcpClient(ip.AddressFamily);
            await tcp.ConnectAsync(ip, server.Port, timeout.Token);
            var stream = tcp.GetStream();
            await stream.WriteAsync(Encoding.ASCII.GetBytes(host), timeout.Token);
            var buffer = new byte[256];
            var read = await stream.ReadAsync(buffer, timeout.Token);
            var answer = Encoding.ASCII.GetString(buffer, 0, read).Trim();
            return answer is "" or "404" ? null : answer;
        }
        catch (Exception ex) when (ex is SocketException or IOException
            || (ex is OperationCanceledException && !cancel.IsCancellationRequested))
        {
            return null;
        }
    }
}
