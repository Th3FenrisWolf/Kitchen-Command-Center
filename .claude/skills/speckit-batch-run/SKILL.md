---
name: speckit-batch-run
description: Run one Spec Kit phase — specify, clarify, plan, tasks, or jira — across every card or spec ready for it, fanned out in parallel subagents. Clarify resolves questions in one session through speckit.grill.run; jira drafts every card, then creates after one approval gate. Use when the user says "batch specify", "clarify batch", "plan batch", "tasks batch", "jira batch", or wants a phase run across many specs at once.
compatibility: Requires spec-kit project structure with .specify/ directory
metadata:
  author: github-spec-kit
  source: batch:commands/speckit.batch.run.md
---

# Phase batch

Advance every **ready** target through one phase, in parallel. Readiness is
per phase — a target already carrying that phase's artifact is done, never
re-run.

`$ARGUMENTS` names the phase: `specify`, `clarify`, `plan`, `tasks`, or
`jira`. Anything else — stop, and name the five valid phases.

## Config

Read `.specify/extensions/batch/batch-config.yml`. Its `card_source` key
names the file the specify phase reads cards from.

Only the specify phase reads it, and a missing file is not a stop when
`specs/jira-cards.md` holds rows with a Jira key: those rows are cards too.
With neither, stop and name both paths — the config file and the
`card_source` it would have pointed at. Every other phase runs without it.

## Feature-state guard

`.specify/feature.json` and any sibling per-feature state file point at one
spec directory, and the phase setup scripts overwrite it as a side effect.
Parallel subagents each pin their own feature explicitly (below), so the race
is harmless to their work, but the file itself ends up pointing at whichever
subagent wrote last.

Snapshot every such file that exists before the phase writes anything at all.
The specify phase scaffolds on the main thread before its fan-out, and
`create-new-feature` writes the state file as it scaffolds, so a snapshot
taken at fan-out has already missed the first write.
Restore each snapshot — or remove the file if it did not exist before — when
the run ends, whether it ends in a report, at the jira gate, or on an early
stop. This is the last thing that happens, after the report.

## Subagent contract

Every subagent this command dispatches, in any phase:

- Gets a self-contained prompt: everything it reads, every path it writes,
  the exact shape to return. It has no memory of this session.
- Pins its own feature: `SPECIFY_FEATURE_DIRECTORY=<spec dir>` before any
  `.specify/scripts/` call, bash or PowerShell.
- Runs as an agent type, set as `subagent_type` on the dispatch call with
  no `model`: `bizstream-ai:architect` for a phase that drafts or judges
  (specify, clarify, plan, tasks) and `bizstream-ai:scout`
  for one that applies decisions already taken (a jira draft or create).
  `.specify/extensions/jev/templates/jev.md` sets the types under Subagent
  model. Without the bizstream-ai plugin, pass the type's model as
  `model`, never the unset default, which inherits this session's model.
- Never asks the user anything. An unresolved decision goes into its return
  value instead.
- Returns a fixed shape: target, status (`done`, `blocked`, or `failed`),
  artifacts written, open questions, GATED markers carried, and a one-line
  note when blocked or failed.

Batch at whatever concurrency the runtime allows; the shape above is what
makes fan-out safe, not a fixed count.

## Readiness

Never re-run a phase whose artifact already exists. GATED markers never
block plan or tasks — a spec plans and tasks with open GATED items, each
recorded as a risk.

| Phase | Targets | Ready when |
|---|---|---|
| specify | cards in `card_source`, and keyed rows of `specs/jira-cards.md` | no `specs/*/spec.md` carries the card's `Card:` line or, for a keyed row, its Jira line — or the one that does is still the unfilled template |
| clarify | `specs/*/` | `spec.md` carries a `[NEEDS CLARIFICATION]` marker that is not `GATED` |
| plan | `specs/*/` | `spec.md` exists, `plan.md` does not |
| tasks | `specs/*/` | `plan.md` exists, `tasks.md` does not |
| jira | rows of the registry `speckit.jira.cards` reads | the row has no key |

## Specify phase

1. Read the cards. In `card_source`, a card is a `##` heading and its scope
   text; a `Gate: <owner> — <question>` line under it names a decision its
   owner holds. In `specs/jira-cards.md`, a card is a row with a Jira key and
   the snapshot under its `### <KEY> <summary>` heading.
