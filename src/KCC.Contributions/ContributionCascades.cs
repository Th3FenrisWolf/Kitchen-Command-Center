using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace KCC.Contributions;

// Only a permanent delete cascades: a variant in the recycle bin keeps its rows, so restoring it restores its
// reviews. Only variants have rows, so the keys of whatever else was deleted match nothing.
public class ContributionCascades(IContributionWrites contributionWrites) :
    INotificationAsyncHandler<ContentDeletedNotification>,
    INotificationAsyncHandler<MemberDeletedNotification>
{
    public Task HandleAsync(ContentDeletedNotification notification, CancellationToken cancellationToken) =>
        contributionWrites.DeleteForVariantsAsync(notification.DeletedEntities.Select(content => content.Key).ToList());

    public Task HandleAsync(MemberDeletedNotification notification, CancellationToken cancellationToken) =>
        contributionWrites.DeleteForMembersAsync(notification.DeletedEntities.Select(member => member.Key).ToList());
}
