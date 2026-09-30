using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;

namespace KCC.Web.Features.Sqlite;

// Umbraco updates automatic relations in a scope of its own after a content, media or member save has committed. It
// reads the existing relations and then writes, and on SQLite a transaction that reads first cannot become the writer
// once another connection has committed: the write fails on a stale snapshot, and Umbraco's retry policy repeats it
// for about ten minutes. Taking the write lock first makes the update the writer before it reads, and the inner
// handler's scope joins this one.
public sealed class WriteLockedRelationsUpdate<TNotification>(
    INotificationHandler<TNotification> inner,
    ICoreScopeProvider scopeProvider,
    int lockId) : IDistributedCacheNotificationHandler<TNotification>
    where TNotification : INotification
{
    public void Handle(TNotification notification) => Handle([notification]);

    public void Handle(IEnumerable<TNotification> notifications)
    {
        using var scope = scopeProvider.CreateCoreScope();
        scope.WriteLock(lockId);
        inner.Handle(notifications);
        scope.Complete();
    }
}
