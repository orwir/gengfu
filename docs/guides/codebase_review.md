# Codebase review template

When the user asks for a comprehensive codebase review (multi-phase walk through every layer to catch drift, dead code, over-engineering, and quiet bugs after a heavy build phase), follow this template. Optimised for: solo-maintained repos, work-as-you-go execution, plan kept in `docs/reviews/` as historical record with selective GitHub issues for cross-cutting follow-ups. The method is language-agnostic; the shell recipes are shown for a Python project, and [Project layout](#cross-phase-working-habits) says how to substitute your own toolchain.

## When to use this template

Run this template when accumulated drift is felt but hard to name — typically after a major release, quarterly, or at the close of a heavy build phase. Don't run it for:

- Hot-path bug fixes — too slow; do the fix first, file follow-ups for adjacent drift.
- Sub-week projects — the payback isn't there.
- Active feature work on the same surface — the codebase is moving; reviews need a stable target.
- Pre-launch hardening — that's a security review (Phase 5 alone), not a 13-phase walk.

**Minimum viable review.** When a full pass is too heavy right now, run Phase 0 → Phase 1 → Phase 3 → close-out and skip the rest deliberately: baseline, doc truth-up, the architecture/simplification audit, delta + ledger. That loop catches the highest-value drift; an abandoned full template catches nothing.

If an early phase produces severe findings (architectural rot, security exposure), surface them and pause — escalating those is itself a valid output of the review.

## Stance: Simplicity First

The review's bias is toward removal, not addition. Most of the value comes from finding code, abstractions, and machinery that pulled their weight under an earlier assumption that no longer holds — patterns chosen for the right reason at the time but kept past the moment that reason eroded. Long-lived projects accumulate this residue because each individual erosion (an ADR revision, a follow-up PR, a scope cut) was too small to justify restructuring on its own.

Concrete stance the reviewer holds throughout:

- **For every load-bearing abstraction, the question is "does the surviving justification still demand this shape?"** Not "is the code clean?" Not "is the test coverage high?" If the shape outlives the rationale, the shape is residue regardless of how clean it reads. The [3.W Load-bearing assumption audit](#3w-load-bearing-assumption-audit) is the dedicated check.
- **Default to the smaller code that solves the surviving problem.** A cache that exists because a decorator fired N times per request stops being justified when the underlying check fires once. A ContextVar that propagates data across layers stops being justified when the data can be passed explicitly. Surface the simplification with a decision-shape item; the maintainer chooses the new boundary.
- **Coupled residue lands together.** If removing the cache forces removing the decorator, treat them as one action item, not two — the split is the wrong axis. A half-removal often re-introduces the original perf or correctness problem the residue was masking.
- **Watch for "no abstractions for single-use code."** Generic decorators, protocols, factories that have exactly one caller and one implementation are residue from a planned-but-cancelled second user. Either ship the second user or collapse the abstraction.

**Simplification proposals are a first-class deliverable, not a by-product.** Accumulation is the default direction of drift — every feature adds, almost nothing removes — and the bill lands on performance, maintainability, and everything built on top. The review is the scheduled counterweight: every phase is expected to surface removal and collapse candidates alongside its findings, the close-out aggregates them into a ranked ledger (12.4), and a review whose output is purely additive (more tests, more docs, more checks) almost certainly didn't look hard enough.

This stance is operationalised in the phase sub-checks and the cross-phase habits below; the reviewer doesn't need to remember to apply it ad hoc.

## How this template runs

A 13-phase review is not a single sitting. Run it in passes — each phase is its own context-bounded unit, picked up cold from the plan file when you start it.

1. **Pass 1 — Skeleton.** One session. Fill in the phase table (skip irrelevant phases; customise the domain phases for the project), capture Phase 0 baseline metrics, write the file header. Don't try to write sub-check detail for phases beyond 0 yet — the template's sub-checks and starter lists are enough; project-specific domain-phase detail gets written when those phases are picked up, not now.
2. **Pass 2 — Per-phase analysis.** One session per phase (or per tight cluster). Pick up the plan, re-read it end-to-end, run the phase's sub-checks, produce findings and proposed action items, and surface them to the user. Once the user authorizes, execute the cheap action items in the same session and defer the rest to a follow-up session.
3. **Pass 3 — Execution and revisits.** Action items often span sessions; the lifecycle markers (`Open` / `Done` / `Deferred → #issue`) keep the plan honest across resumptions.
4. **Pass 4 — Close-out.** The final phase (12 in the default layout). Re-run the baseline, populate the delta table, compile the simplification ledger, fill the [Decision register](#decision-register), file issues for the issue-worthy deferred items and leave the rest `[DEFERRED]` for the next review. The plan file stays in `docs/reviews/` permanently — don't delete it (see [Phase 12.5](#closing-out-phase-12)).

**Per-phase context discipline.** When you pick up a phase you should be able to start from: the plan file + the Phase 0 baseline + the project's contract docs. Don't depend on prior-session conversation memory — write findings and decisions into the plan, not into the chat.

**Recovering from a noisy phase.** Subagents sometimes return reports too speculative or too broad to act on. When that happens, restart the phase with tighter scope (specific files, specific question) rather than salvaging a noisy report. The plan stays; only the in-progress findings get redone.

## Working document

Create `docs/reviews/YYYY-MM-codebase-review.md` (or `YYYY-Q#`; e.g. `docs/reviews/2026-06-codebase-review.md`). **Tracked** — the plan is a permanent historical record, not a throwaway. Past review docs are how [Phase 0.2](#02-ground-rules-read) of the *next* review picks up unresolved items.

The plan is a *living* document — the user will return between phases to surface findings, pick options, and authorise commits. Update findings and action items inline as work lands; the [Phase 12.2](#closing-out-phase-12) baseline-vs-final delta table lives inside the plan, not as a separate artifact.

## File header

```
# {project} — Comprehensive Review Plan

Goal: {one line — usually "catch problems early after a heavy build phase, no overengineering"}
Scope: {surfaces / source roots in}. Out: {what is deliberately not reviewed, and why}.
Phases skipped deliberately: {list, or "none"}.

**How to use this file**

- Work top-down. Tasks are intentionally small and ordered so later phases assume earlier ones landed.
- Each task starts with a 1–2 line **Why** and a **Scope** bullet list.
- Each task is a *prompt*, not a finished design. When you pick a task up, the first deliverable is a short analysis (findings + suggested changes); only then implement.
- Broken state between tasks is fine. Failing tests are fine. Fix at meaningful checkpoints (end of phase or end of session).
- This file lives in `docs/reviews/` permanently — historical record of what was found and what shipped. Don't delete on close-out.
```

## Default phase structure

The 13 phases below cover most full-stack services. Skip irrelevant phases and add domain-specific ones (e.g. ML pipeline review, frontend audit) as needed. Number contiguously.

| Phase | Topic                                               | Why                                                                                                              |
| ----- | --------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------- |
| 0     | Baseline & ground rules                             | Establish what "done" looks like before changing anything                                                        |
| 1     | Documentation truth-up                              | Fix doc drift first so claims are anchors during code work                                                       |
| 2     | Repository hygiene                                  | Cheap mechanical cleanups (artifacts, tombstones, **comment & docstring drift**) so later phases see real signal |
| 3     | Architecture & layering                             | Validate boundaries hold; produce a concept landscape map; audit pattern duplication and ADR supersession        |
| 4     | Configuration                                       | Env vars / settings / defaults                                                                                   |
| 5     | Security                                            | SQL surface, web endpoints, auth, secrets, deps                                                                  |
| 6     | Database & persistence                              | Schema, repo invariants, retention, migrations                                                                   |
| 7..N  | Domain-specific phases (one per critical subsystem) | One key module at a time — see "Domain phase count" note below                                                   |
| N+1   | Errors & observability                              | Exception hygiene, structured logs, health                                                                       |
| N+2   | Tests & coverage                                    | Coverage gaps, mirror discipline, slow tests                                                                     |
| N+3   | Operations & deployment                             | Docker, CI, backups, release                                                                                     |
| N+4   | Final passes                                        | Self-enforcing rules, doc sweep, close out                                                                       |

**Domain phase count.** The 7..N row expands to as many phases as the project has critical subsystems — typically 2–4. Two slots is fine for a small service; a richer one usually needs 3–4 layers:

- a *foundation* layer (external integrations, providers);
- an *orchestration* layer (pipelines, dispatchers);
- a *cross-cutting gate* (validation, safety, authz);
- a *settlement* layer (anything with strict bookkeeping — billing, accounting, state machines).

Translate the labels to your domain: a service with billing might hit all four; a screen-capture utility might have only *capture* and *render*; a CLI tool might collapse to a single *commands* phase. What matters is that each layer with its own correctness logic gets a focused phase — when in doubt, give an orchestration layer or a strict-liability surface its own phase rather than folding it into a neighbour. Order domain phases by execution flow (foundation → orchestration → gate → settlement) so each phase's findings are concrete by the time the dependent phase is picked up. The checks every domain phase runs first are under [Starter sub-checks](#starter-sub-checks--the-remaining-phases).

**Phase numbers in this template.** The sub-check sections below carry the numbers of the default layout — exactly two domain phases, so security = 5, tests = 10, close-out = 12. If your table has more or fewer domain phases, renumber on use; the topics are canonical, not the numbers. Cross-references in the prose (12.2, 12.4, Phase 10) use the default numbers too — read them as *close-out § delta table*, *close-out § ledger*, *tests*.

### Cross-phase working habits

Rules that apply throughout, not in any one phase.

**Subagents.**

- **Spot-check agent claims before relaying.** Subagents over-summarise — they'll confidently say "X is only used in tests" when X is also imported by `composition.py`. One verifying `grep` per load-bearing claim before it goes into the findings table.
- **Delegate large reads, not synthesis.** Send subagents to read a long architecture doc or scan a directory; come back to your own context to write findings.

**Findings discipline.**

- **Don't recommend refactors *because* two files share a prefix or a word.** Architecture-driven layering produces false positives; verify before recommending.
- **Findings get numbered per phase; action items get slugs.** A finding is `P3 #5` (phase + number — number resets each phase). An action item is a kebab-case slug like `` `handler-coverage` `` that is unique across the whole plan, memorable in conversation, greppable, and survives into commit messages and (when relevant) issue titles. Action items cite which findings they address (`Addresses P10 #3, #4`).
- **Severity is a commitment.** Each label changes the default action — pick deliberately, don't default to `Drift` for everything.
- **Unrun is not passed.** A check that couldn't run this session (tool missing, credentials absent, service down) goes into the phase's Checks-run block as *unrun* with the reason — never rolled into a clean phase. "Not checked" and "checked, clean" look identical in a findings table; the Checks-run block is what tells them apart.

**Simplification.**

- **Every phase closes with "what can go?"** Before marking a phase done, write a one-line answer: what did this phase reveal that can be deleted, collapsed, or made cheaper (dead config, single-caller abstractions, per-call work that could be per-process, N+1 queries, redundant test layers)? "Nothing" is a valid answer — but it must be written. Skipping the question is how bloat survives reviews.
- **Prefer the diff that removes.** When a finding admits both an additive fix (wrap, guard, extend) and a subtractive one (delete, inline, collapse), recommend the subtractive shape by default; an additive recommendation needs a stated reason.

**Pace.**

- **First deliverable per task is analysis, not code.** Surface findings + suggested changes; commit only after the user authorises.
- **Broken state between phases is fine.** Failing tests are acceptable mid-phase; fix at phase or session boundaries.

**Project layout.**

- **Substitute `packages apps` with your project's source roots** throughout the grep / find / coverage recipes below. The template uses `packages apps` as a stand-in (a common Python monorepo layout); your project may use `src/`, `lib/`, `internal/`, `cmd/`, or a single nested package. Decide the substitution once at the start of Phase 0 and apply it consistently — the commands won't run as-written on a layout that doesn't have a top-level `packages/`.
- **Substitute the toolchain the same way.** The recipes name `ruff`, `pytest`, `MagicMock` and `*.py` because they are shown for a Python project. Read them as *the linter*, *the test runner*, *the mocking library* and *the source glob*, and swap in your stack's at the same moment. What each recipe measures is the point; the command is one way to get the number.

### Phase 0 sub-checks — baseline & ground rules

Capture the starting state mechanically and read the project's contract documents end-to-end. Phase 12.2 compares against the metrics captured here, so this is load-bearing — don't skip.

#### 0.1 Baseline scan

Run and record results in the plan (Python toolchain shown; substitute yours). These become the "Baseline" column of the Phase 12.2 delta table.

```
# Lint
ruff check . | tail -5
ruff format --check . | tail -5

# Tests
pytest --collect-only -q | tail -3        # test count
pytest --durations=10                     # wall time + slowest
pytest --cov --cov-report=term | tail -5  # coverage

# Repo size
git ls-files | wc -l
find packages apps -name "*.py" | xargs wc -l | tail -1

# Top-10 largest source files — bloat radar, feeds Phase 3
find packages apps -name "*.py" -printf "%s %p\n" | sort -rn | head -10
```

Capture the numbers, not the full output. Standard table:

| Metric                               | Baseline ({date}) |
| ------------------------------------ | ----------------- |
| Lint issues                          | 0                 |
| Tests passed                         | 1369              |
| Coverage                             | 86%               |
| Wall time                            | 40s               |
| Source lines (`packages/` + `apps/`) | 21,514            |
| Direct dependencies (manifest)       | 14                |

Add repo-specific rows (open issues, project-board state, deploy version) when they're useful evidence later.

#### 0.2 Ground-rules read

Read these end-to-end before changing anything:

- `AGENTS.md` / `CONTRIBUTING.md` — the operating contract. A `CLAUDE.md` or `CLAUDE.local.md` that doesn't import `@AGENTS.md` is a finding: it hides `AGENTS.md` from Claude Code.
- `README.md` — what the project claims to do.
- `docs/architecture/` (or equivalent) — load-bearing diagrams and decision records.
- `docs/reviews/` (or equivalent) — past review docs. Read the latest review *of each kind* (code, UX, design): carry its unresolved `[ ]` action items in as starting findings, and every Open or untriggered Accepted-risk row of its [Decision register](#decision-register) into this plan's register, origin pointing back.

The output of 0.2 is *internal alignment*, not a written artifact. If a rule surprises you, that's a finding for Phase 1 (docs truth-up).

### Phase 1 sub-checks — documentation truth-up

Fix doc drift first so claims are anchors during the rest of the review. Code can lie quietly; docs lie loudly. Walk the documentation surface and compare each claim against the code.

- **README claims vs `--help`.** Every command, flag, and example in the README should match the actual CLI / API. Run the examples.
- **Env vars: `.env.example` vs source.** Grep the codebase for env reads; every key that influences behavior should be in `.env.example` with a one-line comment.

```
grep -rEn "os\.environ|getenv|os\.getenv" packages apps --include="*.py"
```

- **Version string single-source.** Grep for the version string; it should appear in exactly one place. Every reader (deploy workflow, health endpoint, Docker tags) should read from there, not duplicate.
- **CHANGELOG vs git tags.** Every released tag should have a CHANGELOG section; every CHANGELOG section above the unreleased line should have a tag.
- **ADR status accuracy.** Each ADR's Status (Proposed / Accepted / Superseded / Deprecated) should match reality. A "Proposed" ADR for a decision shipped six months ago is drift.
- **Link rot.** Resolve every cross-link in `docs/`. Broken anchors are silent failures.
- **Module / class docstrings claiming "this calls X" or "used by Y."** Drift candidates — spot-check the high-traffic modules.
- **Frozen records are exempt.** `docs/reviews/*` and `docs/product/idea.md` are point-in-time; don't flag them for drift against current code.

Severity is usually `Drift`. A wrong env-var name in the README, a `--help` example that crashes, or a CHANGELOG that omits a breaking change escalates to `Real bug`.

#### Phase 1.B — how the docs are *written*, not whether they are true

Truth-up asks whether a claim matches the code. This asks whether a reader can find it. A doc can be perfectly accurate and still cost every reader a re-read, and that failure is invisible to every check above. Run it as a second sweep over the same files:

- **Orientation.** Does each doc open with what it covers, who it is for, and what it excludes — *before* the first `##`? A doc that opens `# Title` → subtitle → `---` → `## 1. …` has told the reader nothing. This is the highest-value fix in the phase and the cheapest: one paragraph per file.
- **Buried conclusions.** Sample paragraphs and check whether the point is in the first sentence or the last. Reasoning-then-verdict reads fine to the author, who already knows the verdict.
- **Paragraph density.** Flag paragraphs past ~700 characters; count them per file. Two shapes dominate — how-to steps that grew into essays, and table cells holding paragraphs. The awk below counts a whole table or list as one paragraph — skip those hits; eight short bullets are not an essay.
- **List duplication.** Any list that appears in two documents. Diff them; the copies will already have drifted, and the drift is the finding.
- **Name and cross-reference consistency.** Count the variants (`ADR-0007` vs `ADR 0007`, an old product name vs the current one). Both forms being live means neither reads as intentional, and it costs a model a retrieval match.
- **Narrative accretion.** The [Phase 2](#phase-2-sub-checks--comment--docstring-hygiene) disease in prose form: docs that grew one paragraph per issue until they read as a lab notebook. Same test — is a past-tense sentence recording what was tried, or stating what holds?

Mechanical counts to start from (adjust the doc root):

```
# paragraphs over ~700 chars, worst files first
awk 'BEGIN{RS="";FS="\n"} {n=length($0); if (n>700) print n"\t"FILENAME}' \
    $(git ls-files "docs/**.md" "*.md") | sort -rn | head -20

# cross-reference form split
grep -rhoE "ADR[- ]0[0-9]{3}" --include="*.md" . | grep -oE "ADR." | sort | uniq -c
```

Severity is `Minor` for a single dense paragraph and `Drift` for a duplicated list or a name the project has outgrown. Report the paragraph-over-700 count as a before/after number — like prose share in Phase 2, it is the one writing metric that moves visibly.

### Phase 2 sub-checks — comment & docstring hygiene

Most projects have rules like "comments explain *why*, not *what*" and "no PR-history references in code." These rules rot quickly because nobody runs a linter for them. Bundle the audit into Phase 2 alongside artifact and tombstone cleanups:

- **WHAT-comments** — comments that restate what the next line does. Delete; the well-named identifier carries the information.
- **Historical references** — "added for the X flow", "removed in #Y", "used by Z", "fixes issue #123". Belong in PR descriptions and commit messages; in code they rot as the codebase evolves.
- **Stale claims** — comment says one thing, code does another. Either the comment lies and gets deleted, or the code is wrong and needs fixing. Flag both directions.
- **Multi-paragraph docstrings on small functions** — usually copy-pasted boilerplate. One line is almost always enough.
- **Workaround comments without a "Why:"** — `# hack to avoid X` is not enough. Add the bug / constraint that forces the workaround so a future reader can decide whether it still applies.
- **Module docstrings carrying rationale that belongs in an ADR** — long "we chose X because Y" prose at the top of a module should live in a numbered ADR; the module docstring points at it.
- **Oversized comments — the one that hides from every other check.** A "why, not what" rule bounds the *kind* of comment and never its *volume*, so a file accretes one legitimate rationale paragraph per issue that touches it and no individual line ever looks wrong. Measure **prose share per file** (comment + doc-comment lines ÷ non-blank lines) and **the longest run of consecutive comment lines**. The content is rarely wrong — it is discovery narrative (runs, variants tried, regressions, dates, issue-by-issue history) that should move to the ADR or the issue and be cited in one line. Derive the caps before applying them, per the procedure below.

#### Deriving the comment-length budget

The budget belongs in `AGENTS.md` as three concrete numbers, because a rule an assistant can't check mechanically is a rule it will argue with. But the numbers are a property of the codebase, not of good taste — derive them once, record them, and re-derive when the codebase changes shape:

1. **Extract every comment block, doc comment, and file header with its line count.** Use the language's own parser or tokenizer where there is one; a scan for the comment prefix is adequate otherwise. Count doc comments separately from ordinary comment blocks — a doc comment is also an API contract, so it legitimately runs longer.

   Classify by *role*, not by syntax: a run of Python `#:` lines, a JSDoc block above a constant, or a Rust `///` run are all doc comments even though they use comment markers, and counting them against the inline-comment budget flags documentation for being documentation.

   **Measure prose only:** stop counting at the first structured section header (`Args:`, `Returns:`, `@param`, `# Arguments`, `<param>`). A 34-line doc comment that is two lines of prose and 31 of parameter entries is documentation working as intended, and a checker that flags it will be ignored — along with everything true it also reports.

2. **Histogram the lengths and find where the cumulative curve flattens** — usually somewhere in the 90th–95th percentile.
3. **Read a sample from just above and just below that line.** The cap belongs where the *character* of the content changes, not at a round number: constraints and API contracts below it, narrative above. If the items above still read as contracts, move it up; if the ones below are already essays, move it down. This step is the one that matters — the percentile only finds the candidate.
   **Sample the boundary, not the extreme tail.** The 200-line monsters are obviously narrative and prove nothing about where the line goes; reading them instead of the items a few lines over the candidate is how a cap ends up far too tight, mandating cuts to documentation that was working. If a cleanup pass starts feeling like line-shaving with no reader benefit, the cap is wrong — stop and re-derive rather than grinding through it.
4. **Write the three numbers into `AGENTS.md`** with the date and the sample size, so the next audit can tell genuine drift from a re-derivation.

A worked example, from the repo this check came from: 93% of comment blocks fit in 8 lines and 97% of doc comments in 20, so those became the caps while module headers — which orient a whole file and legitimately run longer — took 25. Two earlier attempts were wrong in both directions: a single 8 for everything flagged 30% of doc comments, most of them ordinary API documentation, and a doc cap of 15 set by reading only the 60-to-200-line outliers flagged another 100 items that turned out to be clean contract prose. **Numbers that transfer between repos are a coincidence; the procedure is what transfers.**

Grep starting points (Python, adjust for the language):

```
# WHAT-comments — short single-line comments preceding obvious code
grep -rEn "^\s*# [a-z]" packages/ apps/ --include="*.py"

# Historical / authorial references
grep -rEn "added (for|in)|removed in|used by|see issue|fix(es|ed) (#|issue)|introduced (by|in)" packages/ apps/ --include="*.py"

# TODO / FIXME / XXX accumulation — should be near zero, otherwise they belong in issues
grep -rEn "(TODO|FIXME|XXX)\b" packages/ apps/ --include="*.py"

# Oversized blocks — runs of N+ consecutive comment lines, longest first.
# Substitute the language's line-comment prefix for `#` (`//`, `--`, `;`, `%`)
# and the glob for the language's extension. Block comments (/* … */) and doc
# comments need the parser route from the derivation procedure above.
awk -v n=8 '/^[[:space:]]*#/ {if(!c)s=FNR; c++; next}
            {if(c>=n) print c"\t"FILENAME":"s; c=0}' $(git ls-files "*.py") | sort -rn
```

**Do not try to detect oversized comments by wording.** Measured on a repo with the problem, keyword greps for history markers ("used to", "was tried", "regressed") matched 7% of the bloated lines and missed 84% entirely — hard-wrapped prose scatters its own tells across fragments, so no per-line pattern sees the paragraph. Length is the only signal that survives wrapping, which is also why the prevention rule is stated as a line budget rather than a style guideline.

Surface findings in the standard table; severity is usually `Cleanup` (delete) or `Drift` (stale claim). Bulk fixes land as one commit per file or per concern, not one per comment. Report prose share as a before/after number — it is the one comment metric that moves visibly.

### Phase 3 sub-checks — concept landscape, assumption residue, pattern duplication, ADR supersession

After a heavy build phase the riskier questions aren't just "do the boundaries hold" but "did newer patterns supersede older ones, leaving the older ones as dead weight?" and "is each load-bearing abstraction still shaped by an assumption that still holds?" Bundle four sub-checks into Phase 3, in this order: the landscape map produced by 3.X is the input for 3.W (load-bearing assumption audit), 3.Y (pattern duplication), and 3.Z (ADR supersession).

#### 3.X Concept landscape map (do this first)

Produce a one-page concept map *before* probing for issues. It makes the rest of Phase 3 — and the later domain-phase audits — much faster, because you know what's load-bearing and what's incidental.

Delegate the architecture doc to a read-only subagent (it's usually large; reading it in main context is wasteful). Ask for:

- Subsystems with a one-line purpose
- Cross-cutting patterns: handlers, registries, factories, decorators, value objects, services
- Layering/boundaries (apps/ vs packages/, transport vs logic) and how they wire together
- Configuration surface (env vars, tier config, feature gating)
- Data flow for the canonical commands end-to-end
- Storage model (tables, UoW, encryption boundaries)
- **Load-bearing abstractions** — the 8–12 names touched by many files (handler protocol, error hierarchy, request context, single-chokepoint dispatcher, etc.)

Output: a short markdown block at the top of Phase 3 with file:line refs throughout. Keep it for the rest of the review.

#### 3.W Load-bearing assumption audit

Most architecture decay isn't a literal duplicate file or a forgotten ADR — it's an implementation pattern that *made sense under assumption A*, the team incrementally changed assumption A through PRs and ADR revisions, and the original implementation shape stayed put because each individual revision was too small to justify restructuring on its own. The residue accumulates as cache machinery, ContextVar plumbing, decorator hooks, or per-call hot paths that no longer pull their weight. It passes tests, it doesn't show up as pattern duplication, and the ADR's Status line still says *Accepted* — only the revisions tell you the original justification eroded.

This is the canonical "Simplicity First" check: for every load-bearing abstraction in the 3.X concept map, ask *what assumption forces this to be shaped the way it is?* Then check whether that assumption still holds.

For each load-bearing name in 3.X:

- **State the original justification in one sentence.** Read the ADR(s), the docstring, and the commit that introduced the pattern. If you cannot state the justification in one sentence, the abstraction itself is unclear — flag and skip the rest of the check on it.
- **Walk the ADR revisions and notable follow-up PRs.** A long Revisions trailer is a strong signal: every revision narrowed, removed, or substituted some part of the original. The justification is the *intersection* of what survived.
- **Compare current shape against current justification.** If the shape is *strictly larger* than what the surviving justification demands — a per-call decorator when the check fires once per request, a request-scoped cache when only one verdict can occur per request, a ContextVar when the data can be passed directly — that excess is residue.
- **Categorize each excess.** Adapt the pattern-duplication verdicts to this lens:
  * *Load-bearing* — the shape really is justified by something the surviving rationale needs. Note it and move on; don't refactor for taste.
  * *Defense-in-depth* — broader than strictly needed but cheap and guarding a real failure mode. Acceptable; document the failure mode in a comment if the docstring doesn't.
  * *Residue from superseded assumption* — the shape outlived its reason. Severity: `Drift` (architectural). Action: surface the simplification with a decision-shape item so the maintainer picks the new boundary; don't unilaterally rewrite a load-bearing abstraction.
  * *Coupled residue* — the residue can't be peeled off without restructuring (e.g. the cache and the decorator are bound together — removing the cache re-introduces the perf problem the cache was fixing). Treat as one action item, not two; the split is the wrong axis.

A standing example (hypothetical — translate the shape to your domain): an authorization decorator was wired around every storage call because the original design wanted to authorize each access independently. Later revisions narrowed the requirement to validating the caller once per request and added a per-request cache to keep the decorator from re-checking across calls. The surviving justification was "authorize the caller once per request", but the implementation was still "decorator at every call + cache". The audit said: decorator + cache are coupled residue; the new shape is "single gate called once at request entry."

Do NOT pattern-match on superficial cues (long file, many imports, deeply nested logic). The signal is the *delta* between what the rationale needs now and what the implementation does. A short, simple file can still be residue; a long, complex one can still be load-bearing.

Findings shape: name the abstraction, quote the original justification, quote the surviving justification, name the excess. One row per abstraction; most rows resolve to *Load-bearing*.

#### 3.Y Pattern duplication audit

Enumerate file-naming smells from the project tree, then probe each in parallel — one read-only subagent per smell. This catches superseded patterns that still linger.

Smell candidates worth probing (Python names shown; the shapes transfer):

- `<name>.py` next to `multi_<name>.py` or `<name>_v2.py`
- `core/<topic>.py` next to a `<topic>/` package
- Multiple files sharing a prefix (`i18n_*`, `report_*`, `auth_*`) — old+new of one concept, or N distinct surfaces?
- Empty `__init__.py` next to siblings that export real symbols
- Local artifacts (`.mypy_cache/`, `build/`, `dist/`) for tools the project removed but the artifact survived
- Modules mentioned only in older docs / older ADRs

For each smell, the agent's job is: read top-of-file docstrings, count external imports per candidate (`grep` the module path), check tests, look at git log when creation order matters. Ask for a one-line verdict per file from this set:

| Verdict                    | Maps to severity | Action                                                                        |
| -------------------------- | ---------------- | ----------------------------------------------------------------------------- |
| **Live, distinct concern** | Acceptable       | Don't touch; cite the ADR or layering rule that justifies the split           |
| **Stale leftover**         | Cleanup          | Delete; confirm no live importers                                             |
| **Thin shim**              | Cleanup or Minor | Fold if the win is real (small file, single internal user); not urgent        |
| **Transitional**           | Drift            | Old + new both live — decide which way to finish                              |
| **False alarm**            | Acceptable       | Same name, different concern. Note it so the next reviewer doesn't re-flag it |

Most smells in a mature codebase resolve to *Live, distinct concern* — say so explicitly in the findings table so the user knows the pattern is intentionally not refactored.

#### 3.Z ADR supersession spot-check

Any ADR whose Status line says "Supersedes X in `file.py`" is a testable claim. Grep for X. Two silent failure modes that both pass tests:

- **Replacement landed, old path forgotten** — the new code works, the old code is still there, both run. Severity: `Drift`. Action: delete the old path.
- **Old path renamed, logic survived** — the symbol is gone but the equivalent code lives under a new name. Severity: `Drift` or `Real bug` if the survival contradicts the ADR's intent.

Walk every ADR with a Supersedes claim and grep the named symbols. This is the single highest-value sub-check in Phase 3 — drift here is invisible to tests and CI.

#### Phase 3 working habits

In addition to the [cross-phase working habits](#cross-phase-working-habits) above:

- **Don't merge classes whose split encapsulates per-instance state** (counter, lock, cache) just because one is only used by the other. The **file split** may be redundant; the **class split** usually isn't. A "primitive + router" pair where the primitive owns mutable per-instance state is almost always two classes pulling their weight — collapse the file, keep the classes.

### Phase 4 sub-checks — configuration

Config is a bloat magnet: keys outlive the features that read them, and "flexibility" settings spend years pinned to one value. The lens here is shrink-first.

- **Dead keys.** Grep every key in `.env.example` and the settings module against actual reads. Defined-but-unread → delete the key, the doc line, and the plumbing (`Cleanup`).
- **One-position switches.** Feature gates and mode flags that have held a single value since introduction: hardcode the value, delete the branch (`Drift` — the flexibility assumption eroded; cite git log as evidence).
- **Default truth.** The default in code vs `.env.example` vs README — one value stated in three places is two bugs waiting (single-source check).
- **Config-driven indirection with one configuration.** A setting that parameterizes behavior for exactly one caller with exactly one value ever used is the config flavor of a single-use abstraction — collapse it.
- **Validation at the boundary.** Required config should fail fast at startup with the missing key's name; a service that boots half-configured and dies on first use is `Drift-prone`.

### Phase 5 sub-checks — security

The smell isn't subtle bugs; it's *quiet* exposure — secrets that leak in logs, an endpoint that skips a check, a dependency whose fix the pins forbid. The phase's own failure mode is quiet too: a surface that was never walked reads exactly like one walked and found clean. So two rules bind harder here than elsewhere. Every sub-check ends **clean / finding / unrun (why) / n/a** in the Checks-run block. And a deliberate "leave it" is never an `Acceptable` finding — it is a [Decision register](#decision-register) row with a revisit trigger.

Surface tags as in `ux_review.md`: `[web]` a browser-facing surface, `[bot]` a chat-platform bot, `[llm]` anything that sends text to a model, `[web-service]` a deployed container or daemon. Untagged checks apply everywhere; a tagged block the project lacks is recorded as *n/a*, not skipped silently.

Start by re-verifying the kickstart's security baseline (`project_kickstart.md` § Security baseline): `SECURITY.md`, the dependency-update bot config, secret scanning at both layers, least-privilege workflows, the rotation runbook, static analysis where available. Kickstart wires them once; the review is where they get re-checked.

#### 5.1 Secrets

- **Scan the current state, not just diffs.**

```
gitleaks detect --source . --no-banner
```

- **Logging hygiene.** Grep log calls for sensitive names — credentials *and* personal data:

```
grep -rEn "log\.(info|debug|warning|error).*\b(token|secret|key|password|api_key|pepper|salt|email|ip|content|body)\b" packages apps
```

Log identifiers (key fingerprint, provider name, record id), not values. Message content is data, not a log field.

- **`.env.example` cleanliness.** No real values, no copy-pasted production keys.
- **Rotation runbook.** One exists for every long-lived credential, and it still matches the code that reads the credential.

#### 5.2 Dependencies

- **Known advisories.** Run the project's dep-audit tool (`pip-audit`, `npm audit`, `cargo audit`).
- **The advisory path is a process, not a snapshot.** Where does the *next* advisory land — CI, the pre-push hook, a bot PR? And can the fix ship: a version ceiling that excludes the patched release turns the audit into a notification. Check the ceilings on crypto / auth / web deps against their latest release.
- **Security-sensitive deps declare what they need.** A session or crypto library that works only because another dependency happens to pull its backend in (a missing `[secure]`-style extra) is `Drift-prone`: the coincidence ends on the next diet pass.
- **Pinning policy.** Majors pinned on security-sensitive deps (auth, crypto, web frameworks)?
- **Transitive surface.** Spot-check the deepest deps; supply-chain risk lives there.
- **Dependency diet.** One line per direct dependency — what it does, and whether the stdlib or a smaller library now covers it. A dep with one call site (or none) is supply-chain surface and upgrade churn paid for nothing; collapse or drop. Feeds the 12.4 ledger.

#### 5.3 Auth and authz

- **Every entry point enumerated** — routes, commands, handlers, scheduled jobs — with its auth requirement next to it. Per-endpoint `if user.is_admin` is a smell; one gate at the entry is healthier.
- **Object-level authorization.** Every handler that takes an id resolves it against the caller's grants; the id in the URL, the command argument, or the payload is never trusted on its own. This is the check IDOR hides from.
- **Destructive actions guarded server-side.** Purge, ban, delete, key removal: the arm / confirm step is verified by the handler, not only rendered by the UI. A confirmation the client never sends is a purge with no guard.
- **Privilege-boundary tests.** One asserts a non-admin *cannot* reach an admin path; one asserts user A cannot read user B's object. Absent → follow-up.

#### 5.4 `[web]` Sessions, CSRF, and the browser

- **Cookie flags** — `Secure`, `HttpOnly`, `SameSite`, a bounded max-age — on *every* cookie the app sets, not just the session.
- **CSRF on every unsafe method**, and no state change behind a safe one (a `GET` logout is a state change). The exemption list matches the mounted paths exactly — an exemption for a path that isn't mounted is a dead path today and a hole after the next refactor.
- **OAuth / SSO callback.** Fresh `state` per attempt, compared timing-safe, consumed once; the code exchanged server-side; the upstream token kept no longer than needed and never logged.
- **Open redirects.** Any `next` / `return_to` parameter is validated against an allowlist.
- **Output escaping.** Autoescape on; count the `| safe` (or equivalent) sites and justify each.
- **Error responses don't enumerate.** A resource the caller may not see answers the same whether it exists or not.

#### 5.5 Boundary and data surface

- **SQL injection.** Grep for string-formatted SQL; should be zero outside constant fragments.

```
grep -rEn 'f".*(SELECT|UPDATE|INSERT|DELETE)|format.*(SELECT|UPDATE|INSERT|DELETE)' packages apps
```

- **Input validation at the boundary, including size.** A length cap on every free-text field that reaches storage, a model, or a log. Validate at the edge; trust internal invariants.
- **Reverse-proxy trust.** The app believes `X-Forwarded-*` from exactly one hop, or from none. Secure cookies, client IP, and every per-IP limit depend on this line.
- **Unauthenticated entry points inventory** — webhooks, callbacks, health, public forms. Each has signature verification where a signer exists, an idempotency / replay guard, and a rate or size limit. `/health` says the service is healthy, not how it is built.
- **Timing-safe comparison** for every secret, signature, and token compare.
- **Encryption boundaries.** Where does data become plaintext, where does it stop? Every transition is a checkpoint. Backups included: what is in them (ciphertext, with the key elsewhere) and who can read them.

#### 5.6 Multi-tenant isolation

- **Every query on a tenant-scoped table carries the tenant predicate** — grep the writes as well as the reads. One test proves tenant A's id cannot read tenant B's row.
- **Background jobs and operator surfaces** run without a request context and are where the predicate goes missing. Walk them separately.

Single-tenant project: record *n/a* and move on.

#### 5.7 `[llm]` Model and tool surface

- **Untrusted content is framed as data.** Fetched messages, documents, search results, and tool output ride the prompt as *content*, never as instructions — and the framing is enforced at the seam in code, not only asserted in the prompt text.
- **Tool authority.** A tool call carries the *caller's* authority, never the bot's or the operator's; cross-channel, cross-tenant, and cross-user reads are gated at the tool boundary. A tool that takes a model-supplied id applies 5.3's object-level check.
- **Model output is untrusted on the way out.** Rendered into markdown / HTML / embeds → escaped; passed to a tool or a query → validated like user input; never executed.
- **Cost amplification is a denial of service.** Per-request caps on tokens, tool calls, and loop iterations; a timeout on every provider call; the cap fails closed.
- **Customer-supplied provider keys** are used only for their owner's calls and never appear in a shared cache key, a log line, or an error message.

#### 5.8 Privacy and data lifecycle

- **Personal-data inventory.** Which tables, logs, exports, and backups hold it — a short table in the plan.
- **Erasure and export walked end-to-end** against a real database; retention carve-outs match the stated policy, and the policy is written somewhere a user can read it.
- **Every audit record that is written has a read surface.** A compliance record nobody can consult is a liability, not a control.

#### 5.9 Workflows, CI, and the runtime

- **Least-privilege `permissions:` blocks** on every workflow file. No inheriting repo defaults.
- **Secret usage in CI.** Secrets used only in trusted contexts, never on PRs from forks.
- `[web-service]` **Image:** pinned base, non-root user, no secrets baked in, scanned if the registry offers it.
- `[bot]` **Least privilege on the platform:** privileged intents / scopes / permissions are the minimum the shipped features need; the invite link asks for nothing more.

Severity vocabulary: `Real bug` for an actively exploitable issue; `Drift-prone` for "this will become exploitable when X happens"; `Cleanup` for stale config. A deliberate leave-it is an **Accepted risk** row in the Decision register with its trigger — not an `Acceptable` finding to be re-found next review.

### Phase 10 sub-checks — test suite health

A passing suite with adequate coverage is necessary but not sufficient. Tests accumulate redundancy invisibly: framework-behavior tests, layered duplication, mock-heavy fixtures that brittle on every refactor. Phase 10 audits the suite as a system, not as a coverage number. Run sub-checks A→E in order; A frames the rest.

#### 10.A Scale & runtime snapshot

Mechanical counts that frame every later finding. Python toolchain shown, here and through 10.E; substitute yours.

```
# Test count (collection only — fast)
pytest --collect-only -q | tail -5

# Test functions defined; gap to collected count = parametrize fan-out
grep -rEn "^\s*(async )?def test_" tests/ --include="*.py" | wc -l

# Test:source line ratio
find tests -name "*.py" | xargs wc -l | tail -1
find packages apps -name "*.py" | xargs wc -l | tail -1

# Top-10 largest test files
find tests -name "*.py" -printf "%s %p\n" | sort -rn | head -10

# Wall time + slowest tests
pytest --durations=20
```

Starting heuristics, not derived caps — calibrate per repo the way the comment budget is: test:source 0.8–1.5x for logic-heavy code; parametrize fan-out under ~20% of total count; no single test file over ~800 lines without a clear reason.

#### 10.B Coverage reality check (low *and* high)

```
pytest --cov=packages --cov=apps --cov-report=term-missing
```

Both tails matter:

- **Low coverage** is the known smell — usually clusters around transport (HTTP handlers, bot command handlers, CLI entry points). Severity is usually `Drift-prone` or `Real bug`.
- **Suspiciously high coverage** is the inverse smell. A small module at 100% often has tests of framework behavior or trivial getters. Spot-check the last few covered lines for assertion value.
- **Recently refactored modules at low coverage** are regression windows. Cross-reference `git log --since="30 days ago" --name-only` with the coverage report.

#### 10.C Redundancy & bloat signals

The highest line-count payoff of the phase. Each is a separate probe.

- **Layered duplication.** Same code path tested in handler + pipeline + rendering + transport adapter + e2e? The e2e is usually load-bearing; middle layers often re-assert the same thing with different mocks. Per layer, ask: *what does this assert that the layer above/below doesn't?* Verdicts: *Unique responsibility* (keep), *Overlapping with layer X* (trim to the unique cases), *Redundant* (delete).
- **Mock-heavy fixtures.** A `MagicMock`-spawning helper driving N tests becomes brittle on every schema change.

```
grep -rEn "MagicMock\(" tests/ --include="*.py" | cut -d: -f1 | sort | uniq -c | sort -rn | head -10
```

Candidates to replace with typed builders or `SimpleNamespace`.

- **Trivial-assertion tests.** Tests that round-trip a getter, peek at private attributes (`obj._registry`), or assert framework validation. They lock implementation, not behavior.
- **Excessive parametrize fan-out.** A single test with 50 cases on trivial variations. Healthy fan-outs sit under ~20 cases.

```
grep -rEn "@pytest.mark.parametrize" tests/ --include="*.py" -A1
```

- **Snapshot / golden tests with no semantic value.** Tests that lock current output without claiming it's correct. Rename or replace with property-based assertions.

#### 10.D Quality & rule signals

- **Skip / xfail debt.** Each `pytest.mark.skip`, `xfail`, `skipif` is a TODO that pytest hides. Healthy projects have near-zero.

```
grep -rEn "pytest\.mark\.(skip|xfail|skipif)" tests/ --include="*.py"
```

- **Smoke-only tests.** Test functions whose body has no `assert`. Either add a real assertion or delete.
- **Project-rule violations.** Most projects have written or unwritten test rules — audit each one explicitly. Examples:
  * "Don't mock the database in integration tests" → grep `MagicMock` / `mock.patch` inside integration test paths.
  * "No `print()` in tests" → `grep -rEn "print\(" tests/`.
  * "No external network in unit tests" → grep `requests.`, `httpx.`, `aiohttp.` outside fixtures.
- **Slowest tests.** From `pytest --durations=20`. A subprocess-heavy test at 1.5s is fine; a logic test at 0.5s usually has a heavy fixture or a real `sleep`.

#### 10.E Mirror discipline

If the project rule is `tests/` mirrors source 1:1:

```
# Sketch — adapt for the actual layout
comm -23 <(find packages apps -name "*.py" | sed 's|^|tests/|' | sort) \
         <(find tests -name "*.py" | sed 's|test_||' | sort)
```

A naive mirror script over-flags because nested packages can collapse into a single test file (e.g. `packages/db/sqlite/messages.py` covered by `tests/packages/db/test_messages.py`). Pass the script output through a manual false-positive sweep, then surface only the real failure modes:

- **Source file with no test, and no test inheritance from a higher-level file.** Real gap.
- **Test file with no source file.** Stale test (source moved/renamed without updating the test path) or coverage of removed code.

#### Phase 10 working habits

- Don't blanket-prune. Each test removal needs a one-line "what does this assert that another test already covers" justification.
- Trim *layered redundancy* before *trivial assertions* — the line-count payoff is bigger.
- Coverage drops on prune are expected; coverage drops on *new code* are the real signal. Track separately in the Phase 12.2 delta table.
- A passing suite with high coverage doesn't disprove bloat; that's exactly when bloat is hardest to spot.

### Starter sub-checks — the remaining phases

Phases 6, 7..N, N+1 and N+3 don't get full sections; write their detail at pickup, seeded from the lists below and the 3.X landscape map.

**Phase 6 — database & persistence:**

- Schema vs models drift; unused tables and columns (grep for *writes*, not just reads).
- Migrations replay cleanly on an empty database — the only test of migration health that counts.
- Retention / cleanup jobs actually firing — check row timestamps, not the code.
- Query count per canonical command: N+1s and per-call queries that could be batched or cached are simultaneously a perf finding and a simplification finding.

**Phases 7..N — domain phases.** Each subsystem writes its own checks at pickup; these apply to every one of them and go first:

- **Boundary contract.** What enters, what leaves, who validates. Validate at the edge, trust internal invariants — a re-validation deep inside is either dead or hiding a hole at the edge.
- **Every external call** has a timeout, a bounded retry, and an idempotency story; anything retried is safe to repeat.
- **Authority is decided once** at the entry and travels with the request. Anything downstream that re-derives it from ambient state (a global, a ContextVar, a cache) is a 3.W candidate and a 5.3 candidate at once.
- **Cost per canonical command** — external calls, tokens, queries, per-call work that could be per-process. A number per command; feeds the 12.4 ledger.
- **Failure modes** — dependency down, slow, partial. Fail-open vs fail-closed is a written decision (a register row or an ADR), never an accident of exception handling.
- **Runtime hygiene** (async / concurrent code) — no blocking I/O on the event loop, no unawaited tasks, shutdown cancels what it started, shared mutable state has an owner.
- **The seam a test can hold** — one fake per external dependency at the boundary; a mock forest means the boundary is in the wrong place (feeds 10.C).

**Phase N+1 — errors & observability:**

- Bare and over-broad `except` sweeps; swallowed exceptions.
- Exception hierarchy still single-rooted, and every subclass actually raised somewhere — unraised subclasses are dead code.
- Log levels honest (errors that are warnings, info that is debug noise); re-run the 5.1 secrets grep over code added since.
- `/health` truthful: it should fail when the service is unhealthy, not merely report that the process exists.

**Phase N+3 — operations & deployment:**

- Dockerfile against the kickstart essentials: pinned base, deps layer-cached from the lockfile, non-root, `HEALTHCHECK`.
- CI workflows vs local hooks still running identical commands — the local==CI promise rots quietly.
- Backup cron firing *and the restore tested* — a job that runs is not evidence the backup works.
- Compose resource limits still sane against current memory reality.

## Per-phase format

```
## Phase N — Topic

[Optional 1-2 line "Why" if not obvious from the title]

### N.1 Subsection name
- Bullet describing the check / scan / read-end-to-end
- Specific enough that an assistant can pick it up cold
- Cite expected files / commands when known

### N.2 Subsection name
- ...

#### Checks run (Phase N)
- One line per sub-check, ending in exactly one of: clean · finding → #k ·
  unrun (why) · n/a. Clean surfaces are recorded here, not omitted — this
  block is how the next reviewer knows the surface was walked.

#### Snapshot (Phase N)
[Optional — current-state inventory: line counts, decision tables, env-var inventories.
Only include when the snapshot is itself useful evidence for findings.]

#### Findings (Phase N)

| # | Finding | Location | Severity |
|---|---|---|---|
| 1 | One-sentence summary. Follow-up sentence with detail if the row needs it. | `path/to/file.py:123` (markdown link) | **Severity** |
| 2 | ... | ... | ... |

#### Action items (Phase N)

- [ ] **`slug-name`** — Short title. Addresses #1. Concrete edits with file paths. Acceptance criteria when non-obvious.
- [ ] **`other-slug`** — Short title. Addresses #2 + #3 (one action collapses multiple findings).
- [ ] **`stale-comment-pass` [DEFERRED]** — Out of scope this review; carry to next.
```

### Severity vocabulary

Use exactly these labels — each carries an expectation:

| Severity             | Meaning                                                                                      | Default action                                                              |
| -------------------- | -------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------- |
| **Real bug**         | Observable defect for users or operators                                                     | Fix this phase                                                              |
| **Drift**            | Code / docs out of sync with reality; not user-visible                                       | Fix this phase if cheap; otherwise file follow-up                           |
| **Drift-prone**      | Pattern that will silently break in a known-future scenario (e.g. when a second transport lands) | Document the trigger + plan; fix when activated                             |
| **Cleanup**          | Dead code, stale comments, unused exports                                                    | Fix this phase, mechanical                                                  |
| **Minor**            | Inconsistency, style, doc gap, log-level mismatch                                            | Fix only if it's in the immediate diff                                      |
| **Acceptable**       | Confirmed correct or appropriate trade-off                                                   | Leave; note for the record                                                  |
| **Decision pending** | Needs maintainer's call (option A vs B vs C)                                                 | Surface with recommended option + rationale; add a register row; don't act unilaterally |

An accepted *risk* is not an `Acceptable` finding — it is a [Decision register](#decision-register) row with a revisit trigger, so the next review re-checks the trigger instead of re-finding the issue.

### Action-item lifecycle

Each action item is a checkbox line keyed by a unique kebab-case slug. The slug is the action item's *name* — it stays the same as the item moves through Open / Done / Deferred and is the way you refer to it in conversation, commit messages, and (when issued) issue titles.

```
# Open
- [ ] **`handler-coverage`** — Improve transport-handler test coverage. Addresses P10 #3, #4.
      Files / changes / tests.

# Done — append a one-liner about what shipped + commit ref
- [x] **`handler-coverage` [DONE]** — Improve transport-handler test coverage. (commit `abc1234`)

# Deferred — issue-worthy (see threshold below). Filed as a tracking issue.
- [x] **`handler-coverage` [DEFERRED → #166]** — Filed with `type/refactor`, `area/api`, `priority/tier-2`.

# Deferred — stays in this doc, picked up at next review
- [ ] **`docstring-spotcheck` [DEFERRED]** — Skim high-traffic module docstrings for "used by X" / "calls Y" claims; not urgent. Carry to next review.
```

**When does an action item become a GitHub issue?** Only when at least one is true:

1. **Real bug** that affects users or operators — needs visibility outside the review.
2. **Deferred work that needs scheduling** — milestone, external blocker, or dependency on another team / release.
3. **Cross-cutting work** that won't fit in one review-driven PR and has its own multi-session lifecycle.

Otherwise the action stays in the doc. Trivial cleanups, single-PR refactors, and small follow-ups are *not* issue-worthy — issuing them inflates the tracker without adding signal. The next review's [Phase 0.2](#02-ground-rules-read) opens by skimming this doc for open `[ ]` items.

**Commit references.** Use the slug in commit message bodies: `closes plan: handler-coverage`. The plan, the git log, and (when it exists) the issue all line up.

### Cross-phase blocks

When one finding blocks another, note it inline rather than relying on the reader to spot it:

> "Until `session-store-fix` lands, finding #2 (retry on stale session) cannot land cleanly — adding retries while the store returns stale rows just moves the bug, doesn't fix it."

The blocker note belongs in the *Action items* section of the blocking phase, not buried in Findings.

### Decision shapes

When an action item has more than one reasonable execution path, present them as letters with a recommendation:

```
- [ ] **`dep-pinning-policy`** — Decide on dependency pinning policy. Addresses #4. Three reasonable shapes:
  - **(A) Pin majors** — `httpx>=0.27,<1.0`. One-time edit, no new tooling.
  - **(B) Lockfile via `pip-compile`** — reproducible, but ongoing bump churn.
  - **(C) Accept current looseness with rationale** — cheapest, footgun.
  - **Recommended: (A).** Catches the actual risk (surprise major bump at next build) without the lockfile maintenance burden.
```

The user picks; you implement. Don't act unilaterally on letter-shaped items. Every letter-shaped item has a [Decision register](#decision-register) row that links here by slug — the options prose lives here, not in the row.

## Closing out (Phase 12)

```
### 12.1 Self-enforcing rules
- Lock invariants noticed during the review as tests so they don't rot.
  Standard set: layer isolation, cross-transport import bans, single
  version-string source, naming conventions, etc.
- Keep each check tiny — one assertion. If a check would be flaky,
  prefer a doc note.

### 12.2 Final scan re-run
- Re-run Phase 0.1 commands. Compare against baseline in a delta table:

| Metric | Baseline ({date}) | Final ({date}) | Δ |
|---|---|---|---|
| Lint issues | 0 | 0 | — |
| Tests passed | 1369 | 1421 | +52 |
| Coverage | 86% | 84% | −2pt |
| Wall time | 40s | 36s | −4s |
| Source lines | 21,514 | 20,818 | −696 |
| Direct dependencies | 14 | 12 | −2 |

Explain the deltas — coverage drops on real new code at <100% are
expected and worth noting, not hiding. For a maturing codebase, negative
source-line and dependency deltas are a success signal, not an anomaly:
the removal bias should be visible right here.

### 12.3 Doc + CHANGELOG sweep
- For every commit landed during the review, confirm the relevant doc
  was updated in the same commit.
- One CHANGELOG line per user-visible change. Pure refactors don't
  touch CHANGELOG.
- Skim every file the review touched for stale comments and
  docstrings — especially comments that referenced "the new X" or
  "as of #Y" before the review and now describe a state that has
  moved on.

### 12.4 Simplification ledger
- Collect every removal / collapse action item from all phases into one
  ranked table: slug, what it removes (lines / dep / abstraction / config
  keys / test layers), status (Done / Deferred).
- Surface the top deferred simplifications as decision shapes, each
  with a Decision-register row — the maintainer should leave the review knowing the three biggest
  simplification opportunities, even if none ship now.
- An empty ledger means the review didn't look. If that's truly the state
  of the codebase, write the sentence saying so — don't omit the section.

### 12.5 Close out
- For every `[DEFERRED]` item, decide: does it meet the issue-worthiness
  threshold (see Action-item lifecycle)? If yes, file as a GitHub issue
  with labels + project board, and update the line to `[DEFERRED → #N]`.
  If no, leave it as `[DEFERRED]` for the next review to pick up.
- Fill the Decision register — the plan's last section: every
  Decision-pending finding and every letter-shaped action item has a
  row; Open rows carry to the next review.
- The plan file stays in the repo as historical record. Don't delete it.
```

## Decision register

The last section of every review plan. One table, a snapshot as of close-out: every `Decision pending` finding and every letter-shaped action item has a row here, and nothing else does. The register is an *index* — the options and the rationale stay in the phase that raised them; the row says what was asked, what was chosen, and where that choice is recorded.

Heading in the plan: `## Decision register`. Columns:

| Slug                 | Question                                                    | Options → recommended                                             | Origin                       | Status                                                             | Recorded at |
| -------------------- | ----------------------------------------------------------- | ----------------------------------------------------------------- | ---------------------------- | ------------------------------------------------------------------ | ----------- |
| `dep-pinning-policy` | Pin majors, lockfile, or accept looseness?                  | (A) majors · (B) lockfile · (C) accept → **A**                    | P5 #4                        | Decided (A) 2026-06-14                                             | ADR-0021    |
| `logout-post`        | Should logout stay a `GET`?                                 | (A) POST + CSRF · (B) leave → **B**                               | P5 #6, carried from 2026-03  | Accepted risk — revisit when the session carries more than identity | this row    |
| `single-dispatcher`  | Keep one dispatcher when the second transport bypasses it?  | (A) collapse · (B) second transport adopts it · (C) keep → **A**  | P3 #4                        | Open                                                               | —           |

Status vocabulary — exactly these four:

- **Open** — surfaced, not yet chosen.
- **Decided (X) YYYY-MM-DD** — the maintainer picked. `Recorded at` names where the rationale lives: an ADR when the choice changes an architecture claim, the issue when it schedules work, *this row* otherwise. The register is never the only copy of a significant decision.
- **Accepted risk — revisit when <trigger>** — a deliberate "leave it" is a decision, not an `Acceptable` finding. The trigger (a release, an event, a date) is what the next review re-checks, instead of re-discovering the finding and re-arguing it.
- **Dropped (why)** — the question stopped mattering. One clause of why, so it isn't re-raised.

Rules:

- **Carry-forward.** [Phase 0.2](#02-ground-rules-read) copies every Open row, and every Accepted-risk row whose trigger has not fired, from the latest review *of each kind* (code, UX, design) into the new plan's register, origin pointing back. A fired trigger reopens the row. A decision parked in a UX review must be visible to the next codebase review, and vice versa.
- **Third-review rule.** A row entering its third review is decided in that review's Phase 0 — by the maintainer, or by default to the recommendation. Carrying a question costs a re-read every time; three carries means the answer is the recommendation, or the question is dropped.
- **Close-out block.** No action item is `[DONE]` while an Open row it depends on is still Open. Note the block in the action item, per [Cross-phase blocks](#cross-phase-blocks).
- **Decision shapes stay put.** The `(A) / (B) / (C)` block lives in the phase's action items; the register links to it by slug and never repeats the options prose.

## Conventions checklist

When updating the plan during a session, double-check:

- [ ] Findings live in a table; one row per finding; columns are # / Finding / Location / Severity. Numbering resets per phase.
- [ ] Severity uses the seven-label vocabulary, no ad-hoc labels.
- [ ] Action items are markdown checkboxes (`- [ ]` / `- [x]`) keyed by a unique kebab-case slug, not prose paragraphs. Slugs are unique across the whole plan.
- [ ] Done action items have a one-line note about what shipped + commit ref.
- [ ] Deferred items either have a GitHub issue number (`[DEFERRED → #N]`) — only for issue-worthy work — or are flagged `[DEFERRED]` to be picked up at the next review.
- [ ] Cross-phase blocks are noted in the *blocking* phase, not the blocked one.
- [ ] File / line references are markdown links: `[path:line](path#L123)`. Line anchors are fine here, unlike in ADRs — the plan is a point-in-time record; rot doesn't matter.
- [ ] Every phase closed with a written one-line "what can go?" answer — even when the answer is "nothing".
- [ ] Phase 12 includes the simplification ledger, with the top deferred simplifications surfaced as decision shapes.
- [ ] Decision-shape items present options as `(A) / (B) / (C)` with a recommendation; don't act unilaterally on them.
- [ ] Decision register is the plan's last section; every Decision-pending finding and every letter-shaped action item has a row; carried rows name their origin; accepted risks carry a revisit trigger.
- [ ] Every sub-check has a Checks-run line — clean, finding, unrun with the reason, or n/a for a tagged block the project lacks. Clean is recorded, not omitted.
- [ ] File header states scope (in / out) and the phases deliberately skipped.
- [ ] Phase 2 covers comment & docstring hygiene against the project's "WHY not WHAT" rule, not just artifacts and tombstones.
- [ ] Phase 3 produces a concept landscape map first (cite it in later phases), then runs the load-bearing assumption audit, pattern duplication, and ADR supersession against it.
- [ ] Phase 3.W (load-bearing assumption audit) ran for every name in the concept map, with the surviving justification and any excess called out per row.
- [ ] Phase 0 baseline metrics captured before any code change; Phase 12.2 compares against them.
- [ ] Subagent claims spot-checked with at least one verifying `grep` before relaying into the findings table.
- [ ] Sub-section numbering is consistent within each artifact (the template uses 3.W/3.X/3.Y/3.Z, 5.1/5.2, 10.A/10.B; pick one per phase, don't mix).
