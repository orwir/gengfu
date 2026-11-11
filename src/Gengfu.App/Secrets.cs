using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace Gengfu.App;

/// <summary>
/// The server password and the bot's identity, kept out of the settings file, which a person reads
/// and edits in the clear. They sit beside it, encrypted by Windows for this Windows user: another
/// user, or another PC, can't read them, and Gengfu then starts without them, with a new identity.
/// </summary>
internal sealed class Secrets(string path)
{
    // Ties the encryption to Gengfu, so another program run by the same user can't read the file by chance.
    private static readonly byte[] Entropy = "Gengfu"u8.ToArray();

    /// <summary>
    /// The password and the identity as last saved, or none when there is no file this user can
    /// decrypt; <c>Lost</c> says there was one, as another Windows user or PC saves it. Null when
    /// Windows wouldn't let Gengfu read the file, perhaps for another program holding it: the
    /// identity in it is still the bot's, so the file is left as it is.
    /// </summary>
    public (string Password, string? Identity, bool Lost)? Load()
    {
        byte[] encrypted;
        try
        {
            encrypted = File.ReadAllBytes(path);
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return ("", null, false);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
        try
        {
            var kept = JsonSerializer.Deserialize<Kept>(ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.CurrentUser));
            return (kept?.Password ?? "", kept?.Identity, false);
        }
        catch (Exception exception) when (exception is CryptographicException or JsonException)
        {
            return ("", null, true);
        }
    }

    /// <summary>Writes them; false when Windows refused, for the caller to try again later.</summary>
    public bool Save(string password, string identity)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var encrypted = ProtectedData.Protect(JsonSerializer.SerializeToUtf8Bytes(new Kept(password, identity)), Entropy, DataProtectionScope.CurrentUser);
            // Written beside the file and moved over it, so the identity is never half written.
            var temp = path + ".tmp";
            File.WriteAllBytes(temp, encrypted);
            File.Move(temp, path, overwrite: true);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private sealed record Kept(string Password, string? Identity);
}
