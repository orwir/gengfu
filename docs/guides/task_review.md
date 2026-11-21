# Task review

A task's two review rounds, each read by a reviewer agent that has no author context. The **plan round** runs at *Clarify*, before any code: the reviewer reads the plan in the issue against the code it will change, and the developer folds the findings into the plan and renders the **Clarify brief**. The **work round** runs between *Implement & stop* and the hand-back to the maintainer: the developer session hands a **hand-off packet** to the reviewers — a base reviewer, and a lens reviewer for each person- or model-facing surface the diff touches — each reads the tree against it and answers with a **return packet**, and the developer acts on the returns and renders the **hand-back brief**. The packets are data passed between the agents and are never shown to the maintainer; the briefs are the only things the maintainer reads. Written for the two AI roles and for the maintainer who reads the briefs.

It does not cover the maintainer's own read of the diff, the comprehensive passes (`codebase_review.md`, `ia_review.md`, `ux_review.md`, `design_review.md`, `llm_review.md`, all on `review_machinery.md`), or a release gate.

## When it runs

**The plan round** runs when the plan has one of four shapes, the ones in which plans have shipped holes: a rule that must hold at more than one entry point; persisted, billed, auth or safety state; a wait, retry or failure the user sees; a decision that wants an ADR. A plan with none of them skips the round, and the Clarify brief says so; the plan is still written into the issue. One plan round per plan; what stays disputed after it is the maintainer's decision.

**The work round** runs on every task after the local gate is green and before the hand-back, in both flows: the packet points at the uncommitted tree on `develop`, or at the branch's diff against `develop`. Nothing is pushed for it — the PR, when the work rides a branch, is opened at *Complete* as before. A second work round is bought only by a `Real bug` in the first, from the reviewer that found it. Anything still disputed after that goes to the maintainer, never to a third round between agents.

A task with no issue is untracked, and nothing of it goes on the board. An untracked micro-task — one obvious change, nothing to decide — has no plan and skips both rounds, and its hand-back brief says so. Any other untracked task skips the plan round, having no issue to hold a plan, and runs the work round with an `untracked:` packet. Its hand-back brief is its only record, so it finishes in the session it started; one that needs a second session, or makes a decision worth keeping, gets an issue then and is tracked from there: its plan written at *Clarify*, its item on the board.

The rounds answer what the gate cannot. Lint, format, the suite and the coverage floor are settled by the push hook, so no packet re-argues them; the reviewer's questions are the ones only a reader can answer — of a plan, what it leaves out before any code exists; of the tree, scope, decisions, invariants, tests, truth, docs and what could go, and what the comprehensive reviews would otherwise find in it later.

## The plan round

The developer session runs the whole round. The maintainer sees one line when the reviewer launches and the Clarify brief when the round ends; nothing else is printed.

1. **Plan.** The developer reads the ticket and the code the change touches, and writes the issue's *Plan* with `Status: proposed`. It goes into the issue before the round, so the reviewer reads what the maintainer will read, and a session that ends here leaves it behind.
2. **Hand off.** The developer launches the reviewer as a fresh agent whose task is the plan packet: a subagent where the assistant can launch one, otherwise a fresh session the packet is pasted into, with the return pasted back. The plan is the reviewer's until the return arrives: no edit in between.
3. **Review.** The reviewer reads the plan and the code, checks every fact the plan rests on, and returns the plan return as its only output.
4. **Act.** The developer folds every required finding into the plan. A finding the developer disputes becomes a D-row: the reviewer's position as A, the developer's as B.
5. **Accept.** The developer renders the Clarify brief. The maintainer answers its D-rows and says "go"; the developer writes each answer into the plan's *Decisions*, sets `Status: accepted <date>`, and only then writes code.

A skipped round runs steps 1 and 5. When it was skipped and the brief has nothing to answer, the plan is clear — the maintainer validated the task at triage — so no "go" is awaited: the developer sets the plan accepted and goes straight on.

## Plan packet

Read by the reviewer. Claims with coordinates, never the plan pasted: the reviewer reads the plan in the issue and the code beside it. Self-contained, like the hand-off packet. Alternatives are separated by `|`.

```
Plan review: #<N> <title>
Ticket: <the command or link that shows issue N>
Shape: <which of the four, and where the plan meets it>
Read first: <the docs, ADR sections or guides the plan is held to> | none
Rests on: <facts about the code the plan assumes, each with a coordinate — "<rule> is enforced only in <path:line>"> | none
Taken alone: <choices in the plan the maintainer has not made> | none
Open: <what the developer could not settle> | none
```

