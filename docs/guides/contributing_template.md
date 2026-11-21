<!-- Installs to CONTRIBUTING.md at the repo root. Fill every {{token}}, delete
     sections that don't apply. The compressed agent contract (AGENTS.md) points
     here for mechanics; keep the two in sync. -->

# Contributing — {{PROJECT_NAME}}

How to work in this repository. The compressed operating contract for AI
assistants lives in [AGENTS.md](AGENTS.md); this file carries the mechanics and
rationale that don't fit there. Reusable cross-project patterns live in
[docs/guides/project_kickstart.md](docs/guides/project_kickstart.md).

## Local setup

1. Clone and install dependencies: `{{SETUP_COMMANDS}}` ({{e.g., `uv sync`, `npm install`}}).
2. Install the git hooks: `{{HOOK_INSTALL_COMMAND}}` ({{e.g., `pre-commit install
   --hook-type pre-commit --hook-type pre-push`}}). Non-negotiable — the pre-push
   stage is the local CI gate.
3. Copy `.env.example` → `.env` and fill the values (the comments in
   `.env.example` say where each one comes from).

## Branching

- **`{{INTEGRATION_BRANCH}}`** — the one long-lived branch and the default
  branch, where everyday work lands. A push to it ships nothing: a release goes
  out only through the release workflow (Releasing below), and its tags are what
  shipped.
- **Feature branches** — `feat/… | fix/… | chore/… | docs/… | refactor/… |
  perf/… | test/…`, branched from `{{INTEGRATION_BRANCH}}`, squash-merged.
- **Hotfix branches** — `hotfix/…`, cut from the last release's tag, released
  from the branch, then merged into `{{INTEGRATION_BRANCH}}` with a merge commit,
  never squashed (see Hotfixes below).
- **Merged branches get deleted, immediately and on both ends.** `gh pr merge
  --delete-branch` removes the remote copy only; finish the job locally:

  ```bash
  git checkout {{INTEGRATION_BRANCH}} && git pull
  git branch -d feat/my-thing
  git fetch --prune
  ```

  A branch listed by `git branch` is either active work or it shouldn't exist.
  Periodic sweep: `git branch --merged {{INTEGRATION_BRANCH}}` should list
  nothing but `{{INTEGRATION_BRANCH}}` itself.

## Commits

