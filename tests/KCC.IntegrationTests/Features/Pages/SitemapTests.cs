using System.Net;
using KCC.IntegrationTests.Config;

namespace KCC.IntegrationTests.Features.Pages;

public class SitemapTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task Sitemap_ListsPublicPagesOnly()
    {
        using var client = Site.CreateClient();
        var xml = await client.GetStringAsync("/sitemap.xml");

        _ = await Assert.That(xml.Contains("/recipes/", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(xml.Contains("/account", StringComparison.Ordinal)).IsFalse();
        _ = await Assert.That(xml.Contains("status-codes", StringComparison.Ordinal)).IsFalse();
    }

    [Test]
    public async Task Sitemap_ListsSeededRecipesAndVariants()
    {
        using var client = Site.CreateClient();
        var xml = await client.GetStringAsync("/sitemap.xml");

        _ = await Assert.That(xml.Contains("/recipes/fluffy-buttermilk-pancakes/", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(xml.Contains("/recipes/fluffy-buttermilk-pancakes/classic-stack/", StringComparison.Ordinal)).IsTrue();
    }

    // An attribute route answers only the verbs it names, and link checkers and some crawlers ask with HEAD.
    [Test]
    public async Task Sitemap_AnswersHead()
    {
        using var client = Site.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Head, "/sitemap.xml");
        using var response = await client.SendAsync(request);

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task RobotsTxt_DeniesEverythingOutsideProduction()
    {
        using var client = Site.CreateClient();
        var robots = await client.GetStringAsync("/robots.txt");

        _ = await Assert.That(robots.Split('\n', StringSplitOptions.TrimEntries)).Contains("Disallow: /");
        _ = await Assert.That(robots.Contains("/umbraco", StringComparison.Ordinal)).IsFalse();
        _ = await Assert.That(robots.Contains("sitemap.xml", StringComparison.Ordinal)).IsTrue();
    }
}
