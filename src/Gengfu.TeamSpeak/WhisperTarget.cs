using TSLib;
using TSLib.Full;

namespace Gengfu.TeamSpeak;

/// <summary>
/// Who hears the calls: everyone in a server group, in whatever channel they are, or everyone in
/// the chosen channels. Ids come from <see cref="TeamSpeakConnection.ListServerGroupsAsync"/> and
/// <see cref="TeamSpeakConnection.ListChannelsAsync"/>.
/// </summary>
public abstract record WhisperTarget
{
    private WhisperTarget()
    {
    }

    internal abstract void Send(TsFullClient client, byte[] frame);

    public sealed record ServerGroup(ulong Id) : WhisperTarget
    {
        internal override void Send(TsFullClient client, byte[] frame) =>
            client.SendAudioGroupWhisper(frame, Codec.OpusMusic, GroupWhisperType.ServerGroup, GroupWhisperTarget.AllChannels, Id);
    }

    public sealed record Channels(IReadOnlyList<ulong> Ids) : WhisperTarget
    {
        // The same channels are the same listeners; a record alone would compare the two lists as objects.
        public bool Equals(Channels? other) => other is not null && Ids.SequenceEqual(other.Ids);

        public override int GetHashCode() => Ids.Aggregate(0, HashCode.Combine);

        internal override void Send(TsFullClient client, byte[] frame) =>
            client.SendAudioWhisper(frame, Codec.OpusMusic, [.. Ids.Select(id => new ChannelId(id))], []);
    }
}
