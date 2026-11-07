# UX review template

When the user asks for a UI/UX review (a structured pass over the user-facing surface to find friction, inconsistency, and bloat after a feature-heavy stretch), follow this template. It covers any surface a human drives — web, desktop GUI, TUI, CLI, and chat bots — and is optimised for the same context as the rest of the set: solo-maintained products, work-as-you-go execution, plan kept in `docs/reviews/`.

Surface tags: `[web]`, `[desktop]`, `[tui]`, `[cli]`, `[bot]` (chat-platform bots). Untagged checks apply to every surface.

## Shared machinery

Process is inherited from `codebase_review.md` and applies unchanged: the pass structure (skeleton → per-phase analysis → execution → close-out, each phase picked up cold from the plan), per-phase context discipline, the working document pattern (`docs/reviews/YYYY-MM-ux-review.md`, permanent record feeding the next review), findings tables and the per-phase format, the seven-label severity vocabulary, action-item slugs and lifecycle, the issue-worthiness threshold, decision shapes, cross-phase blocks, the per-phase Checks-run block, and the Decision register as the plan's last section. The per-phase **"what can go?"** habit applies here too — a written one-line answer before any phase closes.

This doc defines only what differs: the stance, the CUJ backbone, the phases, and the severity translation at the end.

One addition to prioritisation: **fix order = severity × CUJ frequency.** A papercut on the daily journey outranks a broken corner on the quarterly one. Note the frequency next to each finding's CUJ.

## When to use this template

Run it after a release that grew the surface, before opening the product to strangers, or when the same user question arrives twice — every repeated question is a failed affordance, and the support / feedback inbox is Phase 0 input. Cadence for a living product: roughly once per minor release, or quarterly.

Don't run it:

- Pre-v0.1 — polish before the gauntlet's done-statement ships is procrastination with extra steps.
- Mid-redesign — review a stable target.
- To establish or evolve the design system itself — that's a `design_review.md` pass (it builds the language; this review audits a settled surface for friction).
- On a headless service or library — no surface, no review (an API's "UX" is its docs, covered by the codebase review's truth-up phase).

**Minimum viable review.** Phase 0 → Phase 1 → Phase 8: inventory the journeys, walk them cold, write the deltas and the ledger. Everything else sharpens what the walkthroughs find.

## Stance

1. **The journeys are the product.** Users never experience the architecture or the screen list; they experience three to seven paths from intent to outcome. Review the product the way it's used — journey by journey — not the way it's built. Screen-by-screen audits miss the friction that lives *between* screens.
2. **Subtraction beats polish.** The codebase review's removal bias, translated: every setting is a question the user must answer, every command is something to learn, every option is a decision tax, every step is a place to abandon. For a maturing product the biggest UX wins are deletions — of steps, choices, settings, and words. Simplification proposals are a first-class deliverable here too, aggregated into the same close-out ledger.
3. **You are not the user.** Developer knowledge is the review's enemy: you know which button is safe, which order works, what the error "really" means. Every walkthrough runs in a fresh environment with a non-privileged account, following only the public docs. And if you can watch one real user attempt the primary journey without coaching — silent, taking notes — do it: one observed user beats ten self-walkthroughs.

## CUJs — the backbone

A **Critical User Journey** is a complete path from intent to outcome — "from *I want yesterday's summary* to reading it" — not a feature and not a screen. Inventory 3–7, no more: if more than seven feel critical, the product lacks focus, and that itself is finding #1. Three are always on the list:

- **First contact** — install / invite / sign-up, through to first successful use.
- **The daily driver** — the journey the product exists for.
- **Recovery** — the most common failure, from the user's seat, through to success.

Format, one line each in the plan:

```
CUJ-1 — <intent>: <entry point> → <success criterion>. Frequency: daily | weekly | rare.
```

Canonical home: `docs/product/vision.md` § Critical user journeys. The review plan references CUJs by id; it doesn't restate them.

## Phases

| Phase | Topic                                | Why                                                       |
| ----- | ------------------------------------ | --------------------------------------------------------- |
| 0     | Baseline & inventory                 | CUJs, surface counts, metrics to delta against            |
| 1     | CUJ walkthroughs                     | The core: walk every journey cold in a fresh environment  |
| 2     | First-run & empty states             | The product before there's any data                       |
| 3     | Error paths & recovery               | Errors that teach vs errors that strand                   |
| 4     | Consistency, copy & design language  | One pattern per job, one name per concept                 |
| 5     | Performance as UX                    | Latency budgets and feedback, measured on the CUJs        |
| 6     | Accessibility floor                  | The 30-minute pass that catches most of it                |
| 7     | Surface diet                         | Evidence-based removal of commands, settings, screens     |
| 8     | Close-out                            | Re-walk, deltas, ledger, update the design reference      |

### Phase 0 sub-checks — baseline & inventory

