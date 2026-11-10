using System.IO;
using System.Text.RegularExpressions;

namespace Gengfu.App.Tests;

// A real log, in a folder of the test's own.
public sealed partial class LogTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "gengfu-tests", Guid.NewGuid().ToString("N"));
    private readonly Log _log;

    public LogTests() => _log = new Log(_folder);

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public void Each_entry_is_on_disk_at_once_on_a_line_of_its_own_after_the_local_date_and_time()
    {
        var before = DateTime.Now;

        _log.Write("Gengfu closed");

        var line = Assert.Single(File.ReadAllLines(_log.FilePath));
        var stamped = Stamped().Match(line);
        Assert.True(stamped.Success, line);
        Assert.Equal("Gengfu closed", stamped.Groups["entry"].Value);
        Assert.InRange(DateTime.Parse(stamped.Groups["at"].Value, System.Globalization.CultureInfo.InvariantCulture), before.AddSeconds(-1), DateTime.Now);
    }

    [Fact]
    public void Once_a_file_would_pass_the_cap_the_log_starts_afresh_and_keeps_only_the_full_one_before()
    {
        // Ten of these fit under the cap; an eleventh would pass it.
        var entry = new string('x', 100_000);
        for (var i = 0; i < 10; i++)
        {
            _log.Write($"first {i} {entry}");
        }
        Assert.False(File.Exists(_log.OldFilePath));

        for (var i = 0; i < 11; i++)
        {
            _log.Write($"second {i} {entry}");
        }

        Assert.Equal(Enumerable.Range(0, 10).Select(i => $"second {i} x"), File.ReadAllLines(_log.OldFilePath).Select(line => Entry(line)[..10]));
        Assert.StartsWith("second 10 x", Entry(Assert.Single(File.ReadAllLines(_log.FilePath))), StringComparison.Ordinal);
        Assert.True(new FileInfo(_log.OldFilePath).Length <= Log.Cap);
        Assert.Equal(2, Directory.GetFiles(_folder).Length);
    }

    [Fact]
    public void The_Windows_user_s_profile_folder_shows_as_USERPROFILE()
    {
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        _log.Write($"Access to the path '{profile.ToUpperInvariant()}\\Music\\boss.mp3' is denied.");

        var line = Assert.Single(File.ReadAllLines(_log.FilePath));
        Assert.EndsWith(@"Access to the path '%USERPROFILE%\Music\boss.mp3' is denied.", line, StringComparison.Ordinal);
    }

    [Fact]
    public void An_entry_Windows_won_t_let_Gengfu_write_is_lost_and_Gengfu_goes_on()
    {
        _log.Write("before");

        using (new FileStream(_log.FilePath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            _log.Write("while another program holds the file");
        }
        _log.Write("after");

        Assert.Equal(["before", "after"], File.ReadAllLines(_log.FilePath).Select(Entry));
    }

    [Fact]
    public async Task Entries_from_many_threads_land_whole()
    {
        var writers = Enumerable.Range(0, 4).Select(writer => Task.Run(() =>
        {
            for (var i = 0; i < 100; i++)
            {
                _log.Write($"writer {writer} entry {i}");
            }
        }));

        await Task.WhenAll(writers);

        var lines = File.ReadAllLines(_log.FilePath);
        Assert.Equal(400, lines.Length);
        Assert.All(lines, line => Assert.Matches(Stamped(), line));
        Assert.Equal(400, lines.Distinct().Count());
    }

    private static string Entry(string line) => Stamped().Match(line).Groups["entry"].Value;

    [GeneratedRegex(@"^(?<at>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3})  (?<entry>.*)$")]
    private static partial Regex Stamped();
}
