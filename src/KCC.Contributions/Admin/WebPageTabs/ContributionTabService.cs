using CMS.Helpers;
using KCC.Contributions.Data;

namespace KCC.Contributions.Admin.WebPageTabs;

public sealed class ContributionTabService(
    IVariantReviewInfoProvider reviewProvider,
    IVariantCookNoteInfoProvider cookNoteProvider,
    IVariantCookedInfoProvider cookedProvider,
    MemberNameLookup memberNameLookup,
    IProgressiveCache cache)
{
    internal const int EntryPageSize = 10;

    private const int CacheMinutes = 60;

    private static readonly string[] CacheKeys =
    [
        $"{VariantReviewInfo.OBJECT_TYPE}|all",
        $"{VariantCookNoteInfo.OBJECT_TYPE}|all",
        $"{VariantCookedInfo.OBJECT_TYPE}|all",
    ];

    /// <remarks>
    /// Filtered on the recipe rather than on the recipe's current variant pages, so that a
    /// contribution whose variant page has since been deleted still counts towards the recipe. The
    /// caller separates those out by variant GUID.
    /// </remarks>
    public RecipeContributionRollup GetRecipeRollup(Guid recipeGuid) => cache.Load(
        cs =>
        {
            cs.CacheDependency = CacheHelper.GetCacheDependency(CacheKeys);

            var reviews = reviewProvider.Get()
                .WhereEquals(nameof(VariantReviewInfo.RecipeGuid), recipeGuid)
                .Columns(
                    nameof(VariantReviewInfo.VariantGuid),
                    nameof(VariantReviewInfo.Rating),
                    nameof(VariantReviewInfo.ReviewCreated))
                .ToArray()
                .Select(r => (r.VariantGuid, r.Rating, r.ReviewCreated));

            var notes = cookNoteProvider.Get()
                .WhereEquals(nameof(VariantCookNoteInfo.RecipeGuid), recipeGuid)
                .Columns(nameof(VariantCookNoteInfo.VariantGuid), nameof(VariantCookNoteInfo.NoteCreated))
                .ToArray()
                .Select(n => (n.VariantGuid, n.NoteCreated));

            var cooked = cookedProvider.Get()
                .WhereEquals(nameof(VariantCookedInfo.RecipeGuid), recipeGuid)
                .Columns(nameof(VariantCookedInfo.VariantGuid), nameof(VariantCookedInfo.CookedCreated))
                .ToArray()
                .Select(c => (c.VariantGuid, c.CookedCreated));

            return Aggregate(reviews, notes, cooked);
        },
        new(CacheMinutes, nameof(ContributionTabService), nameof(GetRecipeRollup), recipeGuid));

    internal static RecipeContributionRollup Aggregate(
        IEnumerable<(Guid VariantGuid, decimal Rating, DateTime Created)> reviews,
        IEnumerable<(Guid VariantGuid, DateTime Created)> notes,
        IEnumerable<(Guid VariantGuid, DateTime Created)> cooked)
    {
        var reviewRows = reviews.ToArray();
        var noteRows = notes.ToArray();
        var cookedRows = cooked.ToArray();

        var byVariant = reviewRows.Select(r => r.VariantGuid)
            .Concat(noteRows.Select(n => n.VariantGuid))
            .Concat(cookedRows.Select(c => c.VariantGuid))
            .Distinct()
            .ToDictionary(
                variantGuid => variantGuid,
                variantGuid => TotalsFor(
                    reviewRows.Where(r => r.VariantGuid == variantGuid).ToArray(),
                    noteRows.Where(n => n.VariantGuid == variantGuid).ToArray(),
                    cookedRows.Where(c => c.VariantGuid == variantGuid).ToArray()));

        return new RecipeContributionRollup(TotalsFor(reviewRows, noteRows, cookedRows), byVariant);
    }

    private static ContributionTotals TotalsFor(
        IReadOnlyCollection<(Guid VariantGuid, decimal Rating, DateTime Created)> reviews,
        IReadOnlyCollection<(Guid VariantGuid, DateTime Created)> notes,
        IReadOnlyCollection<(Guid VariantGuid, DateTime Created)> cooked)
    {
        var timestamps = reviews.Select(r => r.Created)
            .Concat(notes.Select(n => n.Created))
            .Concat(cooked.Select(c => c.Created))
            .ToArray();

        return new ContributionTotals(
            reviews.Count == 0 ? null : (double)reviews.Average(r => r.Rating),
            reviews.Count,
            notes.Count,
            cooked.Count,
            timestamps.Length == 0 ? null : timestamps.Max());
    }

    /// <summary>Totals for everything under the recipe that no current variant page accounts for.</summary>
    internal static ContributionTotals OrphanedTotals(
        RecipeContributionRollup rollup,
        IReadOnlyCollection<Guid> liveVariantGuids)
    {
        var orphans = rollup.ByVariant
            .Where(pair => !liveVariantGuids.Contains(pair.Key))
            .Select(pair => pair.Value)
            .ToArray();

        if (orphans.Length == 0)
        {
            return ContributionTotals.Empty;
        }

        var rated = orphans.Where(o => o.AverageRating is not null && o.ReviewCount > 0).ToArray();
        var reviewCount = rated.Sum(o => o.ReviewCount);
        var lastActivity = orphans.Select(o => o.LastActivity).Where(d => d is not null).ToArray();

        return new ContributionTotals(
            reviewCount == 0 ? null : rated.Sum(o => o.AverageRating!.Value * o.ReviewCount) / reviewCount,
            orphans.Sum(o => o.ReviewCount),
            orphans.Sum(o => o.NoteCount),
            orphans.Sum(o => o.CookedCount),
            lastActivity.Length == 0 ? null : lastActivity.Max());
    }

    public ContributionEntryPage GetReviews(Guid variantGuid, int page, Func<int, string> editUrl)
    {
        var reviews = reviewProvider.GetForVariant(variantGuid, Math.Max(0, page), EntryPageSize, out var totalCount);
        var members = memberNameLookup.Displays();

        return new ContributionEntryPage(
            [
                ..reviews.Select(review => new ContributionEntry(
                    review.VariantReviewID,
                    MemberNameLookup.DisplayOrDeleted(members, review.MemberGuid),
                    review.Rating,
                    review.ReviewText ?? string.Empty,
                    review.ReviewCreated,
                    editUrl(review.VariantReviewID)))
            ],
            totalCount,
            Math.Max(0, page));
    }

    public ContributionEntryPage GetCookNotes(Guid variantGuid, int page, Func<int, string> editUrl)
    {
        var notes = cookNoteProvider.GetForVariant(variantGuid, Math.Max(0, page), EntryPageSize, out var totalCount);
        var members = memberNameLookup.Displays();

        return new ContributionEntryPage(
            [
                ..notes.Select(note => new ContributionEntry(
                    note.VariantCookNoteID,
                    MemberNameLookup.DisplayOrDeleted(members, note.MemberGuid),
                    null,
                    note.NoteText ?? string.Empty,
                    note.NoteCreated,
                    editUrl(note.VariantCookNoteID)))
            ],
            totalCount,
            Math.Max(0, page));
    }

    public ContributionTotals GetVariantTotals(Guid variantGuid)
    {
        var rating = reviewProvider.GetAverageForVariant(variantGuid);
        var notes = cookNoteProvider.GetNoteCountsForVariants([variantGuid]);
        var cooked = cookedProvider.GetCookedCountForVariant(variantGuid);

        return new ContributionTotals(
            rating.Count == 0 ? null : rating.Average,
            rating.Count,
            notes.GetValueOrDefault(variantGuid),
            cooked,
            null);
    }

    public IReadOnlyList<int> GetRatingDistribution(Guid variantGuid) =>
        reviewProvider.GetDistributionForVariant(variantGuid);

    /// <remarks>
    /// The id arrives from the client and the page it arrives on authorises one variant only, so a
    /// review filed against another variant is refused rather than deleted.
    /// </remarks>
    public bool DeleteReview(int id, Guid variantGuid)
    {
        var review = reviewProvider.Get(id);

        if (review is null || review.VariantGuid != variantGuid)
        {
            return false;
        }

        reviewProvider.Delete(review);

        return true;
    }

    /// <inheritdoc cref="DeleteReview"/>
    public bool DeleteCookNote(int id, Guid variantGuid)
    {
        var note = cookNoteProvider.Get(id);

        if (note is null || note.VariantGuid != variantGuid)
        {
            return false;
        }

        cookNoteProvider.Delete(note);

        return true;
    }
}
