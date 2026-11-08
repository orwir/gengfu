using System.Runtime.InteropServices;
using Gengfu.App.Speech;

namespace Gengfu.App.Tests.Speech;

/// <summary>
/// Real files through Windows' media stack. Where it isn't installed, as on Windows Server or N
/// editions, every read must fail, so the call falls back to the gong.
/// </summary>
public sealed class MediaFoundationTests : IDisposable
{
    private static readonly bool Installed = MediaFoundationStarts();
    private readonly string _folder = Directory.CreateTempSubdirectory("gengfu-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public void A_WAV_file_reads_as_48_kHz_stereo()
    {
        var path = Write("tone.wav", Sound.Wav(Sound.Tone(440, 1, 44100), 44100));
        if (!Installed)
        {
            Assert.ThrowsAny<Exception>(() => MediaFoundation.Read(path, Pcm.MaxLength));
            return;
        }

        var stereo = MediaFoundation.Read(path, Pcm.MaxLength);

        Assert.InRange(stereo.Length / 2, 47900, 48100);
        Assert.Equal(0.5, Sound.Strength(Sound.Left(stereo)[4800..43200], 440, 48000), 0.02);
    }

    [Fact]
    public void An_MP3_file_reads_as_48_kHz_stereo()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Speech", "tone.mp3");
        if (!Installed)
        {
            Assert.ThrowsAny<Exception>(() => MediaFoundation.Read(path, Pcm.MaxLength));
            return;
        }

        var stereo = MediaFoundation.Read(path, Pcm.MaxLength);

        Assert.InRange(stereo.Length / 2.0, 0.85 * 48000, 1.1 * 48000);
        Assert.Equal(0.5, Sound.Strength(Sound.Left(stereo)[4800..38400], 440, 48000), 0.05);
    }

    [Fact]
    public void Reading_stops_a_little_past_the_limit()
    {
        var path = Write("long.wav", Sound.Wav(Sound.Tone(440, 15, 44100), 44100));
        if (!Installed)
        {
            Assert.ThrowsAny<Exception>(() => MediaFoundation.Read(path, TimeSpan.FromSeconds(2)));
            return;
        }

        var stereo = MediaFoundation.Read(path, TimeSpan.FromSeconds(2));

        Assert.InRange(stereo.Length / 2, (2 * 48000) + 960, 4 * 48000);
    }

    [Fact]
    public void A_missing_file_is_refused()
    {
        Assert.ThrowsAny<Exception>(() => MediaFoundation.Read(Path.Combine(_folder, "missing.mp3"), Pcm.MaxLength));
    }

    [Fact]
    public void A_file_that_is_not_audio_is_refused()
    {
        var path = Write("notes.mp3", "not audio at all"u8.ToArray());

        Assert.ThrowsAny<Exception>(() => MediaFoundation.Read(path, Pcm.MaxLength));
    }

    private string Write(string name, byte[] content)
    {
        var path = Path.Combine(_folder, name);
        File.WriteAllBytes(path, content);
        return path;
    }

    private static bool MediaFoundationStarts()
    {
        try
        {
            if (Interop.MFStartup(0x00020070, 1) < 0)
            {
                return false;
            }
            _ = Interop.MFShutdown();
            return true;
        }
        catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException or SEHException)
        {
            return false;
        }
    }
}
