# Feature Specification: Type Scale

**Feature Branch**: `004-type-scale`

**Created**: 2026-10-14

**Status**: Draft

**Input**: User description: "next, id like to take a look at the font-size across the app, the standard should be at least 16px for body text, and headings should scale up nicely from there, currently idk if theres a lot of consistency, and the nav items read a little small to me, so lets make consistent rules for all the fonts and concepts"

## Today's sizes

A survey of the kit CSS and the component style blocks on 2026-10-14 found about fifteen sizes between 9px and 40px,
set role by role with no shared scale:

| Size | Where |
|---|---|
| 9px | the pad menu chevron |
| 10px | badges, the pad's recipe count |
| 10.5px | kickers, labels, meta lines, `kcc-lbl`, buttons, segmented controls, nav menu items |
| 11px | stat units, step numbers, label and stat icons, the header search hint |
| 12px | the hero stat, large buttons, button and segment icons, checklist quantities, pad line numbers |
| 13px | field icons, pad line icons |
| 15px | body, `kcc-body`, pad lines, icon links, stat icons |
| 16px | fields and textareas, so iOS does not zoom |
| 17px | hand notes, the pad glyph |
| 22px | `kcc-h4`, h3 to h6, small tiles |
| 24px | h2, section names |
| 26px | stat values |
| 38px | large tiles |
| 40px | h1, `kcc-h3` |

The brand's type table in `docs/brand/loose-leaf.md` lists seven roles. Body is 15px, under the owner's 16px floor,
and every Sono caps role is 10.5px.

## Clarifications

### Session 2026-10-14

- Q: How does this scale relate to spec 003, which sets body text to 20px? → A: The body size is open. The owner wants
  to try sizes between 16px and 20px, perhaps 18px, as part of this work. Spec 003's Recipe preset takes the body size
  this spec settles.
- Q: What is the smallest size any text can be? → A: 14px. Sono caps labels go to 14px, and every other text is 16px or
  larger.
- Q: How do headings step up from body? → A: Decide in a look preview, from two or three scales shown on real pages.
- Q: What do the nav items become? → A: Hazelnut at body size, in mixed case, where they are Sono caps today.

### Session 2026-10-19

- Q: At phone width, do the large headings keep their desktop size? → A: No. Below 768px the display heading drops to
  a phone-only size. The other roles keep their size, so the four heading roles stay distinct at every width.
- Q: How do Cook Mode's step text and timer fit the scale? → A: The step text takes a heading step from the scale. The
  timer is a named readout size, counted apart like the icon sizes.
- Q: Do the Sono caps roles share one size? → A: Yes. Every Sono caps role is 14px.
- Q: Does the guard test also catch Tailwind text-size utilities in templates? → A: Yes. It fails on a raw
  `font-size` in CSS and on a Tailwind text-size utility that is not a scale token.
- Q: Which scale direction? → A: Even steps. The first approved round set an 18px body on a 28px rule. Built on the
  site it read as zoomed, and right at 90% zoom, so the owner rescaled it to a 16px body on a 24px rule, with headings
  20, 24, 32 and 40px; see mockups/README.md
- Q: Who moves the vertical spacing onto the paper rule? → A: This feature. Every vertical gap, margin, padding and
  section gap moves to rule units. Spec 003 then adds only the baseline shim and the offset.
- Q: How are the heading roles named? → A: By role, in a clean cutover: `kcc-display`, `kcc-section`, `kcc-heading`
  and `kcc-title` replace `kcc-h3`, `kcc-h4` and the bare `h2` size inside `kcc-secname`. The tests that pin the old
  names change with them.
- Q: What size are the Sono caps on the 16px scale? → A: 14px, on a 24px line.
- Q: Do controls and icons scale down with the type? → A: No. Controls stay 36px and 48px, one and a half and two
  rules, and standalone icons stay 24, 36 and 48px.
