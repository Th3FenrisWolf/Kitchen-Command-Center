# Data model: Ink Wash Bloom and the Grain Rule

Nothing is stored in the database. The only state is one cookie; everything else is derived per render.

## Visit (server, C#: `Features/Bloom/Visit.cs`)

| Field | Type | Rule |
|---|---|---|
| `Seed` | `uint` | Random per visit, from `RandomNumberGenerator`. Kept for every page of the visit (FR-008) |
| `StartedAt` | `DateTimeOffset` | Server clock at the visit's first page, whole seconds |
| `Elapsed` | `TimeSpan` | `now - StartedAt`, clamped to `[0, 300s]`. Derived, never stored |

**Transitions**

```text
no cookie | malformed | expired ──page──▶ new Visit (fresh Seed, StartedAt = now)
live cookie ──page──▶ same Visit, cookie renewed for 30 min
live cookie ──heartbeat (client, once a minute while a page is open)──▶ cookie renewed for 30 min
30 min with no page and no heartbeat ──▶ cookie expires in the browser; next page starts a new Visit (FR-005)
```

**Validation**: the cookie value must parse as the format in [contracts/visit.md](contracts/visit.md);
`StartedAt` in the future or more than a year back is malformed. A malformed value never throws; it starts a visit.

## Bloom context (render, TS: `Features/Bloom/bloomContext.ts`)

Provided once per Vue app (server render and client hydration alike).

| Field | Type | Source |
|---|---|---|
| `seed` | `number` (uint32) | SSR payload `bloom.seed` |
| `page` | `string` | SSR payload `bloom.page`: request path, lower-case, no trailing slash, `/` for home |
| `claimKey(key)` | `(string) => string` | Returns the key, or `key~2`, `key~3` for a repeat on the page, the lowest suffix no mounted sheet holds (A27) |
| `releaseKey(key)` | `(string) => void` | Frees a claimed key when its sheet unmounts, so a remount or a reloaded list takes the same key back |
| `collect(wash)` | `(PlannedWash) => void` | Records each wash for the schedule; read by the server after the render |
| `lag()` | `() => number` | 0 during hydration; seconds since page load for a sheet set up after it (R13) |
| `lateStarts(n)` | `(number) => number[] \| undefined` | `undefined` during hydration; after it, the next `n` starts of the page's arrival sequence, continuing after the page's own washes (R6, A13, A34) |

Without a provided context (a component test, the backoffice) the defaults are seed 0, page `''`, lag 0, and
collection off.

## Sheet bloom (input to `planSheet`)

| Field | Type | Rule |
|---|---|---|
| `key` | `string` | Required `washKey`. `@`-prefixed keys are global; others become `${page}|${washKey}` (FR-009, A12) |
| `size` | `'card' \| 'reading' \| 'page' \| 'none'` | `bloom` prop, else `page` for `tear="hero"`, `reading` for `margin`, `page` for `wide`, else `card`, in that order (FR-011, A32). `none` renders no field (FR-010, A26) |
| `chosen` | `Wash \| undefined` | `wash` prop: a recipe's `washFor`, a Home card's backoffice `wash`, or a colour the component names (FR-015, A5) |
| `slots` | `number[] \| undefined` | Global sheets only: their beats on the visit lattice (A25) |

## Wash (output of `planSheet`, written as inline custom properties and one data attribute)

| Field | Where | Range | Requirement |
|---|---|---|---|
| id | `data-wash` | `hash(key, k)`, 8 hex digits, no seed | the schedule's selector |
| position | `--wash-x`, `--wash-y` | 0–1 of the field, jittered inside the wash's stratum | FR-012 even cover |
| pair offset | `--wash-dx`, `--wash-dy` | partner's centre minus this one, fractions of the field | FR-017 two deep |
| size | `--wash-d` | 0.45–1.25 of field width, before the pair cap | FR-012 |
| aspect | `--wash-aspect`, `--wash-s` | 0.68–1.32; `s = sqrt(1 + 1/aspect²)` | FR-012, FR-017 |
| rotation | `--wash-turn` | 0–360deg | FR-012 |
| radii | `--wash-r1` … `--wash-r4` | 32%–68% | FR-012 |
| first ink | `--wash-ink` | `var(--color-<wash>)` from `WASHES` | FR-014, FR-015 |
| second ink | `--wash-ink-2` | `var(--color-<wash>)` from `WASHES` | FR-014 |
| ink offsets | `--wash-o1` … `--wash-o4` | 28%–72% | the prototype's two-ink offset |
| fallback arrival | `--wash-fallback-at` | a late wash: its start from `lateStarts`, wash 0 the earliest of its sheet's; otherwise `(k + p) / n × 210s` with a jitter of at most `0.3 / n`, `p` from the key's hash, clamped to `[0, 210)` | R6 step 6, A34 |
| strength | `--wash-peak` | 0.75–1, times the ramp's `--wash-strength` | FR-017 |

**Counts**: card 1 or 2, reading 3, page 4, none 0; never more than 4 (FR-011). Wash 0 wears `chosen`.

**Strata and pairs**: 1 wash, the middle half; 2, opposite halves on a hashed diagonal; 3 or 4, one quadrant each,
with washes 0 and 2, and 1 and 3, in opposite quadrants. Pair partners never touch once the CSS cap applies.

**Determinism**: same seed, page, key, size and chosen colour give byte-identical output on every engine (R1).
Numbers are written to three decimals.

## Schedule (output of `scheduleWashes`, a `<style>` in `<head>`)

| Input | Rule |
|---|---|
| Visit lattice | 16 beats, `(j + 0.5 + 0.3 (u_j − 0.5)) × 210s / 16`, in whole milliseconds |
| Arrival sequence | candidate `j` at `frac(c + j / φ) × 210s`: `c = hash(seed, page)`, `j × 0x9E3779B9` added mod 2^32, floored to the millisecond |
| Block radius | `0.75 × 210s / B`, `B` the power of two ≥ `2n + 32` within [64, 512], `n` the page washes; a candidate within it of any visit beat is skipped |
| Rank | page washes by `hash(seed, key, k)`, ties by key, then index; rank `i` takes the `i`-th unskipped candidate |
| Per sheet | the sheet's times reassigned so wash 0 is earliest |
Output: `.kcc-wash[data-wash="<id>"]{--wash-at:<t>s}` per wash. Global washes take `g_slot`.

## Grain surfaces

| Surface | Grain source | Place |
|---|---|---|
| Desk | `.kcc-grain` (`--grain`), `.kcc-crayon` (`--crayon` and its stripe gradient) | `#app`'s first children, `z-index: 0`, under every sheet |
| Wash | `filter: url(#kcc-bloom)` | `.kcc-wash` |
| Tile | `filter: url(#kcc-wax-flat)` | `.kcc-tile` |

`--grain` and `--crayon` are declared only in `Torn/Tokens.css`'s `:root`; the filters only in `Layout.cshtml`.
Nothing else carries a grain (FR-020, FR-021). Retired: `.kcc-sheet::after` grain, the `.kcc-sheet--scroll` grain
layer, `--sheet-grain`, `--sheet-grain-op`, `--sheet-grain-blend`, `--wash-op`, `#kcc-wax`.
