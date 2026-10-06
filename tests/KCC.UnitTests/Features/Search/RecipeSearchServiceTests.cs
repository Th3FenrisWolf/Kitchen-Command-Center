using KCC.Web.Features.Search;

namespace KCC.UnitTests.Features.Search;

public class RecipeSearchServiceTests
{
    [Test]
    public async Task Search_NeedsEveryTermSomewhereInTheNameOrContent()
    {
        var search = Service(
            Doc("Chickpea Curry", description: "Weeknight coconut dinner"),
            Doc("Coconut Rice", description: "A side dish"));

        var results = search.Search(new RecipeSearchCriteria { Query = "coconut weeknight" });

        _ = await Assert.That(Names(results)).IsEqualTo("Chickpea Curry");
    }

    [Test]
    public async Task Search_RanksANameMatchAboveAContentMatch()
    {
        var search = Service(
            Doc("Apple Soup", ingredients: ["Lemon"]),
            Doc("Zesty Lemon Bars"));

        var results = search.Search(new RecipeSearchCriteria { Query = "lemon" });

        _ = await Assert.That(string.Join(",", results.Results.Select(hit => hit.Name))).IsEqualTo("Zesty Lemon Bars,Apple Soup");
    }

    [Test]
    public async Task Search_ReadsOnlyPunctuationAsEverything()
    {
        var search = Service(Doc("Apple Pie"), Doc("Banana Bread"));

        _ = await Assert.That(search.Search(new RecipeSearchCriteria { Query = "?!" }).Total).IsEqualTo(2);
    }

    [Test]
    public async Task Query_RepeatingAWord_StillFindsIt()
    {
        var search = Service(Doc("Chili", description: "smoky chili"));

        var results = search.Search(new RecipeSearchCriteria { Query = string.Join(' ', Enumerable.Repeat("chili", 2000)) });

        _ = await Assert.That(results.Total).IsEqualTo(1);
    }

    [Test]
    public async Task Query_OfMoreWordsThanLuceneAllows_FindsNothingRatherThanThrowing()
    {
        var search = Service(Doc("Chili", description: "smoky chili"));

        var results = search.Search(new RecipeSearchCriteria { Query = string.Join(' ', Enumerable.Range(0, 1100).Select(i => $"word{i}")) });

        _ = await Assert.That(results.Total).IsEqualTo(0);
    }

    [Test]
    public async Task Facets_KeepTheirOwnDimensionWideWhileNarrowingTheOthers()
    {
        var search = Service(
            Doc("Chili", category: "Dinner", diets: ["Spicy"]),
            Doc("Stew", category: "Dinner", diets: ["Hearty"]),
            Doc("Salsa", category: "Snack", diets: ["Spicy"]));

        var results = search.Search(new RecipeSearchCriteria { Categories = ["Dinner"] });

        _ = await Assert.That(results.Total).IsEqualTo(2);
        _ = await Assert.That(results.Facets.Category["Snack"]).IsEqualTo(1);
        _ = await Assert.That(results.Facets.Category["Dinner"]).IsEqualTo(2);
        _ = await Assert.That(results.Facets.Diet["Spicy"]).IsEqualTo(1);
        _ = await Assert.That(results.Facets.Diet["Hearty"]).IsEqualTo(1);
    }

    [Test]
    public async Task Facets_OrValuesWithinADimension()
    {
        var search = Service(Doc("Chili", category: "Dinner"), Doc("Salsa", category: "Snack"), Doc("Tea", category: "Beverage"));

        var results = search.Search(new RecipeSearchCriteria { Categories = ["Dinner", "Snack"] });

        _ = await Assert.That(Names(results)).IsEqualTo("Chili,Salsa");
    }

    [Test]
    public async Task StyleFilter_KeepsRecipesWithAnyChosenStyle()
    {
        var search = Service(Doc("Chili", styles: ["Spicy"]), Doc("Stew"), Doc("Salsa", styles: ["Spicy", "Easy"]), Doc("Toast", styles: ["Easy"]));

        var results = search.Search(new RecipeSearchCriteria { Styles = ["Spicy", "Cheesy"] });

        _ = await Assert.That(Names(results)).IsEqualTo("Chili,Salsa");
    }

    [Test]
    public async Task StyleFacet_CountsEachStyle_AndStaysWideWhileItFilters()
    {
        var search = Service(Doc("Chili", styles: ["Spicy"]), Doc("Stew", styles: ["Easy"]), Doc("Salsa", styles: ["Spicy", "Easy"]));

        var results = search.Search(new RecipeSearchCriteria { Styles = ["Spicy"] });

        _ = await Assert.That(results.Total).IsEqualTo(2);
        _ = await Assert.That(results.Facets.Style["Spicy"]).IsEqualTo(2);
        _ = await Assert.That(results.Facets.Style["Easy"]).IsEqualTo(2);
    }

    [Test]
    public async Task TimeFilter_KeepsRecipesInsideTheRange()
    {
        var search = Service(Doc("Toast", fastest: 5), Doc("Soup", fastest: 30), Doc("Roast", fastest: 90));

        var results = search.Search(new RecipeSearchCriteria { TimeMin = 0, TimeMax = 30 });

        _ = await Assert.That(Names(results)).IsEqualTo("Soup,Toast");
    }

