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
    public sealed record AudioFile(string Path) : CallSound;

    /// <summary>The gong, chosen over a voice.</summary>
    public sealed record Gong : CallSound;
}

/// <summary>Why a call plays the gong in place of what it was given.</summary>
public enum CallProblem
{
    None,

    /// <summary>No voice is installed in the app's language and none was chosen; Settings says how to add one.</summary>
    NoVoice,

    NoText,

    /// <summary>Windows couldn't speak the text; <see cref="CallClip.Detail"/> holds its error.</summary>
    SpeechFailed,

    /// <summary>The file is missing, silent, or not something Windows can play; <see cref="CallClip.Detail"/> holds the error.</summary>
    FileUnreadable,
}

/// <summary>A call ready to whisper: its 20 ms Opus frames, and why it is the gong when it is.</summary>
public sealed record CallClip(IReadOnlyList<byte[]> Frames, CallProblem Problem = CallProblem.None, string? Detail = null);

/// <summary>
/// Renders calls ahead of time and keeps them: text through Windows speech or an audio file through
/// Media Foundation, shaped to 48 kHz stereo (<see cref="Pcm"/>), then 20 ms Opus frames at 48 kbps.
/// A call is never silent: what can't be spoken or read plays the gong, and the clip says why.
/// Renders run one at a time on the thread pool.
/// </summary>
public sealed class CallAudio
{
    /// <summary>Text past this is never spoken: the call would be cut at <see cref="Pcm.MaxLength"/> anyway.</summary>
    private const int MaxTextLength = 500;

    // A match uses a sound or two; a cache that outgrows this is cleared rather than tracked.
    private const int MaxClips = 32;

    private const int Bitrate = 48000;

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
        CallSound.AudioFile file => RenderFile(file.Path),
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

    private CallClip RenderFile(string path)
    {
        try
        {
            // A file edited in place renders afresh.
            var file = new FileInfo(path);
            return new CallClip(Keep(("file", file.FullName, file.LastWriteTimeUtc, file.Exists ? file.Length : -1), () =>
            {
                var (samples, cut) = _read(file.FullName, Pcm.MaxLength);
                return Pcm.Shape(samples, 2, cut) ?? throw new InvalidDataException("The file is silent.");
            }));
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return new CallClip(GongFrames(), CallProblem.FileUnreadable, exception.Message);
        }
    }

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
