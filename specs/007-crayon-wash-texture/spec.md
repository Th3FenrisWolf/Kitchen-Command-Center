# Feature Specification: Crayon Wash Texture

**Feature Branch**: `007-crayon-wash-texture`

**Created**: 2026-11-04

**Status**: Draft

**Input**: User description: "the first phase of my performance sweep (006) brought tons of improvements, however. my
washes moved from looking like crayon/colored pencil washes to feeling more like water color paint drops. im wondering
if there is anything we can do to move back to the original look while retaining the performace increase. maybe seeing
some different options may help, but i've gotten feedback that the site looks "moldy" as it currently stands, and i
think its cuz the washes feel like water color drops"

## Background

The ink washes arrived in 005 as a live filter that displaced each wash's edge, blurred it, and multiplied it by a
colour noise. That read as crayon or coloured pencil on paper. 006 baked the filter into a grain tile and eight blob
masks so no wash carries a filter, and lowered the light wash strength from .55 to .42. 006's third look round
records what the bake gave up: the grain's colour speckle, the displaced wobble along the ink edge, and the free
shapes. What remains is a soft, round spot with a darker ring near its edge, which reads as a watercolour drop.
Visitors have called the site "moldy".

## Clarifications

### Session 2026-11-04

- Q: Which directions should the look rounds show side by side? → A: Three. A faithful restore of the 005 look
  (colour speckle, edge wobble, varied shapes), the darker rim dropped, and a directional pencil tooth that pushes
  past 005 toward crayon. Light strength stays at .42 unless a round shows otherwise.
- Q: How strict is "retain the performance increase"? → A: Budgets only. Every 006 budget and guard passes; texture
  weight or paint time may grow over the after-006 numbers while it stays inside budget.
- Q: In which ramp does the moldy look show? → A: Both. Light and dark are fixed and judged equally.
- Q: Is the bloom in scope? → A: The bloom's 45 steps, timing and count stay as 006 left them. Placement and size
  were first held too, then reopened in look round 2, below.
- Q: May a live filter return on a wash if the budgets pass? → A: Yes, when every 006 budget passes in both engines.
  The kit's no-filter-on-a-wash rule is rewritten to match the shipped approach.
- Q: Which wash direction? → A: Short chalk strokes in both ramps, edge-weighted placement with larger washes, a
  lobed and ragged outline with a fading edge, and a one-pixel soft blur; see mockups/README.md
- Q: Are a wash's placement and size in scope? → A: Yes. The small, whole circles are part of the mould: washes grow
  to 90% to 145% of the sheet's width and centre in the outer band of their quarter, so most are cut by an edge.
- Q: Several 006 budgets already fail on `main`. Is the performance bar absolute or relative? → A: Relative. No 006
  budget or guard that passes on `main` may fail on this branch; a case whose result flips is re-run three times and
  judged on its median. Budgets that already fail on `main` are carried, not fixed here.

### Session 2026-11-05

- Q: The spike regressed search-3 (phone WebKit long frames 19 against `main`'s 16, limit 17; desktop Chromium slowest
  interaction 96 ms against 80 ms, under the 100 ms hard limit). How is it handled? → A: Shrink the washes: sizes run
  from 90% to 125% of the sheet's width, down from 145%. The spike is not re-run; both cases are judged again at the
  final gate with the real chalk masks.
- Q: Round 8: do the projected strengths (about .39 light, .13 dark) and the largest wash's outline (about 1,075 px,
  uncapped) hold? → A: Both approved. The bake goes ahead with the contract's numbers, strengths come from the re-read
  contrast table, and no px cap on a wash's width applies; see mockups/README.md
- Q: At the final gate, `lcp_ms` on four phone Chromium bloomed search cases reads 20 to 40 ms later than on `main`
  (1788 to 1804 ms against limits of 1775 to 1795 ms, which `main` passes), in every run, on battery and on mains
  power. How is it handled? → A: Accept it and re-baseline those four cases in `specs/006-performance-sweep/baseline.json`
  by the measured gap; see research.md R12
- Q: Round 9: do the built washes read as chalk, not moldy or furry (SC-001)? → A: Approved, including dark at .125;
  see mockups/README.md

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A visitor sees crayon, not mould (Priority: P1)

