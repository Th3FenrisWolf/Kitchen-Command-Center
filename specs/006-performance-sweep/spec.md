# Feature Specification: Performance Sweep, Phase 1 - Baseline, Budgets and Paint Cost

**Feature Branch**: `006-performance-sweep`

**Created**: 2026-10-21

**Status**: Draft

**Input**: User description: "Site-wide performance sweep: make the public site lightning fast again. Recent work
(paper piles 002, type scale 004, paper rule 003, ink wash bloom 005, plus fixes) added styling - torn clip-path
sheets, blanks under every sheet, SVG fibre/fall filters, grain/crayon, washes blooming over a visit, hover lifts -
and the site now feels really slow. Scope: measure first-load and interaction performance on every public page type
(home, recipe search, recipe detail, member pages) in both ramps, on desktop and phone, against the Pi-hosted
production build; find the costs (render/paint/composite from filters and clip-paths, CSS size, JS bundle and
hydration, SSR time on the Pi, fonts, images, server response); fix them without changing the Loose Leaf look; and
add guards so regressions are caught. Branch from 005-ink-wash-bloom so the bloom is measured too."

## The sweep in phases

The sweep is too big for one pull request, so it runs as stacked numbered specs, per the constitution's ordering
rule. This spec is Phase 1. Each later phase is specified only after the previous one passes its definition of done.

| Phase | Spec | Covers |
|---|---|---|
| 1 | 006 (this spec) | Measurement baseline for every cost area, the budgets and their CI guards, and the paint, composite and animation cost of the Loose Leaf effects and stylesheet |
| 2 | later | Script weight, hydration and interactivity cost |
| 3 | later | Server rendering on the Pi, caching (app and Cloudflare), fonts and images |

Phase 1 measures every area, including the ones Phases 2 and 3 fix, so each later phase starts from recorded
numbers and is held by the same guards.

## Clarifications

### Session 2026-10-26

- Q: How is "reads the same" judged when an effect is rebuilt? → A: By the owner's eye. Automated side-by-side
  captures with a perceptual diff highlight changed regions, and the owner approves every page type in a preview
  round. Nothing ships on the diff alone.
- Q: What does "holds 60 frames a second" mean? → A: 95% of frames within 16.7 ms and no frame over 50 ms, on the
  throttled phone profile.
- Q: Which network does the phone profile use? → A: Fast 4G (about 9 Mbps down, 170 ms round trip) with the CPU
  slowed 4x.
- Q: Which browser engines are measured? → A: Chromium and WebKit, locally and in the CI guards where stable.
- Q: Which account signs in for the live-site measurement? → A: A dedicated measurement member the owner creates and
  approves on production. Its credentials live in the owner's local environment, never in the repository.
- Q: What runs in CI on every pull request? → A: A key subset: home, recipe search with three pages of results,
  recipe detail, cook mode and one member page, in both ramps, on the phone profile, at a fully bloomed visit, in
  both engines. The full matrix runs in the local report.
- Q: With scripts off, a signed-out visitor whose device is set to dark gets light. Does that stay? → A: Yes. With
  scripts on they still follow the device's dark setting; with scripts off the server renders light. The scripts-off
  check compares signed-out pages in light, and compares dark for a signed-in member with a saved Dark ramp.
- Q: Seeded content gives 25 recipes and no images. Is the test seed extended? → A: Yes, to 36 recipes with images,
  so search holds three full pages and the image cases run. E2E tests that count recipes are updated.
- Q: Is real Safari checked, given headless WebKit may composite in software? → A: Yes. The owner runs a short
  manual pass on an iPhone at acceptance. Headless WebKit stays the report and CI engine.
- Q: Does every page type look the same on desktop after the Phase 1 fixes? → A: Yes, approved in look round 1;
  see mockups/README.md
- Q: Phone LCP on search-1, search-3 and variant-cook regressed by about 380 ms, because a duotone tile icon became
  the LCP element; is it fixed in Phase 1? → A: No: font subsetting fixes it in Phase 3, and Phase 1 records the
  regression (research.md, "Diagnosis after the re-measure")

