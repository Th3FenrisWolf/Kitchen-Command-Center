# Implementation Plan: Paper Piles, Not Tape

**Branch**: `002-paper-piles-not-tape` | **Date**: 2026-10-16 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/002-paper-piles-not-tape/spec.md`, with the look approved on 2026-10-16
in [mockups/README.md](mockups/README.md).

## Summary

Every sheet on the public site rests on one blank sheet, and tape moves off sheets onto applied tiles only. The kit
gains one structure, the **blank**: a `kcc-blank` element that is the last child of a sheet's `kcc-slip`, sits
behind the slip's content at `z-index: -1`, and turns and scales about its centre with no offset. Because it lives
inside the slip, it follows the slip's tilt, its stickiness and a stacker's step back with no extra code, and a slip
is now `height: fit-content`, so the slip, and the blank, are always the sheet's height. Its tear and its base turn
direction come from the top sheet's tear class, which `yarn tears` now generates with a `--blank-tear` and a
`--blank-dir`. Each page then sets per-sheet flips (`kcc-slip--flip`) and, where a breakpoint hides a sheet, a region
factor (`--blank-region`), so its blanks alternate in reading order from first to last. A `kcc-slip--wide` modifier swaps the 1.5° and
1.015 pair for 0.5° and 1.005 on the pad's page, the hero-torn sheets and every sheet wider than 700px. A `kcc-slip--lift` class replaces the hover lift utilities and lifts every child of the
slip except the blank, keeping the focus ring's fade. `KccSheet` renders the blank for every Vue sheet. The four
Razor views write it by hand. The header pad drops two of its three pages. `KccSheet` loses its `tape` prop, and the
kit styles tape only inside a `kcc-tilewrap`, including the list row's new in-flow tilewrap. `#app` clips sideways
overflow, so a tall turned blank never scrolls the page.

## Technical Context

**Language/Version**: CSS (Tailwind CSS 4, `@layer components`), Vue 3.5 SFCs with SSR, TypeScript 6 (tear
generator), Razor (.NET 10)

**Primary Dependencies**: Vite 8, Tailwind CSS 4, the Loose Leaf kit under `src/KCC.Web/Features/Styles/Torn/`, the
tear generator `src/KCC.Web/Features/Torn/tears.ts` run by `yarn tears`

**Storage**: N/A. No schema, dictionary or content change.

**Testing**: Vitest in `tests/KCC.ViteTests` (SSR string tests, kit source scans, `tears.test.ts`), TUnit
integration tests for the Razor pages and the Home tear cycle, a TUnit/Playwright page walker plus the existing cook
mode and photo-viewer E2E files, the full `node tests/scripts/run.mjs`

**Target Platform**: The public site in current evergreen browsers, light and dark ramps

**Project Type**: Web application (Umbraco site with a Vue SSR front end)

**Performance Goals**: SC-005. The recipe search grid, with its first page of results, scrolls and hovers with a
median p95 frame time within 10% of `main`'s and a median count of frames over 50ms at most one above `main`'s,
over five runs each (research R8)

**Constraints**: No runtime JavaScript draws (kit invariant 6). `Tears.css` only from `yarn tears`. Razor-rendered
CSS lives in `@layer components`. Plain CSS only: no container query and no typed arithmetic (mockups/README.md).
Every structural hook a test locates by survives (FR-015). Both ramps.

