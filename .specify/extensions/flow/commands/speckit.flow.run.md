---
description: Run one feature from an idea to an open pull request, through every Spec Kit phase, and stop only when the owner must decide or act. Use when the user says "autopilot", "run the whole flow", "take this idea to a PR", or wants specify through implement without a stop per phase.
---

# Autopilot flow

Take one feature through every phase to an open pull request. This session is
the **orchestrator**: it dispatches each phase to a fresh subagent, reads only
what the subagent returns, and puts to the owner only what the owner holds.
The owner's attention is the scarce resource. The orchestrator's context is
the second one, so a phase's working material stays in its subagent.

`$ARGUMENTS` resolves in this order:

1. A feature directory, or a path inside one (for example `specs/003-checkout`).
   The run resumes at the first phase whose artifact is missing.
2. Anything else is the feature description for a new spec.
3. Empty. Stop, and ask for the feature description.

Snapshot `.specify/feature.json` before the first phase, and restore it as the
Feature-state guard in `.specify/extensions/batch/commands/speckit.batch.run.md`
sets it. The restore is the last thing the run does, including a run that ends
on a stop.

## Decisions

Every open decision a phase meets takes one of three routes.

| Route | The decision | What happens |
|---|---|---|
| **GATED** | The constitution assigns it to someone outside this session | It stays a `[NEEDS CLARIFICATION: GATED — <owner>: <question>]` marker, verbatim. Implement skips its tasks. |
| **Owner** | It sets what the product promises a client or a user: scope, pricing, tiers, limits, who may use it, legal or contract terms, or data that is deleted or exposed. Or no option is a clear default. | It goes to the owner at once, through `speckit.grill.run` in this session, with a recommended answer. |
| **Assumption** | Every other decision: how the work is done, a technical convention, a default with a clear runner-up | The phase takes the recommended answer, writes it into the artifact, and records it. |

An **assumption** is one line in the `## Assumptions` section of `spec.md`:
`- A<n>: <decision>: <answer taken>. Alternative: <the strongest other option>.`
Number assumptions across the feature, never restarting per phase. A plan-phase
assumption goes in the same section, so the owner reviews one list.

## Phases

Every subagent follows the Subagent contract in
`.specify/extensions/batch/commands/speckit.batch.run.md`: a self-contained
prompt, a pinned feature, an explicit agent type, no question to the user, and the
fixed return shape. Pass the type this table names as `subagent_type`, with no
`model`: the type sets the model and the effort. Without the bizstream-ai
plugin, pass the type's model as `model`: Opus for architect, Sonnet for scout. Give
each subagent the Decisions section above verbatim, and have it return its
owner questions, each with a recommended answer, and its assumption count.

| # | Phase | Agent type | Skip when |
|---|---|---|---|
| 1 | `speckit.specify` with the description | `bizstream-ai:architect` | `spec.md` exists and is filled |
| 2 | Clarify: the ambiguity scan the core `speckit.clarify` defines, with each decision routed above, never asked | `bizstream-ai:architect` | `spec.md` has a `## Clarifications` or `## Assumptions` section from an earlier run, and no `[NEEDS CLARIFICATION` marker that is not GATED |
| 3 | `speckit.plan` | `bizstream-ai:architect` | `plan.md` exists |
| 4 | Red team | `bizstream-ai:architect` | `plan.md` carries a `Red team:` line |
| 5 | Assumption review | this session | `spec.md` has no unreviewed assumption |
| 6 | `speckit.tasks` | `bizstream-ai:architect` | `tasks.md` exists |
| 7 | `speckit.analyze`, read-only | `bizstream-ai:architect` | never |
| 8 | `speckit.qa.walk` | `bizstream-ai:scout` | the wizard exists, or the `qa` extension is absent |
| 9 | `speckit.card.implement` | this session | never |

Dispatch phases 7 and 8 in one turn. Analyze edits nothing, and the walk reads
`tasks.md` only for where the code lives, so a later tasks redo does not rerun it.

Phase 9 runs in this session. When it hands work to a builder, it gives one
`tasks.md` phase or one pull request per dispatch, as
`.specify/extensions/jev/templates/jev.md` sets it under Subagent model, and
starts each next builder fresh from the state note the last one wrote.

