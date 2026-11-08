using System.Globalization;

namespace Gengfu.App.Tests;

public sealed class AppLanguageTests
{
    [Fact]
    public void Same_as_Windows_is_Russian_on_a_Russian_Windows_and_English_on_any_other()
    {
        Assert.Equal(AppLanguage.Russian, AppLanguage.Resolve(null, CultureInfo.GetCultureInfo("ru-RU")));
        Assert.Equal(AppLanguage.English, AppLanguage.Resolve(null, CultureInfo.GetCultureInfo("en-GB")));
        Assert.Equal(AppLanguage.English, AppLanguage.Resolve(null, CultureInfo.GetCultureInfo("de-DE")));
    }

    [Fact]
    public void A_chosen_language_wins_over_Windows()
    {
        Assert.Equal(AppLanguage.English, AppLanguage.Resolve("en", CultureInfo.GetCultureInfo("ru-RU")));
        Assert.Equal(AppLanguage.Russian, AppLanguage.Resolve("ru", CultureInfo.GetCultureInfo("en-US")));
    }
}
