using System.Net;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.DevTools.RecipeSeed;
using KCC.Web.Features.Models.Generated;
using KCC.Web.Features.Recipes;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Web;

namespace KCC.IntegrationTests.Features.Recipes;

public class RecipeQueriesTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task GetRecipePage_ReadsASeededRecipe()
    {
        var page = WithContent((content, queries) => queries.GetRecipePage((Recipe)content.GetById(SeedKeys.Recipe("Spicy Ramen Flight"))!));

        _ = await Assert.That(page.Recipe.Name).IsEqualTo("Spicy Ramen Flight");
        _ = await Assert.That(page.Recipe.Url).IsEqualTo("/recipes/spicy-ramen-flight/");
        _ = await Assert.That(page.Recipe.Category).IsEqualTo("Lunch");
        _ = await Assert.That(page.Recipe.AuthorKey).IsEqualTo(SeedKeys.Author("priya.balan"));
        _ = await Assert.That(page.Recipe.ImageUrl).IsNull();
        _ = await Assert.That(page.Variants.Count).IsEqualTo(4);
        _ = await Assert.That(page.AddVariantUrl).IsEqualTo("/recipes/add-variant/");
    }

    [Test]
    public async Task GetRecipePage_ReadsEachVariant()
    {
        var page = WithContent((content, queries) => queries.GetRecipePage((Recipe)content.GetById(SeedKeys.Recipe("Spicy Ramen Flight"))!));

        var shoyu = page.Variants.Single(variant => variant.Name == "Chili Oil Shoyu");

        _ = await Assert.That(shoyu.Url).IsEqualTo("/recipes/spicy-ramen-flight/chili-oil-shoyu/");
        _ = await Assert.That(shoyu.TotalTime).IsEqualTo(20);
        _ = await Assert.That(shoyu.Servings).IsEqualTo(1);
        _ = await Assert.That(string.Join(",", shoyu.Tags)).IsEqualTo("Spicy");
        _ = await Assert.That(shoyu.Nutrition.Calories).IsNull();
        _ = await Assert.That(shoyu.Difficulty).IsNull();
        _ = await Assert.That(shoyu.AuthorKey).IsEqualTo(SeedKeys.Author("priya.balan"));
        _ = await Assert.That(shoyu.IngredientsJson.Contains("\"Chili Oil\"", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task GetVariantPage_ReadsTheRecipeAndTheOtherVariants()
    {
        var page = WithContent((content, queries) => queries.GetVariantPage(
            (RecipeVariant)content.GetById(SeedKeys.Variant("Spicy Ramen Flight", "Chili Oil Shoyu"))!));

        _ = await Assert.That(page.Recipe.Name).IsEqualTo("Spicy Ramen Flight");
        _ = await Assert.That(page.Siblings.Count).IsEqualTo(3);
        _ = await Assert.That(page.Siblings.Any(sibling => sibling.Name == "Chili Oil Shoyu")).IsFalse();
    }

    [Test]
    public async Task GetRecipePage_LeavesOutAnUnpublishedVariant()
    {
        var recipeKey = await TestContent.RecipeAsync(Site.Services, "IT Half Published");
        await TestContent.VariantAsync(Site.Services, recipeKey, "Kept");
        var withdrawn = await TestContent.VariantAsync(Site.Services, recipeKey, "Withdrawn");
        await TestContent.UnpublishAsync(Site.Services, withdrawn);

        var page = WithContent((content, queries) => queries.GetRecipePage((Recipe)content.GetById(recipeKey)!));

        _ = await Assert.That(string.Join(",", page.Variants.Select(variant => variant.Name))).IsEqualTo("Kept");
    }

    [Test]
    public async Task RecipeImage_IsAWebpTileBrowsersKeepForAYear()
    {
        var imageKey = await TestContent.ImageAsync(Site.Services, "IT Stack photo");
        var recipeKey = await TestContent.RecipeAsync(Site.Services, "IT Pictured", TestContent.Image("image", imageKey));

        var tile = WithContent((content, queries) => queries.GetRecipePage((Recipe)content.GetById(recipeKey)!)).Recipe.ImageUrl;
        using var client = Site.CreateClient();
        using var image = await client.GetAsync(tile);

        _ = await Assert.That(tile.Contains($"width={RecipeImages.TileSize}&height={RecipeImages.TileSize}", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(tile.Contains("format=webp", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(image.StatusCode).IsEqualTo(HttpStatusCode.OK);
        _ = await Assert.That(image.Content.Headers.ContentType!.MediaType).IsEqualTo("image/webp");
        _ = await Assert.That(image.Headers.CacheControl!.MaxAge).IsEqualTo(TimeSpan.FromDays(365));
    }

    [Test]
    public async Task GetPublishedRecipes_ListsEveryPublishedRecipeWithItsVariants()
    {
        var withdrawn = await TestContent.RecipeAsync(Site.Services, "IT Never Listed");
        await TestContent.UnpublishAsync(Site.Services, withdrawn);

        var recipes = WithContent((_, queries) => queries.GetPublishedRecipes());

        _ = await Assert.That(recipes.Count(page => page.Recipe.Key == SeedKeys.Recipe("Spicy Ramen Flight"))).IsEqualTo(1);
        _ = await Assert.That(recipes.Single(page => page.Recipe.Key == SeedKeys.Recipe("Spicy Ramen Flight")).Variants.Count).IsEqualTo(4);
        _ = await Assert.That(recipes.Single(page => page.Recipe.Key == SeedKeys.Recipe("Spicy Ramen Flight")).AddVariantUrl).IsEqualTo("/recipes/add-variant/");
        _ = await Assert.That(recipes.Count(page => SeedKeys.Recipe(page.Recipe.Name) == page.Recipe.Key)).IsEqualTo(25);
        _ = await Assert.That(recipes.Any(page => page.Recipe.Key == withdrawn)).IsFalse();
    }

    [Test]
    public async Task GetCreateRecipeUrl_FindsTheWizardUnderTheListing()
    {
        var url = WithContent((content, queries) => queries.GetCreateRecipeUrl(
            (RecipeListingPage)content.GetById(TestContent.RecipeListing(Site.Services))!));

        _ = await Assert.That(url).IsEqualTo("/recipes/create-recipe/");
    }

    private T WithContent<T>(Func<IPublishedContentCache, IRecipeQueries, T> read)
    {
        using var context = Site.Services.GetRequiredService<IUmbracoContextFactory>().EnsureUmbracoContext();
        using var scope = Site.Services.CreateScope();
        return read(context.UmbracoContext.Content!, scope.ServiceProvider.GetRequiredService<IRecipeQueries>());
    }
}
