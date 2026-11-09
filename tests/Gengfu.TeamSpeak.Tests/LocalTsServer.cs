using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X9;
using TSLib.Full;
using ECPoint = Org.BouncyCastle.Math.EC.ECPoint;

namespace Gengfu.TeamSpeak.Tests;

/// <summary>
/// A TeamSpeak server on a local UDP port with a line for each attempt. It takes each attempt through
/// the handshake's opening steps, as TSLib's own server side does (TsCrypt.ProcessInit1). An error
/// line refuses the attempt there, as a real server answers clientinit. Any other line lets the bot
/// in, through the key exchange TSLib keeps for older servers (initivexpand), and is then sent to it,
/// as a server sends the notice that the bot was kicked; an empty line lets the bot stay.
/// </summary>
internal sealed class LocalTsServer : IDisposable
{
    /// <summary>The client id the server gives the bot.</summary>
    public const int BotId = 2;

    // A client's packet: an 8-byte MAC, its id, the client id and its type, then the data. An
    // Init1 packet's data opens with a 4-byte version, then the handshake's step; the last step
    // carries the solved puzzle, then the client's clientinitiv.
    private const int Header = 13;
    private const int Step = Header + 4;
    private const int ClientInitIv = Step + 1 + 232 + 64;

    // What TS3 3.13.7 sends a client it lets in, less the client's own settings.
    private static readonly string InitServer = string.Join(' ',
        @"initserver virtualserver_name=Local virtualserver_welcomemessage virtualserver_platform=Windows",
        @"virtualserver_version=3.13.7\s[Build:\s1655727713] virtualserver_maxclients=32 virtualserver_created=0",
        @"virtualserver_codec_encryption_mode=0 virtualserver_hostmessage virtualserver_hostmessage_mode=0",
        @"virtualserver_default_server_group=8 virtualserver_default_channel_group=8 virtualserver_hostbanner_url",
        @"virtualserver_hostbanner_gfx_url virtualserver_hostbanner_gfx_interval=0",
        @"virtualserver_priority_speaker_dimm_modificator=-18.0000 virtualserver_id=1 virtualserver_hostbutton_tooltip",
        @"virtualserver_hostbutton_url virtualserver_hostbutton_gfx_url virtualserver_name_phonetic virtualserver_icon_id=0",
        @"virtualserver_ask_for_privilegekey=0 virtualserver_hostbanner_mode=0 virtualserver_channel_temp_delete_delay_default=0",
        $"acn=Gengfu aclid={BotId} pv=7 client_talk_power=0 client_needed_serverquery_view_power=75");

    private readonly UdpClient _udp = new(new IPEndPoint(IPAddress.Loopback, 0));
    private readonly IdentityData _key = IdentityData.GenerateNewIdentity(0);
    private readonly Dictionary<IPEndPoint, Attempt> _attempts = [];
    private readonly string[] _lines;
    private int _attemptCount;

    /// <param name="lines">The line for each attempt in turn; the last one answers every later attempt.</param>
    public LocalTsServer(params string[] lines)
    {
        _lines = lines;
        _ = ServeAsync();
    }

    public string Address => $"127.0.0.1:{((IPEndPoint)_udp.Client.LocalEndPoint!).Port}";

    /// <summary>How many attempts have reached the server.</summary>
    public int Attempts => Volatile.Read(ref _attemptCount);

    public void Dispose() => _udp.Dispose();

