# AGENTS.md template

A copy-and-fill skeleton for the operating contract that AI coding assistants (Claude Code, OpenAI Codex CLI, Cursor, Gemini, Copilot, Aider) read on every change. Drop this into the root of a new repo as `AGENTS.md`, replace the `{{placeholder}}` tokens, and delete sections that don't apply to your project type.

Companion to `project_kickstart.md`: the kickstart explains *why* each rule exists; this file is the *rule itself*, compressed to the form the assistant actually reads.

---

## How to use this template

1. **Copy** the section under "Template" (everything inside the fenced block) into a new `AGENTS.md` at your repo root.
2. **Fill placeholders.** Every `{{token}}` is a fill-in. The comments after each one tell you what kind of value goes there.
3. **Prune by project type.** Some sections include alternative paragraphs marked `[web-service]`, `[desktop]`, `[mobile]`, `[cli]`, `[library]` — keep the one that matches and delete the others. Sections with no marker apply to all project types.
4. **No vendor shims.** `AGENTS.md` is read natively by most tools (Claude Code from v2.1.277, Codex, Cursor, Windsurf, Gemini CLI, Copilot). Don't add a `CLAUDE.md`, `.claude/CLAUDE.md` or `CLAUDE.local.md`: any one of them makes Claude Code read `CLAUDE.md` only. Put Claude-only additions in `.claude/rules/`; a one-line `CLAUDE.md` containing `@AGENTS.md` is only for sessions that can't read `AGENTS.md` directly (an older Claude Code). Tool support shifts quarterly, so check the current docs of each assistant you adopt.
5. **Keep it under ~200 lines.** Longer files get skimmed instead of read. Push rationale into `CONTRIBUTING.md` and the kickstart; keep `AGENTS.md` as the compressed contract.

### What belongs in AGENTS.md (and what doesn't)

| Belongs                                                         | Belongs elsewhere                              |
| --------------------------------------------------------------- | ---------------------------------------------- |
| Rules an assistant must apply on every change                   | Rationale and history → `CONTRIBUTING.md`      |
| The smallest viable list of "don'ts"                            | Tutorials and walkthroughs → `docs/guides/`    |
| Pointers to the source-of-truth docs                            | Architecture deep-dives → `docs/architecture/` |
| Loop-safe defaults (no `--no-verify`, no destructive shortcuts) | Decision rationale → ADRs                      |

If a rule is in `AGENTS.md` only to explain *why*, move it. The contract should be terse enough that violations are obvious.

---

## Template

Everything inside the fenced block below is the template body. Copy it as-is, then fill placeholders.

````
# Agent instructions — {{PROJECT_NAME}}

You are working in the `{{OWNER}}/{{REPO}}` repository: a {{LANGUAGE}} {{PROJECT_TYPE}} ({{ONE_LINE_DESCRIPTION}}). Follow every rule below on every code change. When a rule conflicts with a direct user instruction in the current session, ask the user to clarify before proceeding.

For the full rationale behind any rule, read [CONTRIBUTING.md](CONTRIBUTING.md) and [docs/guides/project_kickstart.md](docs/guides/project_kickstart.md). These rules are the compressed operating contract; those files are the source of truth when details are ambiguous.

**Hard gates, before anything else:** never push to `{{PRODUCTION_BRANCH}}`; never commit, push, merge, or close work without explicit approval (see *Task workflow*); never bypass hooks with `--no-verify`; never commit or print secrets. Everything below is detail around these four.

---

## Commands

- Setup: `{{SETUP_CMD}}`
- Test: `{{TEST_CMD}}`
- Lint + format: `{{LINT_CMD}}`
- Run locally: `{{RUN_CMD}}`

These are the canonical invocations — the same ones the hooks and CI run. Don't improvise variants.

## Branching and commits