A visitor opens any public page part way through a visit. The washes on the sheets read as soft chalk laid onto paper: short
strokes, slightly smudged, in large irregular patches that mostly run off an edge of the sheet. Nothing on the sheet
looks like a spreading spot, a watercolour drop or fur.

**Why this priority**: This is the complaint. The site currently reads as moldy, which undercuts the whole identity.

**Independent Test**: Open home, recipe search and a recipe page at a fully bloomed visit, in both ramps, on desktop
and phone, and compare against the pre-006 captures and the after-006 captures.

**Acceptance Scenarios**:

1. **Given** a fully bloomed visit in the light ramp, **When** the visitor views a page with washes, **Then** each
   wash shows short chalk strokes and a lobed, ragged outline that fades out, and no wash shows a smooth fill with a
   darker ring at its edge.
2. **Given** the same visit in the dark ramp, **When** the visitor views the same page, **Then** the washes show the
   same chalk strokes, nothing reads as fur, and every text on them still meets WCAG AA.
3. **Given** any sheet with washes, **When** the visitor views it, **Then** most of its washes are cut by an edge of
   the sheet, and a wash seldom shows as a whole circle.
4. **Given** a visit early in its bloom, **When** a wash is part way through its steps, **Then** it shows the same
   texture at its current size, not a smooth spot that gains texture later.

---

### User Story 2 - The owner chooses between options before anything ships (Priority: P1)

The owner sees the candidate looks side by side on real pages, in both ramps and both engines, and picks one or a
combination before the plan fixes the approach.

**Why this priority**: The owner asked to see options. The look is a brand decision only the owner makes.

**Independent Test**: The look round shows each candidate against the pre-006 and after-006 captures, and the owner's
choice is recorded in the spec folder.

**Acceptance Scenarios**:

1. **Given** the look round, **When** the owner opens it, **Then** it shows at least the three chosen directions
   (faithful 005 restore, dark rim dropped, pencil tooth) on the same sheets and visit state, beside the pre-006 and
   after-006 captures of real pages.
2. **Given** the owner picks a direction or asks for a mix or a change, **When** the next round runs, **Then** it
   shows the revised candidate, and rounds continue until the owner approves one.

---

### User Story 3 - The site stays as fast as 006 made it (Priority: P2)

A visitor on a mid-range phone scrolls recipe search at a fully bloomed visit and the page stays smooth; taps answer
promptly; the washes blooming during a visit cause no stall.

**Why this priority**: The owner wants the look back without losing 006's speed. The texture is the change; speed is
the constraint on it.

**Independent Test**: Run the 006 performance harness over every page profile with the chosen texture and confirm
every budget and guard passes.

**Acceptance Scenarios**:

1. **Given** the chosen texture, **When** the 006 harness runs locally and in CI, **Then** every budget and guard
   passes in both engines, both ramps and both profiles.
2. **Given** scripts are turned off, **When** a page first paints, **Then** its washes match the scripts-enabled first
   paint, as 006 required.

### Edge Cases

- A small wash on a narrow card: the texture keeps its grain and edge at small sizes and never collapses into a flat
  or blurred dot.
- Two washes overlapping at their peak: the stack keeps text readable in both ramps, and the texture of the lower
  wash stays visible through the upper one.
- WebKit and Chromium draw textures and blending differently: the chosen look reads as crayon in both.
- Reduced motion: washes rest at their bloomed state with the same texture.
- A high-density screen: the texture holds its grain size rather than turning smooth when scaled.
- The visit's seed varies shapes per sheet: two neighbouring sheets seldom show the same wash shape at the same place.
- A wash centred past the edge of a small card may show only a sliver: it still shows enough colour to read as a wash.
- A wide reading sheet with four washes: the pair cap may keep one wash smaller than the rest.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Every wash MUST carry the approved chalk marks in both ramps: short strokes along the patch's own
  rotation, broken by the paper tooth, with colour in the gaps, softened by a one-pixel blur, as
  `mockups/README.md` records.
- **FR-002**: A wash MUST NOT show a darker ring near its edge. Its first ink fades out from about 60% of its radius
  to nothing at its edge, and its outline is softened, so the strokes thin out over the last stretch.
