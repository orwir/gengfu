# Design review template

When the user asks for a **design-language pass** — establishing the visual system for the first
time, or evolving it after the surface has grown — follow this template. It is the design
counterpart of `codebase_review.md` (the code) and `ux_review.md` (the journeys): where the UX
review asks *do the journeys work*, the design review asks *is the visual system coherent* — one
function looking and behaving the same everywhere, built from one set of tokens, with
accessibility baked in. The two pair at a release; neither replaces the other.

Surface-agnostic, with surface tags where a check differs: `[web]`, `[desktop]`, `[tui]`,
`[cli]`. Untagged checks apply everywhere. Run it on the trigger described in
`design_workflow.md` § *Triggers*.

## Shared machinery

Process is inherited from `codebase_review.md` and applies unchanged: the pass structure
(skeleton → per-phase analysis → execution → close-out, each phase picked up cold from the plan),
per-phase context discipline, the working-document pattern (`docs/reviews/YYYY-MM-design-review.md`,
a permanent record feeding the next pass), the findings tables and per-phase format, the
seven-label severity vocabulary (translated for the visual system at the end), action-item slugs
and lifecycle, the issue-worthiness threshold, decision shapes, cross-phase blocks, the per-phase Checks-run block, and the Decision register as the plan's last section. The
per-phase **"what can go?"** habit applies with force here — a design pass that only *adds* tokens
and never *collapses* a forked variant didn't look hard enough.

This doc defines only what differs: the stance, the phases, and the severity translation.

## What this is *not*

- **Not a UX review.** `ux_review.md` walks journeys for friction and bloat. This pass walks the
  *visual system* for incoherence. A button that's hard to find is a UX finding; a button that
  looks different on three screens is a design finding. When a UX review's Phase 4 (consistency)
  finds *systemic* drift, it routes here.
- **Not a one-off conformance fix.** A single screen diverging from the reference is a direct
  edit. This pass is for when the *language itself* needs building or rebuilding.
- **Not a redesign of what the product *does*.** Information architecture, flows, and feature
  scope are out of scope — the pass re-skins and systematizes the surfaces that exist; it doesn't
  re-architect them. (IA changes are an ADR + a `ux_review`, run on a settled design.)

## When to use

Run it on a `design_workflow.md` trigger: the surface count doubled, a second surface appeared,
pre-release polish, or a felt systemic inconsistency. The first run on a project that skipped the
founding step is an *establishment* pass (Phases 1–4 carry the weight); later runs are *evolution*
passes (Phase 5, the conformance audit, carries it).

Don't run it: pre-foundation on a one-screen surface (just do the founding step in
`design_workflow.md`); mid-redesign of the same surface (review a stable target); on a headless
service (no surface).

**Minimum viable pass.** Phase 0 → 1 → 3 → 7 → 8: inventory, characterize, systematize the
foundations, codify, close out. That loop produces a real token system and a written reference;
the component, conformance, and accessibility phases sharpen what it finds.

## Stance

1. **Consistency over cleverness; minimum deviation.** One function, one look, everywhere —
   derived from one set of tokens. Every bespoke variant is a tax: it has to be re-found,
   re-tested, and re-maintained. The pass's bias is to *collapse* variants into the shared
   primitive, not to celebrate them.
2. **Subtraction beats addition.** A sprawl of seventeen distinct radii is not a rich system;
   it's the absence of one. Every phase is expected to *reduce* — distinct values per axis, forked
   components, off-token one-offs — alongside what it defines. The close-out ledger ranks the
   collapses.
3. **The foundation carries the floor.** Accessibility, responsiveness, and state coverage are
   not per-screen polish — they're properties of the tokens and primitives. Bake them in once
   (contrast into the color roles, touch targets into the control sizes, every state into the
   shared contract) and every screen inherits them for free. A floor enforced per-screen erodes.

## The inventory is the backbone

Where `ux_review` is anchored on 3–7 journeys, a design pass is anchored on the
**token-and-component inventory** from Phase 0: the distinct values per axis, the primitives and
their forks, and the repeated functions that render differently across the surface. Every later
phase deltas against that inventory.

## Phases

