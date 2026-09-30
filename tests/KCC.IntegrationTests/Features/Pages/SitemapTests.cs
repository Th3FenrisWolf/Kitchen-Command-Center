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
    public async Task RobotsTxt_DeniesEverythingUntilLaunch()
    {
        using var client = Site.CreateClient();
        var robots = await client.GetStringAsync("/robots.txt");

        _ = await Assert.That(robots.Split('\n', StringSplitOptions.TrimEntries)).Contains("Disallow: /");
        _ = await Assert.That(robots.Contains("/umbraco", StringComparison.Ordinal)).IsFalse();
        _ = await Assert.That(robots.Contains("sitemap.xml", StringComparison.Ordinal)).IsTrue();
    }
}
