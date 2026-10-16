# Bug Fix: The segmented control jumps between options instead of sliding

- **Slug**: segmented-slider-no-slide
- **Fixed**: 2026-10-15
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

`SegmentedControl` draws an `aria-hidden` ink thumb again, placed over the checked button from `offsetLeft` and
`offsetWidth`, and the thumb slides over 300ms when the selection changes. The kit contract in `docs/brand/kit.md`
describes the thumb and its motion.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `src/KCC.Web/Features/Components/Recipe/SegmentedControl.vue` | modified | thumb element first in the track; `place()` measures the checked button; no slide on mount, resize, fonts ready or a label change; `kcc-seg--sliding` only once mounted and an option is checked |
| `src/KCC.Web/Features/Styles/Torn/Controls.css` | modified | `kcc-seg` is `position: relative`; `kcc-seg-thumb`, `kcc-seg-thumb--glide`, `kcc-seg--sliding`; the checked button drops its own fill once sliding; label colour fades over 300ms; `:first-child`/`:last-child` padding rules now `:first-of-type`/`:last-of-type`, because the thumb is the first child |
| `docs/brand/kit.md` | modified | `kcc-seg` row, the Pressed state line, a new Motion table row |
| `tests/KCC.ViteTests/Features/Components/Recipe/SegmentedControl.test.ts` | modified | `carries no sliding thumb or Softbound hook` replaced by two SSR tests |
| `tests/KCC.E2ETests/Features/RecipeSearch/RecipeSearchTests.cs` | added test | the thumb sits over the clicked sort segment |

## Diff Highlights

```ts
function place(slide: boolean) {
  const button = buttons()[checkedIndex.value]
  if (!button) {
    return
  }
  thumb.value = { width: `${button.offsetWidth}px`, transform: `translateX(${button.offsetLeft}px)` }
  if (!slide) {
    glide.value = false
    nextTick(() => {
      thumbEl.value?.getBoundingClientRect()
      glide.value = true
    })
  }
}
```

```css
.kcc-seg-thumb--glide {
  transition-property: transform, width;
}

.kcc-seg--sliding button[aria-checked='true'] {
  background: none;
}
```

## Tests Added or Updated

- `SegmentedControl.test.ts` › `renders an aria-hidden thumb first in the track`: the SSR output has the thumb as
  the track's first child, with no retired markup.
- `SegmentedControl.test.ts` › `leaves the fill on the checked button until the client places the thumb`: the SSR
  track has no `kcc-seg--sliding` and the thumb has no `kcc-seg-thumb--glide`, so SSR and no-JS output keep the
  button's own fill and the first placement does not slide.
- `RecipeSearchTests.Sort_thumb_slides_to_the_clicked_segment`: on `/recipes` the visible thumb sits over
  `RELEVANT`; after a click on `TOP RATED` it transitions `transform` over 0.3s, the checked button has no
  background, and the thumb comes to rest over `TOP RATED`.

## Local Verification

- `yarn vitest run` (from `src/KCC.Web`) → 1451 passed, 2 expected fail. The two new SSR tests failed before the
  component change.
- `yarn build:all` → passed. `yarn type-check` → passed. `prettier --check` on the changed files → passed.
- `dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj -- --treenode-filter "/*/*/RecipeSearchTests/*"`
  → 9 of 9 passed.
- The same E2E test against the `HEAD` component and `Controls.css`, rebuilt → failed with a 30s timeout, as
  expected. The fix was then restored and rebuilt.
- `brand-steward` review → no violations. Its two notes (label colour mid-slide, the label-change case missing from
  the Motion row) are applied.
- Not checked by eye: the dark ramp, a narrow scrolled track, and the account settings toggle.

## Deviations from Assessment

- The assessment asked for mounted component tests of the model change and the first placement. The Vite tests run
  in the `node` environment with no DOM library installed, and the assessment allowed no new dependency. The E2E
  test covers the real placement and the transition instead, and the SSR test covers the no-glide start.
- The label colour fade on `.kcc-seg--sliding button` is an addition from the brand review. It keeps the focus
  ring's own `outline-color 0.2s, outline-offset 0.2s`.

## Follow-ups

- `yarn lint` finds no ESLint config in `src/KCC.Web`, so lint did not run on the changed files.
- In `forced-colors: active` the thumb's fill is forced to `Canvas`, so high contrast mode shows no checked segment.
  This was true before the fix as well.
