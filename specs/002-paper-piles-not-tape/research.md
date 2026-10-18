# Research: Paper Piles, Not Tape

Each entry gives the decision, the evidence it rests on, and the alternatives. Paths are relative to the repository
root. "The preview" is `specs/002-paper-piles-not-tape/mockups/pile.html`, approved on 2026-10-16.

## R1. Where the blank sheet lives

**Decision**: The blank is the **last child** of the top sheet's `.kcc-slip`, absolutely positioned over the slip
(`inset: 0`), `pointer-events: none`, `z-index: -1`, turned and scaled about its centre.

**Rationale**:

- `.kcc-slip` always has a `transform` (`rotate(calc(var(--r, 0) * 1deg))` in `Torn/Kit.css`), so every slip is a
  stacking context. A `z-index: -1` child paints inside that context, under the slip's content and above whatever is
  behind the slip. The blank cannot fall behind the desk or a neighbour.
- Inside the slip, the blank inherits the slip's tilt, so "turned 1.5° off the top sheet's tilt" (FR-003) is a plain
  `rotate()` on the blank, and a crisp slip (`--r: 0`) leaves the blank its full turn (FR-005) with no special case.
- It takes the slip's box, so the slip's box must be the sheet's box. A `kcc-slip--fill` card that stretches to its
  grid row (PR #96) stretches its sheet, and its blank, with it (edge case 1, FR-004). A slip without `--fill` passes no
  height to its `.kcc-torn` (`Torn/Kit.css`), so a grid that stretches it leaves the sheet short and the slip tall.
  Two grids do that today: the ingredients and instructions grid in `VariantDetailView.Component.vue` (no
  `items-start`) and the four settings sheets in `AccountSettingsView.Component.vue` (`grid lg:grid-cols-2`). Short
  ingredients beside long instructions would hang the ingredients' blank far below its sheet. The kit therefore gives
  `.kcc-slip` `height: fit-content`, and `.kcc-slip--fill` keeps `height: 100%` after it. A grid or flex container
  stretches only an `auto` height, so a slip without `--fill` stays its sheet's height wherever it sits, now and in any
  future grid (A23). The visible sheet does not change; the slip's rotation origin moves to the sheet's centre, a shift
  of a few pixels on the two stretched grids.
- It travels with the slip: a sticky `StackerBlock.cshtml` sheet carries its blank, and the `Stacker.Component.vue`
  step back (`[.stuck]:scale-95` on the slip) scales the blank with it (FR-011) with no extra rule.
- Last, not first: `tests/KCC.E2ETests/Config/GridRows.cs` reads `slip.querySelector('.kcc-sheet')`, and several Vitest
  files match the first sheet in a slip. Appending the blank keeps every first-match locator on the content sheet.

**Alternatives considered**:

- A wrapper pile element that holds the blank and the slip side by side, as the preview drew it (`.pv-pile`). It needs
  the top's tilt passed in by hand (`--pv-top`), because `--r` is registered non-inheriting (`Torn/Tokens.css`). It
  adds a grid item between every grid and its cards, which breaks `GridRows.cs` (`:scope > .kcc-slip`) and the
  `HomeSectionsPageTests` markup strings, and a stacker step back would need a second rule.
- A `::before` pseudo-element. The tear needs `clip-path` and the shadow needs a `filter` outside the clip (kit
  invariant 8), so one box cannot draw both.
- For the stretched grids, `items-start` on the two grids found today. It fixes them but leaves the next stretched
  grid to a reviewer; `fit-content` on the slip fixes the class of bug.

## R2. The blank's markup

**Decision**: `<span class="kcc-blank" aria-hidden="true"><span></span></span>`. `.kcc-blank` carries the fibre and
fall filter (declared once, in the same rule as `.kcc-torn`), the placement and the turn. `.kcc-blank > span` carries
`display: block`, `background-color: var(--color-paper)`, `clip-path: var(--tear)` and `height: 100%`, and no ruling.
The outer span is blockified by `position: absolute`; the inner one is not, and an empty inline span has no box, so
`display: block` is load-bearing and `pile.test.ts` asserts it.

**Rationale**: FR-002 wants paper torn on four sides with the fibre and the fall, and no ruling. Two elements are the
minimum for a filter outside a clip. Leaving `kcc-torn` and `kcc-sheet` off the blank keeps every hook that a test
locates a sheet by pointing at content only (FR-015): 42 Vitest files and 4 E2E files match `kcc-sheet`, `kcc-torn` or
`kcc-slip` today (`grep -rln` over `tests/`). A `<span>` is valid inside every slip element in use (`div`, `section`,
`article`, `li`, `form`).

**Alternatives considered**: Reuse `kcc-torn` and `kcc-sheet` with the ruling switched off, as the preview did. It
would double the count of `kcc-sheet` in every SSR test and catch the blank in any `.kcc-sheet` query.

## R3. The blank's tear and its turn direction

**Decision**: A blank's direction is the product of three factors:
`rotate(calc(var(--blank-turn) * var(--blank-dir) * var(--blank-flip) * var(--blank-region, 1)))`.

- `--blank-dir` comes from the top sheet's tear preset. `tears.ts` gives each sheet preset a `blank: { tear, dir }`,
  and `tearsCss()` writes it into the preset's class as `--blank-tear: var(--tear-N)` and `--blank-dir: ±1`.
  `.kcc-blank` sets `--tear: var(--blank-tear)` for its paper. The six standard presets turn opposite to their own
  tilt. The two hero presets have no tilt rule; their direction is set: `tear-hero` turns −1, as the preview drew
  it, and `tear-hero-2` turns +1.
- `--blank-flip` is a per-slip factor: `.kcc-slip` sets `1`, `.kcc-slip--flip` sets `-1`, and `KccSheet`'s `flip`
  prop adds the class. It is set on the slip itself, so it never leaks into a nested slip.
- `--blank-region` is an inherited factor a container sets, unset (1) by default. A page sets it with a Tailwind
  arbitrary property (`max-lg:[--blank-region:-1]`) on a run of sheets whose neighbour changes with the breakpoint.

| Top preset | Tilt `--r` | Blank tear | `--blank-dir` |
|---|---|---|---|
| `tear-1` | +0.30 | `tear-4` | −1 |
| `tear-2` | −0.98 | `tear-5` | +1 |
| `tear-3` | +0.05 | `tear-6` | −1 |
| `tear-4` | −0.52 | `tear-1` | +1 |
| `tear-5` | +0.63 | `tear-2` | −1 |
| `tear-6` | −0.70 | `tear-3` | +1 |
| `tear-hero` | −0.58 | `tear-hero-2` | −1 (set) |
| `tear-hero-2` | (R4) | `tear-hero` | +1 (set) |

**Rationale**:

- Three along the cycle always differs from the top's own tear (FR-003). In a row of up to three cards, the three tops
  and three blanks use all six tears. The preview paired the same tears.
- The six standard tilts alternate in sign around the cycle, and lists take `(index % 6) + 1` (kit.md, Tears), so
  inside any list the generated direction alternates with nothing written per sheet.
- Across a page it does not. FR-003 has no list restriction and A18 defines neighbours in reading order, so every page
  must alternate from its first sheet to its last. The red team traced same-direction runs on six pages (recipe
  search, recipe detail, variant detail, account, settings, the wizard). Some neighbours are fixed sheets, which a
  flip settles; others change with state (a spotlight card, the review count, the group count, the wizard step) or with
  the breakpoint (the recipe filters are hidden on a phone until opened). R12 sets every page's factors.
- Factors rather than re-picked tears: a flip changes only the blank, and leaves every top sheet's approved tear and
  tilt as it is.

**Alternatives considered**:

- `KccSheet` props and Razor attributes for the blank's tear and direction on every sheet. Every author would pick
  two more values and the rule could only be checked surface by surface.
- Re-pick each page's tears so their tilt signs alternate. It changes approved top sheets, and it cannot follow a
  breakpoint, because the tear class is markup.
- Alternate only inside lists and amend FR-003. The spec stays as written.

## R4. The hero's blank tear

**Decision**: Add `tear-hero-2` to `SHEET_TEARS`: 160 points a side, amp 2.4, a seed of its own, the chamfer at a
corner other than the hero's top right, and a tilt within ±1°. `yarn tears` writes it. The hero's blank and the pad's
page use it.

**Rationale**: FR-003 needs the hero's blank edge to differ from the hero's. kit.md gives each family a size band:
standard tears for sheets 170 to 700px wide, the hero tear for 330 to 1300px. The hero (`DetailHero`, about 702px in
the preview and up to 1280px at full content width), `FeaturedRecipeCard` and the pad's page (1232px) are in the
hero band. The preview's `kcc-tear-3` under the hero, and `kcc-tear-2` under the pad, stretch a standard wavelength
past its band. The mockup README lists this as still open.

**Alternatives considered**: Reuse `kcc-tear-3`, as the preview did. No generator change, but a wide blank shows a
stretched tear, the scallop kit.md warns about.

**Test impact**: `tears.test.ts` "has six standard sheets, a hero and three tiles" filters standard sheets by
`name !== 'tear-hero'`. It is rewritten first to expect six standard sheets and two hero sheets.

## R5. The wide modifier

**Decision**: `--blank-turn: 1.5deg` and `--blank-scale: 1.015` are set on `.kcc-slip` in `Torn/Kit.css`, and
`.kcc-slip--wide` sets `0.5deg` and `1.005`. `KccSheet` adds `kcc-slip--wide` when `tear === 'hero'` or its new `wide`
prop is set. `FeaturedRecipeCard.vue`, `DetailHero.vue` and `RecipeCardRow.vue` write it on their hand-written slips.
The pad's page carries it. The owner ruled on 2026-10-16 (plan D-6) that every sheet wider than 700px is wide.

**Rationale**: The mockup README names a "wide" modifier on the pad's page and the hero, with plain custom
properties, and no container query or typed arithmetic. Setting the defaults on `.kcc-slip` rather than `:root` stops a
standard slip nested in a wide one from inheriting the wide pair. Every hero-torn sheet is a wide sheet by the kit's
own size band, so tying the modifier to the hero tear in `KccSheet` cannot be forgotten on a new hero.

**Which sheets are wide (D-6, owner)**: the pad's page, the hero, and every sheet wider than 700px. At 1.5° a 1248px
sheet tips out about 16px, which is why the pad's page was made wide. A width is read at a 1280px viewport, the
preview's measuring width, where the content column is 1248px (`--content-max-width: 80rem` and a 16px
`--padding-inline` in `Styles/Layout.css`). A surface marked wide keeps the modifier at every viewport, because the
preview ruled out container queries; on a narrower screen it just tips out less (A19). Widths from the layout code:

