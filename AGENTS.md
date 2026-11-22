# Agent instructions — Gengfu

You are working in the `orwir/gengfu` repository: a C# / .NET 10 Windows desktop app (a TeamSpeak bot that whispers jungle calls for Where Winds Meet guild wars, on a clock matched to the in-game timer). Follow every rule below on every code change. When a rule conflicts with a direct user instruction in the current session, ask the user to clarify before proceeding.

These rules are the compressed operating contract. Why each exists is the lifecycle kit's [project_kickstart.md](https://github.com/orwir/lifecycle-kit/blob/main/method/project_kickstart.md); setting up and releasing is [CONTRIBUTING.md](CONTRIBUTING.md). Every link to the lifecycle kit, here or in any other file, opens its `main` on GitHub: read the file at the same path in the folder `lifecycle where` prints instead (`method/…`, `docs/guide.md`), the release this machine runs.

**Hard gates, before anything else:** never start a release workflow without the release's own "proceed"; never commit, push, merge, or close work without explicit approval (see *Task workflow*); never bypass hooks with `--no-verify`; never commit or print secrets. Everything below is detail around these four.

---

## Commands

- Setup: `powershell -File scripts/setup.ps1` (git hooks + .NET 10 SDK check)
- Gate — format, build, test, coverage: `scripts/check.sh`
- Format: `dotnet format Gengfu.slnx --exclude lib`
- Run locally, on a folder of its own: `GENGFU_HOME=<folder> dotnet run --project src/Gengfu.App`
- Lifecycle: `lifecycle doctor` before a task; `.lifecycle.toml` holds this repository's settings for the lifecycle kit's skills and command; without the kit's Claude plugin, use `bin/lifecycle` and `skills/*/SKILL.md` in a checkout of the kit (`lifecycle where` names it), and with neither, say so and stop before any round

These are the canonical invocations — the same ones the hooks and CI run. Don't improvise variants. When the `dotnet` on PATH lacks the .NET 10 SDK, use the one `scripts/dotnet.sh` finds.

## Branching and commits

- **Default flow:** commit directly to `main`. Solo-maintained with AI assistants; the pre-push hook runs the same gate as CI. Stick to it; mixing flows breaks the project board lifecycle.
- **When to use a feature branch:** open one (`feat/…`, `fix/…`, `chore/…`, `docs/…`, `refactor/…`, `perf/…`) when the work is multi-commit and should land atomically, is risky or invasive (vendored-library updates, cross-cutting refactors), spans multiple sessions, or benefits from a diff-review pass before merging. When in doubt, branch.
- **A push ships nothing.** `main` is the one long-lived branch; a release publishes to players only through the release workflow, dispatched on the commit the release review read (*Versioning and release* below), which creates the `v*` tag. Never create one by hand. A fix that cannot wait for what `main` holds goes on a `hotfix/…` branch cut from the last release's tag ([CONTRIBUTING.md § Hotfixes](CONTRIBUTING.md)).
- **Automated feature-branch flow:**
  ```bash
  git checkout -b feat/my-thing
  # ... work, commit, push ...
  git push -u origin feat/my-thing
  gh pr create --base main --title "<subject>" --body-file <pr.md outside the tree>
  gh pr checks feat/my-thing --watch && gh pr merge feat/my-thing --squash --delete-branch
  git fetch --prune
  ```
  The PR body is `.github/pull_request_template.md`, filled for a reader who never saw the review rounds; `--fill` would send the commit messages instead. The merge runs only once `--watch` exits 0, every check passed (before the first check registers it reports none and fails: run it again); `--delete-branch` then deletes the branch on GitHub and here, after switching to `main` and pulling it, and `git fetch --prune` drops its remote-tracking ref.
- Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/): prefixes `feat`, `fix`, `refactor`, `docs`, `test`, `chore`, `perf`; optional scope (`fix(teamspeak): …`); imperative subject, no trailing period, under 72 characters; the body explains **why**.
- Never use `--no-verify`. If a hook fails, fix the underlying issue; if the hook is wrong, say so and propose a fix.

## Task workflow — three phases

Every tracked task moves through three explicit phases. **Do not collapse them** — each gate is the user's, not yours to assume. The issue carries the task between sessions: a session that picks it up cold continues from the issue body, its ticked boxes and the tree, so nothing a later session needs may live only in the conversation.

