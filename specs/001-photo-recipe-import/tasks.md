---

description: "Task list for Recipe Import from Photos, Phase 1"
---

# Tasks: Recipe Import from Photos, Phase 1

**Input**: Design documents from `/specs/001-photo-recipe-import/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md),
[contracts/](contracts/), [quickstart.md](quickstart.md)

**Tests**: Constitution IV requires test-first work. Every behaviour task follows a task that writes its failing test.
No test calls the paid reading service, and no test host sets an API key (research R15). The integration tests stay on
the one shared `UmbracoSite`, and boot no other site.

**Organization**: Tasks are grouped by user story. US1 and US2 are both P1 and ship together; US2 builds on US1's
import UI.

**Phases**: the phases below are task phases inside the feature's Phase 1. The feature's Phases 2 and 3 get their own
specs later.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: The user story the task belongs to (US1 to US4)
- **OWNER ACTION**: The implementer stops until the owner has done it

## Path Conventions

The web application lives in `src/KCC.Web` (C# features, Vue components, uSync files). Tests live in `tests/`:
`KCC.UnitTests`, `KCC.IntegrationTests` and `KCC.E2ETests` run on TUnit, and `KCC.ViteTests` runs on Vitest from
`src/KCC.Web`. The evaluation console is `tools/KCC.ReaderEval`.

---

## Phase 1: Setup

**Purpose**: Confirm the gates and prepare the repository

- [X] T001 Confirm constitution gates 1 and 2 on the implementing machine: `FONTAWESOME_NPM_AUTH_TOKEN`, `KCC_E2E_MEMBER_USERNAME` and `KCC_E2E_MEMBER_PASSWORD` are set; the .NET SDK named in `global.json`, Node 24 with Yarn 1, the Playwright browsers, a running Docker and `shellcheck` are installed (see "Gates" in `.specify/memory/constitution.md`)
- [X] T002 [P] Check that `.specify/extensions/card/card-config.yml` holds a `review.command`, so that card implement runs the CodeRabbit loop that "Review and merge" requires
- [X] T003 [P] Add `/tools/KCC.ReaderEval/set/` to `.gitignore`, because the repository is public and the evaluation set holds copyrighted pages and private family cards (research R16)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The client fix, the spending ledger, the fake reader on the test host, and one wizard for both flows

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### The Anthropic client (research R4)

- [X] T004 [P] Write failing tests in `tests/KCC.UnitTests/Features/Providers/RecipeIconProviderTests.cs`: a cancelled caller token makes `PickAsync` throw `OperationCanceledException` instead of returning the fallback; a response slower than the provider's budget returns the fallback, with the budget passed in short through the constructor so the test runs fast; and the client-options factory sets `Timeout` to 90 seconds and `MaxRetries` to 1
- [X] T005 Make T004 pass: move the client options into a factory that `src/KCC.Web/Features/Providers/ProvidersComposer.cs` uses (`Timeout` 90 s, `MaxRetries` 1), and give `src/KCC.Web/Features/Providers/RecipeIconProvider.cs` a 15-second default budget, as a constructor argument, on a token linked to the caller's, whose catch lets the caller's cancellation through; and correct the comment on the key check in `RecipeIconProvider.cs`, which says that development runs without a key, because the owner's dev site holds one in its user-secrets (spec A2)

### Options, model profiles and cost (research R5)

- [X] T006 [P] Write failing tests in `tests/KCC.UnitTests/Features/RecipeImport/ReadCostTests.cs`: the cost in micro-dollars from a `Usage` for each profile in research R5 (input, output, cache-write and cache-read tokens); `claude-haiku-5-5` settling at its first price set at 100,000 prompt tokens and at its higher set at 100,001, with input, cache-creation and cache-read tokens counted together; a model id outside the table has no profile; and the reservation estimate follows `specs/001-photo-recipe-import/data-model.md` ("Spend reservation"), with `MaxTokens` 12,000, at the profile's highest price set, so a `claude-haiku-5-5` reservation prices at its higher set. The tests pin research R5's figures, and T079 updates any figure that the evaluation corrects
- [X] T007 [P] Create `src/KCC.Web/Features/RecipeImport/RecipeImportOptions.cs` (section `RecipeImport`, with the keys and defaults in `specs/001-photo-recipe-import/contracts/configuration.md`) and `src/KCC.Web/Features/RecipeImport/RecipeImportComposer.cs`, which binds it with `Configure<RecipeImportOptions>` and registers `TimeProvider.System` when nothing else has
- [X] T008 Make T006 pass with `src/KCC.Web/Features/RecipeImport/ModelProfiles.cs` (the four models in research R5, with `imageTokenCap`, a second price set above 100,000 prompt tokens for `claude-haiku-5-5`, and no profile for an id outside the table) and `src/KCC.Web/Features/RecipeImport/ReadCost.cs`

### Reader contract and the test host (research R13, R15)

- [X] T009 Create `src/KCC.Web/Features/RecipeImport/IRecipePhotoReader.cs` (`IsConfigured` and `ReadAsync`) and the request, response, mark and read-attempt records in `src/KCC.Web/Features/RecipeImport/ReadResponse.cs`, per `specs/001-photo-recipe-import/contracts/read-endpoint.md` and `data-model.md` ("Mark": `kind`, `reason`, `code`, `arg`; "Read attempt": a read with its usage, or a failure that says whether the service answered)
- [X] T010 Add `tests/KCC.IntegrationTests/Config/FakeRecipePhotoReader.cs`: an `IRecipePhotoReader` that delegates to the reader it wraps until a test scripts it (`IsConfigured`, then a read with usage, each failure in `specs/001-photo-recipe-import/data-model.md` ("Read attempt"), or a read held open), reports `IsConfigured` false while it wraps none, records its calls, and returns to delegating on reset. In `tests/KCC.IntegrationTests/Config/UmbracoSite.cs`, register it as a singleton in place of the composer's `IRecipePhotoReader` and around it (the composer registers one from T041 on), set `RecipeImport:MonthlyCeilingUsd` to a non-default `1.00`, pin `RecipeImport:StandInReader` to `false` and `RecipeImport:StandInDelaySeconds` to `0` whatever the environment holds, as `Anthropic:ApiKey` is pinned, and keep `UmbracoSite` the only host in the test process (research R15). Add `tests/KCC.IntegrationTests/Config/TestImportSpend.cs`, which sets and reads a month's key under `Constants.Locks.KeyValues`, and add `PostMultipartAsync` to `tests/KCC.IntegrationTests/Config/MemberClient.cs`, which sends a `MultipartFormDataContent` body with the `RequestVerificationToken` header

### Spending ledger and availability (research R10, R13)

- [X] T011 [P] Write failing integration tests in `tests/KCC.IntegrationTests/Features/RecipeImport/ImportSpendLedgerTests.cs` on the shared `UmbracoSite`, with a far-past month of their own and the ceiling that site sets: a reservation is granted while the month's total is below the ceiling, whatever its size, and adds itself to `KCC.RecipeImport.SpendMicroUsd.<yyyy-MM>`; once the total is at or above the ceiling a reservation is refused and changes nothing; settling adds the settled cost minus the reservation to the reservation's month, also when settled after the month ends, and settling zero gives the whole reservation back; a missing key reads as 0; and 20 parallel reservations finish without a stall, with a total equal to the granted reservations
- [X] T012 [P] Write failing tests in `tests/KCC.UnitTests/Features/RecipeImport/ImportAvailabilityTests.cs` with a fixed `TimeProvider`: enabled exactly when the registered reader reports `IsConfigured`; available while the month's total is under the ceiling, also after the ceiling is raised above it; `availableAgainOn` is the first day of the next UTC month, December to January included; and `maxImageEdge` is the lower of the option and the model's edge, or the option alone for a model with no profile. T079 updates the image edges that these tests pin when the evaluation corrects one
- [X] T013 Make T011 pass with `src/KCC.Web/Features/RecipeImport/ImportSpendLedger.cs` (`IImportSpendLedger` over `ICoreScopeProvider` and `IKeyValueService`), whose calls name their month and whose every write scope takes `scope.WriteLock(Constants.Locks.KeyValues)` before its first read (constitution III), with the reservation and the settlement each in a core scope of its own, so that nothing holds the lock while a read runs (research R10), registered in `src/KCC.Web/Features/RecipeImport/RecipeImportComposer.cs`
- [X] T014 Make T012 pass with `src/KCC.Web/Features/RecipeImport/ImportAvailability.cs`; T047 registers it, once a reader is registered for it to ask

### The client API

- [X] T015 [P] Write failing tests in `tests/KCC.ViteTests/Features/Utilities/Api.test.ts`: every failure result carries the HTTP `status`, 0 when the request got no answer, and the body's `code`, with the two `toEqual` assertions on today's failure shape moved to the new one; and a new `postForm` sends the anti-forgery header and no JSON content type, takes an `AbortSignal`, and gives an `aborted` result, distinct from a failure, for an aborted request
- [X] T016 Make T015 pass in `src/KCC.Web/Features/Utilities/Api.ts`: keep every existing export and its behaviour, apart from the `status` and `code` that every failure result gains

### One wizard for both flows (research R14, R17)

- [X] T017 [P] Write failing tests in `tests/KCC.ViteTests/Features/Components/RecipeWizard/recipeWizardFlow.test.ts` for the typing paths in `specs/001-photo-recipe-import/data-model.md` ("Wizard flow state"): the step order of each mode, with the start step only when the import is enabled; Next and Back; the `canProceed` rules; and progress on the editing steps only, with totals of 5 and 4
- [X] T018 [P] Write failing tests in `tests/KCC.ViteTests/Features/Components/RecipeWizard/wizardValues.test.ts`: the values map exactly to today's `POST /api/recipes` and `POST /api/recipes/{key}/variants` bodies (blank rows dropped, steps numbered from 1, a zero or empty number sent as null, and no row's `mark` in a body), with the cases of `tests/KCC.ViteTests/Features/Pages/AddVariant/reviewSummary.test.ts` carried over; and in `tests/KCC.ViteTests/Features/Components/RecipeWizard/wizardErrors.test.ts`, the "Submit" rows of `specs/001-photo-recipe-import/contracts/wizard-ui.md` ("Errors"), each mapped from the `status` of a failed `post` result
- [X] T019 [P] Write failing SSR tests in `tests/KCC.ViteTests/Features/Components/RecipeWizard/` with `renderSsr` from `tests/KCC.ViteTests/support/ssr.ts`: in `RecipeWizard.test.ts`, recipe mode renders the basics step and variant mode the variant step, with today's ids ending `-prep-time`, `-cook-time` and `-servings`, the Next button, the field hooks `wizard-recipe-name`, `wizard-recipe-description`, `wizard-variant-name` and `wizard-variant-description`, progress totals of 05 and 04, and strings resolved under the `RecipeWizard` prefix; in `IngredientsStep.test.ts`, the `wizard-ingredient-name` hook and a quantity input with `step="any"`; and in `SubmittedSheet.test.ts`, the `wizard-submitted` hook, the recipe message and the variant message, the default closing link under `wizard-closing-link`, labelled `BackToRecipes` and leading to the `recipesUrl` that the test passes, a URL other than `/recipes`, and a passed `closingLink` replacing its label and href
- [X] T020 [P] Write failing tests in `tests/KCC.ViteTests/Features/Components/Forms/NumberStepper.test.ts` (a `valueLabel` replaces "Value" and "{label} in {unit}") and `tests/KCC.ViteTests/Features/Components/Wizard/WizardProgress.test.ts` (a `label` replaces "Steps")
- [X] T021 [P] Write a failing coverage test in `tests/KCC.ViteTests/Features/Components/RecipeWizard/recipeWizardStrings.test.ts` that imports `src/KCC.Web/Features/Components/RecipeWizard/recipeWizardKeys.ts`: every key in the map has a `src/KCC.Web/uSync/v17/Dictionary/recipewizard.<key>.config` file with a non-empty `en-US` translation; every literal key that a file under `src/KCC.Web/Features/Components/RecipeWizard/` uses through `rs(...)` or `<ResourceString for="...">` is in the map; and every key in the map is used there, literally or through the map
- [X] T022 [P] Write a failing boot test in `tests/KCC.IntegrationTests/Features/Baseline/DictionaryTests.cs`: a fresh site imports every `recipewizard.*.config` item with its value; none of the 34 retired `AddVariant.*` keys in `specs/001-photo-recipe-import/contracts/wizard-ui.md` ("Retired AddVariant files") exists; and `AddVariant.AddVariantFor`, `BackTo` and `Cancel` still do (constitution 1.1.0, principle IV)
- [X] T023 Make T017 and T018 pass with `recipeWizardFlow.ts`, `wizardValues.ts` and `wizardErrors.ts` in `src/KCC.Web/Features/Components/RecipeWizard/`; `reviewSummary.ts` stays until T026, because the Add Variant page still imports it
- [X] T024 Make T020 pass in `src/KCC.Web/Features/Components/Forms/NumberStepper.vue` and `src/KCC.Web/Features/Components/Wizard/WizardProgress.vue`, keeping their current output when the new props are absent
- [X] T025 Make T019 pass: extract `RecipeWizard.vue`, `BasicsStep.vue`, `VariantStep.vue`, `IngredientsStep.vue`, `InstructionsStep.vue`, `ReviewStep.vue` and `SubmittedSheet.vue` into `src/KCC.Web/Features/Components/RecipeWizard/` from today's two page components, keeping their markup, kit classes, ids and button names, apart from the quantity input's `step`, which becomes `any`; add the field hooks in `contracts/wizard-ui.md` ("Test hooks"); `RecipeWizard.vue` calls `provideResourceStrings(props.resourceStrings, 'RecipeWizard')`, passes `WizardProgress` its label, takes a `recipesUrl` prop for the success sheet's default href and an optional `closingLink` prop (`label` and `href`), and shows submission errors through `wizardErrors.ts` in a `submit-error` well; and the steps give `NumberStepper` every label from strings
- [X] T026 Rebuild `src/KCC.Web/Features/Pages/CreateRecipe/CreateRecipeView.Component.vue` and `src/KCC.Web/Features/Pages/AddVariant/AddVariantView.Component.vue` as a hero plus the wizard: Add Variant passes `closingLink` ("Back to <recipe>", from `AddVariant.BackTo` and the recipe's name, with the recipe's URL), and Create Recipe passes no `closingLink` but its new `recipesUrl` prop (`recipes-url` in `specs/001-photo-recipe-import/contracts/page-props.md`), which T028 fills from content; update `tests/KCC.ViteTests/Features/Pages/CreateRecipe/CreateRecipeView.test.ts` and `tests/KCC.ViteTests/Features/Pages/AddVariant/AddVariantView.test.ts` to the new markup; then delete `src/KCC.Web/Features/Pages/AddVariant/reviewSummary.ts` and `tests/KCC.ViteTests/Features/Pages/AddVariant/reviewSummary.test.ts`
- [X] T027 Make T021 and T022 pass for the foundational strings: write `src/KCC.Web/Features/Components/RecipeWizard/recipeWizardKeys.ts` with the 54 keys of "Steps, review and submission" in `specs/001-photo-recipe-import/contracts/wizard-ui.md`; write `src/KCC.Web/uSync/v17/Dictionary/recipewizard.config` and one `recipewizard.<key>.config` per key in the format of the existing `addvariant.*.config` files, with new GUIDs and the value its Source column names; and delete the 34 retired `addvariant.<key>.config` files, keeping `addvariant.config`, `AddVariantFor`, `BackTo` and `Cancel`
- [X] T028 Write failing assertions in `tests/KCC.IntegrationTests/Features/Api/SubmissionTests.cs` that both wizard pages render a `RecipeWizard.Next` string, and that Create Recipe renders `recipes-url` with the recipe listing's URL (`/recipes/` on the test site); then make them pass by merging `GetGroup("RecipeWizard")` into the page strings in `src/KCC.Web/Features/Pages/CreateRecipe/CreateRecipePageController.cs` and `src/KCC.Web/Features/Pages/AddVariant/AddVariantPageController.cs`, and by passing `page.Parent<RecipeListingPage>()?.Url()` from `CreateRecipePageController.cs` through `CreateRecipeViewModel.cs` and `Index.cshtml` in `src/KCC.Web/Features/Pages/CreateRecipe/` (constitution I)
- [X] T029 Move `tests/KCC.E2ETests/Features/Members/MemberFlowTests.cs` (`SubmittedRecipe_WaitsOnTheAccountPageAsPending`) and `SubmitRecipeAsync` in `tests/KCC.E2ETests/Features/Backoffice/BackofficeTests.cs` from placeholder text to the field hooks and `wizard-submitted`, with `wizard-closing-link` leading to the recipe listing's URL that the page passed (`/recipes/` on the seeded site); add the `wizard-submit` hook to the Submit for Review button in `src/KCC.Web/Features/Components/RecipeWizard/ReviewStep.vue`; and run both tests

**Checkpoint**: Typed recipes and variants work on the shared wizard, every string comes from the dictionary, and all
six suites pass

---

## Phase 3: User Story 1 - Turn photos into a new recipe draft (Priority: P1) 🎯 MVP

**Goal**: A member photographs a recipe, in Create Recipe or Add Variant, and lands on a filled, marked review that
submits the usual draft

**Independent Test**: With no published recipe like the test recipe, a member imports a photographed printed recipe and
submits it; the owner's draft holds the recipe as printed

### Tests for User Story 1 ⚠️

> Write these tests first and make sure they fail before the implementation

- [X] T030 [P] [US1] Write `tests/KCC.UnitTests/Features/RecipeImport/AnthropicRecipePhotoReaderTests.cs` with a fake `HttpMessageHandler` through `ClientOptions.HttpClient`, as `RecipeIconProviderTests` does: `IsConfigured` only with an API key and a model that has a profile, and an error logged that names a model without one; one `ImageBlockParam` with a `Base64ImageSource` per photo, then the text block; `OutputConfig.Format` holds the embedded schema; the configured model and `MaxTokens` 12,000; `ThinkingConfigAdaptive` and `Effort` for the profiles that take them, `claude-haiku-5-5` included, and neither for `claude-haiku-4-5`; a valid JSON text block parses; `refusal` and `max_tokens` stop reasons and unparsable text each give their own failure kind, with usage; an error status from the service gives a failure that the service answered, without usage; a connection failure gives a failure with no answer; each of those two sends one request only (`MaxRetries` 0); usage gives the cost; the read deadline gives a timed-out failure without usage, with the caller's token never cancelled; two reads through one reader, then an icon call through a `RecipeIconProvider` on the same client, all reach the handler (research R4); and no log message carries image data
- [X] T031 [P] [US1] Write `tests/KCC.UnitTests/Features/RecipeImport/ReadResultNormalizerTests.cs` for every rule in research R7: the limits, the number ranges and rounding, dropped blank rows, shortened or blanked values marked unclear with the code `shortened` or `outOfRange`, translated marks with the code `translated` and the language as `arg`, no server-written reason text, the mark precedence, `found: false` giving a `noRecipe` outcome with empty values, and a read with `found: true` that keeps no ingredient and no step after normalization giving a `noRecipe` outcome too
- [X] T032 [P] [US1] Write `tests/KCC.UnitTests/Features/RecipeImport/PhotoValidationTests.cs`: 1 to 10 files, at most 4 MB each and 22 MB together, JPEG, PNG, WebP or GIF by leading bytes, and anything else refused
- [X] T033 [P] [US1] Write `tests/KCC.UnitTests/Features/RecipeImport/StandInReaderGateTests.cs`: the stand-in reader is chosen only in Development or Testing with `RecipeImport:StandInReader` true, and never in Production; and `tests/KCC.UnitTests/Features/RecipeImport/StandInRecipePhotoReaderTests.cs`: the stand-in answers at once at the default `RecipeImport:StandInDelaySeconds` of 0, and with a delay set it answers only after that delay, on a `TimeProvider` the test controls
- [X] T034 [P] [US1] Write `tests/KCC.IntegrationTests/Features/RecipeImport/RecipeImportApiTests.cs` on the shared `UmbracoSite`, with an `[After(Test)]` hook that resets the fake reader and the current month's key, for `specs/001-photo-recipe-import/contracts/read-endpoint.md`. While the fake still delegates to the composer's own reader: 503 `unavailable`. With the fake switched on: 400 without the anti-forgery token; 401 when signed out, with nothing reserved; 400 `invalidPhotos` and `invalidRequest` in the `{ error, code }` shape; 404 `recipeNotFound` for an unpublished recipe in the variant flow; 200 with the documented body in both flows, where the fake's recorded `ReadRequest` carries the chosen recipe's `RecipeKey` in the variant flow and none in the recipe flow; a `noRecipe` outcome; 502 `readFailed` after an error status from the service, with the month's total unchanged; 502 `readFailed` after a failed connection and 504 `readTimedOut`, each with the reservation kept; 409 `ceilingReached` with `availableAgainOn` and no reader call when the month's key is set at the ceiling; a change in the month's total equal to the read's actual cost; a request whose client goes away mid-read, where the read still runs to its end and its actual cost is settled; no media item created by a read; and 429 on the eleventh read in an hour
- [X] T035 [P] [US1] Write `tests/KCC.IntegrationTests/Features/Pages/RecipeImportPagePropsTests.cs` on the shared `UmbracoSite`, with an `[After(Test)]` hook that resets the fake reader: both wizard pages render `photo-import` per `specs/001-photo-recipe-import/contracts/page-props.md`, disabled while the fake delegates, and enabled and available once the test switches it on
- [X] T036 [P] [US1] Add to `tests/KCC.ViteTests/Features/Components/RecipeWizard/recipeWizardFlow.test.ts` the photo path in both modes (start, photos, reading, review); `applyRead` filling the values, the marks and `otherRecipes`; each ingredient's and each step's mark staying on its own row when another row is removed or added, and when the review drops a blank row; the view-only photo strip showing on the four editing steps and the review while the import holds photos, and not on the start, photo, reading or submitted step; a `noRecipe` read staying on the photo step; a failed read (502, 504, a network failure or any other status) keeping the photos and offering Try again and Type it in; a cancelled read showing no message; an 11th photo refused; a removed photo taken out of the import, with its object URL revoked; `editedSinceRead` per `specs/001-photo-recipe-import/data-model.md` ("Wizard flow state"): set by typing or changing a value and by adding or removing an ingredient or step row, counted from the wizard's start or from the last applied read, cleared by applying a read, left as it was by a `noRecipe` read, and never set by Next, Back, the start choice or a photo added or removed; and `hasUnsavedWork`. Add the "Read" rows of `contracts/wizard-ui.md` ("Errors") to `wizardErrors.test.ts`, apart from the 409 row, which T055 adds (until T057 a 409 falls through to `ReadFailed`), with a 500 and a 502 without a code as "any other failure". Write `tests/KCC.ViteTests/Features/Components/RecipeWizard/photoEncoding.test.ts` for the target size (keeps the aspect ratio, never enlarges, respects `maxImageEdge`), the quality steps (0.85, 0.7, 0.55, then refusal above 2.2 MB), a photo that a stubbed `createImageBitmap` cannot decode, such as a HEIC file outside Safari, refused as `UnsupportedFile` as an oversized one is, and a stub drawing context that is drawn on with `imageSmoothingQuality` set to `'high'` (research R6). Write `tests/KCC.ViteTests/Features/Components/RecipeWizard/leaveGuard.test.ts`: a plain primary click on a same-origin link asks first, and a click with a modifier key or another button, a link with `target="_blank"` or `download`, a link to another origin, a link to a fragment of the same page and a click that another handler cancelled do not
- [X] T037 [P] [US1] Write SSR tests in `tests/KCC.ViteTests/Features/Components/RecipeWizard/`: `StartStep.test.ts` (both start hooks), `PhotoStep.test.ts` (hooks, privacy note, photo count, the too-many-photos and unsupported-file messages, and `NoRecipeFound` after a `noRecipe` read), `PhotoStrip.test.ts` (one `photo-tile` `<button type="button">` per photo, named by `ViewPhoto` and holding its image; `photo-remove` beside the tile button, never inside it, on the photo step's strip and absent from the view-only one; and no `kcc-tilewrap` or `kcc-tile` class in the strip), `ReadingState.test.ts` (`aria-busy`, `reading-cancel`), `ValueMark.test.ts` (the kind, a model `reason` as given, and a `code` rendered from its `MarkReason*` key with its `arg`), and `ReviewStep.test.ts` (marks, a `MissingValue` notice at each required value that a read left empty, with Submit disabled, and the other-recipes message). `PhotoViewer` and `LeaveConfirm` teleport to `body` only after mount, as `CookMode.vue` does, so the server prints neither; T038 and T086 test them in the browser
- [X] T038 [US1] Write `tests/KCC.E2ETests/Features/Members/RecipeImportTests.cs` with fixture photos in `tests/KCC.E2ETests/Fixtures/`, which `tests/KCC.E2ETests/KCC.E2ETests.csproj` copies to the output: on Create Recipe, choose `wizard-start-photos`, set `photo-input` to two fixtures, remove the second with its `photo-remove` button and find one `photo-tile` left, choose `read-photos`, see `review-step`, with no `match-step` on the way, and an `estimate` and an `unclear` `value-mark`, click a `photo-tile` button in `wizard-photos` to open `photo-viewer` (`role="dialog"` with `aria-modal`), check that Tab and Shift+Tab keep focus inside it, close it with its close button and find focus back on that `photo-tile` button, then open it again, close it with Escape and find focus back on the button again, submit, find `wizard-closing-link` leading to the recipe listing's URL that the page passed (`/recipes/` on the seeded site), and find the recipe pending on the Account page; from a recipe page's Add a variant, import one photo, reach the review with no match step, submit a variant, and find `wizard-closing-link` naming the recipe and leading to its page; and, with `navigator.wakeLock` replaced by a recorder in an init script, a read requests one screen wake lock and releases it once the review opens
- [X] T039 [US1] In `tests/KCC.E2ETests/BasePageTest.cs`, add `Args = ["--use-fake-device-for-media-stream", "--use-fake-ui-for-media-stream"]` to the launch options of `BasePageTests`, so that every pooled browser has a fake camera, granted without a prompt (research R18). Then write `tests/KCC.E2ETests/Features/Members/RecipeImportCameraTests.cs`, deriving from `BasePageTests` with no launch options and no permission grant of its own: `camera-view` shows, and two `camera-shutter` presses make two `photo-tile`s; with an init script that makes `navigator.mediaDevices.getUserMedia` reject with a `NotAllowedError`, `camera-message` shows the `CameraDenied` text and `photo-input` still adds a photo (SC-009); with an init script that makes it reject with a `NotFoundError`, `camera-message` shows the `CameraMissing` text; and with an init script that wraps `getUserMedia` and records every track it hands out, every track has ended once the member leaves the photo step, also for a stream that arrives only after the step was left
- [X] T086 [US1] Write `tests/KCC.E2ETests/Features/Members/RecipeImportLeaveTests.cs`, in Chromium: after a photo is added on Create Recipe, the header's logo link (`.pad-logo`) opens `leave-dialog` instead of leaving, and on Add Variant so does the hero's Cancel; the dialog has `role="alertdialog"` and `aria-modal`, with focus on `leave-stay`; Tab and Shift+Tab keep focus inside it; `leave-stay`, and Escape, each close it with the photo still in the strip and focus back on the link that was clicked; `leave-confirm` follows the link and raises no `beforeunload` dialog; a reload still raises a `beforeunload` dialog; and with no photo added, the same links leave at once

### Implementation for User Story 1

- [X] T040 [US1] Add `src/KCC.Web/Features/RecipeImport/ReaderOutputSchema.json`, copied from `specs/001-photo-recipe-import/contracts/reader-output.schema.json`, as an embedded resource in `src/KCC.Web/KCC.Web.csproj`
- [X] T041 [US1] Make T030 pass with `ReaderPrompt.cs` (the rules in research R19, without the catalog section) and `AnthropicRecipePhotoReader.cs` in `src/KCC.Web/Features/RecipeImport/`: `IsConfigured` from the API key and the model's profile, with an error logged for a model that has none; a deadline of `RecipeImport:ReadTimeoutSeconds` linked to the caller's token; one request with no retry, through a message service derived once as `client.Messages.WithOptions(o => o with { MaxRetries = 0 })`, an `IMessageService` that nothing disposes (research R4); a result that says whether the service answered; and logs that hold the outcome, model, tokens and cost only. Register the reader in `src/KCC.Web/Features/RecipeImport/RecipeImportComposer.cs`
- [X] T042 [P] [US1] Make T031 pass with `src/KCC.Web/Features/RecipeImport/ReadResultNormalizer.cs`
- [X] T043 [P] [US1] Make T032 pass with `src/KCC.Web/Features/RecipeImport/PhotoValidation.cs`
- [X] T044 [US1] Add the `imports` policy (`RateLimits.Imports`, `RateLimitOptions.ImportsPerHour` = 10) in `src/KCC.Web/Features/Security/RateLimits.cs` and `src/KCC.Web/Features/Security/SecurityComposer.cs`, the way `Submissions` is added, and set `RateLimits__ImportsPerHour` to 1000 in `tests/KCC.E2ETests/Config/SiteProcess.cs`
- [X] T045 [US1] Make T034 pass with `src/KCC.Web/Features/RecipeImport/RecipeImportApiController.cs` (research R11): `flow` and `recipeKey` bound as strings, with the validated recipe's key passed as `ReadRequest.RecipeKey` in the variant flow; reservation and settlement through `IImportSpendLedger` for the current UTC month, by the table in `specs/001-photo-recipe-import/contracts/read-endpoint.md` ("Cost accounting"); the read and its settlement run without `HttpContext.RequestAborted` (research R4); both flows; and no catalog yet (matching comes in US4)
- [X] T046 [US1] Make T033 pass with `src/KCC.Web/Features/DevTools/RecipeImport/StandInRecipePhotoReader.cs` (a fixed recipe with an estimated prep time, an unclear ingredient and a reader-written description, at no cost, after `RecipeImport:StandInDelaySeconds`) and its gate in `src/KCC.Web/Features/RecipeImport/RecipeImportComposer.cs`; and in `tests/KCC.E2ETests/Config/SiteProcess.cs`, set `RecipeImport__StandInReader` to true and pin `RecipeImport__StandInDelaySeconds` to 0 and `RecipeImport__MonthlyCeilingUsd` to 2 beside it, so that a shell that ran the manual scenarios cannot change them (research R15)
- [X] T047 [US1] Make T035 pass: register `ImportAvailability` in `src/KCC.Web/Features/RecipeImport/RecipeImportComposer.cs`, and pass `photo-import`, beside `recipes-url` on Create Recipe, from `CreateRecipePageController.cs`, `CreateRecipeViewModel.cs` and `Index.cshtml` in `src/KCC.Web/Features/Pages/CreateRecipe/`, and from the same three files in `src/KCC.Web/Features/Pages/AddVariant/`
- [X] T048 [P] [US1] Make the T036 encoding and error tests pass with `src/KCC.Web/Features/Components/RecipeWizard/photoEncoding.ts` (research R6, drawing with `imageSmoothingQuality` set to `'high'`) and the "Read" rows of `wizardErrors.ts` apart from the 409 row, which T057 adds, and add `src/KCC.Web/Features/Components/RecipeWizard/recipeImportApi.ts`, which posts `photos`, `flow` and `recipeKey` through `postForm` with an `AbortSignal`
- [X] T049 [US1] Make the T036 flow tests pass in `recipeWizardFlow.ts` and `wizardValues.ts` in `src/KCC.Web/Features/Components/RecipeWizard/`, with each row's mark kept on its row
- [X] T050 [US1] Make T037 pass: build `StartStep.vue` (without the unavailable state, which US2 adds), `PhotoStep.vue`, `CameraView.vue`, `PhotoStrip.vue` with its view-only mode, `PhotoViewer.vue`, `ReadingState.vue` and `ValueMark.vue` in `src/KCC.Web/Features/Components/RecipeWizard/` with the `kit-builder` agent, per `specs/001-photo-recipe-import/contracts/wizard-ui.md` and research R18 (rear camera up to 3840×2160, `ImageCapture.takePhoto()` where present, every track stopped on unmount, a `NotAllowedError` shown as `CameraDenied` and any other camera failure as `CameraMissing`, the strip as an in-flow row of tile buttons with no `kcc-tilewrap` or `kcc-tile`, the viewer returning focus to the tile button that opened it, Cancel aborting the request, and a screen wake lock held through `useWakeLock` from `src/KCC.Web/Features/Pages/VariantDetail/useWakeLock.ts` while a read runs, where a grant that arrives after `release()` is dropped, with failing tests first in `tests/KCC.ViteTests/Features/Pages/VariantDetail/useWakeLock.test.ts`)
- [X] T051 [US1] Wire the import into `src/KCC.Web/Features/Components/RecipeWizard/RecipeWizard.vue`, `ReviewStep.vue` and the step components for both modes: marks on every step and on the review, the missing values with Submit disabled, `read-error`, the no-recipe and other-recipes messages, and the view-only photo strip (`wizard-photos`) on the four editing steps and the review; and, because Create Recipe now opens on the start step, make `tests/KCC.E2ETests/Features/Members/MemberFlowTests.cs` and `tests/KCC.E2ETests/Features/Backoffice/BackofficeTests.cs` choose `wizard-start-typing` first
- [X] T087 [US1] Make `leaveGuard.test.ts` (T036) and T086 pass with `leaveGuard.ts` and `LeaveConfirm.vue` in `src/KCC.Web/Features/Components/RecipeWizard/`, built with the `kit-builder` agent per `specs/001-photo-recipe-import/contracts/wizard-ui.md` ("Behaviour", Leaving) and research R20: while `hasUnsavedWork` is true, `RecipeWizard.vue` asks in the page before any same-origin link is followed, keeps focus inside the dialog, returns focus to the link after Stay or Escape, keeps a `beforeunload` handler for reload and close, and removes that handler once the member chooses Leave
- [X] T052 [US1] Add the 40 keys of "Photo import and marks" in `specs/001-photo-recipe-import/contracts/wizard-ui.md`, `MissingValue` and `NoRecipeFound` included, to `src/KCC.Web/Features/Components/RecipeWizard/recipeWizardKeys.ts`, and write their `recipewizard.<key>.config` files in `src/KCC.Web/uSync/v17/Dictionary/`, so T021 and T022 pass again
- [X] T053 [US1] Build the evaluation tool: write failing scorer tests in `tests/KCC.UnitTests/Tools/ReaderEvalScorerTests.cs`, the cold penalty, the run's largest cold penalty applied to every model (A18), the warm 90th percentile, and SC-002 met only when the warm 90th percentile plus that penalty is at most 50 seconds (A22) included; failing encoder tests in `tests/KCC.UnitTests/Tools/ReaderEvalEncoderTests.cs` (the EXIF orientation applied, the long edge scaled down to the edge and never enlarged, JPEG at quality 85, then 70, then 55, and a photo still over 2.2 MB refused); and failing run-order tests in `tests/KCC.UnitTests/Tools/ReaderEvalRunOrderTests.cs`: each model's timed reads, its cold first call and that case read again included, come before any of its `count_tokens` calls (A23); create `tools/KCC.ReaderEval/KCC.ReaderEval.csproj` (a net10.0 console that references `src/KCC.Web/KCC.Web.csproj`) in a `/tools/` folder of `KitchenCommandCenter.slnx`, and reference it from `tests/KCC.UnitTests/KCC.UnitTests.csproj`; then implement it per research R16 and `specs/001-photo-recipe-import/quickstart.md`: `--draft`, `--models`, `--check`, `--edge` (2000 by default), every photo encoded before each read with the ImageSharp that KCC.Web already brings, at the lower of the run's edge and the model's image edge, scoring for SC-002 to SC-004, each model's first call timed apart from the rest and its case read again for the cold penalty, a model judged on SC-002 against 50 seconds (A22), image tokens measured with `count_tokens` calls that carry the output schema and come after that model's timed reads (A23), the key from KCC.Web's user-secrets or `ANTHROPIC_API_KEY` and never printed, and a cost estimate with a confirmation before any paid call; then run `--check` once, which calls only `count_tokens` and spends nothing

**Checkpoint**: A member imports a recipe from Create Recipe or Add Variant and submits it; quickstart scenarios 1, 3,
7, 9 and 10 pass

---

## Phase 4: User Story 2 - Keep import spending under the monthly ceiling (Priority: P1)

**Goal**: At the ceiling, the wizards say when the photo import comes back and keep typing open; the owner can change
the ceiling on the Pi

**Independent Test**: With the ceiling above zero and at or below the cost of one read, a member completes one import;
the next attempt shows the import as unavailable until the next month, and typing a recipe in still works

### Tests for User Story 2 ⚠️

- [X] T054 [P] [US2] Write the story's acceptance test `tests/KCC.IntegrationTests/Features/RecipeImport/RecipeImportCeilingTests.cs` on the shared `UmbracoSite`, with an `[After(Test)]` hook that resets the fake reader and the current month's key: with the fake switched on and the month's key set just under the site's ceiling, one read whose scripted cost covers the gap succeeds, the next answers 409 `ceilingReached`, both pages then render `photo-import` as unavailable with `availableAgainOn`, and a typed `POST /api/recipes` still succeeds. The site's ceiling is not the default, so the test also proves that `RecipeImport:MonthlyCeilingUsd` binds. The server paths it covers landed with US1's T034 and T045
- [X] T055 [P] [US2] Add to `tests/KCC.ViteTests/Features/Components/RecipeWizard/recipeWizardFlow.test.ts`: a 409 `ceilingReached` result keeps the flow on the photo step with its photos, marks the import unavailable with `availableAgainOn`, turns Read recipe off, and leaves Type it in; add to `tests/KCC.ViteTests/Features/Components/RecipeWizard/wizardErrors.test.ts` the 409 row of `specs/001-photo-recipe-import/contracts/wizard-ui.md` ("Errors"): `ceilingReached` maps to `ImportUnavailable` with the `availableAgainOn` of the result's `body`; and add to `tests/KCC.ViteTests/Features/Utilities/Api.test.ts` that a failure result carries the parsed body as `body` when it is a JSON object, else null
- [X] T056 [P] [US2] Add to `tests/KCC.ViteTests/Features/Components/RecipeWizard/StartStep.test.ts` and `PhotoStep.test.ts`: `import-unavailable` shows the `ImportUnavailable` string with the date, and the typing choice stays

### Implementation for User Story 2

- [X] T057 [US2] Make T055 and T056 pass in `src/KCC.Web/Features/Utilities/Api.ts` (the failure result's `body`), `recipeWizardFlow.ts`, `wizardErrors.ts`, `RecipeWizard.vue`, `StartStep.vue` and `PhotoStep.vue` in `src/KCC.Web/Features/Components/RecipeWizard/`, and add `ImportUnavailable` to `recipeWizardKeys.ts` with its `src/KCC.Web/uSync/v17/Dictionary/recipewizard.importunavailable.config`
- [X] T058 [US2] Add `RecipeImport__MonthlyCeilingUsd: ${KCC_IMPORT_CEILING_USD:-2}` to the app's environment in `deploy/compose.yaml`; in `deploy/.env.example`, document `KCC_IMPORT_CEILING_USD` and widen the `ANTHROPIC_API_KEY` comment to the photo import; and in `docs/hosting/runbook.md`, say what the key turns on, that each site holding the key reads for real under its own ledger and its own ceiling, the owner's dev site included (spec A2), how to change the ceiling, and that one organization spend limit of about US$20 a month in the Anthropic Console backs the ledgers, which the owner sets and raises before each later evaluation (`specs/001-photo-recipe-import/quickstart.md`, "Turning it on at the Pi")

**Checkpoint**: US1 and US2 together are the shippable minimum: the import works and cannot overspend

---

## Phase 5: User Story 3 - Jump from the review to any step and back (Priority: P2)

**Goal**: Every review section opens its step, and each step returns straight to the review, for typed and imported
recipes

**Independent Test**: A member types a recipe, edits the ingredients from the review, and returns to the review in one
action with the change shown

### Tests for User Story 3 ⚠️

- [X] T059 [P] [US3] Add to `tests/KCC.ViteTests/Features/Components/RecipeWizard/recipeWizardFlow.test.ts`: `edit(section)` opens the step with `returnTo` set; `backToReview()` and Next both return to the review; editing a marked value removes its mark, and editing a marked row removes that row's mark and no other; `startRead()` needs a confirmation whenever `editedSinceRead` is true, whether or not a read result exists, from the review's Read again and from the photo step's Read recipe alike (values typed before a first read, by Type it in and then Back to the start step and From photos, ask; an ingredient or step row added or removed with nothing else changed, followed by Read again, asks; an untouched wizard asks nothing; and a read with no value changed since the last applied read asks nothing, photos added or not); `changePhotos()` keeps the photos and, while the wizard holds values from an applied read, sets `returnTo` to the review, so `backToReview()` leaves the photo step for the review, and Back on the review of a photos path does the same as `changePhotos()`; a read started from the review, or from the photo step with that return, that is cancelled, fails, finds no recipe or gets a 409 lands on the photo step with the return kept; and applying a read clears it (A21)
- [X] T060 [P] [US3] Add to `tests/KCC.ViteTests/Features/Components/RecipeWizard/ReviewStep.test.ts`: `review-edit-basics`, `review-edit-variant`, `review-edit-ingredients` and `review-edit-instructions` in recipe mode, no `review-edit-basics` in variant mode, an `EditSection` label on each, `change-photos` and `read-again` for an import, `read-again-warning` with `read-again-confirm` while a confirmation is pending, and `back-to-review` on a step opened from the review; and to `PhotoStep.test.ts`: the same `read-again-warning` with `read-again-confirm`, and `back-to-review` while the flow returns to the review (A21)
- [X] T061 [US3] Write `tests/KCC.E2ETests/Features/Members/RecipeWizardReviewTests.cs`: a typed recipe reaches the review, `review-edit-ingredients` opens the ingredients with focus on the step's heading, and after a change `back-to-review` shows it on the review with no step in between and focus on the review's heading; for a recipe imported from a recipe page's Add a variant, which has no match step, a photo opens from `wizard-photos` on the ingredients step, editing the unclear ingredient removes its `value-mark`, `read-again` shows `read-again-warning` first, after `change-photos` the photo step's `back-to-review` returns to the review with the edit kept (A21), and after `change-photos` again and one more photo `read-photos` shows the warning too, before `read-again-confirm` fills the review from the new read

### Implementation for User Story 3

- [X] T062 [US3] Make T059 pass in `src/KCC.Web/Features/Components/RecipeWizard/recipeWizardFlow.ts`
- [X] T063 [US3] Make T060 and T061 pass in `ReviewStep.vue`, `PhotoStep.vue`, the four step components and `RecipeWizard.vue` in `src/KCC.Web/Features/Components/RecipeWizard/`: Edit buttons (`kcc-btn--text`), Back to review, on the photo step too after Change photos (A21), Change photos, Read again, the `kcc-well--warning` that guards every read after an edit, and focus on the new step's heading after every step change
- [X] T064 [US3] Add the keys of "Step links" in `specs/001-photo-recipe-import/contracts/wizard-ui.md` to `src/KCC.Web/Features/Components/RecipeWizard/recipeWizardKeys.ts`, and write their `recipewizard.<key>.config` files in `src/KCC.Web/uSync/v17/Dictionary/`, so T021 and T022 pass again

**Checkpoint**: Quickstart scenario 2 and T061 pass: the step links work for typed and imported recipes

---

## Phase 6: User Story 4 - Add the photos as a variant of an existing recipe (Priority: P3)

**Goal**: From Create Recipe, the reader proposes a published recipe and the member decides. The Add Variant photo
path, the story's scenario 6, already arrived with US1

**Independent Test**: With a published taco recipe, a member imports a taco recipe from Create Recipe, accepts the
proposal, and the submission creates an unpublished variant under it

### Tests for User Story 4 ⚠️

- [X] T065 [P] [US4] Write `tests/KCC.UnitTests/Features/RecipeImport/RecipeCatalogTests.cs` (ids `R1` to `Rn` in name order, the 500 cap, descriptions cut to 160 characters, variant names, and an unknown id that maps to nothing) and `tests/KCC.UnitTests/Features/RecipeImport/DuplicateVariantCheckTests.cs` (the normalization and thresholds in research R9, the highest overlap winning across the variants of every recipe it is given, an equal overlap going to the recipe that comes first in catalog order and then to the variant that comes first in its recipe (A25), and the hit naming its variant and its recipe)
- [X] T066 [P] [US4] Add to `tests/KCC.UnitTests/Features/RecipeImport/AnthropicRecipePhotoReaderTests.cs`: a recipe-flow read carries the catalog that its `ReadRequest` holds in its text block, and a variant-flow read, whose request holds none, carries none
- [X] T067 [P] [US4] Add to `tests/KCC.IntegrationTests/Features/RecipeImport/RecipeImportApiTests.cs`: a fake read with `matchId` `R1` gives `match` with that recipe's key and name, and the `recipes` list; an unknown `matchId` gives no match; the variant flow gives neither; a recipe-flow read whose ingredients copy a seeded variant gives `possibleDuplicate` with that variant and its recipe, whether or not the read proposes that recipe; a variant-flow read that copies a variant of the chosen recipe gives it too; and a variant-flow read that copies another recipe's variant gives none
- [X] T068 [P] [US4] Add to `tests/KCC.ViteTests/Features/Components/RecipeWizard/recipeWizardFlow.test.ts`: `acceptMatch` and `pickRecipe` switch to variant mode and set the recipe values aside, `keepNewRecipe` restores them, Change on the review returns to the match step, the view-only photo strip shows on the match step, a `possibleDuplicate` stays with the import through each of those choices and in both modes, none of those choices sets `editedSinceRead`, a variant-mode submission goes to `POST /api/recipes/{key}/variants`, and a read started on Create Recipe after `acceptMatch` or `pickRecipe` builds its request with `flow` `recipe` and no `recipeKey`, and applying it returns the wizard to recipe mode, drops the set-aside values and the chosen recipe, and opens the match step when the new read proposes a recipe, or the review when it does not (A20)
- [X] T069 [P] [US4] Write `tests/KCC.ViteTests/Features/Components/RecipeWizard/MatchStep.test.ts`: `match-step`, `match-accept`, `match-pick`, `match-keep`, and `possible-duplicate` naming the variant and its recipe; and add to `ReviewStep.test.ts` the `AddingTo` line with its Change control, and `possible-duplicate` in recipe mode and in variant mode
- [X] T070 [US4] Write `tests/KCC.E2ETests/Features/Members/RecipeImportMatchTests.cs`: with two fixture photos, the stand-in proposes the first published recipe in name order (A25), `wizard-photos` shows on the match step, and `match-accept` with a submission creates a pending variant under that recipe and ends on a `wizard-closing-link` to the recipe listing's URL that the page passed (`/recipes/` on the seeded site); with three fixture photos on Create Recipe, no match step shows, and the review's `possible-duplicate` names that recipe's first variant; and with three fixture photos from a recipe page's Add a variant, the review shows `possible-duplicate` naming that recipe's first variant, and the submission still goes through

### Implementation for User Story 4

- [X] T071 [US4] Make T065 pass with `src/KCC.Web/Features/RecipeImport/RecipeCatalog.cs` and `src/KCC.Web/Features/RecipeImport/DuplicateVariantCheck.cs`
- [X] T072 [US4] Make T066 and T067 pass: `ReadRequest` in `src/KCC.Web/Features/RecipeImport/ReadResponse.cs` gains the recipe flow's catalog, null in the variant flow; the catalog section of `src/KCC.Web/Features/RecipeImport/ReaderPrompt.cs` for the recipe flow; and in `src/KCC.Web/Features/RecipeImport/RecipeImportApiController.cs`, one catalog built per request, which prices the reservation by its characters, goes to the reader in the `ReadRequest` and maps `matchId` back; the `recipes` list; and the duplicate check: against every catalog recipe's variants in the recipe flow, and against the chosen recipe's variants in the variant flow
- [X] T073 [US4] Make `src/KCC.Web/Features/DevTools/RecipeImport/StandInRecipePhotoReader.cs` propose the catalog's `R1`, the first published recipe in name order (A25), when the import holds two photos and, when it holds three, propose none and give its recipe the ingredients and steps of a published variant: the chosen recipe's first variant in the variant flow, and `R1`'s first variant in the recipe flow, a first variant being the first in its recipe's variant order (research R9, R15)
- [X] T074 [US4] Make T068 to T070 pass with `recipeWizardFlow.ts`, `MatchStep.vue`, the review's `AddingTo` line and Change, the possible-duplicate notice on the match step and on the review, the view-only photo strip on the match step, and a read after a match choice that posts `flow` `recipe` and returns the wizard to recipe mode (A20), in `src/KCC.Web/Features/Components/RecipeWizard/`
- [X] T075 [US4] Add the keys of "Matching" in `specs/001-photo-recipe-import/contracts/wizard-ui.md` to `src/KCC.Web/Features/Components/RecipeWizard/recipeWizardKeys.ts`, and write their `recipewizard.<key>.config` files in `src/KCC.Web/uSync/v17/Dictionary/`, so T021 and T022 pass again
- [X] T076 [US4] Add match cases (`set/catalog.json`) and SC-005 scoring to `tools/KCC.ReaderEval/`, with failing scorer tests first in `tests/KCC.UnitTests/Tools/ReaderEvalScorerTests.cs`

**Checkpoint**: Quickstart scenarios 4 and 11 pass, and every user story works

---

## Phase 7: Reader selection

**Purpose**: Measure the readers on real photos and set the default model (research R1, R5, R16)

- [ ] T077 OWNER ACTION: put 15 to 20 cases in `tools/KCC.ReaderEval/set/` as `specs/001-photo-recipe-import/quickstart.md` ("Reader evaluation") describes, with the original photos: several handwritten cards, and some recipes already on the site; look at the rate limits for `claude-haiku-5-5`, `claude-haiku-4-5`, `claude-sonnet-5-5` and `claude-opus-5-5` in the Anthropic Console before the run, and check that the organization spend limit is set; then approve the spend of about US$18 for the first evaluation at the default image edge of 2000 px: about US$6 for T078's draft run, which reads every case once with `claude-opus-5-5`, and about US$12 for T079's comparison of those four models. A run at another edge needs an approval of its own. Stop until the owner confirms
- [X] T078 Under the spend that T077 approved, run `dotnet run --project tools/KCC.ReaderEval -- --draft` (about US$6), and have the owner correct each `expected.draft.json` in `tools/KCC.ReaderEval/set/` into `expected.json`. The draft run's first call is the schema's first paid use: if the service rejects the schema, stop and report to the owner
- [ ] T079 At least 24 hours after T078's last call, with no call that carries the output schema in those 24 hours (no `--check`, draft run, other evaluation run or real read; A17, A18, research R16), run the comparison of `claude-haiku-5-5`, `claude-haiku-4-5`, `claude-sonnet-5-5` and `claude-opus-5-5` at the default image edge of 2000 px, under the spend that T077 approved (about US$12), with each model's `count_tokens` calls after its timed reads (A23); judge SC-002 for each model on its warm 90th-percentile time plus the largest cold penalty measured in the run, against 50 seconds (A22), a model's cold penalty being its first call's time minus the time of the same case read again; before changing any figure below, change it first in every test that pins it, `ReadCostTests.cs` (T006), `ImportAvailabilityTests.cs` (T012) and `AnthropicRecipePhotoReaderTests.cs` (T030) in `tests/KCC.UnitTests/Features/RecipeImport/` among them, and see those tests fail; set the `Model` and `Effort` defaults in `src/KCC.Web/Features/RecipeImport/RecipeImportOptions.cs` and `specs/001-photo-recipe-import/contracts/configuration.md` to the cheapest model that meets SC-002 to SC-005, and change the `MaxImageEdge` default there only to an edge that a run measured, a run at another edge being a separate run that the owner approves first; correct each `imageTokenCap`, and confirm or correct Haiku 5.5's presumed image edge, in `src/KCC.Web/Features/RecipeImport/ModelProfiles.cs` and research R5 from the measured image tokens; and correct `CatalogCharactersPerToken`, `ReadCostTests` and research R5 if the catalog's measured characters per token is under 3.5 (A31); and record the results under "Measured results" in `specs/001-photo-recipe-import/research.md`. If no model passes, stop and report to the owner

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Docs, brand review and the definition of done

- [X] T080 [P] Update `README.md`: the imports limit beside the other rate limits, the `RecipeImport` settings, a photo-import section on the stand-in reader, the evaluation tool and the real reads of a dev site whose user-secrets hold the key (spec A2), and the "Schema and baseline content" bullets (hand-written uSync files proven by a boot test, per constitution 1.1.0, and the retired baseline export endpoint)
- [X] T081 [P] Add the ledger's write path to "SQLite writes" in `CLAUDE.md`: it writes Umbraco's key-value table and takes `Constants.Locks.KeyValues` before its first read
- [X] T082 Run the `brand-steward` agent over `src/KCC.Web/Features/Components/RecipeWizard/` and the two wizard pages, and fix every violation it reports (D7)
- [X] T083 Check the start, photo, reading, match and review steps, an editing step with its photo strip, the possible-duplicate notice and the leave confirmation from `src/KCC.Web/Features/Components/RecipeWizard/` in the light and the dark ramp in the Browser pane against the running site (D6), and add any new text-on-surface pairing, such as a mark inside a warning well, to `tests/KCC.ViteTests/Features/Styles/contrast.test.ts`
- [ ] T084 Run scenarios 1 to 5 and 7 to 12 of `specs/001-photo-recipe-import/quickstart.md` on the dev site with the stand-in reader, with the `RecipeImport__*` variables on the start command and never exported, scenario 12 in Safari on a Mac, and time SC-001 on scenario 3; if the read in scenario 12 fails at about 60 seconds, report it to the owner with the remedy in research R18; run scenario 6 only with the owner's approval, because it spends money, time SC-001 on it again (A24), and record its time from Read recipe to the filled review (A22) and both SC-001 times under "Measured results" in `specs/001-photo-recipe-import/research.md`
- [X] T085 Run definition-of-done rows D1 to D5 and D10, and D8 and D9 for the `deploy/` change, with the commands in `.specify/memory/constitution.md` ("Definition of done") and `specs/001-photo-recipe-import/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies.
- **Foundational (Phase 2)**: depends on Setup and blocks every user story. Its checkpoint must hold, with all six
  suites green, before the import UI starts.
