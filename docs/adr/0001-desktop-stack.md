# 0001. Desktop stack: C# / .NET 10 + WPF

## Status

Accepted — 2026-11-07

## Context

Gengfu is a Windows app that runs next to the game on a player's PC. It must speak TeamSpeak's
voice protocol, synthesize speech, take global hotkeys and stay light. Development is AI-assisted,
so the stack is chosen for fit, not familiarity.

The alternatives each failed a must-have: TypeScript + Electron has only a port of the TeamSpeak
library and Chromium is heavy next to a game; Python + Qt has no TeamSpeak voice library; Rust's
libraries were unverified; Go has neither a TeamSpeak library nor Discord's voice encryption.

## Decision

C# on .NET 10 (LTS until November 2028), with WPF and its built-in Fluent theme as the component
vocabulary. Speech uses Windows' own voices through `Windows.Media.SpeechSynthesis`.

## Consequences

- TSLib, the reference TeamSpeak client library, is native C# ([ADR-0002](0002-tslib-vendored.md)).
- Speech, audio decoding and hotkeys come with Windows and .NET; no extra runtime ships.
- The UI is Windows-only; `Gengfu.Core` stays portable for a headless host later.
- WPF's Fluent theme API is still marked experimental (`WPF0001`); we accept that warning.
- Discord (v0.2) has a .NET option with its voice encryption: DisCatSharp.Voice 10.7 or later.
