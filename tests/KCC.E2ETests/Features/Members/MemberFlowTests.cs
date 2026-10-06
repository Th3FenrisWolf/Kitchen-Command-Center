using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.Members;

[NotInParallel(MemberSession.Serial)]
public class MemberFlowTests : BasePageTests
{
    [Test]
    public async Task SignUp_WaitsForTheOwnersApproval()
    {
        var userName = $"newcomer-{Guid.NewGuid():N}"[..18];
        _ = await Page.GotoAsync("/account/login");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Sign Up", Exact = true }).ClickAsync();
        await Page.FillAsync("input[name='UserName']", userName);
        await Page.FillAsync("input[name='Email']", $"{userName}@example.test");
        await Page.FillAsync("input[name='Password']", "Newcomer-Passw0rd");
        await Page.FillAsync("input[name='PasswordConfirmation']", "Newcomer-Passw0rd");
        await Page.ClickAsync("form button[type='submit']");

        await Page.WaitForURLAsync(url => url.Contains("/account/registration-complete", StringComparison.OrdinalIgnoreCase));
        await Expect(Page.Locator("main")).ToContainTextAsync("waiting for approval");

        _ = await Page.GotoAsync("/account/login");
        await Page.FillAsync("input[name='UserName']", userName);
        await Page.FillAsync("input[name='Password']", "Newcomer-Passw0rd");
        await Page.ClickAsync("form button[type='submit']");

        await Expect(Page.GetByRole(AriaRole.Alert)).ToContainTextAsync("waiting for approval");
        _ = await Assert.That(Page.Url).Contains("/account/login");
    }

    [Test]
    public async Task ApprovedMember_SignsIn_AndSignsOutFromTheHeader()
    {
        await MemberSession.SignInAsync(Page);
        _ = await Page.GotoAsync("/");

        await Page.GetByRole(AriaRole.Button, new() { Name = "My kitchen" }).ClickAsync();
        await Page.Locator("#pad-card-kitchen").GetByRole(AriaRole.Button, new() { Name = "Sign out", Exact = true }).ClickAsync();

        await Page.WaitForURLAsync(url => new Uri(url).AbsolutePath == "/");
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Sign in", Exact = true })).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Link, new() { Name = "New recipe" })).ToHaveCountAsync(0);
    }

    [Test]
    public async Task SubmittedRecipe_WaitsOnTheAccountPageAsPending()
    {
        var recipeName = $"E2E Submission {Guid.NewGuid():N}"[..23];
        await MemberSession.SignInAsync(Page);
        _ = await Page.GotoAsync("/recipes/create-recipe");
        var next = Page.GetByRole(AriaRole.Button, new() { Name = "Next" });

        await Page.GetByPlaceholder("e.g., Mac & Cheese").FillAsync(recipeName);
        await Page.GetByPlaceholder("A short description of this dish").FillAsync("Submitted by the E2E suite.");
        await next.ClickAsync();
        await Page.GetByPlaceholder("e.g., Classic Stovetop").FillAsync("First Try");
        await next.ClickAsync();
        await Page.GetByPlaceholder("Ingredient name").First.FillAsync("Eggs");
        await next.ClickAsync();
        await Page.GetByPlaceholder("Describe this step").First.FillAsync("Scramble gently.");
        await next.ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Submit for Review" }).ClickAsync();
        await Expect(Page.GetByText("Recipe Submitted!")).ToBeVisibleAsync();

        _ = await Page.GotoAsync("/account");
        var group = Page.Locator("li").Filter(new() { HasText = recipeName });
        await Expect(group.GetByText("Pending review").First).ToBeVisibleAsync();
    }
}
