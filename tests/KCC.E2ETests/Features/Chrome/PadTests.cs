using System.Text.RegularExpressions;
using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.Chrome;

public class PadTests : BasePageTests
{
    private static readonly Regex Open = new(@"\bis-open\b");

    // The pad answers nothing until its island is live, which can come after the load event: a key pressed or a
    // recipe left before then is lost.
    private async Task OpenAsync(string path)
    {
        await Page.GotoAsync(path);
        await Hydration.WaitAsync(Page);
    }

    [Test]
    public async Task AMealInTheRecipesCard_OpensTheLibraryFilteredToIt()
    {
        await OpenAsync("/");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Recipes", Exact = true }).ClickAsync();

        await Page.Locator("#pad-card-recipes a[href='/recipes/?category=Dinner']").ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex(@"/recipes/\?category=Dinner$"));
        await Expect(Page.Locator("#recipe-filters li").Filter(new() { HasText = "Dinner" }).Locator("input[type='checkbox']"))
            .ToBeCheckedAsync();
        await Expect(Page.Locator("#pad-menu-recipes")).ToHaveAttributeAsync("aria-current", "true");
    }

    [Test]
    public async Task Search_FindsRecipesAsYouType_AndEnterOpensTheLibrary()
    {
        await OpenAsync("/");
        var field = Page.Locator("#pad-search-input");

        await field.FillAsync("lasagna");

        await Expect(Page.Locator("#pad-card-search").GetByRole(AriaRole.Link, new() { Name = "Legendary Lasagna" }))
            .ToBeVisibleAsync();

        await field.PressAsync("Enter");

        await Expect(Page).ToHaveURLAsync(new Regex(@"/recipes/\?query=lasagna$"));
        await Expect(Page.Locator("main [data-recipe-name='Legendary Lasagna']")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Search_RemembersTheRecipesYouViewed()
    {
        await OpenAsync("/recipes/spicy-ramen-flight/");
        await OpenAsync("/");

        await Page.Locator("#pad-search-input").FocusAsync();

        var card = Page.Locator("#pad-card-search");
        await Expect(card.GetByText("Recently viewed")).ToBeVisibleAsync();
        await Expect(card.GetByRole(AriaRole.Link, new() { Name = "Spicy Ramen Flight" }))
            .ToHaveAttributeAsync("href", "/recipes/spicy-ramen-flight/");
    }

    [Test]
    public async Task ArrowDown_OpensACardIntoItsFirstLink_AndEscapeReturnsToItsMenu()
    {
        await OpenAsync("/");
        var menu = Page.Locator("#pad-menu-recipes");
        var card = Page.Locator("#pad-card-recipes");
        await menu.FocusAsync();

        await Page.Keyboard.PressAsync("ArrowDown");

        await Expect(card).ToHaveClassAsync(Open);
        await Expect(card.Locator("a").First).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");

        await Expect(card).Not.ToHaveClassAsync(Open);
        await Expect(menu).ToBeFocusedAsync();
    }

    [Test]
    public async Task Escape_InsideTheSearchCard_ClosesIt_AndLeavesFocusInTheField()
    {
        await OpenAsync("/");
        var field = Page.Locator("#pad-search-input");
        var card = Page.Locator("#pad-card-search");
        await field.FocusAsync();
        await Expect(card).ToHaveClassAsync(Open);

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(field).Not.ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");

        await Expect(card).Not.ToHaveClassAsync(Open);
        await Expect(field).ToBeFocusedAsync();
    }

    [Test]
    public async Task Hovering_AMenu_FansItsCardOut_AndLeavingPutsItBack()
    {
        await OpenAsync("/");
        var card = Page.Locator("#pad-card-recipes");
        var peeking = new Regex(@"\bis-peek\b");

        await Page.Locator("#pad-menu-recipes").HoverAsync();
        await Expect(card).ToHaveClassAsync(peeking);

        await Page.Locator("main").HoverAsync();
        await Expect(card).Not.ToHaveClassAsync(peeking);
    }

    [Test]
    public async Task Slash_JumpsToSearch()
    {
        await OpenAsync("/");

        await Page.Keyboard.PressAsync("/");

        await Expect(Page.Locator("#pad-search-input")).ToBeFocusedAsync();
        await Expect(Page.Locator("#pad-card-search")).ToHaveClassAsync(Open);
    }

    [Test]
    public async Task Tab_MovesIntoAnOpenCard_AndOnAlongThePadPastItsLastLink()
    {
        await OpenAsync("/");
        var menu = Page.Locator("#pad-menu-recipes");
        var card = Page.Locator("#pad-card-recipes");
        await menu.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(card).ToHaveClassAsync(Open);

        await Page.Keyboard.PressAsync("Tab");
        await Expect(card.Locator("a").First).ToBeFocusedAsync();

        await card.Locator("a").Last.FocusAsync();
        await Page.Keyboard.PressAsync("Tab");

        await Expect(Page.Locator("#pad-search-input")).ToBeFocusedAsync();
        await Expect(card).Not.ToHaveClassAsync(Open);
    }

    [Test]
    public async Task AClickOutsideThePad_PutsTheOpenCardBack()
    {
        await OpenAsync("/");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Recipes", Exact = true }).ClickAsync();
        await Expect(Page.Locator("#pad-card-recipes")).ToHaveClassAsync(Open);

        await Page.Locator("main").ClickAsync(new() { Position = new() { X = 5, Y = 400 } });

        await Expect(Page.Locator("#pad-card-recipes")).Not.ToHaveClassAsync(Open);
    }

    [Test]
    public async Task ThePad_TucksAwayAsYouRead_AndTheFiltersFollowIt()
    {
        await OpenAsync("/recipes/");
        var header = Page.Locator("header.pad-header");
        var filters = Page.Locator("#recipe-filters");
        var tucked = new Regex(@"\bis-tucked\b");

        await Page.Mouse.WheelAsync(0, 900);

        await Expect(header).ToHaveClassAsync(tucked);
        await Expect(filters).ToHaveCSSAsync("top", "24px");

        await Page.Mouse.WheelAsync(0, -120);

        await Expect(header).Not.ToHaveClassAsync(tucked);
        await Expect(filters).ToHaveCSSAsync("top", "138px");
    }

    [Test]
    public async Task TheTuck_TogglesAClassOnMain_AndLeavesThePadHeightWhereItWas()
    {
        await OpenAsync("/recipes/");
        var main = Page.Locator("main");
        var filters = Page.Locator("#recipe-filters");
        var tucked = new Regex(@"\bis-tucked\b");
        await Expect(main).ToHaveAttributeAsync("style", new Regex(@"--pad-h:\s*\d"));
        var shownHeight = await PadHeightAsync(main);
        await Expect(filters).ToHaveCSSAsync("top", "138px");

        await WheelDownAsync(900);

        await Expect(main).ToHaveClassAsync(tucked);
        await Expect(filters).ToHaveCSSAsync("top", "24px");
        _ = await Assert.That(await PadHeightAsync(main)).IsEqualTo(shownHeight);
        _ = await Assert.That(await main.EvaluateAsync<string>("m => m.style.getPropertyValue('--pad-offset')")).IsEqualTo("");

        await Page.Mouse.WheelAsync(0, -120);

        await Expect(main).Not.ToHaveClassAsync(tucked);
        await Expect(filters).ToHaveCSSAsync("top", "138px");
        _ = await Assert.That(await PadHeightAsync(main)).IsEqualTo(shownHeight);
    }

    [Test]
    public async Task Slash_WhileThePadIsTucked_BringsItBackWithoutScrolling()
    {
        await OpenAsync("/recipes/");
        var header = Page.Locator("header.pad-header");
        var tucked = new Regex(@"\bis-tucked\b");
        await WheelDownAsync(900);
        await Expect(header).ToHaveClassAsync(tucked);
        var scrolled = await Page.EvaluateAsync<double>("() => window.scrollY");

        await Page.Keyboard.PressAsync("/");

        await Expect(Page.Locator("#pad-search-input")).ToBeFocusedAsync();
        _ = await Assert.That(await Page.EvaluateAsync<double>("() => window.scrollY")).IsEqualTo(scrolled);
        await Expect(header).Not.ToHaveClassAsync(tucked);
    }

    private static Task<string> PadHeightAsync(ILocator main) =>
        main.EvaluateAsync<string>("m => m.style.getPropertyValue('--pad-h')");

    // Playwright's wheel returns while the page is still scrolling smoothly.
    private async Task WheelDownAsync(int pixels)
    {
        await Page.EvaluateAsync("() => { window.scrollEnded = new Promise(resolve => addEventListener('scrollend', () => resolve(), { once: true })) }");
        await Page.Mouse.WheelAsync(0, pixels);
        await Page.EvaluateAsync("() => window.scrollEnded");
    }
}