    private async Task ServeAsync()
    {
        while (true)
        {
            try
            {
                var received = await _udp.ReceiveAsync();
                foreach (var answer in Answer(received.Buffer, received.RemoteEndPoint))
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

    private byte[][] Answer(byte[] packet, IPEndPoint client)
    {
        if (packet.Length <= Step)
        {
            return [];
        }
        switch ((PacketType)(packet[Header - 1] & 0x0F))
        {
            case PacketType.Init1:
                // Each attempt opens a socket of its own.
                if (!_attempts.TryGetValue(client, out var attempt))
                {
                    attempt = new Attempt(_lines[Math.Min(_attempts.Count, _lines.Length - 1)]);
                    _attempts.Add(client, attempt);
                    Interlocked.Increment(ref _attemptCount);
                }
                return Handshake(packet, attempt);
            case PacketType.Command when _attempts.TryGetValue(client, out var joining) && joining.Mac is { } mac && !joining.Joined:
                // The client's first command, clientinit, shows it has the keys: only now does it
                // take packets that carry the MAC made from them.
                joining.Joined = true;
                return joining.Line.Length == 0
                    ? [Command(1, mac, InitServer)]
                    : [Command(1, mac, InitServer), Command(2, mac, joining.Line)];
            default:
                return [];
        }
    }

    private byte[][] Handshake(byte[] packet, Attempt attempt)
    {
        switch (packet[Step])
        {
            case 0:
                // The server's cookie, then the client's random bytes reversed.
                var random = packet.AsSpan(Step + 5, 4).ToArray();
                Array.Reverse(random);
                return [Init1([1, .. new byte[16], .. random])];
            case 2:
                // The cheapest puzzle: x = 1, n = 1 and level 1, each big-endian.
                var puzzle = new byte[1 + 64 + 64 + 4 + 100];
                puzzle[0] = 3;
                puzzle[64] = 1;
                puzzle[128] = 1;
                puzzle[132] = 1;
                return [Init1(puzzle)];
            case 4 when attempt.Line.StartsWith("error ", StringComparison.Ordinal):
                // A real server refuses later, after the key exchange, under the session's keys.
                // Answered here, at the last Init1 step, TSLib has no keys yet and takes a command
                // marked unencrypted with a MAC of all zeros.
                return [Command(0, new byte[8], attempt.Line)];
            case 4:
                return [Command(0, new byte[8], KeyExchange(packet, attempt))];
            default:
                return [];
        }
    }

    // The server sends its public key, and each side derives the same secret from its own private
    // key and the other's public one. TSLib then takes a packet marked unencrypted when its MAC is
    // the start of a hash of that secret, mixed with alpha and beta.
    private string KeyExchange(byte[] packet, Attempt attempt)
    {
        var clientInitIv = Encoding.ASCII.GetString(packet, ClientInitIv, packet.Length - ClientInitIv);
        var alpha = Value(clientInitIv, "alpha");
        var beta = RandomNumberGenerator.GetBytes(10);
        var shared = SHA1.HashData(SharedX(PublicKey(Value(clientInitIv, "omega"))));
        var mixed = new byte[20];
        for (var i = 0; i < 10; i++)
        {
            mixed[i] = (byte)(shared[i] ^ alpha[i]);
            mixed[10 + i] = (byte)(shared[10 + i] ^ beta[i]);
        }
        attempt.Mac = SHA1.HashData(mixed)[..8];
        return $"initivexpand alpha={Escape(alpha)} beta={Escape(beta)} omega={Escape(Convert.FromBase64String(_key.PublicKeyString))}";
    }

    // The x of the shared point, as 32 big-endian bytes.
    private byte[] SharedX(ECPoint clientKey)
    {
        var x = clientKey.Multiply(_key.PrivateKey).Normalize().AffineXCoord.ToBigInteger().ToByteArrayUnsigned();
        return [.. new byte[32 - x.Length], .. x];
    }

    // A key as TeamSpeak sends it: a DER sequence whose third and fourth items are x and y.
    private static ECPoint PublicKey(byte[] der)
    {
        var key = (DerSequence)Asn1Object.FromByteArray(der);
        return ECNamedCurveTable.GetByName("prime256v1").Curve.CreatePoint(((DerInteger)key[2]).Value, ((DerInteger)key[3]).Value);
    }

    // Base64 values, whose slashes a command escapes.
    private static byte[] Value(string command, string name) =>
        Convert.FromBase64String(Regex.Match(command, $@"\b{name}=(\S+)").Groups[1].Value.Replace(@"\/", "/", StringComparison.Ordinal));

    private static string Escape(byte[] value) => Convert.ToBase64String(value).Replace("/", @"\/", StringComparison.Ordinal);

    // A server's packet: the MAC, its id and its type, then the data.
    private static byte[] Command(int id, byte[] mac, string line) =>
        [.. mac, (byte)(id >> 8), (byte)id, (byte)PacketType.Command | (byte)PacketFlags.Unencrypted, .. Encoding.UTF8.GetBytes(line)];

    // An Init1 packet's MAC is fixed, and its id is 101.
    private static byte[] Init1(byte[] data) =>
        [.. "TS3INIT1"u8, 0, 101, (byte)PacketType.Init1 | (byte)PacketFlags.Unencrypted, .. data];

    private sealed class Attempt(string line)
    {
        public string Line { get; } = line;

        /// <summary>The MAC of the server's packets once the keys are agreed.</summary>
        public byte[]? Mac { get; set; }

        public bool Joined { get; set; }
    }
}
