# Feature Specification: Recipe Import from Photos, Phase 2 - The Details Step

**Feature Branch**: `010-recipe-details-step`

**Created**: 2026-11-16

**Status**: [On the project board](https://github.com/users/Th3FenrisWolf/projects/3?filterQuery=key%3A010-recipe-details-step)

**Input**: User description: "001 phase 2". Phase 2 of the photo recipe import that `specs/001-photo-recipe-import/spec.md`
sets out: the Details step for category, tags, difficulty and nutrition.

## The import in phases

The import runs as stacked numbered specs, per the constitution's ordering rule. This spec is Phase 2. Phase 1 passed
its definition of done and is merged into `main`.

| Phase | Spec | Covers |
|---|---|---|
| 1 | 001 | The import into today's wizard fields, with matching, step links on the review, marks and the spending ceiling |
| 2 | 010 (this spec) | The Details step: category, tags, difficulty and nutrition for typed and imported recipes |
| 3 | later | The owner's review of imports: source photos and import notes until the draft is published or deleted |

## Where Phase 1 left it

Phase 1 settled these for this phase, and this spec starts from them:

- Both flows gain a Details step for category, tags, difficulty and nutrition, for typed and imported recipes.
- A read suggests a category, tags and a difficulty from the site's existing lists, and copies nutrition only when the
  photos print it. Nutrition is never estimated.

What the site holds today:

- A recipe has an optional category, one of six: Breakfast, Lunch, Dinner, Dessert, Snack and Beverage.
- A variant has optional tags, any number of eleven. Seven are diet tags (Vegetarian, Vegan, Gluten-Free, Dairy-Free,
  Keto, Low-Carb, High-Protein) and four are style tags (Cheesy, Easy, Fast, Spicy).
- A variant has an optional difficulty (Easy, Medium or Hard) and eight optional nutrition values per serving:
  calories, protein, carbohydrates, fat, saturated fat, fiber and sugar in grams, and sodium in milligrams.
- Only the owner creates categories and tags, in the backoffice. The public site already shows all four kinds of value
  on a published recipe, and search already filters by category and by diet and style tags.
- A member's submission sets none of them today. The owner fills them in the backoffice before publishing.

## Clarifications

### Session 2026-11-16

- Q: Where does the Details step sit? → A: After the instructions step and before the review: basics, variant,
  ingredients, instructions, details, review.
- Q: Must a member fill any Details field? → A: No. Every Details field is optional, and the owner fills gaps in the
  backoffice as today.
- Q: How do suggested values show? → A: With a new mark, "suggested by the reader", and a short reason, on the step and
  on the review. Editing the value clears it, as Phase 1's marks clear.
- Q: May a member propose a category or tag that the site does not have? → A: Yes, as a note to the owner. The site
  never creates the category or tag.
- Q: What happens to nutrition printed per whole recipe or in other units? → A: The read converts it to per-serving
  values in the site's units and marks each converted value as calculated, with the printed value in the reason. When
  the photos print no servings, or the servings were estimated, the per-recipe values stay blank.
- Q: When may the reader suggest a diet tag? → A: Only when the photos state it. Style tags may be suggested from the
  recipe itself.
- Q: Where does a member's proposed category or tag go? → A: Into a field on the draft that only the backoffice shows:
  the recipe holds a proposed category, the variant holds proposed tags.
- Q: May the reader propose a new category or tag? → A: No. Only the member writes a proposal. The reader picks from
  the site's lists.
- Q: How is the reader's accuracy on the new values proven? → A: The reader comparison runs again on a test set whose
  expected answers gain the new values, with at least three recipes that print nutrition, against the bars in SC-004
  to SC-006. The cheapest reader that passes every Phase 1 and Phase 2 bar stays the default.
- Q: Do the Phase 2 defaults for proposals, nutrition limits, contradicted diet tags, lists that change during a flow,
  re-reads, marks and the public site match the intent? → A: Yes, as written in this spec.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Fill in the details of a typed recipe (Priority: P1)

A member types a recipe in Create Recipe. After the instructions they reach a Details step. They pick a category, a
few tags and a difficulty, and they enter the nutrition printed on their source, or leave any of it empty. The review
shows a Details section with an Edit control. They submit, and the draft that reaches the owner already holds what
they chose. Add Variant offers the same step, without the category, because a variant belongs to a recipe that already
has one.

**Why this priority**: It gives every submission the values that drive the site's search filters and cards, and it
works with no paid read at all. The import's suggestions in Story 2 fill the same step.

**Independent Test**: With reading not configured, a member types a recipe, picks the Dinner category, the Spicy and
Gluten-Free tags and Medium difficulty, enters 450 calories, and submits. The draft in the backoffice holds those
values, and the same values show on the recipe page once the owner publishes it.

**Acceptance Scenarios**:

1. **Given** a member on Create Recipe who has finished the instructions step, **When** they continue, **Then** the
   Details step opens with a category choice, the site's tags in their diet and style groups, a difficulty choice, the
   eight nutrition values and the proposal notes, all empty.
2. **Given** a member on the Details step who fills nothing, **When** they continue, **Then** the review opens and
   says that no details were given, and submitting works as before.
3. **Given** a member on Add Variant, **When** the Details step opens, **Then** it offers tags, difficulty, nutrition
   and the tag proposal, and offers no category and no category proposal.
4. **Given** a member on the review, **When** they choose Edit on the Details section, **Then** the Details step opens
   with its current values and offers a direct return to the review.
5. **Given** a member who chose details and submits, **When** the submission succeeds, **Then** the draft recipe holds
   the category and the draft variant holds the tags, the difficulty and the nutrition values.
6. **Given** a member who types a nutrition value that is not a whole number from zero up to its limit, **When** they
   try to continue, **Then** the step says what is wrong with that value and keeps them on the step.

---

### User Story 2 - The import suggests the details (Priority: P1)

A member imports a recipe from photos. When the review opens, its Details section already holds a category, tags and a
difficulty that the reader suggested from the site's lists, each marked as suggested with a short reason. Where the
photos print nutrition, it is copied in. Where they print it for the whole recipe or in other units, it is converted
to per-serving values and marked as calculated. Where they print none, nutrition stays empty.

**Why this priority**: This is what Phase 1 promised for this phase, and it saves the member the most typing on the
step that has the most fields.

**Independent Test**: With the stand-in reader, a member imports a recipe photo that prints a nutrition panel per
serving and says "vegan". The review shows a suggested category and difficulty, a Vegan tag that carries no mark, and
nutrition values equal to the printed ones that carry no mark.

**Acceptance Scenarios**:

1. **Given** an import from Create Recipe that proposes no existing recipe, **When** the review opens, **Then** the
   Details section shows the reader's category, tags and difficulty, each one the photos do not state marked as
   suggested with its reason.
2. **Given** photos that print nutrition per serving in the site's units, **When** the review opens, **Then** each
   printed whole number is filled exactly as printed and carries no mark, and each printed fraction is rounded to a
   whole number and marked as calculated with the printed value in its reason.
3. **Given** photos that print nutrition for the whole recipe and print the servings, **When** the review opens,
   **Then** each value is the printed value divided by the servings, rounded to a whole number, and marked as
   calculated with the printed value in its reason.
4. **Given** photos that print energy in kilojoules, sodium in grams, or salt instead of sodium, **When** the review
   opens, **Then** the value is converted to calories or to milligrams of sodium and marked as calculated with the
   printed value in its reason.
5. **Given** photos that print no nutrition, **When** the review opens, **Then** every nutrition value is empty and
   carries no mark.
6. **Given** photos that print nutrition for the whole recipe and no servings, **When** the review opens, **Then** the
   nutrition values are empty, because the servings the read estimated are not used to divide.
7. **Given** a recipe of tofu, rice and vegetables whose photos never say "vegan", **When** the review opens, **Then**
   no diet tag is filled.
8. **Given** a suggested value, **When** the member changes it on the Details step, **Then** its mark clears on the
   step and on the review.
9. **Given** an import whose member accepts a proposed existing recipe, **When** the review opens, **Then** it shows
   no category and no category proposal, and keeps the suggested tags, difficulty and nutrition.

---

### User Story 3 - Propose a category or tag the site lacks (Priority: P3)

A member's recipe fits none of the site's categories or tags, say a Mexican dish. On the Details step they write a
short proposal, such as "Mexican" for a tag. The proposal reaches the owner with the draft, in a field that only the
backoffice shows. The owner decides whether to add the category or tag and assigns it before publishing.

**Why this priority**: It tells the owner which categories and tags members are missing, but the step is useful
without it.

**Independent Test**: A member types a recipe, writes "Mexican" as a proposed tag and submits. The draft variant in the
backoffice shows the proposal, and the published recipe page never shows it.

**Acceptance Scenarios**:

1. **Given** a member on the Details step of Create Recipe, **When** they write a proposed category and a proposed tag,
   **Then** the review shows both, and the submitted draft holds the proposed category on the recipe and the proposed
   tags on the variant.
2. **Given** a draft that holds a proposal, **When** the owner publishes it, **Then** no page of the public site shows
   the proposal, and search does not index it.
3. **Given** an import, **When** the review opens, **Then** both proposal notes are empty, because the reader never
   writes a proposal.

---

### Edge Cases

- **The owner changes the lists during a flow**: the owner deletes or renames a category or tag after the member's
  page loaded. A submission that names one the site no longer has is refused with a message that names the Details
  step, and the member returns to it with the lists reloaded and their other values kept.
- **No category fits for the reader**: the reader leaves the category empty rather than force a poor fit, and says
  nothing.
- **Several categories could fit**: the reader suggests one, and its reason names the alternative it passed over.
- **Difficulty is printed**: a printed "easy" or "advanced" maps to Easy, Medium or Hard. A printed level that maps
  exactly carries no mark; any other mapping is marked as suggested, with the printed word in the reason.
- **"Easy" as a style tag and as a difficulty**: each is decided on its own. A recipe can be Easy in difficulty without
  the Easy tag.
- **A diet tag the recipe contradicts**: the photos say "vegetarian" and list chicken stock. The tag is filled because
  the photos state it, and it is marked as unclear with the conflicting ingredient in the reason.
- **Nutrition with ranges or "less than"**: "<1 g" or "2 to 3 g" keeps its lower whole number and is marked as unclear
  with the printed text in the reason.
- **Nutrition the site does not hold**: cholesterol, potassium, vitamins and the like are ignored.
- **A printed value over the limit**: it is left empty and marked as unclear, as Phase 1 treats a value that breaks a
  wizard rule.
- **The member reads again**: a new read replaces the Details values too, and Phase 1's warning before a read replaces
  edited values covers edited details. The proposal notes are the member's own and survive a new read.
- **Switching between a match and a new recipe**: when the member changes their answer to the proposed recipe, the
  suggested category comes back with the read's other values once they keep a new recipe again.
- **No reading configured**: the Details step works for typed recipes on every site, CI included.

## Requirements *(mandatory)*

### Functional Requirements

**The Details step**

- **FR-001**: Create Recipe and Add Variant MUST each have a Details step after the instructions step and before the
  review, in typed and imported flows.
- **FR-002**: On Create Recipe, the Details step MUST offer one category from the site's published categories, or
  none.
- **FR-003**: In both flows, the Details step MUST offer any number of the site's tags, shown in their diet and style
  groups, a difficulty of Easy, Medium or Hard, or none, and the eight nutrition values per serving in the site's
  units.
- **FR-004**: The Details step MUST NOT offer a category when the flow adds a variant, whether it started on Add
  Variant or the member accepted a proposed existing recipe.
- **FR-005**: Every Details field MUST be optional. A member MUST be able to continue and submit with all of them
  empty.
- **FR-006**: Each nutrition value MUST be a whole number from zero up to its limit, and the step MUST explain a value
  it refuses.
- **FR-007**: The lists of categories and tags MUST come from the site's content, so a category or tag the owner adds
  in the backoffice appears on the step with no code change.

**Proposals**

- **FR-008**: The Details step MUST let the member write a short proposed category (Create Recipe only) and short
  proposed tags, each up to 100 characters.
- **FR-009**: A submission MUST store the proposed category on the draft recipe and the proposed tags on the draft
  variant, in fields that the backoffice shows and the public site and search never show.
- **FR-010**: The site MUST NOT create a category or tag from a proposal.

**Review**

- **FR-011**: The review MUST show a Details section with every Details value and proposal, and say when none was
  given.
- **FR-012**: The Details section MUST carry an Edit control that opens the Details step, with Phase 1's direct return
  to the review.

**Reading**

- **FR-013**: A read MUST suggest at most one category (Create Recipe only), any tags and at most one difficulty, each
  from the site's current lists only.
- **FR-014**: A read MUST fill a diet tag only when the photos state it. It MAY suggest a style tag from the recipe
  itself.
- **FR-015**: A category, tag or difficulty that the photos do not state by that name MUST carry a mark of "suggested
  by the reader" with a short reason.
- **FR-016**: A read MUST copy each nutrition value the photos print per serving in the site's units, unmarked, when
  it is a whole number. A printed value with a fraction, such as `0.5 g`, MUST be rounded to a whole number and
  marked as calculated with the printed value in its reason.
- **FR-017**: A read MUST convert nutrition printed for the whole recipe to per serving by dividing by the printed
  servings, convert kilojoules to calories, sodium in grams to milligrams and salt to sodium, round to a whole number,
  and mark each converted value as calculated with the printed value in its reason.
- **FR-018**: A read MUST NOT estimate nutrition. A value the photos do not print, or one that could only be computed
  from an estimated servings count, MUST stay empty.
- **FR-019**: A read MUST NOT write a proposed category or tag.
- **FR-020**: The site MUST send the reading service the names of its categories and tags, besides what Phase 1 sends,
  and still no member's account data.
- **FR-021**: The new marks, suggested and calculated, MUST behave as Phase 1's marks do: shown with their reason on
  the step and on the review, and cleared when the member edits the value.

**Submission**

- **FR-022**: Submitting MUST store the chosen category on the draft recipe, and the tags, difficulty and nutrition
  values on the draft variant, with the same validation, the same unpublished draft and the same pending entry on the
  member's account page as Phase 1.
- **FR-023**: A submission that names a category or tag the site no longer has MUST be refused with a message that
  returns the member to the Details step, keeping their other values.
- **FR-024**: Submission MUST keep its existing forgery protection and rate limit.

**Owner-editable text**

- **FR-025**: Every piece of text this phase shows MUST be an owner-editable UI string.

### Key Entities

- **Details**: the category (recipe only), the tags, the difficulty and the eight nutrition values of one submission,
  each optional.
- **Proposal**: free text, up to 100 characters, that names a category or tags the member wants the site to have. The
  draft recipe holds the category proposal and the draft variant the tag proposal. Only the backoffice shows it.
- **Mark**: Phase 1's note on a filled value gains two kinds: suggested by the reader, and calculated.
- **Read result**: Phase 1's result gains a suggested category, suggested tags, a suggested difficulty and the
  nutrition values, each with its mark where it has one.
- **Category and tag**: the existing content that the owner keeps in the backoffice. This phase reads them and does
  not change them.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A member picks a category, up to three tags and a difficulty, and continues, in under 30 seconds on a
  phone.
- **SC-002**: In every test submission, the draft holds exactly the category, tags, difficulty, nutrition values and
  proposals the member gave, and nothing the member did not give.
- **SC-003**: No public page and no search result shows a proposal.
- **SC-004**: On the reader test set, the suggested category matches the expected one in at least 9 of 10 recipes that
  have one.
- **SC-005**: On the reader test set, at least 95% of printed nutrition values come out exactly right per serving,
  calculated values included.
- **SC-006**: On the reader test set, no read fills a nutrition value the photos do not print, and no read fills a
  diet tag the photos do not state.
- **SC-007**: Import spending stays inside Phase 1's monthly ceiling, and the site's total running cost stays under
  about US$10 a month.

## Assumptions

- A1: The Details step's place in the photo flow follows Phase 1: after a read the member lands on the review, and the
  Details step opens from the review's Details section like the other section steps.
- A2: The nutrition limits are 10,000 calories, 1,000 grams for each gram value, and 50,000 milligrams of sodium.
- A3: Conversions use 4.184 kilojoules per calorie and 2.5 grams of salt per gram of sodium, and round half up.
- A4: The proposed tags are one free-text field, not a list, so a member may write several names in it.
- A5: The proposal fields join the recipe and variant content types as structure in uSync, with their ModelsBuilder
  models.
- A6: The public site's display of category, tags, difficulty and nutrition and the search filters do not change. They
  already show these values once the owner publishes.
- A7: Phase 1's marks are not stored with the draft. The owner sees no mark, as in Phase 1; Phase 3 brings import
  notes.
- A8: The reader comparison uses the Phase 1 evaluation tool and test set. The owner adds at least three recipes that
  print nutrition, at least one of them per whole recipe or in kilojoules, and confirms the expected Details values.
- A9: The cheapest reader that passes every Phase 1 and Phase 2 bar stays the default. If the current default fails a
  Phase 2 bar, the next cheapest reader that passes all of them becomes the default.
- A10: The tag lists sent to the reader hold names and their diet or style group only, a few hundred characters, and
  the spend reservation counts them.
- A11: Selecting the value a suggestion already holds counts as an edit and clears its mark.

### Recurring cost

- The ceiling does not change: each site that holds the key still stops starting reads at US$2 a month by default.
- A read sends the category and tag names and returns a few more values, which adds well under 1% to its cost.
- Proving SC-004 to SC-006 calls the real service once: about US$1 to US$2 for the comparison, as Phase 1's comparison
  cost about US$1, and up to about US$6 if the strongest reader drafts the new cases' expected answers.

### Out of scope for Phase 2

- Keeping source photos or import notes for the owner (Phase 3).
- Creating a category or tag from the public site.
- Changing how the public site shows these values, or the search filters.
- Recipe and variant images.
- Showing marks to the owner.
