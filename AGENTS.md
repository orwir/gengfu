# Agent instructions — Gengfu

You are working in the `orwir/gengfu` repository: a C# / .NET 10 Windows desktop app (a TeamSpeak bot that whispers jungle calls for Where Winds Meet guild wars, on a clock matched to the in-game timer). Follow every rule below on every code change. When a rule conflicts with a direct user instruction in the current session, ask the user to clarify before proceeding.

These rules are the compressed operating contract. Why each exists is the lifecycle kit's [project_kickstart.md](https://github.com/orwir/lifecycle-kit/blob/main/method/project_kickstart.md); setting up and releasing is [CONTRIBUTING.md](CONTRIBUTING.md). Every link to the lifecycle kit, here or in any other file, opens its `main` on GitHub: read the file at the same path in the folder `lifecycle where` prints instead (`method/…`, `docs/guide.md`), the release this machine runs.

**Hard gates, before anything else** (everything below is detail around them):
- Never ship a release without that release's own "proceed".
- Never commit, push, merge or close work without explicit approval (*Task workflow*), except WIP commits on a feature branch.
- Never bypass a hook with `--no-verify`: fix what it caught, or, if the hook is wrong, say so and propose a fix.
- Never commit or print secrets.

---

## Commands

- Setup: `powershell -File scripts/setup.ps1`
- Gate — format, build, test, coverage: `scripts/check.sh`; given tests alone: `dotnet test Gengfu.slnx --filter "<tests>"`
- Format: `dotnet format Gengfu.slnx --exclude lib`
- Run locally: `GENGFU_HOME=<folder> dotnet run --project src/Gengfu.App`
- Lifecycle: `lifecycle doctor --json` before a task. A finding whose fix is `upgrade` runs the lifecycle kit's upgrade skill first, alone, unless a task's edits are in the tree: that task lands first. Any other `block`, or a failure of the command, goes to the user. `.lifecycle.toml` holds this repository's settings for the kit's skills and command.

These are the canonical invocations — the same ones the hooks and CI run. Don't improvise variants. When the `dotnet` on PATH lacks the .NET 10 SDK, use the one `scripts/dotnet.sh` finds.

## Branching and commits

