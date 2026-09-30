using Microsoft.Playwright;

namespace KCC.ReferenceCapture;

public sealed record Viewport(string Name, int Width, int Height);

public sealed record Capture(
    string Name,
    string Path,
    bool SignedIn,
    Func<IPage, Task>? Prepare = null,
    bool FullPage = true,
    int ExpectedStatus = 200);

public static class Captures
{
    public const string RecipePath = "/recipes/fluffy-buttermilk-pancakes";
    public const string VariantPath = "/recipes/fluffy-buttermilk-pancakes/classic-stack";

    public static readonly string[] Themes = ["light", "dark"];

    public static readonly Viewport[] Viewports =
    [
        new("desktop", 1440, 900),
        new("mobile", 390, 844),
    ];

    public static readonly Capture[] All =
    [
        new("home", "/", SignedIn: false),
        new("recipes", "/recipes", SignedIn: false),
        new("recipe", RecipePath, SignedIn: false),
        new("variant", VariantPath, SignedIn: false),
        // The dialog is a fixed full-screen overlay, exactly one viewport, so shoot it unscrolled rather than full-page.
        new("cook-mode", VariantPath, SignedIn: false, OpenCookModeAsync, FullPage: false),
        new("login", "/account/login", SignedIn: false),
        new("registration-complete", "/account/registration-complete", SignedIn: false),
        new("not-found", "/this-page-does-not-exist", SignedIn: false, ExpectedStatus: 404),
        new("account", "/account", SignedIn: true),
        new("settings", "/account/settings", SignedIn: true),
        new("create-recipe", "/recipes/create-recipe", SignedIn: true),
        new("add-variant", RecipePath, SignedIn: true, OpenAddVariantAsync),
    ];

    private static async Task OpenCookModeAsync(IPage page)
    {
        // Below the lg breakpoint the only open button is hidden and there is no mobile trigger, so the click is dispatched rather than performed.
        await page.Locator("[data-test='cook-mode-open-desktop']").DispatchEventAsync("click");
        await page.Locator("[role='dialog']").WaitForAsync();
    }

    private static async Task OpenAddVariantAsync(IPage page)
    {
        // The add-variant URL carries the recipe's platform-specific key, so follow the recipe page's own link.
        await page.Locator("main a[href*='add-variant']").First.ClickAsync();
        await page.WaitForURLAsync(url => url.Contains("add-variant", StringComparison.OrdinalIgnoreCase));
    }
}
