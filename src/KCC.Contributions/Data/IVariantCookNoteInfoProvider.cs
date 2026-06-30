namespace KCC.Contributions.Data;

public partial interface IVariantCookNoteInfoProvider
{
    IReadOnlyList<VariantCookNoteInfo> GetForVariant(Guid variantGuid, int page, int pageSize, out int totalCount);
    int Add(Guid variantGuid, Guid recipeGuid, Guid memberGuid, string noteText);
    bool DeleteOwn(int noteId, Guid memberGuid);
}
