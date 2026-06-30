namespace KCC.Contributions.Data;

/// <summary>Average rating and count for a variant or recipe.</summary>
public readonly record struct RatingAggregate(double Average, int Count);

public partial interface IVariantReviewInfoProvider
{
    RatingAggregate GetAverageForVariant(Guid variantGuid);
    IReadOnlyDictionary<Guid, RatingAggregate> GetAveragesForVariants(IReadOnlyCollection<Guid> variantGuids);
    RatingAggregate GetRecipeAverage(Guid recipeGuid);
    VariantReviewInfo GetMemberReview(Guid variantGuid, Guid memberGuid);
    void Upsert(Guid variantGuid, Guid recipeGuid, Guid memberGuid, int rating, string reviewText);
    bool DeleteOwn(Guid variantGuid, Guid memberGuid);
}
