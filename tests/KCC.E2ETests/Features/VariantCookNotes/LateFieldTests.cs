using System.Text.RegularExpressions;
using KCC.E2ETests.Config;
using KCC.E2ETests.Config.Perf;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.VariantCookNotes;

[NotInParallel(MemberSession.Serial)]
public partial class LateFieldTests : BasePageTests
{
    [GeneratedRegex(@"/TextAreaField(-[\w-]+)?\.(js|vue)(\?|$)")]
    private static partial Regex FieldChunk();

    [Test]
    [Arguments(Engine.Chromium)]
    [Arguments(Engine.Webkit)]
    public async Task TextTypedBeforeTheFieldsChunkArrives_SurvivesItsHydration_WithFocusAndCaret(Engine engine)
    {
        await using var held = await OpenWithFieldChunkHeldAsync(engine);
        var page = held.Page;
        var field = page.GetByTestId("cook-note-input");
        var add = page.GetByTestId("add-cook-note");
        await field.ClickAsync();
        await field.PressSequentiallyAsync("abcdef");
        await field.PressAsync("ArrowLeft");
        await field.PressAsync("ArrowLeft");
        await field.PressAsync("ArrowLeft");
        await Expect(add).ToBeDisabledAsync();

        held.Release();

        await Expect(add).ToBeEnabledAsync(new() { Timeout = 30000 });
        await Expect(field).ToHaveValueAsync("abcdef");
        await Expect(field).ToBeFocusedAsync();
        await field.PressSequentiallyAsync("X");
        await Expect(field).ToHaveValueAsync("abcXdef");
    }

    [Test]
    [Arguments(Engine.Chromium)]
    [Arguments(Engine.Webkit)]
    public async Task ReviewSubmittedBeforeTheFieldsChunkArrives_PostsTheTypedText(Engine engine)
    {
        const string typed = "typed before the chunk";
        await using var held = await OpenWithFieldChunkHeldAsync(engine);
        var page = held.Page;
        try
        {
            await page.Locator("[data-value='3.5']").First.ClickAsync();
            var field = page.GetByTestId("review-input");
            await field.ClickAsync();
            await field.PressSequentiallyAsync(typed);

            var posted = page.WaitForRequestAsync(request => request.Method == "PUT" && request.Url.Contains("/review"));
            await page.GetByTestId("submit-review").ClickAsync();
            var request = await posted;
            held.Release();

            _ = await Assert.That(request.PostData).Contains(typed);
        }
        finally
        {
            held.Release();
            await MemberTestVariant.DeleteReviewIfPresentAsync(page);
        }
    }

    private async Task<HeldFieldChunk> OpenWithFieldChunkHeldAsync(Engine engine)
    {
        var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        var browser = await Profiles.LaunchAsync(playwright, engine, GpuMode.SwiftShader);
        var context = await browser.NewContextAsync(Profiles.ContextOptions(Profiles.Phone, engine, Ramp.Light, Site.BaseUrl));
        var page = await context.NewPageAsync();
        await MemberSession.SignInAsync(page);
        if (engine == Engine.Chromium)
        {
            await Profiles.ThrottleAsync(await context.NewCDPSessionAsync(page), Profiles.Phone);
        }

        var requested = new TaskCompletionSource();
        var release = new TaskCompletionSource();
        await page.RouteAsync(FieldChunk(), async route =>
        {
            requested.TrySetResult();
            await release.Task;
            await route.ContinueAsync();
        });

        _ = await page.GotoAsync(MemberTestVariant.Path, new() { WaitUntil = WaitUntilState.Commit });
        await Hydration.WaitAsync(page);
        await requested.Task.WaitAsync(TimeSpan.FromSeconds(30));
        return new HeldFieldChunk(playwright, browser, page, release);
    }

    private sealed class HeldFieldChunk(
        IPlaywright playwright,
        IBrowser browser,
        IPage page,
        TaskCompletionSource release
    ) : IAsyncDisposable
    {
        public IPage Page => page;

        public void Release() => release.TrySetResult();

        public async ValueTask DisposeAsync()
        {
            await browser.DisposeAsync();
            playwright.Dispose();
        }
    }
}
