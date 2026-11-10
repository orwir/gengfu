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

    // Windows is faked here, so each case is the same on every machine; the two tests that make a
    // plain CallAudio use the real one.
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
        _readFile = _ => throw new InvalidDataException("Not a sound.");

        var clip = await _audio.RenderAsync(new CallSound.AudioFile(Touch("notes.txt")), English);

        Assert.Equal(CallProblem.FileUnreadable, clip.Problem);
        Assert.Equal("Not a sound.", clip.Detail);
        Assert.Same(await GongFrames(), clip.Frames);
    }

    [Fact]
    public async Task A_file_that_isn_t_on_this_PC_plays_the_gong_and_says_so_without_asking_Windows()
    {
        var clip = await _audio.RenderAsync(new CallSound.AudioFile(Path.Combine(_folder, "missing.mp3")), English);

        Assert.Equal(CallProblem.FileMissing, clip.Problem);
        Assert.Empty(_read);
        Assert.Same(await GongFrames(), clip.Frames);
    }

    [Fact]
    public async Task A_file_that_can_t_play_gives_way_to_what_plays_otherwise_and_says_why()
    {
        var words = new CallSound.Speech("Boss in thirty seconds");
        var spoken = await _audio.RenderAsync(words, English);

        var missing = await _audio.RenderAsync(new CallSound.AudioFile(Path.Combine(_folder, "boss.mp3"), words), English);
        _readFile = _ => throw new InvalidDataException("Not a sound.");
        var unreadable = await _audio.RenderAsync(new CallSound.AudioFile(Touch("notes.txt"), words), English);

        Assert.Equal((CallProblem.FileMissing, CallProblem.None), (missing.Problem, missing.Instead));
        Assert.Same(spoken.Frames, missing.Frames);
        Assert.Equal((CallProblem.FileUnreadable, "Not a sound.", CallProblem.None), (unreadable.Problem, unreadable.Detail, unreadable.Instead));
        Assert.Same(spoken.Frames, unreadable.Frames);
    }

    [Fact]
    public async Task When_what_plays_otherwise_can_t_either_the_gong_plays_and_the_clip_says_why()
    {
        _voices.Clear();

        var clip = await _audio.RenderAsync(new CallSound.AudioFile(Path.Combine(_folder, "boss.mp3"), new CallSound.Speech("Boss in thirty seconds")), English);

        Assert.Equal((CallProblem.FileMissing, CallProblem.NoVoice), (clip.Problem, clip.Instead));
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
    public async Task A_file_Windows_cannot_read_says_why_in_Windows_own_words()
    {
        var clip = await new CallAudio().RenderAsync(new CallSound.AudioFile(Touch("notes.mp3")), English);

        Assert.Equal(CallProblem.FileUnreadable, clip.Problem);
        if (Sound.MediaFoundationInstalled)
        {
            Assert.Matches("0x[0-9A-F]{8}", clip.Detail);
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
    public async Task The_parts_of_one_call_play_in_turn_a_sentence_s_pause_apart_with_nothing_heard_between()
    {
        _speak = (text, _) => Sound.Wav(Sound.Tone(text == "one" ? 300 : 600, 0.5, 16000), 16000);
        var one = (await _audio.RenderAsync(new CallSound.Speech("one"), English)).Frames;
        var two = (await _audio.RenderAsync(new CallSound.Speech("two"), English)).Frames;

        var call = CallAudio.Join([one, two]);

        Assert.Same(one, CallAudio.Join([one]));
        var pause = call.Count - one.Count - two.Count;
        Assert.Equal(TimeSpan.FromSeconds(0.7), pause * TeamSpeak.VoiceSender.FrameLength);
        var heard = Sound.Left(Sound.Decode(call));
        var frame = heard.Length / call.Count;
        Assert.True(Sound.Strength(heard[..(one.Count * frame)], 300, 48000) > 0.1);
        Assert.True(Sound.Strength(heard[((one.Count + pause) * frame)..], 600, 48000) > 0.1);
        // Where one's frames meet the other's, the first part's ending only fades, as the decoder
        // lets it out a moment late; then silence through the pause and the next part's lead-in.
        var end = one.Count * frame;
        Assert.True(Sound.Peak(heard[end..(end + frame)]) <= Sound.Peak(heard[(end - (frame / 4))..end]));
        Assert.InRange(Sound.Peak(heard[(end + frame)..((one.Count + pause + 4) * frame)]), 0, 0.001);
    }

    [Fact]
    public async Task Parts_of_one_call_that_sound_the_same_play_once()
    {
        var path = Touch("boss.mp3");
        var words = (await _audio.RenderAsync(new CallSound.Speech("Jungle in thirty seconds"), English)).Frames;
        var file = (await _audio.RenderAsync(new CallSound.AudioFile(path, new CallSound.Speech("Boss in thirty seconds")), English)).Frames;
        // The same file for another event at the spawn time, rendered afresh: other frames, the same sound.
        File.SetLastWriteTimeUtc(path, File.GetLastWriteTimeUtc(path).AddMinutes(1));
        var again = (await _audio.RenderAsync(new CallSound.AudioFile(path, new CallSound.Speech("Supply in thirty seconds")), English)).Frames;
        Assert.NotSame(file, again);

        Assert.Equal(CallAudio.Join([words, file]), CallAudio.Join([words, file, again]));

        // With no voice, the gong plays in place of each part's words: once.
        _voices.Clear();
        var gongs = await Task.WhenAll(new[] { "Jungle and Boss in thirty seconds", "Boss soon" }.Select(text => _audio.RenderAsync(new CallSound.Speech(text), English)));
        Assert.All(gongs, clip => Assert.Equal(CallProblem.NoVoice, clip.Problem));
        Assert.Equal(await GongFrames(), CallAudio.Join([.. gongs.Select(clip => clip.Frames)]));
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
