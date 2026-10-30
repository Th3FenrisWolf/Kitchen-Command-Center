# QA walk results

- **Run**: 2026-10-15 10:41
- **Result**: 28 pass, 0 fail, 0 skip

| Id | Check | Result | Note |
|---|---|---|---|
| US1-1 | The page asks for camera access and shows a live camera view with a control to take a photo, and they can take several photos without leaving the page. | pass |  |
| US1-2 | The page says so, and the member can still add photos and screenshots from the device. | pass |  |
| SC-009 | A member with no camera, or who refuses camera access, completes an import with photos added from the device. | pass |  |
| EC-11 | An 11th photo is refused with a message. A file that is not a supported photo is refused with a message. | pass |  |
| EC-14 | Leaving or reloading loses it, and the page warns first. | pass |  |
| EC-12 | The page says that the read failed, keeps the photos, and lets the member try again or type the recipe in. | pass |  |
| US1-3 | The page shows progress, and the review screen then opens with the recipe and its first variant filled from the photos. | pass |  |
| US1-4 | Prep time holds an estimate, marked as an estimate on the review and on its step. | pass |  |
| US1-5 | That ingredient shows the reader's best reading, marked as unclear. | pass |  |
| EC-9 | The reader writes a short one and marks it as written by the reader. | pass |  |
| US1-6 | The site creates an unpublished recipe with its first variant, exactly as a typed recipe does today, and the member's account page lists it as pending review. | pass |  |
| US3-1 | That section's step opens with its current values. | pass |  |
| US3-2 | The review opens with their changes, without passing through the steps in between. | pass |  |
| US3-3 | Its mark clears on the step and on the review. | pass |  |
| US3-4 | They can open the import's photos to compare. | pass |  |
| US3-5 | The site first warns that the new read replaces their edits, and then fills the review from the new read. | pass |  |
| SC-007 | From the review, a member reaches any section's step in one action and returns to the review in one action, in both flows, for imported and typed recipes. | pass |  |
| US4-1 | It names that recipe with a short reason before the review opens. | pass |  |
| US4-2 | The flow continues as adding a variant to that recipe, with the variant's values filled from the photos, and the submission creates an unpublished variant under that recipe. | pass |  |
| US4-3 | The flow continues as adding a variant to the recipe they picked. | pass |  |
| US4-4 | The flow continues as Create Recipe with every value filled. | pass |  |
| US4-5 | It names that variant as a possible duplicate, and the member can still continue. | pass |  |
| US4-6 | It fills a new variant of that recipe and proposes no other recipe. | pass |  |
| EC-15 | The submission fails with the existing "recipe not found" message, and the member can go back and keep a new recipe instead. | pass |  |
| US2-1 | The photo choice says that it is unavailable until the first day of the next month, and typing the recipe in works as before. | pass |  |
| US2-4 | Reads are available again for the rest of the month. | pass |  |
| US2-3 | The site refuses it with its standard "try again later" message and spends nothing. | pass |  |
| EC-16 | The photo choice does not appear. | pass |  |