- **Default flow:** commit directly to `main`, since one maintainer's pre-push hook runs the same gate as CI.
- **A feature branch** (`feat/…`, `fix/…`, `chore/…`, `docs/…`, `refactor/…`, `perf/…`), whatever the default, for work that is multi-commit and should land atomically, risky or invasive (vendored-library updates, cross-cutting refactors), spans sessions, or wants a diff review before merging. When in doubt, branch. It lands by the lifecycle kit's `complete` skill: a PR from `.github/pull_request_template.md` for a reader who never saw the review rounds, merged once every check passed, the branch deleted on both ends. A branch listed by `git branch` is either active work or it shouldn't exist.
- **A push ships nothing.** `main` is the one long-lived branch; a release publishes to players only through the release workflow (*Versioning and release* below), which creates the `v*` tag. Never create one by hand. A fix that cannot wait for what `main` holds goes on a `hotfix/…` branch cut from the last release's tag ([CONTRIBUTING.md § Hotfixes](CONTRIBUTING.md)).
- **Commits** follow [Conventional Commits](https://www.conventionalcommits.org/): a prefix of `feat`, `fix`, `refactor`, `docs`, `test`, `chore` or `perf`, an optional scope (`fix(teamspeak): …`), a subject in the imperative with no trailing period and under 72 characters, and a body, when used, that says **why**.

## Task workflow — three phases

Every tracked task moves through three phases. **Do not collapse them**: each gate is the user's, not yours to assume. The issue carries the task between sessions: a session that picks it up cold continues from the issue body, its ticked boxes and the tree, so nothing a later session needs may live only in the conversation.

The rounds, their packets and the briefs are the lifecycle kit's [task_review.md](https://github.com/orwir/lifecycle-kit/blob/main/method/task_review.md); its `clarify`, `review` and `handback` skills run them, and its `complete` skill the close-out. The board card moves by `lifecycle board`: to `In Progress` at *Clarify*, to `Done` at *Complete*.

1. **Clarify.** Read the ticket and the code it touches, ask for any missing detail, and surface scope or constraint conflicts *before* writing code. Write the issue's *Plan* and run the plan round where it applies. Implementation begins only once the plan is accepted: by the maintainer's "go", or by you where [task_review.md](https://github.com/orwir/lifecycle-kit/blob/main/method/task_review.md) § The plan round awaits none. Accepting a plan opens implementation and nothing more.
2. **Implement & stop.** Do the work in full to the *Definition of Done*, ticking each Technical-details box as its path lands and recording a departure under the plan's *Departures* when you make it. Run the local gate in this session, say what could not run, and run the work round. Then **stop and hand back** with the hand-back brief, the changes **uncommitted** outside a feature branch. Corrections, and answers to the brief given without the go-ahead, are worked here, then you stop again. A gap found in one of the kit's guides is filed with `lifecycle gap` (task_review.md § Developer rules).
3. **Complete — on explicit approval only.** An explicit go-ahead ("complete the task", "lgtm") is the *only* trigger to commit the task work, push, close the ticket and delete a merged feature branch. Answers to the brief given with it are made first, within the size the brief stated; a veto, or a bigger change, renders the brief again and waits for a new go-ahead. The same gate holds `git push`, `gh pr create` and `gh pr merge`: auto mode is permission to write code without re-asking, not permission to land it.

What the kit's skills read here:
- **Parts a decision can touch:** the clock, speech, TeamSpeak, settings, the UI and the release, as the `area/…` labels name them.
- **Evidence a root cause is confirmed from:** Gengfu's log (`gengfu.log` in its folder), Velopack's log for an install or an update (`docs/guides/release.md`), a live check on a real or local TeamSpeak server, a failing test, and the player's report.
- **Drawn surfaces:** the main window, Settings and the app icon, each with its design file on the canvas ([docs/guides/design_language.md](docs/guides/design_language.md) § Screens); when a change to one is drawn and closed is [design_workflow.md](https://github.com/orwir/lifecycle-kit/blob/main/method/design_workflow.md)'s.

### Definition of Done

1. **It serves the ticket's goal at real scale, not only its named mechanism** — a whole match, the longest locale (Russian), the real server. If it serves the goal badly, raise it at *Clarify* or take it through the design loop; never ship it because it was named.
2. **It was used the way its user uses it.** A UI change is looked at running: light and dark, English and Russian, by keyboard alone. A TeamSpeak change is heard on a real server. Not having looked is a task not done.
3. **Every scenario in the plan, and every other new behaviour, has a test that fails without it.**
4. **The gate is green and the work round ran**, or the brief says why it was skipped, as an untracked micro-task's does.
5. **The docs are in sync** (*Change → required doc mapping*): every doc the change touches, a `CHANGELOG.md` entry for any user-visible change, new settings, commands, labels or workflows documented where someone would look, an ADR for a non-obvious decision (a box in the plan) referenced where it surfaces, and a pattern reusable across projects filed with `lifecycle gap`.

## Code layout

- `src/Gengfu.App/` — the WPF shell and entry point: windows, input, speech (Windows TTS → Opus), updates. Thin: it wires Core to TeamSpeak.
- `src/Gengfu.TeamSpeak/` — TeamSpeak transport over TSLib: address lookup, connection, whisper, voice frames.
- `src/Gengfu.Core/` — pure logic: match clock, call schedule, sync rules, settings model. **Never reference WPF, Windows-only APIs or TSLib from `Gengfu.Core`.** It must be testable without the UI or a server.
- `tests/` mirrors `src/` 1:1: `src/Gengfu.Core/Foo.cs` → `tests/Gengfu.Core.Tests/FooTests.cs`.
- `lib/` is vendored third-party code. Change it only through the patch list in [lib/README.md](lib/README.md); never reformat it.
- `scripts/` is for tooling. Don't move runtime code here. No new top-level directories without user sign-off.

## Versioning and release

- The version string lives in exactly one place: `Directory.Build.props` → `<Version>`. The About entry (`AppVersion.Current`) and the release workflow read it from the build. **Do not duplicate it.**
- Update `CHANGELOG.md` for any user-visible change: **one claim per entry, at most 120 characters before the `(#N)`**. The rest of the style is [project_kickstart.md § CHANGELOG style](https://github.com/orwir/lifecycle-kit/blob/main/method/project_kickstart.md).
- **Releases run by the lifecycle kit's `release` skill: prepare, review, ship.** The steps are [CONTRIBUTING.md § Releasing](CONTRIBUTING.md) and § Hotfixes, run as written.

## Tests and quality gates

- Tests run with xUnit via `dotnet test`. Pure logic takes its time from a fake clock, never the wall clock.
- Coverage floor: 80% of `Gengfu.Core` lines, enforced by `scripts/check.sh`. Add tests, don't lower the floor.
- Integration tests hit real I/O — a real filesystem for settings, a real local socket for address lookup. **Do not mock the filesystem in integration tests.**
- **Tests, harnesses and checks run on folders of their own** — a temp folder for a `Session`, `GENGFU_HOME` for Gengfu itself — never on the user's settings, secrets or install. The smoke install refuses to run where Gengfu is installed.
- Lint + format: `dotnet format` with `.editorconfig` only. Outside `lib/` the build treats warnings as errors.

## Documentation

- Layers: `README.md` + `README.ru.md` (the front page: what Gengfu is, install) · `docs/guide.md` + `docs/guide.ru.md` (the player guide: install, set up, use) · `CONTRIBUTING.md` (setting up, releasing, hotfixing) · `CHANGELOG.md` · `docs/product/` (vision incl. CUJs; `idea.md` is frozen — never edit it) · `docs/architecture/architecture.md` (the how) · `docs/guides/` (Gengfu's own howtos: `design_language.md`, `release.md`; the lifecycle kit's guides are read from the installed kit, not copied here) · `docs/adr/` (one record per significant choice; index in `docs/README.md`).
- **Player docs come in pairs**, English and Russian: a change to one updates its twin in the same commit, the Russian in the app's own Russian labels (`src/Gengfu.App/Strings/Strings.ru.resx`).
- **A new documentation file is written only where the accepted plan has a box for it, or, outside a tracked task, where the user asks for it.** An ADR for a non-obvious decision is proposed as a box; a file a mapping row names that Gengfu lacks is surfaced. A file a kit flow writes (a review's report, an upgrade's port) is that flow's.
- **Do not recreate `roadmap.md`** — tracked work lives on GitHub.
- Cite code as `file.cs:123` in conversation; in docs use file-relative links and no `#L123` anchors.
- Docs open with orientation (what, for whom, what not), put the conclusion first, keep one claim per paragraph, and use one name per thing (`Gengfu`, `ADR-0002`).

### Change → required doc mapping

| Change | Must update |
| --- | --- |
| New user-facing control, screen or behaviour | `docs/guide.md` + `docs/guide.ru.md`; `README.md` + `README.ru.md` when it changes their bullets or install steps |
| New setting or changed default | `docs/guide.md` + `docs/guide.ru.md` + `docs/architecture/architecture.md` § Configuration; `README.md` + `README.ru.md` when it changes their bullets or install steps |
| New component, changed data flow, new integration | `docs/architecture/architecture.md` |
| Non-obvious design decision | new ADR in `docs/adr/` + its row in `docs/README.md` |
| Any user-visible change | `CHANGELOG.md` |
| New/changed UI component or pattern | `docs/guides/design_language.md` (and the design canvas) |
| Visible change to the main window or Settings | The front-page teaser in both READMEs: the maintainer re-renders it from its source, the private repository `orwir/gengfu-teaser`, and uploads it again |
| New or removed user journey | `docs/product/vision.md` § CUJs |
| Release or packaging change | `docs/guides/release.md`; the install steps of both READMEs and both guides when what a player does changes |
| Process / workflow change | this file, or `CONTRIBUTING.md` for setup and releasing; a pattern reusable across projects is filed with `lifecycle gap` |

## Issues and pull requests

- New issues use `.github/ISSUE_TEMPLATE/` (`bug.md`, `task.md`).
- An issue is captured with no plan; its *Plan* is written at *Clarify*, and changes after acceptance only through its *Departures*. Tick a Technical-details box when its path lands; never edit prose to track progress.
- **Labels:** one `type/…`, one `area/…` and one `priority/…` on every issue.
- **The board:** every write is `lifecycle board <N> <column>`, which puts the card at the top of its column: **Backlog** when you file an issue; the other moves are *Task workflow*'s, and `Backlog → Ready` is the maintainer's triage. Ready's order is the plan: the top card is next. The columns, the workflows the board keeps on, two-stage issues and the label taxonomy: [project_kickstart.md § GitHub tracking setup](https://github.com/orwir/lifecycle-kit/blob/main/method/project_kickstart.md).
- **A `kit-gap` issue** is the lifecycle kit's work, filed here by `lifecycle gap` for its maintainer to lift: it takes none of these rules (no template, no label from the list, no card on the board).
- PRs use `.github/pull_request_template.md`: Summary, `Closes #N`, concrete Verification. Never invent issue numbers — check with `gh issue view <N>`.
- Dependabot PRs: read the changelog first; patch/minor may merge once CI is green, majors get a manual review.

## Secrets and security

- Never commit secrets: server passwords, the bot identity, tokens, signing material. Local test settings live in git-ignored `*.local.*` files.
- Never log or print the server password or the identity's private key — log the server address and nickname instead, and refer to secrets by name only.
- New CI workflows declare least-privilege `permissions:` blocks.

## Executing actions

- Freely: edit files, run the gate, create feature branches. Confirm first: `git reset --hard`, force-pushing, deleting branches with unmerged work, deleting a user's settings or identity file, pushing to shared remotes, publishing a release.
- Tool approval in one scope does not extend to other scopes. Investigate unfamiliar files before deleting — they may be the user's work. Diagnose failing checks; never bypass them.

## When editing code

- **Simplicity First** ([codebase_review.md](https://github.com/orwir/lifecycle-kit/blob/main/method/codebase_review.md)): when a load-bearing abstraction no longer earns its shape, surface the simplification before patching around it.
- **Stay in scope.** No drive-by refactors, renames, dependency bumps or reformatting.
- **Conform to the design language.** Every UI edit reuses [docs/guides/design_language.md](docs/guides/design_language.md); one screen, one design file (pairing table there). New screens and visual changes are designed first ([design_workflow.md](https://github.com/orwir/lifecycle-kit/blob/main/method/design_workflow.md)) and approved before code.
- Comments explain **why**, never what. Budget: 6 lines per comment block, 12 per doc comment, 8 per file header, measured by [codebase_review.md](https://github.com/orwir/lifecycle-kit/blob/main/method/codebase_review.md) § Phase 2 on 2026-11-24 over 377 comment blocks (longest 5), 362 doc comments (longest 10) and 1 file header (4). Rationale beyond that goes to an ADR or the issue.
- Write for the next editor: present tense, the surviving constraint only; no history, no tracker tombstones.
- No compatibility shims, feature flags for work that can ship, half-finished implementations, or error handling for impossible cases. Validate at boundaries: user input, TeamSpeak, files.

## What NOT to do

- Do not add dependencies casually.
- Do not use emojis in code or docs unless asked.
- Do not improvise a review process: a comprehensive review follows the kit's guide for it, on [review_machinery.md](https://github.com/orwir/lifecycle-kit/blob/main/method/review_machinery.md), which names them.

## When in doubt

1. Check whether an existing file already solves the problem.
2. Mid-task fork: internal and reversible → decide and record it under the plan's *Departures* (in the hand-back where there is no plan); user-visible, hard to undo or scope-changing → stop and ask.