1. **Clarify.** Read the ticket and the code it touches, ask for any missing detail, and surface scope or constraint conflicts *before* writing code. Move the board item to `In Progress` when you pick it up (an untracked task has no board item; the approval gates still apply). Write the issue's *Plan* and run the plan round where it applies. Implementation begins only once the plan is accepted: by the maintainer's "go", or, when the round was skipped and nothing is left to answer, by you. Accepting a plan opens implementation and nothing more.
2. **Implement & stop.** Do the work in full to the *Definition of Done* below, ticking each Technical-details box as its path lands and recording a departure under the plan's *Departures* when you make it. Run the local gate in this session, and say what could not run. Run the work round, then **stop and hand back** with the hand-back brief: on `main` the changes stay **uncommitted**; on a feature branch WIP commits are fine. Corrections, and answers to the brief given without the go-ahead, are worked here, then you stop again. Do **not** push, merge, or close anything in this phase.
   - The rounds, their packets and the briefs are the lifecycle kit's [task_review.md](https://github.com/orwir/lifecycle-kit/blob/main/method/task_review.md); its `clarify`, `review` and `handback` skills run them, and its `complete` skill the close-out.
3. **Complete — on explicit approval only.** An explicit go-ahead ("complete the task", "lgtm") is the *only* trigger to commit, push, close the ticket, move the board item to `Done`, and delete a merged feature branch, remote and local. Answers to the brief given with the go-ahead are made first, within the size the brief stated; a veto, or a bigger change, renders the brief again and waits for a new go-ahead. A gap found in one of the kit's guides is filed on the kit as an issue when it is found ([task_review.md](https://github.com/orwir/lifecycle-kit/blob/main/method/task_review.md) § Developer rules). The same gate applies to `git push`, `gh pr create` and `gh pr merge`: auto mode is permission to write code without re-asking, not permission to land it.

### Definition of Done

1. **It serves the ticket's goal at real scale, not only its named mechanism** — a whole match, the longest locale (Russian), the real server. If it serves the goal badly, raise it at *Clarify* or take it through the design loop; never ship it because it was named.
2. **It was used the way its user uses it.** A UI change is looked at running: light and dark, English and Russian, by keyboard alone. A TeamSpeak change is heard on a real server. Not having looked is a task not done.
3. **Every scenario in the plan, and every other new behaviour, has a test that fails without it.**
4. **The gate is green and the work round ran.**
5. **The docs are in sync** (*Change → required doc mapping*): every doc the change touches, a `CHANGELOG.md` entry for any user-visible change, new settings, commands, labels or workflows documented where someone would look, an ADR for a non-obvious decision referenced where it surfaces, and a pattern reusable across projects filed on the lifecycle kit.

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
- **Releases run by the lifecycle kit's `release` skill: prepare, review, ship.** The ship phase starts only on the maintainer's own "proceed" for that release; a prior "do the release" stops at the review. The steps are [CONTRIBUTING.md § Releasing](CONTRIBUTING.md) and § Hotfixes, run as written.

## Tests and quality gates

- Every new feature or bug fix gets tests. New code without tests is incomplete.
- Tests run with xUnit via `dotnet test`. Pure logic takes its time from a fake clock, never the wall clock.
- Coverage floor: 80% of `Gengfu.Core` lines, enforced by `scripts/check.sh` (pre-push and CI). Add tests, don't lower the floor.
- Integration tests hit real I/O — a real filesystem for settings, a real local socket for address lookup. **Do not mock the filesystem in integration tests.**
- **Tests, harnesses and checks run on folders of their own** — a temp folder for a `Session`, `GENGFU_HOME` for Gengfu itself — never on the user's settings, secrets or install. The smoke install refuses to run where Gengfu is installed.
- Lint + format: `dotnet format` with `.editorconfig` only. Outside `lib/` the build treats warnings as errors.

## Documentation

- Layers: `README.md` + `README.ru.md` (the front page: what Gengfu is, install) · `docs/guide.md` + `docs/guide.ru.md` (the player guide: install, set up, use) · `CONTRIBUTING.md` (setting up, releasing, hotfixing; the design round, vendored code, player docs) · `CHANGELOG.md` · `docs/product/` (vision incl. CUJs; `idea.md` is frozen — never edit it) · `docs/architecture/architecture.md` (the how) · `docs/guides/` (Gengfu's own howtos: `design_language.md`, `release.md`; the lifecycle kit's guides are read from the installed kit, not copied here, and a gap found in one is filed on the kit as an issue) · `docs/adr/` (one record per significant choice; index in `docs/README.md`).
- **Player docs come in pairs**, English and Russian: a change to one updates its twin in the same commit, the Russian in the app's own Russian labels (`src/Gengfu.App/Strings/Strings.ru.resx`).
- **Do not create new documentation files unprompted**; suggest an ADR and wait. **Do not recreate `roadmap.md`** — tracked work lives on GitHub.
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
| Visible change to the main window or Settings | The front-page teaser in both READMEs: the maintainer re-renders it and uploads it again |
| New or removed user journey | `docs/product/vision.md` § CUJs |
| Change to vendored code | `lib/README.md` § Local patches |
| Release or packaging change | `docs/guides/release.md` (created with the release issue); the install steps of both READMEs and both guides when what a player does changes |
| Process / workflow change | this file, or `CONTRIBUTING.md` for setup and releasing; a pattern reusable across projects is filed on the lifecycle kit |

