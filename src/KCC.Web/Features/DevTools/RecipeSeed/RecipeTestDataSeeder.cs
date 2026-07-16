using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CMS.ContentEngine;
using CMS.ContentEngine.Internal;
using CMS.DataEngine;
using CMS.Membership;
using CMS.Websites;
using KCC.Contributions.Data;
using KCC.Web.Features.Search;
using Kentico.Xperience.Lucene.Core.Indexing;
using Microsoft.Extensions.DependencyInjection;

namespace KCC.Web.Features.DevTools.RecipeSeed;

/// <summary>
/// Development-only seeder that materializes <see cref="RecipeSeedData"/> into published
/// <c>KCC.Recipe</c> / <c>KCC.RecipeVariant</c> web pages (under <c>/Recipes</c>, so the RecipeSearch
/// Lucene index picks them up), creates the category/diet taxonomy tags they reference, seeds
/// <c>VariantReview</c> rows for varied ratings, backdates publish dates so the "recent" sort is
/// meaningful, and finally rebuilds the search index. Idempotent: recipes/tags/members that already
/// exist by name are reused, and reviews upsert on a deterministic member GUID, so re-running tops up
/// rather than duplicating.
/// </summary>
public static class RecipeTestDataSeeder
{
    private const string ChannelName = "KCC";

    private static readonly ContentQueryExecutionOptions QueryOptions = new();

