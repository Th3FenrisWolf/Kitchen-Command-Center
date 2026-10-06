using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.Chrome;

[NotInParallel(MemberSession.Serial)]
public class MemberPadTests : BasePageTests
{
    public override BrowserNewContextOptions ContextOptions(TestContext testContext)
    {
        var options = base.ContextOptions(testContext);
        options.ViewportSize = new() { Width = 768, Height = 1024 };
        options.ReducedMotion = ReducedMotion.Reduce;
        return options;
    }

    [Test]
    public async Task AMembersRow_FitsTheNarrowestDesktop_WithTheSearchFieldWidened()
    {
        await MemberSession.SignInAsync(Page);
        await Page.GotoAsync("/recipes/");
        var row = Page.Locator("#pad-bar-desk");

        await Page.Locator("#pad-search-input").FocusAsync();

        // The row's width depends on the label fonts, and the field eases out to its focused width.
        await row.Locator(".pad-search-field").EvaluateAsync(
            "async field => { await document.fonts.ready; await Promise.all(field.getAnimations().map(move => move.finished)); }");
        _ = await Assert.That(await row.EvaluateAsync<double>("row => row.scrollWidth - row.clientWidth")).IsEqualTo(0);
        await Expect(row.GetByRole(AriaRole.Link, new() { Name = "New recipe" })).ToBeVisibleAsync();
    }
}
