using KCC.IntegrationTests.Config;
using KCC.Web.Features.Dictionary;
using Microsoft.Extensions.DependencyInjection;

namespace KCC.IntegrationTests.Features.Baseline;

// A new account waits for the owner's approval, and members sign in by username.
public class AccountStringsTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    [Arguments("Login.NotAllowedError", "Your account is waiting for approval. You can sign in once the owner approves it.")]
    [Arguments("RegistrationComplete.Body", "Your account has been created and is waiting for approval. You can sign in once the owner approves it.")]
    [Arguments("Login.InvalidCredentialsError", "That username or password is incorrect.")]
    public async Task BaselineStrings_DescribeApprovalAndUsernames(string key, string expected)
    {
        using var scope = Site.Services.CreateScope();

        _ = await Assert.That(scope.ServiceProvider.GetRequiredService<IResourceStringProvider>().GetOrDefault(key)).IsEqualTo(expected);
    }
}
