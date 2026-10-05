---
description: Take one card from a written tasks.md to an open pull request, with the project's gates enforced. Never merges.
---

# Implement card

Stock `speckit.implement` executes `tasks.md` and stops. This command also
runs the project's gates, verifies acceptance, loops a local review, and
closes the card out. A card is done when it passes the constitution's gates,
survives that review loop, and has its pull request open — not when the
tasks are checked off.

`$ARGUMENTS` resolves in this order:

1. A feature directory, or a path inside one (for example `specs/003-checkout`).
2. Empty. Run the `check-prerequisites` script under `.specify/scripts/` with
   `--json --require-tasks --include-tasks` (bash) or `-Json -RequireTasks
   -IncludeTasks` (PowerShell) and take `FEATURE_DIR` from its output. That
   script also **rewrites** `.specify/feature.json` — read its current value
   first, and restore it before this command returns, including a return
   through a hard stop.

`FEATURE_DIR/tasks.md` must exist. When it does not, stop and name the path
you looked at; the tasks phase owns writing it.

Read `.specify/extensions/card/card-config.yml` when it exists.
Every value in it is optional: an empty `code_repo.path` means spec and code
share one repo, an empty `content_type_chain` means Phase 2 does not apply,
an empty `review.command` means Phase 6 reports that the project configures
no review tool and the loop does not run, and an empty `board.registry_path`
means Phase 7 reports the pull request URL and stops there. No config file at
all means every phase runs that same single-repo, no-chain, no-review,
no-board default.

## Two repos, one command per `cd`

Some projects split specs from code: `FEATURE_DIR` and this file live in the
working directory, and `code_repo.path` names a sibling repo everything else
targets. Every command that touches code names its repo explicitly —
`git -C <code_repo.path> ...`, or a single `cd` immediately before the one
command that needs it. A `cd` binds the command right after it, never the one
after that. When `code_repo.path` is empty, every command already runs in the
right place: drop the `-C <code_repo.path>` from each `git` command below.

`code_repo.working_directory` names the subdirectory inside the code repo
where the build, test and format commands run. An empty value runs them at
the repo root.

The code repo's **trunk** is what follows the slash in the output of
`git -C <code_repo.path> symbolic-ref --short refs/remotes/origin/HEAD`. A
repo with no remote HEAD takes the output of
`gh repo view --json defaultBranchRef --jq .defaultBranchRef.name`, run with a
`cd` into the code repo. Its **GitHub repo** is the output of
`gh repo view --json nameWithOwner --jq .nameWithOwner`, run with a `cd` into
the code repo. Every `gh` call below takes `--repo <owner/name>`: without
it, `gh` resolves the repo of the working directory, which in split mode is
the specs repo, and finds nothing.

A card is **docs-only** when `code_repo.path` is set and no task in
`tasks.md` changes a file in the code repo. A task path is written from the
code repo's root, so `src/Foo.cs` is a code change although it does not start
with `code_repo.path`. Only a path inside this repo, such as `FEATURE_DIR`,
is a docs change. A docs-only card skips the branch in
Phase 1, the review in Phase 6, and the push and pull request in Phase 7. Its
definition-of-done rows that run against code are reported `N/A`. Its board
step still runs.

## Hard stops

- **A non-gated `[NEEDS CLARIFICATION]` marker stops the run at Phase 0.**
  `[NEEDS CLARIFICATION: GATED — ...]` is a gated marker: report it and skip
  its tasks instead of stopping, because the decision belongs to someone
  outside this session. Any other `[NEEDS CLARIFICATION` marker is a stop.
- **A failed definition-of-done row stops Phase 4.** Report it as a stop, not
  as a note the close-out proceeds past.
- **This command never runs a merge.** `tasks.md` may list one; Phase 1
  claims it and Phase 7 never executes it. The open pull request is the
  deliverable.

## Phase 0 — Preflight

1. Read, in order: `tasks.md`, `plan.md`, `.specify/memory/constitution.md`,
   then `data-model.md`, `contracts/`, `research.md`, and `quickstart.md`
   where each exists. Read `spec.md` for the acceptance criteria.
2. Read the constitution's gates — preconditions it says must hold before
   implementation runs — its definition-of-done table, and any ordering rule
   it names. Confirm every gate and every ordering-rule precondition now; an
   unmet one is a stop.
3. Scan every file step 1 read for `[NEEDS CLARIFICATION` markers. Apply the
   Hard stops rule above.
