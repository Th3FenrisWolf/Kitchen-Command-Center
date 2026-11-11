---
name: speckit-flow-run
description: Run one feature from an idea to an open pull request, through every Spec
  Kit phase, and stop only when the owner must decide or act. The branch is recomposed
  into clean commits before it is published. Use when the user says "autopilot", "run
  the whole flow", "take this idea to a PR", or wants specify through implement without
  a stop per phase.
compatibility: Requires spec-kit project structure with .specify/ directory
metadata:
  author: github-spec-kit
  source: preset:kcc-recompose
user-invocable: true
disable-model-invocation: false
---

# Speckit Flow Run Skill

# Autopilot flow

Take one feature through every phase to an open pull request. This session is
the **orchestrator**: it dispatches each phase to a fresh subagent, reads only
what the subagent returns, and puts to the owner only what the owner holds.
The owner's attention is the scarce resource. The orchestrator's context is
the second one, so a phase's working material stays in its subagent.

`$ARGUMENTS` resolves in this order:

1. A feature directory, or a path inside one (for example `specs/003-checkout`).
   The run resumes at the phase the state script names.
2. Anything else is the feature description for a new spec.
3. Empty. Stop, and ask for the feature description.

Snapshot `.specify/feature.json` before the first phase, and restore it as the
Feature-state guard in `.specify/extensions/batch/commands/speckit.batch.run.md`
sets it. The restore is the last thing the run does, including a run that ends
on a stop.

Read `publish` from `.specify/extensions/flow/flow-config.yml`. A missing file
or value is `ask`. Only the owner sets it, in that file.

## State

`.specify/extensions/flow/scripts/flow.mjs` keeps the run's bookkeeping in
`FEATURE_DIR/.flow/state.json`, so the run resumes after a stop, a crash or a
new session. Only the script writes that file. Call it as
`node .specify/extensions/flow/scripts/flow.mjs <command> FEATURE_DIR <arguments>`,
for example `node .specify/extensions/flow/scripts/flow.mjs record specs/003-checkout analyze done`.
Each call prints one JSON line.

| Command | What it does |
|---|---|
| `next` | Names the next phase and the fact that decided it: the marker counts, the checkpoints, the unreviewed assumption ids, and at implement the `tasks-state` counts. Prints `held` with the checkpoint and its open ids by audience while a checkpoint holds |
| `record <phase> <result>` | Records `done`, `skipped`, `blocked` or `failed` for a phase. `--pr <number>` records the pull request, `--note <text>` the reason. Prints `"stop": true` and the cause when a phase reaches its second `blocked` or `failed` |
| `release` | Clears a held feature after the owner acts on its cause. It never lifts a checkpoint hold |
| `tasks-state` | Counts the open `tasks.md` rows by tag, and lists each row's blocked-on edges |
| `status` | Prints every feature under `specs/` with its phase and pull request |
| `add <checkpoint> <row>` | Writes an open decision into `FEATURE_DIR/.flow/checkpoints/<checkpoint>.json` |
| `answer <id> --answer <text> --by <who> --channel <how>` | Writes the answer into its row |
| `route <id> --audience <who> --affects <ids>` | Sends an open row to another audience, with the ids that wait for it |
| `checkpoints` | Lists each checkpoint with its open and interim ids |

The run is a loop: call `next`, run the phase it names, call `record` with the
result, and call `next` again. The script decides which phase is next and when
a phase is done. Do not read the feature directory to decide it. When it
prints `done`, write the report. When it prints `held`:

- With a `checkpoint`: open rows hold the phase in `blocks`. With a person
  present, hand the feature to `speckit.grill.run` in this session, then call
  `next`. With no person present, stop (see No person present).
- With a `cause`: a phase failed twice. Stop and put the `cause` to the owner.

A phase whose artifact was drafted on an answer that later changed comes back
from `next` with a `reason` that names the row. Dispatch it with each changed
row's question and answer in the prompt, so it rewrites the artifact.

`tasks-state` gives each open row one tag:

- `person`: the row carries `[PERSON]`. Only a person can do it.
- `gated`: the row carries `GATED`. Its decision belongs to someone outside
  this session.
- `waits`: the row waits on a `person` or `gated` row, at any depth, or on an
  open checkpoint row. A row waits on the ids in a `[WAITS:T012]` tag, or in a
  clause that starts `Only if`, `Depends on` or `Blocked by`. It waits on an
  open checkpoint row whose `affects` names an id in one of its tags, such as
  `[C2]`.
