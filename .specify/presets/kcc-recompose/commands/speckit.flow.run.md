---
description: Run one feature from an idea to an open pull request, through every Spec Kit phase, and stop only when the owner must decide or act. The branch is recomposed into clean commits before it is published. Use when the user says "autopilot", "run the whole flow", "take this idea to a PR", or wants specify through implement without a stop per phase.
strategy: wrap
---

{CORE_TEMPLATE}

## Recompose before publishing

> This section is authoritative for how the run reaches its pull request. Where anything above conflicts with it,
> this section wins.

- Phase 9 runs `speckit.card.implement` as this project wraps it. Its close-out recomposes the branch with the
  `recompose-branch` skill after the review loop and before the push.
- The recomposition runs in this session. No builder plans it, builds it, pushes the branch or opens the pull
  request.
- The recomposition plan rides on the close-out confirmation, which stays an owner stop under Stops. It is never
  routed as an assumption, never answered by a recommendation, and never skipped on a resumed run.
- The run never pushes a branch or opens a pull request until the recomposition has passed its tree check. A
  resumed run skips the recomposition only when the branch's pull request is already open.
- The Report adds the recomposed history, and the backup branch with its delete command.
