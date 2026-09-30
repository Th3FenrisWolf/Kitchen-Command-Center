using KCC.Contributions.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace KCC.Contributions;

public interface IContributionStats
{
    Task<ContributionStats> GetAsync();

    void Invalidate();
}

public sealed class ContributionStatsSource(IEFCoreScopeProvider<ContributionsDbContext> scopes, IMemoryCache cache) : IContributionStats
{
    private const string CacheKey = "kcc:contribution-stats";

    private CancellationTokenSource generation = new();

    public async Task<ContributionStats> GetAsync()
    {
        // Taken before the load starts: a write that lands during the load cancels this token, so the
        // snapshot the load produces is never cached as current.
        var token = generation.Token;
        return await cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AddExpirationToken(new CancellationChangeToken(token));
            return await LoadAsync();
        });
    }

    public void Invalidate() => Interlocked.Exchange(ref generation, new CancellationTokenSource()).Cancel();

    private async Task<ContributionStats> LoadAsync()
    {
        using var scope = scopes.CreateScope();
        var stats = await scope.ExecuteWithContextAsync(async db =>
        {
            var reviews = await db.Reviews.AsNoTracking().Select(review => new { review.VariantKey, review.Rating }).ToListAsync();
            var cooked = await db.CookedMarks.AsNoTracking().Select(mark => mark.VariantKey).ToListAsync();
            return ContributionStats.Build(reviews.Select(review => (review.VariantKey, review.Rating)), cooked);
        });
        scope.Complete();
        return stats;
    }
}
