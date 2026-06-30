using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.VariantReviews;

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
        _ = await Assert.That(MemberSession.HasCredentials)
            .IsTrue()
            .Because("Set KCC_E2E_MEMBER_USERNAME/PASSWORD to a seeded confirmed member to run this flow.");

        await MemberSession.SignInAsync(Page);
        _ = await GoToFirstVariant();

        // The interactive StarRating renders one button per star with data-star (1-indexed);
        // the review editor exposes data-testid hooks so the flow is independent of the
        // (admin-seeded) Submit/Delete resource-string text.
        var reviewInput = Page.Locator("[data-testid='review-input']");
        var submit = Page.Locator("[data-testid='submit-review']");

        // Submit: pick a rating + text, then submit.
        await Page.Locator("button[data-star='4']").First.ClickAsync();
        await reviewInput.FillAsync("E2E review - tasty");
        await submit.ClickAsync();
        await Expect(Page.GetByText("E2E review - tasty")).ToBeVisibleAsync();

        // Edit: change the text and resubmit (upsert edits in place - still one review).
        await reviewInput.FillAsync("E2E review - edited");
        await submit.ClickAsync();
        await Expect(Page.GetByText("E2E review - edited")).ToBeVisibleAsync();

        // Delete: remove my review; the submitted text disappears from the list.
        await Page.Locator("[data-testid='delete-review']").ClickAsync();
        await Expect(Page.GetByText("E2E review - edited")).ToHaveCountAsync(0);
    }

    private async Task<string> GoToFirstVariant()
    {
        _ = await RecipeNavigation.GoToFirstRecipeAsync(Page);
        return await RecipeNavigation.GoToFirstVariantAsync(Page);
    }
}
