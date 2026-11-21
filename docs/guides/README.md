# The lifecycle method

This folder is the method of the lifecycle kit (`orwir/lifecycle-kit`): the reusable
guides every project follows, from an idea's gauntlet to its releases and reviews. Each
project keeps a copy in `docs/guides/`, pinned to one kit release by `.lifecycle.lock`,
so it reads on its own. A change is made in the copy of the project that found it,
reviewed in that project's diff, and taken into the kit at its task's *Complete*
(*Change a guide*); the other projects take it at their next upgrade.

## Files (lifecycle order)

- **`idea_gauntlet.md`** — pre-project stage-gate: an evening on whether the idea is
  worth its cost (its journeys, the best existing option tried by hand), an evening
  shaping it (the IA, a picture only where needed), then one decision — make, park,
  or kill. Covers software, hardware, and mixed builds. Every idea lives in a
  private ideas home repo: one folder each, with `IDEA.md` and its evidence
  (`docs/product/`, `design/`). A graduated idea moves out into its own repo with
  its kill criteria, `IDEA.md` freezes into `docs/product/idea.md`, and the home
  checks the project on a schedule; a killed one moves to the home's `archive/`.
- **`project_kickstart.md`** — language-agnostic playbook for standing up and
  growing a project (branching, releases, CI, docs layers, tracking, the
  user-facing-surface foundation, what to grow / what to skip).
- **`agents_template.md`** — copy-and-fill skeleton for a new repo's `AGENTS.md`
  (the operating contract AI assistants read).
- **`contributing_template.md`** — copy-and-fill skeleton for a new repo's
  `CONTRIBUTING.md`: branching with branch-deletion hygiene, quality gates, the
  four-column board lifecycle, the release checklist.
- **`issue_template_bug.md`** / **`issue_template_task.md`** — copy-and-fill issue
  templates; install to `.github/ISSUE_TEMPLATE/` as `bug.md` / `task.md`.
- **`pr_template.md`** — copy-and-fill PR template; install to
  `.github/pull_request_template.md`.
- **`task_review.md`** — a task's two review rounds, each read by a reviewer agent
  with no author context (a subagent, where the assistant can launch one). The plan
  round, at Clarify, reads the plan in the issue against the code when the plan has a
  shape that has shipped holes before, and ends in the Clarify brief; the work round,
  before the hand-back, reads the tree against the accepted plan, with a lens reviewer
  per person- or model-facing surface the diff touches, and ends in the hand-back
  brief. The packets between the agents, the briefs, and each role's rules;
  the same rounds whether the work rides a PR or lands on the integration branch.
- **`review_machinery.md`** — what every comprehensive review shares: the five kinds,
  the order they run in and when each is due; a parent issue per review; a lead with
  phase agents in waves; findings and the seven-label severity vocabulary; action items
  and how they close; the private route for exploitable findings; the close-out (record
  frozen, issues carry the follow-through) and the Decision register.
- **`codebase_review.md`** — comprehensive-review template (13 phases, 0–12, in the default
  layout) for solo-maintained repos; the method is language-agnostic and the shell
  recipes are shown for Python. Run after a heavy build phase to catch
  drift, dead code, and quiet bugs. Runs on `review_machinery.md`.
- **`ux_review.md`** — CUJ-driven review of the user-facing surface (web, desktop,
  TUI, CLI, chat bot): expert walkthroughs beside a real-user source, error paths,
  consistency, latency, accessibility floor, surface diet. Runs on `review_machinery.md`.
- **`ia_review.md`** — information-architecture review of the user-facing surface:
  which screens, commands and messages exist, what each shows and for which reader's
  decision, where each fact lives, and what is missing. Ends in a target structure
  written back to the intent doc, an ADR, and filed work. Runs on `review_machinery.md`.
- **`llm_review.md`** — review of what a product tells its language model and how it
  knows the replies are good: the text the model reads, the model settings, and the
  eval that measures the replies. Runs on `review_machinery.md`.
- **`design_workflow.md`** — the design lifecycle (the *how*): found the foundation
  before screen 2, codify every design round back into it, and evolve it on a
  trigger. Companion to each project's own `design_language.md` (the *what* — tokens,
  components, patterns), which is project-specific and not part of the method.
- **`design_review.md`** — the runnable design-language pass: establish or evolve the
  visual system (baseline → north-star → foundations → components & states →
  conformance audit → codify). The design counterpart of the code and UX reviews;
  runs on `review_machinery.md`.

## Use in a project

The kit's `lifecycle` command (on the agent's PATH where the kit's plugin is on,
otherwise `bin/lifecycle` in a checkout of the kit) keeps the copy:

```
lifecycle upgrade --adopt   # the first time: copy the method into docs/guides/
lifecycle upgrade           # later: move the copy to the installed kit's release
lifecycle doctor            # what the repository needs before its next task
lifecycle upgraded          # an upgrade in the tree the integration branch lacks
```

