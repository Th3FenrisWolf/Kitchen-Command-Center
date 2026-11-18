---

description: "Task list for Recipe Import from Photos, Phase 2 - The Details Step"
---

# Tasks: Recipe Import from Photos, Phase 2 - The Details Step

**Input**: Design documents from `/specs/010-recipe-details-step/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md),
[contracts/](contracts/), [quickstart.md](quickstart.md)

**Tests**: Constitution IV requires test-first work. Every behaviour task follows a task that writes its failing test
and sees it fail. No test calls the paid reading service, and no test host sets an API key: the suites use the
stand-in reader and the integration host's `FakeRecipePhotoReader`. The integration tests stay on the one shared
`UmbracoSite`.

**Organization**: One `##` phase is one builder's work in one context. US1 (typed Details) runs first, US3's server
part rides with US1's submission, and US2 (the import's suggestions) follows. Every `[PERSON]` row and every row that
waits on one sits in the last two phases, so all other work builds first.

**Heavy runs**: `pnpm build:all`, the whole integration suite
(`dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj`), the E2E suite and
`node tests/scripts/run.mjs` are CPU-heavy. Each row that runs one says "(heavy run: take the shared CPU lock)".

**No bundle work**: no task changes script loading, the bundle split, `budgets.json`, `baseline.ci.json` or any CI
budget (plan D1, D2). Those are 009's.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: US1, US2 or US3 from spec.md
- **[FR-nnn] / [SC-nnn]**: The requirements the row serves
- **[PERSON]**: The owner does it. The implementing session never does it for them
- **[WAITS:Tnnn]**: The row cannot start until the named rows are done

## Path Conventions

