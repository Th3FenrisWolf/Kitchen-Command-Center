---

description: "Tasks for the performance sweep, phase 1"
---

# Tasks: Performance Sweep, Phase 1 - Baseline, Budgets and Paint Cost

**Input**: Design documents from `specs/006-performance-sweep/`

**Prerequisites**: plan.md (with the approved Architecture), spec.md, research.md, data-model.md, contracts/,
quickstart.md

**Tests**: required. The constitution's principle IV puts a failing test before every behavior change; here the
deterministic guards are those tests, and each fix starts by making its guard fail on today's code.

**Organization**: the harness and the baseline come first, because FR-005 needs the baseline before any fix. Then
the guards (US3), the paint fixes they drive (US1), the look round (US2), the reports (US4), the conditional wash
stage, and the close-out.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: the user story the task serves (US1 to US4)

---

## Phase 1: Setup

- [X] T001 Add `pixelmatch` and `pngjs` as root dev dependencies in `package.json`, and the scripts `perf`,
  `perf:live`, `perf:capture`, `perf:diff` and `perf:baseline` as `contracts/perf-commands.md` defines them; each
  script runs the matching E2E test class by filter and then `tests/scripts/perf-report.mjs` or
  `tests/scripts/perf-diff.mjs`
- [X] T002 Verify how TUnit keeps a test out of the default run and selects it by filter (`[Explicit]` or a category
  filter) with a throwaway test in `tests/KCC.E2ETests/Features/Performance/`; record the mechanism in
  `research.md` R4 and delete the throwaway test
- [X] T003 [P] Add `tests/results/perf/`, `tests/results/perf-live/` to `.gitignore` if `tests/results/` is not
  already ignored

---

## Phase 2: Foundational (harness and baseline)

**Purpose**: measure every case before anything changes (FR-001 to FR-005).

- [X] T004 [P] Write a failing integration test for the seeder's recipe count and images in
  `tests/KCC.IntegrationTests/Features/DevTools/RecipeSeederTests.cs`: 36 recipes, every recipe with a generated
  image, idempotent
- [X] T005 Grow the seed data to 36 recipes in `src/KCC.Web/Features/DevTools/RecipeSeed/RecipeSeedData.cs`, and have
  `RecipeTestDataSeeder.cs` generate a deterministic image per recipe with ImageSharp and attach it as Umbraco media,
  inside one core scope that takes `Constants.Locks.ContentTree` before its first read; make T004 pass
- [X] T006 Update every E2E and integration test that counts seeded recipes or pages to the new totals; run the
  whole integration suite with `dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj`
- [X] T007 [P] Add `performance.mark('kcc:hydrated')` after `app.mount('#app')` in `src/KCC.Web/Features/Main.ts`,
  with a Vitest test in `tests/KCC.ViteTests/Features/` that fails first
- [X] T008 [P] Write `tests/KCC.E2ETests/Config/Perf/Profiles.cs`: the phone profile (390x844, DPR 3, Fast 4G
  9 Mbps / 170 ms, 4x CPU, SwiftShader) and the desktop profile (1440x900, unthrottled, real GPU locally and
  SwiftShader in CI), per research R2; Chromium launch arguments for each GPU mode
- [X] T009 [P] Write `tests/KCC.E2ETests/Config/Perf/Cases.cs`: the case tuple and identity string of
  `data-model.md`, the full matrix, and the CI key subset of spec FR-015; visit states through
  `tests/KCC.E2ETests/Config/Visit.cs` (bare, mid at 120 s, bloomed at 400 s back) with a fixed seed
- [X] T010 Write `tests/KCC.E2ETests/Config/Perf/Collectors.cs`: LCP, layout shift, Event Timing, the
  `kcc:hydrated` mark, rAF frame deltas and long-animation-frame entries over a wheel-step scroll and a 5 s
  mid-bloom window, trace totals, CDP layer count and area, idle GPU-process CPU, CSS rule usage, gzip-9 bytes per
  resource type, and the WebKit equivalents of research R3 (unthrottled, labelled)
- [X] T011 Write `tests/KCC.E2ETests/Config/Perf/DeterministicProbes.cs`: counts of elements with a computed
  `filter`, `clip-path`, `mix-blend-mode` or `backdrop-filter`; running animations on filtered or blended elements
  and on elements with a filtered ancestor; filtered area in px²
