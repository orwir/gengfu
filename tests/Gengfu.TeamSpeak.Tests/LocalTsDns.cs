using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Gengfu.TeamSpeak.Tests;

/// <summary>A TSDNS server on a local TCP port: reads a host name, answers from its table or "404".</summary>
internal sealed class LocalTsDns : IDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly ConcurrentDictionary<string, string> _answers = new(StringComparer.OrdinalIgnoreCase);

    public LocalTsDns()
    {
        _listener.Start();
        _ = ServeAsync();
    }

    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    /// <summary>Every host name asked for, in order.</summary>
    public ConcurrentQueue<string> Asked { get; } = new();

    public void Answer(string host, string answer) => _answers[host] = answer;

    public void Dispose() => _listener.Stop();

    private async Task ServeAsync()
    {
        try
        {
            while (true)
            {
                using var client = await _listener.AcceptTcpClientAsync();
                var stream = client.GetStream();
                var buffer = new byte[256];
                var read = await stream.ReadAsync(buffer);
                var host = Encoding.ASCII.GetString(buffer, 0, read);
                Asked.Enqueue(host);
                await stream.WriteAsync(Encoding.ASCII.GetBytes(_answers.GetValueOrDefault(host, "404")));
            }
        }
        catch (Exception ex) when (ex is ObjectDisposedException or SocketException or IOException)
        {
        }
    }
}
