---
description: Interview the user in dependency-ordered rounds until the named artifact has no open decision left. Works on an idea, a plan, one spec, or a batch of specs.
---

Read `.specify/extensions/grill/templates/grilling.md` and follow it exactly.
It is the protocol; this file only supplies the target.

## Target

The target is `$ARGUMENTS`, resolved in this order:

1. A path, or several paths, to spec or plan files. Grill those, batch mode.
2. A feature directory (for example `specs/003-ci-cd`). Grill every artifact in
   it, batch mode.
3. `all`, or empty with specs present. Grill every spec that carries a
   `[NEEDS CLARIFICATION]` marker, batch mode.
4. Anything else. Treat the text as the idea to grill, single mode.

## Before the first round

Read `.specify/memory/constitution.md` when it exists. It names the decisions
already settled and the gates owned by someone outside this session. Never ask
a settled decision. Never ask a gate that belongs to someone else.

Then collect the open decisions from the target and build the design tree.

## When the frontier empties

Write the answers back into the artifacts they belong to. Replace each
resolved `[NEEDS CLARIFICATION]` marker with the decided text. Leave a
deferred decision as a marker, and say which ones you left.

Report: the rounds you ran, the decisions closed, the decisions gated, and the
markers still open.
