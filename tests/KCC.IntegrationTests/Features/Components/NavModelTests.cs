using System.Net;
using System.Text.Json;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.Components.Header;
using KCC.Web.Features.Pages.Account;
using KCC.Web.Features.Search;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Services;

namespace KCC.IntegrationTests.Features.Components;

public class NavModelTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task AVisitor_GetsTheLibraryWithItsCounts()
    {
        var total = await LibraryTotalAsync();
        using var client = Site.CreateClient();

        var nav = (await RenderedPage.GetAsync(client, "/recipes/?diet=Vegan")).HeaderProp("nav");
        var recipes = nav.GetProperty("recipes");

        _ = await Assert.That(nav.TryGetProperty("member", out _)).IsFalse();
        _ = await Assert.That(nav.GetProperty("recipeTotal").GetInt32()).IsEqualTo(total);
        _ = await Assert.That(Rows(recipes.GetProperty("meals"))).IsEqualTo("Breakfast 4, Lunch 4, Dinner 5, Dessert 4, Snack 4, Beverage 4");
        _ = await Assert.That(recipes.GetProperty("meals")[0].GetProperty("icon").GetString()).IsEqualTo("fa-duotone fa-egg");
        _ = await Assert.That(Rows(recipes.GetProperty("diets")))
            .IsEqualTo("Vegetarian 10, Vegan 12, Gluten-Free 7, Dairy-Free 2, Keto 1, High-Protein 8, Low-Carb 3");
        _ = await Assert.That(Links(recipes.GetProperty("quickPicks"))).IsEqualTo(
            "Under 30 minutes /recipes/?timeMax=30, Top rated /recipes/?sort=rated, Most variants /recipes/?sort=variants, Newest /recipes/?sort=recent, Surprise me /surprise-me");
        _ = await Assert.That(recipes.TryGetProperty("note", out _)).IsFalse();
        _ = await Assert.That(Strings(nav.GetProperty("suggestions"))).IsEqualTo("Dinner,Breakfast,Lunch");
        _ = await Assert.That(nav.GetProperty("labels").GetProperty("Nav.MyKitchen").GetString()).IsEqualTo("My kitchen");
    }

    [Test]
    public async Task AVisitor_CanSignInBackToThePage_OrAskForAnAccount()
    {
        using var client = Site.CreateClient();
        var urls = (await RenderedPage.GetAsync(client, "/recipes/?diet=Vegan")).HeaderProp("nav").GetProperty("urls");

        var signIn = await RenderedPage.GetAsync(client, urls.GetProperty("signIn").GetString()!);
        var register = await RenderedPage.GetAsync(client, urls.GetProperty("register").GetString()!);

        _ = await Assert.That(urls.GetProperty("home").GetString()).IsEqualTo("/");
        _ = await Assert.That(urls.GetProperty("library").GetString()).IsEqualTo("/recipes/");
        _ = await Assert.That(urls.GetProperty("surpriseMe").GetString()).IsEqualTo("/surprise-me");
        _ = await Assert.That(urls.GetProperty("currentPage").GetString()).IsEqualTo("/recipes/?diet=Vegan");
        _ = await Assert.That(signIn.Attribute("return-url")).IsEqualTo("/recipes/?diet=Vegan");
        _ = await Assert.That(register.Attribute(":register")).IsEqualTo("true");
        foreach (var memberOnly in new[] { "newRecipe", "account", "settings", "signOut" })
        {
            _ = await Assert.That(urls.TryGetProperty(memberOnly, out _)).IsFalse();
        }
    }

    [Test]
    [Arguments("/", null)]
    [Arguments("/recipes/", NavSections.Recipes)]
    [Arguments("/recipes/spicy-ramen-flight/", NavSections.Recipes)]
    [Arguments("/recipes/spicy-ramen-flight/chili-oil-shoyu/", NavSections.Recipes)]
    [Arguments("/account/login/", null)]
    [Arguments("/this-page-does-not-exist", null)]
    [Arguments("/error", null)]
    [Arguments(UmbracoSite.ThrowingPath, null)]
    public async Task TheCurrentSection_FollowsThePagesPlaceInTheTree(string path, string? section)
    {
        using var client = Site.CreateClient();

        var nav = (await RenderedPage.GetAsync(client, path)).HeaderProp("nav");

        _ = await Assert.That(nav.TryGetProperty("currentSection", out var current) ? current.GetString() : null).IsEqualTo(section);
        _ = await Assert.That(nav.GetProperty("urls").GetProperty("home").GetString()).IsEqualTo("/");
    }

    [Test]
    [Arguments(UmbracoSite.ThrowingPath)]
    [Arguments(UmbracoSite.ThrowingPath + "?diet=Vegan")]
    public async Task TheErrorPage_PointsTheNavAtThePageThatFailed(string pathAndQuery)
    {
        using var client = Site.CreateClient();

        var urls = (await RenderedPage.GetAsync(client, pathAndQuery)).HeaderProp("nav").GetProperty("urls");

        _ = await Assert.That(urls.GetProperty("currentPage").GetString()).IsEqualTo(pathAndQuery);
        _ = await Assert.That(urls.GetProperty("signIn").GetString()).IsEqualTo($"/account/login/?returnUrl={Uri.EscapeDataString(pathAndQuery)}");
    }

    [Test]
    public async Task AMember_GetsTheirKitchen_CountedAgainAfterASubmission()
    {
        var userName = TestMembers.UniqueUserName("kitchen");
        var memberKey = await TestMembers.ApprovedAsync(Site.Services, userName);
        TestContent.RenameAuthor(Site.Services, memberKey, "Grace", "Hopper");
        var recipeKey = await TestContent.DraftRecipeAsync(Site.Services, "IT Puffin", memberKey);
        using var visitor = new MemberClient(Site);
        _ = await visitor.SignInAsync(userName, TestMembers.Password);

        var before = (await RenderedPage.GetAsync(visitor.Http, "/recipes/")).HeaderProp("nav");
        await TestContent.DraftVariantAsync(Site.Services, recipeKey, "First Try", memberKey);
        var after = (await RenderedPage.GetAsync(visitor.Http, "/recipes/")).HeaderProp("nav");

        var created = Site.Services.GetRequiredService<IMemberService>().GetById(memberKey)!.CreateDate;
        _ = await Assert.That(before.GetProperty("member").GetProperty("firstName").GetString()).IsEqualTo("Grace");
        _ = await Assert.That(before.GetProperty("member").GetProperty("memberSince").GetString()).IsEqualTo(AccountViewModel.FormatMemberSince(created));
        _ = await Assert.That(Kitchen(before)).IsEqualTo("1 recipes, 0 variants, 1 waiting");
        _ = await Assert.That(Kitchen(after)).IsEqualTo("1 recipes, 1 variants, 2 waiting");
    }

    [Test]
    [Arguments("/recipes/", NavSections.Recipes, "%2Frecipes%2F")]
    [Arguments("/recipes/create-recipe/", NavSections.Recipes, "%2F")]
    [Arguments("/account/", NavSections.Kitchen, "%2F")]
    [Arguments("/account/settings/", NavSections.Kitchen, "%2F")]
    public async Task AMember_SignsOutBackToThePage_OrHomeFromAMembersOnlyPage(string path, string section, string returnUrl)
    {
        using var member = await TestMembers.SignedInAsync(Site, "pad");

        var nav = (await RenderedPage.GetAsync(member.Visitor.Http, path)).HeaderProp("nav");
        var urls = nav.GetProperty("urls");

        _ = await Assert.That(nav.GetProperty("currentSection").GetString()).IsEqualTo(section);
        _ = await Assert.That(urls.GetProperty("signOut").GetString()).IsEqualTo($"/account/logout?returnUrl={returnUrl}");
        _ = await Assert.That(urls.GetProperty("newRecipe").GetString()).IsEqualTo("/recipes/create-recipe/");
        _ = await Assert.That(urls.GetProperty("account").GetString()).IsEqualTo("/account/");
        _ = await Assert.That(urls.GetProperty("settings").GetString()).IsEqualTo("/account/settings/");
        _ = await Assert.That(urls.TryGetProperty("signIn", out _)).IsFalse();
        _ = await Assert.That(urls.TryGetProperty("register", out _)).IsFalse();
    }

    [Test]
    public async Task SigningOutThroughTheNav_ReturnsToThePage()
    {
        using var member = await TestMembers.SignedInAsync(Site, "leave");
        var signOut = (await RenderedPage.GetAsync(member.Visitor.Http, "/recipes/")).HeaderProp("nav").GetProperty("urls").GetProperty("signOut").GetString()!;

        using var response = await member.Visitor.SignOutAsync(signOut);

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
        _ = await Assert.That(response.Headers.Location!.OriginalString).IsEqualTo("/recipes/");
    }

    [Test]
    public async Task TheOwnersPicks_ShapeTheRecipesCard()
    {
        await LibraryTotalAsync();
        try
        {
            await TestSiteSettings.SetAsync(
                Site.Services,
                ("navMeals", TestSiteSettings.Picks(Taxonomy("Recipe Categories", "Dinner"), Taxonomy("Recipe Categories", "Breakfast"))),
                ("navDiets", TestSiteSettings.Picks(Taxonomy("Recipe Tags", "Vegan"), Taxonomy("Recipe Tags", "Spicy"))),
                ("navQuickPicks", TestSiteSettings.QuickPicks(
                    Site.Services,
                    TestSiteSettings.Preset(NavPresets.TopRated, "Best loved"),
                    TestSiteSettings.QuickLink("Soups", "/recipes/?query=soup"))),
                ("navSearchSuggestions", "Soup\nTacos"),
                ("navRecipesNote", "Stuck? Dinner is a safe bet."));

            using var client = Site.CreateClient();

            var nav = (await RenderedPage.GetAsync(client, "/")).HeaderProp("nav");
            var recipes = nav.GetProperty("recipes");

            _ = await Assert.That(Rows(recipes.GetProperty("meals"))).IsEqualTo("Dinner 5, Breakfast 4");
            _ = await Assert.That(Rows(recipes.GetProperty("diets"))).IsEqualTo("Vegan 12");
            _ = await Assert.That(Links(recipes.GetProperty("quickPicks"))).IsEqualTo("Best loved /recipes/?sort=rated, Soups /recipes/?query=soup");
            _ = await Assert.That(Strings(nav.GetProperty("suggestions"))).IsEqualTo("Soup,Tacos");
            _ = await Assert.That(recipes.GetProperty("note").GetString()).IsEqualTo("Stuck? Dinner is a safe bet.");
        }
        finally
        {
            await TestSiteSettings.ClearNavAsync(Site.Services);
        }
    }

    private static string Rows(JsonElement rows) =>
        string.Join(", ", rows.EnumerateArray().Select(row => $"{row.GetProperty("label").GetString()} {row.GetProperty("count").GetInt32()}"));

    private static string Links(JsonElement rows) =>
        string.Join(", ", rows.EnumerateArray().Select(row => $"{row.GetProperty("label").GetString()} {row.GetProperty("url").GetString()}"));

    private static string Strings(JsonElement items) => string.Join(",", items.EnumerateArray().Select(item => item.GetString()));

    private static string Kitchen(JsonElement nav)
    {
        var kitchen = nav.GetProperty("member").GetProperty("kitchen");
        return $"{kitchen.GetProperty("recipes").GetInt32()} recipes, {kitchen.GetProperty("variants").GetInt32()} variants, {kitchen.GetProperty("waiting").GetInt32()} waiting";
    }

    private async Task<int> LibraryTotalAsync()
    {
        await Site.Services.GetRequiredService<IRecipeIndexRebuilder>().WhenCurrentAsync(CancellationToken.None);
        return Site.Services.GetRequiredService<IRecipeSearchService>().Search(new RecipeSearchCriteria()).Total;
    }

    private Guid Taxonomy(string folderName, string name) => TestSiteSettings.TaxonomyKey(Site.Services, folderName, name);
}
