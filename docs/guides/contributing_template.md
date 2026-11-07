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

- **`{{PRODUCTION_BRANCH}}`** — production. Merges only: release PRs from
  `{{INTEGRATION_BRANCH}}`, or hotfix PRs from `fix/…` branched off
  `{{PRODUCTION_BRANCH}}` (see Hotfixes below).
- **`{{INTEGRATION_BRANCH}}`** — default branch, where everyday work lands.
- **Feature branches** — `feat/… | fix/… | chore/… | docs/… | refactor/… |
  perf/… | test/…`, branched from `{{INTEGRATION_BRANCH}}`, squash-merged.
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

A task is read twice before it lands: once by a reviewer with no author
context, then by the maintainer. The first read is the round in
`docs/guides/task_review.md` — the developer session renders a hand-off packet,
launches a reviewer agent with it, acts on the return packet and renders the
hand-back brief, in both flows. The packets pass between the two agents and, on
a feature branch, reach the PR at completion as its body and a comment; the
maintainer reads the brief alone and answers its pending rows. The rest is
already checked.

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
4. **Open the release PR** `{{INTEGRATION_BRANCH}}` → `{{PRODUCTION_BRANCH}}`,
   titled `Release v<X.Y.Z>`, body = the new CHANGELOG section.
5. **— Checkpoint —** If an AI assistant is driving, it stops here and waits
   for explicit approval. Steps 6–7 are irreversible and ship to production;
   a prior "do the release" does not extend past this line.
6. **Merge after CI is green.** The merge to `{{PRODUCTION_BRANCH}}` triggers
   the deploy/publish workflow.
7. **Tag** `v<X.Y.Z>` on the merge commit and push the tag.
8. **Create the GitHub Release** from the tag, body = the CHANGELOG section.
9. **Fast-forward `{{INTEGRATION_BRANCH}}`** onto the release merge commit so
   the branches don't diverge:

   ```bash
   git checkout {{INTEGRATION_BRANCH}}
   git merge --ff-only {{PRODUCTION_BRANCH}} && git push
   ```

   If `{{INTEGRATION_BRANCH}}` moved during the release, use a regular merge
   instead of `--ff-only`.
10. **Close the milestone.**
11. **Verify the deployment**: `{{VERSION_PROBE}}` ({{e.g., `curl
    https://…/health`, `--version` of the published artifact}}) reports the new
    version. A release isn't done until this passes.

## Hotfixes

For urgent production fixes:

1. Branch `fix/…` from `{{PRODUCTION_BRANCH}}`.
2. Fix, with a test that fails before and passes after.
3. Bump the patch version, add the CHANGELOG entry.
4. PR back to `{{PRODUCTION_BRANCH}}`; merge after CI (auto-deploys).
5. Tag, create the GitHub Release.
6. Forward-merge `{{PRODUCTION_BRANCH}}` → `{{INTEGRATION_BRANCH}}` so the next
   regular release doesn't lose the patch.
7. Delete the hotfix branch — remote and local.

## Documentation

Every code change that makes an existing doc wrong updates that doc **in the
same commit**. The change → required-doc mapping and the task-closing checklist
live in [AGENTS.md](AGENTS.md); the doc-layer map lives in
[docs/guides/project_kickstart.md](docs/guides/project_kickstart.md).
