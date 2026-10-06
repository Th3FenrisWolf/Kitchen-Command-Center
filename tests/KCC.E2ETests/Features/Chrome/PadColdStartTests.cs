using KCC.E2ETests.Config;

namespace KCC.E2ETests.Features.Chrome;

[NotInParallel]
public class PadColdStartTests
{
    private static readonly string[] Paths = ["/", "/recipes/", "/recipes/spicy-ramen-flight/", "/account/login/", "/this-page-does-not-exist"];

    // The suite's shared site is warmed before any test runs; this one serves its first pages all at once.
    [Test]
    public async Task AColdSitesFirstPages_EachRenderTheWholePad()
    {
        await using var site = new SiteProcess(withSsr: true, seed: true, warmUp: false);
        await site.InitializeAsync();
        using var http = new HttpClient { BaseAddress = site.BaseUrl };

        var pages = await Task.WhenAll(Paths.Select(async path =>
        {
            using var response = await http.GetAsync(path);
            return await response.Content.ReadAsStringAsync();
        }));

        foreach (var html in pages)
        {
            _ = await Assert.That(html).Contains("id=\"pad-menu-recipes\"");
            _ = await Assert.That(html).Contains("href=\"/account/login/?returnUrl=");
            _ = await Assert.That(html).Contains("href=\"/recipes/?category=Dinner\"");
        }
    }
}
