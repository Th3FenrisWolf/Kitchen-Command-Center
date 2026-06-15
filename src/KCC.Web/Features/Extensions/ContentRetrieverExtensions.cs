using CMS.Websites;
using Kentico.Content.Web.Mvc;

namespace KCC.Web.Features.Extensions;

public static class ContentRetrieverExtensions
{
    public static async Task<T> RetrievePage<T>(
        this IContentRetriever contentRetriever,
        int pageId,
        int linkedItemsMaxLevel = 0
    ) where T : IWebPageFieldsSource, new()
    {
        var result = (await contentRetriever.RetrievePages<T>(
            new() { LinkedItemsMaxLevel = linkedItemsMaxLevel },
            query => query
                .Where(w => w.WhereEquals(nameof(IWebPageFieldsSource.SystemFields.WebPageItemID), pageId))
                .TopN(1),
            new($"{nameof(ContentRetrieverExtensions)}|{nameof(RetrievePage)}|{typeof(T).Name}|{pageId}|{linkedItemsMaxLevel}")
        )).FirstOrDefault();

        return result;
    }

    public static async Task<T> RetrieveFirstPage<T>(
        this IContentRetriever contentRetriever,
        int linkedItemsMaxLevel = 0
    ) where T : IWebPageFieldsSource, new()
    {
        var result = (await contentRetriever.RetrievePages<T>(
            new() { LinkedItemsMaxLevel = linkedItemsMaxLevel },
            query => query.TopN(1),
            new($"{nameof(ContentRetrieverExtensions)}|{nameof(RetrieveFirstPage)}|{typeof(T).Name}|{linkedItemsMaxLevel}")
        )).FirstOrDefault();

        return result;
    }
}
