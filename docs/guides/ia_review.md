# Information architecture review template

When the user asks for an **information-architecture (IA) review**, follow this template. The review decides which screens, commands and messages the product should have, what each one shows, where each fact lives, and which reader's decision it serves.

It is the structure counterpart of three other guides:
- `codebase_review.md`, the code;
- `ux_review.md`, the journeys: *do the journeys work?*;
- `design_review.md`, the visual system: *is the visual system coherent?*

This pass asks *is the right information in the right place, under one name, for the decision its reader makes?* It decides a target structure and files the work; it does not redraw screens.

Surface tags as in `review_machinery.md` § Surface tags; untagged checks apply to every surface.

## Shared machinery

Process is `review_machinery.md`'s and applies unchanged; the working document is `docs/reviews/YYYY-MM-ia-review.md`, and the labels are translated at the end.

The per-phase **"what can go?"** habit applies with force: a surface that grew one feature at a time hides screens to merge, figures to drop and commands to retire.

This doc defines only what differs: the stance, the backbone, the phases, the verdict set, the
severity translation and its *At task time* questions.

## What this is *not*

- **Not a UX review.** `ux_review.md` walks the journeys for friction, taking the structure as given. This pass decides the structure. A button that is hard to find is a UX finding. A fact shown in three places, or a screen whose data serves no decision, is an IA finding.
- **Not a design-language pass.** How things look belongs to `design_review.md`. IA is documented in
  tables and diagrams, not in wireframes or comps
  ([NN/g](https://www.nngroup.com/articles/ia-vs-navigation/)).
- **Not the redesign itself.** The pass ends in a target structure, an ADR and filed work. Each screen is redrawn afterwards in its own design round (`design_workflow.md` § *Round*).
- **Not a copy pass.** A label or a figure is reworded here only where it misleads a decision. Voice and wording belong to the UX review's Phase 4.

## When to use

Run it when any of these holds:

- **Before a redesign or a page-by-page polish pass.** Polishing a screen that the structure would merge away is wasted work.
- **Before the product opens to strangers, or when its readers or business model change.** Examples: a new role, a paid tier, a second kind of customer.
- **When a second surface joins the first**, such as a chat bot and a web console, or a CLI and a TUI. Which surface owns which task is a structural question.
- **When the surface grew one feature at a time.** Signs: nobody can say who reads a screen, the same fact shows in three places, or a UX review's surface diet keeps finding problems of structure rather than friction.

Don't run it:
- before v0.1: the gauntlet's Phase 2 draws the first IA;
- in the middle of a redesign of the same surface: the redesign should start from this pass's result;
- on a headless service: there is no surface.

**Minimum viable pass.** Phases 0 → 1 → 3 → 4 → 5 → 8: frame, inventory, decisions, verdicts, the target map, close-out. That loop yields a verdict per row, the list of decisions with nothing behind them, and the structure its close-out writes back. The object, label and validation phases sharpen it; their objects and vocabulary are written back only when they ran.

## Stance

1. **Decisions are the unit of value.** Information earns its place by changing what its reader
   does next. An accurate figure that no decision uses is noise. A decision with nothing on screen
   to stand on is the pass's most valuable finding.
2. **Structure before surface.** IA informs the interface, so a first pass of it — which screens
   exist, what each carries and what each thing is called — comes before anyone polishes a screen
   ([NN/g](https://www.nngroup.com/articles/ia-vs-navigation/)). A tree test checks the structure
   with no visual design at all ([NN/g](https://www.nngroup.com/articles/tree-testing/)), so a
   failure of the structure is not mistaken for one of the interface.
3. **One home per fact.** Every object has one place where it is shown in full and acted on.
   Everywhere else it appears as a reference, such as a badge, a line or a link, never as a second
   full copy that can drift from the first.
4. **Practice over a thin sample.** In a young product, visits and observed values come from few
   users and miss states nobody is in yet. Read them as evidence of how people arrive, never as a
   verdict on their own. Where they are thin, reason from established practice and say so.

## The decision table is the backbone

`ux_review` is anchored on its journeys and `design_review` on its token inventory. An IA pass is anchored on the **reader × decision table** from Phase 3: who reads, what brings them, what they decide, and what that decision needs. Every row of the inventory is judged against it.

## Phases

| Phase | Topic                | Why                                                                     |
| ----- | -------------------- | ----------------------------------------------------------------------- |
| 0     | Frame                | Readers, goals, the evidence policy, surface counts                     |
| 1     | Inventory            | Every screen, command, message and figure, with its production reading  |
| 2     | Objects              | The nouns readers care about, with one home each                        |
| 3     | Tasks & decisions    | Per reader: trigger → decision → information → action; the gaps         |
| 4     | Content audit        | A verdict per row; the per-figure checklist                             |
| 5     | Structure & channels | The target map; which surface owns which task; exceptions pushed        |
| 6     | Navigation & labels  | Front doors, wayfinding, disclosure, one vocabulary                     |
| 7     | Validate             | The target tested with real people where they can be reached           |
| 8     | Close-out            | The target written back, the ADR, the migration list, the deltas        |

**Waves** (`review_machinery.md` § How a review runs): Phase 0 → Phases 1 and 3 in parallel → 2, 4 and 6, which read the inventory and the decision table → 5, the target map → 7, which tests it → 8.

### Phase 0 — Frame

- **Readers.** Every role that meets the surface. Include roles that never sign in, such as a member who only meets the product in chat `[bot]`, and roles the account model merges, such as the person who pays and the person who configures. Where real users can't be interviewed, write proto-personas and mark them as assumptions.
- **Goals.** The product's goal and each reader's goal, one line each. Every verdict is checked against them.
- **Evidence policy**, written before anything is read:
  - which production data may be read, and in what form: aggregates before raw rows, and the least
    raw data that answers the question;
  - where copies sit and when they are deleted, by the session that fetched them, since a later
    session may never find them; and a rule that no message text or person's name enters the review;
  - how far the sample can be trusted: how many users, and which states no live user is in.
- **Surface counts**, taken mechanically: screens, commands, proactive messages, distinct figures, settings. Phase 8 measures its deltas against them.
- **Ground-rules read:**
  - the product's intent doc, `docs/product/vision.md`: its readers, its journeys and any IA section;
  - the design-language reference;
  - the rest per `review_machinery.md` § Phase 0 reads.

### Phase 1 — Inventory

- **Every place information reaches a person**, on every surface:
  - screens, and each of their states;
  - commands and their replies `[cli]` `[bot]`;
  - proactive messages: welcome messages, notifications, refusals, errors;
  - the chrome around content: footers, status lines, progress messages `[bot]`.

  A surface counted by its routes misses its messages.
- **The row unit** is a screen, a command, a message family, or a figure. Figures get one row each, or one per set of columns rendered together. A surface item that carries no information, such as a skip link or a confirm field, is not a row.
- **Per row**, record:
  - its readers;
  - what it shows;
  - where each value comes from;
  - how the reader arrives: navigation, a link in a message, a notification;
  - its **production reading**: the value real data gives today, `n/a` for a state no live user is in (never `0`), or *not measured*.
- **Reuse earlier inventories.** Where an earlier review listed the surface item by item, cite its ids instead of re-listing. Keep the inventory in one table, and time-box it
  ([NN/g](https://www.nngroup.com/articles/content-audits/)).

### Phase 2 — Objects

Slice the system by its nouns, not by its verbs or its screens ([OOUX](https://www.ooux.com/what-is-ooux)).

- **Objects** are what readers care about, such as an account, a subscription, a credential, a
  member or a record. They are not the screens that happen to show them.
- **Per object**, record its relationships, the actions taken on it, and the attributes it carries.
- **Appears-in matrix.** For each object, list every row that shows it. More than one full rendering is a finding: pick the object's **home**, the one place it is shown in full and acted on, and make the rest references.
- **Two more findings:** an object with no home, only ever shown in fragments; and a screen with no object, organized around a feature rather than a noun.

### Phase 3 — Tasks & decisions

- **Per reader, the top tasks**, each with its frequency (daily, weekly, rare) and its urgency. A Top Tasks vote usually draws hundreds of voters or more ([McGovern](https://alistapart.com/article/what-really-matters-focusing-on-top-tasks/)). Without that many, derive the list from usage data where it exists and from practice where it doesn't, and say which.
- **Per task: trigger → decision → information → action.** Record what brings the reader (an event, a notification, a question), what they decide, what they must see to decide it, what they do next, and where each of those happens.
- **Two kinds of finding:**
  - a **gap**: a decision with no information behind it. Gaps are the pass's highest-value output.
  - **dead information**: a row that no decision uses.
- **Rare but critical tasks** are kept by name whatever the traffic says: they are rare by design. Examples: contesting a decision, recovering access, a failed payment.

### Phase 4 — Content audit

Each row gets a verdict, mapped to a label (`review_machinery.md` § Severity vocabulary). The verdict names the row's reader, the decision it serves and its production reading. The usual reasons to remove are that the content is redundant, outdated or trivial.

| Verdict    | Maps to                                           |
| ---------- | ------------------------------------------------- |
| **keep**   | Acceptable                                        |
| **revise** | Minor; Real bug where the row misleads a decision |
| **move**   | Drift: the fact is shown away from its home       |
| **merge**  | Drift: one fact in more than one full copy        |
| **remove** | Cleanup: no decision uses it                      |

- **Evidence rule.** Visit counts and observed values alike never decide a removal on their own. A figure that reads the same for every live user may serve a state none of them is in yet. A removal rests on the decision table, because no decision uses the row, with the readings as support.
- **Per-figure checklist.** A figure that fails an item is revised, or removed if no revision helps:

  | Check      | The figure…                                                              |
  | ---------- | ------------------------------------------------------------------------ |
  | Question   | answers one question its reader asks, stated in the review in one line   |
  | Unit       | names its unit: a count, money, a duration, a share                      |
  | Window     | names its time window, or says it is live                                |
  | Comparison | is shown against something: a limit, a target, the previous period       |
  | Freshness  | says how old it is wherever it can be stale                              |
  | Action     | offers the next action at its threshold, or says none is needed          |

### Phase 5 — Structure & channels

- **One organization scheme per level:** by object, by task, or by audience. Mixing schemes in one menu is a finding.
- **The target map** lists the screens, commands and proactive messages that should exist after the pass, each with its job and the objects it is home to.
  - Merge screens that serve one decision.
  - Split a screen that serves two readers who never meet.
  - For a reader who visits rarely, prefer a shallow structure they can recognize to a deep one they would have to learn.
- **Channel ownership** `[bot]` `[cli]` `[web]`: for each task, which surface owns it.
  - A conversational surface owns what happens in the conversation and what must reach a person.
  - A console owns configuration, money and review.
  - Two surfaces doing one job is a finding: keep the stronger, unless some reader can't reach it.
- **Exceptions are pushed.** People open some surfaces only occasionally. When a state change needs someone to act, such as a credential refused, a trial ending or a payment failing, it must reach the person who can act. The console is where they resolve it, not where they discover it. For each exception, record who must hear, through which channel, and where its link lands.
- **Lifecycle.** Record each object's states over time, and which screen explains each. A state that no screen explains is a gap.

### Phase 6 — Navigation & labels

- **Front doors.** People land deep, from a notification, a link in a chat or a search, not on the home screen (Dan Brown, *Eight Principles of Information Architecture*, 2010). Every screen a link can land on must tell a cold reader:
  - where they are;
  - what this is about;
  - what they can do here.

  Test it by opening each such screen cold and answering those three.
- **Wayfinding.** Global, local and contextual navigation each have one job. The current place is
  marked, and the way back is always there.
- **Progressive disclosure.** What the decision needs comes first, with detail on request. A screen
  that shows everything shows nothing.
- **Labels.** One name per concept across every surface and the docs: a controlled vocabulary. A category whose label does not explain itself is backed by examples. The vocabulary is written back at close-out, and the UX review checks copy against it (`ux_review.md` § Phase 4).

### Phase 7 — Validate

- **Test with real people where they can be reached.** A tree test shows the target map as a bare outline, with no visual design, and asks people where they would find things. A qualitative one needs only a few participants ([NN/g](https://www.nngroup.com/articles/tree-testing/)). Where it costs little, tree-test the current structure first, to see where it fails; a number for the target to beat needs a quantitative test and a larger sample.
- **Time-box the wait.** Set a window when the invitations go out. Then read the test with whoever answered, per task and per participant: a handful of people gives no success rate.
- **AI-simulated users generate hypotheses, never verdicts.** In the study NN/g cites, ChatGPT outperformed most real people at a tree test, so an AI's success at a task proves little ([NN/g](https://www.nngroup.com/articles/synthetic-users/)); a task the AI fails is still worth a look. An AI reader sees only the bare outline and the tasks, never the review or the product's code, or it reads the answers off the target.
- **Heuristic pass on the target**, using Abby Covert's ten IA heuristics ([Covert](https://abbycovert.com/ia-tools/ia-heuristics/)):
  - findable, accessible, clear, communicative, useful,
  - credible, controllable, valuable, learnable, delightful.

  Write one line each, and a finding where one fails.
- **Record it honestly.**
  - Participants are recorded by role and language, never by name or handle, both in the review and in any tool.
  - Record the window, and the results per task.
  - A pass validated without real people says its structure was not tested with them, and the next pass owes that test.

### Phase 8 — Close-out

- **Write the target structure back** into the IA section of the product's intent doc: the target
  map's screens, commands and messages, and — where Phases 2 and 6 ran — its objects and its vocabulary. This is the section the
  gauntlet seeds (`idea_gauntlet.md`, Phase 2). The
  vocabulary keeps one home: where the project has a design-language reference, its terminology
  holds the words and the IA section cites it.
- **An ADR** records the structural decision and cites that section. The review doc keeps the
  evidence.
- **The migration list.** Every change becomes a job-sized child of the review's parent issue (`review_machinery.md` § Action items), closed in its phase's action items as `[FILED → #N]`: a merge, a move, a removal, a new message, a new screen. The Decision register holds the decisions behind them, not the children. Screens are redrawn in their own design rounds, not here.
- **Write journey changes back** into the intent doc. Journey ids keep their numbers
  (`ux_review.md` § *CUJs*).
- **Delta table** against Phase 0. A smaller surface with no copies and no gaps is the success signal:

  | Metric                                  | Baseline     | Final       | Δ       |
  | --------------------------------------- | ------------ | ----------- | ------- |
  | Screens / commands / proactive messages | 18 / 12 / 6  | 13 / 4 / 7  | smaller |
  | Distinct figures                        | 54           | 37          | −17     |
  | Objects with more than one full copy    | 4            | 0           | −4      |
  | Decisions with nothing to stand on      | 6            | 0           | −6      |

- **Simplification ledger** and **Decision register**, per the shared machinery.
- **A measurement plan for the next pass.** Record what the next review will need and this one lacked, with no personal data: which link a visit came from, and whether a task was completed.

## At task time

What a task's work round asks of one diff for this review (`task_review.md` § Lens reviewers): the classes of finding this review keeps meeting that a reader can see in the change that introduces them. The person-facing reviewer asks them, against the IA section of the intent doc and the vocabulary its close-out wrote back. A class two reviews of this kind in a row meet with no question here gets one (`review_machinery.md` § Close-out).

- **One home.** Does the diff render in full a fact whose home is elsewhere, or show one fact twice on a page, instead of a reference to its home (Phases 2 and 4)?
- **One name.** Does every reader-facing word the diff adds match the controlled vocabulary in every locale, with no new synonym and no word used for two things (Phase 6, *Labels*)?
- **Figures.** Which reader's decision does each new figure serve, and does it name its unit and window, show a comparison, and offer the next action at its threshold (Phase 4 checklist)?
- **Reach.** Can every reader a new screen, message or link is for reach it, does a link land where its reader can act, and does the navigation mark the place (Phase 6, *Front doors* and *Wayfinding*)?

## Severity, translated to information architecture

Same seven labels, same default actions — structural meanings:

| Severity             | In the structure, this means                                                                  |
| -------------------- | --------------------------------------------------------------------------------------------- |
| **Real bug**         | A reader can't make a decision the product needs from them: the information is missing, wrong, or contradicts itself between screens |
| **Drift**            | A fact in more than one full copy; one concept under two names; a screen whose data no longer fits its job |
| **Drift-prone**      | Fine now, fragments with growth: a menu that grows per feature, a figure repeated per tier    |
| **Cleanup**          | Dead information: a figure, screen, command or message that no decision uses                  |
| **Minor**            | A figure missing its unit, window or comparison where the reader can still infer it           |
| **Acceptable**       | A deliberate reference to a fact's home, or a rare-by-design screen kept with its reason      |
| **Decision pending** | Competing structures; the maintainer picks via a decision shape                               |

## When to update this template

Per `review_machinery.md` § Updating a guide.
