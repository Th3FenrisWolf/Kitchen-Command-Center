using KCC.Contributions;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace KCC.Web.Features.Search;

// A recipe's document also carries its URL, category, tags, author's name and ratings, which live on other nodes,
// on members and in reviews, so any of those changing can change a document. Rebuilds are cheap, and a filter would
// have to know every one of those dependencies.
public class RecipeIndexTriggers(IRecipeIndexRebuilder rebuilder) :
    INotificationHandler<ContentCacheRefresherNotification>,
    INotificationHandler<MemberSavedNotification>,
    INotificationHandler<MemberDeletedNotification>,
    INotificationHandler<ReviewsChangedNotification>
{
    public void Handle(ContentCacheRefresherNotification notification) => rebuilder.Signal();

    public void Handle(MemberSavedNotification notification) => rebuilder.Signal();

    public void Handle(MemberDeletedNotification notification) => rebuilder.Signal();

    public void Handle(ReviewsChangedNotification notification) => rebuilder.Signal();
}
