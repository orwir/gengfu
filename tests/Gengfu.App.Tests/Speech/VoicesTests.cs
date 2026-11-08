using System.Globalization;
using Gengfu.App.Speech;

namespace Gengfu.App.Tests.Speech;

public sealed class VoicesTests
{
    private static readonly Voice David = new("david", "Microsoft David", "en-US");
    private static readonly Voice Hazel = new("hazel", "Microsoft Hazel", "en-GB");
    private static readonly Voice Irina = new("irina", "Microsoft Irina", "ru-RU");
    private static readonly Voice Pavel = new("pavel", "Microsoft Pavel", "ru-RU");
    private static readonly CultureInfo English = new("en-US");
    private static readonly CultureInfo Russian = new("ru-RU");

    [Fact]
    public void The_chosen_voice_speaks_while_it_is_installed_whatever_its_language()
    {
        Assert.Equal(David, Voices.Pick([Irina, David], "david", Russian));
    }

    [Fact]
    public void Without_a_choice_the_first_voice_in_the_app_language_speaks()
    {
        Assert.Equal(Irina, Voices.Pick([David, Irina, Pavel], null, Russian));
    }

    [Fact]
    public void A_chosen_voice_that_is_gone_gives_way_to_one_in_the_app_language()
    {
        Assert.Equal(Pavel, Voices.Pick([David, Pavel], "irina", Russian));
    }

    [Fact]
    public void A_voice_from_any_region_of_the_language_will_do()
    {
        Assert.Equal(Hazel, Voices.Pick([Irina, Hazel], null, English));
    }

    [Fact]
    public void Without_a_voice_in_the_app_language_there_is_none_and_the_call_plays_the_gong()
    {
        Assert.Null(Voices.Pick([David, Hazel], null, Russian));
        Assert.Null(Voices.Pick([], null, English));
    }

    [Fact]
    public void Each_installed_voice_has_an_id_a_name_and_a_language()
    {
        foreach (var voice in Voices.Installed())
        {
            Assert.NotEmpty(voice.Id);
            Assert.NotEmpty(voice.Name);
            Assert.Matches("^[a-z]{2,3}(-|$)", voice.Language);
        }
    }
}
