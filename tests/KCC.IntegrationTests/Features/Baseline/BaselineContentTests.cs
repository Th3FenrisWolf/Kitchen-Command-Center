using KCC.IntegrationTests.Config;
using KCC.Web.Features.Models.Generated;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;

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
    [Arguments("Vegetarian", "Diet")]
    [Arguments("Vegan", "Diet")]
    [Arguments("Gluten-Free", "Diet")]
    [Arguments("Dairy-Free", "Diet")]
    [Arguments("Keto", "Diet")]
    [Arguments("Low-Carb", "Diet")]
    [Arguments("High-Protein", "Diet")]
    [Arguments("Cheesy", "Style")]
    [Arguments("Easy", "Style")]
    [Arguments("Fast", "Style")]
    [Arguments("Spicy", "Style")]
    public async Task Tag_IsPublishedWithItsKind(string name, string kind)
    {
        _ = await Assert.That(Published<RecipeTag>("Recipe Tags", name, tag => tag.Kind)).IsEqualTo(kind);
    }

    [Test]
    [Arguments("Breakfast", "fa-duotone fa-egg")]
    [Arguments("Lunch", "fa-duotone fa-sandwich")]
    [Arguments("Dinner", "fa-duotone fa-pot-food")]
    [Arguments("Dessert", "fa-duotone fa-cake-candles")]
    [Arguments("Snack", "fa-duotone fa-cookie")]
    [Arguments("Beverage", "fa-duotone fa-mug-hot")]
    public async Task Category_IsPublishedWithItsIcon(string name, string icon)
    {
        _ = await Assert.That(Published<RecipeCategory>("Recipe Categories", name, category => category.Icon)).IsEqualTo(icon);
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

    private string? Published<T>(string folderName, string name, Func<T, string?> read)
        where T : class
    {
        var folder = Content.GetRootContent().Single(node => node.Name == folderName);
        var key = ChildrenOf(folder).Single(child => child.Name == name).Key;
        using var context = Site.Services.GetRequiredService<IUmbracoContextFactory>().EnsureUmbracoContext();
        return read((T)context.UmbracoContext.Content!.GetById(key)!);
    }

    // The short IContentService.GetPagedChildren overload is obsolete in Umbraco 17, and warnings fail the build.
    private List<IContent> ChildrenOf(IContent parent) =>
        Navigation.TryGetChildrenKeys(parent.Key, out var keys) ? Content.GetByIds(keys).ToList() : [];

    private IEnumerable<Guid> TreeKeys(IContent root) =>
        Navigation.TryGetDescendantsKeys(root.Key, out var keys) ? keys.Prepend(root.Key) : [root.Key];
}
