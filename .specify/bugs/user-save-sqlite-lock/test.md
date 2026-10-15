# Bug Verification: Backoffice user saves stall on SQLite for about 10 minutes

- **Slug**: user-save-sqlite-lock
- **Tested**: 2026-10-15
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

The deterministic reproduction from the assessment stalls without the guard and completes in about 1 second with it.
The full integration suite and the full combined suite pass on the fix branch with no regressions.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (pre-fix) | `UserSaveRaceTests` with the guard classes unregistered | fail, as expected | Both race tests timed out at 20 s; both resolve tests failed. Recorded in fix.md. |
| Reproduction (post-fix) | `dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/UserSaveRaceTests/*"` | pass | 4 of 4 in 13 s. A commit on another connection lands between the save's reads and its write. |
| Integration suite | `dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj` | pass | 412 of 412 in 50 s, as `CLAUDE.md` requires after a change under `Features/Sqlite`. |
| Combined suite | `node tests/scripts/run.mjs` | pass | 2650 passed, 0 failed, 0 skipped, across 6 suites. |
| Build | `dotnet build KitchenCommandCenter.slnx -c Release` | pass | 0 warnings, 0 errors. |
| CI under the real poller | the pull request's `build-and-test` job | not-run | Runs when the pull request opens. |

## Output Excerpts

```
UserSaveRaceTests      total: 4   failed: 0   succeeded: 4   duration: 13s 340ms
Integration suite      total: 412 failed: 0   succeeded: 412 duration: 49s 957ms
run.mjs                2650 passed · 0 failed · 0 skipped across 6 suites
Release build          0 Warning(s)   0 Error(s)
```

## Residual Risks

- The CI failure was intermittent. One green CI run does not prove it is gone. Watch the next several runs on main.
- Store writes outside `CreateAsync` and `UpdateAsync` stay unguarded: delete, external logins and tokens, and a
  direct `IBackOfficeUserStore.SaveAsync`.
- A user write called inside an outer scope that has already read takes the lock too late.
- The token endpoint's save reads the user from a cache, so the race test covers sign-in through
  `IBackOfficeUserManager.UpdateAsync` instead. The store's lock covers both paths.

## Recommendation

Close the bug after the pull request's CI run passes. If a `database is locked` failure appears again, the commit-hook
ring in the test output names the competing writer. Start a new assessment from that output.
