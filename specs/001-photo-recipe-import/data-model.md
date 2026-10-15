# Data Model: Recipe Import from Photos, Phase 1

Phase 1 stores no photo and adds no table. It persists one number per month: the import spend. Everything else lives
in the member's open page or exists only for the length of one request.

## Client-side entities (the member's open page)

### Photo

| Field | Type | Rules |
|---|---|---|
| `id` | string | Unique within the import |
| `blob` | Blob (JPEG) | The re-encoded photo, long edge at most `maxImageEdge`, at most 2.2 MB (research R6) |
| `url` | string | `URL.createObjectURL(blob)`, revoked when the photo is removed or the page closes |
| `source` | `'camera'` or `'device'` | Where the photo came from |

### Import

| Field | Type | Rules |
|---|---|---|
| `photos` | Photo[] | 1 to 10 before a read can start. Adding an 11th is refused with a message |
| `read` | ReadResponse or null | The latest read. Reading again replaces it |

The import ends at submission, or when the page closes or reloads. A failed or cancelled read keeps the photos.

### Wizard values (`wizardValues.ts`)

These are the values that the wizard submits, in the shapes the existing endpoints already accept. Phase 1 does not
change `CreateRecipeRequest` or `CreateVariantRequest`. They are not the unpublished recipe and variant nodes that the
spec calls drafts; those come into being only at submission.

| Field | Type | Source |
|---|---|---|
| `recipeName`, `recipeDescription` | string | Recipe mode only |
| `variantName`, `variantDescription` | string | Both modes |
| `prepTime`, `cookTime`, `servings` | number or undefined | Both modes |
| `ingredients` | `{ name, quantity?, unit, isEyeballed, mark? }[]` | Both modes. A row carries its own mark |
| `instructions` | `{ text, mark? }[]` | Both modes. Step numbers are assigned at submission. A row carries its own mark |
| `marks` | map from a value path to a Mark | The single values only: `recipe.name`, `recipe.description`, `variant.name`, `variant.description`, `variant.prepTime`, `variant.cookTime` and `variant.servings` |

A row's mark lives on the row, never under a list index. It follows its row when another row is added or removed, and
when the review drops a blank row. No mark reaches a request body.

### Wizard flow state (`recipeWizardFlow.ts`)

| Field | Type | Rules |
|---|---|---|
| `flow` | `'recipe'` or `'variant'` | Fixed by the page: `recipe` on Create Recipe and `variant` on Add Variant. Every read posts it, with `recipeKey` in the `variant` flow only. A match choice changes `mode`, never `flow` (A20) |
| `mode` | `'recipe'` or `'variant'` | Create Recipe starts in `recipe` and Add Variant in `variant`. On Create Recipe a match choice moves it to `variant`, and applying a new read moves it back to `recipe` (A20) |
| `recipe` | `{ key, name }` or null | Set in variant mode |
| `start` | `'photos'`, `'typing'` or null | Chosen on the start step. It is `typing` straight away when the import is not enabled |
| `step` | StepId | `start`, `photos`, `reading`, `match`, `basics`, `variant`, `ingredients`, `instructions`, `review`, `submitted` |
| `returnTo` | `'review'` or null | Set when a step is opened from the review, and by `changePhotos()` while the wizard holds values from an applied read (A21). Applying a read clears it |
| `matchSettled` | boolean | False after a read that proposes a recipe; a match choice sets it true (FR-024) |
| `editedSinceRead` | boolean | Turns true when the member types or changes any wizard value, or adds or removes an ingredient or step row, counted from the wizard's start or from the last applied read. Applying a read clears it. Next, Back, the start choice and the match choices never set it. Photos are not wizard values, so adding or removing one never sets it |

**Step order**

| Mode and start | Steps |
|---|---|
| Recipe, typing | start (only when the import is enabled) → basics → variant → ingredients → instructions → match, while a proposal is unsettled → review. Back on that match step returns to instructions (A35) |
| Recipe, photos | start → photos → reading → match, only when a read proposes a recipe → review |
| Variant, typing | start (only when the import is enabled) → variant → ingredients → instructions → review |
| Variant, photos | start → photos → reading → review |

On Create Recipe every read follows the "Recipe, photos" row, also in variant mode after a match choice, because
applying the read returns the wizard to recipe mode (A20).

**Progress**: `WizardProgress` shows on the editing steps only, with today's totals: 5 steps in recipe mode (basics,
variant, ingredients, instructions, review) and 4 in variant mode. The start, photo, reading and match steps show no
progress.

**Transitions**

- `edit(section)` from the review opens that section's step with `returnTo = 'review'`. `backToReview()` returns to the
  review and keeps the changes.
- Next on a step opened from the review also returns to the review.
- Back on a step opened from the review moves to the previous step and keeps `returnTo` (A28). Either start choice
  resets it: Type it in clears it, and From photos sets it as `changePhotos()` does.
