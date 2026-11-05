# Research: Crayon Wash Texture

Phase 0 for [plan.md](plan.md). Every measurement below was taken on 2026-11-04 on the owner's Mac, with scratch
scripts that import the repository's own `Turbulence` port from `src/KCC.Web/Features/Bloom/bakeWashes.ts` and the
Playwright 1.64.1 driver (Chromium and WebKit). The scripts are not tracked; the numbers are.

## R1. Bake the approved look, no live filter

- **Decision**: bake. No wash carries a `filter`. `yarn washes` bakes the approved chain into a chalk tile and blob
  masks, as 006 baked the 005 filter.
- **Rationale**: FR-008 allows a live filter only when every 006 budget and guard passes with it. It cannot:
  - The E2E guard `MidBloom_NoRunningAnimationIsFilteredOrUnderAFilter` fails on any running animation that carries a
    filter. A blooming wash is a running animation, so a filtered wash fails it in both engines (006 close-out, T022,
    proved the guard catches a filter).
  - `washField.test.ts` fails on a `filter` on any `.kcc-wash` rule.
  - 006 measured the cost it would bring back: WebKit desktop home went from 760-780 ms worst frame with the live
    filter to 126 ms baked, and from 17 long frames to 12-13 (006 research, T047-T049).
  - The approved chain is heavier than 005's: four turbulences, three displacement maps and two blurs per wash.
- **Alternatives**: the live filter from `round-7-gen.mjs` (fails the guard above); a canvas or script drawing
  (breaks "no runtime JavaScript that draws", constitution II, and the scripts-off first paint, FR-009).

## R2. The blob mask carries the silhouette

- **Decision**: each blob mask is the approved first ink's faded ellipse, displaced by the approved lobe, ragged and
  stroke-end turbulences and blurred, baked once. The first ink in CSS becomes a flat colour; the mask gives it its
  shape and its fade. The second ink stays a CSS radial gradient, closest-side as today, and the planner centres it
  inside the blob's ellipse: `o3 = cx + .25 · rx · (2u − 1)`, `o4 = cy + .25 · ry · (2v − 1)`, with `(cx, cy, rx, ry)`
  the drawn blob's ellipse from `BLOB_SHAPES` and `u`, `v` today's `o3` and `o4` draws.
- **Rationale**: in the approved filter the displacement moves the *ink*, and the visible shape is the displaced
  closest-side ellipse of the first ink: the element's `border-radius` box hardly shapes it, because the ellipse is
  inscribed and touches the box on one side only. Today's structure (an undisplaced CSS gradient cut by a displaced
  box mask) cannot reach that ellipse with its lobes, so it would draw the same soft round spot the owner called
  mould. A scratch bake of this decision draws lobed, ragged, faded patches with chalk marks.
- **Why the second ink moves**: the mask cuts the whole element, both inks, while the approved filter displaced both
  inks together and cut the second only by the rounded box. A second ink centred independently, as the approved round
  draws it, keeps a median 56% of its alpha under the first ink's faded profile (10th percentile 15%; 43% of washes
  keep under half). Centred within a quarter of the blob's radii from the blob's ellipse centre, it keeps an
  estimated median 84% (10th percentile 75%) at about the same size (median area .94 of the first ink's). Explicit
  radii of .75 of the blob's keep 93% but halve its area. These are estimates: a scratch Monte Carlo over 3,000 draws
  on the undisplaced profile. The lobes move both edges together, so the displaced numbers are expected close, but
  they are not measured. `washes.test.ts` measures the retention over the displaced bake (tasks T033: median at least
  .80, 10th percentile at least .70) and the measured figures are recorded here; a retention under either floor goes
  to the owner with the second masked element below.
- **Measured over the displaced bake** (T033): a median retention of .838 and a 10th percentile of .751, over the
  8,507 washes of a thousand seeds on the card, reading and hero fields, with `a₂` the closest-side gradient in the
  content box and `m` each blob's `bakeBlob(b)` alpha, bilinear on the padded box. Both clear their floors (.80, .70).
  These figures are measured by a model of the contract's placement before the planner wrote it; `washes.test.ts`
  re-measures them over the planner's own output once it does. The same model cuts 100% of the washes by an edge on a
  280x120 card, 96.9% on a 700x2400 reading sheet and 98.8% on a 1300x400 hero (SC-007), and repeats a blob at the
  same place on neighbouring sheets in .38% of the wash pairs compared (edge cases, at most 5%).
- **Lost against the mockup**: the second ink's faint skirt outside the first ink's ellipse, and the per-wash random
  ellipse offset (`--wash-o1`, `--wash-o2`), which moves into each baked blob. The owner sees the second ink placed this
  way in the step-2 look round (plan) and judges the built result in SC-001.
- **Alternatives**: a second masked element per wash for the second ink (doubles the painted elements); folding the
  second ink's extent into the blob mask (the first ink is a flat colour over the whole mask, so the skirt would show in
  the first ink's colour); keeping the gradient fade in CSS under a displaced box mask (the round spot).

## R3. Blob bake

- **Decision**:
  - Chain, in the wash's own CSS px at a reference box of **480 px** square: the stroke-end turbulence
    (`0.44 0.004`, 1 octave, seed 11, green channel, scale 10, vertical only), then the ragged turbulence (`0.08`,
    2 octaves, seed 3, scale 12), then the lobe turbulence (`0.012 0.016`, 3 octaves, seed 5, scale 60), each a
    displacement lookup as `feDisplacementMap` applies it; the source is the first ink's normalised profile on the
    blob's ellipse; then a Gaussian blur of `stdDeviation 4`.
  - Profile (first-ink alpha over .34): 1 at 0%, .941 at 62%, .588 at 80%, .206 at 92%, 0 at 100% of the ellipse's
    radius. No rim.
  - Each blob draws its own ellipse centre in [.28, .72] on both axes, closest-side radii, its own
    `border-radius` corners in 38-64%, and its own region of the noise (an offset per index), from a seeded source as
    `blobRadii` does today.
  - `BLOB_SHAPES` and that seeded source live in `Features/Bloom/blobShapes.ts`, which imports nothing from Node.
    `washes.ts` reads `BLOB_SHAPES` for the second ink and ships in the client bundle (`Main.ts` registers every
    `*.Component.vue` eagerly; `KccWashes.Component.vue`, `KccSheet.vue` and `bloomContext.ts` import it), while
    `bakeWashes.ts` imports `node:zlib` and builds PNGs with `Buffer`. The bake imports `BLOB_SHAPES` from there too,
    and a Vitest walk of `washes.ts`'s imports holds the line (tasks T033a).
  - Margin 6% of the box per side (`--wash-span` 1.12). Samples at 1 px, box-averaged to **6 px per texel**:
    a 90 x 90 texture.
  - Encoding: 4-bit indexed PNG, 16 alpha levels in `tRNS`, adaptive row filters.
- **Evidence** (8 blobs, 480 px, 6 px per texel):

  | Margin per side | Highest alpha on the texture's edge | Bytes, 8 blobs |
  |---|---|---|
  | 5% | .0203 | 6,642 |
  | 6% | .0028 | 6,596 |
  | 7% | .0001 | 6,680 |

  At 6% the edge alpha quantises to 0 (one level is .067). The blur of 4 px attenuates anything finer than about
  12 px, so 6 px texels lose nothing a viewer sees; 4-bit steps (.067 of the mask, about .014 of final alpha at light
  strength) sit under the chalk tile's own variation. The 480 px reference is a median site wash (d 1.175 of a
  400 px sheet), so the lobes keep the mockup's absolute size there and scale with every other wash.
