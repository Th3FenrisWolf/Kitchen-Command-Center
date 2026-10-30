# Tasks: Type Scale

**Input**: `specs/004-type-scale/` — plan.md, spec.md, research.md (R1–R10), data-model.md, contracts/type-scale.md,
quickstart.md, mockups/README.md

**Scale**: Phases 1 to 8 built the first approval, 18 / 28. Phase 9 rescales it to 16 / 24, and its values, in
data-model.md, supersede every 18 / 28 value in the earlier phases.

**Tests**: required. Constitution IV: every behaviour change starts with a failing test.

**Paths**: `F/` is `src/KCC.Web/Features/`. `VT/` is `tests/KCC.ViteTests/Features/`. Run Vitest from `src/KCC.Web`
with `yarn test <name>`. Every yarn command needs `FONTAWESOME_NPM_AUTH_TOKEN`.

**Values**: take every size, line height, token and class name from data-model.md. Take every spacing conversion from
research.md R3, every heading mapping from R9 and every icon mapping from R7. A comment that names "the 24px rule" or
an old size is corrected or deleted where the task touches its file (Constitution VII).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: US1 body, US2 caps floor, US3 headings, US4 nav, US5 rules

---

## Phase 1: Setup

- [X] T001 Confirm the card-implement gates (FONTAWESOME_NPM_AUTH_TOKEN, KCC_E2E_MEMBER_USERNAME and
      KCC_E2E_MEMBER_PASSWORD set; .NET SDK from `global.json`; Node 24, Yarn 1, Playwright browsers), then run
      `yarn test` in `src/KCC.Web` and record the baseline pass count in the card log

---

## Phase 2: Foundational (blocks every story)

**Purpose**: the failing tests, the tokens and the 28px rule. The guard from T002 stays red until Phase 7 ends; each
story turns its slice of it green.

- [X] T002 [P] Write `VT/Styles/typeScale.test.ts`, the guard of research R8: scan every `.css`, `.vue`, `.cshtml` and
      `.ts` under `F/` except `F/Styles/Torn/Tears.css`, read files the way `VT/Styles/retiredTokens.test.ts` does, and
      fail with file, line and value on rules 1 to 5 of R8. Add a case that `F/Styles/TailwindConfig.css` declares at
      most eight `--text-*` sizes besides `--text-readout` (SC-002). Run it: it fails on today's code
- [X] T003 [P] Rewrite `VT/Styles/typography.test.ts` to pin data-model.md: each `--text-*` token and its
      `--line-height` in rem in `F/Styles/TailwindConfig.css`, `--text-*: initial`, `--control-md`, `--control-lg`,
      `--pill-sm`, `--icon-sm|md|lg`, `--bl: 1.75rem` and `--rule-drop` in `F/Styles/Torn/Tokens.css`; `body`,
      `input`, `select` and `textarea` reading `var(--text-body)`; bare `h1` → display, `h2` → section, `h3` →
      heading, `h4`–`h6` → title in `F/Styles/Typography.css`; `kcc-display` and `kcc-section` taking the smaller step
      below 768px and their own from `(min-width: 768px)` in `F/Styles/Torn/Kit.css`. Keep the font-file checks. Run
      it: it fails
