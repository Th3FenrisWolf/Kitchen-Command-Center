using KCC.Web.Features.Sitemap;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;

namespace KCC.UnitTests.Features.Sitemap;

public class RobotsTxtProviderTests
{
    [Test]
    public async Task GetContent_WhenOpen_DisallowsThePrivatePaths()
    {
        var content = Provider(denyAll: false).GetContent();

        foreach (var path in new[] { "/umbraco", "/api", "/account", "/error" })
        {
            _ = await Assert.That(content.Contains($"Disallow: {path}", StringComparison.Ordinal)).IsTrue();
        }

        _ = await Assert.That(content.Contains("Sitemap: https://kcc.test/sitemap.xml", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task GetContent_WhenDenyingAll_DisallowsEverything()
    {
        var content = Provider(denyAll: true).GetContent();

        _ = await Assert.That(content.Contains("Disallow: /", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(content.Contains("Disallow: /umbraco", StringComparison.Ordinal)).IsFalse();
    }

    private static RobotsTxtProvider Provider(bool denyAll)
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("kcc.test");

        var accessor = new Mock<IHttpContextAccessor>();
        accessor.SetupGet(a => a.HttpContext).Returns(context);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string> { ["RobotsTxtDenyAll"] = denyAll ? "true" : "false" })
            .Build();

        return new RobotsTxtProvider(accessor.Object, configuration);
    }
}
