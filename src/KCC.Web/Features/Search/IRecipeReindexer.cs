namespace KCC.Web.Features.Search;

/// <summary>
/// Enqueues a Lucene reindex of a single recipe (identified by its content-item GUID) so the index's
/// derived rating fields refresh after a review is written. Reviews are a custom module, not content
/// items, so Kentico's own indexing events never fire for them.
/// </summary>
public interface IRecipeReindexer
{
    /// <summary>
    /// Enqueues an UPDATE task for the recipe in every language the index covers. A no-op when the
    /// index is absent or the recipe cannot be resolved. Never throws into the caller.
    /// </summary>
    /// <param name="recipeContentItemGuid">The recipe's content-item GUID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous reindex operation.</returns>
    Task ReindexRecipeAsync(Guid recipeContentItemGuid, CancellationToken cancellationToken = default);
}
