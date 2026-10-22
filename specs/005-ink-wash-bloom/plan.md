# Implementation Plan: Ink Wash Bloom and the Grain Rule

**Branch**: `005-ink-wash-bloom` | **Date**: 2026-10-21 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/005-ink-wash-bloom/spec.md`

Red team: 2026-10-21, 5 found, 5 fixed, 0 not real

## Summary

Washes stop being author-placed. Every top sheet, except a Home card the owner left bare, carries a field of one to
four washes. A pure TypeScript function derives each wash's size, shape, place, colours and strength from a per-visit
seed and the sheet's stable key. Washes pair up so that no point lies under more than two. A post-render schedule
spreads arrivals across the page by a hash priority, so the result never depends on render order. The server keeps
the visit in a cookie: seed and start time, a 30-minute sliding expiry, renewed by a heartbeat while a page is open.
It passes the seed into the Vue SSR render, writes the schedule in `<head>` and the elapsed visit time on `<html>` as
`--wash-elapsed`. CSS animates each wash with a 90s bloom whose delay is its arrival minus the elapsed time, so the
first paint already shows what the visit has earned and the bloom runs on with no script. The field clips its washes
and never changes the page's size. Grain moves under the paper: the crayon joins the desk grain beneath every sheet,
the sheet grain goes, and only the desk, washes and tiles keep a texture. The brand documents, the kit agents and the
`loose-leaf` skill are rewritten in the same change.

## Technical Context

**Language/Version**: C# on .NET 10; TypeScript 5 and Vue 3.5; CSS with Tailwind 4

**Primary Dependencies**: Umbraco 17 (Razor layout), the Node SSR sidecar (`Features/Ssr`), Vite. No new package

**Storage**: none. One cookie, `kcc-visit` ([contracts/visit.md](contracts/visit.md))

**Testing**: Vitest (`tests/KCC.ViteTests`, run from `src/KCC.Web`), TUnit unit and integration, Playwright E2E
through TUnit; `node tests/scripts/run.mjs` for all six

**Target Platform**: evergreen Chrome, Safari and Firefox on desktop and phone. Needs `@property`, `mod()`, `hypot()`,
container size queries and units, relative colour syntax, `overflow: clip` and individual `scale`: all three engines
ship them. No `sibling-index()` and no `random()`

**Project Type**: server-rendered web site (ASP.NET Core + Umbraco, Vue SSR with hydration)

**Performance Goals**: smooth scroll on the library page under mobile emulation and 4× CPU throttle, both mid-bloom
(150s, about 21 washes animating) and at full bloom (SC-009); one SSR render per page view

**Constraints**: first paint correct with no script (FR-006); no hydration mismatch (byte-identical inline styles,
arrival times outside the Vue render); washes at most two deep, with ink and ink-soft at 4.5:1 and hair-strong at 3:1
over two in both ramps (FR-017); the field never changes layout or scroll ranges; no runtime JS that draws (kit
invariant 6)

**Scale/Scope**: about 45 sheet surfaces (33 files that use `KccSheet`, 6 hand-written Vue sheets, 4 Razor sheets);
up to about 50 washes on a library page

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle / gate | Check | Result |
|---|---|---|
| I. Content lives in the backoffice | No copy in code. The Card block's `wash` field keeps its meaning, its description ("Unset leaves the card bare paper") and its schema (FR-015); no uSync or ModelsBuilder change | Pass |
| II. Loose Leaf is the only look | Kit classes and tokens only, in `@layer components`; both ramps checked; `contrast.test.ts` proves FR-017; enforcement tests stay green and `grain.test.ts` joins them; `retiredTokens` `ALLOWLIST` stays empty. Washes are planned in the render and scheduled after it, and drawn by CSS; the client script only renews a cookie and offsets late-mounted sheets, it draws nothing | Pass |
| III. SQLite has one writer | No database write | N/A |
| IV. Tests first, builds clean | Each behaviour starts with a failing test (list below); `yarn build:all`; warnings as errors; no generated file changes (`tears.ts` untouched) | Pass |
| V. Cheap to run | No service, no recurring cost (A8). SSR renders per visitor instead of from a shared 60s cache. The CPU cost on the Pi is measured by quickstart step 10 and task T098, and recorded in the pull request | Pass |
| VI. Members earn trust | No endpoint, no write, no secret. The cookie is cosmetic and identifies no one (A3) | Pass |
| VII. Code explains itself | `--wash-op`, `at`, the sheet grain and `#kcc-wax` go in one cutover; `docs/brand/*`, `.claude/agents/brand-steward.md`, `.claude/agents/kit-builder.md` and `.claude/skills/loose-leaf/SKILL.md` corrected in the same change | Pass |
| Gate 4: no open NEEDS CLARIFICATION | None in spec or plan | Pass |
| Gate 5: ordering rule | One spec, one pull request; no earlier phase | Pass |

