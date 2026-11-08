using Gengfu.App.Speech;

namespace Gengfu.App.Tests.Speech;

/// <summary>
/// Real files through Windows' media stack. Where it isn't installed, as on Windows Server or N
/// editions, every read must fail, so the call falls back to the gong.
/// </summary>
public sealed class MediaFoundationTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("gengfu-").FullName;

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public void A_WAV_file_reads_as_48_kHz_stereo()
    {
        var path = Write("tone.wav", Sound.Wav(Sound.Tone(440, 1, 44100), 44100));
        if (!Sound.MediaFoundationInstalled)
        {
            Assert.ThrowsAny<Exception>(() => MediaFoundation.Read(path, Pcm.MaxLength));
            return;
        }

        var (stereo, cut) = MediaFoundation.Read(path, Pcm.MaxLength);

        Assert.False(cut);
        Assert.InRange(stereo.Length / 2, 47900, 48100);
        Assert.Equal(0.5, Sound.Strength(Sound.Left(stereo)[4800..43200], 440, 48000), 0.02);
    }

    [Fact]
    public void An_MP3_file_reads_as_48_kHz_stereo()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Speech", "tone.mp3");
        if (!Sound.MediaFoundationInstalled)
        {
            Assert.ThrowsAny<Exception>(() => MediaFoundation.Read(path, Pcm.MaxLength));
            return;
        }

        var (stereo, _) = MediaFoundation.Read(path, Pcm.MaxLength);

        Assert.InRange(stereo.Length / 2.0, 0.85 * 48000, 1.1 * 48000);
        Assert.Equal(0.5, Sound.Strength(Sound.Left(stereo)[4800..38400], 440, 48000), 0.05);
    }

    [Fact]
    public void Reading_stops_a_little_past_the_limit_and_says_the_file_goes_on()
    {
        var path = Write("long.wav", Sound.Wav(Sound.Tone(440, 15, 44100), 44100));
        if (!Sound.MediaFoundationInstalled)
        {
            Assert.ThrowsAny<Exception>(() => MediaFoundation.Read(path, TimeSpan.FromSeconds(2)));
            return;
        }

        var (stereo, cut) = MediaFoundation.Read(path, TimeSpan.FromSeconds(2));

        Assert.True(cut);
        Assert.InRange(stereo.Length / 2, (2 * 48000) + 960, 4 * 48000);
    }

    [Fact]
    public void A_file_is_read_from_where_its_sound_starts()
    {
        var path = Write("late.wav", Sound.Wav([.. Sound.Silence(3, 44100), .. Sound.Tone(440, 12, 44100)], 44100));
        if (!Sound.MediaFoundationInstalled)
        {
            Assert.ThrowsAny<Exception>(() => MediaFoundation.Read(path, Pcm.MaxLength));
            return;
        }

        var (stereo, cut) = MediaFoundation.Read(path, Pcm.MaxLength);

        Assert.True(cut);
        Assert.InRange(stereo.Length / 2, 10 * 48000, 12 * 48000);
        Assert.Equal(0.5, Sound.Strength(Sound.Left(stereo)[..48000], 440, 48000), 0.02);
    }

    [Fact]
    public void A_file_whose_sound_ends_within_the_limit_is_read_whole()
    {
        var path = Write("lead-in.wav", Sound.Wav([.. Sound.Silence(1, 44100), .. Sound.Tone(440, 9.5, 44100)], 44100));
        if (!Sound.MediaFoundationInstalled)
        {
            Assert.ThrowsAny<Exception>(() => MediaFoundation.Read(path, Pcm.MaxLength));
            return;
        }

        var (stereo, cut) = MediaFoundation.Read(path, Pcm.MaxLength);

        Assert.False(cut);
        Assert.InRange(stereo.Length / 2.0, 9.45 * 48000, 9.55 * 48000);
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
}
