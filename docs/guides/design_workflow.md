# Design workflow

The **how** of design work across a project's life — the companion to `design_language.md`
(the **what**: tokens, components, patterns). This doc owns *when* design happens and *how*
it's run; the language doc owns the resolved system; `design_review.md` is the runnable pass
that produces and evolves it.

Sits downstream of `project_kickstart.md` § *User-facing surface foundation*, which says set
the foundation before screen 2 and hands the detail here. Surface-agnostic: applies to any
surface a human drives — web, desktop GUI, TUI, and even a plain CLI (which has a design
language too: verb/flag naming, output modes, color-and-symbol scheme, error + exit-code
shape). Skip only a truly headless service.

## Principle: the language is built and evolved, not just audited

A surface accretes screens faster than it accretes consistency. Left alone, every screen
invents its own spacing, its own button, its own empty state — and by the time the drift is
*felt*, retrofitting consistency across a dozen screens costs a dozen times what setting it up
once would have. The cure is to treat the design language as a thing with a lifecycle: **found**
it before the second screen, **codify** every design round back into it, and **evolve**
it on a trigger — not when the pain finally surfaces.

The three moments below are that lifecycle. Most projects skip straight to "build screens" and
discover the gap mid-flight; the trigger table exists so the evolve step fires on a schedule,
not on frustration.

## The three moments

### 1. Found — before screen 2

Conventions cascade; screens don't. Set the foundation while there's one screen to conform, not
twelve. In order:

1. **Pick the framework + a component/widget vocabulary** so you inherit accessible, themeable
   primitives instead of hand-rolling them. Record the choice in an ADR. (Web → a component
   library; desktop → a widget toolkit; TUI → Textual / Bubble Tea / ratatui; CLI → an arg
   parser + an output/formatting helper.)
2. **Set the tokens once, centrally**, in one theme/config file so a change cascades. The token
   set, by surface:
   - *Web / desktop / TUI:* two font families at most (one UI, one mono for ids/numerics),
     **semantic** color tokens only (no raw hex), a spacing rhythm, a radius scale, an
     elevation/depth model, and motion (duration/easing) tokens — from day one.
   - *Plain CLI:* consistent verb/flag naming, a small fixed set of output modes (human ·
     `--json` · `--quiet`), one color-and-symbol scheme, a uniform error + exit-code convention.
3. **Write the one-page `design_language.md`** (the *what*): the component/output inventory, how
   each kind of data renders (ids, timestamps, money, counts, status), and the one canonical
   pattern per job (destructive confirm, empty state, status indicator, pagination/scroll).
4. **Enforce it in the agent contract**, not just the doc: a rule in `AGENTS.md` that *any* UI
   edit conforms to the reference — reuse what ships, don't fork a parallel variant. A doc is
   advisory to a model; a first-read MUST-rule holds the line. A lint (raw-hex ban outside the
   token file) is the only hard guarantee, usually worth adding the day a second surface appears.

**Don't build a design system here.** At a handful of screens and one maintainer, a token
package / Storybook / style crate costs more than it returns. The one-page reference + the
enforced contract *is* the system until the evolve trigger fires.

### 2. Round — per change to what a screen shows

The design canvas runs inside the coding session: the assistant that writes the code opens the
screen's design file, draws on it, and publishes it for the maintainer to review in place
(on whatever canvas the assistant can draw on and publish from). A round is one conversation with the
codebase, the tokens and the tracker in reach, so nothing is copied between tools and no
message is shaped for a reader on the other side. Use it for every change to what a screen
shows — a new screen, a restructure, a line of copy — because a look agreed in words can still
look wrong on the screen: the drawing is part of the change's plan and is approved with it. The
canvas is rich for web and desktop, thinner for TUI/CLI. A project that came through the
gauntlet starts with its IA in `docs/product/vision.md`, and sometimes a low-fidelity picture in
`design/`: its first rounds draw from the IA and bring those files to full fidelity instead of
drawing beside them. The loop:

1. **Prepare.** Settle what the drawing must respect before anything is drawn: the *fixed*
   functional constraints (fidelity guarantees, platform conventions, the accessibility floor,
   the stack it must build in), the *open* axes, the shipped tokens and primitives, and what's
   out of scope. Search the tracker: a gap already filed, even one whose issue rejected the fix
   the drawing would propose, is a question for the maintainer, not a new ticket. Where the
   stack's ability is in doubt — a custom title bar, a control the toolkit lacks — prototype
   the mechanism first, so the round never draws what can't be built.
