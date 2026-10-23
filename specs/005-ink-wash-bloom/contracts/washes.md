# Contract: washes in the kit

What an author writes and what the kit renders. This replaces the author-placed `kcc-wash` and `KccSheet`'s `at`.

## Markup

A top sheet's washes are one field, the sheet's first child:

```html
<div class="kcc-sheet">
  <span class="kcc-washes" aria-hidden="true">
    <span class="kcc-wash" data-wash="5c0e19a2" style="--wash-x:.281;--wash-y:.214;--wash-dx:.433;--wash-dy:.561;
      --wash-d:.881;--wash-aspect:1.012;--wash-s:1.406;--wash-turn:211.330deg;--wash-r1:41.220%;--wash-r2:63.002%;
      --wash-r3:35.871%;--wash-r4:58.400%;--wash-o1:47.100%;--wash-o2:31.900%;--wash-o3:60.020%;--wash-o4:44.300%;
      --wash-ink:var(--color-peach);--wash-ink-2:var(--color-sky);--wash-fallback-at:88.412s;--wash-peak:.912"></span>
    …up to three more
  </span>
  …content
</div>
```

and, once per page, in `<head>`:

```html
<style id="kcc-bloom-schedule">.kcc-wash[data-wash="5c0e19a2"]{--wash-at:37.084s}…</style>
```

`KccWashes` renders the field; `scheduleWashes` writes the schedule. Nothing else writes either by hand.

## `KccWashes` (`Components/Sheet/KccWashes.Component.vue`, global, so Razor can use it)

| Prop | Type | Default |
|---|---|---|
| `washKey` | `string` | required |
| `size` | `'card' \| 'reading' \| 'page' \| 'none'` | `'card'` |
| `chosen` | `Wash` | none |
| `washSlots` | `number[]` | none; only on a global (`@`) key, beats on the visit lattice |

Razor writes it as the sheet's first child: `<KccWashes wash-key="error" size="reading" chosen="red" />`.

It claims its qualified key from the bloom context in setup and releases it on unmount, so a sheet that mounts
again, or a list that reloads, takes the same key and arrangement rather than `~2`.

## `KccSheet` props

| Prop | Change |
|---|---|
| `wash` | Kept. Now the sheet's chosen colour: its first wash wears it (FR-015) |
| `at` | Removed, with every caller's `:at` |
| `washKey` | New, required. A fixed name per call site; in a list, the list's name and the item's own id, never its index |
| `bloom` | New. `'card' \| 'reading' \| 'page' \| 'none'`; when absent, `page` for `tear="hero"`, `reading` for `margin`, `page` for `wide`, else `card`, in that order (A32) |
| `washSlots` | New. Passed only by the pad's cards |

Every `KccSheet` blooms unless it passes `bloom="none"`. `Card.Component.vue` and `Stacker.Component.vue` pass
`none` when the backoffice wash is unset, so such a card stays bare paper (FR-010). The pad's `.pad-top` takes
`wash-key="@pad"` and fixed `wash-slots`; each pad card `@` plus its `id` (`@pad-card-recipes`, `@pad-card-kitchen`, `@pad-card-search`,
`@pad-card-find`, `@pad-card-menu`) and its own fixed slots, so its
arrangement and timing hold on every page, signed in or not. A hand-written top sheet (`.pad-top`, `DetailHero`,
`FeaturedRecipeCard`, `RecipeCard`, `RecipeCardRow`, `VariantGrid`, the four Razor sheets) adds a `KccWashes`. The
pad's `.pad-page`, every `.kcc-blank`, wells, tiles, labels, tape and controls never take one.

## Kit CSS (`Torn/Kit.css`, `@layer components`)

