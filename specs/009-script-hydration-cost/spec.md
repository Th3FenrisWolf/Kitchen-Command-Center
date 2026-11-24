# Feature Specification: Performance Sweep, Phase 2 - Script Weight, Hydration and Interactivity

**Feature Branch**: `009-script-hydration-cost`

**Created**: 2026-11-16

**Status**: [On the project board](https://github.com/users/Th3FenrisWolf/projects/3?filterQuery=key%3A009-script-hydration-cost)

**Input**: User description: "006 phase 2". Phase 2 of the performance sweep that `specs/006-performance-sweep/spec.md`
sets out: script weight, hydration and interactivity cost.

## The sweep in phases

The sweep runs as stacked numbered specs, per the constitution's ordering rule. This spec is Phase 2. Phase 1 passed
its definition of done and is merged into `main`.

| Phase | Spec | Covers |
|---|---|---|
| 1 | 006 | Measurement baseline for every cost area, the budgets and their CI guards, and the paint, composite and animation cost of the Loose Leaf effects and stylesheet |
| 2 | 009 (this spec) | Script weight, hydration and interactivity cost |
| 3 | later | Server rendering on the Pi, caching (app and Cloudflare), the scripts Cloudflare adds to each page, fonts and images |

Phase 2 carries a phone Chromium interaction and frame gap, recorded in `specs/009-script-hydration-cost/research.md`,
to Phase 3 or a later phase.

## Where Phase 1 left it

Phase 1's measurement, budgets and guards stay as they are, and this phase moves the areas it owns from their
recorded baselines to their targets. Phase 1 recorded these numbers for this phase in
`specs/006-performance-sweep/research.md`, "For Phases 2 and 3" and "Re-measure at f24896f4":

- Every page fetches one script of 117 KB gzipped (370 KB raw), with no split by page. It carries a template compiler,
  because the browser rebuilds the page's header, body and footer from a second copy of their HTML that the server
  sends inside the page. The whole page is made interactive at once, and every shared component loads on every page.
- On the throttled phone in Chromium, the slowest interaction takes 136-200 ms against the 100 ms budget, and 128-176 ms
  on pages where the header does not tuck. Phase 1 found this to be a fixed cost of the first input on every page, and
  did not find its cause.
- Recipe search with three pages of results, phone, fully bloomed: Chromium has 5 frames over 50 ms (worst 208 ms),
  and WebKit has 28 (worst 600 ms). With reduced motion the same page has none, so the long frames come with the
  animated sheets.
- Home and sign-in on the phone in Chromium have 17-24 frames over 50 ms at a fully bloomed visit. Home keeps 12-16
  without the tuck. Phase 1 did not find their cause.
- Desktop Chromium, recipe search with three pages: interactions of 88-104 ms and 2-4 long frames.
- Layout shift while search appends a page of results is reported, not enforced, and this phase owns it.

## Clarifications

### Session 2026-11-16

- Q: What interaction budget must Phase 2 meet on the phone? → A: 100 ms, as Phase 1 set for both profiles. The plan
  finds the cause of the fixed first-input cost and removes it.
- Q: What script weight must Phase 2 reach? → A: At most 60 KB gzipped fetched before a page is usable, on every page
  type. A part needed only later, such as the recipe wizard's later steps, may load after the page is usable. The CI
  guard moves from the baseline to this target.
- Q: How is WebKit on the phone profile judged? → A: Headless WebKit on the phone must not get worse than its
  baseline, and its numbers are reported. The frame and interaction targets apply to Chromium on both profiles and to
  WebKit on desktop. The owner's manual iPhone pass judges Safari on a phone. Headless WebKit showed 2.2 s taps where
  the owner's iPhone pass on 2026-10-30 found Safari kept up.
- Q: May Phase 2 change how anything behaves? → A: No. Infinite scroll, the header tuck, the pad cards and every
  control work as they do today. A part may load after the page is usable, but each control answers within the budget
  the first time it is used.
- Q: Does Phase 2 handle the scripts Cloudflare adds to production pages? → A: No. Phase 3 takes them with its
  Cloudflare work. Phase 2 counts only the site's own script.
- Q: Which pages go to the owner in a look round? → A: Only pages where the before-and-after perceptual diff finds a
  change. Every page type is captured and diffed, in both ramps and on both profiles, and every page type runs the
  scripts-off check.

### Session 2026-11-17

- Q: Where do the 20 KB guard proof and the ten-run stability trial run? → A: Locally, against the same guard classes
  CI runs. No throwaway branch is pushed, because a pushed branch writes live board cards and issues.
- Q: If the owner keeps the washes' look and the phone in Chromium still misses the interaction or frame budget, how
  does Phase 2 close? → A: The owner is asked in one question with the look round: approve the wash change that meets
  the budget, or keep the look and carry the gap. On keeping the look, the phone Chromium interaction and frame budgets
  hold at their baselines, the gap is recorded with its measured numbers, FR-004, FR-005, SC-001 and SC-002 are
  amended for phone Chromium only, and the phase table names the gap as going to Phase 3 or a later phase.
- Q: Where is the space for search's next page of results held? → A: The browser reserves it after the page is usable,
  below the screen. First paint and the scripts-off page stay as they are today.

### Session 2026-11-18

- Q: The look round showed wash changes that would bring phone Chromium closer to its budgets. Approve one, or keep
  the look and carry the gap? → A: "Keep look, approve E": no wash change ships, and the failed-part error state is
  approved as built (`specs/009-script-hydration-cost/mockups/README.md`). The phone profile renders with SwiftShader,
  a software rasteriser the 4x CPU throttle does not reach. With hardware raster and the same throttle, the branch
  meets 100 ms with no look change:

  | Measurement (phone, 4x CPU, hardware raster) | Before (`c1827268`) | Branch |
  |---|---|---|
  | Wizard worst press | 104 ms | 64 ms |
  | Wizard Next | 72 ms | 48 ms |
  | Search-3 tap | 96 ms | 0 ms |

  FR-004, FR-005, FR-013, SC-001 and SC-002 are amended for phone Chromium only, and the gap goes to Phase 3 or a
  later phase.
- Q: A reader parked on the footer either waits 1.5 s for the next page, so that its landing shifts nothing, or gets
  it at once and sees the footer pushed down as before. Which holds? → A: Load at once, as today. Decided by the
  orchestrator under the 2026-11-16 clarify answer (infinite scroll behaves as it does today). FR-008 is amended for
  a reader parked on the footer: that landing shifts no more than before, measured by `AppendShiftTests` under the
  CPU lock (10 runs per viewport, 2026-11-18): 0.0134 at 1920x1080 in every run, and 0 or 0.1478 at 375x812, against
  0.093 and 0.27 for the same flow before the space was reserved. The space reserved below the screen stays as C3
  set it.

### Session 2026-11-19

- Q: How many local runs does the stability trial take? → A: Three of each guard class and of `pnpm perf --check`,
  and three fresh boots: the owner's decision of 2026-11-19, amending the C1 answer. A run that overlaps a machine
  sleep is discarded and run again. A timing metric flips to its target locally only if it passes three of three.

### Session 2026-11-23

- Q: The reserved search band costs headless WebKit on search with one page at a fully bloomed visit: 2 more frames
  over 50 ms across the same scroll distance on the phone profile (12 against 10), and a hover of 104-128 ms against
  72-80 ms on the desktop profile in the dark ramp. Keep the band, or tune its placement first? → A: "Keep the band".
  FR-004 and FR-009 are amended for those cases, and `specs/009-script-hydration-cost/research.md` F17 holds the
  measurements. The owner's iPhone pass (FR-011) judges Safari on a phone.
- Q: The final matrix reads the band's cost in two cases those exceptions do not name: the desktop WebKit hover on
  search with one page in the light ramp at a fully bloomed visit (96-128 ms against 64-88), and the phone WebKit
  frames over 50 ms on search with one page at a bare visit in the dark ramp (10-12 against 8-9). Widen the
  exceptions, or tune the band first? → A: "Widen exceptions". FR-004's exception covers both ramps and FR-009's
  covers every visit.
- Q: FR-004 and SC-001 hold WebKit on the desktop profile to 100 ms, but its hover reads over 100 ms on the recipe page
  (80-128 ms) and on search with three pages (112 ms) before and after this phase, and search with three pages read
  152 ms in `before.json`. How is WebKit on the desktop profile judged? → A: "No worse than before". FR-004, FR-005,
  SC-001 and SC-002 hold it to its baseline in `before.json`, as FR-009 holds WebKit on the phone, and the gap goes to
  Phase 3 or a later phase. Its frames follow the same rule: it reads 7 to 38 frames over 50 ms on search before and
  after this phase. `specs/009-script-hydration-cost/research.md` F18 holds the measurements.
- Q: The share of frames within 16.7 ms cannot be read against its 95% target on a 60 Hz display: it reads 50-65% in
  Chromium on the build before this phase and on the build after it, where Phase 1's baseline read 93-100% at a
  120 Hz cadence.
  How are frames judged for this phase? → A: "Paired runs". A frame metric passes when the build after this phase is
  no worse than the build before it in interleaved runs on the same display, worse meaning worse in two of three
  paired passes. `specs/009-script-hydration-cost/paired-runs.json` holds the paired runs. Repairing the metric so it
  reads the same at any refresh rate goes to Phase 3.
- Q: On a site with no photo-import key the recipe wizard opens on a typing step and fetches it before the page is
  usable: about 61.6 KB on create recipe and 63.6 KB on add variant, against 60 KB. Does the target cover that
  setup? → A: "Target the configured site". FR-006 and SC-003 are measured with photo import configured, as the site
  is deployed. The sizes without it are recorded in research.md F18 and go to Phase 3.
- Q: Two results rested on calls no spec line recorded: the reserved band raises the layer area of search with one
  page in Chromium (FR-010), and three scripts-off first paints differ from their scripts-enabled ones on the build
  before this phase as they do after it (SC-006). Record both as accepted? → A: "Record both". FR-010 excepts the
  band's layer area, and SC-006 excepts a scripts-off difference the build at `c1827268` shows too.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Every tap answers at once (Priority: P1)

A reader on a mid-range phone opens a recipe and taps the header's search, a pad card, the cook-mode button, a
filter or a card. The page answers the first tap as fast as every later one, with no pause after loading. On a
desktop it is the same.

**Why this priority**: the first-input cost sits on every page, so every visitor pays it, and it is the main reason
Phase 1 could not meet the interaction budget on the phone.

**Independent Test**: on the throttled phone profile, load each page type, make the first input as soon as the page
is usable, then use each control on the page once, and record each interaction delay against the 100 ms budget.

**Acceptance Scenarios**:

1. **Given** any public page that has just become usable on the throttled phone profile, **When** the reader makes
   the first tap, click or key press, **Then** its response shows within 100 ms.
2. **Given** any control on any public page, **When** the reader uses it for the first time in a visit, **Then** its
   response shows within 100 ms, including a control whose part loaded after the page became usable.
3. **Given** recipe search with three pages of results on the desktop profile, **When** the reader hovers, taps or
   types, **Then** each response shows within 100 ms.
4. **Given** a fully bloomed visit, or reduced motion on, **When** the reader interacts with any page, **Then** the
   same budget holds.

---

### User Story 2 - Search keeps up with three pages of results (Priority: P1)

A reader scrolls recipe search until three pages of results have appeared, then keeps scrolling up and down. The
page keeps up with their finger at every point, and nothing on screen jumps when a new page of results arrives.

**Why this priority**: recipe search is the page people use most, and it is the one Phase 1 left over the frame
budget in both engines.

**Independent Test**: on the throttled phone and the desktop profile, scroll recipe search until three pages have
loaded, then scroll the full page and back, in both ramps, at a bare and a fully bloomed visit, and record frame
times and layout shift. Layout shift reads 0 except a reader parked on the footer, which is no worse than before:
0.0134 at 1920x1080 and at most 0.1478 at 375x812, against 0.093 and 0.27 before.

**Acceptance Scenarios**:

1. **Given** recipe search with three pages of results on the throttled phone profile in Chromium, **When** the
   reader scrolls the full page and back, **Then** 95% of frames are within 16.7 ms and no frame is over 50 ms.
2. **Given** recipe search while the reader scrolls, **When** the next page of results arrives, **Then** no content
   on screen moves.
3. **Given** any other public page type, **When** the reader scrolls it on either profile, **Then** the same frame
   budget holds, home and sign-in included.

---

### User Story 3 - Pages become usable on less script (Priority: P1)

A reader on a slow phone connection opens any page. The page arrives already drawn by the server, and it becomes
usable after downloading only the script it needs, not the script of every other page.

**Why this priority**: every page carries the whole site's script today, and on the phone profile the download and
the start-up work come before the page answers anything.

**Independent Test**: load each page type on both profiles and record the script fetched before the page is usable,
the script fetched over the rest of the visit, and the time until the page is usable.

**Acceptance Scenarios**:

1. **Given** any public page type, **When** it loads, **Then** it fetches at most 60 KB gzipped of script before it
   is usable.
2. **Given** a visit that uses every control on a page, **When** the visit ends, **Then** the page has fetched no
   more script in total than Phase 1 recorded.
3. **Given** any public page, **When** it loads on the phone profile, **Then** it is usable within 2.0 s, within
   1.0 s on desktop, and no later than its baseline.

---

### User Story 4 - Nothing looks or works differently (Priority: P1)

The owner opens every public page before and after this phase, in both ramps, on desktop and phone, and cannot tell
which is which. Every control, the infinite scroll, the header tuck, the pad cards, cook mode and the recipe wizard
work as they did, and the washes bloom on the same clock.

**Why this priority**: the owner's condition for the sweep. This phase changes how pages become interactive, not
what readers see or can do.

**Independent Test**: run the before-and-after captures with the perceptual diff on every page type and the
scripts-off check, and run the existing end-to-end suites unchanged.

**Acceptance Scenarios**:

1. **Given** before-and-after captures of every page type, ramp and profile at a fixed visit time, **When** they are
   diffed, **Then** the diff flags no change, or the owner approved each flagged page in a look round before it
   shipped.
2. **Given** any public page loaded with scripts disabled, **When** it first paints, **Then** it matches its first
   paint with scripts enabled, by Phase 1's rule.
3. **Given** the end-to-end tests of every control, **When** they run against this phase's build, **Then** they pass
   with no change to what they check.
4. **Given** a visit of any length, **When** a page becomes usable, **Then** its washes show the same bloom state
   they showed before this phase for the same visit time.

---

### User Story 5 - The gains are held (Priority: P2)

The owner or an agent opens a pull request that adds a large library, makes every component load on every page, or
adds start-up work. CI fails the pull request and names the page, profile, engine, ramp, budget and measured number.

**Why this priority**: script weight crept up with the components, one at a time. Without a guard at the target it
will creep again.

**Independent Test**: locally, add about 20 KB gzipped of script to every page, run the guard classes CI runs, and
confirm they fail with that message; remove it and confirm they pass. No throwaway branch is pushed.

**Acceptance Scenarios**:

1. **Given** a change that takes any measured page over 60 KB gzipped of script before it is usable, **When** CI
   runs, **Then** the build fails and reports the page, profile, engine, ramp, budget and measured number.
2. **Given** an unchanged codebase, **When** the guard classes CI runs are run three times locally, **Then** every
   guard passes every time, and CI passes on the pull request.
3. **Given** a budget this phase meets, **When** this phase lands, **Then** its guard enforces the target in place of
   the recorded baseline.

---

### Edge Cases

- A reader taps a control whose part has not arrived yet, on a slow connection: the input is not lost, the control
  shows a response within the interaction budget, and it completes when its part arrives.
- A reader taps before the page is usable: links work as they do today, and the time to usable is held by its own
  budget.
- Scripts fail to load or are blocked: the page still reads and its links still work, as they do with scripts off.
- The owner places an interactive block on home, or any page, through the backoffice: it works wherever it is placed,
  with no code change.
- A signed-in member's pages carry member-only controls: they meet the same budgets, measured with the E2E test
  member locally.
- The recipe wizard has several steps: a later step may load after the wizard is usable, and moving to it still
  answers within the interaction budget.
- Recipe search grows by a page as the reader scrolls: the frame and layout-shift budgets hold at one page and at
  three.
- A page that becomes usable sooner or later than before: the washes' bloom follows the visit's clock, not the moment
  the page became usable.
- Reduced motion on: the same budgets hold, and the look holds without motion.
- Production pages also carry scripts Cloudflare adds: they appear in the live report, apart from the site's own
  script, and no Phase 2 budget counts them.

## Requirements *(mandatory)*

### Functional Requirements

**Measurement**

- **FR-001**: Phase 2 MUST measure with Phase 1's measurement and matrix: every public page type, both profiles, both
  engines, both ramps, a bare and a fully bloomed visit, images on and off, reduced motion on and off. The report MUST
  show each metric before and after this phase, where "before" is `main` when this phase starts.
- **FR-002**: The measurement MUST record, for each page profile, the script fetched before the page is usable, the
  script fetched over the rest of the measured visit, and the time until the page is usable.
- **FR-003**: Before it chooses a fix, Phase 2 MUST find and record what makes up the fixed cost of the first input on
  the throttled phone, and what makes up the long frames on recipe search, home and sign-in.

**Budgets**

- **FR-004**: Every tap, click, hover and key press MUST show its response within 100 ms on every public page type,
  in both ramps, at a bare and a fully bloomed visit, with reduced motion on and off: on the desktop profile in
  Chromium. On the desktop profile in WebKit and on the phone profile in Chromium, no interaction MAY be slower than
  its baseline in `specs/009-script-hydration-cost/before.json`; the gap to 100 ms is recorded with its measured
  numbers in research.md and goes to Phase 3 or a later phase. The first input after a page becomes usable and the
  first use of each control are included. On the desktop profile in WebKit, the hover on recipe search with one page
  of results at a fully bloomed visit is recorded and not held to its baseline: it is the cost of the reserved band
  (Clarifications, 2026-11-23).
- **FR-005**: Scrolling and blooming MUST hold 95% of frames within 16.7 ms and no frame over 50 ms on every public
  page type, recipe search with three pages of results included, in the same cases as FR-004. On the desktop profile in
  WebKit and on the phone profile in Chromium, the share of frames within 16.7 ms and the long frames MUST NOT get
  worse than their baseline in `before.json`; the gap to the target is recorded and goes to Phase 3 or a later phase.
  On the desktop profile in WebKit, the frames over 50 ms on recipe search with one page of results are excepted as
  FR-009 excepts them. On a display that refreshes at 60 Hz the share of frames within 16.7 ms cannot be read against
  95%, so every frame metric is judged there against the build before this phase in paired runs on the same display
  (Clarifications, 2026-11-23).
- **FR-006**: A page MUST fetch at most 60 KB gzipped of script before it is usable, on every public page type. Script
  fetched after the page is usable counts toward no first-load budget, but a visit MUST NOT fetch more script in total
  than Phase 1 recorded for the same page (117,266 B gzipped). The target is measured with photo import configured,
  as the site is deployed (Clarifications, 2026-11-23).
- **FR-007**: Every page MUST become usable within 2.0 s on the phone profile and 1.0 s on the desktop profile, and no
  later than its baseline.
- **FR-008**: Layout shift MUST stay at zero after first paint, including while recipe search appends a page of
  results, except a reader parked on the footer, which is no worse than before: 0.0134 at 1920x1080 and at most
  0.1478 at 375x812, against 0.093 and 0.27 before (Clarifications, 2026-11-18).
- **FR-009**: Headless WebKit on the phone profile MUST NOT get worse than its baseline on any metric, and its numbers
  MUST appear in the report. The targets in FR-004 and FR-005 do not apply to it. The frames over 50 ms on recipe
  search with one page of results are excepted at every visit: they are the cost of the reserved band, 2 more over the
  same scroll distance at a fully bloomed visit, and the measured scroll is about twice as long (Clarifications,
  2026-11-23).
- **FR-010**: No metric Phase 1 holds MAY get worse than its baseline: stylesheet weight, layers and other paint
  counts, load milestones, server response, and font and image weight. The layer area of recipe search with one page
  of results in Chromium is excepted: the reserved band raises it (Clarifications, 2026-11-23).
- **FR-011**: At acceptance the owner MUST run a manual pass in Safari on an iPhone against this phase's build: scroll
  recipe search until three pages have loaded and back, open a recipe and cook mode, and use the header's search and
  pad cards, at a fresh visit and two minutes into one, and record whether it keeps up and whether taps answer at
  once.

**Behaviour and rendering**

- **FR-012**: Every control and feature MUST behave as it does today, including the infinite scroll, the header tuck,
  the pad cards, recently viewed, cook mode, the recipe wizard and the sign-in and account forms.
- **FR-013**: A part of a page MAY load after the page is usable, provided it answers within the interaction budget
  the first time it is used and an input made before it arrives is not lost. On the phone profile in Chromium, the
  recipe wizard's first presses are recorded, every one of them measured, and not held to 100 ms; the gap is recorded
  with its measured numbers in research.md and goes to Phase 3 or a later phase.
- **FR-014**: Every public page MUST keep server-side rendering, and its first paint with scripts disabled MUST match
  its first paint with scripts enabled: in light for a signed-out visitor, and in the saved ramp for a signed-in
  member. With scripts off, content and links work as they do today.
- **FR-015**: Every interactive part the owner can place through the backoffice MUST keep working wherever the owner
  places it, and no content the owner edits MAY need a code change to render.
- **FR-016**: Phase 2 MAY change how pages render on the server and become interactive in the browser, including how
  a page's content reaches the browser, provided FR-012 to FR-015 hold.
- **FR-017**: The washes MUST bloom on the visit's clock: for the same visit time a page shows the same bloom state,
  whenever it becomes usable.

**Look**

- **FR-018**: Every public page MUST read the same before and after, in both ramps, on both profiles, at a bare and a
  fully bloomed visit. Automated before-and-after captures with the perceptual diff cover every page type. A page
  whose diff flags a change goes to the owner in a `/speckit-preview-run` look round, and it does not ship until the
  owner approves it. A page with no flagged change needs no look round.
- **FR-019**: The kit's enforcement tests, the contrast table and the no-drawing-script rule MUST stay green.

**Guards**

- **FR-020**: When this phase lands, CI MUST enforce the 60 KB first-load script target and the time-to-usable
  targets in place of their recorded baselines, on the key subset Phase 1 defined, and a failure MUST name the page,
  profile, engine, ramp, budget and measured number.
- **FR-021**: The interaction and frame budgets MUST enforce their targets in the local check once a three-of-three
  stability trial passes. In CI they MAY stay report-only where they are not stable, and for each one that does, the
  plan MUST name a deterministic stand-in that CI enforces, such as the script a page runs before it is usable or the
  number of parts made interactive on a page.
- **FR-022**: The guards MUST pass on every run of an unchanged codebase, and live-site numbers MUST NOT fail CI.

**Docs**

- **FR-023**: `CLAUDE.md`, `docs/brand/kit.md` and the measurement docs MUST describe any change to how pages render
  on the server, reach the browser and become interactive, and the new budgets.

### Key Entities

- **Page profile**: as in Phase 1: a public page type, a device profile, an engine, a ramp, a visit state, images on
  or off, and reduced motion on or off. Each is one measured case.
- **Usable**: the moment every control the page shows on load carries out its action when used. A link that works
  without script, as it does with scripts off, does not make a page usable. A part the page does not show on load,
  such as the recipe wizard's later steps, may load after it (FR-013). The time to usable is measured from navigation
  to that moment, the same way on every run.
- **First-load script**: the script, gzipped, that a page fetches before it is usable.
- **Later script**: the script a page fetches after it is usable, during the measured visit.
- **Budget**: as in Phase 1: a metric, a limit, and whether it is enforced at the target or at the baseline.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On every public page type, both ramps, at a bare and a fully bloomed visit, every measured tap, click,
  hover and key press shows its response within 100 ms, the first input after load included: on the desktop profile
  in Chromium; on the desktop profile in WebKit and on the throttled phone in Chromium, no measured interaction is
  slower than its baseline, and the recorded gap to 100 ms goes to Phase 3 or a later phase.
- **SC-002**: In the same cases, scrolling recipe search with three pages of results, and every other page type,
  keeps 95% of frames within 16.7 ms and no frame over 50 ms, with no layout shift as results arrive. On the desktop
  profile in WebKit and on the throttled phone in Chromium, frames are no worse than their baseline, and the recorded
  gap goes to Phase 3 or a later phase; the zero layout shift holds there as everywhere. On a 60 Hz display frames
  are judged against the build before this phase in paired runs, as FR-005 sets.
- **SC-003**: Every public page type fetches at most 60 KB gzipped of script before it is usable, down from
  117 KB, and no visit fetches more in total than 117 KB, with photo import configured.
- **SC-004**: Every page is usable within 2.0 s on the phone and 1.0 s on desktop and no later than its baseline, no
  metric Phase 1 holds is worse than its baseline, and headless WebKit on the phone is no worse than its baseline on
  any metric, each with the exceptions FR-004, FR-005, FR-009 and FR-010 name.
- **SC-005**: The owner's iPhone pass in Safari records that recipe search, a recipe, cook mode and the header keep
  up and answer taps at once, at a fresh visit and two minutes into one.
- **SC-006**: The perceptual diff flags no page, or the owner approves every flagged page; every scripts-off first
  paint matches its scripts-enabled one, or differs as it does on the build at `c1827268`; and the end-to-end suites
  pass with no change to what they check.
- **SC-007**: About 20 KB gzipped of script added to every page fails the guard classes CI runs, run locally, with a
  message naming page, profile, engine, ramp, budget and number; on an unchanged codebase those guards pass three local
  runs out of three, and CI passes on the pull request.

## Assumptions

- Phase 1 is merged into `main`, so this branch starts from `main` and the "before" numbers are `main` when this phase
  starts.
- The dependency update that moves the build to pnpm and Node 26 reaches `main` before this phase is planned, and
  this branch is rebased on it, so the plan reads the build this phase is measured on.
- The profiles, engines, matrix, key subset and seeded content are Phase 1's: the phone profile is Fast 4G (about
  9 Mbps down, 170 ms round trip) with the CPU slowed 4x, and search holds three pages of 12 recipes.
- Interaction delay is measured from the input to the next paint that shows its response, as in Phase 1.
- The site ships no third-party script of its own. The scripts Cloudflare adds in production belong to Phase 3.
- The server's response time is Phase 3's to improve. This phase must only not make it worse.
- No paid product or recurring cost is added: measurement runs in the existing CI and on the owner's Mac.
- Phase 3 is specified only after this phase passes its definition of done, from this branch.
