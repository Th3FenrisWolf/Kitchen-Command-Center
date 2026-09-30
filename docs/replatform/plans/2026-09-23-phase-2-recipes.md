# Replatform Phase 2 — Recipes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or
> superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for
> tracking.

**Status:** done (2026-09-25). **Resume point:** the Phase 3 plan in this folder; read **Findings from Phase 2** at
the end of this file first. **Requires Phase 1 done** (it is, 2026-09-24).

**Goal:** Seeded recipe and variant pages render from Umbraco, with their ratings, authors, breadcrumbs and image
tiles, and match the Phase 0 reference screenshots in both ramps.

**Architecture:** A dev-only bootstrap creates two new document types, `recipe` and `recipeVariant`, once; uSync
exports them and they are committed, like Phase 1's schema. `KCC.Contributions` is rebuilt as an EF Core store in
the same SQLite file, registered with `AddUmbracoDbContext`, so every write joins Umbraco's scope and write lock.
Its read side feeds the pages and two GET APIs: stats cached in `IMemoryCache`, plus paged reads. Hijacked
`RecipeController` and `RecipeVariantController` map the typed models through a `RecipeQueries` service and pure
mapping classes onto the existing view models and views. The dev seeder is rewritten on Umbraco's editing services,
and both test fixtures run it before any test.

**Tech Stack:** Umbraco.Cms 17.x (the version Phase 1 pinned); Umbraco.Cms.Persistence.EFCore (same version);
EF Core 10 on SQLite with dotnet-ef 10.0.11; ModelsBuilder; uSync 17; Umbraco's ImageSharp; TUnit 1.27 + Moq;
Microsoft.AspNetCore.Mvc.Testing; TUnit.Playwright; Vitest.

**Spec:** `docs/replatform/specs/2026-09-21-replatform-off-xperience.md`. Sections: §6.4 (query services, caching),
§7 (content model), §8 (authors), §9.1 (contributions), §12 (seeding), §14 (testing), §15 (the Phase 2 row and
gates), §17 (defects fixed) and §19 row 3 (the EF Core write-lock proof).

## Before you start: reconcile with Phase 1 as built

This plan was written from Phase 1's *plan*, before Phase 1 ran. Check each item below. Where the code differs,
adapt the step that depends on it and note the change in your task report.

```bash
grep -n "Status:" docs/replatform/plans/2026-09-23-phase-1-foundation.md
grep -n "GetOrDefault\|GetManyOrDefault" src/KCC.Web/Features/Dictionary/IResourceStringProvider.cs
grep -n "public void Apply" src/KCC.Web/Features/Pages/Shared/PageMetadata.cs
cat src/KCC.Web/Features/Components/Breadcrumbs/BreadcrumbLink.cs
grep -n "WebPageItemID\|Breadcrumbs" src/KCC.Web/Features/Pages/Shared/BasePageViewModel.cs
grep -n "InitializeAsync\|DisposeAsync\|WebProjectDirectory\|Settings()" tests/KCC.IntegrationTests/Config/UmbracoSite.cs
grep -n "public SiteProcess\|LogTail()\|await StartAsync();" tests/KCC.E2ETests/Config/SiteProcess.cs
ls src/KCC.Web/uSync/v17/ContentTypes src/KCC.Web/uSync/v17/MemberTypes src/KCC.Web/Features/Models/Generated
sed -n '/Unported slices/,/<\/ItemGroup>/p' src/KCC.Web/KCC.Web.csproj tests/KCC.UnitTests/KCC.UnitTests.csproj \
  tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj tests/KCC.E2ETests/KCC.E2ETests.csproj
grep -n 'Include="Umbraco.Cms"\|Include="uSync"\|Mvc.Testing' Directory.Packages.props
grep -n '"--only"' tests/KCC.ReferenceCapture/Program.cs
ls src/KCC.Web/Views/Page.cshtml 2>/dev/null || echo "no template fallback"
grep -n "builder.Services.Add" src/KCC.Web/Program.cs
```

Expected, and what to do if not:

1. Phase 1's Status is `done`. If not, stop: this phase builds on it.
2. `IResourceStringProvider` has `string GetOrDefault(string key)` and
   `Dictionary<string, string> GetManyOrDefault(params string[] keys)`. It is registered scoped.
