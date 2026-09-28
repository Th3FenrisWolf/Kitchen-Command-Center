# Replatform Phase 3 — Search Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or
> superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for
> tracking.

**Status:** done (2026-09-27), on branch `replatform-phase-3`, which is based on `replatform-phase-2`. **Resume
point:** the Phase 4 plan in this folder; read **Findings from Phase 3** at the end of this file first. **Requires
Phase 2 done** (it is, 2026-09-25).

**Goal:** The recipe listing page and `GET /api/recipes/search` answer from a first-party Lucene index held in
memory. The index is rebuilt whole shortly after any content, member or review change, and the search E2E suite is
green.

**Architecture:** The Kentico-bound indexing strategy, reindex resolver and review module are deleted. The
drill-sideways query side survives almost unchanged: `RecipeSearchService` now searches a `RecipeIndex` singleton,
which holds one snapshot — an index and its facet taxonomy, both `RAMDirectory`s — and swaps in a fresh one whole.
A `RecipeIndexRebuilder` hosted service builds each snapshot from the published tree through `IRecipeQueries`, the
contribution stats and the author names. It builds at startup, and again about two seconds after the last of any
burst of signals. The signals come from `ContentCacheRefresherNotification`, member saves and deletes, and a new
`ReviewsChangedNotification` that `ContributionWrites` publishes. The listing page becomes a hijacked
`RecipeListingPageController` over the unchanged view and Vue app.

**Tech Stack:** Umbraco.Cms 17.x (the version Phase 1 pinned). Lucene.Net and Lucene.Net.Facet 4.8.0-beta00018,
which arrive through Umbraco.Cms → Examine 3.10 and are never referenced directly. The .NET 10 `BackgroundService`.
TUnit 1.27 + Moq, Microsoft.AspNetCore.Mvc.Testing, TUnit.Playwright, Vitest.

**Spec:** `docs/replatform/specs/2026-09-21-replatform-off-xperience.md`. Sections: §6.4 (query services), §9.2
(recipe search), §11 (publishing a category or tag rebuilds the index), §14 (testing), §15 (the Phase 3 row and
gates), §17 (the "unpublish a variant, lose the recipe" defect) and §19 row 4 (the cache refresher proof).

## Before you start: reconcile with Phases 1 and 2 as built

This plan was written from Phase 1's and Phase 2's *plans*, while Phase 1 was running. Check each item below. Where
the code differs, adapt the step that depends on it and note the change in your task report.

```bash
grep -n "Status:" docs/replatform/plans/2026-09-23-phase-2-recipes.md
sed -n '/Unported slices/,/<\/ItemGroup>/p' src/KCC.Web/KCC.Web.csproj tests/KCC.UnitTests/KCC.UnitTests.csproj \
  tests/KCC.E2ETests/KCC.E2ETests.csproj
sed -n '/public interface IRecipeQueries/,/^}/p;/public class RecipeQueries/p' src/KCC.Web/Features/Recipes/RecipeQueries.cs
grep -n "record RecipePageData\|record RecipeRecord\|record VariantRecord" src/KCC.Web/Features/Recipes/RecipeRecord.cs
grep -n "new RecipeQueries()\|private T WithContent" tests/KCC.IntegrationTests/Features/Recipes/RecipeQueriesTests.cs
grep -n "class ContributionWrites\|Task .*Async(\|stats.Invalidate()" src/KCC.Contributions/ContributionWrites.cs
grep -n "RatingAcross" src/KCC.Contributions/ContributionStats.cs
grep -n "ResolveMany\|static string NameFor" src/KCC.Web/Features/Providers/AuthorNameProvider.cs
grep -n "public IReadOnlyList<BreadcrumbLink> Build" src/KCC.Web/Features/Components/Breadcrumbs/BreadcrumbService.cs
grep -n "public void Apply" src/KCC.Web/Features/Pages/Shared/PageMetadata.cs
grep -n "SeedRecipes\|RunAsync" src/KCC.Web/Features/DevTools/RecipeSeed/DevSeedApiController.cs
grep -n "ConfigureWebHost\|UseSetting\|Settings() =>" tests/KCC.IntegrationTests/Config/UmbracoSite.cs
grep -n "public static\|PublishedAsync(\|TemplateKey" tests/KCC.IntegrationTests/Config/TestContent.cs
cat tests/KCC.IntegrationTests/AssemblyInfo.cs
ls src/KCC.Web/Features/Models/Generated | grep -E '^(HomePage|RecipeListingPage|CreateRecipePage|Recipe|RecipeVariant)\.generated\.cs$'
ls src/KCC.Web/Views/Page.cshtml 2>/dev/null || echo "no template fallback"
grep -n "IRecipeQueries\|BreadcrumbService\|PageMetadata\|IResourceStringProvider" src/KCC.Web/Program.cs
grep -rn "stripTilde" src/KCC.Web/Features || echo "no stripTilde left"
```

Expected, and what to do if not:

1. Phase 2's Status is `done`. If not, stop: this phase builds on it.
2. The `Unported slices` groups still hold these lines, which this plan deletes:
   - `KCC.Web.csproj`: `<Compile Remove="Features/Api/RecipeSearchApiController.cs" />`,
     `<Compile Remove="Features/Pages/RecipeSearch/**" />`, `<Compile Remove="Features/Search/**" />` and
     `<Content Remove="Features/Pages/RecipeSearch/**/*.cshtml" />`.
   - `KCC.UnitTests.csproj`: `<Compile Remove="Features/Search/**" />`.
   - `KCC.E2ETests.csproj`: `<Compile Remove="Features/RecipeSearch/**" />`.

   Any other line belongs to a later phase: leave it.
3. `IRecipeQueries` declares exactly `RecipePageData GetRecipePage(Recipe recipe)` and
   `VariantPageData GetVariantPage(RecipeVariant variant)`, and `public class RecipeQueries : IRecipeQueries` has no
   constructor. The records read:
   - `RecipePageData(RecipeRecord Recipe, IReadOnlyList<VariantRecord> Variants, string AddVariantUrl)`
   - `RecipeRecord(Guid Key, string Name, string Url, string Description, string Icon, string ImageUrl, string Category, Guid? AuthorKey, DateTime CreateDate)`
   - `VariantRecord(Guid Key, string Name, string Url, string Description, string Icon, string ImageUrl, int PrepTime, int CookTime, int Servings, string Difficulty, NutritionRecord Nutrition, IReadOnlyList<string> Tags, string IngredientsJson, string InstructionsJson, Guid? AuthorKey, DateTime CreateDate)`
4. `RecipeQueriesTests` builds the service with `new RecipeQueries()` five times, inside
   `private T WithContent<T>(Func<IPublishedContentCache, T> read)`. Task 3 changes both.
5. `ContributionWrites` takes `(IEFCoreScopeProvider<ContributionsDbContext> scopes, IContributionStats stats)`, and
   its one write, `UpsertReviewAsync`, ends with `stats.Invalidate();`. If Phase 2 grew another method that writes
   reviews, Task 5 gives it the same notification line.
6. `ContributionStats.RatingAcross(IEnumerable<Guid>)` returns `RatingAggregate(double Average, int Count)`.
   `IAuthorNameProvider` has `Task<IReadOnlyDictionary<Guid, string>> ResolveMany(IEnumerable<Guid>)`, and
   `AuthorNameProvider.NameFor(IReadOnlyDictionary<Guid, string>, Guid?)` is static.
7. `BreadcrumbService.Build(IPublishedContent page)` returns `IReadOnlyList<BreadcrumbLink>`, and
   `PageMetadata.Apply(IMetadata page, BasePageViewModel viewModel)` exists. Both are registered (in `Program.cs` or
   a composer), and so are `IResourceStringProvider` and `IRecipeQueries` (scoped).
8. `DevSeedApiController.SeedRecipes` takes `([FromServices] RecipeTestDataSeeder seeder, CancellationToken
   cancellationToken)` and calls `seeder.RunAsync(log, cancellationToken)`.
9. `UmbracoSite.ConfigureWebHost` applies `Settings()` through `builder.UseSetting`, and `AssemblyInfo.cs` holds
   `[assembly: NotInParallel]`.
10. `TestContent` has `RecipeListing`, `RecipeAsync`, `VariantAsync`, `ImageAsync`, `Image` and `UnpublishAsync`,
    plus a private
    `PublishedAsync(IServiceProvider services, string contentTypeAlias, string name, Guid parentKey, IEnumerable<PropertyValueModel> values)`.
11. The generated models include `HomePage`, `RecipeListingPage`, `CreateRecipePage`, `Recipe` and `RecipeVariant`.
12. **The §19 template fallback.** If `src/KCC.Web/Views/Page.cshtml` exists, `TestContent.PublishedAsync` sets
    `TemplateKey` on every create. Categories and tags take no template, so Task 5 Step 1 makes that conditional:
    look the type up once, as `var contentType = scoped.GetRequiredService<IContentTypeService>().Get(contentTypeAlias)!;`,
    use `contentType.Key` for `ContentTypeKey`, and set
    `TemplateKey = contentType.AllowedTemplates?.Any() == true ? (await scoped.GetRequiredService<ITemplateService>().GetAsync("page"))!.Key : null`.
13. No `stripTilde` is left. Phase 1 removed it, including from `RecipeSearchHeader.vue`.

Before any task starts the dev site, check that nothing else is listening on port 58671.

## What the scratch probe already proved

All of the following ran in a copy of Phase 2's scratch Umbraco 17.7 site on SQLite, using this plan's code blocks
verbatim. The run was green on every repeat: 102 unit tests (38 of them new) and 78 integration tests (36 new), with
warnings as errors and StyleCop on. The probe could not run the E2E suite or the reference capture, which need the
front end. Task 7's tests were compiled against TUnit.Playwright 1.27 and Playwright 1.58, but not run.

- **Spec §19 row 4 holds.** By the time a `ContentCacheRefresherNotification` handler runs, the published cache
  already returns the new state:
  - a just-published node's name and URL;
  - a rename;
  - nothing for a just-unpublished node.

  The source agrees. `ContentCacheRefresher.Refresh` updates navigation, publish status, the memory cache and the
  URLs first, and only then does `base.Refresh` publish the notification. The server messenger delivers to the
  local server before it queues anything remote. `ContentPublishedNotification` handlers also see the new state,
  but the refresher's one notification covers publish, unpublish, trash, move and delete alike, so the refresher is
  the trigger.
- **Rebuilds are cheap.** A full integration run triggered 28 rebuilds over 0 to 37 recipes. The median took 14 ms
  and the slowest 105 ms (the first, cold one). The seeder's burst of saves cost a single rebuild.
- **Renames reach the index.** Renaming a category or a tag node relabels its recipes and its facet after the
  rebuild, and renaming a member renames "started by". The tree picker's converted values are not served stale.
- **The §17 defect is fixed.** Unpublishing one of a recipe's two variants keeps the recipe in the index. The
  variant drops out of its count, and that variant's reviews drop out of its rating.
- **The swap is whole.** Four threads searched in a tight loop through ten rebuilds. Every search saw all five
  dinners, and none threw.
- **.NET 10 starts `BackgroundService.ExecuteAsync` on a background thread,** so code before its first `await` no
  longer runs inside `StartAsync`. The startup signal therefore lives in a `StartAsync` override. Without it, a
  caller waiting from start-up returns before the first build.
- **Outside a request,** `IPublishedContentQuery` resolves and reads without an Umbraco context, but `Url()` needs
  one. `IUmbracoContextFactory.EnsureUmbracoContext()` provides it on the rebuild thread.
- **Dates:** a published node's `CreateDate` has `DateTimeKind.Utc`.
- **The API** camel-cases property names but keeps facet keys as written (`Lunch`, `Gluten-Free`).
- **The Kentico base fields** that spec §9.2 mentions are not needed: `MapHit` reads only this project's own fields.

## Global Constraints

Phase 1's and Phase 2's constraints still apply:

- Work on branch **`replatform`**. The replatform's spec, phase plans and reference set are tracked in
  `docs/replatform/`: commit changes to them (a status line, a correction) with the work they describe. Everything
  else under `.superpowers/` stays gitignored: never stage anything there. Commit messages are Title Case imperative
  with no attribution lines. Ask the owner before any `git push`.
- **Umbraco.Cms 17.x, never 18.** Every `Umbraco.Cms*` package takes the same version as `Umbraco.Cms`.
- **The SQLite connection string never contains `Cache=Shared`.**
- **`ModelsMode`** is `SourceCodeManual` in Development and `Nothing` everywhere else. Models live in
  `Features/Models/Generated` (namespace `KCC.Web.Features.Models.Generated`) and are committed. This phase changes
  no schema, so it generates no models and uSync exports nothing.
- **Route hijacking.** A page controller is named `<Alias>Controller`, derives from `RenderController`, has no
  `[Route]`, and returns its view by explicit path.
- **Only APIs that survive Umbraco 18.** Use `ControllerBase` for APIs, `IPublishedContentQuery.ContentAtRoot()`
  for roots, and the friendly extensions `Children<T>()`, `Parent<T>()` and `Url()`. Warnings are errors, so an
  obsolete member fails the build (CS0618).
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
- **The dev site** runs with `cd src/KCC.Web && dotnet run --launch-profile Local` on `https://localhost:58671`.

Phase 3's own constraints:

- **Never reference a Lucene package.** Lucene.Net.Facet comes through Examine (spec §6.1). Index and query with
  `StandardAnalyzer(LuceneVersion.LUCENE_48)`, and hold both the index and the taxonomy in `RAMDirectory`s.
- **The search contract does not change** (spec §9.2). These stay as they are: the field names in
  `RecipeSearchConstants`, the fields `BuildDocument` writes, the `RecipeSearchCriteria` rules and the
  `RecipeSearchResponseMapper` envelope. No `.vue` or `.ts` file changes in this phase.
- **The rebuild rule.** A content cache refresh, a member save or delete, or a review write calls
  `IRecipeIndexRebuilder.Signal()`. A burst of signals folds into one rebuild, `RecipeSearch:RebuildDelay` after its
  last signal: `00:00:02` by default, `00:00:00.100` in the integration host. The startup build skips the delay. A
  build never touches the live snapshot; it swaps in whole.
- **Every review write publishes `ReviewsChangedNotification`** through `IEventAggregator`, after its scope has
  completed and after `stats.Invalidate()`. Phase 4's review delete and cascades, and Phase 5's dashboard edits,
  must do the same.
- **Wait for the index with `IRecipeIndexRebuilder.WhenCurrentAsync`**, never with a sleep or a polling loop. It
  completes once every change signalled before the call is in the index, and it throws if that rebuild failed.
- **Tests that change content make their own nodes.** Name them with a word no seeded recipe uses (`IT Quokka`), and
  never rename, move or delete seeded nodes. Other suites add uncategorised recipes, and `RecipePageTests` adds one
  rated 5.0. Assertions about the seed therefore filter by category, diet or a seeded word. They never count, or
  take the spotlight of, the whole site.

## Not in Phase 3

- **Phase 4:**
  - `RecipeSearchLiveRatingTests`, which signs in and writes a review through the UI. It stays excluded until the
    member sign-in and the review write API exist. When it returns, the rebuild lands about two seconds after the
    review, well inside its existing reload loop.
  - The SQLite concurrency test (spec §14), which repeats parallel review writes, a content save and an index
    rebuild. It drives the rebuild through `IRecipeIndexRebuilder.Signal` and `WhenCurrentAsync`.
  - Member submissions. They are created under the listing but only saved, so the index skips them until the owner
    publishes one.
  - Rate limits and anti-forgery. The search API is a GET, and it gets neither.
- **Phase 5:** the Contributions dashboard. Its review edits and deletes publish `ReviewsChangedNotification`.
- **Out of scope** (spec §17): the "15 min or more" time filter caps at 60 minutes and drops longer recipes.

## File map

