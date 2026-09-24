using KCC.Web.Features.Sitemap;

namespace KCC.UnitTests.Features.Sitemap;

public class SitemapFilterTests
{
    [Test]
    public async Task Urls_SkipsAccountLoginAndWizardTypes()
    {
        var urls = SitemapFilter.Urls(
        [
            new("/", "homePage", false),
            new("/recipes/", "recipeListingPage", false),
            new("/recipes/create-recipe/", "createRecipePage", false),
            new("/recipes/add-variant/", "addVariantPage", false),
            new("/account/", "accountPage", false),
            new("/account/login/", "loginPage", false),
            new("/account/settings/", "accountSettingsPage", false),
            new("/account/registration-complete/", "registrationCompletePage", false),
        ]);

        _ = await Assert.That(string.Join(",", urls)).IsEqualTo("/,/recipes/");
    }

    [Test]
    public async Task Urls_HonoursTheExcludeFlag()
    {
        var urls = SitemapFilter.Urls([new("/hidden/", "homePage", true)]);

        _ = await Assert.That(urls.Any()).IsFalse();
    }

    [Test]
    public async Task Urls_AreLowercased()
    {
        var urls = SitemapFilter.Urls([new("/Recipes/", "recipeListingPage", false)]);

        _ = await Assert.That(string.Join(",", urls)).IsEqualTo("/recipes/");
    }
}
