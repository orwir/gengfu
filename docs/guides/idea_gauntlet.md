# Idea Gauntlet

From "I have an idea" to a committed project, a parked one, or an archived one with
a one-line reason. The job of this document is to make that call once, on
evidence: an evening of research and definition, an evening drawing the product,
then the decision — so whatever survives gets built without second-guessing, and
whatever dies dies for a reason you can write down. Evidence is cheap now (an agent
does most of the legwork), so the verdict waits for it instead of being guessed at
capture.

Companion to `project_kickstart.md`: the gauntlet ends exactly where the kickstart
begins. Every idea lives in your ideas home (see *The ideas home*), one folder per
idea, versioned and backed up from its first line; the gist README carries the
agent bootstrap for setting the home up and capturing an idea into it. Each phase
leaves its evidence where the kickstart will read it (see *Artifacts*), so
graduation moves the folder out into its own repo as it is: `IDEA.md` freezes as
the origins artifact and the rest become the project's first docs. A killed idea
moves to the archive, whole.

Works for software, hardware, and mixed builds — a bot, a smart-home sensor, an
automatic plant waterer, a pinball machine. Items tagged `[sw]` / `[hw]` apply only
to that kind; untagged items always apply. Mixed projects take both.

## The rules

1. **One folder per idea, in your ideas home; `IDEA.md` is the state.** The `Status`
   line says where you are, the checkboxes say what is done, the gates say what was
   decided. The evidence lives in the artifacts beside it, linked from it: `IDEA.md`
   holds answers and links, not research.
2. **Decide once, on evidence, within two evenings.** Phases 1 and 2 take about an
   evening each and end in the one verdict: make, park or kill. Before it, stop only
   on evidence — an existing solution already covers it, or research finds a hard
   blocker. Inside a phase, run the likeliest killer first: existing solutions
   before definition, the competitor walk before your own picture. An idea still
   unclear after two evenings is the finding: cut it down or kill it. A kill at the
   decision is a success: it cost two evenings, not months.
3. **Set the bar before the picture.** After two evenings you're attached to your
   own sketch. Before drawing anything, write what the picture must show for the
   idea to be worth building; the decision judges the picture against that
   sentence, not the feeling, and waits at least one night.
4. **A killed idea gets a log line, not a funeral.** Its folder moves to `archive/`,
   whole, with one line in `archived.md`. Re-opening requires new facts that beat
   the logged reason — renewed enthusiasm doesn't count.
5. **WIP limit.** One idea in exploration at a time, carried to a verdict before the
   next is picked up; at most one idea past the decision. Raise either limit
   deliberately, never by accident. Captured and parked ideas are unlimited. A new
   project's real price is the time it steals from the projects you already run.
6. **No product code, no purchases before Phase 3.** Sketches, mock-ups and
   throwaway canvases are fine: they answer the cheap questions instead of skipping
   them. [hw] No parts orders before the Phase 3 spike defines the minimal shopping
   list.
7. **You decide; an agent can do the legwork.** Research, notes, the walk sheet,
   draft CUJs and the picture can be delegated. The answers in `IDEA.md`, the bar
   and every verdict are yours.

## The ideas home

One private repo holds every idea from capture to verdict; the host and the name
are yours to pick. Each idea is versioned and backed up from its first line, and its
folder never has to carry the final project name.

```
ideas/
├── AGENTS.md        where everything is, and how to capture, kill and graduate
├── archived.md      one line per killed idea -> its folder in archive/
├── graduated.md     one line per graduated idea -> its repo
├── docs/guides/     the gist mirror, shared by every idea
├── active/          every idea still in the gauntlet, parked ones included
│   └── <idea>/
└── archive/         killed ideas, moved here whole
    └── <YYYY-MM-DD>-<idea>/
```

