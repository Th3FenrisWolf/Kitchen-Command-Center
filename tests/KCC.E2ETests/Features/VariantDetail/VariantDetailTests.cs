using Microsoft.Playwright;

namespace KCC.E2ETests.Features.VariantDetail;

public class VariantDetailTests : BasePageTests
{
    // Seeded with a two-step method and neither nutrition nor a difficulty, so each check has one right answer.
    // Vitest covers the filled-in nutrition card and the difficulty tile.
    private const string VariantPath = "/recipes/fluffy-buttermilk-pancakes/classic-stack";

    [Test]
    public async Task Variant_ShowsItsIngredientsAndSteps()
    {
        var response = await Page.GotoAsync(VariantPath);

        _ = await Assert.That(response!.Status).IsEqualTo(200);
        await Expect(Page.GetByText("Buttermilk", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByText("Whisk the batter.", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByText("Griddle until bubbles pop.", new() { Exact = true })).ToBeVisibleAsync();
    }

    [Test]
    public async Task NutritionCard_WithNoValues_ShowsTheEmptyState()
    {
        await Page.GotoAsync(VariantPath);

        await Expect(Page.Locator("h2").Filter(new() { HasText = "Nutrition" })).ToBeVisibleAsync();
        await Expect(Page.GetByText("isn't available")).ToBeVisibleAsync();
        await Expect(Page.Locator("dl")).ToHaveCountAsync(0);
    }

    [Test]
    public async Task DifficultyTile_WhenUnset_IsOmitted()
    {
        await Page.GotoAsync(VariantPath);

        await Expect(Page.Locator("[data-testid='variant-stats']")).ToBeVisibleAsync();
        await Expect(Page.Locator("[data-testid='difficulty-dot']")).ToHaveCountAsync(0);
    }
}
