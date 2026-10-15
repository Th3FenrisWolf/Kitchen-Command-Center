# Bug Assessment: Backoffice user saves stall on SQLite for about 10 minutes

- **Slug**: user-save-sqlite-lock
- **Created**: 2026-10-15
- **Source**: pasted text (CI runs 38755140287, 38755092845, 38652300924, 37659980977, 37651587233)
- **Verdict**: valid
- **Severity**: high

## Report (summarized)

Integration tests fail with `SqliteException: SQLite Error 5: 'database is locked'` after about 9m40s. Every failure
is in `BackofficeClient.InGroupAsync` (`tests/KCC.IntegrationTests/Config/BackofficeClient.cs:53`), inside
`IUserService.CreateAsync` → `BackOfficeUserStore.CreateAsync` → `UserRepository.PersistNewItem`. Five of the last six
test failures in CI are this bug, on main and on pull requests that change only Markdown. A red run on main skips the
`images` job, so production deploys stop.

## Symptom

A backoffice user save waits about 9m40s and then throws `database is locked`. Expected: the save completes in
milliseconds, as content, media and member saves do behind their guards.

## Reproduction

1. Start the integration site (`UmbracoSite`) and wait more than 60 seconds, so Umbraco's distributed-job poller runs.
2. Create a backoffice user with `IUserService.CreateAsync`, as `BackofficeClient.InGroupAsync` does.
3. Let another connection commit between the save's reads and its INSERT. The poller commits every 5 seconds, so this
   happens intermittently in CI. A `UserSavingNotification` handler that commits on `OtherConnection` makes it
   deterministic.

## Suspected Code Paths

All in Umbraco 17.7, decompiled with ilspycmd. None of these takes a distributed lock.

- `UserService.CreateAsync`: one core scope. It reads the performing user, the user groups, `GetByEmail` and
  `GetByUsername`, then Identity's validators, then hashes the password with PBKDF2, then inserts. The hash makes the
  window between read and write the widest of any user path.
- `UserService` write methods: update, invite, profile, avatar, password change and reset, delete, disable, enable,
  unlock, `AddClientIdAsync`. Each reads then writes in one scope.
- `BackOfficeUserStore.UpdateAsync`: `GetAsync`, then `SaveAsync`. The token endpoint (client credentials) and
  `UmbracoSignInManager` reach it for last-login date, failed attempts and the security stamp.
- `DistributedBackgroundJobHostedService`: after a 60-second delay, every 5 seconds
  `DistributedJobService.TryTakeRunnableAsync` takes `EagerWriteLock(Constants.Locks.DistributedJobs)` and commits,
  even when no job is due.
- `tests/KCC.IntegrationTests/Config/BackofficeClient.cs:53`: 15 tests create an API user each, about 90 seconds or
  more into the suite.

## Root Cause Hypothesis

Confidence: high for the mechanism, medium for the named writer. On SQLite a deferred transaction that has read
cannot become the writer once another connection commits, and Umbraco's `RetryPolicy` retries the BUSY result for
about 10 minutes. Backoffice user saves read before they write, with no lock taken first. The distributed-job poller
commits every 5 seconds regardless of test activity, and both CI failures started 88 and 96 seconds into the suite,
after its 60-second delay. The same stall can hit production when the owner signs in to the backoffice or edits a user.

## Proposed Remediation

**Preferred**: add a fifth guard with two parts, both taking `scope.WriteLock(Constants.Locks.MemberTree)` before
their first read, so all identity writes share one lock with `IMemberWriteLock`:

1. `WriteLockedUserService`, a decorator over `IUserService` that opens a core scope and takes the lock around each
   write method, and forwards the read methods unchanged. Register it with `SqliteComposer.Wrap`. Umbraco registers
   `UserService` as a singleton with `ImplementationType` set, so the existing `Wrap` works.
2. `WriteLockedBackOfficeUserStore`, a subclass of `BackOfficeUserStore` that overrides `CreateAsync` and
   `UpdateAsync` the same way. Umbraco registers the store through a factory lambda as
   `IUserStore<BackOfficeIdentityUser>` (scoped) and as `IBackOfficeUserStore`, so `SqliteComposer` needs a `Wrap`
   variant that finds the factory registration and refuses to boot when it cannot.

Add a test-only diagnostic: a `SqliteConnectionInterceptor` that installs `sqlite3_commit_hook` on each NPoco
connection and keeps a bounded ring of recent commits (time, thread, stack). When a test sees a `SqliteException`
with code 5, the ring and the extended error code go into the test output, so the next stall on any path names its
writer.

Keep the distributed-job poller at its production interval in tests.

**Alternatives**:
- Decorate `IUserService` only: less code, and sign-in and the token endpoint stay unguarded. Rejected by the owner.
- Delay `Umbraco:CMS:DistributedJobs:Delay` in tests: calms CI and hides the production hazard. Rejected by the owner.
- Force IMMEDIATE transactions for every scope: serializes reads too. Rejected.

**Files likely to change**:
- `src/KCC.Web/Features/Sqlite/WriteLockedUserService.cs` (new)
- `src/KCC.Web/Features/Sqlite/WriteLockedBackOfficeUserStore.cs` (new)
- `src/KCC.Web/Features/Sqlite/SqliteComposer.cs`
- `tests/KCC.IntegrationTests/Config/UserSaveProbe.cs` (new)
- `tests/KCC.IntegrationTests/Config/CommitLog.cs` (new, the commit-hook ring)
- `tests/KCC.IntegrationTests/Config/UmbracoSite.cs`
- `tests/KCC.IntegrationTests/Features/Sqlite/UserSaveRaceTests.cs` (new)
- `tests/KCC.IntegrationTests/Features/Sqlite/RelationsWriteLockTests.cs` or a sibling resolve test
- `CLAUDE.md` (guard list and test list)

**Tests to add or update**:
- `UserSaveRaceTests`: a `UserSaveProbe` on `UserSavingNotification` commits through `OtherConnection` during the
  next user save. Creating a user with `BackofficeClient.EditorAsync` completes within 20 seconds, and the user exists.
  Without the guard the test fails in 20 seconds. Arm the probe twice to cover the token endpoint's `UpdateAsync`.
- A sign-in or `UpdateAsync` case through the store, with the same probe.
- Resolve tests: `IUserService` resolves to `WriteLockedUserService` over `UserService`, and the user store resolves
  to `WriteLockedBackOfficeUserStore`.
- The full integration suite, as `CLAUDE.md` requires for any change under `Features/Sqlite`.

## Risks & Considerations

- `IUserService` has about 47 members. A new member in an Umbraco upgrade breaks the build, which is the safe failure.
- The store subclass depends on `BackOfficeUserStore`'s long constructor. An upgrade that changes it breaks the build
  or the boot, never the guard silently.
- A user write method called inside an outer scope that has already read still takes the lock late. Umbraco's
  `EnsureLocks` takes a queued lock at the scope's first database access, so the lock must be queued on the outermost
  scope the guard opens.
- `MemberTree` makes member sign-ins and user saves wait for each other for a few milliseconds.
- The commit hook runs on every commit in the Testing environment only. It must never register in production.

## Open Questions

None. The owner settled the guard scope, the lock id, the diagnostic and the poller interval on 2026-10-15.
