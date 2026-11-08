namespace Gengfu.Core.Tests;

public class AppVersionTests
{
    [Fact]
    public void Current_comes_from_the_single_version_source()
    {
        // The test assembly is stamped from the same <Version>, so the two must agree.
        Assert.Equal(AppVersion.Read(typeof(AppVersionTests).Assembly), AppVersion.Current);
    }

    [Fact]
    public void Current_is_semver_without_the_source_revision()
    {
        Assert.Matches(@"^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$", AppVersion.Current);
    }

    [Theory]
    [InlineData("0.1.0", false)]
    [InlineData("1.12.3", false)]
    [InlineData("0.1.0-rc.1", true)]
    [InlineData("0.1.0-dev", true)]
    public void A_version_with_a_suffix_is_a_test_build(string version, bool prerelease)
    {
        Assert.Equal(prerelease, AppVersion.IsPrerelease(version));
    }
}
