using KCC.Web.Features.Components.Header;
using KCC.Web.Features.Pages.Account;
using KCC.Web.Features.Recipes;
using KCC.Web.Features.Search;
using TUnit.Assertions.Enums;

namespace KCC.UnitTests.Features.Components.Header;

public class NavModelBuilderTests
{
    private static readonly NavMember Grace = new("Grace", "October 2026", new KitchenSummary(3, 5, 1));

    private static readonly Dictionary<string, string> Labels = new()
    {
        ["Nav.UnderThirtyMinutes"] = "Under 30 minutes",
        ["Nav.TopRated"] = "Top rated",
        ["Nav.MostVariants"] = "Most variants",
        ["Nav.Newest"] = "Newest",
        ["Nav.SurpriseMe"] = "Surprise me",
    };

    private static readonly NavPageUrls Pages = new(
        "/",
        "/recipes/",
        "/recipes/create-recipe/",
        "/account/login/",
        "/account/",
        "/account/settings/",
        "/account/logout",
        "/surprise-me");

    [Test]
    public async Task AVisitor_GetsSignInBackToThePage_AndRegister_AndNoMemberLinks()
    {
        var model = NavModelBuilder.Build(Input());

        _ = await Assert.That(model.Member).IsNull();
        _ = await Assert.That(model.Urls.SignIn).IsEqualTo("/account/login/?returnUrl=%2Frecipes%2F%3Fdiet%3DVegan");
        _ = await Assert.That(model.Urls.Register).IsEqualTo("/account/login/?mode=register");
        _ = await Assert.That(model.Urls.NewRecipe).IsNull();
        _ = await Assert.That(model.Urls.Account).IsNull();
        _ = await Assert.That(model.Urls.Settings).IsNull();
        _ = await Assert.That(model.Urls.SignOut).IsNull();
    }

    [Test]
    public async Task AMember_GetsTheirKitchenAndNewRecipe_AndNoSignIn()
    {
        var model = NavModelBuilder.Build(Input() with { Member = Grace });

        _ = await Assert.That(model.Member).IsEqualTo(Grace);
        _ = await Assert.That(model.Urls.NewRecipe).IsEqualTo("/recipes/create-recipe/");
        _ = await Assert.That(model.Urls.Account).IsEqualTo("/account/");
        _ = await Assert.That(model.Urls.Settings).IsEqualTo("/account/settings/");
        _ = await Assert.That(model.Urls.SignIn).IsNull();
        _ = await Assert.That(model.Urls.Register).IsNull();
    }

    [Test]
    public async Task Everyone_GetsHomeTheLibrarySurpriseMeAndThisPage()
    {
        foreach (var model in new[] { NavModelBuilder.Build(Input()), NavModelBuilder.Build(Input() with { Member = Grace }) })
        {
            _ = await Assert.That(model.Urls.Home).IsEqualTo("/");
            _ = await Assert.That(model.Urls.Library).IsEqualTo("/recipes/");
            _ = await Assert.That(model.Urls.SurpriseMe).IsEqualTo("/surprise-me");
            _ = await Assert.That(model.Urls.CurrentPage).IsEqualTo("/recipes/?diet=Vegan");
        }
    }

    [Test]
    public async Task SignOut_ReturnsToThePageAndItsQuery()
    {
        var urls = NavModelBuilder.Build(Input() with { Member = Grace }).Urls;

        _ = await Assert.That(urls.SignOut).IsEqualTo("/account/logout?returnUrl=%2Frecipes%2F%3Fdiet%3DVegan");
    }

    [Test]
    [Arguments("accountPage,homePage")]
    [Arguments("accountSettingsPage,accountPage,homePage")]
    [Arguments("createRecipePage,recipeListingPage,homePage")]
    [Arguments("addVariantPage,recipeListingPage,homePage")]
    public async Task SignOut_FromAMembersOnlyPage_ReturnsHome(string pageTypes)
    {
        var urls = NavModelBuilder.Build(Input() with { Member = Grace, PageTypes = pageTypes.Split(',') }).Urls;

        _ = await Assert.That(urls.SignOut).IsEqualTo("/account/logout?returnUrl=%2F");
    }

