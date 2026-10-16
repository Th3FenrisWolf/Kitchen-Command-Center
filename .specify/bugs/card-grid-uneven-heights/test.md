# Bug Verification: Cards in a grid row have different heights

- **Slug**: card-grid-uneven-heights
- **Tested**: 2026-10-15
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

The symptom no longer reproduces. On a seeded site, every sheet in every row of all three card grids has the same
height, at one, two and three columns, in both ramps. The new and existing tests pass, and no regression appeared.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix), light | Built site (Testing env, fresh SQLite, `POST /api/dev/seed-recipes`), measured each grid row's `.kcc-sheet` `offsetHeight` at 1440, 900 and 375px on `/recipes`, `/recipes/spicy-ramen-flight/` and `/recipes/spicy-ramen-flight/miso-vegan/` | pass | 3, 2 and 1 columns on each page. Largest spread inside a row: 0px in all nine cases. |
| Reproduction (post-fix), dark | Same measurement and a screenshot of the variant grid with `prefers-color-scheme: dark` | pass | `data-theme="dark"`. Rows 190/190/190 and 190/190, Add Variant included. Tears, shadow and rule intact. Plus and label centred. |
| Reported case | Search grid at 1440px, light | pass | Crispy Roasted Chickpeas wraps its badges onto two rows. Cold Brew Concentrate and Fluffy Buttermilk Pancakes stretch to its 194px with blank ruled paper below their content. |
| New / updated unit tests | `cd src/KCC.Web && yarn test` | pass | 120 files, 1452 passed, 2 expected fail. Includes the `RecipeCard`, `VariantGrid` and `VariantSiblings` tests from the fix. |
| New E2E tests | `dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj --no-build -- --treenode-filter "/*/*/<Class>/*"` | pass | `VariantGridTests` 1/1, `RecipeSearchTests.Grid_cards_in_a_row_share_one_height` inside 9/9. Both failed on the unfixed bundles during `/speckit-bug-fix`. |
| Related E2E classes | Same command for `RecipeSearchTests`, `RecipeSearchLiveRatingTests`, `VariantDetailTests` | pass | 9/9, 1/1, 3/3. |
| Type-check | `yarn type-check` (`vue-tsc --build`) | pass | |
| Lint | `yarn lint` | pass | 0 errors. 4 warnings in `InputField.vue` and `TextAreaField.vue`, files the fix does not touch. |
| Full suite | `node tests/scripts/run.mjs` | skipped | Long run. The constitution's definition of done runs it before the pull request. |

## Output Excerpts

```text
/recipes 1440px light   cols 3  rows 166/166/166 | 194/194/194 | 166/166/166 | 166/166   spread 0
/recipes 900px light    cols 2  rows 166/166 ×5 | 166                                 spread 0
/recipes/spicy-ramen-flight/ 1440px dark   190/190/190 | 190/190                       spread 0
/recipes/spicy-ramen-flight/miso-vegan/ 1440px light   120/120/120                     spread 0
```

```text
 Test Files  120 passed (120)
      Tests  1452 passed | 2 expected fail (1454)
```

## Residual Risks

- The check used the seeder's recipes, not the owner's own data from the report (Big-Pot Chili, Whole-Wheat Yogurt,
  Bakery Crumb-Top). The seeded search grid has the same shape of fault: one card with an extra badge row.
- The `brand-steward` note from the fix stands: `content-center` moves the Add Variant label off the 24px rule. The
  owner chose centring in Q2.
- No E2E test covers the sibling grid on variant detail. The unit test pins the class there, and the manual
  measurement above covers its layout.
- The full `run.mjs` suite did not run.

## Recommendation

Close the bug. The fix is verified end-to-end on a seeded site in both ramps and at three widths. Run the full
`run.mjs` suite before the pull request opens.
