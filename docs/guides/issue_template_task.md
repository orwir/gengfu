---
name: Task
about: Feature, refactor, docs, or infra work
---

<!-- Installs to .github/ISSUE_TEMPLATE/task.md. Apply the type/ label by hand
     (task covers feature, refactor, docs, infra — no single auto-label fits).
     Note for agents: non-interactive `gh issue create --body` bypasses this
     template — read this file and reproduce its structure. -->

## Goal

One or two sentences: the outcome, not the steps.

## Context

State of the **system**, not of the work — work state lives on the project board.
Why now, constraints, links to prior discussion or ADRs.

## Technical details

Concrete file paths as task-list checkboxes. The marker carries the change type;
the checkbox carries progress (GitHub renders it as a progress bar on the issue
list — never edit prose to track progress).

- [ ] `[NEW]` `packages/foo/bar.py` — what it does
- [ ] `[MODIFY]` `apps/api/handler.py` — what changes
- [ ] `[DELETE]` `packages/foo/legacy.py` — why it can go

## Options (optional)

When more than one reasonable shape exists: **(A) / (B) / (C)** with a
recommendation. The maintainer picks before implementation starts.

## Verification

How "done" is proven: tests to add, commands to run, behavior to observe.

## Related

Issues, ADRs, PRs, external docs.
