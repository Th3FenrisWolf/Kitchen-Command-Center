using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Persistence.Querying;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;

namespace KCC.Web.Features.Sqlite;

// Each of Umbraco's backoffice user writes reads first, in one transaction: the performing user, the groups, any
// user already holding the email or name, then a password hash, then the save. On SQLite that transaction cannot
// become the writer once another connection has committed, and Umbraco's retry policy repeats it for about ten
// minutes. Holding the member write lock from the start makes it the writer before it reads, and shares one lock with
// member saves. The inner service's scopes join this one. Reads pass straight through, and so do the default
// interface members, which UserService replaces.
public sealed class WriteLockedUserService(IUserService inner, ICoreScopeProvider scopeProvider) : IUserService
{
    internal IUserService Inner => inner;

    public int GetCount(MemberCountType countType) => inner.GetCount(countType);

    public bool Exists(string username) => inner.Exists(username);

    public IUser CreateWithIdentity(string username, string email, string passwordValue, string memberTypeAlias) =>
        Locked(() => inner.CreateWithIdentity(username, email, passwordValue, memberTypeAlias));

    public IUser CreateWithIdentity(string username, string email, string passwordValue, string memberTypeAlias, bool isApproved) =>
        Locked(() => inner.CreateWithIdentity(username, email, passwordValue, memberTypeAlias, isApproved));

    public IUser CreateUserWithIdentity(string username, string email) => Locked(() => inner.CreateUserWithIdentity(username, email));

    public IUser GetByProviderKey(object id) => inner.GetByProviderKey(id);

    public IUser GetByEmail(string email) => inner.GetByEmail(email);

    public IUser GetByUsername(string username) => inner.GetByUsername(username);

    public void Delete(IUser membershipUser) => Locked(() => inner.Delete(membershipUser));

    public void Delete(IUser user, bool deletePermanently) => Locked(() => inner.Delete(user, deletePermanently));

    public void Save(IUser entity) => Locked(() => inner.Save(entity));

    public void Save(IEnumerable<IUser> entities) => Locked(() => inner.Save(entities));

    public IEnumerable<IUser> FindByEmail(string emailStringToMatch, long pageIndex, int pageSize, out long totalRecords, StringPropertyMatchType matchType = StringPropertyMatchType.StartsWith) =>
        inner.FindByEmail(emailStringToMatch, pageIndex, pageSize, out totalRecords, matchType);

    public IEnumerable<IUser> FindByUsername(string login, long pageIndex, int pageSize, out long totalRecords, StringPropertyMatchType matchType = StringPropertyMatchType.StartsWith) =>
        inner.FindByUsername(login, pageIndex, pageSize, out totalRecords, matchType);

    public IEnumerable<IUser> GetAll(long pageIndex, int pageSize, out long totalRecords) => inner.GetAll(pageIndex, pageSize, out totalRecords);

    public Guid CreateLoginSession(int userId, string requestingIpAddress) => Locked(() => inner.CreateLoginSession(userId, requestingIpAddress));

    public bool ValidateLoginSession(int userId, Guid sessionId) => Locked(() => inner.ValidateLoginSession(userId, sessionId));

    public void ClearLoginSession(Guid sessionId) => Locked(() => inner.ClearLoginSession(sessionId));

    public int ClearLoginSessions(int userId) => Locked(() => inner.ClearLoginSessions(userId));

    public IDictionary<UserState, int> GetUserStates() => inner.GetUserStates();

    public Task<Attempt<UserCreationResult, UserOperationStatus>> CreateAsync(Guid performingUserKey, UserCreateModel model, bool approveUser = false) =>
        LockedAsync(() => inner.CreateAsync(performingUserKey, model, approveUser));

    public Task<Attempt<UserInvitationResult, UserOperationStatus>> InviteAsync(Guid performingUserKey, UserInviteModel model) =>
        LockedAsync(() => inner.InviteAsync(performingUserKey, model));

    public Task<Attempt<UserOperationStatus>> VerifyInviteAsync(Guid userKey, string token) => inner.VerifyInviteAsync(userKey, token);

    public Task<Attempt<PasswordChangedModel, UserOperationStatus>> CreateInitialPasswordAsync(Guid userKey, string token, string password) =>
        LockedAsync(() => inner.CreateInitialPasswordAsync(userKey, token, password));