`upgrade` writes `.lifecycle.lock`: the release and each copied file's digest. A
copied guide holding an edit not yet in the kit keeps it: the upgrade replays the edit
onto the new release from the release the copy came from, stops on a conflict, and
the edit still goes to the kit at its task's *Complete*. It replaces each file a
template ports into that the repository kept word for word, and names the templates
left to port, each with its file: a project's `AGENTS.md`, `CONTRIBUTING.md` and
`.github/` templates, unless `[templates]` in `.lifecycle.toml` names others. A
release that needs any other change in each repository says so in a `Repositories:`
line of the kit's changelog; `lifecycle upgraded` lists them, and whoever runs the
upgrade makes each one. A repository behind the installed kit
upgrades as a tracked task's first step, and the task's briefs say what the upgrade
brought; it lands as its own commit ahead of the task's where it can.

A copy from before the kit holds a `.gist-revision`: the first `upgrade` checks it
against the gist it came from and refuses if it holds an edit the gist never took.

## Bootstrap a new idea (agent instructions)

When pointed at this method and asked to bootstrap or prepare the gauntlet:

1. Find the user's ideas home: a repo whose root `AGENTS.md` says it is one (the
   user's global agent instructions may name it). If there's none, offer to set
   one up where the user chooses, private by default, with the layout in
   `idea_gauntlet.md` § *The ideas home*: its root `AGENTS.md` (the template
   there), `archived.md`, `graduated.md`, `active/`, `archive/`, a `.lifecycle.toml`
   naming `main` as its integration branch and
   `"idea_gauntlet.md" = "AGENTS.md"` under `[templates]`, and this method copied into
   `docs/guides/` (*Use in a project*).
2. Check `archived.md`: if this idea, or a close variant, was killed before, show
   the line — re-opening needs new facts against the logged reason.
3. Create `active/<idea>/IDEA.md` from the fenced Template body in
   `docs/guides/idea_gauntlet.md`; fill `{{name}}` and the Captured date, set
   `Status: captured`.
4. Stop there. The answers in `IDEA.md`, the bar and every verdict belong to the
   user — never fill them or call a gate. On request, do the legwork into the
   idea's artifacts: Phase 1 research (`docs/product/competitors.md`,
   `docs/product/feasibility.md`), draft CUJs for the user to agree, and the sheet
   for the baseline walk the user does by hand; Phase 2's draft IA and its walk,
   and a picture in `design/` where the walk can't settle the bar; the Phase 3
   spike in `spike/`.

When the user kills the idea: move its folder to `archive/<YYYY-MM-DD>-<idea>/`,
set its Status, and add the line to `archived.md` in the user's words.

When the user says the idea passed and asks to start the project: move the folder
into the project's own repo (`git subtree split --prefix=active/<idea>` keeps its
history), add a line to `graduated.md` with a next check four weeks out, copy
the guides into the new repo, then follow `docs/guides/project_kickstart.md` seeded
from `IDEA.md` and its artifacts (done statement → v0.1 milestone, IN list → first
issues, the spike's stack choice and the decisions recorded in `IDEA.md` → first
ADRs); gauntlet Phase 4 covers the bet and the `IDEA.md` → `docs/product/idea.md`
handoff.

## Change a guide

When a project finds a reusable pattern (not project-specific), it comes back to the
kit so every project inherits it. The change starts in the copy of the project that
found it, inside the task that found it, so it is reviewed in that project's diff with
the rest of the task.

1. **Edit the copy** inside the task. The task's hand-back names the push to the kit
   as a step of its *Complete*; `lifecycle guides check` lists the edited guides.
2. **Push at *Complete***, on the task's go-ahead and before its commit:
   `lifecycle guides push -m "<what changed and why, one line>"`, the line in the kit's
   words, since every project on the kit reads it. It applies the edits
   onto the release the copy is pinned to, replays them onto the kit's latest, and
   releases them as a patch; a conflict stops the push before anything lands, and goes
   back to the maintainer.
3. **Commit the pushed guides and `.lifecycle.lock` with the task.** A pushed guide
   takes the released text, with any change another project made to it meanwhile; the
   lock records the release that took it, and the repository's next upgrade waits until
   the installed kit has it.

Two rules keep every copy safe:

- **An edit never outlives its task.** It is pushed at *Complete* or reverted; an
  upgrade in between carries it, and never decides it.
- **A push starts from the copy's own release**, not from whatever the kit holds now,
  so a copy that fell behind cannot revert anyone else's change.

The maintainer can also change the method in the kit itself, released by the kit's own
rules; projects take it at their next upgrade.

A new file here needs the same justification as a new dependency — prefer extending an
existing guide over adding another file.
