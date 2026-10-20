# Loose Leaf — the kit

The engineering contract for [loose-leaf.md](loose-leaf.md): tokens, classes, structure, invariants,
and the test that enforces each rule. Anything Razor also renders lives in global `@layer components` CSS,
never in a component `<style>` block.

## Files

| Path | Holds |
|---|---|
| `src/KCC.Web/Features/Styles/TailwindConfig.css` | `@theme static` role tokens (light values), the type, control, pill and icon tokens, radius ladder, fonts, safelist; the `*-rule-*` spacing utilities |
| `src/KCC.Web/Features/Styles/Torn/Tokens.css` | kit-only properties, among them the rule `--bl` and its drop `--rule-drop`; the dark ramp under `:root[data-theme='dark']` |
| `src/KCC.Web/Features/Styles/Torn/Tears.css` | **generated** tear presets (`yarn tears`). Never hand-edited |
| `src/KCC.Web/Features/Styles/Torn/Kit.css` | desk, slip / torn / blank / sheet / wash / label / tape / tile, type, chrome, ramp swap |
| `src/KCC.Web/Features/Styles/Torn/Controls.css` | btn, seg, field, range slider, badge, check, stats, steps, recipe-slip parts |
| `src/KCC.Web/Features/Styles/Typography.css` | the 16 / 24 body, the type roles of bare `h1`–`h6` in APCasual, form controls at the body size, every `@font-face` including Sono |
| `src/KCC.Web/Features/Styles/Layout.css` | the `.content-grid` breakout system: content, breakout and full-width columns for pages and sections |
| `src/KCC.Web/Features/Styles/Main.css` | the stylesheet import graph; every `Torn/*.css` file is imported here; `#app` carries the desk colour so the overlays have a backdrop to blend with, and clips sideways overflow |
| `src/KCC.Web/Features/Types/DesignSystem.ts` | `WASHES` and the `Wash` type; the colour axes the safelist test checks |
| `src/KCC.Web/Features/Torn/tornPolygon.ts` | pure tear generator |
| `src/KCC.Web/Features/Torn/tears.ts` | preset table and CSS emitter |
| `src/KCC.Web/Features/Torn/generateTears.ts` | CLI that writes `Tears.css` |
| `src/KCC.Web/Features/Components/Sheet/KccSheet.vue` | the sheet primitive for Vue |
| `src/KCC.Web/Features/Pages/Shared/Layout.cshtml` | renders a signed-in member's saved ramp or runs the pre-paint ramp script; font preloads, `#kcc-wax` / `#kcc-wax-flat` filter defs |
| `src/KCC.Web/Features/Utilities/Ramp.ts` | `applyRamp`, the one way to switch the ramp after the first paint; `deviceRamp` and `rampFor`, the ramp a device or a saved setting asks for |

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
| `tape` | `.985 .012 92 / .6` | `.9 .012 92 / .28` | the strip over a pinned tile |
| `focus` | `var(--color-ink)` | same | focus ring |
| `peach yellow green teal sky lavender pink red` | see the identity | same | the eight washes; fills only |

All values are `oklch(L C H [/ alpha])`.

The type and size tokens sit in the same `@theme static` block and are the same in both ramps. The block opens with
`--text-*: initial`, which removes Tailwind's default sizes (`text-xs` … `text-9xl`), so the only text-size utilities
are the scale's. Each `--text-<role>` carries its line height in `--text-<role>--line-height`, and its `text-<role>`
utility sets both.

| Token | Value | Use |
|---|---|---|
| `--text-display` | 2.5rem / 3rem (40 / 48) | `kcc-display` and bare `h1` from 768px |
| `--text-display-phone` | 2.25rem / 3rem (36 / 48) | `kcc-display` and bare `h1` below 768px |
| `--text-section` | 2rem / 3rem (32 / 48) | `kcc-section` and bare `h2` |
| `--text-heading` | 1.5rem / 1.5rem (24 / 24) | `kcc-heading`, bare `h3`, the stat value |
| `--text-title` | 1.25rem / 1.5rem (20 / 24) | `kcc-title`, `kcc-hand`, bare `h4`–`h6` |
| `--text-body` | 1rem / 1.5rem (16 / 24) | `body`, `kcc-body`, `input`, `select`, `textarea`, the pad's menus and lines |
| `--text-caps` | 0.875rem / 1.5rem (14 / 24) | every small Sono role: kick, label, meta, unit, stat, badge, button, segment |
| `--text-readout` | 4rem / 4.5rem (64 / 72) | `text-readout`, the Cook Mode timer only; counted apart from the text sizes |
| `--control-md`, `--control-lg` | 2.25rem, 3rem (36px, 48px) | the height and line of `kcc-btn`, `kcc-seg`, `kcc-field`; of `kcc-btn--lg` |
| `--pill-sm` | 1.5rem (24px) | the height and line of `kcc-badge`, `kcc-label`, the pad's count and key hint |
| `--icon-sm`, `--icon-md`, `--icon-lg` | 1.5rem, 2.25rem, 3rem (24px, 36px, 48px) | `kcc-icon-sm`, `kcc-icon-md`, `kcc-icon-lg`, the tile glyphs, the pad glyph; counted apart from the text sizes |

