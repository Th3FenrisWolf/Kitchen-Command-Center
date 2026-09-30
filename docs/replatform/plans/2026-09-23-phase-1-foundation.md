# Replatform Phase 1 — Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or
> superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for
> tracking.

**Status:** not started. **Resume point:** Task 1. **Requires Phase 0 done** (it is, 2026-09-23): tag
`xperience-final` on origin, branch `replatform` checked out, `src/KCC.Web/Features/Dictionary/ui-strings.json`
committed, reference screenshots in `docs/replatform/reference/xperience-final/`. Read **Findings from Phase 0**
below before Task 1.

**Goal:** Replace Xperience with an Umbraco 17 host on SQLite that boots from a fresh clone, imports its schema,
dictionary and baseline content from uSync, and renders the home page, header, footer, 404 and 500 pages through the
unchanged Vue SSR pipeline in both ramps.

**Architecture:** `KCC.Web` becomes an Umbraco site (`CreateUmbracoBuilder().AddBackOffice().AddWebsite()`), with
Kentico removed outright. Features that later phases port are excluded from compilation by an `Unported slices`
item group, so every task ends buildable. Schema, dictionary and baseline content are created once by dev-only
bootstrap code, exported by uSync, committed, and imported into every new database on first boot. Page
controllers are Umbraco route-hijacking `RenderController`s that return the existing Razor views, which still hand
Vue template text to the Node SSR service.

**Tech Stack:** Umbraco.Cms 17.x (17.7.0 until 17.8.0 ships), uSync 17.4.2, SQLite (Microsoft.Data.Sqlite),
ModelsBuilder, Vite.AspNetCore 2.4, Vue 3 SSR (Node/Express), TUnit 1.27 + Moq, TUnit.Playwright 1.27,
Microsoft.AspNetCore.Mvc.Testing 10.0.x, Vitest.

**Spec:** `docs/replatform/specs/2026-09-21-replatform-off-xperience.md` — §6 (architecture), §7 (content model), §12
(environments and seeding), §14 (testing), §15 (the Phase 1 row), §19 (behaviours proven first).

## Global Constraints

- Work on branch **`replatform`**. The replatform's spec, phase plans and reference set are tracked in
  `docs/replatform/`: commit changes to them (a status line, a correction) with the work they describe. Everything
  else under `.superpowers/` stays gitignored: never stage anything there. Commit messages are Title Case imperative
  with no attribution lines. Ask the owner before any `git push`.
- **Umbraco.Cms 17.x, never 18.** Use 17.8.0 if NuGet lists it, else 17.7.0. When 17.8.0 ships (scheduled
  2026-10-29), bump it — it fixes the SQLite `Cache=Shared` + WAL locking bug.
- **The SQLite connection string never contains `Cache=Shared`.** Use `Cache=Private`. A unit test guards this.
- **`Umbraco:CMS:ModelsBuilder:ModelsMode` is always set explicitly**: `SourceCodeManual` in Development, `Nothing`
  everywhere else (the default, `InMemoryAuto`, fails boot without an extra package). Models live in
  `Features/Models/Generated` with namespace `KCC.Web.Features.Models.Generated`, and are committed.
- **uSync root folder is pinned to `uSync/v17/`.** Schema imports at every startup; dictionary items import
  create-only; baseline content imports on first boot only. Content is never exported on save.
- **ModelsBuilder also generates the built-in media and member types** into `KCC.Web.Features.Models.Generated`:
  `Folder`, `Image`, `File`, `Member` and the `UmbracoMedia*` types. No document type may take one of those names (so
  the tree folder type is `contentFolder`), and a file that imports the namespace writes `System.IO.File` in full.
- **Document type aliases** (exact): `homePage`, `recipeListingPage`, `createRecipePage`, `addVariantPage`,
  `accountPage`, `loginPage`, `accountSettingsPage`, `registrationCompletePage`, `siteSettings`, `contentFolder`,
  `recipeCategory`, `recipeTag`, `statusCodePage`; composition `metadata`; element types `navLink`, `navGroup`.
- **Route hijacking:** a page controller is named `<Alias>Controller`, derives from `RenderController`, has **no
  `[Route]`**, overrides `Index()` and returns its view **by explicit path** (the base `Index()` 404s when a node has no
  template).
- **Use only APIs that survive Umbraco 18:** no `UmbracoApiController`, no `ILocalizationService`, no
  `IPublishedContent.Children`/`.Parent` properties. Use `ControllerBase` for APIs, `IDictionaryItemService`,
  `IContentEditingService`, `IContentPublishingService`, `IContentTypeEditingService`, and the
  `FriendlyPublishedContentExtensions` (`Children()`, `Descendants()`, `Url()`). Warnings are errors, so a member
  Umbraco 17 marks obsolete fails the build (CS0618) even where it still works. One such member is the short
  `IContentService.GetPagedChildren(id, pageIndex, pageSize, out total)` overload: read children through
  `IDocumentNavigationQueryService.TryGetChildrenKeys` and `IContentService.GetByIds` instead.
- **KCC.Web has nullable reference types disabled** and treats warnings as errors: do not write `string?` (or any `?`
  on a reference type) in `src/KCC.Web` or `tests/KCC.UnitTests` — it raises CS8632. `tests/KCC.IntegrationTests` and
  `tests/KCC.E2ETests` have nullable enabled and use annotations normally. StyleCop runs on `src/KCC.Web`: usings
  sorted, trailing commas in multi-line initializers, static members before instance members of the same access.
- Code comments follow `~/.claude/CLAUDE.md`: explain *why* only; no narration, no future promises.
- **Always build both front-end bundles together**: `yarn build:all` (repo root builds every workspace; in
  `src/KCC.Web` it builds client + SSR).
- The unattended admin password is **at least 10 characters**. Never write real credentials into a file.
- Test commands (TUnit runs as an executable; `dotnet test` on one project reports "Zero tests ran"):
  - unit: `dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj`
  - integration: `dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj`
  - E2E: `dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj` (needs `dotnet build` of `src/KCC.Web` and
    `yarn build:all` first)
  - one class: append `-- --treenode-filter "/*/*/<ClassName>/*"`
  - front end: `cd src/KCC.Web && yarn test`, `yarn type-check`
- Paths are relative to the repo root `/Users/twinright/Repos/Kitchen-Command-Center`. The dev site runs on
  `https://localhost:58671` (launch profile `Local`).

## Findings from Phase 0

Measured on `replatform` at the end of Phase 0 (2026-09-23). The task text below already reflects them; this list
says why.

- **Build warnings (Task 1).** `dotnet build KitchenCommandCenter.sln` starts the phase with 372 NuGet advisory
  warnings: NU1901/NU1902 for `Magick.NET-Q8-AnyCPU` 14.14.0 (the override pin) and NU1902 for `AngleSharp` 0.17.1,
  which arrives only through `Kentico.Xperience.Core` and its `HtmlSanitizer`. `WarningsNotAsErrors` keeps them from
  failing the build. They last until Task 2 deletes the Kentico packages and the Magick.NET override, so Task 1's
  build expects those advisories and nothing else. From Task 2 on, every warning is new and gets looked at.
- **Binary files in git (Tasks 2, 9).** `.gitattributes` held only `* text eol=lf` plus the two font types, so git
  stripped the CR from every CRLF byte pair in committed binaries. The tracked PNGs (their signature reads
  `89504e47 0a1a0a…`) and the two test-image WebPs under `App_Data/CIRepository` and `assets/contentitems` are
  damaged. Tasks 2 and 9 delete them. The two logo WebPs that Task 9 moves (`5e05004d-….webp` → `logo-on-light.webp`,
  `af13c859-….webp` → `logo-on-dark.webp`) are intact (RIFF size equals file size, no CRLF). Image types are now
  marked `binary`, so a new binary must never need a `-text` workaround.
- **The capture tool already takes `--only` (Task 15).** Phase 0 grew `tests/KCC.ReferenceCapture` beyond its plan
  listing. It waits for network idle, fails the run when a page answers with the wrong status (200, or 404 for
  `not-found`) or lands on a different path, hides the dev overlays (MiniProfiler's `.mp-results` and the Vue
  DevTools button), shoots cook mode at viewport size, and takes `--only <names>`. When no selected capture is signed
  in, it skips sign-in. Task 15 therefore no longer edits `Program.cs`.
- **The Playwright pin decides the browsers (Task 14).** Phase 0 pinned `Microsoft.Playwright` 1.58.0 centrally,
  with transitive pinning on. That pin, not TUnit.Playwright's version, now decides which browser build E2E needs, so
  the CI browser cache is keyed on it.
- **UI strings (Task 6).**
  - The export holds 216 keys, identical to the CI repository's.
  - `Custom_ResourceStringTranslation` has 0 rows, so dropping languages loses nothing.
  - Three values are empty and render as their key today: `Theme.SwitchToDark` and `Theme.SwitchToLight` (the theme
    toggle's labels, so screen readers hear the raw keys) and `VariantDetail.SaturatedFat`. Task 6's provider keeps
    that behaviour.
  - `RecipeSearch.NoRecipesHint` reads "No Recipes Hint", which is a placeholder.
  - Filling in any of these values is the owner's call, not this phase's.
- **Reference set.** `docs/replatform/reference/xperience-final/` holds 48 PNGs (2 ramps × 2 widths × 12 pages). Its
  `NOTES.md` lists the differences to expect against the new site. The Xperience pages also log a hydration-mismatch
  warning (`href="/"` against `"~/"`), which should disappear with Task 8's `stripTilde` removal.

## Not in Phase 1

Recipes, variants, search, members, contributions, the wizards, the account pages, backoffice extensions and home
blocks belong to later phases. Their code stays on disk but is excluded from compilation; their nodes exist in the
baseline tree and render 404 until their phase lands.

## File map

| Path | Change | Task |
|---|---|---|
| `src/KCC.Web/Features/{Sections,Pages/PageBuilder,Components/Personalization,AdminHomePage}/` | Delete | 1 |
| `src/KCC.Web/Features/Widgets/**/*.cs`, `**/*.cshtml` | Delete (the three `*.Component.vue` stay) | 1 |
| `src/KCC.Web/Features/{PageBuilderMount.ts,Ssr/PreviewJsonUrlSyncMiddleware.cs,…}` | Delete | 1 |
| `src/KCC.ResourceStrings/`, `src/KCC.Web/App_Data/`, `src/KCC.Web/Features/Models/Generated/` (Kentico) | Delete | 2 |
| `src/KCC.Web/{Program.cs,KCC.Web.csproj,appsettings*.json,Properties/launchSettings.json}` | Rewrite | 2 |
| `src/KCC.Web/Features/Components/Breadcrumbs/BreadcrumbLink.cs` | Create (moved record) | 2 |
| `src/KCC.Web/uSync/v17/**` | Generated, committed | 2, 5, 6, 7 |
| `tests/KCC.UnitTests/Features/Configuration/SqliteConnectionStringTests.cs` | Create | 2 |
| `tests/KCC.IntegrationTests/Config/UmbracoSite.cs` | Create | 3 |
| `tests/KCC.E2ETests/Config/{SiteProcess.cs,RepoPaths.cs,SiteDatabase.cs}` | Create | 4, 6 |
| `src/KCC.Web/Features/DevTools/Baseline/*` | Create; bootstraps removed in 7 | 5–7 |
| `src/KCC.Web/Features/Models/Generated/*.generated.cs` | Generated by ModelsBuilder, committed | 5 |
| `src/KCC.Web/Features/Dictionary/{IResourceStringProvider.cs,DictionaryResourceStringProvider.cs}` | Create | 6 |
| `src/KCC.Web/Features/Ssr/VueSsrService.cs` | Modify (cache key) | 8 |
| `src/KCC.Web/Features/Components/ResourceStrings/ResourceString.Component.vue`, 7 `.vue` files, `Utilities/StringExtensions.ts` | Modify | 8 |
| `src/KCC.Web/Features/Pages/Shared/Layout.cshtml`, `Features/_ViewImports.cshtml` | Rewrite | 9 |
| `src/KCC.Web/Features/Components/Header/*` | Rewrite; logos move in | 9 |
| `src/KCC.Web/Features/Pages/Home/*`, `Pages/Shared/PageMetadata.cs` | Rewrite / create | 10 |
| `src/KCC.Web/Features/Pages/Error/*` | Rewrite / create | 11 |
| `src/KCC.Web/Features/Sitemap/*` | Rewrite / create | 12 |
| `tests/KCC.E2ETests/Features/Chrome/*` | Create | 13 |
| `.github/workflows/build-and-test.yml` | Rewrite | 14 |
| `README.md`, `CLAUDE.md` | Modify | 15 |

---

### Task 1: Remove what does not survive the move

Everything here is deleted for good (spec §6.2, §16). Xperience still builds at the end of this task.

**Files:**
- Delete: `src/KCC.Web/Features/Sections/`, `src/KCC.Web/Features/Pages/PageBuilder/`,
  `src/KCC.Web/Features/Components/Personalization/`, `src/KCC.Web/Features/AdminHomePage/`
- Delete: every `*.cs` and `*.cshtml` under `src/KCC.Web/Features/Widgets/` (keep `Card/Card.Component.vue`,
  `Hero/SmallHero.Component.vue`, `Stacker/Stacker.Component.vue` — pages and Phase 6 blocks use them)
- Delete: `src/KCC.Web/Features/Helpers/EditableAreaHelpers.cs`, `Features/TagHelpers/PageBuilderMountPreloadTagHelper.cs`,
  `Features/TagHelpers/PageBuilderMountScriptTagHelper.cs`, `Features/TagHelpers/ImageTagHelper.cs`,
  `Features/PageBuilderMount.ts`, `Features/Ssr/PreviewJsonUrlSyncMiddleware.cs`,
  `Features/Models/Constants/WidgetConstants.cs`, `Features/Tailwind/TailwindStyleAttribute.cs`,
  `Features/Providers/EnumDropDownOptionsProvider.cs`
- Delete: `tests/KCC.UnitTests/Features/AdminHomePage/`, `tests/KCC.UnitTests/Features/Sections/`,
  `tests/KCC.UnitTests/Features/TagHelpers/`, `tests/KCC.UnitTests/Features/Ssr/PreviewJsonUrlSyncMiddlewareTests.cs`
- Modify: `src/KCC.Web/Program.cs`, `src/KCC.Web/Features/Pages/Shared/Layout.cshtml`,
  `src/KCC.Web/Features/Pages/Home/Index.cshtml`, `src/KCC.Web/Features/_ViewImports.cshtml`,
  `src/KCC.Web/Features/Models/Common/SsrHtmlContent.cs`, `src/KCC.Web/vite.config.ts`,
  `src/KCC.Web/KCC.Web.csproj`, `src/KCC.Web/appsettings.json`, `Directory.Packages.props`

- [ ] **Step 1: Delete the files**

```bash
git rm -r -q src/KCC.Web/Features/Sections src/KCC.Web/Features/Pages/PageBuilder \
  src/KCC.Web/Features/Components/Personalization src/KCC.Web/Features/AdminHomePage
git ls-files 'src/KCC.Web/Features/Widgets/*.cs' 'src/KCC.Web/Features/Widgets/*.cshtml' | xargs git rm -q
git rm -q src/KCC.Web/Features/Helpers/EditableAreaHelpers.cs \
  src/KCC.Web/Features/TagHelpers/PageBuilderMountPreloadTagHelper.cs \
  src/KCC.Web/Features/TagHelpers/PageBuilderMountScriptTagHelper.cs \
  src/KCC.Web/Features/TagHelpers/ImageTagHelper.cs \
  src/KCC.Web/Features/PageBuilderMount.ts \
  src/KCC.Web/Features/Ssr/PreviewJsonUrlSyncMiddleware.cs \
  src/KCC.Web/Features/Models/Constants/WidgetConstants.cs \
  src/KCC.Web/Features/Tailwind/TailwindStyleAttribute.cs \
  src/KCC.Web/Features/Providers/EnumDropDownOptionsProvider.cs
git rm -r -q tests/KCC.UnitTests/Features/AdminHomePage tests/KCC.UnitTests/Features/Sections \
  tests/KCC.UnitTests/Features/TagHelpers
git rm -q tests/KCC.UnitTests/Features/Ssr/PreviewJsonUrlSyncMiddlewareTests.cs
git ls-files src/KCC.Web/Features/Widgets
```

Expected: the last command lists exactly the three `*.Component.vue` files.

- [ ] **Step 2: Remove their registrations from `Program.cs`**

In `src/KCC.Web/Program.cs`:
1. Delete the usings `using KCC.Web.Features.AdminHomePage;`, `using Kentico.Activities.Web.Mvc;`,
   `using Kentico.PageBuilder.Web.Mvc;`, `using Kentico.Xperience.ComponentRegistry;`,
   `using Kentico.Xperience.ManagementApi;`.
2. Inside `builder.Services.AddKentico(features => { … })`, delete `features.UseActivityTracking();` and the whole
   `features.UsePageBuilder(new() { … });` statement, leaving only `features.UseWebPageRouting(…)`.
3. Delete the whole `if (builder.Environment.IsDevelopment()) { … }` block that calls `AddKenticoMiniProfiler`,
   `AddKenticoManagementApi`, `AddComponentRegistry` and `AddMcpServer`.
4. Delete `app.UseAdminHomePageRedirect();`, the two comment lines above `app.UseMiddleware<PreviewJsonUrlSyncMiddleware>();`
   and that line itself.
5. Replace

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseMiniProfiler();
}
else
{
    app.UseExceptionHandler("/error");
}
```

with

```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
}
```

- [ ] **Step 3: Remove Page Builder from the views**

- `src/KCC.Web/Features/Pages/Shared/Layout.cshtml`: delete the four lines `<page-builder-mount-preload />`,
  `<page-builder-styles />`, `<page-builder-mount-script />` and `<page-builder-scripts />`.
- `src/KCC.Web/Features/Pages/Home/Index.cshtml`: delete the `<editable-area … />` line, leaving the `@using` and
  `@model` lines.
- `src/KCC.Web/Features/_ViewImports.cshtml`: delete `@using Kentico.PageBuilder.Web.Mvc;` and
  `@using Kentico.Activities.Web.Mvc;`.

- [ ] **Step 4: Drop the stale comment in `SsrHtmlContent.cs`**

In `src/KCC.Web/Features/Models/Common/SsrHtmlContent.cs`, delete the three comment lines above
`internal const string ServerContentScriptOpen` (they describe `PreviewJsonUrlSyncMiddleware`, now gone). Keep the
constant.

- [ ] **Step 5: Drop the Vite entries**

In `src/KCC.Web/vite.config.ts`, delete these two lines from `rollupOptions.input`:

```ts
              pageBuilderMount: resolve(__dirname, 'Features/PageBuilderMount.ts'),
              adminHomeRedirect: resolve(__dirname, 'Features/AdminHomePage/admin-home-redirect.ts'),
```

- [ ] **Step 6: Drop the packages**

In `src/KCC.Web/KCC.Web.csproj`, delete the `PackageReference` lines for `Kentico.Xperience.AzureStorage`,
`Kentico.Xperience.ComponentRegistry`, `Kentico.Xperience.ComponentRegistry.Admin`,
`Kentico.Xperience.ComponentRegistry.MCP`, `Kentico.Xperience.ManagementApi` and `Kentico.Xperience.MiniProfiler`.

Confirm nothing references the user-secrets package directly:

```bash
grep -rn 'Include="Microsoft.Extensions.Configuration.UserSecrets"' --include='*.csproj' . || echo none
```

Expected: `none`.

In `Directory.Packages.props`, delete the `PackageVersion` lines for: `Kentico.Xperience.AzureStorage`,
`Kentico.Xperience.ComponentRegistry`, `Kentico.Xperience.ComponentRegistry.Admin`,
`Kentico.Xperience.ComponentRegistry.MCP`, `Kentico.Xperience.Core.Tests`, `Kentico.Xperience.ManagementApi`,
`Kentico.Xperience.MiniProfiler`, `LigerShark.WebOptimizer.Core`, `Microsoft.Extensions.Configuration.UserSecrets`,
`Microsoft.Extensions.FileProviders.Embedded`, `NUnit`, `NUnit.Analyzers`, `NUnit3TestAdapter`.

In `src/KCC.Web/appsettings.json`, delete the `"KenticoManagementApi": { "Secret": "" },` entry.

- [ ] **Step 7: Build and test**

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj
(cd src/KCC.Web && yarn test && yarn build:all)
```

Expected: `Build succeeded` with 0 errors and no warnings except the NU1901/NU1902 advisories for
`Magick.NET-Q8-AnyCPU` and `AngleSharp` (see Findings from Phase 0; Task 2 removes both); every remaining unit test
passes; Vitest passes; both bundles build. If
the build reports an unresolved reference to a deleted type, it is a leftover registration — remove it the same way.

- [ ] **Step 8: Commit**

```bash
git add -A src/KCC.Web tests/KCC.UnitTests Directory.Packages.props
git commit -m "Remove Page Builder, Personalization and the Admin Home"
```

---

### Task 2: Replace the Xperience host with Umbraco

Kentico and Umbraco cannot share a host, so this task swaps the platform in one step. At the end, the site boots
into Umbraco on SQLite with the backoffice reachable; no page renders yet, because every view and every
Kentico-bound slice is excluded until its task or phase ports it.

**Files:**
- Delete: `src/KCC.ResourceStrings/`, `src/KCC.Web/App_Data/`, `src/KCC.Web/Features/Models/Generated/`,
  `src/KCC.Web/.config/`, `src/KCC.Web/appsettings.CI.json`,
  `src/KCC.Web/Features/Extensions/{ContentRetrieverExtensions,WebPageFieldsSourceExtensions,HttpContextExtensions}.cs`,
  `src/KCC.Web/Features/Models/Constants/XperienceConstants.cs`,
  `tests/KCC.UnitTests/Features/ResourceStringEditing/`, `tests/KCC.UnitTests/Features/Pages/Shared/PageMappingExtensionsTests.cs`,
  `tests/KCC.IntegrationTests/Config/`, `tests/KCC.IntegrationTests/appsettings.json`,
  `tests/KCC.IntegrationTests/appsettings.CI.json`
- Create: `src/KCC.Web/Features/Components/Breadcrumbs/BreadcrumbLink.cs`,
  `tests/KCC.UnitTests/Features/Configuration/SqliteConnectionStringTests.cs`
- Rewrite: `src/KCC.Web/Program.cs`, `src/KCC.Web/KCC.Web.csproj`, `src/KCC.Web/appsettings.json`,
  `src/KCC.Web/appsettings.Development.json`, `src/KCC.Web/Properties/launchSettings.json`,
  `tests/KCC.UnitTests/KCC.UnitTests.csproj`, `tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj`
- Modify: `Directory.Packages.props`, `KitchenCommandCenter.sln`, `package.json`, `yarn.lock`,
  `src/KCC.Web/vite.config.ts`, `src/KCC.Web/Features/Extensions/UrlHelperExtensions.cs`,
  `src/KCC.Web/Features/Components/Breadcrumbs/BreadcrumbService.cs`, `.gitignore`

