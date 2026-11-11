---
description: Take one card from a written tasks.md to an open pull request, with the project's gates enforced. The close-out runs the ship-branch skill without its Jira steps, so the review, CodeRabbit, the acceptance check and the recomposition all finish before the branch is published. Never merges.
strategy: wrap
---

{CORE_TEMPLATE}

## Close out through ship-branch

> This section changes Phase 3 of the command above. Where the two conflict, this section wins.

The `ship-branch` skill, with `--no-jira`, replaces Phase 3 steps 2, 3 and 6: the review, the push, the pull
request and the watch. It runs in this session, never in a subagent, because the owner approves the publish and
sometimes the grouping. A docs-only card has no code diff: it runs Phase 3 as written, and this section does not apply
to it.

Phase 3 runs in this order:

1. Run Phase 3 step 1, the commit, before ship-branch starts. A local commit publishes nothing.
2. Invoke the `ship-branch` skill with `--no-jira`. When `speckit.flow.run` runs this command, add `--triage`, so the
   review tier follows the size and the risk of the diff. Run by hand, the tier is `full`. When no skill of that name
   is registered, stop: report that the close-out needs `~/.claude/skills/ship-branch`, and push nothing. Run every
   step of the skill, with these overrides:
   - **Base.** Run `.specify/presets/kcc-ship/scripts/recompose-base.sh <trunk>`. It prints the base and the commits
     that will collapse. The base is the closest branch below this one that `origin/<trunk>` does not contain, such as
     the previous phase's branch or a tooling branch this one stacks on. Without one, it is
     `git merge-base HEAD origin/<trunk>`. The reviews, the acceptance check and the recomposition all use it.
   - **A merged parent.** When the base is a branch, look up its pull request with
     `gh pr list --repo <owner/name> --head <base branch without origin/> --state merged`. A merged one is a hard stop:
     the branch must first move onto trunk with `git rebase --onto origin/<trunk> <base> <branch>`. Name that command
     for the owner and run nothing.
   - **Checks.** The baseline and every check after a fix run the definition-of-done rows of
     `.specify/memory/constitution.md` that apply to the diff.
   - **Acceptance criteria.** The acceptance scenarios and success criteria of `FEATURE_DIR/spec.md`. Phase 5 verified
     them before the review fixes; ship-branch verifies them again on the code it publishes.
   - **Identity.** Every commit that ship-branch or `recompose-branch` makes passes the identity flags the
     constitution's Review and merge section names.
   - **Backup.** Name the backup `backup/<branch without its type prefix>`, as `recompose-branch` does. When that branch
     exists from an earlier recomposition, rename it to `backup/<name>-<n>` first, with the lowest free `n`. Keep it
     until `gh pr create` succeeds: `.claude/hooks/require-recomposed-branch.sh` refuses `gh pr create` unless
     `backup/<branch>` exists with the same tree. Delete it right after.
   - **Close-out question.** ship-branch's one question is this command's close-out confirmation. It starts with
     `Publish the close-out`, as Phase 3 requires, and it also names the commit of step 1 and the base with the commits
     that collapse.
   - **Title.** The pull request title is the plain-language summary, with no Jira key.
3. When the pull request is open, run Phase 3 steps 4, 5 and 7 as written. Steps 4 and 5 skip themselves here: no
   `spec.md` carries a Jira line, and `board.registry_path` is empty.

A blocker that ship-branch names stops the publish: a regression, an `unfixed` finding, an `unmet` criterion, or a
force push the owner has not chosen. Never recompose the branch again after the pull request opens. A fix after that,
from the watch or the owner's review, is one commit on top.

**Also done when:** ship-branch ran every step before the push, the branch was recomposed once and its tree matched the
backup, the backup was deleted once the pull request opened, and the watch is on or reported.
