---

description: "Task list for Performance Sweep, Phase 2 - Script Weight, Hydration and Interactivity"
---

# Tasks: Performance Sweep, Phase 2 - Script Weight, Hydration and Interactivity

**Input**: Design documents from `specs/009-script-hydration-cost/`: plan.md (Approach rows D1-D33), spec.md,
research.md, contracts/islands.md, quickstart.md

**Prerequisites**: Phase 1 of the sweep (006) is merged. Gates 1 and 2 of the constitution hold: the Font Awesome token
and the E2E member are set, and Playwright's Chromium and WebKit are installed.

**Tests**: Test-first (constitution IV). Every behaviour change in these tasks starts with a test that fails, and the
next task makes it pass. A move that only changes how code loads, and leaves what it does as it is, keeps every
existing test green and unchanged in what it checks.

**Organization**: one phase is sized for one builder in one context. Phases run in order; a phase's checkpoint must
hold before the next starts.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel with the other `[P]` tasks of its phase (different files, no dependency between them)
- **[Story]**: the user story in spec.md the task serves (US1-US5)
- **[PERSON]**: only the owner can complete it; the builder prepares it, sends it to the orchestrator and waits for
  the owner's answer to be relayed
- **[C2]**: runs only on the path the owner's C2 answer sets (plan.md "C2 fallback amendment")

## Conventions for every task

- **The shared CPU lock (D31).** It covers every E2E run (the suite, one class or one test), every timing or perf run
  (`pnpm perf*`, `DeterministicGuardTests`, `TimingGuardTests`, `ablate.sh`, the trace probes), every capture and diff,
  and every heavy build (`pnpm build:all`, the Release `dotnet build`, `docker buildx bake`). Vitest, the unit suite,
  `pnpm type-check`, `pnpm lint` and `pnpm format` need no lock. A task that does any of the covered work says "under
  the CPU lock" and wraps that work in this script, with `<what>` naming the run:

  ```bash
  SESSION=flow-009
  LOCK=/Users/twinright/Repos/Personal/Kitchen-Command-Center/.git/kcc-cpu.lock
  got=0
  for i in $(seq 60); do
    if mkdir "$LOCK" 2>/dev/null; then got=1; break; fi
    pid=$(sed -n 's/^pid=//p' "$LOCK/owner" 2>/dev/null)
    if [ -n "$pid" ] && ! kill -0 "$pid" 2>/dev/null; then
      mv "$LOCK" "$LOCK.stale.$$" 2>/dev/null && rm -rf "$LOCK.stale.$$"
      continue
    fi
    sleep 5
  done
  if [ "$got" != 1 ]; then
    echo "CPU lock still held after 5 minutes: $(cat "$LOCK/owner" 2>/dev/null)" >&2
    exit 1
  fi
  trap 'rm -rf "$LOCK"' EXIT
  printf 'session=%s\npid=%s\nwhat=%s\nstart=%s\n' "$SESSION" "$$" "<what>" "$(date -u +%FT%TZ)" > "$LOCK/owner"
  <the run>
  ```

  Only the `mkdir` that succeeds takes the lock. A stale lock (its owner pid dead) is renamed away atomically, so two
  waiters never both clear it, and the next `mkdir` decides who takes it. The trap is set only after the lock is
  taken, so a run that never got it removes nothing. A run that exits 1 here has measured nothing: wait and retry it.

- **Builds.** Every bundle builds together: `pnpm build:all` at the repository root, never one side alone. A Release
  site for the harness needs `dotnet build KitchenCommandCenter.slnx -c Release --disable-build-servers` after it.
- **No task commits, pushes, recomposes or opens a pull request.** Card implement's close-out does that.
- **The "before" worktree.** `BEFORE=/Users/twinright/Repos/Personal/Kitchen-Command-Center/.claude/worktrees/009-before`,
  detached at `c1827268`, created in T001 and removed in T087.
- **Q1 is answered** (product checkpoint, answer A): look-preserving fixes first; a wash change still needed goes to
  the owner in a look round and ships only on the owner's relayed approval.
- **C1, C2 and C3 are answered** (2026-11-17, answer A each): the 20 KB proof and the ten-run trial run locally
  against the CI guard classes, and no branch is pushed for them (C1, D24); a wash change still needed goes with one
  question, approve it or keep the look and carry the phone Chromium gap (C2, T056, T056a); the browser reserves
  search's next page below the screen after usable, and first paint and scripts-off stay as today (C3, D19).
- **"Before" for "no worse than baseline"** (FR-007, FR-009, FR-010) is `specs/009-script-hydration-cost/before.json`
  (D27). `specs/006-performance-sweep/baseline.json` is never rewritten on this branch: `pnpm perf:baseline` runs only
  in `$BEFORE`.

---

## Phase 1: Setup - "before" at `c1827268` (D20, FR-001)

**Purpose**: record `main` as it stood when this phase started, before any change lands.

- [X] T001 Create the "before" worktree: `git worktree add --detach "$BEFORE" c1827268`; in it run `pnpm install`,
  then, under the CPU lock, `pnpm build:all` and `dotnet build KitchenCommandCenter.slnx -c Release
  --disable-build-servers`. Confirm the build has no warning. Nothing is measured in this task.
- [X] T002 Run the full matrix at `c1827268` under the CPU lock: in `$BEFORE`, `pnpm perf:baseline` (Phase 1's
  harness, all 232 cases). Copy `$BEFORE/tests/results/perf/results.json` to
  `specs/009-script-hydration-cost/before.json` on the branch.
- [X] T003 Capture the `before` look set under the CPU lock: in `$BEFORE`, `pnpm perf:capture before` (every page
  type, both ramps, both profiles, bare and bloomed). Copy `$BEFORE/tests/results/perf/captures/before/` to
  `tests/results/perf/captures/before/` in this worktree, where `pnpm perf:diff` reads it.
- [X] T004 Re-check sign-in's long frames (research F4): read sign-in's phone Chromium `long_frames` in
  `specs/009-script-hydration-cost/before.json`. If any bloomed or bare case has a long frame, boot the `$BEFORE` site
  as research.md "Method" describes and, under the CPU lock, trace it with
  `specs/009-script-hydration-cost/research/probes/trace.mjs` and `window.mjs`. Write the result under F4 in
  `specs/009-script-hydration-cost/research.md`: either "gone at `c1827268`" or the frames' cause, which T054 then
  fixes.

**Checkpoint**: `before.json` and the `before` capture set exist; F4 is settled.

---

## Phase 2: Foundational - the new measurements and the hydration wait (D15, D17, D21, D23, D25, D26)

**Purpose**: the harness measures what this phase changes before anything changes, and the E2E readiness wait no
longer depends on `#app` holding the app.

**CRITICAL**: no user-story phase starts until this phase's checkpoint holds.

