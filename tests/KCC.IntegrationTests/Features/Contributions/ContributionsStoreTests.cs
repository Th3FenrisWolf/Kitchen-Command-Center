using KCC.Contributions.Data;
using KCC.IntegrationTests.Config;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Persistence.EFCore.Scoping;

namespace KCC.IntegrationTests.Features.Contributions;

public class ContributionsStoreTests
{
    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    private IEFCoreScopeProvider<ContributionsDbContext> Scopes =>
        Site.Services.GetRequiredService<IEFCoreScopeProvider<ContributionsDbContext>>();

    private IKeyValueService KeyValues => Site.Services.GetRequiredService<IKeyValueService>();

    [Test]
    public async Task FirstBoot_CreatesTheTablesAndTheLockRow()
    {
        var tables = await ScalarAsync(
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name IN ('kccReview', 'kccCookNote', 'kccCookedMark')");
        var lockRows = await ScalarAsync($"SELECT COUNT(*) FROM umbracoLock WHERE id = {ContributionLocks.Contributions}");

        _ = await Assert.That(tables).IsEqualTo(3L);
        _ = await Assert.That(lockRows).IsEqualTo(1L);
    }

    [Test]
    public async Task SecondReviewByOneMember_IsRefusedByTheUniqueIndex()
    {
        var variantKey = Guid.NewGuid();
        var memberKey = Guid.NewGuid();
        await WriteAsync(db => db.Reviews.Add(NewReview(variantKey, memberKey)));

        _ = await Assert.That(async () => await WriteAsync(db => db.Reviews.Add(NewReview(variantKey, memberKey))))
            .Throws<DbUpdateException>();
    }

    [Test]
    public async Task SecondCookedMarkByOneMember_IsRefusedByTheUniqueIndex()
    {
        var variantKey = Guid.NewGuid();
        var memberKey = Guid.NewGuid();
        await WriteAsync(db => db.CookedMarks.Add(NewCookedMark(variantKey, memberKey)));

        _ = await Assert.That(async () => await WriteAsync(db => db.CookedMarks.Add(NewCookedMark(variantKey, memberKey))))
            .Throws<DbUpdateException>();
    }

    [Test]
    public async Task EfCoreWrite_RollsBackWithTheUmbracoScopeAroundIt()
    {
        var key = $"kcc-proof-{Guid.NewGuid():N}";
        var variantKey = Guid.NewGuid();

        using (Site.Services.GetRequiredService<ICoreScopeProvider>().CreateCoreScope())
        {
            KeyValues.SetValue(key, "written");
            await WriteAsync(db => db.Reviews.Add(NewReview(variantKey, Guid.NewGuid())));
        }

        _ = await Assert.That(KeyValues.GetValue(key)).IsNull();
        _ = await Assert.That(await CountReviewsAsync(variantKey)).IsEqualTo(0);
    }

    [Test]
    public async Task EfCoreWriteLock_HoldsBackAConcurrentUmbracoWrite()
    {
        var key = $"kcc-proof-{Guid.NewGuid():N}";
        var holding = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // Each writer needs its own ambient scope, so neither may inherit this test's execution context.
        Task writer;
        using (ExecutionContext.SuppressFlow())
        {
            writer = Task.Run(async () =>
            {
                using var scope = Scopes.CreateScope();
                scope.WriteLock(ContributionLocks.Contributions);
                await scope.ExecuteWithContextAsync<Task>(async db =>
                {
                    db.Reviews.Add(NewReview(Guid.NewGuid(), Guid.NewGuid()));
                    await db.SaveChangesAsync();
                });
                holding.SetResult();
                await release.Task;
                scope.Complete();
            });
        }

        await holding.Task;

        Task<DateTime> umbracoWrite;
        using (ExecutionContext.SuppressFlow())
        {
            umbracoWrite = Task.Run(() =>
            {
                KeyValues.SetValue(key, "after");
                return DateTime.UtcNow;
            });
        }

        await Task.Delay(TimeSpan.FromSeconds(1));
        var heldBack = !umbracoWrite.IsCompleted;
        var releasedAt = DateTime.UtcNow;
        release.SetResult();
        await writer;
        var writtenAt = await umbracoWrite;

        _ = await Assert.That(heldBack).IsTrue();
        _ = await Assert.That(writtenAt).IsGreaterThanOrEqualTo(releasedAt);
        _ = await Assert.That(KeyValues.GetValue(key)).IsEqualTo("after");
    }

    private static Review NewReview(Guid variantKey, Guid memberKey) => new()
    {
        VariantKey = variantKey,
        MemberKey = memberKey,
        Rating = 4m,
        Created = DateTime.UtcNow,
        Modified = DateTime.UtcNow,
    };

    private static CookedMark NewCookedMark(Guid variantKey, Guid memberKey) => new()
    {
        VariantKey = variantKey,
        MemberKey = memberKey,
        Created = DateTime.UtcNow,
    };

    private async Task WriteAsync(Action<ContributionsDbContext> change)
    {
        using var scope = Scopes.CreateScope();
        scope.WriteLock(ContributionLocks.Contributions);
        await scope.ExecuteWithContextAsync<Task>(async db =>
        {
            change(db);
            await db.SaveChangesAsync();
        });
        scope.Complete();
    }

    private async Task<int> CountReviewsAsync(Guid variantKey)
    {
        using var scope = Scopes.CreateScope();
        var count = await scope.ExecuteWithContextAsync(db => db.Reviews.CountAsync(review => review.VariantKey == variantKey));
        scope.Complete();
        return count;
    }

    private async Task<long> ScalarAsync(string sql)
    {
        await using var connection = new SqliteConnection($"Data Source={Site.DatabasePath};Pooling=False");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (long)(await command.ExecuteScalarAsync())!;
    }
}
