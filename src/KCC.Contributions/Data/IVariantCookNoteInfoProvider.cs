namespace KCC.Contributions.Data;

public partial interface IVariantCookNoteInfoProvider
{
    IReadOnlyList<VariantCookNoteInfo> GetForVariant(Guid variantGuid, int page, int pageSize, out int totalCount);
    IReadOnlyDictionary<Guid, int> GetNoteCountsForVariants(IReadOnlyCollection<Guid> variantGuids);
    int Add(Guid variantGuid, Guid recipeGuid, Guid memberGuid, string noteText);
    bool DeleteOwn(int noteId, Guid memberGuid);
}
