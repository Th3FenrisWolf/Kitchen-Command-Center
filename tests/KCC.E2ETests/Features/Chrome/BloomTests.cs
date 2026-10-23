using System.Text.RegularExpressions;
using KCC.E2ETests.Config;
using Microsoft.Playwright;

namespace KCC.E2ETests.Features.Chrome;

public partial class BloomTests : BasePageTests
{
    // The opacity a wash reaches in its first second: the bloom's easing leaves the start at about 4.2 times linear.
    private const double Hair = 0.04;

    private const string Home = "/";

    private const string Library = "/recipes/";

    private const string Recipe = "/recipes/spicy-ramen-flight/";

    private const string Variant = "/recipes/spicy-ramen-flight/chili-oil-shoyu";

    private const string RecordBeforeMount = """
        () => {
          const washes = [...document.querySelectorAll('.kcc-wash')]
          window.beforeMount = {
            mounted: !!document.querySelector('#app')?.__vue_app__,
            washes,
            animations: washes.map((wash) => wash.getAnimations()[0]),
            startTimes: washes.map((wash) => wash.getAnimations()[0]?.startTime ?? null),
            styles: washes.map((wash) => {
              const style = getComputedStyle(wash)
              return [wash.dataset.wash, wash.getAttribute('style'), style.animationName, style.animationDelay, style.animationDuration].join(' | ')
            }),
          }
        }
        """;

    private const string CompareAfterMount = """
        async () => {
          await new Promise((resolve) => requestAnimationFrame(() => requestAnimationFrame(resolve)))
          const before = window.beforeMount
          const washes = [...document.querySelectorAll('.kcc-wash')]
          const faults = []
          if (washes.length !== before.washes.length) faults.push(`${before.washes.length} washes before the mount, ${washes.length} after`)
          before.washes.forEach((wash, i) => {
            if (!wash.isConnected || washes[i] !== wash) faults.push(`wash ${i} was replaced`)
            const style = getComputedStyle(wash)
            const now = [wash.dataset.wash, wash.getAttribute('style'), style.animationName, style.animationDelay, style.animationDuration].join(' | ')
            if (now !== before.styles[i]) faults.push(`wash ${i}: ${before.styles[i]} became ${now}`)
            const animation = wash.getAnimations()[0]
            if (animation !== before.animations[i]) faults.push(`wash ${i}: its animation restarted`)
            else if (animation && before.startTimes[i] !== null && animation.startTime !== before.startTimes[i]) faults.push(`wash ${i}: start ${before.startTimes[i]} became ${animation.startTime}`)
          })
          return { mountedBefore: before.mounted, faults }
        }
        """;

    [Test]
    [Arguments(Home)]
    [Arguments(Library)]
    public async Task AFreshVisit_ShowsNoWash(string page)
    {
        await RecipeIndex.CurrentAsync(Page);
        await Washes.FreezeAsync(Page);
        await Page.GotoAsync(page);
        await Washes.SettleAsync(Page);

        var washes = (await Washes.ReadAsync(Page)).SelectMany(field => field.Washes).ToArray();
        var elapsed = await Washes.ElapsedAsync(Page);
        _ = await Assert.That(elapsed).IsLessThanOrEqualTo(1);
        _ = await Assert.That(await Visit.ReadAsync(Page.Context)).Matches(VisitValue());
        _ = await Assert.That(washes).IsNotEmpty();

        // The page is read a moment after it paints, so a wash due in that moment shows a hair of its bloom.
        _ = await Assert.That(washes.Where(wash => wash.At >= elapsed && wash.Opacity != 0).Select(wash => wash.ToString())).IsEmpty();
        _ = await Assert.That(washes.Where(wash => wash.Opacity > Hair).Select(wash => wash.ToString())).IsEmpty();
    }

    [Test]
    [Arguments(Home)]
    [Arguments(Library)]
    [Arguments(Recipe)]
    [Arguments(Variant)]
    public async Task AtFiveMinutes_EveryWashHasFinishedAtItsPeak(string page)
    {
        await RecipeIndex.CurrentAsync(Page);
        await Visit.BeginAsync(Page.Context, Site.BaseUrl, 400);
        await Page.GotoAsync(page);
        await Washes.SettleAsync(Page);

        var washes = (await Washes.ReadAsync(Page)).SelectMany(field => field.Washes).ToArray();

        _ = await Assert.That(await Washes.ElapsedAsync(Page)).IsEqualTo(300);
        _ = await Assert.That(washes).IsNotEmpty();
        _ = await Assert.That(washes.Where(wash => wash.At > Washes.WindowSeconds).Select(wash => wash.ToString())).IsEmpty();
        _ = await Assert.That(washes.Where(wash => wash.PlayState != "finished" || Math.Abs(wash.Opacity - wash.Peak) > 0.001).Select(wash => wash.ToString())).IsEmpty();
    }

