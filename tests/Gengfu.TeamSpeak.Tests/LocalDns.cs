using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Gengfu.TeamSpeak.Tests;

/// <summary>
/// A DNS server on a local UDP port: answers SRV queries from its table, and "no such name" to
/// everything else.
/// </summary>
internal sealed class LocalDns : IDisposable
{
    private const ushort SrvType = 33;

    private readonly UdpClient _udp = new(new IPEndPoint(IPAddress.Loopback, 0));
    private readonly ConcurrentDictionary<string, List<(ushort Priority, string Target, ushort Port)>> _srv =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, bool> _broken = new(StringComparer.OrdinalIgnoreCase);

    public LocalDns() => _ = ServeAsync();

    public IPEndPoint EndPoint => (IPEndPoint)_udp.Client.LocalEndPoint!;

    /// <summary>Every name asked for, in order.</summary>
    public ConcurrentQueue<string> Asked { get; } = new();

    public void AddSrv(string name, string target, int port, ushort priority = 10) =>
        _srv.GetOrAdd(name, _ => []).Add((priority, target, (ushort)port));

    /// <summary>Answers the name with a reply that claims a record and then ends.</summary>
    public void Break(string name) => _broken[name] = true;

    public void Dispose() => _udp.Dispose();

    private async Task ServeAsync()
    {
        try
        {
            while (true)
            {
                var query = await _udp.ReceiveAsync();
                await _udp.SendAsync(Answer(query.Buffer), query.RemoteEndPoint);
            }
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private byte[] Answer(byte[] query)
    {
        // The question: length-prefixed labels from byte 12 up to a zero byte, then type and class.
        var labels = new List<string>();
        var end = 12;
        for (; query[end] != 0; end += query[end] + 1)
        {
            labels.Add(Encoding.ASCII.GetString(query, end + 1, query[end]));
        }
        var type = BinaryPrimitives.ReadUInt16BigEndian(query.AsSpan(end + 1));
        end += 5;
        var name = string.Join('.', labels);
        Asked.Enqueue(name);
        if (_broken.ContainsKey(name))
        {
            return [.. query.AsSpan(0, 2), 0x85, 0x80, 0, 1, 0, 1, 0, 0, 0, 0, .. query.AsSpan(12, end - 12)];
        }
        var records = type == SrvType && _srv.TryGetValue(name, out var found) ? found : [];

        var reply = new List<byte>();
        reply.AddRange(query.AsSpan(0, 2).ToArray());
        reply.AddRange([0x85, (byte)(records.Count > 0 ? 0x80 : 0x83)]);
        reply.AddRange([0, 1, 0, (byte)records.Count, 0, 0, 0, 0]);
        reply.AddRange(query.AsSpan(12, end - 12).ToArray());
        foreach (var (priority, target, port) in records)
        {
            var data = new List<byte>();
            data.AddRange(BigEndian(priority));
            data.AddRange(BigEndian(0));
            data.AddRange(BigEndian(port));
            foreach (var label in target.Split('.'))
            {
                data.Add((byte)label.Length);
                data.AddRange(Encoding.ASCII.GetBytes(label));
            }
            data.Add(0);

            reply.AddRange([0xC0, 12]);
            reply.AddRange(BigEndian(SrvType));
            reply.AddRange([0, 1, 0, 0, 0, 60]);
            reply.AddRange(BigEndian((ushort)data.Count));
            reply.AddRange(data);
        }
        return [.. reply];
    }

    private static byte[] BigEndian(ushort value) => [(byte)(value >> 8), (byte)value];
}