**Post-design re-check**: the design adds one Razor-to-SSR input (the seed), one SSR output (the schedule), one root
custom property, and moves the SSR call to the layout's opening code block. It changes two kit motion rules (the
500ms ceiling and the reduced-motion stilling) by naming the bloom as their one exception; those rules live in
`docs/brand/kit.md`, and the spec's settled rulings (a 90s bloom, a fade under reduced motion) require it. No principle
is broken. Pass.

## Design

### Server: the visit and the schedule

- `Features/Bloom/VisitCookie.cs`: parse and format `1.<seed hex>.<start>`; malformed → `null`.
- `Features/Bloom/VisitClock.cs`: `Resolve(HttpContext, TimeProvider)` reads the cookie, starts a visit when it is
  missing or malformed, writes the cookie with `Max-Age` 1800 unless the response has started, memoises the visit in
  `HttpContext.Items`, returns `Visit` (seed, start, elapsed clamped to 300s).
- `Pages/Shared/Layout.cshtml`: resolves the visit and calls the SSR in its opening code block (header view
  component, `RenderBody()`, footer view component captured first); writes `<style id="kcc-bloom-schedule">` in
  `<head>` and `style="--wash-elapsed: <s>s"` on `<html>`; replaces the `#kcc-wax` filter def with `#kcc-bloom`;
  keeps `#kcc-wax-flat`.
- `Ssr/VueSsrService.cs`, `Models/Common/SsrResult`, `SsrHtmlContent.cs`: carry `bloom` in the render request, the
  cache key and the `server-content` JSON, and `Schedule` in the result and the cache.

### Render: the washes

- `Features/Bloom/washes.ts`: `planSheet({ seed, key, size, chosen })` → `PlannedWash[]` (strata, pairs, geometry,
  colours, strength, `--wash-fallback-at`); `scheduleWashes(seed, washes)` → CSS (the visit and page lattices,
  hash-priority claiming, chosen colour first); `WASH_WINDOW_S = 210`; the counts per size. Pure, no Vue.
- `Features/Bloom/bloomContext.ts`: the injection key, `createBloom(seed, page)` with `claimKey`, `collect`, `lag()`
  and `lateStarts(n)` (the per-page golden-ratio starts for washes mounted after hydration, clear of scheduled
  starts, research R6 step 6); `markHydrated()`.
- `Features/Bloom/visitHeartbeat.ts`: renews `kcc-visit` every 60s and on `pagehide`.
- `Ssr/Server.js`, `Ssr/Server.Entry.ts`, `Main.ts`, `Types/ContentRegions.ts`: `bloom` in the payload; `createApp`
  and the client provide `createBloom(...)`; the sidecar returns `schedule`; `Main.ts` calls `markHydrated()` after
  `mount` and starts the heartbeat.
- `Components/Sheet/KccWashes.Component.vue`: injects the context, plans the sheet, renders the field with
  fixed-decimal inline styles and `data-wash`; collects for the schedule; when set up after hydration, adds
  `--wash-lag` and plans with `lateStarts(n)` in place of the key-hash fallback (A34); renders nothing for `size="none"`.
- `Components/Sheet/KccSheet.vue`: drops `at`; `wash` becomes the chosen colour; adds required `washKey`, `bloom`
  and `washSlots`; renders `KccWashes` as the sheet's first child.

### CSS

- `Styles/Torn/Kit.css`: `.kcc-washes` (clip, size container, blend), the new `.kcc-wash` with the pair cap,
  `kcc-bloom` and `kcc-bloom-fade` keyframes, the reduced-motion swap, the lift rule on `:not(.kcc-washes)`, the
  ramp-switch freeze excluding `.kcc-wash`; `.kcc-crayon` at `z-index: 0`; `.kcc-sheet::after` and the
  `.kcc-sheet--scroll` grain layer removed.
- `Styles/Torn/Tokens.css`: `--wash-strength` (both ramps), `--wash-bloom: 90s`, `--wash-reach`; `--wash-op` and the
  three `--sheet-grain*` tokens removed.
- `Styles/Main.css`: the reduced-motion rule excludes `.kcc-wash`; the `#app` comment describes both desk layers
  under the page.
- `App.vue`: `.kcc-crayon` moves up beside `.kcc-grain`.
- `Styles/TailwindConfig.css`: the hair-strong comment loses the crayon headroom it no longer needs (the value stays).

### Call sites

