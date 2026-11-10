using Gengfu.Core.Settings;

namespace Gengfu.Core.Tests.Settings;

public class AppSettingsTests
{
    [Fact]
    public void A_fresh_Gengfu_calls_30_s_ahead_in_its_own_name_and_the_app_language()
    {
        var settings = new AppSettings();

        Assert.Equal(CallSchedule.DefaultLead, settings.Lead);
        Assert.Equal("Gengfu", settings.Nickname);
        Assert.Equal(ListenerKind.Group, settings.Listeners);
        Assert.Null(settings.Voice);
        Assert.Null(settings.Language);
        Assert.Null(settings.Theme);
    }

    [Fact]
    public void A_fresh_Gengfu_calls_the_built_in_GvG_preset_s_jungle_in_its_default_words()
    {
        var settings = new AppSettings();
        var jungle = Assert.Single(settings.Events);

        Assert.Same(MatchEvent.Jungle, jungle);
        Assert.Null(jungle.Name);
        Assert.Null(jungle.Text);
        Assert.Null(jungle.SoundFile);
        Assert.Equal(CallSchedule.GvgSpawns, jungle.Spawns);
        Assert.True(settings.EventsAreDefault);
    }

    [Fact]
    public void The_events_are_the_default_only_while_they_are_the_jungle_alone_as_built_in()
    {
        var settings = new AppSettings { Events = [MatchEvent.Jungle with { Spawns = [.. CallSchedule.GvgSpawns] }] };
        Assert.True(settings.EventsAreDefault);

        foreach (var events in new IReadOnlyList<MatchEvent>[]
        {
            [],
            [MatchEvent.Jungle, MatchEvent.Jungle with { Name = "Boss" }],
            [MatchEvent.Jungle with { Name = "Forest" }],
            [MatchEvent.Jungle with { Spawns = [TimeSpan.FromMinutes(25)] }],
            [MatchEvent.Jungle with { Text = "Jungle, go go go" }],
            [MatchEvent.Jungle with { SoundFile = @"C:\Sounds\jungle.mp3" }],
        })
        {
            settings.Events = events;
            Assert.False(settings.EventsAreDefault);
        }
    }

    [Theory]
    [InlineData("25:00, 20:00, 15:00", new[] { 1500, 1200, 900 })]
    [InlineData("15 25 20", new[] { 1500, 1200, 900 })]
    [InlineData("5:30;05:30, 5", new[] { 330, 300 })]
    [InlineData("35, 0:00", new[] { 2100, 0 })]
    [InlineData(" , ", new int[0])]
    [InlineData("", new int[0])]
    public void Typed_spawn_times_read_as_game_times_or_whole_minutes_and_come_back_soonest_first_each_once(string typed, int[] seconds)
    {
        var (spawns, unreadable) = MatchEvent.ReadSpawns(typed);

        Assert.Null(unreadable);
        Assert.Equal(seconds.Select(second => TimeSpan.FromSeconds(second)), spawns);
    }

    [Theory]
    [InlineData("25:00, 61:00, 15", "61:00")]
    [InlineData("25, 36", "36")]
    [InlineData("25:5", "25:5")]
    [InlineData("25:60", "25:60")]
    [InlineData("25.30", "25.30")]
    [InlineData("boss", "boss")]
    [InlineData("25:", "25:")]
    public void A_part_that_isn_t_a_game_time_reads_as_none_and_names_itself(string typed, string part)
    {
        var (spawns, unreadable) = MatchEvent.ReadSpawns(typed);

        Assert.Empty(spawns);
        Assert.Equal(part, unreadable);
    }

    [Fact]
    public void Spawn_times_write_as_the_game_shows_them()
    {
        Assert.Equal("25:00, 05:30, 00:00", MatchEvent.WriteSpawns([TimeSpan.FromMinutes(25), TimeSpan.FromMinutes(5.5), TimeSpan.Zero]));
        Assert.Equal("", MatchEvent.WriteSpawns([]));
    }

    [Fact]
    public void Every_lead_on_offer_includes_the_default_and_leaves_the_schedule_whole_ticks_to_fire()
    {
        Assert.Contains(CallSchedule.DefaultLead, AppSettings.Leads);
        Assert.All(AppSettings.Leads, lead => Assert.InRange(lead, TimeSpan.FromSeconds(15), TimeSpan.FromMinutes(1)));
    }
}
