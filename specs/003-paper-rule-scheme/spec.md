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

The workbench's presets each pair a pitch with a body and a title size. The site does not use them: spec 004, the
type scale, sets every size and the pitch, and this feature takes its rhythm from there. The workbench draws the
ruling in feint blue at 1px, a red margin rule two columns in with the copy one column after it, and a grain at 6%.
The owner asks for a rule offset of 2px, where the workbench default is 0.

Spec 004 already ships the rhythm this scheme sits on: the 24px rule `--bl` (1.5rem), the body drop `--rule-drop`
(`calc(var(--bl) / 2 + 0.38208 * var(--text-body))`), the `--text-*` role tokens, and every vertical gap in rule
units. Its ruling puts the top of the 1px rule on the body baseline. Only body text is on the rule today. Headings,
Sono caps, numbers and labels centred on a control sit off it.

## Clarifications

### Session 2026-10-14

- Q: Where does the Recipe preset apply? → A: Everywhere, as the one site default. Superseded on 2026-10-19: spec
  004's scale is the one rhythm, and the presets are gone.
- Q: Which look does the scheme use? → A: The workbench look: feint blue rules, the red margin rule and the workbench
  grain. The dark-ramp colours, and the rules for where the margin rule applies, are still to explore. They are open
  below and go to a look preview.
- Q: Which rulings ship? → A: Ruled and blank. Graph and dot wait until a surface needs them.
- Spec 004, the type scale, reopened the body size on 2026-10-14. It settled on 2026-10-19; see below.

### Session 2026-10-19

- Spec 004 settled the scale on 2026-10-19: a 16px Hazelnut body on a 24px rule, caps 14/24, title 20/24, heading
  24/24, section 32/48, display 40/48 (36/48 below 768px) and readout 64/72. Its FR-014 makes this spec take the
  pitch and the sizes from that scale, and it moved every vertical gap to rule units, so this spec adds the shim and
  the offset.
- Q: What happens to the Recipe, Compact and Display presets? → A: They go. The scheme has one rhythm, spec 004's
  scale, and defines no size or pitch of its own.
- Q: Which rule offset ships? → A: 2px. Every baseline sits 2px above the top of its rule.
- Q: Does the workbench look stay in this spec? → A: Yes. The look preview runs before the plan, and the shim ships
  with the look.
- Q: When a label centred on a 36px or 48px control cannot both look centred and sit on the rule, which wins? → A: The
  rule. The label moves onto the nearest rule and sits a few px off the control's centre.
- Q: Which workbench look direction? → A: Reading sheets, written on the line, margin red; see mockups/README.md.
- Q: How does a control meet the ruling? → A: Pills and circles are filled with paper, so no rule runs through them,
  and each centres on the caps of the line it shares, so its text sits on the rule beside its label. A text field
  drops its pill and is written on the line: its text sits on the rule, its underline is that rule, and a
  hand-drawn X in the margin red starts it. Icon circles in a control row are 32px.
- Q: How does a textarea look? → A: Ruled lines. It loses its box, its text writes on the sheet's own rules, and the X
  starts its first line.
- Q: Which other fields become lines? → A: Every field: selects, the header pad search and the variant search too.
- Q: Which sheets carry the margin rule? → A: Every sheet in the main reading column of the recipe and variant pages,
  every wizard and account form sheet, and cook mode. It drops below 480px. Heroes, toolbars, cards, the header, home
  blocks and empty states never carry it.
- Q: Which icon circles drop to 32px? → A: Every icon button, all twelve, cook mode and the timer included.

### Session 2026-10-30

- Q: FR-002's formula, `var(--bl) / 2 + k · font-size`, holds only for a one-rule line. Where does a block on a 48px
  or 72px line put its baseline? → A: Its own baseline lies at `line-height / 2 + k · font-size` below the top of its
  first line, and the scheme moves the block by at most half a rule so that baseline lands on a rule. FR-002 now
  says this, which is what the shim built in this spec does.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Text sits on the rule on every sheet (Priority: P1)

A visitor reads any sheet on the site and every line of text sits on a pencil rule, in any face and at any size the
sheet uses, on any platform.

