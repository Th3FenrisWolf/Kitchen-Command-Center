using Microsoft.AspNetCore.Identity;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure.Services;

namespace KCC.Web.Features.Sqlite;

public class SqliteComposer : IComposer
{
    // Umbraco's handler is internal, so it can only be found by name.
    internal const string RelationsHandlerTypeName = "Umbraco.Cms.Infrastructure.Persistence.Relations.ContentRelationsUpdate";

    // Umbraco's user service is internal too.
    internal const string UserServiceTypeName = "Umbraco.Cms.Core.Services.UserService";

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
        LockUserWrites(builder.Services);
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

    private static void LockUserWrites(IServiceCollection services)
    {
        Wrap<IUserService>(services, UserServiceTypeName, (provider, inner) =>
            new WriteLockedUserService(inner, provider.GetRequiredService<ICoreScopeProvider>()));
        Replace<IUserStore<BackOfficeIdentityUser>>(services, typeof(BackOfficeUserStore), CreateLockedUserStore);
        Replace<IBackOfficeUserStore>(services, typeof(BackOfficeUserStore), CreateLockedUserStore);
    }

    private static WriteLockedBackOfficeUserStore CreateLockedUserStore(IServiceProvider provider) =>
        ActivatorUtilities.CreateInstance<WriteLockedBackOfficeUserStore>(provider);

    // Without the lock the site hangs for minutes under concurrent writes, so a registration that has moved in an
    // Umbraco upgrade stops the boot rather than going unwrapped. Replacing it in place with the same lifetime keeps
    // the cache-instruction service a singleton, so there is one inner service and one sync lock, and keeps each
    // handler's position among the others.
    private static void Wrap<TService>(IServiceCollection services, string implementationTypeName, Func<IServiceProvider, TService, TService> decorate)
        where TService : class
    {
        var descriptor = Find<TService>(services, implementationTypeName);
        var implementation = descriptor.ImplementationType
            ?? throw new InvalidOperationException($"Umbraco registers {implementationTypeName} as {typeof(TService)} through a factory.");
        services[services.IndexOf(descriptor)] = ServiceDescriptor.Describe(
            typeof(TService),
            provider => decorate(provider, (TService)ActivatorUtilities.CreateInstance(provider, implementation)),
            descriptor.Lifetime);
    }

    // Umbraco builds the backoffice user store in a factory, so only the factory's return type names what it builds.
    private static void Replace<TService>(IServiceCollection services, Type implementation, Func<IServiceProvider, TService> create)
        where TService : class
    {
        var descriptor = Find<TService>(services, implementation.FullName);
        services[services.IndexOf(descriptor)] = ServiceDescriptor.Describe(typeof(TService), create, descriptor.Lifetime);
    }

    private static ServiceDescriptor Find<TService>(IServiceCollection services, string implementationTypeName) =>
        services.SingleOrDefault(candidate =>
            candidate.ServiceType == typeof(TService)
            && (candidate.ImplementationType ?? candidate.ImplementationFactory?.Method.ReturnType)?.FullName == implementationTypeName)
        ?? throw new InvalidOperationException($"Umbraco no longer registers {implementationTypeName} as {typeof(TService)}.");
}
