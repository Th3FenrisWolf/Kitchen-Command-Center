---

description: "Task list for Phone Idea Capture"
---

# Tasks: Phone Idea Capture

**Input**: Design documents from `specs/008-phone-idea-capture/`

**Prerequisites**: [plan.md](plan.md) (Approach D1 to D33, Q1 to Q4, Complexity Tracking), [spec.md](spec.md),
[research.md](research.md), [data-model.md](data-model.md), [contracts/idea-link.md](contracts/idea-link.md),
[contracts/building-label.md](contracts/building-label.md), [quickstart.md](quickstart.md)

**Tests**: Required. The constitution's principle IV puts a failing test before every behavior change. Every test row
below is written first, run, and seen to fail before the row that implements it. The one exception is the GraphQL calls
in `sync.mjs`, `idea.mjs` and `pickup.mjs`, which get no unit test: plan.md's Constitution Check row IV names that
break and its Complexity Tracking justifies it. Every rule those calls apply is a tested `derive.mjs` function, and the
calls themselves are checked by the dry runs and the read-only `idea.mjs` and `pickup.mjs` reads of
[quickstart.md](quickstart.md).

**Organization**: One `tasks.md` phase per builder. A user story that needs both the pure derive layer and the
GitHub writes is split into two phases.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an open row)
- **[Story]**: US1 to US5, from [spec.md](spec.md)
- **[PERSON]**: Only a person can do it. **[WAITS:Tnnn]**: waits on that row

## Shared rules for every row

- The test file is `tests/KCC.ViteTests/Tools/projectBoard.test.ts`. It imports `tools/project-board/derive.mjs` only,
  so every rule a test checks lives in `derive.mjs` as a pure function (plan D1). Run it from `src/KCC.Web` with
  `pnpm vitest run projectBoard`.
- No issue title or body ever goes into a command line, a workflow `run` step or a printed line (FR-008, research R6).
  An idea prints by its issue number only. GraphQL variables go through `graphql()`'s JSON stdin, as today.
- Nothing in this file pushes, opens a pull request or writes the live board. `sync.mjs` runs here only with
  `--dry-run`, after `git fetch --prune`, signed in to `gh` as Th3FenrisWolf. The close-out publishes.
- A row marked "no unit test (Complexity Tracking)" adds a GraphQL call under the justified break of plan.md's
  Constitution Check row IV. It moves no rule out of `derive.mjs`: a rule the call needs gets a failing test and a
  derive function first.
- Comments follow principle VII: no comment that explains what code does.

---

## Phase 1: Setup

**Purpose**: Record the baseline the later dry runs are compared against.

- [X] T001 Confirm `gh auth status` shows Th3FenrisWolf active (`gh auth switch -u Th3FenrisWolf` if not), confirm
  `pnpm vitest run projectBoard` passes from `src/KCC.Web`, then run `git fetch --prune` and
  `node tools/project-board/sync.mjs --dry-run` with today's `tools/project-board/sync.mjs` and save its output to a
  file outside the repository, for T017's comparison

---

## Phase 2: Foundational (the Idea line and archived items)

**Purpose**: Every story needs the board read to see archived items, and US3 and US4 need cards that carry the idea
they name.

- [X] T002 Add failing tests for `parseIdeaLine(markdown)` in `tests/KCC.ViteTests/Tools/projectBoard.test.ts`, per
  [contracts/idea-link.md](contracts/idea-link.md): it returns the number from the first line matching
  `^(?:- )?\*\*Idea\*\*:\s*\[?#(\d+)` (multiline), for the spec form
  `**Idea**: [#148](https://github.com/Th3FenrisWolf/Kitchen-Command-Center/issues/148)`, the assessment form
  `- **Idea**: [#150](…)` and a bare `**Idea**: #148`; ignores anything after the number; takes the first of two lines;
  returns `null` when no line matches, and for `**Idea**:` inside running text
- [X] T003 Add failing tests in `tests/KCC.ViteTests/Tools/projectBoard.test.ts` that `deriveCards` sets `idea` on a
  spec card from the snapshot `specCard` picks (the branch's spec while unmerged, `main`'s once merged; use
  `specSnapshot`'s `extra`), and on a bug card from the assessment `bugCard` picks (give `bugSnapshot` an `extra`
  option appended to the assessment); and that a card with no Idea line has no `idea` property, so the existing
  `toEqual` expectations keep their shape
- [X] T004 Add failing tests in `tests/KCC.ViteTests/Tools/projectBoard.test.ts` for a Key'd item with
  `archived: true` (plan D4): `planChanges` matches it by its Key or alias and plans neither a `create` nor an
  `update` for it, even when its title, body or fields differ from the card
