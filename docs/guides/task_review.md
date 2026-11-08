# Task review

The review round between a task's *Implement & stop* phase and its hand-back to the maintainer: the developer session hands a **hand-off packet** to a reviewer agent that has no author context, the reviewer reads the tree against it and answers with a **return packet**, and the developer acts on the return and renders the **hand-back brief**. The packets are data passed between the two agents and are never shown to the maintainer; the brief is the one thing the maintainer reads. Written for the two AI roles and for the maintainer who reads the brief.

It does not cover the maintainer's own read of the diff, the comprehensive passes (`codebase_review.md`, `ux_review.md`, `design_review.md`), or a release gate.

## When it runs

Every task gets one round unless it is an untracked micro-task, and then the brief says so. It runs after the local gate is green and before the hand-back, in both flows: the packet points at the uncommitted tree on `develop`, or at the branch's diff against `develop`. Nothing is pushed for it — the PR, when the work rides a branch, is opened at *Complete* as before.

A second round is bought only by a `Real bug` in the first. Anything still disputed after that goes to the maintainer, never to a third round between agents.

The round answers what the gate cannot. Lint, format, the suite and the coverage floor are settled by the push hook, so neither packet re-argues them; the reviewer's questions are the ones only a reader can answer — scope, decisions, invariants, tests, prose, docs, and what could go.

## The round

The developer session runs the whole round. The maintainer sees one line when the reviewer launches and the brief when the round ends; nothing else is printed. Which command starts the round and where the reviewer's instructions live is the repository's wiring, not this guide's.

1. **Hand off.** The developer renders the hand-off packet and launches the reviewer as a fresh agent whose task is the packet: a subagent where the assistant can launch one, otherwise a fresh session the packet is pasted into, with the return pasted back. The tree is the reviewer's until the return arrives: no edit in between.
2. **Review.** The reviewer reads the ticket and the tree, verifies every claim, and returns the return packet as its only output.
3. **Act.** The developer fixes every finding the return marks required and re-runs the gate. A `Real bug` in the return buys a second round: a reply per finding id — `fixed <how>` / `disputed <why>` / `deferred #N` — to the same reviewer, which keeps its context, and its second return is acted on the same way. Without one, no reply is written: the brief's *Round* line carries the tally.
4. **Hand back.** The developer renders the brief. The packets stay where the round produced them: the reviewer's task and its result.

## Hand-off packet

Read by the reviewer. Claims with coordinates, never the diff: the reviewer reads the tree itself, and a pasted diff is the author's framing of it. Self-contained — no "as discussed" — because the reader saw none of the discussion. Alternatives are separated by `|`.

```
Review request: #<N> <title> | untracked: <one line of intent>
Flow: direct on develop, uncommitted (base <sha>) | branch <name> | PR #<M>
Diff: git diff | git diff develop...HEAD | gh pr diff <M>
New files: <paths, which git diff does not show> | none
Ticket: gh issue view <N> | untracked
Read first: <the docs, ADR sections or guides the diff is held to, beyond the operating contract> | none

Summary: <what and why, one paragraph; what was removed, or why nothing could go>

Reconciliation (one line per ticket box):
- [MODIFY] <path> — landed | dropped: <why> | added beyond the ticket: <why>
- Deviation from the ticket's mechanism: <named> | none

Decisions taken alone: <options picked without the maintainer; hypotheses left untested and where they are marked; spend> | none

Where to look (2-3 hunks):
- <path:line> — relies on <invariant>; enforced by <test> | stated only

Verification: <N passed, coverage X%, lint clean>; fails without the change: <test ids>
NOT RUN: <what and why> | nothing
Docs: <files touched per the change → doc mapping>; CHANGELOG: "<the line>" | none, because <why>
```

## Return packet

