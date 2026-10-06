using KCC.IntegrationTests.Config;
using KCC.Web.Features.Ramp;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Net.Http.Headers;

namespace KCC.IntegrationTests.Features.Members;

public class RampTests
{
    private const string PrePaintScript = "window.matchMedia('(prefers-color-scheme: dark)')";

    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task SigningIn_BringsTheSavedRampOntoEveryPage_WithoutThePrePaintScript()
    {
        var userName = TestMembers.UniqueUserName("dusk");
        var key = await TestMembers.ApprovedAsync(Site.Services, userName);
        await TestMembers.SaveRampAsync(Site.Services, key, Ramps.Dark);
        using var visitor = new MemberClient(Site);

        _ = await visitor.SignInAsync(userName, TestMembers.Password);
        var page = await RenderedPage.GetAsync(visitor.Http, "/");

        _ = await Assert.That(page.Html).Contains("<html lang=\"en\" data-theme=\"dark\">");
        _ = await Assert.That(page.Html.Contains(PrePaintScript, StringComparison.Ordinal)).IsFalse();
    }

    [Test]
    public async Task AMemberOnDevice_LeavesTheRampToThePrePaintScript()
    {
        using var member = await TestMembers.SignedInAsync(Site, "device");

        var page = await RenderedPage.GetAsync(member.Visitor.Http, "/");

        _ = await Assert.That(page.Html).Contains("<html lang=\"en\" data-theme=\"light\">");
        _ = await Assert.That(page.Html).Contains(PrePaintScript);
    }

    [Test]
    public async Task AMemberOnDevice_DoesNotInheritTheRampThePreviousMemberLeftBehind()
    {
        var previousUserName = TestMembers.UniqueUserName("dusk");
        var previousKey = await TestMembers.ApprovedAsync(Site.Services, previousUserName);
        await TestMembers.SaveRampAsync(Site.Services, previousKey, Ramps.Dark);
        var nextUserName = TestMembers.UniqueUserName("noon");
        _ = await TestMembers.ApprovedAsync(Site.Services, nextUserName);
        using var visitor = new MemberClient(Site);
        _ = await visitor.SignInAsync(previousUserName, TestMembers.Password);

        _ = await visitor.SignInAsync(nextUserName, TestMembers.Password);
        var page = await RenderedPage.GetAsync(visitor.Http, "/");

        _ = await Assert.That(page.Html).Contains("<html lang=\"en\" data-theme=\"light\">");
        _ = await Assert.That(page.Html).Contains(PrePaintScript);
    }

    [Test]
    public async Task SigningOut_DeletesTheCookie_AndTheDeviceDecidesAgain()
    {
        var userName = TestMembers.UniqueUserName("dawn");
        var key = await TestMembers.ApprovedAsync(Site.Services, userName);
        await TestMembers.SaveRampAsync(Site.Services, key, Ramps.Dark);
        using var visitor = new MemberClient(Site);
        _ = await visitor.SignInAsync(userName, TestMembers.Password);

        using var response = await visitor.SignOutAsync();
        var page = await RenderedPage.GetAsync(visitor.Http, "/");

        _ = await Assert.That(RampCookieOf(response).Expires < DateTimeOffset.UtcNow).IsTrue();
        _ = await Assert.That(page.Html).Contains(PrePaintScript);
    }

    [Test]
    public async Task AVisitor_FollowsTheirDevice_WhateverRampCookieTheyCarry()
    {
        using var client = Site.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("Cookie", "kcc-ramp=dark");

        using var response = await client.SendAsync(request);
        var html = await response.Content.ReadAsStringAsync();

        _ = await Assert.That(html).Contains("<html lang=\"en\" data-theme=\"light\">");
        _ = await Assert.That(html).Contains(PrePaintScript);
    }

    [Test]
    public async Task TheSettingsPage_PicksUpARampSavedElsewhere()
    {
        using var member = await TestMembers.SignedInAsync(Site, "elsewhere");
        await TestMembers.SaveRampAsync(Site.Services, member.Key, Ramps.Dark);

        using var settings = await member.Visitor.Http.GetAsync("/account/settings/");
        var settingsHtml = await settings.Content.ReadAsStringAsync();
        var home = await RenderedPage.GetAsync(member.Visitor.Http, "/");

        _ = await Assert.That(RampCookieOf(settings).Value.ToString()).IsEqualTo("dark");
        _ = await Assert.That(settingsHtml).Contains("<html lang=\"en\" data-theme=\"dark\">");
        _ = await Assert.That(home.Html).Contains("<html lang=\"en\" data-theme=\"dark\">");
    }

    [Test]
    public async Task ThePrePaintScript_AsksOnlyTheDevice()
    {
        using var client = Site.CreateClient();

        var html = await client.GetStringAsync("/");

        _ = await Assert.That(html).Contains(PrePaintScript);
        _ = await Assert.That(html.Contains("localStorage", StringComparison.Ordinal)).IsFalse();
    }

    private static SetCookieHeaderValue RampCookieOf(HttpResponseMessage response) =>
        SetCookieHeaderValue.ParseList(response.Headers.TryGetValues("Set-Cookie", out var cookies) ? cookies.ToList() : [])
            .Single(cookie => cookie.Name.Equals(RampCookie.Name, StringComparison.Ordinal));
}
