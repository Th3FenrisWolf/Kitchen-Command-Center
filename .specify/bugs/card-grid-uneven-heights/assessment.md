# Bug Assessment: Cards in a grid row have different heights

- **Slug**: card-grid-uneven-heights
- **Created**: 2026-10-14
- **Source**: pasted text, with screenshots of the recipe search grid and the recipe detail variant grid
- **Verdict**: valid
- **Severity**: low

## Report (verbatim or summarized)

> all the cards in teh recipe search grid should be the same hieght per row so theres no gaps in the grid. the same
> goes for variant list grid on recipe detail page

The first screenshot shows the recipe search grid. In the second row, Big-Pot Chili has three rows of badges and is
much taller than Blueberry Muffins, which has one. The second screenshot shows the variant grid on a recipe detail
page: Whole-Wheat Yogurt, Bakery Crumb-Top and the Add Variant sheet each end at a different height.

## Symptom

Each card is as tall as its own content, so the bottom edges in a row do not line up and the desk shows through under
the shorter cards. Expected: every card in a row is as tall as the tallest card in that row.

## Reproduction

1. Open the recipe search page in the grid view, wide enough for two or more columns.
2. Find a row where one recipe has more badges or a longer name than the others.
3. The shorter cards end above the taller one.
4. Open a recipe detail page with two or more variants. The variant cards and the Add Variant sheet end at different
   heights.

## Suspected Code Paths

- `src/KCC.Web/Features/Components/Recipe/RecipeCard.vue` — the `kcc-slip` `<article>` stretches to the row height,
  because a grid item stretches by default. Its `kcc-torn` link and its `kcc-sheet` are auto height, so the paper
  stops at its content and the stretched slip below it is empty.
- `src/KCC.Web/Features/Styles/Torn/Kit.css:42-49` — `.kcc-slip--fill` already passes the slip's height down through
  `kcc-torn` into `kcc-sheet`. `Widgets/Card/Card.Component.vue:30` uses it. The recipe card does not.
- `src/KCC.Web/Features/Components/RecipeDetail/VariantGrid.vue:26-36` — the Add Variant sheet is a separate
  `kcc-slip` with the same auto-height paper.
- The three grids that hold `RecipeCard`, all `grid-cols-[repeat(auto-fit,minmax(min(300px,100%),1fr))]`:
  - `src/KCC.Web/Features/Pages/RecipeSearch/RecipeSearchView.Component.vue:156-165`
  - `src/KCC.Web/Features/Components/RecipeDetail/VariantGrid.vue:18`
  - `src/KCC.Web/Features/Components/VariantDetail/VariantSiblings.vue:22`

## Root Cause Hypothesis

Confidence: high. The grid row is already as tall as its tallest card, and each slip stretches to it. The paper is a
child of the slip with auto height, so it does not fill the slip. The torn edge and the shadow follow the paper, and
the empty part of the slip shows the desk. The tear presets in `Tears.css` combine percentages with pixel offsets, so
a taller sheet keeps its torn edge.

## Owner decisions (2026-10-14)

- **Q1**: the extra height shows as blank ruled paper under the last item. The content stays at the top, and the meta
  line and the badges keep their places on the rule.
- **Q2**: on the Add Variant sheet, the plus and the label sit in the vertical middle of the taller sheet.
- **Q3**: the fix covers all three grids: recipe search, the variant grid on recipe detail and the sibling grid on
  variant detail.

## Proposed Remediation

**Preferred**: Use the kit's existing modifier.

- Add `kcc-slip--fill` to the `<article>` in `RecipeCard.vue`. The slip, the torn link and the sheet then take the
  row height, and the content stays at the top of the sheet. This one change covers all three grids, because they
  all render `RecipeCard`.
- Add `kcc-slip--fill` to the Add Variant slip in `VariantGrid.vue`, and add `content-center` to its sheet, which is
  already a `grid`, so the plus and the label sit in the middle.
- Check that no grid holding a `RecipeCard` sets `items-start` or another `align-items` value that stops the stretch.
  None does today.

**Alternatives**:
- `grid-auto-rows: 1fr` makes every row as tall as the tallest card on the page. The report asks for equal heights per
  row, so this adds blank paper to every short row.
- A flex column on the sheet with the badges pushed down. The owner chose blank paper below in Q1.

**Files likely to change**:
- `src/KCC.Web/Features/Components/Recipe/RecipeCard.vue`
- `src/KCC.Web/Features/Components/RecipeDetail/VariantGrid.vue`
- `docs/brand/kit.md`, if its `kcc-slip--fill` entry lists where the modifier is used
- `tests/KCC.ViteTests/Features/Components/Recipe/RecipeCard.test.ts`
- An E2E test in `tests/KCC.E2ETests`

**Tests to add or update**:
- A Vitest SSR test that `RecipeCard` renders `kcc-slip--fill` on its slip, and that the Add Variant slip in
  `VariantGrid` does too.
- An E2E test on recipe search with the test seeder's recipes: at a width with two or more columns, every
  `.kcc-sheet` in the first grid row has the same height within 1px. jsdom has no layout, so the height check needs
  Playwright.
- The same E2E check on a recipe detail page with two or more variants, including the Add Variant sheet.

## Risks & Considerations

- `RecipeCard` appears only inside the three grids today. If a later page puts it in a flex row or a block flow, the
  fill modifier has no definite height to pass down and does nothing harmful.
- A card that lifts on hover lifts the whole stretched sheet. That is the intended look.
- The list view uses `RecipeCardRow` in a single column, so it is out of scope.
- Check both ramps and one, two and three columns. Ask the `brand-steward` agent to review the result.
- Both bundles build together: `yarn build:all`.

## Open Questions

- None.
