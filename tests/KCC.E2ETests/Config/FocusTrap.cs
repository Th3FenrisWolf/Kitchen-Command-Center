using Microsoft.Playwright;

namespace KCC.E2ETests.Config;

public static class FocusTrap
{
    // Two presses each way wrap past both ends of a dialog with one or two focusable controls.
    private static readonly string[] Keys = ["Tab", "Tab", "Shift+Tab", "Shift+Tab"];

    public static async Task<bool> HoldsAsync(IPage page, ILocator dialog)
    {
        foreach (var key in Keys)
        {
            await page.Keyboard.PressAsync(key);
            if (!await dialog.EvaluateAsync<bool>("dialog => dialog.contains(document.activeElement)"))
            {
                return false;
            }
        }

        return true;
    }
}
