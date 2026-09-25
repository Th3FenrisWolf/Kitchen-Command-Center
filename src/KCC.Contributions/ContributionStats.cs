namespace KCC.Contributions;

public readonly record struct RatingAggregate(double Average, int Count);

public sealed record VariantStats(decimal RatingSum, int ReviewCount, IReadOnlyList<int> Distribution, int CookedCount)
{
    public static VariantStats None { get; } = new(0m, 0, new int[5], 0);

    public RatingAggregate Rating => ReviewCount == 0 ? default : new((double)(RatingSum / ReviewCount), ReviewCount);
}

public sealed class ContributionStats
{
    private readonly IReadOnlyDictionary<Guid, VariantStats> byVariant;

    private ContributionStats(IReadOnlyDictionary<Guid, VariantStats> byVariant) => this.byVariant = byVariant;

    public static ContributionStats Build(IEnumerable<(Guid VariantKey, decimal Rating)> reviews, IEnumerable<Guid> cookedVariantKeys)
    {
        var ratings = reviews
            .GroupBy(review => review.VariantKey)
            .ToDictionary(group => group.Key, group => group.Select(review => review.Rating).ToList());
        var cooked = cookedVariantKeys
            .GroupBy(key => key)
            .ToDictionary(group => group.Key, group => group.Count());

        return new ContributionStats(ratings.Keys.Union(cooked.Keys).ToDictionary(
            key => key,
            key =>
            {
                var variantRatings = ratings.GetValueOrDefault(key) ?? [];
                return new VariantStats(variantRatings.Sum(), variantRatings.Count, RatingMath.Distribution(variantRatings), cooked.GetValueOrDefault(key));
            }));
    }

    public VariantStats For(Guid variantKey) => byVariant.GetValueOrDefault(variantKey) ?? VariantStats.None;

    public RatingAggregate RatingAcross(IEnumerable<Guid> variantKeys)
    {
        var stats = variantKeys.Distinct().Select(For).ToList();
        var count = stats.Sum(variant => variant.ReviewCount);
        return count == 0 ? default : new((double)(stats.Sum(variant => variant.RatingSum) / count), count);
    }

    public int CookedAcross(IEnumerable<Guid> variantKeys) =>
        variantKeys.Distinct().Sum(key => For(key).CookedCount);
}
