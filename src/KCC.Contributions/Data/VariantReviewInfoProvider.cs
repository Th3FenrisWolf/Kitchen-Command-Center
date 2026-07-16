using CMS.Core;
using CMS.Helpers;

namespace KCC.Contributions.Data;

public partial class VariantReviewInfoProvider
{
    private const int CacheMinutes = 60;
    private const int MaxReviewLength = 4000;

    /// <summary>Mean rating + count over a set of ratings. Empty → (0, 0).</summary>
    internal static RatingAggregate Aggregate(IReadOnlyCollection<int> ratings) =>
        ratings.Count == 0 ? new RatingAggregate(0d, 0) : new RatingAggregate(ratings.Average(), ratings.Count);

    /// <summary>Per-variant mean rating + count.</summary>
    internal static IReadOnlyDictionary<Guid, RatingAggregate> AggregateByVariant(IEnumerable<(Guid VariantGuid, int Rating)> reviews) =>
        reviews.GroupBy(r => r.VariantGuid).ToDictionary(g => g.Key, g => Aggregate(g.Select(x => x.Rating).ToArray()));

    public static bool IsValidRating(int rating) => rating is >= 1 and <= 5;

    internal static bool CanModify(Guid? authorMemberGuid, Guid memberGuid) =>
        authorMemberGuid is { } author && author == memberGuid;

    internal static string ClampText(string text) =>
        string.IsNullOrWhiteSpace(text) ? null : text.Trim().Length <= MaxReviewLength ? text.Trim() : text.Trim()[..MaxReviewLength];

    private static string[] CacheKeys =>
    [
        $"{VariantReviewInfo.OBJECT_TYPE}|all",
    ];

    public RatingAggregate GetAverageForVariant(Guid variantGuid)
    {
        var cache = Service.Resolve<IProgressiveCache>();
        return cache.Load(
            cs =>
            {
                cs.CacheDependency = CacheHelper.GetCacheDependency(CacheKeys);
                var reviews = Get().WhereEquals(nameof(VariantReviewInfo.VariantGuid), variantGuid);
                var ratings = reviews.Select(r => r.Rating);
                return Aggregate([..ratings]);
            },
            new(CacheMinutes, VariantReviewInfo.OBJECT_TYPE, nameof(GetAverageForVariant), variantGuid));
    }

    public IReadOnlyDictionary<Guid, RatingAggregate> GetAveragesForVariants(IReadOnlyCollection<Guid> variantGuids)
    {
        if (variantGuids.Count is 0)
        {
            return new Dictionary<Guid, RatingAggregate>();
        }

        var cache = Service.Resolve<IProgressiveCache>();
        return cache.Load(
            cs =>
            {
                cs.CacheDependency = CacheHelper.GetCacheDependency(CacheKeys);
                var reviews = Get().WhereIn(nameof(VariantReviewInfo.VariantGuid), variantGuids).ToArray();
                var pairs = reviews.Select(r => (r.VariantGuid, r.Rating));
                return AggregateByVariant(pairs);
            },
            new(CacheMinutes, VariantReviewInfo.OBJECT_TYPE, nameof(GetAveragesForVariants), string.Join("|", variantGuids)));
    }

    public RatingAggregate GetRecipeAverage(Guid recipeGuid)
    {
        var cache = Service.Resolve<IProgressiveCache>();
        return cache.Load(
            cs =>
            {
                cs.CacheDependency = CacheHelper.GetCacheDependency(CacheKeys);
                var reviews = Get().WhereEquals(nameof(VariantReviewInfo.RecipeGuid), recipeGuid);
                var ratings = reviews.Select(r => r.Rating);
                return Aggregate([..ratings]);
            },
            new(CacheMinutes, VariantReviewInfo.OBJECT_TYPE, nameof(GetRecipeAverage), recipeGuid));
    }

    public IReadOnlyList<VariantReviewInfo> GetForVariant(Guid variantGuid, int page, int pageSize, out int totalCount)
    {
        var query = Get().WhereEquals(nameof(VariantReviewInfo.VariantGuid), variantGuid);
        totalCount = query.Count;
        return [..query
            .OrderByDescending(nameof(VariantReviewInfo.ReviewCreated))
            .Page(Math.Max(0, page), Math.Max(1, pageSize))];
    }

    public VariantReviewInfo GetMemberReview(Guid variantGuid, Guid memberGuid) => Get()
        .WhereEquals(nameof(VariantReviewInfo.VariantGuid), variantGuid)
        .WhereEquals(nameof(VariantReviewInfo.MemberGuid), memberGuid)
        .TopN(1)
        .FirstOrDefault();

    public void Upsert(Guid variantGuid, Guid recipeGuid, Guid memberGuid, int rating, string reviewText)
    {
        var existing = GetMemberReview(variantGuid, memberGuid);
        var now = DateTime.UtcNow;
        var text = ClampText(reviewText);

        if (existing is null)
        {
            Set(new VariantReviewInfo
            {
                VariantGuid = variantGuid,
                RecipeGuid = recipeGuid,
                MemberGuid = memberGuid,
                Rating = rating,
                ReviewText = text,
                ReviewCreated = now,
                ReviewModified = now,
            });
            return;
        }

        existing.Rating = rating;
        existing.ReviewText = text;
        existing.ReviewModified = now;
        Set(existing);
    }

    public bool DeleteOwn(Guid variantGuid, Guid memberGuid)
    {
        var existing = GetMemberReview(variantGuid, memberGuid);
        if (existing is null || !CanModify(existing.MemberGuid, memberGuid))
        {
            return false;
        }

        Delete(existing);
        return true;
    }
}
