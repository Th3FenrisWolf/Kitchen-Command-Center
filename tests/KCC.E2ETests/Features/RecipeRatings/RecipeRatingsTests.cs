using Microsoft.Playwright;

namespace KCC.E2ETests.Features.RecipeRatings;

public class RecipeRatingsTests : BasePageTests
{
    // Four seeded variants, created a minute apart, of which only the first, Chili Oil Shoyu, has reviews; the
    // seeder marks nothing cooked. Vitest covers the times-cooked badge when there is a count to show.
    private const string RecipePath = "/recipes/spicy-ramen-flight";

    [Test]
    public async Task RecipeHero_TimesCookedBadge_IsHiddenAtZero()
    {
        await Page.GotoAsync(RecipePath);

        await Expect(Page.Locator("[data-testid='times-cooked']")).ToHaveCountAsync(0);
    }

    [Test]
    public async Task TopRatedSort_PutsTheRatedVariantFirst()
    {
        await Page.GotoAsync(RecipePath);
        var cards = Page.Locator("[data-variant-name]");

        await Expect(cards).ToHaveCountAsync(4);
        await Expect(cards.First).ToHaveAttributeAsync("data-variant-name", "Coconut Dairy-Free");

        await Page.GetByTestId("sort-rating").ClickAsync();

        await Expect(cards.First).ToHaveAttributeAsync("data-variant-name", "Chili Oil Shoyu");
    }
}
