---
description: Take one card from a written tasks.md to an open pull request, with the project's gates enforced and the branch recomposed into clean commits before it is published. Never merges.
strategy: wrap
---

{CORE_TEMPLATE}

## Recompose before publishing

> This section changes Phase 7 of the command above. Where the two conflict, this section wins.

Every branch reaches its pull request as a few clean commits. The development history stays as it is until Phase 7.
The recomposition runs once, there, with the `recompose-branch` skill, and it changes the history only, never the
tree. It runs in this session, never in a subagent, because the owner approves its plan.

Phase 7 runs in this order:

1. Run Phase 7 step 1, the commit, before the close-out question. A local commit publishes nothing.
2. Plan the recomposition with the `recompose-branch` skill, steps 1 to 5, with these overrides:
   - **Base.** Run `.specify/presets/kcc-recompose/scripts/recompose-base.sh <trunk>`. It prints the base and the
     commits that will collapse. The base is the closest branch below this one that `origin/<trunk>` does not
     contain, such as the previous phase's branch or a tooling branch this one stacks on. Without one, it is
     `git merge-base HEAD origin/<trunk>`.
   - **A merged parent.** When the base is a branch, look up its pull request with
     `gh pr list --repo <owner/name> --head <base branch without origin/> --state merged`. A merged one is a hard
     stop: the branch must first move onto trunk with `git rebase --onto origin/<trunk> <base> <branch>`. Name that
     command for the owner and run nothing.
   - **Backup.** Name the backup `backup/<branch without its type prefix>`, as the skill does. When that branch
     exists from an earlier recomposition, rename it to `backup/<name>-<n>` first, with the lowest free `n`.
   - **A plan, not commits.** Propose the groups and their messages. Commit nothing yet.
3. Put the plan into the close-out question. Start it with `Publish the close-out`, as Phase 7 requires. Show the
   base, the commits that will collapse, and each new commit with its message and its files. The owner's answer
   approves the plan and the publish together. An answer that changes the groups gets a new plan and a new question.
4. On approval, run the skill's steps 3, 4, 6 and 7: back up, soft reset, build the commits, then prove that
   `git diff <backup> HEAD` is empty and the working tree is clean. Every commit passes the identity flags the
   constitution's Review and merge section names. A failed proof is a hard stop: report it with
   `git reset --hard <backup>` as the way back, and push nothing.
5. Then run Phase 7 from step 2. A branch that is already on the remote needs `git push --force-with-lease`. Name
   that in the close-out question when it applies, and never run it without the owner's approval.
6. Keep the backup branch. The report names it with `git branch -D <backup>`, for the owner to run once the pull
   request looks right.

A docs-only card has no branch to recompose, and this section does not apply to it.

**Also done when:** the branch was recomposed after the review loop and before the push, its tree matches the
backup, and the report names the backup branch.
