---
description: "Task list for the ink wash bloom and the grain rule"
---

# Tasks: Ink Wash Bloom and the Grain Rule

**Input**: Design documents from `specs/005-ink-wash-bloom/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md),
[contracts/visit.md](contracts/visit.md), [contracts/washes.md](contracts/washes.md), [quickstart.md](quickstart.md)

**Tests**: required. The constitution's principle IV makes every behaviour change start with a failing test. In each
phase the test task comes before the code it drives, and it must be seen to fail before that code is written.

**Organization**: one phase per user story where it fits one builder's context. US1 is split across five phases, US2
across two, because each is too large for one. The planner that every story needs is the Foundational phase.

## Format: `[ID] [P?] [Story] Description`

- IDs stay stable across revisions. T094 to T098 were added by the analyze pass and sit in the phase where they
  run, so a phase's listed order, not the numeric order, is the order to run in.

- **[P]**: parallel with the other [P] tasks of its phase: it touches files no other open task touches
- **[Story]**: US1 to US4, from [spec.md](spec.md)
- Vitest runs from `src/KCC.Web` (`cd src/KCC.Web && yarn test <filter>`); TUnit suites run with
  `dotnet run --project <suite>.csproj`, never `dotnet test`. Every yarn command needs `FONTAWESOME_NPM_AUTH_TOKEN`.

---

## Phase 1: Setup

**Purpose**: confirm the gates and a green baseline before anything changes.

- [X] T001 Confirm gates 1 and 2 of `.specify/memory/constitution.md`: `FONTAWESOME_NPM_AUTH_TOKEN`, `KCC_E2E_MEMBER_USERNAME` and `KCC_E2E_MEMBER_PASSWORD` are set; the .NET SDK `global.json` names, Node 24, Yarn 1 and the Playwright browsers for `tests/KCC.E2ETests/KCC.E2ETests.csproj` are installed. Stop if one is missing
- [X] T002 Record the baseline on `005-ink-wash-bloom`: `yarn build:all`, `dotnet build KitchenCommandCenter.slnx -c Release` and `cd src/KCC.Web && yarn test`. Stop and report if any is red before this feature's first change
- [X] T003 Create `src/KCC.Web/Features/Bloom/`, `tests/KCC.ViteTests/Features/Bloom/` and `tests/KCC.UnitTests/Features/Bloom/`, and read `docs/brand/kit.md`, `specs/005-ink-wash-bloom/contracts/washes.md`, `specs/005-ink-wash-bloom/contracts/visit.md` and `specs/005-ink-wash-bloom/reference/artifact-wash.css`

---

## Phase 2: Foundational - the wash planner

**Purpose**: `planSheet`, the pure function every story draws on. It derives one sheet's washes from the visit seed
and the sheet's key (research R1, R7, R8; data-model "Wash").

**Blocks**: every later phase.

- [X] T004 Write failing tests in `tests/KCC.ViteTests/Features/Bloom/washes.test.ts` for determinism and format: the same `{ seed, key, size, chosen }` gives byte-identical output; every number is written to three decimals; each wash's id is `hash(key, k)` as 8 lower-case hex digits with no seed; the integer hash (`Math.imul` mixer) is pinned by known vectors; every field of data-model "Wash" is present with its unit (`deg`, `%`, `s`)
- [X] T005 Add failing tests to `tests/KCC.ViteTests/Features/Bloom/washes.test.ts` for counts and size (FR-011): `page` 4, `reading` 3, `card` 1 or 2 by the hash (both occur across seeds), `none` 0, never more than 4; an exported `sheetSize` resolves, in order, `bloom`, then `tear="hero"` to `page`, then `margin` to `reading`, then `wide` to `page`, else `card` (A32); `margin` with `wide` gives `reading`
- [X] T006 Add failing tests to `tests/KCC.ViteTests/Features/Bloom/washes.test.ts` for colour and strength (FR-014, FR-015, SC-005): `--wash-ink` and `--wash-ink-2` are always `var(--color-<w>)` for a `w` in `WASHES` from `src/KCC.Web/Features/Types/DesignSystem.ts`; wash `k` takes slot `k` and slot `k + 4` of a per-sheet shuffle; a `chosen` colour is wash 0's first ink; `--wash-peak` lies in [0.75, 1]; `--wash-fallback-at` lies in [0, 210); with no `starts` input, fallback starts are stratified (A34): with `n` washes and `p` from the key's hash, wash `k` starts in `[(k + p - 0.3) / n, (k + p + 0.3) / n] × 210s` clamped to [0, 210) (a clamp, never a wrap), so wash 0 is always the earliest and a sheet's own starts are at least `0.4 / n` of the window apart except where the clamp meets an edge; with a `starts` input of `n` times (a late sheet), the fallback times are exactly those times, written to three decimals, and wash 0 takes the earliest (FR-015)
- [X] T007 Add failing tests to `tests/KCC.ViteTests/Features/Bloom/washes.test.ts` for variety and cover (FR-009, FR-012, SC-003, SC-004): 20 seeds of one key give 20 distinct arrangements; on one seed, six or more distinct keys give six distinct arrangements; centres fall in their strata (one wash in the middle half, two in opposite halves of a hashed diagonal, three or four one quadrant each with washes 0 and 2, 1 and 3 in opposite quadrants); width 0.45 to 1.25, aspect 0.68 to 1.32, turn 0 to 360, radii 32% to 68%, ink offsets 28% to 72%
- [X] T008 Add failing tests to `tests/KCC.ViteTests/Features/Bloom/washes.test.ts` for two deep at most (FR-017): a TypeScript mirror of the kit's cap, `max(0, min(d * W, (hypot(dx * W, dy * H) - 2 * reach) / s))`, with `reach` the exported `WASH_REACH_PX`, keeps each pair's circles (radius `width * s / 2` plus `reach`) disjoint for 1,000 seeds on a 280x120 card, a 700x2400 reading sheet and a 1300x400 hero; an unpaired wash carries `--wash-dx: 9` and `--wash-dy: 9`
- [X] T009 Implement `planSheet` (with its optional `starts` input), `sheetSize`, the hash, the fixed-decimal writer, `WASH_WINDOW_S = 210`, `WASH_REACH_PX` and the `PlannedWash` type in `src/KCC.Web/Features/Bloom/washes.ts`, pure and free of Vue, until T004 to T008 pass
- [X] T010 Run `cd src/KCC.Web && yarn test washes && yarn type-check && yarn lint`

**Checkpoint**: the planner is green and imported by nothing yet.

---

## Phase 3: User Story 1 - the visit on the server (Priority: P1)

**Goal**: the server keeps one visit per browser in `kcc-visit`: seed, start, a 30-minute sliding expiry
([contracts/visit.md](contracts/visit.md), research R2, R3).

**Independent test**: the unit suite proves parse, format, new visit, renewal, the elapsed clamp and no write after
the response starts.

- [X] T011 [P] [US1] Write failing tests in `tests/KCC.UnitTests/Features/Bloom/VisitCookieTests.cs`: format writes `1.<8 lower-case hex>.<unix seconds>`; parse round-trips it; a wrong part count, a version other than `1`, bad hex, a start in the future, a start more than a year back and an empty value each return `null` without throwing
- [X] T012 [P] [US1] Write failing tests in `tests/KCC.UnitTests/Features/Bloom/VisitClockTests.cs` with a fixed `TimeProvider` subclass and a `DefaultHttpContext`: no cookie starts a visit with start now and writes `kcc-visit` with `Max-Age=1800`, `Path=/`, `SameSite=Lax`, `Secure` on HTTPS only and no `HttpOnly`; a live cookie keeps its seed and start and is renewed; a malformed one starts a visit; elapsed is clamped to [0, 300] seconds; a started response writes nothing and still returns the visit; a second `Resolve` on one request returns the memoised visit and writes one cookie
- [X] T013 [P] [US1] Write failing tests in `tests/KCC.UnitTests/Features/Bloom/BloomInputsTests.cs`: the page key is the request path lower-cased with no trailing slash, and `/` for the home page
- [X] T014 [US1] Create the `Visit` record (`Seed` as `uint`, `StartedAt`, derived `Elapsed`) in `src/KCC.Web/Features/Bloom/Visit.cs`
- [X] T015 [US1] Implement `VisitCookie` (parse and format) in `src/KCC.Web/Features/Bloom/VisitCookie.cs` until T011 passes
- [X] T016 [US1] Implement `VisitClock.Resolve(HttpContext, TimeProvider)` in `src/KCC.Web/Features/Bloom/VisitClock.cs`, seeding from `RandomNumberGenerator` and memoising in `HttpContext.Items`, until T012 passes. `TimeProvider` is already registered by `src/KCC.Web/Features/RecipeImport/RecipeImportComposer.cs`
- [X] T017 [US1] Implement `BloomInputs` (seed, page) and its page-key normaliser in `src/KCC.Web/Features/Bloom/BloomInputs.cs` until T013 passes
- [X] T018 [US1] Run `dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj` and `dotnet build KitchenCommandCenter.slnx -c Release` with no warning

**Checkpoint**: the visit resolves in C#; nothing calls it yet.

---

## Phase 4: User Story 1 - the arrival schedule, the bloom context and the heartbeat (Priority: P1)

**Goal**: arrivals spread across the page by hash priority (research R6), the render shares one context per app
(data-model "Bloom context"), and an open page keeps its visit alive (research R3).

**Independent test**: Vitest proves the schedule's guarantees, repeat-key suffixes, the lag and the heartbeat.

- [X] T019 [US1] Add failing tests to `tests/KCC.ViteTests/Features/Bloom/washes.test.ts` for `scheduleWashes(seed, washes)` (FR-002, FR-003, FR-015, FR-016, SC-002): every start in [0, 210); no two page starts closer than 0.7 of a page beat and none on the same beat; the longest empty stretch bounded relative to `210 / n` over 1,000 seeds; `B` is the smallest power of two at least `2n + 32`, at most 512; page beats within `0.75 * 210 / B` of any of the 16 visit beats are never claimed; shuffled input gives identical CSS; removing one sheet moves only the washes in its probe run; a global (`@`) key takes its `washSlots` beats and the same times on two different pages; within a sheet wash 0 is the earliest; the output is `.kcc-wash[data-wash="<id>"]{--wash-at:<t>s}` per wash
- [X] T020 [US1] Implement `scheduleWashes` (visit lattice, page lattice, priority claiming, chosen colour first) in `src/KCC.Web/Features/Bloom/washes.ts` until T019 passes
- [X] T021 [P] [US1] Write failing tests in `tests/KCC.ViteTests/Features/Bloom/bloomContext.test.ts`: `createBloom(seed, page)` gives `claimKey` returning a key, then `key~2` and `key~3` for repeats with a development warning (A27); non-`@` keys are qualified as `${page}|${key}` and `@` keys are not; `collect` records planned washes; `lag()` is 0 before `markHydrated()` and the seconds since page load after it; `lateStarts(n)` is `undefined` before `markHydrated()`, and after it returns the next `n` starts of the per-page sequence `frac(c + j / φ) × 210s` with no jitter, `c = frac(hash(seed, page) / 2^32)` (A34, owner ruling N1: spacing per wash); with an empty schedule, 20 late washes in any mix of sheet sizes start at least `210 × φ^-7 ≈ 7.233s` apart for each of 1,000 seeds; with the schedule of a 50-wash page, a candidate within `0.75 × 210 / B` of a scheduled start is skipped, at most 14 skips per page, and 20 late washes still start at least `210 × φ^-8 ≈ 4.470s` apart for each of 1,000 seeds (three-distance theorem, research R6; assert `>= 4.469` to allow for three-decimal rounding); where the skip budget allows, no late start lies within the block radius of a scheduled start; a fixture that blocks more than 14 candidates shows the 15th is taken anyway (FR-003, FR-016); with no provided context the defaults are seed 0, page `''`, lag 0 and no collection
- [X] T022 [P] [US1] Write failing tests in `tests/KCC.ViteTests/Features/Bloom/visitHeartbeat.test.ts` with fake timers and a stubbed `document.cookie`: every 60 seconds and on `pagehide` it rewrites `kcc-visit` with the same value, `max-age=1800`, `path=/`, `samesite=lax`, and `secure` on HTTPS; with no cookie it writes nothing; its stop function clears the timer and the listener
- [X] T023 [US1] Implement the injection key, `createBloom`, `useBloom`, `markHydrated` and `lateStarts` (which runs `scheduleWashes` over the washes hydration collected to know the scheduled starts) in `src/KCC.Web/Features/Bloom/bloomContext.ts` until T021 passes
- [X] T024 [US1] Implement `startVisitHeartbeat` in `src/KCC.Web/Features/Bloom/visitHeartbeat.ts` until T022 passes
- [X] T025 [US1] Run `cd src/KCC.Web && yarn test Bloom && yarn type-check && yarn lint`

**Checkpoint**: the TypeScript side of the bloom is complete and unwired.

---

## Phase 5: User Story 1 - the SSR path and the layout (Priority: P1)

**Goal**: every page resolves the visit, renders with its seed, writes the schedule in `<head>` and the elapsed time
on `<html>` (research R2, R4; [contracts/visit.md](contracts/visit.md) "Page markup" and "SSR sidecar").

**Independent test**: the integration suite shows a page setting `kcc-visit`, `--wash-elapsed` and
`#kcc-bloom-schedule`, and the unit suite shows two seeds never sharing a cached render.

