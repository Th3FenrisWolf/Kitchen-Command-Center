using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.Members;

[NotInParallel(MemberSession.Serial)]
public class RecipeWizardReviewTests : BasePageTests
{
    private const string UnclearMark = "[data-testid='value-mark'][data-kind='unclear']";

    private ILocator Review => Page.GetByTestId("review-step");

    private ILocator ReviewHeading => Heading("Review & Submit");

    private ILocator BackToReview => Page.GetByTestId("back-to-review");

    private ILocator ReadAgainWarning => Page.GetByTestId("read-again-warning");

    [Test]
    public async Task ATypedRecipe_OpensItsIngredientsFromTheReview_AndReturnsStraightThereWithTheChange()
    {
        await TypesARecipeToTheReviewAsync();

        await Page.GetByTestId("review-edit-ingredients").ClickAsync();

        await Expect(Heading("Ingredients")).ToBeFocusedAsync();
        await Expect(Review).ToHaveCountAsync(0);
        await Page.GetByTestId("wizard-ingredient-name").First.FillAsync("Duck eggs");

        await BackToReview.ClickAsync();

        await Expect(Review).ToBeVisibleAsync();
        await Expect(ReviewHeading).ToBeFocusedAsync();
        await Expect(Review).ToContainTextAsync("Duck eggs");
    }

    [Test]
    public async Task AnImportedVariant_KeepsItsEdits_AndAsksBeforeEveryReadThatWouldReplaceThem()
    {
        await PhotoImport.OpenOnAddVariantAsync(Page);
        await PhotoImport.AddFromDeviceAsync(Page, PhotoImport.FirstPage);
        await Page.GetByTestId("read-photos").ClickAsync();
        await Expect(Review).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("match-step")).ToHaveCountAsync(0);
        await Expect(Review.Locator(UnclearMark)).ToHaveCountAsync(1);

        await Page.GetByTestId("review-edit-ingredients").ClickAsync();
        await Expect(Heading("Ingredients")).ToBeFocusedAsync();
        await ViewsAPhotoFromTheStripAsync();
        var buttermilk = Page.GetByTestId("wizard-ingredient-name").Nth(2);
        await Expect(buttermilk).ToHaveValueAsync("Buttermilk");
        await Expect(Page.Locator(UnclearMark)).ToHaveCountAsync(1);
        await buttermilk.FillAsync("Buttermilk, well shaken");
        await Expect(Page.Locator(UnclearMark)).ToHaveCountAsync(0);
        await BackToReview.ClickAsync();
        await Expect(ReviewHeading).ToBeFocusedAsync();
        await Expect(Review).ToContainTextAsync("Buttermilk, well shaken");
        await Expect(Review.Locator(UnclearMark)).ToHaveCountAsync(0);

        await Page.GetByTestId("read-again").ClickAsync();
        await Expect(ReadAgainWarning).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("reading-state")).ToHaveCountAsync(0);
        await CancelsTheWarningBackToAsync(Page.GetByTestId("read-again"));

        await Page.GetByTestId("change-photos").ClickAsync();
        await Expect(Heading("Photos")).ToBeFocusedAsync();
        await Expect(Page.GetByTestId("photo-tile")).ToHaveCountAsync(1);
        await BackToReview.ClickAsync();
        await Expect(ReviewHeading).ToBeFocusedAsync();
        await Expect(Review).ToContainTextAsync("Buttermilk, well shaken");

        await Page.GetByTestId("change-photos").ClickAsync();
        await Page.GetByTestId("read-photos").ClickAsync();
        await Expect(ReadAgainWarning).ToBeVisibleAsync();
        await Page.GetByTestId("photo-remove").ClickAsync();
        await Expect(Page.GetByTestId("read-photos")).ToBeDisabledAsync();
        await CancelsTheWarningBackToAsync(Heading("Photos"));
        await PhotoImport.AddFromDeviceAsync(Page, PhotoImport.FirstPage);
        await Page.GetByTestId("photo-input").SetInputFilesAsync(PhotoImport.SecondPage);
        await Expect(Page.GetByTestId("photo-tile")).ToHaveCountAsync(2);
        await Page.GetByTestId("read-photos").ClickAsync();
        await Expect(ReadAgainWarning).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("reading-state")).ToHaveCountAsync(0);
        await CancelsTheWarningBackToAsync(Page.GetByTestId("read-photos"));
        await Page.GetByTestId("read-photos").ClickAsync();
        await Expect(ReadAgainWarning).ToBeVisibleAsync();

        await Page.GetByTestId("read-again-confirm").ClickAsync();

        await Expect(Review).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("match-step")).ToHaveCountAsync(0);
        await Expect(Review).Not.ToContainTextAsync("Buttermilk, well shaken");
        await Expect(Review.Locator(UnclearMark)).ToHaveCountAsync(1);
        await Expect(Page.GetByTestId("wizard-photos").GetByTestId("photo-tile")).ToHaveCountAsync(2);
    }

    private ILocator Heading(string name) => Page.GetByRole(AriaRole.Heading, new() { Name = name, Exact = true });

    private async Task TypesARecipeToTheReviewAsync()
    {
        await MemberSession.SignInAsync(Page);
        _ = await Page.GotoAsync("/recipes/create-recipe");
        var next = Page.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true });

        await Page.GetByTestId("wizard-start-typing").ClickAsync();
        await Page.GetByTestId("wizard-recipe-name").FillAsync("Review Links");
        await Page.GetByTestId("wizard-recipe-description").FillAsync("Typed by the E2E suite.");
        await next.ClickAsync();
        await Page.GetByTestId("wizard-variant-name").FillAsync("First Try");
        await next.ClickAsync();
        await Page.GetByTestId("wizard-ingredient-name").First.FillAsync("Eggs");
        await next.ClickAsync();
        await Page.GetByTestId("wizard-instruction-text").First.FillAsync("Scramble gently.");
        await next.ClickAsync();

        await Expect(Review).ToBeVisibleAsync();
        await Expect(Review).ToContainTextAsync("Eggs");
    }

    private async Task CancelsTheWarningBackToAsync(ILocator asker)
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).ClickAsync();

        await Expect(ReadAgainWarning).ToHaveCountAsync(0);
        await Expect(asker).ToBeFocusedAsync();
    }

    private async Task ViewsAPhotoFromTheStripAsync()
    {
        var viewer = Page.GetByTestId("photo-viewer");

        await Page.GetByTestId("wizard-photos").GetByTestId("photo-tile").First.ClickAsync();
        await Expect(viewer).ToBeVisibleAsync();
        _ = await Assert.That(string.Join(Environment.NewLine, await Piles.OverlayFaultsAsync(viewer))).IsEmpty();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(viewer).ToHaveCountAsync(0);
    }
}
