# Agent instructions — Gengfu

You are working in the `orwir/gengfu` repository: a C# / .NET 10 Windows desktop app (a TeamSpeak bot that whispers jungle calls for Where Winds Meet guild wars, on a clock matched to the in-game timer). Follow every rule below on every code change. When a rule conflicts with a direct user instruction in the current session, ask the user to clarify before proceeding.

For the full rationale behind any rule, read [CONTRIBUTING.md](CONTRIBUTING.md) and [docs/guides/project_kickstart.md](docs/guides/project_kickstart.md). These rules are the compressed operating contract; those files are the source of truth when details are ambiguous.

**Hard gates, before anything else:** never push to `master`; never commit, push, merge, or close work without explicit approval (see *Task workflow*); never bypass hooks with `--no-verify`; never commit or print secrets. Everything below is detail around these four.

---

## Commands

- Setup: `powershell -File scripts/setup.ps1` (git hooks + .NET 10 SDK check)
- Gate — format, build, test, coverage: `scripts/check.sh`
- Format: `dotnet format Gengfu.slnx --exclude lib`
- Run locally, on a folder of its own: `GENGFU_HOME=<folder> dotnet run --project src/Gengfu.App`

These are the canonical invocations — the same ones the hooks and CI run. Don't improvise variants. When the `dotnet` on PATH lacks the .NET 10 SDK, use the one `scripts/dotnet.sh` finds.

## Branching and commits

- **Default flow:** commit directly to `develop`. Solo-maintained with AI assistants; the pre-push hook runs the same gate as CI. Stick to it; mixing flows breaks the project board lifecycle.
- **When to use a feature branch:** open one (`feat/…`, `fix/…`, `chore/…`, `docs/…`, `refactor/…`, `perf/…`) when the work is multi-commit and should land atomically, is risky or invasive (vendored-library updates, cross-cutting refactors), spans multiple sessions, or benefits from a diff-review pass before merging. When in doubt, branch.
- **Never push directly to `master`.** It triggers the release pipeline; merges only via release PRs from `develop`.
- **Automated feature-branch flow:**
  ```bash
  git checkout -b feat/my-thing
  # ... work, commit, push ...
  git push -u origin feat/my-thing
  gh pr create --fill --base develop
  gh pr merge --squash --auto --delete-branch
  ```
  `--delete-branch` removes only the **remote** copy — once the merge lands, delete the local branch too: `git checkout develop && git pull && git branch -d feat/my-thing && git fetch --prune`.
- Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/): prefixes `feat`, `fix`, `refactor`, `docs`, `test`, `chore`, `perf`; optional scope (`fix(teamspeak): …`); imperative subject, no trailing period, under 72 characters; the body explains **why**.
- Never use `--no-verify`. If a hook fails, fix the underlying issue; if the hook is wrong, say so and propose a fix.

## Task workflow — three phases

Every tracked task moves through three explicit phases. **Do not collapse them** — each gate is the user's, not yours to assume.

1. **Clarify.** Read the ticket and/or description, ask for any missing detail, and surface scope or constraint conflicts *before* writing code. Move the board item to `In Progress` when you pick it up (skip the board steps for untracked micro-tasks; the approval gates still apply).
2. **Implement & stop.** Do the work in full — code, tests, docs — to the *Definition of Done* below. Run the local gate — never report it as passed without having run it in this session, and say so explicitly if something couldn't be run. Run the task review round ([docs/guides/task_review.md](docs/guides/task_review.md)), then **stop and hand back** with the hand-back brief: on `develop` leave the changes **uncommitted**; on a feature branch WIP commits are fine. Do **not** push, merge, or close anything in this phase.
3. **Complete — on explicit approval only.** An explicit go-ahead ("complete the task", "lgtm") is the *only* trigger to commit, push, close the ticket, move the board item to `Done`, and delete a merged feature branch, remote and local. The same gate applies to `git push`, `gh pr create`, and `gh pr merge` — auto mode is permission to write code without re-asking, not permission to land it.

### Definition of Done

1. **It serves the ticket's goal at real scale, not only its named mechanism** — a whole match, the longest locale (Russian), the real server. If it serves the goal badly, raise it at *Clarify* or take it through the design loop; never ship it because it was named.
2. **It was used the way its user uses it.** A UI change is looked at running: light and dark, English and Russian, by keyboard alone. A TeamSpeak change is heard on a real server. Not having looked is a task not done.
3. **Every new behaviour has a test that fails without it.**
4. **The gate is green and the review round ran.**
5. **The docs are in sync:** the *Task-closing checklist*.

