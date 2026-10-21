---
description: Take one card from a written tasks.md to an open pull request, with the
  project's gates enforced and the branch recomposed into clean commits before it
  is published. The review fixes follow as one commit. Never merges.
---



# Implement card

`speckit.implement` does the engineering: the gates, the tasks, the definition
of done and acceptance. This command wraps it in the card's outward-facing
work: the branch before it, and the commit, the push, the pull request, the
review loop of `speckit.review.run` on the open pull request's branch, the Jira
verdict and the board move after it. A card is done when its pull request is
open and reviewed, not when the tasks are checked off.

`$ARGUMENTS` resolves in this order:

1. A feature directory, or a path inside one (for example `specs/003-checkout`).
2. Empty. Run the `check-prerequisites` script under `.specify/scripts/` with
   `--json --require-tasks --include-tasks` (bash) or `-Json -RequireTasks
   -IncludeTasks` (PowerShell) and take `FEATURE_DIR` from its output. That
   script also **rewrites** `.specify/feature.json`. Read its current value
   first, and restore it before this command returns, including a return
   through a hard stop.

`FEATURE_DIR/tasks.md` must exist. When it does not, stop and name the path
you looked at; the tasks phase owns writing it.

Read `.specify/extensions/card/card-config.yml` when it exists.
Every value in it is optional: an empty `code_repo.path` means spec and code
share one repo, and an empty `board.registry_path` means Phase 3 reports the
pull request URL and stops there.

Read `.specify/extensions/card/templates/code-repo.md` before the first command
that touches code. It defines the code repo, the trunk, the GitHub repo and a
docs-only card, which every phase below uses.

## Hard stops

- **A hard stop in `speckit.implement` stops this command.** Report it, and run
  no close-out.
- **This command never runs a merge.** `tasks.md` may list one. The open pull
  request is the deliverable.

## Phase 1: Branch

`tasks.md` often carries its own branch, push, pull-request, and merge steps.
`speckit.implement` leaves every one of them. This phase owns the branch, and
Phase 3 owns the commit, the push and the pull request. No phase runs a merge.

Stop on a dirty working tree in the code repo and name what is uncommitted.
Nothing branches over uncommitted work, because Phase 3 would commit it onto
the feature branch as though this card had written it.

Create the branch, in the code repo when one is configured, from the remote
trunk, never from whatever the code repo has checked out. The form below
switches to a branch that already exists and creates one that does not, so a
resumed run reaches the same place as a first run:

```
git -C <code_repo.path> fetch origin
git -C <code_repo.path> switch <NNN-name> || git -C <code_repo.path> switch --no-track -c <NNN-name> origin/<trunk>
```

A docs-only card creates no branch.

**Done when:** the branch exists and is checked out, or the card is
docs-only.

## Phase 2: Implement

`speckit.implement` must be the one the `bizstream-implement` preset ships.
When `.specify/presets/bizstream-implement/` does not exist, stop and name
`specify preset add bizstream-implement`: stock `speckit.implement` runs none
of the gates.

Run `speckit.implement FEATURE_DIR` in this session, as a command, and wait for
its report. It is the orchestrator for the tasks, and it dispatches its own
workers and reviewers.

A failed acceptance verdict is not a stop here. Name it in the close-out
question, so the owner decides before anything is published.

**Done when:** `speckit.implement` reported, or it hit a hard stop.

## Phase 3: Close-out

Every step here is outward-facing: the push and the pull request are public,
and a board transition is visible past this session. Confirm the close-out
with the user once, then run the whole block. Ask with `AskUserQuestion`, and
start the question with `Publish the close-out`: owner steering keeps every
question that names publishing for the owner, so an autopilot run never pushes
on a predicted answer.

1. Unless the card is docs-only, commit the code, in the code repo, on the
   branch Phase 1 created. Immediately before `git commit`, run
   `git -C <code_repo.path> fetch origin` and
   `git -C <code_repo.path> status --short --branch`, and confirm the branch
   it names is the one Phase 1 created. Another branch is a stop.
   Commit the `[X]` task marks and any spec drift in this repo as a separate
   commit, on the branch this repo is already on. In split mode Phase 1
   branched the code repo only. Name both branches in the report.
2. Push the branch and open the pull request against trunk, with
   `--repo <owner/name>` on every `gh` call. Fill the repo's pull request
   template when one exists. When `spec.md` carries a Jira line, start the
   title with its keys, `KEY-2: <title>` or `KEY-2, KEY-5: <title>`. Never
   merge it: see Hard stops. A docs-only card skips this step.
3. Review the open pull request's branch. A docs-only card has no code diff and
   skips this step. Invoke `speckit.review.run` in this session, as a command,
   and wait for its report. When no skill of that name is registered, read its
   file `.specify/extensions/review/commands/speckit.review.run.md` and follow
   it. Name its checks: re-run Phase 4 of `speckit.implement`, and Phase 5 when
   a fix touched behavior an acceptance item names. Pass the phase findings of
   the `speckit.implement` report as its earlier findings. The review command
   commits nothing, so when it fixed anything, commit the fixes in the code
   repo on the same branch, as in step 1, and push them to the same pull
   request. Open no second pull request. A review that did not run, a "needs a
   rethink" line, or a finding the review leaves to the owner is not a stop:
   name each one in the report and as a comment on the pull request.
