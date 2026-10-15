# Contract: `POST /api/recipe-imports/read`

Reads 1 to 10 photos as one recipe and returns the values for the wizard. It saves nothing: the member submits through
the existing `POST /api/recipes` and `POST /api/recipes/{recipeKey}/variants`, which do not change.

## Request

- **Content type**: `multipart/form-data`, at most 25 MB in total.
- **Headers**: `RequestVerificationToken`, the anti-forgery token from `<script id="api-config">`, as every member
  action sends it.
- **Rate limit**: the `imports` policy, 10 requests an hour per client by default (`RateLimits:ImportsPerHour`).

| Field | Type | Rules |
|---|---|---|
| `photos` | file, repeated | 1 to 10 files. Each is at most 4 MB and is a JPEG, PNG, WebP or GIF by its leading bytes. Together they are at most 22 MB, so that the read fits the reading service's own request limit ([research R6](../research.md#r6-images)) |
| `flow` | text | `recipe` (Create Recipe) or `variant` (Add Variant). The page sets it, not the wizard's mode: Create Recipe posts `recipe` for every read, also after the member chose a match and the wizard moved to variant mode, so every read there carries the catalog and can propose a match again ([data-model.md](../data-model.md#wizard-flow-state-recipewizardflowts), A20) |
| `recipeKey` | text (GUID) | Required when `flow` is `variant`. It must name a published recipe |

## Checks, in order

1. The anti-forgery token, which `[AutoValidateAntiforgeryToken]` checks before the action runs, else **400**.
2. A signed-in member, else **401** with an empty body.
3. A configured reader (`IRecipePhotoReader.IsConfigured`), else **503** `{ "error": "...", "code": "unavailable" }`.
4. A valid form, else **400** `{ "error": "...", "code": "invalidRequest" | "invalidPhotos" }`. Photos over 22 MB
   together are `invalidPhotos`, like any other fault in a photo, and so is a form that cannot be read: malformed, or
   over 25 MB.
5. In the `variant` flow, a published recipe, else **404** `{ "error": "Recipe not found.", "code": "recipeNotFound" }`.
6. A month's total under the ceiling, else **409**
   `{ "error": "...", "code": "ceilingReached", "availableAgainOn": "2026-11-01" }`. Nothing is spent. The total
   includes the reservations of reads still running (research R10).
7. The read. It ends with **200**, **502** `{ "error": "...", "code": "readFailed" }` (an error status from the
   service, a failed connection, a refusal, a cut-off or unusable output), or **504**
   `{ "error": "...", "code": "readTimedOut" }` after `RecipeImport:ReadTimeoutSeconds`.

The rate limiter answers **429** `{ "error": "Too many attempts. Try again later." }` with `Retry-After` before any of
these checks.

A read sends the reading service one request, with no retry, and runs under its deadline only. When the member
cancels, or the browser drops the request, the read still runs to its end, and the server settles it like any other
read. A cancelled read is therefore billed in full
([research R4](../research.md#r4-timeouts-retries-and-cancellation)).

`flow` and `recipeKey` bind as strings, and the action validates them itself. Every 400 from the action therefore has
the `{ error, code }` shape, never the framework's automatic validation response.

The `error` text serves logs and other clients only. The wizard never shows it: it maps the status and `code` to a
`RecipeWizard` dictionary key ([wizard-ui.md](wizard-ui.md#errors)).

## 200 response

```json
{
  "outcome": "recipe",
  "recipe": {
    "name": { "value": "Pancakes", "mark": null },
    "description": { "value": "Fluffy weekend pancakes.",
      "mark": { "kind": "written", "reason": "Not in the photos", "code": null, "arg": null } }
  },
  "variant": {
    "name": { "value": "Buttermilk", "mark": null },
    "description": { "value": "Tangy and tall.",
      "mark": { "kind": "written", "reason": "Not in the photos", "code": null, "arg": null } },
    "prepTime": { "value": 10, "mark": { "kind": "estimate", "reason": "Not printed", "code": null, "arg": null } },
    "cookTime": { "value": 15, "mark": null },
    "servings": { "value": 4, "mark": null }
  },
  "ingredients": [
    { "name": "flour", "quantity": 1.5, "unit": "cups", "isEyeballed": false, "mark": null },
    { "name": "garlic", "quantity": 2, "unit": "cloves", "isEyeballed": false,
      "mark": { "kind": "unclear", "reason": "Printed as 2 to 3", "code": null, "arg": null } },
    { "name": "salt", "quantity": null, "unit": "", "isEyeballed": true, "mark": null }
  ],
  "instructions": [
    { "text": "Whisk the dry ingredients.", "mark": null }
  ],
  "match": { "recipeKey": "8a6f…", "recipeName": "Pancakes", "reason": "Same dish with buttermilk." },
  "possibleDuplicate": null,
  "otherRecipes": [],
  "recipes": [ { "key": "8a6f…", "name": "Pancakes" } ]
}
```

- `outcome` is `recipe` or `noRecipe`. With `noRecipe`, every value is empty, the lists are empty, and `recipes` is null. The outcome is
  `noRecipe` when the model finds no recipe, or none it can read usefully, and also when a read keeps no ingredient
  and no step after normalization ([research R7](../research.md#r7-normalizing-a-read)).
- A mark has `kind` (`estimate`, `unclear`, `written` or `translated`) and either a `reason` from the model or a
  `code` from the server (`shortened`, `outOfRange` or `translated`) with an optional `arg`. A server mark looks like
  `{ "kind": "translated", "reason": null, "code": "translated", "arg": "Italian" }`. The server never sends English
  reason text of its own.
- `match` and `recipes` appear only in the `recipe` flow. `recipes` lists every published recipe for the "pick
  another recipe" choice.
- `possibleDuplicate` is `{ "variantKey", "variantName", "recipeName" }` or null, in both flows. In the `recipe` flow
  it is the closest variant among every recipe in the catalog, proposed or not. In the `variant` flow it is the
  closest variant of the chosen recipe ([research R9](../research.md#r9-possible-duplicates)).
- Field names follow the wizard's request types (`isEyeballed`, `prepTime`, `cookTime`, `servings`), so the client
  maps the response without renaming.

## Server rules the response already meets

The normalizer applies the limits in [research R7](../research.md#r7-normalizing-a-read). Every value in a 200 response
fits what `POST /api/recipes` accepts.

## Cost accounting

Before the read, the server reserves the read's worst-case cost against the month. Afterwards it settles the read. A
reservation is released only when the service answered
([research R10](../research.md#r10-spending-ledger-and-the-monthly-ceiling)):

| What happened | Answer | Settled |
|---|---|---|
| The service answered with usage: a read, a refusal, a cut-off or unusable output | 200, or 502 `readFailed` | The actual cost |
| The service answered with an error status | 502 `readFailed` | Zero: the whole reservation is given back |
| No answer: the connection failed | 502 `readFailed` | Nothing: the reservation stays |
| No answer: the deadline passed | 504 `readTimedOut` | Nothing: the reservation stays |

The settlement does not use the request's aborted token, so a member who has left changes none of these rows. A
process that stops mid-read settles nothing, and its reservation stays.

## What the server keeps

Nothing from the photos. The action holds them for the length of the read only, which is at most
`RecipeImport:ReadTimeoutSeconds` after a member who cancelled has left. ASP.NET Core may buffer an upload in a
temporary file, which it deletes when the request ends; on the Pi, `/tmp` is a memory-backed tmpfs. The feature writes
no file and no media item of its own, and it logs only the outcome, the model, the token counts and the cost.
