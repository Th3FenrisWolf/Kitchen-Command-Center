using CMS.ContentEngine;
using CMS.Websites;
using KCC;
using KCC.Web.Features.Models.Constants;
using Kentico.Xperience.Lucene.Core.Indexing;

namespace KCC.Web.Features.Search;

/// <inheritdoc />
/// <remarks>
/// Resolved from a caller-created DI scope (see <c>VariantReviewSearchModule</c>), so it injects
/// the scoped <see cref="IContentQueryExecutor"/> directly. The root-provider constraint that forces
/// <see cref="RecipeSearchIndexingStrategy"/> to use <c>IServiceScopeFactory</c> does not apply here,
/// because this type is never resolved from the root provider.
/// </remarks>
public class RecipeReindexTargetResolver(
    ILuceneIndexManager indexManager,
    IContentQueryExecutor queryExecutor) : IRecipeReindexTargetResolver
{
    private static readonly ContentQueryExecutionOptions QueryOptions = new();

    /// <inheritdoc />
    public async Task<IReadOnlyList<IndexEventWebPageItemModel>> ResolveAsync(
        Guid recipeContentItemGuid, CancellationToken cancellationToken = default)
    {
        // GetIndex returns null (rather than throwing) when the index has not been created yet; treat
        // that as "nothing to reindex" so a review write on a not-yet-provisioned environment is safe.
        var index = indexManager.GetIndex(RecipeSearchConstants.IndexName);
        if (index is null)
        {
            return [];
        }

        // Recipe web pages live in the site's single website channel. The runtime LuceneIndex no longer
        // exposes a usable channel accessor (WebSiteChannelName is obsolete and always empty in 15.0.3;
        // the per-channel configuration is internal), so use the same channel constant every page
        // controller uses. The index is still required above for its configured languages.
        var channelName = XperienceConstants.WebsiteChannelName;

        var models = new List<IndexEventWebPageItemModel>();
        foreach (var language in index.LanguageNames)
        {
            var query = new ContentItemQueryBuilder()
                .ForContentType(
                    Recipe.CONTENT_TYPE_NAME,
                    config => config
                        .ForWebsite(channelName)
                        .Where(where => where.WhereEquals(
                            nameof(IContentQueryDataContainer.ContentItemGUID), recipeContentItemGuid))
                        .TopN(1))
                .InLanguage(language);

            var recipes = await queryExecutor.GetMappedWebPageResult<Recipe>(query, QueryOptions, cancellationToken);
            var recipe = recipes.FirstOrDefault();
            if (recipe is null)
            {
                // Recipe is not published / not present in this language — nothing to reindex here.
                continue;
            }

            // Same 12-arg projection as RecipeSearchIndexingStrategy.FindItemsToReindex.
            models.Add(new IndexEventWebPageItemModel(
                recipe.SystemFields.WebPageItemID,
                recipe.SystemFields.WebPageItemGUID,
                language,
                Recipe.CONTENT_TYPE_NAME,
                recipe.SystemFields.WebPageItemName,
                recipe.SystemFields.ContentItemIsSecured,
                recipe.SystemFields.ContentItemContentTypeID,
                recipe.SystemFields.ContentItemCommonDataContentLanguageID,
                channelName,
                recipe.SystemFields.WebPageItemTreePath,
                recipe.SystemFields.WebPageItemOrder,
                recipe.SystemFields.WebPageItemParentID));
        }

        return models;
    }
}
