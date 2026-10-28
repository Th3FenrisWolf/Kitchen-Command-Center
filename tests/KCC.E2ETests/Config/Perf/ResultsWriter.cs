using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KCC.E2ETests.Config.Perf;

public sealed record MetricInfo(string Kind, string Unit, bool HigherIsBetter = false);

public static class MetricCatalog
{
    public const string Deterministic = "deterministic";

    public const string Timing = "timing";

    private static readonly Dictionary<string, MetricInfo> Known = new()
    {
        ["lcp_ms"] = new(Timing, "ms"),
        ["hydrated_ms"] = new(Timing, "ms"),
        ["server_response_ms"] = new(Timing, "ms"),
        ["cls"] = new(Timing, "score"),
        ["cls_scroll"] = new(Timing, "score"),
        ["cls_geometry"] = new(Timing, "score"),
        ["interaction_ms_max"] = new(Timing, "ms"),
        ["frame_p95_ms"] = new(Timing, "ms"),
        ["frame_max_ms"] = new(Timing, "ms"),
        ["frames_within_16_7_pct"] = new(Timing, "pct", HigherIsBetter: true),
        ["long_frames"] = new(Timing, "count"),
        ["loaf_count"] = new(Timing, "count"),
        ["bloom_frame_p95_ms"] = new(Timing, "ms"),
        ["bloom_frame_max_ms"] = new(Timing, "ms"),
        ["bloom_frames_within_16_7_pct"] = new(Timing, "pct", HigherIsBetter: true),
        ["bloom_long_frames"] = new(Timing, "count"),
        ["bloom_loaf_count"] = new(Timing, "count"),
        ["idle_gpu_pct"] = new(Timing, "pct-core"),
        ["trace_paint_ms"] = new(Timing, "ms"),
        ["trace_raster_ms"] = new(Timing, "ms"),
        ["trace_composite_ms"] = new(Timing, "ms"),
        ["trace_update_layer_tree_ms"] = new(Timing, "ms"),
        ["trace_layout_ms"] = new(Timing, "ms"),
        ["css_unused_bytes"] = new(Deterministic, "bytes"),
        ["css_unused_pct"] = new(Deterministic, "pct"),
        ["layers"] = new(Deterministic, "count"),
        ["layer_area_mpx"] = new(Deterministic, "mpx"),
        ["filtered_elements"] = new(Deterministic, "count"),
        ["clip_path_elements"] = new(Deterministic, "count"),
        ["blend_elements"] = new(Deterministic, "count"),
        ["backdrop_elements"] = new(Deterministic, "count"),
        ["animations_running"] = new(Deterministic, "count"),
        ["animated_filtered"] = new(Deterministic, "count"),
        ["animated_under_filter"] = new(Deterministic, "count"),
        ["filtered_area_px2"] = new(Deterministic, "px2"),
        ["css_gz_bytes"] = new(Deterministic, "bytes"),
        ["js_gz_bytes"] = new(Deterministic, "bytes"),
        ["font_bytes"] = new(Deterministic, "bytes"),
        ["image_bytes"] = new(Deterministic, "bytes"),
    };

    public static MetricInfo Describe(string metric) =>
        Known.TryGetValue(metric, out var info) ? info : new(Timing, string.Empty);
}

public sealed record Margin(double? Ratio = null, double? Floor = null, double? Pct = null, double? FloorBytes = null, double? Add = null);

public sealed record Scope(string? Page = null, string? Profile = null, string? Engine = null, string? Ramp = null, string? Visit = null)
{
    public bool Matches(Case value) =>
        (Page is null || Page == value.Page)
        && (Profile is null || Profile == value.Profile)
        && (Engine is null || Engine == Case.Wire(value.Engine))
        && (Ramp is null || Ramp == Case.Wire(value.Ramp))
        && (Visit is null || Visit == Case.Wire(value.Visit));
}

