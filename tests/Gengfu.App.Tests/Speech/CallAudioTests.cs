using System.Globalization;
using System.Runtime.InteropServices;
using Gengfu.App.Speech;

namespace Gengfu.App.Tests.Speech;

public sealed class CallAudioTests : IDisposable
{
    private static readonly CultureInfo English = new("en-US");
    private static readonly CultureInfo Russian = new("ru-RU");
    private static readonly Voice David = new("david", "Microsoft David", "en-US");
    private static readonly Voice Irina = new("irina", "Microsoft Irina", "ru-RU");

    private readonly List<Voice> _voices = [David, Irina];
    private readonly List<(string Text, string VoiceId)> _spoken = [];
    private readonly List<(string Path, TimeSpan Limit)> _read = [];
    private readonly string _folder = Directory.CreateTempSubdirectory("gengfu-").FullName;
    private readonly CallAudio _audio;
    private Func<string, string, byte[]> _speak = (_, _) => Sound.Wav(Sound.Tone(300, 0.5, 16000), 16000);
    private Func<string, (float[] Samples, bool Cut)> _readFile = path =>
        File.Exists(path) ? (Sound.Stereo(Sound.Tone(500, 0.5, 48000)), false) : throw new FileNotFoundException("No such file.", path);

    // Windows is faked here, so each case is the same on every machine; the last test uses the real one.
    public CallAudioTests() =>
        _audio = new CallAudio(
            () => [.. _voices],
            (text, voiceId) =>
            {
                _spoken.Add((text, voiceId));
                return _speak(text, voiceId);
            },
            (path, limit) =>
            {
                _read.Add((path, limit));
                return _readFile(path);
            });

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public async Task Text_is_spoken_by_a_voice_in_the_app_language()
    {
        var clip = await _audio.RenderAsync(new CallSound.Speech("Лес через тридцать секунд"), Russian);

        Assert.Equal(CallProblem.None, clip.Problem);
        Assert.Equal(new[] { ("Лес через тридцать секунд", "irina") }, _spoken);
        // 100 ms ahead, the 0.5 s line, then a frame of silence.
        Assert.InRange(clip.Frames.Count, 31, 32);
    }

    [Fact]
    public async Task The_chosen_voice_speaks_whatever_the_app_language()
    {
        await _audio.RenderAsync(new CallSound.Speech("Jungle in thirty seconds", "david"), Russian);

        Assert.Equal(new[] { ("Jungle in thirty seconds", "david") }, _spoken);
    }

    [Fact]
    public async Task One_text_in_another_voice_is_spoken_again()
    {
        await _audio.RenderAsync(new CallSound.Speech("Jungle", "david"), English);
        await _audio.RenderAsync(new CallSound.Speech("Jungle", "irina"), English);

        Assert.Equal(new[] { ("Jungle", "david"), ("Jungle", "irina") }, _spoken);
    }

    [Fact]
    public async Task Without_a_voice_in_the_app_language_the_call_is_the_gong_and_says_why()
    {
        _voices.Remove(Irina);

        var clip = await _audio.RenderAsync(new CallSound.Speech("Лес через тридцать секунд"), Russian);

        Assert.Equal(CallProblem.NoVoice, clip.Problem);
        Assert.Same(await GongFrames(), clip.Frames);
        Assert.Empty(_spoken);
    }

    [Fact]
    public async Task A_voice_added_later_takes_over_from_the_gong()
    {
        _voices.Remove(Irina);
        await _audio.RenderAsync(new CallSound.Speech("Лес"), Russian);
        _voices.Add(Irina);

        var clip = await _audio.RenderAsync(new CallSound.Speech("Лес"), Russian);

        Assert.Equal(CallProblem.None, clip.Problem);
        Assert.Equal(new[] { ("Лес", "irina") }, _spoken);
    }

    [Fact]
    public async Task Empty_text_plays_the_gong()
    {
        var clip = await _audio.RenderAsync(new CallSound.Speech("   "), English);

        Assert.Equal(CallProblem.NoText, clip.Problem);
        Assert.Same(await GongFrames(), clip.Frames);
    }

    [Fact]
    public async Task When_Windows_cannot_speak_the_call_is_the_gong_with_its_error()
    {
        _speak = (_, _) => throw new COMException("The speech engine broke.");

        var clip = await _audio.RenderAsync(new CallSound.Speech("Jungle"), English);

        Assert.Equal(CallProblem.SpeechFailed, clip.Problem);
        Assert.Equal("The speech engine broke.", clip.Detail);
        Assert.Same(await GongFrames(), clip.Frames);
    }

    [Fact]
    public async Task Speech_that_comes_back_silent_plays_the_gong()
    {
        _speak = (_, _) => Sound.Wav(Sound.Silence(0.5, 16000), 16000);

        var clip = await _audio.RenderAsync(new CallSound.Speech("Jungle"), English);

        Assert.Equal(CallProblem.SpeechFailed, clip.Problem);
    }

    [Fact]
    public async Task Text_past_500_characters_is_never_spoken()
    {
        await _audio.RenderAsync(new CallSound.Speech(new string('a', 600)), English);

        Assert.Equal(500, Assert.Single(_spoken).Text.Length);
    }

    [Fact]
    public async Task An_audio_file_plays_as_the_call()
    {
        var path = Touch("jungle.mp3");

        var clip = await _audio.RenderAsync(new CallSound.AudioFile(path), English);

        Assert.Equal(CallProblem.None, clip.Problem);
        Assert.Equal(new[] { (path, TimeSpan.FromSeconds(10)) }, _read);
        Assert.InRange(clip.Frames.Count, 31, 32);
    }

