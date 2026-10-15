using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Persistence.Repositories;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;

namespace KCC.Web.Features.Sqlite;

// Signing in to the backoffice and fetching an API user's token each save the user through Identity, outside the user
// service: the store reads the user and then saves it, in one transaction, which strands on a stale snapshot the way
// a member save does. Holding the member write lock from the start makes it the writer before it reads. The store's
// own scope joins this one.
public sealed class WriteLockedBackOfficeUserStore : BackOfficeUserStore
{
    private readonly ICoreScopeProvider scopeProvider;

    public WriteLockedBackOfficeUserStore(
        ICoreScopeProvider scopeProvider,
        IEntityService entityService,
        IExternalLoginWithKeyService externalLoginService,
        IOptionsSnapshot<GlobalSettings> globalSettings,
        IUmbracoMapper mapper,
        BackOfficeErrorDescriber describer,
        AppCaches appCaches,
        ITwoFactorLoginService twoFactorLoginService,
        IUserGroupService userGroupService,
        IUserRepository userRepository,
        IRuntimeState runtimeState,
        IEventMessagesFactory eventMessagesFactory,
        ILogger<BackOfficeUserStore> logger,
        IBackOfficeUserReader backOfficeUserReader)
        : base(
            scopeProvider,
            entityService,
            externalLoginService,
            globalSettings,
            mapper,
            describer,
            appCaches,
            twoFactorLoginService,
            userGroupService,
            userRepository,
            runtimeState,
            eventMessagesFactory,
            logger,
            backOfficeUserReader) =>
        this.scopeProvider = scopeProvider;

    public override Task<IdentityResult> CreateAsync(BackOfficeIdentityUser user, CancellationToken cancellationToken = default) =>
        LockedAsync(() => base.CreateAsync(user, cancellationToken));

    public override Task<IdentityResult> UpdateAsync(BackOfficeIdentityUser user, CancellationToken cancellationToken = default) =>
        LockedAsync(() => base.UpdateAsync(user, cancellationToken));

    private async Task<IdentityResult> LockedAsync(Func<Task<IdentityResult>> write)
    {
        using var scope = scopeProvider.CreateCoreScope();
        scope.WriteLock(Constants.Locks.MemberTree);
        var result = await write();
        scope.Complete();
        return result;
    }
}
