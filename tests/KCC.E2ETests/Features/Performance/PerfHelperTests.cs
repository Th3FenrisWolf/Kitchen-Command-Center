using System.Text.Json;
using KCC.E2ETests.Config.Perf;

namespace KCC.E2ETests.Features.Performance;

public class PerfHelperTests
{
    private static readonly Case Search = new(Pages.Search3, "phone", Engine.Chromium, Ramp.Dark, VisitState.Bloomed);

    [Test]
    public async Task Identity_IsTheTuple_AndNamesOnlyWhatDiffersFromTheDefaults()
    {
        _ = await Assert.That(Search.Identity).IsEqualTo("search-3 · phone · chromium · dark · bloomed");
        _ = await Assert.That((Search with { ImagesOn = false, Motion = Motion.Reduced, Member = MemberKind.Live }).Identity)
            .IsEqualTo("search-3 · phone · chromium · dark · bloomed · images off · motion reduced · member live");
        _ = await Assert.That(new Case(Pages.Account, "desktop", Engine.Webkit, Ramp.Light, VisitState.Bare, Member: MemberKind.E2e).Identity)
            .IsEqualTo("account · desktop · webkit · light · bare");
        _ = await Assert.That(new Case(Pages.Account, "desktop", Engine.Webkit, Ramp.Light, VisitState.Bare).Identity)
            .IsEqualTo("account · desktop · webkit · light · bare · member none");
    }

    [Test]
    public async Task FullMatrix_HasUniqueIdentities_AndSignsInOnlyMemberPages()
    {
        var cases = Matrix.Full();

        _ = await Assert.That(cases.Select(item => item.Identity).Distinct().Count()).IsEqualTo(cases.Count);
        _ = await Assert.That(cases.Where(item => Pages.NeedsMember(item.Page) != (item.Member != MemberKind.None))).IsEmpty();
        _ = await Assert.That(cases.Where(item => item.Visit == VisitState.Mid && item.Engine != Engine.Chromium)).IsEmpty();
    }

    [Test]
    public async Task KeySubset_IsFivePagesOnThePhoneBloomedInBothEnginesAndRamps()
    {
        var cases = Matrix.KeySubset();

        _ = await Assert.That(cases.Count).IsEqualTo(20);
        _ = await Assert.That(cases.All(item => item.Profile == "phone" && item.Visit == VisitState.Bloomed)).IsTrue();
        _ = await Assert.That(cases.Select(item => item.Page).Distinct()).IsEquivalentTo([Pages.Home, Pages.Search3, Pages.Recipe, Pages.VariantCook, Pages.Account]);
    }

    [Test]
    public async Task Filter_KeepsOnlyTheNamedValues()
    {
        var filter = CaseFilter.Parse(new Dictionary<string, string> { ["PAGE"] = "home,search-3", ["ENGINE"] = "webkit" });

        var kept = filter.Apply(Matrix.Full()).ToList();

        _ = await Assert.That(kept).IsNotEmpty();
        _ = await Assert.That(kept.All(item => item.Engine == Engine.Webkit && item.Page is Pages.Home or Pages.Search3)).IsTrue();
    }

    [Test]
    public async Task RatioBudget_FailsOnlyPastBothTheRatioAndTheFloor_InTheContractsFormat()
    {
        var budget = new Budget("frame_p95_ms", null, "baseline", Margin: new Margin(Ratio: 1.25, Floor: 4));

        var over = BudgetEvaluator.Evaluate("frame_p95_ms", 22.4, 15.1, budget);
        var inside = BudgetEvaluator.Evaluate("frame_p95_ms", 19, 15.1, budget);

        _ = await Assert.That(over.Status).IsEqualTo(Verdict.Fail);
        _ = await Assert.That($"{Search.Identity}: frame_p95_ms {over.Detail}")
            .IsEqualTo("search-3 · phone · chromium · dark · bloomed: frame_p95_ms 22.4 > baseline 15.1 (x1.25, +4 ms)");
        _ = await Assert.That(inside.Status).IsEqualTo(Verdict.Pass);
    }

    [Test]
    public async Task RatioBudget_ScalesAMillisecondBaselineByTheRunnerFactor_AndNamesIt()
    {
        var budget = new Budget("hydrated_ms", null, "baseline", Margin: new Margin(Ratio: 1.25, Floor: 150));

        var slowRunner = BudgetEvaluator.Evaluate("hydrated_ms", 1970.2, 1528.4, budget, runner: 1.23);
        var tooSlowStill = BudgetEvaluator.Evaluate("hydrated_ms", 1970.2, 1528.4, budget, runner: 1.02);
        var usualRunner = BudgetEvaluator.Evaluate("hydrated_ms", 1970.2, 1528.4, budget);

        _ = await Assert.That(slowRunner.Status).IsEqualTo(Verdict.Pass);
        _ = await Assert.That(tooSlowStill.Detail).IsEqualTo("1970.2 > baseline 1528.4 (x1.25, +150 ms; runner x1.02)");
        _ = await Assert.That(usualRunner.Detail).IsEqualTo("1970.2 > baseline 1528.4 (x1.25, +150 ms)");
    }

    [Test]
    public async Task TargetBudget_ScalesAMillisecondLimitByTheRunnerFactor_AndNamesIt()
    {
        var budget = new Budget("hydrated_ms", null, "target", Limit: 2000);

        var slowRunner = BudgetEvaluator.Evaluate("hydrated_ms", 2400, null, budget, runner: 1.25);
        var tooSlowStill = BudgetEvaluator.Evaluate("hydrated_ms", 2600, null, budget, runner: 1.25);
        var usualRunner = BudgetEvaluator.Evaluate("hydrated_ms", 2400, null, budget);
        var count = BudgetEvaluator.Evaluate("islands_hydrated", 3, null, new Budget("islands_hydrated", null, "target", Limit: 2), runner: 1.25);

        _ = await Assert.That(slowRunner.Status).IsEqualTo(Verdict.Pass);
        _ = await Assert.That(tooSlowStill.Detail).IsEqualTo("2600 > target 2000 (runner x1.25)");
        _ = await Assert.That(usualRunner.Detail).IsEqualTo("2400 > target 2000");
        _ = await Assert.That(count.Detail).IsEqualTo("3 > target 2");
    }

    [Test]
    public async Task HydratedBudget_HoldsTheRunnerScaledTarget_AndTheBaseline_LocallyAndOnCi()
    {
        var budgets = BudgetFile.Load(MatrixRun.BudgetsPath);
        var phone = new Case(Pages.Home, "phone", Engine.Chromium, Ramp.Light, VisitState.Bloomed);
        Case[] subjects = [phone, phone with { Page = Pages.Recipe }, phone with { Page = Pages.Search3 }, phone with { Profile = "desktop" }];
        double[] measured = [2400, 2600, 1900, 1300];
        double[] references = [2000, 2100, 1000, 1000];
        var baseline = new BaselineFile { CalibrationMs = 200 };
        for (var index = 0; index < subjects.Length; index++)
        {
            baseline.Cases[subjects[index].Identity] = new() { ["hydrated_ms"] = references[index] };
        }

        IEnumerable<string> FailuresOn(BudgetFile file)
        {
            var report = new RunReport { CalibrationMs = 250 };
            for (var index = 0; index < subjects.Length; index++)
            {
                _ = report.Add(subjects[index], [new Measurement { Values = { ["hydrated_ms"] = measured[index] } }]);
            }

            report.Evaluate(file, baseline);
            return report.Failures();
        }

        _ = await Assert.That(FailuresOn(budgets)).IsEquivalentTo(
        [
            "recipe · phone · chromium · light · bloomed: hydrated_ms 2600 > target 2000 (runner x1.25)",
            "search-3 · phone · chromium · light · bloomed: hydrated_ms 1900 > baseline 1000 (x1.25, +150 ms; runner x1.25)",
            "home · desktop · chromium · light · bloomed: hydrated_ms 1300 > target 1000 (runner x1.25)",
        ]);
        _ = await Assert.That(FailuresOn(budgets.OnCi())).IsEquivalentTo(
        [
            "recipe · phone · chromium · light · bloomed: hydrated_ms 2600 > target 2000 (runner x1.25)",
            "search-3 · phone · chromium · light · bloomed: hydrated_ms 1900 > baseline 1000 (x1.5, +150 ms; runner x1.25)",
            "home · desktop · chromium · light · bloomed: hydrated_ms 1300 > target 1000 (runner x1.25)",
        ]);
    }

    [Test]
    public async Task ScriptBytes_FailAtTarget_LocallyAndOnCi_NamingPageProfileEngineRampBudgetAndNumber()
    {
        var budgets = BudgetFile.Load(MatrixRun.BudgetsPath);
        var wizard = new Case(Pages.CreateRecipe, "phone", Engine.Webkit, Ramp.Dark, VisitState.Bloomed, Member: MemberKind.E2e);
        var run = new Measurement { Values = { ["js_first_load_gz_bytes"] = 81_920, ["js_gz_bytes"] = 137_000 } };
        var baseline = new BaselineFile { Cases = { [wizard.Identity] = new() { ["js_first_load_gz_bytes"] = 81_000, ["js_gz_bytes"] = 136_000 } } };

        foreach (var file in new[] { budgets, budgets.OnCi() })
        {
            var report = new RunReport();
            _ = report.Add(wizard, [run]);
            report.Evaluate(file, baseline);

            _ = await Assert.That(report.Failures()).IsEquivalentTo(
            [
                "create-recipe · phone · webkit · dark · bloomed: js_first_load_gz_bytes 81920 > target 61440",
                "create-recipe · phone · webkit · dark · bloomed: js_gz_bytes 137000 > target 117266",
            ]);
        }
    }

