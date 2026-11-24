using System.Text.Json;
using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.RecipeSearch;

// Layout shift is a Chromium entry type; WebKit's geometry stand-in is the perf harness's cls_geometry.
public class AppendShiftTests : BasePageTests
{
    // The session's site can hold more than the seed: the backoffice test publishes a recipe and leaves it there.
    private const int SeededRecipes = 36;

    private const string RecordShifts = """
        window.__kccShifts = []
        new PerformanceObserver((list) => list.getEntries().forEach((entry) => {
          if (entry.hadRecentInput) return
          window.__kccShifts.push({
            value: entry.value,
            start: Math.round(entry.startTime),
            sources: (entry.sources ?? []).map((source) => {
              const node = source.node
              if (!node) return '(gone)'
              if (node.localName?.startsWith('::')) {
                const host = node.parentElement ?? node.parentNode ?? node.element
                return node.localName + ' of ' + (host ? host.localName + '.' + String(host.className).trim().split(/\s+/).slice(0, 4).join('.') : Object.prototype.toString.call(node)) + ' ' + Math.round(source.previousRect.y) + '>' + Math.round(source.currentRect.y)
              }
              if (node.nodeType !== 1) return node.nodeName
              const classes = typeof node.className === 'string' ? node.className.trim().split(/\s+/).slice(0, 3).join('.') : ''
              return node.localName + (node.id ? '#' + node.id : '') + (classes ? '.' + classes : '')
                + ' ' + Math.round(source.previousRect.y) + '>' + Math.round(source.currentRect.y)
            }),
          })
        })).observe({ type: 'layout-shift', buffered: true })
        """;

    private const string RenderedTwice = "() => new Promise((resolve) => requestAnimationFrame(() => requestAnimationFrame(resolve)))";

    private ILocator Cards => Page.Locator("[data-recipe-name]");

    // A reader parked on the footer sees each page land above it and push it down, as before the space was
    // reserved: the next page is already on its way when the reader gets back to the bottom, and holding it back
    // stalled infinite scroll for a reader who stopped there. The ceilings are the sums the same flow recorded
    // before the space was reserved (the footer's three entries per viewport).
    private static double ParkedOnFooterShiftBefore(int width) => width >= 1024 ? 0.0928 : 0.2704;

    [Test]
    [Arguments(1920, 1080, false)]
    [Arguments(1920, 1080, true)]
    [Arguments(375, 812, false)]
    [Arguments(375, 812, true)]
    public async Task Three_pages_parked_on_the_footer_shift_no_more_than_before(int width, int height, bool bloomed)
    {
        await PrepareAsync(width, height, bloomed);
        await Page.GotoAsync("/recipes");
        await Hydration.WaitAsync(Page);

        await Page.EvaluateAsync("() => document.fonts.ready");
        var from = await Page.EvaluateAsync<double>("() => performance.now()");
        var loaded = await Cards.CountAsync();
        for (var attempt = 0; attempt < 40 && loaded < SeededRecipes; attempt++)
        {
            await Page.EvaluateAsync("() => window.scrollTo(0, document.documentElement.scrollHeight - innerHeight * 4)");
            await Page.EvaluateAsync(RenderedTwice);
            await Page.EvaluateAsync("() => window.scrollTo(0, document.documentElement.scrollHeight)");
            await Page.EvaluateAsync(RenderedTwice);
            loaded = await LandingAsync(loaded);
        }

        await Page.EvaluateAsync(RenderedTwice);
        var shifts = await Page.EvaluateAsync<string>("(from) => JSON.stringify(window.__kccShifts.filter((shift) => shift.value > 0 && shift.start >= from))", from);
        var shift = await Page.EvaluateAsync<double>("(from) => window.__kccShifts.filter((shift) => shift.start >= from).reduce((sum, entry) => sum + entry.value, 0)", from);
        Console.WriteLine($"parked on the footer {width}x{height}: {shift}; entries: {shifts}");
        _ = await Assert.That(loaded).IsGreaterThanOrEqualTo(SeededRecipes);
        _ = await Assert.That(shift).IsLessThanOrEqualTo(ParkedOnFooterShiftBefore(width));
    }

    // No scroll position hides the end of a first page shorter than the screen, so the first append moves the
    // footer as it always has; only a band the server draws could hold that space, and the owner ruled that out.
    // The shift is pinned at what the site recorded before the space was reserved, measured by this case. It is the
    // strip of footer on screen times the distance the landing pushes it, and the same landing is two rules taller on
    // Linux (724 px against 676 on macOS), so the pin carries a margin.
    private const double ShortPageShiftBefore = 0.01676420691486232;

