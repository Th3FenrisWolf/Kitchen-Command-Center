using Kentico.Xperience.Lucene.Core.Indexing;

namespace KCC.Web.Features.Search;

/// <summary>
/// Resolves the Lucene web-page event models to reindex for a recipe identified by its content-item
/// GUID — one per language the RecipeSearch index covers, in the index's website channel.
/// </summary>
public interface IRecipeReindexTargetResolver
{
    /// <summary>
    /// Resolves the reindex targets for the recipe.
    /// </summary>
    /// <param name="recipeContentItemGuid">The recipe's content-item GUID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>One web-page event model per language the RecipeSearch index covers, in the index's website channel; empty when the index is absent or the recipe cannot be resolved.</returns>
    Task<IReadOnlyList<IndexEventWebPageItemModel>> ResolveAsync(
        Guid recipeContentItemGuid, CancellationToken cancellationToken = default);
}
