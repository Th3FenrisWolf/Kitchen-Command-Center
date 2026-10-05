using System.Net;
using System.Net.Http.Json;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;

namespace KCC.IntegrationTests.Features.Api;

public class ProfileApiTests
{
    private const string VariantPath = "/recipes/fluffy-buttermilk-pancakes/classic-stack/";

    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task UpdateProfile_SavesTrimmedNames_AndRenamesTheAuthor()
    {
        using var member = await TestMembers.SignedInAsync(Site, "rename");

        var response = await ReadAsync(await member.Visitor.PostAsync("/api/profile", new { firstName = "  Grace ", lastName = " Hopper  " }));

        _ = await Assert.That(response.Success).IsTrue();
        _ = await Assert.That(await Site.Services.GetRequiredService<IAuthorNameProvider>().Resolve(member.Key)).IsEqualTo("Grace Hopper");
    }

    [Test]
    public async Task UpdateProfile_WithoutALastName_IsRefused()
    {
        using var member = await TestMembers.SignedInAsync(Site, "half");

        var response = await ReadAsync(await member.Visitor.PostAsync("/api/profile", new { firstName = "Grace", lastName = " " }));

        _ = await Assert.That(response.Success).IsFalse();
        _ = await Assert.That(response.Errors!.Single()).IsEqualTo(Site.ResourceString("Account.NameRequiredError"));
    }

    [Test]
    public async Task UpdateProfile_SignedOut_IsUnauthorized()
    {
        using var visitor = new MemberClient(Site);

        using var response = await visitor.PostAsync("/api/profile", new { firstName = "Grace", lastName = "Hopper" });

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task UpdateProfile_AfterTheMemberIsDeleted_IsUnauthorized()
    {
        using var member = await TestMembers.SignedInAsync(Site, "vanished");
        await DeleteMemberAsync(member.Key);

        using var response = await member.Visitor.PostAsync("/api/profile", new { firstName = "Grace", lastName = "Hopper" });

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ChangePassword_KeepsTheMemberSignedIn_AndTheNewPasswordWorks()
    {
        using var member = await TestMembers.SignedInAsync(Site, "rotate");

        var response = await ReadAsync(await member.Visitor.PostAsync("/api/profile/password", new { currentPassword = TestMembers.Password, newPassword = "Brand-New-Passw0rd" }));

        _ = await Assert.That(response.Success).IsTrue();
        _ = await Assert.That(await member.Visitor.IsSignedInAsync(VariantPath)).IsTrue();
        using var elsewhere = new MemberClient(Site);
        _ = await Assert.That((await elsewhere.SignInAsync(member.UserName, TestMembers.Password)).Success).IsFalse();
        _ = await Assert.That((await elsewhere.SignInAsync(member.UserName, "Brand-New-Passw0rd")).Success).IsTrue();
    }

    [Test]
    public async Task ChangePassword_WithTheWrongCurrentPassword_Fails()
    {
        using var member = await TestMembers.SignedInAsync(Site, "forgot");

        var response = await ReadAsync(await member.Visitor.PostAsync("/api/profile/password", new { currentPassword = "not-my-password", newPassword = "Brand-New-Passw0rd" }));

        _ = await Assert.That(response.Success).IsFalse();
        _ = await Assert.That(response.Errors!.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task ChangePassword_ToOneTooShort_Fails()
    {
        using var member = await TestMembers.SignedInAsync(Site, "short");

        var response = await ReadAsync(await member.Visitor.PostAsync("/api/profile/password", new { currentPassword = TestMembers.Password, newPassword = "Short7!" }));

        _ = await Assert.That(response.Success).IsFalse();
    }

    [Test]
    public async Task UpdateRamp_SavesTheChoiceOnTheMember_AndMirrorsItInAnHttpOnlyCookie()
    {
        using var member = await TestMembers.SignedInAsync(Site, "dusk");

        var response = await member.Visitor.PostAsync("/api/profile/ramp", new { ramp = "Dark" });
        var cookie = RampCookieOf(response);

        _ = await Assert.That((await ReadAsync(response)).Success).IsTrue();
        _ = await Assert.That(StoredRamp(member.Key)).IsEqualTo("[\"Dark\"]");
        _ = await Assert.That(cookie.Value.ToString()).IsEqualTo("dark");
        _ = await Assert.That(cookie.HttpOnly).IsTrue();
        _ = await Assert.That(cookie.SameSite).IsEqualTo(Microsoft.Net.Http.Headers.SameSiteMode.Lax);
        _ = await Assert.That(cookie.MaxAge).IsEqualTo(TimeSpan.FromDays(365));
    }

    [Test]
    public async Task UpdateRamp_ToDevice_DeletesTheCookie()
    {
        using var member = await TestMembers.SignedInAsync(Site, "noon");
        _ = await ReadAsync(await member.Visitor.PostAsync("/api/profile/ramp", new { ramp = "Dark" }));

        using var response = await member.Visitor.PostAsync("/api/profile/ramp", new { ramp = "Device" });

        _ = await Assert.That(StoredRamp(member.Key)).IsEqualTo("[\"Device\"]");
        _ = await Assert.That(RampCookieOf(response).Expires < DateTimeOffset.UtcNow).IsTrue();
    }

    [Test]
    public async Task UpdateRamp_ToARampTheSiteDoesNotHave_IsRefused()
    {
        using var member = await TestMembers.SignedInAsync(Site, "sepia");

        using var response = await member.Visitor.PostAsync("/api/profile/ramp", new { ramp = "Sepia" });

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        _ = await Assert.That(StoredRamp(member.Key)).IsNull();
    }

    [Test]
    public async Task UpdateRamp_SignedOut_IsUnauthorized()
    {
        using var visitor = new MemberClient(Site);

        using var response = await visitor.PostAsync("/api/profile/ramp", new { ramp = "Dark" });

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    private static async Task<ProfileResult> ReadAsync(HttpResponseMessage response)
    {
        using (response)
        {
            return await response.Content.ReadFromJsonAsync<ProfileResult>() ?? throw new InvalidOperationException("The profile API answered no body.");
        }
    }

    private async Task DeleteMemberAsync(Guid memberKey)
    {
        using var scope = Site.Services.CreateScope();
        var deleted = await scope.ServiceProvider.GetRequiredService<IMemberEditingService>().DeleteAsync(memberKey, Constants.Security.SuperUserKey);
        if (!deleted.Success)
        {
            throw new InvalidOperationException($"Deleting the member failed: {deleted.Status}.");
        }
    }

    private static SetCookieHeaderValue RampCookieOf(HttpResponseMessage response) =>
        SetCookieHeaderValue.ParseList(response.Headers.TryGetValues("Set-Cookie", out var cookies) ? cookies.ToList() : [])
            .Single(cookie => cookie.Name.Equals("kcc-ramp", StringComparison.Ordinal));

    private string? StoredRamp(Guid memberKey) =>
        Site.Services.GetRequiredService<IMemberService>().GetById(memberKey)!.GetValue<string>("ramp");

    private sealed record ProfileResult(bool Success, string[]? Errors);
}