- [X] T026 [P] [US1] Add failing tests to `tests/KCC.UnitTests/Features/Ssr/VueSsrServiceTests.cs`: the render request carries `bloom: { seed, page }`; the cache key differs for two seeds and for two pages with the same regions; a cache hit returns the cached `Schedule` with the markup
- [X] T027 [P] [US1] Add failing tests to `tests/KCC.UnitTests/Features/Ssr/SsrHtmlContentTests.cs`: the `server-content` JSON carries `bloom` with `seed` as an unsigned 32-bit integer and `page`
- [X] T028 [P] [US1] Write failing tests in `tests/KCC.IntegrationTests/Features/Pages/VisitTests.cs`: `GET /` with no cookie sets `kcc-visit` in the contract's format; `<html>` carries `--wash-elapsed` near `0.0s`; `<head>` holds `<style id="kcc-bloom-schedule">`; a live cookie keeps its seed in `server-content`; a start 120 seconds back renders `--wash-elapsed` within a second of `120.0s`; a start 900 seconds back renders `300.0s`
- [X] T029 [US1] Add `Schedule` to `SsrResult` in `src/KCC.Web/Features/Models/Common/SsrResult.cs` and to `SsrResponse`; take `BloomInputs` in `src/KCC.Web/Features/Ssr/VueSsrService.cs` and `src/KCC.Web/Features/Ssr/VueSsrExtensions.cs`, add it to the render request and `GenerateCacheKey`, and cache `Schedule` in `CachedRender`; write `bloom` into `server-content` in `src/KCC.Web/Features/Models/Common/SsrHtmlContent.cs`, until T026 and T027 pass
- [X] T030 [US1] In `src/KCC.Web/Features/Ssr/Server.Entry.ts`, have `createApp` provide `createBloom(seed, page)` with collection on and export `scheduleWashes`; in `src/KCC.Web/Features/Ssr/Server.js`, read `bloom` from `POST /render`, load `scheduleWashes` from the same SSR module as `createApp` (A30), run it over the collected washes after `renderToString` and return `schedule` beside `html` and `css`; a client-render fallback returns no schedule
- [X] T031 [US1] Add `bloom: { seed: number; page: string }` to `src/KCC.Web/Features/Types/ContentRegions.ts`; in `src/KCC.Web/Features/Main.ts`, provide `createBloom` from `server-content`'s `bloom`, call `markHydrated()` after `mount`, and start `startVisitHeartbeat()`
- [X] T032 [US1] In `src/KCC.Web/Features/Pages/Shared/Layout.cshtml`, inject `TimeProvider`; in the opening code block resolve the visit with `VisitClock.Resolve`, capture the header view component, `RenderBody()` and the footer view component, and call the SSR with `BloomInputs` before `<html>` is written; write `style="--wash-elapsed: <s>s"` (one decimal, invariant culture) on `<html>`, `<style id="kcc-bloom-schedule">` with the schedule in `<head>`, and the rendered app in `<body>` as today, until T028 passes
- [X] T033 [US1] Run `dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj`, `dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj`, `yarn build:all` and `cd src/KCC.Web && yarn test Ssr`

