using KCC.Web.Features.Pages.RecipeSearch;
using KCC.Web.Features.Recipes;
using KCC.Web.Features.Search;

namespace KCC.UnitTests.Features.Pages.RecipeSearch;

public class RecipeLibraryFiltersTests
{
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
}
