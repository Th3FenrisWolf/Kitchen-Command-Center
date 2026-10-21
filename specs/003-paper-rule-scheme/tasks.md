# Tasks: Paper Rule Scheme

**Input**: `specs/003-paper-rule-scheme/`: plan.md, spec.md, research.md (R1–R11), data-model.md,
contracts/paper-rule.md, quickstart.md, mockups/README.md and mockups/workbench-look.html

**Tests**: required. Constitution IV: every behaviour change starts with a failing test.

**Paths**: `F/` is `src/KCC.Web/Features/`. `VT/` is `tests/KCC.ViteTests/Features/`. `ET/` is
`tests/KCC.E2ETests/Features/`. Run Vitest from `src/KCC.Web` with `yarn test <name>`. Every yarn command needs
`FONTAWESOME_NPM_AUTH_TOKEN`. Build with `yarn build:all` at the root, never one bundle alone.

**Values**: take every token value, metric and shift from data-model.md, and every selector, formula and site list
from research.md. `mockups/workbench-look.html` holds working CSS for the ruling, the margin rule, the grain, the shim,
the controls and the fields: port its rules into the kit's names, never its `pv-` classes. Every new kit rule goes in
`@layer components`. A comment states a non-obvious reason or is not written (Constitution VII).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: US1 text on the rule, US2 every role on the rule, US3 the workbench look, US4 the reusable scheme

---

## Phase 1: Setup

- [X] T001 Confirm the card-implement gates (FONTAWESOME_NPM_AUTH_TOKEN, KCC_E2E_MEMBER_USERNAME and
      KCC_E2E_MEMBER_PASSWORD set; .NET SDK from `global.json`; Node 24, Yarn 1, Playwright browsers), then run
      `yarn test` in `src/KCC.Web` and record the baseline pass count in the card log

---

## Phase 2: Foundational (blocks every story)

**Purpose**: the failing guard, the pinned metrics and the tokens every story reads.

- [X] T002 Write `VT/Styles/paperRule.test.ts`, reading stylesheets the way `VT/Styles/typography.test.ts` does. One
      `describe` per research decision, so each story turns its own block green: R1 (every `@font-face` in
      `F/Styles/Typography.css` has the three overrides with data-model.md's values; `--k-hazelnut`, `--k-casual`,
      `--k-sono` in `F/Styles/Torn/Tokens.css`; `--rule-drop` reads `--k-hazelnut`), R2 (the `.kcc-sheet` ruling:
      `linear-gradient`, `background-origin: content-box`, position from `--rule-drop` and `--rule-offset`, no `--pad`
      term; `--rule-offset: 2px`; `.kcc-sheet--blank` with no image), R3 (the shim rule: its `translate` formula, its
      role list, `--k` per family), R4 (`--color-rule` and `--color-margin` in both ramps), R5 (`.kcc-sheet--margin`
      `::before`, its padding, the container on its slip, the 480px query), R6 (`.kcc-row`; the control shift on
      `.kcc-sheet .kcc-btn` and `.kcc-seg`; paper fill on ghost, segment and field), R7 (field as a line: no radius or
      ring, the underline position, `::before` X with `--color-margin`, the area without a box, the error underline),
      R8 (`--control-sm: 2rem`, `.kcc-btn--icon` at `--control-sm`, no `.kcc-btn--lg.kcc-btn--icon` rule), R9
      (`.kcc-sheet::after` grain with its tokens). Run it: it fails
- [X] T003 [P] Add `kcc-field--noicon` to the retired classes in `VT/Styles/retiredTokens.test.ts`. Run it: it fails
- [X] T004 Add the overrides of research R1 to every `@font-face` in `F/Styles/Typography.css` (Hazelnut ×4, APCasual
      ×2, Sono ×2)
- [X] T005 In `F/Styles/Torn/Tokens.css`: add `--k-hazelnut`, `--k-casual`, `--k-sono`, `--rule-offset`,
      `--sheet-grain-op` and `--sheet-grain-blend`; make `--rule-drop` read `--k-hazelnut`; under
      `:root[data-theme='dark']` set the dark `--color-rule`, `--color-margin`, grain opacity and blend
- [X] T006 In `F/Styles/TailwindConfig.css` `@theme static`: `--color-rule` to the light feint blue, a new
      `--color-margin`, and `--control-sm: 2rem`
- [X] T007 `yarn build:all`, then `yarn test paperRule typography contrast`: the R1, R4 and R8 token cases pass,
      `typography` and `contrast` stay green

---

## Phase 3: User Story 1 — Every line of text sits on the rule (P1) 🎯 MVP

**Goal**: body, headings, Sono labels and centred labels sit 2px above a rule on every sheet.

**Independent test**: quickstart step 2 on a recipe page, a recipe search page, a variant page and a form.

