using System.Collections.Concurrent;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.Search;
using Microsoft.Extensions.DependencyInjection;

namespace KCC.IntegrationTests.Features.Search;

public class RecipeIndexSwapTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task SearchesDuringRebuilds_AlwaysSeeAWholeIndex()
    {
        var rebuilder = Site.Services.GetRequiredService<IRecipeIndexRebuilder>();
        var search = Site.Services.GetRequiredService<IRecipeSearchService>();
        var totals = new ConcurrentDictionary<int, int>();
        using var stop = new CancellationTokenSource();

        var searchers = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            while (!stop.IsCancellationRequested)
            {
                var total = search.Search(new RecipeSearchCriteria { Categories = ["Dinner"] }).Total;
                totals.AddOrUpdate(total, 1, (_, count) => count + 1);
            }
        })).ToList();

        try
        {
            for (var rebuild = 0; rebuild < 10; rebuild++)
            {
                rebuilder.Signal();
                await rebuilder.WhenCurrentAsync(CancellationToken.None);
            }
        }
        finally
        {
            await stop.CancelAsync();
        }

        await Task.WhenAll(searchers);

        _ = await Assert.That(string.Join(",", totals.Keys)).IsEqualTo("5");
        _ = await Assert.That(totals[5]).IsGreaterThan(10);
    }
}