[Conventional Commits](https://www.conventionalcommits.org/):

- Prefixes: `feat`, `fix`, `refactor`, `docs`, `test`, `chore`, `perf`. Optional
  scope: `fix(parser): …`.
- Subject in imperative mood, no trailing period, under 72 characters.
- Body (when used) explains **why**, not what — the diff shows what.
- Never `--no-verify`. If a hook fails, fix the cause; if the hook is wrong,
  propose a hook fix.

## Quality gates

- **Commit stage** (fast, changed files): lint, format, config-file sanity,
  large-file guard, secret scan.
- **Push stage** (CI parity, full tree): `{{LINT_TOOL}}` over everything, the
  full `{{TEST_RUNNER}}` suite, coverage floor `{{COVERAGE_FLOOR}}%`. What passes
  the push passes CI; if it doesn't, the hook config has drifted — fix the hook.

## Task review

A task is read twice: its plan before any code is written, its work before it
lands (`docs/guides/task_review.md`). At *Clarify*, where the plan in the issue
has a shape that has shipped holes before, the plan round has a reviewer with
no author context read it against the code, and the maintainer accepts it from
the Clarify brief; a plan with no such shape and nothing to answer is clear,
and the developer goes on. Before the hand-back, the work round has a reviewer
read the tree against the accepted plan, with a lens reviewer beside it for each
person- or model-facing surface the diff touches: the developer session renders
a hand-off packet, launches the reviewer agents with it, acts on their return
packets and renders the hand-back brief, in both flows. The packets pass between
the agents and stay there: nothing of a round is posted to GitHub but the plan
it shaped. The maintainer reads the briefs alone and answers their pending
rows. The rest is already checked.

## Issue and board lifecycle

Work is tracked on GitHub issues and the project board. Four Status columns,
each with one meaning:

| Status          | Meaning                                                        |
| --------------- | -------------------------------------------------------------- |
| **Backlog**     | Captured, not committed. Every new issue starts here.          |
| **Ready**       | Triaged: assigned to a milestone, free to pick up.             |
| **In Progress** | Someone (or an agent) is actively on it.                       |
| **Done**        | Landed and closed.                                             |

Moving Backlog → Ready is the maintainer's triage call. Whoever picks an issue
up moves it to In Progress as their first step; whoever closes it moves it to
Done in the same close-out. The issue **body** describes the state of the
*system*; the board describes the state of the *work* — never duplicate one in
the other.

An issue is written in two stages. At capture it says what and why — a task
down to *Related*, a bug down to *Expected vs actual* — and nothing about how:
a plan written then is stale by the time the work starts. Whoever picks it up
writes the *Plan* at *Clarify*, from the code as it stands then, and it is
accepted before any code: by the maintainer, or, when it has no shape for the
plan round and nothing to answer, by the developer. The plan's *Status* is the
plan's own, as an ADR's is, not the work's. Its ticked boxes and *Departures*
keep the issue true until it closes, so a fresh session can pick the work up
from the issue and the tree.

Labels: one `type/…`, one `priority/…` (may be omitted for backlog-aged items),
and one `area/…` when one fits — create the area label first if it's new.

## Releasing

[SemVer](https://semver.org). Each `MAJOR.MINOR` maps to a milestone; patches
reuse it. The checklist is literal — execute it in order, no shortcuts:

1. **Confirm the milestone is clean.** `gh issue list --milestone v<X.Y>
   --state open` returns nothing (close or move out the rest).
2. **Bump the version** in `{{VERSION_FILE}}` — the single source of truth.
3. **Update `CHANGELOG.md`**: date the new section, group entries under
   Added / Changed / Fixed / Removed, reference issues (`#N`).
4. **Commit and push** the release commit to `{{INTEGRATION_BRANCH}}`.
5. **Review the release** on the compare link from the last tag (`git fetch
   --tags origin` first: the workflow makes the tags on GitHub) to that commit
   (`https://github.com/<owner>/<repo>/compare/v<last>...<sha>`), with the
   readings of every check that runs only before a release. A correction lands
   on `{{INTEGRATION_BRANCH}}`, and the review reads the new head.
6. **— Checkpoint —** If an AI assistant is driving, it stops here and waits
   for explicit approval. Step 7 is irreversible and ships to production; a
   prior "do the release" does not extend past this line.
7. **Dispatch the release workflow** (`{{RELEASE_WORKFLOW}}`, named under
   `[release]` in `.lifecycle.toml`) on the reviewed commit, and watch the run
   to its end: `lifecycle release <sha>`. It refuses when
   `{{INTEGRATION_BRANCH}}` moved since the review (review the new head, then
   dispatch that) or when the version is tagged already; otherwise it builds,
   ships, and creates the tag and the GitHub Release last.
8. **Close the milestone.**
9. **Verify the deployment**: `{{VERSION_PROBE}}` ({{e.g., `curl
   https://…/health`, `--version` of the published artifact}}) reports the new
   version. A release isn't done until this passes.

## Hotfixes

For an urgent production fix while `{{INTEGRATION_BRANCH}}` holds unreleased
work:

1. Cut `hotfix/…` from the last release's tag: `git fetch --tags origin`, then
   `git switch -c hotfix/<name> v<last>`. Where that tag is older than the release workflow, bring the
   workflow file over from `{{INTEGRATION_BRANCH}}` first: a dispatch runs the
   workflow file of the branch it names.
2. Fix, with a test that fails before and passes after, and push the branch.
   Nothing merges it yet.
3. Release from the branch: the Releasing steps with the patch version, the
   dispatch on the branch (`lifecycle release <sha> --ref hotfix/<name>`).
4. Merge the branch into `{{INTEGRATION_BRANCH}}` with a merge commit (`git
   merge --no-ff`), never squashed, so the tag stays in its history, and push.
5. Delete the hotfix branch — remote and local.

With nothing unreleased on `{{INTEGRATION_BRANCH}}`, a fix is an ordinary task
and an ordinary patch release.

## Documentation

Every code change that makes an existing doc wrong updates that doc **in the
same commit**. The change → required-doc mapping and the task-closing checklist
live in [AGENTS.md](AGENTS.md); the doc-layer map lives in
[docs/guides/project_kickstart.md](docs/guides/project_kickstart.md).
