using System.Net;
using KCC.Contributions;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.DevTools.RecipeSeed;
using Microsoft.Extensions.DependencyInjection;

namespace KCC.IntegrationTests.Features.Pages;

public class RecipePageTests
{
    private const string PancakesPath = "/recipes/fluffy-buttermilk-pancakes/";

    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task SeededRecipe_RendersThroughItsController()
    {
        using var client = Site.CreateClient();

        var page = await RenderedPage.GetAsync(client, PancakesPath);

        _ = await Assert.That(page.Status).IsEqualTo(HttpStatusCode.OK);
        _ = await Assert.That(page.Html.Contains("<title>Fluffy Buttermilk Pancakes</title>", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(page.Attribute("recipe-name")).IsEqualTo("Fluffy Buttermilk Pancakes");
        _ = await Assert.That(page.Attribute("recipe-category")).IsEqualTo("Breakfast");
        _ = await Assert.That(page.Attribute("recipe-guid")).IsEqualTo(SeedKeys.Recipe("Fluffy Buttermilk Pancakes").ToString());
        _ = await Assert.That(page.Attribute("add-variant-url")).IsEqualTo("/recipes/add-variant/");
        _ = await Assert.That(page.Attribute("started-by-name")).IsNull();
    }

    [Test]
    public async Task SeededRecipe_CarriesItsRatingAndVariants()
    {
        using var client = Site.CreateClient();

        var page = await RenderedPage.GetAsync(client, PancakesPath);
        var variants = page.Prop("variants");

        _ = await Assert.That(page.Prop("recipe-average-rating").GetDouble()).IsEqualTo(4.25d);
        _ = await Assert.That(page.Prop("recipe-review-count").GetInt32()).IsEqualTo(2);
        _ = await Assert.That(page.Prop("recipe-times-cooked").GetInt32()).IsEqualTo(0);
        _ = await Assert.That(variants.GetArrayLength()).IsEqualTo(1);
        _ = await Assert.That(variants[0].GetProperty("name").GetString()).IsEqualTo("Classic Stack");
        _ = await Assert.That(variants[0].GetProperty("slug").GetString()).IsEqualTo("/recipes/fluffy-buttermilk-pancakes/classic-stack/");
        _ = await Assert.That(variants[0].GetProperty("totalTime").GetInt32()).IsEqualTo(25);
        _ = await Assert.That(variants[0].GetProperty("tags").ToString()).IsEqualTo("[\"Vegetarian\"]");
    }

    [Test]
    public async Task SeededRecipe_TrailRunsFromHome()
    {
        using var client = Site.CreateClient();

        var page = await RenderedPage.GetAsync(client, PancakesPath);
        var trail = page.Prop("breadcrumbs").EnumerateArray()
            .Select(crumb => $"{crumb.GetProperty("linkText").GetString()}>{crumb.GetProperty("url").GetString()}");

        _ = await Assert.That(string.Join("|", trail)).IsEqualTo("Home>/|Recipes>/recipes/|Fluffy Buttermilk Pancakes>");
    }

    [Test]
    public async Task Recipe_RatesOnlyItsPublishedVariants()
    {
        var recipeKey = await TestContent.RecipeAsync(Site.Services, "IT Rated Recipe");
        var kept = await TestContent.VariantAsync(Site.Services, recipeKey, "Kept");
        var withdrawn = await TestContent.VariantAsync(Site.Services, recipeKey, "Withdrawn");
        var writes = Site.Services.GetRequiredService<IContributionWrites>();
        await writes.UpsertReviewAsync(kept, Guid.NewGuid(), 5m, "Lovely");
        await writes.UpsertReviewAsync(withdrawn, Guid.NewGuid(), 1m, "Gone");
        await TestContent.UnpublishAsync(Site.Services, withdrawn);
        using var client = Site.CreateClient();

        var page = await RenderedPage.GetAsync(client, "/recipes/it-rated-recipe/");

        _ = await Assert.That(page.Prop("recipe-average-rating").GetDouble()).IsEqualTo(5d);
        _ = await Assert.That(page.Prop("recipe-review-count").GetInt32()).IsEqualTo(1);
        _ = await Assert.That(page.Prop("variants").GetArrayLength()).IsEqualTo(1);
    }
}
