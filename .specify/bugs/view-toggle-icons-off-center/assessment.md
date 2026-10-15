# Bug Assessment: The grid and list icons sit off centre in the view toggle

- **Slug**: view-toggle-icons-off-center
- **Created**: 2026-10-14
- **Source**: pasted text, with a screenshot of the recipe search toolbar
- **Verdict**: valid
- **Severity**: low

## Report (verbatim or summarized)

> the icons on the grid/list toggle are not centered

The screenshot shows the view toggle at the right of the recipe search toolbar. The grid icon sits left of centre in
its ink fill, and the list icon sits left of centre in its segment.

## Symptom

Each icon in an icon-only `SegmentedControl` hugs the left side of its 36px segment. Expected: each icon sits in the
centre of its segment.

## Reproduction

1. Open the recipe search page.
2. Look at the grid/list toggle at the right of the toolbar.
3. Both icons sit to the left of their segment's centre.

The variant toolbar on a recipe detail page uses the same `variant="icon"` toggle, so it shows the same fault.

## Suspected Code Paths

- `src/KCC.Web/Features/Styles/Torn/Controls.css:111-126` — `.kcc-seg button` is `display: inline-flex` with
  `align-items: center` and no `justify-content`, so the content sits at the start of the button.
- `src/KCC.Web/Features/Components/Recipe/SegmentedControl.vue` — the icon variant adds `w-9 px-0`, which makes the
  button a 36px square with no padding. The 12px icon is narrower than the button, so the start alignment shows.
- `src/KCC.Web/Features/Components/RecipeSearch/RecipeResultsToolbar.vue:33` and
  `src/KCC.Web/Features/Components/RecipeDetail/VariantToolbar.vue:53` — the two icon toggles.

## Root Cause Hypothesis

Confidence: high. Before PR #65 (`d9bcb096`, 2026-09-23), each segment carried the utility `justify-center`. The
Loose Leaf restyle moved the segment's look into `.kcc-seg button` in `Controls.css` and did not carry
`justify-content: center` across. A text segment hugs its label, so the fault shows only on the fixed-width icon
segments.

## Proposed Remediation

**Preferred**: Add `justify-content: center` to `.kcc-seg button` in `Controls.css`. A text segment is as wide as its
label plus padding, so its look does not change. An icon segment centres its icon.

**Alternatives**:
- Add `justify-center` to the icon variant's utilities in `SegmentedControl.vue`. This fixes only this component. A
  `kcc-seg` written by hand elsewhere would keep the fault.

**Files likely to change**:
- `src/KCC.Web/Features/Styles/Torn/Controls.css`
- `tests/KCC.ViteTests/Features/Components/Recipe/SegmentedControl.test.ts` or an E2E test

**Tests to add or update**:
- A layout check that the icon's centre and the segment's centre are within 1px of each other on the recipe search
  view toggle. jsdom has no layout, so this check belongs in the E2E suite with Playwright.

## Risks & Considerations

- The change is one CSS property in the shared kit, so it reaches every `kcc-seg`. A text segment's look does not
  change, because its content fills it.
- Check both ramps and the narrow width where the group scrolls.
- The `segmented-slider-no-slide` bug changes the same component and the same CSS block. Fix the two together or one
  after the other, to keep the diffs apart.

## Open Questions

- None.
