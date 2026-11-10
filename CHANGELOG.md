# Changelog

User-visible changes per release, grouped Added / Changed / Fixed / Removed. One claim per entry;
style rules: [project_kickstart.md § CHANGELOG style](docs/guides/project_kickstart.md).

## [Unreleased]

### Added

- Settings › Events adds, changes and removes the events whose spawns get calls, mid-match too (#38)
- Spawn times can be typed in whole minutes, such as 25, and in any order (#38)
- Each event has a Test call of its own in Settings (#38)
- An event can play a sound file of its own in place of its words (#38)
- Undo puts back an event removed, or the events Back to default replaced (#38)
- With no events, or none with spawn times, the main window's calls list says so (#38)

### Changed

- Test call plays the first event's call (#38)
- The sound file is set per event in Settings › Events, no longer in Calls (#38)
- An event's sound file that isn't on this PC, or that Windows can't play, gives way to its words (#38)
- Open settings file first rewrites a settings file from before 0.3 in its new form (#38)
- Choosing another language in Settings drops a chosen voice that can't speak it, for one that can (#38)

### Fixed

- A named event without words of its own says its name and the lead, such as "Boss in thirty seconds", not the jungle's (#38)
- Calls for the same spawn time go out as one call, such as "Jungle and Boss in thirty seconds", none late (#39)

## [0.2.0] - 2026-11-10

### Added

- Gengfu reconnects when the server itself kicks it, as an idle limit does (#13)
- When a server's flood protection blocks Gengfu, it waits 5 minutes and tries again (#13)
- The status line says when Gengfu raises its identity's security level for a server (#13)
- A server that wants an identity above security level 32 leaves Gengfu off (#13)
- When a server wants more than security level 32, the status line names the level it wants (#13)
- While Gengfu waits to connect again, the status line counts down to its next attempt (#13)
- The status line says when the server reports that the last call reached nobody (#37)
- In Settings, a note under Test call says when nobody heard the last call, and why that may be (#37)
- Settings › TeamSpeak takes a privilege key from a server admin, so the bot joins the key's group (#36)

### Changed

- A full server, or one that refuses for another reason, is tried again every 30 s (#13)

### Fixed

- Gengfu's retries no longer trip a server's flood protection (#13)
- A call the server says reached nobody shows as Not sent, not as called (#37)
- A wrong server password shows as such, not as "no answer" retried every 5 s (#35)
- A ban from the server shows as such, not as "no answer" retried every 5 s (#35)
- A full server shows as full, not as "no answer" (#35)
- Any other refusal from the server shows the server's own words, not "no answer" (#35)
- Gengfu raises its identity's security level when a server asks for more, and joins (#35)
- Icons and status dots beside small text line up with its capital letters (#28)
- The update notice's "i" sits by its first line rather than between its two lines (#28)
- In the settings file, `listeners` other than exactly "group" or "channels" shows as an error, not read as either (#32)
- A setting written twice in the settings file shows as an error, not taken from its later copy (#32)

## [0.1.0] - 2026-11-09

### Added

- Start sets the match clock to 35:00 and counts it down like the in-game timer (#3)
- Stop ends the match and resets the clock to 35:00 (#3)
- Arrows on the minutes and seconds nudge the clock by a minute or a second (#3)
- Click the minutes or seconds and type what the game shows to match the clock (#3)
- Typed seconds land on the nearest minute, so 58 typed at 22:02 gives 21:58 (#3)
- Typed seconds count from the first keystroke, or from Enter after a pause for typing ahead (#3)
- Settings connects Gengfu to a TeamSpeak server with its address, password and a nickname (#6)
- A server address works as it does in the TeamSpeak client, TSDNS names included (#4)
- Gengfu reconnects by itself when the server drops it (#4)
- Calls are whispered to a chosen server group or to chosen channels (#6)
- A line at the top of the main window says whether Gengfu is connected and who hears the calls (#6)
- The main window lists the match's calls (#6)
- The calls list shows the time left until the next call (#6)
- Under the clock, a hint asks you to match the in-game timer (#26)
- A call nobody heard shows as "Not sent" in the list (#6)
- Calls can go out 15, 20, 30, 45 or 60 seconds before each spawn (#6)
- A call speaks your text in any Windows voice, plays a sound file, or strikes the gong (#6)
- A call that can't be spoken or played strikes the gong instead (#5)
- Settings says how to add a Windows voice when none speaks the app's language (#6)
- A call stops after 10 seconds of sound, fading out (#5)
- Every call plays at the same loudness, whatever its voice or file (#5)
- Test call whispers the call once, from the main window or from Settings (#6)
- The language can be set to English or Russian, or left to follow Windows (#6)
- The theme can be set to light or dark, or left to follow Windows (#23)
- Settings are saved as you change them and come back after a restart (#7)
- Gengfu connects at start when a server is saved (#7)
- The server password and the bot's identity are kept encrypted, apart from the settings file (#7)
- Settings › General opens the settings file for editing by hand (#7)
- An edit saved in the settings file applies at once (#7)
- Settings says on which line a hand-edited settings file has an error (#7)
- Gengfu never writes over a settings or secrets file it can't read (#7)
- Events in the settings file add calls for other spawns, each with its own name and words (#7)
- The settings file's events can be shared as a preset (#7)
- The calls list scrolls and keeps the next call in view when the calls don't fit (#7)
- Gengfu installs from one setup file, for your Windows user, with no admin rights (#8)
- New versions download in the background and apply the next time Gengfu starts (#8)
- Once the match clock is stopped, the main window says an update is ready and offers Restart (#8)
- Settings › General says whether Gengfu is up to date, or checking or downloading a new version (#8)
- Gengfu has its own icon on the taskbar, in Start and on the desktop (#8)
- Starting Gengfu while it runs brings its window to the front (#8)
