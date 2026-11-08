using Gengfu.Core;
using Velopack;
using Velopack.Locators;
using Velopack.Sources;

namespace Gengfu.App;

/// <summary>Where keeping Gengfu current stands, for the line under the version and the update notice.</summary>
internal abstract record UpdateState
{
    private UpdateState()
    {
    }

    /// <summary>Setup didn't install this copy, such as a developer build: there is nothing to update.</summary>
    public sealed record Off : UpdateState;

    /// <summary>The first look since Gengfu started is under way.</summary>
    public sealed record Checking : UpdateState;

    public sealed record UpToDate : UpdateState;

    public sealed record Downloading(string Version) : UpdateState;

    /// <summary>Downloaded: it applies the next time Gengfu starts.</summary>
    public sealed record Ready(string Version) : UpdateState;

    /// <summary>The last look failed, offline perhaps; the next one comes on schedule.</summary>
    public sealed record Failed : UpdateState;
}

/// <summary>
/// Keeps an installed Gengfu current: it looks on GitHub at start and every 4 hours, on the thread
/// pool, and downloads a new version quietly. Velopack applies it the next time Gengfu starts, so
/// nothing restarts on its own (ADR-0006).
/// </summary>
internal sealed class Updates
{
    private const string Repository = "https://github.com/orwir/gengfu";

    private static readonly TimeSpan LookEvery = TimeSpan.FromHours(4);

    private readonly ITimeSource _time;
    private readonly UpdateManager? _manager;
    private Task _look = Task.CompletedTask;
    private TimeSpan _lookAt;
    private VelopackAsset? _ready;
    private volatile UpdateState _state;

    /// <param name="manager">Velopack's, for a copy Setup installed; null for any other.</param>
    public Updates(ITimeSource time, UpdateManager? manager)
    {
        _time = time;
        _manager = manager;
        _lookAt = time.Now;
        _state = manager is null ? new UpdateState.Off() : new UpdateState.Checking();
    }

    /// <summary>Read on every tick. A later look keeps the last answer shown until it has its own.</summary>
    public UpdateState State => _state;

    /// <summary>
    /// Velopack's manager, when Setup installed this copy. A test build follows test builds, so an
    /// installed release candidate moves on to the release, and a release never sees a test build.
    /// </summary>
    public static UpdateManager? Installed()
    {
        // VelopackApp, run first in Program.Main, finds the install; a host that skips it has none.
        if (!VelopackLocator.IsCurrentSet)
        {
            return null;
        }
        var manager = new UpdateManager(new GithubSource(Repository, null, AppVersion.IsPrerelease(AppVersion.Current)));
        return manager.IsInstalled ? manager : null;
    }

    /// <summary>Starts a look once one is due; the window calls it on every tick. The task ends with the look.</summary>
    public Task Tick()
    {
        if (_manager is null || !_look.IsCompleted || _ready is not null || _time.Now < _lookAt)
        {
            return Task.CompletedTask;
        }
        var manager = _manager;
        return _look = Task.Run(() => LookAsync(manager));
    }

    /// <summary>
    /// Leaves the downloaded version to Velopack's updater, which applies it once Gengfu has closed
    /// and then opens Gengfu again. False when nothing is ready or the updater couldn't start; the
    /// version still applies at the next start.
    /// </summary>
    public bool ApplyOnClose()
    {
        if (_manager is null || _ready is null)
        {
            return false;
        }
        try
        {
            _manager.WaitExitThenApplyUpdates(_ready, silent: false, restart: true);
            return true;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return false;
        }
    }

    private async Task LookAsync(UpdateManager manager)
    {
        try
        {
            if (await manager.CheckForUpdatesAsync() is not { } update)
            {
                _state = new UpdateState.UpToDate();
                return;
            }
            var version = update.TargetFullRelease.Version.ToString();
            _state = new UpdateState.Downloading(version);
            await manager.DownloadUpdatesAsync(update);
            _ready = update.TargetFullRelease;
            _state = new UpdateState.Ready(version);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            _state = new UpdateState.Failed();
        }
        finally
        {
            _lookAt = _time.Now + LookEvery;
        }
    }
}
