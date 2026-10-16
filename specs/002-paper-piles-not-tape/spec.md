# Feature Specification: Paper Piles, Not Tape

**Feature Branch**: `002-paper-piles-not-tape`

**Created**: 2026-10-14

**Status**: Draft

**Input**: User description: "first one is that paper should never be taped to the desk. paper should be piled. each piece of paper should lay on top of another peice of paper, similar to the nav, but only two tall rather than 3. (nav should move to 2 as well) tape should be reserved for things getting applied on top of the paper, like these icon peices"

## Clarifications

### Session 2026-10-14

- Q: The header pad today is the top sheet plus three blank pages under it. What does "two tall" mean? → A: Every
  pile is two sheets: the sheet with the content and one blank sheet under it. The pad loses two of its three pages.
- Q: Which sheets get a sheet under them? → A: Every sheet, on Vue and Razor pages: recipe cards in grids and lists,
  panels, heroes, forms and empty states. The pad's menu cards are already in the pile and stay as they are.
- Q: A recipe card lifts when you reach for it. What does its pile do? → A: Only the top sheet lifts. The sheet under
  it stays on the desk.
- Q: How does the sheet under a crisp, untilted sheet sit? → A: The top sheet stays square. The sheet under it keeps a
  small tilt.

### Session 2026-10-16

- Q: Which pile direction? → A: Two steps: the blank sheet sits centred under its top sheet with no offset, turned
  1.5° and scaled 1.015 on cards, panels, forms and empty states, and turned 0.5° and scaled 1.005 on the pad page and
  the hero; see mockups/README.md
- Q: Which sheets take the wide turn? → A: The pad's page, the hero, and every sheet wider than 700px (such as recipe
  list rows, the recipe search header and the "At a glance" sheets) turn 0.5° and scale 1.005; every other sheet turns
  1.5° and scales 1.015.
- Q: The hover lift costs render time when cards scroll under a still pointer, and no variant that keeps it passes
  the original SC-005. Which trade? → A: Keep the hover lift and accept the cost; SC-005 records the measured cost.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Every sheet lies on another sheet (Priority: P1)

A visitor sees each sheet of paper on the site resting on a second, blank sheet, as paper on a real desk lies in a
pile. No sheet lies alone on the desk, and no sheet is held down with tape.

**Why this priority**: This is the brand rule itself. Without it, the other stories have nothing to apply to.

**Independent Test**: Open the recipe search page, a recipe detail page and a page built from home blocks. Every sheet
has one blank sheet showing under it, and no sheet carries a strip of tape.

**Acceptance Scenarios**:

1. **Given** the recipe search grid, **When** it renders, **Then** each recipe card shows one blank sheet under it,
   with a different torn edge and a different tilt from the card.
2. **Given** a recipe detail page, **When** it renders, **Then** the hero and each sheet below it show one blank sheet
   under them, and the hero has no strip of tape at its top centre.
3. **Given** a sheet that used to take tape, such as an empty state, a success message or a small hero, **When** it
   renders, **Then** it has no tape and shows one blank sheet under it.
4. **Given** a page that the server renders without Vue, such as the registration complete page, **When** it renders,
   **Then** its sheet follows the same rule as a Vue sheet.
5. **Given** a crisp sheet, such as a form or cook mode, **When** it renders, **Then** the top sheet stays square and
   the sheet under it keeps a small tilt.
6. **Given** the dark ramp, **When** any of the pages above render, **Then** the sheet under each sheet reads as paper
   on the dark desk, as it does in the light ramp.

---

### User Story 2 - Tape holds only what is applied on top of paper (Priority: P1)

Tape now marks one thing only: an item stuck onto a sheet, such as the torn icon tile on a recipe card or a hero.

**Why this priority**: The rule has two halves. A sheet with no tape and a tile with no tape would both break it.

**Independent Test**: Open a recipe card, the featured recipe card and a recipe detail hero. Each icon tile keeps its
small strip of tape, and no sheet has a strip of its own.

**Acceptance Scenarios**:

1. **Given** a recipe card, the featured recipe card or a detail hero, **When** it renders, **Then** its icon tile
   keeps its strip of tape.
2. **Given** the featured recipe card or a detail hero, which today carry a second strip at the sheet's top centre,
   **When** it renders, **Then** only the tile's strip remains.
3. **Given** an author who builds a new sheet, **When** they look for a way to tape the sheet, **Then** the kit offers
   none. Tape exists only for items applied on top of a sheet.

