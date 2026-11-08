# Docs

Where each kind of Gengfu document lives. Start with the vision for what Gengfu is, the
architecture for how it works, and the decision records for why. Installing and using the app is
in the root [README](../README.md); how to work in the repo is in [CONTRIBUTING](../CONTRIBUTING.md).

| Doc | What's in it |
| --- | --- |
| [product/vision.md](product/vision.md) | Problem, users, principles, v0.1 scope, critical user journeys |
| [product/idea.md](product/idea.md) | Frozen origins: the idea gauntlet that led to Gengfu |
| [architecture/architecture.md](architecture/architecture.md) | Projects, data flow, clock model, threading, configuration |
| [guides/design_language.md](guides/design_language.md) | Tokens, components, patterns; each screen's design file |
| `guides/*.md` (other) | Process playbooks mirrored from the shared template gist |
| `adr/` | Decision records — index below |

## Decision records

| ADR | Decision |
| --- | --- |
| [ADR-0001](adr/0001-desktop-stack.md) | Desktop stack: C# / .NET 10 + WPF |
| [ADR-0002](adr/0002-tslib-vendored.md) | TeamSpeak through vendored, patched TSLib |
| [ADR-0003](adr/0003-own-address-resolver.md) | Gengfu resolves server addresses itself |
| [ADR-0004](adr/0004-licensing.md) | MIT for Gengfu; vendored code keeps its licenses |
| [ADR-0005](adr/0005-call-audio-without-bindings-or-native-code.md) | Call audio without Windows bindings or native code |
