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
it before the second screen, **codify** every design-tool handoff back into it, and **evolve**
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

### 2. Handoff — per new screen, restructure, or change judged by eye

Where a design/prototyping tool exists (rich for web, thinner for TUI/CLI), use it for new
screens and restructures, then translate its output. The loop:

1. **Brief.** Write a specialized prompt: the mission, the *fixed* functional constraints
   (fidelity guarantees, platform conventions, the accessibility floor, the stack it must build
   in), the *open* axes, a faithful digest of the current language, the deliverables, and what's
   explicitly out of scope. A grounded brief beats a generic one — hand the tool your real tokens
   and primitives, not adjectives. Say what the tool cannot see, too — usually the issue
   tracker: from inside the tool, a gap already filed, even one whose issue rejected the fix it
   would propose, looks like a gap nobody filed. Have it raise a gap as a question for you to
   check rather than draft the ticket.
2. **Generate.** Let the tool explore. Ask for the target look *and* the foundations behind it
   (a states/components gallery), not just hero screens. Its last message is the **handoff
   block** below — a map of what it changed, read before the design itself.
3. **Reconcile.** Treat the handoff as a **strong draft, not a spec**: reconcile invented data
   and backend assumptions, reuse your *shipped* primitives over its near-duplicates, and drop
   embellishments that fight the product. The tool proposes; the language disposes. Keep a
   **ledger** as you go — every place the build departs from the design, and why — because
   the close-out is written from it.
4. **Codify.** Fold the surviving deltas (a new token, a new pattern) back into
   `design_language.md` so the next screen inherits them — never let a screen carry private
   styling the reference doesn't know about. That codification is what keeps moment 2 from
   silently re-opening the drift moment 1 closed. **Record the pairing too** — one row per
   shipped screen naming the design file that draws it and the source files that decide what
   it draws. Two checks over that table are worth a test: every shipped screen has a row, so
   a screen cannot ship undrawn, and no two rows claim the same screen.
5. **Close.** Once the change has shipped, send the tool the **close-out**, and have it bring
   the design file to what shipped: each delta from the ledger redrawn, each decided fork
   collapsed, every other file the round made folded in or deleted (a spec for work still
   open stays until it ships), the notes brought up to date. The design file claims to be the
   current screen, so a delta the tool never hears about is drift on the day it ships — and
   the next brief for the screen starts from it. The tool answers with the **closed line**,
   and only that line re-stamps the pairing.

**A round can start in the tool instead of a brief.** A change that is easier to judge by eye
than to describe — comment on a region, see it redrawn, repeat — is a round like any other,
minus the brief, and it still ends with the handoff block. The search a brief runs before
anything is drawn moves to reconcile: look up each behaviour change and backend assumption
the drawing implies in the tracker, then settle each with the maintainer before its region
is built.

A change you can state in one line skips the tool altogether: make it in the code, and close
the screen once it lands.

**One round per screen, from its first redraw to its closed line.** However a round starts,
it puts the design file ahead of the code until the change ships and closes, and the
pairing's stamp hashes only the code, so nothing on the codebase side sees that. Three rules
keep the window honest:

- No second round on the screen while one is open, and no close-out for it: each block
  records one round, and a closed line sent mid-round would vouch for a design file that also
  draws unshipped changes.
- A round dropped at any point before its closed line — declined at reconcile, or abandoned
  before it ships — is reverted in the tool to the design file as it stood before the round.
- A close the screen's code needs meanwhile, from a code-first fix or release prep, waits
  and goes out with the round's close-out; a release that cannot wait reverts the round.

**Close after the change lands, not before.** A close-out sent from an unmerged change asks
the tool to redraw from a list of deltas somebody wrote, and the one left off the list stays
drift under a fresh stamp. Sent after, it points at the shipped files and the list is only a
guide to them. The same message closes a screen whose code moved without a design round —
its deltas come from the diff rather than a ledger — which is how a stale reading at release
prep gets acted on.

**One surface, one design file.** The second check above is the rule made mechanical, and it
is worth the machinery because the alternative is seductive. When a change touches one region
of a large screen, drawing that region as its own file keeps a single review small — so it
gets done, and the surrounding regions get stubbed. The file is then a claim about a screen
that does not exist: a reader opens two files to learn one, and the stubbed regions are
reviewed again when the two are finally merged. Scope belongs in the brief that names the
region, not in a file boundary. Two exceptions expire on their own — competing variants of
one screen under review at the same time, which collapse when the maintainer picks, and a
spec for unshipped work, which cannot merge into a file claiming to be the current state
without making it lie, and folds in when the work ships.