Remove every `:at` and hand-written `kcc-wash`; keep each `wash` as the chosen colour (A5); give every sheet a
`wash-key` (A12). `Widgets/Card/Card.Component.vue` and `Widgets/Stacker/Stacker.Component.vue` pass `bloom="none"`
when `washOf(...)` is undefined (FR-010). Add `KccWashes` to `AppHeader.Component.vue` (`.pad-top`, `@pad`, fixed
slots), `PadCard.vue` (`@` plus its `id`, such as `@pad-card-recipes`, fixed slots), `DetailHero.vue`, `FeaturedRecipeCard.vue`, `RecipeCard.vue`,
`RecipeCardRow.vue`, `VariantGrid.vue`, and the Razor sheets in `Pages/Error/Index.cshtml`,
`Pages/Account/RegistrationComplete/Index.cshtml`, `Pages/Home/Blocks/RichTextBlock.cshtml`,
`Pages/Home/Blocks/StackerBlock.cshtml`.

### Tests first

Each fails before its change:

| Test | Proves |
|---|---|
| `tests/KCC.ViteTests/Features/Bloom/washes.test.ts` (new) | determinism; counts by size, never over 4, none for `none`; colours only from `WASHES`; chosen colour on the earliest arrival; 20 seeds give 20 arrangements; distinct keys differ; strata cover the sheet; pair partners disjoint under the TS mirror of the CSS cap for every seed across card, reading-sheet and hero sizes; schedule in `[0, 210)`, minimum gap 0.7 of a page beat, bounded longest stretch, a set function (order of input irrelevant), a removed sheet moves only its probe run, global washes' times the same on every page |
| `tests/KCC.ViteTests/Features/Bloom/bloomContext.test.ts`, `visitHeartbeat.test.ts` (new) | repeat keys suffixed; lag after hydration only; late washes spaced per wash, at least `210 × φ^-8 ≈ 4.470s` apart for 20 over 1,000 seeds and clear of scheduled starts within the skip budget; heartbeat copies the value with a fresh `max-age`, never creates one |
| `tests/KCC.ViteTests/Features/Bloom/firstPaint.test.ts` (new) | SSR markup carries the fields with no `--wash-at` or `--wash-lag`; the schedule has one rule per `data-wash`; hydration with the same seed changes no field and logs no mismatch (FR-001, FR-006, SC-008) |
| `tests/KCC.ViteTests/Features/Styles/washField.test.ts` (new) | `.kcc-washes` clips (`overflow: clip`), is a size container and takes no pointer; `.kcc-wash` is at `opacity: 0` at rest with fill `both` and the visit delay; `.kcc-wash` reads `url(#kcc-bloom)` and `.kcc-tile` keeps `url(#kcc-wax-flat)` |
| `tests/KCC.ViteTests/Features/Components/Sheet/KccWashes.test.ts` (new), `KccSheet.test.ts` | the field as first child; fixed-decimal styles and `data-wash`; same markup on two renders with the same seed; inserting a conditional sheet before it leaves its markup unchanged; `at` gone; `none` renders no field |
| `tests/KCC.ViteTests/Features/Bloom/keys.test.ts` (new) | every `KccSheet` and `KccWashes` in `Features/**` has a `wash-key`; none uses a list index |
| `tests/KCC.ViteTests/Features/Widgets/Card*.test.ts`, `Stacker*.test.ts` | an unset wash renders no field |
| `tests/KCC.ViteTests/Features/Styles/contrast.test.ts` | ink and ink-soft at 4.5:1 and hair-strong at 3:1 over a two-wash stack, both ramps; the `it.fails` hair-strong case (lines 279-284) becomes a plain `it`; reads `--wash-strength` |
| `tests/KCC.ViteTests/Features/Styles/motion.test.ts` | the bloom is the one non-loop over 500ms; 210 + 90 = 300; reduced-motion and ramp-switch rules exclude `.kcc-wash`; `kcc-bloom-fade` animates opacity only |
| `tests/KCC.ViteTests/Features/Styles/grain.test.ts` (new) | FR-024: sources declared only in their homes; aliases resolved; texture images counted; uses only on the four selectors; desk layers first and at `z-index: 0` |
| `paperRule.test.ts`, `kitClasses.test.ts`, `retiredTokens.test.ts`, the component tests that pin washes | the sheet grain gone; `--wash-reach` twice the filter's `stdDeviation`; the Classes table; `--wash-op`, `--sheet-grain*` and the `at` prop retired; washes now on every sheet |
| `tests/KCC.UnitTests/Features/Bloom/VisitCookieTests.cs`, `VisitClockTests.cs` (new) | parse, format, malformed, new visit, renewal, elapsed clamp, no write after the response starts |
| `tests/KCC.UnitTests/Features/Ssr/VueSsrServiceTests.cs`, `SsrHtmlContentTests.cs` | cache key separates seeds and pages; the schedule is cached with the markup; payload and `server-content` carry `bloom` |
| `tests/KCC.IntegrationTests/Features/Pages/VisitTests.cs` (new) | a page sets `kcc-visit`, `--wash-elapsed` and `#kcc-bloom-schedule`; a live cookie keeps its seed; an aged start shows in the elapsed time |
| `tests/KCC.E2ETests/Features/Chrome/BloomTests.cs` (new) | SC-001, SC-002 and SC-008 through `document.getAnimations()`; reduced motion keeps `scale: 1`; signing in mid-visit keeps every existing sheet's arrangement |
| `tests/KCC.E2ETests/Features/Chrome/GrainTests.cs` (new) | bare-paper pixel variance under the threshold and desk variance over it, both ramps; desk layers paint under sheets |
| `tests/KCC.E2ETests/Features/Chrome/WashOverflowTests.cs` (new) | document and scroll-container `scrollWidth` and `scrollHeight` equal with and without washes, at 120s and 300s, desktop and phone |

