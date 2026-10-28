using KCC.E2ETests.Config;
using KCC.E2ETests.Config.Perf;

namespace KCC.E2ETests.Features.Performance;

[Explicit]
[NotInParallel(MemberSession.Serial)]
public class TimingGuardTests
{
    private const int Runs = 3;

    [ClassDataSource<SiteProcess>(Shared = SharedType.None)]
    public SiteProcess Site { get; init; } = null!;

    [Test]
    [Timeout(90 * 60 * 1000)]
    public async Task KeySubset_StaysWithinBudget_OnTheMedianOfThreeRuns(CancellationToken cancellationToken)
    {
        await using var run = await GuardRun.StartAsync(Site.BaseUrl, Engine.Chromium);
        var options = new CollectOptions(Diagnostics: false);
        await run.CalibrateAsync();
        foreach (var subject in Matrix.KeySubset().Where(subject => subject.Engine == Engine.Chromium))
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = await run.MeasureAsync(subject, options, MetricCatalog.Timing);
            var runs = new List<Measurement>();
            for (var index = 0; index < Runs; index++)
            {
                runs.Add(await run.MeasureAsync(subject, options, MetricCatalog.Timing));
            }

            _ = run.Report.Add(subject, runs);
            Console.WriteLine($"[perf guard] {subject.Identity}: {Runs} runs after a warm-up");
        }

        await run.CalibrateAsync();
        var failures = run.Conclude(ResultsWriter.OutputDirectory("local"));

        if (failures.Count > 0)
        {
            Assert.Fail(string.Join('\n', failures));
        }
    }
}