**A pairing with a stamp tells you when it went stale.** Hash each row's sources into the
row; a script that recomputes the hash then names the design files that have fallen behind
the code, and the reading is free and repeatable. Without it, drift is discovered by audit —
which is to say, long after the cheapest moment to fix it. Prefer reporting to failing the
build: on most projects the code under a screen moves far more often than what the screen
looks like, and a hard gate spends a round-trip with the design tool on changes that never
altered the design. Read it on a trigger that already exists, such as release prep.

#### The exchange — three messages

Each crossing between the tool and the codebase is one message in a fixed shape, so nobody
reconstructs a round from a transcript. All three stay in the conversation they belong to,
never a file beside the designs: a handoff kept as a file is one more file to fold at the
close. Alternatives are separated by `|`.

The **handoff block** — the tool's last message of a round, read before the design:

```
Handoff: <screen> — <design file> | for: <issue, or one line of intent>
Changed: <the regions this round redrew> | the whole screen
Other files: <each file beside the screen's own, and which exception it is> | none
Foundation: <each proposed token or component, by name> | none
Copy: <strings added or changed> | none
Forks: <region — A or B, recommending A: the tradeoff> | none
Unsettled: <the one question whose answer changes what ships> | none
Gaps: <what looks like it wants an issue, to check against the tracker> | none
Not verified: <what the tool could not check from where it sits: real limits, real data, the longest locale> | nothing
```

- A file under *Other files* that is neither exception above — competing variants, a spec
  for unshipped work — is a clone: ask before building from it.
- Build the recommended side of each fork and put the other to the maintainer. An
  *Unsettled* question with no recommendation is answered before its region is built.

The **close-out** — sent to the tool once the change has shipped; pointers, not payload:

```
Close-out: <screen> — <design file> | for: <issue, or one line of intent>
Shipped: <commit> on <branch> — <the files that decide what the screen draws>
Deltas (redraw each from the shipped files):
- <region> — dropped | changed | added: <what shipped>, because <why>
Decided: <each fork and the unsettled question: the answer> | none
Files: <design file> stays the screen's one file — fold <files> into it; delete <files>; keep <a spec whose work is still open> | every other file you hold for this screen, each folded, deleted or kept by the same rule | nothing beside it
Notes: <what the shipped state settles, retires or changes> | unchanged
Foundation: <each proposal: shipped as <name> | replaced by <existing name> | dropped> | none
Styles: <the tool's copy of the shipped stylesheet is stale since <commit>: replace it with a file copy of the shipped one — never an edit or a retype — and confirm its size> | unchanged
```

The **closed line** — the tool's answer, one per design file:

```
Closed <design file> at <commit> — redrew <regions> | nothing; folded <files> | nothing; deleted <files> | nothing; could not: <what> | nothing
```

- A design file that draws several screens closes for all of them at once, each with its own
  sources and deltas in the close-out; a screen left out is one its line would stamp unread.
- Anything under *could not* leaves the row unstamped: the close is not done.
- Stamp a row only on the closed line for its own design file, never in bulk: a re-stamp of
  every row at once marks screens nobody closed as current.
- Re-stamp only if the row's sources have not moved since the commit the tool closed
  against. If they have, the tool read an older screen; close again for the newer change.

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

**Don't run a pass for:** mid-build churn on a single screen, or a *single* divergence from the
reference — that's a direct conformance fix (or a `ux_review` Phase 4 finding), not a rebuild of
the language. A pass reviews a reasonably stable target; don't run one in the middle of an active
redesign of the same surface.

## The doc map

| Doc                          | Answers                                                         | Cadence                               |
| ---------------------------- | -------------------------------------------------------------- | ------------------------------------- |
| `design_workflow.md` (this)  | *When and how* does design happen?                             | Read at founding; re-read at each trigger |
| `design_language.md`         | *What* is the resolved system — tokens, components, patterns?  | Living; updated on every handoff and pass |
| `design_review.md`           | The runnable *pass* that establishes/evolves the language      | Run on a trigger                      |
| `ux_review.md`               | Do the *journeys* work — friction, bloat, usability?           | Per minor release / quarterly         |

`design_language.md` is **this project's own** artifact (its real tokens); the other three are
reusable templates. Keep the same shape for a TUI or a desktop app — the tokens differ, the
lifecycle doesn't.
