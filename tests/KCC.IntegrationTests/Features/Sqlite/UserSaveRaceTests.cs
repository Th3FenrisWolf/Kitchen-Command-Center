using System.Globalization;
using KCC.Contributions;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.Sqlite;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;

namespace KCC.IntegrationTests.Features.Sqlite;

// A backoffice user save reads before it writes, so a commit by another connection in between strands the write on a
// stale snapshot, and Umbraco's retry policy repeats it for about ten minutes.
public class UserSaveRaceTests
{
    // Creating a user and fetching its token takes about a second here.
    private static readonly TimeSpan SaveLimit = TimeSpan.FromSeconds(20);

    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    private UserSaveProbe Probe => Site.Services.GetRequiredService<UserSaveProbe>();

    [Test]
    public async Task TheUserService_TakesTheWriteLock()
    {
        _ = await Assert.That(Site.Services.GetRequiredService<IUserService>() is WriteLockedUserService { Inner: var inner } && inner.GetType().FullName == SqliteComposer.UserServiceTypeName).IsTrue();
    }

    [Test]
    public async Task TheBackOfficeUserStore_TakesTheWriteLock()
    {
        using var scope = Site.Services.CreateScope();
        _ = await Assert.That(scope.ServiceProvider.GetRequiredService<IUserStore<BackOfficeIdentityUser>>()).IsTypeOf<WriteLockedBackOfficeUserStore>();
        _ = await Assert.That(scope.ServiceProvider.GetRequiredService<IBackOfficeUserStore>()).IsTypeOf<WriteLockedBackOfficeUserStore>();
    }

    [Test]
    public async Task CreatingAUserWhileAReviewCommits_Finishes()
    {
        Probe.CommitDuringNextSave(WriteAReview);

        using var client = await Task.Run(() => BackofficeClient.EditorAsync(Site)).WaitAsync(SaveLimit);

        await AssertCommittedAndSignedInAsync(client);
    }

    // Signing in records the date through the user store's update, outside the user service. The store reads the user
    // inside the update's transaction once the cached copy has expired, five minutes after it was last read.
    [Test]
    public async Task RecordingASignInWhileAReviewCommits_Finishes()
    {
        using var scope = Site.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IBackOfficeUserManager>();
        var adminId = (await scope.ServiceProvider.GetRequiredService<IUserService>().GetAsync(Constants.Security.SuperUserKey))!.Id;
        var admin = await users.FindByIdAsync(adminId.ToString(CultureInfo.InvariantCulture))
            ?? throw new InvalidOperationException("The unattended install created no administrator.");
        admin.LastLoginDate = DateTime.UtcNow;
        Site.Services.GetRequiredService<AppCaches>().IsolatedCaches.ClearCache<IUser>();
        Probe.CommitDuringNextSave(WriteAReview);

        var updated = await Task.Run(() => users.UpdateAsync(admin)).WaitAsync(SaveLimit);

        _ = await Assert.That(updated.Succeeded).IsTrue();
        _ = await Assert.That(Probe.Committed is not null).IsTrue();
        await Probe.Committed!.WaitAsync(SaveLimit);
    }

    private Task WriteAReview() =>
        Site.Services.GetRequiredService<IContributionWrites>().UpsertReviewAsync(Guid.NewGuid(), Guid.NewGuid(), 4.5m, "Written while the owner adds a user.");

    private async Task AssertCommittedAndSignedInAsync(BackofficeClient client)
    {
        _ = await Assert.That(Probe.Committed is not null).IsTrue();
        await Probe.Committed!.WaitAsync(SaveLimit);

        var current = await client.GetJsonAsync("/umbraco/management/api/v1/user/current");
        var user = await Site.Services.GetRequiredService<IUserService>().GetAsync(current.GetProperty("id").GetGuid());
        _ = await Assert.That(user?.LastLoginDate).IsNotNull();
    }
}