### Session 2026-10-27

- Q: Should a wash whose bloom finished before load stop animating, so the settled Chromium look matches the first
  paint? → A: Yes, approved in look round 2: a wash rests at its bloom's end and fills backwards only; see
  mockups/README.md
- Q: Do the phone captures look the same after the Phase 1 fixes? → A: Yes, approved from the first phone set; see
  mockups/README.md
- Q: Does the header bar's redraw difference in the Chromium scripts-off pairs block the check? → A: No: up to 6
  levels from a repaint of the header's washes is engine noise, accepted; see research.md
- Q: Do the stepped bloom and the halved-point tear look the same as before? → A: Yes, both approved in the
  bloom-and-tear round; the halved tear ships; see mockups/README.md
- Q: Do the baked washes read the same as the live filter? → A: Yes, approved in look round 3; they ship
- Q: How visible should the washes be? → A: Quieter in light (strength .55 to .42), stronger in dark (.16 to .18,
  with the dark soft ink lifted from L .735 to .76 to hold 4.5:1); chosen in look round 4; see mockups/README.md
- Q: Does SC-002 have to be met on the phone profile in Phase 1? → A: No: after the tuck fix the phone's worst
  interactions are 136-200 ms and the no-tuck floor is 128-176 ms, so SC-002 on the phone carries to Phase 2
  (JS and hydration); desktop meets it except Chromium search-3 (below)
- Q: The baked textures add 7.2 KB gzipped to the stylesheet (36,754 to 43,931 B); how is that weight handled? → A:
  Move the textures out of the stylesheet into separate cached PNG files, preloaded so the washes still show at first
  paint, and judge the result in a look round
- Q: May the branch be pushed before the close-out to run the ten-run CI stability trial (T020, T021)? → A: Yes; the
  pull request still opens only from the recomposed branch
- Q: Do `KCC_PERF_LIVE_RECIPE` and `KCC_PERF_LIVE_VARIANT` stay? → A: Yes, optional, set only in the owner's local
  environment, and accepting a site path only
- Q: The texture files move 13,720 B into image bytes; how does the image budget treat them? → A: Raise the
  `image_bytes` baselines by 13,720 B on every images-on case; the inline and file textures look the same (look round 5)
