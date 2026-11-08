# Project Kickstart Playbook

A language-agnostic, tech-agnostic recipe for standing up a new project that stays organized from day zero through its first few releases. Infrastructure opinions assume **Git + GitHub + Docker**; everything else (language, framework, test runner, package manager) is a drop-in choice.

Everything here is extracted from deployed production projects, not theory. Keep one of your own as the living reference (see *Keep a living reference* at the end).

Upstream of this playbook sits `idea_gauntlet.md` (mirrored alongside it): the gauntlet decides *whether* a project gets built, and its filled instance — `IDEA.md` at the folder root — is this playbook's input. This playbook starts once the gauntlet's answer is yes, and hands day-to-day operation to `AGENTS.md` and the issue board.

---

## Philosophy

Three principles drive every decision below. When in doubt, return to these.

1. **Single source of truth.** Every fact — version number, deployed SHA, release notes, tracked work, runtime config — lives in exactly one place. Other places *read* it; they never restate it. A version string duplicated across four files is three bugs waiting to happen.
2. **Local mirrors CI.** The pre-push hook runs the same commands CI runs. "Passes on my machine" stops being a failure mode. If a check only runs in CI, contributors discover failures late and react by retrying, not fixing.
3. **Documentation layered by audience.** A user wants to install the thing. A contributor wants to change the thing. A future-you wants to remember *why*. Each audience gets its own file in its own place. Mixing them produces a README no one finishes reading.

Everything else — branching, release process, CI layout, folder structure — is just these three principles applied to a specific problem.

---

## Day-zero scaffold

Minimum viable directory structure. Names are conventions, not requirements — pick what fits your ecosystem.

```
project/
├── .github/
│   ├── workflows/              # CI, deploy, security scanning
│   ├── ISSUE_TEMPLATE/         # bug.md, task.md, config.yml
│   ├── pull_request_template.md
│   ├── dependabot.yml
│   └── SECURITY.md
├── src/ (or apps/ + packages/ — see *Split transport from logic*)
├── tests/                      # mirrors src/ layout
├── docs/
│   ├── README.md               # docs index
│   ├── product/                # vision incl. CUJs; idea.md (frozen origins)
│   ├── architecture/           # the "how" — components, data flow, design decisions
│   ├── guides/                 # task-oriented howtos (deploy, extend, onboard)
│   └── adr/                    # architecture decision records
├── scripts/                    # setup, build, backup, one-off utilities
├── .env.example                # every env var the app reads, with comments
├── .gitignore
├── .pre-commit-config.yaml     # or equivalent (husky, lefthook, …)
├── Dockerfile
├── docker-compose.yml
├── README.md                   # entry point — what, install, use
├── CHANGELOG.md                # release history
├── CONTRIBUTING.md             # how to work in this repo
├── LICENSE                     # or a proprietary notice
└── <pkg manifest>              # pyproject.toml, package.json, go.mod, Cargo.toml, …
```

### Day-zero guardrails

Before your first commit lands, wire up a local hook config that runs the same checks as CI. The principle (covered in depth in *Pre-commit: local == CI* later in this doc) is that identical commands run at every layer — editor, local git hook, CI — so a check passing locally means it passes CI. Two stages, distinct loops:

- **Commit-stage (fast loop, changed files only):** linter, formatter, trailing-whitespace / config-file (YAML / TOML / JSON) sanity checks, large-file guard, **secret scan** (e.g. gitleaks). Should add no perceivable latency to `git commit`.
- **Push-stage (CI-parity gate, full tree):** linter and formatter re-run on the **entire source tree** (not just changed files), the type checker if the project uses one, the **full test suite**, and the **same coverage threshold CI enforces**. If a project skips the coverage threshold in the hook, the floor isn't actually enforced — CI catches it minutes later, after the push has already landed and the assistant has moved on.

The exact tools depend on language (lint/format/test-runner names differ across Python, Go, Rust, TypeScript, Swift, etc.), but the *category list above* is constant. Get all of it in place before the first feature commit — retrofitting hooks after dozens of commits means a noisy "fix everything" pass, which discourages the cleanup that's the whole point.

### Split transport from logic

Whatever your language, separate **entry points** from **reusable logic**.

- Entry points (`apps/`, `cmd/`, `bin/`) know about HTTP, chat-platform APIs, CLI args, cron triggers. They're thin wrappers.
- Logic (`packages/`, `lib/`, `internal/`) is pure — no framework imports. You can test it without spinning up the transport, and you can add a new transport without touching the logic.

The smell test: **can you add a second transport — another chat platform, a CLI, an HTTP API — without changing anything in the logic layer?** If yes, the split is right.

### Mirror `tests/` on `src/`

If `src/billing/invoices.py` exists, `tests/billing/test_invoices.py` exists. No clever grouping, no `tests/unit/` vs `tests/integration/` top-level split until you actually need it. One-to-one mirroring means you can find the test for any file in one jump.

---

## Source-of-truth files

Every new project should have these on day zero. Each one has a single job; resist the urge to merge them.

