using Microsoft.Playwright;

namespace KCC.E2ETests.Config.Perf;

public sealed record LiveRequest(bool Allowed, bool Bypass);

public sealed class LiveSite
{
    public const string UrlVariable = "KCC_PERF_LIVE_URL";

    public const string UsernameVariable = "KCC_PERF_MEMBER_USERNAME";

    public const string PasswordVariable = "KCC_PERF_MEMBER_PASSWORD";

    public const string BypassVariable = "KCC_PERF_BYPASS_TOKEN";

    public const string RecipeVariable = "KCC_PERF_LIVE_RECIPE";

    public const string VariantVariable = "KCC_PERF_LIVE_VARIANT";

    public const string BypassHeader = "X-KCC-Perf";

    private static readonly string[] Required = [UrlVariable, UsernameVariable, PasswordVariable, BypassVariable];

    private static readonly string[] Optional = [RecipeVariable, VariantVariable];

    private readonly string token;

    private LiveSite(Uri origin, string token, SitePaths paths)
    {
        Origin = origin;
        this.token = token;
        Paths = paths;
    }

    public Uri Origin { get; }

    public SitePaths Paths { get; }

    public static LiveSite FromEnvironment() =>
        Parse(Required.Concat(Optional).ToDictionary(name => name, name => Environment.GetEnvironmentVariable(name) ?? string.Empty));