## Code layout

- `src/Gengfu.App/` — the WPF shell and entry point: windows, input, speech (Windows TTS → Opus), updates. Thin: it wires Core to TeamSpeak.
- `src/Gengfu.TeamSpeak/` — TeamSpeak transport over TSLib: address lookup, connection, whisper, voice frames.
- `src/Gengfu.Core/` — pure logic: match clock, call schedule, sync rules, settings model. **Never reference WPF, Windows-only APIs or TSLib from `Gengfu.Core`.** It must be testable without the UI or a server.
- `tests/` mirrors `src/` 1:1: `src/Gengfu.Core/Foo.cs` → `tests/Gengfu.Core.Tests/FooTests.cs`.
- `lib/` is vendored third-party code. Change it only through the patch list in [lib/README.md](lib/README.md); never reformat it.
- `scripts/` is for tooling. Don't move runtime code here. No new top-level directories without user sign-off.

## Versioning and release

- The version string lives in exactly one place: `Directory.Build.props` → `<Version>`. The About entry (`AppVersion.Current`) and the release workflow read it from the build. **Do not duplicate it.**
- Update `CHANGELOG.md` for any user-visible change: **one claim per entry, at most 120 characters before the `(#N)`**, grouped Added / Changed / Fixed / Removed once per version, no rationale prose.
- Releases follow the numbered checklist in [CONTRIBUTING.md § Releasing](CONTRIBUTING.md), in three phases: **Prepare** (pick the semver bump, bump `<Version>`, date the CHANGELOG, count the copies in use, open the `develop → master` PR), **Review** (stop, surface the PR URL and a summary, wait for "proceed"), **Ship — on explicit approval only** (merge, which tags and publishes through `release.yml`; fast-forward `develop`, close the milestone, verify). A prior "do the release" does not extend past the review checkpoint.

## Tests and quality gates

- Every new feature or bug fix gets tests. New code without tests is incomplete.
- Tests run with xUnit via `dotnet test`. Pure logic takes its time from a fake clock, never the wall clock.
- Coverage floor: 80% of `Gengfu.Core` lines, enforced by `scripts/check.sh` (pre-push and CI). Add tests, don't lower the floor.
- Integration tests hit real I/O — a real filesystem for settings, a real local socket for address lookup. **Do not mock the filesystem in integration tests.**
- **Tests, harnesses and checks run on folders of their own** — a temp folder for a `Session`, `GENGFU_HOME` for Gengfu itself — never on the user's settings, secrets or install. The smoke install refuses to run where Gengfu is installed.
- Lint + format: `dotnet format` with `.editorconfig` only. Outside `lib/` the build treats warnings as errors.

## Documentation

- Layers: `README.md` (players: install, use) · `CONTRIBUTING.md` (how to work here) · `CHANGELOG.md` · `docs/product/` (vision incl. CUJs; `idea.md` is frozen — never edit it) · `docs/architecture/architecture.md` (the how) · `docs/guides/` (howtos, playbooks, `design_language.md`) · `docs/adr/` (one record per significant choice; index in `docs/README.md`).
- **Do not create new documentation files unprompted**; suggest an ADR and wait. **Do not recreate `roadmap.md`** — tracked work lives on GitHub.
- Cite code as `file.cs:123` in conversation; in docs use file-relative links and no `#L123` anchors.
- Docs open with orientation (what, for whom, what not), put the conclusion first, keep one claim per paragraph, and use one name per thing (`Gengfu`, `ADR-0002`).

### Change → required doc mapping

| Change | Must update |
| --- | --- |
| New user-facing control, screen or behaviour | `README.md` |
| New setting or changed default | `README.md` + `docs/architecture/architecture.md` § Configuration |
| New component, changed data flow, new integration | `docs/architecture/architecture.md` |
| Non-obvious design decision | new ADR in `docs/adr/` + its row in `docs/README.md` |
| Any user-visible change | `CHANGELOG.md` |
| New/changed UI component or pattern | `docs/guides/design_language.md` (and the design canvas) |
| New or removed user journey | `docs/product/vision.md` § CUJs |
| Change to vendored code | `lib/README.md` § Local patches |
| Release or packaging change | `docs/guides/release.md` (created with the release issue) |
| Process / workflow change | `CONTRIBUTING.md`; if reusable, propose it for the gist's `project_kickstart.md`, which `docs/guides/` mirrors |