| File                                                             | Purpose                                                                                                                                              | Single source of truth for  |
| ---------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------- |
| `README.md`                                                      | Entry point for anyone landing on the repo. What the project is, how to install, how to use, where to find more.                                     | Project identity            |
| `CHANGELOG.md`                                                   | What changed between releases, grouped Added/Changed/Fixed/Removed.                                                                                  | Release history             |
| `CONTRIBUTING.md`                                                | How to work in this repo: branching, local setup, release process, commit conventions.                                                               | Development workflow        |
| `LICENSE`                                                        | Legal terms of use. MIT/Apache-2.0/BSD for OSS, or an "All rights reserved" notice for closed-source.                                                | Legal                       |
| `.env.example`                                                   | Every environment variable the app reads, grouped Required/Optional, with inline comments on where to obtain each value. Never commit a real `.env`. | Runtime configuration shape |
| `.gitignore`                                                     | What never enters git: secrets, build artifacts, venvs, caches, IDE config.                                                                          | Repo hygiene                |
| Package manifest (`pyproject.toml`, `package.json`, `go.mod`, …) | Dependencies, tooling config, linter/formatter rules, **the version number**.                                                                        | Build, deps, version        |
| `Dockerfile`                                                     | How to build a runnable container image.                                                                                                             | Build recipe                |
| `docker-compose.yml`                                             | How the container runs in context (volumes, network, sidecars, resource limits).                                                                     | Runtime topology            |
| `.pre-commit-config.yaml`                                        | Local hooks mirroring CI.                                                                                                                            | Local quality gate          |

### The version number lives in exactly one file

Pick one file as the version source of truth. Every other place (CHANGELOG, Docker image tags, `/health` endpoint, release workflow) reads from it. Examples:

- Python: `packages/core/__init__.py` → `__version__ = "1.2.3"`
- Node: `package.json` → `"version": "1.2.3"`
- Go: a `const Version = "1.2.3"` in a dedicated `internal/version` package, or a `-ldflags` injected value at build time
- Rust: `Cargo.toml` → `version = "1.2.3"`

Your CI reads from it. Your runtime `/health` endpoint reads from it. When you bump the version, you bump *one file*, not four.

### CHANGELOG style: one claim per entry, user-visible only

`CHANGELOG.md` is for humans scanning what changed between versions, not a commit log or a release-notes draft. Keep it scannable or it becomes noise nobody reads.

- **One claim per entry, at most 120 characters before the `(#N)`.** "One line" is a format rule and constrains nothing, so give it a number. A semicolon or dash welding a second claim means two entries sharing the issue ref, not a longer one — split, never trim until it fits. If it needs a paragraph, the paragraph belongs in the linked issue/PR, not the CHANGELOG.
- **Derive the cap from your own history rather than adopting 120.** Measure entry length excluding the trailing `(#N)`, and split the corpus on whether an entry welds a second clause. In the project this was derived from, 80% of 686 shipped entries carried no `;` or dash and sat at a median of 104 characters; the welded fifth ran 30-45 longer, and every entry that read as "too long" was in it. The cap is a detector for the welding — set it just above where your single-claim population already lives.
- **User/operator-visible only.** New command, new env var, changed default, observable behaviour, user-visible fix — yes. Internal refactors, renames, new log fields nobody consumes — no. The diff and the commit log already cover those.
- **Reference the issue/PR** (`- Fix X (#42)`). Details, rationale, and migration prose live there.
- **Group under Added / Changed / Fixed / Removed once per version.** Collapse adjacent `### Changed` blocks — one per section per release.
- **No rationale prose.** "In production this lands at…" and "we did this because…" belong in the commit message, PR description, or deployment guide.

The payoff: the `[Unreleased]` section stays short enough that a maintainer can read it in ten seconds when cutting a release, and release notes on GitHub are something users actually skim. Expect the entry count to rise when you adopt the cap — splitting welded entries trades fewer, denser lines for more lines that each scan, which is the whole point.

### Expose a version probe

For long-running services, expose `GET /health` that returns JSON including the running version. This turns "did my deploy actually land?" from a guess into a single `curl`. One of the cheapest, highest-leverage habits you can adopt.

```
{ "status": "ok", "version": "1.2.3", "uptime_seconds": 12847 }
```

For non-service projects, the equivalent is a `--version` flag (CLI), an About dialog (desktop), or a settings screen entry (mobile) — same idea, different surface. Whatever your shipping unit is, give the user a way to ask "what version is this?" and have it read from the single source of truth.

---

## Branching model

Two branches are mandatory. Feature branches are optional and earn their keep only in specific situations.

