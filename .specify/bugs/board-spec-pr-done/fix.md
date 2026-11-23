# Bug Fix: Board sync marks a spec Done when only its spec merges

- **Slug**: board-spec-pr-done
- **Fixed**: 2026-11-23
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

A spec card's stage now depends on ticked tasks as well as its pull requests, so a pull request that carries only a
spec leaves the card Specified, and a build on the same branch after the spec has merged shows as Building, then In
review, then Done.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `tools/project-board/derive.mjs` | modified | `pullFor` takes the highest-numbered open pull request, else the highest-numbered merged one; `specCard` takes In review from an open pull request with a ticked task on the branch, Done from a merged pull request with a ticked task on `main`, and Building from a ticked task on the branch (see Deviations for the snapshot the body reads) |
| `tests/KCC.ViteTests/Tools/projectBoard.test.ts` | added and updated tests | `spec stage` describe |
| `CLAUDE.md` | modified | the board section says what moves a spec to In review and Done |

## Diff Highlights (optional)

```js
let stage = "Specified";
if (open && working.done > 0) {
  stage = "In review";
} else if (mergedOnce && landed.done > 0) {
  stage = "Done";
} else if (working.done > 0) {
  stage = "Building";
}
```

`working` counts the branch's tasks (or `main`'s when the branch is gone), `landed` counts `main`'s, and `mergedOnce`
is any merged pull request from the branch.

## Tests Added or Updated

- `spec stage > is In review while its pull request is open, and Done once it merges`: now carries ticked tasks; a
  closed pull request with ticked tasks is Building.
- `spec stage > stays Specified while a pull request carries only the spec`: open, merged, and merged with a `tasks.md`
  that has nothing ticked.
- `spec stage > follows the build on the same branch once the spec pull request has merged`: Building from the
  branch's ticked tasks, In review with the open build pull request linked, whichever order the pull requests come in.
- `spec stage > is Done once the build merges after its spec, and links the build`: links the build pull request #9
  whichever order the pull requests come in.

## Local Verification

- `pnpm exec vitest run ../../tests/KCC.ViteTests/Tools/projectBoard.test.ts` in `src/KCC.Web`: `stays Specified while a
  pull request carries only the spec` (`expected 'In review' to be 'Specified'`) and `follows the build on the same
  branch once the spec pull request has merged` (`expected 'Done' to be 'Building'`) failed before the fix, and the
  link check in `is Done once the build merges after its spec, and links the build` failed until `pullFor` took the
  highest-numbered pull request; 230 of 230 pass after it.
- `node tools/project-board/sync.mjs --dry-run` after `git fetch`: `update 011-design-consistency: Status=Specified,
  reopen`, `update 012-nav-structure: Status=Specified, reopen`, and no other change across 29 cards.
- The definition of done: see the close-out report.

## Deviations from Assessment

- The assessment picked the snapshot for the card's body by pull request state: the branch's while a pull request is
  open or none has merged, otherwise `main`'s. After a spec merges, that hides a build on the branch that has no pull
  request yet. The body now reads `main` when the card is Done, or Specified with a merged pull request, and the branch
  otherwise.
- The assessment said only that `pullFor` prefers an open pull request to a merged one. It now also takes the
  highest-numbered pull request of each state, because the sync reads pull requests oldest first, so a Done card
  linked the merged spec pull request instead of the merged build. `bugCard` shares `pullFor`, so a bug card with an
  open and a merged pull request from its `fix/` branch now shows In review, not Done, until its fix reaches `main`.
- `CLAUDE.md` is in scope for D10: its board section names the stages, and now says what moves a spec to In review
  and Done.

## Follow-ups

- None. The board corrects 011 and 012 on the first sync after this merges to `main`.
