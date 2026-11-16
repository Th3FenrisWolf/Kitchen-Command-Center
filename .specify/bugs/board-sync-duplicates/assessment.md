# Bug Assessment: Board sync rewrites the wrong card and duplicates issues

- **Slug**: board-sync-duplicates
- **Created**: 2026-11-16
- **Source**: pasted text, plus the log of workflow run 43914396036
- **Verdict**: valid
- **Severity**: medium

## Report (verbatim or summarized)

Two defects in the project board sync, `tools/project-board/sync.mjs` and `tools/project-board/derive.mjs`, which
`.github/workflows/project-board.yml` runs from `main` on every push.

1. On 2026-11-16 a spec was created as `008-script-hydration-cost` while the cloud branch
   `claude/tender-johnson-k7h243` already held `specs/008-phone-idea-capture`. Pushing it ran the sync, which gave the
   `008-phone-idea-capture` card Kind=Phase, Feature=006 Performance Sweep, Phase=2 of 3 and made issue #149 a
   sub-issue of #124 (006 phase 1). Stray issues #150 and #151 were created for the new spec. The spec has since been
   renumbered to `009-script-hydration-cost` (issue #152) and #150 is closed.
2. In the same run `createItem` failed. Run 43914396036 (push to `008-script-hydration-cost`, 17:04 UTC) logged:

   ```text
   update 008-phone-idea-capture: Kind=Phase, Feature=006 Performance Sweep, Phase=2 of 3, under 006-performance-sweep
   create 008-script-hydration-cost as issue: Specified
   gh: Content already exists in this project
   {"data":{"addProjectV2ItemById":null},"errors":[{"type":"UNPROCESSABLE","path":["addProjectV2ItemById"],
   "locations":[{"line":3,"column":11}],"message":"Content already exists in this project"}]}
   ```

## Symptom

1. Two spec folders with the same number on different branches are silently treated as one: a phases-table row
   naming the number rewrites whichever folder was read last. Expected: the sync notices the clash, writes no card the
   number touches, says so, and fails the run.
2. When `createIssue` answers without the issue's project item, the fallback `addProjectV2ItemById` fails because
   the issue is already on the board. The run dies, the new issue sits on the board with no Key, and the next run
   creates another issue for the same card. Expected: the sync finds the item it just created and writes its fields.

## Reproduction

1. Push a branch whose `specs/` holds `NNN-a`, while another branch holds `NNN-b`, and let a phases table name `NNN`
   (here `006-performance-sweep`'s table, carried by `008-script-hydration-cost`, named `008` for phase 2).
2. The sync builds `dirByNumber` and keeps the last folder read for `NNN`, then writes the phase fields and the
   parent onto that folder's card.
3. For the folder whose card does not exist yet, `createIssue` runs with `projectV2Ids: [board]`. Its reply holds no
   `projectItems` node for the board, so `addProjectV2ItemById` runs about 2.5 s later and fails with
   `Content already exists in this project`.

Step 3 depends on GitHub placing the item after `createIssue` answers. It reproduced on 2026-11-16 and is not
deterministic.

## Suspected Code Paths

- `tools/project-board/derive.mjs:194` `applyPhases()` — `dirByNumber` maps `dir.slice(0, 3)` to one folder, so a
  second folder with the same number overwrites the first with no sign.
- `tools/project-board/derive.mjs:251` `deriveCards()` — derives every card with no check across folders.
- `tools/project-board/sync.mjs:455` `createItem()` — the comment says `addProjectV2ItemById` returns the existing
  item. GitHub answers `UNPROCESSABLE` instead.
- `tools/project-board/sync.mjs:603` — the run plans and applies every op and always exits 0 unless a call throws.

## Root Cause Hypothesis

High confidence. (1) Spec numbers are assumed unique across every branch, and the number is the only link from a
phases-table row to a card, so a clash picks a folder by read order. Nothing checks the assumption. (2) `createIssue`
adds the issue to the board but does not always return the new item in the same reply. The fallback assumed
`addProjectV2ItemById` was idempotent, which the run log disproves, so the item exists and is never found.

## Proposed Remediation

**Preferred**:

1. In `derive.mjs`, find every number that two or more folders share, across all branches. Derive the cards as now,
   but mark as held every card the number touches: each clashing folder's card, and every card of a phased feature
   whose table names the number (the phase 1 card, each later phase's spec card and each placeholder). `applyPhases`
   resolves only numbers that name one folder. The sync plans changes for the other cards only, treats held keys as
   known so no "no longer in the repository" warning fires for them, prints each clash with its folders and branches
   and the held keys, and exits 1 after writing the rest.
2. In `sync.mjs`, when the `createIssue` reply has no item on the board, re-read the issue's `projectItems` a few
   times with a short, growing pause before falling back to `addProjectV2ItemById`. Put the retry in `derive.mjs` as a
   function that takes the re-read and the pause as parameters, so it is unit-tested. Correct the comment.

**Alternatives**:

- Stop the whole run on any clash, writing nothing. Simpler, but a stray branch then freezes every card on the board
  until it is renumbered or deleted.
- After `Content already exists`, scan the board's items for the issue's content id. Costs a full board read and the
  same eventual consistency applies.

**Files likely to change**:

- `tools/project-board/derive.mjs`
- `tools/project-board/sync.mjs`
- `tests/KCC.ViteTests/Tools/projectBoard.test.ts`

**Tests to add or update**:

- Two folders sharing a number: both cards and every card of a feature naming the number are held, the clash is
  reported with its folders and branches, and other cards are not held.
- The clash as phase 1's number holds the whole feature.
- No clash: no card is held, and one folder on several branches is no clash.
- Finding the item: found in the first reply with no re-read; found on a later re-read; not found after the last try,
  with the pause called between tries.

## Risks & Considerations

- A held card's item is never touched while the clash lasts, so the board can show stale fields until one folder is
  renumbered. That is the intent.
- The run now fails for a state that used to pass. A stale branch still holding an old number fails every sync until
  it is deleted or rebased.
- The retry adds at most about 15 s to a run in the rare case it is needed.

## Open Questions

None. The scope of "touches" (the clashing folders plus the whole phased feature naming the number) is this
assessment's reading of the brief.