| Path | Change | Task |
|---|---|---|
| `tests/KCC.IntegrationTests/Config/PublishedCacheProbe.cs` | Create | 1 |
| `tests/KCC.IntegrationTests/Config/UmbracoSite.cs` | Modify (probe handler; rebuild delay) | 1, 5 |
| `tests/KCC.IntegrationTests/Config/TestContent.cs` | Modify (rename; pickers, trash, category, tag and author helpers) | 1, 5 |
| `tests/KCC.IntegrationTests/Features/Search/PublishedCacheOrderTests.cs` | Create | 1 |
| `src/KCC.Web/Features/Search/{RecipeSearchIndexingStrategy,RecipeReindexTargetResolver,IRecipeReindexTargetResolver,RecipeReindexer,IRecipeReindexer,VariantReviewSearchModule}.cs` | Delete | 2 |
| `tests/KCC.UnitTests/Features/Search/RecipeReindexerTests.cs` | Delete | 2 |
| `src/KCC.Web/Features/Search/{RecipeSearchConstants,RecipeSearchService}.cs` | Modify | 2 |
| `src/KCC.Web/Features/Search/{RecipeFacets,RecipeIndexSnapshot,RecipeIndexBuilder,RecipeIndex}.cs` | Create | 2 |
| `tests/KCC.UnitTests/Features/Search/{RecipeSearchServiceTests,RecipeIndexTests}.cs` | Create | 2 |
| `src/KCC.Web/KCC.Web.csproj` | Modify (exclusions) | 2, 6 |
| `tests/KCC.UnitTests/KCC.UnitTests.csproj` | Modify (exclusion) | 2 |
| `src/KCC.Web/Features/Recipes/RecipeQueries.cs` | Modify | 3 |
| `src/KCC.Web/Features/Search/{RecipeSearchDocuments,RecipeIndexSource}.cs` | Create | 3 |
| `tests/KCC.UnitTests/Features/Search/RecipeSearchDocumentsTests.cs` | Create | 3 |
| `tests/KCC.IntegrationTests/Features/Recipes/RecipeQueriesTests.cs` | Modify | 3 |
| `tests/KCC.IntegrationTests/Features/Search/RecipeIndexSourceTests.cs` | Create | 3 |
| `src/KCC.Web/Features/Search/{RecipeSearchOptions,RecipeIndexRebuilder}.cs` | Create | 4 |
| `tests/KCC.UnitTests/Features/Search/RecipeIndexRebuilderTests.cs` | Create | 4 |
| `src/KCC.Contributions/ReviewsChangedNotification.cs` | Create | 5 |
| `src/KCC.Contributions/ContributionWrites.cs` | Modify | 5 |
| `src/KCC.Web/Features/Search/{RecipeIndexTriggers,SearchComposer}.cs` | Create | 5 |
| `src/KCC.Web/Features/DevTools/RecipeSeed/DevSeedApiController.cs` | Modify | 5 |
| `tests/KCC.IntegrationTests/Features/Search/{RecipeSearchTests,RecipeIndexTriggerTests,RecipeIndexSwapTests}.cs` | Create | 5 |
| `src/KCC.Web/Features/Pages/RecipeSearch/RecipeSearchController.cs` → `RecipeListingPageController.cs` | Move and rewrite | 6 |
| `tests/KCC.IntegrationTests/Features/Pages/RecipeListingPageTests.cs`, `Features/Search/RecipeSearchApiTests.cs` | Create | 6 |
| `tests/KCC.E2ETests/Features/RecipeSearch/RecipeSearchTests.cs` | Rewrite | 7 |
| `tests/KCC.E2ETests/KCC.E2ETests.csproj` | Modify (exclusion) | 7 |
| `README.md`, memory | Modify | 8 |

These compile again unchanged: `Features/Search/{IRecipeSearchService,RecipeSearchCriteria,RecipeSearchDocument,RecipeSearchResults}.cs`,
`Features/Pages/RecipeSearch/{RecipeSearchViewModel.cs,Index.cshtml}`, `Features/Api/RecipeSearchApiController.cs`,
and the unit tests `RecipeSearchCriteriaTests` and `RecipeSearchDocumentTests`. The whole search front end is
unchanged too: `RecipeSearchView`, `useRecipeSearch`, `recipeSearchCriteria` and `Components/RecipeSearch/*`.

---

### Task 1: Prove the published cache is current when its refresher notifies

Spec §19 row 4 is proven before anything is built on it. The index rebuilds from Umbraco's published cache when
`ContentCacheRefresherNotification` fires, so the cache must already hold the change by then. This task adds a
test-only handler to the integration host. For each refreshed node, the handler records what the published cache
answers at the moment the notification is handled.

**Files:**
- Create: `tests/KCC.IntegrationTests/Config/PublishedCacheProbe.cs`,
  `tests/KCC.IntegrationTests/Features/Search/PublishedCacheOrderTests.cs`
- Modify: `tests/KCC.IntegrationTests/Config/UmbracoSite.cs`, `tests/KCC.IntegrationTests/Config/TestContent.cs`

**Interfaces:**
- Consumes: `UmbracoSite` (Phase 1), and `TestContent.RecipeAsync` and `TestContent.UnpublishAsync` (Phase 2).
- Produces:
  - `PublishedCacheProbe`, a singleton in the integration host, with `Sighting? LastSighting(Guid key)` and
    `record Sighting(Guid Key, string? PublishedName, string? PublishedUrl)`.
  - `TestContent.RenameAsync(IServiceProvider services, Guid key, string name) → Task`, which saves the new name and
    publishes it.

- [ ] **Step 1: Write the failing proof**

Create `tests/KCC.IntegrationTests/Features/Search/PublishedCacheOrderTests.cs`:

```csharp
using KCC.IntegrationTests.Config;
using Microsoft.Extensions.DependencyInjection;

namespace KCC.IntegrationTests.Features.Search;

// Spec §19: the content cache refresher's notification fires after the published cache is current, so a rebuild it
// triggers reads the change it was triggered by.
public class PublishedCacheOrderTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    private PublishedCacheProbe Probe => Site.Services.GetRequiredService<PublishedCacheProbe>();

    [Test]
    public async Task Publishing_IsInTheCacheWhenTheNotificationFires()
    {
        var key = await TestContent.RecipeAsync(Site.Services, "IT Refresher Fresh");

        var sighting = Probe.LastSighting(key);

        _ = await Assert.That(sighting?.PublishedName).IsEqualTo("IT Refresher Fresh");
        _ = await Assert.That(sighting?.PublishedUrl).IsEqualTo("/recipes/it-refresher-fresh/");
    }

    [Test]
    public async Task ARename_IsInTheCacheWhenTheNotificationFires()
    {
        var key = await TestContent.RecipeAsync(Site.Services, "IT Refresher Before");

        await TestContent.RenameAsync(Site.Services, key, "IT Refresher After");

        var sighting = Probe.LastSighting(key);
        _ = await Assert.That(sighting?.PublishedName).IsEqualTo("IT Refresher After");
        _ = await Assert.That(sighting?.PublishedUrl).IsEqualTo("/recipes/it-refresher-after/");
    }

    [Test]
    public async Task AnUnpublish_IsOutOfTheCacheWhenTheNotificationFires()
    {
        var key = await TestContent.RecipeAsync(Site.Services, "IT Refresher Withdrawn");

        await TestContent.UnpublishAsync(Site.Services, key);

        var sighting = Probe.LastSighting(key);
        _ = await Assert.That(sighting).IsNotNull();
        _ = await Assert.That(sighting!.PublishedName).IsNull();
    }
}
```

- [ ] **Step 2: Run it to confirm it fails**

```bash
dotnet build KitchenCommandCenter.sln
```

Expected: build errors — `PublishedCacheProbe` and `TestContent.RenameAsync` do not exist.

- [ ] **Step 3: Write the probe, register it, and add the rename helper**

Create `tests/KCC.IntegrationTests/Config/PublishedCacheProbe.cs`:

```csharp
using System.Collections.Concurrent;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace KCC.IntegrationTests.Config;

// The recipe index rebuilds when this notification is handled, so what the published cache answers here is what a
// rebuild reads.
public sealed class PublishedCacheProbe(IUmbracoContextFactory umbracoContextFactory)
    : INotificationHandler<ContentCacheRefresherNotification>
{
    private readonly ConcurrentQueue<Sighting> sightings = new();

    public Sighting? LastSighting(Guid key) => sightings.LastOrDefault(sighting => sighting.Key == key);

    public void Handle(ContentCacheRefresherNotification notification)
    {
        if (notification.MessageObject is not ContentCacheRefresher.JsonPayload[] payloads)
        {
            return;
        }

        using var context = umbracoContextFactory.EnsureUmbracoContext();
        foreach (var key in payloads.Select(payload => payload.Key).OfType<Guid>())
        {
            var content = context.UmbracoContext.Content!.GetById(key);
            sightings.Enqueue(new Sighting(key, content?.Name, content?.Url()));
        }
    }

    public sealed record Sighting(Guid Key, string? PublishedName, string? PublishedUrl);
}
```

In `tests/KCC.IntegrationTests/Config/UmbracoSite.cs`:
1. Add `using Umbraco.Cms.Core.Events;` and `using Umbraco.Cms.Core.Notifications;`, keeping the usings sorted.
2. At the end of `ConfigureWebHost`, after the `foreach` that calls `builder.UseSetting`, add:

```csharp

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<PublishedCacheProbe>();
            services.AddSingleton<INotificationHandler<ContentCacheRefresherNotification>>(
                provider => provider.GetRequiredService<PublishedCacheProbe>());
        });
```

Umbraco's event aggregator resolves every registered `INotificationHandler<T>`, so a handler registered straight on
the service collection runs alongside the site's own.

In `tests/KCC.IntegrationTests/Config/TestContent.cs`, add this method before `UnpublishAsync`:

```csharp
    public static async Task RenameAsync(IServiceProvider services, Guid key, string name)
    {
        using var scope = services.CreateScope();
        var contentService = scope.ServiceProvider.GetRequiredService<IContentService>();
        var content = contentService.GetById(key) ?? throw new InvalidOperationException($"No content {key}.");
        content.Name = name;
        if (!contentService.Save(content).Success)
        {
            throw new InvalidOperationException($"Renaming {key} failed.");
        }

        var published = await scope.ServiceProvider.GetRequiredService<IContentPublishingService>()
            .PublishAsync(key, [new CulturePublishScheduleModel { Culture = null }], Constants.Security.SuperUserKey);
        if (!published.Success)
        {
            throw new InvalidOperationException($"Publishing {name} failed: {published.Status}.");
        }
    }

```

`IContentService.Save` changes only the name. The editing service's update model would replace every property
value, too.

