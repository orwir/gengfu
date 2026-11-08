# Contributing — Gengfu

How to work in this repository. The compressed operating contract for AI
assistants lives in [AGENTS.md](AGENTS.md); this file carries the mechanics and
rationale that don't fit there. Reusable cross-project patterns live in
[docs/guides/project_kickstart.md](docs/guides/project_kickstart.md).

## Local setup

1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
   and clone the repo. Windows 10 or 11 is needed to build and run the app.
2. Install the git hooks: `powershell -File scripts/setup.ps1`. It also checks
   that a .NET 10 SDK is reachable. Non-negotiable — the pre-push stage is the
   local CI gate.
3. There is no `.env`. The app keeps its settings under `%AppData%`; local test
   settings go in `*.local.*` files, which git ignores.

## Branching

- **`master`** — releases. Merges only: release PRs from `develop`, or hotfix
  PRs from `fix/…` branched off `master` (see Hotfixes below).
- **`develop`** — default branch, where everyday work lands.
- **Feature branches** — `feat/… | fix/… | chore/… | docs/… | refactor/… |
  perf/… | test/…`, branched from `develop`, squash-merged.
- **Merged branches get deleted, immediately and on both ends.** `gh pr merge
  --delete-branch` removes the remote copy only; finish the job locally:

  ```bash
  git checkout develop && git pull
  git branch -d feat/my-thing
  git fetch --prune
  ```

  A branch listed by `git branch` is either active work or it shouldn't exist.
  Periodic sweep: `git branch --merged develop` should list nothing but
  `develop` itself.

## Commits

[Conventional Commits](https://www.conventionalcommits.org/):

- Prefixes: `feat`, `fix`, `refactor`, `docs`, `test`, `chore`, `perf`. Optional
  scope: `fix(teamspeak): …`.
- Subject in imperative mood, no trailing period, under 72 characters.
- Body (when used) explains **why**, not what — the diff shows what.
- Never `--no-verify`. If a hook fails, fix the cause; if the hook is wrong,
  propose a hook fix.

## Quality gates

- **Commit stage** (`.githooks/pre-commit`, staged files): whitespace and layout
  of C# files, a 1 MB large-file guard, and a gitleaks secret scan when gitleaks
  is installed (CI always runs it).
- **Push stage** (`.githooks/pre-push` → `scripts/check.sh`, full tree):
  `dotnet format` over everything outside `lib/`, a Release build with warnings
  as errors, the full xUnit suite, and the coverage floor of 80% of
  `Gengfu.Core` lines. CI runs the same script; if something passes the push but
  fails CI, the hook setup has drifted — fix it.

## Task review

A task is read twice before it lands: once by a reviewer with no author
context, then by the maintainer. The first read is the round in
`docs/guides/task_review.md` — the developer session renders a hand-off packet,
launches a reviewer agent with it, acts on the return packet and renders the
hand-back brief, in both flows. The packets pass between the two agents and
stay there: nothing of the round is posted to GitHub. The maintainer reads the
brief alone and answers its pending rows. The rest is already checked.

## Design

New screens and visual changes are designed before they are built:
[docs/guides/design_workflow.md](docs/guides/design_workflow.md) is the how,
[docs/guides/design_language.md](docs/guides/design_language.md) the resolved
tokens, components and patterns. Design happens in Claude Design and is
approved by the maintainer before code.

## Vendored code

`lib/` holds third-party code built from source (TSLib and friends). Change it
only by adding a patch to the list in [lib/README.md](lib/README.md), marked
`gengfu patch` in the source; never reformat it.

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
2. **Bump the version** in `Directory.Build.props` (`<Version>`) — the single
   source of truth.
3. **Update `CHANGELOG.md`**: date the new section, group entries under
   Added / Changed / Fixed / Removed, reference issues (`#N`).
4. **Open the release PR** `develop` → `master`, titled `Release v<X.Y.Z>`,
   body = the new CHANGELOG section.
5. **— Checkpoint —** If an AI assistant is driving, it stops here and waits
   for explicit approval. Steps 6–7 are irreversible and ship to users;
   a prior "do the release" does not extend past this line.
6. **Merge after CI is green.** The merge to `master` triggers the release
   workflow, which builds the app and publishes it to GitHub Releases (added in
   v0.1).
7. **Tag** `v<X.Y.Z>` on the merge commit and push the tag.
8. **Create the GitHub Release** from the tag, body = the CHANGELOG section.
9. **Fast-forward `develop`** onto the release merge commit so the branches
   don't diverge:

   ```bash
   git checkout develop
   git merge --ff-only master && git push
   ```

   If `develop` moved during the release, use a regular merge instead of
   `--ff-only`.
10. **Close the milestone.**
11. **Verify the release**: install the published build (or let an installed
    one update) and check that Settings › General shows the new version. A
    release isn't done until this passes.

## Hotfixes

For urgent fixes to a released version:

1. Branch `fix/…` from `master`.
2. Fix, with a test that fails before and passes after.
3. Bump the patch version, add the CHANGELOG entry.
4. PR back to `master`; merge after CI (publishes the release).
5. Tag, create the GitHub Release.
6. Forward-merge `master` → `develop` so the next regular release doesn't lose
   the patch.
7. Delete the hotfix branch — remote and local.

## Documentation

Every code change that makes an existing doc wrong updates that doc **in the
same commit**. The change → required-doc mapping and the task-closing checklist
live in [AGENTS.md](AGENTS.md); the doc-layer map lives in
[docs/guides/project_kickstart.md](docs/guides/project_kickstart.md).
