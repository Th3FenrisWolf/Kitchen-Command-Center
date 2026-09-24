using System.Net;
using KCC.IntegrationTests.Config;

namespace KCC.IntegrationTests.Features.Pages;

public class ErrorPageTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task UnknownUrl_RendersThe404PageWithA404Status()
    {
        using var client = Site.CreateClient();
        using var response = await client.GetAsync("/this-page-does-not-exist");
        var html = await response.Content.ReadAsStringAsync();

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        _ = await Assert.That(html.Contains("find that page", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task ErrorRoute_RendersThe500PageWithA500Status()
    {
        using var client = Site.CreateClient();
        using var response = await client.GetAsync("/error");
        var html = await response.Content.ReadAsStringAsync();

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.InternalServerError);
        _ = await Assert.That(html.Contains("Something went wrong", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task ErrorRouteWithA404Code_RendersThe404PageWithA404Status()
    {
        using var client = Site.CreateClient();
        using var response = await client.GetAsync("/error/404");
        var html = await response.Content.ReadAsStringAsync();

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        _ = await Assert.That(html.Contains("find that page", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    [Arguments("/error/0")]
    [Arguments("/error/200")]
    public async Task ErrorRouteWithANonErrorCode_FallsThroughToThe404Page(string path)
    {
        using var client = Site.CreateClient();
        using var response = await client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        _ = await Assert.That(html.Contains("find that page", StringComparison.Ordinal)).IsTrue();
    }
}
