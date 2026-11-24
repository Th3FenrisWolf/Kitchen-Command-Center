using System.Text.RegularExpressions;
using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.RecipeWizard;

[NotInParallel(MemberSession.Serial)]
public partial class LaterStepTests : BasePageTests
{
    // The steps a wizard can open on load with the page, so only the later ones are held.
    [GeneratedRegex(@"/(?!Start|Basics|Variant)[A-Z][A-Za-z]*Step(-[\w-]+)?\.(js|vue)(\?|$)")]
    private static partial Regex LaterStepChunk();

    private ILocator CurrentStep => Page.Locator("main [aria-current='step']");

    private ILocator Heading(string name) => Page.GetByRole(AriaRole.Heading, new() { Name = name, Exact = true });

    [Test]
    public async Task ANextThatOutrunsItsStep_MovesTheProgressAtOnce_AndFillsTheStepInWithNothingTypedLost()
    {
        await MemberSession.SignInAsync(Page);
        var network = new TaskCompletionSource();
        var held = 0;
        await Page.RouteAsync(LaterStepChunk(), async route =>
        {
            Interlocked.Increment(ref held);
            await network.Task;
            await route.ContinueAsync();
        });

        _ = await Page.GotoAsync("/recipes/create-recipe");
        await Hydration.WaitAsync(Page);
        var next = Page.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true });
        await Page.GetByTestId("wizard-start-typing").ClickAsync();
        await Page.GetByTestId("wizard-recipe-name").FillAsync("Held Chunk");
        await Page.GetByTestId("wizard-recipe-description").FillAsync("Typed by the E2E suite.");
        await next.ClickAsync();
        await Page.GetByTestId("wizard-variant-name").FillAsync("First Try");
        var stepBefore = await CurrentStep.TextContentAsync();

        await next.ClickAsync();

        await Expect(CurrentStep).Not.ToHaveTextAsync(stepBefore!, new() { Timeout = 1000 });
        await Expect(Page.Locator("main .kcc-slip[aria-busy='true']")).ToBeVisibleAsync(new() { Timeout = 1000 });
        await Expect(Page.GetByTestId("wizard-ingredient-name")).ToHaveCountAsync(0);
        await Assert.That(held).IsGreaterThan(0);

        network.SetResult();

        await Expect(Page.GetByTestId("wizard-ingredient-name").First).ToBeVisibleAsync();
        await Expect(Heading("Ingredients")).ToBeFocusedAsync();
        await Expect(Page.Locator("main [aria-busy='true']")).ToHaveCountAsync(0);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Back", Exact = true }).ClickAsync();
        await Expect(Page.GetByTestId("wizard-variant-name")).ToHaveValueAsync("First Try");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Back", Exact = true }).ClickAsync();
        await Expect(Page.GetByTestId("wizard-recipe-name")).ToHaveValueAsync("Held Chunk");
    }
}
