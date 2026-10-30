# Bug Fix: Neighbouring sheets share a tear, and the account page's empty text sits bare on the desk

- **Slug**: pile-tear-follow-ups
- **Fixed**: 2026-10-30
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

The coming-soon sheets on the account page now swap tears 3 and 5 when the last group would match Favorites. On the
variant page, the sibling cards start two tears on when the signed-out reviews end on a 1. The account page's empty pad
is now a full empty-state sheet: glyph, title, hint and a ghost "New recipe" pill to the create flow.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `src/KCC.Web/Features/Pages/Account/AccountView.Component.vue` | modified | Empty-state sheet; `comingSoonTears` swap; `favoritesFlips` counts the empty sheet as one group; `createRecipeUrl` prop |
| `src/KCC.Web/Features/Pages/Account/AccountPageController.cs` | modified | Passes the create recipe page's URL, from the library under the site root |
| `src/KCC.Web/Features/Pages/Account/AccountViewModel.cs` | modified | `CreateRecipeUrl` |
| `src/KCC.Web/Features/Pages/Account/Index.cshtml` | modified | `create-recipe-url` attribute |
| `src/KCC.Web/uSync/v17/Dictionary/account.nocreationshint.config` | added | Hand-written dictionary item `Account.NoCreationsHint` |
| `src/KCC.Web/uSync/v17/Dictionary/account.newrecipe.config` | added | Hand-written dictionary item `Account.NewRecipe` |
| `src/KCC.Web/Features/Components/VariantDetail/reviewsTrailingSign.ts` | modified | `reviewsTrailingTear`, `formTearAfter`, `siblingTearOffset`, `SUMMARY_TEAR` |
| `src/KCC.Web/Features/Components/VariantDetail/VariantReviews.vue` | modified | Emits `trailingTear` after each load; the form's tear and the summary's tear come from the shared module |
| `src/KCC.Web/Features/Components/VariantDetail/VariantSiblings.vue` | modified | `tearOffset` prop |
| `src/KCC.Web/Features/Pages/VariantDetail/VariantDetailView.Component.vue` | modified | Passes `siblingTearOffset` of the reviews' trailing tear to the siblings |
| `tests/KCC.E2ETests/Features/Performance/baseline.ci.json` | modified | The four phone account rows: `clip_path_elements` 24 to 27, `blend_elements` 9 to 10, for the empty sheet's blank, fall and sheet and its wash |
| `tests/KCC.ViteTests/Features/Pages/Account/AccountView.test.ts` | modified | Empty sheet, hint and pill, coming-soon swap, no repeated neighbour tear for 0 to 12 groups |
| `tests/KCC.ViteTests/Features/Components/VariantDetail/reviewsTrailingSign.test.ts` | modified | Trailing tear and sibling offset |
| `tests/KCC.ViteTests/Features/Components/VariantDetail/VariantReviews.test.ts` | modified | `trailingTear` emitted after load, never on the server |
| `tests/KCC.ViteTests/Features/Components/VariantDetail/VariantSiblings.test.ts` | modified | `tearOffset` |
| `tests/KCC.IntegrationTests/Features/Baseline/AccountStringsTests.cs` | modified | Boot test for the two hand-written dictionary items |
| `tests/KCC.IntegrationTests/Features/Pages/AccountPagesTests.cs` | modified | `create-recipe-url` on the account page |
| `tests/KCC.E2ETests/Features/Chrome/PileTests.cs` | modified | Signed out with one review, the first sibling is `kcc-tear-3`; `StubbedReviewsPage` takes a total |

## Tests Added or Updated

- `AccountView.test.ts` › "lays an empty pad on a wide sheet of its own…", "gives an empty pad a hint and a ghost pill…",
  "leaves the pill off an empty pad when the site has no create page", "swaps the coming-soon tears after 2 and 8
  groups", "never tears two neighbours alike… with 0 to 12 recipe groups"; the flip cases moved to 4 and 6 (Favorites)
  and 0, 1 and 3 (Recent activity).
- `reviewsTrailingSign.test.ts` › `reviewsTrailingTear` signed out and in; `siblingTearOffset`.
- `VariantReviews.test.ts` › "emits the tear the section ends on once the reviews load" (4 cases); no `trailingTear` on
  the server.
