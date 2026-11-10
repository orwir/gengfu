using System.Globalization;

namespace Gengfu.App.Speech;

/// <summary>A Windows voice: its id, its name as Windows shows it, and its language as a BCP 47 tag such as "en-US".</summary>
public sealed record Voice(string Id, string Name, string Language);

/// <summary>Which Windows voices there are, and which one a call speaks with.</summary>
public static class Voices
{
    /// <summary>
    /// The installed voices, Windows' default first; none when Windows has no speech. Read afresh each
    /// time, so a voice added in Windows counts without a restart.
    /// </summary>
    public static IReadOnlyList<Voice> Installed()
    {
        try
        {
            return WindowsSpeech.Voices();
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // Windows without speech: the calls play the gong.
            return [];
        }
    }

    /// <summary>
    /// The voice a call speaks with: the chosen one while it is installed, else the first in the app's
    /// language, else none, and the call plays the gong. Any voice may be chosen whatever the app's
    /// language; matching the text to it is the user's part.
    /// </summary>
    public static Voice? Pick(IReadOnlyList<Voice> installed, string? chosenId, CultureInfo language) =>
        installed.FirstOrDefault(voice => voice.Id == chosenId) ?? installed.FirstOrDefault(voice => Speaks(voice, language));

    /// <summary>The voices as the list shows them: those in the app's language first, each group in Windows' order.</summary>
    public static IReadOnlyList<Voice> ForList(IReadOnlyList<Voice> installed, CultureInfo language) =>
        [.. installed.OrderBy(voice => Speaks(voice, language) ? 0 : 1)];

    internal static bool Speaks(Voice voice, CultureInfo language) =>
        string.Equals(voice.Language.Split('-')[0], language.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase);
}
