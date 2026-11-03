# Bug Assessment: Written fields outside login never take the error line

- **Slug**: field-error-line
- **Created**: 2026-11-03
- **Source**: pasted text (from the orchestrating close-out session)
- **Verdict**: valid
- **Severity**: low

## Report (verbatim or summarized)

> Spec 003 (paper rule scheme) gave the kit's written fields an error line on the rule. Login sets it, marking only
> the fields at fault through `AuthResponse.Code = "invalid-credentials"`. The recipe wizard's fields and the account
> settings fields can show the error line, but no caller sets their `error`, so a validation failure there never
> shows on the field.

## Symptom

A refused save on the account settings page or in the recipe wizard shows a danger well, but no field takes
`kcc-field--error` or `aria-invalid`, so neither the eye nor a screen reader learns which field is at fault. In the
wizard the one per-field refusal, a name over 255 characters, also loses its meaning: the review step shows
"We couldn't submit this. Please try again.", and trying again fails the same way.

Expected (kit.md, Status): a field the message is about takes `kcc-field--error` and `aria-invalid`, with an
`aria-describedby` naming the well; an error about the account stands in the well alone.

## Reproduction

1. Sign in, open Account settings, clear First name, Save changes. The well reads "Please enter your name."; neither
   name field is marked.
2. Change password with a wrong current password, or a new password the policy refuses, or two new passwords that do
   not match. The well shows the message; no password field is marked.
3. Open the recipe wizard, type a 256-character recipe name, go on to review, submit. The review well reads
   "We couldn't submit this. Please try again."; the name field is on another step and is never marked.

## Suspected Code Paths

- `src/KCC.Web/Features/Pages/Account/Settings/AccountSettingsView.Component.vue` — `saveProfile` and
  `changePassword` keep one `{ ok, text }` message per form and bind no `error` on any `InputField`; the wells have
  no id for a field to point at.
- `src/KCC.Web/Features/Api/ProfileApiController.cs` — `ProfileResponse(bool Success, string[] Errors)` carries no
  code, so the client cannot tell a wrong current password from a refused new one, or a missing name from a 401.
- `src/KCC.Web/Features/Api/RecipeApiController.cs:NameError` — the 400 carries only an English `error`, no code and
  no field.
- `src/KCC.Web/Features/Components/RecipeWizard/wizardErrors.ts:submitErrorKey` — maps 400 to `SubmitFailed`
  whatever the reason.
- `src/KCC.Web/Features/Components/RecipeWizard/RecipeWizard.vue:submit` — keeps the failure as a review-step key
  only.
- `src/KCC.Web/Features/Components/RecipeWizard/BasicsStep.vue`, `VariantStep.vue` — the name `InputField`s sit
  inside `Field` but bind neither `error` nor the slot's `error`.
- `src/KCC.Web/Features/Pages/Account/Login/useLoginForm.ts` — the working pattern: `fieldsInError` from the
  response `code`, client mismatch marks both passwords.

Not in scope: the wizard's other fields. `PROCEED_RULES` keeps Next disabled until the required values are filled,
and no other value is refused by the server, so they have no failure to show. The reader already caps names at 255
(`ReadResultNormalizer.NameLimit`).

## Root Cause Hypothesis

Spec 003 added the `error` prop and wired only login, the one caller whose server answer said which fields were at
fault. The profile and recipe endpoints answer with prose only, so their callers had nothing to mark from.
Confidence: high.

## Proposed Remediation

**Preferred** (owner decisions, round 1):

- **Profile** — `ProfileResponse` gains `Code`, as `AuthResponse` has: `name-required`, `password-required`,
  `wrong-password` (Identity's `PasswordMismatch`), `weak-password` (any other Identity error from the change).
  The settings view marks:
  - `name-required`: only the blank name field(s), as sent.
  - `password-required`: only the blank password field(s) of current and new, as sent.
  - `wrong-password`: Current password.
  - `weak-password`: New password only.
  - client mismatch: New and Confirm, as login does.
  - anything else (401, 429, network): the well alone.
  The message stays in the form's one well, which gains an id; marked fields name it with `aria-describedby`. Marks
  clear at the next submit, as login's do.
- **Recipe wizard** — the 400 for a name over 255 characters carries `code: 'nameTooLong'` and
  `field: 'recipeName' | 'variantName'`. On that answer the wizard returns to Basics or Variant with Back to review
  offered, marks the name field through `Field`'s `error` (its own well under the field, from a new resource string),
  and clears the mark when that name is edited. Every other failure keeps today's review well.

**Alternatives** (declined): `maxlength="255"` on the names (cuts a paste silently, and leaves the wizard with no
field failure to show); marking every field a message could concern; moving the settings message under each field.

**Files likely to change**:
- `src/KCC.Web/Features/Api/ProfileApiController.cs`
- `src/KCC.Web/Features/Api/RecipeApiController.cs`
- `src/KCC.Web/Features/Pages/Account/Settings/AccountSettingsView.Component.vue` (and a small composable beside it)
- `src/KCC.Web/Features/Components/RecipeWizard/{RecipeWizard.vue,recipeWizardFlow.ts,wizardErrors.ts,BasicsStep.vue,VariantStep.vue}`
- `src/KCC.Web/uSync/v17/Dictionary/` — one new `RecipeWizard` string
- `docs/brand/kit.md` — the Status paragraph names the new callers

**Tests to add or update**:
- `tests/KCC.IntegrationTests/Features/Api/ProfileApiTests.cs` — codes for missing name, missing password, wrong
  current, too short.
- Recipe API integration test — `nameTooLong` code and field for recipe, first variant and variant.
- `tests/KCC.ViteTests/Features/Pages/Account/Settings/` — each code marks exactly its fields, describedby names the
  well, the well alone otherwise.
- `tests/KCC.ViteTests/Features/Components/RecipeWizard/` — `wizardErrors`, flow (return to step, clear on edit),
  Basics and Variant steps render the mark and its message.

## Risks & Considerations

- `ProfileResponse` and the recipe 400 gain fields only; existing clients ignore them.
- Identity's error codes are framework strings; `PasswordMismatch` is the one the change relies on, pinned by the
  integration test.
- Visual: the error line is the kit's existing `kcc-field--error`; check both ramps on a marked field.

## Open Questions

None. Round 1 settled the four decisions above.
