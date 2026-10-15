# Feature Specification: Recipe Import from Photos, Phase 1

**Feature Branch**: `001-photo-recipe-import`

**Created**: 2026-10-05

**Status**: Draft

**Input**: User description: "lets create a method of creating a recipe. id like to be able to take a picture or screenshot of a recipe and have my app fill out the recipe and first variant itself. if it could determine if it should go into an existing recipe as a new variant, that would be great. i already have an anthropic api key we can use to be able to do the parsing. the website should request camera access so it wont require a photo upload, but a photo upload could be an option. id like to be able to support multiple picutres and then once the user submits they're brought to a review screen that they can review and/or jump to a step to edit or update. it may be worth storing the uploaded pictures for verification if we arent confident in the parsing, but ill let you decide that."

## Clarifications

### Session 2026-10-05

- Q: Who can start a photo import? → A: Every approved, signed-in member, from Create Recipe and from Add Variant. Their drafts still wait for the owner.
- Q: Which fields should the import fill? → A: A Details step for category, tags, difficulty and nutrition is wanted, and it comes in Phase 2. Phase 1 fills the fields the wizards collect today.
- Q: What happens when the photos do not state prep time, cook time or servings? → A: The import estimates the value and marks it as an estimate. Nutrition is never estimated (Phase 2).
- Q: Should the import credit the source book, author or website? → A: No. The import ignores the source.
- Q: How should the work split? → A: Three phases, each with its own numbered spec. Phase 1: the import into today's wizard fields, with matching, step links on the review, marks and the spending ceiling. Phase 2: the Details step. Phase 3: source photos and import notes for the owner's review. This spec covers Phase 1.
- Q: How much may imports spend each month? → A: US$2 a month, changeable in configuration.
- Q: What should the reader optimize for within the ceiling? → A: The cheapest reader that meets the accuracy bars in SC-003 and SC-005, chosen by measurement.
- Q: When are the readers measured? → A: During implementation. An early task builds an evaluation tool on the real reader code, the owner supplies the photos then, and the cheapest reader that passes becomes the default.
- Q: Should the site keep the photos? (The owner delegated this decision.) → A: Yes, from Phase 3: an imported draft keeps its source photos until the owner publishes or deletes it. Phase 1 keeps none. Photos kept forever would slowly use up the nightly backup's size budget.
- Q: Do the Phase 1 defaults for entry, capture, privacy, reading, matching, filling, marks, review, submission, cost and UI strings match the intent? → A: Yes, as written in this spec.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Turn photos into a new recipe draft (Priority: P1)

A signed-in member has a recipe in front of them: a cookbook page, a handwritten card, or a recipe on a screen. On Create Recipe they choose to start from photos. The page asks for the camera, and they take one photo per page or card. They can also add photos or screenshots from the device. They ask the site to read the photos, and after a short wait they land on the review screen. The recipe name and description, the first variant's name and description, prep and cook time, servings, every ingredient and every step are filled in. Values that the photos did not state, or that the reader could not read clearly, carry a mark. They submit, and the owner gets the same unpublished draft that a typed recipe produces today.

**Why this priority**: This is the feature. Typing a recipe by hand is the slowest part of adding one, and this story alone removes it.

**Independent Test**: With no published recipe that resembles the test recipe, a member imports a photographed printed recipe and submits it. The draft that reaches the owner holds the recipe as printed.

**Acceptance Scenarios**:

1. **Given** a signed-in approved member on Create Recipe while photo import is available, **When** they choose to start from photos, **Then** the page asks for camera access and shows a live camera view with a control to take a photo, and they can take several photos without leaving the page.
2. **Given** camera access is refused, or the device has no camera, **When** the member is on the photo step, **Then** the page says so, and the member can still add photos and screenshots from the device.
3. **Given** a member has 1 to 10 photos in the import, **When** they ask the site to read them, **Then** the page shows progress, and the review screen then opens with the recipe and its first variant filled from the photos.
4. **Given** the photos print no prep time, **When** the review opens, **Then** prep time holds an estimate, marked as an estimate on the review and on its step.
5. **Given** a smudged quantity in the photos, **When** the review opens, **Then** that ingredient shows the reader's best reading, marked as unclear.
6. **Given** the member submits from the review, **When** the submission succeeds, **Then** the site creates an unpublished recipe with its first variant, exactly as a typed recipe does today, and the member's account page lists it as pending review.
7. **Given** photos that show no recipe, **When** the site reads them, **Then** it says that it found no recipe, fills nothing, and lets the member retake photos, add photos, or type the recipe in.