- [ ] **Step 4: Run the proof**

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/PublishedCacheOrderTests/*"
```

Expected: 3 passed.

**If any of them fails,** the notification fired before the cache held the change. Stop, and do not start Task 2.
Tell the owner which assertion failed. Spec §19's fallback is to rebuild from the content service's published
versions instead of the published cache. That reshapes Tasks 3 and 5, so the plan needs revising first.

- [ ] **Step 5: Run the whole integration suite**

```bash
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj
```

Expected: every test passes. The probe only records.

- [ ] **Step 6: Commit**

```bash
git add tests/KCC.IntegrationTests
git commit -m "Prove the Published Cache Is Current When Its Refresher Notifies"
```

---

### Task 2: The in-memory index and the query side

The Kentico glue goes: the indexing strategy, the reindex target resolver, the reindexer and the review module
(spec §9.2). `BuildDocument` moves into `RecipeIndexBuilder` unchanged. The query side keeps its drill-sideways
search, sort, paging and spotlight code, and changes only where the searcher comes from. The facet configuration was
written out twice, once for the writer and once for the query; it now lives once, in `RecipeFacets`.

**Files:**
- Delete:
  - `src/KCC.Web/Features/Search/{RecipeSearchIndexingStrategy,RecipeReindexTargetResolver,IRecipeReindexTargetResolver,RecipeReindexer,IRecipeReindexer,VariantReviewSearchModule}.cs`
  - `tests/KCC.UnitTests/Features/Search/RecipeReindexerTests.cs`
- Modify:
  - `src/KCC.Web/Features/Search/RecipeSearchConstants.cs`, `src/KCC.Web/Features/Search/RecipeSearchService.cs`
  - `src/KCC.Web/KCC.Web.csproj`, `tests/KCC.UnitTests/KCC.UnitTests.csproj`
- Create:
  - `src/KCC.Web/Features/Search/{RecipeFacets,RecipeIndexSnapshot,RecipeIndexBuilder,RecipeIndex}.cs`
  - `tests/KCC.UnitTests/Features/Search/{RecipeSearchServiceTests,RecipeIndexTests}.cs`

**Interfaces:**
- Consumes the unchanged `RecipeSearchDocument`, `RecipeSearchCriteria`, `RecipeSearchResults`,
  `RecipeSearchResponseMapper`, `RecipeSearchConstants` and `IRecipeSearchService` in `Features/Search`.
- Produces (namespace `KCC.Web.Features.Search`):
  - `RecipeFacets.Config() → FacetsConfig`, with both facet dimensions multi-valued.
  - `RecipeIndexBuilder.Build(IEnumerable<RecipeSearchDocument> documents) → RecipeIndexSnapshot`, and
    `internal static Document RecipeIndexBuilder.BuildDocument(RecipeSearchDocument d)`.
  - `sealed class RecipeIndexSnapshot : IDisposable`, with `IndexSearcher Searcher` and `TaxonomyReader Taxonomy`.
  - `sealed class RecipeIndex : IDisposable`. It starts empty. Its members are
    `T Search<T>(Func<IndexSearcher, TaxonomyReader, T> search)` and `void Replace(RecipeIndexSnapshot next)`, which
    disposes the snapshot it replaces.
  - `RecipeSearchService(RecipeIndex index) : IRecipeSearchService`.

- [ ] **Step 1: Delete the Kentico glue and bring the folder back into the build**

```bash
git rm -q src/KCC.Web/Features/Search/RecipeSearchIndexingStrategy.cs \
  src/KCC.Web/Features/Search/RecipeReindexTargetResolver.cs \
  src/KCC.Web/Features/Search/IRecipeReindexTargetResolver.cs \
  src/KCC.Web/Features/Search/RecipeReindexer.cs \
  src/KCC.Web/Features/Search/IRecipeReindexer.cs \
  src/KCC.Web/Features/Search/VariantReviewSearchModule.cs \
  tests/KCC.UnitTests/Features/Search/RecipeReindexerTests.cs
```

In `src/KCC.Web/KCC.Web.csproj` and in `tests/KCC.UnitTests/KCC.UnitTests.csproj`, delete
`<Compile Remove="Features/Search/**" />`.

- [ ] **Step 2: Write the failing tests**

These run the query side against small in-memory indexes. No Umbraco is involved.

Create `tests/KCC.UnitTests/Features/Search/RecipeSearchServiceTests.cs`:

```csharp
using KCC.Web.Features.Search;

namespace KCC.UnitTests.Features.Search;

public class RecipeSearchServiceTests
{
    [Test]
    public async Task Search_NeedsEveryTermSomewhereInTheNameOrContent()
    {
        var search = Service(
            Doc("Chickpea Curry", description: "Weeknight coconut dinner"),
            Doc("Coconut Rice", description: "A side dish"));

        var results = search.Search(new RecipeSearchCriteria { Query = "coconut weeknight" });

        _ = await Assert.That(Names(results)).IsEqualTo("Chickpea Curry");
    }

    [Test]
    public async Task Search_RanksANameMatchAboveAContentMatch()
    {
        var search = Service(
            Doc("Apple Soup", ingredients: ["Lemon"]),
            Doc("Zesty Lemon Bars"));

        var results = search.Search(new RecipeSearchCriteria { Query = "lemon" });

        _ = await Assert.That(string.Join(",", results.Results.Select(hit => hit.Name))).IsEqualTo("Zesty Lemon Bars,Apple Soup");
    }

    [Test]
    public async Task Search_ReadsOnlyPunctuationAsEverything()
    {
        var search = Service(Doc("Apple Pie"), Doc("Banana Bread"));

        _ = await Assert.That(search.Search(new RecipeSearchCriteria { Query = "?!" }).Total).IsEqualTo(2);
    }

    [Test]
    public async Task Facets_KeepTheirOwnDimensionWideWhileNarrowingTheOthers()
    {
        var search = Service(
            Doc("Chili", category: "Dinner", diets: ["Spicy"]),
            Doc("Stew", category: "Dinner", diets: ["Hearty"]),
            Doc("Salsa", category: "Snack", diets: ["Spicy"]));

        var results = search.Search(new RecipeSearchCriteria { Categories = ["Dinner"] });

        _ = await Assert.That(results.Total).IsEqualTo(2);
        _ = await Assert.That(results.CategoryFacets["Snack"]).IsEqualTo(1);
        _ = await Assert.That(results.CategoryFacets["Dinner"]).IsEqualTo(2);
        _ = await Assert.That(results.DietFacets["Spicy"]).IsEqualTo(1);
        _ = await Assert.That(results.DietFacets["Hearty"]).IsEqualTo(1);
    }

    [Test]
    public async Task Facets_OrValuesWithinADimension()
    {
        var search = Service(Doc("Chili", category: "Dinner"), Doc("Salsa", category: "Snack"), Doc("Tea", category: "Beverage"));

        var results = search.Search(new RecipeSearchCriteria { Categories = ["Dinner", "Snack"] });

        _ = await Assert.That(Names(results)).IsEqualTo("Chili,Salsa");
    }

    [Test]
    public async Task TimeFilter_KeepsRecipesInsideTheRange()
    {
        var search = Service(Doc("Toast", fastest: 5), Doc("Soup", fastest: 30), Doc("Roast", fastest: 90));

        var results = search.Search(new RecipeSearchCriteria { TimeMin = 0, TimeMax = 30 });

        _ = await Assert.That(Names(results)).IsEqualTo("Soup,Toast");
    }

    [Test]
    [Arguments("rated", "Bread,Apple,Cake")]
    [Arguments("variants", "Cake,Apple,Bread")]
    [Arguments("recent", "Apple,Cake,Bread")]
    [Arguments("relevant", "Apple,Bread,Cake")]
    public async Task Sort_OrdersByTheChosenKey(string sort, string expected)
    {
        var search = Service(
            Doc("Bread", rating: 5, reviews: 1, variants: 1, published: 100),
            Doc("Apple", rating: 4, reviews: 1, variants: 2, published: 300),
            Doc("Cake", rating: 3, reviews: 1, variants: 3, published: 200));

        var results = search.Search(new RecipeSearchCriteria { Sort = sort });

        _ = await Assert.That(string.Join(",", results.Results.Select(hit => hit.Name))).IsEqualTo(expected);
    }

    [Test]
    public async Task Paging_WalksEveryMatchOnceAndSpotlightsOnlyThePageZero()
    {
        var search = Service(Enumerable.Range(1, 5).Select(i => Doc($"Recipe {i:00}", rating: i, reviews: 1)).ToArray());

        var first = search.Search(new RecipeSearchCriteria { PageSize = 2 });
        var second = search.Search(new RecipeSearchCriteria { PageSize = 2, Page = 1 });
        var third = search.Search(new RecipeSearchCriteria { PageSize = 2, Page = 2 });
        var beyond = search.Search(new RecipeSearchCriteria { PageSize = 2, Page = 3 });

        _ = await Assert.That($"{Names(first)}|{Names(second)}|{Names(third)}|{Names(beyond)}")
            .IsEqualTo("Recipe 01,Recipe 02|Recipe 03,Recipe 04|Recipe 05|");
        _ = await Assert.That(first.Spotlight.Name).IsEqualTo("Recipe 05");
        _ = await Assert.That(second.Spotlight).IsNull();
        _ = await Assert.That(first.Total).IsEqualTo(5);
    }

    [Test]
    public async Task Spotlight_NeedsARating()
    {
        var search = Service(Doc("Toast"), Doc("Soup"));

        _ = await Assert.That(search.Search(new RecipeSearchCriteria()).Spotlight).IsNull();
    }

    [Test]
    public async Task Hit_CarriesTheCardFields()
    {
        var search = Service(Doc("Chili", category: "Dinner", diets: ["Spicy", "Vegan"], fastest: 25, rating: 4.5, reviews: 2, variants: 3));

        var hit = search.Search(new RecipeSearchCriteria()).Results.Single();

        _ = await Assert.That(hit.Slug).IsEqualTo("/recipes/chili/");
        _ = await Assert.That(hit.Icon).IsEqualTo("fa-duotone fa-pot-food");
        _ = await Assert.That(hit.Category).IsEqualTo("Dinner");
        _ = await Assert.That(hit.StartedBy).IsEqualTo("Priya Balan");
        _ = await Assert.That(string.Join(",", hit.Tags)).IsEqualTo("Spicy,Vegan");
        _ = await Assert.That(hit.FastestTime).IsEqualTo(25);
        _ = await Assert.That(hit.AverageRating).IsEqualTo(4.5d);
        _ = await Assert.That(hit.ReviewCount).IsEqualTo(2);
        _ = await Assert.That(hit.VariantCount).IsEqualTo(3);
    }

    [Test]
    public async Task Hit_WithoutReviewsHasNoRating()
    {
        var search = Service(Doc("Chili"));

        _ = await Assert.That(search.Search(new RecipeSearchCriteria()).Results.Single().AverageRating).IsNull();
    }

    [Test]
    public async Task EmptyIndex_FindsNothing()
    {
        var results = new RecipeSearchService(new RecipeIndex()).Search(new RecipeSearchCriteria());

        _ = await Assert.That(results.Total).IsEqualTo(0);
        _ = await Assert.That(results.CategoryFacets.Count).IsEqualTo(0);
    }

    private static RecipeSearchService Service(params RecipeSearchDocument[] documents)
    {
        var index = new RecipeIndex();
        index.Replace(RecipeIndexBuilder.Build(documents));
        return new RecipeSearchService(index);
    }

    private static string Names(RecipeSearchResults results) => string.Join(",", results.Results.Select(hit => hit.Name).Order());

    private static RecipeSearchDocument Doc(
        string name,
        string description = "",
        string category = "Dinner",
        string[] diets = null,
        string[] ingredients = null,
        int fastest = 20,
        double rating = 0,
        int reviews = 0,
        int variants = 1,
        long published = 1_789_862_400L) => new()
    {
        Name = name,
        Slug = $"/recipes/{name.ToLowerInvariant().Replace(' ', '-')}/",
        Icon = "fa-duotone fa-pot-food",
        Category = category,
        StartedBy = "Priya Balan",
        Description = description,
        Diets = diets ?? [],
        IngredientNames = ingredients ?? [],
        FastestTime = fastest,
        VariantCount = variants,
        AverageRating = rating,
        ReviewCount = reviews,
        PublishedUnixSeconds = published,
    };
}
```

Create `tests/KCC.UnitTests/Features/Search/RecipeIndexTests.cs`:

```csharp
using KCC.Web.Features.Search;

namespace KCC.UnitTests.Features.Search;

public class RecipeIndexTests
{
    [Test]
    public async Task Replace_ServesTheNewSnapshot()
    {
        using var index = new RecipeIndex();

        index.Replace(RecipeIndexBuilder.Build([new RecipeSearchDocument { Name = "Chili" }]));

        _ = await Assert.That(index.Search((searcher, _) => searcher.IndexReader.NumDocs)).IsEqualTo(1);
    }

    [Test]
    public async Task Replace_WaitsForASearchStillReadingTheOldSnapshot()
    {
        using var index = new RecipeIndex();
        index.Replace(RecipeIndexBuilder.Build([new RecipeSearchDocument { Name = "Chili" }]));
        using var searching = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();

        var search = Task.Run(() => index.Search((searcher, _) =>
        {
            searching.Set();
            release.Wait();
            return searcher.Doc(0).Get(RecipeSearchConstants.FieldName);
        }));
        searching.Wait();
        var replace = Task.Run(() => index.Replace(RecipeIndexBuilder.Build([])));
        await Task.Delay(100);
        var replacedEarly = replace.IsCompleted;
        release.Set();

        _ = await Assert.That(replacedEarly).IsFalse();
        _ = await Assert.That(await search).IsEqualTo("Chili");
        await replace;
        _ = await Assert.That(index.Search((searcher, _) => searcher.IndexReader.NumDocs)).IsEqualTo(0);
    }
}
```

- [ ] **Step 3: Run them to confirm they fail**

```bash
dotnet build KitchenCommandCenter.sln
```

Expected: build errors. `RecipeIndex`, `RecipeIndexBuilder` and `RecipeFacets` do not exist, and
`RecipeSearchService` still refers to Kentico's `ILuceneIndexManager`.

- [ ] **Step 4: Write the index**

Create `src/KCC.Web/Features/Search/RecipeFacets.cs`:

```csharp
using Lucene.Net.Facet;

namespace KCC.Web.Features.Search;

public static class RecipeFacets
{
    // The index writer and the drill-down query must encode facet terms the same way, so both take their
    // configuration from here.
    public static FacetsConfig Config()
    {
        var config = new FacetsConfig();
        config.SetMultiValued(RecipeSearchConstants.FacetCategory, true);
        config.SetMultiValued(RecipeSearchConstants.FacetDiet, true);
        return config;
    }
}
```

Create `src/KCC.Web/Features/Search/RecipeIndexSnapshot.cs`:

```csharp
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Facet.Taxonomy.Directory;
using Lucene.Net.Index;
using Lucene.Net.Search;
using LuceneDirectory = Lucene.Net.Store.Directory;

namespace KCC.Web.Features.Search;

public sealed class RecipeIndexSnapshot : IDisposable
{
    private readonly LuceneDirectory indexDirectory;
    private readonly LuceneDirectory taxonomyDirectory;
    private readonly DirectoryReader reader;

    public RecipeIndexSnapshot(LuceneDirectory indexDirectory, LuceneDirectory taxonomyDirectory)
    {
        this.indexDirectory = indexDirectory;
        this.taxonomyDirectory = taxonomyDirectory;
        reader = DirectoryReader.Open(indexDirectory);
        Taxonomy = new DirectoryTaxonomyReader(taxonomyDirectory);
        Searcher = new IndexSearcher(reader);
    }

    public IndexSearcher Searcher { get; }

    public TaxonomyReader Taxonomy { get; }

    public void Dispose()
    {
        Taxonomy.Dispose();
        reader.Dispose();
        taxonomyDirectory.Dispose();
        indexDirectory.Dispose();
    }
}
```

`Directory` is aliased because `ImplicitUsings` already brings in `System.IO.Directory`.

Create `src/KCC.Web/Features/Search/RecipeIndexBuilder.cs`. `BuildDocument` is the deleted strategy's method,
unchanged:

```csharp
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Facet;
using Lucene.Net.Facet.Taxonomy.Directory;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace KCC.Web.Features.Search;

public static class RecipeIndexBuilder
{
    public static RecipeIndexSnapshot Build(IEnumerable<RecipeSearchDocument> documents)
    {
        var indexDirectory = new RAMDirectory();
        var taxonomyDirectory = new RAMDirectory();
        var facets = RecipeFacets.Config();

        using (var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48))
        using (var writer = new IndexWriter(indexDirectory, new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer)))
        using (var taxonomyWriter = new DirectoryTaxonomyWriter(taxonomyDirectory))
        {
            foreach (var document in documents)
            {
                writer.AddDocument(facets.Build(taxonomyWriter, BuildDocument(document)));
            }

            // Committed even when empty: a reader can only open a directory that holds a commit.
            taxonomyWriter.Commit();
            writer.Commit();
        }

        return new RecipeIndexSnapshot(indexDirectory, taxonomyDirectory);
    }

    internal static Document BuildDocument(RecipeSearchDocument d)
    {
        var doc = new Document
        {
            new TextField(RecipeSearchConstants.FieldName, d.Name, Field.Store.YES),
            new SortedDocValuesField(RecipeSearchConstants.FieldNameSort, new BytesRef((d.Name ?? string.Empty).ToLowerInvariant())),
            new TextField(RecipeSearchConstants.FieldContent, RecipeSearchDocument.BuildContent(d), Field.Store.NO),
            new StringField(RecipeSearchConstants.FieldSlug, d.Slug, Field.Store.YES),
            new StringField(RecipeSearchConstants.FieldIcon, d.Icon, Field.Store.YES),
            new StringField(RecipeSearchConstants.FieldCategory, d.Category, Field.Store.YES),
            new StringField(RecipeSearchConstants.FieldStartedBy, d.StartedBy, Field.Store.YES),
            new StringField(RecipeSearchConstants.FieldTags, RecipeSearchDocument.JoinTags(d.Diets), Field.Store.YES),
            new Int32Field(RecipeSearchConstants.FieldFastestTime, d.FastestTime, Field.Store.YES),
            new Int32Field(RecipeSearchConstants.FieldVariantCount, d.VariantCount, Field.Store.YES),
            new Int32Field(RecipeSearchConstants.FieldReviewCount, d.ReviewCount, Field.Store.YES),
            new StoredField(RecipeSearchConstants.FieldAverageRating + "_v", d.AverageRating),
            new Int64Field(RecipeSearchConstants.FieldPublished, d.PublishedUnixSeconds, Field.Store.YES),
            new NumericDocValuesField(RecipeSearchConstants.FieldFastestTime, d.FastestTime),
            new NumericDocValuesField(RecipeSearchConstants.FieldVariantCount, d.VariantCount),
            new DoubleDocValuesField(RecipeSearchConstants.FieldAverageRating, d.AverageRating),
            new NumericDocValuesField(RecipeSearchConstants.FieldPublished, d.PublishedUnixSeconds),
        };

        if (!string.IsNullOrWhiteSpace(d.Category))
        {
            doc.Add(new FacetField(RecipeSearchConstants.FacetCategory, d.Category));
        }

        foreach (var diet in d.Diets.Where(tag => !string.IsNullOrWhiteSpace(tag)))
        {
            doc.Add(new FacetField(RecipeSearchConstants.FacetDiet, diet));
        }

        return doc;
    }
}
```

Create `src/KCC.Web/Features/Search/RecipeIndex.cs`:

```csharp
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Search;

namespace KCC.Web.Features.Search;

public sealed class RecipeIndex : IDisposable
{
    private readonly ReaderWriterLockSlim swap = new();
    private RecipeIndexSnapshot current = RecipeIndexBuilder.Build([]);

    public T Search<T>(Func<IndexSearcher, TaxonomyReader, T> search)
    {
        swap.EnterReadLock();
        try
        {
            return search(current.Searcher, current.Taxonomy);
        }
        finally
        {
            swap.ExitReadLock();
        }
    }

    // A rebuild builds its snapshot off to the side, so only the swap waits for searches in flight; no search sees
    // a half-built index, and no snapshot is disposed while a search still reads it.
    public void Replace(RecipeIndexSnapshot next)
    {
        RecipeIndexSnapshot previous;
        swap.EnterWriteLock();
        try
        {
            previous = current;
            current = next;
        }
        finally
        {
            swap.ExitWriteLock();
        }

        previous.Dispose();
    }

    public void Dispose()
    {
        current.Dispose();
        swap.Dispose();
    }
}
```

A search holds the read lock only for the time it takes to run, which is milliseconds. A rebuild holds the write
lock only to swap two references.

- [ ] **Step 5: Point the query side at it**

In `src/KCC.Web/Features/Search/RecipeSearchConstants.cs`, delete the Kentico index-registration names and the blank
line after them:

```csharp
    public const string IndexName = "RecipeSearch";
    public const string StrategyName = "RecipeSearch";

```

In `src/KCC.Web/Features/Search/RecipeSearchService.cs`:
1. Delete `using Kentico.Xperience.Lucene.Core.Indexing;` and `using Kentico.Xperience.Lucene.Core.Search;`.
2. Replace the class declaration

```csharp
public class RecipeSearchService(
    ILuceneIndexManager indexManager,
    ILuceneSearchService searchService) : IRecipeSearchService
```

with

```csharp
public class RecipeSearchService(RecipeIndex index) : IRecipeSearchService
```

3. In `Search`, replace

```csharp
        var index = indexManager.GetRequiredIndex(RecipeSearchConstants.IndexName);
        var facetsConfig = BuildFacetsConfig();
```

with

```csharp
        var facetsConfig = RecipeFacets.Config();
```

4. Replace

```csharp
        return searchService.UseSearcherWithDrillSideways(index, (searcher, drillSideways) =>
        {
```

with

```csharp
        return index.Search((searcher, taxonomy) =>
        {
            var drillSideways = new DrillSideways(searcher, facetsConfig, taxonomy);

```

5. Delete the whole `private static FacetsConfig BuildFacetsConfig()` method, with its comment and the blank line
   after it.

The rest of the file — the query, the tokenizer, the sort, the hit mapping, the spotlight and the facet reading —
stays exactly as it is. `git diff src/KCC.Web/Features/Search/RecipeSearchService.cs` shows only this:

```diff
--- a/src/KCC.Web/Features/Search/RecipeSearchService.cs
+++ b/src/KCC.Web/Features/Search/RecipeSearchService.cs
@@ -1,6 +1,4 @@
 using System.Globalization;
-using Kentico.Xperience.Lucene.Core.Indexing;
-using Kentico.Xperience.Lucene.Core.Search;
 using Lucene.Net.Analysis.Standard;
 using Lucene.Net.Analysis.TokenAttributes;
 using Lucene.Net.Documents;
@@ -16,9 +14,7 @@ namespace KCC.Web.Features.Search;
 /// range), applies the selected category/diet facets as a drill-down, and reads cross-aware (drill-sideways) facet
 /// counts alongside a sorted, paged slice of hits and an optional highest-rated spotlight.
 /// </summary>
-public class RecipeSearchService(
-    ILuceneIndexManager indexManager,
-    ILuceneSearchService searchService) : IRecipeSearchService
+public class RecipeSearchService(RecipeIndex index) : IRecipeSearchService
 {
     // Suffix under which the average rating is stored (the un-suffixed key is the sort-only DoubleDocValuesField).
     private const string AverageRatingStoredSuffix = "_v";
@@ -35,8 +31,7 @@ public class RecipeSearchService(
     public RecipeSearchResults Search(RecipeSearchCriteria rawCriteria)
     {
         var criteria = rawCriteria.Normalized();
-        var index = indexManager.GetRequiredIndex(RecipeSearchConstants.IndexName);
-        var facetsConfig = BuildFacetsConfig();
+        var facetsConfig = RecipeFacets.Config();
         var baseQuery = BuildQuery(criteria);

         var drill = new DrillDownQuery(facetsConfig, baseQuery);
@@ -52,8 +47,10 @@ public class RecipeSearchService(

         var sort = BuildSort(criteria);

-        return searchService.UseSearcherWithDrillSideways(index, (searcher, drillSideways) =>
+        return index.Search((searcher, taxonomy) =>
         {
+            var drillSideways = new DrillSideways(searcher, facetsConfig, taxonomy);
+
             // Request the whole index so the returned TopDocs holds every match: both paging and the spotlight
             // scan need the complete hit set, and DrillSideways clamps the requested count to MaxDoc anyway.
             var topN = Math.Max(searcher.IndexReader.MaxDoc, 1);
@@ -100,16 +97,6 @@ public class RecipeSearchService(
         });
     }

-    private static FacetsConfig BuildFacetsConfig()
-    {
-        // Must mirror RecipeSearchIndexingStrategy.FacetsConfigFactory so the drill-down terms are encoded the
-        // same way they were indexed; both dimensions were declared multi-valued there.
-        var config = new FacetsConfig();
-        config.SetMultiValued(RecipeSearchConstants.FacetCategory, true);
-        config.SetMultiValued(RecipeSearchConstants.FacetDiet, true);
-        return config;
-    }
-
     private static Query BuildQuery(RecipeSearchCriteria criteria)
     {
         var textQuery = BuildTextQuery(criteria.Query);
```

- [ ] **Step 6: Run the tests**

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj
```

Expected: `Build succeeded` with 0 warnings, and every unit test passes. That includes the 15 new cases in
`RecipeSearchServiceTests`, the 2 in `RecipeIndexTests`, and the unchanged `RecipeSearchCriteriaTests` and
`RecipeSearchDocumentTests`.

- [ ] **Step 7: Commit**

```bash
git add -A src/KCC.Web/Features/Search src/KCC.Web/KCC.Web.csproj tests/KCC.UnitTests
git commit -m "Hold the Recipe Index in Memory"
```

---

### Task 3: Search documents from the published tree

Spec §6.4 keeps Umbraco's read APIs inside the query services, so the recipe aggregate's service learns to list
every published recipe. A pure mapping turns each one into the unchanged `RecipeSearchDocument`:
- its rating covers its published variants only (spec §9.1);
- its tags and ingredients are the union across those variants;
- its "published" date is the node's create date, as with `article:published_time` (spec §7).

`RecipeIndexSource` wraps the reads that feed a rebuild. It is registered in Task 5.

**Files:**
- Modify: `src/KCC.Web/Features/Recipes/RecipeQueries.cs`,
  `tests/KCC.IntegrationTests/Features/Recipes/RecipeQueriesTests.cs`
- Create:
  - `src/KCC.Web/Features/Search/{RecipeSearchDocuments,RecipeIndexSource}.cs`
  - `tests/KCC.UnitTests/Features/Search/RecipeSearchDocumentsTests.cs`
  - `tests/KCC.IntegrationTests/Features/Search/RecipeIndexSourceTests.cs`

**Interfaces:**
- Consumes:
  - `IRecipeQueries`, `RecipePageData`, `RecipeRecord` and `VariantRecord` (Phase 2)
  - `IContributionStats.GetAsync` and `ContributionStats.RatingAcross` (Phase 2)
  - `IAuthorNameProvider.ResolveMany` and `AuthorNameProvider.NameFor` (Phase 2)
  - `IngredientViewModel`, and `KCC.Web.Features.Helpers.JsonSerializer.DeserializeCollection<T>`
- Produces:
  - `IRecipeQueries.GetPublishedRecipes() → IReadOnlyList<RecipePageData>`. It lists every published recipe under a
    published listing under a published home, each with its published variants.
  - `IRecipeQueries.GetCreateRecipeUrl(RecipeListingPage listing) → string`, which is null when the listing has no
    published create-recipe page.
  - `RecipeQueries(IPublishedContentQuery contentQuery)`, still registered scoped.
  - `RecipeSearchDocuments.From(RecipePageData page, ContributionStats stats, IReadOnlyDictionary<Guid, string> authorNames) → RecipeSearchDocument`
    and `RecipeSearchDocuments.AuthorKeys(IEnumerable<RecipePageData> recipes) → IEnumerable<Guid>`.
  - `IRecipeIndexSource.LoadAsync() → Task<IReadOnlyList<RecipeSearchDocument>>`, implemented by
    `RecipeIndexSource(IUmbracoContextFactory, IRecipeQueries, IContributionStats, IAuthorNameProvider)`.

- [ ] **Step 1: Write the failing mapping tests**

Create `tests/KCC.UnitTests/Features/Search/RecipeSearchDocumentsTests.cs`:

```csharp
using KCC.Contributions;
using KCC.Web.Features.Recipes;
using KCC.Web.Features.Search;

namespace KCC.UnitTests.Features.Search;

public class RecipeSearchDocumentsTests
{
    private static readonly IReadOnlyDictionary<Guid, string> NoNames = new Dictionary<Guid, string>();

    [Test]
    public async Task From_RatesTheRecipeAcrossTheVariantsItHolds()
    {
        var first = Variant("Classic Stack");
        var second = Variant("Blueberry Stack");
        var elsewhere = Guid.NewGuid();
        var stats = ContributionStats.Build([(first.Key, 5m), (second.Key, 3m), (elsewhere, 1m)], []);

        var document = RecipeSearchDocuments.From(Page(Recipe(), first, second), stats, NoNames);

        _ = await Assert.That(document.AverageRating).IsEqualTo(4d);
        _ = await Assert.That(document.ReviewCount).IsEqualTo(2);
    }

    [Test]
    public async Task From_TakesEachTagAndIngredientOnce()
    {
        var first = Variant("Classic Stack", tags: ["Vegan", "Spicy"], ingredientsJson: """[{"name":"Tofu"},{"name":"Chili Oil"}]""");
        var second = Variant("Blueberry Stack", tags: ["Vegan"], ingredientsJson: """[{"name":"Tofu"},{"name":" "}]""");

        var document = RecipeSearchDocuments.From(Page(Recipe(), first, second), ContributionStats.Build([], []), NoNames);

        _ = await Assert.That(string.Join(",", document.Diets)).IsEqualTo("Vegan,Spicy");
        _ = await Assert.That(string.Join(",", document.IngredientNames)).IsEqualTo("Tofu,Chili Oil");
    }

    [Test]
    public async Task From_TimesTheRecipeByItsFastestVariant()
    {
        var page = Page(Recipe(), Variant("Classic Stack", prep: 10, cook: 15), Variant("Blueberry Stack", prep: 5, cook: 5));

        var document = RecipeSearchDocuments.From(page, ContributionStats.Build([], []), NoNames);

        _ = await Assert.That(document.FastestTime).IsEqualTo(10);
        _ = await Assert.That(document.VariantCount).IsEqualTo(2);
    }

    [Test]
    public async Task From_CarriesTheRecipeCard()
    {
        var priya = Guid.NewGuid();
        var names = new Dictionary<Guid, string> { [priya] = "Priya Balan" };

        var document = RecipeSearchDocuments.From(Page(Recipe(priya)), ContributionStats.Build([], []), names);

        _ = await Assert.That(document.Name).IsEqualTo("Fluffy Buttermilk Pancakes");
        _ = await Assert.That(document.Slug).IsEqualTo("/recipes/fluffy-buttermilk-pancakes/");
        _ = await Assert.That(document.Icon).IsEqualTo("fa-duotone fa-pancakes");
        _ = await Assert.That(document.Category).IsEqualTo("Breakfast");
        _ = await Assert.That(document.Description).IsEqualTo("Tall, tender stacks.");
        _ = await Assert.That(document.StartedBy).IsEqualTo("Priya Balan");
        _ = await Assert.That(document.PublishedUnixSeconds).IsEqualTo(1_789_862_400L);
    }

    [Test]
    public async Task From_LeavesStartedByEmptyWithoutAnAuthor()
    {
        var document = RecipeSearchDocuments.From(Page(Recipe()), ContributionStats.Build([], []), NoNames);

        _ = await Assert.That(document.StartedBy).IsEqualTo(string.Empty);
        _ = await Assert.That(document.VariantCount).IsEqualTo(0);
        _ = await Assert.That(document.FastestTime).IsEqualTo(0);
    }

    [Test]
    public async Task From_SkipsIngredientsItCannotRead()
    {
        var page = Page(Recipe(), Variant("Classic Stack", ingredientsJson: "flour, eggs"));

        var document = RecipeSearchDocuments.From(page, ContributionStats.Build([], []), NoNames);

        _ = await Assert.That(document.IngredientNames.Count).IsEqualTo(0);
        _ = await Assert.That(document.VariantCount).IsEqualTo(1);
    }

    [Test]
    public async Task AuthorKeys_ListsEachAuthorOnceAndSkipsBlanks()
    {
        var priya = Guid.NewGuid();
        var pages = new[] { Page(Recipe(priya)), Page(Recipe()), Page(Recipe(priya)) };

        _ = await Assert.That(string.Join(",", RecipeSearchDocuments.AuthorKeys(pages))).IsEqualTo(priya.ToString());
    }

    private static RecipePageData Page(RecipeRecord recipe, params VariantRecord[] variants) => new(recipe, variants, "/recipes/add-variant/");

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

    private static VariantRecord Variant(
        string name,
        int prep = 10,
        int cook = 15,
        IReadOnlyList<string> tags = null,
        string ingredientsJson = "[]") => new(
        Guid.NewGuid(),
        name,
        $"/recipes/fluffy-buttermilk-pancakes/{name.ToLowerInvariant().Replace(' ', '-')}/",
        "Griddle to golden.",
        "fa-duotone fa-pancakes",
        null,
        prep,
        cook,
        4,
        null,
        new NutritionRecord(null, null, null, null, null, null, null, null),
        tags ?? ["Vegetarian"],
        ingredientsJson,
        "[]",
        null,
        new DateTime(2026, 9, 20, 0, 1, 0, DateTimeKind.Utc));
}
```

1,789,862,400 is 2026-09-20T00:00:00Z in Unix seconds.

- [ ] **Step 2: Write the failing query and source tests**

`RecipeQueries` gains a constructor parameter, so `RecipeQueriesTests` resolves it from a scope instead of newing
it. In `tests/KCC.IntegrationTests/Features/Recipes/RecipeQueriesTests.cs`:

1. Replace every `WithContent(content => new RecipeQueries().` with `WithContent((content, queries) => queries.`
   (five places):

```bash
sed -i '' 's/WithContent(content => new RecipeQueries()\./WithContent((content, queries) => queries./' \
  tests/KCC.IntegrationTests/Features/Recipes/RecipeQueriesTests.cs
grep -c "WithContent((content, queries) => queries\." tests/KCC.IntegrationTests/Features/Recipes/RecipeQueriesTests.cs
```

   Expected: `5`.

2. Replace the `WithContent` helper with:

```csharp
    private T WithContent<T>(Func<IPublishedContentCache, IRecipeQueries, T> read)
    {
        using var context = Site.Services.GetRequiredService<IUmbracoContextFactory>().EnsureUmbracoContext();
        using var scope = Site.Services.CreateScope();
        return read(context.UmbracoContext.Content!, scope.ServiceProvider.GetRequiredService<IRecipeQueries>());
    }
```

3. Add these two tests before the helper:

```csharp
    [Test]
    public async Task GetPublishedRecipes_ListsEveryPublishedRecipeWithItsVariants()
    {
        var withdrawn = await TestContent.RecipeAsync(Site.Services, "IT Never Listed");
        await TestContent.UnpublishAsync(Site.Services, withdrawn);

        var recipes = WithContent((_, queries) => queries.GetPublishedRecipes());

        _ = await Assert.That(recipes.Count(page => page.Recipe.Key == SeedKeys.Recipe("Spicy Ramen Flight"))).IsEqualTo(1);
        _ = await Assert.That(recipes.Single(page => page.Recipe.Key == SeedKeys.Recipe("Spicy Ramen Flight")).Variants.Count).IsEqualTo(4);
        _ = await Assert.That(recipes.Count(page => SeedKeys.Recipe(page.Recipe.Name) == page.Recipe.Key)).IsEqualTo(25);
        _ = await Assert.That(recipes.Any(page => page.Recipe.Key == withdrawn)).IsFalse();
    }

    [Test]
    public async Task GetCreateRecipeUrl_FindsTheWizardUnderTheListing()
    {
        var url = WithContent((content, queries) => queries.GetCreateRecipeUrl(
            (RecipeListingPage)content.GetById(TestContent.RecipeListing(Site.Services))!));

        _ = await Assert.That(url).IsEqualTo("/recipes/create-recipe/");
    }

```

Create `tests/KCC.IntegrationTests/Features/Search/RecipeIndexSourceTests.cs`. The source is not registered until
Task 5, so the test builds it from the container:

```csharp
using KCC.IntegrationTests.Config;
using KCC.Web.Features.Search;
using Microsoft.Extensions.DependencyInjection;

namespace KCC.IntegrationTests.Features.Search;

public class RecipeIndexSourceTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task Load_ReadsASeededRecipeWithItsVariantsAuthorAndRating()
    {
        var ramen = (await LoadAsync()).Single(document => document.Name == "Spicy Ramen Flight");

        _ = await Assert.That(ramen.Slug).IsEqualTo("/recipes/spicy-ramen-flight/");
        _ = await Assert.That(ramen.Category).IsEqualTo("Lunch");
        _ = await Assert.That(ramen.StartedBy).IsEqualTo("Priya Balan");
        _ = await Assert.That(string.Join(",", ramen.Diets)).IsEqualTo("Spicy,Vegan,High-Protein,Dairy-Free");
        _ = await Assert.That(ramen.IngredientNames.Contains("Chili Oil")).IsTrue();
        _ = await Assert.That(ramen.FastestTime).IsEqualTo(20);
        _ = await Assert.That(ramen.VariantCount).IsEqualTo(4);
        _ = await Assert.That(ramen.AverageRating).IsEqualTo(4.5d);
        _ = await Assert.That(ramen.ReviewCount).IsEqualTo(3);
    }

    [Test]
    public async Task Load_KeepsARecipeWithNoVariants()
    {
        var board = (await LoadAsync()).Single(document => document.Name == "Bare Cupboard Snack Board");

        _ = await Assert.That(board.VariantCount).IsEqualTo(0);
        _ = await Assert.That(board.FastestTime).IsEqualTo(0);
        _ = await Assert.That(board.StartedBy).IsEqualTo(string.Empty);
    }

    private async Task<IReadOnlyList<RecipeSearchDocument>> LoadAsync()
    {
        using var scope = Site.Services.CreateScope();
        return await ActivatorUtilities.CreateInstance<RecipeIndexSource>(scope.ServiceProvider).LoadAsync();
    }
}
```

```bash
dotnet build KitchenCommandCenter.sln
```

Expected: build errors. `RecipeSearchDocuments`, `RecipeIndexSource`, `GetPublishedRecipes` and
`GetCreateRecipeUrl` do not exist.

- [ ] **Step 3: List the published recipes**

In `src/KCC.Web/Features/Recipes/RecipeQueries.cs`:
1. Add `using Umbraco.Cms.Core;`, keeping the usings sorted.
2. Add to the interface, after `GetVariantPage`:

```csharp

    IReadOnlyList<RecipePageData> GetPublishedRecipes();

    string GetCreateRecipeUrl(RecipeListingPage listing);
```

3. Replace `public class RecipeQueries : IRecipeQueries` with
   `public class RecipeQueries(IPublishedContentQuery contentQuery) : IRecipeQueries`.
4. Add these members after `GetVariantPage`, before the private `RecipeFrom`:

```csharp
    public IReadOnlyList<RecipePageData> GetPublishedRecipes() =>
        contentQuery.ContentAtRoot()
            .OfType<HomePage>()
            .SelectMany(home => home.Children<RecipeListingPage>())
            .SelectMany(listing => listing.Children<Recipe>())
            .Select(GetRecipePage)
            .ToList();

    public string GetCreateRecipeUrl(RecipeListingPage listing) =>
        listing.Children<CreateRecipePage>().FirstOrDefault()?.Url();

```

`Children<T>()` returns only published children, and a published node under an unpublished parent is not in the
published cache at all. So the listing reaches exactly the recipes a visitor can open.

- [ ] **Step 4: Map recipes to documents, and read them for a rebuild**

Create `src/KCC.Web/Features/Search/RecipeSearchDocuments.cs`:

```csharp
using KCC.Contributions;
using KCC.Web.Features.Helpers;
using KCC.Web.Features.Pages.VariantDetail;
using KCC.Web.Features.Providers;
using KCC.Web.Features.Recipes;

namespace KCC.Web.Features.Search;

public static class RecipeSearchDocuments
{
    public static IEnumerable<Guid> AuthorKeys(IEnumerable<RecipePageData> recipes) =>
        recipes.Select(page => page.Recipe.AuthorKey).OfType<Guid>().Distinct();

    public static RecipeSearchDocument From(RecipePageData page, ContributionStats stats, IReadOnlyDictionary<Guid, string> authorNames)
    {
        var recipe = page.Recipe;
        var rating = stats.RatingAcross(page.Variants.Select(variant => variant.Key));

        return new RecipeSearchDocument
        {
            Name = recipe.Name ?? string.Empty,
            Slug = recipe.Url ?? string.Empty,
            Icon = recipe.Icon ?? string.Empty,
            Category = recipe.Category ?? string.Empty,
            StartedBy = AuthorNameProvider.NameFor(authorNames, recipe.AuthorKey) ?? string.Empty,
            Description = recipe.Description ?? string.Empty,
            Diets = page.Variants.SelectMany(variant => variant.Tags).Distinct().ToArray(),
            IngredientNames = page.Variants
                .SelectMany(variant => IngredientNames(variant.IngredientsJson))
                .Distinct()
                .ToArray(),
            FastestTime = RecipeSearchDocument.FastestOf(
                page.Variants.Select(variant => (variant.PrepTime, variant.CookTime)).ToArray()),
            VariantCount = page.Variants.Count,
            AverageRating = rating.Average,
            ReviewCount = rating.Count,
            PublishedUnixSeconds = new DateTimeOffset(recipe.CreateDate.ToUniversalTime()).ToUnixTimeSeconds(),
        };
    }

    // The owner can type this JSON by hand in the backoffice; one slip must not take every recipe out of search.
    private static IEnumerable<string> IngredientNames(string json)
    {
        try
        {
            return JsonSerializer.DeserializeCollection<IngredientViewModel>(json)
                .Select(ingredient => ingredient.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToList();
        }
        catch (System.Text.Json.JsonException)
        {
            return [];
        }
    }
}
```

Create `src/KCC.Web/Features/Search/RecipeIndexSource.cs`:

```csharp
using KCC.Contributions;
using KCC.Web.Features.Providers;
using KCC.Web.Features.Recipes;
using Umbraco.Cms.Core.Web;

namespace KCC.Web.Features.Search;

public interface IRecipeIndexSource
{
    Task<IReadOnlyList<RecipeSearchDocument>> LoadAsync();
}

public class RecipeIndexSource(
    IUmbracoContextFactory umbracoContextFactory,
    IRecipeQueries recipes,
    IContributionStats contributionStats,
    IAuthorNameProvider authorNames) : IRecipeIndexSource
{
    public async Task<IReadOnlyList<RecipeSearchDocument>> LoadAsync()
    {
        IReadOnlyList<RecipePageData> pages;

        // A rebuild runs outside any request, and published URLs are built from an Umbraco context.
        using (umbracoContextFactory.EnsureUmbracoContext())
        {
            pages = recipes.GetPublishedRecipes();
        }

        var stats = await contributionStats.GetAsync();
        var names = await authorNames.ResolveMany(RecipeSearchDocuments.AuthorKeys(pages));
        return pages.Select(page => RecipeSearchDocuments.From(page, stats, names)).ToList();
    }
}
```

- [ ] **Step 5: Run the tests**

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj -- --treenode-filter "/*/*/RecipeSearchDocumentsTests/*"
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj
```

Expected: 0 warnings, 7 passed, and then every integration test passes, including the 2 new `RecipeQueriesTests`
and the 2 `RecipeIndexSourceTests`.

- [ ] **Step 6: Commit**

```bash
git add -A src/KCC.Web/Features/Recipes src/KCC.Web/Features/Search tests/KCC.UnitTests tests/KCC.IntegrationTests
git commit -m "Read Recipe Search Documents from the Published Tree"
```

---

### Task 4: Coalesce rebuilds

Spec §9.2's rule is that any relevant change rebuilds the whole index. A rebuild runs about two seconds after the
last signal of a burst, and one runs at startup. This task writes the scheduler and tests it with a fake source.
Task 5 wires it into the site.

**Files:**
- Create: `src/KCC.Web/Features/Search/{RecipeSearchOptions,RecipeIndexRebuilder}.cs`,
  `tests/KCC.UnitTests/Features/Search/RecipeIndexRebuilderTests.cs`

**Interfaces:**
- Consumes: `RecipeIndex` and `RecipeIndexBuilder` (Task 2), `IRecipeIndexSource` (Task 3), and Umbraco's
  `IRuntimeState`.
- Produces:
  - `RecipeSearchOptions`, with `TimeSpan RebuildDelay`, defaulting to 2 seconds. Task 5 binds it from the
    `RecipeSearch` configuration section.
  - `IRecipeIndexRebuilder`, with `void Signal()` and `Task WhenCurrentAsync(CancellationToken cancellationToken)`.
  - `sealed class RecipeIndexRebuilder(IServiceScopeFactory, RecipeIndex, IRuntimeState, IOptions<RecipeSearchOptions>, ILogger<RecipeIndexRebuilder>) : BackgroundService, IRecipeIndexRebuilder`.
    Each rebuild logs `Rebuilt the recipe index with {RecipeCount} recipes in {ElapsedMs:0} ms`.

- [ ] **Step 1: Write the failing tests**

Create `tests/KCC.UnitTests/Features/Search/RecipeIndexRebuilderTests.cs`:

```csharp
using KCC.Web.Features.Search;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;

namespace KCC.UnitTests.Features.Search;

public class RecipeIndexRebuilderTests
{
    [Test]
    public async Task Start_BuildsTheIndexUnasked()
    {
        var source = new FakeSource("Chili");
        await using var site = await StartAsync(source);

        await site.Rebuilder.WhenCurrentAsync(CancellationToken.None);

        _ = await Assert.That(source.Loads).IsEqualTo(1);
        _ = await Assert.That(site.RecipeCount()).IsEqualTo(1);
    }

    [Test]
    public async Task ABurstOfSignals_CostsOneRebuild()
    {
        var source = new FakeSource("Chili");
        await using var site = await StartAsync(source);
        await site.Rebuilder.WhenCurrentAsync(CancellationToken.None);

        for (var signal = 0; signal < 5; signal++)
        {
            site.Rebuilder.Signal();
        }

        await site.Rebuilder.WhenCurrentAsync(CancellationToken.None);

        _ = await Assert.That(source.Loads).IsEqualTo(2);
    }

    [Test]
    public async Task WhenCurrent_WaitsForTheSignalledChange()
    {
        var source = new FakeSource("Chili");
        await using var site = await StartAsync(source);
        await site.Rebuilder.WhenCurrentAsync(CancellationToken.None);

        source.Names = ["Chili", "Stew"];
        site.Rebuilder.Signal();
        await site.Rebuilder.WhenCurrentAsync(CancellationToken.None);

        _ = await Assert.That(site.RecipeCount()).IsEqualTo(2);
    }

    [Test]
    public async Task AFailedRebuild_KeepsTheLastIndexAndTheNextSignalRetries()
    {
        var source = new FakeSource("Chili");
        await using var site = await StartAsync(source);
        await site.Rebuilder.WhenCurrentAsync(CancellationToken.None);

        source.Names = ["Chili", "Stew"];
        source.Failure = new InvalidOperationException("The published cache is unavailable.");
        site.Rebuilder.Signal();

        _ = await Assert.That(async () => await site.Rebuilder.WhenCurrentAsync(CancellationToken.None))
            .Throws<InvalidOperationException>();
        _ = await Assert.That(site.RecipeCount()).IsEqualTo(1);

        source.Failure = null;
        site.Rebuilder.Signal();
        await site.Rebuilder.WhenCurrentAsync(CancellationToken.None);

        _ = await Assert.That(site.RecipeCount()).IsEqualTo(2);
    }

    [Test]
    public async Task BeforeInstall_BuildsNothing()
    {
        var source = new FakeSource("Chili");
        await using var site = await StartAsync(source, RuntimeLevel.Install);

        await site.Rebuilder.WhenCurrentAsync(CancellationToken.None);

        _ = await Assert.That(source.Loads).IsEqualTo(0);
    }

    private static async Task<RunningRebuilder> StartAsync(FakeSource source, RuntimeLevel level = RuntimeLevel.Run)
    {
        var services = new ServiceCollection().AddScoped<IRecipeIndexSource>(_ => source).BuildServiceProvider();
        var runtimeState = new Mock<IRuntimeState>();
        runtimeState.Setup(state => state.Level).Returns(level);
        var index = new RecipeIndex();
        var rebuilder = new RecipeIndexRebuilder(
            services.GetRequiredService<IServiceScopeFactory>(),
            index,
            runtimeState.Object,
            Options.Create(new RecipeSearchOptions { RebuildDelay = TimeSpan.FromMilliseconds(50) }),
            NullLogger<RecipeIndexRebuilder>.Instance);
        await rebuilder.StartAsync(CancellationToken.None);
        return new RunningRebuilder(rebuilder, index, services);
    }

    private sealed class RunningRebuilder(RecipeIndexRebuilder rebuilder, RecipeIndex index, ServiceProvider services) : IAsyncDisposable
    {
        public RecipeIndexRebuilder Rebuilder => rebuilder;

        public int RecipeCount() => index.Search((searcher, _) => searcher.IndexReader.NumDocs);

        public async ValueTask DisposeAsync()
        {
            await rebuilder.StopAsync(CancellationToken.None);
            rebuilder.Dispose();
            index.Dispose();
            await services.DisposeAsync();
        }
    }

    private sealed class FakeSource(params string[] names) : IRecipeIndexSource
    {
        private int loads;

        public string[] Names { get; set; } = names;

        public Exception Failure { get; set; }

        public int Loads => Volatile.Read(ref loads);

        public Task<IReadOnlyList<RecipeSearchDocument>> LoadAsync()
        {
            Interlocked.Increment(ref loads);
            return Failure is not null
                ? Task.FromException<IReadOnlyList<RecipeSearchDocument>>(Failure)
                : Task.FromResult<IReadOnlyList<RecipeSearchDocument>>(Names.Select(name => new RecipeSearchDocument { Name = name }).ToList());
        }
    }
}
```

```bash
dotnet build KitchenCommandCenter.sln
```

Expected: build errors — `RecipeIndexRebuilder` and `RecipeSearchOptions` do not exist.

- [ ] **Step 2: Write the scheduler**

Create `src/KCC.Web/Features/Search/RecipeSearchOptions.cs`:

```csharp
namespace KCC.Web.Features.Search;

public class RecipeSearchOptions
{
    public TimeSpan RebuildDelay { get; set; } = TimeSpan.FromSeconds(2);
}
```

Create `src/KCC.Web/Features/Search/RecipeIndexRebuilder.cs`:

```csharp
using System.Diagnostics;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;

namespace KCC.Web.Features.Search;

public interface IRecipeIndexRebuilder
{
    void Signal();

    Task WhenCurrentAsync(CancellationToken cancellationToken);
}

public sealed class RecipeIndexRebuilder(
    IServiceScopeFactory scopeFactory,
    RecipeIndex index,
    IRuntimeState runtimeState,
    IOptions<RecipeSearchOptions> options,
    ILogger<RecipeIndexRebuilder> logger) : BackgroundService, IRecipeIndexRebuilder
{
    private readonly SemaphoreSlim wake = new(0);
    private readonly Lock gate = new();
    private readonly List<(long Generation, TaskCompletionSource Done)> waiters = [];
    private long requested;
    private long built;

    public void Signal()
    {
        Interlocked.Increment(ref requested);
        wake.Release();
    }

    public Task WhenCurrentAsync(CancellationToken cancellationToken)
    {
        lock (gate)
        {
            var generation = Interlocked.Read(ref requested);
            if (built >= generation)
            {
                return Task.CompletedTask;
            }

            var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            waiters.Add((generation, done));
            return done.Task.WaitAsync(cancellationToken);
        }
    }

    // Signalled here rather than in ExecuteAsync, which .NET 10 starts on a background thread: anyone waiting from
    // start-up on must wait for the first build.
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        Signal();
        return base.StartAsync(cancellationToken);
    }

    public override void Dispose()
    {
        wake.Dispose();
        base.Dispose();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // The first build skips the quiet period, so a restarted site is searchable at once.
        var quietPeriod = TimeSpan.Zero;
        while (!stoppingToken.IsCancellationRequested)
        {
            await wake.WaitAsync(stoppingToken);

            // Saves arrive in bursts (a publish with descendants, the seeder); each new signal restarts the quiet
            // period, so a burst costs one rebuild.
            while (await wake.WaitAsync(quietPeriod, stoppingToken))
            {
            }

            quietPeriod = options.Value.RebuildDelay;
            await RebuildAsync(Interlocked.Read(ref requested), stoppingToken);
        }
    }

    private async Task RebuildAsync(long generation, CancellationToken stoppingToken)
    {
        try
        {
            // An install or upgrade boot has no content to read yet.
            if (runtimeState.Level == RuntimeLevel.Run)
            {
                var started = Stopwatch.GetTimestamp();
                using var scope = scopeFactory.CreateScope();
                var documents = await scope.ServiceProvider.GetRequiredService<IRecipeIndexSource>().LoadAsync();
                index.Replace(RecipeIndexBuilder.Build(documents));
                logger.LogInformation(
                    "Rebuilt the recipe index with {RecipeCount} recipes in {ElapsedMs:0} ms",
                    documents.Count,
                    Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            }

            Complete(generation, null);
        }
        catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
        {
            // An unhandled exception would stop the host. The previous index keeps serving, and the next change retries.
            logger.LogError(exception, "Rebuilding the recipe index failed");
            Complete(generation, exception);
        }
    }

    private void Complete(long generation, Exception failure)
    {
        lock (gate)
        {
            if (failure is null)
            {
                built = Math.Max(built, generation);
            }

            foreach (var waiter in waiters.Where(waiter => waiter.Generation <= generation).ToList())
            {
                if (failure is null)
                {
                    waiter.Done.TrySetResult();
                }
                else
                {
                    waiter.Done.TrySetException(failure);
                }

                waiters.Remove(waiter);
            }
        }
    }
}
```

How it behaves:
- Every `Signal` bumps a generation and releases the semaphore. The loop drains the semaphore until it has been
  quiet for `RebuildDelay`, then builds everything signalled so far.
- `WhenCurrentAsync` records the generation current at the call, and completes once a build that covers it has
  finished.
- A failed build faults the waiters it covered and leaves the previous snapshot serving. The next signal retries.

- [ ] **Step 3: Run the tests, three times**

```bash
dotnet build KitchenCommandCenter.sln
for run in 1 2 3; do
  dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj -- --treenode-filter "/*/*/RecipeIndexRebuilderTests/*"
done
```

Expected: 5 passed, on every run. They time real delays (50 ms), so a flaky pass is a bug: look at the ordering of
`Signal`, `requested` and `Complete`, not at the delay.

- [ ] **Step 4: Commit**

```bash
git add src/KCC.Web/Features/Search tests/KCC.UnitTests/Features/Search
git commit -m "Coalesce Recipe Index Rebuilds"
```

---

### Task 5: Rebuild on every change, and seed a searchable site

This task registers the index and its rebuilder, and connects the three signals:
- **Content:** `ContentCacheRefresherNotification` fires for every tree change: publish, unpublish, trash, move
  and delete, and for drafts too. Recipe URLs, categories and tags live on other nodes, so any content change can
  change a document, and a rebuild costs milliseconds. There is therefore no filter.
- **Members:** member saves and deletes, for author names.
- **Reviews:** a new notification that every review write publishes.

The dev seeder then waits for the index, so a fixture that seeds can search at once.

**Files:**
- Create:
  - `src/KCC.Contributions/ReviewsChangedNotification.cs`
  - `src/KCC.Web/Features/Search/{RecipeIndexTriggers,SearchComposer}.cs`
  - `tests/KCC.IntegrationTests/Features/Search/{RecipeSearchTests,RecipeIndexTriggerTests,RecipeIndexSwapTests}.cs`
- Modify:
  - `src/KCC.Contributions/ContributionWrites.cs`
  - `src/KCC.Web/Features/DevTools/RecipeSeed/DevSeedApiController.cs`
  - `tests/KCC.IntegrationTests/Config/{UmbracoSite,TestContent}.cs`

**Interfaces:**
- Consumes: Tasks 2–4.
- Produces:
  - `KCC.Contributions.ReviewsChangedNotification : INotification`, a record with no members, published by every
    review write.
  - `RecipeIndexTriggers`, handling `ContentCacheRefresherNotification`, `MemberSavedNotification`,
    `MemberDeletedNotification` and `ReviewsChangedNotification`.
  - `SearchComposer`, which registers:
    - `RecipeSearchOptions`, from `RecipeSearch`;
    - `RecipeIndex` and `IRecipeSearchService`, as singletons;
    - `IRecipeIndexSource`, scoped;
    - `RecipeIndexRebuilder`, as one singleton that is also `IRecipeIndexRebuilder` and a hosted service;
    - the four handlers.
  - `POST /api/dev/seed-recipes` answers only once the index holds what it seeded.
  - Test helpers on `TestContent`:
    - `Pick(string alias, Guid documentKey) → PropertyValueModel`
    - `Author(Guid memberKey) → PropertyValueModel`
    - `CategoryAsync(IServiceProvider, string name) → Task<Guid>`
    - `TagAsync(IServiceProvider, string name) → Task<Guid>`
    - `TrashAsync(IServiceProvider, Guid key) → Task`
    - `AuthorAsync(IServiceProvider, string userName, string firstName, string lastName) → Task<Guid>`, which
      returns the member key
    - `RenameAuthor(IServiceProvider, Guid memberKey, string firstName, string lastName)`

- [ ] **Step 1: Give the tests what they need**

In `tests/KCC.IntegrationTests/Config/UmbracoSite.cs`, add this entry at the end of `Settings()`:

```csharp

        // Tests wait for each rebuild, so a short quiet period keeps the suite quick.
        ["RecipeSearch:RebuildDelay"] = "00:00:00.100",
```

In `tests/KCC.IntegrationTests/Config/TestContent.cs`:
1. Add `using System.Security.Cryptography;` and `using System.Text.Json.Nodes;`, keeping the usings sorted.
2. Add these methods before `RenameAsync`:

```csharp
    public static PropertyValueModel Pick(string alias, Guid documentKey) => new()
    {
        Alias = alias,
        Value = new JsonArray(new JsonObject { ["type"] = "document", ["unique"] = documentKey.ToString() }),
    };

    public static PropertyValueModel Author(Guid memberKey) => new() { Alias = "author", Value = memberKey.ToString() };

    public static Task<Guid> CategoryAsync(IServiceProvider services, string name) =>
        PublishedAsync(services, "recipeCategory", name, Folder(services, "Recipe Categories"), []);

    public static Task<Guid> TagAsync(IServiceProvider services, string name) =>
        PublishedAsync(services, "recipeTag", name, Folder(services, "Recipe Tags"), []);

    public static async Task TrashAsync(IServiceProvider services, Guid key)
    {
        using var scope = services.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<IContentEditingService>()
            .MoveToRecycleBinAsync(key, Constants.Security.SuperUserKey);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Trashing {key} failed: {result.Status}.");
        }
    }

    public static async Task<Guid> AuthorAsync(IServiceProvider services, string userName, string firstName, string lastName)
    {
        using var scope = services.CreateScope();
        var scoped = scope.ServiceProvider;
        var superUser = await scoped.GetRequiredService<IUserService>().GetAsync(Constants.Security.SuperUserKey)
            ?? throw new InvalidOperationException("The super user is missing.");
        var created = await scoped.GetRequiredService<IMemberEditingService>().CreateAsync(
            new MemberCreateModel
            {
                Key = Guid.NewGuid(),
                ContentTypeKey = scoped.GetRequiredService<IMemberTypeService>().Get(Constants.Security.DefaultMemberTypeAlias)!.Key,
                Username = userName,
                Email = $"{userName}@example.test",
                Password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)),
                IsApproved = true,
                Variants = [new VariantModel { Name = $"{firstName} {lastName}" }],
                Properties =
                [
                    new PropertyValueModel { Alias = "firstName", Value = firstName },
                    new PropertyValueModel { Alias = "lastName", Value = lastName },
                ],
            },
            superUser);
        if (!created.Success)
        {
            throw new InvalidOperationException($"Creating member {userName} failed: {created.Status.MemberEditingOperationStatus}.");
        }

        return created.Result.Content!.Key;
    }

    public static void RenameAuthor(IServiceProvider services, Guid memberKey, string firstName, string lastName)
    {
        using var scope = services.CreateScope();
        var memberService = scope.ServiceProvider.GetRequiredService<IMemberService>();
        var member = memberService.GetById(memberKey) ?? throw new InvalidOperationException($"No member {memberKey}.");
        member.SetValue("firstName", firstName);
        member.SetValue("lastName", lastName);
        memberService.Save(member);
    }
```

3. Add this method before the private `PublishedAsync`:

```csharp
    private static Guid Folder(IServiceProvider services, string name)
    {
        services.GetRequiredService<IDocumentNavigationQueryService>().TryGetRootKeysOfType("contentFolder", out var folders);
        return services.GetRequiredService<IContentService>().GetByIds(folders).Single(folder => folder.Name == name).Key;
    }
```

The helpers follow the seeder's shapes: a tree picker takes a `JsonArray` of `{ type, unique }`, and a member picker
takes the member key as a string. If reconciliation item 12 applies, make the `TemplateKey` change in
`PublishedAsync` now. Otherwise categories and tags fail to create.

- [ ] **Step 2: Write the failing tests**

These search the seeded coverage matrix (`RecipeSeedData`). Every expected value below is derived from that file.

Create `tests/KCC.IntegrationTests/Features/Search/RecipeSearchTests.cs`:

```csharp
using KCC.IntegrationTests.Config;
using KCC.Web.Features.Search;
using Microsoft.Extensions.DependencyInjection;

namespace KCC.IntegrationTests.Features.Search;

// Other tests add recipes of their own, with unique names and never a seeded category or tag, and one of them is
// rated 5.0. So these checks against the seed (RecipeSeedData) filter by category, diet or a seeded word, and never
// count or spotlight the whole site.
public class RecipeSearchTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    [Arguments("tahini", "Weeknight Bowls")]
    [Arguments("midnight", "Cold Brew Concentrate")]
    [Arguments("Salazar", "Cold Brew Concentrate,Shakshuka")]
    public async Task Query_FindsAWordInAnIngredientDescriptionOrAuthor(string query, string expected)
    {
        var results = Search(new RecipeSearchCriteria { Query = query });

        _ = await Assert.That(Names(results)).IsEqualTo(expected);
    }

    [Test]
    public async Task Query_LeavesInstructionsOut()
    {
        _ = await Assert.That(Search(new RecipeSearchCriteria { Query = "zephyr" }).Total).IsEqualTo(0);
    }

    [Test]
    public async Task Facets_CountEverySeededCategoryAndDiet()
    {
        var results = Search(new RecipeSearchCriteria());

        _ = await Assert.That(Counts(results.CategoryFacets, "Beverage", "Breakfast", "Dessert", "Dinner", "Lunch", "Snack"))
            .IsEqualTo("Beverage 4, Breakfast 4, Dessert 4, Dinner 5, Lunch 4, Snack 4");
        _ = await Assert.That(Counts(results.DietFacets, "Dairy-Free", "Gluten-Free", "High-Protein", "Keto", "Low-Carb", "Spicy", "Vegan", "Vegetarian"))
            .IsEqualTo("Dairy-Free 2, Gluten-Free 7, High-Protein 8, Keto 1, Low-Carb 3, Spicy 4, Vegan 12, Vegetarian 10");
        _ = await Assert.That(results.DietFacets.ContainsKey("Cheesy")).IsFalse();
    }

    [Test]
    public async Task ACategory_NarrowsTheDietsButKeepsTheOtherCategoriesCounted()
    {
        var results = Search(new RecipeSearchCriteria { Categories = ["Dinner"] });

        _ = await Assert.That(results.Total).IsEqualTo(5);
        _ = await Assert.That(Counts(results.CategoryFacets, "Beverage", "Lunch")).IsEqualTo("Beverage 4, Lunch 4");
        _ = await Assert.That(Counts(results.DietFacets, "High-Protein", "Vegan", "Vegetarian")).IsEqualTo("High-Protein 4, Vegan 1, Vegetarian 1");
    }

    [Test]
    public async Task TimeRange_UsesEachRecipesFastestVariant()
    {
        var results = Search(new RecipeSearchCriteria { Categories = ["Lunch"], TimeMin = 0, TimeMax = 15 });

        _ = await Assert.That(Names(results)).IsEqualTo("Caprese Sandwich,Quinoa Power Salad,Weeknight Bowls");
    }

    [Test]
    [Arguments("relevant", "Hour-Glass Frittata,Legendary Lasagna,Sheet-Pan Salmon,Slow-Braised Short Ribs,Weeknight Tacos")]
    [Arguments("rated", "Legendary Lasagna,Slow-Braised Short Ribs,Sheet-Pan Salmon,Weeknight Tacos,Hour-Glass Frittata")]
    [Arguments("recent", "Legendary Lasagna,Slow-Braised Short Ribs,Weeknight Tacos,Sheet-Pan Salmon,Hour-Glass Frittata")]
    public async Task Sort_OrdersTheDinners(string sort, string expected)
    {
        var results = Search(new RecipeSearchCriteria { Categories = ["Dinner"], Sort = sort });

        _ = await Assert.That(string.Join(",", results.Results.Select(hit => hit.Name))).IsEqualTo(expected);
    }

    [Test]
    public async Task Sort_ByVariants_PutsTheBiggestFlightFirst()
    {
        var results = Search(new RecipeSearchCriteria { Categories = ["Lunch"], Sort = "variants" });

        _ = await Assert.That(results.Results[0].Name).IsEqualTo("Spicy Ramen Flight");
        _ = await Assert.That(results.Results[1].Name).IsEqualTo("Weeknight Bowls");
    }

    [Test]
    public async Task Spotlight_IsTheTopRatedMatch()
    {
        _ = await Assert.That(Search(new RecipeSearchCriteria { Categories = ["Dinner"] }).Spotlight?.Name).IsEqualTo("Legendary Lasagna");

        var beverages = Search(new RecipeSearchCriteria { Categories = ["Beverage"] });
        _ = await Assert.That(beverages.Total).IsEqualTo(4);
        _ = await Assert.That(beverages.Spotlight).IsNull();
    }

    [Test]
    public async Task Paging_WalksEveryMatchOnce()
    {
        var pages = Enumerable.Range(0, 4)
            .Select(page => Search(new RecipeSearchCriteria { Diets = ["Vegan"], PageSize = 5, Page = page }))
            .ToList();

        _ = await Assert.That(string.Join(",", pages.Select(page => page.Results.Count))).IsEqualTo("5,5,2,0");
        _ = await Assert.That(pages.SelectMany(page => page.Results).Select(hit => hit.Slug).Distinct().Count()).IsEqualTo(12);
        _ = await Assert.That(pages.Skip(1).All(page => page.Spotlight is null)).IsTrue();
    }

    [Test]
    public async Task Hit_CarriesWhatTheCardShows()
    {
        var hit = Search(new RecipeSearchCriteria { Query = "ramen" }).Results.Single();

        _ = await Assert.That(hit.Name).IsEqualTo("Spicy Ramen Flight");
        _ = await Assert.That(hit.Slug).IsEqualTo("/recipes/spicy-ramen-flight/");
        _ = await Assert.That(hit.Icon).IsEqualTo("fa-duotone fa-bowl-chopsticks");
        _ = await Assert.That(hit.Category).IsEqualTo("Lunch");
        _ = await Assert.That(hit.StartedBy).IsEqualTo("Priya Balan");
        _ = await Assert.That(string.Join(",", hit.Tags)).IsEqualTo("Spicy,Vegan,High-Protein,Dairy-Free");
        _ = await Assert.That(hit.AverageRating).IsEqualTo(4.5d);
        _ = await Assert.That(hit.ReviewCount).IsEqualTo(3);
        _ = await Assert.That(hit.VariantCount).IsEqualTo(4);
        _ = await Assert.That(hit.FastestTime).IsEqualTo(20);
    }

    [Test]
    public async Task Hit_WithNoVariantsOrReviews_IsStillListed()
    {
        var hit = Search(new RecipeSearchCriteria { Query = "cupboard" }).Results.Single();

        _ = await Assert.That(hit.Name).IsEqualTo("Bare Cupboard Snack Board");
        _ = await Assert.That(hit.VariantCount).IsEqualTo(0);
        _ = await Assert.That(hit.FastestTime).IsEqualTo(0);
        _ = await Assert.That(hit.AverageRating).IsNull();
    }

    private static string Names(RecipeSearchResults results) => string.Join(",", results.Results.Select(hit => hit.Name).Order());

    private static string Counts(IReadOnlyDictionary<string, int> facets, params string[] labels) =>
        string.Join(", ", labels.Select(label => $"{label} {facets.GetValueOrDefault(label)}"));

    private RecipeSearchResults Search(RecipeSearchCriteria criteria) =>
        Site.Services.GetRequiredService<IRecipeSearchService>().Search(criteria);
}
```

Create `tests/KCC.IntegrationTests/Features/Search/RecipeIndexTriggerTests.cs`:

```csharp
using KCC.Contributions;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.Search;
using Microsoft.Extensions.DependencyInjection;

namespace KCC.IntegrationTests.Features.Search;

// Each test changes only content it made, named with a word no seeded recipe uses, so its searches cannot collide
// with the seed or with each other.
public class RecipeIndexTriggerTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task PublishingARecipe_MakesItSearchable()
    {
        var recipeKey = await TestContent.RecipeAsync(Site.Services, "IT Quokka");
        await TestContent.VariantAsync(Site.Services, recipeKey, "Classic");

        var hit = (await SearchWhenCurrentAsync("quokka")).Results.Single();

        _ = await Assert.That(hit.Name).IsEqualTo("IT Quokka");
        _ = await Assert.That(hit.Slug).IsEqualTo("/recipes/it-quokka/");
        _ = await Assert.That(hit.VariantCount).IsEqualTo(1);
    }

    [Test]
    public async Task UnpublishingAVariant_KeepsItsRecipeAndDropsItsReviews()
    {
        var recipeKey = await TestContent.RecipeAsync(Site.Services, "IT Wombat");
        var kept = await TestContent.VariantAsync(Site.Services, recipeKey, "Kept");
        var withdrawn = await TestContent.VariantAsync(Site.Services, recipeKey, "Withdrawn");
        var writes = Site.Services.GetRequiredService<IContributionWrites>();
        await writes.UpsertReviewAsync(kept, Guid.NewGuid(), 4m, "Good");
        await writes.UpsertReviewAsync(withdrawn, Guid.NewGuid(), 2m, "Meh");
        _ = await Assert.That((await SearchWhenCurrentAsync("wombat")).Results.Single().AverageRating).IsEqualTo(3d);

        await TestContent.UnpublishAsync(Site.Services, withdrawn);

        var hit = (await SearchWhenCurrentAsync("wombat")).Results.Single();
        _ = await Assert.That(hit.VariantCount).IsEqualTo(1);
        _ = await Assert.That(hit.AverageRating).IsEqualTo(4d);
        _ = await Assert.That(hit.ReviewCount).IsEqualTo(1);
    }

    [Test]
    public async Task TrashingARecipe_TakesItOut()
    {
        var recipeKey = await TestContent.RecipeAsync(Site.Services, "IT Numbat");
        _ = await Assert.That((await SearchWhenCurrentAsync("numbat")).Total).IsEqualTo(1);

        await TestContent.TrashAsync(Site.Services, recipeKey);

        _ = await Assert.That((await SearchWhenCurrentAsync("numbat")).Total).IsEqualTo(0);
    }

    [Test]
    public async Task RenamingACategory_RelabelsItsRecipes()
    {
        var categoryKey = await TestContent.CategoryAsync(Site.Services, "IT Pantry");
        await TestContent.RecipeAsync(Site.Services, "IT Dingo", TestContent.Pick("category", categoryKey));
        _ = await Assert.That((await SearchWhenCurrentAsync("dingo")).Results.Single().Category).IsEqualTo("IT Pantry");

        await TestContent.RenameAsync(Site.Services, categoryKey, "IT Larder");

        var results = await SearchWhenCurrentAsync("dingo");
        _ = await Assert.That(results.Results.Single().Category).IsEqualTo("IT Larder");
        _ = await Assert.That(results.CategoryFacets.GetValueOrDefault("IT Larder")).IsEqualTo(1);
        _ = await Assert.That(results.CategoryFacets.ContainsKey("IT Pantry")).IsFalse();
    }

    [Test]
    public async Task RenamingATag_RelabelsItsDiet()
    {
        var tagKey = await TestContent.TagAsync(Site.Services, "IT Crunchy");
        var recipeKey = await TestContent.RecipeAsync(Site.Services, "IT Kookaburra");
        await TestContent.VariantAsync(Site.Services, recipeKey, "Classic", TestContent.Pick("tags", tagKey));
        _ = await Assert.That(string.Join(",", (await SearchWhenCurrentAsync("kookaburra")).Results.Single().Tags)).IsEqualTo("IT Crunchy");

        await TestContent.RenameAsync(Site.Services, tagKey, "IT Crispy");

        var results = await SearchWhenCurrentAsync("kookaburra");
        _ = await Assert.That(string.Join(",", results.Results.Single().Tags)).IsEqualTo("IT Crispy");
        _ = await Assert.That(results.DietFacets.GetValueOrDefault("IT Crispy")).IsEqualTo(1);
    }

    [Test]
    public async Task RenamingAnAuthor_RenamesStartedBy()
    {
        var authorKey = await TestContent.AuthorAsync(Site.Services, "it.platypus", "Ada", "Platypus");
        await TestContent.RecipeAsync(Site.Services, "IT Echidna", TestContent.Author(authorKey));
        _ = await Assert.That((await SearchWhenCurrentAsync("echidna")).Results.Single().StartedBy).IsEqualTo("Ada Platypus");

        TestContent.RenameAuthor(Site.Services, authorKey, "Ada", "Lovelace");

        _ = await Assert.That((await SearchWhenCurrentAsync("echidna")).Results.Single().StartedBy).IsEqualTo("Ada Lovelace");
        _ = await Assert.That((await SearchWhenCurrentAsync("lovelace")).Total).IsEqualTo(1);
        _ = await Assert.That((await SearchWhenCurrentAsync("platypus")).Total).IsEqualTo(0);
    }

    [Test]
    public async Task WritingAReview_RatesItsRecipe()
    {
        var recipeKey = await TestContent.RecipeAsync(Site.Services, "IT Bilby");
        var variantKey = await TestContent.VariantAsync(Site.Services, recipeKey, "Classic");
        _ = await Assert.That((await SearchWhenCurrentAsync("bilby")).Results.Single().ReviewCount).IsEqualTo(0);

        await Site.Services.GetRequiredService<IContributionWrites>().UpsertReviewAsync(variantKey, Guid.NewGuid(), 2.5m, "Fine");

        var hit = (await SearchWhenCurrentAsync("bilby")).Results.Single();
        _ = await Assert.That(hit.AverageRating).IsEqualTo(2.5d);
        _ = await Assert.That(hit.ReviewCount).IsEqualTo(1);
    }

    private async Task<RecipeSearchResults> SearchWhenCurrentAsync(string query)
    {
        await Site.Services.GetRequiredService<IRecipeIndexRebuilder>().WhenCurrentAsync(CancellationToken.None);
        return Site.Services.GetRequiredService<IRecipeSearchService>().Search(new RecipeSearchCriteria { Query = query });
    }
}
```

Create `tests/KCC.IntegrationTests/Features/Search/RecipeIndexSwapTests.cs`:

```csharp
using System.Collections.Concurrent;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.Search;
using Microsoft.Extensions.DependencyInjection;

namespace KCC.IntegrationTests.Features.Search;

public class RecipeIndexSwapTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task SearchesDuringRebuilds_AlwaysSeeAWholeIndex()
    {
        var rebuilder = Site.Services.GetRequiredService<IRecipeIndexRebuilder>();
        var search = Site.Services.GetRequiredService<IRecipeSearchService>();
        var totals = new ConcurrentDictionary<int, int>();
        using var stop = new CancellationTokenSource();

        var searchers = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            while (!stop.IsCancellationRequested)
            {
                var total = search.Search(new RecipeSearchCriteria { Categories = ["Dinner"] }).Total;
                totals.AddOrUpdate(total, 1, (_, count) => count + 1);
            }
        })).ToList();

        for (var rebuild = 0; rebuild < 10; rebuild++)
        {
            rebuilder.Signal();
            await rebuilder.WhenCurrentAsync(CancellationToken.None);
        }

        await stop.CancelAsync();
        await Task.WhenAll(searchers);

        _ = await Assert.That(string.Join(",", totals.Keys)).IsEqualTo("5");
        _ = await Assert.That(totals[5]).IsGreaterThan(10);
    }
}
```

- [ ] **Step 3: Run them to confirm they fail**

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/RecipeSearchTests/*"
```

Expected: the build succeeds, and every `RecipeSearchTests` case fails with "No service for type
'KCC.Web.Features.Search.IRecipeSearchService' has been registered".

- [ ] **Step 4: Publish a notification on every review write**

Create `src/KCC.Contributions/ReviewsChangedNotification.cs`:

```csharp
using Umbraco.Cms.Core.Notifications;

namespace KCC.Contributions;

public sealed record ReviewsChangedNotification : INotification;
```

In `src/KCC.Contributions/ContributionWrites.cs`:
1. Add `using Umbraco.Cms.Core.Events;`, keeping the usings sorted.
2. Replace the class declaration with:

```csharp
public sealed class ContributionWrites(
    IEFCoreScopeProvider<ContributionsDbContext> scopes,
    IContributionStats stats,
    IEventAggregator eventAggregator) : IContributionWrites
```

3. After `stats.Invalidate();` in `UpsertReviewAsync`, add:

```csharp
        await eventAggregator.PublishAsync(new ReviewsChangedNotification());
```

The notification goes out after the scope has committed and after the stats are invalidated, so the rebuild it
triggers reads the new review. `IEventAggregator.PublishAsync` runs synchronous handlers as well as asynchronous
ones.

- [ ] **Step 5: Register the index and its triggers**

Create `src/KCC.Web/Features/Search/RecipeIndexTriggers.cs`:

```csharp
using KCC.Contributions;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace KCC.Web.Features.Search;

// A recipe's document also carries its URL, category, tags, author's name and ratings, which live on other nodes,
// on members and in reviews, so any of those changing can change a document. Rebuilds are cheap, and a filter would
// have to know every one of those dependencies.
public class RecipeIndexTriggers(IRecipeIndexRebuilder rebuilder) :
    INotificationHandler<ContentCacheRefresherNotification>,
    INotificationHandler<MemberSavedNotification>,
    INotificationHandler<MemberDeletedNotification>,
    INotificationHandler<ReviewsChangedNotification>
{
    public void Handle(ContentCacheRefresherNotification notification) => rebuilder.Signal();

    public void Handle(MemberSavedNotification notification) => rebuilder.Signal();

    public void Handle(MemberDeletedNotification notification) => rebuilder.Signal();

    public void Handle(ReviewsChangedNotification notification) => rebuilder.Signal();
}
```

Create `src/KCC.Web/Features/Search/SearchComposer.cs`:

```csharp
using KCC.Contributions;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace KCC.Web.Features.Search;

public class SearchComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.Configure<RecipeSearchOptions>(builder.Config.GetSection("RecipeSearch"));
        builder.Services.AddSingleton<RecipeIndex>();
        builder.Services.AddSingleton<IRecipeSearchService, RecipeSearchService>();
        builder.Services.AddScoped<IRecipeIndexSource, RecipeIndexSource>();
        builder.Services.AddSingleton<RecipeIndexRebuilder>();
        builder.Services.AddSingleton<IRecipeIndexRebuilder>(services => services.GetRequiredService<RecipeIndexRebuilder>());
        builder.Services.AddHostedService(services => services.GetRequiredService<RecipeIndexRebuilder>());
        builder
            .AddNotificationHandler<ContentCacheRefresherNotification, RecipeIndexTriggers>()
            .AddNotificationHandler<MemberSavedNotification, RecipeIndexTriggers>()
            .AddNotificationHandler<MemberDeletedNotification, RecipeIndexTriggers>()
            .AddNotificationHandler<ReviewsChangedNotification, RecipeIndexTriggers>();
    }
}
```

Umbraco finds composers by scanning, so `Program.cs` does not change. The default quiet period lives in
`RecipeSearchOptions`, and `appsettings.json` needs no entry.

- [ ] **Step 6: Make the seeder wait for search**

In `src/KCC.Web/Features/DevTools/RecipeSeed/DevSeedApiController.cs`:
1. Add `using KCC.Web.Features.Search;`, keeping the usings sorted.
2. Replace the action's signature with:

```csharp
    public async Task<IActionResult> SeedRecipes(
        [FromServices] RecipeTestDataSeeder seeder,
        [FromServices] IRecipeIndexRebuilder recipeIndex,
        CancellationToken cancellationToken)
```

3. After `var summary = await seeder.RunAsync(log, cancellationToken);`, add:

```csharp

        // The fixtures start testing once this answers, so it waits until search can find what it seeded.
        await recipeIndex.WhenCurrentAsync(cancellationToken);
```

- [ ] **Step 7: Run the tests**

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj
dotnet run --project tests/KCC.UnitTests/KCC.UnitTests.csproj
```

Expected: 0 warnings, and every test passes on both integration runs. That includes the 15 `RecipeSearchTests`
cases, the 7 `RecipeIndexTriggerTests` and the swap test. The suite still runs one test at a time. Each rebuild
logs `Rebuilt the recipe index with …` to the run folder's `logs/`.

- [ ] **Step 8: Check the E2E site still starts**

Seeding now waits for the index, which adds about two seconds to the E2E fixture's start.

```bash
(cd src/KCC.Web && yarn build:all)
dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj
```

Expected: every E2E test passes (Phase 1's and Phase 2's).

- [ ] **Step 9: Commit**

```bash
git add -A src/KCC.Contributions src/KCC.Web/Features/Search src/KCC.Web/Features/DevTools/RecipeSeed tests/KCC.IntegrationTests
git commit -m "Rebuild the Recipe Index on Every Change"
```

---

### Task 6: The listing page and the search API

The Kentico `RecipeSearchController` becomes the hijacking `RecipeListingPageController`. It renders the existing
view with the first page of an unfiltered search. That page's facets are the complete option set the filter panel
keeps for every later search. The search API returns to the build unchanged.

**Files:**
- Move and rewrite: `src/KCC.Web/Features/Pages/RecipeSearch/RecipeSearchController.cs` → `RecipeListingPageController.cs`
- Modify: `src/KCC.Web/KCC.Web.csproj`
- Create: `tests/KCC.IntegrationTests/Features/Pages/RecipeListingPageTests.cs`,
  `tests/KCC.IntegrationTests/Features/Search/RecipeSearchApiTests.cs`

**Interfaces:**
- Consumes:
  - `IRecipeSearchService`, `RecipeSearchCriteria` and `RecipeSearchResponseMapper.ToResponse` (Task 2)
  - `IRecipeQueries.GetCreateRecipeUrl` (Task 3)
  - `BreadcrumbService.Build` (Phase 2)
  - `PageMetadata.Apply` and `IResourceStringProvider.GetManyOrDefault` (Phase 1)
- Produces:
  - `/recipes/`, rendered by `RecipeListingPageController`. Its props are `initial`, `create-recipe-url`,
    `breadcrumbs` and `resource-strings`, and the Vue app is unchanged.
  - `GET /api/recipes/search?query=&category=&diet=&timeMin=&timeMax=&sort=&page=&pageSize=`, unchanged, in the same
    envelope as `initial`. `category` and `diet` repeat.

- [ ] **Step 1: Write the failing tests**

Create `tests/KCC.IntegrationTests/Features/Pages/RecipeListingPageTests.cs`:

```csharp
using System.Net;
using KCC.IntegrationTests.Config;

namespace KCC.IntegrationTests.Features.Pages;

public class RecipeListingPageTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task Listing_RendersThroughItsController()
    {
        using var client = Site.CreateClient();

        var page = await RenderedPage.GetAsync(client, "/recipes/");

        _ = await Assert.That(page.Status).IsEqualTo(HttpStatusCode.OK);
        _ = await Assert.That(page.Html.Contains("<title>Recipes</title>", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(page.Attribute("create-recipe-url")).IsEqualTo("/recipes/create-recipe/");
    }

    [Test]
    public async Task Listing_CarriesTheFirstPageOfEveryRecipe()
    {
        using var client = Site.CreateClient();

        var initial = (await RenderedPage.GetAsync(client, "/recipes/")).Prop("initial");

        _ = await Assert.That(initial.GetProperty("total").GetInt32()).IsGreaterThanOrEqualTo(25);
        _ = await Assert.That(initial.GetProperty("results").GetArrayLength()).IsEqualTo(12);
        _ = await Assert.That(initial.GetProperty("results")[0].GetProperty("name").GetString()).IsEqualTo("Avocado Toast Supreme");
        _ = await Assert.That(initial.GetProperty("facets").GetProperty("category").GetProperty("Dinner").GetInt32()).IsEqualTo(5);
    }

    [Test]
    public async Task Listing_TrailRunsFromHome()
    {
        using var client = Site.CreateClient();

        var page = await RenderedPage.GetAsync(client, "/recipes/");
        var trail = page.Prop("breadcrumbs").EnumerateArray()
            .Select(crumb => $"{crumb.GetProperty("linkText").GetString()}>{crumb.GetProperty("url").GetString()}");

        _ = await Assert.That(string.Join("|", trail)).IsEqualTo("Home>/|Recipes>");
    }
}
```

Create `tests/KCC.IntegrationTests/Features/Search/RecipeSearchApiTests.cs`:

```csharp
using System.Text.Json;
using KCC.IntegrationTests.Config;

namespace KCC.IntegrationTests.Features.Search;

public class RecipeSearchApiTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task Search_AnswersInTheShapeTheListingReads()
    {
        using var client = Site.CreateClient();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/api/recipes/search?query=tahini"));
        var root = document.RootElement;
        var hit = root.GetProperty("results")[0];

        _ = await Assert.That(root.GetProperty("total").GetInt32()).IsEqualTo(1);
        _ = await Assert.That(root.GetProperty("page").GetInt32()).IsEqualTo(0);
        _ = await Assert.That(root.GetProperty("pageSize").GetInt32()).IsEqualTo(12);
        _ = await Assert.That(hit.GetProperty("name").GetString()).IsEqualTo("Weeknight Bowls");
        _ = await Assert.That(hit.GetProperty("slug").GetString()).IsEqualTo("/recipes/weeknight-bowls/");
        _ = await Assert.That(hit.GetProperty("averageRating").GetDouble()).IsEqualTo(4.5d);
        _ = await Assert.That(hit.GetProperty("variantCount").GetInt32()).IsEqualTo(2);
        _ = await Assert.That(hit.GetProperty("fastestTime").GetInt32()).IsEqualTo(15);
        _ = await Assert.That(root.GetProperty("facets").GetProperty("category").GetProperty("Lunch").GetInt32()).IsEqualTo(1);
        _ = await Assert.That(root.GetProperty("facets").GetProperty("diet").GetProperty("Gluten-Free").GetInt32()).IsEqualTo(1);
        _ = await Assert.That(root.GetProperty("spotlight").GetProperty("name").GetString()).IsEqualTo("Weeknight Bowls");
    }

    [Test]
    public async Task Search_ReadsRepeatedFiltersSortAndPaging()
    {
        using var client = Site.CreateClient();
        using var document = JsonDocument.Parse(
            await client.GetStringAsync("/api/recipes/search?category=Dinner&category=Dessert&sort=rated&pageSize=3"));
        var root = document.RootElement;
        var names = root.GetProperty("results").EnumerateArray().Select(hit => hit.GetProperty("name").GetString());

        _ = await Assert.That(root.GetProperty("total").GetInt32()).IsEqualTo(9);
        _ = await Assert.That(string.Join(",", names)).IsEqualTo("Legendary Lasagna,Molten Chocolate Cake,Slow-Braised Short Ribs");
    }

    [Test]
    public async Task Search_PastTheLastPage_AnswersNoResults()
    {
        using var client = Site.CreateClient();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/api/recipes/search?category=Beverage&page=5"));
        var root = document.RootElement;

        _ = await Assert.That(root.GetProperty("total").GetInt32()).IsEqualTo(4);
        _ = await Assert.That(root.GetProperty("results").GetArrayLength()).IsEqualTo(0);
        _ = await Assert.That(root.GetProperty("spotlight").ValueKind).IsEqualTo(JsonValueKind.Null);
    }
}
```

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/RecipeListingPageTests/*"
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj -- --treenode-filter "/*/*/RecipeSearchApiTests/*"
```

Expected: every case fails. No controller serves the listing type, so `/recipes/` answers 404 (or, if Phase 1
took the template fallback, an empty page with no props). `/api/recipes/search` answers 404.

- [ ] **Step 2: Bring the page and the API back**

In `src/KCC.Web/KCC.Web.csproj`, delete these three lines:

```xml
        <Compile Remove="Features/Api/RecipeSearchApiController.cs" />
        <Compile Remove="Features/Pages/RecipeSearch/**" />
        <Content Remove="Features/Pages/RecipeSearch/**/*.cshtml" />