    [Test]
    [Arguments(Library)]
    [Arguments(Recipe)]
    public async Task MidVisit_TheEarnedWashesPaintBeforeTheAppMounts_AndTheMountChangesNone(string page)
    {
        await RecipeIndex.CurrentAsync(Page);
        var visit = await Visit.BeginAsync(Page.Context, Site.BaseUrl, 120);
        await Washes.FreezeAsync(Page);
        var release = new TaskCompletionSource();
        await Page.RouteAsync(MainScript(), async route =>
        {
            await release.Task;
            await route.ContinueAsync();
        });

        // The library's infinite scroll could otherwise add sheets between the two readings.
        var searched = new TaskCompletionSource();
        await Page.RouteAsync("**/api/recipes/search?*", async route =>
        {
            await searched.Task;
            await route.ContinueAsync();
        });

        await Page.GotoAsync(page, new() { WaitUntil = WaitUntilState.Commit });
        await Page.WaitForFunctionAsync("() => document.readyState !== 'loading' && [...document.querySelectorAll('.kcc-wash')].every((wash) => getComputedStyle(wash).animationName !== 'none')");
        var before = (await Washes.ReadAsync(Page)).SelectMany(field => field.Washes).ToArray();
        await Page.EvaluateAsync(RecordBeforeMount);
        release.SetResult();
        await Page.WaitForFunctionAsync("() => !!document.querySelector('#app')?.__vue_app__");

        var mount = await Page.EvaluateAsync<MountCheck>(CompareAfterMount);
        searched.SetResult();
        _ = await Assert.That(mount.MountedBefore).IsFalse();
        _ = await Assert.That(string.Join(Environment.NewLine, mount.Faults)).IsEmpty();

        var elapsed = await Washes.ElapsedAsync(Page);
        var (min, max) = visit.ElapsedWindow();
        _ = await Assert.That(elapsed).IsBetween(min, max);
        await ExpectEarnedAsync(before);
    }

