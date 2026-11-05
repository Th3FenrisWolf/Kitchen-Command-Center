# Quickstart: Validate the Crayon Wash Texture

Each scenario names the command, then what passing looks like. The numbers it checks against are in
[contracts/baked-textures.md](contracts/baked-textures.md), [contracts/wash-rendering.md](contracts/wash-rendering.md)
and [research.md](research.md).

## Prerequisites

- Node 24 with Yarn 1, the .NET SDK from `global.json`, the Playwright browsers.
- `FONTAWESOME_NPM_AUTH_TOKEN`, `KCC_E2E_MEMBER_USERNAME`, `KCC_E2E_MEMBER_PASSWORD` set.
- `export SPECIFY_FEATURE_DIRECTORY=specs/007-crayon-wash-texture` for any Spec Kit script.

## 1. The bake is generated, small and seamless

```sh
cd src/KCC.Web && yarn washes && git status --short Features/Styles/Torn
yarn test Bloom
```

Pass: `Washes/` holds `wash-chalk.png` and `wash-blob-0.png` to `wash-blob-7.png` and nothing else; the textures total
under 16,000 B; `bakeWashes.test.ts`, `washTextures.test.ts`, `washes.test.ts`, `keys.test.ts` and
`firstPaint.test.ts` pass, including no blob whose alpha rises outward from its ellipse centre by more than one
4-bit level (FR-002, no darker ring). A second `yarn washes` changes no pixel.

## 2. Placement, size and determinism

Vitest, `washes.test.ts`. Pass: same input, byte-identical styles; wash ids and the schedule's starts unchanged from
`main` for the same seeds and keys; one axis per wash in its band; one-wash cards in a quarter; `d` in .9 to 1.25;
all eight blobs and both flips drawn; every second ink centred within a quarter of its blob's radii from the blob's
ellipse centre, keeping a median of at least .80 of its alpha under the baked blob (10th percentile at least .70);
paired circles disjoint under the span cap; at least three quarters of washes cut by an edge on the card, reading and
hero fields (SC-007); neighbouring sheets repeat a blob and flip within .10 of the same place in at most 5% of wash
pairs. `washesImports.test.ts`: `washes.ts`'s import graph holds no Node module (it ships to the browser) and reads
`BLOB_SHAPES` from `blobShapes.ts`.

## 3. The kit and the contrast table

```sh
cd src/KCC.Web && yarn test Styles
```

Pass: `washField.test.ts` (the rule in the rendering contract, no filter), `grain.test.ts` (texture on the desk,
washes and tiles only), `contrast.test.ts` (ink, soft ink and hairlines over every two-wash stack at its peak in both
ramps, model at or below every re-read rendering, dark pinned at its cap), `motion.test.ts`, `paperRule.test.ts`, `retiredTokens.test.ts` (no retired wash token in `Features`).

## 4. Both bundles, the whole suite

```sh
yarn build:all
dotnet build KitchenCommandCenter.slnx -c Release
node tests/scripts/run.mjs   # unattended: put a no-op `open` first on PATH
```

Pass: no warning; all six suites green (D1, D2, D4). In `src/KCC.Web`: `yarn type-check`, `yarn lint`,
`yarn format` (D3).

## 5. The look against the approved round

```sh
yarn perf:capture 007-chalk
KCC_PERF_CAPTURES=tests/results/perf/captures yarn perf:diff main 007-chalk
```

Open home, search-1, recipe and sign-in, bloomed, both ramps, both engines, both profiles, beside
`mockups/wash-light.jpg` and `mockups/wash-dark.jpg`, the round the owner answered at the projected strengths (plan
step 2), the after-006 captures (`tests/results/perf/captures/main`) and 006's pre-006 captures at `97c51247`
(`tests/results/perf/captures/before`; gitignored, so captured at `97c51247` in its own worktree when missing, or
the desktop-only `.mockups/007-crayon-wash-texture/round-1-assets/*--live.png` as a recorded fallback, tasks T005), in
a look round (`/speckit-preview-run`). Pass: chalk strokes, lobed ragged
faded outlines, no rim, most washes cut by an edge, no fur in dark; where two washes overlap, the lower wash's chalk
marks show through the upper; the largest washes on a reading sheet and the smallest on a phone card read as the step-2 round did; a
mid-bloom capture shows the marks at the wash's current size; a phone card's wash centred past the card's edge still
shows enough colour in its sliver to read as a wash. The owner approves (SC-001). Then D6 (every
touched page in the Browser pane in both ramps) and D7 (`brand-steward` reports no violation).

## 6. Scripts off

```sh
yarn perf:scripts-off 007-chalk
```

Pass: every pair identical, or within the 6-level header residue 006 ruled engine noise (SC-005).

## 7. The performance gate

