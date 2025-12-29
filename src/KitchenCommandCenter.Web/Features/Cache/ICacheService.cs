using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CMS.ContentEngine;
using KitchenCommandCenter.Web.Models.Constants;

namespace KitchenCommandCenter.Web.Features.Cache;

/// <summary>
/// The cache service that wraps the Kentico Xperience cache implementation.
/// </summary>
public interface ICacheService
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
    Task<IEnumerable<T>> Get<T>(
        ContentItemQueryBuilder query,
        string[] cacheItemNameParts,
        Func<IEnumerable<T>, ICollection<string>> dependencyCacheKeys = null,
        int cacheMinutes = CacheConstants.CacheMinutes,
        ContentQueryExecutionOptions queryOptions = null
    )
        where T : IContentItemFieldsSource;
}