- Q: Do the heading roles stay distinct at phone width? → A: Yes, at every width. Below 768px the display heading takes
  a phone-only 36/48, and the section heading keeps 32/48.
- Q: Which text may be smaller than 16px? → A: Only Sono: the caps roles and short Sono numbers such as times,
  quantities and counts, at 14px.
- Q: Does the member's New recipe button stay icon-only from 768px to 1023px? → A: Its label comes back wherever the
  bar still shows the search field's full placeholder. It stays icon-only only where it does not.
- Q: Where does the rescale land? → A: In the same pull request, #101 on `004-type-scale`, after a look preview.
- Q: Which rescale direction? → A: 16 / 24, with headings 20, 24, 32 and 40px; see mockups/README.md

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Body text is easy to read (Priority: P1)

A visitor reads body text at 16px or larger on every page, in every sheet, form and menu.

**Why this priority**: The owner set this floor first. Body is the most text on the site.

**Independent Test**: Open a recipe detail page, recipe search, a form and the account pages. Every paragraph, list
item, field and description renders at the body size the preview approves, and never below 16px.

**Acceptance Scenarios**:

1. **Given** any sheet with body text, **When** it renders, **Then** the text is Hazelnut at the approved body size, at
   least 16px.
2. **Given** a field, a select or a textarea, **When** a visitor focuses it on iOS, **Then** the page does not zoom.
3. **Given** a recipe card's description, a review or a cook note, **When** it renders, **Then** it uses the body size.
4. **Given** a phone width, **When** a page renders, **Then** body text keeps the same size and no page scrolls
   sideways.

---

### User Story 2 - No text is smaller than 14px (Priority: P1)

Every kicker, label, meta line, badge, button, count and number is 14px or larger.

**Why this priority**: The small Sono caps at 10 and 10.5px are the hardest text to read on the site today, and they
carry real information: categories, times, ratings and counts.

**Independent Test**: Open the recipe search grid, a recipe detail page and a form. Measure every rendered text node.
None is under 14px.

**Acceptance Scenarios**:

1. **Given** a kicker, a label, a meta line or a badge, **When** it renders, **Then** it is Sono caps at 14px or larger.
2. **Given** a button or a segmented control, **When** it renders, **Then** its label is 14px or larger, and the
   control's height still fits it on the rule.
3. **Given** a stat unit, a step number or a checklist quantity, **When** it renders, **Then** it is 14px or larger.
4. **Given** a badge row on a recipe card, **When** the labels grow, **Then** the badges wrap cleanly inside the card.

---

### User Story 3 - Headings step up in one scale (Priority: P1)

Each heading level is one step larger than the level below it, on a single scale that starts from the body size.

**Why this priority**: The owner asked for headings that scale up nicely. Today h1 and `kcc-h3` share 40px, and h3 to
h6 all share 22px, so levels cannot be told apart.

**Independent Test**: Open a page that uses every heading role. Each role has its own size from the approved scale, and
each size sits on the rule.

**Acceptance Scenarios**:

1. **Given** the display title, the section heading, the sheet heading and the card title, **When** they render,
   **Then** each takes its own step of the approved scale, in that order from largest.
2. **Given** a heading that wraps, **When** it renders, **Then** its lines sit on the rule.
3. **Given** a heading's level in the HTML, **When** a page sets its size with a role, **Then** the size follows the
   role and the level stays semantic.
4. **Given** the scale preview, **When** the owner picks a scale, **Then** every heading role takes its step from it.
5. **Given** a phone width, **When** a display heading renders, **Then** it takes its phone size, every other role
   keeps its size, and no two heading roles share a size.

---

### User Story 4 - The nav reads like the pad it is (Priority: P2)

The header pad's menu items read at body size, in Hazelnut, mixed case.

**Why this priority**: The owner finds the nav items small. It is one surface.

**Independent Test**: Open any page. The Recipes and My Kitchen menu items are Hazelnut at the body size, mixed case.
Open each menu.

