using System.Net;
using KCC.IntegrationTests.Config;

namespace KCC.IntegrationTests.Features.Pages;

public class HomePageTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task Home_RendersThroughItsControllerWithoutATemplate()
    {
        using var client = Site.CreateClient();
        using var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        _ = await Assert.That(html.Contains("<title>Kitchen Command Center</title>", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(html.Contains("<script id=\"server-content\" type=\"application/json\">", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Home_HeaderShowsTheSignedOutNavigation()
    {
        using var client = Site.CreateClient();
        var html = await client.GetStringAsync("/");

        _ = await Assert.That(html.Contains("All Recipes", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(html.Contains("Login", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(html.Contains("Profile", StringComparison.Ordinal)).IsFalse();
    }
}
