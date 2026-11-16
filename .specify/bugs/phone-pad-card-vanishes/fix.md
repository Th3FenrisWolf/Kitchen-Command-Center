# Bug Fix: The phone pad cards vanish once they finish opening

- **Slug**: phone-pad-card-vanishes
- **Fixed**: 2026-11-12
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

The sticky header no longer clips sideways on its own, since `#app` already clips at the same edges. A peeking or
open pad card keeps its own compositing layer with `will-change: transform`, the state in which the iPhone recording
shows it drawn. Both changes are CSS only. Chromium draws every page as before. Only the owner's iPhone can show that
WebKit now keeps the card drawn.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `src/KCC.Web/Features/Components/Header/AppHeader.Component.vue` | modified | `.pad-header` drops `overflow-x: clip`, with a comment saying why it stays off |
| `src/KCC.Web/Features/Components/Header/PadCard.vue` | modified | `.pad-card.is-peek` and `.pad-card.is-open` take `will-change: transform`; the resting `.pad-card` takes none |
| `docs/brand/kit.md` | modified | Invariant 14 says `.pad-header` takes no overflow clip of its own, and why; the Enforcement table lists the two new tests |
| `tests/KCC.ViteTests/Features/Components/Header/AppHeader.test.ts` | added test | The sticky header declares no overflow of its own |
| `tests/KCC.ViteTests/Features/Components/Header/PadCards.test.ts` | added test | A peeking or open card asks for its own layer, and a card at rest does not |

## Diff Highlights

```css
.pad-header {
  padding: 16px 0 14px;   /* overflow-x: clip removed */
  position: sticky;
  ...
}

.pad-card.is-open {
  ...
  visibility: visible;
  will-change: transform;   /* also on .pad-card.is-peek */
}
```

## Tests Added or Updated

- `AppHeader.test.ts` › `AppHeader` › `leaves the sideways clip to #app, so the cards hanging below the sticky header
  are drawn`: `.pad-header` is sticky and declares no `overflow`, `overflow-x` or `overflow-y`. It failed before
  the fix: `overflow-x` was `clip`.
- `PadCards.test.ts` › `A shown card` › `keeps its own layer while it peeks or is open, and asks for none at rest`.
  It failed before the fix: `will-change` was undefined on `.pad-card.is-peek`.
- The existing `pile.test.ts` already pins `#app`'s `overflow-x: clip`, which now holds the sideways clip alone.
  `washField.test.ts` still forbids `will-change` on `.kcc-wash`, and it passes.

## Local Verification

- Red first: `npx vitest run AppHeader.test.ts PadCards.test.ts` in `src/KCC.Web` → 2 failed (the two new tests),
  76 passed.
- Green: the same two files and `Features/Styles` → every test passes except two style files, which can't load
  without Font Awesome Pro (see D4).
- Manual checks: none. The container has no WebKit and no .NET SDK, so the site could not run and the iPhone fault
  could not be reproduced. The owner's iPhone check is in `/speckit-bug-test`.

### Definition of done

