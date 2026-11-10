using System.Globalization;
using System.IO;
using Concentus;
using Concentus.Enums;

namespace Gengfu.App.Speech;

/// <summary>What a call plays.</summary>
public abstract record CallSound
{
    private CallSound()
    {
    }

    /// <summary>Text spoken by a Windows voice.</summary>
    /// <param name="VoiceId">The voice the user chose; null picks one by the app's language.</param>
    public sealed record Speech(string Text, string? VoiceId = null) : CallSound;

    /// <summary>An audio file the user picked.</summary>
    /// <param name="Otherwise">What plays when the file can't: an event's words; null plays the gong.</param>
    public sealed record AudioFile(string Path, CallSound? Otherwise = null) : CallSound;

    /// <summary>The gong, chosen over a voice.</summary>
    public sealed record Gong : CallSound;
}

/// <summary>Why a call isn't what it was given: the gong in its place, or for a file that can't play, what plays otherwise.</summary>
public enum CallProblem
{
    None,

    /// <summary>No voice is installed in the app's language and none was chosen; Settings says how to add one.</summary>
    NoVoice,

    NoText,

    /// <summary>Windows couldn't speak the text; <see cref="CallClip.Detail"/> holds its error.</summary>
    SpeechFailed,

    /// <summary>The file is silent, or not something Windows can play; <see cref="CallClip.Detail"/> holds the error.</summary>
    FileUnreadable,

    /// <summary>The file isn't on this PC, as when a preset made on another names it.</summary>
    FileMissing,
}

/// <summary>
/// A call ready to whisper: its 20 ms Opus frames, and why it isn't what it was given when it isn't:
/// the gong, or for a file that can't play, what plays in its place.
/// </summary>
/// <param name="Instead">For a file that can't play, why what plays in its place isn't what it was given either, so the gong plays.</param>
public sealed record CallClip(IReadOnlyList<byte[]> Frames, CallProblem Problem = CallProblem.None, string? Detail = null, CallProblem Instead = CallProblem.None);

/// <summary>
/// Renders calls ahead of time and keeps them: text through Windows speech or an audio file through
/// Media Foundation, shaped to 48 kHz stereo (<see cref="Pcm"/>), then 20 ms Opus frames at 48 kbps.
/// A call is never silent: a file that can't play gives way to what the sound says plays otherwise,
/// and what can't be spoken plays the gong; the clip says why. Renders run one at a time on the
/// thread pool.
/// </summary>
public sealed class CallAudio
{
    /// <summary>Text past this is never spoken: its sound would be cut at <see cref="Pcm.MaxLength"/> anyway.</summary>
    private const int MaxTextLength = 500;

    // A match uses a sound or two; a cache that outgrows this is cleared rather than tracked.
    private const int MaxClips = 32;

    private const int Bitrate = 48000;

    // With each part's own lead-in, about the pause Windows' voices leave between two sentences, so
    // the parts of a call don't run together.
    private static readonly Lazy<IReadOnlyList<byte[]>> Pause = new(() => Encode(new short[Pcm.Rate * 7 / 10 * 2]));

    private readonly Func<IReadOnlyList<Voice>> _voices;
    private readonly Func<string, string, byte[]> _speak;
    private readonly Func<string, TimeSpan, (float[] Samples, bool Cut)> _read;
    private readonly SemaphoreSlim _rendering = new(1, 1);
    private readonly Dictionary<object, IReadOnlyList<byte[]>> _clips = [];

    public CallAudio()
        : this(Voices.Installed, WindowsSpeech.Speak, MediaFoundation.Read)
    {
    }

    internal CallAudio(Func<IReadOnlyList<Voice>> voices, Func<string, string, byte[]> speak, Func<string, TimeSpan, (float[] Samples, bool Cut)> read)
    {
        _voices = voices;
        _speak = speak;
        _read = read;
    }

    /// <summary>The installed voices, as the calls are spoken with them.</summary>
    public IReadOnlyList<Voice> InstalledVoices() => _voices();

    /// <summary>The call's frames, rendered now or kept from before; a voice picked by <paramref name="language"/> when none is chosen.</summary>
    public async Task<CallClip> RenderAsync(CallSound sound, CultureInfo language)
    {
        await _rendering.WaitAsync();
        try
        {
            return await Task.Run(() => Render(sound, language));
        }
        finally
        {
            _rendering.Release();
        }
    }

