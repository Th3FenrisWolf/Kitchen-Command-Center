using KCC.Web.Features.Search;

namespace KCC.UnitTests.Features.Search;

public class RecipeIndexTests
{
    [Test]
    public async Task Replace_ServesTheNewSnapshot()
    {
        using var index = new RecipeIndex();

        index.Replace(RecipeIndexBuilder.Build([new RecipeSearchDocument { Name = "Chili" }]));

        _ = await Assert.That(index.Search((searcher, _) => searcher.IndexReader.NumDocs)).IsEqualTo(1);
    }

    [Test]
    public async Task Replace_WaitsForASearchStillReadingTheOldSnapshot()
    {
        using var index = new RecipeIndex();
        index.Replace(RecipeIndexBuilder.Build([new RecipeSearchDocument { Name = "Chili" }]));
        using var searching = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();

        var search = Task.Run(() => index.Search((searcher, _) =>
        {
            searching.Set();
            release.Wait();
            return searcher.Doc(0).Get(RecipeSearchConstants.FieldName);
        }));
        searching.Wait();
        var replace = Task.Run(() => index.Replace(RecipeIndexBuilder.Build([])));
        await Task.Delay(100);
        var replacedEarly = replace.IsCompleted;
        release.Set();

        _ = await Assert.That(replacedEarly).IsFalse();
        _ = await Assert.That(await search).IsEqualTo("Chili");
        await replace;
        _ = await Assert.That(index.Search((searcher, _) => searcher.IndexReader.NumDocs)).IsEqualTo(0);
    }

    [Test]
    public async Task Replace_MovesTheVersionOn()
    {
        using var index = new RecipeIndex();
        var before = index.Version;

        index.Replace(RecipeIndexBuilder.Build([]));

        _ = await Assert.That(index.Version).IsNotEqualTo(before);
    }
}
