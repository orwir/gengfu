# Architecture

How Gengfu is put together: projects, data flow, the clock model, threading and configuration. Why
it is built this way lives in the [decision records](../README.md#decision-records). Parts marked
*planned* are designed but not built yet.

## Projects

| Project | Role | References |
| --- | --- | --- |
| `src/Gengfu.Core` | Pure logic: match clock, call schedule, sync rules, settings model and file; `AppVersion` | — |
| `src/Gengfu.TeamSpeak` | TeamSpeak transport: address lookup, connection and reconnection, the server's groups and channels, whisper targets, voice frames | Core, `lib/TSLib` |
| `src/Gengfu.App` | WPF shell on the Fluent theme, strings in English and Russian: the main window and Settings, wiring the clock, the calls and TeamSpeak together; call audio; updates; the log; hotkeys (*planned*). Builds `Gengfu.exe` | Core, TeamSpeak, Concentus, Velopack |
| `lib/TSLib` | Vendored TeamSpeak client library, patched — [ADR-0002](../adr/0002-tslib-vendored.md) | — |
| `tests/Gengfu.Core.Tests` | Tests mirroring `Gengfu.Core` | Core |
| `tests/Gengfu.TeamSpeak.Tests` | Tests mirroring `Gengfu.TeamSpeak`, against local DNS, TSDNS and UDP sockets | TeamSpeak |
| `tests/Gengfu.App.Tests` | Tests mirroring `Gengfu.App`, against this machine's own voices and media stack | App |

`Gengfu.Core` never references WPF, Windows-only APIs or TSLib, so it is tested without the UI or a
server, and another transport would plug in without touching it.

## Data flow

1. The clock reads the game time from a monotonic time source.
2. The call schedule makes a call for every spawn of every event, and marks it due once the clock
   reaches its game time (spawn plus lead time) while the spawn is still ahead; the App asks it
   for due calls on every tick.
3. The App renders each event's call when Gengfu starts and whenever it changes, and keeps it: the
   event's words, by default its name and the lead, in the voice Settings sets, or a sound file of
   its own. Words go through a Windows voice, or an audio file is read, to 48 kHz stereo, then to
   20 ms Opus frames ([Call audio](#call-audio)). Calls for one spawn time go out as one call,
   rendered ahead the same way, so none waits behind another: the events that say their name and
   the lead say them in one sentence, "Jungle and Boss in thirty seconds", in the place of the
   first of them, and the rest play their own words or files, all in the events' order. Each call
   asks for its sound again, and gets the kept one unless a voice was added to Windows meanwhile.
4. TeamSpeak sends the frames in real time as a whisper to the chosen listeners — a server group
   or chosen channels — then an empty frame to end the talk. A call that can't go out, with Gengfu
   off the server or nobody chosen, is marked not sent at once, so the list never shows it calling.
   One the server says reached nobody is marked not sent once it has played. Either way, so is
   every event's call that went out in it.

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
  Settings edits the events through it, mid-match too. An edit tells the schedule which event each
  new one was, so an event renamed or given other times keeps its calls, made stays made; an edit
  of the file can't, so there a call is the one of an event of the same name at the same spawn.
  Back to default puts the jungle alone back at its spawn times, keeping its words and sound file,
  so the text and sound file an old file moved into it don't count as a change. It keeps the
  events a Remove or Back to default took, for Undo, until the events change again or Settings
  closes.
  `Caller` makes its calls and test calls, and `StatusLine` puts the connection into the status
  line's words. When the server says a call reached nobody, `Session` keeps that for the status line
  and Settings until a whisper gets through, the connection is new, or other listeners are chosen.
  It holds a privilege key typed in Settings, in memory only, uses it through the connection, and
  keeps the server's answer for Settings until another key is typed or Connect is pressed: the
  connection's own reconnects leave the bot's group as it was.
- `Log` keeps what Gengfu did, for a player to attach to feedback, in plain English whatever the
  app's language: each run's start, with the version, Windows and the time zone, and its close; the
  connection's changes in the status line's words, once per reason however often it retries; each
  call and Test call, with what it played and how it went or why it didn't go out; the clock's
  Starts, Stops and corrections, which `MatchClock` raises as one event whichever control made them;
  the settings file's errors, a `secrets.bin` Gengfu can't read, and saves Windows refused; the
  server's answers to privilege keys; and what update looks found.
- `Program.Main` makes the log before the App and the `Session`, so a failure in either is in it,
  and writes to it an exception that escapes on any thread, with its stack; Gengfu then closes,
  since its state is unknown. A task that fails with nothing waiting on it goes to the log too, and
  Gengfu carries on.
  Each line is built from states, names, the server's own words and exception text, so the server
  password, the bot's identity and a privilege key never reach it; the Windows user's profile
  folder shows as `%USERPROFILE%`.
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
— and shows as not sent, as does a call that couldn't go out or that nobody heard. A correction
back above its game time re-arms it; a changed lead moves every call but never makes one twice.

## Call audio

A call plays words in a Windows voice, an audio file, or the gong: a small gong struck twice, made
in code. It is never silent: a file that can't play, missing on this PC or not something Windows
plays, gives way to its event's words, and words that can't be spoken play the gong; the clip says
why, for Settings to show. A preset made on another PC names files that aren't on this one, so its
events still say what they are.

- **Voice.** The chosen voice while it is installed, else the first in the app's language with
  Windows' default first, else the gong. Any voice may be chosen whatever the app's language; the
  text is the user's to match. The voice list is read afresh each time, so a voice added in
  Windows counts without a restart. Windows adds a language's voices from that language's options
  under Language & region; Speech › Add voices can fail silently: in testing it showed Russian at
  0 MB, and Add did nothing.
- **Shape.** Silence is trimmed; an event's sound stops after 10 s, counted from where its sound
  starts, and fades out, so a wrong file can't hold the whisper; its loudest 400 ms sit at
  −16 LUFS with peaks 1 dB under full scale; 100 ms of silence lead it, so its start is never
  clipped.
- **Encoding.** 20 ms Opus frames at 48 kbps, in managed code. A rendered call is kept until its
  text, its voice or its file changes.
- **One call of several parts.** A call for several events plays its parts in turn, each as kept,
  with 0.7 s of silence between them: with each part's lead-in, about the pause Windows' voices
  leave between two sentences. The frames are joined as encoded; every part starts and ends in
  silence, so listeners' decoders pass from one to the next without a click. Parts that sound the
  same play once: a file two events share, or the gong in place of words no voice can speak. Each
  part keeps its own 10 s, so a call of three events runs up to about 30 s.
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
data book is read only there. The log alone handles the connection's state changes, as the
connection reports them one at a time on that thread: there it reads the login and the settings,
which the UI thread replaces whole and never changes in place. The log takes lines from the UI
thread, TSLib's thread and the thread pool alike; each goes to disk under a lock before `Write`
returns, so a crash right after keeps it. Voice frames go out from the voice sender's own thread,
each up to 60 ms before it plays, so Windows' coarse sleep still keeps listeners fed; calls play
one at a time. The UI thread never blocks on the network, except as Gengfu closes, when it waits
up to 3 s for the connection to leave the server.

## Configuration

Settings live in `AppSettings` (Core) and in one file, `settings.json` in Gengfu's folder
(`%AppData%\Gengfu`, or the one `GENGFU_HOME` names; see [The App](#the-app)), that
`SettingsStore` reads and writes. It is JSON a person can edit, comments and trailing commas
allowed. Every value is checked on reading, and an unknown setting or one written twice is an
error too, so a hand edit never goes wrong unseen; the error names the line and the setting as the
file spells it. A version that renames or drops a setting therefore still reads the old name, or
every file written before it opens as an error.

Each setting applies as soon as it changes, and Gengfu writes the file whole once the changes
stop for half a second. It writes only after a change in the app, so the comments in a hand-edited
file last until then, and never over a file it couldn't read, where the edit would be lost. It
watches the file: an edit saved there applies at once, events, language and theme too, and calls
already made stay made. A file with an error changes nothing, and Settings shows where the error is
until it is fixed; started with one, Gengfu runs on its defaults. A file Windows won't let Gengfu
read or write is tried again every 2 s. A file written before 0.3 set a call's text and sound file
(`callText`, `soundFile`) once for every event;
it opens with them moved into its events, the text into the jungle's and the file into every event
without words of its own, and its next save writes them there. Open settings file writes it so
before it opens it: read again with a preset's events pasted in, the old text and file would go to
them too.

Gengfu's folder also holds the log, `gengfu.log` ([The App](#the-app)). Once the file would pass
1 MB, hundreds of matches' worth, the log starts afresh and keeps the full one as `gengfu.old.log`,
so it never takes more than 2 MB. A line Windows won't let Gengfu write is lost rather than stop a
call.

The server password and the bot's identity stay out of the file, so it can be shared; a privilege
key is kept nowhere, since a used one is worthless. `Secrets`
(App) keeps them beside it in `secrets.bin`, encrypted with Windows' data protection for this
Windows user; one another user or PC wrote reads as empty, and Gengfu makes a new identity. One
Windows won't let Gengfu read, perhaps held by another program, is never written over: the
identity in it is the bot's, so that run makes do with a new one and saves no secrets.

| Setting | Default |
| --- | --- |
| Server address, bot nickname, server password | none, "Gengfu", none |
| Who hears the calls | a server group, none chosen; or chosen channels. Cleared on connecting to another server, where the same ids name other groups or channels |
| Call ahead | 30 s; 15, 20, 30, 45 or 60 |
| Voice | the first voice in the app's language, Windows' default first; any voice or the gong may be chosen; it speaks every event's words. A language chosen in Settings drops a chosen voice that can't speak it |
| Language | the same as Windows: Russian on a Russian Windows, else English |
| Theme | the same as Windows' app mode (Personalization › Colors), following it when it switches; or light or dark. In the file, `"theme"` is `null`, `"light"` or `"dark"` |
| Events | the GvG jungle at 25:00, 20:00, 15:00, 10:00 and 05:00, set in Settings › Events or the file. Each event has its spawns, and may have a name, words and a sound file of its own; without a name it is the jungle, named in the app's language |
| An event's words | its name and the lead, such as "Boss in thirty seconds"; they follow the name, the lead and the language until edited. Empty, its calls play the gong |
| An event's sound file | none: its words are spoken. One that can't play gives way to the words |

## TeamSpeak specifics

- Gengfu resolves server addresses itself — [ADR-0003](../adr/0003-own-address-resolver.md).
- The connection keeps itself up: a failed attempt is retried after 1 and 2 s, then every 5 s,
  until the user disconnects or the server refuses for good, as the table below says. An attempt
  the server doesn't answer gives up after 5 s: on our server, an attempt made during an outage
  never succeeds once the link is back — likely because its opening packet carries the time it was
  made — so only a fresh one gets through. A server that comes back is reached within about 10 s.
- A refusal is tried again only every 30 s, because every attempt a server answers earns flood
  points for the IP. At 250, the default, the server's flood protection blocks the IP: every
  client from it, the player's own TeamSpeak too. Retries every 5 s got there within 90 s, and
  every 30 s not in 12 minutes, on either server. TS3 lifted the block within 20 s, but TS6 held it
  while attempts kept coming, and lifted it only after about 4 quiet minutes; so Gengfu waits 5
  minutes before trying again.
- While the connection waits to try again, its state says when the next attempt starts, on the
  App's clock, which the App hands it. The status line counts down to it on each tick, and says
  Gengfu is trying while the attempt runs.
- A connection silent for 10 s counts as lost: TSLib pings every second, and our server drops a
  client silent for about 20 s. TSLib's own 30 s is patched down
  ([lib/README.md](../../lib/README.md)); with it, a dead connection shows as connected for half
  a minute while every whisper is lost.
- While a whisper reaches nobody, TS3 3.13.7 and TS6 6.0 beta both send `1804 no whisper targets
  found`, about three times a second, the first a quarter of a second into the whisper; while
  anyone hears it, nothing. Nobody hears when nobody in the group or channels is online, when the
  group or channel is gone, or when every listener needs more whisper power than the bot has; the
  error doesn't say which. It names no whisper either, so the connection counts it for the one on
  air, from its first frame, and says how a whisper went once it has played, waiting at least 1 s
  from that frame for the word of a far server.
- A whisper reaches a listener only when the bot's `i_client_whisper_power` is at least the
  listener's `i_client_needed_whisper_power`. Stock TS3 and TS6 set neither, so a bot in Guest
  reaches everyone. A group with enough power, given to the bot while it is connected, counts at
  once, with no reconnect.
- A privilege key gives the bot such a group with no admin online. TSLib sends an empty
  `client_default_token` when it connects, so Gengfu uses a key once connected (`privilegekeyuse`).
  The server says which group it put the bot in (`notifyservergroupclientadded`) just before it
  answers, and the connection names that group from the server's group list. The server checks
  `b_virtualserver_token_use` before it looks the key up, so a refusal for it leaves the key unspent.
  Guest has it by default; a group made from scratch doesn't, so after one key a second may fail.
- The server sends its group list a moment after the connection is up; group ids come from the
  book's keys, because TSLib leaves the group's own `Id` at 0.
- The bot's identity is created once and kept, so a server group can be assigned to it; its
  security level is raised when a server asks. The connection hands it out in the official
  client's export format, and the App keeps it with the server password.
- Raising the level costs about 2^level hashes, about 4.5 million a second on one core: level 28
  takes about a minute, 30 about 4, 32 about 17. TSLib's raise can't be cancelled, and above 160
  it never ends, so Gengfu raises to 32 at most; a server that asks for more leaves it off.

How each refusal ends, the same on TS3 3.13.7 and TS6 6.0 beta, as found against local servers:

| The server | sends | and Gengfu |
| --- | --- | --- |
| refuses the password | `1028 invalid server password` | stops |
| has banned the bot | `3329 you are banned`, with "You are banned …" | stops |
| bans or kicks it while connected, a person doing so | a leave, reason ban or kick, from that person | stops |
| kicks it itself, as an idle limit does | a leave, reason kick, from client 0, "server" | reconnects after 1 s |
| is full | `1027 server maxclient reached` | tries again every 30 s |
| refuses for any other reason | its error | tries again every 30 s, showing the server's words |
| blocks the IP for flooding | `3329 you are banned`, with "flood prevention, please try again later" | waits 5 minutes, then tries again |
| asks for a stronger identity | `519 could not validate client identity`, with the level | raises the identity and tries again at once; above 32, or at a level it already has, stops |
| has the nickname in use | nothing: it names the bot "Gengfu1" | joins |
| sets a minimum client version | nothing: TSLib announces a build from the far future | joins |

Gengfu takes `3331 flood ban` as flood protection too, though neither server sent it. TSLib keeps
only why the bot left. Who kicked it comes with the leave notice, which TSLib raises as an event
before it ends the connection, so the connection reads it there. An admin bot's kick carries the
bot's own client id, so it stops Gengfu as a person's kick does.

How a privilege key ends, the same on TS3 3.13.7 and TS6 6.0 beta:

| The server | sends | and Settings says |
| --- | --- | --- |
| takes the key | the bot's new group, then `ok` | the group the bot is in now; for a channel group's key, that the server gave the key's group |
| doesn't know it: made up, mistyped, or used already, since it deletes a used key | `3840 invalid privilege key` | it doesn't know the key |
| won't let the bot's groups use keys | `2568 insufficient client permissions`, whatever the key | it doesn't let the bot use keys |
| refuses for any other reason | its error | the server's words |
