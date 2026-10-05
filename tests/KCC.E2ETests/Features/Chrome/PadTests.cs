using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.Chrome;

public class PadTests : BasePageTests
{
    private static readonly Regex Open = new(@"\bis-open\b");

    [Test]
    public async Task AMealInTheRecipesCard_OpensTheLibraryFilteredToIt()
    {
        await Page.GotoAsync("/");
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
        await Page.GotoAsync("/");
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
        await Page.GotoAsync("/recipes/spicy-ramen-flight/");
        await Page.GotoAsync("/");

        await Page.Locator("#pad-search-input").FocusAsync();

        var card = Page.Locator("#pad-card-search");
        await Expect(card.GetByText("Recently viewed")).ToBeVisibleAsync();
        await Expect(card.GetByRole(AriaRole.Link, new() { Name = "Spicy Ramen Flight" }))
            .ToHaveAttributeAsync("href", "/recipes/spicy-ramen-flight/");
    }

    [Test]
    public async Task ArrowDown_OpensACardIntoItsFirstLink_AndEscapeReturnsToItsMenu()
    {
        await Page.GotoAsync("/");
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
        await Page.GotoAsync("/");
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
        await Page.GotoAsync("/");
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
        await Page.GotoAsync("/");

        await Page.Keyboard.PressAsync("/");

        await Expect(Page.Locator("#pad-search-input")).ToBeFocusedAsync();
        await Expect(Page.Locator("#pad-card-search")).ToHaveClassAsync(Open);
    }

    [Test]
    public async Task Tab_MovesIntoAnOpenCard_AndOnAlongThePadPastItsLastLink()
    {
        await Page.GotoAsync("/");
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
        await Page.GotoAsync("/");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Recipes", Exact = true }).ClickAsync();
        await Expect(Page.Locator("#pad-card-recipes")).ToHaveClassAsync(Open);

        await Page.Locator("main").ClickAsync(new() { Position = new() { X = 5, Y = 400 } });

        await Expect(Page.Locator("#pad-card-recipes")).Not.ToHaveClassAsync(Open);
    }

    [Test]
    public async Task ThePad_TucksAwayAsYouRead_AndTheFiltersFollowIt()
    {
        await Page.GotoAsync("/recipes/");
        var header = Page.Locator("header.pad-header");
        var filters = Page.Locator("#recipe-filters");
        var tucked = new Regex(@"\bis-tucked\b");

        await Page.Mouse.WheelAsync(0, 900);

        await Expect(header).ToHaveClassAsync(tucked);
        await Expect(filters).ToHaveCSSAsync("top", "24px");

        await Page.Mouse.WheelAsync(0, -120);

        await Expect(header).Not.ToHaveClassAsync(tucked);
        await Expect(filters).ToHaveCSSAsync("top", "126px");
    }

    [Test]
    public async Task Slash_WhileThePadIsTucked_BringsItBackWithoutScrolling()
    {
        await Page.GotoAsync("/recipes/");
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

    // Playwright's wheel returns while the page is still scrolling smoothly.
    private async Task WheelDownAsync(int pixels)
    {
        await Page.EvaluateAsync("() => { window.scrollEnded = new Promise(resolve => addEventListener('scrollend', () => resolve(), { once: true })) }");
        await Page.Mouse.WheelAsync(0, pixels);
        await Page.EvaluateAsync("() => window.scrollEnded");
    }
}
