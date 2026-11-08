# 0006. A self-contained installer that updates itself from GitHub Releases

## Status

Accepted — 2026-11-08

## Context

Gengfu has to run on guildmates' PCs, not only the one it is built on. A merge to master should
publish it, and an installed Gengfu should keep itself current without ever restarting mid-match.

- .NET 10 isn't part of Windows, and Windows Update only patches it once it is installed. A
  framework-dependent Gengfu on a PC without it opens a dialog that sends the player to a 60 MB
  installer needing admin rights.
- Nothing else is needed from the PC: every native file of a self-contained build, Velopack's own
  included, loads only Windows' DLLs and the C runtime that is part of Windows.
- Gengfu can't be one exe. A framework-dependent one needs .NET installed. A self-contained build is
  408 files, or one 64.5 MB exe that unpacks WPF's native libraries into a temp folder.
- Its settings live in `%AppData%`, and its secrets open only for the Windows user who saved them,
  so a portable copy brings nothing to another PC.
- .NET 10 supports Windows 10 only in its Enterprise LTSC editions.
- Code signing costs money, or an approval process for open-source projects that have released.

## Decision

- Gengfu ships as a [Velopack](https://velopack.io) installer of a self-contained win-x64 build.
  Setup installs it for the current Windows user in `%LocalAppData%\Gengfu`, with no admin rights,
  shortcuts in Start and on the desktop, and an uninstall entry. There is no portable version.
- Gengfu supports Windows 11, 64-bit. Windows 10 isn't blocked, nor tested.
- `release.yml` releases each merge to master as the version in `Directory.Build.props`. A `-dev`
  version fails the job; a version with a suffix, such as `0.1.0-rc.1`, publishes as a
  pre-release.
- An installed Gengfu looks on GitHub at start and every 4 hours, and downloads a new version in
  the background. It applies at the next start; the main window says so, with Restart, only while
  the clock is stopped. Nothing restarts on its own.
- A test build takes test builds and then the release; a release never takes a test build.
- v0.1 is unsigned.
- The Velopack library and the vpk tool are pinned together, at 1.2.161.

## Consequences

- A clean Windows 11 PC needs nothing else. Setup is 78 MB, and Gengfu takes about 220 MB
  installed: the app, plus the copy of its package Velopack keeps to build the next update from.
- Updates download only what changed: about 0.3 MB for a change to Gengfu, 2.5 MB for a .NET
  patch.
- Windows Update never patches the .NET inside Gengfu: a .NET fix that matters to it takes a patch
  release ([release.md](../guides/release.md)).
- Windows warns once that the publisher is unknown. Where Smart App Control is on, Windows blocks
  Gengfu outright, with no exception to grant; a signature is the way past it.
- In Velopack 1.2.161, a file locked for about a minute while an update applies can leave no
  installed copy ([velopack#1086](https://github.com/velopack/velopack/issues/1086)); running
  Setup again repairs it. The fix comes with Velopack 1.3.
- Velopack reads GitHub's 10 newest releases, so more than 10 test builds in a row would hide the
  latest release from installed releases.
- Each build is installed and started on the CI machine before it is published.