```

```bash
git mv src/KCC.Web/Features/Pages/RecipeSearch/RecipeSearchController.cs \
  src/KCC.Web/Features/Pages/RecipeSearch/RecipeListingPageController.cs
```

Replace the contents of `src/KCC.Web/Features/Pages/RecipeSearch/RecipeListingPageController.cs` with:

```csharp
using KCC.Web.Features.Components.Breadcrumbs;
using KCC.Web.Features.Dictionary;
using KCC.Web.Features.Models.Generated;
using KCC.Web.Features.Pages.Shared;
using KCC.Web.Features.Recipes;
using KCC.Web.Features.Search;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace KCC.Web.Features.Pages.RecipeSearch;

public class RecipeListingPageController(
    ILogger<RenderController> logger,
    ICompositeViewEngine compositeViewEngine,
    IUmbracoContextAccessor umbracoContextAccessor,
    IRecipeQueries recipes,
    IRecipeSearchService recipeSearch,
    BreadcrumbService breadcrumbs,
    IResourceStringProvider resourceStrings,
    PageMetadata pageMetadata)
    : RenderController(logger, compositeViewEngine, umbracoContextAccessor)
{
    public override IActionResult Index()
    {
        if (CurrentPage is not RecipeListingPage listing)
        {
            return NotFound();
        }

        var viewModel = new RecipeSearchViewModel
        {
            CreateRecipeUrl = recipes.GetCreateRecipeUrl(listing),
            InitialResults = RecipeSearchResponseMapper.ToResponse(recipeSearch.Search(new RecipeSearchCriteria())),
            Breadcrumbs = breadcrumbs.Build(listing),
            ResourceStrings = GetStrings(),
        };
        pageMetadata.Apply(listing, viewModel);

        return View("~/Features/Pages/RecipeSearch/Index.cshtml", viewModel);
    }

    private Dictionary<string, string> GetStrings() => resourceStrings.GetManyOrDefault(
        "RecipeSearch.SearchRecipes",
        "RecipeSearch.CreateRecipe",
        "RecipeSearch.BrowseTheKitchen",
        "RecipeSearch.SearchPlaceholder",
        "RecipeSearch.Search",
        "RecipeSearch.Filters",
        "RecipeSearch.Reset",
        "RecipeSearch.Category",
        "RecipeSearch.Dietary",
        "RecipeSearch.TotalTime",
        "RecipeSearch.Min",
        "RecipeSearch.OrMore",
        "RecipeSearch.OrLess",
        "RecipeSearch.Sort",
        "RecipeSearch.SortRelevant",
        "RecipeSearch.SortTopRated",
        "RecipeSearch.SortVariants",
        "RecipeSearch.SortRecent",
        "RecipeSearch.Grid",
        "RecipeSearch.List",
        "RecipeSearch.ClearAll",
        "RecipeSearch.TopRated",
        "RecipeSearch.Variants",
        "RecipeSearch.StartedBy",
        "RecipeSearch.NoRatingsYet",
        "RecipeSearch.LoadingMore",
        "RecipeSearch.NoRecipesMatch",
        "RecipeSearch.NoRecipesHint",
        "RecipeSearch.ClearAllFilters",
        "RecipeSearch.IngredientSearchComingSoon",
        "RecipeSearch.Recipe",
        "RecipeSearch.Recipes",
        "RecipeSearch.ResultsFor");
}
```

The resource-string list is the Kentico controller's, key for key. `RecipeSearchViewModel`, `Index.cshtml` and
`RecipeSearchApiController` are unchanged.

- [ ] **Step 3: Run the tests**

```bash
dotnet build KitchenCommandCenter.sln
dotnet run --project tests/KCC.IntegrationTests/KCC.IntegrationTests.csproj
```

Expected: 0 warnings, and every test passes, including the 3 `RecipeListingPageTests` and the 3
`RecipeSearchApiTests`.

- [ ] **Step 4: Look at it**

```bash
cd src/KCC.Web && dotnet run --launch-profile Local
```

Once it serves, seed it from another terminal. The seeder skips what exists:
`curl -sk -X POST https://localhost:58671/api/dev/seed-recipes | head -c 200; echo`. Then open
`https://localhost:58671/recipes` and check each of these, in both ramps (toggle in the header):
- the torn header sheet with Create Recipe, 25 recipes, and Legendary Lasagna as Top Rated;
- a search for `tahini` leaves only Weeknight Bowls;
- the Beverage filter leaves four cards and no spotlight;
- the sorts and the list view;
- scrolling to the end loads the second page;
- the browser console shows no hydration warning.

