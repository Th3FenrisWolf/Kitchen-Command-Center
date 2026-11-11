---
description: Apply the remediation from a bug assessment, then run the definition of done before the fix is reported. The fix is published through the ship-branch skill, which reviews it, checks it against the assessment and recomposes it before the pull request opens.
strategy: wrap
---

{CORE_TEMPLATE}

## Publish through ship-branch

> This section replaces gates 2 and 3 of the Gates above. Where the two conflict, this section wins.

- Gate 1, the definition of done, runs as written, first.
- The fix reaches a pull request only through the `ship-branch` skill with `--no-jira`, run in this session. It
  reviews before anything is published, so `speckit.review.run` does not run. When no skill of that name is
  registered, report that publishing needs `~/.claude/skills/ship-branch`, and push nothing.
- ship-branch's overrides for this project:
  - **Checks.** The rows of gate 1.
  - **Acceptance criteria.** From `.specify/bugs/<slug>/assessment.md`: the Symptom no longer occurs when the steps
    under Reproduction run, and each step of the Proposed Remediation is done.
  - **Identity.** Every commit passes the identity flags the constitution's Review and merge section names.
  - **Backup.** Keep `backup/<branch without its type prefix>` until `gh pr create` succeeds, because
    `.claude/hooks/require-recomposed-branch.sh` reads it. Delete it right after.
- When the owner wants no pull request yet, report the review as not run, and name ship-branch as the step that runs
  it.
- Never recompose the branch again after its pull request opens. A later fix is one commit on top.

The report adds ship-branch's table: the review rounds, the CodeRabbit status, the acceptance results, the pull
request and the watch.