| Surface | Width at 1280px | Wide |
|---|---|---|
| `DetailHero`, `SmallHero` | 1248px | yes (hero tear) |
| `FeaturedRecipeCard` | 976px (results column) | yes (hero tear) |
| the pad's page | 1232px (breakout) | yes |
| `RecipeSearchHeader`, "At a glance" (`RecipeDetailView`, `VariantDetailView`), `VariantNutrition`, `VariantCookNotes`, the `VariantReviews` summary, reviews and form, the wizard step sheets and `SubmittedSheet`, `VariantsEmptyState`, `RecipeCardRow` in `VariantList` | 1248px | yes |
| `RecipeCardRow` in the search list, `RecipesEmptyState` | 976px (1248 − 244 filters − 28 gap) | yes |
| `AccountView` recipe groups | 836px (`content-aside` second column) | yes |
| `PhotoViewer` | 1024px (`max-w-5xl`) | yes |
| `VariantInstructions` | 732px (3fr of the 2fr/3fr grid) | yes |
| `CookMode` | 768px (`max-w-3xl`) | yes |
| `StackerBlock.cshtml` sheet, `Stacker` cards, `AccountSettingsView` sheets, home `Card`s | about 610px (two columns at `lg`) or less | no |
| `RichTextBlock`, registration complete, error | 672px (`max-w-2xl`) | no |
| `VariantIngredients` | 488px | no |
| `RecipeCard` in every grid, the add-variant slip | about 300 to 400px | no |
| `LoginView`, `LeaveConfirm` | 448px (`max-w-md`) | no |
| `AccountView` profile sheet, `ComingSoonSection` | 384px (aside column) | no |
| `RecipeFilters` | 244px | no |