- [X] T008 [US1] Write `ET/Chrome/RuleTests.cs`, modelled on `ET/Chrome/PileTests.cs`: for the recipe page, a variant
      page, recipe search and the create recipe wizard, at 375px and 1280px, run a page script that, for every
      `.kcc-sheet` that is ruled, prepends a `display:inline-block; width:0; height:0` marker to each text block and
      reads its `top` as the baseline (with a style that sets `transform: none; rotate: none` on `.kcc-slip` and its
      descendants during the read, and `kcc-meta` set to `display: block`, because a flex container turns the marker
      into a flex item), and asserts each baseline lies `--rule-drop` below a whole rule of the
      content box, within 0.75px (SC-001, SC-003). A second test asserts the same for the FR-017 labels and the Sono
      text inside their controls (SC-008). Run it: it fails
- [X] T009 [US1] Replace the `.kcc-sheet` ruling in `F/Styles/Torn/Kit.css` with research R2, set `--k:
      var(--k-hazelnut)` on `.kcc-sheet`, and add `.kcc-sheet--blank`. Correct the ruling comment
- [X] T010 [US1] Add the shim rule of research R3 to `F/Styles/Torn/Kit.css`: `--own`, the signed `translate`, `--lh`
      per role and per `text-<role>` utility, `--k` per face (APCasual roles and `font-casual`; Sono roles, `kcc-num`
      and `font-mono`)
- [X] T011 [US1] Add `.kcc-row` (research R6) to `F/Styles/Torn/Kit.css`, and the control shift and paper fills to
      `F/Styles/Torn/Controls.css`. Correct the baseline comment at `Controls.css:490`
- [X] T012 [P] [US1] Make the FR-017 rows `kcc-row` in place of `flex items-center gap-3`:
      `F/Components/RecipeSearch/RecipeResultsToolbar.vue:27`, `F/Components/RecipeDetail/VariantToolbar.vue:47`,
      `F/Components/VariantDetail/VariantIngredients.vue:58` (drop `min-h-rule-2`: the row fills its rules),
      `F/Components/RecipeWizard/PhotoStep.vue:108`, `F/Components/RecipeWizard/IngredientsStep.vue:79`; and the Add
      Variant tile in `F/Components/RecipeDetail/VariantGrid.vue:33-35`, its circle and title in a `kcc-row`. Update any
      test in `VT/` that pins the old row classes
- [X] T013 [US1] `yarn build:all`; `yarn test paperRule` (R2, R3, R6 blocks green); run `RuleTests` and move every
      block it reports off the rule into a `kcc-row` or onto a line start, until it passes

---

## Phase 4: User Story 2 — Every role on the type scale sits on the rule (P1)

**Goal**: every role of spec 004, including the readout, sits on the rule; phones do not scroll sideways.

**Independent test**: quickstart steps 2 and 4 on the home page, cook mode, the account pages and the wizard.

- [X] T014 [US2] Extend `ET/Chrome/RuleTests.cs` to cook mode, an account page and the home page, and add a 375px case
      that asserts `document.documentElement.scrollWidth` equals the viewport width on each (SC-004). Run it: the new
      pages fail where a role sits off the rule
- [X] T015 [US2] Fix what T014 reports in `F/Styles/Torn/Kit.css` (role coverage of the shim) or in the component that
      breaks the rhythm, without a pixel nudge. The readout in `F/Pages/VariantDetail/CookMode.vue` and
      `F/Pages/VariantDetail/StepTimer.vue` takes the shim through `text-readout`
- [X] T016 [US2] Check the header pad (`F/Components/Header/PadCard.vue`, `F/Components/Header/AppHeader.Component.vue`)
      at 1024px and 375px: it fits its bar (spec 004 FR-019), its search sits on its rule. Fix any overlap the control
      shift caused with a `kcc-row` or by keeping the pad's controls outside the shift's selector

---

## Phase 5: User Story 3 — The workbench look on paper (P2)

**Goal**: feint blue rules, the margin rule on reading sheets, the grain, 32px icon buttons and fields written on
the line with the red X, in both ramps.

**Independent test**: quickstart step 3 in both ramps, against `mockups/workbench-look-*.jpg`.

- [X] T017 [P] [US3] Change `VT/Components/Forms/InputField.test.ts`, `TextAreaField.test.ts`,
      `VT/**/VariantReviews.test.ts` and `VT/**/VariantCookNotes.test.ts` to expect no `kcc-field--noicon`, no `icon`
      prop and no field `<i>`, and `TextAreaField` heights in whole rules. Add a `KccSheet` test that `margin` adds
      `kcc-sheet--margin` and `blank` adds `kcc-sheet--blank`. Run them: they fail
- [X] T018 [US3] Add the grain (research R9) and the margin rule with its slip container and 480px query (R5) to
      `F/Styles/Torn/Kit.css`
- [X] T019 [US3] Add `margin` and `blank` props to `F/Components/Sheet/KccSheet.vue`
- [X] T020 [US3] Rewrite the field rules in `F/Styles/Torn/Controls.css` per research R7: the line, the underline, the
      X, focus, error, the area without a box, the forced-colors rule for the underline; and `.kcc-btn--icon` at
      `--control-sm` (R8), removing the `--lg` icon rule