**Interfaces:**
- Produces: a booting Umbraco host; configuration keys `DataProtection:KeysDirectory` (optional; defaults to
  `umbraco/Data/keys` under the content root) and `VueSsr:Enabled`/`VueSsr:BaseUrl` (unchanged); the
  `Unported slices` item group in `KCC.Web.csproj` and `KCC.UnitTests.csproj`, whose lines later tasks and phases
  delete.

- [ ] **Step 1: Pick the package versions**

```bash
dotnet package search Umbraco.Cms --exact-match
dotnet package search uSync --exact-match
dotnet package search Microsoft.AspNetCore.Mvc.Testing --exact-match
```

Each prints a table of every published version.

Choose: `Umbraco.Cms` → **17.8.0** if listed, otherwise **17.7.0** (never 18.x); `uSync` → **17.4.2** (or the newest
17.x); `Microsoft.AspNetCore.Mvc.Testing` → the newest **10.0.x**. Record the three versions in the task report.

- [ ] **Step 2: Update `Directory.Packages.props`**

Delete every remaining `Kentico.*` `PackageVersion` line and the `Magick.NET-Q8-AnyCPU` override with its comment.
Add, keeping the list alphabetical:

```xml
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.x" />
    <PackageVersion Include="Umbraco.Cms" Version="17.7.0" />
    <PackageVersion Include="uSync" Version="17.4.2" />
```

(replacing the versions with the ones chosen in Step 1).

- [ ] **Step 3: Remove the Kentico-only projects and files**

```bash
dotnet sln KitchenCommandCenter.sln remove src/KCC.ResourceStrings/KCC.ResourceStrings.csproj \
  src/KCC.Admin/KCC.Admin.csproj src/KCC.Contributions/KCC.Contributions.csproj
git rm -r -q src/KCC.ResourceStrings src/KCC.Web/Features/Models/Generated src/KCC.Web/.config \
  tests/KCC.UnitTests/Features/ResourceStringEditing tests/KCC.IntegrationTests/Config
git rm -r -q src/KCC.Web/App_Data
git rm -q src/KCC.Web/appsettings.CI.json \
  src/KCC.Web/Features/Extensions/ContentRetrieverExtensions.cs \
  src/KCC.Web/Features/Extensions/WebPageFieldsSourceExtensions.cs \
  src/KCC.Web/Features/Extensions/HttpContextExtensions.cs \
  src/KCC.Web/Features/Models/Constants/XperienceConstants.cs \
  tests/KCC.UnitTests/Features/Pages/Shared/PageMappingExtensionsTests.cs \
  tests/KCC.IntegrationTests/appsettings.json tests/KCC.IntegrationTests/appsettings.CI.json
rm -rf src/KCC.Web/App_Data
```

`KCC.Admin` and `KCC.Contributions` leave the solution but stay on disk: Phases 2 and 5 rewrite them. The final
`rm -rf` removes the untracked Kentico runtime folders (the Lucene index and Azure caches), which are regenerable.

- [ ] **Step 4: Move `BreadcrumbLink` into its own file**

`BasePageViewModel` compiles now, but `BreadcrumbService.cs` (Kentico-bound) is excluded until Phase 2.

Create `src/KCC.Web/Features/Components/Breadcrumbs/BreadcrumbLink.cs`:

```csharp
namespace KCC.Web.Features.Components.Breadcrumbs;

public record BreadcrumbLink
(
    string LinkText,
    string Url,
    int? ParentId = null,
    int? WebPageItemId = null
);
```

Then delete the identical `public record BreadcrumbLink (…);` declaration at the end of
`src/KCC.Web/Features/Components/Breadcrumbs/BreadcrumbService.cs`.

- [ ] **Step 5: Strip the Kentico lookup from `UrlHelperExtensions.cs`**

In `src/KCC.Web/Features/Extensions/UrlHelperExtensions.cs`, delete the two `HomePage` methods and the usings
`CMS.Core`, `CMS.Websites` and `Kentico.Content.Web.Mvc`. `ActionFor` and its helpers stay unchanged.

- [ ] **Step 6: Rewrite `src/KCC.Web/KCC.Web.csproj`**

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project Sdk="Microsoft.NET.Sdk.Web">

    <PropertyGroup>
        <ImplicitUsings>enable</ImplicitUsings>
        <TargetFramework>net10.0</TargetFramework>
        <RuntimeIdentifiers>win-x64;linux-x64;linux-arm64</RuntimeIdentifiers>
        <CodeAnalysisRuleSet>..\..\KCCStandardRules.ruleset</CodeAnalysisRuleSet>
        <GenerateDocumentationFile>true</GenerateDocumentationFile>
        <NoWarn>$(NoWarn);AD0001</NoWarn>
        <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
        <WarningsNotAsErrors>NU1608;NU1901;NU1902;NU1903</WarningsNotAsErrors>
        <UserSecretsId>85272aa4-641c-4fe4-b1e7-977f7decf9dc</UserSecretsId>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Anthropic" />
        <PackageReference Include="Microsoft.Extensions.Http.Polly" />
        <PackageReference Include="RobotsTxtCore" />
        <PackageReference Include="SimpleMvcSitemap" />
        <PackageReference Include="StyleCop.Analyzers">
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
            <PrivateAssets>all</PrivateAssets>
        </PackageReference>
        <PackageReference Include="Umbraco.Cms" />
        <PackageReference Include="uSync" />
        <PackageReference Include="Vite.AspNetCore" />
    </ItemGroup>

    <ItemGroup Label="Unported slices">
        <Compile Remove="Features/Api/**" />
        <Compile Remove="Features/Components/Breadcrumbs/BreadcrumbService.cs" />
        <Compile Remove="Features/Components/Header/HeaderViewComponent.cs" />
        <Compile Remove="Features/Components/Header/HeaderViewModel.cs" />
        <Compile Remove="Features/DevTools/RecipeSeed/**" />
        <Compile Remove="Features/Models/Api/**" />
        <Compile Remove="Features/Models/Common/KCCApplicationUser.cs" />
        <Compile Remove="Features/Pages/Account/**" />
        <Compile Remove="Features/Pages/AddVariant/**" />
        <Compile Remove="Features/Pages/CreateRecipe/**" />
        <Compile Remove="Features/Pages/Error/ErrorController.cs" />
        <Compile Remove="Features/Pages/Home/HomeController.cs" />
        <Compile Remove="Features/Pages/RecipeDetail/**" />
        <Compile Remove="Features/Pages/RecipeSearch/**" />
        <Compile Remove="Features/Pages/Shared/PageMappingExtensions.cs" />
        <Compile Remove="Features/Pages/VariantDetail/**" />
        <Compile Remove="Features/Providers/**" />
        <Compile Remove="Features/Search/**" />
        <Compile Remove="Features/Sitemap/**" />
        <Content Remove="Features/_ViewImports.cshtml" />
        <Content Remove="Features/Components/Footer/Footer.cshtml" />
        <Content Remove="Features/Components/Header/Header.cshtml" />
        <Content Remove="Features/Pages/**/*.cshtml" />
    </ItemGroup>

    <ItemGroup>
        <InternalsVisibleTo Include="KCC.UnitTests" />
        <InternalsVisibleTo Include="KCC.IntegrationTests" />
    </ItemGroup>

</Project>
```

`linux-arm64` is for the Pi image in Phase 7. Each later task deletes its own lines from the `Unported slices`
group; when the group is empty it goes too.

- [ ] **Step 7: Rewrite `src/KCC.Web/Program.cs`**

```csharp
using KCC.Web.Features.Ssr;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .Build();

builder.Services.AddMemoryCache();
builder.Services.AddVueSsr(builder.Configuration);

// Umbraco never persists data-protection keys, so without this every restart signs everyone out and
// invalidates the anti-forgery tokens in open forms.
builder.Services.AddDataProtection()
    .SetApplicationName("kcc")
    .PersistKeysToFileSystem(new DirectoryInfo(
        builder.Configuration["DataProtection:KeysDirectory"]
            ?? Path.Combine(builder.Environment.ContentRootPath, "umbraco", "Data", "keys")));

var app = builder.Build();

await app.BootUmbracoAsync();

app.UseVueSsr();

app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

await app.RunAsync();
```

`UseVueSsr()` (development only: web sockets and the Vite dev-server proxy) sits before `UseUmbraco()` so Vite's
asset requests never reach Umbraco's routing. Attribute-routed controllers are mapped by the backoffice's own
`MapControllers()`, so nothing else is needed for `[Route]` controllers.

- [ ] **Step 8: Write the SQLite guard test (it fails until Step 9)**

Create `tests/KCC.UnitTests/Features/Configuration/SqliteConnectionStringTests.cs`:

```csharp
using System.Text.Json;

namespace KCC.UnitTests.Features.Configuration;

public class SqliteConnectionStringTests
{
    [Test]
    public async Task ConnectionString_NeverUsesSharedCache()
    {
        var connectionString = AppSettings().GetProperty("ConnectionStrings").GetProperty("umbracoDbDSN").GetString();

        _ = await Assert.That(connectionString.Contains("Cache=Shared", StringComparison.OrdinalIgnoreCase)).IsFalse();
        _ = await Assert.That(connectionString.Contains("Cache=Private", StringComparison.OrdinalIgnoreCase)).IsTrue();
    }

    [Test]
    public async Task ConnectionString_UsesTheSqliteProvider()
    {
        var provider = AppSettings().GetProperty("ConnectionStrings").GetProperty("umbracoDbDSN_ProviderName").GetString();

        _ = await Assert.That(provider).IsEqualTo("Microsoft.Data.Sqlite");
    }

    [Test]
    public async Task WriteLockWait_IsThirtySeconds()
    {
        var timeout = AppSettings().GetProperty("Umbraco").GetProperty("CMS").GetProperty("Global")
            .GetProperty("DistributedLockingWriteLockDefaultTimeout").GetString();

        _ = await Assert.That(timeout).IsEqualTo("00:00:30");
    }

    private static JsonElement AppSettings()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "KitchenCommandCenter.sln")))
        {
            directory = directory.Parent;
        }

        var path = Path.Combine(directory!.FullName, "src", "KCC.Web", "appsettings.json");
        return JsonDocument.Parse(File.ReadAllText(path)).RootElement;
    }
}
```

- [ ] **Step 9: Rewrite the configuration files**

`src/KCC.Web/appsettings.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "System": "Warning"
      }
    }
  },
  "ConnectionStrings": {
    "umbracoDbDSN": "Data Source=|DataDirectory|/Umbraco.sqlite.db;Cache=Private;Foreign Keys=True;Pooling=True",
    "umbracoDbDSN_ProviderName": "Microsoft.Data.Sqlite"
  },
  "Umbraco": {
    "CMS": {
      "Global": {
        "DistributedLockingWriteLockDefaultTimeout": "00:00:30"
      },
      "Unattended": {
        "InstallUnattended": true,
        "UpgradeUnattended": true,
        "UnattendedTelemetryLevel": "Minimal"
      },
      "ModelsBuilder": {
        "ModelsMode": "Nothing",
        "ModelsNamespace": "KCC.Web.Features.Models.Generated",
        "ModelsDirectory": "~/Features/Models/Generated"
      }
    }
  },
  "uSync": {
    "Settings": {
      "RootFolder": "uSync/v17/",
      "ImportAtStartup": "Settings",
      "ImportOnFirstBoot": true,
      "FirstBootGroup": "All",
      "ExportAtStartup": "None",
      "ExportOnSave": "None",
      "EnableHistory": false
    },
    "Sets": {
      "Default": {
        "Handlers": {
          "DictionaryHandler": {
            "Group": "Settings",
            "Settings": {
              "CreateOnly": true
            }
          }
        }
      }
    }
  },
  "AllowedHosts": "*",
  "Anthropic": {
    "ApiKey": "",
    "Model": "claude-haiku-4-5"
  },
  "RobotsTxtDenyAll": true,
  "Vite": {
    "Base": "/",
    "PackageManager": "yarn",
    "PackageDirectory": "../KCC.Web"
  },
  "VueSsr": {
    "Enabled": true,
    "BaseUrl": "http://localhost:3001"
  }
}
```

Moving `DictionaryHandler` into the `Settings` group puts dictionary items into every startup import, and
`CreateOnly` limits that import to keys missing from the database — so live edits survive and new keys still
arrive (spec §12).

`src/KCC.Web/appsettings.Development.json`:

```json
{
  "Umbraco": {
    "CMS": {
      "ModelsBuilder": {
        "ModelsMode": "SourceCodeManual"
      }
    }
  },
  "uSync": {
    "Settings": {
      "ExportOnSave": "Settings"
    }
  },
  "Vite": {
    "Server": {
      "AutoRun": true,
      "Port": 5173,
      "Https": true
    }
  }
}
```

`src/KCC.Web/Properties/launchSettings.json` — the `CI` profile goes (the E2E fixture starts the site itself from
Task 4):

```json
{
  "iisSettings": {
    "windowsAuthentication": false,
    "anonymousAuthentication": true,
    "iisExpress": {
      "applicationUrl": "https://localhost:58671;http://localhost:58672",
      "sslPort": 58671
    }
  },
  "profiles": {
    "IIS Express": {
      "commandName": "IISExpress",
      "launchBrowser": false,
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ASPNETCORE_VITE": "true"
      }
    },
    "Local": {
      "commandName": "Project",
      "launchBrowser": false,
      "applicationUrl": "https://localhost:58671;http://localhost:58672",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ASPNETCORE_VITE": "true"
      }
    }
  }
}
```

- [ ] **Step 10: Retire the resource-strings workspace**

In the root `package.json`, remove `"src/KCC.ResourceStrings/Client"` from `workspaces`, delete the
`build:resource-strings` script, and change `build:all` to
`"yarn build:admin && yarn build:contributions && yarn build:web"`.

In `src/KCC.Web/vite.config.ts`, delete the `resourceStringEditor: resolve(…)` entry (all four lines) from
`rollupOptions.input`.

```bash
yarn install
```

Expected: completes; `yarn.lock` loses the `@kcc/resource-strings` workspace entries.

- [ ] **Step 11: Point the test projects away from Kentico**

`tests/KCC.UnitTests/KCC.UnitTests.csproj`:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <IsPackable>false</IsPackable>
        <IsTestProject>true</IsTestProject>
        <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
        <WarningsNotAsErrors>NU1608;NU1901;NU1902;NU1903</WarningsNotAsErrors>
    </PropertyGroup>
    <ItemGroup>
        <FrameworkReference Include="Microsoft.AspNetCore.App" />
    </ItemGroup>
    <ItemGroup>
        <PackageReference Include="Microsoft.NET.Test.Sdk" />
        <PackageReference Include="Moq" />
        <PackageReference Include="TUnit" />
    </ItemGroup>
    <ItemGroup Label="Unported slices">
        <Compile Remove="Admin/**" />
        <Compile Remove="Features/Api/**" />
        <Compile Remove="Features/Contributions/**" />
        <Compile Remove="Features/Pages/Account/**" />
        <Compile Remove="Features/Pages/RecipeDetail/**" />
        <Compile Remove="Features/Pages/VariantDetail/**" />
        <Compile Remove="Features/Providers/**" />
        <Compile Remove="Features/Search/**" />
    </ItemGroup>
    <ItemGroup>
        <ProjectReference Include="..\..\src\KCC.Web\KCC.Web.csproj" />
    </ItemGroup>
</Project>
```

`tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj` (Task 3 adds the test host):

```xml
<?xml version="1.0" encoding="UTF-8"?>
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
        <IsPackable>false</IsPackable>
        <IsTestProject>true</IsTestProject>
        <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
        <WarningsNotAsErrors>NU1608;NU1901;NU1902;NU1903</WarningsNotAsErrors>
    </PropertyGroup>
    <ItemGroup>
        <FrameworkReference Include="Microsoft.AspNetCore.App" />
    </ItemGroup>
    <ItemGroup>
        <PackageReference Include="Microsoft.NET.Test.Sdk" />
        <PackageReference Include="Moq" />
        <PackageReference Include="TUnit" />
    </ItemGroup>
    <ItemGroup Label="Unported slices">
        <Compile Remove="Features/Providers/**" />
    </ItemGroup>
    <ItemGroup>
        <ProjectReference Include="..\..\src\KCC.Web\KCC.Web.csproj" />
    </ItemGroup>
</Project>
```

- [ ] **Step 12: Ignore Umbraco's runtime output**

In `.gitignore`:
1. Delete the `# Kentico CI` block (`!**/*cms.user`).
2. Delete the `# Lucene search index runtime storage …` block (both lines).
3. Delete the two `src/KCC.ResourceStrings/Client/…` lines.
4. Append:

```gitignore
# Umbraco runtime data (the database, logs, keys, temp) and uploaded media
src/KCC.Web/umbraco/Data/
src/KCC.Web/umbraco/Logs/
src/KCC.Web/umbraco/models/
src/KCC.Web/wwwroot/media/
```

- [ ] **Step 13: Build**

```bash
dotnet build KitchenCommandCenter.sln
```

Expected: `Build succeeded`, 0 warnings, 0 errors. If a file outside the `Unported slices` group still references a
Kentico type, the fix is to exclude it in that group if a later phase ports it, or delete it if spec §6.2 lists it
as deleted — never to stub Kentico types.

- [ ] **Step 14: Run the guard and remaining unit tests**

```bash
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj
```

Expected: 9 passed (6 `SsrHtmlContentTests`, 3 `SqliteConnectionStringTests`), 0 failed.

- [ ] **Step 15: Set the unattended admin account (ask the owner)**

Ask the owner for the backoffice admin's name, email and a password of at least 10 characters to keep in 1Password,
then:

```bash
cd src/KCC.Web
dotnet user-secrets set "Umbraco:CMS:Unattended:UnattendedUserName" "<name>"
dotnet user-secrets set "Umbraco:CMS:Unattended:UnattendedUserEmail" "<email>"
dotnet user-secrets set "Umbraco:CMS:Unattended:UnattendedUserPassword" "<password>"
cd ../..
```

Leave the old `CMSConnectionString` / `CMSHashStringSalt` secrets in place: the `xperience-final` reference worktree
shares this user-secrets ID.

- [ ] **Step 16: Boot the site once**

```bash
cd src/KCC.Web
dotnet run --launch-profile Local
```

In a second terminal, once the log shows `Now listening on: https://localhost:58671`:

```bash
curl -sk https://localhost:58671/umbraco/management/api/v1/server/status
sqlite3 src/KCC.Web/umbraco/Data/Umbraco.sqlite.db 'PRAGMA journal_mode;'
ls src/KCC.Web/uSync/v17
```

Expected: `{"serverStatus":"Run"}`; `wal`; and folders such as `DataTypes`, `Languages`, `MediaTypes`,
`MemberTypes` — on its first boot with export-on-save on and no uSync folder, uSync exports the clean install's
schema. Open `https://localhost:58671/umbraco` and sign in with the admin account. Stop the site (Ctrl+C).

- [ ] **Step 17: Check for untracked generated files**

```bash
git status --porcelain --untracked-files=all | grep -v '^ D\|^D ' | head -40
```

Expected: only intended new files (`BreadcrumbLink.cs`, the guard test, `src/KCC.Web/uSync/v17/**`). If Umbraco's
build dropped schema files into the project root (for example `appsettings-schema.Umbraco.Cms.json` or
`umbraco-package-schema.json`), add them to the Umbraco block in `.gitignore`.

- [ ] **Step 18: Front end still builds and passes**

```bash
yarn build:all
(cd src/KCC.Web && yarn test)
```

Expected: every workspace builds; Vitest passes.

- [ ] **Step 19: Commit**

```bash
git add -A .gitignore Directory.Packages.props KitchenCommandCenter.sln package.json yarn.lock src tests
git commit -m "Replace the Xperience Host with Umbraco 17"
```

---

### Task 3: An integration test host

One Umbraco host per test session, on a temporary SQLite file with a real unattended install and a real uSync first
boot. Settings go in through `UseSetting`, because Umbraco reads several of them before `WebApplicationFactory`'s
configuration hooks apply.

**Files:**
- Modify: `tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj`
- Create: `tests/KCC.IntegrationTests/Config/UmbracoSite.cs`
- Create: `tests/KCC.IntegrationTests/Features/Hosting/BootTests.cs`

**Interfaces:**
- Produces: `KCC.IntegrationTests.Config.UmbracoSite : WebApplicationFactory<Program>, IAsyncInitializer` with
  `string DatabasePath { get; }`, used as
  `[ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)] public UmbracoSite Site { get; init; } = null!;`.
  SSR is disabled inside it (`VueSsr:Enabled=false`), so rendered pages carry the Vue template text in the
  `#server-content` JSON instead of server-rendered HTML.

- [ ] **Step 1: Reference the test server**

In `tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj`, add to the package `ItemGroup`:

```xml
        <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
```

- [ ] **Step 2: Write the failing boot test**

Create `tests/KCC.IntegrationTests/Features/Hosting/BootTests.cs`:

```csharp
using KCC.IntegrationTests.Config;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;

namespace KCC.IntegrationTests.Features.Hosting;

public class BootTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task FreshDatabase_ReachesTheRunLevel()
    {
        var state = Site.Services.GetRequiredService<IRuntimeState>();

        _ = await Assert.That(state.Level).IsEqualTo(RuntimeLevel.Run);
    }

    [Test]
    public async Task FreshDatabase_UsesWriteAheadLogging()
    {
        await using var connection = new SqliteConnection($"Data Source={Site.DatabasePath};Pooling=False");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode;";

        _ = await Assert.That((string?)await command.ExecuteScalarAsync()).IsEqualTo("wal");
    }
}
```

- [ ] **Step 3: Run it to confirm it fails**

```bash
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj
```

Expected: build error — `UmbracoSite` does not exist.

- [ ] **Step 4: Write the host**

Create `tests/KCC.IntegrationTests/Config/UmbracoSite.cs`:

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core.Interfaces;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;

namespace KCC.IntegrationTests.Config;

public sealed class UmbracoSite : WebApplicationFactory<Program>, IAsyncInitializer
{
    private string runDirectory = string.Empty;

    public string DatabasePath => Path.Combine(runDirectory, "Umbraco.sqlite.db");

