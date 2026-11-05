---

description: "Tasks for the crayon wash texture"
---

# Tasks: Crayon Wash Texture

**Input**: Design documents from `specs/007-crayon-wash-texture/`

**Prerequisites**: plan.md, spec.md (Clarifications, A1 to A24), research.md, data-model.md,
contracts/baked-textures.md, contracts/wash-rendering.md, quickstart.md, mockups/README.md

**Tests**: required. Constitution principle IV puts a failing test before every behavior change, so each
implementation task below follows the test task that covers it, and the test must fail on the code before the change.

**Organization**: the phases follow the plan's implementation order, steps 1 to 8. `main` is measured once, first,
in its own worktree at `origin/main`, while the branch's code still equals it (spec A20). The spike (plan step 1) and the owner's look round at the projected
strengths (plan step 2) come before any bake work, and each ends in an owner checkpoint. Each phase is sized for one
worker subagent. Owner checkpoint tasks are the orchestrator's: a worker stops at one and reports.

**Owner answers (spec A24)**: implement never edits `spec.md`. At an owner checkpoint that needs an answer, implement
stops and reports; the orchestrator puts the question to the owner and records the answer in `spec.md`
Clarifications, with any FR-003b amendment, through `/speckit-clarify`, then resumes implement. Inside implement,
only R12 of `research.md` and `mockups/README.md` record the answer (and `contracts/` or `data-model.md` where T023
(b) changes a number).

**Generated files**: `Features/Styles/Torn/Washes.css` and every PNG in `Features/Styles/Torn/Washes/` come only from
`yarn washes` (in `src/KCC.Web`); `Features/Styles/Torn/Tears.css` only from `yarn tears`, which this feature does
not run. Nobody edits a generated file by hand. Every build is `yarn build:all` at the repository root, both bundles
together, never one side alone.

**Paths**: `src/KCC.Web/Features/...` for the site, `tests/KCC.ViteTests/Features/...` for Vitest, run from
`src/KCC.Web` with `yarn test <pattern>`. Export `SPECIFY_FEATURE_DIRECTORY=specs/007-crayon-wash-texture` before any
`.specify/scripts/` call.

**`main` is never measured in this checkout.** Card implement commits only at its close-out, so this checkout holds
the branch's uncommitted work for the whole run: `git switch main` here would carry it into the "main" measurement,
and a local `main` may lag `origin/main`. Every `main` measurement runs in the **main worktree**, created at
`origin/main` and removed after:

```sh
git fetch origin
git worktree add --detach .claude/worktrees/007-main origin/main
cd .claude/worktrees/007-main && yarn install --frozen-lockfile && yarn build:all \
  && dotnet build KitchenCommandCenter.slnx -c Release
# ... the measurement, then copy its output from .claude/worktrees/007-main/tests/results/perf/ into this checkout
cd <repository root> && git worktree remove --force .claude/worktrees/007-main
```

The harness resolves its results, captures and site from its own repository root, so a run in the worktree writes to
`.claude/worktrees/007-main/tests/results/perf/`. No perf run in the worktree overlaps a run or a build here.

**Every run's output is copied aside before the next run.** Each `yarn perf` run deletes and rewrites
`tests/results/perf/results.json` (`tests/scripts/perf.mjs`), and each deterministic guard run rewrites
`tests/results/perf/deterministic-chromium/` and `tests/results/perf/deterministic-webkit/` (the guard runs both
engines, `DeterministicGuardTests`). A narrowed re-run therefore overwrites the run before it. Every task that reads a
run reads its copy, never `results.json`:

| Run | Copy in `tests/results/perf/` | Task |
|---|---|---|
| `main`, full matrix | `main.json` | T003 (T052 when it re-measures) |
| `main`, guards | `main-deterministic-chromium/`, `main-deterministic-webkit/` | T004 |
| Spike, key cases | `spike.json` | T014 |
| Spike, guards | `spike-deterministic-chromium/`, `spike-deterministic-webkit/` | T015 |
| Gate, full matrix | `gate.json` | T053 |
| Gate, guards | `gate-deterministic-chromium/`, `gate-deterministic-webkit/` | T053 |
| Narrowed re-run | `rerun-<case>-<branch>.json`, `<case>` = `<page>-<profile>-<engine>-<ramp>-<visit>`, `<branch>` = `007` or `main` | T016, T054 |

A run in the main worktree is copied from `.claude/worktrees/007-main/tests/results/perf/` into this checkout's
`tests/results/perf/` under the same names.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: the user story the task serves (US1 crayon not mould, US2 the owner chooses, US3 as fast as 006)

---

## Phase 1: Setup: measure `main` (shared)

**Purpose**: the reference every comparison reads (FR-007, spec A12, A20). `main` is measured once, here, in the
main worktree at `origin/main` (see **`main` is never measured in this checkout** above), never by switching this
checkout. The spike and the gate compare against this run; the gate re-measures only under T052.

- [X] T001 Confirm gates 1 and 2: `FONTAWESOME_NPM_AUTH_TOKEN`, `KCC_E2E_MEMBER_USERNAME`, `KCC_E2E_MEMBER_PASSWORD`
  set; the .NET SDK `global.json` names, Node 24 with Yarn 1 and the Playwright browsers installed. Run
  `git fetch origin` and confirm `git diff --stat origin/main HEAD -- src tests package.json yarn.lock` is empty (the
  branch starts at `origin/main`'s code, so the spike's diff is the spike alone); record `git rev-parse origin/main`
  as the measured commit under a new `## R12. Measurements` heading in
  `specs/007-crayon-wash-texture/research.md`
- [X] T002 Create the main worktree at `origin/main` and build it there:
  `git worktree add --detach .claude/worktrees/007-main origin/main`, then in `.claude/worktrees/007-main`
  `yarn install --frozen-lockfile`, `yarn build:all`, `dotnet build KitchenCommandCenter.slnx -c Release`
  (`KitchenCommandCenter.slnx`). Nothing in this checkout is switched or rebuilt
