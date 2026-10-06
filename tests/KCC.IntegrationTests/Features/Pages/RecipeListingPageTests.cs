using System.Net;
using System.Text.Json;
using KCC.IntegrationTests.Config;

namespace KCC.IntegrationTests.Features.Pages;

public class RecipeListingPageTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task Listing_RendersThroughItsController()
    {
        using var client = Site.CreateClient();

        var page = await RenderedPage.GetAsync(client, "/recipes/");

        _ = await Assert.That(page.Status).IsEqualTo(HttpStatusCode.OK);
        _ = await Assert.That(page.Html.Contains("<title>Recipes</title>", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(page.Attribute("create-recipe-url")).IsEqualTo("/recipes/create-recipe/");
    }

    [Test]
    public async Task Listing_CarriesTheFirstPageOfEveryRecipe()
    {
        using var client = Site.CreateClient();

        var initial = (await RenderedPage.GetAsync(client, "/recipes/")).Prop("initial");

        _ = await Assert.That(initial.GetProperty("total").GetInt32()).IsGreaterThanOrEqualTo(25);
        _ = await Assert.That(initial.GetProperty("results").GetArrayLength()).IsEqualTo(12);
        _ = await Assert.That(initial.GetProperty("results")[0].GetProperty("name").GetString()).IsEqualTo("Avocado Toast Supreme");
        _ = await Assert.That(initial.GetProperty("facets").GetProperty("category").GetProperty("Dinner").GetInt32()).IsEqualTo(5);
    }

    [Test]
    public async Task Listing_TrailRunsFromHome()
    {
        using var client = Site.CreateClient();

        var page = await RenderedPage.GetAsync(client, "/recipes/");
        var trail = page.Prop("breadcrumbs").EnumerateArray()
            .Select(crumb => $"{crumb.GetProperty("linkText").GetString()}>{crumb.GetProperty("url").GetString()}");

        _ = await Assert.That(string.Join("|", trail)).IsEqualTo("Home>/|Recipes>");
    }

    [Test]
    public async Task Listing_OffersWhatItsRecipesUse_InTreeOrder()
    {
        using var client = Site.CreateClient();

        var options = (await RenderedPage.GetAsync(client, "/recipes/")).Prop("options");

        _ = await Assert.That(Names(options, "categories")).IsEqualTo("Breakfast,Lunch,Dinner,Dessert,Snack,Beverage");
        _ = await Assert.That(Names(options, "diets")).IsEqualTo("Vegetarian,Vegan,Gluten-Free,Dairy-Free,Keto,High-Protein,Low-Carb");
        _ = await Assert.That(Names(options, "styles")).IsEqualTo("Spicy");
    }

    [Test]
    public async Task Listing_NamesItsDietsAndStylesGroups()
    {
        using var client = Site.CreateClient();

        var strings = (await RenderedPage.GetAsync(client, "/recipes/")).Prop("resource-strings");

        _ = await Assert.That(strings.GetProperty("RecipeSearch.Diets").GetString()).IsEqualTo("Diets");
        _ = await Assert.That(strings.GetProperty("RecipeSearch.Styles").GetString()).IsEqualTo("Styles");
        _ = await Assert.That(strings.TryGetProperty("RecipeSearch.Dietary", out _)).IsFalse();
    }

    [Test]
    public async Task Listing_OpensADeepLinkFiltered_AndStillOffersEverything()
    {
        using var client = Site.CreateClient();

        var page = await RenderedPage.GetAsync(client, "/recipes/?category=dinner&diet=Vegan&sort=rated");
        var initial = page.Prop("initial");
        var filters = page.Prop("filters");

        _ = await Assert.That(initial.GetProperty("total").GetInt32()).IsEqualTo(1);
        _ = await Assert.That(initial.GetProperty("results")[0].GetProperty("name").GetString()).IsEqualTo("Weeknight Tacos");
        _ = await Assert.That(Names(filters, "categories")).IsEqualTo("Dinner");
        _ = await Assert.That(Names(filters, "diets")).IsEqualTo("Vegan");
        _ = await Assert.That(filters.GetProperty("sort").GetString()).IsEqualTo("rated");
        _ = await Assert.That(Names(page.Prop("options"), "categories")).IsEqualTo("Breakfast,Lunch,Dinner,Dessert,Snack,Beverage");
        _ = await Assert.That(Names(page.Prop("options"), "diets")).IsEqualTo("Vegetarian,Vegan,Gluten-Free,Dairy-Free,Keto,High-Protein,Low-Carb");
    }

    [Test]
    public async Task Listing_IgnoresWhatItDoesNotOffer()
    {
        using var client = Site.CreateClient();

        var page = await RenderedPage.GetAsync(client, "/recipes/?category=Brunch&style=Cheesy&sort=spiciest&timeMin=soon&page=3");
        var initial = page.Prop("initial");
        var filters = page.Prop("filters");

        _ = await Assert.That(initial.GetProperty("total").GetInt32()).IsGreaterThanOrEqualTo(25);
        _ = await Assert.That(initial.GetProperty("page").GetInt32()).IsEqualTo(0);
        _ = await Assert.That(filters.GetProperty("categories").GetArrayLength()).IsEqualTo(0);
        _ = await Assert.That(filters.GetProperty("styles").GetArrayLength()).IsEqualTo(0);
        _ = await Assert.That(filters.GetProperty("sort").GetString()).IsEqualTo("relevant");
        _ = await Assert.That(filters.GetProperty("timeMin").GetInt32()).IsEqualTo(0);
        _ = await Assert.That(filters.TryGetProperty("page", out _)).IsFalse();
    }

    private static string Names(JsonElement lists, string property) =>
        string.Join(",", lists.GetProperty(property).EnumerateArray().Select(name => name.GetString()));
}