- **CUJ inventory** — read it from `docs/product/vision.md` § Critical user journeys; on the first review, create that section there (format above). Then pull the support / feedback / "how do I" questions since the last review — each maps to a CUJ or exposes a missing one.
- **Surface counts**, mechanically: screens / commands / flags, user-facing settings, distinct user-facing error messages. These are the bloat radar and the delta denominators.
- **Ground-rules read:** `docs/product/vision.md` (who this is for), the design-language reference, and — from the latest review of each kind, not only the last UX one — the open `[ ]` items and the Decision register's Open and untriggered Accepted-risk rows.
- **Instrumentation check:** can you tell from logs which commands / screens / flags are actually used, and where journeys abandon? If not, that's the first finding — Phase 7 runs on this evidence.

Baseline table:

| Metric                              | Baseline ({date}) |
| ----------------------------------- | ----------------- |
| CUJ-1 steps (cold)                  | 9                 |
| CUJ-1 time-to-success (cold)        | 4m 10s            |
| Commands / screens                  | 23                |
| User-facing settings                | 17                |
| Errors with next-step guidance      | 12 / 31           |
| p95 latency, primary interaction    | 1.8s              |

### Phase 1 sub-checks — CUJ walkthroughs

- **Fresh environment, every time.** New browser profile / clean container / `[bot]` a fresh workspace joined with a non-admin account. Public docs only — no source, no memory of being the developer.
- **Walk each CUJ twice:** cold (first-time user) and warm (returning user — different friction: re-finding things, re-auth, remembering syntax).
- **Record per step:** what the user must already know, what they must decide or type, what the system shows, and every hesitation. **A hesitation is a finding** — yours comes with full context; a real user's would be worse.
- **Count:** steps, required decisions, and time-to-success per CUJ. These feed the delta table.
- **The README test:** a stranger completes CUJ-1 using only the README. Where they couldn't, either the surface or the doc is wrong — pick one and fix it (the surface twin of the codebase review's docs truth-up).
- `[bot]` Run as a regular member, not the workspace owner: command discovery without prior knowledge, missing-permission behavior, response readability at mobile width.
- `[cli]` Walk `--help`-only: no man page, no README. Flags discoverable, errors name the flag at fault, exit codes honest.

### Phase 2 sub-checks — first-run & empty states

- **Zero-data pass:** every screen / command with nothing in it yet. An empty state names what will be here and the one action that fills it — never a blank table or a bare "no results".
- **Zero-config rule:** the product does something useful before any setting is touched. Every setting required before first value is onboarding friction — defaults, or justify.
- **First five minutes:** from install / invite to first success without docs. `[bot]` after the invite, does anything happen unprompted? Is `/help` discoverable, and are slash-command descriptions actually descriptive? `[web]` the first screen answers "what do I do here". `[cli]` bare invocation prints orientation, not an error.
- **Permissions failure on day one:** `[bot]` invited with missing scopes — does it explain, or sit silent? `[web]` unauthorized states say how to get access, not just "403".

### Phase 3 sub-checks — error paths & recovery

- **Enumerate the messages.** Grep the user-facing error strings; inventory them. Every one answers three questions: *what happened, why, what next.* Codes without words, stack traces, and "something went wrong" all fail.
- **Force the classics, one by one:** malformed input, missing permission, dependency down, rate limit, expired credentials, double-submit. Walk each from the user's seat through to recovery.
- **Silent-failure hunt** — the worst class: actions that fail with no signal. Cross-check logs against the surface; errors logged but never surfaced are findings (`Real bug`).
- **Destructive actions** use the one canonical confirm pattern from the design reference; reversible beats confirmed where cheap (undo > are-you-sure).
- **Retry tax:** after an error, is the user's input preserved? Re-typing everything is punishment for the system's failure.

### Phase 4 sub-checks — consistency, copy & design language

- **Conformance audit** against the design-language reference: components, data rendering (timestamps, IDs, counts, money), the one-pattern-per-job inventory. Verdict per divergence: the surface is wrong (fix it) or the reference is stale (update it) — the same two directions as doc truth-up. When the divergence is *systemic* — the same function rendered differently across many surfaces, or the reference itself thin or self-contradictory — it's not a per-surface fix: route it to a `design_review.md` pass, which rebuilds the language rather than patching screens.
- **Terminology:** one name per concept, everywhere — UI, docs, and error strings. `[bot]` the platform API's internal term leaking into user-facing copy is the classic. Add a glossary table to the design reference if it lacks one.
- **Copy pass:** one casing convention, one voice, jargon only where the audience owns it. Shorter is almost always better — word diet is part of the subtraction stance.
- **i18n / locale** (if shipped): untranslated strings in the non-default locale, date / number formatting per locale, `[bot]` replies follow the configured workspace / user locale rather than the developer's.

### Phase 5 sub-checks — performance as UX