    private const double PlatformMargin = 1.25;

    [Test]
    public async Task A_first_page_shorter_than_the_screen_shifts_no_more_than_before()
    {
        await PrepareAsync(1920, 2200, bloomed: false);
        await Page.AddInitScriptAsync(AtUsable);
        await Page.GotoAsync("/recipes");
        await Hydration.WaitAsync(Page);
        var usable = await Page.EvaluateAsync<JsonElement>("() => window.__kccAtUsable");
        var first = usable.GetProperty("cards").GetInt32();
        _ = await Assert.That(await LandingAsync(first, attempts: 40)).IsGreaterThan(first);
        await Page.EvaluateAsync(RenderedTwice);

        var shift = await Page.EvaluateAsync<double>("() => window.__kccShifts.reduce((sum, entry) => sum + entry.value, 0)");
        Console.WriteLine($"short page shift through the first append: {shift}; entries: " + await Page.EvaluateAsync<string>("() => JSON.stringify(window.__kccShifts)"));
        Console.WriteLine($"short page at usable: {usable}");
        _ = await Assert.That(usable.GetProperty("footer").GetDouble()).IsLessThan(2200);
        _ = await Assert.That(first).IsLessThan(SeededRecipes);
        _ = await Assert.That(shift).IsLessThanOrEqualTo(ShortPageShiftBefore * PlatformMargin);
        _ = await Assert.That(shift).IsLessThan(0.1);
    }

    // The seeded set is three pages, so the reader parked after the first scroll sees both later pages arrive
    // without scrolling again; the second can land within one poll of the first.
    [Test]
    public async Task A_reader_resting_on_the_footer_gets_the_next_page_at_once_without_scrolling()
    {
        await PrepareAsync(1920, 1080, bloomed: false);
        await Page.AddInitScriptAsync(RecordFrames);
        await Page.GotoAsync("/recipes");
        await Hydration.WaitAsync(Page);

        var first = await Cards.CountAsync();
        _ = await Assert.That(first).IsLessThan(SeededRecipes);
        var footer = await Page.EvaluateAsync<double>(
            "() => { window.scrollTo(0, document.documentElement.scrollHeight); return document.querySelector('footer').getBoundingClientRect().top }");
        _ = await Assert.That(footer).IsLessThan(1080);

        var loaded = first;
        for (var i = 0; i < 100 && loaded < SeededRecipes; i++)
        {
            await Page.WaitForTimeoutAsync(50);
            loaded = await Cards.CountAsync();
        }

        _ = await Assert.That(loaded).IsGreaterThanOrEqualTo(SeededRecipes);
        var frames = await Page.EvaluateAsync<double>(FramesBeforeNextRequest);
        Console.WriteLine($"resting reader: next request {frames} frames after the last response");
        _ = await Assert.That(frames).IsLessThanOrEqualTo(ImmediateLoadFrames);
    }

    private const string RecordFrames = """
        window.__kccFrames = []
        const frame = () => {
          window.__kccFrames.push(performance.now())
          requestAnimationFrame(frame)
        }
        requestAnimationFrame(frame)
        """;

    private const string FramesBeforeNextRequest = """
        () => {
          const [second, third] = performance.getEntriesByType('resource').filter((entry) => entry.name.includes('/api/recipes/search'))
          return third ? window.__kccFrames.filter((at) => at > second.responseEnd && at <= third.startTime).length : Infinity
        }
        """;

    // The observer reports the end of the results in the first frame after a landing. The wait is counted in frames
    // because a frame of this page takes about a second on a runner that renders in software.
    private const double ImmediateLoadFrames = 3;

    private const string AtUsable = """
        new PerformanceObserver((list) => {
          if (window.__kccAtUsable || !list.getEntries().some((mark) => mark.name === 'kcc:hydrated')) return
          window.__kccAtUsable = {
            cards: document.querySelectorAll('[data-recipe-name]').length,
            footer: Math.round(document.querySelector('footer').getBoundingClientRect().top),
          }
        }).observe({ type: 'mark' })
        """;

    private async Task PrepareAsync(int width, int height, bool bloomed)
    {
        await Page.SetViewportSizeAsync(width, height);
        if (bloomed)
        {
            _ = await Visit.BeginAsync(Context, Site.BaseUrl, secondsAgo: 3600);
        }

        await Page.AddInitScriptAsync(RecordShifts);
    }

    private async Task<int> LandingAsync(int loaded, int attempts = 15)
    {
        var now = loaded;
        for (var i = 0; i < attempts && now == loaded; i++)
        {
            await Page.WaitForTimeoutAsync(50);
            now = await Cards.CountAsync();
        }

        return now;
    }
}
