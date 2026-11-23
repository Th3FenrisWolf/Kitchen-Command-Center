# Feature Specification: Design Consistency, Phase 1

**Feature Branch**: `011-design-consistency`

**Created**: 2026-11-18

**Status**: [On the project board](https://github.com/users/Th3FenrisWolf/projects/3?filterQuery=key%3A011-design-consistency)

**Idea**: [#154](https://github.com/Th3FenrisWolf/Kitchen-Command-Center/issues/154)

**Input**: User description: "Design consistency / Cleanup. Centered elements. Inputs have transparent background -
inputs still visible. Spacing steps are even and consistent. Site is readable on desktop and mobile." and "phase 1 of
a phased feature. Phase 2 (later) is the whole Create Recipe and Add Variant flow: the "How do you want to start?"
start screen and steps 1-5. Phase 1 is every other public page.", with the owner's decisions from marked-up
screenshots of every page

## Phases

| Phase | Spec | Covers |
|---|---|---|
| 1 | 011 (this spec) | Every public page except the Create Recipe and Add Variant flow: notebook tops, margin rules, merged sheets, the header bar, the phone menu and search panel, transparent controls, even spacing and readability |
| 2 | later | The Create Recipe and Add Variant flow: the "How do you want to start?" screen and steps 1 to 5 |
| 3 | later | The Home page's blocks: a marked-up review of the hero, cards and stacker, and their fixes |

## Background

The owner went through every public page at desktop (1280px) and phone (390px) width, signed out and signed in, and
marked what looks wrong. Most of the marks fall into a few patterns that repeat across pages:

- A page's title sheet is ruled from top to bottom, so the title floats among rules. The owner wants it to look like
  the top of a real sheet of notebook paper: a plain band at the top that holds the kicker and the title, then the
  ruling starts.
- Some sheets that hold a page's copy have no margin rule, while the Account Settings form sheets have one.
- A page's title sits on one sheet and the summary right under it (At a glance, How do you want to start?) on another,
  with desk between them, when they read as one thing.
- Controls on a sheet are filled with paper, so the rules and washes stop at them. In the Dark ramp each field reads
  as a dark bar laid over the sheet.
- On the recipe page, the variant search, sort, view toggle and filter pills lie on the desk, so a member writes on
  the desk rather than on paper.

The screenshots and the owner's marks are kept locally in `.mockups/154-markup/`, which git ignores.

## Clarifications

### Session 2026-11-18

These answers come from the owner's walk through the marked-up screenshots, before this spec was written.

- Q: What does the line drawn under each page title mean? → A: The title sheet should look like the top of a sheet of
  notebook paper: an unruled header band holds the kicker and the title, and the ruling starts below it.
- Q: What does the vertical line at a sheet's left edge mean? → A: The sheet should have a margin rule.
- Q: What should happen to the title sheet and the sheet circled with it? → A: They merge into one sheet.
- Q: What does the X over the margin rule on the Account Settings sheets mean? → A: Those form sheets drop the margin
  rule. The rule stays everywhere else it is today.
- Q: What should change in the header bar? → A: Everything in the bar shares one vertical centre line.
- Q: The marks on the Recipes page's LIBRARY tab, its search field, and the Legendary Lasagna kicker and rating row?
  → A: Withdrawn. The owner crossed out arrows they had changed their mind about. Those stay as they are.
- Q: Where does Add Variant go on the recipe page? → A: The top-right of the merged sheet. The description stays where
  it is.
- Q: What should change about the variant page's Ingredients, Instructions and Nutrition? → A: They merge into one
  sheet.
- Q: Where do the recipe page's variant search, sort, view toggle and filter pills go? → A: Onto a sheet of their own,
  filter pills included. Nobody writes on the desk.
- Q: What should the phone menu's Diets show? → A: A two-column icon list with counts, like Meals.
- Q: What is wrong with Sign in in the phone menu? → A: It overlaps the dashed divider and crowds the sheet's edge.
- Q: What should change in the phone search panel? → A: The search icon is too big inside its green active circle, the
  Try pills go in mealtime order (Breakfast, Lunch, Dinner), and the panel gets an All Recipes link. The phone menu's
  nav gets an All Recipes link too.
- Q: Where does Account Settings' Back to profile go? → A: It becomes a breadcrumb, like the recipe pages.
- Q: Which cards get the notebook top? → A: The featured cards only: the Top rated card on Recipes and the Top variant
  card on the recipe page. The small grid cards stay as they are.
- Q: What does the up arrow under the Filters sheet's Reset mean? → A: Reset moves up onto the FILTERS tab's line.
- Q: Do the Create Recipe and Add Variant start screens belong to phase 1? → A: No. The whole flow, start screen and
  steps, is phase 2.
- Q: Which controls lose their paper fill? → A: The owner wants it gone from every control on a sheet, but worries a
  rule running through a ghost button or a segmented control will look strange. A look round of
  `/speckit-preview-run` after clarify settles it (see the clarify session below).

### Session 2026-11-18 (clarify)

- Q: Should each diet get an icon, so the phone menu's Diets list matches Meals? → A: Yes. Each diet tag gets an icon
  picked in the backoffice, like the meal categories.
- Q: Which meals do the phone search panel's Try pills show? → A: Always Breakfast, Lunch and Dinner, in that order,
  whatever their recipe counts.
- Q: How are the old sheets labelled inside a merged sheet? → A: Each part gets a caps kicker heading inside the
  sheet, and the merged sheet keeps at most one green tab label.
- Q: How is the variant page's merged Ingredients, Instructions and Nutrition sheet laid out? → A: Ingredients and
  Instructions side by side, Nutrition across the full width below; stacked in that order on a phone.
- Q: Is the Home page in phase 1? → A: Only for the site-wide changes (header, controls, spacing). Its blocks get
  their own review in a later phase.
- Q: Which control-fills direction? → A: Nothing filled, with every button's text on a rule; see mockups/README.md
- Q: The Recipes page's applied-filter chips and phone Filters button lie on the desk; which sheet do they move
  onto? → A: A slim sheet of their own above the results grid.

### Session 2026-11-20

- Q: Which changes to what the nav holds stay in this phase? → A: None. The phone menu's Diets icon list (FR-014,
  FR-014a), its All Recipes link (FR-015) and the phone search panel's Try pills and All Recipes link (FR-017) move to
  012 Navigation Structure, which is built after this phase. This phase keeps the header's design fixes.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Pages read like sheets of notebook paper (Priority: P1)

A visitor opens any phase-1 page and sees its title at the top of a sheet of notebook paper: a plain band holds the
kicker and the title, then the ruled lines begin. A sheet a visitor reads down the page carries its red margin rule,
and things that belong together share one sheet.

**Why this priority**: The notebook top, the margin rule and the merged sheets change every page's first impression,
and the owner marked them on almost every page.

**Independent Test**: Open the 404 page, Registration complete, Account Settings, the Recipes page, a recipe and a
variant at both widths and both ramps, and check each title sheet, each margin rule and each merged sheet against this
story.

**Acceptance Scenarios**:

1. **Given** the 404 page, **When** it opens, **Then** the kicker and title sit in an unruled header band at the top
   of the sheet, the ruled lines start below the title, and the sheet carries a margin rule.
2. **Given** a recipe page, **When** it opens, **Then** the title, its kicker, its meta row, the description, Add
   Variant and At a glance share one sheet, with the notebook top, and Add Variant sits at the sheet's top-right.
3. **Given** a variant page at desktop width, **When** it opens, **Then** the title and At a glance share one sheet
   with the notebook top, and Ingredients and Instructions sit side by side on one sheet with Nutrition across its
   full width below, each part under a caps kicker heading.
4. **Given** a variant page at phone width, **When** it opens, **Then** Ingredients, Instructions and Nutrition stack
   in that order on their one sheet.
5. **Given** the Recipes page, **When** it opens, **Then** its top sheet has the notebook top, and the Top rated card
   has the notebook top and a margin rule while the grid cards are unchanged.
6. **Given** a recipe page, **When** it opens, **Then** the Top variant card has the notebook top and a margin rule.
7. **Given** a variant with no reviews, **When** its page opens, **Then** the empty reviews sheet carries a margin
   rule.
8. **Given** Account Settings, **When** it opens, **Then** its title sheet has the notebook top, its Profile, Change
   password, Appearance and Sign out sheets carry no margin rule, and a breadcrumb (Home · Profile · Settings) replaces
   the Back to profile button.
9. **Given** any phase-1 sheet at phone width, **When** it opens, **Then** the notebook top still holds the title, and
   the margin rule follows the kit's existing narrow-sheet behaviour.

---

### User Story 2 - Members write on paper, never on the desk (Priority: P1)

A member searching, sorting or filtering a recipe's variants does it on a sheet. Every field, segmented control and
pill sits on paper, and a control on a sheet no longer blocks the rules and washes behind it.

**Why this priority**: The owner's rule is that nobody writes on the desk, and the paper-filled controls were the
idea's first complaint. The fills read as dark bars in the Dark ramp.

**Independent Test**: Open a recipe page and Account Settings in both ramps after a visit long enough for washes to
bloom, and check where each control sits and what shows through it.

**Acceptance Scenarios**:

1. **Given** a recipe page, **When** it opens, **Then** Search variants, Sort, the grid/list toggle and the filter
   pills sit together on one sheet above the variant grid, and none of them lies on the desk.
2. **Given** a sheet with a field and a bloomed wash, **When** the page is viewed in either ramp, **Then** the field
   shows the sheet's paper, rules and wash through it, and its underline and red X keep it plainly visible.
3. **Given** a ghost button, segmented control or badge on a sheet, **When** the page is viewed, **Then** it carries
   no paper fill, the rules and washes run through it, and its label's text sits on a rule.
4. **Given** any phase-1 page, **When** it is searched for controls, **Then** no field, segmented control, pill or
   button lies outside a sheet.
5. **Given** the Recipes page with filters applied, **When** it opens, **Then** the applied-filter chips and, on a
   phone, the Filters button sit on a slim sheet of their own above the results grid.

---

### User Story 3 - The header bar and the phone menus are tidy (Priority: P2)

A visitor sees the header bar's logo, Recipes menu, My kitchen menu, search and right-hand button on one vertical
centre line. On a phone, the menu's Sign in and the search panel's icon sit with room around them.

**Why this priority**: The header shows on every page, but each change in it is small and local.

**Independent Test**: Open the home page signed out and signed in at both widths, open the phone menu and the phone
search panel, and check each item below.

**Acceptance Scenarios**:

1. **Given** any page at desktop width, **When** it opens, **Then** the logo, Recipes, My kitchen (signed in), the
   search field and the right-hand button share one vertical centre line in the header bar.
2. **Given** any page at phone width, **When** it opens, **Then** the logo, the search button and the Menu button
   share one vertical centre line in the header bar.
3. **Given** the phone menu signed out, **When** it opens, **Then** Sign in clears the dashed divider and keeps the
   sheet's padding from its edge.
4. **Given** the phone search panel, **When** it opens, **Then** the search icon sits inside its active circle with
   room around it.

---

### User Story 4 - Spacing is even and every page reads at both widths (Priority: P2)

A visitor moves between pages and finds the same spacing steps everywhere, with nothing cut off or scrolling sideways
on a phone or a desktop.

**Why this priority**: The idea asked for it, and it guards every change above against new crowding.

**Independent Test**: Walk every phase-1 page at 390px and 1280px in both ramps and check its spacing and overflow.

**Acceptance Scenarios**:

1. **Given** any phase-1 page, **When** it is viewed at 390px or 1280px, **Then** it does not scroll sideways and no
   text or control is cut off.
2. **Given** any phase-1 page, **When** its blocks are measured, **Then** every vertical gap between blocks is a whole
   or half number of rules.
3. **Given** the Recipes page's Filters sheet, **When** it opens, **Then** Reset sits on the FILTERS tab's line and
   the sheet opens straight onto Category.

---

### Edge Cases

- A merged recipe sheet whose description is long, or a recipe with no description: Add Variant stays at the top-right
  and the description keeps its place (FR-007) without running under or behind the button.
- A variant whose instructions run far longer than its ingredients, or one with no nutrition: the merged sheet keeps
  its parts in reading order with no empty band.
- A recipe with one variant: the variant tools sheet still holds its controls, and no control lies on the desk.
- A narrow sheet below the kit's narrow-sheet width: the margin rule follows the kit's existing narrow-sheet rule, and
  the notebook top's band holds a title that wraps to several lines.
- A page title long enough to wrap: the ruling still starts below the title's last line.
- The Dark ramp and a fresh visit with no washes bloomed: transparent controls stay visible on bare paper too.
- Windows High Contrast (forced colours): a field without a paper fill still draws its underline and its focus line.

## Requirements *(mandatory)*

### Functional Requirements

**Notebook top**

- **FR-001**: A phase-1 page's title sheet MUST open with an unruled header band holding its kicker, title and any meta
  row beside the title, with the sheet's ruling starting below that band. This covers the 404 page, Registration
  complete, Account Settings, the Recipes page's top sheet, the recipe page's merged sheet and the variant page's
  merged sheet.
- **FR-002**: The Recipes page's Top rated card and the recipe page's Top variant card MUST have the notebook top and a
  margin rule, the rule at sheet widths of 480px and up as FR-004 sets. Grid cards MUST stay as they are.
- **FR-003**: The notebook top MUST hold in both ramps and at both widths, and text in the band MUST keep the kit's
  baseline rhythm, so the first ruled line below the band falls on the sheet's rule spacing.

**Margin rules**

- **FR-004**: Every phase-1 title sheet, the empty reviews sheet and the merged sheets MUST carry a margin rule at
  sheet widths of 480px and up. Below that the kit's narrow-sheet drop applies, as it does today.
- **FR-005**: The Account Settings Profile, Change password, Appearance and Sign out sheets MUST carry no margin rule.
- **FR-006**: Sheets that carry a margin rule today and are not named in FR-005 MUST keep it.

**Merged sheets and layout**

- **FR-007**: On the recipe page, the title sheet and At a glance MUST be one sheet, with Add Variant at its top-right
  and the description where it is today.
- **FR-008**: On the variant page, the title sheet and At a glance MUST be one sheet, and Ingredients, Instructions and
  Nutrition MUST be one sheet. At desktop width Ingredients and Instructions sit side by side with Nutrition across
  the full width below. On a phone the three stack in that order.
- **FR-008a**: Inside a merged sheet, each former sheet's part (At a glance, Ingredients, Instructions, Nutrition)
  MUST open with a caps kicker heading, and the merged sheet MUST carry at most one green tab label.
- **FR-009**: On the recipe page, Search variants, Sort, the grid/list toggle and the filter pills MUST sit together on
  one sheet above the variant grid.
- **FR-010**: A field, segmented control, pill or button on a phase-1 page MUST NOT lie on the desk.
- **FR-010a**: On the Recipes page, the applied-filter chips and the phone Filters button MUST sit on a slim sheet of
  their own above the results grid.
- **FR-011**: On the Recipes page's Filters sheet, Reset MUST sit on the FILTERS tab's line.
- **FR-012**: Account Settings MUST show a breadcrumb (Home · Profile · Settings) in place of the Back to profile
  button.

**Header bar and phone menus**

- **FR-013**: Every item in the header bar MUST share one vertical centre line, at both widths, signed out and signed
  in.
- **FR-014, FR-014a, FR-015**: Moved to 012 Navigation Structure (its FR-017 and FR-018).
- **FR-016**: The phone menu's Sign in MUST clear the dashed divider and keep the sheet's padding from its edge.
- **FR-017**: Moved to 012 Navigation Structure (its FR-021).
- **FR-018**: The phone search button's icon MUST sit inside its active circle with room around it, sized to match the
  header's other icons.

**Transparent controls**

- **FR-019**: Fields on a sheet MUST carry no paper fill: the sheet's paper, rules and washes show through them, and
  their underline and red X keep them visible in both ramps and in forced colours.
- **FR-020**: Ghost buttons, segmented controls, badges and every other control on a sheet MUST carry no paper fill,
  so the sheet's rules and washes run through them. The approved look is `mockups/control-fills.html`.
- **FR-020a**: The text of every button, segment and badge on a sheet MUST sit on a rule, in every row of controls,
  whether the row is a control's first line or a wrapped one. A control in the notebook top's band, where no rule is
  drawn, sits on the baseline where the rule would fall (FR-003), as a heading action shares its title's baseline.

**Spacing and readability**

- **FR-021**: Every vertical gap between blocks on a phase-1 page MUST be a whole or half number of rules.
- **FR-022**: A phase-1 page MUST NOT scroll sideways or cut off text or a control at 390px or 1280px, in either
  ramp.
- **FR-023**: Every phase-1 change MUST keep text at WCAG AA contrast in both ramps.

**Brand contract**

- **FR-024**: The brand docs MUST describe the notebook top, the new margin-rule placements (title sheets, featured
  cards, the empty reviews sheet), the Account Settings exception, controls with no paper fill and their labels on
  the rule, and the rule that no control lies on the desk. The kit's current ban on a margin rule for heroes, cards and empty states MUST change
  to match.

**Out of scope**

- **FR-025**: The Create Recipe and Add Variant flow, from the "How do you want to start?" screen through step 5, MUST
  stay as it is in this phase.
- **FR-025a**: The Home page's blocks MUST stay as they are in this phase. Home takes only the site-wide changes: the
  header bar, the menus, the controls' fills and spacing.
- **FR-026**: The Recipes page's LIBRARY tab, its page search field, and the Top rated card's kicker and rating row
  MUST stay as they are, apart from FR-001 and FR-002.

### Key Entities

- **Notebook top**: a sheet's unruled header band at its top, holding the kicker, title and meta, with the ruling
  starting below it.
- **Title sheet**: the first sheet of a page, holding the page's title. After this phase, a merged sheet on the recipe
  and variant pages.
- **Featured card**: the one large card that leads a grid: Top rated on the Recipes page and Top variant on the recipe
  page.
- **Variant tools sheet**: the recipe page's sheet holding Search variants, Sort, the grid/list toggle and the filter
  pills.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Every phase-1 title sheet and both featured cards show the notebook top at 390px and 1280px in both
  ramps: 6 title sheets and 2 cards, 32 views, all pass.
- **SC-002**: Zero fields, segmented controls, pills or buttons lie on the desk on any phase-1 page.
- **SC-003**: Every margin-rule change holds at sheet widths of 480px and up: the title sheets, the merged sheets, the
  featured cards and the empty reviews sheet show the rule, and the four Account Settings form sheets do not.
- **SC-004**: The header bar's items have vertical centres within 1px of one another on every phase-1 page at both
  widths, signed out and signed in.
- **SC-005**: Zero phase-1 pages scroll sideways at 390px or 1280px in either ramp.
- **SC-006**: Every vertical gap between blocks on phase-1 pages is a whole or half number of rules.
- **SC-007**: The kit's contrast table and enforcement tests pass, and no text falls below WCAG AA in either ramp.
  The enforcement tests that pin today's contract change with it in this phase: the controls' paper fills on a sheet
  become no fill, and the margin rule's placements follow FR-004 and FR-005.
- **SC-008**: The owner signs off a fresh set of screenshots of every phase-1 page with no new marks against the
  changes in this spec.
- **SC-009**: Every button, segment and badge label on a phase-1 sheet has its text baseline within 1px of a rule, or
  in a notebook top's band of where the rule would fall, at 390px and 1280px.

## Assumptions

- Phase-1 pages are Home (site-wide changes only), Recipes (with and without filters), a recipe, a variant, cook mode, Sign in, Register,
  Registration complete, 404, Account Settings, the signed-in header and the header's menus and search panels.
- Cook mode keeps its sheet and margin rule as they are. The owner did not mark it.
- The kit's existing narrow-sheet behaviour for the margin rule (the rule goes below 480px of sheet width) still holds,
  so phone pages show the notebook top without a margin rule on narrow sheets.
- The variant tools sheet is a plain kit sheet, as the approved look round shows it (`mockups/control-fills.html`).
- The Recipes page's own search field, sort and view stay on its top sheet. Its applied-filter chips and phone
  Filters button lie on the desk today and move to their own sheet (FR-010a).
- Washes, tears, type sizes and the rule spacing stay as they are. This phase arranges sheets and controls, and
  changes no part of the paper itself beyond the header band.
