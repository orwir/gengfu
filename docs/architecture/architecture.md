# Architecture

How Gengfu is put together: projects, data flow, the clock model, threading and configuration. Why
it is built this way lives in the [decision records](../README.md#decision-records). Parts marked
*planned* are designed but not built yet.

## Projects

| Project | Role | References |
| --- | --- | --- |
| `src/Gengfu.Core` | Pure logic: match clock, call schedule, sync rules, settings model; `AppVersion` | — |
| `src/Gengfu.TeamSpeak` | TeamSpeak transport: address lookup, connection and reconnection, the server's groups and channels, whisper targets, voice frames | Core, `lib/TSLib` |
| `src/Gengfu.App` | WPF shell on the Fluent theme, strings in English and Russian: the main window and Settings, wiring the clock, the calls and TeamSpeak together; call audio; hotkeys and updates (*planned*) | Core, TeamSpeak, Concentus |
| `lib/TSLib` | Vendored TeamSpeak client library, patched — [ADR-0002](../adr/0002-tslib-vendored.md) | — |
| `tests/Gengfu.Core.Tests` | Tests mirroring `Gengfu.Core` | Core |
| `tests/Gengfu.TeamSpeak.Tests` | Tests mirroring `Gengfu.TeamSpeak`, against local DNS, TSDNS and UDP sockets | TeamSpeak |
| `tests/Gengfu.App.Tests` | Tests mirroring `Gengfu.App`, against this machine's own voices and media stack | App |

`Gengfu.Core` never references WPF, Windows-only APIs or TSLib, so a second transport (Discord in
v0.2) plugs in without touching it.

## Data flow

1. The clock reads the game time from a monotonic time source.
2. The call schedule marks a call due once the clock reaches its game time (spawn plus lead time)
   while the spawn is still ahead; the App asks it for due calls on every tick.
3. The App renders the call when Gengfu starts and whenever its sound changes, and keeps it: text
   through a Windows voice or an audio file, to 48 kHz stereo, then to 20 ms Opus frames
   ([Call audio](#call-audio)). Each call asks for it again, and gets the kept one unless a voice
   was added to Windows meanwhile.
4. TeamSpeak sends the frames in real time as a whisper to the chosen listeners — a server group
   or chosen channels — then an empty frame to end the talk. A call that can't go out, with Gengfu
   off the server or nobody chosen, is marked not sent at once, so the list never shows it calling.

## The App

- `Session` is one running Gengfu: the settings, the clock and its schedule, the call's sound, and
  the TeamSpeak connection. `Caller` makes its calls and test calls, and `StatusLine` puts the
  connection into the status line's words.
- `MainWindow` draws its own title bar: Settings' way back sits in it, and maximize is left out.
  Its surface is flat, with no Mica backdrop: under a Mica frame Windows draws its own caption
  buttons too. Windows 11 rounds the corners on request.
- `MainView` and `SettingsView` read their strings when they are made, so a new language makes
  them again; Settings keeps its place and focus.

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
window, since a late call still helps. After the spawn it never fires — the mob is already farmed
— and shows as not sent, as does a call that couldn't go out. A correction back above its game
time re-arms it; a changed lead moves every call but never makes one twice.

## Call audio

A call plays text in a Windows voice, an audio file, or the gong: a small gong struck twice, made in
code. It is never silent: text that can't be spoken and a file that can't be read play the gong,
and the clip says why, for Settings to show.

- **Voice.** The chosen voice while it is installed, else the first in the app's language with
  Windows' default first, else the gong. Any voice may be chosen whatever the app's language; the
  text is the user's to match. The voice list is read afresh each time, so a voice added in
  Windows counts without a restart. Windows adds a language's voices from that language's options
  under Language & region; Speech › Add voices can fail silently.
- **Shape.** Silence is trimmed; a call stops after 10 s of sound, counted from where its sound
  starts, and fades out, so a wrong file can't hold the whisper; its loudest 400 ms sit at
  −16 LUFS with peaks 1 dB under full scale; 100 ms of silence lead it, so its start is never
  clipped.
- **Encoding.** 20 ms Opus frames at 48 kbps, in managed code. A rendered call is kept until its
  text, its voice or its file changes.
- **Windows parts.** Speech uses Windows.Media.SpeechSynthesis (Windows 10 and later) and is
  resampled by Gengfu; files use Media Foundation, which Windows N editions lack until the Media
  Feature Pack is added. Both are called directly, with nothing extra shipped
  ([ADR-0005](../adr/0005-call-audio-without-bindings-or-native-code.md)).

## Threading

The window drives the session from a 50 ms dispatcher timer on the UI thread: it makes the calls
due, then redraws the view on show; the digits change only on whole game seconds. It reads the
connection's state on each tick rather than handling its events. Calls render on the thread pool,
one at a time, so the window never waits on Windows' speech or a file; so does the voice list. TSLib runs the connection on its own scheduler thread: the connection's
events fire there, and its data book is read only there. Voice frames go out from the voice
sender's own thread, each up to 60 ms before it plays, so Windows' coarse sleep still keeps
listeners fed; calls play one at a time. The UI thread never blocks on the network.

## Configuration

Settings are held in memory, in `AppSettings` (Core); the server password stays beside them, out
of it. Each applies as soon as it changes:

| Setting | Default |
| --- | --- |
| Server address, bot nickname, server password | none, "Gengfu", none |
| Who hears the calls | a server group, none chosen; or chosen channels |
| Call ahead | 30 s; 15, 20, 30, 45 or 60 |
| Text | the default for the language and the lead, such as "Jungle in thirty seconds"; it follows them until edited |
| Voice | the first voice in the app's language, Windows' default first; any voice or the gong may be chosen |
| Sound file | none: the text is spoken |
| Language | the same as Windows: Russian on a Russian Windows, else English |

*Planned:* one human-readable settings file under `%AppData%\Gengfu`, whose events part doubles as
a shareable preset. The server password and the bot identity stay out of that file. The format and
the secret storage are decided in the settings issue.

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
