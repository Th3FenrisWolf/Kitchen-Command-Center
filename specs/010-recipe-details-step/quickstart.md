# Quickstart: Validating Phase 2 - The Details Step

The spec's acceptance scenarios hold the behaviour. This file adds the steps they do not give: which stand-in read
reaches which state, the size measurement for the pull request, and the one paid reader comparison.

## Free checks: no key, no spend

Every check here runs with the stand-in reader or with reading off. No test host sets an Anthropic key (Phase 1,
research R15), and no step of this section spends money.

1. Build and run the suites: `dotnet build KitchenCommandCenter.slnx -c Release`, `pnpm build:all`, then
   `node tests/scripts/run.mjs` (D1, D2, D4). After any change to `RecipeSubmissions` or `SubmissionValues`, run the
   whole integration suite: `dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj`.
2. **Typed recipe, reading off** (Story 1's independent test). Run the site with no key, sign in as a member, open
   Create Recipe, type a recipe, and on Details pick Dinner, Spicy, Gluten-Free and Medium, and enter 450 calories.
   The review's Details section lists them. Submit, then open the draft in the backoffice: `category`, `tags`,
   `difficulty` and `calories` hold those values and no other nutrition value is stored. Publish it and check the
   recipe and variant pages.
3. **Add Variant**: the Details step offers no category and no proposed category.
4. **Stand-in reads** (Development, with `RecipeImport:StandInReader` on, as Phase 1 set it up). The photo count picks the
   read ([research R8](research.md#r8-the-stand-in-readers-details)):
   - 1 photo: the review shows Breakfast and the Easy tag marked suggested, Vegan unmarked, Easy difficulty
     unmarked, the panel's whole numbers unmarked and fiber 2 marked calculated, "Printed as 1.5 g".
   - 4 photos: every nutrition value is divided by 8 and marked calculated; calories come from kJ and sodium from
     salt.
   - 5 photos: every nutrition value is empty and unmarked.
   - 2 photos, accept the proposed recipe: no category on the review; choose Change match and keep a new recipe: the
     suggested category is back.
5. **Proposals stay private** (Story 3, SC-003): submit "Mexican" as a proposed tag, publish the draft, and check that
   the variant page, the recipe page and `/api/recipes/search` never contain "Mexican". The integration test does the
   same.
6. **Lists changed** (FR-023): load Create Recipe, then in the backoffice unpublish the Spicy tag, then submit with
   Spicy chosen. The wizard returns to Details with the `details-changed` well, Spicy gone from the lists and every
   other value kept.
7. **SC-001 on a phone**: in the Browser pane at the mobile preset, time from the Details step's heading to the review
   after picking a category, three tags and a difficulty. Under 30 seconds.
8. **Both ramps** (D6, D7): the Details step and the review in light and dark, then the `brand-steward` agent.

## Script size for the pull request

Measure the shared script on `origin/main` (in a separate worktree) and on this branch, on the same machine, with
gzip at its smallest setting as the guard does, and put both numbers and the delta in the pull request description.
In each checkout:

```bash
pnpm build:all
f=$(ls src/KCC.Web/wwwroot/assets | grep -i '^main-.*\.js$'); gzip -9 -c "src/KCC.Web/wwwroot/assets/$f" | wc -c
```

The target is under 2,000 bytes added. The E2E perf guard allows 2,345 bytes over the 117,266-byte baseline on every
page. If the change does not fit, the close-out stops and reports it; this branch changes no budget, no baseline and no
bundle split (plan, D2).

## The paid reader comparison: once

This is the only step that spends money. It proves SC-004 to SC-006 and settles the default reader by spec A9 and the
owner's answer to checkpoint Q1.

**Before it, the owner**:

1. adds at least 3 recipes that print nutrition to `tools/KCC.ReaderEval/set/`, at least one of them per whole recipe
   or in kilojoules (spec A8);
2. confirms the `details` block of every case's `expected.json`: the session drafted the 10 Phase 1 cases' blocks with
   `"draft": true`, and the owner writes the blocks of the new cases, corrects what is wrong and deletes each
   `"draft": true`. The comparison refuses to start while any block carries it:

   ```json
   "details": {
     "category": "Dinner",
     "tags": [{ "name": "Vegan", "stated": true }, { "name": "Spicy", "stated": false }],
     "difficulty": null,
     "nutritionPrinted": "perServing",
     "nutrition": { "calories": 450, "proteinG": 12, "carbsG": 60, "fatG": 14,
                    "saturatedFatG": 2, "fiberG": 8, "sugarG": 9, "sodiumMg": 380 }
   }
   ```

   `nutritionPrinted` is `perServing`, `converted` (per whole recipe or in kJ) or null. A nutrient the photos do not
   print is null. `stated` is true only when the photos name the tag. The values are what the site should store,
   after the conversions in research R3;
3. checks that the Anthropic organization spend limit leaves room for about US$2 that month.

**Then**:

1. `dotnet run --project tools/KCC.ReaderEval -- --check`. Free. Proves the service takes the grown schema.
2. Wait at least 24 hours after the last call that carried the schema, as Phase 1's step 3 requires.
3. `dotnet run --project tools/KCC.ReaderEval -- --models claude-haiku-5-5,claude-haiku-4-5,claude-sonnet-5-5,claude-opus-5-5`.
   The tool refuses to start, before any spend, unless the set meets the gate in research R7. It states its estimate
   and asks; the owner approves the spend there.
4. Record the table, Phase 1's columns plus SC-004, SC-005 and SC-006, under "Measured results" in research.md, and
   set the default `Model` in `src/KCC.Web/Features/RecipeImport/RecipeImportOptions.cs` by A9 and the answer to Q1.

A second paid run happens only if the first one fails part-way or the owner asks for one.
