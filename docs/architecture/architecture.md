# Architecture

How Gengfu is put together: projects, data flow, the clock model, threading and configuration. Why
it is built this way lives in the [decision records](../README.md#decision-records). Parts marked
*planned* are designed but not built yet.

## Projects

| Project | Role | References |
| --- | --- | --- |
| `src/Gengfu.Core` | Pure logic: match clock, call schedule, sync rules, settings model; `AppVersion` | — |
| `src/Gengfu.TeamSpeak` | TeamSpeak transport: address lookup, connection, whisper targets, voice frames (*planned*) | Core, `lib/TSLib` |
| `src/Gengfu.App` | WPF shell: windows, input, speech, hotkeys, updates; wires Core to TeamSpeak (*planned*) | Core, TeamSpeak |
| `lib/TSLib` | Vendored TeamSpeak client library, patched — [ADR-0002](../adr/0002-tslib-vendored.md) | — |
| `tests/Gengfu.Core.Tests` | Tests mirroring `Gengfu.Core` | Core |

`Gengfu.Core` never references WPF, Windows-only APIs or TSLib, so a second transport (Discord in
v0.2) plugs in without touching it.

## Data flow (planned)

1. The clock reads the game time from a monotonic time source.
2. The call schedule marks a call due when the clock crosses its game time (spawn plus lead time).
3. The App renders each call once, before the match: text through Windows TTS, or an audio file,
   to 48 kHz stereo PCM, then to 20 ms Opus frames.
4. TeamSpeak sends the frames in real time as a whisper to the configured targets — a server group
   or chosen channels — then an empty frame to end the talk.

## Clock model

The clock keeps one anchor: "at local instant *t* the game showed *r*", so game time now is
*r* − (now − *t*). Start, the arrows and typed values only move the anchor, and every future call
moves with it. The display rounds up: 35:00 holds for the first second and 00:00 shows only at the
end. Typed seconds land on the nearest minute and are timed from the first keystroke.

## Threading (planned)

TSLib runs its connection on its own scheduler thread, and its data book is read only there. Voice
frames go out from a timed audio thread, 20 ms apart. The UI thread never blocks on the network.

## Configuration (planned)

One human-readable settings file under `%AppData%\Gengfu`; its events part doubles as a shareable
preset. The server password and the bot identity stay out of that file. The format and the secret
storage are decided in the settings issue.

## TeamSpeak specifics

- Gengfu resolves server addresses itself — [ADR-0003](../adr/0003-own-address-resolver.md).
- The server sends its group list a moment after the connection is up; group ids come from the
  book's keys, because TSLib leaves the group's own `Id` at 0.
- The bot's identity is created once and kept, so a server group can be assigned to it; its
  security level is raised when a server asks.
