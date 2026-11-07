# 0002. TeamSpeak through vendored, patched TSLib

## Status

Accepted — 2026-11-07

## Context

Gengfu needs a full TeamSpeak voice client for TS3 and TS6 servers, with whisper. TSLib, from
Splamy/TS3AudioBot, is the mature C# implementation, and ts6-crier's TypeScript port of it works on
TS6. Its .NET 10 code exists only on the unreleased `develop` branch, which is not published as a
package.

On `develop`, server-group whisper is broken: commit `2126993e` (2021) dropped the `Newprotocol`
packet flag, so servers discard those whispers. `master` still sets it.

The Phase 3 spike connected to our TS6 server. Normal voice, channel whisper and — with the flag
restored — server-group whisper were all heard.

## Decision

Vendor TSLib and its source generator from `develop` @ `142e4e2f` into `lib/`, together with the
protocol declarations they generate code from, keeping upstream licenses and notices. Carry our
changes as listed patches in [lib/README.md](../../lib/README.md), marked `gengfu patch` in the
source. Use TS3AudioBot's Windows build of libopus.

## Consequences

- Builds from source with no extra package feed, and CI compiles TSLib on every push.
- Re-syncing upstream is manual: copy, re-apply the patch list, run the gate.
- Offering the flag fix upstream is postponed until v0.1 ships.
- `lib/` stays under its own licenses ([ADR-0004](0004-licensing.md)).