- `acceptMatch(recipe)` and `pickRecipe(recipe)` switch the mode to `variant` and set `recipe`. They set the
  recipe-level values aside and keep the variant values, the marks and the photos. `keepNewRecipe()` returns to
  `recipe` mode and restores the recipe-level values that were set aside.
- `matchSettled` is false after a read that proposes a recipe, and `acceptMatch`, `pickRecipe` and `keepNewRecipe` set it
  true. While a proposal is unsettled, every move to the review opens the match step instead (FR-024, FR-028): Back to
  review, and Next on the last typing step.
- A read on Create Recipe posts `flow` `recipe`, with no `recipeKey`, also after `acceptMatch` or `pickRecipe`, so the
  server sends the catalog and the read can propose a match again. Applying it returns the wizard to `recipe` mode,
  drops the set-aside recipe-level values and the chosen `recipe`, and opens the match step when the new read proposes
  a recipe, or the review when it does not (A20).
- The review keeps the match step reachable through "Change", so the member can revise the choice without a new read.
- `changePhotos()` returns to the photo step and keeps the photos. While the wizard holds values from an applied read,
  it sets `returnTo = 'review'`, so the photo step offers Back to review until a new read applies (A21).
- Back on the review of a photos path is `changePhotos()`: it goes to the photo step, and the reading step is never a
  step to go back to.
- Every read starts through one entry, `startRead()`. Whenever `editedSinceRead` is true, it needs a confirmation
  first, whether or not an earlier read exists. The review's Read again and the photo step's Read recipe both go
  through it.
- Applying a read fills the values and clears `editedSinceRead` and `returnTo`. A `noRecipe` read fills nothing and
  leaves both as they were.
- A read started from the review, or from the photo step while `returnTo` is `'review'`, that is cancelled, fails,
  finds no recipe or gets a 409 lands on the photo step with `returnTo = 'review'` (A21).
- A 409 `ceilingReached` result puts the member on the photo step, with the photos, the unavailable notice and its
  `availableAgainOn`. Read recipe is then off until the page reloads, and Type it in leads to the first typing step.
  The total behind a 409 includes the reservations of reads still running (A19), so near the ceiling a 409 can come
  from reads that settle below the ceiling within the read deadline.
- Editing a value removes its mark. Editing any field of an ingredient or a step removes that row's mark, and no
  other.
- A `possibleDuplicate` in the read stays with the import through every choice on the match step, in both modes,
  until the next read.
- While the import holds photos, the view-only photo strip shows on the match step, the four editing steps and the
  review, and on no other step.
- A submission is possible from the review only. The `canProceed` rules are those of today's wizards: a recipe name and
  description, a variant name, at least one named ingredient, and at least one step.
- `hasUnsavedWork` is true while the import holds photos or read values, or a photo is pending, and nothing is
  submitted. The page-wide leave
  guard and the `beforeunload` handler read it (research R20).

## Request-scoped server entities

### Read request (multipart form)

| Field | Type | Rules |
|---|---|---|
| `photos` | file[] | 1 to 10 files. Each is at most 4 MB and is a JPEG, PNG, WebP or GIF by its leading bytes. Together they are at most 22 MB, in a request of at most 25 MB |
| `flow` | `recipe` or `variant` | Required. The page sets it, not the wizard's mode (A20) |
| `recipeKey` | GUID | Required for `variant`, and must name a published recipe |

### Catalog entry (Create Recipe flow only)

| Field | Type | Rules |
|---|---|---|
| `shortId` | string | `R1` to `Rn`, in name order, with at most 500 entries |
| `key`, `name` | GUID, string | From `RecipeRecord` |
| `description` | string | Cut to 160 characters |
| `variantNames` | string[] | From the recipe's published variants |

### Reader output

The JSON that the model returns, as fixed by [contracts/reader-output.schema.json](contracts/reader-output.schema.json).
It is never sent to the browser as is. `ReadResultNormalizer` turns it into the read response.

### Read attempt

What `IRecipePhotoReader.ReadAsync` hands the endpoint, and what the ledger then settles. It never leaves the server.
A reservation is released only when the service answered (research R10).

| Kind | When | Usage | The endpoint answers | The ledger settles |
|---|---|---|---|---|
| Read | The service answered with output that parses | Yes | 200 | The actual cost |
| Refused, cut off or unusable | A `refusal` or `max_tokens` stop reason, or output that does not parse | Yes | 502 `readFailed` | The actual cost |
| Service error | The service answered with an error status | No | 502 `readFailed` | Zero: the whole reservation is given back |
| No answer | The connection failed | No | 502 `readFailed` | Nothing: the reservation stays |
| Timed out | The deadline passed first | No | 504 `readTimedOut` | Nothing: the reservation stays |

### Read response

The spec's "Read result" (Key Entities): the server's answer, as fixed by
[contracts/read-endpoint.md](contracts/read-endpoint.md). It holds `outcome`, the recipe and variant values with their
marks, `ingredients`, `instructions`, `match`, `possibleDuplicate`, `otherRecipes` and, in the Create Recipe flow,
`recipes` for the "pick another recipe" list. `possibleDuplicate` can appear in both flows (research R9). The outcome
is `noRecipe` when the model finds no recipe that it can read usefully (`found` false), and also when a read keeps no
ingredient and no step after normalization (research R7, R19).

