using System.Globalization;
using KCC.E2ETests.Config;
using KCC.E2ETests.Config.Perf;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.RecipeWizard;

// The perf matrix never presses a wizard's Next, so the first press of each later part is read here, on the matrix's
// own profiles.
[NotInParallel(MemberSession.Serial)]
public class FirstPressTests : BasePageTests
{
    private const double BudgetMs = 100;

    private const int BloomedSecondsAgo = 400;

    private const uint Seed = 0x0badf00d;

    private const string CookModeVariant = "/recipes/fluffy-buttermilk-pancakes/classic-stack";

    // Event Timing reports only an interaction of 16 ms or more, so a quicker press is read from its pointerdown to
    // the first frame after the DOM it changed.
    private const string WatchPresses = """
        (() => {
          window.__kccPresses = []
          window.__kccQuickPresses = []
          new PerformanceObserver((list) => list.getEntries().forEach((entry) => {
            if (entry.interactionId) window.__kccPresses.push({ start: entry.startTime, duration: entry.duration })
          })).observe({ type: 'event', durationThreshold: 16, buffered: true })

          let pressedAt = null
          addEventListener('pointerdown', (event) => { pressedAt = event.timeStamp }, true)
          new MutationObserver(() => {
            if (pressedAt === null) return
            const start = pressedAt
            pressedAt = null
            requestAnimationFrame((frame) => window.__kccQuickPresses.push({ start, duration: Math.max(0, frame - start) }))
          }).observe(document, { subtree: true, childList: true, attributes: true, characterData: true })
        })()
        """;

    private const string LongestPressSince = """
        (since) => {
          const longest = (entries) => {
            const durations = entries.filter((entry) => entry.start >= since).map((entry) => entry.duration)
            return durations.length ? Math.max(...durations) : null
          }
          return longest(window.__kccPresses) ?? longest(window.__kccQuickPresses)
        }
        """;

    // The phone profile rasterises on SwiftShader, which the CPU throttle does not reach, so its wizard presses are
    // recorded against the carried gap rather than held to the budget (spec FR-013).
    [Test]
    [Arguments(0)]
    [Arguments(BloomedSecondsAgo)]
    public async Task EveryNextOfCreateRecipe_IsMeasured_TheFirstTime(int secondsAgo)
    {
        await using var phone = await PhoneAsync(secondsAgo);
        var page = phone.Page;
        _ = await page.GotoAsync("/recipes/create-recipe");
        await Hydration.WaitAsync(page);

        var presses = await TypeToTheReviewAsync(page);

        await AssertEveryPressMeasuredAsync($"phone wizard, {secondsAgo} s into the visit", presses);
    }

    [Test]
    [Arguments(0)]
    [Arguments(BloomedSecondsAgo)]
    public async Task EveryNextOfAddVariant_IsMeasured_TheFirstTime(int secondsAgo)
    {
        await using var phone = await PhoneAsync(secondsAgo);
        var page = phone.Page;
        _ = await page.GotoAsync(PhotoImport.RecipePath);
        await page.GetByRole(AriaRole.Link, new() { Name = "Add Variant", Exact = true }).First.ClickAsync();
        await page.WaitForURLAsync(url => new Uri(url).Query.StartsWith("?recipe=", StringComparison.Ordinal));
        await Hydration.WaitAsync(page);

        var presses = await TypeToTheReviewAsync(page);

        await AssertEveryPressMeasuredAsync($"phone wizard, {secondsAgo} s into the visit", presses);
    }

    // Cook mode's opener shows only from the large breakpoint up, so the phone cannot reach it; it is pressed on the
    // matrix's desktop profile, bare and bloomed. A runner with no GPU rasterises the desktop on SwiftShader too, so
    // there the press is recorded, as the phone's are.
    [Test]
    [Arguments(0)]
    [Arguments(BloomedSecondsAgo)]
    public async Task CookMode_OpensWithinTheBudget_TheFirstTime(int secondsAgo)
    {
        var gpu = Profiles.DefaultDesktopGpu();
        await using var desktop = await SessionAsync(Profiles.Desktop, gpu, secondsAgo);

        List<Press> presses = [new("cook mode", await OpenCookModeAsync(desktop.Page))];

        var run = $"desktop cook mode, {secondsAgo} s into the visit";
        await (gpu == GpuMode.Hardware ? AssertWithinBudgetAsync(run, presses) : AssertEveryPressMeasuredAsync(run, presses));
    }

    private static async Task<double?> OpenCookModeAsync(IPage page)
    {
        _ = await page.GotoAsync(CookModeVariant);
        await Hydration.WaitAsync(page);
        await page.WaitForTimeoutAsync(1500);

        return await PressAsync(page, page.Locator("[data-test^='cook-mode-open']:visible").First, page.Locator("[role='dialog']"));
    }

    private const int MostSteps = 12;

