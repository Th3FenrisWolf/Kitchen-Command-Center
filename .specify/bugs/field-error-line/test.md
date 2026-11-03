# Bug Verification: Written fields outside login never take the error line

- **Slug**: field-error-line
- **Tested**: 2026-11-03
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

The assessment's three reproductions now mark the fields at fault, in a real browser against the E2E harness's
site: a blank first name, a wrong current password and mismatched new passwords on account settings, and a
256-character recipe name in the wizard. Login's marks and every other suite are unchanged.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction 1, 2 (post-fix) | `dotnet run --project tests/KCC.E2ETests -- --treenode-filter "/*/*/AccountSettingsErrorTests/*"` | pass, 3/3 | blank first name marks First Name only; wrong current marks Current Password only; mismatch marks New and Confirm; each names its well |
| Reproduction 3 (post-fix) | same, `RecipeWizardReviewTests` | pass, 3/3 | submit returns to Basics, Back to review offered, name marked with the 255 message, mark lifts on edit |
| Login regression | same, `MemberFlowTests` | pass, 4/4 | wrong credentials still mark name and password |
| Server codes | `dotnet run -c Release --project tests/KCC.IntegrationTests -- --treenode-filter` for `ProfileApiTests`, `SubmissionTests`, `AccountApiTests`, `DictionaryTests` | pass, 12, 13, 13, 5 | `wrong-password` from Umbraco's `PasswordMismatch`, `weak-password` from a too-short password, `nameTooLong` with its field, the new dictionary item imported |
| Vite suite | `npx vitest run` in `src/KCC.Web` | pass | 150 files, 2488 passed, 1 expected fail, 1 skipped |
| Full suite | `node tests/scripts/run.mjs`, no-op `open` on `PATH` | pass but for the new class's own locators | .NET 1617/1619 on run 2; the 2 were `AccountSettingsErrorTests` locators, fixed and rerun alone above |
| Build, lint, type-check | `dotnet build -c Release`, `yarn build:all`, `yarn type-check`, `yarn lint`, `yarn format` | pass | 0 warnings; lint's 6 warnings are in untouched lines |
| Both ramps | throwaway E2E screenshots, light and dark, not committed | pass | 2px ink line only on marked fields; wells ink on tint |

## Output Excerpts

```
== AccountSettingsErrorTests   failed: 0  succeeded: 3
== RecipeWizardReviewTests     failed: 0  succeeded: 3
== MemberFlowTests             failed: 0  succeeded: 4
== ProfileApiTests             failed: 0  succeeded: 12
 Test Files  150 passed (150)
      Tests  2488 passed | 1 expected fail | 1 skipped (2490)
```

## Residual Risks

- A new password the policy refuses is exercised by the integration test (`weak-password`) and the composable test
  (marks New Password only), not in a browser; its browser path is the one the wrong-password E2E covers.
- An Identity refusal outside the six policy codes and `PasswordMismatch` sends no code and marks nothing; no test can
  provoke one through Umbraco's member manager.
- The two other bug sessions ran suites on this machine at the same time; no class failed for that reason.

## Recommendation

Close the bug — verified end-to-end for each reproduction the assessment lists.
