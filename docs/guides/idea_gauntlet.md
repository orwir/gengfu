# Idea Gauntlet

From "I have an idea" to a committed project — or to a one-line entry in the kill
log. The job of this document is to kill weak ideas as early and as cheaply as
possible, so whatever survives gets built without second-guessing.

Companion to `project_kickstart.md`: the gauntlet ends exactly where the kickstart begins — in the same folder. An idea starts as a project folder containing `IDEA.md` (this template, filled) and the mirrored doc set in `docs/guides/`; the gist README carries the agent bootstrap for setting that up from a single link. Survive the gates and `IDEA.md` graduates into the repo as the origins artifact; fail them and the folder gets deleted — a one-line kill-log entry is all that remains.

Works for software, hardware, and mixed builds — a bot, a smart-home sensor, an
automatic plant waterer, a pinball machine. Items tagged `[sw]` / `[hw]` apply only
to that kind; untagged items always apply. Mixed projects take both.

## The rules

1. **One folder per idea, one file in it.** A new project folder holds `IDEA.md`
   (the template below, filled) — bootstrap it by pointing an agent at the gist
   (see the gist README) or copy it by hand. The file *is* the state: the `Status`
   line says where you are, the checkboxes say what is done. If the idea dies,
   delete the folder; the kill-log line is the only survivor.
2. **Work top-down; stop at the first failed gate.** Every phase ends with a gate —
   a yes/no question. No yes → no next phase. A failed gate is a success: it cost
   hours, not months.
3. **Phases are ordered by kill-probability-per-hour.** The cheap, likely killers
   (cooldown, existing solutions) run before anything expensive (prototypes, parts
   orders).
4. **A killed idea gets a log line, not a funeral.** One line in the kill log.
   Re-opening requires new facts that beat the logged reason — renewed enthusiasm
   doesn't count.
5. **WIP limit.** At most one idea past Phase 2 at a time (raise the limit
   deliberately, never by accident). Phase 0 parking is unlimited. A new project's
   real price is the time it steals from the projects you already run.
6. **No code, no purchases before Phase 3.** Building feels like progress and skips
   the cheap questions. [hw] No parts orders before the Phase 3 spike defines the
   minimal shopping list.

## Template

Copy everything below into `IDEA.md` at the project-folder root and fill as you go
(the agent bootstrap in the gist README does this step for you).

```
# Idea: {{name}}

- Status: parked | phase 1 | phase 2 | phase 3 | KILLED (see kill log) | GRADUATED -> {{repo}}
- Type: software | hardware | mixed
- Captured: {{YYYY-MM-DD}}

## Phase 0 — Capture (5 minutes)

- [ ] Problem — what hurts, concretely:
- [ ] Who has it ("just me" is a valid answer):
- [ ] Solution shape, one sentence:

GATE (cooldown): re-read after >= 1 week. Still want it? Yes -> Phase 1.
No -> stays parked, or kill it.
Skip the cooldown only for a hard external deadline — write down what it is.

## Phase 1 — Worth it? (1–2 hours, research only)

- [ ] Existing solutions checked — OSS, paid products, [hw] off-the-shelf devices
      and kits. Best 2–3 candidates and the *specific* reason each doesn't fit:
      1.
      2.
- [ ] Build cost, honest guess: time (weekend / weeks / months); money —
      [hw] rough BOM + tools I'd have to buy + parts lead time. Hardware iterations
      cost real money and shipping weeks: budget 2–3x the first guess.
- [ ] Running cost: hosting / APIs / maintenance hours;
      [hw] power, consumables, the physical space it occupies
- [ ] Opportunity cost: which current project loses these hours?
- [ ] Motivation: do I want the *result* or the *process*? Both are valid — but a
      process project gets scoped as a toy; a result project must beat buying.

GATE: an existing solution covers ~80% and closing the gap costs more than living
with it -> use/buy it, KILL with reason "solved by X". Otherwise -> Phase 2.

## Phase 2 — Define (1–2 hours)

- [ ] Done statement: "v0.1 is done when ____" — one sentence, observable.
- [ ] IN — the MVP, max 7 bullets:
- [ ] OUT — explicitly not in v0.1 (be generous here):
- [ ] Riskiest assumption — the single thing most likely to sink it:
      [sw] usually feasibility, data access, an API or model limit
      [hw] usually sensor accuracy, power budget, waterproofing, mechanical
      tolerances, or a skill I don't have yet
- [ ] Constraints: money cap; deadline (only if real); [hw] where it physically
      lives and who else has to tolerate it — the household veto is real.
- [ ] [hw] Safety check: mains voltage, water near electronics, lithium cells,
      springs/solenoids under load? If yes -> prefer certified modules (relay
      boards, valve kits, charging boards) over raw builds, or explicitly accept
      it as a learning project and plan the precautions.

GATE: I can state the done statement and the riskiest assumption from memory, and
the IN list fits a few weekends. Bigger -> cut or split. Can't name the risk ->
not ready to build.

## Phase 3 — Spike (timeboxed)

Timebox: {{one evening – one weekend}}. [hw] Parts spend cap: {{amount}}.

- [ ] Prototype *only* the riskiest assumption. Throwaway by definition.
      [sw] a script or notebook, everything hardcoded
      [hw] dev board + breadboard, cardboard, hand-operated before automated;
      simulate first where possible (Wokwi, Falstad); order the minimal parts,
      never the full BOM
- [ ] Stack / platform picked while spiking, reasons noted — these become the
      project's first ADRs.
- [ ] Result, one line:

GATE: the assumption survived? Yes -> Phase 4. No -> back to Phase 2 with what was
learned, or KILL with reason "disproved: ____".

## Phase 4 — Commit (handoff)

- [ ] [sw] Run docs/guides/project_kickstart.md (an agent can drive it), seeded
      from this file: done statement -> v0.1 milestone, IN list -> first issues,
      the spike's stack choice -> first ADRs.
- [ ] [hw] A repo anyway (firmware, code, docs), plus docs/hardware/: BOM with
      sources and prices, wiring diagram or labeled photo, build log,
      calibration notes.
- [ ] Seed docs/product/vision.md from this file (problem, done statement,
      IN/OUT as v0.1 scope) — vision.md is the living intent doc.
- [ ] Set Status: GRADUATED with the repo link, then move this file to
      docs/product/idea.md. Frozen from here — the origins artifact, never
      edited again.

From here the kickstart playbook owns the process.
```

## Kill log

One file that must outlive deleted project folders — keep it with your personal
notes (a private gist or notes repo), never inside a project: `_kill_log.md`.

| Date       | Idea         | Died at | Reason (one line, falsifiable)                             |
| ---------- | ------------ | ------- | ---------------------------------------------------------- |
| 2026-06-01 | auto-waterer | Phase 1 | solved by a $25 drip timer; the gap isn't worth 3 weekends |

- The reason must be falsifiable: not "too expensive" but "BOM ~€140 vs €25
  off-the-shelf".
- Re-opening a killed idea requires new facts against the logged reason (price
  dropped, a new library shipped, a skill acquired). Start a fresh folder and
  reference the old log line.

## When to update this file

Same rule as the rest of the set: if two ideas in a row needed the same hand-edit
to the template, the template is wrong — fix it here.
