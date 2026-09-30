using KCC.Web.Features.Components.Header;

namespace KCC.UnitTests.Features.Components.Header;

public class HeaderNavTests
{
    private static readonly NavTarget Recipes = new("All Recipes", "/recipes/", null);

    [Test]
    [Arguments(HeaderNav.Always, false, true)]
    [Arguments(HeaderNav.Always, true, true)]
    [Arguments(HeaderNav.SignedIn, false, false)]
    [Arguments(HeaderNav.SignedIn, true, true)]
    [Arguments(HeaderNav.SignedOut, false, true)]
    [Arguments(HeaderNav.SignedOut, true, false)]
    public async Task Visible_HonoursShowWhen(string showWhen, bool isSignedIn, bool expected)
    {
        var entries = new[] { new NavEntry("Recipes", showWhen, Recipes, []) };

        _ = await Assert.That(HeaderNav.Visible(entries, isSignedIn).Any()).IsEqualTo(expected);
    }

    [Test]
    public async Task Visible_EmptyShowWhen_MeansAlways()
    {
        var entries = new[] { new NavEntry("Recipes", null, Recipes, []) };

        _ = await Assert.That(HeaderNav.Visible(entries, isSignedIn: true).Count()).IsEqualTo(1);
        _ = await Assert.That(HeaderNav.Visible(entries, isSignedIn: false).Count()).IsEqualTo(1);
    }

    [Test]
    public async Task Visible_FlatLink_CarriesUrlAndTarget()
    {
        var entries = new[] { new NavEntry("Login", HeaderNav.SignedOut, new NavTarget("Login", "/account/login/", "_self"), []) };

        var item = HeaderNav.Visible(entries, isSignedIn: false).Single();

        _ = await Assert.That(item.DisplayText).IsEqualTo("Login");
        _ = await Assert.That(item.Url).IsEqualTo("/account/login/");
        _ = await Assert.That(item.Target).IsEqualTo("_self");
        _ = await Assert.That(item.SubLinks).IsNull();
    }

    [Test]
    public async Task Visible_Group_CarriesItsLinksInOrder()
    {
        var entries = new[]
        {
            new NavEntry("Account", HeaderNav.SignedIn, null, [new("Profile", "/account/", null), new("Logout", "/account/logout", null)]),
        };

        var item = HeaderNav.Visible(entries, isSignedIn: true).Single();

        _ = await Assert.That(item.Url).IsNull();
        _ = await Assert.That(string.Join(",", item.SubLinks.Select(link => link.DisplayText))).IsEqualTo("Profile,Logout");
    }

    [Test]
    public async Task Visible_UnresolvedFlatLink_IsOmitted()
    {
        var entries = new[]
        {
            new NavEntry("Login", HeaderNav.SignedOut, null, []),
            new NavEntry("Recipes", HeaderNav.Always, Recipes, []),
        };

        var items = HeaderNav.Visible(entries, isSignedIn: false);

        _ = await Assert.That(string.Join(",", items.Select(item => item.DisplayText))).IsEqualTo("Recipes");
    }

    [Test]
    public async Task Visible_GroupWithNoResolvableLinks_IsOmitted()
    {
        var entries = new[]
        {
            new NavEntry("Recipes", HeaderNav.Always, Recipes, []),
            new NavEntry("Account", HeaderNav.SignedIn, null, []),
        };

        var items = HeaderNav.Visible(entries, isSignedIn: true);

        _ = await Assert.That(string.Join(",", items.Select(item => item.DisplayText))).IsEqualTo("Recipes");
    }
}
