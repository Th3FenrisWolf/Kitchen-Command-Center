using CMS.ContentEngine;
using CMS.ContentEngine.Internal;
using CMS.Helpers;
using CMS.Membership;
using KCC.Contributions.Data;
using Kentico.Xperience.Admin.Base;

namespace KCC.Contributions.Admin.Overview;

public class ContributionsOverviewService(
    IRecipeRollupSource rollupSource,
    IVariantReviewInfoProvider reviewProvider,
    IVariantCookNoteInfoProvider cookNoteProvider,
    IVariantCookedInfoProvider cookedProvider,
    ContentItemNameLookup contentItemNameLookup,
    MemberNameLookup memberNameLookup,
    IProgressiveCache cache,
    IPageLinkGenerator pageLinkGenerator)
{
    internal const int RecipePageSize = 20;
    internal const int EntryPageSize = 10;
    private const int CacheMinutes = 60;
    private const int SnippetLength = 200;

    public async Task<RecipeOverviewPageResult> GetRecipesAsync(RecipeOverviewQueryArgs args, CancellationToken cancellationToken)
    {
        var query = NormalizeQuery(args);
        var page = await cache.LoadAsync(
            (cs, ct) =>
            {
                cs.CacheDependency = CacheHelper.GetCacheDependency(
                [
                    $"{VariantReviewInfo.OBJECT_TYPE}|all",
                    $"{VariantCookNoteInfo.OBJECT_TYPE}|all",
                    $"{VariantCookedInfo.OBJECT_TYPE}|all",
                    $"{ContentItemInfo.OBJECT_TYPE}|all",
                    $"{ContentItemLanguageMetadataInfo.OBJECT_TYPE}|all",
                ]);
                return rollupSource.GetPageAsync(query, ct);
            },
            new(CacheMinutes, nameof(ContributionsOverviewService), nameof(GetRecipesAsync), query.Page, query.SearchText, query.MaxAverageRating),
            cancellationToken);

        var recipes = page.Rows
            .Select(r => new RecipeOverviewRow(
                r.RecipeGuid,
                string.IsNullOrEmpty(r.DisplayName) ? ContentItemNameLookup.DeletedFallback : r.DisplayName,
                r.AverageRating,
                r.ReviewCount,
                r.NoteCount,
                r.CookedCount,
                r.LastActivity))
            .ToArray();

        return new RecipeOverviewPageResult(recipes, page.TotalCount, query.Page, query.PageSize);
    }

    internal static RecipeRollupQuery NormalizeQuery(RecipeOverviewQueryArgs args)
    {
        var search = string.IsNullOrWhiteSpace(args.SearchText) ? null : args.SearchText.Trim();
        var maxAverageRating = args.MaxAverageRating is { } max ? Math.Clamp(max, 0.5, 5d) : (double?)null;
        return new RecipeRollupQuery(Math.Max(0, args.Page), RecipePageSize, search, maxAverageRating);
    }

    public RecipeVariantsResult GetRecipeVariants(Guid recipeGuid)
    {
        var variantGuids = CombineDistinct(
            GuidsForRecipe(reviewProvider.Get(), nameof(VariantReviewInfo.RecipeGuid), nameof(VariantReviewInfo.VariantGuid), recipeGuid),
            GuidsForRecipe(cookNoteProvider.Get(), nameof(VariantCookNoteInfo.RecipeGuid), nameof(VariantCookNoteInfo.VariantGuid), recipeGuid),
            GuidsForRecipe(cookedProvider.Get(), nameof(VariantCookedInfo.RecipeGuid), nameof(VariantCookedInfo.VariantGuid), recipeGuid));

        var rows = BuildVariantRows(
            variantGuids,
            contentItemNameLookup.DisplayNames(),
            reviewProvider.GetAveragesForVariants(variantGuids),
            cookNoteProvider.GetNoteCountsForVariants(variantGuids),
            cookedProvider.GetCookedCountsForVariants(variantGuids));

        return new RecipeVariantsResult(rows);
    }

    private static IEnumerable<Guid> GuidsForRecipe<TInfo>(
        CMS.DataEngine.ObjectQuery<TInfo> query,
        string recipeColumn,
        string variantColumn,
        Guid recipeGuid)
        where TInfo : CMS.DataEngine.AbstractInfo<TInfo>, new() =>
        query
            .WhereEquals(recipeColumn, recipeGuid)
            .Column(variantColumn)
            .GetListResult<Guid>();

    internal static IReadOnlyList<Guid> CombineDistinct(params IEnumerable<Guid>[] sources) =>
        [..sources.SelectMany(s => s).Where(g => g != Guid.Empty).Distinct()];

    internal static IReadOnlyList<VariantOverviewRow> BuildVariantRows(
        IReadOnlyList<Guid> variantGuids,
        IReadOnlyDictionary<Guid, string> contentItemNames,
        IReadOnlyDictionary<Guid, RatingAggregate> ratings,
        IReadOnlyDictionary<Guid, int> noteCounts,
        IReadOnlyDictionary<Guid, int> cookedCounts) =>
        [..variantGuids
            .Select(guid =>
            {
                var rating = ratings.TryGetValue(guid, out var aggregate) ? aggregate : new RatingAggregate(0d, 0);
                return new VariantOverviewRow(
                    guid,
                    ContentItemNameLookup.DisplayOrDeleted(contentItemNames, guid),
                    rating.Count > 0 ? rating.Average : null,
                    rating.Count,
                    noteCounts.GetValueOrDefault(guid),
                    cookedCounts.GetValueOrDefault(guid));
            })
            .OrderBy(row => row.VariantName, StringComparer.OrdinalIgnoreCase)];

    public ReviewEntriesResult GetVariantReviews(VariantEntriesArgs args)
    {
        var page = Math.Max(0, args.Page);
        var reviews = reviewProvider.GetForVariant(args.VariantGuid, page, EntryPageSize, out var totalCount);
        var members = memberNameLookup.Displays();

        var entries = reviews
            .Select(r => new ReviewEntry(
                r.VariantReviewID,
                MemberNameLookup.DisplayOrDeleted(members, r.MemberGuid),
                r.Rating,
                Snippet(r.ReviewText),
                r.ReviewCreated,
                AdminPath.ToAdminUrl(pageLinkGenerator.GetPath<ReviewsEditPage>(new PageParameterValues
                {
                    { typeof(ReviewsSectionPage), r.VariantReviewID },
                }))))
            .ToArray();

        return new ReviewEntriesResult(entries, totalCount, page, EntryPageSize);
    }

    public CookNoteEntriesResult GetVariantCookNotes(VariantEntriesArgs args)
    {
        var page = Math.Max(0, args.Page);
        var notes = cookNoteProvider.GetForVariant(args.VariantGuid, page, EntryPageSize, out var totalCount);
        var members = memberNameLookup.Displays();

        var entries = notes
            .Select(n => new CookNoteEntry(
                n.VariantCookNoteID,
                MemberNameLookup.DisplayOrDeleted(members, n.MemberGuid),
                Snippet(n.NoteText),
                n.NoteCreated,
                AdminPath.ToAdminUrl(pageLinkGenerator.GetPath<CookNotesEditPage>(new PageParameterValues
                {
                    { typeof(CookNotesSectionPage), n.VariantCookNoteID },
                }))))
            .ToArray();

        return new CookNoteEntriesResult(entries, totalCount, page, EntryPageSize);
    }

    public bool DeleteReview(int id)
    {
        var review = reviewProvider.Get(id);
        if (review is null)
        {
            return false;
        }

        reviewProvider.Delete(review);
        return true;
    }

    public bool DeleteCookNote(int id)
    {
        var note = cookNoteProvider.Get(id);
        if (note is null)
        {
            return false;
        }

        cookNoteProvider.Delete(note);
        return true;
    }

    /// <summary>Single-line preview, capped for the overview list; full text lives on the edit page.</summary>
    internal static string Snippet(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var collapsed = string.Join(" ", text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
        return collapsed.Length <= SnippetLength ? collapsed : $"{collapsed[..SnippetLength].TrimEnd()}…";
    }
}
