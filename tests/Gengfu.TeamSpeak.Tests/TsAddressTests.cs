using System.Net;
using System.Net.Sockets;
using Heijden.Dns.Portable;

namespace Gengfu.TeamSpeak.Tests;

public sealed class TsAddressTests : IDisposable
{
    private readonly LocalDns _dns = new();
    private readonly LocalTsDns _tsdns = new();
    private readonly Dictionary<string, IPAddress> _hosts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tsdns.example.test"] = IPAddress.Loopback,
    };

    private readonly TsAddress _address;

    public TsAddressTests() => _address = new TsAddress(() => new Resolver(_dns.EndPoint), LookUpHost);

    public void Dispose()
    {
        _dns.Dispose();
        _tsdns.Dispose();
    }

    [Theory]
    [InlineData("45.137.153.17:9608", "45.137.153.17:9608")]
    [InlineData("45.137.153.17", "45.137.153.17:9987")]
    [InlineData(" 45.137.153.17:9608 ", "45.137.153.17:9608")]
    [InlineData("[2001:db8::7]:9608", "[2001:db8::7]:9608")]
    [InlineData("2001:db8::7", "[2001:db8::7]:9987")]
    public async Task An_IP_literal_is_used_as_is(string typed, string expected)
    {
        Assert.Equal(IPEndPoint.Parse(expected), await _address.ResolveAsync(typed));
        Assert.Empty(_dns.Asked);
    }

    [Theory]
    [InlineData("voice.example.test", "203.0.113.5:9100")]
    [InlineData("voice.example.test:9200", "203.0.113.5:9200")]
    public async Task The_hosts_ts3_SRV_record_gives_the_server_and_a_typed_port_wins(string typed, string expected)
    {
        _dns.AddSrv("_ts3._udp.voice.example.test", "box.example.test", 9100);
        _hosts["box.example.test"] = IPAddress.Parse("203.0.113.5");

        Assert.Equal(IPEndPoint.Parse(expected), await _address.ResolveAsync(typed));
    }

    [Fact]
    public async Task The_SRV_record_with_the_lowest_priority_value_wins()
    {
        _dns.AddSrv("_ts3._udp.voice.example.test", "backup.example.test", 9100, priority: 20);
        _dns.AddSrv("_ts3._udp.voice.example.test", "box.example.test", 9100, priority: 10);
        _hosts["backup.example.test"] = IPAddress.Parse("203.0.113.6");
        _hosts["box.example.test"] = IPAddress.Parse("203.0.113.5");

        Assert.Equal(IPEndPoint.Parse("203.0.113.5:9100"), await _address.ResolveAsync("voice.example.test"));
    }

    [Theory]
    [InlineData("teal.example.test:9608", "203.0.113.7:9608")]
    [InlineData("teal.example.test", "203.0.113.7:9987")]
    public async Task TSDNS_found_on_a_parent_domain_gives_the_server_with_PORT_as_the_typed_port(string typed, string expected)
    {
        _dns.AddSrv("_tsdns._tcp.example.test", "tsdns.example.test", _tsdns.Port);
        _tsdns.Answer("teal.example.test", "203.0.113.7:$PORT");

        Assert.Equal(IPEndPoint.Parse(expected), await _address.ResolveAsync(typed));
        Assert.Equal(["teal.example.test"], _tsdns.Asked);
        Assert.Equal(
            ["_ts3._udp.teal.example.test", "_tsdns._tcp.teal.example.test", "_tsdns._tcp.example.test"],
            _dns.Asked);
    }

    [Theory]
    [InlineData("teal.example.test:9608", "203.0.113.7:9608")]
    [InlineData("teal.example.test", "203.0.113.7:9000")]
    public async Task A_typed_port_wins_over_the_port_TSDNS_gives(string typed, string expected)
    {
        _dns.AddSrv("_tsdns._tcp.teal.example.test", "tsdns.example.test", _tsdns.Port);
        _tsdns.Answer("teal.example.test", "203.0.113.7:9000");

        Assert.Equal(IPEndPoint.Parse(expected), await _address.ResolveAsync(typed));
    }

    [Fact]
    public async Task A_TSDNS_without_the_host_falls_back_to_the_systems_DNS()
    {
        _dns.AddSrv("_tsdns._tcp.example.test", "tsdns.example.test", _tsdns.Port);
        _hosts["teal.example.test"] = IPAddress.Parse("198.51.100.9");

        Assert.Equal(IPEndPoint.Parse("198.51.100.9:9987"), await _address.ResolveAsync("teal.example.test"));
        Assert.Equal(["teal.example.test"], _tsdns.Asked);
    }

    [Fact]
    public async Task An_unreachable_TSDNS_falls_back_to_the_systems_DNS()
    {
        _dns.AddSrv("_tsdns._tcp.example.test", "tsdns.example.test", ClosedTcpPort());
        _hosts["teal.example.test"] = IPAddress.Parse("198.51.100.9");

        Assert.Equal(IPEndPoint.Parse("198.51.100.9:9608"), await _address.ResolveAsync("teal.example.test:9608"));
    }

    [Fact]
    public async Task Without_records_the_systems_DNS_resolves_the_host()
    {
        _hosts["plain.example.test"] = IPAddress.Parse("198.51.100.4");

        Assert.Equal(IPEndPoint.Parse("198.51.100.4:9987"), await _address.ResolveAsync("plain.example.test"));
        Assert.Equal(IPEndPoint.Parse("198.51.100.4:9608"), await _address.ResolveAsync("plain.example.test:9608"));
    }

    [Fact]
    public async Task A_host_nothing_resolves_gives_nothing()
    {
        Assert.Null(await _address.ResolveAsync("nowhere.example.test"));
    }

    [Fact]
    public async Task A_DNS_reply_that_cannot_be_read_counts_as_no_record()
    {
        _dns.Break("_ts3._udp.voice.example.test");
        _hosts["voice.example.test"] = IPAddress.Parse("198.51.100.4");

        Assert.Equal(IPEndPoint.Parse("198.51.100.4:9987"), await _address.ResolveAsync("voice.example.test"));
    }

    [Fact]
    public async Task The_DNS_servers_are_read_again_for_each_lookup()
    {
        using var before = new LocalDns();
        var servers = before.EndPoint;
        var address = new TsAddress(() => new Resolver(servers), LookUpHost);
        _dns.AddSrv("_ts3._udp.voice.example.test", "box.example.test", 9100);
        _hosts["box.example.test"] = IPAddress.Parse("203.0.113.5");

        Assert.Null(await address.ResolveAsync("voice.example.test"));
        servers = _dns.EndPoint;
        Assert.Equal(IPEndPoint.Parse("203.0.113.5:9100"), await address.ResolveAsync("voice.example.test"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("teal.example.test:")]
    [InlineData("teal.example.test:0")]
    [InlineData("teal.example.test:65536")]
    [InlineData("teal example.test")]
    public async Task A_malformed_address_gives_nothing_without_asking_anyone(string typed)
    {
        Assert.Null(await _address.ResolveAsync(typed));
        Assert.Empty(_dns.Asked);
    }

    private Task<IPAddress[]> LookUpHost(string host, CancellationToken cancel) =>
        _hosts.TryGetValue(host, out var ip)
            ? Task.FromResult(new[] { ip })
            : Task.FromException<IPAddress[]>(new SocketException((int)SocketError.HostNotFound));

    private static int ClosedTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
