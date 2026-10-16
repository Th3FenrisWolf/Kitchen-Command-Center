# Bug Fix: NuGet and npm packages carry open vulnerability alerts

- **Slug**: dependency-vulnerabilities
- **Fixed**: 2026-10-15
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

The site moves to .NET 10.0.13, Umbraco 17.7.1 and patched npm packages, and the media library refuses TIFF uploads.
Every alert that a patch closes is closed on this branch; the five ImageSharp alerts and `braces` stay open with the
reasons below.

## Alerts on the day of the fix

The open-alert list read on 2026-10-15 held 32 alerts, not the 77 of the assessment: PR #93 had already pinned
`System.Security.Cryptography.Pkcs` and `.Xml` to 10.0.13, and the runtime-pack alerts had closed. Three new medium
alerts had opened on `MessagePack` 3.1.8 (GHSA-qhrr-8q5h-9q3h, fixed in 3.1.9), which Umbraco 17.7.0 and 17.7.1 pull.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `Dockerfile` | modified | `sdk:10.0.401` to `sdk:10.0.402`, `aspnet:10.0.12` to `aspnet:10.0.13` |
| `global.json` | modified | SDK `10.0.100` to `10.0.402`, so CI's `setup-dotnet` and every machine build with the 10.0.13 runtime packs |
| `Directory.Packages.props` | modified | `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.Data.Sqlite`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.Extensions.Http.Polly` to 10.0.13; `Umbraco.Cms*` to 17.7.1; `uSync` to 17.4.3; new transitive pin `MessagePack` 3.1.12 |
| `src/KCC.Web/package.json` | modified | `vue` ^3.5.42, `compression` ^1.8.2, `vitest` ^4.1.11, `postcss` ^8.5.23, `concurrently` ^10.0.6 |
| `packages/admin-client-config/package.json` | modified | `vitest` ^4.1.11 |
| `yarn.lock` | modified | the vulnerable entries dropped and re-resolved: `vue` 3.5.44, `vitest` 4.1.12, `postcss` 8.5.30, `brace-expansion` 5.0.12, `nanoid` 3.3.20, `browserslist` 4.29.3, `source-map-js` 1.2.2, `shell-quote` 1.12.0, `qs` 6.16.1, `proxy-addr` 2.0.8, `postcss-selector-parser` 7.1.6, `baseline-browser-mapping` 2.11.29 |
| `src/KCC.Web/appsettings.json` | modified | `Umbraco:CMS:Content:DisallowedUploadedFileExtensions` adds `tif` and `tiff` |
| `tests/KCC.IntegrationTests/Features/Media/TiffUploadTests.cs` | added test | the media library refuses TIFF, takes jpg, png and webp, and keeps Umbraco's default refusals |
| `tests/KCC.UnitTests/Features/RecipeImport/PhotoValidationTests.cs` | updated test | little-endian and big-endian TIFF cases |

## Diff Highlights

```json
"Content": {
  "DisallowedUploadedFileExtensions": [ "tif", "tiff" ]
},
```

Umbraco binds this set onto its defaults, so `aspx`, `config` and the other default refusals stay.
`TheMediaLibrary_KeepsUmbracosDefaultRefusals` pins that.

## Tests Added or Updated

- `TiffUploadTests.TheMediaLibrary_RefusesTiffUploads(tif|tiff)`: failed before the config change, passes after it.
- `TiffUploadTests.TheMediaLibrary_StillTakesTheSiteImageFormats(jpg|png|webp)`
- `TiffUploadTests.TheMediaLibrary_KeepsUmbracosDefaultRefusals(aspx|config|cshtml)`
- `PhotoValidationTests.AFileThatIsNotAJpegPngWebPOrGif_IsRefused(tiff little-endian|tiff big-endian)`: the photo
  import already refused TIFF by its magic bytes; these cases pin it.

## Local Verification

- `dotnet build KitchenCommandCenter.slnx` on SDK 10.0.402: 0 warnings, 0 errors. NuGet audit raises nothing beyond
  the suppressed ImageSharp advisories.
- `yarn build:all`: both bundles build.
- `yarn audit`: 0 vulnerabilities.
- `node tests/scripts/run.mjs`: 2,664 passed, 0 failed, 0 skipped across 6 suites, the SQLite integration tests among
  them on Umbraco 17.7.1.
- `docker buildx build --platform linux/arm64` for the `app` and `ssr` targets: both build. The app image lists
  `Microsoft.AspNetCore.App 10.0.13` and `Microsoft.NETCore.App 10.0.13`; the SSR image holds `vue` and
  `@vue/server-renderer` 3.5.44, `compression` 1.8.2, `express` 5.3.0, `proxy-addr` 2.0.8 and `qs` 6.16.1.
- Not done: the images have not run on the Pi.

## Deviations from Assessment

- **`ImageFileTypes` left at its default.** Umbraco binds `Content:Imaging:ImageFileTypes` onto its default set, so a
  configured list adds to `tiff` and `tif` and cannot remove them. `DisallowedUploadedFileExtensions` refuses the
  upload instead, so no TIFF enters the media library for ImageSharp to decode.
- **`MessagePack` pinned to 3.1.12.** Not in the assessment: its alerts opened after it. Umbraco 17.7.1 still depends on
  3.1.8, and 3.1.12 is a patch on the same line.
- **`concurrently` raised to ^10.0.6.** It pinned `shell-quote` to exactly 1.8.4; its 10.0.6 patch takes 1.12.0, so no
  root `resolutions` entry is needed.
- **`postcss` range raised to ^8.5.23** in `src/KCC.Web/package.json`, the direct dependency the alert names.
- **Crypto pins.** `System.Security.Cryptography.Pkcs` and `.Xml` were already at 10.0.13 from PR #93.

## Alerts that stay open

| Alert | Package | Reason |
|-------|---------|--------|
| 120 to 124 | `SixLabors.ImageSharp` 3.1.12 | Fixed only in 4.1.2, which Umbraco 17 is not built against (owner, Q17). TIFF uploads are refused, and imaging requests stay HMAC-signed. They close when Umbraco ships a patched ImageSharp. |
| `braces` | `braces` 3.0.3 | No patched release exists. It comes through `micromatch` in build tooling only and never reaches the production images. |

## Follow-ups

- Run the `app` and `ssr` images on the Pi before you deploy, because the production runtime changes.
- After the merge, read the open-alert list and check that it holds only the alerts above.
- Every developer machine needs SDK 10.0.402.
