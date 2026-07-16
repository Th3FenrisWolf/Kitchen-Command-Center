using CMS;
using CMS.Core;
using CMS.DataEngine;
using KCC.Contributions.Data;
using KCC.Web.Features.Search;
using Microsoft.Extensions.DependencyInjection;

[assembly: RegisterModule(typeof(VariantReviewSearchModule))]

namespace KCC.Web.Features.Search;

/// <summary>
/// Keeps the recipe Lucene index's rating fields live. When a <see cref="VariantReviewInfo"/> is written
/// (insert/update/delete) — including outside the review API — reindexes the parent recipe. Reviews are a
/// custom module, not content items, so Kentico's own indexing events never fire for them, and the
/// strategy's <c>FindItemsToReindex</c> is never triggered by a review write.
/// </summary>
public class VariantReviewSearchModule : Module
{
    public VariantReviewSearchModule()
        : base("KCC.Web.VariantReviewSearch")
    {
    }

    protected override void OnInit()
    {
        base.OnInit();

        // Typed per-type events for VariantReviewInfo: these fire only for review writes, not for every
        // object in the app. The global ObjectEvents.Insert/Update/Delete.After handlers are an
        // equivalent alternative (they would need the same VariantReviewInfo type filter below).
        VariantReviewInfo.TYPEINFO.Events.Insert.After += ReindexParentRecipe;
        VariantReviewInfo.TYPEINFO.Events.Update.After += ReindexParentRecipe;
        VariantReviewInfo.TYPEINFO.Events.Delete.After += ReindexParentRecipe;
    }

    // Object events are synchronous. The handler creates a DI scope (module init runs in the root
    // context, so we must NOT resolve scoped services from the root provider directly) and blocks on the
    // reindexer. The work is one content query per indexed language plus an in-memory enqueue, and
    // ASP.NET Core has no synchronization context, so blocking here cannot deadlock.
    private static void ReindexParentRecipe(object sender, ObjectEventArgs e)
    {
        if (e.Object is not VariantReviewInfo review || review.RecipeGuid == Guid.Empty)
        {
            return;
        }

        try
        {
            using var scope = Service.Resolve<IServiceScopeFactory>().CreateScope();
            var reindexer = scope.ServiceProvider.GetRequiredService<IRecipeReindexer>();
            reindexer.ReindexRecipeAsync(review.RecipeGuid).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            // The review write is already committed; a search-index or DI-resolution problem must never
            // surface to the member. Log and swallow so the write path stays green.
            Service.Resolve<IEventLogService>().LogException(nameof(VariantReviewSearchModule), "REINDEX", ex);
        }
    }
}
