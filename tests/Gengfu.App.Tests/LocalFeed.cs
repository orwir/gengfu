using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Velopack;
using Velopack.Locators;
using Velopack.Logging;
using Velopack.Sources;

namespace Gengfu.App.Tests;

/// <summary>
/// A release feed in a folder of the test's own, read through Velopack's file source as an installed
/// Gengfu reads GitHub's. A look or a download can be held, or a look made to fail, as GitHub can be
/// slow or out of reach.
/// </summary>
internal sealed class LocalFeed : IUpdateSource, IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "gengfu-tests", Guid.NewGuid().ToString("N"));
    private readonly SimpleFileSource _files;

    public LocalFeed()
    {
        Directory.CreateDirectory(Folder);
        Directory.CreateDirectory(Packages);
        _files = new SimpleFileSource(new DirectoryInfo(Folder));
    }

    /// <summary>Where the installed copy keeps the versions it downloaded.</summary>
    public string Packages => Path.Combine(_root, "packages");

    /// <summary>While set and unfinished, a look waits for it.</summary>
    public TaskCompletionSource? Hold { get; set; }

    /// <summary>While set and unfinished, a download waits for it.</summary>
    public TaskCompletionSource? HoldDownload { get; set; }

    public bool Fails { get; set; }

    private string Folder => Path.Combine(_root, "feed");

    /// <summary>Lists these versions. Each package is a few bytes and their hashes, all a download checks.</summary>
    public void Publish(params string[] versions)
    {
        var assets = new List<string>();
        foreach (var version in versions)
        {
            var name = $"Gengfu-{version}-full.nupkg";
            var bytes = Encoding.UTF8.GetBytes("Gengfu " + version);
            File.WriteAllBytes(Path.Combine(Folder, name), bytes);
            assets.Add($$"""{"PackageId":"Gengfu","Version":"{{version}}","Type":"Full","FileName":"{{name}}","SHA1":"{{Convert.ToHexString(SHA1.HashData(bytes))}}","SHA256":"{{Convert.ToHexString(SHA256.HashData(bytes))}}","Size":{{bytes.Length}}}""");
        }
        File.WriteAllText(Path.Combine(Folder, "releases.win.json"), $$"""{"Assets":[{{string.Join(",", assets)}}]}""");
    }

    /// <summary>Velopack's manager for an installed copy of <paramref name="installed"/> that reads this feed.</summary>
    public UpdateManager Manager(string installed) => new(this, null, new TestVelopackLocator("Gengfu", installed, Packages, null));

    public async Task<VelopackAssetFeed> GetReleaseFeed(IVelopackLogger logger, string? appId, string channel, Guid? stagingId = null, VelopackAsset? latestLocalRelease = null)
    {
        if (Hold is { } hold)
        {
            await hold.Task;
        }
        if (Fails)
        {
            throw new HttpRequestException("No connection.");
        }
        return await _files.GetReleaseFeed(logger, appId, channel, stagingId, latestLocalRelease);
    }

    public async Task DownloadReleaseEntry(IVelopackLogger logger, VelopackAsset releaseEntry, string localFile, Action<int> progress, CancellationToken cancelToken = default)
    {
        if (HoldDownload is { } hold)
        {
            await hold.Task;
        }
        await _files.DownloadReleaseEntry(logger, releaseEntry, localFile, progress, cancelToken);
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);
}