**Acceptance Scenarios**:

1. **Given** any page, **When** the header renders, **Then** each menu item is Hazelnut at the body size, in mixed
   case.
2. **Given** the current section, **When** the header renders, **Then** its marker underline still marks it.
3. **Given** a menu item, **When** a visitor opens it, **Then** its chevron and its recipe count follow the scale and
   sit on the item's line.
4. **Given** the pad at a phone width, **When** it renders, **Then** the larger items fit, or the phone menu holds
   them, with no item cut off.
5. **Given** the lines inside an open menu card, **When** they render, **Then** they use the body size.
6. **Given** a member's bar at a desk width, **When** the search field still shows its full placeholder beside the
   labelled New recipe button, **Then** the button shows its label.

---

### User Story 5 - Every text role follows one set of rules (Priority: P2)

Every piece of text on the site belongs to a named role, and each role has one face, one size, one line height and one
letter spacing.

**Why this priority**: The owner asked for consistent rules for all the fonts and concepts. A named role set stops new
sizes from creeping in.

**Independent Test**: Read the brand type table. Every role on the site is in it. Run the test suite: a guard fails on
any font size outside the scale.

**Acceptance Scenarios**:

1. **Given** the brand type table, **When** an author reads it, **Then** it lists every role: display, headings, card
   title, body, nav item, kicker, label, meta, badge, button, number, stat, hand note, and the icon sizes.
2. **Given** a stylesheet or a component, **When** it sets a font size outside the scale, **Then** a test fails and
   names the file and the size.
3. **Given** a Razor view and a Vue component that show the same role, **When** they render, **Then** the text is the
   same size.
4. **Given** an icon beside text, **When** it renders, **Then** it takes the text's size. A standalone icon takes one
   of the named icon sizes.

### Edge Cases

- A Sono caps label at 14px is wider than at 10.5px: badge rows, toolbars and the segmented control may wrap or scroll.
  The segmented control already scrolls when it is narrower than its segments.
- A recipe card title at a larger size wraps to more lines: the card grows by whole rules.
- The pad's menu items at body size take more width: the bar must still fit the search field and the New Recipe button
  at desktop widths.
- A browser's minimum font size or zoom: sizes in rem scale with it, and the rule scales too.
- Numbers inherit their context's size. A stat value is its own role.
- The hand note stays the one italic role, at most one per sheet.
- Text inside a wash keeps its contrast at the new sizes.
- Tile glyphs and empty-state icons are icons, not text. They take the named icon sizes.
- Cook Mode is read from a distance. Its step text takes a heading step, and its timer takes the readout size.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Body text MUST be Hazelnut at 16px on a 24px line.
- **FR-002**: Text on the public site MUST NOT be smaller than 14px. Every Sono caps role MUST be 14px. Only Sono text,
  the caps roles and short Sono numbers such as times, quantities and counts, may be smaller than 16px. Every other
  role MUST be 16px or larger.
- **FR-003**: Headings MUST step up from the body size on one scale of about 1.25: card title 20/24, sheet heading
  24/24, section heading 32/48 and display 40/48, as `mockups/README.md` tabulates. Sono caps roles MUST be 14/24.
- **FR-004**: Every heading role MUST have its own size at every width. Two heading roles MUST NOT share a size. The
  HTML level stays semantic, and a role class sets the size.
- **FR-005**: The nav menu items MUST be Hazelnut at the body size, in mixed case.
- **FR-006**: Every text role MUST have one face, one size, one line height and one letter spacing, and every size
  MUST sit on the paper rule.
- **FR-007**: The role set MUST cover display, headings, card title, body, nav item, kicker, label, meta, badge, button,
  number, stat, hand note and field text.
- **FR-008**: An icon beside text MUST take the text's size. A standalone icon MUST take one of a small set of named
  icon sizes.
