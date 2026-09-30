using Microsoft.Playwright;

namespace KCC.E2ETests.Config;

public static class MemberSession
{
    // Every suite that signs in as this member changes what the others see of it, so they run one at a time.
    public const string Serial = "e2e-member";

    // The fixture's seeder creates this approved member from the same two variables, so no login is committed to
    // source control.
    public static string Username => GetRequired("KCC_E2E_MEMBER_USERNAME");

    public static string Password => GetRequired("KCC_E2E_MEMBER_PASSWORD");

    public static async Task SignInAsync(IPage page)
    {
        _ = await page.GotoAsync("/account/login");

        await page.FillAsync("input[name='UserName']", Username);
        await page.FillAsync("input[name='Password']", Password);

        // The submit control renders the "SignIn" dictionary string; clicking it by type keeps the helper
        // independent of the string's value.
        await page.ClickAsync("form button[type='submit']");

        // On success the client sets window.location.href; wait until we've left the login page.
        await page.WaitForURLAsync(url => !url.Contains("/account/login", StringComparison.OrdinalIgnoreCase));
    }

    private static string GetRequired(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException(
                $"{name} is not set. The member E2E flows sign in as the member the fixture seeds from "
                    + "KCC_E2E_MEMBER_USERNAME and KCC_E2E_MEMBER_PASSWORD; set both (see \"E2E tests\" in the README).");
}
