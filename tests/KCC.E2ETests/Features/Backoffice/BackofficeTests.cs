using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.Backoffice;

[NotInParallel(new[] { BackofficeSession.Serial, MemberSession.Serial })]
public class BackofficeTests : BasePageTests
{
    private const string NewcomerPassword = "Newcomer-Passw0rd";

    [Test]
    public async Task ApprovingANewMember_LetsThemSignIn()
    {
        var userName = $"schumann-{Guid.NewGuid():N}"[..17];
        await SignUpAsync(userName);

        await BackofficeSession.SignInAsync(Page);
        _ = await Page.GotoAsync(BackofficeSession.DashboardPath);
        var row = Page.Locator($"kcc-waiting-tab uui-table-row[data-member='{userName}']");
        await Expect(row).ToBeVisibleAsync(new() { Timeout = BackofficeSession.LoadTimeout });
        await row.GetByRole(AriaRole.Button, new() { Name = "Approve" }).ClickAsync();
        await Expect(row).ToHaveCountAsync(0);

        await Context.ClearCookiesAsync();
        _ = await Page.GotoAsync("/account/login");
        await Page.FillAsync("input[name='UserName']", userName);
        await Page.FillAsync("input[name='Password']", NewcomerPassword);
        await Page.ClickAsync("form button[type='submit']");
        await Page.WaitForURLAsync(url => !url.Contains("/account/login", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public async Task PublishingASubmission_PutsItOnTheSite()
    {
        var recipeName = $"Garnet Stew {Guid.NewGuid():N}"[..20];
        await MemberSession.SignInAsync(Page);
        await SubmitRecipeAsync(recipeName);
        await Context.ClearCookiesAsync();

        await BackofficeSession.SignInAsync(Page);
        _ = await Page.GotoAsync(BackofficeSession.DashboardPath);
        var recipeRow = Page.Locator($"kcc-waiting-tab uui-table-row[data-draft='{recipeName}']");
        await recipeRow.GetByRole(AriaRole.Link, new() { Name = recipeName }).ClickAsync(new() { Timeout = BackofficeSession.LoadTimeout });
        await ChooseThenSuggestAnIconAsync();
        await PublishAsync();

        _ = await Page.GotoAsync(BackofficeSession.DashboardPath);
        var variantRow = Page.Locator("kcc-waiting-tab uui-table-row[data-draft='First Try']").Filter(new() { HasText = recipeName });
        await variantRow.GetByRole(AriaRole.Link, new() { Name = "First Try" }).ClickAsync(new() { Timeout = BackofficeSession.LoadTimeout });
        var ingredients = Page.Locator("kcc-ingredients-editor");
        var steps = Page.Locator("kcc-instructions-editor");
        await Expect(ingredients.Locator("uui-input.name input").First).ToHaveValueAsync("Eggs", new() { Timeout = BackofficeSession.LoadTimeout });
        await Expect(steps.Locator("uui-textarea textarea").First).ToHaveValueAsync("Scramble gently.");
        await ingredients.GetByRole(AriaRole.Button, new() { Name = "Add ingredient" }).ClickAsync();
        await ingredients.Locator("uui-input.name input").Last.FillAsync("Chives");
        await ingredients.Locator("input.unit").Last.FillAsync("Pinch");
        await steps.GetByRole(AriaRole.Button, new() { Name = "Add step" }).ClickAsync();
        await steps.Locator("uui-textarea textarea").Last.FillAsync("Garnish.");
        await PublishAsync();

        _ = await Page.GotoAsync(BackofficeSession.DashboardPath);
        await Expect(Page.Locator("kcc-waiting-tab uui-box").First).ToBeVisibleAsync(new() { Timeout = BackofficeSession.LoadTimeout });
        await Expect(recipeRow).ToHaveCountAsync(0);
        await Expect(variantRow).ToHaveCountAsync(0);

        _ = await Page.GotoAsync($"/recipes/{recipeName.ToLowerInvariant().Replace(' ', '-')}/first-try");
        await Expect(Page.GetByText("Chives").First).ToBeVisibleAsync();
        await Expect(Page.GetByText("Garnish.").First).ToBeVisibleAsync();
    }

    [Test]
    public async Task EditingAndDeletingAReview_ReachesTheVariantPage()
    {
        var written = $"Backoffice E2E review {Guid.NewGuid():N}";
        var edited = $"Edited by the owner {Guid.NewGuid():N}";
        try
        {
            await MemberSession.SignInAsync(Page);
            _ = await Page.GotoAsync(MemberTestVariant.Path);
            await Page.Locator("[data-value='4']").First.ClickAsync();
            await Page.Locator("[data-testid='review-input']").FillAsync(written);
            await Page.Locator("[data-testid='submit-review']").ClickAsync();
            await Expect(Page.Locator("[data-testid='reviews-list']").GetByText(written)).ToBeVisibleAsync();
            await Context.ClearCookiesAsync();

            await BackofficeSession.SignInAsync(Page);
            _ = await Page.GotoAsync(BackofficeSession.DashboardPath);
            await Page.GetByRole(AriaRole.Tab, new() { Name = "Reviews" }).ClickAsync(new() { Timeout = BackofficeSession.LoadTimeout });
            var id = await Page.Locator("kcc-entries-tab uui-box").Filter(new() { HasText = written }).GetAttributeAsync("data-entry");
            var review = Page.Locator($"kcc-entries-tab uui-box[data-entry='{id}']");
            await review.GetByRole(AriaRole.Button, new() { Name = "Edit" }).ClickAsync();
            await review.Locator("uui-select select").SelectOptionAsync("2.5");
            await review.Locator("uui-textarea textarea").FillAsync(edited);
            await review.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
            await Expect(review).ToContainTextAsync(edited);
            await Expect(review).ToContainTextAsync("2.5 ★");

            _ = await Page.GotoAsync(MemberTestVariant.Path);
            await Expect(Page.Locator("[data-testid='reviews-list']").GetByText(edited)).ToBeVisibleAsync();

            _ = await Page.GotoAsync(BackofficeSession.DashboardPath);
            await Page.GetByRole(AriaRole.Tab, new() { Name = "Reviews" }).ClickAsync(new() { Timeout = BackofficeSession.LoadTimeout });
            await review.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
            await Page.Locator("umb-confirm-modal").GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
            await Expect(review).ToHaveCountAsync(0);
        }
        catch
        {
            await DeleteTheMembersReviewAsync();
            throw;
        }
    }

    private async Task SignUpAsync(string userName)
    {
        _ = await Page.GotoAsync("/account/login");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign Up", Exact = true }).ClickAsync();
        await Page.FillAsync("input[name='UserName']", userName);
        await Page.FillAsync("input[name='Email']", $"{userName}@example.test");
        await Page.FillAsync("input[name='Password']", NewcomerPassword);
        await Page.FillAsync("input[name='PasswordConfirmation']", NewcomerPassword);
        await Page.ClickAsync("form button[type='submit']");
        await Page.WaitForURLAsync(url => url.Contains("/account/registration-complete", StringComparison.OrdinalIgnoreCase));
    }

    // The variant's prep time, cook time and servings are mandatory, so they are filled here, where a member would.
    private async Task SubmitRecipeAsync(string recipeName)
    {
        _ = await Page.GotoAsync("/recipes/create-recipe");
        var next = Page.GetByRole(AriaRole.Button, new() { Name = "Next" });
        await Page.GetByPlaceholder("e.g., Mac & Cheese").FillAsync(recipeName);
        await Page.GetByPlaceholder("A short description of this dish").FillAsync("Submitted by the backoffice E2E suite.");
        await next.ClickAsync();
        await Page.GetByPlaceholder("e.g., Classic Stovetop").FillAsync("First Try");
        await Page.GetByPlaceholder("What makes this variant special?").FillAsync("The first way.");
        await Page.Locator("input[id$='-prep-time']").FillAsync("10");
        await Page.Locator("input[id$='-cook-time']").FillAsync("20");
        await Page.Locator("input[id$='-servings']").FillAsync("4");
        await next.ClickAsync();
        await Page.GetByPlaceholder("Ingredient name").First.FillAsync("Eggs");
        await next.ClickAsync();
        await Page.GetByPlaceholder("Describe this step").First.FillAsync("Scramble gently.");
        await next.ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Submit for Review" }).ClickAsync();
        await Expect(Page.GetByText("Recipe Submitted!")).ToBeVisibleAsync();
    }

    // The submission's icon is already the name's fallback, so a suggestion alone would change nothing visible: pick
    // another icon from the grid first, then let the suggestion replace it.
    private async Task ChooseThenSuggestAnIconAsync()
    {
        var editor = Page.Locator("kcc-recipe-icon-editor");
        var preview = editor.Locator(".preview i");
        await Expect(preview).ToBeVisibleAsync(new() { Timeout = BackofficeSession.LoadTimeout });
        var submitted = await preview.GetAttributeAsync("class");
        var other = submitted == "fa-duotone fa-cheese" ? "fa-duotone fa-egg" : "fa-duotone fa-cheese";

        await editor.GetByRole(AriaRole.Button, new() { Name = "Select icon" }).ClickAsync();
        var picker = Page.Locator("kcc-icon-picker-modal");
        await picker.GetByLabel("Search icons").FillAsync(other["fa-duotone fa-".Length..]);
        await picker.Locator($"button[title='{other}']").ClickAsync();
        await picker.GetByRole(AriaRole.Button, new() { Name = "Select icon" }).ClickAsync();
        await Expect(preview).ToHaveAttributeAsync("class", other);

        var suggestion = Page.WaitForResponseAsync(response => response.Url.Contains("/kcc/recipe-editor/icon-suggestion", StringComparison.Ordinal));
        await editor.GetByRole(AriaRole.Button, new() { Name = "Suggest with AI" }).ClickAsync();
        var suggested = (await (await suggestion).JsonAsync())!.Value.GetProperty("icon").GetString()!;
        await Expect(preview).ToHaveAttributeAsync("class", suggested);
        _ = await Assert.That(suggested).IsNotEqualTo(other);
    }

    private async Task PublishAsync()
    {
        var published = Page.WaitForResponseAsync(response =>
            response.Request.Method == "PUT"
                && response.Url.Contains("/umbraco/management/api/v1/document/", StringComparison.Ordinal)
                && response.Url.EndsWith("/update-and-publish", StringComparison.Ordinal));
        await Page.GetByRole(AriaRole.Button, new() { Name = "Save and publish", Exact = true }).ClickAsync();
        _ = await Assert.That((await published).Ok).IsTrue();
    }

    private async Task DeleteTheMembersReviewAsync()
    {
        try
        {
            await Context.ClearCookiesAsync();
            await MemberSession.SignInAsync(Page);
            await MemberTestVariant.DeleteReviewIfPresentAsync(Page);
        }
        catch (Exception exception) when (exception is PlaywrightException or TimeoutException)
        {
            // A failed cleanup must not replace the failure that triggered it.
        }
    }
}