- [X] T021 [US3] `F/Components/Forms/InputField.vue`: drop the `icon` prop and `kcc-field--noicon`.
      `F/Components/Forms/TextAreaField.vue`: snap the drag-resized height to whole `--bl` steps.
      `F/Components/Forms/NumberStepper.vue`: drop `kcc-field--noicon`
- [X] T022 [P] [US3] Remove the leading `<i>` from the fields in `F/Components/RecipeSearch/RecipeSearchHeader.vue:43`,
      `F/Components/Header/SearchForm.vue:19`, `F/Components/RecipeDetail/VariantToolbar.vue:41` and
      `F/Components/RecipeWizard/MatchStep.vue:67`
- [X] T023 [P] [US3] Pass `margin` to every sheet in research R5's reach list (recipe, variant, wizard, account, login,
      settings, cook mode), and add `kcc-sheet--margin` to the sheet in
      `F/Pages/Account/RegistrationComplete/Index.cshtml`
- [X] T024 [US3] `yarn build:all`; `yarn test paperRule retiredTokens kitClasses contrast` and the T017 tests pass;
      `RuleTests` stays green with the margin padding and the fields as lines

---

## Phase 6: User Story 4 — A reusable scheme authors can apply (P2)

**Goal**: an author gets text on the rule from the contract alone, in Vue or Razor; the docs say how.

**Independent test**: SC-006, a sheet built from the roles alone sits on the rule.

- [X] T025 [US4] Add a `RuleTests` case on a Razor sheet (`F/Pages/Account/RegistrationComplete/Index.cshtml` or
      `F/Pages/Error/Index.cshtml`) and on a list whose rows run on (the variant ingredients), asserting the same
      baselines (FR-013, US4 scenario 3). Fix what fails
- [X] T026 [P] [US4] `docs/brand/kit.md`: a "Paper rule" section from contracts/paper-rule.md and data-model.md (the
      metrics and k, the shim, the 2px offset, ruled and blank, the margin rule and its reach, `kcc-row`, controls on the
      line, fields as lines with the X, 32px icon buttons); update the Tokens table (`--color-rule`, `--color-margin`,
      `--control-sm`, `--k-*`, `--rule-offset`), Structure rule 4, the Classes table (`kcc-sheet--margin`,
      `kcc-sheet--blank`, `kcc-row`, `kcc-field`, `kcc-field--area`, `kcc-field--error`, `kcc-btn--icon`; remove
      `kcc-field--noicon`), States (field focus and error) and Vue primitives (`KccSheet` props, `InputField`), and
      Enforcement (`paperRule.test.ts`, `RuleTests.cs`)
- [X] T027 [P] [US4] `docs/brand/loose-leaf.md`: the stock rule colour rows, "Pencil ruling" becomes the feint blue
      ruling with the margin rule's reach and the grain, the field and control lines, a Don't for a pixel nudge, and a
      2026-10-19 decisions-log entry
- [X] T028 [P] [US4] `.claude/skills/loose-leaf/SKILL.md`, `.claude/agents/brand-steward.md` and
      `.claude/agents/kit-builder.md`: the ruling, the margin rule's reach, `kcc-row`, fields as lines and the shim
      rule (no nudges)

---

## Phase 7: Polish and definition of done

- [X] T029 In `src/KCC.Web`: `yarn type-check`, `yarn lint`, `yarn format`; commit what lint and format change (D3)
- [X] T030 `dotnet build KitchenCommandCenter.slnx -c Release` (D1) and `yarn build:all` at the root (D2)
- [X] T031 Quickstart step 3 in the Browser pane against the running site, each page in the light and the dark ramp
      (D6, SC-005)
- [X] T032 Run the `brand-steward` agent over the branch diff and fix every violation it reports (D7, SC-007)
- [X] T033 `node tests/scripts/run.mjs` at the root with a no-op `open` first on `PATH`: all six suites pass (D4)
- [X] T034 `grep -rnE "pencil rul|kcc-field--noicon|color-rule|leading icon" README.md CLAUDE.md docs .claude` and
      correct every line the change made untrue (D10)

---

## Dependencies

- Phase 2 blocks every story. T002 and T003 run in parallel; T004 to T006 run in order.
- US1 (Phase 3) comes first: the ruling, the shim and `kcc-row` are what every later check measures against.
- US2 depends on US1. It widens the measurement and fixes coverage.
- US3 depends on Phase 2 and on US1's T009 to T011, because it edits `Kit.css` and `Controls.css` after them.
- US4 depends on US1 to US3: the docs describe the finished scheme.
- Phase 7 depends on every phase.

## Parallel examples

- Phase 2: T002 and T003 at once.
- US3: T017, T022 and T023 at once, after T018 to T021.
- US4: T026, T027 and T028 at once, four separate docs.

## Implementation strategy

1. Phases 1 and 2, then US1: the MVP. Every text block sits on the rule, measured in a browser.
2. US2 widens the proof to every role and page. US3 brings the approved look. US4 writes the contract into the docs.
3. One pull request: card implement recomposes the branch into commits by story before it opens.