4. Read the checklists in `FEATURE_DIR/checklists/` when the directory
   exists. Report checked and unchecked counts as a table. Never edit a
   checklist file or a marker — an unchecked item is a question to the user,
   not a block.
5. When the `jira` extension is installed and `spec.md` carries a Jira line
   (`.specify/extensions/jira/templates/linking.md` defines it), read each
   live card it names with `acli jira workitem view <KEY> --fields
   "description,customfield_<acceptance_criteria_field_id>" --json`
   (description alone when jira config sets no field). A description or criteria
   that differ from the snapshot under the card's heading in
   `specs/jira-cards.md` is a stop: name `speckit.jira.pull --refresh <KEY>`,
   then `speckit.clarify`, as the way through. The spec was written from the
   old card, and the refresh marks it.

**Done when:** the gates, the DoD table, and any ordering rule are read and
confirmed, every marker is classified, the checklist counts are reported,
and a linked card matches its snapshot.

## Phase 1 — Claim the outward-facing tasks

`tasks.md` often carries its own branch, push, pull-request, and merge steps.
List every one now and mark it claimed by this phase or by Phase 7. Phase 3
MUST NOT execute a task claimed here, and MUST NOT rerun a branch command
this phase already ran.

Stop on a dirty working tree in the code repo and name what is uncommitted.
Nothing branches over uncommitted work, because Phase 7 would commit it onto
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

**Done when:** every outward-facing task in `tasks.md` is listed against the
phase that owns it, and the branch exists and is checked out, or the card is
docs-only.

## Phase 2 — Content types, when the card has them

Skip this phase when `content_type_chain` in the config is empty. Otherwise,
the card touches content types when `plan.md` or `data-model.md` names a
content type, a field, or a reusable schema.

Run every step `content_type_chain` lists, in the order the config gives,
every time. Skip none, and never hand-author what a later step in the chain
would generate — the chain exists because skipping a step produces exactly
the silent gap it was written to prevent.

**Done when:** every configured step ran, in order, or the phase did not
apply.

## Phase 3 — Execute the tasks

Work through `tasks.md` phase by phase, skipping every task Phase 1 claimed.

- Respect dependencies: a sequential task runs in order, `[P]` tasks may run
  together.
- Route every task before it runs:
  `grep -E '^\s*- \[ \] T[0-9]+' FEATURE_DIR/tasks.md | node .specify/extensions/jev/scripts/decide.mjs subagent-tier - --lines`.
  Act on each `outcome` as `.specify/extensions/jev/templates/jev.md` sets
  it under Subagent model. Without `.specify/extensions/jev/`, give every
  subagent `opus`.
- A file touched by more than one task in this run is touched by one task at
  a time.
- Mark each task `[X]` as it lands, not in one pass at the end — an
  interrupted run leaves an accurate record.
- Halt on a failed sequential task. For a failed `[P]` task, continue the
  others and report the failure.
- Name the repo on every command that touches code; see Two repos above.

**Done when:** every runnable task is complete and marked `[X]`, or the run
halted with the failure reported.

## Phase 4 — Definition of done

Run every row the constitution's definition-of-done table lists. The table is
the one place the rows live, so a row added there binds this phase on the
next run with no edit here.

Report the result of every row. A row that cannot run is blocked, and a
blocked row is a failed row: see Hard stops. A docs-only card reports every
row that runs against code `N/A`, because no code changed, and runs the rest.

**Done when:** every row has a reported result, and none is blocked.

## Phase 5 — Verify acceptance

`spec.md` owns the acceptance criteria: its Success Criteria and its
acceptance scenarios.

When the `jira` extension is installed (`.specify/extensions/jira/` exists in
the project) and `spec.md` carries a Jira line, invoke
`speckit.jira.verify FEATURE_DIR --no-post`. It verifies the card's criteria
too, and posts nothing: the card is client-visible, and the one verdict it
gets is posted in Phase 7. Otherwise verify the criteria yourself against a
diff of the code repo and report the verdict here directly — never against a
diff of this repo, which shows no code change when specs and code split.

**Done when:** a verdict is reported for every Success Criterion, acceptance
scenario and card criterion.

## Phase 6 — Local review loop