- **Capture:** a new folder in `active/`, with `IDEA.md` from the template below.
- **Park:** it stays in `active/`; its `IDEA.md` says until when.
- **Kill:** the folder moves to `archive/`, prefixed with the date, and gets a line
  in `archived.md`.
- **Graduate:** the folder moves out into its own repo under the project's real
  name (`git subtree split` brings its history along), and gets a line in
  `graduated.md`.

The root `AGENTS.md` carries these steps for any agent opened in an idea's folder.
A new home starts from this one, adjusted to taste:

```
# Ideas home

Every idea still being weighed lives here, run through the idea gauntlet in
`docs/guides/idea_gauntlet.md`. `docs/guides/` mirrors the canonical gist: edit the
gist, never the mirror, and re-sync with the curl block in the gist's README.

## Layout

- `active/<idea>/`: every idea still in the gauntlet, parked ones included.
  `IDEA.md` is its state; its evidence sits beside it.
- `archive/<YYYY-MM-DD>-<idea>/`: killed ideas, moved here whole.
- `archived.md`: one line per killed idea. `graduated.md`: one per graduated idea,
  with its status.

## Rules

- The answers in an idea's `IDEA.md`, its bar and every verdict are the user's:
  never fill them or call a gate. Do the legwork on request, inside that idea's
  folder.
- Facts about an idea go into its own files, not agent memory: memory is usually
  kept per repo, so here it's shared by every idea and stays behind at graduation.
- Commit at the end of each working session and push, so every idea stays backed
  up.

## Capture

Check `archived.md` first: if this idea or a close variant was killed, show the
line; re-opening needs new facts against the logged reason. Then create
`active/<idea>/IDEA.md` from the template in the guide, with the name, the Captured
date and `Status: captured`. The folder takes the idea's working name.

## Kill

`git mv active/<idea> archive/<YYYY-MM-DD>-<idea>`, set `Status: KILLED (see
archived.md)`, add the line to `archived.md` in the user's words, commit.

## Graduate

1. Create the project's repo under its real name and give it the folder with its
   history: `git subtree split --prefix=active/<idea> -b graduate-<idea>`, then
   `git push <new repo> graduate-<idea>:main`.
2. Here: `git rm -r active/<idea>`, add a line to `graduated.md`, commit.
3. In the new repo: mirror the guides into `docs/guides/` and run the kickstart
   (gauntlet Phase 4).

## Re-open

Copy the archived folder back into `active/` (the archive stays as it was) and
cite its `archived.md` line in `IDEA.md`, with the new facts.
```

## Artifacts

What each phase leaves behind, inside the idea's folder. Every file sits where the
kickstart expects it, so graduation moves the folder as it is. Every claim carries
its source and date: research goes stale, and later work needs to know how stale.

- **`IDEA.md`** (Phase 0) — the state: status, the answers, the bar, every verdict,
  links to the rest. Freezes as `docs/product/idea.md` at graduation.
- **`docs/product/competitors.md`** (Phase 1) — each existing solution: what it does
  well, the specific reason it doesn't fit, what's worth borrowing. Phase 2 adds the
  walk through each CUJ and the side-by-side. Stays living: later UX reviews compare
  against it.
- **`docs/product/feasibility.md`** (Phase 1) — the assumptions, the riskiest marked;
  what research settled (data, APIs, platform limits, licences; [hw] parts, power,
  tolerances); open questions. Phase 3 adds the spike's plan, result and stack
  choice. After graduation it's the research record: the stack choice becomes the
  first ADRs, and the facts the code relies on move into `docs/architecture/` as it
  gets written.
- **`docs/product/vision.md`** (Phase 1) — the CUJs; Phase 2 adds the IA. Graduation
  adds the problem, the done statement and the v0.1 scope; from there it's the
  living intent doc.
- **`design/`** (Phase 2) — the picture: low-fidelity flows (a design canvas), a CLI
  or bot transcript, [hw] sketches or photos of a cardboard mock-up. The first
  design rounds start from it (`design_workflow.md`). A project with no user-facing
  surface has none.
