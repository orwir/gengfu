# Gengfu — player guide

How to install Gengfu, set it up and run it through a match, for whoever runs it for their guild.
The [front page](../README.md) is the short version; contributors start at
[CONTRIBUTING.md](../CONTRIBUTING.md). [По-русски](guide.ru.md).

- [Install](#install)
- [Set up](#set-up)
- [Match the timer](#match-the-timer)
- [The calls](#the-calls)
- [On a strict server](#on-a-strict-server)
- [Your settings](#your-settings)
- [The log](#the-log)
- [The name](#the-name)

## Install

Gengfu needs Windows 11, 64-bit, and a TeamSpeak server it may join; the server must allow it to
whisper. It needs no admin rights.

1. Download [Gengfu-win-Setup.exe](https://github.com/orwir/gengfu/releases/latest/download/Gengfu-win-Setup.exe),
   the latest release's Setup; every release and its notes are on the
   [Releases](https://github.com/orwir/gengfu/releases) page. Your browser may warn that the file
   isn't commonly downloaded; keep it.
2. Run it. Gengfu isn't signed yet, so Windows may say **Windows protected your PC**: choose
   **More info**, then **Run anyway**.
3. Setup installs Gengfu for your Windows user, adds it to Start and the desktop, and opens it.

Where Smart App Control is on (Windows Security › App & browser control), Windows blocks
unsigned apps with no way around it, so it blocks Gengfu until Gengfu is signed.

**Updates** download by themselves while Gengfu runs, and apply the next time it starts. While the
timer is stopped, the main window says an update is ready and offers **Restart**; Settings ›
General shows the version and whether it is up to date. Starting Gengfu while it runs only brings
its window to the front. If Gengfu won't start after an update, run Setup again: it repairs the
install and keeps your settings.

**Uninstall** Gengfu in Windows Settings › Apps › Installed apps. Your settings stay in
`%AppData%\Gengfu`; delete that folder to remove them too.

## Set up

Open Settings with the gear at the bottom right. Every change applies at once and is saved.

- **TeamSpeak.** Enter the server address as your TeamSpeak client takes it, the server password
  if it has one, and a nickname for the bot, then press **Connect** or Enter in a field. Gengfu
  stays connected by itself; the line at the top of the main window says how it stands, and while
  Gengfu waits to try again, how long until it does. A privilege key from the server's admin goes
  under **Use a privilege key** ([On a strict server](#on-a-strict-server)).
- **Who hears the calls.** Pick a server group, and everyone in it hears the calls in whatever
  channel they are, or tick the channels that should. The lists come from the server once Gengfu
  is connected, and the server has to allow the bot to whisper. Connecting to another server
  clears the choice.
- **Calls.** *Call ahead* sets how long before each spawn a call goes out: 15 to 60 seconds, 30 by
  default. *Voice* is any Windows voice, or the gong, for every event; if Windows has no voice for
  your language, calls strike the gong until you add one, and Settings says how. **Test call**,
  here or in the main window, whispers the first event's call once, now. If the server says nobody heard it,
  a note under Test call says why that may be.
- **Events.** The events whose spawns get a call: out of the box, the GvG jungle alone. Open one to
  change its name, its spawn times and what its calls say: its name and the lead by default, such
  as "Boss in thirty seconds", or words of your own, or a sound file of its own. Type spawn times
  as the game shows them or in whole minutes, such as `25:00, 15`; Enter or leaving the field
  applies them, and a time Gengfu can't read changes nothing, with a note saying which. Each event
  has its own **Test call**. **Add event** adds one, the bin beside a name removes one, and **Back
  to default** puts the jungle alone back at its five times, keeping its words or sound file;
  **Undo** puts back what they took, until you change the events again or leave Settings. Every edit
  applies at once, mid-match too, and an event you rename or give other times keeps the calls it
  already made.
- **Language.** English, Russian, or the same as Windows. The events' default words follow it, so a
  voice you chose that can't speak the new language gives way to one that can.
- **Theme.** Light, dark, or the same as Windows' app mode (Personalization › Colors), following it
  when it switches.

## Match the timer

Press **Start** as the match begins: Gengfu's timer starts at 35:00 and counts down with the
game's. When the two drift apart, match it while the match runs:

- **Arrows** above and below the minutes and seconds add or take away a minute or a second. In a
  field, ↑ and ↓ do the same.
- **Type** what the game shows: click the minutes or seconds, or press Tab, then type and press
  Enter. Leaving the field — Tab, a click elsewhere, switching to the game — applies too; Esc
  cancels. Typed minutes keep the running seconds. Typed seconds count from your first keystroke.
  Within a minute after the minutes, with nothing else changing the timer in between, they land in
  the minute you typed, or in the one below if the game has moved into it by then. Otherwise they
  land on the nearest minute, so 58 typed while the timer shows 22:02 gives 21:58.
- **Type ahead** for the closest match: type the seconds the game will reach a few seconds from
  now, wait, and press Enter the moment it reaches them. Without a pause of more than a second
  before Enter, Gengfu counts from your first keystroke instead, as it always does when you leave
  the field.

Values the game can't show, such as 75 seconds, are ignored. **Stop** ends the match.

## The calls

The calls list has every call of the match: the next one with the time left until it, a check once
a call went out, and ✕ *Not sent* when nobody heard it: Gengfu was off the server, nobody was
chosen to hear the calls, the whisper failed, the server said it reached nobody, or the timer was
moved past the spawn first. When there are more calls than fit, the list scrolls to keep the next
one in view. With no events, or none with spawn times, the list says so.

Calls for the same spawn time go out as one call, so none comes late. Events that say their name
and the lead say them together, such as "Jungle and Boss in thirty seconds"; events with words or a
sound file of their own play them in turn in that call, and a sound file they share plays once.
Their rows show the call together, and when nobody heard it, every one of them says *Not sent*.

When the server says a call or a Test call reached nobody, the line at the top of the main window
says so until a call gets through, Gengfu connects anew, or you choose other listeners. The server
can't tell why: nobody who should hear it is online, or the server doesn't let the bot whisper to
them.

Under the timer, a line asks you to match the in-game timer until the last call; while a call goes
out, it says what the squads hear instead.

## On a strict server

Gengfu joins as an ordinary TeamSpeak client, so a server that takes ordinary clients takes it. A
stricter server asks its admin for three things:

- **Whisper power.** The bot's server group needs at least the whisper power the listeners' groups
  ask for (`i_client_whisper_power` against `i_client_needed_whisper_power`). Without it the server
  tells Gengfu that nobody heard the calls, and Gengfu says so. The admin gives the bot such a group
  while it is online, or, without being online then, makes a privilege key for that group (in
  TeamSpeak, Permissions › Privilege Keys) and sends it to you. In Settings › TeamSpeak press **Use
  a privilege key**, paste the key and press **Use**: the bot joins the group at once and keeps it.
  A key works once. A later key also needs the bot's group to allow privilege keys
  (`b_virtualserver_token_use`), as Guest does.
- **No idle limit.** Between matches the bot stays silent. When the server's own idle limit
  (`i_client_max_idletime`) kicks it, Gengfu is back within a second, but a call due that moment
  is lost. An admin bot that kicks idle clients stops Gengfu, as a person's kick does. So exempt
  the bot's group from both.
- **A security level up to 32.** If the server asks for a stronger identity, Gengfu raises the
  bot's once and keeps it; the status line says so, and it can take minutes. Above 32 the wait
  would pass half an hour and double with each level, so Gengfu stays off and the line names the
  level the server asks for.

If the server's flood protection blocks your PC, your own TeamSpeak can't connect either. Gengfu
then waits 5 minutes before trying again, so the block can lift.

## Your settings

Gengfu keeps its settings in `%AppData%\Gengfu\settings.json`, saves each change there, and
connects by itself at start when a server is saved. You can also edit the file by hand: an edit
you save there applies at once. If the file has an error, Settings says on which line and offers
**Open settings file**, and Gengfu works with the settings it had, or its defaults if it started
that way, saving nothing, until you fix it.

The server password and the bot's identity are not in that file. They sit beside it in
`secrets.bin`, encrypted by Windows for your Windows user.

### Events in the file

The file's `events` are the ones Settings › Events shows. Each lists its `spawns` as the game shows
them, and may have a `name` for the calls list, a `text` its calls say in place of its name and
the lead, and a `soundFile` they play in place of the words. An event without a name is the jungle.

```json
"events": [
  { "name": null, "spawns": ["25:00", "20:00", "15:00", "10:00", "05:00"], "text": null, "soundFile": null },
  { "name": "Boss", "spawns": ["28:00", "18:00"], "text": "Boss soon", "soundFile": null }
]
```

If an event's `soundFile` isn't on this PC, its calls say its words instead, and Settings says so.

## The log

When a call goes missing, Gengfu's log says why. It is `gengfu.log` in `%AppData%\Gengfu`: paste
that folder into File Explorer's address bar to open it. The log is plain text in English. It
records when Gengfu started and closed, each change in the connection, and each call and Test call,
with what it played and whether anyone heard it, or why it didn't go out. It also records the
timer's Start, Stop and corrections, errors in the settings file, updates, and any error that
closed Gengfu.

It holds your server's address, the bot's nickname, the names of your groups, channels and events,
your Windows version, Gengfu's language and your time zone, but never the server password, the
bot's identity or a privilege key. Once it reaches 1 MB, Gengfu starts it afresh and keeps the full
one as `gengfu.old.log`.

## The name

Gengfu (更夫) was the night watchman of Song-dynasty China, the era Where Winds Meet is set in. He
walked the streets striking a gong and calling out the hour, so nobody missed it.
