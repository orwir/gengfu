# Idea Gauntlet

From "I have an idea" to a committed project, a parked one, or an archived one with
a one-line reason. The gauntlet makes that call on evidence, as early as the
evidence allows: an evening on whether the idea is worth its cost, an evening
shaping it, then the verdict. A project that graduates carries kill criteria into
its repo, because projects die after they start, often of lost time or interest,
rather than at the idea. Research is cheap now (an agent does most of the
legwork), but paper buys little confidence: keep the gauntlet short and let a
small v0.1 in use settle the rest.

Companion to `project_kickstart.md`: the gauntlet ends exactly where the kickstart
begins. Every idea lives in your ideas home (see *The ideas home*), one folder per
idea, versioned and backed up from its first line; the method's README carries the
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
   decided. `IDEA.md` holds answers, verdicts and links; the research and the
   definitions live in the artifacts beside it.
2. **Decide as early as the evidence allows.** Any gate can be the verdict. Before a
   phase's research or shaping starts, name the question it must answer that could
   still flip the verdict; if there is none, skip that work and decide. Inside a
   phase, run the likeliest killer first. An idea still unclear after two evenings
   is the finding: cut it down or kill it. A kill before the build is a success: it
   cost evenings, not months.
3. **Kill on numbers, never on mood.** From Phase 1 on, every kill names its reason
   with its numbers; before that, a captured idea you stopped caring about can
   simply go. Four reasons end an idea before the build:
   - **solved by X:** an existing option already does the job;
   - **adapt X:** making the best option do the job (learning it, configuring it, a
     small script on top) costs less than building, found by using it rather than
     reading about it;
   - **blocked by Y:** research finds a hard blocker;
   - **not worth it:** the cost, up front and per month, outweighs the gain; an idea
     still unclear after two evenings is one.

   A spike adds a fifth: **disproved:** the riskiest assumption failed.
