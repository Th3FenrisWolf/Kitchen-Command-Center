---
description: Execute tasks.md with the project's gates enforced, subagent workers, a review per phase, the definition of done, and acceptance. Never pushes.
---

# Implement

This command replaces stock `speckit.implement`. It executes `tasks.md`, and it
also runs the project's gates and verifies acceptance. The work is done when it
passes the constitution's gates, not when the tasks are checked off. The review
loop of `speckit.review.run` runs later, on the open pull request's branch:
`speckit.card.implement` owns it. This command ends with the change on the
checked-out branch, uncommitted unless a hook commits it. The branch, the push,
the pull request and the card belong to `speckit.card.implement`, which runs
this command.

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
share one repo, and an empty `content_type_chain` means Phase 2 does not apply.
No config file at all means every phase runs that same single-repo,
no-chain default.

Read `.specify/extensions/card/templates/code-repo.md` before the first command
that touches code. It defines the code repo, the trunk, the GitHub repo and a
docs-only card, which every phase below uses.

## Hard stops

- **A non-gated `[NEEDS CLARIFICATION]` marker stops the run at Phase 0.**
  A marker that starts `[NEEDS CLARIFICATION: GATED` is a gated marker: report it and skip
  its tasks instead of stopping, because the decision belongs to someone
  outside this session. Any other `[NEEDS CLARIFICATION` marker is a stop.
- **A failed definition-of-done row stops Phase 4.** Report it as a stop, not
  as a note the run proceeds past.
- **This command never creates a branch, pushes, opens a pull request, or
  merges.** Phase 1 leaves every such task in `tasks.md`.

## Hooks

Read `.specify/extensions.yml` when it exists. Run its `before_implement`
hooks before Phase 0 and its `after_implement` hooks after Phase 5, in
priority order. Skip a hook with `enabled: false`, and a hook with a non-empty
`condition`. Run a hook with `optional: false` as a command in this session,
its dots replaced with hyphens, and wait for it. Name a hook with
`optional: true` in the report, and do not run it.

## Phase 0: Preflight

1. Read, in order: `tasks.md`, `plan.md`, `.specify/memory/constitution.md`,
   then `data-model.md`, `contracts/`, `research.md`, and `quickstart.md`
   where each exists. Read `spec.md` for the acceptance criteria.
2. Read the constitution's gates (preconditions it says must hold before
   implementation runs), its definition-of-done table, and any ordering rule
   it names. Confirm every gate and every ordering-rule precondition now; an
   unmet one is a stop.
3. Scan every file step 1 read for `[NEEDS CLARIFICATION` markers. Apply the
   Hard stops rule above.
4. Read the checklists in `FEATURE_DIR/checklists/` when the directory
   exists. Report checked and unchecked counts as a table. Never edit a
   checklist file or a marker. An unchecked item is a question to the user,
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

## Phase 1: Leave the outward-facing tasks

`tasks.md` often carries its own branch, push, pull-request, and merge steps.
List every one now. Phase 3 MUST NOT execute one. They belong to
`speckit.card.implement`, or to the owner when this command runs alone. This
command works on the branch that is checked out.

**Done when:** every outward-facing task in `tasks.md` is listed as left.

## Phase 2: Content types, when the card has them

Skip this phase when `content_type_chain` in the config is empty. Otherwise,
the card touches content types when `plan.md` or `data-model.md` names a
content type, a field, or a reusable schema.

Run every step `content_type_chain` lists, in the order the config gives,
every time. Skip none, and never hand-author what a later step in the chain
would generate. The chain exists because skipping a step produces exactly
the silent gap it was written to prevent.

**Done when:** every configured step ran, in order, or the phase did not
apply.

## Phase 3: Execute the tasks

Work through `tasks.md` phase by phase, skipping every task Phase 1 left.

Route the rows first, as the bullet below sets. Then pick a mode for each
`tasks.md` phase before its first dispatch. **Team mode** needs both of these:

- `printenv CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS CLAUDE_CODE_ENABLE_TODO_TOOLS`
  prints `1` twice.
- The phase holds three or more **lanes**. A lane is a set of rows whose files
  no row of another lane touches, by the paths the rows and `plan.md` name.
  Rows that name no path share one lane. Only rows that may run together
  form separate lanes: `[P]` rows, or rows with no dependency on another
  lane's rows. A row Jev routes to `none`, `owner` or `code` stays out of
  every lane.

Anything else is **subagent mode**. In subagent mode never pass `name` to an
Agent call: with teams on, a name starts a teammate.

Team mode changes how a worker starts and nothing else. Start one fresh
teammate per lane with an Agent call that carries `name: lane-<phase>-<n>`
and the model, write scope and turn bound that `jev.md` sets for a
teammate under Subagent model. The worker rules below bind a teammate too,
and it returns its result to this session with SendMessage. When an Agent call
for a teammate errors, let the running teammates finish, run the rows still
open in subagent mode, and say so in the Report.

- Respect dependencies: a sequential task runs in order, `[P]` tasks may run
  together.
- Route every task before it runs:
  `grep -E '^\s*- \[ \] T[0-9]+' FEATURE_DIR/tasks.md | node .specify/extensions/jev/scripts/decide.mjs subagent-tier - --lines`.
  Act on each `outcome` as `.specify/extensions/jev/templates/jev.md` sets
  it under Subagent model. Without `.specify/extensions/jev/`, give every
  subagent `opus`.
