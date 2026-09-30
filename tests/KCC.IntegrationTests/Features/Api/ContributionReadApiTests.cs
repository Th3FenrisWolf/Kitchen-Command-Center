using System.Text.Json;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.DevTools.RecipeSeed;

namespace KCC.IntegrationTests.Features.Api;

public class ContributionReadApiTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task Reviews_AnswerInTheShapeVariantReviewsReads()
    {
        using var client = Site.CreateClient();
        var variantKey = SeedKeys.Variant("Fluffy Buttermilk Pancakes", "Classic Stack");

        using var json = JsonDocument.Parse(await client.GetStringAsync($"/api/variant/{variantKey}/reviews?page=0&pageSize=10"));
        var root = json.RootElement;
        var newest = root.GetProperty("reviews")[0];

        _ = await Assert.That(root.GetProperty("average").GetDouble()).IsEqualTo(4.25d);
        _ = await Assert.That(root.GetProperty("count").GetInt32()).IsEqualTo(2);
        _ = await Assert.That(root.GetProperty("distribution").ToString()).IsEqualTo("[0,0,0,2,0]");
        _ = await Assert.That(root.GetProperty("total").GetInt32()).IsEqualTo(2);
        _ = await Assert.That(newest.GetProperty("text").GetString()).IsEqualTo("Seeded review #2");
        _ = await Assert.That(newest.GetProperty("authorName").GetString()).IsEqualTo("(deleted)");
        _ = await Assert.That(newest.GetProperty("isMine").GetBoolean()).IsFalse();
        _ = await Assert.That(newest.GetProperty("created").GetString()).EndsWith("Z");
        _ = await Assert.That(root.GetProperty("myReview").ValueKind).IsEqualTo(JsonValueKind.Null);
    }

    [Test]
    public async Task Notes_AnswerInTheShapeVariantCookNotesReads()
    {
        using var client = Site.CreateClient();
        var variantKey = SeedKeys.Variant("Fluffy Buttermilk Pancakes", "Classic Stack");

        using var json = JsonDocument.Parse(await client.GetStringAsync($"/api/variant/{variantKey}/notes?page=0&pageSize=10"));
        var root = json.RootElement;

        _ = await Assert.That(root.GetProperty("total").GetInt32()).IsEqualTo(0);
        _ = await Assert.That(root.GetProperty("pageSize").GetInt32()).IsEqualTo(10);
        _ = await Assert.That(root.GetProperty("notes").GetArrayLength()).IsEqualTo(0);
    }
}
