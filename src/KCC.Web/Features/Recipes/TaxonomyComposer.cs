using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Extensions;

namespace KCC.Web.Features.Recipes;

public class TaxonomyComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddScoped<TaxonomyDefaults>();
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, TaxonomyDefaultsOnStartup>();
    }
}
