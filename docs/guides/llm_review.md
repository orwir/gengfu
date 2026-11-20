# LLM review template

When the user asks for a review of a product's **LLM surface** — what the product tells its language model, and how it knows the replies are good — follow this template. It covers the text the model reads (instructions, tool names and descriptions, argument schemas, the results and context handed back to it), the model settings, and the eval that measures the replies. Written for whoever runs the review and for the maintainer who reads its record. It does not cover that surface's security (`codebase_review.md` § Phase 5.7) or how people experience the feature (`ux_review.md`).

The whole guide is the `[llm]` surface (`review_machinery.md` § Surface tags).

## Shared machinery

Process is `review_machinery.md`'s and applies unchanged; the working document is `docs/reviews/YYYY-MM-llm-review.md`, and the labels are translated at the end. The per-phase **"what can go?"** habit applies with force: always-on text is paid for on every request, and a surface grown one fix at a time hides clauses for cases that no longer occur.

This doc defines only what differs: the stance, the backbone, the phases, the verdict set, the severity translation and its *At task time* questions.

## What this is *not*

- **Not a security review.** Untrusted content framed as data, tool authority, output handling, cost caps, the model supply chain, poisoned memory and hidden context are `codebase_review.md` § Phase 5.7. This pass reads the same text for what it makes the model do on ordinary traffic.
- **Not a UX review.** Whether people can tell what the feature does, correct a wrong reply or recover from a misunderstanding is `ux_review.md`'s. This pass asks whether the text behind the feature says what the product needs.
- **Not a code review of the plumbing.** The provider client, retries, caching and prompt versioning belong to a codebase domain phase.
- **Not a model bake-off.** Choosing a model is a decision the eval informs; this pass checks that the eval could inform it.

## When to use

Run it when any of these holds:

- **The model or provider changed**, or a provider announced a deprecation the product depends on.
- **The model-facing text grew one fix at a time:** a clause per bug, a tool description that restates the instructions, a prompt nobody can give all the reasons for.
- **Bugs cluster in the model's behaviour** rather than in the code around it.
- **Before the model gets more autonomy:** more tools, fewer confirmations, a wider reach.
- **The eval and the users disagree:** the gate is green while reports say the replies got worse, or the other way round.

Don't run it:

- on a product with no language model in it;
- in the middle of a rewrite of the same text — review a stable target;
- as a substitute for the eval — a review reads the instrument, it does not replace it.

**Minimum viable pass.** Phases 0 → 1 → 4 → 7: frame, inventory, the instrument, close-out. That loop yields the surface inventory, with its duplicated rules and its rows no eval case covers, and an answer to whether the eval can be trusted. The fit, provenance, robustness and cost phases turn it into verdicts and cuts.

## Stance

1. **The product's purpose and its real traffic are the yardstick.** A clause earns its place by changing a reply that real requests need changed. Read the text against what the product is for and what people actually ask, not against how well it reads.
2. **Every clause pays rent.** Always-on text is paid for on every request — in tokens, in latency, and in the attention the model gives it. Text for a rare case belongs where that case is reached, such as a tool description or a conditional block, not in what every request carries.
3. **Know why a clause exists before cutting it.** Most clauses were written for a reply that once went wrong. Read the issue and the eval cases behind a clause before removing it; a cut made without them reopens fixed behaviour.
4. **A reading is only as good as its instrument.** A green eval says nothing until it is known to fail on a broken prompt, to run what production runs, and to rise above its own noise. Trust the instrument before trusting its verdict.
5. **Say what the evidence cannot show.** Every pass names what its eval cannot read, and how that will be read instead.

## The surface inventory is the backbone

`ux_review` is anchored on its journeys, `ia_review` on its decision table, `design_review` on its token inventory. An LLM pass is anchored on the **surface inventory** from Phase 1: every piece of text the model reads, with its size, when it is sent, why it exists, and what in the eval covers it. Every later phase judges rows of it.

## Phases

| Phase | Topic             | Why                                                                 |
| ----- | ----------------- | ------------------------------------------------------------------- |
| 0     | Frame             | Purpose, traffic, models and settings, budgets, the eval as it stands |
| 1     | Surface inventory | Every text the model reads, its size, and when it is sent           |
| 2     | Fit               | Each row against the purpose and the real traffic                   |
| 3     | Provenance        | Why each clause exists, before anything is cut                      |
| 4     | The instrument    | Whether the eval measures production and can be trusted             |
| 5     | Robustness        | Formatting, wording, languages, edge inputs, tool failures          |
| 6     | Cost & latency    | Tokens and time per request; the price of reading the eval          |
| 7     | Close-out         | Deltas, ledger, register, filing, a measurement plan                |