| Selector | Holds |
|---|---|
| `.kcc-washes` | `position: absolute; inset: 0; z-index: 0; overflow: clip; container-type: size; pointer-events: none; mix-blend-mode: var(--wash-blend)` |
| `.kcc-sheet > :where(:not(.kcc-washes))` | lifts content to `z-index: 1` (replaces `:not(.kcc-wash)`) |
| `.kcc-wash` | centre at `--wash-x`, `--wash-y` (`translate: -50% -50%`); width `max(0px, min(calc(var(--wash-d) * 100cqw), calc((hypot(calc(var(--wash-dx) * 100cqw), calc(var(--wash-dy) * 100cqh)) - 2 * var(--wash-reach)) / var(--wash-s))))`, height by `aspect-ratio`; the two radial gradients; `filter: url(#kcc-bloom) saturate(var(--wash-sat))`; `opacity: 0`; the bloom animation with delay `calc(var(--wash-at, var(--wash-fallback-at)) - var(--wash-elapsed, 0s) - var(--wash-lag, 0s))` |
| `@keyframes kcc-bloom` | `opacity: 0; scale: .72` → `opacity: calc(var(--wash-strength) * var(--wash-peak)); scale: 1` |
| `@keyframes kcc-bloom-fade` | opacity only, used under `prefers-reduced-motion: reduce` |
| ramp-switch freeze | excludes `.kcc-wash` |

**Fallback arrivals.** A wash that no schedule rule reaches blooms at `--wash-fallback-at`. A wash mounted after
hydration takes its start from the bloom context, which continues the page's arrival sequence,
`frac(c + j / φ) × 210s`, after the page's own washes. Any other wash takes one start per stratum of its sheet, clamped to
`[0, 210)`. Within a sheet, wash 0 starts first (research R6, A34).

A wash with no pair partner (a card's, or wash 1 of three) gets `--wash-dx: 9; --wash-dy: 9`, which puts its cap
far beyond its own size, so one rule serves every wash.

Tokens (`Torn/Tokens.css`): `--wash-blend` (multiply / screen, as today), `--wash-sat` (2.5 / 3),
`--wash-strength` (per ramp, pinned by `contrast.test.ts`), `--wash-bloom: 90s`, `--wash-reach` (twice the
`#kcc-bloom` bleed's `stdDeviation`, pinned by a test). `--wash-op` is retired.

`Main.css`: the reduced-motion rule's selector excludes `.kcc-wash`.

## Grain

| Allowed | Where |
|---|---|
| `--grain`, `--crayon` | declared in `Torn/Tokens.css` `:root` only; read by `.kcc-grain`, `.kcc-crayon` only |
| `.kcc-grain`, `.kcc-crayon` | first children of `#app`, `z-index: 0` |
| `url(#kcc-bloom)` | `.kcc-wash` only; defined in `Layout.cshtml` |
| `url(#kcc-wax-flat)` | `.kcc-tile` only; defined in `Layout.cshtml` |
| texture images (`data:image/svg`, `repeating-*-gradient`) | only in the rules above |
| the tape's woven stripe (`repeating-linear-gradient`) | `.kcc-tilewrap > .kcc-tape` and its dark form only: the tape's own material, exempt by name |

`grain.test.ts` resolves `var()` aliases and fails on any other use (FR-024).

## Authoring rules (for `docs/brand/kit.md`)

- **Must** give every `KccSheet` and `KccWashes` a stable `wash-key`; in a list, the item's id, never its index.
- **May** pass `wash` to choose a sheet's first colour, `bloom` when the size rule picks wrong, and `bloom="none"` for
  a sheet the owner left bare.
- **May** put copy, controls and labels anywhere on a sheet: washes lie under them, two deep at most, capped for AA.
- **Must not** write a `kcc-wash`, `kcc-washes` or a schedule rule by hand, place a wash, or set its colour beyond
  `wash`.
- **Must not** add a grain, crayon, texture image or `feTurbulence` layer, or an alias of one, to anything but the
  desk, a wash or a tile.
- **Must not** animate a wash from script, or change its `animation` after first render: a changed animation restarts.