- [X] T012 Write `tests/KCC.E2ETests/Config/Perf/ResultsWriter.cs` (results.json, baseline files, budget evaluation
  per `contracts/budgets.md`, failure message format of `contracts/perf-commands.md`) and parameterise
  `tests/KCC.E2ETests/Config/MemberSession.cs` by variable names
- [X] T013 Write `tests/KCC.E2ETests/Features/Performance/PerfMatrixTests.cs` (marked per T002) that boots
  `SiteProcess`, runs every selected case with the filters of `contracts/perf-commands.md`, and writes
  `tests/results/perf/results.json`
- [X] T014 [P] Write `tests/scripts/perf-report.mjs`: results.json to `report.html` and `report.md`, each metric
  with value, budget, baseline, delta and status
- [X] T015 Run `yarn perf:baseline` on the unchanged code and commit `specs/006-performance-sweep/baseline.json`
  (FR-005)

**Checkpoint**: `yarn perf` produces the full report on unchanged code.

---

## Phase 3: User Story 3 - A regression fails the pull request (P1)

**Goal**: CI fails a pull request that goes over a budget (FR-015 to FR-017).

**Independent test**: quickstart scenario 5.

- [X] T016 [US3] Write `tests/KCC.E2ETests/Features/Performance/budgets.json` with every metric of `data-model.md`,
  its mode, limit or margin, and owning phase; weight at +2% or 1 KB (owner, plan Q14)
- [X] T017 [US3] Write `tests/KCC.E2ETests/Features/Performance/DeterministicGuardTests.cs`: the CI key subset in
  Chromium and WebKit, each probe of T011 and the byte totals, evaluated against `budgets.json` and
  `baseline.ci.json`; these run in the default E2E suite
- [X] T018 [US3] Write `tests/KCC.E2ETests/Features/Performance/TimingGuardTests.cs` (marked per T002): the key
  subset in Chromium on SwiftShader, one warm-up and five runs, median, evaluated per research R4
- [X] T019 [US3] Add the `perf` job to `.github/workflows/build-and-test.yml`: own runner, build, Playwright
  install, `TimingGuardTests` by filter, results uploaded as an artifact, `report.md` appended to the job summary,
  25-minute timeout
- [X] T020 [US3] Push the branch, take the CI artifact's values into
  `tests/KCC.E2ETests/Features/Performance/baseline.ci.json`, and commit it
- [X] T021 [US3] Run the `perf` job ten times on unchanged code; for each timing metric record in `research.md` R4
  whether it passed ten of ten at its target, and set its mode in `budgets.json` accordingly
- [ ] T022 [US3] Prove a guard fails on a throwaway branch: `filter: blur(1px)` on `.kcc-sheet` fails both jobs
  with the message format of `contracts/perf-commands.md`; delete the branch

**Checkpoint**: guards green on unchanged code, red on a planted regression.

---

## Phase 4: User Story 1 - Scrolling and tapping feel instant (P1)

**Goal**: frames and interaction within budget in Chromium, cheaper in WebKit (FR-007, FR-009, FR-010).

**Independent test**: quickstart scenario 2.

### Shadows to a static fall (research R7)

- [X] T023 [US1] Add a guard to `DeterministicGuardTests.cs` that fails when an element with a computed `filter`
  has a running animation in its subtree; confirm it fails on today's cards
- [X] T024 [US1] Update `tests/KCC.ViteTests/Features/Styles/pile.test.ts` and `kitClasses.test.ts` for the fall
  element: one `.kcc-fall` per slip, before `.kcc-torn`, carrying the drop-shadows, with a clipped inner span; no
  filter on `.kcc-torn`; confirm they fail
- [X] T025 [US1] Add `.kcc-fall` to `src/KCC.Web/Features/Styles/Torn/Kit.css` (filter on the wrapper, tear clip on
  the inner span, same drop-shadows) and remove the filter from `.kcc-torn`
