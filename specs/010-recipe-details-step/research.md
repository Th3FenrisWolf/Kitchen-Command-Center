# Research: Recipe Import from Photos, Phase 2 - The Details Step

Each item settles one question the plan could not settle in a line. Phase 1's research
([001 research.md](../001-photo-recipe-import/research.md)) still holds; items here only add to it.

## R1. Where the Details step's script goes, and what it costs every page

**Decision**: The step is two new modules beside the other steps, `DetailsStep.vue` and `wizardDetails.ts`, and the
review's Details section is a third, `DetailsSummary.vue`. `RecipeWizard.vue` and `ReviewStep.vue` import them
statically, as they import every other step. The nutrition conversion, the taxonomy check and the mark codes live on
the server, so the browser carries only the step's form, its validation and the value mapping.

**Rationale**:

- `Features/GlobalComponents.ts` registers every `*.Component.vue` through an eager `import.meta.glob`, so
  `CreateRecipeView.Component.vue` and `AddVariantView.Component.vue` pull `RecipeWizard.vue` and all its steps into the
  single `main` chunk that `Features/Main.ts` builds. No component in `src/KCC.Web/Features` uses
  `defineAsyncComponent` or a dynamic `import()` today. Keeping the step out of that chunk would need the first
  dynamic import in the site, which is a change to the bundle split, and the bundle split is 009's work.
- The E2E perf guard measures `js_gz_bytes` per page (`tests/KCC.E2ETests/Config/Perf/Collectors.cs`, gzip at
  `SmallestSize`) against `baseline.ci.json`: 117,266 bytes on every page, with an allowance of 2% or 1,024 bytes,
  whichever is larger, so 2,345 bytes (`budgets.json`, `ResultsWriter.Allowance`). The `main` chunk in this
  worktree's last build (`wwwroot/assets/main-BgBemA9G.js`, built 2026-11-03, not rebuilt from this branch) gzips to
  117,286 bytes at `gzip -9`, so about 2,300 bytes of the allowance remain. This number is from a stale build and
  was not re-measured.
- The step's template is data-driven: the eight nutrition fields, the two tag groups and the three difficulty levels
  are rendered from arrays, not written out, which keeps the compiled render function small. The target is under
  2,000 bytes gzipped for the whole change.

**Alternatives considered**: `defineAsyncComponent(() => import('./DetailsStep.vue'))` now. It keeps the step out of
the shared chunk, but it adds a chunk and a fetch path that 009 is about to design for every later step, and the brief
for this feature rules out changing the split. Moving the conversion to the browser was rejected for size and for
accuracy (R3).

## R2. How the reader names categories and tags

**Decision**: The read sends the site's lists as short ids, `C1` to `Cn` for categories and `T1` to `Tn` for tags with
their group, in the same line format as Phase 1's catalog (`RecipeCatalog`, `R1` to `R500`). A new
`ReaderTaxonomy` class builds the text and maps an id back to a key. An id the lists do not hold is dropped.

**Rationale**: Short ids cost fewer tokens than GUIDs, cannot be mistaken for a name the model made up, and give the
server one place to refuse anything outside the lists (FR-013, FR-019). The text is a few hundred characters for 6
categories and 11 tags (spec A10), and `ReadCost.ReservationMicroUsd` counts it with the catalog's characters.

**Alternatives considered**: Names. A category renamed between the page load and the read would then fail to map, and
a near-miss name ("Gluten Free") would need fuzzy matching.

## R3. Who converts nutrition

