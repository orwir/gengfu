# Docs

Where each kind of Gengfu document lives. Start with the vision for what Gengfu is, the
architecture for how it works, and the decision records for why. Installing the app is on the front
page, the root [README](../README.md), and using it is in the [player guide](guide.md), both also in
Russian; the rules for working in the repo are in [AGENTS](../AGENTS.md), and setting it up and
releasing it in [CONTRIBUTING](../CONTRIBUTING.md).

| Doc | What's in it |
| --- | --- |
| [guide.md](guide.md) · [guide.ru.md](guide.ru.md) | The player guide, in English and Russian: install, set up, matching the timer, the calls, strict servers, settings, the log |
| [product/vision.md](product/vision.md) | Problem, users, principles, v0.1 scope, critical user journeys |
| [product/idea.md](product/idea.md) | Frozen origins: the idea gauntlet that led to Gengfu |
| [architecture/architecture.md](architecture/architecture.md) | Projects, data flow, clock model, threading, configuration |
| [guides/design_language.md](guides/design_language.md) | Tokens, components, patterns; each screen's design file |
| [guides/release.md](guides/release.md) | How a release is built, published and rolled back |
| Lifecycle guides | Not copied here: read from the installed lifecycle kit |
| [reviews/](reviews/) | Review plans and records: what each review found and decided, for the next to start from |
| `adr/` | Decision records — index below |

## Decision records

| ADR | Decision |
| --- | --- |
| [ADR-0001](adr/0001-desktop-stack.md) | Desktop stack: C# / .NET 10 + WPF |
| [ADR-0002](adr/0002-tslib-vendored.md) | TeamSpeak through vendored, patched TSLib |
| [ADR-0003](adr/0003-own-address-resolver.md) | Gengfu resolves server addresses itself |
| [ADR-0004](adr/0004-licensing.md) | MIT for Gengfu; vendored code keeps its licenses |
| [ADR-0005](adr/0005-call-audio-without-bindings-or-native-code.md) | Call audio without Windows bindings or native code |
| [ADR-0006](adr/0006-self-contained-installer-and-updates.md) | A self-contained installer that updates itself from GitHub Releases; its release trigger is superseded by ADR-0008 |
| [ADR-0007](adr/0007-gengfus-folder-and-isolated-copies.md) | One place names Gengfu's folder, and a copy on another folder stays apart |
| [ADR-0008](adr/0008-releases-by-a-dispatched-workflow.md) | Releases ship from a workflow dispatched on the reviewed commit |