Stop the site.

- [ ] **Step 5: Commit**

```bash
git add -A src/KCC.Web/Features/Pages/RecipeSearch src/KCC.Web/KCC.Web.csproj tests/KCC.IntegrationTests
git commit -m "Serve the Recipe Listing and Search API from Umbraco"
```

---

### Task 7: The search E2E suite

This task brings the search suite back and makes its assertions exact against the seed, the way Phase 2 did for the
recipe pages. The Kentico version relied on "egg" matching the hand-made Egg Skillet, which no longer exists.
Tahini is the seed's ingredient-only word. The E2E site is a production build started by `SiteProcess`: a plain
`GotoAsync` settles, and nothing needs `[NotInParallel]`, so the Vite dev-server comments go too. A new test drives
the facet panel, so the drill-sideways counts are checked in the browser as well as in the integration suite.

`RecipeSearchLiveRatingTests` stays excluded (see "Not in Phase 3").

**Files:**
- Rewrite: `tests/KCC.E2ETests/Features/RecipeSearch/RecipeSearchTests.cs`
- Modify: `tests/KCC.E2ETests/KCC.E2ETests.csproj`

**Interfaces:**
- Consumes the seeded facts:
  - 25 recipes, with Legendary Lasagna the only 5.0;
  - no-query results in alphabetical order, starting with Avocado Toast Supreme;
  - only Weeknight Bowls, which is rated, lists tahini;
  - four beverages, all unrated;
  - five dinners;
  - three vegan beverages.
