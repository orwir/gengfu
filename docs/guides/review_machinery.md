# Review machinery

What every comprehensive review shares, whatever it reviews: how a review is tracked and run, the working document, how findings are labelled, how action items close, the private route, the close-out and the Decision register. Written for whoever runs a review and for the maintainer who reads its record. Each kind's guide defines only what differs — its stance, backbone, phases, verdict set and severity translation — plus its *At task time* questions, which a task's work round asks of one diff on the kind's behalf; a task's own plan and work rounds are `task_review.md`, not this.

## The five kinds

| Order | Kind   | Guide                | Asks                                                                             |
| ----- | ------ | -------------------- | -------------------------------------------------------------------------------- |
| 1     | IA     | `ia_review.md`       | Is the right information in the right place, for the decision its reader makes? |
| 2     | LLM    | `llm_review.md`      | What does the product tell its model, and how does it know the replies are good? |
| 3     | UX     | `ux_review.md`       | Do the journeys work?                                                            |
| 4     | Design | `design_review.md`   | Is the visual system coherent?                                                   |
| 5     | Code   | `codebase_review.md` | Does the code still hold its claims, and what can go?                            |

**Order.** Each kind runs on its own, but when several are due they run in the table's order, so each takes the ones before it as given. IA settles which screens, commands and messages exist, which UX walks and design styles. LLM settles what the model is told, so UX judges the replies people actually see. UX removes steps, settings and screens, so design systematizes what survives. The code review comes last: every other review's fixes land in code, and it reads the code that ships.

A review of any kind starts by reading the open work of the latest review of every kind (*Phase 0 reads*): a question parked in a UX review must be visible to the next code review, and the other way round.

## When a review is due

Each kind's guide names its triggers under *When to use*. They are checked, not remembered: at every release prep the project's release checklist goes through them, computing what can be computed — days and releases since the last review of each kind, growth since that review's baseline, bug issues by area since then — and names each trigger that fired. A fired trigger is a proposal to the maintainer, never a review started on its own; when several fire, they run in the order above.

## How a review runs

**A parent issue first.** Before Phase 0 the review gets a parent issue in the project's tracker, holding its goal, its scope and the working document's path. Its number heads the working document, and everything the review leaves to do hangs off it (*Action items*).

**A lead and phase agents.** The session that opens the review leads it: it writes the skeleton and Phase 0, keeps the plan file, and owns everything that spans phases. Each later phase runs as a phase agent — a subagent where the assistant can launch one, a fresh session otherwise — so no single context has to hold the whole review, and independent phases run at the same time.

1. **Skeleton** (the lead). Fill in the phase table (skip phases deliberately; customise the domain phases), take the Phase 0 baseline, write the header.
2. **Phases** (phase agents, in waves). A phase that reads another phase's output waits for it; the rest of its wave runs in parallel. Each kind's guide names its waves.
3. **Execution.** Fixes the maintainer authorizes land phase by phase (*Landing fixes*).
4. **Close-out** (the lead). Re-run the baseline, close every open item, write the ledger, the yield and the register, and freeze the document (*Close-out*).

**A phase agent's brief** holds everything it needs and nothing of the conversation: the plan file, the Phase 0 baseline, the project's contract docs, its phase's sub-checks, the questions in priority order, and a stop rule — report as soon as they are answered. It returns the phase's Checks-run block, findings and proposed action items in the per-phase format, and nothing else.

**What the lead keeps.** Phase agents analyse; the lead verifies and synthesizes. It spot-checks every load-bearing claim before the phase enters the plan, surfaces each phase to the maintainer, and writes what crosses phases: cross-phase blocks, decision shapes, the ledger and the register.

**A time box per wave.** Parallel agents spend in a burst, so the lead states how many agents and how long before launching a wave. An agent still running at its time box reports what it has, with the gaps marked unverified. A return too speculative or too broad to act on is not salvaged: rerun the phase with a tighter brief (named files, one question).

**Pickup.** A phase that runs in a later session starts from the plan file, the Phase 0 baseline and the contract docs — never from an earlier conversation. Findings and decisions go into the plan, not into the chat.

**Severe findings pause the review.** If an early phase finds architectural rot or a security exposure, surface it and pause: escalating is itself a valid output.

**Minimum viable review.** Each kind names its shortest loop. A deliberate short review, recorded as one, beats an abandoned full one, which catches nothing.

### Phase 0 reads

Every kind's Phase 0 reads, before anything changes:

