# Architecture

How Gengfu is put together: projects, data flow, the clock model, threading and configuration. Why
it is built this way lives in the [decision records](../README.md#decision-records). Parts marked
*planned* are designed but not built yet.

## Projects

| Project | Role | References |
| --- | --- | --- |
| `src/Gengfu.Core` | Pure logic: match clock, call schedule, sync rules, settings model and file; `AppVersion` | — |
| `src/Gengfu.TeamSpeak` | TeamSpeak transport: address lookup, connection and reconnection, the server's groups and channels, whisper targets, voice frames | Core, `lib/TSLib` |
| `src/Gengfu.App` | WPF shell on the Fluent theme, strings in English and Russian: the main window and Settings, wiring the clock, the calls and TeamSpeak together; call audio; updates; hotkeys (*planned*). Builds `Gengfu.exe` | Core, TeamSpeak, Concentus, Velopack |
| `lib/TSLib` | Vendored TeamSpeak client library, patched — [ADR-0002](../adr/0002-tslib-vendored.md) | — |
| `tests/Gengfu.Core.Tests` | Tests mirroring `Gengfu.Core` | Core |
| `tests/Gengfu.TeamSpeak.Tests` | Tests mirroring `Gengfu.TeamSpeak`, against local DNS, TSDNS and UDP sockets | TeamSpeak |
| `tests/Gengfu.App.Tests` | Tests mirroring `Gengfu.App`, against this machine's own voices and media stack | App |

`Gengfu.Core` never references WPF, Windows-only APIs or TSLib, so a second transport (Discord in
v0.2) plugs in without touching it.

## Data flow

1. The clock reads the game time from a monotonic time source.
2. The call schedule makes a call for every spawn of every event, and marks it due once the clock
   reaches its game time (spawn plus lead time) while the spawn is still ahead; the App asks it
   for due calls on every tick.
3. The App renders the calls' sound when Gengfu starts and whenever it changes, and keeps it: the
   sound Settings sets, and each event's own text. Text goes through a Windows voice, or an audio
   file is read, to 48 kHz stereo, then to 20 ms Opus frames ([Call audio](#call-audio)). Each call
   asks for its sound again, and gets the kept one unless a voice was added to Windows meanwhile.
4. TeamSpeak sends the frames in real time as a whisper to the chosen listeners — a server group
   or chosen channels — then an empty frame to end the talk. A call that can't go out, with Gengfu
   off the server or nobody chosen, is marked not sent at once, so the list never shows it calling.

## The App

- `Program.Main` runs Velopack before WPF starts: when Setup or the updater starts Gengfu to finish
  their work, it does so and exits there, and an update downloaded earlier applies before the
  window opens. Only one Gengfu runs per folder, held by `SingleInstance`: Velopack's updater
  closes every running copy, so a second start on the same folder applies nothing and brings that
  folder's Gengfu forward, which marks its window for this. A Gengfu on another folder stays as it is.
- `Program.Main` is also the one place that names Gengfu's folder, through `Home`: `%AppData%\Gengfu`,
  or the folder in the `GENGFU_HOME` environment variable
  ([ADR-0007](../adr/0007-gengfus-folder-and-isolated-copies.md)). It builds the `Session` there and
  opens `MainWindow` with it; the App opens no window by itself. A Gengfu on any other folder, as
  tests and checks run it, holds a lock of its own and never updates, so it can't stand in for the
  user's Gengfu, close it or change its install.
- `Session` is one running Gengfu: the settings and their file, the clock and its schedule, the
  calls' sound, the TeamSpeak connection, which it starts when a server is saved, and its updates.
  `Caller` makes its calls and test calls, and `StatusLine` puts the connection into the status
  line's words.
- `MainWindow` draws its own title bar: Settings' way back sits in it, and maximize is left out.
  Its surface is flat, with no Mica backdrop: under a Mica frame Windows draws its own caption
  buttons too. Windows 11 rounds the corners on request.
- `MainView` and `SettingsView` read their strings and settings when they are made, so a new
  language, or an edit of the settings file, makes them again; Settings keeps its place.
- A new theme makes them again too. `AppTheme` sets the App's `ThemeMode`, which swaps the Fluent
  brushes that every colour takes by DynamicResource, and gives every control its template afresh,
  which would scroll each list back to its top. `MainWindow` sets the saved theme before its window
  is made, so the window opens in it.

## Clock model

The clock keeps one anchor: "at local instant *t* the game showed *r*", so game time now is
*r* − (now − *t*). Start, the arrows and typed values only move the anchor, and every future call
moves with it. The display rounds up: 35:00 holds for the first second and 00:00 shows only at the
end. The clock stops at 00:00 and takes corrections only while it runs; Stop returns it to 35:00.

Typed values go through `SyncInput`. Typed minutes keep the running seconds and count from the
clock's minute at the first keystroke. Typed seconds land on the nearest minute that makes a time
the game can show, and are timed from the first keystroke, or from Enter after a pause, for typing
ahead; leaving the field never counts as typing ahead. Its lag constants assume the game rounds its
timer up, as the display does.

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

## Installing and updating

Gengfu ships as a Velopack installer of a self-contained build, published to GitHub Releases by
`release.yml` ([ADR-0006](../adr/0006-self-contained-installer-and-updates.md),
[release.md](../guides/release.md)). Setup installs it in `%LocalAppData%\Gengfu`: the app in
`current`, Velopack's `Update.exe` beside it, and the last package in `packages`, which the next
update is built against. The settings stay in `%AppData%\Gengfu`, out of the install's reach.

- `Updates` looks at GitHub's releases at start and every 4 hours, and downloads a new version: a
  delta when it can, else the whole package. It never looks in a copy Setup didn't install, nor in
  one on a folder of its own.
- A test build, its version suffixed, also takes test builds; a release takes releases only.
- A downloaded version applies the next time Gengfu starts. Restart instead hands it to Velopack's
  updater, which waits for Gengfu to close normally, applies it and starts Gengfu again.
- The main view offers Restart only while the clock is stopped; Settings › General says where
  `Updates` stands. Both read it on each tick.

## Threading

The window drives the session from a 50 ms dispatcher timer on the UI thread: it makes the calls
due, then redraws the view on show; the digits change only on whole game seconds. It reads the
connection's state on each tick rather than handling its events. Calls render on the thread pool,
one at a time, so the window never waits on Windows' speech or a file; so does the voice list. A
look for updates runs there too, and the tick reads where it stands.
The settings file's watcher only marks the file touched; the tick reads it once it has been
still for 250 ms, and writes the settings once they have stood still for 500 ms.
TSLib runs the connection on its own scheduler thread: the connection's events fire there, and its
data book is read only there. Voice frames go out from the voice sender's own thread, each up to
60 ms before it plays, so Windows' coarse sleep still keeps listeners fed; calls play one at a
time. The UI thread never blocks on the network, except as Gengfu closes, when it waits up to 3 s
for the connection to leave the server.

## Configuration

Settings live in `AppSettings` (Core) and in one file, `settings.json` in Gengfu's folder
(`%AppData%\Gengfu`, or the one `GENGFU_HOME` names; see [The App](#the-app)), that
`SettingsStore` reads and writes. It is JSON a person can edit, comments and trailing commas
allowed. Every value is checked on reading, and an unknown setting is an error too, so a typo
never passes unseen; the error names the line and the setting as the file spells it. A version
that renames or drops a setting therefore still reads the old name, or every file written before
it opens as an error.

Each setting applies as soon as it changes, and Gengfu writes the file whole once the changes
stop for half a second. It writes only after a change in the app, so the comments in a hand-edited
file last until then, and never over a file it couldn't read, where the edit would be lost. It
watches the file: an edit saved there applies at once, events, language and theme too, and calls
already made stay made. A file with an error changes nothing, and Settings shows where the error is
until it is fixed.

The server password and the bot's identity stay out of the file, so it can be shared. `Secrets`
(App) keeps them beside it in `secrets.bin`, encrypted with Windows' data protection for this
Windows user; one another user or PC wrote reads as empty, and Gengfu makes a new identity. One
Windows won't let Gengfu read, perhaps held by another program, is never written over: the
identity in it is the bot's, so that run makes do with a new one and saves no secrets.

| Setting | Default |
| --- | --- |
| Server address, bot nickname, server password | none, "Gengfu", none |
| Who hears the calls | a server group, none chosen; or chosen channels. Cleared on connecting to another server, where the same ids name other groups or channels |
| Call ahead | 30 s; 15, 20, 30, 45 or 60 |
| Text | the default for the language and the lead, such as "Jungle in thirty seconds"; it follows them until edited |
| Voice | the first voice in the app's language, Windows' default first; any voice or the gong may be chosen |
| Sound file | none: the text is spoken |
| Language | the same as Windows: Russian on a Russian Windows, else English |
| Theme | the same as Windows, following it when it switches; or light or dark |
| Events | the GvG jungle at 25:00, 20:00, 15:00, 10:00 and 05:00. Each event has its spawns, and may have a name and a text of its own; without them it is the jungle and says the text above. Set in the file only |

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
  client's export format, and the App keeps it with the server password.