- **US1 (Phase 3)**: depends on Foundational.
- **US2 (Phase 4)**: depends on US1's import UI (`StartStep.vue`, `PhotoStep.vue`), its `wizardErrors.ts`, which
  gains the 409 row, and its endpoint.
- **US3 (Phase 5)**: its typed-recipe part depends only on Foundational; its imported-recipe part depends on US1. It
  runs after US2, because both edit the flow module, its test file, `RecipeWizard.vue` and the key map.
- **US4 (Phase 6)**: depends on US1's reader, endpoint and photo path. It runs after US3, because both edit the flow
  module, its test file, `ReviewStep.vue` and the key map.
- **Reader selection (Phase 7)**: depends on US4, because SC-005 needs match cases. It does not block US3.
- **Polish (Phase 8)**: depends on every earlier phase.

### Within Each Phase

- A test task comes before the task that makes it pass, and the test must fail first (constitution IV). T054 is the
  one exception: it is the story's acceptance test over server paths that US1's tests already drove.
- Server: options and profiles, then the reader contract and the fake reader on the test host, the ledger, the reader,
  the normalizer, the endpoint and the page props.
- Client: the flow module, then the components, then their keys and strings.
- Every task that adds keys runs T021 and T022 again.
- T026 hands the wizard the `recipesUrl` prop, and T028 fills it from the server, before T029 checks the closing link.