2. **Draw.** Re-read the design file first: the maintainer may have edited it by hand. Draw the
   target look *and* every state the work needs at real scale — first run, errors, the longest
   locale, both themes — plus the foundations behind it (a states/components gallery), from the
   shipped tokens rather than adjectives. What the round adds waits on a page of its own until
   the close, and a note says only what the drawing can't (both below); the case for the design
   goes in the conversation. Competing variants sit side by side, or behind a toggle set apart
   from the state parameters (below), until the maintainer picks: a variant among the states
   passes for one. A prototype simulates only what is judged by feel — a timer, a sync, a
   drag — and reaches every other state through a toggle that draws it: a prototype that
   re-implements the app's logic has to be ported again at every close.
3. **Review.** End the round with a short summary in the conversation: what changed on the
   canvas, and each open decision as a question with a recommended answer. The maintainer
   judges by eye on the canvas, corrects, and approves the drawing with the plan it belongs
   to, in one approval; nothing is built before it.
   A picked variant collapses into the screen's one file, and the variants that lost go. Where
   the pick settled a genuine fork — two defensible layouts for one region, two interaction
   models for one action — the file's notes keep one line on the option not taken: what it
   was, why it lost, and where the decision is recorded. The line, not a drawing, stops the
   option being proposed again in six months: it sits on the file every round reads, while a
   drawing of the loser is frozen at the pick, drifts from the system around it, and reads as
   a state of the screen.
4. **Build and reconcile.** The approved design is a **strong draft, not a spec**: reuse the
   *shipped* primitives over its near-duplicates, drop embellishments that fight the product,
   and let the platform overrule the drawing where it must — a control's real metrics, chrome
   the OS won't allow, copy too long for its space in the real font. The canvas proposes; the
   language disposes. Keep a **ledger** of every place the build departs from the design, and
   why: the hand-back names each one, and the close is drawn from it.
5. **Codify.** Fold the surviving deltas (a new token, a new pattern) back into
   `design_language.md` so the next screen inherits them — never let a screen carry private
   styling the reference doesn't know about. That codification is what keeps moment 2 from
   silently re-opening the drift moment 1 closed. **Record the pairing too** — one row per
   shipped screen naming the design file that draws it and the source files that decide what
   it draws. Two checks over that table are worth a test: every shipped screen has a row, so
   a screen cannot ship undrawn, and no two rows claim the same screen.
6. **Close.** Before the hand-back, bring the design file to the code the change will ship:
   redraw each delta from the ledger, collapse each decided fork to its line in the notes,
   move what the round added to its surface's page, fold in or delete every other file the
   round made (a spec for work still open stays until it ships), cut the notes back to what a
   note holds, and say so in one line. The maintainer then reads the drawing and the code
   together, and the close lands with the change. A prototype takes only the deltas it
   simulates; logic with no look or feel of its own stays in the code. The design file claims
   to be the current screen, so a delta left out is drift on the day it ships, and the next
   round on the screen starts from it.

A change that moves a screen's code without changing what it shows draws nothing: its plan
says so, and its close only re-stamps the pairing.

**One round per screen, from its first redraw to its close.** A round puts the design file
ahead of the code until the change ships and closes, and the pairing's stamp hashes only the
code, so nothing on the codebase side sees that. Three rules keep the window honest:

- No second round on the screen while one is open: a close made mid-round would vouch for a
  design file that also draws unshipped changes.
- A round dropped before its close — declined at review, or abandoned before it ships — is
  reverted on the canvas to the design file as it stood before the round. So is a design file
  closed by a change that then does not ship: its pre-close version comes back from the
  canvas's history.
- A re-stamp the screen's code needs meanwhile, from a change that leaves its look alone, goes
  ahead: it vouches only for the screen's own file, and the round's additions wait on their page
  until its close.

**Close before the hand-back, and let the gate hold it.** The maintainer approved the drawing
with the plan, and at the hand-back judges the code and the closed design file together, so the
file is drawn from the code the change will ship, with the ledger only a guide to it. A
correction to what the screen shows re-opens the close before the change lands, whether or not
it moves the code the pairing hashes: copy kept in a catalog moves no stamp. A check on every
push and change request names each pairing whose code that change moved since its stamp. The
same close re-stamps a screen whose code moved without changing what it shows — nothing to
redraw — so no stale pairing waits for a release.

**One surface, one design file.** The second check above is the rule made mechanical, and it
is worth the machinery because the alternative is seductive. When a change touches one region
of a large screen, drawing that region as its own file keeps a single review small — so it
gets done, and the surrounding regions get stubbed. The file is then a claim about a screen
that does not exist: a reader opens two files to learn one, and the stubbed regions are
reviewed again when the two are finally merged. Scope belongs in the round, which names the
region it redraws, not in a file boundary. Two exceptions expire on their own — competing
variants of one screen under review at the same time, which collapse when the maintainer
picks, and a spec for unshipped work, which cannot merge into a file claiming to be the current
state without making it lie, and folds in when the work ships.

