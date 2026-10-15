using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.Members;

[NotInParallel(MemberSession.Serial)]
public class RecipeImportMatchTests : BasePageTests
{
    private const string FirstRecipeName = "Avocado Toast Supreme";

    private const string FirstRecipeFirstVariant = "The Basic";

    private const string TacosFirstVariant = "Slow Beef Barbacoa";

    private const string StandInVariantName = "Honey Butter";

    private ILocator MatchStep => Page.GetByTestId("match-step");

    private ILocator Review => Page.GetByTestId("review-step");

    private ILocator PossibleDuplicate => Page.GetByTestId("possible-duplicate");

    private ILocator SubmitError => Page.GetByTestId("submit-error");

    private ILocator ChangeMatch => Review.GetByRole(AriaRole.Button, new() { Name = "Change", Exact = true });

    [Test]
    public async Task TwoPhotosOnCreateRecipe_ProposeTheFirstRecipe_AndAcceptingAddsAPendingVariantToIt()
    {
        await PhotoImport.OpenOnCreateRecipeAsync(Page);
        await PhotoImport.AddFromDeviceAsync(Page, PhotoImport.FirstPage, PhotoImport.SecondPage);

        await Page.GetByTestId("read-photos").ClickAsync();

        await Expect(MatchStep).ToBeVisibleAsync();
        await Expect(Heading($"Is this a new way to make {FirstRecipeName}?")).ToBeFocusedAsync();
        await Expect(Page.GetByTestId("wizard-photos").GetByTestId("photo-tile")).ToHaveCountAsync(2);
        await Expect(Page.GetByTestId("match-accept")).ToContainTextAsync($"Add as a new variant of {FirstRecipeName}");

        await Page.GetByTestId("match-accept").ClickAsync();

        await Expect(Review).ToBeVisibleAsync();
        await Expect(Review).ToContainTextAsync($"Adding a variant to {FirstRecipeName}");
        await Expect(Page.GetByTestId("wizard-photos").GetByTestId("photo-tile")).ToHaveCountAsync(2);
        await Page.GetByTestId("wizard-submit").ClickAsync();
        await Expect(Page.GetByTestId("wizard-submitted")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("wizard-closing-link")).ToHaveAttributeAsync("href", "/recipes/");

        _ = await Page.GotoAsync("/account");
        var group = Page.Locator("li").Filter(new() { HasText = FirstRecipeName });
        var variant = group.Locator("li").Filter(new() { HasText = StandInVariantName });
        await Expect(variant.GetByText("Pending review").First).ToBeVisibleAsync();
    }

    [Test]
    public async Task PickingAnotherRecipe_AddsToThePickedOne_AndChangeThenKeep_SubmitsANewRecipe()
    {
        await PhotoImport.OpenOnCreateRecipeAsync(Page);
        await PhotoImport.AddFromDeviceAsync(Page, PhotoImport.FirstPage, PhotoImport.SecondPage);
        await Page.GetByTestId("read-photos").ClickAsync();
        await Expect(MatchStep).ToBeVisibleAsync();

        _ = await Page.GetByLabel("Recipe", new() { Exact = true })
            .SelectOptionAsync(new SelectOptionValue { Label = PhotoImport.RecipeName });
        await Page.GetByTestId("match-pick").ClickAsync();

        await Expect(Review).ToContainTextAsync($"Adding a variant to {PhotoImport.RecipeName}");

        await ChangeMatch.ClickAsync();
        await Expect(MatchStep).ToBeVisibleAsync();
        await Page.GetByTestId("match-keep").ClickAsync();

        await Expect(Review).ToBeVisibleAsync();
        await Expect(Review).Not.ToContainTextAsync("Adding a variant to");
        await Expect(Review).ToContainTextAsync("Keeping it as a new recipe");
        await Expect(Review).ToContainTextAsync(PhotoImport.StandInRecipeName);
        await Page.GetByTestId("wizard-submit").ClickAsync();
        await Expect(Page.GetByTestId("wizard-submitted")).ToContainTextAsync("Recipe Submitted!");
    }

    [Test]
    public async Task AVariantSubmissionWhoseRecipeIsGone_ClearsItsErrorWhenTheMemberKeepsANewRecipe()
    {
        await Page.RouteAsync(
            "**/api/recipes/*/variants",
            route => route.FulfillAsync(new()
            {
                Status = 404,
                ContentType = "application/json",
                Body = """{"error":"Recipe not found.","code":"recipeNotFound"}""",
            }));
        await PhotoImport.OpenOnCreateRecipeAsync(Page);
        await PhotoImport.AddFromDeviceAsync(Page, PhotoImport.FirstPage, PhotoImport.SecondPage);
        await Page.GetByTestId("read-photos").ClickAsync();
        await Page.GetByTestId("match-accept").ClickAsync();

        await Page.GetByTestId("wizard-submit").ClickAsync();
        await Expect(SubmitError).ToContainTextAsync("That recipe is no longer on the site.");

        await ChangeMatch.ClickAsync();
        await Page.GetByTestId("match-keep").ClickAsync();

        await Expect(Review).ToBeVisibleAsync();
        await Expect(SubmitError).ToHaveCountAsync(0);
    }

    [Test]
    public async Task ThreePhotosOnCreateRecipe_ProposeNoRecipe_AndNameTheFirstRecipesFirstVariantAsAPossibleDuplicate()
    {
        await PhotoImport.OpenOnCreateRecipeAsync(Page);
        await AddsThreePhotosAsync();

        await Page.GetByTestId("read-photos").ClickAsync();

        await Expect(Review).ToBeVisibleAsync();
        await Expect(MatchStep).ToHaveCountAsync(0);
        await Expect(PossibleDuplicate).ToContainTextAsync(
            $"This looks almost the same as {FirstRecipeFirstVariant}, a variant of {FirstRecipeName}.");
    }

    [Test]
    public async Task ThreePhotosOnAddVariant_NameTheRecipesFirstVariantAsAPossibleDuplicate_AndStillSubmit()
    {
        await PhotoImport.OpenOnAddVariantAsync(Page);
        await AddsThreePhotosAsync();

        await Page.GetByTestId("read-photos").ClickAsync();

        await Expect(Review).ToBeVisibleAsync();
        await Expect(MatchStep).ToHaveCountAsync(0);
        await Expect(PossibleDuplicate).ToContainTextAsync(
            $"This looks almost the same as {TacosFirstVariant}, a variant of {PhotoImport.RecipeName}.");

        await Page.GetByTestId("wizard-submit").ClickAsync();

        await Expect(Page.GetByTestId("wizard-submitted")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("wizard-closing-link")).ToHaveAttributeAsync("href", PhotoImport.RecipePath);
    }

    private ILocator Heading(string name) => Page.GetByRole(AriaRole.Heading, new() { Name = name, Exact = true });

    private async Task AddsThreePhotosAsync()
    {
        await PhotoImport.AddFromDeviceAsync(Page, PhotoImport.FirstPage, PhotoImport.SecondPage);
        await Page.GetByTestId("photo-input").SetInputFilesAsync(PhotoImport.FirstPage);
        await Expect(Page.GetByTestId("photo-tile")).ToHaveCountAsync(3);
    }
}
