using KCC.E2ETests.Config;
using KCC.E2ETests.Config.Perf;

namespace KCC.E2ETests.Features.Performance;

// The guard drives SwiftShader at three times the pixel density, which starves the shared site of CPU and times out
// other suites' navigations, so it runs alone.
[NotInParallel]
public class DeterministicGuardTests
{
    // Other suites add reviews and notes to the shared site, and each one changes the washes a page draws.
    [ClassDataSource<SiteProcess>(Shared = SharedType.PerClass)]
    public SiteProcess Site { get; init; } = null!;

    [Test]
    [Arguments(Engine.Chromium)]
    [Arguments(Engine.Webkit)]
    public async Task KeySubset_StaysWithinBudget_OnEveryProbeAndByteTotal(Engine engine, CancellationToken cancellationToken)
    {
        await using var run = await GuardRun.StartAsync(Site.BaseUrl, engine);
        foreach (var subject in Matrix.DeterministicGuard().Where(subject => subject.Engine == engine))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var measurement = await run.MeasureAsync(subject, new CollectOptions(Timing: false), MetricCatalog.Deterministic);
            _ = run.Report.Add(subject, [Matrix.GuardsScriptBytesOnly(subject) ? ScriptBytesOf(measurement) : measurement]);
        }

        var failures = run.Conclude(Path.Combine(ResultsWriter.OutputDirectory("local"), $"deterministic-{Case.Wire(engine)}"));

        if (failures.Count > 0)
        {
            Assert.Fail(string.Join('\n', failures));
        }
    }

    private static Measurement ScriptBytesOf(Measurement measurement)
    {
        var kept = new Measurement();
        foreach (var metric in Matrix.ScriptBytes.Where(measurement.Values.ContainsKey))
        {
            kept.Values[metric] = measurement.Values[metric];
        }

        kept.Gaps.AddRange(measurement.Gaps.Where(gap => Matrix.ScriptBytes.Any(metric => gap.StartsWith($"{metric}:", StringComparison.Ordinal))));
        return kept;
    }

    // A filter or a blend re-runs whenever anything under it changes, so one on or around a blooming wash redraws
    // every frame. Bloomed visits have nothing left running, so the guard looks mid-bloom.
    [Test]
    [Arguments(Engine.Chromium)]
    [Arguments(Engine.Webkit)]
    public async Task MidBloom_NoRunningAnimationIsFilteredOrUnderAFilter(Engine engine, CancellationToken cancellationToken)
    {
        await using var run = await GuardRun.StartAsync(Site.BaseUrl, engine);
        var failures = new List<string>();
        var subjects = Matrix.KeySubset()
            .Where(subject => subject.Engine == engine && subject.Ramp == Ramp.Light)
            .Select(subject => subject with { Visit = VisitState.Mid });
        foreach (var subject in subjects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var probes = await run.MeasureAsync(subject, new CollectOptions(Timing: false), MetricCatalog.Deterministic);
            var running = probes.Values.GetValueOrDefault("animations_running");
            var filtered = probes.Values.GetValueOrDefault("animated_filtered");
            var wrapped = probes.Values.GetValueOrDefault("animated_under_filter");
            if (running == 0)
            {
                failures.Add($"{subject.Identity}: no wash was blooming, so the probe proved nothing");
            }

            if (filtered > 0)
            {
                failures.Add($"{subject.Identity}: {filtered} running animations carry a filter or a blend themselves");
            }

            if (wrapped > 0)
            {
                failures.Add($"{subject.Identity}: {wrapped} running animations sit under an element with a filter");
            }
        }

        if (failures.Count > 0)
        {
            Assert.Fail(string.Join('\n', failures));
        }
    }
}
