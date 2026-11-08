# Architecture

How Gengfu is put together: projects, data flow, the clock model, threading and configuration. Why
it is built this way lives in the [decision records](../README.md#decision-records). Parts marked
*planned* are designed but not built yet.

## Projects

| Project | Role | References |
| --- | --- | --- |
| `src/Gengfu.Core` | Pure logic: match clock, call schedule, sync rules, settings model; `AppVersion` | — |
| `src/Gengfu.TeamSpeak` | TeamSpeak transport: address lookup, connection and reconnection, the server's groups and channels, whisper targets, voice frames | Core, `lib/TSLib` |
| `src/Gengfu.App` | WPF shell on the Fluent theme, strings in English and Russian: the main window's clock and Start/Stop; speech, hotkeys, updates and the TeamSpeak wiring (*planned*) | Core, TeamSpeak |
| `lib/TSLib` | Vendored TeamSpeak client library, patched — [ADR-0002](../adr/0002-tslib-vendored.md) | — |
| `tests/Gengfu.Core.Tests` | Tests mirroring `Gengfu.Core` | Core |
| `tests/Gengfu.TeamSpeak.Tests` | Tests mirroring `Gengfu.TeamSpeak`, against local DNS, TSDNS and UDP sockets | TeamSpeak |

`Gengfu.Core` never references WPF, Windows-only APIs or TSLib, so a second transport (Discord in
v0.2) plugs in without touching it.

## Data flow (planned)

1. The clock reads the game time from a monotonic time source.
2. The call schedule marks a call due once the clock reaches its game time (spawn plus lead time)
   while the spawn is still ahead; the App asks it for due calls on every tick.
3. The App renders each call once, before the match: text through Windows TTS, or an audio file,
   to 48 kHz stereo PCM, then to 20 ms Opus frames.
4. TeamSpeak sends the frames in real time as a whisper to the configured targets — a server group
   or chosen channels — then an empty frame to end the talk.

## Clock model

The clock keeps one anchor: "at local instant *t* the game showed *r*", so game time now is
*r* − (now − *t*). Start, the arrows and typed values only move the anchor, and every future call
moves with it. The display rounds up: 35:00 holds for the first second and 00:00 shows only at the
end. The clock stops at 00:00 and takes corrections only while it runs; Stop returns it to 35:00.

Typed values go through `SyncInput`. Typed minutes keep the running seconds. Typed seconds land on
the nearest minute that makes a time the game can show, and are timed from the first keystroke, or
from Enter after a pause, for typing ahead. Its lag constants assume the game rounds its timer up,
as the display does.

A call fires once while the clock is between its game time and its spawn, and shows as calling
for 3 s: on time as the clock counts down, or late when a correction lands the clock in that
window, since a late call still helps. After the spawn it never fires — the mob is already farmed.
A correction back above its game time re-arms it.

## Threading

The window redraws the clock on the UI thread from a 50 ms dispatcher timer; the digits change only
on whole game seconds. TSLib runs the connection on its own scheduler thread: the connection's
events fire there, and its data book is read only there. Voice frames go out from the voice
sender's own thread, each up to 60 ms before it plays, so Windows' coarse sleep still keeps
listeners fed; calls play one at a time. The UI thread never blocks on the network.

## Configuration (planned)

One human-readable settings file under `%AppData%\Gengfu`; its events part doubles as a shareable
preset. The server password and the bot identity stay out of that file. The format and the secret
storage are decided in the settings issue.

## TeamSpeak specifics

- Gengfu resolves server addresses itself — [ADR-0003](../adr/0003-own-address-resolver.md).
- The connection keeps itself up: a failed attempt is retried after 1 and 2 s, then every 5 s,
  until the user disconnects, or the server kicks, bans or refuses the password. An attempt the
  server doesn't answer gives up after 5 s: on our server, an attempt made during an outage never
  succeeds once the link is back — likely because its opening packet carries the time it was
  made — so only a fresh one gets through. A server that comes back is reached within about 10 s.
- A connection silent for 10 s counts as lost: TSLib pings every second, and our server drops a
  client silent for about 20 s. TSLib's own 30 s is patched down
  ([lib/README.md](../../lib/README.md)); with it, a dead connection shows as connected for half
  a minute while every whisper is lost.
- The server sends its group list a moment after the connection is up; group ids come from the
  book's keys, because TSLib leaves the group's own `Id` at 0.
- The bot's identity is created once and kept, so a server group can be assigned to it; its
  security level is raised when a server asks. The connection hands it out in the official
  client's export format and says when it changes; the App stores it (*planned*).
