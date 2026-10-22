# Feature Specification: Ink Wash Bloom and the Grain Rule

**Feature Branch**: `005-ink-wash-bloom`

**Created**: 2026-10-21

**Status**: Draft

**Input**: User description: "i want to update my ink wash bloom logic based on this artifact:
https://claude.ai/artifact/NuavN8Bf5c2tq2x6EEX2x1 — the site should start with no ink washes and gradually gain them
over time. i want to make sure they are randomized or pseudo random across components. additionally, as part of this,
i want to set a new design rule about the grain we currently have going on. only two things should have the grain
overlay: the desk, and the ink washes. paper, inputs, text, labels, etc should all be free of the overlay."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - The site gains colour the longer a reader stays (Priority: P1)

A reader arrives and the paper is bare. As they read and move between pages, washes of brand colour bloom onto the
sheets one at a time, each at its own moment, until about five minutes into the visit every page they open is fully
coloured. The colour they have earned carries from page to page within the visit. A later visit starts bare again
and blooms in a different arrangement.

**Why this priority**: this is the feature. The bloom turns time spent with the recipes into a visible reward and
replaces the static, author-placed washes, several of which never render today.

**Independent Test**: open the site in a fresh browser, confirm no sheet carries a wash, wait or fast-forward the
visit clock, and confirm washes arrive over five minutes, persist across a navigation, and differ in a second visit.

**Acceptance Scenarios**:

1. **Given** a reader who has not visited in the last 30 minutes, **When** the first page renders, **Then** no sheet
   carries a wash.
2. **Given** a page that has been open for a while, **When** the reader keeps reading, **Then** washes arrive one at
   a time at irregular intervals, each spreading over about a minute and a half, never several at once.
3. **Given** a reader two minutes into a visit, **When** they open another page, **Then** that page arrives with the
   washes two minutes of visit time have earned, already in place on first paint, and keeps blooming from there.
4. **Given** a reader five minutes or more into a visit, **When** any page renders, **Then** every wash on it is fully
   bloomed.
5. **Given** a reader who returns after more than 30 minutes away, **When** the page renders, **Then** the visit
   starts again: the page is bare and the new visit's arrangement differs from the last.
6. **Given** one visit, **When** the reader opens the same page twice, **Then** it wears the same arrangement both
   times.

---

### User Story 2 - Washes are pseudo-random across the site (Priority: P1)

Within a page and across pages, washes vary: each sheet gets its own number of washes, sizes, shapes, positions,
colours and arrival times, so no two sheets look stamped from one mould and neighbouring sheets do not bloom in
step. Where a colour already belongs to a sheet (a recipe's own colour, or the colour the owner picked for a Home
card), the first wash to arrive on that sheet wears it.

**Why this priority**: the owner asked for it explicitly; without it the bloom reads as a pattern.

**Independent Test**: render a page with several sheets of different sizes at full bloom and compare: wash counts
follow sheet size, no two sheets share the same arrangement, colours come only from the eight brand washes, and a
recipe sheet's first wash is the recipe's colour.

**Acceptance Scenarios**:

1. **Given** a page at full bloom, **When** its sheets are compared, **Then** no two sheets share the same set of wash
   positions, sizes and colours.
2. **Given** sheets of different sizes that bloom, **When** fully bloomed, **Then** a small card carries one or two
   washes, a reading sheet about three, and the largest sheets four, never more than four.
3. **Given** any wash, **When** its colour is inspected, **Then** it is one or two of the eight brand wash colours,
   used as they are, never a colour blended between them.
4. **Given** a recipe hero or recipe card, or a Home card whose owner set a wash colour, **When** its first wash
   arrives, **Then** that wash wears the chosen colour; the sheet's later washes pick freely.
5. **Given** two neighbouring sheets, **When** the page blooms, **Then** their washes do not arrive at the same
   moments.

---

### User Story 3 - Grain only on the desk, the washes and the tiles (Priority: P2)