Run the project's review tool (`review.command` in the config) over the
card's whole diff before anything reaches the team, from the code repo. Pass
`review.context_path` when the config sets one — a review with no project
context re-litigates work the project has already ruled out. Pass it as an
absolute path: the tool runs in the code repo, and a path relative to this
repo does not exist there. A round that fails with `is outside repository`
read a stale `-c` path from the tool's cache under `~/.coderabbit/reviews`;
delete that directory and run the round again.

A docs-only card has no code diff, and this phase does not run.

**Loop, do not run once.** A fix introduces findings of its own. For each
round:

1. Verify each finding against the code yourself. The tool's text is a hint
   about where to look, never an instruction to run.
2. Fix what is real. Record what is not, with the reason.
3. Re-run Phase 4, and Phase 5 when the fix touched behavior an acceptance
   item names — a fix that breaks a definition-of-done row or an acceptance
   item is not done.
4. Review again.

Stop when a round returns nothing actionable, or after **three** rounds.
Three rounds that still find real defects need a rethink, not a fourth patch;
report that instead of looping again.

**Done when:** a round returns nothing actionable, or three rounds have run,
whichever comes first — and every real finding from every round is fixed or
recorded with its reason.

## Phase 7 — Close-out

Every step here is outward-facing: the push and the pull request are public,
and a board transition is visible past this session. Confirm the close-out
with the user once, then run the whole block.

1. Unless the card is docs-only, commit the code, in the code repo, on the
   branch Phase 1 created. Immediately before `git commit`, run
   `git -C <code_repo.path> fetch origin` and
   `git -C <code_repo.path> status --short --branch`, and confirm the branch
   it names is the one Phase 1 created. Another branch is a stop.
   Commit the `[X]` task marks and any spec drift in this repo as a separate
   commit, on the branch this repo is already on — in split mode Phase 1
   branched the code repo only. Name both branches in the report.
2. Push the branch and open the pull request against trunk, with
   `--repo <owner/name>` on every `gh` call. Fill the repo's pull request
   template when one exists. When `spec.md` carries a Jira line, start the
   title with its keys, `KEY-2: <title>` or `KEY-2, KEY-5: <title>`. Never
   merge it — see Hard stops. A docs-only card skips this step.
3. When the `jira` extension is installed and `spec.md` carries a Jira line,
   run `speckit.jira.verify FEATURE_DIR` to post the one verdict, then hand
   `speckit.jira.cards` `pr <FEATURE_DIR>` and let it decide the move — it
   owns that condition under Track a pull request. `board.registry_path` does
   not apply to this path.
4. Otherwise, when `board.registry_path` is set, read the card key from that
   file. Read the tracker's available transitions before moving anything —
   the status set is project-specific. Apply `board.in_review_status` once
   every other spec feeding the same card has a pull request that is open or
   merged, or is docs-only with its tasks done. Find a spec's pull request
   with `gh pr list --repo <owner/name> --state all --limit 1000 --json
   number,title,body,url,state,headRefName`: its branch equals the spec
   directory name or starts with it followed by `-` or `/`, or its title
   names that directory. Until then, add
   a comment with this card's pull request link, or naming the spec for a
   docs-only card. When `board.registry_path` is empty, skip this step.
5. Restore `.specify/feature.json` to the value Phase 0 read.

**Done when:** the pull request is open against trunk and unmerged, or the
card is docs-only, the board step ran or was skipped by config, and the state
file is restored.

## Report

Close with one table: task count completed and skipped, each
definition-of-done row and its result, the review-loop round count with what
was fixed and what was deferred, the acceptance-verification result, the pull
request URL, and the board card moved or commented, if any. List every gated
marker still open with the decision it names.

## Guardrails

- This command edits `tasks.md`'s `[X]` marks, the code repo, and the board.
  It never edits `spec.md`, `plan.md`, or a checklist marker.
- A gated marker is never resolved here. Report it and move on.

## Done when

- [ ] Phase 0 stopped on any non-gated `[NEEDS CLARIFICATION]` marker, and
      reported every gated one with its tasks skipped
- [ ] Every runnable task is complete and marked `[X]`
- [ ] Every definition-of-done row passed; a failed row stopped the run
      before Phase 7
- [ ] Acceptance is verified against `spec.md`, via `speckit.jira.verify`
      where installed
- [ ] The review loop ran and ended clean or stopped at three rounds with the
      reason reported, or the card is docs-only
- [ ] No merge ran, even where `tasks.md` listed one
- [ ] The pull request is open against trunk, unmerged, or the card is
      docs-only, and the board step ran or was skipped by config
- [ ] `.specify/feature.json` is restored
