# 0005. Call audio without Windows bindings or native code

## Status

Accepted — 2026-11-08

## Context

A call is text spoken by a Windows voice, or an audio file, encoded as Opus for TeamSpeak. It has to
play on a guildmate's PC, not only on the one it was built on.

- C# reaches Windows' modern APIs through a projection of the whole Windows API: 24 MB in the app
  folder, which .NET can't trim from a WPF app, for the two classes Gengfu needs.
- Windows' older speech API reaches the voices Windows' speech settings install only through an
  undocumented registry location.
- The vendored `libopus.dll` needs the Visual C++ runtime, which a clean Windows lacks; without it,
  no call can be encoded.

## Decision

- Speech goes through Windows.Media.SpeechSynthesis and audio files through Media Foundation's
  Source Reader, both called through their documented interfaces' method tables: no projection.
- Gengfu resamples speech itself, so voice calls don't need Media Foundation, which Windows N
  editions and Windows Server lack.
- Calls are encoded by [Concentus](https://github.com/lostromb/concentus), a C# port of Opus under
  its BSD-3-Clause license; `lib/opus` is removed.
- A call that can't be spoken or read plays a gong made in code, and says why.

## Consequences

- Nothing ships for speech, files or encoding beyond one 0.4 MB managed DLL, and players install
  nothing.
- The interop is about 500 lines of function-pointer calls. A wrong slot fails at once, and the
  tests run every installed voice and decode real files wherever Windows has them; where it
  doesn't, as on CI's Windows Server, they check the fallback instead.
- TSLib's own Opus classes stay unused: they would need a native libopus.
- Release builds must carry Concentus's license notice.
- Supersedes [ADR-0002](0002-tslib-vendored.md)'s use of TS3AudioBot's libopus build.
