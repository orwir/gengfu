# Vendored code

Third-party code Gengfu builds from source. Each folder keeps its upstream license; Gengfu's own
MIT license does not apply here. Why it is vendored: [ADR-0002](../docs/adr/0002-tslib-vendored.md).

| Folder | What | Upstream | License |
| --- | --- | --- | --- |
| `TSLib/` | TeamSpeak 3/5 client library (voice, whisper, crypto) | [Splamy/TS3AudioBot](https://github.com/Splamy/TS3AudioBot) `develop` @ `142e4e2f` (2026-08-24) | OSL-3.0 (`TSLib/LICENSE`) |
| `TSLib/Declarations/` | TeamSpeak protocol declarations TSLib generates code from | [ReSpeak/tsdeclarations](https://github.com/ReSpeak/tsdeclarations) @ `53a1580` | MIT or Apache-2.0 |
| `TSLibAutogen/` | Source generator for `TSLib` | Splamy/TS3AudioBot, same commit | OSL-3.0 (`TSLibAutogen/LICENSE`) |

TSLib's own Opus classes (`TSLib/Audio/Opus/`) load a native libopus, which Gengfu doesn't ship: it
encodes its calls itself ([ADR-0005](../docs/adr/0005-call-audio-without-bindings-or-native-code.md)).

## Local patches

Changes against upstream, each marked `gengfu patch` in the source. Re-apply them when re-syncing.

| File | Change | Why |
| --- | --- | --- |
| `TSLib/Full/TsFullClient.cs` | `SendAudioGroupWhisper` sets the `Newprotocol` packet flag | Without it the server drops server-group whispers. Lost on `develop` in commit `2126993e` (2021); `master` still has it. |
| `TSLib/Full/TsFullClient.cs` | `ChangeState` reads the status before setting it to disconnected | Read after, it is never "connecting", so `Connect()` never returns when an attempt fails: a wrong password, a timeout, a request for a stronger identity. |
| `TSLib/Full/PacketHandler.cs` | `PacketTimeout` is 10 s, not 30 s | Our server drops a client silent for about 20 s; at 30 s, TSLib shows the dead connection as connected for half a minute more while every whisper is lost. |
| `TSLib/MessageProcessor.cs` | An error without a return code is handed on as a `CommandError[]` | As `[errorStatus]` it is an `INotification[]`, which the generated dispatch can't cast, so the error is lost: a server's refusal of a connection, such as a wrong password, or a whisper that reaches nobody. Broken on `develop` in commit `e82b3864` (2026). |

## Re-syncing

1. Copy `TSLib/` and `TSLibAutogen/` from the new upstream commit, without `bin/` and `obj/`.
2. Re-apply the patches above, then update the commit hashes in this file.
3. Match the `ignore` list in [`.github/dependabot.yml`](../.github/dependabot.yml) to the packages
   these projects reference, so Dependabot leaves them to the next re-sync.
4. Run `scripts/check.sh`.
5. Connect to a TS3 and a TS6 server, and check that the Connected state names the server
   (`ConnectionState.ServerUid`). It comes from the key in the server's `initivexpand2`, which the
   tests' fake server never sends; without it, listeners chosen on one server stay on the next.