## Plan return

Read by the developer. The return packet's form, with the plan questions in *Verified clean*. Only findings that change the plan count: `Real bug` (the plan as written ships a defect), `Drift` (it contradicts the code, a doc or an ADR), `Decision pending` (the maintainer's call, with a recommended option). Wording and taste are not findings.

```
Verdict: approve | corrections | split: <what this issue keeps, what is filed>

Findings:
| id | location                   | severity | claim | evidence | required action |
| P1 | <plan section | path:line> | Real bug | ...   | ...      | ...             |

Verified clean: scale yes | seams yes | defaults yes | state yes | changes yes | decisions yes | reuse yes
  (a finding id in place of "yes" where it is not)
For the maintainer: <questions the reviewer could not settle; every Decision pending with a recommended option> | none
```

## Clarify brief

What the maintainer reads before any code, and the only thing of the plan round they read. It follows the hand-back brief's rules — every row stands on its own, no finding ids, rendered exactly as the template shows — and points at the plan instead of repeating it: the issue holds the plan in full, and the brief holds what must be answered and what may be vetoed.

```
**Plan #<N>** <title>
<the shape | no shape> · plan round <approve | corrections | split: <what this issue keeps, what is filed> | skipped> · <n> decision(s) to answer | nothing to answer

**Upgrade** <release before → after: what the kit changed; each change it required of this repository and what was done; the templates installed whole; what was ported into the repository's own files and what was left, with why> (only while the tree holds an upgrade not yet landed on the integration branch)

**The plan**
- Visible: <what a user or operator will see differently | nothing>
- Under it: <the approach, in a sentence or two>
- Seams: <where the rule holds | one surface>

**Scenarios to check** | none
- WHEN <situation> THEN <outcome>

**Decisions pending** (a letter each) | none
**D1.** <the question, ending in "?">
- A: <option>. <consequence>
- B: <option>. <consequence>
- Recommended A: <why>.

**Taken alone** (veto by naming the row) | none
- <what> — <why>.

**Round** <k> findings · <n> folded into the plan · <n> to answer (D…) · <D… disputed | none disputed> | skipped, no shape

**Next** <answer D1, then | > "go" | nothing to answer, no shape: going ahead.
```

- *Upgrade*, in both briefs, is there while the tree holds a move of the repository's copy of the method to a newer release that has not landed on the integration branch (`lifecycle upgraded`). How it lands is *Complete*'s rule.
- *Scenarios to check* holds the scenarios a reader might argue with — a failure path, a wait, a default — not all of them.
- *Taken alone* holds the plan's choices the maintainer did not ask for and might reverse: scope added to or dropped from the ticket, a mechanism the ticket did not name, a follow-up to file.
- About fifteen lines. Past twenty-five, the task wants a split.
- "go" accepts the plan and opens implementation, nothing more: landing still waits for *Complete*'s go-ahead.

## The work round

The developer session runs the whole round. The maintainer sees one line when the reviewers launch and the hand-back brief when the round ends; nothing else is printed. Which command starts either round and where the reviewer's instructions live is the repository's wiring, not this guide's.

1. **Hand off.** The developer renders the hand-off packet and launches the reviewers its lenses call for (*Lens reviewers*), each as a fresh agent whose task is the packet with its own *Lens* line: a subagent where the assistant can launch one, otherwise a fresh session the packet is pasted into, with the return pasted back. The tree is the reviewers' until every return arrives: no edit in between.
2. **Review.** Each reviewer reads the ticket and the tree, verifies the packet's claims — the base reviewer every claim, a lens reviewer those its questions rest on — asks its lens's questions, and returns the return packet as its only output.
3. **Act.** The developer fixes every finding the returns mark required — one defect two returns report is one finding — and re-runs the gate. A `Real bug` in a return buys that reviewer a second round: a reply per finding id — `fixed <how>` / `disputed <why>` / `deferred #N` — to the same reviewer, which keeps its context, and its second return is acted on the same way. Without one, no reply is written: the brief's *Round* line carries the tally.
4. **Hand back.** The developer renders the brief. The packets stay where the round produced them: each reviewer's task and its result.

### Lens reviewers

