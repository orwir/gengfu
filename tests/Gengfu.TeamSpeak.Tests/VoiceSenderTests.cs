namespace Gengfu.TeamSpeak.Tests;

public sealed class VoiceSenderTests : IDisposable
{
    private readonly FakeTimeSource _time = new();
    private readonly VoiceSender _sender;
    private readonly List<(TimeSpan At, byte[] Frame)> _sent = [];

    // The sender sleeps by advancing the fake clock, so a call plays out at once and to the tick.
    public VoiceSenderTests() => _sender = new VoiceSender(_time, _time.Advance);

    public void Dispose() => _sender.Dispose();

    [Fact]
    public async Task Each_frame_goes_out_a_lead_ahead_of_its_turn_and_an_empty_frame_ends_the_talk()
    {
        var start = _time.Now;
        var frames = Frames(10, call: 1);

        await _sender.PlayAsync(frames, Record);

        Assert.Equal([.. frames, []], _sent.Select(sent => sent.Frame));
        Assert.Equal(Leaves(plays: start, frames: 11, reached: start), _sent.Select(sent => sent.At));
    }

    [Fact]
    public async Task A_call_that_comes_during_another_plays_once_the_first_has_played_out()
    {
        var start = _time.Now;
        var firstEnds = start + (VoiceSender.FrameLength * 10);

        var first = _sender.PlayAsync(Frames(10, call: 1), Record);
        var second = _sender.PlayAsync(Frames(5, call: 2), Record);
        await Task.WhenAll(first, second);

        Assert.Equal(
            Leaves(plays: start, frames: 11, reached: start)
                .Concat(Leaves(plays: firstEnds, frames: 6, reached: firstEnds - VoiceSender.Lead)),
            _sent.Select(sent => sent.At));
        Assert.Equal(
            [.. Frames(10, call: 1), [], .. Frames(5, call: 2), []],
            _sent.Select(sent => sent.Frame));
    }

    [Fact]
    public async Task A_send_that_fails_fails_its_own_call_and_the_next_call_still_plays()
    {
        var failing = _sender.PlayAsync(Frames(3, call: 1), _ => throw new InvalidOperationException("socket closed"));
        var next = _sender.PlayAsync(Frames(3, call: 2), Record);

        await Assert.ThrowsAsync<InvalidOperationException>(() => failing);
        await next;
        Assert.Equal([.. Frames(3, call: 2), []], _sent.Select(sent => sent.Frame));
    }

    private static byte[][] Frames(int count, byte call) =>
        [.. Enumerable.Range(0, count).Select(index => new[] { call, (byte)index })];

    // Frame i plays at plays + i × 20 ms and leaves one lead earlier, but not before the sender reaches it.
    private static IEnumerable<TimeSpan> Leaves(TimeSpan plays, int frames, TimeSpan reached) =>
        Enumerable.Range(0, frames).Select(index =>
        {
            var leaves = plays + (VoiceSender.FrameLength * index) - VoiceSender.Lead;
            return leaves > reached ? leaves : reached;
        });

    private void Record(byte[] frame)
    {
        lock (_sent)
        {
            _sent.Add((_time.Now, frame));
        }
    }
}
