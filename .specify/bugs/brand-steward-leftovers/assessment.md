# Bug Assessment: Five brand findings that predate the specs that found them

- **Slug**: brand-steward-leftovers
- **Created**: 2026-11-03
- **Source**: pasted text (the brand steward's notes in the 004 close-out, PR #113 "Left for the owner", and the
  variant reviews findings carried with them)
- **Verdict**: valid (findings 1 to 4); finding 5 valid for one count, the rest not a bug
- **Severity**: low

## Report (verbatim or summarized)

The brand steward found these on `main`, in files a spec touched, and left them because they predate that spec:

1. `VariantReviews.vue`: "No reviews yet" is bare text on the desk, not an empty-state sheet (`docs/brand/kit.md`,
   States, Empty). The account page's empty pad (#115) is the pattern to follow.
2. `VariantReviews.vue`: the review list never sets `aria-busy` while it loads.
3. `VariantsEmptyState` puts its hint in a hand note.
4. The `kcc-range` thumbs have no focus ring.
5. "A few numbers sit outside `kcc-num`" (004 close-out, unnamed).

## Symptom

1. A variant with no reviews shows "No reviews yet." as a bare `p.kcc-body` on the desk. Every other empty state is a
   sheet with a glyph, a title, a hint and an action. While the reviews of a variant that has some are still loading,
   the same line shows too, because it renders whenever no review has loaded.
2. During a load, the reviews region announces nothing busy. The kit's Loading state asks for `aria-busy` on the region.
3. "Try a different filter" sits in `kcc-hand`, where the kit's Empty state asks for a `kcc-body` hint.
4. Tabbing to the search page's time-range slider shows no ring on either thumb. The kit asks for a ring on every
   focusable, and says so for the range as a known gap ("The `kcc-range` thumbs have no focus ring yet.").
5. A fresh brand-steward pass over the 66 Vue and Razor files 004 touched found no template-bound number outside Sono.
   One count, `RecipeCard`'s review count, is Sono only through its `kcc-stat` parent, where `DetailHero` and
   `FeaturedRecipeCard` wrap the same count in `kcc-num`. The other digits outside Sono come from content strings
   (step text, recipe names, member prose, CMS text).

## Reproduction

1. Open a variant with no reviews, signed in or out. Under "Ratings & reviews" the line "No reviews yet." sits on the
   desk.
2. Open a variant with reviews and throttle the network. Before the list arrives, "No reviews yet." shows, and the
   region carries no `aria-busy`.
3. Open a recipe, filter its variants to none. The hint is italic APCasual.
4. Open `/recipes`, tab to the time slider's thumbs. No ring shows, in either ramp.
5. Open a recipe card with reviews. The `· 28` count is a bare text node in `span.text-ink-soft`.

## Suspected Code Paths

- `src/KCC.Web/Features/Components/VariantDetail/VariantReviews.vue:170` — `<ResourceString v-else for="NoReviewsYet"
  as="p" class="kcc-body" />`, the `v-else` of `reviews.length`, so it shows with no review loaded whatever the count.
- `src/KCC.Web/Features/Components/VariantDetail/VariantReviews.vue:45-48` — `loading` is tracked and never rendered.
- `src/KCC.Web/Features/Components/VariantDetail/reviewsTrailingSign.ts` — the section's trailing turn and tear assume
  nothing renders when there are no reviews (`count > 0 ? SUMMARY : null`).
- `src/KCC.Web/Features/Components/RecipeDetail/VariantsEmptyState.vue:14` — `TryDifferentFilter` as `kcc-hand`.
- `src/KCC.Web/Features/Styles/Torn/Controls.css:358-394` — `.kcc-range:focus { outline: none }`, and the thumbs carry
  only a paper ring, with no focus state.
- `src/KCC.Web/Features/Components/Recipe/RecipeCard.vue:50` — the bare review count.
- `docs/brand/kit.md:597` — records the range gap.

## Root Cause Hypothesis

High confidence. Each surface predates the rule it breaks: the reviews section was built before the Empty and Loading
states were written into the kit, the variants empty state used the hand note before the kit settled on a `kcc-body`
hint, and the range thumbs are pseudo-elements, which the global `:focus-visible` outline cannot reach, so the input's
own outline was turned off with nothing put in its place.

## Proposed Remediation

