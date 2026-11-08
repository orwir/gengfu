using Gengfu.App.Speech;
using Gengfu.Core;
using Gengfu.Core.Settings;

namespace Gengfu.App;

/// <summary>
/// Makes the calls. Each call the schedule finds due goes out as a whisper, and one that can't is
/// marked not sent at once, so the list never shows it calling. A test call goes out the same way,
/// outside the schedule.
/// </summary>
/// <param name="canSend">Whether a whisper can go out now: connected, with someone to hear it.</param>
/// <param name="render">A call of the event as it sounds now; null asks for the sound Settings sets, which Test call plays.</param>
/// <param name="whisper">Sends the frames; false when they couldn't go out.</param>
internal sealed class Caller(
    CallSchedule schedule,
    Func<bool> canSend,
    Func<MatchEvent?, Task<CallClip>> render,
    Func<IReadOnlyList<byte[]>, Task<bool>> whisper)
{
    /// <summary>Whether a test call is playing.</summary>
    public bool Testing { get; private set; }

    /// <summary>Makes the calls due now. Call it on every tick, before reading any call's state.</summary>
    public IReadOnlyList<Task> Tick() => [.. schedule.TakeDue().Select(SendAsync)];

    /// <summary>Whispers the call once, now; false when it couldn't go out.</summary>
    public async Task<bool> TestAsync()
    {
        if (Testing || !canSend())
        {
            return false;
        }
        Testing = true;
        try
        {
            return await TrySendAsync(null);
        }
        finally
        {
            Testing = false;
        }
    }

    private async Task SendAsync(Call call)
    {
        if (!canSend() || !await TrySendAsync(call.Event))
        {
            schedule.MarkNotSent(call);
        }
    }

    // A send that fails, however it fails, is a call nobody heard.
    private async Task<bool> TrySendAsync(MatchEvent? matchEvent)
    {
        try
        {
            return await whisper((await render(matchEvent)).Frames);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return false;
        }
    }
}
