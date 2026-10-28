namespace KCC.E2ETests.Config.Perf;

public sealed record BrowserKey(Engine Engine, GpuMode? Gpu);

public static class MatrixRun
{
    public const string ModeVariable = "KCC_PERF_MODE";

    public const string CheckVariable = "KCC_PERF_CHECK";

    public const string RunsVariable = "KCC_PERF_RUNS";

    public static string BaselinePath => Path.Combine(RepoPaths.Root, "specs", "006-performance-sweep", "baseline.json");

    public static string CiBaselinePath => Path.Combine(RepoPaths.Root, "tests", "KCC.E2ETests", "Features", "Performance", "baseline.ci.json");

    public static string BudgetsPath => Path.Combine(RepoPaths.Root, "tests", "KCC.E2ETests", "Features", "Performance", "budgets.json");

    public static BrowserKey BrowserFor(Case subject) =>
        subject.Engine == Engine.Chromium ? new(Engine.Chromium, Profiles.ByName(subject.Profile).Gpu) : new(subject.Engine, null);

    public static bool Narrowed(CaseFilter filter) =>
        filter.Pages.Count + filter.Profiles.Count + filter.Engines.Count + filter.Ramps.Count + filter.Visits.Count > 0;

    public static IReadOnlyList<Case> Order(IEnumerable<Case> cases) => [.. cases.GroupBy(BrowserFor).SelectMany(group => group)];

    public sealed record Settings(string Mode, bool Check, int Runs = 1)
    {
        public bool WritesBaseline => Mode == "baseline";

        public bool IsLive => Mode == ResultsWriter.LiveMode;

        public MemberKind Member => IsLive ? MemberKind.Live : MemberKind.E2e;

        public static Settings FromEnvironment() => Parse(new Dictionary<string, string>
        {
            [ModeVariable] = Environment.GetEnvironmentVariable(ModeVariable) ?? string.Empty,
            [CheckVariable] = Environment.GetEnvironmentVariable(CheckVariable) ?? string.Empty,
            [RunsVariable] = Environment.GetEnvironmentVariable(RunsVariable) ?? string.Empty,
        });

        public static Settings Parse(IReadOnlyDictionary<string, string> values)
        {
            var mode = values.TryGetValue(ModeVariable, out var named) && named.Length > 0 ? named : "local";
            return new(
                mode,
                mode != ResultsWriter.LiveMode && values.TryGetValue(CheckVariable, out var check) && check == "1",
                values.TryGetValue(RunsVariable, out var runs) && int.TryParse(runs, out var count) && count > 1 ? count : 1);
        }
    }
}
