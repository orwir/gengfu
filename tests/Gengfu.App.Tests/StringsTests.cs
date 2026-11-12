using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Gengfu.App.Tests;

public sealed partial class StringsTests
{
    private static readonly IReadOnlyDictionary<string, string> English = Read(CultureInfo.InvariantCulture);
    private static readonly IReadOnlyDictionary<string, string> Russian = Read(CultureInfo.GetCultureInfo("ru"));

    [Fact]
    public void Every_string_ships_in_English_and_Russian()
    {
        Assert.Empty(English.Keys.Except(Russian.Keys));
        Assert.Empty(Russian.Keys.Except(English.Keys));
        Assert.Empty(English.Concat(Russian).Where(pair => string.IsNullOrWhiteSpace(pair.Value)).Select(pair => pair.Key));
    }

    [Fact]
    public void Each_Russian_string_takes_the_same_values_as_its_English_one()
    {
        var differ = English.Keys.Intersect(Russian.Keys)
            .Where(key => !Values(English[key]).SetEquals(Values(Russian[key])));
        Assert.Empty(differ);
    }

    // Only the language's own strings: a key Russian lacks would otherwise be read from English.
    private static Dictionary<string, string> Read(CultureInfo language)
    {
        var set = Strings.ResourceManager.GetResourceSet(language, createIfNotExists: true, tryParents: false)
            ?? throw new InvalidOperationException($"No strings were built for '{language.Name}'.");
        return set.Cast<DictionaryEntry>().ToDictionary(entry => (string)entry.Key, entry => (string)entry.Value!);
    }

    private static HashSet<string> Values(string text) => [.. Placeholder().Matches(text).Select(match => match.Groups[1].Value)];

    [GeneratedRegex(@"(?<!\{)\{(\d+)[^{}]*\}(?!\})")]
    private static partial Regex Placeholder();
}
