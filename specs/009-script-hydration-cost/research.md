# Research: Performance Sweep, Phase 2 - Script Weight, Hydration and Interactivity

Two investigations fed this plan, both on `c1827268` (`main` when this phase started): a byte attribution of the
client bundle with a scratch islands build, and a set of throttled-phone probes with ablations for FR-003. The probe
script, the ablation runner, the trace slicer and a summary of every run are in `research/probes/`.

## Method

- **Site.** A fresh SQLite Release site in the Testing environment, SSR on, seeded with the 36 recipes of
  `RecipeTestDataSeeder`, booted with the environment `SiteProcess` uses, after `pnpm build:all`.
- **Browser.** Chromium 156.0.8078.4 from the Playwright 1.64.1 driver the E2E suite pins, on SwiftShader with the
  phone profile of `Profiles.Phone`: 390x844 at DPR 3, Fast 4G (9 Mbps, 170 ms), 4x CPU, a bloomed visit (400 s) with
  seed `0x0badf00d`. The probe repeats the harness's order (`Collectors.ScrollAsync`, then `HoverAsync`): settle,
  scroll to the top, wait 1.5 s, move the pointer to the middle of the page, wheel down and back in 100 px steps every
  66 ms, then hover up to six `.kcc-slip--lift` cards.
- **Readings.** Every Event Timing entry over 16 ms with its input delay (`processingStart - startTime`), processing
  time and presentation delay; long-animation-frame entries with their scripts; rAF frame deltas; and, in traced runs,
  a Chromium trace with the harness's categories plus `v8.execute`, `blink.user_timing` and `loading`.
- **Ablations.** A stylesheet injected at `DOMContentLoaded`: washes hidden (`.kcc-washes{display:none}`), wash masks
  off, the field's blend off, the fall's filter off, transitions off; and two profile changes: reduced motion, and
  Metal instead of SwiftShader.
- One run per variant, so a single number is a reading, not a median. The probe's rAF frame share is not comparable
  with the harness's `frames_within_16_7_pct` (it reads 53-70% where the harness reads 95-99%); only worst frames,
  long-frame counts and Event Timing durations are used below.

## F1. What the 117 KB script is

`pnpm build:all` at `c1827268`: one chunk, `main-*.js`, 374 KB raw, 119.5 KB gzipped (Vite's figure; the harness's
level-9 figure is 117,266 B). Attribution from its source map, gzipped per group:

| Part | Raw | Gzipped alone |
|---|---|---|
| Vue runtime (`@vue/runtime-core`, `runtime-dom`, `reactivity`, `shared`) | 112.8 KB | 44.6 KB |
| Vue template compiler (`@vue/compiler-core`, `compiler-dom`) | 60.7 KB | 20.8 KB |
| Unmapped (bundler glue, the compiler's tables) | 52.2 KB | 12.3 KB |
| Recipe wizard (`Components/RecipeWizard`) | 47.7 KB | 13.1 KB |
| Header (`Components/Header`) | 18.7 KB | 6.4 KB |
| Account pages, recipe cards, variant detail, forms, search, the rest | 82 KB | 33 KB |

Every page loads all of it. `App.vue` compiles the header, body and footer as templates in the browser, which is why
the compiler ships, and `SsrHtmlContent` sends those templates a second time inside `#server-content`.

Only the header's components (`AppHeader.Component.vue`, `PadCard`, `PadLines`, `SearchCard`, `SearchForm`,
`KitchenCard`, `RecipesCard`) carry `<style>` blocks: today's `main-*.css`, 6.86 KB raw, 2.07 KB gzipped.

## F2. What an islands split weighs

A scratch Vite build reused `vite.config.ts` with `vue` aliased to the runtime-only build, `__VUE_OPTIONS_API__`
off (every component is `<script setup>` or template-only), and one entry per interactive global component, each
hydrating it with `createSSRApp(component, props)`. Gzip level 9 of each page's static import closure:

| Page | Islands on load | First-load script | Over 60 KB? |
|---|---|---|---|
| error, status-404 | header | 41.4 KB | no |
| home | header, stacker | 42.5 KB | no |
| sign-in | header, login | 44.5 KB | no |
| account | header, account | 44.1 KB | no |
| settings | header, settings | 47.6 KB | no |
| recipe | header, recipe detail | 52.2 KB | no |
| search-1, search-3 | header, search | 52.9 KB | no |
| variant-cook | header, variant detail | 57.7 KB | no, 2.3 KB under |
| create-recipe | header, create recipe | 62.9 KB | **yes** |
| add-variant | header, add variant | 63.1 KB | **yes** |

