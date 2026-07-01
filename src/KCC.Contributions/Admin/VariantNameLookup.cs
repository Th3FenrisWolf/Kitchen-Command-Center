using CMS.ContentEngine.Internal;
using CMS.DataEngine;

namespace KCC.Contributions.Admin;

/// <summary>Resolves variant content-item GUIDs to names for admin listings, with a "(deleted)" fallback.</summary>
public class VariantNameLookup(IInfoProvider<ContentItemInfo> contentItemProvider)
{
    internal const string DeletedFallback = "(deleted)";

    /// <summary>Builds a GUID-to-name map for the given content-item GUIDs.</summary>
    public IReadOnlyDictionary<Guid, string> Resolve(IEnumerable<Guid> variantGuids)
    {
        var guids = variantGuids.Where(g => g != Guid.Empty).Distinct().ToArray();
        if (guids.Length == 0)
        {
            return new Dictionary<Guid, string>();
        }

        return contentItemProvider.Get()
            .WhereIn(nameof(ContentItemInfo.ContentItemGUID), guids)
            .ToArray()
            .ToDictionary(c => c.ContentItemGUID, c => c.ContentItemName);
    }

    /// <summary>Returns the resolved name or the "(deleted)" fallback.</summary>
    public static string DisplayOrDeleted(IReadOnlyDictionary<Guid, string> map, Guid variantGuid) =>
        map.TryGetValue(variantGuid, out var name) && !string.IsNullOrEmpty(name) ? name : DeletedFallback;
}