    private static readonly JsonSerializerOptions IngredientJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>Runs the seeder against the supplied (root) service provider.</summary>
    /// <param name="rootServices">The application's root service provider; a scope is created internally.</param>
    /// <param name="log">Destination for human-readable progress output.</param>
    /// <param name="reset">When true, delete any previously-seeded recipes (by name) and their reviews first, so the set is re-created cleanly.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tally of what was created or skipped.</returns>
    public static async Task<SeedSummary> RunAsync(IServiceProvider rootServices, TextWriter log, bool reset = false, CancellationToken ct = default)
    {
        using var scope = rootServices.CreateScope();
        var services = scope.ServiceProvider;

        var managerFactory = services.GetRequiredService<IWebPageManagerFactory>();
        var queryExecutor = services.GetRequiredService<IContentQueryExecutor>();
        var reviews = services.GetRequiredService<IVariantReviewInfoProvider>();
        var reviewRows = services.GetRequiredService<IInfoProvider<VariantReviewInfo>>();
        var luceneClient = services.GetRequiredService<ILuceneClient>();
        var tags = services.GetRequiredService<IInfoProvider<TagInfo>>();
        var taxonomies = services.GetRequiredService<IInfoProvider<TaxonomyInfo>>();
        var members = services.GetRequiredService<IInfoProvider<MemberInfo>>();
        var users = services.GetRequiredService<IInfoProvider<UserInfo>>();
        var channels = services.GetRequiredService<IInfoProvider<ChannelInfo>>();
        var websiteChannels = services.GetRequiredService<IInfoProvider<WebsiteChannelInfo>>();
        var languages = services.GetRequiredService<IInfoProvider<ContentLanguageInfo>>();
        var commonData = services.GetRequiredService<IInfoProvider<ContentItemCommonDataInfo>>();

        var summary = new SeedSummary();

        // ---- Resolve the environment context (channel / language / admin user / parent page) ---------
        var channel = channels.Get().WhereEquals(nameof(ChannelInfo.ChannelName), ChannelName).TopN(1).FirstOrDefault()
            ?? throw new InvalidOperationException($"Channel '{ChannelName}' not found. Has CI restore run?");
        var websiteChannel = websiteChannels.Get()
            .WhereEquals(nameof(WebsiteChannelInfo.WebsiteChannelChannelID), channel.ChannelID).TopN(1).FirstOrDefault()
            ?? throw new InvalidOperationException($"Website channel for '{ChannelName}' not found.");
        var websiteChannelId = websiteChannel.WebsiteChannelID;

        var language = languages.Get().WhereEquals(nameof(ContentLanguageInfo.ContentLanguageIsDefault), true).TopN(1).FirstOrDefault()
            ?? languages.Get().TopN(1).FirstOrDefault()
            ?? throw new InvalidOperationException("No content language configured.");
        var languageName = language.ContentLanguageName;

        var adminUser = users.Get().WhereEquals(nameof(UserInfo.UserName), "administrator").TopN(1).FirstOrDefault()
            ?? users.Get().TopN(1).FirstOrDefault()
            ?? throw new InvalidOperationException("No admin user found to own the seeded pages.");

        var manager = managerFactory.Create(websiteChannelId, adminUser.UserID);

        var recipesParentId = await ResolveRecipesParentAsync(queryExecutor, languageName)
            ?? throw new InvalidOperationException(
                "Could not locate the '/Recipes' parent page (no existing recipe to read its parent from). " +
                "Ensure the CI-seeded recipes are present.");

        log.WriteLine($"Context: channel={ChannelName} (websiteChannelId={websiteChannelId}), language={languageName}, /Recipes parentWebPageItemId={recipesParentId}");

        // ---- Optional reset: delete previously-seeded recipes (by name) + their reviews --------------
        if (reset)
        {
            summary.RecipesDeleted = await DeleteSeededAsync(manager, queryExecutor, reviewRows, languageName, log, ct);
            log.WriteLine($"Reset: deleted {summary.RecipesDeleted} previously-seeded recipe(s).");
        }

        // ---- Ensure taxonomy tags exist, capturing title -> GUID -------------------------------------
        var categoryGuids = EnsureTags(tags, taxonomies, "RecipeCategories", RecipeSeedData.Categories, summary, log, isCategory: true);
        var dietGuids = EnsureTags(tags, taxonomies, "RecipeTags", RecipeSeedData.Diets, summary, log, isCategory: false);

        // ---- Ensure author members exist, capturing key -> member GUID -------------------------------
        var authorGuids = EnsureAuthors(members, RecipeSeedData.Authors, summary, log);

        // ---- Create + publish each recipe and its variants; seed reviews; backdate publish date ------
        var baseUtc = DateTime.UtcNow.Date;
        foreach (var recipe in RecipeSeedData.Recipes)
        {
            ct.ThrowIfCancellationRequested();

            if (await RecipeExistsAsync(queryExecutor, languageName, recipe.Name))
            {
                summary.RecipesSkipped++;
                log.WriteLine($"  skip (exists): {recipe.Name}");
                continue;
            }

            var authorGuid = recipe.AuthorKey is { } key && authorGuids.TryGetValue(key, out var g) ? g : Guid.Empty;
            var recipeData = new ContentItemData(BuildRecipeFields(recipe, categoryGuids, authorGuid));
            var recipePageId = await manager.Create(
                new CreateWebPageParameters(recipe.Name, languageName, new ContentItemParameters(KCC.Recipe.CONTENT_TYPE_NAME, recipeData))
                {
                    ParentWebPageItemID = recipesParentId,
                },
                ct);
            _ = await manager.TryPublish(recipePageId, languageName, ct);

            var recipeItem = await LoadRecipeAsync(queryExecutor, languageName, recipePageId)
                ?? throw new InvalidOperationException($"Failed to read back created recipe '{recipe.Name}'.");
            var recipeGuid = recipeItem.SystemFields.ContentItemGUID;
            summary.RecipesCreated++;

            Guid firstVariantGuid = Guid.Empty;
            for (var i = 0; i < recipe.Variants.Length; i++)
            {
                var variant = recipe.Variants[i];
                var variantData = new ContentItemData(BuildVariantFields(variant, dietGuids, authorGuid));
                var variantPageId = await manager.Create(
                    new CreateWebPageParameters(variant.Name, languageName, new ContentItemParameters(KCC.RecipeVariant.CONTENT_TYPE_NAME, variantData))
                    {
                        ParentWebPageItemID = recipePageId,
                    },
                    ct);
                _ = await manager.TryPublish(variantPageId, languageName, ct);
                summary.VariantsCreated++;

                if (i == 0)
                {
                    var variantItem = await LoadVariantAsync(queryExecutor, languageName, variantPageId);
                    firstVariantGuid = variantItem?.SystemFields.ContentItemGUID ?? Guid.Empty;
                }
            }

            // Reviews attach to the first variant; the recipe-level average aggregates by recipe GUID.
            if (firstVariantGuid != Guid.Empty)
            {
                for (var i = 0; i < recipe.Reviews.Length; i++)
                {
                    var memberGuid = DeterministicGuid($"review::{recipe.Name}::{i}");
                    reviews.Upsert(firstVariantGuid, recipeGuid, memberGuid, recipe.Reviews[i].Rating, $"Seeded review #{i + 1}");
                    summary.ReviewsCreated++;
                }
            }

            // Backdate FirstPublishedWhen (best effort) so the "recent" sort is meaningful.
            TryBackdatePublish(commonData, recipeItem, baseUtc.AddDays(-recipe.PublishedDaysAgo), log);
        }

        // ---- Rebuild the search index so results reflect the seeded data -----------------------------
        // Kentico processes the rebuild on its in-memory Lucene worker; it typically completes within a
        // few seconds. (The freshly-built index isn't visible to a search reader opened earlier in THIS
        // same request, so we don't block here — query /api/recipes/search or reload /recipes to confirm.)
        log.WriteLine("Triggering Lucene index rebuild for 'RecipeSearch' (processes asynchronously)...");
        await luceneClient.Rebuild(RecipeSearchConstants.IndexName, ct);

        log.WriteLine(summary.ToString());
        return summary;
    }