**Why this priority**: The brand says the drawing is exact and type sits on the rule. Today only body text is placed
from the rule. A heading, a Sono label or a label centred on a control sits off it.

**Independent Test**: Open a recipe detail page, a recipe search page, a variant page and a form. Measure each text
baseline against the nearest rule. Every baseline lies 2px above the top of the rule, within 0.75px.

**Acceptance Scenarios**:

1. **Given** a sheet with body text, a display heading over two rules and a Sono meta line, **When** it renders,
   **Then** every baseline lies 2px above a rule, within 0.75px.
2. **Given** a heading that wraps to a second or third line, **When** it renders, **Then** the blocks below it stay on
   the rule.
3. **Given** a block with no text, such as an image or a control row, **When** it renders, **Then** the text below it
   stays on the rule.
4. **Given** the same page on macOS and Windows, **When** it renders, **Then** APCasual's baselines sit in the same
   place on both, because its metrics are pinned.
5. **Given** a page before its brand fonts load, **When** the fonts arrive, **Then** the text moves onto the rule
   with no change to the height of any block.
6. **Given** a label centred beside or on a control, **When** it renders, **Then** its baseline lies on the nearest
   rule, and the control's own text sits on the same rule.

---

### User Story 2 - Every role on the type scale sits on the rule (Priority: P1)

Every text role that spec 004 names sits on the 24px rule, at its own size and line height.

**Why this priority**: Spec 004 set the sizes and the gaps but placed only body text on the rule. Every other story
renders in this rhythm.

**Independent Test**: Open the home page, recipe search, a recipe detail page, cook mode, the account pages and the
create recipe wizard. Measure one line of each role against the ruling.

**Acceptance Scenarios**:

1. **Given** any public page, **When** it renders, **Then** every ruled sheet uses spec 004's 24px rule, `--bl`.
2. **Given** text in any role of spec 004, display 40/48 (36/48 below 768px), section 32/48, heading 24/24, title
   20/24, body 16/24 or caps 14/24, **When** it renders, **Then** its baseline lies 2px above a rule.
3. **Given** the Cook Mode readout at 64/72, **When** it renders on a ruled sheet, **Then** its baseline lies 2px above
   a rule.
4. **Given** spacing between blocks and sections, **When** a page renders, **Then** the text after every gap is back
   on the rule, so no sheet drifts off its rule.
5. **Given** a phone width, **When** a page renders, **Then** no text overflows its sheet and no page scrolls
   sideways.
6. **Given** the header pad, the toolbar and the controls, **When** they render, **Then** they keep their function and
   their 36px and 48px heights.

---

### User Story 3 - The workbench look on paper (Priority: P2)

Each ruled sheet shows feint blue rules, a red margin rule where the rules call for one, and the workbench grain, in
both ramps.

**Why this priority**: The owner chose the workbench look, and the look preview of 2026-10-19 set its dark ramp and
the margin rule's reach.

**Independent Test**: Open a ruled sheet in the light ramp and the dark ramp. The rules are feint blue, the margin rule
appears only where the rules below say it does, and the text keeps its contrast.

**Acceptance Scenarios**:

1. **Given** a ruled sheet in the light ramp, **When** it renders, **Then** its rules are feint blue at 1px.
2. **Given** a ruled sheet in the dark ramp, **When** it renders, **Then** its rules and margin rule use the dark
   values FR-010 sets.
3. **Given** a sheet that the margin rule applies to, **When** it renders, **Then** a red margin rule runs the full
   height of the sheet, and the copy starts one column to its right.
4. **Given** a sheet that the margin rule does not apply to, **When** it renders, **Then** it has no margin rule and
   its copy keeps its normal inset.
5. **Given** any text on a ruled sheet in either ramp, **When** contrast is checked against the paper with its rules,
   grain and washes, **Then** it passes the contrast table.

---

### User Story 4 - A reusable scheme authors can apply (Priority: P2)

An author who builds a new sheet, in Vue or in Razor, picks a ruling, sets each block in a role of spec 004, and gets
text on the rule without measuring anything.

**Why this priority**: The owner asked for a reusable scheme, not a one-off fix of today's sheets.