- **Budgets by interaction class:** instant feel <100ms, in-flow <1s, anything longer owes visible progress. Assign every CUJ step a class; **measure, don't vibe** — p50/p95 from logs, or a stopwatch during the walkthrough.
- **Acknowledge within ~100ms, always:** `[web]` pressed states, skeletons, optimistic updates; `[cli]` progress output for anything over a second; `[bot]` typing indicator or deferred reply inside the platform's ack window; stream long responses instead of batching them.
- **Cold start counts** where the user feels it: `[cli]` startup time is part of every single journey; `[desktop]` launch-to-usable.
- Per-call work that could be per-process is a perf *and* a simplification finding — route it to the codebase review's 3.W lens.

### Phase 6 sub-checks — accessibility floor

Not a WCAG audit; the 30-minute pass that catches the majority.

- `[web]` Keyboard-only CUJ-1: everything reachable, focus visible, no traps. Contrast spot-check on body text and primary actions. Labels on form fields; alt text on meaningful images.
- `[cli]` `[tui]` Output legible without color (`NO_COLOR` respected); meaning never carried by color alone; `--json` or equivalent for the machine user — scripts are users too.
- `[bot]` Meaning not encoded only in emoji or embed color; alt text on images where the platform supports it.
- `[web]` `[desktop]` Reduced-motion respected; text scales without breaking the primary screens.

### Phase 7 sub-checks — surface diet

The subtraction phase. Evidence first: the usage data from Phase 0's instrumentation check.

- **Zero / low-usage features:** commands, screens, flags with no traffic since the last review — remove, merge, or deliberately keep with a written reason (`Cleanup` / `Decision pending`).
- **Settings never changed from default** across real usage: hardcode the default, delete the setting — the surface twin of the config phase's one-position switches.
- **Step diet per CUJ:** for every step recorded in Phase 1, ask what removes it — a default, a merge, an inference. Removing a step beats polishing it.
- **Choice overload:** menus and option lists past ~7 entries; flags duplicating other flags; two paths to the same outcome where one is always better (kill the loser).
- **Removal etiquette:** where real users exist, removals ship behind a deprecation note one release ahead. Subtraction is the goal; surprise is not.

### Phase 8 sub-checks — close-out

- **Re-walk CUJ-1 cold** in a fresh environment; new step count and time-to-success go in the delta table.
- **Delta table** against Phase 0 — fewer steps, fewer settings, a smaller surface, and a faster p95 are the success signals:

| Metric                           | Baseline | Final   | Δ          |
| -------------------------------- | -------- | ------- | ---------- |
| CUJ-1 steps (cold)               | 9        | 6       | −3         |
| CUJ-1 time-to-success (cold)     | 4m 10s   | 2m 30s  | −1m 40s    |
| Commands / screens               | 23       | 19      | −4         |
| User-facing settings             | 17       | 11      | −6         |
| Errors with next-step guidance   | 12 / 31  | 28 / 28 | all, fewer |
| p95 latency, primary interaction | 1.8s     | 0.9s    | −0.9s      |

- **Simplification ledger**, same rules as the codebase review's 12.4: every removal across phases in one ranked table, top deferred items surfaced as decision shapes.
- **Decision register** — the plan's last section, per the shared machinery: every `Decision pending` finding and letter-shaped item has a row; a feature kept with a reason that is really a risk gets a revisit trigger.
- **Update the design-language reference** with every pattern decision the review made — the reference is what holds the line afterward, since `AGENTS.md` enforces it on every future UI edit, including the AI-driven ones.
- **Write CUJ changes back into `vision.md`** — journeys added, retired, or re-scoped. The next review starts from there, not from scratch.
- **Self-enforcing rules where cheap, one assertion each:** `[bot]` help output lists every registered command; `[cli]` snapshot of `--json` output; `[web]` lint forbidding raw hex outside the token file.
- File issues per the shared issue-worthiness threshold; the plan stays in `docs/reviews/` permanently.

## Severity, translated to surfaces

Same seven labels, same default actions — surface meanings:

| Severity             | On a surface, this means                                                            |
| -------------------- | ----------------------------------------------------------------------------------- |
| **Real bug**         | A CUJ can't be completed, work is lost, or failure is silent                        |
| **Drift**            | Surface contradicts the design reference, the docs, or its own other screens        |
| **Drift-prone**      | Fine today, degrades with known growth (unpaginated list, menu that grows per feature) |
| **Cleanup**          | Dead surface: unreachable screens, zero-usage commands, vestigial settings          |
| **Minor**            | Papercuts: wording, alignment, a weak default on a rare path                        |
| **Acceptable**       | Deliberate trade-off, confirmed; note it for the record                             |
| **Decision pending** | Competing patterns; the maintainer picks via a decision shape                       |

## When to update this template

Same rule as the set: two reviews in a row needing the same hand-edit means the template is wrong — fix it here, in the canonical gist.