- **Default flow:** {{commit directly to `develop` | open a feature branch}}. {{Reason: solo-maintained with pre-push hooks mirroring CI | reason: multiple contributors review via PR}}. Pick one and stick to it; mixing flows breaks the project board lifecycle.
- **When to use a feature branch:** open one (`feat/…`, `fix/…`, `chore/…`, `docs/…`, `refactor/…`, `perf/…`) when the work is multi-commit and should land atomically, is risky or invasive (schema migrations, auth, payments, cross-cutting refactors), spans multiple sessions, or benefits from a diff-review pass before merging. When in doubt, branch.
- **Never push directly to `{{PRODUCTION_BRANCH}}`** ({{e.g., `master`, `main`, `release`}}). It {{auto-deploys | triggers the release pipeline | publishes artifacts}}; merges only via release PRs from `{{INTEGRATION_BRANCH}}`.
- **Automated feature-branch flow:**
  ```bash
  git checkout -b feat/my-thing
  # ... work, commit, push ...
  git push -u origin feat/my-thing
  gh pr create --fill --base {{INTEGRATION_BRANCH}}
  gh pr merge --squash --auto --delete-branch
  ```
  Auto-merge must be enabled once in the repo's Settings → General → Pull Requests.
  `--delete-branch` removes only the **remote** copy — once the merge lands, delete the local branch too: `git checkout {{INTEGRATION_BRANCH}} && git pull && git branch -d feat/my-thing && git fetch --prune`. A branch listed by `git branch` is either active work or it shouldn't exist.
- Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/):
  * Allowed prefixes: `feat`, `fix`, `refactor`, `docs`, `test`, `chore`, `perf`.
  * Optional scope in parentheses: `fix(parser): …`, `chore(deps): …`, `chore(ci): …`.
  * Subject in imperative mood, no trailing period, under 72 characters.
  * Body (when used) explains **why**, not what. The diff shows what.
- Never use `--no-verify` to bypass hooks. If a hook fails, fix the underlying issue. If the hook is wrong, say so and propose a fix — do not skip.

## Task workflow — three phases

Every tracked task moves through three explicit phases. **Do not collapse them** — each gate is the user's, not yours to assume.

1. **Clarify.** Read the ticket and/or description, ask for any missing detail, and surface scope or constraint conflicts *before* writing code. Move the board item to `In Progress` when you pick it up (skip the board steps for untracked micro-tasks; the approval gates still apply). Implementation does not begin until the details are clear — whether from the ticket, added clarification, or the discussion.
2. **Implement & stop.** Do the work in full — code, tests, docs, fixtures — to the *Definition of Done* below. Run the local gate (lint + tests) — never report it as passed without having run it in this session, and say so explicitly if something couldn't be run. Run the task review round, then **stop and hand back for review** with the hand-back brief (the packets stay between the two agents): on `{{INTEGRATION_BRANCH}}` leave the changes **uncommitted** in the working tree; on a feature branch intermediate WIP commits are fine (the gate is landing, not committing). If the maintainer's review turns up corrections, work them out here and stop again. Do **not** push, merge, or close anything in this phase.
   - The round is [docs/guides/task_review.md](docs/guides/task_review.md): a hand-off packet to a reviewer agent with no author context, its return packet, and the hand-back brief — the one thing the maintainer reads. An untracked micro-task skips the round, and the brief says so.
3. **Complete — on explicit approval only.** An explicit go-ahead ("complete the task", "lgtm", or equivalent) is the *only* trigger to commit the task work, push, close the ticket, move the board item to `Done`, and — when the work rode a feature branch — delete the merged branch, remote and local. No approval → no commit, no push, no close. The same gate applies to `git push`, `gh pr create`, and `gh pr merge` — auto mode is permission to write code without re-asking, not permission to land it.

### Definition of Done

A task is done when all five hold. The gate and the review round prove some of them; the rest are the developer's to show before the hand-back, and a brief that cannot claim one says which, instead of calling the task done.

1. **It serves the ticket's goal at real scale, not only its named mechanism.** A control, a copy line or a shape the ticket names is where the work starts, not a defense of how it turned out. If it serves the goal badly with real data — the whole list, the longest locale, the smallest screen — raise it at *Clarify* or take it through the design loop ([docs/guides/design_workflow.md](docs/guides/design_workflow.md)); never ship it because it was named.
2. **It was used the way its user uses it.** A UI change is looked at running, with real data: at the smallest and the largest supported width, in every shipped locale, by keyboard alone, and without scripts where the surface promises to work without them. A CLI, API or library change is called the way its caller calls it. Not having looked is a task not done, not a *Not run* line.
3. **Every new behaviour has a test that fails without it** (*Tests and quality gates*).
4. **The gate is green and the review round ran** (above).
5. **The docs are in sync:** the *Task-closing checklist*.