**Independent Test**: Build a test sheet with one block in every role of spec 004 and a row of 36px and 48px controls
with labels. Every baseline sits on the rule with no extra rule written for that sheet.

**Acceptance Scenarios**:

1. **Given** a sheet whose blocks use any role of spec 004, **When** it renders, **Then** its text sits on the rule.
2. **Given** a sheet set to the blank ruling, **When** it renders, **Then** it shows no rules and keeps the same
   rhythm.
3. **Given** a list or a table whose rows run on, **When** it renders, **Then** the rows continue the ruling with no
   extra space between them.
4. **Given** a Razor view, **When** it uses the scheme, **Then** it gets the same result as a Vue component, because
   the scheme is global kit CSS.
5. **Given** the engineering contract, **When** an author reads it, **Then** it names the rulings, the block types,
   the shim, the offset and the rule that text after any block is back on the rule.

### Edge Cases

- A block's own font size differs from the body size: its shim moves its text onto the rule from its own size and
  face, so the rule holds at every size on spec 004's scale.
- The first block on a sheet has no space above it: the shim still works, because it is spent from the block's own
  bottom margin.
- A browser rounds font ascent and descent to whole pixels: a baseline may sit up to half a pixel off, per block,
  and the error never builds up down the sheet.
- A sheet grows to fill its grid row, as in the `card-grid-uneven-heights` bug: the ruling runs on to the bottom of
  the stretched sheet.
- A sheet with an asymmetric padding: the ruling stays anchored to the content box, so padding cannot move it.
- The under-sheets of spec 002's piles are blank, so they use the blank ruling.
- Sono labels, kickers and numbers are a third family on the rule. They need a pinned constant like the other two.
- A printed control, such as a button or a field, sits on a sheet but is not text on the rule. It keeps its 36px or
  48px height, one and a half or two rules, and a half-rule gap after a 36px control brings the text below it back
  on the rule.
- A label centred on a control sits off the rule today. It moves onto the nearest rule, and the control moves a few
  px within its row to centre on the label's caps, so the row still fills the same rules.
- A printed control's fill hides the ruling under it, so no rule runs through a pill or a circle.
- A user's browser zoom or minimum font size: `--bl` and the `--text-*` tokens are set in rem, so the ruling scales
  with the text.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Every brand font family on a sheet, Hazelnut, APCasual and Sono, MUST have pinned ascent, descent and
  line gap, and one baseline constant `k`.
- **FR-002**: The scheme MUST place each block's first baseline on a rule: `--rule-drop` plus a whole number of rules
  below the top of the sheet's content box. A block's own baseline lies at `line-height / 2 + k · font-size` below the
  top of its first line, where `font-size` and `line-height` come from the block's `--text-*` token, and the scheme
  moves the block by at most half a rule to bring that baseline onto the rule. Every line height in spec 004's
  scale is a whole number of rules, so the block's later lines land on rules too. Spec 004's `--rule-drop` is
  `var(--bl) / 2 + k · font-size` for body text, which needs no move.
- **FR-003**: The rule offset MUST be 2px: the top of each 1px rule lies 2px below the baseline on it.
- **FR-004**: The scheme MUST NOT define a size, a line height or a pitch of its own. It MUST read `--bl`,
  `--rule-drop` and the `--text-*` tokens from spec 004, and spec 004's guard, `typeScale.test.ts`, MUST stay green.
- **FR-005**: Every sheet on the public site MUST use spec 004's rhythm: the 24px rule, `--bl`, with 16px Hazelnut
  body text on a 24px line.
- **FR-006**: The text after any block or gap MUST start on a whole rule. A block or a gap MAY be a half rule, as spec
  004 allows for 36px controls and 12px gaps, when it pairs with another half rule.
- **FR-007**: A block whose face, size or line count differs from the body MUST move its text onto the rule without
  changing the space it fills.
- **FR-008**: The scheme MUST ship two rulings: ruled and blank.
- **FR-009**: The ruled sheet MUST use the workbench look: feint blue rules at 1px, the red margin rule where it
  applies, and the workbench grain.
