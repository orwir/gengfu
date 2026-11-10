using System.IO;
using System.Runtime.CompilerServices;

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

    [Fact]
    public void An_exception_that_escapes_or_fails_a_task_nothing_waits_on_lands_in_the_log_with_its_stack()
    {
        var folder = Path.Combine(Path.GetTempPath(), "gengfu-tests", Guid.NewGuid().ToString("N"));
        var log = new Log(folder);
        try
        {
            Program.Escaped(log, Thrown(new InvalidOperationException("The clock broke.")));
            Program.Unobserved(log, new AggregateException(Thrown(new TimeoutException("The lists never came."))));

            var text = File.ReadAllText(log.FilePath);
            Assert.Contains("Gengfu closed after an error nothing caught:" + Environment.NewLine + "System.InvalidOperationException: The clock broke." + Environment.NewLine + "   at ", text, StringComparison.Ordinal);
            Assert.Contains("A task failed with nothing waiting on it; Gengfu carries on:", text, StringComparison.Ordinal);
            Assert.Contains("System.TimeoutException: The lists never came." + Environment.NewLine + "   at ", text, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    // An exception nothing catches can't be raised here without ending the test run: a startup
    // hook in the real Gengfu.exe checks that one.
    [Fact]
    public void A_task_that_fails_with_nothing_waiting_on_it_reaches_the_log_while_Gengfu_listens()
    {
        var folder = Path.Combine(Path.GetTempPath(), "gengfu-tests", Guid.NewGuid().ToString("N"));
        var log = new Log(folder);
        try
        {
            using (Program.LogEscapes(log))
            {
                FailUnwatched("The groups never came.");
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            FailUnwatched("The channels never came.");
            GC.Collect();
            GC.WaitForPendingFinalizers();

            var text = File.ReadAllText(log.FilePath);
            Assert.Contains("A task failed with nothing waiting on it; Gengfu carries on:", text, StringComparison.Ordinal);
            Assert.Contains("System.InvalidOperationException: The groups never came.", text, StringComparison.Ordinal);
            Assert.DoesNotContain("The channels never came.", text, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    // A task that fails and that nothing waits on, gone by the time this returns.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void FailUnwatched(string message)
    {
        var task = Task.Run(() => throw new InvalidOperationException(message));
        SpinWait.SpinUntil(() => task.IsCompleted);
    }

    // Thrown, so it carries the stack it was thrown from.
    private static Exception Thrown(Exception exception)
    {
        try
        {
            throw exception;
        }
        catch (Exception thrown)
        {
            return thrown;
        }
    }
}
