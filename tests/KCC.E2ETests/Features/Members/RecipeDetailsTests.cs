using System.Text.Json;
using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.Members;

[NotInParallel(MemberSession.Serial)]
public class RecipeDetailsTests : BasePageTests
{
    private const string DinnerKey = "f08799ce-a590-8601-b7e3-409d91c099f9";

    private const string GlutenFreeKey = "bd3a5a4d-c8a5-8fad-a9a3-1f552a65380a";

    private const string VeganKey = "b8563daf-0df0-8200-98ac-6753e87e59cc";

    private ILocator Next => Page.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true });

    private ILocator Review => Page.GetByTestId("review-step");

    private ILocator DetailsStep => Page.GetByTestId("details-step");

    [Test]
    public async Task TypedDetails_AreListedOnTheReview_AndTheSubmissionSucceeds()
    {
        await TypesARecipeToTheDetailsStepAsync();

        await Page.GetByTestId("details-category").SelectOptionAsync(new SelectOptionValue { Label = "Dinner" });
        await ChecksTagAsync(GlutenFreeKey);
        await ChecksTagAsync("fefc672b-221c-8cc7-8b7f-50cbee11da2a");
        await Page.GetByTestId("details-difficulty").GetByText("Medium", new() { Exact = true }).ClickAsync();
        await Page.GetByTestId("details-nutrition-calories").FillAsync("450");
        await Next.ClickAsync();

        var details = Page.GetByTestId("review-details");
        await Expect(Review).ToBeVisibleAsync();
        await Expect(details).ToContainTextAsync("Dinner");
        await Expect(details).ToContainTextAsync("Gluten-Free");
        await Expect(details).ToContainTextAsync("Spicy");
        await Expect(details).ToContainTextAsync("Medium");
        await Expect(details).ToContainTextAsync("450");

        await Page.GetByTestId("wizard-submit").ClickAsync();

        await Expect(Page.GetByTestId("wizard-submitted")).ToBeVisibleAsync();
    }

    [Test]
    public async Task AnEmptyDetailsStep_ReachesTheReview_WhichSaysNoDetailsWereGiven()
    {
        await TypesARecipeToTheDetailsStepAsync();

        await Next.ClickAsync();

        await Expect(Review).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("review-details")).ToContainTextAsync("No details given.");
    }

    [Test]
    public async Task AddVariant_ShowsNoCategory()
    {
        await MemberSession.SignInAsync(Page);
        _ = await Page.GotoAsync(PhotoImport.RecipePath);
        await Page.GetByRole(AriaRole.Link, new() { Name = "Add Variant", Exact = true }).First.ClickAsync();
        await Page.GetByTestId("wizard-start-typing").ClickAsync();
        await Page.GetByTestId("wizard-variant-name").FillAsync("Details Variant");
        await Next.ClickAsync();
        await Page.GetByTestId("wizard-ingredient-name").First.FillAsync("Eggs");
        await Next.ClickAsync();
        await Page.GetByTestId("wizard-instruction-text").First.FillAsync("Scramble gently.");
        await Next.ClickAsync();

        await Expect(DetailsStep).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("details-category")).ToHaveCountAsync(0);
        await Expect(Page.GetByTestId("details-proposed-category")).ToHaveCountAsync(0);
        await Expect(Page.GetByTestId("details-tag-" + GlutenFreeKey)).ToHaveCountAsync(1);
        await Next.ClickAsync();
        await Expect(Review).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("review-details")).Not.ToContainTextAsync("Category");
    }

    [Test]
    public async Task EditOnTheReviewsDetails_OpensTheStep_AndReturnsStraightToTheReview()
    {
        await TypesARecipeToTheDetailsStepAsync();
        await Next.ClickAsync();
        await Expect(Review).ToBeVisibleAsync();

        await Page.GetByTestId("review-edit-details").ClickAsync();

        await Expect(DetailsStep).ToBeVisibleAsync();
        await Expect(Review).ToHaveCountAsync(0);
        await ChecksTagAsync(VeganKey);
        await Page.GetByTestId("back-to-review").ClickAsync();

        await Expect(Review).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("review-details")).ToContainTextAsync("Vegan");
    }

    [Test]
    public async Task AFractionalCalorieCount_ShowsTheErrorAndKeepsTheMemberOnTheStep()
    {
        await TypesARecipeToTheDetailsStepAsync();
        var calories = Page.GetByTestId("details-nutrition-calories");

        await calories.FillAsync("1.5");

        await Expect(calories).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(DetailsStep).ToContainTextAsync("Enter a whole number from 0 to 10000.");
        await Expect(Next).ToBeDisabledAsync();
        await Expect(DetailsStep).ToBeVisibleAsync();
        await Expect(Review).ToHaveCountAsync(0);
    }

    [Test]
    public async Task ADetailsChangedAnswer_ReturnsToTheDetails_WithTheWellAndTheOtherValuesKept()
    {
        await TypesARecipeToTheDetailsStepAsync();
        await Page.GetByTestId("details-category").SelectOptionAsync(new SelectOptionValue { Label = "Dinner" });
        await ChecksTagAsync(GlutenFreeKey);
        await ChecksTagAsync(VeganKey);
        await Page.GetByTestId("details-nutrition-calories").FillAsync("450");
        await Next.ClickAsync();
        await Expect(Review).ToBeVisibleAsync();

        var refusal = JsonSerializer.Serialize(new
        {
            error = "The categories or tags have changed. Choose again.",
            code = "detailsChanged",
            choices = new
            {
                categories = new[] { new { key = DinnerKey, name = "Dinner" } },
                tags = new[]
                {
                    new { key = GlutenFreeKey, name = "Gluten-Free", kind = "Diet" },
                    new { key = VeganKey, name = "Vegan", kind = "Diet" },
                },
            },
        });
        await Page.RouteAsync("**/api/recipes", async route =>
        {
            if (route.Request.Method == "POST")
            {
                await route.FulfillAsync(new() { Status = 400, ContentType = "application/json", Body = refusal });
            }
            else
            {
                await route.FallbackAsync();
            }
        });

        await Page.GetByTestId("wizard-submit").ClickAsync();

        await Expect(DetailsStep).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("details-changed")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("details-category")).ToHaveValueAsync(DinnerKey);
        await Expect(Page.GetByTestId("details-tag-" + GlutenFreeKey)).ToBeCheckedAsync();
        await Expect(Page.GetByTestId("details-tag-" + VeganKey)).ToBeCheckedAsync();
        await Expect(Page.GetByTestId("details-nutrition-calories")).ToHaveValueAsync("450");
        await Expect(Page.GetByTestId("back-to-review")).ToBeVisibleAsync();
    }

    [Test]
    public async Task ProposedTagsAndCategory_AreShownOnTheReview_AndSubmitted()
    {
        await TypesARecipeToTheDetailsStepAsync();

        await Page.GetByTestId("details-proposed-tags").FillAsync("Mexican");
        await Page.GetByTestId("details-proposed-category").FillAsync("Sides");
        await Next.ClickAsync();

        var details = Page.GetByTestId("review-details");
        await Expect(details).ToContainTextAsync("Mexican");
        await Expect(details).ToContainTextAsync("Sides");

        await Page.GetByTestId("wizard-submit").ClickAsync();

        await Expect(Page.GetByTestId("wizard-submitted")).ToBeVisibleAsync();
    }

    private async Task ChecksTagAsync(string key)
    {
        var box = Page.GetByTestId("details-tag-" + key);
        var id = await box.GetAttributeAsync("id");
        await Page.Locator($"label[for='{id}']:not(.kcc-box)").ClickAsync();
        await Expect(box).ToBeCheckedAsync();
    }

    private async Task TypesARecipeToTheDetailsStepAsync()
    {
        await MemberSession.SignInAsync(Page);
        _ = await Page.GotoAsync("/recipes/create-recipe");

        await Page.GetByTestId("wizard-start-typing").ClickAsync();
        await Page.GetByTestId("wizard-recipe-name").FillAsync($"Details {Guid.NewGuid():N}"[..16]);
        await Page.GetByTestId("wizard-recipe-description").FillAsync("Typed by the E2E suite.");
        await Next.ClickAsync();
        await Page.GetByTestId("wizard-variant-name").FillAsync("First Try");
        await Next.ClickAsync();
        await Page.GetByTestId("wizard-ingredient-name").First.FillAsync("Eggs");
        await Next.ClickAsync();
        await Page.GetByTestId("wizard-instruction-text").First.FillAsync("Scramble gently.");
        await Next.ClickAsync();

        await Expect(DetailsStep).ToBeVisibleAsync();
    }
}
