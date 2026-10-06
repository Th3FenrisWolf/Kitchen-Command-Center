using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace KCC.Web.Features.Pages.Account;

public sealed class KitchenSummaries(IMemoryCache cache, IServiceScopeFactory scopeFactory, ILogger<KitchenSummaries> logger)
    : IDisposable
{
    private CancellationTokenSource generation = new();

    public KitchenSummary For(Guid memberKey)
    {
        // Taken before the read starts: a change that lands during the read cancels this token, so what the read saw is
        // never cached as current.
        var token = generation.Token;
        try
        {
            return cache.GetOrCreate($"kcc:kitchen-summary:{memberKey}", entry =>
            {
                entry.AddExpirationToken(new CancellationChangeToken(token));
                using var scope = scopeFactory.CreateScope();
                return KitchenSummary.From(scope.ServiceProvider.GetRequiredService<IAuthoredRecipeQueries>().GetAuthoredBy(memberKey));
            });
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Summarising the kitchen of member {MemberKey} failed", memberKey);
            return null;
        }
    }

    public void Clear() => Interlocked.Exchange(ref generation, new CancellationTokenSource()).Cancel();

    public void Dispose() => generation.Dispose();
}

public sealed record KitchenSummary(int Recipes, int Variants, int Waiting)
{
    public static KitchenSummary From(AuthoredRecipes authored) => new(
        authored.Recipes.Count(recipe => recipe.StartedByMe),
        authored.Variants.Count,
        authored.Recipes.Count(recipe => recipe.StartedByMe && !authored.PublishedRecipeKeys.Contains(recipe.Key))
            + authored.Variants.Count(variant => !authored.PublishedVariantKeys.Contains(variant.Key)));
}