3. `PageMetadata.Apply(IMetadata page, BasePageViewModel viewModel)` exists and is registered scoped.
4. `BreadcrumbLink` is Phase 1's record `(string LinkText, string Url, int? ParentId = null, int? WebPageItemId =
   null)`. Task 7 reduces it to its first two members.
5. `BasePageViewModel` has no `WebPageItemID` and keeps `IEnumerable<BreadcrumbLink> Breadcrumbs`.
6. `UmbracoSite` has an `InitializeAsync` that boots the site and checks the Run level, a `DisposeAsync` that deletes
   the run folder, a private static `WebProjectDirectory()`, and a `Settings()` dictionary. Tasks 5 and 6 edit them.
7. `SiteProcess` has the constructors `()` → `this(withSsr: true)` and `(bool withSsr)`, a private `LogTail()`, and
   `await StartAsync();` as the last line of `InitializeAsync`. Task 5 edits them.
8. uSync holds Phase 1's 16 content types (including `recipelistingpage.config`, `recipecategory.config`,
   `recipetag.config` and `metadata.config`) and the member type. `Features/Models/Generated` holds their models,
   including `Member.generated.cs`.
9. The `Unported slices` groups read as follows. A slice Phase 1 already brought back is fine. A slice still
   excluded that this plan never mentions belongs to a later phase: leave it.
   - `KCC.Web.csproj`: `Features/Api/**`, `Features/Components/Breadcrumbs/BreadcrumbService.cs`,
     `Features/DevTools/RecipeSeed/**`, `Features/Models/Api/**`, `Features/Models/Common/KCCApplicationUser.cs`,
     `Features/Pages/{Account,AddVariant,CreateRecipe,RecipeDetail,RecipeSearch,VariantDetail}/**`,
     `Features/Providers/**`, `Features/Search/**`, plus one `Content Remove` line for each of
     `Features/Pages/{Account,AddVariant,CreateRecipe,RecipeDetail,RecipeSearch,VariantDetail}/**/*.cshtml`.
   - `KCC.UnitTests.csproj`: `Admin/**`, `Features/Api/**`, `Features/Contributions/**`, `Features/Pages/Account/**`,
     `Features/Pages/RecipeDetail/**`, `Features/Pages/VariantDetail/**`, `Features/Providers/**`, `Features/Search/**`.
   - `KCC.IntegrationTests.csproj`: `Features/Providers/**`.
   - `KCC.E2ETests.csproj`:
     `Features/{HomePage,RecipeRatings,RecipeSearch,VariantCookNotes,VariantCooked,VariantDetail,VariantReviews}/**`.
10. `Directory.Packages.props` pins `Umbraco.Cms` (17.8.0 or 17.7.0), `uSync` and `Microsoft.AspNetCore.Mvc.Testing`.
    Every `Umbraco.Cms*` package this plan adds takes the same version as `Umbraco.Cms`.
11. The reference-capture tool accepts `--only`. Phase 0 added it.
12. **The §19 template fallback.** If `src/KCC.Web/Views/Page.cshtml` exists, Phase 1 took the fallback, and three
    places in this plan change:
    - Task 2 gives both new types the `Page` template. See the note after Task 2 Step 3.
    - Tasks 5 and 6 pass that template's key in every `ContentCreateModel`. See the notes there.
13. `Program.cs` registers `IResourceStringProvider`, `ISiteSettingsQueries`, `PageMetadata`, `ISitemapPages` and
    `IRobotsTxtProvider`, in that order. Tasks 5, 6 and 7 each add a line after `IRobotsTxtProvider`.
14. **Deterministic keys (ruling R9).** The backoffice rejects GUIDs without RFC 9562 version and variant bits
    (`UmbId.validate`). Task 2's `Key()` in `RecipeSchemaBootstrap` and Task 5's `SeedKeys.For` must set the version-8
    nibble (the high nibble of byte 7 in .NET's `Guid(ReadOnlySpan<byte>)` layout) and the RFC variant (the top two
    bits of byte 8). Restore one shared helper for both, e.g. from
    `git show 5e0840e^:src/KCC.Web/Features/DevTools/Baseline/BaselineKeys.cs`. Otherwise Phase 1's widened
    `SchemaTests` key guard (every type, property and group key) and `BaselineContentTests` (every content key) fail.
15. `UmbracoSite.DisposeAsync` no longer has the `if (runDirectory.Length > 0) { try … }` block that Task 6 Step 1
    replaces. Phase 1 replaced it with a `DeleteIfPresent(path)` helper, which also deletes Umbraco's `LocalTempPath`
    and the Examine temp folder. Add the media-cache folder as one more `DeleteIfPresent` call instead of adding
    `DeleteQuietly`.

Before any task starts the dev site, check that nothing else is listening on port 58671.

## What the scratch probe already proved

All of the following ran in a scratch Umbraco 17.7 site on SQLite, using this plan's code blocks verbatim. The
run was green: 64 unit tests and 42 integration tests. The integration count includes the recipe and variant page
tests, which ran against the real `Index.cshtml` views and a layout that writes the server-content JSON the same way
Phase 1's does. The tasks repeat each proof in the real repo. What the probe could not run: the sitemap test and
the E2E suites, which need Phase 1's sitemap and `SiteProcess`.

- **EF Core joins Umbraco's scope.** With `AddUmbracoDbContext(…, shareUmbracoConnection: true)`, EF Core runs on the
  Umbraco scope's connection and transaction. Two consequences, which together mean spec §19 row 3 holds:
  - An EF write inside an outer Umbraco scope that never completes **rolls back with that scope**.
  - An EF scope holding `WriteLock(ContributionLocks.Contributions)` **holds back a concurrent Umbraco write** until
    it commits.
- **The startup migration** runs on `UmbracoApplicationStartedNotification`. It creates the three tables, their
  unique indexes, and the lock row in `umbracoLock`.
- **The editing services** accept the value shapes listed under Global Constraints.
  - `IContentService.Save` persists a backdated `CreateDate`.
  - Members created through `IMemberEditingService` are approved and carry a first and last name.
- **ModelsBuilder** types the pickers as listed under Global Constraints.
- **Image tiles.** `GetCropUrl(width: 192, height: 192, ImageCropMode.Crop, "&format=webp")` gives
  `…?width=192&height=192&format=webp&v=…`. Once `BrowserMaxAge` is a year, the tile is served as `image/webp` with
  `Cache-Control: public, max-age=31536000, immutable`.
- **Pages and APIs.** The async hijacked controllers render. The reviews API returns camelCase JSON with a
  five-bucket distribution.
- **Dates.** EF Core reads SQLite `DateTime`s back as `Unspecified`, and a browser would read the resulting JSON as
  local time. The converter in Task 1 makes them UTC. Umbraco's own content dates already come back as UTC.
- **A hazard.** After a content, media or member save, Umbraco updates relations: it reads, then writes, without
  taking a write lock. On SQLite, if another writer commits in between, Umbraco retries a stale snapshot for about
  9m40s before failing with "database is locked". Two mitigations follow:
  - The integration suite runs one test at a time (Task 1).
  - The fixtures seed before any test runs (Task 5).

  Phase 4's concurrency test owns the live-site side of this hazard. See the memory note
  `umbraco-sqlite-relations-lock-hazard`.

## Global Constraints

Phase 1's constraints still apply:

- Work on branch **`replatform`**. The replatform's spec, phase plans and reference set are tracked in
  `docs/replatform/`: commit changes to them (a status line, a correction) with the work they describe. Everything
  else under `.superpowers/` stays gitignored: never stage anything there. Commit messages are Title Case imperative
  with no attribution lines. Ask the owner before any `git push`.
- **Umbraco.Cms 17.x, never 18.** Every `Umbraco.Cms*` package takes the same version as `Umbraco.Cms`.
- **The SQLite connection string never contains `Cache=Shared`.**
- **`ModelsMode`** is `SourceCodeManual` in Development and `Nothing` everywhere else. Models live in
  `Features/Models/Generated` (namespace `KCC.Web.Features.Models.Generated`) and are committed.
- **uSync root folder is `uSync/v17/`.** Schema exports on save in Development only; content never exports on save.
- **Route hijacking.** A page controller is named `<Alias>Controller`, derives from `RenderController`, has no
  `[Route]`, and returns its view by explicit path. An async page hides the base action and adds its own:

  ```csharp
  [NonAction]
  public sealed override IActionResult Index() => throw new NotSupportedException();

  public async Task<IActionResult> Index(CancellationToken cancellationToken)
  ```

- **Only APIs that survive Umbraco 18.** Use:
  - `ControllerBase` for APIs;
  - the async editing and publishing services;
  - `IDocumentNavigationQueryService`;
  - the friendly extensions `Children<T>()`, `Parent<T>()`, `AncestorsOrSelf()` and `Url()`.

  Warnings are errors, so an obsolete member fails the build (CS0618). In particular, never use
  `IContentService.GetPagedChildren` or `IMemberService.GetByKey`; use `GetByKeysAsync` or `GetById(Guid)` instead.
- **Nullable reference types and StyleCop.**
  - Nullable is disabled in `KCC.Web`, `KCC.Contributions` and `KCC.UnitTests`. Never write `?` on a reference type
    there: it raises CS8632.
  - `KCC.IntegrationTests` and `KCC.E2ETests` have nullable enabled.
  - StyleCop runs on `src/KCC.Web`. It requires sorted usings, trailing commas in multi-line initializers, and
    static members before instance members.
- **Code comments** follow `~/.claude/CLAUDE.md`: explain *why* only, with no narration and no future promises.
- **Always build both front-end bundles together** with `yarn build:all`.
- **Test commands:**
  - unit: `dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj`
  - integration: `dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj`
  - E2E: `dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj` (after `dotnet build` and `yarn build:all`)
  - one class: append `-- --treenode-filter "/*/*/<ClassName>/*"`
  - front end: `cd src/KCC.Web && yarn test`, then `yarn type-check`
  - everything: `node tests/scripts/run.mjs` (it opens its HTML report when it finishes)
- **The dev site** runs with `cd src/KCC.Web && dotnet run --launch-profile Local` on `https://localhost:58671`. Do
  not use `dotnet watch` while a bootstrap regenerates models.

Phase 2's own constraints:

- **Aliases** (exact):
  - Document types: `recipe`, `recipeVariant`.
  - Data types: `KCC Recipe Icon`, `KCC Ingredients`, `KCC Instructions`, `KCC Difficulty`, `KCC Recipe Category`,
    `KCC Recipe Tags`.
  - Recipe properties: `description`, `icon`, `image`, `category`, `author`.
  - Variant properties: `description`, `icon`, `images`, `difficulty`, `tags`, `author`, `prepTime`, `cookTime`,
    `servings`, `ingredients`, `instructions`, `calories`, `proteinG`, `carbsG`, `fatG`, `saturatedFatG`, `fiberG`,
    `sugarG`, `sodiumMg`.
  - Member properties: `firstName`, `lastName`.
  - Tables `kccReview`, `kccCookNote` and `kccCookedMark`; lock id `ContributionLocks.Contributions` = `1000001`.
- **Generated model types:**

  | Property | Type |
  |---|---|
  | `Recipe.Category` (tree picker, max 1) | `IPublishedContent` |
  | `RecipeVariant.Tags` | `IEnumerable<IPublishedContent>` |
  | `Author` | `IPublishedContent` |
  | `Recipe.Image` | `MediaWithCrops` |
  | `RecipeVariant.Images` | `IEnumerable<MediaWithCrops>` |
  | Integers | `int`. An empty one reads 0, so check `GetProperty(alias)?.HasValue()`. |
  | `Difficulty` | `string`. It reads `""` when unset. |
  | `Ingredients`, `Instructions`, `Icon`, `Description` | `string` |

- **Editing-service value shapes:**

  | Property editor | Value to pass |
  |---|---|
  | Text | `string` |
  | Integer | `int` |
  | Dropdown | `string[]` |
  | Tree picker | a `System.Text.Json.Nodes.JsonArray` of `{ "type": "document", "unique": "<key>" }` |
  | Member picker | the member key as a `string` |
  | Media picker | the JSON string `[{"key":"<new guid>","mediaKey":"<media key>"}]` |
  | An image's `umbracoFile` | the JSON string `{"temporaryFileId":"<key>"}`, after `ITemporaryFileService.CreateAsync` |

  `IContentEditingService.CreateAsync` saves even when a property fails validation and still reports success, so
  check `result.Status == ContentEditingOperationStatus.Success`. An invariant publish is
  `PublishAsync(key, [new CulturePublishScheduleModel { Culture = null }], userKey)`.
- **Every contributions write takes `scope.WriteLock(ContributionLocks.Contributions)` before its first read.**
  SQLite transactions are deferred, and a transaction that reads first cannot become the writer once another writer
  has committed.
- **One tile size:** `RecipeImages.TileSize` = 192 px, in WebP, with a year of browser cache.
- **The seeder:**
  - answers only in Development and in Testing (the fixtures' environment);
  - is idempotent by deterministic keys (`SeedKeys`);
  - is run by `UmbracoSite` and `SiteProcess` before any test.
- **Ratings** count reviews of **published** variants only. A recipe's rating weighs every review equally.

## Not in Phase 2

- **Phase 3:** search, the listing page and the recipe index.
- **Phase 4:** sign-in; the review, note and cooked **write** APIs; cascades; submissions; the approved E2E member.
- **Phase 5:** the ingredient, instruction and icon property editors, and the Contributions dashboard.
- **Phase 6:** home blocks.

Until Phase 3, the recipe listing page (`/recipes/`) still answers 404; the new pages reach it only through their
breadcrumbs.

## File map

| Path | Change | Task |
|---|---|---|
| `src/KCC.Contributions/{Admin/,Data/,ContributionsModule.cs,ContributionsServiceExtensions.cs}` (Kentico) | Delete (the React `Client/` stays for Phase 5) | 1 |
| `src/KCC.Contributions/KCC.Contributions.csproj`, `ContributionsComposer.cs`, `Data/*.cs`, `Data/Migrations/*` | Rewrite / create | 1 |
| `KitchenCommandCenter.sln`, `Directory.Packages.props`, `dotnet-tools.json`, `src/KCC.Web/KCC.Web.csproj` | Modify / create | 1 |
| `tests/KCC.IntegrationTests/AssemblyInfo.cs`, `Features/Contributions/ContributionsStoreTests.cs` | Create | 1 |
| `src/KCC.Web/Features/DevTools/Baseline/RecipeSchemaBootstrap.cs` | Create, run once, delete | 2 |
| `src/KCC.Web/uSync/v17/{ContentTypes,DataTypes,MemberTypes}/*`, `Features/Models/Generated/*` | Generated, committed | 2 |
| `tests/KCC.IntegrationTests/Features/Recipes/RecipeSchemaTests.cs` | Create | 2 |
| `src/KCC.Contributions/{RatingMath,ContributionStats,ContributionStatsSource,ContributionReads,ContributionWrites}.cs` | Create | 3 |
| `tests/KCC.UnitTests/Features/Contributions/*`, `tests/KCC.IntegrationTests/Features/Contributions/ContributionReadsTests.cs` | Replace / create | 3 |
| `src/KCC.Web/Features/Providers/*` | Rewrite one, create two, delete `VariantGuidProvider.cs` | 4 |
| `tests/KCC.{UnitTests,IntegrationTests}/Features/Providers/AuthorNameProviderTests.cs` | Rewrite | 4 |
| `src/KCC.Web/Features/DevTools/RecipeSeed/*` | Rewrite the seeder and endpoint, create `SeedKeys`, fix stale comments | 5 |
| `tests/KCC.IntegrationTests/Config/UmbracoSite.cs`, `tests/KCC.E2ETests/Config/SiteProcess.cs` | Seed before the tests | 5 |
| `src/KCC.Web/Features/Recipes/*`, `src/KCC.Web/appsettings.json` | Create / modify | 6 |
| `tests/KCC.IntegrationTests/Config/TestContent.cs`, `Features/Recipes/RecipeQueriesTests.cs` | Create | 6 |
| `src/KCC.Web/Features/Components/Breadcrumbs/*` | Rewrite / create | 7 |
| `src/KCC.Web/Features/Pages/RecipeDetail/*` | Move the controller, create the mapping | 8 |
| `tests/KCC.IntegrationTests/Config/RenderedPage.cs`, `Features/Pages/{RecipePageTests,SitemapTests}.cs` | Create / modify | 8 |
| `src/KCC.Web/Features/Api/{ContributionResponses,ReviewApiController,CookNoteApiController}.cs` | Create / rewrite, GET only | 9 |
| `src/KCC.Web/Features/Pages/VariantDetail/*`, `Features/Types/ContentTypes.ts` | Move the controller, create the mapping, fix the cover image, delete | 10 |
| `tests/KCC.E2ETests/Features/{VariantDetail,RecipeRatings}/*` | Rewrite | 11 |
| `README.md`, memory | Modify | 12 |

---

### Task 1: The contributions store, and the §19 proof

The three tables move to EF Core, in Umbraco's SQLite file (spec §9.1). This task first repeats the proof of spec §19
row 3: EF Core writes share Umbraco's scope and write lock. Nothing else is built on the store until that proof
passes.

**Files:**
- Delete:
  - Kentico code: `src/KCC.Contributions/Admin/`, `src/KCC.Contributions/Data/` (the info classes),
    `src/KCC.Contributions/ContributionsModule.cs`, `src/KCC.Contributions/ContributionsServiceExtensions.cs`.
  - The six Kentico admin tests `tests/KCC.UnitTests/Features/Contributions/{ContentItemNameLookupTests,
    ContributionFormItemsTests,ContributionTabRegistrationTests,ContributionTabTests,
    ContributionsOverviewServiceTests,MemberNameLookupTests}.cs`.
- Rewrite: `src/KCC.Contributions/KCC.Contributions.csproj`
- Create:
  - `src/KCC.Contributions/Data/{Review,CookNote,CookedMark,ContributionLocks,UtcDateTimeConverter,
    ContributionsDbContext,ContributionsDesignTimeFactory,RunContributionsMigrations}.cs`
  - `src/KCC.Contributions/ContributionsComposer.cs`
  - `src/KCC.Contributions/Data/Migrations/*` (generated)
  - `dotnet-tools.json`
  - `tests/KCC.IntegrationTests/AssemblyInfo.cs`, `tests/KCC.IntegrationTests/Features/Contributions/ContributionsStoreTests.cs`
- Modify: `Directory.Packages.props`, `KitchenCommandCenter.sln`, `src/KCC.Web/KCC.Web.csproj`

**Interfaces:**
- Produces:
  - `KCC.Contributions.Data.ContributionsDbContext`, with `DbSet<Review> Reviews`, `DbSet<CookNote> CookNotes`,
    `DbSet<CookedMark> CookedMarks` and `const int MaxTextLength = 4000`.
  - The entities:
    - `Review`: `int Id`, `Guid VariantKey`, `Guid MemberKey`, `decimal Rating`, `string Text`, `DateTime Created`,
      `DateTime Modified`.
    - `CookNote`: `int Id`, `Guid VariantKey`, `Guid MemberKey`, `string Text`, `DateTime Created`,
      `DateTime Modified`.
    - `CookedMark`: `int Id`, `Guid VariantKey`, `Guid MemberKey`, `DateTime Created`.
  - `ContributionLocks.Contributions` (`const int`, `1000001`).
- Every read and write goes through `IEFCoreScopeProvider<ContributionsDbContext>` (namespace
  `Umbraco.Cms.Persistence.EFCore.Scoping`), in this order:
  1. `using var scope = scopes.CreateScope();`
  2. for a write only, `scope.WriteLock(ContributionLocks.Contributions);`
  3. `await scope.ExecuteWithContextAsync(…);`
  4. `scope.Complete();`

- [ ] **Step 1: Delete the Kentico contributions code**

```bash
git rm -r -q src/KCC.Contributions/Admin src/KCC.Contributions/Data
git rm -q src/KCC.Contributions/ContributionsModule.cs src/KCC.Contributions/ContributionsServiceExtensions.cs
git rm -q tests/KCC.UnitTests/Features/Contributions/ContentItemNameLookupTests.cs \
  tests/KCC.UnitTests/Features/Contributions/ContributionFormItemsTests.cs \
  tests/KCC.UnitTests/Features/Contributions/ContributionTabRegistrationTests.cs \
  tests/KCC.UnitTests/Features/Contributions/ContributionTabTests.cs \
  tests/KCC.UnitTests/Features/Contributions/ContributionsOverviewServiceTests.cs \
  tests/KCC.UnitTests/Features/Contributions/MemberNameLookupTests.cs
git ls-files src/KCC.Contributions | grep -v '^src/KCC.Contributions/Client/'
```

Expected: the last command prints only `src/KCC.Contributions/KCC.Contributions.csproj`. The React `Client/` folder
and its yarn workspace stay until Phase 5 replaces them with Lit, so `yarn build:all` still builds them.

- [ ] **Step 2: Rewrite the project, reference it and add the packages**

Replace `src/KCC.Contributions/KCC.Contributions.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
        <WarningsNotAsErrors>NU1608;NU1901;NU1902;NU1903</WarningsNotAsErrors>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Umbraco.Cms.Persistence.EFCore" />
    </ItemGroup>

    <ItemGroup>
        <InternalsVisibleTo Include="KCC.UnitTests" />
        <InternalsVisibleTo Include="KCC.IntegrationTests" />
    </ItemGroup>

</Project>
```

In `Directory.Packages.props`, add the two lines below in alphabetical position. `Umbraco.Cms.Persistence.EFCore`
must carry exactly the `Umbraco.Cms` version:

```xml
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.11" />
    <PackageVersion Include="Umbraco.Cms.Persistence.EFCore" Version="17.7.0" />
```

In `src/KCC.Web/KCC.Web.csproj`, add the following to the package `ItemGroup`, after `Microsoft.Extensions.Http.Polly`:

```xml
        <PackageReference Include="Microsoft.EntityFrameworkCore.Design">
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
            <PrivateAssets>all</PrivateAssets>
        </PackageReference>
```

Then add a new `ItemGroup` before the `InternalsVisibleTo` group:

```xml
    <ItemGroup>
        <ProjectReference Include="..\KCC.Contributions\KCC.Contributions.csproj" />
    </ItemGroup>
```

dotnet-ef runs from the startup project, so the design package belongs in KCC.Web. `PrivateAssets` keeps it from
flowing on to the test projects.

```bash
dotnet sln KitchenCommandCenter.sln add --solution-folder src src/KCC.Contributions/KCC.Contributions.csproj
dotnet restore KitchenCommandCenter.sln
dotnet list src/KCC.Contributions/KCC.Contributions.csproj package --include-transitive \
  | grep -E "Microsoft\.EntityFrameworkCore(\.Sqlite)? "
```

Expected: `Microsoft.EntityFrameworkCore` and `Microsoft.EntityFrameworkCore.Sqlite` resolve to the same 10.0.x
version, which is 10.0.11 with Umbraco 17.7.0. If the version differs, use it for `Microsoft.EntityFrameworkCore.Design`
here and for `dotnet-ef` in Step 7.

- [ ] **Step 3: Write the failing store tests**

Create `tests/KCC.IntegrationTests/AssemblyInfo.cs`:

```csharp
// Every test shares one SQLite database. Umbraco's relation update after a content, media or member save reads and
// then writes without a write lock, so a commit by a parallel test in between leaves it retrying a stale snapshot
// for minutes. One test at a time keeps the suite deterministic.
[assembly: NotInParallel]
```

Create `tests/KCC.IntegrationTests/Features/Contributions/ContributionsStoreTests.cs`:

```csharp
using KCC.Contributions.Data;
using KCC.IntegrationTests.Config;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace KCC.IntegrationTests.Features.Contributions;

public class ContributionsStoreTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    private IEFCoreScopeProvider<ContributionsDbContext> Scopes =>
        Site.Services.GetRequiredService<IEFCoreScopeProvider<ContributionsDbContext>>();

    private IKeyValueService KeyValues => Site.Services.GetRequiredService<IKeyValueService>();

    [Test]
    public async Task FirstBoot_CreatesTheTablesAndTheLockRow()
    {
        var tables = await ScalarAsync(
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name IN ('kccReview', 'kccCookNote', 'kccCookedMark')");
        var lockRows = await ScalarAsync($"SELECT COUNT(*) FROM umbracoLock WHERE id = {ContributionLocks.Contributions}");

        _ = await Assert.That(tables).IsEqualTo(3L);
        _ = await Assert.That(lockRows).IsEqualTo(1L);
    }

    [Test]
    public async Task SecondReviewByOneMember_IsRefusedByTheUniqueIndex()
    {
        var variantKey = Guid.NewGuid();
        var memberKey = Guid.NewGuid();
        await WriteAsync(db => db.Reviews.Add(NewReview(variantKey, memberKey)));

        _ = await Assert.That(async () => await WriteAsync(db => db.Reviews.Add(NewReview(variantKey, memberKey))))
            .Throws<DbUpdateException>();
    }

    [Test]
    public async Task SecondCookedMarkByOneMember_IsRefusedByTheUniqueIndex()
    {
        var variantKey = Guid.NewGuid();
        var memberKey = Guid.NewGuid();
        await WriteAsync(db => db.CookedMarks.Add(NewCookedMark(variantKey, memberKey)));

        _ = await Assert.That(async () => await WriteAsync(db => db.CookedMarks.Add(NewCookedMark(variantKey, memberKey))))
            .Throws<DbUpdateException>();
    }

    [Test]
    public async Task EfCoreWrite_RollsBackWithTheUmbracoScopeAroundIt()
    {
        var key = $"kcc-proof-{Guid.NewGuid():N}";
        var variantKey = Guid.NewGuid();

        using (Site.Services.GetRequiredService<ICoreScopeProvider>().CreateCoreScope())
        {
            KeyValues.SetValue(key, "written");
            await WriteAsync(db => db.Reviews.Add(NewReview(variantKey, Guid.NewGuid())));
        }

        _ = await Assert.That(KeyValues.GetValue(key)).IsNull();
        _ = await Assert.That(await CountReviewsAsync(variantKey)).IsEqualTo(0);
    }

    [Test]
    public async Task EfCoreWriteLock_HoldsBackAConcurrentUmbracoWrite()
    {
        var key = $"kcc-proof-{Guid.NewGuid():N}";
        var holding = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // Each writer needs its own ambient scope, so neither may inherit this test's execution context.
        Task writer;
        using (ExecutionContext.SuppressFlow())
        {
            writer = Task.Run(async () =>
            {
                using var scope = Scopes.CreateScope();
                scope.WriteLock(ContributionLocks.Contributions);
                await scope.ExecuteWithContextAsync<Task>(async db =>
                {
                    db.Reviews.Add(NewReview(Guid.NewGuid(), Guid.NewGuid()));
                    await db.SaveChangesAsync();
                });
                holding.SetResult();
                await release.Task;
                scope.Complete();
            });
        }

        await holding.Task;

        Task<DateTime> umbracoWrite;
        using (ExecutionContext.SuppressFlow())
        {
            umbracoWrite = Task.Run(() =>
            {
                KeyValues.SetValue(key, "after");
                return DateTime.UtcNow;
            });
        }

        await Task.Delay(TimeSpan.FromSeconds(1));
        var heldBack = !umbracoWrite.IsCompleted;
        var releasedAt = DateTime.UtcNow;
        release.SetResult();
        await writer;
        var writtenAt = await umbracoWrite;

        _ = await Assert.That(heldBack).IsTrue();
        _ = await Assert.That(writtenAt).IsGreaterThanOrEqualTo(releasedAt);
        _ = await Assert.That(KeyValues.GetValue(key)).IsEqualTo("after");
    }

    private static Review NewReview(Guid variantKey, Guid memberKey) => new()
    {
        VariantKey = variantKey,
        MemberKey = memberKey,
        Rating = 4m,
        Created = DateTime.UtcNow,
        Modified = DateTime.UtcNow,
    };

    private static CookedMark NewCookedMark(Guid variantKey, Guid memberKey) => new()
    {
        VariantKey = variantKey,
        MemberKey = memberKey,
        Created = DateTime.UtcNow,
    };

    private async Task WriteAsync(Action<ContributionsDbContext> change)
    {
        using var scope = Scopes.CreateScope();
        scope.WriteLock(ContributionLocks.Contributions);
        await scope.ExecuteWithContextAsync<Task>(async db =>
        {
            change(db);
            await db.SaveChangesAsync();
        });
        scope.Complete();
    }

    private async Task<int> CountReviewsAsync(Guid variantKey)
    {
        using var scope = Scopes.CreateScope();
        var count = await scope.ExecuteWithContextAsync(db => db.Reviews.CountAsync(review => review.VariantKey == variantKey));
        scope.Complete();
        return count;
    }

    private async Task<long> ScalarAsync(string sql)
    {
        await using var connection = new SqliteConnection($"Data Source={Site.DatabasePath};Pooling=False");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (long)(await command.ExecuteScalarAsync())!;
    }
}
```

The last two tests are the §19 proof:
- `EfCoreWrite_RollsBackWithTheUmbracoScopeAroundIt` shows that EF Core joins the ambient Umbraco transaction.
- `EfCoreWriteLock_HoldsBackAConcurrentUmbracoWrite` shows that the EF scope's lock serializes it against
  Umbraco's own writes.

- [ ] **Step 4: Run them to confirm they fail**

```bash
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/ContributionsStoreTests/*"
```

Expected: build errors, because `ContributionsDbContext`, `Review`, `CookedMark` and `ContributionLocks` do not exist.

- [ ] **Step 5: Write the entities, the lock id and the context**

Create `src/KCC.Contributions/Data/Review.cs`:

```csharp
namespace KCC.Contributions.Data;

public class Review
{
    public int Id { get; set; }

    public Guid VariantKey { get; set; }

    public Guid MemberKey { get; set; }

    public decimal Rating { get; set; }

    public string Text { get; set; }

    public DateTime Created { get; set; }

    public DateTime Modified { get; set; }
}
```

Create `src/KCC.Contributions/Data/CookNote.cs`:

```csharp
namespace KCC.Contributions.Data;

public class CookNote
{
    public int Id { get; set; }

    public Guid VariantKey { get; set; }

    public Guid MemberKey { get; set; }

    public string Text { get; set; }

    public DateTime Created { get; set; }

    public DateTime Modified { get; set; }
}
```

Create `src/KCC.Contributions/Data/CookedMark.cs`:

```csharp
namespace KCC.Contributions.Data;

public class CookedMark
{
    public int Id { get; set; }

    public Guid VariantKey { get; set; }

    public Guid MemberKey { get; set; }

    public DateTime Created { get; set; }
}
```

Create `src/KCC.Contributions/Data/ContributionLocks.cs`:

```csharp
namespace KCC.Contributions.Data;

public static class ContributionLocks
{
    // A row in Umbraco's umbracoLock table, inserted by the initial migration. Umbraco's own lock ids are all
    // negative, so a positive id cannot collide with one a later Umbraco release adds.
    public const int Contributions = 1_000_001;
}
```

Create `src/KCC.Contributions/Data/UtcDateTimeConverter.cs`:

```csharp
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace KCC.Contributions.Data;

// SQLite keeps a DateTime as text with no zone, so it reads back Unspecified, and a browser would take the JSON for
// local time. Every time this store writes is UTC.
internal sealed class UtcDateTimeConverter()
    : ValueConverter<DateTime, DateTime>(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
```

Create `src/KCC.Contributions/Data/ContributionsDbContext.cs`:

```csharp
using Microsoft.EntityFrameworkCore;

namespace KCC.Contributions.Data;

public class ContributionsDbContext(DbContextOptions<ContributionsDbContext> options) : DbContext(options)
{
    public const int MaxTextLength = 4000;

    public DbSet<Review> Reviews => Set<Review>();

    public DbSet<CookNote> CookNotes => Set<CookNote>();

    public DbSet<CookedMark> CookedMarks => Set<CookedMark>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Review>(review =>
        {
            review.ToTable("kccReview");
            review.HasKey(r => r.Id);
            review.Property(r => r.Id).HasColumnName("id");
            review.Property(r => r.VariantKey).HasColumnName("variantKey");
            review.Property(r => r.MemberKey).HasColumnName("memberKey");

            // SQLite has no decimal type, so EF would store the rating as text, which SQL cannot sum or order.
            // Half-star steps are exact as doubles.
            review.Property(r => r.Rating).HasColumnName("rating").HasConversion<double>();
            review.Property(r => r.Text).HasColumnName("text").HasMaxLength(MaxTextLength);
            review.Property(r => r.Created).HasColumnName("created");
            review.Property(r => r.Modified).HasColumnName("modified");
            review.HasIndex(r => new { r.VariantKey, r.MemberKey }).IsUnique();
        });

        modelBuilder.Entity<CookNote>(note =>
        {
            note.ToTable("kccCookNote");
            note.HasKey(n => n.Id);
            note.Property(n => n.Id).HasColumnName("id");
            note.Property(n => n.VariantKey).HasColumnName("variantKey");
            note.Property(n => n.MemberKey).HasColumnName("memberKey");
            note.Property(n => n.Text).HasColumnName("text").HasMaxLength(MaxTextLength).IsRequired();
            note.Property(n => n.Created).HasColumnName("created");
            note.Property(n => n.Modified).HasColumnName("modified");
            note.HasIndex(n => n.VariantKey);
        });

        modelBuilder.Entity<CookedMark>(cooked =>
        {
            cooked.ToTable("kccCookedMark");
            cooked.HasKey(c => c.Id);
            cooked.Property(c => c.Id).HasColumnName("id");
            cooked.Property(c => c.VariantKey).HasColumnName("variantKey");
            cooked.Property(c => c.MemberKey).HasColumnName("memberKey");
            cooked.Property(c => c.Created).HasColumnName("created");
            cooked.HasIndex(c => new { c.VariantKey, c.MemberKey }).IsUnique();
        });
    }
}
```

- [ ] **Step 6: Write the design-time factory, the startup migration and the composer**

Create `src/KCC.Contributions/Data/ContributionsDesignTimeFactory.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KCC.Contributions.Data;

// dotnet ef reads the model through this factory, so generating a migration never boots Umbraco.
public class ContributionsDesignTimeFactory : IDesignTimeDbContextFactory<ContributionsDbContext>
{
    public ContributionsDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<ContributionsDbContext>().UseSqlite("Data Source=contributions-design.db").Options);
}
```

Create `src/KCC.Contributions/Data/RunContributionsMigrations.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace KCC.Contributions.Data;

public class RunContributionsMigrations(IDbContextFactory<ContributionsDbContext> contextFactory, IRuntimeState runtimeState)
    : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        if (runtimeState.Level != RuntimeLevel.Run)
        {
            return;
        }

        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
    }
}
```

Create `src/KCC.Contributions/ContributionsComposer.cs`:

```csharp
using KCC.Contributions.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace KCC.Contributions;

public class ContributionsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddUmbracoDbContext<ContributionsDbContext>(
            (IServiceProvider services, DbContextOptionsBuilder options, string connectionString, string providerName) =>
            {
                if (!string.IsNullOrEmpty(providerName) && !string.IsNullOrEmpty(connectionString))
                {
                    options.UseDatabaseProvider(providerName, connectionString);
                }
            },
            shareUmbracoConnection: true);

        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, RunContributionsMigrations>();
    }
}
```

This is the only non-obsolete overload of `AddUmbracoDbContext`. `shareUmbracoConnection: true` puts EF Core on the
ambient Umbraco scope's connection and transaction, which is the whole of the §19 behaviour.

- [ ] **Step 7: Generate the migration**

```bash
dotnet new tool-manifest
dotnet tool install dotnet-ef --version 10.0.11
dotnet build src/KCC.Web/KCC.Web.csproj
dotnet dotnet-ef migrations add InitialContributions --project src/KCC.Contributions --startup-project src/KCC.Web \
  --context ContributionsDbContext --output-dir Data/Migrations
ls src/KCC.Contributions/Data/Migrations
```

Expected:
- `dotnet-tools.json` appears at the repo root. The .NET 10 SDK writes it there rather than under `.config/`.
- The migrations folder holds `<timestamp>_InitialContributions.cs`, `<timestamp>_InitialContributions.Designer.cs`
  and `ContributionsDbContextModelSnapshot.cs`, in namespace `KCC.Contributions.Data.Migrations`.
- The migration creates `kccReview`, `kccCookNote` and `kccCookedMark`. `rating` is a `REAL` column, and the
  migration adds the indexes `IX_kccReview_variantKey_memberKey` (unique), `IX_kccCookNote_variantKey` and
  `IX_kccCookedMark_variantKey_memberKey` (unique).

- [ ] **Step 8: Add the lock row to the migration**

In `<timestamp>_InitialContributions.cs`, add this as the last statement of `Up`:

```csharp
            migrationBuilder.Sql(
                $"INSERT INTO umbracoLock (id, value, name) VALUES ({ContributionLocks.Contributions}, 1, 'KccContributions');");
```

and this as the first statement of `Down`:

```csharp
            migrationBuilder.Sql($"DELETE FROM umbracoLock WHERE id = {ContributionLocks.Contributions};");
```

`ContributionLocks` resolves from the enclosing `KCC.Contributions.Data` namespace, so no using is needed.
`scope.WriteLock(id)` updates this row, and it fails if the row is missing.

- [ ] **Step 9: Run the store tests**

```bash
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/ContributionsStoreTests/*"
```

Expected: 5 passed.

If either of the last two tests fails, spec §19 row 3 does not hold. **Stop and tell the owner.** The fallback is
NPoco on Umbraco's own scope: re-plan Tasks 1 and 3 on it before continuing. It would need:
- DTO classes with `[TableName]`, `[PrimaryKey]` and `[Index]`;
- a `PackageMigrationPlan` that creates the tables and inserts the lock row;
- stores that open `IScopeProvider.CreateScope()` (namespace `Umbraco.Cms.Infrastructure.Scoping`), call
  `scope.WriteLock(ContributionLocks.Contributions)` and query through `scope.Database`.

- [ ] **Step 10: Build and run every suite**

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj
```

Expected: `Build succeeded` with 0 warnings, and both suites green. The integration suite now runs one test at a time.

- [ ] **Step 11: Commit**

```bash
git add -A src/KCC.Contributions tests/KCC.UnitTests/Features/Contributions tests/KCC.IntegrationTests \
  dotnet-tools.json Directory.Packages.props KitchenCommandCenter.sln src/KCC.Web/KCC.Web.csproj
git commit -m "Rebuild the Contributions Store on EF Core"
```

---

### Task 2: The recipe schema

A dev-only bootstrap does five things, the same way Phase 1's schema bootstrap did:
- creates the six data types and the two document types;
- lets the recipe listing page hold recipes, shown as a collection sorted with the last edited first (spec §7);
- adds first and last name to the member type (spec §8);
- regenerates the models;
- exports everything through uSync on save.

Keys come from the same derivation as Phase 1's (`kcc-baseline:<name>`), so a fresh database reproduces the same
files. The bootstrap is committed once and then removed. The uSync files and the generated models remain.

**Files:**
- Create, run once, then delete: `src/KCC.Web/Features/DevTools/Baseline/RecipeSchemaBootstrap.cs`
- Modify, then restore: `src/KCC.Web/Features/DevTools/Baseline/{BaselineComposer,BaselineApiController}.cs`
- Generated and committed:
  - `src/KCC.Web/uSync/v17/ContentTypes/{recipe,recipevariant,recipelistingpage}.config`
  - six new `src/KCC.Web/uSync/v17/DataTypes/kcc-*.config`
  - the member type under `src/KCC.Web/uSync/v17/MemberTypes/`
  - `src/KCC.Web/Features/Models/Generated/{Recipe,RecipeVariant,Member}.generated.cs`, plus whatever else
    ModelsBuilder rewrites
- Create: `tests/KCC.IntegrationTests/Features/Recipes/RecipeSchemaTests.cs`

**Interfaces:**
- Produces:
  - Document types `recipe` and `recipeVariant` (keys `kcc-baseline:contentType:recipe` and
    `kcc-baseline:contentType:recipeVariant`). Both compose `metadata`. `recipe` allows `recipeVariant` children.
  - Generated models `Recipe` and `RecipeVariant` in `KCC.Web.Features.Models.Generated`, with the property types
    listed under Global Constraints.
  - `recipeListingPage` allows `recipe`, and its collection is "List View - Content"
    (`Constants.DataTypes.Guids.ListViewContentGuid`, sorted by update date, newest first).
  - The default member type gains `firstName` and `lastName`, and the generated `Member` gains `FirstName` and
    `LastName`.

- [ ] **Step 1: Write the failing schema tests**

Create `tests/KCC.IntegrationTests/Features/Recipes/RecipeSchemaTests.cs`:

```csharp
using KCC.IntegrationTests.Config;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;

namespace KCC.IntegrationTests.Features.Recipes;

public class RecipeSchemaTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    private IContentTypeService ContentTypes => Site.Services.GetRequiredService<IContentTypeService>();

    [Test]
    [Arguments("recipe")]
    [Arguments("recipeVariant")]
    public async Task RecipeType_ComposesMetadata(string alias)
    {
        var type = ContentTypes.Get(alias);

        _ = await Assert.That(type).IsNotNull();
        _ = await Assert.That(type!.ContentTypeComposition.Any(composed => composed.Alias == "metadata")).IsTrue();
    }

    [Test]
    [Arguments("recipe", "description,icon,image,category,author")]
    [Arguments("recipeVariant", "description,icon,images,difficulty,tags,author,prepTime,cookTime,servings,ingredients,instructions,calories,proteinG,carbsG,fatG,saturatedFatG,fiberG,sugarG,sodiumMg")]
    public async Task RecipeType_HasItsProperties(string alias, string properties)
    {
        var type = ContentTypes.Get(alias)!;

        var missing = properties.Split(',').Where(property => !type.PropertyTypeExists(property));

        _ = await Assert.That(string.Join(",", missing)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task RecipeListing_AllowsRecipesAndListsThemAsACollection()
    {
        var listing = ContentTypes.Get("recipeListingPage")!;

        _ = await Assert.That(listing.AllowedContentTypes!.Any(allowed => allowed.Alias == "recipe")).IsTrue();
        _ = await Assert.That(listing.ListView).IsEqualTo(Constants.DataTypes.Guids.ListViewContentGuid);
    }

    [Test]
    public async Task Recipe_AllowsVariants()
    {
        var recipe = ContentTypes.Get("recipe")!;

        _ = await Assert.That(recipe.AllowedContentTypes!.Any(allowed => allowed.Alias == "recipeVariant")).IsTrue();
    }

    [Test]
    public async Task MemberType_HasFirstAndLastName()
    {
        var member = Site.Services.GetRequiredService<IMemberTypeService>().Get(Constants.Security.DefaultMemberTypeAlias)!;

        _ = await Assert.That(member.PropertyTypeExists("firstName")).IsTrue();
        _ = await Assert.That(member.PropertyTypeExists("lastName")).IsTrue();
    }
}
```

```bash
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/RecipeSchemaTests/*"
```

Expected: all FAIL, because neither type exists yet.

- [ ] **Step 2: Write the bootstrap**

Create `src/KCC.Web/Features/DevTools/Baseline/RecipeSchemaBootstrap.cs`:

```csharp
using System.Security.Cryptography;
using System.Text;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentTypeEditing;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.ContentTypeEditing;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.ModelsBuilder.Building;

namespace KCC.Web.Features.DevTools.Baseline;

public class RecipeSchemaBootstrap(
    IDataTypeService dataTypeService,
    PropertyEditorCollection propertyEditors,
    IConfigurationEditorJsonSerializer configurationSerializer,
    IContentTypeService contentTypeService,
    IContentTypeEditingService contentTypeEditingService,
    IMemberTypeService memberTypeService,
    IShortStringHelper shortStringHelper,
    IModelsGenerator modelsGenerator)
{
    private static readonly string[] Difficulties = ["Easy", "Medium", "Hard"];

    private static readonly string[] MetadataComposition = ["metadata"];

    public async Task<IReadOnlyList<string>> RunAsync()
    {
        var created = new List<string>();

        var textstring = Constants.DataTypes.Guids.TextstringGuid;
        var textarea = Constants.DataTypes.Guids.TextareaGuid;
        var numeric = Constants.DataTypes.Guids.NumericGuid;
        var image = Constants.DataTypes.Guids.MediaPicker3SingleImageGuid;
        var images = Constants.DataTypes.Guids.MediaPicker3MultipleImagesGuid;
        var member = Constants.DataTypes.Guids.MemberPickerGuid;

        var icon = await EnsureDataTypeAsync(
            created,
            "KCC Recipe Icon",
            Constants.PropertyEditors.Aliases.TextBox,
            "Umb.PropertyEditorUi.TextBox",
            new() { ["maxChars"] = 200 });
        var ingredients = await EnsureDataTypeAsync(
            created,
            "KCC Ingredients",
            Constants.PropertyEditors.Aliases.TextArea,
            "Umb.PropertyEditorUi.TextArea",
            []);
        var instructions = await EnsureDataTypeAsync(
            created,
            "KCC Instructions",
            Constants.PropertyEditors.Aliases.TextArea,
            "Umb.PropertyEditorUi.TextArea",
            []);
        var difficulty = await EnsureDataTypeAsync(
            created,
            "KCC Difficulty",
            Constants.PropertyEditors.Aliases.DropDownListFlexible,
            "Umb.PropertyEditorUi.Dropdown",
            new() { ["items"] = Difficulties, ["multiple"] = false });
        var category = await EnsureDataTypeAsync(
            created,
            "KCC Recipe Category",
            Constants.PropertyEditors.Aliases.MultiNodeTreePicker,
            "Umb.PropertyEditorUi.ContentPicker",
            PickerOf("recipeCategory", maxNumber: 1));
        var tags = await EnsureDataTypeAsync(
            created,
            "KCC Recipe Tags",
            Constants.PropertyEditors.Aliases.MultiNodeTreePicker,
            "Umb.PropertyEditorUi.ContentPicker",
            PickerOf("recipeTag", maxNumber: 0));

        await EnsureContentTypeAsync(created, new("recipeVariant", "Recipe Variant", "icon-article")
        {
            Compositions = MetadataComposition,
            Properties =
            [
                new("description", "Description", textarea, "Variant", Mandatory: true),
                new("icon", "Icon", icon, "Variant", Mandatory: true),
                new("images", "Images", images, "Variant"),
                new("difficulty", "Difficulty", difficulty, "Variant"),
                new("tags", "Tags", tags, "Variant"),
                new("author", "Author", member, "Variant"),
                new("prepTime", "Prep time (minutes)", numeric, "Timing", Mandatory: true),
                new("cookTime", "Cook time (minutes)", numeric, "Timing", Mandatory: true),
                new("servings", "Servings", numeric, "Timing", Mandatory: true),
                new("ingredients", "Ingredients", ingredients, "Method", Mandatory: true),
                new("instructions", "Instructions", instructions, "Method", Mandatory: true),
                new("calories", "Calories (per serving)", numeric, "Nutrition"),
                new("proteinG", "Protein (g, per serving)", numeric, "Nutrition"),
                new("carbsG", "Carbs (g, per serving)", numeric, "Nutrition"),
                new("fatG", "Fat (g, per serving)", numeric, "Nutrition"),
                new("saturatedFatG", "Saturated fat (g, per serving)", numeric, "Nutrition"),
                new("fiberG", "Fiber (g, per serving)", numeric, "Nutrition"),
                new("sugarG", "Sugar (g, per serving)", numeric, "Nutrition"),
                new("sodiumMg", "Sodium (mg, per serving)", numeric, "Nutrition"),
            ],
        });
        await EnsureContentTypeAsync(created, new("recipe", "Recipe", "icon-book")
        {
            Compositions = MetadataComposition,
            AllowedChildren = ["recipeVariant"],
            Properties =
            [
                new("description", "Description", textarea, "Recipe", Mandatory: true),
                new("icon", "Icon", icon, "Recipe", Mandatory: true),
                new("image", "Image", image, "Recipe"),
                new("category", "Category", category, "Recipe"),
                new("author", "Author", member, "Recipe"),
            ],
        });

        await AllowRecipesUnderTheListingAsync(created);
        await AddMemberNamesAsync(created, await dataTypeService.GetAsync(textstring));

        modelsGenerator.GenerateModels();
        return created;
    }

    private static Guid Key(string name) =>
        new(SHA256.HashData(Encoding.UTF8.GetBytes($"kcc-baseline:{name}")).AsSpan(0, 16));

    private Dictionary<string, object> PickerOf(string contentTypeAlias, int maxNumber) => new()
    {
        ["startNode"] = new { type = "content" },
        ["minNumber"] = 0,
        ["maxNumber"] = maxNumber,
        ["filter"] = RequireContentType(contentTypeAlias).Key.ToString(),
    };

    private IContentType RequireContentType(string alias) =>
        contentTypeService.Get(alias) ?? throw new InvalidOperationException($"Document type {alias} is missing; Phase 1's baseline creates it.");

    private async Task AllowRecipesUnderTheListingAsync(List<string> created)
    {
        var listing = RequireContentType("recipeListingPage");
        if (listing.AllowedContentTypes?.Any(allowed => allowed.Alias == "recipe") == true)
        {
            return;
        }

        var allowedTypes = listing.AllowedContentTypes?.ToList() ?? [];
        allowedTypes.Add(new ContentTypeSort(RequireContentType("recipe").Key, allowedTypes.Count, "recipe"));
        listing.AllowedContentTypes = allowedTypes;
        listing.ListView = Constants.DataTypes.Guids.ListViewContentGuid;

        var result = await contentTypeService.UpdateAsync(listing, Constants.Security.SuperUserKey);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Updating recipeListingPage failed: {result.Result}.");
        }

        created.Add("recipeListingPage: recipes allowed, collection view");
    }

    private async Task AddMemberNamesAsync(List<string> created, IDataType textstring)
    {
        var memberType = memberTypeService.Get(Constants.Security.DefaultMemberTypeAlias)
            ?? throw new InvalidOperationException("The default member type is missing.");
        if (memberType.PropertyTypeExists("firstName"))
        {
            return;
        }

        memberType.AddPropertyType(
            new PropertyType(shortStringHelper, textstring, "firstName") { Name = "First name", Key = Key("property:Member:firstName"), SortOrder = 1 },
            Constants.Conventions.Member.StandardPropertiesGroupAlias);
        memberType.AddPropertyType(
            new PropertyType(shortStringHelper, textstring, "lastName") { Name = "Last name", Key = Key("property:Member:lastName"), SortOrder = 2 },
            Constants.Conventions.Member.StandardPropertiesGroupAlias);

        var result = await memberTypeService.UpdateAsync(memberType, Constants.Security.SuperUserKey);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Updating the member type failed: {result.Result}.");
        }

        created.Add("Member: firstName, lastName");
    }

    private async Task<Guid> EnsureDataTypeAsync(List<string> created, string name, string editorAlias, string editorUiAlias, Dictionary<string, object> configuration)
    {
        var key = Key($"dataType:{name}");
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

    private async Task EnsureContentTypeAsync(List<string> created, TypeSpec spec)
    {
        var key = Key($"contentType:{spec.Alias}");
        if (contentTypeService.Get(key) is not null)
        {
            return;
        }

        var groups = spec.Properties.Select(property => property.Group).Distinct().ToList();
        var model = new ContentTypeCreateModel
        {
            Key = key,
            Alias = spec.Alias,
            Name = spec.Name,
            Icon = spec.Icon,
            Containers = groups.Select((group, index) => new ContentTypePropertyContainerModel
            {
                Key = Key($"container:{spec.Alias}:{group}"),
                Name = group,
                Type = "Group",
                SortOrder = index,
            }).ToList(),
            Properties = spec.Properties.Select((property, index) => new ContentTypePropertyTypeModel
            {
                Key = Key($"property:{spec.Alias}:{property.Alias}"),
                ContainerKey = Key($"container:{spec.Alias}:{property.Group}"),
                SortOrder = index,
                Alias = property.Alias,
                Name = property.Name,
                DataTypeKey = property.DataType,
                Validation = new PropertyTypeValidation { Mandatory = property.Mandatory },
                Appearance = new PropertyTypeAppearance(),
            }).ToList(),
            Compositions = spec.Compositions.Select(alias => new Composition
            {
                Key = RequireContentType(alias).Key,
                CompositionType = CompositionType.Composition,
            }).ToList(),
            AllowedContentTypes = spec.AllowedChildren
                .Select((alias, index) => new ContentTypeSort(Key($"contentType:{alias}"), index, alias))
                .ToList(),
        };

        var result = await contentTypeEditingService.CreateAsync(model, Constants.Security.SuperUserKey);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Creating document type {spec.Alias} failed: {result.Status}.");
        }

        created.Add(spec.Alias);
    }

    private sealed record PropertySpec(string Alias, string Name, Guid DataType, string Group, bool Mandatory = false);

    private sealed record TypeSpec(string Alias, string Name, string Icon)
    {
        public IReadOnlyList<PropertySpec> Properties { get; init; } = [];

        public IReadOnlyList<string> Compositions { get; init; } = [];

        public IReadOnlyList<string> AllowedChildren { get; init; } = [];
    }
}
```

The bootstrap makes these choices:
- **Ingredients and instructions** are plain text areas holding the same JSON as today. Phase 5 replaces their
  editors without changing the stored value.
- **Icon** is a text box. Phase 5's icon picker takes its place.
- **Difficulty** stays a three-option dropdown (Easy, Medium, Hard). The variant view already draws it.
- **`recipeVariant` is created first**, so that `recipe` can allow it as a child.

- [ ] **Step 3: Register it and expose a dev-only endpoint**

In `src/KCC.Web/Features/DevTools/Baseline/BaselineComposer.cs`, add
`builder.Services.AddTransient<RecipeSchemaBootstrap>();` after the `BaselineExport` registration.

In `src/KCC.Web/Features/DevTools/Baseline/BaselineApiController.cs`, add this action after `Export`:

```csharp
    [HttpPost("recipe-schema")]
    public async Task<IActionResult> CreateRecipeSchema([FromServices] RecipeSchemaBootstrap bootstrap)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        return Ok(await bootstrap.RunAsync());
    }
```

**If Phase 1 took the §19 template fallback** (reconciliation item 12), both new types need the shared template:
1. Inject `ITemplateService templateService` (namespace `Umbraco.Cms.Core.Services`) into the bootstrap.
2. In `EnsureContentTypeAsync`, first resolve `var page = await templateService.GetAsync("page");`.
3. Then set `AllowedTemplateKeys = [page!.Key]` and `DefaultTemplateKey = page.Key` on the `ContentTypeCreateModel`.

- [ ] **Step 4: Run the bootstrap on the dev site**

```bash
dotnet build src/KCC.Web/KCC.Web.csproj
cd src/KCC.Web && dotnet run --launch-profile Local
```

Use plain `dotnet run`, not `dotnet watch`. The generated models would otherwise restart the app mid-request. Once the
site is listening, run this in a second terminal:

```bash
curl -sk -X POST https://localhost:58671/api/dev/baseline/recipe-schema
```

Expected:

```json
["KCC Recipe Icon","KCC Ingredients","KCC Instructions","KCC Difficulty","KCC Recipe Category","KCC Recipe Tags","recipeVariant","recipe","recipeListingPage: recipes allowed, collection view","Member: firstName, lastName"]
```

Stop the site (Ctrl+C), then check what uSync and ModelsBuilder wrote:

```bash
git status --short src/KCC.Web/uSync src/KCC.Web/Features/Models/Generated
ls src/KCC.Web/uSync/v17/DataTypes | grep -ci 'kcc'
grep -c "<ListView>c0808dd3-8133-4e4b-8ce8-e2bea84a96a4</ListView>" src/KCC.Web/uSync/v17/ContentTypes/recipelistingpage.config
grep -rcE "<Alias>(firstName|lastName)</Alias>" src/KCC.Web/uSync/v17/MemberTypes/
grep -n "public virtual" src/KCC.Web/Features/Models/Generated/Recipe.generated.cs \
  src/KCC.Web/Features/Models/Generated/RecipeVariant.generated.cs
```

Expected:
- `git status` shows:
  - new files `ContentTypes/recipe.config` and `ContentTypes/recipevariant.config`, and six new `DataTypes/kcc-*.config`;
  - modified `ContentTypes/recipelistingpage.config` and the member type file;
  - new `Recipe.generated.cs` and `RecipeVariant.generated.cs`, and a modified `Member.generated.cs`.
- There are 10 KCC data types: Phase 1's four plus these six.
- The collection check prints `1`, and the member type file has 2 name aliases.
- The generated members are typed exactly as listed under Global Constraints (`Category` is an `IPublishedContent`,
  `Tags` an `IEnumerable<IPublishedContent>`, and so on). If a picker came out as a different type, Tasks 6, 8 and 10
  must follow the generated type. Record the difference in the task report.

- [ ] **Step 5: Build, then run the schema tests on a fresh database**

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj
```

Expected: `Build succeeded` with 0 warnings, and every integration test passes. The test host is a fresh database,
so it imported the new types from `uSync/v17/` on first boot.

- [ ] **Step 6: Commit the schema**

```bash
git add src/KCC.Web/Features/DevTools/Baseline src/KCC.Web/uSync src/KCC.Web/Features/Models/Generated tests/KCC.IntegrationTests
git commit -m "Add the Recipe and Variant Document Types"
```

- [ ] **Step 7: Remove the one-off bootstrap**

```bash
git rm -q src/KCC.Web/Features/DevTools/Baseline/RecipeSchemaBootstrap.cs
```

Then undo Step 3's changes:
- Delete the `RecipeSchemaBootstrap` registration from `BaselineComposer`.
- Delete the `CreateRecipeSchema` action from `BaselineApiController`.

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj
git add -A src/KCC.Web/Features/DevTools/Baseline
git commit -m "Remove the One-Off Recipe Schema Bootstrap"
```

Expected: the build is clean and every integration test passes.

---

### Task 3: Contribution stats, reads and the review upsert

This task builds the read side the pages need, plus the one write the seeder needs:
- rating maths and a cached snapshot of every variant's ratings and cook counts (spec §6.4: cached in
  `IMemoryCache`, invalidated on write);
- paged review and note reads;
- the review upsert.

Phase 4 adds the other writes behind the same interface.

The old unit tests for the Kentico providers' static helpers move over with the logic: `RatingMathTests` and
`ContributionStatsTests` cover the averaging, the half-star validation and the distribution buckets. The one helper
that does not move is `CanModify`, the author check; Phase 4's write tests pick it up.

**Files:**
- Create:
  - `src/KCC.Contributions/{RatingMath,ContributionStats,ContributionStatsSource,ContributionReads,ContributionWrites}.cs`
  - `tests/KCC.UnitTests/Features/Contributions/{RatingMathTests,ContributionStatsTests}.cs`
  - `tests/KCC.IntegrationTests/Features/Contributions/ContributionReadsTests.cs`
- Modify: `src/KCC.Contributions/ContributionsComposer.cs`, `tests/KCC.UnitTests/KCC.UnitTests.csproj`
- Delete: `tests/KCC.UnitTests/Features/Contributions/{VariantReviewAggregationTests,VariantCookedAggregationTests,CookNoteAggregationTests}.cs`

**Interfaces:**
- Consumes: `ContributionsDbContext`, `ContributionLocks` and `IEFCoreScopeProvider<ContributionsDbContext>`, all
  from Task 1.
- Produces (namespace `KCC.Contributions`):
  - `RatingMath`:
    - `IsValidRating(decimal) → bool` accepts 0.5 to 5 in half steps.
    - `ClampText(string) → string` trims the text, returns null for blank text, and cuts it at 4000 characters.
    - `Distribution(IEnumerable<decimal>) → int[5]` counts ratings into buckets, where index 0 is 1★.
  - `readonly record struct RatingAggregate(double Average, int Count)`.
  - `record VariantStats(decimal RatingSum, int ReviewCount, IReadOnlyList<int> Distribution, int CookedCount)`, with
    `RatingAggregate Rating` and `static VariantStats None`.
  - `ContributionStats`:
    - `static Build(IEnumerable<(Guid VariantKey, decimal Rating)> reviews, IEnumerable<Guid> cookedVariantKeys)`
    - `For(Guid variantKey) → VariantStats`
    - `RatingAcross(IEnumerable<Guid> variantKeys) → RatingAggregate`
    - `CookedAcross(IEnumerable<Guid> variantKeys) → int`
  - `IContributionStats`: `Task<ContributionStats> GetAsync()` and `void Invalidate()`. Registered as a singleton.
  - `record Paged<T>(IReadOnlyList<T> Items, int Total)`.
  - `IContributionReads`, returning results newest first, with page sizes clamped to 1 through
    `ContributionReads.MaxPageSize` (50):
    - `ReviewsAsync(Guid variantKey, int page, int pageSize) → Task<Paged<Review>>`
    - `MemberReviewAsync(Guid variantKey, Guid memberKey) → Task<Review>`, which is null when the member has no review
    - `NotesAsync(Guid variantKey, int page, int pageSize) → Task<Paged<CookNote>>`
    - `HasCookedAsync(Guid variantKey, Guid memberKey) → Task<bool>`
  - `IContributionWrites`: `Task UpsertReviewAsync(Guid variantKey, Guid memberKey, decimal rating, string text)`.
    It throws `ArgumentOutOfRangeException` for an off-step rating and invalidates the stats.

- [ ] **Step 1: Bring the contribution unit tests back and write the failing ones**

Delete the Kentico aggregation tests and the exclusion:

```bash
git rm -q tests/KCC.UnitTests/Features/Contributions/VariantReviewAggregationTests.cs \
  tests/KCC.UnitTests/Features/Contributions/VariantCookedAggregationTests.cs \
  tests/KCC.UnitTests/Features/Contributions/CookNoteAggregationTests.cs
```

In `tests/KCC.UnitTests/KCC.UnitTests.csproj`, delete `<Compile Remove="Features/Contributions/**" />`.

Create `tests/KCC.UnitTests/Features/Contributions/RatingMathTests.cs`:

```csharp
using KCC.Contributions;
using KCC.Contributions.Data;

namespace KCC.UnitTests.Features.Contributions;

public class RatingMathTests
{
    // decimal cannot be an attribute argument, so the cases arrive as doubles.
    [Test]
    [Arguments(0.0)]
    [Arguments(5.5)]
    [Arguments(3.7)]
    [Arguments(-1.0)]
    public async Task IsValidRating_RejectsOffStepAndOutOfRange(double rating)
    {
        _ = await Assert.That(RatingMath.IsValidRating((decimal)rating)).IsFalse();
    }

    [Test]
    [Arguments(0.5)]
    [Arguments(2.5)]
    [Arguments(5.0)]
    public async Task IsValidRating_AcceptsHalfSteps(double rating)
    {
        _ = await Assert.That(RatingMath.IsValidRating((decimal)rating)).IsTrue();
    }

    [Test]
    public async Task ClampText_TrimsAndEmptiesBlanks()
    {
        _ = await Assert.That(RatingMath.ClampText("  tasty  ")).IsEqualTo("tasty");
        _ = await Assert.That(RatingMath.ClampText("   ")).IsNull();
        _ = await Assert.That(RatingMath.ClampText(null)).IsNull();
    }

    [Test]
    public async Task ClampText_CutsAtTheStoredLength()
    {
        var clamped = RatingMath.ClampText(new string('a', ContributionsDbContext.MaxTextLength + 10));

        _ = await Assert.That(clamped.Length).IsEqualTo(ContributionsDbContext.MaxTextLength);
    }

    [Test]
    public async Task Distribution_CountsWholeStarsByBucket()
    {
        var buckets = RatingMath.Distribution([5m, 5m, 4m, 3m, 1m]);

        _ = await Assert.That(string.Join(",", buckets)).IsEqualTo("1,0,1,1,2");
    }

    [Test]
    public async Task Distribution_RoundsHalfStarsDown()
    {
        var buckets = RatingMath.Distribution([4.5m, 3.5m, 1.5m]);

        _ = await Assert.That(string.Join(",", buckets)).IsEqualTo("1,0,1,1,0");
    }

    [Test]
    public async Task Distribution_PutsALoneHalfStarInOneStar()
    {
        var buckets = RatingMath.Distribution([0.5m]);

        _ = await Assert.That(buckets[0]).IsEqualTo(1);
    }
}
```

Create `tests/KCC.UnitTests/Features/Contributions/ContributionStatsTests.cs`:

```csharp
using KCC.Contributions;

namespace KCC.UnitTests.Features.Contributions;

public class ContributionStatsTests
{
    private static readonly Guid First = Guid.NewGuid();
    private static readonly Guid Second = Guid.NewGuid();

    [Test]
    public async Task For_UnknownVariant_HasNoRatingsOrCooks()
    {
        var stats = ContributionStats.Build([], []).For(Guid.NewGuid());

        _ = await Assert.That(stats.Rating).IsEqualTo(new RatingAggregate(0d, 0));
        _ = await Assert.That(stats.CookedCount).IsEqualTo(0);
        _ = await Assert.That(stats.Distribution.Sum()).IsEqualTo(0);
    }

    [Test]
    public async Task For_AveragesTheVariantsOwnRatings()
    {
        var stats = ContributionStats.Build([(First, 5m), (First, 4m), (First, 3m), (Second, 1m)], []);

        _ = await Assert.That(stats.For(First).Rating).IsEqualTo(new RatingAggregate(4d, 3));
    }

    [Test]
    public async Task For_AveragesHalfStarsExactly()
    {
        var stats = ContributionStats.Build([(First, 4.5m), (First, 3.5m)], []);

        _ = await Assert.That(stats.For(First).Rating).IsEqualTo(new RatingAggregate(4d, 2));
    }

    [Test]
    public async Task For_BucketsTheVariantsRatings()
    {
        var stats = ContributionStats.Build([(First, 5m), (First, 4.5m), (Second, 1m)], []);

        _ = await Assert.That(string.Join(",", stats.For(First).Distribution)).IsEqualTo("0,0,0,1,1");
    }

    [Test]
    public async Task For_CountsCooksPerVariant()
    {
        var stats = ContributionStats.Build([], [First, First, Second]);

        _ = await Assert.That(stats.For(First).CookedCount).IsEqualTo(2);
        _ = await Assert.That(stats.For(Second).CookedCount).IsEqualTo(1);
    }

    [Test]
    public async Task RatingAcross_WeighsEveryReviewEqually()
    {
        // The mean of all three reviews is 4; the mean of the two variant averages would be 3.75.
        var stats = ContributionStats.Build([(First, 5m), (First, 4m), (Second, 3m)], []);

        _ = await Assert.That(stats.RatingAcross([First, Second])).IsEqualTo(new RatingAggregate(4d, 3));
    }

    [Test]
    public async Task RatingAcross_LeavesOutVariantsNotAsked()
    {
        var stats = ContributionStats.Build([(First, 5m), (Second, 1m)], []);

        _ = await Assert.That(stats.RatingAcross([First])).IsEqualTo(new RatingAggregate(5d, 1));
    }

    [Test]
    public async Task RatingAcross_CountsARepeatedKeyOnce()
    {
        var stats = ContributionStats.Build([(First, 5m)], []);

        _ = await Assert.That(stats.RatingAcross([First, First]).Count).IsEqualTo(1);
    }

    [Test]
    public async Task CookedAcross_SumsTheVariantsAsked()
    {
        var stats = ContributionStats.Build([], [First, Second, Second, Guid.NewGuid()]);

        _ = await Assert.That(stats.CookedAcross([First, Second])).IsEqualTo(3);
    }
}
```

`RatingAcross_LeavesOutVariantsNotAsked` is the "published variants only" rule (spec §9.1). A recipe asks only about
the variants the published tree gives it, so the reviews of an unpublished variant never count.

```bash
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj -- --treenode-filter "/*/*/RatingMathTests/*"
```

Expected: build errors, because `RatingMath`, `ContributionStats` and `RatingAggregate` do not exist.

- [ ] **Step 2: Write the rating maths and the stats snapshot**

Create `src/KCC.Contributions/RatingMath.cs`:

```csharp
using KCC.Contributions.Data;

namespace KCC.Contributions;

public static class RatingMath
{
    public static bool IsValidRating(decimal rating) =>
        rating >= 0.5m && rating <= 5m && (rating * 2m) % 1m == 0m;

    public static string ClampText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var trimmed = text.Trim();
        return trimmed.Length <= ContributionsDbContext.MaxTextLength ? trimmed : trimmed[..ContributionsDbContext.MaxTextLength];
    }

    // Buckets run from 1★ at index 0 to 5★ at index 4, as the histogram draws them. A half star has not reached
    // the star above it, so it rounds down; a lone 0.5 has no 0★ bucket and counts as 1★.
    public static int[] Distribution(IEnumerable<decimal> ratings)
    {
        var buckets = new int[5];
        foreach (var rating in ratings)
        {
            var star = Math.Clamp((int)Math.Ceiling(rating - 0.5m), 1, 5);
            buckets[star - 1]++;
        }

        return buckets;
    }
}
```

Create `src/KCC.Contributions/ContributionStats.cs`:

```csharp
namespace KCC.Contributions;

public readonly record struct RatingAggregate(double Average, int Count);

public sealed record VariantStats(decimal RatingSum, int ReviewCount, IReadOnlyList<int> Distribution, int CookedCount)
{
    public static VariantStats None { get; } = new(0m, 0, new int[5], 0);

    public RatingAggregate Rating => ReviewCount == 0 ? default : new((double)(RatingSum / ReviewCount), ReviewCount);
}

public sealed class ContributionStats
{
    private readonly IReadOnlyDictionary<Guid, VariantStats> byVariant;

    private ContributionStats(IReadOnlyDictionary<Guid, VariantStats> byVariant) => this.byVariant = byVariant;

    public static ContributionStats Build(IEnumerable<(Guid VariantKey, decimal Rating)> reviews, IEnumerable<Guid> cookedVariantKeys)
    {
        var ratings = reviews
            .GroupBy(review => review.VariantKey)
            .ToDictionary(group => group.Key, group => group.Select(review => review.Rating).ToList());
        var cooked = cookedVariantKeys
            .GroupBy(key => key)
            .ToDictionary(group => group.Key, group => group.Count());

        return new ContributionStats(ratings.Keys.Union(cooked.Keys).ToDictionary(
            key => key,
            key =>
            {
                var variantRatings = ratings.GetValueOrDefault(key) ?? [];
                return new VariantStats(variantRatings.Sum(), variantRatings.Count, RatingMath.Distribution(variantRatings), cooked.GetValueOrDefault(key));
            }));
    }

    public VariantStats For(Guid variantKey) => byVariant.GetValueOrDefault(variantKey) ?? VariantStats.None;

    public RatingAggregate RatingAcross(IEnumerable<Guid> variantKeys)
    {
        var stats = variantKeys.Distinct().Select(For).ToList();
        var count = stats.Sum(variant => variant.ReviewCount);
        return count == 0 ? default : new((double)(stats.Sum(variant => variant.RatingSum) / count), count);
    }

    public int CookedAcross(IEnumerable<Guid> variantKeys) =>
        variantKeys.Distinct().Sum(key => For(key).CookedCount);
}
```

```bash
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj
```

Expected: every unit test passes, including the 7 `RatingMathTests` methods (12 cases) and the 9 `ContributionStatsTests`.

- [ ] **Step 3: Write the failing store-level read tests**

Create `tests/KCC.IntegrationTests/Features/Contributions/ContributionReadsTests.cs`:

```csharp
using KCC.Contributions;
using KCC.Contributions.Data;
using KCC.IntegrationTests.Config;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace KCC.IntegrationTests.Features.Contributions;

public class ContributionReadsTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    private IContributionWrites Writes => Site.Services.GetRequiredService<IContributionWrites>();

    private IContributionReads Reads => Site.Services.GetRequiredService<IContributionReads>();

    private IContributionStats Stats => Site.Services.GetRequiredService<IContributionStats>();

    [Test]
    public async Task UpsertReview_ShowsInTheNextStatsRead()
    {
        var variantKey = Guid.NewGuid();
        _ = (await Stats.GetAsync()).For(variantKey);

        await Writes.UpsertReviewAsync(variantKey, Guid.NewGuid(), 4.5m, "Crispy edges");

        _ = await Assert.That((await Stats.GetAsync()).For(variantKey).Rating).IsEqualTo(new RatingAggregate(4.5d, 1));
    }

    [Test]
    public async Task UpsertReview_ByTheSameMember_ReplacesTheirReview()
    {
        var variantKey = Guid.NewGuid();
        var memberKey = Guid.NewGuid();

        await Writes.UpsertReviewAsync(variantKey, memberKey, 2m, "Too dry");
        await Writes.UpsertReviewAsync(variantKey, memberKey, 4m, "Better with butter");
        var reviews = await Reads.ReviewsAsync(variantKey, 0, 10);

        _ = await Assert.That(reviews.Total).IsEqualTo(1);
        _ = await Assert.That(reviews.Items.Single().Rating).IsEqualTo(4m);
        _ = await Assert.That(reviews.Items.Single().Text).IsEqualTo("Better with butter");
    }

    [Test]
    public async Task UpsertReview_WithAnOffStepRating_IsRefused()
    {
        _ = await Assert.That(async () => await Writes.UpsertReviewAsync(Guid.NewGuid(), Guid.NewGuid(), 3.7m, null))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Reviews_ReadBackAsUtc()
    {
        var variantKey = Guid.NewGuid();
        await Writes.UpsertReviewAsync(variantKey, Guid.NewGuid(), 5m, "Crisp");

        var review = (await Reads.ReviewsAsync(variantKey, 0, 1)).Items.Single();

        _ = await Assert.That(review.Created.Kind).IsEqualTo(DateTimeKind.Utc);
        _ = await Assert.That(review.Modified.Kind).IsEqualTo(DateTimeKind.Utc);
    }

    [Test]
    public async Task Reviews_ArePagedNewestFirst()
    {
        var variantKey = Guid.NewGuid();
        foreach (var text in new[] { "first", "second", "third" })
        {
            await Writes.UpsertReviewAsync(variantKey, Guid.NewGuid(), 5m, text);
        }

        var page = await Reads.ReviewsAsync(variantKey, 0, 2);

        _ = await Assert.That(page.Total).IsEqualTo(3);
        _ = await Assert.That(string.Join(",", page.Items.Select(review => review.Text))).IsEqualTo("third,second");
    }

    [Test]
    public async Task MemberReview_FindsOnlyThatMembersReview()
    {
        var variantKey = Guid.NewGuid();
        var memberKey = Guid.NewGuid();
        await Writes.UpsertReviewAsync(variantKey, Guid.NewGuid(), 3m, "Someone else");
        await Writes.UpsertReviewAsync(variantKey, memberKey, 5m, "Mine");

        var mine = await Reads.MemberReviewAsync(variantKey, memberKey);

        _ = await Assert.That(mine!.Text).IsEqualTo("Mine");
        _ = await Assert.That(await Reads.MemberReviewAsync(variantKey, Guid.NewGuid())).IsNull();
    }

    [Test]
    public async Task Notes_ArePagedNewestFirst()
    {
        var variantKey = Guid.NewGuid();
        var start = DateTime.UtcNow;
        await WriteAsync(db => db.CookNotes.AddRange(
            new CookNote { VariantKey = variantKey, MemberKey = Guid.NewGuid(), Text = "first", Created = start, Modified = start },
            new CookNote { VariantKey = variantKey, MemberKey = Guid.NewGuid(), Text = "second", Created = start.AddSeconds(1), Modified = start },
            new CookNote { VariantKey = variantKey, MemberKey = Guid.NewGuid(), Text = "third", Created = start.AddSeconds(2), Modified = start }));

        var page = await Reads.NotesAsync(variantKey, 0, 2);

        _ = await Assert.That(page.Total).IsEqualTo(3);
        _ = await Assert.That(string.Join(",", page.Items.Select(note => note.Text))).IsEqualTo("third,second");
    }

    [Test]
    public async Task HasCooked_IsTrueOnlyForTheMemberWhoMarkedIt()
    {
        var variantKey = Guid.NewGuid();
        var memberKey = Guid.NewGuid();
        await WriteAsync(db => db.CookedMarks.Add(new CookedMark { VariantKey = variantKey, MemberKey = memberKey, Created = DateTime.UtcNow }));

        _ = await Assert.That(await Reads.HasCookedAsync(variantKey, memberKey)).IsTrue();
        _ = await Assert.That(await Reads.HasCookedAsync(variantKey, Guid.NewGuid())).IsFalse();
    }

    private async Task WriteAsync(Action<ContributionsDbContext> change)
    {
        using var scope = Site.Services.GetRequiredService<IEFCoreScopeProvider<ContributionsDbContext>>().CreateScope();
        scope.WriteLock(ContributionLocks.Contributions);
        await scope.ExecuteWithContextAsync<Task>(async db =>
        {
            change(db);
            await db.SaveChangesAsync();
        });
        scope.Complete();
    }
}
```

Every test uses fresh variant keys, so the suite never depends on the order it runs in. The notes and cooked marks
are inserted directly, because their writes arrive in Phase 4.

```bash
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/ContributionReadsTests/*"
```

Expected: build errors, because `IContributionWrites`, `IContributionReads` and `IContributionStats` do not exist.

- [ ] **Step 4: Write the stats source, the reads and the upsert**

Create `src/KCC.Contributions/ContributionStatsSource.cs`:

```csharp
using KCC.Contributions.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace KCC.Contributions;

public interface IContributionStats
{
    Task<ContributionStats> GetAsync();

    void Invalidate();
}

public sealed class ContributionStatsSource(IEFCoreScopeProvider<ContributionsDbContext> scopes, IMemoryCache cache) : IContributionStats
{
    private const string CacheKey = "kcc:contribution-stats";

    private CancellationTokenSource generation = new();

    public async Task<ContributionStats> GetAsync()
    {
        // Taken before the load starts: a write that lands during the load cancels this token, so the
        // snapshot the load produces is never cached as current.
        var token = generation.Token;
        return await cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AddExpirationToken(new CancellationChangeToken(token));
            return await LoadAsync();
        });
    }

    public void Invalidate() => Interlocked.Exchange(ref generation, new CancellationTokenSource()).Cancel();

    private async Task<ContributionStats> LoadAsync()
    {
        using var scope = scopes.CreateScope();
        var stats = await scope.ExecuteWithContextAsync(async db =>
        {
            var reviews = await db.Reviews.AsNoTracking().Select(review => new { review.VariantKey, review.Rating }).ToListAsync();
            var cooked = await db.CookedMarks.AsNoTracking().Select(mark => mark.VariantKey).ToListAsync();
            return ContributionStats.Build(reviews.Select(review => (review.VariantKey, review.Rating)), cooked);
        });
        scope.Complete();
        return stats;
    }
}
```

Create `src/KCC.Contributions/ContributionReads.cs`:

```csharp
using KCC.Contributions.Data;
using Microsoft.EntityFrameworkCore;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace KCC.Contributions;

public sealed record Paged<T>(IReadOnlyList<T> Items, int Total);

public interface IContributionReads
{
    Task<Paged<Review>> ReviewsAsync(Guid variantKey, int page, int pageSize);

    Task<Review> MemberReviewAsync(Guid variantKey, Guid memberKey);

    Task<Paged<CookNote>> NotesAsync(Guid variantKey, int page, int pageSize);

    Task<bool> HasCookedAsync(Guid variantKey, Guid memberKey);
}

public sealed class ContributionReads(IEFCoreScopeProvider<ContributionsDbContext> scopes) : IContributionReads
{
    public const int MaxPageSize = 50;

    public Task<Paged<Review>> ReviewsAsync(Guid variantKey, int page, int pageSize) =>
        ReadAsync(db => PageAsync(
            db.Reviews.AsNoTracking()
                .Where(review => review.VariantKey == variantKey)
                .OrderByDescending(review => review.Created)
                .ThenByDescending(review => review.Id),
            page,
            pageSize));

    public Task<Review> MemberReviewAsync(Guid variantKey, Guid memberKey) =>
        ReadAsync(db => db.Reviews.AsNoTracking()
            .FirstOrDefaultAsync(review => review.VariantKey == variantKey && review.MemberKey == memberKey));

    public Task<Paged<CookNote>> NotesAsync(Guid variantKey, int page, int pageSize) =>
        ReadAsync(db => PageAsync(
            db.CookNotes.AsNoTracking()
                .Where(note => note.VariantKey == variantKey)
                .OrderByDescending(note => note.Created)
                .ThenByDescending(note => note.Id),
            page,
            pageSize));

    public Task<bool> HasCookedAsync(Guid variantKey, Guid memberKey) =>
        ReadAsync(db => db.CookedMarks.AnyAsync(mark => mark.VariantKey == variantKey && mark.MemberKey == memberKey));

    private static async Task<Paged<T>> PageAsync<T>(IQueryable<T> query, int page, int pageSize)
    {
        var size = Math.Clamp(pageSize, 1, MaxPageSize);
        var total = await query.CountAsync();
        var items = await query.Skip(Math.Max(0, page) * size).Take(size).ToListAsync();
        return new Paged<T>(items, total);
    }

    private async Task<T> ReadAsync<T>(Func<ContributionsDbContext, Task<T>> read)
    {
        using var scope = scopes.CreateScope();
        var result = await scope.ExecuteWithContextAsync(read);
        scope.Complete();
        return result;
    }
}
```

Create `src/KCC.Contributions/ContributionWrites.cs`:

```csharp
using KCC.Contributions.Data;
using Microsoft.EntityFrameworkCore;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace KCC.Contributions;

public interface IContributionWrites
{
    Task UpsertReviewAsync(Guid variantKey, Guid memberKey, decimal rating, string text);
}

public sealed class ContributionWrites(IEFCoreScopeProvider<ContributionsDbContext> scopes, IContributionStats stats) : IContributionWrites
{
    public async Task UpsertReviewAsync(Guid variantKey, Guid memberKey, decimal rating, string text)
    {
        if (!RatingMath.IsValidRating(rating))
        {
            throw new ArgumentOutOfRangeException(nameof(rating), rating, "A rating runs from 0.5 to 5 in half-star steps.");
        }

        using (var scope = scopes.CreateScope())
        {
            // Taken before the read below so this transaction is already the writer when it looks for the
            // member's review: SQLite cannot upgrade a reader whose snapshot another writer has moved past.
            scope.WriteLock(ContributionLocks.Contributions);
            await scope.ExecuteWithContextAsync<Task>(async db =>
            {
                var now = DateTime.UtcNow;
                var review = await db.Reviews.FirstOrDefaultAsync(r => r.VariantKey == variantKey && r.MemberKey == memberKey);
                if (review is null)
                {
                    review = new Review { VariantKey = variantKey, MemberKey = memberKey, Created = now };
                    db.Reviews.Add(review);
                }

                review.Rating = rating;
                review.Text = RatingMath.ClampText(text);
                review.Modified = now;
                await db.SaveChangesAsync();
            });
            scope.Complete();
        }

        stats.Invalidate();
    }
}
```

The invalidation runs only after the scope has disposed, which is when the transaction commits. A reader that
reloads straight away therefore sees the write.

In `src/KCC.Contributions/ContributionsComposer.cs`, add these lines after the notification handler:

```csharp
        builder.Services.AddSingleton<IContributionStats, ContributionStatsSource>();
        builder.Services.AddSingleton<IContributionReads, ContributionReads>();
        builder.Services.AddSingleton<IContributionWrites, ContributionWrites>();
```

Umbraco registers `IEFCoreScopeProvider<T>` as a singleton, and `IMemoryCache` is a singleton too, so these services
capture nothing scoped. That matters in Development, where ASP.NET validates scopes.

- [ ] **Step 5: Run the tests**

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj
```

Expected: 0 warnings. Both suites pass, including the 8 `ContributionReadsTests`.

- [ ] **Step 6: Commit**

```bash
git add -A src/KCC.Contributions tests/KCC.UnitTests tests/KCC.IntegrationTests
git commit -m "Add Contribution Stats, Reads and the Review Upsert"
```

---

### Task 4: Author names

Spec §8: author names come from the member's first and last name, falling back to the username, and they are cached.
A member save or delete drops that member's cached name, so a rename shows on the next page view. The Kentico
`VariantGuidProvider` goes too. It looked up the recipe GUID that the old tables stored, and §9.1 drops that column.
The old write APIs that used it stay excluded until Phase 4 rewrites them without it.

**Files:**
- Rewrite:
  - `src/KCC.Web/Features/Providers/AuthorNameProvider.cs`
  - `tests/KCC.UnitTests/Features/Providers/AuthorNameProviderTests.cs`
  - `tests/KCC.IntegrationTests/Features/Providers/AuthorNameProviderTests.cs`
- Create: `src/KCC.Web/Features/Providers/{AuthorNameCacheRefresher,ProvidersComposer}.cs`
- Delete: `src/KCC.Web/Features/Providers/VariantGuidProvider.cs`
- Modify:
  - `src/KCC.Web/KCC.Web.csproj`: replace `Features/Providers/**` with `Features/Providers/RecipeIconProvider.cs`.
  - `tests/KCC.UnitTests/KCC.UnitTests.csproj`: delete the `Features/Providers/**` line.
  - `tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj`: delete the `Features/Providers/**` line. The group is
    then empty, so delete the whole `Unported slices` `ItemGroup`.

**Interfaces:**
- Produces `IAuthorNameProvider` (namespace `KCC.Web.Features.Providers`), registered as a singleton:
  - `Task<string> Resolve(Guid memberKey)` returns null when the member is unknown or deleted.
  - `Task<IReadOnlyDictionary<Guid, string>> ResolveMany(IEnumerable<Guid> memberKeys)` leaves unknown and deleted
    members out of the dictionary.
  - `void Forget(IEnumerable<Guid> memberKeys)`.
- Also produces three static members on `AuthorNameProvider`:
  - `DeletedMemberName` = `"(deleted)"`
  - `FormatDisplayName(string firstName, string lastName, string userName) → string`
  - `NameFor(IReadOnlyDictionary<Guid, string> names, Guid? memberKey) → string`

- [ ] **Step 1: Bring the provider tests back and rewrite them**

```bash
git rm -q src/KCC.Web/Features/Providers/VariantGuidProvider.cs
```

Make the three project-file changes listed above.

Replace `tests/KCC.UnitTests/Features/Providers/AuthorNameProviderTests.cs` with the file below. The two
`FormatDisplayName` tests are the existing ones, unchanged.

```csharp
using KCC.Web.Features.Providers;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace KCC.UnitTests.Features.Providers;

public class AuthorNameProviderTests
{
    [Test]
    [Arguments("Tucker", "Wright", "twright", "Tucker Wright")]
    [Arguments("Tucker", "", "twright", "Tucker")]
    [Arguments("", "Wright", "twright", "Wright")]
    [Arguments("  Tucker  ", "  Wright  ", "twright", "Tucker Wright")]
    [Arguments("", "", "twright", "twright")]
    [Arguments("  ", "  ", "twright", "twright")]
    [Arguments(null, null, "twright", "twright")]
    public async Task FormatDisplayName_PrefersFullNameThenUsername(string first, string last, string userName, string expected)
    {
        _ = await Assert.That(AuthorNameProvider.FormatDisplayName(first, last, userName)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("", "", "")]
    [Arguments("", "", "   ")]
    [Arguments(null, null, null)]
    public async Task FormatDisplayName_ReturnsNullWhenNothingUsable(string first, string last, string userName)
    {
        _ = await Assert.That(AuthorNameProvider.FormatDisplayName(first, last, userName)).IsNull();
    }

    [Test]
    public async Task ResolveMany_EmptyKeys_NeverAsksTheMemberService()
    {
        var members = new Mock<IMemberService>(MockBehavior.Strict);

        var names = await Provider(members).ResolveMany([Guid.Empty]);

        _ = await Assert.That(names.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ResolveMany_NamesAMemberFromFirstAndLastName()
    {
        var key = Guid.NewGuid();
        var members = MembersReturning(Member(key, "Priya", "Balan", "priya.balan"));

        var names = await Provider(members).ResolveMany([key]);

        _ = await Assert.That(names[key]).IsEqualTo("Priya Balan");
    }

    [Test]
    public async Task ResolveMany_SecondCall_IsServedFromTheCache()
    {
        var key = Guid.NewGuid();
        var members = MembersReturning(Member(key, "Priya", "Balan", "priya.balan"));
        var provider = Provider(members);

        _ = await provider.ResolveMany([key]);
        _ = await provider.ResolveMany([key]);

        members.Verify(m => m.GetByKeysAsync(It.IsAny<Guid[]>()), Times.Once);
    }

    [Test]
    public async Task ResolveMany_UnknownMember_HasNoNameAndIsNotLookedUpAgain()
    {
        var key = Guid.NewGuid();
        var members = MembersReturning();
        var provider = Provider(members);

        var names = await provider.ResolveMany([key]);
        _ = await provider.ResolveMany([key]);

        _ = await Assert.That(names.ContainsKey(key)).IsFalse();
        members.Verify(m => m.GetByKeysAsync(It.IsAny<Guid[]>()), Times.Once);
    }

    [Test]
    public async Task Forget_MakesTheNextCallLookTheMemberUpAgain()
    {
        var key = Guid.NewGuid();
        var members = MembersReturning(Member(key, "Priya", "Balan", "priya.balan"));
        var provider = Provider(members);

        _ = await provider.ResolveMany([key]);
        provider.Forget([key]);
        _ = await provider.ResolveMany([key]);

        members.Verify(m => m.GetByKeysAsync(It.IsAny<Guid[]>()), Times.Exactly(2));
    }

    private static AuthorNameProvider Provider(Mock<IMemberService> members) =>
        new(members.Object, new MemoryCache(new MemoryCacheOptions()));

    private static Mock<IMemberService> MembersReturning(params IMember[] found)
    {
        var members = new Mock<IMemberService>();
        members.Setup(m => m.GetByKeysAsync(It.IsAny<Guid[]>()))
            .ReturnsAsync((Guid[] keys) => found.Where(member => keys.Contains(member.Key)));
        return members;
    }

    private static IMember Member(Guid key, string firstName, string lastName, string userName)
    {
        var member = new Mock<IMember>();
        member.SetupGet(m => m.Key).Returns(key);
        member.SetupGet(m => m.Username).Returns(userName);
        member.Setup(m => m.GetValue<string>("firstName", null, null, false)).Returns(firstName);
        member.Setup(m => m.GetValue<string>("lastName", null, null, false)).Returns(lastName);
        return member.Object;
    }
}
```

Moq cannot see optional parameters, so the `GetValue<string>` setups spell out the defaults: culture, segment and
`published`.

Replace `tests/KCC.IntegrationTests/Features/Providers/AuthorNameProviderTests.cs` with:

```csharp
using KCC.IntegrationTests.Config;
using KCC.Web.Features.Providers;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;

namespace KCC.IntegrationTests.Features.Providers;

public class AuthorNameProviderTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    private IAuthorNameProvider Authors => Site.Services.GetRequiredService<IAuthorNameProvider>();

    [Test]
    public async Task RenamingAMember_ReplacesTheCachedName()
    {
        var members = Site.Services.GetRequiredService<IMemberService>();
        var member = members.CreateMemberWithIdentity(
            $"it-{Guid.NewGuid():N}",
            $"{Guid.NewGuid():N}@example.test",
            "Integration Author",
            Constants.Security.DefaultMemberTypeAlias,
            isApproved: true);
        member.SetValue("firstName", "Ada");
        member.SetValue("lastName", "Lovelace");
        _ = members.Save(member);

        var before = await Authors.Resolve(member.Key);
        member.SetValue("firstName", "Grace");
        member.SetValue("lastName", "Hopper");
        _ = members.Save(member);
        var after = await Authors.Resolve(member.Key);

        _ = await Assert.That(before).IsEqualTo("Ada Lovelace");
        _ = await Assert.That(after).IsEqualTo("Grace Hopper");
    }

    [Test]
    public async Task UnknownMember_HasNoName()
    {
        _ = await Assert.That(await Authors.Resolve(Guid.NewGuid())).IsNull();
    }
}
```

```bash
dotnet build KitchenCommandCenter.sln
```

Expected: build errors from the Kentico `AuthorNameProvider.cs`, which is now compiled again, and from the tests,
because `IAuthorNameProvider` does not exist.

- [ ] **Step 2: Write the provider, its cache refresher and the composer**

Replace `src/KCC.Web/Features/Providers/AuthorNameProvider.cs` with:

```csharp
using Microsoft.Extensions.Caching.Memory;
using Umbraco.Cms.Core.Services;

namespace KCC.Web.Features.Providers;

public interface IAuthorNameProvider
{
    Task<string> Resolve(Guid memberKey);

    Task<IReadOnlyDictionary<Guid, string>> ResolveMany(IEnumerable<Guid> memberKeys);

    void Forget(IEnumerable<Guid> memberKeys);
}

public class AuthorNameProvider(IMemberService memberService, IMemoryCache cache) : IAuthorNameProvider
{
    public const string DeletedMemberName = "(deleted)";

    private static readonly TimeSpan CacheLifetime = TimeSpan.FromHours(1);

    public static string FormatDisplayName(string firstName, string lastName, string userName)
    {
        var fullName = $"{firstName?.Trim()} {lastName?.Trim()}".Trim();
        if (fullName.Length > 0)
        {
            return fullName;
        }

        var fallback = userName?.Trim();
        return string.IsNullOrEmpty(fallback) ? null : fallback;
    }

    public static string NameFor(IReadOnlyDictionary<Guid, string> names, Guid? memberKey) =>
        memberKey is { } key ? names.GetValueOrDefault(key) : null;

    public async Task<string> Resolve(Guid memberKey) => NameFor(await ResolveMany([memberKey]), memberKey);

    public async Task<IReadOnlyDictionary<Guid, string>> ResolveMany(IEnumerable<Guid> memberKeys)
    {
        var names = new Dictionary<Guid, string>();
        var uncached = new List<Guid>();
        foreach (var key in memberKeys.Where(key => key != Guid.Empty).Distinct())
        {
            if (!cache.TryGetValue(CacheKey(key), out string cached))
            {
                uncached.Add(key);
            }
            else if (cached.Length > 0)
            {
                names[key] = cached;
            }
        }

        if (uncached.Count == 0)
        {
            return names;
        }

        var members = (await memberService.GetByKeysAsync([.. uncached])).ToDictionary(member => member.Key);
        foreach (var key in uncached)
        {
            var name = members.TryGetValue(key, out var member)
                ? FormatDisplayName(member.GetValue<string>("firstName"), member.GetValue<string>("lastName"), member.Username)
                : null;

            // A missing name is cached as empty too, so reviews by deleted members cost one lookup, not one a page view.
            cache.Set(CacheKey(key), name ?? string.Empty, CacheLifetime);
            if (name is not null)
            {
                names[key] = name;
            }
        }

        return names;
    }

    public void Forget(IEnumerable<Guid> memberKeys)
    {
        foreach (var key in memberKeys)
        {
            cache.Remove(CacheKey(key));
        }
    }

    private static string CacheKey(Guid memberKey) => $"kcc:author-name:{memberKey}";
}
```

Create `src/KCC.Web/Features/Providers/AuthorNameCacheRefresher.cs`:

```csharp
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace KCC.Web.Features.Providers;

public class AuthorNameCacheRefresher(IAuthorNameProvider authorNames)
    : INotificationHandler<MemberSavedNotification>, INotificationHandler<MemberDeletedNotification>
{
    public void Handle(MemberSavedNotification notification) =>
        authorNames.Forget(notification.SavedEntities.Select(member => member.Key));

    public void Handle(MemberDeletedNotification notification) =>
        authorNames.Forget(notification.DeletedEntities.Select(member => member.Key));
}
```

Create `src/KCC.Web/Features/Providers/ProvidersComposer.cs`:

```csharp
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace KCC.Web.Features.Providers;

public class ProvidersComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IAuthorNameProvider, AuthorNameProvider>();
        builder
            .AddNotificationHandler<MemberSavedNotification, AuthorNameCacheRefresher>()
            .AddNotificationHandler<MemberDeletedNotification, AuthorNameCacheRefresher>();
    }
}
```

`IMemberService.GetByKey` is obsolete in Umbraco 17, so the provider batches lookups through
`GetByKeysAsync(params Guid[])`.

- [ ] **Step 3: Run the tests**

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj -- --treenode-filter "/*/*/AuthorNameProviderTests/*"
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/AuthorNameProviderTests/*"
```

Expected: 0 warnings. The unit tests pass: 7 methods (15 cases). The 2 integration tests pass, because the member
save's notification dropped the cached name before the second `Resolve`.

- [ ] **Step 4: Commit**

```bash
git add -A src/KCC.Web/Features/Providers src/KCC.Web/KCC.Web.csproj tests/KCC.UnitTests tests/KCC.IntegrationTests
git commit -m "Resolve Author Names from Umbraco Members"
```

---

### Task 5: The seeder, run by both fixtures

Spec §12 keeps today's test data and rebuilds the seeder on Umbraco services: 25 recipes and 29 variants, with their
tags, two authors, the reviews, and backdated dates for the "newest" sorts. The coverage-matrix data files are kept
as they are, apart from stale comments. The seeder is idempotent by deterministic keys. Each fixture runs it once,
before the first test. It must run first, because its content and member saves may not overlap a test's writes (see
`AssemblyInfo.cs`).

**Files:**
- Rewrite: `src/KCC.Web/Features/DevTools/RecipeSeed/{RecipeTestDataSeeder,DevSeedApiController}.cs`
- Create:
  - `src/KCC.Web/Features/DevTools/RecipeSeed/SeedKeys.cs`
  - `tests/KCC.UnitTests/Features/DevTools/SeedKeysTests.cs`
  - `tests/KCC.IntegrationTests/Features/DevTools/RecipeSeederTests.cs`
- Modify:
  - `src/KCC.Web/Features/DevTools/RecipeSeed/{RecipeSeedModels,RecipeSeedData}.cs` (stale comments only)
  - `src/KCC.Web/Program.cs`
  - `src/KCC.Web/KCC.Web.csproj` (delete the `Features/DevTools/RecipeSeed/**` line)
  - `tests/KCC.IntegrationTests/Config/UmbracoSite.cs`
  - `tests/KCC.E2ETests/Config/SiteProcess.cs`

**Interfaces:**
- Consumes:
  - `IContributionWrites.UpsertReviewAsync` (Task 3).
  - The `recipe` and `recipeVariant` types (Task 2).
  - Phase 1's baseline: the Home → Recipes listing, and the nodes in the "Recipe Categories" and "Recipe Tags"
    folders (types `recipeCategory` and `recipeTag`).
- Produces:
  - `POST /api/dev/seed-recipes`, answering only in Development and Testing (404 elsewhere), with `{ summary, log }`.
  - `SeedKeys.Recipe(string recipeName)`, `SeedKeys.Variant(string recipeName, string variantName)`,
    `SeedKeys.Author(string userName)` and `SeedKeys.Reviewer(string recipeName, int index)`, each returning a `Guid`.
  - `RecipeTestDataSeeder.RunAsync(TextWriter log, CancellationToken cancellationToken) → Task<SeedSummary>`.
  - `UmbracoSite` and `SiteProcess()` seed before the first test. `SiteProcess(bool withSsr, bool seed = false)`
    does not seed by default.
- Seeded facts that later tasks assert:

  | Recipe | Details | Variants | Reviews |
  |---|---|---|---|
  | Fluffy Buttermilk Pancakes | Breakfast, no author, created 3 days before today (midnight UTC) | Classic Stack: prep 10, cook 15, serves 4, Vegetarian; Flour 2 cups, Buttermilk 1.5 cups, Eggs 1 whole; 2 steps | 4.5 and 4 on Classic Stack: 4.25 over 2 |
  | Spicy Ramen Flight | Lunch, by Priya Balan, 6 days ago | Four, in this order: Chili Oil Shoyu (prep 10, cook 10, serves 1, Spicy; Ramen Noodles, Chili Oil; 2 steps), Miso Vegan, Tonkotsu-Style Protein, Coconut Dairy-Free | 4.5, 5 and 4 on Chili Oil Shoyu: 4.5 over 3 |
  | Avocado Toast Supreme | 5 days ago | | |
  | Bare Cupboard Snack Board | | none | |

  More rules the later tasks rely on:
  - A variant's create date is its recipe's plus one minute per position.
  - Variants inherit their recipe's author.
  - No variant has nutrition, a difficulty or images.
  - Reviewers are keys with no member behind them, so reviews show as "(deleted)", as in the reference screenshots.

- [ ] **Step 1: Write the failing key tests**

Create `tests/KCC.UnitTests/Features/DevTools/SeedKeysTests.cs`:

```csharp
using KCC.Web.Features.DevTools.RecipeSeed;

namespace KCC.UnitTests.Features.DevTools;

public class SeedKeysTests
{
    [Test]
    public async Task Recipe_IsTheSameOnEveryRun()
    {
        _ = await Assert.That(SeedKeys.Recipe("Shakshuka")).IsEqualTo(SeedKeys.Recipe("Shakshuka"));
    }

    [Test]
    public async Task Keys_DifferBetweenKinds()
    {
        var keys = new[]
        {
            SeedKeys.Recipe("Shakshuka"),
            SeedKeys.Variant("Shakshuka", "Shakshuka"),
            SeedKeys.Author("Shakshuka"),
            SeedKeys.Reviewer("Shakshuka", 0),
        };

        _ = await Assert.That(keys.Distinct().Count()).IsEqualTo(keys.Length);
    }

    [Test]
    public async Task Variant_DependsOnItsRecipe()
    {
        _ = await Assert.That(SeedKeys.Variant("Tacos", "Classic")).IsNotEqualTo(SeedKeys.Variant("Nachos", "Classic"));
    }
}
```

In `src/KCC.Web/KCC.Web.csproj`, delete `<Compile Remove="Features/DevTools/RecipeSeed/**" />`, then:

```bash
dotnet build KitchenCommandCenter.sln
```

Expected: build errors. `SeedKeys` is missing, and the Kentico `RecipeTestDataSeeder.cs` and
`DevSeedApiController.cs` no longer compile.

- [ ] **Step 2: Write the keys, the seeder and its endpoint**

Create `src/KCC.Web/Features/DevTools/RecipeSeed/SeedKeys.cs`:

```csharp
using System.Security.Cryptography;
using System.Text;

namespace KCC.Web.Features.DevTools.RecipeSeed;

// The same seed always yields the same key, so a second run finds what the first one created.
public static class SeedKeys
{
    public static Guid Recipe(string recipeName) => For($"recipe::{recipeName}");

    public static Guid Variant(string recipeName, string variantName) => For($"variant::{recipeName}::{variantName}");

    public static Guid Author(string userName) => For($"author::{userName}");

    public static Guid Reviewer(string recipeName, int index) => For($"review::{recipeName}::{index}");

    private static Guid For(string seed) => new(MD5.HashData(Encoding.UTF8.GetBytes(seed)));
}
```

Replace `src/KCC.Web/Features/DevTools/RecipeSeed/RecipeTestDataSeeder.cs` with:

```csharp
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using KCC.Contributions;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Models.ContentPublishing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Services.OperationStatus;

namespace KCC.Web.Features.DevTools.RecipeSeed;

public class RecipeTestDataSeeder(
    IContentService contentService,
    IContentTypeService contentTypeService,
    IContentEditingService contentEditingService,
    IContentPublishingService contentPublishingService,
    IDocumentNavigationQueryService navigation,
    IMemberService memberService,
    IMemberTypeService memberTypeService,
    IMemberEditingService memberEditingService,
    IUserService userService,
    IContributionWrites contributionWrites)
{
    private static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<SeedSummary> RunAsync(TextWriter log, CancellationToken cancellationToken)
    {
        var summary = new SeedSummary();
        var recipeTypeKey = RequireContentType("recipe");
        var variantTypeKey = RequireContentType("recipeVariant");
        var listingKey = FindRecipeListing();
        var categories = KeysByName("recipeCategory");
        var tags = KeysByName("recipeTag");
        var authors = await EnsureAuthorsAsync(summary, log);
        var today = DateTime.UtcNow.Date;

        foreach (var recipe in RecipeSeedData.Recipes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var recipeKey = SeedKeys.Recipe(recipe.Name);
            if (contentService.GetById(recipeKey) is not null)
            {
                summary.RecipesSkipped++;
                log.WriteLine($"  skip (exists): {recipe.Name}");
                continue;
            }

            Guid? authorKey = recipe.AuthorKey is { } author ? authors[author] : null;
            var published = DateTime.SpecifyKind(today.AddDays(-recipe.PublishedDaysAgo), DateTimeKind.Utc);
            await CreatePublishedAsync(recipeKey, recipe.Name, recipeTypeKey, listingKey, published, RecipeValues(recipe, categories, authorKey));
            summary.RecipesCreated++;

            for (var index = 0; index < recipe.Variants.Length; index++)
            {
                var variant = recipe.Variants[index];
                await CreatePublishedAsync(
                    SeedKeys.Variant(recipe.Name, variant.Name),
                    variant.Name,
                    variantTypeKey,
                    recipeKey,
                    published.AddMinutes(index + 1),
                    VariantValues(variant, tags, authorKey));
                summary.VariantsCreated++;
            }

            // Reviews attach to the first variant, as they always have in this data set.
            if (recipe.Variants.Length > 0)
            {
                var firstVariantKey = SeedKeys.Variant(recipe.Name, recipe.Variants[0].Name);
                for (var index = 0; index < recipe.Reviews.Length; index++)
                {
                    await contributionWrites.UpsertReviewAsync(
                        firstVariantKey,
                        SeedKeys.Reviewer(recipe.Name, index),
                        recipe.Reviews[index].Rating,
                        $"Seeded review #{index + 1}");
                    summary.ReviewsWritten++;
                }
            }

            log.WriteLine($"  created: {recipe.Name}");
        }

        log.WriteLine(summary.ToString());
        return summary;
    }

    private static List<PropertyValueModel> RecipeValues(SeedRecipe recipe, IReadOnlyDictionary<string, Guid> categories, Guid? authorKey)
    {
        var values = new List<PropertyValueModel>
        {
            new() { Alias = "description", Value = recipe.Description },
            new() { Alias = "icon", Value = recipe.Icon },
        };

        if (categories.TryGetValue(recipe.Category, out var categoryKey))
        {
            values.Add(new() { Alias = "category", Value = DocumentReferences(categoryKey) });
        }

        AddAuthor(values, authorKey);
        return values;
    }

    private static List<PropertyValueModel> VariantValues(SeedVariant variant, IReadOnlyDictionary<string, Guid> tags, Guid? authorKey)
    {
        var values = new List<PropertyValueModel>
        {
            new() { Alias = "description", Value = variant.Description },
            new() { Alias = "icon", Value = variant.Icon },
            new() { Alias = "prepTime", Value = variant.PrepMinutes },
            new() { Alias = "cookTime", Value = variant.CookMinutes },
            new() { Alias = "servings", Value = variant.Servings },
            new()
            {
                Alias = "ingredients",
                Value = JsonSerializer.Serialize(
                    variant.Ingredients.Select(ingredient => new { name = ingredient.Name, quantity = ingredient.Quantity, unit = ingredient.Unit, isEyeballed = ingredient.IsEyeballed }),
                    CamelCase),
            },
            new()
            {
                Alias = "instructions",
                Value = JsonSerializer.Serialize(variant.Instructions.Select(step => new { step = step.Step, text = step.Text }), CamelCase),
            },
        };

        var tagKeys = variant.Diets.Where(tags.ContainsKey).Select(diet => tags[diet]).ToArray();
        if (tagKeys.Length > 0)
        {
            values.Add(new() { Alias = "tags", Value = DocumentReferences(tagKeys) });
        }

        AddAuthor(values, authorKey);
        return values;
    }

    // The member picker's editor takes the member key as a string, and stores it as a member UDI.
    private static void AddAuthor(List<PropertyValueModel> values, Guid? authorKey)
    {
        if (authorKey is { } key)
        {
            values.Add(new() { Alias = "author", Value = key.ToString() });
        }
    }

    // The multi-node tree picker's editor accepts only a JsonArray of { type, unique } references.
    private static JsonArray DocumentReferences(params Guid[] keys) =>
        new(keys.Select(key => (JsonNode)new JsonObject { ["type"] = "document", ["unique"] = key.ToString() }).ToArray());

    private Guid RequireContentType(string alias) =>
        contentTypeService.Get(alias)?.Key ?? throw new InvalidOperationException($"Document type {alias} is missing; uSync imports it at startup.");

    private Guid FindRecipeListing()
    {
        if (navigation.TryGetRootKeysOfType("homePage", out var homes))
        {
            foreach (var home in homes)
            {
                if (navigation.TryGetChildrenKeysOfType(home, "recipeListingPage", out var listings) && listings.Any())
                {
                    return listings.First();
                }
            }
        }

        throw new InvalidOperationException("The baseline has no recipe listing page under Home.");
    }

    private Dictionary<string, Guid> KeysByName(string contentTypeAlias)
    {
        var keys = new List<Guid>();
        if (navigation.TryGetRootKeys(out var roots))
        {
            foreach (var root in roots)
            {
                if (navigation.TryGetDescendantsKeysOfType(root, contentTypeAlias, out var found))
                {
                    keys.AddRange(found);
                }
            }
        }

        return contentService.GetByIds(keys).ToDictionary(node => node.Name, node => node.Key, StringComparer.Ordinal);
    }

    private async Task<Dictionary<string, Guid>> EnsureAuthorsAsync(SeedSummary summary, TextWriter log)
    {
        var memberTypeKey = memberTypeService.Get(Constants.Security.DefaultMemberTypeAlias)?.Key
            ?? throw new InvalidOperationException("The default member type is missing.");
        var superUser = await userService.GetAsync(Constants.Security.SuperUserKey)
            ?? throw new InvalidOperationException("The super user is missing.");
        var keys = new Dictionary<string, Guid>(StringComparer.Ordinal);

        foreach (var author in RecipeSeedData.Authors)
        {
            if (memberService.GetByUsername(author.UserName) is { } existing)
            {
                keys[author.Key] = existing.Key;
                continue;
            }

            var created = await memberEditingService.CreateAsync(
                new MemberCreateModel
                {
                    Key = SeedKeys.Author(author.UserName),
                    ContentTypeKey = memberTypeKey,
                    Username = author.UserName,
                    Email = author.Email,

                    // Seeded authors never sign in, so their password is random.
                    Password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)),
                    IsApproved = true,
                    Variants = [new VariantModel { Name = $"{author.FirstName} {author.LastName}" }],
                    Properties =
                    [
                        new PropertyValueModel { Alias = "firstName", Value = author.FirstName },
                        new PropertyValueModel { Alias = "lastName", Value = author.LastName },
                    ],
                },
                superUser);
            if (!created.Success)
            {
                throw new InvalidOperationException(
                    $"Creating author {author.UserName} failed: {created.Status.MemberEditingOperationStatus}, {created.Status.ContentEditingOperationStatus}.");
            }

            keys[author.Key] = created.Result.Content.Key;
            summary.AuthorsCreated++;
            log.WriteLine($"  author created: {author.FirstName} {author.LastName} ({author.UserName})");
        }

        return keys;
    }

    private async Task CreatePublishedAsync(Guid key, string name, Guid contentTypeKey, Guid parentKey, DateTime createDate, IEnumerable<PropertyValueModel> values)
    {
        var created = await contentEditingService.CreateAsync(
            new ContentCreateModel
            {
                Key = key,
                ContentTypeKey = contentTypeKey,
                ParentKey = parentKey,
                Variants = [new VariantModel { Name = name }],
                Properties = values,
            },
            Constants.Security.SuperUserKey);

        // A property that fails validation still saves and reports Success, so the status is what counts.
        if (created.Status != ContentEditingOperationStatus.Success)
        {
            throw new InvalidOperationException($"Creating {name} failed: {created.Status}.");
        }

        // The editing service stamps the time of creation; the "newest" sorts need the data set's spread.
        var content = contentService.GetById(key);
        content.CreateDate = createDate;
        if (!contentService.Save(content).Success)
        {
            throw new InvalidOperationException($"Backdating {name} failed.");
        }

        var published = await contentPublishingService.PublishAsync(
            key,
            [new CulturePublishScheduleModel { Culture = null }],
            Constants.Security.SuperUserKey);
        if (!published.Success)
        {
            throw new InvalidOperationException($"Publishing {name} failed: {published.Status}.");
        }
    }
}

public sealed class SeedSummary
{
    public int AuthorsCreated { get; set; }

    public int RecipesCreated { get; set; }

    public int RecipesSkipped { get; set; }

    public int VariantsCreated { get; set; }

    public int ReviewsWritten { get; set; }

    public override string ToString() =>
        $"Seed complete: recipes +{RecipesCreated} (skipped {RecipesSkipped}), variants +{VariantsCreated}, " +
        $"reviews +{ReviewsWritten}, authors +{AuthorsCreated}.";
}
```

**If Phase 1 took the §19 template fallback:** the seeder must pass the shared template's key on every create.
1. Inject `ITemplateService templateService`.
2. At the start of `RunAsync`, resolve `var templateKey = (await templateService.GetAsync("page"))!.Key;`.
3. Pass `templateKey` into `CreatePublishedAsync`, and set `TemplateKey = templateKey` on its `ContentCreateModel`.

The editing service sets no template when the key is null.

Replace `src/KCC.Web/Features/DevTools/RecipeSeed/DevSeedApiController.cs` with:

```csharp
using Microsoft.AspNetCore.Mvc;

namespace KCC.Web.Features.DevTools.RecipeSeed;

[ApiController]
[Route("api/dev")]
[ApiExplorerSettings(IgnoreApi = true)]
public class DevSeedApiController(IWebHostEnvironment environment) : ControllerBase
{
    [HttpPost("seed-recipes")]
    public async Task<IActionResult> SeedRecipes([FromServices] RecipeTestDataSeeder seeder, CancellationToken cancellationToken)
    {
        // The integration and E2E fixtures run the site in the Testing environment.
        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
        {
            return NotFound();
        }

        using var log = new StringWriter();
        var summary = await seeder.RunAsync(log, cancellationToken);
        return Ok(new { summary = summary.ToString(), log = log.ToString() });
    }
}
```

The old `reset` flag is gone. Starting over now means deleting the database file. It had also relied on Kentico's
Lucene worker, and nothing needs that any more.

In `src/KCC.Web/Program.cs`:
- Add `using KCC.Web.Features.DevTools.RecipeSeed;` in sorted position, before `KCC.Web.Features.Dictionary`.
- After the `IRobotsTxtProvider` registration, add `builder.Services.AddTransient<RecipeTestDataSeeder>();`.

- [ ] **Step 3: Correct the stale comments in the data files**

In `src/KCC.Web/Features/DevTools/RecipeSeed/RecipeSeedModels.cs`, replace the three-line header comment with:

```csharp
// Pure, framework-free description of the test-data set the seeder publishes as recipe and recipe-variant pages
// plus review rows. Kept separate from the seeding logic so the dataset reads as a coverage matrix (see
// RecipeSeedData) rather than CMS plumbing.
```

and replace the first two lines of `SeedRecipe`'s `<summary>` with:

```csharp
/// A recipe and everything the search index derives from it. <see cref="Reviews"/> are attached to the
/// first variant; the recipe's rating averages the reviews of all its published variants.
```

In `src/KCC.Web/Features/DevTools/RecipeSeed/RecipeSeedData.cs`, make three replacements:
- In the class `<summary>`, replace `(only a recipe's first category is indexed)` with `(a recipe holds exactly one)`.
- Replace the two-line comment above `Breakfast` with:

  ```csharp
      // The baseline's "Recipe Categories" nodes. A recipe holds exactly one, so covering N categories takes N
      // recipes.
  ```

- Replace the comment above `Vegetarian` with:

  ```csharp
      // The baseline's "Recipe Tags" nodes — multi-valued across a recipe's variants.
  ```

The folder's `.editorconfig`, which relaxes SA1117 and SA1203 for the dense record literals, stays.

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj -- --treenode-filter "/*/*/SeedKeysTests/*"
```

Expected: 0 warnings, and 3 passed.

- [ ] **Step 4: Seed in the integration host, and test the seeder**

In `tests/KCC.IntegrationTests/Config/UmbracoSite.cs`, add `await SeedTestRecipesAsync();` as the last line of
`InitializeAsync`, after the Run-level check. Then add this method after `WebProjectDirectory()`:

```csharp
    // Seeding saves content and members, which must not overlap a test's writes (see AssemblyInfo.cs), so it
    // runs once, before any test.
    private async Task SeedTestRecipesAsync()
    {
        using var client = CreateClient();
        client.Timeout = TimeSpan.FromMinutes(5);
        using var response = await client.PostAsync("/api/dev/seed-recipes", null);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Seeding answered {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        }
    }
```

Create `tests/KCC.IntegrationTests/Features/DevTools/RecipeSeederTests.cs`:

```csharp
using System.Net;
using KCC.Contributions;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.DevTools.RecipeSeed;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;

namespace KCC.IntegrationTests.Features.DevTools;

public class RecipeSeederTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task Seeder_PublishesEveryRecipeAndVariantUnderTheListing()
    {
        var navigation = Site.Services.GetRequiredService<IDocumentNavigationQueryService>();
        var content = Site.Services.GetRequiredService<IContentService>();

        var recipeKeys = RecipeSeedData.Recipes.Select(recipe => SeedKeys.Recipe(recipe.Name)).ToList();
        var variantKeys = RecipeSeedData.Recipes
            .SelectMany(recipe => recipe.Variants.Select(variant => SeedKeys.Variant(recipe.Name, variant.Name)))
            .ToList();
        _ = navigation.TryGetChildrenKeysOfType(TestContent.RecipeListing(Site.Services), "recipe", out var listed);

        _ = await Assert.That(recipeKeys.All(listed.Contains)).IsTrue();
        _ = await Assert.That(content.GetByIds(recipeKeys).Count(recipe => recipe.Published)).IsEqualTo(25);
        _ = await Assert.That(content.GetByIds(variantKeys).Count(variant => variant.Published)).IsEqualTo(29);
    }

    [Test]
    public async Task Seeder_SpreadsTheCreateDatesForTheNewestSorts()
    {
        var content = Site.Services.GetRequiredService<IContentService>();
        var pancakes = content.GetById(SeedKeys.Recipe("Fluffy Buttermilk Pancakes"))!;
        var toast = content.GetById(SeedKeys.Recipe("Avocado Toast Supreme"))!;
        var stack = content.GetById(SeedKeys.Variant("Fluffy Buttermilk Pancakes", "Classic Stack"))!;

        _ = await Assert.That(pancakes.CreateDate - toast.CreateDate).IsEqualTo(TimeSpan.FromDays(2));
        _ = await Assert.That(stack.CreateDate - pancakes.CreateDate).IsEqualTo(TimeSpan.FromMinutes(1));
    }

    [Test]
    public async Task Seeder_CreatesApprovedAuthorsWithTheirNames()
    {
        var priya = Site.Services.GetRequiredService<IMemberService>().GetByUsername("priya.balan")!;

        _ = await Assert.That(priya.Key).IsEqualTo(SeedKeys.Author("priya.balan"));
        _ = await Assert.That(priya.IsApproved).IsTrue();
        _ = await Assert.That(priya.GetValue<string>("firstName")).IsEqualTo("Priya");
        _ = await Assert.That(priya.GetValue<string>("lastName")).IsEqualTo("Balan");
    }

    [Test]
    public async Task Seeder_ReviewsEachRecipesFirstVariant()
    {
        var stats = await Site.Services.GetRequiredService<IContributionStats>().GetAsync();

        _ = await Assert.That(stats.For(SeedKeys.Variant("Fluffy Buttermilk Pancakes", "Classic Stack")).Rating)
            .IsEqualTo(new RatingAggregate(4.25d, 2));
        _ = await Assert.That(stats.For(SeedKeys.Variant("Spicy Ramen Flight", "Miso Vegan")).Rating.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Seeder_SecondRun_SkipsEveryRecipe()
    {
        using var client = Site.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(5);

        using var response = await client.PostAsync("/api/dev/seed-recipes", null);
        var body = await response.Content.ReadAsStringAsync();

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        _ = await Assert.That(body.Contains("recipes +0 (skipped 25)", StringComparison.Ordinal)).IsTrue();
    }
}
```

`TestContent.RecipeListing` arrives in Task 6. To keep this task self-contained, create
`tests/KCC.IntegrationTests/Config/TestContent.cs` now with only that method; Task 6 adds the rest:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Services.Navigation;

namespace KCC.IntegrationTests.Config;

// Builds content for one test through the services the backoffice uses, so a test that changes content
// never touches the seeded data set other tests read.
public static class TestContent
{
    public static Guid RecipeListing(IServiceProvider services)
    {
        var navigation = services.GetRequiredService<IDocumentNavigationQueryService>();
        if (navigation.TryGetRootKeysOfType("homePage", out var homes))
        {
            foreach (var home in homes)
            {
                if (navigation.TryGetChildrenKeysOfType(home, "recipeListingPage", out var listings) && listings.Any())
                {
                    return listings.First();
                }
            }
        }

        throw new InvalidOperationException("The baseline has no recipe listing page.");
    }
}
```

- [ ] **Step 5: Run the integration suite**

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj
```

Expected: every test passes, including the 5 `RecipeSeederTests`. Phase 1's tests still pass with 25 recipes in the
tree: the sitemap test only checks for missing account and status-code URLs.

If the fixture throws `Seeding answered 500`, the response body carries the seeder's exception. The usual cause is a
value shape the editing service rejected; compare it with Global Constraints.

- [ ] **Step 6: Seed the E2E site too**

In `tests/KCC.E2ETests/Config/SiteProcess.cs`:

1. Add the field `private readonly bool seed;` after `private readonly bool withSsr;`.
2. Replace the two constructors with:

```csharp
    public SiteProcess()
        : this(withSsr: true, seed: true)
    {
    }

    public SiteProcess(bool withSsr, bool seed = false)
    {
        this.withSsr = withSsr;
        this.seed = seed;
    }
```

3. In `InitializeAsync`, after `await StartAsync();`, add:

```csharp
        if (seed)
        {
            await SeedTestRecipesAsync();
        }
```

4. Add this method after `StartSsrAsync`:

```csharp
    private async Task SeedTestRecipesAsync()
    {
        using var http = new HttpClient { BaseAddress = BaseUrl, Timeout = TimeSpan.FromMinutes(5) };
        using var response = await http.PostAsync("api/dev/seed-recipes", content: null);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Seeding answered {(int)response.StatusCode}.{LogTail()}");
        }
    }
```

The session-shared site that `BasePageTests` injects is built with `SiteProcess()`, so it seeds. The dictionary
restart test builds `new SiteProcess(withSsr: false)`, which does not seed, so that test stays as fast as before.

```bash
dotnet build KitchenCommandCenter.sln
(cd src/KCC.Web && yarn build:all)
dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj
```

Expected: every E2E test passes (Phase 1's seven).

- [ ] **Step 7: Seed the dev database and look at it**

```bash
cd src/KCC.Web && dotnet run --launch-profile Local
```

In a second terminal:

```bash
curl -sk -X POST https://localhost:58671/api/dev/seed-recipes | head -c 400; echo
curl -sk -X POST https://localhost:58671/api/dev/seed-recipes | grep -o 'recipes +0 (skipped 25)'
```

Expected:
- The first call reports `recipes +25 (skipped 0), variants +29, reviews +27, authors +2`.
- The second call prints `recipes +0 (skipped 25)`.

Sign in to `https://localhost:58671/umbraco` and open Content → Home → Recipes. The recipes appear as a
collection, last edited first, and each opens with its variants beneath it. Stop the site.

The dev database now holds test recipes, so `POST /api/dev/baseline/export` refuses to run. That is by design: the
baseline is only ever exported from a fresh database.

- [ ] **Step 8: Commit**

```bash
git add -A src/KCC.Web/Features/DevTools/RecipeSeed src/KCC.Web/Program.cs src/KCC.Web/KCC.Web.csproj \
  tests/KCC.UnitTests tests/KCC.IntegrationTests tests/KCC.E2ETests
git commit -m "Rebuild the Recipe Seeder on Umbraco Services"
```

---

### Task 6: The recipe query service and image tiles

Spec §6.4 says one small query service per aggregate is the only code that touches Umbraco's read APIs. This task
writes the recipe aggregate's service. It reads a recipe with its published variants, and a variant with its recipe
and siblings, into plain records. Those records are what the mapping classes (Tasks 8 and 10) and their unit tests
consume.

Images become one 192 px WebP tile, served with a year of browser cache (spec §7, *Media*). The two pages show
pictures only through `AccentTile`, whose largest use is 96 px (`size-24`, in the hero and the featured card), so
192 px covers high-density screens. One size everywhere lets the browser reuse the same file across pages.

**Files:**
- Create:
  - `src/KCC.Web/Features/Recipes/{RecipeRecord,RecipeImages,RecipeQueries}.cs`
  - `tests/KCC.IntegrationTests/Features/Recipes/RecipeQueriesTests.cs`
- Modify:
  - `tests/KCC.IntegrationTests/Config/TestContent.cs` (the rest of it)
  - `tests/KCC.IntegrationTests/Config/UmbracoSite.cs` (media paths)
  - `src/KCC.Web/appsettings.json`
  - `src/KCC.Web/Program.cs`

**Interfaces:**
- Consumes: the generated `Recipe`, `RecipeVariant`, `RecipeListingPage` and `AddVariantPage` models (Task 2 and
  Phase 1).
- Produces (namespace `KCC.Web.Features.Recipes`):
  - `record RecipeRecord(Guid Key, string Name, string Url, string Description, string Icon, string ImageUrl, string Category, Guid? AuthorKey, DateTime CreateDate)`
  - `record NutritionRecord(int? Calories, int? ProteinG, int? CarbsG, int? FatG, int? SaturatedFatG, int? FiberG, int? SugarG, int? SodiumMg)`
  - `record VariantRecord(Guid Key, string Name, string Url, string Description, string Icon, string ImageUrl, int PrepTime, int CookTime, int Servings, string Difficulty, NutritionRecord Nutrition, IReadOnlyList<string> Tags, string IngredientsJson, string InstructionsJson, Guid? AuthorKey, DateTime CreateDate)`,
    with `int TotalTime`. `Difficulty` is lower case, or null when unset.
  - `record RecipePageData(RecipeRecord Recipe, IReadOnlyList<VariantRecord> Variants, string AddVariantUrl)`
  - `record VariantPageData(VariantRecord Variant, RecipeRecord Recipe, IReadOnlyList<VariantRecord> Siblings)`
  - `IRecipeQueries`, registered scoped:
    - `RecipePageData GetRecipePage(Recipe recipe)`
    - `VariantPageData GetVariantPage(RecipeVariant variant)`, which returns null when the variant has no recipe
      above it
  - `RecipeImages.TileSize` = 192, and `RecipeImages.TileUrl(MediaWithCrops image) → string`, which returns null
    when there is no image.
- Produces in `TestContent`: `RecipeAsync(IServiceProvider, string name, params PropertyValueModel[] more) → Task<Guid>`,
  `VariantAsync(IServiceProvider, Guid recipeKey, string name, params PropertyValueModel[] more) → Task<Guid>`,
  `ImageAsync(IServiceProvider, string name) → Task<Guid>` (a media key), `Image(string alias, Guid mediaKey) →
  PropertyValueModel`, and `UnpublishAsync(IServiceProvider, Guid key)`.

- [ ] **Step 1: Give the test host its own media folders**

In `tests/KCC.IntegrationTests/Config/UmbracoSite.cs`, add these two entries to `Settings()`:

```csharp
        ["Umbraco:CMS:Global:UmbracoMediaPhysicalRootPath"] = Path.Combine(runDirectory, "media"),

        // ImageSharp maps its cache folder under the content root even when given an absolute path.
        ["Umbraco:CMS:Imaging:Cache:CacheFolder"] = $"~/umbraco/Data/TEMP/{Path.GetFileName(runDirectory)}-media-cache",
```

Next, replace the folder deletion in `DisposeAsync` (the `if (runDirectory.Length > 0) { try … }` block) with:

```csharp
        if (runDirectory.Length > 0)
        {
            DeleteQuietly(runDirectory);
            DeleteQuietly(Path.Combine(WebProjectDirectory(), "umbraco", "Data", "TEMP", $"{Path.GetFileName(runDirectory)}-media-cache"));
        }
```

Finally, add this method after `WebProjectDirectory()`:

```csharp
    private static void DeleteQuietly(string directory)
    {
        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (IOException)
        {
            // A log file can still be flushing; the temp folders are disposable either way.
        }
    }
```

`src/KCC.Web/umbraco/Data/` is gitignored, so the cache folder never shows in `git status`.

- [ ] **Step 2: Finish the test-content helpers**

Replace `tests/KCC.IntegrationTests/Config/TestContent.cs` with:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Models.ContentPublishing;
using Umbraco.Cms.Core.Models.TemporaryFile;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Services.OperationStatus;

namespace KCC.IntegrationTests.Config;

// Builds content for one test through the services the backoffice uses, so a test that changes content
// never touches the seeded data set other tests read.
public static class TestContent
{
    // A 1×1 PNG: enough for ImageSharp to resize and re-encode.
    private const string Png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";

    public static Guid RecipeListing(IServiceProvider services)
    {
        var navigation = services.GetRequiredService<IDocumentNavigationQueryService>();
        if (navigation.TryGetRootKeysOfType("homePage", out var homes))
        {
            foreach (var home in homes)
            {
                if (navigation.TryGetChildrenKeysOfType(home, "recipeListingPage", out var listings) && listings.Any())
                {
                    return listings.First();
                }
            }
        }

        throw new InvalidOperationException("The baseline has no recipe listing page.");
    }

    public static Task<Guid> RecipeAsync(IServiceProvider services, string name, params PropertyValueModel[] more) =>
        PublishedAsync(
            services,
            "recipe",
            name,
            RecipeListing(services),
            [
                new PropertyValueModel { Alias = "description", Value = $"{name}, made for one test." },
                new PropertyValueModel { Alias = "icon", Value = "fa-duotone fa-egg" },
                .. more,
            ]);

    public static Task<Guid> VariantAsync(IServiceProvider services, Guid recipeKey, string name, params PropertyValueModel[] more) =>
        PublishedAsync(
            services,
            "recipeVariant",
            name,
            recipeKey,
            [
                new PropertyValueModel { Alias = "description", Value = $"{name}, made for one test." },
                new PropertyValueModel { Alias = "icon", Value = "fa-duotone fa-egg" },
                new PropertyValueModel { Alias = "prepTime", Value = 5 },
                new PropertyValueModel { Alias = "cookTime", Value = 10 },
                new PropertyValueModel { Alias = "servings", Value = 2 },
                new PropertyValueModel { Alias = "ingredients", Value = """[{"name":"Eggs","quantity":2,"unit":"whole","isEyeballed":false}]""" },
                new PropertyValueModel { Alias = "instructions", Value = """[{"step":1,"text":"Scramble."}]""" },
                .. more,
            ]);

    public static PropertyValueModel Image(string alias, Guid mediaKey) => new()
    {
        Alias = alias,
        Value = $"[{{\"key\":\"{Guid.NewGuid()}\",\"mediaKey\":\"{mediaKey}\"}}]",
    };

    public static async Task<Guid> ImageAsync(IServiceProvider services, string name)
    {
        using var scope = services.CreateScope();
        var temporaryKey = Guid.NewGuid();
        var upload = await scope.ServiceProvider.GetRequiredService<ITemporaryFileService>().CreateAsync(new CreateTemporaryFileModel
        {
            Key = temporaryKey,
            FileName = "photo.png",
            OpenReadStream = () => new MemoryStream(Convert.FromBase64String(Png)),
        });
        if (!upload.Success)
        {
            throw new InvalidOperationException($"Uploading {name} failed: {upload.Status}.");
        }

        var mediaKey = Guid.NewGuid();
        var created = await scope.ServiceProvider.GetRequiredService<IMediaEditingService>().CreateAsync(
            new MediaCreateModel
            {
                Key = mediaKey,
                ContentTypeKey = Constants.MediaTypes.Guids.ImageGuid,
                Variants = [new VariantModel { Name = name }],
                Properties = [new PropertyValueModel { Alias = "umbracoFile", Value = $"{{\"temporaryFileId\":\"{temporaryKey}\"}}" }],
            },
            Constants.Security.SuperUserKey);
        if (created.Status != ContentEditingOperationStatus.Success)
        {
            throw new InvalidOperationException($"Creating image {name} failed: {created.Status}.");
        }

        return mediaKey;
    }

    public static async Task UnpublishAsync(IServiceProvider services, Guid key)
    {
        using var scope = services.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<IContentPublishingService>()
            .UnpublishAsync(key, null, Constants.Security.SuperUserKey);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Unpublishing {key} failed: {result.Result}.");
        }
    }

    private static async Task<Guid> PublishedAsync(IServiceProvider services, string contentTypeAlias, string name, Guid parentKey, IEnumerable<PropertyValueModel> values)
    {
        using var scope = services.CreateScope();
        var scoped = scope.ServiceProvider;
        var key = Guid.NewGuid();
        var created = await scoped.GetRequiredService<IContentEditingService>().CreateAsync(
            new ContentCreateModel
            {
                Key = key,
                ContentTypeKey = scoped.GetRequiredService<IContentTypeService>().Get(contentTypeAlias)!.Key,
                ParentKey = parentKey,
                Variants = [new VariantModel { Name = name }],
                Properties = values,
            },
            Constants.Security.SuperUserKey);
        if (created.Status != ContentEditingOperationStatus.Success)
        {
            throw new InvalidOperationException($"Creating {name} failed: {created.Status}.");
        }

        var published = await scoped.GetRequiredService<IContentPublishingService>()
            .PublishAsync(key, [new CulturePublishScheduleModel { Culture = null }], Constants.Security.SuperUserKey);
        if (!published.Success)
        {
            throw new InvalidOperationException($"Publishing {name} failed: {published.Status}.");
        }

        return key;
    }
}
```

`UnpublishAsync` reports its status in `.Result`, unlike the other publishing results.

**If Phase 1 took the §19 template fallback:** in `PublishedAsync`, set
`TemplateKey = (await scoped.GetRequiredService<ITemplateService>().GetAsync("page"))!.Key` on the
`ContentCreateModel`.

- [ ] **Step 3: Write the failing query tests**

Create `tests/KCC.IntegrationTests/Features/Recipes/RecipeQueriesTests.cs`:

```csharp
using System.Net;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.DevTools.RecipeSeed;
using KCC.Web.Features.Models.Generated;
using KCC.Web.Features.Recipes;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Web;

namespace KCC.IntegrationTests.Features.Recipes;

public class RecipeQueriesTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task GetRecipePage_ReadsASeededRecipe()
    {
        var page = WithContent(content => new RecipeQueries().GetRecipePage((Recipe)content.GetById(SeedKeys.Recipe("Spicy Ramen Flight"))!));

        _ = await Assert.That(page.Recipe.Name).IsEqualTo("Spicy Ramen Flight");
        _ = await Assert.That(page.Recipe.Url).IsEqualTo("/recipes/spicy-ramen-flight/");
        _ = await Assert.That(page.Recipe.Category).IsEqualTo("Lunch");
        _ = await Assert.That(page.Recipe.AuthorKey).IsEqualTo(SeedKeys.Author("priya.balan"));
        _ = await Assert.That(page.Recipe.ImageUrl).IsNull();
        _ = await Assert.That(page.Variants.Count).IsEqualTo(4);
        _ = await Assert.That(page.AddVariantUrl).IsEqualTo("/recipes/add-variant/");
    }

    [Test]
    public async Task GetRecipePage_ReadsEachVariant()
    {
        var page = WithContent(content => new RecipeQueries().GetRecipePage((Recipe)content.GetById(SeedKeys.Recipe("Spicy Ramen Flight"))!));

        var shoyu = page.Variants.Single(variant => variant.Name == "Chili Oil Shoyu");

        _ = await Assert.That(shoyu.Url).IsEqualTo("/recipes/spicy-ramen-flight/chili-oil-shoyu/");
        _ = await Assert.That(shoyu.TotalTime).IsEqualTo(20);
        _ = await Assert.That(shoyu.Servings).IsEqualTo(1);
        _ = await Assert.That(string.Join(",", shoyu.Tags)).IsEqualTo("Spicy");
        _ = await Assert.That(shoyu.Nutrition.Calories).IsNull();
        _ = await Assert.That(shoyu.Difficulty).IsNull();
        _ = await Assert.That(shoyu.AuthorKey).IsEqualTo(SeedKeys.Author("priya.balan"));
        _ = await Assert.That(shoyu.IngredientsJson.Contains("\"Chili Oil\"", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task GetVariantPage_ReadsTheRecipeAndTheOtherVariants()
    {
        var page = WithContent(content => new RecipeQueries().GetVariantPage(
            (RecipeVariant)content.GetById(SeedKeys.Variant("Spicy Ramen Flight", "Chili Oil Shoyu"))!));

        _ = await Assert.That(page.Recipe.Name).IsEqualTo("Spicy Ramen Flight");
        _ = await Assert.That(page.Siblings.Count).IsEqualTo(3);
        _ = await Assert.That(page.Siblings.Any(sibling => sibling.Name == "Chili Oil Shoyu")).IsFalse();
    }

    [Test]
    public async Task GetRecipePage_LeavesOutAnUnpublishedVariant()
    {
        var recipeKey = await TestContent.RecipeAsync(Site.Services, "IT Half Published");
        await TestContent.VariantAsync(Site.Services, recipeKey, "Kept");
        var withdrawn = await TestContent.VariantAsync(Site.Services, recipeKey, "Withdrawn");
        await TestContent.UnpublishAsync(Site.Services, withdrawn);

        var page = WithContent(content => new RecipeQueries().GetRecipePage((Recipe)content.GetById(recipeKey)!));

        _ = await Assert.That(string.Join(",", page.Variants.Select(variant => variant.Name))).IsEqualTo("Kept");
    }

    [Test]
    public async Task RecipeImage_IsAWebpTileBrowsersKeepForAYear()
    {
        var imageKey = await TestContent.ImageAsync(Site.Services, "IT Stack photo");
        var recipeKey = await TestContent.RecipeAsync(Site.Services, "IT Pictured", TestContent.Image("image", imageKey));

        var tile = WithContent(content => new RecipeQueries().GetRecipePage((Recipe)content.GetById(recipeKey)!)).Recipe.ImageUrl;
        using var client = Site.CreateClient();
        using var image = await client.GetAsync(tile);

        _ = await Assert.That(tile.Contains($"width={RecipeImages.TileSize}&height={RecipeImages.TileSize}", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(tile.Contains("format=webp", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(image.StatusCode).IsEqualTo(HttpStatusCode.OK);
        _ = await Assert.That(image.Content.Headers.ContentType!.MediaType).IsEqualTo("image/webp");
        _ = await Assert.That(image.Headers.CacheControl!.MaxAge).IsEqualTo(TimeSpan.FromDays(365));
    }

    private T WithContent<T>(Func<IPublishedContentCache, T> read)
    {
        using var context = Site.Services.GetRequiredService<IUmbracoContextFactory>().EnsureUmbracoContext();
        return read(context.UmbracoContext.Content!);
    }
}
```

`GetRecipePage_LeavesOutAnUnpublishedVariant` is the published-only rule that the recipe's rating relies on in Task 8.

```bash
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/RecipeQueriesTests/*"
```

Expected: build errors, because `RecipeQueries` and `RecipeImages` do not exist.

- [ ] **Step 4: Write the records, the tiles and the service**

Create `src/KCC.Web/Features/Recipes/RecipeRecord.cs`:

```csharp
namespace KCC.Web.Features.Recipes;

public sealed record RecipeRecord(
    Guid Key,
    string Name,
    string Url,
    string Description,
    string Icon,
    string ImageUrl,
    string Category,
    Guid? AuthorKey,
    DateTime CreateDate);

public sealed record NutritionRecord(
    int? Calories,
    int? ProteinG,
    int? CarbsG,
    int? FatG,
    int? SaturatedFatG,
    int? FiberG,
    int? SugarG,
    int? SodiumMg);

public sealed record VariantRecord(
    Guid Key,
    string Name,
    string Url,
    string Description,
    string Icon,
    string ImageUrl,
    int PrepTime,
    int CookTime,
    int Servings,
    string Difficulty,
    NutritionRecord Nutrition,
    IReadOnlyList<string> Tags,
    string IngredientsJson,
    string InstructionsJson,
    Guid? AuthorKey,
    DateTime CreateDate)
{
    public int TotalTime => PrepTime + CookTime;
}

public sealed record RecipePageData(RecipeRecord Recipe, IReadOnlyList<VariantRecord> Variants, string AddVariantUrl);

public sealed record VariantPageData(VariantRecord Variant, RecipeRecord Recipe, IReadOnlyList<VariantRecord> Siblings);
```

Create `src/KCC.Web/Features/Recipes/RecipeImages.cs`:

```csharp
using Umbraco.Cms.Core.Models;
using Umbraco.Extensions;

namespace KCC.Web.Features.Recipes;

public static class RecipeImages
{
    // AccentTile draws a picture at most 96px square (size-24, in the hero and the featured card). Twice that
    // covers high-density screens, and one size for every tile lets a browser reuse the file across pages.
    public const int TileSize = 192;

    public static string TileUrl(MediaWithCrops image) => image?.GetCropUrl(
        width: TileSize,
        height: TileSize,
        imageCropMode: ImageCropMode.Crop,
        furtherOptions: "&format=webp");
}
```

Create `src/KCC.Web/Features/Recipes/RecipeQueries.cs`:

```csharp
using KCC.Web.Features.Models.Generated;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace KCC.Web.Features.Recipes;

public interface IRecipeQueries
{
    RecipePageData GetRecipePage(Recipe recipe);

    VariantPageData GetVariantPage(RecipeVariant variant);
}

public class RecipeQueries : IRecipeQueries
{
    public RecipePageData GetRecipePage(Recipe recipe) => new(
        RecipeFrom(recipe),
        recipe.Children<RecipeVariant>().Select(VariantFrom).ToList(),
        recipe.Parent<RecipeListingPage>()?.Children<AddVariantPage>().FirstOrDefault()?.Url());

    public VariantPageData GetVariantPage(RecipeVariant variant)
    {
        var recipe = variant.Parent<Recipe>();
        if (recipe is null)
        {
            return null;
        }

        return new(
            VariantFrom(variant),
            RecipeFrom(recipe),
            recipe.Children<RecipeVariant>().Where(sibling => sibling.Key != variant.Key).Select(VariantFrom).ToList());
    }

    private static RecipeRecord RecipeFrom(Recipe recipe) => new(
        recipe.Key,
        recipe.Name,
        recipe.Url(),
        recipe.Description,
        recipe.Icon,
        RecipeImages.TileUrl(recipe.Image),
        recipe.Category?.Name,
        recipe.Author?.Key,
        recipe.CreateDate);

    private static VariantRecord VariantFrom(RecipeVariant variant) => new(
        variant.Key,
        variant.Name,
        variant.Url(),
        variant.Description,
        variant.Icon,
        RecipeImages.TileUrl(variant.Images?.FirstOrDefault()),
        variant.PrepTime,
        variant.CookTime,
        variant.Servings,
        string.IsNullOrEmpty(variant.Difficulty) ? null : variant.Difficulty.ToLowerInvariant(),
        new NutritionRecord(
            Optional(variant, "calories", variant.Calories),
            Optional(variant, "proteinG", variant.ProteinG),
            Optional(variant, "carbsG", variant.CarbsG),
            Optional(variant, "fatG", variant.FatG),
            Optional(variant, "saturatedFatG", variant.SaturatedFatG),
            Optional(variant, "fiberG", variant.FiberG),
            Optional(variant, "sugarG", variant.SugarG),
            Optional(variant, "sodiumMg", variant.SodiumMg)),
        (variant.Tags ?? []).Select(tag => tag.Name).ToList(),
        variant.Ingredients,
        variant.Instructions,
        variant.Author?.Key,
        variant.CreateDate);

    // An empty integer property reads as 0, which would print a real zero where the owner entered nothing.
    private static int? Optional(IPublishedContent content, string alias, int value) =>
        content.GetProperty(alias)?.HasValue() == true ? value : null;
}
```

Some details the code relies on:
- The difficulty is lower-cased because the Vue tile keys on `easy`, `medium` and `hard`.
- An unset dropdown reads `""`, which becomes null, so Razor leaves the attribute out.
- The published cache holds only published nodes, so `Children<RecipeVariant>()` is already "published variants only".

In `src/KCC.Web/appsettings.json`, add this under `Umbraco:CMS`, after `ModelsBuilder`:

```json
      "Imaging": {
        "Cache": {
          "BrowserMaxAge": "365.00:00:00"
        }
      }
```

Every processed image URL carries a `v=` cache buster, so ImageSharp marks the response `immutable`, and a year is
safe. Without this setting the default is 7 days.

In `src/KCC.Web/Program.cs`:
- Add `using KCC.Web.Features.Recipes;` in sorted position, after `KCC.Web.Features.Pages.Shared`.
- After the `IRobotsTxtProvider` registration, add `builder.Services.AddScoped<IRecipeQueries, RecipeQueries>();`.

- [ ] **Step 5: Run the tests**

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj
```

Expected: 0 warnings, and every test passes, including the 5 `RecipeQueriesTests`.

- [ ] **Step 6: Commit**

```bash
git add -A src/KCC.Web/Features/Recipes src/KCC.Web/appsettings.json src/KCC.Web/Program.cs tests/KCC.IntegrationTests
git commit -m "Add the Recipe Query Service and Image Tiles"
```

---

### Task 7: Breadcrumbs from the tree

Umbraco's tree puts Home at the root and Recipes beneath it. So the trail is the page's ancestors-or-self, reversed,
with two adjustments:
- The first crumb keeps Home's URL but takes the `Shared.Home` label, which the trail shows as the house icon.
- The last crumb is the page itself, so it gets no link.

Each crumb's label is the page's breadcrumb label, else its metadata title, else its name. That is what the Kentico
service produced. Its `ParentId` and `WebPageItemId` members never reached the Vue component, whose `Breadcrumb`
type is `{ linkText, url }`, so they go.

**Files:**
- Rewrite:
  - `src/KCC.Web/Features/Components/Breadcrumbs/BreadcrumbService.cs`
  - `src/KCC.Web/Features/Components/Breadcrumbs/BreadcrumbLink.cs`
- Create:
  - `src/KCC.Web/Features/Components/Breadcrumbs/BreadcrumbTrail.cs`
  - `tests/KCC.UnitTests/Features/Components/Breadcrumbs/BreadcrumbTrailTests.cs`
  - `tests/KCC.IntegrationTests/Features/Components/BreadcrumbServiceTests.cs`
- Modify:
  - `src/KCC.Web/KCC.Web.csproj` (delete the `Features/Components/Breadcrumbs/BreadcrumbService.cs` line)
  - `src/KCC.Web/Program.cs`

**Interfaces:**
- Consumes: `IResourceStringProvider.GetOrDefault`, and the generated `IMetadata` (Phase 1).
- Produces:
  - `record BreadcrumbLink(string LinkText, string Url)`.
  - `BreadcrumbTrail.Label(string breadcrumbLabel, string metadataTitle, string name) → string`.
  - `BreadcrumbTrail.Build(string homeLabel, IReadOnlyList<BreadcrumbTrail.Crumb> rootFirst) → IReadOnlyList<BreadcrumbLink>`,
    where `record Crumb(string Label, string Url)`.
  - `BreadcrumbService.Build(IPublishedContent page) → IReadOnlyList<BreadcrumbLink>`, registered scoped.

- [ ] **Step 1: Write the failing trail tests**

Create `tests/KCC.UnitTests/Features/Components/Breadcrumbs/BreadcrumbTrailTests.cs`:

```csharp
using KCC.Web.Features.Components.Breadcrumbs;

namespace KCC.UnitTests.Features.Components.Breadcrumbs;

public class BreadcrumbTrailTests
{
    [Test]
    public async Task Build_ShowsHomeUnderItsOwnLabelAndLeavesThePageUnlinked()
    {
        var trail = BreadcrumbTrail.Build(
            "Home",
            [new("Kitchen Command Center", "/"), new("Recipes", "/recipes/"), new("Pancakes", "/recipes/pancakes/")]);

        _ = await Assert.That(Describe(trail)).IsEqualTo("Home>/|Recipes>/recipes/|Pancakes>");
    }

    [Test]
    public async Task Build_OnTheHomePage_IsTheHomeCrumbAlone()
    {
        var trail = BreadcrumbTrail.Build("Home", [new("Kitchen Command Center", "/")]);

        _ = await Assert.That(Describe(trail)).IsEqualTo("Home>/");
    }

    [Test]
    public async Task Build_WithNoPages_IsEmpty()
    {
        _ = await Assert.That(BreadcrumbTrail.Build("Home", []).Count).IsEqualTo(0);
    }

    [Test]
    [Arguments("Short", "Title", "Name", "Short")]
    [Arguments("", "Title", "Name", "Title")]
    [Arguments(" ", null, "Name", "Name")]
    public async Task Label_PrefersTheBreadcrumbLabelThenTheTitleThenTheName(string label, string title, string name, string expected)
    {
        _ = await Assert.That(BreadcrumbTrail.Label(label, title, name)).IsEqualTo(expected);
    }

    private static string Describe(IEnumerable<BreadcrumbLink> trail) =>
        string.Join("|", trail.Select(link => $"{link.LinkText}>{link.Url}"));
}
```

In `src/KCC.Web/KCC.Web.csproj`, delete `<Compile Remove="Features/Components/Breadcrumbs/BreadcrumbService.cs" />`.

```bash
dotnet build KitchenCommandCenter.sln
```

Expected: build errors from the Kentico `BreadcrumbService.cs`, which is compiled again, and because
`BreadcrumbTrail` is missing.

- [ ] **Step 2: Write the trail and the service**

Replace `src/KCC.Web/Features/Components/Breadcrumbs/BreadcrumbLink.cs` with:

```csharp
namespace KCC.Web.Features.Components.Breadcrumbs;

public record BreadcrumbLink(string LinkText, string Url);
```

Create `src/KCC.Web/Features/Components/Breadcrumbs/BreadcrumbTrail.cs`:

```csharp
namespace KCC.Web.Features.Components.Breadcrumbs;

public static class BreadcrumbTrail
{
    public static string Label(string breadcrumbLabel, string metadataTitle, string name) =>
        !string.IsNullOrWhiteSpace(breadcrumbLabel) ? breadcrumbLabel
        : !string.IsNullOrWhiteSpace(metadataTitle) ? metadataTitle
        : name;

    // The first crumb is the home page, which the trail shows as the home icon under its own label; the
    // last is the page itself, which is not linked.
    public static IReadOnlyList<BreadcrumbLink> Build(string homeLabel, IReadOnlyList<Crumb> rootFirst)
    {
        if (rootFirst.Count == 0)
        {
            return [];
        }

        var trail = new List<BreadcrumbLink> { new(homeLabel, rootFirst[0].Url) };
        for (var index = 1; index < rootFirst.Count; index++)
        {
            trail.Add(new(rootFirst[index].Label, index == rootFirst.Count - 1 ? string.Empty : rootFirst[index].Url));
        }

        return trail;
    }

    public sealed record Crumb(string Label, string Url);
}
```

Replace `src/KCC.Web/Features/Components/Breadcrumbs/BreadcrumbService.cs` with:

```csharp
using KCC.Web.Features.Dictionary;
using KCC.Web.Features.Models.Generated;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace KCC.Web.Features.Components.Breadcrumbs;

public class BreadcrumbService(IResourceStringProvider resourceStrings)
{
    public IReadOnlyList<BreadcrumbLink> Build(IPublishedContent page) => BreadcrumbTrail.Build(
        resourceStrings.GetOrDefault("Shared.Home"),
        page.AncestorsOrSelf().Reverse().Select(node => new BreadcrumbTrail.Crumb(Label(node), node.Url())).ToList());

    private static string Label(IPublishedContent node) => node is IMetadata metadata
        ? BreadcrumbTrail.Label(metadata.BreadcrumbLabel, metadata.MetadataTitle, node.Name)
        : node.Name;
}
```

`AncestorsOrSelf()` runs from the page up to the root, which is why the trail reverses it.

In `src/KCC.Web/Program.cs`:
- Add `using KCC.Web.Features.Components.Breadcrumbs;` in sorted position, before `KCC.Web.Features.Components.Header`.
- After the `IRecipeQueries` registration, add `builder.Services.AddScoped<BreadcrumbService>();`.

```bash
grep -rn "new BreadcrumbLink(\|ParentId:\|WebPageItemId:" src tests --include='*.cs'
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj -- --treenode-filter "/*/*/BreadcrumbTrailTests/*"
```

Expected:
- The grep finds nothing outside `BreadcrumbTrail.cs`, so no caller used the dropped members.
- 0 warnings.
- 4 test methods pass (6 cases).

- [ ] **Step 3: Test the service on the seeded tree**

Create `tests/KCC.IntegrationTests/Features/Components/BreadcrumbServiceTests.cs`:

```csharp
using KCC.IntegrationTests.Config;
using KCC.Web.Features.Components.Breadcrumbs;
using KCC.Web.Features.DevTools.RecipeSeed;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Web;

namespace KCC.IntegrationTests.Features.Components;

public class BreadcrumbServiceTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task Build_RunsFromHomeToTheVariant()
    {
        using var scope = Site.Services.CreateScope();
        using var context = Site.Services.GetRequiredService<IUmbracoContextFactory>().EnsureUmbracoContext();
        var variant = context.UmbracoContext.Content!.GetById(SeedKeys.Variant("Fluffy Buttermilk Pancakes", "Classic Stack"))!;

        var trail = scope.ServiceProvider.GetRequiredService<BreadcrumbService>().Build(variant);

        _ = await Assert.That(string.Join("|", trail.Select(link => $"{link.LinkText}>{link.Url}")))
            .IsEqualTo("Home>/|Recipes>/recipes/|Fluffy Buttermilk Pancakes>/recipes/fluffy-buttermilk-pancakes/|Classic Stack>");
    }
}
```

```bash
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/BreadcrumbServiceTests/*"
```

Expected: 1 passed. `Shared.Home` reads "Home" from the dictionary Phase 1 imported.

- [ ] **Step 4: Commit**

```bash
git add -A src/KCC.Web/Features/Components/Breadcrumbs src/KCC.Web/KCC.Web.csproj src/KCC.Web/Program.cs \
  tests/KCC.UnitTests tests/KCC.IntegrationTests
git commit -m "Build Breadcrumbs from the Umbraco Tree"
```

---

### Task 8: The recipe page

The Kentico `RecipeDetailController` becomes the hijacking `RecipeController`. The query service reads the page, a
pure mapping class fills the existing `RecipeDetailViewModel`, and the existing `Index.cshtml` renders it unchanged.
The recipe's rating and times-cooked count cover its published variants only (spec §9.1 and §17).
`RecipeDetailViewModelTests` compiles again unchanged; spec §14 keeps pure view-model tests as they are.

**Files:**
- Move and rewrite: `src/KCC.Web/Features/Pages/RecipeDetail/RecipeDetailController.cs` → `RecipeController.cs`
- Create:
  - `src/KCC.Web/Features/Pages/RecipeDetail/RecipeDetailMapping.cs`
  - `tests/KCC.UnitTests/Features/Pages/RecipeDetail/RecipeDetailMappingTests.cs`
  - `tests/KCC.IntegrationTests/Config/RenderedPage.cs`
  - `tests/KCC.IntegrationTests/Features/Pages/RecipePageTests.cs`
- Modify:
  - `src/KCC.Web/KCC.Web.csproj`: delete the `Features/Pages/RecipeDetail/**` `Compile Remove` line and the
    `Features/Pages/RecipeDetail/**/*.cshtml` `Content Remove` line.
  - `tests/KCC.UnitTests/KCC.UnitTests.csproj`: delete the `Features/Pages/RecipeDetail/**` line.
  - `tests/KCC.IntegrationTests/Features/Pages/SitemapTests.cs`: add one test.

**Interfaces:**
- Consumes:
  - `IRecipeQueries` and `RecipePageData` (Task 6)
  - `IContributionStats` and `ContributionStats` (Task 3)
  - `IAuthorNameProvider` and `AuthorNameProvider.NameFor` (Task 4)
  - `BreadcrumbService` (Task 7)
  - `PageMetadata` and `IResourceStringProvider` (Phase 1)
- Produces:
  - `RecipeDetailMapping.AuthorKeys(RecipePageData) → IEnumerable<Guid>`.
  - `RecipeDetailMapping.Map(RecipePageData page, ContributionStats stats, IReadOnlyDictionary<Guid, string> authorNames) → RecipeDetailViewModel`.
  - `RenderedPage.GetAsync(HttpClient client, string path) → Task<RenderedPage>`, with `Status`, `Html` and `Body`,
    plus two readers:
    - `Attribute(string name) → string?`, the HTML-decoded value of an attribute on the page's root Vue element.
    - `Prop(string name) → JsonElement`, the parsed JSON of the `:name` prop. Pass the name without the colon.

- [ ] **Step 1: Write the failing mapping tests**

In `tests/KCC.UnitTests/KCC.UnitTests.csproj`, delete `<Compile Remove="Features/Pages/RecipeDetail/**" />`.

Create `tests/KCC.UnitTests/Features/Pages/RecipeDetail/RecipeDetailMappingTests.cs`:

```csharp
using KCC.Contributions;
using KCC.Web.Features.Pages.RecipeDetail;
using KCC.Web.Features.Recipes;

namespace KCC.UnitTests.Features.Pages.RecipeDetail;

public class RecipeDetailMappingTests
{
    private static readonly IReadOnlyDictionary<Guid, string> NoNames = new Dictionary<Guid, string>();

    [Test]
    public async Task Map_RatesTheRecipeAcrossTheVariantsItLists()
    {
        var first = Variant("Classic Stack");
        var second = Variant("Blueberry Stack");
        var elsewhere = Guid.NewGuid();
        var stats = ContributionStats.Build([(first.Key, 5m), (second.Key, 3m), (elsewhere, 1m)], []);

        var viewModel = RecipeDetailMapping.Map(Page(first, second), stats, NoNames);

        _ = await Assert.That(viewModel.RecipeAverageRating).IsEqualTo(4d);
        _ = await Assert.That(viewModel.RecipeReviewCount).IsEqualTo(2);
    }

    [Test]
    public async Task Map_SumsTheTimesCookedAcrossItsVariants()
    {
        var first = Variant("Classic Stack");
        var second = Variant("Blueberry Stack");
        var stats = ContributionStats.Build([], [first.Key, second.Key, second.Key]);

        var viewModel = RecipeDetailMapping.Map(Page(first, second), stats, NoNames);

        _ = await Assert.That(viewModel.RecipeTimesCooked).IsEqualTo(3);
    }

    [Test]
    public async Task Map_NamesTheRecipeAndVariantAuthors()
    {
        var priya = Guid.NewGuid();
        var diego = Guid.NewGuid();
        var page = Page(Variant("Classic Stack", authorKey: diego)) with { Recipe = Recipe(authorKey: priya) };
        var names = new Dictionary<Guid, string> { [priya] = "Priya Balan", [diego] = "Diego Salazar" };

        var viewModel = RecipeDetailMapping.Map(page, ContributionStats.Build([], []), names);

        _ = await Assert.That(viewModel.StartedByName).IsEqualTo("Priya Balan");
        _ = await Assert.That(viewModel.Variants.Single().AuthorName).IsEqualTo("Diego Salazar");
    }

    [Test]
    public async Task Map_CarriesEachVariantsCard()
    {
        var variant = Variant("Classic Stack", imageUrl: "/media/stack.jpg?width=192");
        var stats = ContributionStats.Build([(variant.Key, 4.5m), (variant.Key, 4m)], [variant.Key]);

        var card = RecipeDetailMapping.Map(Page(variant), stats, NoNames).Variants.Single();

        _ = await Assert.That(card.Name).IsEqualTo("Classic Stack");
        _ = await Assert.That(card.Slug).IsEqualTo(variant.Url);
        _ = await Assert.That(card.Image).IsEqualTo("/media/stack.jpg?width=192");
        _ = await Assert.That(card.TotalTime).IsEqualTo(25);
        _ = await Assert.That(card.PublishedDate).IsEqualTo(variant.CreateDate);
        _ = await Assert.That(card.AverageRating).IsEqualTo(4.25d);
        _ = await Assert.That(card.ReviewCount).IsEqualTo(2);
        _ = await Assert.That(card.CookedCount).IsEqualTo(1);
        _ = await Assert.That(string.Join(",", card.Tags)).IsEqualTo("Vegetarian");
    }

    [Test]
    public async Task Map_CarriesTheRecipeHeader()
    {
        var page = Page();

        var viewModel = RecipeDetailMapping.Map(page, ContributionStats.Build([], []), NoNames);

        _ = await Assert.That(viewModel.RecipeName).IsEqualTo("Fluffy Buttermilk Pancakes");
        _ = await Assert.That(viewModel.RecipeCategory).IsEqualTo("Breakfast");
        _ = await Assert.That(viewModel.RecipeGuid).IsEqualTo(page.Recipe.Key);
        _ = await Assert.That(viewModel.AddVariantUrl).IsEqualTo("/recipes/add-variant/");
    }

    [Test]
    public async Task AuthorKeys_TakesTheRecipeAndVariantAuthorsAndSkipsBlanks()
    {
        var priya = Guid.NewGuid();
        var diego = Guid.NewGuid();
        var page = Page(Variant("A", authorKey: diego), Variant("B")) with { Recipe = Recipe(authorKey: priya) };

        _ = await Assert.That(string.Join(",", RecipeDetailMapping.AuthorKeys(page))).IsEqualTo($"{priya},{diego}");
    }

    private static RecipePageData Page(params VariantRecord[] variants) => new(Recipe(), variants, "/recipes/add-variant/");

    private static RecipeRecord Recipe(Guid? authorKey = null) => new(
        Guid.NewGuid(),
        "Fluffy Buttermilk Pancakes",
        "/recipes/fluffy-buttermilk-pancakes/",
        "Tall, tender stacks.",
        "fa-duotone fa-pancakes",
        null,
        "Breakfast",
        authorKey,
        new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc));

    private static VariantRecord Variant(string name, Guid? authorKey = null, string imageUrl = null) => new(
        Guid.NewGuid(),
        name,
        $"/recipes/fluffy-buttermilk-pancakes/{name.ToLowerInvariant().Replace(' ', '-')}/",
        "Griddle to golden.",
        "fa-duotone fa-pancakes",
        imageUrl,
        10,
        15,
        4,
        null,
        new NutritionRecord(null, null, null, null, null, null, null, null),
        ["Vegetarian"],
        "[]",
        "[]",
        authorKey,
        new DateTime(2026, 9, 20, 0, 1, 0, DateTimeKind.Utc));
}
```

`Map_RatesTheRecipeAcrossTheVariantsItLists` gives a review to a variant the page does not list. That review stands
for an unpublished variant, and it must not count.

```bash
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj -- --treenode-filter "/*/*/RecipeDetailMappingTests/*"
```

Expected: build errors, because `RecipeDetailMapping` is missing and the recipe-detail slice is still excluded from
KCC.Web.

- [ ] **Step 2: Bring the slice back and write the mapping**

In `src/KCC.Web/KCC.Web.csproj`, delete `<Compile Remove="Features/Pages/RecipeDetail/**" />` and
`<Content Remove="Features/Pages/RecipeDetail/**/*.cshtml" />`. Then move the controller:

```bash
git mv src/KCC.Web/Features/Pages/RecipeDetail/RecipeDetailController.cs src/KCC.Web/Features/Pages/RecipeDetail/RecipeController.cs
```

Create `src/KCC.Web/Features/Pages/RecipeDetail/RecipeDetailMapping.cs`:

```csharp
using KCC.Contributions;
using KCC.Web.Features.Providers;
using KCC.Web.Features.Recipes;

namespace KCC.Web.Features.Pages.RecipeDetail;

public static class RecipeDetailMapping
{
    public static IEnumerable<Guid> AuthorKeys(RecipePageData page) =>
        page.Variants.Select(variant => variant.AuthorKey).Prepend(page.Recipe.AuthorKey).OfType<Guid>();

    public static RecipeDetailViewModel Map(RecipePageData page, ContributionStats stats, IReadOnlyDictionary<Guid, string> authorNames)
    {
        var variantKeys = page.Variants.Select(variant => variant.Key).ToList();
        var rating = stats.RatingAcross(variantKeys);

        return new RecipeDetailViewModel
        {
            RecipeName = page.Recipe.Name,
            RecipeDescription = page.Recipe.Description,
            RecipeImagePath = page.Recipe.ImageUrl,
            RecipeIcon = page.Recipe.Icon,
            RecipeCategory = page.Recipe.Category,
            RecipeGuid = page.Recipe.Key,
            RecipeAverageRating = rating.Average,
            RecipeReviewCount = rating.Count,
            RecipeTimesCooked = stats.CookedAcross(variantKeys),
            AddVariantUrl = page.AddVariantUrl,
            StartedByName = AuthorNameProvider.NameFor(authorNames, page.Recipe.AuthorKey),
            Variants = page.Variants.Select(variant => Summary(variant, stats.For(variant.Key), authorNames)).ToList(),
        };
    }

    private static VariantSummaryViewModel Summary(VariantRecord variant, VariantStats stats, IReadOnlyDictionary<Guid, string> authorNames) => new()
    {
        Name = variant.Name,
        Description = variant.Description,
        Slug = variant.Url,
        Image = variant.ImageUrl,
        Icon = variant.Icon,
        AuthorName = AuthorNameProvider.NameFor(authorNames, variant.AuthorKey),
        Tags = variant.Tags,
        TotalTime = variant.TotalTime,
        PublishedDate = variant.CreateDate,
        AverageRating = stats.Rating.Average,
        ReviewCount = stats.Rating.Count,
        CookedCount = stats.CookedCount,
    };
}
```