| Phase | Topic                              | Why                                                                  |
| ----- | ---------------------------------- | -------------------------------------------------------------------- |
| 0     | Baseline & inventory               | The drift denominator: tokens, components, scale-sprawl, variant counts |
| 1     | Characterize the current language  | A faithful synthesis — the north-star's grounding, no problem-marking |
| 2     | North-star direction               | The target system: principles, fixed constraints, open axes          |
| 3     | Foundations                        | Type · color · shape · surface · spacing · sizing · motion → named scales |
| 4     | Component & state system           | One primitive per job; one shared state contract                     |
| 5     | Conformance & gap audit            | Every surface vs the north-star — *now* mark the problems            |
| 6     | Accessibility as a system          | Contrast / touch / keyboard / motion as token-level rules            |
| 7     | Codify & enforce                   | Write the language doc; add the lint + the agent contract            |
| 8     | Close-out                          | Delta table, simplification ledger, issues                          |

### Phase 0 — Baseline & inventory

Capture the starting state mechanically; it's the denominator Phase 8 deltas against.

- **Token audit.** Count values that *should* be tokens but are hardcoded — the drift
  denominator. `[web]` grep raw hex and off-scale px outside the theme file; `[tui]` `[cli]` grep
  literal ANSI codes / color names outside the central palette. A high count means the tokens
  exist but aren't the source of truth.
- **Scale sprawl.** Per axis, count the *distinct* values in use: radii, spacing steps, font
  sizes, font weights, shadow/elevation levels, motion durations. A long tail of one-off values
  per axis is the headline finding — it means there's no scale, only habits.
- **Component inventory.** List every shared primitive, and every place a panel / button / tile /
  label / menu is hand-rolled instead of reused. A primitive with three hand-rolled near-twins is
  three findings.
- **"One function, many looks" scan.** Pick 3–5 repeated functions (the primary action, a
  selectable item, a status indicator, an empty state) and list every visual variant of each in
  the wild. This is the consistency smell, made concrete.
- **Baseline table:**

  | Metric                                       | Baseline ({date}) |
  | -------------------------------------------- | ----------------- |
  | Hardcoded values outside the token file      | 73                |
  | Distinct radii / spacing steps / font sizes  | 6 / 19 / 11       |
  | Shared primitives / hand-rolled variants     | 5 / 14            |
  | Components covering all interaction states    | 2 / 9             |
  | Surfaces meeting the accessibility floor      | partial           |

### Phase 1 — Characterize the current language

A faithful synthesis of what exists, **not** a critique — the grounding the north-star evolves
*from*, so it reads as continuity, not a stranger. Read the existing `design_language.md` and the
theme file; describe the identity, type, color, components, and patterns as they are, and the
*intent* behind them. Problem-marking is Phase 5; here, only understand. (On an establishment pass
with no prior reference, this phase reverse-engineers the de-facto language from the shipped
surfaces.)

### Phase 2 — North-star direction

The generative phase — define the system the product grows into.

- **Separate fixed from open.** *Fixed:* the functional constraints that aren't matters of taste —
  fidelity guarantees (where the surface must mirror an external truth), platform conventions, the
  accessibility floor, and the stack it must build in. *Open:* everything aesthetic — palette,
  type, mood, shape, surface treatment. Name both explicitly so exploration doesn't trip a real
  constraint.
- **Draw it, where the project has a design canvas.** The north-star is a design round
  (`design_workflow.md` § *Round*): prepared, drawn and reviewed in the session, then
  reconciled. Treat the drawing as a strong draft.
- **Output: a north-star** the rest of the pass systematizes toward — a target look plus the
  principles behind it. Present 2–3 distinct directions when the solution space is wide and the
  maintainer should choose; commit to one with a written rationale otherwise.

### Phase 3 — Foundations

Turn each axis from sprawl into a **named scale**. The test for every axis: a consumer picks from
a token, and no value lands off-grid without a written reason.

- **Type** — families (two at most, one display accent), the weight set, a named size scale,
  line-height/measure, and the hierarchy each step maps to.
- **Color** — the chrome palette *plus* **semantic roles** (interactive / accent / positive /
  warning / danger / info / muted), so usage is principled rather than per-eye. Any
  fidelity-locked palette (game or brand semantics) stays fixed and is documented as such.
- **Shape** — a radius scale (a 4–5 step set), replacing ad-hoc radii.
- **Surface & elevation** — a depth model: surface levels, border treatments, shadows/glows,
  overlay scrims, and a z-layer order.
- **Spacing & layout** — a spacing rhythm and density model, the layout grid, and the breakpoint
  ladder with the responsive behavior per tier.
- **Sizing** — a control-size system (button / input / chip / tab heights, icon sizes, hit areas),
  chosen from a scale, not set per widget.
- **Motion** — duration and easing tokens and a transition vocabulary (hover, open/close,
  enter/exit), with a reduced-motion rule.

### Phase 4 — Component & state system