    [Test]
    [Arguments("recipeListingPage,homePage")]
    [Arguments("recipe,recipeListingPage,homePage")]
    [Arguments("recipeVariant,recipe,recipeListingPage,homePage")]
    [Arguments("createRecipePage,recipeListingPage,homePage")]
    [Arguments("addVariantPage,recipeListingPage,homePage")]
    public async Task Recipes_IsCurrentAnywhereInTheLibrary(string pageTypes)
    {
        var model = NavModelBuilder.Build(Input() with { PageTypes = pageTypes.Split(',') });

        _ = await Assert.That(model.CurrentSection).IsEqualTo(NavSections.Recipes);
    }

    [Test]
    [Arguments("accountPage,homePage")]
    [Arguments("accountSettingsPage,accountPage,homePage")]
    public async Task MyKitchen_IsCurrentOnAccountAndSettings_ForAMemberOnly(string pageTypes)
    {
        var member = NavModelBuilder.Build(Input() with { Member = Grace, PageTypes = pageTypes.Split(',') });
        var visitor = NavModelBuilder.Build(Input() with { PageTypes = pageTypes.Split(',') });

        _ = await Assert.That(member.CurrentSection).IsEqualTo(NavSections.Kitchen);
        _ = await Assert.That(visitor.CurrentSection).IsNull();
    }

    [Test]
    [Arguments("homePage")]
    [Arguments("loginPage,accountPage,homePage")]
    [Arguments("statusCodePage,contentFolder")]
    [Arguments("")]
    public async Task Nothing_IsCurrentElsewhere(string pageTypes)
    {
        var model = NavModelBuilder.Build(Input() with
        {
            Member = Grace,
            PageTypes = pageTypes.Split(',', StringSplitOptions.RemoveEmptyEntries),
        });

        _ = await Assert.That(model.CurrentSection).IsNull();
    }

    [Test]
    public async Task Meals_AreEveryCategoryInTreeOrder_WithItsCountIconAndLibraryLink()
    {
        var meals = NavModelBuilder.Build(Input()).Recipes.Meals;

        _ = await Assert.That(meals).IsEquivalentTo(
            [
                new NavRow("Breakfast", "/recipes/?category=Breakfast", 4, "fa-duotone fa-egg"),
                new NavRow("Lunch", "/recipes/?category=Lunch", 3),
                new NavRow("Dinner", "/recipes/?category=Dinner", 5, "fa-duotone fa-pot-food"),
            ],
            CollectionOrdering.Matching);
    }

    [Test]
    public async Task Meals_FollowTheOwnersPicks_InTheirOrder()
    {
        var settings = NavSettings.Empty with { Meals = ["Dinner", "Breakfast", "Dinner", "Brunch"] };

        var meals = NavModelBuilder.Build(Input() with { Settings = settings }).Recipes.Meals;

        _ = await Assert.That(Names(meals)).IsEqualTo("Dinner,Breakfast");
    }

    [Test]
    public async Task Diets_AreEveryDiet_WithItsCountAndLibraryLink()
    {
        var diets = NavModelBuilder.Build(Input()).Recipes.Diets;

        _ = await Assert.That(diets).IsEquivalentTo(
            [
                new NavRow("Vegetarian", "/recipes/?diet=Vegetarian", 6),
                new NavRow("Vegan", "/recipes/?diet=Vegan", 5),
                new NavRow("Keto", "/recipes/?diet=Keto", 1),
            ],
            CollectionOrdering.Matching);
    }

    [Test]
    public async Task Diets_KeepOnlyThePicksThatAreDiets()
    {
        var settings = NavSettings.Empty with { Diets = ["Spicy", "Keto", "Vegan"] };

        var diets = NavModelBuilder.Build(Input() with { Settings = settings }).Recipes.Diets;

        _ = await Assert.That(Names(diets)).IsEqualTo("Keto,Vegan");
    }

    [Test]
    public async Task Picks_ThatAllFallAway_FallBackToEverything()
    {
        var settings = NavSettings.Empty with { Meals = ["Brunch"], Diets = ["Spicy"] };

        var recipes = NavModelBuilder.Build(Input() with { Settings = settings }).Recipes;

        _ = await Assert.That(Names(recipes.Meals)).IsEqualTo("Breakfast,Lunch,Dinner");
        _ = await Assert.That(Names(recipes.Diets)).IsEqualTo("Vegetarian,Vegan,Keto");
    }

