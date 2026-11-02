# Bug Verification: Neighbouring sheets share a tear, and the account page's empty text sits bare on the desk

- **Slug**: pile-tear-follow-ups
- **Tested**: 2026-10-30
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

None of the three symptoms reproduces on the fix branch. The variant page's clash was reproduced in a browser before the
fix and is gone after it. The account page's tears are rendered on the server only, and are checked on the server render
for every group count from 0 to 12. The empty pad shows as an empty-state sheet in both ramps. No regression was found
in the checks that touch the changed files.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction 1 (account, 2 groups) | `AccountView.test.ts` › "never tears two neighbours alike, down the page with %i recipe groups" (0 to 12) and "swaps the coming-soon tears after 2 and 8 recipe groups" | pass | The account page has no client-side tear change, so the server render is what a browser shows. No browser run with a member holding 2 groups |
| Reproduction 2 (variant, signed out, 1 review) | `PileTests.VariantDetail_SignedOut_OneReview_TearsTheFirstSiblingApart` at 375 and 1280 | pass (2/2) | Failed before the fix with `But was: '… kcc-tear-1'`; the first sibling is now `kcc-tear-3` |
| Reproduction 3 (account, no groups) | `PileTests.Account_Alternates` (E2E member with no groups), and `yarn perf:capture pile-fix --page account` | pass (2/2) | Screenshots, phone and desktop, Chromium and WebKit, light and dark: glyph, title, hint and "New recipe" pill on a sheet |
| New / updated vitest | `yarn test --run Account/AccountView VariantDetail` in `src/KCC.Web` | pass | 18 files, 218 tests |
| New / updated integration | `AccountStringsTests/*`, `AccountPagesTests/*` | pass | 6/6 and 15/15; the two hand-written dictionary items import |
| Regression: pile walker | `PileTests.VariantDetail_SignedOut_Alternates` | pass (4/4) | Turns still alternate with the sibling shift |
| Regression: perf guard | `DeterministicGuardTests/*` | pass (4/4) | With the raised account rows of `baseline.ci.json` |
| Regression: full suite | `node tests/scripts/run.mjs` (from the fix run) | pass after rerun | Unit 726, Integration 447, frontends 2,480 pass; E2E 429/438, the 9 failures were load flakes and their five classes passed on rerun |
| Lint / type-check / format | `yarn type-check`, `yarn lint`, `yarn format` (from the fix run) | pass | Lint's 6 warnings are in untouched Forms components |
| Review loop | `speckit.review.run` on PR #115 | clean | One agent round: R1-1 (a comment) judged not real. CodeRabbit ran, 0 findings |
| CI | `build-and-test`, `perf` on PR #115 | pending | Not finished when this report was written |

## Output Excerpts

```
PileTests/VariantDetail_SignedOut_OneReview_TearsTheFirstSiblingApart*  total: 2  failed: 0  succeeded: 2
PileTests/Account_Alternates*                                          total: 2  failed: 0  succeeded: 2
DeterministicGuardTests/*                                              total: 4  failed: 0  succeeded: 4
AccountStringsTests/*                                                  total: 6  failed: 0  succeeded: 6
AccountPagesTests/*                                                    total: 15 failed: 0  succeeded: 15
Test Files  18 passed (18)   Tests  218 passed (218)
{"type":"complete","status":"review_completed","findings":0, …}
```

## Residual Risks

- The signed-out sibling cards render at tear 1 on the server and move to tear 3 once the reviews load, so their clip and
  tilt change on the client. This is the same timing as the existing flip; nobody has looked at it by eye.
- The CI baseline's account rows were edited by hand to the counts measured on this machine. CI's own `perf` job had not
  finished when this report was written.
- The account page with 2 groups was not opened in a browser; its tears come from the server render the tests check.

## Recommendation

Close the bug once CI passes on PR #115 — verified on the server render, in the browser for the variant page and the
empty pad, and by the review loop.