Read by the developer. Every finding is a commitment: a location, a severity from the seven-label vocabulary in [codebase_review.md § Severity vocabulary](codebase_review.md#severity-vocabulary), evidence, and the action it requires. `Real bug` and `Drift` are required; `Cleanup` and `Minor` are fixed only inside the diff; `Acceptable` is recorded; `Decision pending` goes to the maintainer with a recommended option.

```
Verdict: approve | corrections | split: <what lands now, what is filed>

Findings:
| id | location    | severity | claim | evidence | required action |
| R1 | <path:line> | Real bug | ...   | ...      | ...             |

Verified clean: scope yes | decisions yes | invariants yes | tests yes | prose yes | docs yes | simplicity yes
  (a finding id in place of "yes" where it is not)
For the maintainer: <questions the reviewer could not settle; every Decision pending with a recommended option> | none
```

## Hand-back brief

What the maintainer reads, and the only thing they read: a reader who was at *Clarify* but saw nothing of the round, and who answers or vetoes from the brief alone. What must be answered is separated from what may be vetoed, every row stands on its own, and no row names a finding id — the packets are not in front of this reader, so a row says in words what it rests on. Rendered as chat markdown exactly as the template shows; alternatives are separated by `|`.

```
**Hand-back #<N>** <title> | **Hand-back** untracked: <one line of intent>
<approve | corrections | split: <what lands now, what is filed> | round skipped: untracked micro-task> · <k> round(s) · <develop, uncommitted on <sha> | branch <name> | PR #<M>> · gate <green | not run> · <n> decision(s) to answer | nothing to answer

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

- The first line is the task and the second its status; every lead is bold — *In the tree*, *Decisions pending* and *Taken alone* open a list on the next line, the rest carry their content on the same line; nothing before the first line, nothing after *Next*, and no headings, tables or fenced blocks inside.
- "I" is the developer, "you" the maintainer. A row that rests on the reviewer says so in words — "the reviewer asked", "disputed: the reviewer holds A, I hold B" — never by id.
- *In the tree* is the tree after the round's fixes: three bullets with those leads, one or two sentences each.
- A D-row is answerable from the brief alone: a question ending in "?", one bullet per option with its consequence, and a last bullet with the recommended letter, its reason, and what the tree does now with the size of each option. Every `Decision pending` of a return, every disputed finding (A the reviewer's position, B the developer's) and every *For the maintainer* question the developer could not settle is a D-row.
- *Taken alone* holds only choices the maintainer did not make at *Clarify* and might reverse: scope moved beyond or dropped from the ticket, a deviation from its mechanism, a filed follow-up by number and title, a hypothesis left untested and where it is marked, spend. One sentence each, what and why, at most five — a sixth is a D-row or the close-out comment. Not a row: a description of the implementation, a choice made at *Clarify*, a fix inside the ticket's scope (a count in *Round*).
- *Next* names the maintainer's action and only the non-routine *Complete* steps; the routine close-out is the operating contract's.
- Nothing in the brief that is not in a packet or the tree, and no new argument: the brief condenses, the packets prove.
- About twenty-five lines. Past that, the task wants a split, not a longer brief.

## Developer rules

- The packet is claims with coordinates. Never paste the diff or a file's contents; point at the command that shows them.
- Every section is filled or says `none`. An empty *Decisions* is a claim too, and the reviewer checks it.
- `NOT RUN` is honest: a gate that did not run in this session after the last edit runs before the packet or the brief is rendered, and what still could not run is listed with its reason.
- Do not pre-argue findings; the reviewer has not made any.
- The tree is the reviewer's while it reads: nothing is edited between launching the reviewer and reading its return.
- A disputed finding stays disputed into the brief. It is the maintainer's call, not a second argument to the reviewer.
- The packets are never printed for the maintainer: the reviewer is launched with the hand-off as its task, and its result is the return.

## Reviewer rules

- A fresh agent with no author context, read-only on files: the developer's session shares the tree. Commands that read are allowed — the diff, the history, the ticket, the PR, the linter and the formatter in check mode. Anything that rewrites the tree or its index is not: a formatter or fixer that writes, a stash, a checkout, any edit. The test suite is the developer's too: two coverage runs on one tree share the coverage files, and the second erases the first's number. The repository's reviewer instructions name its commands.
- The packet is the map; the tree is the territory. Read the whole diff by the command the packet gives and every new file it lists, in full. Verify every claim against them: read the test the packet says fails without the change, open the doc it says it updated, check the ticket box it says landed.
- Ask, in this order:
  1. **Scope** — does the diff do what the ticket asked, and only that? Drive-by changes are findings even when correct.
  2. **Decisions** — would the maintainer have made each one the packet lists, and each one it omits?
  3. **Invariants** — is every invariant the change relies on enforced by a test or a loader check, or only stated in prose?
  4. **Tests** — does each new behaviour have a test that fails without it, and is the database real where it could be?
  5. **Prose** — comments within budget, present tense, why not what, no tombstones or issue-number changelogs.
  6. **Docs** — the change → doc mapping applied, and the CHANGELOG line one claim, user-visible, under the cap.
  7. **Simplicity** — what could go, and does any abstraction the diff touches still earn its shape?
- Every finding cites `path:line` and says what breaks or which rule it breaks. A finding without evidence is a question, and goes under *For the maintainer*.
- Name what was verified clean, so silence is never ambiguous.
- Never fix, rewrite or propose a redesign; the round is a read. A simplification is a `Decision pending` with the smaller shape named.
- The return packet is the only output: nothing before it, nothing after it.
- A built-in bug finder, where the assistant has one, may run as a first pass; its findings enter the table like any other, with the reviewer's own evidence.

## Where the packets land

The packets are data between the two agents: they sit in the reviewer's task and its result, and are never printed for the maintainer or posted anywhere. The round is the team's own process, so nothing of it reaches GitHub, before the maintainer's approval or after. At *Complete*, a feature branch's PR body follows the repository's PR template, written for a reader who never saw the round: no packet field, finding id or reviewer verdict in it. What that reader needs from the round, such as a decision, a deviation or a follow-up, goes into the PR body or an issue in that reader's words. On `develop` the close-out comment may quote the brief's *In the tree* bullets.
