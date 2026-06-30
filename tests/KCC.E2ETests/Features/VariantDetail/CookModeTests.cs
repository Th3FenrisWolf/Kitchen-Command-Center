using Microsoft.Playwright;

namespace KCC.E2ETests.Features.VariantDetail;

// TUnit runs tests in parallel by default; concurrent navigations starve the single-threaded
// Vite dev server and every nav times out. [NotInParallel] serializes them (matches the
// AdminHomePageMiddlewareTests precedent in this repo).
[NotInParallel]
public class CookModeTests : BasePageTests
{
    private const string EnabledVariantPath = "/recipes/mac-cheese/spicy-jalapeno";
    private const string DisabledVariantPath = "/recipes/mac-cheese/classic-mac-cheese";

    // The dev page holds an open Vite HMR WebSocket, so the default `load` event never settles
    // and GotoAsync would time out. The variant content is server-rendered into the initial HTML,
    // so DOMContentLoaded already exposes everything these specs assert on.
    private async Task OpenOverlayAsync()
    {
        _ = await Page.GotoAsync(
            EnabledVariantPath,
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded }
        );
        await Page.Locator("[data-test=\"cook-mode-open-desktop\"]").ClickAsync();
        await Expect(Page.Locator("[role=\"dialog\"]")).ToBeVisibleAsync();
    }

    [Test]
    public async Task CookModeButton_IsDisabled_WhenVariantHasNoInstructions()
    {
        _ = await Page.GotoAsync(
            DisabledVariantPath,
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded }
        );

        await Expect(Page.Locator("[data-test=\"cook-mode-open-desktop\"]")).ToBeDisabledAsync();
    }

    [Test]
    public async Task CookModeButton_IsEnabled_AndOpensOverlay_WhenVariantHasInstructions()
    {
        _ = await Page.GotoAsync(
            EnabledVariantPath,
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded }
        );
        var button = Page.Locator("[data-test=\"cook-mode-open-desktop\"]");

        await Expect(button).ToBeEnabledAsync();
        await button.ClickAsync();

        await Expect(Page.Locator("[role=\"dialog\"]")).ToBeVisibleAsync();
        await Expect(Page.Locator("[data-test=\"cook-progress\"]")).ToContainTextAsync("1");
    }

    [Test]
    public async Task Overlay_ClosesViaCloseButton()
    {
        await OpenOverlayAsync();

        await Page.Locator("[data-test=\"cook-close\"]").ClickAsync();

        await Expect(Page.Locator("[role=\"dialog\"]")).ToBeHiddenAsync();
    }

    [Test]
    public async Task Overlay_ClosesViaEscape()
    {
        await OpenOverlayAsync();

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Page.Locator("[role=\"dialog\"]")).ToBeHiddenAsync();
    }

    [Test]
    public async Task Overlay_TrapsFocusWithinTheDialog()
    {
        await OpenOverlayAsync();

        // Tab a handful of times; focus must never escape the dialog.
        for (var i = 0; i < 6; i++)
        {
            await Page.Keyboard.PressAsync("Tab");
            var focusInsideDialog = await Page.EvaluateAsync<bool>(
                "() => { const d = document.querySelector('[role=\\\"dialog\\\"]'); return !!d && d.contains(document.activeElement); }"
            );
            _ = await Assert.That(focusInsideDialog).IsTrue();
        }
    }

    [Test]
    public async Task Overlay_TrapsFocus_OnInitialShiftTab()
    {
        await OpenOverlayAsync();

        // Regression: on open, focus sits on the tabindex="-1" panel root (excluded from
        // the focusables list). An immediate Shift+Tab — before any forward Tab — must wrap
        // backward to the last focusable and stay inside the dialog, not escape the overlay.
        await Page.Keyboard.PressAsync("Shift+Tab");

        var focusInsideDialog = await Page.EvaluateAsync<bool>(
            "() => { const d = document.querySelector('[role=\\\"dialog\\\"]'); return !!d && d.contains(document.activeElement); }"
        );
        _ = await Assert.That(focusInsideDialog).IsTrue();
    }

    [Test]
    public async Task Steps_AdvanceAndGoBack_WithProgressIndicator()
    {
        await OpenOverlayAsync();

        var progress = Page.Locator("[data-test=\"cook-progress\"]");
        var prev = Page.Locator("[data-test=\"cook-prev\"]");
        var next = Page.Locator("[data-test=\"cook-next\"]");

        // First step: Previous disabled, Next enabled.
        await Expect(prev).ToBeDisabledAsync();
        await Expect(progress).ToContainTextAsync("1");

        await next.ClickAsync();
        await Expect(progress).ToContainTextAsync("2");
        // Last of two steps: Next disabled.
        await Expect(next).ToBeDisabledAsync();

        await prev.ClickAsync();
        await Expect(progress).ToContainTextAsync("1");
    }

    [Test]
    public async Task Step_CanBeCheckedOff()
    {
        await OpenOverlayAsync();
        var check = Page.Locator("[data-test=\"cook-check\"]");

        await Expect(check).ToHaveAttributeAsync("aria-pressed", "false");
        await check.ClickAsync();
        await Expect(check).ToHaveAttributeAsync("aria-pressed", "true");
    }
}
