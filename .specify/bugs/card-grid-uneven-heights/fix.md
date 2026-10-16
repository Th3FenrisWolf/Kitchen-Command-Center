# Bug Fix: Cards in a grid row have different heights

- **Slug**: card-grid-uneven-heights
- **Fixed**: 2026-10-15
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

`RecipeCard` and the Add Variant slip now carry the kit's `kcc-slip--fill` modifier, so the torn link and the paper
take the height of the stretched grid item. Every sheet in a row ends at the height of the tallest one, in all three
card grids. The Add Variant sheet centres its plus and label in the taller sheet.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `src/KCC.Web/Features/Components/Recipe/RecipeCard.vue` | modified | `kcc-slip--fill` on the `<article>` slip. Covers recipe search, the variant grid and the sibling grid. |
| `src/KCC.Web/Features/Components/RecipeDetail/VariantGrid.vue` | modified | `kcc-slip--fill` on the Add Variant slip, `content-center` on its sheet (owner decision Q2). |
| `tests/KCC.ViteTests/Features/Components/Recipe/RecipeCard.test.ts` | added test | The slip carries `kcc-slip--fill`. |
| `tests/KCC.ViteTests/Features/Components/RecipeDetail/VariantGrid.test.ts` | added test | The Add Variant slip fills and its sheet centres its content. |
| `tests/KCC.ViteTests/Features/Components/VariantDetail/VariantSiblings.test.ts` | updated test | Its tear regex pinned the slip's class order. It now includes `kcc-slip--fill`. |
| `tests/KCC.E2ETests/Config/GridRows.cs` | added | Groups a grid's slips by `offsetTop` and returns each sheet's `offsetHeight`. Both ignore the slip's rotation. |
| `tests/KCC.E2ETests/Features/RecipeSearch/RecipeSearchTests.cs` | added test | Every row of the search grid has one sheet height. |
| `tests/KCC.E2ETests/Features/RecipeDetail/VariantGridTests.cs` | added | Every row of the Spicy Ramen Flight variant grid, Add Variant sheet included, has one sheet height. |

## Diff Highlights

```diff
-    class="kcc-slip kcc-recipe transition-transform focus-within:-translate-y-1 hover:-translate-y-1"
+    class="kcc-slip kcc-slip--fill kcc-recipe transition-transform focus-within:-translate-y-1 hover:-translate-y-1"
```

```diff
-      class="kcc-slip transition-transform focus-within:-translate-y-1 hover:-translate-y-1"
+      class="kcc-slip kcc-slip--fill transition-transform focus-within:-translate-y-1 hover:-translate-y-1"
 ...
-        <div class="kcc-sheet grid justify-items-center gap-6 text-center" style="--pad: 48px">
+        <div class="kcc-sheet grid content-center justify-items-center gap-6 text-center" style="--pad: 48px">
```

## Tests Added or Updated

- `RecipeCard.test.ts` › "fills the grid row, so every card in a row ends at the same height"
- `VariantGrid.test.ts` › "stretches the add-variant slip to its row and centres the plus and the label on it"
- `VariantSiblings.test.ts` › "gives every sibling a slip of its own…" now also pins the fill on the sibling grid
- `RecipeSearchTests.Grid_cards_in_a_row_share_one_height` — every search grid row within 1px, at 1920×1080
- `VariantGridTests.Variant_cards_and_the_add_variant_sheet_in_a_row_share_one_height` — five slips, every row
  within 1px

## Local Verification

- Before the fix: the two new Vitest tests failed, and both new E2E tests failed with "Expected to be less than or
  equal to 1" on bundles built from the unfixed source.
- `yarn build:all`, then `dotnet build --disable-build-servers` → succeeded.
- `cd src/KCC.Web && yarn test` → 120 files, 1452 passed, 2 expected fail.
- `yarn lint` → 0 errors, 4 warnings, all in `InputField.vue` and `TextAreaField.vue`, untouched here.
- `dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj --no-build -- --treenode-filter "/*/*/<Class>/*"` for
  `RecipeSearchTests` (9/9), `RecipeSearchLiveRatingTests` (1/1), `VariantGridTests` (1/1) and
  `VariantDetailTests` (3/3) → all passed.
- `brand-steward` review → on brand. Tear, filter, clip, tile and tape placement, rule repeat and both ramps stay
  intact. One note, under Follow-ups.
- Not verified: a rendered look in the dark ramp and at one and two columns. The E2E checks run in the light ramp at
  1920px.

## Deviations from Assessment

- `VariantSiblings.test.ts` was not listed. Its regex matched the slip's exact class string, so it needed the new
  class.
- `docs/brand/kit.md` needs no change: its `kcc-slip--fill` entry does not list where the modifier is used.
- The E2E helper `tests/KCC.E2ETests/Config/GridRows.cs` is new. It reads `offsetHeight` because a rotated slip's
  bounding box differs from its layout height.

## Follow-ups

- The brand review notes that `content-center` moves the Add Variant label off the 24px pencil rule (kit.md Rule 4).
  The owner chose centring in Q2. If the rule matters more, drop `content-center` so the plus and label stay at the
  top of the sheet.
- Check the dark ramp and one and two columns by eye in `/speckit-bug-test`.