2. Scaffold every ready card **sequentially on the main thread, never in
   parallel** — `create-new-feature` numbers a feature from the directories
   already on disk and writes `.specify/feature.json` as it runs, so two
   scaffolds racing would number the same slot twice. A card whose spec
   exists as the unfilled template is not scaffolded again; it goes straight
   to step 3. For each new scaffold, the moment it lands:
   - A keyed row: pass the lowercased key as the start of the short name
     (`abc-123-gallery`), so the directory and the branch carry it. Write the
     Jira line `.specify/extensions/jira/templates/linking.md` defines into
     the header block.
   - A `card_source` card: write `**Card**: <card id>` into the header block.
3. Draft in parallel, one subagent per ready card. Its prompt carries the
   card's id, title and full scope text or snapshot, its `Gate:` line verbatim
   if present, its card criteria if any, and the scaffolded `spec.md` path to
   fill in place, keeping the header block. Rules: WHAT and WHY only, never
   implementation; unlimited `[NEEDS CLARIFICATION: ...]` markers rather than
   a guess on a material decision; a `Gate:` line becomes
   `[NEEDS CLARIFICATION: GATED — <owner>: <question>]`, verbatim owner and
   question; every card criterion is covered by at least one acceptance
   scenario that ends with its id, `[C2]`.
4. Review every drafted spec against the project's writing register and its
   testable-AC bar. Fix a small fault directly; re-dispatch a subagent for a
   structural one.
5. Hand every drafted spec's path to `speckit.grill.run`, one session, batch
   mode. A `GATED` marker is not an open decision for this session — leave it
   for grill to skip as already-owned, never surface it yourself.

## Clarify phase

1. Scan in parallel, one read-only subagent per ready spec. Each runs the
   ambiguity scan the core clarify skill defines, against that one spec only,
   and returns its open questions, prioritized, each tagged with the spec
   section it touches. It edits nothing.
2. Dedupe across every returned question — the same decision asked by two
   specs collapses to one. For each surviving question with no marker yet in
   its spec, add one `[NEEDS CLARIFICATION: ...]` line at the section the
   subagent named.
3. Hand every affected spec's path to `speckit.grill.run`, one session, batch
   mode. It runs the rounds and writes the answers back into each spec —
   that is its job, not this command's.
4. A `GATED` marker is never part of the scan, the dedupe, or the grill
   session. It stays verbatim.

## Plan phase

One subagent per ready spec. Its prompt pins the feature and follows the
core `speckit.plan` flow for that spec end to end. It records every `GATED`
marker in the spec as an open risk in `plan.md`, and never asks the user — an
unresolvable unknown goes into `research.md` as `NEEDS CLARIFICATION` and
into the subagent's return value.

Review every plan against the constitution. Fix a small fault directly;
re-dispatch a subagent for a structural one.

## Tasks phase

Same shape as plan: one subagent per ready spec, pinning its feature and
following the core `speckit.tasks` flow. Tasks generation is deterministic
from `spec.md`, `plan.md` and the design artifacts already on disk — nothing
here needs the user.

## Jira phase

The registry row is the unit, not the spec — several specs can feed one row.
This phase writes to a client-visible board, so drafting and creation are two
passes with one gate between them.

1. Draft in parallel, one subagent per row with no key. Each follows
   `speckit.jira.cards` for that row and stops the moment it has drafted —
   it never creates anything.
2. Review every draft the same way a card author would. Fix a small fault
   directly; re-dispatch a subagent for a structural one.
3. **Gate.** Present the whole batch as one table — row, its specs, summary,
   description length, open-decision count — and write every description's
   full text and its Test Notes to a file; never paste full text into chat.
   State the project key and the row count up front. The file plus that
   header is what `speckit.jira.cards` presents at its own step 4, which no
   subagent reaches here. A count near the spec count usually means a spec became a row;
   recheck the registry before presenting if so.

   Stop. Nothing is created until the user approves in this session. One
   approval covers this batch only.
4. Create every new Epic on the main thread first, under
   `speckit.jira.cards` step 5, and write each key into the `Epic` column.
   Two parallel subagents whose rows share an Epic value would otherwise each
   create it.
5. Create in parallel after approval, one subagent per row. Each follows
   `speckit.jira.cards` through to creation with the approved text verbatim —
   it never re-drafts, never rewords, never adds an item the gate did not
   show. It stops on its first failure and reports what it already created.

## Report

One table: target → artifacts produced, questions resolved, questions
deferred, `GATED` markers outstanding with owner. The jira phase's table
covers the create step only, after the gate, and lists every skipped row
with why.

Then restore the feature-state files snapshotted at the start, per the
feature-state guard above.