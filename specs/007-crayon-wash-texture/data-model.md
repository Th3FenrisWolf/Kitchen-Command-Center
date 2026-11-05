# Data Model: Crayon Wash Texture

No stored data changes. The entities are the build-time bake, the planner's output per wash, and the render tokens.

## Baked texture set

Written by `yarn washes` (`Features/Bloom/generateWashes.ts`) from `Features/Bloom/bakeWashes.ts` into
`Features/Styles/Torn/Washes/`, named by the generated `Features/Styles/Torn/Washes.css`.

| Field | Value | Rule |
|---|---|---|
| `chalk` | `wash-chalk.png`, 128 x 128 texels, 1 per CSS px | Seamless; alpha `(blur₁(m) + .5) / 1.5`, row and column means levelled, in [.3, 1]; [contract](contracts/baked-textures.md) |
| `blobs[i]`, i in 0..7 | `wash-blob-<i>.png`, 90 x 90 texels | One silhouette each; alpha 1 near its ellipse's core, 0 on the texture's edge |
| `BLOB_SHAPES[i]` | ellipse centre (cx, cy) in [.28, .72], closest-side radii, corner radii in 38-64%, noise offset | Seeded per index; exported from `Features/Bloom/blobShapes.ts`, which imports nothing from Node, for the bake, the planner's second ink (client bundle) and the SC-007 test |
| `span` | 1.12 | Texture box over wash content box; `--wash-span` |
| `gain` | 1.5 | Undoes the tile's normalisation; `--wash-gain` |

Retired: `wash-grain.png`, `BLOOM_FILTER.grain`, `bakeGrain`, `--wash-tone`, `--wash-grain`, `--wash-blob-span`.

## Planned wash (`PlannedWash.style`, `Features/Bloom/washes.ts`)

| Property | Value | Change |
|---|---|---|
| `--wash-x`, `--wash-y` | One axis in the outer band of the quarter (−.02 to .16, or .84 to 1.02); the other in its half (.10 to .45, or .55 to .90) | Changed (FR-003a) |
| `--wash-dx`, `--wash-dy` | Offset to the pair partner, or `9` | Unchanged |
| `--wash-d` | .90 to 1.25 of the field's width | Changed (FR-003b) |
| `--wash-aspect`, `--wash-s`, `--wash-turn` | As today | Unchanged |
| `--wash-blob` | `var(--wash-blob-<0..7>)` | Unchanged |
| `--wash-flip` | `1` or `-1`, from `hash(seed, key, k, 'flip')` | New |
| `--wash-o1`, `--wash-o2` | | Retired: the ellipse offset is baked per blob |
| `--wash-o3`, `--wash-o4` | Second ink's centre in the content box: `cx + .25 · rx · (2u − 1)`, `cy + .25 · ry · (2v − 1)`, with the drawn blob's ellipse from `BLOB_SHAPES` and `u`, `v` the existing `o3`, `o4` draws | Changed: inside the blob's ellipse, so the mask keeps most of the second ink (research R2) |
| `--wash-ink`, `--wash-ink-2`, `--wash-fallback-at`, `--wash-peak` | As today | Unchanged |

Validation (Vitest): output is byte-identical for the same input; `id` stays `hash(key, k)`; the schedule's starts for a
page are unchanged from today; a one-wash card lies in a quarter; every wash pushes exactly one axis into its band;
`d` in range; every blob and both flips are drawn; every second ink's centre lies within a quarter of its blob's radii
from the blob's ellipse centre; paired circles stay disjoint under the new cap; at least three quarters of washes are
cut by an edge on the representative fields (SC-007); `washes.ts`'s import graph holds no Node module
(`washesImports.test.ts`).

## Render tokens

| Token | Light | Dark | Source |
|---|---|---|---|
| `--wash-strength` | its new cap when under .42, estimated .38 to .40 | its new cap, about .13 | `Tokens.css`, pinned by `contrast.test.ts`; the owner sees both projected values in the step-2 look round (plan) |
| `--wash-sat` | 2.5 | 3 | Unchanged |
| `--wash-blend` | multiply | screen | Unchanged |
| `--wash-reach` | | | Retired |
| `--wash-span`, `--wash-gain` | 1.12, 1.5 | same | `Washes.css`, generated |

## Contrast model (`tests/KCC.ViteTests/Features/Styles/contrast.test.ts`)

| Constant | Today | New |
|---|---|---|
| First ink alpha | `FIRST_INK_RIM_ALPHA` .345 | `FIRST_INK_CORE_ALPHA` .34 |
| Second ink alpha | .26 | .26 |
| Texture term | `GRAIN_GAIN` multiply 1.26, screen .905 | `TEXTURE_GAIN` per blend, fitted to the re-read table, times `--wash-gain` |
| `RENDERED_INK_SOFT` | rows from the grain bake | rows re-read from the chalk bake (research R9) |

## State

A wash's states are unchanged: pending (before its arrival), blooming (45 steps over 90 s, scale .72 to 1, opacity 0
to `strength × peak × gain`), at rest. The texture is present in every state at the wash's current size.
