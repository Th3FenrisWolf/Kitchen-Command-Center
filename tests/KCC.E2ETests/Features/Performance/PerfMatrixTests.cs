using System.Diagnostics;
using KCC.E2ETests.Config;
using KCC.E2ETests.Config.Perf;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.Performance;

[Explicit]
[NotInParallel(MemberSession.Serial)]
public class PerfMatrixTests
{
    [Test]
    [Timeout(4 * 60 * 60 * 1000)]
    public async Task Matrix_MeasuresEverySelectedCase(CancellationToken cancellationToken)
    {
        var settings = MatrixRun.Settings.FromEnvironment();
        var live = settings.IsLive ? LiveSite.FromEnvironment() : null;
        await using var site = live is null ? new SiteProcess() : null;
        if (site is not null)
        {
            await site.InitializeAsync();
        }

        var baseUrl = live?.Origin ?? site!.BaseUrl;
        var options = new CollectOptions(Paths: live?.Paths, Live: live);
        var filter = CaseFilter.FromEnvironment();
        var cases = MatrixRun.Order(filter.Apply(Matrix.Full(settings.Member)));
        var report = new RunReport { Commit = ResultsWriter.Commit() };
        var directory = ResultsWriter.OutputDirectory(settings.Mode);
        var versions = new Dictionary<Engine, string>();
        var members = new MemberStates(once: live is not null);
        var timer = Stopwatch.StartNew();

        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        if (live is not null)
        {
            report.ServerResponse = await ServerResponseAsync(playwright, live, filter, members);
            report.Environment = Environment(versions);
            _ = ResultsWriter.WriteResults(directory, report);
        }

        var index = 0;
        foreach (var group in cases.GroupBy(MatrixRun.BrowserFor))
        {
            var browser = await LaunchAsync(playwright, group.Key);
            versions[group.Key.Engine] = browser.Version;
            try
            {
                foreach (var subject in group)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    index++;
                    if (!browser.IsConnected)
                    {
                        browser = await LaunchAsync(playwright, group.Key);
                        members.Forget(group.Key);
                    }

                    var memberState = subject.Member == MemberKind.None
                        ? null
                        : await members.GetAsync(group.Key, () => SignInAsync(browser, baseUrl, Profiles.ByName(subject.Profile), subject.Engine, live));

                    var runs = new List<Measurement>();
                    for (var run = 0; run < settings.Runs; run++)
                    {
                        runs.Add(await MeasureAsync(browser, baseUrl, subject, options with { StorageState = memberState }, live));
                    }

                    var record = report.Add(subject, runs);
                    Console.WriteLine($"[perf {index}/{cases.Count} {timer.Elapsed:hh\\:mm\\:ss}] {subject.Identity}: {record.Metrics.Count} metrics, {record.Gaps.Count} gaps, {runs.Count} runs");
                    report.Environment = Environment(versions);
                    _ = ResultsWriter.WriteResults(directory, report);
                }
            }
            finally
            {
                await browser.CloseAsync();
            }
        }

        report.Environment = Environment(versions);
        report.Evaluate(BudgetFile.Load(MatrixRun.BudgetsPath), live is null ? BaselineFile.Load(MatrixRun.BaselinePath) : BaselineFile.Empty, ResultsWriter.Platform);
        var path = ResultsWriter.WriteResults(directory, report);
        if (settings.WritesBaseline)
        {
            ResultsWriter.WriteBaseline(MatrixRun.BaselinePath, report, merge: MatrixRun.Narrowed(filter));
        }

        Console.WriteLine($"[perf] {cases.Count} cases in {timer.Elapsed:hh\\:mm\\:ss}; results at {path}");
        _ = await Assert.That(report.Cases.Count).IsEqualTo(cases.Count);
        if (settings.Check)
        {
            _ = await Assert.That(report.Failures()).IsEmpty();
        }
    }

    private static Task<IBrowser> LaunchAsync(IPlaywright playwright, BrowserKey key) =>
        Profiles.LaunchAsync(playwright, key.Engine, key.Gpu ?? GpuMode.Hardware);

    private static async Task<Measurement> MeasureAsync(IBrowser browser, Uri baseUrl, Case subject, CollectOptions options, LiveSite? live)
    {
        try
        {
            return await Collectors.MeasureAsync(browser, baseUrl, subject, options);
        }
        catch (Exception error) when (Measurable(error))
        {
            var failed = new Measurement();
            failed.Gap("case", Describe(error, live));
            return failed;
        }
    }

    private static async Task<string> SignInAsync(IBrowser browser, Uri baseUrl, Profile profile, Engine engine, LiveSite? live)
    {
        if (live is null)
        {
            return await Collectors.SignInStateAsync(
                browser, baseUrl, profile, engine, MemberSession.DefaultUsernameVariable, MemberSession.DefaultPasswordVariable);
        }

        try
        {
            return await Collectors.SignInStateAsync(browser, baseUrl, profile, engine, LiveSite.UsernameVariable, LiveSite.PasswordVariable, live: live);
        }
        catch (Exception error) when (Measurable(error))
        {
            throw new InvalidOperationException($"The measurement member could not sign in: {Describe(error, live)}");
        }
    }

    private static async Task<Dictionary<string, ServerResponseRecord>> ServerResponseAsync(IPlaywright playwright, LiveSite live, CaseFilter filter, MemberStates members)
    {
        var key = new BrowserKey(Engine.Chromium, Profiles.Desktop.Gpu);
        var browser = await LaunchAsync(playwright, key);
        var records = new Dictionary<string, ServerResponseRecord>();
        try
        {
            foreach (var page in ServerResponse.Pages(filter, live.Paths))
            {
                var member = Case.DefaultMember(page, MemberKind.Live);
                var state = member == MemberKind.None
                    ? null
                    : await members.GetAsync(key, () => SignInAsync(browser, live.Origin, Profiles.Desktop, Engine.Chromium, live));
                var path = CasePages.Path(page, live.Paths);
                var samples = new List<ServerSample>();
                var gaps = new List<string>();
                for (var run = 0; run < ServerResponse.Runs; run++)
                {
                    try
                    {
                        samples.Add(await ServerResponse.SampleAsync(browser, live, page, state, gaps.Add));
                    }
                    catch (Exception error) when (Measurable(error))
                    {
                        gaps.Add(Describe(error, live));
                    }
                }

                records[page] = ServerResponseRecord.From(path, member, samples, gaps);
                Console.WriteLine($"[perf live] {page}: server response {records[page].MinMs} to {records[page].MaxMs} ms over {samples.Count} runs");
            }
        }
        finally
        {
            await browser.CloseAsync();
        }

        return records;
    }

    private static bool Measurable(Exception error) =>
        error is PlaywrightException or TimeoutException or InvalidOperationException or System.Text.Json.JsonException;

    private static string Describe(Exception error, LiveSite? live)
    {
        var message = $"{error.GetType().Name}: {error.Message.Split('\n')[0]}";
        return live?.Redact(message) ?? message;
    }

    private static Dictionary<string, string> Environment(IReadOnlyDictionary<Engine, string> versions) =>
        ResultsWriter.Environment(
            versions.GetValueOrDefault(Engine.Chromium),
            versions.GetValueOrDefault(Engine.Webkit),
            $"phone: {Profiles.Phone.Gpu}; desktop: {Profiles.Desktop.Gpu}".ToLowerInvariant());
}