- [X] T004 [P] Add `kcc-h3` and `kcc-h4` to the retired classes in `VT/Styles/retiredTokens.test.ts`. Run it: it fails
- [X] T005 Add the tokens and utilities to the `@theme static` block and the file of `F/Styles/TailwindConfig.css`:
      `--text-*: initial;` first, the seven text tokens with line heights, the control, pill and icon tokens, and the
      eleven rule utilities of research R3 as `@utility <prefix>-rule-* { <property>: calc(var(--bl) * --value(number)); }`
      (`space-y-rule-*` sets `margin-block-end` on `:where(& > :not(:last-child))`, as Tailwind 4.3.2's own `space-y` does)
- [X] T006 Set `--bl: 1.75rem` and add `--rule-drop: calc(var(--bl) / 2 + 0.38208 * var(--text-body))` in
      `F/Styles/Torn/Tokens.css`
- [X] T007 In `F/Styles/Torn/Kit.css`, place the sheet ruling with `--rule-drop` (research R5) instead of
      `calc(var(--pad, 24px) - 5px)`, keeping the 1px line at the band's end
- [X] T008 Run `yarn build:all` at the repository root, then `yarn test typography`: the token cases pass, the role
      cases still fail

**Checkpoint**: tokens exist, the rule is 28px, the guard is red.

---

## Phase 3: User Story 1 — Body text is easy to read (P1) 🎯 MVP

**Goal**: body, fields and pad lines at 18 / 28, every vertical gap in rule units, text on the rule.

**Independent test**: quickstart steps 2 and 4 on a recipe detail page and a form: every paragraph, list item and field
is 18px, and text sits on the ruling across blocks. The guard reports no rule-5 hit in the files this phase touched.

- [X] T009 [US1] In `F/Styles/Typography.css`, set `body` to `var(--text-body)` and `var(--text-body--line-height)`, and
      the field floor (`input`, `select`, `textarea`) to `var(--text-body)`
- [X] T010 [US1] In `F/Styles/Torn/Kit.css`, move `kcc-body`, `kcc-prose` and `kcc-link--icon` to the body token, and
      every rhythm literal of the inventory (margin-top and padding-left `var(--bl)`, the `0 0 48px` padding, the
      `calc(var(--bl) - 1px)` lines) to `var(--bl)` multiples
- [X] T011 [US1] In `F/Styles/Torn/Controls.css`, move the `kcc-field select` and `kcc-field--area textarea` fonts to
      the body token, the `kcc-well`, `kcc-check` rows and `kcc-steps` body to the body line, the 36px and 48px heights
      to `var(--control-md)` and `var(--control-lg)`, and the `calc(var(--pad, 24px) + var(--bl))` headroom to `var(--bl)`
- [X] T012 [P] [US1] Remove `text-[15px]` from the resize grip in `F/Components/Forms/TextAreaField.vue` so it inherits
      the body size, and convert the rhythm utilities of `F/Components/Forms/RangeSlider.vue` and `NumberStepper.vue`
- [X] T013 [P] [US1] Convert the vertical spacing (R3) in `F/Components/RecipeWizard/*.vue` and `stepSheet.ts`:
      ReviewStep, MatchStep, PhotoStep, IngredientsStep, StartStep, StepNav, PhotoStrip, LeaveConfirm, VariantStep,
      ReadAgainWarning, SubmittedSheet, RecipeWizard, ReadingState, PhotoViewer, CameraView, ValueMark, InstructionsStep
      and BasicsStep. First change `VT/Components/RecipeWizard/MatchStep.test.ts` from `leading-6` and 24/48/72px lines
      to the rule utilities and 28/56/84px, and see it fail
- [X] T014 [P] [US1] Convert the vertical spacing in `F/Pages/Account/AccountView.Component.vue`,
      `F/Pages/Account/Settings/AccountSettingsView.Component.vue`, `F/Pages/Account/Login/LoginView.Component.vue`,
      `F/Pages/Account/RegistrationComplete/Index.cshtml`, `F/Pages/AddVariant/AddVariantView.Component.vue` and
      `F/Pages/CreateRecipe/CreateRecipeView.Component.vue`
- [X] T015 [P] [US1] Convert the vertical spacing in `F/Pages/RecipeSearch/RecipeSearchView.Component.vue` and
      `F/Components/RecipeSearch/*.vue` (RecipeSearchHeader, RecipeFilters, RecipeResultsToolbar, AppliedFilterChips,
      RecipesEmptyState), the sticky `lg:top-[calc(1.5rem + var(--pad-offset))]` to `var(--bl)`, and first change
      `tests/KCC.E2ETests/Features/Chrome/PadTests.cs` line 160 from `24px` to `28px`. Keep
      `VT/Styles/stickyOffsets.test.ts` green
- [X] T016 [P] [US1] Convert the vertical spacing in `F/Pages/VariantDetail/*.vue` (VariantDetailView, CookMode,
      CookModeStep, StepTimer), `F/Components/VariantDetail/*.vue` (VariantReviews, VariantNutrition,
      VariantIngredients, VariantCookNotes, VariantInstructions, VariantSiblings),
      `F/Pages/RecipeDetail/RecipeDetailView.Component.vue`, `F/Components/RecipeDetail/*.vue` (VariantToolbar,
      VariantGrid, VariantList, VariantsEmptyState) and `F/Components/StarRating/*.vue`; the `space-y-[72px]` sections
      become `space-y-rule-3`
- [X] T017 [P] [US1] Convert the vertical spacing in `F/Components/Recipe/*.vue` (DetailHero, FeaturedRecipeCard,
      RecipeCard, RecipeCardRow, SegmentedControl), `F/Components/Wizard/WizardProgress.vue`,
      `F/Components/Breadcrumbs/Breadcrumbs.Component.vue` and `F/Components/Footer/AppFooter.Component.vue`
- [X] T018 [P] [US1] Convert the vertical spacing in `F/Pages/Home/Index.cshtml` (`gap-y-[72px]` → `gap-y-rule-3`),
      `F/Pages/Home/Blocks/*.cshtml` (StackerBlock, including its sticky top, CardGridBlock, RichTextBlock),
      `F/Pages/Error/Index.cshtml` and `F/Widgets/**` (SmallHero, Stacker, Card)
- [X] T019 [US1] Run `yarn build:all`, then `yarn test`: every test this phase changed passes, and the guard reports no
      rule-5 hit outside `F/Components/Header/`, which US4 converts. Check quickstart step 4 in the Browser pane on a recipe detail page and the create-recipe wizard

**Checkpoint**: body reads at 18px on a 28px rule everywhere.

---

## Phase 4: User Story 2 — No text is smaller than 14px (P1)

**Goal**: every Sono caps role at 14 / 28, and the small pills on `--pill-sm`.

**Independent test**: quickstart step 2 on the recipe search grid, a recipe detail page and a form: nothing renders under
14px, and badge rows wrap inside their card.

- [X] T020 [US2] In `F/Styles/Torn/Kit.css`, move `kcc-kick`, `kcc-lbl`, `kcc-label` and `kcc-unit` to `--text-caps`,
      the label's line to `var(--pill-sm)`, and let `.kcc-label i` inherit its size
- [X] T021 [US2] In `F/Styles/Torn/Controls.css`, move `kcc-btn`, `kcc-btn--lg`, `kcc-seg button`, `kcc-badge`,
      `.kcc-check .kcc-q`, `.kcc-stats .kcc-v small`, `.kcc-steps .kcc-n`, `.kcc-sheet > .kcc-stat` and `kcc-meta` to
      `--text-caps` (the `font` shorthands to `var(--text-caps)/var(--control-md)` and the badge to
      `var(--text-caps)/var(--pill-sm)`), and let every icon inside a button, segment, field or stat inherit its size
- [X] T022 [US2] Run `yarn build:all` and check the recipe search grid at 375px, 768px and 1280px in the Browser pane:
      badges wrap inside the card, the segmented controls scroll when narrow, and buttons keep their 36px and 48px
      heights

**Checkpoint**: the caps floor holds.

---

## Phase 5: User Story 3 — Headings step up in one scale (P1)

**Goal**: four heading roles by name, each its own step, the top two dropping a step below 768px; Cook Mode on the
scale.

**Independent test**: a page with every role (recipe detail with its siblings, Cook Mode, the account page) shows 44,
36, 28 and 22px at 1280px and 36, 28, 28 and 22px at 375px.

- [X] T023 [US3] Change every test that pins a heading class to the new name, per research R9, and see each fail:
      the Vitest files under `VT/` that assert `kcc-h3`, `kcc-h4` or a bare `kcc-secname` heading (RecipeCard,
      RecipeCardRow, FeaturedRecipeCard, DetailHero, StartStep, MatchStep, ReviewStep, SubmittedSheet, PadCards,
      Stacker, SmallHero, Card, AccountView, CookModeStep, RecipeSearchHeader, RecipesEmptyState, VariantsEmptyState,
      VariantGrid, VariantSiblings, VariantReviews and any other `grep -rl "kcc-h[34]\|kcc-secname" VT` finds),
      `tests/KCC.IntegrationTests/Features/Pages/Home/HomeSectionsPageTests.cs` lines 60 to 63,
      `tests/KCC.E2ETests/Features/HomePage/HomePageTests.cs` lines 30 and 31 and
      `tests/KCC.E2ETests/Features/Chrome/PileTests.cs` line 273
- [X] T024 [US3] In `F/Styles/Torn/Kit.css`, replace `kcc-h3` and `kcc-h4` with `kcc-display`, `kcc-section`,
      `kcc-heading` and `kcc-title` (mobile first, the step at `(min-width: 768px)`), drop the size rule of
      `.kcc-secname h2`, and move `kcc-hand` to the title token. In `F/Styles/Torn/Controls.css`, move
      `.kcc-stats .kcc-v` to the heading token. In `F/Styles/Typography.css`, map bare `h1`–`h6` to the roles (R2).
      In `docs/brand/kit.md`'s Classes table, replace the `kcc-h3`, `kcc-h4` row with the four role rows, so
      `VT/Styles/kitClasses.test.ts` stays green
- [X] T025 [P] [US3] Rename the heading classes in the Vue files of R9: `F/Components/RecipeWizard/` (SubmittedSheet,
      ReviewStep, MatchStep, StartStep), `F/Components/Recipe/` (FeaturedRecipeCard, DetailHero, RecipeCard,
      RecipeCardRow), `F/Components/RecipeSearch/` (RecipeSearchHeader, RecipesEmptyState), `F/Components/Header/`
      (KitchenCard, SearchCard), `F/Components/RecipeDetail/` (VariantsEmptyState, VariantGrid),
      `F/Components/VariantDetail/` (VariantSiblings, VariantReviews), `F/Pages/Account/AccountView.Component.vue`,
      `F/Pages/RecipeSearch/RecipeSearchView.Component.vue`, `F/Pages/RecipeDetail/RecipeDetailView.Component.vue`,
      `F/Widgets/Hero/SmallHero.Component.vue` and `F/Widgets/Stacker/Stacker.Component.vue`. Every `kcc-secname`
      heading takes `kcc-section`
- [X] T026 [P] [US3] Rename the heading classes in the Razor views: `F/Pages/Home/Blocks/CardGridBlock.cshtml`,
      `F/Pages/Home/Blocks/StackerBlock.cshtml`, `F/Pages/Account/RegistrationComplete/Index.cshtml` and
      `F/Pages/Error/Index.cshtml`
- [X] T027 [US3] Put Cook Mode on the scale (FR-017): first change `VT/Pages/VariantDetail/StepTimer.test.ts` (lines 20
      and 33) and the CookModeStep and CookMode tests to the new classes and see them fail; then
      `F/Pages/VariantDetail/CookModeStep.vue` step text → `kcc-section`, `CookMode.vue` servings `text-[22px] leading-6`
      → `text-title`, `StepTimer.vue` readout → `kcc-num text-readout` and stopwatch `text-2xl` → `kcc-icon-sm`
      (add `kcc-icon-sm|md|lg` to `F/Styles/Torn/Kit.css` and to kit.md's Classes table here if T033 has not)
- [X] T028 [US3] Run `yarn build:all`, `yarn test`, then
      `dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj`: the heading tests pass, and
      `grep -rn "kcc-h[34]" src tests docs .claude CLAUDE.md` finds nothing

**Checkpoint**: the heading roles are in place.

---

## Phase 6: User Story 4 — The nav reads like the pad it is (P2)

**Goal**: the pad's menu items in Hazelnut at body size, mixed case, with its counts, keys, glyphs and lines on the
scale.

**Independent test**: at 1280px and 1024px the pad bar holds Recipes, My Kitchen, the search field and New Recipe on one
line; at 375px the phone menu holds every item; menu items are 18px Hazelnut in mixed case.

- [X] T029 [US4] First change the header tests under `VT/Components/Header/` that pin `kcc-kick` on `.pad-menu` or the
      old pad sizes, so they expect no `kcc-kick` on `.pad-menu`, and see them fail
- [X] T030 [US4] In `F/Components/Header/AppHeader.Component.vue`: drop `kcc-kick` from both `.pad-menu` buttons (desk
      and phone bars) and style `.pad-menu` as Hazelnut `var(--text-body)`, no transform, no letter spacing; the chevron
      inherits; `.pad-count` takes `--text-caps` on `var(--pill-sm)`; `.pad-glyph` takes `--icon-sm`; the tuck offset,
      the `calc(100dvh - 96px)` and the scroll padding read `var(--bl)` where they follow the rule
- [X] T031 [P] [US4] In `F/Components/Header/PadLines.vue`, move `.pad-line` to the body token, let its icon inherit,
      and move its number to `--text-caps`; in `SearchForm.vue`, move `.pad-key` to `var(--text-caps)/var(--pill-sm)`;
      convert the rule-keyed gaps and comments in `PadCard.vue`, `SearchCard.vue`, `RecipesCard.vue`, `KitchenCard.vue`
      and `MenuCard.vue`
- [X] T032 [US4] Run `yarn build:all` and check the pad in the Browser pane at 1280px, 1024px, 768px and 375px, in both
      ramps, with the Recipes and My Kitchen cards open: the current section's marker underline still marks it, and no
      item is cut off (SC-005)

**Checkpoint**: the nav reads at body size.

---

## Phase 7: User Story 5 — Every text role follows one set of rules (P2)

**Goal**: icons on named sizes, the guard green, and the brand docs telling the truth.

**Independent test**: `yarn test typeScale` passes; adding `text-[15px]` anywhere fails it with the file and line; the
brand type table lists every role.

- [X] T033 [US5] Put the standalone icons on named sizes (R7): `kcc-icon-sm|md|lg` in `F/Styles/Torn/Kit.css` (if T027
      did not), `.kcc-tile` and `.kcc-tile--lg` glyphs to `--icon-sm` and `--icon-md`,
      `F/Components/RecipeSearch/RecipesEmptyState.vue` and `F/Components/RecipeDetail/VariantsEmptyState.vue`
      `text-5xl` → `kcc-icon-lg`, `F/Components/RecipeDetail/VariantGrid.vue` `text-4xl` → `kcc-icon-md`; change any
      test that pins the old glyph classes first
- [X] T034 [US5] Run `yarn test typeScale` and fix every remaining hit at its source until it passes; never widen the
      guard's allowances to make it pass
- [X] T035 [P] [US5] Rewrite `docs/brand/kit.md`: Files (Typography.css), Tokens (`--bl` 28px, `--rule-drop`, the
      text, control, pill and icon tokens), Type, the Classes rows for every role, `kcc-icon-*` and the controls, the
      spacing paragraph (rule utilities), and an Enforcement row for `typeScale.test.ts`; link
      `specs/004-type-scale/contracts/type-scale.md`'s rules into Type
- [X] T036 [P] [US5] Rewrite the Type section and table of `docs/brand/loose-leaf.md` (lines 81 to 95) from
      data-model.md, and its rhythm lines (20, 127, 131, 132): the 28px rule, gaps and section spacing in rules
- [X] T037 [P] [US5] Correct `.claude/skills/loose-leaf/SKILL.md` (lines 19, 35, 59 to 61),
      `.claude/agents/brand-steward.md` (line 82: flag any `leading-*` and old-rule spacing, point at the guard) and
      `.claude/agents/kit-builder.md` for the new roles, tokens and rule utilities
- [X] T038 [P] [US5] Change "a 24px rule" to "a 28px rule" in the Loose Leaf section of `CLAUDE.md`

**Checkpoint**: the guard passes and the docs match the code.

---

## Phase 8: Polish and definition of done

- [X] T039 In `src/KCC.Web`: `yarn type-check`, `yarn lint`, `yarn format`; commit what lint and format change (D3)
- [X] T040 `dotnet build KitchenCommandCenter.slnx -c Release` (D1) and `yarn build:all` at the root (D2)
- [X] T041 Quickstart steps 2, 3 and 4 in the Browser pane against the running site, on the recipe search, a recipe
      detail page, a variant detail page, Cook Mode, the create-recipe wizard, the account pages, Home and the error
      page, each in the light and the dark ramp (D6, SC-001, SC-003, SC-005)
- [X] T042 Run the `brand-steward` agent over the branch diff and fix every violation it reports (D7, SC-007)
- [X] T043 `node tests/scripts/run.mjs` at the root with a no-op `open` first on `PATH`: all six suites pass (D4)
- [X] T044 `grep -rnE "24px rule|15px|10\.5px|kcc-h[34]" README.md CLAUDE.md docs .claude` and correct every line the
      change made untrue (D10)

---

## Phase 9: Rescale to 16 / 24 (owner, 2026-10-19)

**Goal**: the scale approved in `mockups/rescale.html`: 16px body on a 24px rule, headings 20, 24, 32 and 40px, the
display 36px below 768px, caps 14px, readout 64 / 72. Controls stay 36 and 48px, icons 24, 36 and 48px.

**Independent Test**: quickstart steps 2 to 4 report only 14, 16, 20, 24, 32, 36 (phone display) and 40, plus 64 on the
timer, and body text sits on the 24px ruling.

- [X] T045 Change the tests that pin the 18 / 28 values first and see each fail: `VT/Features/Styles/typography.test.ts`
      (token table, `--bl` 1.5rem, the phone step: display 36 / 48 below 768px, section unchanged),
      `VT/Features/Components/Forms/TextAreaField.test.ts` (default height, three 24px rules), `MatchStep.test.ts`,
      `VariantGrid.test.ts`, `AccountView.test.ts` and `RecipeSearchView.test.ts` names that say 28px, and
      `tests/KCC.E2ETests/Features/Chrome/PadTests.cs` (sticky filters `top` 24px)
- [X] T046 In `F/Styles/TailwindConfig.css` set the text tokens to the data model's values and add
      `--text-display-phone` (2.25rem / 3rem); in `F/Styles/Torn/Tokens.css` set `--bl: 1.5rem`
- [X] T047 Put the phone step on the new token in `F/Styles/Torn/Kit.css` and `F/Styles/Typography.css`: below 768px
      `kcc-display` and `h1` take `--text-display-phone`, and `kcc-section` and `h2` keep the section size at every width
- [X] T048 Measure every ruled sheet on the running site at 375, 768, 1024 and 1280px with the off-rule audit, and
      retune each spot tuned by hand for 28px until body text sits on the 24px ruling: `MatchStep.vue` pill,
      `IngredientsStep.vue` and `PhotoStrip.vue` row templates, `LoginView` field boxes, `CookMode.vue` rows,
      `VariantCookNotes.vue`, `TextAreaField.vue` default height, and in the pad (`AppHeader.Component.vue`,
      `PadCard.vue`, `SearchForm.vue`) the search field widths, `.pad-count` and `.pad-glyph`
- [X] T049 [US4] Bring the member's New recipe label back (FR-019): in `F/Components/Header/AppHeader.Component.vue`,
      show the label at every width where the search field shows its full placeholder, and keep it icon-only only below
      that; measure the bar at 768, 900 and 1024px in both ramps
- [X] T050 [P] Correct the docs to 16 / 24: `docs/brand/kit.md` (Tokens, Type), `docs/brand/loose-leaf.md` (type table,
      rhythm, and a history line for the rescale), `.claude/skills/loose-leaf/SKILL.md`, `.claude/agents/brand-steward.md`,
      `.claude/agents/kit-builder.md`, and "28px rule" in `CLAUDE.md`
- [X] T051 Definition of done again: T039, T040, quickstart steps 2 to 5 against the running site in both ramps, the
      `brand-steward` agent over the branch diff, and `node tests/scripts/run.mjs` with a no-op `open` on `PATH`
      (2026-10-30: D1's only warnings are NU1903 on SixLabors.ImageSharp 3.1.12, GHSA-v76p-62qx-wwq2, published after
      this card and fixed in its own pull request)

---

## Dependencies

- Phase 2 blocks every story. T002 to T004 run in parallel; T005 to T007 run in order.
- US1 (Phase 3) comes first: it moves the rhythm under every later check. Its T012 to T018 run in parallel.
- US2 and US3 depend on Phase 2 only and touch `Kit.css` and `Controls.css` in different rules; run them one after the
  other, because both edit those two files.
- US4 depends on US2 (caps token on the pad's counts and keys).
- US5 depends on all four: its T034 makes the guard green across the codebase.
- Phase 8 depends on every phase. Phase 9 follows Phase 8: T045 first, then T046 and T047, then T048 and T049; T050
  runs beside them.

## Parallel examples

- Phase 2: T002, T003 and T004 at once, three new or rewritten test files.
- US1: T012 to T018 at once, each a separate set of Vue and Razor files.
- US3: T025 (Vue) and T026 (Razor) at once, after T024.
- US5: T035 to T038 at once, four separate docs.

## Implementation strategy

1. Phases 1 and 2, then US1: the MVP. The site reads at 16px on a 24px rule, and the guard shows what remains.
2. US2 and US3 complete the scale. US4 finishes the header. US5 turns the guard green and rewrites the brand docs.
3. One pull request: card implement recomposes the branch into commits by story before it opens.
