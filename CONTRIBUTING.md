# Contributing — Gengfu

How to set this repository up and how to release it, with where its design
rounds, vendored code and player docs are kept. Branching, commits, the
quality gates, the task's review rounds and the board are this repository's
operating contract, [AGENTS.md](AGENTS.md); why each rule exists is the lifecycle
kit's
[project_kickstart.md](https://github.com/orwir/lifecycle-kit/blob/main/method/project_kickstart.md).

## Local setup

1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
   and clone the repo. Windows 11 is needed to build and run the app.
2. Install the git hooks: `powershell -File scripts/setup.ps1`. It also checks
   that a .NET 10 SDK is reachable. Non-negotiable — the pre-push stage is the
   local CI gate. The commit stage (`.githooks/pre-commit`, staged files) checks
   the whitespace and layout of C# files, a 1 MB large-file guard, and a
   gitleaks secret scan when gitleaks is installed (CI always runs it); the push
   stage (`.githooks/pre-push`) runs `scripts/check.sh`, the script CI runs.
3. There is no `.env`. The app keeps its settings in `%AppData%\Gengfu`, or in
   the folder the `GENGFU_HOME` environment variable names. Tests, harnesses and
   checks never use yours: a test gives its `Session` a temp folder, a run of
   Gengfu itself sets `GENGFU_HOME`, and the smoke install refuses to run where
   Gengfu is installed, since Setup would take over its registration. Local test
   settings go in `*.local.*` files, which git ignores.
4. Build with `dotnet build Gengfu.slnx`, and run Gengfu on a folder of its own
   with `GENGFU_HOME=<folder> dotnet run --project src/Gengfu.App`.

## Design

New screens and visual changes are designed before they are built:
the lifecycle kit's
[design_workflow.md](https://github.com/orwir/lifecycle-kit/blob/main/method/design_workflow.md)
is the how,
[docs/guides/design_language.md](docs/guides/design_language.md) the resolved
tokens, components and patterns. Each design round runs on a Claude Design
canvas in the same session as the code, and the maintainer approves the drawing
before code.

## Vendored code

`lib/` holds third-party code built from source (TSLib and friends). Change it
only by adding a patch to the list in [lib/README.md](lib/README.md), marked
`gengfu patch` in the source; never reformat it.

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
   comprehensive reviews that are due (the lifecycle kit's
   [review_machinery.md](https://github.com/orwir/lifecycle-kit/blob/main/method/review_machinery.md)
   § When a review is due), with the trigger that fired each. A correction lands
   on `main`, and the review reads the new head.
7. **— Checkpoint —** If an AI assistant is driving, it stops here and waits
   for explicit approval. Step 8 is irreversible and ships to users; a prior
   "do the release" does not extend past this line.
8. **Dispatch the release workflow** on the reviewed commit and watch the run
   to its end: `lifecycle release <sha>`, or `gh workflow run release.yml
   --ref main -f commit=<sha>` and `gh run watch`. It refuses when `main` moved
   since the review (check the version, the changelog and step 1's milestone
   against the new head, review it, then dispatch that) or when the
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
3. Release from the branch by the Releasing steps, with `hotfix/<name>`
   wherever they name `main`: the patch version, the release commit pushed to
   the branch, corrections landed on it, and the dispatch
   `lifecycle release <sha> --ref hotfix/<name>`. Skip steps 1 and 9: the
   milestone holds the unreleased work.
4. Merge the branch into `main` with a merge commit (`git merge --no-ff`),
   never squashed, so the tag stays in its history, and push.
5. Delete the hotfix branch — remote and local.

With nothing unreleased on `main`, a fix is an ordinary task and an ordinary
patch release.

## Documentation

Players read English and Russian, so their docs come in both: `README.md` and
`README.ru.md` are the front page, `docs/guide.md` and `docs/guide.ru.md` the
player guide. A change to one updates its twin in the same commit, so neither
language falls behind. The Russian names controls by the app's own Russian
labels, from `Strings.ru.resx`, so a player finds on screen what the guide says.
The front page also carries the teaser, a video the maintainer renders outside
this repo: a visible change to the main window or Settings means rendering it
again and uploading the new cuts.