- `VariantSiblings.test.ts` › "starts the cycle two on when the sheet above ends on a 1".
- `AccountStringsTests.EmptyPadStrings_ImportFromTheBaseline` (3 cases).
- `AccountPagesTests.AccountPage_ListsTheMembersPublishedAndPendingWork` › `create-recipe-url`.
- `PileTests.VariantDetail_SignedOut_OneReview_TearsTheFirstSiblingApart` (375, 1280).

Each was seen to fail before its fix: the vitest cases (19), the two integration tests (2 of 3 string cases, and the
page attribute), and the E2E test at both widths (`But was: 'kcc-slip kcc-slip--fill kcc-recipe kcc-slip--lift
kcc-tear-1'`).

## Local Verification

| Row | Result |
|---|---|
| D1 `dotnet build KitchenCommandCenter.slnx -c Release` | Builds, 0 errors. 8 warnings (16 lines), all `NU1903` for `SixLabors.ImageSharp` 3.1.12, advisory `GHSA-v76p-62qx-wwq2`, which `Directory.Build.props` does not suppress. `origin/main` has the same warnings; this change adds none. |
| D2 `yarn build:all` | Pass |
| D3 `yarn type-check`, `yarn lint`, `yarn format` in `src/KCC.Web` | Pass. Lint reports 6 warnings, all in `Components/Forms/InputField.vue`, `SelectField.vue` and `TextAreaField.vue`, untouched. Format changed one test file's wrapping. |
| D4 `node tests/scripts/run.mjs` | Unit 726/726, Integration 447/447, Web Frontend 2,455 pass and 1 skipped, Admin 17/17, Contributions 8/8. E2E 429/438: 9 failures in `HomePageTests`, `ChromeTests`, `RecipeSearchTests`, `BloomTests` and `BloomArrangementTests` (pages served without their server render, and two 30-minute timeouts) while another worktree's E2E suite ran on the same Mac. Each of those five classes then passed on its own: 5/5, 5/5, 12/12, 18/18, 5/5. The earlier full run, before the hint and pill, failed only `DeterministicGuardTests` on the account rows, which the baseline change answers and which passed in this run. |
| D5 Generated files and hand-written uSync | The two dictionary items are hand-written; `AccountStringsTests.EmptyPadStrings_ImportFromTheBaseline` boots the site and reads them back. Pass. |
| D6 Touched pages in both ramps | `yarn perf:capture pile-fix --page account` captured the account page on a fresh site, member with no recipes, phone and desktop, Chromium and WebKit, light and dark (`tests/results/perf/captures/pile-fix/`). The empty sheet holds glyph, title, hint and pill in both ramps; tears run 1, 2, 3, 5. The variant page's sibling shift was checked by `PileTests` at 375 and 1280, not by eye. |
| D7 `brand-steward` | First review: on brand except the empty state's anatomy (no hint, no ghost button), against kit.md → States → Empty. The owner chose to add both; done. Not re-run after that change. |
| D8, D9 | N/A: no change under `deploy/` or to the images. |
| D10 Docs | No line in `README.md`, `CLAUDE.md` or `docs/` is made untrue. kit.md's empty-state anatomy now holds on the account page. |

## Deviations from Assessment

- **The empty state gained a hint and a ghost "New recipe" pill.** The assessment's preferred remedy (glyph and title
  only, existing copy) broke kit.md's empty-state anatomy, which the brand steward reported. The owner chose the
  alternative the assessment had declined: two hand-written dictionary items (`Account.NoCreationsHint`,
  `Account.NewRecipe`), a `CreateRecipeUrl` from the controller, and the pill shown only when the site has a create
  page. Files outside the assessment's list: `AccountPageController.cs`, `AccountViewModel.cs`, `Index.cshtml`, the two
  uSync files and the two integration test files.
- **The CI performance baseline moved.** `DeterministicGuardTests` holds the account page's element counts to "no
  increase"; the empty sheet adds one sheet. The four phone account rows of `baseline.ci.json` were raised by hand to
  the measured counts rather than re-captured, since element counts do not depend on the machine and a re-capture
  would rewrite every timing row from a local run. `specs/006-performance-sweep/baseline.json` (the local matrix) was
  not re-captured.

## Follow-ups

- `Directory.Build.props`: decide on `GHSA-v76p-62qx-wwq2` (a sixth ImageSharp advisory), which breaks D1's "no
  warning" on `main` as well.
- `VariantReviews.vue`: "No reviews yet" is bare body text on the desk, the same fault fixed here on the account page,
  and the server render shows it under a summary that counts reviews until the client load.
- `VariantReviews.vue`: the review list never sets `aria-busy` while loading (kit.md → States → Loading).