    [Test]
    [Arguments(Library)]
    [Arguments(Recipe)]
    public async Task WithoutJavaScript_TheEarnedWashesStillShow(string page)
    {
        await RecipeIndex.CurrentAsync(Page);
        var context = await Browser.NewContextAsync(new BrowserNewContextOptions(ContextOptions(TestContext.Current!)) { JavaScriptEnabled = false });
        try
        {
            await Visit.BeginAsync(context, Site.BaseUrl, 120);
            var tab = await context.NewPageAsync();
            await tab.GotoAsync(page);

            _ = await Assert.That(await tab.EvaluateAsync<bool>("() => !!document.querySelector('#app')?.__vue_app__")).IsFalse();
            var washes = (await Washes.ReadAsync(tab)).SelectMany(field => field.Washes).ToArray();
            await ExpectEarnedAsync(washes);
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    [Test]
    public async Task OneVisit_KeepsEachPagesArrangement_AndItsSeedAcrossPages_AndANewVisitChangesIt()
    {
        await RecipeIndex.CurrentAsync(Page);
        const uint seed = 0x9f3a0c71;
        await Visit.BeginAsync(Page.Context, Site.BaseUrl, 300, seed);
        await Page.GotoAsync(Library);
        await Washes.SettleAsync(Page);
        var first = await ArrangementAsync();

        await Page.ReloadAsync();
        await Washes.SettleAsync(Page);
        _ = await Assert.That(await ArrangementAsync()).IsEquivalentTo(first);
        _ = await Assert.That(await Washes.SeedAsync(Page)).IsEqualTo((long)seed);

        await Page.GotoAsync(Recipe);
        await Washes.SettleAsync(Page);
        var recipe = await ArrangementAsync();
        _ = await Assert.That((long)Visit.SeedOf((await Visit.ReadAsync(Page.Context))!)).IsEqualTo((long)seed);
        _ = await Assert.That(await Washes.SeedAsync(Page)).IsEqualTo((long)seed);
        var shared = first.Keys.Intersect(recipe.Keys).ToArray();
        _ = await Assert.That(shared).IsNotEmpty();
        _ = await Assert.That(shared.Where(id => first[id] != recipe[id])).IsEmpty();

        await Page.Context.ClearCookiesAsync();
        await Visit.BeginAsync(Page.Context, Site.BaseUrl, 300, seed ^ 0x5a5a5a5a);
        await Page.GotoAsync(Library);
        await Washes.SettleAsync(Page);
        var next = await ArrangementAsync();
        _ = await Assert.That(next.Keys.Where(id => first.TryGetValue(id, out var style) && style == next[id])).IsEmpty();
    }

    [Test]
    [NotInParallel(MemberSession.Serial)]
    public async Task SigningInMidVisit_KeepsEverySheetsWashes()
    {
        await RecipeIndex.CurrentAsync(Page);
        var visit = await Visit.BeginAsync(Page.Context, Site.BaseUrl, 60);
        await Page.GotoAsync(Library);
        await Washes.SettleAsync(Page);
        var before = (await Washes.ReadAsync(Page)).SelectMany(field => field.Washes.Select(wash => (field.Pad, wash))).ToArray();
        var seed = Visit.SeedOf((await Visit.ReadAsync(Page.Context))!);

        await MemberSession.SignInAsync(Page);
        await Page.GotoAsync(Library);
        await Washes.SettleAsync(Page);
        var after = (await Washes.ReadAsync(Page)).SelectMany(field => field.Washes).ToDictionary(wash => wash.Id);

        _ = await Assert.That((long)Visit.SeedOf((await Visit.ReadAsync(Page.Context))!)).IsEqualTo((long)seed);
        var elapsed = await Washes.ElapsedAsync(Page);
        var (min, max) = visit.ElapsedWindow();
        _ = await Assert.That(elapsed).IsBetween(min, max);
        _ = await Assert.That(before.Where(item => !after.TryGetValue(item.wash.Id, out var now) || now.Style != item.wash.Style).Select(item => item.wash.Id)).IsEmpty();
        var pad = before.Where(item => item.Pad).Select(item => item.wash).ToArray();
        _ = await Assert.That(pad).IsNotEmpty();
        _ = await Assert.That(pad.Where(wash => !after[wash.Id].Scheduled || after[wash.Id].At != wash.At).Select(wash => wash.Id)).IsEmpty();
    }

    [Test]
    [NotInParallel(MemberSession.Serial)]
    public async Task SwitchingTheRampMidBloom_KeepsEveryWashsProgress()
    {
        await Visit.BeginAsync(Page.Context, Site.BaseUrl, 100);
        await MemberSession.SignInAsync(Page);
        try
        {
            _ = await Page.GotoAsync("/account/settings");
            var to = await Page.Locator("html").GetAttributeAsync("data-theme") == "dark" ? "light" : "dark";
            var count = await Page.EvaluateAsync<int>(
                """
                async () => {
                  const washes = [...document.querySelectorAll('.kcc-wash')].filter((wash) => wash.getAnimations().length > 0)
                  await Promise.all(washes.map((wash) => wash.getAnimations()[0].ready))
                  window.beforeSwitch = { washes, animations: washes.map((wash) => wash.getAnimations()[0]), times: washes.map((wash) => wash.getAnimations()[0]?.currentTime ?? null), at: document.timeline.currentTime }
                  return washes.length
                }
                """);
            _ = await Assert.That(count).IsGreaterThan(0);

            await ChooseRampAsync(to);
            await Expect(Page.Locator("html")).ToHaveAttributeAsync("data-theme", to);
            await Expect(Page.Locator("html")).Not.ToHaveAttributeAsync("data-theme-switching", new Regex(".*"));

            var faults = await Page.EvaluateAsync<string[]>(
                """
                () => {
                  const { washes, animations, times, at } = window.beforeSwitch
                  const passed = document.timeline.currentTime - at
                  const faults = []
                  washes.forEach((wash, i) => {
                    const animation = wash.getAnimations()[0]
                    if (animation !== animations[i]) faults.push(`wash ${i}: its animation restarted`)
                    else if (animation && animation.playState === 'running' && Math.abs(animation.currentTime - times[i] - passed) > 1) faults.push(`wash ${i}: ${times[i]}ms became ${animation.currentTime}ms over ${passed}ms`)
                  })
                  return faults
                }
                """);
            _ = await Assert.That(string.Join(Environment.NewLine, faults)).IsEmpty();
        }
        finally
        {
            _ = await Page.GotoAsync("/account/settings");
            if (await Page.GetByTestId("ramp-device").GetAttributeAsync("aria-checked") != "true")
            {
                await ChooseRampAsync("device");
            }
        }
    }

    [Test]
    [Arguments(Library)]
    [Arguments(Recipe)]
    public async Task UnderReducedMotion_ArrivingWashesFadeInAtFullSize(string page)
    {
        await RecipeIndex.CurrentAsync(Page);
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await Visit.BeginAsync(Page.Context, Site.BaseUrl, 60);
        await Page.GotoAsync(page);
        await Washes.SettleAsync(Page);

        for (var look = 0; look < 3; look++)
        {
            var washes = (await Washes.ReadAsync(Page)).SelectMany(field => field.Washes).ToArray();
            var arriving = washes.Where(wash => wash.PlayState == "running" && wash.Progress > 0).ToArray();
            _ = await Assert.That(arriving).IsNotEmpty();
            _ = await Assert.That(arriving.Where(wash => wash.Name != "kcc-bloom-fade" || wash.Scale is not ("1" or "none")).Select(wash => wash.ToString())).IsEmpty();
            _ = await Assert.That(arriving.Where(wash => wash.Opacity <= 0 || wash.Opacity >= wash.Peak).Select(wash => wash.ToString())).IsEmpty();
            await Page.WaitForTimeoutAsync(500);
        }
    }

    [Test]
    public async Task WithoutReducedMotion_ArrivingWashesGrow()
    {
        await RecipeIndex.CurrentAsync(Page);
        await Visit.BeginAsync(Page.Context, Site.BaseUrl, 60);
        await Page.GotoAsync(Library);
        await Washes.SettleAsync(Page);

        var arriving = (await Washes.ReadAsync(Page)).SelectMany(field => field.Washes).Where(wash => wash.PlayState == "running" && wash.Progress > 0).ToArray();

        _ = await Assert.That(arriving).IsNotEmpty();
        _ = await Assert.That(arriving.Where(wash => wash.Name != "kcc-bloom" || wash.Scale is "1" or "none").Select(wash => wash.ToString())).IsEmpty();
    }

    [Test]
    [Arguments(Library)]
    [Arguments(Recipe)]
    public async Task UnderReducedMotion_AtFiveMinutes_EveryWashIsAtItsPeak(string page)
    {
        await RecipeIndex.CurrentAsync(Page);
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await Visit.BeginAsync(Page.Context, Site.BaseUrl, 400);
        await Page.GotoAsync(page);
        await Washes.SettleAsync(Page);

        var washes = (await Washes.ReadAsync(Page)).SelectMany(field => field.Washes).ToArray();

        _ = await Assert.That(washes).IsNotEmpty();
        _ = await Assert.That(washes.Where(wash => wash.Name != "kcc-bloom-fade" || wash.PlayState != "finished" || Math.Abs(wash.Opacity - wash.Peak) > 0.001).Select(wash => wash.ToString())).IsEmpty();
    }

    private static async Task ExpectEarnedAsync(Washes.Wash[] washes)
    {
        _ = await Assert.That(washes.Where(wash => wash.Name != "kcc-bloom").Select(wash => wash.ToString())).IsEmpty();
        _ = await Assert.That(washes.Count(wash => wash.Progress >= Washes.BloomSeconds)).IsGreaterThan(0);
        _ = await Assert.That(washes.Count(wash => wash.Progress < 0)).IsGreaterThan(0);
        _ = await Assert.That(washes.Where(wash => wash.Progress >= Washes.BloomSeconds && Math.Abs(wash.Opacity - wash.Peak) > 0.001).Select(wash => wash.ToString())).IsEmpty();
        _ = await Assert.That(washes.Where(wash => wash.Progress is > 0.5 and < Washes.BloomSeconds && wash.Opacity <= 0).Select(wash => wash.ToString())).IsEmpty();
        _ = await Assert.That(washes.Where(wash => wash.Progress < 0 && wash.Opacity != 0).Select(wash => wash.ToString())).IsEmpty();
    }

    [GeneratedRegex(@"/assets/main-[^/]*\.js(\?|$)")]
    private static partial Regex MainScript();

    [GeneratedRegex("^1\\.[0-9a-f]{8}\\.[0-9]+$")]
    private static partial Regex VisitValue();

    private async Task<Dictionary<string, string>> ArrangementAsync() =>
        (await Washes.ReadAsync(Page)).SelectMany(field => field.Washes).ToDictionary(wash => wash.Id, wash => wash.Style);

    private Task ChooseRampAsync(string ramp) =>
        Page.RunAndWaitForResponseAsync(
            () => Page.GetByTestId($"ramp-{ramp}").ClickAsync(),
            response => response.Url.EndsWith("/api/profile/ramp", StringComparison.Ordinal) && response.Ok);

    private sealed class MountCheck
    {
        public bool MountedBefore { get; set; }

        public string[] Faults { get; set; } = [];
    }
}
