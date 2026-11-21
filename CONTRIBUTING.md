# Contributing — Gengfu

How to work in this repository. The compressed operating contract for AI
assistants lives in [AGENTS.md](AGENTS.md); this file carries the mechanics and
rationale that don't fit there. Reusable cross-project patterns live in
[docs/guides/project_kickstart.md](docs/guides/project_kickstart.md).

## Local setup

1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
   and clone the repo. Windows 11 is needed to build and run the app.
2. Install the git hooks: `powershell -File scripts/setup.ps1`. It also checks
   that a .NET 10 SDK is reachable. Non-negotiable — the pre-push stage is the
   local CI gate.
3. There is no `.env`. The app keeps its settings in `%AppData%\Gengfu`, or in
   the folder the `GENGFU_HOME` environment variable names. Tests, harnesses and
   checks never use yours: a test gives its `Session` a temp folder, a run of
   Gengfu itself sets `GENGFU_HOME`, and the smoke install refuses to run where
   Gengfu is installed, since Setup would take over its registration. Local test
   settings go in `*.local.*` files, which git ignores.
4. Build with `dotnet build Gengfu.slnx`, and run Gengfu on a folder of its own
   with `GENGFU_HOME=<folder> dotnet run --project src/Gengfu.App`.

## Branching

- **`main`** — the one long-lived branch and the default branch, where
  everyday work lands. A push to it ships nothing: a release goes out only
  through the release workflow (Releasing below), and its tags are what
  shipped.
- **Feature branches** — `feat/… | fix/… | chore/… | docs/… | refactor/… |
  perf/… | test/…`, branched from `main`, squash-merged.
- **Hotfix branches** — `hotfix/…`, cut from the last release's tag, released
  from the branch, then merged into `main` with a merge commit, never squashed
  (see Hotfixes below).
- **Merged branches get deleted, immediately and on both ends.** `gh pr merge
  --delete-branch` removes the remote copy only; finish the job locally:

  ```bash
  git checkout main && git pull
  git branch -d feat/my-thing
  git fetch --prune
  ```

  A branch listed by `git branch` is either active work or it shouldn't exist.
  Periodic sweep: `git branch --merged main` should list nothing but `main`
  itself.

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

A task is read twice: its plan before any code is written, its work before it
lands (`docs/guides/task_review.md`). At *Clarify*, where the plan in the issue
has a shape that has shipped holes before, the plan round has a reviewer with
no author context read it against the code, and the maintainer accepts it from
the Clarify brief; a plan with no such shape and nothing to answer is clear,
and the developer goes on. Before the hand-back, the work round has a reviewer
read the tree against the accepted plan, with a lens reviewer beside it for each
person-facing surface the diff touches: the developer session renders a
hand-off packet, launches the reviewer agents with it, acts on their return
packets and renders the hand-back brief, in both flows. The packets pass between
the agents and stay there: nothing of a round is posted to GitHub but the plan
it shaped. The maintainer reads the briefs alone and answers their pending
rows. The rest is already checked.

The lifecycle kit (`orwir/lifecycle-kit`) runs the rounds: its `clarify`,
`review` and `handback` skills, its read-only reviewer, and its `complete` skill
for the close-out, reading this repository's settings from `.lifecycle.toml`.
`lifecycle doctor` says what the repository needs before a task, and
`lifecycle upgrade` moves `docs/guides/` to a newer kit release.

## Design

New screens and visual changes are designed before they are built:
[docs/guides/design_workflow.md](docs/guides/design_workflow.md) is the how,
[docs/guides/design_language.md](docs/guides/design_language.md) the resolved
tokens, components and patterns. Each design round runs on a Claude Design
canvas in the same session as the code, and the maintainer approves the drawing
before code.

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
reuse it. How the release is built, published and rolled back:
[release.md](docs/guides/release.md). A test build, `X.Y.Z-rc.N`, publishes as
a pre-release, which only installed test builds update to; it skips steps 1,
4 and 9, and keeps its CHANGELOG entries under `[Unreleased]`, which become its
notes. The checklist is literal — execute it in order, no shortcuts:

1. **Confirm the milestone is clean.** `gh issue list --milestone v<X.Y>
   --state open` returns nothing (close or move out the rest).
2. **Bump the version** in `Directory.Build.props` (`<Version>`) — the single
   source of truth.
3. **Update `CHANGELOG.md`**: date the new section, group entries under
   Added / Changed / Fixed / Removed, reference issues (`#N`).
4. **Count the copies in use.** Before the dispatch, add the previous
   release's downloads to [release.md § Copies in use](docs/guides/release.md#copies-in-use).
   Gengfu sends no telemetry, so they are the only count of who runs it.
5. **Commit and push** the release commit to `main`.
6. **Review the release** on the compare link from the last tag (`git fetch
   --tags origin` first: the workflow makes the tags on GitHub) to that commit
   (`https://github.com/orwir/gengfu/compare/v<last>...<sha>`), and name the
   comprehensive reviews that are due (`docs/guides/review_machinery.md` §
   When a review is due), with the trigger that fired each. A correction lands
   on `main`, and the review reads the new head.
7. **— Checkpoint —** If an AI assistant is driving, it stops here and waits
   for explicit approval. Step 8 is irreversible and ships to users; a prior
   "do the release" does not extend past this line.
8. **Dispatch the release workflow** on the reviewed commit and watch the run
   to its end: `lifecycle release <sha>`, or `gh workflow run release.yml
   --ref main -f commit=<sha>` and `gh run watch`. It refuses when `main` moved
   since the review (review the new head, then dispatch that) or when the
   version is tagged already; otherwise it builds the app, installs it on the
   CI machine, and last publishes the GitHub Release with its tag `v<X.Y.Z>`
   and the CHANGELOG section as its notes.
9. **Close the milestone.**
10. **Verify the release**: run the published Setup on a Windows 11 PC (or let
    an installed Gengfu update itself) and check that Settings › General shows
    the new version. A release isn't done until this passes.

## Hotfixes

For an urgent fix to a released version while `main` holds unreleased work:

1. Cut `hotfix/…` from the last release's tag: `git fetch --tags origin`, then
   `git switch -c hotfix/<name> v<last>`. Where that tag is older than the
   release workflow, bring `.github/workflows/release.yml` over from `main`
   first: a dispatch runs the workflow file of the branch it names.
2. Fix, with a test that fails before and passes after, and push the branch.
   Nothing merges it yet.
3. Release from the branch: the Releasing steps with the patch version, the
   dispatch on the branch (`lifecycle release <sha> --ref hotfix/<name>`).
4. Merge the branch into `main` with a merge commit (`git merge --no-ff`),
   never squashed, so the tag stays in its history, and push.
5. Delete the hotfix branch — remote and local.

With nothing unreleased on `main`, a fix is an ordinary task and an ordinary
patch release.

## Documentation

Every code change that makes an existing doc wrong updates that doc **in the
same commit**. The change → required-doc mapping and the task-closing checklist
live in [AGENTS.md](AGENTS.md); the doc-layer map lives in
[docs/guides/project_kickstart.md](docs/guides/project_kickstart.md).

Players read English and Russian, so their docs come in both: `README.md` and
`README.ru.md` are the front page, `docs/guide.md` and `docs/guide.ru.md` the
player guide. A change to one updates its twin in the same commit, so neither
language falls behind. The Russian names controls by the app's own Russian
labels, from `Strings.ru.resx`, so a player finds on screen what the guide says.
The front page also carries the teaser, a video the maintainer renders outside
this repo: a visible change to the main window or Settings means rendering it
again and uploading the new cuts.
