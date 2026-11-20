# UX review template

When the user asks for a UI/UX review (a structured pass over the user-facing surface to find friction, inconsistency, and bloat after a feature-heavy stretch), follow this template. It covers any surface a human drives — web, desktop GUI, TUI, CLI, and chat bots — and is optimised for the same context as the rest of the set: solo-maintained products, work-as-you-go execution, plan kept in `docs/reviews/`.

Surface tags as in `review_machinery.md` § Surface tags; untagged checks apply to every surface.

## Shared machinery

Process is `review_machinery.md`'s and applies unchanged; the working document is `docs/reviews/YYYY-MM-ux-review.md`, and the labels are translated to surfaces at the end. The per-phase **"what can go?"** habit applies here too — a written one-line answer before any phase closes.

This doc defines only what differs: the stance, the CUJ backbone, the phases, its *At task time* questions, and the severity translation at the end.

One addition to prioritisation: **fix order is label first, then the CUJ's frequency.** The labels are categories, not numbers; within one, a papercut on the daily journey outranks a broken corner on the quarterly one. Note the frequency next to each finding's CUJ.

## When to use this template

Run it after a release that grew the surface, before opening the product to strangers, or when the same user question arrives twice — every repeated question is a failed affordance, and the support / feedback inbox is Phase 0 input. Cadence for a living product: roughly once per minor release, or quarterly.

Don't run it:

- Pre-v0.1 — polish before the gauntlet's done-statement ships is procrastination with extra steps.
- Mid-redesign — review a stable target.
- To establish or evolve the design system itself — that's a `design_review.md` pass (it builds the language; this review audits a settled surface for friction).
- To decide which screens, commands and messages exist, what each shows and where each fact lives — that's an `ia_review.md` pass (it settles the structure; this review walks it for friction).
- On a headless service or library — no surface, no review (an API's "UX" is its docs, covered by the codebase review's truth-up phase).

**Minimum viable review.** Phase 0 → Phase 1 → Phase 8: inventory the journeys, walk them cold beside one real-user source, write the deltas and the ledger. Everything else sharpens what the walkthroughs find.

## Stance

1. **The journeys are the product.** Users never experience the architecture or the screen list; they experience a handful of paths from intent to outcome. Review the product the way it's used — journey by journey — not the way it's built. Screen-by-screen audits miss the friction that lives *between* screens.
2. **Subtraction beats polish.** The codebase review's removal bias, translated: every setting is a question the user must answer, every command is something to learn, every option is a decision tax, every step is a place to abandon. For a maturing product, look for deletions — of steps, choices, settings, and words — before polish. Simplification proposals are a first-class deliverable here too, aggregated into the same close-out ledger.
3. **You are not the user.** Developer knowledge is the review's enemy: you know which button is safe, which order works, what the error "really" means. Every walkthrough runs in a fresh environment with a non-privileged account, following only the public docs. That removes your state and your privileges, not your knowledge — so the walk never stands alone, and every review carries one real-user source (Phase 1): one observed user beats ten self-walkthroughs.

## CUJs — the backbone

