# Research: Performance Sweep, Phase 1

Three investigations fed this plan: a profiling run with ablations on a fresh Release site, a design for the
measurement harness and guards, and a survey of the stylesheet and the effects. The profiling's raw results are in
`research/profiling/` (JSON lines, one file per run set).

## Findings

### F1. The cost is GPU and compositor work, not the main thread

Environment: a fresh SQLite Release site in the Testing environment, SSR on, 25 seeded recipes. Chromium 145
(Playwright 1.58.0) on an M5 Max with Metal raster, and the same Chromium on SwiftShader (software GPU) as a stand-in
for a weak phone GPU. WebKit 26.0 headless, unthrottled. CDP's CPU throttle slows only the renderer's main thread,
never raster or the GPU.

| Page | Layers (phone) | Phone frames within 16.7 ms (Metal) | Idle GPU-process CPU, bare visit |
|---|---|---|---|
| Home | 161 | 99.7-99.9% | 23-26% (phone), 29-30% (desktop) |
| Search, 1 page | 132 | 99.3-99.5% | 13-18% / 45% |
| Search, 3 pages | 230 | 99.5-99.8% | 17-19% / 39-47% |
| Recipe detail | 69 | 97.8-99.2% | 14-16% / 15% |

On a strong GPU, Chromium already holds the frame budget. On SwiftShader, search with three pages holds only 66-68%
of frames within 16.7 ms, with stalls of 3.8 s at scroll start and at the turn. Mid-bloom with no input, the GPU
process burns 51-67% of a core: 28-30 animations re-composite every frame. Hover responds within budget today
(worst Event Timing entry 16-72 ms).

### F2. Ablation ranking (search, 3 pages, phone, bloomed, light, median of 3)

| Override | Metal GPU CPU in scroll | SwiftShader frames within 16.7 ms | WebKit p50 frame, desktop / phone | Idle mid-bloom GPU |
|---|---|---|---|---|
| None | 21.9% | 66-68% | 360 ms / 5,335 ms | 53% |
| Wash filter off | 16.3% | 84% | 30 ms / 320 ms | 45% |
| Card shadows off | 9.1% | 99.2% | 224 ms / 1,187 ms | 26% |
| Clip-path off | 17.9% | 79% | 363 ms / 5,359 ms | 51% |
| Grain and crayon off | 21.6% | 65% | 362 ms / 5,418 ms | 53% |
| Bloom paused | 21.8% | 62% | n/a | 0.2% |
| Shadows, wash filter, tile filter, grain off | n/a | 99.4%, max 75 ms | 17 ms / 17 ms | 25% |

1. The two chained drop-shadows on `.kcc-torn` are the largest Chromium cost. The blank's shadow barely matters.
   The torn sheet's shadow wraps the washes, so every bloom frame and every hover re-blurs the whole card.
2. The wash filter `url(#kcc-bloom)` dominates WebKit, by an order of magnitude.
3. The bloom animation costs nothing after 300 s and up to two thirds of a core before it.
4. Grain, crayon and the tile filter only show in combination: they decide whether multi-second stalls happen.
5. Clip-path is the largest main-thread item (31%) with little GPU effect.
6. The washes' blend mode and `container-type` show no measurable cost in Chromium.

Headless WebKit may composite in software, so its absolute numbers may exaggerate real Safari. The owner's iPhone
pass at acceptance (FR-021) checks that.

### F3. Stylesheet weight

The built main stylesheet is 317,022 B raw and 68,216 B gzipped.

| Source | Raw | Gzip |
|---|---|---|
| Font Awesome Pro (3,862 rules, 3,773 icon rules) | 140,311 B | 36,875 B |
| Tear polygons (`:root`) | 116,476 B | 18,475 B |
| Kit and controls | 22,059 B | 4,683 B |
| Tailwind utilities | 11,979 B | 2,989 B |
| Base, theme, properties | 15,148 B | 4,784 B |

The site uses about 90 icon names in `.vue` and `.cshtml` files, plus 153 recipe icons (`src/KCC.Admin/RecipeIcons.cs`)
and the taxonomy defaults (`Recipes/TaxonomyDefaults.cs`). A standard tear preset has about 325 vertices, a hero
about 647, each written as `calc(A% + Bpx)`, and every sheet substitutes the polygon through `var(--tear)`. Every page
needs the hero tears, because the header uses them, so splitting the tears per page saves nothing.

## Decisions

### R1. Measurement harness

- **Decision**: one .NET Playwright harness in `tests/KCC.E2ETests/Features/Performance/`, with shared code in
  `tests/KCC.E2ETests/Config/Perf/`. It reuses `SiteProcess`, the visit helper, the wash helpers and
  `MemberSession`. A Node script, `tests/scripts/perf-report.mjs`, renders `results.json` to HTML and Markdown;
  `tests/scripts/perf-diff.mjs` diffs captures.
- **Rationale**: `SiteProcess` already boots a fresh, seeded, production-mode SSR site; the compose stack runs
  Production, cannot be seeded, and its images are arm64 only. One Playwright install, pinned once.
- **Alternatives rejected**: a Node Playwright harness (duplicates the site boot, the seeder and the member helpers,
  and a second Playwright pinned to the same version); Lighthouse (load only, no scroll, rejected in 002's R8).

### R2. Profiles and engines

- **Decision**: the phone profile is 390x844 at DPR 3, Fast 4G (9 Mbps, 170 ms round trip) and a 4x CPU slowdown,
  run on SwiftShader as the weak-GPU stand-in. The desktop profile is 1440x900, unthrottled, on the machine's GPU
  locally and SwiftShader in CI. WebKit runs unthrottled and is labelled so.
- **Rationale**: F1 shows the CPU throttle hides GPU cost on a strong GPU; SwiftShader separates the overrides where
  Metal does not. CI runners have no GPU, so CI uses SwiftShader anyway. Playwright gives WebKit no throttling and no
  CDP, and a scaling factor would be invented.
- **Alternatives rejected**: Metal for the phone profile (misses the regressions F2 shows); scaled WebKit budgets.

### R3. Metrics

- Chromium: LCP, layout shift and Event Timing through buffered `PerformanceObserver`; rAF frame deltas plus
  long-animation-frame entries while scrolling in wheel steps and for 5 s mid-bloom at 120 s elapsed; trace totals
  for Paint, RasterTask, CompositeLayers, UpdateLayerTree and Layout; CDP layer count and area; idle GPU-process CPU
  over 10 s; unused CSS from CSS rule-usage tracking.
- "Usable" is a `performance.mark('kcc:hydrated')` that `Main.ts` sets after the app mounts.
- WebKit: frame deltas and long frames; layout shift through a geometry diff of the sheets and landmarks, scripts-off
  against settled, which also serves Chromium as a cross-check.
- Server response: Navigation Timing `responseStart - requestStart`.
- Harness rules (T013): a frame is on budget at 16.7 ms or less; `long_frames` counts rAF deltas over 50 ms on both
  engines, and Chromium's long-animation-frame entries are kept apart as `loaf_count`. `cls` is the load phase, up to
  the settled page before any scroll or state change; `cls_scroll` is everything after. Add variant re-settles on the
  page its link opens, so no metric mixes two documents. Every page runs once with images off and once with reduced
  motion, phone, Chromium, light, bloomed. Cook mode has no opener below the `lg` breakpoint, so the phone
  `variant-cook` cases measure the variant page closed and say so in their gaps.

### R4. Guards

- **Deterministic guards** are ordinary tests in the E2E suite, so `run.mjs` and the definition of done run them, in
  both engines on the CI key subset (spec FR-015):
  - the count of elements with a computed `filter`, `clip-path`, `mix-blend-mode` or `backdrop-filter`;
  - the count of running animations on filtered or blended elements, and on elements whose ancestor is filtered;
  - filtered area in px², margin 2%;
  - Chromium layer count, exact;
  - CSS and JS bytes per page, gzip level 9 over the response bodies, failing at baseline plus the larger of 1 KB
    and 2%.
- **Timing guards** run in a separate `perf` job in `.github/workflows/build-and-test.yml`, Chromium on SwiftShader,
  alone on its runner: one warm-up, three runs (five until T021), the median. A metric is enforced at its target only
  where a ten-run trial on unchanged code passes ten times out of ten (SC-006). Otherwise it is enforced against the
  baseline: fail when the median exceeds both baseline x 1.25 and baseline plus a floor (4 ms frame p95, 25 ms
  interaction, 150 ms LCP and server response), or when long frames exceed the baseline median plus one. The plan's
  tasks record each metric's outcome.
- Timing tests are marked so the default E2E run skips them and the `perf` job selects them by filter. The marking
  mechanism (TUnit `[Explicit]` or a category filter) is verified in the first task.
- **Verified (T002, TUnit 1.27.0).** `[Explicit]` on the whole test class keeps it out of the default run, and
  `--treenode-filter "/*/*/<Class>/*"` selects it: a filter that matches only explicit tests runs them. A mixed class
  does not work: with a non-explicit sibling in the filter's match, a method-level `[Explicit]` test is skipped even by
  a class wildcard, and runs only when named in full. So mark every perf class `[Explicit]` at class level, and keep
  non-explicit tests out of those classes. The deterministic guards are not explicit and live in their own class.
  A plain unfiltered run (`dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj`, no filter) ran 382 tests
  and neither `PerfCollectorSmokeTests` nor `PerfMatrixTests`, which it neither ran nor listed as skipped.
  `perf:*` scripts call `tests/scripts/perf.mjs <command> [args]`, which runs
  `dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj -c Release -- --treenode-filter "/*/*/<Class>/*"`
  then `perf-report.mjs` or `perf-diff.mjs`. The run builds the test project; build the site and `yarn build:all`
  first. A run that writes no results renders no report and exits non-zero. Tests read their arguments from environment variables:
  - `KCC_PERF_MODE`: `local` (`perf`), `baseline` (`perf:baseline`), `live` (`perf:live`), `capture` (`perf:capture`)
  - `KCC_PERF_CHECK=1`: set by `--check`; enforce budgets and fail
  - `KCC_PERF_FILTER_PAGE`, `KCC_PERF_FILTER_PROFILE`, `KCC_PERF_FILTER_ENGINE`, `KCC_PERF_FILTER_RAMP`,
    `KCC_PERF_FILTER_VISIT`: from `--page`, `--profile`, `--engine`, `--ramp`, `--visit`
  - `KCC_PERF_REF`: the ref `perf:capture <ref>` names
  Classes: `PerfMatrixTests` (perf, baseline, live), `LookCaptureTests` (capture), `TimingGuardTests` (CI `perf` job).
  `perf:diff <before> <after>` runs `perf-diff.mjs <before> <after>` with no test class.
- **Rationale**: 002's R8 found CI timing flaky; deterministic stand-ins are what FR-017 allows. Software raster on a
  GPU-less runner is steady, if slow.
- **Stability trial (T021).** Ten `perf` jobs at once on unchanged code (run 40559904251); 8 finished. No timing
  metric passed ten of ten at its target, and none came near one. Across runners, per case:
  - `hydrated_ms` and `server_response_ms` stayed inside their baseline margins (x1.25, +150 ms) on all 10 cases.
  - `frame_p95_ms` 100-233 ms, `interaction_ms_max` 768-1,544 ms, `lcp_ms` 2,100-3,456 ms; `long_frames` 38-55 on
    home phone and 53-68 on search-3 phone (margin +1); `frames_within_16_7_pct` about 10 points (margin 5). Each
    swings past its margin.
  - Job time 21-50 min; 2 of 10 hit the 60-minute timeout. The guard step took 17.5-25.1 min; the rest of the spread,
    and both timeouts (51 min), was `playwright.ps1 install --with-deps`.
- **Owner decision (2026-10-28).** On CI only `hydrated_ms` and `server_response_ms` enforce, against
  `baseline.ci.json`; every other timing budget carries `ciMode: report-only` and is reported in the job summary.
  `yarn perf --check` still enforces every budget against `baseline.json`. The guard measures one warm-up and three
  runs per case, not five, and the job's timeout is 45 minutes; no case was dropped. A median of three is still the
  middle run, so it reads as the trial's medians of five did, with one outlier absorbed instead of two.
- **Runner calibration (owner, 2026-10-28).** Run 40729163964 failed on `search-3 · phone · chromium · dark ·
  bloomed`, `hydrated_ms` 1970.2 against 1528.4. Every metric on that runner ran a median x1.23 slower than run
  40700034631 on nearly the same code, server response included, so the runner moved, not the site.
  `TimingGuardTests` now times a fixed script, DOM and layout workload in its own browser before and after the cases,
  five samples each after a warm-up, and takes the median of the ten as the run's `calibrationMs`. A run slower than
  the baseline's `calibrationMs` scales each millisecond baseline by the ratio before its margin applies; a faster
  one never tightens it (contracts/budgets.md).
  - Trial: eight `perf` jobs at once (run 40739675154). Runners came in two speeds: three took 161-168 ms on the
    workload with `hydrated_ms` at x0.97 of baseline, five took 216-261 ms at x1.05 to x1.26. Unscaled, two of the
    eight failed. The baseline's own timing values match the fast runners.
  - `baseline.ci.json` takes `calibrationMs` 206.3: per job, the calibration over the job's median `hydrated_ms`
    ratio to baseline is the calibration a job at baseline speed would read (166-218), and 206.3 is their median.
    Replayed against it, all eight jobs pass, the worst case at 85% of its limit, and a fast runner keeps the
    unscaled margin.
- **T055.** No timing metric flips to `target`: none passed ten of ten.

### R5. Baselines and budgets

- `specs/006-performance-sweep/baseline.json`: the full local matrix before any fix (FR-005).
- `tests/KCC.E2ETests/Features/Performance/budgets.json`: per metric, the limit, the mode (`target` or `baseline`)
  and the margin.
- `tests/KCC.E2ETests/Features/Performance/baseline.ci.json`: per-case CI values taken from a CI artifact, because
  layer count and area depend on the platform. Its timing values are the per-case medians of the eight finished
  trial jobs of run 40559904251, each job's own value a median of five runs; its deterministic values and
  `environment` are still the Mac bootstrap below.
- `yarn perf:baseline` rewrites a baseline; a Phase 2 or 3 change flips a metric's mode to `target` (FR-016).
- `yarn perf:baseline` with a filter rewrites only the cases it ran and keeps the rest of `baseline.json`.
- **Budgets as written (T016).** Every metric of the data model has an entry with its owner. All start in
  `baseline` mode, the phase-1 targets already in `limit` (frames within 16.7 ms 95, long frames 0, frame p95
  16.7 ms, interaction 100 ms), so T021 and T055 flip one field. LCP and hydrated carry FR-006's 2.0 s phone and
  1.0 s desktop limits for Phase 2; `cls` carries 0 and a 0.01 margin. Diagnostics (frame max, LoAF count, traces,
  idle GPU, unused CSS, `cls_geometry`) are `report-only`. A third mode, `report-only`, never fails.
  - `frames_within_16_7_pct` in `baseline` mode fails at baseline minus 5 points, the margin R4 leaves open.
  - `cls_scroll` is report-only, owned by Phase 2: search appends a page of results as the reader scrolls, and the
    appended cards move what lies below them, so a scroll-phase shift measures the infinite load, not a regression.
- **`platformSensitive` and the bootstrap `baseline.ci.json` (T017).** Until T020 takes CI's values, the committed
  `baseline.ci.json` is a bootstrap: `DeterministicGuardTests` run with `KCC_PERF_MODE=baseline` on this Mac write
  the key subset's probes and byte totals into it (metric-merged, so timing values from `TimingGuardTests` in the
  same mode join the same cases). The file's `environment.platform` names the .NET runtime identifier it came from
  (`osx-arm64`). `layers`, `layer_area_mpx` and `filtered_area_px2` are `platformSensitive` in `budgets.json`: when
  the runner's platform differs from the file's, the guard marks them `skipped`, prints
  `<case>: <metric> skipped: platform`, and the report shows "skipped: platform". Counts and byte totals are
  enforced everywhere. A case missing from the file fails with `no baseline in baseline.ci.json`. Each guard writes
  its results to `tests/results/perf/deterministic-<engine>/results.json`.