**Waves** (`review_machinery.md` § How a review runs): Phase 0 → Phases 1, 4 and 6 in parallel → 2, 3 and 5, which read the inventory and the instrument → 7.

### Phase 0 — Frame

- **Purpose.** One paragraph on what the product is for and what a good reply does, confirmed by the maintainer before any verdict. Every later phase is judged against it, so it is a stop, not a formality.
- **Traffic.** The real mix of requests — by kind, language, length, and which tools they reach — taken from production in aggregate. Write the evidence policy first, as `ia_review.md` § Phase 0 does: aggregates before rows, no message text or person's name in the review, and how far a small sample can be trusted.
- **Models and settings.** Every model id in use, with its provider, version and any deprecation date, and the sampling settings each surface sends — including the settings a provider ignores.
- **Budgets.** The size budget for always-on text, the cost per request, and what this pass may spend on paid eval runs. A paid run waits for the maintainer's go-ahead, and the same command against a free stub runs first.
- **The eval as it stands.** Its cases, its graders and judge models, and its last readings with the build each came from. A product with no eval has its first finding here, and the pass stays short.
- **Ground-rules read** per `review_machinery.md` § Phase 0 reads, plus the product's prompt documentation and the decision records behind its budgets.
- **Baseline table:**

  | Metric                                                    | Baseline ({date}) |
  | --------------------------------------------------------- | ----------------- |
  | Always-on text (tokens)                                   | 4,800             |
  | Largest conditional addition (tokens)                     | 1,900             |
  | Rules stated in more than one place                       | 7                 |
  | Inventory rows with an eval case behind them              | 22 / 41           |
  | Noise floor of the gate (spread on an unchanged build)    | ±6 pt             |
  | Cost per canonical request                                | $0.004            |

### Phase 1 — Surface inventory

- **Every text the model reads**, from every place where it is assembled:
  - instructions: system prompt sections, persona, standing rules;
  - tools: names, descriptions, argument schemas and their field descriptions;
  - what comes back: tool results, error strings, templates the code fills in;
  - injected context: memory, retrieved documents, conversation history, examples.

  A surface counted by its prompt file misses its tool text and its error strings.
- **Per row**, record where it lives; its size; whether it is sent on every request or only when something reaches it; what it tells the model; why it exists (the issue, or the reply it was written for); and which eval cases exercise it.
- **One rule, one home.** A rule stated in the instructions, again in a tool description and again in a schema field is a finding: the copies drift apart, and the model weighs whichever it read last. Note where two copies already disagree.

### Phase 2 — Fit

Each row gets a verdict against the purpose and the traffic (*Verdicts and severity*):

- **keep** — it changes replies that real requests need changed;
- **rewrite** — the need is real, but the text misleads or costs too much;
- **move** — the need is real but rare, so it belongs where the case is reached, not in always-on text;
- **cut** — no current request needs it;
- **missing** — a frequent kind of request has no guidance at all.

Look especially for a clause written for traffic that no longer occurs, a qualifier placed far from the rule it limits, an example that steers more replies than the rule it illustrates, and a frequent kind of request the text never mentions.

### Phase 3 — Provenance

Before anything is cut or rewritten:

- **Find what each clause was written for:** the issue, the reply that went wrong, the eval cases added with it. A clause nobody can account for is flagged, not cut.
- **Read the stored results** of those cases from before and after the clause landed. If the clause moved them, cutting it reopens the behaviour; if nothing moved, the clause may never have done anything.
- **Re-run those cases after the edit** — free where the eval can run against a stub, paid only with the maintainer's go-ahead.

### Phase 4 — The instrument

- **Does it run what production runs?** The same model and settings, the same tools, the same context assembly, and cases shaped like the real traffic. Each divergence is a finding: a reading taken on a different setup is a reading of a different product.
- **Can a reading be trusted?**
  - repeated runs on an unchanged build, giving the noise floor that any change must clear;
  - results stamped with the build, model and settings they came from;
  - a positive control — a deliberately broken prompt the eval must fail;
  - graders that score what a reply says, not only its shape;
  - a model used as a judge, calibrated against a sample people labelled, and pinned.
- **What the gate cannot read**, named, each with how it is read instead: a sample of production replies read by a person, a check in the code, an alert.
- **A model's verdicts are hypotheses.** A model used as a grader or as a simulated user has biases of its own, so a person reads a sample of every verdict that decides a change.

### Phase 5 — Robustness