**Preferred** (the owner's choices, 2026-11-03):

1. **The empty reviews section becomes a sheet in the summary's place.** When the review count is 0 the section shows
   a wide `KccSheet`, `wash-key="reviews-empty"`, `bloom="reading"`, on the summary's tear 5 (`SUMMARY_TEAR`), with a
   duotone glyph in `ink-soft` at `kcc-icon-lg`, `NoReviewsYet` as its `kcc-title` and a new `NoReviewsHint` in
   `kcc-body`: "Cook it, then come back and say how it went." No button: signed in, the review form sits right below;
   signed out, the "Log in to review" line stays below it. The summary and the empty sheet are exclusive (`count > 0`
   or not), so with no review loaded the section always ends on tear 5, turning −. `reviewsTrailingSign` and
   `reviewsTrailingTear` lose their `null` case for a loaded section, and the form's turn follows from it. Tear 5 clears
   cook notes' 4, the form's 2 and the first sibling's 1. The count is known on the server, so the server HTML already
   holds the sheet and nothing flashes while a non-empty list loads.
2. **The reviews region takes `aria-busy` while a page loads.** The wrapper around the summary, list and form sets
   `aria-busy="true"` while `loading`, and drops the attribute otherwise. No spinner.
3. **The variants empty hint is `kcc-body`.**
4. **The range thumbs take the kit's ring.** On `.kcc-range:focus-visible`, each thumb pseudo-element adds a 2px ink
   ring outside its 2px paper ring (`box-shadow: 0 0 0 2px paper-2, 0 0 0 4px focus`): 2px of ink, 2px out, as the
   global outline. The box-shadow transitions over 0.2s, as the ring fades in elsewhere. Under `forced-colors: active`,
   which paints no box-shadow, the focused thumb takes a 2px `CanvasText` outline, 2px out. `kit.md` drops "no focus
   ring yet" and names the range's ring under Focus.
5. **`RecipeCard`'s review count takes `kcc-num`.** Content digits keep their own face.

**Alternatives** (considered and declined):

- A log-in button in the empty reviews sheet (needs a login URL passed to `VariantReviews`).
- Dropping the line, since the kick above already reads "0 Reviews".
- A spinner beside a `kcc-kick` while loading.
- A digit splitter (`PadLines`' `LineLabel`) over step text and names.

**Files likely to change**:

- `src/KCC.Web/Features/Components/VariantDetail/VariantReviews.vue`
- `src/KCC.Web/Features/Components/VariantDetail/reviewsTrailingSign.ts`
- `src/KCC.Web/uSync/v17/Dictionary/variantdetail.noreviewshint.config` (new, hand-written)
- `src/KCC.Web/Features/Components/RecipeDetail/VariantsEmptyState.vue`
- `src/KCC.Web/Features/Styles/Torn/Controls.css`
- `src/KCC.Web/Features/Components/Recipe/RecipeCard.vue`
- `docs/brand/kit.md`
- `tests/KCC.ViteTests/Features/Components/VariantDetail/VariantReviews.test.ts`
- `tests/KCC.ViteTests/Features/Components/VariantDetail/reviewsTrailingSign.test.ts`
- `tests/KCC.ViteTests/Features/Components/RecipeDetail/VariantsEmptyState.test.ts`
- `tests/KCC.ViteTests/Features/Components/Recipe/RecipeCard.test.ts`
- a CSS contract test over `Controls.css` for the range ring
- `tests/KCC.IntegrationTests/Features/Baseline/*StringsTests.cs` for the new dictionary item
- `tests/KCC.E2ETests/Features/Performance/baseline.ci.json`, if the seeded variant has no reviews

**Tests to add or update** (written first, each seen to fail):

- `VariantReviews.test.ts`: with count 0, an empty sheet on `kcc-tear-5` holds the glyph, `NoReviewsYet` as
  `kcc-title` and `NoReviewsHint` as `kcc-body`, and no bare `p.kcc-body` for it; with a count and no review loaded, no
  empty sheet and no "No reviews yet"; the region carries `aria-busy="true"` while the request is pending and drops it
  once it settles.
- `reviewsTrailingSign.test.ts`: with no reviews, signed out ends on −1 and tear 5; signed in, the form turns +.
- `VariantsEmptyState.test.ts`: the hint is `kcc-body`, and the sheet holds no `kcc-hand`.
- A CSS test: `Controls.css` rings the focused range thumbs in both engines and under forced colours.
- `RecipeCard.test.ts`: the review count sits in `kcc-num`.
- The integration strings test: `VariantDetail.NoReviewsHint` imports from the baseline.

## Risks & Considerations

- Signed out with no reviews, the siblings' flip moves from `null` to −1 (they now follow a sheet). Signed in, the form
  turns + instead of −. Tests that pin those turns change with the fix.
- The empty sheet adds one sheet with its washes to every variant page with no reviews. If the perf case's seeded
  variant has none, the CI baseline's variant rows take the new sheet's elements, as #115 did for the account rows.
- The new `wash-key` must be stable; `keys.test.ts` scans for it.
- A box-shadow ring on the thumb is printed, not torn, like the paper ring it extends, so it does not break the
  no-stroke rule for sheets.
- Both ramps: the ink ring over `paper-2` and the marker fill is the same ring as every other focusable.

## Open Questions

None. The owner settled the remedies on 2026-11-03.