- [X] T003 In `.claude/worktrees/007-main`, run the full matrix, `yarn perf`, in the foreground, polling its log in
  steps of five minutes or less; copy its `tests/results/perf/results.json` to this checkout's
  `tests/results/perf/main.json`
- [X] T004 In `.claude/worktrees/007-main`, run the deterministic guards,
  `dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj -c Release -- --treenode-filter "/*/*/DeterministicGuardTests/*"`,
  which runs both engines (`DeterministicGuardTests`), and copy its `tests/results/perf/deterministic-chromium` and
  `tests/results/perf/deterministic-webkit` to this checkout's `tests/results/perf/main-deterministic-chromium` and
  `tests/results/perf/main-deterministic-webkit`
- [X] T005 In `.claude/worktrees/007-main`, capture `main`'s look, `yarn perf:capture main`, and copy its
  `tests/results/perf/captures/main` to this checkout's `tests/results/perf/captures/main`, the after-006 reference
  for every look round. Confirm `tests/results/perf/captures/before` (006's pre-006 captures at `97c51247`) exists
  here, for round 9 (T060); `tests/results/` is gitignored, so it exists only in a checkout that 006 ran in. If it is
  missing, capture it: `git worktree add --detach .claude/worktrees/007-before 97c51247`, then in
  `.claude/worktrees/007-before` `yarn install --frozen-lockfile`, `yarn build:all`,
  `dotnet build KitchenCommandCenter.slnx -c Release` and `yarn perf:capture before`; copy its
  `tests/results/perf/captures/before` to this checkout's `tests/results/perf/captures/before`, then
  `git worktree remove --force .claude/worktrees/007-before` from the repository root. Run it after T004 and before
  T006, never beside another run or build. If that capture fails, fall back to the pre-006 live-filter captures in
  `.mockups/007-crayon-wash-texture/round-1-assets/*--live.png` (home and recipe, desktop only), and record the
  fallback and its reach in R12. Keep the main worktree for T016
- [X] T006 From `tests/results/perf/main.json`, write into R12 of `specs/007-crayon-wash-texture/research.md`: the
  budgets that already fail on `main` (carried, not fixed), `bloom_frame_max_ms` for each of the 16 Chromium
  mid-bloom cases, `layer_area_mpx` for search-3, home and recipe against both limits: phone against
  `baseline.ci.json` (29.9, 15.9, 4.9 Mpx; it holds phone cases only, so no desktop limit there) and against the
  pre-006 `specs/006-performance-sweep/baseline.json` (37.0, 21.9, 7.4 Mpx); desktop against the pre-006
  `baseline.json` only (46.7, 38.0, 20.4 Mpx); and `image_bytes`

**Checkpoint**: `main.json`, `main-deterministic-chromium/`, `main-deterministic-webkit/`, the `main` captures and
the `before` captures (or their recorded fallback) exist in this checkout; R12 lists the carried budgets; the main worktree stays for the spike's re-runs.

---

## Phase 2: Spike, part 1: size before texture (US1, US3)

**Goal**: plan step 1's code. The new placement and size, the final padding (`--wash-span` 1.12, 6% of the wash per
side) and the final pair cap (`hypot / (s × span)`), drawn with today's grain and blob textures stretched to the new
padded box. The change ships; it is not thrown away (spec A19).

**Independent test**: `yarn test Bloom Styles` green; the built site shows larger, edge-placed washes with today's
texture.

### Tests first

- [X] T007 [P] [US1] Before changing `washes.ts`, record today's wash ids and the schedule's starts
  (`scheduleStarts`, `fallbackStarts`, `visitStarts`) for fixed seeds and keys on the card, reading and hero fields
  as a fixture in `tests/KCC.ViteTests/Features/Bloom/washes.test.ts`; then add failing tests there, to
  `contracts/wash-rendering.md`: every wash pushes exactly one axis into its band (−.02 to .16, or .84 to 1.02) and
  keeps the other in its half (.10 to .45, or .55 to .90); a one-wash card takes the quarter
  `hash(seed, key, 'corner') & 3`, never the middle; `--wash-d` in [.90, 1.25]; ids and starts equal the fixture;
  paired circles of radius `w · s · span / 2`, span read from `WASH_SPAN` in `bakeWashes.ts`, disjoint over a
  thousand seeds on three fields, with the test's JS width model (`capped`) rewritten to the new cap,
  `min(d · width, hypot / (s · span))`; `WASH_REACH_PX` no longer exported
- [X] T008 [P] [US1] Add failing tests in `tests/KCC.ViteTests/Features/Styles/washField.test.ts`: `--wash-w` caps at
  `hypot(...) / (var(--wash-s) * var(--wash-span))`; `padding` is
  `calc(var(--wash-h) * (var(--wash-span) - 1) / 2) calc(var(--wash-w) * (var(--wash-span) - 1) / 2)` with
  `box-sizing: content-box`; both ink gradients sit in the content box; the blob mask is sized `100% 100%` of the
  padded box; no `--wash-reach` in `Kit.css` or `Tokens.css`. Update `tests/KCC.ViteTests/Features/Styles/paperRule.test.ts`
  wherever it reads `--wash-reach`
- [X] T009 [P] [US1] Add failing tests in `tests/KCC.ViteTests/Features/Bloom/bakeWashes.test.ts`: the generated
  `Washes.css` carries `--wash-span: 1.12`, equal to the exported `WASH_SPAN`, and no `--wash-blob-span`; the grain
  and blob pixels are unchanged
- [X] T009a [P] [US1] Add `--wash-reach` and `--wash-blob-span` to `RETIRED` in
  `tests/KCC.ViteTests/support/retired.ts`, one alternative following the `--wash-op` precedent there
  (`${EDGE[0]}--wash-(?:reach|blob-span)${EDGE[1]}`, with a trailing comment naming what replaced them:
  `--wash-span`). `tests/KCC.ViteTests/Features/Styles/retiredTokens.test.ts` fails on `Kit.css`, `Tokens.css`,
  `Washes.css` and `bakeWashes.ts` until T010 and T012 land. The scan is textual, so a comment that names either
  token counts as a hit

### Implementation