The site is `src/KCC.Web` (C# features, Vue components, uSync files). Tests: `tests/KCC.UnitTests`,
`tests/KCC.IntegrationTests` and `tests/KCC.E2ETests` on TUnit, `tests/KCC.ViteTests` on Vitest. The evaluation console
is `tools/KCC.ReaderEval`; its set, `tools/KCC.ReaderEval/set/`, is gitignored and lives only in the main checkout at
`/Users/twinright/Repos/Personal/Kitchen-Command-Center/tools/KCC.ReaderEval/set/`. In a worktree, link
`tools/KCC.ReaderEval/set` to that folder so the tool and the owner see one set. `RW/` below stands for
`src/KCC.Web/Features/Components/RecipeWizard/`, and `RWT/` for `tests/KCC.ViteTests/Features/Components/RecipeWizard/`.

---

## Phase 1: Setup, schema and strings

**Purpose**: Gates, the two proposal properties and the 33 `RecipeWizard` strings every later phase uses

- [X] T001 Confirm constitution gates 1 and 2 on the implementing machine: `FONTAWESOME_NPM_AUTH_TOKEN`, `KCC_E2E_MEMBER_USERNAME` and `KCC_E2E_MEMBER_PASSWORD` are set; the .NET SDK in `global.json`, the Node line the `Dockerfile` names, pnpm with the Font Awesome `~/.npmrc` line and the Playwright browsers are installed
- [X] T002 [P] [US3] Write failing tests in `tests/KCC.IntegrationTests/Features/Baseline/SchemaTests.cs`: content type `recipe` has property `proposedCategory` and `recipeVariant` has `proposedTags`, each on the `Textstring` data type `0cc0eba1-9960-42c9-bf9b-60e150b429ae`, in the type's first group after the existing properties [FR-009]
- [X] T003 [US3] Make T002 pass: hand-write the two properties into `src/KCC.Web/uSync/v17/ContentTypes/recipe.config` and `recipevariant.config`, following the files' own format, each with a backoffice description saying it comes from the member's submission and never shows on the site (data-model.md) [FR-009]
- [X] T004 [US3] Generate models from a signed-in Development backoffice (Settings → Models Builder → Generate models, README "Schema and baseline content") and keep the regenerated `src/KCC.Web/Features/Models/Generated/Recipe.generated.cs` and `RecipeVariant.generated.cs`, never hand-edited. If the session cannot sign in, stop and report: this row then becomes the owner's click, and later phases can proceed because they write the properties by alias [FR-009]
- [X] T005 [P] Write failing tests for the 33 strings in contracts/details-ui.md ("Strings"): extend `RecipeWizardGroup_ImportsEveryHandWrittenItem_WithItsValue` in `tests/KCC.IntegrationTests/Features/Baseline/DictionaryTests.cs` to the new keys and English values, and `RWT/recipeWizardStrings.test.ts` to the new `recipeWizardKeys.ts` entries [FR-025]
- [X] T006 Make T005 pass: hand-write 33 `src/KCC.Web/uSync/v17/Dictionary/recipewizard.<key>.config` files in the format of the existing 112, with the exact English from the contract, and add the keys to `RW/recipeWizardKeys.ts` [FR-025]
- [X] T007 Run the whole integration suite (heavy run: take the shared CPU lock) and confirm the schema and dictionary boot tests pass

**Checkpoint**: A fresh site boots with the two properties and the 33 strings

---

## Phase 2: The lists (foundational)

**Purpose**: Published categories and tags reach both wizard pages (plan D9)

- [X] T008 [P] Write failing tests in `tests/KCC.IntegrationTests/Features/Recipes/RecipeQueriesTests.cs` for `IRecipeQueries.GetTaxonomyChoices()`: published `recipeCategory` items in content-tree order as `{ key, name }`; published `recipeTag` items as `{ key, name, kind }` with `kind` `Diet` or `Style`, a tag with no `Style` kind counting as `Diet` as `GetTaxonomy` treats it; an unpublished category or tag is absent; a category added by the test appears with no code change [FR-002, FR-003, FR-007]
- [X] T009 [P] Write failing tests in `tests/KCC.IntegrationTests/Features/Pages/RecipeImportPagePropsTests.cs`: Create Recipe renders a `details-choices` prop holding the categories and tags; Add Variant renders it with `categories` empty (contracts/details-ui.md "Page props") [FR-004, FR-007]
- [X] T010 Make T008 pass: add `DetailsChoices`, `CategoryChoice` and `TagChoice` records to `src/KCC.Web/Features/Recipes/RecipeRecord.cs` and `GetTaxonomyChoices()` to `src/KCC.Web/Features/Recipes/RecipeQueries.cs` and its interface, reading the published cache [FR-007]
- [X] T011 Make T009 pass: `CreateRecipePageController.cs`, `CreateRecipeViewModel.cs`, `Index.cshtml` and `CreateRecipeView.Component.vue` in `src/KCC.Web/Features/Pages/CreateRecipe/`, and the same four in `src/KCC.Web/Features/Pages/AddVariant/` (with `categories` empty), pass `details-choices` through `Vue.Prop` to `RecipeWizard` as `choices`; add the `DetailsChoices` TypeScript type to a new `RW/wizardDetails.ts` and an optional `choices` prop to `RW/RecipeWizard.vue` that nothing renders yet [FR-004, FR-007]

**Checkpoint**: Both pages carry the lists; no visible change

---

## Phase 3: User Story 1 and 3 - The submission stores the Details (server)

**Goal**: The API accepts, checks and stores category, tags, difficulty, nutrition and proposals (contracts/submission.md)

**Independent Test**: An API submission with Dinner, Spicy, Gluten-Free, Medium and 450 calories makes a draft that holds exactly those values

- [X] T012 [P] [US1] Write failing tests in `tests/KCC.UnitTests/Features/Submissions/DetailsRulesTests.cs` over data-model.md's "Limits" table: `calories` "Whole number, 0 to 10,000"; `proteinG`, `carbsG`, `fatG`, `saturatedFatG`, `fiberG`, `sugarG` "Whole number, 0 to 1,000"; `sodiumMg` "Whole number, 0 to 50,000"; `proposedCategory`, `proposedTags` "Up to 100 characters after trimming"; `difficulty` "`Easy`, `Medium`, `Hard` or none" by exact match; each refusal names its field [FR-006, FR-008]
- [X] T013 [P] [US1] Write failing tests in `tests/KCC.UnitTests/Features/Submissions/SubmissionValuesTests.cs`: `category` and `tags` as the tree picker's `JsonArray` of `{ type: "document", unique }`, tags in the lists' order with duplicates ignored; `difficulty` as `["Medium"]`; each nutrition value written when given, `0` included, and left out when null (research R5, plan D16, D17); `proposedCategory` and `proposedTags` trimmed and left out when blank; a body with no Details writes exactly Phase 1's values [FR-022, SC-002]
- [X] T014 [US1] Write failing tests in `tests/KCC.IntegrationTests/Features/Api/SubmissionTests.cs`: a Create Recipe submission with Dinner, Spicy, Gluten-Free, Medium, 450 calories and `fatG` 0 makes a draft recipe holding the category and a draft variant holding the tags, `difficulty` (read back as `Medium` through `RecipeVariant.Difficulty` after publishing, which pins D17), `calories` 450, `fatG` 0 and no other nutrition value; an Add Variant submission stores tags, difficulty and nutrition; an empty Details body submits as Phase 1 did; a value out of its limit answers 400 `invalidDetails` with its `field`; a category or tag key that is not published answers 400 `detailsChanged` with the current `choices`, and nothing is written; the anti-forgery check and the `submissions` rate limit still answer as in Phase 1 [FR-005, FR-022, FR-023, FR-024, SC-002]
- [X] T015 [US3] In the same file, write failing tests: proposals "Mexican" (tag) and "Sides" (category) are stored on the draft variant and recipe; after publishing, the recipe page, the variant page and `/api/recipes/search` never contain either text; the count of `recipeCategory` and `recipeTag` nodes is unchanged [FR-008, FR-009, FR-010, SC-003]
- [X] T016 [US1] Make T012 pass: `src/KCC.Web/Features/Submissions/DetailsRules.cs` holding the limits table and the checks [FR-006, FR-008]
- [X] T017 [US1] Add the optional fields of contracts/submission.md to `src/KCC.Web/Features/Models/Api/CreateRecipeRequest.cs` (`category`, `proposedCategory`) and `CreateVariantRequest.cs` (`tags` default `[]`, `difficulty`, `nutrition` of nullable integers, `proposedTags`) [FR-022]
- [X] T018 [US1] Make T013 pass: write the Details in `src/KCC.Web/Features/Submissions/SubmissionValues.cs` and pass them through `src/KCC.Web/Features/Submissions/RecipeSubmissions.cs`, inside its existing one scope after its existing `scope.WriteLock(Constants.Locks.ContentTree)`, with no new read before the lock. Add Variant's single-node save through `IContentEditingService` is unchanged and opens no scope; neither path gains a read before its write [FR-022, FR-009]
- [X] T019 [US1] Make T014 and T015 pass: in `src/KCC.Web/Features/Api/RecipeApiController.cs`, check names (Phase 1), then `DetailsRules` (400 `invalidDetails`), then the member, then the keys against `GetTaxonomyChoices()` from the published cache before `RecipeSubmissions` opens its scope (400 `detailsChanged` with `choices`); keep the anti-forgery attribute and the `submissions` rate limit [FR-022, FR-023, FR-024, FR-010]
- [X] T020 [US1] Run the whole integration suite, as constitution III requires after a change to `RecipeSubmissions` (heavy run: take the shared CPU lock)

**Checkpoint**: The server stores the Details and keeps proposals private

---

## Phase 4: User Story 1 - The wizard's Details logic (client, no components)

**Goal**: Values, limits, flow steps, edits and the refusal path in plain TypeScript

- [X] T021 [P] [US1] Write failing tests in `RWT/wizardDetails.test.ts`: the eight `NutritionField` aliases with their limits and units (calories 10,000 kcal; the six gram values 1,000 g; `sodiumMg` 50,000 mg); validation of the typed text: `''` valid, `'0'` and `'450'` valid, `'-1'`, `'1.5'`, `'abc'` and one over the limit invalid with the limit for `NutritionInvalid`; request mapping from texts to integers or null; tags kept in the lists' order; dropping a category or tag key the new lists lack [FR-003, FR-006]
- [X] T022 [P] [US1] Write failing tests in `RWT/wizardValues.test.ts`: empty values (`category` null, `tags` [], `difficulty` null, each nutrition `''`, proposals `''`); the Create Recipe body carries `category`, `proposedCategory` and the variant's Details; the Add Variant body carries no category; mark paths `recipe.category`, `variant.difficulty`, `variant.tag.<key>`, `variant.nutrition.<field>` [FR-004, FR-022]
- [X] T023 [P] [US1] Write failing tests in `RWT/recipeWizardFlow.test.ts`: `TYPING_STEPS.recipe` is basics, variant, ingredients, instructions, details, review and `TYPING_STEPS.variant` is variant, ingredients, instructions, details, review; `details` is a `ReviewSection` whose Edit opens the step with the return to the review; `PROCEED_RULES.details` is true when every nutrition text is empty or valid; editing a Details value sets it, clears its mark (also when re-picking the suggested value, spec A11) and sets `editedSinceRead`; editing a proposal clears no mark and leaves `editedSinceRead` alone (D18); `details` is in `PHOTO_STRIP_STEPS`, so the photo strip shows on the Details step (plan D29); `refuseDetails(choices)` replaces the lists, drops a missing category or tag, keeps every other value and opens Details with its return to the review [FR-001, FR-005, FR-006, FR-012, FR-021, FR-023]
- [X] T024 [P] [US1] Write failing tests in `RWT/wizardErrors.test.ts`: a 400 with code `detailsChanged` yields the refusal carrying `choices`; `invalidDetails` yields the generic `SubmitFailed` [FR-023]
- [X] T025 [US1] Make T021 pass in `RW/wizardDetails.ts`, rendering-free and small (plan D3, D12)
- [X] T026 [US1] Make T022 pass in `RW/wizardValues.ts`
- [X] T027 [US1] Make T023 pass in `RW/recipeWizardFlow.ts`, adding `details` to `PHOTO_STRIP_STEPS`
- [X] T028 [US1] Make T024 pass in `RW/wizardErrors.ts` [FR-023]

**Checkpoint**: The web Vitest suite passes; the logic is ready for the step

---

## Phase 5: User Story 1 and 3 - The Details step and the review section (client UI)

**Goal**: `DetailsStep.vue`, `DetailsSummary.vue` and their wiring, on kit classes and existing components only (plan D13, D14)

- [X] T029 [P] [US1] Write failing SSR tests in `RWT/DetailsStep.test.ts` per contracts/details-ui.md: a `KccSheet` with `wash-key="wizard-details"` and `data-testid="details-step"`; in mode `recipe` a `SelectField` (`details-category`) with a None option; two `kcc-check` fieldsets, diet then style, with `details-tag-<key>`; a `SegmentedControl` (`Components/Recipe/SegmentedControl.vue`) radio group (`details-difficulty`) of None, Easy, Medium, Hard, with None as `''` mapped to null (plan D32); eight `InputField`s (`details-nutrition-<field>`) with `inputmode="numeric"`, each unit and an inline `NutritionInvalid` error tied by `aria-describedby`; Next disabled while any value is invalid; proposal inputs (`details-proposed-category` in mode `recipe` only, `details-proposed-tags`) with `maxlength="100"` and `ProposalHint`; in mode `variant` no category and no category proposal; every label from the `RecipeWizard` strings [FR-002, FR-003, FR-004, FR-006, FR-008, FR-025]
- [X] T030 [P] [US1] Write failing tests in `RWT/DetailsSummary.test.ts` and `RWT/ReviewStep.test.ts`: `data-testid="review-details"` with the kicker `ReviewDetails` lists the category (mode `recipe`), tags, difficulty, each given nutrition value with its unit and both proposals; `NoDetailsGiven` when nothing was given; no Phase 1 missing-value warning; `review-edit-details` emits `edit('details')` [FR-011, FR-012, FR-005]
- [X] T031 [P] [US1] Write failing tests in `RWT/RecipeWizard.test.ts`: the `choices` prop reaches the step; the details branch renders after instructions; a `detailsChanged` submit answer opens Details with the `details-changed` well, the new lists and the other values kept [FR-001, FR-023]
- [X] T032 [US1] Make T029 pass: build `RW/DetailsStep.vue` with the `kit-builder` agent on Phase 1's step pattern (`STEP_PAD`, the step's `tear` and `flip`, a screen-reader `h2`, one `<form>` emitting `next`, `StepNav`), rendering fields, tag groups and levels from arrays, difficulty through `Components/Recipe/SegmentedControl.vue` (plan D32); no new class, no CSS, no kit change [FR-002, FR-003, FR-004, FR-006, FR-008]
- [X] T033 [US1] Make T030 pass: `RW/DetailsSummary.vue`, imported statically by `RW/ReviewStep.vue` only (plan D1) [FR-011, FR-012]
- [X] T034 [US1] Make T031 pass: import `DetailsStep` statically in `RW/RecipeWizard.vue` only, add its `v-else-if` branch, the `choices` prop and `refuseDetails` on a `detailsChanged` answer; no `defineAsyncComponent`, no dynamic import (plan D1) [FR-001, FR-023]
- [X] T035 [US1] Run D3 in `src/KCC.Web`: `pnpm type-check`, `pnpm lint`, `pnpm format`, keeping what lint and format change

