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
