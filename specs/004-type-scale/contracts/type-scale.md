# Contract: the type scale for authors

The public interface this feature exposes is the set of names a Vue component, a Razor view or a kit stylesheet may use
for text size and vertical rhythm. `docs/brand/kit.md` takes this contract over when the feature lands.

## An author MAY

- Size text with a role class: `kcc-display`, `kcc-section`, `kcc-heading`, `kcc-title`, `kcc-body`, `kcc-hand`,
  `kcc-kick`, `kcc-lbl`, `kcc-meta`, `kcc-unit`.
- Size text with a token utility where no role class fits: `text-caps`, `text-body`, `text-title`, `text-heading`,
  `text-section`, `text-readout`. These carry their own line height. The display size steps down below 768px, so
  it is reached only through `kcc-display` or a bare `h1`.
- Read a token in a kit stylesheet or a style block: `var(--text-<role>)`, `var(--text-<role>--line-height)`,
  `var(--bl)`, `var(--control-md)`, `var(--control-lg)`, `var(--pill-sm)`, `var(--icon-sm|md|lg)`.
- Size a standalone icon with `kcc-icon-sm`, `kcc-icon-md` or `kcc-icon-lg`. An icon beside text sets no size.
- Space blocks vertically in rule units: `mt-rule-1`, `gap-y-rule-1.5`, `space-y-rule-3`, and the other prefixes in
  `data-model.md`.

## An author MUST NOT

- Write a `font-size`, a `font` shorthand size or a px `line-height` outside the tokens.
- Use `text-[<length>]`, any `leading-*`, or a default size name (`text-xs` … `text-9xl`, which no longer generate).
- Space blocks vertically with fixed spacing utilities: `mt|mb|my|pt|pb|py|gap-y|space-y` with `3`, `6`, `9`, `12`,
  `18` or an arbitrary length, or a two-axis `gap-6`, `gap-9`, `gap-12`, `gap-18` or arbitrary `gap` on a grid or a
  column.
- Use `kcc-h3` or `kcc-h4`.

## Enforcement

| Rule | Test |
|---|---|
| Every size, line height and vertical rule spacing above | `tests/KCC.ViteTests/Features/Styles/typeScale.test.ts` (new) |
| The token values, the role classes' sizes and the phone step | `tests/KCC.ViteTests/Features/Styles/typography.test.ts` (rewritten) |
| `kcc-h3` and `kcc-h4` never return | `tests/KCC.ViteTests/Features/Styles/retiredTokens.test.ts` |
| Every kit class is in kit.md's table and defined in `Torn/*.css` | `tests/KCC.ViteTests/Features/Styles/kitClasses.test.ts` |
| Text contrast in both ramps | `tests/KCC.ViteTests/Features/Styles/contrast.test.ts` (unchanged) |