- Consumes the UI hooks:
  - `data-testid`: `recipe-card`, `recipe-spotlight`, `recipe-search-input`, `recipe-search-submit`,
    `recipes-empty` and `view-list`;
  - `data-recipe-name`;
  - the filter panel's `#recipe-filters` and its `.kcc-q` counts.

- [ ] **Step 1: Narrow the exclusion**

In `tests/KCC.E2ETests/KCC.E2ETests.csproj`, replace `<Compile Remove="Features/RecipeSearch/**" />` with:

```xml
        <Compile Remove="Features/RecipeSearch/RecipeSearchLiveRatingTests.cs" />
```

- [ ] **Step 2: Rewrite the suite**

Replace `tests/KCC.E2ETests/Features/RecipeSearch/RecipeSearchTests.cs` with:

```csharp
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.RecipeSearch;

// The seeded recipes (RecipeSeedData) are the only content: 25 recipes, with Legendary Lasagna the only 5.0.
public class RecipeSearchTests : BasePageTests
{
    [Test]
    public async Task Search_page_lists_recipe_cards()
    {
        await Page.GotoAsync("/recipes");

        // The top-rated recipe takes the spotlight, which keeps it out of the grid.
        await Expect(Page.Locator("[data-testid='recipe-spotlight']")).ToHaveAttributeAsync("data-recipe-name", "Legendary Lasagna");
        await Expect(Page.Locator("[data-testid='recipe-card']").First).ToHaveAttributeAsync("data-recipe-name", "Avocado Toast Supreme");
        await Expect(Page.Locator("[data-testid='recipe-card'][data-recipe-name='Legendary Lasagna']")).ToHaveCountAsync(0);
    }

    [Test]
    public async Task Search_narrows_results_by_query()
    {
        await Page.GotoAsync("/recipes");

        // Grid cards and the "Top Rated" spotlight both carry data-recipe-name, so this counts every result in
        // either slot.
        var results = Page.Locator("[data-recipe-name]");
        _ = await Assert.That(await results.CountAsync()).IsGreaterThan(1);

        await SearchForAsync("tahini");

        // Only Weeknight Bowls lists tahini, as an ingredient. It is rated, so it shows as the spotlight.
        await Expect(results).ToHaveCountAsync(1);
        await Expect(Page.Locator("[data-recipe-name='Weeknight Bowls']")).ToHaveCountAsync(1);
    }

    [Test]
    public async Task No_matches_shows_empty_state()
    {
        await Page.GotoAsync("/recipes");

        await SearchForAsync("zzznotarealrecipe");

        await Expect(Page.Locator("[data-testid='recipes-empty']")).ToBeVisibleAsync();
        await Expect(Page.Locator("[data-testid='recipe-card']")).ToHaveCountAsync(0);
    }

    [Test]
    public async Task View_toggle_switches_to_list()
    {
        await Page.GotoAsync("/recipes");
        var cards = Page.Locator("[data-testid='recipe-card']");
        var gridCount = await cards.CountAsync();
        _ = await Assert.That(gridCount).IsGreaterThanOrEqualTo(2);

        await Page.Locator("[data-testid='view-list']").ClickAsync();

        // List rows reuse the grid cards' data-testid, and switching view fetches nothing, so the count holds.
        await Expect(cards).ToHaveCountAsync(gridCount);
    }

    [Test]
    public async Task Category_filter_narrows_results_and_keeps_other_categories_counted()
    {
        await Page.GotoAsync("/recipes");
        var filters = Page.Locator("#recipe-filters");

        await Page.RunAndWaitForResponseAsync(
            () => filters.Locator("label").GetByText("Beverage", new() { Exact = true }).ClickAsync(),
            response => response.Url.Contains("/api/recipes/search", StringComparison.Ordinal));

        // No beverage is rated, so nothing takes the spotlight. Drill-sideways counts keep every category's full
        // count while the diet counts narrow to the beverages.
        await Expect(Page.Locator("[data-testid='recipe-card']")).ToHaveCountAsync(4);
        await Expect(Page.Locator("[data-testid='recipe-spotlight']")).ToHaveCountAsync(0);
        await Expect(filters.Locator("li").Filter(new() { HasText = "Dinner" }).Locator(".kcc-q")).ToHaveTextAsync("5");
        await Expect(filters.Locator("li").Filter(new() { HasText = "Vegan" }).Locator(".kcc-q")).ToHaveTextAsync("3");
    }

    // Search is submit-based: the header emits `submit` only when its form is submitted, which fetches
    // /api/recipes/search.
    private async Task SearchForAsync(string query)
    {
        await Page.Locator("[data-testid='recipe-search-input']").FillAsync(query);
        await Page.RunAndWaitForResponseAsync(
            () => Page.Locator("[data-testid='recipe-search-submit']").ClickAsync(),
            response => response.Url.Contains("/api/recipes/search", StringComparison.Ordinal));
    }
}
```

