using CMS.ContentEngine.Internal;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.Websites.Internal;

namespace KCC.Contributions.Admin.WebPageTabs;

public sealed record ContributionPageIdentity(string ContentTypeName, Guid ContentItemGuid, int ParentWebPageItemId)
{
    public static readonly ContributionPageIdentity None = new(string.Empty, Guid.Empty, 0);

    public bool Is(string contentTypeName) =>
        string.Equals(ContentTypeName, contentTypeName, StringComparison.OrdinalIgnoreCase);
}

public sealed record ContributionVariantPage(int WebPageItemId, Guid ContentItemGuid);

public interface IContributionPageLookup
{
    ContributionPageIdentity Identify(int webPageItemId);

    IReadOnlyList<ContributionVariantPage> GetVariantPages(int recipeWebPageItemId);
}

/// <remarks>
/// <see cref="Identify"/> is on the hot path: <see cref="ContributionTabsExtender"/> calls it for every
/// web page opened in the channel, whether or not it is a recipe.
/// </remarks>
internal sealed class ContributionPageLookup(
    IInfoProvider<WebPageItemInfo> webPageItemProvider,
    IInfoProvider<ContentItemInfo> contentItemProvider,
    IProgressiveCache cache) : IContributionPageLookup
{
    private const int CacheMinutes = 60;

    private static readonly string[] StructureCacheKeys =
    [
        $"{WebPageItemInfo.OBJECT_TYPE}|all",
        $"{ContentItemInfo.OBJECT_TYPE}|all",
    ];

    public ContributionPageIdentity Identify(int webPageItemId) => cache.Load(
        cs =>
        {
            cs.CacheDependency = CacheHelper.GetCacheDependency(StructureCacheKeys);
            return Resolve(webPageItemId);
        },
        new(CacheMinutes, nameof(ContributionPageLookup), nameof(Identify), webPageItemId));

    public IReadOnlyList<ContributionVariantPage> GetVariantPages(int recipeWebPageItemId) => cache.Load(
        cs =>
        {
            cs.CacheDependency = CacheHelper.GetCacheDependency(StructureCacheKeys);
            return ResolveVariantPages(recipeWebPageItemId);
        },
        new(CacheMinutes, nameof(ContributionPageLookup), nameof(GetVariantPages), recipeWebPageItemId));

    private ContributionPageIdentity Resolve(int webPageItemId)
    {
        var webPageItem = webPageItemProvider.Get()
            .WhereEquals(nameof(WebPageItemInfo.WebPageItemID), webPageItemId)
            .Columns(
                nameof(WebPageItemInfo.WebPageItemContentItemID),
                nameof(WebPageItemInfo.WebPageItemParentID))
            .TopN(1)
            .FirstOrDefault();

        if (webPageItem is null)
        {
            return ContributionPageIdentity.None;
        }

        var contentItem = contentItemProvider.Get()
            .WhereEquals(nameof(ContentItemInfo.ContentItemID), webPageItem.WebPageItemContentItemID)
            .Columns(
                nameof(ContentItemInfo.ContentItemGUID),
                nameof(ContentItemInfo.ContentItemContentTypeID))
            .TopN(1)
            .FirstOrDefault();

        return contentItem is null
            ? ContributionPageIdentity.None
            : new ContributionPageIdentity(
                DataClassInfoProvider.GetClassName(contentItem.ContentItemContentTypeID) ?? string.Empty,
                contentItem.ContentItemGUID,
                webPageItem.WebPageItemParentID);
    }

    private IReadOnlyList<ContributionVariantPage> ResolveVariantPages(int recipeWebPageItemId)
    {
        var children = webPageItemProvider.Get()
            .WhereEquals(nameof(WebPageItemInfo.WebPageItemParentID), recipeWebPageItemId)
            .Columns(
                nameof(WebPageItemInfo.WebPageItemID),
                nameof(WebPageItemInfo.WebPageItemContentItemID),
                nameof(WebPageItemInfo.WebPageItemOrder))
            .OrderBy(nameof(WebPageItemInfo.WebPageItemOrder))
            .ToArray();

        if (children.Length == 0)
        {
            return [];
        }

        var variantContentTypeId = DataClassInfoProvider
            .GetDataClassInfo(ContributionContentTypes.RecipeVariant, false)?.ClassID ?? 0;

        var contentItems = contentItemProvider.Get()
            .WhereIn(nameof(ContentItemInfo.ContentItemID), children.Select(c => c.WebPageItemContentItemID).ToArray())
            .WhereEquals(nameof(ContentItemInfo.ContentItemContentTypeID), variantContentTypeId)
            .Columns(nameof(ContentItemInfo.ContentItemID), nameof(ContentItemInfo.ContentItemGUID))
            .ToArray()
            .ToDictionary(item => item.ContentItemID, item => item.ContentItemGUID);

        return
        [
            ..children
                .Where(child => contentItems.ContainsKey(child.WebPageItemContentItemID))
                .Select(child => new ContributionVariantPage(
                    child.WebPageItemID,
                    contentItems[child.WebPageItemContentItemID]))
        ];
    }
}
