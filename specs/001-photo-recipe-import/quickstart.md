# Quickstart: Validate Recipe Import from Photos, Phase 1

How to prove the feature works, end to end. The contracts and the data model hold the details; this guide only runs
them.

## Prerequisites

- Constitution gate 1: `FONTAWESOME_NPM_AUTH_TOKEN`, `KCC_E2E_MEMBER_USERNAME` and `KCC_E2E_MEMBER_PASSWORD` are set.
- Constitution gate 2: the .NET SDK from `global.json`, Node 24 with Yarn 1, and the Playwright browsers. This change
  touches `deploy/`, so it also needs `shellcheck` and a running Docker.
- For the checks that spend money (scenario 6 and the evaluation), the Anthropic key in KCC.Web's user-secrets
  (`Anthropic:ApiKey`, already present on the owner's Mac). Get the owner's approval before each run.
- With that key, the dev site reads for real whenever it starts without `RecipeImport__StandInReader=true`. It spends
  against its own ledger and its own US$2 ceiling, apart from the Pi's, so the two sites can spend about US$4 a month
  together (spec A2).

## Definition of done

Run these from the repository root, in this order:

```bash
dotnet build KitchenCommandCenter.slnx -c Release
```

```bash
yarn build:all
```

```bash
cd src/KCC.Web && yarn type-check && yarn lint && yarn format
```

```bash
node tests/scripts/run.mjs
```

```bash
shellcheck -s sh deploy/smoke-test.sh deploy/deploy.sh deploy/kcc deploy/kcc-backup/kcc-backup
```

```bash
docker buildx bake --load && deploy/smoke-test.sh
```

`run.mjs` opens its HTML report when it finishes, so an unattended run puts a no-op `open` first on `PATH`. The ledger
adds a write path, so the whole integration suite must pass (constitution III). `run.mjs` includes it, and
`dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj` runs it alone.

## Manual scenarios on the dev site

Start the site as `README.md` describes (`dotnet watch --non-interactive` in `src/KCC.Web`, on
`https://localhost:58671`), on a copy of your dev database. Sign in as an approved member. Scenarios 2, 4, 9 and 11
need at least one published recipe, from the dev seeder or your own. Every scenario but 6 runs on the stand-in reader
and spends nothing. Pass `RecipeImport__StandInReader=true`, and any other `RecipeImport__*` variable a scenario names,
on the start command, and never export them, so that no later command in the shell, `run.mjs` included, inherits them:

```bash
cd src/KCC.Web && RecipeImport__StandInReader=true dotnet watch --non-interactive
```

| # | Scenario | Steps | Expected |
|---|---|---|---|
| 1 | New recipe from a photo (US1) | Create Recipe → From photos → Add photos (any JPEG) → Read recipe | The review opens with every section filled. The stand-in's estimated prep time and its unclear ingredient show their marks |
| 2 | Step links (US3) | Open a recipe → Add a variant → From photos → add one photo → Read recipe. On the review, Edit the ingredients → open a photo from the photo strip → change the marked ingredient → Back to review. Then Change photos → Back to review. Then Change photos → add a photo → Read recipe | The photo opens larger from the ingredients step. The review shows the change, and that ingredient's mark is gone. The steps in between were never shown. Back to review leaves the photo step for the review with nothing changed. Read recipe first warns that reading again replaces the edits, and the new read opens the review with no match step |
| 3 | Submit | Repeat scenario 1, then Submit for Review | Recipe Submitted! The Account page lists the recipe as pending, and the backoffice's Waiting tab lists it |
| 4 | Match (US4) | Create Recipe → From photos → add two photos → Read recipe | The match step proposes the first published recipe in name order. "Add as a new variant" leads to a review titled "Adding a variant to …", and the submission creates a variant draft under that recipe |
| 5 | Ceiling (US2) | Restart with `RecipeImport__MonthlyCeilingUsd=0` added to the start command | Both wizards show the photo choice as unavailable until the first day of next month. Typing works |
| 6 | Real reader | Restart without the stand-in variable → Create Recipe → From photos → add photos of a real printed recipe, up to 3 → Read recipe → Submit for Review without correcting anything | The review appears within 60 seconds and matches the page. Record the time from Read recipe to the filled review (SC-002, A22) and the time from opening Create Recipe to "Recipe Submitted!" (SC-001, A24) under "Measured results" in [research.md](research.md#measured-results) |
| 7 | Camera | On a Mac with a camera: From photos → allow the camera → take two photos. Then deny the camera in the site settings and reload | Two tiles appear. After the denial, the camera message shows and Add photos still works |
| 8 | Both ramps (D6) | Switch the theme on the start, photo, reading, match and review steps, on an editing step with its photo strip, on the possible-duplicate notice and on the leave confirmation | Every surface holds in light and dark. Then run the `brand-steward` agent (D7) |
| 9 | Add Variant import (US1) | Open a recipe → Add a variant → From photos → add one photo → Read recipe | The review opens with the variant filled, with no match step, and the submission creates a variant draft under that recipe. The success sheet ends with "Back to" and that recipe's name |
| 10 | Leaving (US1) | Create Recipe → From photos → add a photo → choose the logo link in the header. On Add Variant, add a photo and choose the hero's Cancel. Then reload the page | Each link opens the leave confirmation. Stay keeps the photo. Leave follows the link without a second prompt. The reload raises the browser's own prompt |
| 11 | Possible duplicate (US4) | Open a recipe → Add a variant → From photos → add three photos → Read recipe. Then Create Recipe → From photos → three photos → Read recipe | Both reviews name a published variant and its recipe as a possible duplicate, and Submit still works |
| 12 | Slow read in Safari on a Mac | Restart with `RecipeImport__StandInDelaySeconds=70` added to the start command. In Safari on the Mac: Create Recipe → From photos → add one photo → Read recipe | The review opens after about 70 seconds. If Safari shows the failed-read message at about 60 seconds instead, tell the owner: research R18 holds the risk and the remedy |

Time SC-001 twice, from opening Create Recipe to "Recipe Submitted!" without correcting anything: on scenario 3 with the
stand-in reader, and on scenario 6's real read, under scenario 6's approval (A24). SC-001 asks for under 3 minutes.

After the pull request deploys, check the phones against production, where the real reader runs and every read spends
money: get the owner's approval first, as for scenario 6. On an iPhone in Safari and on an Android phone in Chrome,
photograph a printed recipe on Create Recipe and read it, as scenario 1 does; photograph a published recipe and read
it without submitting, as scenario 4 does; and run scenarios 7 and 10. Judge the capture, whether the photos are
readable enough for the review to match the page, and whether the screen stays on during each read, not the stand-in
reader's marks or proposal. On the iPhone a header link must open the leave confirmation; a reload there gives no
warning, because iOS Safari has no leave prompt (research R20).

## Reader evaluation

The first evaluation spends money: about US$18 at the default image edge of 2000 px. About US$6 goes to the draft run
in step 2, where the strongest model reads every case once, and about US$12 to the comparison of four models on 20
cases in step 3. The owner approves both before the draft run. The tool states its estimate and asks before each paid
run. A run at another edge is a separate run, and the owner approves its spend first. Before the first paid run, look
at the rate limits of the four models in the Anthropic Console, and check that the organization spend limit is set
("Turning it on at the Pi"): about US$20 a month leaves room for this first evaluation.

Put the original photos in the set. Before each read the tool encodes every photo as the browser does: turned upright,
its long edge scaled down to the edge the page would give the browser, and saved as a JPEG of at most 2.2 MB (research
R16).

A free check comes first, well before the comparison in step 3. It makes one `count_tokens` call for each model, with
the prompt and the output schema and no photo, and shows whether that endpoint takes the schema:

```bash
dotnet run --project tools/KCC.ReaderEval -- --check
```

1. Put the cases in `tools/KCC.ReaderEval/set/`, which git ignores:

   ```text
   tools/KCC.ReaderEval/set/
   ├── catalog.json                  # the site's published recipes (A32)
   ├── printed-pancakes/
   │   ├── photos/1.jpg
   │   └── expected.json
   └── handwritten-chili/
       ├── photos/1.jpg
       ├── photos/2.jpg
       └── expected.json
   ```

   `catalog.json` is an array of the site's published recipes as the site would send them, each with `name`,
   `description` and `variants` (the variant names) (A32). Each `expected.json` may carry `match`: the name of the
   catalog recipe the photos are a version of, or null for a recipe not on the site (FR-023). The comparison refuses
   to start, before any spend, unless the set has a catalog, a case whose `match` names a catalog recipe, a case whose
   `match` is null, a case with `"handwritten": true` and one with `false`, each kind with at least one expected
   ingredient, and a case of at most 3 photos (A29).

2. Draft the expected answers with the strongest model, then correct each `expected.draft.json` by hand and save it as
   `expected.json`:

   ```bash
   dotnet run --project tools/KCC.ReaderEval -- --draft
   ```

3. Compare the models, at least 24 hours after the last call that carried the output schema: the draft run, a
   `--check`, another evaluation run or a real read (A17, A18). The service caches a compiled schema for 24 hours
   after its last use, and a cached first call would hide the cold wait (research R16):

   ```bash
   dotnet run --project tools/KCC.ReaderEval -- --models claude-haiku-5-5,claude-haiku-4-5,claude-sonnet-5-5,claude-opus-5-5
   ```

   A run at another edge adds `--edge <px>`, and only after the owner has approved its spend.

4. Judge SC-002 for each model on its warm 90th-percentile time plus the largest cold penalty in the run (A18). A
   model's cold penalty is its first call's time minus the time of the same case read again. The tool reports each
   model's warm 90th percentile and cold penalty, and the run's largest cold penalty. A model meets SC-002 at 50
   seconds or less, which leaves 10 of SC-002's 60 seconds for uploading up to 3 photos of 2.2 MB and for the Pi's
   work (A22).
   The tool counts each model's image tokens only after its timed reads (A23).

5. Set the `RecipeImport:Model` default to the cheapest model that meets SC-002 to SC-005. Correct the image-token
   caps from the measured `count_tokens` figures, and confirm or correct Haiku 5.5's presumed image edge with them.
   Change the `MaxImageEdge` default only to an edge that a run measured. Record the table under "Measured results" in
   [research.md](research.md#measured-results).

## Turning it on at the Pi

The Pi has had `ANTHROPIC_API_KEY` in `/srv/kcc/repo/deploy/.env` since 2026-10-05, so the import turns on with the
deploy. This change also gives the client the timeout that the icon calls have lacked since then.

**The spend limit.** Each site's ledger stops its own reads at its own ceiling. Nothing in the code caps the account as
a whole, the evaluation tool included, so one organization spend limit of about US$20 a month in the Anthropic Console
backs the ledgers. The owner sets it, and raises it before each later evaluation. It sits above the two sites'
ceilings, about US$4 a month together, and the first evaluation, about US$18, fits under it with about US$2 to spare
for that month's reads ("Reader evaluation"). A read that the limit stops gets an error status from the service, so
the ledger gives its reservation back and the wizard shows a failed read.

**Before the deploy**, in the backoffice's Translation section, check whether you edited any `AddVariant.*` value. The
dictionary import is create-only, so the new `RecipeWizard.*` items take the values in the files. After the deploy,
enter each edited value again under its new key ([wizard-ui.md](contracts/wizard-ui.md#retired-addvariant-files) lists
where each key went).

To set a ceiling other than US$2, add `KCC_IMPORT_CEILING_USD=<amount>` to the same `.env`, then run:

```bash
sudo -u kcc -H kcc up -d --wait
```

## Optional cleanup

The 34 retired `AddVariant.*` dictionary items stay in an existing database, unused. Delete them in the backoffice's
Translation section if you want a tidy tree. A fresh database never gets them.