- **One primitive per job.** For each component family, designate the single primitive that owns
  the chrome; collapse the forks into it (the consumer supplies interaction and content, not a
  re-implementation of the frame). Coupled forks land together.
- **One state contract.** Define hover / focus / active / disabled / selected / loading / empty /
  error **once**, and apply it identically across every primitive. Divergent state handling is the
  root of "same function, different feel" — this contract is the cure. A component missing states
  (no empty, no loading) is a finding, not just an inconsistency.

### Phase 5 — Conformance & gap audit

*Now* mark the problems. Audit every surface against the north-star and the Phase 3–4 foundations.
Per divergence, the verdict is one of two directions (the same as documentation truth-up): **the
surface is wrong** (fix it to the token/primitive) or **the reference is stale** (update the
language). On an evolution pass this phase is the bulk of the work; on an establishment pass it's
light — there's little built yet to diverge. Findings go in the standard table with a severity
each; the fixes are action items, batched by surface or by token.

### Phase 6 — Accessibility as a system

Not a per-screen WCAG audit — the floor, baked into the foundations so conformance is automatic:

- **Contrast** designed *into* the color roles (text-on-surface combinations that pass at their
  intended sizes), not checked per label.
- **Touch / hit targets** sized *into* the control-size scale, so no control can be built too
  small.
- `[web]` `[desktop]` **Keyboard:** every interactive element reachable with a visible focus
  indicator from the token set; popups trap focus, close on `Esc`, and return focus.
  **Reduced-motion** honored by the motion tokens. Text scales without breaking the primary
  screens.
- `[tui]` `[cli]` Meaning never carried by color alone (`NO_COLOR` respected); a machine-readable
  mode for the scripted user.

The output is a set of *rules* in the foundations, plus findings where the current floor is
breached.

### Phase 7 — Codify & enforce

- **Write the resolved system into `design_language.md`** (the *what*): every token scale, the
  component inventory, the state contract, the canonical pattern per job, and the responsive
  rules. Update the theme/config file to match.
- **Self-enforcing rules, one assertion each:** a lint banning raw values outside the token file
  `[web]`; a snapshot of the token set; the component-reuse rule in `AGENTS.md`; and, where the project keeps a design file per screen, the screen-to-file pairing (`design_workflow.md` § *Round*). A doc is
  advisory; a lint holds the line — add it now that the system is worth enforcing.
- **Update the agent contract** (`AGENTS.md`) so every future UI edit, including
  AI-driven ones, conforms.

### Phase 8 — Close-out

- **Delta table** against Phase 0 — collapsing values and variants is the success signal:

  | Metric                                      | Baseline    | Final     | Δ        |
  | ------------------------------------------- | ----------- | --------- | -------- |
  | Hardcoded values outside the token file      | 73          | 4         | −69      |
  | Distinct radii / spacing / font sizes        | 6 / 19 / 11 | 4 / 8 / 6 | smaller  |
  | Shared primitives / hand-rolled variants     | 5 / 14      | 8 / 2     | unified  |
  | Components covering all states               | 2 / 9       | 9 / 9     | all      |

- **Simplification ledger**, same rules as the codebase review's: every collapse across phases in
  one ranked table (forked variants killed, off-grid values normalized, dead tokens removed). An
  empty ledger means the pass didn't look.
- **Decision register** — the plan's last section, per the shared machinery: competing directions
  the maintainer hasn't picked are rows there, not prose under a heading of the pass's own invention.
- **File issues** for deferred surface-conformance work per the issue-worthiness threshold; the
  plan stays in `docs/reviews/` permanently as the record the next pass picks up from.

## Severity, translated to the visual system

Same seven labels, same default actions — design meanings:

| Severity             | On the visual system, this means                                                          |
| -------------------- | ----------------------------------------------------------------------------------------- |
| **Real bug**         | A control is unusable: fails the accessibility floor — invisible focus, sub-floor contrast, a target too small to hit |
| **Drift**            | A surface contradicts the design language or its own other screens                        |
| **Drift-prone**      | Fine now, fragments as surfaces grow — a per-screen variant that will multiply            |
| **Cleanup**          | A forked or dead variant; an off-grid one-off with no reason                              |
| **Minor**            | A single off-token value, a slight misalignment                                           |
| **Acceptable**       | A deliberate, documented deviation (a fidelity-locked color, a justified one-off)         |
| **Decision pending** | Competing directions; the maintainer picks via a decision shape                           |

## When to update this template

Same rule as the set: two passes in a row needing the same hand-edit means the template is wrong —
fix it in the canonical gist.
