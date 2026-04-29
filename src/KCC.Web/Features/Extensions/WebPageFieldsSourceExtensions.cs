using CMS.ContentEngine.Internal;
using CMS.Core;
using CMS.DataEngine;
using CMS.Websites;

namespace KCC.Web.Features.Extensions;

public static class PageTitleResolver
{
    public static async Task<string> GetTitle(this IWebPageFieldsSource page)
    {
        if (page is IListingMetadata { ListingHeading.Length: > 0 } listing)
        {
            return listing.ListingHeading;
        }

        if (page is IMetadata { MetadataTitle.Length: > 0 } metadata)
        {
            return metadata.MetadataTitle;
        }

        return await GetDisplayName(page);
    }

    public static async Task<string> GetMetadataTitle(this IWebPageFieldsSource page)
    {
        if (page is IMetadata { MetadataTitle.Length: > 0 } metadata)
        {
            return metadata.MetadataTitle;
        }

        return await GetDisplayName(page);
    }

    public static async Task<string> GetBreadcrumbTitle(this IWebPageFieldsSource page)
    {
        if (page is IMetadata metadata)
        {
            if (!string.IsNullOrEmpty(metadata.BreadcrumbLabel))
            {
                return metadata.BreadcrumbLabel;
            }

            if (!string.IsNullOrEmpty(metadata.MetadataTitle))
            {
                return metadata.MetadataTitle;
            }
        }

        return await GetDisplayName(page);
    }

    private static async Task<string> GetDisplayName(IWebPageFieldsSource page)
    {
        var metadataProvider = Service.Resolve<IInfoProvider<ContentItemLanguageMetadataInfo>>();

        var result = await metadataProvider.Get()
            .WhereEquals(
                nameof(ContentItemLanguageMetadataInfo.ContentItemLanguageMetadataContentItemID),
                page.SystemFields.ContentItemID)
            .WhereEquals(
                nameof(ContentItemLanguageMetadataInfo.ContentItemLanguageMetadataContentLanguageID),
                page.SystemFields.ContentItemCommonDataContentLanguageID)
            .TopN(1)
            .Column(nameof(ContentItemLanguageMetadataInfo.ContentItemLanguageMetadataDisplayName))
            .GetEnumerableTypedResultAsync();

        return result.FirstOrDefault()?.ContentItemLanguageMetadataDisplayName ?? string.Empty;
    }
}
