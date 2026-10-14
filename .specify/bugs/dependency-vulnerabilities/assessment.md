# Bug Assessment: NuGet and npm packages carry 77 open vulnerability alerts

- **Slug**: dependency-vulnerabilities
- **Created**: 2026-10-14
- **Source**: pasted text, and the repository's open Dependabot alerts read through the GitHub API on 2026-10-14
- **Verdict**: valid
- **Severity**: high

## Report (verbatim or summarized)

> we should update our nuget and npm packages to address vulnerabilities

GitHub lists 77 open Dependabot alerts on `main`, numbers 85 to 172 with gaps: 53 NuGet and 24 npm. Eight are
critical: six in the ASP.NET Core runtime packs, and two in npm.
The repository takes Dependabot alerts with no update pull requests, so nothing closes them on its own.

## Symptom

The site builds and ships packages with known vulnerabilities. Expected: no open alert that a patched release can
close, and a written reason for each alert that no patch closes yet.

## Reproduction

1. Read the open alerts: `gh api 'repos/Th3FenrisWolf/Kitchen-Command-Center/dependabot/alerts?state=open&per_page=100'`.
2. Compare each vulnerable range with the version in `Directory.Packages.props`, `Dockerfile`, `global.json` and
   `yarn.lock`.

## Alerts by group

### .NET 10.0.0 to 10.0.12, fixed in 10.0.13 (released 2026-10-13, SDK 10.0.402)

| Package | Manifest | Scope | Worst severity |
|---|---|---|---|
| `Microsoft.AspNetCore.App.Runtime.{linux-arm64,linux-x64,win-x64}` | `KCC.Web.csproj` | development | critical: CVE-2026-88092 and CVE-2026-70188, security feature bypass |
| `Microsoft.NETCore.App.Runtime.{linux-arm64,linux-x64,win-x64}` | `KCC.Web.csproj` | development | high: CVE-2026-70197, remote code execution, and four denial of service CVEs |
| `System.Security.Cryptography.Xml` | `KCC.Admin.csproj`, `KCC.Contributions.csproj` | runtime | high: three denial of service CVEs |
| `System.Security.Cryptography.Pkcs` | `KCC.Web.csproj`, `KCC.Admin.csproj`, `KCC.Contributions.csproj` | runtime | high: CVE-2026-84008 |

The runtime packs come from `<RuntimeIdentifiers>win-x64;linux-x64;linux-arm64</RuntimeIdentifiers>` in
`KCC.Web.csproj` and follow the SDK's bundled runtime. The production image runs on
`mcr.microsoft.com/dotnet/aspnet:10.0.12`, so the shipped ASP.NET Core runtime is the vulnerable one, whatever the
alert's scope says.

### ImageSharp 3.1.12, fixed only in 4.1.2

Five alerts, three high and two medium: TIFF CCITT T4 and T6 encoder overruns, a BigTIFF decoder loop, an unchecked
histogram index in `HistogramEqualization`, and ICC CLUT allocation from unvalidated sizes. ImageSharp is transitive
through Umbraco. Umbraco 17.7.1, the newest 17.x, still depends on `SixLabors.ImageSharp` 3.1.12, and no 3.x release
after 3.1.12 exists.

### npm

The production SSR image installs only `src/KCC.Web/package.json`'s `dependencies`: `express`, `compression` and
`vue` (`Dockerfile`, stage `ssr-deps`). Those alerts reach production. The rest are build and test tooling.

| Package | Fixed in | Reaches production | Note |
|---|---|---|---|
| `@vue/server-renderer` 3.5.39 | 3.5.42 | yes | high: XSS through a missing CR in the attribute-name blacklist. The SSR sidecar renders every page with it. |
| `proxy-addr` | 2.0.8 | yes, through `express` | critical: IP spoofing through a trusted IPv4-mapped IPv6 subnet. `Features/Ssr/Server.js` sets no `trust proxy`, so the path is closed today. |
| `compression` | 1.8.2 | yes | high: memory leak on an early response close. `Server.js:90` uses it. |
| `qs` | 6.16.0 | yes, through `express` | two medium: denial of service and an array-limit bypass |
| `shell-quote` | 1.11.0 | no, `concurrently` | critical and high |
| `vitest`, `@vitest/mocker` | 4.1.11 | no | medium: path traversal in a redirect mock |
| `postcss` | 8.5.23 | no | high and medium: source-map file disclosure |
| `brace-expansion` | 5.0.12 | no | five alerts, high and medium |
| `browserslist` | 4.28.7 | no | two high |
| `nanoid` | 3.3.18 | no | two high |
| `source-map-js` | 1.2.2 | no | high |
| `postcss-selector-parser` | 7.1.6 | no | medium |
| `baseline-browser-mapping` | 2.11.0 | no | medium |
| `braces` | none | no | high: no patched release exists. It comes through `micromatch`, in build tooling only. |

## Suspected Code Paths

- `global.json` — SDK `10.0.100` with `rollForward: latestFeature`. CI installs from it through `actions/setup-dotnet`
  (`.github/workflows/build-and-test.yml:38-40`).
- `Dockerfile:32` — `mcr.microsoft.com/dotnet/sdk:10.0.401`. `Dockerfile:49` —
  `mcr.microsoft.com/dotnet/aspnet:10.0.12`.
