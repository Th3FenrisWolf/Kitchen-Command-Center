using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.Members;

[NotInParallel(MemberSession.Serial)]
public class RampSettingTests : BasePageTests
{
    [Test]
    public async Task SavedRamp_SurvivesSigningOutAndBackIn()
    {
        await MemberSession.SignInAsync(Page);
        try
        {
            _ = await Page.GotoAsync("/account/settings");
            await ChooseAsync("dark");
            await Expect(Page.Locator("html")).ToHaveAttributeAsync("data-theme", "dark");

            var home = await (await Page.APIRequest.GetAsync("/")).TextAsync();
            _ = await Assert.That(home).Contains("<html lang=\"en\" data-theme=\"dark\">");

            await Page.Locator("main form[action='/account/logout'] button[type='submit']").ClickAsync();
            await Page.WaitForURLAsync(url => new Uri(url).AbsolutePath == "/");
            await Expect(Page.Locator("html")).ToHaveAttributeAsync("data-theme", "light");

            await MemberSession.SignInAsync(Page);
            await Expect(Page.Locator("html")).ToHaveAttributeAsync("data-theme", "dark");
        }
        finally
        {
            await Page.Context.ClearCookiesAsync();
            await MemberSession.SignInAsync(Page);
            _ = await Page.GotoAsync("/account/settings");
            if (await Page.GetByTestId("ramp-device").GetAttributeAsync("aria-checked") != "true")
            {
                await ChooseAsync("device");
            }
        }
    }

    private Task ChooseAsync(string ramp) =>
        Page.RunAndWaitForResponseAsync(
            () => Page.GetByTestId($"ramp-{ramp}").ClickAsync(),
            response => response.Url.EndsWith("/api/profile/ramp", StringComparison.Ordinal) && response.Ok);
}