The category checkboxes are screen-reader-only inputs, so the test clicks their visible label.

- [ ] **Step 3: Run the E2E suite**

```bash
dotnet build KitchenCommandCenter.sln
(cd src/KCC.Web && yarn build:all)
dotnet run --project tests/KCC.E2ETests/KCC.E2ETests.csproj
```

Expected: every E2E test passes: Phase 2's, plus the 5 search tests.

If `Category_filter…` cannot find the Beverage label, read the rendered panel with a
`Page.Locator("#recipe-filters").InnerHTMLAsync()` dump. Adjust the locator, never the app: the panel's markup
comes from the unchanged `RecipeFilters.vue`.

- [ ] **Step 4: Commit**

```bash
git add -A tests/KCC.E2ETests
git commit -m "Bring Back the Recipe Search E2E Tests"
```

---

### Task 8: Docs, memory and the Phase 3 gate

**Files:**
- Modify: `README.md`, this plan's Status line
- Memory, outside the repo, in `~/.claude/projects/-Users-twinright-Repos-Kitchen-Command-Center/memory/`:
  - `kcc-web-dev-run-gotchas.md`
  - `recipe-search-deterministic-order.md`
  - `recipe-search-test-seeder.md`
  - `MEMORY.md`

- [ ] **Step 1: Document the index**

