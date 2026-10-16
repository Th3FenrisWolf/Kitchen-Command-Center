# Bug Verification: The grid and list icons sit off centre in the view toggle

- **Slug**: view-toggle-icons-off-center
- **Tested**: 2026-10-15
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

The bug no longer reproduces. On the recipe search view toggle and the recipe detail view toggle, each icon sits in
the centre of its 36px segment, in both ramps, at desktop and phone width. The text segments keep their widths, and no
regression was found. Fix commit: `20e439c` on `fix/view-toggle-icons-off-center`.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction, recipe search | headless Chromium (Playwright 1.58) on a seeded site from the fix branch: `/recipes`, measure each view-toggle icon's centre against its segment's centre | pass | `dx` and `dy` are 0.00px for grid and list, light and dark, 1280×900 and 375×812 |
| Reproduction, recipe detail | same check on `/recipes/avocado-toast-supreme/` | pass | same result |
| Both states, both ramps | screenshots of each toggle with grid checked and with list checked | pass | the icon sits in the centre of the ink fill and of the empty segment |
| Text segments unchanged | the same run measures each text segment's width | pass | sort tracks are 357px (search) and 259px (detail), the same as on `fix/segmented-slider-no-slide` before this fix |
| New tests | `dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj -- --treenode-filter "/*/*/RecipeSearchTests/*"` | pass | 11 of 11, including both `View_toggle_icon_sits_in_the_centre_of_its_segment` cases. The fix run showed both fail against the old CSS |
| Regression, recipe detail | same command, `VariantDetailTests` | pass | 3 of 3 |
| Vite suite | `yarn vitest run` (from `src/KCC.Web`) | pass | 1451 passed, 2 expected fail |
| Type check | `yarn type-check` | pass | |
| Lint | `yarn lint` | not-run | `src/KCC.Web` has no ESLint config |
| Full suite `node tests/scripts/run.mjs` | not run on this branch | skipped | it passed on the base branch, `fix/segmented-slider-no-slide` (2656 passed, 0 failed). This branch adds one CSS line and one E2E test. Run it before the pull request opens |

## Output Excerpts

```text
== light-desktop-search theme=light
  track=357 | sort-relevant w=92.1 | sort-rated w=97.9 | sort-variants w=90.1 | sort-recent w=76.6
  track=72 | view-grid w=36.0 dx=0.00 dy=-0.00 | view-list w=36.0 dx=-0.00 dy=0.00
== dark-phone-detail theme=dark
  track=259 | sort-newest w=76.6 | sort-fastest w=82.3 | sort-rating w=99.8
  track=72 | view-grid w=36.0 dx=0.00 dy=0.00 | view-list w=36.0 dx=0.00 dy=0.00
```

```text
RecipeSearchTests  total: 11  failed: 0  succeeded: 11
VariantDetailTests total: 3   failed: 0  succeeded: 3
Vite               Tests  1451 passed | 2 expected fail (1453)
```

## Residual Risks

- Only Chromium was exercised. Safari and Firefox were not.
- The branch stacks on `fix/segmented-slider-no-slide`. A pull request for this branch carries both fixes unless that
  branch merges first.

## Recommendation

Close the bug. Verified end to end on both view toggles, in both ramps and at both widths. Run the full `run.mjs`
suite on this branch before the pull request opens.
