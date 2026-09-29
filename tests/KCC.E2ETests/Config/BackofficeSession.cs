using Microsoft.Playwright;

namespace KCC.E2ETests.Config;

public static class BackofficeSession
{
    // Parallel sign-ins as one backoffice user left all but one of them on the sign-in page, so the suites that sign
    // in as the admin run one at a time.
    public const string Serial = "backoffice-admin";

    public const string DashboardPath = "/umbraco/section/content/dashboard/contributions";

    // The backoffice downloads its whole client before it draws a section: longer than Playwright's five seconds.
    public const float LoadTimeout = 30_000;

    public static async Task SignInAsync(IPage page)
    {
        _ = await page.GotoAsync("/umbraco");
        await page.FillAsync("#username-input", SiteProcess.AdminEmail);
        await page.FillAsync("#password-input", SiteProcess.AdminPassword);
        await page.GetByRole(AriaRole.Button, new() { Name = "Login", Exact = true }).ClickAsync();
        await page.WaitForURLAsync(url => url.Contains("/umbraco/section/", StringComparison.Ordinal), new() { Timeout = 2 * LoadTimeout });
    }
}
