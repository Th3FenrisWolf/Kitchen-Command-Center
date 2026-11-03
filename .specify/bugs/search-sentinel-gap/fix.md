# Bug Fix: Recipe search infinite scroll stalls while its sentinel stays in view

- **Slug**: search-sentinel-gap
- **Fixed**: 2026-11-03
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

`useInfiniteScroll` now re-observes its sentinel after each page of results lands, so the browser reports the
sentinel's position afresh once the new cards have laid out. A sentinel still in view loads the next page; one the
new cards pushed out loads nothing.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `src/KCC.Web/Features/Components/RecipeSearch/useInfiniteScroll.ts` | modified | New `pageLoaded` watch source; on a change, post-flush, `unobserve` then `observe` the current sentinel |
| `src/KCC.Web/Features/Pages/RecipeSearch/RecipeSearchView.Component.vue` | modified | Passes `results` as `pageLoaded` |
| `tests/KCC.ViteTests/Features/Pages/RecipeSearch/infiniteScroll.test.ts` | added test | Client mount of the view with an observer whose sentinel never leaves view |
| `tests/KCC.E2ETests/Config/Perf/Cases.cs` | modified comment | The comment said a sentinel still in view never loads again; the fix made that untrue. The harness's scroll pattern is unchanged, so measurements stay comparable |

## Diff Highlights

```ts
watch(
  pageLoaded,
  () => {
    const el = sentinel.value
    if (el) {
      observer?.unobserve(el)
      observer?.observe(el)
    }
  },
  { flush: 'post' },
)
```

`useRecipeSearch` assigns a new `results` array on every successful fetch, appended or replaced, and never on a
failed or superseded one, so a failure does not turn into a retry loop.

## Tests Added or Updated

- `infiniteScroll.test.ts` › "keeps loading pages while the sentinel stays in view, until every result is listed" —
  30 results, 12 per page, sentinel always in view: requests pages 1 and 2 and lists all 30 cards. Before the fix it
  requested page 1 only (`expected [ 1 ] to deeply equal [ 1, 2 ]`), committed red as `0b9d85c8`.
- `infiniteScroll.test.ts` › "does not retry a page whose fetch failed while the sentinel stays in view" — a failing
  fetch is requested once. Passes before and after; it pins the alternative the assessment rejected.

## Local Verification

Definition of done (`.specify/memory/constitution.md`):

| Row | Result |
|-----|--------|
| D1 `dotnet build KitchenCommandCenter.slnx -c Release` | pass, 0 warnings, 0 errors |
| D2 `yarn build:all` | pass |
| D3 `yarn type-check`, `yarn lint`, `yarn format` in `src/KCC.Web` | pass; lint reports 6 warnings, all in `Components/Forms/*Field.vue`, none in this diff; format reflowed the new test, committed |
| D4 `node tests/scripts/run.mjs`, no-op `open` on `PATH` | pass: 4098 passed, 0 failed, 1 skipped across 6 suites (.NET unit, integration and E2E 1612/1612; web 2460 passed, 1 expected fail, 1 skipped; admin 17; contributions 8). No E2E class failed, so no rerun |
| D5 generated files | N/A |
| D6 both ramps | N/A, no visual change |
| D7 brand-steward | N/A, no visual change |
| D8, D9 deploy | N/A |
| D10 docs | pass, no line in `README.md`, `CLAUDE.md` or `docs/` made untrue |

## Review

Run on the local diff against `main` (`37febb13`), at the orchestrating session's direction: no pull request is
open, and this branch is collected onto a close-out branch that opens the one pull request.

| Round | Status | Findings |
|-------|--------|----------|
| Agent round 1 (`correctness`, `security-rules`) | completed | 0 |
| Final round (CodeRabbit) | skipped | It runs on the close-out branch's pull request |

Code repo: this worktree. Context: `.specify/memory/constitution.md`, `CLAUDE.md`.

## Deviations from Assessment

None.

## Follow-ups

- `LoadThreePagesAsync` in the perf harness still scrolls away and back on every attempt. With the fix it could
  rest at the bottom, but changing it moves the perf baseline, so it stays.
