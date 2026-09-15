using CMS.Core;
using CMS.Helpers;

namespace KCC.Contributions.Data;

public partial class VariantCookNoteInfoProvider
{
    private const int CacheMinutes = 60;
    private const int MaxNoteLength = 4000;

    internal static string ClampText(string text) => string.IsNullOrWhiteSpace(text)
        ? null
        : text.Trim().Length <= MaxNoteLength
            ? text.Trim()
            : text.Trim()[..MaxNoteLength];

    internal static bool CanModify(VariantCookNoteInfo note, Guid memberGuid) =>
        note is not null && note.MemberGuid == memberGuid;

    /// <summary>Per-variant note count.</summary>
    internal static IReadOnlyDictionary<Guid, int> CountByVariant(IEnumerable<Guid> variantGuids) =>
        variantGuids.GroupBy(g => g).ToDictionary(g => g.Key, g => g.Count());

    private static string[] CacheKeys => [$"{VariantCookNoteInfo.OBJECT_TYPE}|all"];

    public IReadOnlyDictionary<Guid, int> GetNoteCountsForVariants(IReadOnlyCollection<Guid> variantGuids)
    {
        if (variantGuids.Count is 0)
        {
            return new Dictionary<Guid, int>();
        }

        var cache = Service.Resolve<IProgressiveCache>();
        return cache.Load(
            cs =>
            {
                cs.CacheDependency = CacheHelper.GetCacheDependency(CacheKeys);
                var rows = Get().WhereIn(nameof(VariantCookNoteInfo.VariantGuid), variantGuids);
                return CountByVariant(rows.Select(r => r.VariantGuid));
            },
            new(CacheMinutes, VariantCookNoteInfo.OBJECT_TYPE, nameof(GetNoteCountsForVariants), string.Join("|", variantGuids)));
    }

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