### Parallel Opportunities

- Phase 1: T002 and T003.
- Phase 2: the test tasks T004, T006, T011, T012, T015 and T017 to T022, together; T007 beside them.
- US1: tests T030 to T037 together; then T042, T043 and T048 beside T041.
- US2: T054 to T056 together.
- US3: T059 and T060 together.
- US4: T065 to T069 together.

## Parallel Example: User Story 1

```bash
# Write the failing tests for User Story 1 together:
Task: "AnthropicRecipePhotoReaderTests in tests/KCC.UnitTests/Features/RecipeImport/AnthropicRecipePhotoReaderTests.cs"
Task: "ReadResultNormalizerTests in tests/KCC.UnitTests/Features/RecipeImport/ReadResultNormalizerTests.cs"
Task: "PhotoValidationTests in tests/KCC.UnitTests/Features/RecipeImport/PhotoValidationTests.cs"
Task: "RecipeImportApiTests in tests/KCC.IntegrationTests/Features/RecipeImport/RecipeImportApiTests.cs"
Task: "StartStep, PhotoStep, PhotoStrip, ReadingState, ValueMark and ReviewStep SSR tests in tests/KCC.ViteTests/Features/Components/RecipeWizard/"

# Then the independent implementations together:
Task: "ReadResultNormalizer in src/KCC.Web/Features/RecipeImport/ReadResultNormalizer.cs"
Task: "PhotoValidation in src/KCC.Web/Features/RecipeImport/PhotoValidation.cs"
Task: "photoEncoding, wizardErrors and recipeImportApi in src/KCC.Web/Features/Components/RecipeWizard/"
```

