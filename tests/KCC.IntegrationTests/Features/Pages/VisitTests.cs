using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.Bloom;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Net.Http.Headers;

namespace KCC.IntegrationTests.Features.Pages;

public partial class VisitTests
{
    private const string ServerContentOpen = "<script id=\"server-content\" type=\"application/json\">";
    private const uint LiveSeed = 0x9f3a0c71;

    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [ClassDataSource<SsrSidecar>(Shared = SharedType.PerTestSession)]
    public SsrSidecar Ssr { get; init; } = null!;

    [Test]
    public async Task AFirstPage_StartsAVisitInTheContractsFormat()
    {
        var (response, _) = await GetHomeAsync(cookie: null);

        var visit = SetCookieHeaderValue.ParseList(response.Headers.GetValues("Set-Cookie").ToList())
            .Single(cookie => cookie.Name == VisitCookie.Name);

        _ = await Assert.That(VisitValue().IsMatch(visit.Value.ToString())).IsTrue();
        _ = await Assert.That(visit.MaxAge).IsEqualTo(TimeSpan.FromSeconds(1800));
        _ = await Assert.That(visit.Path.ToString()).IsEqualTo("/");
        _ = await Assert.That(visit.SameSite).IsEqualTo(SameSiteMode.Lax);
        _ = await Assert.That(visit.HttpOnly).IsFalse();
    }

    [Test]
    public async Task AFirstPage_StartsTheWashClockAtZero()
    {
        var (_, html) = await GetHomeAsync(cookie: null);

        _ = await Assert.That(WashElapsed(html)).IsLessThanOrEqualTo(1.0);
    }

    [Test]
    public async Task EveryPage_HoldsTheBloomScheduleInItsHead()
    {
        var (_, html) = await GetHomeAsync(cookie: null);

        var schedule = html.IndexOf("<style id=\"kcc-bloom-schedule\">", StringComparison.Ordinal);

        _ = await Assert.That(schedule).IsGreaterThan(html.IndexOf("<head>", StringComparison.Ordinal));
        _ = await Assert.That(schedule).IsLessThan(html.IndexOf("</head>", StringComparison.Ordinal));
    }

    [Test]
    public async Task ALiveVisit_KeepsItsSeed()
    {
        var (_, html) = await GetHomeAsync(LiveCookie(secondsAgo: 30));

        var bloom = ServerContent(html).RootElement.GetProperty("bloom");

        _ = await Assert.That(bloom.GetProperty("seed").GetUInt32()).IsEqualTo(LiveSeed);
        _ = await Assert.That(bloom.GetProperty("page").GetString()).IsEqualTo("/");
    }

    [Test]
    public async Task AVisitTwoMinutesIn_RendersTwoMinutesElapsed()
    {
        var written = DateTimeOffset.UtcNow;
        var (_, html) = await GetHomeAsync(LiveCookie(secondsAgo: 120, written));
        var elapsed = WashElapsed(html);
        var gap = (DateTimeOffset.UtcNow - written).TotalSeconds;

        // The cookie floors the start to a whole second, which adds up to a second, and the page writes the elapsed
        // time rounded to a tenth.
        _ = await Assert.That(elapsed).IsBetween(119.95, 121.05 + gap);
    }

    [Test]
    public async Task AVisitPastTheBloom_ClampsTheElapsedTimeToFiveMinutes()
    {
        var (_, html) = await GetHomeAsync(LiveCookie(secondsAgo: 900));

        _ = await Assert.That(HtmlTag(html)).Contains("style=\"--wash-elapsed: 300.0s\"");
    }

    [Test]
    public async Task TheErrorPage_BloomsItsSheetAsAReadingSheetInRed()
    {
        var regions = await RegionsAsync("/this-page-does-not-exist");

        var render = await Ssr.RenderAsync(string.Empty, regions.Body, string.Empty, regions.Seed, regions.Page);
        var slip = Slips.In(render.Html).Single();

        _ = await Assert.That(SheetOpensWithWashes().IsMatch(slip)).IsTrue();
        _ = await Assert.That(Washes().Count(slip)).IsEqualTo(3);
        _ = await Assert.That(FirstInk(slip)).IsEqualTo("var(--color-red)");
    }

    [Test]
    public async Task RegistrationComplete_BloomsItsSheetAsAReadingSheetInGreen()
    {
        var regions = await RegionsAsync("/account/registration-complete/");

        var render = await Ssr.RenderAsync(string.Empty, regions.Body, string.Empty, regions.Seed, regions.Page);
        var slip = Slips.In(render.Html).Single();

        _ = await Assert.That(SheetOpensWithWashes().IsMatch(slip)).IsTrue();
        _ = await Assert.That(Washes().Count(slip)).IsEqualTo(3);
        _ = await Assert.That(FirstInk(slip)).IsEqualTo("var(--color-green)");
    }

    [Test]
    public async Task Home_BloomsItsRichTextAndStackerSheetsAsReadingSheets()
    {
        var regions = await RegionsAsync("/");

        var render = await Ssr.RenderAsync(string.Empty, regions.Body, string.Empty, regions.Seed, regions.Page);
        var prose = Slips.In(render.Html).Where(slip => slip.Contains("kcc-prose", StringComparison.Ordinal)).ToList();

        _ = await Assert.That(prose.Count).IsEqualTo(4);
        foreach (var slip in prose)
        {
            _ = await Assert.That(SheetOpensWithWashes().IsMatch(slip)).IsTrue();
            _ = await Assert.That(Washes().Count(slip)).IsEqualTo(3);
        }
    }

