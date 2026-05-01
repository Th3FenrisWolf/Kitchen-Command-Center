using CMS;
using CMS.Core;
using KCC.Admin.UIPages.Home;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

[assembly: RegisterImplementation(typeof(HomeStatsService), typeof(HomeStatsService), Lifestyle = Lifestyle.Singleton)]

namespace KCC.Admin.UIPages.Home;

public sealed record RawStats(int Count, DateTimeOffset? LastModified);

public sealed record FeaturedStats(
    IReadOnlyList<TileStat> WebsiteChannel,
    IReadOnlyList<TileStat> ContentHub);

public interface IFeaturedAppStatsSource
{
    Task<RawStats> GetWebsiteChannelStatsAsync(CancellationToken cancellationToken);
    Task<RawStats> GetContentHubStatsAsync(CancellationToken cancellationToken);
}

public sealed class HomeStatsService(
    IFeaturedAppStatsSource source,
    IMemoryCache cache,
    ILogger<HomeStatsService> logger,
    Func<DateTimeOffset> now = null)
{
    private const string CacheKey = "kcc.admin.home.featuredStats";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private readonly Func<DateTimeOffset> now = now ?? (() => DateTimeOffset.UtcNow);

    public async Task<FeaturedStats> GetStatsAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(CacheKey, out FeaturedStats cached) && cached is not null)
        {
            return cached;
        }

        var websiteTask = SafeGet(() => source.GetWebsiteChannelStatsAsync(cancellationToken), "website-channel");
        var hubTask = SafeGet(() => source.GetContentHubStatsAsync(cancellationToken), "content-hub");

        await Task.WhenAll(websiteTask, hubTask);

        var stats = new FeaturedStats(
            WebsiteChannel: ToTileStats(websiteTask.Result, "pages"),
            ContentHub: ToTileStats(hubTask.Result, "items"));

        cache.Set(CacheKey, stats, CacheTtl);
        return stats;
    }

    private async Task<RawStats> SafeGet(Func<Task<RawStats>> fetch, string label)
    {
        try
        {
            return await fetch();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch home stats for {Label}", label);
            return null;
        }
    }

    private IReadOnlyList<TileStat> ToTileStats(RawStats raw, string countLabel)
    {
        if (raw is null)
        {
            return [];
        }

        var stats = new List<TileStat> { new(countLabel, raw.Count.ToString()) };

        if (raw.LastModified is { } lastModified)
        {
            stats.Add(new("last edited", FormatRelative(now() - lastModified)));
        }

        return stats;
    }

    private static string FormatRelative(TimeSpan span) => span switch
    {
        { TotalMinutes: < 1 } => "just now",
        { TotalMinutes: < 60 } => $"{(int)span.TotalMinutes}m",
        { TotalHours: < 24 } => $"{(int)span.TotalHours}h",
        { TotalDays: < 30 } => $"{(int)span.TotalDays}d",
        { TotalDays: < 365 } => $"{(int)(span.TotalDays / 30)}mo",
        _ => $"{(int)(span.TotalDays / 365)}y",
    };
}