    [Test]
    public async Task DeterministicGuard_ReadsTheKeySubset_AndTheWizardPagesForScriptBytesOnly()
    {
        var cases = Matrix.DeterministicGuard();
        var wizard = cases.Except(Matrix.KeySubset()).ToList();

        _ = await Assert.That(cases.Take(Matrix.KeySubset().Count)).IsEquivalentTo(Matrix.KeySubset());
        _ = await Assert.That(wizard.Count).IsEqualTo(8);
        _ = await Assert.That(wizard.Select(item => item.Page).Distinct()).IsEquivalentTo([Pages.CreateRecipe, Pages.AddVariant]);
        _ = await Assert.That(wizard.All(item => item.Profile == "phone" && item.Visit == VisitState.Bloomed && item.Member == MemberKind.E2e)).IsTrue();
        _ = await Assert.That(wizard.All(Matrix.GuardsScriptBytesOnly)).IsTrue();
        _ = await Assert.That(Matrix.KeySubset().Any(Matrix.GuardsScriptBytesOnly)).IsFalse();
        _ = await Assert.That(Matrix.ScriptBytes).IsEquivalentTo(["js_first_load_gz_bytes", "js_gz_bytes"]);
    }

    [Test]
    public async Task RunnerFactor_LeavesCountsBytesAndPercentagesAlone()
    {
        var count = new Budget("filtered_elements", null, "baseline");
        var weight = new Budget("css_gz_bytes", null, "baseline", Margin: new Margin(Pct: 2, FloorBytes: 1024));
        var frames = new Budget("frames_within_16_7_pct", null, "baseline", Margin: new Margin(Add: 5));

        _ = await Assert.That(BudgetEvaluator.Evaluate("filtered_elements", 41, 40, count, runner: 1.5).Status).IsEqualTo(Verdict.Fail);
        _ = await Assert.That(BudgetEvaluator.Evaluate("css_gz_bytes", 70_500, 69_000, weight, runner: 1.5).Status).IsEqualTo(Verdict.Fail);
        _ = await Assert.That(BudgetEvaluator.Evaluate("frames_within_16_7_pct", 60, 70, frames, runner: 1.5).Status).IsEqualTo(Verdict.Fail);
    }

    [Test]
    public async Task RunnerFactor_OnlyEverRelaxes_AndNeedsBothCalibrations()
    {
        _ = await Assert.That(RunReport.RunnerFactor(baselineMs: 200, runMs: 246)).IsEqualTo(1.23);
        _ = await Assert.That(RunReport.RunnerFactor(baselineMs: 200, runMs: 160)).IsEqualTo(1);
        _ = await Assert.That(RunReport.RunnerFactor(baselineMs: null, runMs: 246)).IsEqualTo(1);
        _ = await Assert.That(RunReport.RunnerFactor(baselineMs: 200, runMs: null)).IsEqualTo(1);
    }

    [Test]
    public async Task Report_JudgesTimingAgainstTheRunnerFactor_AndRecordsIt()
    {
        var report = new RunReport { CalibrationMs = 246 };
        var record = report.Add(Search, [new Measurement { Values = { ["hydrated_ms"] = 1970.2, ["layers"] = 71 } }]);
        var baseline = new BaselineFile { CalibrationMs = 200, Cases = { [Search.Identity] = new() { ["hydrated_ms"] = 1528.4, ["layers"] = 70 } } };
        var budgets = new BudgetFile(
        [
            new Budget("hydrated_ms", null, "baseline", Margin: new Margin(Ratio: 1.25, Floor: 150)),
            new Budget("layers", null, "baseline"),
        ]);

        report.Evaluate(budgets, baseline);

        _ = await Assert.That(report.Runner).IsEqualTo(1.23);
        _ = await Assert.That(record.Metrics["hydrated_ms"].Status).IsEqualTo(Verdict.Pass);
        _ = await Assert.That(record.Metrics["hydrated_ms"].Runner).IsEqualTo(1.23);
        _ = await Assert.That(record.Metrics["hydrated_ms"].Baseline).IsEqualTo(1528.4);
        _ = await Assert.That(record.Metrics["layers"].Status).IsEqualTo(Verdict.Fail);
        _ = await Assert.That(record.Metrics["layers"].Runner).IsNull();
    }

    [Test]
    public async Task Baseline_KeepsTheRunnerCalibration_WhenARunWithoutOneMergesIn()
    {
        var path = Path.Combine(Path.GetTempPath(), $"kcc-perf-{Guid.NewGuid():N}.json");
        try
        {
            var calibrated = new RunReport { CalibrationMs = 210.5 };
            _ = calibrated.Add(Search, [new Measurement { Values = { ["hydrated_ms"] = 1500 } }]);
            ResultsWriter.WriteBaseline(path, calibrated);
            var uncalibrated = new RunReport();
            _ = uncalibrated.Add(Search, [new Measurement { Values = { ["layers"] = 70 } }]);
            ResultsWriter.WriteBaseline(path, uncalibrated, merge: true, mergeMetrics: true);

            _ = await Assert.That(BaselineFile.Load(path).CalibrationMs).IsEqualTo(210.5);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task MissingFromBaseline_ReportsOnlyEnforcedMetricsOfMeasuredKinds()
    {
        var report = new RunReport();
        var record = report.Add(Search, [new Measurement { Values = { ["frame_p95_ms"] = 10 } }]);
        var baseline = new BaselineFile
        {
            Cases = { [Search.Identity] = new() { ["frame_p95_ms"] = 10, ["long_frames"] = 0, ["css_gz_bytes"] = 70_000, ["frame_max_ms"] = 30 } },
        };
        var budgets = new BudgetFile(
        [
            new("long_frames", null, "baseline", Margin: new Margin(Add: 1)),
            new("frame_max_ms", null, "report-only"),
            new("css_gz_bytes", null, "baseline", Margin: new Margin(Pct: 2, FloorBytes: 1024)),
        ]);

        var missing = report.MissingFromBaseline(budgets, baseline, new HashSet<string> { MetricCatalog.Timing }).ToList();

        _ = await Assert.That(record.Metrics.Count).IsEqualTo(1);
        _ = await Assert.That(missing.Count).IsEqualTo(1);
        _ = await Assert.That(missing[0]).StartsWith($"{Search.Identity}: long_frames missing from this run");
    }

    [Test]
    public async Task WeightBudget_AllowsTheLargerOfThePercentageAndTheByteFloor()
    {
        var budget = new Budget("css_gz_bytes", null, "baseline", Margin: new Margin(Pct: 2, FloorBytes: 1024));

        _ = await Assert.That(BudgetEvaluator.Evaluate("css_gz_bytes", 70_300, 69_000, budget).Status).IsEqualTo(Verdict.Pass);
        _ = await Assert.That(BudgetEvaluator.Evaluate("css_gz_bytes", 70_500, 69_000, budget).Status).IsEqualTo(Verdict.Fail);
        _ = await Assert.That(BudgetEvaluator.Evaluate("css_gz_bytes", 120_000, 100_000, budget).Status).IsEqualTo(Verdict.Fail);
        _ = await Assert.That(BudgetEvaluator.Evaluate("css_gz_bytes", 101_999, 100_000, budget).Status).IsEqualTo(Verdict.Pass);
    }

    [Test]
    public async Task TargetBudget_ReadsHigherAsBetterForFramesWithinBudget()
    {
        var frames = new Budget("frames_within_16_7_pct", null, "target", Limit: 95);
        var longFrames = new Budget("long_frames", null, "target", Limit: 0);

        _ = await Assert.That(BudgetEvaluator.Evaluate("frames_within_16_7_pct", 66, null, frames).Detail).IsEqualTo("66 < target 95");
        _ = await Assert.That(BudgetEvaluator.Evaluate("frames_within_16_7_pct", 99, null, frames).Status).IsEqualTo(Verdict.Pass);
        _ = await Assert.That(BudgetEvaluator.Evaluate("long_frames", 1, null, longFrames).Detail).IsEqualTo("1 > target 0");
    }

    [Test]
    public async Task BaselineBudget_WithNoMarginFailsOnAnyIncrease_AndWithNoBaselineOnlyReports()
    {
        var count = new Budget("filtered_elements", null, "baseline");
        var longFrames = new Budget("long_frames", null, "baseline", Margin: new Margin(Add: 1));

        _ = await Assert.That(BudgetEvaluator.Evaluate("filtered_elements", 41, 40, count).Status).IsEqualTo(Verdict.Fail);
        _ = await Assert.That(BudgetEvaluator.Evaluate("filtered_elements", 40, 40, count).Status).IsEqualTo(Verdict.Pass);
        _ = await Assert.That(BudgetEvaluator.Evaluate("long_frames", 3, 2, longFrames).Status).IsEqualTo(Verdict.Pass);
        _ = await Assert.That(BudgetEvaluator.Evaluate("long_frames", 4, 2, longFrames).Status).IsEqualTo(Verdict.Fail);
        _ = await Assert.That(BudgetEvaluator.Evaluate("filtered_elements", 41, null, count).Status).IsEqualTo(Verdict.ReportOnly);
        _ = await Assert.That(BudgetEvaluator.Evaluate("filtered_elements", 41, 40, null).Status).IsEqualTo(Verdict.ReportOnly);
    }

    [Test]
    public async Task Budgets_ApplyByScope_AndTheLastMatchWins()
    {
        var budgets = new BudgetFile(
        [
            new Budget("lcp_ms", null, "baseline"),
            new Budget("lcp_ms", new Scope(Profile: "phone"), "target", Limit: 2500),
        ]);

        _ = await Assert.That(budgets.Find("lcp_ms", Search)!.Mode).IsEqualTo("target");
        _ = await Assert.That(budgets.Find("lcp_ms", Search with { Profile = "desktop" })!.Mode).IsEqualTo("baseline");
        _ = await Assert.That(budgets.Find("cls", Search)).IsNull();
    }

    [Test]
    public async Task Report_TakesTheMedianOfTimingRuns_AndListsFailuresAgainstTheBaseline()
    {
        var report = new RunReport();
        var runs = new[] { 30.0, 10.0, 20.0 }.Select(value =>
        {
            var run = new Measurement();
            run.Values["frame_p95_ms"] = value;
            run.Values["layers"] = 70;
            return run;
        }).ToList();
        var record = report.Add(Search, runs);
        var baseline = new BaselineFile { Cases = { [Search.Identity] = new() { ["frame_p95_ms"] = 10, ["layers"] = 70 } } };
        var budgets = new BudgetFile(
        [
            new Budget("frame_p95_ms", null, "baseline", Margin: new Margin(Ratio: 1.25, Floor: 4)),
            new Budget("layers", null, "baseline"),
        ]);

        report.Evaluate(budgets, baseline);

        _ = await Assert.That(record.Metrics["frame_p95_ms"].Value).IsEqualTo(20);
        _ = await Assert.That(record.Metrics["frame_p95_ms"].Runs).IsEquivalentTo([30.0, 10.0, 20.0]);
        _ = await Assert.That(record.Metrics["layers"].Runs).IsNull();
        _ = await Assert.That(record.Metrics["frame_p95_ms"].Delta).IsEqualTo(10);
        _ = await Assert.That(report.Failures()).IsEquivalentTo(["search-3 · phone · chromium · dark · bloomed: frame_p95_ms 20 > baseline 10 (x1.25, +4 ms)"]);
    }

    [Test]
    public async Task Gzip_CountsLevelNineBytes_ForStylesAndScriptsOnly()
    {
        var text = System.Text.Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("a{color:red}", 500)));
        var raw = new byte[1000];

        var totals = ResourceWeights.Totals([("stylesheet", text), ("script", text), ("font", raw), ("image", raw), ("document", raw)]);

        _ = await Assert.That(totals["css_gz_bytes"]).IsBetween(10, 200);
        _ = await Assert.That(totals["js_gz_bytes"]).IsEqualTo(totals["css_gz_bytes"]);
        _ = await Assert.That(totals["font_bytes"]).IsEqualTo(1000);
        _ = await Assert.That(totals["image_bytes"]).IsEqualTo(1000);
    }

