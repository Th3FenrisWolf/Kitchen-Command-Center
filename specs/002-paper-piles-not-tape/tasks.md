---

description: "Task list for Paper Piles, Not Tape"
---

# Tasks: Paper Piles, Not Tape

**Input**: Design documents from `specs/002-paper-piles-not-tape/`: [spec.md](spec.md) (A1 to A31), [plan.md](plan.md)
(D-1 to D-17), [research.md](research.md) (R1 to R12), [data-model.md](data-model.md),
[contracts/pile-markup.md](contracts/pile-markup.md), [quickstart.md](quickstart.md), and the approved look in
[mockups/README.md](mockups/README.md).

**Tests**: Required. Constitution IV: every behaviour change starts with a failing test that the change then makes
pass. In each phase the test tasks come first; run them and see them fail before the implementation tasks start. A
test that already passes on the previous phase's code is coverage, and says so.

**Organization**: One phase is one builder dispatch. The four user stories are split into phases a single builder can
finish in one context. US1 (every sheet lies on another sheet) spans Phases 3 to 10. Each phase ends with its suites
green.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: US1 to US4, from spec.md
- Paths are relative to the repository root.

## Conventions every phase follows

- Vitest runs from `src/KCC.Web`: `yarn test <pattern>` (its `vite.config.ts` drives `tests/KCC.ViteTests`).
- One TUnit suite runs with `dotnet run --project <suite>.csproj`; `dotnet test` reports "Zero tests ran".
- Every yarn command needs `FONTAWESOME_NPM_AUTH_TOKEN`. Build both bundles together with `yarn build:all` at the
  repository root, never one side alone.
- `src/KCC.Web/Features/Styles/Torn/Tears.css` comes only from `yarn tears` (run in `src/KCC.Web`). Never hand-edit it.
- New kit classes go into the kit.md Classes table in the same change as their CSS, or
  `tests/KCC.ViteTests/Features/Styles/kitClasses.test.ts` fails.
- The blank is `<span class="kcc-blank" aria-hidden="true"><span></span></span>`, the **last** child of its slip, with
  no `kcc-slip`, `kcc-torn` or `kcc-sheet` class (contract, D-2). Every existing `data-testid`, role, id and `kcc-*`
  hook survives (FR-015), apart from the sheet tape that US2 removes.
- The new modifiers (`kcc-slip--wide`, `kcc-slip--flip`) and the appended blank change what a test sees: a count of
  `kcc-slip` matches now also counts the modifiers, so count slip elements (for example `class="kcc-slip ` or a tag
  parse); an exact class list (`expect(classesOf(slip)).toEqual([...])`) and an exact opening tag or full markup string
  (`toContain('<section class="kcc-slip kcc-tear-3 my-6">')`) change wherever a modifier lands. The task that adds the
  modifier updates the assertion; the known ones are named in their tasks.
- Vitest runs with `environment: 'node'` and renders through `renderSsr`, where `onMounted` never runs. It covers the
  states the server renders and pure helpers. A state only a browser reaches is driven first by an E2E test in the phase
  that implements it (A30).
- `tests/KCC.E2ETests/Config/SiteProcess.cs` starts `src/KCC.Web/bin/<configuration>/net10.0/KCC.Web.dll`, Debug by
  default, so every single-suite E2E run follows a plain `dotnet build KitchenCommandCenter.slnx` and `yarn build:all`.
  `-c Release` is for D1.
- Comments follow constitution VII: none that restate code; one only where CSS is unconventional for a reason (the
  blank's `z-index: -1` inside the slip's stacking context).

---

## Phase 1: Setup

**Purpose**: Confirm the gates and a clean starting point.

- [X] T001 Confirm constitution gates 1 and 2: `FONTAWESOME_NPM_AUTH_TOKEN`, `KCC_E2E_MEMBER_USERNAME` and `KCC_E2E_MEMBER_PASSWORD` are set; the .NET SDK named in `global.json`, Node 24 with Yarn 1 and the Playwright browsers are installed; the checkout is on `002-paper-piles-not-tape` with `SPECIFY_FEATURE_DIRECTORY=specs/002-paper-piles-not-tape`
- [X] T002 Build the starting point clean: `dotnet build KitchenCommandCenter.slnx -c Release` and `yarn build:all` at the repository root, then `yarn test` in `src/KCC.Web`; record any failure that exists before this feature in the phase hand-back rather than fixing it here

**Checkpoint**: The branch builds and the web suite is green before any change.

---

## Phase 2: Foundational - the generator, the kit's pile and `KccSheet`

**Purpose**: The blank's tear data, the pile CSS and the `KccSheet` blank that every story builds on (D-1 to D-5,
D-15 to D-17).

**CRITICAL**: No user story phase starts until this phase is complete.

### Tests first