Paper, text, inputs, buttons, labels, tape, wells and blank sheets are clean, with no grain or crayon texture over
them. The desk keeps its texture, painted under the paper. Washes keep the grain that makes them read as pigment, and
the pinned colour tiles keep theirs.

**Why this priority**: a new brand rule that the bloom depends on, because grain over the paper would lie over the
washes beneath and muddy them. It is independently shippable and checkable.

**Independent Test**: inspect a page in both ramps and confirm every grain or texture layer sits on the desk, a wash
or a tile, and that paper, text, controls and labels render without one.

**Acceptance Scenarios**:

1. **Given** any page in either ramp, **When** paper, text, an input, a button, a label, tape, a well or a blank sheet
   is inspected, **Then** no grain or crayon texture lies over it.
2. **Given** the desk between sheets, **When** inspected, **Then** it shows the same grain and crayon tooth it shows
   today.
3. **Given** a wash or a tile, **When** inspected, **Then** it keeps its grain.
4. **Given** the brand documents, **When** read, **Then** they state the rule: grain on the desk, the washes and the
   tiles only.

---

### User Story 4 - Readers who ask for less motion (Priority: P3)

A reader whose system asks for reduced motion still starts bare and gains colour on the same schedule, but each wash
only fades in; nothing grows or moves.

**Why this priority**: an accessibility guarantee on top of the core bloom.

**Independent Test**: emulate reduced motion and confirm washes arrive on schedule by opacity alone, with no change of
size.

**Acceptance Scenarios**:

1. **Given** reduced motion is requested, **When** a wash arrives, **Then** it fades in at its final size and place.
2. **Given** reduced motion is requested, **When** the visit passes five minutes, **Then** the page is fully bloomed,
   the same as for every other reader.

### Edge Cases

- **Scripts disabled or slow to load**: the first paint already shows the washes the visit has earned, and the bloom
  continues without any script.
- **Text over a wash**: washes may lie under running text. Washes overlap at most two deep, and where two overlap
  at their strongest, ink and soft ink still meet WCAG AA and control hairlines 3:1 in both ramps.
- **A sheet that scrolls inside itself** (the phone menu cards): its washes stay with its content as it scrolls.
- **A page left open for hours**: the bloom stops at full; nothing keeps changing.
- **A reader in several tabs**: the tabs share one visit, so pages opened in either tab agree on the visit clock and
  the arrangement.
- **A reader switching ramps mid-visit**: washes keep their arrangement and progress; only the blend changes, as it
  does today.
- **Printing a page**: out of scope; the page prints with whatever the screen shows.
- **A very small sheet**, such as a pill-sized card: it still carries at most its size's share of washes, at least
  one when it blooms at all.

## Clarifications

### Session 2026-10-21 (red team)

- Q: A Home card whose backoffice wash is unset says "Unset leaves the card bare paper". Does it bloom? → A: No, it
  stays bare; a set colour still goes to its first wash (FR-010, FR-015).
- Q: Holding AA over four overlapping washes leaves dark-ramp washes faint. Which trade-off? → A: Washes overlap at
  most two deep; strength is capped for a two-wash stack, and control hairlines hold 3:1 over it (FR-017, SC-006).

### Session 2026-10-21 (analyze)

- Q: Is the tape's woven stripe a texture the grain rule removes? → A: No. The weave is the tape's own material, not
  a grain or crayon overlay; tape keeps it, and the grain check exempts it by name (FR-020, FR-021).

### Session 2026-10-21 (implement)

- Q: The stable hash schedule leaves random quiet stretches (typical ~4x the mean gap, up to ~2 minutes on small
  pages). Even spread, or stable timing when a page's sheets change? → A: Even spread. Page washes take golden-ratio
  slots by hash rank, so the longest quiet stretch stays near 2.6x the mean gap on every page. When a page's set of
  sheets changes mid-visit, its washes re-time on the next load (FR-003, A13).
- Q: Holding AA over a two-deep stack leaves dark-ramp washes at strength 0.116 (a single dark wash is 0.26 today).
  What now? → A: Build with it and judge both ramps on the running site at the D6 check; decide then whether to
  change anything (FR-017, FR-018).