In `README.md`, under **Development Best Practices**, add this subsection after Phase 2's **Contributions**:

```markdown
#### Recipe search

The recipe index is a Lucene index held in memory and rebuilt whole: at startup, and two seconds
(`RecipeSearch:RebuildDelay`) after the last of any burst of content, member or review changes. Nothing needs
rebuilding by hand and nothing is written to disk. Each rebuild logs `Rebuilt the recipe index with N recipes`.
Code that writes reviews publishes `ReviewsChangedNotification`, and tests wait for a rebuild with
`IRecipeIndexRebuilder.WhenCurrentAsync`.
```

`CLAUDE.md` says nothing that this phase invalidates, so it is unchanged.

```bash
git add README.md
git commit -m "Document the Recipe Index"
```

- [ ] **Step 2: Update the memory this phase invalidated**

Phases 1 and 2 may already have edited these files. Apply each change to the text that describes the same thing now.

In `kcc-web-dev-run-gotchas.md`, replace item 2 (the one that starts "**The RecipeSearch Lucene index is populated by
an in-memory worker…**") with:

> 2. **The recipe index rebuilds itself** (since replatform Phase 3). It lives in memory, and it is rebuilt whole
>    at startup and 2 s (`RecipeSearch:RebuildDelay`) after the last content, member or review change. Each
>    rebuild logs `Rebuilt the recipe index with N recipes`. `POST /api/dev/seed-recipes` answers only once the
>    index holds the seed. There is nothing to rebuild by hand, and nothing on disk.

In its `description`, replace `RecipeSearch Lucene index only builds inside a served request` with
`the recipe index rebuilds itself in memory`.

In `recipe-search-deterministic-order.md`:
- Replace `RecipeSearchIndexingStrategy.BuildDocument` with `RecipeIndexBuilder.BuildDocument`.
- Replace the sentence that starts "Reindex (the dev seeder's Rebuild / CI seed step) is required" with:

  > After changing indexed fields, restart the site: the startup build re-indexes every recipe.

- Append:

  > Since replatform Phase 3 the index is rebuilt from the published tree on every change, in tree order, so doc
  > order no longer shuffles between runs. The `NameSort` tie-break still decides the no-query order, and
  > `RecipeSearchTests.Sort_OrdersTheDinners` asserts it.

In `recipe-search-test-seeder.md`, append:

> Since replatform Phase 3 the endpoint answers only once the recipe index holds what it seeded
> (`IRecipeIndexRebuilder.WhenCurrentAsync`), so a fixture can search as soon as seeding returns.

In `MEMORY.md`, make two changes:
1. In the `kcc-web-dev-run-gotchas` line, replace the words about the Lucene index building only inside a served
   request with `the recipe index rebuilds itself in memory (replatform Phase 3)`.
2. Append to the replatform line: `Phase 3 landed (<date>): search runs on an in-memory Lucene index, rebuilt on
   every change.`

- [ ] **Step 3: The gate — search E2E green, and the listing in both ramps**

Stop any site on port 58671, then clone the branch fresh and start it:

```bash
GATE="$(mktemp -d)/kcc-phase-3-gate"
git clone --branch replatform --single-branch /Users/twinright/Repos/Kitchen-Command-Center "$GATE"
cd "$GATE" && yarn install --frozen-lockfile
cd src/KCC.Web && dotnet watch --non-interactive
```

The clone shares this machine's user-secrets through the project's `UserSecretsId`. On its first boot it installs,
and it imports the schema, dictionary and baseline from uSync.

Once it serves, run these from the main checkout:

```bash
curl -sk -X POST https://localhost:58671/api/dev/seed-recipes | head -c 200; echo
curl -sk 'https://localhost:58671/api/recipes/search?query=tahini' | head -c 200; echo
dotnet run --project tests/KCC.ReferenceCapture -- --only recipes --out .superpowers/reference/umbraco-phase-3
ls .superpowers/reference/umbraco-phase-3/*/
```

Expected:
- The seed summary reads `recipes +25 (skipped 0), variants +29, reviews +27, authors +2`. It arrives about two
  seconds after the last save, once the index holds the seed.
- The search answers `{"total":1,…"name":"Weeknight Bowls"…`.
- The capture writes `recipes.png` in each of `light-desktop`, `light-mobile`, `dark-desktop` and `dark-mobile`.

Compare each file with the `recipes.png` in the folder of the same name under
`docs/replatform/reference/xperience-final/`. The header and footer are Phase 1's, and were compared at its gate. The
body must match:

| Area | Must match |
|---|---|
| Header sheet | BROWSE THE KITCHEN, SEARCH RECIPES, CREATE RECIPE; the search box and its placeholder; the sort pills RELEVANT, TOP RATED, VARIANTS, RECENT with RELEVANT selected; the grid and list toggle |
| Filters | CATEGORY: Beverage 4, Breakfast 4, Dessert 4, Dinner 5, Lunch 4, Snack 4. DIETARY: Dairy-Free 2, Gluten-Free 7, High-Protein 8, Keto 1, Low-Carb 3, Spicy 4, Vegan 12, Vegetarian 10. TOTAL TIME: Any, 0 MIN to 60+ MIN |
| Spotlight | TOP RATED, DINNER · STARTED BY PRIYA BALAN, LEGENDARY LASAGNA, 1 VARIANTS, 60 MIN |
| Grid | Avocado Toast Supreme, Bare Cupboard Snack Board, Caprese Sandwich, Cold Brew Concentrate, Crispy Roasted Chickpeas, Fluffy Buttermilk Pancakes (4.3 · 2), Golden Milk, Hour-Glass Frittata and Loaded Nachos, in that order, with their tags, times and "No ratings yet" labels |

Two differences are expected:
- **25 recipes, not 27.** The reference also held two hand-made recipes, Egg Skillet and Mac & Cheese. Without
  them, the cards after Crispy Roasted Chickpeas each move up one place, and Mango Lassi and Matcha Panna Cotta end
  the first page.
- **The spotlight reads 5.0 · 3, not 5.0 · 1.** The seed gives Legendary Lasagna three 5-star reviews. The
  reference database held one.

Any other difference is a bug: fix it and re-capture before closing the phase.

Then ask the owner for one check in the gate site's backoffice. Signing in needs their password, so they do this
themselves: Content → Recipe Categories → Snack → rename it `Snacks` → **Save and Publish**. About two seconds later
a reload of `/recipes` shows `Snacks 4` in the filters. They rename it back, and `Snack 4` returns. This is spec
§11's "publishing a category or tag rebuilds the search index", seen from the editor's side.

Stop the gate site and delete `$GATE`.

- [ ] **Step 4: Everything, once more**

From the main checkout:

```bash
dotnet build KitchenCommandCenter.sln
(cd src/KCC.Web && yarn build:all && yarn test && yarn type-check)
node tests/scripts/run.mjs
```

Expected:
- `Build succeeded` with 0 warnings.
- Both bundles build, and Vitest and the type check pass. The search front end did not change, so neither did its
  specs.
- The combined run is green: unit, integration (one test at a time) and E2E (Phase 2's count plus 5). It opens its
  HTML report when it finishes.

- [ ] **Step 5: Close the phase**

Set this file's **Status** line to `done (<date>)`, then commit it:

```bash
git add docs/replatform/plans/2026-09-23-phase-3-search.md
git commit -m "Close Replatform Phase 3"
```

Phase 4 (Members and writes) is planned next, in this folder, against the code as it then stands. It brings back
`RecipeSearchLiveRatingTests`, and every review write it adds publishes `ReviewsChangedNotification`.

## Findings from Phase 3

Found during Phase 3 (2026-09-25 to 2026-09-27). The Phase 4 to 7 plans predate them.

- **The branch.** Phase 3 ran on `replatform-phase-3`, based on `replatform-phase-2`, because Phase 2 was not merged
  into `replatform`. Neither branch is merged yet. The owner committed the Phase 5 and 6 plans on this branch
  (`6a1cbcb`).
- **`WhenCurrentAsync` also fails late callers.** Suppose a caller's changes were covered by a rebuild that failed,
  and no signal has come in since. The caller gets that failure at once; the plan's code left it waiting forever. A
  later successful build supersedes the failure.
- **Failed rebuilds retry on their own.** The first retry comes after `RecipeSearch:RetryDelay` (30 s). The delay
  doubles after each failure, up to 10 minutes, and resets after a success. The index starts empty, so without this
  a failed startup build left search empty until some unrelated change.
- **Dates.** Search documents read a create date with no `DateTimeKind` as UTC (`PageMetadata.AsUtc`), the way
  `article:published_time` does. `ToUniversalTime()` shifted such a date by the server's offset.
- **Tests that add nodes under seeded folders remove them.** `BaselineContentTests` expects exactly 6 children under
  Recipe Categories, 11 under Recipe Tags and 2 under Status Codes. `RecipeIndexTriggerTests` moves the category and
  tag it adds to the recycle bin in a `finally`. Any Phase 4 test that adds taxonomy nodes must do the same.
- **`RecipeSearchLiveRatingTests` needs rework before Phase 4 brings it back.** It gives the first card 5 stars and
  expects that card to become the single top-rated recipe. The seed's Legendary Lasagna is already rated 5.0, so that
  premise fails. While it runs, it also changes the spotlight and first card that
  `RecipeSearchTests.Search_page_lists_recipe_cards` asserts, so the two can't run concurrently as written.
- **A cold-start race in the header.** On a freshly booted site, concurrent first requests can read the site
  settings utility nav's `NavLink.Link` as null. This is an Umbraco first-conversion race, and the header drops the
  Login link for that request. It made `ChromeTests.Home_RendersTheHeaderInEachRamp` flaky once this phase's five
  parallel search tests enlarged the first burst of requests. `SiteProcess` now requests `/` once after seeding, and
  30 concurrent requests right after boot lost the link in 2 of 10 boots before that warm-up. The live site can show
  the same blip on the first burst of requests after a restart.
- **The rebuild debounce has no maximum wait.** Every signal restarts the 2 s quiet period. In Phase 4, sign-ins,
  sign-outs and failed-login counters all save the member, and each save signals, so a steady stream of them could
  postpone rebuilds. Cap the wait if that traffic arrives. `RecipeSearch:RebuildDelay` and `RetryDelay` are not
  validated: `"2"` binds as two days, and a zero `RetryDelay` retries without pause.
- **The seed icon.** Matcha Panna Cotta's icon was `fa-duotone fa-pudding`, which Font Awesome Pro does not have, so
  its tile rendered blank. It is `fa-duotone fa-custard` now.
- **Dev loop.** In two short smoke boots, `dotnet run --launch-profile Local` did not bring Vite and the SSR service
  up: the dev-cert bootstrap was still running. `dotnet watch --non-interactive`, the README's loop, starts both.
- **The gate** ran from a fresh clone of `replatform-phase-3` on a new database.
  - The seed summary, the tahini search and the rebuild log lines match Task 8.
  - `/recipes/` is server-rendered and hydrates with no warning in either ramp, and scrolling loads the second page.
  - All four captures match the reference apart from the differences `NOTES.md` now lists.
  - The owner's backoffice check was handed to the owner: rename a category and see the filter follow about two
    seconds later.
