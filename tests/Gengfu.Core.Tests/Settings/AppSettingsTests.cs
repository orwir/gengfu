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
        Assert.Null(settings.CallText);
        Assert.Null(settings.Voice);
        Assert.Null(settings.SoundFile);
        Assert.Null(settings.Language);
    }

    [Fact]
    public void A_fresh_Gengfu_calls_the_built_in_GvG_preset_s_jungle_in_the_words_Settings_sets()
    {
        var jungle = Assert.Single(new AppSettings().Events);

        Assert.Same(MatchEvent.Jungle, jungle);
        Assert.Null(jungle.Name);
        Assert.Null(jungle.Text);
        Assert.Equal(CallSchedule.GvgSpawns, jungle.Spawns);
    }

    [Fact]
    public void Every_lead_on_offer_includes_the_default_and_leaves_the_schedule_whole_ticks_to_fire()
    {
        Assert.Contains(CallSchedule.DefaultLead, AppSettings.Leads);
        Assert.All(AppSettings.Leads, lead => Assert.InRange(lead, TimeSpan.FromSeconds(15), TimeSpan.FromMinutes(1)));
    }
}