    public async Task InitializeAsync()
    {
        RequireFrontEndBuild();
        runDirectory = Directory.CreateTempSubdirectory("kcc-it-").FullName;

        // The factory builds its host synchronously on first access; TUnit can touch it from parallel tests.
        await Task.Run(() => _ = Server);

        var state = Services.GetRequiredService<IRuntimeState>();
        for (var attempt = 0; state.Level == RuntimeLevel.Upgrading && attempt < 600; attempt++)
        {
            await Task.Delay(100);
        }

        if (state.Level != RuntimeLevel.Run)
        {
            throw new InvalidOperationException($"Umbraco stopped at {state.Level} ({state.Reason}).", state.BootFailedException);
        }
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        SqliteConnection.ClearAllPools();
        if (runDirectory.Length > 0)
        {
            try
            {
                Directory.Delete(runDirectory, recursive: true);
            }
            catch (IOException)
            {
                // A log file can still be flushing; the temp folder is disposable either way.
            }
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        foreach (var (key, value) in Settings())
        {
            builder.UseSetting(key, value);
        }
    }

    private static void RequireFrontEndBuild()
    {
        var manifest = Path.Combine(WebProjectDirectory(), "wwwroot", ".vite", "manifest.json");
        if (!File.Exists(manifest))
        {
            throw new InvalidOperationException("Integration tests render real pages, which need the Vite manifest. Run `yarn build:all` in src/KCC.Web first.");
        }
    }

    private static string WebProjectDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "KitchenCommandCenter.sln")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(directory?.FullName ?? throw new InvalidOperationException("Repository root not found."), "src", "KCC.Web");
    }

    private Dictionary<string, string> Settings() => new()
    {
        ["ConnectionStrings:umbracoDbDSN"] = $"Data Source={DatabasePath};Cache=Private;Foreign Keys=True;Pooling=True",
        ["ConnectionStrings:umbracoDbDSN_ProviderName"] = "Microsoft.Data.Sqlite",
        ["Umbraco:CMS:Unattended:InstallUnattended"] = "true",
        ["Umbraco:CMS:Unattended:UpgradeUnattended"] = "true",
        ["Umbraco:CMS:Unattended:UnattendedUserName"] = "Integration Admin",
        ["Umbraco:CMS:Unattended:UnattendedUserEmail"] = "admin@example.test",
        ["Umbraco:CMS:Unattended:UnattendedUserPassword"] = "Integration-Passw0rd-2026",
        ["Umbraco:CMS:Unattended:UnattendedTelemetryLevel"] = "Minimal",
        ["Umbraco:CMS:ModelsBuilder:ModelsMode"] = "Nothing",
        ["Umbraco:CMS:Hosting:LocalTempStorageLocation"] = "EnvironmentTemp",
        ["Umbraco:CMS:Hosting:SiteName"] = Path.GetFileName(runDirectory),
        ["Umbraco:CMS:Examine:LuceneDirectoryFactory"] = "TempFileSystemDirectoryFactory",
        ["Umbraco:CMS:Logging:Directory"] = Path.Combine(runDirectory, "logs"),
        ["DataProtection:KeysDirectory"] = Path.Combine(runDirectory, "keys"),
        ["uSync:Settings:ExportOnSave"] = "None",
        ["VueSsr:Enabled"] = "false",
    };
}
```

- [ ] **Step 5: Build the front end once, then run the tests**

```bash
(cd src/KCC.Web && yarn build:all)
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj
```

Expected: 2 passed. If boot fails with a `ReflectionTypeLoadException` from Umbraco's type scanning of the test
output folder, add `["Umbraco:CMS:TypeFinder:AdditionalAssemblyExclusionEntries:0"] = "<the failing assembly prefix>"`
to `Settings()` and re-run. If it fails with `BootFailed`, the log is under the temp run folder's `logs/`.

- [ ] **Step 6: Commit**

```bash
git add tests/KCC.IntegrationTests Directory.Packages.props
git commit -m "Add an Umbraco Integration Test Host"
```

---

### Task 4: Start a fresh site for each E2E run

The E2E suite stops reusing whatever runs on port 58671. It starts the built `KCC.Web.dll` on a free port with a
temporary SQLite file, plus its own production-mode SSR process, and waits until Umbraco reports `Run`.

**Files:**
- Delete: `tests/KCC.E2ETests/Config/WebAppFixture.cs`, `tests/KCC.E2ETests/Config/Constants.cs`
- Create: `tests/KCC.E2ETests/Config/RepoPaths.cs`, `tests/KCC.E2ETests/Config/SiteProcess.cs`,
  `tests/KCC.E2ETests/Features/Hosting/SiteBootTests.cs`
- Modify: `tests/KCC.E2ETests/BasePageTest.cs`, `tests/KCC.E2ETests/KCC.E2ETests.csproj`

**Interfaces:**
- Produces: `KCC.E2ETests.Config.SiteProcess : IAsyncInitializer, IAsyncDisposable` with constructors `()` (with
  SSR) and `(bool withSsr)`, members `Uri BaseUrl`, `string DatabasePath`, `Task StartAsync()`, `Task StopAsync()`.
  `BasePageTests` injects a session-shared instance as `Site` and sets Playwright's `BaseURL` from it.

- [ ] **Step 1: Exclude the Xperience-era tests and delete the old fixture**

```bash
git rm -q tests/KCC.E2ETests/Config/WebAppFixture.cs tests/KCC.E2ETests/Config/Constants.cs
```

In `tests/KCC.E2ETests/KCC.E2ETests.csproj`, add before `</Project>`:

```xml
    <ItemGroup Label="Unported slices">
        <Compile Remove="Features/HomePage/**" />
        <Compile Remove="Features/RecipeRatings/**" />
        <Compile Remove="Features/RecipeSearch/**" />
        <Compile Remove="Features/VariantCookNotes/**" />
        <Compile Remove="Features/VariantCooked/**" />
        <Compile Remove="Features/VariantDetail/**" />
        <Compile Remove="Features/VariantReviews/**" />
    </ItemGroup>
```

- [ ] **Step 2: Write the failing boot test**

Create `tests/KCC.E2ETests/Features/Hosting/SiteBootTests.cs`:

```csharp
using System.Text.Json;
using KCC.E2ETests.Config;

namespace KCC.E2ETests.Features.Hosting;

public class SiteBootTests
{
    [ClassDataSource<SiteProcess>(Shared = SharedType.PerTestSession)]
    public SiteProcess Site { get; init; } = null!;

    [Test]
    public async Task FreshSite_ReportsTheRunLevel()
    {
        using var http = new HttpClient { BaseAddress = Site.BaseUrl };
        using var status = JsonDocument.Parse(await http.GetStringAsync("umbraco/management/api/v1/server/status"));

        _ = await Assert.That(status.RootElement.GetProperty("serverStatus").GetString()).IsEqualTo("Run");
    }
}
```

Run `dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj` — expected: build error, `SiteProcess` missing.

- [ ] **Step 3: Write `RepoPaths`**

Create `tests/KCC.E2ETests/Config/RepoPaths.cs`:

```csharp
namespace KCC.E2ETests.Config;

public static class RepoPaths
{
    public static string Root { get; } = FindRoot();

    public static string WebProject => Path.Combine(Root, "src", "KCC.Web");

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "KitchenCommandCenter.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
```

- [ ] **Step 4: Write `SiteProcess`**

Create `tests/KCC.E2ETests/Config/SiteProcess.cs`:

```csharp
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using TUnit.Core.Interfaces;

namespace KCC.E2ETests.Config;

/// <summary>A KCC.Web process on a free port with its own SQLite file, started the way production starts it.</summary>
public sealed class SiteProcess : IAsyncInitializer, IAsyncDisposable
{
    private static readonly string Configuration =
        typeof(SiteProcess).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration ?? "Debug";

    private readonly bool withSsr;
    private Process? site;
    private Process? ssr;
    private string runDirectory = string.Empty;
    private int ssrPort;

    public SiteProcess()
        : this(withSsr: true)
    {
    }

    public SiteProcess(bool withSsr) => this.withSsr = withSsr;

    public Uri BaseUrl { get; private set; } = null!;

    public string DatabasePath => Path.Combine(runDirectory, "Umbraco.sqlite.db");

    private string LogPath => Path.Combine(runDirectory, "site.log");

    public async Task InitializeAsync()
    {
        RequireBuildOutput();
        runDirectory = Directory.CreateTempSubdirectory("kcc-e2e-").FullName;
        Directory.CreateDirectory(Path.Combine(runDirectory, "tmp"));
        BaseUrl = new Uri($"http://127.0.0.1:{FreePort()}/");

        if (withSsr)
        {
            ssrPort = FreePort();
            await StartSsrAsync();
        }

        await StartAsync();
    }

    public async Task StartAsync()
    {
        var info = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = RepoPaths.WebProject,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        info.ArgumentList.Add(SiteDll);
        info.ArgumentList.Add("--urls");
        info.ArgumentList.Add(BaseUrl.GetLeftPart(UriPartial.Authority));
        foreach (var (key, value) in SiteEnvironment())
        {
            info.Environment[key] = value;
        }

        site = StartLogged(info);

        using var http = new HttpClient { BaseAddress = BaseUrl, Timeout = TimeSpan.FromSeconds(10) };
        for (var deadline = DateTime.UtcNow.AddMinutes(5); DateTime.UtcNow < deadline; await Task.Delay(1000))
        {
            if (site.HasExited)
            {
                throw new InvalidOperationException($"The site exited with {site.ExitCode}.{LogTail()}");
            }

            try
            {
                using var response = await http.GetAsync("umbraco/management/api/v1/server/status");
                if ((int)response.StatusCode >= 500)
                {
                    throw new InvalidOperationException($"Umbraco failed to boot.{LogTail()}");
                }

                using var status = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var level = status.RootElement.GetProperty("serverStatus").GetString();
                if (level == "Run")
                {
                    return;
                }

                if (level is "Install" or "BootFailed")
                {
                    throw new InvalidOperationException($"Umbraco stopped at {level}.{LogTail()}");
                }
            }
            catch (HttpRequestException)
            {
                // Kestrel is not listening yet.
            }
            catch (TaskCanceledException)
            {
                // The first request after a fresh install can be slow.
            }
        }

        throw new TimeoutException($"Umbraco did not reach Run within five minutes.{LogTail()}");
    }

    public async Task StopAsync()
    {
        if (site is { HasExited: false })
        {
            site.Kill(entireProcessTree: true);
            await site.WaitForExitAsync();
        }

        site?.Dispose();
        site = null;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        if (ssr is { HasExited: false })
        {
            ssr.Kill(entireProcessTree: true);
            await ssr.WaitForExitAsync();
        }

        ssr?.Dispose();
        if (runDirectory.Length > 0)
        {
            try
            {
                Directory.Delete(runDirectory, recursive: true);
            }
            catch (IOException)
            {
                // A just-killed process can hold a file handle for a moment; the folder is disposable.
            }
        }
    }

    private static string SiteDll => Path.Combine(RepoPaths.WebProject, "bin", Configuration, "net10.0", "KCC.Web.dll");

    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static void RequireBuildOutput()
    {
        if (!File.Exists(SiteDll))
        {
            throw new InvalidOperationException($"{SiteDll} is missing. Build the solution first (`dotnet build`).");
        }

        var web = RepoPaths.WebProject;
        if (!File.Exists(Path.Combine(web, "wwwroot", ".vite", "manifest.json")) || !File.Exists(Path.Combine(web, "wwwroot", "ssr", "Server.Entry.js")))
        {
            throw new InvalidOperationException("The front-end bundles are missing. Run `yarn build:all` in src/KCC.Web first.");
        }
    }

    private async Task StartSsrAsync()
    {
        var info = new ProcessStartInfo("node")
        {
            WorkingDirectory = RepoPaths.WebProject,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        info.ArgumentList.Add("Features/Ssr/Server.js");
        info.Environment["NODE_ENV"] = "production";
        info.Environment["SSR_PORT"] = ssrPort.ToString(CultureInfo.InvariantCulture);
        ssr = StartLogged(info);

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        for (var deadline = DateTime.UtcNow.AddSeconds(60); DateTime.UtcNow < deadline; await Task.Delay(500))
        {
            if (ssr.HasExited)
            {
                throw new InvalidOperationException($"The SSR service exited with {ssr.ExitCode}.{LogTail()}");
            }

            try
            {
                using var response = await http.GetAsync($"http://127.0.0.1:{ssrPort}/health");
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Not listening yet.
            }
        }

        throw new TimeoutException($"The SSR service did not answer /health within a minute.{LogTail()}");
    }

    private Process StartLogged(ProcessStartInfo info)
    {
        var writer = File.AppendText(LogPath);
        writer.AutoFlush = true;
        var log = TextWriter.Synchronized(writer);
        var process = Process.Start(info) ?? throw new InvalidOperationException($"Could not start {info.FileName}.");
        process.OutputDataReceived += (_, e) => log.WriteLine(e.Data);
        process.ErrorDataReceived += (_, e) => log.WriteLine(e.Data);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }

    private string LogTail()
    {
        try
        {
            var lines = File.ReadAllLines(LogPath);
            return $"{Environment.NewLine}Last log lines ({LogPath}):{Environment.NewLine}{string.Join(Environment.NewLine, lines.TakeLast(40))}";
        }
        catch (IOException)
        {
            return string.Empty;
        }
    }

    private Dictionary<string, string> SiteEnvironment()
    {
        var temp = Path.Combine(runDirectory, "tmp");
        return new()
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Testing",
            ["ConnectionStrings__umbracoDbDSN"] = $"Data Source={DatabasePath};Cache=Private;Foreign Keys=True;Pooling=True",
            ["ConnectionStrings__umbracoDbDSN_ProviderName"] = "Microsoft.Data.Sqlite",
            ["Umbraco__CMS__Unattended__InstallUnattended"] = "true",
            ["Umbraco__CMS__Unattended__UpgradeUnattended"] = "true",
            ["Umbraco__CMS__Unattended__UnattendedUserName"] = "E2E Admin",
            ["Umbraco__CMS__Unattended__UnattendedUserEmail"] = "admin@example.test",
            ["Umbraco__CMS__Unattended__UnattendedUserPassword"] = "E2E-Passw0rd-2026",
            ["Umbraco__CMS__Unattended__UnattendedTelemetryLevel"] = "Minimal",
            ["Umbraco__CMS__ModelsBuilder__ModelsMode"] = "Nothing",
            ["Umbraco__CMS__Hosting__LocalTempStorageLocation"] = "EnvironmentTemp",
            ["Umbraco__CMS__Hosting__SiteName"] = Path.GetFileName(runDirectory),
            ["Umbraco__CMS__Examine__LuceneDirectoryFactory"] = "TempFileSystemDirectoryFactory",
            ["Umbraco__CMS__Logging__Directory"] = Path.Combine(runDirectory, "logs"),
            ["Umbraco__CMS__WebRouting__UmbracoApplicationUrl"] = BaseUrl.ToString(),
            ["DataProtection__KeysDirectory"] = Path.Combine(runDirectory, "keys"),
            ["uSync__Settings__ExportOnSave"] = "None",
            ["VueSsr__Enabled"] = withSsr ? "true" : "false",
            ["VueSsr__BaseUrl"] = $"http://127.0.0.1:{ssrPort}",
            ["TMPDIR"] = temp,
            ["TMP"] = temp,
            ["TEMP"] = temp,
        };
    }
}
```

- [ ] **Step 5: Point `BasePageTests` at the fresh site**

Replace `tests/KCC.E2ETests/BasePageTest.cs` with:

```csharp
using KCC.E2ETests.Config;
using Microsoft.Playwright;
using TUnit.Playwright;

namespace KCC.E2ETests;

public class BasePageTests : PageTest
{
    public BasePageTests()
        : base(new BrowserTypeLaunchOptions { Headless = true }) { }

    [ClassDataSource<SiteProcess>(Shared = SharedType.PerTestSession)]
    public SiteProcess Site { get; init; } = null!;

