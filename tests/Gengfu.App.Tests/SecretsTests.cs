using System.IO;
using System.Security.Cryptography;

namespace Gengfu.App.Tests;

// A real file, in a folder of the test's own, encrypted by this machine's Windows for this user.
public sealed class SecretsTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "gengfu-tests", Guid.NewGuid().ToString("N"));
    private readonly Secrets _secrets;

    public SecretsTests() => _secrets = new Secrets(SecretsFile);

    private string SecretsFile => Path.Combine(_folder, "secrets.bin");

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public void Saved_secrets_load_back_and_the_file_reads_as_nothing_but_to_Gengfu()
    {
        Assert.True(_secrets.Save("gvgnight", "123Videntity"));

        Assert.Equal<(string, string?)?>(("gvgnight", "123Videntity"), _secrets.Load());
        var file = File.ReadAllBytes(SecretsFile);
        Assert.Equal(-1, file.AsSpan().IndexOf("gvgnight"u8));
        Assert.Throws<CryptographicException>(() => ProtectedData.Unprotect(file, null, DataProtectionScope.CurrentUser));
        Assert.Equal([SecretsFile], Directory.GetFiles(_folder));
    }

    [Fact]
    public void Without_a_file_this_user_can_decrypt_there_are_no_secrets()
    {
        Assert.Equal<(string, string?)?>(("", null), _secrets.Load());

        // As a file from another PC or another Windows user reads here.
        Directory.CreateDirectory(_folder);
        File.WriteAllBytes(SecretsFile, [1, 2, 3]);
        Assert.Equal<(string, string?)?>(("", null), _secrets.Load());

        Assert.True(_secrets.Save("", "123Vnew"));
        Assert.Equal<(string, string?)?>(("", "123Vnew"), _secrets.Load());
    }

    [Fact]
    public void A_file_Windows_won_t_let_Gengfu_read_reads_as_nothing_known()
    {
        _secrets.Save("gvgnight", "123Videntity");

        using (new FileStream(SecretsFile, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            Assert.Null(_secrets.Load());
        }
        Assert.Equal<(string, string?)?>(("gvgnight", "123Videntity"), _secrets.Load());
    }
}
