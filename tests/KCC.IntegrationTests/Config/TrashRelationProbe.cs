using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace KCC.IntegrationTests.Config;

// After a trash has committed, Umbraco reads whether the old parent and its relation exist, then saves the relation;
// this notification comes between the two. Reviews written freely seldom land there: the trash's own write lock parks
// them in the SQLite driver's 150 ms busy wait, and the relation is usually saved before they wake.
public sealed class TrashRelationProbe : INotificationHandler<RelationSavingNotification>
{
    private Func<Task>? write;

    public Task? Committed { get; private set; }

    public void CommitDuringNextTrash(Func<Task> commit)
    {
        Committed = null;
        write = commit;
    }

    public void Handle(RelationSavingNotification notification)
    {
        if (notification.SavedEntities.Any(relation => relation.RelationType.Alias == Constants.Conventions.RelationTypes.RelateParentDocumentOnDeleteAlias)
            && Interlocked.Exchange(ref write, null) is { } commit)
        {
            Committed = OtherConnection.Commit(commit);
        }
    }
}