Kit-only properties (`Torn/Tokens.css`, not Tailwind tokens): `--bl` 1.5rem (24px), the rule every line height and
vertical space is counted in; `--rule-drop` (`calc(var(--bl) / 2 + 0.38208 * var(--text-body))`, how far below a
line's top the body baseline sits, so the ruling lands on it); `--rd` 6px (labels, wells, textareas), `--wash-blend`
(multiply / screen), `--wash-op` (.62 / .26, dark value test-pinned), `--wash-sat` (1 / 1.45), `--grain-op` (.08 /
.16), `--wax-op` (.3 / .36), `--grain`, `--crayon`. Per element: `--pad` (sheet), `--tear`, `--r`, `--blank-tear` and
`--blank-dir` (set by a preset class), `--blank-turn`, `--blank-scale` and `--blank-flip` (set on the slip and its
modifiers), `--blank-region` (set on a region that turns its sheets' blanks over), `--c --x --y --w --h` (wash).

**Retired** and caught by `retiredTokens.test.ts`: `ink-line`, `ink-on-wash`, `hatch`, `edge`,
`edge-strong`, `flap-1`, `flap-2`, `link`, `danger`, `success`, `warning`, `danger-ink`, `success-ink`,
`warning-ink`, `rating-ink`, the washes `rosewater flamingo mauve maroon sapphire blue`, every `shadow-*`
utility, `rounded-lg` and larger, `font-bold`, `font-semibold`, `font-medium`, the `fa-primary-*` /
`fa-secondary-*` Tailwind utilities (the kit sets the Font Awesome custom properties itself; see Icons),
every `sk-*` class, `v-ink`, `data-ink`, the two heading classes named by level that the type roles replace.
Nothing keeps them alive: the Softbound tokens, the ink module and the `TRANSITIONAL` blocks are gone, and the
test's `ALLOWLIST` is empty. A new entry there needs a reason in the commit message.

**Radius ladder:** `rounded-xs` 3px (checkbox), `rounded-sm` 4px (small marks), `rounded-md` 6px (labels,
wells, textareas), `rounded-full` (pills). Sheets, tiles and images take no radius.

## Type

Seven text sizes in even steps, each read from a token (see Tokens), and every line height a multiple of
the 24px rule, a control or pill height, or 1 on a glyph, number or unit inside a line:

| Role | Class | Face | From 768px | Below 768px |
|---|---|---|---|---|
| Display | `kcc-display`; bare `h1` | APCasual | 40 / 48 | 36 / 48 |
| Section heading | `kcc-section`; bare `h2` | APCasual | 32 / 48 | same |
| Sheet heading, stat value | `kcc-heading`; bare `h3`; `.kcc-stats .kcc-v` | APCasual (the stat value Sono) | 24 / 24 | same |
| Card title, hand note | `kcc-title`; bare `h4`–`h6`; `kcc-hand` | APCasual (the hand note italic) | 20 / 24 | same |
| Body | `body`, `kcc-body`; `input`, `select`, `textarea`; `.pad-menu`, `.pad-line` | Hazelnut | 16 / 24 | same |
| Caps | `kcc-kick`, `kcc-lbl`, `kcc-meta`, `kcc-unit`, `kcc-label`, `kcc-badge`, `kcc-btn`, `kcc-seg` buttons, `.kcc-n`, the stats `small` | Sono caps | 14 / 24 | same |
| Readout | `text-readout`, the Cook Mode timer only | Sono | 64 / 72 | same |

`kcc-stat` and `.kcc-q` take the caps size in Sono without the caps. Inside a control the caps line is the control's
height (`--control-md`, `--control-lg`), and inside a small pill it is `--pill-sm`. Sono caps track at `.1em`, `.14em`
on `kcc-btn` and `kcc-label`, `.12em` on `kcc-badge` and the segments, `.08em` on `kcc-meta`; the APCasual and
Hazelnut roles at 0, the stat value at `-.01em`.

`Features/Styles/Typography.css` is the base: `body` in Hazelnut regular at the body role, and bare `h1`–`h6` in
APCasual at the roles above, so editor rich text gets a sane size. Heading *level* is semantic and heading *size*
comes from the role classes, so a bare heading is never a thing in new markup: write `<h4 class="kcc-title">` or a
`kcc-kick`. Below 768px the display takes its phone size, 36 / 48; every other role keeps one size at every width, so no
two heading roles share a size. Form controls (`input`, `select`, `textarea`) take the body size, 16px, everywhere:
iOS zooms the viewport into any focused field below 16px, and the kit's fields inherit this. Sono is a static
instance (MONO 1 / 400): `font-variation-settings` is a no-op on it. Nothing the kit renders sets a weight, but
editor rich text may still carry `<strong>`, so `Typography.css` keeps the Hazelnut Bold faces declared for it.

An icon beside text sets no size and inherits the text's. A standalone icon takes `kcc-icon-sm` (24px),
`kcc-icon-md` (36px) or `kcc-icon-lg` (48px), or reads `--icon-*` in kit CSS.

The rules for authors ([the type scale contract](../../specs/004-type-scale/contracts/type-scale.md)):

- **May** size text with a role class (`kcc-display`, `kcc-section`, `kcc-heading`, `kcc-title`, `kcc-body`,
  `kcc-hand`, `kcc-kick`, `kcc-lbl`, `kcc-meta`, `kcc-unit`), or, where no role class fits, with a token utility
  (`text-caps`, `text-body`, `text-title`, `text-heading`, `text-section`, `text-readout`), which carries its own
  line height. The display size steps down below 768px, so reach it only through `kcc-display` or a bare `h1`, never
  `text-display` or `text-display-phone`.
- **May** read a token in a kit stylesheet or a style block: `var(--text-<role>)`, `var(--text-<role>--line-height)`,
  `var(--bl)`, `var(--control-md)`, `var(--control-lg)`, `var(--pill-sm)`, `var(--icon-sm|md|lg)`.
- **May** space blocks vertically in rule units (see the spacing paragraph under Classes).
- **Must not** write a `font-size`, a `font` shorthand size or a px `line-height` outside the tokens. A `font-size` is
  a `var(--text-*)`, a `var(--icon-*)`, `inherit` or `1em`; a `line-height` is `var(--bl)`, a
  `var(--text-*--line-height)`, a control or pill token, `1`, or a `calc()` of `var(--bl)` times a number.
- **Must not** use `text-[<length>]`, a `text-<role>/<line>` modifier, any `leading-*`, or a default size name
  (`text-xs` … `text-9xl`, which do not generate).
- **Must not** space blocks vertically on Tailwind's spacing scale: `mt`, `mb`, `my`, `pt`, `pb`, `py`, `gap-y` or
  `space-y` with `3`, `6`, `9`, `12`, `18` or an arbitrary length, or a two-axis `gap-6`, `gap-9`, `gap-12`, `gap-18`
  or arbitrary `gap` on a grid or a column.
- **Must not** size a heading by its level: the two level-named heading classes are retired (see Tokens).

`typeScale.test.ts` enforces these rules on every `.css`, `.vue`, `.cshtml` and `.ts` file under `Features/` but the
generated `Tears.css`, naming the file, the line and the value, and fails when `TailwindConfig.css` declares more than
eight text sizes besides the readout. `typography.test.ts` pins the token values, the roles of `body`, the form
controls and the bare headings, the phone step, and checks every font file it references exists.

## Structure

Paper lies in piles two high: every sheet sits on a blank sheet, torn and shadowed like it, with nothing on it. A
labelled, washed sheet on its blank:

```html
<div class="kcc-slip kcc-tear-3">                 <!-- tilt via --r; position: relative; height: fit-content -->
  <div class="kcc-torn">                          <!-- filter: fibre + fall -->
    <div class="kcc-sheet" style="--pad: 48px">   <!-- paper, clip-path: var(--tear), 24px pencil rule -->
      <span class="kcc-wash" style="--c: var(--color-peach); --x: 88%; --y: 18%; --w: 38%; --h: 60%" aria-hidden="true"></span>
      <p class="kcc-kick">Kitchen Command Center</p>
      <h2 class="kcc-display">The perfect drawing, torn out and kept.</h2>
    </div>
  </div>
  <span class="kcc-label"><i class="fa-duotone fa-scissors" aria-hidden="true"></i>Identity · 01</span>
  <span class="kcc-blank" aria-hidden="true"><span></span></span>   <!-- filter here, clip on the inner span -->
</div>
```

A plain sheet (no label):

```html
<div class="kcc-slip kcc-tear-2">
  <div class="kcc-torn">
    <div class="kcc-sheet">…</div>
  </div>
  <span class="kcc-blank" aria-hidden="true"><span></span></span>
</div>
```

A recipe slip:

```html
<article class="kcc-slip kcc-recipe kcc-tear-5">
  <div class="kcc-torn">
    <div class="kcc-sheet">
      <span class="kcc-stat"><i class="fa-duotone fa-star" aria-hidden="true"></i><span class="kcc-num">4.5</span><span class="text-ink-soft">· 28</span></span>
      <h3 class="kcc-title">Brown Butter Gnocchi</h3>
      <p class="kcc-meta"><span>Pasta</span><span>6 var.</span><span class="kcc-num">25 min</span></p>
      <div class="kcc-badges mt-rule-1"><span class="kcc-badge">Vegetarian</span><span class="kcc-badge">One pan</span></div>
    </div>
  </div>
  <div class="kcc-tilewrap">
    <div class="kcc-torn"><div class="kcc-tile kcc-tear-tile-2" style="--c: var(--color-peach)" aria-hidden="true"><i class="fa-duotone fa-wheat"></i></div></div>
    <span class="kcc-tape" aria-hidden="true"></span>
  </div>
  <span class="kcc-blank" aria-hidden="true"><span></span></span>
</article>
```

When the whole slip is one link, the link **is** the `.kcc-torn` wrapper (`<a class="kcc-torn block">`): it
sits outside the clip, so the tear never shaves its focus ring, and the click target is the whole sheet. The
tilewrap stays a sibling of it and is pointer-transparent, except a `.kcc-tilewrap--flow`, which sits inside the
link's sheet in the content flow (a list row's tile).

Rules of the structure:

1. `clip-path` sits on `.kcc-sheet` / `.kcc-tile`. The `filter` sits on a wrapper **outside** the clipped
   element (`.kcc-torn`), or the shadow is clipped away with it.
2. Label and tilewrap are siblings of `.kcc-torn` inside `.kcc-slip`: outside the clip **and** outside the
   filter, so they are neither torn nor shadowed. The tape sits inside the tilewrap, never directly in the slip.
   The one exception is a `.kcc-tilewrap--flow`, which sits in the sheet's content flow with its tape (a list
   row's tile). The blank is a sibling too, and always the
   slip's **last** child, once: outside the top sheet's clip and filter, with its own filter on `.kcc-blank` and
   its own clip on the inner span. It is never a `kcc-slip`, `kcc-torn` or `kcc-sheet`, so a one-element slip
   (`kcc-slip kcc-torn` on one element) cannot hold one.
3. Every direct child of `.kcc-sheet` except `.kcc-wash` is lifted above the wash by the kit at class
   specificity (`:where`). A kit class that positions a sheet child itself is written `.kcc-sheet > .kcc-x`
   so it wins regardless of import order (see `kcc-stat`).
4. `--pad` on the sheet also positions the pencil rule: pass padding as `style="--pad: …"`, not as padding
   utilities. It is **one length**: the rule's offset is `calc(var(--pad) + var(--rule-drop) - var(--bl) + 1px)`,
   which puts the top of its line on the first body baseline, and a shorthand (`24px 36px 24px 24px`) makes that
   invalid, so the ruling silently falls back to the top edge. Asymmetric room comes from a utility on the content,
   never from `--pad`.
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
11. A slip is `height: fit-content`, so its blank (`inset: 0`) is always as tall as its sheet, even in a grid
    that stretches its items. A slip that must fill a definite-height parent takes `kcc-slip--fill`.
12. A page's blanks alternate in reading order: the visible sheets in `main`, in document order, at the current
    viewport. A blank turns by its preset's `--blank-dir`, which already alternates through `(index % 6) + 1`.
    `kcc-slip--flip` (`KccSheet flip`) turns one slip's blank the other way: set it on a fixed sheet that would
    match its neighbour, and compute it for a sheet that follows a run of varying length. `[--blank-region:-1]` on
    a container, usually behind a breakpoint variant (`max-lg:`), turns every blank inside it, for a run whose
    neighbour a breakpoint hides. The header pad floats over every page and is not a neighbour; an overlay is the
    only sheet on its screen.
13. A sheet on a blank wider than 700px at a 1280px viewport is wide: `kcc-slip--wide` (`KccSheet wide`), and it
    keeps the modifier at every viewport. So are every hero-torn sheet that lies on a blank, and the pad's page.
14. `#app` takes `overflow-x: clip`, so a blank's overhang past a tall sheet's side never scrolls the page. `clip`
    makes no scroll container, so the sticky pad, filters and stacker keep working.
15. The header pad is the one pile written apart: its top sheet (`.pad-top`) holds no `.kcc-blank`, because the
    `.pad-page` slip beside it, torn `kcc-tear-hero-2`, is that blank, and a pad card (`PadCard.vue`, `KccSheet
    :piled="false"`) slides out between the two and holds none.

## Tears

`Tears.css` declares `--tear-1 … --tear-6`, `--tear-hero`, `--tear-hero-2` and `--tear-tile-1 … 3` on `:root` and the classes
that select them:

| Class | Points / side | Amp | Chamfer | Tilt `--r` | Blank `--blank-tear`, `--blank-dir` |
|---|---|---|---|---|---|
| `kcc-tear-1` | 80 | 2.4 | 19px top-right | +0.30 | `tear-4`, −1 |
| `kcc-tear-2` | 80 | 2.4 | 17px bottom-right | −0.98 | `tear-5`, +1 |
| `kcc-tear-3` | 80 | 2.4 | none | +0.05 | `tear-6`, −1 |
| `kcc-tear-4` | 80 | 2.4 | 18px bottom-left | −0.52 | `tear-1`, +1 |
| `kcc-tear-5` | 80 | 2.4 | 20px bottom-right | +0.63 | `tear-2`, −1 |
| `kcc-tear-6` | 80 | 2.4 | 17px bottom-left | −0.70 | `tear-3`, +1 |
| `kcc-tear-hero` | 160 | 2.4 | 28px top-right | −0.58 | `tear-hero-2`, −1 |
| `kcc-tear-hero-2` | 160 | 2.4 | 24px bottom-left | −0.36 | `tear-hero`, +1 |
| `kcc-tear-tile-1..3` | 24 | 1.8 | none | (a pinned tilewrap −2°; a `--flow` one 0) | none |

Each sheet preset names its blank: the tear three along the cycle, so a sheet never lies on its own tear, and the
other hero tear for a hero. A standard blank turns against its sheet's tilt, so the directions alternate around the
cycle and a list that takes `(index % 6) + 1` alternates by itself. The blank turns `--blank-turn` (1.5°, or 0.5°
wide) × `--blank-dir` × `--blank-flip` × `--blank-region` about its centre and scales `--blank-scale` (1.015, or
1.005 wide).

Depth is constant but the wavelength stretches with the box, so each family has a size band: tile tears for
48–128px tiles, the six sheet tears for sheets 170–700px wide, the two hero tears for 330–1300px. A tile tear on a
full-width sheet reads as a scallop, not a tear.

**Assignment:** a list item takes `kcc-tear-${(index % 6) + 1}`, unless its page hands it one from a page-wide
cycle (see **Razor**); a standalone sheet takes a preset from a
stable hash of its id (see `Utilities/BrandColor.ts`) or the one the page design names; the default is
`kcc-tear-1`. Neighbours never share a tear. `kcc-tear-hero-2` is the hero's blank and the pad's page, never a
hero of its own, so two hero sheets on one page (RecipeDetail's hero and its featured variant) both take
`kcc-tear-hero`; keep at least one standard sheet between them.
`yarn tears` regenerates the file; `tears.test.ts` fails if the committed file drifts from the generator. Need a crisp surface (a form, cook mode)? `KccSheet crisp` sets
`--r: 0` on the slip; Razor writes `style="--r: 0"` on the slip itself.

## Classes

| Class | What | Notes |
|---|---|---|
| `kcc-grain`, `kcc-crayon` | desk grain under everything, wax tooth over everything | first and last child of `#app`; `App.vue` owns them; the crayon covers everything inside #app, so a surface teleported to body (cook mode) is deliberately untextured |
| `kcc-slip` (+`--fill`) | tilt + positioning context; `--fill` stretches slip, torn and sheet to a definite-height parent | `--r` from the tear preset; without `--fill` a child sized in percentages computes to auto |
| `kcc-torn` | fibre + fall filter | wraps exactly one clipped element |
| `kcc-blank` › `span` | the blank sheet under a sheet: torn paper with the fibre and fall, no ruling | last child of a slip; `aria-hidden`; tear and base direction from the slip's tear class; turns `--blank-turn` × direction × `--blank-flip` × `--blank-region` and scales `--blank-scale`; the span is `display: block`; stays still when the slip lifts |
| `kcc-slip--wide` | the wide pair: the blank turns 0.5° and scales 1.005 | hero-torn sheets on a blank, the pad's page, any sheet on a blank wider than 700px |
| `kcc-slip--flip` | turns the slip's blank the other way | set where a page's reading order needs it |
| `kcc-slip--lift` | a slip you can open lifts its top sheet 4px on focus within, or on hover with a pointer that can hover; the blank stays | no lift under reduced motion; keeps the focus ring's fade |
| `kcc-sheet` | paper, clip, ruling, padding | `--pad` default 24px, heroes 48px; compact rows (recipe rows, review slips, the account sheet) take 16px and accept a ruling 8px off a 24px neighbour's |
| `kcc-wash` | the pool of colour | `--c` a wash token, `--x --y` the centre, `--w --h` the radii (any length-percentage). Copy may sit on the 70% ring, never on the core: a corner pool caps the radius that points at the copy in px (`min(60%, 68px)` keeps the ring inside a 48px padding at any sheet height) |
| `kcc-label`, `kcc-label--right` | marker pill over the top-left (or right) edge, 24px (`--pill-sm`) | Sono caps 14px, optional leading `<i>` |
| `kcc-pill` | the label's marker fill and ink on an element that stays in the flow | a current wizard step, an open menu button; sets no type: the step keeps its `kcc-kick`, the pad's menu button its body size |
| `kcc-tape` | one strip over an applied tile | only inside `kcc-tilewrap`; a sheet is never taped |
| `kcc-tilewrap` › `kcc-torn` › `kcc-tile` (+`--lg`) | pinned torn wax tile | `--c` the wash, glyph in `marker-ink` at `--icon-sm` (`--icon-md` on `--lg`); pointer-transparent, so a link beneath it keeps the click |
| `kcc-tilewrap--flow` | a tilewrap in the flow of a sheet's content | a list row's tile and its strip; `position: relative`, no offset and no turn |
| `kcc-kick`, `kcc-lbl` | Sono caps 14/24 in ink-soft | section kickers, field labels |
| `kcc-body` | Hazelnut 16/24 body | a class-less link inside (editor rich text) takes the `kcc-link` look |
| `kcc-prose` | editor rich text, with `kcc-body` | each block one rule below the last; lists keep their markers, list numbers in Sono |
| `kcc-display` | APCasual 40/48, 36/48 below 768px | a page's or a hero's title; bare `h1` takes the same role |
| `kcc-section` | APCasual 32/48 | a section heading, the heading in `kcc-secname`, Cook Mode's step text; bare `h2` takes the same role |
| `kcc-heading` | APCasual 24/24 | a whole sheet's title; bare `h3` takes the same role |
| `kcc-title` | APCasual 20/24 | a card's, a row's, a tile's or an empty state's title; bare `h4`–`h6` take the same role |
| `kcc-hand` | APCasual italic 20/24 ink-soft | one per sheet |
| `kcc-icon-sm`, `kcc-icon-md`, `kcc-icon-lg` | standalone icon 24px, 36px, 48px (`--icon-sm\|md\|lg`) | the Cook Mode stopwatch; the add-variant glyph; empty-state glyphs. The tile and pad glyphs read the same tokens in CSS. An icon beside text takes no class and inherits the text's size |
| `kcc-num` | Sono tabular | every number |
| `kcc-unit` | Sono caps 14px in ink-soft | a unit beside a number (min, g, servings); the same small caps a stats row gives its `<small>` |
| `kcc-hr` | dashed hair rule | |
| `kcc-link` | ink + hair-strong underline | prose links, breadcrumbs |
| `kcc-link--icon` | icon-only link (the home crumb), with `kcc-kick` on the item for size | body-size icon (16px) in a 24px (`--bl`) min-height/min-width box, no underline; ink-soft, ink on hover |
| `kcc-secname` | section heading row: a `kcc-section` heading + `kcc-kick` with a dashed underline | one and a half rules below it |
| `kcc-foot` | footer copy block | |
| `kcc-btn` (+`--ghost`, `--ink`, `--text`, `--lg`, `--icon`) | marker pill 36px (`--control-md`); hairline ghost; ink fill; underlined text on the rule; 48px large (`--control-lg`); `--icon` squares the pill (36px, 48px with `--lg`) | Sono caps 14px `.14em`; an `--icon` button is all glyph, so its name goes in `aria-label` |
| `kcc-seg` › `kcc-seg-thumb`, `button[aria-pressed\|aria-checked]` | pill group 36px (`--control-md`), Sono caps 14px, pressed = ink fill | pressed = ink fill; in `SegmentedControl.vue` the fill is an ink thumb that slides to the checked segment once the client has placed it (`kcc-seg--sliding`), and until then the checked button carries it; `kcc-seg-thumb--glide` turns the slide on after each placement that must not slide; an unpressed segment goes from ink-soft to ink on hover; labels never wrap; the group scrolls horizontally when narrower than its segments |
| `kcc-field` (+`--noicon`, `--area`) | 36px (`--control-md`) pill with inset hairline and leading icon; block variant for textareas | field text Hazelnut 16px; focus = 2px inset ink ring |
| `kcc-range` › `kcc-range-track`, `kcc-range-fill` | paper track with an inset hairline, marker fill, ink thumbs with a paper ring | both thumbs are native range inputs stacked over the track; the ring keeps the thumb off the marker fill |
| `kcc-badge`, `kcc-badges` | 24px (`--pill-sm`) hairline pill, Sono caps 14px; wrapping row | |
| `kcc-check` › `li` › `kcc-box` (+`--on`), text, `kcc-q`; `li.kcc-done` | checklist on the rule | the checkbox is `sr-only`, so a focused checkbox rings its box instead |
| `kcc-stats` › `div` › `kcc-lbl` + `kcc-v` | stat row, values Sono 24/24 (`--text-heading`) | `<small>` for the unit, Sono caps 14px |
| `kcc-steps` › `li` › `kcc-n` + `kcc-body` | numbered method | numbers `01`, `02`, … |
| `kcc-well` (+`--danger`, `--success`, `--warning`) | paper tinted 28% with the wash, ink text, 6px radius | status messages; the message itself is a `kcc-kick` |
| `kcc-field--error` | the field's own fill tinted with red | pair with a `kcc-well--danger` message below the field |
| `kcc-recipe`, `kcc-stat`, `kcc-meta` | recipe slip modifiers | see Structure; the stat is `.kcc-sheet > .kcc-stat` |

Vertical space between things is counted in rules, so text stays on the ruling. `TailwindConfig.css` defines
`<prefix>-rule-<n>` as `calc(var(--bl) * n)` for `mt`, `mb`, `my`, `pt`, `pb`, `py`, `gap-y`, `space-y`, `h`,
`min-h` and `top`, with `n` a whole or half number: `mt-rule-1` (24px), `gap-y-rule-1.5` (36px), `mt-rule-2`
(48px), `mt-rule-0.5` (12px), `gap-y-rule-3` between sections. A grid or a wrapping row splits its gap,
`gap-x-6 gap-y-rule-1`; horizontal space and a gap along a single row stay on Tailwind's spacing scale (`gap-x-7`,
`gap-3`).

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

## States

- **Hover.** `kcc-btn` mixes 12% `marker-ink` into its marker, and `kcc-btn--ink` mixes 12% paper into its ink. The
  hairline of `kcc-btn--ghost` turns ink, and the underline of `kcc-btn--text` and `kcc-link` goes from `hair-strong`
  to ink. An unpressed `kcc-seg` segment and `kcc-link--icon` go from `ink-soft` to ink. A slip you can open lifts,
  and a pad menu fans its card out with no underline of its own (see Motion). A row or search result in a pad card
  underlines its name in ink, and the hairline of a Try chip, or of a diet chip in the phone menu, turns ink.
- **Focus.** A 2px ink outline, 2px out, on every focusable (`:focus-visible` in `TailwindConfig.css`). `kcc-field`
  rings inside instead, 2px inset ink on `focus-within`. A `kcc-check` rings its `kcc-box`, because the checkbox
  itself is `sr-only`. A slip that is a link rings its `kcc-torn`, outside the tear. Under `forced-colors: active`
  the field and the segments take a `CanvasText` outline, because high-contrast mode paints no box-shadow. The
  `kcc-range` thumbs have no focus ring yet.
- **Pressed, current, checked.** A pressed or checked `kcc-seg` segment fills with ink. In `SegmentedControl.vue` the
  fill is the `kcc-seg-thumb`, which slides from the old segment to the new one. The current wizard step and
  an open menu button are `kcc-pill`. The section you are in is a marker underline on its menu, 5px of marker
  behind the label, hidden while that menu is open. A checked box is `kcc-box--on`, and a done checklist row
  (`li.kcc-done`) is struck through.
- **Disabled.** Buttons only: 45% opacity, a `not-allowed` cursor and no hover. Nothing else in the kit has a
  disabled look, so a field, segment, checkbox or range gets one here before anything disables it.
- **Error.** `kcc-field--error` on the field and a `kcc-well--danger` below it (see Status).
- **Loading.** What is already shown stays. The region takes `aria-busy`, and the loader is a
  `fa-solid fa-circle-notch fa-spin` beside a `kcc-kick` message (`RecipeSearchView.Component.vue`).
- **Empty.** A sheet, never an empty page: a large duotone glyph in `ink-soft`, a `kcc-title` line, a `kcc-body` hint and a
  ghost button that undoes whatever emptied it (`RecipesEmptyState.vue`). A recipe with no reviews says so in its
  meta line, and empty rating stars are `fa-regular` in `ink-soft`.
- **Browser surfaces.** `accent-color` is ink, and underlines sit 3px below the text. `kcc-seg` hides its
  scrollbar. Text selection, the caret and the page scrollbars are the browser's own. Each ramp sets
  `color-scheme`, `light` on `:root` in `TailwindConfig.css` and `dark` in the dark ramp, so what the browser
  draws itself follows the ramp: a `<select>`'s option list, the scrollbars, an autofilled field's fill. Without
  it the dark ramp's chalk lands on the browser's light list. `contrast.test.ts` checks both.

## Motion

The identity says how paper moves ([loose-leaf.md](loose-leaf.md) → Motion). All of today's motion is CSS:

| What moves | How | Where |
|---|---|---|
| A slip you can open lifts its top sheet 4px on focus within, or on hover with a pointer that can hover; the blank stays; never under reduced motion | `kcc-slip--lift`: `translate` over 300ms `cubic-bezier(0.4, 0, 0.2, 1)`, keeping the focus ring's 0.2s fade | `RecipeCard.vue`, `RecipeCardRow.vue`, `FeaturedRecipeCard.vue`, `VariantGrid.vue` |
| A card's drawer slides up | `transition-all`, height from 0 to full on hover or focus-within | `Card.Component.vue` |
| A pad card fans out as you reach for its menu | 300ms `cubic-bezier(.2, .9, .3, 1.15)`: 19px out of the stack, turned −1.8° about its far top corner, the top right, or +1.8° about its top left for the mirrored desktop search card (on a phone 24px straight down, turned +2.4° about its bottom left) | `PadCard.vue` |
| A pad card comes out of the stack | 480ms `cubic-bezier(.3, 1.2, .4, 1)` to its own tilt, its rows landing 20ms apart; back in over 280ms; switching from another card, it waits 100ms for that one to go | `PadCard.vue` |
| The pad tucks away as you read | 300ms `cubic-bezier(.2, .8, .2, 1)`: up by its height plus 28px (130px for the 102px pad) as you scroll down, once the page is past 150px; back on any scroll up or when focus moves into it; never under reduced motion | `AppHeader.Component.vue` |
| Sticky neighbours follow the pad | `transition-[top]`: their top, which adds `--pad-offset`, eases over 300ms as the pad tucks and comes back, and a stacker card's sentinel eases with it | `RecipeSearchView.Component.vue`, `StackerBlock.cshtml`, `Stacker.Component.vue` |
| A pad menu fades to and from its marker pill | `background-color` and `color` over 300ms, keeping the focus ring's 0.2s fade | `AppHeader.Component.vue` |
| A pad menu's chevron flips | 180° over 300ms while its card is open | `AppHeader.Component.vue` |
| The desktop search field widens as it takes focus | 300ms `cubic-bezier(.2, .8, .2, 1)`, from 250px to 330px (200px to 240px between 768px and 1023px) | `AppHeader.Component.vue` |
| A row's name in a pad card underlines | `text-decoration-color` from transparent to ink over 300ms on hover | `PadLines.vue` |
| A stacked sheet steps back | `transition-all duration-100`, scale from 1 to .95 once it sticks, all but the last | `Stacker.Component.vue` |
| A segment's ink fill slides to the checked segment | `transform` and `width` over 300ms, and the labels' colour fades with it; no slide for the first placement, after a resize, when the fonts load or when the labels change | `SegmentedControl.vue`, `Controls.css` |
| The focus ring fades in | `transition: outline-color 0.2s, outline-offset 0.2s` on every focusable | `TailwindConfig.css` |
| More recipes load | `fa-spin` on a `fa-circle-notch` | `RecipeSearchView.Component.vue` |

1. **300ms is the standard.** `--default-transition-duration` in `Main.css` gives it to every `transition-*`
   utility that names no duration, which is most of them.
2. **500ms is the ceiling**, for a bigger move that would feel rushed at 300ms, like a pad card coming out. A loop such
   as the spinner is exempt: its duration is one turn, not a wait.
3. **Any property, any easing.** Tailwind's default curve unless a move wants another.
4. **Reduced motion stills everything CSS runs.** Under `prefers-reduced-motion: reduce`, `Main.css` sets every
   transition and animation to 0.01ms (not `none`, so `transitionend` and `animationend` still fire) and turns
   smooth scrolling off. Motion started from script (`element.animate`, a `requestAnimationFrame` loop, an animation
   library) never reaches that rule, so it checks `matchMedia('(prefers-reduced-motion: reduce)')` itself.
5. **A ramp switch freezes motion for a frame.** `applyRamp` (`Utilities/Ramp.ts`) sets `data-theme-switching` on
   the root and clears it only after a frame has rendered with it, and `Kit.css` drops every transition and
   animation under it, so no token eases between the ramps. Everything that switches the ramp after the first
   paint goes through it.

## Vue primitives

- `Components/Sheet/KccSheet.vue` — props `label` or a `#label` slot for rich content (a `<ResourceString>`
  keeps its editor hooks that way), `icon`, `labelRight`, `wash`, `at { x y w h }`, `tear` (`1..6 | 'hero'`;
  `'hero'` also adds `kcc-slip--wide`), `piled` (default `true`; `false` renders no blank, and only `PadCard.vue`
  passes it), `wide` (`kcc-slip--wide`), `flip` (`kcc-slip--flip`), `pad`, `crisp` (no tilt: forms, cook mode),
  `as` (`div | section | article | li`). Default slot; the blank renders after the label. Plain `.vue`: import it
  locally. Renders the Structure above with no client JS.
- `Components/Button/Button.vue` — `variant: 'marker' | 'ghost' | 'ink' | 'text'`, `size: 'md' | 'lg'`,
  `as: 'button' | 'a'`. Emits the `kcc-btn` classes; no directive.
- Everything else composes the classes directly.

## Razor

Razor writes the Structure by hand, blank included. `ButtonLinkTagHelper` emits `kcc-btn` plus the modifiers in
`Class`. Loops pick tears from their index: `kcc-tear-@((i % 6) + 1)`. A page built from blocks, like Home, runs one
cycle through all its sheets and cards instead, so neighbours never share a tear across two sections either. A
stacker's sheet is sticky and travels, so the cycle skips its tear for the stack's cards and for the first two sheets
or cards of the next section, where the sheet comes to rest: its half of the width sits over two cards at most,
because grids stop at four columns. A skip passes the tear after it too, so the blanks keep alternating: odd and even
tears turn their blanks opposite ways. When that next section is another stacker, its own sheet's window takes over
after the sheet, so its first card may repeat the first sheet's tear, corner to corner. Filter defs for the wax are
inline in `Layout.cshtml`, so server-rendered washes are filtered before hydration.

## Invariants

1. **Build both bundles together**: `yarn build:all`. Scoped-style hashes must match across client and SSR.
2. **`@theme static`**: the `static` keeps unreferenced tokens alive for the kit CSS and the dark ramp.
3. **Razor-rendered → `@layer components`**, never a scoped `<style>`.
4. **A rule that must beat a utility is unlayered** (the ramp-swap rule at the end of `Kit.css`).
5. **`Tears.css` is generated.** Change `tears.ts`, run `yarn tears`, commit both.
6. **No runtime JS draws anything.** The tear, the wash and the shadow are CSS; SSR output is final.
7. **Both ramps, every change.** The light ramp is the default; dark is where washes and hairlines fail first.
8. **Filter outside clip; label and tape outside both**, except a `.kcc-tilewrap--flow` and its tape, which sit in
   the sheet's content flow. See Structure.
9. **Every sheet lies on a blank**, the slip's last child, outside the top sheet's clip and filter. A sheet is never
   taped; tape holds only a tile. The header pad is the one exception (Structure rule 15).

## Enforcement

| Rule | Test |
|---|---|
| WCAG AA in both ramps, including ink over paper + wash | `tests/KCC.ViteTests/Features/Styles/contrast.test.ts` |
| No retired token, class, utility or directive in `Features/**` or CMS content | `tests/KCC.ViteTests/Features/Styles/retiredTokens.test.ts` (`ALLOWLIST` is empty; a new entry needs a reason in the commit message) |
| Committed `Tears.css` equals the generator; every preset declared on `:root` with a selecting class; sheet tilts written to `--r`; one or two standard sheets uncut; every sheet preset writes its blank (`--blank-tear`, never its own tear, and `--blank-dir`, against its tilt and alternating around the cycle; the hero pair named) and a tile none | `tests/KCC.ViteTests/Features/Torn/tears.test.ts` |
| Tear geometry: deterministic, every vertex inside the box for every shipped preset, seam closes within a step, chamfer never top-left, amp and point-count guards | `tests/KCC.ViteTests/Features/Torn/tornPolygon.test.ts` |
| `DesignSystem.ts` axes match the safelist | `tests/KCC.ViteTests/Features/Types/DesignSystem.test.ts` |
| Every Font Awesome style used is imported | `tests/KCC.ViteTests/Features/Styles/mainCssIconStyles.test.ts` |
| The type, control, pill and icon token values and the cleared default sizes; `--bl` 24px and `--rule-drop`; `body` and the form controls at the body size; the bare headings' roles; the display's phone step and the section's one size; every referenced font file | `tests/KCC.ViteTests/Features/Styles/typography.test.ts` |
| Every `font-size` reads a text or icon token; a `font` shorthand sizes with a text token; a `line-height` is the rule, a token or `1`; no Tailwind text size outside the scale and no `leading-*`; vertical spacing in rule units; at most eight text sizes besides the readout | `tests/KCC.ViteTests/Features/Styles/typeScale.test.ts` |
| Every class in the Classes table is defined in `Torn/*.css` and every class the Torn CSS defines is in the table; `box-shadow` appears only as a hairline or a ring | `tests/KCC.ViteTests/Features/Styles/kitClasses.test.ts` |
| The pile's kit CSS: the slip's `fit-content` and blank defaults, the blank's turn, scale, fibre, fall and unruled paper, the flip and wide pairs, the lift (every child but the blank, 4px, only when motion is welcome and, for hover, the pointer can hover), all in `@layer components`; `#app` clips sideways | `tests/KCC.ViteTests/Features/Styles/pile.test.ts` |
| Tape only over a tile: no stylesheet or component style block styles `.kcc-tape` outside `.kcc-tilewrap > .kcc-tape` in either ramp, and every `kcc-tape` in `Features/**` markup is a child of a `kcc-tilewrap` | `tests/KCC.ViteTests/Features/Styles/pile.test.ts` |
| Every hand-written Razor `kcc-slip` ends with exactly one blank | `tests/KCC.ViteTests/Features/Styles/pile.test.ts` |
| Nothing runs longer than 500ms but a loop; 300ms is the default; reduced motion stills every transition and animation; a ramp switch freezes them, and the ramp switches only through `applyRamp` | `tests/KCC.ViteTests/Features/Styles/motion.test.ts` |

## Building a surface

1. Read the neighbouring component and its test. Keep every structural hook (`data-testid`, roles, ids); e2e
   locates by hook, not markup.
2. Sheets are `KccSheet` or the hand-written Structure, blank last. Pick the tear from the list index or a stable
   hash, mark a sheet wider than 700px `wide`, and flip a blank where the page's reading order needs it.
3. Every class comes from the Classes table or Tailwind; no component `<style>` rule for anything Razor
   also renders.
4. No `rounded-lg`+, `shadow-*`, `font-bold/semibold/medium`. Text size comes from a role class or a type token,
   vertical space from the rule utilities (see Type). Text is `text-ink` or `text-ink-soft`;
   `text-marker-ink` only inside a tile, a label or a marker button.
5. Icons: duotone by default, `currentColor`, no `fa-primary-*` / `fa-secondary-*`.
6. Numbers and meta in Sono: `kcc-num`, `kcc-kick`, `kcc-meta`, `kcc-stat`.
7. Motion takes the 300ms default, or up to 500ms for a bigger move (see Motion). Every focusable shows a ring,
   and only buttons have a disabled look (see States).
8. `ALLOWLIST` in `retiredTokens.test.ts` stays empty. If a surface genuinely needs an entry, the commit
   message says why.
9. Run, from `src/KCC.Web`: `yarn test <name> retiredTokens typeScale contrast motion`, `yarn type-check`, `yarn format`;
   before merging also `yarn build:all` and the browser check in both ramps. Commit.