    private static Dictionary<string, object> BuildRecipeFields(SeedRecipe recipe, IReadOnlyDictionary<string, Guid> categoryGuids, Guid authorGuid)
    {
        var fields = new Dictionary<string, object>
        {
            [nameof(KCC.Recipe.Name)] = recipe.Name,
            [nameof(KCC.Recipe.Description)] = recipe.Description,
            [nameof(KCC.Recipe.Icon)] = recipe.Icon,
            [nameof(KCC.Recipe.AuthorMemberGuid)] = authorGuid,
        };

        if (categoryGuids.TryGetValue(recipe.Category, out var catGuid))
        {
            fields[nameof(KCC.Recipe.Categories)] = TagReferenceJson(catGuid);
        }

        return fields;
    }

    private static Dictionary<string, object> BuildVariantFields(SeedVariant variant, IReadOnlyDictionary<string, Guid> dietGuids, Guid authorGuid)
    {
        var fields = new Dictionary<string, object>
        {
            [nameof(KCC.RecipeVariant.Name)] = variant.Name,
            [nameof(KCC.RecipeVariant.Description)] = variant.Description,
            [nameof(KCC.RecipeVariant.Icon)] = variant.Icon,
            [nameof(KCC.RecipeVariant.PrepTime)] = variant.PrepMinutes,
            [nameof(KCC.RecipeVariant.CookTime)] = variant.CookMinutes,
            [nameof(KCC.RecipeVariant.ServingNumber)] = variant.Servings,
            [nameof(KCC.RecipeVariant.Ingredients)] = JsonSerializer.Serialize(
                variant.Ingredients.Select(x => new { name = x.Name, quantity = x.Quantity, unit = x.Unit, isEyeballed = x.IsEyeballed }),
                IngredientJsonOptions),
            [nameof(KCC.RecipeVariant.Instructions)] = JsonSerializer.Serialize(
                variant.Instructions.Select(x => new { step = x.Step, text = x.Text }),
                IngredientJsonOptions),
            [nameof(KCC.RecipeVariant.AuthorMemberGuid)] = authorGuid,
        };

        var guids = variant.Diets
            .Where(dietGuids.ContainsKey)
            .Select(d => dietGuids[d])
            .ToArray();
        if (guids.Length > 0)
        {
            fields[nameof(KCC.RecipeVariant.Tags)] = TagReferenceJson(guids);
        }

        return fields;
    }

    // Taxonomy + content-reference fields serialize as [{"Identifier":"<guid>"}] (PascalCase — the default).
    private static string TagReferenceJson(params Guid[] guids) =>
        JsonSerializer.Serialize(guids.Select(g => new { Identifier = g }));

    private static Dictionary<string, Guid> EnsureTags(
        IInfoProvider<TagInfo> tags,
        IInfoProvider<TaxonomyInfo> taxonomies,
        string taxonomyName,
        IReadOnlyList<string> titles,
        SeedSummary summary,
        TextWriter log,
        bool isCategory)
    {
        var taxonomy = taxonomies.Get().WhereEquals(nameof(TaxonomyInfo.TaxonomyName), taxonomyName).TopN(1).FirstOrDefault()
            ?? throw new InvalidOperationException($"Taxonomy '{taxonomyName}' not found. Has CI restore run?");

        var map = new Dictionary<string, Guid>(StringComparer.Ordinal);
        for (var i = 0; i < titles.Count; i++)
        {
            var title = titles[i];
            var codeName = Codename(title);
            var existing = tags.Get()
                .WhereEquals(nameof(TagInfo.TagTaxonomyID), taxonomy.TaxonomyID)
                .WhereEquals(nameof(TagInfo.TagName), codeName)
                .TopN(1)
                .FirstOrDefault();

            if (existing is not null)
            {
                map[title] = existing.TagGUID;
                continue;
            }

            var tag = new TagInfo
            {
                TagName = codeName,
                TagTitle = title,
                TagTaxonomyID = taxonomy.TaxonomyID,
                TagGUID = DeterministicGuid($"tag::{taxonomyName}::{title}"),
                TagOrder = i + 1,
            };
            tags.Set(tag);
            map[title] = tag.TagGUID;

            if (isCategory)
            {
                summary.CategoryTagsCreated++;
            }
            else
            {
                summary.DietTagsCreated++;
            }

            log.WriteLine($"  tag created: {taxonomyName}/{title}");
        }

        return map;
    }

