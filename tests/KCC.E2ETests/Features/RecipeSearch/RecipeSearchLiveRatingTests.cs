using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.RecipeSearch;

// A review reaches the search index within seconds: the variant's recipe, unrated before, becomes the Top Rated
// spotlight for a search that finds it. Searching keeps the listing's own spotlight, which other suites assert,
// untouched.
[NotInParallel(MemberSession.Serial)]
public class RecipeSearchLiveRatingTests : BasePageTests
{
    private const string ReviewText = "E2E rating - live reindex";

    [Test]
    public async Task ReviewWrite_MakesTheRecipeTheSearchSpotlight()
    {
        await MemberSession.SignInAsync(Page);
        var spotlight = Page.Locator("[data-testid='recipe-spotlight']");

        await SearchForMatchaAsync();
        await Expect(Page.Locator($"[data-testid='recipe-card'][data-recipe-name='{MemberTestVariant.RecipeName}']")).ToHaveCountAsync(1);
        await Expect(spotlight).ToHaveCountAsync(0);

        _ = await Page.GotoAsync(MemberTestVariant.Path);
        await Page.Locator("[data-value='5']").First.ClickAsync();
        await Page.Locator("[data-testid='review-input']").FillAsync(ReviewText);
        await Page.Locator("[data-testid='submit-review']").ClickAsync();

        try
        {
            await Expect(Page.Locator("[data-testid='reviews-list']").GetByText(ReviewText)).ToBeVisibleAsync();

            // The index rebuilds about two seconds after the review. A search page shows the listing's own spotlight
            // until it has read the search response, so only this recipe's spotlight says the search has caught up.
            var reviewedSpotlight = Page.Locator(
                $"[data-testid='recipe-spotlight'][data-recipe-name='{MemberTestVariant.RecipeName}']");
            for (var attempt = 0; attempt < 20 && !await reviewedSpotlight.IsVisibleAsync(); attempt++)
            {
                await Page.WaitForTimeoutAsync(1000);
                await SearchForMatchaAsync();
            }

            await Expect(spotlight).ToHaveAttributeAsync("data-recipe-name", MemberTestVariant.RecipeName);
            await Expect(spotlight.Locator("[data-testid='recipe-card-rating']")).ToBeVisibleAsync();

            _ = await Page.GotoAsync(MemberTestVariant.Path);
            await Page.Locator("[data-testid='delete-review']").ClickAsync();
            await Expect(Page.Locator("[data-testid='reviews-list']").GetByText(ReviewText)).ToHaveCountAsync(0);
        }
        catch
        {
            await MemberTestVariant.DeleteReviewIfPresentAsync(Page);
            throw;
        }
    }

    private async Task SearchForMatchaAsync()
    {
        _ = await Page.GotoAsync("/recipes");
        await Page.Locator("[data-testid='recipe-search-input']").FillAsync("matcha");
        await Page.RunAndWaitForResponseAsync(
            () => Page.Locator("[data-testid='recipe-search-submit']").ClickAsync(),
            response => response.Url.Contains("/api/recipes/search", StringComparison.Ordinal));
    }
}
