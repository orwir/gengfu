using System.IO;

namespace Gengfu.App.Tests;

public sealed class ProgramTests
{
    private static readonly string Own = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Gengfu");
    private static readonly string Elsewhere = Path.Combine(Path.GetTempPath(), "gengfu-tests", "home");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Without_GENGFU_HOME_Gengfu_is_the_users_own_on_its_own_folder_and_lock(string? variable)
    {
        var home = Home.Of(variable);

        Assert.Equal(Own, home.Folder);
        Assert.False(home.Isolated);
        Assert.Equal(@"Local\Gengfu", home.Lock);
    }

    [Fact]
    public void GENGFU_HOME_gives_Gengfu_that_folder_a_lock_of_its_own_and_no_updates()
    {
        var home = Home.Of(Elsewhere);

        Assert.Equal(Elsewhere, home.Folder);
        Assert.True(home.Isolated);
        Assert.StartsWith(@"Local\Gengfu-", home.Lock);
        Assert.False(Program.AppliesUpdate(home, first: true));
        Assert.Null(Program.Install(home, () => throw new InvalidOperationException("A Gengfu on a folder of its own asked for the install.")));
    }

    [Fact]
    public void The_users_own_Gengfu_takes_the_install_and_applies_an_update_when_it_is_the_first()
    {
        var home = Home.Of(null);
        var asked = false;

        Assert.True(Program.AppliesUpdate(home, first: true));
        Assert.False(Program.AppliesUpdate(home, first: false));
        Assert.Null(Program.Install(home, () =>
        {
            asked = true;
            return null;
        }));
        Assert.True(asked);
    }

    [Fact]
    public void One_folder_has_one_lock_however_GENGFU_HOME_spells_it()
    {
        var spelled = Home.Of(Elsewhere.ToUpperInvariant() + Path.DirectorySeparatorChar);

        Assert.Equal(Home.Of(Elsewhere).Lock, spelled.Lock);
        Assert.NotEqual(Home.Of(Elsewhere).Lock, Home.Of(Elsewhere + "2").Lock);
        Assert.True(Path.IsPathFullyQualified(Home.Of("gengfu-home").Folder));
    }

    [Fact]
    public void GENGFU_HOME_naming_the_users_own_folder_is_the_users_own_Gengfu()
    {
        foreach (var spelled in new[] { Own + Path.DirectorySeparatorChar, Own.ToUpperInvariant(), Own.ToLowerInvariant() })
        {
            var home = Home.Of(spelled);

            Assert.False(home.Isolated);
            Assert.Equal(@"Local\Gengfu", home.Lock);
        }
    }
}
