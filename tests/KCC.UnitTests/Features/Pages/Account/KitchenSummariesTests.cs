using KCC.Web.Features.Pages.Account;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Sync;

namespace KCC.UnitTests.Features.Pages.Account;

public class KitchenSummariesTests
{
    private static readonly Guid Ada = Guid.NewGuid();
    private static readonly Guid Grace = Guid.NewGuid();

    [Test]
    public async Task From_CountsStartedRecipes_EveryVariant_AndWhatWaitsForReview()
    {
        var started = Recipe(startedByMe: true);
        var draft = Recipe(startedByMe: true);
        var someoneElses = Recipe(startedByMe: false);
        var published = Variant(someoneElses);
        var pending = Variant(someoneElses);
        var firstTry = Variant(draft);
        var authored = new AuthoredRecipes(
            [started, draft, someoneElses],
            [published, pending, firstTry],
            new HashSet<Guid> { started.Key },
            new HashSet<Guid> { published.Key });

        _ = await Assert.That(KitchenSummary.From(authored)).IsEqualTo(new KitchenSummary(2, 3, 3));
    }

    [Test]
    public async Task For_ReadsEachMembersKitchenOnce()
    {
        var reads = new CountingReads();
        var summaries = Summaries(reads);

        summaries.For(Ada);
        summaries.For(Ada);
        summaries.For(Grace);

        _ = await Assert.That(reads.Count).IsEqualTo(2);
    }

    [Test]
    public async Task For_ReadsAgain_OnceAContentChangeClearsTheCache()
    {
        var reads = new CountingReads();
        var summaries = Summaries(reads);
        summaries.For(Ada);
        summaries.For(Grace);

        new KitchenSummaryCacheRefresher(summaries).Handle(new ContentCacheRefresherNotification(new object(), MessageType.RefreshAll));
        summaries.For(Ada);
        summaries.For(Grace);

        _ = await Assert.That(reads.Count).IsEqualTo(4);
    }

    [Test]
    public async Task For_KeepsAReadThatAChangeOvertook_OutOfTheCache()
    {
        KitchenSummaries summaries = null;
        var reads = new CountingReads { DuringFirstRead = () => summaries.Clear() };
        summaries = Summaries(reads);

        summaries.For(Ada);
        summaries.For(Ada);
        summaries.For(Ada);

        _ = await Assert.That(reads.Count).IsEqualTo(2);
    }

    [Test]
    public async Task For_AFailedRead_GivesNoSummary_AndIsTriedAgain()
    {
        var reads = new CountingReads { Fails = true };
        var summaries = Summaries(reads);

        var first = summaries.For(Ada);
        summaries.For(Ada);

        _ = await Assert.That(first).IsNull();
        _ = await Assert.That(reads.Count).IsEqualTo(2);
    }

    private static KitchenSummaries Summaries(CountingReads reads) => new(
        new MemoryCache(new MemoryCacheOptions()),
        new ServiceCollection().AddSingleton<IAuthoredRecipeQueries>(reads).BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
        NullLogger<KitchenSummaries>.Instance);

    private static AccountViewModel.AuthoredRecipeInput Recipe(bool startedByMe) =>
        new(Guid.NewGuid(), "Recipe", "fa-duotone fa-egg", "/recipes/recipe/", startedByMe);

    private static AccountViewModel.AuthoredVariantInput Variant(AccountViewModel.AuthoredRecipeInput recipe) =>
        new(Guid.NewGuid(), recipe.Key, "Variant", "fa-duotone fa-egg", "/recipes/recipe/variant/");

    private sealed class CountingReads : IAuthoredRecipeQueries
    {
        public int Count { get; private set; }

        public bool Fails { get; init; }

        public Action DuringFirstRead { get; init; }

        public AuthoredRecipes GetAuthoredBy(Guid memberKey)
        {
            Count++;
            if (Count == 1)
            {
                DuringFirstRead?.Invoke();
            }

            if (Fails)
            {
                throw new InvalidOperationException("The database is unavailable.");
            }

            return AuthoredRecipes.None;
        }
    }
}