    public override BrowserNewContextOptions ContextOptions(TestContext testContext)
    {
        var options = base.ContextOptions(testContext) ?? new();
        options.ColorScheme = ColorScheme.Light;
        options.ViewportSize = new() { Height = 1080, Width = 1920 };
        options.BaseURL = Site.BaseUrl.ToString();
        options.IgnoreHTTPSErrors = true;

        return options;
    }
}
```

- [ ] **Step 6: Run it**

```bash
dotnet build KitchenCommandCenter.sln
(cd src/KCC.Web && yarn build:all)
dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj
```

Expected: 1 passed. On failure the exception carries the last 40 lines of the run's `site.log`.

- [ ] **Step 7: Commit**

```bash
git add -A tests/KCC.E2ETests
git commit -m "Start a Fresh Umbraco Site for Each E2E Run"
```

---

### Task 5: The baseline schema

A dev-only bootstrap creates the Phase 1 data types, element types, the `metadata` composition and the document types
through Umbraco's editing services, then generates the ModelsBuilder models. With export-on-save covering `Settings`
in Development, uSync writes every type to `uSync/v17/`. Every key is derived deterministically from its alias, so a
re-run on a fresh database reproduces the same files. Task 7 deletes the bootstrap; the uSync files and generated
models remain.

**Files:**
- Create: `src/KCC.Web/Features/DevTools/Baseline/BaselineKeys.cs`,
  `src/KCC.Web/Features/DevTools/Baseline/SchemaBootstrap.cs`,
  `src/KCC.Web/Features/DevTools/Baseline/BaselineComposer.cs`,
  `src/KCC.Web/Features/DevTools/Baseline/BaselineApiController.cs`,
  `tests/KCC.IntegrationTests/Features/Baseline/SchemaTests.cs`
- Generated and committed: `src/KCC.Web/uSync/v17/{ContentTypes,DataTypes}/*.config`,
  `src/KCC.Web/Features/Models/Generated/*.generated.cs`

**Interfaces:**
- Produces: `BaselineKeys.For(string name) → Guid` (SHA-256 of `kcc-baseline:<name>`, first 16 bytes). Content types use
  `contentType:<alias>`, data types `dataType:<name>`, properties `property:<alias>:<property>`. Task 7's content bootstrap
  relies on the `contentType:<alias>` convention to find types.
- Produces generated models (namespace `KCC.Web.Features.Models.Generated`): `HomePage`, `RecipeListingPage`,
  `CreateRecipePage`, `AddVariantPage`, `AccountPage`, `LoginPage`, `AccountSettingsPage`, `RegistrationCompletePage`,
  `SiteSettings` (`MainNav`, `UtilityNav`: `BlockListModel`), `ContentFolder`, `RecipeCategory`, `RecipeTag`, `StatusCodePage`
  (`StatusCode`: `int`, `Heading`: `string`, `Body`: `IHtmlEncodedString`), `NavLink` (`DisplayText`, `Link`: `Link`,
  `ShowWhen`: `string`), `NavGroup` (`DisplayText`, `Links`: `IEnumerable<Link>`, `ShowWhen`), and the composition
  interface `IMetadata` (`MetadataTitle`, `MetadataDescription`, `MetadataKeywords`, `MetadataImage`: `MediaWithCrops`,
  `BreadcrumbLabel`, `ShowBreadcrumbs`, `ExcludeFromSitemap`, `TwitterCard`, `TwitterSite`, `TwitterCreator`,
  `TwitterImage`).

- [ ] **Step 1: Write the failing schema test**

Create `tests/KCC.IntegrationTests/Features/Baseline/SchemaTests.cs`:

```csharp
using KCC.IntegrationTests.Config;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Services;

namespace KCC.IntegrationTests.Features.Baseline;

public class SchemaTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    [Arguments("homePage")]
    [Arguments("recipeListingPage")]
    [Arguments("createRecipePage")]
    [Arguments("addVariantPage")]
    [Arguments("accountPage")]
    [Arguments("loginPage")]
    [Arguments("accountSettingsPage")]
    [Arguments("registrationCompletePage")]
    [Arguments("siteSettings")]
    [Arguments("contentFolder")]
    [Arguments("recipeCategory")]
    [Arguments("recipeTag")]
    [Arguments("statusCodePage")]
    [Arguments("metadata")]
    [Arguments("navLink")]
    [Arguments("navGroup")]
    public async Task DocumentType_IsImportedOnFirstBoot(string alias)
    {
        var contentTypes = Site.Services.GetRequiredService<IContentTypeService>();

        _ = await Assert.That(contentTypes.Get(alias)).IsNotNull();
    }

    [Test]
    public async Task HomePage_ComposesMetadata()
    {
        var home = Site.Services.GetRequiredService<IContentTypeService>().Get("homePage")!;

        _ = await Assert.That(home.ContentTypeComposition.Any(type => type.Alias == "metadata")).IsTrue();
    }

    [Test]
    public async Task NavElements_AreElementTypes()
    {
        var contentTypes = Site.Services.GetRequiredService<IContentTypeService>();

        _ = await Assert.That(contentTypes.Get("navLink")!.IsElement).IsTrue();
        _ = await Assert.That(contentTypes.Get("navGroup")!.IsElement).IsTrue();
    }
}
```

```bash
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/SchemaTests/*"
```

Expected: FAIL — the types are not in uSync yet.

- [ ] **Step 2: Write the key helper**

Create `src/KCC.Web/Features/DevTools/Baseline/BaselineKeys.cs`:

```csharp
using System.Security.Cryptography;
using System.Text;

namespace KCC.Web.Features.DevTools.Baseline;

public static class BaselineKeys
{
    public static Guid For(string name) =>
        new(SHA256.HashData(Encoding.UTF8.GetBytes($"kcc-baseline:{name}")).AsSpan(0, 16));
}
```

- [ ] **Step 3: Write the schema bootstrap**

Create `src/KCC.Web/Features/DevTools/Baseline/SchemaBootstrap.cs`:

```csharp
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentTypeEditing;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.ContentTypeEditing;
using Umbraco.Cms.Infrastructure.ModelsBuilder.Building;

namespace KCC.Web.Features.DevTools.Baseline;

public class SchemaBootstrap(
    IDataTypeService dataTypeService,
    PropertyEditorCollection propertyEditors,
    IConfigurationEditorJsonSerializer configurationSerializer,
    IContentTypeService contentTypeService,
    IContentTypeEditingService contentTypeEditingService,
    IModelsGenerator modelsGenerator)
{
    private static readonly string[] ShowWhenOptions = ["Always", "Signed in", "Signed out"];

    private static readonly string[] MetadataComposition = ["metadata"];

    public async Task<IReadOnlyList<string>> RunAsync()
    {
        var created = new List<string>();

        var textstring = Constants.DataTypes.Guids.TextstringGuid;
        var textarea = Constants.DataTypes.Guids.TextareaGuid;
        var richText = Constants.DataTypes.Guids.RichtextEditorGuid;
        var numeric = Constants.DataTypes.Guids.NumericGuid;
        var toggle = Constants.DataTypes.Guids.CheckboxGuid;
        var image = Constants.DataTypes.Guids.MediaPicker3SingleImageGuid;

        var showWhen = await EnsureDataTypeAsync(
            created,
            "KCC Show When",
            Constants.PropertyEditors.Aliases.DropDownListFlexible,
            "Umb.PropertyEditorUi.Dropdown",
            new() { ["items"] = ShowWhenOptions, ["multiple"] = false });
        var singleLink = await EnsureDataTypeAsync(
            created,
            "KCC Single Link",
            Constants.PropertyEditors.Aliases.MultiUrlPicker,
            "Umb.PropertyEditorUi.MultiUrlPicker",
            new() { ["minNumber"] = 1, ["maxNumber"] = 1 });
        var links = await EnsureDataTypeAsync(
            created,
            "KCC Links",
            Constants.PropertyEditors.Aliases.MultiUrlPicker,
            "Umb.PropertyEditorUi.MultiUrlPicker",
            new() { ["minNumber"] = 1, ["maxNumber"] = 0 });

        var navLink = await EnsureContentTypeAsync(created, new("navLink", "Nav Link", "icon-link")
        {
            IsElement = true,
            Properties =
            [
                new("displayText", "Display text", textstring, Mandatory: true),
                new("link", "Link", singleLink, Mandatory: true),
                new("showWhen", "Show when", showWhen),
            ],
        });
        var navGroup = await EnsureContentTypeAsync(created, new("navGroup", "Nav Group", "icon-bulleted-list")
        {
            IsElement = true,
            Properties =
            [
                new("displayText", "Display text", textstring, Mandatory: true),
                new("links", "Links", links, Mandatory: true),
                new("showWhen", "Show when", showWhen),
            ],
        });

        var navItems = await EnsureDataTypeAsync(
            created,
            "KCC Nav Items",
            Constants.PropertyEditors.Aliases.BlockList,
            "Umb.PropertyEditorUi.BlockList",
            new()
            {
                ["blocks"] = new BlockListConfiguration.BlockConfiguration[]
                {
                    new() { ContentElementTypeKey = navLink, Label = "{=displayText}" },
                    new() { ContentElementTypeKey = navGroup, Label = "{=displayText}" },
                },
                ["validationLimit"] = new BlockListConfiguration.NumberRange { Min = 0 },
            });

        await EnsureContentTypeAsync(created, new("metadata", "Metadata", "icon-info")
        {
            Group = "Metadata",
            Properties =
            [
                new("metadataTitle", "Title", textstring),
                new("metadataDescription", "Description", textarea),
                new("metadataKeywords", "Keywords", textstring),
                new("metadataImage", "Image", image),
                new("breadcrumbLabel", "Breadcrumb label", textstring),
                new("showBreadcrumbs", "Show breadcrumbs", toggle),
                new("excludeFromSitemap", "Exclude from sitemap", toggle),
                new("twitterCard", "Twitter card", textstring),
                new("twitterSite", "Twitter site", textstring),
                new("twitterCreator", "Twitter creator", textstring),
                new("twitterImage", "Twitter image", image),
            ],
        });

        await EnsureContentTypeAsync(created, new("statusCodePage", "Status Code Page", "icon-alert")
        {
            Properties =
            [
                new("statusCode", "Status code", numeric, Mandatory: true),
                new("heading", "Heading", textstring, Mandatory: true),
                new("body", "Body", richText),
            ],
        });
        await EnsureContentTypeAsync(created, new("recipeCategory", "Recipe Category", "icon-tag"));
        await EnsureContentTypeAsync(created, new("recipeTag", "Recipe Tag", "icon-tags"));
        await EnsureContentTypeAsync(created, new("contentFolder", "Content Folder", "icon-folder")
        {
            AllowedAsRoot = true,
            AllowedChildren = ["recipeCategory", "recipeTag", "statusCodePage"],
        });
        await EnsureContentTypeAsync(created, new("siteSettings", "Site Settings", "icon-settings-alt")
        {
            AllowedAsRoot = true,
            Group = "Navigation",
            Properties =
            [
                new("mainNav", "Main navigation", navItems),
                new("utilityNav", "Utility navigation", navItems),
            ],
        });

        await EnsureContentTypeAsync(created, new("createRecipePage", "Create Recipe Page", "icon-add") { Compositions = MetadataComposition });
        await EnsureContentTypeAsync(created, new("addVariantPage", "Add Variant Page", "icon-add") { Compositions = MetadataComposition });
        await EnsureContentTypeAsync(created, new("loginPage", "Login Page", "icon-lock") { Compositions = MetadataComposition });
        await EnsureContentTypeAsync(created, new("accountSettingsPage", "Account Settings Page", "icon-settings") { Compositions = MetadataComposition });
        await EnsureContentTypeAsync(created, new("registrationCompletePage", "Registration Complete Page", "icon-check") { Compositions = MetadataComposition });
        await EnsureContentTypeAsync(created, new("accountPage", "Account Page", "icon-user")
        {
            Compositions = MetadataComposition,
            AllowedChildren = ["loginPage", "accountSettingsPage", "registrationCompletePage"],
        });
        await EnsureContentTypeAsync(created, new("recipeListingPage", "Recipe Listing Page", "icon-list")
        {
            Compositions = MetadataComposition,
            AllowedChildren = ["createRecipePage", "addVariantPage"],
        });
        await EnsureContentTypeAsync(created, new("homePage", "Home Page", "icon-home")
        {
            AllowedAsRoot = true,
            Compositions = MetadataComposition,
            AllowedChildren = ["recipeListingPage", "accountPage"],
        });

        modelsGenerator.GenerateModels();
        return created;
    }

    private async Task<Guid> EnsureDataTypeAsync(List<string> created, string name, string editorAlias, string editorUiAlias, Dictionary<string, object> configuration)
    {
        var key = BaselineKeys.For($"dataType:{name}");
        if (await dataTypeService.GetAsync(key) is not null)
        {
            return key;
        }

        var editor = propertyEditors[editorAlias] ?? throw new InvalidOperationException($"No property editor is registered as {editorAlias}.");
        var dataType = new DataType(editor, configurationSerializer)
        {
            Key = key,
            Name = name,
            EditorUiAlias = editorUiAlias,
            DatabaseType = ValueTypes.ToStorageType(editor.GetValueEditor().ValueType),
            ConfigurationData = configuration,
        };

        var result = await dataTypeService.CreateAsync(dataType, Constants.Security.SuperUserKey);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Creating data type {name} failed: {result.Status}.");
        }

        created.Add(name);
        return key;
    }

    private async Task<Guid> EnsureContentTypeAsync(List<string> created, TypeSpec spec)
    {
        var key = BaselineKeys.For($"contentType:{spec.Alias}");
        if (contentTypeService.Get(key) is not null)
        {
            return key;
        }

        var containerKey = BaselineKeys.For($"container:{spec.Alias}");
        var model = new ContentTypeCreateModel
        {
            Key = key,
            Alias = spec.Alias,
            Name = spec.Name,
            Icon = spec.Icon,
            IsElement = spec.IsElement,
            AllowedAsRoot = spec.AllowedAsRoot,
            Containers = spec.Properties.Count == 0
                ? []
                : [new ContentTypePropertyContainerModel { Key = containerKey, Name = spec.Group, Type = "Group", SortOrder = 0 }],
            Properties = spec.Properties.Select((property, index) => new ContentTypePropertyTypeModel
            {
                Key = BaselineKeys.For($"property:{spec.Alias}:{property.Alias}"),
                ContainerKey = containerKey,
                SortOrder = index,
                Alias = property.Alias,
                Name = property.Name,
                DataTypeKey = property.DataType,
                Validation = new PropertyTypeValidation { Mandatory = property.Mandatory },
                Appearance = new PropertyTypeAppearance(),
            }).ToList(),
            Compositions = spec.Compositions.Select(alias => new Composition
            {
                Key = BaselineKeys.For($"contentType:{alias}"),
                CompositionType = CompositionType.Composition,
            }).ToList(),
            AllowedContentTypes = spec.AllowedChildren
                .Select((alias, index) => new ContentTypeSort(BaselineKeys.For($"contentType:{alias}"), index, alias))
                .ToList(),
        };

        var result = await contentTypeEditingService.CreateAsync(model, Constants.Security.SuperUserKey);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Creating document type {spec.Alias} failed: {result.Status}.");
        }

        created.Add(spec.Alias);
        return key;
    }

    private sealed record PropertySpec(string Alias, string Name, Guid DataType, bool Mandatory = false);

    private sealed record TypeSpec(string Alias, string Name, string Icon)
    {
        public bool IsElement { get; init; }

        public bool AllowedAsRoot { get; init; }

        public string Group { get; init; } = "Content";

        public IReadOnlyList<PropertySpec> Properties { get; init; } = [];

        public IReadOnlyList<string> Compositions { get; init; } = [];

        public IReadOnlyList<string> AllowedChildren { get; init; } = [];
    }
}
```

If `BlockListConfiguration.BlockConfiguration` has no `Label` property in the installed version, delete the two
`Label = …` assignments; the backoffice then labels blocks with their type name.

- [ ] **Step 4: Register it and expose a dev-only endpoint**

Create `src/KCC.Web/Features/DevTools/Baseline/BaselineComposer.cs`:

```csharp
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace KCC.Web.Features.DevTools.Baseline;

public class BaselineComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddTransient<SchemaBootstrap>();
    }
}
```

Create `src/KCC.Web/Features/DevTools/Baseline/BaselineApiController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;

namespace KCC.Web.Features.DevTools.Baseline;

[ApiController]
[Route("api/dev/baseline")]
public class BaselineApiController(IWebHostEnvironment environment, IServiceProvider services) : ControllerBase
{
    [HttpPost("schema")]
    public async Task<IActionResult> CreateSchema()
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        return Ok(await services.GetRequiredService<SchemaBootstrap>().RunAsync());
    }
}
```

The controller resolves bootstraps on demand so Tasks 6 and 7 can add theirs without touching the constructor.

- [ ] **Step 5: Run the bootstrap**

```bash
dotnet build src/KCC.Web/KCC.Web.csproj
cd src/KCC.Web && dotnet run --launch-profile Local
```

(plain `dotnet run`, not `dotnet watch`: the generated models would otherwise restart the app mid-request). Once it
listens, in a second terminal:

```bash
curl -sk -X POST https://localhost:58671/api/dev/baseline/schema
```

Expected: a JSON array naming the 4 data types and 16 content types created. Then stop the site and check:

```bash
ls src/KCC.Web/uSync/v17/ContentTypes | wc -l
ls src/KCC.Web/uSync/v17/DataTypes | grep -ci 'kcc'
ls src/KCC.Web/Features/Models/Generated
```

Expected: 16 content type files; 4 `kcc-…` data type files; one `.generated.cs` per content type (including
`Metadata.generated.cs` with the `IMetadata` interface).

- [ ] **Step 6: Build with the generated models**

```bash
dotnet build KitchenCommandCenter.sln
```

Expected: `Build succeeded`, 0 warnings. Generated files carry `<auto-generated>` headers, so StyleCop skips them,
and CS1591 is already off repo-wide. If the build reports warnings from the generated folder, add an
`.editorconfig` section `[src/KCC.Web/Features/Models/Generated/*.cs]` with `generated_code = true` and re-run.

- [ ] **Step 7: Run the schema tests against a fresh database**

```bash
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj
```

Expected: all pass — the fresh database imported every type from `uSync/v17/` on first boot.

- [ ] **Step 8: Commit**

```bash
git add src/KCC.Web/Features/DevTools/Baseline src/KCC.Web/uSync src/KCC.Web/Features/Models/Generated tests/KCC.IntegrationTests
git commit -m "Add the Baseline Schema"
```

---

### Task 6: Move the UI strings into the dictionary

The Phase 0 export becomes ~218 dictionary items (216 plus `Shared.Home`, which the breadcrumbs read, and
`Shared.LogoAlt`, which the static logo needs), grouped under one parent item per key prefix. A first-party provider
replaces `IResourceStringInfoProvider` with the same method names, so ported controllers only change a type name.
This task also proves spec §19's second row: an edited value survives a restart.

**Files:**
- Create: `src/KCC.Web/Features/Dictionary/IResourceStringProvider.cs`,
  `src/KCC.Web/Features/Dictionary/DictionaryResourceStringProvider.cs`,
  `src/KCC.Web/Features/DevTools/Baseline/UiStringsBootstrap.cs`,
  `tests/KCC.UnitTests/Features/Dictionary/DictionaryResourceStringProviderTests.cs`,
  `tests/KCC.IntegrationTests/Features/Baseline/DictionaryTests.cs`,
  `tests/KCC.E2ETests/Config/SiteDatabase.cs`, `tests/KCC.E2ETests/Features/Hosting/DictionaryRestartTests.cs`
- Modify: `src/KCC.Web/Program.cs`, `src/KCC.Web/Features/DevTools/Baseline/{BaselineComposer,BaselineApiController}.cs`,
  `tests/KCC.E2ETests/KCC.E2ETests.csproj`, `Directory.Packages.props`
- Generated and committed: `src/KCC.Web/uSync/v17/Dictionary/*.config`
- Delete: `src/KCC.Web/Features/Dictionary/ui-strings.json` (once the proof passes)

**Interfaces:**
- Produces: `KCC.Web.Features.Dictionary.IResourceStringProvider` with `string GetOrDefault(string key)` and
  `Dictionary<string, string> GetManyOrDefault(params string[] keys)` — a missing or empty value returns the key itself.
  Registered scoped in `Program.cs`.
- Produces (E2E): `SiteDatabase.ReadDictionaryValueAsync(string databasePath, string key) → Task<string?>` and
  `SiteDatabase.WriteDictionaryValueAsync(string databasePath, string key, string value) → Task`.

- [ ] **Step 1: Write the provider's failing unit tests**

Create `tests/KCC.UnitTests/Features/Dictionary/DictionaryResourceStringProviderTests.cs`:

```csharp
using System.Globalization;
using KCC.Web.Features.Dictionary;
using Moq;
using Umbraco.Cms.Core.Dictionary;

namespace KCC.UnitTests.Features.Dictionary;

public class DictionaryResourceStringProviderTests
{
    [Test]
    public async Task GetOrDefault_KnownKey_ReturnsTheValue()
    {
        var provider = CreateProvider(("Login.SignIn", "Sign in"));

        _ = await Assert.That(provider.GetOrDefault("Login.SignIn")).IsEqualTo("Sign in");
    }

    [Test]
    public async Task GetOrDefault_MissingOrEmptyValue_ReturnsTheKey()
    {
        var provider = CreateProvider(("Login.Empty", string.Empty));

        _ = await Assert.That(provider.GetOrDefault("Login.Empty")).IsEqualTo("Login.Empty");
        _ = await Assert.That(provider.GetOrDefault("Login.Missing")).IsEqualTo("Login.Missing");
    }

    [Test]
    public async Task GetManyOrDefault_MapsEveryKeyWithTheKeyAsFallback()
    {
        var provider = CreateProvider(("Login.SignIn", "Sign in"));

        var strings = provider.GetManyOrDefault("Login.SignIn", "Login.Missing");

        _ = await Assert.That(strings["Login.SignIn"]).IsEqualTo("Sign in");
        _ = await Assert.That(strings["Login.Missing"]).IsEqualTo("Login.Missing");
    }

    [Test]
    public async Task GetManyOrDefault_DuplicateKeys_CollapseToOneEntry()
    {
        var provider = CreateProvider(("Login.SignIn", "Sign in"));

        var strings = provider.GetManyOrDefault("Login.SignIn", "Login.SignIn");

        _ = await Assert.That(strings.Count).IsEqualTo(1);
    }

    private static DictionaryResourceStringProvider CreateProvider(params (string Key, string Value)[] entries)
    {
        var dictionary = new Mock<ICultureDictionary>();
        dictionary.Setup(d => d[It.IsAny<string>()]).Returns(string.Empty);
        foreach (var (key, value) in entries)
        {
            dictionary.Setup(d => d[key]).Returns(value);
        }

        var factory = new Mock<ICultureDictionaryFactory>();
        factory.Setup(f => f.CreateDictionary(It.Is<CultureInfo>(culture => culture.Name == "en-US"))).Returns(dictionary.Object);
        return new DictionaryResourceStringProvider(factory.Object);
    }
}
```

```bash
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj
```

Expected: build error — the provider does not exist.

- [ ] **Step 2: Write the provider**

Create `src/KCC.Web/Features/Dictionary/IResourceStringProvider.cs`:

```csharp
namespace KCC.Web.Features.Dictionary;

public interface IResourceStringProvider
{
    string GetOrDefault(string key);

    Dictionary<string, string> GetManyOrDefault(params string[] keys);
}
```

Create `src/KCC.Web/Features/Dictionary/DictionaryResourceStringProvider.cs`:

```csharp
using System.Globalization;
using Umbraco.Cms.Core.Dictionary;

namespace KCC.Web.Features.Dictionary;

public class DictionaryResourceStringProvider(ICultureDictionaryFactory dictionaryFactory) : IResourceStringProvider
{
    // The site has one language, and requests outside Umbraco's content routing (the API, /error) carry no culture
    // of their own, so every lookup names it.
    private static readonly CultureInfo SiteCulture = CultureInfo.GetCultureInfo("en-US");

    public string GetOrDefault(string key) => Resolve(dictionaryFactory.CreateDictionary(SiteCulture), key);

    public Dictionary<string, string> GetManyOrDefault(params string[] keys)
    {
        var dictionary = dictionaryFactory.CreateDictionary(SiteCulture);
        return keys
            .Distinct(StringComparer.Ordinal)
            .ToDictionary(key => key, key => Resolve(dictionary, key), StringComparer.Ordinal);
    }

    private static string Resolve(ICultureDictionary dictionary, string key) =>
        dictionary[key] is { Length: > 0 } value ? value : key;
}
```

In `src/KCC.Web/Program.cs`, add `using KCC.Web.Features.Dictionary;` (sorted) and, after
`builder.Services.AddVueSsr(builder.Configuration);`:

```csharp
builder.Services.AddScoped<IResourceStringProvider, DictionaryResourceStringProvider>();
```

```bash
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj
```

Expected: the 4 new tests pass (13 in total).

- [ ] **Step 3: Write the failing integration test**

Create `tests/KCC.IntegrationTests/Features/Baseline/DictionaryTests.cs`:

```csharp
using KCC.IntegrationTests.Config;
using KCC.Web.Features.Dictionary;
using Microsoft.Extensions.DependencyInjection;

namespace KCC.IntegrationTests.Features.Baseline;

public class DictionaryTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task ExportedString_IsImportedOnFirstBoot()
    {
        using var scope = Site.Services.CreateScope();
        var strings = scope.ServiceProvider.GetRequiredService<IResourceStringProvider>();

        _ = await Assert.That(strings.GetOrDefault("Login.SignIn")).IsNotEqualTo("Login.SignIn");
        _ = await Assert.That(strings.GetOrDefault("Shared.LogoAlt")).IsEqualTo("Kitchen Command Center");
    }

    [Test]
    public async Task UnknownKey_FallsBackToTheKey()
    {
        using var scope = Site.Services.CreateScope();
        var strings = scope.ServiceProvider.GetRequiredService<IResourceStringProvider>();

        _ = await Assert.That(strings.GetOrDefault("Nowhere.ToBeFound")).IsEqualTo("Nowhere.ToBeFound");
    }
}
```

```bash
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/DictionaryTests/*"
```

Expected: `ExportedString_IsImportedOnFirstBoot` FAILS (no dictionary items yet); `UnknownKey_FallsBackToTheKey` passes.

- [ ] **Step 4: Write the import bootstrap**

Create `src/KCC.Web/Features/DevTools/Baseline/UiStringsBootstrap.cs`:

```csharp
using System.Text.Json;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace KCC.Web.Features.DevTools.Baseline;

public class UiStringsBootstrap(IWebHostEnvironment environment, IDictionaryItemService dictionaryItems, ILanguageService languages)
{
    private static readonly Dictionary<string, string> Additions = new(StringComparer.Ordinal)
    {
        ["Shared.Home"] = "Home",
        ["Shared.LogoAlt"] = "Kitchen Command Center",
    };

    public async Task<int> RunAsync()
    {
        var path = Path.Combine(environment.ContentRootPath, "Features", "Dictionary", "ui-strings.json");
        var strings = JsonSerializer.Deserialize<Dictionary<string, string>>(await File.ReadAllTextAsync(path));
        foreach (var (key, value) in Additions)
        {
            strings.TryAdd(key, value);
        }

        var language = await languages.GetDefaultLanguageAsync() ?? throw new InvalidOperationException("The site has no default language.");
        var parents = new Dictionary<string, Guid>(StringComparer.Ordinal);
        var created = 0;

        foreach (var (key, value) in strings.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            if (await dictionaryItems.GetAsync(key) is not null)
            {
                continue;
            }

            var dot = key.IndexOf('.');
            var prefix = dot > 0 ? key[..dot] : null;
            Guid? parentKey = prefix is null || strings.ContainsKey(prefix) ? null : await EnsureParentAsync(prefix, parents);

            var item = new DictionaryItem(parentKey, key)
            {
                Translations = value.Length > 0 ? [new DictionaryTranslation(language, value)] : [],
            };
            var result = await dictionaryItems.CreateAsync(item, Constants.Security.SuperUserKey);
            if (!result.Success)
            {
                throw new InvalidOperationException($"Creating dictionary item {key} failed: {result.Status}.");
            }

            created++;
        }

        return created;
    }

    private async Task<Guid> EnsureParentAsync(string prefix, Dictionary<string, Guid> parents)
    {
        if (parents.TryGetValue(prefix, out var known))
        {
            return known;
        }

        var existing = await dictionaryItems.GetAsync(prefix);
        if (existing is not null)
        {
            return parents[prefix] = existing.Key;
        }

        var result = await dictionaryItems.CreateAsync(new DictionaryItem(null, prefix), Constants.Security.SuperUserKey);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Creating dictionary group {prefix} failed: {result.Status}.");
        }

        return parents[prefix] = result.Result.Key;
    }
}
```

In `BaselineComposer.Compose`, add `builder.Services.AddTransient<UiStringsBootstrap>();`. In
`BaselineApiController`, add:

```csharp
    [HttpPost("ui-strings")]
    public async Task<IActionResult> ImportUiStrings()
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        return Ok(await services.GetRequiredService<UiStringsBootstrap>().RunAsync());
    }
```

- [ ] **Step 5: Run the import**

```bash
cd src/KCC.Web && dotnet run --launch-profile Local
```

In a second terminal:

```bash
curl -sk -X POST https://localhost:58671/api/dev/baseline/ui-strings
```

Expected: a number equal to the Phase 0 export's row count plus 2 (minus any keys already present). Stop the site,
then:

```bash
ls src/KCC.Web/uSync/v17/Dictionary | wc -l
```

Expected: roughly the item count plus one file per prefix group (`Login`, `RecipeSearch`, `Shared`, …). Dictionary
items sit in the `Settings` group, so export-on-save wrote them.

- [ ] **Step 6: Re-run the dictionary integration tests**

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/DictionaryTests/*"
```

Expected: both pass.

- [ ] **Step 7: Pin SQLite for the E2E project**

```bash
dotnet list src/KCC.Web/KCC.Web.csproj package --include-transitive | grep -i 'Microsoft.Data.Sqlite'
```

Add `<PackageVersion Include="Microsoft.Data.Sqlite" Version="<the resolved Microsoft.Data.Sqlite(.Core) version>" />`
to `Directory.Packages.props` (alphabetical), and `<PackageReference Include="Microsoft.Data.Sqlite" />` to the package
`ItemGroup` of `tests/KCC.E2ETests/KCC.E2ETests.csproj`.

- [ ] **Step 8: Write the restart proof (spec §19)**

Create `tests/KCC.E2ETests/Config/SiteDatabase.cs`:

```csharp
using Microsoft.Data.Sqlite;

namespace KCC.E2ETests.Config;

/// <summary>Reads and writes a stopped site's SQLite file directly, the way a live edit would change it.</summary>
public static class SiteDatabase
{
    public static async Task<string?> ReadDictionaryValueAsync(string databasePath, string key)
    {
        await using var connection = await OpenAsync(databasePath);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT t.value FROM cmsLanguageText t
            INNER JOIN cmsDictionary d ON t.UniqueId = d.id
            WHERE d."key" = $key
            """;
        command.Parameters.AddWithValue("$key", key);
        return (string?)await command.ExecuteScalarAsync();
    }

    public static async Task WriteDictionaryValueAsync(string databasePath, string key, string value)
    {
        await using var connection = await OpenAsync(databasePath);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE cmsLanguageText SET value = $value
            WHERE UniqueId = (SELECT id FROM cmsDictionary WHERE "key" = $key)
            """;
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$value", value);
        if (await command.ExecuteNonQueryAsync() != 1)
        {
            throw new InvalidOperationException($"Dictionary item {key} has no translation row to update.");
        }
    }

    private static async Task<SqliteConnection> OpenAsync(string databasePath)
    {
        var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");
        await connection.OpenAsync();
        return connection;
    }
}
```

Create `tests/KCC.E2ETests/Features/Hosting/DictionaryRestartTests.cs`:

```csharp
using KCC.E2ETests.Config;

namespace KCC.E2ETests.Features.Hosting;

[NotInParallel]
public class DictionaryRestartTests
{
    private const string Key = "Login.SignIn";
    private const string EditedValue = "Edited on the live site";

    [Test]
    public async Task EditedDictionaryValue_SurvivesARestart()
    {
        await using var site = new SiteProcess(withSsr: false);
        await site.InitializeAsync();
        await site.StopAsync();

        var imported = await SiteDatabase.ReadDictionaryValueAsync(site.DatabasePath, Key);
        await SiteDatabase.WriteDictionaryValueAsync(site.DatabasePath, Key, EditedValue);

        await site.StartAsync();
        await site.StopAsync();

        _ = await Assert.That(imported).IsNotNull();
        _ = await Assert.That(imported).IsNotEqualTo(EditedValue);
        _ = await Assert.That(await SiteDatabase.ReadDictionaryValueAsync(site.DatabasePath, Key)).IsEqualTo(EditedValue);
    }
}
```

- [ ] **Step 9: Run the proof**

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj -- --treenode-filter "/*/*/DictionaryRestartTests/*"
```

Expected: PASS. If the SQL fails with "no such table/column", inspect the schema with
`sqlite3 <the temp run's Umbraco.sqlite.db> '.schema cmsDictionary' '.schema cmsLanguageText'` (the run folder is named
in the exception) and fix the column names in `SiteDatabase`.

**If the value is overwritten** (the assertion on the edited value fails), take spec §19's fallback — the dictionary
leaves uSync and a first-boot step seeds it from a committed file:

1. In `src/KCC.Web/appsettings.json`, replace the whole `"DictionaryHandler": { … }` entry with
   `"DictionaryHandler": { "Enabled": false }`, then:

   ```bash
   git rm -r -q src/KCC.Web/uSync/v17/Dictionary
   git mv src/KCC.Web/Features/Dictionary/ui-strings.json src/KCC.Web/Features/Dictionary/baseline-strings.json
   ```

2. Add the two extra strings to `baseline-strings.json` (keep it sorted by key):
   `"Shared.Home": "Home"` and `"Shared.LogoAlt": "Kitchen Command Center"`.
3. Make the file ship with the site — in `src/KCC.Web/KCC.Web.csproj`, add an item group:

   ```xml
       <ItemGroup>
           <Content Update="Features/Dictionary/baseline-strings.json" CopyToOutputDirectory="PreserveNewest" CopyToPublishDirectory="PreserveNewest" />
       </ItemGroup>
   ```

4. Create `src/KCC.Web/Features/Dictionary/DictionarySeedHandler.cs`:

   ```csharp
   using System.Text.Json;
   using Umbraco.Cms.Core;
   using Umbraco.Cms.Core.Events;
   using Umbraco.Cms.Core.Models;
   using Umbraco.Cms.Core.Notifications;
   using Umbraco.Cms.Core.Services;

   namespace KCC.Web.Features.Dictionary;

   // uSync's startup import cannot be kept from overwriting live edits, so the dictionary is seeded once, from a
   // committed file, into a database that has none.
   public class DictionarySeedHandler(IWebHostEnvironment environment, IDictionaryItemService dictionaryItems, ILanguageService languages)
       : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
   {
       public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
       {
           if ((await dictionaryItems.GetAtRootAsync()).Any())
           {
               return;
           }

           var path = Path.Combine(environment.ContentRootPath, "Features", "Dictionary", "baseline-strings.json");
           var strings = JsonSerializer.Deserialize<Dictionary<string, string>>(await File.ReadAllTextAsync(path, cancellationToken));
           var language = await languages.GetDefaultLanguageAsync() ?? throw new InvalidOperationException("The site has no default language.");
           var parents = new Dictionary<string, Guid>(StringComparer.Ordinal);

           foreach (var (key, value) in strings.OrderBy(pair => pair.Key, StringComparer.Ordinal))
           {
               var dot = key.IndexOf('.');
               var prefix = dot > 0 ? key[..dot] : null;
               Guid? parentKey = prefix is null || strings.ContainsKey(prefix) ? null : await EnsureParentAsync(prefix, parents);

               var item = new DictionaryItem(parentKey, key)
               {
                   Translations = value.Length > 0 ? [new DictionaryTranslation(language, value)] : [],
               };
               var result = await dictionaryItems.CreateAsync(item, Constants.Security.SuperUserKey);
               if (!result.Success)
               {
                   throw new InvalidOperationException($"Seeding dictionary item {key} failed: {result.Status}.");
               }
           }
       }

       private async Task<Guid> EnsureParentAsync(string prefix, Dictionary<string, Guid> parents)
       {
           if (parents.TryGetValue(prefix, out var known))
           {
               return known;
           }

           var result = await dictionaryItems.CreateAsync(new DictionaryItem(null, prefix), Constants.Security.SuperUserKey);
           if (!result.Success)
           {
               throw new InvalidOperationException($"Seeding dictionary group {prefix} failed: {result.Status}.");
           }

           return parents[prefix] = result.Result.Key;
       }
   }
   ```

5. Create `src/KCC.Web/Features/Dictionary/DictionaryComposer.cs`:

   ```csharp
   using Umbraco.Cms.Core.Composing;
   using Umbraco.Cms.Core.DependencyInjection;
   using Umbraco.Cms.Core.Notifications;
   using Umbraco.Extensions;

   namespace KCC.Web.Features.Dictionary;

   public class DictionaryComposer : IComposer
   {
       public void Compose(IUmbracoBuilder builder) =>
           builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, DictionarySeedHandler>();
   }
   ```

6. Re-run the proof and `DictionaryTests` until both pass. Then skip Step 10 (the file is now the seed), leave
   `UiStringsBootstrap.cs` for Task 7 to delete as planned, and record the fallback in the spec's §12 and §19.

- [ ] **Step 10: Retire the export file**

On the success path the uSync files are the single source of the strings:

```bash
git rm -q src/KCC.Web/Features/Dictionary/ui-strings.json
```

Task 7 deletes `UiStringsBootstrap`, which reads it.

- [ ] **Step 11: Commit**

```bash
git add -A src/KCC.Web tests Directory.Packages.props
git commit -m "Move the UI Strings into the Umbraco Dictionary"
```

---

### Task 7: The baseline site tree and its export

A second bootstrap creates and publishes the baseline tree. A permanent dev-only endpoint exports the `Content`
group to uSync, refusing while any recipe exists, so seeded test data never enters the baseline. Content is not
exported on save; this endpoint is the only way the baseline changes.

**Files:**
- Create: `src/KCC.Web/Features/DevTools/Baseline/ContentBootstrap.cs` (removed at the end of this task),
  `src/KCC.Web/Features/DevTools/Baseline/BaselineExport.cs`,
  `tests/KCC.IntegrationTests/Features/Baseline/BaselineContentTests.cs`
- Modify: `src/KCC.Web/Features/DevTools/Baseline/{BaselineComposer,BaselineApiController}.cs`
- Generated and committed: `src/KCC.Web/uSync/v17/Content/*.config` (and whatever else the `Content` group writes)
- Delete (end of task): `SchemaBootstrap.cs`, `UiStringsBootstrap.cs`, `ContentBootstrap.cs`, `BaselineKeys.cs`

**Interfaces:**
- Produces (kept): `POST /api/dev/baseline/export` (Development only) → 200 with the number of uSync actions, or 409
  with the reason. `BaselineExport.RunAsync() → Task<BaselineExportResult>`, where
  `BaselineExportResult(bool Succeeded, string Message)`.
- Produces baseline nodes (URL segments from names): Home (`/`), Recipes (`/recipes`), Create Recipe, Add Variant,
  Account, Login, Settings, Registration Complete; root nodes Site Settings, Recipe Categories (6), Recipe Tags (11),
  Status Codes (404, 500).

- [ ] **Step 1: Write the failing baseline test**

Create `tests/KCC.IntegrationTests/Features/Baseline/BaselineContentTests.cs`:

```csharp
using KCC.IntegrationTests.Config;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;

namespace KCC.IntegrationTests.Features.Baseline;

public class BaselineContentTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    private IContentService Content => Site.Services.GetRequiredService<IContentService>();

    private IDocumentNavigationQueryService Navigation => Site.Services.GetRequiredService<IDocumentNavigationQueryService>();

    [Test]
    public async Task Home_IsTheFirstPublishedRoot()
    {
        var first = Content.GetRootContent().OrderBy(node => node.SortOrder).First();

        _ = await Assert.That(first.ContentType.Alias).IsEqualTo("homePage");
        _ = await Assert.That(first.Published).IsTrue();
    }

    [Test]
    [Arguments("Recipe Categories", 6)]
    [Arguments("Recipe Tags", 11)]
    [Arguments("Status Codes", 2)]
    public async Task RootFolder_HoldsItsPublishedChildren(string folderName, int expected)
    {
        var folder = Content.GetRootContent().Single(node => node.Name == folderName);
        var children = ChildrenOf(folder);

        _ = await Assert.That(children.Count).IsEqualTo(expected);
        _ = await Assert.That(children.All(child => child.Published)).IsTrue();
    }

    [Test]
    public async Task SiteSettings_HoldTheSignInAndAccountNavigation()
    {
        var settings = Content.GetRootContent().Single(node => node.ContentType.Alias == "siteSettings");
        var utility = settings.GetValue<string>("utilityNav") ?? string.Empty;

        _ = await Assert.That(utility.Contains("Login", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(utility.Contains("Account", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    [Arguments("Recipes", "Create Recipe")]
    [Arguments("Account", "Login")]
    [Arguments("Account", "Settings")]
    [Arguments("Account", "Registration Complete")]
    public async Task AppPage_SitsUnderItsParent(string parentName, string childName)
    {
        var home = Content.GetRootContent().Single(node => node.ContentType.Alias == "homePage");
        var parent = ChildrenOf(home).Single(node => node.Name == parentName);

        _ = await Assert.That(ChildrenOf(parent).Any(child => child.Name == childName && child.Published)).IsTrue();
    }

    // The short IContentService.GetPagedChildren overload is obsolete in Umbraco 17, and warnings fail the build.
    private List<IContent> ChildrenOf(IContent parent) =>
        Navigation.TryGetChildrenKeys(parent.Key, out var keys) ? Content.GetByIds(keys).ToList() : [];
}
```

```bash
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/BaselineContentTests/*"
```

Expected: FAIL — there is no baseline content yet.

- [ ] **Step 2: Write the content bootstrap**

Create `src/KCC.Web/Features/DevTools/Baseline/ContentBootstrap.cs`:

```csharp
using System.Text.Json;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Models.ContentPublishing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;

namespace KCC.Web.Features.DevTools.Baseline;

public class ContentBootstrap(
    IContentEditingService contentEditingService,
    IContentPublishingService contentPublishingService,
    IContentService contentService)
{
    private static readonly string[] Categories = ["Breakfast", "Lunch", "Dinner", "Dessert", "Snack", "Beverage"];

    private static readonly string[] Tags =
        ["Vegetarian", "Vegan", "Gluten-Free", "Dairy-Free", "Keto", "High-Protein", "Low-Carb", "Spicy", "Cheesy", "Easy", "Fast"];

    public async Task<IReadOnlyList<string>> RunAsync()
    {
        var created = new List<string>();

        var home = await EnsurePublishedAsync(created, "home", "Home", "homePage", null, ("metadataTitle", "Kitchen Command Center"));
        var recipes = await EnsurePublishedAsync(created, "recipes", "Recipes", "recipeListingPage", home);
        var createRecipe = await EnsurePublishedAsync(created, "createRecipe", "Create Recipe", "createRecipePage", recipes);
        await EnsurePublishedAsync(created, "addVariant", "Add Variant", "addVariantPage", recipes);
        var account = await EnsurePublishedAsync(created, "account", "Account", "accountPage", home);
        var login = await EnsurePublishedAsync(created, "login", "Login", "loginPage", account);
        var settings = await EnsurePublishedAsync(created, "settings", "Settings", "accountSettingsPage", account);
        await EnsurePublishedAsync(created, "registrationComplete", "Registration Complete", "registrationCompletePage", account);

        await EnsurePublishedAsync(
            created,
            "siteSettings",
            "Site Settings",
            "siteSettings",
            null,
            ("mainNav", BlockList(NavGroup("Recipes", "Always", DocumentLink("All Recipes", recipes), DocumentLink("Create Recipe", createRecipe)))),
            ("utilityNav", BlockList(
                NavLink("Login", "Signed out", DocumentLink("Login", login)),
                NavGroup("Account", "Signed in", DocumentLink("Profile", account), DocumentLink("Settings", settings), ExternalLink("Logout", "/account/logout")))));

        var categories = await EnsurePublishedAsync(created, "recipeCategories", "Recipe Categories", "contentFolder", null);
        foreach (var category in Categories)
        {
            await EnsurePublishedAsync(created, $"category:{category}", category, "recipeCategory", categories);
        }

        var tags = await EnsurePublishedAsync(created, "recipeTags", "Recipe Tags", "contentFolder", null);
        foreach (var tag in Tags)
        {
            await EnsurePublishedAsync(created, $"tag:{tag}", tag, "recipeTag", tags);
        }

        var statusCodes = await EnsurePublishedAsync(created, "statusCodes", "Status Codes", "contentFolder", null);
        await EnsurePublishedAsync(
            created,
            "status:404",
            "404",
            "statusCodePage",
            statusCodes,
            ("statusCode", 404),
            ("heading", "We couldn't find that page"),
            ("body", "<p>The page may have moved, or the link may be wrong. Try the <a href=\"/\">home page</a> or the <a href=\"/recipes\">recipes</a>.</p>"));
        await EnsurePublishedAsync(
            created,
            "status:500",
            "500",
            "statusCodePage",
            statusCodes,
            ("statusCode", 500),
            ("heading", "Something went wrong"),
            ("body", "<p>The kitchen hit a snag while loading this page. Please try again in a moment.</p>"));

        return created;
    }

    private static NavBlock NavLink(string displayText, string showWhen, object link) => new(
        BaselineKeys.For($"block:navLink:{displayText}"),
        BaselineKeys.For("contentType:navLink"),
        [Value("displayText", displayText), Value("link", new[] { link }), Value("showWhen", new[] { showWhen })]);

    private static NavBlock NavGroup(string displayText, string showWhen, params object[] links) => new(
        BaselineKeys.For($"block:navGroup:{displayText}"),
        BaselineKeys.For("contentType:navGroup"),
        [Value("displayText", displayText), Value("links", links), Value("showWhen", new[] { showWhen })]);

    private static object DocumentLink(string name, Guid key) =>
        new { name, type = "document", unique = key, url = (string)null, target = (string)null };

    private static object ExternalLink(string name, string url) =>
        new { name, type = "external", unique = (string)null, url, target = (string)null };

    private static object Value(string alias, object value) =>
        new { alias, value, culture = (string)null, segment = (string)null };

    private static string BlockList(params NavBlock[] blocks) => JsonSerializer.Serialize(new
    {
        layout = new Dictionary<string, object>
        {
            ["Umbraco.BlockList"] = blocks.Select(block => new { contentKey = block.Key, settingsKey = (Guid?)null }).ToArray(),
        },
        contentData = blocks.Select(block => new { key = block.Key, contentTypeKey = block.ContentTypeKey, values = block.Values }).ToArray(),
        settingsData = Array.Empty<object>(),
        expose = blocks.Select(block => new { contentKey = block.Key, culture = (string)null, segment = (string)null }).ToArray(),
    });

    private async Task<Guid> EnsurePublishedAsync(
        List<string> created,
        string name,
        string nodeName,
        string contentTypeAlias,
        Guid? parentKey,
        params (string Alias, object Value)[] values)
    {
        var key = BaselineKeys.For($"content:{name}");
        if (contentService.GetById(key) is null)
        {
            var result = await contentEditingService.CreateAsync(
                new ContentCreateModel
                {
                    Key = key,
                    ContentTypeKey = BaselineKeys.For($"contentType:{contentTypeAlias}"),
                    ParentKey = parentKey,
                    Variants = [new VariantModel { Name = nodeName }],
                    Properties = values.Select(value => new PropertyValueModel { Alias = value.Alias, Value = value.Value }).ToList(),
                },
                Constants.Security.SuperUserKey);

            // A property validation error still reports Success, so the status is what counts.
            if (result.Status != ContentEditingOperationStatus.Success)
            {
                throw new InvalidOperationException($"Creating {nodeName} failed: {result.Status}.");
            }

            created.Add(nodeName);
        }

        var published = await contentPublishingService.PublishAsync(
            key,
            [new CulturePublishScheduleModel { Culture = null }],
            Constants.Security.SuperUserKey);
        if (!published.Success)
        {
            throw new InvalidOperationException($"Publishing {nodeName} failed: {published.Status}.");
        }

        return key;
    }

    private sealed record NavBlock(Guid Key, Guid ContentTypeKey, object[] Values);
}
```

- [ ] **Step 3: Write the export**

Create `src/KCC.Web/Features/DevTools/Baseline/BaselineExport.cs`:

```csharp
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.SyncHandlers.Models;

namespace KCC.Web.Features.DevTools.Baseline;

public sealed record BaselineExportResult(bool Succeeded, string Message);

public class BaselineExport(
    ISyncService syncService,
    ISyncConfigService syncConfig,
    IContentService contentService,
    IContentTypeService contentTypeService,
    IUmbracoContextFactory umbracoContextFactory,
    IWebHostEnvironment environment)
{
    // The handler folders of uSync's Content group. An export overwrites files but never removes the files of
    // deleted items, so the folders are cleared first.
    private static readonly string[] ContentGroupFolders = ["Content", "Media", "Domains", "Blueprints", "RelationTypes"];

    public async Task<BaselineExportResult> RunAsync()
    {
        if (contentTypeService.Get("recipe") is { } recipeType && contentService.Count(recipeType.Alias) > 0)
        {
            return new(false, "This database holds recipes, which are test data. Export the baseline from a fresh database.");
        }

        var workingFolder = syncConfig.GetWorkingFolder();
        foreach (var folder in ContentGroupFolders)
        {
            var path = Path.Combine(environment.ContentRootPath, workingFolder, folder);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }

        using var umbracoContext = umbracoContextFactory.EnsureUmbracoContext();
        var actions = await syncService.StartupExportAsync(workingFolder, new SyncHandlerOptions { Group = "Content" });
        return new(true, $"Exported {actions.Count()} items.");
    }
}
```

In `BaselineComposer.Compose`, add `builder.Services.AddTransient<ContentBootstrap>();` and
`builder.Services.AddTransient<BaselineExport>();`. In `BaselineApiController`, add:

```csharp
    [HttpPost("content")]
    public async Task<IActionResult> CreateContent()
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        return Ok(await services.GetRequiredService<ContentBootstrap>().RunAsync());
    }

    [HttpPost("export")]
    public async Task<IActionResult> Export()
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        var result = await services.GetRequiredService<BaselineExport>().RunAsync();
        return result.Succeeded ? Ok(result.Message) : Conflict(result.Message);
    }
```

- [ ] **Step 4: Create and export the baseline**

```bash
dotnet build src/KCC.Web/KCC.Web.csproj
cd src/KCC.Web && dotnet run --launch-profile Local
```

In a second terminal:

```bash
curl -sk -X POST https://localhost:58671/api/dev/baseline/content
curl -sk -X POST https://localhost:58671/api/dev/baseline/export
```

Expected: the first returns the created node names (8 pages, Site Settings, 3 folders, 17 taxonomy nodes, 2 status
pages); the second returns `Exported N items.` Stop the site and check `ls src/KCC.Web/uSync/v17/Content | wc -l`
(31 files). Open `https://localhost:58671/umbraco` before stopping if you want to see the tree.

- [ ] **Step 5: The baseline tests pass on a fresh database**

```bash
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj
```

Expected: every integration test passes.

- [ ] **Step 6: Commit the baseline**

```bash
git add src/KCC.Web/Features/DevTools/Baseline src/KCC.Web/uSync tests/KCC.IntegrationTests
git commit -m "Add the Baseline Site Tree and Its Export Endpoint"
```

- [ ] **Step 7: Remove the one-off bootstraps**

```bash
git rm -q src/KCC.Web/Features/DevTools/Baseline/SchemaBootstrap.cs \
  src/KCC.Web/Features/DevTools/Baseline/UiStringsBootstrap.cs \
  src/KCC.Web/Features/DevTools/Baseline/ContentBootstrap.cs \
  src/KCC.Web/Features/DevTools/Baseline/BaselineKeys.cs
```

Reduce `BaselineComposer.Compose` to `builder.Services.AddTransient<BaselineExport>();`, and replace
`BaselineApiController` with:

```csharp
using Microsoft.AspNetCore.Mvc;

namespace KCC.Web.Features.DevTools.Baseline;

[ApiController]
[Route("api/dev/baseline")]
public class BaselineApiController(IWebHostEnvironment environment, BaselineExport baselineExport) : ControllerBase
{
    [HttpPost("export")]
    public async Task<IActionResult> Export()
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        var result = await baselineExport.RunAsync();
        return result.Succeeded ? Ok(result.Message) : Conflict(result.Message);
    }
}
```

- [ ] **Step 8: Prove a fresh clone boots into the baseline**

```bash
rm -f src/KCC.Web/umbraco/Data/Umbraco.sqlite.db*
dotnet build KitchenCommandCenter.sln
cd src/KCC.Web && dotnet run --launch-profile Local
```

Expected: first boot installs, then uSync imports schema, dictionary and content. In the backoffice the tree holds
Home (with Recipes and Account), Site Settings and the three folders. `git status` shows no changes under
`src/KCC.Web/uSync` (the import writes nothing back). Stop the site.

- [ ] **Step 9: Commit**

```bash
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj
git add -A src/KCC.Web/Features/DevTools/Baseline
git commit -m "Remove the One-Off Baseline Bootstraps"
```

---

### Task 8: SSR and front-end cleanups

**Files:**
- Modify: `src/KCC.Web/Features/Ssr/VueSsrService.cs`
- Create: `tests/KCC.UnitTests/Features/Ssr/VueSsrServiceTests.cs`,
  `tests/KCC.ViteTests/Features/Components/ResourceStrings/ResourceString.test.ts`
- Modify: `src/KCC.Web/Features/Components/ResourceStrings/ResourceString.Component.vue`,
  `tests/KCC.ViteTests/Features/Pages/AddVariant/AddVariantView.test.ts` (one comment),
  `src/KCC.Web/Features/Utilities/StringExtensions.ts` and the seven `.stripTilde()` call sites

- [ ] **Step 1: Write the failing cache-key test**

Create `tests/KCC.UnitTests/Features/Ssr/VueSsrServiceTests.cs`:

```csharp
using System.Net;
using System.Text;
using KCC.Web.Features.Ssr;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace KCC.UnitTests.Features.Ssr;

public class VueSsrServiceTests
{
    [Test]
    public async Task RenderAsync_SameMarkup_CachesPreviewAndLiveSeparately()
    {
        var handler = new CountingHandler();
        var service = CreateService(handler);

        _ = await service.RenderAsync("<h/>", "<b/>", "<f/>", isPreview: false);
        _ = await service.RenderAsync("<h/>", "<b/>", "<f/>", isPreview: true);

        _ = await Assert.That(handler.Calls).IsEqualTo(2);
    }

    [Test]
    public async Task RenderAsync_RepeatedLiveRender_IsServedFromTheCache()
    {
        var handler = new CountingHandler();
        var service = CreateService(handler);

        _ = await service.RenderAsync("<h/>", "<b/>", "<f/>", isPreview: false);
        _ = await service.RenderAsync("<h/>", "<b/>", "<f/>", isPreview: false);

        _ = await Assert.That(handler.Calls).IsEqualTo(1);
    }

    private static VueSsrService CreateService(HttpMessageHandler handler)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("VueSsr")).Returns(() => new HttpClient(handler, disposeHandler: false) { BaseAddress = new Uri("http://ssr.test") });

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> { ["VueSsr:Enabled"] = "true" })
            .Build();

        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(e => e.EnvironmentName).Returns("Testing");

        return new VueSsrService(
            factory.Object,
            new MemoryCache(new MemoryCacheOptions()),
            configuration,
            environment.Object,
            NullLogger<VueSsrService>.Instance);
    }

    private sealed class CountingHandler : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"html\":\"<div></div>\",\"renderTime\":1}", Encoding.UTF8, "application/json"),
            });
        }
    }
}
```

```bash
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj -- --treenode-filter "/*/*/VueSsrServiceTests/*"
```

Expected: `RenderAsync_SameMarkup_CachesPreviewAndLiveSeparately` FAILS (1 call — the preview render hit the live
entry); the other passes.

- [ ] **Step 2: Key the cache on preview**

In `src/KCC.Web/Features/Ssr/VueSsrService.cs`, change the key line in `RenderAsync` to

```csharp
        var cacheKey = GenerateCacheKey(headerContent, bodyContent, footerContent, isPreview);
```

and replace `GenerateCacheKey` with:

```csharp
    private static string GenerateCacheKey(string header, string body, string footer, bool isPreview)
    {
        var combined = $"{(isPreview ? "preview" : "live")}\n{header}{body}{footer}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(combined));
        return $"ssr:{Convert.ToHexString(hashBytes)}";
    }
```

Re-run the filter from Step 1 — expected: 2 passed.

- [ ] **Step 3: Write the failing `<ResourceString>` test**

Create `tests/KCC.ViteTests/Features/Components/ResourceStrings/ResourceString.test.ts`:

```ts
import { describe, expect, it } from 'vitest'
import { defineComponent, h, provide } from 'vue'
import ResourceString from '~/Components/ResourceStrings/ResourceString.Component.vue'
import { resourceStringsKey } from '~/Components/ResourceStrings/UseResourceStrings'
import { renderSsr } from '../../../support/renderSsr'

const Host = defineComponent({
  props: { preview: Boolean, for: { type: String, default: 'SignIn' } },
  setup(props) {
    provide('isPreview', props.preview)
    provide(resourceStringsKey, { strings: { 'Login.SignIn': 'Sign in' }, prefix: 'Login' })
    return () => h(ResourceString, { for: props.for })
  },
})

describe('ResourceString', () => {
  it('renders the resolved value', async () => {
    expect(await renderSsr(Host)).toContain('Sign in')
  })

  it('falls back to the full key when the value is missing', async () => {
    expect(await renderSsr(Host, { for: 'Missing' })).toContain('Login.Missing')
  })

  it('adds no edit markers in preview', async () => {
    const html = await renderSsr(Host, { preview: true })

    expect(html).not.toContain('kcc-rs-editable')
    expect(html).not.toContain('data-resource-key')
  })
})
```

```bash
cd src/KCC.Web && yarn test ResourceString
```

Expected: "adds no edit markers in preview" FAILS.

- [ ] **Step 4: Drop the markers**

In `src/KCC.Web/Features/Components/ResourceStrings/ResourceString.Component.vue`, delete the line
`const isPreview = inject('isPreview', false)` and replace the template with:

```vue
<template>
  <component :is="props.as || 'span'">
    {{ resolvedValue }}
  </component>
</template>
```

In `tests/KCC.ViteTests/Features/Pages/AddVariant/AddVariantView.test.ts`, the comment near line 96 says the markers
render in preview mode; they no longer exist, so delete that comment line (and any continuation line of the same
comment). Re-run `yarn test ResourceString` — expected: 3 passed.

- [ ] **Step 5: Drop `.stripTilde()`**

Umbraco URLs never start with `~`. In `src/KCC.Web/Features/Utilities/StringExtensions.ts`, delete the
`stripTilde(): string` line from the `String` interface and the `String.prototype.stripTilde = …` assignment (three
lines). Then make these exact replacements:

| File | Before | After |
|---|---|---|
| `Components/Links/AppLink.Component.vue` | `:href="href.stripTilde()"` | `:href="href"` |
| `Components/RecipeSearch/RecipeSearchHeader.vue` | `:href="createRecipeUrl.stripTilde()"` | `:href="createRecipeUrl"` |
| `Components/Header/AppHeader.Component.vue` | `:href="homeUrl.stripTilde()"`, `logo.asset.url.stripTilde()`, `logoLight.asset.url.stripTilde()` | `:href="homeUrl"`, `logo.asset.url`, `logoLight.asset.url` |
| `Components/Header/MenuItem.vue` | `:href="item.url.stripTilde()"`, `:href="subLink.url?.stripTilde()"` | `:href="item.url"`, `:href="subLink.url"` |
| `Pages/AddVariant/AddVariantView.Component.vue` | `computed(() => props.recipeSlug.stripTilde())` | `computed(() => props.recipeSlug)` |
| `Pages/Account/Settings/AccountSettingsView.Component.vue` | `computed(() => props.backUrl.stripTilde())` | `computed(() => props.backUrl)` |
| `Pages/RecipeDetail/RecipeDetailView.Component.vue` | `` `${props.addVariantUrl.stripTilde()}?recipe=` `` | `` `${props.addVariantUrl}?recipe=` `` |

(paths under `src/KCC.Web/Features/`).

```bash
grep -rn "stripTilde" src/KCC.Web/Features tests/KCC.ViteTests || echo "none left"
```

Expected: `none left`.

- [ ] **Step 6: Verify the front end**

```bash
cd src/KCC.Web
yarn test
yarn type-check
yarn prettier --check Features/Components Features/Pages Features/Utilities ../../tests/KCC.ViteTests/Features/Components/ResourceStrings
yarn build:all
cd ../..
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj
```

Expected: all green. If Prettier flags a touched file, run `yarn prettier --write <that file>` and re-check.

- [ ] **Step 7: Commit**

```bash
git add -A src/KCC.Web/Features tests/KCC.UnitTests tests/KCC.ViteTests
git commit -m "Key the SSR Cache on Preview and Drop Kentico Front-End Remnants"
```

---

### Task 9: The chrome — layout, header and footer

**Files:**
- Modify: `src/KCC.Web/KCC.Web.csproj` (delete the `Unported slices` lines for `Features/_ViewImports.cshtml`,
  `Features/Components/Footer/Footer.cshtml`, `Features/Components/Header/Header.cshtml`,
  `Features/Components/Header/HeaderViewComponent.cs`, `Features/Components/Header/HeaderViewModel.cs`, and replace
  `<Content Remove="Features/Pages/**/*.cshtml" />` with one line per remaining unported page folder — see Step 1)
- Rewrite: `src/KCC.Web/Features/_ViewImports.cshtml`, `src/KCC.Web/Features/Pages/Shared/Layout.cshtml`,
  `src/KCC.Web/Features/Components/Header/{HeaderViewComponent.cs,HeaderViewModel.cs,Header.cshtml,AppHeader.Component.vue}`
- Create: `src/KCC.Web/Features/Components/Header/HeaderNavigation.cs`,
  `src/KCC.Web/Features/Components/Header/HeaderNav.cs`, `src/KCC.Web/Features/Components/Header/SiteSettingsQueries.cs`,
  `tests/KCC.UnitTests/Features/Components/Header/HeaderNavTests.cs`,
  `tests/KCC.ViteTests/Features/Components/Header/AppHeader.test.ts`
- Move: the two logos into `src/KCC.Web/Features/Components/Header/Assets/`; delete `src/KCC.Web/assets/`
- Modify: `src/KCC.Web/Program.cs`

**Interfaces:**
- Consumes: `IResourceStringProvider` (Task 6); generated `SiteSettings`, `NavLink`, `NavGroup` (Task 5).
- Produces: `ISiteSettingsQueries.GetHeaderNavigation() → HeaderNavigation(IReadOnlyList<NavEntry> Main, IReadOnlyList<NavEntry> Utility)`;
  `NavEntry(string DisplayText, string ShowWhen, NavTarget Link, IReadOnlyList<NavTarget> Links)` (a flat link has
  `Link`; a group has `Links`); `NavTarget(string DisplayText, string Url, string Target)`;
  `HeaderNav.Visible(IEnumerable<NavEntry>, bool isSignedIn) → IEnumerable<HeaderNavItem>`; AppHeader props
  `homeUrl`, `logoAlt`, `switchToLightLabel`, `switchToDarkLabel`, `mainNavItems`, `utilityNavItems`.

- [ ] **Step 1: Bring the chrome back into the build**

In the `Unported slices` group of `src/KCC.Web/KCC.Web.csproj`, delete the lines for `Features/_ViewImports.cshtml`,
`Features/Components/Footer/Footer.cshtml`, `Features/Components/Header/Header.cshtml`,
`Features/Components/Header/HeaderViewComponent.cs` and `Features/Components/Header/HeaderViewModel.cs`, and replace
`<Content Remove="Features/Pages/**/*.cshtml" />` with:

```xml
        <Content Remove="Features/Pages/Account/**/*.cshtml" />
        <Content Remove="Features/Pages/AddVariant/**/*.cshtml" />
        <Content Remove="Features/Pages/CreateRecipe/**/*.cshtml" />
        <Content Remove="Features/Pages/Error/**/*.cshtml" />
        <Content Remove="Features/Pages/Home/**/*.cshtml" />
        <Content Remove="Features/Pages/RecipeDetail/**/*.cshtml" />
        <Content Remove="Features/Pages/RecipeSearch/**/*.cshtml" />
        <Content Remove="Features/Pages/VariantDetail/**/*.cshtml" />
```

That returns `Pages/_ViewStart.cshtml`, `Pages/Shared/Layout.cshtml` and `Pages/Shared/Partials/Metadata.cshtml` to the
build.

- [ ] **Step 2: Write the failing navigation tests**

Create `tests/KCC.UnitTests/Features/Components/Header/HeaderNavTests.cs`:

```csharp
using KCC.Web.Features.Components.Header;

namespace KCC.UnitTests.Features.Components.Header;

public class HeaderNavTests
{
    private static readonly NavTarget Recipes = new("All Recipes", "/recipes/", null);

    [Test]
    [Arguments(HeaderNav.Always, false, true)]
    [Arguments(HeaderNav.Always, true, true)]
    [Arguments(HeaderNav.SignedIn, false, false)]
    [Arguments(HeaderNav.SignedIn, true, true)]
    [Arguments(HeaderNav.SignedOut, false, true)]
    [Arguments(HeaderNav.SignedOut, true, false)]
    public async Task Visible_HonoursShowWhen(string showWhen, bool isSignedIn, bool expected)
    {
        var entries = new[] { new NavEntry("Recipes", showWhen, Recipes, []) };

        _ = await Assert.That(HeaderNav.Visible(entries, isSignedIn).Any()).IsEqualTo(expected);
    }

    [Test]
    public async Task Visible_EmptyShowWhen_MeansAlways()
    {
        var entries = new[] { new NavEntry("Recipes", null, Recipes, []) };

        _ = await Assert.That(HeaderNav.Visible(entries, isSignedIn: true).Count()).IsEqualTo(1);
        _ = await Assert.That(HeaderNav.Visible(entries, isSignedIn: false).Count()).IsEqualTo(1);
    }

    [Test]
    public async Task Visible_FlatLink_CarriesUrlAndTarget()
    {
        var entries = new[] { new NavEntry("Login", HeaderNav.SignedOut, new NavTarget("Login", "/account/login/", "_self"), []) };

        var item = HeaderNav.Visible(entries, isSignedIn: false).Single();

        _ = await Assert.That(item.DisplayText).IsEqualTo("Login");
        _ = await Assert.That(item.Url).IsEqualTo("/account/login/");
        _ = await Assert.That(item.Target).IsEqualTo("_self");
        _ = await Assert.That(item.SubLinks).IsNull();
    }

    [Test]
    public async Task Visible_Group_CarriesItsLinksInOrder()
    {
        var entries = new[]
        {
            new NavEntry("Account", HeaderNav.SignedIn, null, [new("Profile", "/account/", null), new("Logout", "/account/logout", null)]),
        };

        var item = HeaderNav.Visible(entries, isSignedIn: true).Single();

        _ = await Assert.That(item.Url).IsNull();
        _ = await Assert.That(string.Join(",", item.SubLinks.Select(link => link.DisplayText))).IsEqualTo("Profile,Logout");
    }
}
```

```bash
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj -- --treenode-filter "/*/*/HeaderNavTests/*"
```

Expected: build error — `NavEntry`, `NavTarget`, `HeaderNav` do not exist.

- [ ] **Step 3: Write the navigation model and filter**

Create `src/KCC.Web/Features/Components/Header/HeaderNavigation.cs`:

```csharp
namespace KCC.Web.Features.Components.Header;

public sealed record HeaderNavigation(IReadOnlyList<NavEntry> Main, IReadOnlyList<NavEntry> Utility)
{
    public static HeaderNavigation Empty { get; } = new([], []);
}

public sealed record NavEntry(string DisplayText, string ShowWhen, NavTarget Link, IReadOnlyList<NavTarget> Links);

public sealed record NavTarget(string DisplayText, string Url, string Target);
```

Create `src/KCC.Web/Features/Components/Header/HeaderNav.cs`:

```csharp
using KCC.Web.Features.Models.Common;

namespace KCC.Web.Features.Components.Header;

public static class HeaderNav
{
    public const string Always = "Always";
    public const string SignedIn = "Signed in";
    public const string SignedOut = "Signed out";

    public static IEnumerable<HeaderNavItem> Visible(IEnumerable<NavEntry> entries, bool isSignedIn) =>
        entries.Where(entry => IsVisible(entry.ShowWhen, isSignedIn)).Select(ToHeaderNavItem);

    private static bool IsVisible(string showWhen, bool isSignedIn) => showWhen switch
    {
        SignedIn => isSignedIn,
        SignedOut => !isSignedIn,
        _ => true,
    };

    private static HeaderNavItem ToHeaderNavItem(NavEntry entry) => entry.Link is { } link
        ? new HeaderNavItem { DisplayText = entry.DisplayText, Url = link.Url, Target = link.Target }
        : new HeaderNavItem
        {
            DisplayText = entry.DisplayText,
            SubLinks = entry.Links.Select(target => new PageLink { DisplayText = target.DisplayText, Url = target.Url, Target = target.Target }).ToList(),
        };
}
```

Replace `src/KCC.Web/Features/Components/Header/HeaderViewModel.cs` with:

```csharp
using KCC.Web.Features.Models.Common;

namespace KCC.Web.Features.Components.Header;

public class HeaderViewModel
{
    public string LogoAlt { get; set; }

    /// <summary>
    /// Gets or sets the theme-toggle labels. Resolved server-side because the header renders on every
    /// page, while the Vue ResourceString dict is assembled per page controller.
    /// </summary>
    public string SwitchToLightLabel { get; set; }

    public string SwitchToDarkLabel { get; set; }

    public IEnumerable<HeaderNavItem> MainNavItems { get; set; }

    public IEnumerable<HeaderNavItem> UtilityNavItems { get; set; }
}

public class HeaderNavItem
{
    public string DisplayText { get; set; }

    /// <summary>
    /// Gets or sets the direct-link URL for a flat entry. Null when this entry is a group with
    /// <see cref="SubLinks"/>.
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// Gets or sets the link target (e.g. <c>_self</c>, <c>_blank</c>) paired with
    /// <see cref="Url"/>. Null when this entry is a dropdown.
    /// </summary>
    public string Target { get; set; }

    /// <summary>
    /// Gets or sets the dropdown children for a group entry. Null when this entry is a flat link
    /// (see <see cref="Url"/>).
    /// </summary>
    public IEnumerable<PageLink> SubLinks { get; set; }
}
```

Re-run the filter from Step 2 — expected: all `HeaderNavTests` pass.

- [ ] **Step 4: Read the navigation from Site Settings**

Create `src/KCC.Web/Features/Components/Header/SiteSettingsQueries.cs`:

```csharp
using KCC.Web.Features.Models.Generated;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Blocks;

namespace KCC.Web.Features.Components.Header;

public interface ISiteSettingsQueries
{
    HeaderNavigation GetHeaderNavigation();
}

public class SiteSettingsQueries(IPublishedContentQuery contentQuery) : ISiteSettingsQueries
{
    public HeaderNavigation GetHeaderNavigation()
    {
        var settings = contentQuery.ContentAtRoot().OfType<SiteSettings>().FirstOrDefault();
        return settings is null
            ? HeaderNavigation.Empty
            : new HeaderNavigation(Entries(settings.MainNav), Entries(settings.UtilityNav));
    }

    private static List<NavEntry> Entries(BlockListModel blocks) =>
        (blocks ?? Enumerable.Empty<BlockListItem>())
            .Select(block => block.Content switch
            {
                NavLink link => new NavEntry(link.DisplayText ?? string.Empty, link.ShowWhen, Target(link.Link), []),
                NavGroup group => new NavEntry(
                    group.DisplayText ?? string.Empty,
                    group.ShowWhen,
                    null,
                    (group.Links ?? []).Select(Target).Where(target => target is not null).ToList()),
                _ => null,
            })
            .Where(entry => entry is not null)
            .ToList();

    private static NavTarget Target(Link link) =>
        link?.Url is { Length: > 0 } url ? new NavTarget(link.Name ?? string.Empty, url, link.Target) : null;
}
```

A `navLink` shows its own display text, so `link.Name` only matters inside groups.

- [ ] **Step 5: Rewrite the header view component and view**

Replace `src/KCC.Web/Features/Components/Header/HeaderViewComponent.cs`:

```csharp
using KCC.Web.Features.Dictionary;
using Microsoft.AspNetCore.Mvc;

namespace KCC.Web.Features.Components.Header;

public class HeaderViewComponent(ISiteSettingsQueries siteSettings, IResourceStringProvider resourceStrings) : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        var navigation = siteSettings.GetHeaderNavigation();
        var isSignedIn = User.Identity?.IsAuthenticated == true;

        var viewModel = new HeaderViewModel
        {
            LogoAlt = resourceStrings.GetOrDefault("Shared.LogoAlt"),
            SwitchToLightLabel = resourceStrings.GetOrDefault("Theme.SwitchToLight"),
            SwitchToDarkLabel = resourceStrings.GetOrDefault("Theme.SwitchToDark"),
            MainNavItems = HeaderNav.Visible(navigation.Main, isSignedIn).ToList(),
            UtilityNavItems = HeaderNav.Visible(navigation.Utility, isSignedIn).ToList(),
        };

        return View("~/Features/Components/Header/Header.cshtml", viewModel);
    }
}
```

Replace `src/KCC.Web/Features/Components/Header/Header.cshtml`:

```cshtml
@using KCC.Web.Features.Components.Header

@model HeaderViewModel

<AppHeader
  home-url="/"
  logo-alt="@Model.LogoAlt"
  switch-to-light-label="@Model.SwitchToLightLabel"
  switch-to-dark-label="@Model.SwitchToDarkLabel"
  :main-nav-items="@Vue.Prop(Model.MainNavItems)"
  :utility-nav-items="@Vue.Prop(Model.UtilityNavItems)"
/>
```

In `src/KCC.Web/Program.cs`, add `using KCC.Web.Features.Components.Header;` (sorted) and, after the
`IResourceStringProvider` registration:

```csharp
builder.Services.AddScoped<ISiteSettingsQueries, SiteSettingsQueries>();
```

- [ ] **Step 6: Make the logo a static brand asset**

The dark ramp shows the bone mark (Xperience's "LogoInverse"); the light ramp shows the base mark.

```bash
mkdir -p src/KCC.Web/Features/Components/Header/Assets
git mv src/KCC.Web/assets/contentitems/f6/f67d70e7-a891-4e11-a308-620ade181341/3571d6da-e0ca-404f-ac44-f170a5e108b1/af13c859-c867-4cf0-b9d5-487cd473af4b.webp \
  src/KCC.Web/Features/Components/Header/Assets/logo-on-dark.webp
git mv src/KCC.Web/assets/contentitems/3e/3e10d95b-9184-4f95-bdb8-d19d142c77aa/3571d6da-e0ca-404f-ac44-f170a5e108b1/5e05004d-2c7e-4443-a576-498df0796cc6.webp \
  src/KCC.Web/Features/Components/Header/Assets/logo-on-light.webp
git rm -r -q src/KCC.Web/assets
```

- [ ] **Step 7: Write the failing AppHeader test**

Create `tests/KCC.ViteTests/Features/Components/Header/AppHeader.test.ts`:

```ts
import { describe, expect, it } from 'vitest'
import AppHeader from '~/Components/Header/AppHeader.Component.vue'
import { renderSsr } from '../../../support/renderSsr'

const props = {
  homeUrl: '/',
  logoAlt: 'Kitchen Command Center',
  switchToLightLabel: 'Switch to light',
  switchToDarkLabel: 'Switch to dark',
  mainNavItems: [],
  utilityNavItems: [],
}

describe('AppHeader', () => {
  it('renders one mark per ramp with the same alt text', async () => {
    const html = await renderSsr(AppHeader, props)

    expect(html).toMatch(/<img[^>]*data-ramp="dark"[^>]*alt="Kitchen Command Center"/)
    expect(html).toMatch(/<img[^>]*data-ramp="light"[^>]*alt="Kitchen Command Center"/)
  })

  it('links the mark to the home page', async () => {
    expect(await renderSsr(AppHeader, props)).toContain('href="/"')
  })
})
```

```bash
cd src/KCC.Web && yarn test AppHeader
```

Expected: FAIL — the component still expects `logo` props.

- [ ] **Step 8: Rewrite AppHeader's logo**

In `src/KCC.Web/Features/Components/Header/AppHeader.Component.vue`:
1. Delete the `interface ImageItem { … }` block.
2. In `AppHeaderProps`, replace the `logo: ImageItem` property and the documented `logoLight?: ImageItem` property with:

```ts
    /** Alt text for the mark, resolved server-side from the dictionary. */
    logoAlt: string
```

3. In `<script setup>`, add the imports and update the destructuring:

```ts
  import logoOnDark from '~/Components/Header/Assets/logo-on-dark.webp'
  import logoOnLight from '~/Components/Header/Assets/logo-on-light.webp'
  import ThemeToggle from '~/Components/Theme/ThemeToggle.vue'
  const { homeUrl, logoAlt, switchToLightLabel, switchToDarkLabel, mainNavItems, utilityNavItems } =
    defineProps<AppHeaderProps>()
```

4. Replace the two `<img … />` elements inside the logo link with:

```vue
        <img
          data-ramp="dark"
          loading="eager"
          :src="logoOnDark"
          :alt="logoAlt"
          class="h-12 w-auto sm:h-16"
          height="64"
          width="90"
        />
        <img
          data-ramp="light"
          loading="eager"
          :src="logoOnLight"
          :alt="logoAlt"
          class="h-12 w-auto sm:h-16"
          height="64"
          width="90"
        />
```

The ramp-swap rule at the end of `Features/Styles/Torn/Kit.css` shows only the element whose `data-ramp` matches.

Re-run `yarn test AppHeader` — expected: 2 passed.

- [ ] **Step 9: Rewrite the view imports and the layout**

Replace `src/KCC.Web/Features/_ViewImports.cshtml`:

```cshtml
@using System.IO;
@using Microsoft.AspNetCore.Hosting;
@using Microsoft.Extensions.Hosting;
@using KCC.Web;
@using KCC.Web.Features.Extensions;
@using KCC.Web.Features.Helpers;
@using KCC.Web.Features.Models.Constants;
@using KCC.Web.Features.Pages.Shared;
@using KCC.Web.Features.Ssr;

@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@addTagHelper *, KCC.Web
@addTagHelper *, Vite.AspNetCore
```

In `src/KCC.Web/Features/Pages/Shared/Layout.cshtml`:
1. Replace the using/inject header (lines 1–12) with:

```cshtml
@using KCC.Web.Features.Components.Footer
@using KCC.Web.Features.Components.Header
@using KCC.Web.Features.Dictionary
@using Microsoft.AspNetCore.Hosting
@using Umbraco.Cms.Core.Web

@inject IWebHostEnvironment Env
@inject VueSsrService VueSsrService
@inject IUmbracoContextAccessor UmbracoContextAccessor
@inject Microsoft.AspNetCore.Antiforgery.IAntiforgery Antiforgery
@inject IResourceStringProvider ResourceStrings
@inject Vite.AspNetCore.IViteManifest ViteManifest
@inject Vite.AspNetCore.IViteDevServerStatus ViteDevServer
```

2. At the end of the opening `@{ … }` block (after the `apiConfigJson` statement), add:

```csharp
    var isPreview = UmbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext) && umbracoContext.InPreviewMode;
```

3. Replace `<html lang="@PreferredLanguageRetriever.Get()" data-theme="light">` with `<html lang="en" data-theme="light">`.
4. In the `RenderVueSsrAsync(…)` call, replace `Context.IsPreview() || Context.IsPageBuilder(),` with `isPreview,`.
5. Delete the `<resource-string-editor />` line.

- [ ] **Step 10: Verify**

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj
cd src/KCC.Web
yarn test
yarn type-check
yarn prettier --check Features/Components/Header ../../tests/KCC.ViteTests/Features/Components/Header
yarn build:all
cd ../..
```

Expected: all green (if Prettier flags a touched file, `yarn prettier --write` it and re-check). No page renders
yet — the home controller arrives in Task 10.

- [ ] **Step 11: Commit**

```bash
git add -A src/KCC.Web tests/KCC.UnitTests tests/KCC.ViteTests
git commit -m "Port the Layout, Header and Footer to Umbraco"
```

---

### Task 10: The home page

The first page through the whole path: Umbraco routing → hijacked `HomePageController` → existing view → layout →
SSR. Its integration test is spec §19's first proof: a document type with no template renders through its
controller.

**Files:**
- Modify: `src/KCC.Web/KCC.Web.csproj` (delete the `Features/Pages/Home/HomeController.cs`,
  `Features/Pages/Home/**/*.cshtml` and `Features/Pages/Shared/PageMappingExtensions.cs` lines)
- Delete: `src/KCC.Web/Features/Pages/Home/HomeController.cs`, `src/KCC.Web/Features/Pages/Shared/PageMappingExtensions.cs`
- Create: `src/KCC.Web/Features/Pages/Home/HomePageController.cs`, `src/KCC.Web/Features/Pages/Shared/PageMetadata.cs`,
  `tests/KCC.UnitTests/Features/Pages/Shared/PageMetadataTests.cs`,
  `tests/KCC.IntegrationTests/Features/Pages/HomePageTests.cs`
- Modify: `src/KCC.Web/Features/Pages/Home/Index.cshtml`, `src/KCC.Web/Features/Pages/Shared/BasePageViewModel.cs`,
  `src/KCC.Web/Program.cs`

**Interfaces:**
- Produces: `PageMetadata.Apply(IMetadata page, BasePageViewModel viewModel)` (registered scoped) — every later page
  controller calls it. Title falls back to the node name; `PublishDate` is the node's create date in ISO 8601.

- [ ] **Step 1: Bring the home slice back and drop the Kentico mapping**

Delete the three `Unported slices` lines named above, then:

```bash
git rm -q src/KCC.Web/Features/Pages/Home/HomeController.cs src/KCC.Web/Features/Pages/Shared/PageMappingExtensions.cs
```

In `src/KCC.Web/Features/Pages/Shared/BasePageViewModel.cs`, delete `public int WebPageItemID { get; set; }` (a
Kentico concept nothing reads).

- [ ] **Step 2: Write the failing metadata tests**

Create `tests/KCC.UnitTests/Features/Pages/Shared/PageMetadataTests.cs`:

```csharp
using KCC.Web.Features.Models.Generated;
using KCC.Web.Features.Pages.Shared;
using Moq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors.ValueConverters;
using Umbraco.Cms.Core.Routing;

namespace KCC.UnitTests.Features.Pages.Shared;

public class PageMetadataTests
{
    [Test]
    public async Task Apply_WithoutMetadataTitle_FallsBackToTheNodeName()
    {
        var viewModel = Apply(Page(title: string.Empty));

        _ = await Assert.That(viewModel.Title).IsEqualTo("Home");
    }

    [Test]
    public async Task Apply_WithMetadataTitle_UsesIt()
    {
        var viewModel = Apply(Page(title: "Kitchen Command Center"));

        _ = await Assert.That(viewModel.Title).IsEqualTo("Kitchen Command Center");
    }

    [Test]
    public async Task Apply_PublishDate_IsTheCreateDateInIso8601()
    {
        var viewModel = Apply(Page(title: string.Empty));

        _ = await Assert.That(viewModel.PublishDate).IsEqualTo("2026-09-23T10:00:00.0000000Z");
    }

    [Test]
    public async Task Apply_WithImage_ResolvesItsUrlAndSize()
    {
        var media = new Mock<IPublishedContent>();
        media.Setup(m => m.GetProperty("umbracoWidth")).Returns(Property(1200));
        media.Setup(m => m.GetProperty("umbracoHeight")).Returns(Property(630));
        var image = new MediaWithCrops(media.Object, Mock.Of<IPublishedValueFallback>(), new ImageCropperValue());

        var urls = new Mock<IPublishedUrlProvider>();
        urls.Setup(u => u.GetMediaUrl(media.Object, It.IsAny<UrlMode>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Uri>()))
            .Returns("/media/og.webp");

        var page = Page(title: string.Empty);
        page.SetupGet(p => p.MetadataImage).Returns(image);
        var viewModel = new BasePageViewModel();
        new PageMetadata(urls.Object).Apply(page.Object, viewModel);

        _ = await Assert.That(viewModel.ImagePath).IsEqualTo("/media/og.webp");
        _ = await Assert.That(viewModel.ImageWidth).IsEqualTo(1200);
        _ = await Assert.That(viewModel.ImageHeight).IsEqualTo(630);
    }

    [Test]
    public async Task Apply_WithoutImages_LeavesImagePathsEmpty()
    {
        var viewModel = Apply(Page(title: string.Empty));

        _ = await Assert.That(viewModel.ImagePath).IsNull();
        _ = await Assert.That(viewModel.TwitterImagePath).IsNull();
    }

    private static BasePageViewModel Apply(Mock<IMetadata> page)
    {
        var viewModel = new BasePageViewModel();
        new PageMetadata(Mock.Of<IPublishedUrlProvider>()).Apply(page.Object, viewModel);
        return viewModel;
    }

    private static Mock<IMetadata> Page(string title)
    {
        var page = new Mock<IMetadata>();
        page.SetupGet(p => p.Name).Returns("Home");
        page.SetupGet(p => p.MetadataTitle).Returns(title);
        page.SetupGet(p => p.CreateDate).Returns(new DateTime(2026, 9, 23, 10, 0, 0, DateTimeKind.Utc));
        return page;
    }

    private static IPublishedProperty Property(int value)
    {
        var property = new Mock<IPublishedProperty>();
        property.Setup(p => p.GetValue(It.IsAny<string>(), It.IsAny<string>())).Returns(value);
        return property.Object;
    }
}
```

```bash
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj -- --treenode-filter "/*/*/PageMetadataTests/*"
```

Expected: build error — `PageMetadata` does not exist. (If `MediaWithCrops` or `ImageCropperValue` live in a different
namespace in the installed Umbraco, fix the using from the compiler's suggestion.)

- [ ] **Step 3: Write `PageMetadata`**

Create `src/KCC.Web/Features/Pages/Shared/PageMetadata.cs`:

```csharp
using System.Globalization;
using KCC.Web.Features.Models.Generated;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;

namespace KCC.Web.Features.Pages.Shared;

public class PageMetadata(IPublishedUrlProvider urlProvider)
{
    public void Apply(IMetadata page, BasePageViewModel viewModel)
    {
        viewModel.Title = string.IsNullOrWhiteSpace(page.MetadataTitle) ? page.Name : page.MetadataTitle;
        viewModel.Description = page.MetadataDescription;
        viewModel.Keywords = page.MetadataKeywords;
        viewModel.PublishDate = page.CreateDate.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);
        viewModel.ShowBreadcrumbs = page.ShowBreadcrumbs;
        viewModel.TwitterCard = page.TwitterCard;
        viewModel.TwitterSite = page.TwitterSite;
        viewModel.TwitterCreator = page.TwitterCreator;

        if (page.MetadataImage is { } image)
        {
            viewModel.ImagePath = urlProvider.GetMediaUrl(image.Content);
            viewModel.ImageWidth = Dimension(image.Content, "umbracoWidth");
            viewModel.ImageHeight = Dimension(image.Content, "umbracoHeight");
        }

        if (page.TwitterImage is { } twitterImage)
        {
            viewModel.TwitterImagePath = urlProvider.GetMediaUrl(twitterImage.Content);
        }
    }

    private static int Dimension(IPublishedContent media, string alias) =>
        media.GetProperty(alias)?.GetValue() is int value ? value : 0;
}
```

Re-run the filter — expected: 5 passed. (`MediaWithCrops.Content` is the wrapped media node; if the generated
`MetadataImage` is typed `IPublishedContent` rather than `MediaWithCrops`, pass it directly and drop `.Content`.)

- [ ] **Step 4: Write the failing page test**

Create `tests/KCC.IntegrationTests/Features/Pages/HomePageTests.cs`:

```csharp
using System.Net;
using KCC.IntegrationTests.Config;

namespace KCC.IntegrationTests.Features.Pages;

public class HomePageTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task Home_RendersThroughItsControllerWithoutATemplate()
    {
        using var client = Site.CreateClient();
        using var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        _ = await Assert.That(html.Contains("<title>Kitchen Command Center</title>", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(html.Contains("<script id=\"server-content\" type=\"application/json\">", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Home_HeaderShowsTheSignedOutNavigation()
    {
        using var client = Site.CreateClient();
        var html = await client.GetStringAsync("/");

        _ = await Assert.That(html.Contains("All Recipes", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(html.Contains("Login", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(html.Contains("Profile", StringComparison.Ordinal)).IsFalse();
    }
}
```

- [ ] **Step 5: Write the controller and view, then run**

Create `src/KCC.Web/Features/Pages/Home/HomePageController.cs`:

```csharp
using KCC.Web.Features.Models.Generated;
using KCC.Web.Features.Pages.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace KCC.Web.Features.Pages.Home;

public class HomePageController(
    ILogger<RenderController> logger,
    ICompositeViewEngine compositeViewEngine,
    IUmbracoContextAccessor umbracoContextAccessor,
    PageMetadata pageMetadata)
    : RenderController(logger, compositeViewEngine, umbracoContextAccessor)
{
    public override IActionResult Index()
    {
        var viewModel = new HomeViewModel();
        pageMetadata.Apply((HomePage)CurrentPage, viewModel);

        return View("~/Features/Pages/Home/Index.cshtml", viewModel);
    }
}
```

`src/KCC.Web/Features/Pages/Home/Index.cshtml` keeps only:

```cshtml
@using KCC.Web.Features.Pages.Home

@model HomeViewModel
```

In `src/KCC.Web/Program.cs`, add `using KCC.Web.Features.Pages.Shared;` (sorted) and
`builder.Services.AddScoped<PageMetadata>();` after the `ISiteSettingsQueries` registration.

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/HomePageTests/*"
```

Expected: 2 passed.

**If `/` answers 404** (route hijacking did not serve the template-less node), take spec §19's fallback — one shared,
empty template that only marks nodes as routable; each controller still returns its own view by path:

1. Run the dev site (`cd src/KCC.Web && dotnet run --launch-profile Local`) and sign in to `/umbraco`.
2. Settings → Templates → **Create** → name it `Page` (alias `page`), replace its body with `@{ Layout = null; }`, and
   **Save**. Umbraco writes `src/KCC.Web/Views/Page.cshtml`; uSync writes `uSync/v17/Templates/page.config`.
3. For each of `homePage`, `recipeListingPage`, `createRecipePage`, `addVariantPage`, `accountPage`, `loginPage`,
   `accountSettingsPage`, `registrationCompletePage` and `statusCodePage`: Settings → Document Types → the type →
   **Templates** → allow `Page` and make it the default → **Save**. uSync re-exports each type.
4. Assign the template to the existing baseline nodes: Content → each of the pages above → Info → Template → `Page` →
   **Save and Publish**, then `curl -sk -X POST https://localhost:58671/api/dev/baseline/export` to refresh the
   baseline.
5. Stop the site, re-run the filter above until it passes, commit `Views/Page.cshtml` and the uSync changes with the
   task, and record the fallback in the spec's §19. Later phases give every new routable type the same template.

- [ ] **Step 6: Look at it**

```bash
cd src/KCC.Web && dotnet watch --non-interactive
```

Open `https://localhost:58671/` in both ramps (toggle in the header): the torn-paper header with the nav, an empty body,
the footer. The **Recipes** menu opens to All Recipes and Create Recipe (both 404 until later phases). Stop the site.

- [ ] **Step 7: Commit**

```bash
git add -A src/KCC.Web tests/KCC.UnitTests tests/KCC.IntegrationTests
git commit -m "Serve the Home Page from Umbraco"
```

---

### Task 11: 404 and 500 pages from content

**Files:**
- Modify: `src/KCC.Web/KCC.Web.csproj` (delete the `Features/Pages/Error/ErrorController.cs` and
  `Features/Pages/Error/**/*.cshtml` lines)
- Create: `src/KCC.Web/Features/Pages/Error/StatusCodePages.cs`, `NotFoundContentFinder.cs`, `ErrorComposer.cs`,
  `StatusCodePageController.cs`; `tests/KCC.UnitTests/Features/Pages/Error/ErrorViewModelTests.cs`;
  `tests/KCC.IntegrationTests/Features/Pages/ErrorPageTests.cs`
- Rewrite: `src/KCC.Web/Features/Pages/Error/ErrorController.cs`
- Modify: `src/KCC.Web/Features/Pages/Error/ErrorViewModel.cs`, `src/KCC.Web/Program.cs`

**Interfaces:**
- Produces: `IStatusCodePages.Find(int statusCode) → StatusCodePage` (singleton-safe: only singleton Umbraco services),
  `ErrorViewModel.For(int statusCode, string heading, string body)`.

- [ ] **Step 1: Write the failing view-model tests**

Create `tests/KCC.UnitTests/Features/Pages/Error/ErrorViewModelTests.cs`:

```csharp
using KCC.Web.Features.Pages.Error;

namespace KCC.UnitTests.Features.Pages.Error;

public class ErrorViewModelTests
{
    [Test]
    public async Task For_WithContent_UsesIt()
    {
        var viewModel = ErrorViewModel.For(404, "We couldn't find that page", "<p>Gone.</p>");

        _ = await Assert.That(viewModel.StatusCode).IsEqualTo(404);
        _ = await Assert.That(viewModel.Heading).IsEqualTo("We couldn't find that page");
        _ = await Assert.That(viewModel.Title).IsEqualTo("We couldn't find that page");
        _ = await Assert.That(viewModel.Body).IsEqualTo("<p>Gone.</p>");
    }

    [Test]
    public async Task For_WithoutContent_FallsBackToGenericText()
    {
        var viewModel = ErrorViewModel.For(500, null, null);

        _ = await Assert.That(viewModel.Heading).IsEqualTo("Error");
        _ = await Assert.That(viewModel.Title).IsEqualTo("Error");
        _ = await Assert.That(viewModel.Body).IsEqualTo("An unexpected error occurred.");
    }
}
```

Run with the `ErrorViewModelTests` filter — expected: build error, `For` missing.

- [ ] **Step 2: Add `For`**

Replace `src/KCC.Web/Features/Pages/Error/ErrorViewModel.cs` with:

```csharp
using KCC.Web.Features.Pages.Shared;

namespace KCC.Web.Features.Pages.Error;

public class ErrorViewModel : BasePageViewModel
{
    public int StatusCode { get; set; }

    public string Heading { get; set; }

    public string Body { get; set; }

    public static ErrorViewModel For(int statusCode, string heading, string body)
    {
        var resolvedHeading = string.IsNullOrWhiteSpace(heading) ? "Error" : heading;
        return new ErrorViewModel
        {
            StatusCode = statusCode,
            Heading = resolvedHeading,
            Title = resolvedHeading,
            Body = string.IsNullOrWhiteSpace(body) ? "An unexpected error occurred." : body,
        };
    }
}
```

Re-run — expected: 2 passed.

- [ ] **Step 3: Write the failing page tests**

Create `tests/KCC.IntegrationTests/Features/Pages/ErrorPageTests.cs`:

```csharp
using System.Net;
using KCC.IntegrationTests.Config;

namespace KCC.IntegrationTests.Features.Pages;

public class ErrorPageTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task UnknownUrl_RendersThe404PageWithA404Status()
    {
        using var client = Site.CreateClient();
        using var response = await client.GetAsync("/this-page-does-not-exist");
        var html = await response.Content.ReadAsStringAsync();

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        _ = await Assert.That(html.Contains("find that page", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task ErrorRoute_RendersThe500PageWithA500Status()
    {
        using var client = Site.CreateClient();
        using var response = await client.GetAsync("/error");
        var html = await response.Content.ReadAsStringAsync();

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.InternalServerError);
        _ = await Assert.That(html.Contains("Something went wrong", StringComparison.Ordinal)).IsTrue();
    }
}
```

- [ ] **Step 4: Write the lookup, finder, controllers and composer**

Create `src/KCC.Web/Features/Pages/Error/StatusCodePages.cs`:

```csharp
using KCC.Web.Features.Models.Generated;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services.Navigation;

namespace KCC.Web.Features.Pages.Error;

public interface IStatusCodePages
{
    StatusCodePage Find(int statusCode);
}

// Registered as a singleton because the last-chance content finder is one; both dependencies are singletons too.
public class StatusCodePages(IPublishedContentCache contentCache, IDocumentNavigationQueryService navigation) : IStatusCodePages
{
    public StatusCodePage Find(int statusCode)
    {
        if (!navigation.TryGetRootKeys(out var rootKeys))
        {
            return null;
        }

        foreach (var rootKey in rootKeys)
        {
            if (contentCache.GetById(rootKey) is not ContentFolder folder || !navigation.TryGetChildrenKeys(folder.Key, out var childKeys))
            {
                continue;
            }

            foreach (var childKey in childKeys)
            {
                if (contentCache.GetById(childKey) is StatusCodePage page && page.StatusCode == statusCode)
                {
                    return page;
                }
            }
        }

        return null;
    }
}
```

Create `src/KCC.Web/Features/Pages/Error/NotFoundContentFinder.cs`:

```csharp
using Umbraco.Cms.Core.Routing;

namespace KCC.Web.Features.Pages.Error;

public class NotFoundContentFinder(IStatusCodePages statusCodePages) : IContentLastChanceFinder
{
    public Task<bool> TryFindContent(IPublishedRequestBuilder request)
    {
        var page = statusCodePages.Find(StatusCodes.Status404NotFound);
        if (page is null)
        {
            return Task.FromResult(false);
        }

        // The router has already flagged the request as a 404; supplying content only chooses what renders.
        request.SetPublishedContent(page);
        return Task.FromResult(true);
    }
}
```

Create `src/KCC.Web/Features/Pages/Error/ErrorComposer.cs`:

```csharp
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Extensions;

namespace KCC.Web.Features.Pages.Error;

public class ErrorComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IStatusCodePages, StatusCodePages>();
        builder.SetContentLastChanceFinder<NotFoundContentFinder>();
    }
}
```

Create `src/KCC.Web/Features/Pages/Error/StatusCodePageController.cs`:

```csharp
using KCC.Web.Features.Models.Generated;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace KCC.Web.Features.Pages.Error;

public class StatusCodePageController(
    ILogger<RenderController> logger,
    ICompositeViewEngine compositeViewEngine,
    IUmbracoContextAccessor umbracoContextAccessor)
    : RenderController(logger, compositeViewEngine, umbracoContextAccessor)
{
    public override IActionResult Index()
    {
        var page = (StatusCodePage)CurrentPage;
        Response.StatusCode = page.StatusCode;

        return View("~/Features/Pages/Error/Index.cshtml", ErrorViewModel.For(page.StatusCode, page.Heading, page.Body?.ToHtmlString()));
    }
}
```

Replace `src/KCC.Web/Features/Pages/Error/ErrorController.cs` with:

```csharp
using Microsoft.AspNetCore.Mvc;

namespace KCC.Web.Features.Pages.Error;

[Route("error")]
public class ErrorController(IStatusCodePages statusCodePages) : Controller
{
    // No verb attribute: the exception handler re-executes with the failed request's method, POST included.
    [Route("")]
    [Route("{statusCode:int}")]
    public IActionResult Index(int statusCode = StatusCodes.Status500InternalServerError)
    {
        var page = statusCodePages.Find(statusCode);
        Response.StatusCode = statusCode;

        return View("~/Features/Pages/Error/Index.cshtml", ErrorViewModel.For(statusCode, page?.Heading, page?.Body?.ToHtmlString()));
    }
}
```

In `src/KCC.Web/Program.cs`, after `await app.BootUmbracoAsync();`:

```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
}
```

Delete the two error lines from the `Unported slices` group.

- [ ] **Step 5: Run**

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj
```

Expected: all pass.

- [ ] **Step 6: Commit**

```bash
git add -A src/KCC.Web tests/KCC.UnitTests tests/KCC.IntegrationTests
git commit -m "Serve 404 and 500 Pages from Umbraco Content"
```

---

### Task 12: Sitemap and robots.txt

**Files:**
- Modify: `src/KCC.Web/KCC.Web.csproj` (delete the `Features/Sitemap/**` line), `src/KCC.Web/Program.cs`,
  `src/KCC.Web/Features/Sitemap/RobotsTxtProvider.cs`
- Rewrite: `src/KCC.Web/Features/Sitemap/SitemapController.cs`
- Create: `src/KCC.Web/Features/Sitemap/SitemapPages.cs`, `tests/KCC.UnitTests/Features/Sitemap/SitemapFilterTests.cs`,
  `tests/KCC.UnitTests/Features/Sitemap/RobotsTxtProviderTests.cs`, `tests/KCC.IntegrationTests/Features/Pages/SitemapTests.cs`

**Interfaces:**
- Produces: `SitemapFilter.Urls(IEnumerable<SitemapCandidate>) → IEnumerable<string>`,
  `SitemapCandidate(string Url, string ContentTypeAlias, bool ExcludeFromSitemap)`, `ISitemapPages.Urls()`.

- [ ] **Step 1: Write the failing unit tests**

Create `tests/KCC.UnitTests/Features/Sitemap/SitemapFilterTests.cs`:

```csharp
using KCC.Web.Features.Sitemap;

namespace KCC.UnitTests.Features.Sitemap;

public class SitemapFilterTests
{
    [Test]
    public async Task Urls_SkipsAccountLoginAndWizardTypes()
    {
        var urls = SitemapFilter.Urls(
        [
            new("/", "homePage", false),
            new("/recipes/", "recipeListingPage", false),
            new("/recipes/create-recipe/", "createRecipePage", false),
            new("/recipes/add-variant/", "addVariantPage", false),
            new("/account/", "accountPage", false),
            new("/account/login/", "loginPage", false),
            new("/account/settings/", "accountSettingsPage", false),
            new("/account/registration-complete/", "registrationCompletePage", false),
        ]);

        _ = await Assert.That(string.Join(",", urls)).IsEqualTo("/,/recipes/");
    }

    [Test]
    public async Task Urls_HonoursTheExcludeFlag()
    {
        var urls = SitemapFilter.Urls([new("/hidden/", "homePage", true)]);

        _ = await Assert.That(urls.Any()).IsFalse();
    }

    [Test]
    public async Task Urls_AreLowercased()
    {
        var urls = SitemapFilter.Urls([new("/Recipes/", "recipeListingPage", false)]);

        _ = await Assert.That(string.Join(",", urls)).IsEqualTo("/recipes/");
    }
}
```

Create `tests/KCC.UnitTests/Features/Sitemap/RobotsTxtProviderTests.cs`:

```csharp
using KCC.Web.Features.Sitemap;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;

namespace KCC.UnitTests.Features.Sitemap;

public class RobotsTxtProviderTests
{
    [Test]
    public async Task GetContent_WhenOpen_DisallowsThePrivatePaths()
    {
        var content = Provider(denyAll: false).GetContent();

        foreach (var path in new[] { "/umbraco", "/api", "/account", "/error" })
        {
            _ = await Assert.That(content.Contains($"Disallow: {path}", StringComparison.Ordinal)).IsTrue();
        }

        _ = await Assert.That(content.Contains("Sitemap: https://kcc.test/sitemap.xml", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task GetContent_WhenDenyingAll_DisallowsEverything()
    {
        var content = Provider(denyAll: true).GetContent();

        _ = await Assert.That(content.Contains("Disallow: /", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(content.Contains("Disallow: /umbraco", StringComparison.Ordinal)).IsFalse();
    }

    private static RobotsTxtProvider Provider(bool denyAll)
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("kcc.test");

        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(a => a.HttpContext).Returns(context);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> { ["RobotsTxtDenyAll"] = denyAll ? "true" : "false" })
            .Build();

        return new RobotsTxtProvider(accessor.Object, configuration);
    }
}
```

In `src/KCC.Web/Features/Sitemap/RobotsTxtProvider.cs`, change `private string GetContent()` to
`internal string GetContent()` so the tests can read it, and delete the `Features/Sitemap/**` line from the
`Unported slices` group. Run the unit tests — expected: build error (`SitemapFilter` missing).

- [ ] **Step 2: Write the sitemap pages and controller**

Create `src/KCC.Web/Features/Sitemap/SitemapPages.cs`:

```csharp
using KCC.Web.Features.Models.Generated;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace KCC.Web.Features.Sitemap;

public sealed record SitemapCandidate(string Url, string ContentTypeAlias, bool ExcludeFromSitemap);

public interface ISitemapPages
{
    IEnumerable<string> Urls();
}

public static class SitemapFilter
{
    private static readonly HashSet<string> ExcludedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "accountPage",
        "accountSettingsPage",
        "addVariantPage",
        "createRecipePage",
        "loginPage",
        "registrationCompletePage",
    };

    public static IEnumerable<string> Urls(IEnumerable<SitemapCandidate> candidates) =>
        candidates
            .Where(candidate => !candidate.ExcludeFromSitemap && !ExcludedTypes.Contains(candidate.ContentTypeAlias))
            .Select(candidate => candidate.Url.ToLowerInvariant())
            .Distinct(StringComparer.Ordinal);
}

public class SitemapPages(IPublishedContentQuery contentQuery) : ISitemapPages
{
    public IEnumerable<string> Urls() => SitemapFilter.Urls(
        contentQuery.ContentAtRoot()
            .OfType<HomePage>()
            .SelectMany(home => new IPublishedContent[] { home }.Concat(home.Descendants()))
            .OfType<IMetadata>()
            .Select(page => new SitemapCandidate(page.Url(), page.ContentType.Alias, page.ExcludeFromSitemap)));
}
```

Only the tree under Home is walked, so Site Settings, the folders and the status-code pages never appear.

Replace `src/KCC.Web/Features/Sitemap/SitemapController.cs` with:

```csharp
using Microsoft.AspNetCore.Mvc;
using SimpleMvcSitemap;

namespace KCC.Web.Features.Sitemap;

public class SitemapController(ISitemapPages sitemapPages) : Controller
{
    [HttpGet("sitemap.xml")]
    public IActionResult Index() =>
        new SitemapProvider().CreateSitemap(new SitemapModel(sitemapPages.Urls().Select(url => new SitemapNode(url)).ToList()));
}
```

In `src/KCC.Web/Features/Sitemap/RobotsTxtProvider.cs`, replace the `AddSection` lambda's disallows with:

```csharp
            .AddUserAgent("*")
            .Disallow("/umbraco")
            .Disallow("/api")
            .Disallow("/account")
            .Disallow("/error")
```

In `src/KCC.Web/Program.cs`, add `using KCC.Web.Features.Sitemap;` and `using RobotsTxt;` (sorted), the registrations

```csharp
builder.Services.AddScoped<ISitemapPages, SitemapPages>();
builder.Services.AddScoped<IRobotsTxtProvider, RobotsTxtProvider>();
```

after the `PageMetadata` registration, and `app.UseRobotsTxt();` immediately before `app.UseVueSsr();`.

- [ ] **Step 3: Write the integration tests**

Create `tests/KCC.IntegrationTests/Features/Pages/SitemapTests.cs`:

```csharp
using KCC.IntegrationTests.Config;

namespace KCC.IntegrationTests.Features.Pages;

public class SitemapTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task Sitemap_ListsPublicPagesOnly()
    {
        using var client = Site.CreateClient();
        var xml = await client.GetStringAsync("/sitemap.xml");

        _ = await Assert.That(xml.Contains("/recipes/", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(xml.Contains("/account", StringComparison.Ordinal)).IsFalse();
        _ = await Assert.That(xml.Contains("status-codes", StringComparison.Ordinal)).IsFalse();
    }

    [Test]
    public async Task RobotsTxt_DeniesEverythingUntilLaunch()
    {
        using var client = Site.CreateClient();
        var robots = await client.GetStringAsync("/robots.txt");

        _ = await Assert.That(robots.Contains("Disallow: /", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(robots.Contains("sitemap.xml", StringComparison.Ordinal)).IsTrue();
    }
}
```

- [ ] **Step 4: Run**

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj
```

Expected: all pass. (If the sitemap's nodes come out absolute — `http://localhost/recipes/` — the assertions still
hold.)

- [ ] **Step 5: Commit**

```bash
git add -A src/KCC.Web tests/KCC.UnitTests tests/KCC.IntegrationTests
git commit -m "Serve the Sitemap and robots.txt from Umbraco"
```

---

### Task 13: End-to-end smoke tests for the chrome

**Files:**
- Create: `tests/KCC.E2ETests/Features/Chrome/ChromeTests.cs`, `tests/KCC.E2ETests/Features/Chrome/NotFoundTests.cs`

- [ ] **Step 1: Write the tests**

Create `tests/KCC.E2ETests/Features/Chrome/ChromeTests.cs`:

```csharp
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.Chrome;

public class ChromeTests : BasePageTests
{
    [Test]
    [Arguments("light")]
    [Arguments("dark")]
    public async Task Home_RendersTheHeaderInEachRamp(string ramp)
    {
        await Page.AddInitScriptAsync($"localStorage.setItem('kcc-theme', '{ramp}')");

        var response = await Page.GotoAsync("/");

        _ = await Assert.That(response!.Status).IsEqualTo(200);
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("data-theme", ramp);
        await Expect(Page.Locator($"header img[data-ramp='{ramp}']")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Recipes", Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Login", Exact = true })).ToBeVisibleAsync();
    }

    [Test]
    public async Task Home_IsServerRendered()
    {
        var response = await Page.APIRequest.GetAsync("/");
        var html = await response.TextAsync();

        _ = await Assert.That(html.Contains("<header", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task RecipesMenu_OpensToItsLinks()
    {
        await Page.GotoAsync("/");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Recipes", Exact = true }).ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "All Recipes", Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Create Recipe", Exact = true })).ToBeVisibleAsync();
    }
}
```

Create `tests/KCC.E2ETests/Features/Chrome/NotFoundTests.cs`:

```csharp
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.Chrome;

public class NotFoundTests : BasePageTests
{
    [Test]
    public async Task UnknownUrl_ShowsThe404Page()
    {
        var response = await Page.GotoAsync("/this-page-does-not-exist");

        _ = await Assert.That(response!.Status).IsEqualTo(404);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToContainTextAsync("find that page");
    }
}
```

`Home_IsServerRendered` reads the raw response, so it fails if SSR silently fell back to client-side rendering.

- [ ] **Step 2: Run the whole E2E suite**

```bash
dotnet build KitchenCommandCenter.sln
(cd src/KCC.Web && yarn build:all)
dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj
```

Expected: 7 passed (site boot, dictionary restart, 4 chrome, 1 not-found).

- [ ] **Step 3: Commit**

```bash
git add tests/KCC.E2ETests
git commit -m "Smoke-Test the Umbraco Chrome End to End"
```

---

### Task 14: CI without SQL Server

**Files:**
- Rewrite: `.github/workflows/build-and-test.yml`

- [ ] **Step 1: Rewrite the workflow**

The E2E fixture now starts its own site and SSR process, so the database container, the Kentico provisioning, the
background app and the member SQL all go.

```yaml
name: Build and Test Solution

on:
  pull_request:
    branches: [main, replatform]
  push:
    branches: [main, replatform]

concurrency:
  group: ci-${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true

permissions:
  contents: read

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    timeout-minutes: 40

    env:
      DOTNET_NOLOGO: "true"
      DOTNET_CLI_TELEMETRY_OPTOUT: "true"
      FONTAWESOME_NPM_AUTH_TOKEN: ${{ secrets.FONTAWESOME_NPM_AUTH_TOKEN }}
      KCC_E2E_MEMBER_USERNAME: ${{ secrets.KCC_E2E_MEMBER_USERNAME }}
      KCC_E2E_MEMBER_PASSWORD: ${{ secrets.KCC_E2E_MEMBER_PASSWORD }}

    steps:
      # ---------- Setup ----------
      - name: Checkout
        uses: actions/checkout@v7

      - name: Set up .NET
        uses: actions/setup-dotnet@v6
        with:
          global-json-file: global.json

      - name: Set up Node
        uses: actions/setup-node@v7
        with:
          node-version: "22"
          cache: yarn

      - name: Cache NuGet packages
        uses: actions/cache@v6
        with:
          path: ~/.nuget/packages
          key: nuget-${{ runner.os }}-${{ hashFiles('Directory.Packages.props', '**/*.csproj') }}
          restore-keys: |
            nuget-${{ runner.os }}-

      # ---------- Build task 1: .NET solution (Release) ----------
      - name: Build .NET solution (Release)
        id: build_dotnet
        run: dotnet build KitchenCommandCenter.sln -c Release

      # ---------- Build task 2: frontend (attempted even if .NET build failed) ----------
      - name: Build frontend
        id: build_frontend
        if: ${{ !cancelled() }}
        run: |
          yarn install --frozen-lockfile
          yarn build:all

      # ---------- Gate: run tests only if BOTH builds succeeded ----------
      - name: Verify builds succeeded
        if: ${{ !cancelled() }}
        run: |
          echo "dotnet build:   ${{ steps.build_dotnet.outcome }}"
          echo "frontend build: ${{ steps.build_frontend.outcome }}"
          if [ "${{ steps.build_dotnet.outcome }}" != "success" ] || [ "${{ steps.build_frontend.outcome }}" != "success" ]; then
            echo "::error::A build task failed — skipping tests."
            exit 1
          fi

      # ---------- Playwright browsers (cached across runs; keyed on the Playwright version) ----------
      - name: Resolve Playwright version (cache key)
        id: playwright_version
        run: |
          version=$(grep -oiP 'Include="Microsoft\.Playwright"\s+Version="\K[^"]+' Directory.Packages.props || true)
          if [ -z "$version" ]; then
            echo "::error::Could not resolve Microsoft.Playwright version from Directory.Packages.props"
            exit 1
          fi
          echo "version=$version" >> "$GITHUB_OUTPUT"
          echo "Microsoft.Playwright version: $version"

      - name: Cache Playwright browsers
        id: playwright_cache
        uses: actions/cache@v6
        with:
          path: ~/.cache/ms-playwright
          key: playwright-${{ runner.os }}-${{ steps.playwright_version.outputs.version }}

      # Browser binaries live in the cached dir; OS-level deps do not, so install-deps still runs on a hit.
      - name: Install Playwright browsers + deps (cache miss)
        if: steps.playwright_cache.outputs.cache-hit != 'true'
        run: pwsh tests/KCC.E2ETests/bin/Release/net10.0/playwright.ps1 install --with-deps

      - name: Install Playwright system deps (cache hit)
        if: steps.playwright_cache.outputs.cache-hit == 'true'
        run: pwsh tests/KCC.E2ETests/bin/Release/net10.0/playwright.ps1 install-deps

      # ---------- Full combined test suite ----------
      # The E2E fixture starts KCC.Web and its SSR service itself, each run on a fresh SQLite file.
      - name: Run combined test suite
        run: |
          set +e  # the summary has to be cleared even when tests fail
          node tests/scripts/run.mjs
          status=$?
          # TUnit appends its own per-project tables to the job summary, and its HTML
          # reporter adds a notice line that no setting turns off. The summary file is
          # per-step, so deleting this step's drops all of it; the combined report is
          # published from the step below instead.
          rm -f "$GITHUB_STEP_SUMMARY"
          exit $status

      # ---------- Publish report ----------
      - name: Upload combined test report
        if: ${{ always() }}
        uses: actions/upload-artifact@v7
        with:
          name: combined-test-report
          path: tests/results/
          if-no-files-found: warn

      - name: Publish combined report to job summary
        if: ${{ always() }}
        run: |
          if [ ! -f tests/results/combined-report.md ]; then
            echo "::warning::No combined-report.md was produced — skipping job summary."
            exit 0
          fi
          cat tests/results/combined-report.md >> "$GITHUB_STEP_SUMMARY"
```

- [ ] **Step 2: Run the combined suite locally**

```bash
node tests/scripts/run.mjs
```

Expected: every suite green; the report opens. (`dotnet test` builds the solution in Debug, which is the
configuration `SiteProcess` launches.)

- [ ] **Step 3: Commit and watch CI (ask the owner before pushing)**

```bash
git add .github/workflows/build-and-test.yml
git commit -m "Rebuild CI Without SQL Server"
```

Ask: "Push `replatform` to origin so CI runs?" On yes:

```bash
git push -u origin replatform
gh run watch "$(gh run list --branch replatform --limit 1 --json databaseId -q '.[0].databaseId')" --exit-status
```

Expected: the run succeeds. The `KENTICO_LICENSE_KEY` and `KENTICO_HASH_STRING_SALT` repository secrets are now
unused; Phase 8 removes them.

---

### Task 15: Documentation, memory and the Phase 1 gate

**Files:**
- Modify: `README.md`, `CLAUDE.md`
- Memory (outside the repo): `~/.claude/projects/-Users-twinright-Repos-Kitchen-Command-Center/memory/`

- [ ] **Step 1: Rewrite the README's Kentico sections**

In `README.md`:
1. Replace the intro line with `An Umbraco 17 application with Vue 3 server-side rendering (SSR), on SQLite.`
2. Replace the whole **Local Development → Quick Development Workflow** list (steps 1–6) with:

````markdown
1. **Install frontend dependencies** from the repo root (needs the Font Awesome token, see below):

   ```bash
   yarn install
   ```

2. **Set the backoffice admin account** once per machine. Umbraco creates it on the first boot of a new database.
   The password needs at least 10 characters; keep it in 1Password.

   ```bash
   cd src/KCC.Web
   dotnet user-secrets set "Umbraco:CMS:Unattended:UnattendedUserName" "<name>"
   dotnet user-secrets set "Umbraco:CMS:Unattended:UnattendedUserEmail" "<email>"
   dotnet user-secrets set "Umbraco:CMS:Unattended:UnattendedUserPassword" "<password>"
   ```

3. **Run it** from `src/KCC.Web`:

   ```bash
   dotnet watch --non-interactive
   ```

   The first run creates `umbraco/Data/Umbraco.sqlite.db`, installs Umbraco and imports the schema, UI strings and
   baseline pages from `uSync/v17/`. `dotnet watch` also starts Vite and the SSR service. To start over, stop the site
   and delete `umbraco/Data/Umbraco.sqlite.db*`.

The site is at `https://localhost:58671`; the backoffice is at `/umbraco`.
````

3. Under **Development Best Practices**, replace **Caching Strategy** and **Widget Guidelines** with:

```markdown
#### Schema and baseline content

- Document types, data types and dictionary items are edited in the backoffice and exported by uSync on save in
  Development. Commit `src/KCC.Web/uSync/v17/`.
- Baseline content (the empty page tree, site settings, taxonomy, status pages) is not exported on save. Edit it in a
  fresh database, then run `curl -sk -X POST https://localhost:58671/api/dev/baseline/export` and commit the result.
  The endpoint refuses to run while seeded recipes exist.
- ModelsBuilder runs in `SourceCodeManual` mode: after a schema change, use Settings → Models Builder → Generate models,
  and commit `Features/Models/Generated`.
```

4. Delete the **Code Generation**, **Kentico Upgrades** and **CI/CD Operations** subsections under **Common
   Commands**.
5. Replace the **E2E seed member (one-time)** subsection with:

```markdown
#### E2E tests

The E2E suite starts its own copy of the site on a free port, with a fresh SQLite database and its own SSR process, so
nothing needs setting up beyond building: run `dotnet build` and `yarn build:all` (in `src/KCC.Web`) first. The member
flows return in a later phase and will read `KCC_E2E_MEMBER_USERNAME` / `KCC_E2E_MEMBER_PASSWORD`.
```

- [ ] **Step 2: Fix the stale line in `CLAUDE.md`**

In the **Vue SFC `<style>` blocks** section, delete the sentence that begins "Entries outside that graph need their
own link" — it points at `ResourceStringEditorTagHelper`, which is gone along with its Vite entry.

- [ ] **Step 3: Update the memory the port invalidated**

- `kcc-web-dev-run-gotchas.md`: rewrite item 1 (no channel-domain constraint under Umbraco; the site still runs on
  `https://localhost:58671`) and item 3 (there is no `--kxp-ci-restore`; schema comes from uSync at startup, baseline
  content from the export endpoint). Keep item 2 until Phase 3 and item 4 as is. Update its `MEMORY.md` line.
- `e2e-tests-seed-member-and-db.md`: replace with the new model — the E2E fixture (`SiteProcess`) starts a fresh site
  per run; there is no docker database; member seeding returns in Phase 4. Update its `MEMORY.md` line.
- In `MEMORY.md`, append to the replatform line: `Phase 1 landed on branch replatform (date): Kentico-only notes apply
  to the xperience-final reference only.`

- [ ] **Step 4: The capture tool needs no change**

Phase 0 already gave `tests/KCC.ReferenceCapture` its `--only <name>[,<name>…]` filter (see Findings from
Phase 0); a signed-out selection such as `--only home,not-found` skips sign-in, which this phase does not have.

- [ ] **Step 5: Commit**

```bash
git add README.md CLAUDE.md
git commit -m "Document the Umbraco Development Loop"
```

- [ ] **Step 6: The gate — a fresh clone renders in both ramps**

Stop any site on port 58671, then:

```bash
GATE="$(mktemp -d)/kcc-phase-1-gate"
git clone --branch replatform --single-branch /Users/twinright/Repos/Kitchen-Command-Center "$GATE"
cd "$GATE" && yarn install --frozen-lockfile
cd src/KCC.Web && dotnet watch --non-interactive
```

(the clone shares this machine's user-secrets through the project's `UserSecretsId`). Once it serves, from the main
checkout:

```bash
curl -sk https://localhost:58671/ | grep -c '<header'
dotnet run --project tests/KCC.ReferenceCapture -- --only home,not-found --out .superpowers/reference/umbraco-phase-1
```

Expected: the `curl` count is at least 1 (server-rendered header); 8 screenshots. Compare
`.superpowers/reference/umbraco-phase-1/{light,dark}-desktop/home.png` (gitignored scratch) with
`docs/replatform/reference/xperience-final/{light,dark}-desktop/home.png`: the header and
footer match (same kit, same ramp) and the body is empty; `not-found.png` shows the torn 404 sheet. Stop the gate
site and delete `$GATE`.

Then run everything once more from the main checkout:

```bash
dotnet build KitchenCommandCenter.sln
node tests/scripts/run.mjs
```

Expected: all green.

- [ ] **Step 7: Close the phase**

Set this file's **Status** line to `done (<date>)`. Phase 2 (Recipes) is planned next, in this folder, against the code
as it now stands.
