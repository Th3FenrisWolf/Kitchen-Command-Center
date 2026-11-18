# Contract: Submission requests with Details

The two existing member endpoints keep their routes, their anti-forgery check and their `submissions` rate limit
(FR-024). Their JSON bodies gain optional fields. A body without them submits exactly as in Phase 1.

## `POST /api/recipes` (Create Recipe)

```json
{
  "recipeName": "Skillet Cornbread",
  "recipeDescription": "…",
  "category": "f08799ce-a590-8601-b7e3-409d91c099f9",
  "proposedCategory": "Sides",
  "firstVariant": { "…": "as below" }
}
```

## `POST /api/recipes/{recipeKey}/variants` (Add Variant), and `firstVariant` above

```json
{
  "variantName": "Honey Butter",
  "variantDescription": "…",
  "prepTime": 15, "cookTime": 25, "servings": 8,
  "ingredients": [], "instructions": [],
  "tags": ["fefc672b-221c-8cc7-8b7f-50cbee11da2a", "bd3a5a4d-c8a5-8fad-a9a3-1f552a65380a"],
  "difficulty": "Medium",
  "nutrition": {
    "calories": 450, "proteinG": 12, "carbsG": null, "fatG": 0,
    "saturatedFatG": null, "fiberG": null, "sugarG": null, "sodiumMg": 380
  },
  "proposedTags": "Mexican, Tex-Mex"
}
```

| Field | Type | Rule |
|---|---|---|
| `category` | GUID or null | A published `recipeCategory` |
| `proposedCategory` | string or null | Up to 100 characters after trimming |
| `tags` | GUID array, default `[]` | Published `recipeTag` keys; duplicates are ignored |
| `difficulty` | `"Easy"`, `"Medium"`, `"Hard"` or null | Exact match |
| `nutrition` | object or null; each value an integer or null | The limits in [data-model.md](../data-model.md#limits) |
| `proposedTags` | string or null | Up to 100 characters after trimming |

## Answers

The order of checks: names (Phase 1), then Details rules, then the member, then the lists.

| Status | Body | When |
|---|---|---|
| 200 | `{ "recipeKey" }` or `{ "variantKey" }` | As Phase 1. The draft holds exactly the given Details (SC-002) |
| 400 | `{ "error", "code": "nameTooLong", "field" }` | As Phase 1 |
| 400 | `{ "error", "code": "invalidDetails", "field" }` | A value breaks a rule above. The wizard's own checks stop this first, so the wizard shows its generic `SubmitFailed` |
| 400 | `{ "error", "code": "detailsChanged", "choices": DetailsChoices }` | The category or a tag is not a published category or tag now (FR-023). `choices` is the current lists, in the page prop's shape ([details-ui.md](details-ui.md#page-props)) |
| 401, 404, 429 | As Phase 1 | |

The `detailsChanged` check reads the published cache before `RecipeSubmissions` opens its scope. Create Recipe writes the
Details inside its existing scope, after `scope.WriteLock(Constants.Locks.ContentTree)`. Add Variant's single-node save
through `IContentEditingService` is unchanged and opens no scope. Neither path gains a read before its write
(constitution III).