- **FR-010**: The look MUST have values for the dark ramp: rules `oklch(0.74 0.05 240 / 0.24)`, margin rule
  `oklch(0.66 0.15 22 / 0.58)`, grain 8% soft-light. In light: rules `hsl(205 28% 62% / 0.55)`, margin rule
  `hsl(356 70% 60% / 0.75)`, grain 6% multiply.
- **FR-011**: The margin rule MUST appear on every sheet in the main reading column of the recipe and variant pages,
  on every wizard and account form sheet, and in cook mode, and only on a sheet at least 480px wide. Heroes, toolbars,
  cards, the header, home blocks and empty states MUST NOT carry it.
- **FR-012**: The workbench grain MUST add to the crayon wax. It does not replace it.
- **FR-013**: The scheme MUST work the same in Vue components and Razor views.
- **FR-014**: The brand docs MUST describe the shim, the 2px offset, the look and the margin rule's reach. The
  engineering contract MUST describe the rulings, the block types, the shim and how a role's drop comes from `--bl`,
  `k` and its `--text-*` token.
- **FR-015**: Every text colour MUST pass the contrast table against the new paper, rules and grain, in both ramps.
- **FR-016**: Every structural hook that a test locates by, such as a `data-testid`, a role or a `kcc-*` class, MUST
  survive.
- **FR-017**: A label centred beside or on a control MUST sit on the nearest rule. The control MUST be filled with
  paper and MUST centre on the caps of the line it shares, so its own text sits on the same rule, and its row MUST
  still fill a whole number of rules. The scope includes at least "Sort" on the recipe search toolbar, "Makes" on
  the variant page, the photo count, "To taste", and the Add Variant tile title.
- **FR-018**: Every field MUST be written on the line: no pill, its text on the rule, its underline drawn on that
  rule in `hair-strong` (2px `ink` on focus), and a hand-drawn APCasual X in the margin red before the input in place
  of the leading icon. The label stays above the line. A single-line field keeps a 36px hit area. A textarea has no
  box: its text writes on the sheet's own rules, and the X starts its first line. Selects and search fields follow
  the same rule.
- **FR-019**: Every icon button MUST be 32px. Every other control keeps its 36px or 48px height.

### Key Entities

- **Rule**: spec 004's `--bl`, 24px, the one unit for rule spacing, line height and the gaps between blocks.
- **Baseline constant (k)**: one number per font family that places its baseline at any line height.
- **Ruling**: what a sheet draws on its ground, ruled or blank.
- **Margin rule**: a red vertical rule a fixed number of columns from the sheet's left edge, with the copy starting
  to its right.
- **Block**: a piece of a sheet that fills a whole or half number of rules: text in a role of spec 004, or a box with
  no text, such as an image or a control.
- **Centred label**: text centred on a control taller than its line, moved onto the nearest rule by the shim.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On each public page type, every text baseline lies 2px above the top of its rule, within 0.75px.
- **SC-002**: The same page on macOS and Windows places every baseline in the same place, within 0.5px.
- **SC-003**: Every ruled sheet uses the 24px rule, `--bl`, and the text after every block starts on a whole rule.
- **SC-004**: No page scrolls sideways at 375px wide.
- **SC-005**: Every text colour passes the contrast table in both ramps.
- **SC-006**: A new sheet built from spec 004's roles puts its text on the rule with no rule written for that sheet.
- **SC-007**: The brand review finds no sheet off its rule and no margin rule outside the places the brand rules name.
- **SC-008**: Each label that FR-017 names, and the text inside its control, lies 2px above the top of its rule,
  within 0.75px.

## Assumptions

- The workbench's maths and `paper.css` structure are the reference. The site's own token names and the kit's
  `@layer components` home replace the workbench's names where they differ.
- The workbench's `#sheet` page rules are workbench chrome and do not ship.
- The pencil `rule` token is replaced by the feint blue rule colour, in both ramps.
- Sono's constant is measured from its font files, as Hazelnut's and APCasual's were.
- The look preview of 2026-10-19 settled FR-010, FR-011, FR-012 and the controls; `mockups/` holds the approved
  round.
- Spec 004 sets every role's size and line height. This spec adds no size.
- Spec 004 lands before this feature is built, because the scheme reads its tokens.
- Spec 002, paper piles, uses the blank ruling for its under-sheets. Either feature may land first.
