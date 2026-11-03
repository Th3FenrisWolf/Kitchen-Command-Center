# Bug Fix: Five brand findings that predate the specs that found them

- **Slug**: brand-steward-leftovers
- **Fixed**: 2026-11-03
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

A variant with no reviews now lays an empty-state sheet in the summary's place, and the reviews region is `aria-busy`
while a page loads. The variants empty state's hint is body copy, the range thumbs take the kit's focus ring, and
`RecipeCard`'s review count is a `kcc-num`.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `src/KCC.Web/Features/Components/VariantDetail/VariantReviews.vue` | modified | Empty sheet (`reviews-empty`, tear 5, reading bloom, no margin, no button) as the summary's `v-else`; the bare line is gone; `:aria-busy="loading \|\| undefined"` on the region; emits no longer carry `null` |
| `src/KCC.Web/Features/Components/VariantDetail/reviewsTrailingSign.ts` | modified | With no review loaded the section ends on tear 5 turning −, whatever the count; `ReviewsSection.count` and `COOK_NOTES` dropped as unused; return types lose `null` |
| `src/KCC.Web/Features/Pages/VariantDetail/VariantDetailView.Component.vue` | modified | Server defaults stop passing the dropped `count` |
| `src/KCC.Web/uSync/v17/Dictionary/variantdetail.noreviewshint.config` | added | Hand-written, key `a5c77227-19ea-4432-81c5-c4c111196bbf`, `VariantDetail.NoReviewsHint` = "Cook it, then come back and say how it went." `RecipeVariantController` loads the whole `VariantDetail` group, so no registration |
| `src/KCC.Web/Features/Styles/FontAwesome/iconSafelist.ts` | modified | `comments`, or the glyph renders blank |
| `src/KCC.Web/Features/Components/RecipeDetail/VariantsEmptyState.vue` | modified | `TryDifferentFilter` is `kcc-body max-w-[60ch]` |
| `src/KCC.Web/Features/Styles/Torn/Controls.css` | modified | Focused thumb rings in both engines (separate `-webkit-` and `-moz-` rules), a 0.2s fade gated like the outline fade, and a `CanvasText` outline under forced colours |
| `src/KCC.Web/Features/Components/Recipe/RecipeCard.vue` | modified | Review count in `kcc-num` |
| `docs/brand/kit.md` | modified | States > Focus names the range ring; States > Empty allows a buttonless sheet whose way on sits right below it; Motion and Enforcement rows updated |
| `tests/KCC.IntegrationTests/Features/Baseline/VariantDetailStringsTests.cs` | added | Boot test for the hand-written dictionary item (D5) |
| `tests/KCC.ViteTests/...` | updated | See below |

## Tests Added or Updated

Each was seen to fail before its fix, except the integration strings test, which was written after its config.

- `VariantReviews.test.ts` › "VariantReviews empty sheet" (6 tests) — the sheet's tear, width, no margin, glyph,
  title, hint and no button; none when the count is above 0 before a load; one blank each for it and the form; its −
  turn and the form's + turn under it.
- `VariantReviews.test.ts` › "VariantReviews region while a page loads" — `aria-busy="true"` while the request is
  pending, gone once it settles.
- `reviewsTrailingSign.test.ts` — before any review loads the section ends on −1 and tear 5 signed out, +1 signed in.
- `VariantDetailView.test.ts` — the siblings' flip with no reviews, signed out and in.
- `VariantsEmptyState.test.ts` — hint in body copy, no hand note.
- `RecipeCard.test.ts` — the count is a Sono number.
- `kitStates.test.ts` — each engine's focused thumb ring, its gated fade, the forced-colours outline.
- `kitClasses.test.ts` — the two-ring value joins the allowed box-shadow devices.
- `contrast.test.ts` — `focus` on `paper-2`, both ramps.
- `VariantDetailStringsTests.EmptyReviewsStrings_ImportFromTheBaseline` — both empty-reviews strings import.

## Local Verification

| Row | Result |
|---|---|
| D1 `dotnet build KitchenCommandCenter.slnx -c Release` | 0 warnings, 0 errors |
| D2 `yarn build:all` | pass |
| D3 `yarn type-check`, `yarn lint`, `yarn format` | pass; lint 0 errors, 6 warnings already present in files not touched |
| D4 `node tests/scripts/run.mjs`, no-op `open` on `PATH` | First run: 4,108 passed, 0 failed, 1 skipped across 6 suites. Rerun on the final tree, after the review follow-ups: 4,107 passed, 0 failed, 1 skipped (E2E 439/439, integration 449/449). No class needed a rerun |
| D5 hand-written uSync | `VariantDetailStringsTests` passes in the integration suite |
| D6 both ramps | A throwaway E2E class (deleted) shot the seeded site in Chromium: the empty reviews sheet at 375 and 1280, the variants empty sheet, the range thumbs focused by keyboard, both ramps. Tears down the unreviewed variant run 4, 5, 1, 2, 3 with alternating turns; no `aria-busy` after load. Forced colours: the focused thumb draws a `CanvasText` ring. A mouse click focuses a thumb without `:focus-visible`, so no ring |
| D7 `brand-steward` | On brand. Its two gaps (kit.md's Enforcement row, the `focus`/`paper-2` contrast pair) and the dead `count` field are fixed; the buttonless empty sheet is now written into kit.md |
| D8, D9 | N/A |
| D10 | kit.md corrected as above |
| Review loop | Not run: no pull request opens from this branch. The owner's close-out branch opens one PR, where the review runs |

The CI perf baseline does not move: its variant (`fluffy-buttermilk-pancakes/classic-stack`) has two seeded reviews, so
it shows the summary, not the empty sheet.

## Deviations from Assessment

- `ReviewsSection.count` and `COOK_NOTES` were removed, and `iconSafelist.ts`, `kitClasses.test.ts`,
  `contrast.test.ts` and `VariantDetailView.Component.vue` changed, beyond the listed files. All follow from the fix.
- The thumb's fade sits under `prefers-reduced-motion: no-preference` and the `:root:focus-within` gate, because the
  site's reduced-motion and ramp-switch rules reach no thumb pseudo-element.
- kit.md's Empty state gains a sentence for the buttonless sheet the owner chose.

## Follow-ups

- Under forced colours an unfocused range thumb paints nothing (its ink fill is forced away). This predates the fix.
- Signed out, "Log in to write a review." is bare text and not a link, below the new sheet.