The bar is the maintainer's. A shortfall in 1 or 2 found after landing is unfinished work, and its fix is never declined on cost or on the demand not being there yet.

## Code layout

- `{{ENTRY_DIR}}` ({{e.g., `apps/`, `cmd/`, `bin/`}}) contains transport / entry points. These are thin — they know about {{e.g., HTTP, chat-platform APIs, CLI args, OS event loops, native UI frameworks}}.
- `{{LOGIC_DIR}}` ({{e.g., `packages/`, `lib/`, `internal/`, `core/`}}) contains pure logic. **Never import transport libraries ({{e.g., `fastapi`, `express`, `tauri`, `react-native`, `pyqt`, `cobra`}}) from `{{LOGIC_DIR}}`.** Logic must be testable without spinning up the transport.
- `tests/` mirrors the source tree 1:1. A new file `{{LOGIC_DIR}}/foo/bar.{{EXT}}` gets a new file `tests/{{LOGIC_DIR}}/foo/test_bar.{{EXT}}`. No clever grouping.
- `migrations/` (if applicable) contains numbered schema files. Don't rewrite applied migrations — add a new one.
- `scripts/` is for one-off tooling. Don't move runtime code here.
- Do not create new top-level directories without a clear reason and user sign-off.

## Versioning and release

- The version string lives in exactly one place: `{{VERSION_FILE}}` ({{e.g., `packages/core/__init__.py` → `__version__`, `package.json` → `"version"`, `Cargo.toml` → `version`, `pubspec.yaml` → `version`}}). Every other consumer ({{e.g., deploy workflow, `/health` endpoint, About dialog, `--version` flag, image tags, installer name}}) reads from it. **Do not duplicate the version string anywhere else.**
- Update `CHANGELOG.md` for any user-visible change: **one claim per entry, at most 120 characters before the `(#N)`** — a semicolon or dash welding a second claim means two entries, not a longer one. Grouped Added / Changed / Fixed / Removed once per version, referencing the issue/PR (`- Fix X (#42)`), no rationale prose. Full style spec: [docs/guides/project_kickstart.md § CHANGELOG style](docs/guides/project_kickstart.md).
- The release process is documented as a numbered checklist in [CONTRIBUTING.md § Releasing](CONTRIBUTING.md). Do not invent shortcuts.

## Release workflow — three phases

Releases run in three explicit phases too, mirroring the task workflow. Full mechanics (commands, file targets, post-merge steps) live in [CONTRIBUTING.md § Releasing](CONTRIBUTING.md); this section is the contract.

