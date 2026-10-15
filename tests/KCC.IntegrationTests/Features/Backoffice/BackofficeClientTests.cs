using KCC.IntegrationTests.Config;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Services;

namespace KCC.IntegrationTests.Features.Backoffice;

// A user save in the middle of the suite can meet a commit from the site's background work, so the session creates its
// API users once, before any test.
public class BackofficeClientTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    [Test]
    public async Task SigningIn_CreatesNoUser()
    {
        var before = await UserCountAsync();

        using (await BackofficeClient.AdministratorAsync(Site))
        using (await BackofficeClient.EditorAsync(Site))
        using (await BackofficeClient.TranslatorAsync(Site))
        {
        }

        _ = await Assert.That(await UserCountAsync()).IsEqualTo(before);
    }

    private async Task<long> UserCountAsync()
    {
        using var scope = Site.Services.CreateScope();
        var users = await scope.ServiceProvider.GetRequiredService<IUserService>().GetAllAsync(Constants.Security.SuperUserKey, 0, 1);
        return users.Result!.Total;
    }
}
