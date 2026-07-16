using CMS.Core;
using Kentico.Xperience.Lucene;
using Kentico.Xperience.Lucene.Core.Indexing;

namespace KCC.Web.Features.Search;

/// <summary>
/// Logs a single-recipe Lucene UPDATE task per indexed web-page item. The built-in Lucene queue worker
/// then re-maps the recipe through <see cref="RecipeSearchIndexingStrategy"/>, which re-reads the (now
/// cache-invalidated) review aggregate, so the index's rating fields stay live.
/// </summary>
public class RecipeReindexer(
    IRecipeReindexTargetResolver targetResolver,
    ILuceneTaskLogger taskLogger,
    IEventLogService eventLog) : IRecipeReindexer
{
    /// <inheritdoc />
    public async Task ReindexRecipeAsync(Guid recipeContentItemGuid, CancellationToken cancellationToken = default)
    {
        try
        {
            var targets = await targetResolver.ResolveAsync(recipeContentItemGuid, cancellationToken);
            foreach (var target in targets)
            {
                taskLogger.LogIndexTask(
                    new LuceneQueueItem(target, LuceneTaskType.UPDATE, RecipeSearchConstants.IndexName));
            }
        }
        catch (Exception ex)
        {
            // The review write is already committed; a search-index problem must never surface to the
            // member. Log and swallow so the write path stays green (admin Rebuild is the backstop).
            eventLog.LogException(nameof(RecipeReindexer), "REINDEX", ex);
        }
    }
}
