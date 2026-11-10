using System.Globalization;
using System.IO;
using System.Text;

namespace Gengfu.App;

/// <summary>
/// What Gengfu did, in plain English text a player can attach to feedback: gengfu.log in Gengfu's
/// folder, each entry on a line of its own after the local date and time, an exception's stack on
/// the lines under its entry. Once a file would pass <see cref="Cap"/>, the log starts afresh and
/// keeps the full one as gengfu.old.log, the one before it gone. Its writers build each entry from
/// states, names, the server's own words and exception text, never a secret; the Windows user's
/// profile folder shows as %USERPROFILE%.
/// </summary>
internal sealed class Log(string folder)
{
    /// <summary>The size a file grows to before the log starts another: hundreds of matches.</summary>
    internal const long Cap = 1024 * 1024;

    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);
    private static readonly string Profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    private readonly Lock _writing = new();

    public string FilePath { get; } = Path.Combine(folder, "gengfu.log");

    /// <summary>The full file the last start afresh set aside.</summary>
    public string OldFilePath { get; } = Path.Combine(folder, "gengfu.old.log");

    /// <summary>
    /// Adds an entry, from any thread. It is on disk when this returns, so a crash right after keeps
    /// it. An entry Windows won't let Gengfu write is lost: the calls matter more than their log.
    /// </summary>
    public void Write(string entry)
    {
        lock (_writing)
        {
            // Stamped in turn, so the lines' times run in the order the lines do.
            var text = string.Create(CultureInfo.InvariantCulture, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}  {Hidden(entry)}{Environment.NewLine}");
            try
            {
                Directory.CreateDirectory(folder);
                var file = new FileInfo(FilePath);
                if (file.Exists && file.Length + Utf8.GetByteCount(text) > Cap)
                {
                    File.Move(FilePath, OldFilePath, overwrite: true);
                }
                File.AppendAllText(FilePath, text, Utf8);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
            }
        }
    }

    // Exception text names files by their full path, the Windows user's name in it.
    private static string Hidden(string entry) =>
        Profile.Length == 0 ? entry : entry.Replace(Profile, "%USERPROFILE%", StringComparison.OrdinalIgnoreCase);
}
