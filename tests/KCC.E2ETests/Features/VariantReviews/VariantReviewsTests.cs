using System.Text.RegularExpressions;
using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.VariantReviews;

[NotInParallel(MemberSession.Serial)]
public class VariantReviewsTests : BasePageTests
{
    private const string ReviewedVariant = "/recipes/spicy-ramen-flight/chili-oil-shoyu";

    private const int SeededReviews = 3;

    [Test]
    public async Task Anonymous_SeesLogInToReviewPrompt()
    {
        _ = await Page.GotoAsync(MemberTestVariant.Path);

        // For anonymous users the review editor and cook-note draft are not rendered
        // (both textareas are gated on isAuthenticated), so the page has no textareas.
        await Expect(Page.Locator("textarea")).ToHaveCountAsync(0);

        await Expect(Page.GetByText("review", new() { Exact = false }).First).ToBeVisibleAsync();
    }

    [Test]
    public async Task LoadedReviews_TurnOver_AndTheFormTurnsAgainstTheLastOne()
    {
        await MemberSession.SignInAsync(Page);
        _ = await Page.GotoAsync(ReviewedVariant);

        var reviews = Page.Locator("[data-testid='reviews-list'] > li.kcc-slip");
        await Expect(reviews).ToHaveCountAsync(SeededReviews);
        for (var i = 0; i < SeededReviews; i++)
        {
            await Expect(reviews.Nth(i)).ToHaveClassAsync(new Regex(@"\bkcc-slip--flip\b"));
        }

        var form = Page.Locator("main .kcc-slip", new() { Has = Page.GetByTestId("review-input") });
        var lastTurn = await Piles.TurnAsync(reviews.Last);
        var formTurn = await Piles.TurnAsync(form);

        _ = await Assert.That(lastTurn).IsNotEqualTo(0);
        _ = await Assert.That(formTurn).IsEqualTo(-lastTurn);
    }

    [Test]
    public async Task LoggedInMember_CanSubmitEditAndDeleteAReview()
    {
        await MemberSession.SignInAsync(Page);
        _ = await Page.GotoAsync(MemberTestVariant.Path);

        // The interactive StarRating is a slider whose per-star half/whole hit areas carry data-value (0.5 .. 5);
        // the review editor exposes data-testid hooks so the flow is independent of the dictionary's text.
        var reviewInput = Page.Locator("[data-testid='review-input']");
        var submit = Page.Locator("[data-testid='submit-review']");
        var reviewsList = Page.Locator("[data-testid='reviews-list']");

        try
        {
            await Page.Locator("[data-value='4']").First.ClickAsync();
            await reviewInput.FillAsync("E2E review - tasty");
            await submit.ClickAsync();
            await Expect(reviewsList.GetByText("E2E review - tasty")).ToBeVisibleAsync();

            var reviewSheet = reviewsList.Locator("li.kcc-slip", new() { HasText = "E2E review - tasty" });
            await Expect(reviewSheet.Locator(".kcc-blank")).ToHaveCountAsync(1);
            await Expect(reviewSheet).ToHaveClassAsync(new Regex(@"\bkcc-slip--wide\b"));

            await reviewInput.FillAsync("E2E review - edited");
            await submit.ClickAsync();
            await Expect(reviewsList.GetByText("E2E review - edited")).ToBeVisibleAsync();

            await Page.Locator("[data-testid='delete-review']").ClickAsync();
            await Expect(reviewsList.GetByText("E2E review - edited")).ToHaveCountAsync(0);
        }
        catch
        {
            await MemberTestVariant.DeleteReviewIfPresentAsync(Page);
            throw;
        }
    }

    [Test]
    public async Task LoggedInMember_CanSubmitAHalfStarReview()
    {
        await MemberSession.SignInAsync(Page);
        _ = await Page.GotoAsync(MemberTestVariant.Path);

        var submit = Page.Locator("[data-testid='submit-review']");
        var reviewsList = Page.Locator("[data-testid='reviews-list']");

        try
        {
            await Page.Locator("[data-value='3.5']").First.ClickAsync();
            await Page.Locator("[data-testid='review-input']").FillAsync("E2E half-star review");
            await submit.ClickAsync();
            await Expect(reviewsList.GetByText("E2E half-star review")).ToBeVisibleAsync();

            await Expect(reviewsList.Locator("[data-star='4'][data-state='half']").First).ToBeVisibleAsync();

            await Page.Locator("[data-testid='delete-review']").ClickAsync();
            await Expect(reviewsList.GetByText("E2E half-star review")).ToHaveCountAsync(0);
        }
        catch
        {
            await MemberTestVariant.DeleteReviewIfPresentAsync(Page);
            throw;
        }
    }
}
