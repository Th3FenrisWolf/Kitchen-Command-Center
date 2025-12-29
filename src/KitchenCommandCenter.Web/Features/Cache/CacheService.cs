using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CMS.ContentEngine;
using CMS.Helpers;
using CMS.Websites;
using CMS.Websites.Routing;
using CacheConstants = KitchenCommandCenter.Web.Models.Constants.CacheConstants;

namespace KitchenCommandCenter.Web.Features.Cache;

/// <summary>
/// The cache service that wraps the Kentico Xperience cache implementation.
/// </summary>
/// <param name="linkedItemsDependencyAsyncRetriever">Kentico Xperience ILinkedItemsDependencyAsyncRetriever</param>
/// <param name="progressiveCache">Kentico Xperience IProgressiveCache</param>
/// <param name="websiteChannelContext">Kentico Xperience Website Channel Context</param>
/// <param name="executor">Kentico Xperience content query executor</param>
public class CacheService(
    ILinkedItemsDependencyAsyncRetriever linkedItemsDependencyAsyncRetriever,
    IProgressiveCache progressiveCache,
    IWebsiteChannelContext websiteChannelContext,
    IContentQueryExecutor executor
) : ICacheService
{
    /// <summary>
    /// Gets a collection from the cache.
    /// </summary>
    /// <typeparam name="T">Kentico Xperience Page Type</typeparam>
    /// <param name="query">Query to be executed</param>
    /// <param name="cacheItemNameParts">Values that make up the cache key</param>
    /// <param name="dependencyCacheKeys">List of cache keys to be registered for the retrieved items</param>
    /// <param name="cacheMinutes">Cache expiration (in minutes)</param>
    /// <param name="queryOptions">Query options for changing the result set returned from the query</param>
    /// <returns>Collection of results returned for the provided <paramref name="query"/></returns>
    public async Task<IEnumerable<T>> Get<T>(
        ContentItemQueryBuilder query,
        string[] cacheItemNameParts,
        Func<IEnumerable<T>, ICollection<string>> dependencyCacheKeys = null,
        int cacheMinutes = CacheConstants.CacheMinutes,
        ContentQueryExecutionOptions queryOptions = null
    )
        where T : IContentItemFieldsSource
    {
        return await progressiveCache.LoadAsync(
            async (cacheSettings) =>
            {
                var results =
                    typeof(T) is IWebPageFieldsSource
                        ? await executor.GetMappedWebPageResult<T>(
                            builder: query,
                            options: queryOptions ?? GetDefaultQueryOptions()
                        )
                        : await executor.GetMappedResult<T>(
                            builder: query,
                            options: queryOptions ?? GetDefaultQueryOptions()
                        );

                if (results == null)
                {
                    return default;
                }

                cacheSettings.Cached = !websiteChannelContext.IsPreview;
                cacheSettings.CacheDependency = CacheHelper.GetCacheDependency(
                    dependencyCacheKeys?.Invoke(results) ?? await GetDependencyCacheKeys(results)
                );

                return results;
            },
            new CacheSettings(
                cacheMinutes: cacheMinutes,
                useSlidingExpiration: true,
                cacheItemNameParts: GetCacheItemNameParts(cacheItemNameParts)
            )
        );
    }

    private string[] GetCacheItemNameParts(string[] cacheItemNameParts) =>
        [websiteChannelContext.WebsiteChannelName, .. cacheItemNameParts];

    private void AddPageDependencyCacheKeys<T>(HashSet<string> dependencyCacheKeys, T item)
        where T : IContentItemFieldsSource
    {
        // Builds a cache key "contentitem|byid|<itemId>" for each item
        dependencyCacheKeys.Add(
            CacheHelper.BuildCacheItemName(
                new[]
                {
                    "contentitem",
                    "byid",
                    item.SystemFields.ContentItemID.ToString(CultureInfo.InvariantCulture),
                },
                lowerCase: false
            )
        );

        if (item is IWebPageFieldsSource page)
        {
            // Builds a cache key "webpageitem|bychannel|MyWebsiteChannel|bypath|<pagePath>" for each item
            dependencyCacheKeys.Add(
                CacheHelper.BuildCacheItemName(
                    new[]
                    {
                        "webpageitem",
                        "bychannel",
                        websiteChannelContext.WebsiteChannelName,
                        "bypath",
                        page.SystemFields.WebPageItemTreePath,
                    },
                    lowerCase: false
                )
            );
        }
    }

    private async Task<ICollection<string>> GetDependencyCacheKeys<T>(IEnumerable<T> items)
        where T : IContentItemFieldsSource
    {
        var dependencyCacheKeys = (
            await linkedItemsDependencyAsyncRetriever.Get(
                items.Select(page => page.SystemFields.ContentItemID),
                maxLevel: 1
            )
        ).ToHashSet(StringComparer.InvariantCultureIgnoreCase);

        // Adds cache dependencies on each page in the collection
        foreach (var item in items)
        {
            AddPageDependencyCacheKeys(dependencyCacheKeys, item);
        }

        return dependencyCacheKeys;
    }

    private ContentQueryExecutionOptions GetDefaultQueryOptions() =>
        new() { ForPreview = websiteChannelContext.IsPreview };
}
