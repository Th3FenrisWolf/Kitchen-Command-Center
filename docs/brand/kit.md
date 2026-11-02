# Loose Leaf — the kit

The engineering contract for [loose-leaf.md](loose-leaf.md): tokens, classes, structure, invariants,
and the test that enforces each rule. Anything Razor also renders lives in global `@layer components` CSS,
never in a component `<style>` block.

## Files

| Path | Holds |
|---|---|
| `src/KCC.Web/Features/Styles/TailwindConfig.css` | `@theme static` role tokens (light values), the type, control, pill and icon tokens, radius ladder, fonts, safelist; the `*-rule-*` spacing utilities |
| `src/KCC.Web/Features/Styles/Torn/Tokens.css` | kit-only properties, among them the rule `--bl`, its drop `--rule-drop` and offset `--rule-offset`, and the baseline constants `--k-*`; the dark ramp under `:root[data-theme='dark']` |
| `src/KCC.Web/Features/Styles/Torn/Tears.css` | **generated** tear presets (`yarn tears`). Never hand-edited |
| `src/KCC.Web/Features/Styles/Torn/Washes.css` | **generated** by `yarn washes`: the grain's tone and the wash textures, named by relative `url()` on `.kcc-wash`. Never hand-edited |
| `src/KCC.Web/Features/Styles/Torn/Washes/` | **generated** wash textures (`yarn washes`): `wash-grain.png`, the grain tile, and `wash-blob-0.png` to `wash-blob-7.png`, the eight bled blob masks. The client build keeps them out of the stylesheet and `Layout.cshtml` preloads them in production. Never hand-edited |
| `src/KCC.Web/Features/Styles/Torn/Kit.css` | slip / torn / blank / sheet / wash field and wash / label / tape / tile, the ruling, the margin rule, the shim, `kcc-row`, type, chrome, ramp swap |
| `src/KCC.Web/Features/Styles/Torn/Controls.css` | btn, seg, field, range slider, badge, check, stats, steps, recipe-slip parts |
| `src/KCC.Web/Features/Styles/Typography.css` | the 16 / 24 body, the type roles of bare `h1`–`h6` in APCasual, form controls at the body size, every `@font-face` including Sono, each with its pinned metrics (see Paper rule) |
| `src/KCC.Web/Features/Styles/Layout.css` | the `.content-grid` breakout system: content, breakout and full-width columns for pages and sections |
| `src/KCC.Web/Features/Styles/Main.css` | the stylesheet import graph; every `Torn/*.css` file is imported here; `#app` carries the desk colour and, as its own background layers, the desk's stripe, crayon and grain, blended with `background-blend-mode`; it clips sideways overflow |
| `src/KCC.Web/Features/Types/DesignSystem.ts` | `WASHES` and the `Wash` type; the colour axes the safelist test checks |
| `src/KCC.Web/Features/Torn/tornPolygon.ts` | pure tear generator |
| `src/KCC.Web/Features/Torn/tears.ts` | preset table and CSS emitter |
| `src/KCC.Web/Features/Torn/generateTears.ts` | CLI that writes `Tears.css` |
| `src/KCC.Web/Features/Bloom/bakeWashes.ts` | the wash filter's numbers, a port of `feTurbulence`, and the baker that draws the grain and blob masks as PNGs |
| `src/KCC.Web/Features/Bloom/generateWashes.ts` | CLI that writes `Washes.css` and its textures |
| `src/KCC.Web/Features/Bloom/washTextures.ts` | the texture file names, and the Vite inline limit that keeps them out of the stylesheet |
| `src/KCC.Web/Features/Components/Sheet/KccSheet.vue` | the sheet primitive for Vue |
| `src/KCC.Web/Features/Components/Sheet/KccWashes.Component.vue` | a sheet's wash field, global so Razor can use it; `KccSheet` renders one as its first child |
| `src/KCC.Web/Features/Bloom/` | the bloom: `washes.ts` (pure: sheet size, each wash's numbers, the arrival schedule), `bloomContext.ts` (key claims, the page's washes, late arrivals), `visitHeartbeat.ts`, and the visit in C# (`Visit`, `VisitCookie`, `VisitClock`, `BloomInputs`) |
| `src/KCC.Web/Features/Pages/Shared/Layout.cshtml` | renders a signed-in member's saved ramp or runs the pre-paint ramp script; font preloads, the `#kcc-wax-flat` filter def, `--wash-elapsed` on `<html>`, the schedule's `<style>` in `<head>`, `#kcc-bloom-schedule` |
| `src/KCC.Web/Features/Utilities/Ramp.ts` | `applyRamp`, the one way to switch the ramp after the first paint; `deviceRamp` and `rampFor`, the ramp a device or a saved setting asks for |

## Tokens

Reach tokens through Tailwind utilities (`bg-paper`, `text-ink-soft`) or `var(--color-*)` in kit CSS. Never
a literal colour. Light is the `@theme` value; dark overrides live in `Torn/Tokens.css`.

| Token | Light | Dark | Use |
|---|---|---|---|
| `desk`, `desk-2` | `.884 .016 288`, `.836 .018 288` | `.132 .024 288`, `.176 .028 288` | page ground, raised desk |
| `paper`, `paper-2` | `.958 .009 92`, `.972 .008 92` | `.258 .030 288`, `.222 .028 288` | sheets; paper-2 for inset panels inside a sheet and the neutral `kcc-well` |
| `ink`, `ink-soft` | `.232 .026 288`, `.432 .024 288` (test-pinned; mockup .452) | `.945 .012 92`, `.76 .018 288` | text; kicks, meta, placeholders |
| `hair`, `hair-strong` | ink / .16, ink / .56 (test-pinned; mockup .4) | chalk / .18, chalk / .45 | dividers; control hairlines, link and field underlines |
| `rule` | `.698 .048 239.5 / .55` | `.74 .05 240 / .24` | the feint blue ruling |
| `margin` | `.631 .178 20 / .75` | `.66 .15 22 / .58` | the margin rule and a field's X; never text |
| `marker`, `marker-ink` | `.876 .112 126`, `.24 .03 288` | same | the accent fill and the ink that sits on it and on a tile |
| `fiber`, `fall` | `1 .006 92`, `.25 .03 288 / .28` | `.435 .034 288`, `.04 .02 288 / .7` | the two drop-shadows on `.kcc-fall`, `.kcc-blank` and a tile's `.kcc-torn` |
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
| `--control-sm` | 2rem (32px) | the width and height of `kcc-btn--icon`, at every button size |
| `--control-md`, `--control-lg` | 2.25rem, 3rem (36px, 48px) | the height and line of `kcc-btn` and `kcc-seg`, and the hit area of `kcc-field`; of `kcc-btn--lg` |
| `--pill-sm` | 1.5rem (24px) | the height and line of `kcc-badge`, `kcc-label`, the pad's count and key hint |
| `--icon-sm`, `--icon-md`, `--icon-lg` | 1.5rem, 2.25rem, 3rem (24px, 36px, 48px) | `kcc-icon-sm`, `kcc-icon-md`, `kcc-icon-lg`, the tile glyphs, the pad glyph; counted apart from the text sizes |