- **FR-009**: Fields, selects and textareas MUST stay at 16px or larger, so iOS does not zoom.
- **FR-010**: The scale MUST live in tokens that the kit CSS, the component style blocks and the Razor views all read.
  A stylesheet or a component MUST NOT set a raw font size. The scale tokens MUST be available as named Tailwind text
  sizes.
- **FR-011**: A test MUST fail when a stylesheet or a component sets a font size outside the scale tokens, through a
  CSS `font-size` or through a Tailwind text-size utility, arbitrary values such as `text-[22px]` included.
- **FR-012**: The brand type table MUST list every role with its face, size, line height and spacing, and the
  engineering contract MUST name the tokens and the guard test.
- **FR-013**: Every text colour MUST pass the contrast table at its new size, in both ramps.
- **FR-014**: Spec 003's Recipe preset MUST take its body and title sizes from this scale.
- **FR-015**: Every structural hook that a test locates by, such as a `data-testid`, a role or a `kcc-*` class, MUST
  survive, except the heading classes. `kcc-display`, `kcc-section`, `kcc-heading` and `kcc-title` replace `kcc-h3`,
  `kcc-h4` and the section size of `kcc-secname h2` everywhere, tests included, and no old name remains.
- **FR-016**: Below 768px, the display heading MUST take a phone-only 36/48. Every other role MUST keep one size at
  every width.
- **FR-017**: Cook Mode's step text MUST take a heading step from the scale. Its timer MUST take a named readout size,
  counted apart from the text sizes like the icon sizes: 64/72.
- **FR-018**: The paper rule MUST be 24px, and every vertical gap, margin, padding and section gap MUST be a whole or
  half number of rules, set in rule units, so text stays in step with the ruling. Buttons, fields and segmented
  controls keep their 36px and 48px heights, and standalone icons their 24, 36 and 48px sizes.
- **FR-019**: The member's New recipe button MUST show its label at every desk width where the search field still shows
  its full placeholder.

### Key Entities

- **Type role**: a named use of text, such as body, kicker or card title, with one face, size, line height and spacing.
- **Type scale**: the ordered set of sizes that every role takes its size from, starting at the body size.
- **Scale token**: the one place a size is defined, read by CSS, components and views.
- **Icon size**: one of a small set of sizes for icons that stand alone.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On every public page type, no rendered text is smaller than 14px, and all body text is the approved body
  size of at least 16px.
- **SC-002**: The site uses at most eight distinct text sizes, each one a scale token. Icon sizes and the readout size
  are counted apart.
- **SC-003**: At every width, every heading role has a size distinct from every other heading role and from body.
- **SC-004**: The guard test fails on a raw font size and passes on the current code once the change lands.
- **SC-005**: No page scrolls sideways at 375px wide, and the header pad shows every menu item at 1024px wide.
- **SC-006**: Every text colour passes the contrast table in both ramps.
- **SC-007**: The brand review finds no text outside a named role.

## Assumptions

- A look preview with `/speckit-preview-run` settled the direction on 2026-10-19 on the recipe search grid, a recipe
  detail page, a form, Cook Mode and the header pad. After the 18/28 build, a further look round confirms the 16/24
  rescale. `mockups/` holds the approved look.
- The rule pitch stays 24px with the 16px body. Spec 003's Recipe preset takes 16px on a 24px pitch from this scale.
- The body size and the paper rule are chosen together, so each size sits on spec 003's rule. The preview may also
  change spec 003's pitch.
- Sono stays the face for kickers, labels, meta, badges, buttons and numbers. Only its size changes.
- APCasual stays the face for display and headings. Hazelnut stays the face for body and now nav items.
- "At most eight sizes" counts text sizes only. Icon sizes and Cook Mode's readout size are counted apart.
- The site rem stays 16px. Scale tokens are set in rem, so a reader's browser size setting scales the whole site.
- This spec and spec 003 land in either order, but each must agree with the other's sizes when both land.