- **Formatting and wording:** does a reading move when the same words are reflowed, reordered or re-cased? A surface that moves on formatting is fragile, and some providers are not deterministic even at temperature 0.
- **Languages and locales** the product serves, each with cases of its own.
- **Edge inputs:** empty, very long, mixed-language, a request naming something that does not exist.
- **Tool failures:** what the model sees when a tool errors, times out or returns nothing, and whether the reply then tells the truth.
- Adversarial inputs are `codebase_review.md` § Phase 5.7's.

### Phase 6 — Cost & latency

- **Tokens per canonical request**, split into always-on and conditional; the slowest share of calls and what makes them slow.
- **Model choice against need:** a kind of request served by a larger model than its replies require, or one that fails on a smaller one.
- **What reading the eval costs** per release, and whether a cheaper run — fewer cases, a stub, a panel scoped to the change — answers the same question.

### Phase 7 — Close-out

- **Delta table** against Phase 0. Less always-on text, one home per rule, more rows with an eval case behind them, and a known noise floor are the success signals:

  | Metric                                       | Baseline | Final   | Δ       |
  | -------------------------------------------- | -------- | ------- | ------- |
  | Always-on text (tokens)                      | 4,800    | 3,600   | −1,200  |
  | Rules stated in more than one place          | 7        | 0       | −7      |
  | Inventory rows with an eval case behind them | 22 / 41  | 35 / 36 | covered |
  | Noise floor of the gate                      | unknown  | ±4 pt   | known   |

- **Simplification ledger**, **Decision register** and every action item closed, per `review_machinery.md` § Close-out.
- **A measurement plan for the next pass:** what this one could not read and the next one will need, with no personal data.

## At task time

What a task's work round asks of one diff for this review (`task_review.md` § Lens reviewers): the classes of finding this review keeps meeting that a reader can see in the change that introduces them. The model-facing reviewer asks them, against the product's prompt documentation and its eval cases. A class two reviews of this kind in a row meet with no question here gets one (`review_machinery.md` § Close-out).

- **One rule, one home.** Is a rule the diff writes already stated in, or contradicted by, the instructions, a tool description, a schema field or a sibling return (Phase 1)?
- **Rent.** Is the new text sent on every request, and if it serves a rare case, why is it not on the tool, return or conditional block that case reaches (Stance 2; Phase 2, *move*)?
- **Provenance.** Does each line the diff adds name the eval case that reads it, and were the issue and cases behind each cut clause read before the cut (Phase 3)?
- **Failure text.** What does the model read when the new path errors, times out or returns nothing, and does that text claim only what was established (Phase 5)?
- **The instrument**, for a diff to the eval: does it replay the changed seam the way production now runs it, can each grader or pin it adds fail on the defect it names, and does every comparison pair readings from one build (Phase 4)?

## Verdicts and severity

The verdict set, each verdict mapped to a label:

| Verdict     | Maps to                                                        | Means                                                                 |
| ----------- | -------------------------------------------------------------- | --------------------------------------------------------------------- |
| **keep**    | Acceptable                                                     | The row changes replies that real requests need changed              |
| **rewrite** | Drift, or Real bug where it makes replies wrong                | The need is real; the text misleads, contradicts a row or costs too much |
| **move**    | Drift                                                          | The need is real but rare; it belongs where the case is reached      |
| **cut**     | Cleanup                                                        | No current request needs it                                           |
| **missing** | Real bug where a frequent request fails; Drift-prone otherwise | A kind of request with no guidance                                    |

Same seven labels, same default actions — what they mean on the LLM surface:

| Severity             | On the LLM surface, this means                                                                  |
| -------------------- | ----------------------------------------------------------------------------------------------- |
| **Real bug**         | Replies are wrong, misleading or harmful on real traffic, or the eval passes a broken prompt    |
| **Drift**            | The text contradicts the code, a tool or another row; one rule in two disagreeing copies        |
| **Drift-prone**      | Fine now, breaks with a known change: one model's quirk, an untested language, always-on text near its budget |
| **Cleanup**          | A clause for a case that no longer occurs; a duplicate rule; a guard the code already enforces  |
| **Minor**            | Wording the model reads correctly but a maintainer misreads                                     |
| **Acceptable**       | Deliberate redundancy with eval evidence behind it                                              |
| **Decision pending** | Competing prompt designs or instrument trade-offs; the maintainer picks via a decision shape    |

## When to update this template

Per `review_machinery.md` § Updating a guide. This guide was drawn from four reviews of one product's model-facing text and eval, so expect its first runs elsewhere to need hand-edits — and send them back.
