using Microsoft.Playwright;

namespace KCC.E2ETests.Config;

public static class PhotoImport
{
    public const string RecipeName = "Weeknight Tacos";

    public const string RecipePath = "/recipes/weeknight-tacos/";

    public const string StandInRecipeName = "Skillet Cornbread";

    public static string FirstPage { get; } = Fixture("recipe-page-1.jpg");

    public static string SecondPage { get; } = Fixture("recipe-page-2.jpg");

    public static async Task OpenOnCreateRecipeAsync(IPage page)
    {
        await MemberSession.SignInAsync(page);
        _ = await page.GotoAsync("/recipes/create-recipe");
        await page.GetByTestId("wizard-start-photos").ClickAsync();
    }

    public static async Task OpenOnAddVariantAsync(IPage page)
    {
        await MemberSession.SignInAsync(page);
        _ = await page.GotoAsync(RecipePath);
        await page.GetByRole(AriaRole.Link, new() { Name = "Add Variant", Exact = true }).First.ClickAsync();
        await page.GetByTestId("wizard-start-photos").ClickAsync();
    }

    public static async Task AddFromDeviceAsync(IPage page, params string[] photos)
    {
        await page.GetByTestId("photo-input").SetInputFilesAsync(photos);
        await Assertions.Expect(page.GetByTestId("photo-tile")).ToHaveCountAsync(photos.Length);
    }

    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
}
