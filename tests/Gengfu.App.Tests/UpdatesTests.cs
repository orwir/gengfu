using System.IO;

namespace Gengfu.App.Tests;

// The feed is a real folder, read through Velopack's own code; only time is faked.
public sealed class UpdatesTests : IDisposable
{
    private readonly FakeTimeSource _time = new();
    private readonly LocalFeed _feed = new();

    public void Dispose() => _feed.Dispose();

    [Fact]
    public async Task A_copy_Setup_did_not_install_never_looks()
    {
        var updates = new Updates(_time, null);

        await updates.Tick();

        Assert.IsType<UpdateState.Off>(updates.State);
        Assert.False(updates.ApplyOnClose());
    }

    [Fact]
    public async Task It_looks_at_start_and_says_so_until_it_knows()
    {
        _feed.Publish("0.1.0");
        _feed.Hold = Held();
        var updates = new Updates(_time, _feed.Manager("0.1.0"));

        var look = updates.Tick();
        Assert.IsType<UpdateState.Checking>(updates.State);

        _feed.Hold.SetResult();
        await look;
        Assert.IsType<UpdateState.UpToDate>(updates.State);
    }

    [Fact]
    public async Task A_newer_version_downloads_quietly_and_waits_for_the_next_start()
    {
        _feed.Publish("0.1.0", "0.1.1");
        var updates = new Updates(_time, _feed.Manager("0.1.0"));

        await updates.Tick();

        Assert.Equal(new UpdateState.Ready("0.1.1"), updates.State);
        Assert.True(File.Exists(Path.Combine(_feed.Packages, "Gengfu-0.1.1-full.nupkg")));
        // With a version ready there is nothing more to look for until it applies.
        _time.Advance(TimeSpan.FromHours(5));
        Assert.Same(Task.CompletedTask, updates.Tick());
    }

    [Fact]
    public async Task The_version_shows_as_downloading_while_it_comes_down()
    {
        _feed.Publish("0.1.0", "0.1.1");
        _feed.HoldDownload = Held();
        var updates = new Updates(_time, _feed.Manager("0.1.0"));

        var look = updates.Tick();
        for (var tries = 0; tries < 200 && updates.State is not UpdateState.Downloading; tries++)
        {
            await Task.Delay(25);
        }
        Assert.Equal(new UpdateState.Downloading("0.1.1"), updates.State);

        _feed.HoldDownload.SetResult();
        await look;
        Assert.Equal(new UpdateState.Ready("0.1.1"), updates.State);
    }

    [Fact]
    public async Task It_looks_again_every_four_hours_and_shows_the_last_answer_meanwhile()
    {
        _feed.Publish("0.1.0");
        var updates = new Updates(_time, _feed.Manager("0.1.0"));
        await updates.Tick();
        _feed.Publish("0.1.0", "0.1.1");

        _time.Advance(TimeSpan.FromHours(4) - TimeSpan.FromSeconds(1));
        Assert.Same(Task.CompletedTask, updates.Tick());

        _time.Advance(TimeSpan.FromSeconds(1));
        _feed.Hold = Held();
        var look = updates.Tick();
        Assert.IsType<UpdateState.UpToDate>(updates.State);

        _feed.Hold.SetResult();
        await look;
        Assert.Equal(new UpdateState.Ready("0.1.1"), updates.State);
    }

    [Fact]
    public async Task A_failed_look_says_so_and_the_next_comes_on_schedule()
    {
        _feed.Publish("0.1.0");
        _feed.Fails = true;
        var updates = new Updates(_time, _feed.Manager("0.1.0"));

        await updates.Tick();
        Assert.IsType<UpdateState.Failed>(updates.State);

        _feed.Fails = false;
        Assert.Same(Task.CompletedTask, updates.Tick());
        _time.Advance(TimeSpan.FromHours(4));
        await updates.Tick();
        Assert.IsType<UpdateState.UpToDate>(updates.State);
    }

    [Fact]
    public async Task A_look_under_way_is_never_joined_by_another()
    {
        _feed.Publish("0.1.0");
        _feed.Hold = Held();
        var updates = new Updates(_time, _feed.Manager("0.1.0"));

        var look = updates.Tick();
        _time.Advance(TimeSpan.FromHours(5));
        Assert.Same(Task.CompletedTask, updates.Tick());
        Assert.Same(Task.CompletedTask, updates.Tick());

        _feed.Hold.SetResult();
        await look;
        Assert.Equal(1, _feed.Looks);
    }

    [Fact]
    public void A_test_build_looks_at_test_builds_too_and_a_release_only_at_releases()
    {
        Assert.True(Updates.Source("0.1.0-rc.1").Prerelease);
        Assert.False(Updates.Source("0.1.0").Prerelease);
        Assert.Equal("https://github.com/orwir/gengfu", Updates.Source("0.1.0").RepoUri.ToString().TrimEnd('/'));
    }

    [Fact]
    public async Task Restart_has_nothing_to_hand_over_until_a_version_is_ready()
    {
        _feed.Publish("0.1.0");
        var updates = new Updates(_time, _feed.Manager("0.1.0"));

        await updates.Tick();

        Assert.False(updates.ApplyOnClose());
    }

    // The look carries on in the background when the test lets it go, as it would for GitHub.
    private static TaskCompletionSource Held() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
