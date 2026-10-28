using System.Diagnostics;
using KCC.E2ETests.Config;
using KCC.E2ETests.Config.Perf;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.Performance;

[Explicit]
[NotInParallel(MemberSession.Serial)]
public class LookCaptureTests
{
    [ClassDataSource<SiteProcess>(Shared = SharedType.None)]
    public SiteProcess Site { get; init; } = null!;

    [Test]
    [Timeout(4 * 60 * 60 * 1000)]
    public async Task Capture_WritesAPngPerSelectedCase_AndTheScriptsOffPairs(CancellationToken cancellationToken)
    {
        var directory = LookCapture.Directory(Environment.GetEnvironmentVariable(LookCapture.RefVariable));
        var cases = LookCapture.Order(CaseFilter.FromEnvironment().Apply(LookCapture.Cases()));
        Directory.CreateDirectory(directory);
        var failures = new List<string>();
        var written = 0;
        var timer = Stopwatch.StartNew();
        Ramp? savedRamp = null;

        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        var index = 0;
        foreach (var group in cases.GroupBy(MatrixRun.BrowserFor))
        {
            var browser = await LaunchAsync(playwright, group.Key);
            var pairKey = group.Key with { Gpu = LookCapture.ScriptsOffGpu(group.Key.Engine) ?? group.Key.Gpu };
            IBrowser? pairBrowser = null;
            string? memberState = null;
            try
            {
                foreach (var subject in group)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    index++;
                    if (!browser.IsConnected)
                    {
                        browser = await LaunchAsync(playwright, group.Key);
                        memberState = null;
                    }

                    try
                    {
                        if (subject.Member != MemberKind.None)
                        {
                            memberState ??= await Collectors.SignInStateAsync(
                                browser, Site.BaseUrl, Profiles.ByName(subject.Profile), subject.Engine,
                                MemberSession.DefaultUsernameVariable, MemberSession.DefaultPasswordVariable);
                            if (savedRamp != subject.Ramp)
                            {
                                memberState = await SaveRampAsync(browser, subject, memberState);
                                savedRamp = subject.Ramp;
                            }
                        }

                        if (LookCapture.ComparesScriptsOff(subject) && pairBrowser is not { IsConnected: true })
                        {
                            pairBrowser = pairKey == group.Key ? browser : await LaunchAsync(playwright, pairKey);
                        }

                        written += await CaptureAsync(browser, pairBrowser, subject, LookCapture.StorageStateFor(subject, memberState), directory);
                        Console.WriteLine($"[capture {index}/{cases.Count} {timer.Elapsed:hh\\:mm\\:ss}] {subject.Identity}");
                    }
                    catch (Exception error) when (error is PlaywrightException or TimeoutException or InvalidOperationException)
                    {
                        failures.Add($"{subject.Identity}: {error.GetType().Name}: {error.Message.Split('\n')[0]}");
                        Console.WriteLine($"[capture {index}/{cases.Count}] {failures[^1]}");
                    }
                }
            }
            finally
            {
                if (pairBrowser is not null && pairBrowser != browser)
                {
                    await pairBrowser.CloseAsync();
                }

                await browser.CloseAsync();
            }
        }