**Scale/Scope**: 1 new kit structure, 4 new kit modifiers (`kcc-slip--wide`, `--lift`, `--flip`,
`kcc-tilewrap--flow`), 1 new generated tear preset, about 35 Vue sheets reached through `KccSheet`, 7 Vue components
with hand-written slips, flips on 10 pages and lists, 4 Razor views, the Home tear cycle in C#, the header pad, 2 brand
docs, the `loose-leaf` skill, the `brand-steward` agent and `CLAUDE.md`

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | How this plan meets it | Result |
|---|---|---|
| I. Content in the backoffice | No copy, label or link is added or moved. The blank is `aria-hidden` and holds nothing | Pass |
| II. Loose Leaf is the only look | The pile is kit CSS in `@layer components` in `Torn/Kit.css`, read by Vue and Razor alike. The blank's tear comes from a generated preset. No JavaScript draws. Both ramps are checked (D6). `kitClasses.test.ts` gets the new classes in the kit.md table. The retired-token `ALLOWLIST` stays empty | Pass |
| III. SQLite has one writer | No write path, guard or lock is touched | N/A |
| IV. Tests first, builds clean | Each behaviour starts with a failing test (research R9). `Tears.css` is regenerated with `yarn tears` and committed with `tears.ts`. `yarn build:all` builds both bundles. Hooks survive: the blank carries no `kcc-slip`, `kcc-torn` or `kcc-sheet` class, so no locator that finds a sheet finds it | Pass |
| V. Cheap to run | No new cost, product or package | Pass |
| VI. Members earn trust | No endpoint is touched | N/A |
| VII. Code explains itself | Class names carry the meaning (`kcc-blank`, `kcc-slip--wide`, `kcc-slip--lift`). Comments only where CSS is unconventional (the `z-index: -1` inside the slip's stacking context). The docs the change makes untrue are corrected in the same change, and so are three test comments that mention the hero's tape | Pass |

Gates: no NEEDS CLARIFICATION marker is open. The owner answered D-6 on 2026-10-16. No GATED marker applies: the
constitution names no gated owner.

**Re-check after Phase 1 design**: the contract ([contracts/pile-markup.md](contracts/pile-markup.md)) keeps every
rule above. No violation, so Complexity Tracking stays empty.

## Decisions

Each decision is argued in [research.md](research.md). The assumption numbers refer to `## Assumptions` in the spec.

| # | Decision | Answer taken | Route |
|---|---|---|---|
| D-1 | How a pile is built for Razor and Vue | Global kit CSS in `Torn/Kit.css`. The blank is the last child of the top sheet's `kcc-slip`, behind its content at `z-index: -1`. `KccSheet` renders it. Razor writes the two-element snippet by hand, as it writes the rest of the structure. No wrapper component and no partial | A6 |
| D-2 | The blank's markup | `<span class="kcc-blank" aria-hidden="true"><span></span></span>`: the wrapper takes the fibre and fall filter and the turn, the inner span is the clipped paper. No `kcc-torn` or `kcc-sheet` class | A7 |
| D-3 | The blank's tear and turn direction | Generated per sheet preset: `--blank-tear` is the preset three along the cycle. `--blank-dir` is opposite to the top's tilt for the six standard presets; the hero presets have a set direction, `tear-hero` −1 (as the preview drew it) and `tear-hero-2` +1. The blank turns by `--blank-turn × --blank-dir × --blank-flip × --blank-region` (research R3) | A8, A18 |
| D-4 | The hero's blank tear | A new preset, `tear-hero-2`, generated by `yarn tears`, also used by the pad's page | A9 |
| D-5 | The wide modifier | `kcc-slip--wide` sets `--blank-turn: 0.5deg` and `--blank-scale: 1.005`. `KccSheet` adds it to every hero-torn sheet and to a sheet given its `wide` prop. A hand-written slip writes it in its class list. The pad's page carries it | A10 |
| D-6 | Which other sheets are wide | Owner, 2026-10-16: every sheet wider than 700px is wide, as well as the pad's page and the hero. A sheet's width is read at a 1280px viewport (content column 1248px), and a surface marked wide keeps the modifier at every width. The wide surfaces: the hero-torn `DetailHero`, `FeaturedRecipeCard` and `SmallHero`; the pad's page; and the recipe search header (`RecipeSearchHeader`), the list rows (`RecipeCardRow`, in the search list and `VariantList`), the "At a glance" sheets (`RecipeDetailView`, `VariantDetailView`), `VariantInstructions`, `VariantNutrition`, `VariantCookNotes`, the three `VariantReviews` sheets (summary, each review, the form), the recipe wizard's step sheets (`StartStep`, `BasicsStep`, `IngredientsStep`, `InstructionsStep`, `PhotoStep`, `MatchStep`, `VariantStep`, `ReviewStep`, `ReadingState`) and `SubmittedSheet`, the account page's recipe groups (`AccountView`), both empty states (`RecipesEmptyState`, `VariantsEmptyState`), `CookMode` and `PhotoViewer`. Every other sheet stays at 1.5° and 1.015 (research R5) | Owner, A19 |
| D-7 | The lift | `kcc-slip--lift` replaces `transition-transform hover:-translate-y-1 focus-within:-translate-y-1`. It moves every child of the slip but the blank 4px up, with the `translate` property over the 300ms default. Its transition also lists `outline-color 0.2s, outline-offset 0.2s`, so the card link keeps the focus ring's fade that `@layer base` gives it (research R6) | A11 |
| D-8 | Reduced motion and the lift | The lift is declared under `prefers-reduced-motion: no-preference`, so under reduced motion nothing moves | A12 |
| D-9 | Tape | `KccSheet` loses `tape`. Sheet tape leaves `FeaturedRecipeCard`, `DetailHero`, `SubmittedSheet`, `RecipesEmptyState`, `VariantsEmptyState`, `SmallHero` and `RegistrationComplete`. `Kit.css` styles only `.kcc-tilewrap > .kcc-tape`. A source-scan test fails on tape outside a tilewrap. The applied items are the icon tiles: the pinned tiles of `RecipeCard`, `FeaturedRecipeCard` and `DetailHero` keep their strip, and the list row's in-sheet tile gains one through an in-flow `kcc-tilewrap kcc-tilewrap--flow` (A25), the one tilewrap allowed inside a sheet; kit.md (rule 2, invariant 8), `CLAUDE.md` and the brand-steward agent state that exception during implementation (research R11). The wizard's photo tiles are tiles, not sheets: no blank and no tape (A20) | A13, A20, A25 |
| D-10 | The header pad | Pages 2 and 3 go. The one page is `pad-page kcc-slip kcc-slip--fill kcc-slip--wide kcc-tear-hero-2`: it keeps `kcc-slip--fill`, which gives it the pad's height now that a slip is `height: fit-content` (`.pad-page` sets `inset: 0` and no height), takes no `.kcc-blank` because it is the top sheet's blank, sits centred with no translate, turns `-0.12deg + var(--blank-turn)` and scales `var(--blank-scale)`. Stacking: top 5, cards 4, page 3, so the cards slide out between the two sheets | A14 |
| D-11 | The one-element slip | `RecipeCardRow`'s `<a class="kcc-slip kcc-torn">` splits into a slip holding the link as its `kcc-torn`, so the filter does not shadow the blank twice | A17 |
| D-12 | Render cost | Measured, not assumed: a scripted Playwright scroll and hover of `/recipes` on `main` and on the branch, five runs each. Pass when the branch's median p95 frame time is within 10% of `main`'s and its median count of frames over 50ms is at most `main`'s plus one. Recorded in the pull request. Not a CI test | A15 |
| D-13 | Test coverage | Vitest first for every Vue surface, the kit CSS and the generator, and for every state the server renders and the pure helpers (`reviewsTrailingSign`, `stepFlips`, `resultsRegionFlips`); Vitest runs in Node through `renderSsr`, where `onMounted` never runs. E2E first, with no new package, for every state only a browser reaches: loaded reviews in `VariantReviewsTests`, and the open filters, list views, toolbar search, later wizard steps and the siblings' re-flip in the walker's states (owner, 2026-10-16). Integration tests for the Razor pages, and unit and integration tests for the Home tear cycle (`HomeSectionsTests`, `HomeSectionsPageTests`). E2E first for the teleported `CookMode`, `PhotoViewer` and `LeaveConfirm` in their existing E2E files (`CookModeTests`, `RecipeImportTests`, `RecipeWizardReviewTests`, `RecipeImportLeaveTests`). One E2E walker for alternation, the blank's box, sideways overflow, the lift and the pad's stacking, which it reads from computed z-index, not a hit test, over the states the E2E site can produce: the seeded no-variant recipe, and two pages of reviews from a stubbed endpoint (research R9) | A16, A28 |
| D-14 | Alternation on every page | Each page sets `kcc-slip--flip` on the sheets R12 names, computes the flip of a sheet after a variable run (reviews, recipe groups, a spotlight, the wizard step) from that run, and sets `max-lg:[--blank-region:-1]` on the recipe search results while the phone's filters are closed. The Home tear cycle skips two tears when it avoids a stacker's tear. On the variant page, `VariantDetailView` derives the siblings' flip from `isAuthenticated` and `reviewCount` with the same function the review section uses, for the state the server renders, which holds no reviews; reviews load on mount, so the siblings re-flip after hydration when the loaded reviews change the section's last sheet. Neighbours are the visible sheets in `main` in document order (research R12) | A21, A22, A26, A27 |
| D-15 | A blank as tall as its sheet | `.kcc-slip` takes `height: fit-content`, and `.kcc-slip--fill` keeps `height: 100%`, so a grid that stretches a slip without `--fill` (the variant ingredients beside the instructions, the four settings sheets) no longer makes the slip, and its blank, taller than the sheet (research R1) | A23 |
| D-16 | The blank's overhang | `#app` takes `overflow-x: clip`: the overhang `s·(w/2·cos t + h/2·sin t) − w/2` grows with a sheet's height and passes the 16px gutter on tall phone sheets. `clip` makes no scroll container, so the sticky header, filters and stacker keep working (research R10) | A24 |
| D-17 | The blank's paper box | `.kcc-blank > span` is `display: block`; an empty inline span has no box | A7 |

No Razor sheet is wide at 1280px: the stacker sheet is half the content width, and the rich-text, registration
complete and error sheets are capped at `max-w-2xl` (672px).

## Project Structure

### Documentation (this feature)

```text
specs/002-paper-piles-not-tape/
├── spec.md
├── mockups/              # the approved look preview
├── plan.md               # this file
├── research.md           # Phase 0
├── data-model.md         # Phase 1: the pile's parts, properties and generated data
├── contracts/
│   └── pile-markup.md    # Phase 1: the markup and class contract for Vue and Razor authors
├── quickstart.md         # Phase 1: how to validate the feature
└── tasks.md              # /speckit-tasks, not this command
```

### Source Code (repository root)

```text
src/KCC.Web/Features/
├── Torn/tears.ts                              # blank mapping per preset, tear-hero-2
├── Styles/Torn/Tears.css                      # regenerated by `yarn tears`
├── Styles/Torn/Kit.css                        # kcc-blank, the pile's defaults and `height: fit-content` on kcc-slip,
│                                              # kcc-slip--wide, kcc-slip--lift, kcc-slip--flip, kcc-tilewrap--flow;
│                                              # tape only in a tilewrap
├── Styles/Main.css                            # `overflow-x: clip` on #app
├── Utilities/BrandColor.ts                    # the stale "minus the hero" comment
├── Components/Sheet/KccSheet.vue              # renders the blank; loses `tape`; gains `piled` and `wide`
├── Components/Header/AppHeader.Component.vue  # one page under the pad
├── Components/Header/PadCard.vue              # `:piled="false"`
├── Components/Recipe/RecipeCard.vue           # blank, lift, flip
├── Components/Recipe/RecipeCardRow.vue        # split slip, blank, wide, lift, flip, tile in a flow tilewrap
├── Components/Recipe/FeaturedRecipeCard.vue   # blank, wide, lift, sheet tape out
├── Components/Recipe/DetailHero.vue           # blank, wide, sheet tape out
├── Components/RecipeDetail/VariantGrid.vue    # blank and lift on the add-variant slip
├── Components/RecipeWizard/SubmittedSheet.vue, Components/RecipeSearch/RecipesEmptyState.vue,
│   Components/RecipeDetail/VariantsEmptyState.vue, Widgets/Hero/SmallHero.Component.vue   # `tape` out, wide
├── the other wide `KccSheet` surfaces listed in D-6                                      # `wide`
├── the pages and lists of research R12 (RecipeSearchView, RecipeDetailView, VariantDetailView, VariantGrid,
│   VariantList, VariantSiblings, VariantReviews, AccountView, AccountSettingsView, RecipeWizard)   # flips
├── Components/RecipeWizard/PhotoStrip.vue                                                # unchanged: tiles (A20)
└── Pages/
    ├── Home/Blocks/StackerBlock.cshtml, Home/Blocks/RichTextBlock.cshtml                   # blank by hand
    ├── Account/RegistrationComplete/Index.cshtml                                           # blank, tape out
    ├── Error/Index.cshtml                                                                  # blank
    └── Home/HomeSections.cs                                                                # skip two tears

tests/
├── KCC.ViteTests/Features/Torn/tears.test.ts                       # mapping, tear-hero-2
├── KCC.ViteTests/Features/Styles/pile.test.ts                      # new: kit CSS and source scans
├── KCC.ViteTests/Features/Styles/kitClasses.test.ts, motion.test.ts  # stay green with the new classes
├── KCC.ViteTests/Features/Components/**, Widgets/**, Pages/**      # one blank per sheet, tape on tiles only
├── KCC.UnitTests/Features/Pages/Home/HomeSectionsTests.cs                    # the tear cycle skips two
├── KCC.IntegrationTests/Features/Pages/Home/HomeSectionsPageTests.cs, AccountPagesTests.cs, ErrorPageTests.cs
├── KCC.E2ETests/Config/Piles.cs, Features/Chrome/PileTests.cs     # new: the page walker, the lift, the pad
├── KCC.E2ETests/Features/VariantDetail/CookModeTests.cs, Features/Members/RecipeImportTests.cs,
│   RecipeWizardReviewTests.cs, RecipeImportLeaveTests.cs           # the teleported overlays' blanks
└── KCC.E2ETests/Features/VariantReviews/VariantReviewsTests.cs     # loaded reviews: blank, wide, flip

docs/brand/loose-leaf.md, docs/brand/kit.md, .claude/skills/loose-leaf/SKILL.md, .claude/agents/brand-steward.md,
CLAUDE.md                                                           # the rule, the contract, the invariants
```

**Structure Decision**: The existing web front end in `src/KCC.Web/Features` with its kit CSS, and the existing test
projects. No new project.

## Implementation order

1. The generator: `tears.ts` mapping and `tear-hero-2`, test first, then `yarn tears`.
2. The kit CSS: `kcc-blank`, the slip's `fit-content`, `kcc-slip--wide`, `kcc-slip--lift`, `kcc-slip--flip`,
   `kcc-tilewrap--flow`, tape inside a tilewrap only, and `#app`'s clip, with `pile.test.ts` first and the kit.md
   Classes table in the same change so `kitClasses.test.ts` stays green.
3. `KccSheet`: the blank, `piled`, `wide` (and wide on a hero tear), no `tape`. Then each Vue surface, its test first,
   with `wide` on the D-6 surfaces.
4. The hand-written Vue slips: `RecipeCard`, `RecipeCardRow`, `FeaturedRecipeCard`, `DetailHero`, `VariantGrid`.
5. The flips of research R12, page by page: each server-rendered state's SSR test first, and each browser-only state's
   E2E test first, starting with the walker on recipe search.
6. The header pad.
7. The Razor views and the Home tear cycle, their integration tests first.
8. The overlay assertions in `CookModeTests` and the photo-viewer flows, the walker's remaining pages, and the lift's
   and the pad's E2E tests in their own steps.
9. The docs, the skill, the agent, `CLAUDE.md` and the `BrandColor.ts` comment.
10. The render-cost measurement (quickstart step 5), D6 in both ramps and D7 brand review.

## Definition of done

Rows D1 to D4, D6, D7 and D10 apply. D5 applies because `tears.ts` changes: `Tears.css` comes from `yarn tears` and is
committed with it. D8 and D9 do not apply. D4 is the full `node tests/scripts/run.mjs` at the repository root, after
`dotnet build KitchenCommandCenter.slnx -c Release` and `yarn build:all`.

## Complexity Tracking

No violation.

Red team round 1: 2026-10-16, 10 real flaws (9 plan, 1 routed as A20)
Red team: 2026-10-16, 15 real flaws found, 15 fixed (14 in the plan, 1 routed as A20)
