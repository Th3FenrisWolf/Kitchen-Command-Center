using KCC.IntegrationTests.Config;
using KCC.Web.Features.Search;
using Microsoft.Extensions.DependencyInjection;

namespace KCC.IntegrationTests.Features.Search;

public class RecipeIndexSourceTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task Load_ReadsASeededRecipeWithItsVariantsAuthorAndRating()
    {
        var ramen = (await LoadAsync()).Single(document => document.Name == "Spicy Ramen Flight");

        _ = await Assert.That(ramen.Slug).IsEqualTo("/recipes/spicy-ramen-flight/");
        _ = await Assert.That(ramen.Category).IsEqualTo("Lunch");
        _ = await Assert.That(ramen.StartedBy).IsEqualTo("Priya Balan");
        _ = await Assert.That(string.Join(",", ramen.Tags)).IsEqualTo("Spicy,Vegan,High-Protein,Dairy-Free");
        _ = await Assert.That(ramen.IngredientNames.Contains("Chili Oil")).IsTrue();
        _ = await Assert.That(ramen.FastestTime).IsEqualTo(20);
        _ = await Assert.That(ramen.VariantCount).IsEqualTo(4);
        _ = await Assert.That(ramen.AverageRating).IsEqualTo(4.5d);
        _ = await Assert.That(ramen.ReviewCount).IsEqualTo(3);
    }

    [Test]
    public async Task Load_ListsARecipesStylesBesideItsDiets()
    {
        var ramen = (await LoadAsync()).Single(document => document.Name == "Spicy Ramen Flight");

        _ = await Assert.That(string.Join(",", ramen.Styles)).IsEqualTo("Spicy");
        _ = await Assert.That(string.Join(",", ramen.Diets)).IsEqualTo("Vegan,High-Protein,Dairy-Free");
    }

    [Test]
    public async Task Load_KeepsARecipeWithNoVariants()
    {
        var board = (await LoadAsync()).Single(document => document.Name == "Bare Cupboard Snack Board");

        _ = await Assert.That(board.VariantCount).IsEqualTo(0);
        _ = await Assert.That(board.FastestTime).IsEqualTo(0);
        _ = await Assert.That(board.StartedBy).IsEqualTo(string.Empty);
    }

    private async Task<IReadOnlyList<RecipeSearchDocument>> LoadAsync()
    {
        using var scope = Site.Services.CreateScope();
        return await ActivatorUtilities.CreateInstance<RecipeIndexSource>(scope.ServiceProvider).LoadAsync();
    }
}