**Checkpoint**: the visit reaches every page; no sheet renders a field yet, so the schedule is empty.

---

## Phase 6: User Story 1 - KccWashes and KccSheet (Priority: P1)

**Goal**: the sheet primitive renders its wash field from the planner ([contracts/washes.md](contracts/washes.md)
"Markup", "`KccWashes`", "`KccSheet` props").

**Independent test**: the component tests prove the field, its byte-stable markup and its key stability.

**Note**: making `washKey` required and removing `at` turns `yarn type-check` and the call sites' component tests red
until Phase 8 ends. That is expected; do not patch call sites here.

- [X] T034 [P] [US1] Write failing tests in `tests/KCC.ViteTests/Features/Components/Sheet/KccWashes.test.ts`: the field is `<span class="kcc-washes" aria-hidden="true">` with one `.kcc-wash` per planned wash, each with `data-wash` and the contract's fixed-decimal inline style; two renders with the same seed, page and key give the same markup and a different seed gives different markup; `size="none"` renders nothing; `chosen` is wash 0's `--wash-ink`; with a provided context every wash is collected, with its `washSlots` on a global key; mounted after `markHydrated()` the field adds `--wash-lag` and plans with `starts` from `lateStarts(n)`, so its wash 0 has the earliest `--wash-fallback-at`; a conditional sibling sheet inserted before it leaves its markup unchanged; an `@pad` key renders the same markup on two pages
- [X] T035 [P] [US1] Update `tests/KCC.ViteTests/Features/Components/Sheet/KccSheet.test.ts` to fail first: `.kcc-washes` is the `.kcc-sheet`'s first child, inside its tear (FR-013); `at` is gone and no `kcc-wash` comes from it; `wash` becomes the chosen colour; without `bloom`, `tear="hero"` gives 4 washes, `margin` 3 (with or without `wide`), `wide` alone 4, else 1 or 2 (A32); `bloom="none"` renders no field; `washKey` and `washSlots` pass through
- [X] T095 [P] [US1] Write failing tests in `tests/KCC.ViteTests/Features/Bloom/firstPaint.test.ts` (FR-001, FR-006, SC-008): a small app of three `KccSheet`s rendered with `renderToString` under `createBloom(seed, page)` gives `.kcc-washes` fields whose inline styles carry no `--wash-at` and no `--wash-lag`; `scheduleWashes` over the collected washes gives one rule per rendered `data-wash`; hydrating that markup with `createSSRApp` and the same `bloom` input logs no hydration mismatch and leaves every `.kcc-washes` subtree's `outerHTML` byte-identical; the same app hydrated with a different seed does log a mismatch, proving the test can see one
- [X] T036 [US1] Create `src/KCC.Web/Features/Components/Sheet/KccWashes.Component.vue` (props `washKey`, `size`, `chosen`, `washSlots`; global through `src/KCC.Web/Features/GlobalComponents.ts`, so Razor can write `<KccWashes>`), planning through `planSheet`, claiming its key and collecting through `useBloom`, until T034 and T095 pass
- [X] T037 [US1] In `src/KCC.Web/Features/Components/Sheet/KccSheet.vue`, drop `at` and its wash style, keep `wash` as the chosen colour, add required `washKey`, `bloom` and `washSlots`, and render `KccWashes` as the sheet's first child with `sheetSize`, until T035 and T095 pass
- [X] T038 [US1] Run `cd src/KCC.Web && yarn test Components/Sheet Bloom`

**Checkpoint**: a sheet renders its field with no style yet.

---

## Phase 7: User Story 1 - the bloom in the kit CSS (Priority: P1)

