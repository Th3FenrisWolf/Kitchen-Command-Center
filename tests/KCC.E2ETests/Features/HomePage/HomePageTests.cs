using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.HomePage;

public class HomePageTests : BasePageTests
{
    // The drawer eases open, so its height is fractional mid-transition, and each card opens to its own height.
    // Two digits clears the shut 0px without pinning a final size.
    private static readonly Regex Opened = new(@"^[1-9]\d+(\.\d+)?px$");

    [Test]
    public async Task Home_ShowsTheBaselineSections()
    {
        var response = await Page.GotoAsync("/");

        _ = await Assert.That(response!.Status).IsEqualTo(200);
        await Expect(Page.Locator("section[data-block]")).ToHaveCountAsync(6);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Welcome to Kitchen Command Center!", Level = 1 })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "How About Something Sweeter?" })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Delicious Drinks" })).ToBeVisibleAsync();
    }

    [Test]
    public async Task Home_ServerRendersTheCards()
    {
        var response = await Page.APIRequest.GetAsync("/");
        var html = await response.TextAsync();

        _ = await Assert.That(html.Contains("<h2 class=\"kcc-h4\">Beef</h2>", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(html.Contains("<h3 class=\"kcc-h4\">Vodka</h3>", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task CardDrawer_OpensOnHover_AndLeadsToTheRecipes()
    {
        await Page.GotoAsync("/");
        var beef = Card("Beef");
        var drawer = beef.Locator("[data-card-drawer]");
        await Expect(drawer).ToHaveCSSAsync("height", "0px");

        await beef.HoverAsync();

        await Expect(drawer).ToHaveCSSAsync("height", Opened);
        await beef.GetByRole(AriaRole.Link, new() { Name = "View Recipes" }).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/recipes/$"));
    }

    [Test]
    public async Task CardDrawer_OpensForTheKeyboard()
    {
        await Page.GotoAsync("/");
        var cake = Card("Cake");

        await cake.GetByRole(AriaRole.Link, new() { Name = "View Recipes" }).FocusAsync();

        await Expect(cake.Locator("[data-card-drawer]")).ToHaveCSSAsync("height", Opened);
    }

    [Test]
    public async Task Stacker_PinsItsCards_OnceTheClientRuns()
    {
        await Page.GotoAsync("/");
        var firstCard = Page.Locator("section[data-block='stackerBlock'] [data-card]").First;
        var slip = firstCard.Locator(".kcc-slip");

        // The stacker's observer reports a sentinel only when it goes from fully visible to not, so the card is
        // brought into view and seen there before the page scrolls past it.
        await firstCard.ScrollIntoViewIfNeededAsync();
        await Expect(firstCard).ToBeInViewportAsync();
        await Expect(slip).Not.ToContainClassAsync("stuck");

        await Page.Mouse.WheelAsync(0, Page.ViewportSize!.Height);

        await Expect(slip).ToContainClassAsync("stuck");
    }

    private ILocator Card(string heading) =>
        Page.Locator(".kcc-slip").Filter(new() { Has = Page.GetByRole(AriaRole.Heading, new() { Name = heading, Exact = true }) });
}