A **Critical User Journey** is a complete path from intent to outcome — "from *I want yesterday's summary* to reading it" — not a feature and not a screen. Keep the list short: a product typically has five to seven journeys at the level of a goal ([Google](https://www.mindtheproduct.com/measuring-critical-user-journeys-javier-andres-bargas-avila-ux-research-director-google/)). Past seven, check the granularity first — tasks counted as journeys merge into the goal they serve — and only then read the length as a finding about focus. Three are always on the list:

- **First contact** — install / invite / sign-up, through to first successful use.
- **The daily driver** — the journey the product exists for.
- **Recovery** — the most common failure, from the user's seat, through to success.

Format, one line each in the plan:

```
CUJ-1 — <intent>: <entry point> → <success criterion>. Frequency: daily | weekly | rare.
```

Canonical home: `docs/product/vision.md` § Critical user journeys. The review plan references CUJs by id; it doesn't restate them. Ids are permanent: a retired journey is struck, never renumbered, so every review that cites one still resolves.

## Phases

| Phase | Topic                                | Why                                                       |
| ----- | ------------------------------------ | --------------------------------------------------------- |
| 0     | Baseline & inventory                 | CUJs, surface counts, metrics to delta against            |
| 1     | CUJ walkthroughs                     | The core: walk every journey cold in a fresh environment  |
| 2     | First-run & empty states             | The product before there's any data                       |
| 3     | Error paths & recovery               | Errors that teach vs errors that strand                   |
| 4     | Consistency, copy & design language  | One pattern per job, one name per concept                 |
| 5     | Performance as UX                    | Latency budgets and feedback, measured on the CUJs        |
| 6     | Accessibility floor                  | A quick floor; a full audit before strangers arrive       |
| 7     | Surface diet                         | Evidence-based removal of commands, settings, screens     |
| 8     | Close-out                            | Re-walk, deltas, ledger, update the design reference      |

**Waves** (`review_machinery.md` § How a review runs): Phase 0 → Phases 1–6 in parallel, Phase 1 as several independent walkers → Phase 7, which diets the steps Phase 1 recorded → Phase 8.

### Phase 0 sub-checks — baseline & inventory

- **CUJ inventory** — read it from `docs/product/vision.md` § Critical user journeys; on the first review, create that section there (format above). Then pull the support / feedback / "how do I" questions since the last review — each maps to a CUJ or exposes a missing one.
- **Competitor baseline** — where `docs/product/competitors.md` holds a walk of the alternatives (the gauntlet's Phase 1), carry its per-CUJ counts into the baseline table next to yours: the product should stay ahead of the tools it was built to beat. Re-walk an alternative after it ships a major change, and update the file.
- **Surface counts**, mechanically: screens / commands / flags, user-facing settings, distinct user-facing error messages. These are the bloat radar and the delta denominators.
- **Ground-rules read:** `docs/product/vision.md` (who this is for), the design-language reference, and the rest per `review_machinery.md` § Phase 0 reads.
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

- **The walk is an expert walkthrough, not user evidence.** The reviewer walks as a first-time user would and logs the friction step by step. A single evaluator finds about a third of an interface's known problems, where five find about three quarters ([Nielsen](https://www.nngroup.com/articles/how-to-conduct-a-heuristic-evaluation/)), and the builder knows too much, so the walk's counts are an expert's — never a success rate or a user's time.
- **Fresh environment, every time.** New browser profile / clean container / `[bot]` a fresh workspace joined with a non-admin account. Public docs only — no source, no memory of being the developer.
- **Walk each CUJ twice:** cold (first-time user) and warm (returning user — different friction: re-finding things, re-auth, remembering syntax).
- **Record per step:** what the user must already know, what they must decide or type, what the system shows, and every hesitation. **A hesitation is a finding** — yours comes with full context; a real user's would be worse. Mark the steps where a real user would have given up.
- **Count:** steps, required decisions, and time-to-success per CUJ. These feed the delta table, labelled as the expert's numbers.
- **The README test:** a stranger completes CUJ-1 using only the README. Where they couldn't, either the surface or the doc is wrong — pick one and fix it (the surface twin of the codebase review's docs truth-up).
- **AI agents as walkers add inspection, not users.** They complete tasks more often than people and take the shortest path ([Zhong et al.](https://arxiv.org/abs/2512.03568)), and they report problems that are not there ([Baymard](https://baymard.com/blog/gpt-ux-audit); [MeasuringU](https://measuringu.com/does-ai-find-real-ui-problems-or-just-hallucinations/)). A person triages every finding they raise, and none of their numbers enter the delta table. Run several where agents can be launched — three independent walkers, each from a fresh context with only the public docs: two evaluators share a median 27% of the problems they find ([MeasuringU](https://measuringu.com/evaluator-effect/)), so the union is wider, and the triage keeps out what isn't there.
- `[bot]` Run as a regular member, not the workspace owner — and walked by a person: automating a member's account breaks some platforms' terms, and a bot account sees a different product. Check command discovery without prior knowledge, missing-permission behavior, response readability at mobile width.
- `[cli]` Walk `--help`-only: no man page, no README. Flags discoverable, errors name the flag at fault, exit codes honest.
- **One real-user source, every review:** think-aloud sessions with real people — five for a single audience, three or four each for two, three each for three or more ([NN/g](https://www.nngroup.com/articles/why-you-only-need-to-test-with-5-users/)) — or a journey rebuilt from production logs, in aggregate, with no message text or names. Where no real user can be reached, the review closes with the gap recorded, and the next UX review owes the source.

### Phase 2 sub-checks — first-run & empty states

- **Zero-data pass:** every screen / command with nothing in it yet. An empty state names what will be here and the one action that fills it — never a blank table or a bare "no results".
- **Zero-config rule:** the product does something useful before any setting is touched. Every setting required before first value is onboarding friction — defaults, or justify.
- **First five minutes:** from install / invite to first success without docs. `[bot]` after the invite, does anything happen unprompted? Is `/help` discoverable, and are slash-command descriptions actually descriptive? `[web]` the first screen answers "what do I do here". `[cli]` bare invocation prints orientation, not an error.
- **Permissions failure on day one:** `[bot]` invited with missing scopes — does it explain, or sit silent? `[web]` unauthorized states say how to get access, not just "403".
- `[llm]` **Say what it can do.** Before the first request, a model-driven surface says what it can and cannot do, and how well ([Microsoft's Human-AI guidelines](https://www.microsoft.com/en-us/research/uploads/prod/2019/01/Guidelines-for-Human-AI-Interaction-camera-ready.pdf) 1–2).

### Phase 3 sub-checks — error paths & recovery

- **Enumerate the messages.** Grep the user-facing error strings; inventory them. Every one answers three questions: *what happened, why, what next.* Codes without words, stack traces, and "something went wrong" all fail.
- **Force the classics, one by one:** malformed input, missing permission, dependency down, rate limit, expired credentials, double-submit. Walk each from the user's seat through to recovery.
- **Silent-failure hunt** — the worst class: actions that fail with no signal. Cross-check logs against the surface; errors logged but never surfaced are findings (`Real bug`).
- **Destructive actions** use the one canonical confirm pattern from the design reference; reversible beats confirmed where cheap (undo > are-you-sure).
- **Retry tax:** after an error, is the user's input preserved? Re-typing everything is punishment for the system's failure.
- `[llm]` **Repair, correction, explanation.** A misunderstood request gets a repair — a clarifying question or a restatement — rather than a confident wrong answer; a wrong reply can be corrected or dismissed; where it matters, the reply says why it did what it did (Human-AI guidelines 8–11).

### Phase 4 sub-checks — consistency, copy & design language

- **Conformance audit** against the design-language reference: components, data rendering (timestamps, IDs, counts, money), the one-pattern-per-job inventory. Verdict per divergence: the surface is wrong (fix it) or the reference is stale (update it) — the same two directions as doc truth-up. When the divergence is *systemic* — the same function rendered differently across many surfaces, or the reference itself thin or self-contradictory — it's not a per-surface fix: route it to a `design_review.md` pass, which rebuilds the language rather than patching screens.
- **Terminology:** copy uses the controlled vocabulary the IA review keeps (`ia_review.md` § Phase 6), in the UI, the docs and error strings alike. A word outside it is a finding here; a concept with no word goes to the IA review. `[bot]` the platform API's internal term leaking into user-facing copy is the classic.
- **Copy pass:** one casing convention, one voice, jargon only where the audience owns it. Shorter is almost always better — word diet is part of the subtraction stance.
- **i18n / locale** (if shipped): untranslated strings in the non-default locale, date / number formatting per locale, `[bot]` replies follow the configured workspace / user locale rather than the developer's.

### Phase 5 sub-checks — performance as UX

- **Budgets by interaction class:** instant feel <100ms, in-flow <1s, anything longer owes visible progress. Assign every CUJ step a class; **measure, don't vibe** — p50/p95 from logs, or a stopwatch during the walkthrough.
- **Acknowledge within ~100ms, always:** `[web]` pressed states, skeletons, optimistic updates; `[cli]` progress output for anything over a second; `[bot]` typing indicator or deferred reply inside the platform's ack window; stream long responses instead of batching them.
- **Cold start counts** where the user feels it: `[cli]` startup time is part of every single journey; `[desktop]` launch-to-usable.
- `[llm]` A model reply that can run past ~10 seconds shows progress, and a long one can be stopped ([Nielsen](https://www.nngroup.com/articles/response-times-3-important-limits/)).
- Per-call work that could be per-process is a perf *and* a simplification finding — route it to the codebase review's 3.W lens.

### Phase 6 sub-checks — accessibility floor

Not a WCAG audit: a quick floor that catches what a short manual pass can — W3C's own quick checks "cover just a few" issues ([Easy Checks](https://www.w3.org/WAI/test-evaluate/easy-checks/)). Before the product opens to strangers, a full audit with assistive technology goes beyond it.

- Meaning never carried by color alone, on every surface.
- `[web]` Keyboard-only CUJ-1: everything reachable, focus visible, no traps. Contrast spot-check on body text and primary actions. Labels on form fields; alt text on meaningful images.
- `[cli]` `[tui]` Output legible without color (`NO_COLOR` respected); `--json` or equivalent for the machine user — scripts are users too.
- `[bot]` Meaning not encoded only in emoji or embed color; alt text on images where the platform supports it.
- `[web]` `[desktop]` Reduced-motion respected; text scales without breaking the primary screens.

### Phase 7 sub-checks — surface diet

The subtraction phase. Evidence first: the usage data from Phase 0's instrumentation check.

- **Zero / low-usage features:** commands, screens, flags with no traffic since the last review — remove, merge, or deliberately keep with a written reason (`Cleanup` / `Decision pending`).
- **Settings never changed from default** across real usage: hardcode the default, delete the setting — the surface twin of the config phase's one-position switches.
- **Step diet per CUJ:** for every step recorded in Phase 1, ask what removes it — a default, a merge, an inference. Removing a step beats polishing it.
- **Choice overload:** menus and option lists past ~7 entries; flags duplicating other flags; two paths to the same outcome where one is always better (kill the loser).
- **Structure goes to the IA review.** Removal here is usage-driven: a command, a setting, a step. A screen or command the intent doc's IA section keeps is not removed on usage alone. When the finding is the structure itself — two screens doing one job, one fact shown in several places, a screen no reader's decision uses — route it to an `ia_review.md` pass instead of patching screens one at a time.
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

- **Simplification ledger**, per `review_machinery.md` § Close-out: every removal across phases in one ranked table, the top items not done surfaced as decision shapes.
- **Decision register** — the plan's last section, per the shared machinery: every `Decision pending` finding and letter-shaped item has a row; a feature kept with a reason that is really a risk gets a revisit trigger.
- **Update the design-language reference** with every pattern decision the review made — the reference is what holds the line afterward, since `AGENTS.md` enforces it on every future UI edit, including the AI-driven ones.
- **Write CUJ changes back into `vision.md`** — journeys added, retired, or re-scoped. The next review starts from there, not from scratch.
- **Self-enforcing rules where cheap, one assertion each:** `[bot]` help output lists every registered command; `[cli]` snapshot of `--json` output; `[web]` lint forbidding raw hex outside the token file.
- **Close every action item** per the machinery — done, filed as a child of the parent issue, left on its checklist, reported privately or not taken — and freeze the document.

## At task time

What a task's work round asks of one diff for this review (`task_review.md` § Lens reviewers): the classes of finding this review keeps meeting that a reader can see in the change that introduces them. The person-facing reviewer asks them. A class two reviews of this kind in a row meet with no question here gets one (`review_machinery.md` § Close-out).

- **Readers.** Which readers, roles, tiers and configurations reach the change, and what does each see — a control shown to someone who cannot use it included (Phases 1 and 2)?
- **Failed, empty, boundary.** What does each region or reply the diff touches show when its read fails, finds nothing, or sits on a boundary — zero, one, the cap, an exact page — and does it name the next step (Phases 2 and 3)?
- **Errors.** Does every new message say what happened, why and what next; does a destructive action use the canonical confirm; does the person's input survive the error (Phase 3)?
- **Locale.** Does every string the diff shows a person — fallbacks and formatted values included — come from the reader's locale (Phase 4, *i18n*)?
- **Accessibility floor.** Does each new control carry its native attribute, a unique label, focus entry and, where it changes state, an announcement, with no meaning carried by color alone (Phase 6)?

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

Per `review_machinery.md` § Updating a guide.
