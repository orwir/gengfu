using System.Windows;
using System.Windows.Media;
using Gengfu.Core.Settings;

namespace Gengfu.App.Tests;

[Collection(WpfAppCollection.Name)]
public sealed class AppThemeTests(WpfApp wpf)
{
    [Fact]
    public void Same_as_Windows_follows_Windows_and_a_chosen_theme_wins_over_it()
    {
        Assert.Equal(ThemeMode.System, AppTheme.ModeOf(null));
        Assert.Equal(ThemeMode.Light, AppTheme.ModeOf(Theme.Light));
        Assert.Equal(ThemeMode.Dark, AppTheme.ModeOf(Theme.Dark));
    }

    [Fact]
    public void A_theme_repaints_at_once_and_the_one_in_use_set_again_loads_nothing_afresh()
    {
        var (light, dark, kept) = wpf.Invoke(() =>
        {
            try
            {
                AppTheme.Apply(Theme.Light);
                var light = Base().Color;
                AppTheme.Apply(Theme.Dark);
                var brush = Base();
                AppTheme.Apply(Theme.Dark);
                return (light, brush.Color, ReferenceEquals(brush, Base()));
            }
            finally
            {
                AppTheme.Apply(null);
            }
        });

        Assert.Equal(Color.FromRgb(0xF3, 0xF3, 0xF3), light);
        Assert.Equal(Color.FromRgb(0x20, 0x20, 0x20), dark);
        Assert.True(kept);
    }

    // The window's surface, surface.base in design_language.md.
    private static SolidColorBrush Base() => (SolidColorBrush)Application.Current.FindResource("SolidBackgroundFillColorBaseBrush");
}
