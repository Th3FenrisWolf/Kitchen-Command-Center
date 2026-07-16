namespace KCC.Contributions.Data;

public partial class VariantCookNoteInfoProvider
{
    private const int MaxNoteLength = 4000;

    internal static string ClampText(string text) => string.IsNullOrWhiteSpace(text)
        ? null
        : text.Trim().Length <= MaxNoteLength
            ? text.Trim()
            : text.Trim()[..MaxNoteLength];

    internal static bool CanModify(VariantCookNoteInfo note, Guid memberGuid) =>
        note is not null && note.MemberGuid == memberGuid;

    public IReadOnlyList<VariantCookNoteInfo> GetForVariant(Guid variantGuid, int page, int pageSize, out int totalCount)
    {
        var query = Get().WhereEquals(nameof(VariantCookNoteInfo.VariantGuid), variantGuid);
        totalCount = query.Count;

        return [..query
            .OrderByDescending(nameof(VariantCookNoteInfo.NoteCreated))
            .Page(Math.Max(0, page), Math.Max(1, pageSize))];
    }

    public int Add(Guid variantGuid, Guid recipeGuid, Guid memberGuid, string noteText)
    {
        var now = DateTime.UtcNow;
        var note = new VariantCookNoteInfo
        {
            VariantGuid = variantGuid,
            RecipeGuid = recipeGuid,
            MemberGuid = memberGuid,
            NoteText = ClampText(noteText),
            NoteCreated = now,
            NoteModified = now,
        };
        Set(note);
        return note.VariantCookNoteID;
    }

    public bool DeleteOwn(int noteId, Guid memberGuid)
    {
        var note = Get().WhereEquals(nameof(VariantCookNoteInfo.VariantCookNoteID), noteId).TopN(1).FirstOrDefault();
        if (!CanModify(note, memberGuid))
        {
            return false;
        }

        Delete(note);
        return true;
    }
}