- **`spike/`** (Phase 3) — throwaway code. Delete it any time: what it proved is in
  `feasibility.md`.

[hw] `docs/hardware/` joins at Phase 4.

## Template

Copy everything below into `active/<idea>/IDEA.md` in your ideas home and fill as
you go (the agent bootstrap in the gist README does this step for you).

```
# Idea: {{name}}

- Status: captured | phase 1 | phase 2 | phase 3 | PARKED until {{trigger}} | KILLED (see archived.md) | GRADUATED -> {{repo}}
- Type: software | hardware | mixed
- Captured: {{YYYY-MM-DD}}

## Phase 0 — Capture (5 minutes)

- [ ] Problem — what hurts, concretely:
- [ ] Who has it ("just me" is a valid answer):
- [ ] Solution shape, one sentence:

No gate: the idea waits, captured, for a free evening — the wait is the cooldown.
Re-read it when you pick it up. No longer care? Leave it, or kill it.

## Phase 1 — Explore (an evening: research, then definition)

Research, detailed in docs/product/competitors.md and docs/product/feasibility.md:

- [ ] Existing solutions checked — OSS, paid products, [hw] off-the-shelf devices
      and kits. Best 2–3 candidates and the *specific* reason each doesn't fit:
      1.
      2.
- [ ] Feasibility: what research can settle — data and API access, platform
      limits, licences; [hw] parts availability, power, tolerances. What it can't
      settle goes to feasibility.md as open questions.
- [ ] Build cost, honest guess: time (weekend / weeks / months); money —
      [hw] rough BOM + tools I'd have to buy + parts lead time. Hardware iterations
      cost real money and shipping weeks: budget 2–3x the first guess.
- [ ] Running cost: hosting / APIs / maintenance hours;
      [hw] power, consumables, the physical space it occupies
- [ ] Opportunity cost: which current project loses these hours?
- [ ] Motivation: do I want the *result* or the *process*? Both are valid — but a
      process project gets scoped as a toy; a result project must beat buying.

EARLY EXIT (the only verdict before Phase 2): an existing solution covers ~80% and
closing the gap costs more than living with it -> KILL "solved by X". Research
finds a hard blocker -> KILL "blocked by Y". Otherwise, define it:

- [ ] Done statement: "v0.1 is done when ____" — one sentence, observable.
- [ ] IN — the MVP, max 7 bullets:
- [ ] OUT — explicitly not in v0.1 (be generous here):
- [ ] Riskiest assumption — the single thing most likely to sink it (the others go
      to feasibility.md):
      [sw] usually feasibility, data access, an API or model limit — or the
      experience itself, when that is the edge over existing solutions
      [hw] usually sensor accuracy, power budget, waterproofing, mechanical
      tolerances, or a skill I don't have yet
- [ ] CUJs: the 2–4 journeys that matter, each with a success criterion, in
      docs/product/vision.md (format: ux_review.md § CUJs).
- [ ] Constraints: money cap; deadline (only if real); [hw] where it physically
      lives and who else has to tolerate it — the household veto is real.
- [ ] [hw] Safety check: mains voltage, water near electronics, lithium cells,
      springs/solenoids under load? If yes -> prefer certified modules (relay
      boards, valve kits, charging boards) over raw builds, or explicitly accept
      it as a learning project and plan the precautions.

## Phase 2 — See it (an evening, no product code)

- [ ] The bar, written before anything is drawn — what the picture must show for
      this to be worth building (e.g. "clearly fewer steps than X on CUJ-1 and
      CUJ-2, and worse on none"):
- [ ] The baseline: the best existing solution walked through each CUJ — steps,
      decisions, time — in competitors.md (method: ux_review.md § Phase 1).
- [ ] The picture, in design/: [sw] low-fidelity flows for each CUJ, with the IA
      (objects, vocabulary, screens) in vision.md; a CLI or bot draws a
      transcript; [hw] a sketch or a cardboard mock-up, photographed. No
      user-facing surface -> no picture; decide on Phase 1.
- [ ] Side by side: the picture against the baseline, per CUJ, in competitors.md.

GATE (the decision, after at least one night's sleep): does the picture clear the
bar? Would I use it instead of the best existing solution? Do I accept the running
cost, and does the IN list fit a few weekends? Can I state the done statement and
the riskiest assumption from memory?
- Make -> Phase 3.
- Park -> Status: PARKED until {{a date or an event}}. No trigger means it's a kill.
- Kill -> the folder moves to archive/, with a falsifiable reason in archived.md.

## Phase 3 — Spike (timeboxed)

Timebox: {{one evening – one weekend}}. [hw] Parts spend cap: {{amount}}.

- [ ] Prototype *only* what neither research nor the picture could settle —
      usually the riskiest assumption. Throwaway by definition, in spike/.
      [sw] a script or notebook, everything hardcoded
      [hw] dev board + breadboard, cardboard, hand-operated before automated;
      simulate first where possible (Wokwi, Falstad); order the minimal parts,
      never the full BOM
- [ ] Stack / platform picked while spiking, reasons noted in feasibility.md —
      these become the project's first ADRs.
- [ ] Result, one line (the numbers and surprises go to feasibility.md):

GATE: the assumption survived? Yes -> Phase 4. No -> back to Phase 1 with what was
learned, or KILL with reason "disproved: ____".

## Phase 4 — Commit (handoff)

- [ ] Move this folder out of the ideas home into the project's own repo, under
      its real name, with the guides mirrored into its docs/guides/; add a line to
      graduated.md.
- [ ] [sw] Run docs/guides/project_kickstart.md (an agent can drive it), seeded
      from this file and its artifacts: done statement -> v0.1 milestone, IN list
      -> first issues, the spike's stack choice and the decisions recorded here ->
      first ADRs.
- [ ] [hw] A repo anyway (firmware, code, docs), plus docs/hardware/: BOM with
      sources and prices, wiring diagram or labeled photo, build log,
      calibration notes.
- [ ] Complete docs/product/vision.md: problem, done statement, IN/OUT as v0.1
      scope, next to the CUJs and the IA already there. competitors.md and
      feasibility.md stay as they are.
- [ ] Set Status: GRADUATED with the repo link, then move this file to
      docs/product/idea.md. Frozen from here — the origins artifact, never
      edited again.

From here the kickstart playbook owns the process.
```