    [Fact]
    public async Task A_file_Windows_cannot_read_plays_the_gong_with_its_error()
    {
        var clip = await _audio.RenderAsync(new CallSound.AudioFile(Path.Combine(_folder, "missing.mp3")), English);

        Assert.Equal(CallProblem.FileUnreadable, clip.Problem);
        Assert.Equal("No such file.", clip.Detail);
        Assert.Same(await GongFrames(), clip.Frames);
    }

    [Fact]
    public async Task A_file_that_goes_on_past_the_read_fades_out()
    {
        _readFile = _ => (Sound.Stereo(Sound.Tone(500, 3, 48000)), true);

        var clip = await _audio.RenderAsync(new CallSound.AudioFile(Touch("song.mp3")), English);

        // The 3 s of sound sit 100 ms in; Opus delays them by a few milliseconds more.
        var heard = Sound.Left(Sound.Decode(clip.Frames));
        var start = Sound.Rms(heard[9600..19200]);
        var end = Sound.Rms(heard[(4800 + 144000 - 1200)..(4800 + 144000 - 240)]);
        Assert.True(end < start / 10, $"the end is {end / start:P0} of the start");
    }

    [Fact]
    public async Task A_missing_file_says_why_in_Windows_own_words()
    {
        var clip = await new CallAudio().RenderAsync(new CallSound.AudioFile(Path.Combine(_folder, "missing.mp3")), English);

        Assert.Equal(CallProblem.FileUnreadable, clip.Problem);
        if (Sound.MediaFoundationInstalled)
        {
            Assert.Contains("0x80070002", clip.Detail);
        }
        else
        {
            Assert.False(string.IsNullOrWhiteSpace(clip.Detail));
        }
    }

    [Fact]
    public async Task A_silent_file_plays_the_gong()
    {
        _readFile = _ => (Sound.Silence(1, 48000), false);

        var clip = await _audio.RenderAsync(new CallSound.AudioFile(Touch("silence.wav")), English);

        Assert.Equal(CallProblem.FileUnreadable, clip.Problem);
    }

    [Fact]
    public async Task A_call_rendered_once_is_kept()
    {
        var first = await _audio.RenderAsync(new CallSound.Speech("Jungle"), English);
        var second = await _audio.RenderAsync(new CallSound.Speech("Jungle"), English);

        Assert.Single(_spoken);
        Assert.Same(first.Frames, second.Frames);
    }

    [Fact]
    public async Task A_cache_grown_past_32_sounds_starts_over()
    {
        for (var line = 0; line <= 32; line++)
        {
            await _audio.RenderAsync(new CallSound.Speech($"line {line}"), English);
        }

        await _audio.RenderAsync(new CallSound.Speech("line 0"), English);
        await _audio.RenderAsync(new CallSound.Speech("line 32"), English);

        Assert.Equal(34, _spoken.Count);
    }

    [Fact]
    public async Task A_file_edited_since_renders_afresh()
    {
        var path = Touch("jungle.wav");
        await _audio.RenderAsync(new CallSound.AudioFile(path), English);
        File.SetLastWriteTimeUtc(path, File.GetLastWriteTimeUtc(path).AddMinutes(1));

        await _audio.RenderAsync(new CallSound.AudioFile(path), English);

        Assert.Equal(2, _read.Count);
    }

    [Fact]
    public async Task The_gong_by_choice_plays_with_nothing_wrong()
    {
        var clip = await _audio.RenderAsync(new CallSound.Gong(), Russian);

        Assert.Equal(CallProblem.None, clip.Problem);
        // 100 ms ahead, about 2.3 s of gong, then a frame of silence.
        Assert.InRange(clip.Frames.Count, 115, 125);
    }

    [Fact]
    public async Task Renders_run_one_at_a_time()
    {
        var running = 0;
        var most = 0;
        _speak = (_, _) =>
        {
            most = Math.Max(most, Interlocked.Increment(ref running));
            Thread.Sleep(50);
            Interlocked.Decrement(ref running);
            return Sound.Wav(Sound.Tone(300, 0.5, 16000), 16000);
        };

        await Task.WhenAll(new[] { "one", "two", "three" }.Select(text => _audio.RenderAsync(new CallSound.Speech(text), English)));

        Assert.Equal(1, most);
        Assert.Equal(3, _spoken.Count);
    }

    [Fact]
    public void Frames_decode_back_to_the_sound_at_the_shared_loudness()
    {
        var pcm = Pcm.Shape(Sound.Tone(440, 1, 48000), 1)!;

        var frames = CallAudio.Encode(pcm);

        Assert.Equal(pcm.Length / 1920, frames.Count);
        Assert.All(frames, frame => Assert.NotEmpty(frame));
        Assert.Equal(-16, Pcm.MomentaryMax([.. Sound.Decode(frames).Select(sample => sample / 32768f)]), 0.5);
    }

    [Fact]
    public async Task The_preset_lines_render_on_this_machine_in_a_voice_or_as_the_gong()
    {
        var audio = new CallAudio();
        var installed = Voices.Installed();

        foreach (var (line, language) in new[] { ("Jungle in thirty seconds", English), ("Лес через тридцать секунд", Russian) })
        {
            var clip = await audio.RenderAsync(new CallSound.Speech(line), language);

            var speaks = Voices.Pick(installed, null, language) is not null;
            Assert.Equal(speaks ? CallProblem.None : CallProblem.NoVoice, clip.Problem);
            Assert.True(clip.Frames.Count > 50, $"{line}: {clip.Frames.Count} frames");
        }
    }

    private async Task<IReadOnlyList<byte[]>> GongFrames() => (await _audio.RenderAsync(new CallSound.Gong(), English)).Frames;

    private string Touch(string name)
    {
        var path = Path.Combine(_folder, name);
        File.WriteAllBytes(path, [1]);
        return path;
    }
}
