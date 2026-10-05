using KCC.Web.Features.Pages.Account;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace KCC.Web.Features.Components.Header;

public class NavComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddScoped<NavModelBuilder>();
        builder.Services.AddSingleton<KitchenSummaries>();
        builder.AddNotificationHandler<ContentCacheRefresherNotification, KitchenSummaryCacheRefresher>();
    }
}