**Checkpoint**: A typed recipe passes through Details to the review in the Vitest suite

---

## Phase 6: User Story 1 and 3 - End to end, and the size check

**Goal**: The typed flows prove the stories in the browser; the shared script stays inside the guard

- [X] T036 [US1] Update the typed-submission tests in `tests/KCC.E2ETests/Features/Members/MemberFlowTests.cs` and `RecipeWizardReviewTests.cs` to pass the Details step with one Next, keeping every Phase 1 hook [FR-001, FR-005]
- [X] T037 [US1] Write E2E tests in `tests/KCC.E2ETests/Features/Members/RecipeDetailsTests.cs`, locating by `data-testid` and role: Story 1's independent test (Dinner, Spicy, Gluten-Free, Medium, 450 calories; the review lists them; the submission succeeds); an empty step reaches the review with `NoDetailsGiven`; Add Variant shows no category; Edit on the review's Details section opens the step and returns; `1.5` calories shows the error and keeps the member on the step; a `detailsChanged` answer, routed by Playwright to the submit request, returns to Details with the `details-changed` well and the other values kept. These are acceptance tests over paths T029 to T031 already drove [FR-001, FR-002, FR-003, FR-004, FR-005, FR-006, FR-011, FR-012, FR-023, SC-002]
- [X] T038 [US3] In the same file, an E2E test: a typed recipe with "Mexican" as proposed tags and "Sides" as proposed category shows both on the review and submits [FR-008, FR-011]
- [ ] T039 [US1] Run `pnpm build:all` and the E2E suite (heavy run: take the shared CPU lock)
- [ ] T040 Measure the branch's gzipped `main` chunk as quickstart.md ("Script size for the pull request") sets out and compare it with the guard's 117,266-byte baseline plus 2,345 bytes; if it is over, stop and report the delta (plan D2), changing no budget, baseline or split