1. **Prepare.** Decide the next version yourself from [semver](https://semver.org/) based on the changes since the last release (breaking → major, additive → minor, fixes only → patch) — unless the user names a specific version. Bump the version string in `{{VERSION_FILE}}`, date the CHANGELOG entry, complete any doc-sync, commit, and open the `{{INTEGRATION_BRANCH}} → {{PRODUCTION_BRANCH}}` release PR. All reversible inside this phase; no extra approval needed.
2. **Review.** **Stop** and surface the PR URL plus a one-paragraph summary. Wait for explicit "proceed". Work out any corrections here, then stop again.
3. **Ship — on explicit approval only.** The "proceed" authorizes the entire ship phase in one batch: merge the release PR, push the `vX.Y.Z` tag, fast-forward the integration branch, publish the GitHub release, close the milestone, and verify post-deploy. A prior "do the release" instruction does **not** extend past the review checkpoint — only the phase-3 approval does. The publish steps (PR merge + tag push) are irreversible.

## Tests and quality gates

- Every new feature or bug fix gets tests. New code without tests is incomplete.
- Tests run with `{{TEST_RUNNER}}` ({{e.g., `pytest`, `vitest`, `jest`, `go test`, `cargo test`, `xctest`}}). See `{{PKG_MANIFEST}}` for config.
- Coverage floor is `{{COVERAGE_FLOOR}}%` and enforced by both pre-commit (pre-push stage) and CI. If your change drops coverage below the floor, add tests, don't lower the floor.
- Integration tests hit real I/O ({{e.g., a real SQLite database, a real filesystem, a real local HTTP listener}}). **Do not mock the {{primary-side-effect target}} in integration tests.** Unit tests of pure functions are fine without it.
- Lint + format: `{{LINT_TOOL}}` only ({{e.g., `ruff`, `eslint` + `prettier`, `gofmt` + `golangci-lint`, `rustfmt` + `clippy`, `swiftlint` + `swift-format`}}). Never introduce competing tools that do the same job — pick one per role and stick with it.
- Pre-commit hooks are wired to run the same commands as CI. If something passes locally, it should pass CI. **If this project lacks a local hook config covering lint, format, full test suite, coverage threshold, and secret scan**, set one up before the first commit — see [project_kickstart.md § Day-zero guardrails](docs/guides/project_kickstart.md). Coverage threshold and lint checks should fail the *push*, not just CI.

## Documentation

- Respect the documentation layers:
  * `README.md` — end-user front page (what, install); `docs/guide.md` — the user guide, once split off; translated twins (`*.<lang>.md`) beside each, changed in the same commit
  * `CONTRIBUTING.md` — how to work in the repo (branching, release, commit style)
  * `CHANGELOG.md` — release history
  * `docs/product/` — vision incl. CUJs, the "what" and "why"; competitors and feasibility, the research behind them; `idea.md` is the frozen origins record — never edit it
  * `docs/architecture/` — the "how": components, data flow, config reference
  * `docs/guides/` — task-oriented howtos (deploy / publish / package, extend, kickstart, comprehensive review)
  * `docs/adr/` — architecture decision records, one per significant choice
- **Do not create new documentation files unprompted.** Work from conversation context; if you need to capture a decision, suggest an ADR and wait for approval.
- **Do not recreate `roadmap.md`** or any equivalent tracking file. Tracked work lives on GitHub: issues, milestones, the project board.
- When referencing code, cite `file.{{EXT}}:123` style. Markdown links in docs use **file-relative paths** so they resolve in every renderer (GitHub web, IDE preview, static site generators). From a doc under `docs/adr/`, that means `[{{LOGIC_DIR}}/foo.{{EXT}}](../../{{LOGIC_DIR}}/foo.{{EXT}})` — workspace-root-relative paths like `[foo.{{EXT}}]({{LOGIC_DIR}}/foo.{{EXT}})` only resolve in workspace-aware IDE tooling and 404 everywhere else.
- Avoid `#L123` line anchors in long-lived docs (ADRs especially). Line numbers rot on every refactor — keep the file ref, drop the line.
- When writing an ADR, use the four-section format: Status / Context / Decision / Consequences. Number it sequentially.

### How a doc is written

The comment budget governs prose *inside* code; these govern prose in `docs/` and the root markdown. Same instinct — the reader is looking for something, and every line they read before finding it is a line you charged them.

- **Open with orientation: what this covers, who it is for, and what it deliberately does not cover.** Two or three sentences, before the first `##`. A reader who is in the wrong document must be able to leave inside five seconds. Never open with a bare heading and a horizontal rule.
- **Conclusion first, evidence under it.** State what is true, then why — never a paragraph of reasoning that arrives at its point in the last sentence. This is what makes a doc skimmable by a human and retrievable in fragments by a model.
- **One claim per paragraph, and keep a paragraph under ~700 characters.** Past that, split it or demote the detail to sub-bullets. A numbered step in a how-to is a *step*: if it needs three caveats, they are sub-bullets under it, not more sentences inside it.
- **Tables scan; prose explains.** A table cell that needs a paragraph is content in the wrong container — put the sentence in the cell and the paragraph below the table.
- **One name for one thing.** `{{PROJECT_NAME}}` everywhere, never an older or longer name the project has outgrown. Cross-references use one form (`ADR-0007`, not `ADR 0007`). A concept keeps the same word in every doc that mentions it — a synonym is a retrieval miss for a model and a re-read for a human.
- **A list lives in exactly one place.** Where a full list exists (an ADR index, a command reference), other docs link to it and may quote a handful with a stated reason for the selection. Two copies of a list is two things to update, and one of them will be stale.

## Keep documentation in sync with code

Documentation is a first-class deliverable. **Before you commit any change, check whether any existing doc becomes wrong, incomplete, or misleading because of it. If yes, update the doc in the same commit.** Do not defer doc updates to "later" — drift compounds, and a doc that lies is worse than a doc that's missing.

### Change → required doc mapping

| Change                                                             | Must update                                                                                                                          |
| ------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------ |
| New user command, argument, response format, or UI surface         | the user guide (`README.md`, or `docs/guide.md` once split) and its twins; the front page when its bullets or install steps change  |
| New env var, config option, or changed default                     | `.env.example` (or equivalent config schema) + the user guide and its twins; also `docs/guides/{{deploy_or_publish_guide}}.md` if it affects shipping |
| New component, changed data flow, new integration                  | `docs/architecture/architecture.md`                                                                                                  |
| Non-obvious design decision                                        | new ADR in `docs/adr/` (Status / Context / Decision / Consequences)                                                                  |
| Any user-visible change                                            | `CHANGELOG.md` entry under Added / Changed / Fixed / Removed                                                                         |
| New capability or extension point                                  | `docs/guides/extension_guide.md`                                                                                                     |
| New/changed UI component or pattern (if the project has a surface) | the design-language reference (e.g. `docs/guides/design_language.md`)                                                                |
| Visible change to what the front page's demo shows                 | the demo, captured again                                                                                                             |
| New or removed user journey, or a major flow change                | `docs/product/vision.md` § CUJs                                                                                                      |
| Process / workflow change                                          | `CONTRIBUTING.md`; also `docs/guides/project_kickstart.md` if the pattern is reusable across projects                                |

If a row points at a doc the project doesn't have yet (extension guide, design reference), surface the gap — creating the file is a maintainer decision, not an automatic step.

### Task-closing checklist

Before marking an issue, PR, or user-requested task as complete, verify:

1. Every doc touched by the change is up to date.
2. `CHANGELOG.md` has an entry for any user-visible change.
3. New env vars, commands, config options, labels, or workflows are documented where an operator would look for them.
4. Non-obvious decisions have an ADR, and the ADR is referenced from the code or doc where the decision surfaces.
5. If the change introduces a reusable pattern (not just local code), the reusable pattern is reflected in the relevant guide (`project_kickstart.md`, `extension_guide.md`, deploy/publish guide).

Only after this checklist passes should you report the task as done.

## Issues and pull requests

- New issues use the templates in `.github/ISSUE_TEMPLATE/`:
  * `bug.md` for defects and regressions
  * `task.md` for features, refactors, docs, infra
- Technical-details sections use file-path checkboxes with `[NEW]` / `[MODIFY]` / `[DELETE]` markers — the marker carries the change type, the checkbox carries progress. Tick the box when the path lands; never edit prose to track progress.
- After creating an issue, apply labels from all three axes: `type/…`, `area/…`, `priority/…`. Add it to the milestone if scoped, and add it to the project board with Status = **Backlog** (do not leave it in "No Status" — that hides it from the board views).
- **Reflect work state on the project board.** The board has four Status columns — `Backlog → Ready → In Progress → Done`. `Backlog → Ready` is the maintainer's triage call; the other transitions are yours. When you start working on an issue, move it to **In Progress** as the first step — before reading code, before planning, before writing anything. When the work lands, move it to **Done** as part of the same close-out flow that closes the issue.
- **Position to top on every board write.** Projects v2 has no Created-date sort, so column order is raw position. After every `gh project item-add` and after every status change, call `updateProjectV2ItemPosition` with `afterId: null` so the just-touched item lands at the top of its new column.
- PRs use `.github/pull_request_template.md`. Every PR has a Summary, a linked issue (`Closes #N`), and a concrete Verification section.
- Reference issues in commit bodies and PR descriptions. Do not invent issue numbers — confirm they exist with `gh issue view <N>` first.
- **Dependabot PRs:** treat as normal PRs — read the bump's changelog/release notes before merging. Patch/minor bumps may use the auto-merge flow once CI is green; major bumps always get a manual review.

## Secrets and security

- Never commit `.env`, credentials, API keys, signing certificates, provisioning profiles, or any file matching common secret patterns. A `gitleaks` (or equivalent) pre-commit hook should catch most of this — do not disable it.
- Never log decrypted secrets, auth tokens, or payment data. Log identifiers (`provider`, `user_id`, `key_fingerprint`), not values.
- Never print secret values into the conversation, command output, or debug dumps — not even to diagnose. Reference keys by name (`API_TOKEN is set/empty`), never by value.
- Document secret rotation in `docs/guides/{{deploy_or_publish_guide}}.md` for every long-lived credential. When you're being paged at 2am you do not want to improvise.
- All new CI/CD workflows declare least-privilege `permissions:` blocks, not inherit repo defaults.

[web-service] Run the production container as a non-root user. Reverse-proxy and TLS termination live outside the application image.
[desktop] Code-sign release artifacts; document the signing certificate location and rotation procedure.
[mobile] Provisioning profiles and signing certificates live in {{e.g., Fastlane Match, EAS, App Store Connect}}, never in the repo.
[cli] Tarballs / archives published to package registries are signed or checksummed; document how to verify.
[library] Published packages use the registry's trusted-publisher / OIDC flow when available, not long-lived tokens.

## Executing actions

- Carefully consider blast radius before destructive actions. Freely: edit files, run tests locally, create feature branches. Confirm first: `git reset --hard`, `git push --force`, force-pushing, deleting branches with unmerged work, dropping {{database/state-store}} tables, amending published commits, pushing to shared remotes, publishing to a registry, uploading a release artifact. (Deleting a just-merged feature branch is routine close-out, not a confirm item.)
- Tool approval in one scope does not extend to other scopes (see the *Task workflow* and *Release workflow* sections for the per-workflow gates).
- If you hit unfamiliar files, branches, or config, investigate before deleting — it may be the user's in-progress work.
- When running into a failing check, diagnose the root cause. Do not bypass with `--no-verify`, test-skip markers, lint-suppress comments, or similar. Ask the user if you're stuck.

## When editing code

- **Simplicity First.** When you touch a load-bearing abstraction (decorator, cache, ContextVar / thread-local, registry, factory, protocol, DI container), ask whether the assumption that forced its shape still holds. If the shape outlived its justification, surface the simplification *before* layering on another patch; coupled residue (e.g. a cache + decorator that only make sense together) lands as one change, not two. Full stance and worked example: [docs/guides/codebase_review.md § Stance: Simplicity First](docs/guides/codebase_review.md).
- **Stay in scope.** The diff for a task contains only what that task needs. No drive-by refactors, renames, dependency bumps, or reformatting of untouched code — surface those as suggestions instead of bundling them in.
- Prefer editing existing files over creating new ones.
- **If the project ships a user-facing surface, conform to its design language.** Any UI edit — web, desktop, or TUI, and a CLI's flag/output conventions — reuses the documented components, data rendering, and UX patterns in the design-language reference (e.g. `docs/guides/design_language.md`); don't introduce a parallel variant. Applies to every UI edit, not just new screens. Where the project keeps a design file per screen, **one screen, one design file**: a change to a screen lands inside its file rather than beside it, and the pairing that holds that lives in the design-language reference. (Drop this rule for a headless service or library. See the *User-facing surface foundation* section in `docs/guides/project_kickstart.md`.)
- Don't add comments that explain **what** — the code shows that. Only add comments when the **why** is non-obvious (a hidden constraint, a subtle invariant, a workaround for an external bug).
- **Comments have a length budget: {{COMMENT_BLOCK_BUDGET}} lines for a comment block, {{DOC_COMMENT_BUDGET}} for a doc comment, {{FILE_HEADER_BUDGET}} for a file/module header.** The rule above bounds the *kind* of comment and never its *volume*, so prose that is entirely legitimate "why" still grows without limit — one paragraph appended per issue that touches the file, until a reader cannot find the code between them. Past the budget the fix is to cut it or move it out, never to keep appending; needing more *is the signal* that the content belongs in an ADR. **The budget counts prose only** — a structured parameter/return block (`Args:`, `@param`, `# Arguments`, `<param>`) is API contract, not narrative, and does not count against it. **Derive the three numbers from your own codebase rather than copying anyone's** — the procedure is in `docs/guides/codebase_review.md` § Phase 2 — and re-derive them if the codebase changes shape. A cap set by taste flags the wrong files in both directions.
- **Write for the next editor, not for the reviewer of the change that added it.** A comment answers one question: *what breaks if I change this?* Keep the surviving constraint; drop how it was found. Measurement runs, variants tried, benchmark numbers, cases that regressed, dates, and issue-by-issue narrative are discovery, not constraint — they feel like documentation but are review-time persuasion, and their home is the PR description, read once by the right person and then archived. **Present tense is the test:** a sentence written in the past ("used to", "was tried", "regressed", "turned out to be") is history, and history belongs in version control, the issue tracker, or the ADR. Never in source.
- **Displaced rationale has a home — move it, don't delete it.** A design decision → a numbered ADR in `docs/adr/`. Domain rationale → whichever doc owns that domain in the change → doc mapping above. Experiment logs, benchmarks, and measurement history → the issue that produced them. Then cite the destination from the code in one line (`See ADR-0012.`) instead of restating it.
- **Prefer making the code say it.** A named constant, an extracted function, or a test with a sentence for a name beats a paragraph — and unlike a comment, it fails when it stops being true. Reach for prose only once those are exhausted.
- **Tracker references in comments / doc comments:** a reference (`#123`, `PROJ-456`) is allowed only when it anchors load-bearing rationale (an ADR, a specific external bug, a non-obvious constraint). Test by deleting it — if the remaining prose still answers "why", keep the ref; if it collapses to a WHAT-comment, inline the rationale instead. Bare `added in #123` / `fixes #123` tombstones are banned, in test doc comments too. A comment carrying three or more references is a changelog: move it per the rule above.
- No backwards-compatibility shims, dead-code tombstones, or `# removed X` comments. Delete cleanly.
- No feature flags for work that can simply ship.
- No half-finished implementations. If scope is too big, split the PR and say so; don't leave stubs.
- Do not add error handling for impossible scenarios. Validate at system boundaries (user input, external APIs, deserialization), trust internal invariants.
- Match existing patterns in the file you're editing. If the existing pattern is wrong, fix it in a separate commit with a clear message.

## What NOT to do

- Do not push to `{{PRODUCTION_BRANCH}}`.
- Do not duplicate the version string.
- Do not `--no-verify`.
- Do not leave a merged feature branch behind — delete it, remote and local, in the same close-out.
- Do not commit `.env`, secrets, signing material, or generated credentials.
- Do not recreate `roadmap.md` or any other ad-hoc tracking file.
- Do not create documentation files unprompted.
- Do not let a comment outgrow its budget — cut it, or move it to the ADR / issue that owns it.
- Do not introduce competing lint/format/type tools when one already covers the role.
- Do not use emojis in code or docs unless the user explicitly asks.
- Do not mock the {{primary-side-effect target}} in integration tests.
- Do not add `print()` / `console.log` / `println!` for debugging — use the project's structured logger.
- Do not add dependencies casually. New dependencies need a reason that justifies the supply-chain surface.
- Do not improvise a review process — a task's review round follows `docs/guides/task_review.md`, comprehensive code reviews follow `docs/guides/codebase_review.md`, UI/UX reviews follow `docs/guides/ux_review.md`.

## When in doubt

1. Read the relevant section of `CONTRIBUTING.md` or `docs/guides/project_kickstart.md`.
2. Check if an existing file already solves the problem. Reuse before creating.
3. Mid-task fork: internal and easily reversible → decide, note it in the hand-back, continue. User-visible, hard to undo, or scope-changing → stop and ask.
4. Otherwise ask the user. A 30-second clarification beats a 30-minute undo.
````

---

## Placeholder reference

Quick reference for each `{{token}}` used above.

| Token | Example values | Notes |
|---|---|---|
| `{{PROJECT_NAME}}` | "Acme Reader", "Widget CLI" | Human-readable name |
| `{{OWNER}}/{{REPO}}` | `acme/reader` | GitHub slug |
| `{{SETUP_CMD}}` / `{{TEST_CMD}}` / `{{LINT_CMD}}` / `{{RUN_CMD}}` | `uv sync` / `pytest --cov` / `ruff check . && ruff format .` / `docker compose up` | The canonical invocations CI and hooks run |
| `{{LANGUAGE}}` | "Python 3.13", "Rust 2024", "TypeScript / Node 22", "Swift 5.10" | Primary language and version |
| `{{PROJECT_TYPE}}` | "chat bot", "macOS menu-bar app", "iOS app", "CLI tool", "library" | One-noun shape |
| `{{ONE_LINE_DESCRIPTION}}` | "REST API with SQLite storage, Docker deployment" | Sub-30 words |
| `{{PRODUCTION_BRANCH}}` | `master`, `main`, `release` | Auto-deploy or publish target |
| `{{INTEGRATION_BRANCH}}` | `develop`, `main`, `next` | Default branch, where work lands |
| `{{ENTRY_DIR}}` / `{{LOGIC_DIR}}` | `apps/` + `packages/`; `cmd/` + `internal/`; `src/bin/` + `src/lib/` | Transport-vs-logic split |
| `{{EXT}}` | `py`, `ts`, `tsx`, `go`, `rs`, `swift`, `kt` | Primary source extension |
| `{{VERSION_FILE}}` | `packages/core/__init__.py`, `package.json`, `Cargo.toml`, `pubspec.yaml` | Single source of truth |
| `{{TEST_RUNNER}}` | `pytest`, `vitest`, `jest`, `go test`, `cargo test`, `xctest`, `flutter test` | One command, one runner |
| `{{COVERAGE_FLOOR}}` | `55`, `70`, `80` | Pick something you can defend |
| `{{COMMENT_BLOCK_BUDGET}}` / `{{DOC_COMMENT_BUDGET}}` / `{{FILE_HEADER_BUDGET}}` | `8` / `20` / `25` | **Derive, don't copy** — `codebase_review.md` § Phase 2 has the procedure. The doc-comment number is the larger of the first two: a doc comment is also an API contract |
| `{{LINT_TOOL}}` | `ruff`, `eslint` + `prettier`, `golangci-lint`, `clippy` + `rustfmt`, `swiftlint` | One per role |
| `{{PKG_MANIFEST}}` | `pyproject.toml`, `package.json`, `go.mod`, `Cargo.toml` | Where tool config lives |
| `{{deploy_or_publish_guide}}` | `deployment`, `publishing`, `release`, `distribution` | Filename of the shipping guide |
| `{{primary-side-effect target}}` | "database", "filesystem", "device camera", "network calls to the API" | What integration tests must hit for real |
| `{{database/state-store}}` | "database", "keychain", "user defaults", "Core Data store" | Used in the "blast radius" warning |

---

## Project-type adaptation notes

Where the template's defaults assume a long-running service, here's how each project type usually diverges. Keep these as a checklist when filling the template. Any type that ships a user-facing surface (web, desktop, mobile, TUI, or a CLI's flag/output conventions) should also stand up a design-language reference early — see `docs/guides/project_kickstart.md` § User-facing surface foundation.

### Web service / bot / API

- Default template fits.
- `/health` version probe is mandatory; smoke-check it post-deploy.
- Production branch auto-deploys to a runtime host.

### Desktop app (Electron / Tauri / Qt / native)

- Replace "deploy to VPS" with "publish signed installer to GitHub Releases".
- Drop `/health` for an About dialog showing the version.
- Add a `signing` section under Secrets covering certificate storage and rotation.
- Integration tests hit a real filesystem; UI tests are their own category.

### Mobile app (iOS / Android / React Native / Flutter)

- Replace "deploy" with "upload to TestFlight / Play Console internal track".
- Use Fastlane lanes (or equivalent) for the release pipeline; document the lanes in the publish guide.
- Signing certificates and provisioning profiles never enter the repo — use Match / EAS / App Store Connect.
- The "version probe" is the app's settings screen.

### CLI tool

- Replace "deploy" with "publish to PyPI / npm / crates.io / Homebrew tap".
- `--version` flag is mandatory and reads from the single source of truth.
- Smoke check: install the published artifact in a clean environment and run `--version`.

### Library

- No transport layer — drop the `{{ENTRY_DIR}}` row from Code layout and keep only `{{LOGIC_DIR}}`.
- Replace "release" with "publish to registry".
- Cut the "Executing actions" line about deploys; keep the publish gate.
- Coverage floor is usually higher (libraries get reused; bugs hurt more callers).

### Monorepo with multiple project types

- Either split AGENTS.md per workspace (each workspace gets its own root) or write a single AGENTS.md with explicit per-workspace sections. Do not mix the rules into one undifferentiated list.

---

## When to update the template itself

If you've kickstarted two or more projects with this template and noticed the same paragraph getting hand-edited the same way each time, that paragraph is wrong and the template should be updated. Fix it in the gist, never in a mirror, which the next re-sync overwrites: propose the fix, make it on the maintainer's go-ahead, then re-sync every project's mirrors — improvements compound across every future project. Conversely, if a section keeps getting deleted on copy, consider whether it belongs in `project_kickstart.md` instead, since the template should be terser than the playbook.