Replace the contents of `src/KCC.Web/Features/Pages/RecipeDetail/RecipeController.cs` with the file below. The
`GetStrings` list is the Kentico controller's, unchanged:

```csharp
using KCC.Contributions;
using KCC.Web.Features.Components.Breadcrumbs;
using KCC.Web.Features.Dictionary;
using KCC.Web.Features.Models.Generated;
using KCC.Web.Features.Pages.Shared;
using KCC.Web.Features.Providers;
using KCC.Web.Features.Recipes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace KCC.Web.Features.Pages.RecipeDetail;

public class RecipeController(
    ILogger<RenderController> logger,
    ICompositeViewEngine compositeViewEngine,
    IUmbracoContextAccessor umbracoContextAccessor,
    IRecipeQueries recipes,
    IContributionStats contributionStats,
    IAuthorNameProvider authorNames,
    BreadcrumbService breadcrumbs,
    IResourceStringProvider resourceStrings,
    PageMetadata pageMetadata)
    : RenderController(logger, compositeViewEngine, umbracoContextAccessor)
{
    // Route hijacking calls the synchronous Index unless it is hidden like this; the overload below serves the page.
    [NonAction]
    public sealed override IActionResult Index() => throw new NotSupportedException();

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (CurrentPage is not Recipe recipe)
        {
            return NotFound();
        }

        var page = recipes.GetRecipePage(recipe);
        var viewModel = RecipeDetailMapping.Map(
            page,
            await contributionStats.GetAsync(),
            await authorNames.ResolveMany(RecipeDetailMapping.AuthorKeys(page)));
        viewModel.Breadcrumbs = breadcrumbs.Build(recipe);
        viewModel.ResourceStrings = GetStrings();
        pageMetadata.Apply(recipe, viewModel);

        return View("~/Features/Pages/RecipeDetail/Index.cshtml", viewModel);
    }

    private Dictionary<string, string> GetStrings() => resourceStrings.GetManyOrDefault(
        "RecipeDetail.AddVariant",
        "RecipeDetail.StartedBy",
        "RecipeDetail.Variants",
        "RecipeDetail.By",
        "RecipeDetail.ComingSoon",
        "RecipeDetail.AvgTime",
        "RecipeDetail.Fastest",
        "RecipeDetail.Contributors",
        "RecipeDetail.TopVariant",
        "RecipeDetail.RankingComingSoon",
        "RecipeDetail.AllVariants",
        "RecipeDetail.Sort",
        "RecipeDetail.SortNewest",
        "RecipeDetail.SortFastest",
        "RecipeDetail.SortTopRated",
        "RecipeDetail.Min",
        "RecipeDetail.SearchVariants",
        "RecipeDetail.Grid",
        "RecipeDetail.List",
        "RecipeDetail.Total",
        "RecipeDetail.Of",
        "RecipeDetail.NoVariantsMatch",
        "RecipeDetail.TryDifferentFilter",
        "RecipeDetail.ClearFilters",
        "RecipeDetail.TimesCooked",
        "RecipeDetail.NoRatingsYet",
        "RecipeDetail.Rating",
        "RecipeDetail.Reviews",
        "RecipeDetail.All");
}
```