| Row | Result | Notes |
|---|---|---|
| D1 `dotnet build` | not run | This container has no .NET SDK. CI runs it. |
| D2 `yarn build:all` | not run | Needs `@fortawesome/fontawesome-pro` from the private registry, and `FONTAWESOME_NPM_AUTH_TOKEN` is not set here. Dependencies were installed without that one package for the tests below. CI runs it. |
| D3 type-check, lint, format | pass | `yarn type-check` exit 0. `yarn lint` exit 0, with 0 errors and 6 warnings in `InputField.vue`, `SelectField.vue` and `TextAreaField.vue`, which this change doesn't touch. `yarn format` changed nothing. |
| D4 all six suites | partial | Web Vitest: 150 of 153 files pass (2,597 tests). `serverEntry.test.ts`, `iconSubset.test.ts` and `mainCssIconStyles.test.ts` fail to load because `@fortawesome/fontawesome-pro` isn't installed (`Cannot find module '@fortawesome/fontawesome-pro/css/fontawesome.css'`). Admin Vitest: 17/17. Contributions Vitest: 8/8. The unit, integration and E2E TUnit suites did not run because there's no .NET SDK. CI runs all six. |
| D5 generated files | N/A | Nothing generated changed. |
| D6 both ramps in the Browser pane | not run | There's no running site (no .NET SDK). The fault shows only in WebKit, so the owner's iPhone check in both ramps stands in for this row. |
| D7 brand-steward | pass | No violation. The agent confirmed that `#app` clips at the same edges as the header did (the `.pad-cards` box's 24px overhang at 375px is still cut at the screen edge), and that the card's slip already has a transform, so `will-change` adds no containing block or stacking context. It suggested two doc fixes, both applied: name `.pad-header` in invariant 14, and list the new tests in the Enforcement table. It also ran the enforcement Vitest files: 19 of 20 pass, and `mainCssIconStyles.test.ts` fails to load without Font Awesome Pro. Not run: the 375px sideways-scroll walk in `tests/KCC.E2ETests/Config/Piles.cs`, which needs .NET and a browser. |
| D8 deploy lint | N/A | Nothing under `deploy/` changed. |
| D9 images | N/A | Nothing under `deploy/` changed, and no Docker file changed. |
| D10 docs | pass | `docs/brand/kit.md` invariant 14 gains the header's no-clip rule, and the Enforcement table names the tests that pin it. No other line in `README.md`, `CLAUDE.md` or `docs/` names the header's clip or a card's layer. |

### Review loop

`speckit.review.run` ran on pull request #139. Code repo: this repository. Base: `45e904b`. Context:
`.specify/memory/constitution.md` and `CLAUDE.md`. Checks: the definition-of-done rows above that this container can
run, which are the header Vitest files, `yarn type-check`, `yarn lint` and `yarn format`. The verifier's real findings
were one-line comment edits, so they were applied directly rather than through a worker. The header tests (146/146)
ran after each edit, and type-check, lint and format passed after the last.

| Round | Status | Finding | Verdict | Outcome |
|---|---|---|---|---|
| 1 | completed | R1-1 correctness, `PadCard.vue:93`: the off-breakpoint variants take `will-change` when open | not-real | Only after a resize across 768px while a card is open. The card is hidden and still open, so neither the comment nor the kit row is broken. |
| 1 | | R1-2 rules, `AppHeader.Component.vue:406`: the comment narrates the edit (VII) | real | fixed: present-tense hazard |
| 1 | | R1-3 rules, `PadCard.vue:75`: the comment narrates the fault and restates the code (VII) | real | fixed: one present-tense reason |
| 1 | | R1-4 rules, `AppHeader.test.ts:182`: the comment narrates and cites the bug (VII) | real | fixed: comment removed, since the test name gives the reason |
| 1 | | R1-5 rules, `PadCards.test.ts:329`: the comment narrates and cites the bug (VII) | real | fixed: comment removed |
| 1 | | R1-6 rules, `kit.md:401`: the invariant states a hedged cause and names the bug | not-real | VII's comment rule covers code files. Under D10 the hedged line is true. `/speckit-bug-test` rewords it if the iPhone check disproves the cause. |
| 2 | completed | none | | clean agent loop |
| Final | absent | CodeRabbit | | `coderabbit` is not on `PATH` in this container. Install it with `brew install --cask coderabbit`; other platforms at <https://docs.coderabbit.ai/cli>. |

## Deviations from Assessment

None.

## Follow-ups

- The owner checks on an iPhone (Safari) that Menu and the search card stay drawn after they slide in, in both
  ramps, and that their rows take taps. Desktop Safari's cards are worth a look at the same time.
- If the cards still vanish, compare the two changes on a phone to find which one matters, and look next at the phone
  sheet's scroll container with its `clip-path` (`.kcc-sheet--scroll`).
