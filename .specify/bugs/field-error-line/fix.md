# Bug Fix: Written fields outside login never take the error line

- **Slug**: field-error-line
- **Fixed**: 2026-11-03
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

The profile and recipe endpoints now say which field a refusal concerns, and account settings and the recipe wizard
mark only those fields with the kit's error line (`kcc-field--error`, `aria-invalid`, `aria-describedby` naming the
well).

## Changes

| File | Change | Notes |
|------|--------|-------|
| `src/KCC.Web/Features/Api/ProfileApiController.cs` | modified | `ProfileResponse.Code`: `name-required`, `password-required`, `wrong-password` (`PasswordMismatch`), `weak-password` (only Identity's six password-policy codes); anything else sends no code |
| `src/KCC.Web/Features/Api/RecipeApiController.cs` | modified | the 400 for a name over 255 carries `code: nameTooLong` and `field: recipeName \| variantName` through a `NameRefusal` record |
| `src/KCC.Web/Features/Pages/Account/Settings/useSettingsForms.ts` | added | `useProfileForm`, `usePasswordForm`: the save logic out of the view, with `fieldsInError` from the code |
| `src/KCC.Web/Features/Pages/Account/Settings/AccountSettingsView.Component.vue` | modified | binds `error` and `aria-describedby` per field; each form's well gains an id |
| `src/KCC.Web/Features/Components/RecipeWizard/wizardErrors.ts` | modified | `refusedName(failure)` reads the field from a `nameTooLong` 400 |
| `src/KCC.Web/Features/Components/RecipeWizard/recipeWizardFlow.ts` | modified | `refuseName` opens the step, set to return to review, and keeps `{ field, value }`; `isRefused` holds only while the name still equals that value |
| `src/KCC.Web/Features/Components/RecipeWizard/RecipeWizard.vue` | modified | routes a refused name to `refuseName`; every other failure keeps the review well |
| `src/KCC.Web/Features/Components/RecipeWizard/{BasicsStep,VariantStep}.vue` | modified | `nameRefused` prop; the name `Field` takes the message and its slot's `error` and `describedby` |
| `src/KCC.Web/Features/Components/RecipeWizard/stepSheet.ts` | modified | `describedBy(...)` joins the error id and the mark id |
| `src/KCC.Web/Features/Components/Forms/Field.vue` | modified | the error well sits directly under the control, above the `#after` mark |
| `src/KCC.Web/Features/Components/RecipeWizard/recipeWizardKeys.ts`, `uSync/v17/Dictionary/recipewizard.nametoolong.config` | added | `RecipeWizard.NameTooLong` |
| `docs/brand/kit.md` | modified | Status names the settings and wizard callers, and the well's place above a mark |

## Tests Added or Updated

- `tests/KCC.IntegrationTests/Features/Api/ProfileApiTests.cs` — each code, including a blank new password.
- `tests/KCC.IntegrationTests/Features/Api/SubmissionTests.cs` — `nameTooLong` with the field, for a recipe name, a
  first variant name and an added variant's name.
- `tests/KCC.ViteTests/Features/Pages/Account/Settings/useSettingsForms.test.ts` — each code marks exactly its fields;
  account refusals mark none; marks clear at the next save.
- `tests/KCC.ViteTests/Features/Pages/Account/Settings/AccountSettingsView.test.ts` — no mark before a refusal.
- `tests/KCC.ViteTests/Features/Components/RecipeWizard/{wizardErrors,recipeWizardFlow,BasicsStep,VariantStep}.test.ts`
  — the field read, the return to the step, the mark lifting when the name changes by any path, and the rendered mark
  with its well.
- `tests/KCC.ViteTests/Features/Components/Forms/Field.test.ts` — the error well before the `#after` slot.
- `tests/KCC.E2ETests/Features/Members/AccountSettingsErrorTests.cs` — blank first name, wrong current password, and
  mismatched new passwords each mark only their fields.
- `tests/KCC.E2ETests/Features/Members/RecipeWizardReviewTests.cs` — a 256-character recipe name returns to Basics
  with the mark, and editing it lifts the mark.

## Local Verification

Definition of done (constitution):

| Row | Result |
|---|---|
| D1 `dotnet build KitchenCommandCenter.slnx -c Release` | pass, 0 warnings, 0 errors (after moving a private helper below the public actions for SA1202) |
| D2 `yarn build:all` | pass |
| D3 `yarn type-check`, `yarn lint`, `yarn format` | pass; lint reports 6 warnings, all in untouched `InputField`/`SelectField`/`TextAreaField` lines; format changed nothing |
| D4 `node tests/scripts/run.mjs` (no-op `open` on `PATH`) | run 2: .NET 1617/1619, vitest 2488 + 17 + 8 pass. The 2 failures were the new settings E2E test's own `New Password` locator (the label carries ` *`); fixed, and `AccountSettingsErrorTests` rerun alone passes 3/3. Run 1 failed the same class on Title Case names, also fixed. No other class failed in either run |
| D5 dictionary | `DictionaryTests.RecipeWizardGroup_ImportsEveryHandWrittenItem_WithItsValue` passes with the new item |
| D6 both ramps | the harness's site, signed in: settings with a blank first name and a wrong current password, and Basics with a refused name, shot in light and dark. The 2px ink line falls only on the marked fields, and each well reads ink on its tint |
| D7 brand | `brand-steward`: on brand, no violation. Its notes 1 and 2 are fixed here; note 3 (the 255 in the copy) is a follow-up |
| D8, D9 | N/A |
| D10 | `docs/brand/kit.md` Status updated |

Review: the bug-fix review loop runs on an open pull request, and this branch opens none (the orchestrating session
collects it onto one close-out branch). Instead, a `cavecrew-reviewer` agent reviewed the local diff of `e7f9bbbe`:

| Finding | Outcome |
|---|---|
| 🔴 every non-mismatch Identity error mapped to `weak-password` | fixed: policy codes only |
| 🟡 a read or a match could replace a refused name and keep its mark | fixed: the mark holds only while the name equals the refused value |
| 🟡 settings marks hold until the next submit, unlike the wizard | kept: the assessment settled "clear at the next submit, as login does" |
| ❓ a blank Confirm with a blank New marks New only | kept: the settled rule marks the blank fields of what was sent |

The fixes landed as one commit, `7ef9d7b5`. The CodeRabbit round and `speckit.review.run` on the pull request have
not run; they run once the close-out pull request opens.

## Deviations from Assessment

- `Field.vue` changed (not listed): the brand review found the `#after` mark between a field and its error well,
  against kit.md's "directly below". The well now sits first.
- The wizard keeps `{ field, value }` rather than clearing on edit, so every writer of the name (edit, read, match)
  lifts the mark.
- `weak-password` is sent only for Identity's password-policy codes; the assessment had "any other Identity error".

## Follow-ups

- `RecipeWizard.NameTooLong` types 255 by hand while the server reads `MaxNameLength`; a `{0}` filled from a value the
  server sends would keep them in step.