- **The last review of its own kind:** its per-phase yield (*Close-out*), to choose where to read deep; and its parent issue's open children and unticked leftovers, which this review absorbs — taking each into its own parent, filing it, or dropping it with a reason — before closing the old parent.
- **The latest review of each other kind:** its parent's open children and leftovers, read so this review neither repeats nor contradicts them.
- **The register rows** of the latest review of each kind: its Open rows and its Accepted-risk rows whose trigger has not fired, copied into this plan's register with their origin. A fired trigger reopens its row, and an answer recorded on that review's parent issue since its close-out takes the place of the frozen row's status.
- **The project's contract docs**, plus whatever the kind's own ground-rules read names.

A review document from before this machinery has no parent issue: its open `[ ]` and `[DEFERRED]` items count as its parent's leftovers, and its yield is read from its findings tables.

## Landing fixes

A fix the maintainer authorizes during a review is that review's work and runs the project's task workflow (`task_review.md`): each phase's authorized fixes land as one hand-back, with its own work round and go-ahead, under the parent issue.

An item with one of the plan round's four shapes is not fixed inside the review; it becomes a child issue and gets its own plan round. The shapes: a rule that must hold at more than one entry point; persisted, billed, auth or safety state; a wait, retry or failure the user sees; a decision that wants an ADR.

## Working document

`docs/reviews/YYYY-MM-<kind>-review.md`, with the exact name its kind's guide gives, tracked: the permanent record of what was found, decided and shipped. It is written while the review runs and frozen at close-out — not edited afterwards, whatever lands later. The parent issue and its children carry what follows.

```
# {project} — {Kind} Review

Parent issue: #{P}
Goal: {one line}
Scope: {surfaces / source roots in}. Out: {what is deliberately not reviewed, and why}.
Phases skipped deliberately: {list, or "none"}.

**How to use this file**

- Phases run in the waves the kind's guide names; fixes land in phase order, so later
  phases can assume earlier ones landed.
- Each task starts with a 1–2 line **Why** and a **Scope** list.
- Each task is a prompt, not a design: its first deliverable is a short analysis
  (findings + suggested changes); it is implemented only once the maintainer authorizes.
- Broken state between tasks is fine; fix at checkpoints (end of phase or of session).
- Frozen at close-out; the parent issue carries what follows.
```

## Surface tags

One list for every kind. A tagged check applies only to that surface, untagged checks apply everywhere, and a tagged block the project lacks is recorded as *n/a*, never skipped silently.

| Tag             | Surface                                                     |
| --------------- | ----------------------------------------------------------- |
| `[web]`         | a browser-facing surface                                    |
| `[desktop]`     | a desktop GUI                                               |
| `[tui]`         | a terminal UI                                               |
| `[cli]`         | a command-line tool                                         |
| `[bot]`         | a chat-platform bot                                         |
| `[llm]`         | anything that sends text to a language model and acts on the reply |
| `[web-service]` | a deployed container or daemon                              |

## Findings

- **Spot-check agent claims before relaying them.** Subagents over-summarise — "X is only used in tests" when X is also imported elsewhere. One verifying check per load-bearing claim before it enters the findings table.
- **Delegate phases, keep the synthesis.** A phase agent writes its phase's findings; the lead verifies them and writes everything that spans phases (*How a review runs*).
- **Numbering.** A finding is numbered per phase (`P3 #5`). An action item gets a kebab-case slug, unique across the plan, that survives into commit messages and issue titles; it cites the findings it addresses (`Addresses P10 #3, #4`).
- **Severity is a commitment.** Each label changes the default action; pick deliberately.
- **Unrun is not passed.** A check that could not run (tool missing, credentials absent, service down) is recorded as *unrun* with its reason, never folded into a clean phase.
- **"What can go?"** Every phase closes with a written one-line answer: what did it reveal that can be deleted, collapsed or made cheaper? "Nothing" is a valid answer, but it is written. Where a finding admits an additive fix (wrap, guard, extend) and a subtractive one (delete, inline, collapse), the subtractive one is the default; an additive recommendation states its reason.

### Per-phase format

```
## Phase N — Topic

[Optional 1-2 line "Why"]

### N.1 Subsection name
- The check, scan or read, specific enough to pick up cold; the expected files and commands

#### Checks run (Phase N)
- One line per sub-check, ending in exactly one of: clean · finding → #k · unrun (why) · n/a.
  Clean is recorded, not omitted: this block tells the next reviewer the surface was walked.

#### Snapshot (Phase N)
[Optional — an inventory that is itself evidence: counts, decision tables]

#### Findings (Phase N)
| # | Finding | Location | Severity |
|---|---|---|---|
| 1 | One-sentence summary; a follow-up sentence if the row needs it. | `path:line` (link) | **Severity** |

#### Action items (Phase N)
- [ ] **`slug-name`** — Short title. Addresses #1. Concrete edits with paths; acceptance criteria when non-obvious.

What can go: {one line}
```

### Severity vocabulary

