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
- **Your language.** The app and its calls come in English and Russian.
- **Keeps itself current.** New versions install in the background and apply on the next start.

## Install

Not released yet. Releases will appear on the [Releases](https://github.com/orwir/gengfu/releases)
page. Gengfu needs Windows 10 or 11 and a TeamSpeak server it may join — no admin rights, but the
server must allow it to whisper.

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
