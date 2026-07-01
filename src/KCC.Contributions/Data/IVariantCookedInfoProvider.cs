namespace KCC.Contributions.Data;

public partial interface IVariantCookedInfoProvider
{
    int GetCookedCountForVariant(Guid variantGuid);
    IReadOnlyDictionary<Guid, int> GetCookedCountsForVariants(IReadOnlyCollection<Guid> variantGuids);
    int GetRecipeCookedCount(Guid recipeGuid);
    bool HasMemberCooked(Guid variantGuid, Guid memberGuid);
    void MarkCooked(Guid variantGuid, Guid recipeGuid, Guid memberGuid);
    bool UnmarkCooked(Guid variantGuid, Guid memberGuid);
}
