using Microsoft.Playwright;

namespace KCC.E2ETests.Features.Chrome;

public class ChromeTests : BasePageTests
{
    [Test]
    [Arguments("light")]
    [Arguments("dark")]
    public async Task Home_RendersTheHeaderInEachRamp(string ramp)
    {
        var otherRamp = ramp == "light" ? "dark" : "light";

        await Page.AddInitScriptAsync($"localStorage.setItem('kcc-theme', '{ramp}')");

        var response = await Page.GotoAsync("/");

        _ = await Assert.That(response!.Status).IsEqualTo(200);
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("data-theme", ramp);
        await Expect(Page.Locator($"header img[data-ramp='{ramp}']")).ToBeVisibleAsync();
        await Expect(Page.Locator($"header img[data-ramp='{otherRamp}']")).ToBeHiddenAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Recipes", Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Login", Exact = true })).ToBeVisibleAsync();
    }

    [Test]
    public async Task Home_IsServerRendered()
    {
        var response = await Page.APIRequest.GetAsync("/");
        var html = await response.TextAsync();

        _ = await Assert.That(html.Contains("<header", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task RecipesMenu_OpensToItsLinks()
    {
        await Page.GotoAsync("/");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Recipes", Exact = true }).ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "All Recipes", Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Create Recipe", Exact = true })).ToBeVisibleAsync();
    }
}