- Q: At the D6 check the light washes read as pale pools and the dark ones as nearly invisible. How far can saturation
  rise before AA breaks? → A: Light strength 0.55 with saturation 2.5, dark strength 0.16 with saturation 3. AA is
  judged on text's 3px average over the worst two-wash stack, not on its single worst pixel. `contrast.test.ts` pins
  the values with a model that applies the saturation and a grain gain calibrated to a real render (FR-017, FR-018).

## Requirements *(mandatory)*

### Functional Requirements

**The bloom**

- **FR-001**: Every page MUST render with no washes at the start of a visit.
- **FR-002**: Washes MUST arrive one at a time over the visit. The last wash on a page starts 3 minutes 30 seconds
  into the visit, each wash spreads over 1 minute 30 seconds, and every page is fully bloomed 5 minutes into the
  visit.
- **FR-003**: Arrival times MUST be spread evenly across that window with irregular gaps: no long empty stretch and
  no several washes landing together.
- **FR-004**: Visit time MUST carry across navigations: a page opened during a visit renders with the washes the
  elapsed visit time has earned, in place on first paint, and continues blooming from there.
- **FR-005**: A visit MUST end after 30 minutes with no page of the site open; the next page starts a new visit with
  the clock at zero.
- **FR-006**: The bloom MUST run without any script on the page; the first paint is correct before any script runs.
- **FR-007**: A wash MUST NOT move once fully bloomed, and a page MUST NOT change after full bloom.

**Pseudo-random arrangement**

- **FR-008**: Each visit MUST have its own seed, kept for every page of that visit and replaced when a new visit
  starts.
- **FR-009**: Each sheet's washes MUST be derived from the visit's seed and the sheet's own identity and place on the
  page, so the same page renders the same arrangement within a visit, and different sheets render different ones.
- **FR-010**: Every top sheet MAY bloom. Blank sheets, wells, tiles, labels, tape and controls MUST NOT. A Home card
  whose owner left its wash unset MUST stay bare paper. A pseudo-random share of other sheets MAY stay bare at full
  bloom.
- **FR-011**: A sheet's wash count MUST scale with its size, from one or two on a small card to four on the largest
  sheets, and MUST NOT exceed four.
- **FR-012**: Each wash's size, shape, rotation, position and arrival time MUST be pseudo-random. Sizes are relative
  to the sheet, so one rule gives card-sized washes on cards and page-sized washes on reading sheets. Positions MUST
  cover the sheet evenly rather than clumping.
- **FR-013**: A wash MAY sit anywhere on its sheet, including under running text, and MAY bleed past the sheet's
  margin. It MUST stay within the sheet's tear.
- **FR-014**: Each wash MUST take one or two of the eight brand wash colours as they are; no colour is interpolated
  between two brand colours.
- **FR-015**: Where a sheet has a chosen colour (a recipe's own colour on its recipe surfaces, or the wash the owner
  set on a Home card in the backoffice), its first wash to arrive MUST wear that colour. The backoffice field keeps
  its meaning, its description ("Unset leaves the card bare paper") and its place in the content schema.
- **FR-016**: Neighbouring sheets MUST NOT bloom in step; their arrival times differ.

**Legibility and ramps**

- **FR-017**: Washes on one sheet MUST overlap at most two deep at any point. Ink and soft ink over the strongest
  point of a two-wash overlap MUST meet WCAG AA, averaged over 3px, and control hairlines 3:1, in both ramps. Wash
  strength and saturation are set where that holds, and the living contrast table proves it.
- **FR-018**: Washes MUST keep their pigment in both ramps; only the blend with the paper differs, as today.

**Reduced motion**

- **FR-019**: Under a reduced-motion preference, washes MUST arrive on the same schedule by fading in at final size,
  with no growth or movement.

**The grain rule**

- **FR-020**: Grain and crayon texture MUST lie only on the desk, the washes and the tiles.
- **FR-021**: Paper, text, inputs and every other control, labels, tape, wells and blank sheets MUST render with no
  grain or crayon texture over them, in both ramps.
