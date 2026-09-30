using System.Net;
using System.Text.Json;
using KCC.Contributions;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.Search;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Services;

namespace KCC.IntegrationTests.Features.Backoffice;

// Each test finds its own members, recipes and entries by key: other suites leave drafts, sign-ups and reviews
// behind, so nothing here counts the whole list.
public class ContributionsDashboardApiTests
{
    private const string Base = "/umbraco/management/api/v1/kcc/contributions";

    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task Dashboard_IsForAdministratorsOnly()
    {
        using var editor = await BackofficeClient.EditorAsync(Site);
        using var anonymous = Site.CreateClient();

        using var asEditor = await editor.GetAsync($"{Base}/waiting");
        using var asAnonymous = await anonymous.GetAsync($"{Base}/waiting");

        _ = await Assert.That(asEditor.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
        _ = await Assert.That(asAnonymous.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Waiting_ListsAMemberUntilTheyAreApproved()
    {
        var userName = TestMembers.UniqueUserName("schumann");
        var key = await TestMembers.SignUpAsync(Site.Services, userName);
        using var admin = await BackofficeClient.AdministratorAsync(Site);

        var listed = WaitingMember(await admin.GetJsonAsync($"{Base}/waiting"), key);
        using var approval = await admin.PostAsync($"{Base}/members/{key}/approval");
        var afterwards = WaitingMember(await admin.GetJsonAsync($"{Base}/waiting"), key);

        _ = await Assert.That(listed).IsNotNull();
        _ = await Assert.That(listed!.Value.GetProperty("userName").GetString()).IsEqualTo(userName);
        _ = await Assert.That(listed.Value.GetProperty("email").GetString()).IsEqualTo($"{userName}@example.test");
        _ = await Assert.That(listed.Value.GetProperty("registered").GetDateTime()).IsGreaterThan(DateTime.UtcNow.AddMinutes(-5));
        _ = await Assert.That(approval.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        _ = await Assert.That(afterwards).IsNull();
        _ = await Assert.That(Site.Services.GetRequiredService<IMemberService>().GetById(key)!.IsApproved).IsTrue();
    }

    [Test]
    public async Task Approval_OfAnUnknownMember_IsNotFound()
    {
        using var admin = await BackofficeClient.AdministratorAsync(Site);

        using var approval = await admin.PostAsync($"{Base}/members/{Guid.NewGuid()}/approval");

        _ = await Assert.That(approval.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Waiting_ListsUnpublishedRecipesAndVariants_NewestFirst()
    {
        var author = await TestContent.AuthorAsync(Site.Services, TestMembers.UniqueUserName("boulanger"), "Lili", "Boulanger");
        var draftRecipe = await TestContent.DraftRecipeAsync(Site.Services, "IT Garnet Stew", author);
        var draftVariant = await TestContent.DraftVariantAsync(Site.Services, draftRecipe, "Slow-cooked", author);
        var publishedRecipe = await TestContent.RecipeAsync(Site.Services, "IT Obsidian Pie");
        var newVariant = await TestContent.DraftVariantAsync(Site.Services, publishedRecipe, "Smoky", author);
        using var admin = await BackofficeClient.AdministratorAsync(Site);

        var drafts = (await admin.GetJsonAsync($"{Base}/waiting")).GetProperty("drafts").EnumerateArray().ToList();
        var keys = drafts.Select(draft => draft.GetProperty("key").GetGuid()).ToList();
        var recipe = drafts.Single(draft => draft.GetProperty("key").GetGuid() == draftRecipe);
        var variant = drafts.Single(draft => draft.GetProperty("key").GetGuid() == newVariant);

        _ = await Assert.That(keys).DoesNotContain(publishedRecipe);
        _ = await Assert.That(keys.IndexOf(newVariant)).IsLessThan(keys.IndexOf(draftVariant));
        _ = await Assert.That(keys.IndexOf(draftVariant)).IsLessThan(keys.IndexOf(draftRecipe));
        _ = await Assert.That(recipe.GetProperty("kind").GetString()).IsEqualTo("recipe");
        _ = await Assert.That(recipe.GetProperty("name").GetString()).IsEqualTo("IT Garnet Stew");
        _ = await Assert.That(recipe.GetProperty("authorName").GetString()).IsEqualTo("Lili Boulanger");
        _ = await Assert.That(variant.GetProperty("kind").GetString()).IsEqualTo("variant");
        _ = await Assert.That(variant.GetProperty("recipeName").GetString()).IsEqualTo("IT Obsidian Pie");
    }

    [Test]
    public async Task Reviews_ComeNewestFirst_WithTheirVariantRecipeAndMember()
    {
        var variant = await ReviewedVariantAsync("IT Jasper Loaf", 4.5m, "Dense crumb.");
        using var admin = await BackofficeClient.AdministratorAsync(Site);

        var page = await admin.GetJsonAsync($"{Base}/reviews?page=0&pageSize=5");
        var newest = page.GetProperty("items")[0];

        _ = await Assert.That(page.GetProperty("pageSize").GetInt32()).IsEqualTo(5);
        _ = await Assert.That(newest.GetProperty("variantKey").GetGuid()).IsEqualTo(variant);
        _ = await Assert.That(newest.GetProperty("variantName").GetString()).IsEqualTo("Classic");
        _ = await Assert.That(newest.GetProperty("recipeName").GetString()).IsEqualTo("IT Jasper Loaf");
        _ = await Assert.That(newest.GetProperty("memberName").GetString()).IsEqualTo("Clara Schumann");
        _ = await Assert.That(newest.GetProperty("rating").GetDecimal()).IsEqualTo(4.5m);
        _ = await Assert.That(newest.GetProperty("text").GetString()).IsEqualTo("Dense crumb.");
    }

    [Test]
    public async Task Review_IsEditedThenDeleted()
    {
        var variant = await ReviewedVariantAsync("IT Malachite Tart", 2m, "Too sweet.");
        using var admin = await BackofficeClient.AdministratorAsync(Site);
        var id = await NewestIdAsync(admin, "reviews", variant);

        using var edited = await admin.PutAsync($"{Base}/reviews/{id}", new { rating = 3.5, text = "Sweet, but fair." });
        var afterEdit = (await admin.GetJsonAsync($"{Base}/reviews?page=0&pageSize=5")).GetProperty("items")[0];
        using var deleted = await admin.DeleteAsync($"{Base}/reviews/{id}");
        using var again = await admin.DeleteAsync($"{Base}/reviews/{id}");

        _ = await Assert.That(edited.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        _ = await Assert.That(afterEdit.GetProperty("rating").GetDecimal()).IsEqualTo(3.5m);
        _ = await Assert.That(afterEdit.GetProperty("text").GetString()).IsEqualTo("Sweet, but fair.");
        _ = await Assert.That(deleted.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        _ = await Assert.That(again.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    [Arguments(0)]
    [Arguments(5.5)]
    [Arguments(2.25)]
    public async Task ReviewEdit_OffTheHalfStarScale_IsRefused(decimal rating)
    {
        var variant = await ReviewedVariantAsync($"IT Topaz {rating}", 4m, "Fine.");
        using var admin = await BackofficeClient.AdministratorAsync(Site);
        var id = await NewestIdAsync(admin, "reviews", variant);

        using var edited = await admin.PutAsync($"{Base}/reviews/{id}", new { rating, text = "Changed." });

        _ = await Assert.That(edited.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Edits_ToAnUnknownEntry_AreNotFound()
    {
        using var admin = await BackofficeClient.AdministratorAsync(Site);

        using var review = await admin.PutAsync($"{Base}/reviews/{int.MaxValue}", new { rating = 4, text = "Gone." });
        using var note = await admin.PutAsync($"{Base}/notes/{int.MaxValue}", new { text = "Gone." });
        using var deletedNote = await admin.DeleteAsync($"{Base}/notes/{int.MaxValue}");

        _ = await Assert.That(review.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        _ = await Assert.That(note.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        _ = await Assert.That(deletedNote.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CookNote_IsListedEditedAndDeleted_AndNeedsText()
    {
        var recipe = await TestContent.RecipeAsync(Site.Services, "IT Basalt Bake");
        var variant = await TestContent.VariantAsync(Site.Services, recipe, "Classic");
        var member = await TestContent.AuthorAsync(Site.Services, TestMembers.UniqueUserName("schumann"), "Clara", "Schumann");
        await Site.Services.GetRequiredService<IContributionWrites>().AddNoteAsync(variant, member, "Needs more salt.");
        using var admin = await BackofficeClient.AdministratorAsync(Site);
        var id = await NewestIdAsync(admin, "notes", variant);

        var listed = (await admin.GetJsonAsync($"{Base}/notes?page=0&pageSize=5")).GetProperty("items")[0];
        using var blank = await admin.PutAsync($"{Base}/notes/{id}", new { text = "   " });
        using var edited = await admin.PutAsync($"{Base}/notes/{id}", new { text = "  Needs a pinch more salt.  " });
        var afterEdit = (await admin.GetJsonAsync($"{Base}/notes?page=0&pageSize=5")).GetProperty("items")[0];
        using var deleted = await admin.DeleteAsync($"{Base}/notes/{id}");
        var afterDelete = (await admin.GetJsonAsync($"{Base}/notes?page=0&pageSize=50")).GetProperty("items").EnumerateArray()
            .Select(note => note.GetProperty("id").GetInt32());

        _ = await Assert.That(listed.GetProperty("rating").ValueKind).IsEqualTo(JsonValueKind.Null);
        _ = await Assert.That(listed.GetProperty("recipeName").GetString()).IsEqualTo("IT Basalt Bake");
        _ = await Assert.That(blank.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        _ = await Assert.That(edited.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        _ = await Assert.That(afterEdit.GetProperty("text").GetString()).IsEqualTo("Needs a pinch more salt.");
        _ = await Assert.That(deleted.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        _ = await Assert.That(afterDelete).DoesNotContain(id);
    }

    [Test]
    public async Task ReviewModeration_ReachesTheSearchIndex()
    {
        var variant = await ReviewedVariantAsync("IT Quartz Cake", 4m, "Light.");
        using var admin = await BackofficeClient.AdministratorAsync(Site);
        var id = await NewestIdAsync(admin, "reviews", variant);
        var reviewed = await SearchWhenCurrentAsync("quartz");

        using var edited = await admin.PutAsync($"{Base}/reviews/{id}", new { rating = 2, text = "Heavy after all." });
        var afterEdit = await SearchWhenCurrentAsync("quartz");
        using var deleted = await admin.DeleteAsync($"{Base}/reviews/{id}");
        var afterDelete = await SearchWhenCurrentAsync("quartz");

        _ = await Assert.That(reviewed.AverageRating).IsEqualTo(4d);
        _ = await Assert.That(edited.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        _ = await Assert.That(afterEdit.AverageRating).IsEqualTo(2d);
        _ = await Assert.That(deleted.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        _ = await Assert.That(afterDelete.ReviewCount).IsEqualTo(0);
        _ = await Assert.That(afterDelete.AverageRating).IsNull();
    }

    private static JsonElement? WaitingMember(JsonElement waiting, Guid key)
    {
        foreach (var member in waiting.GetProperty("members").EnumerateArray())
        {
            if (member.GetProperty("key").GetGuid() == key)
            {
                return member;
            }
        }

        return null;
    }

    private static async Task<int> NewestIdAsync(BackofficeClient admin, string kind, Guid variant)
    {
        var newest = (await admin.GetJsonAsync($"{Base}/{kind}?page=0&pageSize=1")).GetProperty("items")[0];
        if (newest.GetProperty("variantKey").GetGuid() != variant)
        {
            throw new InvalidOperationException($"The newest entry in {kind} is not this test's.");
        }

        return newest.GetProperty("id").GetInt32();
    }

    private async Task<Guid> ReviewedVariantAsync(string recipeName, decimal rating, string text)
    {
        var recipe = await TestContent.RecipeAsync(Site.Services, recipeName);
        var variant = await TestContent.VariantAsync(Site.Services, recipe, "Classic");
        var member = await TestContent.AuthorAsync(Site.Services, TestMembers.UniqueUserName("schumann"), "Clara", "Schumann");
        await Site.Services.GetRequiredService<IContributionWrites>().UpsertReviewAsync(variant, member, rating, text);
        return variant;
    }

    private async Task<RecipeSearchHit> SearchWhenCurrentAsync(string word)
    {
        await Site.Services.GetRequiredService<IRecipeIndexRebuilder>().WhenCurrentAsync(CancellationToken.None);
        return Site.Services.GetRequiredService<IRecipeSearchService>().Search(new RecipeSearchCriteria { Query = word }).Results.Single();
    }
}
