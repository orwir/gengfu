using Gengfu.App.Speech;

namespace Gengfu.App.Tests.Speech;

/// <summary>Windows' own speech, on whatever voices this machine has: none at all is a case too.</summary>
public sealed class WindowsSpeechTests
{
    [Fact]
    public void Every_installed_voice_speaks_a_line_as_16_bit_speech()
    {
        foreach (var voice in Voices.Installed())
        {
            var (samples, rate) = Pcm.ReadWav(WindowsSpeech.Speak("Jungle in thirty seconds", voice.Id));

            Assert.InRange(rate, 8000, 48000);
            Assert.True(samples.Length > rate / 2, $"{voice.Name} spoke for under half a second");
            Assert.True(Sound.Peak(samples) > 0.05, $"{voice.Name} was silent");
        }
    }

    [Fact]
    public void A_voice_that_is_not_installed_is_refused()
    {
        if (Voices.Installed().Count == 0)
        {
            Assert.ThrowsAny<Exception>(() => WindowsSpeech.Speak("Jungle", "no such voice"));
            return;
        }
        Assert.Throws<ArgumentException>(() => WindowsSpeech.Speak("Jungle", "no such voice"));
    }
}