- **FR-003**: A wash's outline MUST be lobed and ragged: a strong slow wobble, and stroke ends of uneven length. Washes
  MUST vary in silhouette across a page and across a visit rather than repeating visibly.
- **FR-003a**: Each wash MUST centre in the outer band of its quarter on one axis, from 2% past the sheet's edge to
  16% inside it, and a one-wash card MUST place its wash in a quarter rather than the middle.
- **FR-003b**: Wash sizes MUST run from 90% to 125% of the sheet's width, and a wash MUST still be capped against its
  pair partner so no point lies under more than two washes.
- **FR-004**: Before the plan fixes the approach, a look round MUST show the owner the faithful 005 restore, the
  dark rim dropped (shown as Dry edge), and the pencil tooth, on hand-written wash fields over the kit stylesheet
  beside 006's pre-006 and after-006 captures of real pages, in both ramps, with a mid-bloom toggle and WebKit
  checked by capture.
- **FR-005**: The owner's approved look, and every round leading to it, MUST be recorded in the spec folder.
- **FR-006**: Light and dark washes MUST both carry the approved look, and every text over a wash MUST meet WCAG AA in
  both ramps, including over a two-wash stack at its peak.
- **FR-007**: A 006 performance budget or guard that passes on `main` MUST NOT fail with the approved look: in both
  engines, both ramps and both profiles in the local matrix, and in CI's Chromium guards. A case whose result flips between runs is re-run three times
  and judged on its median.
- **FR-008**: A live filter MAY return on a wash only when FR-007 holds with it in place; otherwise the texture MUST
  be baked or drawn without a filter.
- **FR-009**: A scripts-disabled first paint MUST match the scripts-enabled one, and hydration MUST NOT change how a
  wash looks.
- **FR-010**: The bloom's steps, timing, count and depth MUST stay as 006 left them. A wash's placement and size
  change only as FR-003a and FR-003b set.
- **FR-011**: Grain and texture MUST stay on the desk, the washes and the tiles only; paper, text, controls, labels,
  wells, blanks and tape MUST carry none.
- **FR-012**: The brand and kit documents and `CLAUDE.md` MUST describe how a wash gets its texture once the approved
  look ships, including any rewrite of the no-filter-on-a-wash rule.
- **FR-013**: Any generated texture MUST stay generated from source by the existing generation command and MUST NOT
  be hand-edited.

### Key Entities

- **Wash look**: the texture of the fill, the shape and edge of the outline, the presence or absence of a rim, and
  how strongly it shows in each ramp.
- **Look candidate**: one direction shown in a look round, with its captures in both ramps and both engines.
- **Look round**: a set of candidates shown to the owner together, ending in an approval, a pick, or a revision.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The owner, comparing the built washes on home, recipe search, a recipe page and sign-in in both ramps,
  both engines and both profiles, against the approved look round, approves them as chalk and not moldy or furry.
- **SC-002**: Every 006 performance budget that passes on `main` also passes on this branch in both engines, both
  ramps and both profiles in the local matrix, and CI's Chromium guards pass ten runs out of ten on the same
  commit.
- **SC-003**: The washes blooming during a visit add no frame over 50 ms on either profile, on every bloom case where
  `main` adds none.
- **SC-004**: Every text over a wash, and over a two-wash stack at its peak, meets 4.5:1 in both ramps.
- **SC-005**: Every scripts-disabled first paint matches its scripts-enabled one on every page type.
- **SC-006**: The kit's enforcement tests stay green.
- **SC-007**: On every page type at a fully bloomed visit, at least three quarters of the washes are cut by an edge of
  their sheet.

## Assumptions

- "The original look" is the look at the last commit before 006's first look-changing fix (`97c51247`), whose
  captures 006 already produced; it is the reference for the faithful restore.
- Wash strengths are set by the contrast table, as A10 records; the owner sees the projected strengths before the
  bake (A15).
- The tiles' own wax texture is not part of this feature.
- The people who called the site "moldy" are not part of the review; the owner judges for them.
- The 006 performance harness and its budgets are the measurement; no new budget is added.
- Changes are limited to the washes; tears, sheets, blanks, falls and the desk grain stay as they are.
- A1: How the approved look is built (FR-007, FR-008): baked by `yarn washes` into masks, no filter on a wash, because
  the 006 mid-bloom guard fails on any filtered running animation. Alternative: the live SVG filter from the approved
  round.