public sealed record Budget(
    string Metric,
    Scope? Scope,
    string Mode,
    double? Limit = null,
    Margin? Margin = null,
    string? Owner = null,
    bool PlatformSensitive = false,
    string? CiMode = null);

public sealed record BudgetFile(IReadOnlyList<Budget> Budgets)
{
    public static readonly BudgetFile Empty = new([]);

    public static BudgetFile Load(string path) =>
        File.Exists(path)
            ? JsonSerializer.Deserialize<BudgetFile>(File.ReadAllText(path), ResultsWriter.Json) ?? Empty
            : Empty;

    public BudgetFile OnCi() => new([.. Budgets.Select(budget => budget.CiMode is { } mode ? budget with { Mode = mode } : budget)]);

    public Budget? Find(string metric, Case value) =>
        Budgets.LastOrDefault(budget => budget.Metric == metric && (budget.Scope ?? new Scope()).Matches(value));
}

public sealed record Verdict(string Status, string? Detail)
{
    public const string Pass = "pass";

    public const string Fail = "fail";

    public const string ReportOnly = "report-only";

    public const string Skipped = "skipped";
}

public static class BudgetEvaluator
{
    public static Verdict Evaluate(string metric, double value, double? baseline, Budget? budget)
    {
        if (budget is null || budget.Mode == Verdict.ReportOnly)
        {
            return new(Verdict.ReportOnly, null);
        }

        var info = MetricCatalog.Describe(metric);
        return budget.Mode == "target" ? Target(value, budget, info) : Baseline(value, baseline, budget, info);
    }

    public static string Format(double value)
    {
        var magnitude = value == 0 ? 0 : (int)Math.Floor(Math.Log10(Math.Abs(value)));
        return value.ToString("0." + new string('#', Math.Max(2, 2 - magnitude)), CultureInfo.InvariantCulture);
    }

    private static Verdict Target(double value, Budget budget, MetricInfo info)
    {
        if (budget.Limit is not { } limit)
        {
            return new(Verdict.ReportOnly, null);
        }

        var worse = info.HigherIsBetter ? value < limit : value > limit;
        return worse
            ? new(Verdict.Fail, $"{Format(value)} {(info.HigherIsBetter ? "<" : ">")} target {Format(limit)}")
            : new(Verdict.Pass, null);
    }

    private static Verdict Baseline(double value, double? baseline, Budget budget, MetricInfo info)
    {
        if (baseline is not { } reference)
        {
            return new(Verdict.ReportOnly, null);
        }

        var margin = budget.Margin ?? new Margin();
        var (allowance, label) = Allowance(reference, margin, info);
        var worse = info.HigherIsBetter ? value < reference - allowance : value > reference + allowance;
        return worse
            ? new(Verdict.Fail, $"{Format(value)} {(info.HigherIsBetter ? "<" : ">")} baseline {Format(reference)} ({label})")
            : new(Verdict.Pass, null);
    }

    private static (double Allowance, string Label) Allowance(double baseline, Margin margin, MetricInfo info)
    {
        var suffix = info.Unit switch { "ms" => " ms", "bytes" => " B", _ => string.Empty };
        if (margin.Ratio is { } ratio)
        {
            var floor = margin.Floor ?? 0;
            return (Math.Max((baseline * ratio) - baseline, floor), $"x{Format(ratio)}, +{Format(floor)}{suffix}");
        }

        if (margin.Pct is { } pct)
        {
            var floor = margin.FloorBytes ?? 0;
            return (Math.Max(baseline * pct / 100, floor), $"+{Format(pct)}%, +{Format(floor)}{suffix} floor");
        }

        if (margin.Add is { } add)
        {
            return (add, $"+{Format(add)}");
        }

        return (0, "no increase");
    }
}

public sealed class MetricRecord
{
    public double Value { get; set; }