    [Test]
    public async Task ScriptSplit_CountsSameOriginScriptStartedBeforeTheMarkAsFirstLoad_AndTheRestAsLater_AtLevelNine()
    {
        var entry = System.Text.Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("export const a = 1;", 400)));
        var later = System.Text.Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("export const later = 'x';", 300)));
        var origin = new Uri("http://127.0.0.1:5000/");
        (string Type, string Url, byte[] Body)[] resources =
        [
            ("script", "http://127.0.0.1:5000/assets/main.js", entry),
            ("script", "http://127.0.0.1:5000/assets/step.js", later),
        ];
        ResourceEntry[] entries =
        [
            new("http://127.0.0.1:5000/assets/main.js", 120, "script"),
            new("http://127.0.0.1:5000/assets/step.js", 2400, "script"),
        ];

        var split = ScriptWeights.Split(resources, entries, hydratedAt: 900, origin);
        var totals = ResourceWeights.Totals(ScriptWeights.Classify(resources, entries));

        _ = await Assert.That(split["js_first_load_gz_bytes"]).IsEqualTo(ResourceWeights.GzipBytes(entry));
        _ = await Assert.That(split["js_later_gz_bytes"]).IsEqualTo(ResourceWeights.GzipBytes(later));
        _ = await Assert.That(totals["js_gz_bytes"]).IsEqualTo(split["js_first_load_gz_bytes"] + split["js_later_gz_bytes"]);
    }

    [Test]
    public async Task ScriptSplit_CountsAScriptFromAnotherOriginToNeitherPart()
    {
        var body = System.Text.Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("beacon();", 300)));
        var origin = new Uri("http://127.0.0.1:5000/");
        (string Type, string Url, byte[] Body)[] resources =
        [
            ("script", "https://static.cloudflareinsights.com/beacon.min.js", body),
            ("script", "http://localhost:5000/assets/late.js", body),
        ];
        ResourceEntry[] entries =
        [
            new("https://static.cloudflareinsights.com/beacon.min.js", 50, "script"),
            new("http://localhost:5000/assets/late.js", 5000, "script"),
        ];

        var split = ScriptWeights.Split(resources, entries, hydratedAt: 900, origin);

        _ = await Assert.That(split["js_first_load_gz_bytes"]).IsEqualTo(0);
        _ = await Assert.That(split["js_later_gz_bytes"]).IsEqualTo(0);
    }

    [Test]
    [Arguments("script", "link")]
    [Arguments("other", "link")]
    [Arguments("script", "other")]
    [Arguments("other", "other")]
    public async Task ScriptSplit_CountsAModulePreloadedChunkBeforeTheMarkAsFirstLoadScript(string resourceType, string initiatorType)
    {
        var chunk = System.Text.Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("export const island = 2;", 300)));
        var origin = new Uri("http://127.0.0.1:5000/");
        (string Type, string Url, byte[] Body)[] resources = [(resourceType, "http://127.0.0.1:5000/assets/island.js", chunk)];
        ResourceEntry[] entries = [new("http://127.0.0.1:5000/assets/island.js", 80, initiatorType)];

        var split = ScriptWeights.Split(resources, entries, hydratedAt: 900, origin);
        var totals = ResourceWeights.Totals(ScriptWeights.Classify(resources, entries));

        _ = await Assert.That(split["js_first_load_gz_bytes"]).IsEqualTo(ResourceWeights.GzipBytes(chunk));
        _ = await Assert.That(split["js_later_gz_bytes"]).IsEqualTo(0);
        _ = await Assert.That(totals["js_gz_bytes"]).IsEqualTo(ResourceWeights.GzipBytes(chunk));
    }

    [Test]
    public async Task ScriptSplit_LeavesAPreloadedStylesheetOrImageOutOfTheScript()
    {
        var body = new byte[500];
        var origin = new Uri("http://127.0.0.1:5000/");
        (string Type, string Url, byte[] Body)[] resources =
        [
            ("stylesheet", "http://127.0.0.1:5000/assets/main.css", body),
            ("image", "http://127.0.0.1:5000/assets/wash-chalk.png", body),
        ];
        ResourceEntry[] entries =
        [
            new("http://127.0.0.1:5000/assets/main.css", 10, "link"),
            new("http://127.0.0.1:5000/assets/wash-chalk.png", 10, "link"),
        ];

        var split = ScriptWeights.Split(resources, entries, hydratedAt: 900, origin);

        _ = await Assert.That(split["js_first_load_gz_bytes"]).IsEqualTo(0);
        _ = await Assert.That(ResourceWeights.Totals(ScriptWeights.Classify(resources, entries))["js_gz_bytes"]).IsEqualTo(0);
    }

    [Test]
    public async Task IslandsHydrated_CountsEveryElementCarryingAVueApp()
    {
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();
        await page.SetContentAsync("<div id='app'><header></header><main><section></section></main></div>");

        await page.EvaluateAsync("() => { document.querySelector('#app').__vue_app__ = {}; performance.mark('kcc:hydrated') }");
        var one = await page.EvaluateAsync<int>(ScriptWeights.IslandsScript);
        await page.EvaluateAsync("() => { document.querySelector('header').__vue_app__ = {}; document.querySelector('section').__vue_app__ = {} }");
        var three = await page.EvaluateAsync<int>(ScriptWeights.IslandsScript);

        _ = await Assert.That(one).IsEqualTo(1);
        _ = await Assert.That(three).IsEqualTo(3);
    }

    [Test]
    public async Task InteractionTrace_SumsRestyles_SkipsAnimationOnlyRecalcs_AndCountsWashPaints()
    {
        using var events = JsonDocument.Parse("""
            [
              {"name":"ScheduleStyleInvalidationTracking","ph":"I","pid":1,"tid":1,"ts":1,"args":{"data":{"changedClass":"is-tucked"}}},
              {"name":"StyleRecalcInvalidationTracking","ph":"I","pid":1,"tid":1,"ts":2,"args":{"data":{"reason":"Inline CSS style declaration was mutated"}}},
              {"name":"UpdateLayoutTree","ph":"X","dur":900,"pid":1,"tid":1,"ts":5,"args":{"elementCount":296}},
              {"name":"StyleRecalcInvalidationTracking","ph":"I","pid":1,"tid":1,"ts":1000,"args":{"data":{"reason":"Animation"}}},
              {"name":"UpdateLayoutTree","ph":"X","dur":10,"pid":1,"tid":1,"ts":1001,"args":{"elementCount":13}},
              {"name":"StyleRecalcInvalidationTracking","ph":"I","pid":1,"tid":1,"ts":1100,"args":{"data":{"reason":"Related style rule"}}},
              {"name":"UpdateLayoutTree","ph":"B","pid":1,"tid":1,"ts":1110,"args":{"beginData":{}}},
              {"name":"UpdateLayoutTree","ph":"E","pid":1,"tid":1,"ts":1120,"args":{"elementCount":4}},
              {"name":"Paint","ph":"X","dur":5,"pid":1,"tid":1,"ts":1130,"args":{"data":{"nodeName":"SPAN class='kcc-wash'"}}},
              {"name":"Paint","ph":"X","dur":5,"pid":1,"tid":1,"ts":1131,"args":{"data":{"nodeName":"SPAN class='kcc-wash kcc-wash--b'"}}},
              {"name":"Paint","ph":"X","dur":5,"pid":1,"tid":1,"ts":1132,"args":{"data":{"nodeName":"DIV class='kcc-washes'"}}},
              {"name":"Paint","ph":"X","dur":5,"pid":1,"tid":1,"ts":1133,"args":{"data":{"nodeName":"#document"}}}
            ]
            """);

        var reading = InteractionTrace.Summarize(events.RootElement.EnumerateArray());

        _ = await Assert.That(reading.Restyled).IsEqualTo(300);
        _ = await Assert.That(reading.WashInvalidations).IsEqualTo(2);
    }

    [Test]
    public async Task TraceProbes_RecordAHoverGap_NotAZero_WhenThePageHasNoLiftCard()
    {
        var result = TraceProbes.Record(new InteractionReading(12, 3), hover: null, hoverGap: "no .kcc-slip--lift on the page");

        _ = await Assert.That(result.Values.ContainsKey("restyled_on_hover")).IsFalse();
        _ = await Assert.That(result.Gaps).IsEquivalentTo(new[] { "restyled_on_hover: no .kcc-slip--lift on the page" });
        _ = await Assert.That(result.Values["restyled_on_tuck"]).IsEqualTo(12);
        _ = await Assert.That(result.Values["wash_raster_on_interaction"]).IsEqualTo(3);
    }

    [Test]
    public async Task TraceProbes_RecordTheHoverReading_WhenTheLiftCardWasHovered()
    {
        var result = TraceProbes.Record(new InteractionReading(12, 3), new InteractionReading(0, 2), hoverGap: null);

        _ = await Assert.That(result.Values["restyled_on_hover"]).IsEqualTo(0);
        _ = await Assert.That(result.Values["wash_raster_on_interaction"]).IsEqualTo(5);
        _ = await Assert.That(result.Gaps).IsEmpty();
    }

    [Test]
    public async Task FrameStats_SkipsTheFirstDelta_AndCountsFramesOnBudget()
    {
        var deltas = new List<double> { 999, 16.7, 16.7, 16.7, 40, 16.7, 16.7, 16.7, 16.7, 100 };

        var stats = FrameStats.Summarize(string.Empty, deltas);

        _ = await Assert.That(stats["frame_max_ms"]).IsEqualTo(100);
        _ = await Assert.That(stats["frame_p95_ms"]).IsEqualTo(100);
        _ = await Assert.That(stats["frames_within_16_7_pct"]).IsEqualTo(77.8);
        _ = await Assert.That(stats["long_frames"]).IsEqualTo(1);
        _ = await Assert.That(FrameStats.Summarize("bloom_", deltas).ContainsKey("bloom_frame_p95_ms")).IsTrue();
    }

    [Test]
    public async Task FrameStats_HoldsFramesToSixteenPointSeven_AndCountsLongFramesFromTheDeltasOnEveryEngine()
    {
        var deltas = new List<double> { 0, 16.7, 17, 51, 50 };

        var stats = FrameStats.Summarize(string.Empty, deltas);

        _ = await Assert.That(stats["frames_within_16_7_pct"]).IsEqualTo(25);
        _ = await Assert.That(stats["long_frames"]).IsEqualTo(1);
    }

    [Test]
    public async Task LayoutShift_SplitsLoadFromScroll_AtTheScrollStart()
    {
        var shifts = new List<LayoutShiftEntry> { new(0.01, 100), new(0.002, 900), new(0.2, 1500) };

        var split = LayoutShift.Split(shifts, scrollFrom: 1000);
        var unscrolled = LayoutShift.Split(shifts, scrollFrom: null);

        _ = await Assert.That(split.Load).IsEqualTo(0.012);
        _ = await Assert.That(split.Scroll).IsEqualTo(0.2);
        _ = await Assert.That(unscrolled.Load).IsEqualTo(0.212);
    }

    [Test]
    public async Task Format_KeepsThreeSignificantDigitsBelowOne_ForScores()
    {
        _ = await Assert.That(BudgetEvaluator.Format(0.0042)).IsEqualTo("0.0042");
        _ = await Assert.That(BudgetEvaluator.Format(0.12345)).IsEqualTo("0.123");
        _ = await Assert.That(BudgetEvaluator.Format(22.456)).IsEqualTo("22.46");
        _ = await Assert.That(BudgetEvaluator.Format(0)).IsEqualTo("0");
        _ = await Assert.That(BudgetEvaluator.Evaluate("cls", 0.0042, 0.0011, new Budget("cls", null, "baseline")).Detail)
            .IsEqualTo("0.0042 > baseline 0.0011 (no increase)");
    }

    [Test]
    public async Task FullMatrix_RunsEveryPageWithImagesOffAndWithReducedMotion()
    {
        var cases = Matrix.Full();

        foreach (var page in Pages.All)
        {
            _ = await Assert.That(cases.Count(item => item.Page == page && !item.ImagesOn)).IsEqualTo(1);
            _ = await Assert.That(cases.Count(item => item.Page == page && item.Motion == Motion.Reduced)).IsEqualTo(1);
        }
    }

    [Test]
    public async Task TraceTotals_SumCompleteAndBeginEndEvents_InMilliseconds()
    {
        using var events = JsonDocument.Parse("""
            [
              {"name":"Paint","ph":"X","dur":2500,"pid":1,"tid":1,"ts":0},
              {"name":"Paint","ph":"X","dur":1500,"pid":1,"tid":1,"ts":9},
              {"name":"RasterTask","ph":"B","pid":1,"tid":2,"ts":100},
              {"name":"RasterTask","ph":"E","pid":1,"tid":2,"ts":3100},
              {"name":"Other","ph":"X","dur":9000,"pid":1,"tid":1,"ts":0}
            ]
            """);

        var totals = TraceTotals.Summarize(events.RootElement.EnumerateArray());

        _ = await Assert.That(totals["trace_paint_ms"]).IsEqualTo(4);
        _ = await Assert.That(totals["trace_raster_ms"]).IsEqualTo(3);
        _ = await Assert.That(totals["trace_layout_ms"]).IsEqualTo(0);
    }

    [Test]
    public async Task GeometryShift_ScoresMovedBoxesByImpactTimesDistance()
    {
        var before = new Dictionary<string, double[]> { ["main#0"] = [0, 0, 100, 100], ["footer#0"] = [0, 100, 100, 100] };
        var still = new Dictionary<string, double[]> { ["main#0"] = [0, 0, 100, 100], ["footer#0"] = [0, 100, 100, 100] };
        var moved = new Dictionary<string, double[]> { ["main#0"] = [0, 0, 100, 100], ["footer#0"] = [0, 150, 100, 100] };

        _ = await Assert.That(GeometryShift.Score(before, still, 100, 200)).IsEqualTo(0);
        _ = await Assert.That(GeometryShift.Score(before, moved, 100, 200)).IsEqualTo(0.125);
    }

    [Test]
    public async Task MatrixRun_OrdersCasesSoEachBrowserRunsOnce_PhoneOnSwiftShaderAndWebkitWithoutAGpuMode()
    {
        var cases = Matrix.Full();

        var ordered = MatrixRun.Order(cases);
        var browsers = ordered.Select(MatrixRun.BrowserFor).ToList();
        var switches = browsers.Zip(browsers.Skip(1)).Count(pair => pair.First != pair.Second);

        _ = await Assert.That(ordered).IsEquivalentTo(cases);
        _ = await Assert.That(switches).IsEqualTo(browsers.Distinct().Count() - 1);
        _ = await Assert.That(MatrixRun.BrowserFor(Search)).IsEqualTo(new BrowserKey(Engine.Chromium, GpuMode.SwiftShader));
        _ = await Assert.That(MatrixRun.BrowserFor(Search with { Engine = Engine.Webkit, Profile = "desktop" })).IsEqualTo(new BrowserKey(Engine.Webkit, null));
    }

    [Test]
    public async Task MatrixRun_ReadsModeAndCheck_AndWritesTheBaselineOnlyInBaselineMode()
    {
        var local = MatrixRun.Settings.Parse(new Dictionary<string, string>());
        var baseline = MatrixRun.Settings.Parse(new Dictionary<string, string> { ["KCC_PERF_MODE"] = "baseline", ["KCC_PERF_CHECK"] = "1" });

        _ = await Assert.That(local.Mode).IsEqualTo("local");
        _ = await Assert.That(local.Check).IsFalse();
        _ = await Assert.That(local.WritesBaseline).IsFalse();
        _ = await Assert.That(baseline.Check).IsTrue();
        _ = await Assert.That(baseline.WritesBaseline).IsTrue();
        _ = await Assert.That(MatrixRun.BaselinePath).EndsWith(Path.Combine("specs", "006-performance-sweep", "baseline.json"));
    }

    [Test]
    public async Task FilteredBaseline_ReplacesOnlyTheCasesItRan()
    {
        var path = Path.Combine(Path.GetTempPath(), $"kcc-baseline-{Guid.NewGuid():N}.json");
        var other = Search with { Engine = Engine.Webkit };
        try
        {
            var full = Reported((Search, 10), (other, 20));
            full.Environment = new() { ["chromium"] = "145", ["webkit"] = "26" };
            ResultsWriter.WriteBaseline(path, full);

            var rerun = Reported((other, 25));
            rerun.Environment = new() { ["chromium"] = "n/a", ["webkit"] = "26.1" };
            ResultsWriter.WriteBaseline(path, rerun, merge: true);

            var baseline = BaselineFile.Load(path);
            _ = await Assert.That(baseline.Find(Search.Identity, "layers")).IsEqualTo(10);
            _ = await Assert.That(baseline.Find(other.Identity, "layers")).IsEqualTo(25);
            _ = await Assert.That(baseline.Environment).IsEquivalentTo(new Dictionary<string, string> { ["chromium"] = "145", ["webkit"] = "26.1" });
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task MatrixRun_MergesTheBaselineOnlyWhenAFilterNarrowsTheMatrix()
    {
        _ = await Assert.That(MatrixRun.Narrowed(CaseFilter.Parse(new Dictionary<string, string>()))).IsFalse();
        _ = await Assert.That(MatrixRun.Narrowed(CaseFilter.Parse(new Dictionary<string, string> { ["ENGINE"] = "webkit" }))).IsTrue();
    }

    [Test]
    public async Task ReportOnlyBudget_NeverFails()
    {
        var budget = new Budget("cls_scroll", null, "report-only", Owner: "phase-2");

        _ = await Assert.That(BudgetEvaluator.Evaluate("cls_scroll", 0.9, 0.1, budget).Status).IsEqualTo(Verdict.ReportOnly);
    }

    [Test]
    public async Task CiBudgets_TakeEachBudgetsCiMode_AndKeepTheRestAsTheyAre()
    {
        var budgets = new BudgetFile(
        [
            new Budget("frame_p95_ms", null, "baseline", Margin: new Margin(Ratio: 1.25, Floor: 4), CiMode: "report-only"),
            new Budget("hydrated_ms", null, "baseline", Margin: new Margin(Ratio: 1.25, Floor: 150)),
        ]);
        var run = new Measurement();
        run.Values["frame_p95_ms"] = 200;
        run.Values["hydrated_ms"] = 2000;
        var baseline = new BaselineFile { Cases = { [Search.Identity] = new() { ["frame_p95_ms"] = 100, ["hydrated_ms"] = 1500 } } };

        var local = new RunReport();
        _ = local.Add(Search, [run]);
        local.Evaluate(budgets, baseline);
        var ci = new RunReport();
        var record = ci.Add(Search, [run]);
        ci.Evaluate(budgets.OnCi(), baseline);

        _ = await Assert.That(local.Failures().Count()).IsEqualTo(2);
        _ = await Assert.That(record.Metrics["frame_p95_ms"].Status).IsEqualTo(Verdict.ReportOnly);
        _ = await Assert.That(ci.Failures()).IsEquivalentTo(["search-3 · phone · chromium · dark · bloomed: hydrated_ms 2000 > baseline 1500 (x1.25, +150 ms)"]);
    }

    [Test]
    public async Task CiBudgets_TakeEachBudgetsCiMargin_AndLocalChecksKeepItsMargin()
    {
        var budgets = new BudgetFile(
        [
            new Budget("hydrated_ms", null, "baseline", Margin: new Margin(Ratio: 1.25, Floor: 150), CiMargin: new Margin(Ratio: 1.5, Floor: 150)),
        ]);
        var run = new Measurement();
        run.Values["hydrated_ms"] = 2000;
        var baseline = new BaselineFile { Cases = { [Search.Identity] = new() { ["hydrated_ms"] = 1500 } } };

        var local = new RunReport();
        _ = local.Add(Search, [run]);
        local.Evaluate(budgets, baseline);
        var ci = new RunReport();
        _ = ci.Add(Search, [run]);
        ci.Evaluate(budgets.OnCi(), baseline);

        _ = await Assert.That(local.Failures()).IsEquivalentTo(["search-3 · phone · chromium · dark · bloomed: hydrated_ms 2000 > baseline 1500 (x1.25, +150 ms)"]);
        _ = await Assert.That(ci.Failures()).IsEmpty();
    }

    [Test]
    public async Task CiGuard_EnforcesOnlyHydratedAndServerResponse_AmongTimingMetrics_AndLocalChecksKeepEveryBudget()
    {
        var budgets = BudgetFile.Load(MatrixRun.BudgetsPath);
        static IEnumerable<string> EnforcedTiming(BudgetFile file) => file.Budgets
            .Where(budget => MetricCatalog.Describe(budget.Metric).Kind == MetricCatalog.Timing && budget.Mode != Verdict.ReportOnly)
            .Select(budget => budget.Metric)
            .Distinct();

        _ = await Assert.That(EnforcedTiming(budgets.OnCi())).IsEquivalentTo(["hydrated_ms", "server_response_ms"]);
        _ = await Assert.That(EnforcedTiming(budgets)).Contains("frame_p95_ms");
        _ = await Assert.That(budgets.Budgets
            .Where(budget => MetricCatalog.Describe(budget.Metric).Kind == MetricCatalog.Timing && budget.Mode == "target")
            .Select(budget => budget.Metric)
            .Distinct()).IsEquivalentTo(["hydrated_ms", "cls", "cls_scroll"]);
        _ = await Assert.That(budgets.Budgets
            .Where(budget => MetricCatalog.Describe(budget.Metric).Kind == MetricCatalog.Deterministic && budget.CiMode is not null)).IsEmpty();
    }

    [Test]
    public async Task Budgets_HoldScriptAndProbeGuardsAtTheirTargets_LocallyAndOnCi()
    {
        var local = BudgetFile.Load(MatrixRun.BudgetsPath);
        var ci = local.OnCi();
        var chromium = Search with { Ramp = Ramp.Light };
        var webkit = chromium with { Engine = Engine.Webkit };
        var bare = chromium with { Visit = VisitState.Bare };

        foreach (var file in new[] { local, ci })
        {
            _ = await Assert.That(file.Find("js_first_load_gz_bytes", webkit)).IsSameReferenceAs(file.Find("js_first_load_gz_bytes", chromium));
            _ = await Assert.That((file.Find("js_first_load_gz_bytes", chromium)!.Mode, file.Find("js_first_load_gz_bytes", chromium)!.Limit)).IsEqualTo(("target", 61_440d));
            _ = await Assert.That((file.Find("js_gz_bytes", webkit)!.Mode, file.Find("js_gz_bytes", webkit)!.Limit)).IsEqualTo(("target", 117_266d));
            _ = await Assert.That(file.Find("js_later_gz_bytes", chromium)!.Mode).IsEqualTo(Verdict.ReportOnly);
            _ = await Assert.That(file.Find("islands_hydrated", webkit)! with { Owner = null }).IsEqualTo(new Budget("islands_hydrated", new Scope(), "baseline", NotARegression: true));
            _ = await Assert.That(file.Find("restyled_on_hover", chromium)!.Mode).IsEqualTo(Verdict.ReportOnly);
            _ = await Assert.That((file.Find("restyled_on_tuck", chromium)!.Mode, file.Find("restyled_on_tuck", bare)!.Limit)).IsEqualTo(("target", 16d));
            _ = await Assert.That(file.Find("restyled_on_tuck", webkit)!.Mode).IsEqualTo(Verdict.ReportOnly);
            _ = await Assert.That((file.Find("wash_raster_on_interaction", chromium)!.Mode, file.Find("wash_raster_on_interaction", chromium)!.Limit)).IsEqualTo(("target", 0d));
            _ = await Assert.That(file.Find("wash_raster_on_interaction", bare)!.Mode).IsEqualTo(Verdict.ReportOnly);
            _ = await Assert.That(file.Find("wash_raster_on_interaction", webkit)!.Mode).IsEqualTo(Verdict.ReportOnly);
        }

        var home = chromium with { Page = Pages.Home };
        foreach (var (metric, limit) in new[] { ("cls", 0.0001), ("cls_scroll", 0d) })
        {
            _ = await Assert.That((local.Find(metric, home)!.Mode, local.Find(metric, home)!.Limit)).IsEqualTo(("target", limit));
            _ = await Assert.That(ci.Find(metric, chromium)!.Mode).IsEqualTo(Verdict.ReportOnly);
        }

        _ = await Assert.That(local.Find("cls", chromium)).IsSameReferenceAs(local.Find("cls", home));
        _ = await Assert.That(local.Find("cls_scroll", chromium)!.Mode).IsEqualTo("baseline");

        _ = await Assert.That(local.Budgets.Where(budget => budget.NotARegression).Select(budget => budget.Metric)).IsEquivalentTo(new[] { "js_later_gz_bytes", "islands_hydrated" });
    }

    [Test]
    public async Task CiBaseline_HoldsOnlyTheScriptBytes_ForEveryWizardGuardCase()
    {
        var baseline = BaselineFile.Load(MatrixRun.CiBaselinePath);

        var wrong = Matrix.DeterministicGuard()
            .Where(Matrix.GuardsScriptBytesOnly)
            .Where(subject => !baseline.Cases.TryGetValue(subject.Identity, out var metrics) || !metrics.Keys.Order().SequenceEqual(Matrix.ScriptBytes.Order()))
            .Select(subject => subject.Identity);

        _ = await Assert.That(wrong).IsEmpty();
    }

    [Test]
    public async Task CiBaseline_HoldsFirstLoadScript_ForEveryKeySubsetCase()
    {
        var baseline = BaselineFile.Load(MatrixRun.CiBaselinePath);

        var missing = Matrix.KeySubset()
            .Where(subject => baseline.Find(subject.Identity, "js_first_load_gz_bytes") is null)
            .Select(subject => subject.Identity);

        _ = await Assert.That(missing).IsEmpty();
    }

    // The phone profile has no hover, so restyled_on_hover reads 0 on every key-subset case and cannot guard it.
    [Test]
    public async Task CiBaseline_HoldsTheTuckAndWashProbes_ForEveryChromiumKeySubsetCase_AndNoHoverProbe()
    {
        var baseline = BaselineFile.Load(MatrixRun.CiBaselinePath);
        var chromium = Matrix.KeySubset().Where(subject => subject.Engine == Engine.Chromium).ToList();

        var missing = chromium
            .SelectMany(subject => new[] { "restyled_on_tuck", "wash_raster_on_interaction" }
                .Where(metric => baseline.Find(subject.Identity, metric) is null)
                .Select(metric => $"{subject.Identity}: {metric}"));
        var hover = Matrix.KeySubset()
            .Where(subject => baseline.Find(subject.Identity, "restyled_on_hover") is not null)
            .Select(subject => subject.Identity);

        _ = await Assert.That(missing).IsEmpty();
        _ = await Assert.That(hover).IsEmpty();
    }

    [Test]
    public async Task CiBaseline_HoldsTheEnforcedTimingMetrics_ForEveryChromiumKeySubsetCase()
    {
        var baseline = BaselineFile.Load(MatrixRun.CiBaselinePath);

        var missing = Matrix.KeySubset()
            .Where(subject => subject.Engine == Engine.Chromium)
            .SelectMany(subject => new[] { "hydrated_ms", "server_response_ms" }
                .Where(metric => baseline.Find(subject.Identity, metric) is null)
                .Select(metric => $"{subject.Identity}: {metric}"));

        _ = await Assert.That(missing).IsEmpty();
    }

    [Test]
    public async Task PlatformSensitiveBudget_IsSkippedWhenTheBaselineCameFromAnotherPlatform()
    {
        var budgets = new BudgetFile([new Budget("layers", null, "baseline", PlatformSensitive: true), new Budget("filtered_elements", null, "baseline")]);
        var run = new Measurement();
        run.Values["layers"] = 99;
        run.Values["filtered_elements"] = 41;
        var baseline = new BaselineFile
        {
            Environment = { ["platform"] = "linux-x64" },
            Cases = { [Search.Identity] = new() { ["layers"] = 70, ["filtered_elements"] = 40 } },
        };

        var elsewhere = new RunReport();
        var record = elsewhere.Add(Search, [run]);
        elsewhere.Evaluate(budgets, baseline, "osx-arm64");
        var same = new RunReport();
        _ = same.Add(Search, [run]);
        same.Evaluate(budgets, baseline, "linux-x64");

        _ = await Assert.That(record.Metrics["layers"].Status).IsEqualTo(Verdict.Skipped);
        _ = await Assert.That(elsewhere.Skips()).IsEquivalentTo(["search-3 · phone · chromium · dark · bloomed: layers skipped: platform"]);
        _ = await Assert.That(elsewhere.Failures()).IsEquivalentTo(["search-3 · phone · chromium · dark · bloomed: filtered_elements 41 > baseline 40 (no increase)"]);
        _ = await Assert.That(same.Failures().Count()).IsEqualTo(2);
    }

    [Test]
    public async Task MatrixRun_ReadsTheRunCount_DefaultingToOne()
    {
        _ = await Assert.That(MatrixRun.Settings.Parse(new Dictionary<string, string>()).Runs).IsEqualTo(1);
        _ = await Assert.That(MatrixRun.Settings.Parse(new Dictionary<string, string> { ["KCC_PERF_RUNS"] = "3" }).Runs).IsEqualTo(3);
        _ = await Assert.That(MatrixRun.Settings.Parse(new Dictionary<string, string> { ["KCC_PERF_RUNS"] = "0" }).Runs).IsEqualTo(1);
    }

    [Test]
    public async Task MetricMergedBaseline_KeepsTheOtherMetricsOfACase()
    {
        var path = Path.Combine(Path.GetTempPath(), $"kcc-baseline-{Guid.NewGuid():N}.json");
        try
        {
            ResultsWriter.WriteBaseline(path, Reported((Search, 10)));
            var timing = new RunReport();
            var run = new Measurement();
            run.Values["frame_p95_ms"] = 20;
            _ = timing.Add(Search, [run]);
            ResultsWriter.WriteBaseline(path, timing, merge: true, mergeMetrics: true);

            var baseline = BaselineFile.Load(path);
            _ = await Assert.That(baseline.Find(Search.Identity, "layers")).IsEqualTo(10);
            _ = await Assert.That(baseline.Find(Search.Identity, "frame_p95_ms")).IsEqualTo(20);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task IdleGpu_IsAShareOfOneCore()
    {
        _ = await Assert.That(MetricCatalog.Describe("idle_gpu_pct").Unit).IsEqualTo("pct-core");
        _ = await Assert.That(ResultsWriter.Environment(null, null, "swiftshader")["platform"]).IsEqualTo(ResultsWriter.Platform);
    }

    [Test]
    public async Task Budgets_HoldPhoneChromiumInteractionAndFrames_AtTheirBaseline_AfterTheGeneralEntries()
    {
        var file = BudgetFile.Load(MatrixRun.BudgetsPath);
        var budgets = file.Budgets.ToList();
        var phoneChromium = new Scope(Profile: "phone", Engine: "chromium");
        string[] carried = ["interaction_ms_max", "frames_within_16_7_pct", "long_frames", "frame_p95_ms"];

        foreach (var metric in carried)
        {
            var general = budgets.Single(budget => budget.Metric == metric && (budget.Scope ?? new Scope()) == new Scope());
            var scoped = budgets.SingleOrDefault(budget => budget.Metric == metric && budget.Scope == phoneChromium);

            _ = await Assert.That(scoped).IsNotNull();
            _ = await Assert.That(scoped! with { Scope = null }).IsEqualTo(general with { Scope = null });
            _ = await Assert.That(budgets.IndexOf(scoped)).IsGreaterThan(budgets.IndexOf(general));
            _ = await Assert.That(file.Find(metric, Search)).IsSameReferenceAs(scoped);
            _ = await Assert.That(file.Find(metric, Search with { Profile = "desktop" })).IsSameReferenceAs(general);
            _ = await Assert.That(file.Find(metric, Search with { Engine = Engine.Webkit })).IsSameReferenceAs(general);
        }
    }

    [Test]
    public async Task Budgets_CoverEveryMetricOfTheDataModel_WithAnOwnerAndAMode()
    {
        string[] dataModel =
        [
            "lcp_ms", "hydrated_ms", "cls", "interaction_ms_max", "frame_p95_ms", "frames_within_16_7_pct", "long_frames", "idle_gpu_pct",
            "layers", "layer_area_mpx", "filtered_elements", "animated_filtered", "filtered_area_px2", "css_gz_bytes", "js_gz_bytes",
            "server_response_ms", "font_bytes", "image_bytes",
        ];
        var budgets = BudgetFile.Load(MatrixRun.BudgetsPath).Budgets;
        Budget Of(string metric) => budgets.Single(budget => budget.Metric == metric && (budget.Scope ?? new Scope()) == new Scope());

        _ = await Assert.That(dataModel.Where(metric => budgets.All(budget => budget.Metric != metric))).IsEmpty();
        _ = await Assert.That(budgets.Where(budget => budget.Owner is not ("phase-1" or "phase-2" or "phase-3"))).IsEmpty();
        _ = await Assert.That(budgets.Where(budget => budget.Mode is not ("target" or "baseline" or "report-only"))).IsEmpty();
        _ = await Assert.That(budgets.Where(budget => MetricCatalog.Describe(budget.Metric).Unit.Length == 0)).IsEmpty();
        foreach (var weight in new[] { "css_gz_bytes", "js_gz_bytes", "font_bytes", "image_bytes" })
        {
            _ = await Assert.That(Of(weight).Margin).IsEqualTo(new Margin(Pct: 2, FloorBytes: 1024));
        }

        _ = await Assert.That(Of("frames_within_16_7_pct").Limit).IsEqualTo(95);
        _ = await Assert.That(Of("long_frames").Limit).IsEqualTo(0);
        _ = await Assert.That(Of("interaction_ms_max").Limit).IsEqualTo(100);
        _ = await Assert.That(Of("cls").Limit).IsEqualTo(0.0001);
        _ = await Assert.That(Of("cls_scroll").Mode).IsEqualTo("target");
        _ = await Assert.That(new[] { "layers", "layer_area_mpx", "filtered_area_px2" }.All(metric => Of(metric).PlatformSensitive)).IsTrue();
    }

    [Test]
    public async Task CiBaseline_HoldsEveryKeySubsetCase_AndNamesItsPlatform()
    {
        var baseline = BaselineFile.Load(MatrixRun.CiBaselinePath);

        _ = await Assert.That(baseline.Platform).IsNotNull();
        _ = await Assert.That(Matrix.KeySubset().Where(subject => !baseline.Cases.ContainsKey(subject.Identity))).IsEmpty();
    }

    [Test]
    public async Task LookCapture_CapturesBareInChromiumOnly_AndBloomedInBothEngines_ForEveryPageRampAndProfile()
    {
        var cases = LookCapture.Cases();

        _ = await Assert.That(cases.Count).IsEqualTo(Pages.All.Count * 2 * 2 * 3);
        _ = await Assert.That(cases.Select(item => item.Identity).Distinct().Count()).IsEqualTo(cases.Count);
        _ = await Assert.That(cases.Where(item => item.Visit == VisitState.Mid)).IsEmpty();
        _ = await Assert.That(cases.Where(item => item.Visit == VisitState.Bare && item.Engine != Engine.Chromium)).IsEmpty();
        _ = await Assert.That(cases.Where(item => !item.ImagesOn || item.Motion != Motion.Full)).IsEmpty();
        _ = await Assert.That(cases.Count(item => item.Visit == VisitState.Bloomed && item.Engine == Engine.Webkit)).IsEqualTo(Pages.All.Count * 2 * 2);
        _ = await Assert.That(cases.Where(item => Pages.NeedsMember(item.Page) != (item.Member != MemberKind.None))).IsEmpty();
    }

    [Test]
    public async Task LookCapture_FreezesOnlyTheBareVisit()
    {
        _ = await Assert.That(LookCapture.Freezes(Search with { Visit = VisitState.Bare })).IsTrue();
        _ = await Assert.That(LookCapture.Freezes(Search)).IsFalse();
    }

    [Test]
    public async Task LookCapture_NamesEachPngByItsCase_PageFirst_WithTheScriptsSuffixLast()
    {
        _ = await Assert.That(LookCapture.FileName(Search)).IsEqualTo("search-3__phone__chromium__dark__bloomed.png");
        _ = await Assert.That(LookCapture.FileName(Search with { Page = Pages.Home, Ramp = Ramp.Light }, LookCapture.ScriptsOff))
            .IsEqualTo("home__phone__chromium__light__bloomed__scripts-off.png");
        _ = await Assert.That(LookCapture.FileName(Search with { Page = Pages.Home }, LookCapture.ScriptsOn))
            .IsEqualTo("home__phone__chromium__dark__bloomed__scripts-on.png");
        _ = await Assert.That(LookCapture.FileName(Search with { ImagesOn = false, Member = MemberKind.E2e }))
            .IsEqualTo("search-3__phone__chromium__dark__bloomed__images-off__member-e2e.png");
    }

    [Test]
    public async Task LookCapture_WritesUnderTheCapturesFolderOfTheNamedRef_AndRefusesAMissingOrUnsafeRef()
    {
        _ = await Assert.That(LookCapture.Directory("before"))
            .IsEqualTo(Path.Combine(RepoPaths.Root, "tests", "results", "perf", "captures", "before"));
        _ = await Assert.That(LookCapture.Directory("after-2")).EndsWith(Path.Combine("captures", "after-2"));
        _ = await Assert.That(() => LookCapture.Directory(null)).Throws<InvalidOperationException>();
        _ = await Assert.That(() => LookCapture.Directory("")).Throws<InvalidOperationException>();
        _ = await Assert.That(() => LookCapture.Directory("../before")).Throws<InvalidOperationException>();
        _ = await Assert.That(() => LookCapture.Directory("a/b")).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task LookCapture_ComparesScriptsOffWhenBloomed_SignedOutInLightOnly_AndMembersInTheirSavedRamp()
    {
        var compared = LookCapture.Cases().Where(LookCapture.ComparesScriptsOff).ToList();
        var signedOut = Pages.All.Where(page => !Pages.NeedsMember(page) && page != Pages.Search3).ToList();
        var member = Pages.All.Where(page => Pages.NeedsMember(page) && page != Pages.AddVariant).ToList();

        _ = await Assert.That(compared.All(item => item.Visit == VisitState.Bloomed)).IsTrue();
        _ = await Assert.That(compared.Where(item => item.Member == MemberKind.None && item.Ramp == Ramp.Dark)).IsEmpty();
        _ = await Assert.That(compared.Any(item => item.Member != MemberKind.None && item.Ramp == Ramp.Dark)).IsTrue();
        _ = await Assert.That(compared.Where(item => item.Page is Pages.Search3 or Pages.AddVariant)).IsEmpty();
        _ = await Assert.That(compared.Count).IsEqualTo((signedOut.Count * 1 + member.Count * 2) * 2 * 2);
    }

    [Test]
    public async Task LookCapture_ShootsChromiumScriptsOffPairsInSoftwareRaster_AndWebKitAsItIs()
    {
        _ = await Assert.That(LookCapture.ScriptsOffGpu(Engine.Chromium)).IsEqualTo(GpuMode.SwiftShader);
        _ = await Assert.That(LookCapture.ScriptsOffGpu(Engine.Webkit)).IsNull();
    }

    [Test]
    public async Task LookCapture_SignsInOnlyMemberCases_SoASignedOutCaseNeverCarriesTheMembersSavedRamp()
    {
        const string memberState = "{\"cookies\":[{\"name\":\"kcc-ramp\",\"value\":\"light\"}]}";

        _ = await Assert.That(LookCapture.StorageStateFor(Search with { Page = Pages.Home }, memberState)).IsNull();
        _ = await Assert.That(LookCapture.StorageStateFor(Search with { Page = Pages.Account, Member = MemberKind.E2e }, memberState))
            .IsEqualTo(memberState);
    }

    [Test]
    public async Task LookCapture_OrdersEachBrowsersCasesByRamp_SoTheMembersSavedRampChangesOncePerBrowser()
    {
        var cases = LookCapture.Cases();

        var ordered = LookCapture.Order(cases);
        var browsers = ordered.Select(MatrixRun.BrowserFor).ToList();
        var steps = ordered.Zip(ordered.Skip(1)).ToList();

        _ = await Assert.That(ordered).IsEquivalentTo(cases);
        _ = await Assert.That(browsers.Zip(browsers.Skip(1)).Count(pair => pair.First != pair.Second)).IsEqualTo(browsers.Distinct().Count() - 1);
        _ = await Assert.That(steps.Count(pair => MatrixRun.BrowserFor(pair.First) == MatrixRun.BrowserFor(pair.Second) && pair.First.Ramp != pair.Second.Ramp))
            .IsEqualTo(browsers.Distinct().Count());
    }

    private static readonly Dictionary<string, string> LiveVariables = new()
    {
        ["KCC_PERF_LIVE_URL"] = "https://kcc.example.test/some/path",
        ["KCC_PERF_MEMBER_USERNAME"] = "measure@example.test",
        ["KCC_PERF_MEMBER_PASSWORD"] = "pw-secret-value",
        ["KCC_PERF_BYPASS_TOKEN"] = "token-secret-value",
    };

    [Test]
    public async Task LiveSite_NamesEveryMissingVariable_AndNoValue()
    {
        var partial = new Dictionary<string, string>(LiveVariables) { ["KCC_PERF_MEMBER_PASSWORD"] = "", ["KCC_PERF_BYPASS_TOKEN"] = " " };
        partial.Remove("KCC_PERF_LIVE_URL");

        var error = await Assert.That(() => LiveSite.Parse(partial)).Throws<InvalidOperationException>();

        _ = await Assert.That(error!.Message).Contains("KCC_PERF_LIVE_URL");
        _ = await Assert.That(error!.Message).Contains("KCC_PERF_MEMBER_PASSWORD");
        _ = await Assert.That(error!.Message).Contains("KCC_PERF_BYPASS_TOKEN");
        _ = await Assert.That(error!.Message).DoesNotContain("KCC_PERF_MEMBER_USERNAME");
        _ = await Assert.That(error!.Message).DoesNotContain("measure@example.test");
    }

    [Test]
    public async Task LiveSite_RefusesAUrlThatIsNotAnHttpsOrigin_WithoutEchoingIt()
    {
        foreach (var url in new[] { "kcc.example.test", "ftp://kcc.example.test", "/relative", "http://kcc.example.test" })
        {
            var error = await Assert.That(() => LiveSite.Parse(new Dictionary<string, string>(LiveVariables) { ["KCC_PERF_LIVE_URL"] = url })).Throws<InvalidOperationException>();

            _ = await Assert.That(error!.Message).Contains("KCC_PERF_LIVE_URL");
            _ = await Assert.That(error!.Message).DoesNotContain(url);
        }
    }

    [Test]
    public async Task LiveSite_KeepsOnlyTheOrigin_AndWithholdsItAndTheTokenFromToString()
    {
        var live = LiveSite.Parse(LiveVariables);

        _ = await Assert.That(live.Origin).IsEqualTo(new Uri("https://kcc.example.test/"));
        _ = await Assert.That(live.ToString()).DoesNotContain("kcc.example.test");
        _ = await Assert.That(live.ToString()).DoesNotContain("token-secret-value");
    }

    [Test]
    public async Task LiveSite_UsesTheSeededRecipePaths_UnlessTheOwnerNamesLiveOnes()
    {
        var seeded = LiveSite.Parse(LiveVariables);
        var named = LiveSite.Parse(new Dictionary<string, string>(LiveVariables)
        {
            ["KCC_PERF_LIVE_RECIPE"] = "/recipes/sunday-roast/",
            ["KCC_PERF_LIVE_VARIANT"] = "/recipes/sunday-roast/lamb",
        });

        _ = await Assert.That(seeded.Paths with { ExactSearchCount = true }).IsEqualTo(SitePaths.Seeded);
        _ = await Assert.That(seeded.Paths.ExactSearchCount).IsFalse();
        _ = await Assert.That(CasePages.Path(Pages.Recipe, named.Paths)).IsEqualTo("/recipes/sunday-roast/");
        _ = await Assert.That(CasePages.Path(Pages.AddVariant, named.Paths)).IsEqualTo("/recipes/sunday-roast/");
        _ = await Assert.That(CasePages.Path(Pages.VariantCook, named.Paths)).IsEqualTo("/recipes/sunday-roast/lamb");
        _ = await Assert.That(CasePages.Path(Pages.Recipe)).IsEqualTo(SitePaths.Seeded.Recipe);
    }

    [Test]
    public async Task LiveSite_RefusesALiveRecipeOrVariantThatIsNotASitePath_WithoutEchoingIt()
    {
        foreach (var name in new[] { "KCC_PERF_LIVE_RECIPE", "KCC_PERF_LIVE_VARIANT" })
        {
            foreach (var path in new[] { "https://elsewhere.example.test/recipes/x", "//elsewhere.example.test/recipes/x", "/\\elsewhere.example.test/x", "recipes/x" })
            {
                var error = await Assert.That(() => LiveSite.Parse(new Dictionary<string, string>(LiveVariables) { [name] = path }))
                    .Throws<InvalidOperationException>();

                _ = await Assert.That(error!.Message).Contains(name);
                _ = await Assert.That(error!.Message).DoesNotContain(path);
            }
        }
    }

    [Test]
    public async Task LiveSite_SendsTheBypassHeaderOnlyToItsOwnOrigin()
    {
        var live = LiveSite.Parse(LiveVariables);

        _ = await Assert.That(live.Decide("GET", "https://kcc.example.test/recipes/", signingIn: false)).IsEqualTo(new LiveRequest(Allowed: true, Bypass: true));
        _ = await Assert.That(live.Decide("GET", "https://cdn.example.test/font.woff2", signingIn: false)).IsEqualTo(new LiveRequest(Allowed: true, Bypass: false));
        _ = await Assert.That(live.Decide("GET", "http://kcc.example.test/", signingIn: false).Bypass).IsFalse();
        _ = await Assert.That(live.Decide("GET", "https://kcc.example.test.evil.test/", signingIn: false).Bypass).IsFalse();
        _ = await Assert.That(live.WithBypass(new Dictionary<string, string> { ["accept"] = "text/html" }))
            .IsEquivalentTo(new Dictionary<string, string> { ["accept"] = "text/html", ["X-KCC-Perf"] = "token-secret-value" });
    }

    [Test]
    public async Task LiveSite_AllowsOnlyReads_AndTheSignInPostWhileSigningIn()
    {
        var live = LiveSite.Parse(LiveVariables);

        _ = await Assert.That(live.Decide("HEAD", "https://kcc.example.test/", signingIn: false).Allowed).IsTrue();
        foreach (var method in new[] { "POST", "PUT", "PATCH", "DELETE" })
        {
            _ = await Assert.That(live.Decide(method, "https://kcc.example.test/api/x", signingIn: false).Allowed).IsFalse();
        }

        _ = await Assert.That(live.Decide("POST", "https://kcc.example.test/api/account/login", signingIn: true).Allowed).IsTrue();
        _ = await Assert.That(live.Decide("POST", "https://kcc.example.test/api/account/register", signingIn: true).Allowed).IsFalse();
        _ = await Assert.That(live.Decide("POST", "https://kcc.example.test/api/x", signingIn: true).Allowed).IsFalse();
        _ = await Assert.That(live.Decide("POST", "https://other.example.test/collect", signingIn: true).Allowed).IsFalse();
        _ = await Assert.That(live.Decide("DELETE", "https://kcc.example.test/api/x", signingIn: true).Allowed).IsFalse();
    }

    [Test]
    public async Task LiveSite_RedactsItsOriginAndToken_FromErrorText()
    {
        var live = LiveSite.Parse(LiveVariables);

        var redacted = live.Redact("page.goto: net::ERR_ABORTED at https://kcc.example.test/recipes/ (kcc.example.test, token-secret-value)");

        _ = await Assert.That(redacted).DoesNotContain("kcc.example.test");
        _ = await Assert.That(redacted).DoesNotContain("token-secret-value");
        _ = await Assert.That(redacted).Contains("/recipes/");
    }

    [Test]
    public async Task MatrixRun_InLiveMode_NeverChecksBudgets_AndComparesWithNoBaseline()
    {
        var live = MatrixRun.Settings.Parse(new Dictionary<string, string> { ["KCC_PERF_MODE"] = "live", ["KCC_PERF_CHECK"] = "1" });
        var local = MatrixRun.Settings.Parse(new Dictionary<string, string>());

        _ = await Assert.That(live.IsLive).IsTrue();
        _ = await Assert.That(live.Check).IsFalse();
        _ = await Assert.That(live.WritesBaseline).IsFalse();
        _ = await Assert.That(live.Member).IsEqualTo(MemberKind.Live);
        _ = await Assert.That(local.IsLive).IsFalse();
        _ = await Assert.That(local.Member).IsEqualTo(MemberKind.E2e);
    }

    [Test]
    public async Task LiveMatrix_RunsEveryCaseOfTheLocalMatrix_WithTheMeasurementMemberOnMemberPages()
    {
        var local = Matrix.Full();
        var live = Matrix.Full(MemberKind.Live);

        _ = await Assert.That(live.Select(item => item.Identity)).IsEquivalentTo(local.Select(item => item.Identity));
        _ = await Assert.That(live.Where(item => item.Member == MemberKind.E2e)).IsEmpty();
        _ = await Assert.That(live.Where(item => Pages.NeedsMember(item.Page)).All(item => item.Member == MemberKind.Live)).IsTrue();
    }

    [Test]
    public async Task MemberStates_InLiveMode_SignInOnceForTheWholeRun()
    {
        var chromium = new BrowserKey(Engine.Chromium, GpuMode.SwiftShader);
        var webkit = new BrowserKey(Engine.Webkit, null);
        var live = new MemberStates(once: true);
        var local = new MemberStates(once: false);
        var liveSignIns = 0;
        var localSignIns = 0;

        foreach (var key in new[] { chromium, webkit, chromium })
        {
            _ = await live.GetAsync(key, () => Task.FromResult($"live {++liveSignIns}"));
            _ = await local.GetAsync(key, () => Task.FromResult($"local {++localSignIns}"));
        }

        live.Forget(webkit);
        local.Forget(webkit);
        _ = await live.GetAsync(webkit, () => Task.FromResult($"live {++liveSignIns}"));
        _ = await local.GetAsync(webkit, () => Task.FromResult($"local {++localSignIns}"));

        _ = await Assert.That(liveSignIns).IsEqualTo(1);
        _ = await Assert.That(localSignIns).IsEqualTo(3);
    }

    [Test]
    public async Task ServerResponse_MeasuresEachSelectedPathOnce_TenTimes()
    {
        var all = ServerResponse.Pages(CaseFilter.Parse(new Dictionary<string, string>()), SitePaths.Seeded);
        var named = ServerResponse.Pages(CaseFilter.Parse(new Dictionary<string, string> { ["PAGE"] = "search-3,account" }), SitePaths.Seeded);

        var runs = ServerResponse.Runs;

        _ = await Assert.That(runs).IsEqualTo(10);
        _ = await Assert.That(all.Select(page => CasePages.Path(page, SitePaths.Seeded)).Distinct().Count()).IsEqualTo(all.Count);
        _ = await Assert.That(all).Contains(Pages.Search1);
        _ = await Assert.That(all).DoesNotContain(Pages.Search3);
        _ = await Assert.That(named).IsEquivalentTo([Pages.Search3, Pages.Account]);
    }

    [Test]
    public async Task ServerResponse_SummarisesTheRunsAsARange_WithEachRunsCacheStatusAndEncoding()
    {
        var record = ServerResponseRecord.From(
            "/recipes/",
            MemberKind.None,
            [new(120, "HIT", "br"), new(80, "MISS", "br"), new(300, null, null), new(95, "DYNAMIC", "gzip")]);

        _ = await Assert.That(record.MinMs).IsEqualTo(80);
        _ = await Assert.That(record.MedianMs).IsEqualTo(107.5);
        _ = await Assert.That(record.MaxMs).IsEqualTo(300);
        _ = await Assert.That(record.RunsMs).IsEquivalentTo([120.0, 80, 300, 95]);
        _ = await Assert.That(record.CfCacheStatus).IsEquivalentTo(["HIT", "MISS", "none", "DYNAMIC"]);
        _ = await Assert.That(record.ContentEncoding).IsEquivalentTo(["br", "br", "identity", "gzip"]);
        _ = await Assert.That(record.Member).IsEqualTo("none");
    }

    [Test]
    public async Task Results_WriteTheServerResponseSeries_OnlyWhenTheRunHasOne()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"kcc-results-{Guid.NewGuid():N}");
        try
        {
            var local = Reported((Search, 10));
            using (var written = JsonDocument.Parse(File.ReadAllText(ResultsWriter.WriteResults(directory, local))))
            {
                _ = await Assert.That(written.RootElement.TryGetProperty("serverResponse", out _)).IsFalse();
            }

            local.ServerResponse = new() { [Pages.Home] = ServerResponseRecord.From("/", MemberKind.None, [new(50, "HIT", "br")]) };
            using var live = JsonDocument.Parse(File.ReadAllText(ResultsWriter.WriteResults(directory, local)));
            var home = live.RootElement.GetProperty("serverResponse").GetProperty("home");
            _ = await Assert.That(home.GetProperty("medianMs").GetDouble()).IsEqualTo(50);
            _ = await Assert.That(home.GetProperty("cfCacheStatus")[0].GetString()).IsEqualTo("HIT");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    public async Task CasePages_TreatsA404AsAMissingRecipe_OnlyOnTheRecipePages()
    {
        _ = await Assert.That(CasePages.MissingContent(Pages.Recipe, 404)).IsTrue();
        _ = await Assert.That(CasePages.MissingContent(Pages.VariantCook, 404)).IsTrue();
        _ = await Assert.That(CasePages.MissingContent(Pages.AddVariant, 404)).IsTrue();
        _ = await Assert.That(CasePages.MissingContent(Pages.Recipe, 200)).IsFalse();
        _ = await Assert.That(CasePages.MissingContent(Pages.Status404, 404)).IsFalse();
        _ = await Assert.That(CasePages.MissingContent(Pages.Error, 404)).IsFalse();
    }

    [Test]
    public async Task CasePages_ReadsAMemberCaseOnTheSignInPage_AsASessionTheSiteRefused()
    {
        var account = new Case(Pages.Account, "phone", Engine.Chromium, Ramp.Light, VisitState.Bare, Member: MemberKind.Live);

        _ = await Assert.That(CasePages.SessionRefused(account, "https://kcc.example.test/account/login?returnUrl=%2Faccount")).IsTrue();
        _ = await Assert.That(CasePages.SessionRefused(account, "https://kcc.example.test/account")).IsFalse();
        _ = await Assert.That(CasePages.SessionRefused(account with { Page = Pages.SignIn, Member = MemberKind.None }, "https://kcc.example.test/account/login")).IsFalse();
    }

    private static RunReport Reported(params (Case Subject, double Layers)[] cases)
    {
        var report = new RunReport();
        foreach (var (subject, layers) in cases)
        {
            var run = new Measurement();
            run.Values["layers"] = layers;
            _ = report.Add(subject, [run]);
        }

        return report;
    }
}