---

### User Story 3 - The header pad is two sheets tall (Priority: P2)

The header pad keeps its look of a notepad, but as a pile of two: the top sheet and one blank page under it.

**Why this priority**: The owner named the pad as the model for the rule and asked for it to match. It is one surface.

**Independent Test**: Open any page and look at the header pad. One blank page shows under the top sheet. Open the
Recipes menu and the My Kitchen menu.

**Acceptance Scenarios**:

1. **Given** any page, **When** the header renders, **Then** one blank page shows under the pad's top sheet, where
   three show today.
2. **Given** the header pad, **When** a visitor opens a menu, **Then** the menu card slides out from between the top
   sheet and the page under it, and fans out from its far corner as it does today.
3. **Given** the pad tucked away on scroll, **When** the visitor scrolls up, **Then** the pad returns with its one page
   under it, and no page shows alone.

---

### User Story 4 - The top sheet lifts off its pile (Priority: P3)

When a visitor reaches for a sheet that opens, only the top sheet lifts. The blank sheet under it stays on the desk,
so the pile shows its depth.

**Why this priority**: It refines motion that already exists. The rule holds without it.

**Independent Test**: Hover over a recipe card, then move focus into one with the keyboard. The card lifts and the
sheet under it does not move.

**Acceptance Scenarios**:

1. **Given** a recipe card in a grid, **When** the visitor hovers over it or focuses it, **Then** the card lifts and
   the sheet under it stays where it was.
2. **Given** a stacked sheet in a stacker block, **When** it steps back as the next sheet covers it, **Then** the sheet
   under it steps back with it, so the pile stays together.
3. **Given** the visitor asks for reduced motion, **When** they hover over a card, **Then** nothing moves.

### Edge Cases

- A recipe card grows to the height of its row: the sheet under it grows with it, so the pile never shows a short
  under-sheet.
- Neighbouring sheets in a grid already avoid sharing a torn edge. The sheet under a card also avoids the card's own
  edge.
- A sheet that scrolls horizontally, or that is clipped by its container, still shows its under-sheet.
- A full-screen sheet, such as the photo viewer or cook mode, still shows the under-sheet along any edge that the
  screen does not cut off.
- The pad's menu cards are already in the pile. They get no under-sheet of their own.
- An icon tile is applied on top of a sheet. It gets tape and no under-sheet.
- A label pill is printed on the sheet. It gets neither tape nor an under-sheet.
- A ramp switch freezes motion for one frame. The pile switches ramp with its sheet.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Every sheet on the public site MUST rest on exactly one blank sheet, on Vue pages, Razor pages and home
  blocks alike. The pad's menu cards are the one exception.
- **FR-002**: The blank sheet MUST look like paper: torn on four sides, with the fibre and the fall, with no pencil
  ruling and no content.
- **FR-003**: The blank sheet MUST show a torn edge that differs from its top sheet's edge. It MUST sit centred under
  its top sheet with no offset, turned 1.5° off its top sheet's tilt, or 0.5° on a wide sheet (the pad's page, the
  hero and any sheet wider than 700px), and neighbouring blank sheets MUST turn in opposite directions.
- **FR-004**: The blank sheet MUST match its top sheet's size scaled by 1.015, or by 1.005 on a wide sheet as FR-003
  names it, including a top sheet that grows to fill its grid row.
- **FR-005**: Under a crisp sheet, the top sheet MUST stay square and the blank sheet MUST keep its turn: 1.5°, or 0.5°
  on a wide sheet.
- **FR-006**: A sheet MUST NOT carry tape. The kit MUST offer no way to tape a sheet.
- **FR-007**: An item applied on top of a sheet, such as an icon tile, MUST keep its strip of tape.
- **FR-008**: The header pad MUST be two sheets tall: its top sheet and one blank page under it.
- **FR-009**: The pad's menu cards MUST slide out from between the top sheet and the page under it, with their current
  fan and slide motion.
- **FR-010**: When a sheet lifts on hover or focus, only the top sheet MUST move. The blank sheet MUST stay still.
- **FR-011**: When a stacked sheet steps back, its blank sheet MUST step back with it.
- **FR-012**: Reduced motion MUST still every pile motion, as it stills all motion today.
- **FR-013**: The pile MUST read correctly in the light ramp and the dark ramp.
- **FR-014**: The brand docs MUST state the new rule: paper is piled two sheets high, sheets are never taped, and tape
  holds only items applied on top of a sheet. The engineering contract MUST describe the pile and drop sheet tape.