- [X] T010 [US1] In `src/KCC.Web/Features/Bloom/bakeWashes.ts`, export `WASH_SPAN = 1.12` and emit `--wash-span` in
  place of `--wash-blob-span`, leaving today's grain and blob bake as it is; run `yarn washes` in `src/KCC.Web` to
  regenerate `src/KCC.Web/Features/Styles/Torn/Washes.css`; T009 passes
- [X] T011 [US1] In `src/KCC.Web/Features/Bloom/washes.ts`, port `centre` from `contracts/wash-rendering.md` (labels
  `axis` and `corner`), draw `--wash-d` from `between(.9, 1.25, draw('d'))`, and retire `WASH_REACH_PX` everywhere it
  is read; T007 passes
- [X] T012 [US1] In `src/KCC.Web/Features/Styles/Torn/Kit.css`, give `.kcc-wash` the span padding and cap, place
  both ink gradients in the content box, and size the blob mask `100% 100%`; remove `--wash-reach` from both ramps in
  `src/KCC.Web/Features/Styles/Torn/Tokens.css`. Correct the comment above `.kcc-wash` where it becomes untrue; T008
  and T009a pass
- [X] T013 [US1] `yarn build:all` at the repository root; in `src/KCC.Web`, `yarn test Bloom Styles`,
  `yarn type-check`, `yarn lint`, `yarn format`; open home and search-3 in the Browser pane and confirm the washes are
  larger and mostly cut by an edge (`src/KCC.Web/Features/Styles/Torn/Kit.css`)

**Checkpoint**: Vitest green; washes larger and edge-placed with today's texture.

---

## Phase 3: Spike, part 2: measure and route (US3)

**Goal**: see a size regression before the bake (spec A13, A18; research R10).

**Independent test**: R12 holds the spike's verdict per key case against `main`.

- [X] T014 [US3] `dotnet build KitchenCommandCenter.slnx -c Release`; run the key cases,
  `yarn perf --check --before tests/results/perf/main.json --page search-3,home,recipe` (both profiles, both
  engines, both ramps, bare, mid and bloomed), in the foreground, polling every five minutes or less; copy
  `tests/results/perf/results.json` to `tests/results/perf/spike.json` before any other perf run
- [X] T015 [US3] Run the deterministic guards as T004, in this checkout; copy `tests/results/perf/deterministic-chromium`
  and `tests/results/perf/deterministic-webkit` to `tests/results/perf/spike-deterministic-chromium` and
  `tests/results/perf/spike-deterministic-webkit`, and compare each with its `main-deterministic-<engine>` copy;
  read `layer_area_mpx` for search-3, home and recipe against the limits in T006 (phone against both files, desktop
  against the pre-006 `baseline.json`)
- [X] T016 [US3] For each case whose verdict differs from `main` (`spike.json`, `spike-deterministic-*` against
  `main.json`, `main-deterministic-*`), re-run it three times on both branches, narrowed:
  `KCC_PERF_RUNS=3 yarn perf --check --page <page> --profile <profile> --engine <engine> --ramp <ramp> --visit <visit>`,
  here, then the same command in `.claude/worktrees/007-main` (already built at `origin/main` by T002; never
  `git switch main` in this checkout, which would carry the uncommitted spike into the `main` run). After each run,
  before the next, copy its `results.json` to this checkout's `tests/results/perf/rerun-<case>-007.json` or
  `tests/results/perf/rerun-<case>-main.json`. Judge on the median, read from those copies. When the last re-run is
  done, or when no case flipped, remove the worktree from the repository root:
  `git worktree remove --force .claude/worktrees/007-main`
- [X] T017 [US3] Write the spike's table into R12 of `specs/007-crayon-wash-texture/research.md`: each key case's
  verdict on both branches (`spike.json`, `spike-deterministic-*`, `main.json`, `main-deterministic-*`), any
  three-run medians (`rerun-*.json`), `bloom_frame_max_ms` for the mid-bloom cases against 50 ms
  (SC-003, spec A17), `layer_area_mpx` against both limits, and `image_bytes`
- [X] T018 [US3] **OWNER CHECKPOINT, conditional (spec A13, A18).** Route from T017:
  (a) every budget and guard that passes on `main` passes here, and `layer_area_mpx` is inside both limits: no owner
  question; go to Phase 4.
  (b) only `layers` or `layer_area_mpx` passes a limit, and every timing and byte budget holds: no owner question,
  the owner approved this in A18; record the moved cases in R12, go to Phase 4; T058 re-takes those baselines on the
  final build.
  (c) any timing budget, any other guard, or an SC-003 case fails here and passes on `main` after the median: stop
  and report the FR-003b and FR-007 conflict, with the numbers from R12, to the orchestrator. The orchestrator puts it
  to the owner and records the answer in `specs/007-crayon-wash-texture/spec.md` Clarifications through
  `/speckit-clarify` (spec A24); implement does not edit `spec.md`. No bake work until the orchestrator resumes
  implement; implement then records the answer in R12

**Checkpoint**: the spike's route is recorded; under (c), the owner has answered.

---

## Phase 4: Look round at the projected strengths (US2)

**Goal**: plan step 2. The owner sees the approved chalk at about .39 light and .13 dark beside .42 and .18, with
the second ink inside the first ink's ellipse and the largest and smallest washes, before the bake (spec A15, A16).

**Independent test**: the round is recorded in `mockups/README.md` and the owner's answer routes Phase 5.

- [X] T019 [US2] Follow `.claude/skills/speckit-preview-run/SKILL.md` in `look` mode: run
  `.specify/extensions/preview/scripts/preview-setup.sh 007-crayon-wash-texture` (after `yarn build:all`), and open
  the Browser pane with the `preview` configuration in `.claude/launch.json`