## Issues and pull requests

- New issues use `.github/ISSUE_TEMPLATE/` (`bug.md`, `task.md`); `gh issue create --body` bypasses them — reproduce their structure.
- An issue is captured with no plan; its *Plan* is written at *Clarify*, and changes after acceptance only through its *Departures*. Tick a Technical-details box when its path lands; never edit prose to track progress.
- **Labels:** one `type/…`, one `area/…` and one `priority/…` on every issue. Add the issue to the milestone if scoped.
- **The board:** every write is `lifecycle board <N> <column>`, which puts the card at the top of its column: **Backlog** when you file an issue; the other moves are *Task workflow*'s, and `Backlog → Ready` is the maintainer's triage. What each column means, milestones, why an issue is filed in two stages, and the label taxonomy behind the labels above: [project_kickstart.md § GitHub tracking setup](https://github.com/orwir/lifecycle-kit/blob/main/method/project_kickstart.md).
- PRs use `.github/pull_request_template.md`: Summary, `Closes #N`, concrete Verification. Never invent issue numbers — check with `gh issue view <N>`.
- Dependabot PRs: read the changelog first; patch/minor may merge once CI is green, majors get a manual review.

## Secrets and security

- Never commit secrets: server passwords, the bot identity, tokens, signing material. Local test settings live in git-ignored `*.local.*` files.
- Never log or print the server password or the identity's private key — log the server address and nickname instead, and refer to secrets by name only.
- New CI workflows declare least-privilege `permissions:` blocks.
- Releases are unsigned so far. When code signing is added, document the certificate's location and rotation in `docs/guides/release.md`.

## Executing actions

- Freely: edit files, run the gate, create feature branches. Confirm first: `git reset --hard`, force-pushing, deleting branches with unmerged work, deleting a user's settings or identity file, pushing to shared remotes, publishing a release.
- Tool approval in one scope does not extend to other scopes. Investigate unfamiliar files before deleting — they may be the user's work. Diagnose failing checks; never bypass them.

## When editing code

- **Simplicity First** ([codebase_review.md](https://github.com/orwir/lifecycle-kit/blob/main/method/codebase_review.md)): when a load-bearing abstraction no longer earns its shape, surface the simplification before patching around it.
- **Stay in scope.** No drive-by refactors, renames, dependency bumps or reformatting. Prefer editing existing files.
- **Conform to the design language.** Every UI edit reuses [docs/guides/design_language.md](docs/guides/design_language.md); one screen, one design file (pairing table there). New screens and visual changes are designed first ([design_workflow.md](https://github.com/orwir/lifecycle-kit/blob/main/method/design_workflow.md)) and approved before code.
- Comments explain **why**, never what. Budget: 6 lines per comment block, 12 per doc comment, 8 per file header — provisional, re-derive per [codebase_review.md](https://github.com/orwir/lifecycle-kit/blob/main/method/codebase_review.md) § Phase 2 after v0.1. Rationale beyond that goes to an ADR or the issue.
- Write for the next editor: present tense, the surviving constraint only; no history, no tracker tombstones.
- No compatibility shims, feature flags for work that can ship, half-finished implementations, or error handling for impossible cases. Validate at boundaries: user input, TeamSpeak, files.

## What NOT to do

- Do not reference WPF, Windows APIs or TSLib from `Gengfu.Core`.
- Do not edit `lib/` outside the patch process.
- Do not add dependencies casually.
- Do not use emojis in code or docs unless asked.
- Do not improvise a review process: the comprehensive reviews follow the kit's guides on [review_machinery.md](https://github.com/orwir/lifecycle-kit/blob/main/method/review_machinery.md) — [codebase_review.md](https://github.com/orwir/lifecycle-kit/blob/main/method/codebase_review.md), [ux_review.md](https://github.com/orwir/lifecycle-kit/blob/main/method/ux_review.md), [ia_review.md](https://github.com/orwir/lifecycle-kit/blob/main/method/ia_review.md) and [design_review.md](https://github.com/orwir/lifecycle-kit/blob/main/method/design_review.md).

## When in doubt

1. Read the lifecycle kit's guide for it: [project_kickstart.md](https://github.com/orwir/lifecycle-kit/blob/main/method/project_kickstart.md) says why each rule exists.
2. Check whether an existing file already solves the problem.
3. Mid-task fork: internal and reversible → decide and record it under the plan's *Departures* (in the hand-back where there is no plan); user-visible, hard to undo or scope-changing → stop and ask.
