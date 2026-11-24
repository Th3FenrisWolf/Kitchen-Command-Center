using System.Text.RegularExpressions;
using KCC.E2ETests.Config;
using KCC.E2ETests.Config.Perf;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.Performance;

[Explicit]
[NotInParallel]
public partial class PerfCollectorSmokeTests
{
    private static readonly string[] ChromiumMetrics =
    [
        "lcp_ms", "frame_p95_ms", "frames_within_16_7_pct", "layers", "filtered_elements", "css_gz_bytes", "js_gz_bytes", "trace_paint_ms",
        "js_first_load_gz_bytes", "js_later_gz_bytes", "islands_hydrated", "restyled_on_tuck", "restyled_on_hover", "wash_raster_on_interaction",
    ];

    private static readonly string[] WebkitMetrics =
    [
        "frame_p95_ms", "filtered_elements", "css_gz_bytes", "cls_geometry", "js_first_load_gz_bytes", "js_later_gz_bytes", "islands_hydrated",
    ];

    private static readonly string[] TraceProbeMetrics = ["restyled_on_tuck", "restyled_on_hover", "wash_raster_on_interaction"];

    [ClassDataSource<SiteProcess>(Shared = SharedType.None)]
    public SiteProcess Site { get; init; } = null!;

    [Test]
    public async Task Chromium_CollectsTimingLayersAndDeterministicProbes()
    {
        var measurement = await CollectAsync(Engine.Chromium);

        _ = await Assert.That(ChromiumMetrics.Where(metric => !measurement.Values.ContainsKey(metric) && !HoverGapped(measurement, metric))).IsEmpty();
        _ = await Assert.That(measurement.Values["islands_hydrated"]).IsEqualTo(await ServerIslandsAsync());
    }

    [Test]
    public async Task Webkit_CollectsWhatItCanAndListsWhatItCannot()
    {
        var measurement = await CollectAsync(Engine.Webkit);

        _ = await Assert.That(WebkitMetrics.Where(metric => !measurement.Values.ContainsKey(metric))).IsEmpty();
        _ = await Assert.That(measurement.Values["islands_hydrated"]).IsEqualTo(await ServerIslandsAsync());
        _ = await Assert.That(measurement.Gaps).IsNotEmpty();
        _ = await Assert.That(TraceProbeMetrics.Where(metric => !measurement.Gaps.Any(gap => gap.StartsWith(metric + ":", StringComparison.Ordinal)))).IsEmpty();
    }

    [Test]
    [Arguments(Engine.Chromium)]
    [Arguments(Engine.Webkit)]
    public async Task ModulePreloadedChunk_CountsInsideFirstLoadScript(Engine engine)
    {
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await Profiles.LaunchAsync(playwright, engine, GpuMode.SwiftShader);
        var subject = new Case(Pages.Home, "phone", engine, Ramp.Light, VisitState.Bloomed);
        string? preloaded = null;

        var measurement = await Collectors.MeasureAsync(browser, Site.BaseUrl, subject, new CollectOptions(Timing: false, Prepare: page =>
            page.RouteAsync("**/*", async route =>
            {
                if (route.Request.ResourceType != "document")
                {
                    await route.FallbackAsync();
                    return;
                }

                var response = await route.FetchAsync();
                var html = await response.TextAsync();
                var entry = EntryScript().Match(html).Groups[1].Value;
                preloaded = entry + (entry.Contains('?') ? "&" : "?") + "preloaded";
                await route.FulfillAsync(new() { Response = response, Body = html.Replace("<head>", $"<head><link rel=\"modulepreload\" href=\"{preloaded}\">") });
            })));

        using var http = new HttpClient { BaseAddress = Site.BaseUrl };
        var chunk = ResourceWeights.GzipBytes(await http.GetByteArrayAsync(preloaded));

        _ = await Assert.That(preloaded).IsNotNull();
        _ = await Assert.That(measurement.Values["js_first_load_gz_bytes"]).IsGreaterThanOrEqualTo(2 * chunk);
        _ = await Assert.That(measurement.Values["js_gz_bytes"]).IsEqualTo(measurement.Values["js_first_load_gz_bytes"] + measurement.Values["js_later_gz_bytes"]);
    }

    [Test]
    public async Task PhoneRecipePage_RequestsTheEntryChunkBeforeAnyIslandChunk()
    {
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await Profiles.LaunchAsync(playwright, Engine.Chromium, GpuMode.SwiftShader);
        var subject = new Case(Pages.Recipe, "phone", Engine.Chromium, Ramp.Light, VisitState.Bloomed);
        await using var session = await CaseSession.OpenAsync(browser, Site.BaseUrl, subject, new CollectOptions(Diagnostics: false));

        var requestStarts = await session.Page.EvaluateAsync<Dictionary<string, double>>(
            "() => Object.fromEntries(performance.getEntriesByType('resource').filter(e => new URL(e.name).pathname.endsWith('.js')).map(e => [new URL(e.name).pathname, e.requestStart]))");
        var entry = EntryScript().Match(await session.Page.ContentAsync()).Groups[1].Value;
        var entryPath = new Uri(Site.BaseUrl, entry).AbsolutePath;
        var chunksStartedBeforeTheEntry = requestStarts.Where(chunk => chunk.Key != entryPath && chunk.Value < requestStarts[entryPath]).Select(chunk => chunk.Key);

        _ = await Assert.That(requestStarts.ContainsKey(entryPath)).IsTrue();
        _ = await Assert.That(requestStarts.Count).IsGreaterThan(1);
        _ = await Assert.That(chunksStartedBeforeTheEntry).IsEmpty();
    }

    // A page with no lift card has nothing to hover, and the probe records a gap there instead of a zero.
    private static bool HoverGapped(Measurement measurement, string metric) =>
        metric == "restyled_on_hover" && measurement.Gaps.Any(gap => gap.StartsWith("restyled_on_hover: ", StringComparison.Ordinal));

    private async Task<Measurement> CollectAsync(Engine engine)
    {
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await Profiles.LaunchAsync(playwright, engine, GpuMode.SwiftShader);
        var subject = new Case(Pages.Home, "phone", engine, Ramp.Light, VisitState.Bloomed);
        return await Collectors.MeasureAsync(browser, Site.BaseUrl, subject, new CollectOptions());
    }

    private async Task<int> ServerIslandsAsync()
    {
        using var http = new HttpClient { BaseAddress = Site.BaseUrl };
        var islands = IslandTag().Count(await http.GetStringAsync("/"));
        _ = await Assert.That(islands).IsGreaterThan(0);
        return islands;
    }

    [GeneratedRegex(@"<kcc-island[\s>]")]
    private static partial Regex IslandTag();

    [GeneratedRegex(@"<script(?=[^>]*type=""module"")[^>]*\ssrc=""([^""]+)""")]
    private static partial Regex EntryScript();
}