- A2: What carries the outline: each blob mask bakes the first ink's faded, displaced ellipse; the first ink becomes a
  flat colour and the second ink lies inside that silhouette (A14). Alternative: keep the CSS fade under a displaced
  box mask, which redraws the round spot.
- A3: Blob bake scale: a 480 px reference box (a median site wash), 6% margin per side, 6 px per texel, 4-bit alpha;
  the outline's lobes and blur scale with each wash, and the owner judges the largest and smallest washes in the
  step-2 look round (A16). Alternative: a 600 px reference, the mockup's card washes.
- A4: Chalk tile: 128 CSS px, one texel per CSS px, stitched, rows and columns levelled, 4-bit alpha; one file for
  DPR 1 and 2. Alternative: a 2x tile, four times the bytes and measured no closer.
- A5: Colour in the gaps: the tile carries `(marks + .5) / 1.5` and `--wash-gain: 1.5` multiplies the wash's opacity.
  Alternative: scale each ink's alpha by 1.5, which source-over does not keep equal.
- A6: Silhouette count (FR-003): eight blob masks and a per-wash mirror, about 10.8 KB in all. Alternative: twelve
  masks and no mirror, about 14.2 KB and thirteen preloads.
- A7: Colour: no colour speckle, so `--wash-tone` and the grain tile retire; inks keep their linear-light saturation.
  Alternative: keep the .835 tone, darker inks than the approved round.
- A8: Wash box: padding of 6% of the wash's own size (`--wash-span` 1.12) replaces the 24px `--wash-reach`, and the
  pair cap divides by `s × span`. Alternative: a fixed reach of about 75 px.
- A9: Placement (FR-003a, FR-003b): the approved round's edge placement ported as written, with new hash labels only,
  so wash ids, every other draw and the schedule stay; `--wash-o1` and `--wash-o2` retire. Alternative: re-derive
  every draw, which reshuffles every arrangement.
- A10: Strengths: the contrast table is re-read from the new textures; light takes its new cap when that is under .42,
  estimated .38 to .40, and dark takes its new cap, estimated about .13. The owner sees the approved round at these
  projected strengths before the bake (A15). Alternative: keep .18 dark and thin the marks, which changes the approved
  look.
- A11: Contrast calibration: a headless-Chromium harness kept in `.mockups/007-crayon-wash-texture/` re-reads the
  rendered rows, as 006 did. Alternative: a tracked E2E reading test.
- A12: The perf gate (FR-007, SC-002, SC-003): `yarn perf --check` and the E2E deterministic guards, on this branch and
  on `main`, on the same Mac with the same build steps. No 006 budget or guard that passes on `main` fails here; a case
  whose result flips between the two runs is re-run three times on both branches (`KCC_PERF_RUNS=3`, narrowed to the
  case) and judged on its median; budgets already failing on `main` are carried, not fixed here. Alternative: every
  budget passes outright, which needs the work 006 left to Phases 2 and 3.
- A13: Order: a spike with the new placement and size, the final padding (`--wash-span` 1.12) and the final pair cap,
  and today's textures stretched to the new padded box, runs the key perf cases and the deterministic guards first; a
  budget or guard that passes on `main` and fails in the spike goes to the owner as an FR-003b and FR-007 conflict.
  Alternative: measure only after the bake.
- A14: The second ink's place: the planner centres it within a quarter of the drawn blob's radii from that blob's baked
  ellipse centre (`BLOB_SHAPES`), at the closest-side size it has today, so the blob mask keeps a median 84% of its
  alpha (10th percentile 75%), where an independent centre, as in the approved round, keeps 56% (15%). The faint
  skirt of second ink outside the first ink is lost. Alternative: a second masked element per wash for the second ink,
  which doubles the painted wash boxes.
