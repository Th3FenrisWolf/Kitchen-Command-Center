# Contract: The shared recipe wizard

The UI contract for `src/KCC.Web/Features/Components/RecipeWizard/`. Steps, transitions and progress are in
[data-model.md](../data-model.md#wizard-flow-state-recipewizardflowts). Styling follows `docs/brand/kit.md`; the
`kit-builder` agent builds each surface and the `brand-steward` agent reviews it.

## Components

| Component | Purpose | Kit pattern |
|---|---|---|
| `RecipeWizard.vue` | Shell: progress, current step, the view-only photo strip, the leave guard, submission and the success sheet | `WizardProgress`, `KccSheet crisp` per step, as today |
| `StartStep.vue` | From photos, or type it in | `kcc-btn kcc-btn--lg`. A neutral `kcc-well` holds the unavailable notice |
| `PhotoStep.vue` | Camera, device photos, strip, privacy note, Read recipe, and Back to review after Change photos | `KccSheet crisp`. The shutter and remove buttons are `kcc-btn kcc-btn--icon` with an `aria-label` |
| `CameraView.vue` | Live rear camera with a shutter. It stops every track when it unmounts, and maps a failure to its message (see Camera below) | `<video playsinline muted>`. Images take no radius |
| `PhotoStrip.vue` | One tile per photo: open it larger, or remove it. Its view-only mode has no remove button | An in-flow list (Tailwind flex and gap), never `kcc-tilewrap` or `kcc-tile`, which the kit keeps for a pinned, pointer-transparent wax tile. Each tile is a `<button type="button">` (`photo-tile`) named by `ViewPhoto`, holding a plain `<img alt="" class="block size-20 object-cover">`: 80px, inside the tile tears' 48–128px band, with no wax filter and no radius. `kit-builder` may make the button the `kcc-torn` wrapper and clip the image with a `kcc-tear-tile-{n}` preset and `[clip-path:var(--tear)]`, with neighbours on different presets. `photo-remove` is a `kcc-btn kcc-btn--icon` named by `RemovePhoto`, a sibling of the tile button, never inside it. The strip uses only Classes-table classes and Tailwind, so `docs/brand/kit.md` and `Kit.css` stay unchanged |
| `PhotoViewer.vue` | One photo, full size | Teleported to `body`, `role="dialog"`, `aria-modal`, closes on Escape, keeps focus inside, as `CookMode.vue` does, and returns focus to the tile button that opened it |
| `ReadingState.vue` | Progress during a read, with Cancel. It holds a screen wake lock while the read runs | `aria-busy` on the region, `fa-solid fa-circle-notch fa-spin` and a `kcc-kick` message |
| `MatchStep.vue` | The proposed recipe, another recipe, or a new recipe, and the possible-duplicate notice | `KccSheet crisp`. A possible duplicate shows in a `kcc-well kcc-well--warning` |
| `BasicsStep.vue`, `VariantStep.vue`, `IngredientsStep.vue`, `InstructionsStep.vue` | Today's steps, extracted | Today's markup and fields. The quantity input's `step` is `any` (research R7) |
| `ReviewStep.vue` | Every section, each with Edit. Change photos, Read again, the possible-duplicate notice, and Submit | Today's review markup, plus a `kcc-btn--text` Edit button in each section. The notice is a `kcc-well kcc-well--warning` |
| `SubmittedSheet.vue` | The success sheet: the submitted message and one closing link | Today's success sheet: `KccSheet` with a green wash and tape, `kcc-h3`, `kcc-body`, and the link as a large marker button |
| `LeaveConfirm.vue` | Asks before a link is followed while work is unsaved | Teleported to `body`, `role="alertdialog"`, `aria-modal`, closes on Escape, keeps focus inside, as `CookMode.vue` does, and returns focus to the link after Stay or Escape, or, when that link sat in a header card that has closed, to the card's menu button (the search field for the search card). One `KccSheet crisp` on the desk colour: the warning in a `kcc-well kcc-well--warning kcc-kick`, Stay as `kcc-btn`, Leave as `kcc-btn--ghost` |
| `PossibleDuplicateNotice.vue` | The possible-duplicate notice, shared by the match step and the review | `kcc-well kcc-well--warning kcc-kick`, `possible-duplicate` |
| `ValueMark.vue` | One mark: its kind and its reason | `kcc-badge` for the kind, the reason in body text. Status is never coloured text |
| `recipeWizardKeys.ts` | Every `RecipeWizard` key as a typed constant map | Not a component. Computed keys come from it |
| `leaveGuard.ts` | Says which clicks the leave guard stops | Not a component. A plain function, tested in the node environment |

Two shared components gain a prop, so that the wizard can pass their text from strings:

- `Components/Forms/NumberStepper.vue` gains `valueLabel`, which replaces its built-in "Value" and "{label} in {unit}".
  The wizard also passes `decreaseLabel` and `increaseLabel`.
- `Components/Wizard/WizardProgress.vue` gains `label`, which replaces its built-in `aria-label="Steps"`.

## Behaviour

- **Focus**: when the step changes, focus moves to the new step's heading.
- **Progress**: `WizardProgress` shows on the editing steps only, with 5 steps in recipe mode and 4 in variant mode.
- **Back to review**: a step opened from the review shows `back-to-review` (`BackToReview`), which returns to the
  review with the member's changes. After Change photos, while the wizard holds values from an applied read, the photo
  step shows it too, until a new read applies. A read started from the review, or from the photo step with that
  return, that is cancelled, fails, finds no recipe or gets a 409 lands on the photo step with `back-to-review` (A21).
- **Photos**: the page refuses an 11th photo with `TooManyPhotos`, and a photo it cannot decode or shrink to 2.2 MB
  with `UnsupportedFile`. A photo still being taken or encoded counts as pending: Read recipe stays off until every
  pending photo is added or dropped, so a read never leaves out a photo the member already chose.
- **Camera**: `CameraView` shows `CameraStarting` while the camera starts. When `getUserMedia` fails, `camera-message`
  shows `CameraDenied` for a `NotAllowedError` and `CameraMissing` for any other failure. Add photos stays available
  either way.
- **Photos beside the steps**: while the import holds photos, the shell shows one view-only `PhotoStrip`
  (`wizard-photos`) on the match step, the four editing steps and the review. Each tile is a button that opens
  `PhotoViewer`, and closing the viewer returns focus to that button. Nothing can be removed there: photos are added
  and removed on the photo step only. The strip takes `PhotosTitle` as its label.
- **Reading**: `ReadingState` holds a screen wake lock from the start of a read to its end, through `useWakeLock`
  (`Features/Pages/VariantDetail/useWakeLock.ts`). Cancel aborts the browser's request and returns to the photo step
  with no message (see Back to review). The server still finishes the read and counts its cost (research R4).
- **Leaving**: while the import holds photos or read values, or a photo is pending, and nothing is submitted, one
  page-wide guard asks first (research R20).
  - A plain click on a same-origin link anywhere on the page, in the header, in the footer or on the hero's Cancel,
    opens `LeaveConfirm` instead of leaving. Stay, or Escape, closes it and returns focus to the link, or, when that link sat in a header card that has closed, to the card's menu button (the search field for the search card).
    Leave follows the link.
  - `leaveGuard.ts` says which clicks count. A click with a modifier key or another mouse button, a link with
    `target="_blank"` or `download`, a link to another origin, a link to a fragment of the same page and a click that
    another handler cancelled all pass.
  - A `beforeunload` handler asks the browser to confirm what no link click starts: a reload, a closed tab, the back
    button or one of the header's forms. Leave removes it, so the member is asked once.
  - On iOS Safari, which has no leave prompt, a reload, a closed tab, the back gesture, an app switch or one of the
    header's forms gives no warning.
- **No recipe**: a `noRecipe` read fills nothing and stays on the photo step with the photos, `NoRecipeFound` and
  `TypeInstead` (see Back to review).
- **Reading again**: every read starts through one entry in the flow module, which asks first whenever
  `editedSinceRead` is true, whether or not an earlier read exists. The flag turns true when the member types or
  changes any wizard value, or adds or removes an ingredient or step row, counted from the wizard's start or from the
  last applied read. Applying a read clears it, and Next, Back, the start choice and the match choices never set it
  ([data-model.md](../data-model.md#wizard-flow-state-recipewizardflowts)). The entry shows a `kcc-well--warning`
  (`read-again-warning`) with a Read again button (`read-again-confirm`, `ReadAgain`) and a Cancel button
  (`ReadingCancel`), never `window.confirm`. This holds for Read again on the review, for Read recipe on the photo
  step after Change photos, and for a first Read recipe after the member typed values and came back to the photos.
  On Create Recipe every read posts `flow=recipe`, also after a match choice moved the wizard to variant mode.
  Applying it returns the wizard to recipe mode, drops the set-aside values and the earlier choice, and opens the match
  step again when the new read proposes a recipe (A20).
- **Possible duplicate**: when a read names one, the notice (`possible-duplicate`) shows on the match step, when the
  flow has one, and on the review in both modes, whatever the member chooses on the match step. It names the variant
  and its recipe, and the member can still submit.
- **Missing values**: the review shows `MissingValue` at each required value that a read left empty: the recipe name
  and description, the variant name, an ingredient, or a step. Submit stays disabled until the member fills them.
- **Success sheet**: after a submission the wizard shows `SubmittedSheet`: `RecipeSubmitted` and
  `RecipeSubmittedMessage` in recipe mode, `VariantSubmitted` and `VariantSubmittedMessage` in variant mode, then one
  closing link (`wizard-closing-link`). The wizard hard-codes no href. Its default link is `BackToRecipes` to the
  recipe listing's URL, which the Create Recipe page passes as the wizard's `recipesUrl` prop
  ([page-props.md](page-props.md)). A page may pass its own link through the wizard's `closingLink` prop (`label` and
  `href`). Add Variant passes "Back to <recipe>", built from `AddVariant.BackTo` and the recipe's name, with the
  recipe's URL. Create Recipe passes `recipesUrl` only, so a variant matched there also ends with "Back to Recipes",
  at the listing's URL.
- **Server rendering**: the camera, `createImageBitmap`, `URL.createObjectURL`, the wake lock and the leave guard run
  only after mount. `PhotoViewer` and `LeaveConfirm` teleport to `body` only after mount, behind an `isMounted` flag as
  in `CookMode.vue`, so the server prints neither and the E2E suite tests them. The server renders the start step, or
  the first typing step when the import is not enabled.

## Errors

The wizard shows no text from a server response. It maps each failure to a key, shown in a
`kcc-well kcc-well--danger kcc-kick` with `role="alert"`.

| Request | Status and `code` | Key |
|---|---|---|
| Read | 400 `invalidPhotos` | `PhotosRejected` |
| Read | 400 `invalidRequest`, or 400 with no code | `ReadFailed` |
| Read | 401 | `SignInAgain` |
| Read | 404 `recipeNotFound` | `RecipeNotFound` |
| Read | 409 `ceilingReached` | `ImportUnavailable`, with `availableAgainOn` |
| Read | 429 | `TooManyAttempts` |
| Read | 502 `readFailed`, or a network failure | `ReadFailed` |
| Read | 503 `unavailable` | `ImportOff` |
| Read | 504 `readTimedOut` | `ReadTimedOut` |
| Read | Any other failure | `ReadFailed` |
| Read | Cancelled by the member | No message |
| Submit | 400 | `SubmitFailed` |
| Submit | 401 | `SignInAgain` |
| Submit | 404 | `RecipeNotFound` |
| Submit | 429 | `TooManyAttempts` |
| Submit | Any other failure | `UnexpectedError` |

A failed read keeps the photos and offers `TryAgain` and `TypeInstead` on the photo step (see Back to review under
Behaviour). "Any other failure" covers every status that the table does not list, such as a 500 or a 502 without a
code from the proxy during a deploy.

A 409 is not shown as an error. The member lands on the photo step with the photos, the unavailable notice
(`import-unavailable`) and `TypeInstead`, without the camera or Add photos (A26; see Back to review under Behaviour). The 409 row arrives with User Story 2,
together with `ImportUnavailable`; until then a 409 falls through to `ReadFailed`, as any other failure does.

Every failure result of `Utilities/Api.ts` carries the HTTP `status`, 0 when the request got no answer, the body's
`code`, and the parsed body as `body` when it is a JSON object, else null. `wizardErrors.ts` maps from `status` and
`code`, for reads and for submissions alike, and the 409 row takes `availableAgainOn` from `body`.

## Test hooks

Every id and role that a test locates by today stays: the ids ending `-prep-time`, `-cook-time` and `-servings`, and the
button names Next and Submit for Review. The two E2E submission tests move from placeholder text to the field hooks
below, because two placeholders change (research R17), and they choose `wizard-start-typing` first, because the E2E site
runs the stand-in reader. New elements carry these `data-testid` values:

| `data-testid` | Element |
|---|---|
| `wizard-start-photos`, `wizard-start-typing` | The two start choices |
| `import-unavailable` | The unavailable notice |
| `wizard-recipe-name`, `wizard-recipe-description`, `wizard-variant-name`, `wizard-variant-description` | The text fields of the basics and variant steps |
| `wizard-ingredient-name`, `wizard-instruction-text` | Each ingredient name and each step text |
| `camera-view`, `camera-shutter`, `camera-message` | The camera, its shutter, and its denied or missing message |
| `photo-input` | The file input for device photos |
| `photo-tile`, `photo-remove`, `photo-viewer` | A strip tile's button, which opens the photo; its remove button on the photo step, beside the tile button; and the overlay |
| `wizard-photos` | The view-only photo strip on the reading step (A36), the match step, the four editing steps and the review |
| `read-photos`, `reading-state`, `reading-cancel` | Read recipe, the progress region, and Cancel |
| `read-error`, `submit-error` | A read error well and a submission error well |
| `match-step`, `match-accept`, `match-pick`, `match-keep` | The match step and its choices |
| `possible-duplicate` | The possible-duplicate notice, on the match step and on the review |
| `review-step`, `review-edit-{basics,variant,ingredients,instructions}`, `back-to-review` | The review, its Edit buttons, and the return to the review on a step opened from it and on the photo step after Change photos |
| `value-mark` | Each mark, with `data-kind` set to its kind |
| `change-photos`, `read-again` | The photo controls on the review |
| `read-again-warning`, `read-again-confirm` | The warning before a read replaces edits, and its Read again button |
| `leave-dialog`, `leave-stay`, `leave-confirm` | The leave confirmation, Stay, and Leave |
| `wizard-submit`, `wizard-submitted`, `wizard-closing-link` | Submit for Review, the success sheet, and its closing link |

## Strings

The shared `RecipeWizard` group holds 110 keys. `CreateRecipe` keeps `CreateRecipe.CreateRecipe`. `AddVariant` keeps
`AddVariantFor` and `Cancel` for its hero, and `BackTo` for the closing link that its page passes to the wizard. `{0}`
and `{1}` are filled in by the component.

**Source**: `AV` is the current `AddVariant.<same key>` file, moved with its value; `AV: <key>` is a different current
`AddVariant` key; `CR` is Create Recipe's hard-coded text, quoted where Add Variant differed; `New` is new text. Where
the two wizards differed, the Add Variant value wins, because the owner can already edit it (research R17).

### Steps, review and submission (Foundational)

| Key | English | Source |
|---|---|---|
| `RecipeBasics` | Recipe Basics | CR |
| `RecipeName` | Recipe Name | CR |
| `RecipeNamePlaceholder` | e.g., Mac & Cheese | CR |
| `Description` | Description | AV |
| `RecipeDescriptionPlaceholder` | A short description of this dish | CR |
| `FirstVariant` | First Variant | CR |
| `FirstVariantHint` | Each recipe needs at least one variant — a specific way to make it. | CR |
| `VariantInfo` | Variant Info | AV |
| `VariantName` | Variant Name | AV |
| `VariantNamePlaceholder` | e.g., Classic Stovetop | CR |
| `VariantDescriptionPlaceholder` | Add a short description of your variant | AV: `DescriptionPlaceholder` (CR: "What makes this variant special?") |
| `PrepTime` | Prep Time | AV |
| `CookTime` | Cook Time | AV |
| `Servings` | Servings | AV |
| `Serves` | Serves | AV (CR's review used "Prep", "Cook" and "Serves"; the review now uses `PrepTime`, `CookTime` and `Serves`) |
| `Min` | min | AV |
| `ValueInUnit` | {0} in {1} | New (`NumberStepper`) |
| `DecreaseLabel` | Decrease {0} | New (`NumberStepper`) |
| `IncreaseLabel` | Increase {0} | New (`NumberStepper`) |
| `Ingredients` | Ingredients | AV |
| `IngredientName` | Ingredient Name | AV (CR: "Ingredient name") |
| `IngredientNamePlaceholder` | e.g. All-purpose flour | AV (CR: "Ingredient name") |
| `Eyeball` | Eyeball this ingredient | AV |
| `Quantity` | Quantity | AV |
| `QuantityPlaceholder` | e.g. 2 | AV (CR: "Qty") |
| `Unit` | Unit | AV |
| `UnitPlaceholder` | e.g. cups | AV (CR: "Unit") |
| `RemoveIngredient` | Remove Ingredient | CR (AV: `Remove` served both rows) |
| `AddIngredient` | Add Ingredient | AV |
| `ToTaste` | to taste | AV |
| `Instructions` | Instructions | AV |
| `DescribeThisStep` | Describe this step | AV |
| `RemoveStep` | Remove Step | CR (AV: `Remove`) |
| `AddStep` | Add Step | AV |
| `ProgressLabel` | Steps | New (`WizardProgress`'s fixed `aria-label`) |
| `Next` | Next | AV |
| `Back` | Back | AV |
| `ReviewAndSubmit` | Review & Submit | AV |
| `ReviewRecipe` | Recipe | CR |
| `ReviewFirstVariant` | First variant | CR |
| `Step` | Step | AV (one step; CR showed "steps") |
| `Steps` | Steps | AV (several steps) |
| `SubmitForReview` | Submit for Review | AV |
| `Submitting` | Submitting... | AV |
| `RecipeSubmitted` | Recipe Submitted! | CR |
| `RecipeSubmittedMessage` | Your recipe has been submitted for review. An admin will review and publish it. | CR |
| `VariantSubmitted` | Variant submitted! | AV |
| `VariantSubmittedMessage` | Your variant has been submitted for review. | AV |
| `BackToRecipes` | Back to Recipes | CR |
| `SubmitFailed` | We couldn't submit this. Please try again. | New (AV: `FailedToAddVariant` named a variant only) |
| `UnexpectedError` | Something went wrong. Please try again. | AV |
| `SignInAgain` | Your session has ended. Sign in again, then try once more. | New |
| `RecipeNotFound` | That recipe is no longer on the site. | New |
| `TooManyAttempts` | Too many attempts. Try again later. | New |

### Photo import and marks (User Story 1)

| Key | English | Source |
|---|---|---|
| `StartTitle` | How do you want to start? | New |
| `StartFromPhotos` | From photos | New |
| `StartFromPhotosHint` | Take or add photos of a recipe, and the site fills in the steps. | New |
| `StartTyping` | Type it in | New |
| `StartTypingHint` | Fill in each step yourself. | New |
| `ImportOff` | Photo import is off right now. You can still type the recipe in. | New |
| `PhotosTitle` | Photos | New |
| `TakePhoto` | Take photo | New |
| `AddPhotos` | Add photos | New |
| `CameraStarting` | Starting the camera… | New |
| `CameraDenied` | The camera is blocked. Allow it in your browser settings, or add photos from your device. | New |
| `CameraMissing` | No camera found. Add photos from your device instead. | New |
| `PhotoCount` | {0} of {1} photos | New |
| `TooManyPhotos` | An import holds up to {0} photos. | New |
| `UnsupportedFile` | The site can't read this file. Save it as a JPEG and try again. | New |
| `ViewPhoto` | View photo | New |
| `RemovePhoto` | Remove photo | New |
| `ClosePhoto` | Close | New |
| `PrivacyNote` | Your photos go to an outside AI service to be read. The site keeps no copy. | New |
| `ReadRecipe` | Read recipe | New |
| `Reading` | Reading your recipe… | New |
| `ReadingCancel` | Cancel | New |
| `NoRecipeFound` | We couldn't read a recipe in these photos. Try a clearer photo, add photos, or type it in. | New |
| `ReadFailed` | The read failed. Try again, or type the recipe in. | New |
| `ReadTimedOut` | The read took too long. Try again with fewer or clearer photos. | New |
| `PhotosRejected` | The site couldn't use these photos. Retake them or add others. | New |
| `OtherRecipesSeen` | These photos also show: {0}. Import each one on its own. | New |
| `TryAgain` | Try again | New |
| `TypeInstead` | Type it in | New |
| `MarkEstimate` | Estimate | New |
| `MarkUnclear` | Check this | New |
| `MarkWritten` | Written for you | New |
| `MarkTranslated` | Translated | New |
| `MarkReasonShortened` | Shortened to fit. | New |
| `MarkReasonOutOfRange` | Outside the allowed range. | New |
| `MarkReasonTranslated` | Translated from {0}. | New |
| `MissingValue` | Fill this in before you submit. | New |
| `LeaveWarning` | Leave this page? You will lose your photos and everything you have not submitted. | New |
| `LeaveStay` | Stay | New |
| `LeavePage` | Leave | New |

### Ceiling (User Story 2)

| Key | English | Source |
|---|---|---|
| `ImportUnavailable` | Photo import is resting until {0}. You can still type the recipe in. | New |

### Step links (User Story 3)

| Key | English | Source |
|---|---|---|
| `Edit` | Edit | New |
| `EditSection` | Edit {0} | New |
| `BackToReview` | Back to review | New |
| `ChangePhotos` | Change photos | New |
| `ReadAgain` | Read again | New |
| `ReadAgainWarning` | Reading again replaces your edits. | New |

### Matching (User Story 4)

| Key | English | Source |
|---|---|---|
| `MatchTitle` | Is this a new way to make {0}? | New |
| `AddAsVariant` | Add as a new variant of {0} | New |
| `PickAnotherRecipe` | Add to another recipe | New |
| `PickRecipeLabel` | Recipe | New |
| `KeepNewRecipe` | Keep it as a new recipe | New |
| `PossibleDuplicate` | This looks almost the same as {0}, a variant of {1}. | New |
| `AddingTo` | Adding a variant to {0} | New |
| `KeepingNewRecipe` | Keeping it as a new recipe | New |
| `ChangeMatch` | Change | New |

### Retired `AddVariant` files

- **Moved with their values** (31): `AddIngredient`, `AddStep`, `Back`, `CookTime`, `DescribeThisStep`, `Description`,
  `Eyeball`, `IngredientName`, `IngredientNamePlaceholder`, `Ingredients`, `Instructions`, `Min`, `Next`, `PrepTime`,
  `Quantity`, `QuantityPlaceholder`, `ReviewAndSubmit`, `Serves`, `Servings`, `Step`, `Steps`, `SubmitForReview`,
  `Submitting`, `ToTaste`, `UnexpectedError`, `Unit`, `UnitPlaceholder`, `VariantInfo`, `VariantName`,
  `VariantSubmitted`, `VariantSubmittedMessage`.
- **Retired with a different successor** (3): `DescriptionPlaceholder` (to `VariantDescriptionPlaceholder`), `Remove`
  (to `RemoveIngredient` and `RemoveStep`), `FailedToAddVariant` (to `SubmitFailed`, new text).
- **Kept** (3): `AddVariantFor` and `Cancel` for the hero, and `BackTo` for the closing link, with the group file
  `addvariant.config`.
