# Bug Verification: The segmented control jumps between options instead of sliding

- **Slug**: segmented-slider-no-slide
- **Tested**: 2026-10-15
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

The bug no longer reproduces. On every `SegmentedControl` the assessment names, the ink thumb slides from the old
segment to the new one in about 300ms, in both ramps and on a scrolled phone-width track. The account settings
toggle snaps, as the assessment expects. No regression was found. Fix commit: `2a3913f` on
`fix/segmented-slider-no-slide`.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction, recipe search, light and dark | headless Chromium (Playwright 1.58) on a seeded site from the fix branch: `/recipes`, click each sort segment and each view toggle 1.5s apart, sample the thumb each frame | pass | every click moves the thumb within 21 to 81ms and it comes to rest over the checked segment at 282 to 336ms, with 17 to 32 frames between the two segments |
| Reproduction, recipe detail variant toolbar, light and dark | same check on `/recipes/avocado-toast-supreme/`: sort newest, fastest, top rated, view list, grid | pass | same timing |
| Narrow, scrolled track | 375×812 viewport, track 253px wide of 357px; click `RECENT` (scrollLeft 104), then `TOP RATED` (scrollLeft 92) | pass | thumb offset from the checked button ≤ 0.1px after each slide |
| Account settings ramp toggle | signed in as a generated test member, `/account/settings`: dark, light, device | pass | the thumb snaps to the new segment with no slide. This is the `data-theme-switching` freeze the assessment calls the kit rule |
| Settled look, both ramps | screenshots of the sort, phone and variant controls | pass | light: ink thumb, paper label. Dark: chalk thumb, dark label. The checked button has no background of its own |
| Reproduction in the Browser pane | real clicks on `/recipes` in the app's Browser pane, dark ramp | pass, with a pane artifact | slides of about 280ms. A click that followed a 2s idle wait in a batch began to move about 340ms late, with the transitions created at once and their clock held at 0. A click with no idle wait before it slid at once. Headless Chromium shows no delay, so this is the pane's frame scheduling |
| New / updated tests | `yarn vitest run` (from `src/KCC.Web`) | pass | 1451 passed, 2 expected fail, including the two new SSR tests |
| New E2E test and its suite | `dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj -- --treenode-filter "/*/*/RecipeSearchTests/*"` | pass | 9 of 9, including `Sort_thumb_slides_to_the_clicked_segment`. The fix run showed it fails against the old code |
| Regression, recipe detail | same command, `VariantDetailTests` | pass | 3 of 3 |
| Regression, ramp setting | same command, `RampSettingTests` | pass | 1 of 1 |
| Type check | `yarn type-check` | pass | |
| Builds | `yarn build:all`, `dotnet build` | pass | |
| Lint | `yarn lint` | not-run | `src/KCC.Web` has no ESLint config |
| Reduced motion | not exercised | not-run | the `Main.css` rule covers the CSS transition, per the brand review. No browser check |
| Forced colours | not exercised | not-run | known limitation before and after the fix (see fix.md) |
| Full suite `node tests/scripts/run.mjs` | not run | skipped | long run. The definition of done runs it before the pull request |

## Output Excerpts

```text
[light] sort-rated: from -92.1px, moves at 69ms, settles at 316ms, 26 frames between
[dark] sort-relevant: from 190px, moves at 33ms, settles at 305ms, 17 frames between
[phone] checked=sort-recent offset=0.0 dw=0.4 ... scrollLeft=104 trackW=253/357
[variant-dark] sort-fastest: from -76.6px, moves at 75ms, settles at 313ms, 29 frames between
[ramp] ramp-dark: from -0.4px, moves at -ms, settles at 12ms, 0 frames between
```

```text
RecipeSearchTests  total: 9  failed: 0  succeeded: 9
VariantDetailTests total: 3  failed: 0  succeeded: 3
RampSettingTests   total: 1  failed: 0  succeeded: 1
Vite               Tests  1451 passed | 2 expected fail (1453)
```

## Residual Risks

- Reduced motion and forced colours were not checked in a browser.
- Only Chromium was exercised. Safari and Firefox were not.
- The full `run.mjs` suite has not run on the branch yet.
- The thumb does not slide on the ramp toggle, by the kit's freeze rule. The owner may read that toggle as still
  jumping.

## Recommendation

Close the bug. The slide is back on every segmented control the report covers, verified in headless Chromium in both
ramps, at phone width and on the recipe detail toolbar, and the regression tests hold. Run the full `run.mjs` suite
before the pull request opens.