Kit-only properties (`Torn/Tokens.css`, not Tailwind tokens): `--bl` 1.5rem (24px), the rule every line height and
vertical space is counted in; `--k-hazelnut` .38208, `--k-casual` .25 and `--k-sono` .312, each face's baseline
constant (see Paper rule); `--rule-drop` (`calc(var(--bl) / 2 + var(--k-hazelnut) * var(--text-body))`, how far
below a line's top the body baseline sits); `--rule-offset` 2px, how far below that baseline each rule lies; `--rd`
6px (labels, pills, wells), `--wash-blend` (multiply / screen), `--wash-strength` (.42 / .18; dark's is the cap at which ink, soft ink and
control hairlines hold over two washes at their peak, read over text's 3px average, test-pinned; light's sits under its cap, set by eye), `--wash-bloom` 90s, `--wash-ease` (the bloom curve as a `linear()` staircase of 45 holds, generated by and pinned in `motion.test.ts`), `--wash-reach` 24px (twice the
baked bleed's `stdDeviation`, and each wash's padding; test-pinned), `--wash-sat` (2.5 / 3), `--grain` and `--crayon`, the desk's two textures, each ramp's
strength baked into its SVG (grain .08 / .16, crayon .255 / .306). Per element: `--pad` (sheet),
`--tear` (a tile or photo tile; set by its tile preset class), `--r` and `--blank-dir` (set by a sheet preset class), `--blank-turn`, `--blank-scale` and
`--blank-flip` (set on the slip and its modifiers), `--blank-region` (set on a region that turns its sheets' blanks
over), `--k`, `--lh` and `--own` (set by the kit on each text role for the shim), `--h` and `--centre-size` (a
control's height and the Sono size it centres on), `--c` (a tile's wash), and the `--wash-*` properties `KccWashes` writes on each wash (centre, size, shape, rotation, inks, peak, arrival), with `--wash-elapsed` on `<html>`.

**Retired** and caught by `retiredTokens.test.ts`: `ink-line`, `ink-on-wash`, `hatch`, `edge`,
`edge-strong`, `flap-1`, `flap-2`, `link`, `danger`, `success`, `warning`, `danger-ink`, `success-ink`,
`warning-ink`, `rating-ink`, the washes `rosewater flamingo mauve maroon sapphire blue`, every `shadow-*`
utility, `rounded-lg` and larger, `font-bold`, `font-semibold`, `font-medium`, the `fa-primary-*` /
`fa-secondary-*` Tailwind utilities (the kit sets the Font Awesome custom properties itself; see Icons),
every `sk-*` class, `v-ink`, `data-ink`, the two heading classes named by level that the type roles replace, the
field modifier that dropped a field's icon (the X is every field's one leading mark).
Nothing keeps them alive: the Softbound tokens, the ink module and the `TRANSITIONAL` blocks are gone, and the
test's `ALLOWLIST` is empty. A new entry there needs a reason in the commit message.

**Radius ladder:** `rounded-xs` 3px (checkbox), `rounded-sm` 4px (small marks), `rounded-md` 6px (labels,
wells), `rounded-full` (pills). Sheets, tiles and images take no radius.

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

## Paper rule

Every line on a sheet is written on its ruling: the baseline of each line of text, the text of each control and the
line of each field sits on a rule. CSS does it, from the type alone, in Vue and Razor alike; no author nudges
anything.

**Pinned metrics.** Every `@font-face` in `Typography.css` carries `ascent-override`, `descent-override` and
`line-gap-override: 0%` from its hhea table, so every platform reads the same ascent and descent and a baseline lands
at `line-height / 2 + k * font-size`, where `k = (ascent - descent) / 2`. Without them APCasual drifts about 0.04em
on Windows, whose OS/2 table disagrees with the hhea.

| Family | Faces | ascent / descent override | k | Constant |
|---|---|---|---|---|
| Hazelnut | 400, 700, italic, bold italic | 101.0742% / 24.6582% | 0.38208 | `--k-hazelnut` |
| APCasual | regular, italic | 75% / 25% | 0.25 | `--k-casual` |
| Sono | latin, latin-ext | 91.2% / 28.8% | 0.312 | `--k-sono` |

**The ruling.** `.kcc-sheet` draws a 1px `--color-rule` line every `--bl` from its **content box**, at
`calc(var(--rule-drop) + var(--rule-offset))`: `--rule-drop` puts it on the first body baseline and `--rule-offset`,
2px, lays the top of each rule just under it, in the descender band. Because it hangs from the content box, padding
in any form, `--pad` or a utility, moves the text and the rules together. Body text needs nothing more. A sheet is
ruled; `kcc-sheet--blank` (`KccSheet blank`) drops the rules and keeps the rhythm, so its text still sits where they
would be. `kcc-sheet--scroll` keeps the ruling in its own scrolling background. The blank under a sheet
is never ruled.

**The shim.** One kit rule moves every other text role onto the nearest rule with a signed `translate`, which changes
no layout and survives the rule utilities' margins, which sit in a later layer:

```css
--own: calc(var(--lh) / 2 + var(--k) * 1em);
translate: 0 calc(mod(calc(var(--rule-drop) - var(--own) + var(--bl) / 2), var(--bl)) - var(--bl) / 2);
```

It applies on a sheet to bare `h1`–`h6`, `kcc-display`, `kcc-section`, `kcc-heading`, `kcc-title`, `kcc-hand`,
`kcc-kick`, `kcc-lbl`, `kcc-meta`, `kcc-unit`, `kcc-num`, `.kcc-q`, `.kcc-n`, `kcc-stat`, `.kcc-v`, `kcc-badge` and the
`text-caps`, `text-title`, `text-heading`, `text-section` and `text-readout` utilities. `--lh` comes from the role or
the token utility, and `--k` from the face: the sheet sets `--k-hazelnut`, the APCasual roles and `font-casual` set
`--k-casual`, and the Sono roles and `font-mono` set `--k-sono`. The shift lies in (−12px, 12px], so it reaches the
nearest rule either way:

| Role | Face | Size / line | Shift |
|---|---|---|---|
| body, `kcc-body` | Hazelnut | 16 / 24 | none |
| `kcc-display`, `h1` | APCasual | 40 / 48, 36 / 48 below 768px | +8.1px, +9.1px |
| `kcc-section`, `h2` | APCasual | 32 / 48 | +10.1px |
| `kcc-heading`, `h3` | APCasual | 24 / 24 | +0.1px |
| `kcc-title`, `kcc-hand`, `h4`–`h6` | APCasual | 20 / 24 | +1.1px |
| caps roles, `text-caps` | Sono | 14 / 24 | +1.7px |
| `text-readout` | Sono | 64 / 72 | −9.9px |

`translate` moves no inline box, so a `kcc-num` or a `text-caps` span inside a line of body text stays on that line's
baseline; only a block, a flex item or a grid item moves. A role inside another role, a button, a segment or a `kcc-control`
rides on the outer shift and takes none of its own. The shift assumes the element's line box starts on a line of the sheet,
so blocks are spaced in rule units (see Type) and a block is a whole or half number of rules tall, a half only where
another half pairs with it.

