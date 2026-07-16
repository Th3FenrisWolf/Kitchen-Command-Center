using CMS.ContentEngine;
using CMS.Websites;
using KCC;
using KCC.Contributions.Data;
using KCC.Web.Features.Helpers;
using KCC.Web.Features.Members;
using KCC.Web.Features.Pages.VariantDetail;
using Kentico.Xperience.Lucene.Core.Indexing;
using Lucene.Net.Documents;
using Lucene.Net.Facet;
using Microsoft.Extensions.DependencyInjection;

namespace KCC.Web.Features.Search;

/// <summary>
/// Maps a <see cref="Recipe"/> web page - enriched with its variants and aggregated review rating - to a Lucene
/// document, declares the category and diet facet dimensions, and reindexes the parent recipe when one of its
/// variants changes.
/// </summary>
/// <remarks>
/// The Lucene task processor resolves this strategy from the root service provider, so the scoped services it
/// needs (content query executor, review provider, author resolver, taxonomy retriever) are resolved from an
/// explicitly-created scope inside each operation rather than injected through the constructor.
/// </remarks>
public class RecipeSearchIndexingStrategy(IServiceScopeFactory scopeFactory) : DefaultLuceneIndexingStrategy
{
    private static readonly ContentQueryExecutionOptions QueryOptions = new();

