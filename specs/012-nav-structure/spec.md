# Feature Specification: Navigation Structure

**Feature Branch**: `012-nav-structure`

**Created**: 2026-11-20

**Status**: [On the project board](https://github.com/users/Th3FenrisWolf/projects/3?filterQuery=key%3A012-nav-structure)

**Idea**: [#166](https://github.com/Th3FenrisWolf/Kitchen-Command-Center/issues/166), [#167](https://github.com/Th3FenrisWolf/Kitchen-Command-Center/issues/167)

**Input**: User description: "add dark/light toggle back to navigation" and "brainstorm: do we need more navigation
items? anthing missing from nav that would be helpful for users?", rolled into one spec, with the owner's direction to
take every nav structure and content change out of 011 and leave 011's design fixes where they are

## Background

The header is a notepad: a torn top sheet holding the bar, with menus that open as cards below it. Today the desktop
bar holds the logo, a Recipes menu, a My kitchen menu for members, search, and Sign in or New recipe. A guest sees
three things to use. The phone bar holds the logo, a search button and Menu.

The bar used to carry a sun and moon button that flipped light and dark for everyone. The pad redesign took it out
and made the ramp a member setting under Account Settings' Appearance (Device, Light or Dark). Since then a visitor
follows their device and has no way to choose.

Spec 011 (Design Consistency, phase 1) fixes how the header and its menus look. It also held four changes to what the
menus hold. Those four move here, so 011 keeps only design fixes:

- 011 FR-014 and FR-014a, the phone menu's Diets as a two-column icon list with counts, with a diet icon an editor
  picks, become FR-017 and FR-018 here.
- 011 FR-015, the phone menu's All Recipes link, becomes part of FR-017 here.
- 011 FR-017, the phone search panel's Try pills in mealtime order and its All Recipes link, becomes FR-021 here.

011 keeps the header bar's shared centre line, the phone Sign in's spacing and the phone search icon's size. This
feature builds on 011's header: it is built after 011.

The approved looks are in `mockups/`: `nav.html` for desktop and `phone.html` for the phone menu, with a light and a
dark screenshot of each, and `mockups/README.md` recording the options the owner turned down.

## Clarifications

### Session 2026-11-20

- Q: Who gets the light/dark toggle in the nav? → A: Everyone, signed in or out.
- Q: What does one press do? → A: It flips between light and dark. Device stays a choice in Account Settings only.
- Q: Where does the toggle sit? → A: In the header bar on desktop, inside the Menu card on a phone.
- Q: How long is a visitor's pick kept? → A: On this device, across visits, and the first paint already uses it.
- Q: What does a member's press do to their Appearance setting? → A: It saves to the account, the same as choosing
  Light or Dark in Account Settings.
- Q: Which of 011's requirements move here? → A: The phone menu's Diets icon list (011 FR-014, FR-014a), the phone
  menu's All Recipes link (011 FR-015) and the phone search panel's Try pills and All Recipes link (011 FR-017). The
  Account Settings breadcrumb (011 FR-012) stays in 011.
- Q: Does "Make everything private" (#153) bear on the nav? → A: No. It is about the repository, not the site.
- Q: How are features not built yet treated? → A: The spec maps where each will go in the nav. None of them ships
  here.
- Q: Which desktop nav direction? → A: Menus over meal tabs; see mockups/README.md
- Q: Do guests get a register link in the desktop bar? → A: Yes, Join beside Sign in.
- Q: Which phone menu direction? → A: Divider-tabbed menu; see mockups/README.md
- Q: What counts as Last cooked, and where is it kept? → A: The last variant a member opened in cook mode, saved on
  the account, so it follows them to every device. Guests have no Last cooked.
- Q: Which meals hang as tabs under the desktop header? → A: The meals the owner picks for them in Site Settings, in
  the owner's order, then All. The baseline seeds Breakfast, Lunch, Dinner, Dessert and Snack. (Revised 2026-11-23:
  the first answer fixed the five in code, which constitution I forbids for owner-edited nav entries.)
- Q: Where do the phone search panel's Try pills come from? → A: The existing Site Settings search suggestions, in the
  owner's order. The baseline seeds Breakfast, Lunch and Dinner.
- Q: Do members see Plan before it is built? → A: Yes, dashed and marked soon, as the mockups show.
- Q: Where does Surprise me live? → A: In both Browse's Quick picks and the Cook menu.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Anyone picks light or dark from the nav (Priority: P1)

A visitor reading a recipe at night switches the site to dark from the header, and the site is still dark the next
time they open it on that device. A member does the same and finds the choice saved on their account.

**Why this priority**: It is idea #166, and since the pad redesign a visitor has had no way to choose.

**Independent Test**: Signed out, press the toggle, reload and open the site again; then sign in on two devices,
press it on one and open Account Settings on the other.

**Acceptance Scenarios**:

1. **Given** a visitor on any page in the light ramp at desktop width, **When** they press the toggle in the header
   bar, **Then** the page switches to dark at once.
2. **Given** a visitor who switched to dark, **When** they come back to the site on the same device, **Then** the
   first paint is dark, with no flash of light.
3. **Given** a visitor who has never pressed the toggle, **When** they open the site, **Then** it follows their
   device's light or dark setting, as it does today.
4. **Given** a member whose Appearance is Light, **When** they press the toggle, **Then** the page switches to dark
   and their Appearance setting becomes Dark, on every device they sign in on.
5. **Given** a member whose Appearance is Device and whose device is light, **When** they press the toggle, **Then**
   the page switches to dark and their Appearance setting becomes Dark.
6. **Given** a phone, **When** the Menu card is open, **Then** the toggle sits in the card's account foot and works
   the same way.

---

### User Story 2 - On desktop, Browse and Cook lead the bar and the owner's meals are one click away (Priority: P1)

A visitor at desktop width sees Browse and Cook where Recipes is today, and a row of meal tabs hanging under the
header. One click on Dinner shows every dinner recipe. Cook gathers what they were just looking at and a random pick.

**Why this priority**: It is idea #167. The desktop bar looks empty, especially to a guest, and the content that
exists is hidden behind one menu.

**Independent Test**: Open any page at 1280px signed out and signed in, open each menu, and follow each meal tab.

**Acceptance Scenarios**:

1. **Given** any page at desktop width, **When** it opens, **Then** the bar reads logo, Browse, Cook, search, the
   toggle, then Sign in and Join for a guest or My kitchen and New recipe for a member, and members also see Plan
   after Cook.
2. **Given** the Browse menu, **When** it opens, **Then** its card holds Meals, Diets and Quick picks with their
   counts and an All recipes link with the total, as the Recipes card does today.
3. **Given** any page at desktop width, **When** it opens, **Then** a tab for each meal the owner picked, in
   the owner's order, and All hang under the header's right edge, each with its recipe count.
4. **Given** the Dinner tab, **When** it is clicked, **Then** the Recipes page opens filtered to Dinner, and the
   Dinner tab shows as the current one.
5. **Given** a member who has cooked and viewed recipes, **When** they open Cook, **Then** its card holds Recently
   viewed, Last cooked (the recipe and variant) and Surprise me.
6. **Given** a guest, **When** they open Cook, **Then** its card holds Recently viewed and Surprise me, and no Last
   cooked.
7. **Given** an open Browse or Cook card, **When** it is open, **Then** it does not cover the meal tabs.

---

### User Story 3 - On a phone, the menu is short and tabbed (Priority: P1)

A visitor on a phone opens Menu and finds Browse, Cook and, for members, Plan as divider tabs along the card's top.
Browse lists meals and diets with icons and counts. Their account, the toggle and Sign in or Join sit at the foot.

**Why this priority**: Most cooking happens with a phone in hand, and the phone menu has to carry the same structure
as the desktop.

**Independent Test**: Open the Menu at 390px signed out and signed in, switch between its tabs, and follow a meal, a
diet and All Recipes.

**Acceptance Scenarios**:

1. **Given** any page at phone width, **When** it opens, **Then** the bar holds the logo, the search button and Menu
   as today, with no meal tabs under it.
2. **Given** the Menu card, **When** it opens, **Then** Browse, Cook and, for a member, Plan sit as divider tabs along
   its top, Browse is chosen, and only Browse's section shows.
3. **Given** Browse in the phone menu, **When** it shows, **Then** Meals and Diets are two-column lists with each
   one's icon and count, followed by Quick picks and an All Recipes link.
4. **Given** the Cook tab, **When** it is tapped, **Then** the card shows Cook's section in place of Browse's, without
   leaving the card.
5. **Given** a guest, **When** the Menu card opens, **Then** its foot holds the toggle, Sign in and Join.
6. **Given** a member, **When** the Menu card opens, **Then** its foot holds their kitchen lines, the toggle, New
   recipe and Sign out.
7. **Given** a diet whose icon is not set in the backoffice, **When** Browse shows, **Then** that diet's row shows its
   name and count with no icon, still on the list's rhythm.
8. **Given** the phone search panel, **When** it opens, **Then** the Try pills read the owner's search suggestions in
   their order (Breakfast, Lunch, Dinner on the baseline), and the panel holds an All Recipes link.

---

### User Story 4 - A guest can join from the desktop bar (Priority: P2)

A guest who likes what they see finds Join beside Sign in, without first going to the sign-in page.

**Why this priority**: The phone menu has offered a register link all along; desktop has not.

**Independent Test**: Open any page signed out at desktop width and follow Join.

**Acceptance Scenarios**:

1. **Given** a guest at desktop width, **When** any page opens, **Then** Join sits beside Sign in, Join as the marker
   green button and Sign in as the quieter one.
2. **Given** Join, **When** it is clicked, **Then** the registration page opens, the same page the phone menu's
   register link opens.

---

### User Story 5 - Members see where planning will go (Priority: P3)

A member sees Plan, dashed and marked soon, in the desktop bar and among the phone menu's tabs. It shows that meal
plans, shopping lists and households are coming, and where they will live.

**Why this priority**: It opens nothing yet. It sets the place later features fill.

**Independent Test**: Open the header signed in at both widths and try Plan by pointer and by keyboard.

**Acceptance Scenarios**:

1. **Given** a member, **When** the header shows, **Then** Plan sits after Cook, dashed and marked soon.
2. **Given** a guest, **When** the header shows, **Then** there is no Plan.
3. **Given** a member who clicks, taps or focuses Plan, **When** they activate it, **Then** nothing opens, and a
   screen reader announces it as unavailable and coming soon.

---

### Edge Cases

- A member signs out: the page then follows the visitor rule, the device's own pick if the toggle was pressed while
  signed out on it, else the device's light or dark setting.
- A visitor who picked dark signs in as a member whose Appearance is Light: the member's saved setting wins.
- A member opens a variant in cook mode on a phone and then opens Cook on a laptop: Last cooked shows that variant.
- The variant in Last cooked is deleted or unpublished: Last cooked shows nothing rather than a broken link.
- Nothing viewed yet on this device: Cook's Recently viewed section is left out, and Surprise me still shows while
  the library has recipes.
- A picked meal has no recipes: its tab shows a count of 0 and still links to its filtered list.
- A picked meal's category is deleted or unpublished: its tab is left out.
- The owner picks no meals for the tabs: only All hangs under the header.
- The backoffice holds meals the owner did not pick for the tabs, such as Beverage: they never show as tabs, and show
  in Browse's Meals list when Browse's own Meals pick and their counts allow.
- A desktop window narrow enough to crowd the bar, down to the width where the phone bar takes over: no item
  overlaps another, and the meal tabs stay on one row.
- The pad tucks away on scroll: the meal tabs tuck with it and come back with it.
- Windows High Contrast (forced colours): the toggle, the tabs and the dashed Plan stay visible and keep their focus
  lines.
- Reduced motion: switching ramps and switching phone menu tabs happen without animated colour or slide.

## Requirements *(mandatory)*

### Functional Requirements

**Light/dark toggle**

- **FR-001**: The header MUST carry a light/dark toggle for every visitor, signed in or out: in the desktop bar
  between search and the account buttons, and in the phone Menu card's account foot.
- **FR-002**: One press MUST switch the page between light and dark at once. The toggle MUST name the ramp it
  switches to for assistive technology ("Switch to dark", "Switch to light").
- **FR-003**: A signed-out visitor's pick MUST be kept on that device across visits, and the first paint of every
  later page MUST already be in that ramp, with no flash of the other one. A visitor who has never pressed the toggle
  MUST follow the device's light or dark setting, as today.
- **FR-004**: A member's press MUST save Light or Dark to their account's Appearance setting, the same as choosing it
  in Account Settings, so it holds on every device they sign in on. A member on Device who presses MUST get the
  opposite of the ramp they see, saved.
- **FR-005**: A member's saved Appearance MUST win over a visitor pick kept on the same device.
- **FR-006**: Switching ramps MUST NOT animate colours across the page.

**Desktop bar and menus**

- **FR-007**: The desktop bar MUST read, in order: logo, Browse, Cook, Plan (members only, FR-022), search, the
  toggle, then Sign in and Join for a guest or My kitchen and New recipe for a member. The Recipes menu MUST go.
- **FR-008**: Browse MUST open a card holding Meals, Diets and Quick picks with their counts, an All recipes link
  with the total, and the owner's hand-written note at its foot when one is set, as the Recipes card does today.
- **FR-009**: Cook MUST open a card holding Recently viewed, then Last cooked for a member, then Surprise me. Recently
  viewed MUST show the same recipes the phone search panel shows today. A section with nothing in it MUST be left
  out.
- **FR-010**: Last cooked MUST show the last variant the member opened in cook mode, by recipe and variant name,
  saved on the account so it shows on every device. Guests MUST NOT see Last cooked.
- **FR-011**: Surprise me MUST stay among the Quick pick presets the owner can place in Browse, and on the default
  list when the owner sets none, and MUST also appear in Cook. Like the preset, Cook's Surprise me MUST be left out
  when the library has no recipes.

**Desktop meal tabs**

- **FR-012**: At desktop width, a tab for each meal the owner picks for the tabs, in the owner's order, then All, MUST
  hang under the header's right edge on every public page, each showing its recipe count. All shows the library's
  total.
- **FR-013**: A meal tab MUST open the Recipes page filtered to that meal, and All the Recipes page with no filter.
  The tab matching the current page's meal filter MUST show as current.
- **FR-014**: The meal tabs MUST be a Site Settings pick of meal categories, in the owner's order, separate from the
  Browse card's Meals pick. The baseline content MUST seed it with Breakfast, Lunch, Dinner, Dessert and Snack. A
  picked category that is deleted or unpublished MUST be left out. A meal with no recipes MUST still show its tab
  with a count of 0.
- **FR-015**: The meal tabs MUST tuck and return with the pad, and an open Browse or Cook card MUST NOT cover them.

**Guests**

- **FR-016**: A guest's desktop bar MUST show Join beside Sign in, Join as the marker green button and Sign in as the
  ghost button. Join MUST open the registration page the phone menu's register link opens today.

**Phone menu**

- **FR-017**: The phone bar MUST stay logo, search button and Menu, with no meal tabs. The Menu card MUST carry
  Browse, Cook and, for a member, Plan as divider tabs along its top, open on Browse, and show only the chosen tab's
  section. Tapping a tab MUST switch sections inside the card. Browse MUST hold Meals and Diets as two-column lists
  with each one's icon and count, laid out alike, then Quick picks and an All Recipes link. Cook MUST hold what
  FR-009 sets.
- **FR-018**: An editor MUST be able to pick an icon for each diet tag in the backoffice, the way a meal category's
  icon is picked today. A diet with no icon set MUST show its name and count without one.
- **FR-019**: The phone menu's tabs MUST work as a tab set for keyboard and assistive technology: arrow keys move
  between them, and the chosen one is announced as selected.
- **FR-020**: The Menu card's account foot MUST hold the toggle, then Sign in and Join for a guest; for a member, the
  member's kitchen lines, then the toggle, then New recipe and Sign out.

**Phone search panel**

- **FR-021**: The phone search panel's Try pills MUST be the Site Settings search suggestions, in the owner's order,
  and the panel MUST hold an All Recipes link. When the owner sets no suggestions, the pills MUST be the three meals
  with the most recipes, as today. The baseline content MUST seed the suggestions with Breakfast, Lunch and Dinner.

**Plan**

- **FR-022**: A member MUST see Plan after Cook in the desktop bar and among the phone menu's tabs, drawn dashed and
  marked soon. Activating it MUST open nothing, and assistive technology MUST announce it as unavailable and coming
  soon. A guest MUST NOT see Plan.
- **FR-023**: The nav map for features not built yet MUST be recorded in the brand docs: Plan will hold meal plans,
  shopping lists and households (#165), and My kitchen will hold Favorites. None of them is built here.

**Brand contract**

- **FR-024**: The brand docs MUST describe the Browse, Cook and Plan menus, the desktop meal tabs, the phone menu's
  divider tabs, the toggle's places and the dashed soon state.

**Out of scope**

- **FR-025**: 011's design fixes MUST stay in 011: the bar's shared centre line, the phone Sign in's spacing, the
  phone search icon's size, and every page change it lists. Every item this feature adds to the header bar MUST
  keep 011's shared centre line.
- **FR-026**: The Home page's blocks, the companion app (#148) and households (#165) MUST stay as they are.

### Key Entities

- **Section menu**: one of Browse, Cook and Plan, a menu in the desktop bar and a divider tab in the phone menu.
- **Meal tab**: a tab under the desktop header for one meal the owner picked, or All, with its count and a link to
  the filtered Recipes page.
- **Visitor ramp pick**: Light or Dark, chosen by a signed-out visitor with the toggle and kept on their device.
- **Last cooked**: the last variant a member opened in cook mode, kept on their account.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A visitor's ramp pick survives 10 out of 10 return visits on the same device, and each first paint is in
  the picked ramp with no flash of the other.
- **SC-002**: A member's toggle press shows in Account Settings and on a second signed-in device on its next page
  load, every time.
- **SC-003**: Each meal the owner picked for the tabs is reachable from any public page in one click at desktop width,
  and each meal in Browse's Meals list in two taps on a phone.
- **SC-004**: On the baseline content, a guest's desktop header offers at least 12 ways in (Browse, Cook, search,
  the toggle, Sign in, Join and six meal tabs), up from 3 today.
- **SC-005**: No header item, tab or open card overlaps another, and nothing scrolls sideways, at 390px, 1024px and
  1280px, in both ramps, signed in and out.
- **SC-006**: Every menu, tab and the toggle can be reached and used by keyboard alone, and a screen reader names
  each one and its state.
- **SC-007**: The owner signs off screenshots of the header at both widths that match the approved mockups.

## Assumptions

- 011's header changes are built first. This feature changes what the header holds; 011 sets how it lines up.
- The desktop and phone bars switch at the width they switch at today.
- Recently viewed keeps its current source, limit and device-only storage. This feature shows it in a second place.
- The phone Menu card opens on Browse each time. It does not remember the last tab chosen.
- The Account Settings Appearance setting keeps all three choices: Device, Light and Dark.
- Cook mode is the variant page's existing cook mode. Opening it is what records Last cooked.
- A meal tab's count is that meal's recipe count from the same library counts the Browse card uses, including for a
  meal the Browse card leaves out.
- The phone menu's and the phone search panel's All Recipes links lead to the Recipes page with no filter.
- #167 stays an idea card on the board: the board reads the first idea on the Idea line, #166, as this spec's card.