**Checkpoint**: Story 1 and Story 3 are done; the typed MVP can ship

---

## Phase 7: User Story 2 - What the read sends (server)

**Goal**: The read carries the lists as short ids and reserves for them (research R2, contracts/read-response.md)

- [X] T041 [P] [US2] Write failing tests in `tests/KCC.UnitTests/Features/RecipeImport/ReaderTaxonomyTests.cs`: ids `C1…` for categories and `T1…` for tags with `group` `diet` or `style`; the prompt lines exactly as the contract shows; an id maps back to its key and a tag's kind; an unknown id maps to nothing; the variant flow leaves categories out; `Characters` counts the text [FR-013, FR-020]
- [X] T042 [P] [US2] Write failing tests in `tests/KCC.UnitTests/Features/RecipeImport/ReadCostTests.cs`: `ReservationMicroUsd` counts the taxonomy's characters with the catalog's, at the catalog's characters-per-token rate, and the ceiling's defaults do not change [SC-007]
- [X] T043 [P] [US2] Write failing tests in `tests/KCC.UnitTests/Features/RecipeImport/AnthropicRecipePhotoReaderTests.cs`: the instruction carries the category and tag lines after the catalog and prompt rules 10 to 13; on the variant flow it leaves categories out and says to set `details.category` to null; the output schema requires `details` with the shape in contracts/read-response.md, including `printed` on the category and each tag, and has no proposal field; rule 10 lets an equivalent word fill a value and asks for the word in `printed` ("plant-based" for Vegan); no member account data is in the request [FR-013, FR-014, FR-018, FR-019, FR-020]
- [X] T044 [P] [US2] Write failing tests in `tests/KCC.IntegrationTests/Features/RecipeImport/RecipeImportApiTests.cs`: the read request that `FakeRecipePhotoReader` records carries the site's published categories and tags (no categories on the variant flow), and the spend reservation grows by their characters [FR-020, SC-007]
- [X] T045 [US2] Make T041 pass: `src/KCC.Web/Features/RecipeImport/ReaderTaxonomy.cs` built from `DetailsChoices` (plan D4)
- [X] T046 [US2] Make T042 pass in `src/KCC.Web/Features/RecipeImport/ReadCost.cs` [SC-007]
- [X] T047 [US2] Make T043 pass: `ReaderPrompt.cs` (the list lines and rules 10 to 13), `ReaderOutputSchema.json` (the `details` object, added to the root's `required`), the read request's taxonomy beside the catalog in `ReadResponse.cs`, where `ReadRequest` lives, and `AnthropicRecipePhotoReader.cs` when the request shape needs it, all in `src/KCC.Web/Features/RecipeImport/`. The new `ReadRequest` parameter is optional (null means no lists), so `tools/KCC.ReaderEval` (`AnthropicEvalService.cs`) still compiles and no existing call site changes (plan D31). Leave `specs/001-photo-recipe-import/contracts/reader-output.schema.json` unchanged [FR-013, FR-019, FR-020]
- [X] T048 [US2] Make T044 pass: `src/KCC.Web/Features/RecipeImport/RecipeImportApiController.cs` builds `ReaderTaxonomy` from `GetTaxonomyChoices()` per read and passes its characters to the reservation; extend `tests/KCC.IntegrationTests/Config/FakeRecipePhotoReader.cs` to record the taxonomy and to script `details` output [FR-020]

**Checkpoint**: A read sends the lists; the reservation counts them

---

## Phase 8: User Story 2 - What the read returns (server rules)

**Goal**: Conversion, list and diet rules and the new marks on the server (research R3, R4; plan D5 to D8, D20)

- [X] T049 [P] [US2] Write failing tests in `tests/KCC.UnitTests/Features/RecipeImport/NutritionConversionTests.cs`: a whole per-serving value copies unmarked; `1.5 g` fiber rounds half up to 2, `calculated` with code `printedAs` and arg the printed text; 1200 kJ becomes 287 kcal at 4.184 kJ per kcal; sodium in g becomes mg; salt divides by 2.5 when no sodium is printed, and printed sodium wins over salt; a whole-recipe panel divides by printed servings; a whole-recipe panel with servings absent or marked `estimate` leaves every value empty; a panel whose basis is null leaves every value empty and unmarked (plan D27); a range or `<n` keeps the floor of its lower number and is `unclear` with the model's reason; a result over its limit is empty and `unclear` with code `outOfRange` (spec A2, A3) [FR-016, FR-017, FR-018, SC-005, SC-006]
- [X] T050 [P] [US2] Write failing tests in `tests/KCC.UnitTests/Features/RecipeImport/ReadResultNormalizerTests.cs` for research R4's table: an unknown id is dropped; a category on the variant flow is dropped; a diet tag with `stated: false` is dropped (tofu, rice and vegetables never saying vegan fills no diet tag); a stated diet tag keeps the model's `unclear` mark (vegetarian with chicken stock); a category, style tag or difficulty with `stated: false` is `suggested` with the model's reason; a printed difficulty equal to Easy, Medium or Hard ignoring case carries no mark, any other printed word is `suggested` with `printedAs` and the word; a diet tag named by an equivalent word fills and is `suggested` with `printedAs` and the word ("plant-based" fills Vegan, marked suggested with `printedAs` "plant-based"), and the same for a category or style tag whose `printed` differs from the list name, ignoring case, while one whose `printed` equals it carries no mark (plan D26); difficulty and the Easy tag are independent; mark precedence `unclear`, `estimate`, `suggested`, `calculated`, `written`, `translated`; all eight nutrition keys present; a `noRecipe` outcome carries every Details value null and no tags [FR-004, FR-013, FR-014, FR-015, FR-018, FR-019, SC-006]
- [X] T051 [P] [US2] Write failing tests in `tests/KCC.IntegrationTests/Features/RecipeImport/RecipeImportApiTests.cs`: a scripted read returns `details` in the response shape of contracts/read-response.md, with keys mapped from ids [FR-013, FR-016]
- [X] T052 [US2] Make T049 pass: `src/KCC.Web/Features/RecipeImport/NutritionConversion.cs` with `MidpointRounding.AwayFromZero` on non-negative values [FR-016, FR-017, FR-018]
- [X] T053 [US2] Add `DetailsValues`, mark kinds `suggested` and `calculated`, and code `printedAs` to `src/KCC.Web/Features/RecipeImport/ReadResponse.cs` (data-model.md "Read response", "Mark") [FR-021]
- [X] T054 [US2] Make T050 and T051 pass in `src/KCC.Web/Features/RecipeImport/ReadResultNormalizer.cs`. The new `Normalize` parameters are optional (taxonomy null means no lists, flow defaults to recipe), so `tools/KCC.ReaderEval` (`EvalRunner.cs`) and the existing call sites still compile until Phase 11 wires the taxonomy (plan D31) [FR-013, FR-014, FR-015, FR-018, FR-019]
- [X] T055 [US2] Run the unit suite and the integration suite (heavy run: take the shared CPU lock)

**Checkpoint**: A read returns Details that hold every hard rule

---

## Phase 9: User Story 2 - The suggestions on the step and the review (client)

**Goal**: The wizard fills, marks, sets aside and keeps the right values after a read

- [X] T056 [P] [US2] Write failing tests in `RWT/ValueMark.test.ts`: kinds `suggested` (`MarkSuggested`) and `calculated` (`MarkCalculated`) with `data-kind`; code `printedAs` renders `MarkReasonPrintedAs` with its arg [FR-015, FR-016, FR-017, FR-021, FR-025]
- [X] T057 [P] [US2] Write failing tests in `RWT/recipeImportApi.test.ts` for `ReadResponse.details`, and in `RWT/wizardValues.test.ts` for `valuesFromRead`: category, tags, difficulty and nutrition texts with their marks at their mark paths [FR-013, FR-021]
- [X] T058 [P] [US2] Write failing tests in `RWT/recipeWizardFlow.test.ts`: `applyRead` replaces the Details values and marks and keeps both proposals; accepting a proposed recipe sets the category and its mark aside and keeping a new recipe restores them (plan D19); accepting a match also sets `proposedCategory` aside and keeping a new recipe restores it, and a variant submission never sends it (plan D28); Phase 1's warning before a read replaces edited values counts an edited Details value [FR-004, FR-019, FR-021]
- [X] T059 [P] [US2] Write failing tests in `RWT/DetailsStep.test.ts` and `RWT/DetailsSummary.test.ts`: each marked value shows `ValueMark` after its control, named in `aria-describedby`, a tag's mark beside its row; the summary shows every mark; after an accepted match no category shows [FR-004, FR-015, FR-021]
- [X] T060 [US2] Make T056 pass in `RW/ValueMark.vue` [FR-021]
- [X] T061 [US2] Make T057 and T058 pass in `RW/recipeImportApi.ts`, `RW/wizardValues.ts` and `RW/recipeWizardFlow.ts` [FR-004, FR-019, FR-021]
- [X] T062 [US2] Make T059 pass in `RW/DetailsStep.vue` and `RW/DetailsSummary.vue`, then run D3 in `src/KCC.Web` [FR-015, FR-021]

**Checkpoint**: The Vitest suite shows suggestions, marks and their clearing

---

## Phase 10: User Story 2 - The stand-in reader and the import end to end

**Goal**: Every review state of the import reachable with no paid read (research R8, plan D21)

- [X] T063 [P] [US2] Write failing tests in `tests/KCC.UnitTests/Features/RecipeImport/StandInRecipePhotoReaderTests.cs` for research R8's table: 1 photo (Breakfast not stated with a reason, Vegan stated, Easy style tag not stated, difficulty printed "easy", a per-serving panel with whole numbers and fiber 1.5 g); 2 and 3 photos as Phase 1 with the Details of 1; 4 photos (whole-recipe panel in kJ with salt, 8 printed servings); 5 photos (servings marked `estimate`); names mapped through `ReaderTaxonomy`, a missing name dropped [FR-013, FR-016, FR-017, FR-018]
- [X] T064 [US2] Make T063 pass in `src/KCC.Web/Features/DevTools/RecipeImport/StandInRecipePhotoReader.cs`
- [X] T065 [US2] Write E2E tests in `tests/KCC.E2ETests/Features/Members/RecipeImportDetailsTests.cs` for quickstart.md scenario 4: 1 photo shows Breakfast and the Easy tag `suggested`, Vegan and Easy difficulty unmarked, whole values unmarked, fiber 2 `calculated` "Printed as 1.5 g"; 4 photos show every value divided by 8 and `calculated`; 5 photos show every value empty and unmarked; 2 photos with the match accepted show no category, and Change match then keep a new recipe brings it back; editing a suggested value clears its mark on the step and the review; both proposal fields are empty after a read [FR-004, FR-015, FR-016, FR-017, FR-018, FR-019, FR-021]
- [X] T066 [US2] Run `pnpm build:all` and the E2E suite (heavy run: take the shared CPU lock)

**Checkpoint**: Story 2 works end to end on the stand-in reader

---

## Phase 11: The evaluation tool

**Goal**: `tools/KCC.ReaderEval` scores SC-004 to SC-006 and refuses a run that cannot measure them (research R7, plan D22 to D25)

- [X] T067 [P] Write failing tests in `tests/KCC.UnitTests/Tools/ReaderEvalScorerTests.cs`: SC-004 counts the read's category name equal to the expected one over cases that expect a category; SC-005 counts each expected non-null nutrition value equal to the read's, over all of them; SC-006 counts nutrition filled where the expected value is null plus diet tags filled that the expected answer does not list as stated; `Report` gains the three columns [SC-004, SC-005, SC-006]
- [X] T068 [P] Write failing tests in `tests/KCC.UnitTests/Tools/ReaderEvalSetupTests.cs`: `expected.json` loads the `details` block of quickstart.md ("The paid reader comparison") with `nutritionPrinted` `perServing`, `converted` or null and an optional `draft`; `--draft` writes a `details` block carrying `"draft": true`; `--models` refuses before any spend unless every case has `details`, no block has `"draft": true`, at least 3 cases expect a nutrition value with at least one `converted`, and at least 10 expect a category; the tool sends `TaxonomyDefaults.Values` through `ReaderTaxonomy` [SC-004, SC-005, SC-006]
- [X] T069 Make T068 pass: `ExpectedAnswer.cs` (`ExpectedDetails`, and `FromRead` writes a `details` block carrying `"draft": true` so `--draft` output awaits the owner's confirmation), `EvalSet.cs` (the gate) and the taxonomy in `EvalRunner.cs` or `AnthropicEvalService.cs` in `tools/KCC.ReaderEval/`, and the `--models` text in `EvalOptions.cs`. The taxonomy maps each `TaxonomyDefault` of `TaxonomyDefaults.Values` to a `ReaderTaxonomy` entry: a tag is the entry whose `Alias` is `kind`, its group parsed from `Stored` (`["Diet"]` gives Diet), and a category is the entry whose `Alias` is `icon`
- [X] T070 Make T067 pass in `tools/KCC.ReaderEval/CaseScorer.cs` and `Report.cs`
- [ ] T071 Run the free `dotnet run --project tools/KCC.ReaderEval -- --check` (only `count_tokens` calls, no spend) to prove the service takes the grown schema; record the time of its last call for the 24-hour wait before the paid run. If the service refuses the schema, stop and report [FR-020]

**Checkpoint**: The tool can measure the Phase 2 bars once the set is ready

---

## Phase 12: Draft the evaluation set's Details

**Goal**: The session's draft of the expected `details` for the 10 existing cases (plan D23, D25)

- [X] T072 Draft the `details` block of each of the 10 cases' `expected.json` in the main checkout's `tools/KCC.ReaderEval/set/` from its photos, in quickstart.md's shape: category from `TaxonomyDefaults.Values` or null, tags with `stated` true only when the photos name them, difficulty, `nutritionPrinted` and each nutrient as the site should store it after research R3's conversions, null where not printed. Mark each block `"draft": true` for the owner to confirm, and change nothing else in the file. List in research.md under "Measured results" which cases print nutrition and which expect a category, so the owner sees what the gate still needs [SC-004, SC-005, SC-006]

**Checkpoint**: Every existing case carries a drafted `details` block

---

## Phase 13: Polish and the definition of done

**Purpose**: Docs, brand, ramps, the phone timing, the size note and D1 to D7 and D10

- [X] T073 [P] Correct every line that this change makes untrue in `README.md`, `CLAUDE.md` and `docs/` (D10): the photo import's stand-in photo counts, the evaluation set's `details` block, and the wizard's steps
- [X] T074 Run the `brand-steward` agent over `RW/DetailsStep.vue`, `RW/DetailsSummary.vue` and the two wizard pages, and fix every violation (D7)
- [ ] T075 Check the Details step, its marks, the `details-changed` well and the review's Details section in the light and the dark ramp in the Browser pane against the running site (D6)
- [ ] T076 [US1] Time SC-001 in the Browser pane at the mobile preset: from the Details heading to the review after choosing a category, three tags and a difficulty, under 30 seconds; record the time in research.md under "Measured results" [SC-001]
- [ ] T077 Run quickstart.md's free scenarios 2 to 6 on the dev site with no key and then with the stand-in reader, including the backoffice check that a draft holds only the given values and the unpublish-Spicy refusal [SC-002, SC-003, FR-007, FR-023]
- [X] T078 Measure the gzipped `main` chunk on `origin/main` in a separate worktree and on this branch on the same machine, as quickstart.md ("Script size for the pull request") sets out, each after `pnpm build:all` (heavy run: take the shared CPU lock); record both numbers and the delta in research.md under "Measured results", with the note on 009 from plan.md ("Pull request"): both branches touch `RecipeWizard.vue`, this branch changes no script loading, split, budget or baseline, and 009 moves the step by changing its two static imports. Card implement's close-out copies this into the pull request description
- [X] T079 Run definition-of-done rows D1 to D5 and D10 with the commands in `.specify/memory/constitution.md`, D4 with a no-op `open` first on `PATH` (heavy run: take the shared CPU lock)

**Checkpoint**: Everything except the reader comparison is done

---

## Phase 14: The owner's part

**Purpose**: What only the owner can do before the paid comparison. The session asks for none of it in a subagent

- [ ] T080 [PERSON] Add at least 3 recipes that print nutrition to `tools/KCC.ReaderEval/set/`, at least one per whole recipe or in kilojoules, each with photos and an `expected.json` with its `details` block (spec A8) [SC-005, SC-006]
- [ ] T081 [PERSON] Confirm every case's `details` block, the 10 that T072 drafted included, correcting what is wrong and deleting each `"draft": true` [WAITS:T072,T080] [SC-004, SC-005, SC-006]
- [ ] T082 [PERSON] Check that the Anthropic organization spend limit leaves about US$2 for the month of the comparison [SC-007]
- [ ] T083 [PERSON] Approve the one paid comparison run, about US$1 to US$2, in chat to the implementing session, and again when the tool states its estimate and asks [SC-007]
- [X] T086 [PERSON] Click Settings → Models Builder → Generate models in a signed-in Development backoffice, and keep the regenerated `Recipe.generated.cs` and `RecipeVariant.generated.cs`. Needed only when T004 stopped for want of a backoffice sign-in; T079 cannot pass D5 until this is done when T004 stopped. Not needed: T004 generated the models with no backoffice sign-in [FR-009]

---

## Phase 15: The reader comparison

**Purpose**: The one paid run and the default reader (spec A9, quickstart.md "The paid reader comparison")

- [ ] T084 Run `dotnet run --project tools/KCC.ReaderEval -- --models claude-haiku-5-5,claude-haiku-4-5,claude-sonnet-5-5,claude-opus-5-5` at least 24 hours after T071's last call, with no call carrying the schema in those 24 hours. Never start it without T083's yes. If the gate refuses, stop and report what the set lacks [WAITS:T071,T080,T081,T082,T083] [SC-004, SC-005, SC-006, SC-007]
- [ ] T085 Record the table, Phase 1's columns plus SC-004, SC-005 and SC-006, under "Measured results" in research.md; keep `claude-opus-5-5` as the default unless it fails a Phase 2 bar, and otherwise set the cheapest reader that passes every Phase 2 bar and does no worse on Phase 1's measured bars, recording the shortfall (spec A9). A changed default changes its pinning tests first, then the default `Model` in `src/KCC.Web/Features/RecipeImport/RecipeImportOptions.cs`, the one place the default reader changes, then reruns D1 and D4 (heavy run: take the shared CPU lock) [WAITS:T084] [SC-004, SC-005, SC-006]

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1** (setup, schema, strings): no dependencies. Blocks every later phase.
- **Phase 2** (lists): after Phase 1. Blocks Phases 3, 5, 7 and 11.
- **Phase 3** (submission server): after Phase 2.
- **Phase 4** (client logic): after Phase 2 (`DetailsChoices` type). Can run beside Phase 3.
- **Phase 5** (step UI): after Phase 4.
- **Phase 6** (typed E2E, size): after Phases 3 and 5.
- **Phase 7** (read input): after Phase 2. Can run beside Phases 3 to 6.
- **Phase 8** (read rules): after Phase 7.
- **Phase 9** (client suggestions): after Phases 5 and 8.
- **Phase 10** (stand-in, import E2E): after Phase 9.
- **Phase 11** (evaluation tool): after Phase 8 (`ReaderTaxonomy`, `ReadResponse.Details`).
- **Phase 12** (draft set): after Phase 11 (the `details` shape the tool loads).
- **Phase 13** (polish, DoD): after Phases 6, 10 and 12.
- **Phase 14** (owner): T081 after T072 and T080.
- **Phase 15** (comparison): after Phase 14 and T071.

### Within Each Phase

- A test row comes before the row that makes it pass, and the test must fail first (constitution IV). T037, T038 and T065 are acceptance tests over paths the Vitest and integration rows already drove.
- Server: schema, lists, rules, values, controller. Client: logic, components, wiring.
- `RecipeWizard.vue`, `recipeWizardFlow.ts`, `wizardValues.ts` and their test files are edited in Phases 4, 5 and 9: never in parallel.

### Parallel Opportunities

- Phase 1: T002 and T005. Phase 2: T008 and T009. Phase 3: T012 and T013.
- Phase 4: T021 to T024. Phase 5: T029 to T031. Phase 7: T041 to T044.
- Phase 8: T049 to T051. Phase 9: T056 to T059. Phase 11: T067 and T068.
- Phases 3 and 4 side by side; Phase 7 beside Phases 3 to 6.

## Parallel Example: User Story 2 (Phase 8)

```bash
Task: "NutritionConversionTests in tests/KCC.UnitTests/Features/RecipeImport/NutritionConversionTests.cs"
Task: "ReadResultNormalizerTests Details in tests/KCC.UnitTests/Features/RecipeImport/ReadResultNormalizerTests.cs"
Task: "RecipeImportApiTests details response in tests/KCC.IntegrationTests/Features/RecipeImport/RecipeImportApiTests.cs"
```

## Implementation Strategy

### MVP: Story 1 with Story 3 (Phases 1 to 6)

Typed recipes gain the Details step and proposals, with no paid read and on every site, CI included. Stop at the
Phase 6 checkpoint and validate Story 1's independent test.

### Then Story 2 (Phases 7 to 10), the tool (11, 12) and the polish (13)

Each checkpoint leaves all six suites green. Phase 13 completes everything the session can do alone.

### Last: the owner's part and the comparison (14, 15)

No paid run starts without T083's yes. The free `--check` (T071) runs in Phase 11.

## Notes

- Commit as `Th3FenrisWolf`, passing the identity on every commit as the constitution's "Review and merge" shows.
- The evaluation set is gitignored and never committed: neither the drafted nor the confirmed `details` blocks.
- No row changes `budgets.json`, `baseline.ci.json`, the bundle split or script loading.

## Traceability

| Id | Rows |
|---|---|
| FR-001 | T023, T031, T034, T036, T037 |
| FR-002 | T008, T029, T032, T037 |
| FR-003 | T008, T021, T029, T032, T037 |
| FR-004 | T009, T011, T022, T029, T050, T058, T059, T061, T065 |
| FR-005 | T014, T023, T030, T036, T037 |
| FR-006 | T012, T016, T021, T023, T029, T037 |
| FR-007 | T008, T009, T010, T011, T077 |
| FR-008 | T012, T015, T016, T029, T038 |
| FR-009 | T002, T003, T004, T015, T018, T086 |
| FR-010 | T015, T019 |
| FR-011 | T030, T033, T037, T038 |
| FR-012 | T023, T030, T033, T037 |
| FR-013 | T041, T043, T047, T050, T051, T054, T057, T063 |
| FR-014 | T043, T050, T054 |
| FR-015 | T050, T054, T056, T059, T062, T065 |
| FR-016 | T049, T051, T052, T056, T063, T065 |
| FR-017 | T049, T052, T056, T063, T065 |
| FR-018 | T043, T049, T050, T052, T054, T063, T065 |
| FR-019 | T043, T047, T050, T054, T058, T061, T065 |
| FR-020 | T041, T043, T044, T047, T048, T071 |
| FR-021 | T023, T053, T056, T057, T058, T059, T060, T061, T062, T065 |
| FR-022 | T013, T014, T017, T018, T019, T022 |
| FR-023 | T014, T019, T023, T024, T028, T031, T034, T037, T077 |
| FR-024 | T014, T019 |
| FR-025 | T005, T006, T029, T056 |
| SC-001 | T076 |
| SC-002 | T013, T014, T037, T077 |
| SC-003 | T015, T077 |
| SC-004 | T067, T068, T072, T080, T081, T084, T085 |
| SC-005 | T049, T067, T068, T072, T080, T081, T084, T085 |
| SC-006 | T049, T050, T067, T068, T072, T080, T081, T084, T085 |
| SC-007 | T042, T044, T046, T082, T083, T084 |
