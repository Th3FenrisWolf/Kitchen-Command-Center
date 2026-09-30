using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace KCC.Web.Features.Providers;

public class AuthorNameCacheRefresher(IAuthorNameProvider authorNames)
    : INotificationHandler<MemberSavedNotification>, INotificationHandler<MemberDeletedNotification>
{
    public void Handle(MemberSavedNotification notification) =>
        authorNames.Forget(notification.SavedEntities.Select(member => member.Key));

    public void Handle(MemberDeletedNotification notification) =>
        authorNames.Forget(notification.DeletedEntities.Select(member => member.Key));
}
