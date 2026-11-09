# Releasing Gengfu

How a release is built, published, checked and rolled back, for whoever cuts one. The checklist is
[CONTRIBUTING.md § Releasing](../../CONTRIBUTING.md#releasing); this guide is the machinery behind
it. Why it works this way: [ADR-0006](../adr/0006-self-contained-installer-and-updates.md).

## What a release is

A merge to `master` runs [release.yml](../../.github/workflows/release.yml), which publishes the
version in `Directory.Build.props` as a GitHub Release tagged `v<version>`, its notes the version's
CHANGELOG section. Its files:

- `Gengfu-win-Setup.exe`: what players download and run.
- `Gengfu-<version>-full.nupkg`: the whole app, for new installs and for updates from far behind.
- `Gengfu-<version>-delta.nupkg`: only what changed since the previous release; the first release
  has none.
- `releases.win.json`, `RELEASES`: the feed installed copies read.

## Versions

- A version ending in `-dev` never releases: the job fails at once. So does a version already
  released.
- A version with a suffix, such as `0.1.0-rc.1`, is a test build and publishes as a pre-release.
  Only installed test builds take it; an installed test build moves on to the release when it
  comes. Its notes are the CHANGELOG's `[Unreleased]` section unless it has a section of its own.
- `scripts/release-notes.sh <version>` prints a version's notes, to check them before the merge.

## The release job

1. The CI gate, [ci.yml](../../.github/workflows/ci.yml), as for any change.
2. The version and its notes; either missing stops the job.
3. A self-contained win-x64 build. It takes .NET's license and notices from the runtime pack into
   its `dotnet` folder, beside Gengfu's [THIRD-PARTY-NOTICES.txt](../../THIRD-PARTY-NOTICES.txt).
4. `vpk download github` fetches the last release, for `vpk pack` to make the delta from. A test
   build counts earlier test builds; a release counts only releases.
5. `vpk pack` builds Setup and the packages. Nothing is signed.
6. [smoke-install.ps1](../../scripts/smoke-install.ps1) installs Setup silently on the CI machine
   and starts Gengfu twice, each time waiting for its window, closing it and checking where its
   settings landed: first on a settings folder of its own, then as a player's copy starts, on its
   usual folder with its updates. A test PC gets only the first start, and the script refuses to
   run where Gengfu is installed, as on a developer's PC
   ([ADR-0007](../adr/0007-gengfus-folder-and-isolated-copies.md)).
7. `vpk upload github` creates the release and its tag on the merge commit, and publishes it.

## Velopack

The release job installs vpk at the version of the Velopack package in `Gengfu.App.csproj`, since a
build packed by another version may not update. Bumping the package, Dependabot's way or by hand,
moves both.

## Rolling back

An installed Gengfu never moves to a lower version, so a bad release is undone by a better one:

1. Mark it a pre-release: `gh release edit v<X.Y.Z> --prerelease`. Installed releases that haven't
   taken it skip it from then on, and the Releases page offers the last good Setup again.
2. Revert the bad change as a hotfix ([CONTRIBUTING.md § Hotfixes](../../CONTRIBUTING.md#hotfixes)),
   so the next patch version holds only the revert. Every installed copy, the bad one included,
   updates to it.

## .NET patches

Gengfu carries its own .NET, which Windows Update never patches. The build takes the newest runtime
its SDK knows, so a patch release after a monthly .NET update brings it to players, as about
2.5 MB each. Cut one when the update fixes something Gengfu uses: networking, WPF, the runtime.

## Signing

v0.1 releases are unsigned, so Windows warns about an unknown publisher, and Smart App Control
blocks Gengfu. When signing is added, record here where the certificate lives and how it rotates.

## Checking a release

- The release lists the files above, and its notes are the version's.
- Install it as a player would: download Setup and run it on a Windows 11 PC without a developer's
  tools (Windows Sandbox will do), or let an installed Gengfu update itself. Settings › General
  then shows the new version.
- When an install or an update goes wrong, Velopack's log is `%LocalAppData%\velopack\velopack_Gengfu.log`.