- `Directory.Packages.props` — `Microsoft.AspNetCore.Mvc.Testing` and `Microsoft.Extensions.Http.Polly` at 10.0.12,
  `Microsoft.Data.Sqlite` and `Microsoft.EntityFrameworkCore.Design` at 10.0.11, `Umbraco.Cms*` at 17.7.0.
  `CentralPackageTransitivePinningEnabled` is on, so a `PackageVersion` entry pins a transitive package.
- `src/KCC.Web/KCC.Web.csproj:4` — the runtime identifiers that pull the runtime packs.
- `src/KCC.Web/package.json` — `vue` `^3.5.39`, `express` `^5.2.1`, `compression` `^1.8.1`; devDependencies `vitest`
  `^4.1.9`, `postcss` `^8.5.16`. `packages/admin-client-config/package.json` — `vitest` `^4.1.9`.
- `package.json` — the root `resolutions`, which holds only `@types/node` today.
- `yarn.lock` — Yarn 1 lockfile.

## Root Cause Hypothesis

Confidence: high. The packages were current when they were chosen. New advisories landed after, most of them in the
.NET 10.0.13 security release of 2026-10-13 and in transitive npm packages. Dependabot raises alerts only, by the
owner's choice, so nothing proposed the updates.

## Owner decisions (2026-10-14)

- **Q17, ImageSharp**: mitigate and wait for Umbraco. Stay on 3.1.12, refuse TIFF uploads in the media library and in
  photo import, and keep imaging requests HMAC-signed. The five alerts stay open, each dismissed with a written reason
  or left open with that reason in this file, until Umbraco ships a patched ImageSharp.
- **Q18, scope**: close every alert that a patch closes, and take the other patch releases on the same lines, such as
  Umbraco 17.7.1. No minor or major upgrades.

## Proposed Remediation

**Preferred**:

1. **.NET 10.0.13.** Move `Dockerfile` to `sdk:10.0.402` and `aspnet:10.0.13`. Raise `global.json` to `10.0.402` so CI
   and every machine build with the 10.0.13 runtime packs. Raise the `Microsoft.*` 10.0.x entries in
   `Directory.Packages.props` to 10.0.13, and add `PackageVersion` entries that pin
   `System.Security.Cryptography.Xml` and `System.Security.Cryptography.Pkcs` to 10.0.13.
2. **Umbraco 17.7.1.** Raise `Umbraco.Cms`, `Umbraco.Cms.Api.Management` and `Umbraco.Cms.Persistence.EFCore`. Read its
   release notes for any change to the SQLite guards in `Features/Sqlite`, because `SqliteComposer` refuses to boot if
   a wrapped registration moved.
3. **Other patch releases** on the current lines in `Directory.Packages.props`, such as `uSync` and `Anthropic`, where
   a newer patch exists.
4. **npm.** Raise `vue` to `^3.5.42`, `compression` to `^1.8.2`, and `vitest` to `^4.1.11` in both manifests. Refresh
   `yarn.lock` so the transitive packages take their fixed versions. Add root `resolutions` only for a transitive
   package whose parent's range will not take the fix.
5. **ImageSharp mitigation.** Set Umbraco's `Content:Imaging:ImageFileTypes` to the formats the site uses, without
   `tif` and `tiff`. Check that the photo import refuses a TIFF on the server, and not only through the `accept`
   attribute in `PhotoStep.vue`. Confirm that the imaging HMAC key stays configured.
6. **Leftovers.** Record each alert that stays open, with its reason: the five ImageSharp alerts and `braces`.

**Alternatives**:
- Force ImageSharp 4.1.3 under Umbraco. The owner turned this down in Q17: Umbraco 17 was not built against it.
- Upgrade to Umbraco 18. Out of scope under Q18.

**Files likely to change**:
- `global.json`, `Dockerfile`, `Directory.Packages.props`
- `src/KCC.Web/package.json`, `packages/admin-client-config/package.json`, `package.json`, `yarn.lock`
- `src/KCC.Web/appsettings.json`
- `docs/hosting/` and `.github/workflows/build-and-test.yml`, if they name an SDK or image version
- The photo import's server-side upload check, if it accepts TIFF

**Tests to add or update**:
- An integration test that a TIFF upload is refused by the media library's image types and by the photo import
  endpoint.
- The full `node tests/scripts/run.mjs` run across all six suites, as the definition of done asks. The SQLite suite
  matters most after the Umbraco patch.
- A check after the change: the open-alert list holds only the alerts recorded as leftovers.

## Risks & Considerations

- **Warning:** the production image changes its runtime. Build and run the `app` and `ssr` images for linux-arm64 and
  check the site on the Pi before you deploy.
- Every developer machine and CI needs SDK 10.0.402. The Mac has 10.0.301 today.
- An Umbraco patch can move a registration that `SqliteComposer` replaces. The site then refuses to boot, which the
  integration suite shows.
- `yarn install` needs `FONTAWESOME_NPM_AUTH_TOKEN`.
- Build both bundles together with `yarn build:all` after the `vue` change, so scope IDs match.
- The photo import is on branch `001-photo-recipe-import`, not on `main`. If it lands first, step 5 checks its upload
  path. If this lands first, the photo import's own checks must refuse TIFF.
- New advisories keep landing. Read the alert list again on the day of the fix.

## Open Questions

- None.
