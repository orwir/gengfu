namespace Gengfu.App.Speech;

/// <summary>
/// What a call plays when no voice can speak it: a small gong struck twice, as by the night watchman
/// Gengfu is named after. It is made here, so no sound file ships and every PC plays the same.
/// </summary>
internal static class Gong
{
    private const double Pitch = 330;
    private const double Attack = 0.004;

    // A hand gong's partials: ratio to the pitch, level, seconds to fade by 60 dB, and a detune in Hz
    // whose beating makes the partial shimmer.
    private static readonly (double Ratio, double Level, double Decay, double Detune)[] Partials =
    [
        (1.000, 1.00, 2.8, 0.45), (1.505, 0.62, 2.3, 0.6), (1.987, 0.52, 2.0, 0.8),
        (2.462, 0.40, 1.7, 0.9), (2.951, 0.33, 1.4, 1.1), (3.571, 0.25, 1.1, 1.3),
        (4.183, 0.18, 0.9, 1.5), (5.124, 0.12, 0.7, 1.8), (6.071, 0.08, 0.5, 2.0),
    ];

    /// <summary>The gong, mono at 48 kHz: 2.4 s, struck at the start and again, a little softer, 0.55 s in.</summary>
    public static float[] Render()
    {
        var samples = new float[(int)(2.4 * Pcm.Rate)];
        Strike(samples, 0, 1.0);
        Strike(samples, (int)(0.55 * Pcm.Rate), 0.9);
        FadeOut(samples, (int)(0.4 * Pcm.Rate));
        return samples;
    }

    private static void Strike(float[] samples, int start, double level)
    {
        foreach (var (ratio, partLevel, decay, detune) in Partials)
        {
            Ring(samples, start, Pitch * ratio, partLevel * level, decay, detune);
        }
        Thump(samples, start, level);
    }

    // A struck gong starts sharp and settles: each partial falls from 2.5 % above its pitch.
    private static void Ring(float[] samples, int start, double frequency, double level, double decay, double detune)
    {
        var fall = Math.Log(1000) / decay;
        double phase = 0, beat = 0;
        for (var i = 0; start + i < samples.Length; i++)
        {
            var t = (double)i / Pcm.Rate;
            var envelope = level * Math.Exp(-fall * t) * Math.Min(1, t / Attack);
            if (envelope < 1e-5 && t > Attack)
            {
                break;
            }
            var pitch = frequency * (1 + (0.025 * Math.Exp(-t / 0.08)));
            phase += 2 * Math.PI * pitch / Pcm.Rate;
            beat += 2 * Math.PI * (pitch + detune) / Pcm.Rate;
            samples[start + i] += (float)(envelope * 0.5 * (Math.Sin(phase) + Math.Sin(beat)));
        }
    }

    // The mallet: 60 ms of muffled noise that dies away within about 12 ms. A fixed seed keeps every
    // gong the same.
    private static void Thump(float[] samples, int start, double level)
    {
        var noise = 0x2545F491u;
        double muffled = 0;
        for (var i = 0; i < 0.06 * Pcm.Rate && start + i < samples.Length; i++)
        {
            noise ^= noise << 13;
            noise ^= noise >> 17;
            noise ^= noise << 5;
            muffled += 0.25 * ((noise / (double)uint.MaxValue * 2) - 1 - muffled);
            samples[start + i] += (float)(level * 0.35 * muffled * Math.Exp(-(double)i / Pcm.Rate / 0.012));
        }
    }

    private static void FadeOut(float[] samples, int length)
    {
        for (var i = 0; i < length; i++)
        {
            samples[samples.Length - length + i] *= (float)(0.5 + (0.5 * Math.Cos(Math.PI * i / length)));
        }
    }
}
