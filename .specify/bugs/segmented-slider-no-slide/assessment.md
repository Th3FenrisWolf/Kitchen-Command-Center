# Bug Assessment: The segmented control jumps between options instead of sliding

- **Slug**: segmented-slider-no-slide
- **Created**: 2026-10-14
- **Source**: pasted text, with a screenshot of the recipe search toolbar
- **Verdict**: valid
- **Severity**: low

## Report (verbatim or summarized)

> my slider component used to slide between options, it now just jumps, we need to bring back the slide.

The screenshot shows the recipe search toolbar: `SORT` with `RELEVANT` (selected, ink fill), `TOP RATED`, `VARIANTS`
and `RECENT`, and the grid/list view toggle beside it.

## Symptom

When the owner picks another segment, the ink fill disappears from the old segment and appears on the new one in the
same frame. Before, a fill slid from the old segment to the new one.

## Reproduction

1. Open the recipe search page.
2. In the sort group, click `TOP RATED`, then `RECENT`.
3. The ink fill jumps to each new segment with no movement between them.

The same happens on every `SegmentedControl`: the recipe search sort and view toggles, the variant toolbar's sort and
view toggles on a recipe detail page, and the toggle on the account settings page.

## Suspected Code Paths

- `src/KCC.Web/Features/Components/Recipe/SegmentedControl.vue` — renders only the buttons. The checked button
  carries the fill through `aria-checked`. No element moves.
- `src/KCC.Web/Features/Styles/Torn/Controls.css:140-144` — `.kcc-seg button[aria-checked='true']` paints the ink
  fill on the button itself, so a change of selection repaints two buttons.
- `tests/KCC.ViteTests/Features/Components/Recipe/SegmentedControl.test.ts:38-42` — `carries no sliding thumb or
  Softbound hook` asserts that the SSR output has no `seg-thumb`. It locks the removal in.
- `docs/brand/kit.md:206` and `:252` — the kit contract says "pressed = ink fill". The Motion table
  (`docs/brand/kit.md:267`) lists no segment motion.

## Root Cause Hypothesis

A planned removal, not an accident. Confidence: high. PR #65 (`d9bcb096`, 2026-09-23, "Overhaul Theme into a More
Sophisticated Notebook") ran Task 28 of the Loose Leaf plan, which said: "Remove the sliding thumb element and its
transition if it exists; the pressed fill is the state." Before that commit, `SegmentedControl.vue` drew an
`aria-hidden` `<span class="seg-thumb">` behind the buttons. It measured the checked button and moved the span with
`transform`, `width` and `height` over 260ms. The commit replaced that with the static `kcc-seg` fill. The owner now
wants the motion back.

The old implementation, at `d9bcb096^:src/KCC.Web/Features/Components/Recipe/SegmentedControl.vue`, did these things:

- It measured the checked button with `getBoundingClientRect` and set the thumb's `width`, `height` and `transform`.
- It kept the transition off for the first placement, so the thumb did not slide in from the left on load.
- The checked button carried its own fill until the client mounted, so SSR and no-JS output showed the selection.
- A `ResizeObserver` and `document.fonts.ready` placed the thumb again without animation after a reflow.

## Proposed Remediation

**Preferred**: Bring back a measured thumb in the Loose Leaf look.

- `SegmentedControl.vue` renders an `aria-hidden` thumb element as the first child of the `kcc-seg` track. It places
  the thumb over the checked button from `offsetLeft` and `offsetWidth`, which include the track's scroll position.
  The old `getBoundingClientRect` difference does not, and `.kcc-seg` now scrolls horizontally when it is narrow.
- Keep the old load behavior: no transition for the first placement, a new placement without animation on resize and
  when the fonts load, and the button's own fill until the client mounts.
- The thumb's look and its transition go in `Controls.css` under `@layer components`, as a `kcc-seg` child class. The
  thumb is an ink pill. Once the thumb is ready, the checked button keeps `color: var(--color-paper)` and drops its
  own background. The buttons sit above the thumb.
- The transition uses the kit standard, 300ms (`--default-transition-duration`), on `transform` and `width`. The
  reduced-motion rule in `Main.css` stills it, and `data-theme-switching` freezes it for a ramp switch.
- Add a row for the segment slide to the Motion table in `docs/brand/kit.md`, and change "pressed = ink fill" at
  `kit.md:206` and `:252` to describe the thumb.

**Alternatives**:
- CSS anchor positioning moves the thumb with no script. Browser support is not complete, and the SSR fallback is
  the same. It is less proven here than the old measured thumb.
- Animate only the fill colour of the buttons. This needs no new element, but the fill fades in place and does not
  slide, so it does not fix the report.

**Files likely to change**:
- `src/KCC.Web/Features/Components/Recipe/SegmentedControl.vue`
- `src/KCC.Web/Features/Styles/Torn/Controls.css`
- `docs/brand/kit.md`
- `tests/KCC.ViteTests/Features/Components/Recipe/SegmentedControl.test.ts`

**Tests to add or update**:
- Replace `carries no sliding thumb or Softbound hook` with a test that the SSR output has an `aria-hidden` thumb, and
  that the checked button carries the fill before mount. Keep the `expectNoRetiredMarkup` check.
- A mounted test: after a change of the model, the thumb's `transform` and `width` match the new checked button.
- A mounted test: the first placement has no transition.
- An E2E check on the recipe search toolbar that a sort click moves the thumb, if a component test cannot see the
  layout. jsdom has no layout, so `offsetLeft` is 0 there. The fix may need Playwright for the position check.

## Risks & Considerations

- The fix brings back the measured thumb that the Loose Leaf plan removed. The owner asked for it, so the brand docs
  change with the code. Ask the `brand-steward` agent to review the result.
- jsdom has no layout. A component test can check the wiring but not the position.
- The control scrolls horizontally when it is narrow. The thumb must move with the scrolled buttons.
- The account settings toggle switches the light and dark ramp. `applyRamp` freezes transitions for one frame, so the
  thumb there will not slide on that toggle. That is the kit rule, not a defect.
- Both bundles build together: `yarn build:all`.

## Open Questions

- None. The owner asked for the slide back. The approach above follows the old implementation and the kit's 300ms
  standard.