    private const string FocusedStepTitle = """
        () => document.activeElement instanceof HTMLHeadingElement ? document.activeElement.textContent.trim() : null
        """;

    private const string FocusedStepTitleOtherThan = """
        (title) => {
          const focused = document.activeElement
          return focused instanceof HTMLHeadingElement && focused.textContent.trim() !== title
            && !document.querySelector("main [aria-busy='true']")
        }
        """;

    // The walk follows the rendered wizard rather than a list of steps, so a step the flow gains is pressed too.
    private static async Task<List<Press>> TypeToTheReviewAsync(IPage page)
    {
        var next = page.GetByRole(AriaRole.Button, new() { Name = "Next", Exact = true });
        var review = page.GetByTestId("review-step");

        var title = await PressToNextStepAsync(page, page.GetByTestId("wizard-start-typing"), title: null);
        List<Press> presses = [new("start typing", title.Ms)];

        for (var step = 0; step < MostSteps && !await review.IsVisibleAsync(); step++)
        {
            await FillUntilEnabledAsync(page, next);
            var arrived = await PressToNextStepAsync(page, next, title.Name);
            presses.Add(new($"next to {arrived.Name}", arrived.Ms));
            title = arrived;
        }

        _ = await Assert.That(await review.IsVisibleAsync()).IsTrue();
        return presses;
    }

    private static async Task FillUntilEnabledAsync(IPage page, ILocator next)
    {
        var fields = page.Locator("main input:visible:not([type]), main input[type='text']:visible, main textarea:visible");
        for (var i = 0; i < await fields.CountAsync() && !await next.IsEnabledAsync(); i++)
        {
            var field = fields.Nth(i);
            if (await field.InputValueAsync() == "") await field.FillAsync($"First press {i + 1}");
        }
    }

    private static async Task<(string Name, double? Ms)> PressToNextStepAsync(IPage page, ILocator control, string? title)
    {
        var ms = await PressAsync(page, control, () => page.WaitForFunctionAsync(FocusedStepTitleOtherThan, title));
        return (await page.EvaluateAsync<string>(FocusedStepTitle), ms);
    }

    private static Task<double?> PressAsync(IPage page, ILocator control, ILocator arrived) =>
        PressAsync(page, control, () => arrived.WaitForAsync());

    private static async Task<double?> PressAsync(IPage page, ILocator control, Func<Task> arrival)
    {
        var since = await page.EvaluateAsync<double>("() => performance.now()");
        await control.ClickAsync();
        await arrival();

        // An Event Timing entry is reported only after the frame that answers the press has painted.
        await page.EvaluateAsync("() => new Promise((resolve) => requestAnimationFrame(() => requestAnimationFrame(resolve)))");
        await page.WaitForTimeoutAsync(500);
        return await page.EvaluateAsync<double?>(LongestPressSince, since);
    }

    private static async Task AssertEveryPressMeasuredAsync(string run, List<Press> presses)
    {
        Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"[first press] {run}: worst {presses.Max(press => press.Ms)?.ToString(CultureInfo.InvariantCulture) ?? "unmeasured"} ms; ")
            + string.Join(", ", presses.Select(press => string.Create(CultureInfo.InvariantCulture, $"{press.Name} {press.Ms?.ToString(CultureInfo.InvariantCulture) ?? "unmeasured"} ms"))));

        _ = await Assert.That(presses).IsNotEmpty();
        _ = await Assert.That(presses.Where(press => press.Ms is null).Select(press => press.Name)).IsEmpty();
    }

    private static async Task AssertWithinBudgetAsync(string run, List<Press> presses)
    {
        await AssertEveryPressMeasuredAsync(run, presses);
        _ = await Assert.That(presses.Where(press => press.Ms > BudgetMs).Select(press => $"{press.Name}: {press.Ms} ms")).IsEmpty();
    }

    private sealed record Press(string Name, double? Ms);

    private Task<BrowserSession> PhoneAsync(int secondsAgo) => SessionAsync(Profiles.Phone, GpuMode.SwiftShader, secondsAgo);

    private async Task<BrowserSession> SessionAsync(Profile profile, GpuMode gpu, int secondsAgo)
    {
        var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        var browser = await Profiles.LaunchAsync(playwright, Engine.Chromium, gpu);
        var context = await browser.NewContextAsync(Profiles.ContextOptions(profile, Engine.Chromium, Ramp.Light, Site.BaseUrl));
        await Visit.BeginAsync(context, Site.BaseUrl, secondsAgo, Seed);
        await context.AddInitScriptAsync(WatchPresses);
        var page = await context.NewPageAsync();
        await MemberSession.SignInAsync(page);
        await Profiles.ThrottleAsync(await context.NewCDPSessionAsync(page), profile);
        return new BrowserSession(playwright, browser, page);
    }

    private sealed record BrowserSession(IPlaywright Playwright, IBrowser Browser, IPage Page) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await Browser.CloseAsync();
            Playwright.Dispose();
        }
    }
}