4. **Size the bet at capture, in sessions.** The appetite is the effort you're
   willing to spend on v0.1, counted in sessions: one sitting of focused work,
   about an evening. Free time comes in bursts, so budgets count sessions, never
   calendar time; the calendar only says when to check (rule 7). The appetite sets
   how deep the gauntlet goes
   ([Shape Up](https://basecamp.com/shapeup/1.2-chapter-03): an appetite starts
   with a number and ends with a design):

   | Appetite | What it is                                          | The gauntlet                                                        |
   | -------- | --------------------------------------------------- | ------------------------------------------------------------------- |
   | Toy      | Up to 3 sessions; nobody depends on it              | Phase 1, then the definitions and the verdict; no shaping, no spike |
   | Project  | Up to 15 sessions to v0.1                           | Every phase                                                         |
   | Product  | A project's, plus upkeep a month; others rely on it | Every phase, plus a real user's reaction before the verdict         |

5. **Set the bar before shaping, then structure before surface.** Before anything is
   shaped, write the bar in two halves: the value the shape must show against the
   best existing option, and the cost ceiling it must stay under. Then shape from
   the journeys Phase 1 wrote: the IA first, and a picture only where a walk of the
   journeys through the IA can't settle the bar. The verdict judges the result
   against the bar, not the feeling, and waits at least one night.
6. **A killed idea gets a log line, not a funeral.** Its folder moves to `archive/`,
   whole, with one line in `archived.md`. Re-opening requires new facts that beat
   the logged reason — renewed enthusiasm doesn't count.
7. **Graduation is a bet with terms.** The project carries its kill criteria into
   its repo: if-then lines, each a state and a budget of sessions. Annie Duke's
   *Quit* (2022) pairs each state with a date; free time comes in bursts, so here
   the pair is a budget, with a date only where the world sets one. The first is
   v0.1's budget, the circuit breaker: v0.1 not in use when it runs out re-runs the
   verdict instead of extending by default. A check comes every four weeks, and
   sooner when a budget runs out: it counts the sessions spent and ends in a call
   (continue, re-scope, park, done or dead; *The logs* says what each one writes),
   so no project drifts into dormancy unnoticed. A done project, its bet met and in
   use, is checked every twelve weeks for its use and its upkeep.
8. **WIP limit.** One idea in exploration at a time, carried to a verdict before the
   next is picked up. At most two running projects (`active` in `graduated.md`; a
   done project isn't one), counting an idea past its make verdict, toys included.
   A make verdict, or a parked or done project picked back up, over the limit
   names the project it parks. Raise either limit deliberately, never by accident.
   Captured and parked ideas are unlimited. A new project's real price is the time
   it steals from the projects you already run.
9. **No product code, no purchases before Phase 3.** Sketches, mock-ups and
   throwaway canvases are fine: they answer the cheap questions instead of skipping
   them. [hw] No parts orders before the Phase 3 spike defines the minimal shopping
   list.
10. **You decide; an agent can do the legwork.** Research, notes, draft CUJs, the
    IA, the walk sheets and the picture can be delegated, and so can the
    pre-mortem: a reader with no stake, who reads `IDEA.md` cold and argues for the
    kill, sees what you won't. The answers in `IDEA.md`, the bar, every verdict and
    every check are yours.

## The ideas home

One private repo holds every idea from capture to verdict, and the record of every
project that graduated from it; the host and the name are yours to pick. Each idea
is versioned and backed up from its first line, and its folder never has to carry
the final project name.

```
ideas/
├── AGENTS.md        where everything is, and how to capture, kill, graduate and check
├── archived.md      one line per killed idea -> its folder in archive/
├── graduated.md     one line per graduated idea -> its repo, status and next check
├── docs/guides/     the kit's method, shared by every idea
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
  `graduated.md` with its first check, four weeks out.
- **Check:** when a graduated project's next check comes due, count its sessions,
  read them against the bet in its `docs/product/vision.md`, and call it:
  continue, re-scope, park, done or dead. Its `graduated.md` row records the call
  (see *The logs*).

The root `AGENTS.md` carries these steps for any agent opened in an idea's folder.
A new home starts from this one, adjusted to taste:

```
# Ideas home

Every idea still being weighed lives here, run through the idea gauntlet in
`docs/guides/idea_gauntlet.md`, and `graduated.md` follows every project that left.
`docs/guides/` is a copy of the lifecycle kit's method. A guide changes here only
inside the work that found it, and goes to the kit when the work is approved, by the
steps in `docs/guides/README.md`.

## Layout

- `active/<idea>/`: every idea still in the gauntlet, parked ones included.
  `IDEA.md` is its state; its evidence sits beside it.
- `archive/<YYYY-MM-DD>-<idea>/`: killed ideas, moved here whole.
- `archived.md`: one line per killed idea. `graduated.md`: one per graduated idea,
  with its status and next check.

## Rules

- The answers in an idea's `IDEA.md`, its bar, every verdict and every check are
  the user's: never fill them or make the call. Do the legwork on request, inside
  that idea's folder.
- Facts about an idea go into its own files, not agent memory: memory is usually
  kept per repo, so here it's shared by every idea and stays behind at graduation.
- Commit at the end of each working session and push, so every idea stays backed
  up.

## Checks

At the start of a session, list the `graduated.md` rows whose next check has
passed. For each, show the bet from its project's `docs/product/vision.md` where
that can be read, and its sessions: the days with commits in its repo since the
last check and since graduation, for the user to correct. Then ask the user to
call each one: continue, re-scope, park, done or dead. Write the call into the row
as the guide's *The logs* says.

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
2. Here: `git rm -r active/<idea>`, add a line to `graduated.md` with a next check
   four weeks out, commit.
3. In the new repo: copy the guides into `docs/guides/` (`lifecycle upgrade --adopt`)
   and run the kickstart
   (gauntlet Phase 4).

## Re-open

Copy the archived folder back into `active/` (the archive stays as it was) and
cite its `archived.md` line in `IDEA.md`, with the new facts.
```

## Artifacts

What each phase leaves behind, inside the idea's folder. Every file sits where the
kickstart expects it, so graduation moves the folder as it is and copies nothing.
Every claim carries its source and date: research goes stale, and later work needs
to know how stale.

- **`IDEA.md`** (Phase 0) — the state: status, appetite, the answers, the bar, every
  verdict, links to the rest. It stays short: research and definitions go to the
  files below. Freezes as `docs/product/idea.md` at graduation.
- **`docs/product/competitors.md`** (Phase 1) — each existing option: what it does
  well, the specific reason it doesn't fit, what's worth borrowing; the baseline
  walk of the best one through each CUJ, and its adapt price. Phase 2 adds the side
  by side. Stays living: later UX reviews compare against it.
- **`docs/product/feasibility.md`** (Phase 1) — the assumptions by risk type, the
  riskiest marked; the cost in sessions and money; what research settled (data,
  APIs, platform limits, licences; [hw] parts, power, tolerances); open questions.
  Phase 3 adds the spike's plan, result and stack choice. After graduation it's
  the research record: the stack choice becomes the first ADRs, and the facts the
  code relies on move into `docs/architecture/` as it gets written.
- **`docs/product/vision.md`** (Phase 1) — the problem, who has it, and the CUJs;
  Phase 2 adds the done statement, IN/OUT and the IA; Phase 4 adds the bet. From
  graduation on it's the living intent doc: the IA review rewrites its IA section
  (`ia_review.md` § Phase 8), the UX review builds on its CUJs, and each check reads
  its bet.
- **`design/`** (Phase 2, optional) — only what the IA walk couldn't settle:
  low-fidelity flows on a design canvas, a CLI or bot transcript, [hw] sketches or
  photos of a cardboard mock-up. The first design rounds start from it, or from the
  IA where there is none (`design_workflow.md`).
- **`spike/`** (Phase 3) — throwaway code. Delete it any time: what it proved is in
  `feasibility.md`.

[hw] `docs/hardware/` joins at Phase 4.

## Template

Copy everything below into `active/<idea>/IDEA.md` in your ideas home and fill as
you go (the agent bootstrap in the method's README does this step for you).

```
# Idea: {{name}}

- Status: captured | phase 1 | phase 2 | phase 3 | PARKED until {{trigger}} | KILLED (see archived.md) | GRADUATED -> {{repo}}
- Type: software | hardware | mixed
- Appetite: toy | project | product — {{the sessions v0.1 may take}}
- Captured: {{YYYY-MM-DD}}

## Phase 0 — Capture (5 minutes)

- [ ] Problem — what hurts, concretely:
- [ ] Who has it, and how often ("just me" is a valid answer):
- [ ] Solution shape, one sentence:
- [ ] Appetite, in the header: toy, project or product. It sets how deep the
      gauntlet goes (the guide's rule 4).

No gate: the idea waits, captured, for a free evening — the wait is the cooldown.
Re-read it when you pick it up. No longer care? Leave it, or kill it.

## Phase 1 — Worth it? (an evening)

Research and the walk go to docs/product/competitors.md and
docs/product/feasibility.md, the CUJs to docs/product/vision.md. Likeliest killers
first.

- [ ] Existing options checked — OSS, paid products, [hw] off-the-shelf devices
      and kits. Best 2–3 candidates and the *specific* reason each doesn't fit:
      1.
      2.
- [ ] CUJs: the 2–4 journeys that matter, each with a success criterion and a
      frequency, in vision.md after the problem and who has it (format:
      ux_review.md § CUJs).
- [ ] The baseline walk: use the best option yourself through each CUJ — steps,
      decisions, time (method: ux_review.md § Phase 1). Then its adapt price:
      what it would take to make it do the job — learning it, configuring it, a
      small script on top:
- [ ] Feasibility: what research can settle — data and API access, platform
      limits, licences; [hw] parts availability, power, tolerances. What it can't
      settle goes to feasibility.md as open questions.
- [ ] Cost: does a useful v0.1 fit the appetite's sessions? Upkeep per month, in
      sessions and money — updates, data, support, hosting, paid APIs; [hw]
      power, consumables, the space it takes. Up front — [hw] rough BOM + tools
      I'd have to buy + parts lead time. Hardware iterations cost real money and
      shipping weeks: budget 2–3x the first guess.
- [ ] Portfolio: the running projects (graduated.md), and which one loses these
      sessions. "They can co-exist" is not an answer.
- [ ] Motivation: do I want the *result* or the *process*? Both are valid — but a
      process project gets scoped as a toy; a result project must beat the adapt
      price.
- [ ] Outside view: how did my last graduated projects go (graduated.md), and
      what makes this one different? With no record of my own, what failed
      open-source projects cite: lack of time or interest is a third of the
      stated causes (Coelho & Valente, 2017: https://arxiv.org/abs/1707.02327).
- [ ] Pre-mortem, best run by a reader with no stake: it's three months after
      graduation and the project is dead. Why?
- [ ] Assumptions by risk type, in feasibility.md: value (will it beat the adapt
      price in use?), usability, feasibility, viability (will the upkeep get
      paid?). The riskiest assumption:
      [sw] often value or viability; feasibility when data access, an API or a
      model limit is in play
      [hw] usually sensor accuracy, power budget, waterproofing, mechanical
      tolerances, or a skill I don't have yet

EARLY EXIT (rule 3): solved by X, adapt X, blocked by Y, or not worth it -> KILL,
with the numbers. Otherwise:

- [ ] The question Phase 2's shaping must answer that could still flip the
      verdict. None, or a toy -> Phase 2 writes the bar's cost half and the
      definitions, skips the IA, the walk and the picture, and goes to the GATE:

## Phase 2 — Shape it (an evening, no product code)

The definitions and the IA go to docs/product/vision.md, the comparison to
competitors.md.

- [ ] The bar, written before anything is shaped, in two halves —
      value: what the shape must show against the baseline walk (e.g. "clearly
      fewer steps than X on CUJ-1 and CUJ-2, and worse on none"):
      cost: the ceiling it must stay under (v0.1 within the appetite's sessions;
      upkeep under N sessions and M money a month):
- [ ] Done statement: "v0.1 is done when ____" — one sentence, observable.
- [ ] IN — the MVP, max 7 bullets; OUT — explicitly not in v0.1 (be generous).
- [ ] Constraints: money cap; deadline (only if real); [hw] where it physically
      lives and who else has to tolerate it — the household veto is real.
- [ ] [hw] Safety check: mains voltage, water near electronics, lithium cells,
      springs/solenoids under load? If yes -> prefer certified modules (relay
      boards, valve kits, charging boards) over raw builds, or explicitly accept
      it as a learning project and plan the precautions.
- [ ] The IA, one page, in the shape ia_review.md reads later: per CUJ, the
      reader and the decision they make (§ Phase 3); the objects, each with its
      attributes, actions, relationships and one home (§ Phase 2); the map of
      screens, commands and messages, [hw] controls, displays and alerts
      (§ Phase 5); the vocabulary (§ Phase 6).
- [ ] The IA walk: each CUJ through the IA, in text, with its steps and decisions
      counted. Then re-check the cost half: the objects, their states and the map
      are the honest size of v0.1.
- [ ] The picture, in design/, only for what the walk can't settle — often an
      interaction that is itself the edge: [sw] low-fidelity flows, or a CLI or
      bot transcript; [hw] a sketch or a cardboard mock-up, photographed.
- [ ] Side by side: the IA walk against the baseline walk, per CUJ, in
      competitors.md.
- [ ] Product appetite only: a real user's reaction to the IA walk, or without
      one to the CUJs and the done statement — what they do today, and what they
      would stop doing.

No user-facing surface -> no IA, no walk and no picture. Without an IA walk, for
whatever reason, judge the value half on the CUJs and the done statement.

GATE (the verdict, after at least one night's sleep): does it clear both halves of
the bar? Would I use it instead of the best option, adapted? Can I state the done
statement and the riskiest assumption from memory?
- Make -> Phase 3; a toy goes to Phase 4. Over the WIP limit -> name the project
  it parks:
- Park -> Status: PARKED until {{a date or an event}}. No trigger means it's a kill.
- Kill -> the folder moves to archive/, with a falsifiable reason in archived.md.

## Phase 3 — Spike (one to three sessions)

Budget: {{1–3}} sessions. [hw] Parts spend cap: {{amount}}.

- [ ] Prototype *only* what neither research nor the IA walk could settle —
      usually the riskiest assumption. Throwaway by definition, in spike/.
      [sw] a script or notebook, everything hardcoded; an interactive prototype
      when the risk is the interaction
      [hw] dev board + breadboard, cardboard, hand-operated before automated;
      simulate first where possible (Wokwi, Falstad); order the minimal parts,
      never the full BOM
- [ ] Stack / platform picked while spiking, reasons noted in feasibility.md —
      these become the project's first ADRs.
- [ ] Result, one line (the numbers and surprises go to feasibility.md):

GATE: the assumption survived? Yes -> Phase 4. No -> back to Phase 1 with what was
learned, or KILL with reason "disproved: ____".

## Phase 4 — Commit (handoff)

- [ ] The bet, in docs/product/vision.md: the appetite, the cost ceiling, and the
      kill criteria as if-then lines, each a state and a budget of sessions (a
      date only where the world sets one). The first is v0.1's, the circuit
      breaker: "if v0.1 isn't in use after {{N}} sessions, I re-scope or kill",
      never a quiet extension.
- [ ] Move this folder out of the ideas home into the project's own repo, under
      its real name, with the guides copied into its docs/guides/; add its line
      to graduated.md, with a next check four weeks out.
- [ ] [sw] Run docs/guides/project_kickstart.md (an agent can drive it), seeded
      from the artifacts: done statement -> v0.1 milestone; IN list -> first
      issues; the stack choice (the spike's, where there was one) and the
      decisions recorded here -> first ADRs.
- [ ] [hw] A repo anyway (firmware, code, docs), plus docs/hardware/: BOM with
      sources and prices, wiring diagram or labeled photo, build log,
      calibration notes.
- [ ] Set Status: GRADUATED with the repo link, then move this file to
      docs/product/idea.md. Frozen from here — the origins artifact, never
      edited again.

From here the kickstart playbook owns the process, and graduated.md's checks own
the bet.
```

## The logs

`archived.md` and `graduated.md` record the ideas home's two exits.

| Date       | Idea         | Died at | Reason (one line, falsifiable)                             | Folder                           |
| ---------- | ------------ | ------- | ---------------------------------------------------------- | -------------------------------- |
| 2026-06-01 | auto-waterer | Phase 1 | solved by a $25 drip timer; the gap isn't worth 6 sessions | archive/2026-06-01-auto-waterer/ |

| Graduated  | Idea   | Repo                  | Status | Next check |
| ---------- | ------ | --------------------- | ------ | ---------- |
| 2026-07-15 | plantr | github.com/you/plantr | active | 2026-08-12 |

- The reason must be falsifiable: not "too expensive" but "BOM ~€140 vs €25
  off-the-shelf".
- Re-opening an archived idea requires new facts against the logged reason (price
  dropped, a new library shipped, a skill acquired). Copy its folder back into
  `active/` (the archive stays as it was) and reference the old line in `IDEA.md`.
- A graduated idea's line is all that stays in the home; its repo owns everything
  else, including the frozen `IDEA.md` and the bet in its `vision.md`.

Graduating doesn't mean a project lives. A check comes due every four weeks, and
sooner when a budget in the bet runs out. It counts the project's sessions since
the last check and since graduation (days with commits in its repo, corrected for
work that left none), reads them against the bet, and writes its call into the
row. The line stays whatever the call: the record of what lived and what didn't
is the outside view for the next idea. A running project is one whose Status is
`active`.

| Call     | Status                    | Next check                                   |
| -------- | ------------------------- | -------------------------------------------- |
| continue | active                    | four weeks out                               |
| re-scope | active, the bet rewritten | four weeks out                               |
| park     | parked until {{trigger}}  | the trigger's date, or four weeks out to ask |
| done     | done, in use              | twelve weeks out                             |
| dead     | dead, called {{date}}     | none                                         |

A running project with no sessions since the last check can't continue: the call
is park, with a trigger, or dead. A park needs a trigger, as an idea's does: no
trigger means dead. Done means the bet is met and the project is in use, with
only upkeep left. Its check asks whether it is still in use and whether the
upkeep stayed within the bet's cost ceiling: it stays done, re-opens as a new bet
(re-scope, back to `active`), or is dead once nobody uses it. A next check is
always a date, so every row still in play comes due.

## When to update this file

Same rule as the rest of the set: if two ideas in a row needed the same hand-edit
to the template, the template is wrong — fix it in the kit, through the
copy of the project that found it.
