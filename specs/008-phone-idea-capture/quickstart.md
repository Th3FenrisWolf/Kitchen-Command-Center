# Quickstart: Phone Idea Capture

The workflow runs the sync from `main`, so before the merge the branch's sync runs only by hand, as a dry run. The
live scenarios run after the merge.

## Before the merge

1. `gh auth status` shows Th3FenrisWolf active (`gh auth switch -u Th3FenrisWolf` if not).
2. The unit tests: `pnpm vitest run projectBoard` from `src/KCC.Web`, then all six
   suites with `node tests/scripts/run.mjs`.
3. The dry run, on this branch:

   ```sh
   git fetch --prune
   node tools/project-board/sync.mjs --dry-run
   ```

   Expected, with today's board (research R2): `idea #148`, `idea #153` and `idea #154` each fill
   `Status=Idea, Kind=Idea, Iteration=Current`. No `idea` line for any issue that carries the sync block (#138 to
   #147, #149 to #152, #156 to #158). No issue title appears anywhere in the output. Every other line matches the
   dry run on `main`.
4. The command read:
   - `node tools/project-board/idea.mjs 148` prints its number, URL, title and body.
   - `node tools/project-board/idea.mjs 149` refuses: it carries the Key `008-phone-idea-capture`.
   - `node tools/project-board/idea.mjs 155` refuses: #155 is a pull request.
   - With `gh auth switch -u twinright-bzs`, `idea.mjs 148` stops with an error naming the account. Switch back.
5. `.specify/presets/kcc-ship/scripts/reapply.sh`, then `.claude/skills/speckit-specify/SKILL.md` and
   `.claude/skills/speckit-bug-assess/SKILL.md` show `source: preset:kcc-ship` and the idea section, and the wrap
   test in `projectBoard.test.ts` passes (plan D20).
6. Building at pickup, read-only:
   - The dry run of step 3 again: no line says `unlabel building` and no card moves to Building, since no card issue
     carries a label today (research R8). Every other line is unchanged.
   - `node tools/project-board/pickup.mjs 008-phone-idea-capture` prints `"number": 149` and the repository.
   - `node tools/project-board/pickup.mjs 001-photo-recipe-import` refuses: its issue is closed.
   - `node tools/project-board/pickup.mjs 999-no-such-spec` refuses: no card carries the Key.
   - `node tools/project-board/pickup.mjs bug/board-sync-duplicates` refuses: its issue is closed.
   - `node tools/project-board/pickup.mjs abc` and `pickup.mjs fix/board-sync-duplicates` exit non-zero with
     `<key> is not a card key`.
   - With `gh auth switch -u twinright-bzs`, `pickup.mjs 008-phone-idea-capture` stops with an error naming the
     account. Switch back.
   - After `reapply.sh`, `.claude/skills/speckit-card-implement/SKILL.md` carries the Building at pickup section and
     still carries the close-out section, `.claude/skills/speckit-bug-fix/SKILL.md` carries it and still carries the
     ship-branch section, and both text tests pass (plan D29, D33).

## Owner step before the merge

Per checkpoint Q1, turn off the board's built-in "Item closed" workflow (project 3, Workflows). Leave "Auto-close
issue" on.

Per checkpoint Q2 and plan D32, make the label once (done on 2026-11-16, T061):

```sh
gh label create building --repo Th3FenrisWolf/Kitchen-Command-Center --description "Being built locally; the board shows the card at Building"
```

## After the merge

| Check | Do | Expect |
|---|---|---|
| SC-002 | Wait for the push run on `main` | #148, #153, #154 in the Idea column, Kind Idea, Iteration Current |
| US1, SC-001 | Open 10 issues from the GitHub app, each with a title only, one at a time, each after the previous one reached the board or two minutes passed. Count those on the board within two minutes of their creation | At least 9 of 10 on the board within two minutes, each Idea, Idea, Current, no Key, title unchanged |
| US1 AS3 | Set one of those cards to Future and Kind Bug, then push any branch | Both values stay |
| US1 AS4 | Open an issue as twinright-bzs | The run starts and its job is skipped. The issue is not on the board. A run already waiting still runs |
| US2 | Close one of the test issues, then reopen it | Archived within two minutes, then back with the fields it had |
| US2 AS3 | Archive an open idea's card by hand, then run the workflow by hand | The card is back |
| US3 | `/speckit-specify #<test issue>`, push the branch | One card, carrying the spec's Key, title and stage, the owner's text below the synced block |
| US4 | `/speckit-bug-assess #<another idea>`, push | One card, Kind Bug, the bug's Key |
| US5, SC-007 | Start `/speckit-card-implement` on the next Specified card | Its issue carries `building` before Phase 2, no branch is pushed, and within two minutes the card shows Building |
| US5 AS7 | Start `/speckit-bug-fix` on the next Specified bug | Its issue carries `building` before the remediation is applied, no branch is pushed, and within two minutes the bug's card shows Building |
| US5 AS2, AS4 | On a Specified card's issue, add `building` from the GitHub app, then take it off | Building within two minutes, then Specified within two minutes. An unrelated label added to the same issue starts no run |
| US5 AS3, SC-008 | Let that card implement's close-out push its branch and open its pull request | The label is gone after the run that moves the card to In review |

Close or drop the test issues and branches afterwards. A pushed branch writes the live board.
