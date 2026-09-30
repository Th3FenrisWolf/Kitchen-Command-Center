using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace KCC.Web.Features.Providers;

public class ProvidersComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IAuthorNameProvider, AuthorNameProvider>();
        builder
            .AddNotificationHandler<MemberSavedNotification, AuthorNameCacheRefresher>()
            .AddNotificationHandler<MemberDeletedNotification, AuthorNameCacheRefresher>();
    }
}
