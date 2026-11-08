using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Gengfu.App.Speech;

namespace Gengfu.App.Tests.Speech;

/// <summary>Test sounds and the measurements the tests make on them.</summary>
internal static class Sound
{
    /// <summary>Whether this machine has Media Foundation: Windows N editions without the Media Feature Pack don't.</summary>
    public static bool MediaFoundationInstalled { get; } = MediaFoundationStarts();

    public static float[] Tone(double frequency, double seconds, int rate, double level = 0.5) =>
        [.. Enumerable.Range(0, (int)(seconds * rate)).Select(i => (float)(level * Math.Sin(2 * Math.PI * frequency * i / rate)))];

    public static float[] Silence(double seconds, int rate) => new float[(int)(seconds * rate)];

    public static float[] Stereo(float[] mono) => [.. mono.SelectMany(sample => new[] { sample, sample })];

    public static float[] Left(float[] stereo) => [.. stereo.Where((_, index) => index % 2 == 0)];

    public static float[] Left(short[] stereo) => [.. stereo.Where((_, index) => index % 2 == 0).Select(sample => sample / 32768f)];

    /// <summary>A WAV of 16-bit PCM, as Windows speech writes it.</summary>
    public static byte[] Wav(float[] samples, int rate, int channels = 1, int format = 1, int? dataSize = null)
    {
        var data = new byte[samples.Length * 2];
        for (var i = 0; i < samples.Length; i++)
        {
            BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(i * 2), (short)Math.Round(samples[i] * 32767));
        }
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8);
        writer.Write(36 + data.Length);
        writer.Write("WAVE"u8);
        writer.Write("fmt "u8);
        writer.Write(16);
        writer.Write((short)format);
        writer.Write((short)channels);
        writer.Write(rate);
        writer.Write(rate * channels * 2);
        writer.Write((short)(channels * 2));
        writer.Write((short)16);
        writer.Write("data"u8);
        writer.Write(dataSize ?? data.Length);
        writer.Write(data);
        writer.Flush();
        return stream.ToArray();
    }

    /// <summary>The strength of one frequency in the samples (Goertzel), as an amplitude.</summary>
    public static double Strength(float[] samples, double frequency, int rate)
    {
        var coefficient = 2 * Math.Cos(2 * Math.PI * frequency / rate);
        double previous = 0, beforeThat = 0;
        foreach (var sample in samples)
        {
            var current = sample + (coefficient * previous) - beforeThat;
            beforeThat = previous;
            previous = current;
        }
        var power = (previous * previous) + (beforeThat * beforeThat) - (coefficient * previous * beforeThat);
        return 2 * Math.Sqrt(Math.Max(power, 0)) / samples.Length;
    }

    public static double Rms(float[] samples) => Math.Sqrt(samples.Average(sample => (double)sample * sample));

    public static float Peak(float[] samples) => samples.Max(Math.Abs);

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

    /// <summary>The frames, decoded back to interleaved 48 kHz stereo, as listeners hear them.</summary>
    public static short[] Decode(IReadOnlyList<byte[]> frames)
    {
        using var decoder = Concentus.OpusCodecFactory.CreateDecoder(48000, 2);
        var pcm = new short[frames.Count * 960 * 2];
        for (var i = 0; i < frames.Count; i++)
        {
            decoder.Decode(frames[i], pcm.AsSpan(i * 960 * 2, 960 * 2), 960, false);
        }
        return pcm;
    }
}
