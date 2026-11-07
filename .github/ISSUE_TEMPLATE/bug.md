---
name: Bug report
about: Defect or regression — behavior differs from what is documented or expected
labels: ["type/bug"]
---

<!-- Installs to .github/ISSUE_TEMPLATE/bug.md. Keep prose tight; delete optional
     sections that don't apply. Note for agents: non-interactive
     `gh issue create --body` bypasses this template — read this file and
     reproduce its structure. -->

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

## Root cause (optional)

Fill in once known. Name the file (`path/to/file.py`), the invariant that breaks,
and why.

## Fix options (optional)

When more than one reasonable fix exists, list them as **(A) / (B) / (C)** with a
recommendation. The maintainer picks; don't act unilaterally on letter-shaped items.

## Verification

How the fix will be proven: the test(s) to add, the command to run, the observable
behavior that flips.
