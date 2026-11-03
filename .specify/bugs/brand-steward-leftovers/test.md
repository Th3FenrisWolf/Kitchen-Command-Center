# Bug Verification: Five brand findings that predate the specs that found them

- **Slug**: brand-steward-leftovers
- **Tested**: 2026-11-03
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

None of the five symptoms reproduces on the fix branch. Each reproduction was run against the seeded E2E site in
Chromium, through throwaway E2E classes that were deleted afterwards, and every suite passes.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction 1 (no reviews) | Open `/recipes/spicy-ramen-flight/miso-vegan` at 375 and 1280, both ramps, signed out | pass | An empty sheet with glyph, "No reviews yet." title and the hint replaces the bare line. Slips down the page run `hero 3F 1 6 2F 4 5 1F 2F 3F`: cook notes 4, the empty sheet 5, the first sibling 1, so no neighbours share a tear and turns alternate. Signed in is covered by `PileTests` and the unit tests, not by eye |
| Reproduction 2 (loading) | `/recipes/spicy-ramen-flight/chili-oil-shoyu` with the reviews request held after hydration, then released | pass | While held: one `aria-busy="true"` region, no "No reviews yet", no list. After release: 3 reviews, no `aria-busy` |
| Reproduction 3 (variants hint) | `/recipes/spicy-ramen-flight/` filtered to no match, both ramps | pass | Hint in Hazelnut body copy, no hand note |
| Reproduction 4 (range ring) | `/recipes/`, keyboard focus on each thumb, both ramps; forced colours; a mouse click | pass | Ink ring outside the paper ring on the focused thumb in both ramps, and on the max thumb in dark. Forced colours: a `CanvasText` ring on the focused thumb. A mouse click focuses without `:focus-visible`, so no ring. Chromium only; Firefox not run |
| Reproduction 5 (card count) | `/recipes/`, first rated card | pass | `<span class="kcc-num">3</span>`, computed font Sono |
| New / updated tests | `npx vitest run` in `src/KCC.Web` | pass | 149 files; 2,467 passed, 1 expected fail, 1 skipped |
| Regression suite | `node tests/scripts/run.mjs`, no-op `open` on `PATH` | pass | 4,107 passed, 0 failed, 1 skipped across 6 suites. E2E 439/439 on the first try; no class needed a rerun |
| Build | `dotnet build KitchenCommandCenter.slnx -c Release`; `yarn build:all` | pass | 0 warnings |
| Lint / type-check | `yarn type-check`, `yarn lint`, `yarn format` in `src/KCC.Web` | pass | Lint: 0 errors, 6 warnings already present in untouched form fields |

## Output Excerpts

```
✅ 4107 passed · 0 failed · 1 skipped · 6 suites
during: 1 false 0
after: 0 false
<span aria-hidden="true">· </span><span class="kcc-num">3</span> | Sono, ui-monospace, …
```

## Residual Risks

- Firefox was not run. Its `::-moz-range-thumb` rules mirror the WebKit ones and are pinned by `kitStates.test.ts`.
- Under forced colours an unfocused range thumb paints nothing. This predates the fix and is listed in fix.md.
- The dark ramp's ring meets the marker fill at about 1.2:1 where the thumb sits on the fill. The paper ring inside it
  keeps the edge visible, as the screenshot showed.

## Recommendation

Close the bug once CI passes on the close-out pull request — verified end-to-end on the seeded site, in both ramps.