**Goal**: CSS draws and animates what the render gives it: the clipped field, the capped wash, the 90-second bloom
delayed by the visit, and the strength cap that keeps text legible (research R8 to R10, R14, R15).

**Independent test**: `motion.test.ts`, `contrast.test.ts`, `paperRule.test.ts` and `kitClasses.test.ts` pass.

- [X] T039 [P] [US1] Add failing assertions to `tests/KCC.ViteTests/Features/Styles/motion.test.ts`: the bloom on `.kcc-wash` (`kcc-bloom`, `var(--wash-bloom)`) is the one non-loop animation over 500ms; `--wash-bloom` in `src/KCC.Web/Features/Styles/Torn/Tokens.css` plus `WASH_WINDOW_S` is 300 seconds; `@keyframes kcc-bloom` animates `opacity` and `scale` only; the ramp-switch freeze in `src/KCC.Web/Features/Styles/Torn/Kit.css` excludes `.kcc-wash`
- [X] T040 [P] [US1] Rewrite the wash model in `tests/KCC.ViteTests/Features/Styles/contrast.test.ts` to fail first (FR-017, FR-018, SC-006): read `--wash-strength` instead of `--wash-op`; model the strongest point of a two-wash stack (each wash's first ink at the rim alpha and second at the core alpha, times the strength, times the 1.24 grain gain, source-over in the field, then multiply onto paper in light and screen in dark); for every ordered choice of the eight washes in both layers and inks, assert ink and ink-soft at 4.5:1 and hair-strong at 3:1 in both ramps; the `it.fails` hair-strong case becomes a plain `it`; remove the single-wash ring, core and dark core floor cases; leave the well cases, `softInWell` included, unchanged
- [X] T041 [P] [US1] Add failing assertions to `tests/KCC.ViteTests/Features/Styles/paperRule.test.ts`: `--wash-reach` equals `WASH_REACH_PX` and twice the `stdDeviation` of the `#kcc-bloom` bleed in `src/KCC.Web/Features/Pages/Shared/Layout.cshtml`; `#kcc-wax` is gone and `#kcc-wax-flat` stays
- [X] T094 [P] [US1] Write failing tests in `tests/KCC.ViteTests/Features/Styles/washField.test.ts` over `src/KCC.Web/Features/Styles/Torn/Kit.css` (FR-001, FR-006, FR-007, FR-013, A23): `.kcc-washes` declares `position: absolute`, `inset: 0`, `overflow: clip` (never `hidden`), `container-type: size` and `pointer-events: none`; `.kcc-wash` declares `opacity: 0` at rest, an animation with fill mode `both`, the delay `calc(var(--wash-at, var(--wash-fallback-at)) - var(--wash-elapsed, 0s) - var(--wash-lag, 0s))`, and no `will-change`; `.kcc-wash`'s filter reads `url(#kcc-bloom)`; `.kcc-tile`'s filter still reads `url(#kcc-wax-flat)`
- [X] T042 [P] [US1] Add `--wash-op` to the retired patterns in `tests/KCC.ViteTests/support/retired.ts` so `tests/KCC.ViteTests/Features/Styles/retiredTokens.test.ts` fails while `Tokens.css` declares it
- [X] T043 [US1] In `src/KCC.Web/Features/Styles/Torn/Tokens.css`, remove `--wash-op` and add `--wash-strength` in both ramps, set to the largest value T040 passes with, `--wash-bloom: 90s` and `--wash-reach`
- [X] T044 [US1] In `src/KCC.Web/Features/Styles/Torn/Kit.css` (`@layer components`), replace the author-placed `.kcc-wash` with `.kcc-washes` (absolute, `inset: 0`, `z-index: 0`, `overflow: clip`, `container-type: size`, `pointer-events: none`, `mix-blend-mode: var(--wash-blend)`) and the contract's `.kcc-wash` (centre, pair-capped width, `aspect-ratio`, two radial gradients through `oklch(from var(--wash-ink) l c h / a)` with the rim alpha lift, `filter: url(#kcc-bloom) saturate(var(--wash-sat))`, `opacity: 0`, the bloom with delay `calc(var(--wash-at, var(--wash-fallback-at)) - var(--wash-elapsed, 0s) - var(--wash-lag, 0s))`); add `@keyframes kcc-bloom`; change the lift to `.kcc-sheet > :where(:not(.kcc-washes))`; place the field over the first scrollport of a `.kcc-sheet--scroll` (A21); exclude `.kcc-wash` from the ramp-switch freeze; take easing and gradients from `specs/005-ink-wash-bloom/reference/artifact-wash.css`; continue until T039 and T094 pass
- [X] T045 [US1] In `src/KCC.Web/Features/Pages/Shared/Layout.cshtml`, replace the `#kcc-wax` filter with `#kcc-bloom` (wax displacement, Gaussian bleed, grain composite with `k1 = k2 = .62`) and keep `#kcc-wax-flat`, until T041 passes
- [X] T046 [US1] Update the `kcc-wash` row and add a `kcc-washes` row in the Classes table of `docs/brand/kit.md`, so `tests/KCC.ViteTests/Features/Styles/kitClasses.test.ts` passes; the rest of the rewrite is T082
- [X] T047 [US1] Run `cd src/KCC.Web && yarn test Styles Bloom Components/Sheet`; the Phase 7 tests (T039 to T042, T094) are green, and only the call sites' type errors and component tests may stay red until Phase 8

**Checkpoint**: a `KccSheet` blooms on the visit clock.

---

## Phase 8: User Story 2 - every KccSheet call site (Priority: P1)

**Goal**: every `KccSheet` carries a stable key, author placement is gone, and a Home card left bare stays bare
(FR-009, FR-010, FR-015; A5, A12, A26, A28).

**Independent test**: `keys.test.ts`, the widget tests and every component test pass, and `yarn type-check` is clean
again.

- [X] T048 [US2] Write failing tests in `tests/KCC.ViteTests/Features/Bloom/keys.test.ts`: every `KccSheet`, `KccWashes` and `kccwashes` tag in a `.vue` or `.cshtml` file under `src/KCC.Web/Features` has a `wash-key` or `:wash-key`; no bound key reads an index alias of an enclosing `v-for` or a Razor loop counter; no `KccWashes` sits inside a `.kcc-blank`, `.pad-page`, well, tile, label or tape
- [X] T049 [P] [US2] Add the `:at=` prop to the retired patterns in `tests/KCC.ViteTests/support/retired.ts`
- [X] T050 [P] [US2] Update `tests/KCC.ViteTests/Features/Widgets/Card.test.ts`, `tests/KCC.ViteTests/Features/Widgets/Stacker.test.ts` and `tests/KCC.ViteTests/Features/Widgets/SmallHero.test.ts` to fail first: an unset backoffice wash renders no `.kcc-washes`; a set wash is wash 0's `--wash-ink`; no assertion on `at` placement remains
- [X] T051 [P] [US2] Add failing tests to `tests/KCC.UnitTests/Features/Pages/Home/HomeSectionsTests.cs`: `RichTextSection`, `StackerSection`, `HomeCard` and `StackerCard` carry `Key`, the Block List item's content key (A28)
- [X] T052 [P] [US2] Update the component tests that pin a wash or `at` to fail first, asserting the chosen colour on wash 0 and a `.kcc-washes` field instead: `tests/KCC.ViteTests/Features/Components/ComingSoon/ComingSoonSection.test.ts`, `tests/KCC.ViteTests/Features/Components/RecipeDetail/VariantsEmptyState.test.ts`, `tests/KCC.ViteTests/Features/Components/RecipeSearch/RecipeSearchHeader.test.ts`, `tests/KCC.ViteTests/Features/Components/RecipeSearch/RecipesEmptyState.test.ts`, `tests/KCC.ViteTests/Features/Components/RecipeWizard/SubmittedSheet.test.ts`, `tests/KCC.ViteTests/Features/Components/VariantDetail/VariantCookNotes.test.ts`, `VariantIngredients.test.ts`, `VariantInstructions.test.ts`, `VariantNutrition.test.ts`, `VariantReviews.test.ts` in the same folder, `tests/KCC.ViteTests/Features/Pages/Account/AccountView.test.ts`, `tests/KCC.ViteTests/Features/Pages/AddVariant/AddVariantView.test.ts`, `tests/KCC.ViteTests/Features/Pages/CreateRecipe/CreateRecipeView.test.ts`, `tests/KCC.ViteTests/Features/Pages/RecipeDetail/RecipeDetailView.test.ts`, `tests/KCC.ViteTests/Features/Pages/RecipeSearch/RecipeSearchView.test.ts`. Add failing size cases (FR-011, A32): each review item in `VariantReviews.test.ts` and each recipe group in `AccountView.test.ts` carries 1 or 2 washes; the empty states in `RecipesEmptyState.test.ts` and `VariantsEmptyState.test.ts` carry 3; two reviews with the same rating render different arrangements, keyed by author and date (A33); after a reload that puts a new review first, every review that was already there keeps its `data-wash` ids and inline styles (FR-007)
- [X] T053 [US2] Add `Key` to the records in `src/KCC.Web/Features/Pages/Home/HomeSection.cs`, fill it from each block's content key in `src/KCC.Web/Features/Pages/Home/HomeSections.cs`; in `src/KCC.Web/Features/Pages/Home/Blocks/CardGridBlock.cshtml` pass `wash-key="home-card:@card.Key"`; in `src/KCC.Web/Features/Widgets/Card/Card.Component.vue` take `washKey`, drop `:at` and pass `bloom="none"` when `washOf(cardColor)` is undefined; in `src/KCC.Web/Features/Widgets/Stacker/Stacker.Component.vue` add `key` to `StackerCard`, use it for the `v-for` key and `wash-key="stacker-card:<key>"`, drop `:at` and pass `bloom="none"` when the wash is unset; in `src/KCC.Web/Features/Widgets/Hero/SmallHero.Component.vue` drop `:at` and add `wash-key="small-hero"`; until T050 and T051 pass
- [X] T054 [US2] Drop `:at` and add a fixed `wash-key` (in a list, the item's own id) in `src/KCC.Web/Features/Components/ComingSoon/ComingSoonSection.vue`, `src/KCC.Web/Features/Components/RecipeDetail/VariantsEmptyState.vue`, `src/KCC.Web/Features/Components/RecipeSearch/RecipeFilters.vue`, `RecipeSearchHeader.vue`, `RecipesEmptyState.vue`, and `src/KCC.Web/Features/Components/VariantDetail/VariantCookNotes.vue`, `VariantIngredients.vue`, `VariantInstructions.vue`, `VariantNutrition.vue`, `VariantReviews.vue`. In `VariantReviews.vue` each review item takes `:wash-key="`review:${review.authorName}|${review.created}`"` (A33) and `bloom="card"`, and its `v-for` `:key` becomes the same string, so the `load(0)` reload after a post or a delete (`submit` and `remove`) keeps each existing review's component, and with it its washes, instead of reusing an index's component under a new key (FR-007). `RecipesEmptyState.vue` and `VariantsEmptyState.vue` take `bloom="reading"` (A32). Every other `wide` sheet here keeps the size rule: `margin` ones read as `reading`, and `RecipeSearchHeader.vue` as `page`. Continue until T052's size cases pass
- [X] T055 [US2] Drop `:at` and add a fixed `wash-key` in `src/KCC.Web/Features/Components/RecipeWizard/BasicsStep.vue`, `IngredientsStep.vue`, `InstructionsStep.vue`, `LeaveConfirm.vue`, `MatchStep.vue`, `PhotoStep.vue`, `PhotoViewer.vue`, `ReadingState.vue`, `ReviewStep.vue`, `StartStep.vue`, `SubmittedSheet.vue`, `VariantStep.vue`
- [X] T056 [US2] Drop `:at` and add a fixed `wash-key` in `src/KCC.Web/Features/Pages/Account/AccountView.Component.vue`, `src/KCC.Web/Features/Pages/Account/Login/LoginView.Component.vue`, `src/KCC.Web/Features/Pages/Account/Settings/AccountSettingsView.Component.vue`, `src/KCC.Web/Features/Pages/RecipeDetail/RecipeDetailView.Component.vue`, `src/KCC.Web/Features/Pages/VariantDetail/CookMode.vue`, `src/KCC.Web/Features/Pages/VariantDetail/VariantDetailView.Component.vue`. In `AccountView.Component.vue` each recipe group takes `:wash-key="`account-group:${group.key}`"` and `bloom="card"` (A32)
- [X] T096 [P] [US2] Update `tests/KCC.ViteTests/Features/Components/Header/PadCards.test.ts` to fail first: each of the five pad cards' sheets is keyed `@` plus its `id` (`@pad-card-recipes`, `@pad-card-kitchen`, `@pad-card-search`, `@pad-card-find`, `@pad-card-menu`), and no `at` reaches it
- [X] T097 [US2] In `src/KCC.Web/Features/Components/Header/PadCard.vue`, drop the `at` prop and pass `wash-key` `@` plus its `id` to its `KccSheet`; in `src/KCC.Web/Features/Components/Header/AppHeader.Component.vue`, drop every `PadCard`'s `:at`; continue until T096 passes. The `.pad-top` field and the slots stay in Phase 9
- [X] T057 [US2] Run `cd src/KCC.Web && yarn test && yarn type-check && yarn lint` and `dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj`; type-check and every Vitest file are green, `keys.test.ts` and `retiredTokens.test.ts` included

**Checkpoint**: every `KccSheet` blooms on a stable key.

---

## Phase 9: User Story 2 - hand-written sheets, the pad and the Razor sheets (Priority: P1)

**Goal**: the sheets that do not use `KccSheet` carry a field too, with the sizes and chosen colours of A29, and the
pad blooms on its own beats on every page (A7, A25).

**Independent test**: the component tests, `keys.test.ts` and the integration suite pass; the schedule holds one rule
per rendered wash.

- [X] T058 [P] [US2] Update `tests/KCC.ViteTests/Features/Components/Recipe/DetailHero.test.ts`, `FeaturedRecipeCard.test.ts`, `RecipeCard.test.ts`, `RecipeCardRow.test.ts` and `tests/KCC.ViteTests/Features/Components/RecipeDetail/VariantGrid.test.ts` to fail first: `.kcc-washes` is the sheet's first child; recipe surfaces' wash 0 wears `washFor(seed)`; no hand-written `.kcc-wash` with `--c` remains; counts follow A29
- [X] T059 [P] [US2] Update `tests/KCC.ViteTests/Features/Components/Header/AppHeader.test.ts` and `PadCards.test.ts` to fail first: the `.pad-top` sheet carries a field keyed `@pad` with fixed `wash-slots`; each pad card passes its own fixed `wash-slots`, disjoint from the pad's and from each other's; `.pad-page` and every blank carry none; every pad sheet's markup is identical with and without `nav.member` and on two pages
- [X] T060 [P] [US2] Extend `tests/KCC.IntegrationTests/Features/Pages/VisitTests.cs` to fail first: the home and error pages render `.kcc-washes` fields, `#kcc-bloom-schedule` holds one rule per rendered `data-wash`, and a Home card whose backoffice wash is unset has no field
- [X] T061 [US2] In `src/KCC.Web/Features/Components/Recipe/DetailHero.vue`, `FeaturedRecipeCard.vue`, `RecipeCard.vue`, `RecipeCardRow.vue` and `src/KCC.Web/Features/Components/RecipeDetail/VariantGrid.vue`, remove the hand-written `kcc-wash` and add `KccWashes` as the sheet's first child, keyed `recipe-hero`, `featured-recipe:<recipe key>`, `recipe-card:<recipe key>`, `recipe-card-row:<recipe key>` and `variant-grid-add`, sized and coloured per A29, until T058 passes
- [X] T062 [US2] In `src/KCC.Web/Features/Components/Header/AppHeader.Component.vue`, add `KccWashes` with `wash-key="@pad"`, `size="page"` and four fixed `wash-slots` to the `.pad-top` sheet, and give each of the five `PadCard`s two fixed slots of its own (14 of the 16 visit beats in all); in `src/KCC.Web/Features/Components/Header/PadCard.vue`, take `washSlots` and pass it to its `KccSheet`; until T059 passes
- [X] T063 [US2] Replace the hand-written `kcc-wash` with `<KccWashes>` as the sheet's first child in `src/KCC.Web/Features/Pages/Error/Index.cshtml` (`wash-key="error"`, `size="reading"`, `chosen="red"`) and `src/KCC.Web/Features/Pages/Account/RegistrationComplete/Index.cshtml` (`registration-complete`, `reading`, `green`); add one to `src/KCC.Web/Features/Pages/Home/Blocks/RichTextBlock.cshtml` (`home-richtext:@Model.Key`, `reading`) and `src/KCC.Web/Features/Pages/Home/Blocks/StackerBlock.cshtml` (`home-stacker:@Model.Key`, `reading`); until T060 passes
- [X] T064 [US2] Run `cd src/KCC.Web && yarn test && yarn type-check && yarn lint`, `yarn build:all` and `dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj`; every Vitest file, `keys.test.ts` and `retiredTokens.test.ts` included, is green

**Checkpoint**: US1 and US2 are complete in markup, CSS and the server.

---

## Phase 10: User Stories 1 and 2 - in the browser (Priority: P1)

**Goal**: prove the bloom where only a browser can: first paint, timing, hydration, navigation, sign-in, the ramp
switch, overflow and the arrangement (A31: tests write a back-dated `kcc-visit` before the first navigation).

**Independent test**: the three new E2E classes pass.

- [X] T065 [P] [US1] Write `tests/KCC.E2ETests/Features/Chrome/BloomTests.cs`: a fresh context shows every `.kcc-wash` at opacity 0 (SC-001, FR-001); at 300 seconds every bloom animation has finished at its peak and none starts after 210 seconds (SC-002, FR-007); at 120 seconds the earned washes are in place before hydration and no `.kcc-wash` computed style changes once the app mounts (SC-008, FR-004); with JavaScript disabled at 120 seconds the earned washes still show (FR-006); the same page twice keeps its arrangement, a new cookie changes it, and a navigation keeps the seed (FR-008, US1 scenarios 5 and 6); signing in at about 60 seconds keeps every existing sheet's `data-wash` and inline style (A12, A25); switching the ramp mid-bloom keeps every animation's `currentTime` (FR-018)
- [X] T066 [P] [US2] Write `tests/KCC.E2ETests/Features/Chrome/BloomArrangementTests.cs`: on the library page at 300 seconds no two fields share an arrangement and none holds more than four washes (SC-004); every wash's computed ink matches one of the eight brand wash colours exactly in both ramps (SC-005); neighbouring sheets' `--wash-at` values differ (FR-016); a recipe card's wash 0 wears its tile's colour (FR-015)
- [X] T067 [P] [US1] Write `tests/KCC.E2ETests/Features/Chrome/WashOverflowTests.cs`: on the library, a recipe, a variant page and an open phone pad card at 375px, at 120 and 300 seconds, desktop and phone, the document's and every `.kcc-sheet--scroll`'s `scrollWidth` and `scrollHeight` are equal with washes and with `.kcc-washes { display: none }` injected (A23, A21)
- [X] T068 [US1] Run `dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj` and fix what T065 to T067 find in `src/KCC.Web/Features/Styles/Torn/Kit.css`, `src/KCC.Web/Features/Components/Sheet/KccWashes.Component.vue` or `src/KCC.Web/Features/Pages/Shared/Layout.cshtml`, test first for any behaviour that changes

**Checkpoint**: the MVP, US1 and US2, is shippable.

---

## Phase 11: User Story 3 - grain only on the desk, the washes and the tiles (Priority: P2)

**Goal**: the crayon joins the desk grain under every sheet, the sheet grain goes, and a static scan and a pixel check
hold the rule (research R12; A18, A20).

**Independent test**: `grain.test.ts` and `GrainTests` pass in both ramps; the desk looks as it does on `main`.

- [X] T069 [P] [US3] Write failing tests in `tests/KCC.ViteTests/Features/Styles/grain.test.ts` over every `.css`, `.vue` and `.cshtml` file in `src/KCC.Web/Features`, inline `style` attributes included (FR-020, FR-021, FR-023, FR-024): grain sources are `--grain` and `--crayon`, each matched as a whole name so `--grain-op` (`Torn/Tokens.css`, dark block) is not one, then `url(#kcc-bloom)`, `url(#kcc-wax-flat)`, any `feTurbulence`, `url("data:image/svg`, `repeating-linear-gradient` and `repeating-radial-gradient`; `--grain` and `--crayon` are declared only in `Torn/Tokens.css`'s `:root` and the filters only in `Pages/Shared/Layout.cshtml`; `var()` aliases are resolved; a rule that reads a source or an alias selects only `.kcc-grain`, `.kcc-crayon` (and their dark forms), `.kcc-wash` or `.kcc-tile`; `.kcc-grain` and `.kcc-crayon` are the first two children in `src/KCC.Web/Features/App.vue` and declare `z-index: 0`. The tape's woven stripe is exempt by name (owner ruling, session "analyze"): `.kcc-tilewrap > .kcc-tape` and its `:root[data-theme='dark']` form may use `repeating-linear-gradient`, and the test proves the exemption is by name by running the scanner over fixture CSS, in which the same gradient on `.kcc-field`, on a new `.kcc-tape--wide` rule and on `.kcc-sheet` each fail, and a whole-name `--grain-op` declaration does not count as a source
- [X] T070 [P] [US3] Replace "R9: the workbench grain" in `tests/KCC.ViteTests/Features/Styles/paperRule.test.ts` with failing assertions: no `.kcc-sheet::after` grain rule; a `.kcc-sheet--scroll` background holds the ruling layers only; no `--sheet-grain*` token in either ramp
- [X] T071 [P] [US3] Add `--sheet-grain`, `--sheet-grain-op`, `--sheet-grain-blend` and `url(#kcc-wax)` (not `#kcc-wax-flat`) to the retired patterns in `tests/KCC.ViteTests/support/retired.ts`
- [X] T072 [P] [US3] Write `tests/KCC.E2ETests/Features/Chrome/GrainTests.cs` (SC-007, FR-022), in both ramps at 0 seconds of visit time: a 40x16px patch of bare paper between two rules in a sheet's padding has a luminance standard deviation under 0.5 levels; the same patch on the desk is over it; no element with a grain background or filter paints above a `.kcc-sheet`
- [X] T073 [US3] Move `.kcc-crayon` up beside `.kcc-grain` as `#app`'s first two children in `src/KCC.Web/Features/App.vue`
- [X] T074 [US3] In `src/KCC.Web/Features/Styles/Torn/Kit.css`, give `.kcc-crayon` `z-index: 0` under every sheet, keeping its multiply in light and its dark form; remove the `.kcc-sheet::after` grain and the `.kcc-sheet--scroll` grain layer, keeping the ruling layers; in `src/KCC.Web/Features/Styles/Torn/Tokens.css`, remove `--sheet-grain`, `--sheet-grain-op` and `--sheet-grain-blend` from both ramps
- [X] T075 [US3] Correct the `#app` comment in `src/KCC.Web/Features/Styles/Main.css` to describe both desk layers under the page, and drop the crayon headroom from the hair-strong comment in `src/KCC.Web/Features/Styles/TailwindConfig.css` (the value stays)
- [X] T076 [US3] Run `cd src/KCC.Web && yarn test Styles` and `dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj`; `contrast.test.ts` stays green

**Checkpoint**: US3 holds independently in both ramps.

---

## Phase 12: User Story 4 - readers who ask for less motion (Priority: P3)

**Goal**: under reduced motion each wash fades in at its final size on the same schedule (FR-019; A17).

**Independent test**: `motion.test.ts` and the reduced-motion case of `BloomTests` pass.

- [X] T077 [P] [US4] Add failing assertions to `tests/KCC.ViteTests/Features/Styles/motion.test.ts`: under `prefers-reduced-motion: reduce`, `.kcc-wash` runs `kcc-bloom-fade` with the same duration and delay; `@keyframes kcc-bloom-fade` animates `opacity` only; the reduced-motion rule in `src/KCC.Web/Features/Styles/Main.css` excludes `.kcc-wash`
- [X] T078 [P] [US4] Add failing reduced-motion cases to `tests/KCC.E2ETests/Features/Chrome/BloomTests.cs`: with reduced motion emulated at 60 seconds, every arriving wash's computed `scale` stays `1`; at 300 seconds every wash is at its peak, as for every other reader
- [X] T079 [US4] Add `@keyframes kcc-bloom-fade` and the reduced-motion swap to `src/KCC.Web/Features/Styles/Torn/Kit.css`
- [X] T080 [US4] Exclude `.kcc-wash` from the `!important` reduced-motion rule in `src/KCC.Web/Features/Styles/Main.css`
- [X] T081 [US4] Run `cd src/KCC.Web && yarn test motion` and `dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj`

**Checkpoint**: every user story is complete.

---

## Phase 13: Definition of done

**Purpose**: the brand rewrite (FR-025, D10) and every row of the constitution's definition of done.

- [X] T082 [P] Rewrite `docs/brand/kit.md`: Files; kit-only properties; the Paper rule's grain paragraph (grain on the desk, the washes and the tiles only, and the tape's woven stripe as the tape's own material, not grain); the Structure example and rules 3 and 5; Classes (`kcc-grain`, `kcc-crayon`, `kcc-sheet`, `kcc-sheet--scroll`, `kcc-washes`, `kcc-wash`); Motion (the bloom row, rules 2 and 4, the ramp-switch rule); Vue primitives (`KccSheet`, `KccWashes`); Razor (the filter defs, the schedule, `KccWashes`); Invariant 6; Enforcement (`grain.test.ts`, `washes.test.ts`, `keys.test.ts`, `washField.test.ts`, `firstPaint.test.ts`, `BloomTests`, `BloomArrangementTests`, `GrainTests`, `WashOverflowTests`); the authoring rules from `specs/005-ink-wash-bloom/contracts/washes.md`
- [X] T083 [P] Rewrite `docs/brand/loose-leaf.md`: Washes (the bloom, up to four, two deep at most, under running text, the AA cap, a Home card left unset stays bare); the Wax bullet (grain on the desk, washes and tiles only); Motion (the bloom); Applied patterns (Library, Detail); Don't (drop "one wash" and "text on a wash core", add "no grain on paper"); a Decisions entry dated 2026-10-21
- [X] T084 [P] Correct every line on wash placement, "one wash" and the sheet grain, and add the `wash-key` rule, in `.claude/agents/brand-steward.md`, `.claude/agents/kit-builder.md` and `.claude/skills/loose-leaf/SKILL.md`
- [X] T085 D10: search `README.md`, `CLAUDE.md`, `docs/` and `.claude/` for `--wash-op`, `sheet-grain`, `#kcc-wax` (not `-flat`), `:at`, "one wash" and "running text", and correct each line the change made untrue; finished records under `docs/replatform/` stay as written
- [X] T086 D1: `dotnet build KitchenCommandCenter.slnx -c Release` with no warning
- [X] T087 D2: `yarn build:all` at the repository root
- [X] T088 D3: in `src/KCC.Web`, `yarn type-check`, `yarn lint` and `yarn format`; keep what lint and format change
- [X] T089 D4: `node tests/scripts/run.mjs` at the repository root, with a no-op `open` first on `PATH`; all six suites green
- [X] T090 D5: confirm `git diff main --stat` touches no `src/KCC.Web/Features/Torn/tears.ts`, `Tears.css`, `src/KCC.Web/uSync/`, `src/KCC.Web/Features/Models/Generated` or migration, so the row reports N/A
- [X] T091 D6: run the site (https://localhost:58671) and walk quickstart steps 3 to 8 in the Browser pane, in the light and the dark ramp, at 120 and 300 seconds of visit time: home, library, a recipe, a variant, account, the error page and an open phone pad card; the desk matches `main`
- [X] T092 SC-009: quickstart step 9 on the library page, mobile emulation and 4x CPU throttle, at 150 and 300 seconds; record each run's dropped-frame count for the pull request, and if either stutters apply research R11's levers in order, test first
- [X] T098 Constitution V: walk quickstart step 10 on the home page, `/recipes` and one variant page, cold (a fresh visit per request) and warm (one cookie jar), and record each page's median `renderTime` and the cold-minus-warm cost in the pull request, beside the CPU it ran on
- [X] T093 D7: the `brand-steward` agent reviews the change and reports no violation

---

## Dependencies & Execution Order

### Phase dependencies

- Phase 1 then Phase 2, which blocks everything after it.
- Phases 3 and 4 depend on Phase 2 only and may run in either order; Phase 5 needs both.
- Phase 6 needs Phase 4 (bloom context). Phase 7 needs Phase 6. Phases 6 and 7 may run before Phase 5 is done, but
  Phase 5 must be done before Phase 8.
- Phase 8 needs Phase 7; type-check and the call sites' tests are red from Phase 6 until Phase 8 ends.
- Phase 9 needs Phase 8. Phase 10 needs Phase 9.
- Phase 11 (US3) needs Phase 7, because `grain.test.ts` allows `url(#kcc-bloom)` only on `.kcc-wash`. It does not need
  Phases 8 to 10.
- Phase 12 (US4) needs Phase 10 (`BloomTests.cs`).
- Phase 13 needs every other phase.

### User story dependencies

- **US1 and US2 (P1)** share the planner and the sheet primitive. US2's call sites need US1's CSS and context; US1's
  browser proof needs US2's keyed call sites. They ship together as the MVP.
- **US3 (P2)** is independent in behaviour; it sits after Phase 7 only so its scan sees the final filter names.
- **US4 (P3)** builds on US1's bloom.

### Within each phase

The failing test task first, then the code that makes it pass, then the phase's run task.

## Parallel Opportunities

- Phase 3: T011, T012, T013 (three new test files).
- Phase 4: T021 and T022; T019 and T020 share `washes.ts` and `washes.test.ts`, so they run in sequence.
- Phase 5: T026, T027, T028.
- Phase 6: T034, T035, T095.
- Phase 7: T039, T040, T041, T042, T094.
- Phase 8: T049, T050, T051, T052, T096.
- Phase 9: T058, T059, T060.
- Phase 10: T065, T066, T067.
- Phase 11: T069, T070, T071, T072.
- Phase 12: T077, T078.
- Phase 13: T082, T083, T084.
- Across phases: Phase 11 may run beside Phases 8 to 10 in its own checkout, since it touches `App.vue`, the grain
  rules and tokens, and its own tests; `Kit.css` and `Tokens.css` are shared, so merge with care.

## Parallel Example: Phase 7

```text
Task: "T039 motion.test.ts bloom assertions"
Task: "T040 contrast.test.ts two-wash model"
Task: "T041 paperRule.test.ts --wash-reach and #kcc-bloom"
Task: "T042 retired.ts --wash-op"
Task: "T094 washField.test.ts clip, size container, opacity 0, filters"
```

## Implementation Strategy

### MVP

Phases 1 to 10 deliver US1 and US2: a site that starts bare and blooms pseudo-randomly over five minutes. Stop at the
Phase 10 checkpoint to validate quickstart steps 3 to 6a.

### Incremental delivery

1. Phases 1 and 2: the planner.
2. Phases 3 to 7: the visit reaches the page and a `KccSheet` blooms.
3. Phases 8 to 10: every sheet blooms; the browser proves it.
4. Phase 11: the grain rule.
5. Phase 12: reduced motion.
6. Phase 13: the brand documents and the definition of done. One pull request, through card implement.

## Coverage

| Requirement | Tasks |
|---|---|
| FR-001 | T094, T095, T065 |
| FR-002 | T019, T020, T039, T044, T065 |
| FR-003 | T006, T019, T020, T021 |
| FR-004 | T028, T032, T044, T065 |
| FR-005 | T012, T016, T022, T024 |
| FR-006 | T028, T060, T094, T095, T032, T044, T065 |
| FR-007 | T039, T094, T044, T052, T054, T065 |
| FR-008 | T012, T016, T065 |
| FR-009 | T007, T021, T034, T048, T052, T096, T065 |
| FR-010 | T035, T048, T050, T053, T059, T060 |
| FR-011 | T005, T035, T052, T054, T056 |
| FR-012 | T007, T009 |
| FR-013 | T035, T094, T044 |
| FR-014 | T006, T066 |
| FR-015 | T006, T019, T050, T053, T058, T066 |
| FR-016 | T006, T019, T021, T066 |
| FR-017 | T008, T040, T043, T044 |
| FR-018 | T040, T065 |
| FR-019 | T077, T078, T079, T080 |
| FR-020 | T069, T074, T094 |
| FR-021 | T069, T070, T072, T074 |
| FR-022 | T072, T073, T074, T091 |
| FR-023 | T069, T094 |
| FR-024 | T069 |
| FR-025 | T046, T082, T083, T084, T085 |
| SC-001 | T094, T095, T065 |
| SC-002 | T019, T065 |
| SC-003 | T007, T065 |
| SC-004 | T007, T066 |
| SC-005 | T006, T066 |
| SC-006 | T040, T043 |
| SC-007 | T069, T072 |
| SC-008 | T095, T065 |
| SC-009 | T092 |
| Constitution V (SSR cost) | T098 |