---

## Implementation Strategy

### Minimum shippable increment (US1 and US2)

1. Complete Setup and Foundational. Typed wizards keep working, on shared components.
2. Complete US1. Validate quickstart scenarios 1, 3, 7, 9 and 10.
3. Complete US2. The import cannot overspend, and the Pi can change its ceiling.
4. Stop and validate: US1 and US2 are the smallest change that can merge (spec: "Story 1 cannot ship without this
   guard").

### Incremental delivery within the one pull request

Phase 1 of the feature is one numbered spec and one pull request (constitution ordering rule), so the stories land as
commits on `001-photo-recipe-import`. Add US3, then US4, then the reader selection and the polish. Each checkpoint
leaves all six suites green.

## Notes

- Commit as `Th3FenrisWolf`, passing the identity on every commit as the constitution's "Review and merge" shows.
- The tasks marked OWNER ACTION, the paid runs of T078 and T079, and quickstart scenario 6 spend the owner's money or
  need the owner's photos. Never run them without the owner's go-ahead: T077's approval covers T078 and T079, and
  scenario 6 needs its own.
- `[P]` tasks touch different files and depend on no unfinished task.
- Tasks run in the order listed, not in id order: T086 follows T039, and T087 follows T051.
- After the definition of done, card implement runs the CodeRabbit loop and opens the pull request against `main`. The
  branch sits directly on `main`, so the pull request carries this feature only (plan.md, "Pull request scope").