- **FR-015**: Every structural hook that a test locates by, such as a `data-testid`, a role or a `kcc-*` class, MUST
  survive, apart from the sheet tape that this feature removes.

### Key Entities

- **Sheet**: a torn piece of paper that holds content. It rests on one blank sheet.
- **Blank sheet**: the paper under a sheet. It has no content and no ruling, and it moves with the pile but not with
  a hover lift.
- **Applied item**: something stuck onto a sheet, such as an icon tile. It takes a strip of tape.
- **Pile**: a sheet and its blank sheet together. The header pad is a pile of two, with its menu cards between them.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On every public page type, every sheet has exactly one blank sheet under it, and none of them has tape.
- **SC-002**: Every icon tile keeps its strip of tape, and tape appears nowhere else.
- **SC-003**: The header pad shows exactly one page under its top sheet, and both menus open between the two sheets.
- **SC-004**: On a hover or a focus, a recipe card lifts and its blank sheet does not move.
- **SC-005**: The render cost of the recipe search grid, with its full first page of results, is measured against
  `main` and recorded in the pull request. The owner accepted the measured cost of the hover lift on 2026-10-16: at a
  4x CPU throttle, scrolling with the pointer over the grid runs at a median p95 frame of about 34 to 41ms, against
  9.3ms on `main`.
- **SC-006**: The brand review finds no sheet that breaks the rule in either ramp.

## Assumptions

Reviewed: 2026-10-16

- A1: The blank sheet's look: it follows the approved preview in mockups/README.md, blank paper centred under its top
  sheet with no offset, with its own tear and turn. Alternative: the pad's pages as they are today, shifted down and
  turned a little.
- A2: What "every sheet" covers: each torn sheet that holds content. Printed controls, label pills, icon tiles and the
  desk itself are not sheets. Alternative: give icon tiles a blank sheet of their own as well.
- A3: The pad's menu cards: they keep their size, motion and shadow, and only the pages under them change.
  Alternative: restyle the menu cards to match the new pile.
- A4: The equal-height card grid bug, `card-grid-uneven-heights`: it may land first, and the blank sheet then follows
  the stretched card. Alternative: this feature stretches the cards itself.
- A5: The cost of doubling the paper in a grid: the plan checks the extra torn edges and shadows on a full grid
  before the change ships. Alternative: drop the shadow from the blank sheet up front to halve the cost.
- A6: Where the pile lives: the blank sheet is the last child of its top sheet's slip in the kit's global CSS, behind
  the content, so Vue and Razor build it the same way and it turns, stretches, sticks and steps back with its sheet.
  Alternative: a wrapper element that holds the blank sheet and the sheet side by side, as the preview drew it.
- A7: The blank sheet's markup: a `kcc-blank` element around one clipped span, with no `kcc-torn` or `kcc-sheet`
  class, so nothing that finds a sheet finds the blank. Alternative: reuse `kcc-torn` and `kcc-sheet` with the ruling
  switched off.
- A8: How the blank sheet's tear and base turn direction are chosen: the tear generator writes them into each tear
  preset, the tear three along the cycle, and a direction opposite to the top sheet's tilt for the six standard tears,
  with the hero tear set to turn the way the preview drew it and its second hero tear the other way. Alternative: an
  author passes both on every sheet.
- A9: The hero's blank sheet: a second hero tear, `tear-hero-2`, generated by `yarn tears`, also under the pad.
  Alternative: reuse `kcc-tear-3`, as the preview did.
- A10: How a sheet is marked wide: a `kcc-slip--wide` modifier, which `KccSheet` adds to every hero-torn sheet and to
  a sheet given its `wide` prop, and which a hand-written slip and the pad's page write in their class list.
  Alternative: the generator writes the wide turn into the hero tear classes.
- A11: How a lift leaves the blank sheet still: a `kcc-slip--lift` class lifts every child of the slip but the blank
  sheet by 4px over 300ms. Alternative: lift the slip and move the blank sheet back down by the same amount.
- A12: The lift under reduced motion: the lift does not happen at all. Alternative: keep today's instant 4px jump.
- A13: How the kit stops sheet tape: `KccSheet` loses its `tape` prop, the kit styles tape only inside a tile's
  wrapper, and a source-scan test fails on tape anywhere else. Alternative: keep the class and rely on brand review.
