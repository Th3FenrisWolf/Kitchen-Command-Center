# Bug Assessment: Neighbouring sheets share a tear, and the account page's empty text sits bare on the desk

- **Slug**: pile-tear-follow-ups
- **Created**: 2026-10-30
- **Source**: pasted text (the brand review's follow-ups on PR #100, spec 002)
- **Verdict**: valid
- **Severity**: low

## Report (verbatim or summarized)

The brand review of spec 002 (paper piles) found these on `main`, in files that branch touched, and left them for
follow-up:

1. Account page: the last recipe group and the "Favorites" coming-soon sheet can share `kcc-tear-3`.
2. Variant detail, signed out: the last review and the first sibling card can share `kcc-tear-1`.
3. Account page: "no creations yet" is bare text on the desk, not a sheet.

Two more items from that review are not bugs:

- The recipes empty state's wash lies under its copy. Spec 005 retired "never under running text" and author-placed
  washes, so this is the intended behaviour.
- `CookMode`'s `outline-none` is on the `tabindex="-1"` panel that the focus trap focuses, not on a control. Every
  control inside it keeps the 2px ink focus ring.

## Symptom

Two sheets that lie next to each other in reading order can take the same tear preset, so their edges tear alike, which
the pile forbids. With no recipes, the account page shows its "Nothing on the pad yet." line straight on the desk, where
every other empty state is a sheet.

## Reproduction

1. Sign in as a member with exactly 2 recipe groups (or 8, 14, …). Open `/account` at 375px. The second group and
   "Favorites" stack one after the other, both `kcc-tear-3`.
2. Signed out, open a variant that has exactly 1 review (or 7, 13, …) and at least one sibling variant. After the reviews
   load, the only review and the first sibling card stack one after the other, both `kcc-tear-1`.
3. Sign in as a member with no recipe groups and open `/account`. The "My recipes and variants" section shows a bare
   `<p class="kcc-body">`.

## Suspected Code Paths

- `src/KCC.Web/Features/Pages/Account/AccountView.Component.vue:92` — groups take `listTearFor(index + 1)`, so the last
  of `n` groups takes tear `(n % 6) + 1`.
- `src/KCC.Web/Features/Pages/Account/AccountView.Component.vue:132-133` — "Favorites" is fixed at tear 3 and "Recent
  activity" at tear 5, whatever the last group's tear is.
- `src/KCC.Web/Features/Pages/Account/AccountView.Component.vue:44` — `favoritesFlips` keys Favorites' turn to the group
  count's parity. It assumes nothing sheet-like renders when there are no groups.
- `src/KCC.Web/Features/Pages/Account/AccountView.Component.vue:81` — the bare `NoCreationsYet` paragraph.
- `src/KCC.Web/Features/Components/VariantDetail/VariantReviews.vue:155` — reviews take `listTearFor(i)`, so the last of
  `n` reviews takes tear `((n - 1) % 6) + 1`.
- `src/KCC.Web/Features/Components/VariantDetail/VariantReviews.vue:98-101` — signed in, the form's tear (2, or 3 after a
  tear-2 review) is chosen to clear the first sibling's tear 1. Signed out there is no form, and nothing clears it.
- `src/KCC.Web/Features/Components/VariantDetail/VariantSiblings.vue:28` — siblings always start at `listTearFor(0)`,
  tear 1.
- `src/KCC.Web/Features/Components/VariantDetail/reviewsTrailingSign.ts` — reports the review section's trailing turn
  only, not its trailing tear.
- `src/KCC.Web/Features/Pages/VariantDetail/VariantDetailView.Component.vue:150-158` — wires `trailing-sign` into the
  siblings' `flip`.

## Root Cause Hypothesis

High confidence. Each list picks its tears on its own from `listTearFor`, and the sheets after a list use fixed tears.
Only the turn (`flip`) of the sheet after a list is computed from the list; its tear is not. Spec 002 handled the tear
in one place, the signed-in review form, and left the account page and the signed-out variant page uncovered. The empty
account text predates the pile and was never converted to a sheet.

## Proposed Remediation

