using KCC.IntegrationTests.Config;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;

namespace KCC.IntegrationTests.Features.Baseline;

public class BaselineContentTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    private IContentService Content => Site.Services.GetRequiredService<IContentService>();

    private IDocumentNavigationQueryService Navigation => Site.Services.GetRequiredService<IDocumentNavigationQueryService>();

    [Test]
    public async Task Home_IsTheFirstPublishedRoot()
    {
        var first = Content.GetRootContent().OrderBy(node => node.SortOrder).First();

        _ = await Assert.That(first.ContentType.Alias).IsEqualTo("homePage");
        _ = await Assert.That(first.Published).IsTrue();
    }

    [Test]
    [Arguments("Recipe Categories", 6)]
    [Arguments("Recipe Tags", 11)]
    [Arguments("Status Codes", 2)]
    public async Task RootFolder_HoldsItsPublishedChildren(string folderName, int expected)
    {
        var folder = Content.GetRootContent().Single(node => node.Name == folderName);
        var children = ChildrenOf(folder);

        _ = await Assert.That(children.Count).IsEqualTo(expected);
        _ = await Assert.That(children.All(child => child.Published)).IsTrue();
    }

    [Test]
    public async Task SiteSettings_HoldTheSignInAndAccountNavigation()
    {
        var settings = Content.GetRootContent().Single(node => node.ContentType.Alias == "siteSettings");
        var utility = settings.GetValue<string>("utilityNav") ?? string.Empty;

        _ = await Assert.That(utility.Contains("Login", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(utility.Contains("Account", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    [Arguments("Recipes", "Create Recipe")]
    [Arguments("Account", "Login")]
    [Arguments("Account", "Settings")]
    [Arguments("Account", "Registration Complete")]
    public async Task AppPage_SitsUnderItsParent(string parentName, string childName)
    {
        var home = Content.GetRootContent().Single(node => node.ContentType.Alias == "homePage");
        var parent = ChildrenOf(home).Single(node => node.Name == parentName);

        _ = await Assert.That(ChildrenOf(parent).Any(child => child.Name == childName && child.Published)).IsTrue();
    }

    [Test]
    public async Task Home_HoldsTheBaselineSections()
    {
        var sections = HomeSections();

        _ = await Assert.That(sections.Contains("Welcome to Kitchen Command Center!", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(sections.Contains("How About Something Sweeter?", StringComparison.Ordinal)).IsTrue();
        _ = await Assert.That(sections.Contains("Delicious Drinks", StringComparison.Ordinal)).IsTrue();
    }

    // A fresh database holds no media, so nothing in the baseline may point at any.
    [Test]
    public async Task Home_ReferencesNoMedia()
    {
        var sections = HomeSections();

        _ = await Assert.That(sections).IsNotEmpty();
        _ = await Assert.That(sections.Contains("mediaKey", StringComparison.Ordinal)).IsFalse();
        _ = await Assert.That(sections.Contains("umb://media", StringComparison.Ordinal)).IsFalse();
    }

    [Test]
    public async Task BaselineKeys_AreUuidsTheBackofficeAccepts()
    {
        var keys = Content.GetRootContent().SelectMany(TreeKeys).ToList();
        var rejected = keys.Where(key => !BackofficeUuid.IsAccepted(key)).ToList();

        _ = await Assert.That(keys).IsNotEmpty();
        _ = await Assert.That(rejected).IsEmpty();
    }

    private string HomeSections() =>
        Content.GetRootContent().Single(node => node.ContentType.Alias == "homePage").GetValue<string>("sections") ?? string.Empty;

    // The short IContentService.GetPagedChildren overload is obsolete in Umbraco 17, and warnings fail the build.
    private List<IContent> ChildrenOf(IContent parent) =>
        Navigation.TryGetChildrenKeys(parent.Key, out var keys) ? Content.GetByIds(keys).ToList() : [];

    private IEnumerable<Guid> TreeKeys(IContent root) =>
        Navigation.TryGetDescendantsKeys(root.Key, out var keys) ? keys.Prepend(root.Key) : [root.Key];
}