### Mark

| Field | Type | Rules |
|---|---|---|
| `kind` | `estimate`, `unclear`, `written` or `translated` | One mark per value. When two apply, the order of precedence is `unclear`, `estimate`, `written`, `translated` |
| `reason` | string or null | Set on a mark from the model: its own short reason, such as "Printed as 2 to 3". It is read data, like the recipe text |
| `code` | `shortened`, `outOfRange`, `translated` or null | Set on a mark from the server, never alongside `reason`. The wizard renders it from `MarkReasonShortened`, `MarkReasonOutOfRange` or `MarkReasonTranslated` |
| `arg` | string or null | The code's argument: the source language for `translated` |

The model sets `estimate`, `unclear` and `written` marks with a `reason`. The server sets `translated` marks, and
`unclear` marks when it shortens or blanks a value, each with a `code` (research R7).

### Model profile (code)

| Field | Type |
|---|---|
| `id` | string |
| `prices` | A price set: `inputUsdPerMTok`, `outputUsdPerMTok`, `cacheWriteUsdPerMTok`, `cacheReadUsdPerMTok`, each a decimal |
| `upperPrices` | A second price set, or null |
| `upperPricesAbovePromptTokens` | int or null: the prompt tokens above which `upperPrices` applies, 100,000 for `claude-haiku-5-5` |
| `thinking` | `adaptive` or omitted |
| `takesEffort` | boolean |
| `maxImageEdge` | int |
| `imageTokenCap` | int |

A read's prompt tokens are its input, cache-creation and cache-read tokens together. `ReadCost` settles a read at
`upperPrices` when its prompt tokens are over the threshold, and at `prices` otherwise. A reservation always prices at
the highest set: `upperPrices` where the profile has them (research R5).

Every read sends `MaxTokens = 12,000` (research R5). A model id outside the table has no profile, and the import is
then off: the ledger never prices a read with borrowed figures.

### Spend reservation

| Field | Type | Rules |
|---|---|---|
| `month` | `yyyy-MM` (UTC) | The month in which the read started |
| `reservedMicroUsd` | long | 3,000 prompt tokens, plus the catalog's characters divided by 3.5, plus each photo at `imageTokenCap`, at the input price of the profile's highest price set; plus 12,000 output tokens at that set's output price |

## Persisted state

### Monthly import spend

| Item | Value |
|---|---|
| Store | Umbraco's key-value table, through `IKeyValueService` |
| Key | `KCC.RecipeImport.SpendMicroUsd.<yyyy-MM>`, for example `KCC.RecipeImport.SpendMicroUsd.2026-10` |
| Value | The month's total in micro-dollars, as an invariant integer string. A missing key means 0 |
| Write | Only under `scope.WriteLock(Constants.Locks.KeyValues)`, taken before the first read in the scope |
| Month | Every ledger call names it. The endpoint passes the current UTC month; tests pass a far-past month |
| Scope | One site. Each site that holds the key keeps its own total, against its own ceiling, in its own SQLite file: the Pi, and the owner's dev site (spec A2) |

**Lifecycle**

1. **Reserve.** In a core scope of its own, take the lock, then read the total. If it is at or above the ceiling,
   refuse. Otherwise add the reservation, whatever its size, and complete the scope, so that nothing holds the lock
   while the read runs. The total includes the reservations of reads still running (A19).
2. **Settle.** After the read, in a new core scope that takes the same lock first, add the settled cost minus the
   reservation to the reservation's month. An answer with usage settles the actual cost. An error status settles
   zero. The total can only fall back by what the reservation over-estimated ("Read attempt" above).
3. **No settlement.** A read with no answer keeps its reservation: the deadline passed, the connection failed, or the
   process stopped mid-read.
4. **A member who leaves changes nothing.** A read whose request was cancelled or dropped runs to its end and is
   settled like any other (research R4).
5. **Keep history.** Earlier months' keys stay as a record. Nothing reads or prunes them.

### Import availability (page prop)

| Field | Type | Rules |
|---|---|---|
| `enabled` | boolean | The registered reader reports `IsConfigured`: the Anthropic reader with an API key and a model that has a profile, the stand-in reader (Development and Testing only), or a test fake |
| `available` | boolean | `enabled`, and the month's total is under the ceiling |
| `availableAgainOn` | `yyyy-MM-dd` or null | The first day of the next UTC month when not available |
| `maxImageEdge` | int | The lower of `RecipeImport:MaxImageEdge` and the model's image edge, or the option alone for a model with no profile |

## Relationships to existing data

- A submission creates the same unpublished `recipe` and `recipeVariant` nodes as today, through `RecipeSubmissions`.
  Phase 1 changes neither document type.
- The catalog and the duplicate check read published recipes and variants through `IRecipeQueries`. Drafts are never
  read.