Every diff is read by the base reviewer. A diff that touches a surface one of the comprehensive reviews owns is also read by that surface's lens reviewer, which asks what those reviews would otherwise find months later, while the change is still in hand:

| Lens          | Launched when the diff touches                                                   | Asks                                                                          |
| ------------- | -------------------------------------------------------------------------------- | ----------------------------------------------------------------------------- |
| base          | anything                                                                         | the work-round questions (*Reviewer rules*), then `codebase_review.md` § At task time |
| person-facing | what a person reads or drives: `[web]` `[desktop]` `[tui]` `[cli]` `[bot]`       | § At task time of `ia_review.md`, `ux_review.md` and `design_review.md`       |
| model-facing  | what a language model reads, or the eval that scores its replies: `[llm]`        | `llm_review.md` § At task time                                                |

- The surface tags are `review_machinery.md`'s; which paths carry which tag is the repository's wiring.
- The reviewers read the same tree at the same time, each with the hand-off packet and its own *Lens* line, and each answers with its own return packet.
- A lens reviewer asks only its lens's questions, against the references they name — the intent doc's IA section, the design-language reference, the prompt documentation — and none of the base reviewer's, so no question is asked twice.

## Hand-off packet

Read by the reviewer. Claims with coordinates, never the diff: the reviewer reads the tree itself, and a pasted diff is the author's framing of it. Self-contained — no "as discussed" — because the reader saw none of the discussion. Alternatives are separated by `|`.

```
Review request: #<N> <title> | untracked: <one line of intent>
Lens: base | person-facing | model-facing
Flow: direct on develop, uncommitted (base <sha>) | branch <name> | PR #<M>
Diff: git diff | git diff develop...HEAD | <the command or link that shows PR M's diff>
New files: <paths, which git diff does not show> | none
Ticket: <the command or link that shows issue N> | untracked
Read first: <the docs, ADR sections or guides the diff is held to, beyond the operating contract> | none
Upgrade: <release before → after; the paths it wrote; each change the release required and where it was made; each port, with the template diff it follows (`git diff -- <guides>/<template>`)> | none

Summary: <what and why, one paragraph; what was removed, or why nothing could go>

Reconciliation (one line per box in the plan):
- [MODIFY] <path> — landed | dropped: <why> | added beyond the plan: <why>
- Departures from the accepted plan: <as recorded in the issue> | none

Scenarios (one line per scenario in the plan):
- WHEN <situation> — <test id> | not covered: <why>

Decisions taken alone: <options picked without the maintainer; hypotheses left untested and where they are marked; spend> | none

Where to look (2-3 hunks):
- <path:line> — relies on <invariant>; enforced by <test> | stated only

Verification: <N passed, coverage X%, lint clean>; fails without the change: <test ids>
NOT RUN: <what and why> | nothing
Docs: <files touched per the change → doc mapping>; CHANGELOG: "<the line>" | none, because <why>
```

## Return packet

