using System.Collections.Concurrent;
using Gengfu.Core;

namespace Gengfu.TeamSpeak;

/// <summary>
/// Sends calls as voice: 20 ms Opus frames in real time, then an empty frame that ends the talk.
/// Calls play one at a time on the sender's own thread, and one that arrives during another waits
/// its turn. Each frame goes out up to <see cref="Lead"/> before it plays, so Windows' coarse
/// sleep still keeps listeners' buffers fed without raising the system timer resolution.
/// </summary>
public sealed class VoiceSender : IDisposable
{
    public static TimeSpan FrameLength { get; } = TimeSpan.FromMilliseconds(20);

    public static TimeSpan Lead { get; } = TimeSpan.FromMilliseconds(60);

    private readonly ITimeSource _time;
    private readonly Action<TimeSpan> _sleep;
    private readonly BlockingCollection<Call> _calls = new();
    private readonly Thread _thread;

    public VoiceSender()
        : this(new SystemTimeSource(), wait => Thread.Sleep((int)Math.Ceiling(wait.TotalMilliseconds)))
    {
    }

    internal VoiceSender(ITimeSource time, Action<TimeSpan> sleep)
    {
        _time = time;
        _sleep = sleep;
        _thread = new Thread(Run) { IsBackground = true, Name = "Gengfu voice", Priority = ThreadPriority.AboveNormal };
        _thread.Start();
    }

    /// <summary>Queues a call; the task completes once its last frame and the end of talk are sent.</summary>
    public Task PlayAsync(IReadOnlyList<byte[]> frames, Action<byte[]> send)
    {
        var call = new Call(frames, send);
        _calls.Add(call);
        return call.Sent.Task;
    }

    /// <summary>Sends the calls already queued, then stops the thread.</summary>
    public void Dispose()
    {
        _calls.CompleteAdding();
        _thread.Join();
        _calls.Dispose();
    }

    private void Run()
    {
        // When the previous call ends at the listeners; the next one starts no earlier.
        var free = TimeSpan.Zero;
        foreach (var call in _calls.GetConsumingEnumerable())
        {
            var start = _time.Now > free ? _time.Now : free;
            free = start + (FrameLength * call.Frames.Count);
            try
            {
                for (var i = 0; i <= call.Frames.Count; i++)
                {
                    WaitUntil(start + (FrameLength * i) - Lead);
                    call.Send(i < call.Frames.Count ? call.Frames[i] : []);
                }
                call.Sent.SetResult();
            }
            catch (Exception ex)
            {
                // The thread must outlive a failed send, or every later call would be lost.
                call.Sent.SetException(ex);
            }
        }
    }

    private void WaitUntil(TimeSpan due)
    {
        for (var wait = due - _time.Now; wait > TimeSpan.Zero; wait = due - _time.Now)
        {
            _sleep(wait);
        }
    }

    private sealed class Call(IReadOnlyList<byte[]> frames, Action<byte[]> send)
    {
        public IReadOnlyList<byte[]> Frames { get; } = frames;

        public Action<byte[]> Send { get; } = send;

        public TaskCompletionSource Sent { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
