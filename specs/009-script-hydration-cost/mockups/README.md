# Approved look round: render work (T056, T048a)

**Ruling** (owner, relayed 2026-11-18): "Keep look, approve E".

- **Kept**: the look as shipped. None of the candidates below ships.
- **Approved**: the failed-part error state with Retry, as built (`error-state.jpg`).

## Why keeping the look holds the budgets on a phone

The phone profile runs Chromium with a 4x CPU throttle on SwiftShader, a software rasteriser the throttle does not
reach. Measured on the same Mac with hardware raster (ANGLE Metal, confirmed in `chrome://gpu`) and the throttle kept,
the branch meets 100 ms with no look change:

| Press or tap (phone, 4x CPU) | Raster | Before (`c1827268`) | Branch |
| --- | --- | --- | --- |
| Wizard, worst press | SwiftShader | 608 ms | 376 ms |
| Wizard, worst Next | SwiftShader | 288 ms | 256 ms |
| Wizard, worst press | Hardware | 104 ms | 64 ms |
| Wizard, worst Next | Hardware | 72 ms | 48 ms |
| Search-3 bloomed tap | SwiftShader | 248-256 ms | 112-168 ms |
| Search-3 bloomed tap | Hardware | 96 ms | 0 ms (no entry over 16 ms) |

The SwiftShader gap is carried as the phone Chromium amendment in `spec.md` (FR-004, FR-005, FR-013, SC-001, SC-002),
and goes to Phase 3 or a later phase.

## Candidates shown and not shipped

Each was CSS injected over the built site. The step sheet is `main section.mt-rule-1 > .kcc-slip`.

| Id | Change | CSS | Gain, SwiftShader | Gain, hardware |
| --- | --- | --- | --- | --- |
| W1 | Wizard step sheet without its tear | `… > .kcc-torn > .kcc-sheet, … > .kcc-fall > span { clip-path: none }` | Next 0-56 ms | none |
| W2 | Wizard step sheet without its fall and blank filter | `… > .kcc-fall, … > .kcc-blank { filter: none }` | Next to 104-200 ms (outliers 256, 624) | none |
| W3 | Wizard step sheet without its washes | `… > .kcc-torn > .kcc-sheet > .kcc-washes { display: none }` | mixed | 0-16 ms |
| S1 | Every wash hidden on search | `.kcc-washes { display: none }` | search-3 tap to 64-88 ms | none |

No wizard candidate brought a SwiftShader Next press within 100 ms.

## The approved error state

`error-state.jpg` shows it for a wizard step, the submitted sheet and cook mode, light and dark, phone and desktop. It
appears only when a later part's script fails to load three times. It reuses the danger well, the kit button and
dictionary text (`RecipeWizard.UnexpectedError`, `RecipeWizard.TryAgain`, `VariantDetail.UnexpectedError`,
`VariantDetail.TryAgain`) on the part's own sheet, with no new class or token.

## The reserved search band (D19, C3)

**Ruling** (owner, relayed 2026-11-18): "Band is fine". It ships as built.

After the page is usable, the browser holds the space the next page of results needs below the screen, so a landing
moves nothing a reader can see. In a full-page capture that space shows as empty desk under the last cards
(`search-band-light.jpg`, `search-band-dark.jpg`: before at `c1827268` on the left, the branch right after usable on
the right). It is at most one page of results (the next page's rows at the grid's columns), it is cleared once no
further pages remain, and it is added in the browser after mount only, so the server's HTML and the scripts-off page
never show it. `perf:diff` flags search-1 at 38-44% for this page height alone; every card pixel is unchanged, and
search-3 differs 0%.
