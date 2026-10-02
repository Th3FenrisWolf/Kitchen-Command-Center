using System.Net;
using System.Net.Http.Json;
using KCC.IntegrationTests.Config;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Services;

namespace KCC.IntegrationTests.Features.Api;

public class AccountApiTests
{
    // A seeded variant: the page tells a signed-in visitor apart from a signed-out one.
    private const string VariantPath = "/recipes/fluffy-buttermilk-pancakes/classic-stack/";

    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task SignUp_CreatesAnUnapprovedMember_AndPointsAtRegistrationComplete()
    {
        using var visitor = new MemberClient(Site);
        var userName = TestMembers.UniqueUserName("joiner");

        var result = await SignUpAsync(visitor, userName);

        _ = await Assert.That(result.Success).IsTrue();
        _ = await Assert.That(result.RedirectUrl).IsEqualTo("/account/registration-complete/");
        var member = Site.Services.GetRequiredService<IMemberService>().GetByUsername(userName);
        _ = await Assert.That(member!.IsApproved).IsFalse();
        _ = await Assert.That(member.Email).IsEqualTo($"{userName}@example.test");
    }

    [Test]
    public async Task SignUp_ATakenUserName_Fails()
    {
        using var visitor = new MemberClient(Site);
        var userName = TestMembers.UniqueUserName("twice");
        _ = await SignUpAsync(visitor, userName);

        var again = await SignUpAsync(visitor, userName, $"other-{userName}@example.test");

        _ = await Assert.That(again.Success).IsFalse();
        _ = await Assert.That(again.Errors!.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task SignIn_Unapproved_SaysTheAccountIsWaitingForApproval()
    {
        using var visitor = new MemberClient(Site);
        var userName = TestMembers.UniqueUserName("waiting");
        _ = await SignUpAsync(visitor, userName);

        var result = await visitor.SignInAsync(userName, TestMembers.Password);

        _ = await Assert.That(result.Success).IsFalse();
        _ = await Assert.That(result.Errors!.Single()).IsEqualTo(Site.ResourceString("Login.NotAllowedError"));
        _ = await Assert.That(await visitor.IsSignedInAsync(VariantPath)).IsFalse();
    }

    [Test]
    public async Task SignIn_Approved_SignsInAndReturnsToALocalUrl()
    {
        using var visitor = new MemberClient(Site);
        var userName = TestMembers.UniqueUserName("member");
        await TestMembers.ApprovedAsync(Site.Services, userName);

        var result = await visitor.SignInAsync(userName, TestMembers.Password, "/recipes/");

        _ = await Assert.That(result.Success).IsTrue();
        _ = await Assert.That(result.RedirectUrl).IsEqualTo("/recipes/");
        _ = await Assert.That(await visitor.IsSignedInAsync(VariantPath)).IsTrue();
    }

    [Test]
    public async Task SignIn_ToAForeignUrl_ReturnsHomeInstead()
    {
        using var visitor = new MemberClient(Site);
        var userName = TestMembers.UniqueUserName("member");
        await TestMembers.ApprovedAsync(Site.Services, userName);

        var result = await visitor.SignInAsync(userName, TestMembers.Password, "https://evil.example/recipes/");

        _ = await Assert.That(result.RedirectUrl).IsEqualTo("/");
    }

    [Test]
    public async Task SignIn_AfterFiveWrongPasswords_IsLockedOut()
    {
        using var visitor = new MemberClient(Site);
        var userName = TestMembers.UniqueUserName("guesser");
        await TestMembers.ApprovedAsync(Site.Services, userName);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            _ = await visitor.SignInAsync(userName, "not-the-password");
        }

        var result = await visitor.SignInAsync(userName, TestMembers.Password);

        _ = await Assert.That(result.Errors!.Single()).IsEqualTo(Site.ResourceString("Login.LockedOutError"));
        _ = await Assert.That(await visitor.IsSignedInAsync(VariantPath)).IsFalse();
    }

    [Test]
    public async Task SignIn_WithAWrongPassword_SaysTheCredentialsAreWrong()
    {
        using var visitor = new MemberClient(Site);
        var userName = TestMembers.UniqueUserName("typo");
        await TestMembers.ApprovedAsync(Site.Services, userName);

        var result = await visitor.SignInAsync(userName, "not-the-password");

        _ = await Assert.That(result.Errors!.Single()).IsEqualTo(Site.ResourceString("Login.InvalidCredentialsError"));
    }

    [Test]
    public async Task APost_WithoutTheAntiforgeryToken_IsRejected()
    {
        using var visitor = new MemberClient(Site);

        using var response = await visitor.Http.PostAsJsonAsync("/api/account/login", new { userName = "anyone", password = "anything" });

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task SignOut_OverGet_DoesNothing()
    {
        using var member = await TestMembers.SignedInAsync(Site, "stays");
        var visitor = member.Visitor;

        using var response = await visitor.Http.GetAsync("/account/logout");

        _ = await Assert.That((int)response.StatusCode).IsGreaterThanOrEqualTo(400);
        _ = await Assert.That(await visitor.IsSignedInAsync(VariantPath)).IsTrue();
    }

    [Test]
    public async Task SignOut_OverPost_EndsTheSessionAndGoesHome()
    {
        using var member = await TestMembers.SignedInAsync(Site, "leaver");
        var visitor = member.Visitor;

        using var response = await visitor.SignOutAsync();

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
        _ = await Assert.That(response.Headers.Location!.OriginalString).IsEqualTo("/");
        _ = await Assert.That(await visitor.IsSignedInAsync(VariantPath)).IsFalse();
    }

    [Test]
    public async Task SignOut_WithoutTheAntiforgeryToken_IsRejectedAndLeavesTheMemberSignedIn()
    {
        using var member = await TestMembers.SignedInAsync(Site, "notoken");
        var visitor = member.Visitor;

        using var response = await visitor.Http.PostAsync("/account/logout", new FormUrlEncodedContent([]));

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        _ = await Assert.That(await visitor.IsSignedInAsync(VariantPath)).IsTrue();
    }

    [Test]
    public async Task SignOut_ToAForeignUrl_ReturnsHomeInstead()
    {
        using var member = await TestMembers.SignedInAsync(Site, "foreign");
        var visitor = member.Visitor;

        using var response = await visitor.PostAsync("/account/logout?returnUrl=https%3A%2F%2Fevil.example%2F");

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
        _ = await Assert.That(response.Headers.Location!.OriginalString).IsEqualTo("/");
    }

    [Test]
    public async Task AccountEndpoints_AllowTenAMinutePerClient()
    {
        using var visitor = new MemberClient(Site);
        for (var attempt = 0; attempt < 10; attempt++)
        {
            using var allowed = await visitor.PostAsync("/api/account/login", new { userName = "nobody-at-all", password = "wrong-password" });
            _ = await Assert.That(allowed.StatusCode).IsEqualTo(HttpStatusCode.OK);
        }

        using var limited = await visitor.PostAsync("/api/account/register", new { userName = "nobody-at-all", email = "nobody@example.test", password = "wrong-password" });
        using var neighbour = new MemberClient(Site);
        using var elsewhere = await neighbour.PostAsync("/api/account/login", new { userName = "nobody-at-all", password = "wrong-password" });

        _ = await Assert.That(limited.StatusCode).IsEqualTo(HttpStatusCode.TooManyRequests);
        _ = await Assert.That(elsewhere.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    private static async Task<AuthResult> SignUpAsync(MemberClient visitor, string userName, string? email = null)
    {
        using var response = await visitor.PostAsync("/api/account/register", new { userName, email = email ?? $"{userName}@example.test", password = TestMembers.Password });
        return await response.Content.ReadFromJsonAsync<AuthResult>() ?? throw new InvalidOperationException("Sign-up answered no body.");
    }
}