    /// <summary>
    /// One call of several parts, each as rendered, one after another with a pause between. Parts
    /// that sound the same, such as a file two events share or the gong in place of words, play
    /// once. Each part starts and ends in silence, so the frames of one follow the other's cleanly.
    /// </summary>
    public static IReadOnlyList<byte[]> Join(IReadOnlyList<IReadOnlyList<byte[]>> parts)
    {
        List<IReadOnlyList<byte[]>> heard = [];
        foreach (var part in parts)
        {
            if (!heard.Any(earlier => SoundsAlike(earlier, part)))
            {
                heard.Add(part);
            }
        }
        return heard.Count == 1 ? heard[0] : [.. heard.SelectMany((part, index) => index == 0 ? part : Pause.Value.Concat(part))];
    }

    /// <summary>20 ms Opus frames of interleaved 48 kHz stereo, which comes in whole frames.</summary>
    internal static IReadOnlyList<byte[]> Encode(short[] pcm)
    {
        // The managed encoder only: a native libopus would need the Visual C++ runtime (ADR-0005).
        OpusCodecFactory.AttemptToUseNativeLibrary = false;
        using var encoder = OpusCodecFactory.CreateEncoder(Pcm.Rate, 2, OpusApplication.OPUS_APPLICATION_AUDIO);
        encoder.Bitrate = Bitrate;
        var frame = Pcm.FrameSamples * 2;
        var frames = new List<byte[]>(pcm.Length / frame);
        var packet = new byte[1275];
        for (var offset = 0; offset + frame <= pcm.Length; offset += frame)
        {
            var length = encoder.Encode(pcm.AsSpan(offset, frame), Pcm.FrameSamples, packet, packet.Length);
            frames.Add(packet[..length]);
        }
        return frames;
    }

    private CallClip Render(CallSound sound, CultureInfo language) => sound switch
    {
        CallSound.Speech speech => RenderSpeech(speech, language),
        CallSound.AudioFile file => RenderFile(file, language),
        _ => new CallClip(GongFrames()),
    };

    private CallClip RenderSpeech(CallSound.Speech speech, CultureInfo language)
    {
        var text = speech.Text.Trim();
        if (text.Length == 0)
        {
            return new CallClip(GongFrames(), CallProblem.NoText);
        }
        if (Voices.Pick(_voices(), speech.VoiceId, language) is not { } voice)
        {
            return new CallClip(GongFrames(), CallProblem.NoVoice);
        }
        if (text.Length > MaxTextLength)
        {
            text = text[..MaxTextLength];
        }
        try
        {
            return new CallClip(Keep(("speech", voice.Id, text), () =>
            {
                var (samples, rate) = Pcm.ReadWav(_speak(text, voice.Id));
                return Pcm.Shape(Pcm.To48k(samples, rate), 1) ?? throw new InvalidDataException("The voice said nothing.");
            }));
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return new CallClip(GongFrames(), CallProblem.SpeechFailed, exception.Message);
        }
    }

    // A file that can't play gives way to what plays otherwise. The clip names the file's problem,
    // and what kept the other from playing too, when something did.
    private CallClip RenderFile(CallSound.AudioFile sound, CultureInfo language)
    {
        CallProblem problem;
        string? detail = null;
        try
        {
            // A file edited in place renders afresh.
            var file = new FileInfo(sound.Path);
            if (file.Exists)
            {
                return new CallClip(Keep(("file", file.FullName, file.LastWriteTimeUtc, file.Length), () =>
                {
                    var (samples, cut) = _read(file.FullName, Pcm.MaxLength);
                    return Pcm.Shape(samples, 2, cut) ?? throw new InvalidDataException("The file is silent.");
                }));
            }
            problem = CallProblem.FileMissing;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            (problem, detail) = (CallProblem.FileUnreadable, exception.Message);
        }
        var otherwise = sound.Otherwise is null ? new CallClip(GongFrames()) : Render(sound.Otherwise, language);
        return otherwise with { Problem = problem, Detail = detail, Instead = otherwise.Problem };
    }

    // The encoder gives the same sound the same frames, however often it is rendered.
    private static bool SoundsAlike(IReadOnlyList<byte[]> one, IReadOnlyList<byte[]> other) =>
        ReferenceEquals(one, other) || (one.Count == other.Count && one.Zip(other).All(frames => frames.First.AsSpan().SequenceEqual(frames.Second)));

    private IReadOnlyList<byte[]> GongFrames() => Keep("gong", () => Pcm.Shape(Gong.Render(), 1)!);

    private IReadOnlyList<byte[]> Keep(object key, Func<short[]> render)
    {
        if (_clips.TryGetValue(key, out var kept))
        {
            return kept;
        }
        var frames = Encode(render());
        if (_clips.Count >= MaxClips)
        {
            _clips.Clear();
        }
        _clips[key] = frames;
        return frames;
    }
}