- [X] T020 [US2] Write `.mockups/007-crayon-wash-texture/round-8-gen.mjs` from
  `.mockups/007-crayon-wash-texture/approved-gen.mjs`, generating `.mockups/007-crayon-wash-texture/round-8-strengths.html`:
  the approved live filter (mockup only) at .42 and .18 beside about .39 and .13; the second ink centred within a
  quarter of the first ink's radii from its ellipse centre (research R2); the approved round's card fields, an
  unpaired reading-sheet wash at `d` 1.25 of an 860 px sheet (about 1,075 px) and a 340 px phone card's wash at `d`
  .9 (about 300 px), with the outline's turbulence frequencies divided, and its displacement scales and outline blur
  multiplied, by the wash's size over 480 px so each wash previews what one 480 px reference bake draws, the chalk
  marks unscaled; and the large wash again capped at 1,000 px (spec A21). Both ramps
- [X] T021 [US2] Capture the round in Chromium and WebKit, both ramps, into
  `specs/007-crayon-wash-texture/mockups/round-8-<ramp>-<engine>.jpg`, and serve it in the Browser pane
- [X] T022 [US2] Record round 8 under "Rounds" in `specs/007-crayon-wash-texture/mockups/README.md`: what it shows
  and why (A15, A16, A21)
- [X] T023 [US2] **OWNER CHECKPOINT (spec A15, A16, A24).** Implement stops here and reports round 8 to the
  orchestrator, which puts it to the owner, records the answer in `specs/007-crayon-wash-texture/spec.md`
  Clarifications through `/speckit-clarify`, and resumes implement with it. Implement does not edit `spec.md`. Route:
  (a) approve: Phase 5 bakes the contract's numbers; Phase 8 sets the strengths from the re-read contrast table.
  (b) another strength or mark weight: record the numbers, update `specs/007-crayon-wash-texture/contracts/baked-textures.md`
  (stroke or tooth cut) or `specs/007-crayon-wash-texture/data-model.md` (strengths) before Phase 5; run a further
  round if the owner asks.
  (c) the large outline rejected: the owner's px cap on a wash's width applies, the owner's FR-003b amendment (A16);
  the owner names the px value (recommended 1,000 px, the capped variant in round 8); the orchestrator amends FR-003b
  in `spec.md` in the owner's words through `/speckit-clarify` before implement resumes; T036 and T037 run.
  (d) the second ink's placement rejected: stop and report; the orchestrator puts research R2's alternative (a second
  masked element) to the owner.
  On resuming, implement records the answer in R12 of `specs/007-crayon-wash-texture/research.md` and under round 8
  in `specs/007-crayon-wash-texture/mockups/README.md`, and copies the approved round's HTML and captures into
  `specs/007-crayon-wash-texture/mockups/`

**Checkpoint**: the owner's answer is recorded; Phase 5 knows its numbers.

---

## Phase 5: The bake (US1)

**Goal**: plan step 3. `yarn washes` bakes the chalk tile, the eight silhouettes and `BLOB_SHAPES` to
`contracts/baked-textures.md`, with any change T023 recorded.

**Independent test**: quickstart 1; `yarn test Bloom` green. The kit rule still reads retired tokens until Phase 6,
so `washField`, `grain` and `contrast` stay red until Phases 6 and 8 (spec A22).

### Tests first

- [X] T024 [P] [US1] Add failing tests in `tests/KCC.ViteTests/Features/Bloom/bakeWashes.test.ts` to
  `contracts/baked-textures.md`: committed `Washes.css` and textures equal the generator's by pixels, with exactly
  `wash-chalk.png` and `wash-blob-0.png` to `wash-blob-7.png`; all textures under 16,000 B; no `data:` URI; 4-bit
  indexed PNGs with 16 alpha levels in `tRNS`, read by a decoder in the test; the chalk tile 128 x 128, every alpha in
  [.3, 1], mean within .02 of .748, wrap rows and columns within 1.25 times the mean neighbour difference, row and
  column means within ±.01 of the tile mean; each blob 90 x 90, alpha 1 at its ellipse centre, 0 on every edge
  texel, eight distinct; no darker ring (FR-002): along each of 64 rays from a blob's ellipse centre to its texture's
  edge, sampled texel by texel, the decoded alpha never rises by more than one 4-bit level (1/15) over the highest
  alpha nearer the centre on that ray; `BLOB_SHAPES` exported from `src/KCC.Web/Features/Bloom/blobShapes.ts` (not
  from `bakeWashes.ts`, which imports `node:zlib`) with centres in [.28, .72] and closest-side radii as content-box
  fractions, and each blob baked on the ellipse `BLOB_SHAPES` names; `BLOB_COUNT` equals `WASH_BLOBS`; `Washes.css`
  carries `--wash-span: 1.12`, `--wash-gain: 1.5`, `--wash-chalk` and no `--wash-tone` or `--wash-grain`
- [X] T025 [P] [US1] Add failing tests in `tests/KCC.ViteTests/Features/Bloom/washTextures.test.ts`: `isWashTexture`
  matches `wash-chalk.png` and `wash-blob-<n>.png` only, and no longer `wash-grain.png`; `WASH_CHALK_FILE` is
  exported as `wash-chalk.png` and `WASH_GRAIN_FILE` is not

### Implementation

- [X] T026 [US1] In `src/KCC.Web/Features/Bloom/bakeWashes.ts`: `BLOOM_FILTER` to the contract's numbers table; the
  chalk tile (strokes × tooth, plus .5, a 1 px Gaussian blur with wrap, divided by 1.5, rows and columns levelled,
  stitched); the blobs (first-ink profile on each blob's ellipse, the ends, ragged and lobe displacements, a 4 px
  blur, the 480 px reference, 6% margin, 1 px samples box-averaged to 6 px texels), each on its ellipse from
  `BLOB_SHAPES`; create `src/KCC.Web/Features/Bloom/blobShapes.ts`, with no Node import and no `Buffer`, holding
  `BLOB_COUNT`, `BLOB_SHAPES` and the seeded Park-Miller source it draws from (`setupSeed`, `random`), which
  `bakeWashes.ts` imports for its `Turbulence` rather than keeping a second copy; the 4-bit indexed
  encoder with adaptive row filters; `--wash-gain` and `--wash-chalk` emitted, the tile named through
  `WASH_CHALK_FILE` (T027, landed in the same change). Retire `bakeGrain`, `BLOOM_FILTER.grain`, `GRAIN_TILE_PX` and
  the tone in the same change, with every test import of them (`bakeWashes.test.ts`, `washField.test.ts`); T024
  passes