- A15: A look round between the spike and the bake: the approved round's live filter at the projected strengths (about
  .39 light and .13 dark) beside the approved .42 and .18, with the second ink placed as A14 and the largest and
  smallest washes (A16), in both ramps and both engines. The owner's answer routes the bake: approve, or name another
  strength or mark weight. Alternative: show the strengths only in the final look round (SC-001), after the bake.
- A16: The outline at large sizes: one 480 px reference, judged by the owner in the A15 round on an unpaired
  reading-sheet wash at `d` 1.25 of an 860 px sheet (about 1,075 px, lobes and blur about 2.2 times the approved
  round's) and a 340 px phone card's wash at `d` .9 (about 300 px). If the owner rejects the large outline, the choice
  between a second reference set (about 7.3 KB more, over the 16,000 B texture cap and the 17,014 B `image_bytes`
  ceiling) and a px cap on a wash's width: the owner chose the px cap, an amendment to FR-003b, should the large
  outline be rejected. Alternative: bake a second reference set now.
- A17: How SC-003 is checked: `bloom_frame_max_ms` is report-only in `budgets.json`, so `--check` never fails on it;
  the gate reads it per mid-bloom case from the `main` and branch results, and a case under 50 ms on `main` and over
  50 ms here fails SC-003, under A12's re-run rule. The harness measures mid-bloom in Chromium only (16 cases), so
  WebKit is unmeasured on both branches and carried as such; `main` cases already over 50 ms are carried (after 006
  there were none, 9.4 to 10.4 ms). Alternative: make `bloom_frame_max_ms` an enforced budget, a change to the 006
  harness this feature does not make.
- A18: `layer_area_mpx` on the owner's Mac: the E2E deterministic guards enforce it at +2% against `baseline.ci.json`,
  taken on `osx-arm64` (search-3 phone 29.3 Mpx, limit 29.9), and CI's Linux runners skip it; the matrix enforces it
  at +2% against the pre-006 `baseline.json` (36.3, limit 37.0). The spike reads both. If the larger washes push it past
  either limit while every timing and byte budget holds, the owner has approved re-taking only the `layers` and
  `layer_area_mpx` baselines, only for the cases the washes move, with the before and after values in the pull
  request; a timing failure still goes to the owner. Alternative: shrink the washes until it holds, which changes
  FR-003b.
- A19: The spike's box change (plan step 1) ships rather than being thrown away: it retires `--wash-reach`,
  `WASH_REACH_PX` and `--wash-blob-span` and has `yarn washes` emit `--wash-span` 1.12 from the spike on, with
  today's textures; step 4 keeps the masks, inks, opacity and keyframes. Alternative: a throwaway spike, redone in
  step 4.
- A20: `main` is measured once, at the start, while this branch's code equals `origin/main`: the full `yarn perf`
  matrix, the deterministic guards and the look captures, kept in `tests/results/perf/`. The spike and the gate
  compare against them, and the gate re-measures `main` only if `origin/main` has since moved under `src/`, `tests/`
  or the root `package.json`. Alternative: switch this checkout to `main` for every comparison.
- A21: The step-2 look round previews the bake, not the live filter, at every size: the outline's turbulence
  frequencies, displacement scales and blur scale with the wash's size over the 480 px reference while the chalk
  marks keep their CSS px size, and the largest wash is shown a second time capped at 1,000 px (the largest wash the
  approved round showed), so a rejected large outline can name its cap in the same answer. Alternative: show the
  absolute-px live filter at every size, which previews the approved round rather than what ships.
- A22: The bake (plan step 3) and the rule (step 4) stay separate phases: between them the kit rule still reads the
  retired tokens, so `washField`, `grain` and `contrast` are red until steps 4 and 6 land. Alternative: one phase for
  both, too large for one worker.
- A23: SC-007 counts a wash as cut when its drawn blob's ellipse, at rest size, aspect, turn and mirror, crosses its
  field's edge. Alternative: the padded box crossing the edge, which counts washes whose visible ink stays inside.

- A24: Owner answers during implement (T018(c), T023): implement stops and reports; the orchestrator records the answer
  in `spec.md`, and any FR-003b amendment, through `/speckit-clarify`, then implement resumes. Inside implement only
  `research.md` R12 and `mockups/README.md` record it. Alternative: implement edits `spec.md`, which both runners forbid.

Reviewed: 2026-11-04