Read by the developer. Every finding is a commitment: a location, a severity from the seven-label vocabulary in [review_machinery.md § Severity vocabulary](review_machinery.md#severity-vocabulary), evidence, and the action it requires. `Real bug` and `Drift` are required; `Cleanup` and `Minor` are fixed only inside the diff; `Acceptable` is recorded; `Decision pending` goes to the maintainer with a recommended option.

```
Verdict: approve | corrections | split: <what lands now, what is filed>

Findings:
| id | location    | severity | claim | evidence | required action |
| R1 | <path:line> | Real bug | ...   | ...      | ...             |

Verified clean: scope yes | decisions yes | invariants yes | tests yes | truth yes | docs yes | simplicity yes | code at task time yes
  (a finding id in place of "yes" where it is not; a lens reviewer names its own questions by their leads)
For the maintainer: <questions the reviewer could not settle; every Decision pending with a recommended option> | none
```

## Hand-back brief

What the maintainer reads at the hand-back, and the only thing they read of the work round: a reader who was at *Clarify* but saw nothing of the round, and who answers or vetoes from the brief alone. What must be answered is separated from what may be vetoed, every row stands on its own, and no row names a finding id — the packets are not in front of this reader, so a row says in words what it rests on. Rendered as chat markdown exactly as the template shows; alternatives are separated by `|`.

```
**Hand-back #<N>** <title> | **Hand-back** untracked: <one line of intent>
<approve | corrections | split: <what lands now, what is filed> | round skipped: untracked micro-task> · <k> round(s) · <base | base, person-facing | base, model-facing | base, person-facing, model-facing | no reviewer> · <develop, uncommitted on <sha> | branch <name> | PR #<M>> · gate <green | not run> · <n> decision(s) to answer | nothing to answer

**Upgrade** <release before → after: what the kit changed; each change it required of this repository and what was done; the templates installed whole; what was ported into the repository's own files and what was left, with why> (only while the tree holds an upgrade not yet landed on the integration branch)

**In the tree**
- Visible: <what a user or operator sees differently | nothing>
- Under it: <what changed beneath that>
- Removed: <what is gone | nothing>

**Decisions pending** (a letter each) | none
**D1.** <the question, ending in "?">
- A: <option>. <consequence>
- B: <option>. <consequence>
- Recommended A: <why>. Tree now: <untouched, and A is <size>, B is <size> | already A, and B is a <size> revert>.

**Taken alone** (veto by naming the row) | none
- <what> — <why>.
- Filed #<M> <title> — <what was found; why not here>.

**Verified** <N passed · coverage X% · lint clean · <n> tests fail without the change (<where checked>)>
**Not run** <what, why> | nothing
**Round** <k> findings · <n> fixed · <n> deferred (#…) · <n> recorded · <n> to answer (D…) · <D… disputed | none disputed> | skipped, untracked micro-task

**Next** <answer D1, then | > "lgtm". <non-routine Complete steps | Nothing unusual at Complete.>
```

- The status line's verdict is the strictest of the returns — split over corrections over approve — and its lenses are the reviewers that read the tree; *Round* counts the findings of every return.
- The first line is the task and the second its status; every lead is bold — *In the tree*, *Decisions pending* and *Taken alone* open a list on the next line, the rest carry their content on the same line; nothing before the first line, nothing after *Next*, and no headings, tables or fenced blocks inside.
- "I" is the developer, "you" the maintainer. A row that rests on the reviewer says so in words — "the reviewer asked", "disputed: the reviewer holds A, I hold B" — never by id.
- *In the tree* is the tree after the round's fixes: three bullets with those leads, one or two sentences each.
- A D-row is answerable from the brief alone: a question ending in "?", one bullet per option with its consequence, and a last bullet with the recommended letter, its reason, and what the tree does now with the size of each option. Every `Decision pending` of a return, every disputed finding (A the reviewer's position, B the developer's) and every *For the maintainer* question the developer could not settle is a D-row.
- *Taken alone* holds only choices the maintainer did not make at *Clarify* and might reverse: scope moved beyond or dropped from the accepted plan, a departure from it, a filed follow-up by number and title, a hypothesis left untested and where it is marked, spend. One sentence each, what and why, at most five — a sixth is a D-row or the close-out comment. Not a row: a description of the implementation, a choice made at *Clarify*, a fix inside the plan's scope (a count in *Round*).
- *Next* names the maintainer's action and only the non-routine *Complete* steps; the routine close-out is the operating contract's.
- Nothing in the brief that is not in a packet or the tree, and no new argument: the brief condenses, the packets prove.
- About twenty-five lines. Past that, the task wants a split, not a longer brief.

## Developer rules

- The packet is claims with coordinates. Never paste the diff or a file's contents; point at the command that shows them.
- Every section is filled or says `none`. An empty *Decisions* is a claim too, and the reviewer checks it.
- `NOT RUN` is honest: a gate that did not run in this session after the last edit runs before the packet or the brief is rendered, and what still could not run is listed with its reason.
- Do not pre-argue findings; the reviewer has not made any.
- The tree is the reviewers' while they read: nothing is edited between launching them and reading every return.
- A disputed finding stays disputed into the brief. It is the maintainer's call, not a second argument to the reviewer.
- The packets are never printed for the maintainer: each reviewer is launched with the hand-off as its task, and its result is its return.
- The plan is in the issue before the plan round, and the maintainer's answers are in it before any code: a session that picks the task up cold continues from the issue, its ticked boxes and the tree, never from a conversation it did not see.
- After acceptance the plan changes only by its *Departures*: what changed and why, written when it happens. A departure that is user-visible, hard to undo or changes scope stops for the maintainer first.

## Reviewer rules

- A fresh agent with no author context, read-only on files: the developer's session shares the tree. Commands that read are allowed — the diff, the history, the ticket, the PR, the linter and the formatter in check mode. Anything that rewrites the tree or its index is not: a formatter or fixer that writes, a stash, a checkout, any edit. The test suite is the developer's too: two coverage runs on one tree share the coverage files, and the second erases the first's number. The repository's reviewer instructions name its commands. In the plan round the issue is read-only too: the reviewer never edits the plan.
- In the work round the packet is the map; the tree is the territory. Read the whole diff by the command the packet gives and every new file it lists, in full. The base reviewer verifies every claim against them: reads the test the packet says fails without the change, opens the doc it says it updated, checks the ticket box it says landed. A lens reviewer verifies only the claims its own questions rest on.
- In the plan round the plan is the map and the code is the territory. Read the plan in full and every path it names, check each fact the packet says it rests on, and look for what the plan does not name: the rule's other callers, the other transports and write paths, whatever depends on the behaviour it changes.
- In the plan round, ask, in this order:
  1. **Scale** — does each scenario say what the user sees at real scale, including while it waits, retries or fails?
  2. **Seams** — does the plan name every entry point the rule must hold at — each transport, route, write path, job — and cover each, or reuse the one function that owns the rule?
  3. **Defaults** — what do absence, a cap, a truncation or an unknown value mean, and is that right for every case they cover?
  4. **State** — where does each piece of state come from, and when is it read or resolved?
  5. **Changes** — who depends on what the plan changes or removes, and is each one named?
  6. **Decisions** — is every choice the maintainer should make asked, with a recommended option, and none taken silently?
  7. **Reuse** — does existing code already do part of this, and is there a smaller shape?
- In the work round, the base reviewer asks, in this order:
  1. **Scope** — does the diff do what the accepted plan says, and only that, with every departure recorded under its *Departures*? Drive-by changes are findings even when correct.
  2. **Decisions** — would the maintainer have made each one the packet lists, and each one it omits?
  3. **Invariants** — is every invariant the change relies on enforced by a test or a loader check, or only stated in prose? Does each rule the diff adds hold at every sibling — the other entry point, writer, route, transport or reply site?
  4. **Tests** — does each scenario in the plan, and each other new behaviour, have a test that fails without it, the refused, failed, empty and capped paths included, each asserting its true reason? Can every new test fail, with no skip, glob or parametrize that can run empty? Is the database real where it could be?
  5. **Truth** — is every sentence the diff adds, or leaves beside what it changes, true of the code for each reader and state it reaches: copy, messages, help, model-facing text, docs, comments? Comments within budget, present tense, why not what, no tombstones or issue-number changelogs.
  6. **Docs** — the change → doc mapping applied, the CHANGELOG line one claim, user-visible, under the cap, and nothing outside the diff still naming what it removed or renamed.
  7. **Simplicity** — what could go, and does any abstraction the diff touches still earn its shape? Did the diff bolt a special case or a one-off flag onto a flow whose shape should change instead? Does a restructure delete concepts, or only move them?

  Then it asks the code review's *At task time* questions (`codebase_review.md`).
- An *Upgrade* in the packet is not scope drift: its guides, lock and installed templates are the kit's release, and the base reviewer reads each port against the template diff the packet names.
- A lens reviewer asks its lens's *At task time* questions (*Lens reviewers*), in the order each guide lists them, and none of the base reviewer's.
- Every finding cites `path:line`, or in the plan round the plan's section, and says what breaks or which rule it breaks. A finding without evidence is a question, and goes under *For the maintainer*.
- Name what was verified clean, so silence is never ambiguous.
- Never fix, rewrite or propose a redesign; the round is a read. A simplification is a `Decision pending` with the smaller shape named.
- The return packet is the only output: nothing before it, nothing after it.
- A built-in bug finder, where the assistant has one, may run as a first pass; its findings enter the table like any other, with the reviewer's own evidence.

## Where the packets land

The packets are data between the agents: they sit in each reviewer's task and its result, and are never printed for the maintainer or posted anywhere. The rounds are the team's own process, so nothing of them reaches GitHub, before the maintainer's approval or after — except the plan, which is not a packet: it lives in the issue, where the developer writes it and the maintainer accepts it, and a plan-round finding reaches GitHub only as the change it made to the plan. At *Complete*, a feature branch's PR body follows the repository's PR template, written for a reader who never saw the round: no packet field, finding id or reviewer verdict in it. What that reader needs from the round, such as a decision, a deviation or a follow-up, goes into the PR body or an issue in that reader's words. On `develop` the close-out comment may quote the brief's *In the tree* bullets.
