using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace KCC.Web.Features.Pages.Account;

public class KitchenSummaryCacheRefresher(KitchenSummaries summaries) : INotificationHandler<ContentCacheRefresherNotification>
{
    public void Handle(ContentCacheRefresherNotification notification) => summaries.Clear();
}