These widths are read from the layout code. They were not measured in a browser; D6 checks them by eye.

**Alternatives considered**: The generator writes the wide pair into the hero tear classes. It mixes the pile's
numbers into the tear table and gives no way to mark a wide standard sheet. A container query that switches at 700px
would track the width at every viewport, but the approved preview ruled container queries out.

## R6. The lift

**Decision**: A kit class, `kcc-slip--lift`:

- `.kcc-slip--lift > :not(.kcc-blank)` transitions `translate` over `var(--default-transition-duration)`, and also
  `outline-color 0.2s` and `outline-offset 0.2s`. The lift rule sits in `@layer components` and would otherwise
  replace the `@layer base` focus-ring fade that `TailwindConfig.css` gives every `a` and `button` (lines 73 to 84):
  the slip's direct child is the card's link (`a.kcc-torn` in `RecipeCard.vue`, `FeaturedRecipeCard.vue`,
  `VariantGrid.vue` and the split `RecipeCardRow.vue`). Today the transition sits on the slip, which is not focusable.
- Under `@media (prefers-reduced-motion: no-preference)`, `.kcc-slip--lift:is(:hover, :focus-within) >
  :not(.kcc-blank)` takes `translate: 0 -4px`.

It replaces `transition-transform focus-within:-translate-y-1 hover:-translate-y-1` on `RecipeCard.vue`,
`RecipeCardRow.vue`, `FeaturedRecipeCard.vue` and the add-variant slip in `VariantGrid.vue`.