    public Task<Attempt<IUser, UserOperationStatus>> UpdateAsync(Guid performingUserKey, UserUpdateModel model) =>
        LockedAsync(() => inner.UpdateAsync(performingUserKey, model));

    public Task<Attempt<IUser, UserOperationStatus>> UpdateProfileAsync(Guid userKey, UserUpdateProfileModel model) =>
        LockedAsync(() => inner.UpdateProfileAsync(userKey, model));

    public Task<UserOperationStatus> SetAvatarAsync(Guid userKey, Guid temporaryFileKey) => LockedAsync(() => inner.SetAvatarAsync(userKey, temporaryFileKey));

    public Task<UserOperationStatus> DeleteAsync(Guid performingUserKey, ISet<Guid> keys) => LockedAsync(() => inner.DeleteAsync(performingUserKey, keys));

    public Task<UserOperationStatus> DeleteAsync(Guid performingUserKey, Guid key) => LockedAsync(() => inner.DeleteAsync(performingUserKey, key));

    public Task<UserOperationStatus> DisableAsync(Guid performingUserKey, ISet<Guid> keys) => LockedAsync(() => inner.DisableAsync(performingUserKey, keys));

    public Task<UserOperationStatus> EnableAsync(Guid performingUserKey, ISet<Guid> keys) => LockedAsync(() => inner.EnableAsync(performingUserKey, keys));

    public Task<Attempt<UserUnlockResult, UserOperationStatus>> UnlockAsync(Guid performingUserKey, params Guid[] keys) =>
        LockedAsync(() => inner.UnlockAsync(performingUserKey, keys));

    public Task<Attempt<PasswordChangedModel, UserOperationStatus>> ChangePasswordAsync(Guid performingUserKey, ChangeUserPasswordModel model) =>
        LockedAsync(() => inner.ChangePasswordAsync(performingUserKey, model));

    public Task<UserOperationStatus> ClearAvatarAsync(Guid userKey) => LockedAsync(() => inner.ClearAvatarAsync(userKey));

    public Task<Attempt<ICollection<IIdentityUserLogin>, UserOperationStatus>> GetLinkedLoginsAsync(Guid userKey) => inner.GetLinkedLoginsAsync(userKey);

    public Task<Attempt<PagedModel<IUser>, UserOperationStatus>> GetAllAsync(Guid performingUserKey, int skip, int take) =>
        inner.GetAllAsync(performingUserKey, skip, take);

    public Task<Attempt<PagedModel<IUser>, UserOperationStatus>> FilterAsync(Guid userKey, UserFilter filter, int skip = 0, int take = 100, UserOrder orderBy = UserOrder.UserName, Direction orderDirection = Direction.Ascending) =>
        inner.FilterAsync(userKey, filter, skip, take, orderBy, orderDirection);

    public IEnumerable<IUser> GetAll(long pageIndex, int pageSize, out long totalRecords, string orderBy, Direction orderDirection, UserState[] userState = null, string[] includeUserGroups = null, string[] excludeUserGroups = null, IQuery<IUser> filter = null) =>
        inner.GetAll(pageIndex, pageSize, out totalRecords, orderBy, orderDirection, userState, includeUserGroups, excludeUserGroups, filter);

    public IEnumerable<IUser> GetAll(long pageIndex, int pageSize, out long totalRecords, string orderBy, Direction orderDirection, UserState[] userState = null, string[] userGroups = null, string filter = null) =>
        inner.GetAll(pageIndex, pageSize, out totalRecords, orderBy, orderDirection, userState, userGroups, filter);

    public IProfile GetProfileById(int id) => inner.GetProfileById(id);

    public IProfile GetProfileByUserName(string username) => inner.GetProfileByUserName(username);

    public Task<IUser> GetAsync(Guid key) => inner.GetAsync(key);

    public Task<IEnumerable<IUser>> GetAsync(IEnumerable<Guid> keys) => inner.GetAsync(keys);

    public IUser GetUserById(int id) => inner.GetUserById(id);

    public IEnumerable<IUser> GetUsersById(params int[] ids) => inner.GetUsersById(ids);

    public void DeleteSectionFromAllUserGroups(string sectionAlias) => Locked(() => inner.DeleteSectionFromAllUserGroups(sectionAlias));

