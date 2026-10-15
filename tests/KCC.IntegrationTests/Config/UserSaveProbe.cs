using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace KCC.IntegrationTests.Config;

// A backoffice user save reads the user, its groups and any rival for the same email or name, then inserts or
// updates; this notification comes between the two. The distributed-job poller commits every five seconds, so in CI a
// commit lands there only now and then.
public sealed class UserSaveProbe : INotificationHandler<UserSavingNotification>
{
    private Func<Task>? write;

    public Task? Committed { get; private set; }

    public void CommitDuringNextSave(Func<Task> commit)
    {
        Committed = null;
        write = commit;
    }

    public void Handle(UserSavingNotification notification)
    {
        if (Interlocked.Exchange(ref write, null) is { } commit)
        {
            Committed = OtherConnection.Commit(commit);
        }
    }
}
