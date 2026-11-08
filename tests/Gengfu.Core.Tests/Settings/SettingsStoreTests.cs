using Gengfu.Core.Settings;

namespace Gengfu.Core.Tests.Settings;

// Real files, in a folder of the test's own that goes when it ends.
public sealed class SettingsStoreTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "gengfu-tests", Guid.NewGuid().ToString("N"));
    private readonly SettingsStore _store;

    public SettingsStoreTests() => _store = new SettingsStore(Path.Combine(_folder, "settings.json"));

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public void Saved_settings_load_back_the_same()
    {
        var settings = new AppSettings
        {
            Server = "teal.example.com:9608",
            Nickname = "Гэнфу",
            Listeners = ListenerKind.Channels,
            Group = new Named(1625, "ГВГ"),
            Channels = [new Named(3977, "Пуш 1"), new Named(3978, "Пуш 2")],
            ListenersServer = "teal.example.com:9608",
            Lead = TimeSpan.FromSeconds(45),
            CallText = "Лес через сорок пять секунд",
            Voice = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Speech_OneCore\Voices\Tokens\MSTTS_V110_ruRU_IrinaM",
            SoundFile = @"C:\Users\Игрок\Music\лес.mp3",
            Language = "ru",
            Events =
            [
                MatchEvent.Jungle,
                new MatchEvent { Name = "Босс", Spawns = [TimeSpan.FromMinutes(28), TimeSpan.FromMinutes(12.5)], Text = "Босс через тридцать секунд" },
            ],
        };

        Assert.True(_store.Save(settings));
        var loaded = Assert.IsType<SettingsLoad.Loaded>(new SettingsStore(_store.FilePath).Load()).Settings;

        Assert.Equal(SettingsStore.Serialize(settings), SettingsStore.Serialize(loaded));
        Assert.Equal(settings.Channels, loaded.Channels);
        Assert.Equal((settings.Server, settings.Nickname, settings.Group, settings.Lead, settings.Voice, settings.SoundFile, settings.Language),
            (loaded.Server, loaded.Nickname, loaded.Group, loaded.Lead, loaded.Voice, loaded.SoundFile, loaded.Language));
    }

    [Fact]
    public void Without_a_file_the_defaults_apply_until_a_save_makes_the_folder_and_the_file()
    {
        Assert.IsType<SettingsLoad.Missing>(_store.Load());
        Assert.Null(_store.Known);

        Assert.True(_store.Save(new AppSettings()));

        Assert.Equal([_store.FilePath], Directory.GetFiles(_folder));
        Assert.Equal(File.ReadAllText(_store.FilePath), _store.Known);
    }

    [Fact]
    public void The_file_reads_as_a_person_would_write_it()
    {
        var settings = new AppSettings { Group = new Named(1625, "ГВГ") };

        Assert.Equal(
            """
            {
              "server": "",
              "nickname": "Gengfu",
              "listeners": "group",
              "group": {
                "id": 1625,
                "name": "ГВГ"
              },
              "channels": [],
              "listenersServer": null,
              "lead": 30,
              "callText": null,
              "voice": null,
              "soundFile": null,
              "language": null,
              "events": [
                {
                  "name": null,
                  "spawns": ["25:00", "20:00", "15:00", "10:00", "05:00"],
                  "text": null
                }
              ]
            }
            """,
            SettingsStore.Serialize(settings).ReplaceLineEndings("\n"));
    }

    [Fact]
    public void A_hand_written_file_may_have_comments_and_trailing_commas_and_leave_settings_out()
    {
        File.WriteAllText(Path.Combine(Directory.CreateDirectory(_folder).FullName, "settings.json"), """
            {
              // Our guild's preset.
              "events": [
                { "name": "Boss", "spawns": ["28:00", "5:30",], "text": "Boss in thirty seconds" },
              ],
              "language": "en", /* the rest stays as it is */
            }
            """);

        var settings = Assert.IsType<SettingsLoad.Loaded>(_store.Load()).Settings;

        var boss = Assert.Single(settings.Events);
        Assert.Equal(("Boss", "Boss in thirty seconds"), (boss.Name, boss.Text));
        Assert.Equal([TimeSpan.FromMinutes(28), TimeSpan.FromMinutes(5.5)], boss.Spawns);
        Assert.Equal("en", settings.Language);
        Assert.Equal(("", "Gengfu", CallSchedule.DefaultLead), (settings.Server, settings.Nickname, settings.Lead));
    }

    [Theory]
    [InlineData("{\n  \"server\": \"a\"\n  \"lead\": 30\n}", 3, null)]
    [InlineData("{\n  \"server\": \"a\",\n  \"lead\": 25\n}", 3, "lead")]
    [InlineData("{\n  \"lead\": \"30\"\n}", 2, "lead")]
    [InlineData("{\n  \"nickame\": \"Gengfu\"\n}", 2, "nickame")]
    [InlineData("{\n  \"server\": null\n}", 2, "server")]
    [InlineData("{\n  \"listeners\": \"squad\"\n}", 2, "listeners")]
    [InlineData("{\n  \"listeners\": 1\n}", 2, "listeners")]
    [InlineData("{\n  \"group\": { \"name\": \"GvG\" }\n}", 2, "group")]
    [InlineData("{\n  \"language\": \"de\"\n}", 2, "language")]
    [InlineData("{\n  \"events\": [\n    { \"name\": \"Boss\" }\n  ]\n}", 3, "events[0]")]
    [InlineData("{\n  \"events\": [\n    { \"spawns\": [\"25:00\",\n      \"36:00\"] }\n  ]\n}", 4, "events[0].spawns")]
    [InlineData("{\n  \"events\": [{ \"spawns\": [\"25\"] }]\n}", 2, "events[0].spawns")]
    [InlineData("{\n  \"events\": [{ \"spawns\": [\"25:60\"] }]\n}", 2, "events[0].spawns")]
    [InlineData("{\n  \"events\": [{ \"spawns\": [1500] }]\n}", 2, "events[0].spawns")]
    [InlineData("{\n  \"events\": [{ \"spawns\": \"25:00\" }]\n}", 2, "events[0].spawns")]
    [InlineData("{\n  \"events\": [{ \"name\": \" \", \"spawns\": [] }]\n}", 2, "events[0].name")]
    [InlineData("{\n  \"events\": [{ \"spawns\": [], \"text\": \"\" }]\n}", 2, "events[0].text")]
    [InlineData("{\n  \"events\": [{ \"spawns\": [] },\n    { \"spawns\": [], \"colour\": 1 }]\n}", 3, "events[1].colour")]
    [InlineData("{\n  \"events\": [\n    { \"spawns\": [] },\n    null\n  ]\n}", 4, "events[1]")]
    [InlineData("{\n  \"channels\": [null]\n}", 2, "channels[0]")]
    [InlineData("{\n  \"channels\": [{ \"id\": 3978 }]\n}", 2, "channels[0]")]
    [InlineData("", 1, null)]
    [InlineData("null", 1, null)]
    [InlineData("[]", 1, null)]
    public void A_file_with_an_error_says_on_which_line_and_in_which_setting(string text, int line, string? setting)
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(_store.FilePath, text);

        Assert.Equal(new SettingsError(line, setting), Assert.IsType<SettingsLoad.Broken>(_store.Load()).Error);
    }

    [Fact]
    public void What_Gengfu_last_wrote_or_read_loads_as_unchanged()
    {
        _store.Save(new AppSettings());
        Assert.IsType<SettingsLoad.Unchanged>(_store.Load());

        File.WriteAllText(_store.FilePath, "{ \"nickname\": \"Gengfu 2\" }");
        Assert.Equal("Gengfu 2", Assert.IsType<SettingsLoad.Loaded>(_store.Load()).Settings.Nickname);
        Assert.IsType<SettingsLoad.Unchanged>(_store.Load());

        // A broken text is known too: putting the last good text back loads it again.
        File.WriteAllText(_store.FilePath, "{");
        Assert.IsType<SettingsLoad.Broken>(_store.Load());
        File.WriteAllText(_store.FilePath, "{ \"nickname\": \"Gengfu 2\" }");
        Assert.IsType<SettingsLoad.Loaded>(_store.Load());

        File.Delete(_store.FilePath);
        Assert.IsType<SettingsLoad.Missing>(_store.Load());
        Assert.Null(_store.Known);
    }

    [Fact]
    public void A_file_another_program_holds_shut_is_unreadable_and_refuses_a_save()
    {
        _store.Save(new AppSettings());
        var written = _store.Known;

        using (new FileStream(_store.FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Assert.IsType<SettingsLoad.Unreadable>(_store.Load());
            Assert.False(_store.Save(new AppSettings { Nickname = "Gengfu 2" }));
            Assert.Equal(written, _store.Known);
        }

        Assert.True(_store.Save(new AppSettings { Nickname = "Gengfu 2" }));
        Assert.Equal("Gengfu 2", Assert.IsType<SettingsLoad.Loaded>(new SettingsStore(_store.FilePath).Load()).Settings.Nickname);
    }

    [Fact]
    public async Task Watching_sees_an_edit_made_by_another_program()
    {
        var changed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var watch = _store.Watch(() => changed.TrySetResult());

        await File.WriteAllTextAsync(_store.FilePath, "{}");

        await changed.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }
}