    public static LiveSite Parse(IReadOnlyDictionary<string, string> values)
    {
        string Read(string name) => values.TryGetValue(name, out var value) ? value.Trim() : string.Empty;

        var missing = Required.Where(name => Read(name).Length == 0).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"The live measurement needs {string.Join(", ", missing)} set in your environment (see the live command in specs/006-performance-sweep/contracts/perf-commands.md).");
        }

        if (!Uri.TryCreate(Read(UrlVariable), UriKind.Absolute, out var url) || url.Scheme is not ("https" or "http"))
        {
            throw new InvalidOperationException($"{UrlVariable} must be the live site's http or https origin.");
        }

        var recipe = Read(RecipeVariable) is { Length: > 0 } named ? named : SitePaths.Seeded.Recipe;
        var paths = new SitePaths(
            recipe,
            Read(VariantVariable) is { Length: > 0 } variant ? variant : SitePaths.Seeded.Variant,
            Read(RecipeVariable).Length > 0 ? recipe : SitePaths.Seeded.AddVariantRecipe,
            ExactSearchCount: false);
        return new LiveSite(new Uri(url.GetLeftPart(UriPartial.Authority) + "/"), Read(BypassVariable), paths);
    }

    public LiveRequest Decide(string method, string url, bool signingIn)
    {
        var sameOrigin = Uri.TryCreate(url, UriKind.Absolute, out var target)
            && Uri.Compare(target, Origin, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0;
        var read = method.ToUpperInvariant() is "GET" or "HEAD";
        var signIn = signingIn && sameOrigin && method.Equals("POST", StringComparison.OrdinalIgnoreCase);
        return new(read || signIn, sameOrigin);
    }

    public Dictionary<string, string> WithBypass(IReadOnlyDictionary<string, string> headers) =>
        new(headers) { [BypassHeader] = token };

    public string Redact(string text) =>
        text.Replace(token, "<bypass token>", StringComparison.Ordinal)
            .Replace(Origin.GetLeftPart(UriPartial.Authority), string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(Origin.Host, "<live site>", StringComparison.OrdinalIgnoreCase);

    // Playwright's extra headers reach every origin a page calls, so the token rides on a route, which sees each
    // request's origin first.
    public Task GuardAsync(IBrowserContext context, bool signingIn, Action<string> blocked) =>
        context.RouteAsync("**/*", async route =>
        {
            var request = route.Request;
            var decision = Decide(request.Method, request.Url, signingIn);
            if (!decision.Allowed)
            {
                blocked($"reads only: blocked {request.Method} {Redact(request.Url)}");
                await route.AbortAsync("blockedbyclient");
            }
            else if (decision.Bypass)
            {
                await route.ContinueAsync(new() { Headers = WithBypass(request.Headers) });
            }
            else
            {
                await route.ContinueAsync();
            }
        });

    public override string ToString() => "the live site (origin and bypass token withheld)";
}

public sealed class MemberStates(bool once)
{
    private readonly Dictionary<BrowserKey, string> states = [];

    private string? shared;

    public async Task<string> GetAsync(BrowserKey key, Func<Task<string>> signIn)
    {
        if (once)
        {
            return shared ??= await signIn();
        }

        if (!states.TryGetValue(key, out var state))
        {
            states[key] = state = await signIn();
        }

        return state;
    }

    public void Forget(BrowserKey key)
    {
        if (!once)
        {
            _ = states.Remove(key);
        }
    }
}

public sealed record ServerSample(double Ms, string? CacheStatus, string? Encoding);

public sealed class ServerResponseRecord
{
    public string Path { get; init; } = string.Empty;

    public string Member { get; init; } = string.Empty;

    public double MinMs { get; init; }

    public double MedianMs { get; init; }

    public double MaxMs { get; init; }

    public List<double> RunsMs { get; init; } = [];

    public List<string> CfCacheStatus { get; init; } = [];

    public List<string> ContentEncoding { get; init; } = [];

    public List<string> Gaps { get; init; } = [];

    public static ServerResponseRecord From(string path, MemberKind member, IReadOnlyList<ServerSample> samples, IEnumerable<string>? gaps = null)
    {
        var runs = samples.Select(sample => sample.Ms).ToList();
        return new()
        {
            Path = path,
            Member = Case.Wire(member),
            MinMs = runs.Count == 0 ? 0 : runs.Min(),
            MedianMs = runs.Count == 0 ? 0 : ResultsWriter.Median(runs),
            MaxMs = runs.Count == 0 ? 0 : runs.Max(),
            RunsMs = runs,
            CfCacheStatus = [.. samples.Select(sample => sample.CacheStatus ?? "none")],
            ContentEncoding = [.. samples.Select(sample => sample.Encoding ?? "identity")],
            Gaps = [.. (gaps ?? []).Distinct()],
        };
    }
}

public static class ServerResponse
{
    public const int Runs = 10;

    public static IReadOnlyList<string> Pages(CaseFilter filter, SitePaths paths) =>
        [.. Perf.Pages.All.Where(page => filter.Pages.Count == 0 || filter.Pages.Contains(page)).DistinctBy(page => CasePages.Path(page, paths))];

    public static async Task<ServerSample> SampleAsync(IBrowser browser, LiveSite live, string pageName, string? storageState, Action<string> blocked)
    {
        var path = CasePages.Path(pageName, live.Paths);
        var options = Profiles.ContextOptions(Profiles.Desktop, Engine.Chromium, Ramp.Light, live.Origin);
        options.StorageState = storageState;
        await using var context = await browser.NewContextAsync(options);
        await live.GuardAsync(context, signingIn: false, blocked);
        var page = await context.NewPageAsync();
        var response = await page.GotoAsync(path, new() { WaitUntil = WaitUntilState.DOMContentLoaded })
            ?? throw new InvalidOperationException($"{path} gave no response");

        if (CasePages.MissingContent(pageName, response.Status))
        {
            throw new InvalidOperationException($"{path} answered 404; name a recipe and a variant the site has in {LiveSite.RecipeVariable} and {LiveSite.VariantVariable}");
        }

        if (storageState is not null && CasePages.OnSignInPage(page.Url))
        {
            throw new InvalidOperationException($"{path} sent the member to the sign-in page; the site refused the member's session");
        }

        var headers = await response.AllHeadersAsync();
        var ms = await page.EvaluateAsync<double>("() => { const n = performance.getEntriesByType('navigation')[0]; return n ? n.responseStart - n.requestStart : 0 }");
        return new(Math.Round(ms, 1), headers.GetValueOrDefault("cf-cache-status"), headers.GetValueOrDefault("content-encoding"));
    }
}
