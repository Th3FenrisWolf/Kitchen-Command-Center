using System.Globalization;
using Microsoft.Playwright;

namespace KCC.E2ETests.Config.Perf;

public enum Engine
{
    Chromium,
    Webkit,
}

public enum Ramp
{
    Light,
    Dark,
}

public enum VisitState
{
    Bare,
    Mid,
    Bloomed,
}

public enum Motion
{
    Full,
    Reduced,
}

public enum MemberKind
{
    None,
    E2e,
    Live,
}

public static class Pages
{
    public const string Home = "home";
    public const string Search1 = "search-1";
    public const string Search3 = "search-3";
    public const string Recipe = "recipe";
    public const string VariantCook = "variant-cook";
    public const string CreateRecipe = "create-recipe";
    public const string AddVariant = "add-variant";
    public const string Account = "account";
    public const string SignIn = "sign-in";
    public const string Settings = "settings";
    public const string Error = "error";
    public const string Status404 = "status-404";

    public static readonly IReadOnlyList<string> All =
    [
        Home, Search1, Search3, Recipe, VariantCook, CreateRecipe, AddVariant, Account, SignIn, Settings, Error, Status404,
    ];

    private static readonly HashSet<string> MemberPages = [CreateRecipe, AddVariant, Account, Settings];

    public static bool NeedsMember(string page) => MemberPages.Contains(page);
}

public sealed record Case(
    string Page,
    string Profile,
    Engine Engine,
    Ramp Ramp,
    VisitState Visit,
    bool ImagesOn = true,
    Motion Motion = Motion.Full,
    MemberKind Member = MemberKind.None)
{
    public const uint Seed = 0x0badf00d;

    public static string Wire(Engine engine) => engine.ToString().ToLowerInvariant();

    public static string Wire(Ramp ramp) => ramp.ToString().ToLowerInvariant();

    public static string Wire(VisitState visit) => visit.ToString().ToLowerInvariant();

    public static string Wire(MemberKind member) => member.ToString().ToLowerInvariant();

    public static MemberKind DefaultMember(string page, MemberKind live = MemberKind.E2e) =>
        Pages.NeedsMember(page) ? live : MemberKind.None;

    public static int SecondsAgo(VisitState visit) => visit switch
    {
        VisitState.Bare => 0,
        VisitState.Mid => 120,
        _ => 400,
    };

    public string Identity
    {
        get
        {
            var parts = new List<string> { Page, Profile, Wire(Engine), Wire(Ramp), Wire(Visit) };
            if (!ImagesOn)
            {
                parts.Add("images off");
            }

            if (Motion == Motion.Reduced)
            {
                parts.Add("motion reduced");
            }

            if (Pages.NeedsMember(Page) != (Member != MemberKind.None))
            {
                parts.Add($"member {Wire(Member)}");
            }

            return string.Join(" · ", parts);
        }
    }

    public Case WithDefaultMember(MemberKind live = MemberKind.E2e) => this with { Member = DefaultMember(Page, live) };

    public Task<Config.Visit.Begun> BeginVisitAsync(IBrowserContext context, Uri baseUrl) =>
        Config.Visit.BeginAsync(context, baseUrl, SecondsAgo(Visit), Seed);
}

public static class Matrix
{
    private static readonly string[] MidPages = [Pages.Home, Pages.Search1, Pages.Search3, Pages.Recipe];

