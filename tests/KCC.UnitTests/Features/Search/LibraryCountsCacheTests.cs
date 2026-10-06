using KCC.Web.Features.Search;

namespace KCC.UnitTests.Features.Search;

public class LibraryCountsCacheTests
{
    [Test]
    public async Task Current_CountsTheLibrary_ItsMealsAndDiets_AndWhatIsReadyInHalfAnHour()
    {
        var library = Library(Doc("Chili", "Dinner", 25, "Vegan"), Doc("Stew", "Dinner", 90), Doc("Toast", "Breakfast", 5, "Vegan", "Vegetarian"));

        var counts = library.Cache.Current();

        _ = await Assert.That(counts.Total).IsEqualTo(3);
        _ = await Assert.That(counts.Categories["Dinner"]).IsEqualTo(2);
        _ = await Assert.That(counts.Categories["Breakfast"]).IsEqualTo(1);
        _ = await Assert.That(counts.Diets["Vegan"]).IsEqualTo(2);
        _ = await Assert.That(counts.Diets["Vegetarian"]).IsEqualTo(1);
        _ = await Assert.That(counts.UnderThirtyMinutes).IsEqualTo(2);
    }

    [Test]
    public async Task Current_SearchesOncePerIndexSnapshot()
    {
        var library = Library(Doc("Chili", "Dinner", 25));

        library.Cache.Current();
        library.Cache.Current();
        var searchesBeforeTheSwap = library.Search.Searches;
        library.Index.Replace(RecipeIndexBuilder.Build([Doc("Chili", "Dinner", 25), Doc("Toast", "Breakfast", 5)]));
        var afterTheSwap = library.Cache.Current();

        _ = await Assert.That(searchesBeforeTheSwap).IsEqualTo(2);
        _ = await Assert.That(afterTheSwap.Total).IsEqualTo(2);
        _ = await Assert.That(library.Search.Searches).IsEqualTo(4);
    }

    private static (RecipeIndex Index, LibraryCountsCache Cache, CountingSearch Search) Library(params RecipeSearchDocument[] documents)
    {
        var index = new RecipeIndex();
        index.Replace(RecipeIndexBuilder.Build(documents));
        var search = new CountingSearch(new RecipeSearchService(index));
        return (index, new LibraryCountsCache(index, search), search);
    }

    private static RecipeSearchDocument Doc(string name, string category, int fastest, params string[] diets) => new()
    {
        Name = name,
        Slug = $"/recipes/{name.ToLowerInvariant()}/",
        Category = category,
        Diets = diets,
        FastestTime = fastest,
    };

    private sealed class CountingSearch(IRecipeSearchService inner) : IRecipeSearchService
    {
        public int Searches { get; private set; }

        public RecipeSearchResults Search(RecipeSearchCriteria criteria)
        {
            Searches++;
            return inner.Search(criteria);
        }
    }
}
