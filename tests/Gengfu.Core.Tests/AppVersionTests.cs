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
}