- **Deterministic guards are deterministic.** Three fresh boots gave identical values for every key-subset metric
  in both engines, after two fixes: the seeder dates its reviews from `SeedKeys.ReviewDate` instead of the clock
  (a review's wash key includes its date, so each boot drew different washes on the variant page), and the guard
  boots its own site, because other suites write reviews and notes to the shared one. A case that fails to reach
  its state (WebKit's search load stalled short of three pages once in five runs) is measured once more.
- **Proof (T022 locally).** `filter: blur(1px)` on `.kcc-sheet` failed both engines on every key-subset case with,
  for example, `home · phone · chromium · light · bloomed: filtered_elements 108 > baseline 84 (no increase)` and
  `filtered_area_px2 12313519 > baseline 10253384 (+2%, +0 floor)`. Layer count did not move.
- **Median-of-three key subset in `baseline.json`.** `KCC_PERF_RUNS=N` makes `PerfMatrixTests` measure each case N
  times; timing metrics take the median, deterministic ones the first run. The key subset's Chromium cases (phone,
  bloomed, both ramps, plus their images-off and reduced-motion variants) were re-captured with
  `KCC_PERF_RUNS=3 yarn perf:baseline --page home,search-3,recipe,variant-cook,account --profile phone --engine
  chromium --visit bloomed`, which merged them into `baseline.json` and added `environment.platform`. Their
  variant-cook probe counts moved by one (60 to 61) with the fixed review dates; the other variant-cook cases in
  `baseline.json` still hold values from a boot with clock-dated reviews.
- **`idle_gpu_pct` is a share of one core** (unit `pct-core`, shown as "% of one core"): the GPU process's CPU time
  over wall time, not divided by the core count, so a value over 100 means more than one core was busy.
- **Timing guard run time.** `TimingGuardTests` (10 cases, a warm-up and five runs each, without idle GPU, geometry
  or CSS usage) took 14 min 33 s on this Mac.

### R6. Look captures and the scripts-off check

- Captures: Playwright PNG screenshots with a fixed visit seed, `document.fonts.ready`, settled results and a fixed
  viewport. A visit started 400 s back is fully bloomed and static, so both engines capture it; a bare visit is
  frozen through CDP, Chromium only.
- Diff: `pixelmatch` with `pngjs` (two dev dependencies), YIQ perceptual delta with anti-alias detection, writing a
  heat map. The diff only flags regions; the owner judges (spec Clarifications).
- `perf-diff` writes a look round, before | after | diff for each ramp, profile and visit state, for
  `/speckit-preview-run`. Captures live under the gitignored `tests/results/perf/captures/<ref>/`; the before set is
  captured once from a worktree at the pre-fix commit.
- Scripts-off: a `JavaScriptEnabled=false` context at load against the scripts-on page once settled, bloomed. A
  signed-out page compares in light; dark compares for a signed-in member with a saved Dark ramp.
- **Alternatives rejected**: odiff (native binary download); an SSIM pass mark (a score never passes a page);
  committing PNGs (size).

### R7. Card shadows move to a static layer

- **Decision**: the fall (both drop-shadows) moves off `.kcc-torn` onto a new static element under the top sheet,
  built like the blank: a filtered wrapper around a clipped span with the same tear. `.kcc-torn` carries no filter.
  `KccSheet` and every Razor slip render it.
- **Rationale**: F2 item 1. The paper is opaque inside the tear, so the shadow outside the clip is the same pixels;
  the shadow's input never changes while the washes bloom or the sheet lifts, so it rasterises once.
- **Alternatives rejected**: a single shadow instead of two (changes the 1 px edge); a 9-slice pre-blurred image
  (visible repetition); keeping the filter and promoting the washes (the parent filter still re-runs on every
  change of its input).
- Kit invariant change: CLAUDE.md and `docs/brand/kit.md` say the fall's filter sits on `.kcc-torn`; both change in
  the same commit.

### R8. Stepped bloom

- **Decision**: the 90 s bloom advances in about 45 steps through a generated `linear()` staircase, front-loaded so
  the start reads smooth. Reduced motion is unchanged. The owner sees it in a look round.
- **Rationale**: F2 item 3. Fewer distinct frames means the compositor and WebKit's repaint run about 45 times per
  wash instead of 5,400.

### R9. Grain and crayon as a background

- **Decision**: paint the grain and crayon as `#app` backgrounds with `background-blend-mode`, each ramp's opacity
  baked into the layer, and drop the page-sized blended overlays and the `isolation` they need. The SVG noise stays,
  so WebKit's render does not change.
- **Rationale**: F2 item 4; identical maths over a flat desk, painted once.

### R10. Tear polygons applied once

- **Decision**: `tears.ts` writes `clip-path` directly on grouped selectors per preset (sheet and blank inside the
  preset's slip), so each polygon is parsed once instead of substituted into every element. `--tear-*` stays for
  `PhotoStrip.vue`. `Tears.css` comes from `yarn tears` only.
- **Then**: a halved-point variant (40 or 80 points per side, saving about 58 KB raw and 9 KB gzipped) is generated
  and shown in a look round; it ships only if the owner approves.
- **Rationale**: F2 item 5 and F3.

### R11. Font Awesome subset

- **Decision**: a build step keeps only the icon rules on a safety list: every icon name found in `.vue`, `.cshtml`
  and `.ts` sources, `RecipeIcons.All` and the taxonomy defaults. It also drops the unused animation, stack and
  rotate rules. The backoffice icon picker stays open, so an icon chosen there outside the list renders blank.
- A Vitest guard fails when source references an icon outside the kept set.
- Font files are not subset in Phase 1; Phase 3 owns them.
- **Rationale**: F3; about 127 KB raw and 33 KB gzipped off every page.

### R12. Washes, staged

- **Decision**: R7, R8 and R9 land first. The matrix is re-measured. If WebKit is still over the frame budget, the
  wash filter is rebuilt as pre-baked textures generated at build time and shown in a look round; it ships only if
  the owner approves. Otherwise the live filter stays.
- **Rationale**: F2 item 2; baking carries the highest look risk, because the grain tints each colour channel
  differently and `saturate()` amplifies that.
- **Expected**: Chromium within budget after stage one (shadows off alone gives 99.2% on SwiftShader). WebKit likely
  needs stage two.

### R13. Hover lift

- **Decision**: no `will-change`. Hover is within budget today, and R7 removes the shadow re-blur the lift
  triggers. Re-measure after R7.

### R14. Off-screen cards

- **Decision**: no `content-visibility` in Phase 1. It clips the label, tile, blank and shadow unless
  `overflow-clip-margin` covers them, and WebKit support for that is unverified.

### R15. Seed data

- **Decision**: the Testing seeder grows to 36 recipes, and gives recipes images it generates deterministically at
  seed time (no licensed photos in the repository). E2E tests that count recipes are updated. The seeder's writes
  follow the SQLite rules: one core scope, `Constants.Locks.ContentTree` before the first read.

### R16. Live measurement

- Variables: `KCC_PERF_LIVE_URL`, `KCC_PERF_MEMBER_USERNAME`, `KCC_PERF_MEMBER_PASSWORD`, `KCC_PERF_BYPASS_TOKEN`.
  `MemberSession` takes its variable names as parameters. The run signs in once and reuses the storage state, so the
  sign-in rate limit is not hit. It reads only and never submits.
- Cloudflare bot checks: a free WAF custom rule that skips bot checks for requests carrying the header
  `X-KCC-Perf: <KCC_PERF_BYPASS_TOKEN>`, applied by the owner by hand and recorded in `docs/hosting/runbook.md`.
  It never touches the `/umbraco` Access policy.
- Server response is reported as min, median and max over ten runs, with `cf-cache-status` and `content-encoding`.

### R17. Local load milestones are uncompressed

Kestrel does not compress; Cloudflare does in production. Local load milestones and server response are guarded
against the baseline only; the live report shows the compressed numbers. Phase 3 owns compression.

## For Phases 2 and 3

- One 370 KB (117 KB gzipped) script chunk with no code splitting; it ships Vue's runtime template compiler, because
  `App.vue` compiles the header, body and footer templates at runtime.
- Every page is rendered twice (Razor, then Node), and the SSR body ships again as a template string.
- The SSR cache key includes the visit's bloom seed, so cross-visitor hits are rare; Vue's SSR compile cache is
  unbounded.
- Font Awesome font files are 276-357 KB each; Hazelnut's four faces are 60-65 KB each.
- Both logo images download on every page, because a hidden image is still fetched (`AppHeader.Component.vue`).
- No response compression in Kestrel; check brotli at Cloudflare (54 KB against 68 KB gzip for the main stylesheet).
- Recipe images: one 192x192 WebP size, no `srcset`, lazy loading only on the home stacker.
- SC-002 on the phone profile (owner, 2026-10-27): worst interaction 136-200 ms after the tuck fix, 128-176 ms with
  no tuck, against 100 ms. Phase 2 takes it with the script and hydration work.
- SC-001 (owner, 2026-10-27): at f24896f4, search-3 phone bloomed keeps 99.1% of frames within 16.7 ms in Chromium
  but has 5 frames over 50 ms (worst 208 ms; the bare visit has 3, worst 258 ms), and WebKit keeps 42% with 28 over
  50 ms (worst 600 ms). Phase 2 takes the remaining long frames in both engines.
- Desktop Chromium search-3 (owner, 2026-10-27): worst interaction 56-64 ms at baseline, 88-104 ms at f24896f4 (three
  cases over 100 ms), long frames 0 to 2-4. Phase 2 takes it with the interaction work.


## Re-measure after Phase 1 fixes

T036. Commit `2ac3fff5` (R7 fall, R8 stepped bloom, R9 grain background, R10 tears applied once, R11 icon subset), `yarn build:all`
then a Release build, then `yarn perf` on the same Mac as the baseline: 232 cases, 69 min. The committed record is
`specs/006-performance-sweep/after-phase1.json`, beside the untouched pre-fix `baseline.json`. The 96 WebKit cases in it
come from a second `yarn perf --engine webkit` run: in the first run WebKit's frame cadence drifted to about 34 ms on the
late static pages (error, status-404, account: 0% within 16.7 ms, p95 35 ms, where the baseline held 18 ms), and neither the
repeat nor a three-run median reproduced it (error page: median 19 ms, 41-50% within). Chromium cases are single runs,
except the three-run medians listed below. WebKit has no throttling and runs unthrottled.

Full runs do not support `KCC_PERF_RUNS=3` inside the time budget (about 3.5 h), so the key cases were re-measured with
`KCC_PERF_RUNS=3 yarn perf`: search-3 and error, phone, both engines, both ramps, bare/mid/bloomed, plus images-off and
motion-reduced; search-1 and variant-cook, phone, Chromium, light, bloomed (with variants); home, desktop, Chromium, light,
bare. The medians agreed with the single runs except where noted.

### Verdicts

| Criterion | Verdict | Numbers |
|---|---|---|
| SC-001, Chromium phone, search-3, bloomed, both ramps: 95% of frames within 16.7 ms and no frame over 50 ms | **Partly met: frame share yes, frame ceiling no** | Within 16.7 ms: 99.2% light, 99.2% dark (baseline 90.3, 93.4); three-run medians 99.2, 99.3. p95 9.2 ms (24.7, 17.6). Over 50 ms: 4 long frames per ramp, worst 216 ms light, 217 ms dark (three-run medians 225, 225); the baseline had 8 and 258 ms. Under reduced motion the same page has 0 long frames, worst 34 ms (three-run 42 ms), so the stalls come with the animated sheets. |
| SC-001, WebKit | **Not met** | See "WebKit standing". |
| SC-002, every interaction within 100 ms | **Not met** | Chromium desktop: all 96 cases within 100 ms. Chromium phone: every page type is over, 104-296 ms (list below); the baseline was 144-1040 ms. WebKit phone and desktop: 104-2,512 ms. |
| SC-003, the bloom adds no frame over 50 ms | **Met in Chromium** | `bloom_frame_max_ms` over the 5 s mid-bloom window, home, search-1, search-3 and recipe, both ramps, both profiles: 9.4-10.4 ms, 0 long frames, 100% within 16.7 ms (baseline phone 26-33 ms max, 18.7-26.7% within). WebKit is not measured at mid-bloom by the harness. |
| SC-004, no load milestone, weight or server response worse than baseline | **Not met: one real LCP regression, two bytes-level notes** | See the list below. |

SC-002 detail, Chromium phone worst interaction by page (after; baseline in brackets), bloomed visit, light | dark:
home 264 | 256 (232 | 232); search-1 264 | 248 (280 | 264); search-3 272 | 272 (456 | 392); recipe 208 | 216 (336 | 352);
variant-cook 200 | 208 (200 | 288); add-variant 264 | 288 (0 | 0, no interaction was recorded at baseline); sign-in 264 | 256
(328 | 264); settings 168 | 168 (328 | 336). create-recipe, account, error and status-404 are within 100 ms. Reduced motion:
home 200, search-1 128, search-3 104, add-variant 280. Bare and mid visits sit at 184-296 ms. The worst entry is the same
size on every page, so it is a fixed cost of the first input on the 4x throttled phone, not a card cost; this run did not
attribute it.

SC-003 note: the mid-bloom cases' overall `frame_max_ms` (including the scroll phase) is still 225-258 ms, and the same
stalls appear in the bare and bloomed visits.

SC-004 list, numbers from the single run and, where marked, the three-run median:
- **LCP, phone, search-1 and search-3: worse, confirmed.** Single run 1,792-1,828 ms against 1,420-1,456 ms (+365 to
  +385 ms) on every ramp and visit; three-run medians search-1 light bloomed 1,820 ms (baseline 1,436 ms), search-3 light
  bloomed 1,816 ms (1,428 ms), search-3 dark bloomed 1,812 ms (1,420 ms), search-3 images off 1,720 ms (1,332 ms), search-1
  images off 1,740 ms (1,316 ms), motion reduced 1,804-1,812 ms. All stay under FR-006's 2.0 s, but fail the baseline
  margin (x1.25 and +150 ms) in 13 cases of the single run.
- **LCP, phone, variant-cook: worse in the images-off and motion-reduced variants, confirmed.** Three-run medians 1,840 ms
  (baseline 1,448 ms) and 1,892 ms (1,512 ms). The bloomed light case itself is 1,888 ms against a baseline of 1,800 ms,
  within the margin.
- LCP elsewhere improved or held: every other phone page by 100-320 ms; desktop within +/-30 ms of baseline and under
  1.0 s (home desktop light bare read 284 ms against 132 ms in the single run; the three-run median is 76 ms, so it was noise).
- **JS gzip: +48 B on every one of the 232 cases** (117,218 B to 117,266 B), inside the 1 KB margin.
- CSS gzip: -33,643 B on every case (70,397 B to 36,754 B). Fonts, images and `cls` (0) did not change.
- Hydrated: better by 20-210 ms on the Chromium cases; WebKit cases +/-2 ms, except home desktop dark bloomed (53 to 85 ms).
- Server response: at most +10 ms (variant-cook phone images off 3.9 to 14.1 ms), far inside the +150 ms floor.
- **Baked wash textures: +5,412 B per page, accepted.** The nine PNGs (13,720 B) are cached asset files, preloaded,
  not data URIs: inline they added 7,177 B to the CSS gzip (36,754 B to 43,931 B). With them out, CSS gzip is 28,446 B,
  8,308 B under the CI baseline, and `image_bytes` is 13,720 B over it. The owner chose to raise both `image_bytes`
  baselines by 13,720 B on every images-on case (2026-10-27). Settled captures with the inline and the file textures are
  identical (look round 5). With images off the textures are blocked, so those cases draw no washes.
- Expected, not a milestone: `clip_path_elements` rises on every case (phone bare mean 34 to 51) because the fall is now a
  clipped static element.
- Probe counts: `filtered_elements` 61 to 69 on the variant-cook images-off and motion-reduced cases only (the other
  variant-cook cases are equal; R5 records that those two baselines come from a boot with clock-dated reviews), and
  `animated_filtered` 57 to 58 on search-3 phone dark mid (wash count at that instant).

Idle and layers, Chromium phone means: idle GPU 995% of one core (bare) and 1,433% (mid) down to 8.8% and 9.2% (worst case
17.6%, home phone bare); layers 104 to 79 (bare) and 171 to 138 (mid); layer area 11.7 to 8.2 Mpx; `animated_under_filter`
30 to 0 (bare) and 35 to 0 (mid). `filtered_elements`, `filtered_area_px2` and `animated_filtered` are unchanged (the fall
moved off `.kcc-torn` onto a static element; the washes keep their filter).

Chromium phone frame budget (FR-007), single runs: the share within 16.7 ms is at or above 95% on every page except recipe
dark bloomed (94.5%) and recipe dark mid (94.8%). Every phone page still has frames over 50 ms, between 1 and 24 per run,
worst 442 ms (home light bloomed); home and sign-in carry 17-24, the search and recipe pages 1-8. Chromium desktop is within
budget on every page, ramp and visit.

### WebKit standing for T046

WebKit is not within the frame budget, so T047 to T049 are needed. Owner, 2026-10-27, after the look rounds: start Phase 7. The share within 16.7 ms is 32-54% on every WebKit
case after the fixes and was 32-48% before: headless WebKit presents frames at 17-19 ms, so the 16.7 ms threshold counts
almost none of them, and the share does not measure the fixes. The deciding numbers are the long frames and worst frame:

- Phone, bloomed: home 33-34 long frames, worst 2.3 s (5.3 s before); search-1 11, 2.3-2.4 s (5.4 s); search-3 28 long
  frames, worst 3.6 s (15.1-17.5 s); recipe 6, 1.0 s; variant-cook 5, 1.6 s; sign-in 33, 2.3 s; settings 4, 0.96 s.
- Desktop, bloomed: home 17-19, worst 0.77 s (1.3 s); search-1 13, 0.52 s; search-3 29-30, 0.53-0.54 s (0.69-0.70 s);
  recipe 4, 0.84-0.85 s; variant-cook 4, 2.8 s; sign-in 18-19, 0.76-0.78 s.
- Static pages (create-recipe, add-variant, account, error, status-404): 0 long frames, worst 17-19 ms, both profiles.
- Worst interaction delay (hover and key presses), bloomed: phone 0.86-2.5 s (home 2.2 s, search-3 2.2-2.5 s); desktop
  0.42-0.99 s (settings and create-recipe not measured). Bare visits: phone 136-304 ms, desktop 72-120 ms.
- Search-3 phone, three-run median of the repeat: p95 18-19 ms, 28 long frames, worst 3.7 s. Search-3 desktop bloomed p95
  183-249 ms (baseline 68 ms) and 29-30 long frames (20): the one case where the p95 worsened in the repeat.

Improvement is large in the worst frame (search-3 phone 15.1 s to 3.6 s, home 5.3 s to 2.3 s) and the interaction delay
(search-3 phone 12-14 s to 2.2-2.5 s), and long-frame counts are unchanged to slightly lower. R12's stage-two trigger
(the wash filter `url(#kcc-bloom)`) therefore holds.

Full WebKit table (before -> after, one row per case):

| page | profile | ramp | visit | within 16.7 ms % | p95 ms | long frames | worst frame ms | hover/interaction ms |
|---|---|---|---|---|---|---|---|---|
| home | phone | light | bare | 35.8 → 39.9 | 52 → 69 | 25 → 27 | 436 → 295 | 352 → 200 |
| home | phone | light | bloomed | 35.7 → 41.9 | 852 → 292 | 34 → 34 | 5283 → 2301 | 5176 → 2200 |
| home | phone | dark | bare | 39.2 → 41.6 | 75 → 67 | 25 → 26 | 414 → 295 | 328 → 200 |
| home | phone | dark | bloomed | 36.8 → 40.3 | 858 → 275 | 34 → 33 | 5371 → 2305 | 5264 → 2216 |
| home | desktop | light | bare | 35.9 → 43.6 | 49 → 72 | 12 → 15 | 152 → 122 | 88 → 88 |
| home | desktop | light | bloomed | 37.9 → 38.8 | 88 → 78 | 21 → 17 | 1297 → 767 | 1208 → 496 |
| home | desktop | dark | bare | 38.5 → 43.7 | 41 → 64 | 10 → 13 | 167 → 132 | 104 → 72 |
| home | desktop | dark | bloomed | 35.4 → 40.5 | 99 → 89 | 21 → 19 | 1308 → 768 | 1208 → 560 |
| search-1 | phone | light | bare | 38.2 → 42.1 | 43 → 38 | 9 → 10 | 226 → 189 | 264 → 216 |
| search-1 | phone | light | bloomed | 37.1 → 43 | 18 → 19 | 12 → 11 | 5444 → 2347 | 5360 → 2256 |
| search-1 | phone | dark | bare | 36.5 → 43.5 | 41 → 38 | 10 → 10 | 218 → 191 | 264 → 216 |
| search-1 | phone | dark | bloomed | 35.1 → 42.9 | 18 → 19 | 12 → 11 | 5423 → 2347 | 5328 → 2264 |
| search-1 | desktop | light | bare | 35.2 → 40.5 | 59 → 58 | 10 → 7 | 143 → 133 | 120 → 88 |
| search-1 | desktop | light | bloomed | 34.2 → 43.3 | 327 → 319 | 9 → 13 | 598 → 528 | 512 → 440 |
| search-1 | desktop | dark | bare | 34.4 → 40.4 | 58 → 59 | 8 → 8 | 148 → 135 | 104 → 88 |
| search-1 | desktop | dark | bloomed | 36.9 → 38.1 | 328 → 320 | 9 → 13 | 595 → 518 | 512 → 424 |
| search-3 | phone | light | bare | 37.8 → 42.8 | 42 → 38 | 16 → 14 | 238 → 241 | 248 → 224 |
| search-3 | phone | light | bloomed | 38.5 → 42.5 | 18 → 19 | 28 → 28 | 15106 → 3618 | 12272 → 2240 |
| search-3 | phone | dark | bare | 38.3 → 43 | 40 → 38 | 16 → 14 | 224 → 245 | 240 → 216 |
| search-3 | phone | dark | bloomed | 36 → 42.6 | 18 → 19 | 28 → 28 | 17518 → 3639 | 13640 → 2512 |
| search-3 | desktop | light | bare | 35 → 44.4 | 31 → 42 | 8 → 10 | 148 → 141 | 184 → 168 |
| search-3 | desktop | light | bloomed | 37.1 → 41.3 | 68 → 249 | 20 → 30 | 690 → 542 | 600 → 448 |
| search-3 | desktop | dark | bare | 36.3 → 41.8 | 31 → 41 | 8 → 8 | 147 → 139 | 184 → 168 |
| search-3 | desktop | dark | bloomed | 36.3 → 39.7 | 68 → 183 | 20 → 29 | 702 → 528 | 624 → 440 |
| recipe | phone | light | bare | 36.8 → 45.2 | 18 → 19 | 4 → 3 | 277 → 230 | 200 → 144 |
| recipe | phone | light | bloomed | 32.9 → 43.6 | 18 → 19 | 5 → 6 | 1733 → 1012 | 1616 → 960 |
| recipe | phone | dark | bare | 37.3 → 44.8 | 17 → 19 | 4 → 4 | 281 → 224 | 200 → 136 |
| recipe | phone | dark | bloomed | 34.9 → 47.3 | 18 → 19 | 5 → 6 | 1667 → 1009 | 1560 → 960 |
| recipe | desktop | light | bare | 35.8 → 43.3 | 87 → 45 | 7 → 4 | 136 → 115 | 144 → 112 |
| recipe | desktop | light | bloomed | 36 → 43.4 | 359 → 293 | 4 → 4 | 904 → 841 | 808 → 744 |
| recipe | desktop | dark | bare | 38.6 → 44.4 | 88 → 49 | 7 → 4 | 130 → 115 | 160 → 128 |
| recipe | desktop | dark | bloomed | 34.2 → 49.4 | 348 → 295 | 4 → 4 | 905 → 850 | 808 → 752 |
| variant-cook | phone | light | bare | 36.8 → 43.9 | 18 → 19 | 6 → 6 | 462 → 377 | 216 → 152 |
| variant-cook | phone | light | bloomed | 34.1 → 46.6 | 17 → 19 | 5 → 5 | 3211 → 1628 | 3104 → 1528 |
| variant-cook | phone | dark | bare | 37 → 44.5 | 18 → 19 | 5 → 5 | 468 → 377 | 232 → 144 |
| variant-cook | phone | dark | bloomed | 35.2 → 39.7 | 17 → 19 | 5 → 5 | 3192 → 1640 | 3088 → 1536 |
| variant-cook | desktop | light | bare | 35.8 → 41.6 | 18 → 18 | 5 → 5 | 341 → 289 | 16 → 16 |
| variant-cook | desktop | light | bloomed | 35.1 → 38.8 | 18 → 18 | 4 → 4 | 3067 → 2788 | 1072 → 992 |
| variant-cook | desktop | dark | bare | 37.4 → 37.8 | 18 → 18 | 5 → 5 | 364 → 300 | 16 → 16 |
| variant-cook | desktop | dark | bloomed | 36.2 → 39.1 | 19 → 18 | 4 → 4 | 2977 → 2807 | 1072 → 992 |
| create-recipe | phone | light | bare | 32.4 → 38.5 | 17 → 17 | 0 → 0 | 17 → 18 | 0 → 0 |
| create-recipe | phone | light | bloomed | 35.3 → 54.3 | 18 → 18 | 0 → 0 | 18 → 18 | 0 → 0 |
| create-recipe | phone | dark | bare | 41.2 → 38.2 | 18 → 18 | 0 → 0 | 18 → 18 | 0 → 0 |
| create-recipe | phone | dark | bloomed | 38.2 → 44.1 | 18 → 18 | 0 → 0 | 18 → 18 | 0 → 0 |
| create-recipe | desktop | light | bare | 38.2 → 44.1 | 17 → 18 | 0 → 0 | 18 → 18 | 0 → 0 |
| create-recipe | desktop | light | bloomed | 32.4 → 35.3 | 17 → 18 | 0 → 0 | 17 → 18 | 0 → 0 |
| create-recipe | desktop | dark | bare | 34.5 → 32.4 | 17 → 17 | 0 → 0 | 17 → 18 | 0 → 0 |
| create-recipe | desktop | dark | bloomed | 35.3 → 41.2 | 17 → 18 | 0 → 0 | 17 → 18 | 0 → 0 |
| add-variant | phone | light | bare | 37 → 35.2 | 17 → 17 | 0 → 0 | 18 → 18 | 0 → 0 |
| add-variant | phone | light | bloomed | 37.7 → 40.7 | 17 → 18 | 0 → 0 | 18 → 18 | 0 → 0 |
| add-variant | phone | dark | bare | 33.3 → 40.7 | 17 → 18 | 0 → 0 | 17 → 18 | 0 → 16 |
| add-variant | phone | dark | bloomed | 35.2 → 40.7 | 18 → 18 | 0 → 0 | 18 → 18 | 0 → 0 |
| add-variant | desktop | light | bare | 35.3 → 44.1 | 17 → 18 | 0 → 0 | 18 → 18 | 0 → 16 |
| add-variant | desktop | light | bloomed | 32.4 → 38.2 | 17 → 18 | 0 → 0 | 17 → 18 | 0 → 0 |
| add-variant | desktop | dark | bare | 35.3 → 41.2 | 18 → 18 | 0 → 0 | 18 → 18 | 0 → 0 |
| add-variant | desktop | dark | bloomed | 33.3 → 44.1 | 17 → 18 | 0 → 0 | 17 → 18 | 0 → 0 |
| account | phone | light | bare | 36.4 → 36.4 | 17 → 18 | 0 → 0 | 18 → 18 | 0 → 0 |
| account | phone | light | bloomed | 38.6 → 40.9 | 17 → 18 | 0 → 0 | 18 → 18 | 0 → 0 |
| account | phone | dark | bare | 38.6 → 36.4 | 17 → 18 | 0 → 0 | 18 → 18 | 0 → 0 |
| account | phone | dark | bloomed | 36.4 → 40 | 18 → 18 | 0 → 0 | 18 → 18 | 0 → 0 |
| account | desktop | light | bare | 32.4 → 38.2 | 18 → 18 | 0 → 0 | 18 → 18 | 0 → 0 |
| account | desktop | light | bloomed | 38.2 → 44.1 | 18 → 18 | 0 → 0 | 18 → 18 | 0 → 0 |
| account | desktop | dark | bare | 38.2 → 50 | 18 → 18 | 0 → 0 | 18 → 18 | 0 → 0 |
| account | desktop | dark | bloomed | 41.2 → 42.9 | 18 → 18 | 0 → 0 | 18 → 18 | 0 → 0 |
| sign-in | phone | light | bare | 38.1 → 40.4 | 80 → 68 | 27 → 27 | 422 → 295 | 336 → 200 |
| sign-in | phone | light | bloomed | 36.7 → 38.4 | 846 → 280 | 34 → 33 | 5290 → 2289 | 5184 → 2200 |
| sign-in | phone | dark | bare | 38 → 38.6 | 65 → 64 | 25 → 26 | 431 → 302 | 344 → 200 |
| sign-in | phone | dark | bloomed | 36.8 → 38.5 | 855 → 275 | 34 → 33 | 5426 → 2300 | 5320 → 2216 |
| sign-in | desktop | light | bare | 35.8 → 39.7 | 48 → 72 | 11 → 15 | 153 → 134 | 96 → 64 |
| sign-in | desktop | light | bloomed | 36.7 → 38.2 | 94 → 76 | 19 → 19 | 1307 → 765 | 1208 → 504 |
| sign-in | desktop | dark | bare | 34.5 → 38.7 | 45 → 51 | 12 → 13 | 166 → 128 | 80 → 64 |
| sign-in | desktop | dark | bloomed | 36.9 → 40.6 | 98 → 82 | 22 → 18 | 1305 → 757 | 1216 → 504 |
| settings | phone | light | bare | 36.6 → 42.6 | 17 → 18 | 4 → 4 | 282 → 209 | 200 → 128 |
| settings | phone | light | bloomed | 34.7 → 38.8 | 17 → 18 | 4 → 4 | 1395 → 963 | 1296 → 864 |
| settings | phone | dark | bare | 36.3 → 39.3 | 18 → 18 | 4 → 4 | 270 → 212 | 184 → 128 |
| settings | phone | dark | bloomed | 35.5 → 37.2 | 17 → 18 | 4 → 4 | 1509 → 959 | 1416 → 872 |
| settings | desktop | light | bare | 48.1 → 40 | 90 → 69 | 3 → 3 | 102 → 93 | 0 → 0 |
| settings | desktop | light | bloomed | 35.3 → 37.3 | 715 → 536 | 4 → 4 | 889 → 668 | 0 → 0 |
| settings | desktop | dark | bare | 37 → 41.8 | 97 → 70 | 3 → 3 | 109 → 93 | 0 → 0 |
| settings | desktop | dark | bloomed | 33.3 → 40.4 | 643 → 545 | 4 → 4 | 832 → 660 | 0 → 0 |
| error | phone | light | bare | 35.3 → 45.7 | 17 → 19 | 0 → 0 | 18 → 19 | 0 → 0 |
| error | phone | light | bloomed | 35.3 → 38.2 | 17 → 19 | 0 → 0 | 18 → 19 | 0 → 0 |
| error | phone | dark | bare | 35.3 → 52.9 | 17 → 19 | 0 → 0 | 18 → 19 | 0 → 0 |
| error | phone | dark | bloomed | 44.1 → 44.1 | 18 → 18 | 0 → 0 | 18 → 18 | 0 → 0 |
| error | desktop | light | bare | 38.2 → 40 | 18 → 19 | 0 → 0 | 18 → 19 | 0 → 0 |
| error | desktop | light | bloomed | 35.3 → 44.1 | 18 → 19 | 0 → 0 | 18 → 19 | 0 → 0 |
| error | desktop | dark | bare | 35.3 → 35.3 | 17 → 18 | 0 → 0 | 17 → 19 | 0 → 0 |
| error | desktop | dark | bloomed | 38.2 → 42.9 | 18 → 19 | 0 → 0 | 18 → 19 | 0 → 0 |
| status-404 | phone | light | bare | 35.3 → 50 | 17 → 19 | 0 → 0 | 18 → 19 | 0 → 0 |
| status-404 | phone | light | bloomed | 44.1 → 35.3 | 18 → 19 | 0 → 0 | 18 → 19 | 0 → 0 |
| status-404 | phone | dark | bare | 35.3 → 48.6 | 18 → 19 | 0 → 0 | 18 → 19 | 0 → 0 |
| status-404 | phone | dark | bloomed | 32.4 → 50 | 17 → 19 | 0 → 0 | 18 → 19 | 0 → 0 |
| status-404 | desktop | light | bare | 35.3 → 41.2 | 17 → 19 | 0 → 0 | 17 → 19 | 0 → 0 |
| status-404 | desktop | light | bloomed | 39.4 → 40 | 18 → 19 | 0 → 0 | 18 → 19 | 0 → 0 |
| status-404 | desktop | dark | bare | 38.2 → 45.7 | 17 → 19 | 0 → 0 | 18 → 19 | 0 → 0 |
| status-404 | desktop | dark | bloomed | 33.3 → 47.1 | 18 → 19 | 0 → 0 | 18 → 19 | 0 → 0 |

### Every case, before -> after

Chromium and WebKit, phone and desktop, both ramps. A cell with one value is unchanged. `-` means the engine does not
record it (WebKit has no LCP, layers or idle GPU). "bare" is a visit at 0 s, "mid" at 120 s, "bloomed" at 400 s.
Columns: frames within 16.7 ms, frame p95, worst frame, long frames over 50 ms, worst interaction, LCP, hydrated, layout
shift, gzip CSS and JS bytes, layers, idle GPU as a share of one core, filtered elements, animated filtered elements.

#### home

| case | within16.7% | p95 ms | max ms | long | interact ms | LCP ms | hydr ms | cls | css gz | js gz | layers | idle GPU %core | filtered | anim filt |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| phone · chromium · light · bare | 54.7 → 96.7 | 207.5 → 9.3 | 399.3 → 240.8 | 51 → 19 | 416 → 296 | 2904 → 2836 | 1311.3 → 1162 | 0 | 70397 → 36754 | 117218 → 117266 | 162 → 126 | 1341.4 → 17.6 | 84 | 42 |
| phone · chromium · light · bloomed | 93.4 → 96.8 | 24.8 → 9.3 | 264.7 → 442.1 | 24 → 17 | 232 → 264 | 1500 → 1496 | 1300.3 → 1156.4 | 0 | 70397 → 36754 | 117218 → 117266 | 162 → 126 | 0.2 → 0.1 | 84 | 0 |
| phone · chromium · light · mid | 60.3 → 96.5 | 66.6 → 9.3 | 283.7 → 249.6 | 34 → 22 | 472 → 280 | 1484 → 1184 | 1302.7 → 1156.6 | 0 | 70397 → 36754 | 117218 → 117266 | 162 → 126 | 1406.3 → 5.9 | 84 | 34 |
| phone · chromium · dark · bare | 64 → 96.2 | 40.8 → 9.3 | 274.8 → 240.8 | 22 | 440 → 288 | 1456 → 1356 | 1299.9 → 1156.8 | 0 | 70397 → 36754 | 117218 → 117266 | 162 → 126 | 1217.6 → 16.2 | 84 | 42 |
| phone · chromium · dark · bloomed | 93.4 → 96.3 | 24 → 9.3 | 258.9 → 242 | 23 → 18 | 232 → 256 | 1516 → 1208 | 1309.3 → 1155.3 | 0 | 70397 → 36754 | 117218 → 117266 | 162 → 126 | 0.2 → 0.1 | 84 | 0 |
| phone · chromium · dark · mid | 64.9 → 96.4 | 65.8 → 9.3 | 266.8 → 249.9 | 35 → 24 | 528 → 288 | 1464 → 1188 | 1300.3 → 1156.4 | 0 | 70397 → 36754 | 117218 → 117266 | 162 → 126 | 1453.2 → 6.1 | 84 | 34 |
| phone · chromium · light · bloomed · images off | 92.9 → 96.6 | 24.8 → 9.3 | 249.9 → 224.8 | 23 → 19 | 232 → 264 | 1432 → 1144 | 1223.6 → 1020.9 | 0 | 70397 → 36754 | 117218 → 117266 | 165 → 126 | 0.2 → 0.1 | 87 | 0 |
| phone · chromium · light · bloomed · motion reduced | 94.6 → 97.7 | 16.8 → 9.2 | 374.4 → 216.7 | 16 → 10 | 320 → 200 | 1524 → 1200 | 1314.7 → 1152.9 | 0 | 70397 → 36754 | 117218 → 117266 | 165 → 126 | 0.1 | 87 | 0 |
| phone · webkit · light · bare | 35.8 → 39.9 | 52 → 69 | 436 → 295 | 25 → 27 | 352 → 200 | - | 122 → 83 | - | 70397 → 36754 | 117218 → 117266 | - | - | 84 | 42 |
| phone · webkit · light · bloomed | 35.7 → 41.9 | 852 → 292 | 5283 → 2301 | 34 | 5176 → 2200 | - | 91 → 75 | - | 70397 → 36754 | 117218 → 117266 | - | - | 84 | 0 |
| phone · webkit · dark · bare | 39.2 → 41.6 | 75 → 67 | 414 → 295 | 25 → 26 | 328 → 200 | - | 75 → 83 | - | 70397 → 36754 | 117218 → 117266 | - | - | 84 | 42 |
| phone · webkit · dark · bloomed | 36.8 → 40.3 | 858 → 275 | 5371 → 2305 | 34 → 33 | 5264 → 2216 | - | 92 → 90 | - | 70397 → 36754 | 117218 → 117266 | - | - | 84 | 0 |
| desktop · webkit · light · bare | 35.9 → 43.6 | 49 → 72 | 152 → 122 | 12 → 15 | 88 | - | 87 → 81 | - | 70397 → 36754 | 117218 → 117266 | - | - | 84 | 42 |
| desktop · webkit · light · bloomed | 37.9 → 38.8 | 88 → 78 | 1297 → 767 | 21 → 17 | 1208 → 496 | - | 87 → 81 | - | 70397 → 36754 | 117218 → 117266 | - | - | 84 | 0 |
| desktop · webkit · dark · bare | 38.5 → 43.7 | 41 → 64 | 167 → 132 | 10 → 13 | 104 → 72 | - | 89 → 85 | - | 70397 → 36754 | 117218 → 117266 | - | - | 84 | 42 |
| desktop · webkit · dark · bloomed | 35.4 → 40.5 | 99 → 89 | 1308 → 768 | 21 → 19 | 1208 → 560 | - | 53 → 86 | - | 70397 → 36754 | 117218 → 117266 | - | - | 84 | 0 |
| desktop · chromium · light · bare | 99.5 | 9.1 → 9.2 | 25 → 33.3 | 0 | 40 | 132 → 284 | 94.4 → 85.3 | 0 | 70397 → 36754 | 117218 → 117266 | 165 → 135 | 24.2 → 3 | 84 | 42 |
| desktop · chromium · light · bloomed | 99.6 → 99.8 | 8.9 → 9 | 34 → 33.3 | 0 | 40 | 88 → 80 | 57.3 → 48.8 | 0 | 70397 → 36754 | 117218 → 117266 | 165 → 135 | 0.3 → 0.4 | 84 | 0 |
| desktop · chromium · light · mid | 99.8 → 99.6 | 9 → 9.5 | 24.4 → 25 | 0 | 32 | 72 → 68 | 57.9 → 48.9 | 0 | 70397 → 36754 | 117218 → 117266 | 165 → 135 | 50 → 2.4 | 84 | 34 |
| desktop · chromium · dark · bare | 99.8 → 99.6 | 9.1 → 10 | 24.9 → 25.1 | 0 | 40 → 32 | 80 → 184 | 59.3 → 48.9 | 0 | 70397 → 36754 | 117218 → 117266 | 165 → 135 | 22 → 3.2 | 84 | 42 |
| desktop · chromium · dark · bloomed | 99.6 → 99.8 | 9.1 → 9.7 | 41.7 → 25.9 | 0 | 40 | 88 → 68 | 58.4 → 48 | 0 | 70397 → 36754 | 117218 → 117266 | 165 → 135 | 0.3 → 0.2 | 84 | 0 |
| desktop · chromium · dark · mid | 99.6 → 99.8 | 9.2 → 9.5 | 25 → 33.3 | 0 | 48 → 32 | 120 → 68 | 58.7 → 51.6 | 0 | 70397 → 36754 | 117218 → 117266 | 165 → 135 | 46.8 → 2.7 | 84 | 34 |

#### search-1

| case | within16.7% | p95 ms | max ms | long | interact ms | LCP ms | hydr ms | cls | css gz | js gz | layers | idle GPU %core | filtered | anim filt |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| phone · chromium · light · bare | 69.8 → 98.1 | 32.9 → 9.2 | 249.7 → 258.2 | 10 → 8 | 376 → 288 | 1872 → 1932 | 1307.7 → 1163.5 | 0 | 70397 → 36754 | 117218 → 117266 | 141 → 113 | 561.5 → 4.7 | 100 | 36 |
| phone · chromium · light · bloomed | 91.3 → 97.6 | 24.9 → 9.2 | 258.9 → 183.6 | 8 → 3 | 280 → 264 | 1436 → 1800 | 1309.6 → 1157.6 | 0 | 70397 → 36754 | 117218 → 117266 | 141 → 113 | 0.1 → 0.2 | 100 | 0 |
| phone · chromium · light · mid | 50.2 → 98.1 | 42 → 9.3 | 258.3 → 225.2 | 15 → 8 | 424 → 256 | 1444 → 1824 | 1300.5 → 1167.7 | 0 | 70397 → 36754 | 117218 → 117266 | 141 → 113 | 1429.7 → 9 | 100 | 30 |
| phone · chromium · dark · bare | 66.8 → 98.5 | 33.3 → 9.3 | 250.1 → 258.4 | 9 → 8 | 392 → 296 | 1480 → 1824 | 1311.3 → 1163.4 | 0 | 70397 → 36754 | 117218 → 117266 | 141 → 113 | 546.7 → 4.4 | 100 | 36 |
| phone · chromium · dark · bloomed | 89.9 → 98 | 25.7 → 9.3 | 257.9 → 200 | 8 → 3 | 264 → 248 | 1428 → 1792 | 1306.1 → 1157.2 | 0 | 70397 → 36754 | 117218 → 117266 | 141 → 113 | 0.2 → 0.1 | 100 | 0 |
| phone · chromium · dark · mid | 40.4 → 98 | 50.3 → 9.3 | 266.2 → 258.3 | 15 → 5 | 408 → 288 | 1464 → 1816 | 1310 → 1157.4 | 0 | 70397 → 36754 | 117218 → 117266 | 141 → 113 | 1358.4 → 8.5 | 100 | 30 |
| phone · chromium · light · bloomed · images off | 92.6 → 98 | 24.1 → 9.3 | 242.3 → 199.7 | 8 → 3 | 368 → 264 | 1316 → 1736 | 1215.7 → 1030.4 | 0 | 70397 → 36754 | 117218 → 117266 | 144 → 113 | 0.2 → 0.1 | 103 | 0 |
| phone · chromium · light · bloomed · motion reduced | 89.8 → 99.2 | 24.9 → 9.2 | 232.7 → 50.9 | 3 → 1 | 152 → 128 | 1436 → 1808 | 1306.6 → 1158.5 | 0 | 70397 → 36754 | 117218 → 117266 | 144 → 113 | 0.3 → 0.1 | 103 | 0 |
| phone · webkit · light · bare | 38.2 → 42.1 | 43 → 38 | 226 → 189 | 9 → 10 | 264 → 216 | - | 88 → 95 | - | 70397 → 36754 | 117218 → 117266 | - | - | 100 | 36 |
| phone · webkit · light · bloomed | 37.1 → 43 | 18 → 19 | 5444 → 2347 | 12 → 11 | 5360 → 2256 | - | 81 → 82 | - | 70397 → 36754 | 117218 → 117266 | - | - | 100 | 0 |
| phone · webkit · dark · bare | 36.5 → 43.5 | 41 → 38 | 218 → 191 | 10 | 264 → 216 | - | 79 → 78 | - | 70397 → 36754 | 117218 → 117266 | - | - | 100 | 36 |
| phone · webkit · dark · bloomed | 35.1 → 42.9 | 18 → 19 | 5423 → 2347 | 12 → 11 | 5328 → 2264 | - | 86 | - | 70397 → 36754 | 117218 → 117266 | - | - | 100 | 0 |
| desktop · webkit · light · bare | 35.2 → 40.5 | 59 → 58 | 143 → 133 | 10 → 7 | 120 → 88 | - | 87 → 84 | - | 70397 → 36754 | 117218 → 117266 | - | - | 100 | 38 |
| desktop · webkit · light · bloomed | 34.2 → 43.3 | 327 → 319 | 598 → 528 | 9 → 13 | 512 → 440 | - | 86 → 85 | - | 70397 → 36754 | 117218 → 117266 | - | - | 100 | 0 |
| desktop · webkit · dark · bare | 34.4 → 40.4 | 58 → 59 | 148 → 135 | 8 | 104 → 88 | - | 89 → 86 | - | 70397 → 36754 | 117218 → 117266 | - | - | 100 | 38 |
| desktop · webkit · dark · bloomed | 36.9 → 38.1 | 328 → 320 | 595 → 518 | 9 → 13 | 512 → 424 | - | 87 → 84 | - | 70397 → 36754 | 117218 → 117266 | - | - | 100 | 0 |
| desktop · chromium · light · bare | 98.9 → 99.3 | 9.3 → 9.8 | 33.4 → 33.3 | 0 | 56 → 40 | 84 → 108 | 66.6 → 61.7 | 0 | 70397 → 36754 | 117218 → 117266 | 164 → 137 | 36.8 → 2.7 | 100 | 38 |
| desktop · chromium · light · bloomed | 99.3 → 100 | 8.5 → 9.5 | 33.2 → 16.6 | 0 | 48 → 40 | 92 → 112 | 62.7 → 56.4 | 0 | 70397 → 36754 | 117218 → 117266 | 164 → 137 | 0.2 | 100 | 0 |
| desktop · chromium · light · mid | 99.3 → 98.9 | 8.5 → 9.5 | 25.1 → 33.4 | 0 | 48 → 56 | 84 → 104 | 62.6 → 53.5 | 0 | 70397 → 36754 | 117218 → 117266 | 164 → 137 | 58.6 → 3.1 | 100 | 31 |
| desktop · chromium · dark · bare | 99.3 | 8.6 → 9.9 | 33.2 → 33.4 | 0 | 56 | 88 → 96 | 63.1 → 54.7 | 0 | 70397 → 36754 | 117218 → 117266 | 164 → 137 | 33 → 2.5 | 100 | 38 |
| desktop · chromium · dark · bloomed | 98.9 → 99.3 | 8.6 → 9.6 | 33.4 → 33.3 | 0 | 48 → 56 | 84 → 112 | 62.2 → 53.3 | 0 | 70397 → 36754 | 117218 → 117266 | 164 → 137 | 0.3 | 100 | 0 |
| desktop · chromium · dark · mid | 99.3 → 99.6 | 8.5 → 9.5 | 41.7 → 33.3 | 0 | 56 | 80 → 104 | 65.7 → 56.6 | 0 | 70397 → 36754 | 117218 → 117266 | 164 → 137 | 56.4 → 3.2 | 100 | 31 |

#### search-3

| case | within16.7% | p95 ms | max ms | long | interact ms | LCP ms | hydr ms | cls | css gz | js gz | layers | idle GPU %core | filtered | anim filt |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| phone · chromium · light · bare | 76.5 → 99.2 | 25.5 → 9.2 | 258.4 → 249.1 | 10 → 7 | 504 → 280 | 1452 → 1812 | 1306.1 → 1163.3 | 0 | 70397 → 36754 | 117218 → 117266 | 312 → 261 | 757 → 6.1 | 225 | 69 |
| phone · chromium · light · bloomed | 90.3 → 99.2 | 24.7 → 9.2 | 257.9 → 216.2 | 8 → 4 | 456 → 272 | 1428 → 1812 | 1304.8 → 1165.1 | 0 | 70397 → 36754 | 117218 → 117266 | 312 → 261 | 0.2 → 0.1 | 225 | 0 |
| phone · chromium · light · mid | 79.9 → 98.9 | 33.5 → 9.3 | 699.9 → 250 | 22 → 7 | 1040 → 272 | 1456 → 1820 | 1307.5 → 1160.1 | 0 | 70397 → 36754 | 117218 → 117266 | 312 → 261 | 1397.9 → 9.7 | 225 | 57 |
| phone · chromium · dark · bare | 74.9 → 99.2 | 25.6 → 9.2 | 250.1 → 258.2 | 12 → 5 | 448 → 280 | 1436 → 1808 | 1293.1 → 1160.6 | 0 | 70397 → 36754 | 117218 → 117266 | 312 → 261 | 744.8 → 6.8 | 225 | 69 |
| phone · chromium · dark · bloomed | 93.4 → 99.2 | 17.6 → 9.2 | 251 → 217.4 | 8 → 4 | 392 → 272 | 1420 → 1816 | 1299.7 → 1168.8 | 0 | 70397 → 36754 | 117218 → 117266 | 312 → 261 | 0.2 → 0.1 | 225 | 0 |
| phone · chromium · dark · mid | 80.6 → 99.2 | 33.2 → 9.3 | 266.6 → 257.4 | 17 → 6 | 840 → 280 | 1444 → 1828 | 1296.3 → 1165.1 | 0 | 70397 → 36754 | 117218 → 117266 | 312 → 261 | 1454 → 9.3 | 225 | 57 → 58 |
| phone · chromium · light · bloomed · images off | 90.2 → 99.2 | 25 → 9.2 | 259 → 224.8 | 10 → 4 | 456 → 264 | 1332 → 1728 | 1236.2 → 1026.9 | 0 | 70397 → 36754 | 117218 → 117266 | 315 → 261 | 0.1 → 0.2 | 228 | 0 |
| phone · chromium · light · bloomed · motion reduced | 97 → 99.7 | 9.3 → 9.2 | 92.4 → 34 | 6 → 0 | 144 → 104 | 1444 → 1800 | 1323.4 → 1160 | 0 | 70397 → 36754 | 117218 → 117266 | 144 → 113 | 0.1 → 0.2 | 228 | 0 |
| phone · webkit · light · bare | 37.8 → 42.8 | 42 → 38 | 238 → 241 | 16 → 14 | 248 → 224 | - | 111 → 109 | - | 70397 → 36754 | 117218 → 117266 | - | - | 225 | 69 |
| phone · webkit · light · bloomed | 38.5 → 42.5 | 18 → 19 | 15106 → 3618 | 28 | 12272 → 2240 | - | 149 → 109 | - | 70397 → 36754 | 117218 → 117266 | - | - | 225 | 0 |
| phone · webkit · dark · bare | 38.3 → 43 | 40 → 38 | 224 → 245 | 16 → 14 | 240 → 216 | - | 111 | - | 70397 → 36754 | 117218 → 117266 | - | - | 225 | 69 |
| phone · webkit · dark · bloomed | 36 → 42.6 | 18 → 19 | 17518 → 3639 | 28 | 13640 → 2512 | - | 84 → 136 | - | 70397 → 36754 | 117218 → 117266 | - | - | 225 | 0 |
| desktop · webkit · light · bare | 35 → 44.4 | 31 → 42 | 148 → 141 | 8 → 10 | 184 → 168 | - | 116 → 108 | - | 70397 → 36754 | 117218 → 117266 | - | - | 225 | 71 |
| desktop · webkit · light · bloomed | 37.1 → 41.3 | 68 → 249 | 690 → 542 | 20 → 30 | 600 → 448 | - | 89 → 112 | - | 70397 → 36754 | 117218 → 117266 | - | - | 225 | 0 |
| desktop · webkit · dark · bare | 36.3 → 41.8 | 31 → 41 | 147 → 139 | 8 | 184 → 168 | - | 120 → 112 | - | 70397 → 36754 | 117218 → 117266 | - | - | 225 | 71 |
| desktop · webkit · dark · bloomed | 36.3 → 39.7 | 68 → 183 | 702 → 528 | 20 → 29 | 624 → 440 | - | 114 → 115 | - | 70397 → 36754 | 117218 → 117266 | - | - | 225 | 0 |
| desktop · chromium · light · bare | 99 → 98.6 | 8.6 → 10 | 33.3 → 50 | 0 | 56 → 64 | 88 → 120 | 63.2 → 54.2 | 0 | 70397 → 36754 | 117218 → 117266 | 358 → 308 | 29.6 → 3 | 225 | 71 |
| desktop · chromium · light · bloomed | 99 | 9 → 9.9 | 33.4 → 49.9 | 0 | 56 → 72 | 92 → 112 | 61.3 → 51.6 | 0 | 70397 → 36754 | 117218 → 117266 | 358 → 308 | 0.2 → 0.4 | 225 | 0 |
| desktop · chromium · light · mid | 98.5 → 98.8 | 9.1 → 9.5 | 25.7 → 41.7 | 0 | 64 | 80 → 96 | 60.6 → 53.9 | 0 | 70397 → 36754 | 117218 → 117266 | 358 → 308 | 60.9 → 3 | 225 | 59 |
| desktop · chromium · dark · bare | 99.2 → 98.3 | 8.9 → 9.8 | 33.3 → 50 | 0 | 64 | 92 → 120 | 60.9 → 53.5 | 0 | 70397 → 36754 | 117218 → 117266 | 358 → 308 | 35.3 → 3 | 225 | 71 |
| desktop · chromium · dark · bloomed | 99.2 → 99 | 9 → 9.8 | 33.4 → 41.6 | 0 | 64 | 88 → 116 | 61.9 → 55.9 | 0 | 70397 → 36754 | 117218 → 117266 | 358 → 308 | 0.3 → 0.4 | 225 | 0 |
| desktop · chromium · dark · mid | 98.8 → 98.6 | 8.7 → 9.6 | 25.9 → 41.6 | 0 | 56 → 64 | 80 → 100 | 61.8 → 53.6 | 0 | 70397 → 36754 | 117218 → 117266 | 358 → 308 | 59.3 → 3.3 | 225 | 59 |

#### recipe

| case | within16.7% | p95 ms | max ms | long | interact ms | LCP ms | hydr ms | cls | css gz | js gz | layers | idle GPU %core | filtered | anim filt |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| phone · chromium · light · bare | 74.2 → 96.3 | 41.6 → 15.8 | 259.1 → 216.6 | 9 → 3 | 416 → 240 | 1496 → 1232 | 1273.1 → 1153.2 | 0 | 70397 → 36754 | 117218 → 117266 | 70 → 52 | 546.3 → 4.9 | 43 | 23 |
| phone · chromium · light · bloomed | 83.7 → 95.6 | 34.5 → 16.6 | 232.9 → 166.1 | 7 → 2 | 336 → 208 | 1492 → 1312 | 1270.9 → 1151.2 | 0 | 70397 → 36754 | 117218 → 117266 | 70 → 52 | 0.2 | 43 | 0 |
| phone · chromium · light · mid | 27.5 → 95.1 | 250.3 → 16.6 | 275.1 → 199.9 | 12 → 3 | 456 → 216 | 1436 → 1168 | 1271.8 → 1150.9 | 0 | 70397 → 36754 | 117218 → 117266 | 70 → 52 | 1481 → 12.1 | 43 | 20 |
| phone · chromium · dark · bare | 77 → 95.6 | 34.1 → 9.4 | 259.1 → 208.5 | 9 → 3 | 416 → 232 | 1436 → 1172 | 1273.9 → 1156.1 | 0 | 70397 → 36754 | 117218 → 117266 | 70 → 52 | 528.3 → 5.3 | 43 | 23 |
| phone · chromium · dark · bloomed | 84 → 94.5 | 34.8 → 16.8 | 233.1 → 166.7 | 7 → 2 | 352 → 216 | 1484 → 1228 | 1272.1 → 1150.2 | 0 | 70397 → 36754 | 117218 → 117266 | 70 → 52 | 0.2 → 0.1 | 43 | 0 |
| phone · chromium · dark · mid | 27.6 → 94.8 | 249.9 → 16.7 | 274.9 → 199.1 | 12 → 2 | 440 → 216 | 1432 → 1168 | 1274.1 → 1154.2 | 0 | 70397 → 36754 | 117218 → 117266 | 70 → 52 | 1482.2 → 12.7 | 43 | 20 |
| phone · chromium · light · bloomed · images off | 83.1 → 95.2 | 41.5 → 16.7 | 233.3 → 159.3 | 7 → 2 | 384 → 200 | 1440 → 1144 | 1203.4 → 1013.7 | 0 | 70397 → 36754 | 117218 → 117266 | 73 → 52 | 0.1 | 46 | 0 |
| phone · chromium · light · bloomed · motion reduced | 95.8 → 98.3 | 15.8 → 9.2 | 74.9 → 34.2 | 4 → 0 | 128 → 64 | 1520 → 1220 | 1285.4 → 1148.6 | 0 | 70397 → 36754 | 117218 → 117266 | 73 → 52 | 0.1 → 0.2 | 46 | 0 |
| phone · webkit · light · bare | 36.8 → 45.2 | 18 → 19 | 277 → 230 | 4 → 3 | 200 → 144 | - | 107 → 84 | - | 70397 → 36754 | 117218 → 117266 | - | - | 43 | 23 |
| phone · webkit · light · bloomed | 32.9 → 43.6 | 18 → 19 | 1733 → 1012 | 5 → 6 | 1616 → 960 | - | 102 → 95 | - | 70397 → 36754 | 117218 → 117266 | - | - | 43 | 0 |
| phone · webkit · dark · bare | 37.3 → 44.8 | 17 → 19 | 281 → 224 | 4 | 200 → 136 | - | 73 → 95 | - | 70397 → 36754 | 117218 → 117266 | - | - | 43 | 23 |
| phone · webkit · dark · bloomed | 34.9 → 47.3 | 18 → 19 | 1667 → 1009 | 5 → 6 | 1560 → 960 | - | 98 → 94 | - | 70397 → 36754 | 117218 → 117266 | - | - | 43 | 0 |
| desktop · webkit · light · bare | 35.8 → 43.3 | 87 → 45 | 136 → 115 | 7 → 4 | 144 → 112 | - | 72 → 93 | - | 70397 → 36754 | 117218 → 117266 | - | - | 43 | 23 |
| desktop · webkit · light · bloomed | 36 → 43.4 | 359 → 293 | 904 → 841 | 4 | 808 → 744 | - | 103 → 64 | - | 70397 → 36754 | 117218 → 117266 | - | - | 43 | 0 |
| desktop · webkit · dark · bare | 38.6 → 44.4 | 88 → 49 | 130 → 115 | 7 → 4 | 160 → 128 | - | 72 → 97 | - | 70397 → 36754 | 117218 → 117266 | - | - | 43 | 23 |
| desktop · webkit · dark · bloomed | 34.2 → 49.4 | 348 → 295 | 905 → 850 | 4 | 808 → 752 | - | 97 → 64 | - | 70397 → 36754 | 117218 → 117266 | - | - | 43 | 0 |
| desktop · chromium · light · bare | 99.5 → 97.3 | 8.5 → 10.3 | 33 → 41.7 | 0 | 40 | 272 → 88 | 62.4 → 52.6 | 0 | 70397 → 36754 | 117218 → 117266 | 78 → 58 | 10.9 → 2.6 | 43 | 23 |
| desktop · chromium · light · bloomed | 99 → 98.9 | 8.8 → 10 | 16.7 → 26.5 | 0 | 40 | 244 → 212 | 52.3 → 45.6 | 0 | 70397 → 36754 | 117218 → 117266 | 78 → 58 | 0.2 → 0.3 | 43 | 0 |
| desktop · chromium · light · mid | 99 → 97.9 | 8.9 → 9.9 | 33.3 → 41.7 | 0 | 48 → 40 | 76 → 88 | 52.1 → 44.7 | 0 | 70397 → 36754 | 117218 → 117266 | 78 → 58 | 51.2 → 2.8 | 43 | 20 |
| desktop · chromium · dark · bare | 99 → 96.8 | 9.1 → 9.9 | 33.3 → 41.7 | 0 | 48 → 40 | 80 → 84 | 52.2 → 45.2 | 0 | 70397 → 36754 | 117218 → 117266 | 78 → 58 | 12.9 → 2.6 | 43 | 23 |
| desktop · chromium · dark · bloomed | 99 → 98.4 | 9.1 → 10.2 | 24.9 → 25 | 0 | 40 | 80 → 92 | 51.5 → 44.4 | 0 | 70397 → 36754 | 117218 → 117266 | 78 → 58 | 0.1 → 0.2 | 43 | 0 |
| desktop · chromium · dark · mid | 98.4 | 9.2 → 10.1 | 33.3 → 41.7 | 0 | 48 | 84 → 88 | 59.3 → 45.6 | 0 | 70397 → 36754 | 117218 → 117266 | 78 → 58 | 49.2 → 2.8 | 43 | 20 |

#### variant-cook

| case | within16.7% | p95 ms | max ms | long | interact ms | LCP ms | hydr ms | cls | css gz | js gz | layers | idle GPU %core | filtered | anim filt |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| phone · chromium · light · bare | 65.7 → 97.9 | 33 → 9.3 | 250.5 → 208.3 | 10 → 5 | 480 → 240 | 3556 → 1892 | 1330.1 → 1175 | 0 | 70397 → 36754 | 117218 → 117266 | 106 → 82 | 714 → 4.8 | 61 | 35 |
| phone · chromium · light · bloomed | 91.6 → 97.7 | 25 → 9.3 | 200.8 → 158.4 | 4 → 1 | 200 | 1800 → 1960 | 1321.1 → 1172.8 | 0 | 70397 → 36754 | 117218 → 117266 | 91 → 70 | 0 → 0.2 | 61 | 0 |
| phone · chromium · dark · bare | 63.2 → 97.4 | 32.7 → 9.2 | 249.2 → 208.3 | 9 → 5 | 464 → 248 | 1568 → 1876 | 1339.3 → 1175.7 | 0 | 70397 → 36754 | 117218 → 117266 | 106 → 82 | 510.8 → 5.4 | 61 | 35 |
| phone · chromium · dark · bloomed | 90.6 → 97.7 | 25.1 → 9.3 | 233.2 → 158.8 | 5 → 1 | 288 → 208 | 1564 → 1868 | 1317.1 → 1177.3 | 0 | 70397 → 36754 | 117218 → 117266 | 91 → 70 | 0.1 | 61 | 0 |
| phone · chromium · light · bloomed · images off | 91.2 → 98.3 | 25 → 9.2 | 200.9 → 158.5 | 7 → 2 | 200 → 224 | 1448 → 2032 | 1242.4 → 1041.2 | 0 | 70397 → 36754 | 117218 → 117266 | 91 → 78 | 0.1 → 0.2 | 61 → 69 | 0 |
| phone · chromium · light · bloomed · motion reduced | 98.7 → 99.6 | 9.3 → 9.2 | 66.7 → 33.3 | 1 → 0 | 0 → 16 | 1512 → 2108 | 1330.2 → 1175 | 0 | 70397 → 36754 | 117218 → 117266 | 91 → 78 | 0.1 | 61 → 69 | 0 |
| phone · webkit · light · bare | 36.8 → 43.9 | 18 → 19 | 462 → 377 | 6 | 216 → 152 | - | 80 → 97 | - | 70397 → 36754 | 117218 → 117266 | - | - | 61 | 35 |
| phone · webkit · light · bloomed | 34.1 → 46.6 | 17 → 19 | 3211 → 1628 | 5 | 3104 → 1528 | - | 90 → 83 | - | 70397 → 36754 | 117218 → 117266 | - | - | 61 | 0 |
| phone · webkit · dark · bare | 37 → 44.5 | 18 → 19 | 468 → 377 | 5 | 232 → 144 | - | 87 → 81 | - | 70397 → 36754 | 117218 → 117266 | - | - | 61 | 35 |
| phone · webkit · dark · bloomed | 35.2 → 39.7 | 17 → 19 | 3192 → 1640 | 5 | 3088 → 1536 | - | 90 → 78 | - | 70397 → 36754 | 117218 → 117266 | - | - | 61 | 0 |
| desktop · webkit · light · bare | 35.8 → 41.6 | 18 | 341 → 289 | 5 | 16 | - | 86 → 78 | - | 70397 → 36754 | 117218 → 117266 | - | - | 66 | 38 |
| desktop · webkit · light · bloomed | 35.1 → 38.8 | 18 | 3067 → 2788 | 4 | 1072 → 992 | - | 79 → 81 | - | 70397 → 36754 | 117218 → 117266 | - | - | 66 | 0 |
| desktop · webkit · dark · bare | 37.4 → 37.8 | 18 | 364 → 300 | 5 | 16 | - | 91 → 80 | - | 70397 → 36754 | 117218 → 117266 | - | - | 66 | 38 |
| desktop · webkit · dark · bloomed | 36.2 → 39.1 | 19 → 18 | 2977 → 2807 | 4 | 1072 → 992 | - | 89 → 81 | - | 70397 → 36754 | 117218 → 117266 | - | - | 66 | 0 |
| desktop · chromium · light · bare | 98.6 | 9.3 → 10 | 24.5 → 42.8 | 0 | 48 | 112 → 96 | 64 → 58 | 0 | 70397 → 36754 | 117218 → 117266 | 122 → 97 | 15.3 → 2.7 | 66 | 38 |
| desktop · chromium · light · bloomed | 99.7 → 99.3 | 9.1 → 9.8 | 25 → 33.1 | 0 | 48 → 40 | 96 → 112 | 65.3 → 51.7 | 0 | 70397 → 36754 | 117218 → 117266 | 97 → 75 | 0.3 | 66 | 0 |
| desktop · chromium · dark · bare | 99.7 → 98.3 | 8.9 → 9.7 | 25.1 → 33.3 | 0 | 48 → 40 | 120 → 112 | 58.9 → 50.8 | 0 | 70397 → 36754 | 117218 → 117266 | 122 → 97 | 16.5 → 2.6 | 66 | 38 |
| desktop · chromium · dark · bloomed | 99.3 | 8.6 → 9.9 | 25 → 33.3 | 0 | 48 → 40 | 84 → 104 | 65.6 → 53.7 | 0 | 70397 → 36754 | 117218 → 117266 | 97 → 75 | 0.1 → 0.3 | 66 | 0 |

#### create-recipe

| case | within16.7% | p95 ms | max ms | long | interact ms | LCP ms | hydr ms | cls | css gz | js gz | layers | idle GPU %core | filtered | anim filt |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| phone · chromium · light · bare | 35.5 → 100 | 25 → 9.3 | 25.7 → 9.3 | 0 | 16 → 0 | 1424 → 1116 | 1261.3 → 1145 | 0 | 70397 → 36754 | 117218 → 117266 | 47 → 30 | 1510.8 → 12.3 | 29 | 18 |
| phone · chromium · light · bloomed | 100 | 9.3 → 9.2 | 9.4 | 0 | 0 | 1432 → 1160 | 1263.4 → 1152.2 | 0 | 70397 → 36754 | 117218 → 117266 | 47 → 30 | 0.1 → 0.2 | 29 | 0 |
| phone · chromium · dark · bare | 41.9 → 100 | 25.4 → 9.2 | 25.8 → 9.4 | 0 | 16 | 1448 → 1124 | 1267 → 1142.1 | 0 | 70397 → 36754 | 117218 → 117266 | 47 → 30 | 1463.5 → 11.7 | 29 | 18 |
| phone · chromium · dark · bloomed | 100 | 9.3 → 9.1 | 9.3 | 0 | 0 | 1436 → 1148 | 1267.1 → 1137.6 | 0 | 70397 → 36754 | 117218 → 117266 | 47 → 30 | 0.1 | 29 | 0 |
| phone · chromium · light · bloomed · images off | 100 | 9.2 | 9.3 | 0 | 0 | 1360 → 1060 | 1174.9 → 1013.4 | 0 | 70397 → 36754 | 117218 → 117266 | 47 → 30 | 0.1 | 29 | 0 |
| phone · chromium · light · bloomed · motion reduced | 100 | 9.3 → 9 | 9.3 → 9.4 | 0 | 0 | 1444 → 1152 | 1263.6 → 1141.7 | 0 | 70397 → 36754 | 117218 → 117266 | 47 → 30 | 0.1 → 0.2 | 29 | 0 |
| phone · webkit · light · bare | 32.4 → 38.5 | 17 | 17 → 18 | 0 | 0 | - | 92 → 97 | - | 70397 → 36754 | 117218 → 117266 | - | - | 29 | 18 |
| phone · webkit · light · bloomed | 35.3 → 54.3 | 18 | 18 | 0 | 0 | - | 88 → 72 | - | 70397 → 36754 | 117218 → 117266 | - | - | 29 | 0 |
| phone · webkit · dark · bare | 41.2 → 38.2 | 18 | 18 | 0 | 0 | - | 76 → 82 | - | 70397 → 36754 | 117218 → 117266 | - | - | 29 | 18 |
| phone · webkit · dark · bloomed | 38.2 → 44.1 | 18 | 18 | 0 | 0 | - | 87 → 83 | - | 70397 → 36754 | 117218 → 117266 | - | - | 29 | 0 |
| desktop · webkit · light · bare | 38.2 → 44.1 | 17 → 18 | 18 | 0 | 0 | - | 76 → 71 | - | 70397 → 36754 | 117218 → 117266 | - | - | 29 | 18 |
| desktop · webkit · light · bloomed | 32.4 → 35.3 | 17 → 18 | 17 → 18 | 0 | 0 | - | 90 → 74 | - | 70397 → 36754 | 117218 → 117266 | - | - | 29 | 0 |
| desktop · webkit · dark · bare | 34.5 → 32.4 | 17 | 17 → 18 | 0 | 0 | - | 52 → 61 | - | 70397 → 36754 | 117218 → 117266 | - | - | 29 | 18 |
| desktop · webkit · dark · bloomed | 35.3 → 41.2 | 17 → 18 | 17 → 18 | 0 | 0 | - | 78 → 84 | - | 70397 → 36754 | 117218 → 117266 | - | - | 29 | 0 |
| desktop · chromium · light · bare | 100 | 8.4 → 9.7 | 8.6 → 10.3 | 0 | 0 | 64 | 51.4 → 46.2 | 0 | 70397 → 36754 | 117218 → 117266 | 48 → 31 | 27.7 → 3.1 | 29 | 18 |
| desktop · chromium · light · bloomed | 100 | 9.2 → 10 | 9.4 → 10.3 | 0 | 0 | 76 → 60 | 49 → 42.3 | 0 | 70397 → 36754 | 117218 → 117266 | 48 → 31 | 0.1 → 0.2 | 29 | 0 |
| desktop · chromium · dark · bare | 100 | 9 → 9.9 | 9.3 → 10.4 | 0 | 0 | 68 → 64 | 48.7 → 43.9 | 0 | 70397 → 36754 | 117218 → 117266 | 48 → 31 | 28.7 → 3 | 29 | 18 |
| desktop · chromium · dark · bloomed | 100 | 8.4 → 10 | 9.2 → 10.3 | 0 | 0 | 76 → 64 | 48.1 → 43.3 | 0 | 70397 → 36754 | 117218 → 117266 | 48 → 31 | 0.2 | 29 | 0 |

#### add-variant

| case | within16.7% | p95 ms | max ms | long | interact ms | LCP ms | hydr ms | cls | css gz | js gz | layers | idle GPU %core | filtered | anim filt |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| phone · chromium · light · bare | 18.6 → 100 | 25.6 → 9.3 | 26 → 9.3 | 0 | 288 → 224 | 600 → 564 | 345.4 → 321.4 | 0 | 70397 → 36754 | 117218 → 117266 | 47 → 30 | 1524.2 → 16.3 | 29 | 18 |
| phone · chromium · light · bloomed | 99 → 100 | 9.2 | 16.7 | 0 | 0 → 264 | 608 | 340.4 → 314.2 | 0 | 70397 → 36754 | 117218 → 117266 | 47 → 30 | 0.2 | 29 | 0 |
| phone · chromium · dark · bare | 30.2 → 100 | 25.6 → 9.1 | 25.8 → 9.3 | 0 | 272 → 32 | 576 → 496 | 347.3 → 318.6 | 0 | 70397 → 36754 | 117218 → 117266 | 47 → 30 | 1518.1 → 15.9 | 29 | 18 |
| phone · chromium · dark · bloomed | 100 | 9.2 | 9.4 → 16.1 | 0 | 0 → 288 | 600 → 580 | 340.6 → 322.9 | 0 | 70397 → 36754 | 117218 → 117266 | 47 → 30 | 0.2 | 29 | 0 |
| phone · chromium · light · bloomed · images off | 100 | 9.2 | 9.4 → 16.3 | 0 | 0 | 1396 → 1088 | 1177.6 → 998.3 | 0 | 70397 → 36754 | 117218 → 117266 | 47 → 30 | 0.2 | 29 | 0 |
| phone · chromium · light · bloomed · motion reduced | 100 | 9.2 | 15.8 → 16.1 | 0 | 376 → 280 | 672 → 572 | 347.1 → 323.3 | 0 | 70397 → 36754 | 117218 → 117266 | 47 → 30 | 0.2 | 29 | 0 |
| phone · webkit · light · bare | 37 → 35.2 | 17 | 18 | 0 | 0 | - | 42 → 49 | - | 70397 → 36754 | 117218 → 117266 | - | - | 29 | 18 |
| phone · webkit · light · bloomed | 37.7 → 40.7 | 17 → 18 | 18 | 0 | 0 | - | 45 → 38 | - | 70397 → 36754 | 117218 → 117266 | - | - | 29 | 0 |
| phone · webkit · dark · bare | 33.3 → 40.7 | 17 → 18 | 17 → 18 | 0 | 0 → 16 | - | 40 → 39 | - | 70397 → 36754 | 117218 → 117266 | - | - | 29 | 18 |
| phone · webkit · dark · bloomed | 35.2 → 40.7 | 18 | 18 | 0 | 0 | - | 41 → 38 | - | 70397 → 36754 | 117218 → 117266 | - | - | 29 | 0 |
| desktop · webkit · light · bare | 35.3 → 44.1 | 17 → 18 | 18 | 0 | 0 → 16 | - | 41 → 39 | - | 70397 → 36754 | 117218 → 117266 | - | - | 29 | 18 |
| desktop · webkit · light · bloomed | 32.4 → 38.2 | 17 → 18 | 17 → 18 | 0 | 0 | - | 43 → 40 | - | 70397 → 36754 | 117218 → 117266 | - | - | 29 | 0 |
| desktop · webkit · dark · bare | 35.3 → 41.2 | 18 | 18 | 0 | 0 | - | 39 → 38 | - | 70397 → 36754 | 117218 → 117266 | - | - | 29 | 18 |
| desktop · webkit · dark · bloomed | 33.3 → 44.1 | 17 → 18 | 17 → 18 | 0 | 0 | - | 41 → 36 | - | 70397 → 36754 | 117218 → 117266 | - | - | 29 | 0 |
| desktop · chromium · light · bare | 100 | 8.8 → 10.3 | 9.1 → 10.4 | 0 | 56 → 40 | 152 → 124 | 65.1 → 59.1 | 0 | 70397 → 36754 | 117218 → 117266 | 48 → 31 | 22.1 → 3.1 | 29 | 18 |
| desktop · chromium · light · bloomed | 100 | 8.6 → 9.9 | 9.2 → 10.4 | 0 | 48 → 40 | 136 → 124 | 55.7 → 61.6 | 0 | 70397 → 36754 | 117218 → 117266 | 48 → 31 | 0.1 → 0.4 | 29 | 0 |
| desktop · chromium · dark · bare | 100 | 8.4 → 9.6 | 9.1 → 9.9 | 0 | 40 | 136 → 124 | 59.5 → 57.1 | 0 | 70397 → 36754 | 117218 → 117266 | 48 → 31 | 21.6 → 3 | 29 | 18 |
| desktop · chromium · dark · bloomed | 100 | 8.8 → 10.2 | 9.2 → 10.4 | 0 | 48 → 40 | 144 → 124 | 58.9 → 55.6 | 0 | 70397 → 36754 | 117218 → 117266 | 48 → 31 | 0.2 | 29 | 0 |

#### account

| case | within16.7% | p95 ms | max ms | long | interact ms | LCP ms | hydr ms | cls | css gz | js gz | layers | idle GPU %core | filtered | anim filt |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| phone · chromium · light · bare | 69.6 → 98.9 | 24.9 → 9.2 | 25.1 → 24.2 | 0 | 16 → 0 | 1396 → 1144 | 1261.7 → 1138.2 | 0 | 70397 → 36754 | 117218 → 117266 | 51 → 33 | 1388.9 → 9.5 | 29 | 16 |
| phone · chromium · light · bloomed | 98.8 | 9.3 | 25.6 → 17.5 | 0 | 0 | 1420 → 1136 | 1264.2 → 1143.7 | 0 | 70397 → 36754 | 117218 → 117266 | 51 → 33 | 0.1 | 29 | 0 |
| phone · chromium · dark · bare | 74.5 → 98.9 | 17.6 → 9.2 | 34.1 → 24.3 | 0 | 0 → 16 | 1392 → 1148 | 1262.5 → 1143.6 | 0 | 70397 → 36754 | 117218 → 117266 | 51 → 33 | 1263.1 → 9.2 | 29 | 16 |
| phone · chromium · dark · bloomed | 97.7 → 98.9 | 9.3 | 25.3 → 16.7 | 0 | 0 | 1416 → 1128 | 1268.5 → 1139.2 | 0 | 70397 → 36754 | 117218 → 117266 | 51 → 33 | 0.2 → 0.1 | 29 | 0 |
| phone · chromium · light · bloomed · images off | 97.7 | 9.3 | 17.3 → 16.8 | 0 | 0 | 1344 → 1052 | 1175.1 → 1003.2 | 0 | 70397 → 36754 | 117218 → 117266 | 51 → 33 | 0.1 | 29 | 0 |
| phone · chromium · light · bloomed · motion reduced | 97.7 → 98.9 | 9.3 | 17.1 | 0 | 0 | 1424 → 1144 | 1266.4 → 1143 | 0 | 70397 → 36754 | 117218 → 117266 | 51 → 33 | 0.2 → 0.1 | 29 | 0 |
| phone · webkit · light · bare | 36.4 | 17 → 18 | 18 | 0 | 0 | - | 47 → 93 | - | 70397 → 36754 | 117218 → 117266 | - | - | 29 | 16 |
| phone · webkit · light · bloomed | 38.6 → 40.9 | 17 → 18 | 18 | 0 | 0 | - | 78 → 72 | - | 70397 → 36754 | 117218 → 117266 | - | - | 29 | 0 |
| phone · webkit · dark · bare | 38.6 → 36.4 | 17 → 18 | 18 | 0 | 0 | - | 65 → 75 | - | 70397 → 36754 | 117218 → 117266 | - | - | 29 | 16 |
| phone · webkit · dark · bloomed | 36.4 → 40 | 18 | 18 | 0 | 0 | - | 70 → 57 | - | 70397 → 36754 | 117218 → 117266 | - | - | 29 | 0 |
| desktop · webkit · light · bare | 32.4 → 38.2 | 18 | 18 | 0 | 0 | - | 86 → 70 | - | 70397 → 36754 | 117218 → 117266 | - | - | 29 | 16 |
| desktop · webkit · light · bloomed | 38.2 → 44.1 | 18 | 18 | 0 | 0 | - | 75 → 83 | - | 70397 → 36754 | 117218 → 117266 | - | - | 29 | 0 |
| desktop · webkit · dark · bare | 38.2 → 50 | 18 | 18 | 0 | 0 | - | 64 → 82 | - | 70397 → 36754 | 117218 → 117266 | - | - | 29 | 16 |
| desktop · webkit · dark · bloomed | 41.2 → 42.9 | 18 | 18 | 0 | 0 | - | 87 → 45 | - | 70397 → 36754 | 117218 → 117266 | - | - | 29 | 0 |
| desktop · chromium · light · bare | 100 | 9.1 → 9.7 | 9.3 → 10.1 | 0 | 0 | 72 → 60 | 54.3 → 45.1 | 0 | 70397 → 36754 | 117218 → 117266 | 53 → 33 | 25.7 → 3 | 29 | 16 |
| desktop · chromium · light · bloomed | 100 | 8.5 → 9.9 | 9.4 → 10.2 | 0 | 0 | 92 → 60 | 64.8 → 42.7 | 0 | 70397 → 36754 | 117218 → 117266 | 53 → 33 | 0.1 → 0.2 | 29 | 0 |
| desktop · chromium · dark · bare | 100 | 8.4 → 9.8 | 9.3 → 9.9 | 0 | 0 | 64 | 47.8 → 41.3 | 0 | 70397 → 36754 | 117218 → 117266 | 53 → 33 | 23.5 → 2.8 | 29 | 16 |
| desktop · chromium · dark · bloomed | 100 | 9 → 9.6 | 9.2 → 10 | 0 | 0 | 72 → 56 | 47.9 → 41.4 | 0 | 70397 → 36754 | 117218 → 117266 | 53 → 33 | 0.1 → 0.2 | 29 | 0 |

#### sign-in

| case | within16.7% | p95 ms | max ms | long | interact ms | LCP ms | hydr ms | cls | css gz | js gz | layers | idle GPU %core | filtered | anim filt |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| phone · chromium · light · bare | 67 → 96 | 41.6 → 9.3 | 274.9 → 241.8 | 24 → 20 | 472 → 280 | 1720 → 1344 | 1480.4 → 1326.6 | 0 | 70397 → 36754 | 117218 → 117266 | 165 → 126 | 1249 → 16.9 | 87 | 44 |
| phone · chromium · light · bloomed | 93.9 → 96.7 | 24.9 → 9.3 | 258.5 → 241.7 | 22 → 19 | 328 → 264 | 1688 → 1384 | 1474.9 → 1326.4 | 0 | 70397 → 36754 | 117218 → 117266 | 165 → 126 | 0.2 → 0.1 | 87 | 0 |
| phone · chromium · dark · bare | 67.1 → 96.5 | 41.3 → 9.3 | 266.8 → 233.3 | 22 → 20 | 456 → 288 | 1712 → 1352 | 1477.1 → 1329.1 | 0 | 70397 → 36754 | 117218 → 117266 | 165 → 126 | 1204 → 14.8 | 87 | 44 |
| phone · chromium · dark · bloomed | 93.7 → 96.5 | 24.3 → 9.3 | 258.4 → 200 | 23 → 18 | 264 → 256 | 1680 → 1392 | 1470.1 → 1328.3 | 0 | 70397 → 36754 | 117218 → 117266 | 165 → 126 | 0.2 → 0.1 | 87 | 0 |
| phone · chromium · light · bloomed · images off | 93.2 → 96.4 | 24.2 → 9.3 | 257.6 → 201 | 22 → 19 | 264 | 1600 → 1308 | 1393.7 → 1201.6 | 0 | 70397 → 36754 | 117218 → 117266 | 165 → 126 | 0.2 → 0.1 | 87 | 0 |
| phone · chromium · light · bloomed · motion reduced | 93.6 → 97.5 | 16.9 → 9.2 | 342.1 → 116 | 18 → 8 | 296 → 16 | 1692 → 1392 | 1485.1 → 1329.3 | 0 | 70397 → 36754 | 117218 → 117266 | 165 → 126 | 0.1 | 87 | 0 |
| phone · webkit · light · bare | 38.1 → 40.4 | 80 → 68 | 422 → 295 | 27 | 336 → 200 | - | 94 → 87 | - | 70397 → 36754 | 117218 → 117266 | - | - | 87 | 44 |
| phone · webkit · light · bloomed | 36.7 → 38.4 | 846 → 280 | 5290 → 2289 | 34 → 33 | 5184 → 2200 | - | 94 → 88 | - | 70397 → 36754 | 117218 → 117266 | - | - | 87 | 0 |
| phone · webkit · dark · bare | 38 → 38.6 | 65 → 64 | 431 → 302 | 25 → 26 | 344 → 200 | - | 100 → 85 | - | 70397 → 36754 | 117218 → 117266 | - | - | 87 | 44 |
| phone · webkit · dark · bloomed | 36.8 → 38.5 | 855 → 275 | 5426 → 2300 | 34 → 33 | 5320 → 2216 | - | 91 → 87 | - | 70397 → 36754 | 117218 → 117266 | - | - | 87 | 0 |
| desktop · webkit · light · bare | 35.8 → 39.7 | 48 → 72 | 153 → 134 | 11 → 15 | 96 → 64 | - | 91 → 84 | - | 70397 → 36754 | 117218 → 117266 | - | - | 87 | 44 |
| desktop · webkit · light · bloomed | 36.7 → 38.2 | 94 → 76 | 1307 → 765 | 19 | 1208 → 504 | - | 91 → 76 | - | 70397 → 36754 | 117218 → 117266 | - | - | 87 | 0 |
| desktop · webkit · dark · bare | 34.5 → 38.7 | 45 → 51 | 166 → 128 | 12 → 13 | 80 → 64 | - | 98 → 86 | - | 70397 → 36754 | 117218 → 117266 | - | - | 87 | 44 |
| desktop · webkit · dark · bloomed | 36.9 → 40.6 | 98 → 82 | 1305 → 757 | 22 → 18 | 1216 → 504 | - | 95 → 87 | - | 70397 → 36754 | 117218 → 117266 | - | - | 87 | 0 |
| desktop · chromium · light · bare | 99.8 | 8.5 → 9.8 | 24.9 → 33.3 | 0 | 40 → 32 | 84 → 76 | 65 → 54.1 | 0 | 70397 → 36754 | 117218 → 117266 | 168 → 135 | 24.3 → 3.1 | 87 | 44 |
| desktop · chromium · light · bloomed | 99.8 | 8.6 → 9.9 | 25 → 26.1 | 0 | 40 | 84 → 112 | 61.3 → 52.1 | 0 | 70397 → 36754 | 117218 → 117266 | 168 → 135 | 0.2 | 87 | 0 |
| desktop · chromium · dark · bare | 99.8 → 99.6 | 9.1 → 9.8 | 50.5 → 33.3 | 1 → 0 | 40 → 32 | 80 → 68 | 64.1 → 51 | 0 | 70397 → 36754 | 117218 → 117266 | 168 → 135 | 23.5 → 3.2 | 87 | 44 |
| desktop · chromium · dark · bloomed | 100 → 99.8 | 8.8 → 9.8 | 16.7 | 0 | 40 → 32 | 112 → 72 | 84 → 50.3 | 0 | 70397 → 36754 | 117218 → 117266 | 168 → 135 | 0.1 → 0.2 | 87 | 0 |

#### settings

| case | within16.7% | p95 ms | max ms | long | interact ms | LCP ms | hydr ms | cls | css gz | js gz | layers | idle GPU %core | filtered | anim filt |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| phone · chromium · light · bare | 53.4 → 95 | 117.1 → 16.9 | 241.6 → 183.3 | 9 → 3 | 488 → 200 | 1456 → 1168 | 1272 → 1148.1 | 0 | 70397 → 36754 | 117218 → 117266 | 77 → 55 | 830.3 → 5 | 44 | 27 |
| phone · chromium · light · bloomed | 85.1 → 95.1 | 34.1 → 9.4 | 232.9 → 124.9 | 7 → 1 | 328 → 168 | 1536 → 1248 | 1269.9 → 1146.2 | 0 | 70397 → 36754 | 117218 → 117266 | 77 → 55 | 0.2 | 44 | 0 |
| phone · chromium · dark · bare | 49.2 → 95.4 | 125.6 → 16.4 | 250 → 166.7 | 9 → 3 | 504 → 184 | 1456 → 1168 | 1273.5 → 1146.7 | 0 | 70397 → 36754 | 117218 → 117266 | 77 → 55 | 862.1 → 4.6 | 44 | 27 |
| phone · chromium · dark · bloomed | 85.9 → 96 | 33.3 → 9.3 | 233.3 → 125.8 | 5 → 1 | 336 → 168 | 1528 → 1240 | 1271.5 → 1141.4 | 0 | 70397 → 36754 | 117218 → 117266 | 77 → 55 | 0.3 → 0.1 | 44 | 0 |
| phone · chromium · light · bloomed · images off | 86.9 → 95.1 | 34.3 → 9.3 | 233.1 → 125.4 | 5 → 1 | 328 → 168 | 1464 → 1168 | 1202 → 1018.4 | 0 | 70397 → 36754 | 117218 → 117266 | 77 → 55 | 0.1 → 0.2 | 44 | 0 |
| phone · chromium · light · bloomed · motion reduced | 90.1 → 98.8 | 25.5 → 9.2 | 100 → 34.3 | 4 → 0 | 168 → 56 | 1540 → 1260 | 1283.4 → 1151.7 | 0 | 70397 → 36754 | 117218 → 117266 | 77 → 55 | 0.2 | 44 | 0 |
| phone · webkit · light · bare | 36.6 → 42.6 | 17 → 18 | 282 → 209 | 4 | 200 → 128 | - | 98 → 101 | - | 70397 → 36754 | 117218 → 117266 | - | - | 44 | 27 |
| phone · webkit · light · bloomed | 34.7 → 38.8 | 17 → 18 | 1395 → 963 | 4 | 1296 → 864 | - | 68 → 92 | - | 70397 → 36754 | 117218 → 117266 | - | - | 44 | 0 |
| phone · webkit · dark · bare | 36.3 → 39.3 | 18 | 270 → 212 | 4 | 184 → 128 | - | 102 → 90 | - | 70397 → 36754 | 117218 → 117266 | - | - | 44 | 27 |
| phone · webkit · dark · bloomed | 35.5 → 37.2 | 17 → 18 | 1509 → 959 | 4 | 1416 → 872 | - | 97 → 92 | - | 70397 → 36754 | 117218 → 117266 | - | - | 44 | 0 |
| desktop · webkit · light · bare | 48.1 → 40 | 90 → 69 | 102 → 93 | 3 | 0 | - | 109 → 90 | - | 70397 → 36754 | 117218 → 117266 | - | - | 44 | 27 |
| desktop · webkit · light · bloomed | 35.3 → 37.3 | 715 → 536 | 889 → 668 | 4 | 0 | - | 99 → 91 | - | 70397 → 36754 | 117218 → 117266 | - | - | 44 | 0 |
| desktop · webkit · dark · bare | 37 → 41.8 | 97 → 70 | 109 → 93 | 3 | 0 | - | 105 → 89 | - | 70397 → 36754 | 117218 → 117266 | - | - | 44 | 27 |
| desktop · webkit · dark · bloomed | 33.3 → 40.4 | 643 → 545 | 832 → 660 | 4 | 0 | - | 71 → 89 | - | 70397 → 36754 | 117218 → 117266 | - | - | 44 | 0 |
| desktop · chromium · light · bare | 99.2 → 98.3 | 8.4 → 10.1 | 33.3 → 41.6 | 0 | 0 | 84 → 76 | 56.5 → 52.6 | 0 | 70397 → 36754 | 117218 → 117266 | 79 → 58 | 15 → 2.6 | 44 | 27 |
| desktop · chromium · light · bloomed | 98.3 | 9.1 → 9.6 | 25.3 | 0 | 0 | 80 → 72 | 52.5 → 47.4 | 0 | 70397 → 36754 | 117218 → 117266 | 79 → 58 | 0.2 | 44 | 0 |
| desktop · chromium · dark · bare | 98.3 | 8.8 → 10 | 33.3 → 41.7 | 0 | 0 | 84 → 112 | 54.1 → 47.6 | 0 | 70397 → 36754 | 117218 → 117266 | 79 → 58 | 14.6 → 2.6 | 44 | 27 |
| desktop · chromium · dark · bloomed | 98.4 → 99.2 | 9 → 10 | 33.4 → 33.3 | 0 | 0 | 84 → 76 | 52.3 → 51.5 | 0 | 70397 → 36754 | 117218 → 117266 | 79 → 58 | 0.3 → 0.2 | 44 | 0 |

#### error

| case | within16.7% | p95 ms | max ms | long | interact ms | LCP ms | hydr ms | cls | css gz | js gz | layers | idle GPU %core | filtered | anim filt |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| phone · chromium · light · bare | 77.1 → 100 | 17.3 → 9.1 | 17.4 → 9.4 | 0 | 0 → 16 | 1372 → 1064 | 1265.6 → 1150.6 | 0 | 70397 → 36754 | 117218 → 117266 | 37 → 21 | 975.2 → 4.7 | 23 | 14 |
| phone · chromium · light · bloomed | 100 | 9.2 → 9 | 9.3 | 0 | 0 | 1360 → 1060 | 1261.6 → 1149.5 | 0 | 70397 → 36754 | 117218 → 117266 | 37 → 21 | 0.2 → 0.3 | 23 | 0 |
| phone · chromium · dark · bare | 87 → 100 | 17 → 9.1 | 17.5 → 9.4 | 0 | 0 → 16 | 1348 → 1068 | 1257.7 → 1150.5 | 0 | 70397 → 36754 | 117218 → 117266 | 37 → 21 | 910.5 → 4.4 | 23 | 14 |
| phone · chromium · dark · bloomed | 100 | 9.2 | 9.3 | 0 | 0 | 1360 → 1060 | 1261.8 → 1146.3 | 0 | 70397 → 36754 | 117218 → 117266 | 37 → 21 | 0.2 → 0.1 | 23 | 0 |
| phone · chromium · light · bloomed · images off | 100 | 9.3 → 9.1 | 9.3 | 0 | 0 | 1276 → 984 | 1168.1 → 1009.1 | 0 | 70397 → 36754 | 117218 → 117266 | 37 → 21 | 0.1 | 23 | 0 |
| phone · chromium · light · bloomed · motion reduced | 100 | 9.2 | 9.3 → 9.4 | 0 | 0 | 1360 → 1068 | 1264.6 → 1151.3 | 0 | 70397 → 36754 | 117218 → 117266 | 37 → 21 | 0.1 | 23 | 0 |
| phone · webkit · light · bare | 35.3 → 45.7 | 17 → 19 | 18 → 19 | 0 | 0 | - | 86 → 81 | - | 70397 → 36754 | 117218 → 117266 | - | - | 23 | 14 |
| phone · webkit · light · bloomed | 35.3 → 38.2 | 17 → 19 | 18 → 19 | 0 | 0 | - | 73 | - | 70397 → 36754 | 117218 → 117266 | - | - | 23 | 0 |
| phone · webkit · dark · bare | 35.3 → 52.9 | 17 → 19 | 18 → 19 | 0 | 0 | - | 63 → 56 | - | 70397 → 36754 | 117218 → 117266 | - | - | 23 | 14 |
| phone · webkit · dark · bloomed | 44.1 | 18 | 18 | 0 | 0 | - | 84 → 76 | - | 70397 → 36754 | 117218 → 117266 | - | - | 23 | 0 |
| desktop · webkit · light · bare | 38.2 → 40 | 18 → 19 | 18 → 19 | 0 | 0 | - | 80 → 66 | - | 70397 → 36754 | 117218 → 117266 | - | - | 23 | 14 |
| desktop · webkit · light · bloomed | 35.3 → 44.1 | 18 → 19 | 18 → 19 | 0 | 0 | - | 61 → 67 | - | 70397 → 36754 | 117218 → 117266 | - | - | 23 | 0 |
| desktop · webkit · dark · bare | 35.3 | 17 → 18 | 17 → 19 | 0 | 0 | - | 83 → 66 | - | 70397 → 36754 | 117218 → 117266 | - | - | 23 | 14 |
| desktop · webkit · dark · bloomed | 38.2 → 42.9 | 18 → 19 | 18 → 19 | 0 | 0 | - | 73 → 68 | - | 70397 → 36754 | 117218 → 117266 | - | - | 23 | 0 |
| desktop · chromium · light · bare | 100 | 8.6 → 10.1 | 9.2 → 10.2 | 0 | 0 | 132 → 84 | 81.7 → 42.6 | 0 | 70397 → 36754 | 117218 → 117266 | 39 → 22 | 10 → 2.5 | 23 | 14 |
| desktop · chromium · light · bloomed | 100 | 8.8 → 9.6 | 8.8 → 10.2 | 0 | 0 | 68 → 60 | 44.1 → 38.7 | 0 | 70397 → 36754 | 117218 → 117266 | 39 → 22 | 0.1 → 0.2 | 23 | 0 |
| desktop · chromium · dark · bare | 100 | 8.8 → 9.7 | 9.2 → 10.3 | 0 | 0 | 60 → 56 | 44.1 → 38.8 | 0 | 70397 → 36754 | 117218 → 117266 | 39 → 22 | 9.4 → 2.3 | 23 | 14 |
| desktop · chromium · dark · bloomed | 100 | 8.6 → 9.9 | 9.2 → 10.3 | 0 | 0 | 64 → 56 | 44 → 38.4 | 0 | 70397 → 36754 | 117218 → 117266 | 39 → 22 | 0.1 → 0.2 | 23 | 0 |

#### status-404

| case | within16.7% | p95 ms | max ms | long | interact ms | LCP ms | hydr ms | cls | css gz | js gz | layers | idle GPU %core | filtered | anim filt |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| phone · chromium · light · bare | 75 → 100 | 17.3 → 9.1 | 17.6 → 9.2 | 0 | 16 | 1372 → 1060 | 1263.6 → 1141.9 | 0 | 70397 → 36754 | 117218 → 117266 | 37 → 21 | 872 → 4.5 | 23 | 14 |
| phone · chromium · light · bloomed | 100 | 9.2 | 9.4 → 9.3 | 0 | 0 | 1372 → 1052 | 1259.2 → 1152.9 | 0 | 70397 → 36754 | 117218 → 117266 | 37 → 21 | 0.1 → 0.2 | 23 | 0 |
| phone · chromium · dark · bare | 73.9 → 100 | 17.4 → 9.3 | 17.5 → 9.3 | 0 | 16 | 1356 → 1060 | 1264.6 → 1145.3 | 0 | 70397 → 36754 | 117218 → 117266 | 37 → 21 | 835 → 5.1 | 23 | 14 |
| phone · chromium · dark · bloomed | 100 | 9.2 | 9.3 → 9.4 | 0 | 0 | 1392 → 1056 | 1267.1 → 1142.5 | 0 | 70397 → 36754 | 117218 → 117266 | 37 → 21 | 0.1 | 23 | 0 |
| phone · chromium · light · bloomed · images off | 100 | 9.3 → 8.9 | 9.4 → 9.2 | 0 | 0 | 1276 → 976 | 1169.1 → 1008.6 | 0 | 70397 → 36754 | 117218 → 117266 | 37 → 21 | 0.1 | 23 | 0 |
| phone · chromium · light · bloomed · motion reduced | 100 | 9.1 | 9.3 | 0 | 0 | 1368 → 1068 | 1267.7 → 1147.5 | 0 | 70397 → 36754 | 117218 → 117266 | 37 → 21 | 0.1 | 23 | 0 |
| phone · webkit · light · bare | 35.3 → 50 | 17 → 19 | 18 → 19 | 0 | 0 | - | 76 | - | 70397 → 36754 | 117218 → 117266 | - | - | 23 | 14 |
| phone · webkit · light · bloomed | 44.1 → 35.3 | 18 → 19 | 18 → 19 | 0 | 0 | - | 81 → 54 | - | 70397 → 36754 | 117218 → 117266 | - | - | 23 | 0 |
| phone · webkit · dark · bare | 35.3 → 48.6 | 18 → 19 | 18 → 19 | 0 | 0 | - | 62 → 55 | - | 70397 → 36754 | 117218 → 117266 | - | - | 23 | 14 |
| phone · webkit · dark · bloomed | 32.4 → 50 | 17 → 19 | 18 → 19 | 0 | 0 | - | 72 → 55 | - | 70397 → 36754 | 117218 → 117266 | - | - | 23 | 0 |
| desktop · webkit · light · bare | 35.3 → 41.2 | 17 → 19 | 17 → 19 | 0 | 0 | - | 78 → 75 | - | 70397 → 36754 | 117218 → 117266 | - | - | 23 | 14 |
| desktop · webkit · light · bloomed | 39.4 → 40 | 18 → 19 | 18 → 19 | 0 | 0 | - | 75 → 56 | - | 70397 → 36754 | 117218 → 117266 | - | - | 23 | 0 |
| desktop · webkit · dark · bare | 38.2 → 45.7 | 17 → 19 | 18 → 19 | 0 | 0 | - | 73 → 69 | - | 70397 → 36754 | 117218 → 117266 | - | - | 23 | 14 |
| desktop · webkit · dark · bloomed | 33.3 → 47.1 | 18 → 19 | 18 → 19 | 0 | 0 | - | 77 → 68 | - | 70397 → 36754 | 117218 → 117266 | - | - | 23 | 0 |
| desktop · chromium · light · bare | 100 | 8.4 → 10.1 | 9.3 → 10.3 | 0 | 0 | 64 → 88 | 46.8 → 43.4 | 0 | 70397 → 36754 | 117218 → 117266 | 39 → 22 | 12.2 → 2.5 | 23 | 14 |
| desktop · chromium · light · bloomed | 100 | 9 → 9.9 | 9.3 → 10 | 0 | 0 | 80 → 64 | 45.1 → 39.6 | 0 | 70397 → 36754 | 117218 → 117266 | 39 → 22 | 0.2 | 23 | 0 |
| desktop · chromium · dark · bare | 100 | 8.4 → 9.9 | 8.5 → 10.2 | 0 | 0 | 64 → 88 | 44 → 40.4 | 0 | 70397 → 36754 | 117218 → 117266 | 39 → 22 | 7.1 → 2.2 | 23 | 14 |
| desktop · chromium · dark · bloomed | 100 | 8.8 → 9.8 | 9.4 → 10.2 | 0 | 0 | 76 → 56 | 44.1 → 39.2 | 0 | 70397 → 36754 | 117218 → 117266 | 39 → 22 | 0.1 → 0.2 | 23 | 0 |

## Diagnosis after the re-measure

**Phone LCP (SC-004).** Moving the shadow filter to `.kcc-fall` (R7) took the filter's outset out of the h1's LCP size
(26,846 to 7,817), so a duotone tile icon became the LCP element on search-1, search-3 and variant-cook. That icon
paints only when the 353 KB `fa-duotone-900` woff2 arrives. Paint got no slower: FCP fell from 1,428 to 1,144 ms.
Subsetting the font files to the safelisted icons brought LCP back to 1,444-1,520 ms in a probe. Owner, 2026-10-26:
leave font subsetting to Phase 3; Phase 1 records the regression.

**Phone interactions and long frames (SC-002, FR-007).** Pre-existing: the baseline was 232-456 ms. On the search
pages the worst entry falls in the frame where the header tucks: `useTuckOnScroll.ts` writes `--pad-offset` on
`<html>`, a full-document style pass of about 52 ms at 4x, and the `.is-tucked` transform keeps the GPU busy for about
570 ms. With no tuck the worst entry is 56 ms. Owner, 2026-10-26: keep the tuck and move the write off `:root`, then
re-measure. Home keeps 12-16 long frames with no tuck; not yet isolated.

**Tuck fix, measured.** `useTuckOnScroll` writes `--pad-offset` on `<main>` (every reader sits inside it) and only
`scroll-padding-top` inline on `<html>`. Phone, Chromium, light, three-run medians: worst interaction on search-1
264 to 200 ms, on home 264 to 136-160 ms; long frames unchanged within noise. The no-tuck (reduced motion) cases stay
at 128-176 ms, so the tuck is no longer the only floor: SC-002 stays unmet on the phone profile.

**Washes after hydration (T040).** Chromium paints a finished animation held by fill mode `both` one way until a
style recalc runs and another way after it, so hydration moved every wash off the first paint. Marking finished
washes after hydration and turning their animation off did not hold: on the 404 page the static paint differed from
the filled one. The kept fix is in the CSS: `.kcc-wash` rests at `opacity: calc(var(--wash-strength) *
var(--wash-peak))` and blooms with fill mode `backwards`, so a finished bloom is not applied at all and the first
paint and the settled page take the same path. Owner, 2026-10-27: approved in look round 2. The look capture now
waits two frames after hydration before the scripts-on screenshot; without them the 404 page passed or failed by
timing.

**Scripts-off residue (T040).** After the wash fix, the Chromium pairs still differed by up to 30 in the header bar
and on search-1. None of it is the page. Under Metal GPU raster a static page with scripts off draws the header's
filtered washes one of two ways from frame to frame (12,506 px, delta 30, or 1,443 px, delta 6, or none), so the
pairs are now drawn in SwiftShader (`LookCapture.ScriptsOffGpu`), where a page that has not changed draws the same:
10 of 13 desktop pairs are identical. The rest is one state: a repaint of the header bar redraws its washes up to 6
levels off the first raster (14,999 px). `useTuckOnScroll` writing `scroll-padding-top` on `<html>` at mount triggers
it reliably; adding and removing a header class triggers it at times; the pairs pass or fail from run to run. A
visitor's hover or scroll repaints the header the same way. Search-1 differs by 19 px at delta 1 in the results grid
in some runs. Owner, 2026-10-27: the header redraw residue is engine noise; T040 closes with it, and a pair that fails
on it alone is read as a pass.

**Baked washes and strength (T047 to T049).** WebKit, desktop, bloomed, live to baked: home 17 to 12-13 long frames,
worst frame 760-780 to 126 ms; search-1 16 to 2-3, 426-432 to 129-132 ms. CSS grows by about 16 KB gzipped (the noise
does not compress). Owner, 2026-10-27: baked approved in look round 3; light strength .42, dark .18 with the dark
soft ink at L .76, chosen in look round 4. The model's rendered readings (`RENDERED_INK_SOFT`) were re-read from the
baked washes and scaled by the harness's reading of the earlier settings over their recorded values. The light
strength now sits under its AA cap, so only dark keeps the tenth-stronger pin.

## Re-measure at f24896f4

The full local matrix (232 cases) after every Phase 1 change, against `baseline.json`:

| Criterion | Baseline | f24896f4 |
|---|---|---|
| SC-001, search-3 phone bloomed, Chromium | 90.3-93.4% within, 8 over 50 ms, worst 258 ms | 99.1%, 5, worst 208 ms |
| SC-001, search-3 phone bloomed, WebKit | 36-38.5% within, 28 over 50 ms, worst 15.1-17.5 s | 41.7-42.7%, 28, worst 597-601 ms |
| SC-002, cases over 100 ms, Chromium phone | 52 of 80, worst 1,040 ms | 43 of 80, worst 384 ms |
| SC-002, Chromium desktop | 0 of 56, worst 64 ms | 3 of 56 (search-3 bare at 104 ms) |
| SC-002, WebKit phone | 28 of 48, worst 13,640 ms | 24 of 48, worst 424 ms |
| SC-002, WebKit desktop | 19 of 48, worst 1,216 ms | 0 of 48, worst 88 ms |
| SC-003, mid-bloom window | | every mid case's worst bloom frame at or under 30 ms |

Budget failures against the baseline margins: `clip_path_elements` on 214 cases (the fall is a clipped static element,
expected; both baselines now hold the measured counts, owner 2026-10-27); `interaction_ms_max` on 13 (variant-cook and
add-variant phone, search-1, search-3 and variant-cook desktop); `long_frames` on 7 (search-3 desktop Chromium, home and
recipe desktop WebKit dark); `lcp_ms` on 6 phone cases (carried to Phase 3); `frame_p95_ms` and
`frames_within_16_7_pct` on one WebKit desktop case each.