**The margin rule.** `kcc-sheet--margin` (`KccSheet margin`) draws a 1px `--color-margin` line in `::before`, two
rules in from the sheet's left edge, and starts the copy one rule after it (`padding-inline-start: calc(var(--bl) *
3)`). The slip of a margin sheet is an inline-size container, and below 480px of sheet width the line goes and the
start padding returns to `--pad`, so a form in a narrow column drops it as a phone does. Only margin slips are
containers, because inline-size containment stops a slip sizing to its content. It goes on sheets read down the
page: the recipe page's At a glance; the variant page's At a glance, ingredients, instructions, nutrition, cook notes
and reviews (the summary and the form; a single review in the list is a card); every wizard step and wizard sheet; the account, login and settings sheets and `RegistrationComplete`;
cook mode. The wizard's centred status sheets (`ReadingState`, `SubmittedSheet`) carry it too, their centred content
shifting with the copy inset. Never on a dialog (`LeaveConfirm`, `PhotoViewer`), a hero, a toolbar, a card, the header
pad, a home block, the stacker or an empty state.

**Rows of controls.** A label centred beside a taller control lands off the rule, which the shim cannot see. A row of
36px controls and their labels is a `kcc-row`: a wrapping flex row, top-aligned, 12px apart, half a rule between
lines and below the last, so each line fills two rules and the text below is back on the rule. A label or a `kcc-heading` ends
half a rule down, so its row line fills whole rules however many lines it runs to, and a text or icon button or a
`kcc-control` in the row keeps a control's height in its margin box. A `kcc-check` row that runs onto a second line keeps its box and its amount on its first.

**Controls on the line.** On a sheet `kcc-btn` and `kcc-seg` shift to centre on the Sono caps of the line they start
on, so the label beside them and their own text share a rule:

```css
translate: 0 calc(mod(calc(var(--rule-drop) - var(--k-sono) * var(--centre-size) - var(--h) / 2 + var(--bl) / 2), var(--bl)) - var(--bl) / 2);
```

`--h` is the control's height (`--control-md`, `--control-lg`, `--control-sm` on an icon button, `--bl` on a text
button), and `--centre-size` the Sono size it centres on, `--text-caps` unless the line beside it is larger (cook
mode's servings buttons take `[--centre-size:var(--text-title)]`, the size of their count). A control the kit does not
draw takes `kcc-control` and the same shift: it sets `--h` when it is not 36px, and `--centre-size` when its own line
is not Sono caps (the pad's menus and search glyph centre on a Hazelnut body line, so they take
`calc(var(--k-hazelnut) / var(--k-sono) * var(--text-body))`). On a sheet a ghost button,
a segmented control, a field, a badge and a `kcc-control` fill with paper, so no rule runs through them; a ghost
button keeps that fill on hover, and a `kcc-control` in its `kcc-pill` state takes the marker instead. An action beside a display title sits one rule down (`pt-rule-1 pb-rule-0.5` on its wrapper), so its text
shares the title's baseline rule. A badge is one rule tall (`--pill-sm`), its caps on the rule. The header pad's
sheets are ruled, and the text in its bar and its card footers sits on their rules like any other sheet.

**Icon buttons.** `kcc-btn--icon` is a 32px circle (`--control-sm`) at every button size, `kcc-btn--lg` included:
the camera shutter, the photo viewer's close, cook mode and its timer. 32px is above WCAG 2.5.8's 24px target.

**Fields are lines.** A `kcc-field` is a line to write on: no radius, no ring, no fill of its own, its children
aligned to the start. Its underline is a 1px `hair-strong` line where the sheet's rule would be, at
`calc(var(--rule-drop) + var(--rule-offset))`, and 2px `ink` on `:focus-within`. Its input or select takes
`line-height: var(--bl)`, so the text sits on the rule, and the field keeps `height: var(--control-md)` as its hit
area. Before the line, `::before` writes an X: APCasual at `--text-title`, `--color-margin`, turned −6deg, with the
title role's shim. It is the field's one leading mark, so no icon goes before an input. It is decorative: `content:
'X' / ''` keeps it out of the label's accessible name. `kcc-field--stepper` (`NumberStepper`'s count) is set with
buttons, not written, so it draws no X. `kcc-field--area` has no box: its transparent textarea writes on the sheet's
own rules at `line-height: var(--bl)`, the X before its first line, and focus draws a 2px `ink` outline with no
radius. `kcc-field--error` draws the underline 2px in `ink`, because red reads under 3:1 on paper; the
`kcc-well--danger` message below carries the red.

**The grain.** Grain and crayon texture lie on three things only: the desk, the washes and the tiles. The desk takes
them as `#app`'s own background layers, blended with the desk colour and each other by `background-blend-mode`
(normal and multiply in light; soft-light and normal in dark) and painted once under every sheet, with no overlay
element and no `isolation`. A faint white diagonal stripe layer lies first among them, unblended in light, where multiply
would draw nothing. A wash takes
the baked grain mask `Washes/wash-grain.png` and a tile the grain of `#kcc-wax-flat`. Paper, text, inputs and every other control,
labels, wells, blank sheets and tape carry none, so a wash lying under the paper is never muddied by a layer above it.
The tape's woven stripe, a `repeating-linear-gradient` on `.kcc-tilewrap > .kcc-tape`, is the tape's own material, not
grain, and `grain.test.ts` exempts it by name. `--grain` and `--crayon` are declared in `Torn/Tokens.css`, once per ramp, and nowhere
else, and the test fails on any other use, alias, texture image or `feTurbulence` layer.

**Known limits.** A textarea scrolled by pixels shows its lines between the rules until it scrolls back to a whole
line, so `TextAreaField`'s drag handle snaps its height to whole rules. Where `content: 'X' / ''` is unsupported
(Safari before 17.4, Firefox before 128), the fallback X is read with the field's label.

The rules for authors ([the paper rule contract](../../specs/003-paper-rule-scheme/contracts/paper-rule.md)):

- **May** pick a ruling: a `kcc-sheet` is ruled, `kcc-sheet--blank` is not.
- **May** add the margin rule to a sheet read down the page, with `KccSheet margin` or `kcc-sheet--margin` in Razor.
- **May** set text in any role or token utility (see Type) and get it on the rule.
- **May** read `--rule-drop`, `--rule-offset`, `--k-hazelnut`, `--k-casual`, `--k-sono`, `--color-rule`,
  `--color-margin` and `--control-sm` in a kit stylesheet or a style block.
- **Must not** write a pixel nudge to put text on the rule: the shim does it.
- **Must not** centre a label beside a control with `items-center`: use `kcc-row`.
- **Must not** put an icon before a field's input: the X is the field's one leading mark.
- **Must not** give a block a height that is not a whole or half number of rules, unless a half rule pairs with
  another.
- **Must not** put the margin rule on a hero, a toolbar, a card, the header, a home block or an empty state.

## Structure

Paper lies in piles two high: every sheet sits on a blank sheet, torn and shadowed like it, with nothing on it. A
labelled sheet on its blank, with its wash field:

```html
<div class="kcc-slip kcc-tear-3">                 <!-- tilt via --r; position: relative; height: fit-content -->
  <span class="kcc-fall" aria-hidden="true"><span></span></span>    <!-- fibre + fall: filter here, clip on the inner span -->
  <div class="kcc-torn">                          <!-- no filter: the washes bloom inside it -->
    <div class="kcc-sheet" style="--pad: 48px">   <!-- paper, clipped by its slip's tear preset, 24px feint ruling -->
      <span class="kcc-washes" aria-hidden="true">  <!-- the wash field, first child, rendered by KccWashes -->
        <span class="kcc-wash" data-wash="5c0e19a2" style="--wash-x:.281;--wash-y:.214;--wash-ink:var(--color-peach);…"></span>
      </span>                                         <!-- up to four washes, two deep at most -->
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
  <span class="kcc-fall" aria-hidden="true"><span></span></span>
  <div class="kcc-torn">
    <div class="kcc-sheet">…</div>
  </div>
  <span class="kcc-blank" aria-hidden="true"><span></span></span>
</div>
```

A recipe slip:

```html
<article class="kcc-slip kcc-recipe kcc-tear-5">
  <span class="kcc-fall" aria-hidden="true"><span></span></span>
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

1. `clip-path` sits on `.kcc-sheet` / `.kcc-tile`. The `filter` sits **outside** the clipped element, or the
   shadow is clipped away with it. A sheet's fall is its own element, `.kcc-fall`, the sibling right before
   `.kcc-torn`: the filter on its wrapper, the sheet's tear on its inner span. It holds nothing, so its shadow is drawn
   once, while a filter around the sheet would draw again on every frame of every wash blooming inside. A slip's
   `.kcc-torn` carries no filter; a tile's `.kcc-torn` holds nothing that moves and keeps its own.
2. Label and tilewrap are siblings of `.kcc-torn` inside `.kcc-slip`: outside the clip **and** outside the
   fall, so they are neither torn nor shadowed. The tape sits inside the tilewrap, never directly in the slip.
   The one exception is a `.kcc-tilewrap--flow`, which sits in the sheet's content flow with its tape (a list
   row's tile). The blank is a sibling too, and always the
   slip's **last** child, once: outside the top sheet's clip and fall, with its own filter on `.kcc-blank` and
   its own clip on the inner span. It is never a `kcc-slip`, `kcc-torn` or `kcc-sheet`, so a one-element slip
   (`kcc-slip kcc-torn` on one element) cannot hold one.
3. Every direct child of `.kcc-sheet` except `.kcc-washes` is lifted above the wash field by the kit at class
   specificity (`:where`). A kit class that positions a sheet child itself is written `.kcc-sheet > .kcc-x`
   so it wins regardless of import order (see `kcc-stat`).
4. The ruling hangs from the sheet's content box (see Paper rule), so padding in any form, `--pad` or a utility,
   moves the text and the rules together. `--pad` stays **one length** on a margin sheet: below 480px its start
   padding falls back to `var(--pad, 24px)`, which a shorthand (`24px 36px 24px 24px`) makes invalid. Content
   starts at the top of the content box, so a sheet's first line sits on its first rule.
5. One wash field (up to four washes), one label, one hand note per sheet at most. The field is `KccWashes`'s, the
   sheet's first child, never written by hand (see Washes).
6. `clip-path` clips descendants too. Keep interactive children at least 4px inside the sheet edge (the
   default 24px padding does this), or the focus ring is shaved by the tear.
7. `transform` on `.kcc-slip` makes the slip the containing block for `position: fixed` descendants. Fixed UI (dialogs, cook mode, full-screen panels) is teleported to `body`
   or kept outside sheets; never inside a slip.
8. `clip-path` clips everything inside the sheet, including a dropdown or a sticky toolbar that moves past
   the edge. Menus and popovers that must overflow live outside the sheet.
9. A sheet's tear is a `clip-path` that `Tears.css` writes once per preset on grouped child selectors
   (`.kcc-tear-N > .kcc-torn > .kcc-sheet`, `> .kcc-fall > span`, and the `> .kcc-blank > span` of each slip that names the
   preset as its blank), so it never inherits into a nested slip or a pinned tile; `--r` is registered
   non-inheriting, so nested slips never compound a tilt.
10. A slip placed directly in `main.content-grid` (or any grid) with `mx-auto max-w-*` needs `w-full` too:
    auto margins suppress a grid item's stretch, so without it the sheet shrink-wraps its copy and the
    washes, placed in percentages of the sheet, drift with content length.
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

## Washes

A sheet starts bare and gains colour as the visit goes on. Washes are not placed by authors: `KccWashes` derives each
one's numbers from the visit's seed and the sheet's own key, and the kit CSS draws it from custom properties.

**The visit.** A visit is one reader's stay: a visit cookie, named in `VisitCookie`, holding the seed and start time (30-minute sliding expiry, not
HttpOnly) that every page renews, with a once-a-minute heartbeat while a page is open. `Layout.cshtml` writes the elapsed
time as `--wash-elapsed` on `<html>`; the seed and the page's key go to the SSR sidecar. Each wash's `animation-delay` is
its arrival minus `--wash-elapsed` (and `--wash-lag` for a sheet mounted after hydration), so a page opened mid-visit
paints with the washes the visit has earned and blooms on from there, with no script. A visit that has been away more
than 30 minutes starts again, bare and in a new arrangement. Same page, same visit: same arrangement.

**The sheets.** Every top sheet blooms unless it passes `bloom="none"`; blank sheets, wells, tiles, labels, tape and
controls never do. Size sets the count: `card` one or two washes, `reading` three, `page` four. `bloom` picks it, or
the sheet's props do: `bloom`, then `tear="hero"` to `page`, `margin` to `reading`, `wide` to `page`, else `card`. A Home card
or stacker card whose backoffice wash is unset passes `none` and stays bare paper.

**The washes.** Each has a pseudo-random size, shape, rotation, position and one or two of the eight brand washes as
they are, never blended. Positions cover the sheet in strata and pair up in opposite quadrants; the kit caps each
wash against its partner, so no point lies under more than two. A sheet's chosen colour (`wash`: a recipe's colour on its
recipe surfaces, a Home card's backoffice colour) goes to its first wash. A wash may lie under running text and bleed
past the sheet's margin, never past its tear. `--wash-strength` caps it so ink and soft ink meet AA, and hairlines 3:1, over two
washes at their peak in both ramps, judged on text's 3px average. The dried edge is a lift in the first ink's alpha, never a darker colour.

**Baked, not filtered.** A wash carries no filter: an SVG filter on a blooming wash made WebKit draw it offscreen on
every step. `yarn washes` bakes the bleed and grain of the filter numbers in `bakeWashes.ts` into PNGs in `Washes/`, which `Washes.css` names: a 64px grain tile (its alpha
term, `k1 = k2 = .62`, clipped to 1), eight blob masks (a border-radius blob displaced by the low turbulence and bled,
picked per wash by `--wash-blob`) and `--wash-tone`, the grain's mean darkening of each colour. The first ink's stops
draw the bleed into the wash's `--wash-reach` padding, and both inks are saturated by `--wash-sat` in linear light,
toned, in the kit CSS. The grain's per-channel tint is not baked; its mean is. The textures are separate cached files, not
data URIs: base64 PNG barely gzips, so inlined they weighed more in the stylesheet than they do as files. The client build
never inlines them, and `Layout.cshtml` preloads them in production with `crossorigin`, because a mask fetches in CORS
mode, so a bloomed first paint has its washes.

**The schedule.** Washes arrive one at a time on golden-ratio slots across 210 seconds by hash rank, each spreading
over 90 seconds, so every page is fully bloomed at five minutes and none starts after 3 minutes 30. The visit has sixteen beats; the pad and its cards
name theirs in markup (`wash-slots`), so the pad blooms on the same clock on every page; page washes keep
clear of them. `scheduleWashes` writes the page's arrivals once, after the render, as the `<style>` in `<head>`, `#kcc-bloom-schedule`. Reduced motion keeps the schedule and fades each wash in at its final size.

**Keys.** Every `KccSheet` and `KccWashes` carries a stable `wash-key`, qualified by the page path unless it starts with
`@` (the pad and its cards, the same on every page). A repeat on one page takes `~2`, `~3` with a development warning.

The rules for authors ([the washes contract](../../specs/005-ink-wash-bloom/contracts/washes.md)):

- **Must** give every `KccSheet` and `KccWashes` a stable `wash-key`: a fixed name per call site, and in a list the
  item's own id, never its index (`keys.test.ts` scans every `.vue` and `.cshtml`).
- **May** pass `wash` to choose a sheet's first colour, `bloom` when the size rule picks wrong, and `bloom="none"` for a
  sheet the owner left bare.
- **May** put copy, controls and labels anywhere on a sheet: washes lie under them, two deep at most, capped for AA.
- **Must not** write a `kcc-wash`, a `kcc-washes` or a schedule rule by hand, place a wash, or set its colour beyond
  `wash`.
- **Must not** add a grain, crayon, texture image or `feTurbulence` layer, or an alias of one, to anything but the desk,
  a wash or a tile.
- **Must not** animate a wash from script or change its `animation` after the first render: a changed animation
  restarts.

## Tears

`Tears.css` writes each sheet polygon once, as `clip-path` on the grouped selectors of the sheet, the fall's inner span and the
blank inner span of every slip that names the preset as its blank. It declares `--tear-tile-1 … 3` on `:root`, for the
tiles and the photo strip, and the classes that select them:

| Class | Points / side | Amp | Chamfer | Tilt `--r` | Blank polygon, `--blank-dir` |
|---|---|---|---|---|---|
| `kcc-tear-1` | 40 | 2.4 | 19px top-right | +0.30 | `tear-4`, −1 |
| `kcc-tear-2` | 40 | 2.4 | 17px bottom-right | −0.98 | `tear-5`, +1 |
| `kcc-tear-3` | 40 | 2.4 | none | +0.05 | `tear-6`, −1 |
| `kcc-tear-4` | 40 | 2.4 | 18px bottom-left | −0.52 | `tear-1`, +1 |
| `kcc-tear-5` | 40 | 2.4 | 20px bottom-right | +0.63 | `tear-2`, −1 |
| `kcc-tear-6` | 40 | 2.4 | 17px bottom-left | −0.70 | `tear-3`, +1 |
| `kcc-tear-hero` | 80 | 2.4 | 28px top-right | −0.58 | `tear-hero-2`, −1 |
| `kcc-tear-hero-2` | 80 | 2.4 | 24px bottom-left | −0.36 | `tear-hero`, +1 |
| `kcc-tear-tile-1..3` | 24 | 1.8 | none | (a pinned tilewrap −2°; a `--flow` one 0) | none |

Each sheet preset names its blank: the tear three along the cycle, so a sheet never lies on its own tear, and the
other hero tear for a hero. A standard blank turns against its sheet's tilt, so the directions alternate around the
cycle and a list that takes `(index % 6) + 1` alternates by itself. The blank turns `--blank-turn` (1.5°, or 0.5°
wide) × `--blank-dir` × `--blank-flip` × `--blank-region` about its centre and scales `--blank-scale` (1.015, or
1.005 wide).

A polygon of 40 points a side (80 on a hero, 24 on a tile) is the most the edge needs: more only costs the clip's rasterising, drawn once per preset and shared by the sheet, the fall and the blanks that name it. Depth is constant but the wavelength stretches with the box, so each family has a size band: tile tears for
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
| `#app` background | the desk's grain and crayon tooth, painted under every sheet | `Main.css` `#app` and its dark rule: `--crayon` and `--grain` layers with `background-blend-mode`; no overlay element in `App.vue`; no sheet, control or label lies under a texture of its own (see Paper rule); a surface teleported to body (cook mode) is outside `#app` and untextured |
| `kcc-slip` (+`--fill`) | tilt + positioning context; `--fill` stretches slip, torn and sheet to a definite-height parent | `--r` from the tear preset; without `--fill` a child sized in percentages computes to auto |
| `kcc-torn` | the wrapper outside a clip: a sheet's carries no filter, a tile's the fibre and fall | wraps exactly one clipped element |
| `kcc-fall` › `span` | a sheet's fibre and fall: the filter on the wrapper, the sheet's tear on the span, nothing on it | in a slip, the sibling right before `.kcc-torn`, once; `aria-hidden`; the same box as the top sheet, unturned; lifts with it |
| `kcc-blank` › `span` | the blank sheet under a sheet: torn paper with the fibre and fall, no ruling | last child of a slip; `aria-hidden`; tear and base direction from the slip's tear class; turns `--blank-turn` × direction × `--blank-flip` × `--blank-region` and scales `--blank-scale`; the span is `display: block`; stays still when the slip lifts |
| `kcc-slip--wide` | the wide pair: the blank turns 0.5° and scales 1.005 | hero-torn sheets on a blank, the pad's page, any sheet on a blank wider than 700px |
| `kcc-slip--flip` | turns the slip's blank the other way | set where a page's reading order needs it |
| `kcc-slip--lift` | a slip you can open lifts its top sheet 4px on focus within, or on hover with a pointer that can hover; the blank stays | no lift under reduced motion; keeps the focus ring's fade |
| `kcc-sheet` | paper, clip, ruling, padding | the ruling hangs from the content box; no grain or crayon lies over it; `--pad` default 24px, heroes 48px; compact rows (recipe rows, review slips, the account sheet) take 16px and accept a ruling 8px off a 24px neighbour's |
| `kcc-sheet--blank` | a sheet with no ruling, at the same rhythm | text still sits where the rules would be |
| `kcc-sheet--scroll` | a sheet that is its own scroll container (the phone pad cards): the ruling scrolls with its content | `KccSheet`'s `scroll` prop; its wash field covers the first scrollport and scrolls with the content |
| `kcc-sheet--margin` | the red margin rule two rules in, copy one rule after it | `KccSheet`'s `margin` prop; reading sheets only (see Paper rule); its slip is an inline-size container, and below 480px of sheet width the rule and its padding go |
| `kcc-washes` | a sheet's wash field: clips its washes, sizes them as a size container, and blends them onto the paper as one group | rendered by `KccWashes` (or `KccSheet`) as the sheet's first child, `aria-hidden`; never written by hand; every other child of the sheet is lifted above it |
| `kcc-wash` | one wash: one or two brand inks that bloom over 90 seconds on the visit clock | rendered by `KccWashes` with its `--wash-*` properties and a `data-wash` id; never written or placed by hand; at most two deep anywhere on a sheet |
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
| `kcc-btn` (+`--ghost`, `--ink`, `--text`, `--lg`, `--icon`) | marker pill 36px (`--control-md`); hairline ghost; ink fill; underlined text on the rule; 48px large (`--control-lg`); `--icon` a 32px circle (`--control-sm`) at every size, `--lg` included | Sono caps 14px `.14em`; on a sheet it centres on the caps of the line it starts on, and a ghost fills with paper; an `--icon` button is all glyph, so its name goes in `aria-label` |
| `kcc-control` | a control the kit does not draw, placed on the line like `kcc-btn` | sets `--h` (36px) and `--centre-size` (`--text-caps`) for the shift; fills with paper on a sheet unless it is a `kcc-pill`; keeps a control's height in a `kcc-row`; the pad's menus and search glyph |
| `kcc-row` | a wrapping row of 36px controls and their labels, top-aligned, 12px apart, half a rule between lines and below the last | each line fills two rules, so the text below is back on the rule; use it in place of `items-center` beside a control; a label or `kcc-heading` ends half a rule down, and a text or icon button or a `kcc-control` keeps one |
| `kcc-seg` › `kcc-seg-thumb`, `button[aria-pressed\|aria-checked]` | pill group 36px (`--control-md`), Sono caps 14px, pressed = ink fill | pressed = ink fill; in `SegmentedControl.vue` the fill is an ink thumb that slides to the checked segment once the client has placed it (`kcc-seg--sliding`), and until then the checked button carries it; `kcc-seg-thumb--glide` turns the slide on after each placement that must not slide; an unpressed segment goes from ink-soft to ink on hover; labels never wrap; the group scrolls horizontally when narrower than its segments; on a sheet it fills with paper and centres on the caps of its line |
| `kcc-field` (+`--area`, `--stepper`) | a line to write on: a 1px `hair-strong` underline where the sheet's rule lies, a 36px (`--control-md`) hit area, the margin-red X in `::before`; `--area` writes a textarea on the sheet's rules with no box; `--stepper` is `NumberStepper`'s count, set with buttons rather than written, so it draws no X | field text Hazelnut 16/24 on the rule; no leading icon; on a sheet it fills with paper; focus = 2px ink underline, 2px ink outline with no radius on `--area` |
| `kcc-range` › `kcc-range-track`, `kcc-range-fill` | paper track with an inset hairline, marker fill, ink thumbs with a paper ring | both thumbs are native range inputs stacked over the track; the ring keeps the thumb off the marker fill |
| `kcc-badge`, `kcc-badges` | 24px (`--pill-sm`) hairline pill, Sono caps 14px; wrapping row | one rule tall, its caps on the rule; on a sheet it fills with paper |
| `kcc-check` › `li` › `kcc-box` (+`--on`), text, `kcc-q`; `li.kcc-done` | checklist on the rule | a row that runs on keeps its box and amount on its first line; the checkbox is `sr-only`, so a focused checkbox rings its box instead |
| `kcc-stats` › `div` › `kcc-lbl` + `kcc-v` | stat row, values Sono 24/24 (`--text-heading`) | `<small>` for the unit, Sono caps 14px |
| `kcc-steps` › `li` › `kcc-n` + `kcc-body` | numbered method | numbers `01`, `02`, … |
| `kcc-well` (+`--danger`, `--success`, `--warning`) | paper tinted 28% with the wash, ink text, 6px radius, no grain | status messages; the message itself is a `kcc-kick` |
| `kcc-field--error` | the field's underline 2px in ink; on `--area` a 2px ink outline | red reads under 3:1 on paper, so a `kcc-well--danger` message below the field carries it; `InputField`, `TextAreaField` and `SelectField` add it from their `error` prop |
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