**Preferred** (the owner's choices, 2026-10-30):

1. **Favorites and Recent activity swap tears 3 and 5 when the last group is tear 3.** Favorites takes 5 and Recent
   activity takes 3 when `listTearFor(recipeGroups.length)` is 3; otherwise 3 and 5 as today. Both stay odd, so
   `tearTurn` is unchanged and `favoritesFlips` still holds. Neither lands on tear 1, which sits right above Favorites
   under the profile sheet on desktop.
2. **The siblings shift by 2 when the reviews end on tear 1.** `reviewsTrailingSign.ts` gains the review section's
   trailing tear beside its trailing sign (the last loaded review's tear, else the summary's 5, else the form's tear when
   signed in, else `null`). `VariantReviews` emits it, as it emits the sign. `VariantDetailView` passes VariantSiblings a
   tear offset of 2 when that tear is 1, and 0 otherwise. `VariantSiblings` takes the offset and tears each card with
   `listTearFor(index + offset)`. A shift of 2 keeps every tear's parity, so each card's turn and the `flip` logic stay as
   they are. The server renders no reviews, so the server HTML is unchanged and the shift lands after the client load, as
   the flip does today (spec 002 A27).
3. **The empty account section becomes a sheet.** A wide `KccSheet`, `kcc-tear-2`, with the bowl icon and the existing
   `NoCreationsYet` copy as its `kcc-title`, centred like `RecipesEmptyState` and `VariantsEmptyState`, with a stable
   `wash-key` (`account-empty`). No new dictionary items. It reads as one group in the pile: `favoritesFlips` becomes
   `(recipeGroups.length || 1) % 2 === 0`, so Favorites still alternates against it, and tear 2 clears the profile's 1
   and Favorites' 3.

**Alternatives** (considered and declined):

- Favorites takes the first of 3, 5 and 1 that differs from the last group. Tear 1 can land right under the profile
  sheet's tear 1 on desktop.
- Siblings always continue the reviews' tear sequence. Every variant page's sibling tears change, and the flip has to be
  reworked.
- A create button and hint in the empty state. Needs new dictionary items in uSync and a create URL on the view's props.

**Files likely to change**:

- `src/KCC.Web/Features/Pages/Account/AccountView.Component.vue`
- `src/KCC.Web/Features/Components/VariantDetail/reviewsTrailingSign.ts`
- `src/KCC.Web/Features/Components/VariantDetail/VariantReviews.vue`
- `src/KCC.Web/Features/Components/VariantDetail/VariantSiblings.vue`
- `src/KCC.Web/Features/Pages/VariantDetail/VariantDetailView.Component.vue`
- `tests/KCC.ViteTests/Features/Pages/Account/AccountView.test.ts`
- `tests/KCC.ViteTests/Features/Components/VariantDetail/reviewsTrailingSign.test.ts`
- `tests/KCC.ViteTests/Features/Components/VariantDetail/VariantReviews.test.ts`
- `tests/KCC.ViteTests/Features/Components/VariantDetail/VariantSiblings.test.ts`
- `tests/KCC.ViteTests/Features/Pages/VariantDetail/VariantDetailView.test.ts`, if it exists, else the sibling wiring
  test nearest it
- `tests/KCC.E2ETests/Features/Chrome/PileTests.cs`, if the walker gains a tear check

**Tests to add or update** (written first, each seen to fail):

- `AccountView.test.ts`: with 2 groups, the last group is `kcc-tear-3`, Favorites `kcc-tear-5` and Recent activity
  `kcc-tear-3`; with 1 or 3 groups, Favorites 3 and Recent activity 5; with 0 groups, the empty sheet renders as a
  `kcc-slip kcc-tear-2` holding a `kcc-title` with the copy, and Favorites takes no flip.
- `reviewsTrailingSign.test.ts`: the trailing tear for signed out and in, with 0, 1, 2 and 7 loaded reviews, and with
  reviews counted but none loaded.
- `VariantReviews.test.ts`: the component emits the trailing tear after each load.
- `VariantSiblings.test.ts`: with offset 2 the first card is `kcc-tear-3`; with no offset it is `kcc-tear-1`.
- The variant page's wiring: signed out with 1 loaded review, the first sibling is not `kcc-tear-1`.
- Optional: `PileTests`' walker also checks that no two adjacent visible `.kcc-torn` share a `kcc-tear-*` class, on the
  account page with 0 and 2 groups and the variant page signed out with 1 review.

## Risks & Considerations

- Every tear class change moves snapshot-like assertions such as `toContain('<section class="kcc-slip kcc-tear-3 …">')`;
  the tests that pin tears on these pages change with the fix.
- The sibling shift runs only on the client, after the reviews load. Server HTML and hydration stay matched, as for the
  current flip.
- The empty-state sheet adds one sheet with up to four washes to an account page with no recipes. The washes carry no
  filter (spec 006), so the render cost is one more sheet.
- The new `wash-key` must be stable; `keys.test.ts` scans for it.
- The brand steward reviews the change before the pull request, per the constitution's definition of done.

## Open Questions

None. The owner settled the slug and the three remedies on 2026-10-30.
