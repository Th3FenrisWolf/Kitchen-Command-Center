---
description: Run one feature from an idea to an open pull request, through every Spec Kit phase, and stop only when the owner must decide or act. The close-out runs the ship-branch skill, so the branch is reviewed, checked against acceptance and recomposed before it is published. Use when the user says "autopilot", "run the whole flow", "take this idea to a PR", or wants specify through implement without a stop per phase.
strategy: wrap
---

{CORE_TEMPLATE}

## Ship through ship-branch, never merge

> This section is authoritative for how the run reaches its pull request. Where anything above conflicts with it,
> this section wins.

- Phase 7 runs `speckit.card.implement` as this project wraps it. Its close-out runs the `ship-branch` skill with
  `--no-jira`: two review rounds, one CodeRabbit round, the acceptance check and the recomposition, all before the
  push, then the pull request and its watch.
- ship-branch runs in this session. No builder runs a review round, recomposes the branch, pushes it or opens the
  pull request.
- `publish` is always `ask` in this project: automation never merges. Phase 10, merge on green, never runs, and
  `gh pr merge` never runs.
- The close-out confirmation stays an owner stop under Stops, because it approves the publish. It is never routed as
  an assumption, never answered by a recommendation, and never skipped on a resumed run.
- The run never pushes a branch or opens a pull request until ship-branch has passed its tree check and named no
  blocker. A resumed run skips ship-branch only when the branch's pull request is already open.
- Phases 8 and 9 commit their QA files on top of the open pull request, as one commit. The branch is never recomposed
  again.
- The Report adds ship-branch's table: the review rounds, the CodeRabbit status, the acceptance results, the
  recomposed history and the watch.