`kcc-well--danger` (red), `kcc-well--success` (green), `kcc-well--warning` (yellow). A field in error takes
`kcc-field--error`, which draws its underline 2px in ink, and puts the message in a `kcc-well--danger` directly below
it: the well carries the red. `InputField`, `TextAreaField` and `SelectField` add the class, with `aria-invalid`, when
their `error` prop is set; inside `Field`, bind it from the slot's `error`. Only a field the message is about takes
it, with an `aria-describedby` that names the well: `LoginView` marks the name and the password when the credentials
are wrong and the two passwords when they do not match, and an error about the account, such as one waiting for
approval or locked out, or one the sign-up server reports, stands in the well alone. Icons inside a
well are duotone in ink like everywhere else. `contrast.test.ts` asserts ink on every well in both ramps.

## Icons

Font Awesome duotone by default; `fa-solid` / `fa-regular` where the simpler glyph reads better (empty rating
stars are `fa-regular`). `Torn/Kit.css` sets `--fa-primary-color` and `--fa-secondary-color` to `currentColor` and
`--fa-secondary-opacity` to `.35` globally (`.4` inside tiles, labels and marker buttons). Components never set
the Font Awesome custom properties and never use the `fa-primary-*` / `fa-secondary-*` utilities.

## States

- **Hover.** `kcc-btn` mixes 12% `marker-ink` into its marker, and `kcc-btn--ink` mixes 12% paper into its ink. The
  hairline of `kcc-btn--ghost` turns ink, its paper fill kept on a sheet, and the underline of `kcc-btn--text` and `kcc-link` goes from `hair-strong`
  to ink. An unpressed `kcc-seg` segment and `kcc-link--icon` go from `ink-soft` to ink. A slip you can open lifts,
  and a pad menu fans its card out with no underline of its own (see Motion). A row or search result in a pad card
  underlines its name in ink, and the hairline of a Try chip, or of a diet chip in the phone menu, turns ink.
- **Focus.** A 2px ink outline, 2px out, on every focusable (`:focus-visible` in `TailwindConfig.css`). `kcc-field`
  draws its underline 2px in ink on `focus-within`, with no ring, and `kcc-field--area`, which has no underline,
  takes a 2px ink outline with no radius. A `kcc-check` rings its `kcc-box`, because the checkbox itself is
  `sr-only`. A slip that is a link rings its `kcc-torn`, outside the tear. Under `forced-colors: active`, which
  paints no box-shadow and no gradient, a focused field and segment take a 2px `CanvasText` outline, a field's
  underline is a 1px `CanvasText` border and an area a 1px `CanvasText` outline. The
  `kcc-range` thumbs have no focus ring yet.
- **Pressed, current, checked.** A pressed or checked `kcc-seg` segment fills with ink. In `SegmentedControl.vue` the
  fill is the `kcc-seg-thumb`, which slides from the old segment to the new one. The current wizard step and
  an open menu button are `kcc-pill`. The section you are in is a marker underline on its menu, 5px of marker
  behind the label, hidden while that menu is open. A checked box is `kcc-box--on`, and a done checklist row
  (`li.kcc-done`) is struck through.
- **Disabled.** A button, and a `kcc-check` row whose checkbox is disabled: 45% opacity and a `not-allowed` cursor,
  and a button loses its hover. Nothing else in the kit has a disabled look, so a field, segment, range or a
  checkbox outside a `kcc-check` row gets one here before anything disables it.
- **Error.** `kcc-field--error` draws the field's underline 2px in ink, and a `kcc-well--danger` below it carries the
  red (see Status).
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
| The pad tucks away as you read | 300ms `cubic-bezier(.2, .8, .2, 1)`: up by its height plus 28px (142px for the 114px pad) as you scroll down, once the page is past 150px; back on any scroll up or when focus moves into it; never under reduced motion | `AppHeader.Component.vue` |
| Sticky neighbours follow the pad | `transition-[top]`: their top, which adds `--pad-offset`, eases over 300ms as the pad tucks and comes back, and a stacker card's sentinel eases with it | `RecipeSearchView.Component.vue`, `StackerBlock.cshtml`, `Stacker.Component.vue` |
| A pad menu fades to and from its marker pill | `background-color` and `color` over 300ms, keeping the focus ring's 0.2s fade | `AppHeader.Component.vue` |
| A pad menu's chevron flips | 180° over 300ms while its card is open | `AppHeader.Component.vue` |
| The desktop search field widens as it takes focus | 300ms `cubic-bezier(.2, .8, .2, 1)`, from 250px to 330px (200px to 240px between 768px and 1023px) | `AppHeader.Component.vue` |
| A row's name in a pad card underlines | `text-decoration-color` from transparent to ink over 300ms on hover | `PadLines.vue` |
| A stacked sheet steps back | `transition-all duration-100`, scale from 1 to .95 once it sticks, all but the last | `Stacker.Component.vue` |
| A segment's ink fill slides to the checked segment | `transform` and `width` over 300ms, and the labels' colour fades with it; no slide for the first placement, after a resize, when the fonts load or when the labels change | `SegmentedControl.vue`, `Controls.css` |
| A wash blooms | the bloom keyframes: opacity 0 to its peak and scale .72 to 1 over 90s (`--wash-bloom`), in 45 steps that crowd the start, where the curve climbs fastest (`--wash-ease`), delayed to its arrival on the visit clock, filling backwards only, so a finished bloom leaves the wash on its resting style; under reduced motion the fade keyframes, opacity alone | `Torn/Kit.css`, `KccWashes.Component.vue` |
| The focus ring fades in | `transition: outline-color 0.2s, outline-offset 0.2s` on every focusable | `TailwindConfig.css` |
| More recipes load | `fa-spin` on a `fa-circle-notch` | `RecipeSearchView.Component.vue` |

1. **300ms is the standard.** `--default-transition-duration` in `Main.css` gives it to every `transition-*`
   utility that names no duration, which is most of them.
2. **500ms is the ceiling**, for a bigger move that would feel rushed at 300ms, like a pad card coming out. A loop such
   as the spinner is exempt: its duration is one turn, not a wait. So is the bloom, the kit's one non-loop animation
   over 500ms: a wash arrives over 90 seconds by design, and `motion.test.ts` allows that one entry.
3. **Any property, any easing.** Tailwind's default curve unless a move wants another.
4. **Reduced motion stills everything CSS runs but the washes.** Under `prefers-reduced-motion: reduce`, `Main.css` sets every
   transition and animation to 0.01ms (not `none`, so `transitionend` and `animationend` still fire) and turns
   smooth scrolling off. `Main.css` leaves a wash out: under reduced motion it fades in at its final size on the same schedule (the fade keyframes), and the page is as bloomed at five minutes as for every other reader. Motion started from script (`element.animate`, a `requestAnimationFrame` loop, an animation
   library) never reaches that rule, so it checks `matchMedia('(prefers-reduced-motion: reduce)')` itself.
5. **A ramp switch freezes motion for a frame.** `applyRamp` (`Utilities/Ramp.ts`) sets `data-theme-switching` on
   the root and clears it only after a frame has rendered with it, and `Kit.css` drops every transition and
   animation under it but a wash's, whose `animation: none` would restart its bloom, so a wash keeps its arrangement and progress and only the blend changes; no token eases between the ramps. Everything that switches the ramp after the first
   paint goes through it.