---

### User Story 2 - Keep import spending under the monthly ceiling (Priority: P1)

The owner pays for every read. The site totals what reads cost in each calendar month and stops starting new reads when the month's total reaches the ceiling, US$2 by default. Members then see that photo import is unavailable until the next month, and typing a recipe in still works. The owner can change the ceiling in configuration without a code change.

**Why this priority**: The site must stay under about US$10 a month, all in, and reads are its only usage-based cost. Story 1 cannot ship without this guard.

**Independent Test**: With the ceiling set above zero and at or below the cost of one read, a member completes one import. The next attempt shows the import as unavailable until the next month, and typing a recipe in still works.

**Acceptance Scenarios**:

1. **Given** the month's import spend has reached the ceiling, **When** a member opens Create Recipe or Add Variant, **Then** the photo choice says that it is unavailable until the first day of the next month, and typing the recipe in works as before.
2. **Given** the site restarts or is redeployed in the middle of a month, **When** a member starts a read, **Then** the month's spend so far still counts against the ceiling.
3. **Given** a member has started as many reads in the past hour as the hourly limit allows, **When** they start one more, **Then** the site refuses it with its standard "try again later" message and spends nothing.
4. **Given** the ceiling was reached and the owner raises it in configuration, **When** the site runs with the new value, **Then** reads are available again for the rest of the month.

---

### User Story 3 - Jump from the review to any step and back (Priority: P2)

On the review screen, every section of the recipe has an Edit control: the recipe, the variant, the ingredients and the steps. The control opens that section's step. The step offers a way straight back to the review, and the member's changes are kept. This works the same for imported and typed recipes, in Create Recipe and in Add Variant. While the member checks an imported recipe, the photos stay available for comparison.

**Why this priority**: The review screen is where an imported recipe gets checked. Today it is read-only and steps back one step at a time, so fixing one ingredient means walking back through the wizard.

**Independent Test**: A member types a recipe, reaches the review, edits the ingredients through their Edit control, and returns to the review in one action with the change shown. No import is needed.

**Acceptance Scenarios**:

1. **Given** a member on the review of either flow, **When** they choose Edit on a section, **Then** that section's step opens with its current values.
2. **Given** a member opened a step from the review, **When** they choose to return to the review, **Then** the review opens with their changes, without passing through the steps in between.
3. **Given** an imported recipe with marked values, **When** the member edits a marked value, **Then** its mark clears on the step and on the review.
4. **Given** an imported recipe, **When** the member is on the review or on any step, **Then** they can open the import's photos to compare.
5. **Given** a member on the review of an imported recipe who has edited values, **When** they return to the photos, change them and read again, **Then** the site first warns that the new read replaces their edits, and then fills the review from the new read.

---

### User Story 4 - Add the photos as a variant of an existing recipe (Priority: P3)

Sometimes the photos show another way to make a dish the site already has, such as a different chili or a different pancake. When an import starts from Create Recipe, the reader judges whether the photos show a version of a published recipe. If they do, the site names that recipe and says why, and the member chooses: add the photos as a new variant of it, add them to a different published recipe, or keep a new recipe. When the photos look almost exactly like an existing variant, the site warns that they may be a duplicate. An import that starts from Add Variant fills a new variant of the recipe the member already chose.

**Why this priority**: It keeps each dish in one place on the site. A member can already get the same result by starting from Add Variant, so the import is useful without it.

**Independent Test**: With a published taco recipe on the site, a member imports a photographed taco recipe from Create Recipe. The site proposes the taco recipe, the member accepts, and the submission creates a new unpublished variant under it.

**Acceptance Scenarios**:

1. **Given** published recipes on the site and photos of a version of one of them, **When** the site reads the photos from Create Recipe, **Then** it names that recipe with a short reason before the review opens.
2. **Given** a proposed recipe, **When** the member chooses to add the photos as a new variant of it, **Then** the flow continues as adding a variant to that recipe, with the variant's values filled from the photos, and the submission creates an unpublished variant under that recipe.
3. **Given** a proposed recipe, **When** the member picks another published recipe instead, **Then** the flow continues as adding a variant to the recipe they picked.
4. **Given** a proposed recipe, **When** the member keeps a new recipe, **Then** the flow continues as Create Recipe with every value filled.
5. **Given** photos that almost exactly copy an existing variant, **When** the site reads them, **Then** it names that variant as a possible duplicate, and the member can still continue.
6. **Given** an import started from Add Variant for a chosen recipe, **When** the site reads the photos, **Then** it fills a new variant of that recipe and proposes no other recipe.

---

### Edge Cases

- **One recipe across several photos**: ingredients on one page and the method on the next, or photos taken out of order. The reader combines them into one recipe in the order the recipe itself gives.
- **More than one recipe in the photos**: the reader fills the review from the recipe that the photos mainly show and names the others it saw, so the member can import each one separately.
- **Hard photos**: blur, glare, shadow, a curled page or a dark kitchen. The reader fills what it can and marks what it cannot read clearly. When it can read nothing useful, it says so and suggests a clearer photo.
- **Handwriting**: handwritten cards are in scope. Uncertain words and numbers are marked unclear.
- **Screenshots**: ads, comments and other clutter around a recipe on a web page are ignored.
- **Ingredient groups**: headings such as "For the sauce" and "For the dough" collapse into one ingredient list in printed order, because a recipe variant holds one flat list.
- **Amounts**: fractions and mixed numbers become numbers, so 1½ becomes 1.5. A range such as "2 to 3 cloves" keeps its lower number and is marked unclear, with the printed range in the mark. "Salt to taste" and items with no amount become to-taste items. Units stay as printed.
- **Another language**: the recipe comes back in English, marked as translated.
- **Missing text**: when the photos give no description or no variant name, the reader writes a short one and marks it as written by the reader.
- **Values that break a wizard rule**: a filled value that the wizard would reject, such as a name longer than the existing limit, is shortened or left blank and marked unclear, so the existing submission rules stay as they are.
- **Too many or wrong files**: an 11th photo is refused with a message. A file that is not a supported photo is refused with a message.
- **A read fails**: the reading service is down, too slow or refuses the request. The page says that the read failed, keeps the photos, and lets the member try again or type the recipe in.
- **The ceiling is reached during a read**: that read finishes, and the next one is refused.
- **The member leaves**: an import lives only in the open page, as today's wizard does. Leaving or reloading loses it, and the page warns first.
- **The proposed recipe disappears**: the owner unpublishes it before the member submits. The submission fails with the existing "recipe not found" message, and the member can go back and keep a new recipe instead.
- **No reading configured**: where the owner's reading account is not configured, as in CI and the automated tests, the photo choice does not appear. A site that holds the account's key, the owner's development site included, reads for real against its own ceiling.

## Requirements *(mandatory)*

### Functional Requirements

**Starting an import**

- **FR-001**: Create Recipe and Add Variant MUST each let a signed-in, approved member choose between starting from photos and typing the recipe in.
- **FR-002**: The site MUST show the photo choice only when reading is configured. When the month's import spend has reached the ceiling, the choice MUST show as unavailable, say until when, and leave typing the recipe in available.
- **FR-003**: Only a signed-in, approved member MUST be able to start a read. The site MUST refuse any other request before anything is spent.

**Capturing photos**

- **FR-004**: Choosing photos MUST request the device camera and show a live view from which the member takes photos, one after another, without leaving the page.
- **FR-005**: The member MUST be able to add photos and screenshots from the device's files or photo library, with or without the camera.
- **FR-006**: When the device has no camera or camera access is refused, the page MUST say so and keep adding photos from the device available.
- **FR-007**: The member MUST see every photo in the import, view any of them larger, and remove any of them before a read.
- **FR-008**: An import MUST hold 1 to 10 photos, and the page MUST refuse more with a message.
- **FR-009**: The site MUST accept the photo and screenshot formats that current iPhone and Android devices produce, and MUST refuse other files with a message.
- **FR-010**: Before the first read, the photo step MUST tell the member that the photos go to an outside AI service to be read and that the site keeps no copy of them.

