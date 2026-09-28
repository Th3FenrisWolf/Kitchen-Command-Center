using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure.Services;

namespace KCC.Web.Features.Sqlite;

public class SqliteComposer : IComposer
{
    // Umbraco's handler is internal, so it can only be found by name.
    internal const string RelationsHandlerTypeName = "Umbraco.Cms.Infrastructure.Persistence.Relations.ContentRelationsUpdate";

    public void Compose(IUmbracoBuilder builder)
    {
        LockRelationsUpdate<ContentSavedNotification>(builder.Services, Constants.Locks.ContentTree);
        LockRelationsUpdate<ContentPublishedNotification>(builder.Services, Constants.Locks.ContentTree);
        LockRelationsUpdate<ContentUnpublishedNotification>(builder.Services, Constants.Locks.ContentTree);
        LockRelationsUpdate<MediaSavedNotification>(builder.Services, Constants.Locks.MediaTree);
        LockRelationsUpdate<MemberSavedNotification>(builder.Services, Constants.Locks.MemberTree);
        LockRelateOnTrash<ContentMovedToRecycleBinNotification>(builder.Services, Constants.Locks.ContentTree);
        LockRelateOnTrash<MediaMovedToRecycleBinNotification>(builder.Services, Constants.Locks.MediaTree);
        LockCacheInstructionSync(builder.Services);
        builder.Services.AddScoped<IMemberWriteLock, MemberWriteLock>();
    }

    private static void LockRelationsUpdate<TNotification>(IServiceCollection services, int lockId)
        where TNotification : INotification =>
        Wrap<INotificationHandler<TNotification>>(services, RelationsHandlerTypeName, (provider, inner) =>
            new WriteLockedRelationsUpdate<TNotification>(inner, provider.GetRequiredService<ICoreScopeProvider>(), lockId));

    private static void LockRelateOnTrash<TNotification>(IServiceCollection services, int lockId)
        where TNotification : INotification =>
        Wrap<INotificationAsyncHandler<TNotification>>(services, typeof(RelateOnTrashNotificationHandler).FullName, (provider, inner) =>
            new WriteLockedRelateOnTrash<TNotification>(inner, provider.GetRequiredService<ICoreScopeProvider>(), lockId));

    private static void LockCacheInstructionSync(IServiceCollection services) =>
        Wrap<ICacheInstructionService>(services, typeof(CacheInstructionService).FullName, (provider, inner) =>
            new WriteLockedCacheInstructionService(
                inner,
                provider.GetRequiredService<ILastSyncedManager>(),
                provider.GetRequiredService<ICoreScopeProvider>()));

    // Without the lock the site hangs for minutes under concurrent writes, so a registration that has moved in an
    // Umbraco upgrade stops the boot rather than going unwrapped. Replacing it in place keeps its lifetime, and a
    // handler's position among the others.
    private static void Wrap<TService>(IServiceCollection services, string implementationTypeName, Func<IServiceProvider, TService, TService> decorate)
        where TService : class
    {
        var descriptor = services.SingleOrDefault(candidate =>
                candidate.ServiceType == typeof(TService)
                && candidate.ImplementationType?.FullName == implementationTypeName)
            ?? throw new InvalidOperationException($"Umbraco no longer registers {implementationTypeName} as {typeof(TService)}.");

        var implementation = descriptor.ImplementationType;
        services[services.IndexOf(descriptor)] = ServiceDescriptor.Describe(
            typeof(TService),
            provider => decorate(provider, (TService)ActivatorUtilities.CreateInstance(provider, implementation)),
            descriptor.Lifetime);
    }
}
