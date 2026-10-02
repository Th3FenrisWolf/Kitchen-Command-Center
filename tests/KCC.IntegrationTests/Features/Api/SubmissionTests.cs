using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KCC.Admin;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.Search;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;

namespace KCC.IntegrationTests.Features.Api;

public class SubmissionTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task CreateRecipe_SavesADraftUnderTheListing_WithItsFirstVariant()
    {
        using var member = await TestMembers.SignedInAsync(Site, "chef");

        var recipeKey = await CreateRecipeAsync(member.Visitor, "IT Wolverine Stew");

        using var scope = Site.Services.CreateScope();
        var content = scope.ServiceProvider.GetRequiredService<IContentService>();
        var recipe = content.GetById(recipeKey)!;
        scope.ServiceProvider.GetRequiredService<IDocumentNavigationQueryService>().TryGetChildrenKeys(recipeKey, out var children);
        var variant = content.GetById(children.Single())!;
        _ = await Assert.That(recipe.ParentId).IsEqualTo(content.GetById(TestContent.RecipeListing(Site.Services))!.Id);
        _ = await Assert.That(recipe.Published).IsFalse();
        _ = await Assert.That(variant.Published).IsFalse();
        _ = await Assert.That(variant.Name).IsEqualTo("Slow and Low");
        _ = await Assert.That(recipe.GetValue<string>("icon")).IsEqualTo(RecipeIcons.Fallback("IT Wolverine Stew"));
        _ = await Assert.That(recipe.GetValue<string>("author")).IsEqualTo(Udi.Create(Constants.UdiEntityType.Member, member.Key).ToString());
        _ = await Assert.That(variant.GetValue<string>("ingredients")).IsEqualTo("""[{"name":"Beans","quantity":2,"unit":"Cans","isEyeballed":false}]""");
    }

    [Test]
    public async Task CreateRecipe_WithATooLongFirstVariantName_IsRejectedAndAddsNothing()
    {
        using var member = await TestMembers.SignedInAsync(Site, "chef");
        var listing = TestContent.RecipeListing(Site.Services);
        var before = ChildCount(listing);

        using var response = await member.Visitor.PostAsync("/api/recipes", Recipe("IT Wolverine Overlong", new string('a', 256)));

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        _ = await Assert.That(ChildCount(listing)).IsEqualTo(before);
    }

    [Test]
    public async Task CreateRecipe_StaysOutOfSearch_AndShowsAsPendingOnTheAccountPage()
    {
        using var member = await TestMembers.SignedInAsync(Site, "chef");

        _ = await CreateRecipeAsync(member.Visitor, "IT Wolverine Chili");
        await Site.Services.GetRequiredService<IRecipeIndexRebuilder>().WhenCurrentAsync(CancellationToken.None);

        var search = Site.Services.GetRequiredService<IRecipeSearchService>().Search(new RecipeSearchCriteria { Query = "wolverine chili" });
        var groups = (await RenderedPage.GetAsync(member.Visitor.Http, "/account/")).Prop("recipe-groups").EnumerateArray().ToList();
        _ = await Assert.That(search.Results.Any(hit => hit.Name == "IT Wolverine Chili")).IsFalse();
        _ = await Assert.That(groups.Single().GetProperty("recipeName").GetString()).IsEqualTo("IT Wolverine Chili");
        _ = await Assert.That(groups.Single().GetProperty("isPending").GetBoolean()).IsTrue();
    }

    [Test]
    public async Task AddVariant_ToAPublishedRecipe_SavesADraftUnderIt()
    {
        var recipe = await TestContent.RecipeAsync(Site.Services, "IT Wolverine Pie");
        using var member = await TestMembers.SignedInAsync(Site, "chef");

        using var response = await member.Visitor.PostAsync($"/api/recipes/{recipe}/variants", Variant("Deep Dish"));
        var variantKey = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("variantKey").GetGuid();

        using var scope = Site.Services.CreateScope();
        var variant = scope.ServiceProvider.GetRequiredService<IContentService>().GetById(variantKey)!;
        _ = await Assert.That(variant.ParentId).IsEqualTo(scope.ServiceProvider.GetRequiredService<IContentService>().GetById(recipe)!.Id);
        _ = await Assert.That(variant.Published).IsFalse();
    }

    [Test]
    public async Task AddVariant_ToAnythingButAPublishedRecipe_IsNotFound()
    {
        using var member = await TestMembers.SignedInAsync(Site, "chef");
        var draft = await TestContent.DraftRecipeAsync(Site.Services, "IT Wolverine Draft", member.Key);
        var published = await TestContent.RecipeAsync(Site.Services, "IT Wolverine Tart");
        var variant = await TestContent.VariantAsync(Site.Services, published, "Classic");

        using var toDraft = await member.Visitor.PostAsync($"/api/recipes/{draft}/variants", Variant("Nope"));
        using var toVariant = await member.Visitor.PostAsync($"/api/recipes/{variant}/variants", Variant("Nope"));
        using var toNothing = await member.Visitor.PostAsync($"/api/recipes/{Guid.NewGuid()}/variants", Variant("Nope"));

        _ = await Assert.That(toDraft.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        _ = await Assert.That(toVariant.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        _ = await Assert.That(toNothing.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Submissions_SignedOut_AreUnauthorized()
    {
        using var visitor = new MemberClient(Site);

        using var response = await visitor.PostAsync("/api/recipes", Recipe("IT Wolverine Ghost"));

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Submissions_AllowFiveAnHourPerClient()
    {
        using var member = await TestMembers.SignedInAsync(Site, "chef");
        for (var submission = 0; submission < 5; submission++)
        {
            _ = await CreateRecipeAsync(member.Visitor, $"IT Wolverine Batch {submission}");
        }

        using var limited = await member.Visitor.PostAsync("/api/recipes", Recipe("IT Wolverine Batch 5"));

        _ = await Assert.That(limited.StatusCode).IsEqualTo(HttpStatusCode.TooManyRequests);
        var retryAfter = limited.Headers.RetryAfter?.Delta ?? TimeSpan.Zero;
        _ = await Assert.That(retryAfter.TotalSeconds).IsGreaterThan(60);
        var error = (await limited.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString();
        _ = await Assert.That(error!.Contains("minute", StringComparison.OrdinalIgnoreCase)).IsFalse();
    }

    [Test]
    public async Task WizardPages_SendASignedOutVisitorToSignIn()
    {
        var recipe = await TestContent.RecipeAsync(Site.Services, "IT Wolverine Cake");
        using var visitor = new MemberClient(Site);

        using var create = await visitor.Http.GetAsync("/recipes/create-recipe/");
        using var add = await visitor.Http.GetAsync($"/recipes/add-variant/?recipe={recipe}");

        _ = await Assert.That(create.Headers.Location!.OriginalString).IsEqualTo("/account/login/?returnUrl=%2Frecipes%2Fcreate-recipe%2F");
        _ = await Assert.That(add.Headers.Location!.OriginalString).IsEqualTo($"/account/login/?returnUrl=%2Frecipes%2Fadd-variant%2F%3Frecipe%3D{recipe}");
    }

    [Test]
    public async Task AddVariantPage_NamesThePublishedRecipe_AndRefusesAnyOther()
    {
        var recipe = await TestContent.RecipeAsync(Site.Services, "IT Wolverine Bread");
        using var member = await TestMembers.SignedInAsync(Site, "chef");
        var draft = await TestContent.DraftRecipeAsync(Site.Services, "IT Wolverine Dough", member.Key);

        var page = await RenderedPage.GetAsync(member.Visitor.Http, $"/recipes/add-variant/?recipe={recipe}");
        var refused = await RenderedPage.GetAsync(member.Visitor.Http, $"/recipes/add-variant/?recipe={draft}");
        var missing = await RenderedPage.GetAsync(member.Visitor.Http, "/recipes/add-variant/");

        _ = await Assert.That(page.Attribute("recipe-id")).IsEqualTo(recipe.ToString());
        _ = await Assert.That(page.Attribute("recipe-name")).IsEqualTo("IT Wolverine Bread");
        _ = await Assert.That(page.Attribute("recipe-slug")).IsEqualTo("/recipes/it-wolverine-bread/");
        _ = await Assert.That(refused.Status).IsEqualTo(HttpStatusCode.NotFound);
        _ = await Assert.That(missing.Status).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CreateRecipePage_RendersTheWizardForAMember()
    {
        using var member = await TestMembers.SignedInAsync(Site, "chef");

        var page = await RenderedPage.GetAsync(member.Visitor.Http, "/recipes/create-recipe/");

        _ = await Assert.That(page.Status).IsEqualTo(HttpStatusCode.OK);
        _ = await Assert.That(page.Body).Contains("<CreateRecipeView");
    }

    private static object Recipe(string name) => Recipe(name, "Slow and Low");

    private static object Recipe(string name, string variantName) => new
    {
        recipeName = name,
        recipeDescription = "Made for one test.",
        firstVariant = Variant(variantName),
    };

    private static object Variant(string name) => new
    {
        variantName = name,
        variantDescription = "Warming.",
        prepTime = 15,
        cookTime = 120,
        servings = 6,
        ingredients = new[] { new { name = "Beans", quantity = 2, unit = "Cans", isEyeballed = false } },
        instructions = new[] { new { step = 1, text = "Simmer for two hours." } },
    };

    private static async Task<Guid> CreateRecipeAsync(MemberClient visitor, string name)
    {
        using var response = await visitor.PostAsync("/api/recipes", Recipe(name));
        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("recipeKey").GetGuid();
    }

    private int ChildCount(Guid parentKey)
    {
        using var scope = Site.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IDocumentNavigationQueryService>().TryGetChildrenKeys(parentKey, out var children)
            ? children.Count()
            : 0;
    }
}
