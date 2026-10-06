using System.Text.RegularExpressions;
using KCC.IntegrationTests.Config;

namespace KCC.IntegrationTests.Features.Components;

public class HeaderTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task AVisitorsHeader_HandsThePadItsLogoTextAndNavModel_AndNothingElse()
    {
        using var client = Site.CreateClient();

        var page = await RenderedPage.GetAsync(client, "/recipes/");

        _ = await Assert.That(AttributesOf(page.Header)).IsEquivalentTo(["logo-alt", ":nav"]);
        _ = await Assert.That(page.Header).Contains("logo-alt=\"Kitchen Command Center\"");
        _ = await Assert.That(page.HeaderProp("nav").GetProperty("urls").TryGetProperty("signIn", out _)).IsTrue();
    }

    [Test]
    public async Task AMembersHeader_HandsThePadTheSameTwo_WithTheirKitchen()
    {
        using var member = await TestMembers.SignedInAsync(Site, "header");

        var page = await RenderedPage.GetAsync(member.Visitor.Http, "/recipes/");

        _ = await Assert.That(AttributesOf(page.Header)).IsEquivalentTo(["logo-alt", ":nav"]);
        _ = await Assert.That(page.HeaderProp("nav").TryGetProperty("member", out _)).IsTrue();
    }

    // A prop holding JSON has its quotes encoded, so only attribute names match here.
    private static string[] AttributesOf(string header) =>
        Regex.Matches(header, "\\s([:@a-z-]+)=\"").Select(match => match.Groups[1].Value).ToArray();
}