- A task row whose outcome names a model runs in a worker on that `model`,
  never as an agent type. `none`, `owner` and `code` follow their rows in
  `jev.md`. Never hand the whole card to one subagent. A session that
  hands off more than one row at once dispatches `bizstream-ai:builder` for
  one `tasks.md` phase at most, and starts each next builder fresh from the
  state note the last one wrote. A phase with an `opus` row splits as
  `jev.md` sets it: the `opus` rows go to their own Opus builder, and the
  rest of the phase stays on the builder with no `model`.
- A file touched by more than one task in this run is touched by one task at
  a time.
- Mark each task `[X]` as it lands, not in one pass at the end, so an
  interrupted run leaves an accurate record.
- Halt on a failed sequential task. For a failed `[P]` task, finish the
  other `[P]` tasks running beside it, report the failure, and stop before
  Phase 4.
- Name the repo on every command that touches code, as `code-repo.md` sets it.

This session is the orchestrator. It holds the task list and the results, and
the code stays in the workers. An **architect** subagent below is a
dispatch with `subagent_type: bizstream-ai:architect` and no `model`, which
runs on Opus at high effort. Without the bizstream-ai plugin, pass
`model: opus` instead. Every **worker** subagent:

- Gets a self-contained prompt: the task rows verbatim, the repo and working
  directory, the paths from `plan.md` and `data-model.md` the rows touch, the
  constitution's coding rules, the build or test command that checks its
  change, and the path of each project skill the rows need. It has no memory
  of this session.
- Never asks the user anything, never commits, and never runs a task Phase 1
  left. An unresolved decision goes into its return value.
- Returns a fixed shape: task ids, status (`done`, `blocked`, or `failed`),
  files changed, the check it ran and its result, and a one-line note when
  blocked or failed. It returns no diff and no file content.

After each `tasks.md` phase lands, dispatch one **reviewer**: an architect
subagent with `model: sonnet`, with fresh context. Its prompt carries the phase's task rows, the
acceptance scenarios they serve, the repo, and every file the workers
returned as changed. Workers do not commit, so a new file is untracked and
`git diff` does not show it. The reviewer runs
`git -C <code_repo.path> diff HEAD -- <file>` for each tracked file and reads
each untracked file in full. For a tracked file it reads the changed hunks and
the functions around them, not the whole file, and reads the code they call.
A file with an empty diff, because a worker committed it, it reads in full. It
returns defects only: file, line, defect, and why it breaks a task or a
scenario. It edits nothing.

The phase review only finds. Keep its defects as the **phase findings**, and
go on to the next phase. Report them: `speckit.review.run` confirms and fixes
them once, with the rest of the diff, so no defect is confirmed or fixed twice.

**Done when:** every runnable task is complete and marked `[X]`, and every
phase has a reviewer result, or the run halted with the failure reported.

## Phase 4: Definition of done

Run every row the constitution's definition-of-done table lists. The table is
the one place the rows live, so a row added there binds this phase on the
next run with no edit here.

Report the result of every row. A row that cannot run is blocked, and a
blocked row is a failed row: see Hard stops. A docs-only card reports every
row that runs against code `N/A`, because no code changed, and runs the rest.

**Done when:** every row has a reported result, and none is blocked.

## Phase 5: Verify acceptance

`spec.md` owns the acceptance criteria: its Success Criteria and its
acceptance scenarios.

When the `jira` extension is installed (`.specify/extensions/jira/` exists in
the project) and `spec.md` carries a Jira line, invoke
`speckit.jira.verify FEATURE_DIR --no-post`. It verifies the card's criteria
too, and posts nothing: the card is client-visible, and the one verdict it
gets is posted at close-out by `speckit.card.implement`. Otherwise dispatch one verifier architect subagent,
with fresh context, because the session that wrote the code grades its own
work too kindly. Its prompt carries `spec.md`'s path, the repo, and the trunk.
It verifies every criterion against a diff of the code repo, never against a
diff of this repo, which shows no code change when specs and code split. A
docs-only card is the exception: it verifies against this repo's diff. It
returns one row per criterion: id, verdict, and the file and line that prove
it. Report its verdict here.

**Done when:** a verdict is reported for every Success Criterion, acceptance
scenario and card criterion.

## Report

Restore `.specify/feature.json` to the value read at the start, then close
with one table: task count completed and skipped, each outward-facing task
left, the reviewer result for each `tasks.md` phase, each definition-of-done
row and its result, the mode Phase 3 ran in per `tasks.md` phase, the phase findings, and a verdict
for each Success Criterion and acceptance scenario. List every gated marker still open with the decision it names, and
every optional hook named.

## Guardrails

- This command edits `tasks.md`'s `[X]` marks and the code repo. It never
  edits `spec.md`, `plan.md`, or a checklist marker.
- A gated marker is never resolved here. Report it and move on.

## Done when

- [ ] Phase 0 stopped on any non-gated `[NEEDS CLARIFICATION]` marker, and
      reported every gated one with its tasks skipped
- [ ] Every runnable task is complete and marked `[X]`, and no branch, push,
      pull-request or merge task ran
- [ ] Every task ran in a worker on the tier Jev routed, and every phase's
      reviewer ran as an architect subagent with fresh context
- [ ] Every definition-of-done row passed; a failed row stopped the run
- [ ] Acceptance is verified against `spec.md`, via `speckit.jira.verify
      --no-post` where installed
- [ ] The phase findings are reported
- [ ] `.specify/feature.json` is restored