- [X] T027 [US1] In `src/KCC.Web/Features/Bloom/washTextures.ts`, rename `WASH_GRAIN_FILE` to `WASH_CHALK_FILE` and
  name `wash-chalk.png` in place of `wash-grain.png`, updating its import in
  `src/KCC.Web/Features/Bloom/bakeWashes.ts`; leave `src/KCC.Web/Features/Bloom/generateWashes.ts` unchanged; T025
  passes
- [X] T028 [US1] Run `yarn washes` in `src/KCC.Web`; confirm `src/KCC.Web/Features/Styles/Torn/Washes/` holds the nine
  files and nothing else, that a second `yarn washes` leaves `git status --short Features/Styles/Torn` clean, and that
  `src/KCC.Web/Features/Pages/Shared/Layout.cshtml`'s `assets/wash-*.png` preload still covers every texture (nine);
  `yarn test Bloom`, `yarn type-check`, `yarn lint`, `yarn format` in `src/KCC.Web`

**Checkpoint**: the textures are generated, under 16,000 B and stable across runs.

---

## Phase 6: The rule (US1)

**Goal**: plan step 4. `.kcc-wash` and both keyframes to `contracts/wash-rendering.md`: a flat first ink, the
radial second ink, the chalk and blob masks intersected, the mirror transform, the gain on the opacity.

**Independent test**: `yarn test Styles` green except `contrast.test.ts` (Phase 8).

### Tests first

- [X] T029 [P] [US1] Add failing tests in `tests/KCC.ViteTests/Features/Styles/washField.test.ts` to the `.kcc-wash`
  table: the second ink `radial-gradient(closest-side at calc(var(--wash-o3) * 100%) calc(var(--wash-o4) * 100%), …)`
  at .26, `no-repeat`, origin and clip `content-box`; the first ink a flat tint at .34 over the border box;
  `mask-image: var(--wash-chalk), var(--wash-blob)`, `mask-composite: intersect, add`, `mask-mode: alpha`,
  `mask-repeat: repeat, no-repeat`, `mask-position: 0 0, center`, `mask-size: 128px 128px, 100% 100%`;
  `transform: scale(var(--wash-flip), 1)`; the opacity and the ends of `kcc-bloom` and `kcc-bloom-fade` at
  `calc(var(--wash-strength) * var(--wash-peak) * var(--wash-gain))`; both tints without `--wash-tone`; no `filter`,
  `mix-blend-mode`, `isolation`, `will-change` or `border-radius` on `.kcc-wash`; no `--wash-o1`, `--wash-o2`,
  `--wash-bleed-step` or `--wash-grain`; animation, steps and delay unchanged
- [X] T030 [P] [US1] Add failing tests in `tests/KCC.ViteTests/Features/Styles/grain.test.ts`: `--wash-chalk` and the
  blob masks appear on `.kcc-wash` only, `--wash-grain` nowhere, and texture still lies on the desk, the washes and
  the tiles only (FR-011); update `tests/KCC.ViteTests/Features/Styles/paperRule.test.ts` where it reads a retired
  wash token
- [X] T030a [P] [US1] Add `--wash-tone`, `--wash-grain` and `--wash-bleed-step` to `RETIRED` in
  `tests/KCC.ViteTests/support/retired.ts`, as T009a did (`${EDGE[0]}--wash-(?:tone|grain|bleed-step)${EDGE[1]}`,
  with a trailing comment: the chalk tile and the flat first ink replace them). `retiredTokens.test.ts` fails on
  `Kit.css` until T031 lands

### Implementation

- [X] T031 [US1] Rewrite `.kcc-wash`, `kcc-bloom` and `kcc-bloom-fade` in
  `src/KCC.Web/Features/Styles/Torn/Kit.css` to the contract; correct the comment above `.kcc-wash` (it describes the
  rim, the bleed steps and the grain) to the present rule; T029, T030 and T030a pass
- [X] T032 [US1] `yarn build:all`; `yarn test Styles Bloom`, `yarn type-check`, `yarn lint`, `yarn format` in
  `src/KCC.Web`; open home and a recipe in the Browser pane in both ramps and confirm chalk marks, lobed silhouettes
  and no rim (`src/KCC.Web/Features/Styles/Torn/Kit.css`)

**Checkpoint**: the built site draws the baked chalk wash; only `contrast.test.ts` may be red.

---

## Phase 7: The mirror, the second ink and SC-007 (US1)

**Goal**: plan step 5. The planner writes `--wash-flip`, centres the second ink inside its blob's ellipse, stops
writing `--wash-o1` and `--wash-o2`, and SC-007 holds over the baked shapes.

**Independent test**: quickstart 2.

### Tests first

- [X] T033 [P] [US1] Add failing tests in `tests/KCC.ViteTests/Features/Bloom/washes.test.ts`: `--wash-flip` is
  `draw('flip') < .5 ? '-1' : '1'` and both values occur; all eight blobs are drawn; every `--wash-o3`, `--wash-o4`
  lies within a quarter of `BLOB_SHAPES[b]`'s radii of its ellipse centre; `--wash-o1` and `--wash-o2` are not
  written and `WashProperty` drops them; output byte-identical for the same input; ids and starts still equal T007's
  fixture; at least three quarters of washes cut by an edge on the card, reading and hero fields (SC-007, spec A23);
  neighbouring sheets seldom repeat a blob at the same place (edge cases): over a thousand seeds, for each pair of
  consecutive keys on the card and reading fields, a wash on one sheet "repeats" a wash on the other when both draw
  the same blob and flip and their centres (`--wash-x`, `--wash-y`) lie within .10 of each other on both axes; repeats
  are at most 5% of the wash pairs compared; the second ink stays under its blob (research R2): over a thousand seeds
  on the three fields, each second ink's retention, `Σ(a₂ · m) / Σ(a₂)` with `a₂` its closest-side gradient in the
  content box and `m` its blob's alpha from `bakeWashes.ts`'s `bakeBlob(b)` mapped onto the padded box (the
  displaced bake, not the undisplaced estimate), has a median of at least .80 and a 10th
  percentile of at least .70; record the measured median and 10th percentile under R2 in
  `specs/007-crayon-wash-texture/research.md`. A retention under either floor is not loosened: stop and report it,
  and the orchestrator puts R2's alternative (a second masked element) to the owner