**Arrange the canvas by surface, never by round.** A round that adds its boards where the last
one stopped files the canvas by history: one screen's states scatter across every round that
touched it, and a board that grows shoves everything after it. Give each surface a page (a
file, where the tool has no pages), with its states grouped by the region of the screen they
change (below), and the foundations a page of their own. What an open round adds waits on a page of
its own, so the review sees the round alone; the close moves each board into its surface's
group and removes the page. History lives in the canvas's versions and the tracker, never in
the layout. A board per state that imports the surface's file shows states side by side at no
upkeep, since it follows the file; toggles alone suit a surface whose combinations multiply
(viewport × theme × role × locale). A new surface or platform gets a canvas of its own; split
one only as it heads toward hundreds of boards.

**Lay out every design file the same way**, so a reader learns the frame once and judges only
the screen. Two kinds of parameter drive it, and they never share a control. A *view parameter*
changes how the same state renders: viewport, theme, locale. A *state parameter* changes which
state the screen is in: who reads it, what the data holds, what was just done. Top to bottom:

- **Backdrop:** one neutral tone behind every frame, outside both themes, as a photo editor's
  pasteboard is mid-grey, so the frame's edge shows in light and dark alike.
- **Head:** the screen's name and its route, nothing more; the pairing keeps its sources.
- **View bar:** the view parameters, the same ones in the same order and words on every file,
  pinned while the file scrolls. A surface with one locale says it has one.
- **Frame:** the screen at the chosen viewport, full height, in its real chrome. It never
  scrolls inside itself: the file scrolls, and the frame sideways only on a narrower window.
- **State rail:** beside the frame, pinned, with a scroll of its own; above the frame when the
  window cannot hold both. Scenarios first, then the reader and any variable that changes the
  whole screen (a tier, a plan), then one row per variable, grouped under the region of the
  screen it changes, in the screen's order. An overlay (a dialog, a gate, a panel) is a row of
  the region that opens it.
  - A row holds two to five options, the default first; past five it is two variables.
  - A row that does not apply now is disabled with its reason, never removed, so the rail does
    not shift under the cursor.
  - Two rows that fill one slot (the banner, the open overlay) never both light: picking one
    clears the other or disables it with its reason, so the rail shows only what the frame
    draws.
  - An option that sets two variables at once is a scenario: up to six named presets for the
    combinations that tell a story. The one last picked stays marked as changed once a row
    moves off it, so the reader still sees where they started.
- **Galleries:** a variable whose values differ only in copy (banners, refusals, toasts) is
  drawn together under the frame, stacked at the frame's width, instead of as options, since
  copy is judged by reading it together and the longest locale against the rest.
- **Notes**, last (*What a note holds*).

Build the bar and the rail once, in the template every file starts from, and let each file
supply only its values: a control bar each file builds for itself grows its own words for the
same control. A canvas of static frames keeps the same split, with the view parameters as its
columns or variable modes and the state parameters as its rows.

**A pairing with a stamp tells you when it went stale.** Hash each row's sources into the row
when its design file closes; a script that recomputes the hash then names the design files
that have fallen behind the code, and the reading is free and repeatable. Stamp a row only when
its own design file closes, never in bulk, and only if its sources haven't moved since the
close was drawn: a bulk stamp marks screens nobody closed as current. A design file that draws
several screens closes for all of them at once. Without a stamp, drift is discovered by audit —
which is to say, long after the cheapest moment to fix it. Gate each change on the pairings
it moved, and the whole table at the release. The change that moved a screen closes it, which
is a bare re-stamp when the look did not change, so the gate spends a redraw only where the
design moved, and a pairing someone else left stale blocks a change only through code the
two share.

**What a note holds.** One notes card per surface, six to nine short fields, each something a
reader of the drawing would otherwise get wrong:

- a decision that looks arbitrary: the call, and why it is load-bearing (would a competent
  developer tidy it away?);
- an absence that was chosen, a decided fork's losing option among them: what was considered,
  why it lost, and where the decision is recorded;
- what a value is of — its unit, window or source — where the drawing can't say;
- what a failure looks like: which state is empty, which unavailable, and why they differ;
- a gate: what arms it, and where it is enforced;
- a caveat that is copy, not a tooltip;
- how to reach the drawn states: which toggle shows which;
- at most one **UNSETTLED** question, and only where its answer changes what ships.

