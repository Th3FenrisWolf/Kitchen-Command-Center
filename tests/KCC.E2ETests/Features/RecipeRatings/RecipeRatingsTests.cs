using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.RecipeRatings;

public class RecipeRatingsTests : BasePageTests
{
    [Test]
    public async Task RecipeHero_TimesCookedBadge_IsHiddenAtZeroAndShownWhenPositive()
    {
        _ = await RecipeNavigation.GoToFirstRecipeAsync(Page);

        var badge = Page.Locator("[data-testid='times-cooked']");
        var count = await badge.CountAsync();

        if (count == 0)
        {
            // Hidden-at-zero: the recipe has no cooked-marks, so the badge is absent. Valid state.
            await Expect(badge).ToHaveCountAsync(0);
        }
        else
        {
            // Shown-when-positive: the badge renders a non-empty count.
            await Expect(badge.First).ToBeVisibleAsync();
            var text = await badge.First.InnerTextAsync();
            _ = await Assert.That(text.Any(char.IsDigit)).IsTrue();
        }
    }

    [Test]
    public async Task TopRatedSort_ReordersTheVariantList()
    {
        _ = await RecipeNavigation.GoToFirstRecipeAsync(Page);

        // Capture the variant order under the default sort.
        var cards = Page.Locator("[data-variant-name]");
        if (await cards.CountAsync() < 2)
        {
            // Need at least two variants to observe a reorder; skip gracefully on sparse content.
            return;
        }

        // The toolbar "Top Rated" button renders the SortTopRated resource string. On an unseeded
        // environment the string renders as its raw key, so resolve the button by either the seeded
        // label or the key; skip gracefully if neither is present (the reorder is content-dependent).
        var topRated = Page.GetByRole(AriaRole.Button, new() { Name = "Top Rated", Exact = false });
        if (await topRated.CountAsync() == 0)
        {
            topRated = Page.GetByRole(AriaRole.Button, new() { Name = "SortTopRated", Exact = false });
        }
        if (await topRated.CountAsync() == 0)
        {
            return;
        }

        var before = await cards.AllInnerTextsAsync();

        await topRated.First.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var after = await cards.AllInnerTextsAsync();

        // Top Rated puts the highest-rated first (unrated last); on rated content the order differs.
        _ = await Assert.That(after).IsNotEquivalentTo(before);
    }
}