**Rationale**: FR-010 wants only the top sheet to move. Moving the slip's children rather than the slip leaves the
blank where it was. The `translate` property composes with the tilewrap's own `transform: rotate(-2deg)`, so the tile
and its tape lift with the sheet. The 4px and the 300ms default match kit.md's Motion table. The pointer target does
not change: the blank takes no pointer events.

**Reduced motion (A12)**: kit.md Motion rule 4 shortens transitions to 0.01ms under reduced motion, so today a card
still jumps 4px. US4 scenario 3 says nothing moves. Declaring the lift only under `no-preference` meets both FR-012 and
the scenario.

**Alternatives considered**: Keep lifting the slip and counter-translate the blank by 4px with the same transition.
One rule fewer, but two opposed motions that must stay in step, and the counter-move runs in the slip's rotated frame.

## R7. Tape

**Decision**:

- `KccSheet.vue` loses the `tape` prop (FR-006: the kit offers no way to tape a sheet).
- The sheet strips go from `FeaturedRecipeCard.vue` and `DetailHero.vue` (their second `kcc-tape`), from
  `Pages/Account/RegistrationComplete/Index.cshtml`, and with the prop from `SubmittedSheet.vue`,
  `RecipesEmptyState.vue`, `VariantsEmptyState.vue` and `SmallHero.Component.vue`.
- `Torn/Kit.css` folds the base `.kcc-tape` rule and `.kcc-tilewrap .kcc-tape` into one `.kcc-tilewrap > .kcc-tape`
  rule (and its dark-ramp rule), so a tape outside a tilewrap has no look at all.
- A new source scan in `pile.test.ts` fails on any `kcc-tape` in `Features/**/*.vue` or `*.cshtml` whose parent is not
  a `kcc-tilewrap`, and on any `.kcc-tape` selector in `Torn/*.css` outside `.kcc-tilewrap`.
- The `tape` colour token stays: the tile strip uses it.
- The list row's tile is an applied item too. `RecipeCardRow.vue` puts its `AccentTile` inside the sheet, in the
  row's flex flow, with no tilewrap and no strip. It gains an in-flow tilewrap, `kcc-tilewrap kcc-tilewrap--flow`
  (`position: relative`, no offset and no turn, so the row keeps its square layout), holding the tile and its strip
  (FR-007, SC-002). The scan accepts it, since its parent is a `kcc-tilewrap` (A25). It stays inside the sheet, where
  the tile already sits inside the clip and the filter today: placed beside the `.kcc-torn` it would leave the row's
  flex flow, fight `--pad` and need its own lift. That is one narrow exception to the rule that a tilewrap and its tape
  sit outside the clip and the filter, so the four places that state the rule are amended in the same change (R11).
- The recipe wizard's photo tiles (`PhotoStrip.vue`: torn photos on the desk above the step) are tiles, not sheets:
  no blank and no tape (A20).

**Rationale**: `grep -rn kcc-tape` over `src/KCC.Web/Features` finds exactly these places. The tile strips in
`RecipeCard`, `FeaturedRecipeCard` and `DetailHero` stay (FR-007). The kit's enforcement is source-scan tests like
`retiredTokens.test.ts`, so tape gets one too.

**Alternatives considered**: Keep `.kcc-tape` styled anywhere and rely on brand review. FR-006 asks that the kit offer
no way, and a review cannot fail a build.

## R8. The render cost (SC-005)

**Decision**: Measure, do not assume. Quickstart step 5 runs a throwaway Playwright script, not committed, against the
locally running site with the recipe seeder's 25 recipes, on `main` and on the branch:

- 1280 by 800, Chromium, CPU throttled 4× through CDP (`Emulation.setCPUThrottlingRate`), light ramp, then dark.
- A scripted scroll of `/recipes/` from top to bottom and back in fixed wheel steps, then a hover across every card in
  the first row.
- Frame times from `requestAnimationFrame` deltas and `long-animation-frame` entries, 5 runs each.
- Per run: the p95 frame time and the count of frames over 50ms. Per branch and ramp: the median of the five runs.
- Pass: the branch's median p95 is within 10% of `main`'s, and its median count of frames over 50ms is at most
  `main`'s plus one. Medians of paired counts, not a single stray frame, decide the result.