    public string Kind { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public List<double>? Runs { get; set; }

    public Budget? Budget { get; set; }

    public double? Baseline { get; set; }

    public double? Delta { get; set; }

    public string Status { get; set; } = Verdict.ReportOnly;

    [JsonIgnore]
    public string? Detail { get; set; }
}

public sealed class CaseRecord
{
    public CaseRecord(Case subject) => Subject = subject;

    [JsonIgnore]
    public Case Subject { get; }

    public Dictionary<string, MetricRecord> Metrics { get; } = [];

    public List<string> Gaps { get; } = [];
}

public sealed class RunReport
{
    public DateTimeOffset Captured { get; set; } = DateTimeOffset.UtcNow;

    public string Commit { get; set; } = string.Empty;

    public Dictionary<string, string> Environment { get; set; } = [];

    public List<CaseRecord> Cases { get; } = [];

    public Dictionary<string, ServerResponseRecord>? ServerResponse { get; set; }

    public CaseRecord Add(Case subject, IReadOnlyList<Measurement> runs)
    {
        var record = new CaseRecord(subject);
        foreach (var name in runs.SelectMany(run => run.Values.Keys).Distinct())
        {
            var values = runs.Where(run => run.Values.ContainsKey(name)).Select(run => run.Values[name]).ToList();
            var info = MetricCatalog.Describe(name);
            record.Metrics[name] = new()
            {
                Kind = info.Kind,
                Unit = info.Unit,
                Value = info.Kind == MetricCatalog.Timing ? ResultsWriter.Median(values) : values[0],
                Runs = info.Kind == MetricCatalog.Timing ? values : null,
            };
        }

        record.Gaps.AddRange(runs.SelectMany(run => run.Gaps).Distinct());
        Cases.Add(record);
        return record;
    }

    public void Evaluate(BudgetFile budgets, BaselineFile baseline, string? platform = null)
    {
        var otherPlatform = platform is not null && baseline.Platform != platform;
        foreach (var record in Cases)
        {
            foreach (var (name, metric) in record.Metrics)
            {
                var reference = baseline.Find(record.Subject.Identity, name);
                var budget = budgets.Find(name, record.Subject);
                var verdict = otherPlatform && budget is { PlatformSensitive: true }
                    ? new Verdict(Verdict.Skipped, "platform")
                    : BudgetEvaluator.Evaluate(name, metric.Value, reference, budget);
                metric.Budget = budget;
                metric.Baseline = reference;
                metric.Delta = reference is { } was ? Math.Round(metric.Value - was, 4) : null;
                metric.Status = verdict.Status;
                metric.Detail = verdict.Detail;
            }
        }
    }

    public IEnumerable<string> Failures() =>
        Cases.SelectMany(record => record.Metrics
            .Where(pair => pair.Value.Status == Verdict.Fail)
            .Select(pair => $"{record.Subject.Identity}: {pair.Key} {pair.Value.Detail}"));

    public IEnumerable<string> MissingFromBaseline(BudgetFile budgets, BaselineFile baseline, IReadOnlySet<string> kinds) =>
        Cases.SelectMany(record => baseline.Cases.GetValueOrDefault(record.Subject.Identity, [])
            .Keys
            .Where(name => kinds.Contains(MetricCatalog.Describe(name).Kind)
                && !record.Metrics.ContainsKey(name)
                && budgets.Find(name, record.Subject) is { } budget
                && budget.Mode != Verdict.ReportOnly)
            .Select(name => $"{record.Subject.Identity}: {name} missing from this run (in {Path.GetFileName(MatrixRun.CiBaselinePath)})"));

    public IEnumerable<string> Skips() =>
        Cases.SelectMany(record => record.Metrics
            .Where(pair => pair.Value.Status == Verdict.Skipped)
            .Select(pair => $"{record.Subject.Identity}: {pair.Key} skipped: {pair.Value.Detail}"));
}

public sealed class BaselineFile
{
    public static readonly BaselineFile Empty = new();

