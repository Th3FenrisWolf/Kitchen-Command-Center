using KCC.Web.Features.Pages.RecipeSearch;
using KCC.Web.Features.Recipes;
using KCC.Web.Features.Search;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;

namespace KCC.UnitTests.Features.Pages.RecipeSearch;

public class RecipeLibraryFiltersTests
{
    private static readonly RecipeTaxonomy Offered = new(["Breakfast", "Lunch", "Dinner"], ["Vegan", "Keto"], ["Spicy"]);

    [Test]
    public async Task Options_KeepTheTreeOrder_AndDropWhatNoRecipeUses()
    {
        var taxonomy = new RecipeTaxonomy(["Breakfast", "Lunch", "Dinner"], ["Vegetarian", "Vegan", "Keto"], ["Spicy", "Cheesy"]);
        var counts = new RecipeFacetCounts
        {
            Category = new Dictionary<string, int> { ["Dinner"] = 5, ["Breakfast"] = 4 },
            Diet = new Dictionary<string, int> { ["Vegan"] = 12, ["Vegetarian"] = 10 },
            Style = new Dictionary<string, int> { ["Spicy"] = 4 },
        };

        var options = RecipeLibraryFilters.Options(taxonomy, counts);

        _ = await Assert.That(string.Join(",", options.Categories)).IsEqualTo("Breakfast,Dinner");
        _ = await Assert.That(string.Join(",", options.Diets)).IsEqualTo("Vegetarian,Vegan");
        _ = await Assert.That(string.Join(",", options.Styles)).IsEqualTo("Spicy");
    }

    [Test]
    public async Task FromQuery_ReadsEveryFilter()
    {
        var criteria = Parse("?query=soup&category=Dinner&category=Lunch&diet=Vegan&style=Spicy&timeMin=10&timeMax=30&sort=rated");

        _ = await Assert.That(criteria.Query).IsEqualTo("soup");
        _ = await Assert.That(string.Join(",", criteria.Categories)).IsEqualTo("Dinner,Lunch");
        _ = await Assert.That(string.Join(",", criteria.Diets)).IsEqualTo("Vegan");
        _ = await Assert.That(string.Join(",", criteria.Styles)).IsEqualTo("Spicy");
        _ = await Assert.That(criteria.TimeMin).IsEqualTo(10);
        _ = await Assert.That(criteria.TimeMax).IsEqualTo(30);
        _ = await Assert.That(criteria.Sort).IsEqualTo("rated");
    }

    [Test]
    [Arguments("variants")]
    [Arguments("rated")]
    [Arguments("recent")]
    public async Task FromQuery_ReadsEachSort(string sort)
    {
        _ = await Assert.That(Parse($"?sort={sort}").Sort).IsEqualTo(sort);
    }

    [Test]
    public async Task FromQuery_ReadsTheUnderThirtyMinutesLink()
    {
        var criteria = Parse("?timeMax=30");

        _ = await Assert.That(criteria.TimeMin).IsEqualTo(0);
        _ = await Assert.That(criteria.TimeMax).IsEqualTo(30);
    }

    [Test]
    public async Task FromQuery_DecodesAQueryAsTheClientEncodesIt()
    {
        _ = await Assert.That(Parse("?query=mac+%26+cheese").Query).IsEqualTo("mac & cheese");
    }

    [Test]
    public async Task FromQuery_WithNothing_IsTheWholeLibrary()
    {
        var criteria = Parse(string.Empty);

        _ = await Assert.That(criteria.Query).IsEqualTo(string.Empty);
        _ = await Assert.That(criteria.Categories).IsEmpty();
        _ = await Assert.That(criteria.Diets).IsEmpty();
        _ = await Assert.That(criteria.Styles).IsEmpty();
        _ = await Assert.That(criteria.TimeMin).IsEqualTo(0);
        _ = await Assert.That(criteria.TimeMax).IsEqualTo(RecipeSearchCriteria.MaxTime);
        _ = await Assert.That(criteria.Sort).IsEqualTo("relevant");
        _ = await Assert.That(criteria.Page).IsEqualTo(0);
        _ = await Assert.That(criteria.PageSize).IsEqualTo(RecipeSearchCriteria.DefaultPageSize);
    }

    [Test]
    public async Task FromQuery_IgnoresWhatTheLibraryDoesNotOffer()
    {
        var criteria = Parse("?category=Brunch&diet=Spicy&style=Vegan&sort=spiciest&timeMin=soon&timeMax=");

        _ = await Assert.That(criteria.Categories).IsEmpty();
        _ = await Assert.That(criteria.Diets).IsEmpty();
        _ = await Assert.That(criteria.Styles).IsEmpty();
        _ = await Assert.That(criteria.Sort).IsEqualTo("relevant");
        _ = await Assert.That(criteria.TimeMin).IsEqualTo(0);
        _ = await Assert.That(criteria.TimeMax).IsEqualTo(RecipeSearchCriteria.MaxTime);
    }

    [Test]
    public async Task FromQuery_MatchesAnyCase_AndKeepsTheLibrarysSpelling()
    {
        var criteria = Parse("?category=dinner&diet=VEGAN&sort=Rated");

        _ = await Assert.That(string.Join(",", criteria.Categories)).IsEqualTo("Dinner");
        _ = await Assert.That(string.Join(",", criteria.Diets)).IsEqualTo("Vegan");
        _ = await Assert.That(criteria.Sort).IsEqualTo("rated");
    }

    [Test]
    public async Task FromQuery_TakesEachValueOnce()
    {
        var criteria = Parse("?category=Dinner&category=dinner&category=Lunch");

        _ = await Assert.That(string.Join(",", criteria.Categories)).IsEqualTo("Dinner,Lunch");
    }

    [Test]
    public async Task FromQuery_TrimsTheQuery_AndPutsTheTimesInRange()
    {
        var swapped = Parse("?query=%20%20soup%20&timeMin=45&timeMax=10");
        var clamped = Parse("?timeMin=-5&timeMax=500");

        _ = await Assert.That(swapped.Query).IsEqualTo("soup");
        _ = await Assert.That(swapped.TimeMin).IsEqualTo(10);
        _ = await Assert.That(swapped.TimeMax).IsEqualTo(45);
        _ = await Assert.That(clamped.TimeMin).IsEqualTo(0);
        _ = await Assert.That(clamped.TimeMax).IsEqualTo(RecipeSearchCriteria.MaxTime);
    }

    private static RecipeSearchCriteria Parse(string query) =>
        RecipeLibraryFilters.FromQuery(new QueryCollection(QueryHelpers.ParseQuery(query)), Offered);
}
