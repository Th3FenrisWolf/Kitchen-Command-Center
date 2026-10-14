# Feature Specification: Paper Rule Scheme

**Feature Branch**: `003-paper-rule-scheme`

**Created**: 2026-10-14

**Status**: Draft

**Input**: User description: "next we need to make a reusable scheme for paper rule based on this artifact: https://claude.ai/artifact/3vYgZQcR6pSg2CnkgDhw1X. i want to use the recipe preset updated to use a 2px rule offset"

## Source

The Paper Rule Workbench artifact (version `1789562980-d004`) holds the reference: `paper.css` and a workbench page
that measures real baselines against the ruling. Its scheme has three layers:

- **Metrics**: each font family is pinned with `ascent-override`, `descent-override` and `line-gap-override`, so one
  constant per family, `k = (ascent − descent) / 2`, places its baseline at any line height. Hazelnut is
  `k = 0.38208` and APCasual is `k = 0.25`.
- **Rhythm**: one unit, the pitch, is the rule spacing and the line height. A rule lands at
  `pitch / 2 + k · font-size` from the top of a line, plus the rule offset. A block in another face, size or line
  count takes a shim that moves its text onto the rule while the block still fills a whole number of pitches.
- **Sheet**: ground, ruling, margin rule and grain.

The Recipe preset is a pitch of 2rem (32px), body text of 1.25rem (20px), and titles of 2.5rem over two rules. The
workbench draws the ruling in feint blue at 1px, a red margin rule two columns in with the copy one column after it,
and a grain at 6%. The owner asks for the Recipe preset with a rule offset of 2px, where the workbench default is 0.

## Clarifications

### Session 2026-10-14

- Q: Where does the Recipe preset apply? → A: Everywhere. It becomes the one site default, and every sheet moves from
  the 24px rule to the 32px rule with the larger type. Compact and Display stay in the scheme for later use.
- Q: Which look does the scheme use? → A: The workbench look: feint blue rules, the red margin rule and the workbench
  grain. The dark-ramp colours, and the rules for where the margin rule applies, are still to explore. They are open
  below and go to a look preview.
- Q: Which rulings ship? → A: Ruled and blank. Graph and dot wait until a surface needs them.
- Spec 004, the type scale, reopened the body size on 2026-10-14. The owner wants to try 16px to 20px, perhaps 18px,
  in 004's look preview. The 20px body and 40px title below stand until that preview settles them.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Text sits on the rule on every sheet (Priority: P1)

A visitor reads any sheet on the site and every line of text sits on a pencil rule, in any face and at any size the
sheet uses, on any platform.

**Why this priority**: The brand says the drawing is exact and type sits on the rule. Today the rule is drawn from the
padding with a fixed nudge, so a size or face change drifts off it.

**Independent Test**: Open a recipe detail page, a recipe search page and a form. Measure each text baseline against
the nearest rule. Every baseline lies within 0.75px of the rule plus the 2px offset.

**Acceptance Scenarios**:

1. **Given** a sheet with body text, a title over two rules and a Sono meta line, **When** it renders, **Then** every
   baseline lies on a rule, 2px above it, within 0.75px.
2. **Given** a title that wraps to a second or third line, **When** it renders, **Then** the blocks below it stay on
   the rule.
3. **Given** a block with no text, such as an image or a control row, **When** it renders, **Then** it fills a whole
   number of pitches and the text below it stays on the rule.
4. **Given** the same page on macOS and Windows, **When** it renders, **Then** APCasual's baselines sit in the same
   place on both, because its metrics are pinned.
5. **Given** a page before its brand fonts load, **When** the fonts arrive, **Then** the text moves onto the rule
   with no change to the height of any block.

---

### User Story 2 - The Recipe preset is the site's rhythm (Priority: P1)

Every sheet on the public site uses the Recipe preset: a 32px rule, 20px body text and 40px titles over two rules.

**Why this priority**: The owner chose one site default. Every other story renders in it.

**Independent Test**: Open the home page, recipe search, a recipe detail page, cook mode, the account pages and the
create recipe wizard. The ruling is 32px on every sheet, and body text is 20px.

**Acceptance Scenarios**:

1. **Given** any public page, **When** it renders, **Then** every ruled sheet uses a 32px pitch.
2. **Given** body text on any sheet, **When** it renders, **Then** it is 20px Hazelnut on a 32px line.
3. **Given** a page title, **When** it renders, **Then** it is 40px APCasual over two rules.
4. **Given** spacing between blocks and sections, **When** a page renders, **Then** every gap is a whole number of
   pitches, so no sheet drifts off its rule.
5. **Given** a phone width, **When** a page renders at the larger type, **Then** no text overflows its sheet and no
   page scrolls sideways.
6. **Given** the header pad, the toolbar and the controls, **When** they render, **Then** they keep their function at
   the new rhythm.

---

### User Story 3 - The workbench look on paper (Priority: P2)

Each ruled sheet shows feint blue rules, a red margin rule where the rules call for one, and the workbench grain, in
both ramps.

**Why this priority**: The owner chose the workbench look. The dark ramp and the margin rule's reach still need a
preview, so this story waits on that round.

**Independent Test**: Open a ruled sheet in the light ramp and the dark ramp. The rules are feint blue, the margin rule
appears only where the rules below say it does, and the text keeps its contrast.

**Acceptance Scenarios**:

1. **Given** a ruled sheet in the light ramp, **When** it renders, **Then** its rules are feint blue at 1px.
2. **Given** a ruled sheet in the dark ramp, **When** it renders, **Then** its rules and margin rule use the dark
   values the preview approves.
3. **Given** a sheet that the margin rule applies to, **When** it renders, **Then** a red margin rule runs the full
   height of the sheet, and the copy starts one column to its right.
4. **Given** a sheet that the margin rule does not apply to, **When** it renders, **Then** it has no margin rule and
   its copy keeps its normal inset.
5. **Given** any text on a ruled sheet in either ramp, **When** contrast is checked against the paper with its rules,
   grain and washes, **Then** it passes the contrast table.

---

### User Story 4 - A reusable scheme authors can apply (Priority: P2)

An author who builds a new sheet, in Vue or in Razor, picks a preset and a ruling, marks each block as body or
display, and gets text on the rule without measuring anything.

**Why this priority**: The owner asked for a reusable scheme, not a one-off fix of today's sheets.

**Independent Test**: Build a test sheet in the Compact preset and one in the Display preset. Both put every baseline
on the rule with no extra rule written for that sheet.

**Acceptance Scenarios**:

1. **Given** a sheet set to the Recipe, Compact or Display preset, **When** it renders, **Then** its pitch, body size
   and title size follow that preset, and its text sits on the rule.
2. **Given** a sheet set to the blank ruling, **When** it renders, **Then** it shows no rules and keeps the same
   rhythm.
3. **Given** a list or a table whose rows run on, **When** it renders, **Then** the rows continue the ruling with no
   extra space between them.
4. **Given** a Razor view, **When** it uses the scheme, **Then** it gets the same result as a Vue component, because
   the scheme is global kit CSS.
5. **Given** the engineering contract, **When** an author reads it, **Then** it names the presets, the rulings, the
   block types and the rule that every block fills whole pitches.

### Edge Cases

- A block's own font size differs from the sheet's body size: its shim moves its text onto the rule from its own
  size, so the rule holds at any size.
- The first block on a sheet has no space above it: the shim still works, because it is spent from the block's own
  bottom margin.
- A browser rounds font ascent and descent to whole pixels: a baseline may sit up to half a pixel off, per block,
  and the error never builds up down the sheet.
- A sheet grows to fill its grid row, as in the `card-grid-uneven-heights` bug: the ruling runs on to the bottom of
  the stretched sheet.
- A sheet with an asymmetric padding: the ruling stays anchored to the content box, so padding cannot move it.
- The under-sheets of spec 002's piles are blank, so they use the blank ruling.
- Sono labels, kickers and numbers are a third family on the rule. They need a pinned constant like the other two.
- A printed control, such as a button or a field, sits on a sheet but is not text on the rule. It still fills whole
  pitches in the flow.
- A user's browser zoom or minimum font size: the pitch is set in rem, so the ruling scales with the text.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Every brand font family on a sheet, Hazelnut, APCasual and Sono, MUST have pinned ascent, descent and
  line gap, and one baseline constant `k`.
- **FR-002**: The scheme MUST place each rule at `pitch / 2 + k · font-size` from the top of a line, plus the rule
  offset, measured from the sheet's content box.