- `runnable`: every other open row.

Implement is done when no `runnable` row is open and a pull request is
recorded. List the `person`, `gated` and `waits` rows in the report.

## Decisions

Every open decision a phase meets takes one of three routes. Ask none of them
in a subagent.

| Route | The decision | What happens |
|---|---|---|
| **Gate** | The constitution assigns it to someone outside this session | A checkpoint row with that owner as `audience` and the criterion ids it holds as `affects`. Never asked here. Only the tasks that name those ids wait. |
| **Owner** | It sets what the product promises a client or a user: scope, pricing, tiers, limits, who may use it, legal or contract terms, or data that is deleted or exposed. Or no option is a clear default. In plan only, also a new store, a new dependency or a new public contract. | A checkpoint row with audience `po`: `product` for what the feature does, from any phase; `design` for the shape of the build, from plan only. The phase drafts on the recommended answer. |
| **Routine** | Every other decision: how the work is done, a technical convention such as a header, a status code or a window type, a default with a clear runner-up | The phase takes the recommended answer. In `plan.md` it writes one row in the `## Approach` table: `\| D<n> \| <decision> \| <choice> \| <rejected> \| <why> \|`. In `spec.md` it writes the answer in place of the question, as core clarify does, with no `A<n>` line. A how-to decision met in specify or clarify waits for plan. Nobody reviews a routine decision. |

Write a gate or owner decision with `flow.mjs add`, by the Checkpoint rows
section of `.specify/extensions/grill/templates/grilling.md`. Never write a
`[NEEDS CLARIFICATION]` marker or an `A<n>` line.

## Phases

Every subagent follows the Subagent contract in
`.specify/extensions/batch/commands/speckit.batch.run.md`: a self-contained
prompt, a pinned feature, an explicit agent type, no question to the user, and the
fixed return shape. Pass the type this table names as `subagent_type`, with no
`model`: the type sets the model and the effort. Without the bizstream
plugin, pass the type's model as `model`: Opus for architect, Sonnet for scout. Give
each subagent the Decisions section above verbatim and `FEATURE_DIR`, and have
it return the ids of the rows it wrote and the count of Approach rows it
added.

`next` names a phase by the id in the second column. It skips a phase whose
artifact exists, the QA phases when the `qa` extension is absent, and merge
when `publish` is `ask`. Record merge as `skipped` when the card is
docs-only.

| # | Phase id | Phase | Agent type |
|---|---|---|---|
| 1 | `specify` | `speckit.specify` with the description | `bizstream:architect` |
| 2 | `clarify` | Clarify: the ambiguity scan the core `speckit.clarify` defines, with each decision routed above, never asked | `bizstream:architect` |
| 3 | `plan` | `speckit.plan` | `bizstream:architect` |
| 4 | `assumption-review` | Assumption review, for a spec with `A<n>` lines from before checkpoints | this session |
| 5 | `tasks` | `speckit.tasks` | `bizstream:architect` |
| 6 | `analyze` | `speckit.analyze`, read-only | `bizstream:architect` |
| 7 | `implement` | `speckit.card.implement` | this session |
| 8 | `qa-audit` | QA audit | `bizstream:architect` |
| 9 | `qa-walk` | `speckit.qa.walk` | `bizstream:scout` |
| 10 | `merge` | Merge on green | this session |

Phases 8 and 9 run after the code exists, so each check opens a page that is
real. Commit `qa-audit-results.md` and the wizard on the pull request's branch,
in one commit, and push it.

Phase 7 runs in this session. When it hands work to a builder, it gives one
`tasks.md` phase or one pull request per dispatch, as
`.specify/extensions/card/templates/subagents.md` sets it, and
starts each next builder fresh from the state note the last one wrote. When
`speckit.card.implement` opens the pull request, call `record` with
`implement done --pr <number>`.

After each subagent returns:

- New Approach rows: check each row the phase added to `plan.md` against the
  Owner row of the Decisions table. A row that sets a limit, a price, a tier,
  who may use the feature, or a contract term was never the phase's to take:
  remove it, and write it as a `product` row with the phase's choice as the
  recommended answer.
- New checkpoint rows: with a person present, hand the feature to
  `speckit.grill.run` in this session before `record`, so the answers are in
  the artifact when the phase is recorded. With no person present, leave them
  open.