## The logs

`archived.md` and `graduated.md` record the ideas home's two exits.

| Date       | Idea         | Died at | Reason (one line, falsifiable)                             | Folder                           |
| ---------- | ------------ | ------- | ---------------------------------------------------------- | -------------------------------- |
| 2026-06-01 | auto-waterer | Phase 1 | solved by a $25 drip timer; the gap isn't worth 3 weekends | archive/2026-06-01-auto-waterer/ |

| Graduated  | Idea   | Repo                  | Status |
| ---------- | ------ | --------------------- | ------ |
| 2026-07-15 | plantr | github.com/you/plantr | active |

- The reason must be falsifiable: not "too expensive" but "BOM ~€140 vs €25
  off-the-shelf".
- Re-opening an archived idea requires new facts against the logged reason (price
  dropped, a new library shipped, a skill acquired). Copy its folder back into
  `active/` (the archive stays as it was) and reference the old line in `IDEA.md`.
- A graduated idea's line is all that stays in the home; its repo owns everything
  else, including the frozen `IDEA.md`.
- Graduating doesn't mean a project lives. When one stops, change its status in
  `graduated.md` (active, dormant or dead, with the date it was called) instead of
  removing the line.

## When to update this file

Same rule as the rest of the set: if two ideas in a row needed the same hand-edit
to the template, the template is wrong — fix it here.
