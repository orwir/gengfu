# Vendored code

Third-party code Gengfu builds from source. Each folder keeps its upstream license; Gengfu's own
MIT license does not apply here. Why it is vendored: [ADR-0002](../docs/adr/0002-tslib-vendored.md).

| Folder | What | Upstream | License |
| --- | --- | --- | --- |
| `TSLib/` | TeamSpeak 3/5 client library (voice, whisper, crypto) | [Splamy/TS3AudioBot](https://github.com/Splamy/TS3AudioBot) `develop` @ `142e4e2f` (2026-08-24) | OSL-3.0 (`TSLib/LICENSE`) |
| `TSLib/Declarations/` | TeamSpeak protocol declarations TSLib generates code from | [ReSpeak/tsdeclarations](https://github.com/ReSpeak/tsdeclarations) @ `53a1580` | MIT or Apache-2.0 |
| `TSLibAutogen/` | Source generator for `TSLib` | Splamy/TS3AudioBot, same commit | OSL-3.0 (`TSLibAutogen/LICENSE`) |
| `opus/win-x64/libopus.dll` | Native Opus encoder, loaded by TSLib | Splamy/TS3AudioBot `TS3AudioBot/lib/x64/` | BSD-3-Clause (`opus/LICENSE`) |

## Local patches

Changes against upstream, each marked `gengfu patch` in the source. Re-apply them when re-syncing.

| File | Change | Why |
| --- | --- | --- |
| `TSLib/Full/TsFullClient.cs` | `SendAudioGroupWhisper` sets the `Newprotocol` packet flag | Without it the server drops server-group whispers. Lost on `develop` in commit `2126993e` (2021); `master` still has it. |
| `TSLib/Full/TsFullClient.cs` | `ChangeState` reads the status before setting it to disconnected | Read after, it is never "connecting", so `Connect()` never returns when an attempt fails: a wrong password, a timeout, a request for a stronger identity. |
| `TSLib/Full/PacketHandler.cs` | `PacketTimeout` is 10 s, not 30 s | Our server drops a client silent for about 20 s; at 30 s, TSLib shows the dead connection as connected for half a minute more while every whisper is lost. |

## Re-syncing

1. Copy `TSLib/` and `TSLibAutogen/` from the new upstream commit, without `bin/` and `obj/`.
2. Re-apply the patches above, then update the commit hashes in this file.
3. Run `scripts/check.sh`.