After each subagent returns:

- Owner questions: write each one into its artifact as a
  `[NEEDS CLARIFICATION: ...]` marker, then hand the artifact to
  `speckit.grill.run` in this session before the next phase.
- `done`: go to the next phase.
- `blocked` or `failed`: dispatch the phase one more time with the note in the
  prompt. A second `blocked` or `failed` is a stop.

### Red team

Try to break the plan before any code exists, when a flaw is cheapest to fix.
The red team runs one round.

1. Dispatch one **breaker** architect subagent with fresh context. Its prompt carries
   the paths of `spec.md`, `plan.md`, the design artifacts and the
   constitution, and one job: find where the plan fails. A flaw is a scenario,
   a Success Criterion, a constitution rule or a definition-of-done row that
   the plan as written cannot meet, or an assumption that the plan contradicts.
   Each flaw names the plan section, what it fails, the case that breaks it,
   and its evidence: a line in an artifact, or a file and line in the code.
   The breaker returns at most five flaws, worst first, and hands back after
   about 30 tool calls. It edits nothing.
2. Route each flaw that is a decision under Decisions. Send every other flaw,
   and each assumption this step recorded, to plan: dispatch it one more time
   with them in the prompt. The plan subagent checks each flaw against its
   evidence first, fixes each real flaw, and returns each flaw that is not real
   with the reason. A breaker always finds something, so no second breaker
   reviews the redo.
   List every flaw fixed in the assumption review, so the owner sees what the
   red team changed.

Add a line `Red team: <date>, <flaws found>, <fixed>, <not real>` to `plan.md`.

**Done when:** every flaw was routed under Decisions, fixed, or returned as
not real with its reason, and `plan.md` carries the `Red team:` line.

### Assumption review

Put every assumption to the owner in one message, after plan and before tasks:
a table of id, decision, answer taken, and alternative, and below it each
red-team flaw the plan fixed. Then ask one
`AskUserQuestion` with the question `Accept these assumptions as the feature's
scope?` and two options, accept all (recommended) and change some. The word
scope keeps owner steering from answering it. When the owner changes an
assumption, apply each change to `spec.md`, then dispatch plan one more time
with the changes in the prompt, so `plan.md` follows. Mark the section
reviewed with a line `Reviewed: <date>`.

**Done when:** the owner accepted every assumption, or each change is applied
and `plan.md` was regenerated.

### Analyze

Analyze edits nothing. For each CRITICAL or HIGH finding, send it to the phase
that owns the artifact. For `plan.md` or `tasks.md`, dispatch that phase one
more time with the findings in the prompt. For `spec.md`, write each finding
as a `[NEEDS CLARIFICATION: ...]` marker and run phase 2 one more time. Then
run analyze one more time. A CRITICAL or HIGH finding that survives the second
analyze goes to the owner as one question, with the finding and your
recommended fix. Record MEDIUM and LOW findings for the report.

**Done when:** analyze returns no CRITICAL or HIGH finding, or the owner ruled
on each one.

## Stops

Stop and put it to the owner for:

- An owner question, through `speckit.grill.run`.
- The assumption review.
- A hard stop in `speckit.card.implement`.
- A second `blocked` or `failed` from one phase.
- The close-out confirmation in `speckit.card.implement`.

Everything else is routine: take it without asking.

## Report

Close with one table: each phase, its agent type, its result, and the questions the
owner answered. Add the red-team flaws found and fixed, the assumption count and how many
the owner changed, the analyze findings you recorded, every GATED marker still open with its owner,
and the pull request URL or the stop that came before it. Restore
`.specify/feature.json` after the report.

## Done when

- [ ] Every phase ran or was skipped by its rule, in table order, with 7 and 8 together
- [ ] Every subagent ran as the agent type the table names
- [ ] Every decision took one route: GATED, owner, or a recorded assumption
- [ ] The red team ran one round, and every flaw is fixed, routed, or not real with its reason
- [ ] The owner reviewed every assumption before tasks ran
- [ ] Analyze ended with no CRITICAL or HIGH finding, or the owner ruled on each
- [ ] `speckit.card.implement` ended with the pull request open, or on a hard stop that is reported
- [ ] `.specify/feature.json` is restored