- [X] T034 [P] [US1] Add a failing case in `tests/KCC.ViteTests/Features/Bloom/firstPaint.test.ts`: the
  server-rendered wash style carries `--wash-flip` and no `--wash-o1` or `--wash-o2`, and equals the hydrated style
  (FR-009)
- [X] T033a [P] [US1] Add a failing test in a new `tests/KCC.ViteTests/Features/Bloom/washesImports.test.ts`:
  `washes.ts` ships in the client bundle (`Main.ts` registers every `*.Component.vue` eagerly, and
  `KccWashes.Component.vue`, `KccSheet.vue` and `bloomContext.ts` import it), so its static import graph, walked from
  `src/KCC.Web/Features/Bloom/washes.ts` through every `import` and `export … from` specifier, resolving `~/` to
  `src/KCC.Web/Features/` and relative paths, contains no `node:` specifier and no Node builtin
  (`builtinModules` from `node:module`), and includes `src/KCC.Web/Features/Bloom/blobShapes.ts`. It fails before
  T035 because `washes.ts` does not yet import `blobShapes.ts`, and it fails if T035 imports from `bakeWashes.ts`
- [X] T034a [P] [US1] Add `--wash-o1` and `--wash-o2` to `RETIRED` in `tests/KCC.ViteTests/support/retired.ts`, as
  T009a did (`${EDGE[0]}--wash-o[12]${EDGE[1]}`, with a trailing comment: each blob bakes its own ellipse offset).
  `retiredTokens.test.ts` fails on `washes.ts`, and `expectNoRetiredMarkup` on any rendered page with washes, until
  T035 lands

### Implementation

- [X] T035 [US1] In `src/KCC.Web/Features/Bloom/washes.ts`: import `BLOB_SHAPES` from
  `src/KCC.Web/Features/Bloom/blobShapes.ts`, never from `bakeWashes.ts` (it imports `node:zlib` and uses `Buffer`,
  and `washes.ts` ships to the browser), write `--wash-o3` and `--wash-o4` from the drawn blob's ellipse with the
  existing draws, add `--wash-flip`, drop `--wash-o1` and `--wash-o2` from the output and `WashProperty`; T033, T033a,
  T034 and T034a pass
- [ ] T036 [US1] Only if T023 routed (c): add failing tests for the owner's px cap. In
  `tests/KCC.ViteTests/Features/Styles/washField.test.ts`, pin the `--wash-w` expression with the cap as its own
  `min()` term at the owner's px value, written literally (for example `1000px`), so a changed value fails the test.
  In `tests/KCC.ViteTests/Features/Bloom/washes.test.ts`, in this same task, rewrite the JS width model (`capped`) to
  `min(d · width, hypot / (s · span), <cap>)`, so the pair-cap and SC-007 cases measure the capped widths before
  T037 changes the CSS
- [ ] T037 [US1] Only if T023 routed (c): cap `--wash-w` at the owner's px value in
  `src/KCC.Web/Features/Styles/Torn/Kit.css`; T036 passes
- [X] T038 [US1] `yarn build:all`; `yarn test Bloom Styles`, `yarn type-check`, `yarn lint`, `yarn format` in
  `src/KCC.Web` (`tests/KCC.ViteTests/Features/Bloom/washes.test.ts`)

**Checkpoint**: quickstart 2 passes; only `contrast.test.ts` may be red.

---

## Phase 8: Contrast (US1)

**Goal**: plan step 6. Re-read the rendered table from the new textures and pin both strengths (FR-006, SC-004,
spec A10, A11; research R9).

**Independent test**: quickstart 3; `contrast.test.ts` green.

- [X] T039 [US1] After `yarn build:all`, write `.mockups/007-crayon-wash-texture/contrast-harness.mjs`: headless
  Chromium renders ink-soft over each two-wash stack at its peak with the built kit CSS and textures, in both ramps,
  and reads the worst 3 px average; record the readings and the method under R9 in
  `specs/007-crayon-wash-texture/research.md`
- [X] T040 [US1] Add failing tests in `tests/KCC.ViteTests/Features/Styles/contrast.test.ts`:
  `FIRST_INK_CORE_ALPHA` .34 in place of `FIRST_INK_RIM_ALPHA`; `TEXTURE_GAIN` per blend in place of `GRAIN_GAIN`,
  fitted so the model reads at or below every new rendered reading; `--wash-gain` read from `Washes.css`;
  `RENDERED_INK_SOFT` from T039; ink, soft ink and hairlines over every two-wash stack at its peak at 4.5:1 or more
  in both ramps; the light strength at or under its cap; the dark strength pinned at its cap by the tenth-stronger
  test