The shared chunk (Vue's runtime, the bloom context, `washes.ts`, `KccSheet`) is 31.8 KB; the header island 7.9 KB;
the whole wizard 16.4 KB. The two wizard pages fit once the steps a page does not show on load are split off
(D13); cook mode gives variant detail room (D14). The bootstrap that finds and hydrates islands adds about 1 KB.

With `build.cssCodeSplit` on, every component style block still lands in one CSS file, the header island's, with the
same content hash as today's `main-*.css`. `cssCodeSplit: false` refuses a CSS file among the inputs, so it would move
`Main.css` and is not used.

## F3. What makes up the first-input cost on the throttled phone (FR-003)

**The first input is not the slow one.** Moving the pointer onto the page answered within 16 ms on every page probed
(no Event Timing entry over the threshold). The 136-248 ms entries Phase 1 attributed to "the first input" are the
pointer boundary events (`pointerover`, `pointerout`, `pointerenter`, `mouseover`) that fire when the first wheel steps
move content under a still pointer, 350-530 ms after the pointer arrived, and the hover lifts that follow:

| Page | Worst entry | When | Input delay | Processing | Presentation delay |
|---|---|---|---|---|---|
| home | 232-248 ms | 2nd wheel step, the frame the header tucks | 0-1 ms | 0 ms | 232-248 ms |
| search-1 | 144-248 ms | 1st and 2nd wheel steps; 176 ms on a hover lift | 0 ms | 0 ms | all of it |
| search-3 | 248 ms scroll, 200 ms hover | as search-1 | 0 ms | 0 ms | all of it |

**No script runs in it.** No long animation frame after usable carries a script on any page; input delay and
handler time are zero. Hydration finished 1.12-1.15 s after navigation, seconds before. So splitting the script and
hydrating less does not move this number.

**The trace splits the presentation delay in two** (`research/probes/window.mjs` over the traced runs):

1. **Style recalc on the main thread, 25-52 ms.** `UpdateLayoutTree` over 296 elements (home) to 519-889 elements
   (search), at 4x CPU. The tuck writes `--pad-offset` on `<main>`, an inherited custom property, so every element in
   `<main>` restyles; the hover change restyles the card's group.
2. **The commit waits on the GPU process, 130-200 ms.** The next `ProxyMain::BeginMainFrame` sits in
   `LayerTreeHost::WaitForCommitCompletion` for 132-199 ms while `CrGpuMain` runs one `GPUTask` of 130-190 ms: the
   software GPU rasterising the sheets the scroll exposed, the hover lifted or the tuck moved.

**The washes are most of the GPU's share.** Worst entry by ablation (one run each):

| Variant | home | search-1 | search-3 |
|---|---|---|---|
| As shipped | 232-248 ms | 248 ms | 248 ms |
| Washes hidden | 48 ms | 96 ms | 112 ms |
| Wash masks off | 80 ms | 168 ms | 136 ms |
| Wash blend off | 232 ms | | |
| Fall filter off | 200 ms | | |
| Transitions off | 232 ms | | |
| Reduced motion (no tuck, no lift) | 0 ms | 136 ms (hover 0) | 48 ms (hover 0) |
| Metal GPU | 40 ms | 168 ms (80 traced) | |

A wash is two masks composited (`mask-image: var(--wash-chalk), var(--wash-blob)` with `mask-composite: intersect`)
over two gradients, rotated and scaled. The trace does not break the `GPUTask` down further; the ablations put most
of it on the masks, which SwiftShader composites on the CPU for every wash in a sheet the frame invalidated or moved. On Metal the same frames take 40-80 ms, of which the style
recalc is most.

**Conclusion.** The fixed cost is rendering, not script: a page-wide restyle from the tuck's inherited property, then
the software GPU re-rasterising masked washes in the frames where the header tucks, a card lifts or the first scroll
exposes new sheets. The fix is render work in those three moments (D18), and Q1 holds what Phase 2 may do if no
look-preserving change reaches 100 ms.

## F4. What makes up the long frames on recipe search, home and sign-in (FR-003)

| Page | Long frames (worst) | Washes hidden | Masks off | Reduced motion | Metal |
|---|---|---|---|---|---|
| search-3 | 5 (183 ms) | 0 (50 ms) | 3 (83 ms) | 0 (33 ms) | |
| search-1 | 5 (233 ms) | 3 (167 ms) | 6 (167 ms) | 3 (167 ms) | 1 (150 ms) |
| home | 3 (217 ms) | 2 (133 ms) | 2 (83 ms) | 1 (50 ms) | 0 (33 ms) |
| sign-in | 0 (17 ms) | 0 | 0 | 0 | |

**Re-check at `c1827268` (T004).** `before.json` holds sign-in's phone Chromium `long_frames` at 0 in all six cases
(light and dark, bare and bloomed, images off, reduced motion): gone at `c1827268`, so no trace was taken and T054 is
N/A.

The long frames are the same frames as F3's slow entries: the tuck, the hover lifts and the first raster of newly
exposed sheets, each paying the restyle and the masked-wash raster. Search-3's go to zero with the washes hidden or
with reduced motion, which matches Phase 1's finding that the reduced-motion page had none.

**Not reproduced: sign-in's 17-24 long frames.** Phase 1 recorded them at `2ac3fff5`, before the tuck moved
`--pad-offset` off `:root`. On `c1827268` the probe's sign-in scroll had none. The first implementation task re-runs
the whole matrix on `main` (FR-001's "before") and either confirms that they are gone or traces them with the same
probe before the fix is chosen.

## F5. Layout shift while search appends

`after-phase1.json` reads `cls_scroll` 0.2356 on every search-3 case, both engines' Chromium cases, every ramp and
visit. The harness reaches three pages by jumping to the bottom (`LoadThreePagesAsync`), so the footer is on screen
when each page of cards lands above it and pushes it down. FR-008 makes the score zero (D19).

**The shifting nodes (T057).** `AppendShiftTests` records each `layout-shift` entry's `sources` while it reaches three
pages the way `LoadThreePagesAsync` does (2026-11-18, Release site, Chromium, under the CPU lock, machine load average
4-5 with the sibling session's VM busy). Before D19, the footer is the only node that moves while pages append:

| Case | Entries while appending |
|---|---|
| desktop 1920x1080, bare and bloomed | footer 960>984 (0.0014, the loading line), footer 984>off screen (0.04), footer 960>off screen (0.0514) |
| phone 375x812, bare and bloomed | footer 692>716 (0.0044, the loading line), footer 716>off screen (0.1182), footer 692>off screen (0.1478) |

WebKit records no `layout-shift` entries; its stand-in is `cls_geometry`. Every case also records one load-time entry
of 0.000001-0.00001 on a branch and before it alike: Font Awesome's duotone `::before` glyphs grow 1-2 px when the
icon font arrives, after first paint and before any scroll. It rounds to 0 in `cls`, and D19 leaves it.

**After D19.** The browser reserves the next page's rows in the loading line once the page is usable, sized from the
server's total and the grid's columns, and the results region keeps its height through each landing. Space is added or
given back only inside a frame and only while the edge it moves is below the screen, and no page loads until its space
is in place. A first try applied the space between frames, and a jump to the bottom in the same frame moved the footer
(phone 0.1478, one run in five): Chromium compares a frame with the last one painted. Measured in frames, the four
append cases record no entry after fonts are ready in 6 runs of each.

**A reader resting on the footer.** The first build of D19 held the next page back until its space was in place, so
a reader who stopped at the very bottom, footer on screen, got no more results until scrolling up a little: infinite
scroll stalled. A second build waited 1.5 s for a resting reader and then loaded the page as it always had, so that
landing moved the footer. Both are superseded by "Loading at once on the footer" below.

**A first page shorter than the screen.** At 1920x2200 the footer is on screen when the page is usable (top 2080), and
no scroll position hides the end of the results, so the first append loads as it always has and pushes the footer
off. The first build added the next page's space in the same frame as the landing, so the footer was seen to move by
more than the screen's height: 0.0545 in 3 of 3 runs. Space added after a landing now waits for the landing's frame to
be painted, and the case reads 0.01676 (0.000004 of it the icon-font entry), the same as before D19 measured by the
same test at `c1827268` (3 of 3 runs each, 2026-11-18, under the CPU lock, load average 14-28 with Spotlight indexing).
The test pins that number and 0.1 as ceilings. Reaching 0 there needs the server to draw the band, which C3 rules out:
this residual goes to the owner. At 1920x4000 the footer sits at the viewport's foot below the free space and every
page fits above it: no shift.

**Loading at once on the footer (2026-11-18).** The 1.5 s wait and a parked reader's zero shift cannot both hold
with a load that serves a resting reader at once. Decided by the orchestrator under the 2026-11-16 clarify answer
(infinite scroll behaves as it does today): the page loads at once, and FR-008 is amended for a reader parked on the
footer, whose landing shifts no more than before. A load now waits only for the frame that tries to place the space;
where it cannot be placed, the page loads at once and its space follows the painted landing. `AppendShiftTests`
(Release site, Chromium, under the CPU lock):

| Case | Before the space was reserved | Now |
|---|---|---|
| parked on the footer, 1920x1080, bare and bloomed | 0.093 (0.0014 + 0.04 + 0.0514) | 0.0134 in 10 of 10 runs, the footer 960>off screen once |
| parked on the footer, 375x812, bare and bloomed | 0.27 (0.0044 + 0.1182 + 0.1478) | 0 in 2 of 10 runs, 0.1478 in 8, the footer 692>off screen once |
| first page shorter than the screen, 1920x2200 | 0.01676 | 0.01676 in 5 of 5 |
| resting reader: next request after the last response | waited 1.5 s | 16-31 ms in 12 of 12 runs |

The append cases pin before's sums, 0.0928 and 0.2704, as ceilings; the short page keeps 0.01676.

**T061 on the branch (2026-11-18, `pnpm perf --check --page search-1,search-3`, 44 cases, HEAD 4e55e632 with the
Phase 2 build, under the CPU lock, no other build, test or browser load running).**
`cls` and `cls_scroll` against `after-phase1.json`, Chromium, every ramp and visit (bare, bloomed, mid, images off,
motion reduced):

| Case | Before (Phase 1) | Now |
|---|---|---|
| search-1 phone, `cls` | 0.0001 | 0.0001 on most cases, 0 on light bare |
| search-1 phone, `cls_scroll` | 0 | 0 |
| search-1 desktop, `cls` and `cls_scroll` | 0 | 0 |
| search-3 phone, `cls` | 0.0001 | 0.0001 |
| search-3 phone, `cls_scroll` | 0.2356 | 0.1422 |
| search-3 desktop, `cls` | 0 | 0 |
| search-3 desktop, `cls_scroll` | 0.1324-0.1484 | 0.0214 |

WebKit records no `layout-shift` entries; `cls_geometry` reads as before on all 16 search cases (phone 0, desktop
0.0669 or 0). The harness's search-3 jumps to the bottom, so it is the reader parked on the footer of amended FR-008:
the score is no worse than before on every case and the check still reports the `cls_scroll` target of 0 as a fail,
which the amendment accepts. Search-1 `cls_scroll` is 0 on all 20 Chromium cases.
The 0.0001 `cls` on phone is the icon-font entry (`fa-plate-utensils`, 0.000055, at the foot of the screen) that
search read at `c1827268` too; see F13.

`perf:diff before after` on search: `look-3-search-1.html` (20 cases) and `look-3-search-3.html` (12), nothing else.
`perf:scripts-off after` on search-1 reads 36 px (desktop Chromium), 6028 (desktop WebKit) and 2377 (phone Chromium)
of header residue, and a FAIL on the phone WebKit case, 7915 px with a largest channel delta of 205. The before run
at `c1827268` (`perf:scripts-off before`) reads the same four cases: 18431 px, 62, 2208 and the same FAIL at 409 px
and delta 205. The residues and the failure are present before 009; the failing pixels are the sort pill's edge,
and the pixel counts differ between the runs (not attributed).

**The resting reader's stall was the test's.** One run in three stopped at 36 cards for 5 s. The seeded set is 36
recipes, three pages: the test polled every 50 ms for the second page, and with loading at once the third page follows
the second within about 30 ms, so a poll could see 36 at once and then wait for a fourth page that does not exist.
Holding the poll 1 s after the scroll reproduced it in 3 of 3 runs. The test now scrolls once, reads the footer in the
same script as the scroll, and waits for all 36 cards with no further scroll.

**The band at the end.** The space is placed only by the browser after mount; `ReservedSpace.test.ts` pins the
server's HTML with no `min-height` and no `style` on the results region or the loading line, so a scripts-off page
never shows it. It is at most one page of results: `nextPageHeight` sizes it from the rows the next page fills,
`min(page size, total - loaded)` cards, so a short last page reserves only its own rows. When the last page lands,
and when a new search has no further pages, no `min-height` is left on the results region or the loading line. A last
page that landed while the end of the results was on screen kept the region's held height until the next scroll, the
same height the cards then filled; a hold the results already fill now goes in the frame after the landing, wherever
the end is, because removing it moves nothing.

## F6. Time to usable

The probe's `kcc:hydrated` mark read 1,124-1,153 ms on the phone, under FR-007's 2.0 s. The split shortens it by
the compiler's parse and the templates' compile; the guard holds it under target and under baseline (D16).

## F7. Trace probe counts (T010)

`DeterministicGuardTests` ran on three fresh boots of the branch's Release site (2026-11-17, under the CPU lock), and
a fourth boot wrote the CI baselines (T009). All four read the same counts on every phone Chromium case, light and
dark alike, with no tolerance:

| Page | `restyled_on_tuck` | `restyled_on_hover` | `wash_raster_on_interaction` |
|---|---|---|---|
| home | 296 | gap | 33 |
| search-3 | 1,302 | 0 | 60 |
| recipe | 212 | 0 | 14 |
| variant-cook | 352 | gap | 26 |
| account | 87 | gap | 8 |

Two choices made the counts repeat:

- **Restyles.** A running transition restyles its elements on every frame, and how many frames fall inside the trace
  varies by boot, so the raw sum of `UpdateLayoutTree` element counts moved by 1-65 between boots. A recalc counts
  only when an invalidation other than an animation precedes it.
- **Wash raster.** This Chromium emits no paint invalidation tracking events, so the probe counts `Paint` events whose
  node is a `.kcc-wash`, one per repainted wash layer.

`restyled_on_hover` reads 0 on the phone profile, which has no hover, so the `(hover: hover)` lift never applies. A page
with no `.kcc-slip--lift` records a named gap for it instead of a value. It
stays report-only and out of `baseline.ci.json`; the CI baselines hold `restyled_on_tuck` and
`wash_raster_on_interaction` for the ten phone Chromium key-subset cases. On the desktop profile (Chromium, light,
bloomed, one boot) it reads search-3 40, recipe 39, and 0 on home, variant-cook and account; the same run read
tuck/wash of 296/33 on home, 1,413/62 on search-3, 212/14 on recipe, 357/26 on variant-cook and 0/0 on account.
WebKit reports each probe as a named gap.

## F8. The "before" values for the new metrics (T011)

`pnpm perf:baseline` ran once, in one take, at `c1827268` with Phase 2's measurement code copied over it, from
18:20Z to 19:28Z under the CPU lock, and wrote `js_first_load_gz_bytes`, `js_later_gz_bytes`, `islands_hydrated`,
`restyled_on_tuck`, `restyled_on_hover` and `wash_raster_on_interaction` into `before.json`. Phone Chromium light
bloomed reads 118570 / 0 / 1 for the three script keys on every page, and tuck 296 / 1302 / 212 / 352 / 87 and wash
raster 33 / 60 / 14 / 26 / 8 on home, search-3, recipe, variant-cook and account. Hover reads 0 on search-3 and
recipe and is absent elsewhere. WebKit cases carry no probe keys. A first run was killed after about 10 minutes
because its probes ran before the timing pass; its numbers were discarded.

The machine was mostly quiet. Samples every 10 minutes (`ps` and `uptime`) show load averages of 2.8 to 6.6. What else
ran: a Spotlight index burst (`mds_stores` 35%) in the first minutes after the copy and build, a Docker/Virtualization
VM at 3-9%, GitKraken's renderer (up to 41%) and WindowServer (about 20%) at the 19:20 and final samples, and
`mediaanalysisd` at 109% in the 19:00 sample while the WebKit cases ran. No other test run, build or browser was
active during the run; flow-010 held no lock then. A `dotnet` build by another session started at the end sample.

## F9. The wizard and cook-mode later parts (T047, T048)

All runs were under the CPU lock on 2026-11-18. `js_first_load_gz_bytes` and `js_later_gz_bytes`, the same on every
profile and engine:

| Page | Before (F8) | Later steps only | + reading, submitted, photo strip | + leave confirm | + Basics and Variant |
| --- | --- | --- | --- | --- | --- |
| create-recipe | 118,570 | 63,494 | 61,742 | 61,277 | **56,571** |
| add-variant | 118,570 | 63,729 | 61,976 | 61,512 | **56,806** |
| variant-cook | 118,570 | 60,719 | 60,697 | 60,704 | 60,704 |

The wizard's later script grew from 10,790 to 23,109 as the parts moved out; cook mode's is 3,671. The last move made
every `*Step.vue` except `StartStep.vue` a later step, with Basics and Variant prefetched ahead of the rest at idle,
because add-variant was still 72 B over 61,440 after the leave confirm moved.

**First press (FirstPressTests).** The perf harness never presses a wizard's Next, and on the phone it never opens
cook mode either: the opener is `hidden lg:flex`, so every phone `variant-cook` case carries the gap "cook mode has no
opener at this width" and measures the page with it closed. The `interaction_ms_max` of 248 ms on the bloomed phone
variant page (72 bare, 0 with motion reduced) therefore belongs to the harness's own pointer moves over a blooming page,
not to cook mode. `tests/KCC.E2ETests/Features/RecipeWizard/FirstPressTests.cs` presses each control once and reads
the longest Event Timing entry from the press to the second frame after its answer:

| Press (phone, Chromium, light) | create-recipe bare | bloomed | add-variant bare | bloomed |
| --- | --- | --- | --- | --- |
| Start typing | 184 | 280 | 344 | 496 |
| Next to variant | 168 | 224 | | |
| Next to ingredients | 152 | 192 | 168 | 216 |
| Next to instructions | 168 | 168 | 152 | 144 |
| Next to review | 200 | 224 | 144 | 216 |

Cook mode, pressed on the desktop profile where its opener shows: 48 ms bare and 40 ms bloomed, inside 100 ms.

Every wizard press misses 100 ms on the throttled phone, bare as well as bloomed. It is not the chunk: a run before the
last move, with Basics and Variant still in the wizard's own chunk, read the same (start typing 312 and next to variant
224 bloomed), and every later step was prefetched by then. It is not mainly the washes: the bare visit misses too, and
blooming adds about 0 to 150 ms. What remains is the step change's own render on four-times-slowed CPU with
SwiftShader raster, which this phase did not trace. The cook-mode traces from `trace.mjs` on the desktop profile were
not usable for attribution: every window, bare, bloomed, washes hidden and motion reduced alike, is 420-570 ms of
`CrGpuMain` GPU tasks from the trace browser's own software GPU, which the test's browser does not use.

Machine: not quiet. WindowServer 25-29% and Codex 19-20% through every run, a GitKraken renderer at 62% at the start of
the last run, other `chrome-headless-shell` processes at 5-7% during the first, and Docker at 9%.

**The step change at `c1827268` (T048a).** FirstPressTests copied into `$BEFORE` and run under the CPU lock
(2026-11-18 10:21, WindowServer 37%, Codex 20%, Huntress 12%) reads the same misses before this feature, so the slow
step change predates 009:

| Press (phone, Chromium, light) | create-recipe bare | bloomed | add-variant bare | bloomed |
| --- | --- | --- | --- | --- |
| Start typing | 224 | 504 | 328 | 248 |
| Next to variant | 176 | 256 | | |
| Next to ingredients | 160 | 192 | 192 | 208 |
| Next to instructions | 176 | 176 | 168 | 168 |
| Next to review | 216 | 216 | 168 | 192 |

Cook mode at `c1827268`, desktop: passes bare and bloomed.

**Trace of a step change (T048a).** A CDP trace of each create-recipe press on the phone profile (4x CPU, SwiftShader,
10:22-10:28, same machine load), main thread and GPU, with Event Timing and long-animation-frame entries:

- The press's Event Timing duration (176-264 ms) splits into about 60-80 ms on the main thread and 90-125 ms of
  `GPUTask` on `CrGpuMain` after it. The GPU share is SwiftShader rastering the whole viewport (the root layer's paint
  covers 390x845): the new sheet's tear clip, its fall and blank filters and the desk under them. The CPU slowdown
  does not reach the GPU process, so this share depends on the host, not on the throttle.
- Main thread, per call site (variant press, bare): the click handler's microtasks take 78 ms. Of that, 58 ms is
  forced style and layout: the old step's `removeChild` forces a style recalc of 486 elements (22-24 ms), the new
  inputs' `v-model` `mounted` (setting `value`) forces layout (12.7 ms), and `focusHeading`'s `focus()` after
  `nextTick` forces a second recalc of 549 elements (22 ms). Paint itself is 2-3 ms.
- Ablations by injected CSS (each press, ms, bare / bloomed): base 160-224 / 192-240; no washes 144-208 / 144-232 with
  main-thread processing down from about 55 to 22 ms; no tear clip 120-160 / 112-144; no fall or blank filter
  128-184 / 136-208; no desk 120-208 / 168-216; none of the four 32-48 / 40-72 on every Next (start typing stays
  about 200). So no single look-preserving change reaches 100 ms: removing every forced recalc would leave about one
  recalc and layout (about 35 ms) plus the 90-125 ms GPU raster. Only removing the tear, the fall or the washes from
  the step sheet gets near the budget, which is a look change (Q1).

**Look-preserving cuts (T048a).** Two of the forced recalculations are gone from every step change, so any new
`*Step.vue` inherits the cut: the wizard's step watcher, which runs before Vue patches the DOM, blurs a focused control
inside the leaving step, so its removal no longer restyles the step; and the new step's heading is focused after the
next paint (a frame, then a task), not inside the frame that swaps the step. Focus still lands on the same heading.
The `v-model` mount layout stays: its only shared site is `Forms/InputField.vue` and `TextAreaField.vue`, and binding
`value` there instead would drop `v-model`'s number cast site-wide. FirstPressTests now walks the rendered wizard,
filling empty text fields until Next enables and pressing it until the review step, so a step the flow gains is
measured with no edit. Under the CPU lock (2026-11-18 10:39, WindowServer 35-40%, Codex 18-20%, Zen 18%, the
Virtualization VM 8%):

| Press (phone, Chromium, light) | create-recipe bare | bloomed | add-variant bare | bloomed |
| --- | --- | --- | --- | --- |
| Start typing | 208 | 496 | 184 | 224 |
| Next to variant | 152 | 208 | | |
| Next to ingredients | 136 | 184 | 144 | 208 |
| Next to instructions | 152 | 160 | 128 | 128 |
| Next to review | 192 | 264 | 136 | 192 |

Cook mode, desktop: 32 ms bare and bloomed. Every wizard press still misses 100 ms, by 28 to 396 ms, inside the run
to run spread of the tables above: the 90-125 ms GPU raster of the new sheet remains, and only a look change to the
step sheet's tear, fall or washes reaches the budget. That goes to the owner as a look round (Q1).

## F10. Render work for taps and frames (T052-T055)

All runs were under the CPU lock on 2026-11-18 with the branch's Release site (SSR on, 36 seeded recipes) after
D28 (T051): the tuck toggles `.is-tucked` on `<main>` and writes `--pad-h` there only when the pad's height changes.
Machine: not quiet. WindowServer 25-40%, Codex 20%, a GitKraken renderer up to 63%, Slack 9%, and from 10:44 another
session's Release site and `dotnet` (up to 54%); load averages 4.4 to 10.2.

**D18 (1), the tuck's restyle (T052).** `DeterministicGuardTests` (4 of 4 passed), phone Chromium light bloomed,
before (F8) and after:

| Page | `restyled_on_tuck` | `wash_raster_on_interaction` |
| --- | --- | --- |
| home | 296 to 15 | 33 to 0 |
| search-3 | 1,302 to 3 | 60 to 0 |
| recipe | 212 to 3 | 14 to 0 |
| variant-cook | 352 to 3 | 26 to 0 |
| account | 87 to 3 | 8 to 0 |

`restyled_on_hover` stays 0 on search-3 and recipe and a named gap elsewhere (the phone has no hover). The probe's
wash repaints went with the restyle: the tuck's inherited property was what repainted every wash layer in `<main>`.

`ablate.sh` (phone, bloomed, one run each, F3's columns; before is F3/F4 at `c1827268`):

| Page | Worst entry before | After (runs) | Long frames before | After (runs) | Worst frame after |
| --- | --- | --- | --- | --- | --- |
| home | 232-248 ms | 64, 64, 64 | 3 | 3, 1, 2 | 67-83 ms |
| search-1 | 248 ms | 128, 136, 136 | 5 | 7, 5, 6 | 167 ms |
| search-3 | 248 ms | 112, 112, 120 | 5 | 2, 2, 1 | 83 ms |

`pnpm perf:capture after --page home,search-1,search-3` and `pnpm perf:diff before after`: 52 paired cases, every one
0% of pixels differ (`look-1-home`, `look-1-search-1`, `look-1-search-3`). The diff needs `_round.html` in the
checkout's own `.mockups/006-performance-sweep/`; a worktree has none until it is copied from the main checkout.

**D18 (2), raster candidates (T053).** With `wash_raster_on_interaction` already 0 after D18 (1), no candidate can read
lower on the probe, so each was screened first by injecting its CSS at `DOMContentLoaded` (`ablate.sh`, phone and,
for the lift, desktop), one run each, and would have been built into the kit and captured only if it moved the
numbers. None did; none is shipped:

| Candidate (injected CSS) | home | search-1 | search-3 |
| --- | --- | --- | --- |
| As built (D28) | 64 ms, 2 long | 136 ms, 6 long | 120 ms, 1 long |
| Washes contained: `.kcc-washes{contain:strict}` | 64, 2 | 136, 5 | 112, 1 |
| Sticky tops not eased: `[class*="transition-[top]"]{transition-property:none}` | 64, 2 | 136, 5 | 112, 1 |
| Tucked pad composited: `.pad-header.is-tucked{will-change:transform}` (a layer at rest while tucked) | 64, 3 | 152, 6 | 120, 1 |
| Torn wrapper paint-contained: `.kcc-torn{contain:paint}` | 64, 2 | 144, 5 | 112, 2 |
| Slip layout-contained: `.kcc-slip{contain:layout style}` | 64, 2 | 136, 5 | 112, 2 |
| Lifted card composited while hovered: `.kcc-slip--lift:hover > :not(.kcc-blank){will-change:translate}` (desktop search-1) | | 184 ms (hover 120 against 152), 8 long against 8 | |

**What is left, by ablation (look changes, set aside for T056).** Phone, bloomed, after D28, one run each:

| Variant (injected CSS) | home | search-1 | search-3 |
| --- | --- | --- | --- |
| As built | 64 ms, 2 long, 63.6% | 136 ms, 6 long, 66.2% | 120 ms, 1 long, 54.6% |
| Washes hidden: `.kcc-washes{display:none!important}` | 32 ms, 2 long, 68.4% | 48 ms, 4 long, 62.5% | 88 ms, 1 long, 55.1% |
| Wash masks off: `.kcc-wash{mask-image:none!important;-webkit-mask-image:none!important}` | 64, 2, 63.9% | 136, 4, 67.2% | 112, 3, 56.1% |
| Fall filter off: `.kcc-fall{filter:none!important}` | 64, 1, 69.5% | 176, 5, 63.8% | 96, 1, 56.4% |
| Reduced motion (profile, not CSS) | 0, 0, 68.5% | 136, 3, 68.6% | 48, 0, 55.6% |

(The percentage is the probe's rAF share, not the harness's `frames_within_16_7_pct`; see Method.) Search-1's 136 ms
and its 167 ms frames survive reduced motion, so they are the first raster of the sheets the first scroll exposes,
not the tuck or a lift; only hiding the washes takes them to 48 ms. Hiding the washes is a look change (Q1). The
masks and the fall no longer carry a share once the restyle is gone.

**Sign-in (T054).** N/A: T004 read sign-in's phone Chromium `long_frames` as 0 in all six cases at `c1827268` (F4).

**Re-measure (T055, D18 (3)).** `pnpm perf --check --page search-3,home,sign-in`, phone Chromium, phone WebKit, then
desktop in both engines, 11:09-11:34 under the CPU lock (load averages 9.3 at the start falling to 2.7; WindowServer,
Codex and GitKraken as above). One run per case. Each cell is worst interaction ms / long frames / share within
16.7 ms %, with `before.json` in brackets:

| Case | home | search-3 | sign-in |
| --- | --- | --- | --- |
| phone Chromium light bare | 40 (88) / 2 (6) / 49.5 (46.4) | 56 (152) / 2 (2) / 50.0 (46.6) | 16 (16) / 0 (0) / 61.8 (52.9) |
| phone Chromium light bloomed | 64 (248) / 2 (4) / 48.5 (47.8) | **120** (264) / 2 (6) / 47.6 (48.2) | 0 (0) / 0 (0) / 64.7 (64.7) |
| phone Chromium dark bare | 40 (88) / 2 (6) / 52.7 (48.5) | 56 (136) / 0 (3) / 50.7 (50.3) | 16 (16) / 0 (0) / 55.9 (61.8) |
| phone Chromium dark bloomed | 64 (248) / 2 (3) / 51.7 (47.5) | **120** (256) / 2 (5) / 49.3 (47.4) | 0 (0) / 0 (0) / 61.8 (57.1) |
| phone Chromium light mid | 88 (136) / 3 (6) / 47.8 (44.9) | 56 (256) / 4 (4) / 53.6 (51.2) | |
| phone Chromium light bloomed, images off | 64 (232) / 2 (4) / 47.9 (47.9) | **120** (240) / 1 (6) / 50.0 (50.8) | 0 (0) / 0 (0) / 50.0 (58.8) |
| phone Chromium light bloomed, motion reduced | 0 (0) / 1 (1) / 50.2 (50.2) | 40 (48) / 0 (0) / 49.1 (49.9) | 0 (0) / 0 (0) / 64.7 (65.7) |
| phone WebKit light bare | 216 (224) / 10 (11) / 36.7 (40.8) | 112 (152) / 13 (10) / 38.5 (39.3) | 0 (0) / 0 (0) / 32.4 (45.7) |
| phone WebKit light bloomed | 256 (224) / 18 (17) / 34.5 (39.0) | 488 (520) / 27 (27) / 38.5 (40.8) | 0 (0) / 0 (0) / 41.2 (32.4) |
| phone WebKit dark bloomed | 264 (296) / 16 (17) / 38.0 (40.6) | 472 (536) / 26 (27) / 33.5 (44.9) | 0 (0) / 0 (0) / 32.4 (35.3) |
| desktop Chromium light bare | 16 (16) / 1 (0) / 59.5 (59.0) | 32 (56) / 0 (1) / 53.1 (54.4) | 0 (16) / 0 (0) / 58.8 (64.7) |
| desktop Chromium light bloomed | 16 (32) / 0 (0) / 59.6 (58.5) | 24 (56) / 0 (1) / 56.6 (52.4) | 0 (0) / 0 (0) / 67.6 (50.0) |
| desktop Chromium light mid | 16 (16) / 1 (0) / 49.4 (48.4) | **192** (56) / 1 (1) / 49.3 (50.8) | |
| desktop Chromium dark bloomed | 16 (32) / 0 (0) / 59.1 (57.6) | 24 (48) / 0 (1) / 50.6 (49.8) | 0 (0) / 0 (0) / 55.9 (51.4) |
| desktop WebKit light bare | 32 (40) / 13 (12) / 35.6 (38.0) | 64 (72) / 21 (17) / 35.9 (36.4) | 0 (0) / 0 (0) / 32.4 (47.1) |
| desktop WebKit light bloomed | 40 (72) / 14 (15) / 29.0 (41.7) | 96 (136) / 38 (36) / 33.0 (36.7) | 0 (0) / 0 (0) / 29.4 (40.0) |
| desktop WebKit dark bloomed | 72 (40) / 14 (12) / 32.8 (41.4) | **112** (80) / 35 (36) / 34.5 (37.6) | 0 (0) / 0 (0) / 35.3 (41.2) |

The matrix has no reduced-motion case on the desktop profile or in WebKit. Every run exited 2: the share within
16.7 ms fails its budget in every Chromium case, before as after (the harness reads 47-68% on this machine), and
WebKit's long frames fail theirs in several.

The gap left:

- **Phone Chromium:** search-3 bloomed at 120 ms (light, dark, images off), the only interaction over 100 ms. Every
  phone Chromium interaction is under its baseline. The ablations put it on the first raster of the washes the scroll
  exposes; only hiding the washes reaches the budget (search-3 88 ms and search-1 48 ms in the probe), a look change
  for T056.
- **Desktop:** search-3 light mid in Chromium read 192 ms against a 56 ms baseline (one reading on a blooming
  visit; bare and bloomed read 24-32 ms), and search-3 dark bloomed in WebKit read 112 ms. Both want a repeat before
  they are called regressions.
- **Frames:** no case reaches 95% within 16.7 ms in any engine, and WebKit keeps 10-38 long frames on home and
  search-3, as before.
- **FR-009 (phone WebKit no worse than `before.json`):** interactions and long frames hold within their margins; the
  share within 16.7 ms reads lower than `before.json` in most WebKit cases (sign-in light bare 32.4 against 45.7, the
  one that fails), on pages and paths Phase 8 did not change, under a heavier machine load than T011's.

**T055 outliers repeated.** Each outlier three times, `pnpm perf --check --page <page> --profile <profile> --engine
<engine> --ramp <ramp> --visit <visit>`, 12:10-12:16 on 2026-11-18 under the CPU lock, one case after another in
rotation, on a fresh `pnpm build:all` and Release build of the branch with the review fixes (load averages 3.1-4.8 at
the start of each run; no other perf run held the lock). Worst interaction ms / long frames / share within 16.7 ms %,
`before.json` in brackets:

| Case | Run 1 | Run 2 | Run 3 |
| --- | --- | --- | --- |
| search-3 desktop Chromium light mid | 32 / 0 / 49.4 | 32 / 0 / 49.4 | 32 / 1 / 48.9 (56 / 1 / 50.8) |
| search-3 desktop WebKit dark bloomed | 144 / 39 / 41.3 | 144 / 38 / 36.2 | 96 / 35 / 35.3 (80 / 36 / 37.6) |
| sign-in phone WebKit light bare | 0 / 0 / 40.0 | 0 / 0 / 37.1 | 0 / 0 / 38.2 (0 / 0 / 45.7) |

- **search-3 desktop Chromium light mid:** noise. 32 ms in all three runs, under the 56 ms of `before.json`; the 192 ms
  reading did not repeat.
- **search-3 desktop WebKit dark bloomed:** the regression holds. Two of three runs read 144 ms and the third 96 ms,
  each over the 80 ms of `before.json` (a single reading). The harness passes it against its budget; long frames
  (35-39 against 36) and the share within 16.7 ms (35.3-41.3 against 37.6) hold. T055 stayed open until the case was
  measured at `c1827268` under the same machine state (below).
- **sign-in phone WebKit light bare (FR-009):** the share within 16.7 ms reads 37.1-40.0 against 45.7, below
  `before.json` in all three runs but within the harness's margin, which passes it each time; the 32.4 reading was the
  low end. Sign-in takes no interaction and has no long frame in any run. Whether 6-9 points under a single `before`
  reading is a regression wants the same case at `c1827268` in the same session.

**T055 outliers at `c1827268` and on the branch, same session.** Both cases three times in `$BEFORE` (`c1827268`, the
Phase 2 harness) and then three times on the branch (`55932244` plus the uncommitted working tree, which held the
error-state brand fixes and another phase's in-progress search edit), one case after another in rotation,
12:30-12:36 on 2026-11-18 in one hold of the CPU lock, each side on its own Release site. Machine: load averages
4.4 falling to 2.1 over the twelve runs; WindowServer 38%, a Zen browser tab 29% and its GPU helper 17%, Codex 20%; no
other perf run. Worst interaction ms / long frames / share within 16.7 ms % / frame p95 ms:

| Case | Side | Run 1 | Run 2 | Run 3 |
| --- | --- | --- | --- | --- |
| search-3 desktop WebKit dark bloomed | `c1827268` | 128 / 36 / 37.4 / 103 | 128 / 34 / 36.0 / 98 | 136 / 38 / 35.7 / 116 |
| search-3 desktop WebKit dark bloomed | branch | 128 / 35 / 33.6 / 98 | 88 / 37 / 34.9 / 120 | 128 / 36 / 35.4 / 117 |
| sign-in phone WebKit light bare | `c1827268` | 0 / 0 / 38.2 / 18 | 0 / 0 / 35.3 / 18 | 0 / 0 / 32.4 / 17 |
| sign-in phone WebKit light bare | branch | 0 / 0 / 40.0 / 18 | 0 / 0 / 34.3 / 17 | 0 / 0 / 37.1 / 18 |

- **search-3 desktop WebKit dark bloomed:** no regression. Measured the same way, `c1827268` reads 128-136 ms, and the
  branch 88-128 ms; long frames (34-38 against 35-37), the share (35.7-37.4 against 33.6-35.4) and frame p95 (98-116
  against 98-120) overlap. The 80 ms of `before.json` was a single low reading of a case that reads 128-136 ms at
  `c1827268` on this machine. The branch's runs exit 2 on `long_frames` and `frame_p95_ms` against its harness
  baseline (20 and 68), which `c1827268` misses by as much.
- **sign-in phone WebKit light bare (FR-009):** no regression. `c1827268` reads a share of 32.4-38.2 and the branch
  34.3-40.0, with no interaction and no long frame on either side; the 45.7 of `before.json` was a single high
  reading.

T055 is done: the branch is no worse than `c1827268` measured the same way in either case.

## F11. The phone Chromium gap carried (T056, T056a)

The owner kept the look (2026-11-18, "Keep look, approve E"; `specs/009-script-hydration-cost/mockups/README.md`
holds the ruling, the candidates and their gains). No wash change ships, and the phone Chromium interaction and frame
budgets stay at baseline mode: `budgets.json` carries entries scoped `{"profile": "phone", "engine": "chromium"}` for
`interaction_ms_max`, `frames_within_16_7_pct`, `long_frames` and `frame_p95_ms`, after the general ones, which
T066's flip to target does not reach. The gap goes to Phase 3 or a later phase.

**Pages, phone Chromium (T055, F10).** Worst interaction ms / long frames / share within 16.7 ms %, `before.json` in
brackets, one run per case:

| Visit | home | search-3 | sign-in |
| --- | --- | --- | --- |
| light bare | 40 (88) / 2 (6) / 49.5 (46.4) | 56 (152) / 2 (2) / 50.0 (46.6) | 16 (16) / 0 (0) / 61.8 (52.9) |
| light bloomed | 64 (248) / 2 (4) / 48.5 (47.8) | 120 (264) / 2 (6) / 47.6 (48.2) | 0 (0) / 0 (0) / 64.7 (64.7) |
| dark bare | 40 (88) / 2 (6) / 52.7 (48.5) | 56 (136) / 0 (3) / 50.7 (50.3) | 16 (16) / 0 (0) / 55.9 (61.8) |
| dark bloomed | 64 (248) / 2 (3) / 51.7 (47.5) | 120 (256) / 2 (5) / 49.3 (47.4) | 0 (0) / 0 (0) / 61.8 (57.1) |
| light mid | 88 (136) / 3 (6) / 47.8 (44.9) | 56 (256) / 4 (4) / 53.6 (51.2) | |
| light bloomed, images off | 64 (232) / 2 (4) / 47.9 (47.9) | 120 (240) / 1 (6) / 50.0 (50.8) | 0 (0) / 0 (0) / 50.0 (58.8) |
| light bloomed, motion reduced | 0 (0) / 1 (1) / 50.2 (50.2) | 40 (48) / 0 (0) / 49.1 (49.9) | 0 (0) / 0 (0) / 64.7 (65.7) |

The gap: search-3 bloomed at 120 ms against 100 ms, and every case's share within 16.7 ms under 95%. No interaction
is slower than its baseline.

**Wizard, phone Chromium (FR-013, F9).** Every first press misses 100 ms, 128-496 ms in F9's last run (create-recipe
bare 136-208, bloomed 160-496; add-variant bare 128-184, bloomed 128-224). `FirstPressTests` records each press on the
phone profile and requires every press measured, without asserting 100 ms; cook mode, on the desktop profile, still
asserts it (32 ms bare and bloomed).
At T056a's run (2026-11-18 13:14, under the CPU lock, after a fresh `pnpm build:all` and Release build) the phone
wizard's worst press read: create-recipe 264 ms bare and 456 ms bloomed, add-variant 352 ms bare and 208 ms bloomed;
every press measured. Cook mode on desktop read 48 ms bare and 40 ms bloomed.

**Why: SwiftShader.** The phone profile rasterises on SwiftShader, which the 4x CPU throttle does not reach. The look
round measured the same throttle on the same Mac with SwiftShader and with hardware raster (ANGLE Metal):

| Press or tap (phone, 4x CPU) | Raster | Before (`c1827268`) | Branch |
| --- | --- | --- | --- |
| Wizard, worst press | SwiftShader | 608 ms | 376 ms |
| Wizard, worst Next | SwiftShader | 288 ms | 256 ms |
| Search-3 bloomed tap | SwiftShader | 248-256 ms | 112-168 ms |
| Wizard, worst press | Hardware | 104 ms | 64 ms |
| Wizard, worst Next | Hardware | 72 ms | 48 ms |
| Search-3 bloomed tap | Hardware | 96 ms | 0 ms (no entry over 16 ms) |

With hardware raster the branch meets 100 ms with no look change.

## F12. Guards at target (T062-T067)

**Modes (T064).** `js_first_load_gz_bytes` at target 61,440 B and `js_gz_bytes` at target 117,266 B, locally and on CI.
`hydrated_ms` at target, 2,000 ms phone and 1,000 ms desktop, scaled by the runner factor as a baseline is, and held
to its baseline as well (`alsoBaseline`, with `ciMargin` on CI); a failure names whichever side failed.
`islands_hydrated` at baseline with no increase. It keeps `notARegression`, which only the before-and-after report
reads: the guard compares the branch's count with `baseline.ci.json`, written from the islands (T065), while the report
compares with `before.json`, where the whole-page app counted 1, so a rise there is the islands, not a regression.
`js_later_gz_bytes` stays report-only with `notARegression` for the same reason. `restyled_on_tuck` at target 16 on
Chromium: Phase 8 left 15 on phone home, 16 on desktop home and 3-4 elsewhere (F10). `wash_raster_on_interaction` at
target 0 on Chromium bloomed visits only, since bare and mid visits read 1-4 while their washes bloom during the tuck.
`restyled_on_hover` stays report-only (the phone has no hover). WebKit reports the three probes as named gaps.
`cls` and `cls_scroll` at target 0 locally and report-only on CI. The interaction and frame metrics keep their
baseline mode locally and report-only on CI until T066; the phone Chromium entries T056a scoped stay at baseline.

**CI baselines (T065).** `DeterministicGuardTests` with `KCC_PERF_MODE=baseline`, both engines, 13:31-13:34 on
2026-11-18 under the CPU lock, after `pnpm build:all` and the Release build of `efc3085f` plus the working tree (which
held Phase 9's uncommitted search edits). It merged the branch's counts into every key-subset case and gave
create-recipe and add-variant entries holding only the two byte metrics. The writer also added `restyled_on_hover` (0)
to search-3 and recipe, which was removed by hand. `hydrated_ms`, `server_response_ms` and `calibrationMs` did not
change. First-load script now, gz B: home 47,147, search-3 59,234, recipe 57,593, variant-cook 61,970, account
48,937, create-recipe 57,366, add-variant 57,599; `islands_hydrated` 2 on every key-subset page; `restyled_on_tuck`
15 on home and 3 elsewhere; `wash_raster_on_interaction` 0.

**variant-cook is over the target.** Its first load reads 61,964-62,001 B across three builds of the same tree, 524 to
561 B over 61,440, in both engines and ramps; every other page passes. Every page's first load grew by 0.8-2.4 KB
since Phase 7's measurement (variant-cook 60,704). The growth is common to every page, so it sits in shared
first-load code such as the `main` chunk (35,706 B gz); the later-part retries, `beforeHydrate` and the cook-mode
pending dialog have landed since. Until variant-cook loses about 600 B of first-load
script, `DeterministicGuardTests` fails on unchanged code, so the stability trial (T066) has not started and the
proof's passing half (T067) is open.

**The 20 KB proof (T067).** One CPU lock hold, 13:40-14:09 on 2026-11-18: `Main.ts` imported a module of 27,600
random base64 characters (20,966 B gzipped) and wrote its length to `window`; `pnpm build:all`, the Release build,
`DeterministicGuardTests`, `TimingGuardTests`; then the edit was removed, both builds and both classes ran again, and
`git diff src/KCC.Web/Features/Main.ts` was empty. With the import, `DeterministicGuardTests` failed in both engines on
every page, light and dark, naming page, profile, engine, ramp, budget and number, for example:

```text
home · phone · chromium · light · bloomed: js_first_load_gz_bytes 69489 > target 61440
search-3 · phone · webkit · dark · bloomed: js_first_load_gz_bytes 81535 > target 61440
create-recipe · phone · chromium · light · bloomed: js_first_load_gz_bytes 79705 > target 61440
```

(home 69,489, search-3 81,535, recipe 79,935, variant-cook 84,308, account 71,276, create-recipe 79,705, add-variant
79,938; the visit totals stayed under 117,266). `TimingGuardTests` passed with the import (11 min 14 s; runner
calibration 78.95 ms against 206.8 ms, so x1; `hydrated_ms` 927-1,670 ms) and without it (11 min 13 s; 79.75 ms;
888-1,532 ms). Without the import `DeterministicGuardTests` failed only on variant-cook at 62,001 B. The timing run
also read `cls` 0.0001 on search-3 and account, which the local target of 0 fails.

**The source of that 0.0001 (2026-11-18, `layout-shift` entry sources, phone Chromium, light, bloomed, under the CPU lock).**
It is one entry before `kcc:hydrated`, from a Font Awesome duotone `::before` glyph that grows when the icon font
arrives: `fa-bowl-food` on account (0.00005, 541>545 px down the page, 38x56 to 49x49) and `fa-plate-utensils` on
search (0.000055, y 805, at the foot of the 812 px screen), the load-time entry that F5 describes. Both round to
0.0001 in `cls`. At `c1827268`, with the same measuring code, account reads the same entry at the same size (0.00005,
`fa-bowl-food`, cls 0.0001, three of three cases) and search reads the same one: 009 did not cause it. Phase 1's 0
on account came from an entry that falls just under the rounding or arrives after the load mark in that run; it is not
a difference in the page. No change made: the glyph's box belongs to the icon font, and a fix is a new icon-sizing
rule on every page that shows the glyph.

Machine during both runs: load averages 3.8-6.1; Claude, its renderer and `syspolicyd` at 4-9%, a Docker VM at 4%; no
other perf run held the lock.

**T067 on HEAD (`a40e0c4a`, no import, 18:13-18:28 on 2026-11-18 under the CPU lock).** The 20 KB import is not in the tree:
`git diff -- src/KCC.Web/Features/Main.ts` is empty. `pnpm build:all` exited 0, the Release build exited 0 with 0
errors, and `DeterministicGuardTests` (18:14-18:17) and `TimingGuardTests` (18:17-18:28) each passed. The byte guard
passed both classes on HEAD, including the variant-cook first-load byte entry. The pass lines do not print byte values;
the 60,693 B figure for variant-cook is the orchestrator's measurement and was not re-read in this run. T067 is closed.

## Decisions

The decisions are rows D1-D33 of the plan's Approach table. Rationale that needs more than a row:

- **Islands over whole-page hydration without a compiler (D1).** Razor writes the page, so no build step sees the
  markup around the components; a precompiled region template is impossible. Hydrating only the interactive global
  components drops the compiler, the second copy of the HTML and every component a page does not use, and needs no
  change to any Razor view. A server-only global component (D3) ships no script at all.
- **A wrapper element (D2).** Vue mounts an app into a container and hydrates its children; it has no supported way to
  hydrate one element among siblings. `<kcc-island style="display: contents">` is the container. `display: contents`
  keeps grid and flex layout, but a child combinator such as `.content-grid > :not(.breakout, .full-width)` or
  `:where(#app > *)` no longer reaches the island's root. The audit (T016) lists every child, sibling and structural
  selector that can meet an island: island roots sit only at `#app`'s top level (the header), as `<main>`'s child (the
  page views) and inside the stacker block's grid. The before-and-after captures check the result.
- **The fallback bundle (D6).** Today, when the sidecar fails or SSR is off, the browser renders the whole page from
  the templates in `#server-content`. Without a compiler in the island bundle that rescue would show a blank page.
  A third build of today's whole-app entry, with the compiler, keeps it: the server links it only when it has no
  rendered HTML, which also keeps the integration suite's `RenderedPage` reading templates with SSR off. Importing the
  full Vue build into the island bundle instead would put a second Vue runtime in the same graph, and the compiled
  components would bind to the other one.
- **Interaction fix order (D18).** The tuck's restyle is a cheap, look-neutral change (25-52 ms). The raster share is
  larger, and every candidate is measured with the probe before it is kept: compositing what moves during a lift or a
  tuck only while it moves (no layer at rest, so Phase 1's exact layer count holds), and keeping a lifted card from
  invalidating the sheets around it. A candidate that changes how a wash looks goes to a look round, and Q1 decides
  what happens if none reaches 100 ms.

## F13. Bringing first-load script back under the target

**Where the growth came from.** Client builds of `8e1a5ed5` (a scratch worktree) and of the branch with Phase 9's
uncommitted search edits, 2026-11-18 14:12 under the CPU lock, compared chunk by chunk over each page's static
closure (`Main.ts`, `AppHeader` and the page's island), with source maps attributing each chunk's bytes:

| Chunk | gz B growth | What moved in |
|---|---|---|
| `main` | +312 | `Islands/laterParts.ts` +519 B raw (the retry, timeout and failure code, the arrival wait), `beforeHydrate.ts` +150 B raw |
| `VariantDetailView` | +605 | `CookModePending.vue` +1,098 B raw (the pending dialog with its close button, Escape and error state), `containFocus.ts` +322 B raw |
| `RecipeWizard` | +278 | `PartFailed`, the wizard's error states |
| `RecipeSearchView` | +902 | Phase 9's uncommitted reserved-space edits |

`main` held all of `laterParts.ts` because `Main.ts` imported `scheduleLaterParts` from it, so every page paid for
the loader only variant-cook and the wizards use. The pending dialog and the error state stay in first load: they are
what a reader sees when cook mode is slow or fails, so they cannot wait on a chunk that may be the one failing (D30).

**What moved.** Load-only, every existing test unchanged:

- `Islands/laterSchedule.ts` holds the idle prefetch queue (`registerLaterPart`, `scheduleLaterParts`), the only part
  `Main.ts` and `Fallback.ts` need. `laterParts.ts` keeps the loader and re-exports the two, and `whenArrived` moves to
  `Islands/whenArrived.ts`, which only the wizard imports.
- `siblingToCard` moves to `Components/Recipe/siblingCard.ts`, so the variant page no longer carries the search and
  recipe card builders of `recipeCardModel.ts`, which re-exports it.
- The note and review forms' `TextAreaField` is a later part again, through `memberTextArea.ts`, on the variant page
  only. Vue hydrates an async field once its chunk arrives, and a bound `value` then wrote the model's empty state over
  what a member typed first. The field now writes its textarea by hand: its first content is a `v-text` child, which
  hydration leaves alone, a watch writes later model values, and on mount a value that differs from the textarea's
  `defaultValue` is adopted into the model. The node, its focus and its caret survive, and the dev build raises no
  hydration mismatch for it (`TextAreaFieldHydration.test.ts` fails on a `mismatch` warning).
- **The submit.** A review submitted before the field arrived sent the model's stale text. `typedText` in
  `memberTextArea.ts` returns the live textarea's value when it differs from its `defaultValue`, else the model's, and
  `VariantReviews` and `VariantCookNotes` send it. The wizard's `whenArrived` does not fit: the field's server HTML has
  no pending root to wait on. `VariantLateField.test.ts` hydrates the page with the chunk held and failed before the
  fix. The cook note's Add button stays disabled until the field has arrived and the model holds the text, so only the
  review form could submit early.

`tests/KCC.ViteTests/Features/Islands/firstLoadModules.test.ts` reads the source map of every chunk in a page's
static closure and fails when `laterParts.ts` or `whenArrived.ts` is in the entry's, or `recipeCardModel.ts` or
`whenArrived.ts` in the variant page's. It failed before each move.

**After.** First-load script by the guard's `js_first_load_gz_bytes`, gz B, from `DeterministicGuardTests` on
2026-11-18 under the CPU lock (Chromium and WebKit read the same), against the limit of 61,440:

| Page | gz B | Margin |
|---|---|---|
| home | 46,770 | 14,670 |
| search-3 | 58,799 | 2,641 |
| recipe | 57,188 | 4,252 |
| variant-cook | 60,693 | 747 |
| account | 48,564 | 12,876 |
| create-recipe | 57,607 | 3,833 |
| add-variant | 57,841 | 3,599 |

The tightest page is variant-cook, 747 B under the limit and 1,277 B under its baseline. Home, search, recipe and
account sit 373 to 435 B under their baselines; the two wizard pages sit 241 and 242 B over theirs, from the loader
becoming a chunk of its own, and keep 3.6 KB of room. With the field static, variant-cook read about 61,573, 130 B
over the limit. The deferred field's chunk is 1,031 B gz.

`LateFieldTests` passed on Chromium and WebKit: text typed on the throttled phone profile before the field's chunk
arrives survives its hydration with focus and caret, and a review submitted in that window posts the typed text.

## F14. Stability (T066)

The owner's 2026-11-19 decision amends the C1 answer: three runs each, not ten, and a metric flips to target locally only
if it passes three of three. Each run took the CPU lock alone, under `caffeinate -dims`, on AC power, on HEAD 721edde8
plus the boot-deadline harness change. Runs from the earlier attempts do not count.

Earlier T066 failures: the Mac went into Clamshell Sleep on battery (`pmset -g log`, 2026-11-18 18:38:31) and woke only in
2 s DarkWakes, so the site boot froze and the wall-clock five-minute deadline in `SiteProcess` expired. The SQLite Error 14
lines in the failure tail are Umbraco's normal pre-install probes, which every healthy boot logs; the database path was
never at fault. `BootDeadline` now names the sleep ("The machine slept N min N s during boot") when wall time and awake
time differ by more than 30 s.

Every run began on AC power (waited 0 min) and no `Sleep` entry in `pmset -g log` fell inside any run; no run was
discarded as slept. Load at start was 3-9 (the Mac's usual Zen, ChatGPT and WindowServer load).

| Run | Start | End | Result |
|---|---|---|---|
| DeterministicGuardTests 1 | 08:44:19 | 08:47:25 | pass, 4 of 4 |
| DeterministicGuardTests 2 | 08:47:27 | 08:50:35 | pass, 4 of 4 |
| DeterministicGuardTests 3 | 08:50:42 | 08:53:50 | pass, 4 of 4 |
| TimingGuardTests 1 | 08:53:52 | 09:05:26 | pass, 1 of 1 |
| TimingGuardTests 2 | 09:05:27 | 09:16:59 | pass, 1 of 1 |
| TimingGuardTests 3 | 09:17:01 | 09:28:34 | pass, 1 of 1 |
| `pnpm perf --check` 1 | 09:28:36 | 10:37:43 | FAIL, 288 failures |
| `pnpm perf --check` 2 | 10:37:45 | 11:46:35 | FAIL, 287 failures |
| `pnpm perf --check` 3 | 11:46:37 | 12:55:32 | FAIL, 293 failures |
| DeterministicGuardTests boot 1 | 12:55:33 | 12:58:38 | pass, 4 of 4 |
| DeterministicGuardTests boot 2 | 12:58:40 | 13:01:46 | pass, 4 of 4 |
| DeterministicGuardTests boot 3 | 13:01:48 | 13:04:54 | pass, 4 of 4 |

The six deterministic runs read the same exact-count budgets and passed all of them, so the probe counts agree. Only the
last boot's `results.json` survives (the file is overwritten per run); it reads `islands_hydrated` 2 on the bloomed
home, search-3 and recipe cases in both engines.

Per-metric pass counts, three runs each:

| Metric | Deterministic | Timing | perf --check |
|---|---|---|---|
| Guard classes as a whole | 3 of 3 | 3 of 3 | 0 of 3 |
| `frames_within_16_7_pct` | n/a | n/a | 0 of 3 (fails in every run: 69 phone Chromium, 56 desktop Chromium, 1 desktop WebKit) |
| `frame_p95_ms` | n/a | n/a | 0 of 3 (56 desktop Chromium, 24 phone Chromium, 8 WebKit) |
| `long_frames` | n/a | n/a | 0 of 3 (desktop WebKit 7-10, phone WebKit 5-10, phone Chromium 0-2) |
| `interaction_ms_max` | n/a | n/a | 0 of 3 (phone Chromium 3, phone WebKit 4) |

`perf --check` also failed `lcp_ms` (10-18 phone Chromium), `clip_path_elements` (18) and `hydrated_ms` (2-3 phone
Chromium) each run. The failures are mostly the four frame metrics in baseline mode on this machine, so the check does not
pass locally even before a flip.

Flips: none. Measured against the target limit on the non-phone-Chromium cases of runs 1 and 3 (run 2 kept only its
rendered report), `frames_within_16_7_pct` meets it on 0 of 152 cases, `long_frames` on 100, `frame_p95_ms` on about 120
and `interaction_ms_max` on about 127, so no metric passes three of three, and all four stay at baseline in `budgets.json`
(phone Chromium stays at baseline by T056a in any case).

The 287 to 293 failures compare a local run on this Mac with Phase 1's CI numbers in
`specs/006-performance-sweep/baseline.json`, so they do not show whether 009 is worse than before. The four frame metrics
also failed baseline mode on search-1 and search-3 in the 2026-11-18 check at 4e55e632. The `clip_path_elements` failures
are all the account page (27 phone, 26 desktop, against baselines of 24 and 23); the same page reads 27 at c1827268 in the
F13 capture, so the reading is present before this branch and nothing changes for it. T068 compares the full matrix with
`before.json`, captured on this Mac at c1827268, and that comparison is the regression evidence.

## F15. After against before, look and scripts-off (T068-T070)

**`before.json` recaptured.** The 2026-11-17 capture of `c1827268` predates the run conditions T066 settled on, so it is
replaced. `pnpm perf:baseline` ran in the `$BEFORE` worktree (its own build was current: `pnpm build:all` and the Release
build did nothing) from 13:09:01 to 14:17:10 on 2026-11-19, under the CPU lock, `caffeinate -dims`, on AC power. It wrote
232 cases with every key the earlier file had, at commit `c18272681b05`, Chromium 156.0.8078.4, WebKit 27.2, runner 1.
The old file read 118570 B first load on every page; nothing else about its shape changed.

**After.** `pnpm perf --check --before specs/009-script-hydration-cost/before.json` ran on HEAD `9d864f0c` from 14:17:45 to
15:26:53, same conditions. It exited 2 with 289 failures: the check enforces `baseline.json` (Phase 1's CI numbers), so
the failures are the local-machine gap F14 describes, not the before comparison. The load at start was 17 for the first
minutes (`mediaanalysisd` at 109%), then the usual 5-9. Both runs: AC power, waited 0 min, no `Sleep` entry in
`pmset -g log` in either window, nothing discarded. The rest of T069 and T070 ran after, 15:26:54 to 15:39:51, same
conditions and no sleep. `after.json` is the after run's `results.json`.

**Against before, by the report's own rule** (D27: the before column and each budget's local `margin`, never `ciMargin`;
a count with no margin is worse on any increase). The report marks 227 rows worse: 109 phone and 46 desktop Chromium
`css_unused_bytes` and 4 `css_unused_pct` (report-only, no margin, no budget: the rule does not say whether they count),
26 phone Chromium `hydrated_ms`, 14 `layer_area_mpx`, 1 `layers`, 48 `frames_within_16_7_pct`, 11 `long_frames`, 7
`interaction_ms_max`, 3 `lcp_ms`, 2 `frame_p95_ms`. The byte targets hold: first load at most 60,693 B and visit total at
most 80,987 B on every case (both against 118,570 B before), `hydrated_ms` at most 1,680.8 ms on the phone (target 2,000)
and 75.9 ms on the desktop (target 1,000). Phone Chromium `hydrated_ms` median is 1288 ms against 1128 ms before.

**Condition 3 table.** `hydrated_ms` and `lcp_ms` on phone Chromium, and the four frame metrics on search-1 and search-3,
both ramps, bare and bloomed. The results hold no `bloom_` variant keys: the bloomed visit is its own case, so the
`bloom_` budgets never fire. Counts: `hydrated_ms` 12 worse of 48; `lcp_ms` 2 of 48; `frames_within_16_7_pct` 4 of 32;
`frame_p95_ms` 0 of 32; `long_frames` 7 of 32; `interaction_ms_max` 4 of 32.

| Case | Metric | Before | After | Margin | Verdict |
|---|---|---|---|---|---|
| account · phone · chromium · dark · bare | hydrated_ms | 1127.5 | 998.8 | x1.25/+150 | ok |
| account · phone · chromium · dark · bloomed | hydrated_ms | 1127.1 | 992.3 | x1.25/+150 | ok |
| account · phone · chromium · light · bare | hydrated_ms | 1123.5 | 1012.8 | x1.25/+150 | ok |
| account · phone · chromium · light · bloomed | hydrated_ms | 1120.9 | 989 | x1.25/+150 | ok |
| add-variant · phone · chromium · dark · bare | hydrated_ms | 287.6 | 412.6 | x1.25/+150 | ok |
| add-variant · phone · chromium · dark · bloomed | hydrated_ms | 332.4 | 477.5 | x1.25/+150 | ok |
| add-variant · phone · chromium · light · bare | hydrated_ms | 283.1 | 417.8 | x1.25/+150 | ok |
| add-variant · phone · chromium · light · bloomed | hydrated_ms | 274.5 | 423.3 | x1.25/+150 | ok |
| create-recipe · phone · chromium · dark · bare | hydrated_ms | 1133.5 | 1334 | x1.25/+150 | ok |
| create-recipe · phone · chromium · dark · bloomed | hydrated_ms | 1119.8 | 1468.9 | x1.25/+150 | worse |
| create-recipe · phone · chromium · light · bare | hydrated_ms | 1130.6 | 1354.7 | x1.25/+150 | ok |
| create-recipe · phone · chromium · light · bloomed | hydrated_ms | 1119.1 | 1496.3 | x1.25/+150 | worse |
| error · phone · chromium · dark · bare | hydrated_ms | 1130.4 | 874 | x1.25/+150 | ok |
| error · phone · chromium · dark · bloomed | hydrated_ms | 1128.6 | 883.9 | x1.25/+150 | ok |
| error · phone · chromium · light · bare | hydrated_ms | 1132.5 | 900.6 | x1.25/+150 | ok |
| error · phone · chromium · light · bloomed | hydrated_ms | 1132.8 | 889.2 | x1.25/+150 | ok |
| home · phone · chromium · dark · bare | hydrated_ms | 1121.7 | 900.2 | x1.25/+150 | ok |
| home · phone · chromium · dark · bloomed | hydrated_ms | 1124.7 | 890.4 | x1.25/+150 | ok |
| home · phone · chromium · light · bare | hydrated_ms | 1125.7 | 918.2 | x1.25/+150 | ok |
| home · phone · chromium · light · bloomed | hydrated_ms | 1123.8 | 893.4 | x1.25/+150 | ok |
| recipe · phone · chromium · dark · bare | hydrated_ms | 1129 | 1486.1 | x1.25/+150 | worse |
| recipe · phone · chromium · dark · bloomed | hydrated_ms | 1130.6 | 1495.9 | x1.25/+150 | worse |
| recipe · phone · chromium · light · bare | hydrated_ms | 1126 | 1419.9 | x1.25/+150 | worse |
| recipe · phone · chromium · light · bloomed | hydrated_ms | 1125.3 | 1665.3 | x1.25/+150 | worse |
| search-1 · phone · chromium · dark · bare | hydrated_ms | 1124.5 | 1322.1 | x1.25/+150 | ok |
| search-1 · phone · chromium · dark · bloomed | hydrated_ms | 1129.4 | 1321.9 | x1.25/+150 | ok |
| search-1 · phone · chromium · light · bare | hydrated_ms | 1132.8 | 1363.7 | x1.25/+150 | ok |
| search-1 · phone · chromium · light · bloomed | hydrated_ms | 1126.6 | 1463.9 | x1.25/+150 | worse |
| search-3 · phone · chromium · dark · bare | hydrated_ms | 1120.2 | 1321.4 | x1.25/+150 | ok |
| search-3 · phone · chromium · dark · bloomed | hydrated_ms | 1134.7 | 1322 | x1.25/+150 | ok |
| search-3 · phone · chromium · light · bare | hydrated_ms | 1124 | 1363.6 | x1.25/+150 | ok |
| search-3 · phone · chromium · light · bloomed | hydrated_ms | 1125.7 | 1378.2 | x1.25/+150 | ok |
| settings · phone · chromium · dark · bare | hydrated_ms | 1142.3 | 1261.1 | x1.25/+150 | ok |
| settings · phone · chromium · dark · bloomed | hydrated_ms | 1131 | 1453.2 | x1.25/+150 | worse |
| settings · phone · chromium · light · bare | hydrated_ms | 1143.7 | 1254.4 | x1.25/+150 | ok |
| settings · phone · chromium · light · bloomed | hydrated_ms | 1137.2 | 1442.8 | x1.25/+150 | worse |
| sign-in · phone · chromium · dark · bare | hydrated_ms | 1142.1 | 977.5 | x1.25/+150 | ok |
| sign-in · phone · chromium · dark · bloomed | hydrated_ms | 1121.1 | 976.7 | x1.25/+150 | ok |
| sign-in · phone · chromium · light · bare | hydrated_ms | 1122.8 | 993.9 | x1.25/+150 | ok |
| sign-in · phone · chromium · light · bloomed | hydrated_ms | 1128.2 | 973.4 | x1.25/+150 | ok |
| status-404 · phone · chromium · dark · bare | hydrated_ms | 1129.8 | 869 | x1.25/+150 | ok |
| status-404 · phone · chromium · dark · bloomed | hydrated_ms | 1131.7 | 871.6 | x1.25/+150 | ok |
| status-404 · phone · chromium · light · bare | hydrated_ms | 1125.6 | 889 | x1.25/+150 | ok |
| status-404 · phone · chromium · light · bloomed | hydrated_ms | 1131.3 | 888.8 | x1.25/+150 | ok |
| variant-cook · phone · chromium · dark · bare | hydrated_ms | 1148.3 | 1485.4 | x1.25/+150 | worse |
| variant-cook · phone · chromium · dark · bloomed | hydrated_ms | 1146.3 | 1488.9 | x1.25/+150 | worse |
| variant-cook · phone · chromium · light · bare | hydrated_ms | 1150 | 1418.7 | x1.25/+150 | ok |
| variant-cook · phone · chromium · light · bloomed | hydrated_ms | 1148.5 | 1545.9 | x1.25/+150 | worse |
| account · phone · chromium · dark · bare | lcp_ms | 808 | 836 | x1.25/+150 | ok |
| account · phone · chromium · dark · bloomed | lcp_ms | 792 | 816 | x1.25/+150 | ok |
| account · phone · chromium · light · bare | lcp_ms | 808 | 836 | x1.25/+150 | ok |
| account · phone · chromium · light · bloomed | lcp_ms | 788 | 816 | x1.25/+150 | ok |
| add-variant · phone · chromium · dark · bare | lcp_ms | 416 | 428 | x1.25/+150 | ok |
| add-variant · phone · chromium · dark · bloomed | lcp_ms | 564 | 636 | x1.25/+150 | ok |
| add-variant · phone · chromium · light · bare | lcp_ms | 456 | 424 | x1.25/+150 | ok |
| add-variant · phone · chromium · light · bloomed | lcp_ms | 408 | 428 | x1.25/+150 | ok |
| create-recipe · phone · chromium · dark · bare | lcp_ms | 772 | 796 | x1.25/+150 | ok |
| create-recipe · phone · chromium · dark · bloomed | lcp_ms | 748 | 772 | x1.25/+150 | ok |
| create-recipe · phone · chromium · light · bare | lcp_ms | 784 | 804 | x1.25/+150 | ok |
| create-recipe · phone · chromium · light · bloomed | lcp_ms | 744 | 796 | x1.25/+150 | ok |
| error · phone · chromium · dark · bare | lcp_ms | 740 | 736 | x1.25/+150 | ok |
| error · phone · chromium · dark · bloomed | lcp_ms | 720 | 720 | x1.25/+150 | ok |
| error · phone · chromium · light · bare | lcp_ms | 768 | 736 | x1.25/+150 | ok |
| error · phone · chromium · light · bloomed | lcp_ms | 720 | 720 | x1.25/+150 | ok |
| home · phone · chromium · dark · bare | lcp_ms | 996 | 1116 | x1.25/+150 | ok |
| home · phone · chromium · dark · bloomed | lcp_ms | 812 | 824 | x1.25/+150 | ok |
| home · phone · chromium · light · bare | lcp_ms | 2128 | 4284 | x1.25/+150 | worse |
| home · phone · chromium · light · bloomed | lcp_ms | 804 | 828 | x1.25/+150 | ok |
| recipe · phone · chromium · dark · bare | lcp_ms | 820 | 856 | x1.25/+150 | ok |
| recipe · phone · chromium · dark · bloomed | lcp_ms | 820 | 828 | x1.25/+150 | ok |
| recipe · phone · chromium · light · bare | lcp_ms | 824 | 864 | x1.25/+150 | ok |
| recipe · phone · chromium · light · bloomed | lcp_ms | 812 | 840 | x1.25/+150 | ok |
| search-1 · phone · chromium · dark · bare | lcp_ms | 1744 | 1760 | x1.25/+150 | ok |
| search-1 · phone · chromium · dark · bloomed | lcp_ms | 1792 | 1820 | x1.25/+150 | ok |
| search-1 · phone · chromium · light · bare | lcp_ms | 1780 | 1860 | x1.25/+150 | ok |
| search-1 · phone · chromium · light · bloomed | lcp_ms | 1784 | 2948 | x1.25/+150 | worse |
| search-3 · phone · chromium · dark · bare | lcp_ms | 1748 | 1856 | x1.25/+150 | ok |
| search-3 · phone · chromium · dark · bloomed | lcp_ms | 1784 | 1800 | x1.25/+150 | ok |
| search-3 · phone · chromium · light · bare | lcp_ms | 1748 | 1856 | x1.25/+150 | ok |
| search-3 · phone · chromium · light · bloomed | lcp_ms | 1780 | 2200 | x1.25/+150 | ok |
| settings · phone · chromium · dark · bare | lcp_ms | 832 | 844 | x1.25/+150 | ok |
| settings · phone · chromium · dark · bloomed | lcp_ms | 836 | 852 | x1.25/+150 | ok |
| settings · phone · chromium · light · bare | lcp_ms | 824 | 824 | x1.25/+150 | ok |
| settings · phone · chromium · light · bloomed | lcp_ms | 832 | 852 | x1.25/+150 | ok |
| sign-in · phone · chromium · dark · bare | lcp_ms | 1012 | 752 | x1.25/+150 | ok |
| sign-in · phone · chromium · dark · bloomed | lcp_ms | 728 | 740 | x1.25/+150 | ok |
| sign-in · phone · chromium · light · bare | lcp_ms | 728 | 752 | x1.25/+150 | ok |
| sign-in · phone · chromium · light · bloomed | lcp_ms | 732 | 740 | x1.25/+150 | ok |
| status-404 · phone · chromium · dark · bare | lcp_ms | 736 | 736 | x1.25/+150 | ok |
| status-404 · phone · chromium · dark · bloomed | lcp_ms | 720 | 728 | x1.25/+150 | ok |
| status-404 · phone · chromium · light · bare | lcp_ms | 736 | 732 | x1.25/+150 | ok |
| status-404 · phone · chromium · light · bloomed | lcp_ms | 748 | 724 | x1.25/+150 | ok |
| variant-cook · phone · chromium · dark · bare | lcp_ms | 1808 | 1820 | x1.25/+150 | ok |
| variant-cook · phone · chromium · dark · bloomed | lcp_ms | 1920 | 1992 | x1.25/+150 | ok |
| variant-cook · phone · chromium · light · bare | lcp_ms | 1796 | 1724 | x1.25/+150 | ok |
| variant-cook · phone · chromium · light · bloomed | lcp_ms | 1980 | 2028 | x1.25/+150 | ok |
| search-1 · desktop · chromium · dark · bare | frames_within_16_7_pct | 58.4 | 58 | +5 | ok |
| search-1 · desktop · chromium · dark · bloomed | frames_within_16_7_pct | 54.4 | 56.6 | +5 | ok |
| search-1 · desktop · chromium · light · bare | frames_within_16_7_pct | 64.9 | 58.7 | +5 | worse |
| search-1 · desktop · chromium · light · bloomed | frames_within_16_7_pct | 63.3 | 58 | +5 | worse |
| search-1 · desktop · webkit · dark · bare | frames_within_16_7_pct | 37.6 | 32.7 | +5 | ok |
| search-1 · desktop · webkit · dark · bloomed | frames_within_16_7_pct | 35.8 | 37.3 | +5 | ok |
| search-1 · desktop · webkit · light · bare | frames_within_16_7_pct | 34.5 | 37.5 | +5 | ok |
| search-1 · desktop · webkit · light · bloomed | frames_within_16_7_pct | 35.5 | 39.8 | +5 | ok |
| search-1 · phone · chromium · dark · bare | frames_within_16_7_pct | 51.2 | 47.6 | +5 | ok |
| search-1 · phone · chromium · dark · bloomed | frames_within_16_7_pct | 47.6 | 49.1 | +5 | ok |
| search-1 · phone · chromium · light · bare | frames_within_16_7_pct | 50.5 | 51.3 | +5 | ok |
| search-1 · phone · chromium · light · bloomed | frames_within_16_7_pct | 51.8 | 50.1 | +5 | ok |
| search-1 · phone · webkit · dark · bare | frames_within_16_7_pct | 42 | 39.3 | +5 | ok |
| search-1 · phone · webkit · dark · bloomed | frames_within_16_7_pct | 41 | 42 | +5 | ok |
| search-1 · phone · webkit · light · bare | frames_within_16_7_pct | 43.3 | 40.7 | +5 | ok |
| search-1 · phone · webkit · light · bloomed | frames_within_16_7_pct | 41.4 | 40.8 | +5 | ok |
| search-3 · desktop · chromium · dark · bare | frames_within_16_7_pct | 52.3 | 52.8 | +5 | ok |
| search-3 · desktop · chromium · dark · bloomed | frames_within_16_7_pct | 55.7 | 58.8 | +5 | ok |
| search-3 · desktop · chromium · light · bare | frames_within_16_7_pct | 54.1 | 52.6 | +5 | ok |
| search-3 · desktop · chromium · light · bloomed | frames_within_16_7_pct | 55.1 | 55.8 | +5 | ok |
| search-3 · desktop · webkit · dark · bare | frames_within_16_7_pct | 43.6 | 36.7 | +5 | worse |
| search-3 · desktop · webkit · dark · bloomed | frames_within_16_7_pct | 39.2 | 37.7 | +5 | ok |
| search-3 · desktop · webkit · light · bare | frames_within_16_7_pct | 41.3 | 37.4 | +5 | ok |
| search-3 · desktop · webkit · light · bloomed | frames_within_16_7_pct | 39.4 | 38.2 | +5 | ok |
| search-3 · phone · chromium · dark · bare | frames_within_16_7_pct | 48.5 | 48.7 | +5 | ok |
| search-3 · phone · chromium · dark · bloomed | frames_within_16_7_pct | 49.1 | 46.9 | +5 | ok |
| search-3 · phone · chromium · light · bare | frames_within_16_7_pct | 48.6 | 49.8 | +5 | ok |
| search-3 · phone · chromium · light · bloomed | frames_within_16_7_pct | 48 | 51.7 | +5 | ok |
| search-3 · phone · webkit · dark · bare | frames_within_16_7_pct | 43 | 37.5 | +5 | worse |
| search-3 · phone · webkit · dark · bloomed | frames_within_16_7_pct | 41.9 | 40.9 | +5 | ok |
| search-3 · phone · webkit · light · bare | frames_within_16_7_pct | 43.4 | 40.4 | +5 | ok |
| search-3 · phone · webkit · light · bloomed | frames_within_16_7_pct | 42.1 | 41.7 | +5 | ok |
| search-1 · desktop · chromium · dark · bare | frame_p95_ms | 16.7 | 16.7 | x1.25/+4 | ok |
| search-1 · desktop · chromium · dark · bloomed | frame_p95_ms | 16.8 | 16.7 | x1.25/+4 | ok |
| search-1 · desktop · chromium · light · bare | frame_p95_ms | 16.8 | 16.8 | x1.25/+4 | ok |
| search-1 · desktop · chromium · light · bloomed | frame_p95_ms | 16.8 | 16.8 | x1.25/+4 | ok |
| search-1 · desktop · webkit · dark · bare | frame_p95_ms | 57 | 47 | x1.25/+4 | ok |
| search-1 · desktop · webkit · dark · bloomed | frame_p95_ms | 59 | 54 | x1.25/+4 | ok |
| search-1 · desktop · webkit · light · bare | frame_p95_ms | 56 | 48 | x1.25/+4 | ok |
| search-1 · desktop · webkit · light · bloomed | frame_p95_ms | 60 | 51 | x1.25/+4 | ok |
| search-1 · phone · chromium · dark · bare | frame_p95_ms | 16.8 | 16.8 | x1.25/+4 | ok |
| search-1 · phone · chromium · dark · bloomed | frame_p95_ms | 16.8 | 16.8 | x1.25/+4 | ok |
| search-1 · phone · chromium · light · bare | frame_p95_ms | 16.8 | 16.8 | x1.25/+4 | ok |
| search-1 · phone · chromium · light · bloomed | frame_p95_ms | 16.8 | 16.8 | x1.25/+4 | ok |
| search-1 · phone · webkit · dark · bare | frame_p95_ms | 48 | 18 | x1.25/+4 | ok |
| search-1 · phone · webkit · dark · bloomed | frame_p95_ms | 19 | 18 | x1.25/+4 | ok |
| search-1 · phone · webkit · light · bare | frame_p95_ms | 48 | 18 | x1.25/+4 | ok |
| search-1 · phone · webkit · light · bloomed | frame_p95_ms | 19 | 18 | x1.25/+4 | ok |
| search-3 · desktop · chromium · dark · bare | frame_p95_ms | 16.8 | 16.7 | x1.25/+4 | ok |
| search-3 · desktop · chromium · dark · bloomed | frame_p95_ms | 16.8 | 16.7 | x1.25/+4 | ok |
| search-3 · desktop · chromium · light · bare | frame_p95_ms | 16.8 | 16.8 | x1.25/+4 | ok |
| search-3 · desktop · chromium · light · bloomed | frame_p95_ms | 16.8 | 16.8 | x1.25/+4 | ok |
| search-3 · desktop · webkit · dark · bare | frame_p95_ms | 61 | 56 | x1.25/+4 | ok |
| search-3 · desktop · webkit · dark · bloomed | frame_p95_ms | 136 | 117 | x1.25/+4 | ok |
| search-3 · desktop · webkit · light · bare | frame_p95_ms | 54 | 59 | x1.25/+4 | ok |
| search-3 · desktop · webkit · light · bloomed | frame_p95_ms | 113 | 108 | x1.25/+4 | ok |
| search-3 · phone · chromium · dark · bare | frame_p95_ms | 16.8 | 16.7 | x1.25/+4 | ok |
| search-3 · phone · chromium · dark · bloomed | frame_p95_ms | 16.8 | 16.7 | x1.25/+4 | ok |
| search-3 · phone · chromium · light · bare | frame_p95_ms | 16.8 | 16.8 | x1.25/+4 | ok |
| search-3 · phone · chromium · light · bloomed | frame_p95_ms | 16.8 | 16.8 | x1.25/+4 | ok |
| search-3 · phone · webkit · dark · bare | frame_p95_ms | 45 | 47 | x1.25/+4 | ok |
| search-3 · phone · webkit · dark · bloomed | frame_p95_ms | 18 | 18 | x1.25/+4 | ok |
| search-3 · phone · webkit · light · bare | frame_p95_ms | 46 | 40 | x1.25/+4 | ok |
| search-3 · phone · webkit · light · bloomed | frame_p95_ms | 18 | 18 | x1.25/+4 | ok |
| search-1 · desktop · chromium · dark · bare | long_frames | 0 | 0 | +1 | ok |
| search-1 · desktop · chromium · dark · bloomed | long_frames | 0 | 0 | +1 | ok |
| search-1 · desktop · chromium · light · bare | long_frames | 0 | 0 | +1 | ok |
| search-1 · desktop · chromium · light · bloomed | long_frames | 0 | 0 | +1 | ok |
| search-1 · desktop · webkit · dark · bare | long_frames | 8 | 6 | +1 | ok |
| search-1 · desktop · webkit · dark · bloomed | long_frames | 10 | 15 | +1 | worse |
| search-1 · desktop · webkit · light · bare | long_frames | 9 | 7 | +1 | ok |
| search-1 · desktop · webkit · light · bloomed | long_frames | 10 | 14 | +1 | worse |
| search-1 · phone · chromium · dark · bare | long_frames | 5 | 3 | +1 | ok |
| search-1 · phone · chromium · dark · bloomed | long_frames | 3 | 3 | +1 | ok |
| search-1 · phone · chromium · light · bare | long_frames | 5 | 7 | +1 | worse |
| search-1 · phone · chromium · light · bloomed | long_frames | 5 | 3 | +1 | ok |
| search-1 · phone · webkit · dark · bare | long_frames | 13 | 6 | +1 | ok |
| search-1 · phone · webkit · dark · bloomed | long_frames | 10 | 12 | +1 | worse |
| search-1 · phone · webkit · light · bare | long_frames | 11 | 5 | +1 | ok |
| search-1 · phone · webkit · light · bloomed | long_frames | 10 | 13 | +1 | worse |
| search-3 · desktop · chromium · dark · bare | long_frames | 0 | 0 | +1 | ok |
| search-3 · desktop · chromium · dark · bloomed | long_frames | 0 | 0 | +1 | ok |
| search-3 · desktop · chromium · light · bare | long_frames | 1 | 0 | +1 | ok |
| search-3 · desktop · chromium · light · bloomed | long_frames | 1 | 0 | +1 | ok |
| search-3 · desktop · webkit · dark · bare | long_frames | 21 | 18 | +1 | ok |
| search-3 · desktop · webkit · dark · bloomed | long_frames | 43 | 36 | +1 | ok |
| search-3 · desktop · webkit · light · bare | long_frames | 19 | 20 | +1 | ok |
| search-3 · desktop · webkit · light · bloomed | long_frames | 36 | 35 | +1 | ok |
| search-3 · phone · chromium · dark · bare | long_frames | 4 | 1 | +1 | ok |
| search-3 · phone · chromium · dark · bloomed | long_frames | 5 | 2 | +1 | ok |
| search-3 · phone · chromium · light · bare | long_frames | 3 | 6 | +1 | worse |
| search-3 · phone · chromium · light · bloomed | long_frames | 5 | 2 | +1 | ok |
| search-3 · phone · webkit · dark · bare | long_frames | 10 | 16 | +1 | worse |
| search-3 · phone · webkit · dark · bloomed | long_frames | 26 | 26 | +1 | ok |
| search-3 · phone · webkit · light · bare | long_frames | 13 | 8 | +1 | ok |
| search-3 · phone · webkit · light · bloomed | long_frames | 26 | 27 | +1 | ok |
| search-1 · desktop · chromium · dark · bare | interaction_ms_max | 32 | 24 | x1.25/+25 | ok |
| search-1 · desktop · chromium · dark · bloomed | interaction_ms_max | 32 | 24 | x1.25/+25 | ok |
| search-1 · desktop · chromium · light · bare | interaction_ms_max | 32 | 32 | x1.25/+25 | ok |
| search-1 · desktop · chromium · light · bloomed | interaction_ms_max | 32 | 24 | x1.25/+25 | ok |
| search-1 · desktop · webkit · dark · bare | interaction_ms_max | 64 | 72 | x1.25/+25 | ok |
| search-1 · desktop · webkit · dark · bloomed | interaction_ms_max | 64 | 96 | x1.25/+25 | worse |
| search-1 · desktop · webkit · light · bare | interaction_ms_max | 64 | 80 | x1.25/+25 | ok |
| search-1 · desktop · webkit · light · bloomed | interaction_ms_max | 80 | 96 | x1.25/+25 | ok |
| search-1 · phone · chromium · dark · bare | interaction_ms_max | 72 | 192 | x1.25/+25 | worse |
| search-1 · phone · chromium · dark · bloomed | interaction_ms_max | 304 | 120 | x1.25/+25 | ok |
| search-1 · phone · chromium · light · bare | interaction_ms_max | 88 | 408 | x1.25/+25 | worse |
| search-1 · phone · chromium · light · bloomed | interaction_ms_max | 304 | 112 | x1.25/+25 | ok |
| search-1 · phone · webkit · dark · bare | interaction_ms_max | 128 | 128 | x1.25/+25 | ok |
| search-1 · phone · webkit · dark · bloomed | interaction_ms_max | 496 | 408 | x1.25/+25 | ok |
| search-1 · phone · webkit · light · bare | interaction_ms_max | 128 | 136 | x1.25/+25 | ok |
| search-1 · phone · webkit · light · bloomed | interaction_ms_max | 496 | 432 | x1.25/+25 | ok |
| search-3 · desktop · chromium · dark · bare | interaction_ms_max | 40 | 24 | x1.25/+25 | ok |
| search-3 · desktop · chromium · dark · bloomed | interaction_ms_max | 48 | 24 | x1.25/+25 | ok |
| search-3 · desktop · chromium · light · bare | interaction_ms_max | 48 | 32 | x1.25/+25 | ok |
| search-3 · desktop · chromium · light · bloomed | interaction_ms_max | 40 | 24 | x1.25/+25 | ok |
| search-3 · desktop · webkit · dark · bare | interaction_ms_max | 64 | 56 | x1.25/+25 | ok |
| search-3 · desktop · webkit · dark · bloomed | interaction_ms_max | 152 | 144 | x1.25/+25 | ok |
| search-3 · desktop · webkit · light · bare | interaction_ms_max | 64 | 80 | x1.25/+25 | ok |
| search-3 · desktop · webkit · light · bloomed | interaction_ms_max | 96 | 128 | x1.25/+25 | worse |
| search-3 · phone · chromium · dark · bare | interaction_ms_max | 184 | 56 | x1.25/+25 | ok |
| search-3 · phone · chromium · dark · bloomed | interaction_ms_max | 256 | 112 | x1.25/+25 | ok |
| search-3 · phone · chromium · light · bare | interaction_ms_max | 136 | 64 | x1.25/+25 | ok |
| search-3 · phone · chromium · light · bloomed | interaction_ms_max | 264 | 112 | x1.25/+25 | ok |
| search-3 · phone · webkit · dark · bare | interaction_ms_max | 152 | 128 | x1.25/+25 | ok |
| search-3 · phone · webkit · dark · bloomed | interaction_ms_max | 552 | 496 | x1.25/+25 | ok |
| search-3 · phone · webkit · light · bare | interaction_ms_max | 152 | 104 | x1.25/+25 | ok |
| search-3 · phone · webkit · light · bloomed | interaction_ms_max | 544 | 504 | x1.25/+25 | ok |

**Worse by the rule, deterministic.** `layer_area_mpx` on search-1, Chromium, phone and desktop, every visit (bare, bloomed,
mid, images off, motion reduced): phone 12 to 13.1, 2.8 to 4, 11.5 to 12.6; desktop 20.2 to 22.5, 6.3 to 8.5, 18.9 to
21.1 (margin 2%). `layers` is unchanged on every search-1 case. A filtered rerun of search-1 read the same 14 values.
The band alone causes it (proven below, under the orchestrator's Ruling 1); the only change in the search area since `c1827268` is T060's reserved band and its loads at
once (`useReservedSpace`), and the captures are taller after (phone 11544 to 20835 device rows). Removing the band would
undo FR-008's amendment, so no fix was written; it waits for a ruling. `layers` 241 to 242 on search-3 phone Chromium dark
mid read 241 again in a filtered rerun (a one-run blip, not reproducible, so no failing test was possible).
`clip_path_elements` on account reads 27 in both the recaptured `before.json` and the after run (24 is only Phase 1's CI
baseline): present at `c1827268`, nothing changed.

**Timing candidates in the T068 run (before T071).** Phone Chromium `hydrated_ms` is worse than before by the margin on 26
cases (all pages with a Vue island that is not home/search, e.g. recipe light bloomed 1125.3 to 1665.3, search-1 light bloomed
1126.6 to 1463.9, variant-cook 1148.5 to 1545.9, create-recipe 1119.1 to 1496.3, settings 1137.2 to 1442.8). `lcp_ms`: home
light bare 2128 to 4284, search-1 light bloomed 1784 to 2948. The full list is the report's "Worse than before" section.

**T069 flagged pages.** `perf:diff before after` wrote 12 look pages (`.mockups/006-performance-sweep/look-4-<page>.html`,
248 cases). Only search-1 differs: 12 cases, 38.3% to 44.2% of pixels. Every other page reads 0%. The search-1 captures
are taller after (phone light bare 11544 to 20835 device rows, desktop 1988 to 3532). Screenshot paths for T072 are under
`tests/results/perf/captures/{before,after}/search-1__<phone|desktop>__<chromium|webkit>__<light|dark>__<bare|bloomed>.png`,
with heat maps in `tests/results/perf/captures/after/heat/before-vs-after/`.

**T070 scripts-off.** `perf:scripts-off after`: 33 of 52 pairs identical, 17 header residue (largest delta 4), 2 FAIL:
search-1 phone WebKit light bloomed (7915 px, delta 205) and settings desktop WebKit dark bloomed (46 px, delta 7).
`perf:scripts-off before` reads 33 identical, 16 residue and 3 FAIL: the same two plus recipe phone Chromium light
bloomed (5321 px, delta 8, which passes after). Both failures are present at `c1827268` (see F5 for the sort pill's edge).

**The reserved band and `layer_area_mpx` (the orchestrator's Ruling 1).** On the after build, search-1, light, Chromium,
the layer tree was read as the `layers` probe reads it, then `style.minHeight` was cleared on the results region
(`section[aria-live]`) and on the sentinel after the grid, and the tree read again. The scroll height fell from 6,945 to
3,848 px on the phone and from 3,532 to 1,988 px on the desktop; the layer count did not change.

| Case | Layers | With the band (Mpx) | Band cleared (Mpx) | Before (Mpx) |
|---|---|---|---|---|
| phone bare | 113 | 13.11 | 11.97 | 12 |
| phone bloomed | 11 | 4.03 | 2.82 | 2.8 |
| desktop bare | 136 | 22.46 | 20.24 | 20.2 |
| desktop bloomed | 21 | 8.53 | 6.30 | 6.3 |

The band alone causes the rise: clearing it returns the area to before, with a remainder under 0.02 Mpx. The rise is
the consequence of the owner's approved band ("Band is fine", `mockups/README.md`) and is accepted under Ruling 1;
`budgets.json` is unchanged.

**Phone Chromium `hydrated_ms`: root cause.** Waterfalls and traces of before and after, phone profile, light ramp,
Release builds. The entry chunk `main-*.js` was the one script not modulepreloaded (`IslandHeadLinks` left it out), and
its `<script type="module">` sits at the end of the body. The harness site is plain HTTP/1.1 (six connections), the
phone profile adds 170 ms to every request, and the local Kestrel sends no compression, so the entry queued behind the
island chunks: its request started at 592 ms on home and 933 ms on recipe, against about 397 ms before, and the
heaviest pages finished it last. A fixed main-thread task at about 1,180 ms (the old widget's teardown from the blank
page Playwright opens first, 82-287 ms on bloomed visits) then fell inside the hydration window on those pages, which is
why bloomed visits read later than bare ones. From the entry's arrival to `kcc:hydrated` is 60 ms after against 90 ms
before on recipe bare, which rules out per-island mount cost, `beforeHydrate` waits and long tasks.

**T071, the entry preloaded first.** `IslandHeadLinks.For` writes the entry as the first `modulepreload`, ahead of every
island chunk (unit tests red then green; the E2E guard `PhoneRecipePage_RequestsTheEntryChunkBeforeAnyIslandChunk`
fails on the old code and passes). The bytes are the same files: first load at most 60,693 B. Three phone Chromium runs
on AC power with no sleep in any window, at loads 3.69, 3.36 and 7.16 (the third started with another repository's
virtual machine at 76% CPU; its `hydrated_ms` matches the first two within a few ms), against `before.json` by D27:

- No later than before: home, account, sign-in, error and 404 806-917 ms (before about 1,125), search-1 and search-3
  1,096-1,117, create-recipe 1,098-1,115, settings 1,035-1,044.
- Consistently later than before, inside D27's margin, on every case in at least two of three runs:
  - recipe 1,186-1,196 ms against 1,125-1,134 on every ramp and visit (about +60 ms), and images off 1,187-1,195
    against 993 (threshold 1,242);
  - variant-cook 1,141-1,197 ms against 1,146-1,163 (up to +47 ms), and images off 1,191-1,197 against 1,013
    (threshold 1,266).
- Worse by D27 on that build: add-variant light bloomed 463, 470 and 414 ms against 274 (threshold 424, worse in 2
  of 3; the 463 and 470 runs were at load 7.16, so they are kept here but are not clean and decide nothing), and
  add-variant light bloomed images off 1,190, 1,259 and 1,194 against 950 (threshold 1,187, worse in 3 of 3). The other
  add-variant cases read 407-480 ms against 281-332, later than before and inside the margin. Both are settled below.

**T071, the header's imports in one chunk.** Traces of add-variant before, after the entry preload and after this fix,
phone, light, bloomed, with the member signed in and arriving from the recipe page:

- Images off. Playwright's `page.route` bypasses the HTTP cache, so every script is fetched cold. Before, `main` was the
  one script (220 to 916 ms). After the entry preload, 17 scripts started at about 191 ms and queued on six HTTP/1.1
  connections, one 170 ms round trip per step of the queue: `AppHeader` and the five chunks of what it imports
  statically (`KccWashes`, `SignOutForm`, `KccSheet`, `useRecentlyViewed`, the shared runtime) answered between 404 and
  614 ms, ahead of add-variant's own chunks (644 to 1,174 ms), and `kcc:hydrated` waits for the last of them.
- Cached (light bloomed). Before, all 21 resources were memory-cache hits from the recipe page. After, the header's
  chunks and `main` are cached, but add-variant's own five chunks (`AddVariantView`, `SmallHero`, `laterParts`,
  `recipeWizardKeys`, `RecipeWizard`) are not, since the recipe page never loads them: one round trip, 194 to 397 ms,
  then `kcc:hydrated` at about 412 ms. Grouping the header's imports does not remove that round trip; only a prefetch
  from the recipe page or bundling the wizard into a chunk the recipe page loads would, and the orchestrator ruled both
  out under the SC-004 ruling (Option A).

The client build now writes what `AppHeader` imports statically as one shared chunk, `HeaderImports` (rolldown
`codeSplitting.groups` in `vite.config.ts`, without its dependencies, so the Vue runtime stays in `main`), and
`AppHeader` stays its own chunk, keyed by its component path in the manifest that `IslandHeadLinks` reads. The header
fetches three scripts instead of six. This is the orchestrator's decision under the owner's no-behaviour-change clarify
answer, and the islands contract records it. `clientBuild.test.ts` was red before the config change and green after;
Vitest, `build:all`, the Release build and the unit tests (752) pass, and the CSS files keep their hashes.
On this build, `perf:capture after` then `perf:diff before after` (every page type, both ramps, both profiles, bare and
bloomed; the T069 captures kept as `captures/after-t068`) reads 0% on every page but search-1, whose 12 cases read
38.3-44.2% as in T069 (the reserved band, Ruling 1). `perf:scripts-off after` reads as T070 did: 33 of 52 pairs
identical, 17 header residue, and the same 2 FAIL present at `c1827268`.

Three phone Chromium runs on AC power, no sleep in any window (run 1 11:14-11:51 at load 3.94; run 2 13:34-14:10 at
3.90, rising to 11.00 by its end while another repository's site and test browsers ran; run 3 12:57-13:34 at 1.73; a
first run 2 from 11:50 overlapped a sleep and was discarded), against `before.json` by D27: no case is worse in two of
three runs, and first load is at most 60,061 B.

- Earlier than before: home, account, sign-in, error and 404 765-880 ms (before 1,121-1,145), search-1 and search-3
  976-1,040 (1,120-1,142), recipe 1,018-1,034 (1,125-1,134), variant-cook 1,009-1,023 (1,146-1,163), create-recipe
  1,013-1,034 (1,119-1,141), settings 922-944 (1,131-1,145).
- Consistently later than before, inside D27's margin, in all three runs:
  - add-variant light bloomed 413 and 414 ms in the two clean runs against 274 (threshold 424), from the round trip for
    add-variant's own chunks above; it passes on those two, since a third clean run over the limit would still leave it
    within D27 in 2 of 3. Run 2 read 455 ms at load 11.00, a loaded run like the two at 7.16 above, kept here but not
    clean; the other add-variant visits 408-470 against 281-332;
  - images off on the island pages: add-variant 1,079, 1,026 and 1,079 against 950 (threshold 1,187), recipe
    1,030-1,057 against 993, search-1 and search-3 1,023-1,043 against 993-996, create-recipe 1,015-1,036 against 987,
    variant-cook 1,019-1,030 against 1,013.

Desktop Chromium, one run: 31-107 ms on every page against 1,000, none worse by D27.

## F16. Flagged-case reruns (the orchestrator's Ruling 2)

The 17 cases T068 flagged outside `hydrated_ms` and `layer_area_mpx` were run three times on each side, and each pass's
after value judged against the same pass's before value by D27; a case is worse only when it is worse in two of three
passes. Before passes 1 and 2 are the two valid full runs at `c1827268` (2026-11-19 16:02-17:10 at load 5.27, and
18:18-19:26 at load 1.19, both on AC power with no sleep). Before pass 3 and after passes 1-3 ran on 2026-11-20 on the
final code (`b8c5ad57`, after T071's entry preload and `HeaderImports`), one filtered `pnpm perf` per group, under the
CPU lock and `caffeinate -dims`, each started only on AC power with no process above 50% CPU, and checked against
`pmset -g log` for sleep afterwards. Every run passed on its first attempt, with no wait and no sleep.

| Pass | Side | Cases | Time | Load (1 min) |
|---|---|---|---|---|
| 3 | before | home, phone, Chromium, light, bare | 14:24:01-14:24:49 | 3.98 |
| 3 | before | search-1 and search-3, phone, Chromium | 14:24:51-14:36:32 | 4.69 |
| 3 | before | search-1, desktop, Chromium, light | 14:36:34-14:38:26 | 6.41 |
| 3 | before | search-1 and search-3, desktop, WebKit | 14:38:28-14:40:45 | 2.42 |
| 3 | before | search-1 and search-3, phone, WebKit | 14:40:47-14:44:04 | 1.72 |
| 1 | after | home, phone, Chromium, light, bare | 14:44:06-14:44:57 | 1.96 |
| 1 | after | search-1 and search-3, phone, Chromium | 14:44:59-14:56:58 | 6.95 |
| 1 | after | search-1, desktop, Chromium, light | 14:56:59-14:59:00 | 7.06 |
| 1 | after | search-1 and search-3, desktop, WebKit | 14:59:02-15:01:28 | 3.19 |
| 1 | after | search-1 and search-3, phone, WebKit | 15:01:30-15:04:56 | 3.29 |
| 2 | after | home, phone, Chromium, light, bare | 15:04:58-15:05:45 | 2.64 |
| 2 | after | search-1 and search-3, phone, Chromium | 15:05:47-15:17:45 | 5.25 |
| 2 | after | search-1, desktop, Chromium, light | 15:17:47-15:19:47 | 4.24 |
| 2 | after | search-1 and search-3, desktop, WebKit | 15:19:49-15:22:15 | 3.03 |
| 2 | after | search-1 and search-3, phone, WebKit | 15:22:17-15:25:43 | 2.21 |
| 3 | after | home, phone, Chromium, light, bare | 15:25:45-15:26:32 | 2.64 |
| 3 | after | search-1 and search-3, phone, Chromium | 15:26:34-15:38:34 | 3.18 |
| 3 | after | search-1, desktop, Chromium, light | 15:38:36-15:40:42 | 7.49 |
| 3 | after | search-1 and search-3, desktop, WebKit | 15:40:43-15:43:09 | 4.42 |
| 3 | after | search-1 and search-3, phone, WebKit | 15:43:11-15:46:38 | 3.32 |

| Case | Metric | Before (3 passes) | After (3 passes) | Margin | Worse passes | Verdict |
|---|---|---|---|---|---|---|
| home · phone · chromium · light · bare | `lcp_ms` | 2532, 2336, 2268 | 2500, 2112, 2088 | x1.25/+150 | 0 of 3 | not worse |
| search-1 · phone · chromium · light · bloomed | `lcp_ms` | 1804, 1800, 1792 | 1860, 1920, 1860 | x1.25/+150 | 0 of 3 | not worse |
| search-1 · desktop · chromium · light · bare | `frames_within_16_7_pct` | 62.8, 58.5, 61.5 | 53.5, 54.0, 56.3 | -5 pts | 2 of 3 | worse |
| search-1 · desktop · chromium · light · bloomed | `frames_within_16_7_pct` | 60.5, 59.9, 58.5 | 55.2, 58.8, 57.4 | -5 pts | 1 of 3 | not worse |
| search-3 · desktop · webkit · dark · bare | `frames_within_16_7_pct` | 41.1, 43.5, 39.5 | 36.7, 37.5, 37.2 | -5 pts | 1 of 3 | not worse |
| search-3 · phone · webkit · dark · bare | `frames_within_16_7_pct` | 41.4, 42.9, 38.6 | 39.5, 38.2, 38.7 | -5 pts | 0 of 3 | not worse |
| search-3 · phone · webkit · dark · bare | `long_frames` | 17, 19, 12 | 17, 21, 22 | +1 | 2 of 3 | worse |
| search-1 · desktop · webkit · dark · bloomed | `long_frames` | 10, 8, 10 | 13, 13, 15 | +1 | 3 of 3 | worse |
| search-1 · desktop · webkit · dark · bloomed | `interaction_ms_max` | 72, 80, 80 | 104, 120, 120 | x1.25/+25 | 3 of 3 | worse |
| search-1 · desktop · webkit · light · bloomed | `long_frames` | 8, 12, 10 | 14, 13, 14 | +1 | 2 of 3 | worse |
| search-1 · phone · chromium · light · bare | `long_frames` | 5, 4, 6 | 1, 2, 2 | +1 | 0 of 3 | not worse |
| search-1 · phone · chromium · light · bare | `interaction_ms_max` | 72, 88, 104 | 64, 56, 64 | x1.25/+25 | 0 of 3 | not worse |
| search-1 · phone · webkit · dark · bloomed | `long_frames` | 10, 10, 10 | 14, 14, 14 | +1 | 3 of 3 | worse |
| search-1 · phone · webkit · light · bloomed | `long_frames` | 10, 10, 10 | 12, 14, 14 | +1 | 3 of 3 | worse |
| search-3 · phone · chromium · light · bare | `long_frames` | 5, 3, 3 | 2, 0, 0 | +1 | 0 of 3 | not worse |
| search-1 · phone · chromium · dark · bare | `interaction_ms_max` | 104, 88, 88 | 64, 64, 56 | x1.25/+25 | 0 of 3 | not worse |
| search-3 · desktop · webkit · light · bloomed | `interaction_ms_max` | 176, 96, 128 | 96, 120, 152 | x1.25/+25 | 0 of 3 | not worse |

Seven cases are worse by the rule, all on search and all frame or interaction metrics: `frames_within_16_7_pct` on
search-1 desktop Chromium light bare; `long_frames` on search-1 WebKit (desktop dark and light bloomed, phone dark and
light bloomed) and on search-3 phone WebKit dark bare; and `interaction_ms_max` on search-1 desktop WebKit dark bloomed.
They go to T071. The other ten are not worse; `lcp_ms` on home and search-1 and every phone Chromium case read within
their margin in all three passes.

## F17. The seven search cases: cause (T071)

None of the seven is a code defect. Five are the reserved band, one is the frame-share metric's own arithmetic, and one
is run-to-run spread. Every run below took the CPU lock alone under `caffeinate -dims` on AC power, on the final code
(`f44c01eb`), on 2026-11-20 16:01-16:24 (load 1.5-3.9) and 2026-11-23 08:31-08:39 (load 2.6-4.5, and 14.6 for the last
band-off pass). The harness and source edits used were scratch edits, removed afterwards; the client build after the
last one is byte-identical to the build before the first (`wwwroot/.vite/manifest.json` compared).

**The scroll window follows the page height.** `ScrollAsync` scrolls `scrollHeight - innerHeight` in 100 px steps of
66 ms, down and back, and `long_frames` is a count over that window. The band made search-1 taller, so the window is
about twice as long after: 3,004 to 6,101 px on the phone and 1,088 to 2,632 px on the desktop. A count over a full
scroll is not like for like between two pages of different height.

**search-1, WebKit, bloomed: `long_frames`.** Each cell is one pass.

| Run | Phone light | Phone dark | Desktop light | Desktop dark |
|---|---|---|---|---|
| Before (`c1827268`, F16) | 10, 10, 10 | 10, 10, 10 | 8, 12, 10 | 10, 8, 10 |
| After, full scroll (8 passes) | 13-14 | 13-15 | 13-14 | 13-14 |
| After, scroll capped at the before distance | 12, 12 | 12, 12 | 10, 12 | 11, 7 |
| After, the band's min-height cleared before the scroll | 12, 11 | 12, 12 | 11, 8 | 10, 9 |
| After, the band's placement disabled in the build | 10, 9, 10 | 10, 10, 9 | 6, 5, 10 | 8, 5, 4 |

Over the same distance the desktop reads as before. The phone keeps 2 more long frames than before, and they go when
the band's placement is disabled (`place` returning at once and `ready` returning true in `useReservedSpace`), so they
are the placement's own work: the frame a load waits for, the card measurement and the min-height it writes. With the
placement disabled the desktop reads 4-10 against 8-12 before.

**search-1, desktop WebKit, dark, bloomed: `interaction_ms_max`.** Before 72, 80, 80; after 104-128 in eight passes;
with the placement disabled 80, 72, 80. The band causes it. On phone WebKit the same metric moves the other way:
496 before (F15), 360-392 after, 488-560 with the placement disabled.

**search-1, desktop Chromium, light, bare: `frames_within_16_7_pct`.** Before 62.8, 58.5, 61.5; after 53.5, 54.0, 56.3
(F16) and 54.2, 55.9, 57.9; after with the scroll capped at the before distance 62.3, 53.0, 56.1. Every pass reads
`frame_p95_ms` 16.7 or 16.8, at most one frame over 50 ms and a worst frame of 33-50 ms, so nothing is slow. One pass
with the frame deltas counted: 314 frames, 185 at or under 16.7 ms (58.9%), 297 at or under 16.75 ms (94.6%), 313 at or
under 16.85 ms (99.7%), none over 50 ms. Chromium coarsens frame timestamps to 0.1 ms, and the difference of two of them
is often `16.700000000000003`, which `delta <= 16.7` in `FrameStats` rejects: 112 of the 314 frames. A page at a
perfect 60 Hz reads about 77% by this arithmetic. WebKit's timestamps are whole milliseconds, so its deltas are 16, 17
and 18 ms and only the 16 ms ones count: phone search-1 reads 36% within 16.7 ms while 97% of its frames are within
18 ms. The metric measures timestamp rounding. It cannot reach the 95% target on any code, in either engine, and a
5-point move in it is not a regression. It is not changed here, because `before.json` and Phase 1's baselines were
captured with it; a repair needs both recaptured.

**search-3, phone WebKit, dark, bare: `long_frames`.** Three more passes on each side, alternating after and before:
after 19, 11, 9 and before 12, 11, 11. With F16's passes the after side reads 9-22 over seven passes and the before side
11-19 over six. The same code moves by more than the `+1` margin from pass to pass, and the alternating passes show no
difference between the sides.

**What is left for T071.** Nothing to fix in code. The search-1 WebKit cases are the cost of the reserved band the owner
approved, as the layer area is under Ruling 1: 2 more long frames over the same scroll distance on phone WebKit at a
bloomed visit, and a desktop WebKit hover of 104-128 ms against 72-80 in dark at a bloomed visit, against a landing that
no longer moves the page (FR-008). The owner's ruling of 2026-11-23 is "Keep the band", and the spec's FR-004 and
FR-009 carry the exception.

## F18. Definition of done (T068-T072, T077-T087)

Run on 2026-11-23, each heavy step under the CPU lock, on AC power with no sleep in any window. The matrix, the look
diff, scripts-off and the image build ran at `9227b788`; the two test fixes below are `fd012306`, and they change no
measured code.

| Task | Row | Result |
|---|---|---|
| T068 | Final matrix | Byte and hydration targets hold; the frame flags are the display's refresh rate and the reserved band (below) |
| T069 | Look diff | 0% of pixels on every page but search-1, whose 12 cases read 38.3-44.2%: the reserved band, approved ("Band is fine", `mockups/README.md`) |
| T070 | Scripts-off | 34 of 52 pairs identical, 15 header residue, 3 FAIL, all three present at `c1827268` (F15): search-1 phone WebKit light bloomed (7915 px), settings desktop WebKit dark bloomed (46 px), recipe phone Chromium light bloomed (5321 px, delta 8, which fails in some runs and not in others on both trees) |
| T072 | Look round | Only search-1 is flagged, and its band is the look the owner approved; no new round |
| T077 | DoD D1, D2 | `pnpm build:all` exit 0; `dotnet build KitchenCommandCenter.slnx -c Release` 0 warnings, 0 errors |
| T078 | DoD D3 | `pnpm type-check` failed on `vite.config.ts` (the island build's `as const` made the `HeaderImports` group list read-only); `satisfies BuildOptions` passes, with the same bundles. `pnpm lint` 0 errors and 6 warnings, all present at `c1827268` (`_class` and `_style` in the three form fields). `pnpm format` changed nothing |
| T079 | Integration suite | 464 of 464 pass, `DictionaryTests` included |
| T080 | Vitest and E2E | Web 2,941 pass (1 expected fail, 2 skipped), admin 17, contributions 8. E2E: see "Six suites" below. `git diff main -- tests/KCC.E2ETests` changes no existing check's assertion beyond the readiness wait, `BloomTests`' two mount checks, the tuck class test and one shortened comment in `CookModeTests`; the rest is new tests and the perf harness |
| T081 | DoD D4 | See "Six suites" below |
| T082 | DoD D6, D7 | See "Look and brand" below |
| T083 | DoD D5 | Applies: two dictionary items were added. Their boot test is `DictionaryTests`, in T079 |
| T084 | Quickstart 2-9 | See "Quickstart walk" below |
| T085 | DoD D9 | `docker buildx bake --load` exit 0; `deploy/smoke-test.sh` every line ok; `ghcr.io/th3fenriswolf/kcc-app:local` holds `/app/wwwroot/fallback/` (272 files, with its `.vite` manifest) |
| T086 | FR-011, SC-005 | The owner's pass on 2026-11-23, in Safari on an iPhone against a seeded Release site of this build on the local network: recipe search to three pages and back, a recipe and a variant, the header's search and pad cards, at a fresh visit and two minutes into one. Cook mode has no opener below 1024 px (F9), so a phone cannot open it and the variant page stands for it. The owner's answer: "Keeps up, taps answer". The pass ran before the ship review's fixes (F19) |

### The final matrix (T068)

`pnpm perf --check --before specs/009-script-hydration-cost/before.json`, 232 cases, 09:10 to 10:19 at a 1-minute load
of 3.1 falling to 1.2. `after.json` held this run until the ship review; it now holds F19's.

- **Script weight.** First load is at most 60,078 B (variant-cook; the target is 61,440) and the visit total at most
  80,669 B (add-variant; the target is 117,266), against 118,570 B on every page before. First load by page: home
  46,188, search 58,213, recipe 56,568, variant-cook 60,078, create-recipe 57,185, add-variant 57,420, account 47,963,
  sign-in 48,285, settings 51,592, error and 404 45,066.
- **Usable.** No case is over 2,000 ms on the phone or 1,000 ms on the desktop, and the report marks no `hydrated_ms`
  worse than before. Phone Chromium reads a median of 933 ms (at most 1,077) against 1,128 (at most 1,163) before.
  Add-variant on phone Chromium stays later than before inside D27's margin, 409-420 ms against 275-332, as F15 found.
- **Usable on WebKit, later inside the margin.** Unthrottled WebKit reads a median of 106 ms on the phone profile and
  104 ms on the desktop against 56 and 47 before, at most 177 ms. The paired runs below repeat it on one display
  (home 110-138 against 42-97, search-1 140-200 against 79-162, recipe 114-153 against 65-127), so it is the island
  chunks' round trips and not the display. D27's margin (x1.25 or +150 ms) holds on every case, and the targets are
  1,000 and 2,000 ms.
- **Deterministic metrics (FR-010).** `layer_area_mpx` rises on the 14 search-1 Chromium cases, as in F15 (Ruling 1).
  `layers` read 242 against 241 on search-3 phone Chromium light mid, and 241 in three reruns of each tree: a one-run
  reading, as F15 saw on the dark mid case. `css_unused_bytes` and `css_unused_pct` are report-only.

**The report's baseline was read at 120 Hz.** `perf --check` marks a timing metric against Phase 1's baseline
(`specs/006-performance-sweep/baseline.json`), whose Chromium `frame_p95_ms` is 8.4-9.3 ms: a 120 Hz cadence.
`before.json`, F15's after run and this run all read 16.7-16.8 ms, on 60 Hz displays; an earlier wording here gave
`before.json` the 120 Hz reading, which was wrong. A frame at 60 Hz is 16.7 ms, and the frame share counts a frame as
within 16.7 ms only when its coarsened timestamp difference is at most 16.7 (F17), so a page that draws every frame
reads about 50-65% in Chromium and 30-40% in WebKit, where Phase 1's baseline read 93-100%. The report marks 102
`frames_within_16_7_pct` cases, 2 `bloom_frames_within_16_7_pct` and the WebKit frame counts below as worse than that
baseline for this reason. Against `before.json` itself the share reads a few points apart in both directions, which
the paired reruns below and F19 put to the test. Four desktop Chromium cases read one frame over 50 ms (50.0 or
50.1 ms, three frames at 60 Hz).

**Paired reruns on the 60 Hz displays** (after, then before at `c1827268`, interleaved, 10:44 to 11:40; three passes for
each case the report newly marked, one for the frame-share samples; D27's margins, worse only when worse in two of three
passes):

| Cases | Metric | After | Before | Verdict |
|---|---|---|---|---|
| settings desktop Chromium, home phone Chromium, recipe desktop Chromium mid | `frames_within_16_7_pct` | 43.7-66.1 | 45.4-63.9 | the same at 60 Hz |
| same | `frame_p95_ms` | 16.7-16.8 | 16.7-16.8 (one 33.2) | the same |
| home, search-1, search-3, recipe, account, WebKit | `frames_within_16_7_pct` | 29.7-41.4 | 30.9-41.7 | the same |
| home phone WebKit bare, both ramps | `long_frames` | 14-15 | 13-15 | no worse |
| home desktop WebKit bare | `long_frames` | 9-13 | 11-12 | worse in one pass of three (dark) |
| home desktop WebKit light bare | `interaction_ms_max` | 64, 40, 24 | 16, 24, 32 | worse in one pass of three |
| search-3 phone WebKit bare | `long_frames` | light 16, 14, 23; dark 18, 20, 27 | light 17, 22, 19; dark 21, 23, 18 | worse in one pass of three each |
| recipe desktop WebKit, four cases | `interaction_ms_max` | 80-128 | 88-120 | no worse |
| recipe desktop WebKit, four cases | `frame_p95_ms` | 43-72 | 55-76 | no worse |
| account phone WebKit dark bare | `frame_p95_ms` | 136, 17, 136 | 21, 141, 141 | the same two readings on both trees |
| search-3 phone Chromium light mid | `layers` | 241, 241, 241 | 241, 241, 241 | the same |
| search-1 phone WebKit bloomed, both ramps | `long_frames` | 12-14 | 10 | the band (FR-009's exception) |
| search-1 desktop WebKit bloomed, both ramps | `long_frames` | 13-14 | 7-11 | the band |
| search-1 desktop WebKit dark bloomed | `interaction_ms_max` | 128, 96, 128 | 72, 80, 80 | the band (FR-004's exception) |
| search-1 desktop WebKit light bloomed | `interaction_ms_max` | 96, 128, 96 | 88, 80, 64 | worse in two of three: the band's hover in the light ramp |
| search-1 phone WebKit dark bare | `long_frames` | 11, 12, 10 | 8, 9, 9 | worse in two of three: the band's longer scroll at a bare visit |
| search-1 phone WebKit light bare, desktop WebKit bare | `long_frames` | 5-11 | 7-12 | worse in one pass of three at most |
| search-1 desktop WebKit bare | `interaction_ms_max` | 64-96 | 56-64 | worse in one pass of three each |

Phone Chromium is better than before in the same passes: home `interaction_ms_max` 32-104 ms against 88-248, `long_frames`
0-4 against 0-9, and search-3 light mid 120-160 ms against 240 with 3 long frames against 5-6.

Two of the band's cases fall outside the wording of the 2026-11-23 exceptions, which name the dark ramp for the hover
and a fully bloomed visit for the frames: the desktop WebKit hover on search-1 in the light ramp at a bloomed visit,
and the phone WebKit frames over 50 ms on search-1 in the dark ramp at a bare visit. Both are the band F17 measured,
and no other change touches search. The owner's answer on 2026-11-23 is "Widen exceptions": FR-004's exception now
covers both ramps and FR-009's every visit, so T071 has nothing to fix.

On the desktop profile in WebKit, the recipe page's hover reads 80-128 ms on both trees at 60 Hz, where `before.json`
read 72-96 ms, and search-3 at a bloomed visit reads 112 ms in this run against 152 ms in `before.json` (F10): FR-004's
100 ms is not met on those two pages in WebKit on the desktop, before or after this phase. The owner's answer on
2026-11-23 is "No worse than before": FR-004, FR-005, SC-001 and SC-002 now hold WebKit on the desktop profile to its
baseline, and the gap to the targets goes to Phase 3 or a later phase.

### Six suites (T080, T081)

The first run of `node tests/scripts/run.mjs` at `9227b788` failed six E2E tests, and none was a site defect:

- Two `PerfHelperTests` asserted a local `cls` limit of 0 and a target for `cls_scroll` on search-3. `budgets.json` has
  held `cls` at 0.0001 since the 2026-11-18 decision and search-3's `cls_scroll` at its baseline since FR-008 was amended.
- Four `AppendShiftTests` asserted exactly 36 recipe cards. `BackofficeTests.PublishingASubmission_PutsItOnTheSite`
  publishes a recipe into the session's one site and leaves it, so a case that ran after it counted 37. The shifts
  themselves read 0 and 0.0134, under their ceilings. The tests now ask for at least the seeded 36.

After both fixes (`fd012306`): all six suites pass, 4,668 tests with none failed and 2 skipped: the three .NET suites 1,701 (E2E, integration and unit), web Vitest 2,941 with 1 expected fail, admin 17, contributions 8.

### Look and brand (T082)

- **Brand review.** `brand-steward` over the diff found three violations, all in the wizard's pending and failed
  sheets: `StepPending` dropped `flip`, so a pending step's blank turned the other way and turned over when the step
  arrived; and the failed photo strip's sheet took tear 4 whatever the step, the same tear as step 4 beside it, with
  its blank turned the same way as the step's. Fixed test-first in `9227b788` (`stepFlip.ts`, 14 tests red, then
  green); the re-check reports no violation.
- **Both ramps.** Against a fresh seeded Release site in the Testing environment, in the Browser pane, light and dark:
  home, search, a recipe, a variant and its cook mode, sign-in and 404; every page reads `kcc:hydrated`, and the
  console holds no error. Signed in as a member made for the run, in a Playwright Chromium against the same site
  (the pane cannot hold back a chunk), light and dark: account, settings, create recipe and add variant, with no
  console error.
- **A later step's pending sheet (D30).** With the later-step chunks held, Next on the Variant step shows the step's
  own sheet in both ramps: `kcc-slip kcc-slip--wide kcc-slip--flip kcc-tear-3`, `kcc-sheet--margin`, 96 px high,
  `aria-busy="true"`, no text, while the progress bar is already on step 3. With the chunks refused, the same sheet
  shows the error line and Try again after the three attempts, with `aria-busy` gone.

### Quickstart walk (T084)

| Scenario | Result |
|---|---|
| 2. Weight (US3, SC-003) | Holds: first load at most 60,078 B, visit total at most 80,669 B, and the report shows each metric before and after |
| 3. Usable (FR-007) | Holds: at most 1,077 ms on the phone in Chromium and 177 ms in WebKit, at most 156 ms on the desktop; none worse than before by D27 |
| 4. Taps and frames (US1, US2) | Desktop Chromium: every interaction at most 56 ms, `cls` 0, `cls_scroll` 0 but on search-3 (0.0214 against 0.1644 before; phone 0.1422 against 0.2356), which FR-008 holds to its baseline. Phone Chromium is no worse than before (the look kept under C2; F11 holds the gap). WebKit is no worse than before but for the band's cases above. The frame share cannot be read on a 60 Hz display |
| 5. Cause probes (FR-003) | `ablate.sh / home base nowash reduced` on the phone profile: `base` worst entry 64 ms with 2 frames over 50 ms (worst 67 ms), `nowash` 64 ms with 3, `reduced` 0 ms with none. `base` is within 100 ms; its long frames are the phone Chromium gap F11 carries |
| 6. Look (US4, SC-006) | T069 and T070 above |
| 7. Behaviour (FR-012) | The six suites above |
| 8. Sidecar down | With the SSR process stopped, the site answers 200, links `fallback/assets/fallback-*.css` and `fallback-*.js`, and search renders in the browser with its sheets torn and washed, 13 cards and `kcc:hydrated`; the Top rated sort reloads to `?sort=rated` with 12 cards. `FallbackPageTests` covers the rest in the E2E suite |
| 9. Later parts (FR-013) | `LaterStepTests` passes: Next moves the progress at once with the step's chunk held, and the step fills in with nothing typed lost. F9 holds the press timings |

## F19. The ship review and the runs after it

Run on 2026-11-23 after F18, on the same three 60 Hz monitors, each heavy step under the CPU lock on AC power.

The ship review ran two rounds of two reviewers with a verifier, then one CodeRabbit round over `src/` (the whole
diff is over its file limit). It changed measured code in four places:

- The two wizard views carry `beforeHydrate`. The hook sat on `RecipeWizard.vue`, where the island code never reads
  it, so a wizard that opened on a typing step went live before that step's chunk had arrived.
- An island whose `beforeHydrate` rejects is rendered afresh, so a later part the server rendered and the browser never
  received shows its failed state.
- The wait for a landing page's paint no longer gives way to a frame a scroll had already asked for.
- The count of the search grid's columns, reverted since ("The band's height" below).

### Guards (SC-007)

At the reviewed code `DeterministicGuardTests` passed three runs of three, four tests each, and `TimingGuardTests`
three of three. With 15,000 random bytes imported by `Main.ts` as base64, 15.9 KB gzipped on every page (home
reads 62,243 B against 46,327), less than the 20 KB SC-007 names,
`KeySubset_StaysWithinBudget_OnEveryProbeAndByteTotal` failed in both engines and named each case, as
`home · phone · chromium · light · bloomed: js_first_load_gz_bytes 62243 > target 61440`. With the import removed
the manifest rebuilt byte for byte.

### The matrix at the reviewed code

`pnpm perf --check --before specs/009-script-hydration-cost/before.json`, 232 cases, 14:27 to 15:36. `after.json` is
this run's `results.json`, with the desktop search cases replaced as "The band's height" below says.

- **Script weight.** First load is at most 60,199 B (variant-cook) and the visit total at most 80,784 B
  (add-variant). First load by page: home 46,327, search 58,374, recipe 56,687, variant-cook 60,199, create-recipe
  57,323, add-variant 57,560, account 48,103, sign-in 48,428, settings 51,726, error and 404 45,213.
- **Usable.** Phone Chromium reads a median of 927 ms, at most 1,084. WebKit reads a median of 111 ms on the phone
  profile and 104 ms on the desktop, at most 167 ms. Desktop Chromium reads at most 78 ms.
- **Flags.** 291 against T068's 316, in the same families: the frame share and `frame_p95_ms` at 60 Hz, the band's
  cases on search-1, `clip_path_elements` on account (27 on both builds, F15) and `lcp_ms` on phone Chromium, which
  the report compares with Phase 1's budget and F16 found no worse than `before.json`. No byte, hydration, layer,
  `cls` or `cls_scroll` metric is flagged. Desktop Chromium's interactions read at most 56 ms.
- **Flags T068 did not raise.** Eight: `lcp_ms` on three phone Chromium cases, where two others dropped out; three
  WebKit `long_frames` (recipe on the desktop at a bloomed visit, 6 and 7 against 6 in `before.json` and 4-6 against
  5-6 in F18's pairs; search-1 on the desktop, excepted); and the frame share on sign-in and 404 in phone WebKit,
  paired below.

### Paired runs after the review

Three passes each, the build after and then the build at `c1827268`. `paired-runs.json` holds them.

| Cases | Metric | After | Before | Verdict |
|---|---|---|---|---|
| sign-in phone WebKit light bare | `frames_within_16_7_pct` | 45.7, 35.3, 32.4 | 41.2, 32.4, 32.4 | the same |
| 404 phone WebKit light bloomed | `frames_within_16_7_pct` | 35.3, 35.3, 33.3 | 32.4, 32.4, 35.3 | the same |
| both | `frame_p95_ms`, `long_frames` | 17-18 ms, 0 | 17-18 ms, 0 | the same |
| search-1 desktop WebKit bare, band of 4 rows | `interaction_ms_max` | dark 96, 96, 96; light 96, 96, 80 | dark 64, 64, 64; light 64, 56, 56 | worse in three of three and two of three |
| search-1 desktop WebKit dark bloomed, band of 4 rows | `frame_p95_ms` | 96, 67, 101 | 55, 57, 54 | worse in two of three |
| search-1 desktop WebKit, band of 4 rows | `long_frames` | 13-19 | 7-11 | the band (FR-009's exception) |

A machine build outside the lock ran during the last two passes of the search-1 pairs and through the sign-in and
404 pairs (1-minute load 20 to 30). Both builds shared it, and the first search-1 pass, at a load of 3, reads as the
other two.

### The band's height

The review found that the reserved band was taller than the page it holds space for. At 1440 px the grid has three
columns, the cards' tilt lifts their tops a few pixels apart, and the count of cards level with the first one reads
two columns, so twelve cards reserve six rows where they fill four: 1,544 px against 1,030. The fix counted every
card that starts above the first one's middle. Measured on search-1, WebKit on the desktop profile, three passes each:

| Band on the desktop | `interaction_ms_max`, bare | `frame_p95_ms`, bare | `long_frames`, bare | `cls_scroll`, search-3, Chromium |
|---|---|---|---|---|
| none, the build at `c1827268` | 56-64 | 57-61 | 9-11 | 0.1644 |
| 6 rows, the miscount | 56-80 | 44-50 | 9-12 | 0.0214 |
| 4 rows, one page | 80-104 | 58-67 | 12-16 | 0.0691 |

The six-row reading is today's code with the count as it was. A WebKit probe that replays the harness's wheel scroll
on all three builds shows the band standing still for the whole scroll, no page loading and the same thirteen cards,
with frames over 50 ms at about 0.27 a wheel step on each build: nothing in the page explains the cost of the shorter
band. The phone has one column and reads the same either way.

The owner's answer on 2026-11-23 is "ok lets do 6 rows": the fix is reverted, the band is the one the owner approved
("Band is fine") and walked on the iPhone, and the miscount is a known gap for Phase 3. No exception is added for it.
The owner also chose not to repeat the full matrix, so only the cases the revert can move were measured again.

### After the revert

On the code that ships: `pnpm build:all` and the Release build pass, the six suites read 4,675 passed, none failed and
2 skipped, and `DeterministicGuardTests` passes four of four.

The twenty desktop search cases were measured once more (`pnpm perf --before ... --page search-1 --profile desktop`,
then `search-3`) and replace their cases in `after.json`; every other case there is the matrix above. They read as
T068 did:

- **search-1, Chromium.** No frame over 50 ms, interactions 24-32 ms, `cls` and `cls_scroll` 0, `layer_area_mpx`
  22.5, 8.5 and 21.1 at a bare, bloomed and mid visit against 20.2, 6.3 and 18.9: the band (FR-010's exception).
- **search-1, WebKit.** At a bare visit the hover reads 72 and 56 ms against 64 in `before.json`, `frame_p95_ms`
  46-47 against 56-57, and 9 and 11 frames over 50 ms against 8 and 9. At a bloomed visit the hover reads 112 ms and
  the frames over 50 ms 13 and 15: the band's cases FR-004 and FR-009 except.
- **search-3, Chromium.** `cls_scroll` 0.0214 against 0.1484-0.1644, interactions 24-32 ms against 40-48.
- **search-3, WebKit.** 20 to 36 frames over 50 ms against 19 to 43, `frame_p95_ms` 60-109 against 54-136, hover
  64-128 ms against 64-152: no worse than before. The light bloomed hover read 128 ms against 96 in `before.json`;
  three pairs on this code read 80, 128 and 80 ms against 120, 128 and 128.

**Look and scripts-off.** `perf:capture after`, `perf:diff before after` and `perf:scripts-off after` ran at the
reviewed code, and the two search pages were captured again on the code that ships. The look diff reads 0% on every
page but search-1 (13 of its 20 cases, 29.9% to 44.2%, the band the owner approved) and one search-3 case at 0.01%.
Scripts-off reads 32 of 52 pairs identical, 17 header residue and the same 3 FAIL as at `c1827268`: search-1 phone
WebKit light bloomed (56,312 px in both runs here, 7,915 in T070 and 409 at `c1827268`; the sheet's rule lines sit a
pixel apart), settings desktop WebKit dark bloomed (46 px) and recipe phone Chromium light bloomed (5,321 px, delta 8).

**Pairs for the acceptance check.** The acceptance check named single readings in `after.json` that are over their
margin against `before.json` and had no pair. Three passes each on the code that ships, the build after and then the
build at `c1827268`:

| Case | Metric | After | Before | Verdict |
|---|---|---|---|---|
| home desktop WebKit light bloomed | `interaction_ms_max` | 64, 32, 48 | 40, 32, 80 | no worse |
| home desktop WebKit light bloomed | `long_frames` | 15, 15, 11 | 15, 14, 10 | the same |
| account phone WebKit dark bloomed | `interaction_ms_max` | 72, 72, 72 | 80, 72, 80 | no worse |
| search-1 desktop Chromium light bare | `frames_within_16_7_pct` | 59.6, 54.8, 55.2 | 61.5, 59.2, 61.9 | worse in one pass of three |
| search-1 desktop Chromium light bare | `frame_p95_ms`, `long_frames` | 16.7-16.8 ms; 0, 0, 1 | 16.7-16.8 ms; 0 | the same |
| recipe phone Chromium dark bare | `lcp_ms` | 2884, 3212, 2672 | 2588, 2496, 2772 | no worse |
| account phone Chromium light bloomed | `lcp_ms` | 1560 | 1504 | one pass; no worse |

The machine went to sleep during the later passes of the account pair and through the 404 pair, so those passes are
left out, and 404's `lcp_ms` on phone Chromium at a bloomed visit in the light ramp stays a single reading: 976 ms in
`after.json` against 748, and 820 in T068.

**`after.json` against `before.json`, read plainly.** Both were read at 60 Hz, four days apart. The median frame share
is 56.8% against 59.1% in desktop Chromium, 52.5% against 52.3% in phone Chromium, 37.1% against 41.2% in desktop
WebKit and 37.0% against 42.1% in phone WebKit; 81 cases read more than 5 points lower and 21 more than 5 higher. Every
pair run on one day reads the two builds within each other's range, which is why the owner's ruling judges frames by
pairs. `lcp_ms` on phone Chromium reads a median of about 90 ms later on the light pages (home 944 against 842, recipe
928 against 824, sign-in 832 against 738), inside D27's margin of x1.25 or +150 ms, and no later on search and the
variant page.

**Without photo import.** On a seeded site with no reader configured, the wizard opens on its typing step and
fetches it before the page is usable. Gzipped first load, from the files each page fetched before `kcc:hydrated`:
create-recipe 61,593 B against 57,150 B with import on, add-variant 63,618 B against 57,387 B. The six or seven extra
files are the step, `StepNav` and the field parts. The owner's answer on 2026-11-23 is "Target the configured site":
FR-006 is measured with import configured, and this gap goes to Phase 3.

**For Phase 3.** The frame share reads a 60 Hz display as half its frames late (F18); the counts of frames over 50 ms
grow with the distance scrolled, so a taller page reads more of them; the band's column count reads tilted cards as
fewer columns; and the wizard's first load without photo import is over 60 KB.

## F20. The first CI run on the pull request (2026-11-24)

The pull request's first CI run failed `perf` and `build-and-test` at `00844b0e`. None of the failing guards and tests
had run on a CI runner before: the 20 KB proof and the stability trial ran locally (Session 2026-11-17).

**The usable mark.** `TimingGuardTests` read `hydrated_ms` over its target on the phone in Chromium, dark and bloomed:
home 2,640.6 ms and search with three pages 2,763.3 ms against 2,000 ms times the runner's 1.23. The runs were in two
groups. A fast one, 1,262 to 1,537 ms, was level with `main` on the same runner (1,290 to 1,651 ms). A slow one, 2,107
to 2,861 ms, took every run of search, three of six on home, two of six on the recipe, and none on the account page.

The cause is the order of the first paint and the mounts on a runner that draws in software. The phone profile
rasterises on SwiftShader, whose threads share the runner's four cores with the page, so the first paint takes one to
two seconds there. Each island mounted as its chunk arrived, the header first, and its changes started a second frame;
that frame's commit waits for the first paint to finish, and the page's own island mounted only after it. `main` mounts
the whole page in one task, so no frame comes between.

It does not reproduce on this machine at any CPU slowdown, on 18 cores. It does in a Linux container with the profile's
SwiftShader flags, the phone profile at a 12x slowdown and the container held to two cores (six to twenty loads a
cell, usable mark in ms, median and worst):

| Build | Home | Search |
|---|---|---|
| `main` | 2,233 and 2,392 | 2,192 and 2,577 |
| Branch at `00844b0e` | 1,445 and 2,875 | 2,348 and 2,791 |
| Branch, every island mounted in one task | 1,390 and 2,498 | 1,634 and 2,081 |

`hydrateIslands` now waits for every island's chunk and mounts them all in one task. The header answers when the page
does, no earlier, which is also when `kcc:hydrated` is marked. One home load in twenty still read over 2,262 ms, the
CI baseline times its margin. On this machine the guard class reads home 804 to 813 ms, search with three pages 1,041
to 1,047 ms, the recipe 1,049 to 1,054 ms and the account page 842 to 850 ms, and the six suites pass.

**The six end-to-end tests.**

| Test | Read on CI | Cause | Change |
|---|---|---|---|
| `PadTests`, slash jumps to search; search remembers a viewed recipe | key press and a viewed recipe lost | the load event no longer waits for the islands | every pad test waits for the usable mark |
| `AppendShiftTests`, resting reader | next request 1,292 ms after a landing, budget 500 ms | the observer reports in the next frame, and a frame takes about a second in software | the wait is counted in frames, at most 3 (2 locally) |
| `AppendShiftTests`, short first page | shift 0.01795, pinned 0.01676 | the landing is 724 px tall on Linux and 676 px on macOS; the strip of footer is the same | the pin carries a 1.25 margin |
| `FirstPressTests`, cook mode, bare and bloomed | 160 ms and 3,448 ms, budget 100 ms | a runner with no GPU rasterises the desktop on SwiftShader | held to the budget on a GPU, recorded without one, as the phone's presses are |