## Vue primitives

- `Components/Sheet/KccSheet.vue` — props `label` or a `#label` slot for rich content (a `<ResourceString>`
  keeps its editor hooks that way), `icon`, `labelRight`, `washKey` (required, see Washes), `wash` (the sheet's chosen colour), `bloom` (`card | reading | page | none`), `washSlots` (the pad's cards only), `tear` (`1..6 | 'hero'`;
  `'hero'` also adds `kcc-slip--wide`), `piled` (default `true`; `false` renders no blank, and only `PadCard.vue`
  passes it), `wide` (`kcc-slip--wide`), `flip` (`kcc-slip--flip`), `pad`, `crisp` (no tilt: forms, cook mode),
  `margin` (`kcc-sheet--margin`), `blank` (`kcc-sheet--blank`), `scroll` (`kcc-sheet--scroll`), `as` (`div |
  section | article | li`). Default slot; the blank renders after the label. Plain `.vue`: import it
  locally. Renders the Structure above with no client JS.
- `Components/Sheet/KccWashes.Component.vue` — a sheet's wash field, global so Razor can write it as the sheet's first
  child: `washKey` (required), `size` (`card | reading | page | none`), `chosen` and `washSlots`. It claims its key from
  the bloom context once in setup and releases it on unmount, so a remounted sheet keeps its arrangement.
- `Components/Forms/InputField.vue` — a `kcc-field` label around one input, on the line with the X; `v-model`,
  `inputClass` for the input, and `error`, which adds `kcc-field--error` and `aria-invalid`. Every attribute but
  `class` and `style` lands on the input; those two style the field. No icon.
- `Components/Forms/TextAreaField.vue` — a `kcc-field--area` around a textarea; `v-model`, `required`, `readonly`,
  `placeholder` and `error`. Three rules tall, and its drag handle snaps the height to whole rules, two at least.
- `Components/Forms/SelectField.vue` — a `kcc-field` around one select, its options in the default slot, and `error`.
  Every attribute but `class` and `style` lands on the select.
- `Components/Forms/NumberStepper.vue` — a `kcc-field--stepper` between two 32px ghost icon buttons.
- `Components/Forms/Field.vue` — a `kcc-lbl` label over its control, with a hint, or an error in a
  `kcc-well--danger`. Its slot hands the control `describedby` and `error`.
- `Components/Button/Button.vue` — `variant: 'marker' | 'ghost' | 'ink' | 'text'`, `size: 'md' | 'lg'`,
  `as: 'button' | 'a'`. Emits the `kcc-btn` classes; no directive.
- Everything else composes the classes directly.

## Razor