- Call `record` with the phase id, the subagent's status and `--note <note>`.
- `done`: call `next`.
- `blocked` or `failed`: dispatch the phase one more time with the note in the
  prompt. A record that prints `"stop": true` is a stop.

### Assumption review

A spec written before checkpoints keeps its `A<n>` lines in `## Assumptions`.
`next` names this phase only while some of them are unreviewed. It moves the
ones that touch scope onto the product checkpoint.

1. **Triage.** Dispatch one `bizstream:architect` with every unreviewed
   `A<n>` line. It sorts each one into exactly one class, with a one-line
   reason, and edits nothing:
   - `scope`: the answer changes what the feature does for a user or a client:
     what is in or out, a behavior a user sees, data the feature keeps or
     shows, or an outside system it talks to.
   - `routine`: the answer changes only how the work is done.
   - `unsure`: neither class is clear.
2. **Rows.** Write each `scope` and `unsure` line as a `product` row: the
   `A<n>` id, audience `po`, the decision as the question, the answer taken as
   the recommended answer, its alternative, and phase `clarify`.
3. **Record.** Every `routine` line is accepted. End the `## Assumptions`
   section with one line:
   `Reviewed: <date>. Shown to the owner: <ids>. Accepted without review: <ids>.`
   A later review appends its own line.

`next` then holds on the rows. An answer that differs from the line sends
clarify back, then plan, by the loop above.

Phase 7 lists every assumption accepted without review in the pull request
body. After `speckit.card.implement` opens the pull request, read its body with
`gh pr view <number> --repo <owner/name> --json body`, append a section
`## Assumptions accepted without review` with each such `A<n>` line from
`spec.md`, verbatim, and write it back with `gh pr edit <number> --repo
<owner/name> --body-file <file>`.

**Done when:** every unreviewed assumption has a class, every `scope` and
`unsure` line is a product row, and the `Reviewed:` line names every id.

### Analyze

Analyze runs once and edits nothing. It checks the plan against the spec and
the constitution, so it catches a plan that breaks a rule before any code
exists.

1. Write each routine decision the subagent returns into the Approach table of
   `plan.md` yourself, and each gate or owner decision as a checkpoint row.
2. A checkpoint row does not hold the fixes. Dispatch the redos below first,
   then, with a person present, put the rows through `speckit.grill.run` while
   they run.
3. Send each CRITICAL finding to the phase that owns its artifact. For
   `plan.md`, dispatch plan one more time with the findings in the prompt. The
   plan subagent checks each finding against its evidence first, fixes each
   real one, and returns each one that is not real with the reason. For
   `spec.md`, dispatch phase 2 one more time with the findings in the prompt,
   so it routes each one by the Decisions table, then plan. When plan changed, or a CRITICAL
   finding is on `tasks.md`, dispatch tasks one more time with the change in
   the prompt, after the last plan redo.
4. A redo writes its new decisions as rows, so `next` holds on them before
   phase 7, and an answer that differs from the draft sends the phase back.

Do not run analyze again: the gates and the review of `speckit.implement`
catch what a redo misses, against real code. Record HIGH, MEDIUM and LOW
findings for the report.

**Done when:** every CRITICAL finding went to its owning phase and was fixed
or returned as not real with its reason, `tasks.md` follows the last
`plan.md`, and the rest are recorded.

### QA audit

The audit subagent asks the owner nothing, so its brief answers the
host and accounts round of `speckit.qa.audit` (step 5 of the
`bizstream:qa-audit` skill it runs) in advance:

- The base URL is the one step 3 recommends.
- The host is not a confirmed test environment.
- No credentials. A criterion that needs an account is `not-checked` with the
  reason `no credentials`.

Before step 1, the subagent runs
`curl -s -o /dev/null -w '%{http_code}' --max-time 10 <base URL>`. Any HTTP
status means the app answers. `000`, or no base URL found, means it does not:
the subagent runs no audit and returns `skipped` with the reason, such as `no
answer at http://localhost:5000` or `no base URL in the project config`. A
stop in the audit's preflight, such as no browser tool, is also `skipped`
with its reason. A skip is not a stop: phase 9 runs, with every criterion as
a stage. Record the reason for the report.

**Done when:** `qa-audit-results.md` exists, or the phase returned `skipped`
with its reason.

### Merge on green

