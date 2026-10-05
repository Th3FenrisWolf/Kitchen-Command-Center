using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.Chrome;

public class PhonePadTests : BasePageTests
{
    private const double PressDelayMs = 80;
    private const int ScrollAttempts = 3;

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

    // Playwright's touchscreen can only tap, so the scroll goes through the DevTools protocol's touch events.
    [Test]
    public async Task AScrollThatStartsOnThePad_LeavesTheCardsInTheStack()
    {
        var touch = await Page.Context.NewCDPSessionAsync(Page);
        var delays = new List<double?>();

        for (var attempt = 0; attempt < ScrollAttempts; attempt++)
        {
            var scroll = await ScrollFromTheMenuAsync(touch);
            if (scroll.MovedWithinThePressDelay)
            {
                _ = await Assert.That(scroll.States.Where(state => OutOfTheStack.IsMatch(state))).IsEmpty();
                return;
            }

            delays.Add(scroll.PressToFirstMoveMs);
        }

        var seen = string.Join(", ", delays.Select(delay => delay is { } ms ? $"{ms:F0} ms" : "no move"));
        Fail.Test(
            $"The touch scroll could not be dispatched within the {PressDelayMs} ms press delay in {ScrollAttempts} attempts (press to first move: {seen}).");
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

    private static Task TouchAsync(ICDPSession session, string type, double x, double y) =>
        session.SendAsync(
            "Input.dispatchTouchEvent",
            new Dictionary<string, object>
            {
                ["type"] = type,
                ["touchPoints"] = type == "touchEnd" ? Array.Empty<object>() : new object[] { new Dictionary<string, object> { ["x"] = x, ["y"] = y } },
            });

    private async Task<TouchScroll> ScrollFromTheMenuAsync(ICDPSession touch)
    {
        await Page.GotoAsync("/recipes/");
        await Page.EvaluateAsync(
            """
            () => {
              const scroll = (window.padScroll = { states: [], down: null, move: null })
              const card = document.querySelector('#pad-card-menu')
              new MutationObserver(() => scroll.states.push(card.className)).observe(card, { attributes: true })
              const menu = document.querySelector('#pad-menu-menu')
              menu.addEventListener('pointerdown', (event) => {
                if (event.pointerType === 'touch') scroll.down ??= performance.now()
              })
              menu.addEventListener('pointermove', (event) => {
                if (event.pointerType === 'touch') scroll.move ??= performance.now()
              })
            }
            """);
        var menu = (await Page.Locator("#pad-menu-menu").BoundingBoxAsync())!;
        var x = menu.X + (menu.Width / 2);
        var y = menu.Y + (menu.Height / 2);

        await TouchAsync(touch, "touchStart", x, y);
        for (var step = 1; step <= 6; step++)
        {
            await TouchAsync(touch, "touchMove", x, y + (step * 30));
        }

        await TouchAsync(touch, "touchEnd", x, y + 180);
        await Page.WaitForTimeoutAsync(400);

        return await Page.EvaluateAsync<TouchScroll>("() => window.padScroll");
    }

    private sealed class TouchScroll
    {
        public string[] States { get; set; } = [];

        public double? Down { get; set; }

        public double? Move { get; set; }

        public double? PressToFirstMoveMs => Move - Down;

        public bool MovedWithinThePressDelay => PressToFirstMoveMs is <= PressDelayMs;
    }
}