    public Task<Attempt<IEnumerable<NodePermissions>, UserOperationStatus>> GetPermissionsAsync(Guid userKey, params Guid[] nodeKeys) =>
        inner.GetPermissionsAsync(userKey, nodeKeys);

    public Task<Attempt<IEnumerable<NodePermissions>, UserOperationStatus>> GetMediaPermissionsAsync(Guid userKey, IEnumerable<Guid> mediaKeys) =>
        inner.GetMediaPermissionsAsync(userKey, mediaKeys);

    public Task<Attempt<IEnumerable<NodePermissions>, UserOperationStatus>> GetDocumentPermissionsAsync(Guid userKey, IEnumerable<Guid> contentKeys) =>
        inner.GetDocumentPermissionsAsync(userKey, contentKeys);

    public EntityPermissionCollection GetPermissions(IUser user, params int[] nodeIds) => inner.GetPermissions(user, nodeIds);

    public EntityPermissionCollection GetPermissions(IUserGroup[] groups, bool fallbackToDefaultPermissions, params int[] nodeIds) =>
        inner.GetPermissions(groups, fallbackToDefaultPermissions, nodeIds);

    public EntityPermissionSet GetPermissionsForPath(IUser user, string path) => inner.GetPermissionsForPath(user, path);

    public EntityPermissionSet GetPermissionsForPath(IUserGroup[] groups, string path, bool fallbackToDefaultPermissions = false) =>
        inner.GetPermissionsForPath(groups, path, fallbackToDefaultPermissions);

    public void ReplaceUserGroupPermissions(int groupId, ISet<string> permissions, params int[] entityIds) =>
        Locked(() => inner.ReplaceUserGroupPermissions(groupId, permissions, entityIds));

    public void AssignUserGroupPermission(int groupId, string permission, params int[] entityIds) =>
        Locked(() => inner.AssignUserGroupPermission(groupId, permission, entityIds));

    public IEnumerable<IUser> GetAllInGroup(int? groupId) => inner.GetAllInGroup(groupId);

    public IEnumerable<IUser> GetAllNotInGroup(int groupId) => inner.GetAllNotInGroup(groupId);

    public Task<Attempt<UserOperationStatus>> VerifyPasswordResetAsync(Guid userKey, string token) => inner.VerifyPasswordResetAsync(userKey, token);

    public Task<Attempt<PasswordChangedModel, UserOperationStatus>> ResetPasswordAsync(Guid userKey, string token, string password) =>
        LockedAsync(() => inner.ResetPasswordAsync(userKey, token, password));

    public Task<Attempt<UserOperationStatus>> SendResetPasswordEmailAsync(string userEmail) => LockedAsync(() => inner.SendResetPasswordEmailAsync(userEmail));

    public Task<Attempt<UserInvitationResult, UserOperationStatus>> ResendInvitationAsync(Guid performingUserKey, UserResendInviteModel model) =>
        LockedAsync(() => inner.ResendInvitationAsync(performingUserKey, model));

    public Task<Attempt<PasswordChangedModel, UserOperationStatus>> ResetPasswordAsync(Guid performingUserKey, Guid userKey) =>
        LockedAsync(() => inner.ResetPasswordAsync(performingUserKey, userKey));

    public Task<UserClientCredentialsOperationStatus> AddClientIdAsync(Guid userKey, string clientId) => LockedAsync(() => inner.AddClientIdAsync(userKey, clientId));

    public Task<bool> RemoveClientIdAsync(Guid userKey, string clientId) => LockedAsync(() => inner.RemoveClientIdAsync(userKey, clientId));

    public Task<IUser> FindByClientIdAsync(string clientId) => inner.FindByClientIdAsync(clientId);

    public Task<IEnumerable<string>> GetClientIdsAsync(Guid userKey) => inner.GetClientIdsAsync(userKey);

    private void Locked(Action write) => Locked(() =>
    {
        write();
        return true;
    });

    private T Locked<T>(Func<T> write)
    {
        using var scope = scopeProvider.CreateCoreScope();
        scope.WriteLock(Constants.Locks.MemberTree);
        var result = write();
        scope.Complete();
        return result;
    }

    private async Task<T> LockedAsync<T>(Func<Task<T>> write)
    {
        using var scope = scopeProvider.CreateCoreScope();
        scope.WriteLock(Constants.Locks.MemberTree);
        var result = await write();
        scope.Complete();
        return result;
    }
}