- [X] T026 [US1] Render the fall in `src/KCC.Web/Features/Components/Sheet/KccSheet.vue` and in every slip outside it:
  `AppHeader.Component.vue`, `DetailHero.vue`, `FeaturedRecipeCard.vue`, `RecipeCard.vue`, `RecipeCardRow.vue`,
  `VariantGrid.vue`, `PhotoViewer.vue`, `CookMode.vue`, `Card.Component.vue`, and the Razor views
  `Pages/Account/RegistrationComplete/Index.cshtml`, `Pages/Error/Index.cshtml`,
  `Pages/Home/Blocks/RichTextBlock.cshtml`, `Pages/Home/Blocks/StackerBlock.cshtml`; make T023 and T024 pass

### Stepped bloom (research R8)

- [X] T027 [US1] Add a guard that the bloom's timing function is a staircase of 40 to 50 steps, and update
  `tests/KCC.ViteTests/Features/Styles/motion.test.ts` and `washField.test.ts` for it; confirm they fail
- [X] T028 [US1] Generate a front-loaded `linear()` staircase of about 45 steps for the 90 s bloom and apply it in
  `src/KCC.Web/Features/Styles/Torn/Tokens.css` and `Kit.css`; reduced motion unchanged; make T027 pass

### Grain and crayon as a background (research R9)

- [X] T029 [US1] Update `tests/KCC.ViteTests/Features/Styles/grain.test.ts` (and `pile.test.ts` where it names the
  overlays): grain and crayon are `#app` backgrounds with `background-blend-mode`, no page-sized blended overlay,
  no `isolation` on `#app`; confirm they fail
- [X] T030 [US1] Move grain and crayon into `#app` backgrounds in `src/KCC.Web/Features/Styles/Torn/Kit.css` and
  `src/KCC.Web/Features/Styles/Main.css`, each ramp's opacity baked in, keeping the SVG noise; remove the two spans
  from `src/KCC.Web/Features/App.vue`; make T029 pass

### Stylesheet (research R10, R11)

- [X] T031 [P] [US1] Update `tests/KCC.ViteTests/Features/Torn/tears.test.ts`: each preset's polygon appears once,
  as `clip-path` on grouped selectors for its sheet and blank; `--tear-*` kept for `PhotoStrip.vue`; confirm it fails
- [X] T032 [US1] Change `src/KCC.Web/Features/Torn/tears.ts` and `generateTears.ts` to emit grouped `clip-path` rules,
  run `yarn tears`, and commit the regenerated `src/KCC.Web/Features/Styles/Torn/Tears.css`; make T031 pass
- [X] T033 [P] [US1] Add `tests/KCC.ViteTests/Features/Styles/iconSubset.test.ts`: every icon name in `.vue`,
  `.cshtml` and `.ts` sources, `RecipeIcons.All` and the taxonomy defaults is in the kept set, and no other icon rule
  ships; update `mainCssIconStyles.test.ts`; confirm it fails
- [X] T034 [US1] Add a build step that keeps only the safety list's Font Awesome icon rules and drops the unused
  animation, stack and rotate rules, wired into `src/KCC.Web/Features/Styles/Main.css` and the Vite config; make T033
  pass
- [X] T035 [US1] Generate the halved-point tear variant (40 or 80 points per side) into the scratch folder
  `.mockups/006-performance-sweep/`, not into `Tears.css`, for the look round

### Re-measure

- [X] T036 [US1] Run `yarn build:all` and `yarn perf`; record before and after for every case in
  `research.md`; confirm SC-001 to SC-003 in Chromium and note WebKit's standing for T046

**Checkpoint**: Chromium within frame and interaction budgets; deterministic guards green.

---

## Phase 5: User Story 2 - The site looks exactly as it does today (P1)

**Goal**: the owner approves every page type (FR-012, FR-013).

**Independent test**: quickstart scenario 4.

- [X] T037 [US2] Write `tests/KCC.E2ETests/Features/Performance/LookCaptureTests.cs` (marked per T002): PNG per case
  at a fixed seed, fonts ready, results settled, bare (Chromium, frozen) and bloomed (both engines), plus a
  scripts-off capture compared in light when signed out and in the saved ramp for a signed-in member
- [X] T038 [P] [US2] Write `tests/scripts/perf-diff.mjs`: pixelmatch heat maps per case and a look round page per
  page type in `.mockups/006-performance-sweep/` from `_round.html`
- [X] T039 [US2] Capture `before` from a worktree at the commit before T023, and `after` on this branch
- [X] T040 [US2] Fail the scripts-off check when a page's scripts-off capture differs from its scripts-on first paint;
  fix any page that fails
