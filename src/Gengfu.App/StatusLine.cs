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
    public static StatusLine Of(ConnectionState state, string? server, AppSettings settings, CultureInfo language) =>
        state.Status switch
        {
            ConnectionStatus.Connected => Listening(settings, language),
            ConnectionStatus.Connecting => Trying(state, server ?? settings.Server),
            _ => NotConnected(state.Problem, server is null && string.IsNullOrWhiteSpace(settings.Server)),
        };

    /// <summary>The line beside Connect in Settings, where the listeners are in view and the words are shorter.</summary>
    public static StatusLine InSettings(ConnectionState state, string? server, AppSettings settings) =>
        state switch
        {
            { Status: ConnectionStatus.Connected } => new(StatusTone.Success, Strings.Connected, false),
            { Status: ConnectionStatus.Connecting, Problem: ConnectionProblem.None } => new(StatusTone.Pending, Strings.Connecting, false),
            { Status: ConnectionStatus.Connecting } => Trying(state, server ?? settings.Server),
            { Problem: ConnectionProblem.WrongPassword } => new(StatusTone.Critical, Strings.WrongPassword, false),
            { Problem: ConnectionProblem.Kicked } => new(StatusTone.Critical, Strings.Kicked, false),
            { Problem: ConnectionProblem.Banned } => new(StatusTone.Critical, Strings.Banned, false),
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

    private static StatusLine Trying(ConnectionState state, string server) =>
        state.Problem switch
        {
            ConnectionProblem.None => new(StatusTone.Pending, Copy.Format(Strings.StatusConnecting, server), false),
            ConnectionProblem.AddressNotFound => Retrying(Copy.Format(Strings.StatusAddressNotFound, server)),
            ConnectionProblem.ServerShutdown => Retrying(Strings.StatusServerShutdown),
            ConnectionProblem.ServerFull => Retrying(Strings.StatusServerFull),
            ConnectionProblem.Refused when state.Detail is { } detail => Retrying(Copy.Format(Strings.StatusRefused, detail)),
            _ => Retrying(Copy.Format(Strings.StatusNoAnswer, server)),
        };

    private static StatusLine Retrying(string text) => new(StatusTone.Retrying, text, false);

    private static StatusLine NotConnected(ConnectionProblem problem, bool notSetUp) =>
        new(StatusTone.Critical, problem switch
        {
            ConnectionProblem.WrongPassword => Strings.StatusWrongPassword,
            ConnectionProblem.Kicked => Strings.StatusKicked,
            ConnectionProblem.Banned => Strings.StatusBanned,
            _ when notSetUp => Strings.StatusNotSetUp,
            _ => Strings.StatusNotConnected,
        }, true);
}
