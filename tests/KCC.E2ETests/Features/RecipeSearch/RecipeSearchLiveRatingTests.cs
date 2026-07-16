using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.RecipeSearch;

// [NotInParallel]: TUnit parallelizes by default; concurrent navigations starve the single-threaded
// Vite dev server. Matches RecipeSearchTests / VariantReviewsTests.
[NotInParallel]
public class RecipeSearchLiveRatingTests : BasePageTests
{
    private const string ReviewText = "E2E rating - live reindex";

    // The dev page holds an open Vite HMR WebSocket, so `load` never settles; the listing is
    // server-rendered, so DOMContentLoaded already exposes the cards/spotlight.
    private async Task GotoSearchAsync() =>
        _ = await Page.GotoAsync("/recipes", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

    [Test]
    public async Task ReviewWrite_MakesRecipeAppearAsLiveTopRatedSpotlight()
    {
        await MemberSession.SignInAsync(Page);

        // Pick the first listed recipe and remember its name. With the unrated seed it renders as a grid
        // card; once rated it becomes the top-rated spotlight (which the grid intentionally excludes).
        await GotoSearchAsync();
        var firstCard = Page.Locator("[data-testid='recipe-card']").First;
        await Expect(firstCard).ToBeVisibleAsync();
        var recipeName = await firstCard.GetAttributeAsync("data-recipe-name");
        _ = await Assert.That(recipeName).IsNotNull();

        // Open its first variant and submit a 5-star review (makes it the single highest-rated recipe).
        await firstCard.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var variantUrl = await RecipeNavigation.GoToFirstVariantAsync(Page);
        await Page.Locator("[data-value='5']").First.ClickAsync();
        await Page.Locator("[data-testid='review-input']").FillAsync(ReviewText);
        await Page.Locator("[data-testid='submit-review']").ClickAsync();
        await Expect(Page.Locator("[data-testid='reviews-list']").GetByText(ReviewText)).ToBeVisibleAsync();

        try
        {
            // The review write enqueues a Lucene reindex processed asynchronously by the queue worker.
            // Reload /recipes until the recipe surfaces as the top-rated spotlight (each reload re-runs
            // search server-side against the live index). Bounded ~20s; not a fixed sleep.
            var spotlight = Page.Locator("[data-testid='recipe-spotlight']");
            for (var attempt = 0; attempt < 20; attempt++)
            {
                await GotoSearchAsync();
                if (await spotlight.IsVisibleAsync()
                    && await spotlight.GetAttributeAsync("data-recipe-name") == recipeName)
                {
                    break;
                }

                await Page.WaitForTimeoutAsync(1000);
            }

            // The spotlight only renders for a genuinely-rated recipe, so its presence for THIS recipe
            // (with the rating hook) proves the review write refreshed the index live — no rebuild.
            await Expect(spotlight).ToBeVisibleAsync();
            await Expect(spotlight).ToHaveAttributeAsync("data-recipe-name", recipeName!);
            await Expect(spotlight.Locator("[data-testid='recipe-card-rating']")).ToBeVisibleAsync();
        }
        finally
        {
            // Clean up so the run is repeatable; the delete-reindex reverts the recipe to unrated.
            _ = await Page.GotoAsync(variantUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await Page.Locator("[data-testid='delete-review']").ClickAsync();
            await Expect(Page.Locator("[data-testid='reviews-list']").GetByText(ReviewText)).ToHaveCountAsync(0);
        }
    }
}