- **FR-022**: The desk MUST keep its grain and its crayon tooth, painted under the paper, looking as it does today.
- **FR-023**: Washes and tiles MUST keep their grain.
- **FR-024**: An automated check MUST fail when a grain or crayon layer is applied to anything other than the desk, a
  wash or a tile.

**Brand documents**

- **FR-025**: The identity and kit documents MUST be updated in the same change: the grain rule; the bloom; and the
  retirement of "one wash per sheet", "never under running text" and author-placed washes. Every line those changes
  make untrue MUST be corrected.

### Key Entities

- **Visit**: one reader's continuous stay on the site. Holds a start time and a seed. Ends after 30 minutes with no
  page open. Shared by every tab of that browser.
- **Wash**: a pool of one or two brand colours on a sheet. Has a size, shape, rotation, position, colour pair,
  arrival time and strength, all derived from the visit seed and its sheet.
- **Sheet**: a top sheet of torn paper. Has an identity and place on the page, a size that sets its wash count, and
  optionally a chosen colour.
- **Grain surfaces**: the desk, washes and tiles: the only surfaces allowed a grain layer.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: At the first page of a new visit, 100% of sheets carry no visible wash.
- **SC-002**: At 5 minutes of visit time, 100% of washes on any page are fully bloomed; no wash starts later than
  3 minutes 30 seconds.
- **SC-003**: Across 20 sampled visits of the same page, no two visits produce the same arrangement; within one
  visit, the same page renders the same arrangement 100% of the time.
- **SC-004**: On a page with at least six sheets at full bloom, no two sheets share an arrangement, and no sheet
  carries more than four washes.
- **SC-005**: Every wash colour on every sampled page matches one of the eight brand wash colours exactly.
- **SC-006**: Ink and soft ink over the densest wash overlap (two deep) meet WCAG AA (4.5:1 for body text), and
  control hairlines 3:1, in both ramps.
- **SC-007**: In both ramps, zero grain or crayon layers lie over paper, text, controls, labels, tape, wells or blank
  sheets; the desk, washes and tiles keep theirs.
- **SC-008**: A page opened mid-visit shows its earned washes on first paint, with no visible pop or jump when the
  page becomes interactive.
- **SC-009**: Scrolling a fully bloomed page stays smooth on a mid-range phone, with no dropped-frame stutter a reader
  notices.

## Assumptions

Reviewed: 2026-10-21

- A1: Visit boundary and clock: a visit ends after 30 minutes with no page of the site open; its clock is wall-clock
  time from the visit's first page, so time away within those 30 minutes counts. Alternative: count only time with a
  page open.
- A2: Visit scope: one visit per browser, shared by its tabs; a private window is its own visit. Alternative: one visit
  per tab.
- A3: Who carries the visit: the server hands each page the visit's seed and elapsed time, so first paint is right with
  no script; the state is cosmetic, identifies no one and needs no consent banner. Alternative: a script sets them
  after first paint.
- A4: Tuning: the artifact's arrival spacing, bloom easing, dried-edge rim and two-ink overlap carry over; its
  14-colour palettes are replaced by the eight brand washes. Alternative: retune from scratch.
- A5: Existing washes: author-placed washes and their placement props are removed; an author-set colour becomes the
  sheet's chosen colour (FR-015), which also ends the corner washes that never render today. Alternative: keep
  author placement for the first wash.
- A6: Status wells are not washes: they keep their tinted paper, without grain. Alternative: wells bloom too.
- A7: The header pad is a top sheet and blooms like any other, wearing the same arrangement on every page of a visit.
  Alternative: the pad stays bare.
- A8: Cost: the feature adds no service or recurring cost. Alternative: none.
- A9: Where a wash's numbers come from: pure TypeScript derives each wash's shape, place, colours and strength from
  the visit seed and the sheet in the Vue render, and its arrival in a schedule after the render (A13); the kit CSS
  draws them from custom properties. Alternative: the prototype's CSS
  hash on `sibling-index()`, which only Chromium ships.
