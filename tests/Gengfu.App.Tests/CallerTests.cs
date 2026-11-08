using Gengfu.App.Speech;
using Gengfu.Core;
using Gengfu.Core.Settings;

namespace Gengfu.App.Tests;

public sealed class CallerTests
{
    private readonly FakeTimeSource _time = new();
    private readonly MatchClock _clock;
    private readonly CallClip _clip = new([[1], [2], [3]]);
    private readonly List<IReadOnlyList<byte[]>> _whispered = [];
    private readonly List<MatchEvent?> _rendered = [];
    private CallSchedule _schedule;
    private bool _canSend = true;
    private Func<IReadOnlyList<byte[]>, Task<bool>> _whisper;

    public CallerTests()
    {
        _clock = new MatchClock(_time);
        _schedule = new CallSchedule(_clock, [MatchEvent.Jungle], CallSchedule.DefaultLead);
        _whisper = _ => Task.FromResult(true);
    }

    private Call First => _schedule.Calls[0];

    [Fact]
    public async Task A_due_call_goes_out_as_the_call_sounds_now()
    {
        var caller = NewCaller();
        _clock.Start();
        RunTo(First.At);

        await Task.WhenAll(caller.Tick());

        Assert.Equal(new[] { _clip.Frames }, _whispered);
        Assert.Equal(CallState.Calling, _schedule.StateOf(First));
    }

    [Fact]
    public void A_call_due_when_nothing_can_go_out_is_not_sent_before_the_list_could_show_it_calling()
    {
        _canSend = false;
        var caller = NewCaller();
        _clock.Start();
        RunTo(First.At);

        var sends = caller.Tick();

        Assert.All(sends, send => Assert.True(send.IsCompleted));
        Assert.Equal(CallState.NotSent, _schedule.StateOf(First));
        Assert.Empty(_rendered);
        Assert.Empty(_whispered);
    }

    [Fact]
    public async Task A_whisper_that_fails_however_it_fails_leaves_the_call_not_sent()
    {
        foreach (var failure in new Func<IReadOnlyList<byte[]>, Task<bool>>[]
        {
            _ => Task.FromResult(false),
            _ => throw new InvalidOperationException("The connection dropped."),
            _ => Task.FromException<bool>(new InvalidOperationException("The send failed.")),
        })
        {
            _whisper = failure;
            var caller = NewCaller();
            _clock.Start();
            RunTo(First.At);

            await Task.WhenAll(caller.Tick());

            Assert.Equal(CallState.NotSent, _schedule.StateOf(First));
            _clock.Stop();
            _schedule.TakeDue();
        }
    }

    [Fact]
    public async Task A_call_plays_its_event_s_sound_and_a_test_call_the_one_Settings_sets()
    {
        var boss = new MatchEvent { Name = "Boss", Spawns = [TimeSpan.FromMinutes(26)], Text = "Boss in thirty seconds" };
        _schedule = new CallSchedule(_clock, [MatchEvent.Jungle, boss], CallSchedule.DefaultLead);
        var caller = NewCaller();
        _clock.Start();
        RunTo(First.At);

        await Task.WhenAll(caller.Tick());
        await caller.TestAsync();

        Assert.Equal(new[] { boss, null }, _rendered);
    }

    [Fact]
    public async Task A_call_that_fails_after_the_events_change_shows_as_not_sent_where_it_now_stands()
    {
        var playing = new TaskCompletionSource<bool>();
        _whisper = _ => playing.Task;
        var caller = NewCaller();
        _clock.Start();
        RunTo(First.At);
        var sends = caller.Tick();

        _schedule.ChangeEvents([new MatchEvent { Name = "Boss", Spawns = [TimeSpan.FromMinutes(25.25)] }, MatchEvent.Jungle]);
        playing.SetResult(false);
        await Task.WhenAll(sends);

        Assert.Equal(new[] { CallState.Next, CallState.NotSent }, _schedule.Calls.Take(2).Select(_schedule.StateOf));
    }

    [Fact]
    public async Task A_test_call_goes_out_once_and_shows_while_it_plays()
    {
        var playing = new TaskCompletionSource<bool>();
        _whisper = _ => playing.Task;
        var caller = NewCaller();

        var test = caller.TestAsync();
        Assert.True(caller.Testing);
        Assert.False(await caller.TestAsync());
        Assert.Single(_whispered);

        playing.SetResult(true);
        Assert.True(await test);
        Assert.False(caller.Testing);
    }

    [Fact]
    public async Task A_test_call_needs_something_to_go_out_through()
    {
        _canSend = false;

        Assert.False(await NewCaller().TestAsync());
        Assert.Empty(_whispered);
    }

    private Caller NewCaller() =>
        new(
            _schedule,
            () => _canSend,
            matchEvent =>
            {
                _rendered.Add(matchEvent);
                return Task.FromResult(_clip);
            },
            frames =>
            {
                _whispered.Add(frames);
                return _whisper(frames);
            });

    // Lets the running clock count down, uncorrected, until it shows the given game time.
    private void RunTo(TimeSpan gameTime) => _time.Advance(_clock.GameTime - gameTime);
}
