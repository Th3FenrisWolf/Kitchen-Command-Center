using Microsoft.Playwright;

namespace KCC.E2ETests.Features.RecipeSearch;

// The seeded recipes (RecipeSeedData) are the only content: 25 recipes, with Legendary Lasagna the only 5.0.
public class RecipeSearchTests : BasePageTests
{
    [Test]
    public async Task Search_page_lists_recipe_cards()
    {
        await Page.GotoAsync("/recipes");

        // The top-rated recipe takes the spotlight, which keeps it out of the grid.
        await Expect(Page.Locator("[data-testid='recipe-spotlight']")).ToHaveAttributeAsync("data-recipe-name", "Legendary Lasagna");
        await Expect(Page.Locator("[data-testid='recipe-card']").First).ToHaveAttributeAsync("data-recipe-name", "Avocado Toast Supreme");
        await Expect(Page.Locator("[data-testid='recipe-card'][data-recipe-name='Legendary Lasagna']")).ToHaveCountAsync(0);
    }

    [Test]
    public async Task Search_narrows_results_by_query()
    {
        await Page.GotoAsync("/recipes");

        // Grid cards and the "Top Rated" spotlight both carry data-recipe-name, so this counts every result in
        // either slot.
        var results = Page.Locator("[data-recipe-name]");
        _ = await Assert.That(await results.CountAsync()).IsGreaterThan(1);

        await SearchForAsync("tahini");

        // Only Weeknight Bowls lists tahini, as an ingredient. It is rated, so it shows as the spotlight.
        await Expect(results).ToHaveCountAsync(1);
        await Expect(Page.Locator("[data-recipe-name='Weeknight Bowls']")).ToHaveCountAsync(1);
    }

    [Test]
    public async Task No_matches_shows_empty_state()
    {
        await Page.GotoAsync("/recipes");

        await SearchForAsync("zzznotarealrecipe");

        await Expect(Page.Locator("[data-testid='recipes-empty']")).ToBeVisibleAsync();
        await Expect(Page.Locator("[data-testid='recipe-card']")).ToHaveCountAsync(0);
    }

    [Test]
    public async Task View_toggle_switches_to_list()
    {
        await Page.GotoAsync("/recipes");
        var cards = Page.Locator("[data-testid='recipe-card']");
        var gridCount = await cards.CountAsync();
        _ = await Assert.That(gridCount).IsGreaterThanOrEqualTo(2);

        await Page.Locator("[data-testid='view-list']").ClickAsync();

        // List rows reuse the grid cards' data-testid, and switching view fetches nothing, so the count holds.
        await Expect(cards).ToHaveCountAsync(gridCount);
    }

    [Test]
    public async Task Category_filter_narrows_results_and_keeps_other_categories_counted()
    {
        await Page.GotoAsync("/recipes");
        var filters = Page.Locator("#recipe-filters");

        await Page.RunAndWaitForResponseAsync(
            () => filters.Locator("label").GetByText("Beverage", new() { Exact = true }).ClickAsync(),
            response => response.Url.Contains("/api/recipes/search", StringComparison.Ordinal));

        // No beverage is rated, so nothing takes the spotlight. Drill-sideways counts keep every category's full
        // count while the diet counts narrow to the beverages.
        await Expect(Page.Locator("[data-testid='recipe-card']")).ToHaveCountAsync(4);
        await Expect(Page.Locator("[data-testid='recipe-spotlight']")).ToHaveCountAsync(0);
        await Expect(filters.Locator("li").Filter(new() { HasText = "Dinner" }).Locator(".kcc-q")).ToHaveTextAsync("5");
        await Expect(filters.Locator("li").Filter(new() { HasText = "Vegan" }).Locator(".kcc-q")).ToHaveTextAsync("3");
    }

    // Search is submit-based: the header emits `submit` only when its form is submitted, which fetches
    // /api/recipes/search.
    private async Task SearchForAsync(string query)
    {
        await Page.Locator("[data-testid='recipe-search-input']").FillAsync(query);
        await Page.RunAndWaitForResponseAsync(
            () => Page.Locator("[data-testid='recipe-search-submit']").ClickAsync(),
            response => response.Url.Contains("/api/recipes/search", StringComparison.Ordinal));
    }
}