- A10: How the visit is kept: a `kcc-visit` cookie (seed and server start time) with a 30-minute sliding expiry,
  readable by script, renewed by every page and by a once-a-minute heartbeat while a page is open; without script,
  by every page only. Alternative: an HttpOnly cookie renewed by page requests alone, which ends a visit during a
  long read.
- A11: How the visit reaches the page: the elapsed time as `--wash-elapsed` on `<html>`, written by Razor; the seed
  and page key in the SSR payload and the SSR cache key. Alternative: both written into the Razor markup.
- A12: Sheet identity: a required, stable `washKey` on every sheet (a fixed name per call site; a list item's own id,
  never its index), qualified by the page path unless it starts with `@` (the pad and its cards keep theirs on every
  page). Alternative: the page path plus Vue's `useId()`, which shifts when a conditional sheet renders before it.
- A13: Arrival order: set once per page after the render, as a `<style>` in `<head>`: page washes, ranked by a hash
  priority, take golden-ratio slots `frac(c + j/φ) × 210s` (owner ruling, session "implement"), so the spread is even
  on every page; late washes continue the same sequence. A change to a page's set of sheets re-times its washes on the
  next load. Alternative: a hash lattice with probing, which keeps timing stable when a sheet changes but leaves random
  quiet stretches.
- A14: Wash count by size: hero or wide sheets 4, margin sheets 3, other sheets 1 or 2, with a `bloom` prop to
  override; every top sheet blooms. Alternative: a container query on sheet width, with a share of cards left bare.
- A15: The dried edge: a lift in the first ink's alpha near the rim, not a darker colour, so every pigment is a brand
  wash exactly. Alternative: the prototype's darkened rim.
- A16: Sheets mounted after first paint take the time since page load as a lag, so they bloom on the visit clock; a
  page restored from the back-forward cache resumes where it was left. Alternative: no correction.
- A17: Motion rules: the bloom is the kit's one non-loop animation over 500ms; the reduced-motion and ramp-switch
  rules exclude washes, which fade on the same schedule under reduced motion. Alternative: none that keeps FR-019 and
  the ramp-switch edge case.
- A18: Desk texture: `.kcc-crayon` moves beside `.kcc-grain` as `#app`'s first children at `z-index: 0`; the sheet
  grain and its three tokens are removed and retired. Alternative: masking the crayon to the desk around each tear.
- A19: Wash strength: one `--wash-strength` cap per ramp, replacing `--wash-op`, set by `contrast.test.ts` over a
  two-wash stack at peak (ink and ink-soft 4.5:1, hair-strong 3:1), each wash peaking at 75–100% of it; the
  `it.fails` hair-strong case becomes a plain assertion. Alternative: a cap per colour.
- A20: Grain enforcement: a static Vitest scan of `Features/**` that allows `--grain` and `--crayon` to be declared
  only in `Torn/Tokens.css` and the filters only in `Layout.cshtml`, resolves `var()` aliases, counts any texture
  image as a source, and allows uses only on the desk, wash and tile selectors; plus E2E checks in both ramps of
  paint order and of pixel variance on bare paper against the desk. Alternative: E2E pixel sampling alone.
- A21: Scroll sheets: the wash field covers the first scrollport and scrolls with the content. Alternative: a field
  sized to the full scroll height.
- A22: Bleed: one SVG filter, `#kcc-bloom` (wax displacement, bleed blur, grain), replaces `#kcc-wax` and the CSS
  `blur()`. Alternative: `blur()` chained after `url(#kcc-wax)`.
- A23: The wash field clips (`overflow: clip`) and is a size container, so washes never add to a page's or a scroll
  sheet's scroll range, and the kit can size them against the real sheet. Alternative: `overflow: hidden`, which
  makes the field a scroll container.
- A24: Two deep at most: washes pair up ({0, 2} and {1, 3}) in opposite quadrants, and the kit caps each wash so the
  circle round it, bleed included, stays within half the distance to its partner, measured in CSS against the real
  field; one or two washes need no cap. Alternative: rejection sampling on the server, which cannot know the sheet's
  height.
