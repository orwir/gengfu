# Changelog

User-visible changes per release, grouped Added / Changed / Fixed / Removed. One claim per entry;
style rules: [project_kickstart.md § CHANGELOG style](docs/guides/project_kickstart.md).

## [Unreleased]

### Added

- Start sets the match clock to 35:00 and counts it down like the in-game timer (#3)
- Stop ends the match and resets the clock to 35:00 (#3)
- Arrows on the minutes and seconds nudge the clock by a minute or a second (#3)
- Click the minutes or seconds and type what the game shows to match the clock (#3)
- Typed seconds land on the nearest minute, so 58 typed at 22:02 gives 21:58 (#3)
- Typed seconds count from the first keystroke, or from Enter after a pause for typing ahead (#3)
- Settings connects Gengfu to a TeamSpeak server with its address, password and a nickname (#6)
- Calls are whispered to a chosen server group or to chosen channels (#6)
- A line at the top of the main window says whether Gengfu is connected and who hears the calls (#6)
- The main window lists the match's calls and shows the next one with the time left (#6)
- A call nobody heard shows as "Not sent" in the list (#6)
- Calls can go out 15, 20, 30, 45 or 60 seconds before each spawn (#6)
- A call speaks your text in any Windows voice, plays a sound file, or strikes the gong (#6)
- Test call whispers the call once, from the main window or from Settings (#6)
- The language can be set to English or Russian, or left to follow Windows (#6)
