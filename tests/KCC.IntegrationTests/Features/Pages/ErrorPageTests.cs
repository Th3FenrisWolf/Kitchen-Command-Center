using System.Net;
using System.Text.Json;
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
    [Arguments("GET")]
    [Arguments("POST")]
    public async Task UnhandledException_RendersThe500PageWithA500Status(string method)
    {
        using var client = Site.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), UmbracoSite.ThrowingPath);
        using var response = await client.SendAsync(request);
        var html = await response.Content.ReadAsStringAsync();

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.InternalServerError);
        _ = await Assert.That(html.Contains("Something went wrong", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task StatusPage_KeepsEditorContentOutOfVueCompilation()
    {
        using var client = Site.CreateClient();
        using var response = await client.GetAsync("/this-page-does-not-exist");
        var body = BodyContent(await response.Content.ReadAsStringAsync());

        _ = await Assert.That(body).Matches(@"<h1\b[^>]*\sv-pre\b[^>]*>[^<]*find that page</h1>");
        _ = await Assert.That(body).Matches(@"<div\b[^>]*\sv-pre\b[^>]*>\s*<p>The page may have moved");
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

    // The page body reaches the browser as a Vue template inside this JSON payload, which App.vue compiles.
    private static string BodyContent(string html)
    {
        const string open = "<script id=\"server-content\" type=\"application/json\">";
        var start = html.IndexOf(open, StringComparison.Ordinal) + open.Length;
        var end = html.IndexOf("</script>", start, StringComparison.Ordinal);
        using var json = JsonDocument.Parse(html[start..end]);

        return json.RootElement.GetProperty("bodyContent").GetString() ?? string.Empty;
    }
}