**Reading**

- **FR-011**: A read MUST treat all photos in the import as one recipe and MUST show progress until a result or a failure arrives.
- **FR-012**: A read MUST fill the recipe name and description, the variant name and description, prep time and cook time in minutes, servings, every ingredient with its quantity, unit, name and to-taste flag in printed order, and every method step in order.
- **FR-013**: Values that the photos state MUST come from the photos. Units MUST stay as printed, fractions MUST become numbers, and ingredients without an amount or marked "to taste" MUST become to-taste items.
- **FR-014**: When the photos do not state prep time, cook time or servings, the read MUST estimate the value and mark it as an estimate.
- **FR-015**: When the photos give no description or no variant name, the read MUST write a short one and mark it as written by the reader.
- **FR-016**: A value that the reader cannot read with confidence MUST carry its best reading and a mark of unclear, with a short reason.
- **FR-017**: A recipe in another language MUST come back in English, marked as translated.
- **FR-018**: When the photos hold no recipe, the site MUST say so, fill nothing, and offer to retake photos, add photos, or type the recipe in.
- **FR-019**: When the photos hold more than one recipe, the read MUST fill from the recipe that the photos mainly show and MUST name the others it saw.
- **FR-020**: When a read fails, the page MUST say so, keep the photos, and offer to try again or type the recipe in.
- **FR-021**: The site MUST send the reading service only the photos and what it needs to read and match them: the names and descriptions of published recipes and of their variants. It MUST NOT send any member's account data.
- **FR-022**: The site MUST keep no copy of an import's photos once a read ends, whether it succeeds or fails. Only the member's open page holds them.

**Matching an existing recipe**

- **FR-023**: A read started from Create Recipe MUST judge whether the photos show a version of a published recipe and, if they do, propose that one recipe with a short reason.
- **FR-024**: Given a proposal, the member MUST choose to add the import as a new variant of the proposed recipe, add it as a new variant of another published recipe they pick, or keep a new recipe. The site MUST NOT choose for them.
- **FR-025**: Adding the import as a variant MUST continue as Add Variant for that recipe, with the variant's values filled from the read.
- **FR-026**: When the photos almost exactly copy an existing variant, the site MUST name that variant as a possible duplicate and still let the member continue.
- **FR-027**: A read started from Add Variant MUST fill a new variant of the chosen recipe and MUST NOT propose another recipe.

**Review and editing**

- **FR-028**: After a read, and once the member settles any proposed recipe, the flow MUST open its review screen with every filled value.
- **FR-029**: The review MUST show each section (the recipe, the variant, the ingredients and the steps) with an Edit control that opens that section's step.
- **FR-030**: A step opened from the review MUST offer a direct return to the review that keeps the member's changes.
- **FR-031**: FR-029 and FR-030 MUST hold in both flows, for imported and typed recipes.
- **FR-032**: Every marked value (estimate, unclear, written by the reader, or translated) MUST show its mark and reason on its step and on the review. Editing a marked value MUST clear its mark.
- **FR-033**: While the member reviews or edits an imported recipe, the import's photos MUST stay available to view.
- **FR-034**: From the review, the member MUST be able to return to the photos, change them and read again. Before a new read replaces values that the member has edited, the site MUST warn them.
- **FR-035**: Before a member leaves a flow that holds photos or read values, the page MUST warn that unsubmitted work will be lost.
- **FR-036**: Submitting an imported recipe or variant MUST behave exactly as submitting a typed one does today: the same validation, the same unpublished draft for the owner, and the same pending entry on the member's account page.

**Spending and limits**

- **FR-037**: The site MUST total what reads actually cost in each calendar month, failed reads included, and MUST keep that total across restarts and deploys.
- **FR-038**: A read MUST start only while the month's total is below the ceiling. The ceiling MUST default to US$2 a month, and the owner MUST be able to change it in configuration without a code change.
- **FR-039**: Starting a read MUST carry a per-client hourly limit that the owner can change in configuration, as the existing limits are changed. Over the limit, the site MUST refuse with its standard "try again later" message and spend nothing.
- **FR-040**: Starting a read MUST carry the same forgery protection as the site's other member actions.

