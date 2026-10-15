# Implementation Plan: Recipe Import from Photos, Phase 1

**Branch**: `001-photo-recipe-import` | **Date**: 2026-10-07 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-photo-recipe-import/spec.md`

## Summary

Members turn photos of a recipe into a filled Create Recipe or Add Variant wizard.

- **Capture.** The page opens its own rear-camera view and also takes photos from the device. The browser shrinks each
  photo to a JPEG of at most 2.2 MB.
- **Reading.** A new member endpoint, `POST /api/recipe-imports/read`, sends 1 to 10 photos to an Anthropic model with
  a structured-output schema. It validates and limits the result, maps a proposed match back to a published recipe,
  flags a possible duplicate, and returns values with marks. Marks the server sets carry codes, never English text. It
  stores no photo. A read runs to its end under its own 85-second deadline, also when the member cancels or the
  connection drops.
- **Spending.** Every read reserves its worst-case cost, at the model's highest prices, against a US$2 monthly ceiling,
  kept in Umbraco's key-value table under its write lock, and then settles the real cost. A reservation is released
  only when the service answered. Each site that holds the key, the owner's dev site included, keeps its own total, and
  one organization spend limit of about US$20 a month in the Anthropic Console backs them all.
- **One wizard.** The two single-file wizards become one set of shared step components, driven by a plain TypeScript
  flow module. That module gives every review section an Edit link and a direct Back to review, for typed and imported
  recipes alike. An import's photos stay in view while it is checked and edited, and a page-wide guard asks before a
  member leaves unsubmitted work. Every string, error message included, comes from the `RecipeWizard` dictionary
  group.
- **Debt cleared.** The change fixes the Anthropic client's missing timeout and the icon provider's swallowed
  cancellation. The owner put the key on the Pi on 2026-10-05, so production runs with that debt until this ships.
- **Evaluation.** A console tool measures the readers on the owner's photos, encoded as the browser encodes them, so
  that a task can set the cheapest model that meets SC-002 to SC-005 as the default.

## Technical Context

**Language/Version**: C# on .NET 10 (`global.json`); TypeScript and Vue 3 on Node 24

**Primary Dependencies**:

- Umbraco CMS 17.7, and 17.8 or later at launch.
- The Anthropic C# SDK `Anthropic` 12.24.1, already referenced. It stays at that version, because the fallback was
  declined (research R3).
- Vue 3 with SSR, Vite, Tailwind CSS 4 and the Loose Leaf kit.
- No new package. The evaluation tool encodes photos with ImageSharp, which Umbraco already brings (research R16).

**Storage**:

- No photo is stored.
- One value per month in Umbraco's `umbracoKeyValue` table (`KCC.RecipeImport.SpendMicroUsd.<yyyy-MM>`), in the site's
  SQLite file.
- No migration and no schema change.

**Testing**:

- TUnit with Moq: unit tests, integration tests on fresh SQLite sites, and E2E tests with Playwright.
- Vitest in the node environment: plain TypeScript logic plus SSR markup.
- No test calls the paid service, and no test host sets an API key (research R15).
- The integration tests stay on the one host that the suite boots today (research R15).
- Both test hosts pin the stand-in reader's settings, so a variable left in the shell cannot change them (research
  R15).
- Every E2E browser launches with Chromium's fake camera, from the suite's shared base class (research R18).

**Target Platform**: A linux/arm64 container on a Raspberry Pi 5 behind Cloudflare Tunnel. Browsers: iOS Safari 17 or
later, Android Chrome, and current desktop browsers.

**Project Type**: Web application: an Umbraco site with a Vue SSR front end and Lit backoffice extensions. Phase 1
touches no backoffice extension.

**Performance Goals**:

- The filled review appears within 60 seconds for 9 of 10 reads of up to 3 photos (SC-002). The evaluation holds the
  reading service to 50 seconds of that, which leaves 10 seconds for uploading up to 3 photos and for the Pi's work
  (A22).
- A read is cut off at 85 seconds, inside Cloudflare's 100-second origin limit.
- Preparing a photo in the browser takes under 2 seconds.

**Constraints**:

- The read ceiling is US$2 a month for each site that holds the key (`RecipeImport:MonthlyCeilingUsd`), so the Pi and
  the owner's dev site can spend about US$4 a month together (spec A2).
- One organization spend limit of about US$20 a month in the Anthropic Console backs the ledgers. The owner sets it
  and raises it before each later evaluation.
- An import holds 1 to 10 photos. The browser sends each one at no more than 2.2 MB; the server accepts up to 4 MB per
  photo, 22 MB of photos together and 25 MB per request.
- Writes take their lock before their first read (constitution III).
- The camera needs HTTPS.
- The repository is public, so the evaluation photos stay in a gitignored folder.

**Scale/Scope**:

- A few members, and tens of reads a month.
- 25 to a few hundred published recipes in the matching catalog, capped at 500.
- 110 `RecipeWizard` strings, and two wizard pages rebuilt on shared components.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Checked against constitution 1.1.0, before and after design, after `/speckit-analyze` on 2026-10-07, and again on
2026-10-12, after the red team and after the second `/speckit-analyze`. No violation remains.

| Principle | How the plan meets it |
|---|---|
| I. Content Lives in the Backoffice | Every new string, the leave confirmation's and the missing-value notice's included, the Create Recipe wizard's 41 hard-coded ones, `NumberStepper`'s and `WizardProgress`'s fixed text, and every error the wizard shows come from the `RecipeWizard` dictionary group (R17, [wizard-ui.md](contracts/wizard-ui.md#errors)). The success sheet's closing link takes its label from a dictionary item and its href from content on both pages: the recipe listing's URL on Create Recipe and the recipe's URL on Add Variant. The wizard hard-codes no href (R14). Server-set marks carry codes, never English (R7). The ceiling is operational configuration, not content. There is no document-type change |
| II. Loose Leaf Is the Only Look | The new surfaces, the leave confirmation included, use kit patterns ([contracts/wizard-ui.md](contracts/wizard-ui.md), R20). The photo strip is a row of tile buttons built from Classes-table classes and Tailwind, never the kit's pinned, pointer-transparent tile, so `docs/brand/kit.md` and `Kit.css` do not change. `kit-builder` builds the surfaces and `brand-steward` reviews them. Both ramps are checked (D6, D7), and `contrast.test.ts` gains any new text-on-surface pairing. The kit's enforcement tests stay green |
| III. SQLite Has One Writer | The spend ledger takes `scope.WriteLock(Constants.Locks.KeyValues)` before its first read in every write scope (R10). The reservation and the settlement each open a core scope of their own, so no lock is held while a read runs. Submissions still go through `RecipeSubmissions` unchanged. The whole integration suite runs, including a ledger race test |
| IV. Tests First, Builds Clean | Every behaviour starts with a failing test (see `tasks.md`), the dialogs' focus traps, their focus return and the focus on each new step's heading included. Existing ids and roles stay; the two E2E submission tests move to `data-testid` locators ([wizard-ui.md](contracts/wizard-ui.md#test-hooks)). The integration tests stay on the one shared host, with one scriptable fake reader, so no second site boots in the test process (R15). The leave guard has a Chromium E2E case (R20). The camera cases run in the suite's pooled browsers, which all launch with a fake camera, and they stub `getUserMedia` for a denied or missing camera (R18). Hand-written uSync dictionary files are proven by a boot test, as 1.1.0 requires. There are no ModelsBuilder or migration changes |
| V. Cheap to Run, Cheap to Upgrade | The spec names the cost: a US$2 a month ceiling for each site that holds the key, about US$4 a month for the Pi and the owner's dev site together, plus about US$18 for the first evaluation (about US$6 for the draft run and about US$12 for the comparison) and about US$12 for each later comparison. One organization spend limit of about US$20 a month in the Anthropic Console backs the ledgers; the owner sets it and raises it before each later evaluation. A model with no price profile turns the import off, so the ledger never prices a read by guesswork, and every reservation prices at the profile's highest price set, so a read past a price threshold still fits its reservation (R5). No new package: the evaluation tool's encoder uses the ImageSharp that Umbraco brings. The endpoint derives from `ControllerBase` |
| VI. Members Earn Trust | The endpoint checks the member, the anti-forgery token and a new `imports` rate limit (10 an hour). Drafts stay unpublished. The key stays in user-secrets or the Pi's `.env`. The stand-in reader, with its delay option, answers only in Development and Testing, and both test hosts pin its settings |
| VII. Code Explains Itself | No narrating comments. The moved `AddVariant` keys are a clean cutover. `README.md`, `CLAUDE.md`, the runbook and `deploy/.env.example` are corrected in the same change |

**Gates for card implement**: gates 1 and 2 hold on the owner's Mac (Docker and shellcheck are installed). Gate 3 is
this table. Gate 4 holds: there is no open marker. Gate 5 holds: this is Phase 1 of 3, so no earlier phase is pending.

**Definition-of-done rows that apply**: D1 to D7 and D10, plus D8 and D9 because `deploy/compose.yaml` and
`deploy/.env.example` change. D5 applies for the hand-written dictionary files.

**Pull request scope**: the branch sits directly on `main`, which already holds the Spec Kit tooling, the constitution
and the ignore rule for `.specify/feature.json`. The pull request carries this feature only.

## Project Structure

### Documentation (this feature)

```text
specs/001-photo-recipe-import/
├── spec.md
├── plan.md                         # This file
├── research.md                     # R1–R20, and the evaluation's measured results once they exist
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── read-endpoint.md            # POST /api/recipe-imports/read
│   ├── reader-output.schema.json   # the model's structured output
│   ├── page-props.md               # props of the two wizard pages
│   ├── wizard-ui.md                # components, errors, test hooks, strings
│   └── configuration.md            # RecipeImport, RateLimits and the Pi
├── checklists/requirements.md
└── tasks.md
```

### Source Code (repository root)

```text
src/KCC.Web/
├── Features/
│   ├── RecipeImport/                                  # new
│   │   ├── RecipeImportApiController.cs               # the read endpoint (R11)
│   │   ├── RecipeImportComposer.cs                    # registrations and options binding
│   │   ├── RecipeImportOptions.cs                     # the RecipeImport section
│   │   ├── IRecipePhotoReader.cs                      # with IsConfigured (R13)
│   │   ├── AnthropicRecipePhotoReader.cs              # request, output parsing, usage (R2, R4, R5)
│   │   ├── ReaderPrompt.cs                            # R19
│   │   ├── ReaderOutputSchema.json                    # embedded copy of the contract's schema
│   │   ├── ModelProfiles.cs                           # price sets, thinking, effort, image edge and token cap (R5)
│   │   ├── ReadCost.cs
│   │   ├── ReadResultNormalizer.cs                    # R7
│   │   ├── RecipeCatalog.cs                           # R8
│   │   ├── DuplicateVariantCheck.cs                   # R9
│   │   ├── PhotoValidation.cs                         # R6
│   │   ├── ImportSpendLedger.cs                       # with IImportSpendLedger (R10)
│   │   ├── ImportAvailability.cs                      # R13
│   │   └── ReadResponse.cs                            # response and read-attempt records
│   ├── DevTools/RecipeImport/StandInRecipePhotoReader.cs   # new, Development and Testing only (R15)
│   ├── Components/RecipeWizard/                       # new: flow module, wizard values, keys, steps, success sheet, leave guard (wizard-ui.md)
│   ├── Components/Forms/NumberStepper.vue             # changed: valueLabel prop
│   ├── Components/Wizard/WizardProgress.vue           # changed: label prop
│   ├── Pages/CreateRecipe/                            # changed: merged strings, photo-import and recipes-url props, thin view
│   ├── Pages/AddVariant/                              # changed: merged strings, photo-import prop, thin view, its closing link; reviewSummary.ts folds into wizardValues.ts
│   ├── Pages/VariantDetail/useWakeLock.ts             # changed: a grant that arrives after release() is dropped (the reading step and Cook Mode)
│   ├── Utilities/Api.ts                               # changed: postForm with an abort signal; status and code on every failure result
│   ├── Security/RateLimits.cs, SecurityComposer.cs    # changed: the imports policy
│   └── Providers/ProvidersComposer.cs, RecipeIconProvider.cs   # changed: timeout, one retry, cancellation (R4); the key check's comment corrected (spec A2)
├── uSync/v17/Dictionary/                              # new recipewizard.*.config; 34 addvariant.*.config removed
└── KCC.Web.csproj                                     # changed: ReaderOutputSchema.json as an embedded resource
deploy/compose.yaml, deploy/.env.example               # changed: KCC_IMPORT_CEILING_USD
tools/KCC.ReaderEval/                                  # new console project in the slnx /tools/ folder, with an encoder like the browser's (R16)
tests/
├── KCC.UnitTests/Features/RecipeImport/               # reader, normalizer, cost, catalog, duplicates, photos, availability, gate, stand-in
├── KCC.UnitTests/Features/Providers/RecipeIconProviderTests.cs   # changed: cancellation and timeout
├── KCC.UnitTests/Tools/                               # new ReaderEvalScorerTests.cs, ReaderEvalEncoderTests.cs and ReaderEvalRunOrderTests.cs; KCC.UnitTests.csproj references tools/KCC.ReaderEval
├── KCC.IntegrationTests/Config/                       # UmbracoSite changed (fake reader, non-default ceiling, stand-in settings pinned); FakeRecipePhotoReader and TestImportSpend new; MemberClient multipart
├── KCC.IntegrationTests/Features/RecipeImport/        # ledger, endpoint, ceiling
├── KCC.IntegrationTests/Features/Pages/RecipeImportPagePropsTests.cs
├── KCC.IntegrationTests/Features/Baseline/DictionaryTests.cs     # changed: RecipeWizard boot test
├── KCC.IntegrationTests/Features/Api/SubmissionTests.cs          # changed: merged strings, the recipes-url attribute
├── KCC.E2ETests/BasePageTest.cs                       # changed: BasePageTests launches every browser with a fake camera
├── KCC.E2ETests/Config/SiteProcess.cs                 # changed: stand-in reader, its delay and the ceiling pinned, imports limit
├── KCC.E2ETests/KCC.E2ETests.csproj                   # changed: the fixture photos copied to the output
├── KCC.E2ETests/Fixtures/                             # fixture photos
├── KCC.E2ETests/Features/Members/                     # MemberFlowTests changed; RecipeImport, camera, leave, review and match tests new
├── KCC.E2ETests/Features/Backoffice/BackofficeTests.cs           # changed: start choice and data-testid locators
├── KCC.ViteTests/Features/Components/                 # RecipeWizard/ new; Forms and Wizard tests changed
├── KCC.ViteTests/Features/Utilities/Api.test.ts       # changed: status and code on every failure result; postForm
├── KCC.ViteTests/Features/Pages/                      # CreateRecipe/CreateRecipeView.test.ts and AddVariant/AddVariantView.test.ts changed; AddVariant/reviewSummary.test.ts deleted
└── KCC.ViteTests/Features/Styles/contrast.test.ts     # changed: any new text-on-surface pairing
README.md, CLAUDE.md, docs/hosting/runbook.md, .gitignore, KitchenCommandCenter.slnx   # changed
```

**Structure Decision**: The feature lives where the codebase already puts things:

- server code in a feature folder under `src/KCC.Web/Features`;
- shared Vue components under `Features/Components`;
- tests in the existing suites, on the one integration host that exists today. A scriptable fake reader on that host
  replaces any second test site (R15). The E2E suite's shared base class, `BasePageTests` in
  `tests/KCC.E2ETests/BasePageTest.cs`, gives every browser a fake camera, so the camera tests launch no browser of
  their own (R18).

The only new top-level folder is `tools/`, for the evaluation console. That console must stay out of the six test
suites and must never run in CI.

### Delivery order

The order follows the dependencies. Each step starts with its failing tests; `tasks.md` holds the detail.

1. **Client fix.** Add the Anthropic client timeout and retry, and let the icon provider pass cancellation through
   (R4).
2. **Spending.** Options, model profiles, cost, the ledger with its race test, availability, and the fake reader on
   the shared test host.
3. **Shared wizard.** The flow module, the wizard values, the key map, the extracted steps, the success sheet and
   their strings, for typed recipes; then the two E2E submission tests move to `data-testid` locators.
4. **Photo import (US1).** The reader, the normalizer, the endpoint, the stand-in reader, the page props, the photo UI,
   the photo strip beside the steps, the leave guard and the evaluation tool, for both flows. The two E2E submission
   tests choose the typing start once the start step shows.
5. **Ceiling (US2).** The unavailable state in the wizard, the 409 mapping, and the compose variable.
6. **Step links (US3).** Edit, Back to review, also on the photo step after Change photos (A21), Change photos, Read
   again, and the one warning that guards every read after an edit.
7. **Matching (US4).** The catalog, the duplicate check, the match step, the possible-duplicate notice, and a new read
   after a match choice, which returns the wizard to recipe mode (A20).
8. **Reader selection.** The owner supplies the photos and approves the spend, about US$18 for the first evaluation,
   the tool runs, and the default model is set.
9. **Docs and the definition of done.** Rows D1 to D10, then the CodeRabbit loop.

## Complexity Tracking

No violation to justify. Constitution 1.1.0 allows the hand-written uSync dictionary files, provided that a boot test
proves them.

## Red team

Red team round 1: 2026-10-07, 16 real flaws (F1 to F16), fixed in 9f7471f1.
Red team round 2: 2026-10-12, 16 real flaws (F17 to F32), fixed in d9dbbc04; F26 and F31 recorded as A18 and A19.
Red team: 2026-10-12, 32 real flaws found, 32 fixed.