- Q: SC-001 is not met on search-3 phone in either engine after Phase 1; is it fixed in Phase 1? → A: No: Chromium has
  5 frames over 50 ms and WebKit 28, the same on bare visits, so SC-001 carries to Phase 2 (research.md, "Re-measure
  at f24896f4")
- Q: Desktop Chromium search-3 regressed to 104 ms interactions and 2-4 long frames; is it fixed in Phase 1? → A: No,
  it carries to Phase 2 with the interaction work; every other desktop page meets SC-002, WebKit included
- Q: `clip_path_elements` rises on 214 cases because the fall is a clipped static element; how is it budgeted? → A:
  Both baselines hold the measured counts

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Scrolling and tapping feel instant (Priority: P1)

A reader on a mid-range phone opens recipe search and scrolls through the results, hovers or taps cards, opens
filters, and moves to a recipe. The page keeps up with their finger: no stutter while scrolling, no lag between a
tap and the page's answer, and no pause while the ink washes bloom. On a desktop it is the same, only more so.

**Why this priority**: this is where the recent styling hurts most. Every card carries several filtered, clipped,
shadowed and blended layers, and a search page shows a dozen or more cards, so the cost multiplies on the page
people use most.

**Independent Test**: on the throttled phone profile, scroll the recipe search page from top to bottom and back,
hover or tap six cards, open and close the filters, and record frame times and interaction delays against the
budgets.

**Acceptance Scenarios**:

1. **Given** recipe search with its first page of results on the throttled phone profile, **When** the reader
   scrolls the full page and back, **Then** the page holds the frame-rate budget, with no frame over the long-frame
   limit.
2. **Given** any card on any public page, **When** the reader hovers, focuses or taps it, **Then** the visible
   response begins within the interaction budget and the lift still reads as it does today.
3. **Given** a visit in progress with washes blooming, **When** the reader scrolls or interacts, **Then** the bloom
   adds no frame over the long-frame limit.
4. **Given** recipe search after loading further results by scrolling, **When** the reader keeps scrolling,
   **Then** the frame-rate budget still holds with three pages of results on screen.

---

### User Story 2 - The site looks exactly as it does today (Priority: P1)

The owner opens every public page before and after the sweep, in the light and the dark ramp, on desktop and phone,
and cannot tell which is which. The torn edges, the blank under every sheet, the shadows, the grain and crayon on
the desk, the washes and their bloom, the tape and tiles, and the hover lift all read the same. With scripts turned
off, the first paint is identical to the first paint with scripts on.

**Why this priority**: the owner's condition for the sweep. An effect may be rebuilt by cheaper means, but the look
it produces must not change.

**Independent Test**: capture each public page type, both ramps, desktop and phone, at a fixed visit time, before
and after, and compare side by side; capture the same pages with scripts disabled and compare against the
scripts-on first paint.

**Acceptance Scenarios**:

1. **Given** before-and-after captures of each page type, ramp and device at a fixed visit time, **When** they are
   compared, **Then** they read the same to the owner, and any difference the comparison finds was shown to the
   owner in a preview round and approved before it shipped.
2. **Given** any public page loaded with scripts disabled, **When** it first paints, **Then** it matches the first
   paint of the same page with scripts enabled, in light for a signed-out visitor and in the member's saved ramp for
   a signed-in one.
3. **Given** a rebuilt effect, **When** it renders in both ramps, **Then** text contrast still meets WCAG AA and the
   kit's enforcement tests stay green.

---

### User Story 3 - A regression fails the pull request (Priority: P1)

The owner or an agent opens a pull request that adds a heavy effect, a large stylesheet rule set or a costly
animation. CI fails the pull request and names the page, the device profile, the budget and the measured number,
before anything reaches the Pi.

**Why this priority**: the site got slow one reasonable-looking change at a time. Without a guard it will again.

**Independent Test**: on a throwaway branch, add an expensive effect to every card and confirm CI fails with a
message naming the page, profile, budget and number; remove it and confirm CI passes.

**Acceptance Scenarios**:

1. **Given** a change that pushes any measured page over a budget, **When** CI runs, **Then** the build fails and
   reports which page, profile, budget and measured number.
2. **Given** a change that stays within every budget, **When** CI runs on an unchanged codebase several times,
   **Then** the guards pass every time: they do not fail on noise.
3. **Given** a cost area that Phase 2 or 3 owns and that is still over its target, **When** CI runs, **Then** its
   guard fails only on regression past the recorded baseline, and the target replaces the baseline when that phase
   lands.

---

### User Story 4 - The owner can see where the time goes (Priority: P2)

The owner runs one command, or reads one report, and sees for every public page type, ramp and device profile: load
milestones, interaction delay, frame times while scrolling and blooming, stylesheet and script weight, server
response time, and font and image weight, each against its budget. The same report exists for the live Pi through
Cloudflare.

**Why this priority**: the baseline tells Phases 2 and 3 where to spend effort, and the live report shows whether
the local numbers hold in the real deployment.

**Independent Test**: run the measurement against the local production build and against the live site, and
confirm each report lists every page type, ramp and profile with every metric and its budget.

**Acceptance Scenarios**:

1. **Given** the local production build, **When** the owner runs the measurement, **Then** a report lists every
   page type in both ramps on both profiles, with each metric beside its budget and its baseline.
2. **Given** the live site, **When** the owner runs the measurement against it, **Then** the same report is produced
   from the Pi through Cloudflare, signed out and signed in.
3. **Given** the baseline captured before any Phase 1 fix, **When** Phase 1 finishes, **Then** the report shows the
   before and after numbers side by side.

---

### Edge Cases

- A visit at five minutes or more renders every wash fully bloomed: the heaviest wash state is measured, not only a
  bare first visit.
- Recipe search with infinite scroll grows the page; measurement covers one page of results and three.
- The dark ramp blends washes differently from the light ramp; each ramp is measured on its own.
- A reader who prefers reduced motion gets the same look without motion; the guards cover that mode too.
- A recipe with no image draws an accent tile instead; pages are measured with and without images.
- Member pages need a signed-in member: the local measurement signs in with the E2E test member, and the live
  measurement with the dedicated measurement member.
- A hover or focus lift on a touch device: the tap response and any lift are measured on the phone profile.
- The Pi's own response time varies with load and the SSR cache; the live report states it as a range over several
  runs, and live numbers never fail CI.

## Requirements *(mandatory)*

### Functional Requirements

**Measurement**

- **FR-001**: The project MUST provide one repeatable measurement that runs the production build locally on a
  throttled phone profile and a desktop profile, in the Chromium and the WebKit engine, in the light and the dark
  ramp, and records, per public page type:
  load milestones, layout shift, interaction delay, frame times while scrolling and while washes bloom, stylesheet
  and script weight, server response time, and font and image weight.
- **FR-002**: The public page types measured MUST be: home, recipe search (one page of results and three), recipe
  detail, variant detail with cook mode, create recipe, add variant, account, sign-in, account settings, and the
  error and status pages.
- **FR-003**: Each page MUST be measured at a bare first visit and at a fully bloomed visit, with and without recipe
  images, and with reduced motion on and off.
- **FR-004**: The same measurement MUST run against the live site through Cloudflare, signed out and signed in with
  a dedicated measurement member that the owner creates and approves on production, and report server response as a
  range over several runs. The member's credentials MUST come from the owner's local environment, never from a
  tracked file.
- **FR-005**: The baseline MUST be captured before any Phase 1 fix and committed with this spec, so later phases and
  the guards compare against it.

**Budgets**

- **FR-006**: The budgets MUST be, on the throttled phone profile (Fast 4G, about 9 Mbps down and 170 ms round
  trip, with the CPU slowed 4x): the page is usable and its largest content shows within 2.0 s. On the desktop
  profile: within 1.0 s. On both profiles: every tap, click, hover or key press shows its response within 100 ms,
  scrolling and blooming hold 60 frames a second (95% of frames within 16.7 ms and no frame over 50 ms), and layout
  shift stays at zero after first paint.
- **FR-007**: Phase 1 MUST bring every page type within the frame-rate and interaction budgets on both profiles in
  both ramps, at a bare and a fully bloomed visit.
- **FR-008**: Phase 1 MUST NOT make any load milestone, stylesheet or script weight, or server response worse than
  the baseline.

**Paint and render cost**

- **FR-009**: Phase 1 MUST cut the paint, composite and animation cost of the Loose Leaf effects: the torn sheets,
  the blank under every sheet and both their shadows, the grain and crayon, the washes with their blending and
  bloom, the tiles and tape, and the hover lift.
- **FR-010**: Phase 1 MUST cut stylesheet weight that the pages do not use, and the cost of applying it.
- **FR-011**: An effect MAY be rebuilt by any cheaper means, including a pre-made texture in place of a live one, a
  static layer in place of an animated one, or fewer layers, provided the result reads the same.
- **FR-012**: The sweep MAY change how pages render and hydrate, but every public page MUST keep server-side
  rendering, and its first paint with scripts disabled MUST match its first paint with scripts enabled. A signed-out
  visitor gets light with scripts off and follows the device's dark setting with scripts on, so a signed-out page is
  compared in light; a signed-in member with a saved ramp is compared in that ramp.

**Look**

- **FR-013**: Every public page MUST read the same before and after, in both ramps, on both profiles, at a bare and
  a fully bloomed visit. The owner judges it by eye: automated side-by-side captures with a perceptual diff highlight
  changed regions, every page type is shown to the owner in a `/speckit-preview-run` look round, and nothing ships
  until the owner approves it. A diff score alone never passes a page.
- **FR-014**: The kit's enforcement tests, the contrast table and the no-drawing-script rule MUST stay green.
  Generated tear presets MUST still come from their generator.

**Guards**

- **FR-015**: CI MUST fail a pull request when any page it measures goes over a budget it meets, and MUST report
  the page, profile, engine, ramp, budget and measured number. CI measures a key subset on every pull request: home,
  recipe search with three pages of results, recipe detail, cook mode and one member page, in both ramps, on the
  phone profile, at a fully bloomed visit, in both engines. The full matrix runs in the local report.
- **FR-016**: For a cost area still over its target (owned by Phase 2 or 3), CI MUST fail only on regression past
  the recorded baseline. The phase that brings the area within target replaces the baseline with the target.
- **FR-017**: The guards MUST be stable: on an unchanged codebase they pass on every run. A guard that cannot be
  made stable at a budget MUST measure a deterministic stand-in for it, such as weight or the number of costly
  layers on a page, and the plan MUST say which.
- **FR-018**: Live-site numbers MUST NOT fail CI; they appear in the live report only.
- **FR-021**: At acceptance the owner MUST run a short manual pass in Safari on an iPhone: scroll recipe search and
  open a recipe, at a fresh visit and two minutes into one, and record whether it keeps up.

**Cloudflare and docs**

- **FR-019**: Any Cloudflare setting the sweep recommends MUST be on the free plan, MUST be listed in the spec of
  the phase that recommends it, is applied by the owner by hand, and MUST be recorded in `docs/hosting/runbook.md`.
- **FR-020**: `docs/brand/kit.md` and `CLAUDE.md` MUST describe any effect that is rebuilt, and the budgets and how
  to run the measurement MUST be documented.

### Key Entities

- **Page profile**: a public page type, a device profile, a ramp, a visit state (bare or fully bloomed), images on
  or off, and reduced motion on or off. Each is one measured case.
- **Budget**: a metric, a limit, and whether it is enforced at the target or at the baseline.
- **Baseline**: the measured value of each metric for each page profile before Phase 1's fixes.
- **Report**: every page profile's metrics against budget and baseline, for the local build or the live site.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On the throttled phone profile, in both engines, scrolling recipe search with three pages of results,
  at a fully bloomed visit, in both ramps, keeps 95% of frames within 16.7 ms and no frame over 50 ms.
- **SC-002**: On every public page type, both profiles and both ramps, every measured tap, click, hover and key
  press shows its response within 100 ms.
- **SC-003**: The washes blooming during a visit add no frame over 50 ms on either profile.
- **SC-004**: No load milestone, weight or server response is worse than the baseline on any page profile.
- **SC-005**: The owner, comparing before-and-after captures of every page type in both ramps on both profiles,
  approves them as the same look, and every scripts-disabled first paint matches its scripts-enabled one.
- **SC-006**: A deliberately expensive effect added to every card fails CI with a message naming page, profile,
  ramp, budget and number; on an unchanged codebase the guards pass ten runs out of ten.
- **SC-007**: One command produces the full report for the local build, and one for the live site.

## Assumptions

- The phone profile is a mid-range phone emulated by CPU and network throttling; the desktop profile is an
  unthrottled desktop browser. WebKit is measured because every browser on an iPhone uses it, and it paints filters
  differently from Chromium.
- Interaction delay is measured from the input to the next paint that shows its response.
- The fully bloomed visit is reached by fast-forwarding the visit clock, as 005 already allows, so measurement does
  not wait five minutes.
- The recipe test seeder's recipes are the measured content. The seeder grows to 36 recipes with images, so search
  holds three full pages and the image cases run.
- The paper-piles hover lift, whose render cost the owner accepted in 002, stays; Phase 1 makes it cheaper without
  changing how it reads.
- 005 (ink wash bloom) is merged into `main`, so the bloom is part of what is measured.
- No paid product or recurring cost is added: measurement runs in the existing CI and on the owner's Mac.
- Phases 2 and 3 are specified only after Phase 1 passes its definition of done, from this branch.