**Owner-editable text**

- **FR-041**: Every piece of text that this feature shows MUST be an owner-editable UI string, and so MUST the fixed English text that the Create Recipe flow shows today.

### Key Entities

- **Import**: one member's attempt to turn photos into a recipe or a variant. It holds 1 to 10 photos, the flow it started from (Create Recipe, or Add Variant with its chosen recipe) and the latest read result. It lives only in the member's open page and ends at submission or when the page closes.
- **Read result**: the values a read fills (recipe name and description, variant name and description, prep and cook time, servings, ingredients and steps), the mark and reason on each marked value, an optional proposed recipe with its reason, an optional possible-duplicate variant, and the names of any other recipes seen in the photos.
- **Mark**: a note on one filled value, of one kind: estimate, unclear, written by the reader, or translated. It carries a short reason, and editing the value clears it.
- **Monthly import spend**: a calendar month, the total cost of the reads in it, and the ceiling. It survives restarts and deploys.
- **Recipe draft and variant draft**: the existing unpublished recipe and variant that a submission creates. This feature does not change them.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A member who finds nothing to correct turns a typical printed recipe of up to 3 photos into a submitted draft in under 3 minutes.
- **SC-002**: For reads of up to 3 photos, the filled review appears within 60 seconds in at least 9 of 10 reads.
- **SC-003**: On a test set of 15 to 20 photographed recipes that includes the owner's handwritten cards, at least 95% of printed and 85% of handwritten ingredient lines come out right in quantity, unit and name, and every method step appears, in order, in at least 9 of 10 recipes.
- **SC-004**: On the same test set, every filled value that the photos do not state carries a mark. No unmarked invented value reaches the review.
- **SC-005**: On test imports of recipes that are already on the site, the reader proposes the right recipe in at least 9 of 10 cases. On test imports of recipes that are not on the site, it proposes a recipe in at most 1 of 10 cases.
- **SC-006**: Import spending in any calendar month stays at or under the ceiling, apart from reads already running when the ceiling was reached, and the site's total running cost stays under about US$10 a month.
- **SC-007**: From the review, a member reaches any section's step in one action and returns to the review in one action, in both flows, for imported and typed recipes.
- **SC-008**: After any read, successful or failed, the site holds no copy of its photos.
- **SC-009**: A member with no camera, or who refuses camera access, completes an import with photos added from the device.

## Assumptions

