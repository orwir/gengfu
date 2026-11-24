# Contributing — Gengfu

How to set this repository up and how to release it. Branching, commits, the
quality gates, the task's review rounds and the board are this repository's
operating contract, [AGENTS.md](AGENTS.md); why each rule exists is the lifecycle
kit's
[project_kickstart.md](https://github.com/orwir/lifecycle-kit/blob/main/method/project_kickstart.md).

## Local setup

1. Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
   and clone the repo. Windows 11 is needed to build and run the app.
2. Install the git hooks: `powershell -File scripts/setup.ps1`.
   Non-negotiable — the pre-push stage is the local CI gate.
   Install [gitleaks](https://github.com/gitleaks/gitleaks) too,
   at the version CI's last `secrets` run printed (8.19 or later, which has the
   `git` command the hook runs), and pin it there:
   `winget install --id Gitleaks.Gitleaks --exact --scope user --version <v>`,
   then `winget pin add --id Gitleaks.Gitleaks --version <major.minor>.*`.
   Restart the shells and apps you commit from, so they find it on `PATH`:
   where it is missing, the commit stage says its secret scan did not run.
   When CI's version moves, move this copy with it:
   `winget pin remove --id Gitleaks.Gitleaks`,
   `winget upgrade --id Gitleaks.Gitleaks --exact --version <v>`, and the pin
   again at the new line.
3. There is no `.env`. The app keeps its settings in `%AppData%\Gengfu`, or in
   the folder the `GENGFU_HOME` environment variable names.
4. Build with `dotnet build Gengfu.slnx`, and run Gengfu with
   `GENGFU_HOME=<folder> dotnet run --project src/Gengfu.App`.

## Releasing

[SemVer](https://semver.org), picked by what landed since the last tag: a
release ships what has landed. How the release is built, published and rolled
back: [release.md](docs/guides/release.md). A test build, `X.Y.Z-rc.N`
([release.md § Versions](docs/guides/release.md#versions)), keeps its CHANGELOG
entries under `[Unreleased]`, which become its notes. The checklist is literal —
execute it in order, no shortcuts:

1. **Read what the release's CI will read.** Under the release's dispatch,
   CI's secret scan (gitleaks-action) reads the whole history, not only the
   commits a push brings: run `gitleaks git -v --redact` on a fresh clone, at
   the gitleaks version the action's last run printed (the copy Local setup
   pins, moved first as it says where the action's version moved), with the
   repository's `.gitleaks.toml` and `.gitleaksignore`, and land what it finds
   before the bump. A secret it finds is rotated and its fingerprint added to
   `.gitleaksignore`, never only deleted: the history keeps it.
2. **Bump the version** in `Directory.Build.props` (`<Version>`) — the single
   source of truth.
3. **Update `CHANGELOG.md`**: date the new section, group entries under
   Added / Changed / Fixed / Removed, reference issues (`#N`).
4. **Commit and push** the release commit to `main`.
5. **Review the release** on the compare link from the last tag (`git fetch
   --tags origin` first: the workflow makes the tags on GitHub) to that commit
   (`https://github.com/orwir/gengfu/compare/v<last>...<sha>`), with the
   readings of every check that runs only before a release. A correction lands
   on `main`, and the review reads the new head.
6. **— Checkpoint —** If an AI assistant is driving, it stops here and waits
   for explicit approval. Step 7 is irreversible and ships to users; a prior
   "do the release" does not extend past this line.
7. **Dispatch the release workflow** (`release.yml`) on the reviewed commit and
   watch the run to its end: `lifecycle release <sha>`. It refuses when `main`
   moved since the review (check the version and the changelog against the new
   head, review it, then dispatch that) or when the version is tagged already;
   otherwise it runs as
   [release.md § The release run](docs/guides/release.md#the-release-run) says.
8. **Verify the release**: it lists the files
   [release.md § What a release is](docs/guides/release.md#what-a-release-is)
   names, with the version's notes. Run the published Setup on a Windows 11 PC
   without a developer's tools (Windows Sandbox will do), or let an installed
   Gengfu update itself, and check that Settings › General shows the new
   version. A release isn't done until this passes.

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
   `lifecycle release <sha> --ref hotfix/<name>`.
4. Merge the branch into `main` with a merge commit (`git merge --no-ff`),
   never squashed, so the tag stays in its history, and push.
5. Delete the hotfix branch — remote and local.

With nothing unreleased on `main`, a fix is an ordinary task and an ordinary
patch release.