    [Test]
    public async Task AHomeCardWithNoWash_StaysBarePaper()
    {
        var regions = await RegionsAsync("/");
        var card = HomeCard().Match(regions.Body).Value;
        var bare = CardColor().Replace(card, "card-color=\"\"", 1);

        var washed = await Ssr.RenderAsync(string.Empty, card, string.Empty, regions.Seed, regions.Page);
        var unwashed = await Ssr.RenderAsync(string.Empty, bare, string.Empty, regions.Seed, regions.Page);

        _ = await Assert.That(bare).IsNotEqualTo(card);
        _ = await Assert.That(washed.Html).Contains("<span class=\"kcc-washes\"");
        _ = await Assert.That(unwashed.Html).Contains("kcc-sheet");
        _ = await Assert.That(unwashed.Html).DoesNotContain("kcc-washes");
    }

    [Test]
    [Arguments("/")]
    [Arguments("/this-page-does-not-exist")]
    public async Task TheSchedule_HoldsOneRulePerRenderedWash(string path)
    {
        var regions = await RegionsAsync(path);

        var render = await Ssr.RenderAsync(regions.Header, regions.Body, regions.Footer, regions.Seed, regions.Page);
        var washes = Washes().Matches(render.Html).Select(wash => wash.Groups[1].Value).Order(StringComparer.Ordinal).ToList();
        var rules = ScheduleRule().Matches(render.Schedule).Select(rule => rule.Groups[1].Value).Order(StringComparer.Ordinal).ToList();

        _ = await Assert.That(washes.Count).IsGreaterThan(0);
        _ = await Assert.That(washes.Distinct().Count()).IsEqualTo(washes.Count);
        _ = await Assert.That(ScheduleRule().Replace(render.Schedule, string.Empty)).IsEqualTo(string.Empty);
        _ = await Assert.That(string.Join(",", rules)).IsEqualTo(string.Join(",", washes));
    }

    private static string LiveCookie(int secondsAgo) => LiveCookie(secondsAgo, DateTimeOffset.UtcNow);

    private static string LiveCookie(int secondsAgo, DateTimeOffset written) =>
        $"{VisitCookie.Name}=1.{LiveSeed:x8}.{written.ToUnixTimeSeconds() - secondsAgo}";

    private static string HtmlTag(string html)
    {
        var start = html.IndexOf("<html", StringComparison.Ordinal);
        return html[start..(html.IndexOf('>', start) + 1)];
    }

    private static double WashElapsed(string html)
    {
        var match = WashElapsedStyle().Match(HtmlTag(html));
        return match.Success
            ? double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)
            : throw new InvalidOperationException($"No --wash-elapsed on {HtmlTag(html)}");
    }

    private static JsonDocument ServerContent(string html)
    {
        var start = html.IndexOf(ServerContentOpen, StringComparison.Ordinal) + ServerContentOpen.Length;
        return JsonDocument.Parse(html[start..html.IndexOf("</script>", start, StringComparison.Ordinal)]);
    }

    [GeneratedRegex("^1\\.[0-9a-f]{8}\\.[0-9]+$")]
    private static partial Regex VisitValue();

    [GeneratedRegex("style=\"--wash-elapsed: ([0-9]+\\.[0-9])s\"")]
    private static partial Regex WashElapsedStyle();

    private static string? FirstInk(string slip) => WashInk().Match(slip) is { Success: true } match ? match.Groups[1].Value : null;

    [GeneratedRegex("<div class=\"kcc-sheet(?: [^\"]*)?\"[^>]*><span class=\"kcc-washes\"")]
    private static partial Regex SheetOpensWithWashes();

    [GeneratedRegex("<span class=\"kcc-wash\" data-wash=\"([0-9a-f]{8})\"")]
    private static partial Regex Washes();

    [GeneratedRegex("--wash-ink:([^;\"]+)")]
    private static partial Regex WashInk();

    [GeneratedRegex("\\.kcc-wash\\[data-wash=\"([0-9a-f]{8})\"\\]\\{--wash-at:[0-9]*\\.[0-9]{3}s\\}")]
    private static partial Regex ScheduleRule();

    [GeneratedRegex("<Card\\b[\\s\\S]*?</Card>")]
    private static partial Regex HomeCard();

    [GeneratedRegex("card-color=\"bg-[a-z]+\"")]
    private static partial Regex CardColor();

    private async Task<PageRegions> RegionsAsync(string path)
    {
        using var client = Site.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("Cookie", LiveCookie(secondsAgo: 30));

        using var response = await client.SendAsync(request);
        using var content = ServerContent(await response.Content.ReadAsStringAsync());
        var root = content.RootElement;
        var bloom = root.GetProperty("bloom");
        return new PageRegions(
            root.GetProperty("headerContent").GetString() ?? string.Empty,
            root.GetProperty("bodyContent").GetString() ?? string.Empty,
            root.GetProperty("footerContent").GetString() ?? string.Empty,
            bloom.GetProperty("seed").GetUInt32(),
            bloom.GetProperty("page").GetString() ?? string.Empty);
    }

    private sealed record PageRegions(string Header, string Body, string Footer, uint Seed, string Page);

    private async Task<(HttpResponseMessage Response, string Html)> GetHomeAsync(string? cookie)
    {
        using var client = Site.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        if (cookie is not null)
        {
            request.Headers.Add("Cookie", cookie);
        }

        var response = await client.SendAsync(request);
        return (response, await response.Content.ReadAsStringAsync());
    }
}