- **`master` (or `main`)** — production. Every commit here is deployed. Nobody pushes directly; merges only.
- **`develop`** — integration branch. Default branch in GitHub. **The pre-push hook runs the same lint + test gate as CI before any push lands**, so it is safe to commit directly here; CI itself re-runs on PRs as a backstop.
- **feature branches** — optional, branched from `develop`, named `feat/…`, `fix/…`, `chore/…`, `docs/…`, `refactor/…`. Used only when the change warrants them (see below).
- **Commit messages** — [Conventional Commits](https://www.conventionalcommits.org/) (`feat` / `fix` / `chore` / `docs` / `refactor` / `test` / `perf`), imperative subject under 72 characters, body for the *why* when it isn't obvious. `gh pr create --fill` and squash-merge titles inherit their quality from these.

### Pick the flow that matches the situation

Your default flow depends on team size and the nature of the work. For a **solo developer (especially one working through AI assistants)**, most work should land on `develop` directly — the PR ceremony is pure overhead when there's nobody else to review and the pre-push hook already runs the same lint + test + coverage-floor gate as CI. For a **team or open-source project**, feature branches become the default because the PR *is* the review surface.

Either way, feature branches remain the right call for specific kinds of work, and automating them brings the cost down to near zero.

### Solo + AI-assisted flow

For everyday work — small features, fixes, docs, chores — commit directly to `develop`:

```
git checkout develop
git pull
# ... work, commit ...
git push
```

The pre-push stage of the local hook config runs the identical lint, format-check, and test commands (with the coverage floor) that CI runs, so every push has already cleared the CI gate locally. CI itself re-runs on PRs as a backstop. Releases still go through a `develop → master` PR, so the auto-deploy gate on `master` stays intact. Broken commits on `develop` are a fix-forward or `git revert` problem, not a crisis — nothing is deployed.

The hook is the mechanical gate; the read is `task_review.md` (mirrored alongside this playbook). Before a task is handed back, the developer session hands a packet to a reviewer agent with no author context — a subagent it launches, or a fresh session where the tool has none — and renders a hand-back brief for the maintainer from its return packet: two plain-text packets between the agents, and the same round whether the work rides a PR or lands on `develop` directly. The maintainer reads the brief and nothing else of the round, then the diff, with what a gate cannot check (scope, decisions, invariants, prose, docs) already read by a reviewer the author could not steer.

### Feature-branch flow (escape hatch for riskier work)

Use a branch when the change is:

- **Multi-commit** and should land atomically on `develop`
- **Risky or invasive** (schema migrations, auth, payments, cross-cutting refactors)
- **Spanning multiple sessions** — save progress on a branch without polluting `develop`
- Something you want to **review as a diff** before merging
- Something that benefits from a **PR description as a mini-changelog**

When those apply, automate the flow end-to-end. The overhead is ~30 seconds per feature and AI assistants can drive all four commands:

```
git checkout -b feat/my-thing
# ... work, commit ...
git push -u origin feat/my-thing
gh pr create --fill --base develop
gh pr merge --squash --auto --delete-branch
```

- `gh pr create --fill` uses your commit messages as the PR title and body — so write good commit messages.
- `gh pr merge --squash --auto --delete-branch` waits for CI to pass, squashes the branch into one commit on `develop`, and deletes the remote branch. You can close the terminal; the merge completes in the background.
- `--delete-branch` removes only the **remote** copy. Once the merge lands, finish the job locally — stale merged branches are how `git branch` output turns into noise:

  ```
  git checkout develop && git pull
  git branch -d feat/my-thing
  git fetch --prune
  ```

  A branch listed by `git branch` is either active work or it shouldn't exist.

**One-time setup:** enable auto-merge in the repo at `Settings → General → Pull Requests → Allow auto-merge`. `gh pr merge --auto` fails without it.

### Team / open-source flow

Once you have more than one contributor, invert the defaults: **feature branches become the default** and direct commits to `develop` stop. The PR is the review surface; you don't want two people stomping on each other in a shared branch. Route all normal work through branches so the PR-triggered CI run is the single gate of record; contributors who can't be trusted to keep pre-push hooks installed should not be direct-committing to `develop` anyway.

### Why not trunk-based?

Trunk-based (single `main` branch, feature flags for in-progress work) is excellent once you have:

- A test suite you trust to catch regressions immediately.
- Feature flag infrastructure.
- Observability to catch production issues fast.

Until then, `develop` as a staging buffer costs nothing and catches mistakes before they deploy.

---

## Release process

Codify release as a numbered checklist in `CONTRIBUTING.md`. Not "the maintainer knows how" — a literal numbered list anyone on the team could execute.

Use [Semantic Versioning](https://semver.org): `MAJOR.MINOR.PATCH`. Each `MAJOR.MINOR` maps to a milestone; patch releases reuse the same milestone.

### Planning

1. **Pick the milestone.** Open `v<MAJOR.MINOR>` if it doesn't exist. Don't pre-create a dozen — create them as you commit to scope.
2. **Triage.** Move issues from `Backlog` to `Ready` and assign them to the milestone. Reshuffling between milestones is free until release work starts.
3. **Work the issues.** Branch from `develop`, reference the issue in the PR (`Closes #N`), merge to `develop`, delete the merged branch — remote and local.
4. **Confirm the milestone is ready.** All issues closed or moved out. `gh issue list --milestone v1.2 --state open` should be empty.

### Cutting the release

5. **Bump the version** in the single source of truth file.
6. **Update `CHANGELOG.md`** with the new version and date, grouped Added / Changed / Fixed / Removed, referencing issue numbers.
7. **Open release PR `develop` → `master`** with title `Release v<version>` and body containing the new CHANGELOG section.
8. **Merge after CI green.** The merge to `master` triggers the deploy workflow.
9. **Tag the release** and push the tag.
10. **Create a GitHub Release** from the tag (gh CLI or UI).
11. **Fast-forward `develop`** onto the release merge commit (`git merge --ff-only master`) so the branches don't diverge.
12. **Close the milestone.**
13. **Verify deployment** — curl `/health` and confirm it reports the new version.

**AI assistant checkpoint.** Steps 8 and 9 are irreversible and deploy to production. If an AI agent is executing this release, it should draft the CHANGELOG, open the release PR, and then **stop and require explicit user approval** before running the merge or pushing the tag. Confirm each of the two steps individually — a prior "do the release" does not extend to them. Codify this rule in your agent-instructions file (see the *AI agent instructions* section below) so assistants apply it consistently.

### Hotfix variant

For urgent fixes: branch from `master`, fix, PR back to `master`, bump patch, update CHANGELOG, merge (auto-deploy), tag, release. Then forward-merge `master → develop` so the patch isn't lost on the next regular release, and delete the hotfix branch — remote and local.

---

## CI/CD recipe

Three workflows. Each has one job.

### 1. `ci.yml` — run on every PR and on `develop`

Triggered by `pull_request` plus `push` to `develop`. Feature-branch pushes already fire the PR-triggered run (no duplicates); the `develop` push trigger is the backstop for the solo direct-commit flow — without it nothing independently re-checks `develop` until the release PR, and a machine with stale or missing hooks goes unnoticed for weeks.

```
name: CI
on:
  pull_request:
  push:
    branches: [develop]
  workflow_call:     # lets deploy.yml reuse this
concurrency:
  group: ${{ github.workflow }}-${{ github.head_ref || github.ref_name }}
  cancel-in-progress: true
```

Jobs: install deps, lint, format check, type check (if applicable), tests with a coverage floor. Nothing else belongs here.

### 2. `deploy.yml` — run on push to `master`

Triggered by `push` to `master`. Reuses `ci.yml` via `workflow_call`, then builds and deploys.

```
name: Deploy
on:
  push:
    branches: [master]
jobs:
  ci:
    uses: ./.github/workflows/ci.yml
  build-and-deploy:
    needs: ci
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write
    steps:
      - uses: actions/checkout@v4
      - name: Read version
        id: version
        run: echo "version=$(<command to extract version>)" >> $GITHUB_OUTPUT
      - name: Login to registry
        uses: docker/login-action@v3
        with: { registry: ghcr.io, username: ${{ github.actor }}, password: ${{ secrets.GITHUB_TOKEN }} }
      - name: Build and push
        uses: docker/build-push-action@v6
        with:
          context: .
          push: true
          tags: |
            ghcr.io/<owner>/<repo>:latest
            ghcr.io/<owner>/<repo>:${{ github.sha }}
            ghcr.io/<owner>/<repo>:v${{ steps.version.outputs.version }}
      - name: Deploy
        # SSH into VPS, `docker compose pull && up -d`, wait for healthy, verify /health version
```

Three image tags on every deploy: `latest`, the git SHA, and `v<version>`. SHA tags give you exact-commit rollback; version tags give you human-readable rollback. Document the actual rollback procedure in the deploy guide and run it once before you need it — pin the previous tag (`IMAGE_TAG=<sha> docker compose up -d`) or revert the merge on `master` and let the pipeline redeploy.

**Smoke check is not optional.** After `docker compose up -d`, the workflow should:

1. Poll the container's health status until `healthy` or timeout.
2. Call `/health` and assert the reported version matches the expected version.
3. Fail the workflow (and the deploy) if either check fails.

A deploy that silently half-landed is worse than a deploy that obviously failed.

### 3. `codeql.yml` — security scanning (public repos only)

CodeQL catches the "I accidentally introduced a SQL injection" class of bugs without you thinking about it. It's free for public repos; on private repos it requires the paid GitHub Code Security add-on (the former GHAS), sold only on organization Team/Enterprise plans. **On private personal repos it's not available** — the workflow will run but SARIF upload fails. Skip this file on private personal projects; add it the day the repo goes public or moves to an org with Code Security.

---

## Pre-commit: local == CI

Pre-commit hooks are wasted potential if they run different commands than CI. Make them identical.

Two stages:

| Stage                          | What runs                                                                                          | Why                                                                               |
| ------------------------------ | -------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------- |
| `pre-commit` (on `git commit`) | Fast things: lint, format, trailing whitespace, YAML check, large-file check, **secret scanning**. | Keeps the commit loop snappy. Catches obvious problems before they enter history. |
| `pre-push` (on `git push`)     | Slow things: **full test suite with the same coverage floor as CI**.                               | CI should be a backstop, not the first time tests run.                            |

**Keep the push gate fast.** Every push pays its wall time. Run the suite across worker processes (pytest-xdist, Jest's workers), in CI too with the worker count sized to its runner, and hunt the costs every test pays: a template engine or schema rebuilt per test, a live network call a mock missed.

**Pin each tool once.** A hook manager that pins its own tool version — pre-commit's `rev:`, a lockfile under husky/lefthook — is a second pin beside whatever CI installs. Two pins drift on the next bump, and the failure reads as a broken CI rather than version skew: the commit hook reformats a file one way, CI's `--check` rejects it. A test asserting that the two agree does not survive Dependabot, which bumps each ecosystem in its own PR: every release of the tool arrives as two PRs that each fail the assertion.

**Let CI run the hooks.** CI calls the hook manager's own hooks — `pre-commit run <hook-id> --all-files` — and leaves the tool out of its own install, so the hook's pin is the only one and a bump lands as one PR (`pre-commit` is a Dependabot ecosystem). Keep `--all-files` in local runs too: without it, pre-commit stashes unstaged edits and checks only what is staged.

**Secret scanning** (e.g. [gitleaks](https://github.com/gitleaks/gitleaks)) on `pre-commit` is the cheapest credential-leak prevention you can buy. Your `.env` is gitignored, but accidents happen — a secret pasted into a doc example, a config file with a real token committed because you forgot `.gitignore`. A 200ms hook catches all of them.

---

## User-facing surface foundation

Applies to any surface a human drives — web, desktop GUI, terminal UI — and even a plain CLI, which still has a design language: command/flag naming, output modes, color and symbol scheme, error + exit-code shape. Skip only for a truly headless service. The principle is surface-agnostic:

**Set the foundation before you build screen 2.** Conventions cascade; screens don't. Deciding the tokens and the canonical patterns up front costs an afternoon and propagates for free; retrofitting consistency across a dozen screens later costs a dozen times more.

In order:

1. **Pick the framework + a component/widget vocabulary** so you inherit accessible, themeable primitives instead of hand-rolling them (record the choice in an ADR). Web → a CSS/component library; desktop → a widget toolkit (Qt, Tauri, …); TUI → Textual / Bubble Tea / ratatui; CLI → an arg parser + an output/formatting helper.
2. **Set the tokens once, centrally**, so a change cascades. What "tokens" are depends on the surface:
   - *Web / desktop / TUI:* two font families at most (one UI, one mono for ids/numerics), semantic color tokens only (no raw hex), light + dark from day one, one spacing rhythm — in a theme/config file.
   - *Plain CLI:* consistent verb/flag naming, a small fixed set of output modes (human · `--json` · `--quiet`), one color-and-symbol scheme, a uniform error + exit-code convention.
3. **Don't build a design system.** At a handful of screens and one maintainer, a token package / Storybook / style crate costs more than it returns. Treat your stylesheet (or output helpers) as the de-facto system and write a one-page **design-language reference**: the component/output inventory, how each kind of data is rendered (ids, timestamps, money, counts), and the one canonical pattern per job (destructive confirm, empty state, pagination/scrolling, status indicator). Revisit only when the screen count doubles or a second surface appears.
4. **Enforce it in the agent contract, not just the doc.** Add a rule to `AGENTS.md` (mirrored in `CLAUDE.md`) that *any* UI edit conforms to the reference — reuse what ships, don't introduce a parallel variant. A doc is advisory to a model; a first-read MUST-rule holds the line. The only hard guarantee is a lint, usually over-engineering at this size.
5. **Design new screens and restructures on a canvas** the assistant draws in the same session as the code (Claude Design; rich for web and desktop, thinner for TUI/CLI). The maintainer approves the drawing before code, and the build treats it as a strong draft, not a spec (reconcile invented data/backend assumptions, reuse your shipped primitives over its near-duplicates, drop embellishments).
   - A change you can state in one line goes straight to the code; one easier judged by eye is a design round like any other.
   - Where the canvas keeps a file per screen, **pair them in one table** — a row per shipped screen naming its design file and the sources that decide what it draws — and hold it with a test: every screen has a row, and no two rows claim the same screen. It costs a line at screen 2, and it is what stops a change to one screen becoming a second design file for it (`docs/guides/design_workflow.md` § *Round*).

The design language has a lifecycle, not just a document. Keep `docs/guides/design_workflow.md` (the *how* — found the foundation before screen 2, codify every design round back into it, evolve it on a trigger) alongside `docs/guides/design_language.md` (the *what* — the resolved tokens, components, and patterns). The same shape applies to a Textual/ratatui TUI or a Qt/Tauri desktop app.

Once shipped, the surface gets audited periodically against this foundation by two complementary passes: `docs/guides/ux_review.md` walks the journeys for friction and bloat, and `docs/guides/design_review.md` — the runnable pass `design_workflow.md` triggers — establishes or evolves the visual system itself when the surface has outgrown the foundation set here.

---

## Documentation layers

Each document answers a different question. If two docs are answering the same question, merge them. If one doc is answering three questions, split it.

| Layer            | File(s)                  | Answers                                                                                 |
| ---------------- | ------------------------ | --------------------------------------------------------------------------------------- |
| **Entry**        | `README.md`              | "What is this? How do I install it? How do I use it?"                                   |
| **Contribution** | `CONTRIBUTING.md`        | "How do I work in this repo?" — branching, setup, release, commit style                 |
| **History**      | `CHANGELOG.md`           | "What changed between versions?"                                                        |
| **Vision**       | `docs/product/vision.md` | "What problem does this solve? Who is it for? What does success look like?" — owns the CUJ inventory |
| **Architecture** | `docs/architecture/architecture.md` | "How does the system work?" — components, data flow, how configuration is wired. One file until it outgrows itself |
| **Guides**       | `docs/guides/*.md`       | "How do I do X?" — deploy, add a provider, extend a capability                          |
| **Decisions**    | `docs/adr/NNNN-title.md` | "Why did we do it this way?" — one ADR per significant choice, numbered, status-tracked |

### `vision.md` is seeded, not written

It is seeded from the graduated `IDEA.md` (gauntlet Phase 4) — which itself freezes as `docs/product/idea.md`, the never-edited origins artifact — and grows a **CUJ inventory** at the first UX review (`ux_review.md` § CUJs). One living file owns product intent end-to-end; the frozen one remembers where it came from. Don't add a separate requirements doc — that's `roadmap.md` with a different name; tracked work lives in issues, scope history lives in milestones and the CHANGELOG.

### ADRs deserve a special note

[Architecture Decision Records](https://adr.github.io/) are the single highest-leverage documentation habit. One file per significant decision, structured as:

```
# NNNN. Title

## Status
Proposed / Accepted / Superseded by NNNN / Deprecated

## Context
The forces at play. What constraint, pain point, or requirement triggered this?

## Decision
What did we actually decide?

## Consequences
What does this make easier? What does it make harder? What did we give up?
```

Write one when you make a non-obvious technical choice. Future-you (and any new contributor) will thank you. Suggested first ADRs to write: storage choice, deployment platform choice, auth model.

### `docs/README.md` as navigation hub

A short index file in `docs/` that tables the other docs with a one-line "what's in it" summary. Keeps the layering explicit — anyone landing in `docs/` can orient in ten seconds.

### Doc maintenance is part of Definition of Done

Documentation drift is the single most common way a well-organized project turns into an unreadable one. The cure is a rule, not discipline: **every code change that makes an existing doc wrong updates that doc in the same commit.** Deferred doc updates rot; a doc that lies is worse than a doc that's missing.

Write the mapping down where it's read on every change: `AGENTS.md`. The template (`agents_template.md`) ships the standard **change → required doc** table pre-filled — that copy is the operative one; extend it there, not here. The shape, compressed: new env var → `.env.example` + `README.md`; new component or changed data flow → `docs/architecture/architecture.md`; non-obvious decision → new ADR; any user-visible change → `CHANGELOG.md`; reusable process change → `CONTRIBUTING.md` and this playbook.

Before merging a PR or closing an issue, verify: relevant docs updated, CHANGELOG entry exists, new env vars / commands / config options are documented, non-obvious decisions have an ADR. If the task introduced a reusable *pattern* rather than local code, that pattern belongs in `project_kickstart.md` so future projects inherit it.

### AI agent instructions

If you use AI assistants to write code (Claude Code, Codex, Cursor, Gemini CLI, Copilot), codify the rules once in a file assistants will read. `AGENTS.md` has become the cross-vendor standard; the notable holdout is Claude Code:

| Tool                                                          | File it reads                                  |
| ------------------------------------------------------------- | ---------------------------------------------- |
| OpenAI Codex / Cursor / Windsurf / Gemini CLI / Copilot / most newer tools | `AGENTS.md` (cross-vendor standard) |
| Claude Code                                                   | `CLAUDE.md` (supports `@AGENTS.md` import)     |
| Aider                                                         | `CONVENTIONS.md` (passed via `--read`)         |

**Pattern:** put the canonical rules in `AGENTS.md` at the repo root, then add a thin real-file `CLAUDE.md` shim containing `@AGENTS.md` (Claude Code's import syntax) plus any Claude-specific addendum. Symlinks don't work reliably on Windows, so keep shims as real files. Tool support shifts quarterly — when adopting a new assistant, check its current docs for which file it reads.

**What belongs in the rules file** (keep it under ~200 lines — longer files get ignored by humans and models alike):

- Branching model and commit conventions
- Code layout rule (transport vs logic split; what can import what)
- Testing expectation (coverage floor, test location mirroring, no-mock-DB rules)
- Versioning rule (where the version string lives; don't duplicate)
- Documentation sync rule (change → required doc mapping; task-closing checklist)
- Security rules (secrets, non-root containers, least-privilege workflows)
- A "What NOT to do" section for the most common violations
- Pointers to `CONTRIBUTING.md` and this playbook for detail

**What to leave out:** long prose, justifications, history. The rules file is an operating contract; the rationale lives in `CONTRIBUTING.md` and this playbook.

**Starting point:** `agents_template.md` (mirrored alongside this playbook in `docs/guides/`) is a copy-and-fill skeleton with the cross-project essence of these rules pre-filled. Drop it in as your new repo's `AGENTS.md`, replace the `{{placeholders}}`, prune the project-type-specific lines, and you have a working operating contract in ten minutes.

---

## GitHub tracking setup

Issues and a project board are the single source of truth for *tracked work*. The `roadmap.md` file in your repo is a lie within a week. The issue list isn't.

### Label taxonomy

Three axes, orthogonal. Every issue gets one `type/…`, one `priority/…` (priority may be omitted for backlog-aged items), and one `area/…` when one fits — create the area label first if it's new.

| Axis          | Labels                                                                 | Purpose                   |
| ------------- | ----------------------------------------------------------------------- | ------------------------- |
| **type/**     | `type/feature`, `type/bug`, `type/refactor`, `type/docs`, `type/infra` | What kind of work?        |
| **area/**     | `area/api`, `area/db`, `area/ui`, `area/deploy`, … (invent as needed)  | Which part of the system? |
| **priority/** | `priority/tier-1`, `priority/tier-2`, `priority/tier-3`                | How urgent?               |

Avoid `good-first-issue`, `help-wanted`, `wontfix` noise until you have contributors who need them.

### Milestones

One per `MAJOR.MINOR` version. Patch releases share the parent minor milestone. Create the milestone when you commit to scope — not weeks in advance.

### Project board

Use GitHub Projects v2 with a Status field, standardized at four columns: `Backlog → Ready → In Progress → Done`. Backlog = captured, not committed; Ready = triaged into a milestone, free to pick up; In Progress = actively worked; Done = landed and closed. Review state lives on the PR itself, not the board — a separate `In Review` column just goes stale. Built-in automations can cover the mechanical edge (issue closed → Done); the other transitions are deliberate moves.

If you're working with an AI coding assistant, give it explicit ownership of the transitions it's well-positioned to make: **Backlog** when it creates an issue on your behalf, **In Progress** as the first step when it picks one up (before any code reading or planning), **Done** as part of the same close-out batch that closes the issue. Moving `Backlog → Ready` stays the maintainer's triage call. Codify this in your `AGENTS.md` so the rule survives across sessions. The board lies if the assistant writes code while the issue still says Backlog — and "No Status" is worse, because the item disappears from board views entirely.

### Issue templates

Two markdown templates in `.github/ISSUE_TEMPLATE/`:

- **`bug.md`** — sections: Goal, Context, Reproduction, Expected vs actual, Root cause (optional), Fix options (optional), Verification.
- **`task.md`** — sections: Goal, Context, Technical details (file-path checkboxes with `[NEW]` / `[MODIFY]` / `[DELETE]` markers — the marker carries the change type, the checkbox carries progress), Options (optional), Verification, Related.

Markdown templates (rather than YAML issue forms) preserve the freeform prose + file-path markers that make issues actually readable. Forms over-constrain technical details. Canonical copies ship with this doc set (`issue_template_bug.md`, `issue_template_task.md`) — install those rather than retyping the section lists.

Add `config.yml` next to them so the two templates are the only entry path:

```
blank_issues_enabled: false
```

### PR template

`.github/pull_request_template.md` with sections: Summary, Related issue, Changes, Verification (checklist), Notes for reviewer. One-paragraph template, not a wall of text. Canonical copy: `pr_template.md` in this doc set.

### Day-zero bootstrap commands

```
# Labels
gh label create "type/feature"   --color 0e8a16 --description "New capability"
gh label create "type/bug"       --color b60205 --description "Defect or regression"
gh label create "type/refactor"  --color fbca04 --description "Code reshape, no behavior change"
gh label create "type/docs"      --color 0075ca --description "Documentation only"
gh label create "type/infra"     --color c5def5 --description "Build, CI, deploy, tooling"
gh label create "priority/tier-1" --color b60205 --description "High priority quick win"
gh label create "priority/tier-2" --color e99695 --description "Medium priority feature"
gh label create "priority/tier-3" --color c2e0c6 --description "Strategic / research"

# First milestone
gh api repos/<owner>/<repo>/milestones -X POST -f title=v0.1 -f description="Initial release"

# Project board
gh project create --owner <owner> --title "<project-name>"
# Then configure Status field options via the GraphQL API or UI:
# Backlog / Ready / In Progress / Done
```

---

## Deployment pattern

This section is shaped for a **long-running server-side service** (bot, API, web app). If you're shipping a desktop app, mobile app, CLI tool, or library, the principle is the same — *push to `master` triggers a fully automated, smoke-checked release artifact* — but the artifact changes:

- **Desktop:** signed installer/`.dmg`/`.AppImage` uploaded as a GitHub Release asset by the deploy workflow.
- **Mobile:** signed `.ipa`/`.aab` uploaded to TestFlight / Play Console internal track via Fastlane or equivalent.
- **CLI / library:** package published to the relevant registry (PyPI, npm, crates.io, Homebrew tap) by the deploy workflow.

Whatever the artifact, the rules below still apply: tag with the version, smoke-check the output, fail loud, never deploy what didn't pass CI. Skip the Docker/VPS specifics that don't apply to your project.

The target for a service: **push to `master` → container built → deployed to VPS → smoke-checked → done.** Under 5 minutes, fully automated.

### Building blocks

- **Container registry** — GHCR is free and integrates with GitHub Actions (`${{ secrets.GITHUB_TOKEN }}`). No separate account.
- **Runtime host** — any VPS (Hetzner, DigitalOcean, Vultr, Linode). Ubuntu LTS, Docker + Compose installed.
- **Reverse proxy** — [Caddy](https://caddyserver.com) as a compose sidecar handles TLS automatically via Let's Encrypt. One line of config (`your-domain.com { reverse_proxy app:8080 }`).
- **Secrets** — live in `.env` on the VPS, loaded via `env_file:` in compose. Never in git, never in the image, never in workflow logs.
- **Volumes** — persistent data (SQLite DB, uploaded files) in a named volume mounted into the container. Survives `docker compose up -d --remove-orphans`.

### Dockerfile essentials

- **Start from a pinned base.** `python:3.13-slim-bookworm` is better than `python:3.13-slim` (OS version pinned). A full digest pin (`@sha256:…`) is best but needs a bot to keep it fresh.
- **Hold the image's runtime to CI's.** The base image, CI's setup step and the linter's target version each name the runtime's minor version in their own file, so a test asserts they agree. The image is the only one a bump bot moves — Dependabot's docker updates change the base tag alone, and a CI that builds no image stays green — so without the test the runtime that ships stops being the one CI tests. Unlike a tool pinned twice (see *Pin each tool once*), the assertion fails only the PR that would split them, and that PR is the reminder to move all three in one change.
- **Layer-cache dependencies first.** Copy the manifest + lockfile, install deps, *then* copy source. Changing a source file doesn't reinstall deps.
- **Build from a committed lockfile.** The manifest declares intent; the lockfile pins reality. Without one, every image build resolves dependencies differently — the single-source-of-truth principle, applied to deps.
- **Run as non-root.** `RUN useradd --create-home app && USER app`. Defense in depth against container escapes.
- **Declare a `HEALTHCHECK`.** Docker and the deploy workflow both use it.
- **One process per container.** Use a separate service for each long-running process.

### docker-compose essentials

- **`restart: unless-stopped`** — survives reboots without restart loops.
- **`mem_limit` and `cpus`.** Prevents a runaway process from taking down the host.
- **`healthcheck:`** at the compose level, used by `depends_on: condition: service_healthy` for sidecars.
- **Named volumes**, not bind mounts, for data. Easier to back up, doesn't depend on host paths.

### Secret rotation

Document in `docs/guides/deployment.md`: for each secret (API keys, tokens, encryption keys), how to rotate it end-to-end. When you're being paged at 2am you do not want to improvise.

### Backups

- Anything you can't regenerate — the SQLite volume, uploaded files — gets an automated, scheduled, **off-host** backup: a cron job shipping `sqlite3 .backup` output or volume tarballs to object storage or another machine. The VPS dying must not be a data event.
- **Test the restore once.** An untested backup is a hope, not a backup. Document the restore steps in `docs/guides/deployment.md`, next to rotation.
- Put a dead-man switch on the backup cron (a healthchecks.io-style success ping). Silent backup failure is the default failure mode.

### Uptime — you are the pager

Solo projects have no on-call rotation; nothing notices a dead service except an annoyed user. Point a free external uptime monitor (UptimeRobot, healthchecks.io, Better Stack, …) at `/health` the day the service first deploys, with alerts going somewhere you actually look. Costs five minutes and pairs with the version probe — the same endpoint serves both.

---

## Security baseline

The minimum every project should have on day zero:

1. **`SECURITY.md`** — how to report a vulnerability, expected response time, what's in scope. Even for solo projects. [GitHub Security Advisories](https://docs.github.com/en/code-security/security-advisories) provide a private disclosure channel for free.
2. **Dependabot** (`.github/dependabot.yml`) — weekly PRs for dependency updates, grouped by ecosystem (pip/npm, actions, docker, pre-commit). Silent security updates become loud when a PR lands in your inbox. Don't omit the hook manager — its pins are the ones with no other bump path.
3. **CodeQL** (`.github/workflows/codeql.yml`) — scheduled + PR-triggered static analysis. Catches SQL injection, path traversal, hardcoded credentials, unsafe deserialization. Zero-config for most languages. **Public repos and Code Security–enabled orgs only**; not available on private personal repos (see section on `codeql.yml` above).
4. **Secret scanning** — pre-commit gitleaks hook (local) and GitHub's built-in push protection (server).
5. **Non-root containers** — already covered above.
6. **Least-privilege workflow permissions** — every job's `permissions:` block should enumerate only what it needs. Default is `read: all`, which is often too much.

---

## Day-zero checklist

Copy-pasteable, in order.

```
# 1. Initialize on develop (or cd into the existing idea folder and init in place)
mkdir -p my-project && cd my-project
git init -b develop

# 2. Create the scaffold (adjust for your language)
mkdir -p .github/{workflows,ISSUE_TEMPLATE} src tests docs/{product,architecture,guides,adr} scripts

# 3. Source-of-truth files
touch README.md CHANGELOG.md CONTRIBUTING.md LICENSE .env.example .gitignore
touch Dockerfile docker-compose.yml .pre-commit-config.yaml
touch .github/SECURITY.md .github/pull_request_template.md .github/dependabot.yml
touch .github/ISSUE_TEMPLATE/{bug.md,task.md,config.yml}
touch .github/workflows/{ci.yml,deploy.yml}
# Add codeql.yml only if the repo is public or the org has Code Security
touch docs/README.md

# 4. First commit — nothing can be pushed before this exists
git add .
git commit -m "chore: initial scaffold"

# 5. Create the remote and push develop (the first branch pushed becomes the default)
gh repo create <owner>/<repo> --private --source=. --push --description "..."

# 6. Create master from develop — the release-PR target must exist
git branch master
git push -u origin master

# 7. Labels + first milestone (see commands above)
# 8. Project board (see commands above)

# 9. Branch protection on master (JSON body — nested fields don't work with -F)
# "contexts" must match your CI job name exactly
gh api -X PUT repos/<owner>/<repo>/branches/master/protection --input - <<'JSON'
{
  "required_status_checks": { "strict": true, "contexts": ["CI / lint-and-test"] },
  "required_pull_request_reviews": { "required_approving_review_count": 0 },
  "enforce_admins": false,
  "restrictions": null,
  "required_linear_history": false,
  "allow_force_pushes": false,
  "allow_deletions": false
}
JSON

# 10. Design foundation — before screen 2, for any human-driven surface (see guides/design_workflow.md)
#   pick the component vocabulary (record in an ADR) · set tokens once, centrally (fonts, semantic
#   colors, spacing, radius, elevation, motion) · write docs/guides/design_language.md · add the
#   "every UI edit conforms to the reference" rule to AGENTS.md · pair each screen with its
#   design file in one table, held by a test
```

After this checklist you have a repo that is wired end-to-end: CI runs on PRs, deploy runs on merge to master, docs have a home, issues have templates, contributions have a process, secrets are scanned, and the design foundation is set before the second screen.

---

## Grow as you go

Don't add these on day zero. Add them when the project earns them.

| When to add                          | Thing                                         | Why wait                                                                         |
| ------------------------------------ | --------------------------------------------- | --------------------------------------------------------------------------------- |
| When a second contributor joins      | `CODEOWNERS`                                  | Solo = pointless                                                                 |
| When you merge >10 PRs/week          | `release-drafter` auto-changelog              | Manual CHANGELOG is fine at low volume and keeps you thinking about what changed |
| When you run >1 environment          | GitHub Environments with protection rules     | Single-env deploys don't need approval gates                                     |
| When you accept outside PRs          | `CODE_OF_CONDUCT.md`                          | Solo projects don't need it; OSS projects do                                     |
| When you have >3 services in compose | Kubernetes / Nomad / Swarm                    | Docker Compose handles 1-5 services fine                                         |
| When deploys exceed 5 min            | Build caching (BuildKit mounts, ccache, etc.) | Premature optimization                                                           |
| When you hit a real perf problem     | Benchmarks, flamegraphs, APM                  | Same                                                                             |
| When you get paged                   | Runbooks, on-call rotation, alerting          | Same                                                                             |

---

## What to skip (and why)

Things that look like best practice but are overkill for small/solo projects. If someone tells you "you need this," ask what problem it solves — and whether you actually have that problem.

- **Monorepo tooling (Nx, Turborepo, Bazel)** — unless you genuinely have multiple deployed services sharing code, a plain repo with `apps/` + `packages/` is simpler. The tooling tax isn't worth it until it is.
- **Microservices** — start with a monolith. You can always split later. You rarely un-split.
- **Event sourcing, CQRS, hexagonal architecture** — patterns for specific problems. Apply when the problem shows up, not before.
- **Custom auth** — use a provider (Auth0, Clerk, Supabase Auth, whatever your platform offers). Auth bugs are not the bugs you want to debug.
- **Kubernetes for a side project** — Docker Compose on one VPS handles real traffic up to hundreds of concurrent users. The complexity of k8s is never "free."
- **Generated SDKs from OpenAPI** — useful if you have external API consumers. If the only caller is your own frontend, hand-writing a thin client is often simpler.
- **Feature flag services (LaunchDarkly, etc.)** — a boolean in config does the job until you need user-targeted rollouts.

---

## Keep a living reference

A playbook stays honest only while it points at something running. Keep at least one deployed project that implements this playbook end-to-end, and when kickstarting a new repo, crib from it directly: `CONTRIBUTING.md` for the branching and release text, `.github/workflows/` for CI and deploy, `.pre-commit-config.yaml` for the local gate, the issue and PR templates, and `docs/` for the layering. When a newer project improves on the reference, it becomes the new reference — and the improvement gets folded back into this playbook so every future project inherits it.
