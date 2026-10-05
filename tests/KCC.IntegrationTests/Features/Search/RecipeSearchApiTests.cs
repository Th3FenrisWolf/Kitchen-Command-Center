using System.Text.Json;
using KCC.IntegrationTests.Config;

namespace KCC.IntegrationTests.Features.Search;

public class RecipeSearchApiTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task Search_AnswersInTheShapeTheListingReads()
    {
        using var client = Site.CreateClient();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/api/recipes/search?query=tahini"));
        var root = document.RootElement;
        var hit = root.GetProperty("results")[0];

        _ = await Assert.That(root.GetProperty("total").GetInt32()).IsEqualTo(1);
        _ = await Assert.That(root.GetProperty("page").GetInt32()).IsEqualTo(0);
        _ = await Assert.That(root.GetProperty("pageSize").GetInt32()).IsEqualTo(12);
        _ = await Assert.That(hit.GetProperty("name").GetString()).IsEqualTo("Weeknight Bowls");
        _ = await Assert.That(hit.GetProperty("slug").GetString()).IsEqualTo("/recipes/weeknight-bowls/");
        _ = await Assert.That(hit.GetProperty("averageRating").GetDouble()).IsEqualTo(4.5d);
        _ = await Assert.That(hit.GetProperty("variantCount").GetInt32()).IsEqualTo(2);
        _ = await Assert.That(hit.GetProperty("fastestTime").GetInt32()).IsEqualTo(15);
        _ = await Assert.That(root.GetProperty("facets").GetProperty("category").GetProperty("Lunch").GetInt32()).IsEqualTo(1);
        _ = await Assert.That(root.GetProperty("facets").GetProperty("diet").GetProperty("Gluten-Free").GetInt32()).IsEqualTo(1);
        _ = await Assert.That(root.GetProperty("spotlight").GetProperty("name").GetString()).IsEqualTo("Weeknight Bowls");
    }

    [Test]
    public async Task Search_ReadsRepeatedFiltersSortAndPaging()
    {
        using var client = Site.CreateClient();
        using var document = JsonDocument.Parse(
            await client.GetStringAsync("/api/recipes/search?category=Dinner&category=Dessert&sort=rated&pageSize=3"));
        var root = document.RootElement;
        var names = root.GetProperty("results").EnumerateArray().Select(hit => hit.GetProperty("name").GetString());

        _ = await Assert.That(root.GetProperty("total").GetInt32()).IsEqualTo(9);
        _ = await Assert.That(string.Join(",", names)).IsEqualTo("Legendary Lasagna,Molten Chocolate Cake,Slow-Braised Short Ribs");
    }

    [Test]
    public async Task Search_FiltersAndCountsByStyle()
    {
        using var client = Site.CreateClient();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/api/recipes/search?style=Spicy&pageSize=48"));
        var root = document.RootElement;
        var total = root.GetProperty("total").GetInt32();
        var tags = root.GetProperty("results").EnumerateArray()
            .Select(hit => hit.GetProperty("tags").EnumerateArray().Select(tag => tag.GetString()).ToList());

        _ = await Assert.That(total).IsGreaterThan(0);
        _ = await Assert.That(tags.All(hitTags => hitTags.Contains("Spicy"))).IsTrue();
        _ = await Assert.That(root.GetProperty("facets").GetProperty("style").GetProperty("Spicy").GetInt32()).IsEqualTo(total);
    }

    [Test]
    public async Task Search_StyleFilter_AnswersNoResultsForADietTag()
    {
        using var client = Site.CreateClient();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/api/recipes/search?style=Vegan&pageSize=48"));
        var root = document.RootElement;

        _ = await Assert.That(root.GetProperty("total").GetInt32()).IsEqualTo(0);
    }

    [Test]
    public async Task Search_PastTheLastPage_AnswersNoResults()
    {
        using var client = Site.CreateClient();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/api/recipes/search?category=Beverage&page=5"));
        var root = document.RootElement;

        _ = await Assert.That(root.GetProperty("total").GetInt32()).IsEqualTo(4);
        _ = await Assert.That(root.GetProperty("results").GetArrayLength()).IsEqualTo(0);
        _ = await Assert.That(root.GetProperty("spotlight").ValueKind).IsEqualTo(JsonValueKind.Null);
    }
}
