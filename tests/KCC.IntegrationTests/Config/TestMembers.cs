using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;

namespace KCC.IntegrationTests.Config;

// Members that can sign in, created the way the sign-up endpoint creates them.
public static class TestMembers
{
    public const string Password = "Quokka-Passw0rd";

    public static string UniqueUserName(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..(prefix.Length + 9)];

    public static async Task<Guid> SignUpAsync(IServiceProvider services, string userName, string password = Password)
    {
        using var scope = services.CreateScope();
        var members = scope.ServiceProvider.GetRequiredService<IMemberManager>();
        var user = MemberIdentityUser.CreateNew(userName, $"{userName}@example.test", Constants.Security.DefaultMemberTypeAlias, isApproved: false, userName);
        var created = await members.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            throw new InvalidOperationException($"Signing up {userName} failed: {string.Join(" ", created.Errors.Select(error => error.Description))}");
        }

        return user.Key;
    }

    public static async Task<Guid> ApprovedAsync(IServiceProvider services, string userName, string password = Password)
    {
        var key = await SignUpAsync(services, userName, password);
        await ApproveAsync(services, key);
        return key;
    }

    // The backoffice's Approved toggle saves the member through the editing service, so approval does the same.
    public static async Task ApproveAsync(IServiceProvider services, Guid memberKey)
    {
        using var scope = services.CreateScope();
        var scoped = scope.ServiceProvider;
        var member = scoped.GetRequiredService<IMemberService>().GetById(memberKey) ?? throw new InvalidOperationException($"No member {memberKey}.");
        var superUser = await scoped.GetRequiredService<IUserService>().GetAsync(Constants.Security.SuperUserKey)
            ?? throw new InvalidOperationException("The super user is missing.");
        var updated = await scoped.GetRequiredService<IMemberEditingService>().UpdateAsync(
            memberKey,
            new MemberUpdateModel
            {
                Email = member.Email,
                Username = member.Username,
                IsApproved = true,
                Variants = [new VariantModel { Name = member.Name ?? member.Username }],
            },
            superUser);
        if (!updated.Success)
        {
            throw new InvalidOperationException($"Approving {member.Username} failed: {updated.Status.MemberEditingOperationStatus}.");
        }
    }
}