- **Size range**: the approved filter worked in absolute CSS px (lobe 60, ragged 12, blur 4), so its outline looked the
  same on every wash. Baked at one reference, the outline scales: an unpaired reading-sheet wash at `d` 1.25 of an
  860 px sheet is about 1,075 px, with lobes about 134 px and a blur of about 9 px (2.2 times); a reading-sheet wash
  averages about 740 px (1.5 times); a 340 px phone card's wash at `d` .9 is about 300 px (.64 times). Paired washes
  are capped (page sheets always, reading sheets about 60%), so the largest washes are the unpaired ones. The approved
  round never showed a wash over about 1,000 px. The step-2 look round (plan) shows the largest and smallest, and the
  owner judges them (spec A16). Sheet widths are the breaker's simulation figures, not measured on the site.
- **If the owner rejects the large outline**: a second reference set (eight more blobs, about 6.6 KB, taking the
  textures to about 17.4 KB, over the 16,000 B test cap and the 17,014 B `image_bytes` ceiling of R6), or a px cap on a
  wash's width (against FR-003b). The owner chose the px cap, an amendment to FR-003b, should the large outline be
  rejected (spec A16); the owner names its value in the step-2 round.
- **Alternatives**: a 600 px reference (the mockup's card washes, lobes 20% smaller on site cards); a set per sheet
  size (more files); 8-bit grey (about 2 to 3 KB per blob, measured at a 600 px reference).

## R4. Chalk tile

- **Decision**: one tile, 128 CSS px square, 1 texel per CSS px, stitched so it repeats seamlessly. Per texel:
  `m = clip(5R_s − 1.6) × clip(3R_t − 0.5)` with `R_s` the stroke turbulence (`0.44 0.1`, 2 octaves, seed 11) and
  `R_t` the paper tooth (`0.6`, 2 octaves, seed 21), then `(m + 0.5)`, then a Gaussian blur of 1 px with wrap, then
  divided by 1.5 so the mask peaks at 1. `--wash-gain: 1.5` on the wash's opacity restores the approved composite
  (`k2 = 1`, `k3 = 0.5`). 4-bit indexed PNG as R3.
- **Why the composite reduces to a mask**: the approved output is `blur₁(b · (m + 0.5))` with `b` the ink, already
  blurred by 4 px. `b` is smooth over the 1 px blur, so `blur₁(b · T) ≈ b · blur₁(T)`, and the ink alpha never
  passes 1 (`.512 × 1.5 = .77`).
- **Evidence**, a flat ink under the live filter (marks only) against the baked tile as a mask, 256 x 256 CSS px,
  alpha recovered from the pixels:

  | Engine, DPR | Source | Mean | Spread (sd) | Autocorrelation x at 1, 2, 3 px | y at 2, 4, 8 px | Worst 3 px average |
  |---|---|---|---|---|---|---|
  | Chromium, 1 | live filter | .748 | .118 | .82 .47 .18 | .75 .33 −.17 | .935 |
  | Chromium, 1 | tile 1x, 8-bit | .748 | .120 | .79 .37 .06 | .74 .30 −.22 | .931 |
  | Chromium, 1 | tile 1x, 4-bit | .748 | .122 | .77 .36 .06 | .72 .29 −.22 | .929 |
  | Chromium, 2 | live filter | .747 | .118 | .81 .45 .16 | .75 .34 −.16 | .949 |
  | Chromium, 2 | tile 1x, 8-bit | .747 | .114 | .81 .42 .11 | .76 .30 −.23 | .940 |
  | Chromium, 2 | tile 2x, 8-bit | .749 | .118 | .79 .37 .06 | .75 .30 −.23 | .948 |
  | WebKit, 1 | live filter | .756 | .092 | .89 .62 .33 | .78 .34 −.26 | .941 |
  | WebKit, 1 | tile 1x, 8-bit | .748 | .120 | .79 .37 .06 | .74 .30 −.22 | .931 |
  | WebKit, 2 | live filter | .756 | .101 | .85 .53 .22 | .77 .32 −.25 | .948 |
  | WebKit, 2 | tile 1x, 8-bit | .747 | .117 | .80 .40 .09 | .75 .30 −.23 | .941 |

  The tile matches Chromium's live mark in coverage, spread and stroke length (y). Across the stroke (x) it is a
  little crisper than the live filter, and WebKit's live filter is softer than Chromium's; the bake follows the
  Chromium rendering the owner approved. A 2x tile is no closer at DPR 2, and 4-bit equals 8-bit.
- **Bytes**:

  | Tile | 8-bit grey and alpha, no filter (today's encoder) | 8-bit grey | 4-bit indexed |
  |---|---|---|---|
  | 64 px | 4,680 | 2,833 | 1,173 |
  | 96 px | 10,185 | 6,288 | 2,401 |
  | 128 px | 17,833 | 11,088 | 4,169 |

- **Tile size**: 128 px. 64 px repeats about every six strokes of length; 128 px stays under a fifth of a median
  wash and turns with it.
- **Seams**: the scratch tile's wrap row differs by .045 against .033 inside, and its row means vary by .021
  (.672 to .784), which showed in a scratch render as a faint horizontal stripe every 128 px. The live filter has the
  same row variation but never repeats it. The bake subtracts each row's and column's deviation from the tile mean
  before quantising, and the test bounds the seam and the row and column means (contract).
- **Alternatives**: a 2x tile (four times the bytes, no closer); 64 px (visible repeat).

## R5. How the tile and the masks move with a wash

- The masks paint in the wash's local space, so they turn with `--wash-turn`, follow the mirror (R6) and grow with the
  bloom's `scale` from .72 to 1, as the live filter's user space did. A blooming wash shows its marks at its current
  size (US1 scenario 4).
- The tile keeps 128 CSS px on every wash: a small wash on a narrow card keeps its marks' size (edge case 1). The blob
  mask stretches with the wash box, so aspect stretches the silhouette but not the marks.
- Stroke ends: the stroke-end displacement is vertical in local space, as the strokes are, and survives the mirror
  (horizontal) and the aspect stretch. After the 4 px blur it reads as a softer edge along the stroke axis.
- DPR 1 and 2: one 1x tile serves both (R4 table).

## R6. Silhouette count

- **Decision**: 8 blob masks and a per-wash mirror (`--wash-flip`, 1 or −1), so 16 silhouettes before rotation,
  aspect, size and the edge crop.
- **Budget**: `image_bytes` is judged against the baseline plus the larger of 1,024 B and 2%. The smallest images-on
  baseline is 164,698 B (both baseline files), already raised by today's 13,720 B of textures, so the textures may
  total at most 17,014 B. `bakeWashes.test.ts` holds them under 16,000 B.

  | Option | Textures | Requests preloaded | Bytes (measured or scaled) |
  |---|---|---|---|
  | Today | grain + 8 blobs | 9 | 13,720 |
  | **8 blobs + mirror + chalk** | 9 | 9 | about 10,800 |
  | 12 blobs + chalk | 13 | 13 | about 14,200 |
  | 16 blobs + chalk | 17 | 17 | about 17,500, over |

- **Rationale**: the mirror doubles the silhouettes for one hash draw and no bytes, and keeps the preload count, which
  006 left competing with the phone LCP. Search-3 draws about 50 washes: each silhouette appears about three times, at
  different turns, stretches and crops.
- **Alternatives**: 12 blobs without a mirror (more requests, fewer silhouettes).

## R7. The wash box and the pair cap

- **Decision**: the wash keeps a content box of `--wash-w` by `--wash-h`. Its padding becomes 6% of its own width
  inline and 6% of its height in the block axis, so the padded box is the texture's box (`--wash-span` 1.12, written by
  `yarn washes`). The pair cap becomes `hypot(...) / (var(--wash-s) * var(--wash-span))`. `--wash-reach` and
  `WASH_REACH_PX` retire.
- **Rationale**: the baked silhouette scales with its wash, so its overhang does too: 29 px on a 480 px wash, about
  75 px on a 1,250 px one. A fixed 24 px padding would clip the lobes of a large wash with a straight edge. The
  padded box's half diagonal is `w · s · span / 2`, so the new cap keeps every pair's circles disjoint as today.
- **Alternatives**: a px reach of about 75 px (shrinks every paired wash and pads every small one).

## R8. Placement and size

- **Decision**: port `placements.edge` from the approved round into `washes.ts`:
  - Each wash pushes one axis, `hash(seed, key, k, 'axis') & 1`, into the outer band of its quarter: from −.02 to .16
    of the sheet on the near side, .84 to 1.02 on the far side. The other axis stays in its half: .10 to .45 or
    .55 to .90.
  - A one-wash card takes the quarter `hash(seed, key, 'corner') & 3`.
  - `--wash-d` runs .90 to 1.25 (shrunk from 1.45 after the spike; see R12). The pair cap stays.
  - `--wash-o1` and `--wash-o2` retire (R2); `--wash-flip` joins (R6).
- **Determinism**: every draw is `hash(seed, key, k, label)`, so new labels (`axis`, `corner`, `flip`) and dropped ones
  (`o1`, `o2`) leave every other draw unchanged. Wash ids stay `hash(key, k)`, `quadrantsFor` stays, and the schedule
  (`scheduleStarts`, `fallbackStarts`, `visitStarts`) reads ids, keys and indices only, so arrivals do not move.
  `wash-key` rules and `keys.test.ts` are untouched.

## R9. Contrast

- **Decision**: re-read the rendered table from the new textures and refit the model. Light keeps .42 if it holds,
  otherwise takes its cap. Dark takes its new cap, pinned by the tenth-stronger test.
- **Model changes**: the first ink's alpha becomes .34 (no rim); `GRAIN_GAIN` becomes a per-blend texture gain fitted
  so the model reads at or below every new rendered reading; `--wash-gain` (1.5) multiplies the strength.
- **Estimate**, from today's model with the wash alpha scaled by the measured ratio of text's worst 3 px average
  (chalk 1.435 against today's grain .990: ×1.45), colour calibration left as fitted today:

  | | Light cap | Dark cap | Light at .42 | Dark at .18 |
  |---|---|---|---|---|
  | Today | .569 | .196 | 4.69 | at cap |
  | Chalk, ×1.43 | .401 | .138 | 4.42 | 3.86 |
  | Chalk, ×1.5 | .382 | .131 | 4.34 | 3.74 |

  Dropping `--wash-tone` lightens each ink, which helps multiply and hurts screen. Light at .42 fails AA on both rows
  (4.42 and 4.34), so expect light at its cap, .38 to .40, and dark about .13, set by the re-read table. The approved
  round ran at .42 and .18, so both strengths fall below what the owner approved: dark by about a quarter. The spec's
  assumption ("the plan lowers it and records it") covers the drop, and the step-2 look round (plan) shows the owner
  the approved round at about .39 and .13 before the bake, so a drop the owner rejects changes the bake, not the
  finished work.
- **Rendered readings (T039, `contrast-harness.mjs`)**: the harness loads the built kit CSS with its textures inlined
  into headless Chromium, lays two washes on a paper stage at peak (`--wash-peak` 1, gain 1.5, no animation, 520 px
  wide, second ink at the blob's centre), screenshots it without text and reads the lowest average over a 3 px square of
  the contrast against ink-soft. It scans every colour as all four inks over every blob pair (stage 1, at the
  reference strength), then colour mixes over the worst blobs and offsets and flips of the worst stacks. Worst case
  throughout: red on light, yellow or sky on dark, blobs 4 and 6 stacked on the same centre.

  | Ramp | Strength | Lowest reading |
  |---|---|---|
  | Light (sat 2.5) | .42 / .46 / .50 / .60 | 4.591 / 4.459 / 4.297 / 3.985 |
  | Dark (sat 3) | .10 / .12 / .125 / .13 / .16 / .18 | 5.041 / 4.635 / 4.54 / 4.429 / 3.974 / 3.663 |

- **Fit (T040)**: `FIRST_INK_CORE_ALPHA` .34, `TEXTURE_GAIN` 1.11 multiply and .95 screen (the lowest gains at which
  the model reads at or below every row), `--wash-gain` read from `Washes.css`. Model caps, where ink, ink-soft and the
  hairline all hold over every two-wash stack at its peak: light .433, dark .125.
- **Taken (T041, T042)**: light stays at .42, under its cap of .433, so it does not move. Dark falls from .18 to .125,
  its cap, the strongest the tenth-stronger test allows (.1375 fails). The rendered reading at .125 is 4.54.
- **Harness**: as 006 did, a headless-Chromium page kept in `.mockups/007-crayon-wash-texture/` renders ink-soft over
  each two-wash stack at its peak with the built kit CSS and textures, reads the worst 3 px average, and the readings
  go into `RENDERED_INK_SOFT` with the method in this file.

## R10. The performance gate

- **What the change can move**: `image_bytes` falls (R6); `css_gz_bytes` falls (fewer gradient stops); `filtered_*`,
  `animated_filtered` and `animated_under_filter` do not change (no filter); `blend_elements` and `clip_path_elements`
  do not change; larger washes add raster area per bloom step and at first paint, which can move frame timings and
  `layer_area_mpx`.
- **The rule (owner, 2026-11-04)**: relative to `main`. No 006 budget or guard that passes on `main` may fail on this
  branch, locally and in CI, in both engines, both ramps and both profiles. A case whose result flips between the two
  runs is re-run three times on both branches, `KCC_PERF_RUNS=3 yarn perf --check` narrowed with `--page`,
  `--profile`, `--engine`, `--ramp` and `--visit`, and judged on its median (timing metrics already report the median
  of their runs). Budgets that already fail on `main` are carried, not fixed: 006 closed with budgets still failing
  (phone LCP, interaction on 13 cases, long frames on 7). The carried list is the `main` run's own failures.
- **Where each check runs**:
  - `yarn perf --check` (`PerfMatrixTests`) evaluates against the pre-006 `specs/006-performance-sweep/baseline.json`
    (`osx-arm64`), whatever `main` measures; the comparison with `main` is the gate's, read from both runs' results.
  - The E2E deterministic and timing guards (`DeterministicGuardTests`, `TimingGuardTests`) evaluate against
    `baseline.ci.json` with the CI modes and margins. They run in the E2E suite on the Mac and in CI.
- **SC-003 is read, not enforced**: `bloom_frame_max_ms` is `report-only` in `budgets.json`, and `bloom_long_frames`
  carries a margin of one frame over its baseline of 0, so `--check` can pass a bloom that adds a frame over 50 ms.
  The gate reads `bloom_frame_max_ms` per mid-bloom case from both runs: a case under 50 ms on `main` and over it here
  fails SC-003, under the re-run rule. The harness measures mid-bloom in Chromium only, 16 cases (home, search-1, search-3,
  recipe; both profiles, both ramps), so WebKit's bloom is unmeasured on both branches and carried as such. After 006
  every Chromium mid-bloom case read 9.4 to 10.4 ms (`after-phase1.json`), so none is carried today; the `main` run
  decides.
- **`layer_area_mpx`** is `platformSensitive` with a +2% margin. CI's runners are Linux, so the guard skips it there.
  On the owner's Mac it is enforced twice:

  | Check | Baseline | search-3 phone | home phone | recipe phone | After 006 |
  |---|---|---|---|---|---|
  | E2E deterministic guard | `baseline.ci.json` (`osx-arm64`, taken during 006) | 29.3, limit 29.9 | 15.6, limit 15.9 | 4.8, limit 4.9 | the same values |
  | `yarn perf --check` | pre-006 `baseline.json` | 36.3, limit 37.0 | 21.5, limit 21.9 | 7.3, limit 7.4 | 29.3, 15.6, 4.8 |

  The guard has 2% headroom, the matrix about 26% on search-3. Larger washes grow an uncapped wash's box about 1.9
  times in area on average (mean `d` .85 to 1.175), so if the washes are among the measured layers the guard fails on
  the Mac. Whether they are is not known: reduced motion halves search-3's layer area (15.3), so something animated is
  layered. The spike reads it. If it rises past either limit while every timing budget holds, the owner decides
  whether the layer-area baselines are re-taken for this change (spec A18); otherwise the size change goes back to the
  owner as an FR-003b and FR-007 conflict.
- **`main` is measured in its own worktree**: card implement commits only at its close-out, so this checkout holds the
  branch's uncommitted work through the whole run; switching it to `main` would measure that work as `main`, and a
  local `main` may lag `origin/main`. `main` is measured once, before the spike, in a worktree at `origin/main`
  (`git worktree add --detach .claude/worktrees/007-main origin/main`, built there with `yarn build:all`), which serves
  the spike's three-run re-runs and is removed after them. The gate re-measures it the same way only if `origin/main`
  has moved under `src/`, `tests/` or the root `package.json` (spec A20).
- **Each run's output is copied aside**: every `yarn perf` run deletes and rewrites `tests/results/perf/results.json`
  (`tests/scripts/perf.mjs`), and every deterministic guard run rewrites `deterministic-chromium/` and
  `deterministic-webkit/` (`DeterministicGuardTests` runs both engines), so a narrowed re-run would overwrite the run
  the gate reads. Each run is copied to its own name before the next (`main.json`, `spike.json`, `gate.json`,
  `rerun-<case>-<branch>.json` and the matching `*-deterministic-<engine>/` folders; tasks, "Every run's output is
  copied aside").
- **CI**: the pull request's `perf` job and the E2E guards pass ten of ten on the pull request's head commit, after the
  review loop's fix commit is pushed, and again on any later head (SC-002). The pull request opens at card implement's
  close-out, so this runs in the review loop, outside `speckit.implement`.
- **Order**: a spike runs the planner and CSS size change first, with the final padding (6% per side,
  `--wash-span` 1.12) and the final pair cap (`hypot / (s × 1.12)`), and today's textures stretched to the new padded
  box, on the key cases and the deterministic guards, so a size regression is seen before the bake. Today's 24 px
  padding and `(hypot − 2 × 24) / s` cap would misread the final raster: an unpaired 1,250 px wash's padded box is about
  1,400 px with the final padding against 1,298 px with today's (16% more area), and a capped pair's box is smaller
  with the final cap. If larger washes alone break a budget or guard that passes on `main`, FR-003b and FR-007
  conflict, and the owner decides.

## R11. Documentation

FR-012 touches, in the same change: `CLAUDE.md` (Loose Leaf, the tear: the wash bullet), `docs/brand/kit.md` (file
table rows for `Washes.css`, `Washes/` and `bakeWashes.ts`; the tokens paragraph: `--wash-reach` out, `--wash-span`
and `--wash-gain` in, the strengths; the grain paragraph; the Washes section: placement, the fading edge with no rim, "Baked, not
filtered"; the enforcement table rows), `docs/brand/loose-leaf.md` (Washes, Grain, a dated change-log entry) and
`.claude/skills/loose-leaf/SKILL.md` (the baked-texture line). The no-filter rule stands; its text names the chalk
tile and the silhouettes. `specs/007-crayon-wash-texture/mockups/README.md` "Still open" is answered.

## R12. Measurements

- **Measured commit**: `origin/main` at `20b63e1ee913f076f47ccc31d2724e215ed41935`. At measuring time
  `git diff --stat origin/main HEAD -- src tests package.json yarn.lock` was empty, so the spike's diff is the spike alone.
- **Run**: `yarn perf` in the main worktree, 232 cases, took 3,675 s; copied to `tests/results/perf/main.json`. The
  deterministic guards (`DeterministicGuardTests`, both engines) passed, 4 of 4. `captures/main` (248 files) and
  `captures/before` (248 files, 006's pre-006 captures) exist in this checkout; no fallback needed.
- **Budgets already failing on `main` (carried, not fixed)**, by metric, with the failing case count:
  `frames_within_16_7_pct` 124, `frame_p95_ms` 90, `long_frames` 21, `clip_path_elements` 18 (account page, all
  engines and profiles), `interaction_ms_max` 15, `lcp_ms` 11 (4 phone `images off` or search-1, 7 desktop),
  `bloom_frame_p95_ms` 8 and `bloom_frames_within_16_7_pct` 8 (the eight desktop Chromium mid-bloom cases),
  `hydrated_ms` 1 (`search-1 · desktop · webkit · light · bloomed`). No `image_bytes`, `layer_area_mpx` or other
  deterministic budget fails except `clip_path_elements`.
- **`bloom_frame_max_ms`**, the 16 Chromium mid-bloom cases (home, search-1, search-3, recipe; phone and desktop; light
  and dark): 16.8 ms in every one, so none is over 50 ms on `main` and none is carried under SC-003.
- **`layer_area_mpx`** (Chromium, bare visit; light and dark equal; bloomed reads lower), Mpx:

  | Page | Phone | Phone limit, `baseline.ci.json` | Phone limit, pre-006 `baseline.json` | Desktop | Desktop limit, pre-006 `baseline.json` |
  |---|---|---|---|---|---|
  | search-3 | 27.2 | 29.9 | 37.0 | 32.3 | 46.7 |
  | home | 14.4 | 15.9 | 21.9 | 26.0 | 38.0 |
  | recipe | 4.1 | 4.9 | 7.4 | 13.1 | 20.4 |

  Bloomed: search-3 5.5 phone, 11.3 desktop; home 3.9, 8.4; recipe 1.2, 3.5.
- **`image_bytes`**: 164,698 B on every page, 165,608 B on recipe, in every case; all pass.

### Spike (T014 to T017)

- **Run**: `spike.json` (66 key cases) and `spike-deterministic-*`, in this checkout. Guards 4 of 4, verdicts match
  `main` in both engines. `image_bytes` and layer counts equal `main`'s.
- **`bloom_frame_max_ms`**: 16.8 ms in all 16 Chromium mid-bloom cases, as on `main`; none is over 50 ms (SC-003).
- **`layer_area_mpx`** (Chromium, bare), Mpx, `main` to spike:

  | Page | Phone | Phone limit, `baseline.ci.json` | Phone limit, pre-006 `baseline.json` | Desktop | Desktop limit, pre-006 `baseline.json` |
  |---|---|---|---|---|---|
  | search-3 | 27.2 to 32.4 | 29.9, over | 37.0, ok | 32.3 to 37.7 | 46.7, ok |
  | home | 14.4 to 17.3 | 15.9, over | 21.9, ok | 26.0 to 32.5 | 38.0, ok |
  | recipe | 4.1 to 4.7 | 4.9, ok | 7.4, ok | 13.1 to 17.4 | 20.4, ok |

- **Verdicts that differ, single run** (`spike.json` against `main.json`): spike fails where `main` passes in
  `search-3 · phone · chromium · light · bare` `lcp_ms` (2192 against 1744), `home · phone · chromium · light · bloomed ·
  images off` `frame_p95_ms`, `home · desktop · webkit · dark · bare` `long_frames` (12 against 11),
  `search-3 · phone · webkit · light · bare` `long_frames` (24 against 11), `recipe · desktop · webkit · dark · bloomed`
  `long_frames` (7 against 5) and `search-3 · desktop · chromium · light · bare` `interaction_ms_max` (88 against 80).
  The spike passes where `main` fails in ten others (lcp_ms on four desktop cases, `recipe · phone · chromium · light ·
  bloomed` `frame_p95_ms`, and so on): noise on `main`, not carried.
- **Three-run medians** (`KCC_PERF_RUNS=3`, `rerun-<case>-{007,main}.json`, runs of 5 Nov with no sleep):

  | Case | Metric | Spike | `main` | Limit | Verdict |
  |---|---|---|---|---|---|
  | search-3 · phone · chromium · light · bare | `lcp_ms` | 1740 | 1736 | 2000 | noise, both pass |
  | home · phone · chromium · light · bloomed · images off | `frame_p95_ms` | 16.8 | 16.8 | 16.7 (margin 1.25, floor 4) | both pass |
  | home · desktop · webkit · dark · bare | `long_frames` | 11 | 11 | baseline 10 + 1 | both pass |
  | search-3 · phone · webkit · light · bare | `long_frames` | 19 | 16 | baseline 16 + 1 | spike fails, `main` passes |
  | recipe · desktop · webkit · dark · bloomed | `long_frames` | 7 | 6 | baseline 4 + 1 | both fail, carried |
  | search-3 · desktop · chromium · light · bare | `interaction_ms_max` | 96 | 80 | 100 (margin 1.25, floor 25) | spike fails, `main` passes; inside the 100 ms limit |

Route (T018): (c), a timing regression on search-3. The owner ruled on 2026-11-05: shrink wash sizes to `d` .90 to
1.25, do not re-run the spike, and judge both search-3 cases again at the final gate with the real chalk masks.

### Round 8 (T023)

Approved by the owner on 2026-11-05: the projected strengths, about .39 light and .13 dark, and the uncapped large
wash's outline at about 1,075 px. The bake runs the contract's numbers; Phase 8 sets the strengths from the re-read
contrast table; no px cap applies, so T036 and T037 do not run.

### Gate (T052 to T058)

- **Commits**: `origin/main` unchanged at `20b63e1e` (T052), so `main.json` stands. The gate ran this branch's working
  tree on `7ce79899`: `yarn perf --check --before tests/results/perf/main.json`, 232 cases, copied to `gate.json`; then
  `DeterministicGuardTests`, copied to `gate-deterministic-chromium` and `gate-deterministic-webkit`.
- **Power**: `main.json` ran on mains power on the evening of 4 Nov. The gate ran on battery on 5 Nov. The re-runs
  below pair the branches on the same power source.
- **Deterministic guards**: 4 of 4 pass in both engines. Every verdict in `gate-deterministic-*` matches
  `main-deterministic-*`.
- **Carried from `main`, failing on both branches**, by metric, with case counts on `main` and here:
  `frames_within_16_7_pct` 124 and 126, `frame_p95_ms` 90 and 93, `long_frames` 21 and 20, `clip_path_elements` 18 and
  18 (account, 26 or 27 against 23 or 24, from the account pad's sheet in `37febb13`), `interaction_ms_max` 15 and 9,
  `bloom_frame_p95_ms` 8 and 8, `bloom_frames_within_16_7_pct` 8 and 8, `lcp_ms` 11 and 4. Every carried case
  outside the re-run list below fails on `main` too. WebKit's bloom is unmeasured on both branches (the harness
  measures mid-bloom in Chromium only) and stays carried.
- **Verdicts that differ, single run**: 14 checks fail here and pass on `main`: `lcp_ms` on three phone Chromium
  bloomed cases, five frame checks, four WebKit `long_frames`, and `layer_area_mpx` on two cases. 22 checks fail on
  `main` and pass here: `lcp_ms` 10, `interaction_ms_max` 6, `long_frames` 5, `hydrated_ms` 1. These count as noise
  on `main`.
- **Three-run medians** (`KCC_PERF_RUNS=3`, `rerun-<case>-{007,main}.json`):

  | Case | Metric | 007 | `main` | Limit | Verdict |
  |---|---|---|---|---|---|
  | search-1 · phone · chromium · dark · bloomed | `lcp_ms` | 1788 | 1768 | 1785 | 007 fails, `main` passes |
  | search-3 · phone · chromium · light · bloomed | `lcp_ms` | 1788 | 1764 | 1785 | 007 fails, `main` passes |
  | search-3 · phone · chromium · dark · bloomed | `lcp_ms` | 1804 | 1768 | 1775 | 007 fails, `main` passes |
  | search-1 · phone · chromium · light · bloomed · motion reduced | `lcp_ms` | 1804 | 1764 | 1795 | 007 fails, `main` passes |
  | variant-cook · phone · chromium · light · bloomed · motion reduced | `lcp_ms` | 1916 | 1912 | 1890 | both fail, carried |
  | sign-in · phone · chromium · dark · bare | `frames_within_16_7_pct` | 52.9 | 0 | 67.1, less 5 | both fail, carried |
  | home · phone · chromium · light · bloomed · images off | `frame_p95_ms` | 33.2 | 33.4 | 24.8 (margin 1.25, floor 4) | both fail, carried |
  | settings · phone · chromium · light · bloomed · images off | `frame_p95_ms` | 33.3 | 33.4 | 34.3 (margin 1.25, floor 4) | both pass |
  | account · phone · webkit · dark · bloomed | `frame_p95_ms` | 151 | 32 | 18 (margin 1.25, floor 4) | both fail, carried |
  | account · desktop · webkit · dark · bloomed | `frames_within_16_7_pct` | 44.1 | 44.1 | 41.2, less 5 | both pass |
  | home · desktop · webkit · dark · bare | `long_frames` | 11 | 11 | baseline 10 + 1 | both pass |
  | search-1 · phone · webkit, four cases | `long_frames` | 10 to 12 | 10 to 11 | baselines 9 to 12, + 1 | same verdicts on both, on mains power |
  | search-1 · desktop · webkit, four cases | `long_frames` | 8 to 9 | 9 to 12 | baselines 8 to 10, + 1 | 007 passes in all four; `main` fails dark bare and dark bloomed |

  The `lcp_ms` rows ran twice: on battery, with `main` mostly on battery too, and then both branches on mains
  power. Both pairs gave the same verdicts, and the AC medians are the ones above. The WebKit rows first ran with `main` on
  mains power and this branch on battery, which gave `search-1 · desktop · webkit · dark · bloomed` 11 against 8. Re-run
  with both branches on mains power, no WebKit verdict differs from `main`. Images off, the same search pages
  read 1668 to 1688 ms on both branches, so the later LCP comes only with the photos and the bloomed washes together.
- **The two search-3 cases the owner held for this gate**: `search-3 · phone · webkit · light · bare` `long_frames` 12
  against `main`'s 11, baseline 16, both pass. `search-3 · desktop · chromium · light · bare` `interaction_ms_max` 56
  against 80, both pass.
- **SC-003**: `bloom_frame_max_ms` is 16.8 ms in all 16 Chromium mid-bloom cases on both branches. None reaches 50 ms.
- **`image_bytes`**: passes in every case: 162,376 B on every page, 163,286 B on recipe, against `main`'s 164,698 B and
  165,608 B. The nine textures weigh 11,398 B, under the 16,000 B test cap and the 17,014 B ceiling (R6).
- **`layers`**: unchanged from `main` in every case (search-3 261 phone and 307 desktop, home 126 and 135, recipe 52
  and 58, Chromium bare).
- **`layer_area_mpx`** (Chromium, bare), Mpx, `main` to 007, both passing their `baseline.json` limit except
  variant-cook desktop:

  | Page | Phone | Desktop |
  |---|---|---|
  | search-3 | 27.2 to 30.1 | 32.3 to 35.9 |
  | home | 14.4 to 15.8 | 26.0 to 29.9 |
  | recipe | 4.1 to 4.6 | 13.1 to 16.9 |
  | variant-cook | 6.3 to 7.3 | 29.8 to 42.6, over 40.2 + 2% |

  Bloomed reads as on `main`: search-3 5.5 phone and 11.3 desktop, home 3.9 and 8.4, recipe 1.2 and 3.5. CI's
  `baseline.ci.json` measures phone Chromium bloomed on CI hardware only, which this checkout cannot reproduce; T063's
  CI runs read it.
- **Baseline changes** (T058), in `specs/006-performance-sweep/baseline.json`, which `yarn perf --check` reads through
  `MatrixRun.BaselinePath`. These are live harness inputs changed under owner rulings, not a rewrite of 006's record:

  | Case | Metric | Before | After | Ruling |
  |---|---|---|---|---|
  | variant-cook · desktop · chromium · light · bare | `layer_area_mpx` | 40.2 | 42.6 | A18, 2026-11-04 |
  | variant-cook · desktop · chromium · dark · bare | `layer_area_mpx` | 40.2 | 42.6 | A18, 2026-11-04 |
  | search-1 · phone · chromium · dark · bloomed | `lcp_ms` | 1428 | 1448 | owner, 2026-11-05 |
  | search-3 · phone · chromium · light · bloomed | `lcp_ms` | 1428 | 1452 | owner, 2026-11-05 |
  | search-3 · phone · chromium · dark · bloomed | `lcp_ms` | 1420 | 1456 | owner, 2026-11-05 |
  | search-1 · phone · chromium · light · bloomed · motion reduced | `lcp_ms` | 1436 | 1476 | owner, 2026-11-05 |

  The `lcp_ms` baselines rise by the measured gap between the two branches' mains-power medians, so `main`'s headroom
  carries over unchanged. The owner chose to re-baseline over looking for the 20 to 40 ms or shrinking the washes.
  FR-007 holds with these two rulings: no other budget or guard that passes on `main` fails here.
- **Scripts off** (T057, SC-005): `yarn perf:capture 007-chalk`, then `yarn perf:scripts-off 007-chalk`. Of 52 pairs, 35
  are identical, 15 differ only by the header residue (channel delta 4 or less), and 2 fail:
  `search-1__phone__webkit__light__bloomed` (409 pixels, delta 205) and `settings__desktop__webkit__dark__bloomed` (46
  pixels, delta 7). `yarn perf:scripts-off main` fails the same two pairs (409 pixels, delta 205; 47 pixels, delta 7), so
  both are carried from `main`.
- **D6 look** (T051), from the `007-chalk` captures (Chromium bloomed, both ramps) of home, search-1, a recipe and
  sign-in: every sheet keeps its tear, fall and blank. Text, rules and labels read over the washes in both ramps. The
  chalk tooth shows at full size. Dark washes sit at .125 and read faintly. On the wide, short home category cards
  a wash is cut by the sheet's edges and reads as a soft vertical band. That is the approved geometry, and it goes
  to the owner's round 9.
