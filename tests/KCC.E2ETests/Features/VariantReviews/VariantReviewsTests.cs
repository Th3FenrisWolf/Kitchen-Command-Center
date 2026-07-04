using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.VariantReviews;

// TUnit runs tests in parallel by default; concurrent navigations starve the single-threaded
// Vite dev server and every nav times out. [NotInParallel] serializes them (matches the
// AdminHomePageMiddlewareTests / CookModeTests precedent in this repo).
[NotInParallel]
public class VariantReviewsTests : BasePageTests
{
    [Test]
    public async Task Anonymous_SeesLogInToReviewPrompt()
    {
        _ = await GoToFirstVariant();

        // For anonymous users the review editor and cook-note draft are not rendered
        // (both textareas are gated on isAuthenticated), so the page has no textareas.
        await Expect(Page.Locator("textarea")).ToHaveCountAsync(0);

        // The ratings/reviews section still renders its heading, which contains "review".
        await Expect(Page.GetByText("review", new() { Exact = false }).First).ToBeVisibleAsync();
    }

    [Test]
    public async Task LoggedInMember_CanSubmitEditAndDeleteAReview()
    {
        await MemberSession.SignInAsync(Page);
        _ = await GoToFirstVariant();

        // The interactive StarRating is a slider whose per-star half/whole hit areas carry
        // data-value (0.5 .. 5); the review editor exposes data-testid hooks so the flow is
        // independent of the (admin-seeded) Submit/Delete resource-string text.
        var reviewInput = Page.Locator("[data-testid='review-input']");
        var submit = Page.Locator("[data-testid='submit-review']");

        // Scope assertions to the reviews list: in development the MiniProfiler widget echoes the
        // executed INSERT/UPDATE (with the review text as a parameter) into a <code> block on the
        // page, so a page-wide GetByText(reviewText) matches two elements and trips strict mode.
        var reviewsList = Page.Locator("[data-testid='reviews-list']");

        // Submit: pick a rating + text, then submit.
        await Page.Locator("[data-value='4']").First.ClickAsync();
        await reviewInput.FillAsync("E2E review - tasty");
        await submit.ClickAsync();
        await Expect(reviewsList.GetByText("E2E review - tasty")).ToBeVisibleAsync();

        // Edit: change the text and resubmit (upsert edits in place - still one review).
        await reviewInput.FillAsync("E2E review - edited");
        await submit.ClickAsync();
        await Expect(reviewsList.GetByText("E2E review - edited")).ToBeVisibleAsync();

        // Delete: remove my review; the submitted text disappears from the list.
        await Page.Locator("[data-testid='delete-review']").ClickAsync();
        await Expect(reviewsList.GetByText("E2E review - edited")).ToHaveCountAsync(0);
    }

    [Test]
    public async Task LoggedInMember_CanSubmitAHalfStarReview()
    {
        await MemberSession.SignInAsync(Page);
        _ = await GoToFirstVariant();

        var submit = Page.Locator("[data-testid='submit-review']");
        var reviewsList = Page.Locator("[data-testid='reviews-list']");

        // Click the left half of the 4th star -> 3.5, add text, submit.
        await Page.Locator("[data-value='3.5']").First.ClickAsync();
        await Page.Locator("[data-testid='review-input']").FillAsync("E2E half-star review");
        await submit.ClickAsync();
        await Expect(reviewsList.GetByText("E2E half-star review")).ToBeVisibleAsync();

        // The stored 3.5 round-trips: the review's readonly stars show a half at position 4.
        await Expect(reviewsList.Locator("[data-star='4'][data-state='half']").First).ToBeVisibleAsync();

        // Clean up so the run is repeatable.
        await Page.Locator("[data-testid='delete-review']").ClickAsync();
        await Expect(reviewsList.GetByText("E2E half-star review")).ToHaveCountAsync(0);
    }

    private async Task<string> GoToFirstVariant()
    {
        _ = await RecipeNavigation.GoToFirstRecipeAsync(Page);
        return await RecipeNavigation.GoToFirstVariantAsync(Page);
    }
}
