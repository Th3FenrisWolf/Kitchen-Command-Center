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
- **FR-003**: The blank sheet MUST show a torn edge that differs from its top sheet's edge, and a tilt within ±1° that
  differs from its top sheet's tilt.
- **FR-004**: The blank sheet MUST match its top sheet's size, including a top sheet that grows to fill its grid row.
- **FR-005**: Under a crisp sheet, the top sheet MUST stay square and the blank sheet MUST keep a small tilt.
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
- **SC-005**: The recipe search grid with its full first page of results scrolls as smoothly as it does before the
  change, with no frame drops that a visitor can see.
- **SC-006**: The brand review finds no sheet that breaks the rule in either ramp.

## Assumptions

- The look of the blank sheet follows the pad's pages today: blank paper, offset a few pixels down from its top
  sheet, with its own tear and tilt.
- "Every sheet" means each torn sheet that holds content. Printed controls, label pills, icon tiles and the desk itself
  are not sheets.
- The pad's menu cards keep their size, motion and shadow. Only the pages under them change.
- The equal-height card grid bug, `card-grid-uneven-heights`, may land first. The blank sheet then follows the
  stretched card.
- Every sheet in a grid doubles its paper. The plan checks the cost of the extra torn edges and shadows on a full grid.