- A25: The pad's timing: the visit has a lattice of sixteen beats; the pad and its cards name their beats in markup,
  and page washes keep clear of all sixteen, so the pad blooms on the same clock on every page, signed in or not.
  Alternative: the pad's washes claim page beats like any other.
- A26: A sheet that must stay bare passes `bloom="none"`; the Home card and stacker card pass it when their backoffice
  wash is unset. Alternative: a separate `bare` flag.
- A27: Duplicate keys on one page: the second and later take `~2`, `~3` in render order, with a development warning.
  Alternative: fail the render.
- A28: Home sheet keys: Home sections and cards carry no stable id today, so `RichTextSection`, `StackerSection`,
  `HomeCard` and `StackerCard` gain `Key`, the Block List item's content key, and their sheets take
  `home-richtext:<key>`, `home-stacker:<key>`, `home-card:<key>` and `stacker-card:<key>`. Alternative: the block's
  position in the Home Block List, which moves every later sheet's arrangement when the owner reorders or inserts a
  block.
- A29: Size and chosen colour of the sheets that do not use `KccSheet`'s size rule: `DetailHero` and
  `FeaturedRecipeCard` take `page`; `VariantGrid`'s add card, `RecipeCard` and `RecipeCardRow` take `card`; the
  error, registration-complete, rich-text and stacker Razor sheets take `reading`. Recipe surfaces choose
  `washFor(seed)`, the error page red and registration complete green, as they wear today; the Home rich-text and
  stacker sheets choose none. Alternative: every one passes `card` and lets the hash pick.
- A30: Where the schedule runs: `Server.Entry.ts` exports `scheduleWashes` beside `createApp`, and `Server.js` loads
  both from the same SSR module and calls it after `renderToString`. Alternative: a separate build of `washes.ts`
  for the sidecar, which could drift from the render's copy.
- A31: How E2E tests place a reader in a visit: they write `kcc-visit` with a back-dated start into the browser
  context before the first navigation, as the visit contract allows. Alternative: a Development-only endpoint that
  sets the visit, which adds an endpoint to guard.
- A32: Size precedence: `sheetSize` resolves `bloom`, then `tear="hero"` to `page`, then `margin` to `reading`, then
  `wide` to `page`, else `card`, because `wide` sets a sheet's width, and most reading sheets are both `margin` and
  `wide`. Small `wide` list items pass `bloom="card"` (the review items in `VariantReviews.vue` and the recipe groups
  in `AccountView.Component.vue`), and the two empty-state sheets (`RecipesEmptyState.vue`, `VariantsEmptyState.vue`)
  pass `bloom="reading"`. Alternative: keep `wide` before `margin` and pass `bloom="reading"` on every wide reading
  sheet.
- A33: Review sheet key: `Review` has no id, so a review sheet takes `review:<authorName>|<created>`, which never
  reaches the page (only its hash does), and a repeat takes `~2` (A27). Alternative: add an id to the reviews API
  and `Review`.
- A34: Arrival for washes that no schedule rule covers. Spacing is per wash (owner ruling, session "analyze"). The
  bloom context hands the j-th wash mounted after hydration on a page the start `frac(c + j / φ) × 210s`, with no
  jitter and with `c` from the visit seed and the page. Late washes continue the page's golden-ratio sequence (A13)
  after its scheduled washes, and a candidate within the block radius of one of the pad's 16 visit beats is skipped,
  so late starts keep the page's even spread. Within a late sheet, wash 0 takes the
  earliest of its starts. A server-rendered sheet's fallback, used only when no schedule rule reaches it, keeps one
  start per stratum of the window: `(k + p) / n × 210s` with a jitter of at most `0.3 / n`, `p` from its key's hash,
  clamped to `[0, 210)`. Alternative: a golden-ratio phase per late sheet, which failed a 2s gap on 859 of 1,000
  seeds in the analyze simulation.