Two differences from the Kentico controller:
- A variant card's published date is the variant's create date (spec §7).
- The recipe no longer needs its stored GUID, because its variants come from the published tree.

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj
```

Expected: 0 warnings. Every unit test passes, including the 6 `RecipeDetailMappingTests` and the existing
`RecipeDetailViewModelTests`.

- [ ] **Step 3: Write the page tests**

Create `tests/KCC.IntegrationTests/Config/RenderedPage.cs`:

```csharp
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace KCC.IntegrationTests.Config;

// The test host renders with SSR off, so a page's body arrives as the Vue template text inside the
// server-content JSON, and its root component carries the view model as attributes.
public sealed class RenderedPage
{
    private const string ServerContentOpen = "<script id=\"server-content\" type=\"application/json\">";

    private RenderedPage(HttpStatusCode status, string html, string body)
    {
        Status = status;
        Html = html;
        Body = body;
    }

    public HttpStatusCode Status { get; }

    public string Html { get; }

    public string Body { get; }

    public static async Task<RenderedPage> GetAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();
        var body = string.Empty;
        var start = html.IndexOf(ServerContentOpen, StringComparison.Ordinal);
        if (start >= 0)
        {
            start += ServerContentOpen.Length;
            var end = html.IndexOf("</script>", start, StringComparison.Ordinal);
            using var content = JsonDocument.Parse(html[start..end]);
            body = content.RootElement.GetProperty("bodyContent").GetString() ?? string.Empty;
        }

