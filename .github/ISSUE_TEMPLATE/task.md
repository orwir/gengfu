---
name: Task
about: Feature, refactor, docs, or infra work
---

<!-- Installs to .github/ISSUE_TEMPLATE/task.md. Apply the type/ label by hand
     (task covers feature, refactor, docs, infra — no single auto-label fits).
     Note for agents: non-interactive `gh issue create --body` bypasses this
     template — read this file and reproduce its structure.
     Written in two stages. At capture: Goal, Context and Related, and nothing
     about how. At Clarify, whoever picks the issue up adds the Plan, from the
     code as it stands then, and keeps it true until the issue closes. -->

## Goal

One or two sentences: the outcome, not the steps.

## Context

State of the **system**, not of the work — work state lives on the project board.
Why now, constraints, links to prior discussion or ADRs.

## Related

Issues, ADRs, PRs, external docs.

## Plan

Added at Clarify. A session that picks the issue up cold continues from this
section, its ticked boxes and the tree.

**Status:** proposed | accepted YYYY-MM-DD

### Behaviour

One scenario per line, from the side of whoever uses it, failure paths included:

- WHEN <situation> THEN <what they see or get, and for how long>
- Changes: <existing behaviour that changes, and what depends on it> | none
- Removes: <existing behaviour that goes, and what depends on it> | none

**Seams:** <every entry point the rule must hold at> | one surface

### Technical details

Concrete file paths as task-list checkboxes. The marker carries the change type;
the checkbox carries progress (GitHub renders it as a progress bar on the issue
list — never edit prose to track progress).

- [ ] `[NEW]` `packages/foo/bar.py` — what it does
- [ ] `[MODIFY]` `apps/api/handler.py` — what changes
- [ ] `[DELETE]` `packages/foo/legacy.py` — why it can go

### Decisions

The maintainer's choices: the one-way doors (user-visible behaviour, hard to
undo, scope or spend) and any reviewer finding the developer disputes, each as
**(A) / (B)** with a recommendation. The pick and its reason replace the
recommendation before implementation starts. Below them,
a `Taken:` line for each choice made without the maintainer, with its reason;
the maintainer may veto any of them.

- **D1.** <question> (A) <option> · (B) <option> — recommended A | picked A: <why>
- Taken: <what> — <why>

### Verification

How "done" is proven: a test per scenario that fails without the change, commands
to run, behaviour to observe.

### Departures

Where the work left the accepted plan, and why, written when it happens. | none