With `publish: auto`, the owner has confirmed the close-out ahead of time.
Run `speckit.card.implement` without its `Publish the close-out` question:
start its Phase 3 block at step 1. A failed acceptance verdict still asks the
question. After phases 8 and 9 pushed their commit, or were skipped:

1. Run `gh pr checks <number> --repo <owner/name> --watch --fail-fast` in the
   background, and wait for it to exit.
2. When every check passed, and at least one check ran, run
   `gh pr merge <number> --repo <owner/name> --auto` with the first method
   that `gh repo view <owner/name> --json squashMergeAllowed,mergeCommitAllowed,rebaseMergeAllowed`
   allows, in that order: `--squash`, `--merge`, `--rebase`. GitHub merges
   the pull request when its other rules, such as a required review, are met.
3. A failed check, or no check at all, is a stop. Enable no auto-merge. Name
   each failing check in the report.

**Done when:** auto-merge is enabled on a pull request whose checks all
passed, or the stop is reported.

## No person present

`next` prints `"headless": true` when `SPECKIT_HEADLESS` is `1`. Then nobody
can answer:

- Never ask the user, never call `AskUserQuestion`, never run
  `speckit.grill.run`.
- Each phase writes its rows and drafts on the recommended answers.
- When `next` prints a checkpoint hold, end the run with `"stop":"owner"`, the
  checkpoint and its open ids. Whoever runs the flow collects the answers with
  `flow.mjs answer` and starts it again.
- Never call `release`. A held `cause` is the owner's.

## Stops

Stop and put it to the owner for:

- A checkpoint hold: through `speckit.grill.run` with a person present, or the
  end of the run with none.
- A hard stop in `speckit.card.implement`.
- `next` prints `held` with a `cause`, or a `record` prints `"stop": true`: a
  second `blocked` or `failed` from one phase.
- The close-out confirmation in `speckit.card.implement`, when `publish` is
  `ask`.
- A failed check or no check in phase 10.

Everything else is routine: take it without asking.

## Report

Close with one table: each phase, its agent type, its result, and the rows
answered in it. Add the count of rows written per checkpoint and how many the
answers changed, the open rows by audience, the Approach row count, the analyze
findings you recorded, the QA audit skip reason when phase 8 skipped, every
GATED marker still open with its owner, and the pull request URL with its
auto-merge state, or the stop that came before it. Restore
`.specify/feature.json` after the report.

## Done when

- [ ] Every phase ran in the order `next` named it, or was skipped by its rule, and each result is recorded with `record`
- [ ] Every subagent ran as the agent type the table names
- [ ] Every decision took one route: a gate row, an owner row, or an Approach row
- [ ] No phase wrote a `[NEEDS CLARIFICATION]` marker or an `A<n>` line, and no question was asked with no person present
- [ ] Every unreviewed `A<n>` line from before checkpoints was triaged before tasks ran, and each `scope` and `unsure` one is a product row
- [ ] The pull request body lists every assumption accepted without review
- [ ] Analyze ran once, and every CRITICAL finding went to its owning phase
- [ ] `speckit.card.implement` ended with the pull request open, or on a hard stop that is reported
- [ ] The QA audit wrote its results, or its skip reason is reported
- [ ] The QA audit results and the QA wizard are committed and pushed on the pull request's branch, or their skip rules held
- [ ] With `publish: auto`, auto-merge is enabled after every check passed, or the stop is reported
- [ ] `.specify/feature.json` is restored


## Recompose before publishing

> This section is authoritative for how the run reaches its pull request. Where anything above conflicts with it,
> this section wins.

- Phase 9 runs `speckit.card.implement` as this project wraps it. Its close-out recomposes the branch with the
  `recompose-branch` skill after the commit and before the push, and lands every review fix as one commit after the
  pull request opens.
- The recomposition runs in this session. No builder plans it, builds it, pushes the branch or opens the pull
  request.
- A confident grouping is built before the close-out confirmation, which shows the new history. A grouping that is a
  judgment call rides on the confirmation as a plan, and nothing is committed until the owner answers. The
  confirmation stays an owner stop under Stops, because it approves the publish. It is never routed as an
  assumption, never answered by a recommendation, and never skipped on a resumed run.
- The run never pushes a branch or opens a pull request until the recomposition has passed its tree check. A
  resumed run skips the recomposition only when the branch's pull request is already open.
- The Report adds the recomposed history and the review-feedback commit, and says that the backup branch was deleted
  once the pull request opened.
