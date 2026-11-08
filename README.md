# Gengfu

Jungle calls for Where Winds Meet guild wars, on TeamSpeak. Gengfu joins your TeamSpeak server as a
bot and whispers "jungle in thirty seconds" to every squad before each spawn, on a clock you match
to the in-game timer. This page is for players and guild officers; contributors start at
[CONTRIBUTING.md](CONTRIBUTING.md).

> **Status:** in development — v0.1 is not released yet.

## What it does

- **A clock that reads like the game's.** It counts 35:00 → 00:00, so checking it against the
  in-game timer takes one glance.
- **Easy to match.** Start sets 35:00; fix any drift with the arrows, or click the minutes or
  seconds and type what the game shows.
- **Every squad hears the call.** Gengfu whispers to a server group (say, everyone in GvG) or to
  chosen channels, on TeamSpeak 3 and 6 servers.
- **Your language.** The app comes in English and Russian, and calls speak in a Windows voice for
  your language. If Windows has none, they strike a gong until you add one.
- **Your own calls.** Add calls for other spawns in the settings file, and share them with your
  guild as a preset.
- **Keeps itself current.** New versions download in the background and apply the next time
  Gengfu starts — never in the middle of a match.

## Install

Gengfu needs Windows 11, 64-bit, and a TeamSpeak server it may join; the server must allow it to
whisper. It needs no admin rights.

1. Download `Gengfu-win-Setup.exe` from the latest release on the
   [Releases](https://github.com/orwir/gengfu/releases) page.
2. Run it. Gengfu isn't signed yet, so Windows may say **Windows protected your PC**: choose
   **More info**, then **Run anyway**. Your browser may also warn that the file isn't commonly
   downloaded; keep it.
3. Setup installs Gengfu for your Windows user, adds it to Start and the desktop, and opens it.

Where Smart App Control is on (Windows Security › App & browser control), Windows blocks
unsigned apps with no way around it, so it blocks Gengfu until Gengfu is signed.

**Updates** download by themselves while Gengfu runs, and apply the next time it starts. Once the
match clock is stopped, the main window says an update is ready and offers **Restart**; Settings ›
General shows the version and whether it is up to date. If Gengfu won't start after an update, run
Setup again: it repairs the install and keeps your settings.

**Uninstall** Gengfu in Windows Settings › Apps › Installed apps. Your settings stay in
`%AppData%\Gengfu`; delete that folder to remove them too.

## Set up

Open Settings with the gear at the bottom right. Every change applies at once and is saved.

- **TeamSpeak.** Enter the server address as your TeamSpeak client takes it, the server password
  if it has one, and a nickname for the bot, then press **Connect**. Gengfu stays connected by
  itself; the line at the top of the main window says how it stands.
- **Who hears the calls.** Pick a server group, and everyone in it hears the calls in whatever
  channel they are, or tick the channels that should. The lists come from the server once Gengfu
  is connected, and the server has to allow the bot to whisper. Connecting to another server
  clears the choice.
- **Calls.** *Call ahead* sets how long before each spawn a call goes out: 15 to 60 seconds, 30 by
  default. *Say* is what the call speaks, and *Voice* is any Windows voice, or the gong. You can
  use a sound file instead. **Test call** whispers the call once, now.
- **Language.** English, Russian, or the same as Windows.

## Your settings

Gengfu keeps its settings in `%AppData%\Gengfu\settings.json`, saves each change there, and
connects by itself at start when a server is saved. **Open settings file** in Settings › General
opens the file in the app Windows uses for .json files, or in Notepad. An edit you save there
applies at once. If the file has an error, Settings says on which line, and Gengfu works with the
settings it had, saving nothing, until you fix it.

The server password and the bot's identity are not in that file. They sit beside it in
`secrets.bin`, encrypted by Windows for your Windows user, so the settings file is safe to share.

### Events and presets

The file's `events` say which spawns get a call. Out of the box there is one, the GvG jungle at
25:00, 20:00, 15:00, 10:00 and 05:00. Each event lists its `spawns` as the game shows them, and may
have a `name` for the calls list and a `text`, spoken in the chosen voice in place of the call's
text from Settings. An event without a name is the jungle.

```json
"events": [
  { "name": null, "spawns": ["25:00", "20:00", "15:00", "10:00", "05:00"], "text": null },
  { "name": "Boss", "spawns": ["28:00", "18:00"], "text": "Boss in thirty seconds" }
]
```

To share a preset, send the `events` part; the file holds no password or identity anyway. Whoever
gets it pastes it over the `events` in their own file through **Open settings file**.

## The calls

Under the clock, Gengfu shows the next call and the time left until it. The list below has every
call of the match: a check once it went out, and ✕ *Not sent* when nobody heard it: Gengfu was
off the server, nobody was chosen to hear the calls, the whisper failed, or the clock was moved
past the spawn first. When there are more calls than fit, the list scrolls to keep the next one in
view.

## Match the clock

Press **Start** as the match begins: the clock starts at 35:00 and counts down with the game. When
it drifts from the game's timer, match it while the match runs:

- **Arrows** above and below the minutes and seconds add or take away a minute or a second. In a
  field, ↑ and ↓ do the same.
- **Type** what the game shows: click the minutes or seconds, or press Tab, then type and press
  Enter. Leaving the field — Tab, a click elsewhere, switching to the game — applies too; Esc
  cancels. Typed minutes keep the running seconds. Typed seconds count from your first keystroke
  and land on the nearest minute, so 58 typed while the clock shows 22:02 gives 21:58.
- **Type ahead** for the closest match: type the seconds the game will reach a few seconds from
  now, wait, and press Enter the moment it reaches them. Without a pause of more than a second
  before Enter, Gengfu counts from your first keystroke instead.

Values the game can't show, such as 75 seconds, are ignored. **Stop** ends the match.

## Build from source

Needs the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```
dotnet build Gengfu.slnx
dotnet run --project src/Gengfu.App
```

## The name

Gengfu (更夫) was the night watchman of Song-dynasty China, the era Where Winds Meet is set in. He
walked the streets striking a gong and calling out the hour, so nobody missed it.

## License

MIT — see [LICENSE](LICENSE). Vendored third-party code in [lib/](lib/README.md) keeps its own
licenses; the TeamSpeak library TSLib is OSL-3.0.

Gengfu is a fan-made tool, not affiliated with the makers of Where Winds Meet or of TeamSpeak.
