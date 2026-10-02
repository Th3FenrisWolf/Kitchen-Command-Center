using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.Search;
using Microsoft.Extensions.DependencyInjection;

namespace KCC.IntegrationTests.Features.Api;

// Each test writes to a variant it made, so no count here depends on another test.
public class ContributionWriteApiTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task Review_IsWrittenEditedAndDeleted_AsOneReviewPerMember()
    {
        var variant = await VariantAsync("IT Narwhal");
        using var member = await SignedInAsync();

        await ExpectOkAsync(await member.PutAsync($"/api/variant/{variant}/review", new { rating = 4.5, text = "Crisp edges." }));
        await ExpectOkAsync(await member.PutAsync($"/api/variant/{variant}/review", new { rating = 3, text = "Softer the next day." }));
        var reviews = await ReviewsAsync(member, variant);

        _ = await Assert.That(reviews.GetProperty("total").GetInt32()).IsEqualTo(1);
        _ = await Assert.That(reviews.GetProperty("myReview").GetProperty("rating").GetDecimal()).IsEqualTo(3m);
        _ = await Assert.That(reviews.GetProperty("myReview").GetProperty("text").GetString()).IsEqualTo("Softer the next day.");

        await ExpectOkAsync(await member.DeleteAsync($"/api/variant/{variant}/review"));
        using var again = await member.DeleteAsync($"/api/variant/{variant}/review");

        _ = await Assert.That((await ReviewsAsync(member, variant)).GetProperty("total").GetInt32()).IsEqualTo(0);
        _ = await Assert.That(again.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Review_ReachesTheSearchIndex()
    {
        var variant = await VariantAsync("IT Quetzal");
        using var member = await SignedInAsync();

        await ExpectOkAsync(await member.PutAsync($"/api/variant/{variant}/review", new { rating = 5, text = "Worth the wait." }));
        await Site.Services.GetRequiredService<IRecipeIndexRebuilder>().WhenCurrentAsync(CancellationToken.None);

        var hit = Site.Services.GetRequiredService<IRecipeSearchService>().Search(new RecipeSearchCriteria { Query = "quetzal" }).Results.Single();
        _ = await Assert.That(hit.ReviewCount).IsEqualTo(1);
        _ = await Assert.That(hit.AverageRating).IsEqualTo(5d);
    }

    [Test]
    [Arguments(0)]
    [Arguments(5.5)]
    [Arguments(3.25)]
    public async Task Review_WithARatingOffTheHalfStarScale_IsRefused(decimal rating)
    {
        var variant = await VariantAsync("IT Marmot");
        using var member = await SignedInAsync();

        using var response = await member.PutAsync($"/api/variant/{variant}/review", new { rating, text = "Hmm." });

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Writes_ToAnUnknownOrUnpublishedVariant_AreNotFound()
    {
        var recipe = await TestContent.RecipeAsync(Site.Services, "IT Ibex");
        var withdrawn = await TestContent.VariantAsync(Site.Services, recipe, "Withdrawn");
        await TestContent.UnpublishAsync(Site.Services, withdrawn);
        using var member = await SignedInAsync();

        using var unknown = await member.PutAsync($"/api/variant/{Guid.NewGuid()}/review", new { rating = 4, text = "Lost." });
        using var unpublished = await member.PostAsync($"/api/variant/{withdrawn}/cooked");
        using var note = await member.PostAsync($"/api/variant/{withdrawn}/note", "Too late.");

        _ = await Assert.That(unknown.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        _ = await Assert.That(unpublished.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        _ = await Assert.That(note.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Writes_SignedOut_AreUnauthorized()
    {
        var variant = await VariantAsync("IT Gibbon");
        using var visitor = new MemberClient(Site);

        using var review = await visitor.PutAsync($"/api/variant/{variant}/review", new { rating = 4, text = "Anonymous." });
        using var note = await visitor.PostAsync($"/api/variant/{variant}/note", "Anonymous.");
        using var cooked = await visitor.PostAsync($"/api/variant/{variant}/cooked");

        _ = await Assert.That(review.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
        _ = await Assert.That(note.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
        _ = await Assert.That(cooked.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Writes_WithoutTheAntiForgeryToken_AreBadRequest()
    {
        var variant = await VariantAsync("IT Meerkat");
        using var member = await SignedInAsync();

        using var review = await member.Http.PutAsJsonAsync($"/api/variant/{variant}/review", new { rating = 4, text = "No token." });
        using var note = await member.Http.PostAsJsonAsync($"/api/variant/{variant}/note", "No token.");
        using var cooked = await member.Http.PostAsync($"/api/variant/{variant}/cooked", null);

        _ = await Assert.That(review.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        _ = await Assert.That(note.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        _ = await Assert.That(cooked.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Deletes_SignedOut_AreUnauthorized()
    {
        var variant = await VariantAsync("IT Manatee");
        using var visitor = new MemberClient(Site);

        using var review = await visitor.DeleteAsync($"/api/variant/{variant}/review");
        using var note = await visitor.DeleteAsync("/api/note/999999");
        using var cooked = await visitor.DeleteAsync($"/api/variant/{variant}/cooked");

        _ = await Assert.That(review.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
        _ = await Assert.That(note.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
        _ = await Assert.That(cooked.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task CookNote_IsAddedAndOnlyItsAuthorCanDeleteIt()
    {
        var variant = await VariantAsync("IT Capybara");
        using var author = await SignedInAsync();
        using var neighbour = await SignedInAsync();

        using var added = await author.PostAsync($"/api/variant/{variant}/note", "  Rest the dough overnight.  ");
        var id = (await added.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        var notes = await (await author.Http.GetAsync($"/api/variant/{variant}/notes")).Content.ReadFromJsonAsync<JsonElement>();
        using var blank = await author.PostAsync($"/api/variant/{variant}/note", "   ");
        using var stolen = await neighbour.DeleteAsync($"/api/note/{id}");

        _ = await Assert.That(notes.GetProperty("notes")[0].GetProperty("text").GetString()).IsEqualTo("Rest the dough overnight.");
        _ = await Assert.That(notes.GetProperty("notes")[0].GetProperty("isMine").GetBoolean()).IsTrue();
        _ = await Assert.That(blank.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        _ = await Assert.That(stolen.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);

        await ExpectOkAsync(await author.DeleteAsync($"/api/note/{id}"));
        var after = await (await author.Http.GetAsync($"/api/variant/{variant}/notes")).Content.ReadFromJsonAsync<JsonElement>();
        _ = await Assert.That(after.GetProperty("total").GetInt32()).IsEqualTo(0);
    }

    [Test]
    public async Task Cooked_CountsEachMemberOnce()
    {
        var variant = await VariantAsync("IT Lemur");
        using var member = await SignedInAsync();
        using var neighbour = await SignedInAsync();

        var first = await CookedAsync(await member.PostAsync($"/api/variant/{variant}/cooked"));
        var twice = await CookedAsync(await member.PostAsync($"/api/variant/{variant}/cooked"));
        var second = await CookedAsync(await neighbour.PostAsync($"/api/variant/{variant}/cooked"));
        var undone = await CookedAsync(await member.DeleteAsync($"/api/variant/{variant}/cooked"));

        _ = await Assert.That(first).IsEqualTo((1, true));
        _ = await Assert.That(twice).IsEqualTo((1, true));
        _ = await Assert.That(second).IsEqualTo((2, true));
        _ = await Assert.That(undone).IsEqualTo((1, false));
    }

    [Test]
    public async Task ContributionWrites_AllowThirtyAMinutePerClient()
    {
        var variant = await VariantAsync("IT Ibis");
        using var member = await SignedInAsync();
        for (var write = 0; write < 30; write++)
        {
            using var allowed = await member.PostAsync($"/api/variant/{variant}/cooked");
            _ = await Assert.That(allowed.StatusCode).IsEqualTo(HttpStatusCode.OK);
        }

        using var limited = await member.PutAsync($"/api/variant/{variant}/review", new { rating = 4, text = "One too many." });

        _ = await Assert.That(limited.StatusCode).IsEqualTo(HttpStatusCode.TooManyRequests);
    }

    private static async Task ExpectOkAsync(HttpResponseMessage response)
    {
        using (response)
        {
            _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        }
    }

    private static async Task<JsonElement> ReviewsAsync(MemberClient member, Guid variant) =>
        await (await member.Http.GetAsync($"/api/variant/{variant}/reviews")).Content.ReadFromJsonAsync<JsonElement>();

    private static async Task<(int Count, bool HasCooked)> CookedAsync(HttpResponseMessage response)
    {
        using (response)
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            return (body.GetProperty("cookedCount").GetInt32(), body.GetProperty("hasCooked").GetBoolean());
        }
    }

    private async Task<Guid> VariantAsync(string recipeName)
    {
        var recipe = await TestContent.RecipeAsync(Site.Services, recipeName);
        return await TestContent.VariantAsync(Site.Services, recipe, "Classic");
    }

    private async Task<MemberClient> SignedInAsync() => (await TestMembers.SignedInAsync(Site, "writer")).Visitor;
}