    public override async Task<Document> MapToLuceneDocumentOrNull(IIndexEventItemModel item)
    {
        if (item is not IndexEventWebPageItemModel page ||
            !string.Equals(page.ContentTypeName, Recipe.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        using var scope = scopeFactory.CreateScope();
        var services = scope.ServiceProvider;
        var queryExecutor = services.GetRequiredService<IContentQueryExecutor>();
        var reviews = services.GetRequiredService<IVariantReviewInfoProvider>();
        var authors = services.GetRequiredService<IAuthorNameResolver>();
        var taxonomy = services.GetRequiredService<ITaxonomyRetriever>();

        var recipe = await GetRecipe(queryExecutor, page);
        if (recipe is null)
        {
            return null;
        }

        var variants = await GetVariants(queryExecutor, recipe.SystemFields.WebPageItemID, page.WebsiteChannelName, page.LanguageName);
        var rating = reviews.GetRecipeAverage(recipe.SystemFields.ContentItemGUID);

        var categoryIds = recipe.Categories?.Select(category => category.Identifier) ?? [];
        var categories = await taxonomy.RetrieveTags(categoryIds, page.LanguageName);
        var categoryTitle = categories.FirstOrDefault()?.Title ?? string.Empty;

        var tagIds = variants
            .SelectMany(variant => variant.Tags?.Select(tag => tag.Identifier) ?? [])
            .Distinct();
        var resolvedTags = await taxonomy.RetrieveTags(tagIds, page.LanguageName);
        var diets = resolvedTags.Select(tag => tag.Title).Distinct().ToArray();

        var ingredientNames = variants
            .SelectMany(variant => JsonSerializer.DeserializeCollection<IngredientViewModel>(variant.Ingredients))
            .Select(ingredient => ingredient.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct()
            .ToArray();

        var startedBy = await authors.Resolve(recipe.AuthorMemberGuid) ?? string.Empty;
        var published = recipe.SystemFields.ContentItemCommonDataFirstPublishedWhen ?? recipe.MetadataPublishDate;
        var publishedUtc = DateTime.SpecifyKind(published, DateTimeKind.Utc);

        var model = new RecipeSearchDocument
        {
            Name = recipe.Name ?? string.Empty,
            Slug = recipe.GetUrl().RelativePath,
            Icon = recipe.Icon ?? string.Empty,
            Category = categoryTitle,
            StartedBy = startedBy,
            Description = recipe.Description ?? string.Empty,
            Diets = diets,
            IngredientNames = ingredientNames,
            FastestTime = RecipeSearchDocument.FastestOf(
                variants.Select(variant => (variant.PrepTime, variant.CookTime)).ToArray()),
            VariantCount = variants.Count,
            AverageRating = rating.Average,
            ReviewCount = rating.Count,
            PublishedUnixSeconds = new DateTimeOffset(publishedUtc, TimeSpan.Zero).ToUnixTimeSeconds(),
        };

        return BuildDocument(model);
    }

    public override FacetsConfig FacetsConfigFactory()
    {
        var config = new FacetsConfig();
        config.SetMultiValued(RecipeSearchConstants.FacetCategory, true);
        config.SetMultiValued(RecipeSearchConstants.FacetDiet, true);
        return config;
    }

    // Reindex the parent recipe when one of its variant web pages changes; otherwise defer to the base behaviour
    // (which reindexes the changed recipe itself).
    public override async Task<IEnumerable<IIndexEventItemModel>> FindItemsToReindex(IndexEventWebPageItemModel changedItem)
    {
        if (changedItem is null ||
            !string.Equals(changedItem.ContentTypeName, RecipeVariant.CONTENT_TYPE_NAME, StringComparison.OrdinalIgnoreCase))
        {
            return await base.FindItemsToReindex(changedItem);
        }

        if (changedItem.ParentID is not int parentWebPageItemId)
        {
            return [];
        }

        using var scope = scopeFactory.CreateScope();
        var queryExecutor = scope.ServiceProvider.GetRequiredService<IContentQueryExecutor>();

        var query = new ContentItemQueryBuilder()
            .ForContentType(
                Recipe.CONTENT_TYPE_NAME,
                config => config
                    .ForWebsite(changedItem.WebsiteChannelName)
                    .Where(where => where.WhereEquals(nameof(WebPageFields.WebPageItemID), parentWebPageItemId))
                    .TopN(1))
            .InLanguage(changedItem.LanguageName);

        var recipes = await queryExecutor.GetMappedWebPageResult<Recipe>(query, QueryOptions);

        return recipes
            .Select(recipe => new IndexEventWebPageItemModel(
                recipe.SystemFields.WebPageItemID,
                recipe.SystemFields.WebPageItemGUID,
                changedItem.LanguageName,
                Recipe.CONTENT_TYPE_NAME,
                recipe.SystemFields.WebPageItemName,
                recipe.SystemFields.ContentItemIsSecured,
                recipe.SystemFields.ContentItemContentTypeID,
                recipe.SystemFields.ContentItemCommonDataContentLanguageID,
                changedItem.WebsiteChannelName,
                recipe.SystemFields.WebPageItemTreePath,
                recipe.SystemFields.WebPageItemOrder,
                recipe.SystemFields.WebPageItemParentID))
            .ToList();
    }

    /// <summary>Builds the Lucene <see cref="Document"/> for an already-projected recipe.</summary>
    /// <param name="d">The recipe projection to index.</param>
    /// <returns>A Lucene document carrying the stored, full-text, doc-value and facet fields.</returns>
    internal static Document BuildDocument(RecipeSearchDocument d)
    {
        var doc = new Document
        {
            new TextField(RecipeSearchConstants.FieldName, d.Name, Field.Store.YES),
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

    private static async Task<Recipe> GetRecipe(IContentQueryExecutor queryExecutor, IndexEventWebPageItemModel page)
    {
        var query = new ContentItemQueryBuilder()
            .ForContentType(
                Recipe.CONTENT_TYPE_NAME,
                config => config
                    .WithLinkedItems(1)
                    .ForWebsite(page.WebsiteChannelName)
                    .Where(where => where.WhereEquals(nameof(WebPageFields.WebPageItemGUID), page.ItemGuid))
                    .TopN(1))
            .InLanguage(page.LanguageName);

        var recipes = await queryExecutor.GetMappedWebPageResult<Recipe>(query, QueryOptions);
        return recipes.FirstOrDefault();
    }

    private static async Task<IReadOnlyList<RecipeVariant>> GetVariants(IContentQueryExecutor queryExecutor, int recipeWebPageItemId, string channelName, string language)
    {
        var query = new ContentItemQueryBuilder()
            .ForContentType(
                RecipeVariant.CONTENT_TYPE_NAME,
                config => config
                    .WithLinkedItems(1)
                    .ForWebsite(channelName)
                    .Where(where => where.WhereEquals(nameof(WebPageFields.WebPageItemParentID), recipeWebPageItemId)))
            .InLanguage(language);

        var variants = await queryExecutor.GetMappedWebPageResult<RecipeVariant>(query, QueryOptions);
        return variants.ToList();
    }
}
