using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;

namespace KCC.Web.Features.Sqlite;

// Umbraco's cache-instruction sync reads the pending instructions, runs them and then records the last one it ran,
// all in one transaction. On SQLite a transaction that has read cannot become the writer once another connection has
// committed, so a review landing mid-sync strands that record on a stale snapshot, Umbraco's retry policy repeats it
// for about ten minutes, and this server's cache updates pause until it gives up. Taking the write lock first makes
// the sync the writer before it reads, and the inner service's scope joins this one.
public sealed class WriteLockedCacheInstructionService(
    ICacheInstructionService inner,
    ILastSyncedManager lastSynced,
    ICoreScopeProvider scopeProvider) : ICacheInstructionService
{
    internal ICacheInstructionService Inner => inner;

    public bool IsColdBootRequired(int lastId) => inner.IsColdBootRequired(lastId);

    public bool IsInstructionCountOverLimit(int lastId, int limit, out int count) =>
        inner.IsInstructionCountOverLimit(lastId, limit, out count);

    public int GetMaxInstructionId() => inner.GetMaxInstructionId();

    public void DeliverInstructions(IEnumerable<RefreshInstruction> instructions, string localIdentity) =>
        inner.DeliverInstructions(instructions, localIdentity);

    public void DeliverInstructionsInBatches(IEnumerable<RefreshInstruction> instructions, string localIdentity) =>
        inner.DeliverInstructionsInBatches(instructions, localIdentity);

    [Obsolete("Use ProcessAllInstructions.")]
    public ProcessInstructionsResult ProcessInstructions(
        CacheRefresherCollection cacheRefreshers,
        CancellationToken cancellationToken,
        string localIdentity,
        int lastId) =>
        InWriteLockWhenPending(lastId, () => inner.ProcessInstructions(cacheRefreshers, cancellationToken, localIdentity, lastId));

    public ProcessInstructionsResult ProcessAllInstructions(
        CacheRefresherCollection cacheRefreshers,
        CancellationToken cancellationToken,
        string localIdentity) =>
        InWriteLockWhenPending(
            lastSynced.GetLastSyncedExternalAsync().GetAwaiter().GetResult() ?? 0,
            () => inner.ProcessAllInstructions(cacheRefreshers, cancellationToken, localIdentity));

    public ProcessInstructionsResult ProcessInternalInstructions(
        CacheRefresherCollection cacheRefreshers,
        CancellationToken cancellationToken,
        string localIdentity) =>
        InWriteLockWhenPending(
            lastSynced.GetLastSyncedInternalAsync().GetAwaiter().GetResult() ?? 0,
            () => inner.ProcessInternalInstructions(cacheRefreshers, cancellationToken, localIdentity));

    private ProcessInstructionsResult InWriteLockWhenPending(int lastSyncedId, Func<ProcessInstructionsResult> process)
    {
        // An idle sync only reads, so it runs unlocked rather than committing every five seconds. An instruction that
        // arrives after this check is processed unguarded, as Umbraco processes every instruction.
        if (inner.GetMaxInstructionId() <= lastSyncedId)
        {
            return process();
        }

        using var scope = scopeProvider.CreateCoreScope();

        // On SQL Server, the exit path, Servers makes the sync wait only on server registration, not on content writes.
        scope.WriteLock(Constants.Locks.Servers);
        var result = process();
        scope.Complete();
        return result;
    }
}
