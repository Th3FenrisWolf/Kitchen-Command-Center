using KCC.IntegrationTests.Config;
using KCC.Web.Features.Members;

namespace KCC.IntegrationTests.Features.Members;

/// <summary>
/// Resolves <see cref="IAuthorNameResolver"/> from the shared Kentico DI host and exercises it
/// against the real member <c>IInfoProvider</c>. Requires a Kentico database (connection string
/// via user secrets locally, or appsettings.CI.json when ASPNETCORE_ENVIRONMENT=CI); no seeded
/// data is needed because the test queries for a member guid that does not exist.
/// </summary>
[TestsDI]
public class AuthorNameResolverTests(IAuthorNameResolver resolver)
{
    [Test]
    public async Task Resolve_ReturnsNull_ForUnknownMember()
    {
        var displayName = await resolver.Resolve(Guid.NewGuid());

        _ = await Assert.That(displayName).IsNull();
    }
}
