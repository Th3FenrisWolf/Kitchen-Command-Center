# Contract: The Details step, the review's Details section and their strings

## Page props

Both wizard pages gain one prop. Their Razor views pass it with `Vue.Prop`, and the page component hands it to
`RecipeWizard` as `choices`.

| Page | Prop | Source |
|---|---|---|
| `CreateRecipeView` | `details-choices` | `IRecipeQueries.GetTaxonomyChoices()`: categories and tags |
| `AddVariantView` | `details-choices` | The same, with `categories` empty |

```json
{
  "categories": [{ "key": "164e3680-…", "name": "Breakfast" }],
  "tags": [{ "key": "8a01e89e-…", "name": "Vegetarian", "kind": "Diet" }]
}
```

The names are the owner's content, so a category or tag added in the backoffice shows on the next page load with no
code change (FR-007).

## Components

| Component | File | Imported by |
|---|---|---|
| `DetailsStep` | `Features/Components/RecipeWizard/DetailsStep.vue` | `RecipeWizard.vue` only |
| `DetailsSummary` | `Features/Components/RecipeWizard/DetailsSummary.vue` | `ReviewStep.vue` only |
| details rules and mapping | `Features/Components/RecipeWizard/wizardDetails.ts` | `DetailsStep`, `DetailsSummary`, `wizardValues.ts`, `recipeWizardFlow.ts` |

`DetailsStep` follows the other steps: a `KccSheet` with `wash-key="wizard-details"`, `STEP_PAD`, the step's `tear`
and `flip`, a screen-reader `h2`, one `<form>` that emits `next`, and `StepNav`. It uses kit classes and existing form
components only, and adds no CSS:

| Part | Control | Shown |
|---|---|---|
| Category | `SelectField` with a None option | Mode `recipe` only (FR-004) |
| Tags | Two `<fieldset>` lists in `kcc-check` markup, as `RecipeFilters.vue` draws them: diet, then style | Always |
| Difficulty | The existing `SegmentedControl` (`Components/Recipe/SegmentedControl.vue`): None, Easy, Medium, Hard, with None as `''` mapped to null (plan D32) | Always |
| Nutrition | Eight `InputField`s, `inputmode="numeric"`, each with its unit and inline error | Always |
| Proposals | `InputField`, `maxlength="100"`: proposed category (mode `recipe` only), proposed tags | Always |

Each value with a mark shows `ValueMark` after its control and names it in `aria-describedby`, as Phase 1's steps do.
A tag's mark sits beside its row.

`DetailsSummary` is one review section with the kicker `ReviewDetails` and an Edit control
(`data-testid="review-edit-details"`) that emits `edit('details')` (FR-012). It lists the category (mode `recipe`),
the tags, the difficulty, each given nutrition value with its unit, the proposals, and every mark. With nothing given
it shows `NoDetailsGiven` (FR-011). The section never shows the Phase 1 missing-value warning, because every Details
field is optional (FR-005).

## Test hooks

| Hook | Element |
|---|---|
| `data-testid="details-step"` | The step's sheet |
| `data-testid="details-category"` | The category select |
| `data-testid="details-tag-<key>"` | Each tag checkbox |
| `data-testid="details-difficulty"` | The difficulty group; its radios by role and name |
| `data-testid="details-nutrition-<field>"` | Each nutrition input |
| `data-testid="details-proposed-category"`, `data-testid="details-proposed-tags"` | The proposal inputs |
| `data-testid="review-details"`, `data-testid="review-edit-details"` | The review section and its Edit control |
| `data-testid="details-changed"` | The well that explains a `detailsChanged` refusal |
| `data-testid="value-mark"` with `data-kind="suggested"` or `"calculated"` | As Phase 1's marks |

The existing E2E typed-submission tests gain one Next on the Details step and keep every Phase 1 hook.

## Strings

All new text is in the `RecipeWizard` dictionary group as hand-written uSync files
(`src/KCC.Web/uSync/v17/Dictionary/recipewizard.<key>.config`), added to `recipeWizardKeys.ts`, and proven by the
dictionary boot test and `recipeWizardStrings.test.ts` (FR-025). Category and tag names are content, not strings. The
keys sit in the `RecipeWizard` group even where the `VariantDetail` or `RecipeSearch` group holds a similar label, because
the pages load only `CreateRecipe` and `RecipeWizard` (plan D30).

| Key | English |
|---|---|
| `Details` | Details |
| `DetailsHint` | All optional. The owner can fill in anything you leave out. |
| `Category` | Category |
| `NoCategory` | No category |
| `DietTags` | Diet |
| `StyleTags` | Style |
| `Difficulty` | Difficulty |
| `DifficultyNone` | Not set |
| `DifficultyEasy` | Easy |
| `DifficultyMedium` | Medium |
| `DifficultyHard` | Hard |
| `NutritionPerServing` | Nutrition per serving |
| `Calories` | Calories |
| `Protein` | Protein |
| `Carbohydrates` | Carbohydrates |
| `Fat` | Fat |
| `SaturatedFat` | Saturated fat |
| `Fiber` | Fiber |
| `Sugar` | Sugar |
| `Sodium` | Sodium |
| `Kcal` | kcal |
| `Grams` | g |
| `Milligrams` | mg |
| `NutritionInvalid` | Enter a whole number from 0 to {0}. |
| `ProposedCategory` | Suggest a new category |
| `ProposedTags` | Suggest new tags |
| `ProposalHint` | The owner sees this when reviewing your recipe. It never shows on the site. |
| `ReviewDetails` | Details |
| `NoDetailsGiven` | No details given. |
| `DetailsChanged` | The site's categories or tags changed while you were working. Check your choices on the Details step and submit again. |
| `MarkSuggested` | Suggested by the reader |
| `MarkCalculated` | Calculated |
| `MarkReasonPrintedAs` | Printed as {0} |

The owner may reword any of them in the backoffice; the English is the baseline the uSync files carry.