    public static IReadOnlyList<Case> Full(MemberKind member = MemberKind.E2e)
    {
        var cases = new List<Case>();
        foreach (var page in Pages.All)
        {
            foreach (var profile in new[] { Profiles.Phone.Name, Profiles.Desktop.Name })
            {
                foreach (var engine in Enum.GetValues<Engine>())
                {
                    foreach (var ramp in Enum.GetValues<Ramp>())
                    {
                        cases.Add(new Case(page, profile, engine, ramp, VisitState.Bare).WithDefaultMember(member));
                        cases.Add(new Case(page, profile, engine, ramp, VisitState.Bloomed).WithDefaultMember(member));
                        if (engine == Engine.Chromium && MidPages.Contains(page))
                        {
                            cases.Add(new Case(page, profile, engine, ramp, VisitState.Mid).WithDefaultMember(member));
                        }
                    }
                }
            }
        }

        foreach (var page in Pages.All)
        {
            var variant = new Case(page, Profiles.Phone.Name, Engine.Chromium, Ramp.Light, VisitState.Bloomed).WithDefaultMember(member);
            cases.Add(variant with { ImagesOn = false });
            cases.Add(variant with { Motion = Motion.Reduced });
        }

        return cases;
    }

    public static IReadOnlyList<Case> KeySubset(MemberKind member = MemberKind.E2e) =>
    [
        .. new[] { Pages.Home, Pages.Search3, Pages.Recipe, Pages.VariantCook, Pages.Account }
            .SelectMany(page => Enum.GetValues<Engine>()
                .SelectMany(engine => Enum.GetValues<Ramp>()
                    .Select(ramp => new Case(page, Profiles.Phone.Name, engine, ramp, VisitState.Bloomed).WithDefaultMember(member)))),
    ];
}

public sealed record CaseFilter(
    IReadOnlySet<string> Pages,
    IReadOnlySet<string> Profiles,
    IReadOnlySet<string> Engines,
    IReadOnlySet<string> Ramps,
    IReadOnlySet<string> Visits)
{
    public static CaseFilter FromEnvironment() => new(
        Read("PAGE"), Read("PROFILE"), Read("ENGINE"), Read("RAMP"), Read("VISIT"));

    public bool Matches(Case value) =>
        Allows(Pages, value.Page)
        && Allows(Profiles, value.Profile)
        && Allows(Engines, Case.Wire(value.Engine))
        && Allows(Ramps, Case.Wire(value.Ramp))
        && Allows(Visits, Case.Wire(value.Visit));

    public IEnumerable<Case> Apply(IEnumerable<Case> cases) => cases.Where(Matches);

    public static CaseFilter Parse(IReadOnlyDictionary<string, string> values)
    {
        HashSet<string> Get(string key) => values.TryGetValue(key, out var raw) ? Split(raw) : [];
        return new(Get("PAGE"), Get("PROFILE"), Get("ENGINE"), Get("RAMP"), Get("VISIT"));
    }

    private static HashSet<string> Read(string name) =>
        Split(Environment.GetEnvironmentVariable($"KCC_PERF_FILTER_{name}") ?? string.Empty);

    private static HashSet<string> Split(string raw) =>
        raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(item => item.ToLower(CultureInfo.InvariantCulture))
            .ToHashSet();

    private static bool Allows(IReadOnlySet<string> allowed, string value) => allowed.Count == 0 || allowed.Contains(value);
}

public sealed record SitePaths(string Recipe, string Variant, string AddVariantRecipe, bool ExactSearchCount)
{
    public static readonly SitePaths Seeded = new(
        "/recipes/legendary-lasagna/", "/recipes/fluffy-buttermilk-pancakes/classic-stack", "/recipes/weeknight-tacos/", ExactSearchCount: true);
}

public static class CasePages
{
    public const int SeededRecipes = 36;

    public static bool Navigates(string page) => page == Pages.AddVariant;

    public static string Path(string page, SitePaths? paths = null) => page switch
    {
        Pages.Home => "/",
        Pages.Search1 or Pages.Search3 => "/recipes/",
        Pages.Recipe => (paths ?? SitePaths.Seeded).Recipe,
        Pages.VariantCook => (paths ?? SitePaths.Seeded).Variant,
        Pages.CreateRecipe => "/recipes/create-recipe",
        Pages.AddVariant => (paths ?? SitePaths.Seeded).AddVariantRecipe,
        Pages.Account => "/account",
        Pages.SignIn => "/account/login",
        Pages.Settings => "/account/settings",
        Pages.Error => "/error",
        Pages.Status404 => "/this-page-does-not-exist",
        _ => throw new ArgumentOutOfRangeException(nameof(page), page, "Unknown page."),
    };

