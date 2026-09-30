using KCC.IntegrationTests.Config;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Security;

namespace KCC.IntegrationTests.Features.Members;

// Spec §19: a member created unapproved cannot sign in, and approval is that one flag.
public class MemberApprovalTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task UnapprovedMember_IsNotAllowedToSignIn()
    {
        var userName = TestMembers.UniqueUserName("waiting");
        await TestMembers.SignUpAsync(Site.Services, userName);

        var result = await CheckPasswordAsync(userName, TestMembers.Password);

        _ = await Assert.That(result.IsNotAllowed).IsTrue();
        _ = await Assert.That(result.Succeeded).IsFalse();
    }

    [Test]
    public async Task ApprovedMember_SignsIn()
    {
        var userName = TestMembers.UniqueUserName("approved");
        var key = await TestMembers.SignUpAsync(Site.Services, userName);

        await TestMembers.ApproveAsync(Site.Services, key);

        _ = await Assert.That((await CheckPasswordAsync(userName, TestMembers.Password)).Succeeded).IsTrue();
    }

    [Test]
    public async Task FiveWrongPasswords_LockTheMemberOutForFifteenMinutes()
    {
        var userName = TestMembers.UniqueUserName("lockout");
        await TestMembers.ApprovedAsync(Site.Services, userName);

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            _ = await CheckPasswordAsync(userName, "not-the-password");
        }

        var result = await CheckPasswordAsync(userName, TestMembers.Password);

        _ = await Assert.That(result.IsLockedOut).IsTrue();
        using var scope = Site.Services.CreateScope();
        var member = await scope.ServiceProvider.GetRequiredService<IMemberManager>().FindByNameAsync(userName);
        _ = await Assert.That(member!.LockoutEnd!.Value).IsBetween(DateTimeOffset.UtcNow.AddMinutes(14), DateTimeOffset.UtcNow.AddMinutes(16));
    }

    [Test]
    [Arguments("Seven77", false)]
    [Arguments("Eight888", true)]
    public async Task Password_NeedsEightCharacters(string password, bool accepted)
    {
        using var scope = Site.Services.CreateScope();
        var userName = TestMembers.UniqueUserName("length");
        var user = MemberIdentityUser.CreateNew(userName, $"{userName}@example.test", Constants.Security.DefaultMemberTypeAlias, isApproved: false, userName);

        var created = await scope.ServiceProvider.GetRequiredService<IMemberManager>().CreateAsync(user, password);

        _ = await Assert.That(created.Succeeded).IsEqualTo(accepted);
    }

    private async Task<SignInResult> CheckPasswordAsync(string userName, string password)
    {
        using var scope = Site.Services.CreateScope();
        var member = await scope.ServiceProvider.GetRequiredService<IMemberManager>().FindByNameAsync(userName)
            ?? throw new InvalidOperationException($"No member {userName}.");
        return await scope.ServiceProvider.GetRequiredService<SignInManager<MemberIdentityUser>>()
            .CheckPasswordSignInAsync(member, password, lockoutOnFailure: true);
    }
}
