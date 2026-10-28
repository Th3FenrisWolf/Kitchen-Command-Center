using Microsoft.Playwright;

namespace KCC.E2ETests.Config;

public static class MemberSession
{
    // Every suite that signs in as this member changes what the others see of it, so they run one at a time.
    public const string Serial = "e2e-member";

    // The fixture's seeder creates this approved member from the same two variables, so no login is committed to
    // source control.
    public const string DefaultUsernameVariable = "KCC_E2E_MEMBER_USERNAME";

    public const string DefaultPasswordVariable = "KCC_E2E_MEMBER_PASSWORD";

    public static string Username => GetRequired(DefaultUsernameVariable);

    public static string Password => GetRequired(DefaultPasswordVariable);

    public static Task SignInAsync(IPage page) => SignInAsync(page, DefaultUsernameVariable, DefaultPasswordVariable);

    public static async Task SignInAsync(IPage page, string usernameVariable, string passwordVariable)
    {
        _ = await page.GotoAsync("/account/login");

        await page.FillAsync("input[name='UserName']", GetRequired(usernameVariable));
        await page.FillAsync("input[name='Password']", GetRequired(passwordVariable));

        // The submit control renders the "SignIn" dictionary string; clicking it by type keeps the helper
        // independent of the string's value.
        await page.ClickAsync("form button[type='submit']");

        await page.WaitForURLAsync(url => !url.Contains("/account/login", StringComparison.OrdinalIgnoreCase));
    }

    private static string GetRequired(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException(
                $"{name} is not set. The member E2E flows sign in as the member the fixture seeds from "
                    + "KCC_E2E_MEMBER_USERNAME and KCC_E2E_MEMBER_PASSWORD, and the live measurement as the member in "
                    + "KCC_PERF_MEMBER_USERNAME and KCC_PERF_MEMBER_PASSWORD; set the pair the run needs (see \"E2E tests\" in the README).");
}
