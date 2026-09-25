using System.Net;
using System.Text.Json;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.DevTools.RecipeSeed;
using KCC.Web.Features.Recipes;

namespace KCC.IntegrationTests.Features.Pages;

public class VariantPageTests
{
    private const string ShoyuPath = "/recipes/spicy-ramen-flight/chili-oil-shoyu/";

    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task SeededVariant_RendersItsMethod()
    {
        using var client = Site.CreateClient();

        var page = await RenderedPage.GetAsync(client, ShoyuPath);
        var ingredients = page.Prop("ingredients").EnumerateArray().Select(ingredient => ingredient.GetProperty("name").GetString());

        _ = await Assert.That(page.Status).IsEqualTo(HttpStatusCode.OK);
        _ = await Assert.That(page.Html.Contains("<title>Chili Oil Shoyu</title>", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(page.Attribute("variant-name")).IsEqualTo("Chili Oil Shoyu");
        _ = await Assert.That(page.Attribute("created-by-name")).IsEqualTo("Priya Balan");
        _ = await Assert.That(page.Attribute("recipe-name")).IsEqualTo("Spicy Ramen Flight");
        _ = await Assert.That(page.Attribute("recipe-slug")).IsEqualTo("/recipes/spicy-ramen-flight/");
        _ = await Assert.That(page.Attribute("difficulty")).IsNull();
        _ = await Assert.That(string.Join(",", ingredients)).IsEqualTo("Ramen Noodles,Chili Oil");
        _ = await Assert.That(page.Prop("instructions").GetArrayLength()).IsEqualTo(2);
        _ = await Assert.That(page.Prop("tags").ToString()).IsEqualTo("[\"Spicy\"]");
        _ = await Assert.That(page.Prop("calories").ValueKind).IsEqualTo(JsonValueKind.Null);
    }

    [Test]
    public async Task SeededVariant_CarriesItsRatingAndSiblings()
    {
        using var client = Site.CreateClient();

        var page = await RenderedPage.GetAsync(client, ShoyuPath);
        var siblings = page.Prop("sibling-variants").EnumerateArray().Select(sibling => sibling.GetProperty("name").GetString()).ToList();

        _ = await Assert.That(page.Prop("average-rating").GetDouble()).IsEqualTo(4.5d);
        _ = await Assert.That(page.Prop("review-count").GetInt32()).IsEqualTo(3);
        _ = await Assert.That(page.Prop("cooked-count").GetInt32()).IsEqualTo(0);
        _ = await Assert.That(page.Prop("has-cooked").GetBoolean()).IsFalse();
        _ = await Assert.That(page.Prop("is-authenticated").GetBoolean()).IsFalse();
        _ = await Assert.That(page.Prop("variant-guid").GetString()).IsEqualTo(SeedKeys.Variant("Spicy Ramen Flight", "Chili Oil Shoyu").ToString());
        _ = await Assert.That(siblings.Count).IsEqualTo(3);
        _ = await Assert.That(siblings.Contains("Chili Oil Shoyu")).IsFalse();
    }

    [Test]
    public async Task VariantImage_IsTheSizedCoverTile()
    {
        var imageKey = await TestContent.ImageAsync(Site.Services, "IT Bowl photo");
        var recipeKey = await TestContent.RecipeAsync(Site.Services, "IT Pictured Bowl");
        await TestContent.VariantAsync(Site.Services, recipeKey, "Glazed", TestContent.Image("images", imageKey));
        using var client = Site.CreateClient();

        var page = await RenderedPage.GetAsync(client, "/recipes/it-pictured-bowl/glazed/");
        var cover = page.Attribute("cover-image");

        _ = await Assert.That(cover).IsNotNull();
        _ = await Assert.That(cover!.Contains($"width={RecipeImages.TileSize}&height={RecipeImages.TileSize}", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(cover.Contains("format=webp", StringComparison.Ordinal)).IsTrue();
    }
}
