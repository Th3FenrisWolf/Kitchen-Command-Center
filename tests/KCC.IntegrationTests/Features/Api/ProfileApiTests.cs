using System.Net;
using System.Net.Http.Json;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.Dictionary;
using KCC.Web.Features.Providers;
using Microsoft.Extensions.DependencyInjection;
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
        using var member = await SignedInAsync("rename");

        var response = await ReadAsync(await member.Visitor.PostAsync("/api/profile", new { firstName = "  Grace ", lastName = " Hopper  " }));

        _ = await Assert.That(response.Success).IsTrue();
        _ = await Assert.That(await Site.Services.GetRequiredService<IAuthorNameProvider>().Resolve(member.Key)).IsEqualTo("Grace Hopper");
    }

    [Test]
    public async Task UpdateProfile_WithoutALastName_IsRefused()
    {
        using var member = await SignedInAsync("half");

        var response = await ReadAsync(await member.Visitor.PostAsync("/api/profile", new { firstName = "Grace", lastName = " " }));

        _ = await Assert.That(response.Success).IsFalse();
        _ = await Assert.That(response.Errors!.Single()).IsEqualTo(String("Account.NameRequiredError"));
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
        using var member = await SignedInAsync("vanished");
        await DeleteMemberAsync(member.Key);

        using var response = await member.Visitor.PostAsync("/api/profile", new { firstName = "Grace", lastName = "Hopper" });

        _ = await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ChangePassword_KeepsTheMemberSignedIn_AndTheNewPasswordWorks()
    {
        using var member = await SignedInAsync("rotate");

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
        using var member = await SignedInAsync("forgot");

        var response = await ReadAsync(await member.Visitor.PostAsync("/api/profile/password", new { currentPassword = "not-my-password", newPassword = "Brand-New-Passw0rd" }));

        _ = await Assert.That(response.Success).IsFalse();
        _ = await Assert.That(response.Errors!.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task ChangePassword_ToOneTooShort_Fails()
    {
        using var member = await SignedInAsync("short");

        var response = await ReadAsync(await member.Visitor.PostAsync("/api/profile/password", new { currentPassword = TestMembers.Password, newPassword = "Short7!" }));

        _ = await Assert.That(response.Success).IsFalse();
    }

    private static async Task<ProfileResult> ReadAsync(HttpResponseMessage response)
    {
        using (response)
        {
            return await response.Content.ReadFromJsonAsync<ProfileResult>() ?? throw new InvalidOperationException("The profile API answered no body.");
        }
    }

    private async Task<SignedInMember> SignedInAsync(string prefix)
    {
        var userName = TestMembers.UniqueUserName(prefix);
        var key = await TestMembers.ApprovedAsync(Site.Services, userName);
        var visitor = new MemberClient(Site);
        _ = await visitor.SignInAsync(userName, TestMembers.Password);
        return new SignedInMember(visitor, key, userName);
    }

    private string String(string key)
    {
        using var scope = Site.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IResourceStringProvider>().GetOrDefault(key);
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

    private sealed record ProfileResult(bool Success, string[]? Errors);

    private sealed record SignedInMember(MemberClient Visitor, Guid Key, string UserName) : IDisposable
    {
        public void Dispose() => Visitor.Dispose();
    }
}