Behaviour, timings, formats and data shapes are the repo's to state: a note that needs one names
the doc section or the issue, since a copy on the canvas goes stale on the next merge and no
review sees it go. A note may say more while its round is open; the close cuts it back.

**Keep no log beside the design.** The canvas's versions, the commits and the issues already
hold the history, so a record of rounds kept beside them — in a file on the canvas, in its
notes, in the assistant's memory — is a copy that grows every round and is never read whole.
Keep the current state only: each surface's pairing and stamp, and any round still open. An
index of design files names each file and its surface in one line, never a summary of what a
page draws or decides: a summary kept in two places drifts.

**Where this comes from.** Figma's own guidance files work by product surface, keeps longer
documentation outside the design file, splits a file heading toward hundreds of frames per
platform, and leaves arranging frames to the end — "any earlier and you'll re-organize with
every iteration" ([Figma](https://www.figma.com/best-practices/team-file-organization/)).
thoughtbot settled on one file mirroring production, with each ticket's work in a file of its
own, after its lists of explorations and future designs "got out of hand"
([thoughtbot](https://thoughtbot.com/blog/how-we-used-designops-and-figma-to-scale-our-team)).
GitLab goes further than a notes card: notes "should be temporary in nature, and not need to
persist" ([GitLab](https://design.gitlab.com/get-started/uik-annotation)).

**Where the layout comes from.** Storybook draws the line between the two kinds of parameter:
globals such as theme, locale and viewport apply across stories from a toolbar, while args are
one story's inputs ([Storybook](https://storybook.js.org/docs/essentials/toolbars-and-globals)), and Chromatic
saves combinations of the globals as named modes ([Chromatic](https://chromatic.com/docs/modes)).
Teams that keep explorations move them off the working file into an archive
([Viget](https://www.viget.com/articles/figma-workflow-on-the-viget-product-design-team/));
on a canvas with history, its versions are that archive.

### 3. Evolve — when the trigger fires

When the foundation strains, run a **`design_review.md`** pass: the structured, multi-phase
process that re-characterizes the current language, sets a north-star, systematizes the
foundations (type · color · shape · surface · spacing · sizing · motion), unifies the component
& state vocabulary, audits every surface for conformance, and re-codifies `design_language.md`.
The pass is both the first-time establishment (if *Found* was skipped — and it usually was) and
the periodic evolution.

## Triggers — when to run a pass

| Run a `design_review` pass when…                                                        | Why                                                                                                                              |
| --------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------- |
| The screen/surface count has roughly **doubled** since the language was last set         | Conventions set for 3 screens fray at 12; the doubling is the cheapest schedulable proxy for "enough drift to be worth a pass"   |
| A **second surface or platform** appears (web + desktop, CLI + TUI)                      | Two surfaces sharing one product need one language; the moment the second lands is when the shared vocabulary must go explicit   |
| **Pre-release polish**, before opening to strangers                                      | Pair it with a `ux_review`: the UX review finds journey friction, the design pass finds systemic incoherence — same release gate |
| A **felt inconsistency a single PR can't fix** — "this one function looks different in three places" | Systemic drift is a language problem, not a screen problem; patching screens one at a time re-grows it               |
| The **first release prep after a quarter** with no pass — a recount, not a pass                | Re-run the last pass's Phase 0 counts against its delta. A new value that maps to an existing token is a direct conformance fix; a pass follows only for growth that needs a new token or primitive |

**Don't run a pass for:** mid-build churn on a single screen, or a *single* divergence from the
reference — that's a direct conformance fix (or a `ux_review` Phase 4 finding), not a rebuild of
the language. A pass reviews a reasonably stable target; don't run one in the middle of an active
redesign of the same surface.

## The doc map

| Doc                          | Answers                                                         | Cadence                               |
| ---------------------------- | -------------------------------------------------------------- | ------------------------------------- |
| `design_workflow.md` (this)  | *When and how* does design happen?                             | Read at founding; re-read at each trigger |
| `design_language.md`         | *What* is the resolved system — tokens, components, patterns?  | Living; updated on every round and pass   |
| `design_review.md`           | The runnable *pass* that establishes/evolves the language      | Run on a trigger                      |
| `ux_review.md`               | Do the *journeys* work — friction, bloat, usability?           | Per minor release / quarterly         |
| `ia_review.md`               | Is the *information* in the right place, for whose decision?   | Before a redesign or opening to strangers |

`design_language.md` is **this project's own** artifact (its real tokens); the other four are
reusable templates. Keep the same shape for a TUI or a desktop app — the tokens differ, the
lifecycle doesn't.
