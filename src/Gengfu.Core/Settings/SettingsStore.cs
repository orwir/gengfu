using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gengfu.Core.Settings;

/// <summary>Where the settings file went wrong.</summary>
/// <param name="Line">Counted from 1; null when the file couldn't be read at all.</param>
/// <param name="Setting">The setting as the file spells it, such as "events[1].spawns"; null outside any.</param>
public sealed record SettingsError(int? Line, string? Setting);

/// <summary>What the settings file held when it was last read.</summary>
public abstract record SettingsLoad
{
    private SettingsLoad()
    {
    }

    /// <summary>There is no file: the defaults apply until a save makes one.</summary>
    public sealed record Missing : SettingsLoad;

    /// <summary>The file holds what Gengfu last read or wrote.</summary>
    public sealed record Unchanged : SettingsLoad;

    /// <summary>Windows wouldn't let Gengfu read the file, perhaps for another program holding it.</summary>
    public sealed record Unreadable : SettingsLoad;

    public sealed record Loaded(AppSettings Settings) : SettingsLoad;

    public sealed record Broken(SettingsError Error) : SettingsLoad;
}

/// <summary>
/// The settings file: JSON a person can read and edit, comments and trailing commas allowed. Gengfu
/// writes it whole, so comments go with the next save. Every value is checked on reading, and a
/// misspelt setting or one written twice is an error too, so a hand edit never goes wrong unseen.
/// </summary>
public sealed class SettingsStore(string filePath)
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        // Russian names stay readable: the file is never put into a web page.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        AllowDuplicateProperties = false,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
    };

    public string FilePath { get; } = filePath;

    /// <summary>The file's text as Gengfu last read or wrote it; null while there is no file.</summary>
    public string? Known { get; private set; }

    public static string Serialize(AppSettings settings) => JsonSerializer.Serialize(settings, Json);

    /// <summary>Reads the file afresh.</summary>
    public SettingsLoad Load()
    {
        string text;
        try
        {
            // Shared with whatever else has the file open, such as the editor saving it.
            using var reader = new StreamReader(new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete));
            text = reader.ReadToEnd();
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            Known = null;
            return new SettingsLoad.Missing();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new SettingsLoad.Unreadable();
        }
        if (text == Known)
        {
            return new SettingsLoad.Unchanged();
        }
        Known = text;
        return Parse(text);
    }

    /// <summary>Writes the settings; false when Windows refused, for the caller to try again later.</summary>
    public bool Save(AppSettings settings)
    {
        var text = Serialize(settings);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            // Written beside the file and moved over it, so the file is never half written.
            var temp = FilePath + ".tmp";
            File.WriteAllText(temp, text);
            File.Move(temp, FilePath, overwrite: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
        Known = text;
        return true;
    }

    /// <summary>Calls <paramref name="changed"/>, on a thread-pool thread, whenever the file may have changed on disk.</summary>
    public IDisposable Watch(Action changed)
    {
        var folder = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(folder);
        var watcher = new FileSystemWatcher(folder, Path.GetFileName(FilePath))
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
        };
        watcher.Changed += (_, _) => changed();
        watcher.Created += (_, _) => changed();
        watcher.Deleted += (_, _) => changed();
        watcher.Renamed += (_, _) => changed();
        watcher.EnableRaisingEvents = true;
        return watcher;
    }

    internal static SettingsLoad Parse(string text)
    {
        AppSettings? settings;
        try
        {
            settings = JsonSerializer.Deserialize<AppSettings>(text, Json);
        }
        catch (JsonException exception)
        {
            // The reader counts lines from 0, and its paths start at "$", the file as a whole.
            var setting = exception.Path is { Length: > 1 } path ? path[1..].TrimStart('.') : null;
            return new SettingsLoad.Broken(new SettingsError((int)(exception.LineNumber ?? 0) + 1, setting));
        }
        return settings is null ? new SettingsLoad.Broken(new SettingsError(1, null))
            : NullElement(text) is { } error ? new SettingsLoad.Broken(error)
            : new SettingsLoad.Loaded(settings);
    }

    // The serializer lets a null element into a list whatever the list's type says, so the events
    // and the channels are looked through for one.
    private static SettingsError? NullElement(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var reader = new Utf8JsonReader(bytes, new JsonReaderOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        string? list = null;
        var index = 0;
        while (reader.Read())
        {
            if (reader.CurrentDepth == 1 && reader.TokenType == JsonTokenType.PropertyName)
            {
                (list, index) = (reader.GetString(), 0);
            }
            else if (reader.CurrentDepth == 2 && list is "events" or "channels")
            {
                if (reader.TokenType == JsonTokenType.Null)
                {
                    return new SettingsError(bytes.AsSpan(0, (int)reader.TokenStartIndex).Count((byte)'\n') + 1, $"{list}[{index}]");
                }
                index += reader.TokenType == JsonTokenType.StartObject ? 1 : 0;
            }
        }
        return null;
    }
}