### Verification beyond the suites

- SC-009, twice: the quickstart's step 9 at 150s (mid-bloom) and at 300s, on the library page, mobile emulation, 4×
  CPU throttle; the dropped-frame counts go in the pull request.
- D6 and D7: both ramps in the Browser pane mid-bloom and at full bloom, then the `brand-steward` review.

### Documents (FR-025, D10)

`docs/brand/loose-leaf.md`: Washes (the bloom, up to four, two deep at most, under text, the AA cap, a Home card left
unset stays bare), Wax bullet (grain on desk, washes and tiles only), Motion (the bloom), Applied patterns (Library,
Detail), Don't (drop "one wash", "text on a wash core"; add "no grain on paper"), a dated Decisions entry.
`docs/brand/kit.md`: Files, kit-only properties, Paper rule's grain paragraph, Structure example and rules 3 and 5,
Classes (`kcc-grain`/`kcc-crayon`, `kcc-sheet`, `kcc-sheet--scroll`, `kcc-washes`, `kcc-wash`), Motion (the bloom
row; rules 2 and 4; the ramp-switch rule), Vue primitives (`KccSheet`, `KccWashes`), Razor (filter defs, the
schedule, `KccWashes`), Invariant 6, Enforcement (`grain.test.ts`, `washes.test.ts`, `keys.test.ts`, the new E2E).
`.claude/agents/brand-steward.md`, `.claude/agents/kit-builder.md`, `.claude/skills/loose-leaf/SKILL.md`: every line
on wash placement, "one wash" and the sheet grain, and the `wash-key` rule.

## Project Structure

### Documentation (this feature)

```text
specs/005-ink-wash-bloom/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── visit.md
│   └── washes.md
├── reference/artifact-wash.css
├── checklists/requirements.md
└── tasks.md              # /speckit-tasks
```

### Source Code (repository root)

```text
src/KCC.Web/Features/
├── Bloom/                         # new: VisitCookie.cs, VisitClock.cs, Visit.cs, BloomInputs.cs,
│                                  #      washes.ts, bloomContext.ts, visitHeartbeat.ts
├── Components/Sheet/              # KccSheet.vue, KccWashes.Component.vue (new)
├── Components/{Header,Recipe,RecipeDetail,RecipeSearch,VariantDetail,RecipeWizard,ComingSoon}/  # call sites
├── Widgets/{Card,Stacker,Hero}/   # call sites
├── Pages/Shared/Layout.cshtml     # visit, SSR before <head>, schedule, --wash-elapsed, #kcc-bloom
├── Pages/{Error,Account/RegistrationComplete,Home/Blocks}/  # Razor sheets
├── Ssr/                           # VueSsrService.cs, Server.js, Server.Entry.ts
├── Models/Common/                 # SsrResult, SsrHtmlContent.cs
├── Styles/                        # Main.css, TailwindConfig.css, Torn/Kit.css, Torn/Tokens.css
├── Types/ContentRegions.ts
├── App.vue
└── Main.ts

tests/
├── KCC.ViteTests/Features/{Bloom,Components/Sheet,Widgets,Styles}/
├── KCC.UnitTests/Features/{Bloom,Ssr}/
├── KCC.IntegrationTests/Features/Pages/
└── KCC.E2ETests/Features/Chrome/

docs/brand/{loose-leaf.md,kit.md}
.claude/agents/{brand-steward.md,kit-builder.md}
.claude/skills/loose-leaf/SKILL.md
```

**Structure Decision**: one feature folder, `Features/Bloom`, holds the visit (C#) and the wash planning and
schedule (TS), as `Features/Ssr` already mixes C#, TS and JS. The rendering stays with the sheet primitive in
`Components/Sheet`, and the drawing stays in the kit CSS.

## Complexity Tracking

No constitution violation to justify.