- A1: Every approved, signed-in member can import. There is no owner-only mode, because the public site does not tell the owner apart from other members. The owner imports through their own member account.
- A2: The site already holds settings for the owner's Anthropic API account, which it uses to choose recipe icons. Reading uses the same account. Where no key is configured, as in CI and the automated tests, the photo choice does not appear. Each site that holds the key, the owner's development site included, reads for real and keeps its own monthly import spend against its own ceiling.
- A3: No automated test calls the paid reading service. Tests use a stand-in for it.
- A4: The plan fixes how the reader is picked, and an implementation task picks it: the cheapest reader that meets SC-002 to SC-005 on the test set. SC-003 and SC-005 are the accuracy bars the owner chose; SC-002 and SC-004 bind every reader anyway. The owner supplies some handwritten cards for the test set, and the task records the measured cost of a typical read.
- A5: A typical import has 1 to 3 photos.
- A6: A calendar month runs in UTC.
- A7: The hourly read limit defaults to 10 reads per client and sits beside the existing rate limits.
- A8: An import lives only in the member's open page, as today's wizard state does. Nothing is saved until submission.
- A9: Matching considers published recipes only, because a variant can be added only to a published recipe.
- A10: Units are not converted, and times are whole minutes.
- A11: The owner's review and publishing in the backoffice do not change in Phase 1.
- A12: The import does not credit the source book, author or website.
- A13: The recipe icon still comes from the site's existing icon choice at submission.
- A14: What a cancelled or dropped read costs: a read runs under its 85-second deadline only, not under the request's aborted token. After a Cancel or a dropped connection the read runs to its end and settles its real usage, so a cancelled read is billed in full. The reading step holds a screen wake lock. Alternative: stop the read upstream and keep the worst-case reservation, accepting that about seven cancelled reads end the month's imports.
- A15: How the page warns before a member leaves unsubmitted work (FR-035): while the wizard holds unsaved work, one page-wide guard asks for confirmation in the page before it follows any same-origin link (header, footer and the hero's Cancel alike), and the browser's `beforeunload` prompt stays for reload and close where the browser has one, disarmed once the member confirms. On iOS Safari a reload, a closed tab, the back gesture, an app switch or one of the header's forms (search, sign out) gives no warning, because the platform has no leave prompt. Alternative: the browser's prompt where one exists, plus a confirmation on the wizard's own exits only.
- A16: Where a submission ends: the wizard owns the success sheet, with a default "Back to Recipes" closing link; a page may pass the closing link (label and href); Add Variant passes "Back to <recipe>" and the recipe's URL; a variant matched on Create Recipe ends with "Back to Recipes". Alternative: add `url` to `match` and `recipes` and a `RecipeWizard.BackTo` key, so that a matched variant links to its recipe.
- A17: When the cold read is timed for SC-002: the evaluation tool times each model's first call apart from the rest, and the comparison run starts at least 24 hours after the draft run's last call, because a compiled output schema stays cached for 24 hours after its last use and a cached first call would hide the wait. Alternative: take the cold time from the draft run's first call alone, and run the comparison straight after it.
- A18: How the cold penalty applies across models (SC-002): the largest cold penalty measured in the comparison run applies to every model, and nothing calls the API with the output schema (no --check, draft run, other evaluation run or real read) in the 24 hours before the run. Alternative: time each model's cold call in a run of its own, at least 24 hours after any call with the schema.
- A19: Whether running reads count toward the ceiling (FR-002): a running read's reservation counts, so near the ceiling the import can show as resting until the next month for up to the read deadline before those reads settle, and the page that got the 409 keeps Read recipe off until it reloads. Alternative: keep running reads' reservations under their own key and answer with a distinct code and a 'try again in a minute' message, leaving Read recipe on.
- A20: What a Create Recipe read does after the member chose a match (FR-023): every read on Create Recipe posts flow=recipe with the catalog; applying it returns the wizard to recipe mode, drops the set-aside values and the earlier choice, and opens the match step again when the new read proposes a recipe. Alternative: post flow=variant with the chosen recipe's key, keep the choice, and keep the first read's match and recipes for Change.
- A21: How the member returns to the review from the photo step (FR-030): changePhotos() sets returnTo to the review while read values exist, so the photo step shows back-to-review until a new read applies, and a read started from the review that is cancelled, fails, finds no recipe or gets a 409 lands on the photo step with that return. Alternative: FR-030 covers the section steps only, and the photo step offers no return.
- A22: The allowance in SC-002 for everything but the reading service: the evaluation judges a model's time against 50 seconds, which leaves 10 seconds for uploading up to 3 photos of 2.2 MB and for the Pi's work, and scenario 6 records one end-to-end time. Alternative: judge the reading service's time alone against 60 seconds.
- A23: When the comparison run counts image tokens: each model's count_tokens calls come after its timed reads, so they cannot warm the schema cache before the cold call. Alternative: send the image-token count_tokens calls without the output schema.
- A24: How SC-001 is timed: it is timed on scenario 3 with the stand-in reader and again on scenario 6's real read, under scenario 6's approval. Alternative: keep the stand-in timing alone, as a measure of the screens only.
- A25: Which recipe "first published recipe" means for the stand-in and in ties: the catalog's first recipe in name order, and the possible-duplicate check breaks an equal overlap by catalog order, then by variant order. Alternative: content-tree order, stated the same way everywhere.
- A26: What the photo step offers while the import rests after a 409: it hides the camera and Add photos, and keeps the strip with remove, the unavailable notice, Back and Type it in. Alternative: keep capturing, as after any failed read.
- A27: Where focus goes when the member cancels the read-again warning: back to the button that asked, Read again on the review or Read recipe on the photo step, or to the step's heading when that button is disabled. Alternative: the step's heading always.
- A28: What Back does on a step opened from the review: it moves to the previous step and keeps the return, so Next there also goes to the review. Alternative: Back drops the return, and Next walks the steps again.
- A29: When the reader comparison refuses to run: before any spend, unless every Success Criterion it judges can be judged (a catalog, at least one on-site and one off-site match case, a handwritten case, a printed case, a case of at most 3 photos). Alternative: run and report the criterion as not judged.
- A30: How the duplicate check reads a dash or a slash in a name: as a space, so "All-purpose" equals "all purpose"; other punctuation is removed. Alternative: remove every punctuation mark.
- A31: The catalog's characters per token in the spend reservation: keep 3.5 now and measure it with the catalog in T079. Alternative: lower it now without a measurement.
- A32: What `catalog.json` in the evaluation set holds: the site's published recipes as the site would send them. Alternative: only the recipes the match cases name.
- A33: How the match step sets the reader's reason: in body text, soft ink, as a mark's reason is set. Alternative: a hand note.
- A34: What the review says beside Change after the member kept a new recipe: "Keeping it as a new recipe" (`KeepingNewRecipe`). Alternative: ask the match question again.
- A35: What Back does on a match step reached from a typing path: it returns to the instructions step and leaves the proposal unsettled. Alternative: no Back, so the member answers before going anywhere else.
- A36: Whether the photos stay in view while a read runs: the view-only strip stays above the reading state, because the kit keeps what is already shown during a load. Alternative: the reading state replaces the photos.
- A37: How an eyeballed ingredient shows its quantity and unit: the two fields are hidden and the row says "to taste" in its place, because the kit gives no field a disabled look. Alternative: add a disabled field look to the kit first.
- A38: What counts as a right ingredient name in SC-003: the name as written, or a spelling the expected answer accepts for it, such as "mozzarella cheese" for "mozz cheese". Alternative: an exact copy only.
- A39: What a read longer than the limits keeps: the first 100 ingredients and the first 60 steps, with no notice for the rest, because a recipe that long is unlikely. Alternative: mark the list as shortened.

Reviewed: 2026-10-12 (A1 to A25); 2026-10-15 (A26 to A39)

### Recurring cost

- The feature adds one usage-based cost: the owner's Anthropic API account bills every read. A typical read of up to 3 photos costs from well under US$0.01 to about US$0.15, depending on the reader the plan picks. A read of 10 photos on Create Recipe with 500 published recipes costs up to about US$0.65 with the strongest reader.
- Each site that holds the key stops starting reads at its own monthly ceiling, US$2 by default, and two sites hold it: the Pi and the owner's development site. The feature therefore adds at most about US$2 a month for each, about US$4 a month for the two, and the site stays under its US$10 target.
- Measuring SC-002 to SC-005 calls the real service. The first evaluation costs about US$18: about US$6 to draft the expected answers with the strongest reader, and about US$12 to compare the readers. Each later comparison, after a reader change, costs about US$12.

### Out of scope for Phase 1

- Category, tags, difficulty and nutrition (Phase 2).
- Keeping source photos or import notes for the owner (Phase 3).
- Recipe and variant images. The import never sets one, and the owner picks images in the backoffice, as today.
- Importing from a web address, a PDF or a video.
- Saving an unfinished import to finish later.
- A spending report inside the site. The Anthropic Console shows the account's spend.

### Decided for later phases

The owner settled these during this specification. Each later phase is specified only after the previous phase passes its definition of done, and its spec starts from these answers.

- **Phase 2, the Details step**: both flows gain a Details step for category, tags, difficulty and nutrition, for typed and imported recipes. A read suggests a category, tags and a difficulty from the site's existing lists, and copies nutrition only when the photos print it. Nutrition is never estimated.
- **Phase 3, the owner's review of imports**: an imported draft keeps its source photos, and notes of the values that the reader marked, until the owner publishes or deletes it. The owner sees them where submissions are reviewed. The photos never appear on the public site, and they leave with their draft, so the nightly backup stays inside its size budget.