- [X] T003 [P] Rewrite the generator tests first in `tests/KCC.ViteTests/Features/Torn/tears.test.ts`: "has six standard sheets, a hero and three tiles" becomes six standard sheets, two hero sheets (`tear-hero`, `tear-hero-2`) and three tiles; every sheet preset's class writes `--blank-tear: var(--tear-N)` and `--blank-dir: ±1` after `--r`; a blank tear is never the preset's own and names a declared sheet preset; the six standard presets map three along the cycle (1→4, 2→5, 3→6, 4→1, 5→2, 6→3), turn opposite their tilt's sign and alternate around the cycle; `tear-hero` maps to `tear-hero-2` with `-1`; `tear-hero-2` maps to `tear-hero` with `+1`, tilts within ±1° and chamfers a corner other than `tear-hero`'s; a tile preset writes no blank (research R3, R4, data-model "Generated preset data")
- [X] T004 [P] Create `tests/KCC.ViteTests/Features/Styles/pile.test.ts` with the kit's pile assertions, reading `src/KCC.Web/Features/Styles/Torn/Kit.css` and `src/KCC.Web/Features/Styles/Main.css` as `kitClasses.test.ts` reads files: `.kcc-slip` has `height: fit-content` and sets `--blank-flip: 1`, `--blank-turn: 1.5deg` and `--blank-scale: 1.015`; `.kcc-slip--fill` keeps `height: 100%` and is declared after it; `.kcc-blank` is `position: absolute`, `inset: 0`, `pointer-events: none`, `z-index: -1`, sets `--tear: var(--blank-tear)`, shares the `.kcc-torn` filter rule, and transforms `rotate(calc(var(--blank-turn) * var(--blank-dir) * var(--blank-flip) * var(--blank-region, 1))) scale(var(--blank-scale))`; `.kcc-blank > span` is `display: block`, `height: 100%`, `background-color: var(--color-paper)`, `clip-path: var(--tear)` and draws no ruling; `.kcc-slip--flip` sets `--blank-flip: -1`; `.kcc-slip--wide` sets `0.5deg` and `1.005`; `.kcc-slip--wide` and `.kcc-slip--flip` are declared after `.kcc-slip`, so they win at equal specificity; all of it sits in `@layer components`; `#app` in `Main.css` takes `overflow-x: clip` (research R1, R2, R3, R5, R9, R10)
- [X] T005 [P] Extend `tests/KCC.ViteTests/Features/Components/Sheet/KccSheet.test.ts` first: one blank, the last child of the slip and after the label; no blank with `piled: false`; `kcc-slip--wide` with `tear: 'hero'` and with `wide: true`, and on no other sheet; `kcc-slip--flip` with `flip: true` only; update the exact markup the blank and the modifier change: the default sheet's full markup (line 17) and the label test (line 34) now end with the blank, and `KccSheet.test.ts:40`'s `<article class="kcc-slip kcc-tear-hero">` gains `kcc-slip--wide` (contract "`KccSheet.vue`")
- [X] T006 [P] Add a test to `tests/KCC.ViteTests/Features/Components/Header/PadCards.test.ts` that a pad card renders no `kcc-blank` (FR-001's exception, A3)

### Implementation

- [X] T007 Add the blank mapping and `tear-hero-2` to `src/KCC.Web/Features/Torn/tears.ts`: `TearPreset` gains `blank?: { tear: string; dir: 1 | -1 }`; a `tear-hero-2` preset joins `SHEET_TEARS` (160 points a side, amp 2.4, a seed of its own, a corner other than `tear-hero`'s 1, a tilt within ±1°); every sheet preset gets its `blank` per research R3's table; `sheetClass` writes `--blank-tear` and `--blank-dir` after `--r`; `tests/KCC.ViteTests/Features/Torn/tornPolygon.test.ts` still passes for the new preset. Mind the size budget: `tears.test.ts` asserts `tearsCss().length` stays under 120,000 characters, today's output is 96,217, and `--tear-hero` alone is about 21,750, so a second 160-point hero leaves about 1,600 to spare. If the output passes the budget, stop and report it rather than raise the limit or thin the preset
- [X] T008 Run `yarn tears` in `src/KCC.Web` to regenerate `src/KCC.Web/Features/Styles/Torn/Tears.css`; confirm it declares `--tear-hero-2` and each sheet class writes `--blank-tear` and `--blank-dir`; `yarn test tears tornPolygon` passes (depends on T007)
- [X] T009 [P] Correct the comment at line 4 of `src/KCC.Web/Features/Utilities/BrandColor.ts` to "minus the two hero tears"; `SHEET_TEAR_COUNT` stays 6
- [X] T010 Add the pile to `src/KCC.Web/Features/Styles/Torn/Kit.css` in `@layer components`: `height: fit-content` and the `--blank-flip`, `--blank-turn`, `--blank-scale` defaults on `.kcc-slip` (`.kcc-slip--fill` keeps `height: 100%` after it); `.kcc-blank` added to the `.kcc-torn` filter rule; the `.kcc-blank` placement, `z-index: -1` and transform; `.kcc-blank > span`; `.kcc-slip--flip`; `.kcc-slip--wide` (research R1 to R3, R5; data-model "Custom properties")
- [X] T011 [P] Give `#app` `overflow-x: clip` in `src/KCC.Web/Features/Styles/Main.css` (D-16, research R10)
- [X] T012 Add `kcc-blank` › `span`, `kcc-slip--wide` and `kcc-slip--flip` to the Classes table of `docs/brand/kit.md`, worded as in the contract's "Classes table entries", and name `kcc-tear-hero-2` in its Tears section, so `tests/KCC.ViteTests/Features/Styles/kitClasses.test.ts` stays green (its `TEAR` pattern matches `kcc-tear-hero` but not `kcc-tear-hero-2`)
- [X] T013 Render the blank in `src/KCC.Web/Features/Components/Sheet/KccSheet.vue`: new props `piled` (default `true`), `wide` (default `false`) and `flip` (default `false`); `kcc-slip--wide` when `tear === 'hero'` or `wide`; `kcc-slip--flip` when `flip`; the blank renders after the label and the tape when `piled`, as the slip's last child. Leave `tape` for Phase 11 (contract "`KccSheet.vue`")
- [X] T014 Pass `:piled="false"` to `KccSheet` in `src/KCC.Web/Features/Components/Header/PadCard.vue` (D-10, A3)
- [X] T015 In `src/KCC.Web` run `yarn test`, `yarn type-check`, `yarn lint` and `yarn format`; where a Vitest assertion breaks only because a slip now ends with the blank, update it to the new markup without weakening what it checks. Then, because `height: fit-content` on every slip and the blank inside every `KccSheet` reach Razor and browser layout, build with `yarn build:all` and a plain `dotnet build KitchenCommandCenter.slnx` (Debug, which `SiteProcess.cs` starts) and run the whole integration suite (`dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj`) and the E2E Chrome, HomePage and grid-row tests (`dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj -- --treenode-filter "/*/*/<Class>/*"` for `ChromeTests`, `PadTests`, `PhonePadTests`, `MemberPadTests`, `PadColdStartTests`, `NotFoundTests`, `HomePageTests`, and the `tests/KCC.E2ETests/Config/GridRows.cs` users `RecipeSearchTests` and `VariantGridTests`)

**Checkpoint**: Every `KccSheet` already rests on a blank; the generator, the kit and `KccSheet` tests pass.

---

## Phase 3: User Story 1 - wide sheets on recipe search, recipe detail and variant detail (Priority: P1)

**Goal**: Each `KccSheet` surface on the three recipe pages rests on one blank, and the ones wider than 700px take the
wide pair (D-6, research R5).

**Independent Test**: `yarn test RecipeSearchHeader RecipesEmptyState VariantsEmptyState RecipeDetailView VariantDetailView VariantInstructions VariantNutrition VariantCookNotes VariantReviews VariantIngredients RecipeFilters`, then `VariantReviewsTests` in the E2E suite.

### Tests for User Story 1 (write first, see them fail)

- [X] T016 [P] [US1] Assert one `kcc-blank` and `kcc-slip--wide` on the header sheet in `tests/KCC.ViteTests/Features/Components/RecipeSearch/RecipeSearchHeader.test.ts`, updating the exact opening tag at `RecipeSearchHeader.test.ts:13`
- [X] T017 [P] [US1] Assert one `kcc-blank` and `kcc-slip--wide` in `tests/KCC.ViteTests/Features/Components/RecipeSearch/RecipesEmptyState.test.ts` (its tape assertion stays until Phase 11)
- [X] T018 [P] [US1] Assert one `kcc-blank` and `kcc-slip--wide` in `tests/KCC.ViteTests/Features/Components/RecipeDetail/VariantsEmptyState.test.ts` (its tape assertion stays until Phase 11)
- [X] T019 [P] [US1] Assert the "At a glance" sheet carries one `kcc-blank` and `kcc-slip--wide` in `tests/KCC.ViteTests/Features/Pages/RecipeDetail/RecipeDetailView.test.ts`
- [X] T020 [P] [US1] Assert the "At a glance" sheet carries one `kcc-blank` and `kcc-slip--wide` in `tests/KCC.ViteTests/Features/Pages/VariantDetail/VariantDetailView.test.ts`
- [X] T021 [P] [US1] Assert one `kcc-blank` and `kcc-slip--wide` in `tests/KCC.ViteTests/Features/Components/VariantDetail/VariantInstructions.test.ts`, `VariantNutrition.test.ts` and `VariantCookNotes.test.ts` in the same folder; check that the opening-tag pattern at `VariantCookNotes.test.ts:15` still holds with the modifier
- [X] T022 [P] [US1] Assert the summary (count above 0) and the review form (signed in) each carry one `kcc-blank` and `kcc-slip--wide` in `tests/KCC.ViteTests/Features/Components/VariantDetail/VariantReviews.test.ts`. The server renders no reviews (`load(0)` runs in `onMounted`, `VariantReviews.vue:103`), so each loaded review's blank and modifier are browser-only: T023 catches them first
- [X] T023 [P] [US1] In `LoggedInMember_CanSubmitEditAndDeleteAReview` of `tests/KCC.E2ETests/Features/VariantReviews/VariantReviewsTests.cs`, once the submitted review shows in `[data-testid="reviews-list"]`, assert its sheet holds exactly one `.kcc-blank` and carries `kcc-slip--wide`
- [X] T024 [P] [US1] Assert one `kcc-blank` and no `kcc-slip--wide` in `tests/KCC.ViteTests/Features/Components/VariantDetail/VariantIngredients.test.ts` and `tests/KCC.ViteTests/Features/Components/RecipeSearch/RecipeFilters.test.ts` (coverage: these pass on Phase 2's `KccSheet`)

### Implementation for User Story 1

- [X] T025 [P] [US1] Pass `wide` to the sheet in `src/KCC.Web/Features/Components/RecipeSearch/RecipeSearchHeader.vue`
- [X] T026 [P] [US1] Pass `wide` to the sheets in `src/KCC.Web/Features/Components/RecipeSearch/RecipesEmptyState.vue` and `src/KCC.Web/Features/Components/RecipeDetail/VariantsEmptyState.vue`
- [X] T027 [P] [US1] Pass `wide` to the "At a glance" sheet in `src/KCC.Web/Features/Pages/RecipeDetail/RecipeDetailView.Component.vue`
- [X] T028 [P] [US1] Pass `wide` to the "At a glance" sheet in `src/KCC.Web/Features/Pages/VariantDetail/VariantDetailView.Component.vue`
- [X] T029 [P] [US1] Pass `wide` to the sheets in `src/KCC.Web/Features/Components/VariantDetail/VariantInstructions.vue`, `VariantNutrition.vue` and `VariantCookNotes.vue`
- [X] T030 [P] [US1] Pass `wide` to the three sheets in `src/KCC.Web/Features/Components/VariantDetail/VariantReviews.vue`
- [X] T031 [US1] In `src/KCC.Web` run `yarn test`, `yarn type-check`, `yarn lint` and `yarn format`; then `dotnet build KitchenCommandCenter.slnx` (Debug), `yarn build:all`, and `VariantReviewsTests` with `dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj -- --treenode-filter "/*/*/VariantReviewsTests/*"`

**Checkpoint**: The recipe pages' `KccSheet` surfaces are piled with the right turn.

---

## Phase 4: User Story 1 - wide sheets in the recipe wizard and account, and the standard sheets (Priority: P1)

**Goal**: The wizard's step sheets, `ReadingState`, `SubmittedSheet` and the account page's recipe groups take the wide
pair; every other `KccSheet` surface rests on one standard blank; the photo tiles take none (D-6, A20).

**Independent Test**: `yarn test StartStep BasicsStep IngredientsStep InstructionsStep PhotoStep MatchStep VariantStep ReviewStep ReadingState SubmittedSheet CreateRecipeView AddVariantView AccountView SmallHero Card Stacker LoginView ComingSoonSection AccountSettingsView PhotoStrip`, then `RecipeImportLeaveTests` in the E2E suite.

### Tests for User Story 1 (write first, see them fail)

- [X] T032 [P] [US1] Assert the step sheet carries one `kcc-blank` and `kcc-slip--wide` in `tests/KCC.ViteTests/Features/Components/RecipeWizard/StartStep.test.ts`, `BasicsStep.test.ts`, `IngredientsStep.test.ts` and `InstructionsStep.test.ts`, updating the exact class list at `StartStep.test.ts:22`
- [X] T033 [P] [US1] Assert the step sheet carries one `kcc-blank` and `kcc-slip--wide` in `tests/KCC.ViteTests/Features/Components/RecipeWizard/PhotoStep.test.ts`, `MatchStep.test.ts`, `VariantStep.test.ts` and `ReviewStep.test.ts`, updating the exact class lists at `PhotoStep.test.ts:37` and `MatchStep.test.ts:45`
- [X] T034 [P] [US1] Assert one `kcc-blank` and `kcc-slip--wide` in `tests/KCC.ViteTests/Features/Components/RecipeWizard/ReadingState.test.ts` and `SubmittedSheet.test.ts`, updating the exact class list at `ReadingState.test.ts:16`
- [X] T035 [P] [US1] Assert the wizard's step sheet and its `SmallHero` each carry one `kcc-blank` and `kcc-slip--wide` in `tests/KCC.ViteTests/Features/Pages/CreateRecipe/CreateRecipeView.test.ts` and `tests/KCC.ViteTests/Features/Pages/AddVariant/AddVariantView.test.ts`, updating the exact opening tags at `CreateRecipeView.test.ts:57` and `AddVariantView.test.ts:70`
- [X] T036 [P] [US1] Assert each recipe group sheet carries `kcc-slip--wide` and the profile and coming-soon sheets do not, all with one `kcc-blank`, in `tests/KCC.ViteTests/Features/Pages/Account/AccountView.test.ts`
- [X] T037 [P] [US1] Assert the hero-torn section carries one `kcc-blank` and `kcc-slip--wide` in `tests/KCC.ViteTests/Features/Widgets/SmallHero.test.ts` (coverage: `KccSheet` adds the modifier for `tear: 'hero'`; if it fails, fix `src/KCC.Web/Features/Widgets/Hero/SmallHero.Component.vue`)
- [X] T038 [P] [US1] Coverage of Phase 2's `KccSheet`, in four places:
  - Assert one `kcc-blank` per sheet and no `kcc-slip--wide` in `tests/KCC.ViteTests/Features/Widgets/Card.test.ts`, `tests/KCC.ViteTests/Features/Widgets/Stacker.test.ts`, `tests/KCC.ViteTests/Features/Pages/Account/Login/LoginView.test.ts` and `tests/KCC.ViteTests/Features/Components/ComingSoon/ComingSoonSection.test.ts`.
  - In `tests/KCC.ViteTests/Features/Pages/Account/Settings/AccountSettingsView.test.ts`, assert that the four settings sheets (its `sheetsOf` helper) carry one `kcc-blank` each and no `kcc-slip--wide`, and that the page's `SmallHero` carries `kcc-slip--wide`.
  - Add a blank assertion to `tests/KCC.E2ETests/Features/Members/RecipeImportLeaveTests.cs`: the open `[data-testid="leave-dialog"]` holds exactly one `.kcc-blank`. The dialog teleports to `body` after mount, so `LeaveConfirm.test.ts` reads the template source and cannot see the blank `KccSheet` renders.
- [X] T039 [P] [US1] Assert the photo tiles render no `kcc-blank` and no `kcc-tape` in `tests/KCC.ViteTests/Features/Components/RecipeWizard/PhotoStrip.test.ts` (A20; coverage, `PhotoStrip.vue` does not change)

### Implementation for User Story 1

- [X] T040 [P] [US1] Pass `wide` to the step sheet in `src/KCC.Web/Features/Components/RecipeWizard/StartStep.vue`, `BasicsStep.vue`, `IngredientsStep.vue`, `InstructionsStep.vue`, `PhotoStep.vue`, `MatchStep.vue`, `VariantStep.vue` and `ReviewStep.vue`
- [X] T041 [P] [US1] Pass `wide` to the sheets in `src/KCC.Web/Features/Components/RecipeWizard/ReadingState.vue` and `src/KCC.Web/Features/Components/RecipeWizard/SubmittedSheet.vue`
- [X] T042 [P] [US1] Pass `wide` to the recipe group sheets in `src/KCC.Web/Features/Pages/Account/AccountView.Component.vue`
- [X] T043 [US1] In `src/KCC.Web` run `yarn test`, `yarn type-check`, `yarn lint` and `yarn format`, then `dotnet build KitchenCommandCenter.slnx` (Debug), `yarn build:all` and `RecipeImportLeaveTests` with `dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj -- --treenode-filter "/*/*/RecipeImportLeaveTests/*"`

**Checkpoint**: Every `KccSheet` surface but the teleported overlays carries its blank and its turn.

---

## Phase 5: User Story 1 - the hand-written Vue slips (Priority: P1)

**Goal**: The five components that write their own slip rest on a blank, and the list row splits into a slip and a link
so it can hold one (D-11, A17). Each takes a `flip` prop for the page flips of Phases 7 to 9.

**Independent Test**: `yarn test RecipeCard RecipeCardRow FeaturedRecipeCard DetailHero VariantGrid VariantList`.

### Tests for User Story 1 (write first, see them fail)

- [X] T044 [P] [US1] In `tests/KCC.ViteTests/Features/Components/Recipe/RecipeCard.test.ts` assert one blank, the article's last child; no `kcc-slip--wide`; `kcc-slip--flip` only with `flip: true`
- [X] T045 [P] [US1] In `tests/KCC.ViteTests/Features/Components/Recipe/RecipeCardRow.test.ts` rewrite "is one link over a flat torn sheet" (lines 44 to 54) first: the slip element carries `kcc-slip kcc-slip--wide`, the tear class and `--r: 0`, and holds an `<a class="kcc-torn block">` and, last, one blank; the `a.kcc-torn` carries the `href`, `data-testid="recipe-card"` and `card.dataAttrs` (`data-recipe-name`), as in `RecipeCard`; still exactly one `<a`; no element carries both `kcc-slip` and `kcc-torn`; `kcc-slip--flip` only with `flip: true`. Rewrite "lifts under the pointer and under focus" (lines 61 to 66) to read the lift utilities from the slip element, not the link
- [X] T046 [P] [US1] In `tests/KCC.ViteTests/Features/Components/Recipe/FeaturedRecipeCard.test.ts` and `tests/KCC.ViteTests/Features/Components/Recipe/DetailHero.test.ts` assert one blank, the slip's last child, and `kcc-slip--wide`
- [X] T047 [P] [US1] In `tests/KCC.ViteTests/Features/Components/RecipeDetail/VariantGrid.test.ts` assert the add-variant slip holds one blank; with `flip: true` every card and the add-variant slip carry `kcc-slip--flip`, and none without it
- [X] T048 [P] [US1] In `tests/KCC.ViteTests/Features/Components/RecipeDetail/VariantList.test.ts` assert each row holds one blank and, with `flip: true`, carries `kcc-slip--flip`

### Implementation for User Story 1

- [X] T049 [P] [US1] Append the blank and a `flip` prop to `src/KCC.Web/Features/Components/Recipe/RecipeCard.vue` (the lift utilities stay until Phase 13)
- [X] T050 [P] [US1] Split the one-element slip in `src/KCC.Web/Features/Components/Recipe/RecipeCardRow.vue` into a `div` slip carrying `kcc-slip kcc-slip--wide`, the `kcc-tear-N` class, `style="--r: 0"` and the lift utilities (until Phase 13), holding the link and the blank; the link is `<a class="kcc-torn block">` with the `href` and `v-bind="card.dataAttrs"`, as in `RecipeCard.vue`; add a `flip` prop; keep the hooks that `tests/KCC.E2ETests/Config/GridRows.cs` (`:scope > .kcc-slip`, `slip.querySelector('.kcc-sheet')`) and the list tests locate by (contract "A piled sheet")
- [X] T051 [P] [US1] Append the blank and `kcc-slip--wide` to the slip in `src/KCC.Web/Features/Components/Recipe/FeaturedRecipeCard.vue`, and add a `flip` prop
- [X] T052 [P] [US1] Append the blank and `kcc-slip--wide` to the slip in `src/KCC.Web/Features/Components/Recipe/DetailHero.vue`
- [X] T053 [P] [US1] Append the blank to the add-variant slip and add a `flip` prop passed to every card and the add-variant slip in `src/KCC.Web/Features/Components/RecipeDetail/VariantGrid.vue`
- [X] T054 [P] [US1] Add a `flip` prop passed to every row in `src/KCC.Web/Features/Components/RecipeDetail/VariantList.vue`
- [X] T055 [US1] In `src/KCC.Web` run `yarn test`, `yarn type-check`, `yarn lint` and `yarn format`

**Checkpoint**: Every Vue sheet on the site is piled.

---

## Phase 6: User Story 1 - the Razor sheets and the Home tear cycle (Priority: P1)

**Goal**: The four Razor views write the blank by hand, and the Home tear cycle keeps its tilts, and so its blanks,
alternating through a stacker's skip (D-1, D-14, A26).

**Independent Test**: `yarn test pile`, then `dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj` and
`dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj`.

### Tests for User Story 1 (write first, see them fail)

- [X] T056 [P] [US1] Add a source scan to `tests/KCC.ViteTests/Features/Styles/pile.test.ts`: every `kcc-slip` element in `src/KCC.Web/Features/**/*.cshtml` holds `<span class="kcc-blank" aria-hidden="true"><span></span></span>` as its last child
- [X] T057 [P] [US1] In `tests/KCC.UnitTests/Features/Pages/Home/HomeSectionsTests.cs` assert that when the cycle avoids a stacker's tear it skips two tears, not one, and that the tilt signs of consecutive draws alternate down the page; rewrite the expected sequences first, traced against `TearCycle`: `From_StackerCards_SkipTheSheetsTear` cards "2,3,4,5,6,2" become "2,3,4,5,6,3", and `From_FirstTwoCardsBelowAStacker_SkipItsSheetsTear` grid "6,2,3" becomes "6,3,4"
- [X] T058 [P] [US1] In `tests/KCC.IntegrationTests/Features/Pages/Home/HomeSectionsPageTests.cs` rewrite first, traced against the baseline Home (rich text, an eight-card grid, a stacker with a sheet and six cards, two rich texts): `Home_RunsOneTearCycleDownThePage` sheets "1,4,6,1" become "1,4,1,2" and stacker cards "5,6,1,2,3,5" become "5,6,1,2,3,6" (grid "2,3,4,5,6,1,2,3" is unchanged); the `kcc-tear-6 mx-auto w-full max-w-2xl` argument of `Home_SetsItsTextOnTornSheets` becomes `kcc-tear-2 mx-auto w-full max-w-2xl`. Then assert the stacker and rich-text sheets each end with one blank, and that the signs of the page's tears alternate in document order through the stacker's skip
- [X] T059 [P] [US1] In `tests/KCC.IntegrationTests/Features/Pages/ErrorPageTests.cs` assert the error sheet ends with one blank
- [X] T060 [P] [US1] In `tests/KCC.IntegrationTests/Features/Pages/AccountPagesTests.cs` assert the registration complete sheet ends with one blank (its tape goes in Phase 11)

### Implementation for User Story 1

- [X] T061 [P] [US1] Append the blank to the sheet slip in `src/KCC.Web/Features/Pages/Home/Blocks/StackerBlock.cshtml`
- [X] T062 [P] [US1] Append the blank to the slip in `src/KCC.Web/Features/Pages/Home/Blocks/RichTextBlock.cshtml`
- [X] T063 [P] [US1] Append the blank to the slip in `src/KCC.Web/Features/Pages/Error/Index.cshtml`
- [X] T064 [P] [US1] Append the blank as the slip's last child in `src/KCC.Web/Features/Pages/Account/RegistrationComplete/Index.cshtml`
- [X] T065 [P] [US1] Make `TearCycle` skip two tears when it avoids a stacker's tear in `src/KCC.Web/Features/Pages/Home/HomeSections.cs`, and correct the comments near lines 50 and 75 that the change makes untrue
- [X] T066 [US1] Run `yarn test pile` in `src/KCC.Web`, then the unit suite and the whole integration suite with `dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj` and `dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj`

**Checkpoint**: Vue and Razor sheets are piled alike.

---

## Phase 7: User Story 1 - alternation on recipe search and recipe detail (Priority: P1)

**Goal**: The blanks on the two pages alternate in reading order from first sheet to last, in every state (research
R12, A18, A21, A22). This phase creates the browser walker, because the phone's open filters, the list view and a
toolbar search are states only a browser reaches (A30).

**Independent Test**: `yarn test RecipeSearchView RecipeDetailView RecipeFilters resultsRegion`, then `PileTests` in the
E2E suite.

### Tests for User Story 1 (write first, see them fail)

- [X] T067 [US1] Create the walker in `tests/KCC.E2ETests/Config/Piles.cs`: it reads every visible `.kcc-blank` inside `main` in document order and checks that each blank's turn sign, from its computed transform matrix, is opposite the one before (FR-003); that each blank's offset box equals its slip's `.kcc-torn` offset box (FR-004); and that `scrollWidth <= clientWidth` on the page (R10)
- [X] T068 [US1] Create `tests/KCC.E2ETests/Features/Chrome/PileTests.cs` running the walker at 375px and 1280px over the recipe search and recipe detail states of R9: recipe search in grid and list view, with a spotlight (a query that matches one rated recipe) and without, with the phone's filters open and closed; recipe detail with variants in grid and list view, with the featured variant, and with a toolbar search that matches none so the featured card sits over the empty state; "Bare Cupboard Snack Board" (no variants). It catches first the states the server never renders: the open filters, the list views and the toolbar search (depends on T067)
- [X] T069 [P] [US1] Create `tests/KCC.ViteTests/Features/Pages/RecipeSearch/resultsRegion.test.ts`: `resultsRegionFlips(sheetOpen)` is `true` while the phone's filters are closed and `false` once they open
- [X] T070 [P] [US1] In `tests/KCC.ViteTests/Features/Pages/RecipeSearch/RecipeSearchView.test.ts` assert, per R12, the states the server renders (Vitest runs with `environment: 'node'` through `renderSsr`, where `onMounted` never runs): the filters sheet carries `kcc-slip--flip`; with a spotlight every `RecipeCard` carries it, and without one none does; `RecipesEmptyState` carries it; the results `<section>` carries `max-lg:[--blank-region:-1]` in the server render, where the filters start closed. The open filters and the list view's `RecipeCardRow`s are browser-only: T068 catches them first
- [X] T071 [P] [US1] In `tests/KCC.ViteTests/Features/Pages/RecipeDetail/RecipeDetailView.test.ts` assert, per R12, the states the server renders: "At a glance" carries `kcc-slip--flip`; with the featured variant the grid's cards and the add-variant slip carry it; without one they do not; with no variants `VariantsEmptyState` carries it. The list view and the featured card over the empty state (a toolbar search) are browser-only: T068 catches them first
- [X] T072 [P] [US1] If `RecipeFilters` needs a `flip` prop to reach its sheet, assert it in `tests/KCC.ViteTests/Features/Components/RecipeSearch/RecipeFilters.test.ts`

### Implementation for User Story 1

- [X] T073 [P] [US1] Create `src/KCC.Web/Features/Pages/RecipeSearch/resultsRegion.ts` exporting `resultsRegionFlips(sheetOpen)`. It returns a boolean, and the class literal stays in the template, because Tailwind does not scan `.ts` files and would never emit a class built there
- [X] T074 [US1] Set the flips and the region factor of R12 in `src/KCC.Web/Features/Pages/RecipeSearch/RecipeSearchView.Component.vue` (the results section's `max-lg:[--blank-region:-1]` bound to `resultsRegionFlips(sheetOpen)`), with `src/KCC.Web/Features/Components/RecipeSearch/RecipeFilters.vue` passing a `flip` to its sheet if attribute fallthrough does not reach it (depends on T073)
- [X] T075 [US1] Set the flips of R12 in `src/KCC.Web/Features/Pages/RecipeDetail/RecipeDetailView.Component.vue`: `flip` on the "At a glance" `KccSheet`; `flip` on `VariantGrid` and `VariantList` through the props Phase 5 gave them; and `flip` on `VariantsEmptyState`, which declares no such prop, through attribute fallthrough to its root `KccSheet`
- [X] T076 [US1] In `src/KCC.Web` run `yarn test`, `yarn type-check`, `yarn lint` and `yarn format`; then `dotnet build KitchenCommandCenter.slnx` (Debug: `tests/KCC.E2ETests/Config/SiteProcess.cs` starts `src/KCC.Web/bin/<configuration>/net10.0/KCC.Web.dll`), `yarn build:all`, and `dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj -- --treenode-filter "/*/*/PileTests/*"`

**Checkpoint**: Search and recipe detail alternate in every state, server-rendered and browser-only.

---

## Phase 8: User Story 1 - alternation on account, account settings and the wizard (Priority: P1)

**Goal**: The account page, the settings page and every wizard step alternate in reading order (research R12).

**Independent Test**: `yarn test AccountView AccountSettingsView RecipeWizard CreateRecipeView AddVariantView stepFlip`,
then `PileTests` in the E2E suite.

### Tests for User Story 1 (write first, see them fail)

- [X] T077 [P] [US1] Create `tests/KCC.ViteTests/Features/Components/RecipeWizard/stepFlip.test.ts`: `stepFlips(step)` is `true` when `listTearFor(step - 1)` is odd and `false` when it is even, over every step the wizard counts
- [X] T078 [US1] Add the wizard's states to `tests/KCC.E2ETests/Features/Chrome/PileTests.cs`, signed in as the E2E member: the create-recipe and add-variant wizards on their first and second steps, at 375px and 1280px. It catches first the steps past the first, which only a browser reaches
- [X] T079 [P] [US1] In `tests/KCC.ViteTests/Features/Pages/Account/AccountView.test.ts` assert the recipe groups take `listTearFor(i + 1)`; "Favorites" carries `kcc-slip--flip` when the group count is even (0 and 2) and "Recent activity" when it is odd (1 and 3), and neither otherwise
- [X] T080 [P] [US1] In `tests/KCC.ViteTests/Features/Pages/Account/Settings/AccountSettingsView.test.ts` assert "Appearance" and "Sign out" carry `kcc-slip--flip` and "Profile" and "Password" do not; the existing count `sheets.match(/kcc-slip/g)` (line 60) would also count the modifiers, so count slip elements instead
- [X] T081 [P] [US1] In `tests/KCC.ViteTests/Features/Components/RecipeWizard/RecipeWizard.test.ts` assert, for the first step, the one the server renders: its step sheet carries `kcc-slip--flip` exactly when `stepFlips(1)` is `true`. Update the exact opening tags at `tests/KCC.ViteTests/Features/Pages/CreateRecipe/CreateRecipeView.test.ts:57` and `tests/KCC.ViteTests/Features/Pages/AddVariant/AddVariantView.test.ts:70`, which change again when the first step flips. Steps past the first, `ReadingState` and `SubmittedSheet` in place are browser-only: T078 catches them first

### Implementation for User Story 1

- [X] T082 [P] [US1] Create `src/KCC.Web/Features/Components/RecipeWizard/stepFlip.ts` exporting `stepFlips(step)`, built on `listTearFor`
- [X] T083 [P] [US1] Set the coming-soon flips from the group count's parity in `src/KCC.Web/Features/Pages/Account/AccountView.Component.vue`
- [X] T084 [P] [US1] Set `flip` on "Appearance" and "Sign out" in `src/KCC.Web/Features/Pages/Account/Settings/AccountSettingsView.Component.vue`
- [X] T085 [US1] Pass `stepFlips` of the current step from `src/KCC.Web/Features/Components/RecipeWizard/RecipeWizard.vue` to the step sheet and `ReadingState`, adding a `flip` prop to `src/KCC.Web/Features/Components/RecipeWizard/StartStep.vue`, `BasicsStep.vue`, `IngredientsStep.vue`, `InstructionsStep.vue`, `PhotoStep.vue`, `MatchStep.vue`, `VariantStep.vue`, `ReviewStep.vue` and `ReadingState.vue` where attribute fallthrough does not reach the sheet (depends on T082)
- [X] T086 [US1] In `src/KCC.Web` run `yarn test`, `yarn type-check`, `yarn lint` and `yarn format`; then `dotnet build KitchenCommandCenter.slnx` (Debug), `yarn build:all`, and `PileTests` with `dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj -- --treenode-filter "/*/*/PileTests/*"`

**Checkpoint**: Account, settings and the wizard alternate.

---

## Phase 9: User Story 1 - alternation on the variant detail page (Priority: P1)

**Goal**: The variant page alternates through its variable review run (research R12, A27). The server renders no
reviews, and `load(0)` runs on every mount (`src/KCC.Web/Features/Components/VariantDetail/VariantReviews.vue:103`),
so the server HTML alternates for the state it renders, and the siblings re-flip once reviews load.

**Independent Test**: `yarn test reviewsTrailingSign VariantReviews VariantSiblings VariantDetailView`, then
`VariantReviewsTests` and `PileTests` in the E2E suite.

### Tests for User Story 1 (write first, see them fail)

- [X] T087 [P] [US1] Create `tests/KCC.ViteTests/Features/Components/VariantDetail/reviewsTrailingSign.test.ts`: `reviewsTrailingSign({ isAuthenticated, count, loaded })` returns the sign of the review section's last sheet, or `null` when it renders none, for signed in and out, with reviews and none, with no reviews loaded and with one or more pages loaded (R12)
- [X] T088 [P] [US1] In `tests/KCC.E2ETests/Features/VariantReviews/VariantReviewsTests.cs`, once `[data-testid="reviews-list"]` shows, assert every review sheet carries `kcc-slip--flip` and the form's turn opposes the last review's. It catches first the loaded reviews, which only a browser renders
- [X] T089 [US1] Add the variant detail states to `tests/KCC.E2ETests/Features/Chrome/PileTests.cs` at 375px and 1280px: signed out and signed in, with reviews and without; on a variant with reviews the test waits for `[data-testid="reviews-list"]` before it walks, because the siblings can re-flip after hydration; and signed in, before and after "load more", with `/api/variant/*/reviews` stubbed through `Page.RouteAsync` to return two pages (A28). It catches first the siblings' re-flip after an emitted trailing sign
- [X] T090 [P] [US1] In `tests/KCC.ViteTests/Features/Components/VariantDetail/VariantReviews.test.ts` assert the state the server renders, with no reviews loaded: the form's flip opposes the summary when the count is above 0, else cook notes' `+`; nothing is emitted during setup. Loaded reviews' flips, the form's flip after a load and the emitted sign are browser-only: T088 and T089 catch them first
- [X] T091 [P] [US1] In `tests/KCC.ViteTests/Features/Components/VariantDetail/VariantSiblings.test.ts` assert each card carries `kcc-slip--flip` with `flip: true` and none without it
- [X] T092 [P] [US1] In `tests/KCC.ViteTests/Features/Pages/VariantDetail/VariantDetailView.test.ts` assert "At a glance" and `VariantNutrition` carry `kcc-slip--flip`, and the siblings flip in the four server states of R12, with no reviews rendered: signed out with reviews, flip; signed out with none, no flip; signed in with reviews, no flip; signed in with none, flip. The re-flip after an emitted trailing sign is browser-only: T089 catches it first

### Implementation for User Story 1

- [X] T093 [P] [US1] Create `src/KCC.Web/Features/Components/VariantDetail/reviewsTrailingSign.ts` with the pure function of R12
- [X] T094 [US1] Flip every review, set the form's flip from `reviewsTrailingSign`, and emit the trailing sign after each client `load()`, the mount's `load(0)` included, in `src/KCC.Web/Features/Components/VariantDetail/VariantReviews.vue` (depends on T093)
- [X] T095 [P] [US1] Add a `flip` prop passed to every card in `src/KCC.Web/Features/Components/VariantDetail/VariantSiblings.vue`
- [X] T096 [US1] Set the "At a glance" and nutrition flips and compute the siblings' flip from `isAuthenticated` and `reviewCount` with `loaded: 0` for the server's render, re-computing it on the reviews' emit, in `src/KCC.Web/Features/Pages/VariantDetail/VariantDetailView.Component.vue` (depends on T093 to T095)
- [X] T097 [US1] In `src/KCC.Web` run `yarn test`, `yarn type-check`, `yarn lint` and `yarn format`; then `dotnet build KitchenCommandCenter.slnx` (Debug), `yarn build:all`, and `VariantReviewsTests` and `PileTests` with `dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj -- --treenode-filter "/*/*/<Class>/*"`

**Checkpoint**: Every page alternates, in the server's render and after the browser loads more.

---

## Phase 10: User Story 1 - the teleported overlays and the rest of the walker (Priority: P1)

**Goal**: Cook mode and the photo viewer take the wide pair, proven first in their E2E files because an SSR string sees
nothing inside `<Teleport>`; then the walker covers the remaining page types, whose states the server renders and Vitest
already drives (research R9, R10, A16, A30).

**Independent Test**: `dotnet build KitchenCommandCenter.slnx` (Debug), `yarn build:all`, then
`dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj`.

### Tests for User Story 1 (write first, see them fail)

- [X] T098 [P] [US1] In `tests/KCC.E2ETests/Features/VariantDetail/CookModeTests.cs` assert cook mode's sheet holds exactly one `.kcc-blank`, carries `kcc-slip--wide` and is crisp (`--r` computes to 0)
- [X] T099 [P] [US1] In the photo-viewer flows of `tests/KCC.E2ETests/Features/Members/RecipeImportTests.cs` and `tests/KCC.E2ETests/Features/Members/RecipeWizardReviewTests.cs` assert the `[data-testid="photo-viewer"]` sheet holds exactly one `.kcc-blank`, carries `kcc-slip--wide` and is crisp

### Implementation for User Story 1

- [X] T100 [P] [US1] Pass `wide` to the sheet in `src/KCC.Web/Features/Pages/VariantDetail/CookMode.vue`
- [X] T101 [P] [US1] Pass `wide` to the sheet in `src/KCC.Web/Features/Components/RecipeWizard/PhotoViewer.vue`
- [X] T102 [US1] Add the remaining states of R9 to `tests/KCC.E2ETests/Features/Chrome/PileTests.cs` at 375px and 1280px: account; account settings; Home; registration complete; the error page. These are a regression check across pages (A30): each state is server-rendered and its flips were driven first by Vitest or the integration tests
- [X] T103 [US1] Build with `dotnet build KitchenCommandCenter.slnx` (Debug) and `yarn build:all`, then run `dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj`; where the walker finds two neighbours turning the same way on a server-rendered state, add the missing assertion to that page's SSR test first, then fix the page's flips

**Checkpoint**: User Story 1 is complete: every sheet on every page type rests on one blank, alternating.

---

## Phase 11: User Story 2 - tape holds only what is applied on top of paper (Priority: P1)

**Goal**: No sheet carries tape and the kit offers no way to tape one; every icon tile keeps or gains its strip
(FR-006, FR-007, D-9, A13, A25).

**Independent Test**: `yarn test pile KccSheet FeaturedRecipeCard DetailHero RecipeCard RecipeCardRow RecipesEmptyState VariantsEmptyState SmallHero SubmittedSheet kitClasses`, and `AccountPagesTests` in the integration suite.

### Tests for User Story 2 (write first, see them fail)

- [X] T104 [P] [US2] Add the tape rules to `tests/KCC.ViteTests/Features/Styles/pile.test.ts`: no `.kcc-tape` selector in `src/KCC.Web/Features/Styles/Torn/*.css` outside `.kcc-tilewrap > .kcc-tape` (and its dark-ramp rule); `.kcc-tilewrap--flow` is `position: relative` with no offset and no turn; every `kcc-tape` in `src/KCC.Web/Features/**/*.vue` and `**/*.cshtml` has a `kcc-tilewrap` parent
- [X] T105 [P] [US2] In `tests/KCC.ViteTests/Features/Components/Sheet/KccSheet.test.ts` reword "keeps the label and tape outside the torn wrapper" to the label alone and assert no sheet renders `kcc-tape`
- [X] T106 [P] [US2] In `tests/KCC.ViteTests/Features/Components/Recipe/FeaturedRecipeCard.test.ts` and `tests/KCC.ViteTests/Features/Components/Recipe/DetailHero.test.ts` change "with a strip on the sheet" to one `kcc-tape`, inside the tilewrap
- [X] T107 [P] [US2] Assert no `kcc-tape` in `tests/KCC.ViteTests/Features/Components/RecipeSearch/RecipesEmptyState.test.ts`, `tests/KCC.ViteTests/Features/Components/RecipeDetail/VariantsEmptyState.test.ts`, `tests/KCC.ViteTests/Features/Widgets/SmallHero.test.ts` and `tests/KCC.ViteTests/Features/Components/RecipeWizard/SubmittedSheet.test.ts`, and drop "taped" from their test names
- [X] T108 [P] [US2] In `tests/KCC.ViteTests/Features/Components/Recipe/RecipeCardRow.test.ts` assert the tile sits in a `kcc-tilewrap kcc-tilewrap--flow` inside the sheet with exactly one `kcc-tape`
- [X] T109 [P] [US2] In `tests/KCC.IntegrationTests/Features/Pages/AccountPagesTests.cs` assert the registration complete page renders no `kcc-tape`

### Implementation for User Story 2

- [X] T110 [US2] In `src/KCC.Web/Features/Styles/Torn/Kit.css` fold the base `.kcc-tape` rule and `.kcc-tilewrap .kcc-tape` into one `.kcc-tilewrap > .kcc-tape` rule, with its dark-ramp rule, and add `.kcc-tilewrap--flow`; the `--color-tape` token stays
- [X] T111 [US2] Add `kcc-tilewrap--flow` to the Classes table of `docs/brand/kit.md` and reword `kcc-tape` to "one strip over an applied tile, only inside `kcc-tilewrap`; a sheet is never taped", so `kitClasses.test.ts` stays green
- [X] T112 [US2] Remove the `tape` prop and its markup from `src/KCC.Web/Features/Components/Sheet/KccSheet.vue`
- [X] T113 [P] [US2] Remove the sheet's top-centre strip, keeping the tile's, in `src/KCC.Web/Features/Components/Recipe/FeaturedRecipeCard.vue` and `src/KCC.Web/Features/Components/Recipe/DetailHero.vue`
- [X] T114 [P] [US2] Drop `tape` from `src/KCC.Web/Features/Components/RecipeWizard/SubmittedSheet.vue`, `src/KCC.Web/Features/Components/RecipeSearch/RecipesEmptyState.vue`, `src/KCC.Web/Features/Components/RecipeDetail/VariantsEmptyState.vue` and `src/KCC.Web/Features/Widgets/Hero/SmallHero.Component.vue`
- [X] T115 [P] [US2] Remove the strip from `src/KCC.Web/Features/Pages/Account/RegistrationComplete/Index.cshtml`
- [X] T116 [P] [US2] Wrap the row's `AccentTile` and a new `kcc-tape` in `<div class="kcc-tilewrap kcc-tilewrap--flow">` inside the sheet's flow in `src/KCC.Web/Features/Components/Recipe/RecipeCardRow.vue` (contract "An applied tile inside a sheet")
- [X] T117 [P] [US2] Drop "and its tape" from the "the sheet and its tape sit flush" comments in `tests/KCC.ViteTests/Features/Pages/CreateRecipe/CreateRecipeView.test.ts`, `tests/KCC.ViteTests/Features/Pages/AddVariant/AddVariantView.test.ts` and `tests/KCC.ViteTests/Features/Pages/Account/Settings/AccountSettingsView.test.ts`
- [X] T118 [US2] State the one in-sheet tilewrap exception in `docs/brand/kit.md` Structure rule 2 (near lines 128 to 129) and invariant 8 (near line 336): label, tape and tilewrap stay siblings of `.kcc-torn` outside the clip and the filter, except a `.kcc-tilewrap--flow`, which sits in the sheet's content flow with its tape (research R11)
- [X] T119 [P] [US2] State the same exception in the Tear invariants of `CLAUDE.md` (near lines 202 to 203)
- [X] T120 [P] [US2] Add the exception to the violation at line 36 of `.claude/agents/brand-steward.md`: a `.kcc-tilewrap--flow` with its tape may sit in a sheet's content flow
- [X] T121 [P] [US2] Add the exception to the checklist line "label and tape outside both" (line 65) of `.claude/skills/loose-leaf/SKILL.md`
- [X] T122 [US2] In `src/KCC.Web` run `yarn test`, `yarn type-check`, `yarn lint` and `yarn format`, then the whole integration suite with `dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj`

**Checkpoint**: Tape exists only over tiles, and the four places that state the structure rule agree with the list row.

---

## Phase 12: User Story 3 - the header pad is two sheets tall (Priority: P2)

**Goal**: The pad keeps one page under its top sheet, centred and turned, with the menu cards between the two (FR-008,
FR-009, D-10, A14).

**Independent Test**: `yarn test AppHeader PadCards`, then the Chrome E2E tests in
`dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj`.

### Tests for User Story 3 (write first, see them fail)

- [X] T123 [P] [US3] In `tests/KCC.ViteTests/Features/Components/Header/AppHeader.test.ts` assert one `.pad-page` with the classes `pad-page kcc-slip kcc-slip--fill kcc-slip--wide kcc-tear-hero-2` and no `kcc-blank` in it or in `.pad-top`; change the stack test's tear extractor (line 114) from `/\bkcc-tear-\w+/` to `/\bkcc-tear-[\w-]+/` and expect 5 tears on a desktop and 4 on a phone, all different; assert the style block gives `.pad-top`, `.pad-cards` and `.pad-page` z-indices 5, 4 and 3, and the page no translate and `rotate(calc(-0.12deg + var(--blank-turn))) scale(var(--blank-scale))`
- [X] T124 [P] [US3] Add a pad test to `tests/KCC.E2ETests/Features/Chrome/PileTests.cs`: one `.pad-page` shows, and the computed z-indices of `.pad-top`, `.pad-cards` and `.pad-page` are 5, 4 and 3 in the header's stacking context, read from computed styles rather than a hit test (SC-003)

### Implementation for User Story 3

- [X] T125 [US3] In `src/KCC.Web/Features/Components/Header/AppHeader.Component.vue` remove pages 2 and 3 (near lines 393 to 396) and their `.pad-page--2` and `.pad-page--3` rules; give the one page the classes of T123, drop its offset rule `.pad-page--1`, and set its transform and the z-indices 5, 4 and 3 in the style block; correct the comment above `.pad-cards` (`AppHeader.Component.vue:455`), which says the cards hang between the first page and the two below it
- [X] T126 [US3] Run `yarn test` in `src/KCC.Web`, then `dotnet build KitchenCommandCenter.slnx` (Debug), `yarn build:all` and the E2E suite, confirming `tests/KCC.E2ETests/Features/Chrome/PadTests.cs`, `PhonePadTests.cs`, `MemberPadTests.cs` and `PadColdStartTests.cs` stay green (the menus slide out and the pad tucks with its page)

**Checkpoint**: The pad is a pile of two with the cards between.

---

## Phase 13: User Story 4 - the top sheet lifts off its pile (Priority: P3)

**Goal**: A hover or a focus lifts only the top sheet; a stacker's step back carries the blank; reduced motion stills
it all (FR-010 to FR-012, D-7, D-8, A11, A12).

**Independent Test**: `yarn test pile motion kitClasses RecipeCard RecipeCardRow FeaturedRecipeCard VariantGrid Stacker`, then `PileTests` in the E2E suite.

### Tests for User Story 4 (write first, see them fail)

- [X] T127 [P] [US4] Add the lift to `tests/KCC.ViteTests/Features/Styles/pile.test.ts`: `.kcc-slip--lift > :not(.kcc-blank)` transitions `translate` over `var(--default-transition-duration)` and also `outline-color 0.2s, outline-offset 0.2s`; `.kcc-slip--lift:is(:hover, :focus-within) > :not(.kcc-blank)` takes `translate: 0 -4px` only inside `@media (prefers-reduced-motion: no-preference)`; `tests/KCC.ViteTests/Features/Styles/motion.test.ts` stays green
- [X] T128 [P] [US4] Assert the slip carries `kcc-slip--lift` and none of `transition-transform`, `hover:-translate-y-1` or `focus-within:-translate-y-1` in `tests/KCC.ViteTests/Features/Components/Recipe/RecipeCard.test.ts`, `RecipeCardRow.test.ts` and `FeaturedRecipeCard.test.ts`, and on the add-variant slip in `tests/KCC.ViteTests/Features/Components/RecipeDetail/VariantGrid.test.ts`
- [X] T129 [P] [US4] Coverage (passes on Phase 2's `KccSheet`, which puts the blank inside the slip): in `tests/KCC.ViteTests/Features/Widgets/Stacker.test.ts` assert each stacked card's blank sits inside the slip that takes `[.stuck]:scale-95`, so it steps back with its sheet (FR-011)
- [X] T130 [P] [US4] Add lift tests to `tests/KCC.E2ETests/Features/Chrome/PileTests.cs`: hovering and focusing a recipe card on `/recipes/` moves its `.kcc-torn` up 4px and leaves its `.kcc-blank` box where it was (SC-004); with reduced motion emulated, neither moves

### Implementation for User Story 4

- [X] T131 [US4] Add `kcc-slip--lift` to `src/KCC.Web/Features/Styles/Torn/Kit.css` in `@layer components` as T127 states
- [X] T132 [US4] Add `kcc-slip--lift` to the Classes table of `docs/brand/kit.md` and name it in the Motion table's lift row, so `kitClasses.test.ts` stays green
- [X] T133 [P] [US4] Replace `transition-transform focus-within:-translate-y-1 hover:-translate-y-1` with `kcc-slip--lift` in `src/KCC.Web/Features/Components/Recipe/RecipeCard.vue`, `src/KCC.Web/Features/Components/Recipe/RecipeCardRow.vue` (on the new outer slip), `src/KCC.Web/Features/Components/Recipe/FeaturedRecipeCard.vue` and the add-variant slip in `src/KCC.Web/Features/Components/RecipeDetail/VariantGrid.vue`
- [X] T134 [US4] Run `yarn test`, `yarn type-check`, `yarn lint` and `yarn format` in `src/KCC.Web`, then `dotnet build KitchenCommandCenter.slnx` (Debug), `yarn build:all` and the E2E suite

**Checkpoint**: All four user stories work.

---

## Phase 14: Polish - the docs that the change makes untrue (D10)

**Purpose**: The brand docs state the new rule and the contract describes the pile (FR-014, research R11). The
structure-rule exception for `kcc-tilewrap--flow` landed in Phase 11.

- [X] T135 [P] Update `docs/brand/loose-leaf.md`: "a strip of tape where it was held down" (line 13), principle 1 "sometimes taped" (line 18), Handling (lines 107 to 108: top sheets tilt within ±1°, and the blank under each turns 1.5°, or 0.5° on a wide sheet, off it), "Six sheet tears, one hero tear and three tile tears" (line 112: two hero tears), and the Tape bullet (line 118): paper is piled two high, a sheet is never taped, tape holds applied items only
- [X] T136 [P] Update the rest of `docs/brand/kit.md`: the Structure examples gain the blank and lose sheet tape; rule 2 adds the blank as the slip's last child; the slip is `height: fit-content`; the direction factors (`--blank-dir`, `--blank-flip`, `--blank-region`) and the reading-order rule of R12; `#app` clips sideways overflow; Tears describes `tear-hero-2` and the blank mapping; the `KccSheet` props drop `tape` and add `piled`, `wide` and `flip`; the wide rule (the pad's page, the hero, any sheet wider than 700px at a 1280px viewport); the Enforcement table gains `tests/KCC.ViteTests/Features/Styles/pile.test.ts` rows (the pile's kit CSS and the lift; tape only inside a tilewrap, by CSS and by source scan; every Razor `kcc-slip` ends with a blank, by source scan), and its `tears.test.ts` row adds the blank mapping (`--blank-tear` and `--blank-dir` on every sheet preset)
- [X] T137 [P] Update `.claude/skills/loose-leaf/SKILL.md`: principle 1, the structure snippet with the blank, and the `KccSheet` props
- [X] T138 [P] Update `.claude/agents/brand-steward.md`: tape only inside a tilewrap; a sheet with no blank, a taped sheet or a sheet with two blanks is a violation, except the three pad pieces that hold no `.kcc-blank`: a pad card (`PadCard.vue`), the pad's top sheet (`.pad-top`, whose blank is its `.pad-page` sibling) and the `.pad-page` itself, which is that blank
- [X] T139 [P] Add the blank to the Tear invariants in `CLAUDE.md`: the blank is the slip's last child, outside the top sheet's clip and filter, with its own filter on `.kcc-blank` and its clip on the inner span
- [X] T140 Sweep `README.md`, `CLAUDE.md` and `docs/` for any other line this feature makes untrue (sheet tape, three pad pages, one hero tear, the lift utilities) and correct it

---

## Phase 15: Definition of done

**Purpose**: Rows D1 to D7 and D10 of the constitution, and SC-005. D8 and D9 do not apply (no change under `deploy/`,
`Dockerfile` or `docker-bake.hcl`).

- [X] T141 D1: `dotnet build KitchenCommandCenter.slnx -c Release` at the repository root, with no warning
- [X] T142 D2: `yarn build:all` at the repository root
- [X] T143 D3: in `src/KCC.Web` run `yarn type-check`, `yarn lint` and `yarn format`, and commit what lint and format change
- [X] T144 D5: run `yarn tears` in `src/KCC.Web` and confirm `git diff --exit-code src/KCC.Web/Features/Styles/Torn/Tears.css` is clean, so `Tears.css` matches `src/KCC.Web/Features/Torn/tears.ts` and both are committed together
- [X] T145 D4: `node tests/scripts/run.mjs` at the repository root after T141 and T142, with a no-op `open` first on `PATH`; all six suites pass
- [X] T146 D6: run the site (`dotnet run` in `src/KCC.Web`, https://localhost:58671), seed with `POST /api/dev/seed-recipes`, and walk every page in the table of `specs/002-paper-piles-not-tape/quickstart.md` step 4 in the Browser pane, in the light and the dark ramp, at 1280px and at a phone width, against `specs/002-paper-piles-not-tape/mockups/pile-light.jpg` and `pile-dark.jpg`
- [X] T147 SC-005: run the render-cost A/B of `specs/002-paper-piles-not-tape/quickstart.md` step 5 with a throwaway Playwright script in the session scratchpad, the `main` runs from a separate worktree of `origin/main` on the same machine and port (A29); five runs per ramp per branch; record the table for the pull request description. On a fail, stop and raise it to the owner before trading any part of the blank's look for speed (research R8)
- [X] T148 D7: run the `brand-steward` agent over the branch's diff (`git diff main...HEAD`) and fix every violation it reports, test first where it is a behaviour
- [X] T149 D10: confirm every line in `README.md`, `CLAUDE.md` and `docs/` that the change makes untrue is corrected, and check every task in this file

---

## Dependencies & Execution Order

### Phase dependencies

- **Phase 1 (Setup)**: none.
- **Phase 2 (Foundational)**: after Phase 1. Blocks every later phase.
- **Phases 3 and 4**: after Phase 2; independent of each other.
- **Phase 5**: after Phase 2. Phases 7 to 9 need its `flip` props.
- **Phase 6**: after Phase 2; independent of Phases 3 to 5.
- **Phase 7**: after Phases 3 and 5. **Phase 8**: after Phases 4 and 7. **Phase 9**: after Phases 3, 5 and 8.
- **Phase 10**: after Phases 3 to 9. Phase 7 creates `PileTests.cs` and the walker; Phases 8, 9, 10, 12 and 13
  extend it, so those phases run after Phase 7 and never side by side.
- **Phase 11 (US2)**: after Phases 3 to 6 and 8, whose files it changes again: the empty states (3), `SubmittedSheet`,
  `SmallHero` and the wizard and settings tests (4), `RecipeCardRow.vue` and the hero cards (5), the registration
  complete view and `pile.test.ts` (6), and `AccountSettingsView.test.ts` (8).
- **Phase 12 (US3)**: after Phases 10 and 11.
- **Phase 13 (US4)**: after Phases 5, 10, 11 and 12 (it lifts the split row and extends `PileTests.cs`).
- **Phases 11, 12 and 13 run in sequence, never side by side**: 11 and 13 both change
  `src/KCC.Web/Features/Styles/Torn/Kit.css`, `docs/brand/kit.md`, `tests/KCC.ViteTests/Features/Styles/pile.test.ts`,
  `RecipeCardRow.vue`, `FeaturedRecipeCard.vue` and their tests, and 12 and 13 both extend
  `tests/KCC.E2ETests/Features/Chrome/PileTests.cs`.
- **Phase 14 (Docs)**: after Phases 11 to 13, whose behaviour it describes.
- **Phase 15 (Done)**: after every other phase.

Run the phases in number order unless a dispatcher runs the independent pairs above side by side in separate
worktrees.

### User story dependencies

- **US1 (P1)**: needs only the foundation. It is the MVP.
- **US2 (P1)**: needs US1's split list row and the Razor blank; testable on its own through its Vitest and integration
  assertions.
- **US3 (P2)**: needs the foundation's `tear-hero-2`, `kcc-slip--wide` and `PadCard` exception, and runs after US2
  because the phases share files.
- **US4 (P3)**: needs US1's hand-written slips and blanks.

### Within each phase

- Tests are written and seen to fail before the implementation tasks.
- A kit class's CSS and its kit.md Classes row land in the same phase.
- The phase's closing task runs its suites green before the phase hands back.

## Parallel Examples

### Phase 2

```text
T003 tests/KCC.ViteTests/Features/Torn/tears.test.ts
T004 tests/KCC.ViteTests/Features/Styles/pile.test.ts
T005 tests/KCC.ViteTests/Features/Components/Sheet/KccSheet.test.ts
T006 tests/KCC.ViteTests/Features/Components/Header/PadCards.test.ts
```

### Phase 3 (US1)

```text
T016 to T024: the eight Vitest files and `VariantReviewsTests.cs`, side by side
T025 to T030: the six implementation files, side by side
```

### Phase 11 (US2)

```text
T113 FeaturedRecipeCard.vue + DetailHero.vue
T114 SubmittedSheet.vue + RecipesEmptyState.vue + VariantsEmptyState.vue + SmallHero.Component.vue
T115 RegistrationComplete/Index.cshtml
T116 RecipeCardRow.vue
T119 to T121: CLAUDE.md, brand-steward.md, SKILL.md (T118 edits kit.md after T111, so it runs alone)
```

## Implementation Strategy

### MVP first

1. Phases 1 and 2: the foundation. Every `KccSheet` already rests on a blank.
2. Phases 3 to 10: User Story 1. Stop and validate with `PileTests` and quickstart step 2: every sheet is piled and
   alternates.

### Incremental delivery

1. US1 (Phases 3 to 10), then US2 (Phase 11): the brand rule in full, both halves.
2. US3 (Phase 12): the pad matches.
3. US4 (Phase 13): the lift refinement.
4. Phases 14 and 15: the docs, then the definition of done, the render cost and the brand review, before card
   implement's review loop and the pull request.

## Notes

- No NEEDS CLARIFICATION or GATED marker is open. The constitution names no gated owner.
- Phases 3 to 9 split one story's surface sweep so each builder dispatch stays within one context.
- Phases 7 to 9 drive each browser-only state first with an E2E test; Phase 10's remaining walker states are a
  regression check over server-rendered pages (A30).
