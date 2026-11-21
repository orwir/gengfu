# 0008. Releases ship from a workflow dispatched on the reviewed commit

## Status

Accepted — 2026-11-21. Supersedes the release trigger of
[ADR-0006](0006-self-contained-installer-and-updates.md), a merge to master; the rest of ADR-0006
stands.

## Context

Gengfu had two long-lived branches: `develop`, where work landed, and `master`, where a merge ran
`release.yml` and published the version in `Directory.Build.props`.

- Each release took a `develop → master` PR, whose diff was the release review, and a fast-forward
  of `develop` after it. A hotfix took a PR into `master` and a forward-merge back into `develop`.
- What kept an unreviewed commit from shipping was `master`'s branch protection: a pull request and
  a green `gate` check before any merge.
- Gengfu is maintained alone, with AI assistants, and its work lands on the integration branch
  directly. The lifecycle kit its process runs on keeps one long-lived branch, `main`, from its
  0.4.0 release.

## Decision

- One long-lived branch, `main`, the default branch. `master` is deleted and `develop` renamed to
  `main`.
- `release.yml` runs on `workflow_dispatch` alone, with the commit the release review read as its
  input. No push, merge or tag ships anything.
- Before it builds, the run refuses a branch other than `main` or a `hotfix/…` one, a head other
  than the reviewed commit, a `-dev` version and a version tagged already. Only `git ls-remote
  --exit-code` exiting 2, no such tag, lets a version through. The refusal steps keep the names of
  the kit's CI/CD recipe, which `lifecycle release` reads.
- Then it runs the CI gate, builds, packs, installs and starts the build as a player would, and
  last publishes the GitHub Release with its tag in one `vpk upload github --tag`.
- One run at a time, never cancelled.
- A release is reviewed on the compare link from the last tag to the head, then dispatched on that
  head: `lifecycle release <sha>`.
- A hotfix is cut from the last release's tag on a `hotfix/…` branch, released from it, then merged
  into `main` with a merge commit.

## Consequences

- A push to `main` ships nothing, so shipping needs no branch protection: `master`'s went with it.
  `main` is protected only from force pushes and deletion, with no required check or review, so
  work still lands on it directly.
- The dispatch is the one irreversible step. AGENTS.md's hard gate and an ask rule in
  `.claude/settings.json` stand before it.
- A refused run builds nothing, and a failed one leaves no release and no tag. A second dispatch of
  the same version waits for the first, then is refused at its tag.
- A tag cut before this workflow has no `release.yml` to dispatch: a hotfix from it brings the file
  over from `main` first.
- Under a dispatch the gate's gitleaks scan reads the whole history, not one push's commits. A
  finding in an old commit holds the release until a `.gitleaksignore` line lands, which moves
  `main` past the reviewed head.
- A release's review reads a compare link, not a pull request, and nothing on GitHub requires the
  `gate` check before shipping; the run's own gate does.