    [Test]
    public async Task MealsAndDiets_LeaveOutWhatHasNoRecipes()
    {
        var counts = Input().Counts with
        {
            Categories = new Dictionary<string, int> { ["Dinner"] = 5 },
            Diets = new Dictionary<string, int> { ["Vegan"] = 5 },
        };

        var recipes = NavModelBuilder.Build(Input() with { Counts = counts }).Recipes;

        _ = await Assert.That(Names(recipes.Meals)).IsEqualTo("Dinner");
        _ = await Assert.That(Names(recipes.Diets)).IsEqualTo("Vegan");
    }

    [Test]
    public async Task QuickPicks_AreTheFivePresets_WithTheirLabelsGlyphsLinksAndTheQuickCount()
    {
        var quickPicks = NavModelBuilder.Build(Input()).Recipes.QuickPicks;

        _ = await Assert.That(quickPicks).IsEquivalentTo(
            [
                new NavRow("Under 30 minutes", "/recipes/?timeMax=30", 7, "fa-duotone fa-stopwatch"),
                new NavRow("Top rated", "/recipes/?sort=rated", Icon: "fa-duotone fa-star"),
                new NavRow("Most variants", "/recipes/?sort=variants", Icon: "fa-duotone fa-layer-group"),
                new NavRow("Newest", "/recipes/?sort=recent", Icon: "fa-duotone fa-sparkles"),
                new NavRow("Surprise me", "/surprise-me", Icon: "fa-duotone fa-dice"),
            ],
            CollectionOrdering.Matching);
    }

    [Test]
    public async Task QuickPicks_FollowTheBlocks_WithTheirOwnLabelsAndLinks()
    {
        var settings = NavSettings.Empty with
        {
            QuickPicks =
            [
                new NavQuickPickSetting(NavPresets.TopRated, "Best loved", null, null),
                new NavQuickPickSetting(null, "Soups", "/recipes/?query=soup", null),
                new NavQuickPickSetting(null, "Our shop", "https://example.test/", "_blank"),
                new NavQuickPickSetting(NavPresets.Newest, null, null, null),
            ],
        };

        var quickPicks = NavModelBuilder.Build(Input() with { Settings = settings }).Recipes.QuickPicks;

        _ = await Assert.That(quickPicks).IsEquivalentTo(
            [
                new NavRow("Best loved", "/recipes/?sort=rated", Icon: "fa-duotone fa-star"),
                new NavRow("Soups", "/recipes/?query=soup"),
                new NavRow("Our shop", "https://example.test/", Target: "_blank"),
                new NavRow("Newest", "/recipes/?sort=recent", Icon: "fa-duotone fa-sparkles"),
            ],
            CollectionOrdering.Matching);
    }

    [Test]
    public async Task QuickPicks_KeepTheQuickCount_WhenTheOwnerRelabelsUnderThirtyMinutes()
    {
        var settings = NavSettings.Empty with
        {
            QuickPicks = [new NavQuickPickSetting(NavPresets.UnderThirtyMinutes, "Quick ones", null, null)],
        };

        var quickPicks = NavModelBuilder.Build(Input() with { Settings = settings }).Recipes.QuickPicks;

        _ = await Assert.That(quickPicks).IsEquivalentTo(
            [new NavRow("Quick ones", "/recipes/?timeMax=30", 7, "fa-duotone fa-stopwatch")],
            CollectionOrdering.Matching);
    }

    [Test]
    public async Task QuickPicks_LeaveOutAPresetWithNothingBehindIt()
    {
        var slow = NavModelBuilder.Build(Input() with { Counts = Input().Counts with { UnderThirtyMinutes = 0 } }).Recipes;
        var empty = NavModelBuilder.Build(Input() with
        {
            Counts = new LibraryCounts(0, new Dictionary<string, int>(), new Dictionary<string, int>(), 0),
        }).Recipes;

        _ = await Assert.That(Names(slow.QuickPicks)).IsEqualTo("Top rated,Most variants,Newest,Surprise me");
        _ = await Assert.That(empty.QuickPicks).IsEmpty();
        _ = await Assert.That(empty.Meals).IsEmpty();
        _ = await Assert.That(empty.Diets).IsEmpty();
    }

