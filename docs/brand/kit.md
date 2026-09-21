# Torn & Waxed — the kit

The engineering contract for [torn-and-waxed.md](torn-and-waxed.md): tokens, classes, structure, invariants,
and the test that enforces each rule. Anything Razor also renders lives in global `@layer components` CSS,
never in a component `<style>` block.

## Files

| Path | Holds |
|---|---|
| `src/KCC.Web/Features/Styles/TailwindConfig.css` | `@theme static` role tokens (light values), radius ladder, fonts, safelist |
| `src/KCC.Web/Features/Styles/Torn/Tokens.css` | kit-only properties; the dark ramp under `:root[data-theme='dark']` |
| `src/KCC.Web/Features/Styles/Torn/Tears.css` | **generated** tear presets (`yarn tears`). Never hand-edited |
| `src/KCC.Web/Features/Styles/Torn/Kit.css` | desk, slip / torn / sheet / wash / label / tape / tile, type, chrome, ramp swap |
| `src/KCC.Web/Features/Styles/Torn/Controls.css` | btn, seg, field, range slider, badge, check, stats, steps, recipe-slip parts |
| `src/KCC.Web/Features/Styles/Typography.css` | the 15 / 24 base, APCasual on bare `h1`–`h6`, the 16px control floor, every `@font-face` including Sono |
| `src/KCC.Web/Features/Styles/Layout.css` | the `.content-grid` breakout system: content, breakout and full-width columns for pages and sections |
| `src/KCC.Web/Features/Styles/Sections/MultipleColumnSection.css` | the page-builder column grid for `MultipleColumnSection`, in `@layer components` because Razor renders it |
| `src/KCC.Web/Features/Styles/Main.css` | the stylesheet import graph; every `Torn/*.css` file is imported here and `#app` carries the desk colour so the overlays have a backdrop to blend with |
| `src/KCC.Web/Features/Types/DesignSystem.ts` | `WASHES` and the `Wash` type; the colour axes the safelist test checks |
| `src/KCC.Web/Features/Torn/tornPolygon.ts` | pure tear generator |
| `src/KCC.Web/Features/Torn/tears.ts` | preset table and CSS emitter |
| `src/KCC.Web/Features/Torn/generateTears.ts` | CLI that writes `Tears.css` |
| `src/KCC.Web/Features/Components/Sheet/KccSheet.vue` | the sheet primitive for Vue |
| `src/KCC.Web/Features/Pages/Shared/Layout.cshtml` | pre-paint ramp script, font preloads, `#kcc-wax` / `#kcc-wax-flat` filter defs |

## Tokens

Reach tokens through Tailwind utilities (`bg-paper`, `text-ink-soft`) or `var(--color-*)` in kit CSS. Never
a literal colour. Light is the `@theme` value; dark overrides live in `Torn/Tokens.css`.

| Token | Light | Dark | Use |
|---|---|---|---|
| `desk`, `desk-2` | `.884 .016 288`, `.836 .018 288` | `.132 .024 288`, `.176 .028 288` | page ground, raised desk |
| `paper`, `paper-2` | `.958 .009 92`, `.972 .008 92` | `.258 .030 288`, `.222 .028 288` | sheets; paper-2 for inset panels inside a sheet and the neutral `kcc-well` |
| `ink`, `ink-soft` | `.232 .026 288`, `.432 .024 288` (test-pinned; mockup .452) | `.945 .012 92`, `.735 .018 288` | text; kicks, meta, placeholders |
| `hair`, `hair-strong` | ink / .16, ink / .56 (test-pinned; mockup .4) | chalk / .18, chalk / .45 | dividers; control hairlines and underlines |
| `rule` | `.58 .045 288 / .22` | `.88 .02 288 / .13` | pencil ruling |
| `marker`, `marker-ink` | `.876 .112 126`, `.24 .03 288` | same | the accent fill and the ink that sits on it and on every wash |
| `fiber`, `fall` | `1 .006 92`, `.25 .03 288 / .28` | `.435 .034 288`, `.04 .02 288 / .7` | the two drop-shadows on `.kcc-torn` |
| `tape` | `.985 .012 92 / .6` | `.9 .012 92 / .28` | tape strips |
| `focus` | `var(--color-ink)` | same | focus ring |
| `peach yellow green teal sky lavender pink red` | see the identity | same | the eight washes; fills only |