4. When the `jira` extension is installed and `spec.md` carries a Jira line,
   run `speckit.jira.verify FEATURE_DIR` to post the one verdict, then hand
   `speckit.jira.cards` `pr <FEATURE_DIR>` and let it decide the move. It
   owns that condition under Track a pull request. `board.registry_path` does
   not apply to this path.
5. Otherwise, when `board.registry_path` is set, read the card key from that
   file. Read the tracker's available transitions before moving anything:
   the status set is project-specific. Apply `board.in_review_status` once
   every other spec feeding the same card has a pull request that is open or
   merged, or is docs-only with its tasks done. Find a spec's pull request
   with `gh pr list --repo <owner/name> --state all --limit 1000 --json
   number,title,body,url,state,headRefName`: its branch equals the spec
   directory name or starts with it followed by `-` or `/`, or its title
   names that directory. Until then, add
   a comment with this card's pull request link, or naming the spec for a
   docs-only card. When `board.registry_path` is empty, skip this step.
6. Restore `.specify/feature.json` to the value read at the start, when this
   command ran the `check-prerequisites` script.

**Done when:** the pull request is open against trunk and unmerged, the review
ran and its fixes are pushed to that pull request, or the card is docs-only,
the board step ran or was skipped by config, and the state
file is restored.

## Report

Close with one table: the branch, the `speckit.implement` report rows (tasks,
definition of done, acceptance), the pull request URL, the review table of `speckit.review.run`, the Jira
verdict posted, and the board card moved or commented, if any.

## Guardrails

- This command edits the code repo, the branch, and the board. It never edits
  `spec.md`, `plan.md`, or a checklist marker.

## Done when

- [ ] The branch exists, from the remote trunk, or the card is docs-only
- [ ] `speckit.implement` ran to its end, or its hard stop is reported
- [ ] No merge ran, even where `tasks.md` listed one
- [ ] The pull request is open against trunk, unmerged, and the review loop
      ran on its branch with its fixes pushed, or the card is
      docs-only, and the board step ran or was skipped by config
- [ ] `.specify/feature.json` is restored


## Recompose before publishing

> This section changes Phase 3 of the command above. Where the two conflict, this section wins.

Every branch reaches its pull request as a few clean commits. The development history stays as it is until Phase 3.
The recomposition runs once, there, with the `recompose-branch` skill, after the commit and before the push. It
changes the history only, never the tree. It runs in this session, never in a subagent, because the owner approves
the publish and sometimes the grouping.

Phase 3 runs in this order:

1. Run Phase 3 step 1, the commit, before the close-out question. A local commit publishes nothing.
2. Plan the recomposition with the `recompose-branch` skill, steps 1, 2 and 5, with these overrides:
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
   - **Confident or a judgment call.** The skill's step 5 lists the reasons to ask. A grouping with none of them is
     confident. A grouping with one of them is a judgment call.
3. Build a confident grouping now, before the close-out question: run the skill's steps 3, 4, 6 and 7. Back up, soft
   reset, build the commits, then prove that `git diff <backup> HEAD` is empty and the working tree is clean. Every
   commit passes the identity flags the constitution's Review and merge section names. A judgment call commits
   nothing yet.
4. Ask the close-out question. Start it with `Publish the close-out`, as Phase 3 requires. Show the base and the
   commits that collapse.
   - **Confident.** Show the new history as built, each commit with its message and its files. The owner's answer
     approves the publish only.
   - **A judgment call.** Show the proposed commits with their messages and files, and name the open question. The
     owner's answer approves the plan and the publish together. On approval, run the skill's steps 3, 4, 6 and 7.

   An answer that changes the groups gets a new question. When the commits are already built, rebuild them from the
   backup first: run `git reset --hard <backup>`, then the skill's steps 4, 6 and 7 again. A failed tree check is a
   hard stop at any point: keep the backup, report the failure with `git reset --hard <backup>` as the way back, and
   push nothing.
5. Then run Phase 3 step 2. A branch that is already on the remote needs `git push --force-with-lease`. Name that in
   the close-out question when it applies, and never run it without the owner's approval.
6. Delete the backup branch with `git branch -D <backup>` right after `gh pr create` succeeds. Keep it until then:
   `.claude/hooks/require-recomposed-branch.sh` refuses `gh pr create` unless `backup/<branch>` exists with the same
   tree. The report says the backup was deleted.
7. Run Phase 3 step 3, the review, with one change: commit nothing until `speckit.review.run` has finished every
   round. Then commit every fix from every round as one commit, `fix(<NNN>): address review feedback`, and push it
   to the same pull request. A review that fixed nothing adds no commit. Never recompose the branch again after the
   pull request opens.

A docs-only card has no branch to recompose, and this section does not apply to it.

**Also done when:** the branch was recomposed after the commit and before the push, its tree matched the backup, the
backup was deleted once the pull request opened, and the review fixes, if any, landed as one commit on top.
