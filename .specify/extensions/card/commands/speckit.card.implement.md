---
description: Take one card from a written tasks.md to an open pull request. Branches, runs speckit.implement, then commits, pushes, opens the pull request, runs the review loop on its branch and pushes the fixes, and moves the card. Never merges.
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