All values are `oklch(L C H [/ alpha])`.

Kit-only properties (`Torn/Tokens.css`, not Tailwind tokens): `--bl` 24px, `--rd` 6px (labels, wells,
textareas), `--wash-blend` (multiply / screen), `--wash-op` (.62 / .26, dark value test-pinned),
`--wash-sat` (1 / 1.45), `--grain-op` (.08 / .16), `--wax-op` (.3 / .36), `--grain`, `--crayon`. Per
element: `--pad` (sheet), `--tear` and `--r` (set by a preset class), `--c --x --y --w --h` (wash).

**Retired** and caught by `retiredTokens.test.ts`: `ink-line`, `ink-on-wash`, `hatch`, `edge`,
`edge-strong`, `flap-1`, `flap-2`, `link`, `danger`, `success`, `warning`, `danger-ink`, `success-ink`,
`warning-ink`, `rating-ink`, the washes `rosewater flamingo mauve maroon sapphire blue`, every `shadow-*`
utility, `rounded-lg` and larger, `font-bold`, `font-semibold`, `font-medium`, the `fa-primary-*` /
`fa-secondary-*` Tailwind utilities (the kit sets the Font Awesome custom properties itself; see Icons),
every `sk-*` class, `v-ink`, `data-ink`. Nothing keeps them alive: the Softbound tokens, the ink module and
the `TRANSITIONAL` blocks are gone, and the test's `ALLOWLIST` is empty. A new entry there needs a reason in
the commit message.

**Radius ladder:** `rounded-xs` 3px (checkbox), `rounded-sm` 4px (small marks), `rounded-md` 6px (labels,
wells, textareas), `rounded-full` (pills). Sheets, tiles and images take no radius.

## Type

`Features/Styles/Typography.css` is the base: `body` 15 / 24 in Hazelnut regular; `h1` 40 / 48 in APCasual;
`h2` 24 / 24; `h3`–`h6` 22 / 24. Heading *level* is semantic and heading *size* comes from `kcc-h3` /
`kcc-h4`, so a bare `h4` is never a thing in new markup — write `<h4 class="kcc-h4">` or a `kcc-kick`.
Form controls (`input`, `select`, `textarea`) are 16px everywhere: iOS zooms the viewport into any focused
field below 16px, and the kit's fields inherit this floor. Sono is a static instance (MONO 1 / 400):
`font-variation-settings` is a no-op on it. `typography.test.ts` pins these numbers and checks every font
file it references exists. Nothing the kit renders sets a weight, but editor rich text may still carry
`<strong>`, so `Typography.css` keeps the Hazelnut Bold faces declared for it.

## Structure

A labelled, taped, washed sheet:

```html
<div class="kcc-slip kcc-tear-3">                 <!-- tilt via --r; position: relative -->
  <div class="kcc-torn">                          <!-- filter: fibre + fall -->
    <div class="kcc-sheet" style="--pad: 48px">   <!-- paper, clip-path: var(--tear), 24px pencil rule -->
      <span class="kcc-wash" style="--c: var(--color-peach); --x: 88%; --y: 18%; --w: 38%; --h: 60%" aria-hidden="true"></span>
      <p class="kcc-kick">Kitchen Command Center</p>
      <h2 class="kcc-h3">The perfect drawing, torn out and kept.</h2>
    </div>
  </div>
  <span class="kcc-label"><i class="fa-duotone fa-scissors" aria-hidden="true"></i>Identity · 01</span>
  <span class="kcc-tape" aria-hidden="true"></span>
</div>
```

A plain sheet (no label, no tape):

```html
<div class="kcc-slip kcc-torn kcc-tear-2">
  <div class="kcc-sheet">…</div>
</div>
```

A recipe slip:

```html
<article class="kcc-slip kcc-recipe kcc-tear-5">
  <div class="kcc-torn">
    <div class="kcc-sheet">
      <span class="kcc-stat"><i class="fa-duotone fa-star" aria-hidden="true"></i><span class="kcc-num">4.5</span><span class="text-ink-soft">· 28</span></span>
      <h3 class="kcc-h4">Brown Butter Gnocchi</h3>
      <p class="kcc-meta"><span>Pasta</span><span>6 var.</span><span class="kcc-num">25 min</span></p>
      <div class="kcc-badges mt-6"><span class="kcc-badge">Vegetarian</span><span class="kcc-badge">One pan</span></div>
    </div>
  </div>
  <div class="kcc-tilewrap">
    <div class="kcc-torn"><div class="kcc-tile kcc-tear-tile-2" style="--c: var(--color-peach)" aria-hidden="true"><i class="fa-duotone fa-wheat"></i></div></div>
    <span class="kcc-tape" aria-hidden="true"></span>
  </div>
</article>
```

When the whole slip is one link, the link **is** the `.kcc-torn` wrapper (`<a class="kcc-torn block">`): it
sits outside the clip, so the tear never shaves its focus ring, and the click target is the whole sheet. The
tilewrap stays a sibling of it and is pointer-transparent.

Rules of the structure:

1. `clip-path` sits on `.kcc-sheet` / `.kcc-tile`. The `filter` sits on a wrapper **outside** the clipped
   element (`.kcc-torn`), or the shadow is clipped away with it.
2. Label, tape and tilewrap are siblings of `.kcc-torn` inside `.kcc-slip`: outside the clip **and** outside
   the filter, so they are neither torn nor shadowed.
3. Every direct child of `.kcc-sheet` except `.kcc-wash` is lifted above the wash by the kit at class
   specificity (`:where`). A kit class that positions a sheet child itself is written `.kcc-sheet > .kcc-x`
   so it wins regardless of import order (see `kcc-stat`).
4. `--pad` on the sheet also positions the pencil rule: pass padding as `style="--pad: …"`, not as padding
   utilities. It is **one length**: the rule's offset is `calc(var(--pad) - 5px)`, and a shorthand
   (`24px 36px 24px 24px`) makes that invalid, so the ruling silently falls back to the top edge. Asymmetric
   room comes from a utility on the content, never from `--pad`.
5. One wash, one label, one hand note per sheet at most.
6. `clip-path` clips descendants too. Keep interactive children at least 4px inside the sheet edge (the
   default 24px padding does this), or the focus ring is shaved by the tear.
7. `transform` on `.kcc-slip` and `filter` on `.kcc-torn` each make the slip the containing block for
   `position: fixed` descendants. Fixed UI (dialogs, cook mode, full-screen panels) is teleported to `body`
   or kept outside sheets; never inside a slip.
8. `clip-path` clips everything inside the sheet, including a dropdown or a sticky toolbar that moves past
   the edge. Menus and popovers that must overflow live outside the sheet.
9. `--tear` inherits from the slip into the sheet (wanted) and into a pinned tile (not wanted, so
   `.kcc-tilewrap` resets it to the first tile tear); `--r` is registered non-inheriting, so nested slips
   never compound a tilt.
10. A slip placed directly in `main.content-grid` (or any grid) with `mx-auto max-w-*` needs `w-full` too:
    auto margins suppress a grid item's stretch, so without it the sheet shrink-wraps its copy and the
    wash's percentage placement drifts with content length.

## Tears

`Tears.css` declares `--tear-1 … --tear-6`, `--tear-hero` and `--tear-tile-1 … 3` on `:root` and the classes
that select them:

| Class | Points / side | Amp | Chamfer | Tilt `--r` |
|---|---|---|---|---|
| `kcc-tear-1` | 80 | 2.4 | 19px top-right | +0.30 |
| `kcc-tear-2` | 80 | 2.4 | 17px bottom-right | −0.98 |
| `kcc-tear-3` | 80 | 2.4 | none | +0.05 |
| `kcc-tear-4` | 80 | 2.4 | 18px bottom-left | −0.52 |
| `kcc-tear-5` | 80 | 2.4 | 20px bottom-right | +0.63 |
| `kcc-tear-6` | 80 | 2.4 | 17px bottom-left | −0.70 |
| `kcc-tear-hero` | 160 | 2.4 | 28px top-right | −0.58 |
| `kcc-tear-tile-1..3` | 24 | 1.8 | none | (tilewrap −2°) |

Depth is constant but the wavelength stretches with the box, so each family has a size band: tile tears for
48–128px tiles, the six sheet tears for sheets 170–700px wide, the hero tear for 330–1300px. A tile tear on a
full-width sheet reads as a scallop, not a tear.

**Assignment:** a list item takes `kcc-tear-${(index % 6) + 1}`; a standalone sheet takes a preset from a
stable hash of its id (see `Utilities/BrandColor.ts`) or the one the page design names; the default is
`kcc-tear-1`. Neighbours never share a tear. The kit ships one hero tear, so two hero sheets on one page
(RecipeDetail's hero and its featured variant) do share it; keep at least one standard sheet between them.
`yarn tears` regenerates the file; `tears.test.ts` fails if the committed file drifts from the generator. Need a crisp surface (a form, cook mode)? `KccSheet crisp` sets
`--r: 0` on the slip; Razor writes `style="--r: 0"` on the slip itself.

## Classes

| Class | What | Notes |
|---|---|---|
| `kcc-grain`, `kcc-crayon` | desk grain under everything, wax tooth over everything | first and last child of `#app`; `App.vue` owns them; the crayon covers everything inside #app, so a surface teleported to body (cook mode) is deliberately untextured |
| `kcc-slip` (+`--fill`) | tilt + positioning context; `--fill` stretches slip, torn and sheet to a definite-height parent | `--r` from the tear preset; without `--fill` a child sized in percentages computes to auto |
| `kcc-torn` | fibre + fall filter | wraps exactly one clipped element |
| `kcc-sheet` | paper, clip, ruling, padding | `--pad` default 24px, heroes 48px; compact rows (recipe rows, review slips, the account sheet) take 16px and accept a ruling 8px off a 24px neighbour's |
| `kcc-wash` | the pool of colour | `--c` a wash token, `--x --y` the centre, `--w --h` the radii (any length-percentage). Copy may sit on the 70% ring, never on the core: a corner pool caps the radius that points at the copy in px (`min(60%, 68px)` keeps the ring inside a 48px padding at any sheet height) |
| `kcc-label`, `kcc-label--right` | marker pill over the top-left (or right) edge | Sono caps, optional leading `<i>` |
| `kcc-pill` | the label's marker fill and ink on an element that stays in the flow | a current wizard step, an open menu button; type comes from `kcc-kick` on the same element |
| `kcc-tape` | one strip, top-centre | opt-in |
| `kcc-tilewrap` › `kcc-torn` › `kcc-tile` (+`--lg`) | pinned torn wax tile | `--c` the wash, glyph in `marker-ink`; pointer-transparent, so a link beneath it keeps the click |
| `kcc-kick`, `kcc-lbl` | Sono caps 10.5/24 in ink-soft | section kickers, field labels |
| `kcc-body` | 15/24 body | |
| `kcc-h3`, `kcc-h4` | APCasual 40/48, 22/24 | size and leading only; bare `h1`–`h3` already take APCasual from `Typography.css`, so these go on any element that needs display or heading size |
| `kcc-hand` | APCasual italic 17/24 ink-soft | one per sheet |
| `kcc-num` | Sono tabular | every number |
| `kcc-unit` | Sono caps 11px in ink-soft | a unit beside a number (min, g, servings); the same small caps a stats row gives its `<small>` |
| `kcc-hr` | dashed hair rule | |
| `kcc-link` | ink + hair-strong underline | prose links, breadcrumbs |
| `kcc-link--icon` | icon-only link (the home crumb), with `kcc-kick` on the item for size | body-size icon (15px) in a 24px min-height/min-width box, no underline; ink-soft, ink on hover |
| `kcc-secname` | section heading row: `h2` + `kcc-kick` with a dashed underline | |
| `kcc-foot` | footer copy block | |
| `kcc-btn` (+`--ghost`, `--ink`, `--text`, `--lg`, `--icon`) | marker pill 36px; hairline ghost; ink fill; underlined text; 48px large; `--icon` squares the pill (36px, 48px with `--lg`) | Sono caps `.14em`; an `--icon` button is all glyph, so its name goes in `aria-label` |
| `kcc-seg` › `button[aria-pressed\|aria-checked]` | pill group, pressed = ink fill | pressed = ink fill; an unpressed segment goes from ink-soft to ink on hover; labels never wrap; the group scrolls horizontally when narrower than its segments |
| `kcc-field` (+`--noicon`, `--area`) | 36px pill with inset hairline and leading icon; block variant for textareas | focus = 2px inset ink ring |
| `kcc-range` › `kcc-range-track`, `kcc-range-fill` | paper track with an inset hairline, marker fill, ink thumbs with a paper ring | both thumbs are native range inputs stacked over the track; the ring keeps the thumb off the marker fill |
| `kcc-badge`, `kcc-badges` | 22px hairline pill, Sono 10 caps; wrapping row | |
| `kcc-check` › `li` › `kcc-box` (+`--on`), text, `kcc-q`; `li.kcc-done` | checklist on the rule | the checkbox is `sr-only`, so a focused checkbox rings its box instead |
| `kcc-stats` › `div` › `kcc-lbl` + `kcc-v` | stat row, Sono 26 | `<small>` for the unit |
| `kcc-steps` › `li` › `kcc-n` + `kcc-body` | numbered method | numbers `01`, `02`, … |
| `kcc-well` (+`--danger`, `--success`, `--warning`) | paper tinted 28% with the wash, ink text, 6px radius | status messages; the message itself is a `kcc-kick` |
| `kcc-field--error` | the field's own fill tinted with red | pair with a `kcc-well--danger` message below the field |
| `kcc-recipe`, `kcc-stat`, `kcc-meta` | recipe slip modifiers | see Structure; the stat is `.kcc-sheet > .kcc-stat` |

Spacing between things uses Tailwind utilities on the 24px rule: `mt-6` (24px), `gap-9` (36px),
`gap-x-7` (28px), `mt-12` (48px), `gap-y-[72px]` for sections.

## Status

Status is never coloured text. It is a **well**: paper tinted 28% with the status wash
(`color-mix(in oklab, var(--color-red) 28%, var(--color-paper))`), carrying ink text.

```html
<p class="kcc-well kcc-well--danger kcc-kick" role="alert">Something we need is missing.</p>
```

`kcc-well--danger` (red), `kcc-well--success` (green), `kcc-well--warning` (yellow). A field in error adds
`kcc-field--error` to the field and puts the message in a `kcc-well--danger` directly below it. Icons inside a
well are duotone in ink like everywhere else. `contrast.test.ts` asserts ink on every well in both ramps.

## Icons

Font Awesome duotone by default; `fa-solid` / `fa-regular` where the simpler glyph reads better (empty rating
stars are `fa-regular`). `Torn/Kit.css` sets `--fa-primary-color` and `--fa-secondary-color` to `currentColor` and
`--fa-secondary-opacity` to `.35` globally (`.4` inside tiles, labels and marker buttons). Components never set
the Font Awesome custom properties and never use the `fa-primary-*` / `fa-secondary-*` utilities.

## Vue primitives

- `Components/Sheet/KccSheet.vue` — props `label` or a `#label` slot for rich content (a `<ResourceString>`
  keeps its editor hooks that way), `icon`, `labelRight`, `wash`, `at { x y w h }`, `tear` (`1..6 | 'hero'`),
  `tape`, `pad`, `crisp` (no tilt: forms, cook mode), `as` (`div | section | article | li`). Default slot.
  Plain `.vue`: import it locally. Renders the Structure above with no client JS.
- `Components/Button/Button.vue` — `variant: 'marker' | 'ghost' | 'ink' | 'text'`, `size: 'md' | 'lg'`,
  `as: 'button' | 'a'`. Emits the `kcc-btn` classes; no directive.
- Everything else composes the classes directly.

## Razor

Razor writes the Structure by hand. `ButtonLinkTagHelper` emits `kcc-btn` plus the modifiers in `Class`.
Widget loops pick tears from their index: `kcc-tear-@((i % 6) + 1)`. Filter defs for the wax are inline in
`Layout.cshtml`, so server-rendered washes are filtered before hydration.

## Invariants

1. **Build both bundles together**: `yarn build:all`. Scoped-style hashes must match across client and SSR.
2. **`@theme static`**: the `static` keeps unreferenced tokens alive for the kit CSS and the dark ramp.
3. **Razor-rendered → `@layer components`**, never a scoped `<style>`.
4. **A rule that must beat a utility is unlayered** (the ramp-swap rule at the end of `Kit.css`).
5. **`Tears.css` is generated.** Change `tears.ts`, run `yarn tears`, commit both.
6. **No runtime JS draws anything.** The tear, the wash and the shadow are CSS; SSR output is final.
7. **Both ramps, every change.** The light ramp is the default; dark is where washes and hairlines fail first.
8. **Filter outside clip; label and tape outside both.** See Structure.

## Enforcement

| Rule | Test |
|---|---|
| WCAG AA in both ramps, including ink over paper + wash | `tests/KCC.ViteTests/Features/Styles/contrast.test.ts` |
| No retired token, class, utility or directive in `Features/**` or CMS content | `tests/KCC.ViteTests/Features/Styles/retiredTokens.test.ts` (`ALLOWLIST` is empty; a new entry needs a reason in the commit message) |
| Committed `Tears.css` equals the generator; every preset declared on `:root` with a selecting class; sheet tilts written to `--r`; one or two standard sheets uncut | `tests/KCC.ViteTests/Features/Torn/tears.test.ts` |
| Tear geometry: deterministic, every vertex inside the box for every shipped preset, seam closes within a step, chamfer never top-left, amp and point-count guards | `tests/KCC.ViteTests/Features/Torn/tornPolygon.test.ts` |
| `DesignSystem.ts` axes match the safelist | `tests/KCC.ViteTests/Features/Types/DesignSystem.test.ts` |
| Every Font Awesome style used is imported | `tests/KCC.ViteTests/Features/Styles/mainCssIconStyles.test.ts` |
| The 15 / 24 base, heading sizes, the 16px control floor and every referenced font file | `tests/KCC.ViteTests/Features/Styles/typography.test.ts` |
| Every class in the Classes table is defined in `Torn/*.css` and every class the Torn CSS defines is in the table; `box-shadow` appears only as a hairline or a ring | `tests/KCC.ViteTests/Features/Styles/kitClasses.test.ts` |

## Building a surface

1. Read the neighbouring component and its test. Keep every structural hook (`data-testid`, roles, ids); e2e
   locates by hook, not markup.
2. Sheets are `KccSheet` or the hand-written Structure. Pick the tear from the list index or a stable hash.
3. Every class comes from the Classes table or Tailwind; no component `<style>` rule for anything Razor
   also renders.
4. No `rounded-lg`+, `shadow-*`, `font-bold/semibold/medium`. Text is `text-ink` or `text-ink-soft`;
   `text-marker-ink` only inside a tile, a label or a marker button.
5. Icons: duotone by default, `currentColor`, no `fa-primary-*` / `fa-secondary-*`.
6. Numbers and meta in Sono: `kcc-num`, `kcc-kick`, `kcc-meta`, `kcc-stat`.
7. `ALLOWLIST` in `retiredTokens.test.ts` stays empty. If a surface genuinely needs an entry, the commit
   message says why.
8. Run, from `src/KCC.Web`: `yarn test <name> retiredTokens contrast`, `yarn type-check`, `yarn format`; before
   merging also `yarn build:all` and the browser check in both ramps. Commit.