Exactly these labels; each carries a default action, and each kind's guide translates their meaning to its surface.

| Severity             | Meaning                                                    | Default action                                                         |
| -------------------- | ---------------------------------------------------------- | ---------------------------------------------------------------------- |
| **Real bug**         | Observable defect for users or operators                   | Fix this phase                                                         |
| **Drift**            | Out of sync with reality; not user-visible                 | Fix this phase if cheap; otherwise file                                |
| **Drift-prone**      | Will silently break in a known future scenario             | Document the trigger and the plan; fix when it fires                   |
| **Cleanup**          | Dead code or surface, stale comments, unused exports       | Fix this phase, mechanically                                           |
| **Minor**            | Inconsistency, style, a small gap                          | Fix only inside the immediate diff                                     |
| **Acceptable**       | Confirmed correct, or a deliberate trade-off               | Leave; note for the record                                             |
| **Decision pending** | Needs the maintainer's call (option A vs B vs C)           | Surface with a recommended option; add a register row; don't act alone |

Two items skip "fix this phase" whatever their label: one with a plan-round shape becomes a child issue (*Landing fixes*), and an exploitable one takes the private route.

An accepted risk is not an `Acceptable` finding: it is a register row with a revisit trigger, so the next review re-checks the trigger instead of re-finding the issue.

The labels are categories, not numbers. A ledger or a fix list sorts by label, then — where the kind records it — by how often the problem is met: a journey's frequency, a file's change rate, a request kind's share of traffic.

**Verdict sets.** An inventory phase gives each row a verdict — keep, merge, remove and the like — instead of a free-form finding. Each kind's verdict set maps every verdict to one of the labels above in a *Maps to* column, so a verdict row sorts and acts like any other finding.

## Action items

Each action item is a checkbox keyed by its slug. At close-out every one ends in exactly one of these states, and none stays open in the document:

```
- [ ] **`slug`** — Open, while the review runs.
- [x] **`slug` [DONE]** — What shipped. (commit `abc1234`)
- [x] **`slug` [FILED → #N]** — A child of the parent issue: job-sized, with its details.
- [x] **`slug` [LEFT IN #P]** — A small leftover, waiting on the parent's checklist.
- [x] **`slug` [REPORTED PRIVATELY]** — No location, no detail (*The private route*).
- [x] **`slug` [NOT TAKEN]** — One clause of why. An accepted risk is a register row instead.
```

**Children and leftovers.** Work that needs its own plan, schedule or session becomes a child issue of the parent — job-sized, so several related items ride in one child rather than one issue each. A small item nobody needs yet waits as a line on the parent's checklist and becomes a child, with its details, when someone takes it up. Children are sub-issues where the tracker has them, otherwise linked from the parent's body.

**The parent after close-out.** It leaves the tracker's in-progress state and holds only follow-through. It closes when its checklist is empty and its children are closed, or when the next review of the same kind absorbs what is left (*Phase 0 reads*). No leftover outlives the next review of its kind. A decision taken on an Open register row after close-out is recorded on the parent, with the row's slug and the pick, since the frozen document cannot take it.

**Commit references.** A commit body names the slug (`closes plan: handler-coverage`), so the plan, the history and the issues line up.

### Cross-phase blocks

When one finding blocks another, say so in the *blocking* phase's action items, not in the blocked one's findings:

> "Until `session-store-fix` lands, finding #2 (retry on stale session) cannot land cleanly — adding retries while the store returns stale rows just moves the bug."

### Decision shapes

When an action item has more than one reasonable path, present the paths as letters with a recommendation:

```
- [ ] **`dep-pinning-policy`** — Decide on dependency pinning policy. Addresses #4.
  - **(A) Pin majors** — one-time edit, no new tooling.
  - **(B) Lockfile** — reproducible, but ongoing bump churn.
  - **(C) Accept current looseness** — cheapest, a footgun.
  - **Recommended: (A).** Catches the real risk without the lockfile's upkeep.
```

The maintainer picks; the reviewer implements. Every letter-shaped item has a register row that links here by slug.

## The private route

An exploitable security finding never enters the working document, an issue or a close-out comment. It goes through the project's private disclosure channel — the one its security policy names — and the document records it as `[REPORTED PRIVATELY]`, with no location or detail, until the fix ships. A review document in a public repository is a public record, and so is every issue it files.

## Close-out

The last phase of every kind. Each kind adds its own steps before these (a re-walk, a final scan):