Razor writes the Structure by hand, blank included, and `kcc-sheet--margin` and `kcc-sheet--blank` where a
sheet takes them. `ButtonLinkTagHelper` emits `kcc-btn` plus the modifiers in
`Class`. Loops pick tears from their index: `kcc-tear-@((i % 6) + 1)`. A page built from blocks, like Home, runs one
cycle through all its sheets and cards instead, so neighbours never share a tear across two sections either. A
stacker's sheet is sticky and travels, so the cycle skips its tear for the stack's cards and for the first two sheets
or cards of the next section, where the sheet comes to rest: its half of the width sits over two cards at most,
because grids stop at four columns. A skip passes the tear after it too, so the blanks keep alternating: odd and even
tears turn their blanks opposite ways. When that next section is another stacker, its own sheet's window takes over
after the sheet, so its first card may repeat the first sheet's tear, corner to corner. A Razor sheet that blooms writes
`<KccWashes wash-key="error" size="reading" chosen="red" />` as its first child (the error page, registration
complete, the Home rich-text and stacker sheets, whose keys carry their Block List item's key). The tiles' filter def, `#kcc-wax-flat`, is
inline in `Layout.cshtml`, so server-rendered tiles are filtered before hydration; the washes carry no filter. The same layout resolves the visit, writes `--wash-elapsed` on
`<html>` and the SSR sidecar's schedule as a `<style>` tag, `#kcc-bloom-schedule`, so the first paint carries the washes the
visit has earned.

## Invariants

1. **Build both bundles together**: `yarn build:all`. Scoped-style hashes must match across client and SSR.
2. **`@theme static`**: the `static` keeps unreferenced tokens alive for the kit CSS and the dark ramp.
3. **Razor-rendered → `@layer components`**, never a scoped `<style>`.
4. **A rule that must beat a utility is unlayered** (the ramp-swap rule at the end of `Kit.css`).
5. **`Tears.css` is generated.** Change `tears.ts`, run `yarn tears`, commit both.
6. **No runtime JS draws anything.** The tear, the wash, its bloom and the shadow are CSS; SSR output is final: the server writes each wash's numbers and arrival, and the bloom runs from the first paint with no script. The only script the bloom has is the visit's heartbeat, which renews a cookie.
7. **Both ramps, every change.** The light ramp is the default; dark is where washes and hairlines fail first.
8. **Filter outside clip; label and tape outside both**, except a `.kcc-tilewrap--flow` and its tape, which sit in
   the sheet's content flow. See Structure.
9. **Every sheet lies on a blank**, the slip's last child, outside the top sheet's clip and filter. A sheet is never
   taped; tape holds only a tile. The header pad is the one exception (Structure rule 15).
10. **No nudge to the rule.** The shim puts text on the rule and `kcc-row` puts labels beside controls on it; a pixel
    offset to land a line is a bug in the block's height or spacing (see Paper rule).
11. **Grain on the desk, the washes and the tiles only.** Nothing else takes a grain, crayon or texture layer; the
    tape's weave is its own material. `grain.test.ts` fails otherwise.
12. **Every sheet has a stable `wash-key`.** A list item's own id, never its index (see Washes).

## Enforcement

| Rule | Test |
|---|---|
| WCAG AA in both ramps, including ink over paper + wash and over a two-wash stack at its peak (text's 3px average, `--wash-sat` applied, a grain gain calibrated to a Chromium render), which pins the dark `--wash-strength` and caps the light one; control hairlines 3:1 over it | `tests/KCC.ViteTests/Features/Styles/contrast.test.ts` |
| No retired token, class, utility or directive in `Features/**` or CMS content | `tests/KCC.ViteTests/Features/Styles/retiredTokens.test.ts` (`ALLOWLIST` is empty; a new entry needs a reason in the commit message) |
| Committed `Tears.css` equals the generator; every sheet polygon written once on grouped `clip-path` selectors, none as a custom property; every tile declared on `:root` with a selecting class; sheet tilts written to `--r`; one or two standard sheets uncut; every sheet preset writes its blank (the polygon of another preset, never its own, and `--blank-dir`, against its tilt and alternating around the cycle; the hero pair named) and a tile none | `tests/KCC.ViteTests/Features/Torn/tears.test.ts` |
| Tear geometry: deterministic, every vertex inside the box for every shipped preset, seam closes within a step, chamfer never top-left, amp and point-count guards | `tests/KCC.ViteTests/Features/Torn/tornPolygon.test.ts` |
| `DesignSystem.ts` axes match the safelist | `tests/KCC.ViteTests/Features/Types/DesignSystem.test.ts` |
| Every Font Awesome style used is imported | `tests/KCC.ViteTests/Features/Styles/mainCssIconStyles.test.ts` |
| The type, control, pill and icon token values and the cleared default sizes; `--bl` 24px and `--rule-drop`; `body` and the form controls at the body size; the bare headings' roles; the display's phone step and the section's one size; every referenced font file | `tests/KCC.ViteTests/Features/Styles/typography.test.ts` |
| Every `font-size` reads a text or icon token; a `font` shorthand sizes with a text token; a `line-height` is the rule, a token or `1`; no Tailwind text size outside the scale and no `leading-*`; vertical spacing in rule units; at most eight text sizes besides the readout | `tests/KCC.ViteTests/Features/Styles/typeScale.test.ts` |
| Every class in the Classes table is defined in `Torn/*.css` and every class the Torn CSS defines is in the table; `box-shadow` appears only as a hairline or a ring; one rule casts the drop-shadows, on the fall, the blank and a tile's `.kcc-torn` | `tests/KCC.ViteTests/Features/Styles/kitClasses.test.ts` |
| A disabled `kcc-check` row takes the disabled button's opacity and cursor; no `transform` or `translate` in the kit CSS nudges by 1 to 3px | `tests/KCC.ViteTests/Features/Styles/kitStates.test.ts` |
| The pile's kit CSS: the slip's `fit-content` and blank defaults, the blank's turn, scale, fibre, fall and unruled paper, the fall's box and tear, no filter on a slip's `.kcc-torn`, the flip and wide pairs, the lift (every child but the blank, 4px, only when motion is welcome and, for hover, the pointer can hover), all in `@layer components`; `#app` clips sideways | `tests/KCC.ViteTests/Features/Styles/pile.test.ts` |
| Tape only over a tile: no stylesheet or component style block styles `.kcc-tape` outside `.kcc-tilewrap > .kcc-tape` in either ramp, and every `kcc-tape` in `Features/**` markup is a child of a `kcc-tilewrap` | `tests/KCC.ViteTests/Features/Styles/pile.test.ts` |
| Every hand-written Razor `kcc-slip` ends with exactly one blank; every slip that holds a sheet, in a component or in Razor, holds one fall, right before its `.kcc-torn` | `tests/KCC.ViteTests/Features/Styles/pile.test.ts` |
| The pinned metrics and `--k-*`; the ruling from the content box at `--rule-drop` + `--rule-offset` and the blank sheet; the shim's formula and role list; `--color-rule` and `--color-margin` in both ramps; the margin rule, its container and its 480px query; `kcc-row` and the checklist row; the controls' shift and paper fills; the field as a line, its X, its focus and error lines and the area; the 32px icon button; no `::after` grain on a sheet, the scroll sheet's one ruling layer, `#kcc-wax-flat` the only filter def and `--wash-reach` at `WASH_REACH_PX` | `tests/KCC.ViteTests/Features/Styles/paperRule.test.ts` |
| Real baselines on the rule in a browser, at 375px and 1280px: every text block, field and control row label on the recipe, variant, search, login, account, settings, cook mode and wizard pages, the header pad, a Razor sheet and a list whose rows run on, to their last lines; the heading action; the margin rule's reach; no sideways scroll | `tests/KCC.E2ETests/Features/Chrome/RuleTests.cs` |
| Nothing runs longer than 500ms but a loop; 300ms is the default; reduced motion stills every transition and animation; a ramp switch freezes them, and the ramp switches only through `applyRamp`; the bloom is the one entry over 500ms, its keyframes, the fade under reduced motion and the washes left out of both stilling rules | `tests/KCC.ViteTests/Features/Styles/motion.test.ts` |
| Grain and crayon only on the desk, a wash and a tile: `--grain` and `--crayon` declared in `Tokens.css` only, `var()` aliases resolved, any texture image (a baked PNG included) counted, `#kcc-bloom` nowhere and `#kcc-wax-flat` on `.kcc-tile` only, the tape's weave exempt by name, the desk's textures are `#app`'s own background layers, each with its blend mode (multiply in light, the stripe normal, soft-light in dark), no overlay element and no `isolation` | `tests/KCC.ViteTests/Features/Styles/grain.test.ts` |
| The wash field's CSS: `.kcc-washes` clips, contains and blends as a group, content lifted above it, the wash's width cap, aspect, padding, alphas and rim; no `.kcc-wash` rule carries a filter and `Layout.cshtml` defines no `#kcc-bloom`; the baked masks and the linear-light saturation of the inks | `tests/KCC.ViteTests/Features/Styles/washField.test.ts` |
| Committed `Washes.css` equals the generator and names the textures, no data URIs; the committed textures match the bake by pixels, no file more or less, under 16 KB together; one blob per shape `washes.ts` draws; the grain tile's alpha term and tone; each blob solid inside and bled out; the bleed at half `--wash-reach` and the grain composite at k1 = k2 = .62 | `tests/KCC.ViteTests/Features/Bloom/bakeWashes.test.ts` |
| Each wash's numbers from the seed and key: counts by size, strata, pairs, colours from the eight, the chosen colour first, the schedule's even spread | `tests/KCC.ViteTests/Features/Bloom/washes.test.ts`, `bloomContext.test.ts` |
| Every `KccSheet` and `KccWashes` has a `wash-key` that never reads a list index, and no `KccWashes` sits inside a blank, well, tile, label or tape | `tests/KCC.ViteTests/Features/Bloom/keys.test.ts` |
| The client build never inlines a wash texture and leaves every other asset to Vite's default; `Layout.cshtml` preloads the textures as CORS images outside Development | `tests/KCC.ViteTests/Features/Bloom/washTextures.test.ts` |
| The server-rendered page paints with the washes the visit has earned and hydration changes none | `tests/KCC.ViteTests/Features/Bloom/firstPaint.test.ts` |
| The visit: cookie format, clock, seed, the heartbeat | `tests/KCC.UnitTests/Features/Bloom/`, `tests/KCC.ViteTests/Features/Bloom/visitHeartbeat.test.ts`, `tests/KCC.IntegrationTests/Features/Pages/VisitTests.cs` |
| Mid-bloom, in both engines, no element with a `filter` holds a running animation | `tests/KCC.E2ETests/Features/Performance/DeterministicGuardTests.cs` |
| In a browser: bare at the start, fully bloomed at five minutes, earned washes in place before hydration and with scripts off, reduced motion fades without growth; every field's arrangement differs and every ink is one of the eight; washes add nothing to any scroll range; bare paper has no grain and the desk does | `tests/KCC.E2ETests/Features/Chrome/BloomTests.cs`, `BloomArrangementTests.cs`, `WashOverflowTests.cs`, `GrainTests.cs` |

## Building a surface

1. Read the neighbouring component and its test. Keep every structural hook (`data-testid`, roles, ids); e2e
   locates by hook, not markup.
2. Sheets are `KccSheet` or the hand-written Structure, blank last, each with a stable `wash-key`. Pick the tear from the list index or a stable
   hash, mark a sheet wider than 700px `wide`, and flip a blank where the page's reading order needs it.
3. Every class comes from the Classes table or Tailwind; no component `<style>` rule for anything Razor
   also renders.
4. No `rounded-lg`+, `shadow-*`, `font-bold/semibold/medium`. Text size comes from a role class or a type token,
   vertical space from the rule utilities (see Type). Text is `text-ink` or `text-ink-soft`;
   `text-marker-ink` only inside a tile, a label or a marker button.
5. Text lands on the rule by itself: no pixel nudge. Controls beside labels go in a `kcc-row`, fields are
   `InputField`, `TextAreaField`, `SelectField`, `NumberStepper` or `kcc-field` markup with no leading icon, and a sheet read down
   the page takes the margin rule (see Paper rule).
6. Icons: duotone by default, `currentColor`, no `fa-primary-*` / `fa-secondary-*`.
7. Numbers and meta in Sono: `kcc-num`, `kcc-kick`, `kcc-meta`, `kcc-stat`.
8. Motion takes the 300ms default, or up to 500ms for a bigger move (see Motion). Every focusable shows a ring,
   and only buttons and `kcc-check` rows have a disabled look (see States).
9. `ALLOWLIST` in `retiredTokens.test.ts` stays empty. If a surface genuinely needs an entry, the commit
   message says why.
10. Run, from `src/KCC.Web`: `yarn test <name> retiredTokens typeScale contrast motion keys grain`, `yarn type-check`, `yarn format`;
   before merging also `yarn build:all` and the browser check in both ramps. Commit.
