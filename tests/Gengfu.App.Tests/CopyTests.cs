using System.Globalization;
using Gengfu.App.Speech;
using Gengfu.Core.Settings;

namespace Gengfu.App.Tests;

public sealed class CopyTests
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en");
    private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru");

    [Fact]
    public void Every_lead_on_offer_has_its_own_default_text_in_both_languages()
    {
        foreach (var language in new[] { English, Russian })
        {
            var texts = AppSettings.Leads.Select(lead => Copy.CallText(lead, language)).ToList();
            Assert.Equal(texts.Count, texts.Distinct().Count());
        }
        Assert.Equal("Jungle in thirty seconds", Copy.CallText(TimeSpan.FromSeconds(30), English));
        Assert.Equal("Лес через тридцать секунд", Copy.CallText(TimeSpan.FromSeconds(30), Russian));
        Assert.Throws<ArgumentOutOfRangeException>(() => Copy.CallText(TimeSpan.FromSeconds(40), English));
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
    public void Time_left_rounds_up_like_the_clock_and_stops_at_zero()
    {
        Assert.Equal("2:13", Copy.Span(TimeSpan.FromSeconds(132.1)));
        Assert.Equal("0:59", Copy.Span(TimeSpan.FromSeconds(59)));
        Assert.Equal("0:00", Copy.Span(TimeSpan.Zero));
        Assert.Equal("0:00", Copy.Span(TimeSpan.FromSeconds(-3)));
    }
}
