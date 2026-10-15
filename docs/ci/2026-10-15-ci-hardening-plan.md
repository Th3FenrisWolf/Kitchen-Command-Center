# CI hardening plan

Date: 2026-10-15. Source: every failed `Build and Test Solution` run since 2026-09-30.

## What failed

| Run | Branch | Failure | Cause |
| --- | --- | --- | --- |
| 38755140287 | backlog (PR #92) | `ContributionsDashboardApiTests.Edits_ToAnUnknownEntry_AreNotFound`, 9m35s | `database is locked` in `BackofficeClient.InGroupAsync:53` |
| 38755092845 | main (merge of #93) | same | same |
| 38652300924 | backlog (PR #92) | `RecipeEditorApiTests.Units_AreTheCuratedList`, 9m40s | same |
| 37659980977 | main | two tests | same, twice in one run |
| 37651587233 | chore/dependabot-alerts-only | one test | same |
| 37394669238 | main | E2E touch scroll | "could not be dispatched within the 80 ms press delay" (runner timing) |
| 37385419575, 37385372685 | pad-nav | images job | NETSDK1064, fixed on main since 1f69cc9a |

Five of the last six test failures are one bug. PR #92 changes only Markdown, so its red check is this flake.

**Impact on production:** `images` needs `build-and-test`. The red run on main skipped the image push, so the Pi
does not have the #93 images yet.

## The bug

`BackofficeClient.InGroupAsync` creates a fresh Umbraco API user through `IUserService.CreateAsync` for every test
that needs a backoffice token. 15 tests call it. `UserRepository.PersistNewItem` reads, then inserts, in one deferred
SQLite transaction. When another connection commits between the read and the insert, SQLite refuses the upgrade and
Umbraco's `RetryPolicy` retries for about 9m40s, then throws. This is the same hazard that the four guards in
`src/KCC.Web/Features/Sqlite` cover for content, media, members and cache instructions. Backoffice user saves have
no guard.

The integration suite runs `[assembly: NotInParallel]`, so the competing writer is the site's own background work:
the cache-instruction sync, `CacheInstructionsPruningJob`, or an index rebuild left over from the previous test. No
run has identified which one yet.

The same stall can happen in production when an administrator creates a backoffice or API user on the Pi.

## Plan

### Step 0. Unblock main now

Re-run the failed job of run 38755092845. A green run pushes the #93 images and moves the `main` tags.

### Step 1. Prove the competing writer (bug flow: `/speckit-bug-assess`)

- Write a deterministic test in `tests/KCC.IntegrationTests/Features/Sqlite`, in the style of
  `CacheInstructionSyncTests`: hold a write on `OtherConnection` between the read and the insert of a user save, and
  assert that the save stalls.
- Log the holder of the SQLite write lock when a `SqliteException` with code 5 reaches a test, so the next CI failure
  names the job.

### Step 2. Guard the user save (product fix)

- Add a fifth guard that takes the write lock before the first read of a backoffice user save, the way
  `IMemberWriteLock.RunAsync` does for members. Register it in `SqliteComposer`, so the site refuses to boot if an
  Umbraco upgrade moves the registration.
- Add the path to the guard list in `CLAUDE.md`, and add its test to `RelationsWriteLockTests` or a sibling.
- Run the full integration suite, as `CLAUDE.md` requires for any change under `Features/Sqlite`.

### Step 3. Make the test helper write less

- Create the three API users (administrator, editor, translator) one time per test session in `UmbracoSite`, after
  seeding and warm-up. `BackofficeClient` then only requests a token.
- This removes 15 user writes from the middle of the suite. Step 2 still fixes the cause; this step only reduces the
  exposure.

### Step 4. Fail fast and loud

- Give integration tests a default `[Timeout]` of 2 minutes. A lock then fails in 2 minutes, and the job no longer
  spends 10 minutes on one test.
- Upload the site logs of each integration and E2E run with `combined-test-report` when a test fails, so a CI-only
  failure can be read without a re-run.

### Step 5. The touch-scroll E2E flake

- Pause the Playwright page clock around the press, as the CI timing notes describe. Do not raise the 80 ms budget.
- Already done: run 37394669238 tested a commit from before #85, which paused the page clock in
  `PhonePadTests.AScrollThatStartsOnThePad_LeavesTheCardsInTheStack`. No run since has failed it. On 2026-10-15 the
  test passed with a 500 ms gap after the press and 100 ms before each move, so it needs no change.

### Steps 3 and 4: how they landed

- `UmbracoSite` calls `BackofficeClient.CreateSessionUsersAsync` after seeding. The integration fixture has no
  warm-up step. Only the E2E `SiteProcess` warms up. `UserSaveRaceTests` still creates a new user through
  `BackofficeClient.ForNewUserAsync`, because a user save is what it races.
- `[assembly: Timeout(120_000)]` wraps each test body only. TUnit initializes the shared site outside the limit, so
  boot and seeding keep their own time.
- Both fixtures copy their run folder's `*.log` files and `logs/` into `KCC_TEST_SITE_LOGS` before they delete it.
  CI sets it to `$RUNNER_TEMP/site-logs` and copies that into `tests/results/site-logs` when the test step fails.

### Step 6. Catch flakes before a pull request does

- Add a `workflow_dispatch` and nightly `schedule` trigger that runs the integration and E2E suites three times on
  main and fails on any test that fails once.
- Make `tests/scripts/run.mjs` write the failed test names to `tests/results/failures.json`, so runs can be compared.
- Do not add automatic retries. A retry would have hidden this bug, which is also a production stall.

## Order

Step 0 now. Steps 1 and 2 as one bug through `/speckit-bug-assess`, `/speckit-bug-fix` and `/speckit-bug-test`.
Steps 3 to 5 as one small chore branch. Step 6 last, after the owner decides on the nightly cost in Actions minutes.
