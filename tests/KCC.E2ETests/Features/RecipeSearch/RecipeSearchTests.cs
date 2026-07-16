using Microsoft.Playwright;

namespace KCC.E2ETests.Features.RecipeSearch;

// TUnit runs tests in parallel by default; concurrent navigations starve the single-threaded
// Vite dev server and every nav times out. [NotInParallel] serializes them (matches the
// VariantReviewsTests / RecipeRatingsTests precedent in this repo).
[NotInParallel]
public class RecipeSearchTests : BasePageTests
{
    // The dev page holds an open Vite HMR WebSocket, so the default `load` event never settles
    // and GotoAsync would time out. The listing is server-rendered, so DOMContentLoaded already
    // exposes the seeded recipe cards these specs assert on.
    private async Task GotoSearchAsync() =>
        _ = await Page.GotoAsync("/recipes", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

    // Search is submit-based: RecipeSearchHeader only emits `submit` (which sets state.query)
    // when the form is submitted, which kicks off a `/api/recipes/search` fetch. Wait for that
    // request to settle before the caller inspects the results.
    private async Task SearchForAsync(string query)
    {
        await Page.Locator("[data-testid='recipe-search-input']").FillAsync(query);
        await Page.Locator("[data-testid='recipe-search-submit']").ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [Test]
    public async Task Search_page_lists_recipe_cards()
    {
        await GotoSearchAsync();

        // Seeded index currently has 2 recipes ("Egg Skillet", "Mac & Cheese"); assert a lower
        // bound so the spec doesn't need updating as more recipes are seeded.
        var count = await Page.Locator("[data-testid='recipe-card']").CountAsync();
        _ = await Assert.That(count).IsGreaterThanOrEqualTo(2);
    }

    [Test]
    public async Task Search_narrows_results_by_query()
    {
        await GotoSearchAsync();
        var unfilteredCount = await Page.Locator("[data-testid='recipe-card']").CountAsync();

        await SearchForAsync("egg");

        // "egg" matches only the seeded "Egg Skillet" recipe.
        var cards = Page.Locator("[data-testid='recipe-card']");
        await Expect(cards).ToHaveCountAsync(1);

        var eggMatch = Page.Locator("[data-testid='recipe-card'][data-recipe-name*='Egg' i]");
        await Expect(eggMatch).ToHaveCountAsync(1);

        // The filtered result (1) is fewer than the unfiltered listing.
        _ = await Assert.That(unfilteredCount).IsGreaterThan(1);
    }

    [Test]
    public async Task No_matches_shows_empty_state()
    {
        await GotoSearchAsync();

        await SearchForAsync("zzznotarealrecipe");

        await Expect(Page.Locator("[data-testid='recipes-empty']")).ToBeVisibleAsync();
        await Expect(Page.Locator("[data-testid='recipe-card']")).ToHaveCountAsync(0);
    }

    [Test]
    public async Task View_toggle_switches_to_list()
    {
        await GotoSearchAsync();
        var cards = Page.Locator("[data-testid='recipe-card']");
        var gridCount = await cards.CountAsync();
        _ = await Assert.That(gridCount).IsGreaterThanOrEqualTo(2);

        await Page.Locator("[data-testid='view-list']").ClickAsync();

        // List rows (RecipeListRow) reuse the same [data-testid='recipe-card'] hook as the grid
        // cards (RecipeCard); switching view is display-only (no refetch), so the count is
        // unchanged. Expect(...) retries until Vue's reactive re-render lands.
        await Expect(cards).ToHaveCountAsync(gridCount);
    }
}
