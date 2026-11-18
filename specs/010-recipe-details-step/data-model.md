# Data Model: Recipe Import from Photos, Phase 2 - The Details Step

Phase 1's entities stand ([001 data-model.md](../001-photo-recipe-import/data-model.md)). This file lists what this
phase adds or changes.

## Persisted state

### Recipe (content type `recipe`, changed)

| Property | Editor | New | Written by a submission |
|---|---|---|---|
| `category` | Multi-node tree picker, `KCCRecipeCategory`, at most 1 | no | The chosen category's key, when one was chosen and the flow creates a recipe |
| `proposedCategory` | `Textstring` (`0cc0eba1-…`) | yes | The member's proposal, trimmed, up to 100 characters; left out when blank |

### Recipe Variant (content type `recipeVariant`, changed)

| Property | Editor | New | Written by a submission |
|---|---|---|---|
| `tags` | Multi-node tree picker, `KCCRecipeTags`, any number | no | The chosen tags' keys, in the lists' order |
| `difficulty` | Flexible dropdown, `KCCDifficulty` (Easy, Medium, Hard) | no | `["Easy"]`, `["Medium"]` or `["Hard"]` (research R5) |
| `calories`, `proteinG`, `carbsG`, `fatG`, `saturatedFatG`, `fiberG`, `sugarG`, `sodiumMg` | `Umbraco.Integer` | no | Each value the member gave, `0` included; left out when empty |
| `proposedTags` | `Textstring` | yes | The member's proposal, trimmed, up to 100 characters; left out when blank |

Both new properties sit in the type's first group, after the existing properties, with a backoffice description that
they come from the member's submission and never show on the site. They travel as hand-written uSync changes to
`recipe.config` and `recipevariant.config`, with regenerated `Recipe.generated.cs` and `RecipeVariant.generated.cs`,
and `SchemaTests` gains their keys. No other store, table or migration changes. Marks are not stored (spec A7).

### Category and tag (unchanged)

`recipeCategory` and `recipeTag` content under the root content folders. A tag's `kind` is `Diet` or `Style`; a tag
with no `Style` kind counts as a diet tag, as `RecipeQueries.GetTaxonomy` already treats it. This phase reads them only.

## Limits

| Value | Rule |
|---|---|
| `calories` | Whole number, 0 to 10,000 |
| `proteinG`, `carbsG`, `fatG`, `saturatedFatG`, `fiberG`, `sugarG` | Whole number, 0 to 1,000 |
| `sodiumMg` | Whole number, 0 to 50,000 |
| `proposedCategory`, `proposedTags` | Up to 100 characters after trimming |
| `difficulty` | `Easy`, `Medium`, `Hard` or none |
| `category` | A published `recipeCategory` key, or none; never on Add Variant |
| `tags` | Published `recipeTag` keys, no duplicates |

The browser and the API hold the same limits. The browser's copy lives in `wizardDetails.ts`, the API's in
`DetailsRules.cs`.

## Server entities (request scoped)

### Details choices (new: `IRecipeQueries.GetTaxonomyChoices()`)

```text
DetailsChoices
  categories: [{ key: Guid, name: string }]          published recipeCategory, content-tree order
  tags:       [{ key: Guid, name: string, kind: "Diet" | "Style" }]
```

Feeds the page prop, the submission check (FR-023) and `ReaderTaxonomy`.

### Reader taxonomy (new: `ReaderTaxonomy`)

Built from `DetailsChoices` per read. Gives each category a short id `C1`… and each tag `T1`…, writes the prompt lines
and maps an id back to a key and, for a tag, its kind (research R2). `Characters` joins the spend reservation.

### Reader output (changed)

`ReaderOutput` gains `ReaderDetails`:

```text
ReaderDetails
  category:   { id: string|null, stated: bool, printed: string|null, reason: string|null }
  tags:       [{ id: string, stated: bool, printed: string|null, mark: ReaderMark|null }]
  difficulty: { level: "Easy"|"Medium"|"Hard"|null, printed: string|null, reason: string|null }
  nutrition:  { basis: "serving"|"recipe"|null,
                values: [{ nutrient, amount: number|null, unit, printed: string, inexact: bool }] }
    basis null: every nutrition value is empty and unmarked
    nutrient: calories | protein | carbohydrates | fat | saturatedFat | fiber | sugar | sodium | salt
    unit:     kcal | kJ | g | mg
```

The exact schema is in [contracts/read-response.md](contracts/read-response.md#reader-output-schema-additions).

### Read response (changed)

`ReadResponse` gains `Details`:

```text
DetailsValues
  category:   MarkedValue<Guid?>         always null on the variant flow
  tags:       [{ key: Guid, mark: Mark|null }]
  difficulty: MarkedValue<string?>
  nutrition:  { calories, proteinG, carbsG, fatG, saturatedFatG, fiberG, sugarG, sodiumMg }: MarkedValue<int?> each
```

A `noRecipe` outcome carries empty Details.

### Mark (changed)

| Field | Phase 1 | Added |
|---|---|---|
| `kind` | `estimate`, `unclear`, `written`, `translated` | `suggested`, `calculated` |
| `code` | `shortened`, `outOfRange`, `translated` | `printedAs`, with `arg` holding the printed text or word |

Precedence when two marks meet one value, strongest first: `unclear`, `estimate`, `suggested`, `calculated`,
`written`, `translated`. The model may set `estimate`, `unclear` and `written` as before; the server alone sets
`suggested` and `calculated` (research R4).

### Submission requests (changed)

See [contracts/submission.md](contracts/submission.md).

## Client entities (the member's open page)

### Wizard values (`wizardValues.ts`, changed)

| Field | Type | Empty | Notes |
|---|---|---|---|
| `category` | `string \| null` | `null` | A category key; ignored on submit when the mode is `variant` |
| `tags` | `string[]` | `[]` | Tag keys |
| `difficulty` | `'Easy' \| 'Medium' \| 'Hard' \| null` | `null` | |
| `nutrition` | `Record<NutritionField, string>` | `''` each | The text as typed, so the step can say what is wrong with it (FR-006) |
| `proposedCategory` | `string` | `''` | |
| `proposedTags` | `string` | `''` | |
| `marks` | as Phase 1 | | Gains the paths `recipe.category`, `variant.difficulty`, `variant.tag.<key>` and `variant.nutrition.<field>` |

`NutritionField` is the Umbraco alias: `calories`, `proteinG`, `carbsG`, `fatG`, `saturatedFatG`, `fiberG`, `sugarG`,
`sodiumMg`.

### Wizard flow state (`recipeWizardFlow.ts`, changed)

| Change | Rule |
|---|---|
| `StepId` and `ReviewSection` gain `details` | `TYPING_STEPS.recipe` is basics, variant, ingredients, instructions, details, review; `TYPING_STEPS.variant` is variant, ingredients, instructions, details, review (FR-001) |
| `choices: DetailsChoices` | From the page prop; replaced by the lists a `detailsChanged` refusal returns (FR-023) |
| `RecipeLevelValues` gains `category` and its mark | Set aside when the member accepts a proposed recipe, restored when they keep a new recipe (FR-004, spec edge case) |
| `PROCEED_RULES.details` | True when every nutrition text is empty or valid; the step shows each invalid value's error inline (FR-006) |
| Editing a Details value | Sets the value, clears that value's mark (FR-021, spec A11) and sets `editedSinceRead`. Editing a proposal clears no mark and leaves `editedSinceRead` alone |
| `applyRead` | Replaces the Details values and their marks from the read, and keeps both proposals (spec edge case) |
| `refuseDetails(choices)` | Replaces `choices`, drops a chosen category or tag the new lists lack, keeps every other value, and opens the Details step with its return to the review (FR-023) |

### State transitions

```text
typed:   … instructions → details → review → submitted
import:  … reading → (match →) review ⇄ details      (Edit on the review's Details section, spec A1)
refused: review --submit 400 detailsChanged--> details (lists replaced, return to review)
```
