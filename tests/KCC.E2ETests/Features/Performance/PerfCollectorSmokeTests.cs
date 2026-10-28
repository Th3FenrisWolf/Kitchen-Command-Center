using KCC.E2ETests.Config;
using KCC.E2ETests.Config.Perf;

namespace KCC.E2ETests.Features.Performance;

[Explicit]
[NotInParallel]
public class PerfCollectorSmokeTests
{
    private static readonly string[] ChromiumMetrics =
    [
        "lcp_ms", "frame_p95_ms", "frames_within_16_7_pct", "layers", "filtered_elements", "css_gz_bytes", "js_gz_bytes", "trace_paint_ms",
    ];

    private static readonly string[] WebkitMetrics = ["frame_p95_ms", "filtered_elements", "css_gz_bytes", "cls_geometry"];

    [ClassDataSource<SiteProcess>(Shared = SharedType.None)]
    public SiteProcess Site { get; init; } = null!;

    [Test]
    public async Task Chromium_CollectsTimingLayersAndDeterministicProbes()
    {
        var measurement = await CollectAsync(Engine.Chromium);

        _ = await Assert.That(ChromiumMetrics.Where(metric => !measurement.Values.ContainsKey(metric))).IsEmpty();
    }

    [Test]
    public async Task Webkit_CollectsWhatItCanAndListsWhatItCannot()
    {
        var measurement = await CollectAsync(Engine.Webkit);

        _ = await Assert.That(WebkitMetrics.Where(metric => !measurement.Values.ContainsKey(metric))).IsEmpty();

        _ = await Assert.That(measurement.Gaps).IsNotEmpty();
    }

    private async Task<Measurement> CollectAsync(Engine engine)
    {
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await Profiles.LaunchAsync(playwright, engine, GpuMode.SwiftShader);
        var subject = new Case(Pages.Home, "phone", engine, Ramp.Light, VisitState.Bloomed);
        return await Collectors.MeasureAsync(browser, Site.BaseUrl, subject, new CollectOptions());
    }
}
