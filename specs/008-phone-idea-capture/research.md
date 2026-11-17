# Research: Phone Idea Capture

Facts checked against the live board and repository on 2026-11-16 with read-only GraphQL queries as Th3FenrisWolf.

## R1. The board read hides archived cards

- **Finding**: `ProjectV2.items` takes `archivedStates`, default `[NOT_ARCHIVED]`. `readProject` in
  `tools/project-board/sync.mjs` passes none, so today the sync never sees an archived card. A Key'd card archived by
  hand would be created again on the next run. The board holds 0 archived items and 34 others today.
- **Decision**: `readProject` passes `archivedStates: [ARCHIVED, NOT_ARCHIVED]` and reads `isArchived` on each item.
  Archive and restore use `archiveProjectV2Item` and `unarchiveProjectV2Item`, both in the schema.
- **Alternatives**: a second query per idea through `Issue.projectItems(includeArchived: true)`. Rejected: one more
  call per issue, and the card path needs archived items too.

## R2. Finding the owner's issues

- **Finding**: `repository.issues(filterBy: {createdBy: "Th3FenrisWolf"}, states: [OPEN, CLOSED])` returns 35
  issues. 32 carry `<!-- sync -->` (the sync's own, including the duplicates #138 to #147 and #150, #151, #157). Three
  do not: #148, #153 and #154, all open, on the board with blank Status, Kind and Iteration. No other owner issue
  without the block exists, so the first run fills those three and adds nothing else.
- **Decision**: read every owner issue with `id`, `number`, `state` and `body`, paginated by 100, and join them to the
  board items by content id. The body is tested for the sync block and kept in memory for an off-board takeover,
  whose synced block is merged above the owner's text (FR-012, plan D22). The sync never prints it.
- **Alternatives**: the author of each board item. Rejected: it misses open ideas not yet on the board.

## R3. The issue events the sync starts

- **Finding**: an event made with a personal access token starts workflows; only `GITHUB_TOKEN` events do not. The sync
  writes with `PROJECT_BOARD_TOKEN`, a classic token of Th3FenrisWolf, so every issue it opens, closes or reopens
  fires an `issues` event with the owner as author. Issue events always run the workflow file on the default branch.
- **Decision**: the job runs on an `issues` event only when `github.event.issue.user.login` equals
  `github.repository_owner` and `github.event.issue.body` does not contain `<!-- sync -->`. `contains` on an empty
  body is false, so a title-only idea passes. The test lives in workflow expressions, never in a `run` step (FR-008).

## R4. Concurrency and filtered events

- **Finding**: the workflow-level `concurrency` group is assigned to a run before any job's `if` is evaluated. With
  one fixed group and `cancel-in-progress: false`, a newly queued run replaces the run already pending in the group.
  A filtered issue event that took the fixed group would therefore replace a waiting board update, then skip its own
  job (FR-007).
- **Decision**: the group is an expression. A filtered issue event gets `project-board-skip-<run_id>`, unique to it;
  every other event gets `project-board`. The job's `if` repeats the same filter, so the filtered run does nothing.
- **Alternatives**: job-level concurrency, relying on a skipped job never entering its group. Rejected: that ordering
  is not documented, and the workflow-level group keeps today's semantics for every other event.

## R5. The board's built-in workflows

- **Finding**: the board has four built-in workflows, all enabled: Auto-add sub-issues to project, Auto-close issue,
  Item closed and Pull request merged. "Item closed" sets Status Done when an issue closes: the sync-marked duplicates
  #138 to #146 sit at Done with blank Kind and Iteration. With it on, a closed idea is set to Done before the sync
  archives it, and comes back at Done when reopened, which breaks US2 scenario 2 and SC-006 for Status. The GraphQL
  schema offers no mutation to switch a built-in workflow on or off.
- **Decision**: open question Q1 for the owner (checkpoint `product`). The plan drafts on turning "Item closed" off.
  The sync already sets Done and closes the issue for every Key'd card, so turning it off changes only issues the sync
  does not own. "Auto-close issue" stays on: an idea dragged to Done closes its issue, and the sync then archives it.

## R6. Printing issue text in a workflow log

- **Finding**: a line on stdout that starts with `::` is a workflow command to the runner. An issue title printed by
  the sync could carry one, and anyone can open an issue in this public repository.
- **Decision**: the sync names an idea by its issue number only, in dry runs, warnings and errors. Card keys come from
  the repository and print as today.

## R7. Reading an idea from a command

- **Finding**: the board is private (`public: false`). An account other than the owner sees no board items, so a Key
  check run as twinright-bzs would find none and pass an issue it should refuse.
- **Decision**: `tools/project-board/idea.mjs` first reads the signed-in login (`viewer`) and stops with an error
  unless it is the board owner. It then reads the issue's author, and its Key on this board, without the body. Only
  for the owner's issue with no Key does it read the title and body, and it refuses one whose body carries the sync
  block (FR-011). Both queries are exports of `derive.mjs`, and a test checks that the first names no title or body
  field (plan D21); the viewer rule is `ideaViewerError` there too (plan D23).

## R8. Labels on a card's issue

- **Finding**: checked read-only on 2026-11-16. `Issue.labels` is readable through a board item's `content`, so the
  board read reaches every card issue's labels with no extra call. `RemoveLabelsFromLabelableInput` takes
  `labelableId` and `labelIds`, and `CreateLabelInput` exists in the schema. The repository had no `building` label
  then (the owner made it later that day, T061), and no open card issue carries any label (#149 `008-phone-idea-capture`, #152 `009-script-hydration-cost`,
  #156 `010-recipe-details-step`), so the first run after the merge changes no card through it. `gh issue edit
  --add-label` adds labels by name, so the label must exist first.
- **Decision**: the board read adds `labels(first: 20) { nodes { id name } }` to its Issue fragment (plan D25); the
  sync removes the label with `removeLabelsFromLabelable` (plan D27); the owner makes the label once (plan D32).

## R9. Label events

- **Finding**: an `issues` event of type `labeled` or `unlabeled` carries the label in `github.event.label`. Every
  card issue carries `<!-- sync -->`, so today's job filter (research R3) skips every label event on a card issue, and
  a label added at pickup would wait for the next push or the 06:17 run.
- **Decision**: the workflow adds `labeled` and `unlabeled`, and runs them only for the `building` label on an issue
  the owner opened, with the same split concurrency group as research R4 (plan D30).

## Not verified

- Whether a field can be written on an archived item. The plan avoids the case: a restore unarchives before it fills,
  and a Key'd archived card that is not a takeover is left untouched (plan D4).
- The two-minute bound of SC-001 and SC-006 depends on Actions queue time, which only a live run measures.
- That `gh issue edit --add-label` on an issue that already carries the label succeeds without a change, which a
  resumed card implement relies on. The wrap treats a failure as a report, not a stop, either way.
- `github.event.label.name` on a live `labeled` event; the after-merge check of [quickstart.md](quickstart.md) covers
  it.
