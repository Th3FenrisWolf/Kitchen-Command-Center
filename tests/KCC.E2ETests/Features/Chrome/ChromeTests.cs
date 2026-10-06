using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.Chrome;

public class ChromeTests : BasePageTests
{
    [Test]
    [Arguments("light")]
    [Arguments("dark")]
    public async Task Home_RendersThePadInTheDevicesRamp(string ramp)
    {
        var otherRamp = ramp == "light" ? "dark" : "light";
        await Page.EmulateMediaAsync(new() { ColorScheme = ramp == "dark" ? ColorScheme.Dark : ColorScheme.Light });

        var response = await Page.GotoAsync("/");

        _ = await Assert.That(response!.Status).IsEqualTo(200);
        await Expect(Page.Locator("html")).ToHaveAttributeAsync("data-theme", ramp);
        await Expect(Page.Locator($"#pad-bar-desk img[data-ramp='{ramp}']")).ToBeVisibleAsync();
        await Expect(Page.Locator($"#pad-bar-desk img[data-ramp='{otherRamp}']")).ToBeHiddenAsync();
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Recipes", Exact = true })).ToBeVisibleAsync();
    }

    [Test]
    public async Task Home_IsServerRendered()
    {
        var response = await Page.APIRequest.GetAsync("/");
        var html = await response.TextAsync();

        _ = await Assert.That(html.Contains("<header", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task AVisitor_SeesSignInBackToThePage_AndNothingOfAMember()
    {
        await Page.GotoAsync("/recipes/");

        var signIn = Page.GetByRole(AriaRole.Link, new() { Name = "Sign in", Exact = true });
        await Expect(signIn).ToBeVisibleAsync();
        await Expect(signIn).ToHaveAttributeAsync("href", "/account/login/?returnUrl=%2Frecipes%2F");
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "New recipe" })).ToHaveCountAsync(0);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "My kitchen" })).ToHaveCountAsync(0);
    }

    [Test]
    public async Task Recipes_OpensItsCard()
    {
        await Page.GotoAsync("/");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Recipes", Exact = true }).ClickAsync();

        var card = Page.Locator("#pad-card-recipes");
        await Expect(card).ToHaveClassAsync(new Regex(@"\bis-open\b"));
        await Expect(card.GetByRole(AriaRole.Link, new() { Name = "Surprise me" })).ToBeVisibleAsync();
        await Expect(card.GetByRole(AriaRole.Link, new() { NameRegex = new Regex(@"^All \d+ recipes$") })).ToBeVisibleAsync();
    }
}
