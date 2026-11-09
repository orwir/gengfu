# 0007. One place names Gengfu's folder, and a copy on another folder stays apart

## Status

Accepted — 2026-11-09. Narrows [ADR-0006](0006-self-contained-installer-and-updates.md)'s one Gengfu
per Windows session to one per settings folder.

## Context

The developer's PC is also a player's: Gengfu's settings, the bot's identity and the install sit
next to the code.

- `App.xaml` opened `MainWindow` whenever a dispatcher ran the App, and `MainWindow` made its
  `Session` on `%AppData%\Gengfu`. On 2026-11-09 a test fixture started about 50 real Gengfus on the
  maintainer's settings this way, and each connected their bot to their TeamSpeak server. A harness
  wrote a test value into their settings file the same day.
- An installed Gengfu read `%AppData%\Gengfu` with no way to point it elsewhere, so a check of the
  install ran on the user's settings.
- Velopack's Setup registers Gengfu for the Windows user, its Installed apps entry and Start menu
  shortcut, whatever folder it installs to.
- The maintainer's rule: no test, harness or check ever reaches the user's settings, secrets or
  install, by construction rather than by care.

## Decision

- `Program.Main` alone names Gengfu's folder: `%AppData%\Gengfu`, or the folder in the
  `GENGFU_HOME` environment variable. It builds the `Session` there and opens `MainWindow` with it;
  the App opens no window by itself, and `Session` has no constructor that picks a folder.
- One Gengfu runs per folder. The user's own folder keeps the lock every version holds; another
  folder gets a lock of its own, and the first Gengfu on a folder marks its window, so a second
  start brings forward only its own folder's Gengfu.
- A Gengfu on any folder but the user's own never looks for, downloads or applies updates, so it
  can't close the user's Gengfu or change the install.
- Tests give a `Session` a temp folder, and a run of Gengfu itself sets `GENGFU_HOME`.
- The smoke install refuses to run where Gengfu is installed for the Windows user. It starts the
  installed Gengfu on a folder of its own and checks the settings land there. On CI, where nobody's
  settings live, it then starts Gengfu once more on its usual folder, updates included.

## Consequences

- A test or check can't open Gengfu on the user's settings: nothing it can start names that folder.
- The smoke install can't run on a developer's PC that has Gengfu installed; CI and clean test PCs
  run it.
- Only CI's second start runs the installed update wiring before a release is published: a copy on
  a folder of its own skips it.
- An older Gengfu's window carries no mark, so a second start of a newer version on the user's
  folder doesn't raise it. Only a development build running beside an older install meets this;
  an installed Gengfu runs one version at a time.
- `GENGFU_HOME` compares folders by spelling: a short name, junction or substituted drive for
  `%AppData%\Gengfu` counts as another folder.
