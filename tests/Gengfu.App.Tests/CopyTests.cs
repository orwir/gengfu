using System.Globalization;
using Gengfu.App.Speech;
using Gengfu.Core.Settings;
using Gengfu.TeamSpeak;

namespace Gengfu.App.Tests;

public sealed class CopyTests
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en");
    private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru");

    [Fact]
    public void An_event_s_default_words_are_its_name_and_the_lead_for_every_lead_in_both_languages()
    {
        var boss = new MatchEvent { Name = "Boss", Spawns = [] };
        foreach (var language in new[] { English, Russian })
        {
            var words = AppSettings.Leads.Select(lead => Copy.DefaultWords(boss, lead, language)).ToList();
            Assert.Equal(words.Count, words.Distinct().Count());
            Assert.All(words, said => Assert.StartsWith("Boss ", said, StringComparison.Ordinal));
        }
        Assert.Equal("Jungle in thirty seconds", Copy.DefaultWords(MatchEvent.Jungle, TimeSpan.FromSeconds(30), English));
        Assert.Equal("Лес через тридцать секунд", Copy.DefaultWords(MatchEvent.Jungle, TimeSpan.FromSeconds(30), Russian));
        Assert.Equal("Босс через минуту", Copy.DefaultWords(boss with { Name = "Босс" }, TimeSpan.FromMinutes(1), Russian));
        Assert.Equal("Boss in forty-five seconds", Copy.DefaultWords(boss, TimeSpan.FromSeconds(45), English));
        Assert.Throws<ArgumentOutOfRangeException>(() => Copy.DefaultWords(boss, TimeSpan.FromSeconds(40), English));
    }

    [Fact]
    public void Events_called_together_say_each_name_once_joined_then_the_lead_in_both_languages()
    {
        var thirty = TimeSpan.FromSeconds(30);
        var boss = new MatchEvent { Name = "Boss", Spawns = [] };
        var supply = new MatchEvent { Name = "Supply", Spawns = [] };

        Assert.Equal("Jungle and Boss in thirty seconds", Copy.DefaultWords([MatchEvent.Jungle, boss], thirty, English));
        Assert.Equal("Jungle, Boss and Supply in thirty seconds", Copy.DefaultWords([MatchEvent.Jungle, boss, supply], thirty, English));
        Assert.Equal("Лес и Босс через тридцать секунд", Copy.DefaultWords([MatchEvent.Jungle, boss with { Name = "Босс" }], thirty, Russian));
        Assert.Equal(
            "Лес, Босс и Снабжение через минуту",
            Copy.DefaultWords([MatchEvent.Jungle, boss with { Name = "Босс" }, supply with { Name = "Снабжение" }], TimeSpan.FromMinutes(1), Russian));
        Assert.Equal("Boss and Jungle in thirty seconds", Copy.DefaultWords([boss, MatchEvent.Jungle, boss with { Text = "" }], thirty, English));
    }

    [Fact]
    public void A_closed_event_shows_its_spawn_times_and_its_file_or_what_is_wrong_with_it()
    {
        using var english = new UiCulture("en-US");
        var boss = new MatchEvent { Name = "Boss", Spawns = [TimeSpan.FromMinutes(25), TimeSpan.FromMinutes(15)] };
        var withFile = boss with { SoundFile = @"C:\Sounds\boss.mp3" };

        Assert.Equal(("25:00 · 15:00", false), Copy.EventSummary(boss, CallProblem.None, unreadable: false));
        Assert.Equal(("25:00 · 15:00 · boss.mp3", false), Copy.EventSummary(withFile, CallProblem.None, unreadable: false));
        Assert.Equal(("No spawn times yet", false), Copy.EventSummary(boss with { Spawns = [] }, CallProblem.None, unreadable: false));
        // No voice is every event's trouble, so Calls says it, not each event.
        Assert.Equal(("25:00 · 15:00", false), Copy.EventSummary(boss, CallProblem.NoVoice, unreadable: false));
        Assert.Equal(("Nothing to say: its calls play a gong", true), Copy.EventSummary(boss, CallProblem.NoText, unreadable: false));
        Assert.Equal(("Windows couldn’t speak the text", true), Copy.EventSummary(boss, CallProblem.SpeechFailed, unreadable: false));
        Assert.Equal(("Windows can’t play boss.mp3", true), Copy.EventSummary(withFile, CallProblem.FileUnreadable, unreadable: false));
        Assert.Equal(("boss.mp3 isn’t on this PC", true), Copy.EventSummary(withFile, CallProblem.FileMissing, unreadable: false));
        Assert.Equal(("Can’t read the spawn times", true), Copy.EventSummary(withFile, CallProblem.FileMissing, unreadable: true));
    }

    [Fact]
    public void A_file_that_can_t_play_says_why_and_what_its_calls_play_instead_in_both_languages()
    {
        using (new UiCulture("en-US"))
        {
            Assert.Equal("boss.mp3 isn’t on this PC, so its calls say “Boss in thirty seconds”.",
                Copy.FileHint(CallProblem.FileMissing, "boss.mp3", "Boss in thirty seconds"));
            Assert.Equal("boss.mp3 isn’t on this PC, so its calls play a gong.", Copy.FileHint(CallProblem.FileMissing, "boss.mp3", null));
            Assert.Equal("Windows can’t play this file, so its calls say “Boss in thirty seconds”. Pick another, such as an MP3 or WAV.",
                Copy.FileHint(CallProblem.FileUnreadable, "boss.mp3", "Boss in thirty seconds"));
            Assert.Equal("Windows can’t play this file, so its calls play a gong. Pick another, such as an MP3 or WAV.",
                Copy.FileHint(CallProblem.FileUnreadable, "boss.mp3", null));
        }
        using (new UiCulture("ru-RU"))
        {
            Assert.Equal("Файла босс.mp3 нет на этом компьютере, поэтому объявления говорят: «Босс через тридцать секунд».",
                Copy.FileHint(CallProblem.FileMissing, "босс.mp3", "Босс через тридцать секунд"));
        }
    }

    [Fact]
    public void A_Russian_count_takes_the_form_its_number_asks_for()
    {
        Assert.Equal(
            new[] { "1 канал", "2 канала", "4 канала", "5 каналов", "11 каналов", "12 каналов", "21 канал", "22 канала", "25 каналов", "111 каналов" },
            new[] { 1, 2, 4, 5, 11, 12, 21, 22, 25, 111 }.Select(count => Copy.ChannelCount(count, Russian)));
    }

    [Fact]
    public void An_English_count_is_singular_only_for_one()
    {
        Assert.Equal(
            new[] { "1 channel", "2 channels", "21 channels" },
            new[] { 1, 2, 21 }.Select(count => Copy.ChannelCount(count, English)));
    }

    [Fact]
    public void A_privilege_key_s_bar_names_the_group_it_gave_or_says_why_the_server_didn_t_take_it()
    {
        using (new UiCulture("en-US"))
        {
            Assert.Equal("Key used — Gengfu is in Callers now.", Copy.KeyAnswer(new KeyResult(KeyOutcome.Taken, "Callers")));
            Assert.Equal("Key used — the server gave Gengfu the key’s group.", Copy.KeyAnswer(new KeyResult(KeyOutcome.Taken)));
            Assert.Equal("The server doesn’t know this key: it’s used already, or mistyped.", Copy.KeyAnswer(new KeyResult(KeyOutcome.Unknown)));
            Assert.Equal(
                "The server doesn’t let the bot use privilege keys: an admin has to give it the group.",
                Copy.KeyAnswer(new KeyResult(KeyOutcome.NotAllowed)));
            Assert.Equal("The server refused the key: flood ban", Copy.KeyAnswer(new KeyResult(KeyOutcome.Refused, "flood ban")));
            Assert.Equal(
                "The connection dropped before the server answered. Try again once connected.",
                Copy.KeyAnswer(new KeyResult(KeyOutcome.NoAnswer)));
        }
        using (new UiCulture("ru-RU"))
        {
            Assert.Equal("Ключ применён — Gengfu теперь в группе Боты.", Copy.KeyAnswer(new KeyResult(KeyOutcome.Taken, "Боты")));
            Assert.Equal("Сервер отклонил ключ: flood ban", Copy.KeyAnswer(new KeyResult(KeyOutcome.Refused, "flood ban")));
        }
    }

    [Fact]
    public void A_lead_reads_in_whole_seconds()
    {
        using (new UiCulture("en-US"))
        {
            Assert.Equal("45 seconds", Copy.Lead(TimeSpan.FromSeconds(45)));
        }
        using (new UiCulture("ru-RU"))
        {
            Assert.Equal("45 секунд", Copy.Lead(TimeSpan.FromSeconds(45)));
        }
    }

    [Fact]
    public void A_voice_shows_its_language_in_that_language_s_own_words()
    {
        Assert.Equal("Microsoft Irina — русский (Россия)", Copy.VoiceLabel(new Voice("irina", "Microsoft Irina", "ru-RU")));
        Assert.Equal("Microsoft David — English (United States)", Copy.VoiceLabel(new Voice("david", "Microsoft David", "en-US")));
        Assert.Equal("Odd — not a-tag!", Copy.VoiceLabel(new Voice("odd", "Odd", "not a-tag!")));
    }

    [Fact]
    public void An_event_goes_by_its_own_name_or_as_the_jungle_in_the_app_s_language()
    {
        using (new UiCulture("en-US"))
        {
            Assert.Equal("Jungle", Copy.EventName(MatchEvent.Jungle));
        }
        using (new UiCulture("ru-RU"))
        {
            Assert.Equal("Лес", Copy.EventName(MatchEvent.Jungle));
        }
        Assert.Equal("Boss", Copy.EventName(new MatchEvent { Name = "Boss", Spawns = [] }));
    }

    [Fact]
    public void The_settings_file_bar_names_the_line_and_the_setting_where_it_has_them()
    {
        using var english = new UiCulture("en-US");

        Assert.Equal(
            "settings.json has an error on line 23 (events[1].spawns). Until it’s fixed, Gengfu works with the settings shown here and saves nothing.",
            Copy.FileError(new SettingsError(23, "events[1].spawns")));
        Assert.StartsWith("settings.json has an error on line 3. Until", Copy.FileError(new SettingsError(3, null)), StringComparison.Ordinal);
        Assert.StartsWith("Gengfu can’t read settings.json.", Copy.FileError(new SettingsError(null, null)), StringComparison.Ordinal);
    }

    [Fact]
    public void The_line_under_the_version_says_where_updates_stand_in_both_languages()
    {
        using (new UiCulture("en-US"))
        {
            Assert.Null(Copy.UpdateLine(new UpdateState.Off()));
            Assert.Equal("Checking for updates…", Copy.UpdateLine(new UpdateState.Checking()));
            Assert.Equal("Up to date · updates install on their own", Copy.UpdateLine(new UpdateState.UpToDate()));
            Assert.Equal("Downloading version 0.1.1…", Copy.UpdateLine(new UpdateState.Downloading("0.1.1")));
            Assert.Equal("Version 0.1.1 installed · applies the next time Gengfu starts", Copy.UpdateLine(new UpdateState.Ready("0.1.1")));
            Assert.Equal("Couldn’t check for updates — trying again later", Copy.UpdateLine(new UpdateState.Failed()));
        }
        using (new UiCulture("ru-RU"))
        {
            Assert.Null(Copy.UpdateLine(new UpdateState.Off()));
            Assert.Equal("Проверяю обновления…", Copy.UpdateLine(new UpdateState.Checking()));
            Assert.Equal("Последняя версия · обновления ставятся сами", Copy.UpdateLine(new UpdateState.UpToDate()));
            Assert.Equal("Загружаю версию 0.1.1…", Copy.UpdateLine(new UpdateState.Downloading("0.1.1")));
            Assert.Equal("Версия 0.1.1 установлена · применится после перезапуска", Copy.UpdateLine(new UpdateState.Ready("0.1.1")));
            Assert.Equal("Не удалось проверить обновления — попробую позже", Copy.UpdateLine(new UpdateState.Failed()));
        }
    }

    [Fact]
    public void Time_left_rounds_up_like_the_clock_and_stops_at_zero()
    {
        Assert.Equal("2:13", Copy.Span(TimeSpan.FromSeconds(132.1)));
        Assert.Equal("0:59", Copy.Span(TimeSpan.FromSeconds(59)));
        Assert.Equal("0:00", Copy.Span(TimeSpan.Zero));
        Assert.Equal("0:00", Copy.Span(TimeSpan.FromSeconds(-3)));
    }
}
