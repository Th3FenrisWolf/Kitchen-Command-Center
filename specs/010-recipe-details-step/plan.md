# Implementation Plan: Recipe Import from Photos, Phase 2 - The Details Step

**Branch**: `010-recipe-details-step` | **Date**: 2026-11-17 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/010-recipe-details-step/spec.md`

## Summary

Create Recipe and Add Variant gain a Details step between the instructions and the review: category (Create Recipe
only), tags in their diet and style groups, difficulty, the eight nutrition values per serving, and two proposal notes.
Every field is optional.

- **The step** is built on Phase 1's shared wizard: one more step component, one more review section, and new values
  and marks in the flow module. It uses kit classes and existing form components only.
- **The lists** come from the published categories and tags through a new page prop, so the owner's backoffice
  changes show with no code change. A submission that names a category or tag the site no longer has is refused with
  the current lists, and the member lands back on Details with their other values kept.
- **The draft** stores the values in the recipe's and variant's existing properties. Two new text properties hold the
  proposals, which only the backoffice shows.
- **The read** sends the lists as short ids. The model picks from them and copies the printed nutrition panel; the
  server enforces the diet-tag and list rules, converts nutrition to per-serving values in the site's units, and sets
  the new `suggested` and `calculated` marks.
- **The evaluation tool** scores SC-004 to SC-006, and one paid comparison run settles the default reader.

## Technical Context

**Language/Version**: C# on .NET 10 (`global.json`); TypeScript and Vue 3 on the Node line the `Dockerfile` names

**Primary Dependencies**: Umbraco CMS 17 LTS, the Anthropic C# SDK already referenced, Vue 3 with SSR, Vite, Tailwind
CSS 4 and the Loose Leaf kit. No new package.

**Storage**: The site's SQLite file through Umbraco. Two new properties on existing content types (spec A5); no
table, migration or key-value change.

**Testing**: TUnit with Moq (unit, integration on the shared fresh-SQLite host, E2E with Playwright) and Vitest. Every
test uses the stand-in or the scriptable fake reader; no test host sets an API key.

**Target Platform**: The linux/arm64 container on the Raspberry Pi 5 behind Cloudflare; iOS Safari 17+, Android
Chrome and current desktop browsers.

**Project Type**: Web application: an Umbraco site with a Vue SSR front end. No backoffice extension changes.

**Performance Goals**: SC-001, under 30 seconds on a phone for a category, three tags and a difficulty. The read stays
within Phase 1's 60-second bar; the Details add a few hundred tokens each way.

**Constraints**:

- The shared script grows by under 2,000 bytes gzipped, inside the E2E guard's 2,345-byte allowance (research R1).
- No change to script loading, the bundle split or any CI budget or baseline (sibling feature 009 owns them).
- The read ceiling stays US$2 a month per site (SC-007). The reservation counts the lists' characters.
- Paid reads: one comparison run (about US$1 to US$2), and none in routine testing.

**Scale/Scope**: 6 categories and 11 tags today; 33 new `RecipeWizard` strings; one new step, one new review section.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design: no violation.*

Checked against constitution 2.1.0.

| Principle | How the plan meets it |
|---|---|
| I. Content Lives in the Backoffice | Every new string is a `RecipeWizard` dictionary item ([details-ui.md](contracts/details-ui.md#strings)), the difficulty labels included. Category and tag names come from content (FR-007). The two new properties travel as uSync with their regenerated ModelsBuilder models in one commit |
| II. Loose Leaf Is the Only Look | `SelectField`, `kcc-check`, `SegmentedControl`, `InputField`, `KccSheet` and `ValueMark` only; no new class, no CSS file, no kit change. `kit-builder` builds the step, `brand-steward` reviews it, and both ramps are checked (D6, D7). The new marks reuse `kcc-badge`, so `contrast.test.ts` gains no pairing |
| III. SQLite Has One Writer | Create Recipe writes the Details inside its existing scope, after `scope.WriteLock(Constants.Locks.ContentTree)`; Add Variant's single-node save through `IContentEditingService` is unchanged and opens no scope; neither path gains a read before its write. The list check reads the published cache before the scope opens. The ledger's lock is unchanged; only the reservation's size grows. The whole integration suite runs |
| IV. Tests First, Builds Clean | Every behaviour starts with a failing test (`tasks.md`). Phase 1's hooks stay; new tests locate by `data-testid` and role. The uSync files are hand-written as the repository's own files show, and the schema and dictionary boot tests prove them. The models come from Generate models, not by hand |
| V. Cheap to Run, Cheap to Upgrade | The spec names the cost. The ceiling is unchanged; a read sends a few hundred more characters. One paid comparison run, about US$1 to US$2. No new package. The endpoints stay on `ControllerBase` |
| VI. Members Earn Trust | Both submission endpoints keep the anti-forgery check and the `submissions` rate limit; the read keeps `imports`. Drafts stay unpublished. The proposals never reach a public page or search (SC-003). The reader is sent the lists and no account data (FR-020) |
| VII. Code Explains Itself | No narrating comments. `README.md`, `CLAUDE.md` and the runbook are checked for lines this change makes untrue (D10) |

**Gates for card implement**: gates 1 and 2 are the environment. Gate 3 is this table. Gate 4 holds: no open
clarification marker and no open checkpoint row; Q1 was answered on 2026-11-17. Gate 5 holds: Phase 1 (001) passed its definition of done and is merged into `main`.

**Definition-of-done rows that apply**: D1 to D7 and D10. D5 applies for the uSync schema and dictionary files and the
regenerated models. D8 and D9 do not apply: nothing under `deploy/`, no `Dockerfile` change.

## Architecture

```text
page load   CreateRecipePageController / AddVariantPageController
              └─ IRecipeQueries.GetTaxonomyChoices() → details-choices prop → RecipeWizard.choices