- [X] T041 [US1] Set `--wash-strength` in `src/KCC.Web/Features/Styles/Torn/Tokens.css`: light at its new cap when
  that is under .42 (or the owner's T023 value when lower), dark at its new cap; correct the comment on the dark
  strength if it becomes untrue; T040 passes
- [X] T042 [US1] `yarn build:all`; `yarn test Styles Bloom` in `src/KCC.Web`; record the strengths taken in
  `specs/007-crayon-wash-texture/research.md` R9

**Checkpoint**: every Vitest file green; both strengths pinned.

---

## Phase 9: Docs (FR-012, D10)

**Purpose**: plan step 7; research R11. Every line the change makes untrue is corrected in the same change.

- [X] T043 [P] Rewrite the wash bullet under "The tear" in `CLAUDE.md`: the chalk tile and the eight silhouettes,
  baked by `yarn washes`, the mirror, `--wash-span`, `--wash-gain`, and the no-filter rule naming the chalk tile and
  the silhouettes; correct the grain bullet if it names a wash grain
- [X] T044 [P] Update `docs/brand/kit.md`: the file table rows for `Washes.css`, `Washes/` and `bakeWashes.ts`; the
  tokens paragraph (`--wash-reach` out, `--wash-span` and `--wash-gain` in, the strengths from T041); the grain
  paragraph; the Washes section (edge-band placement, sizes .9 to 1.25, the fading edge with no rim, "Baked, not
  filtered"); the enforcement table rows
- [X] T045 [P] Update `docs/brand/loose-leaf.md`: Washes and Grain, and a dated change-log entry
- [X] T046 [P] Update the baked-texture line and any wash description in `.claude/skills/loose-leaf/SKILL.md`; check
  `.claude/agents/brand-steward.md` and `.claude/agents/kit-builder.md` for wash descriptions the change makes untrue
  and correct them
- [X] T047 [P] Answer "Still open" in `specs/007-crayon-wash-texture/mockups/README.md`: baked, no filter (research
  R1), and the strengths the contrast table set (T041)
- [X] T048 Search the live files for retired names: `rg -n "wash-reach|WASH_REACH_PX|wash-tone|wash-grain|wash-blob-span|wash-o1|wash-o2|wash-bleed-step|bakeGrain|WASH_GRAIN_FILE|GRAIN_TILE_PX|GRAIN_GAIN|FIRST_INK_RIM" CLAUDE.md docs .claude/skills .claude/agents src/KCC.Web/Features tests/KCC.ViteTests`,
  a double check behind the `RETIRED` entries of T009a, T030a and T034a, which scan only `src/KCC.Web/Features` and
  `src/KCC.Web/uSync/v17`; fix every hit except those entries in `tests/KCC.ViteTests/support/retired.ts` and the
  test assertions that a name is absent, and every line that still describes a rim or a grain tile on a wash (`docs/brand/kit.md`)

**Checkpoint**: quickstart 8 passes.

---

## Phase 10: Gate: suites, brand and performance (US3)

**Goal**: plan step 8, quickstart 4, 6 and 7, D1 to D4, D6, D7 (FR-007, FR-009, SC-002, SC-003, SC-005, SC-006).

**Independent test**: R12 holds the gate's write-up with no budget or guard that passes on `main` failing here.

- [X] T049 [US3] D1 to D4: `yarn build:all`; `dotnet build KitchenCommandCenter.slnx -c Release`; in `src/KCC.Web`,
  `yarn type-check`, `yarn lint`, `yarn format`; `node tests/scripts/run.mjs` with a no-op `open` first on `PATH`;
  all six suites green (`tests/scripts/run.mjs`)
- [X] T050 [US1] D7: run the `brand-steward` agent (`.claude/agents/brand-steward.md`) over the branch's diff; fix
  every violation it reports, test first where it is a behavior change. When T050 changed any file, run T049 again
  in full and run `brand-steward` again, until a pass changes nothing; only then go on to T051
- [X] T051 [US1] D6: open home, recipe search, a recipe and sign-in in the Browser pane against the running site, in
  the light and the dark ramp; note what each shows in R12 of `specs/007-crayon-wash-texture/research.md`
- [X] T052 [US3] `git fetch origin`. If `origin/main` has moved past T001's commit with a change under `src/`,
  `tests/` or the root `package.json` (`git diff --stat <T001 commit> origin/main -- src tests package.json`),
  re-measure `main` as T002 to T004, in a new main worktree at the current `origin/main`, never by switching this
  checkout, record the new commit in R12 and keep the worktree for T054 (quickstart 7); otherwise keep `tests/results/perf/main.json` (spec A20)
- [X] T053 [US3] Run the full gate in this checkout: `yarn perf --check --before tests/results/perf/main.json`, then
  copy `tests/results/perf/results.json` to `tests/results/perf/gate.json` before any other run; then the
  deterministic guards as T004, then copy `tests/results/perf/deterministic-chromium` and
  `tests/results/perf/deterministic-webkit` to `tests/results/perf/gate-deterministic-chromium` and
  `tests/results/perf/gate-deterministic-webkit`. Both in the foreground, polling every five minutes or less
  (`tests/results/perf/gate.json`)
- [X] T054 [US3] Re-run every case whose verdict differs from `main` (`gate.json`, `gate-deterministic-*` against
  `main.json`, `main-deterministic-*`) three times on both branches, as T016: here, and in a main worktree at
  `origin/main` (T052's, or a new one created and built as T002). After each run, before the next, copy its
  `results.json` to `tests/results/perf/rerun-<case>-007.json` or `tests/results/perf/rerun-<case>-main.json`,
  overwriting any spike copy of the same case. Judge on the median read from those copies, then remove the worktree
  with `git worktree remove --force .claude/worktrees/007-main` (`tests/results/perf/`)
- [X] T055 [US3] Read SC-003 from both results: no Chromium mid-bloom case at or under 50 ms on `main` reads over
  50 ms here, with T054's medians for any re-run case (`tests/results/perf/gate.json`,
  `tests/results/perf/main.json`, `tests/results/perf/rerun-*.json`)
- [X] T056 [US3] From `gate.json` and `gate-deterministic-*`, confirm `image_bytes` passes its budget in every case
  (the owner's ruling lets texture weight grow inside budget, so it is not held to `main`'s value): the textures stay
  under the 16,000 B test cap, and the `image_bytes` ceiling research R6 derives is 17,014 B. Confirm `layers` and
  `layer_area_mpx` within their limits: `layer_area_mpx` as T006 lists, `layers` against its baseline in both files
  (`tests/KCC.E2ETests/Features/Performance/baseline.ci.json`, `specs/006-performance-sweep/baseline.json`), since
  T058 re-takes either when it moves
- [X] T057 [US3] Scripts off: `yarn perf:capture 007-chalk`, then `yarn perf:scripts-off 007-chalk`; every pair
  identical or within the 6-level header residue 006 ruled engine noise (SC-005, `tests/results/perf/captures/007-chalk`)
- [X] T058 [US3] Only under T018 (b), or if T056 finds `layers` or `layer_area_mpx` past a limit while every timing
  and byte budget holds (spec A18): re-take only those two metrics, only for the cases the washes move, in
  `tests/KCC.E2ETests/Features/Performance/baseline.ci.json` and `specs/006-performance-sweep/baseline.json`, as each
  limit requires, with the before and after values in R12 for the pull request. A timing failure is not re-taken:
  stop and put it to the owner. `specs/006-performance-sweep/baseline.json` is not only 006's record: it is a live
  harness input, read by `yarn perf --check` through `MatrixRun.BaselinePath`
  (`tests/KCC.E2ETests/Config/Perf/MatrixRun.cs`). Editing those two metrics in it is a baseline change under the
  owner's A18 approval, not a rewrite of 006's history; say so in R12 and in the pull request
- [X] T059 [US3] Write the gate's write-up into R12 of `specs/007-crayon-wash-texture/research.md`: every case's
  verdict on both branches (`gate.json`, `gate-deterministic-*`, `main.json`, `main-deterministic-*`), the medians
  (`rerun-*.json`), the carried list from `main.json` with values on both branches, WebKit's bloom unmeasured on both
  branches and carried, the SC-003 table, `image_bytes`, `layers` and `layer_area_mpx`

**Checkpoint**: no budget or guard that passes on `main` fails here; R12 is the pull request's performance section.

---

## Phase 11: Final look round (US2, US1)

**Goal**: quickstart 5; SC-001, US1 scenarios 1 to 4, FR-005.

**Independent test**: the owner's approval is recorded in `mockups/README.md`.

- [X] T060 [US2] `KCC_PERF_CAPTURES=tests/results/perf/captures yarn perf:diff main 007-chalk`, and write
  `.mockups/007-crayon-wash-texture/round-9-built.html`: home, recipe search, a recipe and sign-in, bloomed and
  mid-bloom, both ramps, both engines, both profiles, beside `specs/007-crayon-wash-texture/mockups/wash-light.jpg`,
  `wash-dark.jpg`, the approved round 8, the after-006 captures (`tests/results/perf/captures/main`) and 006's
  pre-006 captures at `97c51247` (`tests/results/perf/captures/before`, the "original look" the spec's assumptions
  name, or T005's recorded fallback); include the largest reading-sheet wash and the smallest phone-card wash, a
  close crop of a two-wash overlap in both ramps, so the owner can see the lower wash's chalk marks show through the
  upper wash, and a close crop of a phone card's wash centred past the card's edge in both ramps, so the owner can see
  that its sliver still shows enough colour to read as a wash (edge cases)
- [X] T061 [US2] Serve round 9 in the Browser pane and record it under "Rounds" in
  `specs/007-crayon-wash-texture/mockups/README.md`
- [X] T062 [US2] **OWNER CHECKPOINT (SC-001).** The owner approves the built washes as chalk, not moldy or furry, or
  names a difference. A named difference goes back to the phase that owns it (bake numbers Phase 5, rule Phase 6,
  planner Phase 7, strengths Phase 8), test first, then T049 to T059 again for any code change, and a new round.
  Record the approval and its date in `specs/007-crayon-wash-texture/mockups/README.md` and copy the approved
  captures into `specs/007-crayon-wash-texture/mockups/`

**Checkpoint**: SC-001 approved. Implement's phases are all complete here; SC-002 is checked after the pull request
opens (T063).

---

## Post-PR: SC-002 in CI (orchestrator, outside implement)

**Purpose**: card implement opens the pull request only at its close-out, after `speckit.implement` reports, so no
implement phase can run this task. The orchestrator runs it during the review loop on the open pull request
(`speckit-card-implement` Phase 3, step 3). It is not an implement task: `speckit.implement` leaves it unchecked and
reports complete without it.

- [ ] T063 [US3] **Post-PR, orchestrator.** On the open pull request's head commit, after the review loop's fix
  commit is pushed: its `perf` job and the E2E deterministic guards pass ten runs out of ten on that commit (SC-002,
  `.github/workflows/build-and-test.yml`). If any commit lands on the pull request after those ten runs, run the ten
  again on the new head. Record the commit and the run links in R12 of
  `specs/007-crayon-wash-texture/research.md`

**Checkpoint**: SC-002 recorded.

---

## Dependencies and order

- Phase 1 runs first, in the main worktree at `origin/main`, while this branch's code still equals it. Phases 2 and
  3 (the spike) follow; Phase 3 ends at T018. No task switches this checkout off `007-crayon-wash-texture`.
- Phase 4 needs T018 routed (a) or (b), or the owner's answer under (c). Phase 4 ends at T023. No bake work starts
  before T023 is answered.
- Phase 5 needs T023. Phase 6 needs Phase 5 (it reads `--wash-chalk` and `--wash-gain`). Phase 7 needs Phase 5
  (`BLOB_SHAPES`) and Phase 6 (`--wash-flip` in the rule). Phase 8 needs Phases 5 to 7: the harness reads the built
  textures and rule.
- Phase 9 needs Phase 8 (the strengths). Phase 10 needs Phases 1 to 9. Phase 11 needs Phase 10. T063 is post-PR:
  the orchestrator runs it in the review loop after card implement's close-out opens the pull request, outside
  `speckit.implement`.
- Owner checkpoints: T018 (conditional), T023, T062.

## Parallel examples

- Phase 2: T007, T008, T009 and T009a together; then T010 before T011 (the cap test reads `WASH_SPAN`), and T012.
- Phase 5: T024 and T025 together.
- Phase 6: T029, T030 and T030a together.
- Phase 7: T033, T033a, T034 and T034a together.
- Phase 9: T043 to T047 together, then T048.
- No performance run runs beside another or beside a build, in this checkout or the main worktree: they share the
  machine.

## Implementation strategy

The spike is the cheapest way to learn whether the larger washes alone break a budget; it lands before any texture
work so a size conflict reaches the owner while it is still cheap to change. The look round at the projected
strengths settles the strengths and the large outline before the bake fixes them. Each worker takes one phase, never
the whole card, and stops at an owner checkpoint.