    public DateTimeOffset? Captured { get; set; }

    public string? Commit { get; set; }

    public Dictionary<string, string> Environment { get; set; } = [];

    public Dictionary<string, Dictionary<string, double>> Cases { get; set; } = [];

    [JsonIgnore]
    public string? Platform => Environment.GetValueOrDefault("platform");

    public static BaselineFile Load(string path) =>
        File.Exists(path)
            ? JsonSerializer.Deserialize<BaselineFile>(File.ReadAllText(path), ResultsWriter.Json) ?? Empty
            : Empty;

    public double? Find(string identity, string metric) =>
        Cases.TryGetValue(identity, out var metrics) && metrics.TryGetValue(metric, out var value) ? value : null;
}

public static class ResultsWriter
{
    public const string LiveMode = "live";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string OutputDirectory(string mode) =>
        Path.Combine(RepoPaths.Root, "tests", "results", mode == LiveMode ? "perf-live" : "perf");

    public static double Median(IReadOnlyList<double> values)
    {
        var sorted = values.Order().ToList();
        var middle = sorted.Count / 2;
        return sorted.Count % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2;
    }

    public static string WriteResults(string directory, RunReport report)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "results.json");
        var document = new
        {
            captured = report.Captured,
            commit = report.Commit,
            environment = report.Environment,
            cases = report.Cases.ToDictionary(record => record.Subject.Identity, record => record.Metrics),
            gaps = report.Cases.Where(record => record.Gaps.Count > 0).ToDictionary(record => record.Subject.Identity, record => record.Gaps),
            serverResponse = report.ServerResponse,
        };
        File.WriteAllText(path, JsonSerializer.Serialize(document, Json));
        return path;
    }

    public static void WriteBaseline(string path, RunReport report, bool merge = false, bool mergeMetrics = false)
    {
        var previous = merge ? BaselineFile.Load(path) : BaselineFile.Empty;
        var cases = new Dictionary<string, Dictionary<string, double>>(previous.Cases);
        foreach (var record in report.Cases)
        {
            var metrics = mergeMetrics && cases.TryGetValue(record.Subject.Identity, out var kept) ? new Dictionary<string, double>(kept) : [];
            foreach (var (name, metric) in record.Metrics)
            {
                metrics[name] = metric.Value;
            }

            cases[record.Subject.Identity] = metrics;
        }

        var baseline = new BaselineFile
        {
            Captured = report.Captured,
            Commit = report.Commit,
            Environment = MergeEnvironment(previous.Environment, report.Environment),
            Cases = cases,
        };
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(baseline, Json));
    }

    private static Dictionary<string, string> MergeEnvironment(Dictionary<string, string> previous, Dictionary<string, string> current)
    {
        var merged = new Dictionary<string, string>(previous);
        foreach (var (key, value) in current.Where(pair => pair.Value != "n/a" || !previous.ContainsKey(pair.Key)))
        {
            merged[key] = value;
        }

        return merged;
    }

    public static string Platform => System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier;

    public static Dictionary<string, string> Environment(string? chromiumVersion, string? webkitVersion, string gpu) => new()
    {
        ["platform"] = Platform,
        ["chromium"] = chromiumVersion ?? "n/a",
        ["webkit"] = webkitVersion ?? "n/a",
        ["gpu"] = gpu,
        ["machine"] = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
        ["ci"] = (System.Environment.GetEnvironmentVariable("CI") is { Length: > 0 }).ToString(),
    };

    public static string Commit()
    {
        try
        {
            using var git = Process.Start(new ProcessStartInfo("git", ["rev-parse", "HEAD"])
            {
                WorkingDirectory = RepoPaths.Root,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            });
            var sha = git?.StandardOutput.ReadToEnd().Trim();
            git?.WaitForExit();
            return string.IsNullOrEmpty(sha) ? "unknown" : sha;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return "unknown";
        }
    }
}
