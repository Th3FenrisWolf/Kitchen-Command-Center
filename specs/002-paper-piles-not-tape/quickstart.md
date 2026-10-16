# Quickstart: validate Paper Piles, Not Tape

How to prove the feature works. The tests themselves are written by the tasks. The pile's markup and classes are in
[contracts/pile-markup.md](contracts/pile-markup.md).

## Prerequisites

- `FONTAWESOME_NPM_AUTH_TOKEN`, `KCC_E2E_MEMBER_USERNAME` and `KCC_E2E_MEMBER_PASSWORD` set (constitution gate 1).
- The .NET SDK from `global.json`, Node 24 with Yarn 1, the Playwright browsers.

## 1. The generator and the kit

From `src/KCC.Web`:

```sh
yarn tears
git diff --stat Features/Styles/Torn/Tears.css   # changed only with tears.ts
yarn test tears pile kitClasses motion retiredTokens contrast
```

Expected: all pass. `Tears.css` declares `--tear-hero-2`, and each sheet class writes `--blank-tear` and `--blank-dir`.

## 2. The surfaces

```sh
yarn test KccSheet RecipeCard RecipeCardRow FeaturedRecipeCard DetailHero VariantGrid RecipesEmptyState \
  VariantsEmptyState SmallHero Card Stacker AppHeader PadCards CreateRecipeView AddVariantView RecipeSearchHeader \
  RecipeSearchView RecipeFilters RecipeDetailView VariantDetailView VariantIngredients VariantInstructions \
  VariantNutrition VariantCookNotes VariantReviews reviewsTrailingSign AccountView LoginView ComingSoonSection \
  RecipeWizard stepFlip StartStep BasicsStep IngredientsStep InstructionsStep PhotoStep MatchStep VariantStep \
  ReviewStep ReadingState SubmittedSheet VariantList VariantSiblings AccountSettingsView PhotoStrip resultsRegion
yarn type-check && yarn lint && yarn format
```

Expected: every sheet renders one `kcc-blank`, no sheet renders a `kcc-tape`, the tiles keep theirs, the surfaces
wider than 700px (research R5) carry `kcc-slip--wide` and no other does, the flips of research R12 are where they
belong, the list row's tile sits in a `kcc-tilewrap--flow` with its strip, the photo tiles have neither blank nor tape,
and the pad renders one page.

## 3. The build and every suite

From the repository root:

```sh
dotnet build KitchenCommandCenter.slnx -c Release
dotnet build KitchenCommandCenter.slnx   # Debug: SiteProcess.cs starts bin/<configuration>/net10.0/KCC.Web.dll
yarn build:all
node tests/scripts/run.mjs        # put a no-op `open` first on PATH for an unattended run
```

Expected: no warning, and all six suites green, including the Razor page tests, the Home tear cycle, the overlay
assertions in `CookModeTests`, `RecipeImportTests`, `RecipeWizardReviewTests` and `RecipeImportLeaveTests`, the loaded
reviews in `VariantReviewsTests`, and `PileTests`, whose walker checks
on every page type at 375px and 1280px that the blanks alternate in reading order, that each blank is as tall and wide
as its sheet, and that no page scrolls sideways. The walker's states come from the seed ("Bare Cupboard Snack Board" for a
recipe with no variants) and from a stubbed reviews endpoint for "load more", and it waits for the reviews list before it walks a
variant page with reviews. The SSR tests cover the states of research R12 that the server renders, and the E2E tests
cover first the states only a browser reaches.

## 4. Both ramps, by eye (D6)

Run the site (`dotnet run` in `src/KCC.Web`, https://localhost:58671), seed recipes with the Development seeder
(`POST /api/dev/seed-recipes`), and open each page in the light and the dark ramp at 1280px and at a phone width:

| Page | Check |
|---|---|
| `/recipes/` grid and list view | each card and row on one blank, every blank on the page turned the other way from the one before, with the filters open and closed on a phone; each tile keeps or gains its tape; hover lifts the card and not its blank; the search header, the featured card and each list row turn their blank 0.5°, the cards 1.5° |
| a recipe detail page | the hero on a `tear-hero-2` blank with no strip at its top centre; each sheet below on its blank |
| a variant detail page and cook mode | the crisp sheets square, their blanks turned; ingredients at 1.5°, instructions, nutrition, cook notes and reviews at 0.5°; cook mode's blank turns 0.5° and shows where the screen leaves an edge; short ingredients beside long instructions keep a blank no taller than the sheet; at 375px nothing scrolls sideways |
| Home | stacker and rich-text sheets (Razor) and the card grid on blanks; a stacked card steps back with its blank |
| the header pad | one page under the top sheet, centred; Recipes and My Kitchen cards slide out between the two; the pad tucks and returns with its page |
| the registration complete page, a 404 | the Razor sheets on blanks, no tape |
| the recipe wizard's submitted sheet, an empty search | piled, no tape |
| the recipe wizard's photo viewer (open a photo from the photo step) | the crisp sheet square on a blank turned 0.5°, the blank showing where the screen leaves an edge; no tape; the leave dialog's sheet on its blank |

Compare against `mockups/pile-light.jpg` and `mockups/pile-dark.jpg`. Then run the `brand-steward` agent over the
diff (D7).

## 5. The render cost (SC-005)

With the site running and seeded, run a throwaway Playwright script (kept in the session's scratch space, not
committed) once on `main` and once on the branch, on the same machine:

1. Chromium at 1280 by 800, CPU throttled 4× with `Emulation.setCPUThrottlingRate`.
2. Open `/recipes/`, wait for the grid, then scroll to the bottom and back in fixed wheel steps.
3. Hover across each card of the first row.
4. Record frame times from `requestAnimationFrame` deltas and `long-animation-frame` entries.
5. Five runs per ramp per branch.

For each run, take the p95 frame time and the count of frames over 50ms; for each branch and ramp, take the median of
the five runs. Pass: the branch's median p95 is within 10% of `main`'s, and its median count of frames over 50ms is at
most `main`'s plus one. Put the table in the pull request description. On a fail, stop and ask the owner before trading any part of the blank's look
for speed (research R8).
