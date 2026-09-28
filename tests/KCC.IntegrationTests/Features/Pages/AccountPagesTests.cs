using System.Net;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.Dictionary;
using Microsoft.Extensions.DependencyInjection;

namespace KCC.IntegrationTests.Features.Pages;

public class AccountPagesTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    [Arguments("/account/", "%2Faccount%2F")]
    [Arguments("/account/settings/", "%2Faccount%2Fsettings%2F")]
    public async Task SignedOut_MemberPages_SendTheVisitorToSignIn(string path, string returnUrl)
    {
        using var visitor = new MemberClient(Site);

        using var response = await visitor.Http.GetAsync(path);

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
        _ = await Assert.That(response.Headers.Location!.OriginalString).IsEqualTo($"/account/login/?returnUrl={returnUrl}");
    }

    [Test]
    public async Task AccountPage_ListsTheMembersPublishedAndPendingWork()
    {
        var userName = TestMembers.UniqueUserName("cook");
        var member = await TestMembers.ApprovedAsync(Site.Services, userName);
        TestContent.RenameAuthor(Site.Services, member, "Grace", "Hopper");
        var otherAuthor = await TestContent.AuthorAsync(Site.Services, TestMembers.UniqueUserName("other"), "Mary", "Somerville");
        var foreign = await TestContent.RecipeAsync(Site.Services, "IT Okapi", TestContent.Author(otherAuthor));
        await TestContent.VariantAsync(Site.Services, foreign, "Mary's Way", TestContent.Author(otherAuthor));
        await TestContent.VariantAsync(Site.Services, foreign, "Grace's Way", TestContent.Author(member));
        await TestContent.DraftVariantAsync(Site.Services, foreign, "Grace's Draft", member);
        var pending = await TestContent.DraftRecipeAsync(Site.Services, "IT Tapir", member);
        await TestContent.DraftVariantAsync(Site.Services, pending, "First Try", member);
        using var visitor = new MemberClient(Site);
        _ = await visitor.SignInAsync(userName, TestMembers.Password);

        var page = await RenderedPage.GetAsync(visitor.Http, "/account/");

        _ = await Assert.That(page.Attribute("display-name")).IsEqualTo("Grace Hopper");
        _ = await Assert.That(page.Attribute("settings-url")).IsEqualTo("/account/settings/");
        _ = await Assert.That(page.Attribute("logout-url")).IsEqualTo("/account/logout");
        var groups = page.Prop("recipe-groups").EnumerateArray().ToList();
        _ = await Assert.That(string.Join(" | ", groups.Select(Describe))).IsEqualTo(
            "IT Okapi (published, not started): Grace's Draft (pending), Grace's Way (published) | IT Tapir (pending, started): First Try (pending)");
    }

    [Test]
    public async Task LoginPage_WhenSignedIn_ReturnsToALocalUrlOnly()
    {
        var userName = TestMembers.UniqueUserName("back");
        await TestMembers.ApprovedAsync(Site.Services, userName);
        using var visitor = new MemberClient(Site);
        _ = await visitor.SignInAsync(userName, TestMembers.Password);

        using var local = await visitor.Http.GetAsync("/account/login/?returnUrl=%2Frecipes%2F");
        using var foreign = await visitor.Http.GetAsync("/account/login/?returnUrl=https%3A%2F%2Fevil.example%2F");

        _ = await Assert.That(local.Headers.Location!.OriginalString).IsEqualTo("/recipes/");
        _ = await Assert.That(foreign.Headers.Location!.OriginalString).IsEqualTo("/");
    }

    [Test]
    public async Task LoginPage_PassesOnlyALocalReturnUrlToTheForm()
    {
        using var visitor = new MemberClient(Site);

        var local = await RenderedPage.GetAsync(visitor.Http, "/account/login/?returnUrl=%2Faccount%2F");
        var foreign = await RenderedPage.GetAsync(visitor.Http, "/account/login/?returnUrl=https%3A%2F%2Fevil.example%2F");

        _ = await Assert.That(local.Attribute("return-url")).IsEqualTo("/account/");
        // Razor drops an attribute whose value is null.
        _ = await Assert.That(foreign.Attribute("return-url")).IsNull();
    }

    [Test]
    public async Task SettingsPage_ShowsTheMembersNamesAndEmail()
    {
        var userName = TestMembers.UniqueUserName("names");
        var member = await TestMembers.ApprovedAsync(Site.Services, userName);
        TestContent.RenameAuthor(Site.Services, member, "Katherine", "Johnson");
        using var visitor = new MemberClient(Site);
        _ = await visitor.SignInAsync(userName, TestMembers.Password);

        var page = await RenderedPage.GetAsync(visitor.Http, "/account/settings/");

        _ = await Assert.That(page.Attribute("first-name")).IsEqualTo("Katherine");
        _ = await Assert.That(page.Attribute("last-name")).IsEqualTo("Johnson");
        _ = await Assert.That(page.Attribute("email")).IsEqualTo($"{userName}@example.test");
        _ = await Assert.That(page.Attribute("back-url")).IsEqualTo("/account/");
        _ = await Assert.That(page.Attribute("logout-url")).IsEqualTo("/account/logout");
    }

    [Test]
    public async Task RegistrationCompletePage_SaysTheAccountWaitsAndLinksToSignIn()
    {
        using var visitor = new MemberClient(Site);

        var page = await RenderedPage.GetAsync(visitor.Http, "/account/registration-complete/");

        _ = await Assert.That(page.Status).IsEqualTo(HttpStatusCode.OK);
        _ = await Assert.That(page.Body).Contains(WebUtility.HtmlEncode(String("RegistrationComplete.Body")));
        _ = await Assert.That(page.Body).Contains("href=\"/account/login/\"");
    }

    private static string Describe(System.Text.Json.JsonElement group)
    {
        var variants = group.GetProperty("variants").EnumerateArray()
            .Select(variant => $"{variant.GetProperty("name").GetString()} ({(variant.GetProperty("isPending").GetBoolean() ? "pending" : "published")})");
        var state = group.GetProperty("isPending").GetBoolean() ? "pending" : "published";
        var started = group.GetProperty("startedByYou").GetBoolean() ? "started" : "not started";
        return $"{group.GetProperty("recipeName").GetString()} ({state}, {started}): {string.Join(", ", variants)}";
    }

    private string String(string key)
    {
        using var scope = Site.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IResourceStringProvider>().GetOrDefault(key);
    }
}
