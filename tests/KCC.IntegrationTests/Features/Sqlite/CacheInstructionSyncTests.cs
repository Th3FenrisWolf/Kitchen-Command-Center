using KCC.Contributions;
using KCC.IntegrationTests.Config;
using KCC.Web.Features.Sqlite;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Factories;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure.Services;

namespace KCC.IntegrationTests.Features.Sqlite;

public class CacheInstructionSyncTests
{
    private const string AnotherServer = "kcc-integration-tests/another-server";

    // A sync takes milliseconds. One stranded on a stale snapshot retries for about ten minutes.
    private static readonly TimeSpan SyncLimit = TimeSpan.FromSeconds(20);

    [ClassDataSource<UmbracoSite>(Shared = SharedType.PerTestSession)]
    public UmbracoSite Site { get; init; } = null!;

    private ICacheInstructionService Service => Site.Services.GetRequiredService<ICacheInstructionService>();

    private string SiteIdentity => Site.Services.GetRequiredService<IMachineInfoFactory>().GetLocalIdentity();

    [Test]
    public async Task TheCacheInstructionService_TakesTheWriteLock()
    {
        _ = await Assert.That(Service is WriteLockedCacheInstructionService { Inner: CacheInstructionService }).IsTrue();
    }

    [Test]
    public async Task ASync_FinishesWhenAReviewCommitsPartWayThrough()
    {
        var writes = Site.Services.GetRequiredService<IContributionWrites>();
        var (variant, reviewer) = (Guid.NewGuid(), Guid.NewGuid());
        var refresher = new CommittingRefresher(() => writes.UpsertReviewAsync(variant, reviewer, 4.5m, "Committed part-way through a sync."));

        // As another server the sync below runs every pending instruction, and its refreshers know only this test's.
        await CatchUpAsync();

        // Sent as the site, the instruction is one the site's own sync skips. That sync also runs every five seconds
        // once the site is a minute old, and records the instruction as synced if it gets there first.
        ProcessInstructionsResult result;
        var attempts = 0;
        do
        {
            Service.DeliverInstructions([new RefreshInstruction { RefresherId = refresher.RefresherUniqueId, RefreshType = RefreshMethodType.RefreshAll }], SiteIdentity);
            result = await SyncAsync(new CacheRefresherCollection(() => [refresher]), AnotherServer);
        }
        while (result.NumberOfInstructionsProcessed == 0 && ++attempts < 3);

        _ = await Assert.That(result.NumberOfInstructionsProcessed).IsEqualTo(1);
        await refresher.Committed!.WaitAsync(SyncLimit);

        await CatchUpAsync();
        var synced = await Site.Services.GetRequiredService<ILastSyncedManager>().GetLastSyncedExternalAsync();
        _ = await Assert.That(synced).IsEqualTo(Service.GetMaxInstructionId());
    }

    [Test]
    public async Task AnIdleSync_FinishesWhileAnotherConnectionHoldsTheWriteLock()
    {
        using var writer = new SqliteConnection($"Data Source={Site.DatabasePath};Pooling=False");
        writer.Open();

        // A transaction that writes must first become the one writer, which this connection is until it rolls back,
        // so a sync that finishes meanwhile took no write lock and wrote nothing.
        using var hold = await HoldTheWriterWithNothingPendingAsync(writer);
        var result = await SyncAsync(Site.Services.GetRequiredService<CacheRefresherCollection>(), SiteIdentity);

        _ = await Assert.That(result.LastId).IsEqualTo(0);
    }

    // Nothing can be committed while the writer is held, so an instruction that landed after the catch-up, such as a
    // batch a request sends once it has ended, is caught up after letting go.
    private async Task<SqliteTransaction> HoldTheWriterWithNothingPendingAsync(SqliteConnection writer)
    {
        while (true)
        {
            await CatchUpAsync();
            var hold = writer.BeginTransaction(deferred: false);
            if (Service.GetMaxInstructionId() <= await Site.Services.GetRequiredService<ILastSyncedManager>().GetLastSyncedExternalAsync())
            {
                return hold;
            }

            hold.Dispose();
        }
    }

    // The site's own sync, which takes a hundred instructions at a time.
    private async Task CatchUpAsync()
    {
        var refreshers = Site.Services.GetRequiredService<CacheRefresherCollection>();
        while ((await SyncAsync(refreshers, SiteIdentity)).LastId != 0)
        {
        }
    }

    private Task<ProcessInstructionsResult> SyncAsync(CacheRefresherCollection refreshers, string localIdentity) =>
        Task.Run(() => Service.ProcessAllInstructions(refreshers, CancellationToken.None, localIdentity)).WaitAsync(SyncLimit);

    // The sync runs this between reading its pending instructions and recording the last one it ran.
    private sealed class CommittingRefresher(Func<Task> commit) : ICacheRefresher
    {
        public Guid RefresherUniqueId { get; } = Guid.NewGuid();

        public string Name => nameof(CommittingRefresher);

        public Task? Committed { get; private set; }

        public void RefreshAll() => Committed = OtherConnection.Commit(commit);

        public void Refresh(int id) => throw new NotSupportedException();

        public void Refresh(Guid id) => throw new NotSupportedException();

        public void Remove(int id) => throw new NotSupportedException();
    }
}
