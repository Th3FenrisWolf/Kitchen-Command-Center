using KCC.Web.Features.Bloom;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Net.Http.Headers;

namespace KCC.UnitTests.Features.Bloom;

public class VisitClockTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeMilliseconds(1_792_000_300_750);

    private static readonly DateTimeOffset NowInWholeSeconds = DateTimeOffset.FromUnixTimeSeconds(1_792_000_300);

    [Test]
    public async Task Resolve_WithNoCookie_StartsAVisitNowAndWritesIt()
    {
        var context = new DefaultHttpContext();

        var visit = VisitClock.Resolve(context, new FixedClock(Now));

        _ = await Assert.That(visit.StartedAt).IsEqualTo(NowInWholeSeconds);
        _ = await Assert.That(visit.Elapsed).IsEqualTo(TimeSpan.Zero);
        var cookie = SetCookie(context);
        _ = await Assert.That(cookie.Name.ToString()).IsEqualTo("kcc-visit");
        _ = await Assert.That(cookie.Value.ToString()).IsEqualTo(VisitCookie.Format(visit));
        _ = await Assert.That(cookie.MaxAge).IsEqualTo(TimeSpan.FromSeconds(1800));
        _ = await Assert.That(cookie.Path.ToString()).IsEqualTo("/");
        _ = await Assert.That(cookie.SameSite).IsEqualTo(Microsoft.Net.Http.Headers.SameSiteMode.Lax);
        _ = await Assert.That(cookie.Secure).IsFalse();
        _ = await Assert.That(cookie.HttpOnly).IsFalse();
    }

    [Test]
    public async Task Resolve_OverHttps_MarksTheCookieSecure()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";

        _ = VisitClock.Resolve(context, new FixedClock(Now));

        _ = await Assert.That(SetCookie(context).Secure).IsTrue();
    }

    [Test]
    public async Task Resolve_WithALiveCookie_KeepsItsSeedAndStartAndRenewsIt()
    {
        var context = Requesting("kcc-visit=1.9f3a0c71.1792000176");

        var visit = VisitClock.Resolve(context, new FixedClock(Now));

        _ = await Assert.That(visit.Seed).IsEqualTo(0x9f3a0c71u);
        _ = await Assert.That(visit.StartedAt).IsEqualTo(DateTimeOffset.FromUnixTimeSeconds(1_792_000_176));
        _ = await Assert.That(visit.Elapsed).IsEqualTo(TimeSpan.FromMilliseconds(124_750));
        var cookie = SetCookie(context);
        _ = await Assert.That(cookie.Value.ToString()).IsEqualTo("1.9f3a0c71.1792000176");
        _ = await Assert.That(cookie.MaxAge).IsEqualTo(TimeSpan.FromSeconds(1800));
    }

    [Test]
    [Arguments("kcc-visit=garbage")]
    [Arguments("kcc-visit=2.9f3a0c71.1792000176")]
    [Arguments("kcc-visit=1.9f3a0c71.1792000400")]
    public async Task Resolve_WithAMalformedCookie_StartsAVisit(string cookies)
    {
        var context = Requesting(cookies);

        var visit = VisitClock.Resolve(context, new FixedClock(Now));

        _ = await Assert.That(visit.StartedAt).IsEqualTo(NowInWholeSeconds);
        _ = await Assert.That(visit.Seed).IsNotEqualTo(0x9f3a0c71u);
        _ = await Assert.That(SetCookie(context).Value.ToString()).IsEqualTo(VisitCookie.Format(visit));
    }

    [Test]
    public async Task Resolve_WithNoCookieTwice_MintsDifferentSeeds()
    {
        var seeds = Enumerable.Range(0, 8)
            .Select(_ => VisitClock.Resolve(new DefaultHttpContext(), new FixedClock(Now)).Seed)
            .Distinct();

        _ = await Assert.That(seeds.Count()).IsGreaterThan(1);
    }

    [Test]
    public async Task Elapsed_IsClampedToFiveMinutes()
    {
        var context = Requesting($"kcc-visit=1.9f3a0c71.{NowInWholeSeconds.AddMinutes(-20).ToUnixTimeSeconds()}");

        var visit = VisitClock.Resolve(context, new FixedClock(Now));

        _ = await Assert.That(visit.Elapsed).IsEqualTo(TimeSpan.FromSeconds(300));
    }

    [Test]
    public async Task Elapsed_IsNeverNegative()
    {
        var visit = new Visit(1, Now.AddSeconds(5), Now);

        _ = await Assert.That(visit.Elapsed).IsEqualTo(TimeSpan.Zero);
    }

    [Test]
    public async Task Resolve_AfterTheResponseStarted_WritesNothingAndReturnsTheVisit()
    {
        var context = Requesting("kcc-visit=1.9f3a0c71.1792000176");
        context.Features.Set<IHttpResponseFeature>(new StartedResponse());

        var visit = VisitClock.Resolve(context, new FixedClock(Now));

        _ = await Assert.That(visit.Seed).IsEqualTo(0x9f3a0c71u);
        _ = await Assert.That(context.Response.Headers.SetCookie.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Resolve_AfterTheResponseStarted_WithNoCookie_StillReturnsAVisit()
    {
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(new StartedResponse());

        var visit = VisitClock.Resolve(context, new FixedClock(Now));

        _ = await Assert.That(visit.StartedAt).IsEqualTo(NowInWholeSeconds);
        _ = await Assert.That(context.Response.Headers.SetCookie.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Resolve_TwiceOnOneRequest_ReturnsTheSameVisitAndWritesOneCookie()
    {
        var context = new DefaultHttpContext();

        var first = VisitClock.Resolve(context, new FixedClock(Now));
        var second = VisitClock.Resolve(context, new FixedClock(Now.AddSeconds(30)));

        _ = await Assert.That(second).IsSameReferenceAs(first);
        _ = await Assert.That(context.Response.Headers.SetCookie.Count).IsEqualTo(1);
    }

    private static DefaultHttpContext Requesting(string cookies)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = cookies;
        return context;
    }

    private static SetCookieHeaderValue SetCookie(HttpContext context) =>
        SetCookieHeaderValue.Parse(context.Response.Headers.SetCookie.ToString());

    private sealed class FixedClock(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class StartedResponse : HttpResponseFeature
    {
        public override bool HasStarted => true;
    }
}
