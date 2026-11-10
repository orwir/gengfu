using System.Globalization;
using Gengfu.Core;
using Gengfu.Core.Settings;

namespace Gengfu.App;

/// <summary>The frames of one call, and what they play, in the log's words.</summary>
internal sealed record CallRender(IReadOnlyList<byte[]> Frames, string Plays);

/// <summary>How a whisper went: whether anyone may have heard it, and how, in the log's words.</summary>
internal sealed record CallWhisper(bool Heard, string How);

/// <summary>
/// Makes the calls. The calls the schedule finds due for one spawn time go out as one call, in one
/// whisper. One that can't is marked not sent at once, so the list never shows it calling, and one
/// nobody heard once it has played. A test call goes out the same way, outside the schedule. Each
/// call it makes leaves a line in the log: what it played and how it went, or why it didn't go out.
/// </summary>
/// <param name="log">Takes a line for Gengfu's log.</param>
/// <param name="whyNot">Why no call can go out now, such as "not connected"; null when one can.</param>
/// <param name="render">The frames of one call of the events, at one spawn time, as it sounds now.</param>
/// <param name="whisper">Sends the frames, and says whether the server let anyone hear them.</param>
internal sealed class Caller(
    CallSchedule schedule,
    Action<string> log,
    Func<string?> whyNot,
    Func<IReadOnlyList<MatchEvent>, Task<CallRender>> render,
    Func<IReadOnlyList<byte[]>, Task<CallWhisper>> whisper)
{
    /// <summary>Whether a test call is playing.</summary>
    public bool Testing { get; private set; }

    /// <summary>
    /// The calls as they go out: those for one spawn time together, in their order, so none waits
    /// behind another for the same spawn.
    /// </summary>
    public static IEnumerable<IReadOnlyList<Call>> Joined(IEnumerable<Call> calls) =>
        calls.GroupBy(call => call.Spawn).Select(together => together.ToList());

    /// <summary>Makes the calls due now. Call it on every tick, before reading any call's state.</summary>
    public IReadOnlyList<Task> Tick() => [.. Joined(schedule.TakeDue()).Select(SendAsync)];

    /// <summary>Whispers the event's call once, now; false when it couldn't go out, or the server said it reached nobody.</summary>
    public async Task<bool> TestAsync(MatchEvent matchEvent)
    {
        if (Testing || whyNot() is not null)
        {
            return false;
        }
        Testing = true;
        try
        {
            var (heard, how) = await TrySendAsync([matchEvent]);
            log($"Test call: {Names([matchEvent])} — {how}");
            return heard;
        }
        finally
        {
            Testing = false;
        }
    }

    private async Task SendAsync(IReadOnlyList<Call> calls)
    {
        var at = MatchClock.Format(schedule.Clock.GameTime);
        IReadOnlyList<MatchEvent> events = [.. calls.Select(call => call.Event)];
        var (heard, how) = whyNot() is { } why ? (false, "not sent: " + why) : await TrySendAsync(events);
        log($"Call for {MatchClock.Format(calls[0].Spawn)} at {at}: {Names(events)} — {how}");
        if (!heard)
        {
            foreach (var call in calls)
            {
                schedule.MarkNotSent(call);
            }
        }
    }

    // A send that fails, however it fails, is a call nobody heard.
    private async Task<(bool Heard, string How)> TrySendAsync(IReadOnlyList<MatchEvent> events)
    {
        try
        {
            var rendered = await render(events);
            var whispered = await whisper(rendered.Frames);
            return (whispered.Heard, $"played {rendered.Plays} — {whispered.How}");
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return (false, "not sent, after an error:" + Environment.NewLine + exception);
        }
    }

    // The log is in English whatever the app's language, the jungle's name too.
    private static string Names(IReadOnlyList<MatchEvent> events) =>
        string.Join(", ", events.Select(matchEvent => Copy.EventName(matchEvent, CultureInfo.InvariantCulture)));
}
