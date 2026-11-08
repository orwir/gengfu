using System.Buffers.Binary;
using System.IO;
using Gengfu.TeamSpeak;

namespace Gengfu.App.Speech;

/// <summary>
/// Turns decoded audio into a call: 48 kHz stereo, silence trimmed, at most <see cref="MaxLength"/>,
/// at one shared loudness, in whole frames of <see cref="VoiceSender.FrameLength"/>.
/// </summary>
internal static class Pcm
{
    public const int Rate = 48000;

    /// <summary>Samples per channel in a frame: as long as the frames <see cref="VoiceSender"/> paces.</summary>
    public static int FrameSamples { get; } = (int)(Rate * VoiceSender.FrameLength.TotalSeconds);

    // Every call is set to this loudness of its loudest 400 ms (ITU-R BS.1770), about as loud as
    // Windows' own voices speak, with its peaks kept 1 dB under full scale.
    private const double Loudness = -16;
    private const double PeakLimit = 0.891;

    // Trimming cuts what lies 50 dB under the peak; a sound that never rises 60 dB above silence is none.
    private const double TrimLevel = 0.003;
    private const double SilenceFloor = 0.001;

    /// <summary>The longest call: a longer text or file is cut here, fading out, so a wrong file can't hold the whisper for minutes.</summary>
    public static TimeSpan MaxLength { get; } = TimeSpan.FromSeconds(10);

    // Listeners hear a moment of silence before the sound, so its start is never clipped, and after
    // it, so the encoder's lookahead doesn't eat its end.
    private static readonly int LeadFrames = Rate / 10;
    private static readonly int TailFrames = FrameSamples;
    private static readonly int FadeFrames = Rate / 5;

    /// <summary>A WAV of 16-bit PCM, as Windows speech writes it, as mono samples at its own rate.</summary>
    public static (float[] Samples, int Rate) ReadWav(ReadOnlySpan<byte> wav)
    {
        if (wav.Length < 12 || !wav[..4].SequenceEqual("RIFF"u8) || !wav[8..12].SequenceEqual("WAVE"u8))
        {
            throw new InvalidDataException("The speech is not a WAV.");
        }
        int format = 0, channels = 0, rate = 0, bits = 0;
        for (var position = 12; position + 8 <= wav.Length;)
        {
            var id = wav.Slice(position, 4);
            var size = BinaryPrimitives.ReadInt32LittleEndian(wav[(position + 4)..]);
            var body = position + 8;
            if (id.SequenceEqual("fmt "u8) && size >= 16 && body + 16 <= wav.Length)
            {
                format = BinaryPrimitives.ReadUInt16LittleEndian(wav[body..]);
                channels = BinaryPrimitives.ReadUInt16LittleEndian(wav[(body + 2)..]);
                rate = BinaryPrimitives.ReadInt32LittleEndian(wav[(body + 4)..]);
                bits = BinaryPrimitives.ReadUInt16LittleEndian(wav[(body + 14)..]);
            }
            else if (id.SequenceEqual("data"u8))
            {
                // PCM, or the extensible header around it.
                if (format is not (1 or 0xFFFE) || bits != 16 || channels is < 1 or > 2 || rate <= 0)
                {
                    throw new InvalidDataException($"The speech is not 16-bit PCM: format {format}, {bits} bits, {channels} channels, {rate} Hz.");
                }
                // A writer that streams may leave the size unset.
                var length = size <= 0 || size > wav.Length - body ? wav.Length - body : size;
                return (Mono(wav.Slice(body, length), channels), rate);
            }
            if (size < 0)
            {
                break;
            }
            position = body + size + (size & 1);
        }
        throw new InvalidDataException("The speech has no audio.");
    }

    /// <summary>Mono audio resampled to 48 kHz through a windowed-sinc filter.</summary>
    public static float[] To48k(float[] samples, int rate)
    {
        if (rate == Rate)
        {
            return samples;
        }
        var common = Gcd(rate, Rate);
        int up = Rate / common, down = rate / common;

        // The filter keeps what the slower of the two rates can carry and blocks the rest, with a
        // little room for its slope; 16 zero crossings a side keep it 58 dB down.
        var scale = Math.Min(1.0, (double)Rate / rate);
        var cutoff = 0.92 * scale;
        var reach = (int)Math.Ceiling(16 / scale);
        var taps = 2 * reach;
        var filter = new float[up * taps];
        for (var phase = 0; phase < up; phase++)
        {
            var offset = (double)phase / up;
            double sum = 0;
            for (var tap = 0; tap < taps; tap++)
            {
                var distance = tap - reach + 1 - offset;
                var weight = cutoff * Sinc(distance * cutoff) * Blackman(distance / reach);
                filter[(phase * taps) + tap] = (float)weight;
                sum += weight;
            }
            for (var tap = 0; tap < taps; tap++)
            {
                filter[(phase * taps) + tap] /= (float)sum;
            }
        }

        var output = new float[(int)((long)samples.Length * up / down)];
        for (var n = 0; n < output.Length; n++)
        {
            var position = (long)n * down;
            var first = (int)(position / up) - reach + 1;
            var row = (int)(position % up) * taps;
            double value = 0;
            for (var tap = 0; tap < taps; tap++)
            {
                var source = first + tap;
                if ((uint)source < (uint)samples.Length)
                {
                    value += samples[source] * filter[row + tap];
                }
            }
            output[n] = (float)value;
        }
        return output;
    }