**Decision**: The reader copies what is printed and the server converts. For each nutrient the photos print, the model
returns the amount as printed (a range's lower number; `<1` as 0), its unit as printed, the printed text, whether it
was a range or a "less than", and once for the panel whether it is per serving or per whole recipe.
`ReadResultNormalizer` then:

1. picks sodium as printed, else salt divided by 2.5 (spec A3);
2. converts kJ to kcal at 4.184 kJ per kcal, and sodium in grams to milligrams;
3. divides a whole-recipe panel by the servings, only when the read's servings value is present and its mark is not
   `estimate` (FR-018); otherwise every nutrition value stays empty. A panel whose basis is null leaves every
   nutrition value empty and unmarked;
4. rounds half up (`MidpointRounding.AwayFromZero` on non-negative values, spec A3);
5. marks the value: none when the printed per-serving number was already whole and needed no conversion; `calculated`
   with code `printedAs` and the printed text as its argument when it was converted or rounded; `unclear` with the
   model's reason for a range or a "less than"; `unclear` with Phase 1's `outOfRange` code, and an empty value, when
   the result passes its limit (spec A2).

Nutrients the site does not hold (cholesterol, potassium, vitamins) are not in the schema's enum, so the model cannot
return them.

**Rationale**: SC-005 asks for 95% of printed values exactly right per serving. Division, unit conversion and rounding
are exact in C# and unit-testable case by case, and a model doing arithmetic is neither. The model's job shrinks to
reading the panel, which is the part only it can do.

**Alternatives considered**: The model returns final per-serving values. Rejected: untestable without a paid read, and
SC-006 (no invented value) is easier to hold when the server can see that a value was printed.

## R4. Which rules the server enforces after the model

**Decision**: The normalizer enforces every hard rule of FR-013 to FR-019, whatever the model returns:

| Rule | Enforcement |
|---|---|
| Category only on Create Recipe (FR-013, FR-004) | Dropped when the read's flow is `variant` |
| Values from the current lists only (FR-013) | An unknown id is dropped |
| Diet tag only when stated (FR-014, SC-006) | A diet tag with `stated: false` is dropped. A stated one keeps the model's `unclear` mark, such as a vegetarian tag with chicken stock |
| Suggested mark (FR-015) | A category, a style tag or a difficulty with `stated: false` gets kind `suggested` and the model's reason. A stated category or tag whose `printed` word differs from the list name, ignoring case, keeps its value and gets `suggested` with code `printedAs` and the word. A diet tag named by an equivalent word ("plant-based" for Vegan) still fills (FR-014) and carries that mark |
| Difficulty printed (spec edge case) | A printed word equal to Easy, Medium or Hard, ignoring case, carries no mark. Any other printed word gets `suggested` with code `printedAs` and the word |
| No proposal (FR-019) | The reader output has no proposal field |
| No estimated nutrition (FR-018) | Only values with printed text are kept; the whole-recipe rule in R3 |

**Rationale**: SC-006 is a zero bar. A prompt alone cannot hold a zero bar; a filter can.

## R5. How the draft stores the new values

**Decision**: `SubmissionValues` writes, through `IContentEditingService` as today:

- `category` and `tags`: the multi-node tree picker's `JsonArray` of `{ type: "document", unique }` references, as
  `RecipeTestDataSeeder.DocumentReferences` already writes them.
- `difficulty`: the flexible dropdown's stored shape, an array of one string such as `["Medium"]`, as
  `TaxonomyDefaults` stores a tag's `kind`. Not verified through `IContentEditingService`: the first failing
  integration test pins it by reading `RecipeVariant.Difficulty` back as `Medium` after publishing.
- the eight nutrition values: written when the member gave one, `0` included, and left out when empty. Phase 1's
  `AddNumber` drops `0`, which is right for times and wrong here, because `RecipeQueries.Optional` already shows a
  stored `0` as a real zero.
- `proposedCategory` on the recipe and `proposedTags` on the variant: trimmed text, left out when blank.

**Rationale**: Each shape is the one the backoffice editor and the published model already read, so the owner's
backoffice shows the values with no editor change.

## R6. How the proposals stay off the public site

**Decision**: Two new properties on the existing types, `proposedCategory` (recipe) and `proposedTags` (recipe
variant), on the existing `Textstring` data type (`0cc0eba1-9960-42c9-bf9b-60e150b429ae`). The API refuses more than
100 characters (FR-008), and the step's inputs carry `maxlength="100"`.

**Rationale**: The public site reads recipes only through `RecipeQueries`, which maps named properties into
`RecipeRecord` and `VariantRecord`; the first-party search index builds from those records
(`Search/RecipeIndexBuilder.cs`). Neither will name the new properties. The Delivery API is not enabled, and Umbraco's
own Examine indexes are not queried by any public route. SC-003's test publishes a draft holding a proposal and
checks the recipe page, the variant page and the search API.

**Alternatives considered**: A new data type with `maxChars: 100`. It adds a uSync file and a boot-test case for a
limit the API already holds.

## R7. The reader comparison for SC-004 to SC-006

**Decision**: `tools/KCC.ReaderEval` gains a `details` block in `expected.json`, Details scoring, three report columns
and a stricter pre-spend gate. It sends the site's lists as `TaxonomyDefaults.Values` holds them. Exactly one paid
comparison runs, over the four models Phase 1 compared. No paid draft is planned.

- **Scoring.** SC-004: the read's category name equals the expected one, over the cases that expect a category.
  SC-005: each expected non-null nutrition value equals the read's value, over all of them. SC-006: the count of
  nutrition values filled where the expected value is null, plus the count of diet tags filled that the expected
  answer does not list as stated. It must be 0.
- **Gate.** `--models` refuses to start, before any spend, unless every case has a `details` block, at least 3 cases
  expect a nutrition value, at least one of them is marked as printed per whole recipe or in kilojoules (spec A8), and
  at least 10 cases expect a category, so that "9 of 10" is measurable, and no block carries `"draft": true` (plan D23,
  D25).
- **Cost.** Phase 1's four-model comparison over 10 cases spent about US$1.03 against an estimate of up to US$5.74
  (001 research, Measured results). With 13 or more cases and the taxonomy text, one run is expected to spend about
  US$1 to US$2, which is the spec's figure. The tool states its estimate and asks before spending, as today.
- **Expected values.** The session drafts the `details` blocks of the 10 existing cases by hand, each with
  `"draft": true`. The owner adds at least 3 recipes that print nutrition and confirms every block, deleting its
  `"draft": true` (spec A8). `--draft` drafts only cases with no `expected.json`, at about US$0.06 a case
  on Opus 5.5, and stays available, but this plan does not schedule it.
- **Free check first.** `--check` makes `count_tokens` calls only, and shows that the service accepts the grown schema
  before the paid run.

**Rationale**: One run measures every model against every bar, which is all A9 needs. The stand-in and fake readers
cover every behaviour in the six suites at no cost.

**Alternatives considered**: A taxonomy file in the set. The site's lists equal `TaxonomyDefaults.Values` today
(spec, "What the site holds today"), so a file would add a person task for no difference. Comparing only the default
model: A9 picks the cheapest reader that passes, so the cheaper ones must be measured too.

## R8. The stand-in reader's Details

**Decision**: `StandInRecipePhotoReader` gains Details in its fixed output and two more photo counts, so the E2E suite
can reach every review state with no paid read:

| Photos | Details in the output |
|---|---|
| 1, or any count not listed | Category Breakfast not stated, with a reason; Vegan stated; Easy style tag not stated; difficulty printed "easy"; a per-serving panel with whole numbers and one fraction (fiber 1.5 g) |
| 2 | As now (proposes `R1`), with the Details of 1 |
| 3 | As now (copies a variant), with the Details of 1 |
| 4 | A whole-recipe panel in kJ with salt, and 8 printed servings |
| 5 | A whole-recipe panel with servings marked `estimate`, so nutrition stays empty |

It maps names to the request's short ids through `ReaderTaxonomy`, and drops a name the lists no longer hold.

## Measured results

The session drafted the `details` block of the 10 Phase 1 cases from their photos (T072). Each block carries
`"draft": true` until the owner confirms it.

- **Print nutrition**: 3 cases, all `perServing`: `printed-ham-and-eggs-bake`, `printed-hash-brown-bacon-egg-skillet`
  and `printed-turkey-enchiladas`. Each prints calories, protein, fat, total carbs and fiber, already whole, so no
  conversion applies. None prints saturated fat, sugar or sodium. No case is `converted`.
- **Expect a category**: 9 cases. Dinner: `handwritten-hawaiian-chicken-casserole`, `printed-carbonara`,
  `printed-creole-shrimp`, `printed-turkey-enchiladas`. Breakfast: `handwritten-mushrooms-and-eggs`,
  `printed-ham-and-eggs-bake`, `printed-hash-brown-bacon-egg-skillet`. Snack: `handwritten-spinach-artichoke-tarts`,
  `printed-banana-bread-card`. `handwritten-rolls` expects none: no list category fits a bread roll.
- **Difficulty**: no case prints one.
- **What the R7 gate still needs from the owner**:
  - nutrition: met at 3 cases, but none per whole recipe or in kJ, so at least one new case that prints a
    whole-recipe or kJ panel (spec A8);
  - category: 9 of 10, so at least one more case that expects a category, from the new cases or by giving
    `handwritten-rolls` one;
  - every block confirmed, with no `"draft": true` left, the new cases' blocks included.

The gzipped shared `main` chunk was measured on this machine after `pnpm build:all` in each checkout, as quickstart.md
sets out (T078):

| Checkout | Commit | `main` chunk, gzip -9 |
|---|---|---|
| `origin/main` | `c1827268` | 118,231 bytes |
| This branch | `14d31326` | 121,749 bytes |
| Delta | | +3,518 bytes (+2.98%) |

The delta misses the 2,000-byte target and the E2E guard's 2,345-byte allowance: the guard reads 122,101 bytes
against its 117,266-byte baseline. The owner accepted the breach: the pull request merges after
009-script-hydration-cost. Both branches touch `RecipeWizard.vue`. This branch changes no script loading, bundle split,
budget or baseline, and 009 moves the Details step with the other later steps by changing its two static imports, the
one in `RecipeWizard.vue` (`laterStep('DetailsStep')`) and the one of `DetailsSummary` in `ReviewStep.vue`.
