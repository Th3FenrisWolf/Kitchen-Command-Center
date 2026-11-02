# Bug Assessment: ImageSharp 3.1.12 carries a new high advisory and breaks the clean build

- **Slug**: imagesharp-3-2-0
- **Created**: 2026-10-30
- **Source**: pasted text, the Release build's NuGet audit output, and the GitHub advisory API read on 2026-10-30
- **Verdict**: valid
- **Severity**: high

## Report (verbatim or summarized)

> NuGet audit NU1903 on SixLabors.ImageSharp 3.1.12 (GHSA-v76p-62qx-wwq2, high, published 2026-10-07, fixed in 3.2.0)
> breaks D1 "no warning" on main. The owner already chose the fix: a separate fix PR pinning SixLabors.ImageSharp 3.2.0
> (not suppression).

GHSA-v76p-62qx-wwq2: in a tiled fax TIFF, the tile buffer is sized by `TileWidth` but the fax decompressor writes
scanlines of `ImageWidth`, a heap out-of-bounds write. Vulnerable `>= 3.0.0, < 3.2.0` and `>= 4.0.0, < 4.1.1`.

## Symptom

`dotnet build KitchenCommandCenter.slnx -c Release` on `main` (afb4af2b) prints `warning NU1903` for
`SixLabors.ImageSharp` 3.1.12 in `KCC.Web`, `KCC.UnitTests`, `KCC.IntegrationTests` and `KCC.ReaderEval`: 8 warnings,
and every one of them is this advisory. Expected: no warning (constitution D1).

## Reproduction

1. `dotnet build KitchenCommandCenter.slnx -c Release --disable-build-servers` at the repository root.
2. Read the NU1903 lines naming GHSA-v76p-62qx-wwq2.

## Suspected Code Paths

- `Directory.Packages.props` — central versions with `CentralPackageTransitivePinningEnabled`. ImageSharp is not
  pinned, so it resolves to Umbraco's floor: `Umbraco.Cms.Imaging.ImageSharp` 17.7.1 depends on ImageSharp `>= 3.1.12`.
- `Directory.Build.props:8-14` — five `NuGetAuditSuppress` entries for the ImageSharp advisories that PR #97 could not
  close on 3.x.

## Root Cause Hypothesis

High confidence. The advisory was published on 2026-10-07, after PR #97. Umbraco 17.7.1 asks only for ImageSharp
`>= 3.1.12`, and nothing in the repository raises it. 3.2.0 is the first 3.x release with the fix, and it also patches
all five suppressed advisories:

| Advisory | Severity | 3.x fixed in |
|---|---|---|
| GHSA-gwg2-r3hj-4w44 | medium | 3.2.0 |
| GHSA-j3p4-wp97-rph4 | high | 3.2.0 |
| GHSA-j9gm-c75j-xc9q | high | 3.2.0 |
| GHSA-jjfr-hcj7-qf5w | high | 3.2.0 |
| GHSA-wmxv-xphr-5c9g | medium | 3.2.0 |

## Proposed Remediation

**Preferred**: pin `SixLabors.ImageSharp` 3.2.0 with a `PackageVersion` in `Directory.Packages.props`, as
`System.Security.Cryptography.Pkcs` and `.Xml` are pinned. Transitive pinning lifts it for every project. Delete all
five `NuGetAuditSuppress` entries from `Directory.Build.props`, because 3.2.0 closes each of them. The ranges still
fit: Umbraco wants `>= 3.1.12`, and `SixLabors.ImageSharp.Web` 3.2.0 wants `>= 3.1.11`.

**Alternatives**:
- Suppress GHSA-v76p-62qx-wwq2 like the other five. Rejected by the owner: it leaves the vulnerable decoder in.
- Move to ImageSharp 4.1.2. Ruled out in PR #97: Umbraco 17 is not built against 4.x, and processed media URLs answered
  400.

**Files likely to change**:
- `Directory.Packages.props`
- `Directory.Build.props`
- `.specify/bugs/dependency-vulnerabilities/fix.md` is a finished record and stays as written.

**Tests to add or update**: none (owner, 2026-10-30). D1, the clean Release build, is the guard. Processed media is
already covered: `RecipeQueriesTests` and `HomeImagesTests` fetch a `?width=` URL and assert `image/webp`.

## Risks & Considerations

- A minor-version bump of the image pipeline under Umbraco: the processed-media integration tests and the seeder's
  ImageSharp use (`RecipeSeedImages.cs`, `KCC.ReaderEval/PhotoEncoder.cs`) must still pass.
- The TIFF upload refusal from PR #97 (`TiffUploadTests`) stays as defence in depth (owner, 2026-10-30), since the new
  advisory is in the TIFF decoder as well.

## Open Questions

None.