    [Test]
    public async Task QuickPicks_LeaveOutAnUnknownPresetAndAHalfFilledLink()
    {
        var settings = NavSettings.Empty with
        {
            QuickPicks =
            [
                new NavQuickPickSetting("Most loved", null, null, null),
                new NavQuickPickSetting(null, "  ", "/recipes/", null),
                new NavQuickPickSetting(null, "Soups", null, null),
                new NavQuickPickSetting(null, "Stews", "/recipes/?query=stew", null),
            ],
        };

        var quickPicks = NavModelBuilder.Build(Input() with { Settings = settings }).Recipes.QuickPicks;

        _ = await Assert.That(Names(quickPicks)).IsEqualTo("Stews");
    }

    [Test]
    public async Task WithoutALibrary_OnlySurpriseMeAndQuickLinksRemain()
    {
        var settings = NavSettings.Empty with
        {
            QuickPicks =
            [
                new NavQuickPickSetting(NavPresets.TopRated, null, null, null),
                new NavQuickPickSetting(NavPresets.SurpriseMe, null, null, null),
                new NavQuickPickSetting(null, "Soups", "/soups/", null),
            ],
        };

        var recipes = NavModelBuilder.Build(Input() with { Urls = Pages with { Library = null }, Settings = settings }).Recipes;

        _ = await Assert.That(recipes.Meals).IsEmpty();
        _ = await Assert.That(recipes.Diets).IsEmpty();
        _ = await Assert.That(Names(recipes.QuickPicks)).IsEqualTo("Surprise me,Soups");
    }

    [Test]
    public async Task Suggestions_AreTheThreeBiggestMeals_WhenTheOwnerSetsNone()
    {
        var input = Input() with
        {
            Taxonomy = new RecipeTaxonomy(["Breakfast", "Lunch", "Dinner", "Snack"], ["Vegan"], []),
            Counts = Input().Counts with
            {
                Categories = new Dictionary<string, int> { ["Breakfast"] = 4, ["Lunch"] = 4, ["Dinner"] = 5, ["Snack"] = 4 },
            },
        };

        _ = await Assert.That(string.Join(",", NavModelBuilder.Build(input).Suggestions)).IsEqualTo("Dinner,Breakfast,Lunch");
    }

    [Test]
    public async Task Suggestions_AreTheOwnersPhrases_WhenSet()
    {
        var model = NavModelBuilder.Build(Input() with { Settings = NavSettings.Empty with { SearchSuggestions = ["Soup", "Tacos"] } });

        _ = await Assert.That(string.Join(",", model.Suggestions)).IsEqualTo("Soup,Tacos");
    }

    [Test]
    public async Task TheTotalNoteAndLabels_PassThrough()
    {
        var model = NavModelBuilder.Build(Input() with { Settings = NavSettings.Empty with { RecipesNote = "Stuck? Dinner is a safe bet." } });

        _ = await Assert.That(model.RecipeTotal).IsEqualTo(12);
        _ = await Assert.That(model.Recipes.Note).IsEqualTo("Stuck? Dinner is a safe bet.");
        _ = await Assert.That(model.Labels).IsEqualTo(Labels);
    }

    private static NavInput Input() => new()
    {
        PathAndQuery = "/recipes/?diet=Vegan",
        PageTypes = ["recipeListingPage", "homePage"],
        Urls = Pages,
        Taxonomy = new RecipeTaxonomy(["Breakfast", "Lunch", "Dinner"], ["Vegetarian", "Vegan", "Keto"], ["Spicy"]),
        CategoryIcons = new Dictionary<string, string> { ["Breakfast"] = "fa-duotone fa-egg", ["Dinner"] = "fa-duotone fa-pot-food" },
        Settings = NavSettings.Empty,
        Counts = new LibraryCounts(
            12,
            new Dictionary<string, int> { ["Breakfast"] = 4, ["Lunch"] = 3, ["Dinner"] = 5 },
            new Dictionary<string, int> { ["Vegetarian"] = 6, ["Vegan"] = 5, ["Keto"] = 1 },
            7),
        Labels = Labels,
    };

    private static string Names(IEnumerable<NavRow> rows) => string.Join(",", rows.Select(row => row.Label));
}
