# Contract: Page props for the two wizards

Both pages stay at their current nodes and keep their sign-in redirects. Their Razor views pass these props to the Vue
page components.

## `CreateRecipeView` (`/recipes/create-recipe`)

| Prop | Type | Source |
|---|---|---|
| `resource-strings` | `Record<string, string>` via `Vue.Prop` | `GetGroup("CreateRecipe")` merged with `GetGroup("RecipeWizard")`. Keys stay full aliases, such as `RecipeWizard.Next` |
| `photo-import` | `ImportAvailability` via `Vue.Prop` | `ImportAvailability.GetAsync()`, see [data-model.md](../data-model.md#import-availability-page-prop) |
| `recipes-url` | string | `page.Parent<RecipeListingPage>()?.Url()`: the recipe listing's URL, `/recipes/` on the seeded site. The page passes it to the wizard as `recipesUrl`, the href of the success sheet's default closing link ([wizard-ui.md](wizard-ui.md#behaviour)) |

## `AddVariantView` (`/recipes/add-variant?recipe=<key>`)

| Prop | Type | Source |
|---|---|---|
| `resource-strings` | `Record<string, string>` via `Vue.Prop` | `GetGroup("AddVariant")` merged with `GetGroup("RecipeWizard")` |
| `photo-import` | `ImportAvailability` via `Vue.Prop` | As above |
| `recipe-id`, `recipe-name`, `recipe-slug` | strings | Unchanged. `SubmissionTests` pins these attributes. The page also builds the wizard's closing link from `recipe-name` and `recipe-slug` ([wizard-ui.md](wizard-ui.md#behaviour)) |

## Shape of `photo-import`

```json
{ "enabled": true, "available": false, "availableAgainOn": "2026-11-01", "maxImageEdge": 2000 }
```

`Vue.Prop` drops nulls, so `availableAgainOn` is absent while the import is available.

`enabled` is true when the registered reader reports `IsConfigured` (research R13). The Anthropic reader does so only
with an API key and a model that has a price profile (research R5).

## Rendering rules

- `enabled: false`: the wizard opens on its first typing step, exactly as today.
- `enabled: true, available: false`: the start step shows the photo choice as unavailable, with `availableAgainOn`, and
  typing stays open.
- `enabled: true, available: true`: the start step offers both choices.
