using System.Net;
using System.Net.Sockets;
using System.Text;
using TSLib.Full;

namespace Gengfu.TeamSpeak.Tests;

/// <summary>
/// A TeamSpeak server on a local UDP port that refuses every client. It takes each attempt through
/// the handshake's opening steps, as TSLib's own server side does (TsCrypt.ProcessInit1), then
/// answers with an error line that carries no return code, as a real server answers clientinit.
/// </summary>
internal sealed class LocalTsServer : IDisposable
{
    // A client's packet: an 8-byte MAC, its id, the client id and its type, then the data. An
    // Init1 packet's data opens with a 4-byte version, then the handshake's step.
    private const int Header = 13;
    private const int Step = Header + 4;

    private readonly UdpClient _udp = new(new IPEndPoint(IPAddress.Loopback, 0));
    private readonly Dictionary<IPEndPoint, int> _attempts = [];
    private readonly string[] _refusals;

    /// <param name="refusals">The error line for each attempt in turn; the last one answers every later attempt.</param>
    public LocalTsServer(params string[] refusals)
    {
        _refusals = refusals;
        _ = ServeAsync();
    }

    public string Address => $"127.0.0.1:{((IPEndPoint)_udp.Client.LocalEndPoint!).Port}";

    public void Dispose() => _udp.Dispose();

    private async Task ServeAsync()
    {
        while (true)
        {
            try
            {
                var received = await _udp.ReceiveAsync();
                if (Answer(received.Buffer, received.RemoteEndPoint) is { } answer)
                {
                    await _udp.SendAsync(answer, received.RemoteEndPoint);
                }
            }
            catch (SocketException)
            {
                // After a packet to a client that has already gone, Windows fails the next receive.
            }
            catch (ObjectDisposedException)
            {
                return;
            }
        }
    }

    private byte[]? Answer(byte[] packet, IPEndPoint client)
    {
        if (packet.Length <= Step || (PacketType)(packet[Header - 1] & 0x0F) != PacketType.Init1)
        {
            return null;
        }
        // Each attempt opens a socket of its own.
        _attempts.TryAdd(client, _attempts.Count);
        var attempt = _attempts[client];
        switch (packet[Step])
        {
            case 0:
                // The server's cookie, then the client's random bytes reversed.
                var random = packet.AsSpan(Step + 5, 4).ToArray();
                Array.Reverse(random);
                return Init1([1, .. new byte[16], .. random]);
            case 2:
                // The cheapest puzzle: x = 1, n = 1 and level 1, each big-endian.
                var puzzle = new byte[1 + 64 + 64 + 4 + 100];
                puzzle[0] = 3;
                puzzle[64] = 1;
                puzzle[128] = 1;
                puzzle[132] = 1;
                return Init1(puzzle);
            case 4:
                // A real server refuses later, after the key exchange, under the session's keys.
                // Answered here, at the last Init1 step, TSLib has no keys yet and takes a command
                // marked unencrypted with a MAC of all zeros.
                var refusal = Encoding.UTF8.GetBytes(_refusals[Math.Min(attempt, _refusals.Length - 1)]);
                return [.. new byte[8], 0, 0, (byte)PacketType.Command | (byte)PacketFlags.Unencrypted, .. refusal];
            default:
                return null;
        }
    }

    // A server's packet: the MAC, fixed for Init1, then its id, 101 for Init1, and its type.
    private static byte[] Init1(byte[] data) =>
        [.. "TS3INIT1"u8, 0, 101, (byte)PacketType.Init1 | (byte)PacketFlags.Unencrypted, .. data];
}