- [X] T005 [P] Write failing tests in `tests/KCC.E2ETests/Features/Performance/PerfHelperTests.cs` for the byte split
  (D15): same-origin scripts whose resource entry starts before the `kcc:hydrated` mark count to
  `js_first_load_gz_bytes`, the rest of the measured visit's same-origin script to `js_later_gz_bytes`, both at gzip
  level 9 like `js_gz_bytes`, and `js_gz_bytes` stays the visit total (first load plus later); a script from another
  origin (Cloudflare's, Phase 3's) counts to neither; a chunk fetched through `<link rel="modulepreload">` before the
  mark counts as first-load script, with the resource entry's initiator type that each engine reports for it; and for
  `islands_hydrated`, the number of elements carrying `__vue_app__` after the mark (1 on today's page, `#app`).
- [X] T006 [P] Write failing tests in `tests/KCC.E2ETests/Features/Performance/PerfCollectorSmokeTests.cs`: a measured
  case reports `js_first_load_gz_bytes`, `js_later_gz_bytes` and `islands_hydrated` on both engines, and
  `restyled_on_tuck`, `restyled_on_hover` and `wash_raster_on_interaction` on Chromium, with a named gap on WebKit; on
  both engines a page with a `modulepreload` link reads that chunk's bytes inside `js_first_load_gz_bytes`.
- [X] T007 Implement the split and the island count in `tests/KCC.E2ETests/Config/Perf/Collectors.cs`, beside the
  existing `js_gz_bytes` read, with the same counter and gzip level and a same-origin filter, so T005 passes; T006's
  byte checks pass under the CPU lock.
- [X] T008 Implement the trace probes (D17, D23) in a new `tests/KCC.E2ETests/Config/Perf/TraceProbes.cs`: a CDP trace
  with `devtools.timeline` and `disabled-by-default-devtools.timeline.invalidationTracking`, around one scripted tuck
  (scroll past `TUCK_BELOW_PX`, 150 px) and one scripted lift (hover the first `.kcc-slip--lift`). `restyled_on_tuck`
  and `restyled_on_hover` sum the `UpdateLayoutTree` element counts of each move; `wash_raster_on_interaction` counts
  paint invalidations that reach a `.kcc-wash` across both. Read them in `DeterministicGuardTests` and the matrix run
  (`tests/KCC.E2ETests/Config/Perf/MatrixRun.cs`, `ResultsWriter.cs`), so T006 passes (under the CPU lock). The probes
  run on Chromium only; on WebKit each reports a named gap.
- [X] T009 Register the new metrics in `tests/KCC.E2ETests/Features/Performance/budgets.json` at their first modes
  (D26, D27): `js_first_load_gz_bytes` `mode: report-only` (006's `baseline.json` has no value for it and is not
  rewritten on this branch) and `ciMode: baseline`; `islands_hydrated`, `js_later_gz_bytes` and the three trace probes
  report-only, `islands_hydrated` until T064 and T065, the probes until T010's three boots agree. Write their CI
  baselines under the CPU lock: run `DeterministicGuardTests` on the branch's Release site with
  `KCC_PERF_MODE=baseline`, which writes `baseline.ci.json` through `GuardRun.Conclude` with `merge: true,
  mergeMetrics: true`. That merge also rewrites every metric the run measured (`ResultsWriter.WriteBaseline`), so
  `git diff` the file and restore every existing value, `captured`, `commit` and `environment` included, keeping only
  the new keys.
  In `tests/scripts/perf-report.mjs`, show every metric with a before column, and mark each case and metric worse than
  `--before` beyond its budget's local margin (D27).
- [X] T010 Under the CPU lock, run `DeterministicGuardTests` on three fresh boots of the branch's Release site and
  compare `restyled_on_tuck`, `restyled_on_hover` and `wash_raster_on_interaction`. If any differs between boots, fix
  the probe's scripted moves or waits in `TraceProbes.cs` and repeat until three boots read the same counts. Record the
  counts in `specs/009-script-hydration-cost/research.md`.
- [X] T011 Add "before" values for the new metrics (D25), under the CPU lock: copy this worktree's
  `tests/KCC.E2ETests/` and `tests/scripts/` over `$BEFORE`'s (`cp -R`, no git operation), rebuild there with the
  Release command, run `pnpm perf:baseline` in `$BEFORE`, and add only the keys `js_first_load_gz_bytes`,
  `js_later_gz_bytes`, `islands_hydrated`, `restyled_on_tuck`, `restyled_on_hover` and `wash_raster_on_interaction` to
  each case of `specs/009-script-hydration-cost/before.json`. T002's values for every other metric stay.
- [X] T012 Move the E2E readiness wait and the mount checks to the documented "usable" mark (D21):
  `tests/KCC.E2ETests/Config/Hydration.cs` (`Hydration.WaitAsync`, line 10) waits for
  `performance.getEntriesByName('kcc:hydrated').length > 0` instead of `#app.__vue_app__`. In
  `tests/KCC.E2ETests/Features/Chrome/BloomTests.cs`, both mount checks read the mark too: `RecordBeforeMount`'s
  `mounted` (line 24, asserted false as `MountedBefore` at line 129) and the scripts-off check (line 151). After
  islands, `#app` never carries `__vue_app__`, so both would pass whatever happened. `c1827268` already writes the mark
  (`Main.ts:34`), so under the CPU lock run the E2E suite on the unchanged site and confirm it stays green. This moves
  ahead of the islands work so the E2E suite keeps running through Phases 4-6.

**Checkpoint**: the report shows each new metric before and after; three boots agree on the probes; E2E green with
the new wait.

---

## Phase 3: User Story 3 - islands: registry and server output (D1-D4, D12, D22, D33) (Priority: P1)

**Goal**: the sidecar knows which global components are islands, wraps each in `<kcc-island data-island="N">`, and
returns `islands`, `components` and `pageWashes` (contracts/islands.md "Global components", "Server output").

**Independent Test**: Vitest's island, SSR and audit tests pass; the sidecar's `/render` returns the three new fields
for a home page and a search page.

- [X] T013 [P] [US3] Write failing `tests/KCC.ViteTests/Features/Islands/registry.test.ts`: `GlobalComponents.ts`
  exports an eager registry (sidecar, fallback) and a lazy registry (client, one dynamic import per file) over the same
  `*.Component.vue` glob; the islands are exactly `AppHeader`, `Stacker` and the eight page views; the server-only
  components are exactly `AppFooter`, `Card`, `UnderlineLink`, `KccWashes`, `Breadcrumbs`, `ResourceString` and
  `SmallHero`; a lowercase tag resolves to the same entry; no `serverOnly` component has a `<style>` block (D33: in
  development its CSS would vanish with `<style data-ssr-styles>`). None has one today, so that check passes at once;
  the others fail.
- [X] T014 [P] [US3] Write failing `tests/KCC.ViteTests/Features/Islands/ssrIslands.test.ts` and extend
  `tests/KCC.ViteTests/Features/Ssr/serverEntry.test.ts`: a render returns `islands` (`{ name, props, keys }` in
  document order, `keys` the wash keys each island claimed, in order), `components` (the registry path of every global
  component rendered) and `pageWashes`; each island's HTML sits in `<kcc-island data-island="N">`; a server-only
  component gets no wrapper; an island inside a server-only component's slot content gets its own; the render fails,
  naming the component, when an island placed by Razor receives slot content or a function prop.
- [X] T015 [P] [US3] Write failing `tests/KCC.ViteTests/Features/Islands/explicitImports.test.ts` (D12): for every
  `.vue` file under `src/KCC.Web/Features/`, every component tag in its template is imported by that file, Vue's
  built-ins excepted. It must fail today on `Breadcrumbs` in the recipe, variant and search views.
- [X] T016 [P] [US3] Write failing `tests/KCC.ViteTests/Features/Islands/islandSelectors.test.ts` (D2): `kcc-island`
  is `display: contents` in the kit's base layer, and every child, sibling or structural selector in
  `src/KCC.Web/Features/Styles/**` and the style blocks that can meet an island root (an island root sits at `#app`'s
  top level, as `<main>`'s child, or in the stacker's grid) is on a checked list that names how it still reaches the
  root, e.g. `:where(#app > *)` and `.content-grid > :not(.breakout, .full-width)`.
- [X] T017 [US3] Split `src/KCC.Web/Features/GlobalComponents.ts` into the eager and the lazy registry, with the island
  or server-only rule read from the component's `serverOnly` option (D3), and add `defineOptions({ serverOnly: true })`
  to `Components/Footer/AppFooter.Component.vue`, `Widgets/Card/Card.Component.vue`,
  `Components/Links/UnderlineLink.Component.vue`, `Components/Sheet/KccWashes.Component.vue`,
  `Components/Breadcrumbs/Breadcrumbs.Component.vue`, `Components/ResourceStrings/ResourceString.Component.vue` and
  `Widgets/Hero/SmallHero.Component.vue`, so T013 passes. Declare the option for the type checker with a
  `ComponentCustomOptions` augmentation (`declare module 'vue' { interface ComponentCustomOptions { serverOnly?:
  boolean } }`) in a new `src/KCC.Web/Features/Islands/serverOnly.ts`, so `pnpm type-check` accepts
  `defineOptions({ serverOnly: true })`.
- [X] T018 [US3] Add to `src/KCC.Web/Features/Islands/`: `payload.ts` (the island entry and payload types),
  `ssrIslands.ts` (the server wrapper that renders `<kcc-island data-island="N">`, records name, evaluated props and the
  wash keys claimed inside, and refuses slot content and functions), and `registry.ts` (the client lookup from name to
  the lazy loader). The wrapper reads claimed keys from the bloom in `Features/Bloom/bloomContext.ts` through a scope
  per island; the key replay on the client is T032's.
- [X] T019 [US3] Return the three fields from the sidecar: `src/KCC.Web/Features/Ssr/Server.Entry.ts` uses the eager
  registry with the island wrapper, and `src/KCC.Web/Features/Ssr/Server.js` adds `islands`, `components` and
  `pageWashes` beside `html` and `schedule` in `/render`, so T014 passes. Development style inlining
  (`CollectCss.js`, `<style data-ssr-styles>`) is unchanged.
- [X] T020 [US3] Import every component a `.vue` file uses (D12): `Breadcrumbs` in
  `Pages/RecipeDetail/RecipeDetailView.Component.vue`, `Pages/VariantDetail/VariantDetailView.Component.vue` and
  `Pages/RecipeSearch/RecipeSearchView.Component.vue`, and any other tag T015 names, so T015 passes.
- [X] T021 [US3] Add `kcc-island { display: contents }` to the kit's base layer under
  `src/KCC.Web/Features/Styles/Torn/` and adjust each selector T016's audit finds that no longer reaches an island root,
  so T016 passes. Kit enforcement tests and `contrast.test.ts` stay green.
- [X] T022 [US3] Run the web Vitest suite and `pnpm type-check` in `src/KCC.Web`. Until Phase 5 the browser still
  hydrates the whole page from the region templates, so a Release page reports a hydration mismatch on the wrappers;
  the E2E suite is not run in this phase.

**Checkpoint**: the sidecar wraps islands and returns `islands`, `components` and `pageWashes`; Vitest green.

---

## Phase 4: User Story 3 - head links and preloads (D10, D11, FR-014) (Priority: P1)

**Goal**: every rendered component's CSS is linked in the head and every island's chunk closure is preloaded, from
the build manifest, so the scripts-off first paint keeps every style once the client stops importing all components.

**Independent Test**: unit tests pass; a Release page's head lists `Main.css`, then each rendered component's CSS
once, and a `modulepreload` per island chunk.

- [X] T023 [P] [US3] Write failing tests in `tests/KCC.UnitTests/Features/Ssr/VueSsrServiceTests.cs`: `SsrResult`
  carries `Islands`, `Components` and `PageWashes` from `/render`, and empty values when a field is missing.
- [X] T024 [P] [US3] Write failing `tests/KCC.UnitTests/Features/Islands/IslandHeadLinksTests.cs` against a manifest
  fixture behind `Vite.AspNetCore.IViteManifest`: a stylesheet link for the CSS of every component in `Components` and
  of its static imports, deduplicated and after `Main.css`; a `modulepreload` for the chunk of every island in
  `Islands` and its static imports, the shared Vue chunk included; nothing when the result has no HTML.
- [X] T025 [US3] Add `Islands`, `Components` and `PageWashes` to `src/KCC.Web/Features/Models/Common/SsrResult.cs`
  and read them in `src/KCC.Web/Features/Ssr/VueSsrService.cs`, so T023 passes.
- [X] T026 [US3] Create `src/KCC.Web/Features/Islands/IslandHeadLinks.cs` and write its links in
  `src/KCC.Web/Features/Pages/Shared/Layout.cshtml`'s head after the `Main.css` link, from the `app` result the layout
  already computes before the head, so T024 passes. The header is preloaded like any island (D11). Dev-server mode
  writes none, as today's entry stylesheet link is skipped there.
- [X] T027 [US3] Under the CPU lock, `pnpm build:all` and the Release build, then load home and search in the Browser pane against the
  Release site and check the head: one link per CSS file, the preloads present, nothing duplicated with the entry's
  own CSS link. Run the unit suite.

**Checkpoint**: the head links every rendered component's CSS and preloads each island's chunks; the page still
hydrates whole.

---

## Phase 5: User Story 3 - payload, bloom and the island bootstrap (D7, D8, D9) (Priority: P1)

**Goal**: `#server-content` carries the bloom and the islands instead of region HTML, and `Main.ts` hydrates each
island from its own chunk, marking `kcc:hydrated` after the last (contracts/islands.md "Hydration").

**Independent Test**: Vitest and unit tests pass; under the CPU lock the E2E suite passes unchanged in what it checks.

- [X] T028 [P] [US3] Write failing tests in `tests/KCC.UnitTests/Features/Ssr/SsrHtmlContentTests.cs`: with rendered
  HTML, `#server-content` holds `{ isPreview, bloom: { seed, page, pageWashes }, islands }` and no `headerContent`,
  `bodyContent` or `footerContent`; with no HTML it holds today's three region templates; an island prop holding
  `</script>` (and `<!--`) is written escaped, so `#server-content` ends only at its own closing tag and the parsed
  prop equals the input.
- [X] T029 [P] [US3] Write failing tests in `tests/KCC.ViteTests/Features/Bloom/bloomContext.test.ts`: replaying an
  island's server `keys` makes the client claim the same qualified keys the server did; `markHydrated(pageWashes)`
  takes the server's page-wash count, so late starts match today's for the same visit time (FR-017).
  `firstPaint.test.ts` and `keys.test.ts` stay green.
- [X] T030 [P] [US3] Write failing `tests/KCC.ViteTests/Features/Islands/hydrate.test.ts` and extend
  `tests/KCC.ViteTests/Features/Hydration/hydratedMark.test.ts`: every `<kcc-island>` on load hydrates in parallel with
  `createSSRApp(component, props)` and the one bloom and `isPreview` provided; `kcc:hydrated` is marked after the last
  mounts, then `markHydrated(pageWashes)` and the heartbeat; the API config is set before any island mounts; a chunk
  that fails to load leaves its island's server HTML and still lets the rest mark; `style[data-ssr-styles]` is removed
  after the last island.
- [X] T031 [US3] Write the payload in `src/KCC.Web/Features/Models/Common/SsrHtmlContent.cs` and its type in
  `src/KCC.Web/Features/Types/ContentRegions.ts`, so T028 passes.
- [X] T032 [US3] Add the key replay and `markHydrated(pageWashes)` to `src/KCC.Web/Features/Bloom/bloomContext.ts`,
  so T029 passes.
- [X] T033 [US3] Create `src/KCC.Web/Features/Islands/hydrate.ts` and reduce `src/KCC.Web/Features/Main.ts` to the
  bootstrap (contracts/islands.md "Hydration"): the dev error overlay import stays first, then the API config, one
  bloom, the hydrator, the mark, the heartbeat and the dev style removal. `App.vue` and the eager registry leave the
  client graph and stay in `Ssr/Server.Entry.ts`. T030 passes.
- [X] T034 [US3] Under the CPU lock, `pnpm build:all`, the Release build, then the E2E suite. It passes with no
  change to what it checks; the BloomTests clock checks hold (FR-017). Fix the code, never a check.

**Checkpoint**: pages hydrate as islands; the E2E suite is green. The client still carries the template compiler.

---

## Phase 6: User Story 3 - runtime-only Vue and the fallback build (D5, D6, D32) (Priority: P1)

**Goal**: the island build drops the template compiler; a third build keeps today's in-browser rendering for a page
the sidecar could not render (contracts/islands.md "Fallback").

**Independent Test**: with the sidecar off every page renders in the browser and its controls work; with it on no
client chunk carries `@vue/compiler-core`.

- [X] T035 [P] [US3] Write failing `tests/KCC.ViteTests/Features/Islands/clientBuild.test.ts` over
  `src/KCC.Web/vite.config.ts`: the client build resolves `vue` to the runtime-only package default and defines
  `__VUE_OPTIONS_API__`, `__VUE_PROD_DEVTOOLS__` and `__VUE_PROD_HYDRATION_MISMATCH_DETAILS__` as `false`; the SSR build
  and the dev server keep `vue.esm-bundler.js`; `--mode fallback` builds `Features/Fallback.ts` with the compiler into
  `wwwroot/fallback/`.
- [X] T036 [P] [US3] Write failing `tests/KCC.IntegrationTests/Features/Pages/FallbackPageTests.cs`: with SSR off, as
  `RenderedPage` runs, a page links the fallback entry from `wwwroot/fallback/` and not `Features/Main.ts`, links a
  stylesheet for every CSS file of the fallback entry's closure as `wwwroot/fallback/.vite/manifest.json` lists it
  (D32: Vite.AspNetCore reads only the main manifest, so without these links the fallback page loses every component
  style in production), and `#server-content` carries the three region templates `RenderedPage` reads. Write failing
  `tests/KCC.UnitTests/Features/Islands/FallbackLinksTests.cs` against a fallback manifest fixture: the entry script
  and its CSS closure, each once, in production; in dev-server mode a module script for `Features/Fallback.ts` from the
  dev server and no `wwwroot/fallback/` link, since the dev server builds nothing into `wwwroot/fallback/`.
- [X] T037 [US3] Implement D5 and D6 in `src/KCC.Web/vite.config.ts`, and create `src/KCC.Web/Features/Fallback.ts`,
  today's whole-app entry (`App.vue`, the eager registry, the bloom, the mark, the heartbeat), so T035 passes.
- [X] T038 [US3] Make `build:all` in `src/KCC.Web/package.json` build the client, SSR and fallback bundles together;
  include `wwwroot/fallback/.vite/manifest.json` in `src/KCC.Web/KCC.Web.csproj`'s publish as the main manifest is
  (`<Content Include="wwwroot/.vite/**" />` does not match it); add `**/wwwroot/fallback` to `.gitignore` beside
  `**/wwwroot/ssr` (no rule ignores it today). Under the CPU lock, run `pnpm build:all` and confirm `git status` shows
  nothing under `wwwroot/fallback/`.
- [X] T039 [US3] Link the fallback in `src/KCC.Web/Features/Pages/Shared/Layout.cshtml` when the sidecar returned no
  HTML (SSR off, failure, timeout, open circuit), through a reader in `src/KCC.Web/Features/Islands/` (D32): in
  production the entry script and a stylesheet link for each CSS file of its closure, from
  `wwwroot/fallback/.vite/manifest.json`; in dev-server mode `Features/Fallback.ts` from the dev server. The island
  entry and T026's links only when the sidecar returned HTML. T036 passes.
- [X] T040 [US3] Check `.github/workflows/build-and-test.yml` and the `Dockerfile`: every step, cache and artifact that
  carries `wwwroot` output also carries `wwwroot/fallback/`; change the workflow only where it does not. The Dockerfile
  copies every `src/*/wwwroot` and needs no instruction change (its line-3 comment is T076's; T085 confirms the image).
- [X] T041 [US3] Sidecar down (quickstart 8): under the CPU lock, `pnpm build:all` and the Release build; boot the
  Release site with `VueSsr__Enabled=false`, open every page type in the Browser pane, confirm the header and the
  other style-block styles hold, and use its controls; do the same once against the dev server; then the sidecar on,
  confirm no client chunk in `wwwroot/` contains `compiler-core`. Run `FallbackPageTests`, `FallbackLinksTests` and
  the integration tests under `tests/KCC.IntegrationTests/Features/Pages/`.

**Checkpoint**: three bundles build together; the island client is runtime-only; the fallback renders.

---

## Phase 7: User Story 3 - later parts: wizard steps and cook mode (D13, D14, D30, FR-013) (Priority: P1)

**Goal**: the wizard's later steps and cook mode load after usable, prefetched at idle, and answer within budget even
when a reader outruns the chunk.

**Independent Test**: create-recipe and add-variant read at most 61,440 B first-load; every Next answers within
100 ms on the phone; with the network cut after `kcc:hydrated`, Next still moves the progress bar.

- [X] T042 [P] [US3] Write failing `tests/KCC.ViteTests/Features/Components/RecipeWizard/wizardSteps.test.ts`: the
  glob in `wizardSteps.ts` holds exactly the `Components/RecipeWizard/*Step.vue` files on disk except `StartStep`
  (D34: `BasicsStep` and `VariantStep` are later parts, and the opening step is resolved before hydration, D35), so a new `*Step.vue` joins it by its name alone; `laterStep(name)` throws on an
  unknown name; no file under `src/KCC.Web/Features/` statically imports a later step; `prefetchLaterSteps()` calls
  every loader; a later step's pending state is the step's own `KccSheet` and tear, empty, with `aria-busy="true"` and
  no text and no dictionary key (D30).
- [X] T043 [P] [US3] Write failing tests in `tests/KCC.ViteTests/Features/Components/VariantDetail/` (a new
  `CookModeLater.test.ts`): `VariantDetailView.Component.vue` takes `CookMode.vue` through `defineAsyncComponent`, with
  no static import; opening cook mode before its chunk arrives changes state at once and shows the pending sheet (D30:
  the part's own `KccSheet` with its tear, empty, `aria-busy="true"`, no text), and cook mode fills in on arrival. Write failing `tests/KCC.ViteTests/Features/Islands/laterParts.test.ts`: registered
  prefetches run in idle time after `kcc:hydrated`, with a timeout where `requestIdleCallback` is missing (Safari).
- [X] T044 [P] [US3] Write a failing E2E test `tests/KCC.E2ETests/Features/RecipeWizard/LaterStepTests.cs` with the
  E2E member: on Create Recipe, block the later-step chunks after `kcc:hydrated`, press Next, and see the progress bar
  move at once and the step sheet pending (`aria-busy="true"`); unblock and see the step fill in with nothing typed
  lost (quickstart 9). Confirm it fails under the CPU lock.
- [X] T045 [US3] Create `src/KCC.Web/Features/Components/RecipeWizard/wizardSteps.ts` (one
  `import.meta.glob(['./*Step.vue', '!./StartStep.vue'])` (D34), each wrapped in `defineAsyncComponent` with the
  pending sheet of D30, exporting `laterStep(name)` and `prefetchLaterSteps()`), and in `RecipeWizard.vue` replace each
  later step's static import with `const <Name>Step = laterStep('<Name>Step')` (`BasicsStep`, `VariantStep`,
  `IngredientsStep`, `InstructionsStep`, `MatchStep`, `PhotoStep`, `ReviewStep`), the opening step resolved through
  `beforeHydrate` before hydration (D35), templates unchanged; `next()`
  changes `wizard.step` at once. If `Components/RecipeWizard/DetailsStep.vue` exists on the branch after a rebase
  (feature 010 merged first), make the same swap for it, `const DetailsStep = laterStep('DetailsStep')`, and leave
  `DetailsSummary` a static import in `ReviewStep.vue`; build nothing else of 010. If it does not exist, build no
  Details step. T042 passes; then, under the CPU lock, `pnpm build:all`, the Release build and T044, which passes.
- [X] T046 [US3] Take `CookMode.vue` through `defineAsyncComponent` in
  `src/KCC.Web/Features/Pages/VariantDetail/VariantDetailView.Component.vue`, with its prefetch registered (D14); create
  `src/KCC.Web/Features/Islands/laterParts.ts` and schedule it from `Main.ts` after the mark. T043 passes.
- [X] T047 [US3] Under the CPU lock, build and run `pnpm perf --page create-recipe,add-variant,variant-cook` on both
  profiles and engines and read `js_first_load_gz_bytes`. If either wizard page is over 61,440 B, first add a failing
  check to `wizardSteps.test.ts` that `ReadingState.vue`, `SubmittedSheet.vue` and `PhotoStrip.vue` are not static
  imports, then move them to `defineAsyncComponent` by cook mode's rule and measure again. Record the numbers in
  `specs/009-script-hydration-cost/research.md`.
- [X] T048 [US3] Under the CPU lock, `pnpm perf --page create-recipe,add-variant --profile phone --engine chromium`:
  every Next and the cook-mode button answer within 100 ms the first time (FR-013). Run the web Vitest suite and the
  E2E suite (CPU lock) unchanged in what they check.
- [X] T048a [US3] The perf harness never presses a wizard Next, and on the phone cook mode has no opener, so
  `tests/KCC.E2ETests/Features/RecipeWizard/FirstPressTests.cs` measures FR-013 (Event Timing, phone profile for the
  wizard, desktop for cook mode, bare and bloomed). It reads 144-496 ms for the wizard presses with every later step
  already prefetched. Under the CPU lock, run FirstPressTests against `$BEFORE` (copy the test over, as T011 did) to
  record what `c1827268` reads; trace a step change on the phone profile (main thread: script, style, layout, paint,
  per call site) and cut the step-change render until every wizard press is within 100 ms on bare and bloomed visits.
  A cut that changes how a sheet, tear or wash looks follows Q1: look-preserving first, a look change only through
  a look round. Record before, trace and after in research.md F9.
  Closed by the owner's ruling ("Keep look, approve E", `specs/009-script-hydration-cost/mockups/README.md`): the
  look-preserving cuts shipped, and the phone SwiftShader wizard presses are the carried FR-013 gap (research.md F11).

**Checkpoint**: every page's first-load script is at most 61,440 B; later parts arrive at idle and answer when
outrun. This completes User Story 3's weight.

---

## Phase 8: User Story 1 - render work for taps and frames (D18, D28, FR-003, Q1, C2) (Priority: P1)

**Goal**: the tuck, the lift and the first scroll stop restyling all of `<main>` and re-rasterising masked washes, so
every tap answers within 100 ms and frames hold their budget without changing the look.

**Independent Test**: `pnpm perf --check --page search-3,home,sign-in --profile phone --engine chromium` and the
desktop profile in both engines pass interaction and frame budgets (phone Chromium at baseline instead, if T056a
applied); `restyled_on_tuck` and `wash_raster_on_interaction` drop from their before values.

- [X] T049 [P] [US1] Write failing tests for D28, and move the tests that pin `--pad-offset` to it:
  `tests/KCC.ViteTests/Features/Components/Header/useTuckOnScroll.test.ts` (lines 26-38 read `--pad-offset`): `--pad-h`
  is written on `<main>` on measure and resize only, never on a tuck or a show; the tuck and the show toggle
  `.is-tucked` on `<main>`; `scroll-padding-top` on the root is written as today. `tests/KCC.ViteTests/Features/Styles/stickyOffsets.test.ts`
  (lines 28-30 require `var(--pad-offset`): every sticky element adds `var(--pad-h)` to its top only under
  `main:not(.is-tucked)`, so it keeps its offset for the shown and the tucked pad. Update the markup checks to the new
  rule, each still checking the same offset: `tests/KCC.ViteTests/Features/Pages/RecipeSearch/RecipeSearchView.test.ts:137`,
  `tests/KCC.ViteTests/Features/Widgets/Stacker.test.ts:76-77,87-88` and
  `tests/KCC.IntegrationTests/Features/Pages/Home/HomeSectionsPageTests.cs:43`.
- [X] T050 [P] [US1] Write a failing E2E test in `tests/KCC.E2ETests/Features/Chrome/PadTests.cs` that names the tuck's
  class toggle: after a scroll past 150 px `<main>` carries `.is-tucked` and no `--pad-offset`, and its `--pad-h` is
  the value it held before the scroll; the sticky neighbours sit where they did. Existing pad and phone-pad checks stay
  unchanged. Confirm it fails under the CPU lock.
- [X] T051 [US1] D18 (1), D28: in `src/KCC.Web/Features/Components/Header/useTuckOnScroll.ts` (line 21 writes
  `--pad-offset` on every tuck), write `--pad-h` on `<main>` only on measure and resize, and toggle `.is-tucked` on
  `<main>` on tuck and show; keep the root's `scroll-padding-top` as today. Move every reader to
  `var(--pad-h)` under `main:not(.is-tucked)`: `Pages/Home/Blocks/StackerBlock.cshtml:10`,
  `Pages/RecipeSearch/RecipeSearchView.Component.vue:129` and `Widgets/Stacker/Stacker.Component.vue:43,48` (the cards'
  and the sentinels' inline tops); any shared rule goes in `@layer components` under
  `src/KCC.Web/Features/Styles/Torn/`. No stylesheet reads `--pad-offset` today. Correct the comment above `publish`
  in `useTuckOnScroll.ts`, which then describes the old property. Timing and end states unchanged. T049 passes; under
  the CPU lock, `pnpm build:all`, the Release build and T050, which passes, and the integration suite.
- [X] T052 [US1] Measure D18 (1) under the CPU lock: `restyled_on_tuck` and `restyled_on_hover` from
  `DeterministicGuardTests`, and `sh specs/009-script-hydration-cost/research/probes/ablate.sh / home base` and the same
  for search-1 and search-3; then `pnpm perf:capture after` and `pnpm perf:diff before after` on home and search
  (CPU lock). Record in `specs/009-script-hydration-cost/research.md`.
- [X] T053 [US1] D18 (2), one raster candidate at a time, each started with its probe expectation in
  `DeterministicGuardTests` (`wash_raster_on_interaction` lower than T052's reading) and measured under the CPU lock
  with that probe, `ablate.sh` on home, search-1 and search-3, and a capture and diff (CPU lock). Candidates: composite
  a lifting card or the tucking pad only while it moves (no layer at rest, so Phase 1's exact layer counts hold,
  FR-010); keep a lifted card from invalidating the sheets around it; keep the first scroll's newly exposed sheets
  from re-rasterising their washes more than once. Edit the kit CSS under `src/KCC.Web/Features/Styles/Torn/` (never the
  generated `Tears.css` or `Washes.css`). Each candidate keeps the tear's invariants (CLAUDE.md "The tear"): the
  fibre-and-fall `filter` stays on `.kcc-fall`, outside the clipped sheet; a slip's `.kcc-torn`, a wash, its chalk tile
  and its silhouette take no filter; grain and crayon stay on the desk, the washes and the tiles only; a wash stays
  placed by `KccSheet` and `KccWashes` and blooms from `--wash-elapsed`. After each candidate run
  `tests/KCC.ViteTests/Features/Styles/grain.test.ts`, `pile.test.ts`, `kitClasses.test.ts`, `kitStates.test.ts`,
  `washField.test.ts`, `motion.test.ts` and `contrast.test.ts`, and `tests/KCC.ViteTests/Features/Torn/tears.test.ts`;
  a candidate that needs one of them changed is set aside. Keep a candidate only if those pass, the diff flags nothing
  and the layer counts hold; set aside, unshipped, any candidate that changes how a wash looks, for T056.
- [X] T054 [US1] If T004 found sign-in's long frames at `c1827268`, write a failing test for their cause and fix it,
  running any E2E or perf check of it under the CPU lock; otherwise mark N/A with T004's finding.
- [X] T055 [US1] Re-measure (D18 (3)) under the CPU lock: `pnpm perf --check --page search-3,home,sign-in --profile
  phone --engine chromium`, then `--profile desktop` in both engines, bare and bloomed, reduced motion on and off.
  Record each page's worst interaction, long frames and share within 16.7 ms, and any gap left, in
  `specs/009-script-hydration-cost/research.md`. Headless WebKit on the phone is no worse than `before.json` (FR-009).
- [X] T056 [PERSON] [C2] [US1] Only if T055 leaves a gap that only a wash change closes: prepare one
  `/speckit-preview-run` look round on only the affected pages, before and after, both ramps, phone and desktop, under
  the CPU lock; save the screenshot paths with the measured gain each change buys (worst interaction, share within
  16.7 ms and long frames, before and after); send them to the orchestrator with one question for the owner: approve
  the wash change, or keep the look and carry the phone Chromium gap. On "approve" the change ships, and T055 runs
  again with it. On "keep the look" the change stays out of the branch and T056a applies. Until the answer is relayed
  the change stays out of the branch. N/A when T055 leaves no gap.
  Ruling (2026-11-18): "Keep look, approve E", recorded in `specs/009-script-hydration-cost/mockups/README.md`.
- [X] T056a [C2] [US1] Only on the owner's relayed "keep the look" answer to T056 (plan.md "C2 fallback amendment"):
  apply the drafted text to `specs/009-script-hydration-cost/spec.md` (FR-004, FR-005, SC-001, SC-002, and the
  sentence under the phase table); in `tests/KCC.E2ETests/Features/Performance/budgets.json`, add entries scoped
  `{"profile": "phone", "engine": "chromium"}` for `interaction_ms_max`, `frames_within_16_7_pct`, `long_frames` and
  `frame_p95_ms` that hold today's baseline mode and margins, after the general entries (the last matching budget wins,
  `ResultsWriter.cs:98`), so T066's flip to target leaves phone Chromium at baseline; the report's before column reads
  them against `before.json` (D27); record the gap in
  `specs/009-script-hydration-cost/research.md` with T055's measured numbers per page and visit. T066 then flips no
  phone Chromium interaction or frame metric to target. N/A on "approve" or when T056 is N/A.

**Checkpoint**: interaction and frame budgets met by look-preserving changes or an approved wash change, or the phone
Chromium gap carried by the owner's decision with the amendment applied.

---

## Phase 9: User Story 2 - layout shift while search appends (D19, FR-008, C3) (Priority: P1)

**Goal**: nothing on screen moves when the next page of results arrives.

**Independent Test**: `cls` and `cls_scroll` read 0 on search-1 and search-3 in every case, except a reader parked on
the footer, which is no worse than before: 0.0134 at 1920x1080 and at most 0.1478 at 375x812, against 0.093 and 0.27
before (FR-008, amended 2026-11-18).

- [X] T057 [US2] Record the shifting nodes under the CPU lock: the `layout-shift` entries' `sources` on search-3, both
  profiles and engines, bare and bloomed, while `LoadThreePagesAsync` appends. Add them to F5 in
  `specs/009-script-hydration-cost/research.md`.
- [X] T058 [P] [US2] Write a failing Vitest test in a new
  `tests/KCC.ViteTests/Features/Components/RecipeSearch/ReservedSpace.test.ts` (D19, the owner's C3 answer): the
  server render holds no reserved space, so its HTML is today's; after `kcc:hydrated`, while more results remain, the
  browser adds the space the next page needs below the screen, sized from the server's total and the grid's columns;
  on the last page no space is held.
- [X] T059 [P] [US2] Write a failing E2E test in `tests/KCC.E2ETests/Features/RecipeSearch/` (a new
  `AppendShiftTests.cs`): a `PerformanceObserver` sees no `layout-shift` entry with a value above 0 while three pages
  append, and the reader's scroll position does not move when a page lands (FR-012). A second case covers a short
  first page: a viewport tall enough that the footer is on screen at `kcc:hydrated`, with more results remaining; it
  asserts no `layout-shift` entry above 0 from first paint through the first append. Confirm both fail under the CPU
  lock.
- [X] T060 [US2] Reserve the space in the browser after `kcc:hydrated`, below the screen, in
  `src/KCC.Web/Features/Components/RecipeSearch/` and `src/KCC.Web/Features/Pages/RecipeSearch/RecipeSearchView.Component.vue`,
  with any kit rule in `@layer components` under `src/KCC.Web/Features/Styles/Torn/`. The server's HTML, first paint
  and the scripts-off page stay as today. Infinite scroll behaves as it does today. T058 passes; under the CPU lock,
  `pnpm build:all`, the Release build and T059, which passes. If the short-first-page case cannot reach 0 without a
  server-drawn band, record its measured shift in research.md F5, send it to the orchestrator, and draw no band.
- [X] T061 [US2] Under the CPU lock: `pnpm perf --check --page search-1,search-3` on both profiles and engines
  (`cls` and `cls_scroll` 0), `pnpm perf:capture after` and `pnpm perf:diff before after` on search, and
  `pnpm perf:scripts-off` on search (scripts-off first paint matches).
  `cls_scroll` is held to amended FR-008, no worse than before for the parked reader (search-3 phone 0.2356 to 0.1422, desktop 0.1484 to 0.0214); the check still prints its target of 0 as a fail. Numbers in research.md F5.

**Checkpoint**: search appends with zero layout shift; look and scripts-off hold on search.

---

## Phase 10: User Story 5 - guards at target (D16, D17, D23, D24, D26, D29, C1, FR-020 to FR-022) (Priority: P2)

**Goal**: CI enforces the 60 KB first-load target, the visit total and the usable-time targets, and deterministic
stand-ins for the interaction and frame budgets; the local check enforces every budget this phase meets.

**Independent Test**: the guard tests pass on unchanged code every run, and fail on 20 KB of added script with page,
profile, engine, ramp, budget and number.

- [X] T062 [P] [US5] Write failing tests in `tests/KCC.E2ETests/Features/Performance/PerfHelperTests.cs`: a target
  budget scaled by the runner factor (`RunnerCalibration.cs`) for `hydrated_ms`; a failure of
  `js_first_load_gz_bytes` at target names page, profile, engine, ramp, budget and measured number.
- [X] T063 [US5] Implement the runner-scaled target in `tests/KCC.E2ETests/Config/Perf/GuardRun.cs` and
  `RunnerCalibration.cs`, and the bytes guard (D29) in
  `tests/KCC.E2ETests/Features/Performance/DeterministicGuardTests.cs` over `Cases.KeySubset` (home, search-3, recipe,
  variant-cook, account) plus create-recipe and add-variant, both engines, for `js_first_load_gz_bytes` and
  `js_gz_bytes`; `hydrated_ms` at target and at baseline with `ciMargin` in `TimingGuardTests.cs`. T062 passes. The two
  wizard cases have no `baseline.ci.json` entry until T065 writes one.
- [X] T064 [US5] Set the modes in `tests/KCC.E2ETests/Features/Performance/budgets.json` per plan.md "Guards":
  `js_first_load_gz_bytes` target 61,440 (local and CI); `js_gz_bytes` target 117,266; `js_later_gz_bytes`
  report-only; `islands_hydrated` baseline, no increase (D26: report-only until here); `restyled_on_tuck`,
  `restyled_on_hover` and `wash_raster_on_interaction` at the targets Phase 8 reached, scoped to Chromium, since the
  trace stand-ins run on Chromium only and WebKit reports a named gap; `hydrated_ms` target 2,000 ms phone and
  1,000 ms desktop; `cls` and `cls_scroll` target 0 locally and report-only on CI; interaction and frame metrics
  report-only on CI.
- [X] T065 [US5] Refresh the deterministic values of `tests/KCC.E2ETests/Features/Performance/baseline.ci.json` from
  the branch head under the CPU lock (D26): `DeterministicGuardTests` with `KCC_PERF_MODE=baseline` on both engines,
  which metric-merges into the file, so `islands_hydrated` and the probes take the islands' counts and create-recipe
  and add-variant gain entries. The timing values (`hydrated_ms`, `server_response_ms`) and `calibrationMs` stay
  Phase 1's CI values: no branch is pushed to collect new ones (C1). `git diff` the file and confirm no timing value
  changed.
- [X] T066 [US5] Stability (D24, C1), under the CPU lock for each run, on unchanged code: three runs each of
  `DeterministicGuardTests` and `TimingGuardTests`, the guard classes CI runs, and three of `pnpm perf --check`
  (the owner's 2026-11-19 decision amends the C1 answer from ten to three); plus
  `DeterministicGuardTests` on three fresh boots reading the same probe counts. Flip `interaction_ms_max`,
  `frames_within_16_7_pct`, `long_frames` and `frame_p95_ms` to target in the local check only for the metrics that
  pass three of three (not phone Chromium when T056a applied); a metric that does not stays at baseline locally, and its
  stand-in from T064 carries it on CI (FR-021). Record every run in `specs/009-script-hydration-cost/research.md`.
  No branch is pushed; CI runs the same classes on the pull request.
- [X] T067 [US5] The 20 KB proof (SC-007, D24, C1), under the CPU lock: as an uncommitted scratch edit, import about
  20 KB gzipped of script into `src/KCC.Web/Features/Main.ts`, `pnpm build:all`, the Release build, run
  `DeterministicGuardTests` and `TimingGuardTests`, and confirm the byte guard fails naming page, profile, engine,
  ramp, budget and number; remove the edit, rebuild, and confirm both pass and `git diff src/KCC.Web/Features/Main.ts`
  shows no trace of it. Record the failure text in `specs/009-script-hydration-cost/research.md`. No branch is
  pushed; the CI runs follow on the open pull request.

**Checkpoint**: guards at target, stable locally three of three and three of three; the 20 KB proof recorded.

---

## Phase 11: User Story 4 - after, look and scripts-off (D20, D27, FR-010, FR-014, FR-018) (Priority: P1)

**Goal**: the whole matrix after this phase, compared with before; the look and the scripts-off first paint hold on
every page type.

**Independent Test**: `pnpm perf --check --before specs/009-script-hydration-cost/before.json` passes; the diff flags
nothing, or the owner approved every flagged page; scripts-off passes on every page type.

- [X] T068 [US4] Under the CPU lock: `pnpm build:all`, the Release build, then
  `pnpm perf --check --before specs/009-script-hydration-cost/before.json` over the full matrix. Copy
  `tests/results/perf/results.json` to `specs/009-script-hydration-cost/after.json`. Confirm SC-001 to SC-004 from the
  report: first-load at most 61,440 B and visit total at most 117,266 B on every page type; `hydrated_ms` within
  2,000 / 1,000 ms and no later than before; no metric Phase 1 holds worse than before (FR-010, layer counts included);
  headless WebKit on the phone no worse than before on any metric (FR-009). "Before" is `before.json` through the
  report's before column (D27), not 006's `baseline.json`; list every case the report marks worse.
- [X] T069 [US4] Under the CPU lock: `pnpm perf:capture after`, then `pnpm perf:diff before after`: every page type,
  both ramps, both profiles, bare and bloomed. List the flagged pages, if any, in
  `specs/009-script-hydration-cost/research.md`.
- [X] T070 [US4] Under the CPU lock: `pnpm perf:scripts-off` on every page type, light signed out and the saved ramp
  for the E2E member, with Phase 1's residue rule (FR-014).
- [X] T071 [US4] Fix any regression T068 to T070 found, each starting with a failing test, and repeat the run that found
  it under the CPU lock. N/A when none.
- [X] T072 [PERSON] [US4] Only if T069 flagged a page: prepare a `/speckit-preview-run` look round on only the flagged
  pages, before and after, both ramps, phone and desktop; save the screenshot paths and send them to the orchestrator.
  A flagged page ships only after the owner's approval is relayed; approved shots go to
  `specs/009-script-hydration-cost/mockups/`. N/A when nothing is flagged.

**Checkpoint**: `after.json` written; look and scripts-off hold or are approved.

---

## Phase 12: Polish - docs (FR-023, constitution VII, definition-of-done row DoD D10)

**Purpose**: every doc line this phase makes untrue is corrected.

- [X] T073 [P] Update `CLAUDE.md`: "Vue SFC `<style>` blocks" (CSS linked per rendered component from the manifest;
  `pnpm build:all` builds the client, SSR and fallback bundles; the fallback path), a short "Islands" section (the
  `serverOnly` rule, props only for an island Razor places, explicit imports, later parts and the wizard step rule:
  `<Name>Step.vue` plus `const <Name>Step = laterStep('<Name>Step')`), and the new budgets.
- [X] T074 [P] Update `docs/brand/kit.md`: the hydration lines in its SSR section (`kcc-island`, the payload, the
  fallback) and "build both bundles together" becoming all three.
- [X] T075 [P] (009's own `contracts/perf-commands.md` and `contracts/budgets.md` replace the edit to 006's) Update `specs/006-performance-sweep/contracts/perf-commands.md` and
  `specs/006-performance-sweep/contracts/budgets.md`: `js_first_load_gz_bytes`, `js_later_gz_bytes`,
  `islands_hydrated`, the restyle and raster probes (Chromium only), the same-origin first-load split, the byte guard's
  cases (D29), the target modes and the runner-scaled target.
- [X] T076 [P] Update `README.md` and `docs/hosting/runbook.md` where they name the bundle; grep the repository's
  Markdown for `two bundles`, `both bundles`, `main-*.js`, `__vue_app__`, `headerContent`, `build:ssr`,
  `GlobalComponents`, `GlobalComponents-*.css`, `registerGlobalComponents` and `global registration`, and correct each
  line that is now untrue. Dated plans under `docs/replatform/plans/` record past work and stay as written. Correct the `Dockerfile`'s line-3 comment ("The client and SSR bundles come out of one
  `pnpm build:all`"), which names two bundles; no Dockerfile instruction changes. This comment edit makes DoD D9 apply
  (T085).

---

## Phase 13: Polish - acceptance and the definition of done

**Purpose**: every definition-of-done row that applies and every success criterion. "DoD Dn" below names a row of
the constitution's definition of done, not an Approach row of plan.md: DoD D1-D4, D6, D7, D9 and D10 apply; DoD D5
and D8 are N/A unless T083 or T085 finds otherwise.

- [X] T077 DoD D1 and D2, under the CPU lock: `pnpm build:all` at the repository root, then
  `dotnet build KitchenCommandCenter.slnx -c Release`; no warning.
- [X] T078 DoD D3: in `src/KCC.Web`, `pnpm type-check`, `pnpm lint` and `pnpm format`; keep what lint and format change.
- [X] T079 Run the full integration suite: `dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj`.
- [X] T080 Run the Vitest suites (web, admin, contributions) and, under the CPU lock, the E2E suite. Confirm with
  `git diff main -- tests/KCC.E2ETests` that no existing check changed what it asserts: only the readiness wait and
  `BloomTests`' two mount checks moved to the `kcc:hydrated` mark (T012), the tuck class test (T050), new tests and
  the perf harness differ (SC-006).
- [X] T081 DoD D4, under the CPU lock: `node tests/scripts/run.mjs` at the repository root with a no-op `open` first on
  `PATH`; all six suites pass.
- [X] T082 DoD D6 and D7: open every touched page type in the Browser pane against the Release site in the light and
  the dark ramp, including a later part's pending sheet (D30: the step's own `KccSheet` and tear, empty,
  `aria-busy="true"`, no text), shown by blocking the later-step chunk as T044 does; run the `brand-steward` agent
  over the diff; it reports no violation.
- [X] T083 DoD D5 is N/A unless `tears.ts`, the schema, the dictionary, the baseline content or a contributions entity
  changed; confirm none did. (2026-11-23: the dictionary did change, `VariantDetail.TryAgain` and
  `VariantDetail.UnexpectedError` for the failed-part error state, so DoD D5 applies; `DictionaryTests` in T079's
  integration run is their boot test, and it passes.)
- [X] T084 Walk quickstart.md scenarios 2 to 9 against `after.json` and the running site, each measurement under the
  CPU lock, and tick each spec acceptance scenario of US1-US5 it covers in
  `specs/009-script-hydration-cost/research.md`.
- [X] T085 DoD D9, under the CPU lock (T076 changes the Dockerfile's comment): `docker buildx bake --load`, then
  `deploy/smoke-test.sh`; the image carries `wwwroot/fallback/`. DoD D8 is N/A unless `deploy/` changed.
- [X] T086 [PERSON] The owner's iPhone pass (FR-011, SC-005): in Safari on an iPhone against this phase's build, scroll
  recipe search to three pages and back, open a recipe and cook mode, and use the header's search and pad cards, at a
  fresh visit and two minutes into one. The builder sends the steps and the build's address to the orchestrator and
  records the owner's relayed answer, whether it keeps up and whether taps answer at once, in
  `specs/009-script-hydration-cost/research.md`.
- [x] T087 Remove the "before" worktree: `git worktree remove "$BEFORE"`, after T069's diff and T084 are done.

---

## Dependencies & Execution Order

### Phase dependencies

- **Phase 1 (before)**: first; nothing on the branch changes code before `before.json` and the `before` captures exist.
- **Phase 2 (measurement, wait)**: after Phase 1 (T011 adds to `before.json`). Blocks every later phase.
- **Phases 3-7 (US3)**: in order. 3 (server output) → 4 (head links, needs `components`) → 5 (payload and bootstrap,
  needs the links so production keeps its CSS) → 6 (runtime-only, needs the bootstrap so no template compiles) → 7
  (later parts, needs the bootstrap's idle hook). Phase 3 leaves a hydration mismatch that Phase 5 removes; the E2E
  suite runs from Phase 5 on.
- **Phase 8 (US1)**: after Phase 2's probes; independent of Phases 3-7 in code, but runs after them so its
  measurements read the final script.
- **Phase 9 (US2)**: after Phase 8 (shares search's captures and Kit.css).
- **Phase 10 (US5)**: after Phases 7-9; the targets come from their results.
- **Phase 11 (US4)**: after Phase 10.
- **Phase 12 (docs)**: after Phase 11; T073-T076 in parallel.
- **Phase 13 (acceptance)**: last.

### Within each phase

- Tests marked [P] first, together; each must fail before its implementation task runs.
- Implementation tasks in ID order.
- E2E runs, measurement, capture and heavy builds take the CPU lock and run one at a time.

### Parallel opportunities

- Phase 2: T005, T006.
- Phase 3: T013, T014, T015, T016.
- Phase 4: T023, T024. Phase 5: T028, T029, T030. Phase 6: T035, T036.
- Phase 7: T042, T043, T044. Phase 8: T049, T050. Phase 9: T058, T059 (T059's E2E run takes the CPU lock).
- Phase 12: T073-T076.

## Parallel Example: Phase 3

```text
Task: "Write failing registry.test.ts in tests/KCC.ViteTests/Features/Islands/"
Task: "Write failing ssrIslands.test.ts and extend serverEntry.test.ts"
Task: "Write failing explicitImports.test.ts"
Task: "Write failing islandSelectors.test.ts"
```

## Implementation Strategy

1. Phases 1-2: record before and the new metrics; nothing user-visible changes.
2. Phases 3-7: the script split (US3), the largest share of the budget work. Stop after Phase 7 and check every page's
   first-load bytes against 61,440 B.
3. Phase 8: the interaction and frame budgets (US1), look-preserving first; a wash change only through T056, and the
   phone Chromium gap carried only through T056a.
4. Phase 9: search's layout shift (US2).
5. Phase 10: guards to target (US5).
6. Phase 11: after, look and scripts-off (US4), with T072 only if the diff flags a page.
7. Phases 12-13: docs, the definition of done, and the owner's iPhone pass.

## Notes

- `[PERSON]` tasks: T056 (a wash change still needed, conditional), T072 (pages the diff flags, conditional), T086
  (the iPhone pass).
- `[C2]` tasks: T056 (the one look round and question) and T056a (the amendment, only on "keep the look").
- Feature 010 builds on Phase 7: its `DetailsStep` takes `const DetailsStep = laterStep('DetailsStep')` and is split by
  its file name alone. If 010 merges first, T045 makes that swap after the rebase.