### Task-closing checklist

1. Every doc touched by the change is up to date.
2. `CHANGELOG.md` has an entry for any user-visible change.
3. New settings, commands, labels or workflows are documented where someone would look for them.
4. Non-obvious decisions have an ADR, referenced from the code or doc where they surface.
5. A reusable pattern is reflected in the relevant guide.

## Issues and pull requests

- New issues use `.github/ISSUE_TEMPLATE/` (`bug.md`, `task.md`); `gh issue create --body` bypasses them — reproduce their structure.
- Technical details use file-path checkboxes with `[NEW]` / `[MODIFY]` / `[DELETE]` markers; tick boxes to track progress, never edit prose.
- Label every issue on three axes (`type/…`, `area/…`, `priority/…`), add it to the milestone if scoped, and add it to the project board with Status = **Backlog** — never "No Status".
- Board transitions: `Backlog → Ready` is the maintainer's call. Move an issue to **In Progress** as your first step when you start it, and to **Done** in the same close-out that closes it. After every board write, call `updateProjectV2ItemPosition` with `afterId: null` so the item lands at the top of its column.
- PRs use `.github/pull_request_template.md`: Summary, `Closes #N`, concrete Verification. Never invent issue numbers — check with `gh issue view <N>`.
- Dependabot PRs: read the changelog first; patch/minor may auto-merge once CI is green, majors get a manual review.

## Secrets and security

- Never commit secrets: server passwords, the bot identity, tokens, signing material. Local test settings live in git-ignored `*.local.*` files.
- Never log or print the server password or the identity's private key — log the server address and nickname instead, and refer to secrets by name only.
- New CI workflows declare least-privilege `permissions:` blocks.
- Releases are unsigned so far. When code signing is added, document the certificate's location and rotation in `docs/guides/release.md`.

## Executing actions

- Freely: edit files, run the gate, create feature branches. Confirm first: `git reset --hard`, force-pushing, deleting branches with unmerged work, deleting a user's settings or identity file, pushing to shared remotes, publishing a release.
- Tool approval in one scope does not extend to other scopes. Investigate unfamiliar files before deleting — they may be the user's work. Diagnose failing checks; never bypass them.

## When editing code

- **Simplicity First** ([docs/guides/codebase_review.md](docs/guides/codebase_review.md)): when a load-bearing abstraction no longer earns its shape, surface the simplification before patching around it.
- **Stay in scope.** No drive-by refactors, renames, dependency bumps or reformatting. Prefer editing existing files.
- **Conform to the design language.** Every UI edit reuses [docs/guides/design_language.md](docs/guides/design_language.md); one screen, one design file (pairing table there). New screens and visual changes are designed first ([docs/guides/design_workflow.md](docs/guides/design_workflow.md)) and approved before code.
- Comments explain **why**, never what. Budget: 6 lines per comment block, 12 per doc comment, 8 per file header — provisional, re-derive per `codebase_review.md` § Phase 2 after v0.1. Rationale beyond that goes to an ADR or the issue.
- Write for the next editor: present tense, the surviving constraint only; no history, no tracker tombstones.
- No compatibility shims, feature flags for work that can ship, half-finished implementations, or error handling for impossible cases. Validate at boundaries: user input, TeamSpeak, files.

## What NOT to do

- Do not push to `master`, duplicate the version string, or use `--no-verify`.
- Do not reference WPF, Windows APIs or TSLib from `Gengfu.Core`.
- Do not edit `lib/` outside the patch process.
- Do not commit secrets, identities or local `*.local.*` files.
- Do not recreate `roadmap.md` or create documentation files unprompted.
- Do not leave a merged feature branch behind.
- Do not add dependencies casually, or competing lint/format tools.
- Do not use emojis in code or docs unless asked.
- Do not improvise a review process — use `task_review.md`, `codebase_review.md`, `ux_review.md`.

## When in doubt

1. Read the relevant section of `CONTRIBUTING.md` or `docs/guides/project_kickstart.md`.
2. Check whether an existing file already solves the problem.
3. Mid-task fork: internal and reversible → decide and note it in the hand-back; user-visible, hard to undo or scope-changing → stop and ask.
