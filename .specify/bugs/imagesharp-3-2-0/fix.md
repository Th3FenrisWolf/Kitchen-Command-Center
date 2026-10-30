# Bug Fix: ImageSharp 3.1.12 carries a new high advisory and breaks the clean build

- **Slug**: imagesharp-3-2-0
- **Fixed**: 2026-10-30
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

`SixLabors.ImageSharp` is pinned to 3.2.0, which closes GHSA-v76p-62qx-wwq2 and the five advisories that
`Directory.Build.props` suppressed. All five suppressions are removed. The Release build is clean again.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `Directory.Packages.props` | modified | new transitive pin `SixLabors.ImageSharp` 3.2.0, lifted for every project by `CentralPackageTransitivePinningEnabled` |
| `Directory.Build.props` | modified | the five `NuGetAuditSuppress` entries removed: 3.2.0 patches each of them |

## Tests Added or Updated

None, by the owner's choice (2026-10-30): D1 is the guard. The existing processed-media tests (`RecipeQueriesTests`,
`HomeImagesTests`, which fetch a `?width=` URL and assert `image/webp`) and `TiffUploadTests` ran on 3.2.0 and pass.

## Local Verification

| Row | Check | Result |
|---|---|---|
| D1 | `dotnet build KitchenCommandCenter.slnx -c Release --disable-build-servers` | pass: 0 warnings, 0 errors (was 8 × NU1903); `project.assets.json` resolves `SixLabors.ImageSharp/3.2.0` |
| D2 | `yarn build:all` | pass |
| D3 | front-end checks | not triggered: no `.vue`, `.ts`, `.js` or `.css` change |
| D4 | `node tests/scripts/run.mjs` with a no-op `open` | pass: 4,057 passed, 0 failed, 1 skipped across 6 suites |
| D5-D9 | generated files, ramps, brand, deploy lint, images | not triggered |
| D10 | docs | pass: no line in `README.md`, `CLAUDE.md` or `docs/` names the suppressions or the ImageSharp version |

The review loop runs once the pull request is open.

## Deviations from Assessment

None.

## Follow-ups

- `.specify/bugs/dependency-vulnerabilities/fix.md` still records alerts 120 to 124 as open. It is a finished record and
  stays as written. GitHub closes those Dependabot alerts once `main` resolves 3.2.0.
- The TIFF upload refusal stays as defence in depth (owner, 2026-10-30).