        return new RenderedPage(response.StatusCode, html, body);
    }

    public string? Attribute(string name)
    {
        var match = Regex.Match(Body, $"\\s{Regex.Escape(name)}=\"([^\"]*)\"");
        return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value) : null;
    }

    public JsonElement Prop(string name)
    {
        var json = Attribute($":{name}") ?? throw new InvalidOperationException($"The page has no :{name} prop.");
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
```

The helper leans on four rendering behaviours:
- Razor HTML-encodes attribute values and drops an attribute whose value is null.
- `Vue.Prop` writes camelCase JSON with `&quot;` for quotes, and leaves null members out.
- The server-content JSON escapes markup as `\u003C` and similar, which `JsonDocument` decodes.
- An attribute's `\s` prefix keeps `recipe-name` from matching inside `:recipe-name`.

Create `tests/KCC.IntegrationTests/Features/Pages/RecipePageTests.cs`:

```csharp
using System.Net;
using KCC.Contributions;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.DevTools.RecipeSeed;
using Microsoft.Extensions.DependencyInjection;

namespace KCC.IntegrationTests.Features.Pages;

public class RecipePageTests
{
    private const string PancakesPath = "/recipes/fluffy-buttermilk-pancakes/";

    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task SeededRecipe_RendersThroughItsController()
    {
        using var client = Site.CreateClient();

        var page = await RenderedPage.GetAsync(client, PancakesPath);

        _ = await Assert.That(page.Status).IsEqualTo(HttpStatusCode.OK);
        _ = await Assert.That(page.Html.Contains("<title>Fluffy Buttermilk Pancakes</title>", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(page.Attribute("recipe-name")).IsEqualTo("Fluffy Buttermilk Pancakes");
        _ = await Assert.That(page.Attribute("recipe-category")).IsEqualTo("Breakfast");
        _ = await Assert.That(page.Attribute("recipe-guid")).IsEqualTo(SeedKeys.Recipe("Fluffy Buttermilk Pancakes").ToString());
        _ = await Assert.That(page.Attribute("add-variant-url")).IsEqualTo("/recipes/add-variant/");
        _ = await Assert.That(page.Attribute("started-by-name")).IsNull();
    }

    [Test]
    public async Task SeededRecipe_CarriesItsRatingAndVariants()
    {
        using var client = Site.CreateClient();

        var page = await RenderedPage.GetAsync(client, PancakesPath);
        var variants = page.Prop("variants");

        _ = await Assert.That(page.Prop("recipe-average-rating").GetDouble()).IsEqualTo(4.25d);
        _ = await Assert.That(page.Prop("recipe-review-count").GetInt32()).IsEqualTo(2);
        _ = await Assert.That(page.Prop("recipe-times-cooked").GetInt32()).IsEqualTo(0);
        _ = await Assert.That(variants.GetArrayLength()).IsEqualTo(1);
        _ = await Assert.That(variants[0].GetProperty("name").GetString()).IsEqualTo("Classic Stack");
        _ = await Assert.That(variants[0].GetProperty("slug").GetString()).IsEqualTo("/recipes/fluffy-buttermilk-pancakes/classic-stack/");
        _ = await Assert.That(variants[0].GetProperty("totalTime").GetInt32()).IsEqualTo(25);
        _ = await Assert.That(variants[0].GetProperty("tags").ToString()).IsEqualTo("[\"Vegetarian\"]");
    }

    [Test]
    public async Task SeededRecipe_TrailRunsFromHome()
    {
        using var client = Site.CreateClient();

        var page = await RenderedPage.GetAsync(client, PancakesPath);
        var trail = page.Prop("breadcrumbs").EnumerateArray()
            .Select(crumb => $"{crumb.GetProperty("linkText").GetString()}>{crumb.GetProperty("url").GetString()}");

        _ = await Assert.That(string.Join("|", trail)).IsEqualTo("Home>/|Recipes>/recipes/|Fluffy Buttermilk Pancakes>");
    }

    [Test]
    public async Task Recipe_RatesOnlyItsPublishedVariants()
    {
        var recipeKey = await TestContent.RecipeAsync(Site.Services, "IT Rated Recipe");
        var kept = await TestContent.VariantAsync(Site.Services, recipeKey, "Kept");
        var withdrawn = await TestContent.VariantAsync(Site.Services, recipeKey, "Withdrawn");
        var writes = Site.Services.GetRequiredService<IContributionWrites>();
        await writes.UpsertReviewAsync(kept, Guid.NewGuid(), 5m, "Lovely");
        await writes.UpsertReviewAsync(withdrawn, Guid.NewGuid(), 1m, "Gone");
        await TestContent.UnpublishAsync(Site.Services, withdrawn);
        using var client = Site.CreateClient();

        var page = await RenderedPage.GetAsync(client, "/recipes/it-rated-recipe/");

        _ = await Assert.That(page.Prop("recipe-average-rating").GetDouble()).IsEqualTo(5d);
        _ = await Assert.That(page.Prop("recipe-review-count").GetInt32()).IsEqualTo(1);
        _ = await Assert.That(page.Prop("variants").GetArrayLength()).IsEqualTo(1);
    }
}
```

`Recipe_RatesOnlyItsPublishedVariants` is spec §17's "reviews of deleted variants still count" defect, seen from the
page. The unpublished variant's 1-star review would pull the average to 3.

In `tests/KCC.IntegrationTests/Features/Pages/SitemapTests.cs`, add:

```csharp
    [Test]
    public async Task Sitemap_ListsSeededRecipesAndVariants()
    {
        using var client = Site.CreateClient();
        var xml = await client.GetStringAsync("/sitemap.xml");

        _ = await Assert.That(xml.Contains("/recipes/fluffy-buttermilk-pancakes/", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(xml.Contains("/recipes/fluffy-buttermilk-pancakes/classic-stack/", StringComparison.Ordinal)).IsTrue();
    }
```

Both types compose `metadata`, so Phase 1's sitemap walk under Home already includes them. The test pins that down.

- [ ] **Step 4: Run the page tests**

```bash
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj
```

Expected: every test passes, including the 4 `RecipePageTests` and the new sitemap test.

If `SeededRecipe_RendersThroughItsController` answers 404, Umbraco did not pick `RecipeController`:
- Check that the class name is exactly `RecipeController`.
- If Phase 1 took the template fallback, check that the node has the `Page` template.

If `RenderedPage` finds no body, save `page.Html` to a file and look at how Phase 1's layout writes the
server-content script.

- [ ] **Step 5: Look at it in both ramps**

```bash
(cd src/KCC.Web && yarn build:all)
cd src/KCC.Web && dotnet watch --non-interactive
```

The dev database was seeded in Task 5. Open `https://localhost:58671/recipes/fluffy-buttermilk-pancakes/` in the
Browser pane, first in the light ramp and then in the dark (toggle in the header). Compare the page with
`docs/replatform/reference/xperience-final/{light,dark}-desktop/recipe.png`. It should show:
- the trail HOME · RECIPES · FLUFFY BUTTERMILK PANCAKES;
- the BREAKFAST eyebrow, "4.3 · 2 reviews" and ADD VARIANT;
- At a glance: 1 variant, 25 min fastest, 25 min average, 0 contributors;
- the Top Variant card and one grid card (Classic Stack, VEGETARIAN), plus the Add Variant card.

The ADD VARIANT link goes to `/recipes/add-variant/?recipe=<key>`, which still answers 404 until Phase 4. Stop the
site.

- [ ] **Step 6: Commit**

```bash
git add -A src/KCC.Web/Features/Pages/RecipeDetail src/KCC.Web/KCC.Web.csproj tests/KCC.UnitTests tests/KCC.IntegrationTests
git commit -m "Serve Recipe Pages from Umbraco"
```

---

### Task 9: Read APIs for reviews and cook notes

The variant page's review and cook-note panels fetch their lists client-side, from
`GET /api/variant/{variantGuid}/reviews` and `GET /api/variant/{variantGuid}/notes`. This task brings those two
GETs back, in the exact shapes that `Features/Types/Recipe.ts` reads: `ReviewsResponse` and `CookNotesResponse`,
serialized in camelCase. The write endpoints come in Phase 4:
- `PUT` and `DELETE /review`
- `POST` and `DELETE /note`
- the `/cooked` pair

Phase 4 adds them to these same controllers. It ports their logic from
`git show xperience-final:src/KCC.Web/Features/Api/ReviewApiController.cs` (and its siblings), and their tests from
the still-excluded `tests/KCC.UnitTests/Features/Api/*ApiControllerTests.cs`.

**Files:**
- Create:
  - `src/KCC.Web/Features/Api/ContributionResponses.cs`
  - `tests/KCC.UnitTests/Features/Api/{ReviewListTests,CookNoteListTests}.cs`
  - `tests/KCC.IntegrationTests/Features/Api/ContributionReadApiTests.cs`
- Rewrite, GET only: `src/KCC.Web/Features/Api/{ReviewApiController,CookNoteApiController}.cs`
- Modify:
  - `src/KCC.Web/KCC.Web.csproj`: replace `Features/Api/**` with the five Kentico controllers left.
  - `tests/KCC.UnitTests/KCC.UnitTests.csproj`: replace `Features/Api/**` with the five Kentico test files.

**Interfaces:**
- Consumes: `IContributionStats`, `IContributionReads`, `ContributionReads.MaxPageSize` (Task 3),
  `IAuthorNameProvider`, and `AuthorNameProvider.DeletedMemberName` (Task 4).
- Produces (namespace `KCC.Web.Features.Api`):
  - `GET /api/variant/{variantGuid:guid}/reviews?page=0&pageSize=10` → `ReviewsResponse(double Average, int Count, IReadOnlyList<int> Distribution, int Total, int Page, int PageSize, IReadOnlyList<ReviewItem> Reviews, MyReview MyReview)`,
    where:
    - `ReviewItem(string AuthorName, decimal Rating, string Text, DateTime Created, bool IsMine)`
    - `MyReview(decimal Rating, string Text)`, which is null when signed out or not reviewed
  - `GET /api/variant/{variantGuid:guid}/notes?page=0&pageSize=10` → `CookNotesResponse(int Total, int Page, int PageSize, IReadOnlyList<CookNoteItem> Notes)`,
    where `CookNoteItem(int Id, string AuthorName, string Text, DateTime Created, bool IsMine)`.
  - Newest first. A reviewer with no member shows as `(deleted)`. `Page` and `PageSize` report the clamped values
    that were served.

- [ ] **Step 1: Write the failing controller tests**

In `tests/KCC.UnitTests/KCC.UnitTests.csproj`, replace `<Compile Remove="Features/Api/**" />` with:

```xml
        <Compile Remove="Features/Api/CookNoteApiControllerTests.cs" />
        <Compile Remove="Features/Api/ProfileApiControllerTests.cs" />
        <Compile Remove="Features/Api/RecipeApiControllerTests.cs" />
        <Compile Remove="Features/Api/ReviewApiControllerTests.cs" />
        <Compile Remove="Features/Api/VariantCookedApiControllerTests.cs" />
```

Create `tests/KCC.UnitTests/Features/Api/ReviewListTests.cs`:

```csharp
using KCC.Contributions;
using KCC.Contributions.Data;
using KCC.Web.Features.Api;
using KCC.Web.Features.Providers;
using Moq;
using Umbraco.Cms.Core.Security;

namespace KCC.UnitTests.Features.Api;

public class ReviewListTests
{
    private static readonly Guid VariantKey = Guid.NewGuid();

    [Test]
    public async Task GetReviews_NamesReviewersAndMarksDeletedOnes()
    {
        var priya = Guid.NewGuid();
        var controller = Controller([Review(priya, 5m), Review(Guid.NewGuid(), 4m)], new() { [priya] = "Priya Balan" });

        var response = (await controller.GetReviews(VariantKey)).Value;

        _ = await Assert.That(string.Join(",", response.Reviews.Select(review => review.AuthorName))).IsEqualTo("Priya Balan,(deleted)");
    }

    [Test]
    public async Task GetReviews_SignedOut_HasNoOwnReviewAndMarksNothingMine()
    {
        var controller = Controller([Review(Guid.NewGuid(), 5m)], []);

        var response = (await controller.GetReviews(VariantKey)).Value;

        _ = await Assert.That(response.MyReview).IsNull();
        _ = await Assert.That(response.Reviews.Any(review => review.IsMine)).IsFalse();
    }

    [Test]
    public async Task GetReviews_SignedIn_ReturnsAndMarksTheMembersReview()
    {
        var member = Guid.NewGuid();
        var mine = Review(member, 4.5m, "Crispy edges");
        var controller = Controller([mine, Review(Guid.NewGuid(), 3m)], [], member, mine);

        var response = (await controller.GetReviews(VariantKey)).Value;

        _ = await Assert.That(response.MyReview).IsEqualTo(new MyReview(4.5m, "Crispy edges"));
        _ = await Assert.That(string.Join(",", response.Reviews.Select(review => review.IsMine))).IsEqualTo("True,False");
    }

    [Test]
    public async Task GetReviews_ReportsTheVariantsAggregateAndDistribution()
    {
        var controller = Controller([Review(Guid.NewGuid(), 5m), Review(Guid.NewGuid(), 4m)], []);

        var response = (await controller.GetReviews(VariantKey)).Value;

        _ = await Assert.That(response.Average).IsEqualTo(4.5d);
        _ = await Assert.That(response.Count).IsEqualTo(2);
        _ = await Assert.That(string.Join(",", response.Distribution)).IsEqualTo("0,0,0,1,1");
    }

    [Test]
    public async Task GetReviews_ReportsThePageItServed()
    {
        var controller = Controller([], []);

        var response = (await controller.GetReviews(VariantKey, page: -3, pageSize: 500)).Value;

        _ = await Assert.That(response.Page).IsEqualTo(0);
        _ = await Assert.That(response.PageSize).IsEqualTo(ContributionReads.MaxPageSize);
    }

    private static Review Review(Guid memberKey, decimal rating, string text = null) => new()
    {
        VariantKey = VariantKey,
        MemberKey = memberKey,
        Rating = rating,
        Text = text,
        Created = DateTime.UtcNow,
        Modified = DateTime.UtcNow,
    };

    private static ReviewApiController Controller(Review[] reviews, Dictionary<Guid, string> names, Guid? memberKey = null, Review mine = null)
    {
        var stats = new Mock<IContributionStats>();
        stats.Setup(s => s.GetAsync()).ReturnsAsync(ContributionStats.Build(reviews.Select(review => (review.VariantKey, review.Rating)), []));

        var reads = new Mock<IContributionReads>();
        reads.Setup(r => r.ReviewsAsync(VariantKey, It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new Paged<Review>(reviews, reviews.Length));
        reads.Setup(r => r.MemberReviewAsync(VariantKey, It.IsAny<Guid>())).ReturnsAsync(mine);

        var authors = new Mock<IAuthorNameProvider>();
        authors.Setup(a => a.ResolveMany(It.IsAny<IEnumerable<Guid>>())).ReturnsAsync(names);

        var members = new Mock<IMemberManager>();
        members.Setup(m => m.GetCurrentMemberAsync())
            .ReturnsAsync(memberKey is { } key ? new MemberIdentityUser { Key = key } : null);

        return new ReviewApiController(stats.Object, reads.Object, authors.Object, members.Object);
    }
}
```

Create `tests/KCC.UnitTests/Features/Api/CookNoteListTests.cs`:

```csharp
using KCC.Contributions;
using KCC.Contributions.Data;
using KCC.Web.Features.Api;
using KCC.Web.Features.Providers;
using Moq;
using Umbraco.Cms.Core.Security;

namespace KCC.UnitTests.Features.Api;

public class CookNoteListTests
{
    private static readonly Guid VariantKey = Guid.NewGuid();

    [Test]
    public async Task GetNotes_NamesAuthorsAndMarksTheMembersOwn()
    {
        var member = Guid.NewGuid();
        var controller = Controller([Note(1, member, "Use buttermilk"), Note(2, Guid.NewGuid(), "Rest the batter")], new() { [member] = "Priya Balan" }, member);

        var response = (await controller.GetNotes(VariantKey)).Value;

        _ = await Assert.That(string.Join(",", response.Notes.Select(note => note.AuthorName))).IsEqualTo("Priya Balan,(deleted)");
        _ = await Assert.That(string.Join(",", response.Notes.Select(note => note.IsMine))).IsEqualTo("True,False");
        _ = await Assert.That(response.Notes.First().Id).IsEqualTo(1);
    }

    [Test]
    public async Task GetNotes_ReportsTheTotalAndThePageItServed()
    {
        var controller = Controller([Note(1, Guid.NewGuid(), "Rest the batter")], []);

        var response = (await controller.GetNotes(VariantKey, page: 2, pageSize: 0)).Value;

        _ = await Assert.That(response.Total).IsEqualTo(1);
        _ = await Assert.That(response.Page).IsEqualTo(2);
        _ = await Assert.That(response.PageSize).IsEqualTo(1);
    }

    private static CookNote Note(int id, Guid memberKey, string text) => new()
    {
        Id = id,
        VariantKey = VariantKey,
        MemberKey = memberKey,
        Text = text,
        Created = DateTime.UtcNow,
        Modified = DateTime.UtcNow,
    };

    private static CookNoteApiController Controller(CookNote[] notes, Dictionary<Guid, string> names, Guid? memberKey = null)
    {
        var reads = new Mock<IContributionReads>();
        reads.Setup(r => r.NotesAsync(VariantKey, It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new Paged<CookNote>(notes, notes.Length));

        var authors = new Mock<IAuthorNameProvider>();
        authors.Setup(a => a.ResolveMany(It.IsAny<IEnumerable<Guid>>())).ReturnsAsync(names);

        var members = new Mock<IMemberManager>();
        members.Setup(m => m.GetCurrentMemberAsync())
            .ReturnsAsync(memberKey is { } key ? new MemberIdentityUser { Key = key } : null);

        return new CookNoteApiController(reads.Object, authors.Object, members.Object);
    }
}
```

In `src/KCC.Web/KCC.Web.csproj`, replace `<Compile Remove="Features/Api/**" />` with:

```xml
        <Compile Remove="Features/Api/AccountApiController.cs" />
        <Compile Remove="Features/Api/ProfileApiController.cs" />
        <Compile Remove="Features/Api/RecipeApiController.cs" />
        <Compile Remove="Features/Api/RecipeSearchApiController.cs" />
        <Compile Remove="Features/Api/VariantCookedApiController.cs" />
```

```bash
dotnet build KitchenCommandCenter.sln
```

Expected: build errors from the Kentico `ReviewApiController.cs` and `CookNoteApiController.cs`, which are compiled
again. The new tests fail too, because `ReviewsResponse` and `MyReview` are missing.

- [ ] **Step 2: Write the response shapes and the two controllers**

Create `src/KCC.Web/Features/Api/ContributionResponses.cs`:

```csharp
namespace KCC.Web.Features.Api;

public sealed record ReviewsResponse(
    double Average,
    int Count,
    IReadOnlyList<int> Distribution,
    int Total,
    int Page,
    int PageSize,
    IReadOnlyList<ReviewItem> Reviews,
    MyReview MyReview);

public sealed record ReviewItem(string AuthorName, decimal Rating, string Text, DateTime Created, bool IsMine);

public sealed record MyReview(decimal Rating, string Text);

public sealed record CookNotesResponse(int Total, int Page, int PageSize, IReadOnlyList<CookNoteItem> Notes);

public sealed record CookNoteItem(int Id, string AuthorName, string Text, DateTime Created, bool IsMine);
```

Replace `src/KCC.Web/Features/Api/ReviewApiController.cs` with:

```csharp
using KCC.Contributions;
using KCC.Web.Features.Providers;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Security;

namespace KCC.Web.Features.Api;

[ApiController]
[Route("api/variant")]
public class ReviewApiController(
    IContributionStats contributionStats,
    IContributionReads contributionReads,
    IAuthorNameProvider authorNames,
    IMemberManager memberManager) : ControllerBase
{
    [HttpGet("{variantGuid:guid}/reviews")]
    public async Task<ActionResult<ReviewsResponse>> GetReviews(Guid variantGuid, int page = 0, int pageSize = 10)
    {
        var stats = (await contributionStats.GetAsync()).For(variantGuid);
        var memberKey = (await memberManager.GetCurrentMemberAsync())?.Key;
        var reviews = await contributionReads.ReviewsAsync(variantGuid, page, pageSize);
        var names = await authorNames.ResolveMany(reviews.Items.Select(review => review.MemberKey));
        var mine = memberKey is { } key ? await contributionReads.MemberReviewAsync(variantGuid, key) : null;

        return new ReviewsResponse(
            stats.Rating.Average,
            stats.Rating.Count,
            stats.Distribution,
            reviews.Total,
            Math.Max(0, page),
            Math.Clamp(pageSize, 1, ContributionReads.MaxPageSize),
            reviews.Items.Select(review => new ReviewItem(
                names.GetValueOrDefault(review.MemberKey) ?? AuthorNameProvider.DeletedMemberName,
                review.Rating,
                review.Text,
                review.Created,
                review.MemberKey == memberKey)).ToList(),
            mine is null ? null : new MyReview(mine.Rating, mine.Text));
    }
}
```

Replace `src/KCC.Web/Features/Api/CookNoteApiController.cs` with:

```csharp
using KCC.Contributions;
using KCC.Web.Features.Providers;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Security;

namespace KCC.Web.Features.Api;

[ApiController]
[Route("api")]
public class CookNoteApiController(
    IContributionReads contributionReads,
    IAuthorNameProvider authorNames,
    IMemberManager memberManager) : ControllerBase
{
    [HttpGet("variant/{variantGuid:guid}/notes")]
    public async Task<ActionResult<CookNotesResponse>> GetNotes(Guid variantGuid, int page = 0, int pageSize = 10)
    {
        var notes = await contributionReads.NotesAsync(variantGuid, page, pageSize);
        var names = await authorNames.ResolveMany(notes.Items.Select(note => note.MemberKey));
        var memberKey = (await memberManager.GetCurrentMemberAsync())?.Key;

        return new CookNotesResponse(
            notes.Total,
            Math.Max(0, page),
            Math.Clamp(pageSize, 1, ContributionReads.MaxPageSize),
            notes.Items.Select(note => new CookNoteItem(
                note.Id,
                names.GetValueOrDefault(note.MemberKey) ?? AuthorNameProvider.DeletedMemberName,
                note.Text,
                note.Created,
                note.MemberKey == memberKey)).ToList());
    }
}
```

The routes and prefixes are the Kentico controllers', so Phase 4's writes slot in beside these actions without
moving anything.

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj
```

Expected: 0 warnings. Every unit test passes, including the 5 `ReviewListTests` and the 2 `CookNoteListTests`.

- [ ] **Step 3: Check the wire format against the seeded data**

Create `tests/KCC.IntegrationTests/Features/Api/ContributionReadApiTests.cs`:

```csharp
using System.Text.Json;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.DevTools.RecipeSeed;

namespace KCC.IntegrationTests.Features.Api;

public class ContributionReadApiTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task Reviews_AnswerInTheShapeVariantReviewsReads()
    {
        using var client = Site.CreateClient();
        var variantKey = SeedKeys.Variant("Fluffy Buttermilk Pancakes", "Classic Stack");

        using var json = JsonDocument.Parse(await client.GetStringAsync($"/api/variant/{variantKey}/reviews?page=0&pageSize=10"));
        var root = json.RootElement;
        var newest = root.GetProperty("reviews")[0];

        _ = await Assert.That(root.GetProperty("average").GetDouble()).IsEqualTo(4.25d);
        _ = await Assert.That(root.GetProperty("count").GetInt32()).IsEqualTo(2);
        _ = await Assert.That(root.GetProperty("distribution").ToString()).IsEqualTo("[0,0,0,2,0]");
        _ = await Assert.That(root.GetProperty("total").GetInt32()).IsEqualTo(2);
        _ = await Assert.That(newest.GetProperty("text").GetString()).IsEqualTo("Seeded review #2");
        _ = await Assert.That(newest.GetProperty("authorName").GetString()).IsEqualTo("(deleted)");
        _ = await Assert.That(newest.GetProperty("isMine").GetBoolean()).IsFalse();
        _ = await Assert.That(newest.GetProperty("created").GetString()).EndsWith("Z");
        _ = await Assert.That(root.GetProperty("myReview").ValueKind).IsEqualTo(JsonValueKind.Null);
    }

    [Test]
    public async Task Notes_AnswerInTheShapeVariantCookNotesReads()
    {
        using var client = Site.CreateClient();
        var variantKey = SeedKeys.Variant("Fluffy Buttermilk Pancakes", "Classic Stack");

        using var json = JsonDocument.Parse(await client.GetStringAsync($"/api/variant/{variantKey}/notes?page=0&pageSize=10"));
        var root = json.RootElement;

        _ = await Assert.That(root.GetProperty("total").GetInt32()).IsEqualTo(0);
        _ = await Assert.That(root.GetProperty("pageSize").GetInt32()).IsEqualTo(10);
        _ = await Assert.That(root.GetProperty("notes").GetArrayLength()).IsEqualTo(0);
    }
}
```

The reference screenshot shows both seeded reviews in the 4★ bar, which is what `[0,0,0,2,0]` asserts: 4.5 rounds
down to 4. The newest-first order puts review #2 on top. The trailing `Z` is Task 1's UTC converter, so the browser
prints the date in the reader's own time zone.

```bash
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/ContributionReadApiTests/*"
```

Expected: 2 passed.

- [ ] **Step 4: Commit**

```bash
git add -A src/KCC.Web/Features/Api src/KCC.Web/KCC.Web.csproj tests/KCC.UnitTests tests/KCC.IntegrationTests
git commit -m "Serve Review and Cook Note Lists from the Contributions Store"
```

---

### Task 10: The variant page, with a working cover image

The Kentico `VariantDetailController` becomes the hijacking `RecipeVariantController`, the same way the recipe page
moved in Task 8.

Spec §17 lists a variant cover-image defect: the image never rendered, because the Vue view read `Asset.Url`
(PascalCase) from camelCase JSON. This task fixes it by changing the contract. The server now sends one URL, already
sized, as `cover-image`, and the Kentico-only `ImageItem` type goes.

`VariantViewModelTests` compiles again unchanged.

**Files:**
- Move and rewrite: `src/KCC.Web/Features/Pages/VariantDetail/VariantController.cs` → `RecipeVariantController.cs`
- Create:
  - `src/KCC.Web/Features/Pages/VariantDetail/VariantDetailMapping.cs`
  - `tests/KCC.UnitTests/Features/Pages/VariantDetail/VariantDetailMappingTests.cs`
  - `tests/KCC.IntegrationTests/Features/Pages/VariantPageTests.cs`
- Modify:
  - `src/KCC.Web/Features/Pages/VariantDetail/{VariantDetailViewModel.cs,Index.cshtml,VariantDetailView.Component.vue}`
  - `tests/KCC.ViteTests/Features/Pages/VariantDetail/VariantDetailView.test.ts`
  - `src/KCC.Web/KCC.Web.csproj` (delete the two `VariantDetail` lines)
  - `tests/KCC.UnitTests/KCC.UnitTests.csproj` (delete `Features/Pages/VariantDetail/**`)
- Delete: `src/KCC.Web/Features/Types/ContentTypes.ts`

**Interfaces:**
- Consumes:
  - `IRecipeQueries` and `VariantPageData` (Task 6)
  - `IContributionStats` and `IContributionReads.HasCookedAsync` (Task 3)
  - `IAuthorNameProvider` (Task 4)
  - `BreadcrumbService` (Task 7)
  - `PageMetadata` and `IResourceStringProvider` (Phase 1)
  - `IMemberManager.GetCurrentMemberAsync()` (namespace `Umbraco.Cms.Core.Security`), which returns null when no
    one is signed in
- Produces:
  - `VariantDetailMapping.AuthorKeys(VariantPageData) → IEnumerable<Guid>`.
  - `VariantDetailMapping.Map(VariantPageData page, ContributionStats stats, IReadOnlyDictionary<Guid, string> authorNames, bool hasCooked, bool isAuthenticated) → VariantDetailViewModel`.
  - `VariantDetailViewModel.CoverImage` (`string`) replaces `Images`, and the Vue prop `coverImage?: string`
    replaces `images`.

- [ ] **Step 1: Write the failing Vitest case**

In `tests/KCC.ViteTests/Features/Pages/VariantDetail/VariantDetailView.test.ts`, add this case after
`it('carries the recipe name in the hero eyebrow', …)`:

```ts
  it('shows the cover image in the hero tile', async () => {
    const html = await render({ coverImage: '/media/gnocchi.webp' })

    expect(html).toContain('src="/media/gnocchi.webp"')
    expect(html).toContain('alt="Crispy Edge Gnocchi"')
  })
```

```bash
cd src/KCC.Web && yarn test VariantDetailView
```

Expected: the new case FAILS, because `coverImage` is not a prop yet and no `<img>` renders. The other cases pass.

- [ ] **Step 2: Take the sized URL in the Vue view**

In `src/KCC.Web/Features/Pages/VariantDetail/VariantDetailView.Component.vue`, make three edits:

1. Delete the line `  import type { ImageItem } from '~/Types/ContentTypes'`.
2. Replace the `images` prop and its comment:

   ```ts
       /**
        * Only the first is used, as the hero tile.
        */
       images?: ImageItem[]
   ```

   with:

   ```ts
       /**
        * Already sized for the hero tile by the server.
        */
       coverImage?: string
   ```

3. Delete the line `  const coverImage = computed(() => props.images?.[0]?.Asset?.Url)` and the blank line after it.
   The template's `:image="coverImage"` now reads the prop.

Delete the type file, whose only importer was that line:

```bash
git rm -q src/KCC.Web/Features/Types/ContentTypes.ts
grep -rn --include='*.ts' --include='*.vue' "ContentTypes'\|ImageItem\|Asset?.Url" src/KCC.Web/Features tests/KCC.ViteTests || echo clean
cd src/KCC.Web && yarn test VariantDetailView && yarn type-check
```

Expected: `clean`, the case passes, and the type check is clean. The C# view model still names `ImageItem` until
Step 4 changes it.

- [ ] **Step 3: Write the failing mapping tests**

In `tests/KCC.UnitTests/KCC.UnitTests.csproj`, delete `<Compile Remove="Features/Pages/VariantDetail/**" />`.

Create `tests/KCC.UnitTests/Features/Pages/VariantDetail/VariantDetailMappingTests.cs`:

```csharp
using KCC.Contributions;
using KCC.Web.Features.Pages.VariantDetail;
using KCC.Web.Features.Recipes;

namespace KCC.UnitTests.Features.Pages.VariantDetail;

public class VariantDetailMappingTests
{
    private static readonly IReadOnlyDictionary<Guid, string> NoNames = new Dictionary<Guid, string>();

    [Test]
    public async Task Map_ReadsTheIngredientsAndInstructions()
    {
        var variant = Variant() with
        {
            IngredientsJson = """[{"name":"Flour","quantity":2,"unit":"cups","isEyeballed":false}]""",
            InstructionsJson = """[{"step":1,"text":"Whisk the batter."}]""",
        };

        var viewModel = Map(variant);

        _ = await Assert.That(viewModel.Ingredients.Single().Name).IsEqualTo("Flour");
        _ = await Assert.That(viewModel.Ingredients.Single().Quantity).IsEqualTo(2m);
        _ = await Assert.That(viewModel.Instructions.Single().Text).IsEqualTo("Whisk the batter.");
    }

    [Test]
    public async Task Map_MalformedIngredients_ShowsNone()
    {
        var viewModel = Map(Variant() with { IngredientsJson = "[{\"name\": \"Flour\"" });

        _ = await Assert.That(viewModel.Ingredients.Count()).IsEqualTo(0);
    }

    [Test]
    public async Task Map_KeepsUnsetNutritionUnset()
    {
        var viewModel = Map(Variant() with { Nutrition = new NutritionRecord(250, null, null, null, null, null, null, 0) });

        _ = await Assert.That(viewModel.Calories).IsEqualTo(250);
        _ = await Assert.That(viewModel.ProteinG).IsNull();
        _ = await Assert.That(viewModel.SodiumMg).IsEqualTo(0);
    }

    [Test]
    public async Task Map_RatesTheVariantAndEachSiblingByTheirOwnReviews()
    {
        var variant = Variant();
        var sibling = Variant("Blueberry Stack");
        var stats = ContributionStats.Build([(variant.Key, 5m), (sibling.Key, 3m), (sibling.Key, 4m)], [variant.Key]);
        var page = new VariantPageData(variant, Recipe(), [sibling]);

        var viewModel = VariantDetailMapping.Map(page, stats, NoNames, hasCooked: false, isAuthenticated: false);

        _ = await Assert.That(viewModel.AverageRating).IsEqualTo(5d);
        _ = await Assert.That(viewModel.ReviewCount).IsEqualTo(1);
        _ = await Assert.That(viewModel.CookedCount).IsEqualTo(1);
        _ = await Assert.That(viewModel.SiblingVariants.Single().Rating).IsEqualTo(3.5d);
        _ = await Assert.That(viewModel.SiblingVariants.Single().TotalTime).IsEqualTo(25);
    }

    [Test]
    public async Task Map_CarriesTheCoverImageTheRecipeAndTheSignedInState()
    {
        var variant = Variant() with { ImageUrl = "/media/stack.jpg?width=192" };
        var page = new VariantPageData(variant, Recipe(), []);

        var viewModel = VariantDetailMapping.Map(page, ContributionStats.Build([], []), NoNames, hasCooked: true, isAuthenticated: true);

        _ = await Assert.That(viewModel.CoverImage).IsEqualTo("/media/stack.jpg?width=192");
        _ = await Assert.That(viewModel.RecipeName).IsEqualTo("Fluffy Buttermilk Pancakes");
        _ = await Assert.That(viewModel.RecipeSlug).IsEqualTo("/recipes/fluffy-buttermilk-pancakes/");
        _ = await Assert.That(viewModel.VariantGuid).IsEqualTo(variant.Key);
        _ = await Assert.That(viewModel.HasCooked).IsTrue();
        _ = await Assert.That(viewModel.IsAuthenticated).IsTrue();
    }

    [Test]
    public async Task Map_NamesTheVariantAuthor()
    {
        var diego = Guid.NewGuid();
        var page = new VariantPageData(Variant() with { AuthorKey = diego }, Recipe(), []);

        var viewModel = VariantDetailMapping.Map(page, ContributionStats.Build([], []), new Dictionary<Guid, string> { [diego] = "Diego Salazar" }, false, false);

        _ = await Assert.That(viewModel.CreatedByName).IsEqualTo("Diego Salazar");
        _ = await Assert.That(string.Join(",", VariantDetailMapping.AuthorKeys(page))).IsEqualTo(diego.ToString());
    }

    private static VariantDetailViewModel Map(VariantRecord variant) =>
        VariantDetailMapping.Map(new VariantPageData(variant, Recipe(), []), ContributionStats.Build([], []), NoNames, false, false);

    private static RecipeRecord Recipe() => new(
        Guid.NewGuid(),
        "Fluffy Buttermilk Pancakes",
        "/recipes/fluffy-buttermilk-pancakes/",
        "Tall, tender stacks.",
        "fa-duotone fa-pancakes",
        null,
        "Breakfast",
        null,
        new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc));

    private static VariantRecord Variant(string name = "Classic Stack") => new(
        Guid.NewGuid(),
        name,
        $"/recipes/fluffy-buttermilk-pancakes/{name.ToLowerInvariant().Replace(' ', '-')}/",
        "Griddle to golden.",
        "fa-duotone fa-pancakes",
        null,
        10,
        15,
        4,
        null,
        new NutritionRecord(null, null, null, null, null, null, null, null),
        ["Vegetarian"],
        "[]",
        "[]",
        null,
        new DateTime(2026, 9, 20, 0, 1, 0, DateTimeKind.Utc));
}
```

```bash
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj -- --treenode-filter "/*/*/VariantDetailMappingTests/*"
```

Expected: build errors, because `VariantDetailMapping` is missing and the variant slice is still excluded from KCC.Web.

- [ ] **Step 4: Bring the slice back and write the mapping**

In `src/KCC.Web/KCC.Web.csproj`, delete `<Compile Remove="Features/Pages/VariantDetail/**" />` and
`<Content Remove="Features/Pages/VariantDetail/**/*.cshtml" />`. Then move the controller:

```bash
git mv src/KCC.Web/Features/Pages/VariantDetail/VariantController.cs src/KCC.Web/Features/Pages/VariantDetail/RecipeVariantController.cs
```

Make two edits to the view model and view:
- In `src/KCC.Web/Features/Pages/VariantDetail/VariantDetailViewModel.cs`, replace
  `public IEnumerable<ImageItem> Images { get; set; } = [];` with `public string CoverImage { get; set; }`.
- In `src/KCC.Web/Features/Pages/VariantDetail/Index.cshtml`, replace `:images="@Vue.Prop(Model.Images)"` with
  `cover-image="@Model.CoverImage"`. With no image the value is null, and Razor leaves the attribute out.

Create `src/KCC.Web/Features/Pages/VariantDetail/VariantDetailMapping.cs`:

```csharp
using KCC.Contributions;
using KCC.Web.Features.Helpers;
using KCC.Web.Features.Providers;
using KCC.Web.Features.Recipes;

namespace KCC.Web.Features.Pages.VariantDetail;

public static class VariantDetailMapping
{
    public static IEnumerable<Guid> AuthorKeys(VariantPageData page) =>
        new[] { page.Variant.AuthorKey }.OfType<Guid>();

    public static VariantDetailViewModel Map(
        VariantPageData page,
        ContributionStats stats,
        IReadOnlyDictionary<Guid, string> authorNames,
        bool hasCooked,
        bool isAuthenticated)
    {
        var variant = page.Variant;
        var variantStats = stats.For(variant.Key);

        return new VariantDetailViewModel
        {
            VariantName = variant.Name,
            VariantDescription = variant.Description,
            Icon = variant.Icon,
            CoverImage = variant.ImageUrl,
            PrepTime = variant.PrepTime,
            CookTime = variant.CookTime,
            Servings = variant.Servings,
            Difficulty = variant.Difficulty,
            Calories = variant.Nutrition.Calories,
            ProteinG = variant.Nutrition.ProteinG,
            CarbsG = variant.Nutrition.CarbsG,
            FatG = variant.Nutrition.FatG,
            SaturatedFatG = variant.Nutrition.SaturatedFatG,
            FiberG = variant.Nutrition.FiberG,
            SugarG = variant.Nutrition.SugarG,
            SodiumMg = variant.Nutrition.SodiumMg,
            Tags = variant.Tags,
            Ingredients = Collection<IngredientViewModel>(variant.IngredientsJson),
            Instructions = Collection<InstructionViewModel>(variant.InstructionsJson),
            VariantSlug = variant.Url,
            RecipeName = page.Recipe.Name,
            RecipeSlug = page.Recipe.Url,
            CreatedByName = AuthorNameProvider.NameFor(authorNames, variant.AuthorKey),
            VariantGuid = variant.Key,
            AverageRating = variantStats.Rating.Average,
            ReviewCount = variantStats.Rating.Count,
            CookedCount = variantStats.CookedCount,
            HasCooked = hasCooked,
            IsAuthenticated = isAuthenticated,
            SiblingVariants = page.Siblings.Select(sibling => new SiblingVariantViewModel
            {
                Name = sibling.Name,
                Slug = sibling.Url,
                Icon = sibling.Icon,
                Rating = stats.For(sibling.Key).Rating.Average,
                TotalTime = sibling.TotalTime,
            }).ToList(),
        };
    }

    // The owner can type this JSON by hand in the backoffice, and a slip should empty the list rather than
    // take the page down.
    private static IEnumerable<T> Collection<T>(string json)
    {
        try
        {
            return JsonSerializer.DeserializeCollection<T>(json);
        }
        catch (System.Text.Json.JsonException)
        {
            return [];
        }
    }
}
```

`JsonSerializer` here is the repo's `KCC.Web.Features.Helpers.JsonSerializer`, which reads camelCase and returns an
empty list for blank JSON. That is why the catch names `System.Text.Json.JsonException` in full.

Replace the contents of `src/KCC.Web/Features/Pages/VariantDetail/RecipeVariantController.cs` with the file below.
The `GetStrings` list is the Kentico controller's, unchanged:

```csharp
using KCC.Contributions;
using KCC.Web.Features.Components.Breadcrumbs;
using KCC.Web.Features.Dictionary;
using KCC.Web.Features.Models.Generated;
using KCC.Web.Features.Pages.Shared;
using KCC.Web.Features.Providers;
using KCC.Web.Features.Recipes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace KCC.Web.Features.Pages.VariantDetail;

public class RecipeVariantController(
    ILogger<RenderController> logger,
    ICompositeViewEngine compositeViewEngine,
    IUmbracoContextAccessor umbracoContextAccessor,
    IRecipeQueries recipes,
    IContributionStats contributionStats,
    IContributionReads contributionReads,
    IAuthorNameProvider authorNames,
    IMemberManager memberManager,
    BreadcrumbService breadcrumbs,
    IResourceStringProvider resourceStrings,
    PageMetadata pageMetadata)
    : RenderController(logger, compositeViewEngine, umbracoContextAccessor)
{
    // Route hijacking calls the synchronous Index unless it is hidden like this; the overload below serves the page.
    [NonAction]
    public sealed override IActionResult Index() => throw new NotSupportedException();

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (CurrentPage is not RecipeVariant variant || recipes.GetVariantPage(variant) is not { } page)
        {
            return NotFound();
        }

        var member = await memberManager.GetCurrentMemberAsync();
        var viewModel = VariantDetailMapping.Map(
            page,
            await contributionStats.GetAsync(),
            await authorNames.ResolveMany(VariantDetailMapping.AuthorKeys(page)),
            hasCooked: member is not null && await contributionReads.HasCookedAsync(variant.Key, member.Key),
            isAuthenticated: member is not null);
        viewModel.Breadcrumbs = breadcrumbs.Build(variant);
        viewModel.ResourceStrings = GetStrings();
        pageMetadata.Apply(variant, viewModel);

        return View("~/Features/Pages/VariantDetail/Index.cshtml", viewModel);
    }

    private Dictionary<string, string> GetStrings() => resourceStrings.GetManyOrDefault(
        "VariantDetail.Ingredients",
        "VariantDetail.VariantOf",
        "VariantDetail.By",
        "VariantDetail.CookMode",
        "VariantDetail.Close",
        "VariantDetail.Next",
        "VariantDetail.Previous",
        "VariantDetail.Step",
        "VariantDetail.Of",
        "VariantDetail.MarkDone",
        "VariantDetail.Done",
        "VariantDetail.Servings",
        "VariantDetail.StartTimer",
        "VariantDetail.Pause",
        "VariantDetail.Reset",
        "VariantDetail.ComingSoon",
        "VariantDetail.Prep",
        "VariantDetail.Cook",
        "VariantDetail.Count",
        "VariantDetail.Difficulty",
        "VariantDetail.DifficultyEasy",
        "VariantDetail.DifficultyMedium",
        "VariantDetail.DifficultyHard",
        "VariantDetail.Makes",
        "VariantDetail.Fewer",
        "VariantDetail.More",
        "VariantDetail.ToTaste",
        "VariantDetail.Nutrition",
        "VariantDetail.PerServing",
        "VariantDetail.Calories",
        "VariantDetail.Protein",
        "VariantDetail.Carbs",
        "VariantDetail.Fat",
        "VariantDetail.SaturatedFat",
        "VariantDetail.Fiber",
        "VariantDetail.Sugar",
        "VariantDetail.Sodium",
        "VariantDetail.NutritionNotProvided",
        "VariantDetail.Instructions",
        "VariantDetail.CookNotes",
        "VariantDetail.CookNotesComingSoon",
        "VariantDetail.RatingsReviews",
        "VariantDetail.ReviewsComingSoon",
        "VariantDetail.OtherVariants",
        "VariantDetail.WriteReview",
        "VariantDetail.YourReview",
        "VariantDetail.SubmitReview",
        "VariantDetail.EditReview",
        "VariantDetail.DeleteReview",
        "VariantDetail.LogInToReview",
        "VariantDetail.NoReviewsYet",
        "VariantDetail.ReviewCount",
        "VariantDetail.AddCookNote",
        "VariantDetail.CookNotePlaceholder",
        "VariantDetail.NoCookNotesYet",
        "VariantDetail.DeleteNote",
        "VariantDetail.ICookedThis",
        "VariantDetail.CookedCount",
        "VariantDetail.LoadMore",
        "VariantDetail.Reviews",
        "VariantDetail.NoRatingsYet",
        "VariantDetail.TimesCooked");
}
```

Until Phase 4 no member can sign in, so `isAuthenticated` is always false and the page shows "Log in to write a
review." The flag is still read here, so Phase 4 does not have to touch the controller.

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj
```

Expected: 0 warnings. Every unit test passes, including the 6 `VariantDetailMappingTests` and the existing
`VariantViewModelTests`.

- [ ] **Step 5: Write the page tests**

Create `tests/KCC.IntegrationTests/Features/Pages/VariantPageTests.cs`:

```csharp
using System.Net;
using System.Text.Json;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.DevTools.RecipeSeed;
using KCC.Web.Features.Recipes;

namespace KCC.IntegrationTests.Features.Pages;

public class VariantPageTests
{
    private const string ShoyuPath = "/recipes/spicy-ramen-flight/chili-oil-shoyu/";

    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task SeededVariant_RendersItsMethod()
    {
        using var client = Site.CreateClient();

        var page = await RenderedPage.GetAsync(client, ShoyuPath);
        var ingredients = page.Prop("ingredients").EnumerateArray().Select(ingredient => ingredient.GetProperty("name").GetString());

        _ = await Assert.That(page.Status).IsEqualTo(HttpStatusCode.OK);
        _ = await Assert.That(page.Html.Contains("<title>Chili Oil Shoyu</title>", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(page.Attribute("variant-name")).IsEqualTo("Chili Oil Shoyu");
        _ = await Assert.That(page.Attribute("created-by-name")).IsEqualTo("Priya Balan");
        _ = await Assert.That(page.Attribute("recipe-name")).IsEqualTo("Spicy Ramen Flight");
        _ = await Assert.That(page.Attribute("recipe-slug")).IsEqualTo("/recipes/spicy-ramen-flight/");
        _ = await Assert.That(page.Attribute("difficulty")).IsNull();
        _ = await Assert.That(string.Join(",", ingredients)).IsEqualTo("Ramen Noodles,Chili Oil");
        _ = await Assert.That(page.Prop("instructions").GetArrayLength()).IsEqualTo(2);
        _ = await Assert.That(page.Prop("tags").ToString()).IsEqualTo("[\"Spicy\"]");
        _ = await Assert.That(page.Prop("calories").ValueKind).IsEqualTo(JsonValueKind.Null);
    }

    [Test]
    public async Task SeededVariant_CarriesItsRatingAndSiblings()
    {
        using var client = Site.CreateClient();

        var page = await RenderedPage.GetAsync(client, ShoyuPath);
        var siblings = page.Prop("sibling-variants").EnumerateArray().Select(sibling => sibling.GetProperty("name").GetString()).ToList();

        _ = await Assert.That(page.Prop("average-rating").GetDouble()).IsEqualTo(4.5d);
        _ = await Assert.That(page.Prop("review-count").GetInt32()).IsEqualTo(3);
        _ = await Assert.That(page.Prop("cooked-count").GetInt32()).IsEqualTo(0);
        _ = await Assert.That(page.Prop("has-cooked").GetBoolean()).IsFalse();
        _ = await Assert.That(page.Prop("is-authenticated").GetBoolean()).IsFalse();
        _ = await Assert.That(page.Prop("variant-guid").GetString()).IsEqualTo(SeedKeys.Variant("Spicy Ramen Flight", "Chili Oil Shoyu").ToString());
        _ = await Assert.That(siblings.Count).IsEqualTo(3);
        _ = await Assert.That(siblings.Contains("Chili Oil Shoyu")).IsFalse();
    }

    [Test]
    public async Task VariantImage_IsTheSizedCoverTile()
    {
        var imageKey = await TestContent.ImageAsync(Site.Services, "IT Bowl photo");
        var recipeKey = await TestContent.RecipeAsync(Site.Services, "IT Pictured Bowl");
        await TestContent.VariantAsync(Site.Services, recipeKey, "Glazed", TestContent.Image("images", imageKey));
        using var client = Site.CreateClient();

        var page = await RenderedPage.GetAsync(client, "/recipes/it-pictured-bowl/glazed/");
        var cover = page.Attribute("cover-image");

        _ = await Assert.That(cover).IsNotNull();
        _ = await Assert.That(cover!.Contains($"width={RecipeImages.TileSize}&height={RecipeImages.TileSize}", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(cover.Contains("format=webp", StringComparison.Ordinal)).IsTrue();
    }
}
```

`Prop("calories")` is `null` rather than `0`, which is Task 6's empty-integer rule reaching the page.

```bash
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj
```

Expected: every test passes, including the 3 `VariantPageTests`.

- [ ] **Step 6: Look at it in both ramps**

```bash
(cd src/KCC.Web && yarn build:all)
cd src/KCC.Web && dotnet watch --non-interactive
```

Open `https://localhost:58671/recipes/fluffy-buttermilk-pancakes/classic-stack/` in the Browser pane, in the light
and dark ramps. Compare it with `docs/replatform/reference/xperience-final/{light,dark}-desktop/variant.png`. It should
show:
- the four-crumb trail and Cook Mode;
- VARIANT OF FLUFFY BUTTERMILK PANCAKES, "4.3 · 2 reviews" and the VEGETARIAN badge;
- Prep 10, Cook 15 and Servings 4, with no difficulty;
- the three ingredients and the two steps;
- the nutrition empty state, and "No cook notes yet.";
- the histogram with both reviews at 4★;
- "(deleted) · <date> · Seeded review #2" above "#1", then "Log in to write a review.".

Open Cook Mode and compare it with `cook-mode.png`: STEP 1 OF 2, WHISK THE BATTER., servings 4, the three
ingredients. Stop the site.

- [ ] **Step 7: Commit**

```bash
git add -A src/KCC.Web/Features/Pages/VariantDetail src/KCC.Web/Features/Types src/KCC.Web/KCC.Web.csproj \
  tests/KCC.UnitTests tests/KCC.IntegrationTests tests/KCC.ViteTests
git commit -m "Serve Variant Pages from Umbraco with Sized Cover Images"
```

---

### Task 11: End-to-end checks of the recipe and variant pages

Three E2E suites come back. They move to seeded paths, because the hand-made `/recipes/mac-cheese/…` pages died with
Kentico. The Kentico versions could only assert structure, since they never knew what content would be there. Their
content-dependent checks become exact assertions against the seed, and three changes follow from that:

- **`CookModeButton_IsDisabled_WhenVariantHasNoInstructions` goes.** Every seeded variant has a method, and Vitest
  already covers the disabled pill (`VariantDetailView.test.ts`).
- **The two nutrition-row tests and the difficulty-set test go.** They asserted only what holds for *any* content.
  Vitest covers the filled-in card and the tile (`VariantNutrition.test.ts`, `StatTiles.test.ts`).
- **The "Top Rated" check becomes real.** The old one looked for a *button* named "Top Rated", but the sort
  options are `role="radio"`, so it always returned early. The new check clicks `data-testid="sort-rating"`.

The stale comments about the Vite dev server and `CIRepository` go with them. The E2E site is a production build
started by `SiteProcess`, so a plain `GotoAsync` settles, and nothing needs `[NotInParallel]`.

**Files:**
- Rewrite: `tests/KCC.E2ETests/Features/VariantDetail/{VariantDetailTests,CookModeTests}.cs`,
  `tests/KCC.E2ETests/Features/RecipeRatings/RecipeRatingsTests.cs`
- Modify: `tests/KCC.E2ETests/KCC.E2ETests.csproj` (delete the `Features/RecipeRatings/**` and
  `Features/VariantDetail/**` lines)

**Interfaces:**
- Consumes the seeded facts from Task 5. Classic Stack has three ingredients, two steps, no nutrition and no
  difficulty. Spicy Ramen Flight has four variants, and only Chili Oil Shoyu has reviews.

- [ ] **Step 1: Rewrite the three suites**

In `tests/KCC.E2ETests/KCC.E2ETests.csproj`, delete `<Compile Remove="Features/RecipeRatings/**" />` and
`<Compile Remove="Features/VariantDetail/**" />`.

Replace `tests/KCC.E2ETests/Features/VariantDetail/VariantDetailTests.cs` with:

```csharp
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.VariantDetail;

public class VariantDetailTests : BasePageTests
{
    // Seeded with a two-step method and neither nutrition nor a difficulty, so each check has one right answer.
    // Vitest covers the filled-in nutrition card and the difficulty tile.
    private const string VariantPath = "/recipes/fluffy-buttermilk-pancakes/classic-stack";

    [Test]
    public async Task Variant_ShowsItsIngredientsAndSteps()
    {
        var response = await Page.GotoAsync(VariantPath);

        _ = await Assert.That(response!.Status).IsEqualTo(200);
        await Expect(Page.GetByText("Buttermilk", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByText("Whisk the batter.", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByText("Griddle until bubbles pop.", new() { Exact = true })).ToBeVisibleAsync();
    }

    [Test]
    public async Task NutritionCard_WithNoValues_ShowsTheEmptyState()
    {
        await Page.GotoAsync(VariantPath);

        await Expect(Page.Locator("h2").Filter(new() { HasText = "Nutrition" })).ToBeVisibleAsync();
        await Expect(Page.GetByText("isn't available")).ToBeVisibleAsync();
        await Expect(Page.Locator("dl")).ToHaveCountAsync(0);
    }

    [Test]
    public async Task DifficultyTile_WhenUnset_IsOmitted()
    {
        await Page.GotoAsync(VariantPath);

        await Expect(Page.Locator("[data-testid='variant-stats']")).ToBeVisibleAsync();
        await Expect(Page.Locator("[data-testid='difficulty-dot']")).ToHaveCountAsync(0);
    }
}
```

Replace `tests/KCC.E2ETests/Features/VariantDetail/CookModeTests.cs` with:

```csharp
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.VariantDetail;

public class CookModeTests : BasePageTests
{
    // Seeded with a two-step method. Every seeded variant has one, so the disabled pill is covered in Vitest.
    private const string VariantPath = "/recipes/fluffy-buttermilk-pancakes/classic-stack";

    private async Task OpenOverlayAsync()
    {
        _ = await Page.GotoAsync(VariantPath);
        await Page.Locator("[data-test=\"cook-mode-open-desktop\"]").ClickAsync();
        await Expect(Page.Locator("[role=\"dialog\"]")).ToBeVisibleAsync();
    }

    [Test]
    public async Task CookModeButton_IsEnabled_AndOpensOverlay_WhenVariantHasInstructions()
    {
        _ = await Page.GotoAsync(VariantPath);
        var button = Page.Locator("[data-test=\"cook-mode-open-desktop\"]");

        await Expect(button).ToBeEnabledAsync();
        await button.ClickAsync();

        await Expect(Page.Locator("[role=\"dialog\"]")).ToBeVisibleAsync();
        await Expect(Page.Locator("[data-test=\"cook-progress\"]")).ToContainTextAsync("1");
    }

    [Test]
    public async Task Overlay_ClosesViaCloseButton()
    {
        await OpenOverlayAsync();

        await Page.Locator("[data-test=\"cook-close\"]").ClickAsync();

        await Expect(Page.Locator("[role=\"dialog\"]")).ToBeHiddenAsync();
    }

    [Test]
    public async Task Overlay_ClosesViaEscape()
    {
        await OpenOverlayAsync();

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("[role=\"dialog\"]")).ToBeHiddenAsync();
    }

    [Test]
    public async Task Overlay_TrapsFocusWithinTheDialog()
    {
        await OpenOverlayAsync();

        // Tab a handful of times; focus must never escape the dialog.
        for (var i = 0; i < 6; i++)
        {
            await Page.Keyboard.PressAsync("Tab");
            var focusInsideDialog = await Page.EvaluateAsync<bool>(
                "() => { const d = document.querySelector('[role=\\\"dialog\\\"]'); return !!d && d.contains(document.activeElement); }"
            );
            _ = await Assert.That(focusInsideDialog).IsTrue();
        }
    }

    [Test]
    public async Task Overlay_TrapsFocus_OnInitialShiftTab()
    {
        await OpenOverlayAsync();

        // Regression: on open, focus sits on the tabindex="-1" panel root (excluded from
        // the focusables list). An immediate Shift+Tab — before any forward Tab — must wrap
        // backward to the last focusable and stay inside the dialog, not escape the overlay.
        await Page.Keyboard.PressAsync("Shift+Tab");

        var focusInsideDialog = await Page.EvaluateAsync<bool>(
            "() => { const d = document.querySelector('[role=\\\"dialog\\\"]'); return !!d && d.contains(document.activeElement); }"
        );
        _ = await Assert.That(focusInsideDialog).IsTrue();
    }

    [Test]
    public async Task Steps_AdvanceAndGoBack_WithProgressIndicator()
    {
        await OpenOverlayAsync();

        var progress = Page.Locator("[data-test=\"cook-progress\"]");
        var prev = Page.Locator("[data-test=\"cook-prev\"]");
        var next = Page.Locator("[data-test=\"cook-next\"]");

        // First step: Previous disabled, Next enabled.
        await Expect(prev).ToBeDisabledAsync();
        await Expect(progress).ToContainTextAsync("1");

        await next.ClickAsync();
        await Expect(progress).ToContainTextAsync("2");
        // Last of two steps: Next disabled.
        await Expect(next).ToBeDisabledAsync();

        await prev.ClickAsync();
        await Expect(progress).ToContainTextAsync("1");
    }

    [Test]
    public async Task Step_CanBeCheckedOff()
    {
        await OpenOverlayAsync();
        var check = Page.Locator("[data-test=\"cook-check\"]");

        await Expect(check).ToHaveAttributeAsync("aria-pressed", "false");
        await check.ClickAsync();
        await Expect(check).ToHaveAttributeAsync("aria-pressed", "true");
    }
}
```

The overlay tests and their helper are the Kentico ones, unchanged apart from the path.

Replace `tests/KCC.E2ETests/Features/RecipeRatings/RecipeRatingsTests.cs` with:

```csharp
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.RecipeRatings;

public class RecipeRatingsTests : BasePageTests
{
    // Four seeded variants, created a minute apart, of which only the first, Chili Oil Shoyu, has reviews; the
    // seeder marks nothing cooked. Vitest covers the times-cooked badge when there is a count to show.
    private const string RecipePath = "/recipes/spicy-ramen-flight";

    [Test]
    public async Task RecipeHero_TimesCookedBadge_IsHiddenAtZero()
    {
        await Page.GotoAsync(RecipePath);

        await Expect(Page.Locator("[data-testid='times-cooked']")).ToHaveCountAsync(0);
    }

    [Test]
    public async Task TopRatedSort_PutsTheRatedVariantFirst()
    {
        await Page.GotoAsync(RecipePath);
        var cards = Page.Locator("[data-variant-name]");

        await Expect(cards).ToHaveCountAsync(4);
        await Expect(cards.First).ToHaveAttributeAsync("data-variant-name", "Coconut Dairy-Free");

        await Page.GetByTestId("sort-rating").ClickAsync();

        await Expect(cards.First).ToHaveAttributeAsync("data-variant-name", "Chili Oil Shoyu");
    }
}
```

The Newest sort puts Coconut Dairy-Free first, because it is the last created. The Top Rated sort puts the only rated
variant first. The Top Variant block deliberately carries no `data-variant-name` (see
`Components/Recipe/recipeCardModel.ts`), so the locator counts only the four grid cards.

`Config/RecipeNavigation.cs` stays. The review, note and cooked suites still use it, and they are excluded until
Phase 4, by which time the listing page from Phase 3 lets it work again.

- [ ] **Step 2: Run the E2E suite**

```bash
dotnet build KitchenCommandCenter.sln
(cd src/KCC.Web && yarn build:all)
dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj
```

Expected: 19 passed. That is Phase 1's 7, plus 3 variant, 7 cook-mode and 2 ratings tests. On a failure, the
exception carries the last 40 lines of the run's `site.log`. A 404 on either path means the seeding in `SiteProcess`
did not run.

- [ ] **Step 3: Commit**

```bash
git add -A tests/KCC.E2ETests
git commit -m "Point the Recipe and Variant E2E Tests at Seeded Pages"
```

---

### Task 12: Docs, memory and the Phase 2 gate

**Files:**
- Modify: `README.md`
- Memory, outside the repo, in `~/.claude/projects/-Users-twinright-Repos-Kitchen-Command-Center/memory/`:
  - `recipe-search-test-seeder.md`
  - `recipe-search-deterministic-order.md`
  - `recipe-search-spotlight-hides-grid-card.md`
  - `MEMORY.md`

- [ ] **Step 1: Document the test recipes and the contributions store**

In `README.md`, under **Development Best Practices**, add these two subsections after Phase 1's
**Schema and baseline content**:

````markdown
#### Test recipes

With the site running, seed the test data: 25 recipes, 29 variants, two authors and their reviews.

```bash
curl -sk -X POST https://localhost:58671/api/dev/seed-recipes
```

The endpoint answers only in Development and in the Testing environment the test fixtures use. It skips recipes that
already exist, and it is what the integration and E2E fixtures run before their first test. While seeded recipes
exist, the baseline export refuses to run, so change the baseline from a fresh database.

#### Contributions

Reviews, cook notes and cooked marks are EF Core tables (`kccReview`, `kccCookNote`, `kccCookedMark`) in the same
SQLite file, migrated at startup. Every write runs inside Umbraco's scope and takes `ContributionLocks.Contributions`
before its first read, so it shares Umbraco's single SQLite writer. After changing an entity or
`ContributionsDbContext`, add a migration:

```bash
dotnet tool restore
dotnet dotnet-ef migrations add <Name> --project src/KCC.Contributions --startup-project src/KCC.Web \
  --context ContributionsDbContext --output-dir Data/Migrations
```
````

In the **E2E tests** subsection, replace `and its own SSR process, so nothing needs setting up` with
`and its own SSR process, and seeds the test recipes before the first test, so nothing needs setting up`.

`CLAUDE.md` says nothing that this phase invalidates, so it is unchanged.

- [ ] **Step 2: Commit the docs**

```bash
git add README.md
git commit -m "Document the Test Recipes and the Contributions Store"
```

- [ ] **Step 3: Update the memory this phase invalidated**

Replace the body of `recipe-search-test-seeder.md`. Keep its frontmatter `name` and `type`, and set the
`description` to the first line below:

```markdown
description: Dev/Testing-only seeder — POST /api/dev/seed-recipes on the running app publishes 25 recipes, 29 variants, 2 authors and 27 reviews through Umbraco services; both test fixtures run it before any test

The seeder lives in `src/KCC.Web/Features/DevTools/RecipeSeed/`: `RecipeTestDataSeeder`, `RecipeSeedData`,
`RecipeSeedModels`, `SeedKeys` and `DevSeedApiController`. Since the replatform's Phase 2 (<date>) it runs on
Umbraco's editing, publishing and member services.

Run it against the running dev site: `curl -sk -X POST https://localhost:58671/api/dev/seed-recipes`.
- It answers in Development and in Testing (the environment `UmbracoSite` and `SiteProcess` use), and 404s
  everywhere else.
- It is idempotent by deterministic keys (`SeedKeys`, an MD5 of a seed string), so a second run reports
  `recipes +0 (skipped 25)`.
- There is no reset. To start over, delete the database file.

What it creates:
- Two approved author members, Priya Balan and Diego Salazar, with random passwords.
- 25 published recipes under `/recipes`. Each takes its category from the baseline's "Recipe Categories" folder,
  and its create date is backdated by `PublishedDaysAgo` for the "newest" sorts.
- 29 variants, one minute apart, with tags from "Recipe Tags". They have no nutrition, difficulty or images, so the
  nutrition card always shows its empty state.
- 27 reviews on each recipe's first variant, written through `IContributionWrites`. The reviewer keys have no
  member behind them, so the reviews render as "(deleted)".

Both fixtures seed before any test: `UmbracoSite.InitializeAsync`, and `SiteProcess()` (the session-shared E2E
site). Seeding saves content and members, which must not overlap a test's writes; see
[[umbraco-sqlite-relations-lock-hazard]].

While seeded recipes exist, `POST /api/dev/baseline/export` refuses to run. Export the baseline from a fresh
database.

The data set is still the search coverage matrix that Phase 3's search tests lean on:
- free-text tokens per field, including the un-indexed "zephyr";
- all 6 categories and all 8 diets;
- time edges at 0, 60 and 90 minutes;
- distinct ratings, and 0 to 4 variants per recipe;
- more than 12 recipes, for paging.
```

In `recipe-search-deterministic-order.md`, the rule that "reviews are excluded from CI" no longer holds: every
environment is seeded with reviews now. Replace the clause starting "reviews are excluded from CI so CI seeds are
ALWAYS unrated" with:

> every environment, CI included, is seeded with reviews by the test fixtures (since replatform Phase 2), so rated
> recipes and the spotlight appear everywhere

In `recipe-search-spotlight-hides-grid-card.md`, replace the sentence starting "This is **environment-dependent**"
with:

> Since the replatform's Phase 2 every environment seeds reviews, so a lone rated match is the spotlight everywhere,
> CI included; a test counting `recipe-card` fails in all of them.

In `MEMORY.md`, make three changes:
1. Replace the `recipe-search-test-seeder` line with:

   `- [Recipe test seeder](recipe-search-test-seeder.md) — POST /api/dev/seed-recipes (Development/Testing) publishes 25 recipes, 29 variants, 2 authors, 27 reviews via Umbraco services; idempotent by SeedKeys; both fixtures seed before any test; seeded variants have no nutrition, difficulty or images`

2. In the two search lines, replace `Reviews excluded from CI → CI seeds always unrated/no-spotlight` with
   `every environment seeds reviews since replatform Phase 2`, and
   `(env-dependent: fails locally with seeded reviews, passes in CI without)` with
   `(in every environment since replatform Phase 2)`.
3. Append to the replatform line: `Phase 2 landed on branch replatform (<date>): recipes, variants and contributions
   run on Umbraco + EF Core.`

The `umbraco-sqlite-relations-lock-hazard` memory was written while this plan was drafted, and it stays as it is.

- [ ] **Step 4: The gate — seeded pages match the reference in both ramps**

Stop any site on port 58671, then clone the branch fresh and start it:

```bash
GATE="$(mktemp -d)/kcc-phase-2-gate"
git clone --branch replatform --single-branch /Users/twinright/Repos/Kitchen-Command-Center "$GATE"
cd "$GATE" && yarn install --frozen-lockfile
cd src/KCC.Web && dotnet watch --non-interactive
```

The clone shares this machine's user-secrets through the project's `UserSecretsId`. On its first boot it installs,
imports the schema, dictionary and baseline from uSync, and runs the contributions migration.

Once it serves, run these from the main checkout:

```bash
curl -sk -X POST https://localhost:58671/api/dev/seed-recipes | head -c 200; echo
dotnet run --project tests/KCC.ReferenceCapture -- --only recipe,variant,cook-mode --out .superpowers/reference/umbraco-phase-2
ls .superpowers/reference/umbraco-phase-2/*/ | sort | uniq -c
```

Expected:
- The seed summary reads `recipes +25 (skipped 0), variants +29, reviews +27, authors +2`.
- The capture writes 12 screenshots: `recipe.png`, `variant.png` and `cook-mode.png` in each of `light-desktop`,
  `light-mobile`, `dark-desktop` and `dark-mobile`.

Compare each file with the file of the same name under `docs/replatform/reference/xperience-final/`. They must match
in content and layout.

| Capture | Must match |
|---|---|
| Recipe | The trail; the hero (BREAKFAST, 4.3 · 2 reviews, ADD VARIANT); At a glance (1 · 25 min · 25 min · 0); the Top Variant block; the Classic Stack grid card and the Add Variant card; the toolbar and the tag filters |
| Variant | The four-crumb trail and Cook Mode; the hero with VEGETARIAN; Prep 10 · Cook 15 · Servings 4; the ingredients and the two steps; the nutrition empty state; "No cook notes yet."; the histogram with both reviews in 4★; review #2 above review #1; "Log in to write a review." |
| Cook mode | STEP 1 OF 2, WHISK THE BATTER., servings 4, the three ingredients, and the Previous / Mark Done / Next row |

The only expected difference is the review dates. The reference's seeded reviews read "SEP 1, 2026", while these
read the gate's date. Any other difference is a bug: fix it and re-capture before closing the phase. Stop the gate
site and delete `$GATE`.

- [ ] **Step 5: Everything, once more**

From the main checkout:

```bash
dotnet build KitchenCommandCenter.sln
(cd src/KCC.Web && yarn build:all && yarn test && yarn type-check)
node tests/scripts/run.mjs
```

Expected:
- `Build succeeded` with 0 warnings.
- Both bundles build, and Vitest and the type check pass.
- The combined run is green: unit, integration (one test at a time) and E2E (19). It opens its HTML report when
  it finishes.

- [ ] **Step 6: Close the phase**

Set this file's **Status** line to `done (<date>)`. Phase 3 (Search) is planned next, in this folder, against the code
as it then stands.

## Findings from Phase 2

Found during Phase 2 (2026-09-25). The Phase 3 and 4 plans predate them.

- **Built-in member property keys.** uSync pins Umbraco's built-in member properties to legacy int-based keys
  (`umbracoMemberComments` is `2a280588-0000-0000-0000-000000000000`). Once `member.config` differs from the
  database, as it did when Task 2 added `firstName` and `lastName`, the import writes those keys to the database, and
  editing them in the file makes uSync count a change on every boot. The 17.7 backoffice calls `UmbId.validate` only
  on user ids and in the UFM content- and member-name components, never on a property type's key, so `SchemaTests`
  skips `umbracoMember*` properties. R9 still applies to every other key, including any member property Phase 4 adds.
- **The gate** ran from a fresh clone of branch `replatform-phase-2` on a new database. All 12 captures match the
  reference in content and layout. Besides the review dates, one difference predates this phase: in every mobile
  capture, the torn strip under the header's dashed rule differs at its bottom-left corner (3×4 px at x 18–20,
  y 114–117, by at most 34 levels in light and 26 in dark). Phase 1's home and 404 captures carry the identical patch,
  and Phase 1's gate did not report it. The cause is not traced; `NOTES.md` now lists it.
- **No dev SSR styles.** No Vue component has a `<style>` block (the kit lives in global CSS), so the dev sidecar has
  nothing to inline and pages carry no `<style data-ssr-styles>`. Its absence is not a delivery failure.
- **Fresh-install warnings.** A new database logs five `Configured database is reporting as not being available`
  warnings (SQLite Error 14), one a second, before the unattended install starts: Umbraco's availability check polls
  for a file that does not exist yet. They are expected, and they add about 5 s to each fixture boot.
- **Seeding soon after boot.** The gate seeded about 80 s after its first boot, before Umbraco's first
  cache-instruction sync, and logged Phase 1's cache-instruction stall at the 2-minute mark. It is harmless on one
  server and Phase 4's fix covers it; the test runs finish before that sync.
