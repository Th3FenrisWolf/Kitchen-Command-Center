using KCC.IntegrationTests.Config;
using KCC.Web.Features.Search;
using Microsoft.Extensions.DependencyInjection;

namespace KCC.IntegrationTests.Features.Search;

// Other tests add recipes of their own, with unique names and never a seeded category or tag, and one of them is
// rated 5.0. So these checks against the seed (RecipeSeedData) filter by category, diet or a seeded word, and never
// count or spotlight the whole site.
public class RecipeSearchTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    [Arguments("tahini", "Weeknight Bowls")]
    [Arguments("midnight", "Cold Brew Concentrate")]
    [Arguments("Salazar", "Cold Brew Concentrate,Shakshuka")]
    public async Task Query_FindsAWordInAnIngredientDescriptionOrAuthor(string query, string expected)
    {
        var results = Search(new RecipeSearchCriteria { Query = query });

        _ = await Assert.That(Names(results)).IsEqualTo(expected);
    }

    [Test]
    public async Task Query_LeavesInstructionsOut()
    {
        _ = await Assert.That(Search(new RecipeSearchCriteria { Query = "zephyr" }).Total).IsEqualTo(0);
    }

    [Test]
    public async Task Facets_CountEverySeededCategoryAndDiet()
    {
        var results = Search(new RecipeSearchCriteria());

        _ = await Assert.That(Counts(results.Facets.Category, "Beverage", "Breakfast", "Dessert", "Dinner", "Lunch", "Snack"))
            .IsEqualTo("Beverage 4, Breakfast 4, Dessert 4, Dinner 5, Lunch 4, Snack 4");
        _ = await Assert.That(Counts(results.Facets.Diet, "Dairy-Free", "Gluten-Free", "High-Protein", "Keto", "Low-Carb", "Spicy", "Vegan", "Vegetarian"))
            .IsEqualTo("Dairy-Free 2, Gluten-Free 7, High-Protein 8, Keto 1, Low-Carb 3, Spicy 4, Vegan 12, Vegetarian 10");
        _ = await Assert.That(results.Facets.Diet.ContainsKey("Cheesy")).IsFalse();
    }

    [Test]
    public async Task ACategory_NarrowsTheDietsButKeepsTheOtherCategoriesCounted()
    {
        var results = Search(new RecipeSearchCriteria { Categories = ["Dinner"] });

        _ = await Assert.That(results.Total).IsEqualTo(5);
        _ = await Assert.That(Counts(results.Facets.Category, "Beverage", "Lunch")).IsEqualTo("Beverage 4, Lunch 4");
        _ = await Assert.That(Counts(results.Facets.Diet, "High-Protein", "Vegan", "Vegetarian")).IsEqualTo("High-Protein 4, Vegan 1, Vegetarian 1");
    }

    [Test]
    public async Task TimeRange_UsesEachRecipesFastestVariant()
    {
        var results = Search(new RecipeSearchCriteria { Categories = ["Lunch"], TimeMin = 0, TimeMax = 15 });

        _ = await Assert.That(Names(results)).IsEqualTo("Caprese Sandwich,Quinoa Power Salad,Weeknight Bowls");
    }

    [Test]
    [Arguments("relevant", "Hour-Glass Frittata,Legendary Lasagna,Sheet-Pan Salmon,Slow-Braised Short Ribs,Weeknight Tacos")]
    [Arguments("rated", "Legendary Lasagna,Slow-Braised Short Ribs,Sheet-Pan Salmon,Weeknight Tacos,Hour-Glass Frittata")]
    [Arguments("recent", "Legendary Lasagna,Slow-Braised Short Ribs,Weeknight Tacos,Sheet-Pan Salmon,Hour-Glass Frittata")]
    public async Task Sort_OrdersTheDinners(string sort, string expected)
    {
        var results = Search(new RecipeSearchCriteria { Categories = ["Dinner"], Sort = sort });

        _ = await Assert.That(string.Join(",", results.Results.Select(hit => hit.Name))).IsEqualTo(expected);
    }

    [Test]
    public async Task Sort_ByVariants_PutsTheBiggestFlightFirst()
    {
        var results = Search(new RecipeSearchCriteria { Categories = ["Lunch"], Sort = "variants" });

        _ = await Assert.That(results.Results[0].Name).IsEqualTo("Spicy Ramen Flight");
        _ = await Assert.That(results.Results[1].Name).IsEqualTo("Weeknight Bowls");
    }

    [Test]
    public async Task Spotlight_IsTheTopRatedMatch()
    {
        _ = await Assert.That(Search(new RecipeSearchCriteria { Categories = ["Dinner"] }).Spotlight?.Name).IsEqualTo("Legendary Lasagna");

        var beverages = Search(new RecipeSearchCriteria { Categories = ["Beverage"] });
        _ = await Assert.That(beverages.Total).IsEqualTo(4);
        _ = await Assert.That(beverages.Spotlight).IsNull();
    }

    [Test]
    public async Task Paging_WalksEveryMatchOnce()
    {
        var pages = Enumerable.Range(0, 4)
            .Select(page => Search(new RecipeSearchCriteria { Diets = ["Vegan"], PageSize = 5, Page = page }))
            .ToList();

        _ = await Assert.That(string.Join(",", pages.Select(page => page.Results.Count))).IsEqualTo("5,5,2,0");
        _ = await Assert.That(pages.SelectMany(page => page.Results).Select(hit => hit.Slug).Distinct().Count()).IsEqualTo(12);
        _ = await Assert.That(pages.Skip(1).All(page => page.Spotlight is null)).IsTrue();
    }

    [Test]
    public async Task Hit_CarriesWhatTheCardShows()
    {
        var hit = Search(new RecipeSearchCriteria { Query = "ramen" }).Results.Single();

        _ = await Assert.That(hit.Name).IsEqualTo("Spicy Ramen Flight");
        _ = await Assert.That(hit.Slug).IsEqualTo("/recipes/spicy-ramen-flight/");
        _ = await Assert.That(hit.Icon).IsEqualTo("fa-duotone fa-bowl-chopsticks");
        _ = await Assert.That(hit.Category).IsEqualTo("Lunch");
        _ = await Assert.That(hit.StartedBy).IsEqualTo("Priya Balan");
        _ = await Assert.That(string.Join(",", hit.Tags)).IsEqualTo("Spicy,Vegan,High-Protein,Dairy-Free");
        _ = await Assert.That(hit.AverageRating).IsEqualTo(4.5d);
        _ = await Assert.That(hit.ReviewCount).IsEqualTo(3);
        _ = await Assert.That(hit.VariantCount).IsEqualTo(4);
        _ = await Assert.That(hit.FastestTime).IsEqualTo(20);
    }

    [Test]
    public async Task Hit_WithNoVariantsOrReviews_IsStillListed()
    {
        var hit = Search(new RecipeSearchCriteria { Query = "cupboard" }).Results.Single();

        _ = await Assert.That(hit.Name).IsEqualTo("Bare Cupboard Snack Board");
        _ = await Assert.That(hit.VariantCount).IsEqualTo(0);
        _ = await Assert.That(hit.FastestTime).IsEqualTo(0);
        _ = await Assert.That(hit.AverageRating).IsNull();
    }

    private static string Names(RecipeSearchResults results) => string.Join(",", results.Results.Select(hit => hit.Name).Order());

    private static string Counts(IReadOnlyDictionary<string, int> facets, params string[] labels) =>
        string.Join(", ", labels.Select(label => $"{label} {facets.GetValueOrDefault(label)}"));

    private RecipeSearchResults Search(RecipeSearchCriteria criteria) =>
        Site.Services.GetRequiredService<IRecipeSearchService>().Search(criteria);
}
