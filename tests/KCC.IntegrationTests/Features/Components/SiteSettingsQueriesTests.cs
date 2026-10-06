using KCC.IntegrationTests.Config;
using KCC.Web.Features.Components.Header;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Assertions.Enums;
using Umbraco.Cms.Core.Web;

namespace KCC.IntegrationTests.Features.Components;

public class SiteSettingsQueriesTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task GetNavSettings_OnAFreshSite_HasNothingSet()
    {
        var settings = Read();

        _ = await Assert.That(settings.Meals).IsEmpty();
        _ = await Assert.That(settings.Diets).IsEmpty();
        _ = await Assert.That(settings.QuickPicks).IsEmpty();
        _ = await Assert.That(settings.SearchSuggestions).IsEmpty();
        _ = await Assert.That(settings.RecipesNote).IsNull();
    }

    [Test]
    public async Task GetNavSettings_ReadsWhatTheOwnerSet()
    {
        try
        {
            await TestSiteSettings.SetAsync(
                Site.Services,
                ("navMeals", TestSiteSettings.Picks(Category("Dinner"), Category("Breakfast"))),
                ("navDiets", TestSiteSettings.Picks(Tag("Vegan"), Tag("Spicy"))),
                ("navQuickPicks", TestSiteSettings.QuickPicks(
                    Site.Services,
                    TestSiteSettings.Preset(NavPresets.TopRated, "Best loved"),
                    TestSiteSettings.QuickLink("Soups", "/recipes/?query=soup"))),
                ("navSearchSuggestions", "Soup\n  \nTacos "),
                ("navRecipesNote", " Stuck? Dinner is a safe bet. "));

            var settings = Read();

            _ = await Assert.That(string.Join(",", settings.Meals)).IsEqualTo("Dinner,Breakfast");
            _ = await Assert.That(string.Join(",", settings.Diets)).IsEqualTo("Vegan,Spicy");
            _ = await Assert.That(settings.QuickPicks).IsEquivalentTo(
                [
                    new NavQuickPickSetting(NavPresets.TopRated, "Best loved", null, null),
                    new NavQuickPickSetting(null, "Soups", "/recipes/?query=soup", null),
                ],
                CollectionOrdering.Matching);
            _ = await Assert.That(string.Join(",", settings.SearchSuggestions)).IsEqualTo("Soup,Tacos");
            _ = await Assert.That(settings.RecipesNote).IsEqualTo("Stuck? Dinner is a safe bet.");
        }
        finally
        {
            await TestSiteSettings.ClearNavAsync(Site.Services);
        }
    }

    private Guid Category(string name) => TestSiteSettings.TaxonomyKey(Site.Services, "Recipe Categories", name);

    private Guid Tag(string name) => TestSiteSettings.TaxonomyKey(Site.Services, "Recipe Tags", name);

    private NavSettings Read()
    {
        using var context = Site.Services.GetRequiredService<IUmbracoContextFactory>().EnsureUmbracoContext();
        using var scope = Site.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<SiteSettingsQueries>().GetNavSettings();
    }
}