    private static Dictionary<string, Guid> EnsureAuthors(
        IInfoProvider<MemberInfo> members,
        IReadOnlyList<SeedAuthor> authors,
        SeedSummary summary,
        TextWriter log)
    {
        var map = new Dictionary<string, Guid>(StringComparer.Ordinal);
        foreach (var author in authors)
        {
            var existing = members.Get().WhereEquals(nameof(MemberInfo.MemberName), author.UserName).TopN(1).FirstOrDefault();
            if (existing is not null)
            {
                map[author.Key] = existing.MemberGuid;
                continue;
            }

            var member = new MemberInfo
            {
                MemberName = author.UserName,
                MemberEmail = author.Email,
                MemberEnabled = true,
            };
            member.SetValue("MemberFirstName", author.FirstName);
            member.SetValue("MemberLastName", author.LastName);
            members.Set(member);
            map[author.Key] = member.MemberGuid;
            summary.AuthorsCreated++;
            log.WriteLine($"  author member created: {author.FirstName} {author.LastName} ({author.UserName})");
        }

        return map;
    }

    private static void TryBackdatePublish(IInfoProvider<ContentItemCommonDataInfo> commonData, KCC.Recipe recipe, DateTime whenUtc, TextWriter log)
    {
        try
        {
            var rows = commonData.Get()
                .WhereEquals("ContentItemCommonDataContentItemID", recipe.SystemFields.ContentItemID)
                .WhereEquals("ContentItemCommonDataContentLanguageID", recipe.SystemFields.ContentItemCommonDataContentLanguageID)
                .ToList();

            foreach (var row in rows)
            {
                row.SetValue("ContentItemCommonDataFirstPublishedWhen", whenUtc);
                commonData.Set(row);
            }
        }
        catch (Exception ex)
        {
            log.WriteLine($"  (note) could not backdate publish date for '{recipe.Name}': {ex.Message}");
        }
    }