    public static bool SessionRefused(Case subject, string url) =>
        subject.Member != MemberKind.None && OnSignInPage(url);

    public static bool OnSignInPage(string url) =>
        new Uri(url).AbsolutePath.StartsWith(Path(Pages.SignIn), StringComparison.OrdinalIgnoreCase);

    public static bool MissingContent(string page, int status) =>
        status == 404 && page is Pages.Recipe or Pages.VariantCook or Pages.AddVariant;

    public static async Task OpenAsync(IPage page, string name, SitePaths? paths = null)
    {
        switch (name)
        {
            case Pages.Search1:
                await page.RouteAsync("**/api/recipes/search**", route => route.AbortAsync());
                break;
        }

        var path = Path(name, paths);
        var response = await page.GotoAsync(path, new() { WaitUntil = WaitUntilState.Load });
        if (response is not null && MissingContent(name, response.Status))
        {
            throw new InvalidOperationException(
                $"{path} answered 404; a live run names a recipe and a variant the site has in KCC_PERF_LIVE_RECIPE and KCC_PERF_LIVE_VARIANT");
        }
    }

    public const string NoCookModeOpener = "cook mode has no opener at this width; the variant page is measured with it closed";

    public static async Task<string?> ReachStateAsync(IPage page, string name, SitePaths? paths = null)
    {
        switch (name)
        {
            case Pages.Search3:
                return await LoadThreePagesAsync(page, (paths ?? SitePaths.Seeded).ExactSearchCount);
            case Pages.VariantCook:
                var opener = page.Locator("[data-test^='cook-mode-open']:visible");
                if (await opener.CountAsync() == 0)
                {
                    return NoCookModeOpener;
                }

                await opener.First.ClickAsync();
                await page.Locator("[role='dialog']").WaitForAsync();
                break;
            case Pages.AddVariant:
                await page.GetByRole(AriaRole.Link, new() { Name = "Add Variant", Exact = true }).First.ClickAsync();
                await page.WaitForURLAsync(url => new Uri(url).Query.StartsWith("?recipe=", StringComparison.Ordinal));
                break;
        }

        return null;
    }

    private const string RenderedTwice = "() => new Promise((resolve) => requestAnimationFrame(() => requestAnimationFrame(resolve)))";

    private static async Task<string?> LoadThreePagesAsync(IPage page, bool exact)
    {
        const int StalledAttempts = 5;
        var cards = page.Locator("[data-recipe-name]");
        var loaded = await cards.CountAsync();
        var stalled = 0;
        for (var attempt = 0; attempt < 40 && loaded < SeededRecipes && (exact || stalled < StalledAttempts); attempt++)
        {
            // The search loads on the sentinel's intersection change, and a sentinel still in view when a page
            // finishes loading never changes, so each attempt scrolls away from the bottom before returning to it.
            // Intersections are computed only in a rendering update, and headless WebKit on Linux renders the phone
            // about once a second, so a fixed pause can let both scrolls land in one update and cancel out.
            await page.EvaluateAsync("() => window.scrollTo(0, document.documentElement.scrollHeight - innerHeight * 4)");
            await page.EvaluateAsync(RenderedTwice);
            await page.EvaluateAsync("() => window.scrollTo(0, document.documentElement.scrollHeight)");
            await page.EvaluateAsync(RenderedTwice);
            await page.WaitForTimeoutAsync(750);
            var now = await cards.CountAsync();
            stalled = now > loaded ? 0 : stalled + 1;
            loaded = now;
        }

        if (exact && loaded != SeededRecipes)
        {
            throw new InvalidOperationException($"search loaded {loaded} of {SeededRecipes} recipe cards");
        }

        return loaded < SeededRecipes ? $"search loaded {loaded} recipe cards, fewer than three pages" : null;
    }
}
