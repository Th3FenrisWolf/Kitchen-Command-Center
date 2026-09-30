using KCC.ReferenceCapture;
using Microsoft.Playwright;

var outDir = ArgValue(args, "--out") ?? throw new ArgumentException("--out <directory> is required");
var baseUrl = ArgValue(args, "--base-url") ?? "https://localhost:58671";
var only = ArgValue(args, "--only");
var selected = only is null ? Captures.All : SelectCaptures(only);

using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });

// Phase 2 has no sign-in until Phase 4, so an --only selection of public pages must not require credentials.
var signedInPasses = new[] { false, true }.Where(signedIn => selected.Any(c => c.SignedIn == signedIn)).ToArray();

var written = 0;
foreach (var theme in Captures.Themes)
{
    foreach (var viewport in Captures.Viewports)
    {
        var themeViewport = $"{theme}-{viewport.Name}";
        var folder = Path.Combine(outDir, themeViewport);
        Directory.CreateDirectory(folder);

        foreach (var signedIn in signedInPasses)
        {
            await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                BaseURL = baseUrl,
                IgnoreHTTPSErrors = true,
                ViewportSize = new ViewportSize { Width = viewport.Width, Height = viewport.Height },
                ColorScheme = theme == "dark" ? ColorScheme.Dark : ColorScheme.Light,
            });

            // The layout's pre-paint script reads the stored ramp before the OS preference.
            await context.AddInitScriptAsync($"try {{ localStorage.setItem('kcc-theme', '{theme}'); }} catch (e) {{ }}");

            var page = await context.NewPageAsync();
            if (signedIn)
            {
                await SignInAsync(page);
            }

            foreach (var capture in selected.Where(c => c.SignedIn == signedIn))
            {
                var response = await OpenAsync(page, capture.Path);
                ValidateCapture(page, capture, response, themeViewport);

                if (capture.Prepare is not null)
                {
                    await capture.Prepare(page);
                    await SettleAsync(page);
                }

                var file = Path.Combine(folder, $"{capture.Name}.png");

                // Dev tooling overlays visitors never see: MiniProfiler's badge and the Vue DevTools button.
                const string HideDevOverlaysCss = ".mp-results, #__vue-devtools-container__, #vue-devtools-anchor { display: none !important; }";

                await page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path = file,
                    FullPage = capture.FullPage,
                    Animations = ScreenshotAnimations.Disabled,
                    Style = HideDevOverlaysCss,
                });
                written++;
                Console.WriteLine(file);
            }
        }
    }
}

Console.WriteLine($"{written} screenshots written to {outDir}");
return 0;

static string? ArgValue(string[] args, string name)
{
    var index = Array.IndexOf(args, name);
    if (index < 0)
    {
        return null;
    }

    if (index + 1 >= args.Length)
    {
        throw new ArgumentException($"{name} requires a value.");
    }

    return args[index + 1];
}

static Capture[] SelectCaptures(string only)
{
    var names = only.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    var byName = Captures.All.ToDictionary(c => c.Name);
    var unknown = names.Where(n => !byName.ContainsKey(n)).ToArray();
    if (unknown.Length > 0)
    {
        throw new ArgumentException(
            $"Unknown --only name(s): {string.Join(", ", unknown)}. Valid names: {string.Join(", ", byName.Keys)}.");
    }

    return names.Select(n => byName[n]).ToArray();
}

static async Task<IResponse?> OpenAsync(IPage page, string path)
{
    var response = await page.GotoAsync(path, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
    await SettleAsync(page);
    return response;
}

static void ValidateCapture(IPage page, Capture capture, IResponse? response, string themeViewport)
{
    var status = response?.Status;
    if (status != capture.ExpectedStatus)
    {
        throw new InvalidOperationException(
            $"{capture.Name} ({themeViewport}): expected HTTP {capture.ExpectedStatus} for '{capture.Path}' but got {(status?.ToString() ?? "no response")}.");
    }

    var actualPath = new Uri(page.Url).AbsolutePath.TrimEnd('/');
    var expectedPath = capture.Path.TrimEnd('/');
    if (!string.Equals(actualPath, expectedPath, StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            $"{capture.Name} ({themeViewport}): expected path '{capture.Path}' but landed on '{page.Url}'.");
    }
}

static async Task SettleAsync(IPage page)
{
    // Fonts swap in late, and in development Main.ts removes the inlined SSR styles once hydration has run.
    await page.WaitForFunctionAsync(
        "() => document.fonts.status === 'loaded' && !document.querySelector('style[data-ssr-styles]')",
        null,
        new PageWaitForFunctionOptions { Timeout = 20_000 });

    // Client-only fetches (reviews, cook notes) start in onMounted, after hydration has already finished.
    await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 20_000 });
}

static async Task SignInAsync(IPage page)
{
    var username = Environment.GetEnvironmentVariable("KCC_E2E_MEMBER_USERNAME");
    var password = Environment.GetEnvironmentVariable("KCC_E2E_MEMBER_PASSWORD");
    if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
    {
        throw new InvalidOperationException("Set KCC_E2E_MEMBER_USERNAME and KCC_E2E_MEMBER_PASSWORD for the signed-in captures.");
    }

    await OpenAsync(page, "/account/login");
    await page.FillAsync("input[name='UserName']", username);
    await page.FillAsync("input[name='Password']", password);
    await page.ClickAsync("form button[type='submit']");
    await page.WaitForURLAsync(url => !url.Contains("/account/login", StringComparison.OrdinalIgnoreCase));
}
