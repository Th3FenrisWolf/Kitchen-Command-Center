using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.Chrome;

public class PhonePadTests : BasePageTests
{
    private static readonly Regex OutOfTheStack = new(@"\bis-(peek|open)\b");

    public override BrowserNewContextOptions ContextOptions(TestContext testContext)
    {
        var options = base.ContextOptions(testContext);
        options.ViewportSize = new() { Width = 375, Height = 812 };
        options.HasTouch = true;
        options.IsMobile = true;
        return options;
    }

    [Test]
    public async Task Menu_OpensOnATap_ToTheLibrary()
    {
        await Page.GotoAsync("/");

        await Page.Locator("#pad-menu-menu").TapAsync();

        var card = Page.Locator("#pad-card-menu");
        await Expect(card).ToHaveClassAsync(new Regex(@"\bis-open\b"));
        await card.Locator("a[href='/recipes/?category=Dinner']").TapAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(@"/recipes/\?category=Dinner$"));
    }

    [Test]
    [Arguments("menu")]
    [Arguments("find")]
    public async Task HoldingAMenu_PeeksItsCardStraightDown(string id)
    {
        await Page.GotoAsync("/");
        var card = Page.Locator($"#pad-card-{id}");
        var rest = await BottomCornersAsync(card);

        var menu = (await Page.Locator($"#pad-menu-{id}").BoundingBoxAsync())!;
        var touch = await Page.Context.NewCDPSessionAsync(Page);
        await TouchAsync(touch, "touchStart", menu.X + (menu.Width / 2), menu.Y + (menu.Height / 2));
        await Expect(card).ToHaveClassAsync(new Regex(@"\bis-peek\b"));
        var peek = await BottomCornersAsync(card);

        _ = await Assert.That(peek.Left.X).IsCloseTo(rest.Left.X, 1);
        _ = await Assert.That(peek.Right.X).IsCloseTo(rest.Right.X, 1);
        _ = await Assert.That(peek.Left.Y).IsGreaterThan(rest.Left.Y);
        _ = await Assert.That(peek.Right.Y).IsGreaterThan(peek.Left.Y);
    }

    // Playwright's touchscreen can only tap, so the scroll goes through the DevTools protocol's touch events. Each is its
    // own call, and on a slow machine the press delay can run out between two of them, so the page clock is paused.
    [Test]
    public async Task AScrollThatStartsOnThePad_LeavesTheCardsInTheStack()
    {
        await Page.Clock.InstallAsync();
        await Page.GotoAsync("/recipes/");
        await Page.EvaluateAsync(
            """
            () => {
              const scroll = (window.padScroll = { states: [], pressed: false })
              const card = document.querySelector('#pad-card-menu')
              new MutationObserver(() => scroll.states.push(card.className)).observe(card, { attributes: true })
              document.querySelector('#pad-menu-menu').addEventListener('pointerdown', (event) => {
                if (event.pointerType === 'touch') scroll.pressed = true
              })
            }
            """);
        await Page.Clock.PauseAtAsync(DateTime.Now.AddMinutes(1));
        var menu = (await Page.Locator("#pad-menu-menu").BoundingBoxAsync())!;
        var x = menu.X + (menu.Width / 2);
        var y = menu.Y + (menu.Height / 2);
        var touch = await Page.Context.NewCDPSessionAsync(Page);

        await TouchAsync(touch, "touchStart", x, y);
        for (var step = 1; step <= 6; step++)
        {
            await TouchAsync(touch, "touchMove", x, y + (step * 30));
        }

        await Page.Clock.RunForAsync(1000);
        await TouchAsync(touch, "touchEnd", x, y + 180);
        await Page.WaitForTimeoutAsync(400);

        var scroll = await Page.EvaluateAsync<TouchScroll>("() => window.padScroll");
        _ = await Assert.That(scroll.Pressed).IsTrue();
        _ = await Assert.That(scroll.States.Where(state => OutOfTheStack.IsMatch(state))).IsEmpty();
    }

    [Test]
    public async Task Slash_WithTheMenuOpen_SwitchesToSearchWithItsFieldFocused()
    {
        await Page.GotoAsync("/");
        await Page.Locator("#pad-menu-menu").TapAsync();
        await Expect(Page.Locator("#pad-card-menu")).ToHaveClassAsync(new Regex(@"\bis-open\b"));

        await Page.Keyboard.PressAsync("/");

        await Expect(Page.Locator("#pad-card-find")).ToHaveClassAsync(new Regex(@"\bis-open\b"));
        await Expect(Page.Locator("#pad-menu-find")).ToHaveClassAsync(new Regex(@"\bkcc-pill\b"));
        await Expect(Page.Locator("#pad-find-input")).ToBeFocusedAsync();
    }

    private static Task<BottomCorners> BottomCornersAsync(ILocator card) =>
        card.EvaluateAsync<BottomCorners>(
            """
            (card) => {
              card.getAnimations().forEach((animation) => animation.finish())
              const pin = (side) => {
                const dot = document.createElement('span')
                dot.style.cssText = `position: absolute; bottom: 0; ${side}: 0`
                card.append(dot)
                const { x, y } = dot.getBoundingClientRect()
                dot.remove()
                return { x, y }
              }
              return { left: pin('left'), right: pin('right') }
            }
            """);

    private static Task TouchAsync(ICDPSession session, string type, double x, double y) =>
        session.SendAsync(
            "Input.dispatchTouchEvent",
            new Dictionary<string, object>
            {
                ["type"] = type,
                ["touchPoints"] = type == "touchEnd" ? Array.Empty<object>() : new object[] { new Dictionary<string, object> { ["x"] = x, ["y"] = y } },
            });

    private sealed class BottomCorners
    {
        public Corner Left { get; set; } = new();

        public Corner Right { get; set; } = new();
    }

    private sealed class Corner
    {
        public double X { get; set; }

        public double Y { get; set; }
    }

    private sealed class TouchScroll
    {
        public string[] States { get; set; } = [];

        public bool Pressed { get; set; }
    }
}
