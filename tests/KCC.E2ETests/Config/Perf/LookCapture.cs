using System.Text.RegularExpressions;

namespace KCC.E2ETests.Config.Perf;

public static partial class LookCapture
{
    public const string RefVariable = "KCC_PERF_REF";

    public const string ScriptsOn = "scripts-on";

    public const string ScriptsOff = "scripts-off";

    public const int MaxDeviceRows = 4096;

    public static string Root => Path.Combine(RepoPaths.Root, "tests", "results", "perf", "captures");

    public static IReadOnlyList<Case> Cases(MemberKind member = MemberKind.E2e) =>
    [
        .. Matrix.Full(member).Where(subject =>
            subject.ImagesOn
            && subject.Motion == Motion.Full
            && (subject.Visit == VisitState.Bloomed || (subject.Visit == VisitState.Bare && subject.Engine == Engine.Chromium))),
    ];

    public static IReadOnlyList<Case> Order(IEnumerable<Case> cases) =>
        [.. MatrixRun.Order(cases).GroupBy(MatrixRun.BrowserFor).SelectMany(group => group.OrderBy(subject => subject.Ramp))];

    public static bool Freezes(Case subject) => subject.Visit == VisitState.Bare;

    // Search's third page and the add-variant form are reached by script, so with scripts off there is nothing of
    // theirs to compare; search's first paint is the search-1 case.
    public static bool ComparesScriptsOff(Case subject) =>
        subject.Visit == VisitState.Bloomed
        && subject.ImagesOn
        && subject.Motion == Motion.Full
        && subject.Page is not (Pages.Search3 or Pages.AddVariant)
        && (subject.Member != MemberKind.None || subject.Ramp == Ramp.Light);

    // Chromium's GPU raster can draw a static page one of two ways from frame to frame, scripts or none, so the
    // pairs are drawn in software, where a page that has not changed draws the same.
    public static GpuMode? ScriptsOffGpu(Engine engine) => engine == Engine.Chromium ? GpuMode.SwiftShader : null;

    public static string? StorageStateFor(Case subject, string? memberState) => subject.Member == MemberKind.None ? null : memberState;

    public static string FileName(Case subject, string? scripts = null)
    {
        var parts = subject.Identity.Split(" · ").Select(part => part.Replace(' ', '-'));
        return string.Join("__", scripts is null ? parts : parts.Append(scripts)) + ".png";
    }

    public static string Directory(string? reference) =>
        reference is { Length: > 0 } && SafeReference().IsMatch(reference) && reference.Trim('.').Length > 0
            ? Path.Combine(Root, reference)
            : throw new InvalidOperationException($"{RefVariable} must name the capture set, such as before or after; it was '{reference}'");

    [GeneratedRegex("^[A-Za-z0-9._-]+$")]
    private static partial Regex SafeReference();
}
