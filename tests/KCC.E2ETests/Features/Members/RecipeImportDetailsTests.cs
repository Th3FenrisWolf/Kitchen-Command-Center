using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.Members;

[NotInParallel(MemberSession.Serial)]
public class RecipeImportDetailsTests : BasePageTests
{
    private const string BreakfastKey = "164e3680-09c0-8a2d-b70c-7ffc738aed1c";

    private const string DinnerKey = "f08799ce-a590-8601-b7e3-409d91c099f9";

    private const string VeganKey = "b8563daf-0df0-8200-98ac-6753e87e59cc";

    private const string EasyKey = "9c164b66-296c-8468-a4a1-4deee3879fba";

    private ILocator Review => Page.GetByTestId("review-step");

    private ILocator ReviewDetails => Page.GetByTestId("review-details");

    private ILocator DetailsStep => Page.GetByTestId("details-step");

    [Test]
    public async Task OnePhoto_SuggestsBreakfastAndEasy_AndCopiesThePrintedPanel()
    {
        await ReadsAsync(1);

        await Expect(ReviewDetails).ToContainTextAsync("Breakfast");
        await Expect(ReviewDetails).ToContainTextAsync("Vegan");
        await Expect(ReviewDetails).ToContainTextAsync("Easy");
        await Expect(ReviewDetails).ToContainTextAsync("320");
        await Expect(Marks(ReviewCategory("Breakfast"), "suggested")).ToHaveCountAsync(1);
        await Expect(Marks(ReviewTag("Easy"), "suggested")).ToHaveCountAsync(1);
        await Expect(Marks(ReviewTag("Vegan"))).ToHaveCountAsync(0);
        await Expect(Marks(ReviewBlock("Difficulty"))).ToHaveCountAsync(0);
        await Expect(Marks(ReviewNutrition("Calories"))).ToHaveCountAsync(0);
        await Expect(Marks(ReviewNutrition("Fiber"), "calculated")).ToHaveCountAsync(1);
        await Expect(Marks(ReviewNutrition("Fiber"), "calculated")).ToContainTextAsync("Printed as 1.5 g");
        await Expect(Marks(ReviewDetails)).ToHaveCountAsync(3);

        await Page.GetByTestId("review-edit-details").ClickAsync();

        await Expect(DetailsStep).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("details-category")).ToHaveValueAsync(BreakfastKey);
        await Expect(Page.GetByTestId("details-tag-" + VeganKey)).ToBeCheckedAsync();
        await Expect(Page.GetByTestId("details-tag-" + EasyKey)).ToBeCheckedAsync();
        await Expect(Page.GetByTestId("details-nutrition-calories")).ToHaveValueAsync("320");
        await Expect(Page.GetByTestId("details-nutrition-fiberG")).ToHaveValueAsync("2");
        await Expect(Marks(FieldOf("details-category"), "suggested")).ToHaveCountAsync(1);
        await Expect(Marks(TagItem(EasyKey), "suggested")).ToHaveCountAsync(1);
        await Expect(Marks(TagItem(VeganKey))).ToHaveCountAsync(0);
        await Expect(Marks(Page.GetByTestId("details-difficulty").Locator("xpath=../.."))).ToHaveCountAsync(0);
        await Expect(Marks(FieldOf("details-nutrition-calories"))).ToHaveCountAsync(0);
        await Expect(Marks(FieldOf("details-nutrition-fiberG"), "calculated")).ToHaveCountAsync(1);
        await Expect(Marks(FieldOf("details-nutrition-fiberG"), "calculated")).ToContainTextAsync("Printed as 1.5 g");
        await Expect(Marks(DetailsStep)).ToHaveCountAsync(3);
        await Expect(Page.GetByTestId("details-proposed-tags")).ToHaveValueAsync(string.Empty);
        await Expect(Page.GetByTestId("details-proposed-category")).ToHaveValueAsync(string.Empty);
    }

    [Test]
    public async Task EditingASuggestedValue_ClearsItsMarkOnTheStepAndTheReview()
    {
        await ReadsAsync(1);
        await Page.GetByTestId("review-edit-details").ClickAsync();
        await Expect(DetailsStep).ToBeVisibleAsync();

        await Page.GetByTestId("details-nutrition-fiberG").FillAsync("3");
        await Page.GetByTestId("details-category").SelectOptionAsync(new SelectOptionValue { Label = "Dinner" });

        await Expect(Marks(FieldOf("details-nutrition-fiberG"))).ToHaveCountAsync(0);
        await Expect(Marks(FieldOf("details-category"))).ToHaveCountAsync(0);
        await Expect(Marks(DetailsStep, "calculated")).ToHaveCountAsync(0);
        await Expect(Marks(DetailsStep, "suggested")).ToHaveCountAsync(1);
        await Expect(Marks(TagItem(EasyKey), "suggested")).ToHaveCountAsync(1);
        await Page.GetByTestId("back-to-review").ClickAsync();

        await Expect(Review).ToBeVisibleAsync();
        await Expect(ReviewDetails).ToContainTextAsync("Dinner");
        await Expect(Marks(ReviewNutrition("Fiber"))).ToHaveCountAsync(0);
        await Expect(Marks(ReviewDetails, "calculated")).ToHaveCountAsync(0);
        await Expect(Marks(ReviewDetails, "suggested")).ToHaveCountAsync(1);
        await Expect(Marks(ReviewTag("Easy"), "suggested")).ToHaveCountAsync(1);

        await Page.GetByTestId("review-edit-details").ClickAsync();
        await TogglesTagAsync(EasyKey, false);
        await TogglesTagAsync(EasyKey, true);

        await Expect(Marks(TagItem(EasyKey))).ToHaveCountAsync(0);
        await Expect(Marks(DetailsStep)).ToHaveCountAsync(0);
        await Page.GetByTestId("back-to-review").ClickAsync();

        await Expect(Review).ToBeVisibleAsync();
        await Expect(ReviewTag("Easy")).ToHaveCountAsync(1);
        await Expect(Marks(ReviewTag("Easy"))).ToHaveCountAsync(0);
        await Expect(Marks(ReviewDetails)).ToHaveCountAsync(0);
    }

    [Test]
    public async Task FourPhotos_DivideTheWholeRecipePanelByTheEightPrintedServings()
    {
        await ReadsAsync(4);

        await Expect(Marks(ReviewDetails, "calculated")).ToHaveCountAsync(6);

        await Page.GetByTestId("review-edit-details").ClickAsync();

        await Expect(DetailsStep).ToBeVisibleAsync();
        await ExpectNutritionAsync(("calories", "250"), ("proteinG", "10"), ("carbsG", "20"), ("fatG", "8"), ("fiberG", "2"), ("sodiumMg", "1000"));
        await ExpectNutritionAsync(("saturatedFatG", string.Empty), ("sugarG", string.Empty));
        await Expect(Marks(DetailsStep, "calculated")).ToHaveCountAsync(6);
    }

    [Test]
    public async Task FivePhotos_LeaveEveryNutritionValueEmptyAndUnmarked()
    {
        await ReadsAsync(5);

        await Expect(ReviewDetails.Locator(".kcc-stats")).ToHaveCountAsync(0);
        await Expect(Marks(ReviewDetails, "calculated")).ToHaveCountAsync(0);

        await Page.GetByTestId("review-edit-details").ClickAsync();

        await Expect(DetailsStep).ToBeVisibleAsync();
        await ExpectNutritionAsync(
            ("calories", string.Empty),
            ("proteinG", string.Empty),
            ("carbsG", string.Empty),
            ("fatG", string.Empty),
            ("saturatedFatG", string.Empty),
            ("fiberG", string.Empty),
            ("sugarG", string.Empty),
            ("sodiumMg", string.Empty));
        foreach (var field in new[] { "calories", "proteinG", "carbsG", "fatG", "saturatedFatG", "fiberG", "sugarG", "sodiumMg" })
        {
            await Expect(Marks(FieldOf("details-nutrition-" + field))).ToHaveCountAsync(0);
        }

        await Expect(Marks(DetailsStep, "calculated")).ToHaveCountAsync(0);
    }

    [Test]
    public async Task TwoPhotosWithTheMatchAccepted_ShowNoCategory_AndChangeMatchThenKeepBringsItBack()
    {
        await PhotoImport.OpenOnCreateRecipeAsync(Page);
        await PhotoImport.AddFromDeviceAsync(Page, PhotoImport.FirstPage, PhotoImport.SecondPage);
        await Page.GetByTestId("read-photos").ClickAsync();
        await Page.GetByTestId("match-accept").ClickAsync();

        await Expect(Review).ToBeVisibleAsync();
        await Expect(ReviewDetails).Not.ToContainTextAsync("Breakfast");
        await Expect(ReviewDetails).Not.ToContainTextAsync("Category");
        await Expect(ReviewDetails).ToContainTextAsync("Vegan");

        await Review.GetByRole(AriaRole.Button, new() { Name = "Change", Exact = true }).ClickAsync();
        await Page.GetByTestId("match-keep").ClickAsync();

        await Expect(Review).ToBeVisibleAsync();
        await Expect(ReviewDetails).ToContainTextAsync("Breakfast");
        await Expect(Marks(ReviewDetails, "suggested")).ToHaveCountAsync(2);
    }

    private static ILocator Marks(ILocator scope, string? kind = null) =>
        scope.Locator(kind is null ? "[data-testid='value-mark']" : $"[data-testid='value-mark'][data-kind='{kind}']");

    private ILocator ReviewCategory(string name) => ReviewDetails.Locator("div.mt-rule-1").Filter(new() { HasText = name });

    private ILocator ReviewBlock(string label) => ReviewDetails.Locator("div.mt-rule-1").Filter(new() { HasText = label });

    private ILocator ReviewTag(string name) => ReviewDetails.Locator("li").Filter(new() { HasText = name });

    private ILocator ReviewNutrition(string label) => ReviewDetails.Locator(".kcc-stats > div").Filter(new() { HasText = label });

    private ILocator FieldOf(string testId) =>
        DetailsStep.Locator("div:has(> label.kcc-lbl)").Filter(new() { Has = Page.GetByTestId(testId) });

    private ILocator TagItem(string key) => Page.GetByTestId("details-tag-" + key).Locator("xpath=ancestor::li");

    private async Task TogglesTagAsync(string key, bool on)
    {
        var box = Page.GetByTestId("details-tag-" + key);
        var id = await box.GetAttributeAsync("id");
        await Page.Locator($"label[for='{id}']:not(.kcc-box)").ClickAsync();
        if (on)
        {
            await Expect(box).ToBeCheckedAsync();
        }
        else
        {
            await Expect(box).Not.ToBeCheckedAsync();
        }
    }

    private async Task ReadsAsync(int photos)
    {
        await PhotoImport.OpenOnCreateRecipeAsync(Page);
        await PhotoImport.AddFromDeviceAsync(Page, PhotoImport.FirstPage);
        for (var count = 2; count <= photos; count++)
        {
            await Page.GetByTestId("photo-input").SetInputFilesAsync(PhotoImport.FirstPage);
            await Expect(Page.GetByTestId("photo-tile")).ToHaveCountAsync(count);
        }

        await Page.GetByTestId("read-photos").ClickAsync();
        await Expect(Review).ToBeVisibleAsync();
    }

    private async Task ExpectNutritionAsync(params (string Field, string Value)[] values)
    {
        foreach (var (field, value) in values)
        {
            await Expect(Page.GetByTestId("details-nutrition-" + field)).ToHaveValueAsync(value);
        }
    }
}
