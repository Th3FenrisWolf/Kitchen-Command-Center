using KCC.IntegrationTests.Config;
using KCC.Web.Features.Providers;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;

namespace KCC.IntegrationTests.Features.Providers;

public class AuthorNameProviderTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    private IAuthorNameProvider Authors => Site.Services.GetRequiredService<IAuthorNameProvider>();

    [Test]
    public async Task RenamingAMember_ReplacesTheCachedName()
    {
        var members = Site.Services.GetRequiredService<IMemberService>();
        var member = members.CreateMemberWithIdentity(
            $"it-{Guid.NewGuid():N}",
            $"{Guid.NewGuid():N}@example.test",
            "Integration Author",
            Constants.Security.DefaultMemberTypeAlias,
            isApproved: true);
        member.SetValue("firstName", "Ada");
        member.SetValue("lastName", "Lovelace");
        _ = members.Save(member);

        var before = await Authors.Resolve(member.Key);
        member.SetValue("firstName", "Grace");
        member.SetValue("lastName", "Hopper");
        _ = members.Save(member);
        var after = await Authors.Resolve(member.Key);

        _ = await Assert.That(before).IsEqualTo("Ada Lovelace");
        _ = await Assert.That(after).IsEqualTo("Grace Hopper");
    }

    [Test]
    public async Task UnknownMember_HasNoName()
    {
        _ = await Assert.That(await Authors.Resolve(Guid.NewGuid())).IsNull();
    }
}
