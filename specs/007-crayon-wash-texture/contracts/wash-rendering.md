# Contract: Wash Rendering

The planner's output and the `.kcc-wash` rule in `Features/Styles/Torn/Kit.css`. `KccWashes` and the schedule are
unchanged. `washField.test.ts` and `washes.test.ts` pin this contract.

## Planner (`planSheet` in `Features/Bloom/washes.ts`)

```ts
function centre(sheet, count, k, quadrants): [x: number, y: number]
// count 1: quadrant = hash(seed, key, 'corner') & 3, in place of quadrants[0]
// pushX   = hash(seed, key, k, 'axis') & 1
// band(far, u)  = far ? .84 + .18u : −.02 + .18u
// inside(far, u) = far ? .55 + .35u : .10 + .35u
// x = pushX ? band(q & 1, u) : inside(q & 1, u);  y = pushX ? inside(q & 2, v) : band(q & 2, v)
```

- `--wash-d` = `between(.9, 1.25, draw('d'))`.
- `--wash-o3`, `--wash-o4`: with `b` the drawn blob and `{ cx, cy, rx, ry } = BLOB_SHAPES[b]` (content-box fractions,
  imported from `Features/Bloom/blobShapes.ts`; never from `bakeWashes.ts`, which imports `node:zlib` and uses
  `Buffer`, while `washes.ts` ships in the client bundle through `KccWashes.Component.vue`, `KccSheet.vue` and
  `bloomContext.ts`),
  `o3 = cx + .25 · rx · (2 · draw('o3') − 1)` and `o4 = cy + .25 · ry · (2 · draw('o4') − 1)`. The draws keep their
  labels, so no other draw moves. The mirror flips the whole element, so the second ink needs no flip of its own.
- `--wash-flip` = `draw('flip') < .5 ? '-1' : '1'`.
- `--wash-o1`, `--wash-o2` are no longer written. `WashProperty` drops them and adds `--wash-flip`.
- `WASH_REACH_PX` retires; the pair-cap test reads the span from the bake.
- `washes.ts`'s static import graph holds no `node:` module and no Node builtin (`washesImports.test.ts`).

## `.kcc-wash`

| Property | Value |
|---|---|
| `--wash-w` | `max(0px, min(calc(var(--wash-d) * 100cqw), calc(hypot(calc(var(--wash-dx) * 100cqw), calc(var(--wash-dy) * 100cqh)) / (var(--wash-s) * var(--wash-span)))))` |
| `--wash-h` | `calc(var(--wash-w) / var(--wash-aspect))` |
| `--wash-tint`, `--wash-tint-2` | the ink saturated by `--wash-sat` in linear light, as today, without `--wash-tone` |
| `width` | `var(--wash-w)`, `box-sizing: content-box`, `aspect-ratio: var(--wash-aspect)` |
| `padding` | `calc(var(--wash-h) * (var(--wash-span) - 1) / 2) calc(var(--wash-w) * (var(--wash-span) - 1) / 2)` |
| background, top layer | second ink: `radial-gradient(closest-side at calc(var(--wash-o3) * 100%) calc(var(--wash-o4) * 100%), tint-2 / .26, tint-2 / 0)`, `no-repeat`, origin and clip `content-box` |
| background, bottom layer | first ink: flat `tint / .34` over the whole border box |
| `mask-image` | `var(--wash-chalk), var(--wash-blob)` |
| `mask-composite` | `intersect, add` |
| `mask-mode` | `alpha` |
| `mask-repeat` | `repeat, no-repeat` |
| `mask-position` | `0 0, center` |
| `mask-size` | `128px 128px, 100% 100%` |
| `transform` | `scale(var(--wash-flip), 1)` |
| `rotate`, `translate`, `left`, `top` | as today |
| `opacity` and the bloom's end | `calc(var(--wash-strength) * var(--wash-peak) * var(--wash-gain))` |
| `animation`, `animation-delay` | as today |
| `filter`, `mix-blend-mode`, `isolation`, `will-change`, `border-radius` | none |

`kcc-bloom-fade` (reduced motion) ends at the same opacity. `.kcc-washes` is unchanged.

## Guarantees

- No point lies under more than two washes: each pair's padded boxes' circles, radius `w · s · span / 2`, are
  disjoint, as `washes.test.ts` checks over a thousand seeds on three fields.
- The scripts-off first paint equals the scripts-enabled one: the style is server-rendered and no script touches a
  wash (FR-009).
- The texture appears at every step of the bloom at the wash's current size.
- The second ink lies inside its blob's ellipse: its centre is within a quarter of the blob's radii from the ellipse
  centre, so the blob mask keeps an estimated median 84% of its alpha (research R2: a Monte Carlo on the undisplaced
  profile, not measured on the bake). `washes.test.ts` measures it over the displaced bake: a median of at least .80
  and a 10th percentile of at least .70.
