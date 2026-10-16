# Bug Fix: The grid and list icons sit off centre in the view toggle

- **Slug**: view-toggle-icons-off-center
- **Fixed**: 2026-10-15
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

`.kcc-seg button` centres its content with `justify-content: center`, so the icon in each 36px icon-only segment sits
in the middle of its segment. A text segment is as wide as its content plus padding, so its look does not change.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `src/KCC.Web/Features/Styles/Torn/Controls.css` | modified | `justify-content: center` on `.kcc-seg button` |
| `tests/KCC.E2ETests/Features/RecipeSearch/RecipeSearchTests.cs` | added test | the icon's centre is within 1px of its segment's centre, for grid and list |

## Diff Highlights

```css
.kcc-seg button {
  ...
  gap: 8px;
  justify-content: center;
  letter-spacing: 0.12em;
  ...
}
```

## Tests Added or Updated

- `RecipeSearchTests.View_toggle_icon_sits_in_the_centre_of_its_segment("view-grid")` and `("view-list")`: on
  `/recipes`, the horizontal centre of the segment's `<i>` is less than 1px from the segment's centre.

## Local Verification

- The new test against the `HEAD` `Controls.css`, rebuilt with `yarn build:all` → 2 of 2 failed, `Expected to be less
  than 1`.
- With the fix, rebuilt: `dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj -- --treenode-filter
  "/*/*/RecipeSearchTests/*"` → 11 of 11 passed. The same command for `VariantDetailTests` → 3 of 3 passed.
- `yarn vitest run` (from `src/KCC.Web`) → 1451 passed, 2 expected fail.
- `prettier --check` on `Controls.css` → passed.
- Not checked by eye: both ramps and the narrow scrolled track. The change is to horizontal alignment only, which
  neither the ramp nor the track's scroll position affects.

## Deviations from Assessment

- The branch `fix/view-toggle-icons-off-center` stacks on `fix/segmented-slider-no-slide`, which changes the same
  CSS block. The assessment asked for the two fixes to go one after the other.

## Follow-ups

- The recipe detail view toggle has the same fix through the shared CSS. No E2E test pins its icon positions.
