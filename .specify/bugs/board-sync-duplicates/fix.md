# Bug Fix: Board sync rewrites the wrong card and duplicates issues

- **Slug**: board-sync-duplicates
- **Fixed**: 2026-11-16
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

The fix shipped in pull request #155 (`chore/specs-and-fixes`), merged on 2026-11-16, alongside specs 008, 009 and 010.
This record is committed apart from it because the board sync marks a bug Done when its `fix/<slug>` pull request
merges or its `fix.md` is on `main`, and neither held for this one.

1. A spec number that two or more folders share, on any branches, no longer resolves to whichever folder was read
   last. The sync holds every card the number touches: each clashing folder's card, and every card of a phased
   feature whose table on any branch names the number. It writes every other card, prints the clash with its folders,
   branches and held keys, and exits 1.
2. After `createIssue`, the sync re-reads the issue's `projectItems` up to four times, with pauses of 1, 2, 4 and 8 s,
   and calls `addProjectV2ItemById` only when no read found the item. A failed read is logged and retried.

## Changes

The fix's own files. The same commit also added this assessment and changed the phases tables of specs 001 and 006.

| File | Change | Notes |
|------|--------|-------|
| `tools/project-board/derive.mjs` | modified | adds `numberOf`, `numberClashes`, `clashReport`, `waitForBoardItem`, `tryRead` and `strayKeys`; `deriveCards` and `applyPhases` mark held cards, `applyPhases` resolves only numbers that name one folder, and `planChanges` skips held cards |
| `tools/project-board/sync.mjs` | modified | adds `sleep`, `readIssueItems` and `addToBoard`; takes stray keys from `strayKeys`, re-reads a created issue for its item, reports a clash and sets `process.exitCode = 1` |
| `tests/KCC.ViteTests/Tools/projectBoard.test.ts` | added tests | the clash holds and finding the created item |
| `CLAUDE.md` | modified | describes the clash behaviour |

## Diff Highlights

```js
export function waitForBoardItem(issue, projectId, reread, pause) {
  let itemId = boardItemOf(issue, projectId);
  for (const ms of ITEM_PAUSES_MS) {
    if (itemId) {
      return itemId;
    }
    pause(ms);
    itemId = boardItemOf(tryRead(reread, issue.id), projectId);
  }
  return itemId;
}
```

```js
if (clashes.length > 0) {
  console.error(clashReport(clashes, cards));
  process.exitCode = 1;
}
```

## Tests Added or Updated

In `tests/KCC.ViteTests/Tools/projectBoard.test.ts`:

- `a spec number two folders share`: names the number with each folder and its branches; one folder on several
  branches is no clash; holds both folders and every card of the feature whose table names the number, including a
  table on another branch only; holds the whole feature when phase 1's number is the shared one; holds nothing while
  every number names one folder; plans no change for a held card and calls none of its items a stray; reports the
  clash and the held cards plainly.
- `a created issue`, five new tests: takes the item from the `createIssue` reply with no re-read; found on a later
  read, pausing longer each time; reads on past a failed read or a missing issue; gives up with no item and no error
  when every read fails or after the last read.

## Local Verification

- `pnpm exec vitest run Tools/projectBoard.test.ts` (from `tests/KCC.ViteTests`, at `8ea0218b`) → 64 of 64 passed.
- Pull request #155 reported `node tests/scripts/run.mjs` → 4285 passed, 0 failed, 2 skipped across 6 suites.
- The Project board run on the push of `8ea0218b` to `main` (run 43937935855) succeeded.
- Not checked: a live clash across two branches, and that `gh` returns a created issue's item on a later read within
  about 15 s. Neither can be reproduced offline.

### Definition of done

As pull request #155 records it, and this record's own run where it says so.

| Row | Result | Notes |
|---|---|---|
| D1 `dotnet build` | not recorded | The fix changes no .NET code. |
| D2 `pnpm build:all` | not recorded | The fix changes no bundled source; `tools/project-board/` runs under Node directly. |
| D3 type-check, lint, format | not recorded | Applies through `tests/KCC.ViteTests/Tools/projectBoard.test.ts`. |
| D4 all six suites | pass | `node tests/scripts/run.mjs` → 4285 passed, 0 failed, 2 skipped, per pull request #155. This record's run of `Tools/projectBoard.test.ts` → 64 of 64. |
| D5 generated files | N/A | Nothing generated changed. |
| D6 both ramps | N/A | No public-site visual change. |
| D7 brand-steward | N/A | No public-site visual change. |
| D8 deploy lint | N/A | Nothing under `deploy/` changed. |
| D9 images | N/A | No Docker or `deploy/` file changed. |
| D10 docs | pass | `CLAUDE.md` describes the clash behaviour. |

### Review loop

`ship-branch` ran on pull request #155 at the `full` tier, by hand.

| Round | Findings | Outcome |
|---|---|---|
| 1 | 3, 1 real | Fixed: a failed or empty re-read of a created issue ended the run with no Key set, so the next run would duplicate the issue. |
| 2 | 3, all real | Fixed: a swallowed read error is logged, and a stale comment is corrected. The third was in spec 008. |
| CodeRabbit | 4, all real | Fixed: a table on another branch naming a clashing number holds the feature, with a new test. The other three were in specs 008, 009 and 010. |

No test was failing before the work.

## Deviations from Assessment

- The fix went out on `chore/specs-and-fixes` with three new specs, not on `fix/board-sync-duplicates`, and no
  `fix.md` went with it.
- The fix landed as one squashed commit, so this record cannot show that the new tests failed before the fix.

## Follow-ups

For `/speckit-bug-test`:

- Watch the next sync run that creates an issue: the issue gets its Key and fields, and no second issue appears.
- A live clash across two branches: the run exits 1, prints the clash and its held keys, leaves the held cards as they
  were and writes the rest.