/// <summary>The lead in whole seconds, one of those Settings offers.</summary>
internal sealed class LeadConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var seconds)
        && AppSettings.Leads.Contains(TimeSpan.FromSeconds(seconds))
            ? TimeSpan.FromSeconds(seconds)
            : throw new JsonException();

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options) =>
        writer.WriteNumberValue((int)value.TotalSeconds);
}

/// <summary>Spawn times as the game shows them, such as "25:00", written on one line.</summary>
internal sealed class SpawnsConverter : JsonConverter<IReadOnlyList<TimeSpan>>
{
    public override IReadOnlyList<TimeSpan> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException();
        }
        List<TimeSpan> spawns = [];
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            spawns.Add(reader.TokenType == JsonTokenType.String && Parse(reader.GetString()!) is { } spawn ? spawn : throw new JsonException());
        }
        return spawns;
    }

    public override void Write(Utf8JsonWriter writer, IReadOnlyList<TimeSpan> value, JsonSerializerOptions options) =>
        writer.WriteRawValue("[" + string.Join(", ", value.Select(spawn => "\"" + MatchClock.Format(spawn) + "\"")) + "]");

    /// <summary>"5:00" and "05:00" alike; the game's timer shows nothing above 35:00.</summary>
    internal static TimeSpan? Parse(string text) =>
        text.Split(':') is [{ Length: 1 or 2 } minutesText, { Length: 2 } secondsText]
        && int.TryParse(minutesText, NumberStyles.None, CultureInfo.InvariantCulture, out var minutes)
        && int.TryParse(secondsText, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds)
        && seconds < 60
        && new TimeSpan(0, minutes, seconds) is var spawn
        && spawn <= MatchClock.MatchLength
            ? spawn
            : null;
}

/// <summary>"en" or "ru"; null, which never reaches a converter, follows Windows.</summary>
internal sealed class LanguageConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String && reader.GetString() is "en" or "ru" ? reader.GetString()! : throw new JsonException();

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) => writer.WriteStringValue(value);
}

/// <summary>
/// "light" or "dark", spelt exactly; null, which never reaches a converter, follows Windows. The
/// stock enum converter would also read "Dark", " dark" or "light, dark", so a slip would pass unseen.
/// </summary>
internal sealed class ThemeConverter : JsonConverter<Theme>
{
    public override Theme Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType != JsonTokenType.String ? throw new JsonException()
        : reader.GetString() switch
        {
            "light" => Theme.Light,
            "dark" => Theme.Dark,
            _ => throw new JsonException(),
        };

    public override void Write(Utf8JsonWriter writer, Theme value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value == Theme.Light ? "light" : "dark");
}

/// <summary>"group" or "channels", spelt exactly, for the reason <see cref="ThemeConverter"/> gives.</summary>
internal sealed class ListenersConverter : JsonConverter<ListenerKind>
{
    public override ListenerKind Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType != JsonTokenType.String ? throw new JsonException()
        : reader.GetString() switch
        {
            "group" => ListenerKind.Group,
            "channels" => ListenerKind.Channels,
            _ => throw new JsonException(),
        };

    public override void Write(Utf8JsonWriter writer, ListenerKind value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value == ListenerKind.Group ? "group" : "channels");
}

/// <summary>An event's name or sound file with something in it: a blank one would show or play nothing.</summary>
internal sealed class NotBlankConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String && reader.GetString() is { } words && !string.IsNullOrWhiteSpace(words)
            ? words
            : throw new JsonException();

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) => writer.WriteStringValue(value);
}
