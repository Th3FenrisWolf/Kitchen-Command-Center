using KCC.E2ETests.Config;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace KCC.E2ETests.Features.Hosting;

// With the sidecar off every page is the one a sidecar failure serves: no server HTML, rendered by the fallback build.
[NotInParallel]
public class FallbackPageTests
{
    private static readonly string[] Paths = ["/", "/recipes/", "/recipes/spicy-ramen-flight/", "/account/login/"];

    [Test]
    public async Task WithoutTheSidecar_EveryPageRendersInTheBrowser_StyledAndWorking()
    {
        await using var site = new SiteProcess(withSsr: false, seed: true);
        await site.InitializeAsync();
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(new() { BaseURL = site.BaseUrl.ToString() });
        var errors = new List<string>();
        page.PageError += (_, error) => errors.Add(error);

        foreach (var path in Paths)
        {
            await page.GotoAsync(path);
            await page.WaitForFunctionAsync("() => performance.getEntriesByName('kcc:hydrated').length > 0");

            _ = await Assert.That(await page.Locator("#app > *").CountAsync()).IsGreaterThan(0);
            await Expect(page.Locator("#pad-menu-recipes")).ToBeVisibleAsync();

            // AppHeader's style block reaches the page only through the fallback's own stylesheet links.
            await Expect(page.Locator(".pad-header")).ToHaveCSSAsync("position", "sticky");
        }

        await page.GotoAsync("/");
        await page.Locator("#pad-search-input").FillAsync("lasagna");
        await Expect(page.Locator("#pad-card-search").GetByRole(AriaRole.Link, new() { Name = "Legendary Lasagna" }))
            .ToBeVisibleAsync();

        _ = await Assert.That(errors).IsEmpty();
    }
}