    // Delete previously-seeded recipes (matched by name) — child variant pages first, then the recipe
    // page, then its review rows — so the set can be re-created cleanly. Best-effort per recipe.
    private static async Task<int> DeleteSeededAsync(
        IWebPageManager manager,
        IContentQueryExecutor queryExecutor,
        IInfoProvider<VariantReviewInfo> reviewRows,
        string languageName,
        TextWriter log,
        CancellationToken ct)
    {
        var deleted = 0;
        foreach (var recipe in RecipeSeedData.Recipes)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var recipeItem = await LoadRecipeByNameAsync(queryExecutor, languageName, recipe.Name);
                if (recipeItem is null)
                {
                    continue;
                }

                var variants = await LoadVariantsByParentAsync(queryExecutor, languageName, recipeItem.SystemFields.WebPageItemID);
                foreach (var variant in variants)
                {
                    await manager.Delete(new DeleteWebPageParameters(variant.SystemFields.WebPageItemID, languageName) { Permanently = true }, ct);
                }

                await manager.Delete(new DeleteWebPageParameters(recipeItem.SystemFields.WebPageItemID, languageName) { Permanently = true }, ct);

                foreach (var row in reviewRows.Get()
                    .WhereEquals(nameof(VariantReviewInfo.RecipeGuid), recipeItem.SystemFields.ContentItemGUID)
                    .ToList())
                {
                    reviewRows.Delete(row);
                }

                deleted++;
                log.WriteLine($"  reset (deleted): {recipe.Name}");
            }
            catch (Exception ex)
            {
                log.WriteLine($"  (note) could not delete '{recipe.Name}': {ex.Message}");
            }
        }

        return deleted;
    }

    private static async Task<KCC.Recipe> LoadRecipeByNameAsync(IContentQueryExecutor queryExecutor, string languageName, string name)
    {
        var query = new ContentItemQueryBuilder()
            .ForContentType(
                KCC.Recipe.CONTENT_TYPE_NAME,
                c => c.ForWebsite(ChannelName).Where(w => w.WhereEquals(nameof(KCC.Recipe.Name), name)).TopN(1))
            .InLanguage(languageName);
        return (await queryExecutor.GetMappedWebPageResult<KCC.Recipe>(query, QueryOptions)).FirstOrDefault();
    }

    private static async Task<IReadOnlyList<KCC.RecipeVariant>> LoadVariantsByParentAsync(IContentQueryExecutor queryExecutor, string languageName, int parentWebPageItemId)
    {
        var query = new ContentItemQueryBuilder()
            .ForContentType(
                KCC.RecipeVariant.CONTENT_TYPE_NAME,
                c => c.ForWebsite(ChannelName).Where(w => w.WhereEquals(nameof(WebPageFields.WebPageItemParentID), parentWebPageItemId)))
            .InLanguage(languageName);
        return (await queryExecutor.GetMappedWebPageResult<KCC.RecipeVariant>(query, QueryOptions)).ToList();
    }

    private static async Task<int?> ResolveRecipesParentAsync(IContentQueryExecutor queryExecutor, string languageName)
    {
        var query = new ContentItemQueryBuilder()
            .ForContentType(KCC.Recipe.CONTENT_TYPE_NAME, c => c.ForWebsite(ChannelName).TopN(1))
            .InLanguage(languageName);
        var recipe = (await queryExecutor.GetMappedWebPageResult<KCC.Recipe>(query, QueryOptions)).FirstOrDefault();
        return recipe?.SystemFields.WebPageItemParentID;
    }

    private static async Task<bool> RecipeExistsAsync(IContentQueryExecutor queryExecutor, string languageName, string name)
    {
        var query = new ContentItemQueryBuilder()
            .ForContentType(
                KCC.Recipe.CONTENT_TYPE_NAME,
                c => c.ForWebsite(ChannelName).Where(w => w.WhereEquals(nameof(KCC.Recipe.Name), name)).TopN(1))
            .InLanguage(languageName);
        return (await queryExecutor.GetMappedWebPageResult<KCC.Recipe>(query, QueryOptions)).Any();
    }

    private static async Task<KCC.Recipe> LoadRecipeAsync(IContentQueryExecutor queryExecutor, string languageName, int webPageItemId)
    {
        var query = new ContentItemQueryBuilder()
            .ForContentType(
                KCC.Recipe.CONTENT_TYPE_NAME,
                c => c.ForWebsite(ChannelName).Where(w => w.WhereEquals(nameof(WebPageFields.WebPageItemID), webPageItemId)).TopN(1))
            .InLanguage(languageName);
        return (await queryExecutor.GetMappedWebPageResult<KCC.Recipe>(query, QueryOptions)).FirstOrDefault();
    }

    private static async Task<KCC.RecipeVariant> LoadVariantAsync(IContentQueryExecutor queryExecutor, string languageName, int webPageItemId)
    {
        var query = new ContentItemQueryBuilder()
            .ForContentType(
                KCC.RecipeVariant.CONTENT_TYPE_NAME,
                c => c.ForWebsite(ChannelName).Where(w => w.WhereEquals(nameof(WebPageFields.WebPageItemID), webPageItemId)).TopN(1))
            .InLanguage(languageName);
        return (await queryExecutor.GetMappedWebPageResult<KCC.RecipeVariant>(query, QueryOptions)).FirstOrDefault();
    }

    // Codename: letters/digits only (e.g. "Gluten-Free" -> "GlutenFree"). Titles are unique, so codenames are too.
    private static string Codename(string title) =>
        new string(title.Where(char.IsLetterOrDigit).ToArray());

    // Stable GUID from a seed string (MD5 of the UTF-8 bytes), so tags and review authors are idempotent.
    private static Guid DeterministicGuid(string seed) =>
        new(MD5.HashData(Encoding.UTF8.GetBytes(seed)));
}

/// <summary>Tally of what the seeder created (or skipped) in a run.</summary>
public sealed class SeedSummary
{
    public int CategoryTagsCreated { get; set; }
    public int DietTagsCreated { get; set; }
    public int AuthorsCreated { get; set; }
    public int RecipesCreated { get; set; }
    public int RecipesSkipped { get; set; }
    public int RecipesDeleted { get; set; }
    public int VariantsCreated { get; set; }
    public int ReviewsCreated { get; set; }

    public override string ToString() =>
        $"Seed complete: recipes +{RecipesCreated} (skipped {RecipesSkipped}, deleted {RecipesDeleted}), " +
        $"variants +{VariantsCreated}, reviews +{ReviewsCreated}, category tags +{CategoryTagsCreated}, " +
        $"diet tags +{DietTagsCreated}, authors +{AuthorsCreated}.";
}
