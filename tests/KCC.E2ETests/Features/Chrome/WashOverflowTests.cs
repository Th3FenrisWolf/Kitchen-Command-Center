using System.Text.RegularExpressions;
using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.Chrome;

public class WashOverflowTests : BasePageTests
{
    private const string Library = "/recipes/";

    private const string Recipe = "/recipes/spicy-ramen-flight/";

    private const string Variant = "/recipes/spicy-ramen-flight/chili-oil-shoyu";

    private const string PadCard = "pad-card";

    private const string Measure = """
        () => [document.documentElement, ...document.querySelectorAll('.kcc-sheet--scroll')]
          .filter((element) => element === document.documentElement || element.checkVisibility())
          .map((element) => `${element === document.documentElement ? 'document' : element.closest('[id]')?.id ?? element.className} ${element.scrollWidth}x${element.scrollHeight}`)
        """;

    private const string WashedScrollSheets = """
        () => [...document.querySelectorAll('.kcc-sheet--scroll')]
          .filter((sheet) => sheet.checkVisibility() && [...sheet.querySelectorAll('.kcc-wash')].some((wash) => wash.checkVisibility()))
          .length
        """;

    [Test]
    [Arguments(Library, 120, false)]
    [Arguments(Library, 300, false)]
    [Arguments(Recipe, 120, false)]
    [Arguments(Recipe, 300, false)]
    [Arguments(Variant, 120, false)]
    [Arguments(Variant, 300, false)]
    [Arguments(Library, 120, true)]
    [Arguments(Library, 300, true)]
    [Arguments(Recipe, 120, true)]
    [Arguments(Recipe, 300, true)]
    [Arguments(Variant, 120, true)]
    [Arguments(Variant, 300, true)]
    [Arguments(PadCard, 120, true)]
    [Arguments(PadCard, 300, true)]
    public async Task Washes_NeverChangeAScrollRange(string page, int seconds, bool phone)
    {
        await RecipeIndex.CurrentAsync(Page);
        var options = new BrowserNewContextOptions(ContextOptions(TestContext.Current!));
        if (phone)
        {
            options.ViewportSize = new() { Width = 375, Height = 812 };
            options.HasTouch = true;
            options.IsMobile = true;
        }

        var context = await Browser.NewContextAsync(options);
        try
        {
            await Visit.BeginAsync(context, Site.BaseUrl, seconds);
            var tab = await context.NewPageAsync();
            if (page == PadCard)
            {
                await tab.GotoAsync("/");
                await tab.Locator("#pad-menu-menu").TapAsync();
                await Expect(tab.Locator("#pad-card-menu")).ToHaveClassAsync(new Regex(@"\bis-open\b"));
                await tab.Locator("#pad-card-menu").EvaluateAsync("async (card) => { await Promise.all(card.getAnimations({ subtree: true }).filter((a) => !a.effect.target.matches('.kcc-wash')).map((a) => a.finished)) }");
            }
            else
            {
                await tab.GotoAsync(page);
                await Washes.SettleAsync(tab);
            }

            await tab.EvaluateAsync("() => document.fonts.ready");
            _ = await Assert.That((await Washes.ReadAsync(tab)).Length).IsGreaterThan(0);
            if (page == PadCard)
            {
                _ = await Assert.That(await tab.EvaluateAsync<int>(WashedScrollSheets)).IsGreaterThan(0);
            }

            var withWashes = await tab.EvaluateAsync<string[]>(Measure);
            await tab.AddStyleTagAsync(new() { Content = ".kcc-washes { display: none !important; }" });
            var without = await tab.EvaluateAsync<string[]>(Measure);

            _ = await Assert.That(withWashes).IsEquivalentTo(without);
        }
        finally
        {
            await context.CloseAsync();
        }
    }
}
