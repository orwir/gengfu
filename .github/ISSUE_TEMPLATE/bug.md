---
name: Bug report
about: Defect or regression — behavior differs from what is documented or expected
labels: ["type/bug"]
---

<!-- Installs to .github/ISSUE_TEMPLATE/bug.md. Keep prose tight; delete optional
     sections that don't apply. Note for agents: non-interactive
     `gh issue create --body` bypasses this template — read this file and
     reproduce its structure.
     Written in two stages. At capture: what was observed, Goal through Expected
     vs actual, plus Root cause when it is already known. At Clarify, whoever picks
     the issue up confirms the root cause, adds the Plan, and keeps it true until
     the issue closes. -->

## Goal

One sentence: what does "fixed" look like?

## Context

State of the **system**, not of the work — work state lives on the project board.
Version where it happens, environment, when it started, who or what is affected.

## Reproduction

Numbered steps from a clean state. Exact commands and inputs.

1. …
2. …

## Expected vs actual

- **Expected:** …
- **Actual:** … (paste the relevant log lines or output, trimmed)

## Root cause

Optional at capture, confirmed at Clarify. Name the file (`path/to/file.py`), the
invariant that breaks, and why.

## Plan

Added at Clarify. A session that picks the issue up cold continues from this
section, its ticked boxes and the tree.

**Status:** proposed | accepted YYYY-MM-DD

### Behaviour

- WHEN <the reproduction> THEN <the expected result> — fails today
- WHEN <a neighbouring case the fix must keep> THEN <what it does today>
- Changes: <existing behaviour the fix changes, and what depends on it> | none

**Seams:** <every entry point with the same defect, or the one function that owns the rule> | one surface

### Technical details

- [ ] `[MODIFY]` `path/to/file.py` — what changes

### Decisions

The fix options, each as **(A) / (B)** with a recommendation. The pick and its
reason replace the recommendation before implementation starts.

- **D1.** <question> (A) <fix> · (B) <fix> — recommended A | picked A: <why>

### Verification

The test that reproduces the bug and fails before the fix, the neighbours' tests,
the command to run, the observable behaviour that flips.

### Departures

Where the work left the accepted plan, and why, written when it happens. | none
