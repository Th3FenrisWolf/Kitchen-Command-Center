using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KCC.Admin;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.Models.Generated;
using KCC.Web.Features.Recipes;
using KCC.Web.Features.Search;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentPublishing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

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
        await AssertNameRefusalAsync(response, "variantName");
    }

    [Test]
    public async Task CreateRecipe_WithATooLongRecipeName_NamesTheRecipeName()
    {
        using var member = await TestMembers.SignedInAsync(Site, "chef");

        using var response = await member.Visitor.PostAsync("/api/recipes", Recipe(new string('a', 256)));

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await AssertNameRefusalAsync(response, "recipeName");
    }

    [Test]
    public async Task AddVariant_WithATooLongName_NamesTheVariantName()
    {
        var recipe = await TestContent.RecipeAsync(Site.Services, "IT Wolverine Gratin");
        using var member = await TestMembers.SignedInAsync(Site, "chef");

        using var response = await member.Visitor.PostAsync($"/api/recipes/{recipe}/variants", Variant(new string('a', 256)));

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await AssertNameRefusalAsync(response, "variantName");
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
        _ = await Assert.That(page.Attribute("recipes-url")).IsEqualTo("/recipes/");
    }

    [Test]
    public async Task WizardPages_SendTheirOwnStrings_AndTheSharedWizardStrings()
    {
        var recipe = await TestContent.RecipeAsync(Site.Services, "IT Wolverine Soup");
        using var member = await TestMembers.SignedInAsync(Site, "chef");

        var create = (await RenderedPage.GetAsync(member.Visitor.Http, "/recipes/create-recipe/")).Prop("resource-strings");
        var add = (await RenderedPage.GetAsync(member.Visitor.Http, $"/recipes/add-variant/?recipe={recipe}")).Prop("resource-strings");

        _ = await Assert.That(create.GetProperty("CreateRecipe.CreateRecipe").GetString()).IsEqualTo("Create Recipe");
        _ = await Assert.That(create.GetProperty("RecipeWizard.Next").GetString()).IsEqualTo("Next");
        _ = await Assert.That(add.GetProperty("AddVariant.BackTo").GetString()).IsEqualTo("Back to");
        _ = await Assert.That(add.GetProperty("RecipeWizard.Next").GetString()).IsEqualTo("Next");
    }

    [Test]
    public async Task CreateRecipe_WithDetails_StoresThemOnTheDraftRecipeAndVariant()
    {
        using var member = await TestMembers.SignedInAsync(Site, "chef");
        var body = Recipe("IT Wolverine Enchiladas", DetailedVariant("Verde"), category: Taxonomy("Dinner"));

        using var response = await member.Visitor.PostAsync("/api/recipes", body);
        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var recipeKey = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("recipeKey").GetGuid();

        try
        {
            await AssertStoredDetailsAsync(recipeKey);
        }
        finally
        {
            await TestContent.TrashQuietlyAsync(Site.Services, recipeKey);
            await Site.Services.GetRequiredService<IRecipeIndexRebuilder>().WhenCurrentAsync(CancellationToken.None);
        }
    }

    private async Task AssertStoredDetailsAsync(Guid recipeKey)
    {
        var (recipe, variant) = Draft(recipeKey);
        _ = await Assert.That(recipe.GetValue<string>("category")).IsEqualTo(DocumentUdi(Taxonomy("Dinner")));
        _ = await Assert.That(variant.GetValue<string>("tags")).IsEqualTo($"{DocumentUdi(Taxonomy("Spicy"))},{DocumentUdi(Taxonomy("Gluten-Free"))}");
        _ = await Assert.That(variant.GetValue<int?>("calories")).IsEqualTo(450);
        _ = await Assert.That(variant.GetValue<int?>("fatG")).IsEqualTo(0);
        foreach (var alias in new[] { "proteinG", "carbsG", "saturatedFatG", "fiberG", "sugarG", "sodiumMg" })
        {
            _ = await Assert.That(variant.GetValue<int?>(alias)).IsNull();
        }

        await PublishAsync(recipe.Key, variant.Key);
        _ = await Assert.That(Published<RecipeVariant, string?>(variant.Key, published => published.Difficulty)).IsEqualTo("Medium");
    }

    [Test]
    public async Task AddVariant_WithDetails_StoresTheTagsDifficultyAndNutrition()
    {
        var recipe = await TestContent.RecipeAsync(Site.Services, "IT Wolverine Tacos");
        using var member = await TestMembers.SignedInAsync(Site, "chef");

        using var response = await member.Visitor.PostAsync($"/api/recipes/{recipe}/variants", DetailedVariant("Al Pastor"));
        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var variantKey = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("variantKey").GetGuid();

        using var scope = Site.Services.CreateScope();
        var variant = scope.ServiceProvider.GetRequiredService<IContentService>().GetById(variantKey)!;
        _ = await Assert.That(variant.GetValue<string>("tags")).IsEqualTo($"{DocumentUdi(Taxonomy("Spicy"))},{DocumentUdi(Taxonomy("Gluten-Free"))}");
        _ = await Assert.That(variant.GetValue<int?>("calories")).IsEqualTo(450);
        _ = await Assert.That(variant.GetValue<int?>("fatG")).IsEqualTo(0);

        try
        {
            await PublishAsync(variantKey);
            _ = await Assert.That(Published<RecipeVariant, string?>(variantKey, published => published.Difficulty)).IsEqualTo("Medium");
        }
        finally
        {
            await TestContent.TrashQuietlyAsync(Site.Services, recipe);
            await Site.Services.GetRequiredService<IRecipeIndexRebuilder>().WhenCurrentAsync(CancellationToken.None);
        }
    }

    [Test]
    public async Task CreateRecipe_WithNoDetails_WritesNoneOfThem()
    {
        using var member = await TestMembers.SignedInAsync(Site, "chef");

        var (recipe, variant) = Draft(await CreateRecipeAsync(member.Visitor, "IT Wolverine Plain"));

        _ = await Assert.That(recipe.GetValue<string>("category")).IsNull();
        _ = await Assert.That(recipe.GetValue<string>("proposedCategory")).IsNull();
        foreach (var alias in new[] { "tags", "difficulty", "proposedTags" })
        {
            _ = await Assert.That(variant.GetValue<string>(alias)).IsNull();
        }

        foreach (var alias in NutritionAliases)
        {
            _ = await Assert.That(variant.GetValue<int?>(alias)).IsNull();
        }
    }

    [Test]
    public async Task CreateRecipe_WithAValueOutOfItsLimit_IsInvalidDetailsAndAddsNothing()
    {
        using var member = await TestMembers.SignedInAsync(Site, "chef");
        var listing = TestContent.RecipeListing(Site.Services);
        var before = ChildCount(listing);

        using var response = await member.Visitor.PostAsync(
            "/api/recipes",
            Recipe("IT Wolverine Feast", DetailedVariant("Huge", calories: 10_001)));

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        _ = await Assert.That(ChildCount(listing)).IsEqualTo(before);
        await AssertDetailsRefusalAsync(response, "calories");
    }

    [Test]
    public async Task AddVariant_WithATooLongProposal_IsInvalidDetails()
    {
        var recipe = await TestContent.RecipeAsync(Site.Services, "IT Wolverine Nachos");
        using var member = await TestMembers.SignedInAsync(Site, "chef");

        using var response = await member.Visitor.PostAsync(
            $"/api/recipes/{recipe}/variants",
            DetailedVariant("Loaded", proposedTags: new string('a', 101)));

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        _ = await Assert.That(ChildCount(recipe)).IsEqualTo(0);
        await AssertDetailsRefusalAsync(response, "proposedTags");
    }

    [Test]
    public async Task CreateRecipe_WithAnUnpublishedCategory_IsDetailsChangedWithTheCurrentChoicesAndAddsNothing()
    {
        var withdrawn = await TestContent.CategoryAsync(Site.Services, "IT Wolverine Withdrawn Category");
        await TestContent.UnpublishAsync(Site.Services, withdrawn);
        try
        {
            await AssertCategoryRefusedAsync(withdrawn);
        }
        finally
        {
            await TestContent.TrashQuietlyAsync(Site.Services, withdrawn);
        }
    }

    private async Task AssertCategoryRefusedAsync(Guid withdrawn)
    {
        using var member = await TestMembers.SignedInAsync(Site, "chef");
        var listing = TestContent.RecipeListing(Site.Services);
        var before = ChildCount(listing);

        using var response = await member.Visitor.PostAsync("/api/recipes", Recipe("IT Wolverine Changed", DetailedVariant("Classic"), category: withdrawn));

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        _ = await Assert.That(ChildCount(listing)).IsEqualTo(before);
        var choices = await AssertDetailsChangedAsync(response);
        var categories = choices.GetProperty("categories").EnumerateArray().Select(category => category.GetProperty("key").GetGuid()).ToList();
        _ = await Assert.That(categories).Contains(Taxonomy("Dinner"));
        _ = await Assert.That(categories).DoesNotContain(withdrawn);
        _ = await Assert.That(choices.GetProperty("tags").EnumerateArray().Any(tag => tag.GetProperty("name").GetString() == "Spicy")).IsTrue();
    }

    [Test]
    public async Task AddVariant_WithAnUnpublishedTag_IsDetailsChangedAndAddsNothing()
    {
        var recipe = await TestContent.RecipeAsync(Site.Services, "IT Wolverine Burrito");
        var withdrawn = await TestContent.TagAsync(Site.Services, "IT Wolverine Withdrawn Tag");
        await TestContent.UnpublishAsync(Site.Services, withdrawn);
        try
        {
            using var member = await TestMembers.SignedInAsync(Site, "chef");

            using var response = await member.Visitor.PostAsync($"/api/recipes/{recipe}/variants", DetailedVariant("Wet", tags: [Taxonomy("Spicy"), withdrawn]));

            _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
            _ = await Assert.That(ChildCount(recipe)).IsEqualTo(0);
            var choices = await AssertDetailsChangedAsync(response);
            _ = await Assert.That(choices.GetProperty("tags").EnumerateArray().Any(tag => tag.GetProperty("key").GetGuid() == withdrawn)).IsFalse();
        }
        finally
        {
            await TestContent.TrashQuietlyAsync(Site.Services, withdrawn);
        }
    }

    [Test]
    public async Task Submissions_WithDetailsButWithoutTheAntiforgeryToken_AreABadRequest()
    {
        using var member = await TestMembers.SignedInAsync(Site, "chef");
        var listing = TestContent.RecipeListing(Site.Services);
        var before = ChildCount(listing);

        using var response = await member.Visitor.Http.PostAsJsonAsync("/api/recipes", Recipe("IT Wolverine Forged", DetailedVariant("Classic"), category: Taxonomy("Dinner")));

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        _ = await Assert.That(ChildCount(listing)).IsEqualTo(before);
    }

    [Test]
    public async Task Proposals_AreStoredOnTheDraft_AndNeverReachThePublicSite()
    {
        using var member = await TestMembers.SignedInAsync(Site, "chef");
        var taxonomyBefore = TaxonomyCount();

        using var response = await member.Visitor.PostAsync(
            "/api/recipes",
            Recipe("IT Wolverine Fajitas", DetailedVariant("Sizzling", proposedTags: " Mexican "), category: Taxonomy("Dinner"), proposedCategory: " Sides "));
        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var recipeKey = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("recipeKey").GetGuid();
        try
        {
            await AssertProposalsStayPrivateAsync(recipeKey);
        }
        finally
        {
            await TestContent.TrashQuietlyAsync(Site.Services, recipeKey);
            await Site.Services.GetRequiredService<IRecipeIndexRebuilder>().WhenCurrentAsync(CancellationToken.None);
        }

        _ = await Assert.That(TaxonomyCount()).IsEqualTo(taxonomyBefore);
    }

    private async Task AssertProposalsStayPrivateAsync(Guid recipeKey)
    {
        var (recipe, variant) = Draft(recipeKey);
        _ = await Assert.That(recipe.GetValue<string>("proposedCategory")).IsEqualTo("Sides");
        _ = await Assert.That(variant.GetValue<string>("proposedTags")).IsEqualTo("Mexican");

        await PublishAsync(recipe.Key, variant.Key);
        await Site.Services.GetRequiredService<IRecipeIndexRebuilder>().WhenCurrentAsync(CancellationToken.None);
        using var client = Site.CreateClient();
        var recipePage = await RenderedPage.GetAsync(client, Published<Recipe, string>(recipe.Key, published => published.Url()));
        var variantPage = await RenderedPage.GetAsync(client, Published<RecipeVariant, string>(variant.Key, published => published.Url()));
        var search = await client.GetStringAsync("/api/recipes/search?query=wolverine%20fajitas");

        _ = await Assert.That(recipePage.Status).IsEqualTo(HttpStatusCode.OK);
        _ = await Assert.That(variantPage.Status).IsEqualTo(HttpStatusCode.OK);
        _ = await Assert.That(search).Contains("IT Wolverine Fajitas");
        foreach (var proposal in new[] { "Mexican", "Sides" })
        {
            var proposalSearch = await client.GetStringAsync($"/api/recipes/search?query={proposal}");
            _ = await Assert.That(proposalSearch).DoesNotContain("IT Wolverine Fajitas");
        }

        foreach (var text in new[] { recipePage.Html, variantPage.Html, search })
        {
            _ = await Assert.That(text).DoesNotContain("Mexican");
            _ = await Assert.That(text).DoesNotContain("Sides");
        }
    }

    private static object Recipe(string name) => Recipe(name, "Slow and Low");

    private static object Recipe(string name, string variantName) => new
    {
        recipeName = name,
        recipeDescription = "Made for one test.",
        firstVariant = Variant(variantName),
    };

    private static object Recipe(string name, object firstVariant, Guid? category = null, string? proposedCategory = null) => new
    {
        recipeName = name,
        recipeDescription = "Made for one test.",
        category,
        proposedCategory,
        firstVariant,
    };

    private static readonly string[] NutritionAliases = ["calories", "proteinG", "carbsG", "fatG", "saturatedFatG", "fiberG", "sugarG", "sodiumMg"];

    private static object DetailedVariant(string name, Guid[]? tags = null, int calories = 450, string? proposedTags = null) => new
    {
        variantName = name,
        variantDescription = "Warming.",
        prepTime = 15,
        cookTime = 120,
        servings = 6,
        ingredients = new[] { new { name = "Beans", quantity = 2, unit = "Cans", isEyeballed = false } },
        instructions = new[] { new { step = 1, text = "Simmer for two hours." } },
        tags = tags ?? [Taxonomy("Spicy"), Taxonomy("Gluten-Free")],
        difficulty = "Medium",
        nutrition = new { calories, proteinG = (int?)null, fatG = 0 },
        proposedTags,
    };

    private static Guid Taxonomy(string name) => TaxonomyDefaults.Values.Single(value => value.Name == name).Key;

    private static string DocumentUdi(Guid key) => Udi.Create(Constants.UdiEntityType.Document, key).ToString();

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

    private static async Task AssertNameRefusalAsync(HttpResponseMessage response, string field)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        _ = await Assert.That(body.GetProperty("code").GetString()).IsEqualTo("nameTooLong");
        _ = await Assert.That(body.GetProperty("field").GetString()).IsEqualTo(field);
    }

    private static async Task AssertDetailsRefusalAsync(HttpResponseMessage response, string field)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        _ = await Assert.That(body.GetProperty("code").GetString()).IsEqualTo("invalidDetails");
        _ = await Assert.That(body.GetProperty("field").GetString()).IsEqualTo(field);
        _ = await Assert.That(body.GetProperty("error").GetString()).IsNotNull();
    }

    private static async Task<JsonElement> AssertDetailsChangedAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        _ = await Assert.That(body.GetProperty("code").GetString()).IsEqualTo("detailsChanged");
        _ = await Assert.That(body.GetProperty("error").GetString()).IsNotNull();
        return body.GetProperty("choices");
    }

    private (IContent Recipe, IContent Variant) Draft(Guid recipeKey)
    {
        using var scope = Site.Services.CreateScope();
        var content = scope.ServiceProvider.GetRequiredService<IContentService>();
        scope.ServiceProvider.GetRequiredService<IDocumentNavigationQueryService>().TryGetChildrenKeys(recipeKey, out var children);
        return (content.GetById(recipeKey)!, content.GetById(children.Single())!);
    }

    private async Task PublishAsync(params Guid[] keys)
    {
        using var scope = Site.Services.CreateScope();
        var publishing = scope.ServiceProvider.GetRequiredService<IContentPublishingService>();
        foreach (var key in keys)
        {
            var published = await publishing.PublishAsync(key, [new CulturePublishScheduleModel { Culture = null }], Constants.Security.SuperUserKey);
            if (!published.Success)
            {
                throw new InvalidOperationException($"Publishing {key} failed: {published.Status}.");
            }
        }
    }

    private TResult Published<T, TResult>(Guid key, Func<T, TResult> read)
        where T : class
    {
        using var context = Site.Services.GetRequiredService<IUmbracoContextFactory>().EnsureUmbracoContext();
        return read((T)context.UmbracoContext.Content!.GetById(key)!);
    }

    private int TaxonomyCount()
    {
        using var scope = Site.Services.CreateScope();
        var content = scope.ServiceProvider.GetRequiredService<IContentService>();
        return content.Count("recipeCategory") + content.Count("recipeTag");
    }

    private int ChildCount(Guid parentKey)
    {
        using var scope = Site.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IDocumentNavigationQueryService>().TryGetChildrenKeys(parentKey, out var children)
            ? children.Count()
            : 0;
    }
}
