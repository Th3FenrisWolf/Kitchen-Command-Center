using Microsoft.Playwright;

namespace KCC.E2ETests.Config;

/// <summary>Signs a seeded, confirmed member in through the public /account/login form.</summary>
public static class MemberSession
{
    // Credentials for the seeded, confirmed E2E member are supplied via environment variables so no
    // login is committed to source control. A confirmed (enabled) member with these credentials must
    // exist in the target database — see "E2E seed member" in the README for the one-time setup and
    // where to define the variables (on macOS/zsh, ~/.zshenv, which `dotnet test` also inherits).
    public static string Username => GetRequired("KCC_E2E_MEMBER_USERNAME");

    public static string Password => GetRequired("KCC_E2E_MEMBER_PASSWORD");

    private static string GetRequired(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException(
                $"{name} is not set. The logged-in member E2E flows need a seeded, confirmed member's "
                    + "credentials — set KCC_E2E_MEMBER_USERNAME and KCC_E2E_MEMBER_PASSWORD in your "
                    + "environment (see \"E2E seed member\" in the README).");

    /// <summary>Logs the seeded member in via the login form and waits for the post-login redirect.</summary>
    public static async Task SignInAsync(IPage page)
    {
        // The dev page holds an open Vite HMR WebSocket, so the default `load` event never settles
        // and GotoAsync would time out. The login form is server-rendered, so DOMContentLoaded
        // already exposes the username/password fields we fill below.
        _ = await page.GotoAsync("/account/login", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // LoginView renders username/password through InputField (which spreads $attrs onto a
        // plain <input>), so the fields are reachable by their name attribute.
        await page.FillAsync("input[name='UserName']", Username);
        await page.FillAsync("input[name='Password']", Password);

        // The form's submit control renders the "SignIn" resource string; click it by role/type
        // rather than text so the helper is independent of the seeded string value.
        await page.ClickAsync("form button[type='submit']");

        // On success the client sets window.location.href; wait until we've left the login page.
        await page.WaitForURLAsync(url => !url.Contains("/account/login", StringComparison.OrdinalIgnoreCase));
    }
}
