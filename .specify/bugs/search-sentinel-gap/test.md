# Bug Verification: Recipe search infinite scroll stalls while its sentinel stays in view

- **Slug**: search-sentinel-gap
- **Tested**: 2026-11-03
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

The reproduction ran in a real browser against the E2E site, before and after the fix: in a viewport tall enough
that the sentinel stays in view, the pre-fix build stops short of the 36 seeded recipes and the fixed build lists all
36 without a scroll. All six suites pass with the fix.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction, pre-fix | Temporary Playwright test (not committed): viewport 1280×6000, open `/recipes`, expect 36 `[data-recipe-name]` within 15s; `useInfiniteScroll.ts` and the view checked out from `37febb13`, `yarn build:all`, then `dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj -- --treenode-filter "/*/*/ScratchSentinelTests/*"` | fail (bug reproduces) | 1 failed: the count never reached 36 |
| Reproduction, post-fix | Same test on `2fd6ff59`, bundles rebuilt | pass | 1 succeeded, 18.6s |
| New tests | `npx vitest run ../../tests/KCC.ViteTests/Features/Pages/RecipeSearch/infiniteScroll.test.ts` in `src/KCC.Web` | pass | 2 passed; the first failed before the fix (`expected [ 1 ] to deeply equal [ 1, 2 ]`) |
| Regression suite | `node tests/scripts/run.mjs`, no-op `open` on `PATH` | pass | 4098 passed, 0 failed, 1 skipped across 6 suites; no E2E class failed |
| Build | `dotnet build KitchenCommandCenter.slnx -c Release`; `yarn build:all` | pass | 0 warnings |
| Lint / type-check / format | `yarn type-check`, `yarn lint`, `yarn format` in `src/KCC.Web` | pass | 6 lint warnings, all in `Components/Forms/*Field.vue`, outside this diff |

The temporary test file was deleted and the two source files restored to `HEAD` and rebuilt after the pre-fix run.

## Output Excerpts

```text
pre-fix:   total: 1  failed: 1  succeeded: 0
post-fix:  Test run summary: Passed!  total: 1  failed: 0  succeeded: 1  duration: 18s 555ms
suite:     4098 passed · 0 failed · 1 skipped across 6 suites
```

## Residual Risks

- Checked in Chromium on desktop only. Headless WebKit renders the phone viewport about once a second (per the perf
  harness), which slows each re-observe but does not change the outcome.
- The tall-viewport browser check is not in the committed suite; the committed test fakes the observer.

## Recommendation

Close the bug — verified end-to-end. A committed E2E version of the tall-viewport check would pin real observer
behaviour; it is a follow-up, not part of this fix.