    [Test]
    [Arguments("rated", "Bread,Apple,Cake")]
    [Arguments("variants", "Cake,Apple,Bread")]
    [Arguments("recent", "Apple,Cake,Bread")]
    [Arguments("relevant", "Apple,Bread,Cake")]
    public async Task Sort_OrdersByTheChosenKey(string sort, string expected)
    {
        var search = Service(
            Doc("Bread", rating: 5, reviews: 1, variants: 1, published: 100),
            Doc("Apple", rating: 4, reviews: 1, variants: 2, published: 300),
            Doc("Cake", rating: 3, reviews: 1, variants: 3, published: 200));

        var results = search.Search(new RecipeSearchCriteria { Sort = sort });

        _ = await Assert.That(string.Join(",", results.Results.Select(hit => hit.Name))).IsEqualTo(expected);
    }

    [Test]
    public async Task Paging_WalksEveryMatchOnceAndSpotlightsOnlyThePageZero()
    {
        var search = Service(Enumerable.Range(1, 5).Select(i => Doc($"Recipe {i:00}", rating: i, reviews: 1)).ToArray());

        var first = search.Search(new RecipeSearchCriteria { PageSize = 2 });
        var second = search.Search(new RecipeSearchCriteria { PageSize = 2, Page = 1 });
        var third = search.Search(new RecipeSearchCriteria { PageSize = 2, Page = 2 });
        var beyond = search.Search(new RecipeSearchCriteria { PageSize = 2, Page = 3 });

        _ = await Assert.That($"{Names(first)}|{Names(second)}|{Names(third)}|{Names(beyond)}")
            .IsEqualTo("Recipe 01,Recipe 02|Recipe 03,Recipe 04|Recipe 05|");
        _ = await Assert.That(first.Spotlight.Name).IsEqualTo("Recipe 05");
        _ = await Assert.That(second.Spotlight).IsNull();
        _ = await Assert.That(first.Total).IsEqualTo(5);
    }

    [Test]
    public async Task Spotlight_NeedsARating()
    {
        var search = Service(Doc("Toast"), Doc("Soup"));

        _ = await Assert.That(search.Search(new RecipeSearchCriteria()).Spotlight).IsNull();
    }

    [Test]
    public async Task Hit_CarriesTheCardFields()
    {
        var search = Service(Doc("Chili", category: "Dinner", tags: ["Spicy", "Vegan"], fastest: 25, rating: 4.5, reviews: 2, variants: 3));

        var hit = search.Search(new RecipeSearchCriteria()).Results.Single();

        _ = await Assert.That(hit.Slug).IsEqualTo("/recipes/chili/");
        _ = await Assert.That(hit.Icon).IsEqualTo("fa-duotone fa-pot-food");
        _ = await Assert.That(hit.Category).IsEqualTo("Dinner");
        _ = await Assert.That(hit.StartedBy).IsEqualTo("Priya Balan");
        _ = await Assert.That(string.Join(",", hit.Tags)).IsEqualTo("Spicy,Vegan");
        _ = await Assert.That(hit.FastestTime).IsEqualTo(25);
        _ = await Assert.That(hit.AverageRating).IsEqualTo(4.5d);
        _ = await Assert.That(hit.ReviewCount).IsEqualTo(2);
        _ = await Assert.That(hit.VariantCount).IsEqualTo(3);
    }

    [Test]
    public async Task Hit_ListsEveryTag_WhileEachFacetCountsOnlyItsOwnKind()
    {
        var search = Service(Doc("Chili", tags: ["Vegan", "Spicy"], diets: ["Vegan"], styles: ["Spicy"]));

        var results = search.Search(new RecipeSearchCriteria());

        _ = await Assert.That(string.Join(",", results.Results.Single().Tags)).IsEqualTo("Vegan,Spicy");
        _ = await Assert.That(string.Join(",", results.Facets.Diet.Keys)).IsEqualTo("Vegan");
        _ = await Assert.That(string.Join(",", results.Facets.Style.Keys)).IsEqualTo("Spicy");
    }

    [Test]
    public async Task Hit_WithoutReviewsHasNoRating()
    {
        var search = Service(Doc("Chili"));

        _ = await Assert.That(search.Search(new RecipeSearchCriteria()).Results.Single().AverageRating).IsNull();
    }

    [Test]
    public async Task EmptyIndex_FindsNothing()
    {
        var results = new RecipeSearchService(new RecipeIndex()).Search(new RecipeSearchCriteria());

        _ = await Assert.That(results.Total).IsEqualTo(0);
        _ = await Assert.That(results.Facets.Category.Count).IsEqualTo(0);
    }

    private static RecipeSearchService Service(params RecipeSearchDocument[] documents)
    {
        var index = new RecipeIndex();
        index.Replace(RecipeIndexBuilder.Build(documents));
        return new RecipeSearchService(index);
    }

    private static string Names(RecipeSearchResults results) => string.Join(",", results.Results.Select(hit => hit.Name).Order());

    private static RecipeSearchDocument Doc(
        string name,
        string description = "",
        string category = "Dinner",
        string[] tags = null,
        string[] diets = null,
        string[] styles = null,
        string[] ingredients = null,
        int fastest = 20,
        double rating = 0,
        int reviews = 0,
        int variants = 1,
        long published = 1_789_862_400L) => new()
    {
        Name = name,
        Slug = $"/recipes/{name.ToLowerInvariant().Replace(' ', '-')}/",
        Icon = "fa-duotone fa-pot-food",
        Category = category,
        StartedBy = "Priya Balan",
        Description = description,
        Tags = tags ?? [],
        Diets = diets ?? [],
        Styles = styles ?? [],
        IngredientNames = ingredients ?? [],
        FastestTime = fastest,
        VariantCount = variants,
        AverageRating = rating,
        ReviewCount = reviews,
        PublishedUnixSeconds = published,
    };
}
