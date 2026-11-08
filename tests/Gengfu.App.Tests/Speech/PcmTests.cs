using Gengfu.App.Speech;

namespace Gengfu.App.Tests.Speech;

public sealed class PcmTests
{
    [Fact]
    public void A_WAV_of_16_bit_PCM_reads_as_mono_samples_at_its_own_rate()
    {
        var tone = Sound.Tone(440, 0.1, 16000);

        var (samples, rate) = Pcm.ReadWav(Sound.Wav(tone, 16000));

        Assert.Equal(16000, rate);
        Assert.Equal(tone.Length, samples.Length);
        Assert.All(samples.Zip(tone), pair => Assert.Equal(pair.Second, pair.First, 0.0001));
    }

    [Fact]
    public void Stereo_speech_is_mixed_to_mono()
    {
        float[] stereo = [.. Enumerable.Range(0, 100).SelectMany(_ => new[] { 0.4f, 0.2f })];

        var (samples, _) = Pcm.ReadWav(Sound.Wav(stereo, 22050, channels: 2));

        Assert.Equal(100, samples.Length);
        Assert.All(samples, sample => Assert.Equal(0.3, sample, 0.001));
    }

    [Fact]
    public void Speech_written_without_its_size_reads_to_the_end()
    {
        var tone = Sound.Tone(440, 0.1, 16000);

        var (samples, _) = Pcm.ReadWav(Sound.Wav(tone, 16000, dataSize: 0));

        Assert.Equal(tone.Length, samples.Length);
    }

    [Fact]
    public void Speech_that_is_not_16_bit_PCM_is_refused()
    {
        Assert.Throws<InvalidDataException>(() => Pcm.ReadWav(Sound.Wav(Sound.Tone(440, 0.1, 16000), 16000, format: 3)));
        Assert.Throws<InvalidDataException>(() => Pcm.ReadWav("not speech at all"u8));
        Assert.Throws<InvalidDataException>(() => Pcm.ReadWav(Sound.Wav([], 16000)[..36]));
    }

    [Fact]
    public void Resampling_to_48_kHz_keeps_the_pitch_and_the_level()
    {
        var resampled = Pcm.To48k(Sound.Tone(1000, 1, 16000), 16000);

        Assert.Equal(48000, resampled.Length);
        var middle = resampled[4800..43200];
        Assert.Equal(0.5, Sound.Strength(middle, 1000, 48000), 0.01);
        Assert.Equal(0.5 / Math.Sqrt(2), Sound.Rms(middle), 0.01);
    }

    [Fact]
    public void Resampling_keeps_out_the_mirror_image_of_the_old_rate()
    {
        // Upsampled carelessly, a 6 kHz tone at 16 kHz comes with a 10 kHz ghost.
        var resampled = Pcm.To48k(Sound.Tone(6000, 1, 16000), 16000)[4800..43200];

        Assert.True(Sound.Strength(resampled, 10000, 48000) < Sound.Strength(resampled, 6000, 48000) / 300);
    }

    [Fact]
    public void Resampling_from_a_rate_with_no_whole_ratio_keeps_the_pitch()
    {
        var resampled = Pcm.To48k(Sound.Tone(1000, 1, 22050), 22050);

        Assert.Equal(48000, resampled.Length);
        Assert.Equal(0.5, Sound.Strength(resampled[4800..43200], 1000, 48000), 0.01);
    }

    [Fact]
    public void Audio_already_at_48_kHz_passes_through()
    {
        var tone = Sound.Tone(440, 0.1, 48000);

        Assert.Same(tone, Pcm.To48k(tone, 48000));
    }

    [Fact]
    public void Shaping_trims_the_silence_leaves_100_ms_before_the_sound_and_fills_whole_20_ms_frames()
    {
        float[] sound = [.. Sound.Silence(0.5, 48000), .. Sound.Tone(440, 0.3, 48000), .. Sound.Silence(0.5, 48000)];

        var left = Sound.Left(Pcm.Shape(sound, 1)!);

        var first = Array.FindIndex(left, sample => Math.Abs(sample) > 0.001);
        var last = Array.FindLastIndex(left, sample => Math.Abs(sample) > 0.001);
        Assert.Equal(4800, first);
        Assert.InRange(last - first, 14390, 14400);
        Assert.InRange(left.Length - 1 - last, 960, 960 + 959);
        Assert.Equal(0, left.Length % 960);
    }

    [Fact]
    public void A_sound_longer_than_10_s_is_cut_there_and_fades_out()
    {
        var left = Sound.Left(Pcm.Shape(Sound.Tone(440, 12, 48000), 1)!);

        Assert.Equal(4800 + 480000 + 960, left.Length);
        var start = Sound.Rms(left[4800..(4800 + 9600)]);
        var end = Sound.Rms(left[(4800 + 480000 - 960)..(4800 + 480000)]);
        Assert.True(end < start / 20, $"the last 20 ms are {end / start:P0} of the start");
    }

    [Fact]
    public void A_sound_cut_off_before_its_end_fades_out_though_it_is_under_10_s()
    {
        var tone = Sound.Tone(440, 5, 48000);

        var cut = Sound.Left(Pcm.Shape(tone, 1, cut: true)!);
        var whole = Sound.Left(Pcm.Shape(tone, 1)!);

        // The sound sits from 100 ms in; compare its first and last 20 ms.
        var start = Sound.Rms(cut[4800..5760]);
        Assert.True(Sound.Rms(cut[(4800 + 240000 - 960)..(4800 + 240000)]) < start / 20);
        Assert.Equal(start, Sound.Rms(whole[(4800 + 240000 - 960)..(4800 + 240000)]), 0.01);
    }

    [Theory]
    [InlineData(0.05)]
    [InlineData(0.8)]
    public void Quiet_and_loud_sounds_come_out_equally_loud(double level)
    {
        var call = Pcm.Shape(Sound.Tone(440, 1, 48000, level), 1)!;

        Assert.Equal(-16, Pcm.MomentaryMax([.. call.Select(sample => sample / 32768f)]), 0.2);
    }

    [Fact]
    public void A_sound_of_sharp_peaks_stays_under_the_peak_limit()
    {
        var clicks = new float[48000];
        for (var i = 0; i < clicks.Length; i += 4800)
        {
            clicks[i] = 0.5f;
        }

        var call = Pcm.Shape(clicks, 1)!;

        Assert.InRange(call.Max(sample => Math.Abs((double)sample)), 0.88 * 32767, 0.892 * 32767);
        Assert.True(Pcm.MomentaryMax([.. call.Select(sample => sample / 32768f)]) < -16);
    }

    [Fact]
    public void A_stereo_file_keeps_its_two_channels()
    {
        float[] leftOnly = [.. Sound.Tone(440, 0.5, 48000).SelectMany(sample => new[] { sample, 0f })];

        var call = Pcm.Shape(leftOnly, 2)!;

        Assert.True(Sound.Peak(Sound.Left(call)) > 0.1);
        Assert.Equal(0, call.Where((_, index) => index % 2 == 1).Max(sample => Math.Abs((int)sample)));
    }

    [Fact]
    public void Silence_shapes_to_nothing()
    {
        Assert.Null(Pcm.Shape(Sound.Silence(1, 48000), 1));
        Assert.Null(Pcm.Shape(Sound.Tone(440, 1, 48000, level: 0.0005), 1));
    }
}
