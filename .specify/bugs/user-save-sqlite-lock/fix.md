# Bug Fix: Backoffice user saves stall on SQLite for about 10 minutes

- **Slug**: user-save-sqlite-lock
- **Fixed**: 2026-10-15
- **Assessment**: [assessment.md](assessment.md)
- **Status**: fixed locally; the full integration suite is still to run

## Summary

Backoffice user writes now take `Constants.Locks.MemberTree` before their first read, so on SQLite they become the
writer before another connection can commit under them. Two seams carry the lock. `WriteLockedUserService` wraps every
write method of `IUserService`. `WriteLockedBackOfficeUserStore` replaces Umbraco's `BackOfficeUserStore` and locks
its `CreateAsync` and `UpdateAsync`, which sign-in and the token endpoint reach outside the user service. The
integration tests gain a commit-hook ring that names the last writers when a test fails with SQLite error 5.

## Changes

| File | Change |
|---|---|
| `src/KCC.Web/Features/Sqlite/WriteLockedUserService.cs` | New decorator over `IUserService`. Locks each write, forwards each read, and forwards the default interface members to `UserService`. |
| `src/KCC.Web/Features/Sqlite/WriteLockedBackOfficeUserStore.cs` | New subclass of `BackOfficeUserStore`. Locks `CreateAsync` and `UpdateAsync`. |
| `src/KCC.Web/Features/Sqlite/SqliteComposer.cs` | Wraps `IUserService` in place. A new `Replace` swaps the store's factory registration of `IUserStore<BackOfficeIdentityUser>` and its `IBackOfficeUserStore` registration for the subclass. A shared `Find` matches an implementation type or a factory's return type, and the boot throws when it finds neither. |
| `tests/KCC.IntegrationTests/Config/UserSaveProbe.cs` | New probe on `UserSavingNotification`. Commits on `OtherConnection` during the next user save. |
| `tests/KCC.IntegrationTests/Config/CommitLog.cs` | New `SqliteConnectionInterceptor`. Installs `sqlite3_commit_hook` on each NPoco connection and keeps the last 64 commits (time, thread, stack). An `[AfterEvery(Test)]` hook prints them with the extended error code when a test fails with `SqliteException` code 5. |
| `tests/KCC.IntegrationTests/Config/UmbracoSite.cs` | Registers the probe, and registers `CommitLog` first among the provider interceptors so it receives the raw `SqliteConnection`. |
| `tests/KCC.IntegrationTests/Features/Sqlite/UserSaveRaceTests.cs` | New. Two race tests and two resolve tests. |
| `CLAUDE.md` | "SQLite writes": five guards, the new one listed, the user store's remaining gaps, the new test file and the commit ring. |

## Tests Added or Updated

- `CreatingAUserWhileAReviewCommits_Finishes`: the probe commits a review during the first save of
  `BackofficeClient.EditorAsync`. The call finishes within 20 seconds, the probe's commit completes, and the new user
  holds a last sign-in date from the token endpoint.
- `RecordingASignInWhileAReviewCommits_Finishes`: reads the administrator through `IBackOfficeUserManager`, sets its
  last sign-in date, clears the user cache, and updates it while the probe commits. It finishes within 20 seconds.
- `TheUserService_TakesTheWriteLock`: `IUserService` resolves to `WriteLockedUserService` over Umbraco's `UserService`.
- `TheBackOfficeUserStore_TakesTheWriteLock`: `IUserStore<BackOfficeIdentityUser>` and `IBackOfficeUserStore` resolve
  to `WriteLockedBackOfficeUserStore`.

## Local Verification

| Check | Result |
|---|---|
| `UserSaveRaceTests` without the guard registered | 4 of 4 failed: both resolve tests, and both race tests with `TimeoutException` at 20 s |
| `UserSaveRaceTests` with the guard | 4 of 4 passed in 13.8 s; the race tests took 1.3 s and 1.1 s |
| `CommitLog` hook | A test throwing `SqliteException` code 5 printed the header and the extended code 517; a run against the site filled the ring with 64 commits and their stacks |
| `dotnet build KitchenCommandCenter.slnx -c Release` | Succeeded, 0 warnings, 0 errors |
| Full integration suite | Not run here; the owner runs it |

## Deviations from Assessment

- The second race test records a sign-in through `IBackOfficeUserManager.UpdateAsync` rather than the token endpoint.
  Through the token endpoint the test passed without the guard: the store's `UpdateAsync` reads the user through
  `UserRepository.Get(int)`, which serves a user read in the last five minutes from the cache, so nothing is read
  inside the transaction. The test clears the user cache to reach the read that a sign-in after five idle minutes
  makes.
- Umbraco's `UserService` is internal, so the resolve test checks the inner service's type name.
- The store also replaces the `IBackOfficeUserStore` registration, so both registrations resolve to the subclass.

## Follow-ups

- The store's other writes (delete, external logins, tokens) and `IBackOfficeUserStore.SaveAsync` called outside the
  user service remain unguarded. They are listed in `CLAUDE.md`.
- A user write called inside an outer scope that has already read takes the lock too late.
