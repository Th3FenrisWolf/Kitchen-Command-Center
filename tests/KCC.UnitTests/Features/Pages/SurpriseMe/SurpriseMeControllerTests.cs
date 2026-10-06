using KCC.Web.Features.Pages.SurpriseMe;
using KCC.Web.Features.Search;
using Microsoft.AspNetCore.Mvc;

namespace KCC.UnitTests.Features.Pages.SurpriseMe;

public class SurpriseMeControllerTests
{
    [Test]
    public async Task Index_SendsTheVisitorToARecipe()
    {
        var redirect = (RedirectResult)Controller("/recipes/chili/").Index();

        _ = await Assert.That(redirect.Url).IsEqualTo("/recipes/chili/");
        _ = await Assert.That(redirect.Permanent).IsFalse();
    }

    [Test]
    public async Task Index_PicksAmongEveryRecipe()
    {
        string[] recipes = ["/recipes/chili/", "/recipes/stew/", "/recipes/toast/", "/recipes/soup/", "/recipes/salad/"];
        var controller = Controller(recipes);

        var landed = Enumerable.Range(0, 50).Select(_ => ((RedirectResult)controller.Index()).Url).ToList();

        _ = await Assert.That(landed.All(url => recipes.Contains(url))).IsTrue();
        _ = await Assert.That(landed.Distinct().Count()).IsGreaterThan(1);
    }

    [Test]
    public async Task Index_WithAnEmptyLibrary_GoesHome()
    {
        _ = await Assert.That(((RedirectResult)Controller().Index()).Url).IsEqualTo("/");
    }

    private static SurpriseMeController Controller(params string[] recipes)
    {
        var index = new RecipeIndex();
        index.Replace(RecipeIndexBuilder.Build(recipes.Select(url => new RecipeSearchDocument { Name = url, Slug = url })));
        var search = new RecipeSearchService(index);
        return new SurpriseMeController(new LibraryCountsCache(index, search), search);
    }
}