- [X] T005 Add failing tests in `tests/KCC.ViteTests/Tools/projectBoard.test.ts` for several items that carry one Key
  (plan D19, data-model's Board item): with an archived item first in read order and an unarchived one after it,
  `planChanges` plans its `update` against the unarchived item's id and no op for the archived one; with two
  unarchived items, the first in read order; with only archived items, the first archived one, so no `create` and no
  `update` (T004's rule); the same holds when the card is matched by an alias
- [X] T006 Implement `parseIdeaLine` as an export of `tools/project-board/derive.mjs`, and set `idea` on spec and bug
  cards from the picked snapshot only when the line exists, so T002 and T003 pass (depends on T002, T003)
- [X] T007 In `tools/project-board/derive.mjs`, make `planChanges`' `byKey` keep the first unarchived item for each Key,
  else the first archived one (plan D19), in one match function that `claimIdeas` reuses in T026, and skip an archived
  Key'd item after matching it (plan D4), so T004 and T005 pass and no other test changes (depends on T004, T005)
- [X] T008 [P] In `readProject` of `tools/project-board/sync.mjs`, pass `archivedStates: [ARCHIVED, NOT_ARCHIVED]` to
  `items` and read `isArchived` into each item as `archived` (plan D3, research R1). No unit test (Complexity
  Tracking); T017's dry run checks it

**Checkpoint**: archived cards are visible to the sync, and every spec and bug card knows the idea it names.

---

## Phase 3: User Story 1, derive (Priority: P1) MVP

**Goal**: The pure rules that turn an owner's issue into an Idea card and fill only its blanks.

**Independent test**: the Vitest cases below pass with no network.

- [X] T009 [US1] Add failing tests for `ownerIdeas(issues, items)` in
  `tests/KCC.ViteTests/Tools/projectBoard.test.ts`. An owner issue is `{ id, number, state, synced, body }` per
  [data-model.md](data-model.md), where `synced` means the body contains `<!-- sync -->`. It returns each issue that is
  not `synced` and whose item, joined by `item.contentId === issue.id`, carries no `Key`, with that item or `null`.
  Cover: an issue off the board; one on the board with blank fields; a `synced` one (none); one whose item has a Key
  (none, FR-001 and edge case "an issue on the board that carries a Key is the sync's"); and an item with blank fields
  whose content is not among the owner's issues, which never appears (FR-006)
- [X] T010 [US1] Add failing tests for `planIdeas(ideas, claimed = new Set())` in
  `tests/KCC.ViteTests/Tools/projectBoard.test.ts`, for the open rows of the data-model's Owner idea table: open and
  off the board gives `{ op: "idea", issue: <number>, contentId, add: true, fields: { Status: "Idea", Kind: "Idea",
  Iteration: "Current" } }`; open on the board with all three blank (as #148 is) gives `fields` with all three and the
  item's id; a field counts as blank when it is missing from the item or empty (plan D16); an item set to
  `Iteration: Future` and `Kind: Bug` gets only `Status: Idea` (US1 scenario 3); all three set gives no op; a number
  in `claimed` gives no op; the op never carries a title, body, label or state (FR-004)
- [X] T011 [US1] Add failing tests in `tests/KCC.ViteTests/Tools/projectBoard.test.ts` for `describeOp(op)`, an export
  of `tools/project-board/derive.mjs`: today's `create` and `update` lines stay exactly as `describe` in `sync.mjs`
  prints them; an add prints `idea #148: add, Status=Idea, Kind=Idea, Iteration=Current`; a fill prints
  `idea #148: fill, Status=Idea, Kind=Idea, Iteration=Current` with only the fields it sets; and an op built from an
  issue whose title and body are `::error::hijack` prints neither (plan D11, FR-008)
- [X] T012 [US1] Add failing tests in `tests/KCC.ViteTests/Tools/projectBoard.test.ts` that read
  `.github/workflows/project-board.yml` as text (plan D15, D9, D10): `on` has `issues` with
  `types: [opened, reopened, closed]`; the job's `if` and the workflow `concurrency.group` both compare
  `github.event.issue.user.login` with `github.repository_owner` and both test `contains(github.event.issue.body,
  '<!-- sync -->')`; the group names `project-board-skip-` with `github.run_id` for a filtered issue event and
  `project-board` otherwise; `cancel-in-progress` stays `false`; no `run:` line mentions `github.event`; and the job's
  repository guard equals `` `${config.owner}/${config.repo}` `` from `board.json`
- [X] T013 [US1] Implement `ownerIdeas`, the add and fill parts of `planIdeas`, and `describeOp` in
  `tools/project-board/derive.mjs`; move `describe` out of `tools/project-board/sync.mjs` into `describeOp` and import
  it there, so T009, T010 and T011 pass and every existing test still does (depends on T009, T010, T011)

**Checkpoint**: the sync's idea rules for an open issue are tested. T012 stays red until T016.

---

## Phase 4: User Story 1, sync and workflow (Priority: P1) MVP

**Goal**: A title-only issue from the owner's phone reaches the Idea column on the next run, and an issue event starts
that run.

**Independent test**: the dry run lists `idea #148`, `idea #153` and `idea #154` as fills, and T012 passes.

- [X] T014 [US1] Add `readOwnerIssues()` to `tools/project-board/sync.mjs` (plan D2, research R2):
  `repository(owner, name) { issues(first: 100, after, filterBy: { createdBy: <board.json owner> }, states: [OPEN,
  CLOSED]) { nodes { id number state body } pageInfo { hasNextPage endCursor } } }`, paginated, mapped to
  `{ id, number, state, synced, body }`. Per [data-model.md](data-model.md)'s Owner issue, `body` stays in memory for
  an off-board takeover's body merge (T025, T027) and is never printed. No unit test (Complexity Tracking); T017's dry
  run checks it (depends on T013)
- [X] T015 [US1] In `tools/project-board/sync.mjs`, compute `planIdeas(ownerIdeas(readOwnerIssues(), project.items))`
  and apply its ops after the card ops and the parent links (plan D17): an add calls `addProjectV2ItemById` for the
  issue's `contentId`, then writes each field with `setField`; a fill writes each blank field with its own `setField`
  (plan D16). Every idea op prints through `describeOp`, with the `[dry run] ` prefix in a dry run (FR-016), and the
  closing count line includes the idea ops. An add's error names the issue number only. No unit test for the writes
  (Complexity Tracking) (depends on T014)
- [X] T016 [US1] Change `.github/workflows/project-board.yml` (plan D9, D10, research R3, R4): add `issues: types:
  [opened, reopened, closed]`; set `concurrency.group` to `${{ github.event_name == 'issues' &&
  (github.event.issue.user.login != github.repository_owner || contains(github.event.issue.body, '<!-- sync -->')) &&
  format('project-board-skip-{0}', github.run_id) || 'project-board' }}`; extend the job's `if` to
  `github.repository == 'Th3FenrisWolf/Kitchen-Command-Center' && (github.event_name != 'issues' ||
  (github.event.issue.user.login == github.repository_owner && !contains(github.event.issue.body, '<!-- sync -->')))`;
  correct the concurrency comment so it says why a filtered issue event takes a group of its own. No `run` step reads
  an issue field. T012 passes (depends on T012)
- [X] T017 [US1] Run `pnpm vitest run projectBoard` from `src/KCC.Web` (all green), then `git fetch --prune` and
  `node tools/project-board/sync.mjs --dry-run`. Expect, per [quickstart.md](quickstart.md) step 3: `idea #148`,
  `idea #153` and `idea #154` each fill `Status=Idea, Kind=Idea, Iteration=Current`; no `idea` line for an issue that
  carries the sync block; no issue title anywhere; every other line matches T001's saved output. Read-only (depends on
  T015, T016)

**Checkpoint**: US1 works in a dry run. The live check (SC-001, SC-002) runs after the merge.

---

## Phase 5: User Story 2, drop and bring back (Priority: P2)

**Goal**: A closed idea's card is archived, and a reopened or hand-archived open idea comes back with its fields.

**Independent test**: the Vitest cases below pass, and the dry run lists any archive or restore by number only.

- [X] T018 [US2] Add failing tests in `tests/KCC.ViteTests/Tools/projectBoard.test.ts` for the rest of the
  data-model's Owner idea table: closed with an unarchived item gives `{ op: "idea", issue, archive: true }` and
  nothing else (US2 scenario 1, FR-005); open with an archived item gives `restore: true` plus only its blank fields,
  or `restore` alone when all three are set (US2 scenarios 2 and 3, FR-002); closed and off the board, or closed and
  archived, gives no op (US2 scenario 4, FR-005); no op changes the issue's state (FR-004). `describeOp` prints
  `idea #160: archive`, `idea #160: restore` and `idea #160: restore, Status=Idea` for a restore that fills
- [X] T019 [US2] Extend `planIdeas` in `tools/project-board/derive.mjs` with archive and restore so T018 passes
  (depends on T018)
- [X] T020 [US2] In `tools/project-board/sync.mjs`, apply `restore` with `unarchiveProjectV2Item(input: { projectId,
  itemId })` before any field write of the same op, and `archive` with `archiveProjectV2Item(input: { projectId,
  itemId })`. Neither touches the issue. No unit test (Complexity Tracking) (depends on T019)
- [X] T021 [US2] Run `pnpm vitest run projectBoard` (all green) and the dry run again: any `archive` or `restore` line
  names an owner issue that is closed on the board or open and archived, by number only, and the lines T017 checked
  are unchanged. Read-only (depends on T020)

**Checkpoint**: US1 and US2 hold in a dry run.

---

## Phase 6: User Story 3, takeover in the sync (Priority: P2)

**Goal**: A spec that names an idea turns the idea's card into its own card, and the board never shows two.

**Independent test**: the Vitest cases below pass.

- [X] T022 [US3] Add failing tests for `claimIdeas(cards, ideas, items)` in
  `tests/KCC.ViteTests/Tools/projectBoard.test.ts`, per the data-model's Claim rules and FR-014 (plan D7): it returns
  `{ claims, warnings }`, where `claims` maps a card key to `{ issue, item }` (`item` is `null` off the board). A card
  already matched by its Key or an alias claims nothing (its warnings are T023's); a card naming an issue that is not
  an owner idea (another author, `synced`, a Key'd item, no such issue) claims nothing and warns; among several cards
  naming one idea the key that sorts first with `localeCompare` claims it, and each other warns naming the winner and
  the issue. Every warning names keys and `#N` only, never a title
- [X] T023 [US3] Add failing tests in `tests/KCC.ViteTests/Tools/projectBoard.test.ts` for claim rule 1's warning
  (plan D18, data-model's Claim): a card matched by its Key whose item's `contentId` is the `id` of the issue its Idea
  line names, as after its own takeover, gives no warning; a card matched by its Key to an item whose `contentId` is
  another issue's gives one warning naming the card's key and `#N`; the same two cases for a card matched by an alias;
  a matched card naming a number missing from the owner-issue read warns; and the match uses T007's preference, so an
  archived duplicate first in read order does not make a card that took its idea over warn
- [X] T024 [US3] Add failing tests in `tests/KCC.ViteTests/Tools/projectBoard.test.ts` for
  `planChanges(cards, items, claims)` with a claimed idea, per the data-model's Card takeover table (plan D6): on the
  board, an `update` with the card's Key and fields, its title, and a body whose synced block sits above the owner's
  text (US3 scenario 2, FR-012); archived, the same with `unarchive: true` (FR-013), and with `state: "OPEN"` when the
  issue is closed and the card is not Done (US3 scenario 3); in every case no `create` op for that card.
  `planChanges(cards, items)` with no claims still matches every existing test. `planIdeas` gives no op for a claimed
  idea, and `describeOp` ends a takeover's update line with `takes over #148`. The off-board case is T025's
- [X] T025 [US3] Add failing tests in `tests/KCC.ViteTests/Tools/projectBoard.test.ts` for an off-board takeover (plan
  D22, data-model's adopt item): `claimIdeas` gives the claim the item `{ type: "Issue", contentId: <issue id>, state:
  <issue state>, body: <owner's body>, fields: {} }` with no `id`, `title` or `parentId`, and `planChanges` plans one
  `update` with `adopt: true`, that `contentId`, the card's title, `Iteration: "Current"` among its fields, and a body
  whose synced block sits above the owner's text; a closed idea with a card that is not Done gets `state: "OPEN"`; a
  closed idea with a Done card gets no `state`; an open idea with a card that is not Done gets no `state`; a title-only
  idea (empty body) gets the synced block alone; no `create` op
- [X] T026 [US3] Implement `claimIdeas` in `tools/project-board/derive.mjs`, matching cards with T007's match function
  and warning only when the matched item's content is not the named issue (plan D18); build the adopt item of plan D22
  for an off-board claim; add the `claims` argument of `planChanges` (default empty) and the takeover fields
  (`unarchive`, `adopt`, `Iteration: "Current"` on adopt), so T022 to T025 pass (depends on T022, T023, T024, T025)
- [X] T027 [US3] Wire the takeover into `tools/project-board/sync.mjs`: claim after `deriveCards` and `ownerIdeas`,
  pass the claimed numbers to `planIdeas`, and print each warning with the `warning: ` prefix. In `apply`, an
  `unarchive` op calls `unarchiveProjectV2Item` before any other write; an `adopt` op calls `addProjectV2ItemById` for
  `contentId`, takes the returned item id, then writes as an update does, its state included when the op carries one.
  Add each takeover's key and `contentId` to `issueIds` so its parent link and its children's links resolve. No unit
  test for the calls (Complexity Tracking) (depends on T026)
- [X] T028 [US3] Run `pnpm vitest run projectBoard` (all green) and the dry run: no spec names an idea yet, so its
  output matches T021's. Read-only (depends on T027)

**Checkpoint**: the sync takes an idea over; the command that writes the Idea line comes next.

---

## Phase 7: User Story 3, the command (Priority: P2)

**Goal**: `/speckit-specify #NNN` reads the owner's idea and writes the Idea line.

**Independent test**: `idea.mjs` answers per the contract, and T035's test passes on the regenerated skill.

- [X] T029 [US3] Add failing tests in `tests/KCC.ViteTests/Tools/projectBoard.test.ts` for `ideaArgumentError(arg)`
  and `ideaRefusal(number, issue)`, exports of `tools/project-board/derive.mjs` (plan D12, research R7, FR-011).
  `ideaArgumentError` returns the stderr message `<arg> is not an issue number` for an argument that does not match
  the contract's `^\d+$` (`abc`, `#148`, `-1`, `12a`, an empty string), `no issue number given` for a missing
  argument, and `null` for `148`; idea.mjs
  prints it on stderr and exits non-zero before any read, since the contract's refusals are for issues only.
  `ideaRefusal` checks in this order and returns the contract's text exactly: `#N does not exist in this repository`
  for `null`; `#N was opened by another account` when `issue.author` is not `config.owner`; `#N already carries the
  Key <key>` when `issue.key` is set; `#N is a board card the sync made` when `issue.synced`; otherwise `null`
- [X] T030 [US3] Add failing tests in `tests/KCC.ViteTests/Tools/projectBoard.test.ts` for `ideaViewerError(login)`,
  an export of `tools/project-board/derive.mjs` (plan D23): it returns a message naming the signed-in account for
  `twinright-bzs` or any login that is not `config.owner`, and `null` for `config.owner`
- [X] T031 [US3] Add failing tests in `tests/KCC.ViteTests/Tools/projectBoard.test.ts` for idea.mjs's two GraphQL
  queries, exported from `tools/project-board/derive.mjs` as `IDEA_GATE_QUERY` and `IDEA_TEXT_QUERY` (plan D21,
  research R7): `IDEA_GATE_QUERY` names `author` and `projectItems(includeArchived: true)` and no `title` or `body`
  field (checked as whole words in the query text); `IDEA_TEXT_QUERY` names `title`, `body` and `url`
- [X] T032 [US3] Implement `ideaArgumentError`, `ideaRefusal`, `ideaViewerError`, `IDEA_GATE_QUERY` and
  `IDEA_TEXT_QUERY` in `tools/project-board/derive.mjs` so T029, T030 and T031 pass (depends on T029, T030, T031)
- [X] T033 [US3] Write `tools/project-board/idea.mjs`, per [contracts/idea-link.md](contracts/idea-link.md). It imports
  `ideaArgumentError`, `ideaViewerError`, `ideaRefusal`, `IDEA_GATE_QUERY` and `IDEA_TEXT_QUERY` from `derive.mjs` and
  runs in this order: the argument check; the viewer check (read `viewer { login }`, exit non-zero with
  `ideaViewerError`'s message on stderr when it returns one), before any issue read; `IDEA_GATE_QUERY` for the issue's
  `author { login }` and its Key on this board (`projectItems(includeArchived: true)`, matched to the board's project
  id), treating a "could not resolve to an Issue" error (a pull request number) as no issue; `ideaRefusal`, printing
  `{ number, refused }` on a refusal; then, only for the owner's Key-less issue, `IDEA_TEXT_QUERY` for `title`, `body`
  and `url`, and `ideaRefusal` again with `synced` from that body, so a body carrying `<!-- sync -->` is refused;
  otherwise it prints `{ number, url, title, body }`. Exit 0 for an answer or a refusal, non-zero with stderr for a
  failed check or read. Every `gh` call uses `execFileSync` with the query on stdin. No unit test for the reads
  (Complexity Tracking); T034 checks them (depends on T032)
- [X] T034 [US3] Check `tools/project-board/idea.mjs` against live data, read-only, per
  [quickstart.md](quickstart.md) step 4: as Th3FenrisWolf, `node tools/project-board/idea.mjs 148` prints its number,
  URL, title and body; `149` refuses with the Key `008-phone-idea-capture`; `155` refuses as not existing (it is a pull
  request); `abc` exits non-zero with `ideaArgumentError`'s message. Then `gh auth switch -u twinright-bzs`,
  `idea.mjs 148` exits non-zero with an error naming that account, and `gh auth switch -u Th3FenrisWolf` (depends on
  T033)
- [X] T035 [US3] Add a failing test in `tests/KCC.ViteTests/Tools/projectBoard.test.ts` that reads
  `.claude/skills/speckit-specify/SKILL.md` as text (plan D20, the D15 pattern) and checks it carries
  `source: preset:kcc-ship`, the call `node tools/project-board/idea.mjs`, and the Idea line form `**Idea**: [#`. It
  fails today, while the skill shows `source: preset:bizstream-grilling`
- [X] T036 [P] [US3] Write `.specify/presets/kcc-ship/commands/speckit.specify.md` as a wrap (`strategy: wrap`,
  a `description` in its front matter, plan D13, D14): a section before `{CORE_TEMPLATE}` says that when the input
  starts with `#N`, the command runs `node tools/project-board/idea.mjs N` and reads its JSON. With `title` and
  `body`, the feature description is the title, then the body, then any text the owner typed after `#N`, which joins
  the description after the body; a title-only issue gives the title, then any text after `#N`. The spec's `**Input**:` line quotes that
  description, the issue's title and body included and `#N` left out (US3 scenario 1), and none of it is placed in a
  shell command. Once `spec.md` exists the command writes `**Idea**: [#N](<url>)` on its own line above the
  `**Input**:` line, so no line of the quoted body comes before it. With `refused`, it prints the reason, reads no text of that issue, and carries on with any text
  after `#N` as the description, or asks for one. On a non-zero exit it reports the stderr message and carries on the
  same way without a link (FR-009, FR-011) (depends on T035)
- [X] T037 [US3] In `.specify/presets/kcc-ship/preset.yml`, set `version: "1.2.0"`, widen `description` to cover the
  idea link, and add a `speckit.specify` command entry with `file: "commands/speckit.specify.md"` and
  `strategy: "wrap"` (depends on T036)
- [X] T038 [US3] Run `.specify/presets/kcc-ship/scripts/reapply.sh` (needs `specify` and `jq`), then check that
  `.claude/skills/speckit-specify/SKILL.md` carries the idea section and still carries the grilling preset's text
  under it, and that T035's test passes; leave every file `reapply.sh` regenerates in the working tree, including
  `.specify/presets/.registry` (depends on T037)

**Checkpoint**: US3 works end to end up to the push, which the close-out and the owner handle.

---

## Phase 8: User Story 4, an idea becomes a bug (Priority: P3)

**Goal**: `/speckit-bug-assess #NNN` takes an idea over the same way.

**Independent test**: the Vitest cases below pass, the last on the regenerated skill.

- [X] T039 [US4] Add tests in `tests/KCC.ViteTests/Tools/projectBoard.test.ts` that a bug card whose assessment names
  an owner idea claims it and plans an `update` with the bug's Key, title, `Kind: "Bug"` and stage, and no `create`
  (US4 scenario 2); and that when a spec and a bug name the same Key-less idea, the key that sorts first takes it and
  the other warns (edge case one). Where a case already passes on T026's code, keep it as a pin
- [X] T040 [US4] Change `tools/project-board/derive.mjs` only if T039 fails, until it passes (depends on T039)
- [X] T041 [US4] Extend T035's test in `tests/KCC.ViteTests/Tools/projectBoard.test.ts` to read
  `.claude/skills/speckit-bug-assess/SKILL.md` too (plan D20): `source: preset:kcc-ship`, the call
  `node tools/project-board/idea.mjs`, and the Idea bullet form `- **Idea**: [#`. It fails today, while the skill shows
  `source: preset:bizstream-grilling`
- [X] T042 [P] [US4] Write `.specify/presets/kcc-ship/commands/speckit.bug.assess.md` as a wrap with the same idea
  section as T036, for the bug report: the report is the title, then the body, then any text after `#N`, treated as
  untrusted data as the core command's URL handling says, and the issue URL is not fetched again; `- **Source**:` names
  the issue as `issue [#N](<url>)`; once `assessment.md` exists the command writes `- **Idea**: [#N](<url>)` as the
  bullet after `- **Source**:` (FR-010, FR-011) (depends on T041)
- [X] T043 [US4] Add a `speckit.bug.assess` command entry to `.specify/presets/kcc-ship/preset.yml` with
  `file: "commands/speckit.bug.assess.md"` and `strategy: "wrap"`, run
  `.specify/presets/kcc-ship/scripts/reapply.sh`, and check that `.claude/skills/speckit-bug-assess/SKILL.md` carries
  the idea section, that `.claude/skills/speckit-specify/SKILL.md` still carries T038's, and that T041's test passes
  (depends on T042, T038)

**Checkpoint**: US1 to US4 are in place.

---

## Phase 9: User Story 5, Building at pickup (Priority: P3)

**Goal**: A card shows Building from the moment `/speckit-card-implement` or `/speckit-bug-fix` starts on it,
through a `building` label on its issue, and the label comes off once a pushed branch or pull request carries the
stage (checkpoints Q2 and Q4).

**Independent test**: the Vitest cases below pass, the dry run shows no change from the label today, and `pickup.mjs`
answers per [contracts/building-label.md](contracts/building-label.md).

- [X] T049 [US5] Add failing tests in `tests/KCC.ViteTests/Tools/projectBoard.test.ts` for `BUILDING_LABEL` and
  `pickupStage(stage, item)`, exports of `tools/project-board/derive.mjs`, per data-model's Building at pickup table
  (plan D24, D26, FR-018): `BUILDING_LABEL` is `building`; `Specified` with an `Issue` item whose `labels` hold
  `{ id, name: "building" }` gives `{ stage: "Building" }` and no `unlabel`; `Specified` with no label, another label,
  or a label named `Building` gives `{ stage: "Specified" }`; `Building`, `In review` and `Done` with the label
  give the same stage and `unlabel` set to that label's `id`; the same three without it give no `unlabel`; a
  `DraftIssue` item, or an item with no `labels`, gives the stage unchanged; the issue's `state` does not change the
  answer
- [X] T050 [US5] Add failing tests in `tests/KCC.ViteTests/Tools/projectBoard.test.ts` for `planChanges` on labeled
  items (plan D26, D27, FR-018, FR-019): a Specified card whose issue item carries `building` and Status `Specified`
  gets an `update` with `fields: { Status: "Building" }` and no `unlabel`; the same item already at `Building` gets no
  op, so the rule is stable from run to run; a card derived `Building` from a ticked task, `In review` or `Done` whose
  item carries the label gets an `update` with `unlabel: <label id>` beside any other change it already had; an archived
  Key'd item with the label and a held card get no op; a claimed idea's item (T024) carrying the label is read the same
  way; `planIdeas` on an owner idea that carries the label gives the ops it gives today and none touches the label
  (FR-004); and `describeOp` ends an update line with `unlabel building` when the op carries `unlabel`, never printing a
  title
- [X] T051 [US5] Implement `BUILDING_LABEL` and `pickupStage` in `tools/project-board/derive.mjs`, run it in
  `planChanges` on every matched, unarchived card item before the fields are compared, carry its `unlabel` on the
  update op, and print it in `describeOp`, so T049 and T050 pass and every existing test still does (depends on T049,
  T050)
- [X] T052 [US5] In `tools/project-board/sync.mjs` (plan D25, D27, research R8): add `labels(first: 20) { nodes { id
  name } }` to `readProject`'s `... on Issue` fragment and map it to `item.labels`, `[]` for any other content; in
  `apply`, after the op's other writes, an op with `unlabel` calls `removeLabelsFromLabelable(input: { labelableId:
  <the issue's contentId>, labelIds: [<unlabel>] })`, its error naming the card's key only. No unit test (Complexity
  Tracking); T062's dry run checks it (depends on T051)
- [X] T053 [US5] Extend T012's test in `tests/KCC.ViteTests/Tools/projectBoard.test.ts` so it fails on today's
  `.github/workflows/project-board.yml` (plan D30, FR-021): the `issues` trigger is
  `types: [opened, reopened, closed, labeled, unlabeled]`; the job's `if` and the workflow `concurrency.group` both
  test `github.event.action` for `labeled` and `unlabeled` and compare `github.event.label.name` with a quoted literal
  equal to `BUILDING_LABEL`; both still carry the author and `<!-- sync -->` tests of T012; `cancel-in-progress` stays
  `false`; no `run:` line mentions `github.event`
- [X] T054 [US5] Change `.github/workflows/project-board.yml` (plan D30, research R9): `issues: types: [opened,
  reopened, closed, labeled, unlabeled]`; the job's `if` becomes `github.repository ==
  'Th3FenrisWolf/Kitchen-Command-Center' && (github.event_name != 'issues' || (github.event.issue.user.login ==
  github.repository_owner && ((github.event.action != 'labeled' && github.event.action != 'unlabeled' &&
  !contains(github.event.issue.body, '<!-- sync -->')) || ((github.event.action == 'labeled' || github.event.action ==
  'unlabeled') && github.event.label.name == 'building'))))`; `concurrency.group` takes `project-board-skip-<run_id>`
  for an issue event that fails the same test and `project-board` otherwise; correct the concurrency comment so it
  names label events too. No `run` step reads an issue or label field. T053 passes (depends on T053)
- [X] T055 [US5] Add failing tests in `tests/KCC.ViteTests/Tools/projectBoard.test.ts` for `pickupArgumentError(arg)`
  and `pickupTarget(key, items)`, exports of `tools/project-board/derive.mjs`, per
  [contracts/building-label.md](contracts/building-label.md) and data-model's Pickup target (plan D28):
  `pickupArgumentError` returns `no card key given` for a missing argument, `<arg> is not a card key` for `abc`, `#152`,
  `bug/`, `fix/x`, `bug/X y`, `009_script` and an empty string, and `null` for `009-script-hydration-cost` and
  `bug/imagesharp-3-2-0` (plan D33); `pickupTarget` returns `{ key, number, repository }` for an open issue item, with
  `repository` `<owner>/<repo>` from `board.json`, and the contract's refusal text exactly for no item, a draft, an
  archived item and a closed issue; with an archived and an unarchived item for one Key it answers from the unarchived
  one (plan D19)
- [X] T056 [US5] Implement `pickupArgumentError` and `pickupTarget` in `tools/project-board/derive.mjs`, matching with
  `indexByKey`, so T055 passes (depends on T055)
- [X] T057 [US5] Write `tools/project-board/pickup.mjs`, per [contracts/building-label.md](contracts/building-label.md).
  It imports `pickupArgumentError`, `ideaViewerError` and `pickupTarget` from `derive.mjs` and runs in this order: the
  argument check; the viewer check (`viewer { login }`, exit non-zero with `ideaViewerError`'s message on stderr when it
  returns one); the board's items, paginated, with `isArchived`, the `Key` field and `... on Issue { number state }`,
  archived ones included; then prints `pickupTarget`'s answer as JSON. It reads no title or body and writes nothing.
  Exit 0 for an answer or a refusal, non-zero with stderr for a failed check or read. Every `gh` call uses
  `execFileSync` with the query on stdin. No unit test for the reads (Complexity Tracking); T062 checks them (depends
  on T056)
- [X] T058 [US5] Add a failing test in `tests/KCC.ViteTests/Tools/projectBoard.test.ts` that reads
  `.claude/skills/speckit-card-implement/SKILL.md` as text (plan D29, the D20 pattern) and checks it carries
  `source: preset:kcc-ship`, a `## Building at pickup` heading before `## Close out through ship-branch`, the call
  `node tools/project-board/pickup.mjs`, and `gh issue edit` with `--add-label building`. It fails today, while the
  skill has no pickup section
- [X] T059 [P] [US5] Add a `## Building at pickup` section to
  `.specify/presets/kcc-ship/commands/speckit.card.implement.md`, after `{CORE_TEMPLATE}` and before
  `## Close out through ship-branch` (plan D29, FR-020): it adds one step after Phase 1 is done and before Phase 2
  starts. The command runs `node tools/project-board/pickup.mjs <FEATURE_DIR's folder name>` and reads its JSON. With
  `number`, it runs `gh issue edit <number> --repo <repository> --add-label building`, putting nothing else from the
  JSON in the command. With `refused`, a non-zero exit or a failed edit, it reports the reason in one line, naming
  `gh label create building` when the label is missing, and carries on to Phase 2. It never pushes the branch for the
  label, runs on a resumed run and on a docs-only card too, and is never a stop. The section names the board stage it
  shows and that the sync takes the label off (depends on T058)
- [X] T060 [US5] In `.specify/presets/kcc-ship/preset.yml`, set `version` from 1.3.0 to `"1.4.0"`, once for both pickup
  sections, and widen the `description` and the `speckit.card.implement` entry's `description` to name the pickup label
  (plan D31); run `.specify/presets/kcc-ship/scripts/reapply.sh`, then check that
  `.claude/skills/speckit-card-implement/SKILL.md` carries the pickup section and still the close-out section, that the
  specify and bug-assess skills still carry T038's and T043's idea sections, and that T058's test passes; leave every
  file `reapply.sh` regenerates in the working tree (depends on T059)
- [X] T063 [US5] Add a failing test in `tests/KCC.ViteTests/Tools/projectBoard.test.ts` that reads
  `.claude/skills/speckit-bug-fix/SKILL.md` as text (plan D33, the D20 pattern) and checks it carries
  `source: preset:kcc-ship`, a `## Building at pickup` heading before `## Publish through ship-branch`, the call
  `node tools/project-board/pickup.mjs bug/`, and `gh issue edit` with `--add-label building`. It fails today, while
  the skill has no pickup section
- [X] T064 [P] [US5] Add a `## Building at pickup` section to the existing wrap
  `.specify/presets/kcc-ship/commands/speckit.bug.fix.md`, after `{CORE_TEMPLATE}` and before
  `## Publish through ship-branch`, and create no second bug fix wrap (plan D33, FR-020, Q4): it adds one step after
  Execution step 1 has confirmed the plan, the verdict not being `invalid`, and before step 2 applies the remediation.
  The command runs `node tools/project-board/pickup.mjs bug/<BUG_SLUG>` and reads its JSON, then follows T059's rules:
  with `number`, `gh issue edit <number> --repo <repository> --add-label building` and nothing else from the JSON in
  the command; with `refused`, a non-zero exit or a failed edit, a one-line report naming `gh label create building`
  when the label is missing, and on to step 2. It never pushes a branch for the label and is never a stop. The section
  names the board stage it shows and that the sync takes the label off (depends on T063)
- [X] T065 [US5] In `.specify/presets/kcc-ship/preset.yml`, widen the `speckit.bug.fix` entry's `description` to name
  the pickup label, keeping T060's 1.4.0 (plan D31); run `.specify/presets/kcc-ship/scripts/reapply.sh`, then check
  that `.claude/skills/speckit-bug-fix/SKILL.md` carries the pickup section and still the ship-branch section, that
  the card implement skill still carries T060's pickup section, that the specify and bug-assess skills still carry
  their idea sections, and that T058's and T063's tests pass; leave every file `reapply.sh` regenerates in the working
  tree (depends on T064, T060)
- [x] T061 [PERSON] Before the merge, the owner makes the label once, as listed in [quickstart.md](quickstart.md):
  `gh label create building --repo Th3FenrisWolf/Kitchen-Command-Center` with its description (plan D32). No code row
  waits on it; the after-merge checks of US5 do
- [X] T062 [US5] Run `pnpm vitest run projectBoard` from `src/KCC.Web` (all green), then `git fetch --prune`, the dry
  run and the `pickup.mjs` reads of [quickstart.md](quickstart.md) step 6: no `unlabel building` line and no card moved
  to Building, every other line as T028's; `pickup.mjs 008-phone-idea-capture` answers `#149`; `001-photo-recipe-import`
  refuses as closed; `bug/board-sync-duplicates` refuses as closed; `999-no-such-spec` refuses as missing; `abc` and
  `fix/board-sync-duplicates` exit non-zero; as twinright-bzs it exits non-zero naming that account, then `gh auth
  switch -u Th3FenrisWolf`. Read-only (depends on T052, T054, T057, T060, T065)

**Checkpoint**: every story is in place.

---

## Phase 10: Docs, owner steps and the definition of done

- [X] T044 [WAITS:T062] Correct the project board notes in `CLAUDE.md`'s Spec Kit section (FR-017, constitution VII):
  the workflow also runs on issue events from the owner's account; every issue the owner opens is an idea the sync puts
  in Idea with Kind Idea and Iteration Current, filling only blank fields, archives when it closes and brings back when
  it reopens or is archived by hand while open; the sync still never touches a draft card without a Key, and nobody
  else's issue; `/speckit-specify #NNN` and `/speckit-bug-assess #NNN` read an idea through
  `tools/project-board/idea.mjs` and write the Idea line, and the sync then makes the idea's card the spec's or bug's;
  setting a Key by hand still works; `reapply.sh` also restores these two wraps and card implement's pickup step. Add
  the `building` label to the stage list (FR-022): Building also comes from a `building` label on the card's issue while
  nothing pushed has moved the card past Specified; `/speckit-card-implement` and `/speckit-bug-fix` add it when they
  start, through `tools/project-board/pickup.mjs` and one `gh issue edit`, and push nothing; the owner may add or remove
  it by hand; the sync takes it off once a branch or pull request carries the stage, and a `building` label event starts
  a run. Check `README.md` and `docs/` for a line this feature makes untrue and correct it (depends on T043, T062)
- [X] T045 [PERSON] The owner runs `/speckit-constitution` to amend the Flow section of
  `.specify/memory/constitution.md` as a MINOR change, 2.0.0 to 2.1.0 (FR-017): the board holds the owner's issues as
  ideas the sync files and archives, the sync touches an idea card only to fill its blanks and archive or restore it,
  and a spec or bug takes over the idea its Idea line names; and a card whose issue carries the `building` label shows
  at Building while nothing pushed has moved it past Specified, card implement and bug fix adding the label when they
  start and the sync taking it off once a branch or pull request carries the stage (FR-022). Only the owner amends the
  constitution, and the pull request does not ship without it
- [x] T046 [PERSON] Before the merge, the owner turns off the board's built-in "Item closed" workflow (project 3,
  Workflows) and leaves "Auto-close issue" on, as decided in Q1 and listed in [quickstart.md](quickstart.md)
- [X] T047 [WAITS:T044] Run the definition of done rows that apply: D1 `dotnet build KitchenCommandCenter.slnx -c
  Release`, D2 `pnpm build:all`, D3 `pnpm type-check`, `pnpm lint` and `pnpm format` in `src/KCC.Web`, keeping what lint
  and format change, D4 `node tests/scripts/run.mjs` with a no-op `open` first on `PATH`, and D10 against T044. D5 to D9
  are N/A
- [X] T048 [WAITS:T045] Check that `.specify/memory/constitution.md` reads 2.1.0, that its Flow section and
  `CLAUDE.md`'s board notes say the same thing, and correct `CLAUDE.md` where they differ

---

## Dependencies

- Phase 1, then Phase 2. Every later phase needs Phase 2.
- US1: Phase 3, then Phase 4. US2 (Phase 5) needs Phase 4. US3's sync phase (Phase 6) needs Phase 4; its command phase
  (Phase 7) needs Phase 2 only for the code, but runs after Phase 6 so one builder owns `derive.mjs` at a time. US4
  (Phase 8) needs Phases 6 and 7.
- US5 (Phase 9) runs after Phase 8: it writes `derive.mjs`, the test file, `sync.mjs`, the workflow and `preset.yml`,
  which earlier phases also write. Inside it, T051 waits on T049 and T050, T054 on T053, T056 on T055, T059 on T058,
  T064 on T063, and T065 on T064 and T060, so `reapply.sh` last runs with both pickup sections in place. T062 waits
  on T065.
- Every phase from 2 to 9 writes `tests/KCC.ViteTests/Tools/projectBoard.test.ts` and `tools/project-board/derive.mjs`,
  so the phases run in order, not side by side.
- Each wrap row waits on its failing skill test: T036 on T035, T042 on T041, T059 on T058, T064 on T063.
- T044 runs after Phase 9 (T062), since it documents the bug-assess wrap and the `building` label. T045, T046 and T061
  are the owner's, and no code row waits on them. T048 waits on T045. T047 runs after T044.

## Parallel examples

- Phase 2: T008 (`sync.mjs`) beside T002 to T007 (the test file and `derive.mjs`).
- Phase 7: T036 (the wrap) beside T029 to T034, once T035's failing test is in.
- Phase 8: T042 (the wrap) beside T039 and T040, once T041's failing test is in.
- Phase 9: T052 (`sync.mjs`) beside T053 to T056; T059 and T064 (the wraps) beside T053 to T057, once T058's and
  T063's failing tests are in.
- Phase 10: none. T044 lands before T047, whose D10 row reads it.

## Implementation strategy

- **MVP**: Phases 1 to 4. A title-only issue from the phone reaches the Idea column, and #148, #153 and #154 are filled
  on the first run after the merge (SC-001, SC-002).
- Then Phase 5 (drop and bring back), Phases 6 and 7 (spec takeover and its command), Phase 8 (bug takeover),
  Phase 9 (Building at pickup), and Phase 10.

## After the merge

The owner runs the "After the merge" table of [quickstart.md](quickstart.md) for SC-001 to SC-008. Those checks open
and close issues and push branches, so they write the live board, and they are not rows of this file. SC-001's check
opens 10 title-only issues from the GitHub app, one at a time, and passes when at least 9 of the 10 reach the board
within two minutes of their creation, each Idea, Idea, Current, with no Key and its title unchanged.

## Notes

- Plan D4 leaves a Key'd archived card that is not a takeover untouched; whether a field can be written on an archived
  item stays unverified (research, Not verified), and no row writes one.
- Task ids changed when this file was redone for plan D18 to D23: the old T005 to T041 are now T006 to T048, with the
  new test rows T005, T023, T025, T030, T031, T035 and T041. Old T039, the owner's "Item closed" step, is T046 and
  stays done; old T038, the constitution amendment, is T045, and its waiting row is T048.
- T049 to T062 were added for checkpoint Q2, Building at pickup, and the docs phase became Phase 10. No earlier id
  changed. Q3 is answered: the label counts whoever added it, so no row reads who added it. Q4 is answered: bug fix
  adds the label too, through T063 to T065. T061 is done; the owner made the label on 2026-11-16.