`main` is measured once, in Setup (tasks T001 to T006), never by switching this checkout (spec A20). Card implement
commits only at its close-out, so this checkout holds the branch's uncommitted work for the whole run, and a local
`main` may lag `origin/main`; every `main` run is in its own worktree at `origin/main`, removed after:

```sh
git fetch origin
git worktree add --detach .claude/worktrees/007-main origin/main
cd .claude/worktrees/007-main
yarn install --frozen-lockfile && yarn build:all && dotnet build KitchenCommandCenter.slnx -c Release
yarn perf
dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj -c Release -- --treenode-filter "/*/*/DeterministicGuardTests/*"
yarn perf:capture main
cd ../../..
cp .claude/worktrees/007-main/tests/results/perf/results.json tests/results/perf/main.json
cp -R .claude/worktrees/007-main/tests/results/perf/deterministic-chromium tests/results/perf/main-deterministic-chromium
cp -R .claude/worktrees/007-main/tests/results/perf/deterministic-webkit tests/results/perf/main-deterministic-webkit
cp -R .claude/worktrees/007-main/tests/results/perf/captures/main tests/results/perf/captures/main
```

The worktree stays for the spike's re-runs and is removed after them
(`git worktree remove --force .claude/worktrees/007-main`). At the gate, `main` is re-measured the same way only if
`origin/main` has moved past the Setup commit with a change under `src/`, `tests/` or the root `package.json`:

```sh
git fetch origin && git diff --stat <Setup commit> origin/main -- src tests package.json
```

Otherwise `tests/results/perf/main.json` stands. The branch, in this checkout:

```sh
yarn build:all && dotnet build KitchenCommandCenter.slnx -c Release
yarn perf --check --before tests/results/perf/main.json
cp tests/results/perf/results.json tests/results/perf/gate.json
dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj -c Release -- --treenode-filter "/*/*/DeterministicGuardTests/*"
cp -R tests/results/perf/deterministic-chromium tests/results/perf/gate-deterministic-chromium
cp -R tests/results/perf/deterministic-webkit tests/results/perf/gate-deterministic-webkit
```

Every `yarn perf` run deletes and rewrites `results.json`, and every guard run rewrites both `deterministic-*`
folders, so each run is copied aside before the next, and the gate reads the copies, never `results.json`.

A case whose verdict differs between the two runs is re-run three times on both branches, narrowed to it, and judged on
its median: here, and in a main worktree at `origin/main` built as above, removed after.

```sh
KCC_PERF_RUNS=3 yarn perf --check --page search-3 --profile phone --engine chromium --ramp light --visit mid
cp tests/results/perf/results.json tests/results/perf/rerun-search-3-phone-chromium-light-mid-007.json
```

In the main worktree the copy is `rerun-<case>-main.json`, into this checkout's `tests/results/perf/`.

Pass, relative to `main` (FR-007, spec A12):

- Every budget and guard that passes on `main` passes here, in both engines, ramps and profiles, after the three-run
  median for any case that flipped. Budgets that fail on `main` are carried: list them from `main.json` in the gate's
  write-up, with their values on both branches.
- SC-003, read from both results (`bloom_frame_max_ms` is report-only, so `--check` does not enforce it): no mid-bloom
  case at or under 50 ms on `main` reads over 50 ms here. The 16 mid-bloom cases are Chromium only; WebKit's bloom is
  unmeasured on both branches and listed as carried, with any Chromium case already over 50 ms on `main`.
- `layer_area_mpx` on this Mac: within 2% of `baseline.ci.json` in the deterministic guard (phone only: search-3
  29.9, home 15.9, recipe 4.9 Mpx) and of the pre-006 `baseline.json` in `--check` (phone 37.0, 21.9, 7.4 Mpx; desktop
  46.7, 38.0, 20.4 Mpx). Past either, the owner decides (spec A18).
- `image_bytes` passes its budget in every case. Texture weight may grow over `main`'s inside budget (the owner's
  ruling), so it is not held to `main`'s value: the textures stay under the 16,000 B test cap, and the `image_bytes`
  ceiling is 17,014 B (research R6).

Each full run is about 70 minutes; run it in the foreground and poll its log in steps of five minutes or less. After the
pull request opens and the review loop's fix commit is pushed, its `perf` job and the E2E deterministic guards pass
ten runs out of ten on the pull request's head commit, and again on any later head (SC-002). That check runs in card implement's review loop, outside `speckit.implement` (tasks T063).

## 8. The docs

Pass: `CLAUDE.md`, `docs/brand/kit.md`, `docs/brand/loose-leaf.md` and the `loose-leaf` skill describe the chalk tile,
the silhouettes, `--wash-span`, `--wash-gain` and the strengths, and name no grain tile, rim, tone or `--wash-reach`
on a wash (D10, FR-012).
