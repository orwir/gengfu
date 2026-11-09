using System.Globalization;
using Gengfu.Core.Settings;
using Gengfu.TeamSpeak;

namespace Gengfu.App;

/// <summary>The status line's dot: filled when settled, hollow while Gengfu keeps trying by itself.</summary>
internal enum StatusTone
{
    Success,
    Caution,
    Pending,
    Retrying,
    Critical,

    /// <summary>Not connected, with nothing gone wrong: a critical dot beside plain words.</summary>
    Off,
}

/// <summary>
/// What the main window's status line says about TeamSpeak: the dot, the words, and whether a Settings
/// link follows, which it does only when the user has to act.
/// </summary>
internal sealed record StatusLine(StatusTone Tone, string Text, bool LinksToSettings)
{
    /// <param name="server">The server Gengfu is connected or connecting to; null when it has none.</param>
    /// <param name="now">The time on the clock the connection's next attempt is set on.</param>
    public static StatusLine Of(ConnectionState state, string? server, AppSettings settings, CultureInfo language, TimeSpan now) =>
        state.Status switch
        {
            ConnectionStatus.Connected => Listening(settings, language),
            ConnectionStatus.Connecting => Trying(state, server ?? settings.Server, now),
            _ => NotConnected(state, server is null && string.IsNullOrWhiteSpace(settings.Server)),
        };

    /// <summary>The line beside Connect in Settings, where the listeners are in view and the words are shorter.</summary>
    public static StatusLine InSettings(ConnectionState state, string? server, AppSettings settings, TimeSpan now) =>
        state switch
        {
            { Status: ConnectionStatus.Connected } => new(StatusTone.Success, Strings.Connected, false),
            { Status: ConnectionStatus.Connecting, Problem: ConnectionProblem.None } => new(StatusTone.Pending, Strings.Connecting, false),
            { Status: ConnectionStatus.Connecting } => Trying(state, server ?? settings.Server, now),
            { Problem: ConnectionProblem.WrongPassword } => new(StatusTone.Critical, Strings.WrongPassword, false),
            { Problem: ConnectionProblem.Kicked } => new(StatusTone.Critical, Strings.Kicked, false),
            { Problem: ConnectionProblem.Banned } => new(StatusTone.Critical, Strings.Banned, false),
            { Problem: ConnectionProblem.IdentityTooWeak, Detail: { } level } => new(StatusTone.Critical, Copy.Format(Strings.LevelTooHigh, level), false),
            _ => new(StatusTone.Off, Strings.StatusNotConnected, false),
        };

    private static StatusLine Listening(AppSettings settings, CultureInfo language)
    {
        if (settings.Listeners == ListenerKind.Group)
        {
            return settings.Group is { } group
                ? new(StatusTone.Success, Copy.Format(Strings.StatusGroup, group.Name), false)
                : NoListeners;
        }
        return settings.Channels switch
        {
            [] => NoListeners,
            [var channel] => new(StatusTone.Success, Copy.Format(Strings.StatusChannel, channel.Name), false),
            var channels => new(StatusTone.Success, Copy.Format(Strings.StatusChannels, Copy.ChannelCount(channels.Count, language)), false),
        };
    }

    private static StatusLine NoListeners => new(StatusTone.Caution, Strings.StatusNoListeners, true);

    private static StatusLine Trying(ConnectionState state, string server, TimeSpan now)
    {
        // A wait counts down to the next attempt, as the calls list counts to the next call, so a
        // long one shows Gengfu at work and when it tries.
        var again = state.RetryAt is { } at && at > now
            ? Copy.Format(Strings.TryingAgainIn, Copy.Span(at - now))
            : Strings.TryingAgainNow;
        return state.Problem switch
        {
            ConnectionProblem.None => new(StatusTone.Pending, Copy.Format(Strings.StatusConnecting, server), false),
            // A raise is Gengfu at work, not something gone wrong: it keeps the plain connecting dot.
            ConnectionProblem.IdentityTooWeak when state.Detail is { } level => new(StatusTone.Pending, Copy.Format(Strings.StatusRaisingIdentity, level), false),
            ConnectionProblem.AddressNotFound => Retrying(Copy.Format(Strings.StatusAddressNotFound, server, again)),
            ConnectionProblem.ServerShutdown => Retrying(Copy.Format(Strings.StatusServerShutdown, again)),
            ConnectionProblem.KickedByServer => Retrying(Copy.Format(Strings.StatusKickedByServer, again)),
            ConnectionProblem.ServerFull => Retrying(Copy.Format(Strings.StatusServerFull, again)),
            ConnectionProblem.FloodProtection => Retrying(Copy.Format(Strings.StatusFloodProtection, again)),
            ConnectionProblem.Refused when state.Detail is { } detail => Retrying(Copy.Format(Strings.StatusRefused, detail, again)),
            _ => Retrying(Copy.Format(Strings.StatusNoAnswer, server, again)),
        };
    }

    private static StatusLine Retrying(string text) => new(StatusTone.Retrying, text, false);

    private static StatusLine NotConnected(ConnectionState state, bool notSetUp) =>
        new(StatusTone.Critical, state.Problem switch
        {
            ConnectionProblem.WrongPassword => Strings.StatusWrongPassword,
            ConnectionProblem.Kicked => Strings.StatusKicked,
            ConnectionProblem.Banned => Strings.StatusBanned,
            ConnectionProblem.IdentityTooWeak when state.Detail is { } level => Copy.Format(Strings.StatusLevelTooHigh, level),
            _ when notSetUp => Strings.StatusNotSetUp,
            _ => Strings.StatusNotConnected,
        }, true);
}