    /// <summary>
    /// 48 kHz audio, mono or interleaved stereo, shaped into a call of interleaved 16-bit stereo;
    /// null when nothing in it is audible. <paramref name="cut"/> says the audio was cut off before
    /// its end: a sound still going there fades out, however short it is.
    /// </summary>
    public static short[]? Shape(float[] samples, int channels, bool cut = false)
    {
        var stereo = channels == 1 ? Stereo(samples) : samples;
        var peak = Peak(stereo);
        if (peak < SilenceFloor)
        {
            return null;
        }
        var first = FirstLoud(stereo, peak * TrimLevel);
        var last = LastLoud(stereo, peak * TrimLevel);
        var frames = Math.Min(last - first + 1, (int)(MaxLength.TotalSeconds * Rate));
        var call = stereo.AsSpan(first * 2, frames * 2).ToArray();
        // A sound that ended before the cut keeps its own ending; one cut while going fades out.
        var stillGoing = cut && (stereo.Length / 2) - 1 - last < FrameSamples;
        if (stillGoing || last - first + 1 > frames)
        {
            FadeOut(call);
        }

        var gain = Math.Min(Math.Pow(10, (Loudness - MomentaryMax(call)) / 20), PeakLimit / Peak(call));
        var total = LeadFrames + frames + TailFrames;
        total += (FrameSamples - (total % FrameSamples)) % FrameSamples;
        var output = new short[total * 2];
        for (var i = 0; i < call.Length; i++)
        {
            output[(LeadFrames * 2) + i] = (short)Math.Clamp(Math.Round(call[i] * gain * short.MaxValue), short.MinValue, short.MaxValue);
        }
        return output;
    }

    /// <summary>
    /// The loudness of the loudest 400 ms in LUFS (ITU-R BS.1770: K-weighted, channels summed), or of
    /// the whole sound when it is shorter.
    /// </summary>
    public static double MomentaryMax(float[] stereo)
    {
        var frames = stereo.Length / 2;
        var power = new double[frames];
        for (var channel = 0; channel < 2; channel++)
        {
            // The two K-weighting filters at 48 kHz: a high shelf, then a high pass.
            double x1 = 0, x2 = 0, y1 = 0, y2 = 0, z1 = 0, z2 = 0;
            for (var i = 0; i < frames; i++)
            {
                double x = stereo[(2 * i) + channel];
                var y = (1.53512485958697 * x) - (2.69169618940638 * x1) + (1.19839281085285 * x2) + (1.69065929318241 * y1) - (0.73248077421585 * y2);
                x2 = x1;
                x1 = x;
                var z = y - (2 * y1) + y2 + (1.99004745483398 * z1) - (0.99007225036621 * z2);
                y2 = y1;
                y1 = y;
                z2 = z1;
                z1 = z;
                power[i] += z * z;
            }
        }
        var window = Math.Min(frames, (int)(0.4 * Rate));
        double sum = 0, loudest = 0;
        for (var i = 0; i < frames; i++)
        {
            sum += power[i];
            if (i >= window)
            {
                sum -= power[i - window];
            }
            if (i >= window - 1)
            {
                loudest = Math.Max(loudest, sum / window);
            }
        }
        return -0.691 + (10 * Math.Log10(Math.Max(loudest, 1e-12)));
    }

    private static float[] Mono(ReadOnlySpan<byte> data, int channels)
    {
        var samples = new float[data.Length / (2 * channels)];
        for (var i = 0; i < samples.Length; i++)
        {
            var sum = 0;
            for (var channel = 0; channel < channels; channel++)
            {
                sum += BinaryPrimitives.ReadInt16LittleEndian(data[(((i * channels) + channel) * 2)..]);
            }
            samples[i] = sum / (32768f * channels);
        }
        return samples;
    }

    private static float[] Stereo(float[] mono)
    {
        var stereo = new float[mono.Length * 2];
        for (var i = 0; i < mono.Length; i++)
        {
            stereo[2 * i] = stereo[(2 * i) + 1] = mono[i];
        }
        return stereo;
    }

    private static int FirstLoud(float[] stereo, double level)
    {
        var frame = 0;
        while (Math.Abs(stereo[2 * frame]) <= level && Math.Abs(stereo[(2 * frame) + 1]) <= level)
        {
            frame++;
        }
        return frame;
    }

    private static int LastLoud(float[] stereo, double level)
    {
        var frame = (stereo.Length / 2) - 1;
        while (Math.Abs(stereo[2 * frame]) <= level && Math.Abs(stereo[(2 * frame) + 1]) <= level)
        {
            frame--;
        }
        return frame;
    }

    private static void FadeOut(float[] stereo)
    {
        var frames = stereo.Length / 2;
        var fade = Math.Min(FadeFrames, frames);
        for (var i = 0; i < fade; i++)
        {
            var gain = (float)(0.5 + (0.5 * Math.Cos(Math.PI * (i + 1) / fade)));
            var frame = frames - fade + i;
            stereo[2 * frame] *= gain;
            stereo[(2 * frame) + 1] *= gain;
        }
    }

    private static float Peak(float[] samples)
    {
        var peak = 0f;
        foreach (var sample in samples)
        {
            peak = Math.Max(peak, Math.Abs(sample));
        }
        return peak;
    }

    private static double Sinc(double x) => x == 0 ? 1 : Math.Sin(Math.PI * x) / (Math.PI * x);

    private static double Blackman(double x) =>
        Math.Abs(x) >= 1 ? 0 : 0.42 + (0.5 * Math.Cos(Math.PI * x)) + (0.08 * Math.Cos(2 * Math.PI * x));

    private static int Gcd(int a, int b) => b == 0 ? a : Gcd(b, a % b);
}
