using Gengfu.Core;
using Gengfu.Core.Settings;

namespace Gengfu.App.Tests;

public sealed class CallerTests
{
    private readonly FakeTimeSource _time = new();
    private readonly MatchClock _clock;
    private readonly IReadOnlyList<byte[]> _frames = [[1], [2], [3]];
    private readonly List<IReadOnlyList<byte[]>> _whispered = [];
    private readonly List<IReadOnlyList<MatchEvent>> _rendered = [];
    private readonly List<string> _logged = [];
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

        Assert.Equal(new[] { _frames }, _whispered);
        Assert.Equal(CallState.Calling, _schedule.StateOf(First));
    }

    [Fact]
    public async Task Calls_for_one_spawn_time_go_out_as_one_whisper_on_the_tick_they_come_due()
    {
        var boss = new MatchEvent { Name = "Boss", Spawns = [TimeSpan.FromMinutes(25)] };
        _schedule = new CallSchedule(_clock, [MatchEvent.Jungle, boss], CallSchedule.DefaultLead);
        var caller = NewCaller();
        _clock.Start();
        RunTo(First.At);

        var sends = caller.Tick();

        // Whispered within the tick: nothing waits behind it.
        Assert.Equal(new[] { _frames }, _whispered);
        Assert.Equal(new[] { new[] { MatchEvent.Jungle, boss } }, _rendered);
        await Task.WhenAll(sends);
        var together = _schedule.Calls.Where(call => call.Spawn == TimeSpan.FromMinutes(25)).ToList();
        Assert.Equal(2, together.Count);
        Assert.All(together, call => Assert.Equal(CallState.Calling, _schedule.StateOf(call)));

        _time.Advance(CallSchedule.CallingFor);
        Assert.All(together, call => Assert.Equal(CallState.Called, _schedule.StateOf(call)));
    }

    [Fact]
    public async Task A_call_nobody_heard_or_that_couldn_t_go_out_marks_every_event_s_call_in_it_not_sent()
    {
        var boss = new MatchEvent { Name = "Boss", Spawns = [TimeSpan.FromMinutes(25)] };
        foreach (var canSend in new[] { true, false })
        {
            _canSend = canSend;
            _whisper = _ => Task.FromResult(false);
            _schedule = new CallSchedule(_clock, [MatchEvent.Jungle, boss], CallSchedule.DefaultLead);
            var caller = NewCaller();
            _clock.Start();
            RunTo(First.At);

            await Task.WhenAll(caller.Tick());

            Assert.Equal(new[] { CallState.NotSent, CallState.NotSent }, _schedule.Calls.Take(2).Select(_schedule.StateOf));
            _clock.Stop();
        }
    }

    [Fact]
    public async Task Calls_for_other_spawn_times_due_on_one_tick_go_out_one_after_another()
    {
        // The first tick finds the clock at 25:20, inside the calls of 25:10 and of 25:00, as after a correction.
        var boss = new MatchEvent { Name = "Boss", Spawns = [TimeSpan.FromSeconds((25 * 60) + 10)] };
        _schedule = new CallSchedule(_clock, [MatchEvent.Jungle, boss], CallSchedule.DefaultLead);
        var caller = NewCaller();
        _clock.Start();
        RunTo(TimeSpan.FromSeconds((25 * 60) + 20));

        await Task.WhenAll(caller.Tick());

        Assert.Equal(new[] { new[] { boss }, new[] { MatchEvent.Jungle } }, _rendered);
        Assert.Equal(2, _whispered.Count);
        // Late, so the log gives the game time each went out at.
        Assert.Equal(
            [
                "Call for 25:10 at 25:20: Boss — played “Boss” — sent to the GvG group",
                "Call for 25:00 at 25:20: Jungle — played “Jungle” — sent to the GvG group",
            ],
            _logged);
    }

    [Fact]
    public async Task Each_call_leaves_a_line_in_the_log_saying_what_it_played_and_how_it_went()
    {
        var boss = new MatchEvent { Name = "Boss", Spawns = [TimeSpan.FromMinutes(25)] };
        _schedule = new CallSchedule(_clock, [MatchEvent.Jungle, boss], CallSchedule.DefaultLead);
        var caller = NewCaller();
        _clock.Start();
        RunTo(First.At);
        await Task.WhenAll(caller.Tick());

        _whisper = _ => Task.FromResult(false);
        RunTo(TimeSpan.FromSeconds((20 * 60) + 30));
        await Task.WhenAll(caller.Tick());
        await caller.TestAsync(boss);

        Assert.Equal(
            [
                "Call for 25:00 at 25:30: Jungle, Boss — played “Jungle and Boss” — sent to the GvG group",
                "Call for 20:00 at 20:30: Jungle — played “Jungle” — nobody heard it",
                "Test call: Boss — played “Boss” — nobody heard it",
            ],
            _logged);
    }

    [Fact]
    public async Task A_call_that_can_t_go_out_says_why_in_the_log_and_one_that_fails_leaves_its_error()
    {
        _canSend = false;
        var caller = NewCaller();
        _clock.Start();
        RunTo(First.At);
        caller.Tick();

        _canSend = true;
        _whisper = _ => throw new InvalidOperationException("The connection dropped.");
        RunTo(TimeSpan.FromSeconds((20 * 60) + 30));
        await Task.WhenAll(caller.Tick());

        Assert.Equal("Call for 25:00 at 25:30: Jungle — not sent: not connected", _logged[0]);
        var failed = _logged[1].Split(Environment.NewLine);
        Assert.Equal("Call for 20:00 at 20:30: Jungle — not sent, after an error:", failed[0]);
        Assert.Equal("System.InvalidOperationException: The connection dropped.", failed[1]);
        Assert.StartsWith("   at ", failed[2], StringComparison.Ordinal);
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
    public async Task A_call_plays_its_event_s_sound_and_a_test_call_the_one_of_the_event_tested()
    {
        var boss = new MatchEvent { Name = "Boss", Spawns = [TimeSpan.FromMinutes(26)], Text = "Boss in thirty seconds" };
        _schedule = new CallSchedule(_clock, [MatchEvent.Jungle, boss], CallSchedule.DefaultLead);
        var caller = NewCaller();
        _clock.Start();
        RunTo(First.At);

        await Task.WhenAll(caller.Tick());
        await caller.TestAsync(MatchEvent.Jungle);

        Assert.Equal(new[] { new[] { boss }, new[] { MatchEvent.Jungle } }, _rendered);
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

        var test = caller.TestAsync(MatchEvent.Jungle);
        Assert.True(caller.Testing);
        Assert.False(await caller.TestAsync(MatchEvent.Jungle));
        Assert.Single(_whispered);

        playing.SetResult(true);
        Assert.True(await test);
        Assert.False(caller.Testing);
    }

    [Fact]
    public async Task A_test_call_needs_something_to_go_out_through()
    {
        _canSend = false;

        Assert.False(await NewCaller().TestAsync(MatchEvent.Jungle));
        Assert.Empty(_whispered);
    }

    // Each call plays its events' names; a whisper heard went to the GvG group.
    private Caller NewCaller() =>
        new(
            _schedule,
            _logged.Add,
            () => _canSend ? null : "not connected",
            events =>
            {
                _rendered.Add(events);
                return Task.FromResult(new CallRender(_frames, $"“{string.Join(" and ", events.Select(matchEvent => matchEvent.Name ?? "Jungle"))}”"));
            },
            frames =>
            {
                _whispered.Add(frames);
                // Called here, so a whisper that throws as it starts still throws to the caller.
                return Went(_whisper(frames));

                static async Task<CallWhisper> Went(Task<bool> heard) =>
                    await heard ? new CallWhisper(true, "sent to the GvG group") : new CallWhisper(false, "nobody heard it");
            });

    // Lets the running clock count down, uncorrected, until it shows the given game time.
    private void RunTo(TimeSpan gameTime) => _time.Advance(_clock.GameTime - gameTime);
}
