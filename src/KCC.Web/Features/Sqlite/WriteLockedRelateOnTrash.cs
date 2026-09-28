using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;

namespace KCC.Web.Features.Sqlite;

// Once a trash has committed, Umbraco relates each trashed item to its old parent in a scope of its own: it reads
// whether the parent and the relation exist, then saves the relation and an audit entry. That transaction strands on
// a stale snapshot the way the relations update does, and has the same cure. Umbraco's handler is not a
// distributed-cache handler, so neither is this one: a publisher that reaches only those skips both.
public sealed class WriteLockedRelateOnTrash<TNotification>(
    INotificationAsyncHandler<TNotification> inner,
    ICoreScopeProvider scopeProvider,
    int lockId) : INotificationAsyncHandler<TNotification>
    where TNotification : INotification
{
    public Task HandleAsync(TNotification notification, CancellationToken cancellationToken) =>
        HandleAsync([notification], cancellationToken);

    public async Task HandleAsync(IEnumerable<TNotification> notifications, CancellationToken cancellationToken)
    {
        using var scope = scopeProvider.CreateCoreScope();
        scope.WriteLock(lockId);
        await inner.HandleAsync(notifications, cancellationToken);
        scope.Complete();
    }
}
