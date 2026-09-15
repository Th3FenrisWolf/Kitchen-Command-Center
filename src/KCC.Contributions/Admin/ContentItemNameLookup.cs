using CMS.ContentEngine;
using CMS.ContentEngine.Internal;
using CMS.DataEngine;
using CMS.Helpers;

namespace KCC.Contributions.Admin;

/// <summary>Resolves content-item GUIDs (recipes and variants) to display names, with a "(deleted)" fallback.</summary>
public class ContentItemNameLookup(
    IInfoProvider<ContentItemInfo> contentItemProvider,
    IInfoProvider<ContentItemLanguageMetadataInfo> metadataProvider,
    IInfoProvider<ContentLanguageInfo> languageProvider,
    IProgressiveCache cache)
{
    internal const string DeletedFallback = "(deleted)";
    private const int CacheMinutes = 60;

    public IReadOnlyDictionary<Guid, string> DisplayNames() =>
        cache.Load(
            cs =>
            {
                cs.CacheDependency = CacheHelper.GetCacheDependency(
                [
                    $"{ContentItemInfo.OBJECT_TYPE}|all",
                    $"{ContentItemLanguageMetadataInfo.OBJECT_TYPE}|all",
                    $"{ContentLanguageInfo.OBJECT_TYPE}|all",
                ]);
                return BuildMap();
            },
            new(CacheMinutes, nameof(ContentItemNameLookup), nameof(DisplayNames)));

    private Dictionary<Guid, string> BuildMap()
    {
        var defaultLanguageId = languageProvider.Get()
            .WhereEquals(nameof(ContentLanguageInfo.ContentLanguageIsDefault), true)
            .TopN(1)
            .FirstOrDefault()?.ContentLanguageID ?? 0;

        var items = contentItemProvider.Get()
            .Columns(
                nameof(ContentItemInfo.ContentItemID),
                nameof(ContentItemInfo.ContentItemGUID),
                nameof(ContentItemInfo.ContentItemName))
            .ToArray()
            .Select(i => (i.ContentItemID, i.ContentItemGUID, i.ContentItemName));

        var metadata = metadataProvider.Get()
            .Columns(
                nameof(ContentItemLanguageMetadataInfo.ContentItemLanguageMetadataContentItemID),
                nameof(ContentItemLanguageMetadataInfo.ContentItemLanguageMetadataContentLanguageID),
                nameof(ContentItemLanguageMetadataInfo.ContentItemLanguageMetadataDisplayName))
            .ToArray()
            .Select(m => (
                m.ContentItemLanguageMetadataContentItemID,
                m.ContentItemLanguageMetadataContentLanguageID,
                m.ContentItemLanguageMetadataDisplayName));

        return Merge(items, metadata, defaultLanguageId);
    }

    /// <summary>Display name in the default language, else any language, else the codename.</summary>
    internal static Dictionary<Guid, string> Merge(
        IEnumerable<(int Id, Guid Guid, string CodeName)> items,
        IEnumerable<(int ItemId, int LanguageId, string DisplayName)> metadata,
        int defaultLanguageId)
    {
        var displayNameByItem = metadata
            .Where(m => !string.IsNullOrWhiteSpace(m.DisplayName))
            .GroupBy(m => m.ItemId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(m => m.LanguageId == defaultLanguageId ? 0 : 1)
                    .ThenBy(m => m.LanguageId)
                    .First().DisplayName);

        return items.ToDictionary(
            i => i.Guid,
            i => displayNameByItem.TryGetValue(i.Id, out var displayName) ? displayName : i.CodeName);
    }

    public static string DisplayOrDeleted(IReadOnlyDictionary<Guid, string> map, Guid contentItemGuid) =>
        map.TryGetValue(contentItemGuid, out var name) && !string.IsNullOrEmpty(name) ? name : DeletedFallback;
}
