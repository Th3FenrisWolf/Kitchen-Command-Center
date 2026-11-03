using System.Text.RegularExpressions;
using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.Members;

[NotInParallel(MemberSession.Serial)]
public class AccountSettingsErrorTests : BasePageTests
{
    private static readonly Regex ErrorLine = new(@"\bkcc-field--error\b");

    [Test]
    public async Task ABlankName_DrawsTheErrorLineUnderThatNameOnly()
    {
        await MemberSession.SignInAsync(Page);
        _ = await Page.GotoAsync("/account/settings");
        await Page.FillAsync("input[name='FirstName']", " ");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Save Changes", Exact = true }).ClickAsync();

        await ExpectMarkedAsync(Page.Locator("input[name='FirstName']"), Page.GetByRole(AriaRole.Alert));
        await ExpectUnmarkedAsync(Page.Locator("input[name='LastName']"));
    }

    [Test]
    public async Task AWrongCurrentPassword_DrawsTheErrorLineUnderItOnly()
    {
        await MemberSession.SignInAsync(Page);
        _ = await Page.GotoAsync("/account/settings");
        var (current, fresh, confirmation) = PasswordFields();
        await current.FillAsync("Not-My-Passw0rd");
        await fresh.FillAsync("Brand-New-Passw0rd");
        await confirmation.FillAsync("Brand-New-Passw0rd");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Update Password", Exact = true }).ClickAsync();

        await ExpectMarkedAsync(current, Page.GetByRole(AriaRole.Alert));
        await ExpectUnmarkedAsync(fresh);
        await ExpectUnmarkedAsync(confirmation);
    }

    [Test]
    public async Task NewPasswordsThatDoNotMatch_DrawTheErrorLineUnderBoth()
    {
        await MemberSession.SignInAsync(Page);
        _ = await Page.GotoAsync("/account/settings");
        var (current, fresh, confirmation) = PasswordFields();
        await current.FillAsync("Not-My-Passw0rd");
        await fresh.FillAsync("Brand-New-Passw0rd");
        await confirmation.FillAsync("Brand-New-Passw0rd!");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Update Password", Exact = true }).ClickAsync();

        var alert = Page.GetByRole(AriaRole.Alert);
        await ExpectMarkedAsync(fresh, alert);
        await ExpectMarkedAsync(confirmation, alert);
        await ExpectUnmarkedAsync(current);
    }

    private (ILocator Current, ILocator Fresh, ILocator Confirmation) PasswordFields() =>
        (Page.GetByLabel("Current Password"), Page.GetByLabel(new Regex("^New Password")), Page.GetByLabel("Confirm New Password"));

    private async Task ExpectMarkedAsync(ILocator input, ILocator alert)
    {
        await Expect(alert).ToBeVisibleAsync();
        await Expect(input.Locator("xpath=..")).ToHaveClassAsync(ErrorLine);
        await Expect(input).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(input).ToHaveAttributeAsync("aria-describedby", (await alert.GetAttributeAsync("id"))!);
    }

    private async Task ExpectUnmarkedAsync(ILocator input)
    {
        await Expect(input.Locator("xpath=..")).Not.ToHaveClassAsync(ErrorLine);
        await Expect(input).Not.ToHaveAttributeAsync("aria-invalid", "true");
    }
}
