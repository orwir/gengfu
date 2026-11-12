# Gengfu

English · [Русский](README.ru.md)

Jungle calls for Where Winds Meet guild wars, on TeamSpeak. Gengfu joins your server as a bot and
whispers "jungle in thirty seconds" to every squad before each spawn, on a timer you match to the
in-game one.

https://github.com/user-attachments/assets/21de36a6-5a8a-4312-8fa8-5f33b1258dcc

- **Reads like the game's timer.** It counts 35:00 → 00:00, so checking it takes one glance; fix
  any drift with the arrows, or type what the game shows.
- **Every squad hears the call.** Gengfu whispers to a server group, say everyone in GvG, or to
  chosen channels, on TeamSpeak 3 and 6.
- **Your calls, your language.** The jungle comes set up; add the boss or any other spawn in
  Settings. Gengfu comes in English and Russian, and calls speak in a Windows voice or play a
  sound file of your own.
- **Keeps itself current.** New versions download in the background and apply the next time
  Gengfu starts, never mid-match.

## Install

Gengfu needs Windows 11, 64-bit, and a TeamSpeak server that lets it join and whisper.

1. Download [Gengfu-win-Setup.exe](https://github.com/orwir/gengfu/releases/latest/download/Gengfu-win-Setup.exe).
   If your browser warns that the file isn't commonly downloaded, keep it.
2. Run it. Gengfu isn't signed yet, so Windows may say **Windows protected your PC**: choose
   **More info**, then **Run anyway**. Setup asks nothing and opens Gengfu. Where Smart App
   Control is on, Windows blocks Gengfu until Gengfu is signed.
3. Open Settings with the gear at the bottom right. Enter your server's address, and its password
   if it has one, then press Enter. Pick who hears the calls and press **Test call**: your squad
   hears "Jungle in thirty seconds". If nobody does, Settings says why that may be.

Strict servers, more events, matching the timer mid-match and the rest are in the
[player guide](docs/guide.md).

---

Contributors start at [CONTRIBUTING.md](CONTRIBUTING.md). Gengfu is [MIT-licensed](LICENSE);
vendored code in [lib/](lib/README.md) keeps its own licenses, and the TeamSpeak library TSLib is
OSL-3.0. A fan-made tool, not affiliated with the makers of Where Winds Meet or of TeamSpeak.
