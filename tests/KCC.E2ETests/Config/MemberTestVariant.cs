using Microsoft.Playwright;

namespace KCC.E2ETests.Config;

// The seeded variant the member suites review, note and mark cooked. No read-only suite asserts on it, it has no
// reviews of its own, and its recipe sorts after Legendary Lasagna, so even a five-star review cannot take the
// listing's Top Rated spotlight.
public static class MemberTestVariant
{
    public const string RecipeName = "Matcha Panna Cotta";

    public const string Path = "/recipes/matcha-panna-cotta/green-tea-set";

    // The page loads the member's review after it renders, so the delete button is there only once that has finished.
    private const float ReviewLoadTimeout = 5000;

    // A review left on this shared variant by a failed test would break the next suite's starting state.
    public static async Task DeleteReviewIfPresentAsync(IPage page)
    {
        try
        {
            _ = await page.GotoAsync(Path);
            var delete = page.Locator("[data-testid='delete-review']");
            await delete.ClickAsync(new() { Timeout = ReviewLoadTimeout });
            await delete.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = ReviewLoadTimeout });
        }
        catch (Exception exception) when (exception is PlaywrightException or TimeoutException)
        {
            // A review that is not there surfaces as a timeout, which is not a PlaywrightException. Either way the
            // failure that brought us here is the one worth reporting.
        }
    }
}
