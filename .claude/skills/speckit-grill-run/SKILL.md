---
name: speckit-grill-run
description: Interview the user in dependency-ordered rounds until the named artifact
  has no open decision left. Works on an idea, a plan, one spec, or a batch of specs.
compatibility: Requires spec-kit project structure with .specify/ directory
metadata:
  author: BizStream
  source: extension:grill
user-invocable: true
disable-model-invocation: false
---

# Grill Run Skill

Read `.specify/extensions/grill/templates/grilling.md` and follow it exactly.
It is the protocol; this file only supplies the target.

## Target

The target is `$ARGUMENTS`, resolved in this order:

1. A path, or several paths, to spec or plan files. Grill those, batch mode.
2. A feature directory (for example `specs/003-ci-cd`). Grill every artifact in
   it, batch mode.
3. `all`, or empty with specs present. Grill every spec that carries a
   `[NEEDS CLARIFICATION]` marker or an open checkpoint row, batch mode.
4. Anything else. Treat the text as the idea to grill, single mode.

`--notes <path>` beside any target names meeting notes or an email to match
against the open rows, as the Meeting notes section of the protocol sets it.

## Before the first round

Read `.specify/memory/constitution.md` when it exists. It names the decisions
already settled and the gates owned by someone outside this session. Never ask
a settled decision. Never ask a gate that belongs to someone else.

Then collect the open decisions from the target and build the design tree.

## When the frontier empties

Write the answers back into the artifacts they belong to. Replace each
resolved `[NEEDS CLARIFICATION]` marker with the decided text. Write each
answered row into the artifact its `phase` names. Leave a deferred decision as
a marker or an open row, and say which ones you left.

Report: the rounds you ran, the decisions closed, the decisions gated, the
rows sent to the client, and the markers and rows still open.