- A14: The pad's one page: it takes `tear-hero-2` and the wide turn, sits centred, and stacks under the menu cards,
  which stack under the top sheet. Alternative: keep `kcc-tear-2` under the pad, as the preview did.
- A15: How the render cost is checked: a scripted scroll and hover of the recipe search grid on `main` and on the
  branch, on one machine, five runs each, passing when the branch's median p95 frame time is within 10% of `main`'s
  and its median count of frames over 50ms is at most one more, recorded in the pull request. Alternative: a
  performance test in CI.
- A16: Test coverage: Vitest for the kit, the generator and every Vue sheet, integration tests for the Razor pages,
  the existing browser tests for cook mode and the photo viewer, and one browser walker over every page type for
  alternation, the blank's size, sideways overflow, the lift and the pad. Alternative: a browser test for every
  surface.
- A17: The recipe list row, a slip and its link on one element today: it splits into a slip and a link, so it can
  hold a blank sheet. Alternative: leave single-element slips without a blank sheet.
- A18: What "neighbouring" means for turn direction: the sheet before and the sheet after in reading order, on every
  page. Sheets stacked above each other in a multi-column grid are not reading-order neighbours and can turn the same
  way. Alternative: also alternate vertical neighbours by grid position.
- A19: Where a sheet's width is read for the wide rule: at a 1280px viewport, the preview's measuring width, and a
  surface marked wide keeps the modifier at every viewport. Alternative: a container query that switches at 700px.
- A20: The recipe wizard's photo tiles: they are tiles, not sheets, so they take no blank sheet and no tape.
  Alternative: treat each photo as a sheet with a blank sheet under it.
- A21: What reading order covers: the visible sheets inside `main`, in document order, at the current screen width.
  The header pad floats over every page and is no sheet's neighbour, and an overlay such as cook mode stands alone.
  Alternative: count the pad's page as the first sheet of every page.
- A22: How a page keeps its blank sheets alternating: a flip on a single sheet, computed from the run before it where
  that run varies, and a flip on a container for sheets whose neighbour a breakpoint hides. Alternative: re-pick each
  page's tears so their tilts alternate.
- A23: Keeping the blank sheet as tall as its sheet: every slip is as tall as its content (`height: fit-content`)
  unless it fills its parent, so a grid that stretches its items cannot stretch a blank sheet past its sheet.
  Alternative: `items-start` on the two grids that stretch sheets today.
- A24: A blank sheet that overhangs the screen's edge: the app's root clips sideways overflow, so a tall turned sheet
  never makes the page scroll sideways. Alternative: turn tall sheets less.
- A25: The recipe list row's tile: it is an applied item and gains its strip of tape through a tile wrapper that
  stays in the row's flow, inside the sheet, the one wrapper the kit's docs allow there. Alternative: count the row's
  tile as printed in the sheet and narrow SC-002 to pinned tiles.
- A26: The home page's tear cycle: when it avoids a stacker's tear it skips two tears instead of one, so the tilts,
  and the blank sheets, keep alternating. Alternative: flip the sheet after each skip.
- A27: The variant page's first paint: the server renders no reviews, so the page works out the reviews section's last
  blank sheet from sign-in and the review count, with the same rule the reviews section uses, for the state the server
  renders; the reviews load on mount, and the other variants re-flip after hydration whenever the loaded reviews, or
  more loaded later, change the section's last sheet. Alternative: render the other variants without a flip until the
  reviews load.
- A28: Testing "load more" reviews in the browser: the test stubs the reviews endpoint to return two pages. Alternative:
  seed more than ten reviews on one variant in the development seeder.
- A29: Where the render-cost check's `main` runs come from: a separate worktree of `origin/main`, built and run on the
  same machine and port one after the other with the branch, so the feature checkout never switches branch.
  Alternative: check out `main` in this checkout for the baseline runs and switch back.
- A30: Which test drives a state first (the owner ruled on 2026-10-16 that browser-only states are tested E2E first,
  with no new packages): a state only a browser reaches, such as loaded reviews, the phone's open filters, a list view,
  a toolbar search, a wizard step past the first or a re-flip after reviews load, is driven first by an E2E test in the
  phase that implements it; a state the server renders, and a pure helper, by Vitest; the walker's remaining
  server-rendered pages are a regression check. Alternative: write the whole walker after every page's flips land.
- A31: Pull requests: the feature ships as one pull request, because alternation across a page needs every surface
  piled at once and US2 to US4 are small next to US1. Alternative: split US1 and US2 to US4 into two pull requests.
