using Gengfu.Core;
using Gengfu.Core.Settings;

namespace Gengfu.App;

/// <summary>
/// Makes the calls. The calls the schedule finds due for one spawn time go out as one call, in one
/// whisper. One that can't is marked not sent at once, so the list never shows it calling, and one
/// nobody heard once it has played. A test call goes out the same way, outside the schedule.
/// </summary>
/// <param name="canSend">Whether a whisper can go out now: connected, with someone to hear it.</param>
/// <param name="render">The frames of one call of the events, at one spawn time, as it sounds now.</param>
/// <param name="whisper">Sends the frames; false when they couldn't go out, or the server said they reached nobody.</param>
internal sealed class Caller(
    CallSchedule schedule,
    Func<bool> canSend,
    Func<IReadOnlyList<MatchEvent>, Task<IReadOnlyList<byte[]>>> render,
    Func<IReadOnlyList<byte[]>, Task<bool>> whisper)
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
        if (Testing || !canSend())
        {
            return false;
        }
        Testing = true;
        try
        {
            return await TrySendAsync([matchEvent]);
        }
        finally
        {
            Testing = false;
        }
    }

    private async Task SendAsync(IReadOnlyList<Call> calls)
    {
        if (!canSend() || !await TrySendAsync([.. calls.Select(call => call.Event)]))
        {
            foreach (var call in calls)
            {
                schedule.MarkNotSent(call);
            }
        }
    }

    // A send that fails, however it fails, is a call nobody heard.
    private async Task<bool> TrySendAsync(IReadOnlyList<MatchEvent> events)
    {
        try
        {
            return await whisper(await render(events));
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return false;
        }
    }
}