typed       basics → variant → ingredients → instructions → DETAILS → review → submit
import      start → photos → reading → (match) → review ⇄ DETAILS (Edit on the Details section)

read        RecipeImportApiController
              ├─ ReaderTaxonomy (C1…, T1…) → ReaderPrompt instruction; ReadCost reservation counts it
              ├─ reader → ReaderOutput.details (ids, stated, printed panel)
              └─ ReadResultNormalizer: list and diet rules, nutrition conversion, suggested/calculated marks
                   → ReadResponse.details → valuesFromRead

submit      RecipeApiController
              ├─ names (Phase 1) → DetailsRules (limits) → member → list check against published content
              │     └─ 400 detailsChanged { choices } → refuseDetails → DETAILS
              └─ RecipeSubmissions (one scope, ContentTree lock) → SubmissionValues (category, tags, difficulty,
                    nutrition, proposals)
```

## Where the Details step loads, and how 009 moves it

**Today.** `DetailsStep.vue` and `DetailsSummary.vue` sit in `Features/Components/RecipeWizard/`. `RecipeWizard.vue`
imports `DetailsStep` statically and renders it in one `v-else-if` branch; `ReviewStep.vue` imports `DetailsSummary`
the same way. Both wizard pages reach `RecipeWizard.vue` from a `*.Component.vue` file, which `GlobalComponents.ts`
registers through its eager `import.meta.glob`, so the step ships in the single `main` chunk that `Features/Main.ts`
builds, on every page. The current structure has no lazy path, so the step cannot stay out of that chunk without the
site's first dynamic import, which this branch does not add (research R1). What can stay out of the script does: the
nutrition conversion, the list rules and the mark codes run on the server, and the lists and strings arrive as page
data.

**When 009 splits the wizard.** The step never renders at first paint: the wizard opens on its start step or its first
typing step. Each component is imported in exactly one place, so 009 turns those two imports into
`defineAsyncComponent(() => import('./DetailsStep.vue'))` and the same for `DetailsSummary.vue`, with the other later
steps, and nothing else changes. `wizardDetails.ts` holds plain functions that `recipeWizardFlow.ts` and `wizardValues.ts`
also use, so it stays in the wizard's eager code, at a few hundred bytes.

**Overlap.** Both branches edit `RecipeWizard.vue`, and 009 moves the `js_gz_bytes` guard. Whichever merges second
rebases: if 009 is first, the Details step joins its split of the later steps; if 010 is first, 009 measures from a
`main` that already holds the step.

## Approach

| ID | Decision | Choice | Rejected | Why |
|---|---|---|---|---|
| D1 | How the step is built and loaded | `DetailsStep.vue`, `DetailsSummary.vue` and `wizardDetails.ts` on Phase 1's step pattern, imported statically in one place each | `defineAsyncComponent` now | No lazy path exists, and the brief leaves the bundle split to 009 (research R1) |
| D2 | If the change breaks the shared-script guard | Close-out stops and reports the measured delta; no budget, baseline or split change in this branch | Re-baseline `baseline.ci.json`; lazy-load the step here | Both are 009's ground. Target under 2,000 bytes against a 2,345-byte allowance |
| D3 | Keeping the step's script small | Nutrition fields, tag groups and difficulty levels rendered from arrays; conversion and list rules on the server | One hand-written field per value; client-side conversion | Smaller render function; exact server arithmetic (research R1, R3) |
| D4 | How the reader names list values | Short ids `C1…`, `T1…` with group, in a new `ReaderTaxonomy` | GUIDs; names | Fewer tokens; one place that refuses values outside the lists (research R2) |
| D5 | Who converts nutrition | The reader copies amount, unit, printed text, `inexact` and the panel's basis; `ReadResultNormalizer` converts, divides, rounds and marks | The model returns final per-serving values | SC-005's exactness and SC-006's zero bar need testable arithmetic (research R3) |
| D6 | Holding FR-013 to FR-019 | The normalizer drops unknown ids, unstated diet tags and a category on the variant flow, and sets every `suggested` and `calculated` mark | Prompt rules alone | A zero bar needs a filter (research R4) |
| D7 | Mark shape for the new kinds | Kinds `suggested` and `calculated`; server code `printedAs` with the printed text as `arg`; the model's reasons as in Phase 1 | English reasons from the server | Phase 1's rule: server-set marks carry codes, and the owner can reword `MarkReasonPrintedAs` |
| D8 | Mark precedence | `unclear`, `estimate`, `suggested`, `calculated`, `written`, `translated` | Calculated above suggested | A value the member should doubt shows the strongest warning |
| D9 | Lists for the page | New `IRecipeQueries.GetTaxonomyChoices()` feeding a `details-choices` prop on both pages | A new lists endpoint fetched by the step | SSR already has the data; no new endpoint and no extra fetch |
| D10 | Submission shape | Keys for category and tags, `"Easy"`/`"Medium"`/`"Hard"`, a `nutrition` object of nullable integers, proposal strings, all optional fields on the existing request classes | Names | Keys survive a rename; an old body still submits ([submission.md](contracts/submission.md)) |
| D11 | Refusing a changed list | 400, code `detailsChanged`, body carries the current `choices` | 409; a refetch from the client | Matches Phase 1's `nameTooLong` refusal and needs one round trip |
| D12 | Where the limits live | `DetailsRules.cs` on the server and `wizardDetails.ts` in the browser, each with its own tests on the same table | A shared generated file | Two small tables; a generator costs more than it saves |
| D13 | Nutrition inputs | `InputField` with `inputmode="numeric"`, the value held as typed text, an inline error per invalid value, Next disabled while any is invalid | `NumberStepper`; `type="number"` | A stepper is unusable up to 50,000; `type="number"` hides invalid text, so the step could not explain it (FR-006) |
| D14 | Step controls | Category `SelectField` with None; tags as two `kcc-check` fieldsets; difficulty as a `SegmentedControl` radio group with None (D32) | A new chip pattern | Existing kit parts only; no CSS or kit change |
| D15 | Proposal properties | `proposedCategory` and `proposedTags` on the existing `Textstring` data type; the API holds the 100-character limit | A new data type with `maxChars` | One fewer uSync file for a limit the API already holds (research R6) |
| D16 | Storing nutrition | Each given value written, `0` included; empty values left out | Phase 1's `AddNumber`, which drops `0` | `0 g` fiber is a real value and `RecipeQueries` already shows it (research R5) |
| D17 | Storing difficulty | The dropdown's stored array, `["Medium"]`, pinned by the first integration test reading `Difficulty` back | A plain string | The shape `TaxonomyDefaults` uses for the same editor; unverified through `IContentEditingService` until that test passes (research R5) |
| D18 | Proposals and a new read | Proposals survive a read, and editing one leaves `editedSinceRead` and marks alone | Treat them as read values | Spec edge case: they are the member's own |
| D19 | Category when a match is accepted | Joins `setAside` with its mark, and comes back on keep new recipe | Cleared for good | Spec edge case on switching |
| D20 | Rounding and ranges | Half up on non-negative values; a range or `<n` keeps the floor of its lower bound, marked unclear | Banker's rounding | Spec A3 and the ranges edge case |
| D21 | Stand-in reader | Fixed Details in its output, plus 4 photos (whole-recipe kJ and salt, 8 servings) and 5 photos (servings estimated) | A new configuration switch | Photo counts already pick its behaviours (research R8) |
| D22 | Taxonomy in the evaluation | The tool sends `TaxonomyDefaults.Values` | A `taxonomy.json` in the set | The site's lists equal the defaults today; no extra person task (research R7) |
| D23 | Paid runs | One comparison over Phase 1's four models; no paid draft scheduled; the session drafts the 10 existing cases' `details` blocks by hand and the owner confirms them; `--check` (free) first; no paid run before the owner's explicit yes | A draft run for the new cases | The cost brief: only what SC-004 to SC-006 need (research R7) |
| D24 | The comparison's pre-spend gate | Refuses unless every case has `details`, 3 or more expect nutrition with one per whole recipe or in kJ, and 10 or more expect a category | Phase 1's gate only | A run that cannot measure the bars wastes its spend |
| D25 | Marking a session-drafted `details` block | `"draft": true` inside the block; the owner deletes it on confirming, and D24's gate also refuses while any block holds it | A separate `expected.details.draft.json` per case | One file per case to confirm, and the gate cannot spend on an unconfirmed answer key |
| D26 | A category or tag named on the photos by an equivalent word | The reader's category and tag entries gain `printed` (the word on the photos); the normalizer keeps a stated value and marks it `suggested` with code `printedAs` and the word when `printed` differs from the list name, ignoring case | Equivalents unmarked; drop "obvious equivalent" from rule 10 | FR-015 marks any value not stated by that name, and FR-014 still lets an equivalent fill a diet tag; this mirrors the printed-difficulty rule |
| D27 | A nutrition panel whose basis is null | Every nutrition value empty and unmarked, as for a whole-recipe panel with no printed servings | Copy as per serving, marked unclear | FR-018 and SC-006's zero bar favour an empty value over a guessed basis |
| D28 | Proposed category when a match is accepted | `proposedCategory` joins `setAside` with the category and returns on keep a new recipe; a variant submission never sends it | Dropped for good | FR-004 hides it on the variant flow; D18 keeps proposals as the member's own |
| D29 | Photo strip on the Details step | `details` joins `PHOTO_STRIP_STEPS` | No strip on Details | The member checks printed nutrition against the photos there, as on every other section step |
| D30 | Labels the VariantDetail and RecipeSearch groups already hold | New keys in the `RecipeWizard` group | Concatenate `VariantDetail` into the wizard pages' strings | The pages load only `CreateRecipe` and `RecipeWizard` (CreateRecipePageController.cs:42-43); a whole second group ships more than it saves, and the owner can word the form apart from the page |
| D31 | Widening `ReadRequest` and `Normalize` | New parameters optional (taxonomy null means no lists, flow defaults to recipe) | Update every call site in T047 and T054 | KCC.UnitTests references KCC.ReaderEval, so each checkpoint stays green until Phase 11 wires the taxonomy |
| D32 | Difficulty control | The existing `SegmentedControl` with None as `''` mapped to null | Hand-written `kcc-seg` markup | Keyboard moves, roles and the thumb exist already, and it adds no script size |

## Reader comparison and cost

- Routine testing spends nothing: the six suites use the stand-in and fake readers.
- One paid run proves SC-004 to SC-006: `--models` over `claude-haiku-5-5`, `claude-haiku-4-5`, `claude-sonnet-5-5`
  and `claude-opus-5-5`, about US$1 to US$2, after the free `--check` and the 24-hour wait
  ([quickstart.md](quickstart.md#the-paid-reader-comparison-once)). The tool states its estimate and asks first.
- The test set gets its new expected values in two parts. The implementing session drafts the `details` block of each
  of the 10 existing cases from their photos, marked as a draft. The owner adds at least 3 recipes that print
  nutrition, one per whole recipe or in kJ, and confirms every `details` block (spec A8). The owner's part is a person
  task, and the tasks that run the comparison wait for it.
- The default reader follows spec A9 as answered on 2026-11-17: `claude-opus-5-5` unless it fails a Phase 2 bar.
- No paid run starts until the owner's explicit yes reaches this session.
- The spec's "up to about US$6" for a drafted answer key is not planned; `--draft` stays available at about US$0.06 a
  new case if the owner wants it.

## Project Structure

### Documentation (this feature)

```text
specs/010-recipe-details-step/
├── spec.md
├── plan.md                       # this file
├── research.md                   # R1 to R8
├── data-model.md
├── quickstart.md                 # stand-in states, size measurement, the one paid run
├── contracts/
│   ├── submission.md             # POST /api/recipes and /variants with Details
│   ├── read-response.md          # prompt lines, reader schema additions, response details
│   └── details-ui.md             # page prop, components, test hooks, strings
├── checklists/requirements.md
└── tasks.md                      # /speckit-tasks
```

### Source Code (repository root)

```text
src/KCC.Web/
├── Features/
│   ├── Components/RecipeWizard/
│   │   ├── DetailsStep.vue                    # new
│   │   ├── DetailsSummary.vue                 # new: the review's Details section
│   │   ├── wizardDetails.ts                   # new: fields, limits, validation, request mapping
│   │   ├── RecipeWizard.vue                   # changed: choices prop, the details branch, refuseDetails
│   │   ├── ReviewStep.vue                     # changed: DetailsSummary
│   │   ├── recipeWizardFlow.ts                # changed: details step, section, setAside, edits, applyRead
│   │   ├── wizardValues.ts                    # changed: values, marks, valuesFromRead, requests
│   │   ├── recipeImportApi.ts                 # changed: ReadResponse.details
│   │   ├── ValueMark.vue                      # changed: suggested, calculated, printedAs
│   │   ├── wizardErrors.ts                    # changed: detailsChanged
│   │   └── recipeWizardKeys.ts                # changed: 33 keys
│   ├── Pages/CreateRecipe/, Pages/AddVariant/ # changed: details-choices prop (controller, view model, cshtml, view)
│   ├── Recipes/RecipeQueries.cs, RecipeRecord.cs   # changed: GetTaxonomyChoices, DetailsChoices
│   ├── Models/Api/CreateRecipeRequest.cs, CreateVariantRequest.cs   # changed: Details fields
│   ├── Api/RecipeApiController.cs             # changed: Details rules, list check, detailsChanged
│   ├── Submissions/DetailsRules.cs            # new
│   ├── Submissions/SubmissionValues.cs, RecipeSubmissions.cs   # changed: Details values
│   ├── RecipeImport/
│   │   ├── ReaderTaxonomy.cs                  # new
│   │   ├── NutritionConversion.cs             # new: research R3
│   │   ├── ReadResponse.cs                    # changed: details, mark kinds and code
│   │   ├── ReadResultNormalizer.cs            # changed: Details rules
│   │   ├── ReaderPrompt.cs, ReaderOutputSchema.json   # changed
│   │   ├── ReadCost.cs, RecipeImportApiController.cs  # changed: taxonomy characters, ReaderTaxonomy
│   │   └── AnthropicRecipePhotoReader.cs      # changed only if the request shape needs it
│   ├── DevTools/RecipeImport/StandInRecipePhotoReader.cs   # changed: research R8
│   └── Models/Generated/Recipe.generated.cs, RecipeVariant.generated.cs   # regenerated
└── uSync/v17/
    ├── ContentTypes/recipe.config, recipevariant.config   # changed: the two proposal properties
    └── Dictionary/recipewizard.*.config                   # 33 new
