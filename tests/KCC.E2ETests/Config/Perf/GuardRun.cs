using Microsoft.Playwright;

namespace KCC.E2ETests.Config.Perf;

public sealed class GuardRun : IAsyncDisposable
{
    private readonly IPlaywright playwright;
    private readonly Uri baseUrl;
    private readonly Engine engine;
    private IBrowser browser;
    private readonly HashSet<string> measuredKinds = [];
    private readonly List<double> calibration = [];
    private string? memberState;

    private GuardRun(IPlaywright playwright, IBrowser browser, Uri baseUrl, Engine engine)
    {
        this.playwright = playwright;
        this.browser = browser;
        this.baseUrl = baseUrl;
        this.engine = engine;
    }

    public RunReport Report { get; } = new() { Commit = ResultsWriter.Commit() };

    public static async Task<GuardRun> StartAsync(Uri baseUrl, Engine engine)
    {
        var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        var browser = await Profiles.LaunchAsync(playwright, engine, Profiles.Phone.Gpu);
        return new(playwright, browser, baseUrl, engine);
    }

    private async Task EnsureBrowserAsync()
    {
        if (!browser.IsConnected)
        {
            browser = await Profiles.LaunchAsync(playwright, engine, Profiles.Phone.Gpu);
            memberState = null;
        }
    }

    public async Task CalibrateAsync()
    {
        await EnsureBrowserAsync();
        calibration.AddRange(await RunnerCalibration.SampleAsync(browser));
    }

    public async Task<Measurement> MeasureAsync(Case subject, CollectOptions options, string kind)
    {
        await EnsureBrowserAsync();
        if (subject.Member != MemberKind.None)
        {
            memberState ??= await Collectors.SignInStateAsync(
                browser, baseUrl, Profiles.ByName(subject.Profile), subject.Engine,
                MemberSession.DefaultUsernameVariable, MemberSession.DefaultPasswordVariable);
        }

        measuredKinds.Add(kind);
        var measurement = await MeasureOnceRetriedAsync(subject, options with { StorageState = memberState });
        var kept = new Measurement();
        foreach (var (name, value) in measurement.Values.Where(pair => MetricCatalog.Describe(pair.Key).Kind == kind))
        {
            kept.Values[name] = value;
        }

        kept.Gaps.AddRange(measurement.Gaps);
        return kept;
    }

    // Search's infinite load occasionally stalls short of three pages in WebKit; a case that never reached its state
    // measured nothing, so it gets one more try.
    private async Task<Measurement> MeasureOnceRetriedAsync(Case subject, CollectOptions options)
    {
        try
        {
            return await Collectors.MeasureAsync(browser, baseUrl, subject, options);
        }
        catch (Exception error) when (error is PlaywrightException or TimeoutException or InvalidOperationException)
        {
            Console.WriteLine($"[perf guard] {subject.Identity}: retrying after {error.GetType().Name}: {error.Message.Split('\n')[0]}");
            return await Collectors.MeasureAsync(browser, baseUrl, subject, options);
        }
    }

    public IReadOnlyList<string> Conclude(string directory)
    {
        var baseline = BaselineFile.Load(MatrixRun.CiBaselinePath);
        Report.Environment = ResultsWriter.Environment(
            engine == Engine.Chromium ? browser.Version : null,
            engine == Engine.Webkit ? browser.Version : null,
            Profiles.Phone.Gpu.ToString().ToLowerInvariant());
        var budgets = BudgetFile.Load(MatrixRun.BudgetsPath).OnCi();
        Report.CalibrationMs = calibration.Count > 0 ? ResultsWriter.Median(calibration) : null;
        Report.Evaluate(budgets, baseline, ResultsWriter.Platform);
        var path = ResultsWriter.WriteResults(directory, Report);
        if (MatrixRun.Settings.FromEnvironment().WritesBaseline)
        {
            ResultsWriter.WriteBaseline(MatrixRun.CiBaselinePath, Report, merge: true, mergeMetrics: true);
            return [];
        }

        foreach (var skip in Report.Skips())
        {
            Console.WriteLine($"[perf guard] {skip} ({baseline.Platform ?? "unknown"} baseline, {ResultsWriter.Platform} runner)");
        }

        if (Report.CalibrationMs is { } calibrated)
        {
            Console.WriteLine($"[perf guard] runner calibration {calibrated} ms against {baseline.CalibrationMs?.ToString() ?? "none"} ms in the baseline: budgets x{Report.Runner}");
        }

        Console.WriteLine($"[perf guard] {Report.Cases.Count} cases; results at {path}");
        return
        [
            .. Report.Cases
                .Where(record => !baseline.Cases.ContainsKey(record.Subject.Identity))
                .Select(record => $"{record.Subject.Identity}: no baseline in {Path.GetFileName(MatrixRun.CiBaselinePath)}"),
            .. Report.MissingFromBaseline(budgets, baseline, measuredKinds),
            .. Report.Failures(),
        ];
    }

    public async ValueTask DisposeAsync()
    {
        await browser.CloseAsync();
        playwright.Dispose();
    }
}