        Console.WriteLine($"[capture] {written} PNGs for {cases.Count} cases in {timer.Elapsed:hh\\:mm\\:ss} at {directory}");
        _ = await Assert.That(cases).IsNotEmpty();
        _ = await Assert.That(failures).IsEmpty();
    }

    private static Task<IBrowser> LaunchAsync(IPlaywright playwright, BrowserKey key) =>
        Profiles.LaunchAsync(playwright, key.Engine, key.Gpu ?? GpuMode.Hardware);

    private async Task<int> CaptureAsync(IBrowser browser, IBrowser? pairBrowser, Case subject, string? memberState, string directory)
    {
        var options = new CollectOptions(StorageState: memberState, Timing: false, Diagnostics: false, Freeze: LookCapture.Freezes(subject));
        await using (var session = await CaseSession.OpenAsync(browser, Site.BaseUrl, subject, options))
        {
            if (session.Measurement.Gaps.Count > 0)
            {
                Console.WriteLine($"[capture] {subject.Identity}: {string.Join("; ", session.Measurement.Gaps)}");
            }

            await session.Page.EvaluateAsync("() => document.fonts.ready");
            await File.WriteAllBytesAsync(Path.Combine(directory, LookCapture.FileName(subject)), await FullPageAsync(session.Page, subject));
        }

        if (!LookCapture.ComparesScriptsOff(subject))
        {
            return 1;
        }

        await FirstPaintAsync(pairBrowser ?? browser, subject, memberState, scripts: true, Path.Combine(directory, LookCapture.FileName(subject, LookCapture.ScriptsOn)));
        await FirstPaintAsync(pairBrowser ?? browser, subject, memberState, scripts: false, Path.Combine(directory, LookCapture.FileName(subject, LookCapture.ScriptsOff)));
        return 3;
    }

    // Chromium renders a full-page capture as one surface, and past 8192 device rows it repeats the top of the page
    // and drops text, so a tall Chromium page is captured in strips and stacked.
    private static async Task<byte[]> FullPageAsync(IPage page, Case subject)
    {
        var scale = Profiles.ByName(subject.Profile).DeviceScaleFactor;
        var width = page.ViewportSize!.Width;
        var height = await page.EvaluateAsync<int>("() => Math.ceil(Math.max(document.documentElement.scrollHeight, document.body?.scrollHeight ?? 0))");
        var segments = PngStack.Segments(height, scale, LookCapture.MaxDeviceRows);
        if (subject.Engine != Engine.Chromium || segments.Count == 1)
        {
            return await page.ScreenshotAsync(new() { FullPage = true });
        }

        var strips = new List<byte[]>();
        foreach (var (y, rows) in segments)
        {
            strips.Add(await page.ScreenshotAsync(new() { FullPage = true, Clip = new() { X = 0, Y = y, Width = width, Height = rows } }));
        }

        return PngStack.Vertical(strips);
    }

    // The case capture may show a state a script reached and a full page; a first paint is the page as it loads, so
    // the scripts-on side of the pair is taken the same way as the scripts-off side.
    private async Task FirstPaintAsync(IBrowser browser, Case subject, string? memberState, bool scripts, string path)
    {
        var options = Profiles.ContextOptions(Profiles.ByName(subject.Profile), subject.Engine, subject.Ramp, Site.BaseUrl);
        options.StorageState = memberState;
        options.JavaScriptEnabled = scripts;
        await using var context = await browser.NewContextAsync(options);
        _ = await subject.BeginVisitAsync(context, Site.BaseUrl);
        var page = await context.NewPageAsync();
        _ = await page.GotoAsync(CasePages.Path(subject.Page), new() { WaitUntil = WaitUntilState.Load });
        if (scripts)
        {
            await Washes.SettleAsync(page);
            await page.EvaluateAsync("() => new Promise(done => requestAnimationFrame(() => requestAnimationFrame(done)))");
        }

        await page.EvaluateAsync("() => document.fonts.ready");
        var theme = await page.EvaluateAsync<string?>("() => document.documentElement.dataset.theme ?? null");
        if (theme != Case.Wire(subject.Ramp))
        {
            throw new InvalidOperationException($"the page rendered the {theme ?? "unset"} ramp with scripts {(scripts ? "on" : "off")}, not {Case.Wire(subject.Ramp)}");
        }

        _ = await page.ScreenshotAsync(new() { Path = path });
    }

    // The settings page rewrites the ramp cookie from the member's saved ramp on every visit, so the ramp is saved
    // through the settings form rather than set as a cookie.
    private async Task<string> SaveRampAsync(IBrowser browser, Case subject, string memberState)
    {
        var options = Profiles.ContextOptions(Profiles.ByName(subject.Profile), subject.Engine, subject.Ramp, Site.BaseUrl);
        options.StorageState = memberState;
        await using var context = await browser.NewContextAsync(options);
        var page = await context.NewPageAsync();
        _ = await page.GotoAsync("/account/settings", new() { WaitUntil = WaitUntilState.Load });
        await Washes.SettleAsync(page);
        var choice = page.GetByTestId($"ramp-{Case.Wire(subject.Ramp)}");
        if (await choice.GetAttributeAsync("aria-checked") != "true")
        {
            _ = await page.RunAndWaitForResponseAsync(
                () => choice.ClickAsync(),
                response => response.Url.EndsWith("/api/profile/ramp", StringComparison.Ordinal) && response.Ok);
        }

        return await context.StorageStateAsync();
    }
}