- [X] T041 [US2] Run `/speckit-preview-run` look rounds: every page type, the stepped bloom, and the halved-point tear
  of T035; record the owner's verdicts in `spec.md` Clarifications and `specs/006-performance-sweep/mockups/README.md`
- [X] T042 [US2] If the owner approves the halved-point tear, apply it in `tears.ts`, run `yarn tears`, commit
  `Tears.css`, and update the size budget in `tears.test.ts`; otherwise leave the tear as it is
- [X] T043 [US2] Fix every difference the owner names, re-capture, and repeat T041 until every page type is approved

**Checkpoint**: owner approval recorded for every page type.

---

## Phase 6: User Story 4 - The owner can see where the time goes (P2)

**Goal**: one command per report, local and live (FR-001, FR-004, SC-007).

**Independent test**: quickstart scenarios 1 and 6.

- [X] T044 [US4] Add the live mode to `PerfMatrixTests.cs`: `KCC_PERF_LIVE_URL`, the measurement member, the
  `X-KCC-Perf` header from `KCC_PERF_BYPASS_TOKEN`, sign in once and reuse the storage state, reads only, ten runs
  for server response with `cf-cache-status` and `content-encoding`
- [X] T045 [US4] Show before and after side by side in `perf-report.mjs` (spec US4 scenario 3)

---

## Phase 7: Washes, stage two (US1, conditional)

**Runs only if T036 leaves WebKit over the frame budget (research R12).**

- [X] T046 [US1] Decide from T036: if WebKit is within budget, mark T047 to T049 not needed and record why in
  `research.md` R12
- [X] T047 [US1] Generate pre-baked wash textures at build time from the `#kcc-bloom` filter, for both ramps, with a
  guard that no `.kcc-wash` carries a live SVG filter
- [X] T048 [US1] Look round for the baked washes against the live ones, both ramps, both engines
- [X] T049 [US1] Ship the baked washes only on the owner's approval; otherwise revert to the live filter and record
  the WebKit gap in the report and `research.md` for a later phase

---

## Phase 8: Close-out

- [ ] T050 Owner action: create and approve the measurement member on production, and add the Cloudflare WAF skip
  rule for `X-KCC-Perf`; record the rule in `docs/hosting/runbook.md` (FR-019)
- [ ] T051 Run `yarn perf:live` after deploy and commit its report summary to `research.md`
- [ ] T052 Owner action: the iPhone pass of quickstart scenario 7; record the result in `research.md` (FR-021)
- [X] T053 [P] Update `docs/brand/kit.md` and `CLAUDE.md` for the fall element, the stepped bloom, grain as a
  background and the grouped tear rules (FR-020, D10)
- [X] T054 [P] Document the budgets and the perf commands in `README.md`
- [X] T055 Flip every budget Phase 1 meets to `target` in `budgets.json`, with its stability trial recorded
- [ ] T056 Run the definition of done: D1 to D7 and D10 (`dotnet build KitchenCommandCenter.slnx -c Release`,
  `yarn build:all`, `yarn type-check`, `yarn lint`, `yarn format` in `src/KCC.Web`, `node tests/scripts/run.mjs`,
  both ramps in the Browser pane, the `brand-steward` agent)

---

## Dependencies and order

- Phase 1, then Phase 2. T015 (baseline) precedes every task from T023 on.
- Phase 3 (guards) precedes Phase 4: each fix starts with its guard failing.
- Inside Phase 4 the four groups (fall, bloom, grain, stylesheet) touch different files except `Kit.css`; run them
  in the order written, one group at a time. T036 closes the phase.
- Phase 5 needs Phase 4. Phase 6 needs Phase 2 and can run beside Phases 4 and 5.
- Phase 7 needs T036 and T041. Phase 8 needs everything else; T050 and T052 are the owner's.

## Parallel examples

- Phase 2: T007, T008, T009 and T014 together; T004 beside them.
- Phase 4: T031 and T033 together, before T032 and T034.
- Phase 5: T038 beside T037.
- Phase 8: T053 and T054 together.

## Implementation strategy

The minimum useful slice is Phases 1 to 3: the baseline and the guards stop the site getting slower. Phase 4's fall
element is the largest single win (research F2) and should land first within it. Each subagent takes one phase or
one Phase 4 group, never the whole card.