- **Delta table** against the Phase 0 baseline, every delta explained. The kind defines the metrics.
- **Simplification ledger.** Every removal and collapse from every phase in one ranked table: slug, what it removes, state. The top items not done this time are surfaced as decision shapes with register rows, so the maintainer leaves knowing the biggest simplifications even when none ship now. An empty ledger means the review didn't look; if that is truly the state of the surface, write the sentence saying so — never omit the section.
- **Every action item closed** in one of its states, with the parent issue's children filed and its checklist written.
- **Per-phase yield:** one line per phase with its findings by label and its unrun checks. The next review of the same kind reads it to choose where to read deep.
- **Task-time yield:** the findings one change introduced, counted by the *At task time* question that should have caught them in that change's work round, and the classes no question covers. A finding counts against a question only when the repository's wiring would have launched that question's lens for the change; one that never reached its lens is a wiring fix, not a new question. A class no question covers that this review and the last of its kind both met gets a question in the kind's *At task time* (*Updating a guide*). The next review reads the count against its own: a falling one means the work round is catching them.
- **Decision register**, filled as the document's last section.
- **Freeze.** The document is not edited after close-out.

## Decision register

The last section of every review document: one table, a snapshot at close-out. Every `Decision pending` finding and every letter-shaped action item has a row, and nothing else does. The register is an index: the options and the rationale stay in the phase that raised them, and the row says what was asked, what was chosen, and where that choice is recorded.

It is the one thing a frozen document hands on besides its parent issue: the next review copies its rows by reading it, without editing it.

| Slug                 | Question                                                   | Options → recommended                                         | Origin                      | Status                                                              | Recorded at |
| -------------------- | ---------------------------------------------------------- | ------------------------------------------------------------- | --------------------------- | ------------------------------------------------------------------- | ----------- |
| `dep-pinning-policy` | Pin majors, lockfile, or accept looseness?                 | (A) majors · (B) lockfile · (C) accept → **A**                | P5 #4                       | Decided (A) 2026-06-14                                              | ADR-0021    |
| `logout-post`        | Should logout stay a `GET`?                                | (A) POST + CSRF · (B) leave → **B**                           | P5 #6, carried from 2026-03 | Accepted risk — revisit when the session carries more than identity | this row    |
| `single-dispatcher`  | Keep one dispatcher when the second transport bypasses it? | (A) collapse · (B) second transport adopts it · (C) keep → **A** | P3 #4                    | Open                                                                | —           |

Status vocabulary — exactly these four:

- **Open** — surfaced, not yet chosen.
- **Decided (X) YYYY-MM-DD** — the maintainer picked. *Recorded at* names where the rationale lives: an ADR when the choice changes an architecture claim, the issue when it schedules work, *this row* otherwise. The register is never the only copy of a significant decision.
- **Accepted risk — revisit when <trigger>** — a deliberate "leave it" is a decision, not an `Acceptable` finding. The trigger (a release, an event, a date) is what the next review re-checks.
- **Dropped (why)** — the question stopped mattering; one clause of why, so it isn't raised again.

Rules:

- **Carry-forward.** The next review of any kind copies every Open row and every Accepted-risk row whose trigger has not fired (*Phase 0 reads*); a fired trigger reopens its row.
- **Third-review rule.** A row entering its third review is decided in that review's Phase 0 — by the maintainer, or by default to the recommendation. Three carries means the answer is the recommendation, or the question is dropped.
- **Close-out block.** No action item is `[DONE]` while an Open row it depends on is still Open; note the block in the action item (*Cross-phase blocks*).
- **Decision shapes stay put.** The `(A) / (B) / (C)` block lives in the phase's action items; the register links to it by slug and never repeats the options.

## Conventions checklist

Before a phase or the review closes:

- [ ] The header names the parent issue, the scope (in / out) and the phases deliberately skipped.
- [ ] Phase 0 absorbed the leftovers and open children of the last review of its kind, and closed that review's parent.
- [ ] Findings are table rows (# / Finding / Location / Severity), numbered per phase and labelled from the seven; every verdict row maps to a label.
- [ ] Every sub-check has a Checks-run line: clean, finding, unrun with its reason, or n/a.
- [ ] Every phase has its written "what can go?" line.
- [ ] Action items are checkboxes keyed by unique slugs; letter-shaped ones offer `(A) / (B) / (C)` with a recommendation and have a register row.
- [ ] Cross-phase blocks sit in the blocking phase.
- [ ] Subagent claims were spot-checked before entering the findings.
- [ ] At close-out every item is DONE, FILED, LEFT IN the parent, REPORTED PRIVATELY or NOT TAKEN; the ledger, the per-phase yield, the task-time yield and the register are written; nothing exploitable is in the document.
- [ ] File and line references are links (`[path:line](path#L123)`); line anchors are fine in a frozen record.

## Updating a guide

Two reviews in a row needing the same hand-edit means the guide is wrong: fix it in the kit, through the copy of the project that found it. The rule holds for this file and for every kind's guide.