tools/KCC.ReaderEval/                          # changed: ExpectedAnswer.details, CaseScorer, Report, EvalSet gate, taxonomy
tests/
├── KCC.UnitTests/Features/RecipeImport/       # normalizer Details, NutritionConversion, ReaderTaxonomy, ReadCost, stand-in
├── KCC.UnitTests/Features/Submissions/        # SubmissionValues, DetailsRules
├── KCC.UnitTests/Tools/                       # scorer and gate for SC-004 to SC-006
├── KCC.IntegrationTests/Features/Api/SubmissionTests.cs   # draft holds the Details; detailsChanged; proposals stay private
├── KCC.IntegrationTests/Features/RecipeImport/RecipeImportApiTests.cs   # lists sent, details returned
├── KCC.IntegrationTests/Features/Pages/       # details-choices prop on both pages
├── KCC.IntegrationTests/Features/Baseline/    # SchemaTests keys; dictionary boot test
├── KCC.E2ETests/Features/Members/             # typed Details, import Details, Add Variant, refusal; existing typed tests pass Details
└── KCC.ViteTests/Features/Components/RecipeWizard/   # DetailsStep, DetailsSummary, wizardDetails, flow, values, ValueMark, strings
```

**Structure Decision**: Everything lives where Phase 1 put its parts: the step with the other steps, the server rules
in `Submissions/` and `RecipeImport/`, the tests in the existing suites on the one shared integration host.

## Build order

Each step starts with its failing tests; `tasks.md` holds the detail.

1. **Schema and strings.** The two proposal properties in uSync, Generate models, `SchemaTests` keys; the 33
   dictionary items with their boot test.
2. **Lists.** `GetTaxonomyChoices` and the `details-choices` prop on both pages.
3. **Submission (US1 server, US3).** Request fields, `DetailsRules`, `SubmissionValues` (D16, D17), the list check and
   `detailsChanged`; the proposals-stay-private test (SC-003). Whole integration suite.
4. **The step (US1 client).** `wizardDetails.ts`, the flow and values changes, `DetailsStep`, `DetailsSummary`, the
   refusal path; then the E2E typed tests. Measure the script size (D2).
5. **The read (US2).** `ReaderTaxonomy`, the reservation, prompt and schema, `NutritionConversion`, the normalizer
   rules, `ValueMark`'s new kinds, `valuesFromRead`, setAside; the stand-in reader and the E2E import tests.
6. **Evaluation tool.** Expected `details`, scoring, report columns, the gate; `--check` (free).
7. **Reader comparison** (waits for the owner's set additions, confirmations and approval). The one paid run, the measured table, the
   default model.
8. **Docs and the definition of done.** D1 to D7 and D10; SC-001 timed on the phone preset; the pull request.

## Pull request

The description carries the gzipped size of the shared `main` chunk on `origin/main` and on this branch, measured as
[quickstart.md](quickstart.md#script-size-for-the-pull-request) sets out, with the delta. It also carries a short note
on 009: both branches touch `RecipeWizard.vue`, this branch changes no script loading, bundle split, budget or
baseline, and 009 moves the Details step with the other later steps by changing its two static imports.

## Person tasks

- **The owner** adds at least 3 recipes that print nutrition to the evaluation set, one per whole recipe or in kJ,
  and confirms the `details` block of every case, including the 10 the implementing session drafts (spec A8).
- **The owner** checks that the Anthropic organization spend limit leaves about US$2 for the month of the comparison,
  and approves the spend when the tool asks.
- **Generate models** needs a signed-in Development backoffice (README, "Schema and baseline content"). If the
  implementing session cannot sign in, the owner clicks Settings → Models Builder → Generate models.

## Open with the owner

None. Q1 was answered on 2026-11-17 (spec A9).

## Complexity Tracking

No violation to justify.