The numbers go in the pull request description.

**Rationale**: The blank doubles the `drop-shadow` filters and the clip paths on a grid. Scrolling static filtered
content is mostly raster reuse, but a hover lift repaints the filtered top sheet, and the tilted, scaled blanks add
overdraw. A CI timing test would be flaky (CI latency is not reproducible locally). A local A/B run on one machine
answers the spec's question.

**If it fails**: the fix trades look against cost, for example dropping the 9px fall from the blank and keeping the
fibre, which breaks FR-002. That is a brand decision for the owner, raised only if the measurement fails.

**Alternatives considered**: A Lighthouse run (it measures load, not scrolling), or a CI performance test (flaky).

## R9. Test-first coverage

**Decision**:

| What | Test, written first |
|---|---|
| Blank mapping, `tear-hero-2`, generated output | `tests/KCC.ViteTests/Features/Torn/tears.test.ts`: every sheet preset writes `--blank-tear` and `--blank-dir`; the blank tear is never its own; the six standard directions are opposite their tilt and alternate around the cycle; `tear-hero` is −1 and `tear-hero-2` +1; heroes map to each other |
| Kit CSS | New `tests/KCC.ViteTests/Features/Styles/pile.test.ts`: `.kcc-slip` has `height: fit-content` and `.kcc-slip--fill` `100%` after it; `.kcc-blank` sets `z-index: -1`, `pointer-events: none`, and turns by the product of `--blank-turn`, `--blank-dir`, `--blank-flip` and `--blank-region`; `.kcc-blank > span` is `display: block`; `.kcc-slip` sets `--blank-flip: 1` and `.kcc-slip--flip` `-1`; `.kcc-slip--wide` sets the 0.5° and 1.005 pair; the lift excludes `.kcc-blank`, sits under `no-preference`, and keeps `outline-color 0.2s, outline-offset 0.2s` in its transition; `#app` clips `overflow-x`; tape only in a tilewrap (source scan); every Razor `kcc-slip` holds a `kcc-blank` (source scan) |
| Kit table | `kitClasses.test.ts` stays green: `kcc-blank`, `kcc-slip--wide`, `kcc-slip--lift`, `kcc-slip--flip` and `kcc-tilewrap--flow` go into the kit.md Classes table in the same change |
| Motion | `motion.test.ts` stays green: the lift names the default duration |
| `KccSheet` | `KccSheet.test.ts`: one blank, last in the slip; none with `piled: false`; `kcc-slip--wide` on `tear: 'hero'` and on `wide: true`, and not otherwise; `kcc-slip--flip` on `flip: true`; no tape markup and no `tape` prop |
| Vue surfaces | The existing SSR test of each surface asserts one `kcc-blank` per sheet, no sheet tape, `kcc-slip--wide` exactly on the wide surfaces of R5, and the flips of R12: `RecipeCard`, `RecipeCardRow` (its tile in a `kcc-tilewrap--flow` with its strip), `FeaturedRecipeCard`, `DetailHero`, `VariantGrid`, `VariantList`, `VariantSiblings`, `RecipesEmptyState`, `VariantsEmptyState`, `SmallHero`, `Card`, `Stacker`, `VariantReviews`, `AccountView`, `AccountSettingsView`, `RecipeDetailView`, `VariantDetailView` (the siblings' flip in all four review states of R12: signed in or out, with reviews or none), and the step and `SubmittedSheet` assertions in `CreateRecipeView` and `AddVariantView`. `PhotoStrip.test.ts` asserts no blank and no tape |
| The pad | `AppHeader.test.ts`: one `pad-page`; the stack test's tear extractor changes from `/\bkcc-tear-\w+/`, which stops at a hyphen and reads `kcc-tear-hero-2` as `kcc-tear-hero`, to `/\bkcc-tear-[\w-]+/`; it then counts 5 tears on a desktop and 4 on a phone, all different; the style block gives the top, the cards and the page z-indices 5, 4 and 3; `PadCards.test.ts`: a pad card has no blank |
| Razor and Home | `tests/KCC.UnitTests/Features/Pages/Home/HomeSectionsTests.cs` (the cycle skips two tears past a stacker's, and its signs alternate), `HomeSectionsPageTests` (stacker and rich-text sheets hold a blank; the tear cycle's signs alternate through a stacker's skip), `AccountPagesTests` (registration complete: a blank, no tape), `ErrorPageTests` (a blank) |
| Teleported overlays | `CookMode.vue`, `PhotoViewer.vue` and `LeaveConfirm.vue` render inside `<Teleport v-if="isMounted" to="body">`, so an SSR string test sees nothing. Their assertions (one blank; for cook mode and the photo viewer also `kcc-slip--wide` and crisp) go first into the E2E files that already open them: `tests/KCC.E2ETests/Features/VariantDetail/CookModeTests.cs`, the photo-viewer flows in `tests/KCC.E2ETests/Features/Members/RecipeImportTests.cs` and `RecipeWizardReviewTests.cs` (`data-testid="photo-viewer"`), and the leave dialog in `tests/KCC.E2ETests/Features/Members/RecipeImportLeaveTests.cs` (`data-testid="leave-dialog"`) |
| In a browser | New `tests/KCC.E2ETests/Features/Chrome/PileTests.cs`, with a shared walker in `tests/KCC.E2ETests/Config/Piles.cs`. On each page type and state the E2E site can produce (recipe search in grid and list view, with a spotlight from a query that matches one rated recipe and without one, with the filters open and closed on a phone; recipe detail for a recipe with variants, with the featured variant, and with a toolbar search that matches none so the featured card sits over the empty state; recipe detail for "Bare Cupboard Snack Board", the seeded recipe with no variants (`RecipeSeedData.cs`), so no featured card and the empty state alone; variant detail signed in and out, and, signed in, before and after "load more", with `/api/variant/*/reviews` stubbed through `Page.RouteAsync` to return two pages of reviews, as `RecipeImportTests` and `RecipeImportLeaveTests` already stub routes, because the seed has at most three reviews a recipe and a page is ten; account; account settings; the wizard's first and second steps; Home; registration complete; the error page) at 375px and at 1280px, the walker reads every visible `.kcc-blank` in `main` in document order and checks: its turn's sign, from the computed transform matrix, alternates with the one before (FR-003, R12); its offset box equals its slip's `.kcc-torn` offset box (FR-004, R1); and the page has no horizontal scroll (`scrollWidth <= clientWidth`, R10). Hovering and focusing a recipe card moves its `.kcc-torn` up and leaves its `.kcc-blank` box where it was (FR-010, SC-004); with reduced motion emulated, nothing moves. The pad shows one `.pad-page`, and the computed z-indices of `.pad-top`, `.pad-cards` and `.pad-page` are 5, 4 and 3 in the header's stacking context (FR-009, SC-003). Paint order is read from computed styles, not `elementFromPoint`: the page and the blank take no pointer events, so a hit test cannot see them |

**Rationale**: Constitution IV: every behaviour change starts with a failing test, and new tests locate by
`data-testid`, role or a kit class. SSR string tests are how this repository tests Vue markup. Alternation across a
page, the blank's box, the overhang, the lift and the stacking only exist in a browser, so one walker checks them on
every page type. Visual fidelity in both ramps is D6, by eye, against the two preview JPEGs.

**Alternatives considered**: An E2E test per surface. Slow, and the walker covers every page with one helper.

## R10. The blank's overhang

**Decision**: `#app` (`Features/Styles/Main.css`) takes `overflow-x: clip`, and the walker of R9 checks at 375px and
1280px that no page scrolls sideways (A24).

**Evidence**: The horizontal overhang of a blank past its sheet's side is
`s·(w/2·cos t + h/2·sin t) − w/2`, for scale `s`, turn `t`, width `w` and height `h`. It grows with the height. The
mockup README's tip-out table measures the vertical lift of a corner (`w/2·sin t`), not this. A crisp 343px sheet on a
375px phone at 1.5° passes the 16px gutter once it is about 1015px tall (the open `RecipeFilters`, a long
`VariantIngredients` at its −1.2° net turn past about 1264px), and a 610px settings sheet at 1.5° passes it at about
877px. `grep -rn overflow` over `Features/**/*.{vue,cshtml,css}` finds only two containers that clip sideways:
`Pages/Home/Index.cshtml` (`overflow-x-clip`) and `.pad-header` (`overflow-x: clip`). `Layout.cshtml` and `#app` clip
nothing, so the overhang would scroll the page.

`overflow: clip`, unlike `hidden`, makes no scroll container, so the sticky header, the sticky filters and the sticky
stacker keep working, as they do under Home's `overflow-x-clip`. The clip cuts the blank only past the viewport's
edge, where it could not be seen anyway. Teleported overlays sit in `body`, outside `#app`, and a fixed box adds no
scrollable overflow.

**Alternatives considered**: A smaller turn on tall sheets. It needs the sheet's height in CSS, which means a
container query or script, both ruled out (mockup README, kit invariant 6).

## R11. Docs that the change makes untrue (D10)

| File | What changes |
|---|---|
| `docs/brand/loose-leaf.md` | "a strip of tape where it was held down" (line 13), principle 1 "sometimes taped" (line 18), Handling "sheets tilt within ±1° and may take one strip of tape" (lines 107 to 108: top sheets tilt within ±1°, and the blank under each turns 1.5°, or 0.5° on a wide sheet, off it), "Six sheet tears, one hero tear and three tile tears" (line 112: two hero tears), the Tape bullet (line 118): paper is piled two high, sheets are never taped, tape holds applied items only |
| `docs/brand/kit.md` | The Structure examples gain the blank and lose sheet tape; rule 2 adds the blank; Classes: `kcc-blank`, `kcc-slip--wide`, `kcc-slip--lift`, `kcc-slip--flip`, `kcc-tilewrap--flow`, and `kcc-tape` becomes "inside a tilewrap only"; the slip is `height: fit-content`; the direction factors and the page rule of R12; `#app` clips sideways overflow; Tears gains `tear-hero-2` and the blank mapping; Motion's lift row names `kcc-slip--lift`; the `KccSheet` props drop `tape` and add `piled` and `wide`; the rule for wide sheets (the pad's page, the hero, any sheet wider than 700px); invariant 8 |
| `.claude/skills/loose-leaf/SKILL.md` | Principle 1, the structure snippet, the `KccSheet` props, the checklist line on tape (line 65, "label and tape outside both", gains the `.kcc-tilewrap--flow` exception) |
| `.claude/agents/brand-steward.md` | Line 36 ("`.kcc-label`, `.kcc-tape` or `.kcc-tilewrap` placed inside `.kcc-torn` or inside `.kcc-sheet`" is a violation) gains the exception: a `.kcc-tilewrap--flow` with its tape may sit in a sheet's content flow. Also: tape only inside a tilewrap; a sheet with no blank, or a taped sheet, is a violation |
| `CLAUDE.md` | The Tear invariants at lines 202 to 203: the label, the tape and the tilewrap stay siblings of `.kcc-torn` outside the clip and the filter, except a `.kcc-tilewrap--flow`, which sits in the sheet's content flow with its tape; and the blank is the slip's last child, outside the top sheet's clip and filter |
| `docs/brand/kit.md`, the exception | Structure rule 2 (lines 128 to 129) and invariant 8 (line 336) state the same exception for `.kcc-tilewrap--flow`. With the brand-steward and `CLAUDE.md` rows, these are the four places that state the rule; they are edited during implementation, with the row's change |
| `src/KCC.Web/Features/Utilities/BrandColor.ts` | The comment at line 4, "SHEET_TEARS (minus the hero)", becomes "minus the two hero tears" once `tear-hero-2` joins `SHEET_TEARS` |
| Test comments | `CreateRecipeView.test.ts`, `AccountSettingsView.test.ts`, `AddVariantView.test.ts`: "the sheet and its tape sit flush" loses "and its tape" |

## R12. Alternation on every page

**Decision**: Each page sets the flips below so that its sheets alternate in reading order: the visible sheets inside
`main`, in document order, at the current viewport (A21). The header pad floats over every page and is not a
neighbour. An overlay (cook mode, the photo viewer, the leave confirm) is the only sheet on its screen. Directions
below are the blank's sign: `−` or `+`. "Flip" is `kcc-slip--flip` (or `KccSheet flip`) on that slip.

| Page | Sequence and factors |
|---|---|
| Recipe search | Header (tear-3, −). Filters (tear-5, −): flip, so +. Spotlight `FeaturedRecipeCard` (hero, −). Cards and rows (`listTearFor(i)`, − + − …): `RecipeCard` and `RecipeCardRow` take `flip` when a spotlight shows, so the list starts +. `RecipesEmptyState` (tear-2, +), shown only with no spotlight and no results: flip, so −. On a phone the filters are hidden until opened (`hidden` unless `sheetOpen`, `lg:block`): the results `<section>` takes `max-lg:[--blank-region:-1]` while `!sheetOpen`, which turns every sheet after the header over, so the run still alternates from the header |
| Recipe detail | `DetailHero` (−). "At a glance" (tear-3, −): flip, so +. Featured variant (hero, −), optional. `VariantGrid` and `VariantList` take `flip` when the featured card shows (the add-variant slip with them). `VariantsEmptyState` (tear-4, +): flip when no featured card shows |
| Variant detail | `DetailHero` (−). "At a glance" (tear-3, −): flip, +. `VariantIngredients` (tear-1, −). `VariantInstructions` (tear-6, +). `VariantNutrition` (tear-2, +): flip, −. `VariantCookNotes` (tear-4, +). Reviews summary (tear-5, −), shown when the count is above 0. Each review (`listTearFor(i)`, starting −): flip all, so the list starts + after the summary. The review form (crisp, shown when signed in): its flip opposes the sheet before it (the last review, else the summary, else cook notes, +). One pure function, `reviewsTrailingSign({ isAuthenticated, count, loaded })` in `Components/VariantDetail/`, returns the sign of the review section's last sheet, or `null` when it renders none. `VariantReviews` uses it for the form's flip. `VariantDetailView` calls it with the props it already holds (`isAuthenticated`, `reviewCount`) and `loaded: 0`, and flips `VariantSiblings` (`listTearFor(i)`, starting −) to oppose the result, or cook notes (+) when it is `null`. The server renders that state, with no reviews, because reviews load only on the client (`onMounted(() => load(0))` in `VariantReviews.vue`): signed out with reviews, the summary (−) is last, so the siblings flip to +; signed out with none, cook notes (+), no flip; signed in with reviews, summary − then form +, no flip; signed in with none, form −, flip. `load(0)` runs on every mount, so on a variant with reviews the list fills after hydration. After each client `load()`, the mount's included, `VariantReviews` emits the new trailing sign and the page re-computes the siblings' flip, so the siblings can re-flip after hydration. The server HTML alternates for the state it renders, and the emit runs only after mount, so hydration still matches (A27) |
| Account | Profile (tear-1, −). Recipe groups (`listTearFor(i + 1)`, starting +). Coming soon "Favorites" (tear-3, −): flip when the group count is even, including 0. Coming soon "Recent activity" (tear-5, −): flip when the group count is odd |
| Account settings | `SmallHero` (−). Profile (tear-2, +). Password (tear-5, −). Appearance (tear-1, −): flip, +. Sign out (tear-6, +): flip, − |
| Recipe wizard (create, add variant) | `SmallHero` (−). The step sheet (`listTearFor(step − 1)`) and `ReadingState`: flip when the tear is odd, so every step is +. `SubmittedSheet` (tear-2, +). The photo tiles are not sheets (A20) |
| Home | One tear cycle (`HomeSections.cs`) runs through sheets and cards, so signs alternate while it runs in order. When it avoids a stacker's tear it now skips two tears, not one, so the next draw keeps the opposite sign (A26). `HomeSectionsPageTests` asserts the cycle's signs alternate |
| Login, registration complete, error | One sheet each |

The checks are split by what each test can produce. Vitest runs with `environment: 'node'` and renders through
`renderSsr`, where `onMounted` never runs, so the SSR tests of each page and list assert its flips only for the states
the server renders: both parities of the recipe group count, the four review states with no reviews loaded, the wizard's
first step, a spotlight or none, a featured variant or none, and the phone's filters closed. Three pure helpers carry the
logic behind the rest, each with its own Vitest: `reviewsTrailingSign`, `stepFlips` for the wizard step, and
`resultsRegionFlips` for the region factor from `sheetOpen`. The states only a browser reaches (loaded reviews and "load
more", the phone's filters open, the list views, a toolbar search that leaves the featured card over the empty state,
wizard steps past the first, and the siblings' re-flip once reviews load) are driven first by E2E tests, written in the
phase that implements each: `VariantReviewsTests` and the walker's states in `PileTests`, which waits for
`[data-testid="reviews-list"]` before it walks a variant page with reviews. The walker runs at 375px and 1280px and
fails on any two neighbours that turn the same way (A30).

**Rationale**: The red team's trace (round 1, flaw 2) and the templates read for this table
(`RecipeSearchView.Component.vue`, `RecipeDetailView.Component.vue`, `VariantDetailView.Component.vue`,
`VariantReviews.vue`, `AccountView.Component.vue`, `AccountSettingsView.Component.vue`, `RecipeWizard.vue`,
`HomeSections.cs`). A variable-length run between two fixed sheets cannot alternate at both ends with fixed values,
so the sheet after a run computes its flip from the run's length or its last sign.

**Alternatives considered**: A render-order counter shared through `provide`/`inject`. It cannot see a sheet that CSS
hides at a breakpoint, and the Razor sheets on Home are outside Vue's render.
