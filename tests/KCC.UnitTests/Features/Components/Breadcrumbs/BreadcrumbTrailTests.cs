using KCC.Web.Features.Components.Breadcrumbs;

namespace KCC.UnitTests.Features.Components.Breadcrumbs;

public class BreadcrumbTrailTests
{
    [Test]
    public async Task Build_ShowsHomeUnderItsOwnLabelAndLeavesThePageUnlinked()
    {
        var trail = BreadcrumbTrail.Build(
            "Home",
            [new("Kitchen Command Center", "/"), new("Recipes", "/recipes/"), new("Pancakes", "/recipes/pancakes/")]);

        _ = await Assert.That(Describe(trail)).IsEqualTo("Home>/|Recipes>/recipes/|Pancakes>");
    }

    [Test]
    public async Task Build_OnTheHomePage_IsTheHomeCrumbAlone()
    {
        var trail = BreadcrumbTrail.Build("Home", [new("Kitchen Command Center", "/")]);

        _ = await Assert.That(Describe(trail)).IsEqualTo("Home>/");
    }

    [Test]
    public async Task Build_WithNoPages_IsEmpty()
    {
        _ = await Assert.That(BreadcrumbTrail.Build("Home", []).Count).IsEqualTo(0);
    }

    [Test]
    [Arguments("Short", "Title", "Name", "Short")]
    [Arguments("", "Title", "Name", "Title")]
    [Arguments(" ", null, "Name", "Name")]
    public async Task Label_PrefersTheBreadcrumbLabelThenTheTitleThenTheName(string label, string title, string name, string expected)
    {
        _ = await Assert.That(BreadcrumbTrail.Label(label, title, name)).IsEqualTo(expected);
    }

    private static string Describe(IEnumerable<BreadcrumbLink> trail) =>
        string.Join("|", trail.Select(link => $"{link.LinkText}>{link.Url}"));
}