- **FR-003**: The rule offset MUST be 2px.
- **FR-004**: The scheme MUST offer the three workbench presets: Recipe (2rem pitch, 1.25rem body, 2.5rem title over
  two rules), Compact (1.5rem, 1rem, 1.75rem) and Display (3rem, 1.5rem, 4rem).
- **FR-005**: The Recipe preset MUST be the default for every sheet on the public site. Its body and title sizes MUST
  come from spec 004's type scale. [NEEDS CLARIFICATION: the body size and the pitch it sits on, settled by spec 004's
  look preview.]
- **FR-006**: Every block on a sheet MUST fill a whole number of pitches, and every gap between blocks MUST be a whole
  number of pitches.
- **FR-007**: A block whose face, size or line count differs from the body MUST move its text onto the rule without
  changing the space it fills.
- **FR-008**: The scheme MUST ship two rulings: ruled and blank.
- **FR-009**: The ruled sheet MUST use the workbench look: feint blue rules at 1px, the red margin rule where it
  applies, and the workbench grain.
- **FR-010**: The look MUST have values for the dark ramp. [NEEDS CLARIFICATION: the dark-ramp colours for the rule,
  the margin rule and the grain. The owner wants to explore them in a look preview.]
- **FR-011**: The margin rule MUST appear only on the sheets the brand rules name. [NEEDS CLARIFICATION: where the red
  margin rule applies and where it does not, such as recipe slips, heroes, forms, cook mode and narrow cards. The
  owner wants to explore this in a look preview.]
- **FR-012**: The workbench grain MUST sit with the Loose Leaf wax. [NEEDS CLARIFICATION: whether the workbench grain
  replaces the crayon wax on paper or adds to it. Settle in the same look preview.]
- **FR-013**: The scheme MUST work the same in Vue components and Razor views.
- **FR-014**: The brand docs MUST describe the new rhythm, the look and the margin rule's reach, and replace every
  statement of the 24px rule. The engineering contract MUST describe the presets, the rulings and the block types.
- **FR-015**: Every text colour MUST pass the contrast table against the new paper, rules and grain, in both ramps.
- **FR-016**: Every structural hook that a test locates by, such as a `data-testid`, a role or a `kcc-*` class, MUST
  survive.

### Key Entities

- **Preset**: a named set of pitch, body size and title size. Recipe, Compact and Display.
- **Pitch**: the one unit for rule spacing, line height and the gaps between blocks.
- **Baseline constant (k)**: one number per font family that places its baseline at any line height.
- **Ruling**: what a sheet draws on its ground, ruled or blank.
- **Margin rule**: a red vertical rule a fixed number of columns from the sheet's left edge, with the copy starting
  to its right.
- **Block**: a piece of a sheet that fills a whole number of pitches: body text, display text, or a box with no text.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On each public page type, every text baseline lies within 0.75px of its rule plus the 2px offset.
- **SC-002**: The same page on macOS and Windows places every baseline in the same place, within 0.5px.
- **SC-003**: Every ruled sheet uses a 32px pitch, and every block's height is a whole multiple of it.
- **SC-004**: No page scrolls sideways at 375px wide.
- **SC-005**: Every text colour passes the contrast table in both ramps.
- **SC-006**: A new sheet in any of the three presets puts its text on the rule with no rule written for that sheet.
- **SC-007**: The brand review finds no sheet off its rule and no margin rule outside the places the brand rules name.

## Assumptions

- The workbench's maths and `paper.css` structure are the reference. The site's own token names and the kit's
  `@layer components` home replace the workbench's names where they differ.
- The workbench's `#sheet` page rules are workbench chrome and do not ship.
- The pencil `rule` token is replaced by the feint blue rule colour, subject to the dark-ramp preview.
- Sono's constant is measured from its font files, as Hazelnut's and APCasual's were.
- A look preview with `/speckit-preview-run` settles FR-010, FR-011 and FR-012 before the plan. It also shows the
  larger rhythm on the recipe search grid, a recipe detail page and a form.
- The type scale outside the preset, such as labels, kickers and stat numbers, is set in the preview and sits on the
  rule.
- Spec 002, paper piles, uses the blank ruling for its under-sheets. Either feature may land first.
